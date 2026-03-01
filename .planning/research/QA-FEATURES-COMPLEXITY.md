# QA Testing: Feature Complexity & Dependencies Matrix

**Project:** PortalHC KPB v3.0 — Comprehensive End-to-End Testing
**Researched:** 2026-03-01
**Purpose:** Inform phase ordering, identify blockers, scope differentiators vs MVP

---

## Feature Complexity Rankings

### Table Stakes (Must-Have for v3.0)

#### 1. Assessment E2E Flow
**Overall Complexity: HIGH**
- **Subfeatures:**
  - Assessment Creation & Configuration (MEDIUM) — title, package, schedule, threshold, review visibility
  - Bulk User Assignment (MEDIUM) — section-filtered picker, idempotent, deduplication
  - Exam Flow (MEDIUM) — question navigation, answer types, timeout handling
  - Auto-Save & Resume (MEDIUM) — 30-sec interval, browser close → refresh recovery
  - Results Display (LOW) — score calculation, pass/fail, competencies earned
  - Assessment History (MEDIUM) — Riwayat tab, attempt numbering, archived attempts
  - HC Monitoring Dashboard (HIGH) — live progress, HC action suite (Reset/Close/Force/Token)
  - Data Integrity (MEDIUM) — FK cascades, referential integrity, multi-unit scenarios

**Why HIGH:**
- 7 interconnected subfeatures; changes to one ripple to others
- Auto-save/resume requires session state management across browser session boundaries
- Monitoring dashboard live updates require either polling or WebSocket (complexity tradeoff)
- History tracking with attempt numbering requires AssessmentAttemptHistory table + archive-before-clear logic
- **Blocks:** Coaching Proton E2E (final approval creates auto-assessment)

**QA Effort:** 45 min end-to-end

#### 2. Coaching Proton E2E Flow
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - Coach-Coachee Mapping (LOW) — section validation, bulk import
  - Coaching Session Creation (LOW) — coachee picker (mapped only), evidence notes + files
  - 3-Tier Approval Workflow (MEDIUM) — Sr Supervisor → Section Head → HC; rejection loops
  - Auto-Assessment Creation (MEDIUM) — HC final approval triggers assessment creation
  - Competency Auto-Update (MEDIUM) — KKJ level updates; monotonic progression validation
  - Evidence & History (LOW) — consolidated modal, audit trail

**Why MEDIUM:**
- Dependent on Assessment E2E working correctly (final approval creates assessment)
- Approval workflow is state-machine (complex state transitions, but well-contained)
- Competency update is straightforward (query-based lookup + level increment)
- **Blocks:** Nothing in v3.0, but is the primary development flow

**Depends On:** Assessment E2E (hard dependency)

**QA Effort:** 30 min end-to-end

#### 3. Master Data Management CRUD
**Overall Complexity: MEDIUM**
- **Subfeatures per entity type:**
  - KKJ Matrix CRUD (LOW) — create, edit level, delete with cascade
  - CPDP Items CRUD (LOW) — standard CRUD, no FK complexity
  - Worker Management (MEDIUM) — multi-unit assignment, bulk import, search/filter
  - Coach-Coachee Mapping CRUD (MEDIUM) — section validation, bulk import, historical preservation
  - Assessment Packages & Questions (MEDIUM) — shuffle validation, cascade delete, bulk import
  - Silabus & Coaching Guidance (LOW) — file upload, metadata CRUD, download links

**Why MEDIUM:**
- Individual entities are LOW-MEDIUM complexity
- Aggregate complexity from 6 entity types + audit logging on all CRUD operations
- Referential integrity (FK cascades) requires careful testing per entity
- Bulk import for workers + mappings adds complexity (duplicate handling, transaction semantics)
- **Blocks:** All other flows (no assessments/coaching without master data)

**QA Effort:** 20 min per entity type; 2 hours total

