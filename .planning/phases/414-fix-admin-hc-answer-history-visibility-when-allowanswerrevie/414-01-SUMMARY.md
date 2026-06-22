---
phase: 414-fix-admin-hc-answer-history-visibility-when-allowanswerrevie
plan: 01
subsystem: authorization
tags: [aspnet-core-mvc, view-gating, owner-vs-non-owner, pure-static-helper, xunit, assessment-results]

# Dependency graph
requires:
  - phase: 346-...
    provides: "Pola pure static helper IsResultsAuthorized (cetakan) + ResultsAuthorizationTests"
  - phase: 409-...
    provides: "Pola pure static helper IsParticipantRemoved (cetakan kedua)"
provides:
  - "Field VM efektif CanReviewAnswers (raw AllowAnswerReview tetap utuh)"
  - "Pure static helper CMPController.CanReviewAnswers(bool,bool) testable no-DB"
  - "Gate per-soal Results decoupled dari toggle by owner-vs-non-owner"
  - "Nota admin (XSS-safe) saat review tampil karena bypass non-owner"
affects: [DEF-01 share-URL hasil ke atasan lintas-section, EssayGrading review delegation]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Pure static gate helper (allowAnswerReview || !isOwner) — no-DB unit-testable"
    - "VM-flag dua-field (raw + efektif) → view membedakan OFF-tapi-admin-lihat untuk nota"

key-files:
  created:
    - "HcPortal.Tests/CanReviewAnswersTests.cs"
  modified:
    - "Models/AssessmentResultsViewModel.cs"
    - "Controllers/CMPController.cs"
    - "Views/CMP/Results.cshtml"

key-decisions:
  - "D-01: tambah field VM CanReviewAnswers; AllowAnswerReview tetap raw toggle (pembeda nota admin)"
  - "D-02: bypass semua non-owner (owner-check tunggal cukup, sudah lolos IsResultsAuthorized)"
  - "D-03: pure static helper CanReviewAnswers(allow,isOwner) => allow || !isOwner; hitung sekali pakai dua kali"
  - "D-04: nota admin diturunkan di view dari CanReviewAnswers && !AllowAnswerReview (tanpa field VM ke-3)"
  - "OQ-1: legacy/empty path hardcode CanReviewAnswers=false (tak ada QuestionReviews untuk ditinjau)"

patterns-established:
  - "Gate desync guard: gate build + VM flag WAJIB pakai satu variabel canReviewAnswers (Pitfall 1)"
  - "Nota XSS-safe: teks Bahasa Indonesia static Razor encoded, no @Html.Raw di blok baru (T-414-02)"

requirements-completed: []

# Metrics
duration: 10min
completed: 2026-06-22
---

# Phase 414 Plan 01: Fix Visibilitas History Jawaban Admin/HC saat AllowAnswerReview OFF Summary

**Decouple gate "Tinjauan Jawaban" per-soal di CMP/Results dari toggle AllowAnswerReview by owner-vs-non-owner — Admin/HC (non-owner yang sudah lolos IsResultsAuthorized) selalu lihat review; peserta (owner) tetap di-gate toggle, lewat pure static helper CanReviewAnswers + field VM efektif + nota admin XSS-safe.**

## Performance

- **Duration:** ~10 min (565 detik)
- **Started:** 2026-06-22T01:26:32Z
- **Completed:** 2026-06-22T01:35:57Z
- **Tasks:** 4
- **Files modified:** 3 modified + 1 created

## Accomplishments
- Field VM `CanReviewAnswers` (nilai efektif) ditambah ke `AssessmentResultsViewModel`; `AllowAnswerReview` raw toggle tetap utuh (D-01).
- Pure static helper `CMPController.CanReviewAnswers(bool allowAnswerReview, bool isOwner) => allowAnswerReview || !isOwner` (pola `IsResultsAuthorized`/`IsParticipantRemoved`), dihitung SEKALI pasca-auth dan dipakai DUA kali (gate build L2266 + VM flag) — anti-desync (Pitfall 1).
- View gate render + alert pindah ke `Model.CanReviewAnswers`; nota admin (Bahasa Indonesia, static encoded, no `@Html.Raw`) tampil hanya saat `CanReviewAnswers && !AllowAnswerReview` (D-04).
- Unit test xUnit 4-InlineData pure static (no-DB) mengunci matrix owner-vs-non-owner × toggle (SC-1/SC-2/SC-3).
- `IsResultsAuthorized` + `Forbid()` gerbang akses utuh (T-414-01); net access surface tak berubah.

## Task Commits

Each task was committed atomically:

1. **Task 1: Tambah field VM efektif CanReviewAnswers** — `fe0fc15c` (feat)
2. **Task 2: Pure static helper CanReviewAnswers + wiring action Results** — `572df8a4` (feat)
3. **Task 3: View gate pakai CanReviewAnswers + nota admin (XSS-safe)** — `e5dd9272` (feat)
4. **Task 4: Unit test xUnit pure static CanReviewAnswers (no-DB)** — `b71dc985` (test)

**Plan metadata:** _(metadata commit — docs: complete plan)_

_Catatan TDD: helper produksi (Task 2) mendahului test (Task 4) karena helper pure-static = regression lock; test 4/4 GREEN langsung tanpa fase RED terpisah (pola ResultsAuthorizationTests, deterministik no-DB)._

