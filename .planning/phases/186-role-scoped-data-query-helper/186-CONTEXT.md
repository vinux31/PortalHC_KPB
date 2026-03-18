# Phase 186: Role-Scoped Data Query Helper - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

BuildSertifikatRowsAsync helper di CDPController yang menggabungkan TrainingRecord + AssessmentSession dengan role-scoped access mengikuti pattern GetCurrentUserRoleLevelAsync() dari v7.6.

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion
All implementation choices are at Claude's discretion — pure infrastructure phase.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CMPController.GetCurrentUserRoleLevelAsync()` — role-scoping pattern (L1-3 full, L4 section, L5 coach assignments, L6 own data)
- `Models/UserRoles.cs` — GetRoleLevel() utility
- `Models/CertificationManagementViewModel.cs` — SertifikatRow, CertificateStatus, RecordType (from Phase 185)

### Established Patterns
- Role-scoped queries follow GetCurrentUserRoleLevelAsync pattern in CMPController
- UserRoles.GetRoleLevel() already used in CMP, CDP, and Admin controllers

### Integration Points
- Phase 185 SertifikatRow — output type for query results
- Phase 187 will consume BuildSertifikatRowsAsync to populate view
- Data sources: TrainingRecord (filter: SertifikatUrl not null), AssessmentSession (filter: GenerateCertificate=true AND IsPassed=true)

</code_context>

<specifics>
## Specific Ideas

No specific requirements — infrastructure phase.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 186-role-scoped-data-query-helper*
*Context gathered: 2026-03-18*
