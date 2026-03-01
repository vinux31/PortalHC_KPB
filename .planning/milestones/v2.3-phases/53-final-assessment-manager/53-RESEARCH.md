# Phase 53: Final Assessment Manager - Research

**Researched:** 2026-02-28
**Domain:** Assessment system extension + examination intake workflow
**Confidence:** HIGH

## Summary

Phase 53 transforms from "admin management of ProtonFinalAssessment records" (original OPER-04) to "Proton Assessment Exam" — adding exam capability to the existing Assessment/Exam infrastructure. This is not a new data model but a category-driven adaptation: the system treats "Assessment Proton" as a new assessment category with context-aware form fields (Track + Tahun selection), 2-tier exam types (online for Tahun 1-2, offline interview for Tahun 3), and eligibility gating based on deliverable completion (100% approved Proton deliverables). HC and Admin access is unified in ManageAssessment with identical permissions. The phase also removes the legacy CreateFinalAssessment and HCApprovals pages, consolidating HC review into ProtonProgress.

**Primary recommendation:** Extend AssessmentSession.Category enum to include "Assessment Proton"; adapt CreateAssessment form to show Track/Tahun dropdowns when category="Assessment Proton"; implement eligibility filter (coachee must have all deliverables Approved) in user picker; add interview-specific form in AssessmentMonitoringDetail for Tahun 3 results input; move HC review logic from HCApprovals to ProtonProgress modal/panel.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Exam Types:**
- Tahun 1 & 2 (Operator/Panelman): Online multiple-choice using existing assessment system (packages, shuffling, timer, auto-save, scoring)
- Tahun 3: Offline interview with manual HC input (judges, aspect ratings fixed, notes, document upload)
- Questions per track differ; HC selects at creation
- Passing grade configurable per exam (default 70%)
- Failed coachees can retake unlimited times

**Eligibility:**
- Only coachees with 100% deliverable approval eligible for Proton exam assignment
- Auto-filtered coachee list on CreateAssessment based on Track & Tahun

**Form Adaptation:**
- When category="Assessment Proton": show Track dropdown (Operator/Panelman), Tahun dropdown (1/2/3)
- Tahun 1-2: Duration, Pass%, Schedule fields visible
- Tahun 3: Schedule only (no Duration/questions)
- User picker displays only eligible coachees (100% deliverables, matching Track/Tahun)
- All eligible coachees selectable (no section restriction)

**Tahun 3 Interview Input:**
- HC inputs results in same monitoring page (AssessmentMonitoringDetail)
- Form: judge list, per-aspect ratings (fixed aspects, Claude discretion), notes, document upload
- Pass/fail: HC decides manually (not auto-scored)
- Aspect scores informational only

**Access & Permissions:**
- HC + Admin: identical full access (create, edit, delete, monitor, export, reset, force-close)
- Via Kelola Data Hub (Admin/Index)
- Other roles: no access

**Category Integration:**
- Category name: "Assessment Proton"
- ManageAssessment: mixed with all assessments, category filter dropdown
- CMP/Assessment (coachee): Proton exams appear with "Assessment Proton" badge
- Tahun 3 appears as "Interview" status (no "Start Exam" button)
- Coachee pre-interview: see schedule info; post-HC input: see results

**Delete Page & Data:**
- CreateFinalAssessment page + controller action: delete
- HCApprovals page: delete entirely
- HC review via ProtonProgress (not HCApprovals)
- ProtonFinalAssessment data: delete legacy records; table may be dropped
- "Siap untuk Final Assessment" section: remove (not migrate)

**Acknowledge Flow:**
- None — no acknowledge mechanism post-exam

### Claude's Discretion

- Fixed aspect list for Tahun 3 interview (Claude determines appropriate aspects)
- UI implementation details (form layout, badge styling, responsive design)
- Monitoring detail adaptation for interview result input
- Coachee eligibility query optimization

### Deferred Ideas (OUT OF SCOPE)

