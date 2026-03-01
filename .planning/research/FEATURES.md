# QA Testing Features & Verification Checklist

**Project:** Portal HC KPB v3.0 — Full QA & Feature Completion
**Researched:** 2026-03-01
**Confidence:** HIGH (Based on PROJECT.md scope)

---

## Table Stakes (Expected by Users)

Features users expect the portal to have. Missing these = product feels incomplete or broken.

| Feature | Why Expected | Complexity | Testing Approach | Status |
|---------|--------------|------------|------------------|--------|
| **Assessment End-to-End Flow** | Core product feature: create → assign → schedule → exam → results | High | Functional test: full workflow with real data | Existing, needs verification |
| **Coaching Proton Workflow** | Defined feature: mapping → coaching sessions → evidence → approval | High | Functional test: coaching session creation, evidence upload, HC approval | Existing, needs verification |
| **Master Data CRUD** | Admin must manage: KKJ Matrix, CPDP Items, Coach-Coachee Mapping, Silabus | High | Integration tests: verify each CRUD operation persists correctly | Existing (v2.3), needs verification |
| **User Role Authorization** | Different views for Admin, HC, Workers; HC-only features visible | Medium | Functional tests: verify access gates per role | Existing (v2.5), needs verification |
| **Dashboard/Home Page** | Shows user's relevant data (assessments, coaching status, IDP) | Medium | Functional test: verify correct data displays per role | Existing, needs verification |
| **Data Export (Excel/PDF)** | Admin/HC can export assessment results, coaching reports | Medium | Integration test: verify file generation, data accuracy | Existing (v2.3/v2.4), needs verification |
| **Kelola Data Hub** | Centralized master data management (3 sections: Manajemen Pekerja, Assessment, Coaching) | High | Functional tests: verify all hub cards accessible, links work | Existing (v2.5), needs verification |
| **Session Resume After Disconnect** | Worker can reconnect and resume exam from last page | Medium | Functional test: close session, reconnect, verify last page restored | Existing (v2.1), needs verification |

---

## Differentiators (Not Expected, But Valued)

Features that set this product apart. Low/missing = users won't complain, but implementation strengthens UX.

| Feature | Value Proposition | Complexity | Testing Approach | Status |
|---------|-------------------|------------|------------------|--------|
| **Live HC Monitoring** | HC can watch exam progress in real-time (auto-refresh every 10s) | Medium | Functional test: verify polling updates progress table | Existing (v2.1), needs verification |
| **Coaching Evidence Tracking** | Photo/document upload for coaching sessions, linked to coachee progress | Medium | Integration test: verify file storage, retrieval, access control | Existing (v2.3), needs verification |
| **IDP Plan Page** | Coachee sees silabus items + downloadable guidance docs for self-study | High | Functional test: verify correct silabus items displayed, docs downloadable | NEW in v3.0, needs development |
| **Audit Log** | Admin can see who changed what, when (for compliance) | Medium | Integration test: verify CRUD operations logged | NEW in v3.0 (card added to hub), needs verification |
| **Assessment History** | Workers can view all past attempts, retake analysis | Medium | Functional test: verify History tab shows correct attempts with attempt numbers | Existing (v2.2), needs verification |
| **Auto-Save During Exam** | Answers auto-save every question click; worker never loses progress | Medium | Integration test: verify SaveAnswer endpoint handles concurrent saves | Existing (v2.1), needs verification |

---

## Anti-Features (What NOT to Build/What to Remove)

Explicit decisions to NOT build certain things or remove obsolete code.

| Anti-Feature | Why Avoid | What to Do Instead | Status |
|--------------|-----------|-------------------|--------|
| **Selenium/Playwright UI Tests** (at this phase) | Slow, brittle, maintenance burden on brownfield; better ROI from code analysis + manual QA | Manual QA checklist + code analysis tools; plan E2E automation for v3.1+ | Don't implement now; defer to v3.1 |
| **CMP/ProtonMain Page** (duplicate) | CDP/ProtonMain action removed in v2.6; replaced by Admin panel Training Records card | Already done; verify it's completely removed + no orphaned links | Completed v2.6; verify in QA |
| **Duplicate Admin CRUD Paths** | CMP had ManageQuestions, CreateTrainingRecord duplicates (Admin versions canonical in v2.3) | Already consolidated in Admin panel; remove any lingering CMP paths | Verify in QA; ensure no orphaned links exist |
| **"Proton Progress" naming** | Confusing; should be "Coaching Proton" per product naming | Rename throughout codebase (UI, code comments, database field labels) | NEW task for v3.0 |
| **Breaking Change Backward Compatibility** | Don't support legacy API endpoints or old data formats | Use migrations for schema changes; API versioning if external consumers exist | Standard practice; follow existing pattern |

