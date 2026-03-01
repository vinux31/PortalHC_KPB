# Phase 65: Actions - Research

**Researched:** 2026-02-27
**Domain:** ASP.NET Core MVC — AJAX actions, modal-based approval workflow, coaching session creation, evidence upload, Excel/PDF export
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Approve/Reject UX**
- Modal-based approval: clicking "Tinjau" button on a row opens a modal with Action dropdown (Approve/Reject) + Comment field
- Comment is **required for Reject**, optional for Approve
- Modal shows full context: Kompetensi, Sub-Kompetensi, Deliverable name, and a link to view/download the uploaded evidence
- "Tinjau" button is a **visible button per row**, not a dropdown item
- Button enabled only when coach has uploaded evidence (status = Submitted)
- **Column ownership:** SrSpv button only in SrSpv column, SectionHead button only in SH column, HC button only in HC column
- **Independent approval:** Either SrSpv OR SectionHead can approve (no sequential dependency between them). Both roles can still approve even if the other already has
- After modal submission: **toast notification + in-place row update** (AJAX, no full page reload)
- **HC Review:** Simple confirm button (not the full modal). Small confirm dialog "Mark as reviewed?"

**Approval Badge Display**
- Status badge with **hover tooltip** showing approver name and date
- When SH approves but SrSpv hasn't acted, SrSpv column still shows "Pending" (each column reflects only that role's action)
- Rejection reason is only visible on the **Deliverable detail page**, not in the table

**Rejection Flow**
- Status stays **"Rejected"** until coach re-uploads (clear audit trail)
- Rejected deliverable **blocks the next** deliverable in sequence from proceeding
- Coach re-uploads evidence -> status becomes **"Submitted"** again
- Old evidence is **replaced** (no version history)
- On resubmission: approval columns for SrSpv/SH **reset to Pending** (fresh review cycle)
- No visual distinction between first-time submission and re-submission

**Status Flow Redesign**
- **Remove Locked status entirely** — all deliverables start as **Pending**
- New flow: Pending -> Submitted -> Approved / Rejected -> (if rejected) -> Submitted
- Badge colors: Pending = Gray, Submitted = Blue, Approved = Green, Rejected = Red
- HC statuses: Pending = Gray, Reviewed = Green

**Combined Evidence + Coaching Modal**
- **Single "Submit Evidence" button** that combines evidence submission and coaching report into one modal
- Coach only, visible on **Pending and Rejected** deliverables
- Modal fields:
  1. Header (auto-filled, read-only): Kompetensi, Sub-Kompetensi, Deliverable
  2. Date (auto-filled today, editable)
  3. Kompetensi Coachee (free-text)
  4. Catatan Coach (free-text notes)
  5. Kesimpulan (dropdown: "Kompeten secara mandiri" / "Masih perlu dikembangkan")
  6. Result (dropdown: "Need Improvement" / "Suitable" / "Good" / "Excellence")
  7. File upload (optional, PDF/JPG/PNG, max 10MB)
- **Batch submission:** Coach can select multiple deliverables in one modal. Pre-filled with clicked deliverable, can add more
- **All fields are shared** across selected deliverables
- **One file** for all selected deliverables (optional)
- All selected deliverables change to **Submitted** on submit
- Creates CoachingSession record(s) linked to selected deliverables
- Coach can **edit** coaching reports but **cannot delete** them
- All roles can view coaching reports (full transparency)

**Progress Calculation**
- Pending = 0%, Submitted = 50%, Approved = 100%, Rejected = 0%
- Stat cards: Progress % (average), Pending Evidence (Pending + Rejected), Pending Approvals (role-aware, Submitted count)

**Table Column Layout**
- Columns: Kompetensi | Sub-Kompetensi | Deliverable | Evidence | Approval Sr.Spv | Approval SH | Approval HC | Detail
- Evidence column: Coach sees "Submit Evidence" button; other roles see status badge
- Approval columns: "Tinjau"/"Review" buttons for respective roles
- Detail column: "Lihat Detail" link to Deliverable detail page
- Keep rowspan merging for Kompetensi/Sub-Kompetensi grouping

**Coachee View**
- Read-only table with all statuses visible
- Can download evidence files
- No export buttons

**Export**
- Excel (ClosedXML) + PDF for current coachee only
- Includes coaching data (Catatan Coach, Kesimpulan, Result)
- Buttons at top of page near stat cards
- Filename: `CoacheeName_Progress_YYYY-MM-DD.xlsx` / `.pdf`
- Roles: SrSpv, SectionHead, HC, Admin only

