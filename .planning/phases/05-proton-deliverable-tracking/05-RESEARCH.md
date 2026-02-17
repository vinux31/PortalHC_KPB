# Phase 5: Proton Deliverable Tracking - Research

**Researched:** 2026-02-17
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
├── ProtonModels.cs            # NEW — 4 master/tracking models in one file
│   ├── ProtonKompetensi       # Master: Kompetensi (top level)
│   ├── ProtonSubKompetensi    # Master: Sub Kompetensi (FK to Kompetensi)
│   ├── ProtonDeliverable      # Master: per-deliverable item (FK to SubKompetensi)
│   ├── ProtonTrackAssignment  # Per-user: which track (Panelman/Operator Tahun 1/2/3)
│   └── ProtonDeliverableProgress  # Per-user per-deliverable: status, evidence path
└── ProtonViewModels.cs        # NEW — ViewModels for Proton pages
```

### New DB Tables (one migration)
```
ProtonKompetensi        — master: Id, NamaKompetensi, TrackType, Urutan
ProtonSubKompetensi     — master: Id, ProtonKompetensiId (FK), NamaSubKompetensi, Urutan
ProtonDeliverable       — master: Id, ProtonSubKompetensiId (FK), NamaDeliverable, Urutan
ProtonTrackAssignment   — per-user: Id, CoacheeId, AssignedById, TrackType, TahunKe, AssignedAt
ProtonDeliverableProgress — per-user: Id, CoacheeId, ProtonDeliverableId (FK), Status, EvidencePath, SubmittedAt, ...
```

### Recommended Project Structure (Phase 5 additions)
```
Controllers/
└── CDPController.cs         # Add ProtonMain(), AssignTrack(), PlanIdpProton(), Deliverable(), UploadEvidence()

Data/
└── ApplicationDbContext.cs  # Add 5 new DbSets + OnModelCreating config

Models/
├── ProtonModels.cs          # NEW — 5 entities
└── ProtonViewModels.cs      # NEW — ProtonMainViewModel, ProtonPlanViewModel, DeliverableViewModel

Migrations/
└── YYYYMMDDHHMMSS_AddProtonDeliverableTracking.cs  # NEW — one migration

Views/CDP/
├── PlanIdp.cshtml           # EXTEND — add DB-driven view for Coachee role (hybrid: PDF for HC/SH, table for Coachee)
├── ProtonMain.cshtml        # NEW — Proton main page with track assignment
└── Deliverable.cshtml       # NEW — Deliverable detail page with evidence upload

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
**What:** Tracks the completion state of each deliverable for a specific coachee. Created on first access (lazy) or when track is assigned.
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
```csharp
// Source: same sequential validation pattern used in IdpItem status progression
// Called when loading a Deliverable page or the PlanIdp list
public static bool IsDeliverableAccessible(
    IEnumerable<ProtonDeliverableProgress> allProgress,
    ProtonDeliverableProgress targetProgress,
    IEnumerable<ProtonDeliverable> orderedDeliverables)
{
    // Find the index of the target deliverable in the ordered list
    var orderedList = orderedDeliverables.OrderBy(d => d.ProtonSubKompetensi?.ProtonKompetensi?.Urutan)
                                          .ThenBy(d => d.ProtonSubKompetensi?.Urutan)
                                          .ThenBy(d => d.Urutan).ToList();
    var targetIndex = orderedList.FindIndex(d => d.Id == targetProgress.ProtonDeliverableId);
    if (targetIndex == 0) return true;  // First deliverable is always accessible

    // Check if previous deliverable is Approved
    var previousDeliverable = orderedList[targetIndex - 1];
    var previousProgress = allProgress.FirstOrDefault(p => p.ProtonDeliverableId == previousDeliverable.Id);
    return previousProgress?.Status == "Approved";
}
```

