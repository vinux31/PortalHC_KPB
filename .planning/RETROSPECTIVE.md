# Project Retrospective: Portal HC KPB

*A living document updated after each milestone. Lessons feed forward into future planning.*

---

## Milestone: v4.0 — E2E Use-Case Audit

**Shipped:** 2026-03-12
**Phases:** 6 (153-158) | **Plans:** 16

### What Was Built
- Assessment flow audit: Fixed FK crash, open redirect, certificate access control, TrainingRecord auto-creation
- Coaching Proton audit: Fixed mapping reactivation cascade, ProtonFinalAssessment creation on interview pass
- Admin Kelola Data audit: Fixed DeleteWorker cascade order, CPDP MIME type, missing audit log entries
- CDP Dashboard audit: Fixed URL manipulation, duplicate key crash on multiple assignments
- Auth audit: Full 7-controller authorization matrix verified
- Navigation audit: All links verified, GuideDetail case-sensitivity fix

### What Worked
- **Use-case flow audit format**: Organizing by flow (not by page/role) caught cross-cutting bugs (e.g., cascade order in DeleteWorker affecting coaching data)
- **Hybrid code+browser UAT**: Code review found bugs that browser testing alone would miss (security issues, edge cases); browser UAT confirmed fixes worked
- **Independent phases**: All 6 audit phases were independent — could execute in any order without blocking
- **Budget model profile**: Sonnet executor handled audit-style work (read → analyze → fix → verify) efficiently

### What Was Inefficient
- **SUMMARY frontmatter gaps**: All SUMMARY files have empty `requirements_completed` arrays — systematic gap in audit-style summary writing
- **Nyquist validation skipped**: All 6 phases missing VALIDATION.md — audit phases don't fit the Nyquist pattern well (they're verification, not feature-building)

### Patterns Established
- **Audit phase pattern**: Code review → findings document → targeted fixes → browser UAT → VERIFICATION.md
- **Authorization matrix**: Full controller-level authorization audit as a reusable verification approach

### Key Lessons
1. Audit milestones are faster than build milestones — 6 phases in 2 days vs typical 3-4 days
2. The v3.0 known gaps (Phase 89 PlanIDP, ASSESS-04 PositionTargetHelper) were both resolved by this audit
3. Tech debt documentation (10 items) provides a clear backlog for future work without blocking the milestone

### Cost Observations
- Model mix: 100% sonnet (executor)
- Sessions: ~4 sessions across 2 days
- Notable: Highest requirements-per-day ratio (33 requirements in 2 days)

---

## Milestone: v3.21 — Account Profile & Settings Cleanup

**Shipped:** 2026-03-11
**Phases:** 1 (Phase 152) | **Plans:** 1 | **Tasks:** 2

### What Was Built
- AccountController authorization hardened with class-level `[Authorize]` + `[AllowAnonymous]` on Login/AccessDenied
- ProfileViewModel decoupling Profile view from ApplicationUser entity
- Client-side validation on Settings page, international phone regex support
- Profile page UI polish (button label, row spacing)

### What Worked
- Single-phase milestone for small cleanup tasks — fast turnaround, no dependency overhead
- Budget model profile (sonnet executor) handled the straightforward changes efficiently

### What Was Inefficient
- Nothing significant — simple milestone executed cleanly

### Patterns Established
- Read-only profile pages should use a dedicated ViewModel, not the entity model directly

### Key Lessons
- Small cleanup milestones (6 requirements, 1 phase) can ship in a single session with minimal overhead

### Cost Observations
- Model mix: 100% sonnet (executor + verifier)
- Sessions: 1
- Notable: Entire milestone (plan → execute → verify → UAT → archive) completed in one session

---

## Milestone: v2.2 — Attempt History

**Shipped:** 2026-02-26
**Phases:** 1 (Phase 46) | **Plans:** 2 | **Tasks:** 4

