# Phase 63: Data Source Fix - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — CDPController data layer, EF Core queries, Razor view model mapping
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- Columns stay the same: Kompetensi, Sub Kompetensi, Deliverable, Evidence, Approval SrSpv, Approval SectionHead, Approval HC
- Track info (e.g., "Panelman Tahun 2") displayed **outside the table**, near the coachee's name — not as a column per row
- Row ordering follows Proton master data `Urutan` field (Kompetensi.Urutan → SubKompetensi.Urutan → Deliverable.Urutan)
- Kompetensi and Sub Kompetensi cells use **rowspan merge** when values repeat
- Evidence column shows **badge status only** (Uploaded/Pending) — no file name or preview
- Action column retains existing dropdown actions — all actions **redirect to Deliverable page** (no inline operations)
- Table grouping: maintain existing Kompetensi → Sub Kompetensi → Deliverable hierarchy
- Mobile/responsive: **horizontal scroll** for the table
- SrSpv OR SectionHead approve = considered **100% approved** (they are equivalent)
- HC approval is a **formality** — required before coachee assessment but not counted as primary approval
- Approval status derivation from ProtonDeliverableProgress fields is Claude's discretion (may derive from Status + ApprovedById or add explicit fields)
- Dropdown starts **empty** — coach must select a coachee before table appears
- Coachee list ordered **by track** first, then alphabetically within each track group
- Dropdown shows **name only** (no track info, no progress %)
- One coachee at a time — no multi-view
- If coach has no coachees (empty CoachCoacheeMapping): **dropdown disabled** with placeholder "Tidak ada coachee"
- **Progress %** = weighted average: Locked/Active = 0%, Submitted = 50%, SrSpv/SectionHead Approved = 100%
- **Pending Actions** = count of deliverables with status Active + Rejected
- **Pending Approvals** = count of deliverables with status Submitted
- Stats position: **same as current layout** (no change)
- Data refreshed **on every page navigation** — no cache (no-cache headers)
- Switching coachee in dropdown uses **AJAX partial update** (no full page reload)
- Loading state: **spinner/loading indicator** while fetching coachee data via AJAX
- **Coachee (Level 6):** Sees only own progress. No dropdown. No action buttons.
- **Coach (Level 5):** Sees coachees assigned via **CoachCoacheeMapping**. Dropdown shows assigned coachees. Can redirect to upload evidence.
- **SrSpv/SectionHead (Level 4):** Dropdown Unit (within their Bagian) → Coachee. Can redirect to approve from Progress page.
- **HC (Level 2):** Dropdown Bagian → Unit → Coachee. Can mark HC Review.
- **Admin (Level 1):** Same as HC — full visibility.
- All actions redirect to Deliverable page — Progress page is monitoring only.
- Unauthorized access: **silently redirect to own data** instead of showing error.
- Coachee without track assignment: show **informative message** "Coachee ini belum memiliki penugasan track"
- Coachee with track but missing progress records: show **informative message** "Data progress tidak ditemukan"
- Database errors: **automatic retry** (1-2 attempts) then show generic error message
- **Cut-over langsung** — /CDP/ProtonProgress is new, /CDP/Progress is deactivated
- **New URL:** `/CDP/ProtonProgress`
- **Old URL:** `/CDP/Progress` — **disabled** (not redirected)
- **Navigation label:** Changed from "Progress" to **"Proton Progress"**
- IdpItems table **retained** — still used by Dashboard (HomeController) and CPDP Report (CMPController)
- **Reuse TrackingItem ViewModel** — same ViewModel, change mapping in controller from IdpItem to ProtonDeliverableProgress

### Claude's Discretion

- Progress % visual representation (progress bar, gauge, etc.)
- Approval field strategy (derive from Status vs. add explicit SrSpv/SectionHead fields to ProtonDeliverableProgress)
- Exact AJAX endpoint design and response format
- TrackingItem field mapping details
- QA/verification strategy after cut-over
- Spinner/loading indicator style

### Deferred Ideas (OUT OF SCOPE)

