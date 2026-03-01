# Project Retrospective: Portal HC KPB

*A living document updated after each milestone. Lessons feed forward into future planning.*

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

**Running total:** 66 phases, ~150 plans, 16 days