### What Was Built
- AssessmentAttemptHistory table — archive row written at Reset time, preserving Score, IsPassed, AttemptNumber, timestamps
- Archival logic in ResetAssessment — Completed sessions only; archive + reset share one SaveChangesAsync
- Unified history query — merged archived + current Completed sessions with batch Attempt # computation (GroupBy avoids N+1)
- Dual sub-tab History tab — Riwayat Assessment + Riwayat Training with client-side filters (worker search + title dropdown)

### What Worked
- Archive-before-clear pattern: inserting the archive block before UserResponse deletion meant session field values were still available — no extra query needed to capture them
- Batch count pattern: computing archived AttemptNumber via one GroupBy query + ToDictionary lookup eliminated N+1 for all current session rows
- Tuple return from helper: returning `(assessment, training)` from GetAllWorkersHistory() kept the two sorted/shaped lists cleanly separated without a discriminator flag

### What Was Inefficient
- Plan spec said "3 plans" but 2 plans covered all requirements cleanly — the spec was slightly over-estimated; quick review before planning could have reduced to 2 upfront

### Patterns Established
- **Archive-before-clear**: When resetting stateful records, archive the current row *before* deletions/resets so field values are still available in memory. Share the downstream SaveChangesAsync.
- **Batch count for sequence numbers**: Compute `AttemptNumber` as `existingRows.Count + 1` using a single `GroupBy` across all (UserId, Title) pairs, then dictionary lookup per row — no sequence column needed.
- **Nested Bootstrap sub-tabs**: `ul.nav.nav-tabs` inside an existing `div.tab-pane` works cleanly for two-level navigation; default active sub-tab set via `active show` classes.
- **Client-side `data-*` filter**: `data-worker` + `data-title` attributes on `<tr>` elements; JS filterAssessmentRows() reads both inputs and sets `row.style.display` — no round-trip, works with static server render.

### Key Lessons
1. EF migrations require `--configuration Release` when the Debug build exe is locked by a running process — standard environment constraint for this project.
2. `GetAllWorkersHistory()` returning a tuple is appropriate when two result sets have fundamentally different shapes (sort order, columns) — don't force them into a single typed list.
3. For sequential numbering without a DB sequence: count existing rows for the same (UserId, title) key, add 1. Consistent at both archive time (Plan 01) and query time (Plan 02).

### Cost Observations
- Model profile: budget
- 1-day milestone (one sitting)
- Fast execution: 2 plans × ~10 min average = ~20 min total active work

---

## Milestone: v2.3 — Admin Portal

**Shipped:** 2026-03-01
**Phases:** 8 (47-53, 59) | **Plans:** 29 | **Tasks:** 33

### What Was Built
- AdminController with 12-card hub page — centralized admin tool access with role-gated navigation
- KKJ Matrix & CPDP Items spreadsheet editors — inline editing, bulk-save, multi-cell clipboard, Excel export
- Assessment Management migration — all manage actions (Create/Edit/Delete/Reset/ForceClose/Export/Monitoring/History) from CMP to Admin
- Coach-Coachee Mapping manager — grouped-by-coach view with bulk assign, soft-delete, section filter
- Proton Silabus & Coaching Guidance — two-tab /Admin/ProtonData replacing ProtonCatalog with CRUD + file management
- DeliverableProgress Override — third ProtonData tab for HC to fix stuck records; sequential lock removed entirely
- Final Assessment Manager — Assessment Proton exam category with eligibility gates and Tahun 3 interview workflow
- ProtonCatalog cleanup — dead controller/views removed after full migration

### What Worked
- **Admin hub pattern**: Single AdminController with card-based Index page scales well — each phase added 1-2 new tool pages without architectural changes
- **Spreadsheet-style editing**: Bulk-save pattern (collect all rows as JSON, POST once) is faster and more reliable than per-row AJAX
- **Phased migration**: Moving Assessment Management in 5 plans (scaffold → CRUD → monitoring → cleanup → gap fixes) prevented breaking changes mid-milestone
- **SUMMARY.md extraction**: Phase summaries provided quick context restoration across sessions

