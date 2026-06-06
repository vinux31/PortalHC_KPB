---
phase: 321
slug: assessment-edit-jawaban-peserta
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-21
revised: 2026-05-21 (iteration 1 — plan split 4→5)
---

# Phase 321 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Stack: .NET 8 Razor + EF Core 8 + SignalR + Playwright (TypeScript). Project test infra: `dotnet build` + browser UAT + Playwright opportunistic (per CONTEXT D-11 + Phase 320 precedent).
>
> **Revision iteration 1 (user-amendment 4-PLAN → 5-PLAN):** Old PLAN 03 awal 7 tasks di-split jadi PLAN 03 (4 tasks: GET + View + JS + Preview) + PLAN 04 (3 tasks: POST + Dropdown + SignalR). Old PLAN 04 di-rename jadi PLAN 05. Task ID di-renumber + Wave 3 controller rows defer Playwright reference ke Wave 5 (rows aktual pakai `dotnet build` + grep).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | `dotnet build` (compile gate) + Playwright TypeScript (E2E gate, `tests/e2e/*.spec.ts`) + manual UAT (browser + sqlcmd DB verify) |
| **Config file** | `playwright.config.ts` (existing); `HcPortal.csproj` (existing); no xUnit/NUnit project (CONTEXT D-08/D-11) |
| **Quick run command** | `dotnet build` (compile + analyzer warning gate, ~15s) |
| **Full suite command** | `dotnet build && npx playwright test tests/e2e/edit-peserta-answers.spec.ts` (~3-5 min including auth fixtures) |
| **Estimated runtime** | ~15s build + ~3 min Playwright = ~3.5 min full |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (compile gate must pass — per `docs/DEV_WORKFLOW.md §5` pre-commit checklist)
- **After every plan wave:** `dotnet build` + browser smoke verify (`dotnet run` + manual click-through golden path)
- **After PLAN 05 (UAT) execute:** Full Playwright suite (HARD GATE 4/4 pass — no bypass) + 4 manual UAT items (DB cascade flip / SignalR cross-tab / Activity Log tab / migration rollback)
- **Before `/gsd-verify-work`:** Full suite must be green + manual UAT 4/4 ticked
- **Max feedback latency:** ~20 seconds per task (compile only); 3-5 min per wave (Playwright)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 321-01-01 | 01 | 1 | EDIT-13 | T-321-01 (audit integrity) | Model + DbSet + index + migration | build | `dotnet build` | ❌ W0 (Models/AssessmentEditLog.cs) | ⬜ pending |
| 321-01-02 | 01 | 1 | EDIT-13 | T-321-01 | Migration apply + rollback test lokal | manual | `dotnet ef database update {prev} && dotnet ef database update` | N/A (migration file) | ⬜ pending |
| 321-01-03 | 01 | 1 | EDIT-02 | T-321-02 (eligibility gating) | `IsEditable` helper Completed + !ManualEntry + !Proton T3 | build | `dotnet build` | ❌ W0 (Helpers/AssessmentEditEligibility.cs) | ⬜ pending |
| 321-01-04 | 01 | 1 | EDIT-02, EDIT-06 | T-321-02 | ViewModel DTOs (EditPesertaAnswersViewModel + EditAnswersSubmission + EditDraftSubmission) | build | `dotnet build` | ❌ W0 (Models/EditPesertaAnswersViewModel.cs + Models/EditAnswersSubmission.cs) | ⬜ pending |
| 321-02-01 | 02 | 2 | EDIT-03 | — | `ComputeScoreAndETInternalAsync(session, overrideAnswers?)` extracted no-side-effect | build | `dotnet build` | ❌ W0 (Services/GradingService.cs refactor) | ⬜ pending |
| 321-02-02 | 02 | 2 | EDIT-03 | T-321-03 (recompute integrity) | `RegradeAfterEditAsync(session)` DELETE+recompute+ExecuteUpdateAsync status guard | build | `dotnet build` | ❌ W0 (GradingService.cs add) | ⬜ pending |
| 321-03-01 | 03 | 3 | EDIT-01 | T-321-04 (auth gating) | `GET EditPesertaAnswers` `[Authorize(Roles="Admin, HC")]` + IsEditable check | build+grep | `dotnet build && grep -q "EditPesertaAnswers(int id)" Controllers/AssessmentAdminController.cs && grep -q "Roles = \"Admin, HC\"" Controllers/AssessmentAdminController.cs` | ❌ W0 (controller action) — Playwright auth-gate test deferred to Wave 5 (321-05-02) | ⬜ pending |
| 321-03-02 | 03 | 3 | EDIT-01, EDIT-05 | T-321-05 (form CSRF) | View `EditPesertaAnswers.cshtml` per-question form + reason dropdown + anti-forgery token + hidden UpdatedAt | build+grep | `dotnet build && grep -q "@Html.AntiForgeryToken" Views/Admin/EditPesertaAnswers.cshtml && grep -q "Soal salah / typo" Views/Admin/EditPesertaAnswers.cshtml` | ❌ W0 (Views/Admin/EditPesertaAnswers.cshtml) | ⬜ pending |
| 321-03-03 | 03 | 3 | EDIT-05 | T-321-05 | Frontend JS dirty state + reason validation (Lainnya wajib free-text) + flip modal AJAX | grep | `grep -q "menggagalkan peserta" wwwroot/js/edit-peserta-answers.js && grep -q "meluluskan peserta" wwwroot/js/edit-peserta-answers.js && grep -q "hidden.bs.modal" wwwroot/js/edit-peserta-answers.js` — manual browser dev tools confirm | ❌ W0 (wwwroot/js/edit-peserta-answers.js) | ⬜ pending |
| 321-03-04 | 03 | 3 | EDIT-10 | T-321-08 (preview contract) | `POST PreviewEditScore` dry-run JSON `{oldScore, newScore, oldIsPassed, newIsPassed, hasCert, willGenerateCert}` | build+grep | `dotnet build && grep -q "PreviewEditScore" Controllers/AssessmentAdminController.cs && grep -q "_gradingService.PreviewScoreAsync" Controllers/AssessmentAdminController.cs && grep -q "willGenerateCert" Controllers/AssessmentAdminController.cs` — Playwright flip-preview test deferred to Wave 5 (321-05-02) | ❌ W0 (controller action) | ⬜ pending |
| 321-04-01 | 04 | 4 | EDIT-02..08 | T-321-06 (transaction integrity), T-321-07 (concurrency) | `POST SubmitEditAnswers` transaction scope edit+audit+regrade+cascade + UpdatedAt token | build+grep | `dotnet build && grep -q "SubmitEditAnswers" Controllers/AssessmentAdminController.cs && grep -q "BeginTransactionAsync" Controllers/AssessmentAdminController.cs && grep -q "Sesi sudah diubah admin lain" Controllers/AssessmentAdminController.cs` — Playwright happy-path + concurrency test deferred to Wave 5 (321-05-02) | ❌ W0 (controller action) | ⬜ pending |
| 321-04-02 | 04 | 4 | EDIT-12 | T-321-09 (UI gating) | Dropdown ⋮ hybrid `AssessmentMonitoringDetail.cshtml` + `IsEditable` conditional render + ARIA | build+grep | `dotnet build && grep -q "AssessmentEditEligibility.IsEditableShallow" Views/Admin/AssessmentMonitoringDetail.cshtml && grep -q "dropdown-menu-end" Views/Admin/AssessmentMonitoringDetail.cshtml` | ❌ W0 (modify view) | ⬜ pending |
| 321-04-03 | 04 | 4 | EDIT-09 | — | SignalR frontend handler `workerAnswerEdited` + `showAssessmentToast` extended dengan `delayMs` param (D-07 8s LOCKED) | build+grep | `dotnet build && grep -q "delayMs" wwwroot/js/assessment-hub.js && grep -q "showAssessmentToast.*8000" Views/Admin/AssessmentMonitoringDetail.cshtml && grep -q "workerAnswerEdited" Views/Admin/AssessmentMonitoringDetail.cshtml` — manual 2-tab browser cross-tab verify | ❌ W0 (modify hub.js + view) | ⬜ pending |
| 321-05-01 | 05 | 5 | EDIT-11 | T-321-10 (audit visibility) | Activity Log Edit History tab + lazy-load partial `_EditHistoryPartial.cshtml` filtered by SessionId sort EditedAt DESC | build+grep | `dotnet build && grep -q "EditHistoryPartial" Controllers/AssessmentAdminController.cs && grep -q "tab-edit-history" Views/Admin/AssessmentMonitoringDetail.cshtml && grep -L "@Html.Raw" Views/Admin/_EditHistoryPartial.cshtml \| grep -q .` | ❌ W0 (modify modal + new partial) | ⬜ pending |
| 321-05-02 | 05 | 5 | EDIT-01, EDIT-03, EDIT-06, EDIT-07, EDIT-10 | T-321-04, T-321-06, T-321-07, T-321-08 | Playwright 4-test HARD GATE (auth-gate + happy-path + concurrency-stale + flip-preview-AJAX) — SEED_WORKFLOW pre+cleanup | playwright | `npx playwright test tests/e2e/edit-peserta-answers.spec.ts` (4/4 pass HARD GATE) | ❌ W0 (tests/e2e/edit-peserta-answers.spec.ts + SEED journal) | ⬜ pending |
| 321-05-03 | 05 | 5 | EDIT-04, EDIT-09, EDIT-11, EDIT-13 | T-321-11 | Manual UAT 4-item: DB cascade flip Pass↔Fail via sqlcmd (SEED_WORKFLOW pre+cleanup) + SignalR 2-tab live update (8s toast) + Activity Log Edit History tab content + Migration rollback re-verify + Tag + Merge + Push + IT notify | manual | sqlcmd queries + 2-tab browser observe + dotnet ef database update + git tag/merge/push | N/A (UAT + git ops) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/edit-peserta-answers.spec.ts` — Playwright test suite (4 test: auth-gate / happy-path-edit-save / concurrency-stale / flip-preview-AJAX) per CONTEXT D-04 — **created Wave 5** (PLAN 05 Task 2)
- [ ] `tests/e2e/fixtures/auth.ts` — REUSE existing fixture (Phase 320 precedent `tests/e2e/export-per-peserta.spec.ts` pattern), no new framework install
- [ ] `tests/e2e/fixtures/accounts.ts` — REUSE existing (10 role credentials all password `123456`, `coachee` key = Worker negative test)
- [ ] `playwright.config.ts` — verify base URL `http://localhost:5277` + storageState pattern existing
- [ ] NO new test framework install (CONTEXT D-08/D-11 — no xUnit/NUnit project; `dotnet build` + Playwright + manual UAT only)
- [ ] SEED_WORKFLOW pre+cleanup tracked di `docs/SEED_JOURNAL.md` (PLAN 05 Task 2 + Task 3 UAT 1)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| DB cascade flip Pass→Fail | EDIT-04 | Need DB direct query (Playwright tidak praktis untuk SQL Server `NomorSertifikat IS NULL` + `TrainingRecord.Status = 'Failed'` assertion) | `sqlcmd -S localhost\SQLEXPRESS -d HcPortalDB_Dev -Q "SELECT NomorSertifikat, ValidUntil FROM AssessmentSessions WHERE Id={sessionId}; SELECT Status FROM TrainingRecords WHERE UserId='{userId}' AND PackageId={packageId}"` setelah Playwright happy-path edit Pass→Fail. **Pre+cleanup via SEED_WORKFLOW.md** (PLAN 05 Task 3 UAT 1). |
| DB cascade flip Fail→Pass | EDIT-04 | Same — verify `NomorSertifikat NOT NULL` + retry behavior + `TrainingRecord.Status = 'Passed'` insert/upsert | Same sqlcmd post-Playwright Fail→Pass edit |
| SignalR cross-tab live update | EDIT-09 | Requires 2 browser tab visual observation (toast pop + row score update no refresh + 8s timeout D-07 LOCKED) — Playwright 2-context support exist tapi visual diff `data-testid` row+toast assertion brittle | Buka 2 tab `/Admin/AssessmentMonitoringDetail/{batchKey}` — Admin di tab 1 edit jawaban → observe tab 2 row score cell + toast verbose audit-style muncul tanpa refresh manual (timeout 8 detik) |
| Activity Log Edit History tab content | EDIT-11 | Modal lazy-load + visual timeline format ("[timestamp] Soal #N: 'QuestionText' — [Old] → [New] oleh ActorRole (ActorName). Alasan: ReasonLabel") best verified visually | Buka monitoring detail → klik 🕐 → klik tab "Edit History" → verify timeline entries match `AssessmentEditLog` table state |
| Migration apply + rollback lokal | EDIT-13 | `dotnet ef` interactive CLI — Playwright tidak relevan | `dotnet ef database update {PreviousMigrationName} --context ApplicationDbContext` → verify `AssessmentEditLogs` table DROP via sqlcmd `SELECT name FROM sys.tables WHERE name='AssessmentEditLogs'` empty → `dotnet ef database update --context ApplicationDbContext` re-apply → verify table re-CREATE |
| Pre-commit checklist `docs/DEV_WORKFLOW.md §5` | All REQ | Build + run + browser verify + DB lokal verify + notify IT post-push — process-level, not code-level. **Per-task `<verify><automated>` `dotnet build` gate + 1-commit-per-task cadence = operational embodiment of §5** (per PLAN 01 objective note) | Tick per commit per CLAUDE.md mandate |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies (W0 = Playwright spec file create on Wave 5)
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify (every task hits `dotnet build` minimum; Playwright covers Wave 5 critical paths)
- [ ] Wave 0 covers all MISSING references (Playwright spec file + reuse existing fixtures + SEED_JOURNAL entries)
- [ ] No watch-mode flags (Playwright `--headed --debug` excluded — CI mode default)
- [ ] Feedback latency < 20s per task commit (compile gate); < 5 min per wave (Playwright + manual smoke)
- [ ] `nyquist_compliant: true` set in frontmatter (toggle after planner + checker approve all PLAN files)
- [ ] **Iteration 1 amendment:** Task ID mapping renumber (old 03-04..07 → 04-01..03 → 05-01..03) + Wave 3 rows defer Playwright references ke Wave 5 + D-07 8s LOCKED + SEED_WORKFLOW pre/cleanup gating

**Approval:** pending (will be approved after `/gsd-plan-phase 321 --research` completes + `gsd-plan-checker` PASS iteration 2)
</content>
