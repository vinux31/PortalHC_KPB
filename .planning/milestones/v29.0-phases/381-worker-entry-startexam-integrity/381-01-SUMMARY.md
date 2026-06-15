---
phase: 381-worker-entry-startexam-integrity
plan: 01
subsystem: assessment-entry
tags: [assessment, sibling-query, prepost, shuffle, determinism, exam-entry]
requires: [WSE-01-engine-filter]
provides: [WSE-04-type-aware-sibling, sibling-prepost-predicate, reshuffle-type-parity]
affects: [Helpers/SiblingSessionQuery.cs, Controllers/CMPController.cs, Controllers/AssessmentAdminController.cs]
tech-stack:
  added: []
  patterns: [shared-expression-predicate, type-aware-isolation, in-memory-sibling-grouping]
key-files:
  created:
    - Helpers/SiblingSessionQuery.cs
    - HcPortal.Tests/SiblingPrePostFilterTests.cs
    - HcPortal.Tests/SiblingDeterminismTests.cs
  modified:
    - Controllers/CMPController.cs
    - Controllers/AssessmentAdminController.cs
key-decisions:
  - "D-01/D-09: type-aware isolation (BUKAN equality ketat) — `isPrePost ? s.AssessmentType==type : (s.AssessmentType != PreTest && != PostTest)`. Hanya Pre/Post diisolasi; Standard/''/null satu grup (zero behavior-change non-PrePost, aman legacy). LinkedGroupId TIDAK dipakai."
  - "D-02/D-08: shared helper `SiblingPrePostAwarePredicate` dipakai IDENTIK di StartExam + ReshufflePackage + ReshuffleAll (determinisme workerIndex Phase 373). UpdateShuffleSettings/ManagePackages lock-detection DIBIARKAN group-wide."
  - "DEVIATION (full-parity, user-approved): ReshuffleAll diberi packages type-aware per-session (sessionPackages) + workerIndex type-aware (SiblingKey dict) + skip-guard type tanpa paket — bukan hanya workerIndex (plan-scope). Menutup jalur assignment terkontaminasi yang StartExam sajikan via resume."
requirements-completed: [WSE-04]
duration: ~45 min
completed: 2026-06-15
---

# Phase 381 Plan 01: WSE-04 Type-Aware Sibling Isolation + Determinism Summary

Pre/Post same-day tidak lagi saling memungut paket. Diskriminasi pool dipusatkan di satu shared Expression predicate `SiblingSessionQuery.SiblingPrePostAwarePredicate` (type-aware: hanya PreTest/PostTest diisolasi; Standard/''/null tetap satu grup non-PrePost — zero behavior-change + aman data legacy). Helper dipakai IDENTIK di `StartExam` GET + `ReshufflePackage` + `ReshuffleAll` → sibling-set & `workerIndex` konsisten lintas entry↔reshuffle (invariant Phase 373 terjaga). `UpdateShuffleSettings`/`ManagePackages` lock-detection dibiarkan group-wide (D-08).

## Execution

- **Duration:** ~45 min (termasuk detour: stop dev-server PID 3144 yang mengunci binary) · **Tasks:** 3 · **Files:** 5 (2 helper/test baru + 1 helper + 2 controller)
- **Commits:** `cec93f6f` (Task 1 helper+unit RED→GREEN), `c2c3a803` (Task 2 StartExam rewire + determinism test), `924d7122` (Task 3 reshuffle full-parity)

### Task 1 — Shared type-aware predicate + 5 unit tests (TDD RED→GREEN)
`Helpers/SiblingSessionQuery.SiblingPrePostAwarePredicate(title, category, scheduleDate, assessmentType)`. RED diproven: `SiblingPrePostFilterTests` ditulis dulu → `dotnet build` gagal `CS0103: SiblingSessionQuery does not exist` → helper ditambah → 5/5 green. Tests: Pre isolation, Post isolation, Standard groups non-PrePost (Standard/''/null), null caller, key-mismatch. (Komentar D-01 di-reword agar grep `LinkedGroupId`==0 — kriteria literal.)

### Task 2 — StartExam rewire + determinism test
`CMPController.StartExam` sibling-query (~1012) kini pakai helper. `sortedSiblingIds.OrderBy(x=>x)` + `IndexOf(id)` (workerIndex) dibiarkan (Pitfall 2 sudah benar). Empty-package pre-check Phase 380 (`preCheckSiblingIds`, ~970-990) TIDAK disentuh. `SiblingDeterminismTests` (2 [Fact]) mengunci invariant `OrderBy(x=>x).IndexOf` stabil terlepas urutan input = bukti StartExam-side == reshuffle-side bila sibling-SET identik.

