# Homepage Redesign Pitfalls

**Domain:** ASP.NET Core MVC Portal — Homepage Minimalist Redesign
**Researched:** 2026-03-10
**Confidence:** HIGH (analyzed actual codebase: HomeController.cs, Views/Home/Index.cshtml, DashboardHomeViewModel.cs, home.css)

---

## Critical Pitfalls

### Pitfall 1: Deleting CSS Classes Still Referenced by Other Views

**What goes wrong:**
You remove `.glass-card`, `.hero-section`, `.timeline`, or `.deadline-card` from `home.css` and delete the file reference from Views/Home/Index.cshtml. But another view or partial (AdminController pages, CMPController pages, CDPController pages) also uses the same CSS class name. When that view loads, styling fails silently—no error in console, just missing shadows, wrong padding, broken borders. The bug only manifests when navigating away from homepage.

**Why it happens:**
- CSS class names lack namespacing: `.glass-card` is generic, not `.home-glass-card`
- `home.css` is scoped only to Home/Index.cshtml view (single `<link>` tag), hiding cross-view dependencies
- Developer assumes "only homepage uses home.css" without grep-checking the entire codebase
- Modern JS frameworks (Vue/React) catch this at compile time; Razor+CSS is loosely coupled, allowing silent failures
- No automated tool warns: "CSS class defined here but never used" (except coverage analysis tools, rarely run in MVC projects)

**How to avoid:**

1. **Before ANY CSS deletion:** Run comprehensive search:
   ```bash
   grep -r "glass-card\|hero-section\|timeline\|deadline-card\|circular-progress\|card-icon-wrapper\|quick-access" /path/to/project --include="*.cshtml" --include="*.js" --include="*.html"
   ```
   If results exist outside `Views/Home/`, the class is shared — do NOT delete from home.css.

2. **Safe removal strategy:**
   - **Option A (Recommended for MVP):** Keep unused CSS in `home.css` (minified, only ~5KB cost). Document in code comment: `/* [REMOVED FROM MARKUP IN v3.18] .glass-card kept for potential API compatibility or future feature use */`
   - **Option B (Cleaner):** Prefix classes with `.home-`: `.home-glass-card`, `.home-hero-section`. Update ALL Razor bindings simultaneously. Run full regression test.
   - **Option C (Long-term):** Extract shared CSS classes to `site.css` for site-wide reuse. Consolidate `.home-` prefix classes into site.css without prefix. Delete `home.css` entirely.

3. **Prevent regression:**
   - Add to Phase 1 checklist: "CSS Cross-Reference Audit" — run grep for each CSS class before deletion, document findings in VERIFICATION.md
   - Keep deleted CSS classes in git history with commit message: "refactor: remove glass-card from markup (kept in CSS for backward compatibility)"
   - Add code comment above each class definition: `/* Used by Views/Home/Index.cshtml lines X-Y. Do NOT delete without checking dependent views. */`

**Warning signs:**

