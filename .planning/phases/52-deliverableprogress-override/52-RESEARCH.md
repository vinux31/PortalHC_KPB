# Phase 52: DeliverableProgress Override - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — EF Core batch update, Bootstrap modal with AJAX, Bootstrap nav-tab extension, admin override workflow
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Phase Boundary:**
- Admin/HC can view all ProtonDeliverableProgress records and override stuck or erroneous statuses
- Also removes sequential lock logic (parallel with Phase 65 v2.4 work) — all deliverables become Active on assignment instead of Locked
- Scope: Override page + lock removal. New approval actions on Progress page are Phase 65.

**Record Discovery:**
- Tab ke-3 di /ProtonData: "Coaching Proton Override" (after Silabus and Coaching Guidance)
- Tab only visible/rendered for Admin and HC roles
- Filter cascade: Bagian → Unit → Track (consistent with Silabus tab pattern)
- Per-worker rows: one row per worker, deliverable badges as sub-columns showing status
- Additional filter dropdown above table: Semua / Hanya Rejected / Hanya Pending HC (filter by status to focus on problematic records)

**Table & Badge Display:**
- Badge colors: Active=blue, Submitted=yellow, Approved=green, Rejected=red
- No "Locked" status — removed (see Lock Removal below)
- All badges are clickable (including Approved — admin may need to revert)
- Clicking any badge opens Bootstrap modal with full context + override form

**Override Modal Content:**
- Full context display: current status, evidence file (download link), all timestamps (SubmittedAt/ApprovedAt/RejectedAt), RejectionReason, HCApprovalStatus, ApprovedById/HCReviewedById
- Override form with 2 dropdowns:
  1. Status utama: Active / Submitted / Approved / Rejected
  2. HC Status: Pending / Reviewed
- Editable RejectionReason textarea (can edit or clear)
- Mandatory "Alasan Override" textarea — required before save
- Single "Simpan Override" button — no double confirmation needed

**Override Behavior:**
- Auto-fill timestamps on status change:
  - → Approved: set ApprovedAt=now, ApprovedById=current admin/HC user
  - → Rejected: set RejectedAt=now
  - → Submitted: set SubmittedAt=now
  - → Active: clear ApprovedAt, RejectedAt, SubmittedAt timestamps
- Override reason logged to AuditLog (existing v1.7 AuditLogService)
- Individual override only — no bulk operations

**Override Access:**
- Admin AND HC roles can access tab and perform overrides
- Both roles have identical capabilities on this page

**Sequential Lock Removal (parallel with Phase 65):**
- Remove "Locked" status entirely from the system
- CDPController AssignTrack: all deliverables created as "Active" (not first=Active, rest=Locked)
- CDPController Deliverable(): remove sequential lock check (lines 817-830)
- ProtonProgress stats: remove Locked from status counts and chart labels
- Override dropdown: no Locked option
- Existing "Locked" records in DB: bulk-update to "Active" (migration or startup seed)

### Claude's Discretion
- Exact modal layout and spacing
- AJAX vs full page reload on save
- Loading spinner behavior
- Error message display format

### Deferred Ideas (OUT OF SCOPE)
- Bulk override (select multiple records, override all at once) — future enhancement if needed
- Override history log visible in modal (who overrode what when) — separate from AuditLog page
- Worker reassignment/transfer handling (orphan progress records) — future phase
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| OPER-03 | Admin can view and override ProtonDeliverableProgress status — correct stuck or erroneous deliverable records | ProtonDeliverableProgress model fully documented; ProtonDataController tab extension pattern established from Phase 51; AuditLogService available and proven; CDPController AssignTrack + Deliverable() lock logic located at specific lines for removal |
</phase_requirements>

## Summary

Phase 52 has two distinct deliverables: (1) a new "Coaching Proton Override" tab added to the existing `/ProtonData` page where Admin and HC can view all ProtonDeliverableProgress records per-worker with deliverable badges, click any badge to open a Bootstrap modal with full context, and submit an override that updates status + timestamps + AuditLog; (2) complete removal of the "Locked" status from the system — CDPController AssignTrack changes all deliverables to Active on creation, Deliverable() sequential lock check removed, stats/charts in BuildProtonProgressSubModelAsync updated, and existing Locked DB records bulk-migrated to Active.

