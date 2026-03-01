# Phase 63: Data Source Fix - Context

**Gathered:** 2026-02-27
**Status:** Ready for planning

<domain>
## Phase Boundary

Switch the Progress page from legacy IdpItems data source to ProtonDeliverableProgress + ProtonTrackAssignment. Display real coachee list from CoachCoacheeMapping. Compute accurate summary stats. Create new endpoint at /CDP/ProtonProgress. The Progress page becomes a monitoring hub with role-based access and action links (redirect to Deliverable page for actual operations).

</domain>

<decisions>
## Implementation Decisions

### Table Row Content
- Columns stay the same: Kompetensi, Sub Kompetensi, Deliverable, Evidence, Approval SrSpv, Approval SectionHead, Approval HC
- Track info (e.g., "Panelman Tahun 2") displayed **outside the table**, near the coachee's name — not as a column per row
- Row ordering follows Proton master data `Urutan` field (Kompetensi.Urutan → SubKompetensi.Urutan → Deliverable.Urutan)
- Kompetensi and Sub Kompetensi cells use **rowspan merge** when values repeat
- Evidence column shows **badge status only** (Uploaded/Pending) — no file name or preview
- Action column retains existing dropdown actions — all actions **redirect to Deliverable page** (no inline operations)
- Table grouping: maintain existing Kompetensi → Sub Kompetensi → Deliverable hierarchy
- Mobile/responsive: **horizontal scroll** for the table

### Approval Logic
- SrSpv OR SectionHead approve = considered **100% approved** (they are equivalent)
- HC approval is a **formality** — required before coachee assessment but not counted as primary approval
- Approval status derivation from ProtonDeliverableProgress fields is Claude's discretion (may derive from Status + ApprovedById or add explicit fields)

### Coachee List Behavior
- Dropdown starts **empty** — coach must select a coachee before table appears
- Coachee list ordered **by track** first, then alphabetically within each track group
- Dropdown shows **name only** (no track info, no progress %)
- One coachee at a time — no multi-view
- If coach has no coachees (empty CoachCoacheeMapping): **dropdown disabled** with placeholder "Tidak ada coachee"

### Summary Stats
- **Progress %** = weighted average:
  - No evidence uploaded (Active/Locked) = **0%**
  - Evidence uploaded (Submitted) = **50%**
  - SrSpv/SectionHead Approved = **100%**
  - Formula: sum of weights / total deliverables
- **Pending Actions** = count of deliverables with status Active + Rejected (items requiring coach action)
- **Pending Approvals** = count of deliverables with status Submitted (awaiting SrSpv/SectionHead approval)
- Stats position: **same as current layout** (no change)

### Data Freshness & Loading
- Data refreshed **on every page navigation** — no cache (no-cache headers)
- Switching coachee in dropdown uses **AJAX partial update** (no full page reload)
- Loading state: **spinner/loading indicator** while fetching coachee data via AJAX

### Role-Based Access
- **Coachee (Level 6):** Sees only own progress. No dropdown. No action buttons.
- **Coach (Level 5):** Sees coachees assigned via **CoachCoacheeMapping** (not Unit-based filter). Dropdown shows assigned coachees. Can redirect to upload evidence.
- **SrSpv/SectionHead (Level 4):** Dropdown Unit (within their Bagian) → Coachee. Can redirect to approve from Progress page.
- **HC (Level 2):** Dropdown Bagian → Unit → Coachee. Can mark HC Review.
- **Admin (Level 1):** Same as HC — full visibility.
- All actions (upload evidence, approve, HC review) **redirect to Deliverable page** — Progress page is monitoring only.
- Unauthorized access (e.g., coachee manipulating URL): **silently redirect to own data** instead of showing error.

### Error Handling
- Coachee without track assignment: show **informative message** "Coachee ini belum memiliki penugasan track"
- Coachee with track but missing progress records: show **informative message** "Data progress tidak ditemukan" (admin should fix via Kelola Data)
- Database errors: **automatic retry** (1-2 attempts) then show generic error message
- Unauthorized URL manipulation: redirect to own data

### Transition from IdpItems
- **Cut-over langsung** — /CDP/ProtonProgress is new, /CDP/Progress is deactivated
- **New URL:** `/CDP/ProtonProgress`
- **Old URL:** `/CDP/Progress` — **disabled** (not redirected)
- **Navigation label:** Changed from "Progress" to **"Proton Progress"**
- IdpItems table **retained** — still used by Dashboard (HomeController) and CPDP Report (CMPController). Migration of those areas is a separate future phase.
- ProtonDeliverableProgress already has its own data from track assignment — **no data migration** from IdpItems needed.
- **Reuse TrackingItem ViewModel** — same ViewModel, change mapping in controller from IdpItem to ProtonDeliverableProgress.

### Claude's Discretion
- Progress % visual representation (progress bar, gauge, etc.)
- Approval field strategy (derive from Status vs. add explicit SrSpv/SectionHead fields to ProtonDeliverableProgress)
- Exact AJAX endpoint design and response format
- TrackingItem field mapping details
- QA/verification strategy after cut-over
- Spinner/loading indicator style

</decisions>

<specifics>
## Specific Ideas

- Coach upload evidence per deliverable for coachee — this is the primary workflow
- Proton has a "Penugasan Coachee" system (track assignment) that determines what deliverables a coachee works on
- ProtonDeliverableProgress records are auto-generated when coachee is assigned to a track via CDPController.AssignTrack
- "Kelola Data" admin portal (Phase 47+) handles data corrections — Phase 52 will add DeliverableProgress Override
- The existing BuildProtonProgressSubModelAsync in CDPController already has role-based scoping patterns for Dashboard — reuse similar logic but with CoachCoacheeMapping for Coach role

</specifics>

<deferred>
## Deferred Ideas

- Dashboard (HomeController) migration from IdpItems to ProtonDeliverableProgress — future phase
- CPDP Progress Report (CMPController) migration from IdpItems — future phase
- IdpItems table cleanup/removal — after all consumers migrated

</deferred>

---

*Phase: 63-data-source-fix*
*Context gathered: 2026-02-27*