---

## Feature Testing Matrix

### Assessment Management (Table Stakes)

**Create Assessment**
- [ ] Admin can create assessment with title, category, schedule
- [ ] Assessment appears in list immediately after creation
- [ ] Validation: empty title should error

**Assign Assessment to Workers**
- [ ] Admin/HC can select workers from list
- [ ] Bulk assignment works (multiple workers at once)
- [ ] Assigned workers see assessment in their dashboard
- [ ] Unassigned workers don't see it

**Exam Workflow**
- [ ] Worker starts exam, sees first question
- [ ] Timer counts down, displays remaining time
- [ ] Worker can navigate between questions
- [ ] Answers auto-save on radio click (v2.1)
- [ ] Worker can submit exam, redirect to results page

**Results & History**
- [ ] Results page shows score, pass/fail status
- [ ] History tab shows all past attempts with attempt numbers
- [ ] Worker can see time taken, answers per attempt
- [ ] Reset clears session, allows retake

---

### Coaching Proton Workflow (Table Stakes)

**Coach-Coachee Mapping**
- [ ] Admin maps coaches to coachees (can be 1-to-many)
- [ ] Coachees see mapped coaches in their dashboard
- [ ] Coaches see assigned coachees in their dashboard
- [ ] Soft-delete works (unmap without losing history)

**Coaching Session & Evidence**
- [ ] Coach can create coaching session for coachee
- [ ] Evidence (photo/document) can be uploaded
- [ ] Coaching report can be written and saved
- [ ] Coachee can view coaching history and evidence

**Approval Workflow (Role-Based)**
- [ ] SrSpv (Senior Supervisor) can approve/reject deliverable progress
- [ ] SectionHead can approve/reject
- [ ] HC can approve/reject or override
- [ ] Rejection status shows to coachee
- [ ] Override by HC takes precedence

**Progress Tracking**
- [ ] Deliverable progress shows completion % (0-100)
- [ ] Status transitions: Not Started → In Progress → Completed → (optional) Override
- [ ] Timeline shows coaching date, completion date
- [ ] Reports can be exported (Excel/PDF)

---

### Master Data Management (Table Stakes)

**KKJ Matrix (Competency Matrix)**
- [ ] Admin can view all KKJ items with positions, levels
- [ ] Add KKJ: inline add form, save to database
- [ ] Edit KKJ: pencil-icon in-place rename, antiforgery token used
- [ ] Delete KKJ: modal shows affected coachees, confirm delete
- [ ] Export to Excel: all items with headers

**CPDP Items (Development Syllabus)**
- [ ] View all CPDP items organized by track/level
- [ ] Add/edit/delete items with validation
- [ ] Bulk operations: multi-cell copy/paste, bulk save
- [ ] Proper foreign key constraints prevent orphaning

**Proton Silabus & Coaching Guidance**
- [ ] Two-tab interface: Silabus + Guidance
- [ ] Silabus tab: Kompetensi → SubKompetensi → Deliverables (CRUD)
- [ ] Guidance tab: upload PDF/docs, download link for coachees
- [ ] Delete guards: show coachee impact before allowing delete

**Training Records**
- [ ] Card added to Kelola Data hub (v2.6)
- [ ] Workers can access training history
- [ ] Admin can manage training records (view, add, delete)
- [ ] Exports work correctly

---

### Authorization & Role-Based Access (Table Stakes)

**Worker Role (Pekerja)**
- [ ] Can view assigned assessments
- [ ] Can take exams, see results
- [ ] Can view coaching status, evidence, guidance docs
- [ ] Cannot access Admin panel or master data

**Coach Role (Pembimbing)**
- [ ] Can see assigned coachees
- [ ] Can create coaching sessions, upload evidence
- [ ] Can view coachee progress
- [ ] Cannot access Admin panel (only views)

**HC (Human Capital)**
- [ ] Can access full Admin panel (Kelola Data hub)
- [ ] Can view all assessments, coaching sessions, workers
- [ ] Can approve/reject progress
- [ ] Can override silabus/coaching guidance
- [ ] Can manage master data
- [ ] Can export reports

**Admin (Superuser)**
- [ ] Full CRUD on all master data
- [ ] Can see audit logs of all changes
- [ ] Can reset assessments, close exams early
- [ ] Can manage users, roles, permissions

**Access Gate Tests**
- [ ] Worker accesses /Admin/ManageAssessments → 403 Unauthorized
- [ ] HC accesses non-HC features → 403
- [ ] Navbar shows correct menu items per role
- [ ] "Kelola Data" menu visible to HC and Admin, hidden from Worker/Coach