### What Was Inefficient
- **GAP plans**: Phases 47-49 each needed UAT gap plans (47-03/04/05, 48-04, 49-05) — initial plans didn't fully capture UI requirements, requiring correction passes
- **Phase removal churn**: 4 phases were removed/superseded during execution (56, 57, 58, 60) — upfront requirements were over-scoped
- **5 requirements left incomplete**: OPER-05, CRUD-01 through CRUD-04 were planned but phases were removed before execution

### Patterns Established
- **Admin hub card pattern**: Each admin tool gets a card on Admin/Index with icon, description, and link; cards grouped by domain (Data Management, Proton, Assessment & Training)
- **Spreadsheet editor pattern**: Read-mode table → Edit mode toggle → JSON bulk-save POST → DOM re-render; multi-cell clipboard via getTableCells() 2D array
- **AuditLog on write**: All admin write operations log to AuditLog via AuditLogService.LogAsync — consistent across all admin tools
- **ProtonData tab pattern**: Multiple admin tools sharing one route (/Admin/ProtonData) via Bootstrap nav-tabs — reduces hub card count

### Key Lessons
1. Scope v2.3 requirements more tightly — 12 requirements was too many; 7 shipped, 5 deferred. Better to ship smaller milestones with 100% completion.
2. GAP plans are a sign that initial discuss-phase didn't capture enough detail. Future phases should include more concrete UI mockup questions.
3. Migration phases (49, 59) are clean and predictable — the pattern of "move, verify references, delete originals" works reliably.
4. Sequential lock removal (Phase 52) was the right call — Active-on-assignment is simpler to understand and maintain.

### Cost Observations
- Model profile: budget
- 4-day milestone across multiple sessions
- 29 plans is the largest milestone yet — previous max was v1.7 with 14 plans

---

## Milestone: v2.4 — CDP Progress

**Shipped:** 2026-03-01
**Phases:** 4 (61-64) | **Plans:** 9

### What Was Built
- ProtonProgress page with data from ProtonDeliverableProgress + ProtonTrackAssignment — replaced IdpItems data source
- 5 filter parameters (Bagian/Unit, Coachee, Track, Tahun) wired to EF Core Where composition with role-scope-first pattern
- Per-role approval workflow: SrSpv/SectionHead/HC each with independent approval columns; data migration backfills from existing records
- Combined coaching report + evidence submission modal; CoachingSession FK linked to deliverable progress
- Excel (ClosedXML) + PDF (QuestPDF) export from Progress page
- Group-boundary server-side pagination (20 rows/page) with 3 empty state scenarios

### What Worked
- **Role-scope-first pattern**: Deriving scopedCoacheeIds from the logged-in user's role before applying any URL parameters ensures security by default — filters only narrow within already-authorized scope
- **EF Where composition**: Chaining `.Where()` calls on IQueryable and calling single `ToListAsync()` at the end keeps all filtering server-side with clean code structure
- **Per-role approval**: Independent columns for SrSpv/SH/HC approvals allowed each role to act independently without blocking others; data migration backfilled cleanly

### What Was Inefficient
- Phase renumbering (originally 63-66, renamed to 61-64 after phase removal) created git commit number mismatch — commits reference old numbers while SUMMARY files use new numbers

### Patterns Established
- **Role-scope-first filtering**: Always scope data by user role (CoachCoacheeMapping for coach, section for SrSpv, etc.) before applying user-selected filters
- **Per-role approval columns**: Independent approval per authorization level; any rejection overrides overall status; individual approvals don't cascade
- **Group-boundary pagination**: Group rows by logical unit (coachee + kompetensi + sub), slice groups into pages without splitting — better UX than arbitrary row cuts

### Key Lessons
1. ProtonProgress was a complete rewrite — the existing page was stub/mock data. Starting fresh was faster than patching.
2. QuestPDF was added for PDF export alongside ClosedXML for Excel — two export libraries now coexist cleanly.
3. Per-role approval migration needed a data fix (Locked→Pending) that was combined with schema migration — efficient single migration.

### Cost Observations
- Model profile: budget
- 2-day milestone (Feb 27-28)
- Executed in parallel with v2.5 phases

