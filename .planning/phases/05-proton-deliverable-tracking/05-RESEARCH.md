# Phase 5: Proton Deliverable Tracking - Research

**Researched:** 2026-02-17 (fresh pass — overwrites previous)
**Domain:** ASP.NET Core MVC — new master data tables, sequential deliverable workflow, first file upload feature, Proton track assignment
**Confidence:** HIGH (based entirely on direct codebase inspection)

---

## Summary

Phase 5 is the first phase that introduces brand-new master data tables, user-specific progress tracking, and file upload — all capabilities the codebase has not done before. The core challenge is designing a normalized deliverable hierarchy that enables sequential locking (PROTN-03) while keeping the read-only view (PROTN-02) fast and simple.

The existing `CpdpItem` table has a 2-level flat structure (`NamaKompetensi` + `Silabus` as sub-competency), with `TargetDeliverable` as a multi-line text blob per row. This is not granular enough to support per-deliverable sequential locking. Phase 5 must create a proper 3-table master hierarchy: `ProtonKompetensi` → `ProtonSubKompetensi` → `ProtonDeliverable`, then link it to a per-user progress table `ProtonDeliverableProgress` that tracks which deliverable a coachee is on.

The Proton track (Panelman vs Operator, Tahun 1/2/3) determines which set of deliverables a coachee must complete. Track assignment (PROTN-01) adds a `ProtonTrackAssignment` record linking a coachee to a track. The sequential lock (PROTN-03) is enforced by querying whether the previous deliverable in the ordered list has been approved. File upload (PROTN-04/PROTN-05) is the first IFormFile feature in the codebase and requires `IWebHostEnvironment` injection and `wwwroot/uploads/evidence/` as the storage path.

**Primary recommendation:** Five new DB tables in one migration: `ProtonKompetensi`, `ProtonSubKompetensi`, `ProtonDeliverable`, `ProtonTrackAssignment`, `ProtonDeliverableProgress`. Seed deliverable hierarchy from the existing CPDP data pattern. Store evidence files to `wwwroot/uploads/evidence/{deliverableProgressId}/`. No new NuGet packages needed.

---

## Standard Stack

### Core (already installed — no additions needed)
| Component | Version | Purpose | Notes |
|-----------|---------|---------|-------|
| ASP.NET Core MVC | .NET 8.0 | Controller/View framework | TargetFramework: net8.0 |
| EF Core SqlServer | 8.0.0 | ORM + migrations | Already in csproj |
| EF Core Tools | 8.0.0 | `dotnet ef migrations add` | Already in csproj |
| ASP.NET Core Identity | 8.0.0 | Auth + role checking | ApplicationUser already has RoleLevel |
| Bootstrap 5 | CDN via _Layout.cshtml | UI framework | All views use Bootstrap 5 |
| Bootstrap Icons | CDN | Icon library | `bi bi-*` pattern used throughout |
| IFormFile | ASP.NET Core built-in | File upload handling | First use in codebase — no extra package |
| IWebHostEnvironment | ASP.NET Core built-in | `WebRootPath` for file storage | Inject via constructor — same pattern as ApplicationDbContext |

### No New Packages Required
Phase 5 does not need any new NuGet packages. `IFormFile` is part of `Microsoft.AspNetCore.Http` which is already available.

---

## Architecture Patterns

### New Models Overview
```
Models/
├── ProtonModels.cs            # NEW — 5 master/tracking entities in one file
│   ├── ProtonKompetensi       # Master: Kompetensi (top level)
│   ├── ProtonSubKompetensi    # Master: Sub Kompetensi (FK to Kompetensi)
│   ├── ProtonDeliverable      # Master: per-deliverable item (FK to SubKompetensi)
│   ├── ProtonTrackAssignment  # Per-user: which track (Panelman/Operator Tahun 1/2/3)
│   └── ProtonDeliverableProgress  # Per-user per-deliverable: status, evidence path
└── ProtonViewModels.cs        # NEW — ViewModels for Proton pages
```

### New DB Tables (one migration)
```
ProtonKompetensi        — master: Id, NamaKompetensi, TrackType, TahunKe, Urutan
ProtonSubKompetensi     — master: Id, ProtonKompetensiId (FK), NamaSubKompetensi, Urutan
ProtonDeliverable       — master: Id, ProtonSubKompetensiId (FK), NamaDeliverable, Urutan
ProtonTrackAssignment   — per-user: Id, CoacheeId, AssignedById, TrackType, TahunKe, IsActive, AssignedAt
ProtonDeliverableProgress — per-user: Id, CoacheeId, ProtonDeliverableId (FK), Status, EvidencePath, EvidenceFileName, SubmittedAt, ApprovedAt, RejectedAt, CreatedAt
```

### Recommended Project Structure (Phase 5 additions)
```
Controllers/
└── CDPController.cs         # Add IWebHostEnvironment injection, ProtonMain(), AssignTrack(),
                             # update PlanIdp(), add Deliverable(), UploadEvidence()

Data/
└── ApplicationDbContext.cs  # Add 5 new DbSets + OnModelCreating config

Models/
├── ProtonModels.cs          # NEW — 5 entities
└── ProtonViewModels.cs      # NEW — ProtonMainViewModel, ProtonPlanViewModel, DeliverableViewModel

Migrations/
└── YYYYMMDDHHMMSS_AddProtonDeliverableTracking.cs  # NEW — one migration

Views/CDP/
├── PlanIdp.cshtml           # EXTEND — add @model object? directive + hybrid rendering block
│                            # (CURRENT: no @model directive, ViewBag-only, PDF download)
│                            # (NEW: @model object? + if/else for Coachee vs PDF path)
├── ProtonMain.cshtml        # NEW — Proton main page with track assignment modal
├── Deliverable.cshtml       # NEW — Deliverable detail page with evidence upload
└── Index.cshtml             # EXTEND — add Proton Main navigation card

Data/
└── SeedProtonData.cs        # NEW — seed ProtonKompetensi/SubKompetensi/Deliverable hierarchy
```

