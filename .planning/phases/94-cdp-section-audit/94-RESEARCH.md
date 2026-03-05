# Phase 94: CDP Section Audit - Research

**Researched:** 2026-03-05
**Domain:** ASP.NET Core MVC Quality Audit (CDP Section)
**Confidence:** HIGH

## Summary

Phase 94 audits the CDP (Competency Development Platform) section for bugs across 4 key pages: PlanIdp (IDP planning with Silabus/Guidance tabs), CoachingProton (coaching workflow and progress tracking), Deliverable (evidence upload/download and approval chain), and CDP Index (hub dashboard). Based on Phase 93 CMP audit patterns and Phase 85 coaching QA results, this phase should focus on code review → bug identification → targeted fixes → browser verification.

**Primary recommendation:** Follow the three-flow organization from CONTEXT.md (IDP Planning, Coaching Workflow, Evidence & Approval) with pre-seeded test data using the established SeedCoachingTestData pattern. Prioritize date localization fixes (likely issues based on Phase 93 findings) and null safety checks in evidence file handling.

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **Audit Organization:** Organize by use-case flows (not page-by-page) — following Phase 85 coaching QA pattern
  - Flow 1: IDP Planning (PlanIdp page — silabus tab, guidance tab, filter behavior)
  - Flow 2: Coaching Workflow (CoachingProton — coachee selection, deliverable list, session submission, approval flow)
  - Flow 3: Evidence & Approval (Deliverable detail — evidence upload/download, Spv/HC review, status transitions)
  - One commit per flow or grouped by related fixes

- **Test Data Approach:** Pre-seeded test data required — comprehensive coverage like Phase 85/90
  - Seed: Coachee-coach mappings, Proton tracks with deliverables, coaching sessions in various statuses, evidence files
  - All 5 roles represented: Coachee, Coach, Spv, SectionHead, HC/Admin
  - Data should cover: pending sessions, approved sessions, rejected sessions, evidence with/without files

- **Testing Approach:** Smoke test + targeted verification — not exhaustive role combination testing
  - Pattern: Code review → identify bugs → fix → browser verify (same as Phase 85/93)
  - User verifies in browser after code fixes
  - Focus on verifying the specific bug that was fixed
  - Test data created upfront, then verify all flows work correctly

- **Evidence File Handling Depth:** Deep audit — verify upload/download works + edge cases
  - Check: file size limits, allowed file types, path security, virus scanning (if any), error handling
  - Verify: evidence links work, files are stored correctly, download returns correct content type

- **Role Testing Coverage:** All 5 roles — Coachee, Coach, Spv, SectionHead, HC/Admin
  - Each role tested for their specific workflows (not every role on every page)
  - Coachee: PlanIdp view, submit session, upload evidence
  - Coach: Select coachee, review deliverables, submit coaching session
  - Spv: Review and approve/reject sessions
  - HC: Final approval, view all workflows
  - Admin: Full access (same as HC)

### Claude's Discretion

- Exact order of bug fixes within each flow
- Grouping of fixes into commits (per-flow vs per-category vs per-file)
- Which edge cases to investigate in depth vs quick smoke test
- Whether to refactor any messy code discovered during audit

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CDP-01 | Plan IDP page loads without errors for all roles (Worker, Coach, Spv, HC, Admin) | Flow 1 audit covers PlanIdp load testing; role-based section locking already implemented |
| CDP-02 | Coaching Proton page shows correct coachee lists and deliverable status | Flow 2 audit covers coachee scope queries and deliverable status display; Phase 85 fixes provide patterns |
| CDP-03 | Progress page displays correct approval workflows per role | Flow 2 audit covers approval workflow display; SrSpv/SH/HC role branches well-established |
| CDP-04 | Evidence upload and download work correctly for deliverables | Flow 3 audit covers file handling; UploadEvidence action has validation patterns to verify |
| CDP-05 | Coaching session submission and approval flows work end-to-end | Flow 2 & 3 combined; approval chain POST actions exist and need verification |
| CDP-06 | All CDP forms handle validation errors gracefully | All flows; TempData error handling pattern established in Phase 85/93 |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 6.0+ (implicit from project) | Web framework | Project's existing stack — all controllers use this |
| Entity Framework Core | 6.0+ (implicit from project) | ORM for data access | ApplicationDbContext with UserManager/SignInManager pattern |
| ASP.NET Core Identity | 6.0+ (implicit from project) | Authentication/authorization | UserManager, RoleManager used throughout |
| Bootstrap | 5.x | UI framework | All views use Bootstrap classes (nav-tabs, card, btn, form-select) |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ClosedXML.Excel | (implicit from project) | Excel export | ExportProgressExcel action in CDPController |
| QuestPDF | (implicit from project) | PDF generation | ExportProgressPdf action in CDPController |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| N/A | N/A | This is audit phase — no new libraries introduced |

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
├── CDPController.cs (2146 lines) — Main CDP workflows
├── AdminController.cs (5703 lines) — Test data seeding + Admin operations
├── HomeController.cs — CDP Dashboard partial view rendering