**Deliverable Detail Page**
- Update to show coaching report data (Catatan, Kesimpulan, Result, uploaded file)
- Shows rejection reason for rejected deliverables

### Claude's Discretion
- Excel format (flat rows vs merged cells)
- PDF table layout details
- Exact modal styling and button placement
- Toast notification design
- Confirm dialog design for HC Review
- Evidence column badge text/style for non-coach roles
- How multi-deliverable selector UI works in the combined modal

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| ACTN-01 | SrSpv/SectionHead bisa approve deliverable dari Progress page, status tersimpan ke ProtonDeliverableProgress di database | New AJAX endpoints `ApproveFromProgress` / `RejectFromProgress` in CDPController; schema migration adds SrSpv/SH independent approval fields |
| ACTN-02 | SrSpv/SectionHead bisa reject deliverable dari Progress page dengan alasan tertulis | Same endpoint as ACTN-01 for reject path; RejectionReason stored in existing field; modal shows textarea for reason |
| ACTN-03 | Coach bisa submit laporan coaching dari modal, tersimpan sebagai CoachingSession record di database | New `SubmitEvidence` AJAX endpoint combines file upload + CoachingSession creation; needs migration to add `ProtonDeliverableProgressId` FK on CoachingSession |
| ACTN-04 | Upload evidence dan lihat evidence di Progress page tersambung ke existing Deliverable workflow | Reuse existing `UploadEvidence` endpoint; batch path: one file saved per progress record in loop; EvidencePath stored as before |
| ACTN-05 | Export data progress ke Excel (ClosedXML) dan PDF | ClosedXML already installed; PDF needs new package (QuestPDF or similar); two new GET endpoints `ExportProgressExcel` and `ExportProgressPdf` |
</phase_requirements>

## Summary

Phase 65 wires all action buttons on the ProtonProgress page to the database. The page was built in Phase 64 with a complete filter bar but stub/missing action handlers. This phase adds the backend endpoints (AJAX) and frontend modals/UI for five action types: Approve, Reject, Submit Evidence+Coaching, and Export Excel/PDF.

The most significant architectural challenge is that the current `ProtonDeliverableProgress.Status` is a single field shared by both SrSpv and SectionHead. The CONTEXT.md decision that "either SrSpv OR SectionHead can approve independently" means the model must be extended with per-role approval tracking fields (`SrSpvApprovalStatus`, `SrSpvApprovedById`, `SrSpvApprovedAt`, `ShApprovalStatus`, `ShApprovedById`, `ShApprovedAt`). This requires a new EF migration. The existing `Status`, `ApprovedById`, `ApprovedAt`, `RejectedAt`, `RejectionReason` fields stay but represent the "overall" deliverable status.

The `CoachingSession` model currently has no link back to `ProtonDeliverableProgress`. The combined Evidence+Coaching modal requires creating one `CoachingSession` per selected deliverable (or one shared session linked to all), storing the progress ID as a foreign key. A migration adding `ProtonDeliverableProgressId` (nullable int, no FK constraint — matches project pattern) to `CoachingSession` is needed.

**Primary recommendation:** Implement in 3 plans: (1) Schema migration + model updates + new AJAX endpoints for Approve/Reject/HCReview from ProtonProgress; (2) Combined Evidence+Coaching modal and `SubmitEvidence` AJAX endpoint; (3) Export Excel+PDF endpoints + Deliverable detail page coaching data display.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | 0.105.0 (already installed) | Excel export | Already used for ExportAnalyticsResults in CDPController |
| QuestPDF | 2024.x | PDF export | Open-source, .NET-native, no external process, no wkhtmltopdf binary needed |
| Bootstrap 5 (bundled) | 5.x | Modal, toast, badge UI | Already used throughout the project |
| Vanilla fetch() | Browser native | AJAX POST for approve/reject/submit | Already used in CpdpItems, ProtonCatalog, ManageAssessment |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Bootstrap Toast API | 5.x (bundled) | In-place success/error notification after AJAX | After approve/reject/submit response |
| data-bs-toggle="modal" | Bootstrap 5 | Tinjau modal trigger on row button | Per-row "Tinjau" button |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| QuestPDF | iTextSharp (AGPL) | iTextSharp has restrictive license for commercial apps |
| QuestPDF | Rotativa/wkhtmltopdf | Rotativa requires wkhtmltopdf binary deployment; QuestPDF is pure .NET |
| QuestPDF | PdfSharpCore | PdfSharpCore has limited layout capabilities; QuestPDF has better table support |
| Bootstrap Toast | SweetAlert2 / custom toast | SweetAlert2 is an extra dependency; Bootstrap Toast is already available |

