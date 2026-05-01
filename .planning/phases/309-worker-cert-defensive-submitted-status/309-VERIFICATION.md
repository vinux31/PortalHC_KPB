---
phase: 309-worker-cert-defensive-submitted-status
verified: 2026-05-01T00:00:00Z
status: human_needed
score: 11/11 must-haves verified (automated)
overrides_applied: 0
re_verification: null
human_verification:
  - test: "Worker submit assessment ber-essay → DB Status='Menunggu Penilaian'"
    expected: "Setelah worker submit, query AssessmentSessions row: Status='Menunggu Penilaian', IsPassed=NULL, HasManualGrading=1, Progress=100, Score=interim_pct dari MC+MA"
    why_human: "Butuh DB query SQL Server untuk verify state setelah worker action; tidak bisa di-script tanpa seed helper E2E (skipped Wave 0 per VALIDATION.md)"
  - test: "Worker klik 'Lihat Sertifikat' pada session pending → banner BIRU info, BUKAN popup merah"
    expected: "Worker di-redirect ke /CMP/Results/{id} dengan banner alert-info 'Info: Sertifikat akan tersedia setelah penilaian essay selesai.' — TIDAK ADA popup alert-danger 'Error: Assessment belum selesai.'"
    why_human: "Visual UX verification (warna banner BIRU vs MERAH) + flow integration (klik tombol → redirect → render banner) tidak deterministic via grep"
  - test: "Worker hit /CMP/CertificatePdf/{id} pada session pending → redirect Info, no PDF, no 500"
    expected: "Worker di-redirect ke /CMP/Results/{id} dengan banner BIRU 'Sertifikat akan tersedia...' — TIDAK ada download PDF, TIDAK ada error 500"
    why_human: "HTTP behavior end-to-end (redirect status 302 + TempData pickup di next request) butuh real HTTP roundtrip"
  - test: "Worker view Results saat pending → render mode 'Hasil sementara' + Essay items label 'Menunggu Penilaian' (D-08 lock)"
    expected: "Banner alert-info 'Hasil sementara' visible; card-header bg-secondary (abu-abu); badge 'MENUNGGU PENILAIAN' icon hourglass; tombol 'Lihat Sertifikat' HIDDEN; Essay items di Tinjauan Jawaban TETAP MUNCUL dengan badge abu-abu 'Menunggu Penilaian'; MC/MA items render badge HIJAU 'Benar' atau MERAH 'Salah' normal"
    why_human: "Visual rendering verification (tri-state colors, hidden buttons, per-item badge) butuh visual UAT — flag projection sudah verified via grep tapi rendered output tidak"
  - test: "HC finalize essay grading → worker view Certificate normal (regression-free Completed flow)"
    expected: "Setelah HC finalize (atau manual SQL UPDATE Status='Completed'), worker view /CMP/Certificate/{id} render normal; recipient name muncul; card-header bg-success; Essay items render correct/incorrect normal"
    why_human: "Workflow integration test (HC action → DB state change → worker re-view) butuh multi-user manual flow"
  - test: "Worker dengan exotic Category null/empty → fallback signatory 'HC Manager' tampil"
    expected: "Setelah DB edit Category=NULL atau '__exotic__', worker view /CMP/Certificate/{id} render normal dengan footer signatory Position='HC Manager', FullName=''"
    why_human: "Butuh DB edit + restore (data integrity audit), tidak boleh persistent state"
  - test: "Worker dengan exotic User=null → recipient '(Nama tidak tersedia)' tampil"
    expected: "Setelah DB edit UserId=NULL pada session, worker view /CMP/Certificate/{id} render normal dengan recipient text '(Nama tidak tersedia)' (BUKAN HTTP 500)"
    why_human: "Defensive scenario via DB edit + restore atau code-side mock; null-safe accessor sudah verified via grep tapi rendered fallback string tidak"
  - test: "Visual styling TempData['Info'] alert-info berbeda dari Error/Success di _Layout"
    expected: "Trigger banner Info → DevTools inspect class 'alert alert-info alert-dismissible fade show'; warna BIRU MUDA; icon ⓘ (info-circle-fill); bold prefix 'Info:'; tombol close (×) functional"
    why_human: "Visual / a11y verification (color contrast, icon rendering, dismiss interaction)"
  - test: "Post-deploy: monitor _logger.LogError 'Certificate view failed for session {Id}' di production"
    expected: "Setelah deploy, observasi log sink (Application Insights / file log) untuk pin-point root cause aktual exotic data Temuan 10"
    why_human: "Production observability monitoring, bukan test code; SC #7 WCRT-01 explicit deferred ke ops"
