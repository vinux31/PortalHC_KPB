# Phase 6: Approval Workflow & Completion - Research

**Researched:** 2026-02-17
**Domain:** ASP.NET Core MVC — multi-role approval workflow, in-app notifications, final assessment creation, competency level update
**Confidence:** HIGH (based entirely on direct codebase inspection; Phase 5 is complete and verified)

---

## Summary

Phase 6 closes the Proton loop: deliverables move from "Submitted" through SrSpv/SectionHead approval, HC review, and finally a formal Proton Assessment that updates the coachee's `UserCompetencyLevel`. The data foundation from Phase 5 (`ProtonDeliverableProgress`, status values "Locked/Active/Submitted/Approved/Rejected") is complete and running. Phase 6 extends the existing `ProtonDeliverableProgress` model with approval metadata fields and adds two new tables: `ProtonNotification` for HC in-app notifications and `ProtonFinalAssessment` for the final assessment record.

The approval architecture is deliberately simple: SrSpv and SectionHead act independently — either approving alone is sufficient to advance the coachee (APPRV-02/APPRV-03). This means there is NO multi-approval state machine; the first approval (by whichever approver) transitions status from "Submitted" to "Approved". HC review is a separate, non-blocking channel tracked via a dedicated `HCApprovalStatus` column on the progress record (NOT the main `Status` column). HC must complete all pending `HCApprovalStatus="Pending"` reviews before PROTN-07 allows creating the final Proton Assessment.

APPRV-01 ("Coach can submit a deliverable for approval") is architecturally already satisfied by Phase 5's `UploadEvidence` action, which transitions status to "Submitted". Phase 6 does NOT need to re-implement submission — it implements the review side only (approver actions + HC review + notification + final assessment).

**Primary recommendation:** Three new DB columns on `ProtonDeliverableProgress` (RejectionReason, ApprovedById, HCApprovalStatus), two new tables (ProtonNotification, ProtonFinalAssessment), and four new controller actions in `CDPController`: `ApproveDeliverable`, `RejectDeliverable`, `HCReviewDeliverable`, `CreateFinalAssessment`. All approval actions exist as POST-only with `[ValidateAntiForgeryToken]`. No new NuGet packages.

---

## Standard Stack

### Core (already installed — no additions needed)
| Component | Version | Purpose | Notes |
|-----------|---------|---------|-------|
| ASP.NET Core MVC | .NET 8.0 | Controller/View framework | TargetFramework: net8.0 |
| EF Core SqlServer | 8.0.0 | ORM + migrations | Already in csproj |
| EF Core Tools | 8.0.0 | `dotnet ef migrations add` | Already in csproj |
| ASP.NET Core Identity | 8.0.0 | Auth + role checking | ApplicationUser.RoleLevel confirmed |
| Bootstrap 5 | CDN via _Layout.cshtml | UI framework | All views use Bootstrap 5 |
| Bootstrap Icons | CDN | Icon library | `bi bi-*` pattern used throughout |
| TempData | ASP.NET Core built-in | Success/Error feedback | Existing pattern across all controllers |

### No New Packages Required
Phase 6 does not need any new NuGet packages. The approval workflow is pure EF Core + ASP.NET Core MVC with Bootstrap modals for UI.

---

## Architecture Patterns

### What Phase 5 Already Built (Do NOT Re-implement)
| Existing | Status | Notes |
|----------|--------|-------|
| `ProtonDeliverableProgress.Status` = "Submitted" | Done | `UploadEvidence` POST already sets this — APPRV-01 covered |
| `ProtonDeliverableProgress.RejectedAt` | Done | Field exists in schema already |
| Sequential lock check (previous must be Approved) | Done | `Deliverable` GET already enforces this |
| Role-based access to `Deliverable` page | Done | `Deliverable` GET checks section membership for coaches |

### New DB Changes (One Migration)

Phase 6 adds **3 columns to `ProtonDeliverableProgresses`** and **2 new tables**:

```
ProtonDeliverableProgresses (ALTER — add columns):
  + RejectionReason     nvarchar(max) nullable  — written reason when rejected (APPRV-05)
  + ApprovedById        nvarchar(max) nullable  — string user ID of the approver (no FK — matches pattern)
  + HCApprovalStatus    nvarchar(50)  not null default 'Pending'  — "Pending" or "Reviewed" (APPRV-04)
  + HCReviewedAt        datetime2     nullable  — when HC reviewed
  + HCReviewedById      nvarchar(max) nullable  — HC user ID who reviewed

ProtonNotification (new table):
  Id                    int PK identity
  RecipientId           nvarchar(450) — HC user ID (no FK — matches pattern)
  CoacheeId             nvarchar(max) — coachee who completed all deliverables
  CoacheeName           nvarchar(max) — denormalized display name (avoids extra JOIN on read)
  Message               nvarchar(max) — notification text
  Type                  nvarchar(50)  — "AllDeliverablesComplete" (extensible)
  IsRead                bit           not null default 0
  CreatedAt             datetime2     — when notification was created
  ReadAt                datetime2     nullable — when HC marked as read

ProtonFinalAssessment (new table):
  Id                    int PK identity
  CoacheeId             nvarchar(450) — coachee (no FK — matches pattern)
  CreatedById           nvarchar(max) — HC user ID who created (no FK — matches pattern)
  ProtonTrackAssignmentId int         — FK to ProtonTrackAssignments (Restrict) — links to track
  Status                nvarchar(50)  — "Draft", "Completed" (extensible for future states)
  CompetencyLevelGranted int          — the level awarded (0-5, same as UserCompetencyLevel.CurrentLevel)
  Notes                 nvarchar(max) nullable — HC's assessment notes
  CreatedAt             datetime2
  CompletedAt           datetime2 nullable
```