### Task 3 — ReshufflePackage + ReshuffleAll (full parity)
- **ReshufflePackage:** sibling-query → helper; `packages` query (`siblingSessionIds.Contains`) jadi type-aware otomatis.
- **ReshuffleAll (DEVIATION full-parity):** plan hanya minta workerIndex type-aware, tapi `packages` global tetap campur Pre+Post → assignment terkontaminasi yang StartExam sajikan via resume. Fix lengkap: `SiblingKey` local func + `sortedByKey` dict (workerIndex per-session terhadap type-nya) **dan** `sessionPackages` (filter packages per-session by type-aware sibling) + guard skip session bila type-nya belum punya paket. Normal exam (semua non-PrePost satu key `__NONPREPOST__`) → identik perilaku lama.
- **D-08:** `UpdateShuffleSettings`/`ManagePackages` sibling-query inline TIDAK berubah (lock group-wide).

## Verification

- `dotnet build` — 0 errors.
- `dotnet test HcPortal.Tests` — **391 passed, 0 failed** (full suite, no regression; +7 baru: 5 SiblingPrePostFilter + 2 SiblingDeterminism).
- RED→GREEN proven Task 1 (`CS0103` sebelum helper → 5/5 setelah).
- Grep: `SiblingPrePostAwarePredicate` di `SiblingSessionQuery.cs`(1) + `CMPController.cs`(1) + `AssessmentAdminController.cs`(1, ReshufflePackage).
- D-08: 2 inline sibling-query tersisa (UpdateShuffleSettings@5377 + ManagePackages@5446), helper TIDAK dipakai di keduanya.
- empty-package pre-check (`preCheckSiblingIds`) intact di CMPController.
- **No migration:** hanya edit controller/helper/test — no Model/DbContext/Migrations diff (gate final Plan 03).

## Deviations from Plan

**[Rule 2 — Missing critical, user-approved via interactive checkpoint] ReshuffleAll full-parity packages** — Found during: Task 3. Issue: plan Task 3 hanya menentukan workerIndex type-aware untuk ReshuffleAll; `packages` tetap campur Pre+Post → ReshuffleAll pada grup same-day Pre/Post menulis assignment campur-paket yang StartExam sajikan saat resume (WSE-04 tak tuntas di jalur reshuffle). Fix: tambah `sessionPackages` type-aware per-session + skip-guard. User dikonfirmasi pilih "Full parity" di checkpoint interaktif. Files: `Controllers/AssessmentAdminController.cs` (ReshuffleAll loop). Verification: build 0 err + 391/391; normal-exam path unchanged (satu key). Commit `924d7122`.

**Total deviations:** 1 (correctness completion, user-approved). **Impact:** WSE-04 kini tuntas penuh di StartExam + ReshufflePackage + ReshuffleAll.

## Notes

- **Dev-server lock detour:** `dotnet test`/build awal gagal karena `HcPortal.exe` (PID 3144, via `dotnet run` PID 25444) — dev server leftover dari 380 UAT — mengunci binary. User approve stop; proses dihentikan; build lanjut. Pastikan tak menjalankan app saat eksekusi/test 381.
- **Single-writer:** 380 sudah COMPLETE (secure 7/7 threats_open:0, UAT 5/5) → sesi ini sole writer; STATE/ROADMAP di-advance normal (tak ada concurrent race seperti 380-01).

## Next

Ready for Plan 381-02 (WSE-05 — guard ×3 write-site impersonasi + preview in-memory). Wave 2 — depends 381-01 (file-overlap `CMPController.cs`); eksekusi sequential.

## Self-Check: PASSED
- key-files exist on disk: ✓ (SiblingSessionQuery.cs, SiblingPrePostFilterTests.cs, SiblingDeterminismTests.cs, CMPController.cs, AssessmentAdminController.cs)
- `git log --grep="381-01"` returns 3 commits: ✓ (cec93f6f, c2c3a803, 924d7122)
- All acceptance criteria re-run green: ✓
- Plan-level verification (build 0 err, xUnit 391/391, no migration, D-08 honored): ✓