## Files Created/Modified
- `Models/AssessmentResultsViewModel.cs` — tambah `public bool CanReviewAnswers { get; set; }` setelah `AllowAnswerReview` (raw tak terhapus).
- `Controllers/CMPController.cs` — helper static `CanReviewAnswers`; hitung `isOwner`+`canReviewAnswers` sekali pasca-auth; gate build `if (canReviewAnswers)`; VM package-path `CanReviewAnswers=canReviewAnswers`; VM legacy-path `CanReviewAnswers=false`.
- `Views/CMP/Results.cshtml` — gate L316 → `Model.CanReviewAnswers`; alert L413 → `!Model.CanReviewAnswers`; nota admin baru (`CanReviewAnswers && !AllowAnswerReview`).
- `HcPortal.Tests/CanReviewAnswersTests.cs` — (baru) xUnit `[Theory]` 4 InlineData pure static.

## Decisions Made
- Mengikuti D-01..D-04 + OQ-1 (rekomendasi RESEARCH) persis seperti plan. Legacy/empty path hardcode `CanReviewAnswers=false` (tak ada `QuestionReviews` untuk ditinjau; mencegah nota admin keliru pada sesi kosong).
- Helper diletakkan tepat setelah `IsParticipantRemoved` (L2540), nota admin sebagai `alert-info` di dalam card "Tinjauan Jawaban" (Claude's Discretion warna/penempatan).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None. Line numbers sedikit bergeser dari CONTEXT (sesuai catatan RESEARCH ±0-2 baris); semua titik edit dilokasikan via string kode eksak, bukan nomor baris. Git warning LF→CRLF pada file test baru (kosmetik, normal di Windows).

## Verification Results

- **dotnet build:** Build succeeded, **0 error** (per task + final).
- **dotnet test (filter ~CanReviewAnswers):** **4/4 Passed** (Failed 0, Duration 219 ms).
- **dotnet test (full suite):** **609/609 Passed** (Failed 0) = baseline 605 + 4 test baru, **0 regresi, tak ada test hilang**.
- **dotnet run @ localhost:5277 (AD-off):** "Now listening" + "Application started"; GET / **HTTP 200**.
- **migration:** `git status Migrations/ Data/` kosong → **migration=FALSE**.
- **Grep guard:** `if (assessment.AllowAnswerReview)` = **0** di CMPController.cs (gate lama hilang); `Model.AllowAnswerReview` di Results.cshtml hanya tersisa **1** (kondisi nota D-04, bukan gate); `@Html.Raw` di blok nota baru = **0** (2 occurrence pre-existing di chart L272-273, out of scope).

## Known Stubs
None. Tidak ada hardcoded empty value yang mengalir ke UI, tidak ada placeholder/TODO, dan `CanReviewAnswers` selalu di-set dari variabel terhitung (package path) atau `false` deliberate (legacy path, sesuai OQ-1).

## Security Notes
- T-414-01 (Broken Access Control): `IsResultsAuthorized` + `Forbid()` **TETAP** dijalankan tanpa perubahan SEBELUM helper baru (3 occurrence Forbid utuh). `CanReviewAnswers` hanya melonggarkan DISPLAY review untuk non-owner yang SUDAH berwenang. Net access surface tak berubah.
- T-414-02 (XSS): nota admin = teks Bahasa Indonesia static Razor encoded, NO interpolasi data user, NO `@Html.Raw` di blok baru.
- T-414-03 (Spoofing impersonation): `isOwner` dihitung dari `user` efektif (impersonation-aware `GetCurrentUserRoleLevelAsync`), konsisten `IsResultsAuthorized` (Pitfall 4 — JANGAN `_userManager.GetUserAsync(User)` lagi).

## User Setup Required
None - no external service configuration required.

## Manual UAT (gate terpisah — lesson 354, BELUM dijalankan di plan ini)
Razor `@if` butuh verifikasi runtime browser (bukan grep/build). Gate autopilot/verify-work dua-persona di `http://localhost:5277`:
- **SC-1:** Login admin (non-owner) → View Results peserta `AllowAnswerReview=false` → section "Tinjauan Jawaban" TAMPIL + nota admin tampil.
- **SC-2:** Login worker pemilik (owner) → hasil sendiri toggle OFF → alert "Tinjauan jawaban tidak tersedia untuk assessment ini.".
- **SC-3:** Toggle ON → admin & owner lihat review tanpa nota (regresi nol).
- Seed UAT (bila perlu): assessment package `AllowAnswerReview=false` + responses, klasifikasi temporary+local-only, snapshot+restore (CLAUDE.md SEED_WORKFLOW).

## Next Phase Readiness
- Plan 414-01 (1 of 1) selesai; semua gate teknis lokal terpenuhi.
- **NOT pushed** (branch main). Reminder notify IT: commit `b71dc985` (HEAD plan) — **migration=FALSE** (deploy Dev/Prod = tanggung jawab IT, CLAUDE.md step 4-5).
- Off-theme bugfix v32.5; TIDAK menambah REQ ke akuntansi 11/11.
- DEF-01 (share-URL hasil ke atasan lintas-section + review essay) tetap deferred → discuss/spec terpisah.

## Self-Check: PASSED

- Files: all 5 FOUND (3 modified + 1 created + SUMMARY).
- Commits: all 4 FOUND (fe0fc15c, 572df8a4, e5dd9272, b71dc985).

---
*Phase: 414-fix-admin-hc-answer-history-visibility-when-allowanswerrevie*
*Completed: 2026-06-22*