### Pattern 1: Three-Level Hierarchy Entities
**What:** Normalized master data for Kompetensi → SubKompetensi → Deliverable, each with a `TrackType` discriminator and `Urutan` (display order).
**When to use:** Reading the deliverable list for PROTN-02; also used to determine "previous deliverable" for PROTN-03 sequential lock.
```csharp
// Source: pattern from Models/KkjModels.cs hierarchy + Models/IdpItem.cs FK pattern
public class ProtonKompetensi
{
    public int Id { get; set; }
    public string NamaKompetensi { get; set; } = "";
    // "Panelman" or "Operator" — determines which track this belongs to
    public string TrackType { get; set; } = "";
    // "Tahun 1", "Tahun 2", "Tahun 3" — year of the track
    public string TahunKe { get; set; } = "";
    public int Urutan { get; set; }
    public ICollection<ProtonSubKompetensi> SubKompetensiList { get; set; } = new List<ProtonSubKompetensi>();
}

public class ProtonSubKompetensi
{
    public int Id { get; set; }
    public int ProtonKompetensiId { get; set; }
    public ProtonKompetensi? ProtonKompetensi { get; set; }
    public string NamaSubKompetensi { get; set; } = "";
    public int Urutan { get; set; }
    public ICollection<ProtonDeliverable> Deliverables { get; set; } = new List<ProtonDeliverable>();
}

public class ProtonDeliverable
{
    public int Id { get; set; }
    public int ProtonSubKompetensiId { get; set; }
    public ProtonSubKompetensi? ProtonSubKompetensi { get; set; }
    public string NamaDeliverable { get; set; } = "";
    public int Urutan { get; set; }
}
```

### Pattern 2: Track Assignment Entity
**What:** Links a Coachee to a Proton track (TrackType + TahunKe). One active assignment per coachee at a time.
**When to use:** PROTN-01 — Coach/SrSpv assigns coachee; PROTN-02/03 — used to look up which deliverables the coachee must complete.
```csharp
// Source: pattern from Models/CoachCoacheeMapping.cs
public class ProtonTrackAssignment
{
    public int Id { get; set; }
    // No FK constraint — consistent with CoachingLog/CoachCoacheeMapping pattern
    public string CoacheeId { get; set; } = "";
    public string AssignedById { get; set; } = "";  // Coach or SrSpv who assigned
    // "Panelman" or "Operator"
    public string TrackType { get; set; } = "";
    // "Tahun 1", "Tahun 2", "Tahun 3"
    public string TahunKe { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
```

### Pattern 3: Per-User Deliverable Progress Entity
**What:** Tracks the completion state of each deliverable for a specific coachee. Created eagerly when a track is assigned (bulk insert — first deliverable Active, rest Locked).
**When to use:** PROTN-03 (sequential lock), PROTN-04 (evidence upload), PROTN-05 (resubmit).
```csharp
// Source: pattern from Models/IdpItem.cs (Evidence path, approval status) + Models/CoachCoacheeMapping.cs
public class ProtonDeliverableProgress
{
    public int Id { get; set; }
    public string CoacheeId { get; set; } = "";     // No FK — consistent with existing pattern
    public int ProtonDeliverableId { get; set; }
    public ProtonDeliverable? ProtonDeliverable { get; set; }

    // Status: "Locked", "Active", "Submitted", "Approved", "Rejected"
    public string Status { get; set; } = "Locked";

    // Evidence file (PROTN-04/05)
    public string? EvidencePath { get; set; }  // Relative path: "/uploads/evidence/{id}/{filename}"
    public string? EvidenceFileName { get; set; }  // Original filename for display

    // Timestamps
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Pattern 4: Sequential Lock Logic
**What:** A deliverable is "Active" only if the previous deliverable in the ordered list (by Urutan) is "Approved". The first deliverable of a track assignment starts as "Active".
**When to use:** PROTN-03 enforcement — prevents access to next deliverable until current is approved.
**Critical:** Must be enforced server-side in the `Deliverable()` GET action, not just in the view.
```csharp
// Source: same sequential validation pattern used in IdpItem status progression
// Called when loading a Deliverable page
// Load ALL progress records in ONE query (avoids N+1):
var allProgress = await _context.ProtonDeliverableProgresses
    .Include(p => p.ProtonDeliverable)
        .ThenInclude(d => d.ProtonSubKompetensi)
        .ThenInclude(s => s.ProtonKompetensi)
    .Where(p => p.CoacheeId == progress.CoacheeId)
    .ToListAsync();

// Find ordered list of all deliverables in the track
var orderedDeliverables = allProgress
    .Select(p => p.ProtonDeliverable)
    .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
    .ThenBy(d => d.ProtonSubKompetensi.Urutan)
    .ThenBy(d => d.Urutan)
    .ToList();

var targetIndex = orderedDeliverables.FindIndex(d => d.Id == progress.ProtonDeliverableId);
bool isAccessible = targetIndex == 0 ||
    allProgress.First(p => p.ProtonDeliverableId == orderedDeliverables[targetIndex - 1].Id).Status == "Approved";
```

### Pattern 5: File Upload (First Use in Codebase)
**What:** ASP.NET Core `IFormFile` binding with `IWebHostEnvironment` to resolve `wwwroot` path. Files stored as `wwwroot/uploads/evidence/{progressId}/{timestamped-filename}`.
**When to use:** PROTN-04 (upload evidence), PROTN-05 (resubmit with new evidence).
```csharp
// Source: ASP.NET Core built-in IFormFile — new for this codebase
// IWebHostEnvironment injected in constructor alongside ApplicationDbContext

// In CDPController constructor (add alongside existing fields):
private readonly IWebHostEnvironment _env;
public CDPController(..., IWebHostEnvironment env)
{
    _env = env;
}