- Dashboard (HomeController) migration from IdpItems to ProtonDeliverableProgress — future phase
- CPDP Progress Report (CMPController) migration from IdpItems — future phase
- IdpItems table cleanup/removal — after all consumers migrated
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| DATA-01 | Progress page menampilkan data dari ProtonDeliverableProgress + ProtonTrackAssignment dengan konteks track (Panelman/Operator, Tahun 1/2/3), bukan dari IdpItems | EF query pattern found in BuildProtonProgressSubModelAsync; ProtonDeliverable hierarchy with Urutan fields confirmed |
| DATA-02 | Coach melihat daftar coachee asli dari CoachCoacheeMapping, bukan hardcoded mock data | CoachCoacheeMapping model confirmed with CoachId/CoacheeId/IsActive fields; _context.CoachCoacheeMappings DbSet confirmed |
| DATA-03 | Summary stats (progress %, pending actions, pending approvals) dihitung dari ProtonDeliverableProgress yang benar | ProtonDeliverableProgress.Status values confirmed ("Locked", "Active", "Submitted", "Approved", "Rejected"); formula maps directly to Status values |
| DATA-04 | Data di Progress page tersinkron otomatis dengan database — perubahan approval/evidence di Deliverable page langsung terlihat di Progress | [ResponseCache(Duration=0, NoStore=true)] pattern confirmed; AJAX partial-update endpoint needed for coachee switch |
</phase_requirements>

---

## Summary

Phase 63 is a data layer replacement: swap the existing `/CDP/Progress` action (which queries `IdpItems`) with a new `/CDP/ProtonProgress` action that queries `ProtonDeliverableProgress` joined with `ProtonTrackAssignment`. The codebase already contains all the required models, DbSets, and patterns — this is primarily a controller + view rewrite, not a new infrastructure build.

The key technical challenge is building the correct EF Core join: `ProtonDeliverableProgress` records are linked to `ProtonDeliverable`, which is linked through `ProtonSubKompetensi` → `ProtonKompetensi` → `ProtonTrack`. The Urutan-based ordering and rowspan merging in the view require careful Include chaining in the query. The BuildProtonProgressSubModelAsync method in CDPController already demonstrates the correct batch-loading pattern to avoid N+1 queries.

The second challenge is the coachee dropdown for the Coach role: the existing code uses hardcoded mock data (`mockCoachees`), which must be replaced with a real query against `CoachCoacheeMappings` filtered by `CoachId == user.Id && IsActive == true`, then joined to `ApplicationUser` for names. The AJAX partial-update pattern (fetch API, no-jQuery) is already used in ProtonCatalog and the CDP Analytics partial — the same `fetch()` approach should be used for the coachee data endpoint.

**Primary recommendation:** Create a new `ProtonProgress()` GET action in CDPController, a new `GetCoacheeDeliverables(string coacheeId)` JSON endpoint for AJAX, and a new `ProtonProgress.cshtml` view that reuses the `TrackingItem` ViewModel with updated mapping from ProtonDeliverableProgress. Disable the old `/CDP/Progress` action. Update the CDP Index page card link.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | In project (HcPortal.csproj) | Controller + View routing | Project framework |
| Entity Framework Core | In project | ORM for database queries | Project ORM |
| Microsoft.AspNetCore.Identity | In project | UserManager, role lookups | Project auth system |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap 5 | In `_Layout.cshtml` | Table, badge, spinner, dropdown UI components | All view markup |
| Bootstrap Icons | In `_Layout.cshtml` | Icons for status badges and buttons | All icon usage |
| Vanilla `fetch()` API | Already used (ProtonCatalog, CDP Analytics) | AJAX partial update for coachee data | Coachee dropdown AJAX endpoint |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Vanilla fetch() | jQuery $.ajax() | KkjMatrix uses $.ajax(); ProtonCatalog/CdpAnalytics use fetch(); both work — use fetch() for consistency with recent additions |
| Derive approval state from Status | Add explicit SrSpvApproved/SectionHeadApproved boolean fields | Adding fields requires migration; deriving from Status=="Approved" + ApprovedById covers the same logic with zero migration cost — prefer derivation |
| Server-side rowspan merge | Client-side JS merge | Server-side in Razor is simpler and avoids flash-of-unmerged-content |