- History timeline per coachee (Proton progress + deliverable history + exam results) — separate phase
- Notification system (HC alert on 100% deliverables) — separate phase
- Eligible coachee list page — implicit via filter in ProtonProgress or CreateAssessment

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| OPER-04 | Admin can view, approve, reject, and edit ProtonFinalAssessment records | Changed scope: ProtonFinalAssessment table remains but is not user-facing; Proton exam replaces it as primary assessment intake. Assessment system extends to handle Proton category with integrated result tracking. |

</phase_requirements>

## Standard Stack

### Core Libraries
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core | 6.0+ | MVC controller/view framework | Already used for AdminController (ManageAssessment, CreateAssessment, EditAssessment) |
| Entity Framework Core | 6.0+ | ORM for ProtonDeliverableProgress, AssessmentSession, ProtonTrackAssignment queries | Existing pattern for all Proton data access (seen in CDPController, AdminController) |
| Bootstrap 5 | 5.3+ | Responsive form layout, modals | Consistent with all existing admin UI (CreateAssessment, AssessmentMonitoringDetail already use Bootstrap forms) |
| jQuery + AJAX | (in project) | Client-side modal/form handling, dynamic field visibility | Already used in ManageWorkers (modal assign/edit), CoachCoacheeMapping (bulk actions), CreateAssessment (user selection) |

### Supporting Libraries
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ClosedXML | 0.105.0+ | Excel export for exam results | Already in codebase for CpdpItems export; reuse for Proton exam export |
| QuestPDF | (if in project) | PDF export for exam history | If project uses for other reports; check appsettings for PDF config |

### No New Packages Required
- All core infrastructure exists (AssessmentSession, AssessmentPackage, ProtonDeliverableProgress, ProtonTrackAssignment)
- No ORM changes, validation frameworks, or UI libraries needed

## Architecture Patterns

### Recommended Project Structure

Existing structure remains; no new folders:
```
Controllers/
├── AdminController.cs         # CreateAssessment, EditAssessment, ManageAssessment (extend category logic)
└── CDPController.cs           # ProtonProgress (add HC review modal for deliverables)

Models/
├── AssessmentSession.cs       # Extend: add ProtonTrack FK for Proton category sessions
├── ProtonModels.cs            # ProtonTrackAssignment, ProtonDeliverableProgress (existing)
└── ProtonViewModels.cs        # Add: ProtonExamViewModel, InterviewResultsViewModel (new)

Views/
├── Admin/
│   ├── CreateAssessment.cshtml      # Extend: add Track/Tahun conditionals for Proton category
│   ├── ManageAssessment.cshtml      # Extend: add Proton badge for category "Assessment Proton"
│   └── AssessmentMonitoringDetail.cshtml  # Extend: add interview results form for Tahun 3
├── CDP/
│   └── ProtonProgress.cshtml        # Extend: add HC review modal (move from HCApprovals)
└── (DELETE)
    ├── CreateFinalAssessment.cshtml  # Delete entirely
    └── HCApprovals.cshtml           # Delete entirely

Migrations/
├── AddProtonCategoryAssessment      # Add ProtonTrackId to AssessmentSession
└── DeleteLegacyFinalAssessmentData  # Remove ProtonFinalAssessment records
```

### Pattern 1: Category-Driven Form Adaptation

**What:** Use AssessmentSession.Category field to conditionally show/hide form fields and alter user picker logic. This pattern avoids creating separate assessment types (AssessmentProton, TrainingAssessment, etc.) and instead extends the existing unified Category field.

**When to use:** Multi-category systems where form shape differs per category (like this project: OJT/IHT/Proton all use AssessmentSession but have different field requirements).

**Example:**