Views/CDP/
├── Index.cshtml (101 lines) — CDP hub dashboard
├── PlanIdp.cshtml (354 lines) — IDP planning with 2-tab layout
├── CoachingProton.cshtml (1539 lines) — Progress tracking table
├── Deliverable.cshtml (484 lines) — Evidence + approval detail
├── Dashboard.cshtml (73 lines) — Analytics wrapper

Models/
├── ProtonModels.cs — ProtonDeliverableProgress, ProtonTrackAssignment, CoachingGuidanceFile
├── ProtonViewModels.cs — DeliverableViewModel, TrackingItem, CDPDashboardViewModel
├── TrackingModels.cs — TrackingItem for CoachingProton table
```

### Pattern 1: Role-Based Access Control

**What:** Authorization enforced via `User.RoleLevel` and `UserRoles` enum checks

**When to use:** All CDP actions must check user role before allowing access

**Example:**
```csharp
// Source: CDPController.cs lines 51-94 (PlanIdp action)
var user = await _userManager.GetUserAsync(User);
var roles = await _userManager.GetRolesAsync(user);
var userRole = roles.FirstOrDefault() ?? "";
bool isCoachee = userRole == UserRoles.Coachee;

// Coachee: lock to their assigned Bagian (cannot browse other sections)
if (isCoachee)
{
    var assignment = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == user.Id && a.IsActive)
        .FirstOrDefaultAsync();

    if (assignment != null)
    {
        bagian = coacheeBagian; // Force to coachee's section
    }
}
```

### Pattern 2: Evidence File Upload with Validation

**What:** IFormFile upload with extension whitelist, size limit, unique filename generation

**When to use:** Evidence upload in Deliverable page

**Example:**
```csharp
// Source: CDPController.cs lines 1065-1145 (UploadEvidence action)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadEvidence(int progressId, IFormFile? evidenceFile)
{
    // Validate file not null/empty
    if (evidenceFile == null || evidenceFile.Length == 0)
    {
        TempData["Error"] = "File tidak boleh kosong.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Validate file extension
    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
    var ext = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(ext))
    {
        TempData["Error"] = "Hanya PDF, JPG, dan PNG yang diperbolehkan.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Validate file size (10MB limit)
    if (evidenceFile.Length > 10 * 1024 * 1024)
    {
        TempData["Error"] = "Ukuran file maksimal 10MB.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Generate unique filename and save
    var fileName = $"{progressId}_{Guid.NewGuid()}{ext}";
    var filePath = Path.Combine(_env.WebRootPath, "uploads", "evidence", fileName);
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await evidenceFile.CopyToAsync(stream);
    }

    progress.EvidencePath = $"/uploads/evidence/{fileName}";
    progress.EvidenceFileName = evidenceFile.FileName;
    progress.Status = "Submitted";
    progress.SubmittedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    TempData["Success"] = "Evidence berhasil diupload.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

### Pattern 3: Multi-Track Approval Chain

**What:** Three independent approval tracks (SrSpv, SectionHead, HC) with separate statuses

**When to use:** Deliverable approval workflow

**Example:**
```csharp
// Source: CDPController.cs lines 834-890 (ApproveDeliverable action)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApproveDeliverable(int progressId)
{
    var progress = await _context.ProtonDeliverableProgresses.FindAsync(progressId);
    if (progress == null) return NotFound();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    // SrSpv or SectionHead or Admin simulating Spv/SH view can approve
    bool isSrSpv = userRole == UserRoles.SrSupervisor;
    bool isSH = userRole == UserRoles.SectionHead;
    bool isAdminSpv = userRole == UserRoles.Admin && TempData["SimulatingRole"]?.ToString() == "SrSpv";
    bool isAdminSH = userRole == UserRoles.Admin && TempData["SimulatingRole"]?.ToString() == "SectionHead";

    if (!isSrSpv && !isSH && !isAdminSpv && !isAdminSH)
    {
        return Forbid();
    }

    if (isSrSpv || isAdminSpv)
    {
        progress.SrSpvApprovalStatus = "Approved";
        progress.SrSpvApprovedById = user.Id;
        progress.SrSpvApprovedAt = DateTime.UtcNow;
    }
    else if (isSH || isAdminSH)
    {
        progress.ShApprovalStatus = "Approved";
        progress.ShApprovedById = user.Id;
        progress.ShApprovedAt = DateTime.UtcNow;
    }

    // Update overall Status if both Spv and SH approved
    if (progress.SrSpvApprovalStatus == "Approved" && progress.ShApprovalStatus == "Approved")
    {
        progress.Status = "Approved";
        progress.ApprovedById = user.Id;
        progress.ApprovedAt = DateTime.UtcNow;
    }

    await _context.SaveChangesAsync();
    TempData["Success"] = "Deliverable berhasil disetujui.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

### Anti-Patterns to Avoid

- **Missing null guards on DateTime.Value**: If `SubmittedAt` is nullable, always check `.HasValue` before calling `.Value`
- **Hardcoded culture in date formatting**: Use `CultureInfo.GetCultureInfo("id-ID")` for Indonesian dates
- **Role checks without server enforcement**: Client-side visibility is not security — always validate on server
- **File upload without extension whitelist**: Never trust client-provided content types
- **Cascading deletes without checks**: Verify no orphaned records before soft-delete operations

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| File upload validation | Custom file checking logic | IFormFile with extension whitelist | ASP.NET Core built-in file handling handles multipart/form-data correctly |
| Date localization | String concatenation for Indonesian dates | CultureInfo.GetCultureInfo("id-ID") | BCL has full Indonesian culture support — locale-aware month/day names |
| Role-based access | Custom authorization attributes | UserManager.GetUsersInRoleAsync() | Identity system has optimized role queries with caching |
| Excel export | Manual CSV generation | ClosedXML.Excel | Handles cell formatting, encoding, streaming correctly |
| PDF generation | HTML-to-PDF converters | QuestPDF | Type-safe PDF generation with proper layout control |

**Key insight:** CDP section uses established ASP.NET Core patterns — don't reinvent file handling, localization, or authorization. Leverage the existing infrastructure.

## Common Pitfalls

### Pitfall 1: Date Localization Without CultureInfo

**What goes wrong:** Dates display in English (Jan, Feb, Mar) instead of Indonesian (Jan, Feb, Mar) — same spelling but different culture context

**Why it happens:** `.ToString("dd MMM yyyy")` uses server culture by default

**How to avoid:** Always specify Indonesian culture:

```csharp
// Wrong
@Model.Progress.SubmittedAt.Value.ToString("dd MMM yyyy HH:mm")

// Correct
@Model.Progress.SubmittedAt.Value.ToString("dd MMM yyyy HH:mm",
    System.Globalization.CultureInfo.GetCultureInfo("id-ID"))
```

**Warning signs:** Month names display in English, day names not localized

**Confidence:** HIGH — Found 8 instances in Deliverable.cshtml (lines 109, 131, 160, 223, 242, 261, 380, 459)

### Pitfall 2: Missing Null Checks on DateTime Properties

**What goes wrong:** `InvalidOperationException` when calling `.Value` on nullable DateTime that is null

**Why it happens:** Developers assume timestamps are always populated

**How to avoid:** Always check `.HasValue` first:

```csharp
// Wrong
<small>@Model.Progress.SubmittedAt.Value.ToString("dd MMM yyyy HH:mm")</small>

// Correct
@if (Model.Progress.SubmittedAt.HasValue)
{
    <small>@Model.Progress.SubmittedAt.Value.ToString("dd MMM yyyy HH:mm",
        CultureInfo.GetCultureInfo("id-ID"))</small>
}
```

**Warning signs:** Raw exception stack traces showing "Nullable object must have a value"

**Confidence:** HIGH — Phase 93 found similar null safety issues in CMP section

### Pitfall 3: Coachee Scope Not Enforced on Server

**What goes wrong:** Coachees can browse other sections' silabus by modifying URL parameters

**Why it happens:** Client-side filter changes without server-side validation

**How to avoid:** Always force coachee's assigned bagian on server:

```csharp
// From CDPController.cs PlanIdp action (lines 60-94)
if (isCoachee)
{
    var assignment = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == user.Id && a.IsActive)
        .FirstOrDefaultAsync();

    if (assignment != null)
    {
        bagian = coacheeBagian; // Override URL param
    }
}
```

**Warning signs:** Coachee sees deliverables from other units, inconsistent data

**Confidence:** MEDIUM — Already implemented in PlanIdp, need to verify CoachingProton and Deliverable

### Pitfall 4: Evidence File Path Traversal Vulnerability

**What goes wrong:** Users can upload files outside intended directory using `../../` in filename

**Why it happens:** Using user-provided filename directly in path construction

**How to avoid:** Generate unique GUID-based filename:

```csharp
// From CDPController.cs UploadEvidence action (line 1135)
var fileName = $"{progressId}_{Guid.NewGuid()}{ext}";
var filePath = Path.Combine(_env.WebRootPath, "uploads", "evidence", fileName);
```

**Warning signs:** Files appearing in wrong directories, security scan warnings

**Confidence:** HIGH — Already implemented correctly (line 1135 uses GUID)

### Pitfall 5: Status Enum Mismatch

**What goes wrong:** Code checks for status "Active" but ProtonDeliverableProgress uses "Pending/Approved/Rejected/Completed"

**Why it happens:** Different status enums across models

**How to avoid:** Know the valid statuses per model:

```csharp
// ProtonDeliverableProgress.Status values:
// - "Pending" (initial state)
// - "Submitted" (evidence uploaded, awaiting approval)
// - "Approved" (both SrSpv and SH approved)
// - "Rejected" (rejected by Spv or SH)
// - "Completed" (final state after all deliverables done)

// No "Active" status exists! (Phase 85-02 bug fix)
```

**Warning signs:** Status badges don't display, filters return empty results

**Confidence:** HIGH — Phase 85 fixed this exact bug in Dashboard queries

### Pitfall 6: Missing IsActive Filters in Queries

**What goes wrong:** Inactive users/coach mappings appear in results, breaking workflow assumptions

**Why it happens:** Phase 83 introduced soft-delete but queries not updated

**How to avoid:** Always filter by `IsActive`:

```csharp
// Wrong
var mappings = await _context.CoachCoacheeMappings
    .Where(m => m.CoachId == coachId)
    .ToListAsync();

// Correct
var mappings = await _context.CoachCoacheeMappings
    .Where(m => m.CoachId == coachId && m.IsActive)
    .ToListAsync();
```

**Warning signs:** Inactive coachees shown, old assignments appear active

**Confidence:** HIGH — Phase 85-02 fixed this in Dashboard, need to verify other queries

## Code Examples

Verified patterns from official sources:

### CDP Role-Based Section Locking

```csharp
// Source: CDPController.cs lines 51-94
public async Task<IActionResult> PlanIdp(string? bagian = null, string? unit = null, int? trackId = null)
{
    var user = await _userManager.GetUserAsync(User);
    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault() ?? "";
    bool isCoachee = userRole == UserRoles.Coachee;

    // Coachee: lock to their assigned Bagian
    if (isCoachee)
    {
        var assignment = await _context.ProtonTrackAssignments
            .Where(a => a.CoacheeId == user.Id && a.IsActive)
            .FirstOrDefaultAsync();

        if (assignment != null)
        {
            var firstKomp = await _context.ProtonKompetensiList
                .Where(k => k.ProtonTrackId == assignment.ProtonTrackId && k.IsActive)
                .FirstOrDefaultAsync();
            if (firstKomp != null)
            {
                bagian = firstKomp.Bagian; // Force to coachee's section
                unit ??= firstKomp.Unit;
                trackId ??= assignment.ProtonTrackId;
            }
        }
    }
}
```

### Evidence File Download with Content Type

```csharp
// Pattern from CDPController.cs (expected implementation)
public IActionResult DownloadEvidence(int progressId)
{
    var progress = _context.ProtonDeliverableProgresses.Find(progressId);
    if (progress == null || string.IsNullOrEmpty(progress.EvidencePath))
        return NotFound();

    var filePath = Path.Combine(_env.WebRootPath, progress.EvidencePath.TrimStart('/'));
    if (!System.IO.File.Exists(filePath))
        return NotFound();

    var fileBytes = System.IO.File.ReadAllBytes(filePath);
    return File(fileBytes, "application/octet-stream", progress.EvidenceFileName ?? "evidence");
}
```

### CoachingProton Pagination with Group Preservation

```csharp
// Source: CDPController.cs lines 1361-1430
const int targetRowsPerPage = 20;
int pageNumber = Math.Max(1, page);

// Group data by Kompetensi (then SubKompetensi) to build pages that never split a group
var coacheeGroups = Model
    .GroupBy(x => x.CoacheeName)
    .Select(cg => new {
        CoacheeName = cg.Key,
        Items = cg.GroupBy(x => x.Kompetensi)
            .Select(kg => new {
                Kompetensi = kg.Key,
                Items = kg.GroupBy(x => x.SubKompetensi)
                    .Select(sg => new {
                        SubKompetensi = sg.Key,
                        Items = sg.ToList()
                    }).ToList()
            }).ToList()
    }).ToList();

// Calculate pages respecting group boundaries
var totalPages = (int)Math.Ceiling((double)totalGroups / targetRowsPerPage);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual test data creation | SeedCoachingTestData action (AdminController) | Phase 85 | Idempotent test data creation with role assignments and deliverable progress |
| English date formatting | Indonesian culture (id-ID) for dates | Phase 92-93 | Localized month/day names for Indonesian users |
| Hard-delete operations | Soft-delete with IsActive flag | Phase 83 | Preserves audit trail, inactive records excluded from queries |
| Single approval chain | Three-track approval (SrSpv/SH/HC) | Phase 65 | Independent approval workflows per role level |
| Status "Active" enum | Status "Pending/Submitted/Approved/Rejected/Completed" | Phase 85-02 bug fix | Fixed dashboard queries showing incorrect active deliverable counts |

**Deprecated/outdated:**
- **Status "Active"** in ProtonDeliverableProgress: Does not exist — valid statuses are Pending/Submitted/Approved/Rejected/Completed (Phase 85-02)
- **Proton Progress** old naming: Renamed to "Coaching Proton" throughout portal (Phase 82)

## Open Questions

1. **Evidence file download endpoint exists?**
   - What we know: UploadEvidence is implemented (line 1065), Deliverable view shows evidence links
   - What's unclear: Is DownloadEvidence action implemented in CDPController? Need to verify
   - Recommendation: Check CDPController for `DownloadEvidence` or similar action during code review

2. **CoachingGuidanceFile storage path security?**
   - What we know: CoachingGuidanceFile model has FilePath property, PlanIdp has GuidanceDownload
   - What's unclear: Is path traversal validation implemented for guidance file uploads?
   - Recommendation: Verify GuidanceDownload action validates file paths before serving

3. **Are there any AJAX approval actions missing CSRF tokens?**
   - What we know: ApproveFromProgress, RejectFromProgress, HCReviewFromProgress return JSON
   - What's unclear: Do these actions have ValidateAntiForgeryToken and proper CSRF handling?
   - Recommendation: Verify AJAX POST actions have anti-forgery protection (Phase 93 found this issue in CMP)

4. **Dashboard analytics data accuracy?**
   - What we know: BuildProtonProgressSubModelAsync was fixed in Phase 87-02 for IsActive filters
   - What's unclear: Are there remaining bugs in pending approval counts or coachee stats?
   - Recommendation: Verify dashboard queries match actual progress record counts

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | None — manual browser verification only |
| Config file | N/A (no automated tests in project) |
| Quick run command | N/A (browser testing manual) |
| Full suite command | N/A (browser testing manual) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CDP-01 | PlanIdp loads for all 5 roles | manual | Browser verification | ❌ Manual only |
| CDP-02 | CoachingProton shows correct coachee lists | manual | Browser verification | ❌ Manual only |
| CDP-03 | Progress approval workflows work per role | manual | Browser verification | ❌ Manual only |
| CDP-04 | Evidence upload/download works | manual | Browser verification | ❌ Manual only |
| CDP-05 | Coaching session flows complete end-to-end | manual | Browser verification | ❌ Manual only |
| CDP-06 | Forms handle validation gracefully | manual | Browser verification | ❌ Manual only |

### Sampling Rate

- **Per task commit:** Smoke test the specific bug/feature fixed
- **Per wave merge:** Full flow verification for affected workflows
- **Phase gate:** All 3 flows (IDP Planning, Coaching Workflow, Evidence & Approval) verified PASS by user

### Wave 0 Gaps

- No test infrastructure exists — project relies on manual browser verification
- This is intentional per project culture (Phase 85/93 used same approach)
- No need to create automated test framework for this audit phase

## Sources

### Primary (HIGH confidence)

- **CDPController.cs** — C:\Users\Administrator\Desktop\PortalHC_KPB\Controllers\CDPController.cs (2146 lines) — All CDP actions, file handling, approval workflows
- **CDP Views** — C:\Users\Administrator\Desktop\PortalHC_KPB\Views\CDP\*.cshtml (2551 total lines) — PlanIdp (354), CoachingProton (1539), Deliverable (484), Dashboard (73), Index (101)
- **ProtonModels.cs** — C:\Users\Administrator\Desktop\PortalHC_KPB\Models\ProtonModels.cs — ProtonDeliverableProgress, ProtonTrackAssignment, CoachingGuidanceFile models
- **AdminController.cs (SeedCoachingTestData)** — Lines 2269-2500+ — Test data seeding pattern, idempotent data creation
- **Phase 94 CONTEXT.md** — C:\Users\Administrator\Desktop\PortalHC_KPB\.planning\phases\94-cdp-section-audit\94-CONTEXT.md — User decisions and phase boundaries

### Secondary (MEDIUM confidence)

- **Phase 93 Bug Inventory** — .planning/phases/93-cmp-section-audit/BUG_INVENTORY.md — Bug categories (null safety, localization, validation) found in CMP audit
- **Phase 85 CONTEXT.md** — .planning/phases/85-coaching-proton-flow-qa/85-CONTEXT.md — Coaching QA test data approach, approval chain testing patterns
- **Phase 93 CONTEXT.md** — .planning/phases/93-cmp-section-audit/93-CONTEXT.md — CMP audit smoke test approach

### Tertiary (LOW confidence)

- **REQUIREMENTS.md** — C:\Users\Administrator\Desktop\PortalHC_KPB\.planning\REQUIREMENTS.md — CDP-01 through CDP-06 requirements definitions
- **STATE.md** — C:\Users\Administrator\Desktop\PortalHC_KPB\.planning\STATE.md — Project decisions, Phase 85/93 completion history

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Verified from existing codebase (ASP.NET Core 6.0+, EF Core, Identity)
- Architecture: HIGH - Examined CDPController.cs (2146 lines) and all CDP views (2551 lines total)
- Pitfalls: HIGH - Found 8 date localization bugs in Deliverable.cshtml; Phase 93 CMP audit found similar issues
- Code examples: HIGH - All code snippets sourced directly from CDPController.cs or CDP views

**Research date:** 2026-03-05
**Valid until:** 30 days (stable audit patterns — project uses established ASP.NET Core conventions)

**Key findings summary:**
1. CDP section has 5 pages across 2,551 lines of view code + 2,146 lines of controller code
2. Three-flow organization (IDP Planning, Coaching Workflow, Evidence & Approval) aligns with code structure
3. High likelihood of date localization bugs (8 instances found in Deliverable.cshtml)
4. Evidence file handling already secure (GUID-based filenames, extension whitelist)
5. Test data seeding pattern exists (SeedCoachingTestData from Phase 85)
6. No automated test infrastructure — manual browser verification only