**Installation:** No new packages required. All dependencies already present.

---

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
└── CDPController.cs         # Add ProtonProgress() and GetCoacheeDeliverables() actions

Views/CDP/
├── ProtonProgress.cshtml    # New view (replaces Progress.cshtml for Proton data)
└── Progress.cshtml          # Disabled (return 404 or redirect to Index)

Models/
└── TrackingModels.cs        # Reuse TrackingItem — update mapping only
```

### Pattern 1: EF Core Batch-Load with Include Chaining

**What:** Load ProtonDeliverableProgress records with full hierarchy via ThenInclude, avoiding N+1 queries.

**When to use:** The ProtonProgress action needs Kompetensi/SubKompetensi/Deliverable names for each row. Without Include, each navigation property access triggers a separate SQL query.

**Example (from CDPController.cs AssignTrack, line 729):**
```csharp
// Source: Controllers/CDPController.cs (AssignTrack)
var deliverables = await _context.ProtonDeliverableList
    .Include(d => d.ProtonSubKompetensi)
        .ThenInclude(s => s.ProtonKompetensi)
    .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == protonTrackId)
    .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
        .ThenBy(d => d.ProtonSubKompetensi.Urutan)
        .ThenBy(d => d.Urutan)
    .ToListAsync();
```

For the ProtonProgress query, add the progress join:
```csharp
// Pattern for ProtonProgress: join progress records to hierarchy
var progresses = await _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable)
        .ThenInclude(d => d.ProtonSubKompetensi)
            .ThenInclude(s => s.ProtonKompetensi)
    .Where(p => p.CoacheeId == targetCoacheeId)
    .OrderBy(p => p.ProtonDeliverable.ProtonSubKompetensi.ProtonKompetensi.Urutan)
        .ThenBy(p => p.ProtonDeliverable.ProtonSubKompetensi.Urutan)
        .ThenBy(p => p.ProtonDeliverable.Urutan)
    .ToListAsync();
```

### Pattern 2: CoachCoacheeMapping Query for Coachee List

**What:** Fetch the real coachee list for a Coach by joining CoachCoacheeMapping to ApplicationUser.

**When to use:** Coach role (RoleLevel == 5) requests coachee dropdown on ProtonProgress page.

**Example:**
```csharp
// Source: Models/CoachCoacheeMapping.cs + Data/ApplicationDbContext.cs (line 39)
var coacheeIds = await _context.CoachCoacheeMappings
    .Where(m => m.CoachId == user.Id && m.IsActive)
    .Select(m => m.CoacheeId)
    .ToListAsync();

var coachees = await _context.Users
    .Where(u => coacheeIds.Contains(u.Id))
    .OrderBy(u => u.FullName)
    .ToListAsync();

// For ordering by track first, then name:
var assignments = await _context.ProtonTrackAssignments
    .Include(a => a.ProtonTrack)
    .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
    .ToDictionaryAsync(a => a.CoacheeId, a => a);

var orderedCoachees = coachees
    .OrderBy(u => assignments.TryGetValue(u.Id, out var a) ? a.ProtonTrack?.Urutan ?? 999 : 999)
    .ThenBy(u => u.FullName)
    .ToList();
```

### Pattern 3: AJAX Partial Update for Coachee Switch

**What:** When the Coach dropdown changes, fetch the deliverable table HTML (or JSON) for the new coachee without a full page reload.

**When to use:** Coachee selector change event on ProtonProgress page.

**Example (from Views/ProtonCatalog/Index.cshtml line 216):**
```javascript
// Source: Views/ProtonCatalog/Index.cshtml
fetch('/ProtonCatalog/GetCatalogTree?trackId=' + trackId)
    .then(r => r.json())
    .then(data => { /* update DOM */ });
