---
phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja
plan: 02
subsystem: assessment-retake
tags: [retake, worker-self-service, csrf, idor, tier-feedback, riwayat, controller, csharp, integration-test]

# Dependency graph
requires:
  - phase: 405-backend-core
    provides: "RetakeService.ExecuteAsync/CanRetakeAsync (AddScoped Program.cs:63), AssessmentAttemptResponseArchive, RetakeArchiveBuilder, RiwayatUnifier"
  - phase: 407-01
    provides: "RetakeReviewMode enum + RetakeRules.ResolveReviewMode (leak-safe A1), AssessmentResultsViewModel +7 retake/tier field, AllWorkersHistoryRow.IsCurrentAttempt"
provides:
  - "POST CMP/RetakeExam(id) — worker self-service retake: [ValidateAntiForgeryToken] + ownership Forbid (IDOR) + server-authoritative CanRetakeAsync re-check + ExecuteAsync + TempData token-clear + redirect StartExam"
  - "CMPController DI RetakeService (constructor)"
  - "CMPController.Results populate 7 VM field (RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached/RiwayatAttempts) + riwayat load — server-authoritative tier/eligibility"
  - "RetakeExamEndpointTests — 3 kasus RTK-09 (non-owner Forbid / not-eligible redirect / sukses token-cleared)"
affects: [407-03, 408]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Server-authoritative retake gate: countdown/disable JS hanya UX, server re-cek CanRetakeAsync SEBELUM ExecuteAsync (D-01)"
    - "Tier/eligibility dihitung di controller (server) lalu di-inject ke VM; view (407-03) hanya merender — leak-safety = keputusan server"
    - "Controller endpoint test via FakeUserStore (IUserStore + IUserRoleStore) + StubSession + real-SQL RetakeServiceFixture — uji 3-state actionresult tanpa full DI stack"

key-files:
  created:
    - "HcPortal.Tests/RetakeExamEndpointTests.cs"
  modified:
    - "Controllers/CMPController.cs"

key-decisions:
  - "RetakeExam cermin baris-per-baris HC ResetAssessment (AssessmentAdminController :4244-4327); beda: actor=worker (effective user impersonation-aware), guard=ownership Forbid + CanRetakeAsync (bukan IsResettable/Pre-Post), redirect=StartExam (HC ke Monitoring)"
  - "Pitfall 5 dipatuhi: tier ResolveReviewMode pakai assessment.IsPassed (bool?) BUKAN viewModel.IsPassed (bool) — pending(null) terjaga leak-safe"
  - "attemptsRemaining utk tier sertakan AllowRetake + ShouldHideRetakeToggle (bukan hanya currentAttempt<MaxAttempts) → assessment non-retake / PreTest / Manual → kunci tetap tampil (ShowFullReview), cegah regresi review"
  - "Test Opsi A (controller unit atas real-SQL fixture) dipilih atas Opsi B — deterministik, uji 3-state actionresult langsung; FakeUserStore WAJIB implement IUserRoleStore (GetCurrentUserRoleLevelAsync panggil GetRolesAsync setelah GetUserAsync)"

patterns-established:
  - "Worker write-path retake = ownership Forbid SEBELUM CanRetakeAsync SEBELUM ExecuteAsync SEBELUM token-clear SEBELUM redirect (urutan load-bearing, diuji 3 kasus)"

requirements-completed: [RTK-09, RTK-10, RTK-12, RTK-13]

# Metrics
duration: 12min
completed: 2026-06-22
---

# Phase 407 Plan 02: Worker Self-Service RetakeExam + Results VM Populate Summary

**Action POST `CMP/RetakeExam(id)` worker self-service (antiforgery + ownership Forbid IDOR + server-authoritative `CanRetakeAsync` re-check + `ExecuteAsync` + token-clear + redirect StartExam, cermin HC `ResetAssessment`) + DI `RetakeService` ke `CMPController` + `Results` mengisi 7 VM field retake/tier (tier via `assessment.IsPassed` bool? — Pitfall 5) + riwayat pekerja via `RiwayatUnifier`, dikunci 3 endpoint test RTK-09. 0 migration.**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-06-22T02:16:39Z
- **Completed:** 2026-06-22T02:28:15Z
- **Tasks:** 3
- **Files modified:** 1 (CMPController.cs); **created:** 1 (RetakeExamEndpointTests.cs)