The ProtonData page already uses Bootstrap nav-tabs with two tabs (Silabus, Coaching Guidance). Adding a third tab follows an identical pattern with no structural changes to the controller routing (the tab content lives in the same Index view). The override modal is a standard Bootstrap modal with AJAX POST — the same pattern used in Phase 50 (CoachCoacheeMapping modals) and Phase 51 (GuidanceReplace/Delete modals). The ProtonDeliverableProgress model already has all required fields: Status, EvidencePath/FileName, SubmittedAt/ApprovedAt/RejectedAt, RejectionReason, ApprovedById, HCApprovalStatus, HCReviewedAt, HCReviewedById.

The cross-cutting concern is that "Locked" removal touches multiple files: CDPController (two methods), CDPDashboardViewModel (CoacheeProgressRow.Locked property), and BuildProtonProgressSubModelAsync (statusLabels list, statusData list, CoacheeProgressRow population). No EF migration is needed for schema changes — only a data migration (UPDATE ProtonDeliverableProgresses SET Status='Active' WHERE Status='Locked'). The ProtonDeliverableProgress model's Status field is a string, no enum constraint.

**Primary recommendation:** Split into two plans. Plan 01: Override tab UI + GET data load + OverrideSave POST endpoint + AuditLog. Plan 02: Lock removal — CDPController changes + stats cleanup + DB data migration. Both plans can be executed in wave 1 (no inter-plan dependency) but Plan 01 should be verified first since it's the core feature.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | .NET 8 | Controller + View framework | Project standard |
| Entity Framework Core | .NET 8 | ORM — query and update ProtonDeliverableProgress | Project standard |
| Bootstrap 5 | CDN via _Layout | Modal, nav-tabs, badge styling | Project UI standard |
| AuditLogService | v1.7 (project-internal) | Log override actions to AuditLog table | Already injected in ProtonDataController |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| UserManager<ApplicationUser> | ASP.NET Identity | Get current user for ApprovedById/HCReviewedById | Already injected in ProtonDataController |
| System.Text.Json | .NET 8 stdlib | Serialize data islands for JS | Already used in ProtonData/Index.cshtml |

**Installation:** No new packages required. All dependencies already present.

## Architecture Patterns

### Recommended Project Structure

No new files needed. All changes are additive to existing files:

```
Controllers/
  ProtonDataController.cs        — Add OverrideList GET + OverrideSave POST actions
  CDPController.cs               — Remove Locked logic from AssignTrack + Deliverable()
Views/
  ProtonData/
    Index.cshtml                 — Add third tab + override table + override modal
Models/
  CDPDashboardViewModel.cs       — Remove Locked field from CoacheeProgressRow
```

Data migration is handled as a raw SQL command in a new EF migration file, or a startup seed — context decision for Claude.

### Pattern 1: Third Bootstrap Nav-Tab in ProtonData/Index.cshtml

**What:** Add a third `<li class="nav-item">` to the existing `<ul class="nav nav-tabs">` and a corresponding `<div class="tab-pane">` to the existing `<div class="tab-content">`.

**When to use:** Extending an existing tab page. No routing changes needed.

**Example (from existing Phase 51 pattern):**
```html
<!-- In the nav-tabs ul — add after Coaching Guidance tab li -->
<li class="nav-item" role="presentation">
    <button class="nav-link" id="override-tab" data-bs-toggle="tab" data-bs-target="#overrideTabContent"
            type="button" role="tab" aria-controls="overrideTabContent" aria-selected="false">
        <i class="bi bi-shield-exclamation me-1"></i>Coaching Proton Override
    </button>
</li>

<!-- In the tab-content div — add after Coaching Guidance tab-pane -->
<div class="tab-pane fade" id="overrideTabContent" role="tabpanel" aria-labelledby="override-tab">
    <!-- Filter cascade + table + modal here -->
</div>
```