// In UploadEvidence POST action:
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UploadEvidence(int progressId, IFormFile evidenceFile)
{
    if (evidenceFile == null || evidenceFile.Length == 0)
    {
        TempData["Error"] = "File tidak boleh kosong.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Only allow PDF, JPG, PNG (evidence documents)
    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
    var ext = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
    if (!allowedExtensions.Contains(ext))
    {
        TempData["Error"] = "Hanya PDF, JPG, dan PNG yang diperbolehkan.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Max 10MB
    if (evidenceFile.Length > 10 * 1024 * 1024)
    {
        TempData["Error"] = "Ukuran file maksimal 10MB.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    var progress = await _context.ProtonDeliverableProgresses
        .FirstOrDefaultAsync(p => p.Id == progressId);
    if (progress == null) return NotFound();

    // Status check: only Active or Rejected can be uploaded
    if (progress.Status != "Active" && progress.Status != "Rejected")
    {
        TempData["Error"] = "Deliverable ini tidak dapat diupload.";
        return RedirectToAction("Deliverable", new { id = progressId });
    }

    // Build file path: wwwroot/uploads/evidence/{progressId}/
    var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "evidence", progressId.ToString());
    Directory.CreateDirectory(uploadDir);  // idempotent

    // Sanitize filename — Path.GetFileName strips directory traversal components
    var safeFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(evidenceFile.FileName)}";
    var filePath = Path.Combine(uploadDir, safeFileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await evidenceFile.CopyToAsync(stream);
    }

    // Store relative web path (for <a href="...">)
    progress.EvidencePath = $"/uploads/evidence/{progressId}/{safeFileName}";
    progress.EvidenceFileName = evidenceFile.FileName;
    progress.Status = "Submitted";
    progress.SubmittedAt = DateTime.UtcNow;
    if (progress.RejectedAt != null) progress.RejectedAt = null;  // Clear on resubmit

    await _context.SaveChangesAsync();

    TempData["Success"] = "Evidence berhasil diupload. Menunggu review approver.";
    return RedirectToAction("Deliverable", new { id = progressId });
}
```

### Pattern 6: Seed Data for ProtonDeliverable Hierarchy
**What:** A new `SeedProtonData.cs` class added to `Data/` and called from `Program.cs`, following the same pattern as `SeedMasterData`.
**When to use:** `Program.cs` startup seeding — called once, idempotent (skip if `ProtonKompetensiList` already has data).
```csharp
// Source: Data/SeedMasterData.cs pattern (SeedKkjMatrixAsync, SeedCpdpItemsAsync)
public static class SeedProtonData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.ProtonKompetensiList.AnyAsync())
        {
            Console.WriteLine("Proton deliverable hierarchy already seeded, skipping...");
            return;
        }

        // Seed Operator Tahun 1 — real domain data
        var k1 = new ProtonKompetensi
        {
            NamaKompetensi = "Safe Work Practice & Lifesaving Rules",
            TrackType = "Operator",
            TahunKe = "Tahun 1",
            Urutan = 1
        };
        context.ProtonKompetensiList.Add(k1);
        await context.SaveChangesAsync();

        var sk1 = new ProtonSubKompetensi
        {
            ProtonKompetensiId = k1.Id,
            NamaSubKompetensi = "Safe Work Practice",
            Urutan = 1
        };
        context.ProtonSubKompetensiList.Add(sk1);
        await context.SaveChangesAsync();

        context.ProtonDeliverableList.AddRange(new List<ProtonDeliverable>
        {
            new() { ProtonSubKompetensiId = sk1.Id, NamaDeliverable = "Mampu memahami 5 Tingkatan Budaya HSSE", Urutan = 1 },
            new() { ProtonSubKompetensiId = sk1.Id, NamaDeliverable = "Mampu memahami Pengertian Bahaya", Urutan = 2 },
            new() { ProtonSubKompetensiId = sk1.Id, NamaDeliverable = "Mampu memahami 9 Perilaku Wajib", Urutan = 3 }
        });
        await context.SaveChangesAsync();

        // ... additional Kompetensi seeded similarly
        // Placeholder for Panelman Tahun 1, Operator Tahun 2, Operator Tahun 3
        // TODO: Replace placeholder tracks with actual data when source material is available

        Console.WriteLine("Proton deliverable hierarchy seeded successfully.");
    }
}
```

### Pattern 7: PlanIdp View — Hybrid Rendering (Important: no @model currently)
**What:** The CURRENT `PlanIdp.cshtml` has NO `@model` directive at all — it uses only `ViewBag` (ViewBag.UserRole, ViewBag.PdfFileName, etc.). Phase 5 adds `@model object?` as the FIRST directive and a conditional block at the top.
**When to use:** PROTN-02 — Coachee views their deliverable list without status or links.
**Approach:**
1. Add `@model object?` directive at the top (first `@model` ever in this file)
2. At the start of the HTML section, add if/else blocks:
   - `@if (ViewBag.IsProtonView == true)` → render DB-driven deliverable table from `Model as ProtonPlanViewModel`
   - `@else if (ViewBag.NoAssignment == true)` → render "no track assigned" alert
   - `@else` → render EXISTING PDF content (completely unchanged)
3. The controller detects Coachee role, sets `ViewBag.IsProtonView = true`, passes a `ProtonPlanViewModel` as the model
4. For all other roles, the existing PDF path sets no IsProtonView flag and passes no model — the `@model object?` is backward-compatible

**PROTN-02 requirement for Coachee view:** Read-only table only. No status column. No navigation links on table rows. The ONLY navigation is an "active deliverable" button ABOVE the table (linking to `/CDP/Deliverable/{progressId}`). If `protonModel.ActiveProgress != null`, show the button; if null (all done or none active), show a completion message.

### Pattern 8: ProtonMain Page — Track Assignment (PROTN-01)
**What:** A new `ProtonMain.cshtml` page showing the list of coachees (from the coach's section) and their current track assignment. Coach/SrSpv can assign or change a coachee's track via a dropdown + POST modal.
**When to use:** PROTN-01 — the "Proton Main page" mentioned in the requirement.
**URL:** `/CDP/ProtonMain`
**Role check:** `user.RoleLevel > 5` returns Forbid() — consistent with existing `CreateSession` pattern in the same controller (line 308: `if (user.RoleLevel > 5) return Forbid()`).
**Coachee list source:** Section-based query `_context.Users.Where(u => u.Section == user.Section && u.RoleLevel == 6)` — same as the existing `Coaching()` action (lines 252-256).
**Navigation:** Each coachee row with an active progress record (Status == "Active") shows a "Lihat Deliverable" link to `/CDP/Deliverable/{progressId}`. This requires loading `ActiveProgresses` in the ProtonMain GET action.

### Pattern 9: AssignTrack — Eager Progress Creation
**What:** When a track is assigned, ProtonDeliverableProgress records are bulk-inserted for ALL deliverables in the track immediately. First = "Active", rest = "Locked". Previous progress for the coachee is deleted first.
**Why eager:** Avoids handling "progress doesn't exist yet" case in downstream code (sequential lock check, Deliverable GET). Simpler throughout.
**Implementation:** Query deliverables ordered by `(Kompetensi.Urutan, SubKompetensi.Urutan, Deliverable.Urutan)`, create progress list, `AddRange`, `SaveChanges`. Also delete previous ProtonDeliverableProgress records before inserting new ones (reassignment resets progress).

### Pattern 10: Role Constant Values (Important for String Comparison)
**What:** When checking roles by string value (not RoleLevel), use the constants from `UserRoles.cs` exactly.
**Why it matters:** `UserRoles.SrSupervisor = "Sr Supervisor"` (space, no period). Direct string comparison against "SrSupervisor" fails.
```csharp
// From Models/UserRoles.cs (confirmed by inspection):
UserRoles.Coach          = "Coach"
UserRoles.SrSupervisor   = "Sr Supervisor"   // NOTE: space between "Sr" and "Supervisor"
UserRoles.SectionHead    = "Section Head"    // NOTE: space
UserRoles.Coachee        = "Coachee"
UserRoles.HC             = "HC"
UserRoles.Admin          = "Admin"
```

### Anti-Patterns to Avoid
- **Storing evidence files in the database as binary (VARBINARY):** SQL Server stores them as large blobs; this makes backups huge, queries slow, and prevents direct browser download. Store file paths only, files to `wwwroot/uploads/`.
- **Using `CpdpItem` directly for the deliverable list:** `CpdpItem.TargetDeliverable` is a multi-line text blob, not per-deliverable rows. It cannot support per-row sequential locking. Create the new hierarchy tables.
- **Locking the entire track on first upload instead of per-deliverable:** The sequential lock allows viewing the full read-only list but only allows uploading/accessing the Deliverable detail for the current active one.
- **Querying ProtonDeliverableProgress in a loop (N+1):** Load ALL progress records for the coachee in ONE query, then match in-memory.
- **Sequential lock enforced only in the view (not server-side):** The `Deliverable()` GET action must check the sequential lock and return a locked view if not accessible. UI-only enforcement is bypassed by direct URL navigation.
- **Not sanitizing uploaded filenames:** An attacker could craft a filename with `../../../` path traversal. Always use `Path.GetFileName()` (not the full path from `IFormFile.FileName`) and prepend a timestamp.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Anti-forgery on file upload form | Custom CSRF | `[ValidateAntiForgeryToken]` + tag helper forms | Already used on all POST actions in this codebase |
| File extension validation | Regex on Content-Type | Check `Path.GetExtension()` against allowlist + check `Length > 0` | Content-Type can be spoofed; extension + server-side check is sufficient for internal portal |
| File size limit | Custom JS check | Check `evidenceFile.Length > 10 * 1024 * 1024` in controller | Controller check is simpler and matches codebase style |
| Sequential ordering | Custom sort logic | Order by composite key `(Kompetensi.Urutan, SubKompetensi.Urutan, Deliverable.Urutan)` via LINQ | Int columns guarantee stable ordering |
| Track assignment UI | Custom modal/JS | Bootstrap modal with a simple POST form (same pattern as CreateSession in Coaching.cshtml) | Matches existing coaching assignment modal pattern |
| Master data management UI | Admin CRUD screens | Seed data only — no admin UI for Phase 5 | Deliverable hierarchy is defined per domain, not user-managed; Phase 5 scope is tracking, not authoring |
| IWebHostEnvironment registration | `services.AddX()` in Program.cs | Nothing — DI resolves it automatically | IWebHostEnvironment is a built-in service already registered by the ASP.NET Core host |

**Key insight:** The IFormFile pattern is a built-in ASP.NET Core mechanism. No library is needed for basic file upload — just inject `IWebHostEnvironment` to get the `wwwroot` path.

---

## Common Pitfalls

### Pitfall 1: IWebHostEnvironment Not Injected in CDPController
**What goes wrong:** `_env.WebRootPath` throws a NullReferenceException because `IWebHostEnvironment` was not added to the CDPController constructor.
**Why it happens:** `IWebHostEnvironment` is currently NOT used anywhere in the codebase. No existing controller demonstrates the pattern. The existing CDPController constructor (lines 17-22) only takes `UserManager`, `SignInManager`, and `ApplicationDbContext`.
**How to avoid:** Add `private readonly IWebHostEnvironment _env;` to CDPController fields and inject `IWebHostEnvironment env` in the constructor. ASP.NET Core DI resolves it automatically — no registration needed in `Program.cs`.
**Warning signs:** `NullReferenceException` on `_env.WebRootPath` at runtime, or `CS0103` compile error referencing `_env`.

### Pitfall 2: File Upload Form Missing `enctype="multipart/form-data"`
**What goes wrong:** The form submits but `IFormFile` parameter in the action is always `null`, even with a file selected.
**Why it happens:** Without `enctype="multipart/form-data"`, the browser sends the form as `application/x-www-form-urlencoded`, which cannot transmit binary data. The ASP.NET Core model binder cannot bind `IFormFile` from URL-encoded forms.
**How to avoid:** Always add `enctype="multipart/form-data"` to any `<form>` with a file input: `<form method="post" asp-action="UploadEvidence" enctype="multipart/form-data">`.
**Warning signs:** `IFormFile` is null in the action despite file being selected in the browser.

### Pitfall 3: wwwroot/uploads/ Directory Does Not Exist at Startup
**What goes wrong:** `new FileStream(filePath, FileMode.Create)` throws `DirectoryNotFoundException` on the first upload.
**Why it happens:** `wwwroot/uploads/evidence/{progressId}/` does not exist until code creates it. The directory structure is not pre-created.
**How to avoid:** Always call `Directory.CreateDirectory(uploadDir)` before writing the file. This is idempotent (no error if directory already exists).
**Warning signs:** `DirectoryNotFoundException` on first file upload; works on subsequent uploads if directory was manually created.

### Pitfall 4: Sequential Lock Checked at View Layer Only
**What goes wrong:** A determined user bypasses the UI lock by directly navigating to `/CDP/Deliverable/{id}` for a deliverable they should not have access to.
**Why it happens:** If the sequential lock is only enforced in the view (e.g., not showing the link), the controller action does not check it.
**How to avoid:** The `Deliverable()` GET action must check whether the previous deliverable is Approved and return a "locked" view (not Forbid() — the user IS authorized, the deliverable is just locked) if not accessible. Never rely on UI-only enforcement for access control.
**Warning signs:** Coachee can access locked deliverables by guessing or copying URLs.

### Pitfall 5: ProtonTrackAssignment Not Deactivated on Reassignment
**What goes wrong:** A coachee accumulates multiple active assignments. `ProtonTrackAssignments.Where(a => a.CoacheeId == x && a.IsActive)` returns multiple rows, causing `.FirstOrDefault()` to silently pick an arbitrary one.
**Why it happens:** AssignTrack POST forgot to deactivate the previous active assignment before creating a new one.
**How to avoid:** In AssignTrack POST, before creating the new assignment: query all active assignments for the coachee, set `IsActive = false` for each, call SaveChanges. Also delete (or archive) all existing ProtonDeliverableProgress records for the coachee since the track is being reset.
**Warning signs:** PlanIdp page for a coachee shows a different track than what was assigned most recently.

### Pitfall 6: CpdpItem TrackType Mismatch
**What goes wrong:** Coachees assigned to the "Panelman" track see zero deliverables because no Panelman deliverables were seeded.
**Why it happens:** The existing `CpdpItem` seeded data only covers the Operator track (GAST RFCC NHT). No Panelman deliverable data is available.
**How to avoid:** Seed the Panelman track as placeholder data (1 Kompetensi, 1 SubKompetensi, 3 Deliverables) with `// TODO: Replace with actual Panelman data`. Also seed Tahun 2 and Tahun 3 as placeholders. This ensures the UI shows something for all valid track combinations and the TODO is visible in source code.
**Warning signs:** Coachee assigned Panelman track sees zero deliverables in their IDP Plan. `PlanIdp.cshtml` shows the "no assignment" fallback incorrectly if the hierarchy is empty.