### Recommended Project Structure (Phase 6 additions)
```
Controllers/
└── CDPController.cs         # ADD 4 new actions:
                             #   ApproveDeliverable (POST)
                             #   RejectDeliverable (POST)
                             #   HCReviewDeliverable (POST)
                             #   HCApprovals (GET) — HC review queue
                             #   CreateFinalAssessment (GET + POST)
                             # MODIFY existing Deliverable GET for HC/approver role logic

Models/
└── ProtonModels.cs          # ADD ProtonNotification, ProtonFinalAssessment classes
                             # MODIFY ProtonDeliverableProgress (3 new fields)
└── ProtonViewModels.cs      # ADD ApprovalQueueViewModel, HCApprovalQueueViewModel,
                             #     FinalAssessmentViewModel

Data/
└── ApplicationDbContext.cs  # ADD ProtonNotifications, ProtonFinalAssessments DbSets
                             # ADD EF config for ProtonFinalAssessment FK

Migrations/
└── YYYYMMDDHHMMSS_AddApprovalWorkflow.cs  # NEW — one migration

Views/CDP/
├── Deliverable.cshtml       # EXTEND — add approval action buttons for SrSpv/SectionHead;
                             #          add rejection notice with RejectionReason display (APPRV-06);
                             #          add HC review button for HC role
├── HCApprovals.cshtml       # NEW — HC's queue of submitted deliverables needing review
├── CreateFinalAssessment.cshtml  # NEW — HC form to create final Proton Assessment
└── PlanIdp.cshtml           # EXTEND Coachee branch — show final assessment status (PROTN-08)
```

