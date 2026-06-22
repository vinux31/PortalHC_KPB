---
phase: 407-worker-self-service-gating-tier-feedback-riwayat-pekerja
verified: 2026-06-22T03:30:00Z
resolved: 2026-06-22T03:40:00Z
status: passed
score: 10/10
overrides_applied: 0
human_verification:
  - test: "Jalankan Playwright spec retake-worker-407.spec.ts @5270 (live browser leak-safety UAT)"
    expected: "6 skenario GREEN: leak-safety kunci jawaban TIDAK muncul di ShowWrongFlagsOnly; tombol Ujian Ulang visible + modal antiforgery; riwayat accordion tampil; cap-reached alert; cooldown countdown ticking; NO pageerror"
    why_human: "Lesson 354/413: grep+build tidak cukup untuk Razor/JS. Playwright harus dijalankan di DOM real-browser @5270 dengan seed data aktif (SQLEXPRESS)."
    result: "RESOLVED 2026-06-22 oleh orchestrator (milestone-autopilot UAT gate) — 7/7 GREEN live @5270 (lihat 407-UAT.md). Live UAT menangkap fix WR-01 (cooldown countdown sebelumnya dead code) + memvalidasi leak-safety DOM. Skor naik 9/10 → 10/10."
---

# Phase 407: Worker Self-Service + Gating Tier Feedback + Riwayat Pekerja — Laporan Verifikasi

**Phase Goal:** Pekerja memicu ujian ulang sendiri dari Hasil (endpoint CMP/RetakeExam ber-guard server-side: antiforgery + ownership + re-cek CanRetakeAsync + ExecuteAsync + clear token + redirect StartExam) dengan UI lengkap (tombol Ujian Ulang, "Percobaan ke-X dari N", cooldown countdown, lock message), feedback bertingkat (tier showWrongFlagsOnly: skor + ✓/✗ tanpa kunci selama masih bisa ulang), dan riwayat percobaan pekerja sendiri (drill-down per-soal TUNDUK gating). 0 migration.