### Pitfall 7: File Path Traversal if Using IFormFile.FileName Directly
**What goes wrong:** An attacker uploads a file named `../../appsettings.json` which overwrites the application config file.
**Why it happens:** `IFormFile.FileName` can contain path separators on some clients.
**How to avoid:** Always use `Path.GetFileName(evidenceFile.FileName)` which strips directory components and keeps only the base filename. Prepend a timestamp: `$"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(...)}"`.
**Warning signs:** Files not appearing in the expected directory; unexpected files appearing in parent directories.

### Pitfall 8: Multiple CASCADE Paths on ProtonDeliverableProgress
**What goes wrong:** EF Core migration generates but `dotnet ef database update` fails with `SqlException: Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths`.
**Why it happens:** `ProtonDeliverableProgress → ProtonDeliverable → ProtonSubKompetensi → ProtonKompetensi` — if any FK uses Cascade delete, deleting a `ProtonKompetensi` cascades through to `ProtonDeliverableProgress`. SQL Server restricts this.
**How to avoid:** Use `DeleteBehavior.Restrict` on ALL FK relationships in the Proton hierarchy (`ProtonSubKompetensi → ProtonKompetensi`, `ProtonDeliverable → ProtonSubKompetensi`, `ProtonDeliverableProgress → ProtonDeliverable`). Master data is never deleted operationally anyway.
**Warning signs:** Migration generates successfully but `dotnet ef database update` fails with FOREIGN KEY constraint error.

