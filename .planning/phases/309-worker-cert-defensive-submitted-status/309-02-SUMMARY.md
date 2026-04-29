---
phase: 309-worker-cert-defensive-submitted-status
plan: 02
subsystem: backend-state-machine
tags: [aspnet-core-mvc, status-normalization, helper-extraction, viewmodel-extension, razor-conditional, tempdata-info, essay-pending-render]

# Dependency graph
requires:
  - phase: 309-01-worker-cert-defensive
    provides: Certificate action wrapped try-catch (Plan 309-02 swap status check di line ~1795 aman karena method sudah defensive); using System.Data.Common already added (no duplicate needed)
provides:
  - "AssessmentConstants.AssessmentStatus.PendingGrading const = \"Menunggu Penilaian\" (D-04)"
  - "AssessmentConstants.IsAssessmentSubmitted(string?) static helper top-level returning true untuk Completed ATAU PendingGrading (D-05)"
  - "AssessmentResultsViewModel.IsPendingGrading bool field (default false) untuk pending mode flag"
  - "QuestionReviewItem.IsEssayPending bool field (default false) untuk Essay pending label per item (D-08 OQ#3 iter-1)"
  - "3 lokasi swap status check di CMPController.cs (Certificate L~1795, CertificatePdf L~1894, Results L~2141) ke !AssessmentConstants.IsAssessmentSubmitted dengan TempData[\"Error\"] copy Bahasa Indonesia (CLAUDE.md)"
  - "2 PendingGrading branch (Certificate + CertificatePdf) dengan TempData[\"Info\"] redirect Results sebelum GenerateCertificate/IsPassed checks (D-07 Pitfall 3)"
  - "Results action ViewModel projection IsPendingGrading di package path + legacy path"
  - "Results action questionReviews loop projection IsEssayPending = (Status==PendingGrading && QuestionType==Essay) — Essay items TETAP masuk list (D-08 lock)"
  - "TempData[\"Info\"] alert block di _Layout.cshtml dengan icon bi-info-circle-fill (antara existing Error block dan Success block)"
  - "Results.cshtml pending mode rendering: banner 'Hasil sementara', card-header bg-secondary, badge 'MENUNGGU PENILAIAN' icon hourglass, motivasi block info, cert button HIDDEN saat pending"
  - "Results.cshtml Question Review section: Essay items badge 'Menunggu Penilaian' (text-bg-secondary + hourglass) saat q.IsEssayPending=true (D-08 iter-1 fix)"
