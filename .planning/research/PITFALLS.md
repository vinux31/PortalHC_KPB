# Pitfalls Research

**Domain:** ASP.NET Core MVC — UX Consolidation / Refactoring (v1.2)
**Researched:** 2026-02-18
**Confidence:** HIGH — based on direct codebase analysis of Controllers, Views, and Models

---

## Critical Pitfalls

### Pitfall 1: History Disappears When Completed Is Filtered From Worker View

**What goes wrong:**
The Assessment page currently delivers all statuses (Open, Upcoming, Completed) in one server query with client-side tab switching in JavaScript. If Completed is filtered out of the server query without first building a history destination, users lose access to their completed assessments and all links to Results and Certificate pages. The transition creates a window — possibly permanently if the history page is never built — where passing scores and certificates are simply unreachable.

**Why it happens:**
The current `Assessment.cshtml` renders all statuses server-side; the "Completed" tab is pure client-side JavaScript filtering over the full model. Removing Completed from the controller query is one line of code and feels complete immediately. The links to `/CMP/Results` and `/CMP/Certificate` only exist inside the Completed card block in the view. If that block goes away, both entry points vanish in the same commit.

**How to avoid:**
Build or verify the history destination exists first, deploy it, then remove Completed from the main list. The Results and Certificate actions in `CMPController` must remain accessible by direct URL even after the list card is removed — they are already ownership-checked so no security gap, but users need a UI path to reach them. Any "History" page must have a visible link from the Assessment list page before the Completed tab removal ships.

**Warning signs:**
- Users report they cannot find past exam results after the page update
- `Certificate` and `Results` pages return HTTP 200 but there is no navigation path to them from the Assessment list
- Completed count in any dashboard stat card drops to zero with no corresponding history-page link visible

**Phase to address:** Assessment page refactor phase. Must complete in order: (1) confirm/build history destination, (2) update navigation, (3) remove Completed from main query. Do not merge steps 1 and 3 into the same commit.

---

### Pitfall 2: Admin SelectedView Not Checked in New or Modified Actions

**What goes wrong:**
Authorization in `CMPController.Assessment` checks `userRole == "Admin" || userRole == "HC"` to grant manage view. If a history action is added without the SelectedView branch, an Admin simulating Coachee view could see all users' history (missing the personal filter) or an Admin simulating HC view may see a filtered personal view instead of all users. The same risk applies to any modified action during the consolidation.

**Why it happens:**
The codebase has two orthogonal auth layers: `[Authorize(Roles = "...")]` attributes handle coarse access, and runtime `user.SelectedView` checks in the controller body handle view-scoped filtering. Any new or copied action that omits the SelectedView branch silently produces wrong data — the page renders without error, so the bug is only caught by manual role-switching testing.

**How to avoid:**
Treat SelectedView as a mandatory second auth dimension on every new or modified action. The five SelectedView values in production are HC, Atasan, Coach, Coachee, Admin. Test each after any action change. When copying an existing action to a new controller, copy the full SelectedView block, not just the DB query.

**Warning signs:**
- Admin simulating Coachee sees all users' history rather than only their own
- Admin simulating HC cannot see cross-user history after a query is narrowed
- No error is thrown; the page just shows wrong data silently

**Phase to address:** Every phase that adds or modifies controller actions. Add "five-SelectedView test" as an explicit checklist item before marking any action done.

---

### Pitfall 3: Merging TrainingRecord and AssessmentSession Into One Razor Table Breaks Column Rendering

**What goes wrong:**
`TrainingRecord` and `AssessmentSession` have structurally different schemas. `TrainingRecord` has `Judul`, `Kategori`, `Tanggal`, `Penyelenggara`, `Status` (Passed/Valid/Wait Certificate), `ValidUntil`, `CertificateType`. `AssessmentSession` has `Title`, `Category`, `Schedule`, `DurationMinutes`, `Status` (Open/Upcoming/Completed), `Score`, `IsPassed`, `CompletedAt`. When merged into one ViewModel and rendered in a shared table, nullable columns from one source render as blank or throw null reference exceptions in the other. The pass/fail badge logic is entirely different between the two: TrainingRecord uses `Status == "Passed" || Status == "Valid"`, AssessmentSession uses `IsPassed == true`.

