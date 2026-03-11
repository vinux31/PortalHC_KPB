# Phase 155: Admin Kelola Data Audit - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit all admin data management operations (ADMIN-01 through ADMIN-06) for bugs, authorization gaps, and edge cases. Covers worker CRUD + import, KKJ/CPDP file management, Proton Data management (ProtonDataController), and audit logging. Code review + browser UAT hybrid format.

</domain>

<decisions>
## Implementation Decisions

### Audit Plan Grouping
- **By domain (3 plans):**
  - Plan 1: Worker management (ADMIN-01, ADMIN-02) — full scope: Create, Edit, Delete, Deactivate, Reactivate, Import, Export, WorkerDetail
  - Plan 2: KKJ/CPDP file management (ADMIN-03, ADMIN-04) — includes bagian CRUD (KkjBagianAdd/Delete) plus all file operations (upload, download, archive, history)
  - Plan 3: Proton Data + Audit Log (ADMIN-05, ADMIN-06) — ProtonDataController full audit + exhaustive audit log matrix across ALL admin actions
- Gap-closure plan only if verification finds gaps (not pre-allocated)

### Delete Cascade Depth (ADMIN-01)
- **Full cascade trace** for DeleteWorker: trace every FK relationship (AssessmentSessions, CoachCoacheeMappings, ProtonTrackAssignments, ProtonDeliverableProgresses, TrainingRecords, etc.) and verify no orphans remain
- **Audit both paths:** Deactivate (soft-delete: user can't log in, data stays) AND Delete (hard-delete: cascade removes all FK records)
- **Include ReactivateWorker** edge cases (same pattern as Phase 154 FINDING-01 — reactivate might not restore related records)

### Proton Data Scope Boundary (ADMIN-05)
- **Fresh audit of ProtonDataController** — Phase 154 covered CDPController (coachee/coach side), ProtonDataController (admin side) was NOT audited
- **Audit downstream impact** of admin actions on active coaching sessions (e.g., deleting/deactivating silabus items that coachees are working on). This fulfills the "changes take effect immediately" success criterion.

### Audit Log Coverage (ADMIN-06)
- **Exhaustive check:** Grep every admin action for AuditLog.Add() or equivalent. Produce a complete matrix: action -> logged (yes/no). Flag any gaps.
- **Write + read verification:** Verify entries are created AND that AuditLog viewer page (Admin/AuditLog) displays entries with correct actor NIP, timestamp, action detail
- **Cross-cutting in Plan 3:** The complete audit log matrix covers ALL admin actions (worker, files, proton) in one place

### Bug Fix Policy
- Same as Phase 153-154: minor bugs (validation, display, null check) fixed inline; major bugs (crash/data loss) get separate gap-closure plan
- Threshold: crash or data loss = major, everything else = minor

### Authorization Depth
- Same as Phase 153-154: check [Authorize] attributes + test direct URL access without correct role
- AdminController actions should be [Authorize(Roles = "Admin, HC")]
- ProtonDataController should be [Authorize(Roles = "Admin, HC")]

### Claude's Discretion
- Exact audit checklist per requirement
- How to structure audit report format
- Which controller actions to prioritize within each plan
- Browser UAT test data setup approach

</decisions>

<specifics>
## Specific Ideas

- Audit methodology follows Phase 153-154 pattern (code review -> AUDIT-REPORT.md -> browser UAT -> fix bugs)
- AdminController is the main controller for worker CRUD, KKJ/CPDP files
- ProtonDataController handles silabus CRUD, guidance, override (separate controller with [Authorize(Roles="Admin,HC")])
- Worker import uses Excel template pattern (DownloadImportTemplate + ImportWorkers)

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- AdminController.cs: Worker CRUD (~line 3398-4280), KKJ (~line 58-396), CPDP (~line 397-574), AuditLog (~line 2489)
- ProtonDataController.cs: Silabus CRUD, Guidance upload/download/replace/delete, Override list/detail/save, StatusData, DeleteKompetensi
- ManageUserViewModel: ViewModel for worker create/edit
- ImportWorkers + DownloadImportTemplate: Excel import pattern

### Established Patterns
- Phase 153-154 audit pattern: code review -> AUDIT-REPORT.md -> browser UAT -> inline fixes
- AuditLog.Add() pattern for action logging
- [Authorize(Roles = "Admin, HC")] on worker management actions
- [ValidateAntiForgeryToken] on POST actions

### Integration Points
- AdminController (worker CRUD, file management, audit log viewer)
- ProtonDataController (silabus, guidance, override — admin-side Proton management)
- Models: ApplicationUser, KkjFile, CpdpFile, ProtonKompetensiList, ProtonTrack, AuditLog

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 155-admin-kelola-data-audit*
*Context gathered: 2026-03-11*
