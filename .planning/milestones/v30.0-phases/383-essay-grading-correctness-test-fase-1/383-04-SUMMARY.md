---
phase: 383-essay-grading-correctness-test-fase-1
plan: "04"
subsystem: testing
tags: [essay-grading, regression-test, integration, authz-lock, ECG-06]
requires:
  - "AssessmentScoreAggregator.Compute (essay-aware) — 383-01"
  - "AssessmentAdminController.SubmitEssayScore / FinalizeEssayGrading (existing, D-05 locked)"
provides:
  - "Regression lock SubmitEssayScore persist + range guard (ECG-06)"
  - "Regression lock FinalizeEssayGrading recompute-incl-essay + idempotent (ECG-06)"
  - "Authz reflection-assert [Authorize(Roles=Admin,HC)] kedua action (T-383-07)"
affects:
  - "HcPortal.Tests/EssayFinalizeRecomputeTests.cs"
tech-stack:
  added: []
  patterns:
    - "Mirror-data-level (precedent file) — hindari ctor 12-dep controller"
    - "Disposable real-SQL fixture (ExecuteUpdateAsync tak jalan di EF8 InMemory)"
    - "Reflection-assert attribute untuk lock authz (mirror tak eksekusi [Authorize])"
key-files:
  created: []
  modified:
    - "HcPortal.Tests/EssayFinalizeRecomputeTests.cs"
decisions:
  - "ECG-06 lock via mirror-data-level + reflection-authz; NO production code change (D-05)"
  - "Authz dikunci via reflection (BUKAN gap) — kedua action public, GetMethods().First() hindari overload-ambiguity"
metrics:
  duration: "~8 min"
  tasks: 2
  files: 1
  tests_added: 5
  completed: 2026-06-15
---

# Phase 383 Plan 04: Essay Grading Correctness — Regression Lock (ECG-06) Summary

Regression test mengunci logika Simpan Skor + Selesaikan Penilaian essay (poin 2, sudah benar per D-05) tanpa mengubah kode produksi — 5 test baru di `EssayFinalizeRecomputeTests.cs` (real-SQL disposable fixture + 1 pure authz reflection).

## What Was Built

