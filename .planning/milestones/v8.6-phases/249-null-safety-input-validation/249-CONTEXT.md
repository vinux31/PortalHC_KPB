# Phase 249: Null Safety & Input Validation - Context

**Gathered:** 2026-03-24
**Status:** Ready for planning
**Source:** Auto-generated (no gray areas — pure defensive bug fix)

<domain>
## Phase Boundary

Hilangkan semua titik null-dereference dan unsafe cast yang berpotensi crash di CMP melalui guard yang defensive. Tidak ada perubahan logika bisnis — hanya defensive checks.

</domain>

<decisions>
## Implementation Decisions

### Null Safety Pattern
- **D-01:** `GetCurrentUserRoleLevelAsync()` — return error/redirect jika user null, bukan null-forgiving `user!`
- **D-02:** `Model.FullName` di WorkerDetail.cshtml — gunakan `?? ""` fallback
- **D-03:** `ViewBag.UnansweredCount` dan `ViewBag.AssessmentId` di ExamSummary.cshtml — ganti hard cast `(int)` dengan null-safe pattern (`as int?` atau `?? default`)

### Input Validation Pattern
- **D-04:** `DateTime.Parse()` → `DateTime.TryParse()` di 3 action CMP (ExportRecordsTeamAssessment, ExportRecordsTeamTraining, RecordsTeamPartial)
- **D-05:** `ToDictionary` key collision di bulk renewal — guard dengan `.GroupBy().ToDictionary()` atau `.DistinctBy()` sebelum `ToDictionary`

### Claude's Discretion
- Pilihan exact pattern untuk null-safe ViewBag cast (as int? vs try-cast vs null-coalescing)
- Pilihan fallback value untuk DateTime.TryParse (default date range atau return error)
- Pilihan error handling untuk duplicate key (skip duplicates, merge, atau return validation error)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — SAFE-01 through SAFE-05 definitions

### Source Files (targets)
- `Controllers/CMPController.cs` — GetCurrentUserRoleLevelAsync, ExportRecordsTeamAssessment, ExportRecordsTeamTraining, RecordsTeamPartial
- `Controllers/AdminController.cs` — bulk renewal ToDictionary collision
- `Views/Admin/WorkerDetail.cshtml` — FullName null display
- `Views/CMP/ExamSummary.cshtml` — ViewBag unsafe cast

</canonical_refs>

<code_context>
## Existing Code Insights

### Established Patterns
- Controllers use `[Authorize]` class-level — user should always be authenticated, but session can expire
- ViewBag is used throughout for passing simple values — null-safe pattern should be consistent
- DateTime parsing in export actions receives query string parameters

### Integration Points
- No new files — all changes are in existing controllers and views
- No migration needed — no data model changes

</code_context>

<specifics>
## Specific Ideas

No specific requirements — standard defensive coding patterns apply.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 249-null-safety-input-validation*
*Context gathered: 2026-03-24 via auto mode*