## Accomplishments
- **DI inject `RetakeService`** ke `CMPController` (field + ctor param ke-15 + assign, mirror triple `_gradingService`/`_impersonationService`). TANPA registrasi DI baru (sudah `AddScoped` Program.cs:63).
- **Action POST `RetakeExam(int id)`** (RTK-09) — ditempatkan tepat setelah `Results`, sebelum `#region Helper Methods`:
  - `[HttpPost]` + `[ValidateAntiForgeryToken]` (CSRF, T-407-csrf) — class-level `[Authorize]` sudah ada.
  - Ownership Forbid: `if (assessment.UserId != user.Id) return Forbid();` SEBELUM mutasi (IDOR, T-407-idor). Effective user via `GetCurrentUserRoleLevelAsync()` (impersonation-aware).
  - Server-authoritative re-check `if (!await _retakeService.CanRetakeAsync(id))` → not-eligible redirect Results + `TempData["Error"]` (T-407-bypass; countdown JS non-authoritative).
  - `_retakeService.ExecuteAsync(id, user.Id, actorName, "RetakeAssessment", "worker_retake")` (actorName format mirror :4298).
  - `TempData.Remove($"TokenVerified_{id}")` (must-fix #1, T-407-token) → `RedirectToAction("StartExam", new { id })`.
- **`CMPController.Results` perluas 7 VM field + riwayat load** (RTK-10/12/13) — disisip sebelum `return View(viewModel)`:
  - Counting `eraRetakeArchives` mirror `CanRetakeAsync` :237-242 → `currentAttempt`.
  - `attemptsRemaining` = `AllowRetake && !ShouldHideRetakeToggle(...) && currentAttempt < MaxAttempts` (non-retake aman → ShowFullReview).
  - `viewModel.RetakeMode = RetakeRules.ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining)` — **Pitfall 5** pakai `assessment.IsPassed` (bool?).
  - `CanRetake`/`CurrentAttempt`/`MaxAttempts`/`CooldownUntilUtc`/`IsCapReached` di-set; `RiwayatAttempts` via `RiwayatUnifier.Build(...)` + `RetakeArchiveBuilder.Build(0,...)` (cermin RiwayatPercobaan :3493-3522).
  - Branch `AllowAnswerReview` yang membangun `QuestionReviews` **UTUH** (tier ShowWrongFlagsOnly reuse list yang sama).
- **`RetakeExamEndpointTests.cs`** (Wave-0 gap RTK-09) — 3 kasus hijau atas real-SQL fixture.

## Task Commits

Each task committed atomically:

1. **Task 1: DI RetakeService + action RetakeExam** - `f77d7710` (feat) — build 0 error.
2. **Task 2: Results populate 7 VM field + riwayat load** - `120d6286` (feat) — build 0 error.
3. **Task 3: RetakeExamEndpointTests 3 kasus RTK-09** - `af19a643` (test) — 3/3 hijau (real-SQL @SQLEXPRESS).

## Files Created/Modified
- `Controllers/CMPController.cs` — DI `RetakeService` (field/ctor/assign); action `RetakeExam` (41 baris); `Results` extend (7 VM field + riwayat load, 44 baris). Branch QuestionReviews + semua logika existing UTUH.
- `HcPortal.Tests/RetakeExamEndpointTests.cs` — NEW. `FakeUserStore` (IUserStore + IUserRoleStore empty-roles) + `StubSession` (no impersonation → UseRealUser) + `MakeController` (real ctx/userManager/impersonation/retakeService, deps lain null!-substitute) + reuse `RetakeServiceFixture`/`NoOpHubContext` dari RetakeServiceTests. 3 `[Fact]` `[Trait Category Integration]`.

## Decisions Made
- **RetakeExam cermin ResetAssessment** (lihat frontmatter key-decisions). Beda terkontrol: actor worker, ownership Forbid, CanRetakeAsync re-check, redirect StartExam.
- **Pitfall 5** dipatuhi verbatim — tier pakai `assessment.IsPassed` (bool?), bukan `viewModel.IsPassed` (bool non-nullable).
- **attemptsRemaining tier-aware non-retake** — sertakan `AllowRetake` + `ShouldHideRetakeToggle` agar assessment non-retake/PreTest/Manual tetap ShowFullReview (cegah regresi tampilan kunci).
- **Test Opsi A** — controller unit test atas real-SQL fixture (deterministik 3-state ActionResult). `FakeUserStore` WAJIB `IUserRoleStore` karena `GetCurrentUserRoleLevelAsync` memanggil `GetRolesAsync` setelah `GetUserAsync` (UseRealUser path).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] FakeUserStore harus implement IUserRoleStore<ApplicationUser>**
- **Found during:** Task 3 (test run pertama)
- **Issue:** `GetCurrentUserRoleLevelAsync` (UseRealUser path) memanggil `_userManager.GetRolesAsync(real)` SETELAH `GetUserAsync` → `UserManager.GetUserRoleStore()` melempar `NotSupportedException: Store does not implement IUserRoleStore<TUser>`. Test gagal 3/3 sebelum mencapai assertion.
- **Fix:** `FakeUserStore` ditambah implement `IUserRoleStore<ApplicationUser>` (5 method; `GetRolesAsync` kembalikan list kosong → role level 0; RetakeExam tak gating by role level).
- **Files modified:** `HcPortal.Tests/RetakeExamEndpointTests.cs`
- **Commit:** `af19a643` (sudah inline sebelum commit Task 3)