### Pattern 1: Approval Action (SrSpv / SectionHead)
**What:** A POST action that transitions `ProtonDeliverableProgress.Status` from "Submitted" to "Approved" and unlocks the next deliverable. Either approver alone is sufficient (APPRV-02/APPRV-03).
**When to use:** Approver clicks "Setujui" button on the `Deliverable` page.
**Critical:** After approving, unlock the NEXT deliverable in sequence by finding its progress record and setting Status="Active".
```csharp
// Source: CDPController.cs existing UploadEvidence POST pattern + sequential unlock logic
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveDeliverable(int progressId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    // Only SrSpv (RoleLevel 4) or SectionHead (RoleLevel 4) can approve
    bool canApprove = userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead;
    if (!canApprove) return Forbid();

    var progress = await _context.ProtonDeliverableProgresses
        .Include(p => p.ProtonDeliverable)
            .ThenInclude(d => d.ProtonSubKompetensi)
                .ThenInclude(s => s.ProtonKompetensi)
        .FirstOrDefaultAsync(p => p.Id == progressId);
    if (progress == null) return NotFound();

    // Only "Submitted" deliverables can be approved
    if (progress.Status != "Submitted")
    {
        TempData["Error"] = "Hanya deliverable berstatus Submitted yang dapat disetujui.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Approve
    progress.Status = "Approved";
    progress.ApprovedAt = DateTime.UtcNow;
    progress.ApprovedById = user.Id;

    // Unlock next deliverable — load all progress for this coachee in track order
    var allProgresses = await _context.ProtonDeliverableProgresses
        .Include(p => p.ProtonDeliverable)
            .ThenInclude(d => d.ProtonSubKompetensi)
                .ThenInclude(s => s.ProtonKompetensi)
        .Where(p => p.CoacheeId == progress.CoacheeId)
        .ToListAsync();

    var trackType = progress.ProtonDeliverable!.ProtonSubKompetensi!.ProtonKompetensi!.TrackType;
    var tahunKe = progress.ProtonDeliverable.ProtonSubKompetensi.ProtonKompetensi.TahunKe;

    var orderedProgresses = allProgresses
        .Where(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.TrackType == trackType
                 && p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.TahunKe == tahunKe)
        .OrderBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.Urutan ?? 0)
        .ThenBy(p => p.ProtonDeliverable?.ProtonSubKompetensi?.Urutan ?? 0)
        .ThenBy(p => p.ProtonDeliverable?.Urutan ?? 0)
        .ToList();

    int approvedIndex = orderedProgresses.FindIndex(p => p.Id == progressId);
    if (approvedIndex >= 0 && approvedIndex + 1 < orderedProgresses.Count)
    {
        var nextProgress = orderedProgresses[approvedIndex + 1];
        if (nextProgress.Status == "Locked")
        {
            nextProgress.Status = "Active";
        }
    }

    await _context.SaveChangesAsync();

    // Check: if ALL progresses for this coachee+track are now Approved, trigger HC notification
    bool allApproved = orderedProgresses.All(p =>
        p.Id == progressId ? true : p.Status == "Approved");
    if (allApproved)
    {
        await CreateHCNotificationAsync(progress.CoacheeId);
    }

    TempData["Success"] = "Deliverable berhasil disetujui.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

### Pattern 2: Reject with Reason (APPRV-05)
**What:** POST action setting Status="Rejected" with a written reason. Clears ApprovedAt and sets RejectedAt.
**When to use:** Approver clicks "Tolak" and submits the rejection reason form.
**Critical:** The rejection reason must be stored and visible to BOTH coach and coachee (APPRV-06) — this is shown in the `Deliverable` view for all roles that can see the record.
```csharp
// Source: same approval pattern above + CDPController existing TempData feedback
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RejectDeliverable(int progressId, string rejectionReason)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    bool canReject = userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead;
    if (!canReject) return Forbid();

    if (string.IsNullOrWhiteSpace(rejectionReason))
    {
        TempData["Error"] = "Alasan penolakan wajib diisi.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    var progress = await _context.ProtonDeliverableProgresses
        .FirstOrDefaultAsync(p => p.Id == progressId);
    if (progress == null) return NotFound();

    if (progress.Status != "Submitted")
    {
        TempData["Error"] = "Hanya deliverable berstatus Submitted yang dapat ditolak.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    progress.Status = "Rejected";
    progress.RejectedAt = DateTime.UtcNow;
    progress.RejectionReason = rejectionReason;
    progress.ApprovedById = null;  // Clear if previously set

    await _context.SaveChangesAsync();

    TempData["Success"] = "Deliverable berhasil ditolak.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

### Pattern 3: HC Review Action (APPRV-04)
**What:** HC marks a "Submitted" deliverable as reviewed (sets HCApprovalStatus="Reviewed"). This is NOT a blocking gate — it does NOT change the main `Status` column. HC uses a dedicated queue page (`HCApprovals`) to process pending reviews.
**When to use:** HC clicks "Tandai Sudah Diperiksa" on their review queue or on the deliverable page.
```csharp
// Source: same POST pattern
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> HCReviewDeliverable(int progressId)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    if (userRole != UserRoles.HC) return Forbid();

    var progress = await _context.ProtonDeliverableProgresses
        .FirstOrDefaultAsync(p => p.Id == progressId);
    if (progress == null) return NotFound();

    // HC can review any status (Submitted, Approved, Rejected) that has HCApprovalStatus=Pending
    progress.HCApprovalStatus = "Reviewed";
    progress.HCReviewedAt = DateTime.UtcNow;
    progress.HCReviewedById = user.Id;

    await _context.SaveChangesAsync();

    TempData["Success"] = "Deliverable telah ditandai sebagai sudah diperiksa HC.";
    return RedirectToAction("HCApprovals");
}
```

### Pattern 4: HC Notification — In-App (PROTN-06)
**What:** When all deliverables in a track are approved (after the last `ApproveDeliverable`), create a `ProtonNotification` record for all HC users. HC sees these notifications from the `HCApprovals` page (polled on page load — no real-time SignalR per v2 requirements).
**When to use:** Called internally after `ApproveDeliverable` determines all progresses are now Approved.
```csharp
// Source: no existing notification model — new pattern for Phase 6
// Called as a private helper from ApproveDeliverable
private async Task CreateHCNotificationAsync(string coacheeId)
{
    var coachee = await _context.Users
        .Where(u => u.Id == coacheeId)
        .Select(u => new { u.FullName, u.UserName })
        .FirstOrDefaultAsync();
    var coacheeName = coachee?.FullName ?? coacheeId;

    // Find all HC users
    var hcUsers = await _userManager.GetUsersInRoleAsync(UserRoles.HC);

    var notifications = hcUsers.Select(hc => new ProtonNotification
    {
        RecipientId = hc.Id,
        CoacheeId = coacheeId,
        CoacheeName = coacheeName,
        Message = $"{coacheeName} telah menyelesaikan semua deliverable Proton.",
        Type = "AllDeliverablesComplete",
        IsRead = false,
        CreatedAt = DateTime.UtcNow
    }).ToList();

    _context.ProtonNotifications.AddRange(notifications);
    await _context.SaveChangesAsync();
}
```

### Pattern 5: HC Approvals Queue Page (GET)
**What:** HC sees two lists on `HCApprovals` GET: (1) pending HC reviews (HCApprovalStatus="Pending" and Status="Submitted"/"Approved"/"Rejected"), and (2) unread notifications (AllDeliverablesComplete type).
**When to use:** PROTN-06 — HC visits `HCApprovals` to process their queue.
**URL:** `/CDP/HCApprovals`
```csharp
// Source: CDPController ProtonMain GET pattern
public async Task<IActionResult> HCApprovals()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    if (userRole != UserRoles.HC) return Forbid();

    // Pending HC reviews — deliverables not yet reviewed by HC
    var pendingReviews = await _context.ProtonDeliverableProgresses
        .Include(p => p.ProtonDeliverable)
            .ThenInclude(d => d.ProtonSubKompetensi)
                .ThenInclude(s => s.ProtonKompetensi)
        .Where(p => p.HCApprovalStatus == "Pending"
                 && (p.Status == "Submitted" || p.Status == "Approved" || p.Status == "Rejected"))
        .OrderBy(p => p.SubmittedAt)
        .ToListAsync();

    // Unread notifications for this HC user
    var notifications = await _context.ProtonNotifications
        .Where(n => n.RecipientId == user.Id && !n.IsRead)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    // Coachee names for display (batch query — avoids N+1)
    var coacheeIds = pendingReviews.Select(p => p.CoacheeId).Distinct().ToList();
    var userNames = await _context.Users
        .Where(u => coacheeIds.Contains(u.Id))
        .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? u.Id);

    var viewModel = new HCApprovalQueueViewModel
    {
        PendingReviews = pendingReviews,
        Notifications = notifications,
        UserNames = userNames
    };

    return View(viewModel);
}
```

### Pattern 6: Final Proton Assessment Creation (PROTN-07)
**What:** HC can create a `ProtonFinalAssessment` only after ALL pending HC reviews for a coachee are marked "Reviewed". The creation form is pre-populated with the coachee's track. On POST, creates the `ProtonFinalAssessment` record and updates `UserCompetencyLevel` for the relevant competencies.
**Guard condition:** Before allowing HC to create the final assessment, check that no `ProtonDeliverableProgress` records for this coachee have `HCApprovalStatus="Pending"`.
```csharp
// Source: CDPController Deliverable pattern + CMPController CreateAssessment pattern (Authorize HC)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreateFinalAssessment(int trackAssignmentId, int competencyLevelGranted, string? notes)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    if (roles.FirstOrDefault() != UserRoles.HC) return Forbid();

    var assignment = await _context.ProtonTrackAssignments
        .FirstOrDefaultAsync(a => a.Id == trackAssignmentId && a.IsActive);
    if (assignment == null) return NotFound();

    // Guard: no pending HC reviews for this coachee
    bool hasPendingHCReviews = await _context.ProtonDeliverableProgresses
        .AnyAsync(p => p.CoacheeId == assignment.CoacheeId
                    && p.HCApprovalStatus == "Pending"
                    && (p.Status == "Submitted" || p.Status == "Approved"));
    if (hasPendingHCReviews)
    {
        TempData["Error"] = "Selesaikan semua review HC sebelum membuat final assessment.";
        return RedirectToAction("CreateFinalAssessment", new { trackAssignmentId });
    }

    var finalAssessment = new ProtonFinalAssessment
    {
        CoacheeId = assignment.CoacheeId,
        CreatedById = user.Id,
        ProtonTrackAssignmentId = trackAssignmentId,
        Status = "Completed",
        CompetencyLevelGranted = competencyLevelGranted,
        Notes = notes,
        CreatedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow
    };
    _context.ProtonFinalAssessments.Add(finalAssessment);

    // Update UserCompetencyLevel — "Proton" source
    // NOTE: UserCompetencyLevel uses KkjMatrixItemId — the Proton track maps to a general competency
    // For Phase 6: create or update a single competency record using a designated KKJ item
    // (See Open Question #2 — exact KkjMatrixItemId mapping TBD)

    await _context.SaveChangesAsync();

    TempData["Success"] = "Final Proton Assessment berhasil dibuat.";
    return RedirectToAction("HCApprovals");
}
```

### Pattern 7: Coachee Proton View — Final Assessment Status (PROTN-08)
**What:** The existing `PlanIdp.cshtml` Coachee branch (`@if (ViewBag.IsProtonView == true)`) is extended to show the final assessment status and resulting competency level update when `ProtonFinalAssessment` exists for the coachee.
**When to use:** After HC creates `ProtonFinalAssessment`, the coachee sees a new "Final Assessment" section at the bottom of their PlanIdp view.
**Controller change:** `PlanIdp` GET for Coachee path additionally queries `ProtonFinalAssessments` for the coachee and passes it via `ProtonPlanViewModel.FinalAssessment`.
```csharp
// In CDPController.PlanIdp() — add to the Coachee path:
var finalAssessment = await _context.ProtonFinalAssessments
    .Where(fa => fa.CoacheeId == user.Id)
    .OrderByDescending(fa => fa.CreatedAt)
    .FirstOrDefaultAsync();

// Add FinalAssessment property to ProtonPlanViewModel
var protonViewModel = new ProtonPlanViewModel
{
    TrackType = assignment.TrackType,
    TahunKe = assignment.TahunKe,
    KompetensiList = kompetensiList,
    ActiveProgress = activeProgress,
    FinalAssessment = finalAssessment  // NEW
};
```

### Pattern 8: Deliverable Page — Role-Conditional Approval Buttons
**What:** The existing `Deliverable.cshtml` is extended with conditional approval/rejection forms. The view must show different actions based on user role:
- **SrSpv / SectionHead**: Approve and Reject buttons when Status="Submitted"
- **HC**: "Tandai Sudah Diperiksa" button when HCApprovalStatus="Pending"
- **Coach / Coachee**: Read-only rejection reason display when Status="Rejected"
**ViewBag flags needed:**
- `ViewBag.CanApprove` (bool) — true for SrSpv/SectionHead
- `ViewBag.CanHCReview` (bool) — true for HC and HCApprovalStatus="Pending"
- `ViewBag.UserRole` — already set in CDPController; extend Deliverable GET to set these flags

### Anti-Patterns to Avoid
- **Requiring BOTH SrSpv AND SectionHead to approve:** Requirements APPRV-02/APPRV-03 explicitly state either one alone is sufficient. A multi-step approval state machine would violate this. One approval transitions to "Approved" immediately.
- **Blocking HC approval in the per-deliverable main `Status` column:** HC review must be tracked in a SEPARATE column (`HCApprovalStatus`). Using the main `Status` column for HC would block sequential lock advancement (which only cares about SrSpv/SectionHead approval per PROTN-03).
- **N+1 on unlock-next in ApproveDeliverable:** Load ALL progress records for the coachee in ONE query — same pattern as `Deliverable` GET in Phase 5.
- **Calling `_userManager.GetUsersInRoleAsync()` inside a request loop:** Call once and batch — HC users are few but this is still a best practice.
- **Allowing final assessment creation before all HC reviews are done:** The guard condition in `CreateFinalAssessment` must check `HCApprovalStatus="Pending"` before proceeding. Do not rely on the UI disabling the button — enforce server-side.
- **Storing the notification count in a ViewBag for all pages:** Notifications are on-demand (HC visits `HCApprovals`) per v1.1 scope. Do NOT add a notification badge to every page — that requires a DB query per request. Defer to Phase 7 or a navbar-level query if needed.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Multi-step approval state machine | Custom state machine for two-approver consensus | Single-approver immediate transition | Requirements say either approver alone is sufficient |
| Email notification | SMTP email sending | `ProtonNotification` table (in-app) | v2 requirement only (NOTF-01/02 are v2); in-app notification is v1.1 PROTN-06 |
| Real-time notification badge | SignalR hub + client JS | Polling on page load when HC visits HCApprovals | v2 requirement; out of scope per REQUIREMENTS.md |
| Custom authorization attribute | `[Authorize(Policy="ApproverPolicy")]` | Role-string checks in controller (`userRole == UserRoles.SrSupervisor`) | Matches existing CDPController pattern; no custom policies configured |
| Competency level update service | A service class or background job | Direct EF Core upsert in `CreateFinalAssessment` POST | Existing `UserCompetencyLevel` pattern (Phase 3) already does direct upsert — same approach |

**Key insight:** The HC notification system is a simple DB-polled queue. Real-time push (SignalR) is explicitly out of scope in `REQUIREMENTS.md` ("Real-time notifications (SignalR): In-page polling sufficient for v1.1"). The `ProtonNotification` table is a lightweight read-on-demand solution.

---

## Common Pitfalls

### Pitfall 1: Approval Accidentally Blocks Sequential Lock
**What goes wrong:** The approval implementation modifies the main `Status` column for HC reviews, so a deliverable stuck at HC review (HCApprovalStatus="Pending") cannot be approved by SrSpv/SectionHead because Status is in the wrong state.
**Why it happens:** Conflating the two approval channels — SrSpv/SectionHead approval (main Status) and HC review (HCApprovalStatus) — into one column.
**How to avoid:** Use a SEPARATE column `HCApprovalStatus` ("Pending"/"Reviewed") exclusively for HC tracking. The main `Status` ("Submitted" → "Approved" → "Rejected") is driven ONLY by SrSpv/SectionHead actions.
**Warning signs:** Coachee cannot advance to next deliverable even though SrSpv approved; or HC review seems to change the main status badge.

### Pitfall 2: "Unlock Next" Logic Not Running After Approval
**What goes wrong:** Approver approves deliverable N, but deliverable N+1 stays "Locked" forever.
**Why it happens:** `ApproveDeliverable` POST updates the current record but doesn't set the next record's Status from "Locked" to "Active".
**How to avoid:** After updating `progress.Status = "Approved"`, reload ALL ordered progresses for the coachee+track, find the current index, check the next record, and set it to "Active" if it's currently "Locked". This is the inverse of the sequential lock check already in `Deliverable` GET (Phase 5).
**Warning signs:** Coachee's `PlanIdp` page always shows the same "Lanjut ke Deliverable Aktif" button even after approval; or shows "Semua deliverable telah selesai" prematurely.

### Pitfall 3: HC Final Assessment Created When Reviews Pending
**What goes wrong:** HC creates `ProtonFinalAssessment` before reviewing all deliverables, violating APPRV-04.
**Why it happens:** The `CreateFinalAssessment` form shows no warning and the server-side guard is missing.
**How to avoid:** In `CreateFinalAssessment` POST, check `_context.ProtonDeliverableProgresses.AnyAsync(p => p.CoacheeId == coacheeId && p.HCApprovalStatus == "Pending" && (p.Status == "Submitted" || p.Status == "Approved"))` before creating the assessment. Return TempData["Error"] and redirect back if pending items exist.
**Warning signs:** HC can reach the create form and submit even with pending reviews showing on `HCApprovals`.

### Pitfall 4: All-Deliverables-Complete Notification Fires Multiple Times
**What goes wrong:** Each time any delivery is re-approved (e.g., after resubmission), the "all approved" check fires again and inserts duplicate `ProtonNotification` records for HC.
**Why it happens:** The all-approved check in `ApproveDeliverable` does not check if a notification was already sent.
**How to avoid:** Before creating the notification, check `_context.ProtonNotifications.AnyAsync(n => n.CoacheeId == coacheeId && n.Type == "AllDeliverablesComplete")`. Only create if none exists. Or: use `AddRange` with a conditional guard.
**Warning signs:** HC sees duplicate notifications for the same coachee in `HCApprovals`.

### Pitfall 5: Rejection Reason Not Visible to Coachee (APPRV-06)
**What goes wrong:** The rejection reason is stored in the DB but not displayed in the `Deliverable.cshtml` view for the coachee role.
**Why it happens:** The view only showed the rejection alert (already in Phase 5) without the `RejectionReason` field text.
**How to avoid:** In `Deliverable.cshtml`, within the `@if (Model.Progress?.Status == "Rejected")` block, add a display of `Model.Progress.RejectionReason`. The view is accessible by both coach and coachee (access check in `Deliverable` GET already allows both).
**Warning signs:** Coachee sees "Deliverable ditolak" but cannot read why; must contact coach separately.

### Pitfall 6: ApprovedById / HCReviewedById Using FK Column
**What goes wrong:** If `ApprovedById` or `HCReviewedById` are configured as FK columns to `Users`, EF Core may generate a cascade path that conflicts with existing Restrict constraints.
**Why it happens:** EF Core infers FK from naming convention (`ApprovedBy` navigation property + `ApprovedById` column).
**How to avoid:** Do NOT add navigation properties for `ApprovedById`/`HCReviewedById`/`HCReviewedBy` in `ProtonDeliverableProgress`. Keep them as plain `string?` fields with no navigation property — consistent with CoachId/CoacheeId/AssignedById throughout the codebase. No DbContext FK configuration needed for these columns.
**Warning signs:** Migration generates FOREIGN KEY constraint on ApprovedById; `dotnet ef database update` fails with cascade path error.

### Pitfall 7: Deliverable GET Not Returning Approver Context
**What goes wrong:** `Deliverable.cshtml` cannot show approval buttons because `ViewBag.CanApprove` is not set by the controller.
**Why it happens:** The Phase 5 `Deliverable` GET action builds the viewModel with only `CanUpload` — it does not distinguish approver roles.
**How to avoid:** Extend the `Deliverable` GET to set additional ViewBag flags: `ViewBag.CanApprove = (userRole == UserRoles.SrSupervisor || userRole == UserRoles.SectionHead)`. Alternatively, add `CanApprove` and `CanHCReview` fields to `DeliverableViewModel` (cleaner than ViewBag).
**Warning signs:** Approver visits Deliverable page and sees no Approve/Reject buttons.

### Pitfall 8: ProtonFinalAssessment FK cascade conflict
**What goes wrong:** `dotnet ef database update` fails when `ProtonFinalAssessment.ProtonTrackAssignmentId` is configured with Cascade delete.
**Why it happens:** `ProtonTrackAssignment` → `ProtonFinalAssessment` creates a new cascade path. All Proton FK relationships use Restrict.
**How to avoid:** Configure `ProtonFinalAssessment` FK in `OnModelCreating` with `DeleteBehavior.Restrict` — consistent with Phase 5 pattern for all Proton relationships.

---

## Code Examples

Verified patterns from codebase inspection:

### ProtonDeliverableProgress — Extended Model (add 5 new fields)
```csharp
// Source: Models/ProtonModels.cs existing ProtonDeliverableProgress + Phase 6 additions
public class ProtonDeliverableProgress
{
    // ... existing fields unchanged ...
    public int Id { get; set; }
    public string CoacheeId { get; set; } = "";
    public int ProtonDeliverableId { get; set; }
    public ProtonDeliverable? ProtonDeliverable { get; set; }
    public string Status { get; set; } = "Locked";    // SrSpv/SectionHead approval channel
    public string? EvidencePath { get; set; }
    public string? EvidenceFileName { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ===== Phase 6 additions =====
    /// <summary>Written rejection reason (APPRV-05). Set by SrSpv or SectionHead.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>User ID of the approver (SrSpv or SectionHead) who approved. No FK — matches pattern.</summary>
    public string? ApprovedById { get; set; }

    /// <summary>HC review channel — independent of main Status. "Pending" or "Reviewed". (APPRV-04)</summary>
    public string HCApprovalStatus { get; set; } = "Pending";

    /// <summary>When HC marked as reviewed. Nullable — not set until reviewed.</summary>
    public DateTime? HCReviewedAt { get; set; }

    /// <summary>HC user ID who reviewed. No FK — matches pattern.</summary>
    public string? HCReviewedById { get; set; }
}
```

### ProtonNotification Model (new)
```csharp
// Source: new class in Models/ProtonModels.cs — follows ProtonTrackAssignment pattern (no FK)
public class ProtonNotification
{
    public int Id { get; set; }

    /// <summary>HC user ID who receives the notification. No FK — matches pattern.</summary>
    public string RecipientId { get; set; } = "";

    /// <summary>Coachee who completed all deliverables.</summary>
    public string CoacheeId { get; set; } = "";

    /// <summary>Denormalized display name — avoids JOIN on notification reads.</summary>
    public string CoacheeName { get; set; } = "";

    /// <summary>Human-readable notification text.</summary>
    public string Message { get; set; } = "";

    /// <summary>Notification category. "AllDeliverablesComplete" for PROTN-06.</summary>
    public string Type { get; set; } = "";

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
```

### ProtonFinalAssessment Model (new)
```csharp
// Source: new class in Models/ProtonModels.cs — FK to ProtonTrackAssignment with Restrict
public class ProtonFinalAssessment
{
    public int Id { get; set; }

    /// <summary>Coachee who received the assessment. No FK — matches pattern.</summary>
    public string CoacheeId { get; set; } = "";

    /// <summary>HC user who created the assessment. No FK — matches pattern.</summary>
    public string CreatedById { get; set; } = "";

    /// <summary>FK to ProtonTrackAssignment — identifies which track this assessment covers.</summary>
    public int ProtonTrackAssignmentId { get; set; }
    public ProtonTrackAssignment? ProtonTrackAssignment { get; set; }

    /// <summary>Assessment status: "Draft" or "Completed".</summary>
    public string Status { get; set; } = "Completed";

    /// <summary>Competency level granted (0-5, same scale as UserCompetencyLevel.CurrentLevel).</summary>
    public int CompetencyLevelGranted { get; set; }

    /// <summary>HC's notes for the final assessment.</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
```

### ApplicationDbContext — New DbSets and FK Config
```csharp
// Source: Data/ApplicationDbContext.cs existing Phase 5 pattern

// Add after ProtonDeliverableProgresses DbSet:
public DbSet<ProtonNotification> ProtonNotifications { get; set; }
public DbSet<ProtonFinalAssessment> ProtonFinalAssessments { get; set; }

// In OnModelCreating (after Phase 5 Proton config):
builder.Entity<ProtonFinalAssessment>(entity =>
{
    entity.HasOne(fa => fa.ProtonTrackAssignment)
        .WithMany()
        .HasForeignKey(fa => fa.ProtonTrackAssignmentId)
        .OnDelete(DeleteBehavior.Restrict);  // Consistent with all Proton FKs
    entity.HasIndex(fa => fa.CoacheeId);
    entity.HasIndex(fa => new { fa.CoacheeId, fa.Status });
});

// ProtonNotification has no FK — just indexes
builder.Entity<ProtonNotification>(entity =>
{
    entity.HasIndex(n => n.RecipientId);
    entity.HasIndex(n => new { n.RecipientId, n.IsRead });
    entity.HasIndex(n => n.CoacheeId);
});
```

### DeliverableViewModel — Extended
```csharp
// Source: Models/ProtonViewModels.cs existing DeliverableViewModel — add 3 fields
public class DeliverableViewModel
{
    public ProtonDeliverableProgress? Progress { get; set; }
    public ProtonDeliverable? Deliverable { get; set; }
    public string CoacheeName { get; set; } = "";
    public string TrackType { get; set; } = "";
    public string TahunKe { get; set; } = "";
    public bool IsAccessible { get; set; }
    public bool CanUpload { get; set; }

    // Phase 6 additions
    /// <summary>True for SrSpv/SectionHead when Status=="Submitted" — shows approve/reject buttons.</summary>
    public bool CanApprove { get; set; }
    /// <summary>True for HC when HCApprovalStatus=="Pending" — shows "Tandai Sudah Diperiksa" button.</summary>
    public bool CanHCReview { get; set; }
    /// <summary>Current user's role — used in view for conditional rejection reason display (APPRV-06).</summary>
    public string CurrentUserRole { get; set; } = "";
}
```

### ProtonPlanViewModel — Extended for PROTN-08
```csharp
// Source: Models/ProtonViewModels.cs existing ProtonPlanViewModel — add 1 field
public class ProtonPlanViewModel
{
    public string TrackType { get; set; } = "";
    public string TahunKe { get; set; } = "";
    public List<ProtonKompetensi> KompetensiList { get; set; } = new();
    public ProtonDeliverableProgress? ActiveProgress { get; set; }

    // Phase 6 addition (PROTN-08)
    /// <summary>Final assessment created by HC, if any. Null = not yet created.</summary>
    public ProtonFinalAssessment? FinalAssessment { get; set; }
}
```

### HCApprovalQueueViewModel (new)
```csharp
// Source: new class in Models/ProtonViewModels.cs — follows ProtonMainViewModel pattern
public class HCApprovalQueueViewModel
{
    /// <summary>Progress records needing HC review (HCApprovalStatus=="Pending").</summary>
    public List<ProtonDeliverableProgress> PendingReviews { get; set; } = new();

    /// <summary>Unread notifications for this HC user.</summary>
    public List<ProtonNotification> Notifications { get; set; } = new();

    /// <summary>Coachee display names keyed by CoacheeId — avoids N+1.</summary>
    public Dictionary<string, string> UserNames { get; set; } = new();

    /// <summary>Coachees who have completed all deliverables and are pending final assessment.</summary>
    public List<(string CoacheeId, string CoacheeName, int TrackAssignmentId)> ReadyForFinalAssessment { get; set; } = new();
}
```

---

## State of the Art

| Current State (End of Phase 5) | Phase 6 Target | Notes |
|-------------------------------|----------------|-------|
| `ProtonDeliverableProgress.Status` transitions: Locked → Active → Submitted → (Approved or Rejected) | Add approval actions that drive the Status transition from Submitted → Approved/Rejected | Phase 5 implemented upload (→ Submitted) only |
| No rejection reason stored | Add `RejectionReason` column to `ProtonDeliverableProgress` | APPRV-05: written reason required |
| No HC review tracking on progress records | Add `HCApprovalStatus`, `HCReviewedAt`, `HCReviewedById` columns | HC review is non-blocking to main status |
| No notification system | New `ProtonNotification` table for HC in-app notifications | In-app only; email/real-time is v2 |
| No final assessment model | New `ProtonFinalAssessment` table | PROTN-07 |
| `PlanIdp.cshtml` Coachee view shows only deliverable list + active button | Extend to show final assessment card if `FinalAssessment != null` | PROTN-08 |
| `Deliverable.cshtml` shows upload form and status only | Extend to show approve/reject forms for SrSpv/SectionHead, HC review button for HC, rejection reason text for all roles | APPRV-02 through APPRV-06 |
| No `HCApprovals` page | New `HCApprovals` page with pending reviews queue and notification list | PROTN-06 / APPRV-04 |
| `CDPController` has 6 actions | Add 5 new actions: `ApproveDeliverable`, `RejectDeliverable`, `HCReviewDeliverable`, `HCApprovals`, `CreateFinalAssessment` | One controller — keeps CDP domain together |

**APPRV-01 status:** Already implemented by Phase 5. `UploadEvidence` POST sets Status="Submitted" — this IS the "submit for approval" action. Phase 6 does NOT re-implement submission; it implements what happens AFTER submission (review side).

---

## Open Questions

1. **Which `KkjMatrixItem` does the Proton track completion map to for `UserCompetencyLevel` update?**
   - What we know: `UserCompetencyLevel` uses `KkjMatrixItemId` FK to `KkjMatrices` table. The Proton track (Operator/Panelman Tahun 1/2/3) represents specific HSSE competency domains. `KkjMatrixItem` has `Kompetensi` and `SkillGroup` columns.
   - What's unclear: There is no established mapping between Proton track type and a specific `KkjMatrixItemId`. This is domain knowledge that should come from the client.
   - Recommendation: For Phase 6, the `CreateFinalAssessment` form shows a dropdown of `KkjMatrixItem` names so HC can select which competency is being certified. HC selects the relevant competency item(s). This is consistent with how assessment results already update `UserCompetencyLevel`. The planner should design the form to include a `KkjMatrixItemId` selector.

2. **Should `HCApprovals` page only show pending reviews, or also a list of coachees ready for final assessment?**
   - What we know: HC must complete all pending reviews before PROTN-07. After completing reviews, HC needs to know WHICH coachees are ready for final assessment.
   - Recommendation: `HCApprovals` shows two sections: (A) pending reviews queue, and (B) "Ready for Final Assessment" list — coachees where all progresses have HCApprovalStatus="Reviewed" AND no `ProtonFinalAssessment` exists yet. This gives HC a complete workflow on one page.

3. **What does PROTN-08 show for competency level update specifically?**
   - What we know: `ProtonFinalAssessment.CompetencyLevelGranted` stores the awarded level (0-5). `UserCompetencyLevel` tracks current vs target.
   - Recommendation: `PlanIdp.cshtml` Coachee branch shows a final "Hasil Penilaian" card at the bottom: displays the `ProtonFinalAssessment.CompetencyLevelGranted` value and the name of the KKJ competency updated. This makes the competency update visible to the coachee (PROTN-08 satisfied).

4. **Does the `Deliverable` GET need to show approval actions for SrSpv/SectionHead in same section only, or all SrSpv/SectionHead?**
   - What we know: Phase 5 restricts Coach's access to the Deliverable page by section membership (`coachee.Section != user.Section` → Forbid). SrSpv and SectionHead are level 4 — they oversee the section.
   - Recommendation: Apply the same section membership check for SrSpv/SectionHead approvers as for coaches. If `user.Section != coachee.Section` → Forbid. This is consistent and prevents cross-section approval.

---

## Sources

### Primary (HIGH confidence — direct codebase inspection)
- `Models/ProtonModels.cs` — confirmed 5 existing entities; no approval fields; no ProtonNotification/ProtonFinalAssessment
- `Models/ProtonViewModels.cs` — confirmed existing viewmodels; DeliverableViewModel has CanUpload/IsAccessible only
- `Controllers/CDPController.cs` — confirmed full action list; confirmed UploadEvidence already sets Status="Submitted"; confirmed RoleLevel checks pattern; confirmed section-based access control in Deliverable GET
- `Data/ApplicationDbContext.cs` — confirmed existing 5 Proton DbSets; confirmed DeleteBehavior.Restrict pattern; confirmed no notification/final assessment tables
- `Models/ApplicationUser.cs` — confirmed RoleLevel; no notification nav property
- `Models/UserRoles.cs` — confirmed exact constants: HC="HC", SrSupervisor="Sr Supervisor", SectionHead="Section Head"
- `Models/Competency/UserCompetencyLevel.cs` — confirmed CurrentLevel/TargetLevel (int 0-5); Source field; KkjMatrixItemId FK; upsert via unique index on (UserId, KkjMatrixItemId)
- `Models/AssessmentSession.cs` — confirmed existing assessment model; not reused for Proton Final Assessment (scope and purpose differ)
- `Controllers/CMPController.cs` — confirmed `[Authorize(Roles = "Admin, HC")]` pattern for HC-only actions; confirmed CreateAssessment pattern
- `Migrations/20260217063156_AddProtonDeliverableTracking.cs` — confirmed existing 5 Proton tables with no approval columns
- `.planning/REQUIREMENTS.md` — confirmed APPRV-01 through APPRV-06, PROTN-06/07/08 definitions; confirmed v2 scope for email notifications
- `.planning/STATE.md` — confirmed HC approval is non-blocking per deliverable; confirmed Phase 5 complete with 5/5 truths verified
- `Views/CDP/Deliverable.cshtml` — confirmed current structure: status display, rejection alert (no reason text), upload form; confirmed access pattern

### Secondary (HIGH confidence — requirements + verified architecture)
- `.planning/ROADMAP.md` — Phase 6 success criteria; confirms "either approver alone is sufficient"
- `.planning/phases/05-proton-deliverable-tracking/05-VERIFICATION.md` — Phase 5 complete; confirms all 5 Phase 5 truths; identifies gaps that Phase 6 must close

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; same ASP.NET Core 8 patterns
- Entity design (5 new fields + 2 tables): HIGH — follows existing Proton entity patterns exactly; no FK on string IDs; DeleteBehavior.Restrict on ProtonFinalAssessment FK
- Approval workflow logic: HIGH — single-approver immediate transition is unambiguous; derived directly from APPRV-02/03 requirements
- HC notification pattern: HIGH — in-app DB queue confirmed by requirements (email is v2); GetUsersInRoleAsync is standard Identity method
- PROTN-07/08 final assessment: MEDIUM — exact KkjMatrixItemId mapping is an open question; rest of the pattern is HIGH confidence
- Common pitfalls: HIGH — identified from reading actual Phase 5 code and understanding the two-channel approval design

**Research date:** 2026-02-17
**Valid until:** 2026-03-17 (stable ASP.NET Core 8.0 stack; findings are codebase-specific)
