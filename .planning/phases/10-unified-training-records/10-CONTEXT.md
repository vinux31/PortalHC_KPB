# Phase 10: Unified Training Records - Context

**Gathered:** 2026-02-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Merge completed assessment sessions and manual training records into a single chronological table on the Training Records page. Workers see their full development history in one view. HC sees a worker list with a combined completion count drawn from both sources. Assessment page filtering (Phase 11) and dashboard consolidation (Phase 12) are separate phases.

</domain>

<decisions>
## Implementation Decisions

### Table column strategy
- Single unified table — all columns always visible (no type-conditional hiding)
- Non-applicable cells show `—` (em dash), not a blank — makes "field doesn't apply" explicit
- Column order: Date | Type | Nama/Judul | Score | Pass/Fail | Penyelenggara | Tipe Sertifikat | Berlaku Sampai | Status
- Type column uses a colored badge (pill): e.g., blue for "Assessment Online", green for "Training Manual"

### Table sorting
- Sorted most-recent-first by date
- Tie-break: Assessment rows appear before Training Manual rows on the same date

### HC worker list metric
- Completion metric shows a combined count, not a percentage: e.g., "5 completed (3 assessments + 2 trainings)"
- What counts as complete for assessments: `IsPassed = true` only (failed attempts excluded)
- What counts as complete for training records: `Status = Passed` or `Status = Valid` only (Pending, Expired, etc. excluded)
- No percentage denominator — training records have no defined "expected" ceiling

### Certificate expiry warnings
- Warning shown only when record is already expired (past `Berlaku Sampai` date)
- No lookahead window — no "expiring soon" warning in this phase

### Empty state
- Worker with no records sees: "Belum ada riwayat pelatihan" (plain text, no call to action)

### Role behavior: Admin SelectedView
- Admin in `SelectedView="Coachee"` sees the HC worker list (elevated access), not individual records
- Admin always gets the highest-access view regardless of simulated role

### Claude's Discretion
- Badge color choices (exact hex/Bootstrap class for blue/green pills)
- Responsive behavior of the merged table on smaller screens
- Worker list column structure in HC view (beyond the completion count)

</decisions>

<specifics>
## Specific Ideas

- The unified table replaces the current Training Records view entirely — no separate tabs for assessment vs. training
- The `—` dash pattern is consistent: every column present for every row, type determines which are populated

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 10-unified-training-records*
*Context gathered: 2026-02-18*