### Pitfall 9: PlanIdp @model Directive Breaks Existing PDF Logic
**What goes wrong:** After adding `@model ProtonPlanViewModel` to PlanIdp.cshtml, the existing PDF path throws a compile error because the view expects a ProtonPlanViewModel but the controller returns `View()` with no model for non-Coachee roles.
**Why it happens:** Changing the `@model` to a specific type means all paths through the controller must pass a compatible model.
**How to avoid:** Use `@model object?` (nullable object) so the Razor view can accept any model type (or null). Cast inside the conditional block: `@{ var protonModel = Model as HcPortal.Models.ProtonPlanViewModel; }`. The existing PDF path in the controller continues to call `return View()` with no model argument — which is `null` for `@model object?` — and the `@else` branch renders the PDF content using `ViewBag` only as before.
**Warning signs:** Build error "The model item passed into the ViewDataDictionary is of type..." or NullReferenceException in the PDF rendering section.

---

## Code Examples

Verified patterns from official sources and codebase inspection:

### CDPController — ProtonMain GET (PROTN-01 assignment page)
```csharp
// Source: CDPController.cs Coaching() GET pattern (lines 181-297) + section-based coachee query
public async Task<IActionResult> ProtonMain()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    // Access check: only coaching/supervisory roles (RoleLevel <= 5) — same as CreateSession
    if (user.RoleLevel > 5) return Forbid();

    // Coachees in same section — same pattern as Coaching() (lines 252-257)
    var coachees = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
        .OrderBy(u => u.FullName)
        .ToListAsync();

    var coacheeIds = coachees.Select(c => c.Id).ToList();

    var assignments = await _context.ProtonTrackAssignments
        .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
        .ToListAsync();

    // Load active progress records — needed for "Lihat Deliverable" links per coachee row
    var activeProgresses = await _context.ProtonDeliverableProgresses
        .Where(p => coacheeIds.Contains(p.CoacheeId) && p.Status == "Active")
        .ToListAsync();

    var viewModel = new ProtonMainViewModel
    {
        Coachees = coachees,
        Assignments = assignments,
        ActiveProgresses = activeProgresses
    };

    return View(viewModel);
}
```