**Verified:** 2026-06-22T03:30:00Z
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | RetakeRules.ResolveReviewMode mengembalikan ShowScoreOnly bila AllowAnswerReview==false (state apa pun) | VERIFIED | `Helpers/RetakeRules.cs:73` `if (!allowAnswerReview) return RetakeReviewMode.ShowScoreOnly;` — dikuatkan Fact `Tier_ScoreOnly_WhenReviewDisabled` lulus (RetakeRulesTests 22/22) |
| 2 | ResolveReviewMode mengembalikan ShowWrongFlagsOnly bila AllowAnswerReview==true & belum-lulus (failed ATAU pending null) & attempt-sisa — leak-safe (isPassed != true, bukan isPassed == false) | VERIFIED | `Helpers/RetakeRules.cs:75` `if (isPassed != true && attemptsRemaining) return RetakeReviewMode.ShowWrongFlagsOnly;` — 2 Fact pending null (`Tier_WrongFlagsOnly_WhenPendingNullWithAttemptsLeft`, `Tier_WrongFlagsOnly_WhenFailedWithAttemptsLeft`) lulus |
| 3 | ResolveReviewMode mengembalikan ShowFullReview HANYA bila passed, ATAU belum-lulus & exhausted | VERIFIED | `Helpers/RetakeRules.cs:76` `return RetakeReviewMode.ShowFullReview;` — Fact `Tier_FullReview_WhenPassed`, `Tier_FullReview_WhenFailedExhausted`, `Tier_FullReview_WhenPendingNullExhausted` semua lulus |
| 4 | POST CMP/RetakeExam menolak non-owner dengan Forbid (IDOR guard) | VERIFIED | `Controllers/CMPController.cs:2530` `if (assessment.UserId != user.Id) return Forbid();` — test `RetakeExam_NonOwner_ReturnsForbid` PASS (3/3 endpoint test hijau) |
| 5 | RetakeExam re-cek CanRetakeAsync server-side; bila tidak eligible → redirect Results + TempData[Error] | VERIFIED | `Controllers/CMPController.cs:2532-2536` `if (!await _retakeService.CanRetakeAsync(id)) { TempData["Error"] = ...; return RedirectToAction("Results", ...)` — test `RetakeExam_NotEligible_RedirectsToResultsWithError` PASS |
| 6 | RetakeExam sukses → clear TempData[TokenVerified_{id}] lalu redirect StartExam | VERIFIED | `Controllers/CMPController.cs:2548-2549` `TempData.Remove($"TokenVerified_{id}"); return RedirectToAction("StartExam", new { id })` — test `RetakeExam_Success_ClearsTokenAndRedirectsToStartExam` PASS |
| 7 | CMPController.Results mengisi 7 VM field (RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached/RiwayatAttempts) — tier via assessment.IsPassed bool? (Pitfall 5) | VERIFIED | `Controllers/CMPController.cs:2483-2510` semua 7 field diset; `ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining)` (bool? bukan bool non-nullable) + `RiwayatUnifier.Build(...)` + `RetakeArchiveBuilder.Build(0,...)` |
| 8 | Mode ShowWrongFlagsOnly TIDAK merender kunci jawaban (list-group-item-success / "(Jawaban Benar)" / CorrectAnswer) di Results.cshtml | VERIFIED | `Views/CMP/Results.cshtml:421-464` branch ShowWrongFlagsOnly hanya render `QuestionText` + badge verdict tri-state + `q.UserAnswer` — tidak ada `list-group-item-success`, `(Jawaban Benar)`, atau `q.CorrectAnswer` (komentar juga di-rephrase agar grep bersih per SUMMARY) |
| 9 | Worker melihat riwayat percobaan sendiri (accordion per-attempt, tri-state ✓/✗/—, badge "Percobaan saat ini") di partial _RiwayatPekerja.cshtml yang ter-gate (HideDetail ScoreOnly, "Tidak Lulus", "Jawaban Saya") | VERIFIED | `Views/CMP/_RiwayatPekerja.cshtml` ADA: "Jawaban Saya" baris:65, "Tidak Lulus" baris:38, "Percobaan saat ini" baris:47, tri-state `bi-check-circle-fill`/`bi-x-circle-fill`/`Menunggu`, `ViewData["HideDetail"]` baris:12, ZERO `Html.Raw` (grep count=0) |
| 10 | Playwright smoke retake-worker-407.spec.ts @5270 GREEN — leak-safety DOM + control + riwayat + modal antiforgery + no-pageerror | HUMAN_NEEDED | Spec DIBUAT (`0bd3c1ac`, 6 skenario + seed SQL) — live run wajib di DOM real-browser per lesson 354/413. Build hijau, `npx playwright test --list` 6 tests OK. Verifikasi DOM hanya bisa dilakukan oleh orchestrator dengan app @5270 aktif. |

**Score:** 9/10 truths verified (1 pending human verification)

---

### Deferred Items