---

## Milestone: v2.5 — User Infrastructure & AD Readiness

**Shipped:** 2026-03-01
**Phases:** 8 (65-72) | **Plans:** 14

### What Was Built
- Dynamic Profile page bound to @model ApplicationUser; null-safe fallback; avatar initials from FullName
- Functional Settings page with ChangePassword, EditProfile (FullName/Position), and disabled placeholder items
- ManageWorkers migration: 11 actions from CMPController → AdminController with HC access; clean break (no redirects)
- Kelola Data hub: Admin/Index restructured into 3 domain sections; HC nav access extended
- Dual auth infrastructure: IAuthService + LocalAuthService + LdapAuthService; config toggle; System.DirectoryServices NuGet
- Login flow: IAuthService-based auth; AD hint; profile sync (FullName/Email); unregistered user rejection
- User structure: UserRoles.GetDefaultView() helper; SeedData modernization; AuthSource lifecycle (added then removed)
- Hybrid auth: HybridAuthService wraps AD-first + local fallback for admin user

### What Worked
- **IAuthService abstraction**: Clean separation of auth concerns behind interface — switching from local to AD requires only config toggle, no code changes
- **Clean break migration**: Removing old CMP ManageWorkers entirely (no redirects) eliminated dead code and confusion about canonical URLs
- **Kelola Data hub 3-section layout**: Grouping admin tools by domain made navigation intuitive; HC users can now access worker management directly

### What Was Inefficient
- **AuthSource field lifecycle**: Added in Phase 69 (per-user auth source), removed in Phase 72 (global config routing). The discuss-phase for Phase 69 should have concluded global routing earlier.
- **Phase 71 SeedData cleanup needed Phase 72 follow-up**: Modernizing SeedData in Phase 71 revealed the Admin KPB user needed hybrid auth fallback, requiring an entire additional phase (72)

### Patterns Established
- **IAuthService for dual auth**: Interface with Task<AuthResult> pattern; AuthenticationConfig POCO for LDAP settings; DI factory delegate for config-based registration
- **HybridAuthService composite**: Wraps two concrete services; email-based routing for fallback; silent failure semantics (same error UX regardless of which path failed)
- **GetDefaultView() single source of truth**: Role → SelectedView mapping extracted to static helper; SeedData and runtime both use same function

### Key Lessons
1. Global config routing (UseActiveDirectory toggle) is simpler than per-user AuthSource — one config flag instead of per-row DB field
2. Hybrid auth pattern solves the "admin needs local login in AD mode" problem elegantly — HybridAuthService tries AD first, falls back to local for specific email
3. The Supervisor role (level 5) addition was necessary for production role hierarchy — discovered during implementation, not during requirements

### Cost Observations
- Model profile: budget
- 2-day milestone (Feb 27-28), executed in parallel with v2.4
- 8 phases is second-largest (v2.3 had 8 phases too) but 14 plans is more manageable than v2.3's 29

---

## Milestone: v2.7 — Assessment Monitoring

**Shipped:** 2026-03-01
**Phases:** 3 (79-81) | **Plans:** 4

### What Was Built
- Assessment Monitoring group list page (/Admin/AssessmentMonitoring) with real-time stats, search/filter, status badges, Regenerate Token
- Per-participant monitoring detail with live progress, countdown timer, token card with inline copy/regenerate
- Full HC action suite on dedicated monitoring page (Reset, Force Close, Bulk Close, Close Early, Regenerate Token)
- Admin ManageQuestions page (ManageQuestions GET, AddQuestion POST, DeleteQuestion POST) accessible from ManageAssessment dropdown
- Hub cleanup — Monitoring dropdown removed from ManageAssessment, Training Records card removed from Section C, table min-height styling