### CDPController — AssignTrack POST (PROTN-01)
```csharp
// Source: CDPController.cs CreateSession POST pattern (lines 300-341)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AssignTrack(string coacheeId, string trackType, string tahunKe)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    if (user.RoleLevel > 5) return Forbid();

    if (string.IsNullOrEmpty(coacheeId) || string.IsNullOrEmpty(trackType) || string.IsNullOrEmpty(tahunKe))
    {
        TempData["Error"] = "Data tidak lengkap.";
        return RedirectToAction("ProtonMain");
    }

    // Deactivate existing active assignment for this coachee
    var existing = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == coacheeId && a.IsActive)
        .ToListAsync();
    foreach (var a in existing) a.IsActive = false;

    // Delete previous progress records (reassignment resets progress)
    var prevProgress = _context.ProtonDeliverableProgresses
        .Where(p => p.CoacheeId == coacheeId);
    _context.ProtonDeliverableProgresses.RemoveRange(prevProgress);

    // Create new assignment
    var assignment = new ProtonTrackAssignment
    {
        CoacheeId = coacheeId,
        AssignedById = user.Id,
        TrackType = trackType,
        TahunKe = tahunKe,
        IsActive = true,
        AssignedAt = DateTime.UtcNow
    };
    _context.ProtonTrackAssignments.Add(assignment);
    await _context.SaveChangesAsync();  // Save first to get assignment.Id

    // Bulk-create progress records — first = Active, rest = Locked
    var deliverables = await _context.ProtonDeliverableList
        .Include(d => d.ProtonSubKompetensi)
            .ThenInclude(s => s.ProtonKompetensi)
        .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.TrackType == trackType
                 && d.ProtonSubKompetensi.ProtonKompetensi.TahunKe == tahunKe)
        .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
        .ThenBy(d => d.ProtonSubKompetensi.Urutan)
        .ThenBy(d => d.Urutan)
        .ToListAsync();

    var progressList = deliverables.Select((d, i) => new ProtonDeliverableProgress
    {
        CoacheeId = coacheeId,
        ProtonDeliverableId = d.Id,
        Status = i == 0 ? "Active" : "Locked",
        CreatedAt = DateTime.UtcNow
    }).ToList();

    _context.ProtonDeliverableProgresses.AddRange(progressList);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Track Proton berhasil ditetapkan.";
    return RedirectToAction("ProtonMain");
}
```

### CDPController — PlanIdp Updated (PROTN-02 Coachee view)
```csharp
// Source: CDPController.cs PlanIdp() existing (lines 29-78) + new DB query path prepended
public async Task<IActionResult> PlanIdp(string? bagian = null, string? unit = null, string? level = null)
{
    var user = await _userManager.GetUserAsync(User);
    string userRole = "Operator";
    int userLevel = 6;

    if (user != null)
    {
        var roles = await _userManager.GetRolesAsync(user);
        userRole = roles.FirstOrDefault() ?? "Operator";
        userLevel = user.RoleLevel;
    }

    // PROTN-02: Coachee role gets DB-driven deliverable list BEFORE existing PDF logic
    if (user != null && (userRole == UserRoles.Coachee ||
        (userRole == UserRoles.Admin && user.SelectedView == "Coachee")))
    {
        var assignment = await _context.ProtonTrackAssignments
            .Where(a => a.CoacheeId == user.Id && a.IsActive)
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            ViewBag.UserRole = userRole;
            ViewBag.NoAssignment = true;
            return View();  // View will render "no assignment" alert in @else if block
        }

        var kompetensiList = await _context.ProtonKompetensiList
            .Include(k => k.SubKompetensiList)
                .ThenInclude(s => s.Deliverables)
            .Where(k => k.TrackType == assignment.TrackType && k.TahunKe == assignment.TahunKe)
            .OrderBy(k => k.Urutan)
            .ToListAsync();

        // Load the coachee's current active deliverable (for navigation button above table)
        var activeProgress = await _context.ProtonDeliverableProgresses
            .Where(p => p.CoacheeId == user.Id && p.Status == "Active")
            .FirstOrDefaultAsync();

        var planViewModel = new ProtonPlanViewModel
        {
            TrackType = assignment.TrackType,
            TahunKe = assignment.TahunKe,
            KompetensiList = kompetensiList,
            ActiveProgress = activeProgress
        };

        ViewBag.UserRole = userRole;
        ViewBag.IsProtonView = true;
        return View(planViewModel);
    }

    // EXISTING PDF LOGIC (unchanged) — continues below for all other roles
    // ... (rest of existing PlanIdp implementation unchanged)
}
```

### DbContext — 5 New DbSets and Config
```csharp
// Source: Data/ApplicationDbContext.cs existing pattern (lines 19-38 for DbSets, lines 40-216 for OnModelCreating)

// In DbSets section (after line 38, after CoachCoacheeMappings):
public DbSet<ProtonKompetensi> ProtonKompetensiList { get; set; }
public DbSet<ProtonSubKompetensi> ProtonSubKompetensiList { get; set; }
public DbSet<ProtonDeliverable> ProtonDeliverableList { get; set; }
public DbSet<ProtonTrackAssignment> ProtonTrackAssignments { get; set; }
public DbSet<ProtonDeliverableProgress> ProtonDeliverableProgresses { get; set; }

// In OnModelCreating (add at end of method, before closing brace):
builder.Entity<ProtonSubKompetensi>(entity =>
{
    entity.HasOne(s => s.ProtonKompetensi)
        .WithMany(k => k.SubKompetensiList)
        .HasForeignKey(s => s.ProtonKompetensiId)
        .OnDelete(DeleteBehavior.Restrict);  // Avoid cascade cycle
    entity.HasIndex(s => s.ProtonKompetensiId);
});

builder.Entity<ProtonDeliverable>(entity =>
{
    entity.HasOne(d => d.ProtonSubKompetensi)
        .WithMany(s => s.Deliverables)
        .HasForeignKey(d => d.ProtonSubKompetensiId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasIndex(d => d.ProtonSubKompetensiId);
});

builder.Entity<ProtonDeliverableProgress>(entity =>
{
    entity.HasOne(p => p.ProtonDeliverable)
        .WithMany()
        .HasForeignKey(p => p.ProtonDeliverableId)
        .OnDelete(DeleteBehavior.Restrict);
    entity.HasIndex(p => p.CoacheeId);
    entity.HasIndex(p => new { p.CoacheeId, p.ProtonDeliverableId }).IsUnique();
    entity.HasIndex(p => p.Status);
});

builder.Entity<ProtonTrackAssignment>(entity =>
{
    entity.HasIndex(a => a.CoacheeId);
    entity.HasIndex(a => new { a.CoacheeId, a.IsActive });
});
```