```

For ProtonProgress, the endpoint should return JSON:
```csharp
// New JSON endpoint in CDPController
[HttpGet]
public async Task<IActionResult> GetCoacheeDeliverables(string coacheeId)
{
    // Returns JSON array of TrackingItem + track context
    // Verify caller has access to this coacheeId before returning data
}
```

### Pattern 4: TrackingItem Mapping from ProtonDeliverableProgress

**What:** Map ProtonDeliverableProgress (with loaded navigation properties) to TrackingItem for view compatibility.

**Current mapping (IdpItem → TrackingItem, Progress() action, line 1473):**
```csharp
// Source: Controllers/CDPController.cs (Progress action, line 1473)
var data = idpItems.Select(idp => new TrackingItem
{
    Id = idp.Id,
    Kompetensi = idp.Kompetensi ?? "",
    SubKompetensi = idp.SubKompetensi ?? "",
    Deliverable = idp.Deliverable ?? "",
    EvidenceStatus = string.IsNullOrEmpty(idp.Evidence) ? "Pending" : "Uploaded",
    ApprovalSrSpv = idp.ApproveSrSpv ?? "Not Started",
    ApprovalSectionHead = idp.ApproveSectionHead ?? "Not Started",
    ApprovalHC = idp.ApproveHC ?? "Not Started",
}).ToList();
```

**New mapping (ProtonDeliverableProgress → TrackingItem):**
```csharp
var data = progresses.Select(p => new TrackingItem
{
    Id = p.Id,
    Kompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "",
    SubKompetensi = p.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "",
    Deliverable = p.ProtonDeliverable?.NamaDeliverable ?? "",
    EvidenceStatus = p.EvidencePath != null ? "Uploaded" : "Pending",
    // Approval derivation: Status=="Approved" means SrSpv OR SectionHead approved
    // (SrSpv and SectionHead are equivalent — either one counts as 100% approved)
    ApprovalSrSpv = p.Status == "Approved" ? "Approved"
                  : p.Status == "Rejected" ? "Rejected"
                  : p.Status == "Submitted" ? "Pending"
                  : "Not Started",
    ApprovalSectionHead = p.Status == "Approved" ? "Approved"
                        : p.Status == "Rejected" ? "Rejected"
                        : p.Status == "Submitted" ? "Pending"
                        : "Not Started",
    ApprovalHC = p.HCApprovalStatus == "Reviewed" ? "Approved" : "Pending",
    SupervisorComments = p.RejectionReason ?? "",
}).ToList();
```

Note: Since SrSpv and SectionHead are equivalent approvers, both columns show the same derived value. The `ApprovedById` field exists on ProtonDeliverableProgress but no role-flag distinguishes SrSpv from SectionHead — deriving from Status is the correct approach without a migration.

### Pattern 5: No-Cache Response Header

**What:** Ensure Progress page always fetches fresh data from DB, not from browser/proxy cache.

**When to use:** On the new ProtonProgress action (DATA-04 requirement).

**Example (from Controllers/HomeController.cs line 279):**
```csharp
// Source: Controllers/HomeController.cs
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public async Task<IActionResult> ProtonProgress(string? coacheeId = null)
{
    // ...
}
```

### Pattern 6: Summary Stats Computation

**What:** Compute the three stat cards from ProtonDeliverableProgress records.

**Formula (from CONTEXT.md decisions):**
- Progress % = weighted sum / total deliverables × 100
  - Locked/Active → 0 weight
  - Submitted → 0.5 weight
  - Approved (SrSpv/SectionHead) → 1.0 weight
- Pending Actions = count of status "Active" + "Rejected"
- Pending Approvals = count of status "Submitted"

```csharp
// Compute from loaded progresses list
int total = progresses.Count;
double weightedSum = progresses.Sum(p =>
    p.Status == "Approved" ? 1.0 :
    p.Status == "Submitted" ? 0.5 : 0.0);