### Pattern 5: File Upload (First Use in Codebase)
**What:** ASP.NET Core `IFormFile` binding with `IWebHostEnvironment` to resolve `wwwroot` path. Files stored as `wwwroot/uploads/evidence/{progressId}/{originalName}`.
**When to use:** PROTN-04 (upload evidence), PROTN-05 (resubmit with new evidence).
```csharp
// Source: ASP.NET Core built-in IFormFile — new for this codebase
// IWebHostEnvironment injected in constructor alongside ApplicationDbContext

// In CDPController constructor:
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

    // Build file path: wwwroot/uploads/evidence/{progressId}/
    var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "evidence", progressId.ToString());
    Directory.CreateDirectory(uploadDir);

    // Sanitize filename — keep original name but prepend timestamp to avoid collisions
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
            Console.WriteLine("ℹ️ Proton deliverable hierarchy already seeded, skipping...");
            return;
        }

        // Seed Panelman Tahun 1 sample hierarchy
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

        var deliverables = new List<ProtonDeliverable>
        {
            new() { ProtonSubKompetensiId = sk1.Id, NamaDeliverable = "Mampu memahami 5 Tingkatan Budaya HSSE", Urutan = 1 },
            new() { ProtonSubKompetensiId = sk1.Id, NamaDeliverable = "Mampu memahami Pengertian Bahaya", Urutan = 2 },
            new() { ProtonSubKompetensiId = sk1.Id, NamaDeliverable = "Mampu memahami 9 Perilaku Wajib", Urutan = 3 }
        };
        context.ProtonDeliverableList.AddRange(deliverables);
        await context.SaveChangesAsync();
    }
}
```

### Pattern 7: PlanIdp View — Hybrid (PDF for HC, DB-table for Coachee)
**What:** The existing `PlanIdp.cshtml` shows a static PDF. Phase 5 adds a DB-driven read-only table for Coachee role users that shows their full deliverable list grouped by Kompetensi > SubKompetensi > Deliverable.
**When to use:** PROTN-02 — Coachee views their deliverable list without status or links.
**Approach:** The controller detects role. For Coachee with an active `ProtonTrackAssignment`, it queries `ProtonKompetensi.Include(...SubKompetensiList.Include(...Deliverables))` filtered by the assigned `TrackType` + `TahunKe`. The view gets a `ProtonPlanViewModel`. For all other roles, the existing PDF logic remains untouched.