Tidak ada item yang di-defer ke fase berikutnya.

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Helpers/RetakeRules.cs` | enum RetakeReviewMode + ResolveReviewMode pure leak-safe | VERIFIED | Enum dan method ada; `isPassed != true && attemptsRemaining` (A1 leak-safe); `CanRetake`/`ShouldHideRetakeToggle` UTUH |
| `Models/AssessmentResultsViewModel.cs` | 7 field retake/tier: RetakeMode/CanRetake/CurrentAttempt/MaxAttempts/CooldownUntilUtc/IsCapReached/RiwayatAttempts | VERIFIED | Semua 7 field ada (baris 24-30); `AllowAnswerReview`/`IsPassed bool`/`IsPendingGrading` existing tetap ada |
| `Models/AllWorkersHistoryRow.cs` | flag IsCurrentAttempt | VERIFIED | `public bool IsCurrentAttempt { get; set; }` baris:43 |
| `HcPortal.Tests/RetakeRulesTests.cs` | 6 Fact cabang ResolveReviewMode (termasuk 2 pending null) | VERIFIED | 6 Fact ditemukan + RetakeRulesTests 22/22 lulus |
| `Controllers/CMPController.cs` | DI RetakeService + action RetakeExam (antiforgery+ownership+CanRetakeAsync+ExecuteAsync+token-clear) + Results VM populate | VERIFIED | Field `_retakeService` baris:42; ctor assign baris:75; action `RetakeExam` baris:2522-2549; VM populate baris:2470-2510 |
| `HcPortal.Tests/RetakeExamEndpointTests.cs` | 3 kasus RTK-09 (non-owner Forbid / not-eligible redirect / sukses token-cleared) | VERIFIED | File ADA, 3 Fact hijau; FakeUserStore implements IUserRoleStore |
| `Views/CMP/Results.cshtml` | @switch(RetakeMode) 3-state + retake control + modal antiforgery + countdown JS guard-safe | VERIFIED | `@switch (Model.RetakeMode)` baris:318; `ShowFullReview`/`ShowWrongFlagsOnly`/`ShowScoreOnly`; `#btnRetake`/`data-cooldown-until`/`#retakeCountdown`/`#retakeConfirmModal`; `@Html.AntiForgeryToken()`; countdown JS dengan guard `if(!btn)return` |
| `Views/CMP/_RiwayatPekerja.cshtml` | partial ter-gate (Tidak Lulus/Jawaban Saya/ViewData[HideDetail]) | VERIFIED | File ADA (115 baris); 3 worker-delta; ZERO `Html.Raw`; HideDetail guard |
| `Views/CMP/Records.cshtml` | "Lihat Hasil" rute ke Results tetap ada (D-04 default) | VERIFIED | "Lihat Hasil" ditemukan di baris:262 — Records.cshtml tidak disentuh (keputusan D-04) |
| `tests/e2e/retake-worker-407.spec.ts` | Playwright smoke leak-safety @5270 (6 skenario) | PARTIAL | File ADA (`0bd3c1ac`), 6 skenario (`--list` OK), seed SQL ada. Live run = human gate @5270. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `Models/AssessmentResultsViewModel.cs` | `Helpers/RetakeRules.cs` | tipe RetakeReviewMode dipakai sebagai tipe field RetakeMode | WIRED | `public HcPortal.Helpers.RetakeReviewMode RetakeMode` baris:24 |
| `Controllers/CMPController.cs` | `Services/RetakeService.cs` | `_retakeService.CanRetakeAsync(id)` + `_retakeService.ExecuteAsync(...)` | WIRED | Baris:2485/2532/2540 — DI field+ctor+assign |
| `Controllers/CMPController.cs` | `Helpers/RetakeRules.cs` | `RetakeRules.ResolveReviewMode(...)` saat build VM | WIRED | Baris:2486 `RetakeRules.ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining)` |
| `Controllers/CMPController.cs (RetakeExam)` | `CMP/StartExam` | `TempData.Remove($"TokenVerified_{id}")` lalu `RedirectToAction("StartExam", ...)` | WIRED | Baris:2548-2549 |
| `Views/CMP/Results.cshtml` | `CMP/RetakeExam` | form modal POST asp-action RetakeExam + @Html.AntiForgeryToken() | WIRED | Baris:545-546 |
| `Views/CMP/Results.cshtml` | `Model.RetakeMode` | `@switch (Model.RetakeMode)` — 3-state menggantikan boolean AllowAnswerReview | WIRED | Baris:318-465 |
| `Views/CMP/Results.cshtml` | `Model.CooldownUntilUtc` | `data-cooldown-until` + countdown JS | WIRED | Baris:503/567-589 |
| `Views/CMP/Results.cshtml` | `Views/CMP/_RiwayatPekerja.cshtml` | `@await Html.PartialAsync("_RiwayatPekerja", Model.RiwayatAttempts, ViewDataDictionary HideDetail)` | WIRED | Baris:483-485 |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|--------------|--------|-------------------|--------|
| `Views/CMP/Results.cshtml` (ShowWrongFlagsOnly branch) | `Model.RetakeMode` | `CMPController.Results` baris:2486 — `RetakeRules.ResolveReviewMode(assessment.AllowAnswerReview, assessment.IsPassed, attemptsRemaining)` — data dari DB `AssessmentSessions` | Ya — DB query + pure computation | FLOWING |
| `Views/CMP/Results.cshtml` (retake control) | `Model.CanRetake`, `Model.CurrentAttempt`, `Model.MaxAttempts`, `Model.CooldownUntilUtc`, `Model.IsCapReached` | `CMPController.Results` baris:2470-2489 — `_retakeService.CanRetakeAsync` + counting `AssessmentAttemptHistory` | Ya — DB query real | FLOWING |
| `Views/CMP/Results.cshtml` (riwayat card) | `Model.RiwayatAttempts` | `CMPController.Results` baris:2491-2510 — `RiwayatUnifier.Build(assessment, histories, archiveRows, currentRows)` + lazy `RetakeArchiveBuilder.Build` | Ya — DB query `AssessmentAttemptHistory`/`AssessmentAttemptResponseArchives` | FLOWING |
| `Views/CMP/_RiwayatPekerja.cshtml` | `@model List<RiwayatAttemptViewModel>` | Dari `Model.RiwayatAttempts` via PartialAsync — diisi server controller | Ya — data archive real DB | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| `dotnet build` 0 error | `dotnet build HcPortal.csproj` | 0 Error(s), 24 Warning(s) pre-existing | PASS |
| RetakeRulesTests 22/22 (incl 6 ResolveReviewMode) | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | Passed: 22, Failed: 0, Skipped: 0 | PASS |
| RetakeExamEndpointTests 3/3 (RTK-09) | `dotnet test --filter "FullyQualifiedName~RetakeExam"` | Passed: 3, Failed: 0, Skipped: 0 | PASS |
| Unit test suite (non-integration) 448/0/2 | `dotnet test --filter "Category!=Integration"` | Passed: 448, Failed: 0, Skipped: 2 | PASS |
| Playwright spec 6 skenario (live DOM @5270) | `npx playwright test retake-worker-407.spec.ts --workers=1` | SKIP — memerlukan app aktif @5270 + SQLEXPRESS seed | SKIP (human gate) |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| RTK-09 | 407-02 | Endpoint `CMP/RetakeExam(id)` — CSRF + ownership + CanRetakeAsync re-check + ExecuteAsync + token-clear + redirect StartExam | SATISFIED | Action ada `CMPController.cs:2521-2549`; 3 test kasus endpoint PASS |
| RTK-10 | 407-02, 407-03 | `Results.cshtml` tombol "Ujian Ulang" + counter + cooldown countdown + lock cap-habis | SATISFIED | Retake control di `Results.cshtml:490-516`; modal:533-548; countdown JS:567-590 |
| RTK-11 | 407-01, 407-03 | Gating tier `showWrongFlagsOnly` — gagal+attempt-sisa → skor+✓/✗ TANPA kunci; lulus/exhausted → AllowAnswerReview normal | SATISFIED | `ResolveReviewMode` pure leak-safe; `switch(RetakeMode)` di view; ShowWrongFlagsOnly branch TANPA kunci |
| RTK-12 | 407-01, 407-02, 407-03 | View riwayat pekerja + drill-down archive + `IsCurrentAttempt` flag | SATISFIED | `_RiwayatPekerja.cshtml` ter-gate; `AllWorkersHistoryRow.IsCurrentAttempt`; `RiwayatUnifier.Build` di Results |
| RTK-13 | 407-02 | Guards komprehensif server-authoritative (ownership + CanRetakeAsync re-check) | SATISFIED | Ownership Forbid SEBELUM CanRetakeAsync SEBELUM ExecuteAsync di `RetakeExam`; CanRetakeAsync wraps `RetakeRules.CanRetake` (dari 405) |

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `Views/CMP/_RiwayatPekerja.cshtml` | Comments di-rephrase agar tidak memuat literal leak-token — tidak ada `Html.Raw` di markup ter-render | Info | Bukan anti-pattern: executor sudah memperbaiki sebelum commit (SUMMARY §Deviations). Markup ter-render bersih. |
| Tidak ada | Tidak ada `return null`/`return {}`/`TODO`/`PLACEHOLDER` yang mengalir ke render | — | CLEAN |