### Program.cs — Add SeedProtonData call
```csharp
// Source: Program.cs existing seeding pattern (lines 65-71)
// Add after existing SeedCompetencyMappings.SeedAsync call:
await SeedData.InitializeAsync(services);
await SeedMasterData.SeedKkjMatrixAsync(context);
await SeedMasterData.SeedCpdpItemsAsync(context);
await SeedMasterData.SeedSampleTrainingRecordsAsync(context);
await SeedCompetencyMappings.SeedAsync(context);
await SeedProtonData.SeedAsync(context);   // NEW — Phase 5
```

### Views/CDP/PlanIdp.cshtml — Model Directive Change
```razor
@* CURRENT (no @model directive — add this as first line): *@
@model object?
@{
    ViewData["Title"] = "IDP Proton - Individual Development Plan";
    var userRole = ViewBag.UserRole as string ?? "Operator";
    // ... existing ViewBag reads unchanged ...
}

@* Add BEFORE existing HTML, using conditional blocks: *@
@if (ViewBag.IsProtonView == true)
{
    @{ var protonModel = Model as HcPortal.Models.ProtonPlanViewModel; }
    @* Active deliverable navigation button above table *@
    @if (protonModel?.ActiveProgress != null)
    {
        <div class="alert alert-info">
            <i class="bi bi-arrow-right-circle me-2"></i>
            Anda memiliki deliverable aktif.
            <a href="/CDP/Deliverable/@protonModel.ActiveProgress.Id" class="btn btn-primary btn-sm ms-2">
                Lanjut ke Deliverable Aktif
            </a>
        </div>
    }
    else
    {
        <div class="alert alert-success">
            <i class="bi bi-check-circle me-2"></i>Semua deliverable telah selesai diproses.
        </div>
    }
    @* Read-only deliverable table: Kompetensi > SubKompetensi > Deliverable *@
    @* PROTN-02: NO status column, NO navigation links on rows *@
}
else if (ViewBag.NoAssignment == true)
{
    <div class="alert alert-info">
        <i class="bi bi-info-circle me-2"></i>
        Belum ada track Proton yang ditetapkan. Hubungi coach Anda untuk penetapan track.
    </div>
}
else
{
    @* EXISTING PDF content — completely unchanged *@
    @if (isHC && !hasBagianSelected)
    { ... }
    else
    { ... }
}
```

### Views/CDP/Deliverable.cshtml — Upload Form Critical Attribute
```razor
@* CRITICAL: enctype="multipart/form-data" is required for IFormFile to work *@
@* Without this, IFormFile parameter in UploadEvidence POST will always be null *@
@if (Model.CanUpload)
{
    <form method="post" asp-action="UploadEvidence" enctype="multipart/form-data">
        <input type="hidden" name="progressId" value="@Model.Progress.Id" />
        <div class="mb-3">
            <label class="form-label">Upload Evidence</label>
            <input type="file" name="evidenceFile" class="form-control" accept=".pdf,.jpg,.jpeg,.png" />
            <div class="form-text">Format: PDF, JPG, PNG. Maksimal 10MB.</div>
        </div>
        <button type="submit" class="btn btn-primary">
            @(Model.Progress.Status == "Rejected" ? "Upload Ulang Evidence" : "Upload Evidence")
        </button>
    </form>
}
```

---

## State of the Art

| Current State | Phase 5 Target | Notes |
|---------------|----------------|-------|
| `CpdpItem` has flat hierarchy with `NamaKompetensi` + `Silabus` + `TargetDeliverable` (text blob) | New `ProtonKompetensi` → `ProtonSubKompetensi` → `ProtonDeliverable` 3-table hierarchy | Flat CpdpItem is for CPDP gap analysis. New hierarchy is for Proton sequential tracking. |
| `PlanIdp.cshtml` has NO `@model` directive — uses ViewBag only, shows PDF download | `PlanIdp.cshtml` gains `@model object?` + if/else blocks — Coachee role gets DB table; all other roles unchanged | First `@model` directive in this file. `@model object?` preserves backward compat. |
| `CDPController` constructor: `(UserManager, SignInManager, ApplicationDbContext)` — 3 params | Add 4th param: `IWebHostEnvironment env` for `_env.WebRootPath` in UploadEvidence | ASP.NET Core DI resolves IWebHostEnvironment automatically — no Program.cs registration |
| No file upload anywhere in codebase | `CDPController.UploadEvidence` POST with `IFormFile` + `IWebHostEnvironment` | First IFormFile usage — establishes pattern for future upload features |
| `CoachCoacheeMapping` exists, registered in DbContext in Phase 4, but has no data | `ProtonTrackAssignment` is the assignment entity for Proton — separate concept | CoachCoacheeMapping is for general coaching mapping; ProtonTrackAssignment is Proton-specific |
| No `ProtonDeliverableProgress` records exist | Created eagerly in bulk when `AssignTrack` POST runs | First = "Active", rest = "Locked"; deleted and recreated on reassignment |
| `Program.cs` calls SeedCompetencyMappings.SeedAsync — last seed call | Add `SeedProtonData.SeedAsync(context)` after the existing last seed call | New SeedProtonData.cs file following exact SeedMasterData.cs pattern |
| `Views/CDP/Index.cshtml` has 4 navigation cards | Add 5th card: "Proton Main" linking to `/CDP/ProtonMain`, icon `bi bi-clipboard-check` | Matches existing 4-card grid layout and Bootstrap card style |

**Deprecated/unchanged:**
- `CpdpItem`: Remains as-is for the existing CPDP gap analysis. Not removed or altered in Phase 5.
- `IdpItem`: Remains as-is. Not the same as Proton deliverables.
- `CoachCoacheeMapping`: Remains as the general coach-coachee relationship table. Phase 5's `ProtonTrackAssignment` is a separate concept.
- `CDPController` existing actions (Index, PlanIdp PDF path, Dashboard, Coaching, Progress): All unchanged except PlanIdp which gains a Coachee-specific block at the top.

---

## Open Questions

1. **What are the actual deliverable contents for Panelman vs Operator tracks?**
   - What we know: The seeded `CpdpItem` data represents the Operator track (GAST RFCC NHT). The `GAST_RFCCNHT_Operator_Kompetensi_02022026.pdf` covers Operator track content.
   - What's unclear: The Panelman track deliverables are unknown and not in any seeded data or PDF. "Tahun 2" and "Tahun 3" content is also not seeded.
   - Recommendation: Seed Tahun 1 Operator track with real data from CpdpItem-inspired content. For Panelman and Tahun 2/3 tracks, seed placeholder data (1 Kompetensi, 1 SubKompetensi, 2-3 Deliverables) marked with `// TODO: Replace with actual data`. The planner should note that real data import is deferred pending source material.