```csharp
// AdminController.CreateAssessment GET
public async Task<IActionResult> CreateAssessment()
{
    var model = new AssessmentSession { /* defaults */ };

    // Standard form prep
    ViewBag.Categories = new List<SelectListItem>
    {
        new SelectListItem { Value = "OJT", Text = "OJT" },
        // ... existing categories
        new SelectListItem { Value = "Assessment Proton", Text = "Assessment Proton" }
    };

    // Proton-specific prep (lazy-loaded by JS on category change)
    if (HttpContext.Request.Query.TryGetValue("category", out var catVal) && catVal == "Assessment Proton")
    {
        ViewBag.Tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
        ViewBag.Years = new List<SelectListItem>
        {
            new SelectListItem { Value = "Tahun 1", Text = "Tahun 1" },
            new SelectListItem { Value = "Tahun 2", Text = "Tahun 2" },
            new SelectListItem { Value = "Tahun 3", Text = "Tahun 3" }
        };
    }

    return View(model);
}

// CreateAssessment.cshtml
@{
    var isProtonCategory = Model.Category == "Assessment Proton";
}

<select id="categoryDropdown" asp-for="Category" class="form-select">
    @Html.DisplayFor(m => ViewBag.Categories)
</select>

<div id="protonFields" class="@(isProtonCategory ? "" : "d-none")">
    <label>Track</label>
    <select id="trackSelect" class="form-select" name="ProtonTrackId">
        <!-- Populated by ViewBag.Tracks or AJAX -->
    </select>

    <label>Tahun</label>
    <select id="tahunSelect" class="form-select" name="TahunKe">
        <option value="Tahun 1">Tahun 1</option>
        <option value="Tahun 2">Tahun 2</option>
        <option value="Tahun 3">Tahun 3</option>
    </select>

    <div id="durationField" class="@(Model.Category == "Assessment Proton" && (Model.TahunKe == "Tahun 1" || Model.TahunKe == "Tahun 2") ? "" : "d-none")">
        <label>Duration (minutes)</label>
        <input type="number" asp-for="DurationMinutes" class="form-control">
    </div>
</div>

<script>
document.getElementById('categoryDropdown').addEventListener('change', function() {
    const isProton = this.value === 'Assessment Proton';
    document.getElementById('protonFields').classList.toggle('d-none', !isProton);
    if (isProton) {
        // Trigger eligible coachee filter via AJAX
        filterEligibleCoachees();
    } else {
        // Show all users
        showAllUsers();
    }
});

document.getElementById('tahunSelect').addEventListener('change', function() {
    const durationField = document.getElementById('durationField');
    const isYearOJ = ['Tahun 1', 'Tahun 2'].includes(this.value);
    durationField.classList.toggle('d-none', !isYearOJ);
});
</script>
```

Source: Pattern observed in CreateAssessment.cshtml (lines 6-23) where categories already hardcode a list. Proton adds conditional visibility.

### Pattern 2: Eligibility-Filtered User Picker

**What:** Filter the coachee/user list displayed in CreateAssessment based on eligibility criteria (100% deliverables approved + matching Track/Tahun). This avoids showing ineligible users and prevents HC from mistakenly assigning exams to users not ready.

**When to use:** Multi-user role-based systems where certain workflows require pre-conditions (here: Proton exam eligibility).

**Example:**

```csharp
// AdminController.CreateAssessment POST or AJAX endpoint
[HttpPost]
public async Task<IActionResult> GetEligibleCoachees(string trackId, string tahun)
{
    var track = await _context.ProtonTracks.FindAsync(int.Parse(trackId));
    if (track == null) return Json(new { error = "Track not found" });

    // Find coachees assigned to this track
    var trackAssignments = await _context.ProtonTrackAssignments
        .Where(a => a.ProtonTrackId == int.Parse(trackId) && a.IsActive)
        .Select(a => a.CoacheeId)
        .ToListAsync();

    // Filter: 100% deliverables approved for this track
    var eligibleCoachees = new List<string>();

    foreach (var coacheeId in trackAssignments)
    {
        var deliverables = await _context.ProtonDeliverableProgresses
            .Where(p => p.CoacheeId == coacheeId
                     && p.ProtonDeliverable.ProtonSubKompetensi.ProtonKompetensi.ProtonTrackId == int.Parse(trackId))
            .ToListAsync();

        // All deliverables must be "Approved" (not just submitted or pending)
        if (deliverables.Any() && deliverables.All(d => d.Status == "Approved"))
        {
            eligibleCoachees.Add(coacheeId);
        }
    }

    // Load user details for eligible coachees
    var users = await _context.Users
        .Where(u => eligibleCoachees.Contains(u.Id))
        .Select(u => new { u.Id, u.FullName, u.Email })
        .OrderBy(u => u.FullName)
        .ToListAsync();

    return Json(users);
}
```