**Why it happens:**
The merge feels simple because both represent things a user completed. Developers create a flattened union ViewModel, map the shared-looking fields (Title maps to Judul, Schedule maps to Tanggal), and ship it. The divergence in nullable fields only surfaces in production with real data: a TrainingRecord row tries to display `Score` (which does not exist on it) or an AssessmentSession row tries to display `ValidUntil` (which does not exist on it).

**How to avoid:**
Use a discriminated union ViewModel with a `SourceType` enum field (TrainingRecord vs AssessmentSession). In Razor, switch on `SourceType` to render the correct column template per row. Do not map fields that do not exist — conditionally render or skip. The existing `Records.cshtml` uses a client-side JS tab filter over categories (PROTON, OTS, IHT, etc.); the least-risk migration path is adding an "Assessment Results" tab that renders assessment rows independently, rather than trying to unify all rows into one column layout.

**Warning signs:**
- Null reference exceptions in Razor when rendering merged table
- Score column empty for all TrainingRecord rows; ValidUntil column empty for all AssessmentSession rows
- The certificate expiry alert banner (which reads `TrainingRecord.IsExpiringSoon`) silently breaks if AssessmentSession rows are in the same model list

**Phase to address:** Training Records merge phase. Define the ViewModel contract before writing any Razor. The `IsExpiringSoon` computed property on `TrainingRecord` must remain intact — do not lift it into the merged ViewModel without preserving the calculation logic.

---

### Pitfall 4: Broken Pagination When Two EF Core Sources Are Combined

**What goes wrong:**
The current `Records` action returns `List<TrainingRecord>` from a single server query. AssessmentSessions are paginated server-side in the `Assessment` action with `.Skip().Take()`. If the merged Training Records view combines both sources with server-side pagination, and the EF queries run separately then are concatenated in C#, the pagination breaks: page 1 might show 20 TrainingRecord rows and 0 AssessmentSession rows because in-memory sorting places all AssessmentSessions on later pages. The pagination count (TotalCount in ViewBag) is also wrong if it comes from only one source.

**Why it happens:**
EF Core cannot UNION across two different DbSets into a single ordered paginated SQL query without raw SQL or a shared base table. Developers often load both lists completely, concatenate in memory, sort, then apply `.Skip().Take()` — which defeats pagination performance and loads all rows before discarding most.

**How to avoid:**
For this portal's scale (single company, bounded user counts), full in-memory load then paginate is acceptable if total records per user are bounded. Document that assumption explicitly. The existing JS filter tab pattern — an "Assessment Results" tab alongside existing training category tabs — avoids the pagination problem entirely by keeping the two sources in separate display contexts. If a unified table is required, calculate TotalCount as the sum of both source counts and apply a combined sort before `.Skip().Take()`.

**Warning signs:**
- Combined pagination count in the UI does not match visible row count
- Page 2 is empty when page 1 is not full
- Response time spikes when a user has more than 50 records of either type

**Phase to address:** Training Records merge phase. The decision between unified table and tab extension must be made before writing any ViewModel code. Tab extension is recommended as it eliminates this pitfall class entirely.

---

### Pitfall 5: Cross-Controller Links Break Silently When ReportsIndex Moves

**What goes wrong:**
`CMPController.ReportsIndex` is linked from three locations in the current codebase:

1. `Views/CDP/Dashboard.cshtml` line 97: `asp-controller="CMP" asp-action="ReportsIndex"` — the quick-link "Open Reports" button
2. `Views/CMP/Index.cshtml` line 137: `@Url.Action("ReportsIndex", "CMP")` — the HC Reports hub card
3. `Views/CMP/UserAssessmentHistory.cshtml` lines 11 and 201: breadcrumb and back-button both use `@Url.Action("ReportsIndex")` with no controller argument