**Key insight:** The tab is within the existing `[Authorize(Roles = "Admin,HC")]` controller. No additional role check needed at controller level since ProtonDataController is already restricted to Admin and HC.

### Pattern 2: Override Table — Per-Worker Row with Deliverable Badges

**What:** Query all coachees + their ProtonDeliverableProgress records. Group by coachee. Render one table row per coachee with deliverable badges as inline elements.

**Data query approach:**
```csharp
// In ProtonDataController.OverrideList GET action
// 1. Get deliverables for the selected Bagian+Unit+Track scope
var deliverables = await _context.ProtonDeliverableList
    .Include(d => d.ProtonSubKompetensi)
        .ThenInclude(s => s.ProtonKompetensi)
    .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.Bagian == bagian
             && d.ProtonSubKompetensi.ProtonKompetensi.Unit == unit
             && d.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == trackId)
    .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
        .ThenBy(d => d.ProtonSubKompetensi.Urutan)
        .ThenBy(d => d.Urutan)
    .ToListAsync();

var deliverableIds = deliverables.Select(d => d.Id).ToList();

// 2. Get all progress records for these deliverables
var allProgresses = await _context.ProtonDeliverableProgresses
    .Where(p => deliverableIds.Contains(p.ProtonDeliverableId))
    .ToListAsync();

// 3. Get coachee names
var coacheeIds = allProgresses.Select(p => p.CoacheeId).Distinct().ToList();
var coacheeNames = await _context.Users
    .Where(u => coacheeIds.Contains(u.Id))
    .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);
```

**Status filter (additional dropdown):**
- "Semua" — no filter, return all coachees
- "Hanya Rejected" — only coachees who have at least one progress record with Status == "Rejected"
- "Hanya Pending HC" — only coachees who have at least one progress record with HCApprovalStatus == "Pending" AND Status == "Approved"

This filter is applied in-memory after loading all data (small dataset per unit scope).

**Badge rendering (Razor):**
```razor
@foreach (var deliverable in deliverables)
{
    var progress = coacheeProgresses.FirstOrDefault(p => p.ProtonDeliverableId == deliverable.Id);
    var status = progress?.Status ?? "—";
    var badgeClass = status switch {
        "Active"    => "bg-primary",
        "Submitted" => "bg-warning",
        "Approved"  => "bg-success",
        "Rejected"  => "bg-danger",
        _           => "bg-secondary"
    };
    if (progress != null)
    {
        <button class="badge @badgeClass border-0 me-1 btn-override"
                data-progress-id="@progress.Id"
                title="@deliverable.NamaDeliverable">
            @status[0]  <!-- First letter abbreviation or short label -->
        </button>
    }
}
```

**Modal data loading:** Pass progress ID as `data-progress-id` attribute on badge. JS fetches `/ProtonData/OverrideDetail?id=X` (GET, returns JSON with full record context) to populate the modal before showing it. This is AJAX-then-show pattern (load on click) to avoid large data islands.

### Pattern 3: OverrideSave POST Endpoint

**What:** Receives progressId + newStatus + newHCStatus + newRejectionReason + overrideReason. Updates record, auto-fills timestamps, writes AuditLog.