#### 4. Role-Based Access Control (10 Roles)
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - Admin role (Level 1) — full CRUD, see all
  - HC role (Level 2) — full CRUD, all data, no user/role management
  - Management roles (Level 3: Direktur, VP, Manager) — see all data, some approval authority
  - Section Head (Level 4) — see section data only, L2 approval authority
  - Sr Supervisor (Level 4) — see section + unit data, L1 approval authority
  - Coach/Supervisor (Level 5) — see own coachees only, can create sessions
  - Coachee (Level 6) — see own data only
  - Plus: Navbar visibility gating, form field visibility per role, authorization filters on controllers

**Why MEDIUM:**
- 10 roles across 6 authorization levels; role hierarchy well-defined
- Section/unit filtering requires careful FK filtering in queries
- Role-aware view models need per-role conditional rendering
- Cross-role access attempts → must return 403 (security boundary)
- **Blocks:** All flows (broken auth breaks everything)

**QA Effort:** 15 min per role (150 min total) or 1 smoke test per role (15 min total)

#### 5. Exam Auto-Save & Resume
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - Auto-save interval (30 sec) triggered by client-side timer
  - SaveAnswer endpoint persists PackageUserResponse rows
  - Session state preserved across browser close + refresh
  - Resume exam from same question without re-answering prior questions

**Why MEDIUM:**
- Requires client-side state management (timer) + server-side session tracking
- Browser close → session interruption → recovery is edge case
- Concurrent SaveAnswer calls from same user (potential race condition)
- **Blocks:** Assessment E2E (core UX feature)

**QA Effort:** 10 min focused test

#### 6. Dashboard & Navigation (Role-Aware UX)
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - Home page role-specific cards (Assessment Lobby, Manage Assessments, Kelola Data hub)
  - Kelola Data hub 3-section organization (Manajemen Pekerja, Assessment, Coaching)
  - Navbar visibility gating (HC sees Kelola Data menu, Coach does not)
  - Card statistics (count badges) accuracy
  - All links navigate to working pages without 404

**Why MEDIUM:**
- View model filtering per role (straightforward but error-prone)
- Hub card organization requires coordination across 12+ cards
- Card statistics queries must match displayed data (aggregation complexity)
- **Blocks:** UX entry point (broken nav kills adoption)

**QA Effort:** 15 min per role

#### 7. Dual Authentication (AD + Local Fallback)
**Overall Complexity: LOW**
- **Subfeatures:**
  - AD login (primary) — LDAP authentication against domain
  - Local login (fallback) — AspNetUsers table password hash
  - User sync (IAuthService.SyncADUser) — on first login or repeat login
  - Fallback resilience — when AD down, local still works

**Why LOW:**
- IAuthService abstraction keeps AD-specific logic isolated
- Fallback logic is straightforward (try AD → catch → try local)
- User sync is idempotent (can run multiple times safely)
- **Blocks:** Access to portal (broken auth blocks everything)

**QA Effort:** 10 min focused test

#### 8. Training Records History (with Attempt #)
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - AssessmentAttemptHistory table tracks all archived attempts
  - ResetAssessment archives before clearing (no data loss)
  - Riwayat tab shows chronological list with attempt numbers
  - Merged view with Training Manual records

**Why MEDIUM:**
- Requires AssessmentAttemptHistory table + data migration
- ResetAssessment two-step logic (archive then clear) requires transaction
- History queries need proper sorting (newest first) + filtering
- Attempt numbering is count-based (simple) but must be accurate
- **Blocks:** Assessment E2E (core feature)

**QA Effort:** 10 min focused test

#### 9. Assessment Result Configuration (Threshold + Review Visibility)
**Overall Complexity: LOW**
- **Subfeatures:**
  - HC sets pass threshold (0-100%) per assessment
  - HC toggles answer review visibility (yes/no)
  - Grading logic applies threshold at submit time
  - Results view conditionally shows questions based on review visibility

