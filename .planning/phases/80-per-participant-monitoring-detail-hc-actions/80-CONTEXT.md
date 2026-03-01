# Phase 80: Per-Participant Monitoring Detail & HC Actions - Context

**Gathered:** 2026-03-01
**Status:** Ready for planning

<domain>
## Phase Boundary

HC/Admin can drill into any assessment group from the new Assessment Monitoring page to see per-participant live progress, and can perform all monitoring actions (Reset, Force Close, Bulk Close, Close Early, Regenerate Token) from the dedicated monitoring detail page.

**Key finding:** The detail page (`AssessmentMonitoringDetail.cshtml`) and all individual actions (Reset, Force Close, Force Close All, Close Early, Reshuffle) already exist from previous milestones (v2.1 Phase 44, v1.9 Phase 39, etc.). Phase 80 primarily updates navigation to flow from the new monitoring page and adds Regenerate Token to the detail page.

</domain>

<decisions>
## Implementation Decisions

### Navigation Flow
- Back button and breadcrumb always point to `AssessmentMonitoring` (the new Phase 79 page), not `ManageAssessment`
- Update `ViewBag.BackUrl` in the controller from `ManageAssessment` to `AssessmentMonitoring`
- Breadcrumb path: **Kelola Data > Assessment Monitoring > [Group Title]** (use the actual assessment title as the last breadcrumb item)
- After actions (Reset, Force Close, etc.), simple redirect back to `AssessmentMonitoringDetail` — no filter state preservation needed
- Leave the ManageAssessment monitoring dropdown as-is until Phase 81 removes it entirely

### Regenerate Token Placement
- Show token in a **dedicated card/section** near the header area (not inline with action buttons)
- Display the **full token value** — no masking (admin-only page, low security risk)
- Add a **copy-to-clipboard button** (clipboard icon) next to the token value with brief "Copied!" feedback
- Regenerate button alongside the displayed token
- Only show this section for token-required groups (`IsTokenRequired == true`)
- Use **inline JS fetch** to regenerate — update token display in-place without page reload (matches Phase 79 pattern)

### Claude's Discretion
- Exact placement of the token section (above or below summary cards)
- Token section styling (card style, border, background)
- Copy button animation/feedback duration
- Whether to show a confirmation dialog before regenerating

</decisions>

<specifics>
## Specific Ideas

- Token section should make it easy for HC to copy and share the access token with participants
- The inline JS update for token regeneration should reuse the same `/Admin/RegenerateToken` POST endpoint used by Phase 79's group list

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Views/Admin/AssessmentMonitoringDetail.cshtml`: Complete detail page with real-time polling (10s), countdown timers, summary cards, per-user table with all actions (Reset, Force Close, Reshuffle, View Results)
- `Models/AssessmentMonitoringViewModel.cs`: `MonitoringGroupViewModel` already has `IsTokenRequired` and `AccessToken` properties (added in Phase 79)
- `Controllers/AdminController.cs`: `AssessmentMonitoringDetail` action (line 1408), `RegenerateToken` POST action, `GetMonitoringProgress` polling endpoint all exist
- Phase 79's `AssessmentMonitoring.cshtml`: Contains JS fetch pattern for Regenerate Token that can be reused

### Established Patterns
- Group-level actions in card header: Export Results, Submit Assessment (Close Early), Force Close All, Reshuffle All
- Per-user actions in table: Reset (all statuses), Force Close (InProgress), Reshuffle (package mode), View Results (Completed)
- All POST actions use `@Html.AntiForgeryToken()` + form submission with `confirm()` guard
- JS polling via `fetchProgress()` updates rows, summary counts, and countdowns every 10s
- Action redirects use `RedirectToAction("AssessmentMonitoringDetail", new { title, category, scheduleDate })` pattern

### Integration Points
- `ViewBag.BackUrl` in `AssessmentMonitoringDetail` action (line 1500) — currently `ManageAssessment`, needs to become `AssessmentMonitoring`
- Breadcrumb in `AssessmentMonitoringDetail.cshtml` (line 60-66) — hardcoded, needs update
- `RegenerateToken` POST endpoint at `/Admin/RegenerateToken` — already exists, returns JSON `{ success, token }`

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 80-per-participant-monitoring-detail-hc-actions*
*Context gathered: 2026-03-01*