int progressPercent = total > 0 ? (int)(weightedSum / total * 100) : 0;
int pendingActions = progresses.Count(p => p.Status == "Active" || p.Status == "Rejected");
int pendingApprovals = progresses.Count(p => p.Status == "Submitted");
```

### Pattern 7: Rowspan Merging in Razor

**What:** Merge Kompetensi and SubKompetensi cells vertically when consecutive rows share the same value.

**When to use:** Razor view rendering of the deliverable table.

**Implementation approach (server-side):**
```csharp
// In the view, compute rowspan counts before rendering
// Group rows by Kompetensi then SubKompetensi
var kompetensiGroups = data
    .GroupBy(x => x.Kompetensi)
    .Select(g => new {
        Kompetensi = g.Key,
        RowCount = g.Count(),
        SubGroups = g.GroupBy(x => x.SubKompetensi)
            .Select(sg => new {
                SubKompetensi = sg.Key,
                RowCount = sg.Count(),
                Items = sg.ToList()
            }).ToList()
    }).ToList();
```

Then in Razor:
```html
@foreach (var kompGroup in kompetensiGroups)
{
    bool firstKomp = true;
    foreach (var subGroup in kompGroup.SubGroups)
    {
        bool firstSub = true;
        foreach (var item in subGroup.Items)
        {
            <tr>
            @if (firstKomp) {
                <td rowspan="@kompGroup.RowCount">@kompGroup.Kompetensi</td>
                firstKomp = false;
            }
            @if (firstSub) {
                <td rowspan="@subGroup.RowCount">@subGroup.SubKompetensi</td>
                firstSub = false;
            }
            <!-- Deliverable, Evidence, Approvals, Action -->
            </tr>
        }
    }
}
```

### Anti-Patterns to Avoid

- **Do not query IdpItems in ProtonProgress():** The entire purpose of Phase 63 is to replace IdpItems with ProtonDeliverableProgress. Any IdpItems reference in the new action is a bug.
- **Do not use Unit-based scoping for Coach role:** The old BuildProtonProgressSubModelAsync scopes Coach by `user.Unit`. Phase 63 requires Coach scoping via `CoachCoacheeMapping.CoachId` instead.
- **Do not load navigation properties lazily:** The project uses EF with SQLite (HcPortal.db). Lazy loading may not be configured. Always use `.Include().ThenInclude()` explicitly.
- **Do not redirect /CDP/Progress to /CDP/ProtonProgress:** CONTEXT.md explicitly states to disable (404 or redirect to Index), not redirect to the new URL.
- **Do not re-enable the inline approval buttons from the old view:** Actions column must redirect to Deliverable page only — no inline approve/reject in Phase 63.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Role-based scoping | Custom RBAC middleware | `userLevel` / `UserRoles.*` constants already in project | Pattern established in every existing CDPController action |
| Approval status display | New field in DB | Derive from `ProtonDeliverableProgress.Status` | Status state machine already covers all approval states; no migration needed |
| Track label display | New track name field | `ProtonTrack.DisplayName` (format: "Panelman - Tahun 1") or `TrackType + " " + TahunKe` | ProtonTrack model already has DisplayName auto-generated at seed time |
| User name resolution | New user service | `_context.Users.Where(u => ids.Contains(u.Id))` batch pattern | Used throughout CDPController — BuildProtonProgressSubModelAsync line 249 |

**Key insight:** The project deliberately avoids navigation FK constraints on user IDs (CoacheeId is a string, not FK) to prevent cascade delete issues. Always load user display names via a separate batch query, never via a navigation property on the progress model.

---

## Common Pitfalls

### Pitfall 1: Approval State Ambiguity (SrSpv vs SectionHead)

**What goes wrong:** The old Progress.cshtml has separate "Sr. Supervisor" and "Section Head" approval columns with different logic. `ProtonDeliverableProgress` has a single `Status` field that becomes "Approved" regardless of which level-4 role approved. There is no separate `SrSpvApproved`/`SectionHeadApproved` boolean.

**Why it happens:** The original approval workflow set Status="Approved" and records `ApprovedById`, but doesn't store which role the approver held at the time of approval.

**How to avoid:** Derive both ApprovalSrSpv and ApprovalSectionHead from the same `Status` field. Both columns show the same state. Document this explicitly in a comment. The CONTEXT.md decision supports this: "SrSpv OR SectionHead approve = considered 100% approved (they are equivalent)."

**Warning signs:** If you see different values being shown in the SrSpv and SectionHead columns for the same progress record, the derivation logic is wrong.

---

### Pitfall 2: N+1 Query on Deliverable Hierarchy

**What goes wrong:** Loading `ProtonDeliverableProgresses` without Include, then accessing `.ProtonDeliverable.ProtonSubKompetensi.ProtonKompetensi.NamaKompetensi` in a loop generates one SQL query per row.

**Why it happens:** EF Core's navigation properties return `null` without `.Include()` when lazy loading is not configured (SQLite + default EF setup in this project).

**How to avoid:** Always chain `.Include(p => p.ProtonDeliverable).ThenInclude(d => d.ProtonSubKompetensi).ThenInclude(s => s.ProtonKompetensi)` in the initial query.

**Warning signs:** `NullReferenceException` in view on `p.ProtonDeliverable.NamaDeliverable`, or unexpectedly slow page loads.

---

### Pitfall 3: Stale Coach Coachee List (IsActive Filter Missing)

**What goes wrong:** Querying `CoachCoacheeMappings` without filtering `IsActive == true` returns disabled coach-coachee relationships, showing coachees that are no longer under the coach.

**Why it happens:** `CoachCoacheeMapping.IsActive` and `CoachCoacheeMapping.EndDate` exist specifically for soft-delete. Without filtering, historical mappings appear active.

**How to avoid:** Always add `.Where(m => m.CoachId == user.Id && m.IsActive)`.

**Warning signs:** Coach sees more coachees than expected, including former coachees.

---

### Pitfall 4: AJAX Security — Unauthorized Coachee Data Exposure

**What goes wrong:** The `GetCoacheeDeliverables(string coacheeId)` AJAX endpoint returns data for any coacheeId passed in the URL without verifying that the requesting user has permission to view that coachee's data.

**Why it happens:** AJAX endpoints are easy to call directly with a manipulated coacheeId.

**How to avoid:** In the AJAX endpoint, verify:
- If caller is Coach: `_context.CoachCoacheeMappings.Any(m => m.CoachId == user.Id && m.CoacheeId == coacheeId && m.IsActive)`
- If caller is SrSpv/SectionHead: coachee's `Section == user.Section`
- If caller is HC/Admin: allow all
- If caller is Coachee: only allow `coacheeId == user.Id`
- If unauthorized: silently return own data (not 403)

**Warning signs:** The AJAX endpoint returns 200 with data for any random user ID.

---

### Pitfall 5: Progress % Integer Rounding

**What goes wrong:** `(int)(weightedSum / total * 100)` may show 0% when it should show 50% for edge cases (e.g., 1 submitted out of 1 total deliverable: 0.5 / 1 * 100 = 50.0 → cast to int = 50, this is actually fine).

**Why it happens:** The formula is correct for this case. The real risk is dividing by zero when `total == 0`.

**How to avoid:** Always guard with `total > 0 ? ... : 0`.

**Warning signs:** NaN or divide-by-zero errors for coachees with no track assignment.

---

### Pitfall 6: TrackingItem Reuse — Periode Field

**What goes wrong:** `TrackingItem.Periode` was left empty in the original IdpItem mapping ("Not in IdpItem schema, can be added later"). With ProtonDeliverableProgress, the track info is available via ProtonTrackAssignment. But the user decision is to show track info **outside the table** (near coachee name), not in a Periode column.

**Why it happens:** TrackingItem has a `Periode` field that was never used.

**How to avoid:** Leave `Periode` empty or set it to `track.TahunKe` for potential future use, but do not render it as a table column. Display track info via a separate ViewBag or ViewModel property above the table.

---

## Code Examples

Verified patterns from project source:

### ProtonDeliverableProgress Status Values
```csharp
// Source: Models/ProtonModels.cs (line 89)
// Values: "Locked", "Active", "Submitted", "Approved", "Rejected"
public string Status { get; set; } = "Locked";