**Why LOW:**
- Threshold application is straightforward (score >= threshold → pass)
- Review visibility is boolean toggle (simple conditional rendering)
- No complex state management required
- **Blocks:** Assessment results UX (minor impact if broken)

**QA Effort:** 10 min focused test

#### 10. Section-Level Access Filtering (Section Heads, Sr Supervisors, Coaches)
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - User picker filtered to show only user's section workers
  - Worker table/list filtered by section (query-level filtering)
  - Cross-section URL bypass blocked with 403
  - Section FK column used in all queries for filtering

**Why MEDIUM:**
- Requires FK filtering on multiple queries (Worker list, Assessment list, Coaching list)
- Section value stored in User.Section (single value) → scope filtering
- URL bypass prevention requires authorization filters on controller actions
- **Blocks:** Role-based access (security boundary)

**QA Effort:** 15 min focused test

#### 11. Kelola Data Hub Organization (3 Sections)
**Overall Complexity: MEDIUM**
- **Subfeatures:**
  - 3 sections: Manajemen Pekerja (workers, units, coach-coachee), Assessment (monitoring, training, questions, packages), Data Proton (KKJ, CPDP, Silabus)
  - HC-only nav menu visibility
  - Card stats (counts) accuracy
  - All cards link to working pages

**Why MEDIUM:**
- Hub layout coordination across 12+ cards
- Card statistics must query accurately (complex aggregations)
- Role-aware visibility (HC/Admin see all, Coach sees none)
- **Blocks:** Master data navigation (critical for HC/Admin UX)

**QA Effort:** 15 min focused test

#### 12. Multi-Unit User Support (Users in 2+ Units)
**Overall Complexity: LOW**
- **Subfeatures:**
  - User.Units Many-to-Many relationship (can have 2+ units)
  - Filter/picker shows all assigned units
  - Assessment assignment respects all unit memberships
  - Search/filter works per unit

**Why LOW:**
- Many-to-Many relationship is standard EF Core pattern
- Filtering logic is straightforward (worker.Units.Any(u => u.Id == selectedUnit))
- No complex state management required
- **Blocks:** User model (if missing, all user-based features fail)

**QA Effort:** 10 min focused test

---

### Differentiators (Nice-to-Have)

#### 1. Real-Time Assessment Monitoring (Live Progress)
**Complexity: HIGH**
- Polling-based page refresh (5-10 sec) vs WebSocket (signalR)
- Polling approach: Frontend setTimeout + fetch AssessmentMonitoringDetail data
- WebSocket approach: SignalR hub broadcasting updates to all HC monitoring same group
- **Trade-off:** Polling is simple but adds latency + server load; WebSocket is real-time but complex infrastructure

**Recommendation:** Keep polling for v3.0; consider WebSocket for v3.2 if monitoring feedback demands it

#### 2. Archive-Before-Clear Assessment Lifecycle
**Complexity: MEDIUM**
- ResetAssessment must archive completed session before clearing answers
- Archive logic: INSERT into AssessmentAttemptHistory (Score, IsPassed, etc.) BEFORE DELETE PackageUserResponse
- Single transaction ensures consistency (all-or-nothing)
- History shows archived attempt with original score + attempt number

**Recommendation:** Essential for attempt history; implement in Assessment E2E phase

#### 3. Auto-Update Competency (Monotonic Progression)
**Complexity: MEDIUM**
- On assessment completion, query AssessmentCompetencyMap for category → KKJ mapping
- Update worker's KKJ level to MAX(current_level, new_level) (monotonic: never decreases)
- Trigger on SubmitExam + ResetAssessment + FinalAssessmentApproval

**Recommendation:** Essential for Coaching Proton E2E; implement in coaching phase

#### 4. Governance & Audit Trail (AuditLog Service)
**Complexity: MEDIUM**
- AuditLogService logs all HC management actions (Create, Edit, Delete, Reset, Close, Regenerate)
- Fields: Actor NIP, Timestamp, Action Type, Entity Type, Entity ID, Before/After (optional)
- Paginated view (25/page) for HC/Admin access
- New AuditLog hub card in Kelola Data