2. **Should uploading evidence replace the previous file or add a new revision?**
   - What we know: PROTN-05 says "Coach can revise evidence and resubmit a rejected deliverable." Single active evidence file per deliverable.
   - Recommendation: For Phase 5, overwrite `EvidencePath` in `ProtonDeliverableProgress` with the new file path. Store new file in the same `uploads/evidence/{progressId}/` folder with a new timestamped name. Do NOT delete old files — let them accumulate (audit trail) and defer cleanup to a future phase.

3. **Is `CoachCoacheeMapping` needed for the ProtonMain coachee list, or is section-based query sufficient?**
   - What we know: `CoachCoacheeMapping` has no data. `CDPController.Coaching()` uses `_context.Users.Where(u => u.Section == user.Section && u.RoleLevel == 6)` for the coachee list successfully.
   - Recommendation: Use the section-based query (same as Coaching page pattern). `CoachCoacheeMapping` can be used in a future phase when coach-coachee relationships are explicitly managed.

4. **CanUpload logic for the Deliverable page — who can upload?**
   - What we know: PROTN-04 says "Coach can upload evidence files for a deliverable." PROTN-05 says "Coach can revise evidence and resubmit a rejected deliverable."
   - Recommendation: `CanUpload = (progress.Status == "Active" || progress.Status == "Rejected") AND current user RoleLevel <= 5` (coach/supervisor/HC role). The coachee cannot upload their own evidence — the coach uploads on their behalf.

---

## Sources

### Primary (HIGH confidence — direct codebase inspection)
- `Controllers/CDPController.cs` — confirmed constructor has no IWebHostEnvironment; confirmed PlanIdp returns View() with no model argument; confirmed Coaching() section-based coachee query; confirmed CreateSession uses RoleLevel > 5 check; confirmed no ProtonMain/AssignTrack/Deliverable actions
- `Views/CDP/PlanIdp.cshtml` — confirmed NO `@model` directive (all ViewBag); confirmed PDF-only rendering; confirmed role checks use ViewBag.UserRole; confirmed existing `isHC && !hasBagianSelected` branching structure
- `Views/CDP/Index.cshtml` — confirmed 4 navigation cards; no Proton Main card; confirmed Bootstrap card styling pattern with icon-box divs
- `Views/CDP/Coaching.cshtml` — confirmed Bootstrap modal pattern for form submission (data-bs-toggle/data-bs-target); confirmed TempData alert pattern (alert-success, alert-danger dismissible)
- `Data/ApplicationDbContext.cs` — confirmed no ProtonDeliverable tables; confirmed CoachCoacheeMapping registered; confirmed DeleteBehavior.Restrict used on UserCompetencyLevel; confirmed existing OnModelCreating structure
- `Models/ProtonModels.cs` — DOES NOT EXIST (confirmed: `ls Models/` shows no ProtonModels.cs or ProtonViewModels.cs)
- `Models/ApplicationUser.cs` — confirmed RoleLevel, Section, SelectedView fields
- `Models/UserRoles.cs` — confirmed exact string values: Coach="Coach", SrSupervisor="Sr Supervisor" (with space), SectionHead="Section Head" (with space), Coachee="Coachee"
- `Models/CoachingSession.cs` — confirmed string IDs for CoachId/CoacheeId with no FK, `Deliverable` field is a free-text string
- `Models/CoachCoacheeMapping.cs` — confirmed string CoachId/CoacheeId pattern (no FK)
- `Models/CoachingViewModels.cs` — confirmed ViewModels pattern in separate file
- `Program.cs` — confirmed `app.UseStaticFiles()` with custom options handles `wwwroot/` serving; confirmed seed call order; confirmed IWebHostEnvironment is NOT explicitly registered (built-in)
- `HcPortal.csproj` — confirmed .NET 8.0, EF Core 8.0.0; `IFormFile` is built-in, no extra package
- `Migrations/20260217053753_UpdateCoachingSessionFields.cs` — confirmed Phase 4 complete; current DB state: 5 Proton tables DO NOT EXIST
- `.planning/phases/04-foundation-coaching-sessions/04-VERIFICATION.md` — confirmed Phase 4 fully verified (4/4 truths); confirmed CoachCoacheeMapping registered in DB via AddCoachingFoundation migration

### Secondary (HIGH confidence — planning docs)
- `.planning/REQUIREMENTS.md` — PROTN-01 through PROTN-05 definitions
- `.planning/ROADMAP.md` — Phase 5 success criteria, dependency on Phase 4
- `.planning/STATE.md` — confirmed Phase 5 "Ready to plan"; confirmed decisions from v1.1 roadmap
- `05-01-PLAN.md` — reviewed for detail alignment with research
- `05-02-PLAN.md` — reviewed; confirmed PlanIdp hybrid approach, ActiveProgresses loading pattern
- `05-03-PLAN.md` — reviewed; confirmed sequential lock implementation, CanUpload=Active||Rejected logic, enctype requirement

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages already in csproj; IFormFile is built-in; confirmed by csproj inspection
- New entity design (5 tables): HIGH — pattern follows existing CoachCoacheeMapping/IdpItem models; no new design patterns
- Sequential lock logic: HIGH — derived directly from the stated requirements and existing status-field patterns in IdpItem/CoachingSession
- File upload pattern: HIGH — IFormFile + IWebHostEnvironment is standard ASP.NET Core 8.0; confirmed no existing usage to conflict with
- PlanIdp hybrid rendering: HIGH — confirmed current view has no @model directive; @model object? approach is backward-compatible
- Role check pattern: HIGH — confirmed `user.RoleLevel > 5` is the standard in this codebase (CreateSession at line 308)
- Seed data content: MEDIUM — Operator Tahun 1 data derivable from CpdpItem seed; Panelman/Tahun 2/3 unknown (placeholder recommended)
- Common pitfalls: HIGH — confirmed by reading actual controller/view code and absence of IFormFile patterns

**Research date:** 2026-02-17
**Valid until:** 2026-03-17 (stable ASP.NET Core 8.0 stack; findings are codebase-specific)