_Catatan: fix berada di file test baru yang belum di-commit saat ditemukan, jadi langsung diperbaiki sebelum commit Task 3 (1 commit, bukan 2). 1 fix attempt, di bawah limit 3._

## Authentication Gates
None — endpoint test pakai real-SQL fixture lokal (@localhost\SQLEXPRESS sudah tersedia, sama dgn RetakeServiceTests).

## Issues Encountered
- Working tree memuat file untracked pre-existing (`akun-doc-*.jpeg`, `docs/akun-multirole-multiunit/`, xlsx, `docs/SEED_JOURNAL.md` changes) dari sesi sebelumnya. TIDAK distage — di luar scope plan ini (hanya 2 file plan di-commit per task).

## User Setup Required
None.

## Threat Model Compliance
Semua disposition `mitigate` di plan `<threat_model>` ter-implement + diuji:
- **T-407-idor** (ownership): `assessment.UserId != user.Id → Forbid()` SEBELUM mutasi (Task 1); diuji `RetakeExam_NonOwner_ReturnsForbid` (sesi tetap Completed).
- **T-407-csrf**: `[ValidateAntiForgeryToken]` (Task 1; form modal @Html.AntiForgeryToken di 407-03).
- **T-407-bypass** (cooldown/cap): server `CanRetakeAsync` re-cek SEBELUM ExecuteAsync; diuji `RetakeExam_NotEligible_RedirectsToResultsWithError` (LULUS → false → redirect + Error + no mutasi).
- **T-407-token**: `TempData.Remove($"TokenVerified_{id}")` setelah ExecuteAsync sukses; diuji `RetakeExam_Success_ClearsTokenAndRedirectsToStartExam` (token TERHAPUS + redirect StartExam + sesi→Open).
- **T-407-doublearchive** (accept): di-handle ExecuteAsync claim-atomik (service 405) — endpoint hanya memanggil.
- **T-407-leak** (read-path): tier `ResolveReviewMode` dihitung server pakai `assessment.IsPassed` bool? → inject ke VM; view (407-03) men-suppress leak-site.

## Threat Flags
None — `RetakeExam` POST endpoint sudah dimodelkan di plan `<threat_model>` (4 mitigate + 1 accept + 1 read-path). Tidak ada surface baru di luar threat register.

## Verification
- `dotnet build HcPortal.csproj` — **0 error** (24 warning pre-existing di file unrelated, out-of-scope).
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RetakeExam"` — **3/3 hijau** (RTK-09).
- `dotnet test HcPortal.Tests --filter "Category!=Integration"` — **448/0/2** (2 skip SQLEXPRESS-gated; no regresi, baseline 407-01 identik).

## Next Phase Readiness
- **407-03 (view):** `Results.cshtml` `@switch (Model.RetakeMode)` (ShowFullReview/ShowWrongFlagsOnly/ShowScoreOnly) untuk suppress leak-site `:366/:367/:386-389/:403` di ShowWrongFlagsOnly + retake control block (btnRetake + countdown `data-cooldown-until` + IsCapReached lock) + confirmation modal POST ke `RetakeExam` (+`@Html.AntiForgeryToken()`) + `_RiwayatPekerja.cshtml` partial render `Model.RiwayatAttempts`. Semua 7 VM field + RiwayatAttempts sudah terisi server.
- **0 migration** plan ini (controller + test murni). migration v32.4 satu-satunya tetap di 405-01 (`AddRetakeColumnsAndArchive`).
- No blockers.

## Self-Check: PASSED

- `Controllers/CMPController.cs` — FOUND (modified).
- `HcPortal.Tests/RetakeExamEndpointTests.cs` — FOUND (created).
- Commits `f77d7710`, `120d6286`, `af19a643` — all FOUND in git log.

---
*Phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja*
*Completed: 2026-06-22*