If `ReportsIndex` moves to CDPController, links 1 and 2 produce 404 immediately on first navigation. Link 3 breaks silently: `@Url.Action("ReportsIndex")` without a controller argument resolves against the current controller context (CMP), so it generates a URL that points to the old location. No compile-time or startup error occurs.

**Why it happens:**
Tag helpers with explicit `asp-controller` are easy to audit by grep. `@Url.Action` calls without a controller argument are context-dependent and invisible to a simple search for the action name alone. They appear syntactically correct in the editor and generate a URL at render time that is wrong only because the action moved.

**How to avoid:**
Before moving any action: grep for both `asp-action="ReportsIndex"` AND the literal string `"ReportsIndex"` across all `.cshtml` files. When found without an explicit controller argument, add the controller name. Either add a redirect stub in CMPController (`return RedirectToAction("ReportsIndex", "CDP")`) or update all links before deleting the original. `UserAssessmentHistory.cshtml` must be updated to include `"CDP"` (or wherever it moves) as the explicit controller argument in both `Url.Action` calls.

**Warning signs:**
- The CDP Dashboard "Open Reports" button returns 404 after the move
- Breadcrumb in UserAssessmentHistory points to a dead URL
- Admin loses the HC Reports shortcut and finds no alternative navigation path

**Phase to address:** Dashboard consolidation phase (moving HC Reports). Run the link audit before writing the move code, not after.

---

### Pitfall 6: Authorization Drift When ReportsIndex Moves to a Different Controller

**What goes wrong:**
`CMPController.ReportsIndex` carries `[Authorize(Roles = "Admin, HC")]` explicitly. `CDPController` has only class-level `[Authorize]`, which enforces any authenticated user. If the action is copied to CDPController without re-declaring the role attribute, any logged-in user can access the HC Reports page — seeing all completed assessment data, scores, and section-level analytics.

**Why it happens:**
Moving an action between controllers is a copy-paste operation. Developers copy the method body and sometimes the action-level attributes, but miss that the original action's tighter authorization came from its own `[Authorize(Roles)]` attribute, not from an inherited controller-level attribute. The application compiles and runs; the authorization hole is only caught by logging in as a lower-privilege user and visiting the URL directly.

**How to avoid:**
Re-declare `[Authorize(Roles = "Admin, HC")]` explicitly on the moved action. Additionally, verify whether Admin in Coachee SelectedView should be blocked from the reports page — currently `ReportsIndex` does not check `SelectedView`, so this is a design decision. Test access as a Coachee-role user and as an Admin in Coachee view after any move.

**Warning signs:**
- A Coach or Coachee-role user can access the HC Reports URL directly after the move
- No 403 is returned; the page renders with full data
- No automated tests catch this because the project has no automated tests

**Phase to address:** Dashboard consolidation phase. Authorization verification is the first checklist item after any cross-controller move, not the last.

---

### Pitfall 7: Orphaned Links After CompetencyGap Deletion

**What goes wrong:**
`CMP/CompetencyGap` is linked from three locations in the current codebase:

1. `Views/CMP/Index.cshtml` line 72: the "Gap Analysis" hub card button
2. `Views/CMP/CpdpProgress.cshtml` line 19: a "View Gap Analysis" navigation button
3. `Views/CMP/CompetencyGap.cshtml` line 36: a hardcoded JavaScript string `window.location.href='/CMP/CompetencyGap?userId=' + this.value` in an `onchange` handler — this is a string literal, not a tag helper, so it produces no compile-time error when the route is removed

After deletion without updating these links, clicking items 1 and 2 returns 404. Item 3 is inside the deleted view file itself, so it is moot. However, items 1 and 2 remain live in their respective views and will break for every user who visits the CMP hub or the CpdpProgress page.

**Why it happens:**
Tag helper references are found by searching `asp-action`. Hardcoded URL strings in JavaScript and in `Url.Action` calls require searching for the action name as a plain string. Developers run the tag helper audit and declare the link audit complete, missing the JavaScript literals.

