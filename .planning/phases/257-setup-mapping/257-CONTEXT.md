# Phase 257: Setup & Mapping - Context

**Gathered:** 2026-03-25
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT coach-coachee mapping end-to-end: test semua flow CRUD, import Excel, assign track, deactivate/reactivate, dan fix bug yang ditemukan. Scope = mapping level (MAP-01..08), bukan deliverable progress cascade.

</domain>

<decisions>
## Implementation Decisions

### Seed Data Strategy
- **D-01:** Reuse data existing di DB — cek apakah cukup coach/coachee/track, tambah manual kalau kurang. Tidak perlu fresh seed script.

### Bug Fix Scope
- **D-02:** Fix bug langsung in-place saat ditemukan (Claude analisa → fix → commit → user verifikasi). Pending UAT Phase 235/247 yang relevan dengan mapping ikut di-test ulang.

### Verification Evidence
- **D-03:** Claude analisa code untuk pastikan logic benar, lalu user verifikasi di browser untuk flow kritis. Checklist per requirement MAP-01..08.

### Test Scenario Depth
- **D-04:** Happy path + key edge cases per requirement (duplikat import, assign tanpa track, deactivate mapping dengan session aktif). Tidak perlu exhaustive semua kombinasi.

### Progression Warning (D-09)
- **D-05:** Behavior warning only sudah benar — warning muncul, user confirm, lalu proceed. Bukan hard block.

### Import Excel Error Handling
- **D-06:** Partial commit — row valid tetap di-commit, row error ditampilkan di summary (Success/Error/Skip/Reactivated per row). Bukan all-or-nothing.

### Track Assignment Cascade
- **D-07:** Test cascade hanya sampai ProtonTrackAssignment level. DeliverableProgress cascade di-test di Phase 259.

### Claude's Discretion
- Urutan test scenario per requirement
- Detail edge cases mana yang paling kritis untuk di-test

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Mapping CRUD & Import
- `Controllers/AdminController.cs` §CoachCoacheeMapping (line ~3632) — List, pagination, search
- `Controllers/AdminController.cs` §CoachCoacheeMappingAssign (line ~3984) — Assign flow + D-09 warning
- `Controllers/AdminController.cs` §ImportCoachCoacheeMapping (line ~3789) — Import Excel flow
- `Controllers/AdminController.cs` §DownloadMappingImportTemplate (line ~3754) — Template download
- `Controllers/AdminController.cs` §CoachCoacheeMappingDeactivate (line ~4320) — Deactivate + cascade
- `Controllers/AdminController.cs` §CoachCoacheeMappingReactivate (line ~4390) — Reactivate + reuse assignment
- `Views/Admin/CoachCoacheeMapping.cshtml` — UI view

### Models
- `Models/CoachCoacheeMapping.cs` — Entity model
- `Models/ProtonModels.cs` — ProtonTrackAssignment, ProtonDeliverableProgress
- `Models/ImportMappingResult.cs` — Import result per row

### Requirements
- `.planning/REQUIREMENTS.md` §Setup & Mapping — MAP-01..MAP-08

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- 13 controller actions sudah lengkap untuk semua mapping operations
- ImportMappingResult model dengan per-row status tracking (Success/Error/Skip/Reactivated)
- CoachAssignRequest dengan ConfirmProgressionWarning flag untuk D-09

### Established Patterns
- Import Excel pattern: download template → upload file → process rows → return summary (sama seperti ImportWorkers)
- Deactivate/reactivate pattern: soft delete via IsActive flag + cascade ke related entities
- AuditLog tracking untuk setiap operasi CRUD

### Integration Points
- AdminController (Authorize Roles = "Admin, HC")
- Views/Admin/Index.cshtml — hub page navigasi ke CoachCoacheeMapping
- ProtonTrackAssignment cascade dari CoachCoacheeMapping deactivate/reactivate

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 257-setup-mapping*
*Context gathered: 2026-03-25*