---

### Data Quality & Integrity (Table Stakes)

**Database Constraints**
- [ ] Foreign keys prevent orphaning (e.g., can't delete coach with active coachees)
- [ ] Unique constraints work (e.g., duplicate KKJ not allowed)
- [ ] Null constraints enforced (required fields not null)

**Data Consistency**
- [ ] Assessment attempts counted correctly in history
- [ ] Coaching progress % calculated correctly
- [ ] Export files match database data exactly
- [ ] Soft-deletes work (archived records still queryable, but hidden in lists)

**Validation**
- [ ] Empty titles rejected on create
- [ ] Email format validated on user create
- [ ] Date ranges validated (start < end)
- [ ] Required dropdowns cannot submit empty

---

## IDP Plan Page (NEW Feature for v3.0)

### Current Behavior
- Non-existent or placeholder

### Target Behavior

**Coachee Dashboard**
- [ ] IDP Plan card visible on home dashboard
- [ ] Link navigates to `/CDP/IdpPlan` or similar

**IDP Plan Content**
- [ ] Shows assigned silabus items for coachee
- [ ] Each item displays:
  - Kompetensi name
  - SubKompetensi name
  - Deliverables to complete
  - Current progress %
  - Coaching guidance PDF link (downloadable)
- [ ] Items grouped by Track/Level for readability
- [ ] Timeline: expected completion date, actual completion date

**Download Functionality**
- [ ] "Download Guidance" button per deliverable
- [ ] PDF/DOC links work, files download correctly
- [ ] Multiple downloads don't break database
- [ ] Guidance file audit logged (who downloaded, when)

**Integration with Coaching Flow**
- [ ] When coach logs evidence against deliverable, IDP progress updates
- [ ] Coachee sees real-time % progress
- [ ] Completed deliverables marked with checkmark

---

## Feature Dependency Map

```
Assessment Creation
  └─> Assessment Scheduling
      └─> Exam Window Opens
          └─> Worker starts exam
              └─> Auto-save answers (v2.1)
                  └─> Session Resume (v2.1)
                      └─> Exam Submit
                          └─> Results Page
                              └─> History Tab
                                  └─> Assessment History (v2.2)

Coach Mapping (Admin)
  └─> Coach Dashboard (sees coachees)
      └─> Coaching Session Create
          └─> Evidence Upload
              └─> Coaching Report
                  └─> HC Approval Workflow
                      └─> Deliverable Progress Update
                          └─> IDP Plan Progress (NEW)
                              └─> Coachee sees completion %

Master Data (Admin)
  └─> KKJ Matrix CRUD
  └─> CPDP Items CRUD
  └─> Proton Silabus CRUD
  └─> Coaching Guidance Upload
      └─> Guidance Download (for IDP plan)
  └─> Audit Log (NEW)
```

---

## Manual QA Checklist (Phase 3.0)

### Assessment Flow (2 hours)

- [ ] **Create Assessment**
  - [ ] As Admin, create new assessment (title: "Safety Exam", category: "Online")
  - [ ] Verify success message, redirect to list
  - [ ] Verify assessment appears in list with correct title

- [ ] **Assign to Workers**
  - [ ] Select 3-5 test workers
  - [ ] Verify "Assignment Created" message
  - [ ] Login as worker, verify assessment in dashboard
  - [ ] Login as different worker (not assigned), verify NOT in dashboard

- [ ] **Exam Flow**
  - [ ] Worker starts exam, sees first question
  - [ ] Timer displays, counts down correctly
  - [ ] Click radio option, verify highlight
  - [ ] Navigate to next question, previous question works
  - [ ] Close browser during exam (simulate dropout)
  - [ ] Reopen, verify last page restored (session resume)
  - [ ] Submit exam, redirect to results page

- [ ] **Results & Retake**
  - [ ] Results show score (e.g., "7 / 10")
  - [ ] Pass/fail status displays correctly
  - [ ] Click "Retake", verify new attempt starts
  - [ ] History tab shows both attempts with attempt numbers

### Coaching Proton Flow (2 hours)

- [ ] **Mapping & Dashboard**
  - [ ] As Admin, map Coach "John" to Coachee "Alice"
  - [ ] Login as John (Coach), verify "Alice" in coachee list
  - [ ] Login as Alice (Coachee), verify "John" in coach list

- [ ] **Coaching Session**
  - [ ] Coach creates coaching session for Alice
  - [ ] Coach uploads evidence (photo/PDF)
  - [ ] Coach writes coaching report
  - [ ] Coach submits; verify success message
  - [ ] Alice login, verify coaching session visible with evidence link

- [ ] **Evidence & Reports**
  - [ ] Alice clicks evidence link, file downloads correctly
  - [ ] Export coaching report to PDF, verify content
  - [ ] Export to Excel, open in spreadsheet software, verify data

- [ ] **Approval Workflow**
  - [ ] Deliverable shows "Pending Approval" status
  - [ ] As HC, approve deliverable
  - [ ] Verify status changes to "Approved"
  - [ ] As HC, reject a different deliverable, verify status = "Rejected"
  - [ ] As HC, override (if applicable), verify takes precedence

### Master Data Management (1.5 hours)

- [ ] **KKJ Matrix**
  - [ ] View KKJ list, verify all positions, levels show
  - [ ] Add new KKJ: Position "Operator", Level "2"
  - [ ] Verify added row appears, data saved
  - [ ] Edit (pencil icon), rename to "Operator-L2"
  - [ ] Delete, modal shows affected coachees
  - [ ] Confirm delete, verify removed from list
  - [ ] Export to Excel, verify headers + data

- [ ] **Proton Silabus**
  - [ ] Admin panel, click Proton Data tab
  - [ ] Expand Kompetensi, verify SubKompetensi list
  - [ ] Expand SubKompetensi, verify Deliverables
  - [ ] Add new Deliverable, verify appears
  - [ ] Delete Deliverable, verify guard shows impact
  - [ ] Upload Coaching Guidance PDF
  - [ ] IDP Plan page (new), verify PDF link downloads

- [ ] **Audit Log** (NEW)
  - [ ] Admin panel, click Audit Log card
  - [ ] Filter by user, verify changes logged
  - [ ] Filter by entity type, verify assessment creates logged
  - [ ] Export audit report to Excel

### Authorization & Menus (1 hour)

- [ ] **Role-Based Access**
  - [ ] Login as Worker, verify cannot access /Admin → 403 error
  - [ ] Login as HC, verify can access /Admin/ManageAssessments
  - [ ] Navbar: Worker should see [Home, Dashboard, Settings], no "Kelola Data"
  - [ ] Navbar: HC should see [Home, Dashboard, Kelola Data, Settings]
  - [ ] Navbar: Admin should see all menus

- [ ] **Kelola Data Hub** (HC/Admin only)
  - [ ] Click Kelola Data menu
  - [ ] Verify 3 sections visible: Manajemen Pekerja, Assessment, Coaching
  - [ ] Verify Training Records card added (v2.6)
  - [ ] Each card navigates to correct page
  - [ ] Master data cards load without errors

### Code Analysis & Dead Code (30 min)

- [ ] **Run Analyzers**
  - [ ] `dotnet build /p:AnalysisLevel=latest` — review warnings
  - [ ] Fix CRITICAL warnings (null safety, dead code)
  - [ ] Verify no Selenium/Playwright code remains from old attempts

- [ ] **Verify Cleanup**
  - [ ] Search codebase for "ProtonMain" — should find 0 results
  - [ ] Search for "CMP/CreateTrainingRecord" — should find 0 (replaced by Admin)
  - [ ] Verify "Proton Progress" renamed to "Coaching Proton"

---

## Feature Priority for Phase 3.0

### Critical Path (Test First)
1. **Assessment End-to-End** (table stakes) — 20% effort
2. **Session Resume on Disconnect** (table stakes) — 10% effort
3. **Auto-Save During Exam** (differentiator, prevents data loss) — 10% effort
4. **Master Data CRUD** (table stakes) — 15% effort
5. **Coaching Approval Workflow** (table stakes) — 15% effort

### Secondary Path (Test After Critical)
6. **IDP Plan Page** (differentiator, new feature) — 15% effort
7. **Audit Log** (compliance, new) — 5% effort
8. **Live Monitoring** (differentiator) — 5% effort
9. **Code Analysis & Dead Code Cleanup** (ongoing) — 10% effort
10. **Rename "Proton Progress" → "Coaching Proton"** (UI/code) — 5% effort

---

## Sources & References

- `.planning/PROJECT.md` — Detailed scope
- `.planning/milestones/v2.1-ROADMAP.md` — Auto-save, session resume
- `.planning/milestones/v2.3-ROADMAP.md` — Master data CRUD
- `.planning/milestones/v2.4-ROADMAP.md` — Coaching workflow
- `.planning/milestones/v2.5-ROADMAP.md` — Authorization, Kelola Data hub
- `.planning/milestones/v2.6-ROADMAP.md` — Dead code removal

---

**Feature landscape for:** Portal HC KPB v3.0 — Full QA & Feature Completion
**Researched:** 2026-03-01
**Confidence:** HIGH
**Next:** Use this checklist to drive manual QA execution in Phase 3.0.