**How to avoid:**
Before deleting any controller action: search for the action name as a string literal in all `.cshtml` files, not only as an `asp-action` attribute value. The specific search for this case: grep for `CompetencyGap` across all Views. Update `CMP/Index.cshtml` line 72 and `CMP/CpdpProgress.cshtml` line 19 before or in the same commit as the deletion. If a replacement page exists (e.g., CpdpProgress covers similar ground), add a temporary redirect in CMPController for at least one release.

**Warning signs:**
- The CMP Index hub page loads but the Gap Analysis card button returns 404
- CpdpProgress view has a broken "View Gap Analysis" navigation button
- Users who bookmarked the CompetencyGap URL see an unhandled error page

**Phase to address:** Gap Analysis removal phase. The two hub-page links (CMP Index and CpdpProgress) must be updated in the same commit that removes the action and view.

---

### Pitfall 8: Client-Side JS Tab Filter Breaks When Records View Receives Mixed Model Types

**What goes wrong:**
The existing `Records.cshtml` filters rows by tab via JavaScript: `data-kategori` HTML attributes on rows match tab button values (PROTON, OTS, IHT, etc.). The filter script assumes the model is a flat `List<TrainingRecord>` where every item has a `Kategori` property rendered as the `data-kategori` attribute. If AssessmentSession rows are injected into the same view via a merged ViewModel, those rows have no `Kategori` value (`AssessmentSession` uses `Category`, not `Kategori`). The JS filter either hides all assessment rows always (they never match any tab) or shows them always (if the filter falls back to show-unmatched).

**Why it happens:**
The JS tab filter is data-driven from a Razor attribute rendered server-side. When the model type changes, the attribute-rendering logic must also change, but the JS filter code stays the same. The disconnect is invisible until the merged data is tested against the actual filter UI with real records of both types.

**How to avoid:**
When merging sources, ensure the Razor template emits the correct `data-kategori` (or `data-source-type`) attribute for both row types. For AssessmentSession rows, either map `Category` to one of the existing tab values where it overlaps (e.g., "OJT" maps to the OJT tab) or add a dedicated "Assessments" tab with value "Assessment" and emit that attribute on all AssessmentSession rows. Define this mapping before writing Razor, not during debugging.

**Warning signs:**
- After merge, individual category tabs show 0 rows for assessment items even when assessment records exist
- Assessment rows appear on every tab simultaneously if the `data-kategori` attribute is empty and the filter treats empty as "always show"
- Row count mismatch between the "All" implicit view and tab-filtered views