**Recommendation:** Essential for enterprise governance; implement in Master Data phase

#### 5. Bulk Import with Templates (Workers, Mappings)
**Complexity: MEDIUM**
- Download template (Excel) with headers: NIP, Name, Section, Unit1, Unit2 (for workers)
- Upload file → parse → validate → bulk insert
- Duplicate NIP handling: skip or update existing
- Success message with count; redirect to list

**Recommendation:** Efficiency feature; defer if schedule tight; implement in Master Data phase

#### 6. Coaching Evidence Consolidation (Single Modal)
**Complexity: LOW**
- Coaching session detail → Evidence section → single modal with notes + files
- No separate tabs for notes vs files (consolidated UX)
- Modal shows file list with download links

**Recommendation:** UX improvement; implement in Coaching Proton phase

#### 7. Bulk Assign with Deduplication (Same Title+Category+Schedule)
**Complexity: MEDIUM**
- CreateAssessment detects sibling sessions (same title+category+schedule for same section)
- EditAssessment applies same deduplication when adding workers
- Prevents accidental duplicate cohorts

**Recommendation:** Data integrity feature; implement in Assessment phase

---

## Feature Dependencies Map

```
FOUNDATION (no dependencies):
  ├─ Auth System (AD + Local)
  │   └─ Requires: IAuthService, AspNetUsers table
  └─ User Roles (10 roles, 6 levels)
      └─ Requires: UserRoles.cs, AspNetUserRoles table

MASTER DATA (depends on Foundation):
  ├─ KKJ Matrix
  │   └─ Requires: KKJCompetency table
  ├─ CPDP Items
  │   └─ Requires: CDPItem table
  ├─ Workers (Multi-Unit)
  │   └─ Requires: ApplicationUser + User_Unit Many-to-Many
  ├─ Coach-Coachee Mapping
  │   └─ Requires: BidirectionalCoachMapping table
  ├─ Assessment Packages & Questions
  │   └─ Requires: AssessmentPackage, PackageQuestion, PackageOption tables
  │   └─ Requires: KKJ Matrix for category mapping
  └─ Silabus & Coaching Guidance
      └─ Requires: File storage (local or cloud)

ASSESSMENT E2E (depends on Master Data + Foundation):
  ├─ Assessment Creation
  │   └─ Requires: Assessment table, AssessmentPackage FK
  │   └─ Requires: KKJ Matrix for category → competency mapping
  ├─ Bulk User Assignment
  │   └─ Requires: User picker, UserPackageAssignment creation logic
  ├─ Exam Flow
  │   └─ Requires: AssessmentSession, PackageUserResponse tables
  │   └─ Requires: Question randomization (shuffledIds)
  ├─ Auto-Save & Resume
  │   └─ Requires: SaveAnswer endpoint, session state persistence
  ├─ Results Display
  │   └─ Requires: AssessmentCompetencyMap for KKJ updates
  │   └─ Requires: Assessment.PassThreshold, ReviewVisibility settings
  ├─ Assessment History
  │   └─ Requires: AssessmentAttemptHistory table, ResetAssessment archive logic
  └─ HC Monitoring Dashboard
      └─ Requires: AssessmentMonitoringDetail view model, polling/WebSocket

COACHING PROTON E2E (depends on Assessment E2E + Master Data + Foundation):
  ├─ Coach-Coachee Mapping (depends on: Master Data)
  │   └─ Requires: BidirectionalCoachMapping table
  ├─ Coaching Session Creation
  │   └─ Requires: CoachingSession table, Evidence file storage
  ├─ 3-Tier Approval Workflow
  │   └─ Requires: CoachingSession.ApprovalStatus state machine
  │   └─ Requires: Role hierarchy (Sr Supervisor < Section Head < HC)
  ├─ Auto-Assessment Creation
  │   └─ Requires: Assessment E2E working (final approval creates assessment)
  │   └─ Requires: ProtonAssessment flag on Assessment
  └─ Competency Auto-Update
      └─ Requires: Assessment E2E completion (SubmitExam endpoint)
      └─ Requires: AssessmentCompetencyMap + KKJ Matrix

DASHBOARD & NAVIGATION (depends on Master Data + Foundation):
  ├─ Home Page Role-Specific Cards
  │   └─ Requires: HomeController.Index view model per role
  ├─ Kelola Data Hub Organization
  │   └─ Requires: Hub layout configuration, 3-section card grouping
  └─ Card Statistics
      └─ Requires: Aggregation queries (assessment count, pending approvals, etc.)

DIFFERENTIATORS (depend on core flows):
  ├─ Real-Time Monitoring (depends on: Assessment E2E)
  ├─ Archive-Before-Clear (depends on: Assessment E2E)
  ├─ Auto-Update Competency (depends on: Assessment E2E + Coaching Proton E2E)
  ├─ Governance & Audit Trail (depends on: Master Data CRUD)
  ├─ Bulk Import (depends on: Master Data CRUD)
  └─ Evidence Consolidation (depends on: Coaching Proton E2E)
```