**Example:**
```csharp
// DTO at namespace level (outside class, inside namespace — Phase 47+ pattern)
public class OverrideSaveRequest
{
    public int ProgressId { get; set; }
    public string NewStatus { get; set; } = "";
    public string NewHCStatus { get; set; } = "";
    public string? NewRejectionReason { get; set; }
    public string OverrideReason { get; set; } = "";
}

// POST: /ProtonData/OverrideSave
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> OverrideSave([FromBody] OverrideSaveRequest req)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    if (string.IsNullOrWhiteSpace(req.OverrideReason))
        return Json(new { success = false, message = "Alasan override wajib diisi." });

    var progress = await _context.ProtonDeliverableProgresses.FindAsync(req.ProgressId);
    if (progress == null) return Json(new { success = false, message = "Record tidak ditemukan." });

    var oldStatus = progress.Status;

    // Auto-fill timestamps based on new status
    switch (req.NewStatus)
    {
        case "Approved":
            progress.ApprovedAt = DateTime.UtcNow;
            progress.ApprovedById = user.Id;
            break;
        case "Rejected":
            progress.RejectedAt = DateTime.UtcNow;
            break;
        case "Submitted":
            progress.SubmittedAt = DateTime.UtcNow;
            break;
        case "Active":
            progress.ApprovedAt = null;
            progress.RejectedAt = null;
            progress.SubmittedAt = null;
            break;
    }

    progress.Status = req.NewStatus;
    progress.HCApprovalStatus = req.NewHCStatus;
    progress.RejectionReason = req.NewRejectionReason;

    await _context.SaveChangesAsync();

    await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "Override",
        $"Override deliverable progress #{progress.Id}: {oldStatus} → {req.NewStatus}. Alasan: {req.OverrideReason}",
        targetId: progress.Id, targetType: "ProtonDeliverableProgress");

    return Json(new { success = true });
}
```

### Pattern 4: Lock Removal in CDPController

**What:** Remove the sequential lock mechanism introduced at initial phase. Three changes:

**Change 1 — AssignTrack (line ~743):** Change `Status = index == 0 ? "Active" : "Locked"` to `Status = "Active"` for all deliverables.

```csharp
// BEFORE:
var progressList = deliverables.Select((d, index) => new ProtonDeliverableProgress
{
    CoacheeId = coacheeId,
    ProtonDeliverableId = d.Id,
    Status = index == 0 ? "Active" : "Locked",
    CreatedAt = DateTime.UtcNow
}).ToList();

// AFTER:
var progressList = deliverables.Select(d => new ProtonDeliverableProgress
{
    CoacheeId = coacheeId,
    ProtonDeliverableId = d.Id,
    Status = "Active",
    CreatedAt = DateTime.UtcNow
}).ToList();
```

**Change 2 — Deliverable() sequential lock check (lines 817-830):** Remove the block entirely. Set `isAccessible = true` always (or remove the IsAccessible logic from the ViewModel and View if unused after this change).

```csharp
// REMOVE this entire block:
// Sequential lock check
int currentIndex = orderedProgresses.FindIndex(p => p.Id == progress.Id);
bool isAccessible;
if (currentIndex <= 0)
{
    isAccessible = true;
}
else
{
    var previousProgress = orderedProgresses[currentIndex - 1];
    isAccessible = previousProgress.Status == "Approved";
}
```

Also remove the `orderedProgresses` computation if it's only used for the lock check. The `allProgresses` load can be removed too unless used elsewhere in Deliverable().

**Change 3 — ApproveDeliverable() unlock-next block (lines 930-939):** Remove the block that unlocks the next deliverable when one is approved:

```csharp
// REMOVE this block:
int currentIndex = orderedProgresses.FindIndex(p => p.Id == progress.Id);
if (currentIndex >= 0 && currentIndex < orderedProgresses.Count - 1)
{
    var nextProgress = orderedProgresses[currentIndex + 1];
    if (nextProgress.Status == "Locked")
    {
        nextProgress.Status = "Active";
    }
}
```

Also remove the `allProgresses` + `orderedProgresses` load from ApproveDeliverable() if they were only used for this unlock logic.

**Change 4 — BuildProtonProgressSubModelAsync:** Remove "Locked" from status labels and data:

```csharp
// BEFORE:
var statusLabels = new List<string> { "Approved", "Submitted", "Active", "Rejected", "Locked" };
var statusData = new List<int>
{
    allProgresses.Count(p => p.Status == "Approved"),
    allProgresses.Count(p => p.Status == "Submitted"),
    allProgresses.Count(p => p.Status == "Active"),
    allProgresses.Count(p => p.Status == "Rejected"),
    allProgresses.Count(p => p.Status == "Locked")
};

// AFTER:
var statusLabels = new List<string> { "Approved", "Submitted", "Active", "Rejected" };
var statusData = new List<int>
{
    allProgresses.Count(p => p.Status == "Approved"),
    allProgresses.Count(p => p.Status == "Submitted"),
    allProgresses.Count(p => p.Status == "Active"),
    allProgresses.Count(p => p.Status == "Rejected")
};
```