**Phase to address:** Training Records merge phase. Tab filter logic must be reviewed and updated in the same PR as the ViewModel change, not separately.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Remove Completed from Assessment query before history page exists | Faster to ship the filter | Users lose access to all past results and certificates with no recovery path | Never — history destination must precede source removal |
| Use `@Url.Action("ReportsIndex")` without explicit controller after moving action | Less typing | Silent 404 after controller changes; caught only in manual testing | Never during cross-controller moves — always specify controller |
| In-memory concat + sort + Skip/Take for merged Training Records | Simple code | Loads all records per user into memory before pagination; acceptable at current scale | Acceptable at this portal's scale (single company, bounded users); must be documented |
| Delete CompetencyGap action without updating hub-page links | Fast deletion | Two broken buttons on visited pages; one is a high-traffic hub | Never — link audit must precede deletion |
| Copy ReportsIndex to CDPController without re-declaring `[Authorize(Roles)]` | Fast move | Any authenticated user sees HC-level analytics | Never |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Admin SelectedView + role-based `[Authorize]` attributes | Relying only on `[Authorize(Roles = "Admin, HC")]` without checking `SelectedView` at runtime | Check both: attribute for coarse auth, `user.SelectedView` in action body for view-scoped filtering |
| EF Core multi-source merge for Razor table | Running two separate queries and concatenating in C# with server-side pagination | Concatenate in memory at this scale OR use independent tabs per source; never calculate pagination count from only one source |
| `asp-controller` tag helpers vs `Url.Action` vs hardcoded JS strings | Assuming tag helpers catch all broken links | Grep for the action name as a plain string in all `.cshtml` files before deleting any action |
| Cross-controller `Url.Action` without controller argument | View generates correct URL in current controller context; breaks silently when called from a different controller | Always specify controller name in `Url.Action` calls that cross controller boundaries |
| `[Authorize(Roles)]` inheritance after action moves to new controller | Assume class-level `[Authorize]` provides the same protection | Re-declare all action-level role restrictions explicitly on the moved action |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Full in-memory load of merged TrainingRecord + AssessmentSession before pagination | Slow page load when users have many records | Accept at current scale; add a safety cap (e.g., `.Take(500)` per source) | Noticeable when any user exceeds roughly 500 combined records |
| Loading `AssessmentSessions.Include(a => a.Questions).Include(a => a.Responses)` for a history/list page | Extremely slow list load; massive result sets | History list must NOT include Questions or Responses — only summary fields (Title, Category, Score, IsPassed, CompletedAt) | Breaks immediately for any assessment with a question bank |
| Re-running the full ReportsIndex aggregate queries (CategoryStats, ScoreDistribution, AverageScore) after moving the action | Slow report pagination, no caching added during move | Preserve the existing query structure when moving; do not add eager-loading that was not there before | Already slow at over 100 completed assessments; adding unnecessary Include makes it worse |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Moving ReportsIndex without `[Authorize(Roles = "Admin, HC")]` | Any authenticated user sees all completed assessment data, scores, and section breakdowns | Explicitly re-declare the role attribute on the moved action; verify with a Coachee-role test user |
| Adding a History action that accepts a `userId` parameter without ownership check | Any user views any other user's completed assessment history by guessing IDs | The existing `UserAssessmentHistory` action correctly checks ownership; replicate the same pattern in any new history action |
| Merged Training Records + Assessment view that exposes `AccessToken` in a coachee-facing row | Token visible to the coachee who owns the assessment | `AccessToken` must never appear in coachee-facing views; it is currently guarded behind `viewMode == "manage"` — preserve that guard in any merged view |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Removing Completed from Assessment list with no replacement page | Users who return to check their score find nothing and conclude their data was lost | Add a visible "History" link before removing the Completed tab; verify the link is prominent, not buried |
| Merging Training Records and Assessments with identical column headers but different field semantics | "Status" means Passed/Valid/Wait Certificate for TrainingRecords but Open/Upcoming/Completed for AssessmentSessions; users see confusing mixed values in the same column | Render separate status badge styling per row type, or use different column headers per tab; never display both status vocabularies under the same label |
| Moving HC Reports into CDP Dashboard without updating the CMP Index hub card | Users who navigate via CMP hub find the Reports card broken; users who navigate via CDP Dashboard find it; two user populations have completely different outcomes | Update the CMP Index hub card link in the same release as the move, or add a redirect from the old route |
| Deleting Gap Analysis page without a redirect or in-page message | Users who bookmarked the URL or follow an old email link see an error page with no context or alternative | Add a temporary `RedirectToAction("CpdpProgress", "CMP")` stub for at least one release cycle before full removal |

---

## "Looks Done But Isn't" Checklist

