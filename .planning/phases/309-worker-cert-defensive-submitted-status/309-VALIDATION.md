---
phase: 309
slug: worker-cert-defensive-submitted-status
status: planned
nyquist_compliant: true
wave_0_complete: false
created: 2026-04-29
last_updated: 2026-04-29
revision: iter-1 (split Plan 309-03 + iter-1 D-08 fix Essay items + BI compliance)
---

# Phase 309 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (E2E TS) + dotnet build (compile gate) |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `cd tests && npx playwright test --grep "Phase 309" --reporter=list` |
| **Full suite command** | `cd tests && npx playwright test --reporter=list` |
| **Build command** | `dotnet build` (must compile 0 errors, warnings ≤ 92 baseline) |
| **Estimated runtime** | ~30 detik (quick) / ~5 menit (full) |

**Note:** Project tidak punya unit test framework (xUnit/NUnit) — manual UAT + dotnet build adalah primary validation. Wave 0 E2E coverage di-defer ke Plan 313+ jika Playwright support untuk PendingGrading state seed di-tambah; Phase 309 fokus dotnet build + manual UAT 6-step.

---

## Sampling Rate

- **Setelah setiap task commit:** `dotnet build` (must compile 0 errors)
- **Setelah Plan 309-01 complete:** Manual UAT 3-step Plan 309-01 Task 3 (smoke happy path + User=null + Category=null)
- **Setelah Plan 309-02 complete:** Manual UAT 6-step Plan 309-02 Task 5 (full SUB-01 flow + regression Completed + Essay review label verify per iter-1 D-08 fix)
- **Setelah Plan 309-03 complete:** dotnet build green (autonomous plan, no manual UAT — coverage via Plan 309-02 Task 5 Step 1 manual UAT regression GradingService)
- **Sebelum `/gsd-verify-work`:** Both manual UAT PASSED + dotnet build green
- **Max feedback latency:** ~30 detik (dotnet build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 309-01-T1 | 01 | 1 | WCRT-01 SC#1, #2, #3 | T-309-01, T-309-03 | Try-catch wrap Certificate action dengan structured logging — TempData["Error"] generic Bahasa Indonesia, TIDAK leak stack trace | dotnet build + grep | `dotnet build && grep -c '_logger.LogError(ex, "Certificate view failed' Controllers/CMPController.cs` | ✅ source | ⬜ pending |
| 309-01-T2 | 01 | 1 | WCRT-01 SC#4, #5 | T-309-01 | ResolveCategorySignatory wrap try-catch dengan _logger.LogWarning fallback signatory; Certificate.cshtml line 227 null-safe | dotnet build + grep | `dotnet build && grep -F '@(Model.User?.FullName ?? "(Nama tidak tersedia)")' Views/CMP/Certificate.cshtml` | ✅ source | ⬜ pending |
| 309-01-T3 | 01 | 1 | WCRT-01 SC#6 | T-309-01 | Manual UAT 3-step — exotic User=null + Category=null tetap render dengan fallback | manual UAT | (checkpoint:human-verify; no automated) | manual-only | ⬜ pending |
| 309-02-T1 | 02 | 2 | SUB-01 SC#8 | T-309-05 | Constant + helper di AssessmentConstants — IsAssessmentSubmitted normalize Completed/PendingGrading | dotnet build + grep | `dotnet build && grep -F 'public static bool IsAssessmentSubmitted(string? status)' Models/AssessmentConstants.cs` | ✅ source | ⬜ pending |
| 309-02-T2 | 02 | 2 | SUB-01 SC#9, #10 (CMPController + ViewModel portion) | T-309-05, T-309-06 | 3 lokasi swap status check (TempData["Error"] BI "Assessment belum selesai." per CLAUDE.md — iter-1 fix Warning #1) + 2 PendingGrading branch + ViewModel projection IsPendingGrading + **Essay items TETAP di list dengan IsEssayPending flag (CONTEXT D-08 lock — iter-1 fix Blocker #1)** | dotnet build + grep | `dotnet build && grep -c 'AssessmentConstants.IsAssessmentSubmitted(assessment.Status)' Controllers/CMPController.cs` (≥ 3) && `grep -c 'TempData\["Error"\] = "Assessment belum selesai\.";' Controllers/CMPController.cs` (≥ 3) && `grep -F 'IsEssayPending = (assessment.Status == AssessmentConstants.AssessmentStatus.PendingGrading' Controllers/CMPController.cs` | ✅ source | ⬜ pending |
| 309-02-T3 | 02 | 2 | SUB-01 SC#10 (Layout portion) | T-309-04 | TempData["Info"] alert block di _Layout.cshtml — alert-info + bi-info-circle-fill, no XSS surface | dotnet build + grep | `dotnet build && grep -F '@if (TempData["Info"] != null)' Views/Shared/_Layout.cshtml` | ✅ source | ⬜ pending |
| 309-02-T4 | 02 | 2 | SUB-01 SC#10 (Results view) | — | Results.cshtml pending mode — banner "Hasil sementara", badge "MENUNGGU PENILAIAN", cert button HIDDEN, **Question Review Essay items label "Menunggu Penilaian" via q.IsEssayPending conditional (iter-1 D-08 fix)** | dotnet build + grep | `dotnet build && grep -F 'MENUNGGU PENILAIAN' Views/CMP/Results.cshtml && grep -E '@if \(.*\.IsEssayPending\)' Views/CMP/Results.cshtml` | ✅ source | ⬜ pending |
| 309-02-T5 | 02 | 2 | SUB-01 SC#11 + regression + iter-1 D-08 verify | T-309-04, T-309-05, T-309-06, T-309-07 | Manual UAT 6-step — full pending flow + regression Completed flow + **Essay items review tampil dengan label "Menunggu Penilaian" (iter-1 D-08 verify Step 4)** | manual UAT | (checkpoint:human-verify; no automated) | manual-only | ⬜ pending |
| 309-03-T1 | 03 | 2 | (opportunistic OQ#2 RESOLVED — split iter-1) | T-309-08 | GradingService literal "Menunggu Penilaian" line 196, 199 → constant; literal "Completed" line 196 juga konsisten; eliminate typo risk; depends_on=[309-02] untuk constant availability | dotnet build + grep | `dotnet build && grep -c '"Menunggu Penilaian"' Services/GradingService.cs` (= 0) && `grep -c 'AssessmentConstants.AssessmentStatus.PendingGrading' Services/GradingService.cs` (≥ 2) | ✅ source | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**iter-1 revision changes:**
- ✂ Removed: 309-02-T5 GradingService refactor (split ke Plan 309-03 Task 1) + 309-02-T6 manual UAT (renamed ke 309-02-T5 setelah Task 5 GradingService dihapus)
- ➕ Added: 309-03-T1 GradingService refactor (Wave 2 sequential, autonomous plan, depends_on=[309-02])
- 🔄 Updated 309-02-T2: tambah `IsEssayPending` projection (iter-1 D-08 fix Blocker #1) + TempData["Error"] BI compliance (iter-1 fix Warning #1)
- 🔄 Updated 309-02-T4: tambah Question Review Essay items label conditional (iter-1 D-08 fix)
- 🔄 Renamed 309-02-T6 → 309-02-T5 (manual UAT — task count plan 309-02 turun dari 6 ke 5)

---

## Wave 0 Requirements

**DECISION:** Phase 309 SKIPS Wave 0 E2E test scaffold. Reasoning:
- Pending-grading state seed via Playwright butuh DB seed helper (currently absent — would require new test infrastructure)
- Manual UAT 6-step Plan 309-02 Task 5 + 3-step Plan 309-01 Task 3 cover all 11 success criteria via human verification
- Grep-verifiable acceptance criteria di setiap task memastikan code pattern correctness pre-UAT
- Phase 310 (Essay Finalize Idempotency) yang sequential setelah Phase 309 akan establish grading-state E2E infrastructure jika diperlukan

If future Phase butuh PendingGrading E2E test:
- [ ] (FUTURE) `tests/e2e/helpers/assessmentSeedHelper.ts` — DB seed assessment session dengan Status="Menunggu Penilaian"
- [ ] (FUTURE) `tests/e2e/assessment-309.spec.ts` — describe block "Phase 309 SUB-01 pending state"

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions Source |
|----------|-------------|------------|--------------------------|
| Worker dengan exotic Category null/empty fallback "HC Manager" | WCRT-01 SC#6 | Sulit reproduce kondisi data exotic via E2E (data dependent + restore) | Plan 309-01 Task 3 Step 3 — DB edit Category=NULL, view Certificate, assert signatory "HC Manager" |
| Post-deploy LogError monitoring | WCRT-01 SC#7 | Production observability, bukan test code | Setelah deploy → monitor `_logger.LogError "Certificate view failed for session {Id}"` di Application Insights / file log untuk pin-point root cause aktual |
| Worker submit assessment ber-essay tidak menerima popup merah | SUB-01 SC#11 | E2E butuh DB seed pending state (out of scope Wave 0) | Plan 309-02 Task 5 Step 1+2 — submit essay → klik Sertifikat → assert banner BIRU info bukan popup merah error |
| Visual styling TempData["Info"] alert-info berbeda dari Error/Success | SUB-01 D-09 | Visual / a11y verification | Plan 309-02 Task 5 Step 6 — DevTools inspect class `alert alert-info`, verify icon + color + dismiss button |
| Regression Completed flow setelah finalize essay | SUB-01 SC#10 | Workflow integration test (HC finalize + worker re-view) | Plan 309-02 Task 5 Step 5 — manual SQL update Status='Completed' → worker view Certificate normal |
| Defensive Certificate User=null render "(Nama tidak tersedia)" | WCRT-01 SC#4 | Data integrity audit yang butuh DB edit + restore | Plan 309-01 Task 3 Step 2 — DB edit UserId=NULL atau code-side mock, view Certificate, assert text fallback |
| **Essay items review tampil label "Menunggu Penilaian" saat pending (iter-1 D-08)** | SUB-01 D-08 | Visual verification ViewModel field projection + Razor conditional render | Plan 309-02 Task 5 Step 4 — verify Essay items TETAP MUNCUL di "Tinjauan Jawaban" dengan badge ABU-ABU "Menunggu Penilaian" (BUKAN skipped); MC/MA tetap correct/incorrect badge normal |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (dotnet build) atau manual UAT checkpoint
- [x] Sampling continuity: dotnet build runs setelah setiap task commit
- [x] Wave 0 explicitly skipped per phase-specific decision (rationale documented)
- [x] No watch-mode flags
- [x] Feedback latency < 30s (dotnet build)
- [x] `nyquist_compliant: true` set di frontmatter
- [x] iter-1 revision: Plan 309-03 split + Essay items label fix + BI compliance fix all reflected in Per-Task Verification Map

**Approval:** ready (planner sign-off 2026-04-29 + iter-1 revision sign-off 2026-04-29)