// HCApprovalStatus is independent:
// Values: "Pending" or "Reviewed"
public string HCApprovalStatus { get; set; } = "Pending";
```

### ApplicationDbContext DbSet Names
```csharp
// Source: Data/ApplicationDbContext.cs (lines 39-46)
public DbSet<CoachCoacheeMapping> CoachCoacheeMappings { get; set; }
public DbSet<ProtonTrackAssignment> ProtonTrackAssignments { get; set; }
public DbSet<ProtonDeliverableProgress> ProtonDeliverableProgresses { get; set; }
public DbSet<ProtonDeliverable> ProtonDeliverableList { get; set; }
// Note: ProtonDeliverableList (not ProtonDeliverables) — unusual naming
```

### UserRoles Constants
```csharp
// Source: Models/UserRoles.cs
UserRoles.Coach       // "Coach"
UserRoles.HC          // "HC"
UserRoles.Admin       // "Admin"
UserRoles.SrSupervisor // "Sr Supervisor"
UserRoles.SectionHead  // "Section Head"
UserRoles.Coachee     // "Coachee"
```

### Role Level Pattern Used Throughout CDPController
```csharp
// Source: Controllers/CDPController.cs (every action)
var user = await _userManager.GetUserAsync(User);
if (user == null) return Challenge();
var roles = await _userManager.GetRolesAsync(user);
var userRole = roles.FirstOrDefault() ?? "";
int userLevel = user.RoleLevel;
```

### No-Cache Attribute
```csharp
// Source: Controllers/HomeController.cs (line 279)
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public async Task<IActionResult> ProtonProgress(...)
```

### Disabling an Action (Pattern for Old /CDP/Progress)
```csharp
// Option A: Return 404
public IActionResult Progress() => NotFound();

