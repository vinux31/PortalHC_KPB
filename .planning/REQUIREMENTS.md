# Requirements — v20.0 CMP Records Overhaul + Cilacap UX/Restore

**Milestone:** v20.0
**Started:** 2026-05-30
**Status:** Active (3-phase sequential plan, target Phase 336-338)

> Fresh slate post v18.0 + v19.0 close (2026-05-29). Previous requirements archived:
> - `milestones/v18.0-REQUIREMENTS.md` (CASCADE-01 + DUPL-01..05)
> - `milestones/v19.0-REQUIREMENTS.md` (SEC-01..03 + FOUNDATION + CLOSURE + VAL-01..02 + TZ-01 + CSCD-AUDIT + CSCD-01..07)

## Goal

Tutup 3 PR pending: (1) CMP/Records full overhaul (filter silent-fail + data integrity + arch SQL push-down + a11y), (2) 6 gap UX Cilacap incident (Admin Assessment Monitoring + Excel breakdown + BulkPdf), (3) Investigate + restore PreTest OJT GAST Cilacap data loss + naming convention + guardrail backup.

**Bundling rationale:** Cilacap incident (2026-05-29) discovered concurrent dengan CMP/Records audit (2026-05-27). Cross-link: Gap #5 Excel breakdown (PR #2) = enabler restore Option C (PR #3). PR #1 independent track, zero file overlap PR #2/#3 method-level.

## Target Phase Plan (Sequential Opsi 2)

```
Phase 336 → Phase 337 → Phase 338
  ↓           ↓           ↓
PR#3 invest. PR#1 CMP    PR#2 Cilacap UX + PR#3 restore execute
~1-2 hari   ~1 minggu+  ~1 minggu
```

## v20.0 Requirements

### Category 1: CMP Records Overhaul (Phase 337) — Approach C Full

**Filter + Data Integrity (Critical):**
- [ ] **CMP-01**: Status filter ("Sudah"/"Belum") works without Category dependency (B-01 fix: `WorkerDataService.cs:302` guard removal)
- [ ] **CMP-02**: Sub Category dropdown filter wired to server-side query (B-02 fix: `CMPController.cs:740-758` forward param)
- [ ] **CMP-03**: Category filter narrows worker list, bukan cuma rewrite CompletionPercentage (B-03 fix)
- [ ] **CMP-04**: Category match uses equality, BUKAN `.Contains` substring (B-04 fix: `WorkerDataService.cs:287`)
- [ ] **CMP-05**: Export endpoints honor subCategory filter (B-05 fix: `ExportRecordsTeamTraining`)
- [ ] **CMP-06**: Status derivation distinguish Completed+null IsPassed dari Failed (B-06 fix: `GetUnifiedRecords:51`)
- [ ] **CMP-07**: Training `SertifikatUrl` rendered di kolom Sertifikat My Records (B-07 fix: `Records.cshtml:184-199`)
- [ ] **CMP-08**: Training `Status="Permanent"` tampil indikator permanen (B-08 fix)
- [ ] **CMP-09**: HTML entity search fix (`data-title.ToLower()` HtmlEncode) (B-09 fix)
- [ ] **CMP-10**: Per-filter counter di My Records (B-10 fix)
- [ ] **CMP-11**: AttemptNumber correct walau Title null (B-11 fix: `WorkerDataService.cs:129`)

**UX + Race Safety:**
- [ ] **CMP-12**: AbortController di `filterTeamTable` AJAX (U-01 fix)
- [ ] **CMP-13**: Export URL sync sebelum klik (U-02 fix: `updateExportLinks` ordering)
- [ ] **CMP-14**: Sub Category dropdown disabled saat no children (U-03 fix)
- [ ] **CMP-15**: Date filter UX hint saat counter turun (U-04 fix)
- [ ] **CMP-16**: My Records ↔ Team View filter parity (U-05 fix)
- [ ] **CMP-17**: Filter state persist antar switch tab (U-06 fix)
- [ ] **CMP-18**: Hapus dead `data-*` attributes (U-07 fix: `_RecordsTeamBody.cshtml:47-56`)

**Code Quality:**
- [ ] **CMP-19**: Row keyboard-navigable (no inline onclick) (C-01 fix)
- [ ] **CMP-20**: Tab nav ARIA roles (`role="tab"` + `aria-controls` + `aria-selected`) (C-02 fix)
- [ ] **CMP-21**: ViewModel ganti duplicate `UserManager.GetUserAsync` di view (C-03 fix)
- [ ] **CMP-22**: Single source roleLevel check (controller, bukan view) (C-04 fix)
- [ ] **CMP-23**: Memoize year list di view (C-05 fix)

**Architecture:**
- [ ] **CMP-24**: SQL push-down `GetAllWorkersHistory` (A-01 fix)
- [ ] **CMP-25**: Date filter SQL where (A-02 fix: `WorkerDataService.cs:229-241`)
- [ ] **CMP-26**: Pagination Team View (A-03 fix)

### Category 2: Cilacap UX 6 Gap (Phase 338 Wave 1-3)