### What Worked
- **Focused extraction pattern**: Moving existing monitoring functionality from a dropdown to a dedicated page was clean — controller actions already existed, just needed new views and navigation wiring
- **discuss-phase context capture**: CONTEXT.md for Phase 81 captured 4 distinct items (2 removals, 1 addition, 1 styling fix) which let the planner create well-scoped plans
- **Budget profile efficiency**: Sonnet planner/executor with haiku checker/verifier delivered all 4 plans without iteration — checker passed on first try for all phases
- **Single-day milestone**: All 3 phases planned and executed in one session with no blockers

### What Was Inefficient
- **Plan-index wave mismatch**: Plan 81-02 frontmatter specified wave 2 with depends_on 81-01, but the plan-index tool returned both as wave 1 — had to manually verify and enforce correct wave ordering
- **Summary one-liner extraction**: summary-extract returned null for all one_liner fields — summaries may not have the expected field format

### Patterns Established
- **Monitoring extraction pattern**: When a feature outgrows a dropdown action, create dedicated page with group list → detail drill-down → actions; remove old entry point last
- **Admin controller mirroring**: Copying CMP controller actions to AdminController with only redirect-target changes provides Admin-context equivalent pages without shared code complexity

### Key Lessons
1. Small focused milestones (3 phases, 4 plans) execute cleanly in a single session — ideal scope for extraction/cleanup work
2. Phase 81's discuss-phase captured a bonus feature (ManageQuestions) that wasn't in original requirements — discuss-phase is the right place to expand scope
3. CLN-01/CLN-02 cleanup phases should always be last — ensures new functionality is verified before removing old entry points

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker/verifier/researcher)
- 1-session milestone (~2 hours total)
- 4 plans is the smallest non-trivial milestone

---

## Milestone: v3.0 — Full QA & Feature Completion

**Shipped:** 2026-03-05
**Phases:** 10 (82-91, 86 superseded) | **Plans:** 34

### What Was Built
- Cleanup & Rename: "Proton Progress" → "Coaching Proton" throughout, orphaned CMP pages removed, AuditLog card added
- Master Data QA: Worker/Silabus soft delete infrastructure with IsActive filters across all queries
- Assessment Flow QA: Full lifecycle verified (create, assign, exam, auto-save, results, certificate)
- Coaching Proton QA: Full coaching workflow verified (mapping, evidence, multi-level approval, exports)
- Dashboard & Navigation QA: All dashboards role-scoped, login flow secure, nav visibility enforced
- KKJ Matrix Full Rewrite: Document-based file management (KkjFile/CpdpFile) replacing spreadsheet
- PlanIDP 2-Tab Redesign: Unified Silabus + Coaching Guidance tabs for all roles
- Admin & CMP Assessment Audit: 20 assessment flows verified with CSRF fixes and Records redesign

### What Worked
- **Use-case flow QA**: Testing by flow (not by page) caught cross-page integration issues that page-level testing would miss
- **Soft delete infrastructure**: Adding IsActive to ApplicationUser and ProtonKompetensi once, then filtering everywhere, was cleaner than scattered delete logic
- **Browser verification pattern**: Claude analyzes code → user verifies in browser → Claude fixes bugs. Efficient division of labor.
- **Supersession**: Phase 86 → 89 pivot was clean — superseding a phase and creating a better-scoped replacement avoids sunk cost

### What Was Inefficient
- **Phase 89 missing VERIFICATION.md**: Phase shipped without verification file — gap discovered only during milestone audit
- **Phase 88 verification mismatch**: VERIFICATION.md claims don't match actual codebase state — verification was written without re-reading the final code
- **PositionTargetHelper gap**: Component exists only in worktree, not main codebase — incomplete merge or abandoned branch work

### Patterns Established
- **Use-case flow QA**: Organize QA by user flows (assessment lifecycle, coaching workflow) not by page or role
- **Seed data actions**: Idempotent SeedXxxTestData actions for browser verification — quick setup for manual testing
- **Soft delete with IsActive**: Add bool IsActive to entity, filter in all queries, deactivate instead of delete