- View loads but cards have no shadow, wrong padding, missing border-radius (CSS missing silently)
- Console has no errors or 404s (CSS failures don't throw)
- Bug appears only on second page navigation, not on homepage (isolation prevents detection)
- Styles break on mobile (375px) but work on desktop (cascade order issue from separate style removal)
- Code review comment: "Why did the Admin cards lose their hover effect?" (unrelated page affected)

**Phase to address:**
**Phase 1 (Setup/Verification):** Before ANY deletion, add "CSS Cross-Reference" checklist. Run grep search for each class about to be removed. Save results in VERIFICATION.md. Mark as BLOCKED if classes found outside Home/.

---

### Pitfall 2: Removing ViewModel Properties Without Auditing Controller Calculation Logic

**What goes wrong:**
You decide the new homepage design doesn't need `IdpProgressPercentage` or `HasUrgentAssessments`, so you delete these properties from `DashboardHomeViewModel`. But in `HomeController.Index()`, the controller STILL CALCULATES these values (lines 67–75 for IdpProgressPercentage, lines 72–75 for HasUrgentAssessments).

On page load: `InvalidOperationException: The property 'IdpProgressPercentage' does not exist on type 'DashboardHomeViewModel'.`

OR, worse: You miss a calculation entirely because the property was auto-bound by the Razor view. You delete the property and controller code, thinking it's dead—but a hidden API endpoint (e.g., `api/home/summary` that serializes the model to JSON) now returns incomplete data. A mobile app depending on that API breaks.

**Why it happens:**
- ViewModel properties are invisible in Razor markup unless explicitly referenced with `@Model.[property]`
- Model-to-View binding is loose; property can exist but never render
- No automated refactoring tool warns "property defined but never used in markup"
- Brownfield projects accumulate cruft; hard to distinguish "unused vestigial code" from "supporting hidden API"
- Copy-paste error: Developer deletes property from ViewModel but forgets to remove the controller calculation (or vice versa)

**How to avoid:**

1. **Map EVERY ViewModel property to its usage before deletion:**
   ```bash
   # Extract all @Model.* references in the view
   grep -o "@Model\.[A-Za-z_]*" Views/Home/Index.cshtml | sort | uniq > /tmp/view_usage.txt

   # Extract all ViewModel properties
   grep "public.*{ get; set; }" Models/DashboardHomeViewModel.cs | sort > /tmp/vm_properties.txt

   # Compare: properties NOT in view_usage.txt are candidates for removal
   comm -23 /tmp/vm_properties.txt /tmp/view_usage.txt
   ```

2. **For each property NOT in View:**
   - Is it calculated in the controller? (lines 42–75 in HomeController.Index)
   - Is it serialized by a JSON endpoint? (Search: `Json(viewModel)`, `return viewModel;` in API actions)
   - Is it used in a background job, export, or test?
   - If answer to all three is NO, then safe to delete both property AND calculation

3. **Safe deletion workflow:**
   - Step 1: Comment out View markup for that property (if it exists) with timestamp: `<!-- [REMOVED v3.18 — IDP card removed] @Model.IdpProgressPercentage -->`
   - Step 2: Comment out ViewModel property: `// [REMOVED v3.18 — see HomeController line 67 for calculation deletion] public int IdpProgressPercentage { get; set; }`
   - Step 3: Comment out controller calculation: `// [REMOVED v3.18 — IDP card no longer displayed] viewModel.IdpProgressPercentage = ...`
   - Step 4: Run full UAT (all pages, all roles)
   - Step 5: If no regression after 1 sprint, delete the comments and commit with message: "refactor: remove unused IDP progress card"

**Warning signs:**

- Compilation error after deleting ViewModel property: `'DashboardHomeViewModel' does not contain a definition for 'X'`
- NullReferenceException on homepage load
- API response missing expected property
- Razor compile warning: `CS8601: Possible null reference assignment`
- Code review: "Why is this property calculated but never used in the View?"

**Phase to address:**
**Phase 1 (Cleanup):** Create ViewModel property audit. For EACH of 8 properties, mark as [KEEP / OBSOLETE / REMOVE] with explicit reason. Only remove properties with BOTH markup removed AND controller calculation removed in same commit.

---

### Pitfall 3: Orphaned Data-Fetching Logic Causing Silent Performance Regression

**What goes wrong:**
You decide to remove the "Recent Activity" timeline section and "Upcoming Deadlines" section from the View (lines 204–299 of Index.cshtml). You delete the HTML markup. But in `HomeController.Index()`, you leave these controller methods intact:

```csharp
RecentActivities = await GetRecentActivities(targetUserIds),      // Still executes
UpcomingDeadlines = await GetUpcomingDeadlines(targetUserIds)     // Still executes
```

Each homepage load now wastes:
- 4 additional database queries (2 AssessmentSessions queries, 1 IdpItems query, 1 CoachingLogs query, 1 TrainingRecords query)
- Async delay: 100–500ms per query
- Memory allocation for large lists that never render

With 5,000 concurrent users visiting homepage, this becomes 5,000 × 4 = 20,000 wasted queries/second hitting the database. Performance regression is invisible in single-user testing but catastrophic at scale.

**Why it happens:**
- Developers assume "if View markup is deleted, the code doesn't execute"
- EF Core queries execute eagerly; no lazy-load warning for unbound results
- No profiler automatically detects "query result never serialized or bound"
- Performance regression invisible in UAT (1–10 users); only visible under load testing
- Database metrics not monitored during phase; regression caught weeks later when other features slow down

**How to avoid:**

1. **Never delete View markup without deleting controller logic first:**
   - Step 1: Comment out controller data-fetching: `// RecentActivities = await GetRecentActivities(targetUserIds),`
   - Step 2: Run performance baseline test (measure homepage load time)
   - Step 3: If load time improves significantly (>50ms), deletion was justified
   - Step 4: THEN delete View markup
   - Step 5: Document performance delta in commit message

2. **If data-fetching must stay (API compatibility, future feature):**
   ```csharp
   // [KEPT FOR API v3.18.1] Used by /api/home/summary endpoint (not displayed on homepage)
   // Performance note: This query executes even if not rendered. Safe because used elsewhere.
   var recentActivities = await GetRecentActivities(targetUserIds);
   viewModel.RecentActivities = recentActivities;
   ```

3. **Performance testing checklist:**
   - Measure baseline: `stopwatch.Start()` before Index() call, `stopwatch.Stop()` after View render
   - Compare: before deletion vs. after deletion
   - Document: "Removed GetRecentActivities: 245ms → 178ms (29% faster)"
   - Commit message includes performance metric

**Warning signs:**

- Homepage load time increases or stays same despite "removing sections"
- Database slow query log shows unexpected queries on homepage
- Slow database server gets slower after deployment
- Performance monitoring shows query count increase with no feature increase
- New developer asks: "Why is GetUpcomingDeadlines still being called if that section was removed?"

**Phase to address:**
**Phase 2 (View Simplification):** Before removing any View section, remove its associated data-fetching. Measure baseline vs. optimized load time. If time decreases >50ms, document in VERIFICATION.md. If time increases, investigate why (may indicate a new slow query).

---

### Pitfall 4: Breaking Global AOS (Animate On Scroll) Dependencies

**What goes wrong:**
Current homepage uses `data-aos="fade-down"`, `data-aos="fade-up"`, `data-aos="zoom-in"` on 15+ elements. The redesign removes all animation attributes from HTML. You think: "We're also removing AOS from this page, so let's remove the AOS script from `_Layout.cshtml`."

But if another view (CDP/Index, CMP/Index, AdminController pages) also uses `data-aos` attributes, removing the AOS script breaks animations site-wide. Even if no other view uses it NOW, the 10KB minified library is already loaded globally; removing it saves nothing and prevents future features from using smooth scroll animations.

**Why it happens:**
- Global script includes in `_Layout.cshtml` are invisible; developer doesn't realize scope
- No error is thrown when `data-aos` attribute exists but AOS JavaScript is missing (silently ignored)
- Animation removal feels like "cleanup" when it's actually "removing a site-wide library"
- Developer assumes homepage is the only user of AOS

**How to avoid:**

1. **Audit AOS usage across ENTIRE project before removal:**
   ```bash
   grep -r "data-aos\|AOS\|aos.init()" /path/to/project --include="*.cshtml" --include="*.js" --include="*.html"
   ```
   If results exist ONLY in Views/Home/Index.cshtml, safe to remove. If found elsewhere, AOS script MUST stay in _Layout.cshtml.

2. **Safe removal paths:**
   - **Path A (Recommended):** Keep AOS script in _Layout.cshtml. Remove only `data-aos` attributes from homepage HTML markup. Cost: 10KB library, benefit: can re-enable animations later, other pages unaffected.
   - **Path B (Cleaner, more work):** Verify no other views use AOS. Move `<script src="~/lib/aos/aos.js"></script>` and `<link href="https://unpkg.com/aos@2.3.1/dist/aos.css" rel="stylesheet">` from _Layout.cshtml to Views/Home/Index.cshtml. Add explicit AOS init script at bottom of Index.cshtml. Verify no errors.
   - **Path C (Safest):** Keep AOS CSS in _Layout, keep AOS script, just remove `data-aos` attributes. Library loading costs nothing extra; removes risk.

3. **Testing checklist:**
   - Run grep for all AOS references
   - Count results; if >10 (outside Home/), AOS is site-wide
   - Check git blame for AOS script addition; was it intentional site-wide or homepage-only?
   - Test all pages that might use animations to verify no regression

**Warning signs:**

- HTML elements have `data-aos="fade-in"` attributes that don't animate (script missing)
- Console shows no errors (silent failure)
- Another page (CMP/Index, CDP/Index) that should animate doesn't (AOS script removed accidentally)
- Library still exists in wwwroot/lib/aos/ but `<script>` tag was deleted
- Regression after "homepage redesign" on an unrelated page

**Phase to address:**
**Phase 1 (Audit):** Search project for all AOS references. Create list of views using animations. Document decision: "Keep AOS library site-wide" or "Homepage-only (move to View)" or "Remove entirely". Execute safely with full regression test.

---

### Pitfall 5: ViewModel Properties Accumulate as "Vestigial" After Section Removal

**What goes wrong:**
Current `DashboardHomeViewModel` has 8 properties:
- `CurrentUser`, `Greeting` — used in hero section (KEEP)
- `IdpTotalCount`, `IdpCompletedCount`, `IdpProgressPercentage` — used in IDP card (REMOVE)
- `PendingAssessmentCount`, `HasUrgentAssessments` — used in Assessment card (REMOVE)
- `MandatoryTrainingStatus` — used in Mandatory Training card (REMOVE)
- `RecentActivities` — used in timeline (REMOVE)
- `UpcomingDeadlines` — used in deadline section (REMOVE)

After redesign, 6 of 8 properties are orphaned. But the class definition stays unchanged. Two months later, a new developer reads the code and wonders: "Why are these unused properties here? Let me delete them." They delete `IdpProgressPercentage`. Unknown to them, an admin reporting API (`/api/admin/home-metrics`) still serializes the ViewModel to JSON. The API response now has a missing field—mobile app breaks.

**Why it happens:**
- ViewModel definitions lack inline documentation about which properties are "in use" vs. "vestigial"
- No automated tool detects "property defined but never serialized"
- Brownfield projects don't enforce "ViewModel must match View bindings exactly"
- Model reuse across multiple actions (e.g., `Index()` for View, implicit use in JSON serialization) hides dependencies
- Future developers inherit code without context

**How to avoid:**

1. **Document property usage inline with XML comments:**
   ```csharp
   public class DashboardHomeViewModel
   {
       /// <summary>
       /// [USED IN v3.18] Displayed in hero greeting section (line 20 of View).
       /// Used by: Home/Index.cshtml
       /// Safe to remove: NO (core UI element)
       /// </summary>
       public ApplicationUser CurrentUser { get; set; } = null!;

       /// <summary>
       /// [REMOVED IN v3.18] Was used in IDP Status card (removed from View).
       /// Used by: (none in View; potentially used by /api/home/summary endpoint)
       /// Safe to remove: Check if any API endpoints serialize this property before removal
       /// Marked: [Obsolete] — will be removed in v3.19 after API compatibility verified
       /// </summary>
       [Obsolete("Removed from homepage view in v3.18; verify API compatibility before deleting")]
       public int IdpTotalCount { get; set; }
   }
   ```

2. **Create property lifecycle document (added to VERIFICATION.md):**
   ```markdown
   ## v3.18 ViewModel Property Audit

   ### Removed from View (Safe to Delete After Verification)
   - IdpTotalCount (line 42 controller) → IDP card removed from homepage
   - IdpCompletedCount (line 45) → IDP card removed
   - IdpProgressPercentage (line 67) → IDP card removed

   ### Verification Checklist
   - [ ] No API endpoint returns this ViewModel
   - [ ] No background job or export reads these properties
   - [ ] No unit tests reference these properties
   - [ ] Code review confirms deletion is safe
   - [ ] All properties deleted in SINGLE commit, not scattered

   ### Timeline
   - v3.18: Mark `[Obsolete]`, leave code
   - v3.18.1 (1 sprint later): Final verification
   - v3.19: Delete `[Obsolete]` properties
   ```

3. **Enforce ViewModel hygiene:**
   - Code review rule: "Every ViewModel property must have a one-line inline comment explaining its use"
   - Never leave commented-out properties; delete or mark `[Obsolete]`
   - Annual audit: ViewModel properties vs. View bindings must match

**Warning signs:**

- ViewModel has 10+ properties, View uses 3 (code smell)
- Properties exist with names like "RecentActivities", "UpcomingDeadlines" but View is empty
- Git blame shows property added 6 months ago, never modified, with no comments
- Code review: "Why is this property here? Nobody uses it."
- API endpoint returns null/empty for property that was removed from View

**Phase to address:**
**Phase 1 (Cleanup):** For EACH removed View section, mark associated ViewModel properties as `[Obsolete]` with reason and safe-to-delete timeline. Review in Phase 3. Document removal justification in git commit message.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Leave commented-out controller method (GetRecentActivities) instead of deleting | "Safer — can recover quickly if needed" | Code readability suffers; future developers don't know if it's intentional or dead code | 1 sprint MAXIMUM. After UAT confirmation, delete. Never leave commented for >2 sprints. Document reason in git commit. |
| Keep unused ViewModel properties without `[Obsolete]` annotation | "Faster — less refactoring" | Next developer deletes them without checking dependencies, breaking hidden API | NEVER acceptable. Always annotate with reason + safe-to-delete timeline. Review quarterly. |
| Don't update CSS imports in Index.cshtml when moving styles to site.css | "Avoid touching other files" | Two CSS sources for same class; cascade confusion; maintenance nightmare; duplicate rules | NEVER. Always delete old `<link>` tag after moving styles. One canonical CSS file per feature. |
| Keep AOS script in _Layout even if homepage removes all animations | "Insurance — might need it later" | 10KB unused library on every page load; slower site-wide; bloats network transfer | Acceptable ONLY if another view uses `data-aos`. Otherwise, remove completely. Test with grep first. |
| Delete HTML markup first, leave controller data-fetching running | "Homepage looks cleaner faster; data deletion takes more work" | Wasted DB queries; silent performance regression; invisible until load testing | NEVER. Always delete data-fetching FIRST, measure performance delta (must show >50ms improvement), THEN delete markup. |

---

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| CSS class reuse across views | Delete `.glass-card` class from home.css assuming only homepage uses it; Admin cards lose styling | grep for ALL occurrences of CSS class name across *.cshtml files before ANY deletion |
| ViewModel property deletion | Delete property from class definition without checking if API endpoint serializes it | Document property usage (View + API + background job); verify ALL usages removed before deletion |
| AOS animation library | Remove `<script src="aos.js"></script>` from _Layout.cshtml because "homepage doesn't animate" | Search entire project for `data-aos` first; if found elsewhere, AOS script MUST stay |
| Bootstrap version assumptions | Assume all pages on Bootstrap 5 when removing responsive utility classes | Check _Layout.cshtml for CDN version; verify all legacy pages also on Bootstrap 5+ before deleting compatibility code |
| Entity Framework lazy loading | Delete `GetRecentActivities()` method thinking it's unused, unaware `/api/home/summary` endpoint still calls it | Search codebase for method name; check all ApiController usages; test JSON response before deletion |
| Scoped CSS (Razor CSS isolation) | Assume style changes in Views/Home/Index.cshtml.css don't affect other views | home.css is NOT scoped CSS (no .cshtml.css file); it's global. Use grep to verify isolated usage. |

---

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|-----------|-----------------|
| Leaving GetRecentActivities/GetUpcomingDeadlines queries executing after removing View sections | Homepage load time stays same or increases despite "removing content"; database query log shows 4 extra queries per request | Delete controller data-fetching BEFORE View removal; measure load time baseline vs. optimized; verify >50ms improvement (or investigate why decrease didn't occur) | 1,000+ concurrent users: 5,000 wasted queries/second kills database. At 5,000 users: 25,000 queries/second overwhelms connection pool. |
| Keeping circular-progress SVG calculation with gradient definitions in ViewModel even if View is removed | SVG rendering cost on server-side (50–100ms per request for gradient calculation) even though user never sees it | Remove calculation from HomeController lines 55–72 entirely; verify ViewModel no longer contains circumference/offset logic | Mobile rendering with limited CPU or constrained server resources; shows up in performance monitoring as unexpected homepage latency spike |
| Not removing `data-aos` attributes from removed sections (even if AOS script is removed) | Browser still observes deleted DOM elements for intersection; memory leak if hundreds of removed elements still have observers | Remove ALL `data-aos` attributes alongside AOS script removal; verify grep finds zero AOS in View after deletion | Mobile browsers (iOS Safari) with limited RAM show memory pressure or jank after removing sections that still have observers attached |
| Keeping old CSS breakpoints responsive rules for sections that no longer exist | Mobile viewport loads CSS rules for `.timeline` media queries that will never apply | Delete responsive `@media` rules for removed sections; consolidate to site.css only for kept sections | >100 removed CSS rules across mobile breakpoints; stylesheet parsing time increases; less critical but measurable on old devices |

---

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|-----------|
| Deleting null-safety check in hero section (currently shows `@Model.CurrentUser.FullName` with fallback) without adding it to simplified hero | If simplified hero directly accesses `Model.CurrentUser.FullName`, and currentUser is null, NullReferenceException crashes page | Always add null-safe operator: `@Model.CurrentUser?.FullName ?? "Staff"`. Never remove `[Authorize]` check from HomeController.Index(). Test with anonymous access attempt. |
| Removing role-scoped data filter from controller when simplifying dashboard | If data-fetching methods (GetRecentActivities, GetUpcomingDeadlines) filtered by `targetUserIds`, and you delete that parameter, authorization bypass allows viewing other workers' personal data | Keep role-scoped filtering in controller. Test with two users (Worker + Manager role) to verify one cannot see the other's IDP/Assessment/Coaching data. |
| Deleting TrainingStatusInfo.CertificateUrl validation if Mandatory Training card is removed | Certificate download URL validation removed; future card redesign might reference CertificateUrl without checking if it's a valid local path (not attacker-controlled URL) | Document security-critical properties (CertificateUrl, ValidUntil, IsValid) with comments. Verify URL is valid before deletion. Don't delete validation logic. |
| Removing user-visible error messages if form submission logic is deleted | If Quick Access links are removed and their target actions are deleted, users navigating via bookmark get 404 instead of helpful "This page has moved" message | Keep Action methods as redirects: `return RedirectToAction("Index", "CMP");` instead of deleting them entirely. Allows graceful migration path. |

---

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Removing "Upcoming Deadlines" section entirely without alternate deadline visibility | Users no longer see assessment deadlines on homepage; must navigate to CMP/Assessment page to find when assessments close. Urgent deadlines invisible until they log in elsewhere. | Keep deadline visibility in simplified form: add single-line badge under Quick Access (e.g., "3 assessments due this week") or move critical deadlines to navbar alert. Don't remove entirely. |
| Removing "Recent Activity" timeline without providing alternate activity history | Users can't quickly see what they just completed (assessment score, IDP update, coaching session). They have to navigate to each module to find last action. | Provide 2–3 recent activity lines in Quick Access section or navbar, or add "Your Recent Activity" link to Settings/Profile page. Don't delete history entirely. |
| Simplifying IDP card but removing progress percentage | Users can't tell if they're at 10% or 90% completion on development plan | Show one-line summary under Quick Access: "IDP: 2/5 completed" or "IDP Progress: 40%". Don't hide the metric entirely. |
| Removing cards without testing all user roles | Redesign assumes all users care about same metrics; but HC cares about team IDP completion, Supervisor cares about pending assessments, Workers care about their own progress | Test redesigned homepage with ALL roles (Worker, Supervisor, SrSpv, SectionHead, HC, Admin) before shipping. Verify each role sees relevant metrics. Some sections may need role-specific visibility. |
| Breaking navigation flow by removing entry points to critical pages | Users previously clicked "Update Progress" on IDP card to navigate to CDP/Index; now that card is removed, they don't know where to find IDP | Ensure Quick Access cards still exist and link to CMP/Assessment and CDP/Index. Don't remove ALL entry points to features. |

---

## "Looks Done But Isn't" Checklist

- [ ] **CSS Cross-Reference Audit:** Ran `grep -r "glass-card\|hero-section\|timeline\|deadline-card\|circular-progress\|card-icon-wrapper\|quick-access"` across entire project. Saved output to VERIFICATION.md. Confirmed ZERO results outside Views/Home/Index.cshtml (or documented if shared with other views).

- [ ] **ViewModel Property Inventory:** Created list of all 8 properties in DashboardHomeViewModel. For EACH property, documented: [KEEP / OBSOLETE / DELETE] with reason + safe-to-delete timeline. Only deleted properties marked DELETE (never KEEP or OBSOLETE).

- [ ] **Controller Data-Fetching Removal:** For EACH View section removed, verified corresponding controller method was also deleted or marked with comment explaining why it's kept. Did NOT leave orphaned `viewModel.RecentActivities = await GetRecentActivities(...);` calls.

- [ ] **Performance Testing:** Measured homepage load time BEFORE redesign. Deleted controller data-fetching. Re-measured load time AFTER. Documented delta in VERIFICATION.md. If no improvement >50ms, investigated why (may indicate unrelated slow query).

- [ ] **Global Dependencies Audit:** Searched project for `data-aos`, `AOS`, `aos.init()`, `glass-card`, `hero-section` references. Confirmed they appear ONLY in Home/Index.cshtml or explicitly documented if shared.

- [ ] **CSS Import Statements:** Verified old `<link href="~/css/home.css" />` is NOT redundant (either moved styles to site.css and deleted link, or kept link for homepage-only styles). No duplicate CSS imports.

- [ ] **Responsive Testing:** Tested redesigned homepage on mobile (375px width), tablet (768px), and desktop (1920px). Verified no layout collapse from removed sections. Quick Access cards still visible and clickable on mobile.

- [ ] **All Roles Testing:** Tested homepage with Worker, Supervisor, SrSpv, SectionHead, HC, and Admin roles. Verified each role still sees relevant information (e.g., HC still sees team metrics, Worker still sees personal IDP/Assessment status).

- [ ] **Navigation Flow:** Verified Quick Access cards still exist and link to CMP/Assessment and CDP/Index. Tested clicking all links from homepage. Confirmed no 404 errors.

- [ ] **API Compatibility:** Verified no ApiController action returns DashboardHomeViewModel (unlikely in MVC, but checked). If Home/Index can return JSON response, verified ViewModel structure change doesn't break any clients.

- [ ] **UAT Coverage:** Completed full UAT with all 3 testing flows: (1) Home → Quick Access → Assessment, (2) Home → Quick Access → IDP, (3) Home → Hero (Profile) → Settings. All flows verified working. No broken links.

- [ ] **Code Comments:** Added inline comments to ViewModel class documenting property usage. Marked `[Obsolete]` properties with reason + safe-to-delete timeline. Added `/* [REMOVED FROM MARKUP IN v3.18] */` comments above deleted CSS rules (if kept in file).

- [ ] **Git Commit Quality:** Each removal committed separately with message explaining what was removed and why. Example: "refactor: remove GetRecentActivities from controller (homepage load time improved 245ms → 178ms)". Never bundled unrelated deletions.

---

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Deleted CSS class still used by another view (visual regression on AdminController page) | LOW | 1. Restore from git: `git checkout HEAD~1 -- wwwroot/css/home.css`. 2. Re-run grep to identify which views use this class. 3. Move class to site.css with comment explaining shared use. 4. Delete from home.css. 5. Commit: "refactor: move glass-card to site.css for shared use". 6. Re-test affected pages. |
| Removed ViewModel property but controller logic still references it (NullReferenceException at runtime) | MEDIUM | 1. Restore property from git: `git checkout HEAD~1 -- Models/DashboardHomeViewModel.cs`. 2. Commit: "revert: restore [property] for controller compatibility". 3. In next sprint, mark with `[Obsolete]` and plan removal in separate commit with full verification. |
| Deleted controller data-fetching method but API endpoint still calls it (API returns 500 error) | MEDIUM | 1. Restore method from git. 2. Add code comment: `// Used by /api/admin/home-metrics endpoint, don't remove without coordinating with API consumers`. 3. Create tracking issue: "Refactor [method] usage after API migration". 4. Keep for 1 sprint, coordinate removal with API team. 5. Commit: "revert: restore [method] for API compatibility". |
| Removed AOS script globally but another page has `data-aos` attributes (animations don't play) | HIGH | 1. Restore AOS script in _Layout.cshtml: `<script src="~/lib/aos/aos.js"></script>`. 2. Restore AOS CSS: `<link href="https://unpkg.com/aos@2.3.1/dist/aos.css">`. 3. Re-run grep to find all `data-aos` attributes. 4. If multiple pages use AOS, keep it site-wide. If only homepage, move script to View instead of _Layout. 5. Test all pages with animations. 6. Commit: "revert: restore AOS library (used by multiple views)". |
| Removed "Upcoming Deadlines" section; users can't see assessment schedules (high UX impact) | HIGH | 1. Re-add View section (lines 242–298). 2. Re-enable controller method: `UpcomingDeadlines = await GetUpcomingDeadlines(targetUserIds),`. 3. Instead of full timeline, simplify to show only 2–3 urgent deadlines (next 3 days). 4. Conduct quick UAT with 2 workers. 5. Commit: "revert: restore deadline visibility in simplified form". 6. Measure performance impact and document in VERIFICATION.md. |
| Deleted properties marked with `[Obsolete]` but API endpoint returns null for them (client library breaks) | HIGH | 1. Restore property to ViewModel. 2. Search codebase for property name to find all API usages. 3. Coordinate with API clients (mobile app, 3rd-party consumers) for migration plan. 4. Keep property for 1–2 sprints while clients migrate. 5. Commit: "revert: restore [property], coordinating with API consumers for removal". |

---

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification Checklist |
|---------|------------------|-----------|
| CSS class deletion breaks another view | **Phase 1 (Setup/Audit)** | Run `grep -r "glass-card\|hero-section\|..."` before ANY deletion. Save results to VERIFICATION.md. ZERO results outside Home/ = safe. Any results = BLOCKED from deletion. |
| ViewModel property removal causes NullReferenceException | **Phase 1 (Cleanup)** | Create property inventory spreadsheet: [Property Name] [Used in View? Y/N] [Used in Controller? Y/N] [Safe to Delete? Y/N]. Only mark DELETE if both Controller and View usage removed. Review with code owner. |
| Controller data-fetching orphaned (performance regression) | **Phase 2 (View Simplification)** | Measure homepage load time baseline. For EACH section removed: delete controller method, re-measure. Document delta in VERIFICATION.md. Verify >50ms improvement or investigate why. |
| AOS script removed globally despite other views using it | **Phase 1 (Audit)** | Search project: `grep -r "data-aos\|AOS\|aos.init"`. If results exist outside Home/, AOS script MUST stay in _Layout.cshtml. Document decision in ARCHITECTURE.md. |
| ViewModel properties become unmaintainable with orphaned properties | **Phase 3 (Verification)** | ViewModel review: Every property has inline XML comment with usage location + safe-to-delete status. No property appears "unused" without documentation. All `[Obsolete]` properties have removal timeline. |
| CSS imports become redundant or cascading | **Phase 2 (View Simplification)** | Check all `<link href="...css">` tags in Views/Home/Index.cshtml. If styles moved to site.css, verify old link is deleted. If home-specific, verify no duplicate imports. Single source of truth per CSS rule. |
| Database queries increase unexpectedly after "removing sections" | **Phase 2 (View Simplification)** | Enable EF Core query logging. Load homepage before and after redesign. Count SQL queries. If query count increases, investigate: orphaned data-fetching? New unrelated slow query? Document findings. |

---

## Sources

- **Codebase analysis:**
  - Controllers/HomeController.cs (lines 23–77: Index action with 8 data-fetching calls)
  - Views/Home/Index.cshtml (lines 1–299: Hero, Cards, Timeline, Deadlines sections)
  - Models/DashboardHomeViewModel.cs (8 properties: CurrentUser, Greeting, IDP*, Assessment*, TrainingStatusInfo, RecentActivities, UpcomingDeadlines)
  - wwwroot/css/home.css (513 lines: hero-section, glass-card, timeline, deadline-card, quick-access, circular-progress styles)

- **Architecture insights from codebase:**
  - No other views import home.css (link only in Views/Home/Index.cshtml)
  - No views outside Home/ use `glass-card`, `hero-section`, `timeline`, `deadline-card` class names (verified with grep)
  - AOS library loaded globally in _Layout.cshtml; used ONLY in Views/Home/Index.cshtml (data-aos attributes)
  - DashboardHomeViewModel not reused by other controllers (unique to HomeController)

- **Standards and best practices:**
  - ASP.NET Core MVC View-to-ViewModel binding patterns
  - Entity Framework Core query optimization (eager vs. lazy loading)
  - CSS namespace and scoping strategies for MVC applications
  - Performance monitoring in production systems

---

*Pitfalls research for: Portal HC KPB v3.18 Homepage Minimalist Redesign*
*Specific to removing: Glass cards (IDP, Assessment, Mandatory Training), Timeline (Recent Activity), Deadlines section, Hero animations*
*Applicable to: CSS cleanup, ViewModel consolidation, controller optimization, performance measurement phases*
*Date: 2026-03-10 | Confidence: HIGH | Analysis scope: Full codebase (Controllers, Views, Models, CSS)*
