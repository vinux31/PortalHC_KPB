# Phase 185: ViewModel and Data Model Foundation - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Define SertifikatRow and CertificationManagementViewModel classes with RecordType discriminator, server-side CertificateStatus derivation, and canonical date mapping for TrainingRecord and AssessmentSession data sources.

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion
All implementation choices are at Claude's discretion — pure infrastructure phase.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Models/TrainingRecord.cs` — has ValidUntil, CertificateType, NomorSertifikat, computed IsExpiringSoon/DaysUntilExpiry
- `Models/AssessmentSession.cs` — has Category, Schedule, CompletedAt, IsPassed, GenerateCertificate; no ValidUntil or NomorSertifikat yet (Phase 192)
- Existing ViewModel pattern: single file per ViewModel in `Models/` folder (e.g., CDPDashboardViewModel.cs)

### Established Patterns
- ViewModels use simple POCO classes in `HcPortal.Models` namespace
- Nullable fields use `?` suffix with explicit null handling
- Status strings are pre-computed (not enums) — consistent with TrainingRecord.Status pattern

### Integration Points
- Phase 186 (Role-Scoped Query) will populate these ViewModels
- Phase 187 (View) will consume CertificationManagementViewModel for Razor rendering

</code_context>

<specifics>
## Specific Ideas

No specific requirements — infrastructure phase.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>
