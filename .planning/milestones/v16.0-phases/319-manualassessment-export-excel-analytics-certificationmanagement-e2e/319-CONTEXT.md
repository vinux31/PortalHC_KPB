# Phase 319: ManualAssessment + Export Excel + Analytics + CertificationManagement E2E - Context

**Gathered:** 2026-05-12
**Status:** Ready for planning
**Mode:** `--auto` (Claude picked recommended defaults inline; user-review-friendly)

<domain>
## Phase Boundary

Test coverage E2E (via Playwright) untuk **admin features** Portal HC yang BUKAN exam-taking flow:

1. **ManualAssessment** (HC manual entry skor tanpa peserta exam) — `TrainingAdminController.cs:555-740` (Add/Edit/Delete)
2. **ManageCategories** CRUD — `AssessmentAdminController.cs:368`
3. **Export Excel** endpoint — 8 controllers expose Excel export (CMP/CDP/CoachMapping/Worker/dll); pilih representative subset
4. **Analytics dashboard** — `CMPController.cs:2493-2745` (AnalyticsDashboard + GetAnalyticsData + Cascade endpoints, Chart.js v4 horizontal bar)
5. **CertificationManagement** — `CMPController.cs:3666-3776` (CMP variant only; CDP variant `CDPController.cs:3539` DEFERRED Phase 320+)

Out of scope: certificate reissue workflow detail, sertifikat lookup deep-search, multi-tenancy admin (UAT scope, not test infra).

</domain>

<decisions>
## Implementation Decisions

### FLOW Structure (D-319-01)

Plan 5 FLOWs (one per feature group), append ke `tests/e2e/exam-types.spec.ts` (preserve consistency dengan Phase 317+318 helpers stack):

- **FLOW T** — ManualAssessment full CRUD (T1-T6: HC create/edit/delete + DB verify + Worker-side visibility)
- **FLOW U** — ManageCategories CRUD (U1-U4: HC create category + edit + delete + duplicate-name reject)
- **FLOW V** — Export Excel endpoint (V1-V3: APIRequest GET /Admin/ExportAssessmentMatrix → content-type `application/vnd.ms-excel` atau `xlsx` + bytes>2048 + filename pattern)
- **FLOW W** — Analytics dashboard (W1-W4: HC navigate `/CMP/AnalyticsDashboard` → JSON endpoint `GetAnalyticsData` intercept → assert data shape + Chart.js canvas present)
- **FLOW X** — CertificationManagement (X1-X3: HC navigate `/CMP/CertificationManagement` page → filter by title → detail page navigation)

**Sub-test target:** 20+ total (T 6 + U 4 + V 3 + W 4 + X 3 = 20). Cumulative target `exam-types.spec.ts` post-319: **49 + 20 = 69 sub-tests**.

### Wave Structure (D-319-02)

- **Wave 1 (parallel-eligible):** FLOW T + FLOW U (different controllers, no shared state).
- **Wave 2 (sequential — file-level lock on exam-types.spec.ts):** FLOW V → FLOW W → FLOW X.

Plan structure: 4 plans:
- 319-01-PLAN: helpers extension (Export Excel + Analytics JSON intercept helpers) + FLOW T (ManualAssessment 6 sub-tests)
- 319-02-PLAN: FLOW U (ManageCategories 4 sub-tests) — sequencing setelah Plan 01 untuk helpers landed
- 319-03-PLAN: FLOW V (Export Excel 3 sub-tests) + FLOW W (Analytics 4 sub-tests)
- 319-04-PLAN: FLOW X (CertificationManagement 3 sub-tests) + docs finalize (REQUIREMENTS QA-09 entry + ROADMAP sync + closure report + final regression gate ≥69/69)

### Export Excel Verification (D-319-03)