- [ ] **Assessment filter (remove Completed):** Verify the Results and Certificate pages are still reachable from at least one UI location after removing the Completed card block. Check the back-link from Results still points to the Assessment list.
- [ ] **Admin SelectedView in every changed action:** Manually test all five SelectedView values (HC, Atasan, Coach, Coachee, Admin) for every action that was added or modified. Do this before marking the phase done.
- [ ] **Training Records merge:** Verify the certificate expiry alert banner (`IsExpiringSoon`) still renders correctly — it reads from `TrainingRecord` properties that must survive in the merged ViewModel.
- [ ] **ReportsIndex move:** Grep for `Url.Action("ReportsIndex"` without a controller argument before and after the move. `UserAssessmentHistory.cshtml` has two such calls; both must be updated.
- [ ] **CompetencyGap deletion:** Search for the literal string `CompetencyGap` in all `.cshtml` files. `CpdpProgress.cshtml` line 19 and `CMP/Index.cshtml` line 72 must be updated in the same commit as the deletion.
- [ ] **Authorization after cross-controller move:** Access the new Reports URL as a Coachee-role user; expect HTTP 403. Then access as Admin in Coachee SelectedView; verify intended behavior.
- [ ] **JS tab filter after merge:** Click every tab in the merged Training Records view with a mixed dataset; verify AssessmentSession rows appear in the correct tab or the dedicated "Assessment" tab.
- [ ] **Pagination count after merge:** If server-side pagination is used on merged data, verify the displayed page count matches the actual total of combined rows from both sources.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Completed sessions disappear with no history page | MEDIUM | Revert the one-line filter removal; deploy; build history page; re-attempt removal after history page ships |
| ReportsIndex 404 after cross-controller move | LOW | Add redirect stub `return RedirectToAction("ReportsIndex", "CDP")` in CMPController; deploy immediately |
| CompetencyGap 404 from orphaned hub-page links | LOW | Add redirect stub in CMPController pointing to CpdpProgress; update two view files in the same hotfix |
| Auth hole on moved ReportsIndex | LOW | Add `[Authorize(Roles = "Admin, HC")]` to the action in CDPController; redeploy |
| Merged table null reference exception in production | HIGH | Roll back to separate views; redesign merged ViewModel with discriminated union before re-attempting |
| JS tab filter hides all assessment rows in merged view | MEDIUM | Add `data-kategori="Assessment"` attribute emission for AssessmentSession rows and a matching "Assessment" tab button; or roll back to separate views |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| History disappears when Completed filtered out | Assessment page refactor phase | Navigate to Results and Certificate from every remaining UI entry point after removing the Completed card block |
| Admin SelectedView ignored in new or modified actions | Any phase that touches controller actions | Five-SelectedView manual test for every changed action before the phase is marked complete |
| Merged table column mismatch (TrainingRecord vs AssessmentSession) | Training Records merge phase | Render merged table with real data from both sources; inspect every column for null or semantically incorrect values |
| Broken pagination from two sources | Training Records merge phase | Load a user with 20+ TrainingRecords and 10+ AssessmentSessions; verify page 1 and page 2 counts are correct |
| Cross-controller links break when ReportsIndex moves | Dashboard consolidation phase | Click every link to ReportsIndex from CMP Index, CDP Dashboard, and UserAssessmentHistory before and after the move |
| Authorization drift on moved action | Dashboard consolidation phase | Access the new Reports URL as a Coachee-role user; expect 403 |
| Orphaned links after CompetencyGap deletion | Gap Analysis removal phase | Load CMP Index hub and CpdpProgress view; click all navigation elements that previously referenced the deleted page |
| JS tab filter breaks with mixed model types | Training Records merge phase | Click every tab in the merged view with mixed data; verify row counts match expected values per tab |

---

## Sources

- Direct codebase analysis: `Controllers/CMPController.cs`, `Controllers/CDPController.cs`
- Cross-link audit: `Views/Shared/_Layout.cshtml`, `Views/CMP/Index.cshtml`, `Views/CDP/Dashboard.cshtml`, `Views/CMP/UserAssessmentHistory.cshtml`, `Views/CMP/CpdpProgress.cshtml`, `Views/CMP/CompetencyGap.cshtml`
- Model schema: `Models/TrainingRecord.cs`, `Models/AssessmentSession.cs`, `Models/ApplicationUser.cs`
- Authorization pattern: `[Authorize(Roles = "Admin, HC")]` on `CMPController.ReportsIndex`, `CMPController.EditAssessment`, `CMPController.DeleteAssessment`; runtime SelectedView checks in `CDPController.Dashboard`, `CDPController.Coaching`, `CMPController.Records`
- Client-side filter pattern: `Views/CMP/Records.cshtml` (JS category tab filter), `Views/CMP/Assessment.cshtml` (JS status tab filter)

---
*Pitfalls research for: Portal HC KPB — v1.2 UX Consolidation (ASP.NET Core MVC refactoring)*
*Researched: 2026-02-18*