Also remove `Locked = progresses.Count(p => p.Status == "Locked")` from CoacheeProgressRow construction, and remove the `Locked` property from `CoacheeProgressRow` in CDPDashboardViewModel.cs.

**Change 5 — Data migration:** Bulk-update existing Locked records to Active. Can be done via a raw SQL command in a new EF Core migration:

```csharp
// In new migration Up() method:
migrationBuilder.Sql("UPDATE ProtonDeliverableProgresses SET Status = 'Active' WHERE Status = 'Locked'");
```

This is the safest approach (runs once, tracked, reversible). The Down() method cannot restore original Locked states so leave it as-is (or as a no-op).

### Pattern 5: OverrideDetail GET (AJAX endpoint for modal population)

**What:** Returns full ProtonDeliverableProgress record as JSON for modal display.

```csharp
// GET: /ProtonData/OverrideDetail?id=X
public async Task<IActionResult> OverrideDetail(int id)
{
    var progress = await _context.ProtonDeliverableProgresses
        .Include(p => p.ProtonDeliverable)
            .ThenInclude(d => d.ProtonSubKompetensi)
                .ThenInclude(s => s.ProtonKompetensi)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (progress == null) return Json(new { success = false });

    // Get approver name if exists
    string? approverName = null;
    if (!string.IsNullOrEmpty(progress.ApprovedById))
    {
        approverName = await _context.Users
            .Where(u => u.Id == progress.ApprovedById)
            .Select(u => u.FullName ?? u.UserName)
            .FirstOrDefaultAsync();
    }

    string? hcReviewerName = null;
    if (!string.IsNullOrEmpty(progress.HCReviewedById))
    {
        hcReviewerName = await _context.Users
            .Where(u => u.Id == progress.HCReviewedById)
            .Select(u => u.FullName ?? u.UserName)
            .FirstOrDefaultAsync();
    }

    return Json(new {
        success = true,
        id = progress.Id,
        deliverableName = progress.ProtonDeliverable?.NamaDeliverable,
        status = progress.Status,
        hcApprovalStatus = progress.HCApprovalStatus,
        evidenceFileName = progress.EvidenceFileName,
        evidencePath = progress.EvidencePath,
        submittedAt = progress.SubmittedAt?.ToString("dd MMM yyyy HH:mm"),
        approvedAt = progress.ApprovedAt?.ToString("dd MMM yyyy HH:mm"),
        rejectedAt = progress.RejectedAt?.ToString("dd MMM yyyy HH:mm"),
        rejectionReason = progress.RejectionReason,
        approvedByName = approverName,
        hcReviewedAt = progress.HCReviewedAt?.ToString("dd MMM yyyy HH:mm"),
        hcReviewedByName = hcReviewerName
    });
}
```

### Anti-Patterns to Avoid

- **Loading all ProtonDeliverableProgress rows globally:** The Override tab must always have Bagian+Unit+Track filter applied before loading data. Unfiltered loads can be thousands of rows. Guard the GET: if no filter selected, show empty state.
- **Inline status badge as form submit:** Badges should trigger an AJAX call, not a form post — the override modal must appear first to collect AlasanOverride before any save.
- **Removing IsAccessible from DeliverableViewModel without checking views:** The `IsAccessible` field is used in `Views/CDP/Deliverable.cshtml` to show/hide the upload action. After lock removal, set `IsAccessible = true` unconditionally in the ViewModel rather than deleting the field — safer refactor with no view changes needed.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Timestamp auto-fill on status change | Custom timestamp manager | Inline switch statement in OverrideSave | Simple enough, no abstraction needed |
| User name lookup for approver display | N+1 queries per record | Single dictionary batch load or inline nullable string query | Project pattern from Phase 50 |
| Anti-CSRF for AJAX POST | Custom token extraction | RequestVerificationToken header (existing pattern from Phase 51 GuidanceDelete) | Already established |
| Data island for override tab | Full server render | AJAX-on-click OverrideDetail endpoint | Table data for override is large + dynamic; badge click loads one record at a time |