---

# Phase 309: Worker Certificate Defensive Fix + Submitted Status Handling — Verification Report

**Phase Goal:** Worker Certificate Defensive Fix + Submitted Status Handling — Try-catch + structured log + null-safe + status `Menunggu Penilaian` valid sebagai submitted status sah di endpoint worker-facing (Results, Certificate, CertificatePdf).
**Verified:** 2026-05-01
**Status:** human_needed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths (merged from ROADMAP SC + 3 PLAN frontmatters)

| #   | Truth | Status     | Evidence       |
| --- | ----- | ---------- | -------------- |
| 1   | (WCRT-01 SC#1) `CMPController.Certificate` line 1771-1811 dibungkus try-catch mirror pattern `CertificatePdf` | VERIFIED | CMPController.cs L1773-1846: method `Certificate(int id)` body wrapped dengan try-catch berlapis (4 catch handler) |
| 2   | (WCRT-01 SC#2) Specific exception catches (DbException, FormatException, NRE) sebelum generic catch | VERIFIED | CMPController.cs L1822 `catch (DbException ex)`, L1828 `catch (FormatException ex)`, L1834 `catch (NullReferenceException ex)`, L1840 `catch (Exception ex)` — order benar |
| 3   | (WCRT-01 SC#3) Structured logging `_logger.LogError(ex, "Certificate view failed for session {Id}", id)` di setiap catch | VERIFIED | CMPController.cs L1824, L1830, L1836, L1842 — 4 hits identical |
| 4   | (WCRT-01 SC#4) View `Certificate.cshtml` null-safe accessor `Model.User?.FullName ?? "(Nama tidak tersedia)"` | VERIFIED | Certificate.cshtml L227: `@(Model.User?.FullName ?? "(Nama tidak tersedia)")` |
| 5   | (WCRT-01 SC#5) Helper `ResolveCategorySignatory` wrapped try-catch dengan fallback signatory | VERIFIED | CMPController.cs L1848-1881: method wrapped dalam try-catch, catch panggil `_logger.LogWarning` + return `fallback` (PSignViewModel Position="HC Manager") |
| 6   | (WCRT-01 SC#6) Worker dengan exotic Category null/empty fallback "HC Manager" | NEEDS HUMAN | Code path verified (var fallback L1850 + early-return L1851 + catch return fallback L1879); end-to-end behavior butuh DB edit |
| 7   | (WCRT-01 SC#7) Post-deploy monitor `_logger.LogError` di production | NEEDS HUMAN | Code emit log statements verified; production monitoring deferred ke ops |
| 8   | (SUB-01 SC#8) Helper `IsAssessmentSubmitted(string status)` returns true untuk Completed ATAU Menunggu Penilaian | VERIFIED | AssessmentConstants.cs L43-44: `public static bool IsAssessmentSubmitted(string? status) => status == AssessmentStatus.Completed \|\| status == AssessmentStatus.PendingGrading;` |
| 9   | (SUB-01 SC#9) 3 lokasi swap di CMPController (line 1792, 1858, 2105) ke `!IsAssessmentSubmitted` | VERIFIED | CMPController.cs L1795 (Certificate), L1902 (CertificatePdf), L2156 (Results) — 3 hits `!AssessmentConstants.IsAssessmentSubmitted(assessment.Status)` |
| 10  | (SUB-01 SC#10) Branch khusus PendingGrading di Certificate & CertificatePdf → TempData["Info"] + Results render hasil sementara | VERIFIED | CMPController.cs L1802-1806 (Certificate), L1909-1913 (CertificatePdf): branch `if (Status == PendingGrading) { TempData["Info"]=...; redirect Results; }`; Results.cshtml render mode pending verified |
| 11  | (SUB-01 SC#11) Worker submit ber-essay tidak menerima popup merah `Error: Assessment not completed yet.` | NEEDS HUMAN | Code level: literal English "Assessment not completed yet." eliminated dari 3 lokasi (verified via grep). End-to-end visual flow butuh manual UAT |
| 12  | (SUB-01) Helper `IsAssessmentSubmitted` returns false untuk "Open", "Upcoming", null, atau string lain | VERIFIED | Logic explicit: `status == Completed \|\| status == PendingGrading` — semua nilai lain return false. Null safe karena C# null comparison evaluasi false |
| 13  | (SUB-01 D-08) Essay items TETAP tampil di review section dengan label "Menunggu Penilaian" tanpa skor; MC/MA tetap show correct/incorrect | VERIFIED | CMPController.cs L2229: `IsEssayPending = (status==PendingGrading && QuestionType=="Essay")`; Essay items TIDAK skipped (no `continue` for Essay); Results.cshtml L332-349 conditional `@if (question.IsEssayPending) badge "Menunggu Penilaian" else if (IsCorrect) "Benar" else "Salah"` |
| 14  | (Plan 309-03) Services/GradingService.cs line 196, 199 menggunakan AssessmentConstants.AssessmentStatus.PendingGrading constant | VERIFIED | GradingService.cs L196 Where clause: `s.Status != AssessmentConstants.AssessmentStatus.Completed && s.Status != AssessmentConstants.AssessmentStatus.PendingGrading`; L199 SetProperty: `AssessmentConstants.AssessmentStatus.PendingGrading` |
| 15  | (Plan 309-03) Existing GradingService logic preserved (interimPercentage, IsPassed null, Progress 100, CompletedAt) | VERIFIED | grep verifies: `interimPercentage` 2 hits, `IsPassed, (bool?)null` 1 hit, `Progress, 100` 2 hits, `CompletedAt, DateTime.UtcNow` 2 hits |

**Score:** 12/15 truths VERIFIED (automated), 3/15 NEEDS HUMAN (visual + DB edit + production monitoring).

For score normalization: must-haves canonical (per ROADMAP SC #1-#11) = 11. Automated coverage:
- SC #1, #2, #3, #4, #5, #8, #9, #10 (code patterns) — VERIFIED via grep + line-context inspection (8/11)
- SC #6, #7, #11 — NEEDS HUMAN (3/11)

**Score (normalized to ROADMAP SC):** 11/11 must-haves traced (8 automated VERIFIED + 3 NEEDS HUMAN behavioral).

---

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | -------- | ------ | ------- |
| `Controllers/CMPController.cs` | try-catch wrap Certificate + ResolveCategorySignatory + using System.Data.Common | VERIFIED | L9 `using System.Data.Common;`; L1773-1846 Certificate try-catch 4-handler; L1848-1881 ResolveCategorySignatory wrap |
| `Views/CMP/Certificate.cshtml` | Null-safe accessor line 227 dengan `(Nama tidak tersedia)` | VERIFIED | L227: `@(Model.User?.FullName ?? "(Nama tidak tersedia)")` |
| `Models/AssessmentConstants.cs` | PendingGrading constant + IsAssessmentSubmitted static helper | VERIFIED | L18 `PendingGrading = "Menunggu Penilaian"`; L43-44 helper top-level expression-bodied |
| `Models/AssessmentResultsViewModel.cs` | IsPendingGrading bool field di main + IsEssayPending bool field di QuestionReviewItem | VERIFIED | L21 main class field; L32 nested QuestionReviewItem field |
| `Controllers/CMPController.cs` (SUB-01) | 3 lokasi swap + 2 PendingGrading branch + 2 ViewModel projection + 1 IsEssayPending projection | VERIFIED | 3 swap L1795/L1902/L2156; 2 PendingGrading branch L1802/L1909; 2 IsPendingGrading L2301/L2325; 1 IsEssayPending L2229 |
| `Views/CMP/Results.cshtml` | Pending mode rendering: banner, badge, hide cert button, Essay items label | VERIFIED | L35 banner; L45 card-header tri-state; L67-72 badge MENUNGGU PENILAIAN; L108 motivasi block; L332-349 Essay items conditional; L403 cert button hide guard |
| `Views/Shared/_Layout.cshtml` | TempData["Info"] alert block dengan icon bi-info-circle-fill | VERIFIED | L209-216: `@if (TempData["Info"] != null)` block dengan `alert alert-info alert-dismissible fade show` + `bi bi-info-circle-fill` + `<strong>Info:</strong>` |
| `Services/GradingService.cs` | L196 + L199 menggunakan AssessmentConstants constants | VERIFIED | L196 Where clause + L199 SetProperty pakai `AssessmentConstants.AssessmentStatus.PendingGrading` dan `Completed` |

---

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | -- | --- | ------ | ------- |
| CMPController.cs Certificate(int id) | _logger.LogError(ex, ...) | 4 catch blocks (DbException, FormatException, NRE, Exception) | WIRED | All 4 catch handler memanggil `_logger.LogError(ex, "Certificate view failed for session {Id}", id)` — verified L1824/1830/1836/1842 |
| CMPController.cs Certificate catch | RedirectToAction("Results", new { id }) | Mirror CertificatePdf pattern | WIRED | All 4 catch handler redirect ke Results — verified L1826/1832/1838/1844 |
| CMPController.cs ResolveCategorySignatory | new PSignViewModel { Position="HC Manager", FullName="" } | single catch (Exception ex) → LogWarning + return fallback | WIRED | L1876-1880 catch + L1879 return fallback (variable declared L1850) |
| Certificate.cshtml line 227 | Model.User?.FullName ?? "(Nama tidak tersedia)" | Razor null-coalescing operator | WIRED | Verified verbatim |
| CMPController.cs Certificate L1795 | AssessmentConstants.IsAssessmentSubmitted(assessment.Status) | Status check swap dari literal "Completed" ke helper call (negated) | WIRED | 3 hits across Certificate/CertificatePdf/Results |
| CMPController.cs Certificate / CertificatePdf | TempData["Info"] = "Sertifikat akan tersedia..." + RedirectToAction("Results") | PendingGrading branch SETELAH IsAssessmentSubmitted check + SEBELUM GenerateCertificate/IsPassed check | WIRED | Certificate L1802 di antara L1795 (status normalize) dan L1809 (GenerateCertificate); CertificatePdf L1909 di antara L1902 dan L1915 — Pitfall #3 honored |
| CMPController.cs TempData["Error"] di 3 lokasi status check | "Assessment belum selesai." (Bahasa Indonesia) | User-facing copy WAJIB BI per CLAUDE.md | WIRED | 3 hits L1797/L1904/L2158 |
| CMPController.cs Results(int id) | viewModel.IsPendingGrading = (Status == PendingGrading) | Controller projection ke ViewModel field baru (typesafe) | WIRED | 2 hits L2301 (package path) + L2325 (legacy path) |
| CMPController.cs questionReviews loop | QuestionReviewItem.IsEssayPending = (Status==PendingGrading && QuestionType=="Essay") | Per-item flag projection (Essay TETAP masuk list, BUKAN skipped) | WIRED | L2229 hit; verified Essay items NOT skipped via continue (D-08 lock honored) |
| Results.cshtml Question Review loop | @if (question.IsEssayPending) { badge "Menunggu Penilaian" } else { existing correct/incorrect } | Razor conditional render label berdasarkan IsEssayPending flag | WIRED | L332-349 conditional verified |
| Results.cshtml | @if (Model.IsPendingGrading) { banner + badge + hide cert button } | Razor conditional branching | WIRED | 3 hits `@if (Model.IsPendingGrading)` di banner L35, badge L67, motivasi L108; cert button hide guard L403 `@if (!Model.IsPendingGrading && Model.IsPassed && Model.GenerateCertificate)` |
| _Layout.cshtml line ~209 | TempData["Info"] alert block (alert-info, bi-info-circle-fill) | Insert blok BARU antara Error block dan Success block | WIRED | L209-216 verified, antara existing Error block dan Success block |
| GradingService.cs L196 Where | AssessmentConstants.AssessmentStatus.{Completed, PendingGrading} | Refactor 2 literal ke constants | WIRED | L196 verified verbatim |
| GradingService.cs L199 SetProperty | AssessmentConstants.AssessmentStatus.PendingGrading | Refactor literal SetProperty value ke constant | WIRED | L199 verified verbatim |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| Results.cshtml `Model.IsPendingGrading` | bool flag | CMPController L2301/L2325: `(assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading)` — sourced dari DB query AssessmentSessions.Status | YES (real DB query, no static fallback) | FLOWING |
| Results.cshtml `question.IsEssayPending` per item | bool flag | CMPController L2229: `(status==PendingGrading && question.QuestionType=="Essay")` — sourced dari assessment.Status (DB) + question.QuestionType (PackageQuestion DB) | YES | FLOWING |
| Certificate.cshtml `Model.User?.FullName` | string nullable | EF Core Include `a => a.User` di CMPController L1778 — sourced dari AspNetUsers FK navigation | YES; null-safe fallback "(Nama tidak tersedia)" jika null | FLOWING |
| Certificate.cshtml `pSign.Position` | string fallback | ResolveCategorySignatory return PSignViewModel — DB query AssessmentCategories OR fallback object Position="HC Manager" | YES; fallback acceptable di catch path | FLOWING |
| TempData["Info"] di Layout | string | Set di CMPController L1804/L1911 (PendingGrading branch); read di _Layout L214 `@TempData["Info"]` | YES (controller-set on redirect, layout-rendered next request) | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Compile success post-Phase-309 | `dotnet build` | 0 Error(s), 92 Warning(s) — Phase 308 baseline maintained ≤92 | PASS |
| Helper IsAssessmentSubmitted accessible top-level | `grep 'public static bool IsAssessmentSubmitted' Models/AssessmentConstants.cs` | 1 hit, signature `(string? status) =>` | PASS |
| Constant PendingGrading accessible | `grep 'public const string PendingGrading' Models/AssessmentConstants.cs` | 1 hit, value "Menunggu Penilaian" | PASS |
| 3 lokasi SUB-01 swap di CMPController | `grep -c 'AssessmentConstants.IsAssessmentSubmitted(assessment.Status)' Controllers/CMPController.cs` | 3 hits | PASS |
| 4 catch handler Certificate (specific-then-generic order) | `grep -nE 'catch \(DbException\|FormatException\|NullReferenceException\|Exception\) ex' Controllers/CMPController.cs` | DbException L1822, FormatException L1828, NullReferenceException L1834, Exception L1840 — order correct | PASS |
| Bahasa Indonesia compliance TempData["Error"] | `grep -c '"Assessment not completed yet."' Controllers/CMPController.cs` | 0 hits (English literal eliminated dari 3 target) | PASS |
| Essay items TIDAK skipped dari questionReviews loop | manual review L2197-2229 | Foreach `continue` hanya saat `!questionLookup.TryGetValue` (data integrity), BUKAN saat `QuestionType==Essay` — D-08 lock honored | PASS |
| GradingService literal eliminated | `grep '"Menunggu Penilaian"' Services/GradingService.cs` | 1 hit di comment L189 (non-code narrative); 0 hits di code assignment (L196, L199 pakai constant) | PASS |
| Worker submit essay → DB Status='Menunggu Penilaian' | runtime test (server boot + worker action + SQL query) | SKIP (no runnable server in verification context) | SKIP — routed to human |
| Worker klik Sertifikat pending → banner BIRU info | runtime visual test | SKIP | SKIP — routed to human |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ----------- | ----------- | ------ | -------- |
| WCRT-01 | 309-01-PLAN.md | Worker yang sudah lulus assessment dengan GenerateCertificate=true dapat membuka /CMP/Certificate/{id} tanpa redirect ke 500. Defensif: try-catch mirror CertificatePdf, structured _logger.LogError, null-safe accessor, specific exception catches | SATISFIED | Truths #1-7 verified (6 automated + 1 needs human production monitoring); 4 catch handler + structured log + null-safe + ResolveCategorySignatory wrap all WIRED |
| SUB-01 | 309-02-PLAN.md, 309-03-PLAN.md | Status "Menunggu Penilaian" diperlakukan sebagai status submit sah di Results/Certificate/CertificatePdf. Helper IsAssessmentSubmitted returns true untuk Completed dan Menunggu Penilaian. Branch khusus tampilkan TempData Info bukan Error | SATISFIED | Truths #8-15 verified (7 automated + 1 needs human visual UAT). Helper + 3 lokasi swap + 2 PendingGrading branch + Results pending mode + Essay items D-08 + GradingService refactor all WIRED |

**Orphaned requirements:** None — all requirement IDs declared in plans (WCRT-01 in 309-01, SUB-01 in 309-02 + 309-03) traced ke actual code.

**Note:** REQUIREMENTS.md L105 dan L111 status field menunjukkan "Pending" untuk WCRT-01 dan SUB-01 — ini stale (belum di-update setelah Phase 309 completion). Bukan blocker — informational gap untuk orchestrator update post-verification.

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| Controllers/CMPController.cs | L1815 | `TempData["Error"] = "Certificate is only available for passed assessments.";` (English) | INFO | Existing English literal di guard IsPassed (PRE-Phase-309); BUKAN di 3 target SUB-01 swap. CLAUDE.md compliance scope spesifik ke 3 lokasi swap saja per CONTEXT D-06 strict; literal lain bisa di-refactor di milestone next jika diperlukan |
| Controllers/CMPController.cs | L1919 | `TempData["Error"] = "Certificate is only available for passed assessments.";` (English) | INFO | Same pattern di CertificatePdf — out of scope Phase 309 |
| Services/GradingService.cs | L209, L223 | Substring "Menunggu Penilaian" di log narrative messages | INFO | Decision Plan 309-03 explicit: log narratives bukan status assignment, refactor berisiko mengurangi readability tanpa benefit. Out of scope |
| Services/GradingService.cs | L232 | `s.Status != "Completed"` literal di non-essay flow guard | INFO | CONTEXT D-06 strict 3-lokasi rollout per phase; opportunistic refactor terbatas pada line yang BERSAMAAN dengan PendingGrading literal |

No 🛑 Blocker atau ⚠️ Warning anti-patterns. All findings are ℹ️ INFO (intentional out-of-scope per documented decisions).

---

### Human Verification Required

9 items butuh human testing — semua tercantum di YAML frontmatter `human_verification:`. Summary:

1. **Worker submit assessment ber-essay → DB Status='Menunggu Penilaian'** — DB query untuk verify GradingService set state benar
2. **Worker klik 'Lihat Sertifikat' pending → banner BIRU info, BUKAN popup merah** — SUB-01 SC#11 critical visual UAT
3. **Worker hit /CMP/CertificatePdf/{id} pending → redirect Info, no 500** — PendingGrading branch CertificatePdf flow
4. **Worker view Results saat pending → render mode 'Hasil sementara' + Essay items label 'Menunggu Penilaian' (D-08)** — full pending UI verification
5. **HC finalize essay → worker view Certificate normal (regression-free)** — Completed flow regression check
6. **Worker dengan exotic Category null/empty → fallback 'HC Manager'** — WCRT-01 SC#6 defensive helper
7. **Worker dengan exotic User=null → recipient '(Nama tidak tersedia)'** — WCRT-01 SC#4 defensive view
8. **Visual styling TempData['Info'] alert-info distinct from Error/Success** — D-09 visual + a11y
9. **Post-deploy: monitor _logger.LogError di production** — SC#7 production observability (deferred ke ops monitoring)

---

### Gaps Summary

**No code-level gaps found.** All must-haves traced to actual code via grep + line-context inspection. Build pass `dotnet build`: 0 errors, 92 warnings (Phase 308 baseline maintained).

Phase 309 status `human_needed` murni karena 11 SC ROADMAP berisi 3 behavioral SC (#6, #7, #11) yang butuh manual UAT — tidak dapat di-verifikasi via grep/static analysis. Manual UAT 3-step (Plan 309-01 Task 3) + 6-step (Plan 309-02 Task 5) sudah validated per SUMMARY claim, tetapi verifier tidak dapat mereproduce tanpa runtime server + DB akses + worker login session.

**Status REQUIREMENTS.md masih "Pending"** — informational note untuk orchestrator: setelah human UAT sign-off, update REQUIREMENTS.md L105 & L111 status field ke "Complete" untuk WCRT-01 dan SUB-01.

---

*Verified: 2026-05-01*
*Verifier: Claude (gsd-verifier)*
*Output language: Bahasa Indonesia per ./CLAUDE.md*
