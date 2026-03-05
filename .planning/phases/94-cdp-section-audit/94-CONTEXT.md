# Phase 94: CDP Section Audit - Context

**Gathered:** 2026-03-05
**Status:** Ready for planning

<domain>
## Phase Boundary

Audit CDP (Competency Development Platform) pages for bugs — Plan IDP, Coaching Proton, Deliverable detail, and CDP Index hub. Focus is finding and fixing bugs, NOT adding new features or changing functionality.

**Pages to Audit:**
- /CDP/PlanIdp — IDP planning with silabus and guidance tabs
- /CDP/CoachingProton — Coaching session management (coachee list, deliverable status, session submission)
- /CDP/Deliverable — Deliverable detail page (evidence upload/download, approval workflow)
- /CDP/Index — CDP dashboard hub

**Requirements:** CDP-01 through CDP-06 (load without errors, coachee lists, approval workflows, evidence upload/download, session flows, validation)

</domain>

<decisions>
## Implementation Decisions

### Audit Organization
- **Organize by use-case flows** (not page-by-page) — following Phase 85 coaching QA pattern
- Flow 1: IDP Planning (PlanIdp page — silabus tab, guidance tab, filter behavior)
- Flow 2: Coaching Workflow (CoachingProton — coachee selection, deliverable list, session submission, approval flow)
- Flow 3: Evidence & Approval (Deliverable detail — evidence upload/download, Spv/HC review, status transitions)
- One commit per flow or grouped by related fixes

### Test Data Approach
- **Pre-seeded test data required** — comprehensive coverage like Phase 85/90
- Seed: Coachee-coach mappings, Proton tracks with deliverables, coaching sessions in various statuses, evidence files
- All 5 roles represented: Coachee, Coach, Spv, SectionHead, HC/Admin
- Data should cover: pending sessions, approved sessions, rejected sessions, evidence with/without files

### Testing Approach
- **Smoke test + targeted verification** — not exhaustive role combination testing
- Pattern: Code review → identify bugs → fix → browser verify (same as Phase 85/93)
- User verifies in browser after code fixes
- Focus on verifying the specific bug that was fixed
- Test data created upfront, then verify all flows work correctly

### Evidence File Handling Depth
- **Deep audit** — verify upload/download works + edge cases
- Check: file size limits, allowed file types, path security, virus scanning (if any), error handling
- Verify: evidence links work, files are stored correctly, download returns correct content type

### Role Testing Coverage
- **All 5 roles** — Coachee, Coach, Spv, SectionHead, HC/Admin
- Each role tested for their specific workflows (not every role on every page)
- Coachee: PlanIdp view, submit session, upload evidence
- Coach: Select coachee, review deliverables, submit coaching session
- Spv: Review and approve/reject sessions
- HC: Final approval, view all workflows
- Admin: Full access (same as HC)

### Bug Priority
- Claude's discretion — prioritize based on severity and user impact
- Critical: crashes, null references, raw exceptions shown to users
- High: broken flows, incorrect data displayed, navigation failures
- Medium: UX issues (unclear text, missing links, confusing UI)
- Low: cosmetic issues, typos, minor inconsistencies

### Claude's Discretion
- Exact order of bug fixes within each flow
- Grouping of fixes into commits (per-flow vs per-category vs per-file)
- Which edge cases to investigate in depth vs quick smoke test
- Whether to refactor any messy code discovered during audit

</decisions>

<specifics>
## Specific Ideas

- Follow Phase 85 (Coaching Proton QA) pattern: Code review → Identify bugs → Fix → Browser verify
- Follow Phase 93 (CMP Audit) commit style: `fix(cdp): [description]` with Co-Authored-By footer
- Use Indonesian culture (id-ID) for all date formatting, matching Phase 92/93 fixes
- Preserve existing functionality — bug fixes only, no behavior changes
- "Secara menyeluruh dan detail" — thoroughness is the priority, not speed (from Phase 90)

</specifics>

<code_context>
## Existing Code Insights

### CDPController.cs (~4700 lines)
- Main controller with PlanIdp, CoachingProton, Deliverable, Dashboard, Progress workflows
- Role-based access patterns using user.RoleLevel and UserRoles enum
- Evidence file upload/download handling with IWebHostEnvironment
- Coaching session submission and approval chains (Coach → Spv → HC)

### CDP Views
- **Index.cshtml** — CDP hub/dashboard with stats and navigation cards
- **PlanIdp.cshtml** — IDP planning with 2-tab layout (Silabus + Guidance), filter dropdowns, role-based section locking
- **CoachingProton.cshtml** — Coaching session management with coachee dropdown, deliverable table, session submission modal
- **Deliverable.cshtml** — Deliverable detail with evidence upload/download, approval workflow, status history timeline
- **Dashboard.cshtml** — CDP-specific dashboard with Proton progress and approval stats

### Known Bug Patterns (from Phase 85 fixes)
- **Status enum mismatch**: ProtonDeliverableProgress has no 'Active' status, valid statuses are Pending/Approved/Rejected/Completed
- **Missing IsActive filters**: Dashboard queries were including inactive users before Phase 85-02 fix
- **Coachee scope**: SrSpv/SH (level 4) coachee dropdown was missing entirely before Phase 85-03 fix
- **Redirect targets**: HCReviewDeliverable was redirecting to wrong page before Phase 85-03 fix

### Established Patterns from Phase 85/93
- Date localization fix: Add `@using System.Globalization` to view, use `CultureInfo.GetCultureInfo("id-ID")`
- Null safety: Use `?.` null-conditional and `??` coalescing operators
- Role-based filtering: Check `user.RoleLevel` or `User.IsInRole()` for authorization
- Evidence storage: `/uploads/deliverables/` directory with unique filenames
- Commit pattern: `fix(cdp): [flow] - [description]` with detailed commit body

### Models to Review
- ProtonDeliverableProgress — Status (Pending/Approved/Rejected/Completed), EvidenceFilePath, CoachingNotes
- ProtonCoachingSession — CoachId, CoacheeId, DeliverableId, SessionDate, Notes, Status
- CoachingGuidanceFile — Bagian, Unit, ProtonTrackId, FileName, FilePath, UploadedAt
- ProtonTrackAssignment — CoacheeId, CoachId, ProtonTrackId, IsActive
- ApplicationUser — RoleLevel, Section, Unit, IsActive

### Integration Points
- PlanIdp → ProtonKompetensiList (silabus data) + CoachingGuidanceFiles (guidance documents)
- CoachingProton → ProtonTrackAssignments (coach-coachee mapping) + ProtonDeliverableProgress (deliverable status)
- Deliverable → Evidence file upload/download + approval chain (Coach → Spv → HC)
- Dashboard → Aggregates data from all CDP workflows
- Role-based section filtering — Coachee locked to assigned Bagian, other roles can browse

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 94-cdp-section-audit*
*Context gathered: 2026-03-05*