**Installation (QuestPDF only — ClosedXML already present):**
```bash
dotnet add package QuestPDF
```

## Architecture Patterns

### Recommended Project Structure

New endpoints added to `CDPController.cs` (same controller as existing ProtonProgress/Approve/Reject):

```
Controllers/
└── CDPController.cs                   # Add 6 new POST/GET actions
Models/
└── ProtonModels.cs                    # Add SrSpv/SH approval fields to ProtonDeliverableProgress
    CoachingSession.cs                 # Add ProtonDeliverableProgressId field
Views/CDP/
└── ProtonProgress.cshtml              # Add modals, action buttons, toast script, export buttons
    Deliverable.cshtml                 # Add coaching report display section
Migrations/
└── [timestamp]_AddPerRoleApprovalFields.cs   # New migration
```

### Pattern 1: AJAX Action with AntiForgery Token (Existing Pattern)

The project already uses this pattern in CpdpItems and AdminController. Reuse exactly.

**What:** Hidden form provides AntiForgery token; JS reads token and includes in fetch POST header.
**When to use:** All AJAX POST endpoints (ApproveFromProgress, RejectFromProgress, HCReviewFromProgress, SubmitEvidenceWithCoaching).

```javascript
// Source: existing pattern in Views/Admin/CpdpItems.cshtml and Views/Admin/AssessmentMonitoringDetail.cshtml
var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
fetch('/CDP/ApproveFromProgress', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        'RequestVerificationToken': token
    },
    body: 'progressId=' + progressId + '&comment=' + encodeURIComponent(comment)
})
.then(r => r.json())
.then(data => {
    if (data.success) {
        showToast(data.message, 'success');
        updateRowBadge(progressId, data.newStatus, data.approverName, data.approvedAt);
    } else {
        showToast(data.message || 'Terjadi kesalahan.', 'danger');
    }
});
```

For multipart/form-data (file upload with coaching data), use `FormData` object:
```javascript
// Source: pattern needed for Submit Evidence with file upload
var formData = new FormData();
formData.append('progressIds', JSON.stringify(selectedIds));
formData.append('date', dateVal);
formData.append('koacheeCompetencies', competenciesVal);
formData.append('catatanCoach', catatanVal);
formData.append('kesimpulan', kesimpulanVal);
formData.append('result', resultVal);
if (fileInput.files[0]) formData.append('evidenceFile', fileInput.files[0]);
// Note: with FormData, do NOT set Content-Type header (browser sets multipart boundary automatically)
fetch('/CDP/SubmitEvidenceWithCoaching', {
    method: 'POST',
    headers: { 'RequestVerificationToken': token },
    body: formData
})
```

### Pattern 2: Bootstrap Modal Triggered by Row Button

**What:** "Tinjau" button on each table row triggers a shared modal (not one modal per row). Button stores row data in `data-*` attributes; JS populates modal fields on show.

**When to use:** Approve/Reject modal (shared, populated via JS on open).

```javascript
// Source: Bootstrap 5 docs — modal show event
// In ProtonProgress.cshtml @section Scripts
var tinjaModal = document.getElementById('tinjaModal');
tinjaModal.addEventListener('show.bs.modal', function(event) {
    var btn = event.relatedTarget; // button that triggered the modal
    var progressId = btn.dataset.progressId;
    var deliverable = btn.dataset.deliverable;
    var kompetensi = btn.dataset.kompetensi;
    var subKompetensi = btn.dataset.subKompetensi;
    var evidenceUrl = btn.dataset.evidenceUrl;
    // Populate modal fields
    document.getElementById('modalProgressId').value = progressId;
    document.getElementById('modalDeliverableName').textContent = deliverable;
    // ... etc
});
```

### Pattern 3: Bootstrap Toast for In-Place Notification

**What:** After AJAX success/failure, show Bootstrap Toast (not full page reload).
**When to use:** All AJAX action responses.