- [ ] **CIL-01**: Filter default ManageAssessment + AssessmentMonitoring tab "Semua Status" ATAU badge counter Closed (Gap #1)
- [ ] **CIL-02**: Search "Cilacap" Semua Status return parent group — query aggregation include Closed (Gap #2)
- [ ] **CIL-03**: History tab row clickable / kolom Actions → drill-down `/CMP/Results/{sessionId}` (Gap #3)
- [ ] **CIL-04**: Banner alert di `/CMP/Assessment` admin/HC role → redirect `/Admin/ManageAssessment?tab=history` (Gap #4)
- [ ] **CIL-05** (HIGH PRIORITY): `ExportAssessmentResults` Excel +sheet "Detail Per Soal" + "Elemen Teknis" (Gap #5)
- [ ] **CIL-06**: Endpoint `/Admin/BulkExportPdf` ZIP via QuestPDF — 1 PDF per peserta (Gap #6)

### Category 3: PreTest Cilacap Investigation + Restore (Phase 336 + Phase 338 Wave 4-5)

- [ ] **REST-01** (Phase 336): Investigate git log Mar 30–May 19, identifikasi migration culprit (kandidat: `b89b6559` SamePackage, `a7bb443e` AddAssessmentV14Columns, `569eb0a8` 7 kolom baru, `f82bad2e` Rubrik/Essay)
- [ ] **REST-02** (Phase 336): Confirm root cause (migration drop / EnsureCreated reset / seed reset / manual)
- [ ] **REST-03** (Phase 336): Decide restore strategy A (re-import manual) / B (skip) / C (tunggu Gap #5)
- [ ] **REST-04** (Phase 338 Wave 4): Implement chosen restore strategy
- [ ] **REST-05** (Phase 338 Wave 5): Guardrail pre-deploy backup SQL Server dump `.bak` `AssessmentSessions` + `AssessmentAttemptHistory` + `PackageUserResponses` SEBELUM migration
- [ ] **REST-06** (Phase 338 Wave 5): Document naming convention "{Pre|Post} Test {Track} {Lokasi}" + enforce `LinkedGroupId` auto-pair Pre/Post di admin create form
- [ ] **REST-07** (Phase 338 Wave 5): Update `docs/DEV_WORKFLOW.md` dgn backup SOP

## Out of Scope

- **Backlog carry-over** (Phase 235 / 247 / 281 / 285 / 293 / 297 / 298 / 303 / EPRV-01) — defer ke v21.0 atau later milestone. Reason: v20.0 focus tutup 3 PR baru, hindari scope creep.
- **Pre-Post Renewal otomatis** (Phase 297 undecided) — out, butuh user decision separate.
- **Essay nvarchar(max) decision** (Phase 298 undecided) — out, butuh user decision separate.
- **AI-generated coaching summaries** — eksplorasi, belum mature.
- **Competency heatmap** — eksplorasi, defer.
- **Phase 322 HTMX filter conversion** — sudah SHIPPED v17.0, BUKAN duplicate dengan CIL-01/CIL-02 (beda concern).

## Carry-over Backlog (deferred ke v21.0+)

- [ ] **EPRV-01** (v15.0): Preview Essay rubrik/jawaban
- [ ] **Phase 303 Plan 02 Task 3** (v14.0 UAT): Coach Workload 12-step
- [ ] **Phase 235** (v8.0 UAT): 5 items human verify
- [ ] **Phase 247**: 2 TODO approval chain
- [ ] **Phase 297**: Pre-Post Renewal automation decision
- [ ] **Phase 298**: essay max char limit decision
- [ ] **Phase 293**: `GetSectionUnitsDictAsync` Level 2+ support
- [ ] **v11.2 paused** Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page)

## Traceability

(Filled saat phase shipped — REQ → Phase mapping)

| REQ-ID | Phase | Status |
|--------|-------|--------|
| CMP-01..26 | 337 | SHIPPED LOCAL |
| CIL-01..05 | 338 (W1-3) | SHIPPED LOCAL |
| CIL-06 | 338 (W4) → **339** | PARTIAL (orphan UI) — gap closure Phase 339 |
| REST-01..03 | 336 | SHIPPED LOCAL |
| REST-04 | 338 (W4) → **339** | PARTIAL (orphan nav) — gap closure Phase 339 |
| REST-05 + REST-07 | 338 (W5) | SHIPPED LOCAL |
| REST-06 | 338 (W5) → **339** | PARTIAL (Title validator missing) — gap closure Phase 339 |

---

*Updated: 2026-06-02 — Gap closure post `/gsd-audit-milestone v20.0` (2026-06-02). 3 partial REQ reassigned ke Phase 339: CIL-06 (BulkExportPdf orphan), REST-04 (BulkBackfill orphan), REST-06 (Title `[RegularExpression]` missing — auto-pair OK). Source: `.planning/v20.0-MILESTONE-AUDIT.md` + integration checker findings. Checkboxes tetap `[ ]` (belum dicentang sebelumnya).*
*Prev: 2026-05-30 — REQ defined for 3-phase Opsi 2 sequential plan (Phase 336-338). Sources: CMP/Records audit memory 2026-05-27 (Approach C locked 2026-05-30) + Cilacap todo 001/002 (2026-05-29).*
*Created: 2026-05-29 post v18.0 + v19.0 milestone close.*