Adopt **APIRequest pattern** (sama dengan Phase 318 Plan 04 `verifyCertificatePdfDownload`):
- `page.request.get('/Admin/{ExportEndpoint}')` — cookies inherited dari page context
- Assertions:
  - status 200
  - content-type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` ATAU `application/vnd.ms-excel`
  - content-disposition `attachment.*\.xlsx`
  - body bytes > 2048 (Excel file size minimum guard)
- Helper: `verifyExcelDownload(page, endpointPath)` di `examTypes.ts` (consistency dengan `verifyCertificatePdfDownload`)

Cover 1 representative endpoint untuk efficiency — pilih CMP atau Admin level (TBD di research). Skip multi-endpoint coverage.

**AMENDED 2026-05-12:** Endpoint final = `/AssessmentAdmin/ExportCategoriesExcel` (paling sederhana, no params). Hipotetis `/Admin/ExportAssessmentMatrix` TIDAK exist di codebase (verified via grep — RESEARCH Pitfall 1). User-confirmed via plan-phase orchestrator AskUserQuestion.

### Analytics Chart Assertion Strategy (D-319-04)

Chart.js canvas adalah opaque rendering — DOM-text scrape tidak reliable. Strategy:
1. **Primary:** JSON endpoint intercept (`page.waitForResponse` URL match `/CMP/GetAnalyticsData`) → parse response → assert data shape (datasets[].data > 0, labels array, axis config)
2. **Secondary:** DOM smoke — assert `canvas[id^="analyticsChart"]` visible + `data-loaded` attribute atau ChartJS lifecycle hook
3. **DB cross-check:** Query AssessmentSessions COUNT by category → compare dengan API response totals

Helper: `interceptAnalyticsResponse(page, params)` di `examTypes.ts` — wrap `waitForResponse` + JSON parse + return typed shape.

### CertificationManagement Scope (D-319-05)

- ~~**CMP variant only** (`CMPController.cs:3666`)~~ — **AMENDED 2026-05-12:** switch ke **CDP variant** (`/CDP/CertificationManagement`, `Views/CDP/CertificationManagement.cshtml` exists). CMP variant abandoned karena `Views/CMP/CertificationManagement.cshtml` MISSING → controller `return View(vm)` produces 500. Verified via filesystem check + RESEARCH Pitfall 2. User-confirmed via plan-phase orchestrator AskUserQuestion (DISCUSSION-LOG separate event).
- CDP variant becomes **PRIMARY** untuk Phase 319 FLOW X (3 sub-tests X1-X3 unchanged: navigate, filter, detail).
- CMP variant variant **DEFERRED Phase 320+** — requires view file creation + controller fix (production bug separate scope).
- Skip: reissue workflow, multi-page pagination edge cases, search-by-NomorSertifikat (separate phase kalau muncul UAT bug)

### ManageCategories Scope (D-319-06)

- 4 sub-tests CRUD basic happy-path + 1 negative (duplicate name reject)
- U1: HC navigate `/Admin/ManageCategories` + create category 'OJT-Phase319'
- U2: HC edit category name
- U3: HC delete category
- U4: HC create duplicate name → DB unique constraint violation visible via `.alert-danger` atau form error
- Cleanup: U3 already deletes happy-path category; matrix snapshot RESTORE handles edge cleanup

### ManualAssessment Scope (D-319-07)

- 6 sub-tests cover full CRUD lifecycle + worker visibility
- T1: HC navigate `/TrainingAdmin/AddManualAssessment` form
- T2: HC submit form (fields: title, category, peserta, score 0-100, completed date)
- T3: DB verify AssessmentSession Status='Completed' + Score persisted + Type='Manual'
- T4: HC edit ManualAssessment + Score update
- T5: Worker view `/CMP/Assessment` — ManualAssessment visible di "Completed" tab (worker-side visibility, no Start button)
- T6: HC delete ManualAssessment + DB row removed

### Helper Extensions (D-319-08)

Append ke existing `tests/e2e/helpers/examTypes.ts` (consistency):
- `verifyExcelDownload(page, endpointPath): Promise<{ bytes; filename }>` — Plan 01 (atau Plan 03)
- `interceptAnalyticsResponse(page, params): Promise<{ datasets; labels }>` — Plan 03
- Optional: `createManualAssessment(page, opts)` helper — Plan 01 (HC submit form wrapper)

JANGAN bikin new helper file untuk Phase 319 (single file consolidation rule, Phase 317-318 precedent).

### Requirements Mapping (D-319-09)

ROADMAP.md saat ini placeholder `QA-04 (admin features coverage)` — tapi REQUIREMENTS.md `QA-04` = "Visual regression". Conflict. Plan 04 (docs finalize) fix mapping:
- Add **QA-09** ke REQUIREMENTS.md Future Requirements: "Admin features E2E coverage — ManualAssessment CRUD, ManageCategories CRUD, Export Excel endpoint validation, Analytics dashboard JSON+DOM verification, CertificationManagement listing+filter."
- Sync ROADMAP Phase 319 `Requirements` line: `QA-04` → `QA-09`
- Append Traceability row: `| QA-09 | 319 | Complete |`

### Claude's Discretion

- Exact Plan numbering split (3 plans vs 4 plans) — planner decides at `/gsd-plan-phase 319`
- Per-FLOW selector pattern (data-testid vs class+text) — researcher reads existing patterns
- Test runtime budget per sub-test — planner sets (default 60s typical)
- Wave 0 smoke for Wave 0 YELLOW assumptions (Analytics JSON shape unknown until first inspection) — planner adds W0.x sub-test kalau researcher flags YELLOW

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase 318 mature foundation
- `tests/e2e/exam-types.spec.ts` — 49 sub-tests cumulative (Phase 317 27 + Phase 318 21)
- `tests/e2e/helpers/examTypes.ts` — 10 exports + `verifyCertificatePdfDownload` (APIRequest pattern reference)
- `tests/e2e/helpers/wizardSelectors.ts` — selectors stack (preserve pattern)
- `tests/helpers/utils.ts` — `today()`, `yesterday()`, `tomorrow()`, `uniqueTitle()`
- `tests/helpers/dbSnapshot.ts` — `queryScalar` (numeric), `queryString` (text)

### Phase 319 production code (subjects of testing)
- `Controllers/TrainingAdminController.cs:555-740` — ManualAssessment Add/Edit/Delete
- `Controllers/AssessmentAdminController.cs:368` — ManageCategories action
- `Controllers/CMPController.cs:2493-2745` — AnalyticsDashboard + GetAnalyticsData
- `Controllers/CMPController.cs:3666-3776` — CertificationManagement (CMP variant)
- `Controllers/CMPController.cs` (search "ExportToExcel") — Excel export endpoint TBD by researcher

### Workflow + Standards
- `CLAUDE.md` — DEV_WORKFLOW (lokal-first verify, Team IT promosi Dev/Prod, flag "no migration")
- `docs/DEV_WORKFLOW.md` — environment map + SOP migration
- `docs/SEED_WORKFLOW.md` — temporary seed data classification + SQL Server BACKUP/RESTORE
- `.planning/REQUIREMENTS.md` — QA-01..QA-08 (Phase 319 adds QA-09)
- `.planning/ROADMAP.md` — Phase 319 entry (line ~401-406)

### Phase 318 SUMMARYs (architectural findings reference)
- `.planning/phases/318-*/318-02-SUMMARY.md` — SURF-317-A fix (CMPController Results MA-aware)
- `.planning/phases/318-*/318-03-SUMMARY.md` — PrePost shared-package pool design
- `docs/test-reports/2026-05-12-phase-318-summary.md` — Phase 318 closure report

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets

- **`verifyCertificatePdfDownload`** (Phase 318 Plan 04) — APIRequest pattern proven. Adapt jadi `verifyExcelDownload` (different content-type, different filename pattern).
- **`createAssessmentViaWizard`** (Phase 317) — wizard 4-step pattern. ManualAssessment likely BUKAN wizard (single-form); buat new helper `createManualAssessmentViaForm` kalau form structure cukup kompleks.
- **`db.queryString` + `queryScalar`** — proven untuk DB cross-check pattern (Phase 318 P/Q/R Plan).
- **`uniqueTitle`** — title isolation untuk parallel test safety.

### Established Patterns

- **Direct API approach untuk binary/download** — APIRequest cookies-inherited (Phase 318 Plan 04 R4 verified).
- **JSON endpoint intercept** — `page.waitForResponse(r => r.url().includes('endpoint'))` + `.json()` parse (Phase 318 Plan 03 P6 fallback strategy reference).
- **Sequential file-level `test.describe.configure({ mode: 'serial' })`** — preserve untuk per-FLOW shared-state pattern.
- **`[318-X]` title marker convention** — extend ke `[319-T]`, `[319-U]`, dst untuk test data isolation + grep-able dev DB cleanup.

### Integration Points

- New FLOWs appended di END of `tests/e2e/exam-types.spec.ts` (sebelum `void verifyResultPage`).
- Helpers extension di END of `tests/e2e/helpers/examTypes.ts` (sama pattern Plan 03+04).
- `tests/helpers/utils.ts` — no new utility needed (today/yesterday/uniqueTitle sufficient untuk Phase 319).

</code_context>

<specifics>
## Specific Ideas

- **Excel content-type discovery:** Researcher should grep `Response.ContentType` di production controllers untuk verify exact MIME string (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` standard untuk `.xlsx`; `application/vnd.ms-excel` legacy untuk `.xls`).
- **Analytics chart canvas selector:** Chart.js v4 mounted di `<canvas>` element. Use `canvas#chartId` selector + `attached` state — bukan visibility (canvas drawing internal).
- **CertificationManagement filter UI:** Likely Bootstrap form dengan input + button → POST/AJAX. Plan researcher mengkonfirmasi pattern.

</specifics>

<deferred>
## Deferred Ideas

- **CDP CertificationManagement variant** (CDPController.cs:3539) — Phase 320+ wholesale CDP coverage
- **Search-by-NomorSertifikat UAT scenarios** — separate phase kalau UAT muncul bug
- **Excel re-query independent verification** (per Phase 301 D-19) — already proven, no new test needed
- **Multi-page pagination edge cases** (CertificationManagement page>1) — happy-path single page sufficient untuk Phase 319 scope
- **ManualAssessment bulk import** — feature TBD apakah exist; out of scope kalau ada
- **Analytics drill-down per-employee** — out of scope (dashboard summary only)

### Reviewed Todos (not folded)
[None — no pending todos matched Phase 319 scope at discuss time]

</deferred>

---

*Phase: 319-manualassessment-export-excel-analytics-certificationmanagement-e2e*
*Context gathered: 2026-05-12*