```javascript
// Source: Bootstrap 5 Toast docs — existing Bootstrap 5 already bundled
function showToast(message, type) {
    var toastEl = document.getElementById('actionToast');
    var toastBody = document.getElementById('actionToastBody');
    toastEl.className = 'toast align-items-center text-bg-' + type + ' border-0';
    toastBody.textContent = message;
    var toast = bootstrap.Toast.getOrCreateInstance(toastEl);
    toast.show();
}
```

Toast container in view:
```html
<div class="toast-container position-fixed bottom-0 end-0 p-3" style="z-index:1100">
    <div id="actionToast" class="toast align-items-center border-0" role="alert" aria-live="assertive">
        <div class="d-flex">
            <div class="toast-body" id="actionToastBody"></div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    </div>
</div>
```

### Pattern 4: ClosedXML Excel Export (Existing Pattern)

Already implemented in `CDPController.ExportAnalyticsResults()`. Reuse the same approach:

```csharp
// Source: CDPController.cs line ~554 — existing ExportAnalyticsResults
using var workbook = new XLWorkbook();
var worksheet = workbook.Worksheets.Add("Proton Progress");
// ... header + data rows ...
worksheet.Columns().AdjustToContents();
using var stream = new MemoryStream();
workbook.SaveAs(stream);
return File(stream.ToArray(),
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    $"{coacheeName}_Progress_{DateTime.Now:yyyy-MM-dd}.xlsx");
```

### Pattern 5: Per-Role Independent Approval Schema

The current `ProtonDeliverableProgress.Status` (Submitted/Approved/Rejected) is shared. Phase 65 adds per-role fields:

```csharp
// Add to ProtonDeliverableProgress (no FK — matches project pattern)
/// <summary>SrSpv independent approval. Values: "Pending", "Approved", "Rejected"</summary>
public string SrSpvApprovalStatus { get; set; } = "Pending";
public string? SrSpvApprovedById { get; set; }
public DateTime? SrSpvApprovedAt { get; set; }

/// <summary>SectionHead independent approval. Values: "Pending", "Approved", "Rejected"</summary>
public string ShApprovalStatus { get; set; } = "Pending";
public string? ShApprovedById { get; set; }
public DateTime? ShApprovedAt { get; set; }

/// <summary>SrSpv/SH rejection reason (shared field, last writer wins)</summary>
// RejectionReason field already exists
```

The overall `Status` on the record remains the canonical deliverable state (Pending/Submitted/Approved/Rejected), driven by the workflow:
- On resubmission (evidence upload): reset `SrSpvApprovalStatus` = "Pending", `ShApprovalStatus` = "Pending"
- `ApproveFromProgress` sets the per-role field; does NOT change overall `Status` to "Approved" unless at least one of SrSpv/SH has approved AND the other hasn't rejected

### Pattern 6: CoachingSession Linked to Deliverable Progress

Add `ProtonDeliverableProgressId` (nullable int, no FK constraint — matches CoachingLog/CoachCoacheeMapping pattern) to `CoachingSession`. For batch submission, create one `CoachingSession` per selected deliverable progress record, with shared field values.

```csharp
// Add to CoachingSession model:
/// <summary>Links coaching session to a specific deliverable. No FK — matches project pattern.</summary>
public int? ProtonDeliverableProgressId { get; set; }
```

### Anti-Patterns to Avoid