### Key Lessons
1. Always create VERIFICATION.md immediately after phase execution — don't defer to later sessions
2. Verification files should be written by re-reading actual code, not from memory of what was built
3. Use-case flow QA is superior to page-by-page QA for catching integration issues
4. 10 phases in 4 days is sustainable but verification quality suffers at speed

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker)
- 4-day milestone (2026-03-02 → 2026-03-05)
- QA phases are faster than build phases — mostly reading + verifying, less code generation

---

## Milestone: v3.6 — Histori Proton

**Shipped:** 2026-03-06
**Phases:** 2 (107-108) | **Plans:** 4

### What Was Built
- CDP "Histori Proton" navbar menu with role-scoped access (Coachee self-redirect, Coach/SrSpv/SH section, HC/Admin all)
- Worker list page with search by nama/NIP, filter by unit/section, step indicator, status badges
- Vertical timeline detail page with left-aligned line, colored circles (green=Lulus, yellow=Dalam Proses), expandable Bootstrap Collapse cards
- HistoriProtonDetailViewModel with ProtonTimelineNode — queries ProtonTrackAssignment + ProtonFinalAssessment + CoachCoacheeMapping

### What Worked
- **Cloning CoachingProton role-scoping**: HistoriProton role access copied CoachingProton's RoleLevel branching — proven pattern, zero auth bugs
- **Small focused milestone**: 2 phases, 4 plans — planned and executed in a single session with no blockers
- **Backend-first waves**: Plan 01 (ViewModel + controller) in wave 1, Plan 02 (view) in wave 2 — clean separation, view could reference compiled model

### What Was Inefficient
- **Plan-index wave mismatch (again)**: Plan 02 frontmatter had wave 2 + depends_on 01, but plan-index tool returned both as wave 1 — same bug as v2.7
- **Summary one-liner extraction still null**: summary-extract returns null for one_liner — format issue persists across milestones

### Patterns Established
- **Timeline CSS inline pattern**: All timeline CSS in a `<style>` block within the Razor view — appropriate for single-page custom styling
- **Left-aligned vertical timeline**: `.timeline` with `::before` pseudo-element for line, `.timeline-node::before` for circles — reusable pattern for any stepped history

### Key Lessons
1. Small milestones (2 phases) execute cleanly in one session — ideal for focused feature additions
2. Role-scoping patterns are now mature enough to clone without modification — CoachingProton is the reference implementation
3. Coach data best sourced from CoachCoacheeMapping (not ProtonTrackAssignment which lacks CoachId)

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker/researcher)
- 1-session milestone (~1.5 hours)
- 4 plans is efficient for a complete new feature (list + detail pages)

---

## Milestone: v3.8 — CoachingProton UI Redesign

**Shipped:** 2026-03-07
**Phases:** 1 (112) | **Plans:** 1

### What Was Built
- Converted 4 Pending badge spans to proper `btn-outline-warning` Tinjau buttons with modal triggers
- Added `fw-bold` + colored border to resolved status badges (Approved/Rejected/Reviewed) via Razor helpers
- Synchronized 6 JS innerHTML locations with new badge styling for AJAX consistency
- Unified Export PDF button to green outline matching Excel export
- Styled Evidence badges: Sudah Upload = bold green+border, Belum Upload = plain gray

### What Worked
- **Single-file scope**: Entire milestone touched only `CoachingProton.cshtml` — no cross-file coordination needed
- **Razor helper leverage**: Updating `GetApprovalBadge` and `GetApprovalBadgeWithTooltip` fixed all server-rendered badges at once
- **CONTEXT.md locked decisions**: discuss-phase captured exact class names and color mappings upfront — zero ambiguity during execution
- **Research line-number mapping**: Researcher identified exact line numbers for all 6 JS innerHTML locations — executor hit them all on first pass

### What Was Inefficient
- Nothing notable — single-plan milestone executed cleanly in one session

### Patterns Established
- **btn vs badge convention**: Interactive elements use `btn btn-outline-*` classes; read-only status indicators use `badge` with `fw-bold border` for resolved states
- **JS innerHTML sync rule**: After changing server-rendered badge/button styling, grep all `innerHTML` assignments in the same view — they MUST match

