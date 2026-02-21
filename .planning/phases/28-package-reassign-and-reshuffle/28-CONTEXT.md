# Phase 28: Package Reshuffle - Context

**Gathered:** 2026-02-21
**Status:** Ready for planning

<domain>
## Phase Boundary

HC can reshuffle a worker's current package assignment on the AssessmentMonitoringDetail page — either per-worker or all eligible workers at once. Reshuffle assigns a new random package from the assessment's available packages.

This phase covers **reshuffle only**. Re-assign (selecting a specific package) was dropped after discussion — not needed.

Reshuffle is only applicable to **package-mode assessments** (where HC uses packages to randomize questions per worker). Question-mode assessments have no package to reshuffle.

</domain>

<decisions>
## Implementation Decisions

### Scope change
- Re-assign feature (dropdown to pick a specific package) is **removed** — not being built
- Only reshuffle is implemented in this phase

### When reshuffle is available
- **Package-mode assessments only** — reshuffle controls do not appear for question-mode assessments
- Reshuffle is a **recovery action** — primary use case is after a technical incident (connection loss, session reset) where HC wants to re-randomize packages to ensure fairness
- Only **Pending** workers can be reshuffled — workers who are Ongoing or Completed are ineligible

### Per-worker reshuffle
- Each worker card on the monitoring tab shows a Reshuffle button
- Pending workers: button is active
- Ongoing workers: button is visible but grayed out (disabled)
- Completed workers: button is visible but grayed out (disabled)

### Reshuffle All
- A "Reshuffle All" button exists on the page for bulk reshuffling
- Automatically excludes Ongoing and Completed workers — only reshuffles Pending workers
- Shows a **confirmation dialog** before executing: e.g., "Reshuffle all [N] pending workers?"
- After completion, shows a **modal result list** — each worker listed with their reshuffle status (reshuffled / skipped and why)

### After-action feedback
- **Per-worker reshuffle**: toast notification + card updates in-place (no full page reload)
- **Reshuffle All**: modal result list showing each worker name and outcome after bulk operation completes

### Page location
- Controls are on the **AssessmentMonitoringDetail** page, monitoring tab
- URL pattern: `/CMP/AssessmentMonitoringDetail?...`
- HC-only — workers never see these controls

### Claude's Discretion
- Exact placement of per-worker reshuffle button within the monitoring card
- Exact placement of "Reshuffle All" button on the page (header area, toolbar, etc.)
- Tooltip text for disabled buttons
- Exact confirmation dialog wording and styling
- Result modal layout and styling

</decisions>

<specifics>
## Specific Ideas

- Reshuffle is primarily a recovery tool — HC uses it after resetting workers due to technical issues (connection loss, etc.) to ensure packages are freshly randomized
- Reshuffle All result modal should show who was reshuffled and who was skipped, so HC can confirm the operation was effective

</specifics>

<deferred>
## Deferred Ideas

- Re-assign (selecting a specific package for a specific worker) — was originally in Phase 28 roadmap scope but user decided it's not needed. Could be revisited in a future phase if needed.

</deferred>

---

*Phase: 28-package-reassign-and-reshuffle*
*Context gathered: 2026-02-21*