// Option B: Redirect to Index
public IActionResult Progress() => RedirectToAction("Index");
```

### Navigation Link Pattern (CDP/Index.cshtml link update)
```html
<!-- Source: Views/CDP/Index.cshtml (line 51) — to be updated -->
<a href="@Url.Action("ProtonProgress", "CDP")" class="btn btn-warning w-100">
    <i class="bi bi-arrow-right-circle me-2"></i>Proton Progress
</a>
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Mock coachee list (3 hardcoded ApplicationUser objects) | Real query from CoachCoacheeMapping | Phase 63 (this phase) | Phase 63 replaces mock with real data |
| IdpItems query for progress rows | ProtonDeliverableProgress + hierarchy Include | Phase 63 (this phase) | Core data layer replacement |
| Unit-based Coach scoping (BuildProtonProgressSubModelAsync) | CoachCoacheeMapping-based Coach scoping | Phase 63 (this phase) | Correct coach-coachee relationship used |
| /CDP/Progress URL | /CDP/ProtonProgress URL | Phase 63 (this phase) | New URL, old disabled |

**Deprecated in this phase:**
- `Progress()` action in CDPController — disabled (returns NotFound or redirects to Index)
- The mock coachees in `Progress()` action (lines 1447-1453 of CDPController.cs)
- IdpItems query in `Progress()` action (line 1466 of CDPController.cs)

---

## Open Questions

1. **Approval column display when both SrSpv and SectionHead columns show same value**
   - What we know: Both approval columns will show identical states derived from `Status` field
   - What's unclear: Does the UI need to distinguish which level-4 role actually approved (using ApprovedById + a user lookup)?
   - Recommendation: Follow CONTEXT.md — "SrSpv OR SectionHead approve = considered 100% approved (they are equivalent)." Show the same badge in both columns. Do not add a role-lookup; it would require an extra query per row.

2. **AJAX endpoint format: JSON vs HTML partial**
   - What we know: ProtonCatalog uses JSON + client-side DOM rebuild; Assessment monitoring uses fetch + JSON
   - What's unclear: Should `GetCoacheeDeliverables` return JSON (client renders the table) or an HTML partial (server renders, client swaps innerHTML)?
   - Recommendation: Return JSON array of TrackingItem-like objects + track metadata. Client-side JS builds the table rows and updates the stat cards. This keeps the endpoint simple and avoids a Razor partial rendering path.