**Key insight:** The Override tab filter cascade data (Bagian/Unit/Track dropdowns) can reuse the same `orgStructure` JS variable and `allTracks` ViewBag data already passed to the Index view by the existing GET action — no additional server data needed for the filter dropdowns. Only the table rows need a controller action.

## Common Pitfalls

### Pitfall 1: Filter State Not Preserved When Switching Tabs
**What goes wrong:** User selects Bagian/Unit/Track in Override tab, clicks a badge, saves override — page reloads or AJAX completes, but active tab resets to Silabus.
**Why it happens:** Bootstrap tabs default to first tab on page load. If save triggers `window.location.reload()`, tab state is lost.
**How to avoid:** Use AJAX-only save (no page reload). On success, refresh only the override table rows in the current tab via AJAX re-fetch. If reload is chosen as Claude's discretion, pass `?activeTab=override` query param and use JS to activate the correct tab on load.
**Warning signs:** If you find yourself doing `window.location.reload()` in the override save success handler.

### Pitfall 2: Locked DB Records Cause Display Inconsistency After Lock Removal
**What goes wrong:** Code removes "Locked" from status dropdowns but old DB records still have Status="Locked". The override table shows badges with no color match, or the status dropdown can't be set to the current status.
**Why it happens:** The data migration is forgotten or runs after the code deploy.
**How to avoid:** Include the SQL migration `UPDATE ProtonDeliverableProgresses SET Status='Active' WHERE Status='Locked'` in the EF migration that accompanies the code change.
**Warning signs:** Any badge in the override table showing an unrecognized status value.

### Pitfall 3: ProtonDeliverableProgress Unique Index Violation
**What goes wrong:** Attempting to create duplicate progress records when AssignTrack is called for an existing coachee.
**Why it happens:** The existing AssignTrack already deletes existing progress records before creating new ones — this is correct. But if the migration or lock removal script is run separately and then AssignTrack is called, there could be orphan records.
**How to avoid:** Verify the delete-before-create block in AssignTrack is intact when modifying it. The unique index on `(CoacheeId, ProtonDeliverableId)` in ApplicationDbContext will catch duplicates at the DB layer.

### Pitfall 4: Removing allProgresses Load from ApproveDeliverable() Breaks all-approved Check
**What goes wrong:** Removing the unlock-next block in ApproveDeliverable() also accidentally removes the `allProgresses` + `orderedProgresses` computation, but `allApproved` still uses `orderedProgresses`.
**Why it happens:** The `bool allApproved = orderedProgresses.All(p => p.Status == "Approved")` check needs `orderedProgresses` to still exist.
**How to avoid:** Keep the allProgresses load and orderedProgresses computation for the `allApproved` check; only remove the "unlock next" block. Or simplify: replace `orderedProgresses.All(p => p.Status == "Approved")` with a direct DB query `!await _context.ProtonDeliverableProgresses.AnyAsync(p => p.CoacheeId == progress.CoacheeId && p.Status != "Approved")`.

### Pitfall 5: OverrideList Data Shape — Deliverables vs Progresses Mismatch
**What goes wrong:** Not all coachees have a ProtonDeliverableProgress record for every deliverable in the scope. If a coachee was assigned the track but some progress records are missing, the badge grid has gaps.
**Why it happens:** Track assignment + progress creation is atomic (AssignTrack creates all records), but manual DB changes or edge cases could produce partial records.
**How to avoid:** In the override table, check `progress != null` before rendering a badge. Render a grey "—" placeholder for missing records. Document this as expected behavior.

## Code Examples

Verified patterns from existing codebase:

### AuditLog Call Pattern (from ProtonDataController)
```csharp
// Source: Controllers/ProtonDataController.cs — GuidanceUpload
await _auditLog.LogAsync(user.Id, user.FullName, "Upload",
    $"Uploaded guidance file '{file.FileName}' ({FormatFileSize(file.Length)}) for {bagian}/{unit}/Track {trackId}",
    targetId: record.Id, targetType: "CoachingGuidanceFile");

// Phase 52 equivalent:
await _auditLog.LogAsync(user.Id, user.FullName ?? user.UserName ?? user.Id, "Override",
    $"Override deliverable progress #{progress.Id}: {oldStatus} → {req.NewStatus}. Alasan: {req.OverrideReason}",
    targetId: progress.Id, targetType: "ProtonDeliverableProgress");
```

### AJAX POST with AntiForgeryToken (from ProtonData/Index.cshtml — GuidanceDelete)
```javascript
// Source: Views/ProtonData/Index.cshtml — btnConfirmGuidanceDelete handler
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
const resp = await fetch('/ProtonData/GuidanceDelete', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
    body: JSON.stringify({ id: guidanceDeleteId })
});
const data = await resp.json();
if (data.success) {
    // handle success
}
```

### Bootstrap Tab Navigation JS (for preserving active tab after AJAX)
```javascript
// Activate override tab programmatically (after success, stay on override tab)
const overrideTab = document.getElementById('override-tab');
if (overrideTab) {
    bootstrap.Tab.getInstance(overrideTab)?.show() ?? new bootstrap.Tab(overrideTab).show();
}
```

### Badge Click → Modal Population (pattern used in Phase 50 CoachCoacheeMapping edit modal)
```javascript
// Source pattern: Views/Admin/CoachCoacheeMapping.cshtml — edit modal
document.addEventListener('click', async function(e) {
    const btn = e.target.closest('.btn-override');
    if (!btn) return;
    const progressId = btn.dataset.progressId;

    // Fetch detail
    const resp = await fetch(`/ProtonData/OverrideDetail?id=${progressId}`);
    const data = await resp.json();
    if (!data.success) { alert('Gagal memuat data.'); return; }

    // Populate modal fields
    document.getElementById('overrideProgressId').value = data.id;
    document.getElementById('overrideCurrentStatus').textContent = data.status;
    document.getElementById('overrideStatusSelect').value = data.status;
    document.getElementById('overrideHCStatusSelect').value = data.hcApprovalStatus;
    document.getElementById('overrideRejectionReason').value = data.rejectionReason ?? '';
    document.getElementById('overrideAlasan').value = '';
    // ... populate other fields

    new bootstrap.Modal(document.getElementById('overrideModal')).show();
});
```