- **Separate modal per row:** Use one shared modal populated by JS on show — DOM/memory efficient
- **Full page reload after AJAX action:** Toast + in-place row update instead (CONTEXT.md explicit requirement)
- **Sequential SrSpv->SH approval:** Both roles approve independently per CONTEXT.md
- **CoachingSession with a hard FK to ProtonDeliverableProgress:** Use no-FK string/int pattern consistent with rest of project
- **Creating separate upload endpoint for coaching:** Combine evidence + coaching into one `SubmitEvidenceWithCoaching` action

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| PDF generation | Custom PDF builder | QuestPDF | Font metrics, page layout, table overflow, headers/footers are non-trivial |
| Toast notifications | Custom toast CSS/JS | Bootstrap 5 Toast (already bundled) | Bootstrap already on every page |
| Modal | Custom modal from scratch | Bootstrap 5 Modal (already bundled) | Already used in admin pages |
| AntiForgery in AJAX | Custom CSRF handling | `@Html.AntiForgeryToken()` + `RequestVerificationToken` header | Existing project pattern |
| Excel with merged cells | ClosedXML merged cell API | ClosedXML flat rows (Claude's discretion — simpler) | Merged cells add complexity, flat is readable |

**Key insight:** The project already has AntiForgery + fetch() + ClosedXML patterns that work. Use them exactly as-is for approve/reject/export endpoints.

## Common Pitfalls

### Pitfall 1: Single Status Field Cannot Model Independent Approval

**What goes wrong:** Using the existing `ProtonDeliverableProgress.Status` for per-role approval means approving as SrSpv overwrites SectionHead's state and vice versa.
**Why it happens:** The original design had a single approval chain; CONTEXT.md requires independent SrSpv and SH approval.
**How to avoid:** Add `SrSpvApprovalStatus` and `ShApprovalStatus` fields via migration before implementing the approval actions.
**Warning signs:** If the table only shows one "Approved" badge when either role approves, the schema change is missing.

### Pitfall 2: TrackingItem Missing Fields for New UI

**What goes wrong:** `TrackingItem` maps `ProtonDeliverableProgress` to view data. It currently has `ApprovalSrSpv`/`ApprovalSectionHead` as computed strings. After schema migration, these should reflect per-role status, not overall `Status`.
**Why it happens:** The Phase 64 mapping logic (`ApprovalSrSpv = p.Status == "Approved" ? "Approved" : ...`) ignores per-role fields.
**How to avoid:** After migration, update the mapping in `ProtonProgress` action to use `p.SrSpvApprovalStatus` and `p.ShApprovalStatus`. Also add `SrSpvApprovedById`, `SrSpvApprovedAt`, `ShApprovedById`, `ShApprovedAt`, `EvidencePath` to `TrackingItem` so the view can render tooltip text and evidence links.
**Warning signs:** Tooltip shows wrong approver, or evidence link is missing.

### Pitfall 3: Reset Approval on Re-Upload

**What goes wrong:** When a coach re-uploads evidence after rejection, the existing `UploadEvidence` endpoint does not reset SrSpv/SH approval statuses.
**Why it happens:** `UploadEvidence` was written before per-role approval existed.
**How to avoid:** In the new `SubmitEvidenceWithCoaching` endpoint (or by modifying `UploadEvidence`), explicitly set `SrSpvApprovalStatus = "Pending"` and `ShApprovalStatus = "Pending"` on re-upload.
**Warning signs:** After coach re-uploads, old approval badge still shows "Approved" or "Rejected".

### Pitfall 4: Multipart FormData with AntiForgery

**What goes wrong:** When sending `FormData` (file upload + fields), setting `Content-Type: application/json` breaks multipart parsing.
**Why it happens:** `FormData` requires `multipart/form-data` with boundary — browser sets this automatically only if Content-Type is NOT manually set.
**How to avoid:** For file upload AJAX: include `RequestVerificationToken` header but do NOT set `Content-Type` header. Let browser set it.
**Warning signs:** Server receives null file or 400 Bad Request.

### Pitfall 5: Status Locked Removal Migration

**What goes wrong:** Existing `ProtonDeliverableProgress` records in the database may have `Status = "Locked"` (set by `AssignTrack`). After removing Locked from the status flow, these records become inconsistent.
**Why it happens:** `AssignTrack` creates first record as "Active" and rest as "Locked". CONTEXT.md removes Locked.
**How to avoid:** Migration data update: `UPDATE ProtonDeliverableProgresses SET Status = 'Pending' WHERE Status = 'Locked'` and `SET Status = 'Pending' WHERE Status = 'Active'`. Also update `AssignTrack` to create all new records as "Pending". Update `ApproveDeliverable` to not set next record to "Active".
**Warning signs:** Table shows "Locked" badge or "Not Started" badge when it should show "Pending".

### Pitfall 6: QuestPDF License

**What goes wrong:** QuestPDF changed its license in 2023; community (free) edition has limitations.
**Why it happens:** QuestPDF introduced a commercial license for high-volume use.
**How to avoid:** QuestPDF Community license is free for companies with revenue < $1M/year or individuals. Internal corporate portal use typically qualifies. If license is a concern, use `PdfSharpCore` as fallback (simpler API but adequate for table-style PDF). Set license at app startup: `QuestPDF.Settings.License = LicenseType.Community;`
**Warning signs:** Runtime exception "QuestPDF is used in a commercial context without a valid license."

### Pitfall 7: rowspan Conflict with Action Buttons

**What goes wrong:** Adding "Tinjau" button per row conflicts with rowspan on Kompetensi/Sub-Kompetensi cells when re-rendering after AJAX.
**Why it happens:** rowspan merging means the Kompetensi cell for row 1 spans rows 1-3; if row 1's badge updates in-place, the rowspan structure is preserved but the wrong cell gets updated.
**How to avoid:** In-place update should only update the approval badge cell(s) in the specific `<tr>` — NOT the full row. Use `data-progress-id` on the `<td>` elements to find and update the correct cells.
**Warning signs:** After AJAX approve, the wrong row's badge changes, or rowspan is broken.

## Code Examples

### Approve/Reject AJAX Endpoint (CDPController)

```csharp
// New endpoint — Source: project pattern from existing ApproveDeliverable()
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveFromProgress(int progressId, string? comment)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Json(new { success = false, message = "Unauthorized" });
    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    bool isSrSpv = userRole == UserRoles.SrSupervisor;
    bool isSH = userRole == UserRoles.SectionHead;
    if (!isSrSpv && !isSH) return Json(new { success = false, message = "Forbidden" });

    var progress = await _context.ProtonDeliverableProgresses
        .FirstOrDefaultAsync(p => p.Id == progressId);
    if (progress == null) return Json(new { success = false, message = "Not found" });

    // Section check
    var coacheeUser = await _context.Users.FindAsync(progress.CoacheeId);
    if (coacheeUser?.Section != user.Section)
        return Json(new { success = false, message = "Cross-section not allowed" });

    // Only Submitted status can be approved
    if (progress.Status != "Submitted")
        return Json(new { success = false, message = "Hanya status Submitted yang dapat disetujui" });

    // Set per-role approval
    if (isSrSpv)
    {
        progress.SrSpvApprovalStatus = "Approved";
        progress.SrSpvApprovedById = user.Id;
        progress.SrSpvApprovedAt = DateTime.UtcNow;
    }
    else // SH
    {
        progress.ShApprovalStatus = "Approved";
        progress.ShApprovedById = user.Id;
        progress.ShApprovedAt = DateTime.UtcNow;
    }

    // Update overall Status to Approved (either role approving is sufficient)
    progress.Status = "Approved";
    progress.ApprovedAt = DateTime.UtcNow;
    progress.ApprovedById = user.Id;

    await _context.SaveChangesAsync();

    var approverName = user.FullName ?? user.UserName ?? user.Id;
    return Json(new {
        success = true,
        message = "Deliverable berhasil disetujui",
        newStatus = isSrSpv ? progress.SrSpvApprovalStatus : progress.ShApprovalStatus,
        approverName,
        approvedAt = DateTime.UtcNow.ToString("dd MMM yyyy")
    });
}
```

### SubmitEvidenceWithCoaching AJAX Endpoint (CDPController)

```csharp
// New multipart endpoint — handles combined evidence + coaching session creation
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> SubmitEvidenceWithCoaching(
    [FromForm] string progressIdsJson,
    [FromForm] DateTime date,
    [FromForm] string koacheeCompetencies,
    [FromForm] string catatanCoach,
    [FromForm] string kesimpulan,
    [FromForm] string result,
    IFormFile? evidenceFile)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Json(new { success = false, message = "Unauthorized" });
    if (user.RoleLevel != 5) return Json(new { success = false, message = "Coach only" });

    var progressIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(progressIdsJson)
        ?? new List<int>();

    // Load + validate all progress records
    var progresses = await _context.ProtonDeliverableProgresses
        .Include(p => p.ProtonDeliverable)
            .ThenInclude(d => d!.ProtonSubKompetensi)
                .ThenInclude(s => s!.ProtonKompetensi)
        .Where(p => progressIds.Contains(p.Id) && p.CoacheeId != null)
        .ToListAsync();

    // Handle optional file upload (save once, reuse path for all)
    string? evidencePath = null;
    string? evidenceFileName = null;
    if (evidenceFile != null && evidenceFile.Length > 0)
    {
        // Save to first progressId directory, reuse path for all
        var firstId = progresses.First().Id;
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "evidence", firstId.ToString());
        Directory.CreateDirectory(uploadDir);
        var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(evidenceFile.FileName)}";
        var filePath = Path.Combine(uploadDir, safeFileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await evidenceFile.CopyToAsync(stream);
        evidencePath = $"/uploads/evidence/{firstId}/{safeFileName}";
        evidenceFileName = evidenceFile.FileName;
    }

    foreach (var progress in progresses)
    {
        // Update progress status
        progress.Status = "Submitted";
        progress.SubmittedAt = DateTime.UtcNow;
        progress.SrSpvApprovalStatus = "Pending"; // Reset on resubmit
        progress.ShApprovalStatus = "Pending";
        if (evidencePath != null)
        {
            progress.EvidencePath = evidencePath;
            progress.EvidenceFileName = evidenceFileName;
        }

        // Create CoachingSession record
        var session = new CoachingSession
        {
            CoachId = user.Id,
            CoacheeId = progress.CoacheeId,
            Date = date,
            Kompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.ProtonKompetensi?.NamaKompetensi ?? "",
            SubKompetensi = progress.ProtonDeliverable?.ProtonSubKompetensi?.NamaSubKompetensi ?? "",
            Deliverable = progress.ProtonDeliverable?.NamaDeliverable ?? "",
            CoacheeCompetencies = koacheeCompetencies,
            CatatanCoach = catatanCoach,
            Kesimpulan = kesimpulan,
            Result = result,
            Status = "Submitted",
            ProtonDeliverableProgressId = progress.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.CoachingSessions.Add(session);
    }

    await _context.SaveChangesAsync();
    return Json(new { success = true, message = $"{progresses.Count} deliverable berhasil disubmit" });
}
```

### ClosedXML Excel Export for Progress (CDPController)

```csharp
// Source: existing pattern CDPController.ExportAnalyticsResults() lines ~554-596
[HttpGet]
[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]
public async Task<IActionResult> ExportProgressExcel(string coacheeId)
{
    // Load data (reuse same query as ProtonProgress action)
    // ...
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add("Progress");

    // Header row
    int col = 1;
    foreach (var header in new[] {"Kompetensi","Sub Kompetensi","Deliverable","Evidence",
                                   "Approval SrSpv","Approval SH","Approval HC",
                                   "Catatan Coach","Kesimpulan","Result"})
    {
        ws.Cell(1, col).Value = header;
        col++;
    }
    var headerRange = ws.Range(1, 1, 1, col-1);
    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

    // Data rows
    int row = 2;
    foreach (var item in data)
    {
        ws.Cell(row, 1).Value = item.Kompetensi;
        // ... etc
        row++;
    }
    ws.Columns().AdjustToContents();

    using var stream = new MemoryStream();
    workbook.SaveAs(stream);
    var coacheeName = /* coachee full name */;
    return File(stream.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"{coacheeName}_Progress_{DateTime.Now:yyyy-MM-dd}.xlsx");
}
```

### QuestPDF Table PDF Export

```csharp
// Install: dotnet add package QuestPDF
// Set license at startup (Program.cs or controller constructor):
// QuestPDF.Settings.License = LicenseType.Community;

[HttpGet]
[Authorize(Roles = "Sr Supervisor, Section Head, HC, Admin")]
public async Task<IActionResult> ExportProgressPdf(string coacheeId)
{
    // Load data ...
    var pdf = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(20);
            page.Content().Column(col =>
            {
                // Header
                col.Item().Text($"Proton Progress — {coacheeName}").FontSize(14).Bold();
                col.Item().Text($"Export date: {DateTime.Now:dd MMM yyyy}").FontSize(9);
                col.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2); // Kompetensi
                        cols.RelativeColumn(2); // Sub Kompetensi
                        // ... etc
                    });
                    // Header row
                    // Data rows
                });
            });
        });
    });

    var stream = new MemoryStream();
    pdf.GeneratePdf(stream);
    return File(stream.ToArray(), "application/pdf",
        $"{coacheeName}_Progress_{DateTime.Now:yyyy-MM-dd}.pdf");
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `Status = "Locked"/"Active"` for sequential unlock | `Status = "Pending"` for all deliverables | Phase 65 redesign | Simpler flow; "Locked" removed from UI and DB |
| Single `Status` for all approvers | Per-role `SrSpvApprovalStatus` + `ShApprovalStatus` | Phase 65 schema migration | Supports independent SrSpv/SH approval |
| No CoachingSession<->Progress link | `ProtonDeliverableProgressId` on CoachingSession | Phase 65 migration | Enables coaching report display on Deliverable detail page |
| Upload Evidence on Deliverable detail page only | "Submit Evidence" modal on Progress page | Phase 65 | Inline submission without leaving the page |

**Deprecated/outdated:**
- `Status = "Locked"` and `Status = "Active"`: replaced by `Status = "Pending"` — update `AssignTrack`, migration, and any status checks
- `ApproveDeliverable` / `RejectDeliverable` actions on Deliverable page: kept for backward compatibility, but Phase 65 adds new AJAX variants for Progress page

## Open Questions

1. **SrSpv approves, then SH rejects — what is overall Status?**
   - What we know: CONTEXT.md says both can approve independently; rejection should block next deliverable
   - What's unclear: If SrSpv approves and SH rejects, what is `Status`? Rejected (blocking) seems correct
   - Recommendation: `Status = "Rejected"` if ANY role rejects (rejection takes precedence). Planner must define this rule explicitly in the plan.

2. **Batch coaching — one CoachingSession per deliverable or one shared?**
   - What we know: CONTEXT.md says "Creates CoachingSession record(s) linked to selected deliverables"
   - What's unclear: One session per deliverable (with `ProtonDeliverableProgressId`) or one session with a comma-separated list
   - Recommendation: One `CoachingSession` per deliverable progress record (cleaner FK, matches the `(s)` in CONTEXT.md). They share the same Coach/Date/CatatanCoach/Kesimpulan/Result values.

3. **Evidence file path for batch: one file shared across multiple deliverables?**
   - What we know: CONTEXT.md says "One file for all selected deliverables"
   - What's unclear: Should EvidencePath be stored per-record (same path repeated) or stored once in CoachingSession?
   - Recommendation: Store the same path in each `ProtonDeliverableProgress.EvidencePath` (consistent with existing pattern). Also store in `CoachingSession` if CoachingSession needs to reference the file.

4. **QuestPDF license acceptance at startup**
   - What we know: QuestPDF requires `QuestPDF.Settings.License = LicenseType.Community;` call before use
   - What's unclear: Whether Program.cs or per-controller is better
   - Recommendation: Add to `Program.cs` app startup for global configuration.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (no automated test framework detected in project) |
| Config file | None |
| Quick run command | Manual browser testing |
| Full suite command | Manual browser testing per UAT checklist |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ACTN-01 | SrSpv clicking "Tinjau" -> Approve -> DB updated | manual | N/A — no test framework | N/A |
| ACTN-02 | SrSpv clicking "Tinjau" -> Reject with reason -> DB updated | manual | N/A | N/A |
| ACTN-03 | Coach submitting Evidence+Coaching modal -> CoachingSession created | manual | N/A | N/A |
| ACTN-04 | Evidence upload from modal -> file saved, viewable from Progress and Deliverable page | manual | N/A | N/A |
| ACTN-05 | Export Excel/PDF buttons -> file download in browser | manual | N/A | N/A |

### Sampling Rate
- **Per task commit:** Manual smoke test of the specific endpoint changed
- **Per wave merge:** Full UAT run of all 5 requirements
- **Phase gate:** All 5 ACTN requirements verified manually before `/gsd:verify-work`

### Wave 0 Gaps
None — no automated test framework is used in this project. All validation is manual UAT.

## Sources

### Primary (HIGH confidence)
- Codebase direct inspection — `Controllers/CDPController.cs` (existing `ApproveDeliverable`, `RejectDeliverable`, `UploadEvidence`, `ExportAnalyticsResults` patterns)
- Codebase direct inspection — `Views/Admin/CpdpItems.cshtml` (fetch() + AntiForgery pattern)
- Codebase direct inspection — `Models/ProtonModels.cs` (ProtonDeliverableProgress fields)
- Codebase direct inspection — `Models/CoachingSession.cs` (existing session model)
- Codebase direct inspection — `HcPortal.csproj` (ClosedXML 0.105.0 installed, no PDF library)
- Codebase direct inspection — `Views/CDP/ProtonProgress.cshtml` (current table structure, Phase 64 output)

### Secondary (MEDIUM confidence)
- QuestPDF license documentation — Community license free for companies < $1M revenue / internal tools
- Bootstrap 5 Toast and Modal API — available via bundled Bootstrap in project (verified via `_Layout.cshtml` and existing modal usage in project)

### Tertiary (LOW confidence)
- None

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — ClosedXML verified installed; Bootstrap 5 verified in use; QuestPDF is well-established .NET PDF library (only library license needs validation at install time)
- Architecture: HIGH — all patterns derived directly from existing project code
- Pitfalls: HIGH — identified from direct code inspection of existing ProtonDeliverableProgress model and Phase 64 code

**Research date:** 2026-02-27
**Valid until:** 2026-03-27 (stable codebase)