### Pattern 3: Monitoring Detail Interview Result Input

**What:** Extend AssessmentMonitoringDetail view to include a form for HC to input Tahun 3 interview results (judges, aspect scores, notes, file uploads). This form appears ONLY when exam type is Tahun 3 and status allows input (e.g., "Completed" but not yet graded).

**When to use:** Assessment systems with multi-type exams where some types require manual input (offline interviews, practicals, etc.).

**Example:**

```csharp
// AdminController.AssessmentMonitoringDetail GET
public async Task<IActionResult> AssessmentMonitoringDetail(int id)
{
    var session = await _context.AssessmentSessions.FindAsync(id);
    // ... existing logic

    // NEW: If Proton Tahun 3, load interview aspects
    if (session.Category == "Assessment Proton")
    {
        ViewBag.InterviewAspects = new List<string>
        {
            "Pengetahuan Teknis",
            "Kemampuan Komunikasi",
            "Kepemimpinan",
            "Integritas"
            // Claude determines these
        };
    }

    return View(viewModel);
}

// AssessmentMonitoringDetail.cshtml
@if (Model.Session.Category == "Assessment Proton" && Model.Session.TahunKe == "Tahun 3")
{
    <div class="card mt-4">
        <div class="card-header">
            <h5>Hasil Interview</h5>
        </div>
        <div class="card-body">
            <form asp-action="SubmitInterviewResults" asp-controller="Admin" method="post" enctype="multipart/form-data">
                <input type="hidden" name="sessionId" value="@Model.Session.Id">

                <div class="mb-3">
                    <label>Daftar Juri</label>
                    <input type="text" name="judges" class="form-control" placeholder="Nama juri, dipisahkan dengan koma">
                </div>

                @foreach (var aspect in ViewBag.InterviewAspects)
                {
                    <div class="mb-3">
                        <label>@aspect</label>
                        <select name="aspectScore_@aspect" class="form-select">
                            <option value="1">1 - Kurang</option>
                            <option value="2">2 - Cukup</option>
                            <option value="3">3 - Baik</option>
                            <option value="4">4 - Sangat Baik</option>
                            <option value="5">5 - Luar Biasa</option>
                        </select>
                    </div>
                }

                <div class="mb-3">
                    <label>Catatan</label>
                    <textarea name="notes" class="form-control" rows="4"></textarea>
                </div>

                <div class="mb-3">
                    <label>Upload Dokumen Pendukung</label>
                    <input type="file" name="supportingDocs" class="form-control" multiple>
                </div>

                <div class="mb-3">
                    <label>
                        <input type="checkbox" name="isPassed"> Lulus
                    </label>
                </div>

                <button type="submit" class="btn btn-primary">Simpan Hasil</button>
            </form>
        </div>
    </div>
}
```

Source: Pattern based on existing AssessmentMonitoringDetail.cshtml which already displays per-session details and allows admin actions.

### Pattern 4: HC Review Modal in ProtonProgress

**What:** Move HC review from standalone HCApprovals page into a modal/collapse panel in ProtonProgress, allowing HC to review deliverable approvals inline while managing coachee progress. This consolidates two related workflows (progress view + approval review) into one place.

**When to use:** Approval workflows where reviewers need context (related data, progress status) to make decisions — better UX than separate pages.

**Example:**

```csharp
// CDPController.ProtonProgress — existing action
// Add: HC review modal data when HC user accesses page

var hcReviewData = new List<object>();
if (userLevel <= 2) // HC/Admin
{
    hcReviewData = await _context.ProtonDeliverableProgresses
        .Where(p => p.HCApprovalStatus == "Pending"
                 && scopedCoacheeIds.Contains(p.CoacheeId))
        .Select(p => new {
            p.Id,
            p.CoacheeId,
            CoacheeName = p.CoacheeId, // Batch-load names
            p.ProtonDeliverable.NamaDeliverable,
            p.Status,
            SubmittedAt = p.SubmittedAt ?? DateTime.UtcNow
        })
        .ToListAsync();
}

ViewBag.HCReviewData = hcReviewData;
```