---

## Testing Blocking Relationships

| If **X** Fails | Then **Y** is Blocked | Why |
|---------------|----------------------|-----|
| Auth System | **Everything** | Can't log in; all flows blocked |
| Master Data Seeding | Assessment E2E, Coaching E2E, Master Data CRUD | No KKJ, workers, coaches to use in tests |
| Assessment E2E | Coaching Proton E2E | Final approval creates assessment; can't test without Assessment working |
| Assessment E2E | Competency Auto-Update | Auto-update triggered on assessment completion; can't test without Assessment working |
| Coaching Proton E2E | (Nothing in v3.0) | Coaching is enhancement; not blocking for MVP |
| Master Data CRUD | Audit Trail logging | CRUD operations must log to AuditLog; can't test audit without CRUD working |
| Role-Based Access | Dashboard & Navigation | Role-aware card visibility depends on User.IsInRole() checks; can't test without Auth working |

---

## Recommended QA Phase Ordering

| Phase | Focus | Complexity | Duration | Blocker Status | Go/No-Go Criteria |
|-------|-------|-----------|----------|---|---|
| **1** | Foundation (Auth, Nav, Master Data seed) | LOW | 30 min | Blocks all others | Auth works; Nav links functional; seed data present |
| **2** | Assessment E2E | HIGH | 45 min | Blocks Coaching | All assessment tests PASS (create, exam, results, history, monitoring) |
| **3** | Coaching Proton E2E | MEDIUM | 30 min | Blocks (nothing v3.0) | Approval chain works; auto-assessment created; competency updated |
| **4** | Master Data CRUD + Integrity | MEDIUM | 2 hours | (Concurrent with 2-3) | All entity CRUD works; FK cascades; audit log entries |
| **5** | Cross-Role & Edge Cases | MEDIUM | 1 hour | (After 2-4) | Role boundaries enforced; multi-unit works; history recovery works |
| **6** | Code Cleanup | LOW | 1 hour | (After all) | ProtonMain removed; Proton Progress renamed; orphaned cards removed |
| **7** | UAT Gate & Release Prep | LOW | 1 hour | (After all) | Full cycle PASS; stakeholder sign-off |

**Total Effort:** 7-8 weeks if phases sequential; 4-5 weeks if phases 4-5 run in parallel with 2-3

---

## Sources

- **Complexity Assessment:** Industry best practices (BrowserStack UAT, Katalon), codebase analysis
- **Dependencies:** Feature relationship analysis from PROJECT.md, Controllers, Models
- **Blocking Relationships:** Logical inference from feature dependencies

---

*QA Features: Complexity & Dependencies Matrix*
*Researched: 2026-03-01 | Confidence: HIGH*