Tidak ada anti-pattern blocker atau warning ditemukan.

---

### Human Verification Required

#### 1. Playwright Smoke Leak-Safety @5270

**Test:** Jalankan `npx playwright test tests/e2e/retake-worker-407.spec.ts --workers=1` dengan app berjalan di `http://localhost:5270` (branch ITHandoff, `Authentication__UseActiveDirectory=false dotnet run --urls http://localhost:5270`) + SQLEXPRESS tersedia + `E2E_BASE_URL=http://localhost:5270`.

**Persiapan:** Seed SQL sudah ada di `tests/sql/retake-worker-407-seed.sql` (3 sesi: A=LeakSafe+Eligible, B=CapReached, C=CooldownActive). Spec menjalankan `db.backup` sebelum dan `db.restore` setelah — DB akan di-RESTORE otomatis. Tandai `docs/SEED_JOURNAL.md` entry 407-03 sebagai `cleaned` setelah selesai.

**Expected (6 skenario):**
1. **leak-safety** — Card "Tinjauan Jawaban" + notice "Kunci jawaban disembunyikan"; DOM TIDAK mengandung `KUNCIBENAR_A1`/`KUNCIBENAR_A2`/`(Jawaban Benar)`; TIDAK ada `.list-group-item-success`; ADA badge Benar/Salah + "Jawaban Anda:"; `no-pageerror`
2. **control eligible** — `#btnRetake` visible+enabled+label "Ujian Ulang"; counter "Percobaan ke-X dari N" tampil
3. **modal** — klik "Ujian Ulang" → `#retakeConfirmModal` visible; `input[name=__RequestVerificationToken]` count=1; `no-pageerror`
4. **riwayat** — card "Riwayat Percobaan Saya" tampil; `#riwayatPekerjaAccordion` ada; badge "Percobaan saat ini"
5. **cap reached** — alert-warning "Batas percobaan tercapai"; `#btnRetake` count=0
6. **cooldown** — tombol disabled + `#retakeCountdown` format HH:MM:SS + nilai berubah (ticking)