```html
<!-- ProtonProgress.cshtml -->
@if (User.IsInRole("HC") || User.IsInRole("Admin"))
{
    <div class="alert alert-info">
        <h5>HC Review Queue (@ViewBag.HCReviewData?.Count ?? 0 pending)</h5>
        @if (ViewBag.HCReviewData?.Count > 0)
        {
            <ul class="list-group mt-2">
                @foreach (var item in ViewBag.HCReviewData)
                {
                    <li class="list-group-item d-flex justify-content-between align-items-center">
                        @item.CoacheeName - @item.NamaDeliverable
                        <button type="button" class="btn btn-sm btn-primary" data-bs-toggle="modal" data-bs-target="#reviewModal" data-item-id="@item.Id">
                            Review
                        </button>
                    </li>
                }
            </ul>
        }
    </div>
}

<!-- Review Modal -->
<div class="modal fade" id="reviewModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">HC Review Deliverable</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <form asp-action="SubmitHCReview" method="post">
                <div class="modal-body">
                    <input type="hidden" name="progressId" id="reviewProgressId">
                    <div class="mb-3">
                        <label class="form-label">Status</label>
                        <select name="hcApprovalStatus" class="form-select">
                            <option value="Reviewed">Reviewed</option>
                        </select>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                    <button type="submit" class="btn btn-primary">Mark Reviewed</button>
                </div>
            </form>
        </div>
    </div>
</div>

<script>
const reviewModal = document.getElementById('reviewModal');
reviewModal?.addEventListener('show.bs.modal', (event) => {
    const itemId = event.relatedTarget.dataset.itemId;
    document.getElementById('reviewProgressId').value = itemId;
});
</script>
```

Source: Pattern based on existing ProtonProgress modals for coachee selection (line 1493 in CDPController shows dropdown population); mirrors ManageWorkers modal pattern (Plan 50-02).

### Anti-Patterns to Avoid

- **Creating separate "ProtonAssessment" model:** Don't. Reuse AssessmentSession.Category. Adding new assessment types via models creates N+1 code paths and duplicate validation/UI logic.
- **Pre-loading all eligible coachees on GET:** Don't. Load on category+track selection via AJAX. CreateAssessment GET with full user list is already expensive; filtering client-side is fragile.
- **Storing interview aspect choices in code:** Consider seeding a ProtonInterviewAspect table later (deferred phase), but for now Claude's fixed list is acceptable.
- **Deleting ProtonFinalAssessment table immediately:** Keep it; mark records as "legacy" or archive. Drop in future cleanup phase after all data exported.
- **Adding separate HC role checks:** Use existing role constants (UserRoles.HC, UserRoles.Admin). No new authorization patterns.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| User eligibility filtering | Custom eligibility logic per exam type | Entity Framework Where + filtering over ProtonDeliverableProgress & ProtonTrackAssignment | Business rules change (100% -> 80% thresholds, new track types); encapsulate in EF queries or service method |
| Multi-user form submission with validation | Manual loop with try-catch per user | EF AddRange + single SaveChangesAsync + transaction (Pattern 1 in CreateAssessment POST shows this at line 649) | Database consistency, rollback on partial failure; don't reinvent transaction handling |
| Interview result storage | New ProtonInterviewResult table | Store directly in AssessmentSession as JSON or extend ProtonDeliverableProgress.InterviewResults POCO | Exam results are session-scoped; cramming into separate table adds unnecessary joins |
| Category-based form visibility | Build separate views (CreateProtonAssessment.cshtml, CreateOJTAssessment.cshtml) | Single CreateAssessment.cshtml with conditionals on Category field | Duplication of validation, error handling, user picker logic; maintenance nightmare |
| HC review notification/queue | Custom notification service | Query ProtonDeliverableProgress.HCApprovalStatus == "Pending" on-demand in ProtonProgress GET | Notifications deferred to Phase 54; don't pre-build infrastructure |

**Key insight:** This phase is 90% form adaptation + query filtering, not new data structures. The infrastructure (AssessmentSession, ProtonDeliverableProgress, ProtonTrackAssignment) is already in place. Complexity comes from conditional UI + eligibility logic, not from building new entities.