### Pattern 8: ProtonMain Page — Track Assignment (PROTN-01)
**What:** A new `ProtonMain.cshtml` page showing the list of coachees (from the coach's section) and their current track assignment. Coach/SrSpv can assign or change a coachee's track via a dropdown + POST.
**When to use:** PROTN-01 — the "Proton Main page" mentioned in the requirement.
**URL:** `/CDP/ProtonMain`

### Anti-Patterns to Avoid
- **Storing evidence files in the database as binary (VARBINARY):** SQL Server stores them as large blobs; this makes backups huge, queries slow, and prevents direct browser download. Store file paths only, files to `wwwroot/uploads/`.
- **Using `CpdpItem` directly for the deliverable list:** `CpdpItem.TargetDeliverable` is a multi-line text blob, not per-deliverable rows. It cannot support per-row sequential locking. Create the new hierarchy tables.
- **Locking the entire track on first upload instead of per-deliverable:** The sequential lock should allow viewing all deliverables in the list but only accessing the Deliverable detail page for the current active one.
- **Confusing `IdpItem` with `ProtonDeliverableProgress`:** `IdpItem` is the old flat IDP planning item, not the Proton deliverable. They should remain separate.
- **Querying ProtonDeliverableProgress in a loop (N+1):** Load all progress records for the coachee in one query, then match in-memory.
- **Not sanitizing uploaded filenames:** An attacker could craft a filename with `../../../` path traversal. Always use `Path.GetFileName()` (not the full path from `IFormFile.FileName`) and prepend a timestamp.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Anti-forgery on file upload form | Custom CSRF | `[ValidateAntiForgeryToken]` + tag helper forms | Already used on all POST actions in this codebase |
| File extension validation | Regex on Content-Type | Check `Path.GetExtension()` against allowlist + check `Length > 0` | Content-Type can be spoofed; extension + server-side check is sufficient for internal portal |
| File size limit | Custom JS check | ASP.NET Core `MultipartBodyLengthLimit` attribute OR check `Length > limit` in controller | Both work; controller check is simpler and matches codebase style |
| Sequential ordering | Custom sort logic | Order by composite key `(Kompetensi.Urutan, SubKompetensi.Urutan, Deliverable.Urutan)` via LINQ | Int columns guarantee stable ordering |
| Track assignment UI | Custom modal/JS | Bootstrap modal with a simple POST form (same pattern as CreateSession in Coaching.cshtml) | Matches existing coaching assignment modal pattern |
| Master data management UI | Admin CRUD screens | Seed data only — no admin UI for Phase 5 | Deliverable hierarchy is defined per domain, not user-managed; Phase 5 scope is tracking, not authoring |

**Key insight:** The IFormFile pattern is a built-in ASP.NET Core mechanism. No library is needed for basic file upload — just inject `IWebHostEnvironment` to get the `wwwroot` path.

---

## Common Pitfalls

### Pitfall 1: IWebHostEnvironment Not Injected in CDPController
**What goes wrong:** `_env.WebRootPath` throws a NullReferenceException because `IWebHostEnvironment` was not added to the CDPController constructor.
**Why it happens:** `IWebHostEnvironment` is not currently used anywhere in the codebase. No existing controller demonstrates the pattern.
**How to avoid:** Add `private readonly IWebHostEnvironment _env;` to CDPController fields and inject in the constructor: `public CDPController(..., IWebHostEnvironment env)`. ASP.NET Core DI resolves it automatically — no registration needed in `Program.cs`.
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
**Why it happens:** If the sequential lock is only enforced in the view (e.g., hiding the link), the controller action does not check it.
**How to avoid:** The `Deliverable()` GET action must check `IsDeliverableAccessible()` and return `Forbid()` or a locked page if the previous deliverable is not Approved. Never rely on UI-only enforcement for access control.
**Warning signs:** Coachee can access locked deliverables by guessing or copying URLs.

### Pitfall 5: ProtonTrackAssignment Not Validated for Active Assignment
**What goes wrong:** A coachee has two active assignments (e.g., Operator Tahun 1 and Panelman Tahun 2). The PlanIdp view queries by CoacheeId and gets both tracks, causing duplicate or incorrect deliverable lists.
**Why it happens:** No unique constraint prevents two active assignments per coachee.
**How to avoid:** Enforce at the DB level with a filtered unique index: unique on `(CoacheeId, IsActive = true)`. In the controller, when assigning, deactivate any existing active assignment first. Use `.Where(a => a.CoacheeId == coacheeId && a.IsActive).FirstOrDefault()` for lookups.
**Warning signs:** `ProtonPlanViewModel` shows deliverables from two tracks mixed together; `PlanIdp` page appears empty (zero-join if wrong track queried).

### Pitfall 6: CpdpItem TrackType Mismatch
**What goes wrong:** The `CpdpItem` seeded data does not distinguish Panelman vs Operator tracks. The new `ProtonDeliverable` hierarchy must tag each Kompetensi with `TrackType`. If seed data has mismatched track types, coachees assigned to "Panelman" track see zero deliverables.
**Why it happens:** `CpdpItem` has no `TrackType` column — it represents a single unit's CPDP data (GAST RFCC NHT, Operator level per the seeded PDF context). The Panelman track deliverable data may differ entirely from the Operator data.
**How to avoid:** Seed the `ProtonDeliverable` hierarchy with explicit `TrackType = "Operator"` (from the CPDP seed data) and create a separate seed pass for `TrackType = "Panelman"` with different deliverables. If actual Panelman deliverable data is not available, seed Panelman as an empty track with a placeholder and note it as an open item.
**Warning signs:** Coachee assigned Panelman track sees zero deliverables in their IDP Plan.

### Pitfall 7: File Path Traversal if Using IFormFile.FileName Directly
**What goes wrong:** An attacker uploads a file named `../../appsettings.json` which overwrites the application config file.
**Why it happens:** `IFormFile.FileName` can contain path separators on some clients.
**How to avoid:** Always use `Path.GetFileName(evidenceFile.FileName)` which strips directory components and keeps only the base filename. Prepend a timestamp: `$"{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(...)}`.
**Warning signs:** Files not appearing in the expected directory; unexpected files appearing in parent directories.

### Pitfall 8: Multiple CASCADE Paths on ProtonDeliverableProgress
**What goes wrong:** EF Core migration fails with `SqlException: Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths`.
**Why it happens:** `ProtonDeliverableProgress` has no FK to `Users` (string ID, same as CoachCoacheeMapping), so no cascade cycle from User side. However, `ProtonDeliverable → ProtonSubKompetensi → ProtonKompetensi` — if any of these use Cascade delete, deleting a `ProtonKompetensi` cascades through to `ProtonDeliverableProgress` via the FK chain. SQL Server restricts this.
**How to avoid:** Use `DeleteBehavior.Restrict` on the master hierarchy FK relationships (`ProtonDeliverableProgress → ProtonDeliverable`, `ProtonDeliverable → ProtonSubKompetensi`, `ProtonSubKompetensi → ProtonKompetensi`). Master data is never deleted operationally anyway.
**Warning signs:** Migration generates but `dotnet ef database update` fails with FOREIGN KEY constraint error.

---

## Code Examples

### CDPController — ProtonMain GET (PROTN-01 assignment page)
```csharp
// Source: CDPController.cs Coaching() GET pattern + existing coachee list query
public async Task<IActionResult> ProtonMain()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault();

    // Only Coach, SrSpv, Admin can access this page
    if (user.RoleLevel > 5 && userRole != UserRoles.SrSupervisor)
        return Forbid();

    // Get coachees in the same section
    var coachees = await _context.Users
        .Where(u => u.Section == user.Section && u.RoleLevel == 6)
        .OrderBy(u => u.FullName)
        .ToListAsync();

    // Get existing track assignments for those coachees
    var coacheeIds = coachees.Select(c => c.Id).ToList();
    var assignments = await _context.ProtonTrackAssignments
        .Where(a => coacheeIds.Contains(a.CoacheeId) && a.IsActive)
        .ToListAsync();

    var viewModel = new ProtonMainViewModel
    {
        Coachees = coachees,
        Assignments = assignments
    };

    return View(viewModel);
}
```

### CDPController — AssignTrack POST (PROTN-01)
```csharp
// Source: CDPController.cs CreateSession POST pattern
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AssignTrack(string coacheeId, string trackType, string tahunKe)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    if (user.RoleLevel > 5 && (await _userManager.GetRolesAsync(user)).FirstOrDefault() != UserRoles.SrSupervisor)
        return Forbid();

    // Deactivate existing active assignment for this coachee
    var existing = await _context.ProtonTrackAssignments
        .Where(a => a.CoacheeId == coacheeId && a.IsActive)
        .ToListAsync();
    foreach (var a in existing) a.IsActive = false;

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
    await _context.SaveChangesAsync();

    // Initialize progress records for all deliverables in the track
    // First deliverable = Active, rest = Locked
    var deliverables = await _context.ProtonDeliverableList
        .Include(d => d.ProtonSubKompetensi)
            .ThenInclude(s => s.ProtonKompetensi)
        .Where(d => d.ProtonSubKompetensi.ProtonKompetensi.TrackType == trackType
                 && d.ProtonSubKompetensi.ProtonKompetensi.TahunKe == tahunKe)
        .OrderBy(d => d.ProtonSubKompetensi.ProtonKompetensi.Urutan)
        .ThenBy(d => d.ProtonSubKompetensi.Urutan)
        .ThenBy(d => d.Urutan)
        .ToListAsync();

    for (int i = 0; i < deliverables.Count; i++)
    {
        _context.ProtonDeliverableProgresses.Add(new ProtonDeliverableProgress
        {
            CoacheeId = coacheeId,
            ProtonDeliverableId = deliverables[i].Id,
            Status = i == 0 ? "Active" : "Locked",
            CreatedAt = DateTime.UtcNow
        });
    }
    await _context.SaveChangesAsync();

    TempData["Success"] = "Track Proton berhasil ditetapkan.";
    return RedirectToAction("ProtonMain");
}
```

### CDPController — PlanIdp Updated (PROTN-02 Coachee view)
```csharp
// Source: CDPController.cs PlanIdp() existing + new DB query path
public async Task<IActionResult> PlanIdp(string? bagian = null, string? unit = null, string? level = null)
{
    var user = await _userManager.GetUserAsync(User);
    var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
    var userRole = roles.FirstOrDefault() ?? "Coachee";

    // For Coachee role: show DB-driven deliverable list
    if (user != null && (userRole == UserRoles.Coachee ||
        (userRole == UserRoles.Admin && user.SelectedView == "Coachee")))
    {
        var targetUserId = user.Id;

        var assignment = await _context.ProtonTrackAssignments
            .Where(a => a.CoacheeId == targetUserId && a.IsActive)
            .FirstOrDefaultAsync();

        if (assignment == null)
        {
            ViewBag.UserRole = userRole;
            ViewBag.NoAssignment = true;
            return View(new ProtonPlanViewModel());
        }

        var kompetensiList = await _context.ProtonKompetensiList
            .Include(k => k.SubKompetensiList)
                .ThenInclude(s => s.Deliverables)
            .Where(k => k.TrackType == assignment.TrackType && k.TahunKe == assignment.TahunKe)
            .OrderBy(k => k.Urutan)
            .ToListAsync();

        var planViewModel = new ProtonPlanViewModel
        {
            TrackType = assignment.TrackType,
            TahunKe = assignment.TahunKe,
            KompetensiList = kompetensiList
        };

        ViewBag.UserRole = userRole;
        return View(planViewModel);
    }

    // For all other roles: existing PDF logic (unchanged)
    // ... existing PlanIdp logic ...
    return View();  // existing path unchanged
}
```

### DbContext — 5 New DbSets and Config
```csharp
// Source: Data/ApplicationDbContext.cs existing pattern

// In DbSets section:
public DbSet<ProtonKompetensi> ProtonKompetensiList { get; set; }
public DbSet<ProtonSubKompetensi> ProtonSubKompetensiList { get; set; }
public DbSet<ProtonDeliverable> ProtonDeliverableList { get; set; }
public DbSet<ProtonTrackAssignment> ProtonTrackAssignments { get; set; }
public DbSet<ProtonDeliverableProgress> ProtonDeliverableProgresses { get; set; }

// In OnModelCreating:
builder.Entity<ProtonSubKompetensi>(entity =>
{
    entity.HasOne(s => s.ProtonKompetensi)
        .WithMany(k => k.SubKompetensiList)
        .HasForeignKey(s => s.ProtonKompetensiId)
        .OnDelete(DeleteBehavior.Restrict);
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
// Source: Program.cs existing seeding pattern
await SeedData.InitializeAsync(services);
await SeedMasterData.SeedKkjMatrixAsync(context);
await SeedMasterData.SeedCpdpItemsAsync(context);
await SeedMasterData.SeedSampleTrainingRecordsAsync(context);
await SeedCompetencyMappings.SeedAsync(context);
await SeedProtonData.SeedAsync(context);   // NEW — Phase 5
```

---

## State of the Art

| Current State | Phase 5 Target | Notes |
|---------------|----------------|-------|
| `CpdpItem` has flat hierarchy with `NamaKompetensi` + `Silabus` + `TargetDeliverable` (text blob) | New `ProtonKompetensi` → `ProtonSubKompetensi` → `ProtonDeliverable` 3-table hierarchy | Flat CpdpItem is for CPDP gap analysis. New hierarchy is for Proton sequential tracking. |
| `PlanIdp.cshtml` shows static PDF by filename | `PlanIdp.cshtml` shows DB-driven table for Coachee role; PDF path retained for HC/SrSpv | Hybrid view — Coachee role gets new path; all other roles use existing PDF path |
| No file upload anywhere in codebase | `CDPController.UploadEvidence` POST with `IFormFile` + `IWebHostEnvironment` | First IFormFile usage — `IWebHostEnvironment` injection is the new pattern to introduce |
| `CoachCoacheeMapping` exists but has no data (assignment UI not built) | `ProtonTrackAssignment` is the assignment entity for Proton; `CoachCoacheeMapping` is for general coaching mapping (Phase 4) | These are separate entities serving different purposes |
| No `ProtonDeliverableProgress` records exist | Created in bulk when `AssignTrack` POST runs (one record per deliverable in the track) | First record starts as "Active", rest "Locked" |
| `CDPController` has no `IWebHostEnvironment` constructor parameter | Add `IWebHostEnvironment env` to constructor | ASP.NET Core DI resolves this automatically; no `Program.cs` registration needed |

**Deprecated/unchanged:**
- `CpdpItem`: Remains as-is for the existing CPDP gap analysis. Not removed or altered in Phase 5.
- `IdpItem`: Remains as-is. Not the same as Proton deliverables.
- `CoachCoacheeMapping`: Remains as the general coach-coachee relationship table. Phase 5's `ProtonTrackAssignment` is a separate concept.

---

## Open Questions

1. **What are the actual deliverable contents for Panelman vs Operator tracks?**
   - What we know: The seeded `CpdpItem` data represents a single unit (GAST RFCC NHT, Operator level) per the PDF filename `GAST_RFCCNHT_Operator_Kompetensi_02022026.pdf`. The existing PDF covers Operator track content.
   - What's unclear: The Panelman track deliverables are unknown and not in any seeded data or PDF. "Tahun 2" and "Tahun 3" content is also not seeded.
   - Recommendation: Seed Tahun 1 Operator track from the existing `CpdpItem`-inspired data. For Panelman and Tahun 2/3 tracks, seed placeholder data (1 Kompetensi, 1 SubKompetensi, 3 Deliverables) marked with `// TODO: Replace with actual Panelman/Tahun2/3 data`. The planner should include a note that real data import is deferred pending source material.

2. **Should ProtonDeliverableProgress be created eagerly (on track assignment) or lazily (on first page access)?**
   - What we know: Eager creation (bulk insert on AssignTrack) simplifies the Deliverable access check — no need to handle "progress doesn't exist yet" case.
   - What's unclear: If a track has 200+ deliverables, eager creation inserts 200+ rows at once. This is fine for SQL Server but needs a short transaction.
   - Recommendation: Eager creation on `AssignTrack` POST. Simpler logic throughout. SQL Server handles batch inserts well.

3. **Should uploading evidence replace the previous file or add a new revision?**
   - What we know: PROTN-05 says "Coach can revise evidence and resubmit a rejected deliverable." The requirement implies only one active evidence file per deliverable. PROTN-04 is the initial upload.
   - What's unclear: Whether old evidence files should be deleted (disk cleanup) or retained (audit trail).
   - Recommendation: For Phase 5, overwrite `EvidencePath` in `ProtonDeliverableProgress` with the new file path. Store new file in the same `uploads/evidence/{progressId}/` folder. Do NOT delete old files — let them accumulate (audit trail) and defer cleanup to Phase 6 or Phase 7.

4. **What URL structure for the Deliverable detail page?**
   - What we know: The requirement says "Deliverable page" (PROTN-04). The `ProtonDeliverableProgress.Id` is the most natural route key since it ties together the coachee + deliverable uniquely.
   - What's unclear: Whether the URL should be `/CDP/Deliverable/{progressId}` or `/CDP/Deliverable/{deliverableId}` with coacheeId from current user.
   - Recommendation: `/CDP/Deliverable/{progressId}` — `progressId` uniquely identifies the coachee+deliverable pair, simplifies auth check (just verify `progress.CoacheeId == currentUser.Id` or `currentUser.RoleLevel <= 5`).

5. **Is `CoachCoacheeMapping` needed for the ProtonMain page coachee list, or can the section-based query be used?**
   - What we know: `CDPController.Coaching()` uses `_context.Users.Where(u => u.Section == user.Section && u.RoleLevel == 6)` for the coachee list. `CoachCoacheeMapping` exists but has no data.
   - What's unclear: Whether PROTN-01 should be constrained to coachees officially mapped to the coach (via `CoachCoacheeMapping`) or any coachee in the section.
   - Recommendation: Use the section-based query (same as Coaching page pattern). `CoachCoacheeMapping` can be used in a future phase when coach-coachee relationships are explicitly managed.

---

## Sources

### Primary (HIGH confidence — direct codebase inspection)
- `Models/CoachingLog.cs` — confirmed Proton form structure (SubKompetensi, Deliverables, Kesimpulan, Result)
- `Models/CoachingSession.cs` — confirmed Phase 4 final schema (7 domain fields, no Topic/Notes)
- `Models/CoachCoacheeMapping.cs` — confirmed structure, confirmed in DbContext but no data
- `Models/ApplicationUser.cs` — confirmed RoleLevel, Section fields used for access control
- `Models/UserRoles.cs` — confirmed role constants: Coach (5), SrSupervisor (4), SectionHead (4), Coachee (6)
- `Models/IdpItem.cs` — confirmed Evidence is a string path, approval fields are strings
- `Models/TrackingModels.cs` — confirmed TrackingItem is display-only DTO, not a DB entity
- `Data/ApplicationDbContext.cs` — confirmed Phase 4 entities registered; no ProtonDeliverable tables exist
- `Data/SeedMasterData.cs` — confirmed CpdpItem seeded data structure (NamaKompetensi + Silabus + TargetDeliverable as text blob); confirmed only Operator GAST RFCC NHT track data present
- `Controllers/CDPController.cs` — confirmed no `IFormFile`/`IWebHostEnvironment` usage; confirmed PlanIdp returns static PDF; confirmed Progress() queries IdpItems mapped to TrackingItem DTO
- `Views/CDP/PlanIdp.cshtml` — confirmed PDF-only implementation, role-based filter exists for HC/SH/SrSpv
- `Views/CDP/Progress.cshtml` — confirmed table structure with approval columns; modal is non-functional stub (console.log only)
- `Views/CDP/Index.cshtml` — confirmed 4 navigation cards; Proton Main page not yet linked
- `HcPortal.csproj` — confirmed .NET 8.0, EF Core 8.0.0; `IFormFile` is built-in, no extra package
- `Program.cs` — confirmed `app.UseStaticFiles()` handles `wwwroot/` serving; seed pattern confirmed
- `Migrations/20260217053753_UpdateCoachingSessionFields.cs` — confirmed Phase 4 complete; current DB state known
- `.planning/phases/04-foundation-coaching-sessions/04-VERIFICATION.md` — confirmed Phase 4 fully verified (4/4 truths)

### Secondary (HIGH confidence — planning docs)
- `.planning/REQUIREMENTS.md` — PROTN-01 through PROTN-05 definitions
- `.planning/ROADMAP.md` — Phase 5 success criteria, dependency on Phase 4
- `.planning/codebase/CONVENTIONS.md` — naming patterns, error handling, logging

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages already in csproj; IFormFile is built-in; confirmed by csproj inspection
- New entity design (5 tables): HIGH — pattern follows existing CoachCoacheeMapping/IdpItem models directly; no new design patterns
- Sequential lock logic: HIGH — derived directly from the stated requirements and existing status-field patterns in IdpItem/CoachingSession
- File upload pattern: HIGH — IFormFile + IWebHostEnvironment is standard ASP.NET Core 8.0; confirmed no existing usage to conflict with
- Seed data content: MEDIUM — Operator track data derivable from CpdpItem seed; Panelman/Tahun 2/3 data unknown (placeholder recommended)
- PlanIdp hybrid view: HIGH — view currently does role checks; adding Coachee-specific path follows existing if/else branching pattern
- Common pitfalls: HIGH — confirmed by reading actual controller/view code and absence of IFormFile patterns

**Research date:** 2026-02-17
**Valid until:** 2026-03-17 (stable ASP.NET Core 8.0 stack; findings are codebase-specific)