**Why human:** Lesson 354/413 — grep+build tidak menangkap ReferenceError/handler-attach yang abort di browser. Leak-safety DOM hanya bisa dibuktikan dengan DOM real-browser (TIDAK cukup dengan grep `ShowWrongFlagsOnly` bersih). Spec sudah dibuat dan build OK (`--list` 6 tests) tapi live DOM assert memerlukan app aktif + SQLEXPRESS.

---

### Gaps Summary

Tidak ada gaps programatik yang gagal. Semua 9 truths yang dapat diverifikasi secara otomatis telah VERIFIED:

- Helper pure `RetakeReviewMode`/`ResolveReviewMode` — VERIFIED dengan 6 unit test
- Endpoint `RetakeExam` (antiforgery + ownership Forbid + CanRetakeAsync re-check + token-clear + redirect) — VERIFIED dengan 3 endpoint tests
- VM `AssessmentResultsViewModel` 7 field + `AllWorkersHistoryRow.IsCurrentAttempt` — VERIFIED (field ada, diisi di controller)
- `switch(Model.RetakeMode)` 3-state di Results.cshtml — VERIFIED (grep + analisis branch)
- Branch `ShowWrongFlagsOnly` LEAK-SAFE — VERIFIED (tidak ada `list-group-item-success`/`(Jawaban Benar)`/`CorrectAnswer` di branch)
- Partial `_RiwayatPekerja.cshtml` ter-gate — VERIFIED (ZERO `Html.Raw`, "Tidak Lulus"/"Jawaban Saya"/HideDetail ada)
- Build 0 error, unit suite 448/0/2 — VERIFIED (dijalankan langsung)

Satu-satunya item yang belum bisa dikonfirmasi secara otomatis adalah **Playwright live DOM @5270** (skenario 1 leak-safety DOM dan no-pageerror wajib dibuktikan di real-browser per lesson 413). Status: **human_needed**.

---

_Verified: 2026-06-22T03:30:00Z_
_Verifier: Claude (gsd-verifier)_