3. **Coachee Level 6 view — does it need a separate query path or reuse the same AJAX endpoint?**
   - What we know: Coachee sees own data only, no dropdown. They always see their own `user.Id`.
   - What's unclear: Whether the initial page load for Coachee should eagerly load their data (no AJAX needed) while Coach/SrSpv/HC use AJAX.
   - Recommendation: For Coachee, load data eagerly on page GET and pass via ViewModel (no AJAX). For all other roles, initial page load shows empty state, data loads via AJAX when coachee is selected.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None detected — no test project found in repository |
| Config file | None |
| Quick run command | Manual browser testing (build + run) |
| Full suite command | `dotnet build` (compilation check) |

No automated test infrastructure exists in this project. All validation is manual browser testing.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| DATA-01 | Table rows come from ProtonDeliverableProgress, not IdpItems | manual-only | `dotnet build` (compile check) | N/A — no test files |
| DATA-02 | Coach dropdown shows real coachees from CoachCoacheeMapping | manual-only | `dotnet build` (compile check) | N/A — no test files |
| DATA-03 | Summary stats match actual ProtonDeliverableProgress records | manual-only | `dotnet build` (compile check) | N/A — no test files |
| DATA-04 | Changes on Deliverable page immediately reflect on Progress page (no stale cache) | manual-only | `dotnet build` (compile check) | N/A — no test files |

**Manual test protocol (replace automated testing):**
1. Build and run: `dotnet run` from project root
2. Log in as a Coach user with known CoachCoacheeMapping entries — verify dropdown shows real names
3. Log in as a Coachee with a ProtonTrackAssignment — verify table rows match ProtonDeliverableProgress records
4. Approve an evidence on the Deliverable page — navigate to ProtonProgress — verify status badge updated
5. Navigate to `/CDP/Progress` — verify it is disabled (404 or redirect)
6. Navigate to `/CDP/ProtonProgress` — verify it loads correctly

### Sampling Rate
- **Per task commit:** `dotnet build` (compilation must pass)
- **Per wave merge:** Build + manual smoke test (login as Coach, Coachee, HC)
- **Phase gate:** Full manual test protocol above before marking phase complete

### Wave 0 Gaps
- [ ] No test project exists — all validation is manual. No Wave 0 test file creation needed.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/CDPController.cs` — existing Progress() action (lines 1412-1488), BuildProtonProgressSubModelAsync (lines 205-360), AssignTrack (lines 673-751)
- `Models/ProtonModels.cs` — ProtonDeliverableProgress, ProtonTrackAssignment, ProtonTrack, ProtonDeliverable models
- `Models/CoachCoacheeMapping.cs` — CoachId, CoacheeId, IsActive fields
- `Models/TrackingModels.cs` — TrackingItem ViewModel (reuse target)
- `Models/CDPDashboardViewModel.cs` — CoacheeProgressRow pattern (batch-load reference)
- `Data/ApplicationDbContext.cs` — DbSet names and registration
- `Views/CDP/Progress.cshtml` — existing view structure (column layout, stat cards, role-conditional rendering)
- `Views/ProtonCatalog/Index.cshtml` — fetch() AJAX pattern
- `.planning/phases/63-data-source-fix/63-CONTEXT.md` — all locked decisions

### Secondary (MEDIUM confidence)
- `Models/UserRoles.cs` — role constant names verified against CDPController usage
- `Models/ApplicationUser.cs` — RoleLevel field confirmed

### Tertiary (LOW confidence)
- None

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project, no new dependencies
- Architecture patterns: HIGH — patterns extracted directly from existing CDPController code
- Pitfalls: HIGH — identified from direct code inspection (mock data in Progress(), missing IsActive filter risk, N+1 Include requirement)

**Research date:** 2026-02-27
**Valid until:** 2026-03-27 (stable — no external dependencies changing)
