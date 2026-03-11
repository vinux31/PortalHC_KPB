# Phase 154: Coaching Proton Flow Audit - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit the full Coaching Proton workflow (PROTON-01 through PROTON-07) for bugs, authorization gaps, and edge cases. Covers mapping, evidence upload, coaching sessions, approval chain, HC oversight, Assessment Proton (online + interview), and Histori Proton timeline. Code review + browser UAT hybrid format.

</domain>

<decisions>
## Implementation Decisions

### Audit Plan Grouping
- **By flow stage** — 3 plans:
  - Plan 1: Setup — PROTON-01 (coach-coachee mapping)
  - Plan 2: Execution — PROTON-02 (evidence), PROTON-03 (sessions), PROTON-04 (approvals)
  - Plan 3: Oversight — PROTON-05 (HC review), PROTON-06 (assessment proton), PROTON-07 (histori)
- Gap-closure plan only if verification finds gaps (not pre-allocated)
- Plan 2 stays as one plan — evidence/sessions/approvals are tightly coupled

### Edge Cases to Audit
- **Multi-unit users**: Audit that mapping and deliverable assignment handle users in >1 unit correctly
- **Dual role scoping (SrSpv + SectionHead)**: Let code audit discover actual behavior, flag if incorrect
- **Reassignment**: Skip — jarang terjadi, not priority
- **Coaching session CRUD**: Audit full create + edit + delete, termasuk apakah session yang sudah approved bisa diedit
- **Evidence upload**: Functional audit only (upload works, visible to coach/approver), skip deep security audit

### Assessment Proton Scope (PROTON-06)
- Full audit for both paths: Tahun 1-2 online AND Tahun 3 interview
- Interview path gets same audit depth as online — code review + browser UAT

### Histori Proton (PROTON-07)
- Audit semua event yang ada di kode — mapping, deliverable, session, approval, assessment
- Verify urutan timeline benar (chronological)

### Bug Fix Policy
- **Minor bugs** (validation, display, missing filter, null check): Fix inline dalam plan yang sama
- **Major bugs** (crash/data loss): Catat di audit report, fix di gap-closure plan terpisah
- Threshold: crash atau data loss = major, sisanya minor

### Authorization Depth
- Check [Authorize] attributes di controller actions
- Test direct URL access tanpa role yang benar
- Same depth as Phase 153

### Browser UAT Data
- Data mapping coach-coachee sudah ada dari development sebelumnya — langsung pakai

### Claude's Discretion
- Exact audit checklist per requirement
- How to structure audit report format
- Which controller actions to prioritize within each plan

</decisions>

<specifics>
## Specific Ideas

- Audit methodology follows Phase 153 pattern (code review → browser UAT → fix bugs → verify)
- Coaching Proton is the canonical name (not "Proton Progress")
- CDPController.cs is the main controller for Coaching Proton actions

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- CDPController.cs: Main controller with CoachingProton, Deliverable, FilterCoachingProton actions
- BuildProtonProgressSubModelAsync: Helper for supervisor/HC Proton progress view
- ProtonTrackAssignments, ProtonDeliverableProgresses, ProtonFinalAssessments: Key DB tables
- _CoachingProtonContentPartial: Partial view for AJAX-filtered coaching content

### Established Patterns
- Phase 153 audit pattern: 4 plans (3 audit rounds + 1 gap closure)
- Hybrid audit: Code review produces AUDIT-REPORT.md, then browser UAT verifies
- Fixes applied inline during audit, committed with descriptive messages

### Integration Points
- CDPController (coaching flow, deliverables, dashboard)
- ProtonDataController (silabus, guidance — admin side, covered in Phase 155)
- Models: ProtonTrack, ProtonKompetensiList, ProtonTrackAssignment, ProtonDeliverableProgress, ProtonFinalAssessment

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 154-coaching-proton-flow-audit*
*Context gathered: 2026-03-11*