### DTO at namespace level (Phase 47+ pattern — required for EF/JSON binding)
```csharp
// Source: Controllers/ProtonDataController.cs — GuidanceDeleteRequest
// DTOs are declared OUTSIDE the class, inside the namespace block

namespace HcPortal.Controllers
{
    public class OverrideSaveRequest    // <-- outside ProtonDataController class
    {
        public int ProgressId { get; set; }
        public string NewStatus { get; set; } = "";
        public string NewHCStatus { get; set; } = "";
        public string? NewRejectionReason { get; set; }
        public string OverrideReason { get; set; } = "";
    }

    [Authorize(Roles = "Admin,HC")]
    public class ProtonDataController : Controller { ... }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Sequential lock (first Active, rest Locked) | All Active on assignment | Phase 52 | Simplifies CoacheeProgressRow, removes chart label, removes unlock-next block from ApproveDeliverable() |
| Locked status in status counts | 4-status counts only (Approved/Submitted/Active/Rejected) | Phase 52 | CDPController BuildProtonProgressSubModelAsync simplification, chart color palette drops 5th color |

**Deprecated/outdated after Phase 52:**
- `CoacheeProgressRow.Locked` property in CDPDashboardViewModel — remove after migration
- Sequential lock check block in CDPController.Deliverable() lines 817-830 — remove entirely
- Unlock-next block in CDPController.ApproveDeliverable() lines 930-939 — remove entirely
- `Status = index == 0 ? "Active" : "Locked"` in CDPController.AssignTrack() line ~743 — change to `"Active"` always

## Open Questions

1. **OverrideList GET — pagination or client-side scroll?**
   - What we know: Per-unit scope keeps rows small (typically 10-30 coachees × N deliverables badges)
   - What's unclear: Whether full table fits without pagination; deliverable count per track varies
   - Recommendation: No pagination for Phase 52. If a unit has 30 coachees × 20 deliverables = 600 badge cells, that's still manageable in a horizontally-scrolling table. Add a note to consider pagination in future if performance degrades.

2. **ApproveDeliverable() — remove allProgresses load entirely or keep for allApproved check?**
   - What we know: The allProgresses + orderedProgresses computation serves two purposes: (1) unlock-next, (2) allApproved check
   - What's unclear: Whether the allApproved check is still needed after lock removal
   - Recommendation: Keep the allApproved check (triggers HC notification when all deliverables are approved — this functionality should remain). Simplify: remove the orderedProgresses sort and just check `_context.ProtonDeliverableProgresses.Where(p => p.CoacheeId == ... && p.Status != "Approved").AnyAsync()`.

3. **Override tab filter — same GET param approach as Silabus tab or pure AJAX?**
   - What we know: Silabus tab uses `?bagian=&unit=&trackId=` GET params (page reload on filter). Guidance tab uses AJAX (no page reload on filter). Override context decision.
   - What's unclear: Whether page reload for Override is acceptable UX or if AJAX is needed
   - Recommendation: Use AJAX like the Guidance tab. Override table is loaded asynchronously after filter button click. This avoids tab state loss on page reload.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (project pattern — no automated test framework detected) |
| Config file | none |
| Quick run command | Manual browser test |
| Full suite command | Manual browser test |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| OPER-03 | Admin can view ProtonDeliverableProgress records in Override tab | manual | N/A | ❌ Wave 0 |
| OPER-03 | Admin can click badge and see full override modal with record context | manual | N/A | ❌ Wave 0 |
| OPER-03 | Override saves with correct status+timestamps+AuditLog entry | manual | N/A | ❌ Wave 0 |
| OPER-03 | HC role also has access (same as Admin) | manual | N/A | ❌ Wave 0 |
| OPER-03 | Locked status removed — AssignTrack creates all Active | manual | N/A | ❌ Wave 0 |
| OPER-03 | Existing Locked DB records migrated to Active | manual via DB check | N/A | ❌ Wave 0 |

### Wave 0 Gaps
- No automated test infrastructure detected (no `*.test.cs`, no `pytest.ini`, no `jest.config.*`)
- All validation via manual browser UAT as established in project pattern

## Sources

### Primary (HIGH confidence)
- Direct codebase inspection — `Controllers/ProtonDataController.cs`, `Controllers/CDPController.cs` (lines verified)
- Direct codebase inspection — `Models/ProtonModels.cs` (ProtonDeliverableProgress fields confirmed)
- Direct codebase inspection — `Models/CDPDashboardViewModel.cs` (CoacheeProgressRow.Locked confirmed)
- Direct codebase inspection — `Views/ProtonData/Index.cshtml` (tab pattern, JS AJAX patterns confirmed)
- Direct codebase inspection — `Data/ApplicationDbContext.cs` (DbSet names, unique index on CoacheeId+DeliverableId confirmed)
- Direct codebase inspection — `Services/AuditLogService.cs` (LogAsync signature confirmed)

### Secondary (MEDIUM confidence)
- Phase 50 plan patterns (CoachCoacheeMapping modal AJAX) — verified matches existing ProtonDataController patterns
- Phase 51 plan patterns (tab structure, GuidanceDelete AJAX) — directly applicable

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project, no new dependencies
- Architecture: HIGH — all patterns verified from existing working code in same controller/view
- Pitfalls: HIGH — identified from actual code inspection of the specific lines to be modified

**Research date:** 2026-02-27
**Valid until:** 2026-03-27 (stable codebase, 30 days)
