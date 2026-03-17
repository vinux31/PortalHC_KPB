# Phase 182: CDP/CoachingProton Evidence Column Bug Fix - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Fix the Evidence column in CoachingProton page to reflect the workflow status (`Status` field: Pending/Submitted/Approved/Rejected) instead of only checking whether a file was uploaded (`EvidencePath != null`). Currently, when coach submits evidence without attaching a file, the Evidence column stays "Belum Upload" even though the record status changed to "Submitted".

</domain>

<decisions>
## Implementation Decisions

### Evidence column logic
- Evidence column must derive its display from the `Status` field, not `EvidencePath`
- Mapping: Pending → "Belum Upload" (grey), Submitted → "Sudah Upload" (green), Approved → Approved badge (green+bold), Rejected → Rejected badge (red)
- Match existing badge pattern already used in the page (bg-secondary, bg-success, etc.)
- No distinction between submitted-with-file and submitted-without-file — both show "Sudah Upload"

### EvidenceStatus computation
- In `CDPController.cs` line ~1483, change `EvidenceStatus = p.EvidencePath != null ? "Uploaded" : "Pending"` to derive from `p.Status` instead
- The view (CoachingProton.cshtml) checks `item.EvidenceStatus == "Uploaded"` — update to match new status values or use `item.Status` directly

### Button behavior
- Coach still sees "Submit Evidence" button when status is Pending or Rejected — no change needed
- After submission, button disappears and appropriate status badge shows

### Scope
- Only fix the Evidence column — do not change approval columns (SrSpv, SH, HC)
- No tooltip or legend needed — badge labels and colors are sufficient

### Claude's Discretion
- Exact badge HTML/CSS to match existing pattern
- Whether to use `item.Status` directly in the view or keep the `EvidenceStatus` intermediary property

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements fully captured in decisions above.

### Key source files
- `Controllers/CDPController.cs` §1477-1490 — TrackingItem mapping where EvidenceStatus is computed
- `Controllers/CDPController.cs` §1930-2050 — SubmitEvidenceWithCoaching action (file is optional)
- `Views/CDP/CoachingProton.cshtml` §419-437 — Evidence column rendering (coach view)
- `Views/CDP/CoachingProton.cshtml` §535-552 — Evidence column rendering (single-coachee view)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `GetApprovalBadge()` / `GetApprovalBadgeWithTooltip()` — existing helper functions in CoachingProton.cshtml for rendering approval badges with consistent styling
- Bootstrap badge classes already in use: `bg-secondary`, `bg-success`, `bg-danger`, `bg-warning`

### Established Patterns
- TrackingItem DTO maps DB fields to view-friendly properties in CDPController line ~1477
- View checks `item.EvidenceStatus` and `item.Status` separately — both available in TrackingItem

### Integration Points
- `CDPController.CoachingProton()` action builds TrackingItem list — single place to fix the mapping
- Two evidence column render blocks in the view (multi-coachee and single-coachee) — both need updating

</code_context>

<specifics>
## Specific Ideas

- User discovered the bug: coach submits evidence without file → Evidence column stays "Belum Upload" even though Status = "Submitted"
- Root cause: `EvidenceStatus = p.EvidencePath != null ? "Uploaded" : "Pending"` checks file presence, not workflow state

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 182-page-cdp-coachingproton-kolom-evidence-jelaskan-status-yang-ada-di-kolom-ini*
*Context gathered: 2026-03-17*