Menambah 5 test + 2 mirror helper + 1 scope helper ke `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (ECG-06). NO production code change — `SubmitEssayScore`/`FinalizeEssayGrading` di-lock apa adanya.

### Task 1 — Lock SubmitEssayScore (commit `24e44cb4`)
- `MirrorSubmitEssayScoreAsync` — mirror `AssessmentAdminController.cs:3460-3477` data-level (persist `EssayScore` + range guard `score < 0 || score > question.ScoreValue`), dengan komentar drift-guard ke file:line controller.
- `SubmitEssayScore_Persists_WhenInRange` — skor 80 (dalam 0..100) ter-persist ke `EssayScore` (verifikasi via ctx baru).
- `SubmitEssayScore_Rejects_WhenOutOfRange` — 150 (>ScoreValue) + -5 (<0) ditolak; `EssayScore` tetap 0 (tak ter-persist). T-298-13 / V5 ASVS.
- `QuestionOfSessionAsync` — helper scope soal ke session (fixture = shared DB; `FirstAsync` global tak aman → bug awal `ok=false` karena ambil soal milik test lain; fixed).

### Task 2 — Lock FinalizeEssayGrading + authz (commit `158a9f03`)
- `MirrorFinalizeWriteAsync` — mirror `FinalizeEssayGrading` write-step (`AssessmentAdminController.cs:3506` D-03 no-op Completed, `:3585` `Compute` essay-aware, `:3593-3599` `ExecuteUpdateAsync` WHERE `Status==PendingGrading`). Returns rowsAffected.
- `Finalize_Recompute_IncludesEssayScore` — essay dinilai 80 → recompute essay-aware 80% (bukan 0), `MirrorFinalizeWriteAsync` menulis 1 baris, sesi `PendingGrading → Completed`, `Score=80` `IsPassed=true`.
- `Finalize_NoOp_WhenAlreadyCompleted` — re-finalize sesi sudah `Completed` → 0 baris (WHERE `Status==PendingGrading` tak match) → `Score` tak berubah (idempotent, no double-count). D-03 / T-383-09.
- `SubmitAndFinalize_RequireAdminHcAuthorize` (class `EssaySubmitFinalizeAuthzTests`, **pure, no DB**) — reflection-assert `[Authorize]` di kedua action; `Roles` mengandung "Admin" + "HC". T-383-07 / V4 ASVS. **Authz dikunci, BUKAN gap** (RESEARCH OQ#3 resolved).

## Verification Results

| Check | Result |
|-------|--------|
| `dotnet build` | 0 error (25 pre-existing warning, out of scope) |
| `dotnet test` SubmitEssayScore | 2/2 hijau |
| `dotnet test` Finalize_Recompute/NoOp/RequireAdminHc | 3/3 hijau |
| `dotnet test` EssayFinalizeRecompute + authz | 8/8 hijau |
| `dotnet test` (full, incl Integration) | **440/440 hijau** |
| SQLEXPRESS (`localhost\SQLEXPRESS`) | **TERSEDIA** — integration jalan penuh (tidak skip) |
| Authz lock | **reflection-assert (terkunci)**, bukan known gap |
| Production code untouched | `git hash-object Controllers/AssessmentAdminController.cs` = `0e69e96c...` (identik baseline pre-plan) |

### Migration guard (D-04)
`dotnet ef migrations add _verify_383 --no-build` → **Up/Down kosong (0 model diff)**, lalu file dihapus (orphan, tak pernah applied, tak masuk git). `ApplicationDbContextModelSnapshot.cs` & seluruh `Migrations/` tree TIDAK berubah vs HEAD. Membuktikan **0 migration baru** (plan ini hanya menyentuh file test — zero model/DbContext change). Tak ada carry-migration baru untuk notify IT dari plan ini.

> Catatan: `dotnet ef migrations remove` abort (cek DB state — migration `AddShuffleTogglesToAssessmentSession` sudah applied di DB lokal), jadi orphan `_verify_383` files dihapus manual via `rm` (aman: empty diff, tak pernah applied, untracked). Tree dikonfirmasi clean.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed test cross-session question leak (shared fixture DB)**
- **Found during:** Task 1 (test `SubmitEssayScore_Persists_WhenInRange` gagal `ok=false`).
- **Issue:** `ctx.PackageQuestions.FirstAsync()` mengambil soal pertama dari **seluruh** DB shared-fixture (xUnit `IClassFixture` 1 DB untuk semua test di class), bisa milik session test lain → `questionId` tak match response session yang di-seed → mirror return `(false, "Jawaban tidak ditemukan")`.
- **Fix:** Tambah helper `QuestionOfSessionAsync(ctx, sessionId)` yang scope soal ke package milik session; kedua test SubmitEssayScore pakai helper ini (bukan `FirstAsync` global).
- **Files modified:** `HcPortal.Tests/EssayFinalizeRecomputeTests.cs`
- **Commit:** `24e44cb4`

Selebihnya: plan dieksekusi sesuai tulisan. Authz di-lock via reflection (opsi utama plan, bukan fallback "known gap").

## Threat Model Coverage

| Threat ID | Disposition | Status |
|-----------|-------------|--------|
| T-383-07 (EoP non-HC menilai essay) | mitigate (lock) | ✅ reflection-assert `[Authorize(Roles=Admin,HC)]` kedua action |
| T-383-08 (skor essay di luar range) | mitigate (lock) | ✅ test range guard `score<0 \|\| >ScoreValue` |
| T-383-09 (double-finalize korup state) | mitigate (lock) | ✅ test idempotent no-op saat `Status==Completed` |

Tidak ada permukaan keamanan baru di luar threat_model (plan ini test-only, NO production change).

## Known Stubs

None — plan ini menambah test, bukan UI/data flow.

## Self-Check: PASSED

- FOUND: `HcPortal.Tests/EssayFinalizeRecomputeTests.cs` (modified, 5 test baru)
- FOUND commit `24e44cb4` (Task 1 SubmitEssayScore lock)
- FOUND commit `158a9f03` (Task 2 Finalize + authz lock)
- VERIFIED: `Controllers/AssessmentAdminController.cs` hash `0e69e96c...` = identik baseline (production untouched, D-05)
- VERIFIED: full suite 440/440 hijau (incl Integration real-SQL)
- VERIFIED: migration guard 0 model diff (D-04 no-migration)