### Key Lessons
1. Pure CSS/HTML redesigns are ideal single-plan milestones — low risk, high visual impact
2. The discuss-phase "locked decisions" pattern eliminates design ambiguity at execution time
3. JS innerHTML is the #1 risk area for styling drift — always audit after Razor template changes

### Cost Observations
- Model profile: budget (sonnet planner/executor, haiku checker/researcher)
- 1-session milestone (~30 min total)
- Smallest possible milestone: 1 phase, 1 plan, 2 tasks

---

## Milestone: v4.3 — Bug Finder

**Shipped:** 2026-03-13
**Phases:** 3 (168-170) | **Plans:** 8

### What Was Built
- Code audit: 2 dead actions removed, 2 silent catches fixed, 3 unused imports cleaned
- File & database audit: 40+ temp files removed, .gitignore hardened, all 35 DbSets verified active
- Security review: NotificationController CSRF gap closed, 4 XSS patterns fixed, 2 import endpoints secured

### What Worked
- **Audit milestone structure**: 3 orthogonal phases (code/files/security) with zero cross-phase dependencies — all 3 ran in Wave 1 in parallel
- **Pre-scan in planner**: Planner agent pre-scanned codebase during planning, identified specific gaps (e.g., NotificationController CSRF) — executor had precise targets
- **Budget model profile**: All 8 plans executed with sonnet — audit/fix work doesn't need opus-level reasoning

### What Was Inefficient
- **SUMMARY frontmatter still empty**: `requirements_completed` arrays still null in all 8 SUMMARYs — same gap as v4.0
- **Nyquist validation skipped again**: Audit phases don't produce VALIDATION.md — pattern doesn't fit audit work

### Patterns Established
- **Security audit template**: Auth → CSRF → XSS/SQLi → uploads is a clean 4-step security checklist
- **Json.Serialize() for JS contexts**: Replaces unsafe Html.Raw(x.Replace()) pattern — established as canonical approach

### Key Lessons
- Audit milestones complete fast (1 day for 3 phases) because scope is well-defined and findings are binary (gap exists or doesn't)
- File cleanup should happen early in project lifecycle — 40+ screenshots accumulated over 167 phases

### Cost Observations
- Model mix: 100% sonnet (executor + verifier + checker + integration)
- Sessions: 1
- Notable: Entire milestone (plan + execute + verify + audit) completed in a single session

---

## Cross-Milestone Trends

| Milestone | Phases | Plans | Days | Avg plans/day |
|-----------|--------|-------|------|---------------|
| v1.0 | 3 | 10 | 1 | 10 |
| v1.1 | 5 | 13 | 2 | 6.5 |
| v1.2 | 4 | 7 | 1 | 7 |
| v1.3 | 3 | 3 | 1 | 3 |
| v1.4 | 1 | 3 | 1 | 3 |
| v1.5 | 1 | 7 | 1 | 7 |
| v1.6 | 3 | 3 | 1 | 3 |
| v1.7 | 6 | 14 | 2 | 7 |
| v1.8 | 6 | 10 | 2 | 5 |
| v1.9 | 5 | 8 | 2 | 4 |
| v2.0 | 3 | 5 | 1 | 5 |
| v2.1 | 5 | 13 | 2 | 6.5 |
| v2.2 | 1 | 2 | 1 | 2 |
| v2.3 | 8 | 29 | 4 | 7.25 |
| v2.4 | 4 | 9 | 2 | 4.5 |
| v2.5 | 8 | 14 | 2 | 7 |
| v2.6 | 6 | 12 | 1 | 12 |
| v2.7 | 3 | 4 | 1 | 4 |
| v3.0 | 10 | 34 | 4 | 8.5 |
| v3.6 | 2 | 4 | 1 | 4 |
| v3.8 | 1 | 1 | 1 | 1 |
| v3.21 | 1 | 1 | 1 | 1 |
| v4.0 | 6 | 16 | 2 | 8 |
| v4.3 | 3 | 8 | 1 | 8 |

**Running total:** 98 phases, ~230 plans, 28 days