affects: [309-03-grading-service-refactor (Wave 2 sequential — refactor literal 'Menunggu Penilaian' di GradingService L196/199 ke konstanta dari Plan 309-02), 310-essay-finalize-idempotency (reuse PendingGrading constant)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Status normalization helper di static class Constants — top-level method dengan nullable parameter (string?) untuk safety null comparison"
    - "ViewModel field-projection untuk pending semantic flag (typesafe, non-breaking, default false maintain semantik existing)"
    - "Razor tri-state conditional pattern di view: @if (Model.IsPendingGrading) { pending render } else if (Model.IsPassed) { success } else { fail } — branch order PENTING (pending check first)"
    - "Per-item conditional flag (IsEssayPending) pada QuestionReviewItem untuk per-row Razor branching tanpa parent IsPendingGrading propagation"
    - "TempData[\"Info\"] sebagai alert key baru di _Layout (antara Error dan Success) — alert-info Bootstrap 5.3 dengan dismiss button"

key-files:
  created: []
  modified:
    - "Models/AssessmentConstants.cs (PendingGrading constant + top-level IsAssessmentSubmitted helper)"
    - "Models/AssessmentResultsViewModel.cs (IsPendingGrading di main class + IsEssayPending di nested QuestionReviewItem)"
    - "Controllers/CMPController.cs (3 lokasi swap status check ke !IsAssessmentSubmitted dengan TempData Error BI; 2 PendingGrading branch Certificate + CertificatePdf; ViewModel projection IsPendingGrading di 2 lokasi Results action; questionReviews loop projection IsEssayPending)"
    - "Views/CMP/Results.cshtml (banner 'Hasil sementara', card-header tri-state bg-secondary, badge tri-state 'MENUNGGU PENILAIAN', motivasi block tri-state alert-info pending, action button hide cert guard, Question Review Essay items label 'Menunggu Penilaian')"
    - "Views/Shared/_Layout.cshtml (TempData[\"Info\"] alert block dengan icon bi-info-circle-fill antara existing Error dan Success block)"

key-decisions:
  - "ViewModel field IsPendingGrading dipilih over ViewBag untuk typesafety + compile-time check (OQ#1 RESOLVED — Plan 309-02 Task 2 Step 1)"
  - "Essay items TETAP masuk questionReviews list dengan flag IsEssayPending baru (option A) — REVISED dari iter-0 option B SKIP per CONTEXT D-08 lock (OQ#3 RESOLVED iter-1)"
  - "GradingService refactor literal 'Menunggu Penilaian' ke constant SPLIT ke Plan 309-03 Wave 2 sequential (iter-1 scope_sanity fix — kurangi task count Plan 309-02 dari 6 ke 5)"
  - "OMIT _logger.LogInformation di branch PendingGrading per CONTEXT D-10 lock (log noise)"
  - "TempData[\"Error\"] copy Bahasa Indonesia 'Assessment belum selesai.' di 3 lokasi swap (CLAUDE.md compliance — iter-1 Warning #1 fix)"
  - "PendingGrading branch placement HARUS sebelum GenerateCertificate dan IsPassed checks (Pitfall #3 lock — IsPassed null saat pending akan trigger bypass salah)"
  - "Lokasi C (Results) TIDAK punya PendingGrading branch — Results render pending-mode di view, bukan redirect (D-07 explicit)"

patterns-established:
  - "Pattern 'Status normalization via constants + helper': Constants own canonical strings + top-level static helper untuk semantic check (returns bool); call site di Controller pakai !helper(...) atau == constant"
  - "Pattern 'ViewModel pending flag projection': Controller projection (Status==Pending) ke ViewModel bool field di setiap construction path; non-breaking default false"
  - "Pattern 'Per-item conditional flag': flag IsEssayPending di nested ViewModel item untuk per-row Razor branching, alih-alih parent flag propagation"
  - "Pattern 'TempData multi-key Layout render': add new alert key via 1 file edit di _Layout.cshtml, reusable untuk semua controller"

requirements-completed: [SUB-01]

# Metrics
duration: ~30min (Tasks 1-4 implementation + verification + commits, exclude Task 5 manual UAT checkpoint)
completed: 2026-04-29
---

# Phase 309-02: Submitted Status Handling (SUB-01) Summary

**Eliminasi popup merah `Error: Assessment belum selesai.` saat worker submit assessment ber-essay buka /CMP/Results, klik Lihat Sertifikat, atau klik Download PDF — via helper `IsAssessmentSubmitted` yang menormalisasi status `Completed` dan `Menunggu Penilaian` sebagai sah submitted, plus Razor pending-mode rendering (banner Hasil sementara, badge MENUNGGU PENILAIAN, cert button HIDDEN, Essay items label "Menunggu Penilaian" tanpa correct/incorrect).**

## Performance

- **Duration:** ~30 menit (Task 1-4 implementation, exclude Task 5 manual UAT checkpoint)
- **Started:** 2026-04-29 (worktree agent-af489757b14a00c52)
- **Completed:** 2026-04-29 (4 task commits — Task 5 awaiting orchestrator checkpoint approval)
- **Tasks:** 4 dari 5 complete (Task 5 = manual UAT 6-step checkpoint, ditangani orchestrator)
- **Files modified:** 5 (Models/AssessmentConstants.cs, Models/AssessmentResultsViewModel.cs, Controllers/CMPController.cs, Views/CMP/Results.cshtml, Views/Shared/_Layout.cshtml)

## Accomplishments

### Task 1 — Models/AssessmentConstants.cs (D-04, D-05)

- Tambah `AssessmentStatus.PendingGrading = "Menunggu Penilaian"` constant (D-04)
- Tambah top-level static helper `IsAssessmentSubmitted(string?)` returning bool (D-05) — single source of truth untuk semantic "submitted" yang return true untuk Completed ATAU PendingGrading
- Helper menggunakan expression-bodied syntax `=>` konsisten existing convention; nullable parameter `string?` aman terhadap null argument (null == "..." evaluasi false di C#)
- Existing constants (Manual/Online/PreTest/PostTest, Open/Upcoming/Completed, Permanent/Annual/ThreeYear, FileValidation) preserved utuh

### Task 2 — Models/AssessmentResultsViewModel.cs + Controllers/CMPController.cs (D-06, D-07, D-08)

**ViewModel:**
- Tambah field `IsPendingGrading` (bool, default false) di main class `AssessmentResultsViewModel`
- Tambah field `IsEssayPending` (bool, default false) di nested class `QuestionReviewItem` (NEW iter-1 D-08 fix)
- `IsPassed` field PRESERVED non-nullable (Pitfall #4 honored — tidak ubah ke `bool?`)

**Controller — 3 lokasi swap status check:**
- Certificate (line ~1795 dalam try-catch dari Plan 309-01): swap `assessment.Status != "Completed"` ke `!AssessmentConstants.IsAssessmentSubmitted(assessment.Status)` dengan TempData["Error"] = "Assessment belum selesai." (Bahasa Indonesia per CLAUDE.md)
- CertificatePdf (line ~1894): swap dengan pattern identik
- Results (line ~2141): swap dengan pattern identik (NO PendingGrading branch — D-07)

**Controller — 2 PendingGrading branch:**
- Certificate: `if (Status == PendingGrading) { TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai."; return RedirectToAction("Results", new { id }); }` SEBELUM `if (!GenerateCertificate)` check (Pitfall #3)
- CertificatePdf: PendingGrading branch identik pattern Certificate
- Results: TIDAK ada branch — pending mode di-render di view via `Model.IsPendingGrading` flag

**Controller — Results action ViewModel projection:**
- 2 instance `viewModel = new AssessmentResultsViewModel { ... }` (package path line ~2280, legacy path line ~2301) — keduanya tambah `IsPendingGrading = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)`
- questionReviews loop (line ~2214 dalam `if (assessment.AllowAnswerReview)`): tambah `IsEssayPending = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading && (question.QuestionType ?? "MultipleChoice") == "Essay")` di `new QuestionReviewItem { ... }` initializer
- Essay items TETAP masuk list (TIDAK skip dengan continue) per CONTEXT D-08 lock — iter-1 fix Blocker #1 (REVERSE iter-0 SKIP pattern)

### Task 3 — Views/Shared/_Layout.cshtml (D-09)

- Insert blok BARU `@if (TempData["Info"] != null) { ... }` ANTARA existing Error block (line 199-208) dan Success block (line 209)
- Style: `alert alert-info alert-dismissible fade show`, icon `bi bi-info-circle-fill`, label `<strong>Info:</strong>`, dismiss button (×)
- Konsisten dengan existing pattern Warning/Error/Success — append-only edit, tidak ubah blok existing

### Task 4 — Views/CMP/Results.cshtml (D-08 — 6 changes)

- **Change A:** Banner alert-info "Hasil sementara — Essay menunggu penilaian HC. Skor & sertifikat akan diperbarui setelah penilaian selesai." setelah Page Header
- **Change B:** Card-header tri-state class `@(Model.IsPendingGrading ? "bg-secondary" : (Model.IsPassed ? "bg-success" : "bg-danger"))` — abu-abu saat pending
- **Change C:** Status badge tri-state branching — `text-bg-secondary` "MENUNGGU PENILAIAN" dengan icon `bi-hourglass-split` saat pending; LULUS/TIDAK LULUS saat completed
- **Change D:** Motivasi block tri-state — `alert-info` dengan icon `bi-hourglass-split` text "Skor di atas adalah nilai sementara dari soal Pilihan Tunggal & Pilihan Jamak. Skor final & sertifikat tersedia setelah penilaian Essay selesai." saat pending
- **Change E:** Action buttons cert button hide guard `@if (!Model.IsPendingGrading && Model.IsPassed && Model.GenerateCertificate)` — tombol "Lihat Sertifikat" HIDDEN saat pending
- **Change F (NEW iter-1 D-08 fix):** Question Review Essay items label — wrap existing IsCorrect conditional dengan `@if (question.IsEssayPending) { badge text-bg-secondary "Menunggu Penilaian" } else if (question.IsCorrect) { Benar } else { Salah }` — Essay items render label pending tanpa correct/incorrect; MC/MA dan Completed flow Essay items render normal (regression-safe)

## Task Commits

Each task was committed atomically dengan `--no-verify` flag (parallel worktree convention):

1. **Task 1: AssessmentConstants — PendingGrading + IsAssessmentSubmitted** — `08d76a3b` (feat)
2. **Task 2: ViewModel + 3 lokasi swap + 2 branch + Results projection + Essay flag** — `535f4be4` (feat)
3. **Task 3: _Layout.cshtml TempData["Info"] alert block** — `5da42e6c` (feat)
4. **Task 4: Results.cshtml pending mode rendering + Essay items label D-08** — `c5fe9f4f` (feat)
5. **Task 5: Manual UAT 6-step checkpoint** — awaiting orchestrator checkpoint approval (no commit — manual verification only)

**Plan metadata:** SUMMARY.md commit pending (akan di-tag setelah orchestrator checkpoint resolution)

## Files Created/Modified

- `Models/AssessmentConstants.cs` — `AssessmentStatus.PendingGrading` const + top-level `IsAssessmentSubmitted(string?)` static helper (Phase 309 D-04, D-05)
- `Models/AssessmentResultsViewModel.cs` — `IsPendingGrading` field di main class + `IsEssayPending` field di nested QuestionReviewItem (default false untuk keduanya, non-breaking)
- `Controllers/CMPController.cs` — 3 lokasi swap (`!IsAssessmentSubmitted` + TempData["Error"] BI), 2 PendingGrading branch (Certificate + CertificatePdf), Results action ViewModel projection IsPendingGrading di package + legacy path, questionReviews loop projection IsEssayPending
- `Views/CMP/Results.cshtml` — 6 changes pending mode rendering (banner, card-header tri-state, status badge tri-state, motivasi block tri-state, cert button hide guard, Question Review Essay label)
- `Views/Shared/_Layout.cshtml` — TempData["Info"] alert block (alert-info + bi-info-circle-fill + dismiss)

## Decisions Made

- **OQ#1 RESOLVED — ViewModel field over ViewBag:** Pilih typesafe ViewModel field `bool IsPendingGrading` (default false). Compile-time check, eksplisit di ViewModel contract, non-breaking. Sama untuk `IsEssayPending` di QuestionReviewItem.
- **OQ#3 RESOLVED iter-1 — Essay items TETAP tampil dengan IsEssayPending flag (option A):** REVISED dari iter-0 option B SKIP per CONTEXT D-08 lock explicit "Essay questions tetap tampil di review section dengan label Menunggu Penilaian tanpa nilai (MC/MA tetap show correct/incorrect)". iter-0 SKIP pattern BERTENTANGAN dengan D-08 — iter-1 revert.
- **Plan 309-03 SPLIT iter-1 — GradingService refactor di-defer:** OQ#2 (refactor literal "Menunggu Penilaian" di GradingService L196/199 ke konstanta) di-SPLIT ke Plan 309-03 Wave 2 sequential per iter-1 scope_sanity fix (kurangi task count Plan 309-02 dari 6 ke 5). Plan 309-03 depends_on=[309-02], reuse `AssessmentConstants.AssessmentStatus.PendingGrading` introduced di Plan 309-02 Task 1.
- **OQ#4 RESOLVED — OMIT _logger.LogInformation di branch PendingGrading:** Per CONTEXT D-10 lock (log noise). Forensics opsional via config flag jika audit team request post-deploy.
- **CLAUDE.md compliance — TempData["Error"] Bahasa Indonesia:** "Assessment belum selesai." (BUKAN "Assessment not completed yet.") di 3 lokasi swap. Konsisten dengan project directive `./CLAUDE.md` line 3 + RESEARCH.md §"Project Constraints" mandate.
- **PendingGrading branch placement BEFORE GenerateCertificate/IsPassed:** Per Pitfall #3 lock — IsPassed null saat pending akan trigger bypass salah jika branch ditempatkan setelah `if (!GenerateCertificate) return NotFound()` atau setelah `if (IsPassed != true)` check.
- **Lokasi C (Results) NO PendingGrading branch:** Per D-07 explicit — Results render pending mode di view via `Model.IsPendingGrading` flag, bukan redirect. Hanya Certificate dan CertificatePdf yang punya redirect branch.
- **IsPassed field PRESERVED non-nullable bool:** Tidak ubah ke `bool?` (Pitfall #4) — view existing line 36, 58, 93 use `Model.IsPassed` direct tanpa `.HasValue` guard. Tambah field BARU `IsPendingGrading` lebih aman, non-breaking.

## Deviations from Plan

None — plan executed exactly as written. Tidak ada Rule 1/2/3 auto-fix bugs/missing critical functionality/blocking issues yang ditemukan selama eksekusi Tasks 1-4. Build pass 0 errors di setiap task verification.

**Note tooling:** Awal Task 1 saya keliru menulis edit ke path root project (luar worktree). Setelah identify, root project file di-revert ke clean state via `git checkout --`, dan edit di-apply ulang dengan absolute path worktree yang benar. Tidak ada perubahan substansi dari plan; hanya teknis tooling. Root project verified clean (M only di .planning/config.json, docs/tutorial-workflow-claude.html, tests/playwright-report/index.html — pre-existing tracked changes, BUKAN dari Task 1-4 work).

## Issues Encountered

None — semua acceptance criteria PASS via grep checks + dotnet build PASS 0 errors 92 warnings (Phase 308 baseline maintained).

**Tooling note (resolved):** Initial path confusion antara root project dan worktree path saat Task 1. Resolved dengan revert root project + apply edit ke absolute worktree path. Pattern: SEMUA Edit/Read tool calls WAJIB pakai absolute path `.claude/worktrees/agent-af489757b14a00c52/...`.

## User Setup Required

None untuk Plan 309-02 code edit. Manual UAT (Task 5) butuh:
- App running di local: `dotnet run`
- Login worker yang punya akses ke assessment ber-essay
- SQL Server access (SSMS atau VS SQL editor) untuk verify DB state
- Optional: Browser DevTools (F12) untuk inspeksi Network response

## Verification Status

**Automated (PASS):**

- `dotnet build`: 0 errors, 92 warnings (Phase 308 baseline maintained ≤ 92)
- `grep PendingGrading = "Menunggu Penilaian"` di AssessmentConstants.cs: 1 hit ✓
- `grep IsAssessmentSubmitted(string? status) =>` di AssessmentConstants.cs: 1 hit ✓
- `grep public bool IsPendingGrading { get; set; } = false;` di AssessmentResultsViewModel.cs: 1 hit ✓
- `grep public bool IsEssayPending { get; set; } = false;` di AssessmentResultsViewModel.cs: 1 hit ✓
- `grep -c AssessmentConstants.IsAssessmentSubmitted(assessment.Status)` di CMPController.cs: 3 hits ✓ (3 lokasi swap)
- `grep -c assessment.Status != "Completed"` di CMPController.cs: 0 hits di 3 target lokasi (legacy `if (assessment.Status != "InProgress" && assessment.Status != "Open")` di exam abandon flow line 1146 BUKAN target SUB-01 — tetap)
- `grep -c AssessmentConstants.AssessmentStatus.PendingGrading` di CMPController.cs: 5 hits ✓ (2 PendingGrading branch + 2 IsPendingGrading projection + 1 IsEssayPending projection)
- `grep -c TempData["Info"] = "Sertifikat akan tersedia setelah penilaian essay selesai.";` di CMPController.cs: 2 hits ✓ (Certificate + CertificatePdf branch)
- `grep -c TempData["Error"] = "Assessment belum selesai.";` di CMPController.cs: 3 hits ✓ (CLAUDE.md BI compliance)
- `grep -c TempData["Error"] = "Assessment not completed yet.";` di CMPController.cs: 0 hits ✓ (English literal eliminated dari 3 target)
- `grep -c IsPendingGrading = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)` di CMPController.cs: 2 hits ✓ (package + legacy path)
- `grep IsEssayPending = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading` di CMPController.cs: 1 hit ✓
- `grep (question.QuestionType ?? "MultipleChoice") == "Essay"` di CMPController.cs: 1 hit ✓
- `grep @if (TempData["Info"] != null)` di _Layout.cshtml: 1 hit ✓
- `grep alert alert-info alert-dismissible fade show` di _Layout.cshtml: 1 hit ✓
- `grep bi bi-info-circle-fill me-2` di _Layout.cshtml: 1 hit ✓
- `grep <strong>Info:</strong> @TempData["Info"]` di _Layout.cshtml: 1 hit ✓
- `grep Hasil sementara` di Results.cshtml: 1 hit ✓ (banner heading)
- `grep MENUNGGU PENILAIAN` di Results.cshtml: 1 hit ✓ (status badge uppercase)
- `grep Menunggu Penilaian` di Results.cshtml: 1 hit ✓ (Question Review Essay badge label) — note: `grep -F` case-sensitive, "MENUNGGU PENILAIAN" tidak match "Menunggu Penilaian"; spirit acceptance terpenuhi (status badge + Question Review)
- `grep -c bi-hourglass-split` di Results.cshtml: 3 hits ✓ (status badge + motivasi block + Question Review Essay)
- `grep Model.IsPendingGrading ? "bg-secondary"` di Results.cshtml: 1 hit ✓ (card-header tri-state)
- `grep @if (!Model.IsPendingGrading && Model.IsPassed && Model.GenerateCertificate)` di Results.cshtml: 1 hit ✓
- `grep -c @if (Model.IsPendingGrading)` di Results.cshtml: 3 hits ✓ (banner + status badge + motivasi block)
- `grep question.IsEssayPending` di Results.cshtml: 1 hit ✓ (Question Review Essay branch)
- Existing structure preserved: `grep -c Lihat Sertifikat`: 1, `grep -c TIDAK LULUS`: 1, `grep -c Kembali`: ≥2 (back button)
- **NEGATIVE check (iter-1 D-08 lock):** Essay items TIDAK di-skip dengan `continue` dari questionReviews loop — iter-0 SKIP pattern ABSENT (verified manual review CMPController.cs L2197-2229 — foreach hanya `continue` saat `!questionLookup.TryGetValue`, bukan saat Essay)

**Manual UAT (Task 5 — checkpoint awaiting orchestrator approval):**

- Step 1: Worker submit essay → DB status `Menunggu Penilaian` (regression GradingService — refactor terjadi di Plan 309-03)
- Step 2: Worker klik Sertifikat → banner BIRU "Sertifikat akan tersedia..." (BUKAN popup merah) — SC #11 critical
- Step 3: Worker hit `/CMP/CertificatePdf/{id}` saat pending → redirect Info banner (no PDF, no 500)
- Step 4: Worker view Results saat pending → banner "Hasil sementara" + badge "MENUNGGU PENILAIAN" + cert button HIDDEN + **Essay items TETAP tampil dengan label "Menunggu Penilaian"** (iter-1 D-08 verify)
- Step 5: HC finalize → worker view Certificate normal (regression-free Completed flow + Essay items render correct/incorrect normal)
- Step 6: Visual smoke test TempData["Info"] alert in Layout (alert-info, info-circle-fill, dismiss button)

## Next Phase Readiness

**Plan 309-02 ready untuk close pasca Task 5 manual UAT approval.**

**Plan 309-03 (GradingService refactor) prerequisites SATISFIED:**

- `AssessmentConstants.AssessmentStatus.PendingGrading` constant introduced di Task 1 — Plan 309-03 dapat langsung refactor literal "Menunggu Penilaian" di GradingService.cs L196 + L199 ke `AssessmentConstants.AssessmentStatus.PendingGrading`
- Constant accessible: `public const string` di nested `static class AssessmentStatus` — fully public, namespace `HcPortal.Models` (existing import path konsisten)
- Helper IsAssessmentSubmitted juga tersedia jika GradingService butuh status semantic check di future

**Phase 310 dependency satisfied:**

- `AssessmentConstants.AssessmentStatus.PendingGrading` available untuk reuse di Phase 310 FinalizeEssayGrading flow (Phase 310 file scope: `Controllers/AssessmentAdminController.cs` + `Views/CDP/CertificationManagement.cshtml` — TIDAK overlap dengan Phase 309 file scope, parallel-safe CONFIRMED)

**Threat model status (per `<threat_model>` Plan 309-02):**

- T-309-04 (Information Disclosure TempData): MITIGATED — semua TempData values static literal Bahasa Indonesia, NO ex.Message / DB error / PII; Razor auto-HTML-encode `@TempData["Info"]` → XSS surface zero
- T-309-05 (Tampering status state machine): ACCEPTED — IsAssessmentSubmitted expand access ke Completed|PendingGrading; access decision tetap gated by `assessment.UserId == user.Id || isAdmin/HC` (existing security check); IsPendingGrading hanya UI flag, bukan auth decision
- T-309-06 (Tampering ViewModel orphan path): MITIGATED — default `= false` di field declaration; explicit projection di package + legacy path; future ViewModel construction tanpa assignment graceful fallback ke false (render normal, BUKAN crash)
- T-309-07 (Repudiation no audit log): ACCEPTED — per CONTEXT D-10 lock; future monitoring opsional via config flag jika request

## Known Stubs

None — Plan 309-02 tidak introduce stub atau placeholder. Semua fields, branches, dan rendering paths punya logic / data source yang lengkap. Plan 309-03 akan reuse constant yang introduced di sini (sequential refactor, BUKAN stub yang dijanjikan).

## Self-Check: PASSED

- File `Models/AssessmentConstants.cs`: FOUND, modified (PendingGrading constant + IsAssessmentSubmitted helper)
- File `Models/AssessmentResultsViewModel.cs`: FOUND, modified (IsPendingGrading + IsEssayPending fields)
- File `Controllers/CMPController.cs`: FOUND, modified (3 swap + 2 branch + 2 ViewModel projection + 1 IsEssayPending projection)
- File `Views/CMP/Results.cshtml`: FOUND, modified (6 changes pending mode + Essay label)
- File `Views/Shared/_Layout.cshtml`: FOUND, modified (TempData Info block)
- Commit `08d76a3b` (Task 1): FOUND di git log
- Commit `535f4be4` (Task 2): FOUND di git log
- Commit `5da42e6c` (Task 3): FOUND di git log
- Commit `c5fe9f4f` (Task 4): FOUND di git log
- All acceptance criteria Task 1-4: PASS (verified via grep + dotnet build 0 errors 92 warnings)
- Plan 309-03 prerequisite check: `AssessmentConstants.AssessmentStatus.PendingGrading` accessible as `public const string` ✓ (Plan 309-03 sequential refactor enabled)

---
*Phase: 309-worker-cert-defensive-submitted-status*
*Plan: 02 (SUB-01 — Submitted Status Helper + UX Branching)*
*Completed: 2026-04-29 (auto tasks); manual UAT 6-step checkpoint awaiting orchestrator approval*
*Output language: Bahasa Indonesia per ./CLAUDE.md*