## Common Pitfalls

### Pitfall 1: Mixing Track Selection UI with User Picker Logic

**What goes wrong:** HC selects Track/Tahun, but user picker doesn't filter and HC assigns to ineligible coachees. Or: user picker filters, but HC can't change Track/Tahun without resetting selections.

**Why it happens:** Form state management across dependent dropdowns is complex. Frontend JS and backend logic can diverge.

**How to avoid:**
1. Clear separation: Track/Tahun → eligibility filter → user selection (3 sequential steps)
2. Use AJAX to reload eligible users when Track/Tahun changes (don't rely on page load)
3. Backend validation: verify selected users match Track/Tahun before SaveChanges
4. Test matrix: Tahun 1 + Operator + different unit filters

**Warning signs:**
- Form submitted with "Track/Tahun mismatch: user assigned to wrong track"
- Users appearing/disappearing unexpectedly in picker when Track changes
- HC surprised that ineligible user was accepted

### Pitfall 2: Tahun 3 Interview Form Scope Creep

**What goes wrong:** Interview form starts as "judges + aspect scores + notes" but becomes "judges + aspect scores + notes + competency level + final sign-off + retake policy + approval chain" — adds weeks to implementation.

**Why it happens:** Interview is a new feature; HC asks "while we're at it, can we also..." and scope mushrooms.

**How to avoid:**
1. Lock the form to CONTEXT decisions: judges, aspects, notes, file upload, HC pass/fail decision — that's it
2. No signature workflows, competency grants, or approval chains in Phase 53
3. Deferred features (history timeline, notifications) go to separate phases
4. Do 3-step implementation: (1) form inputs, (2) storage, (3) coachee viewing results

**Warning signs:**
- Design doc growing beyond "interview results form"
- Questions like "do we need PDF report for interview?"
- Requests to link interview pass -> competency level -> KKJ mapping

### Pitfall 3: Deleting HCApprovals Page Without HC Migration Path

**What goes wrong:** HCApprovals deleted, HC confused about where to find "Pending Reviews" queue, workflow breaks for 1-2 days while HC relearns new location.

**Why it happens:** Pages deleted before replacement is working/tested.

**How to avoid:**
1. Add HC Review modal to ProtonProgress BEFORE deleting HCApprovals
2. Brief HC: "Same review queue, now in ProtonProgress page under section Y"
3. Test with HC user: full workflow from ProtonProgress -> review queue -> approve -> refresh
4. Delete HCApprovals.cshtml + controller action only after HC confirms new flow works

**Warning signs:**
- Phase gate test says "HCApprovals page not found" (expected after deletion)
- But HC review in ProtonProgress not tested
- No UAT step for HC review workflow

### Pitfall 4: Over-Engineering Tahun 1-2 Online Exams

**What goes wrong:** Trying to add Proton-specific question shuffling, marking, or scoring logic when the existing exam engine (AssessmentPackage + shuffling) already handles it.

**Why it happens:** Phase 53 is positioning as "new exam system" so developers assume it needs custom exam handling.

**How to avoid:**
- Tahun 1-2: Treat as normal Assessment category exams
- Use existing AssessmentPackage + PackageQuestion + shuffling
- Proton-specific: only Track/Tahun metadata + eligibility gating
- Interview (Tahun 3): custom form for manual input
- No new exam engine code

**Warning signs:**
- Discussion of "custom Proton question bank" or "Proton shuffling algorithm"
- New tables like "ProtonQuestion" or "ProtonExamRound"
- Code paths diverging (if category=="Assessment Proton" then custom shuffle logic)

## Code Examples

Verified patterns from project codebase:

### Category Enum in CreateAssessment.cshtml

```csharp
// Source: CreateAssessment.cshtml lines 6-23
var categories = new List<SelectListItem>
{
    new SelectListItem { Value = "OJT", Text = "OJT" },
    new SelectListItem { Value = "IHT", Text = "IHT" },
    new SelectListItem { Value = "Training Licencor", Text = "Training Licencor" },
    new SelectListItem { Value = "OTS", Text = "OTS" },
    new SelectListItem { Value = "Mandatory HSSE Training", Text = "Mandatory HSSE Training" },
    new SelectListItem { Value = "Proton", Text = "Proton" } // ADD THIS
};
```

### Multi-User Create with Transaction

```csharp
// Source: AdminController.CreateAssessment POST lines 649-656
_context.AssessmentSessions.AddRange(sessions);

using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
    // ... audit log
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### Eligibility Query Pattern (from ProtonProgress)

```csharp
// Source: CDPController.ProtonProgress lines 1417-1435
// Scope coachees by role level
List<string> scopedCoacheeIds;
if (userLevel <= 2) // HC/Admin
{
    scopedCoacheeIds = await _context.Users
        .Where(u => u.RoleLevel == 6)
        .Select(u => u.Id).ToListAsync();
}
else if (userLevel == 4) // SrSpv/SectionHead
{
    scopedCoacheeIds = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
        .Select(u => u.Id).ToListAsync();
}
// ... then apply Track filter if needed
```

Adapt this pattern for Proton exam eligibility:
1. Start with all CoachCoacheeMapping or ProtonTrackAssignment coachees
2. Filter where 100% deliverables are "Approved"
3. Further filter by Track/Tahun if selected

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ProtonFinalAssessment table as primary data source | AssessmentSession.Category="Assessment Proton" for Tahun 1-2; ProtonDeliverableProgress for Tahun 3 interview results | Phase 53 (this phase) | Legacy table can be archived; new exams flow through Assessment infrastructure |
| CreateFinalAssessment page | Removed; intake via CreateAssessment with Proton category | Phase 53 | Unified assessment creation UX |
| HCApprovals page for HC review queue | Integrated into ProtonProgress modal/panel | Phase 53 | Fewer pages, better context for HC |
| ProtonFinalAssessment data entry (competency level, KKJ mapping) | Deferred; Phase 53 stores interview results only; competency granting is future phase | Phase 53 | Separates "exam intake + grading" from "competency certification" |

**Deprecated/outdated:**
- CreateFinalAssessment controller action and view — replaced by extended CreateAssessment
- HCApprovals controller action and view — replaced by ProtonProgress HC review modal
- Per-coachee "Final Assessment" workflow — unified into assessment system

## Open Questions

1. **Interview Aspect List:** CONTEXT says Claude determines aspects. Current thinking: "Pengetahuan Teknis, Komunikasi, Kepemimpinan, Integritas" — verify these are appropriate for Proton interview context during planning.
   - What we know: Fixed list (not user-selectable); stored in code or seeded table
   - What's unclear: Exact aspect names, scoring scale (1-5? Yes/No?)
   - Recommendation: Seed a small ProtonInterviewAspect table (5 rows) in EF migration; makes future phase (aspect management) easier

2. **Interview Result Persistence:** Should interview results (judges, aspect scores, notes, file uploads) be stored in ProtonDeliverableProgress as a JSON column, or create ProtonInterviewResult table?
   - What we know: Data is session-scoped (one per exam per coachee); HC enters once; coachee views once
   - What's unclear: Future need to edit, re-rate, or show history
   - Recommendation: Store in ProtonDeliverableProgress as InterviewResultsJson POCO for Phase 53; extract to separate table in future "interview history" phase

3. **File Upload Directory:** Where to store interview support files (PDF, Excel, photos)?
   - What we know: Coaching guidance files stored in /uploads/guidance/ (Phase 51); evidence files in /uploads/evidence/
   - What's unclear: Consistent naming/organization for interview docs
   - Recommendation: /uploads/interviews/{sessionId}_{timestamp}_{originalname} — matches evidence pattern

4. **Tahun 3 Status in CMP:** Should Tahun 3 show as "Interview" or "Assessment Proton - Interview"?
   - What we know: CMP/Assessment shows badge "Assessment Proton" for Proton exams; Tahun 3 can't be started (no "Begin" button)
   - What's unclear: Exact status label for Tahun 3
   - Recommendation: Status="Interview" (short, clear); badge explains category

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (or project standard — check csproj) |
| Config file | None detected; tests likely in {project}.Tests project |
| Quick run command | `dotnet test --filter "Phase53" -x` (if tests tagged) |
| Full suite command | `dotnet test -x` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| OPER-04 | Admin can create Proton exam (Tahun 1-2 online) | Integration | AdminController_CreateAssessment_ProtonYearOneTwo_CreatesSession | ❌ Wave 0 |
| OPER-04 | Admin can create Proton exam (Tahun 3 interview) | Integration | AdminController_CreateAssessment_ProtonYear3_CreatesSession | ❌ Wave 0 |
| OPER-04 | Ineligible coachee filtered from Proton user picker | Unit | GetEligibleCoachees_RequiresApprovedDeliverables | ❌ Wave 0 |
| OPER-04 | HC can input Tahun 3 interview results | Integration | AdminController_SubmitInterviewResults_UpdatesProgress | ❌ Wave 0 |
| OPER-04 | HC can review deliverable in ProtonProgress modal | Integration | CDPController_ProtonProgress_HCReviewModal_Visible | ❌ Wave 0 |
| | | | | |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Phase53" -x` (unit + quick integration)
- **Per wave merge:** `dotnet test -x` (full suite)
- **Phase gate:** Full suite green + UAT with HC user before sign-off

### Wave 0 Gaps
- [ ] `{Tests/}AdminControllerTests.cs` — CreateAssessment with Proton category
- [ ] `{Tests/}CDPControllerTests.cs` — ProtonProgress HC review modal
- [ ] `{Tests/}ProtonEligibilityTests.cs` — GetEligibleCoachees filtering
- [ ] Framework setup (xUnit assertions, mocking DbContext, mocking UserManager)
- [ ] Integration test database seed (create test ProtonTrack, ProtonDeliverable, ProtonDeliverableProgress records)

*(Existing test infrastructure check needed — read *.csproj to identify test framework; if no tests exist, Wave 0 includes creating test project structure)*

## Sources

### Primary (HIGH confidence)
- **Codebase:** AdminController.cs (lines 446-723 CreateAssessment, 1561+ ManageAssessment) — established pattern for multi-user form submission, validation, audit logging
- **Codebase:** CDPController.cs (lines 1397-1550 ProtonProgress, 1061-1140 HCApprovals) — Proton progress querying, HC review queue logic
- **Codebase:** CreateAssessment.cshtml (lines 1-200) — form structure, category dropdown, user picker UX
- **Codebase:** ProtonModels.cs (all 192 lines) — ProtonTrack, ProtonDeliverableProgress, ProtonFinalAssessment entity structure
- **CONTEXT.md:** 53-CONTEXT.md (all sections) — locked decisions on Tahun 1-2 vs 3, eligibility rules, scope

### Secondary (MEDIUM confidence)
- **Codebase:** ManageAssessment.cshtml (lines 1-200) — category badge display pattern, table layout for Proton badge integration
- **Codebase:** AssessmentMonitoringDetail.cshtml — monitoring UI pattern for exam management
- **Codebase:** Phase 51 (Silabus) — file upload pattern (`/uploads/guidance/`) for interview document storage
- **Codebase:** CoachCoocheeMapping.cshtml (Plan 50-02) — bulk modal pattern for HC/Admin actions

### Tertiary (LOW confidence)
- **Training data:** ASP.NET Core form binding patterns for dynamic fields — verified in existing CreateAssessment but conditional Proton fields untested
- **Training data:** ProtonFinalAssessment deletion/archival best practices — deferred discussion, not immediate concern

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — ASP.NET Core + EF Core + Bootstrap already in project; no new packages needed
- Architecture: **HIGH** — Form adaptation pattern proven in CreateAssessment; eligibility filtering reuses existing CDPController patterns
- Pitfalls: **MEDIUM** — Track/Tahun UI coordination is new; interview form scope creep is anticipated risk; actual pitfall severity depends on HC feedback during planning

**Research date:** 2026-02-28
**Valid until:** 2026-03-30 (30 days; assessment infrastructure stable, but HC interview feedback may refine form design)
