# Phase 90: KKJ Matrix Admin Full Rewrite — Research

**Researched:** 2026-03-02
**Domain:** File-based document management system for KKJ Matrix (replaces table-based UI)
**Confidence:** HIGH

## Summary

Phase 90 replaces the Admin KKJ Matrix table-based editor (CRUD competency items) and CMP KKJ Matrix worker view with a file-based document system. Admin/HC users upload PDF and Excel files per bagian. Workers download and view files. All KKJ database tables (KkjMatrixItem, KkjTargetValue, KkjColumn, PositionColumnMapping) are dropped via EF migration. Phase 88 (Excel Import to DB) is obsoleted. KkjBagian model remains for bagian tab organization only. This is a complete architectural shift from structured data to document storage, with simplified Admin UI (tab navigation + file list) and simplified CMP worker view (file list + download).

**Primary recommendation:** Use filesystem storage (wwwroot/uploads/kkj/) with optional KkjFile DB model for metadata tracking. Filesystem-only approach is simpler and aligns with existing file upload patterns (ImportWorkers saves to wwwroot/uploads). Tab-based bagian navigation with inline file list management. Separate upload page via POST form (not modal). Archive files on re-upload to same bagian (move to history subfolder).

## User Constraints (from CONTEXT.md)

### Locked Decisions
- File formats: PDF (.pdf) and Excel (.xlsx, .xls) only — max 10MB per file
- Multiple files per bagian (not 1:1) — user can upload multiple KKJ files to same bagian for different purposes
- File storage: "When admin uploads new file, old files with same purpose move to archive/history"
- Admin Page Layout: Tab navigation per bagian (dynamic from KkjBagian.DisplayOrder) + file list table + upload button (separate page)
- Upload Page: Separate form page (not modal) with file picker, title/keterangan field, bagian dropdown, validation (PDF/Excel, max 10MB), redirect back to KkjMatrix tab after success
- CMP/Kkj Worker View: Rewrite to file list + download (no more competency table), role logic same as Phase 89 (L1-L4 see all bagians, L5-L6 see own bagian only)
- DB Cleanup: Drop tables KkjMatrices, KkjTargetValues, KkjColumns, PositionColumnMappings via EF migration. Keep KkjBagian model for bagian CRUD.
- Remove PositionTargetHelper entirely
- Remove all GetTargetLevel/GetTargetLevelAsync usage from assessment flow
- Permissions: Admin + HC upload/delete/manage files; all users can download/view (role-filtered by bagian)
- Navigation: Update link descriptions in CMP hub and Kelola Data hub
- Phase 88 Impact: Phase 88 (KKJ Matrix Excel Import) is obsoleted — remove from roadmap

### Claude's Discretion
- File storage approach: KkjFile DB model vs filesystem scan only
- KkjSectionSelect.cshtml — keep or delete
- File naming convention on server (e.g., "{bagianId}_{timestamp}_{originalName}" vs "{bagianId}_{guid}_{originalName}")
- Archive/history UI design (view history button with list, or inline toggle?)
- Cleanup of related code (SeedMasterData KKJ section, legacy code in views, etc.)

### Deferred Ideas (OUT OF SCOPE)
- CDP PlanIDP connection — user clarified this belongs to Admin Silabus & Coaching Guidance, not KKJ. Separate phase if needed.
- File preview (PDF inline embed) — user chose download-only for now, could add preview later.

## Phase Requirements

No specific requirement IDs provided. Phase contributes to DATA-01 from REQUIREMENTS.md: "KKJ Matrix spreadsheet editor works (CRUD, bulk save, bagian management) and data links correctly to CMP/Kkj view." However, Phase 90 replaces spreadsheet editor with file upload system, so DATA-01 scope shifts from "spreadsheet editing" to "file management."

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 8 + ASP.NET Core | 8.x | Web framework | Project standard since Phase 0 |
| Entity Framework Core | 8.x | ORM for DB operations | Standard pattern in existing code |
| Razor Pages / MVC | Built-in | View rendering | Project standard for Kelola Data hub |
| Bootstrap 5 | Latest (via CDN) | UI framework | Standard across portal (AdminController views, ImportWorkers pattern) |
| Newtonsoft.Json / System.Text.Json | Built-in | JSON serialization | Already used for ViewBag serialization (Phase 89: kkjItems/kkjBagians JSON) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IFormFile (System.Web) | Built-in | File upload handling | Standard pattern in existing ImportWorkers action |
| Path.Combine + Directory APIs | Built-in | Filesystem operations | Safe file path handling in C# (replaces string concat) |
| System.IO.File | Built-in | Read/write files | File archival: move to history folder |
| Mime type detection | Use Path.GetExtension() | Validate file type on upload | Ensure only PDF/Excel accepted |

### No New Dependencies Needed
The existing codebase (Phase 89 + all prior phases) already has all libraries needed for file upload, storage, and metadata tracking. No NuGet packages required.

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
├── AdminController.cs
│   ├── KkjMatrix() [GET]                    — Load bagians + files, render Admin/KkjMatrix.cshtml
│   ├── KkjUpload() [GET]                    — Form page with file picker + bagian dropdown
│   ├── KkjUpload() [POST]                   — Process file upload, validate, save, redirect
│   ├── KkjFileDelete() [POST]               — Soft delete or hard delete file
│   ├── KkjBagianAdd() [POST]                — Add new bagian (EXISTING, reuse)
│   ├── KkjBagianSave() [POST]               — Edit bagian name (EXISTING, reuse)
│   └── KkjBagianDelete() [POST]             — Delete bagian (EXISTING, reuse)
├── CMPController.cs
│   └── Kkj() [GET]                          — Load files for selected bagian, role-filtered
Views/
├── Admin/
│   ├── KkjMatrix.cshtml                     — Tab navigation (bagians) + file list + management
│   ├── KkjUpload.cshtml                     — Upload form (new file)
│   └── KkjSectionSelect.cshtml              — DELETE or REPURPOSE (Claude's discretion)
├── CMP/
│   ├── Kkj.cshtml                           — File list view + download buttons (rewritten)
Models/
├── KkjModels.cs
│   ├── KkjBagian (KEEP)
│   ├── KkjMatrixItem (DELETE)
│   ├── KkjColumn (DELETE)
│   ├── KkjTargetValue (DELETE)
│   ├── PositionColumnMapping (DELETE)
│   └── KkjFile (NEW — optional, Claude's discretion)
wwwroot/
├── uploads/
│   └── kkj/
│       ├── {bagianId}/
│       │   ├── {fileName}                   — Active files
│       │   └── history/
│       │       └── {fileName}               — Archived files (versioned uploads)
Helpers/
├── PositionTargetHelper.cs                  — DELETE entirely
```

### Pattern 1: File Upload with Bagian Context

**What:** Separate upload page (not modal) with file picker, title field, bagian dropdown. Validates file type (PDF/Excel) and size (max 10MB). Stores file in wwwroot/uploads/kkj/{bagianId}/ with timestamp + safe name. Optionally creates KkjFile DB record for metadata (uploader, upload date, purpose).

**When to use:** Admin/HC user needs to upload new KKJ documentation for a specific bagian. File should be versioned; old uploads moved to history folder when new file uploaded with same purpose.

**Example:**
```csharp
// Controllers/AdminController.cs — KkjUpload POST action
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjUpload(IFormFile file, string keterangan, int bagianId)
{
    // Validation
    if (file == null || file.Length == 0)
        return View(new { Error = "File wajib dipilih" });

    var allowedExt = new[] { ".pdf", ".xlsx", ".xls" };
    var ext = Path.GetExtension(file.FileName).ToLower();
    if (!allowedExt.Contains(ext))
        return View(new { Error = "Hanya PDF atau Excel yang didukung" });

    if (file.Length > 10 * 1024 * 1024) // 10MB
        return View(new { Error = "Ukuran file max 10MB" });

    // Storage: wwwroot/uploads/kkj/{bagianId}/
    var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kkj", bagianId.ToString());
    Directory.CreateDirectory(dir);

    // Archive old files if uploading same purpose
    // (User decides: by keterangan match? by file name pattern?)

    // Save with timestamp + original extension
    var safeName = $"{bagianId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{ext}";
    var filePath = Path.Combine(dir, safeName);
    using var stream = new FileStream(filePath, FileMode.Create);
    await file.CopyToAsync(stream);

    // Optional: Store metadata in KkjFile table
    // (Claude's discretion: DB model vs filesystem scan only)

    // Success: redirect back to KkjMatrix at bagianId tab
    return RedirectToAction("KkjMatrix", new { bagian = bagianId });
}
```

**Source:** Verified against existing ImportWorkers pattern in AdminController.cs (line 3776+) — uses IFormFile, Path.Combine, Directory.CreateDirectory, FileStream.CopyAsync, TempData for feedback, redirect on success.

### Pattern 2: Tab-Based Bagian Navigation with File List

**What:** Admin/KkjMatrix.cshtml uses Bootstrap 5 nav-tabs for bagian tabs (dynamic from KkjBagian DB order). Each tab shows file list table (name, type, size, upload date, uploader) + action buttons (download, delete, view history). Tab body includes "Upload File" button that navigates to KkjUpload.cshtml (not inline modal).

**When to use:** Admin needs to view and manage KKJ files organized by bagian. Multiple files per bagian allowed. Files can be downloaded, deleted, or viewed in version history.

**Example (Razor):**
```html
<!-- Views/Admin/KkjMatrix.cshtml -->
@{
    var bagians = ViewBag.Bagians as List<KkjBagian>;
}

<ul class="nav nav-tabs" role="tablist">
    @foreach (var bagian in bagians.OrderBy(b => b.DisplayOrder))
    {
        <li class="nav-item" role="presentation">
            <button class="nav-link @(bagian.Id == ViewBag.SelectedBagianId ? "active" : "")"
                    id="tab-@bagian.Id" data-bs-toggle="tab" data-bs-target="#pane-@bagian.Id">
                @bagian.Name
            </button>
        </li>
    }
</ul>

<div class="tab-content">
    @foreach (var bagian in bagians.OrderBy(b => b.DisplayOrder))
    {
        <div class="tab-pane fade @(bagian.Id == ViewBag.SelectedBagianId ? "show active" : "")" id="pane-@bagian.Id">
            <!-- File list table -->
            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>Nama File</th>
                        <th>Tipe</th>
                        <th>Ukuran</th>
                        <th>Tanggal Upload</th>
                        <th>Uploader</th>
                        <th>Aksi</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var file in (ViewBag.FilesByBagian[bagian.Id] as List<KkjFile>) ?? new List<KkjFile>())
                    {
                        <tr>
                            <td>@file.FileName</td>
                            <td>@Path.GetExtension(file.FilePath).TrimStart('.')</td>
                            <td>@(file.FileSizeBytes / 1024)KB</td>
                            <td>@file.UploadedAt.ToString("dd MMM yyyy HH:mm")</td>
                            <td>@file.UploaderName</td>
                            <td>
                                <a href="@Url.Action("DownloadFile", new { fileId = file.Id })" class="btn btn-sm btn-outline-primary">
                                    <i class="bi bi-download"></i>
                                </a>
                                <button onclick="deleteFile(@file.Id)" class="btn btn-sm btn-outline-danger">
                                    <i class="bi bi-trash"></i>
                                </button>
                                <a href="@Url.Action("FileHistory", new { bagianId = bagian.Id })" class="btn btn-sm btn-outline-secondary">
                                    <i class="bi bi-history"></i>
                                </a>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>

            <!-- Upload button -->
            <a href="@Url.Action("KkjUpload", new { bagianId = bagian.Id })" class="btn btn-primary">
                <i class="bi bi-cloud-arrow-up"></i>Upload File
            </a>
        </div>
    }
</div>
```

**Source:** Bootstrap 5 nav-tabs pattern verified in Phase 89 — used elsewhere in portal. Phase 89 implements dynamic column navigation; this pattern reuses same Bootstrap structure.

### Pattern 3: Role-Based File Access (CMP/Kkj Worker View)

**What:** CMPController.Kkj() loads files for selected bagian. L1-L4 users see all bagians in dropdown selector; L5-L6 see only their own bagian. Files displayed as list with download buttons. No competency table; only file management.

**When to use:** Worker needs to download KKJ files for their assigned bagian. File visibility filtered by user role and unit assignment.

**Example:**
```csharp
// Controllers/CMPController.cs — Kkj() rewritten
[HttpGet]
[Authorize]
public async Task<IActionResult> Kkj(string? section)
{
    var user = await GetCurrentUserAsync();
    var userLevel = user?.RoleLevel ?? 6;

    // L1-L4: all bagians; L5-L6: own bagian only
    IQueryable<KkjBagian> bagiansQuery = _context.KkjBagians;
    if (userLevel >= 5)
    {
        // Own bagian only
        bagiansQuery = bagiansQuery.Where(b => b.Name == user.Bagian);
    }

    var availableBagians = await bagiansQuery.OrderBy(b => b.DisplayOrder).ToListAsync();
    ViewBag.AllBagians = availableBagians;

    // Select default bagian
    var selectedBagian = section ?? availableBagians.FirstOrDefault()?.Name ?? "";

    // Load files for selected bagian
    var bagianRecord = await _context.KkjBagians.FirstOrDefaultAsync(b => b.Name == selectedBagian);
    var files = bagianRecord != null
        ? await _context.KkjFiles
            .Where(f => f.BagianId == bagianRecord.Id && !f.IsArchived)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync()
        : new List<KkjFile>();

    ViewBag.SelectedBagian = selectedBagian;
    ViewBag.Files = files;
    return View();
}
```

**Source:** Pattern verified against Phase 89 CMPController.Kkj() rewrite (commit abf7e4b) — uses role-based bagian filtering, ViewBag population, section param validation.

### Anti-Patterns to Avoid

- **Do NOT keep legacy KKJ table CRUD code.** Phase 90 deletes KkjMatrixItem, KkjColumn, KkjTargetValue models entirely. Any inline edit logic, save buttons, or competency cells must be removed.
- **Do NOT use inline modal for upload.** CONTEXT.md specifies "separate upload page" (like ImportWorkers). Modal-in-page creates UX confusion and validation complexity.
- **Do NOT embed file preview inline.** CONTEXT.md defers PDF preview to future phase. Download-only for Phase 90.
- **Do NOT scan wwwroot at runtime for files.** Create KkjFile DB model (even if optional) to avoid N directory scans on page load. Metadata lookup is O(1) vs filesystem O(N).
- **Do NOT forget archive strategy.** CONTEXT.md says "move to archive/history" on re-upload. Simple approach: mark IsArchived flag in KkjFile, or move file to history/ subfolder. Must be consistent.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| File type validation | Custom regex on extension | C# Path.GetExtension() + whitelist | Handles edge cases (double extensions, case sensitivity) |
| Safe file path construction | String.Format or + concat | Path.Combine() | Prevents path traversal attacks; handles platform-specific separators |
| File size limits | Manual byte counting | IFormFile.Length property | Built-in, reliable |
| Directory creation | Manual mkdir wrapper | Directory.CreateDirectory() | Idempotent, creates parent dirs, handles permissions |
| File streaming to disk | Manual buffer loops | IFormFile.CopyToAsync() | Async, efficient, uses system streams |
| Bagian CRUD logic | New bagian operations | Reuse KkjBagianAdd/Save/Delete from Phase 89 AdminController | Already tested, works with KkjBagian model |
| Bagian dropdown filtering | Custom bagian lists per view | ViewBag.AvailableBagians from controller | Single source of truth; role filtering in one place |

**Key insight:** File upload is deceptively complex when handling validation, path safety, concurrency (simultaneous uploads), and archival. Use IFormFile's built-in APIs, filesystem APIs with Path.Combine, and DB metadata (KkjFile) for reliable implementation. Custom file handling code introduces security risks (path traversal) and UX bugs (duplicate uploads, race conditions).

## Common Pitfalls

### Pitfall 1: Assuming File Extension Validation is Simple

**What goes wrong:** Developer checks `if (filename.EndsWith(".pdf"))` or uses IndexOf("."). Attackers bypass via ".pdf.exe" or ".PDF" (case). Non-ASCII filenames cause encoding issues.

**Why it happens:** File upload seems straightforward; extension feels like a simple string check. Easy to miss case sensitivity and double extensions.

**How to avoid:** Use `Path.GetExtension(filename).ToLower()` and whitelist allowed extensions: `new[] { ".pdf", ".xlsx", ".xls" }`. Validate before filename is used. Test with: ".pdf.exe", ".PDF", ".xlsx.zip", non-ASCII names.

**Warning signs:** Code with `filename.Contains("pdf")` or hardcoded string checks. Missing `.ToLower()` call.

### Pitfall 2: Not Validating File Size Before Streaming to Disk

**What goes wrong:** Code checks `IFormFile.Length` in bytes but forgets that upload can be interrupted or spoofed. No max-size check → disk fills up with large files.

**Why it happens:** File upload post-processing (streaming) can take time. Developer forgets to check size early. Assumes IIS upload limits suffice (they don't).

**How to avoid:** Check `file.Length > 10 * 1024 * 1024` (10MB) before `CopyToAsync()`. Reject immediately if oversized. Document max-size in upload form ("Max 10MB"). Test with file >10MB to ensure rejection.

**Warning signs:** No length check before CopyToAsync. Missing error message if file too large.

### Pitfall 3: Archive Strategy Forgotten on Re-Upload

**What goes wrong:** User uploads KKJ file v1.0, then KKJ file v2.0 with same name. Old file is silently overwritten (no version history). User can't access previous version.

**Why it happens:** First implementation ignores archive requirement. Code only stores latest file. CONTEXT.md says "old files move to archive" but implementation doesn't implement it.

**How to avoid:** Before saving new file with same bagian: either (a) move old files to history/ subfolder, or (b) set IsArchived=true flag in KkjFile table. Create FileHistory() action to list archived files. Test: upload file1.pdf, then file2.pdf, verify file1.pdf still accessible in history.

**Warning signs:** No history view. No archive folder. Timestamp not included in saved filename.

### Pitfall 4: Missing Role Check on File Download

**What goes wrong:** Download endpoint `DownloadFile(int fileId)` loads file by ID but doesn't verify user can access that bagian. L5 (Coach) can download files from bagians they're not assigned to.

**Why it happens:** Download logic focuses on file I/O and forgets to re-validate bagian access. Easy to miss role filtering in action.

**How to avoid:** In DownloadFile(), load KkjFile by ID, check BagianId, then validate user access: `if (userLevel >= 5 && bagian.Name != userBagian) return Forbid()`. Reuse bagian filtering logic from Kkj() action.

**Warning signs:** No authorization check in download action. File.IsArchived confuses with delete permission (archived ≠ inaccessible).

### Pitfall 5: Deleting Database Tables Before Verifying Assessment Flow Won't Break

**What goes wrong:** Phase 90 drops KkjMatrixItem, KkjTargetValue, KkjColumn tables via EF migration. But somewhere in assessment flow code, `GetTargetLevel(competencyId)` is still called → runtime null reference error. Page breaks in Phase 84/85 testing.

**Why it happens:** PositionTargetHelper.GetTargetLevel() was removed, but developer missed a reference in AssessmentController or helper. Easy to miss if you don't grep the whole codebase.

**How to avoid:** Before writing migration to drop tables: grep entire Controllers, Services, Helpers for KkjMatrixItem, KkjColumn, KkjTargetValue, GetTargetLevel, GetTargetLevelAsync. List all references. Verify each is removed or refactored. Create new assessment with monitoring to ensure no errors.

**Warning signs:** Build succeeds but runtime throws "Cannot create index on null reference" or "Cannot find type KkjMatrixItem". Assessment page 404s after file upload.

### Pitfall 6: Folder Permission Issues on wwwroot/uploads/kkj/

**What goes wrong:** wwwroot/uploads/kkj folder doesn't exist initially. Code calls `Directory.CreateDirectory()` which succeeds, but IIS app pool user lacks write permission. File save silently fails or throws UnauthorizedAccessException.

**Why it happens:** wwwroot folder has permissive ACLs (web server writes logs). Subdirs might not inherit. Developer assumes Directory.CreateDirectory() handles permissions.

**How to avoid:** Run app in development first (local IIS Express or Kestrel — no permission issues). In production deployment, pre-create wwwroot/uploads/kkj/ and test write access. Wrap file saves in try-catch with specific logging: `catch (UnauthorizedAccessException ex)`. Document folder setup in deployment guide.

**Warning signs:** Upload succeeds in dev but fails in production. No error logged (FileStream silently returns). Web server event log shows access denied.

## Code Examples

Verified patterns from Phase 89 and existing codebase:

### File Upload Form (KkjUpload.cshtml)

**Source:** Verified against ImportWorkers.cshtml (Views/Admin/ImportWorkers.cshtml) — file picker, drag-drop, validation, process button, results display.

```html
<!-- Views/Admin/KkjUpload.cshtml -->
@{
    ViewData["Title"] = "Upload File KKJ Matrix";
    var bagians = ViewBag.Bagians as List<KkjBagian>;
}

<div class="container py-4" style="max-width: 860px;">
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a href="@Url.Action("Index", "Admin")">Kelola Data</a></li>
            <li class="breadcrumb-item"><a href="@Url.Action("KkjMatrix", "Admin")">KKJ Matrix</a></li>
            <li class="breadcrumb-item active">Upload File</li>
        </ol>
    </nav>

    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="fw-bold"><i class="bi bi-cloud-arrow-up me-2"></i>Upload File KKJ Matrix</h2>
        <a href="@Url.Action("KkjMatrix", "Admin")" class="btn btn-outline-secondary">
            <i class="bi bi-arrow-left me-2"></i>Kembali
        </a>
    </div>

    <!-- Error alert if any -->
    @if (ViewBag.Error != null)
    {
        <div class="alert alert-danger alert-dismissible fade show">
            <i class="bi bi-exclamation-triangle me-2"></i>@ViewBag.Error
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    }

    <!-- Upload form -->
    <div class="card border-0 shadow-sm">
        <div class="card-body">
            <form asp-action="KkjUpload" asp-controller="Admin" method="post" enctype="multipart/form-data" id="uploadForm">
                @Html.AntiForgeryToken()

                <!-- Bagian selector -->
                <div class="mb-3">
                    <label for="bagianId" class="form-label fw-semibold">Pilih Bagian <span class="text-danger">*</span></label>
                    <select name="bagianId" id="bagianId" class="form-select" required>
                        <option value="">-- Pilih Bagian --</option>
                        @foreach (var b in bagians.OrderBy(x => x.DisplayOrder))
                        {
                            <option value="@b.Id">@b.Name</option>
                        }
                    </select>
                </div>

                <!-- File picker -->
                <div class="mb-3">
                    <label for="file" class="form-label fw-semibold">Pilih File <span class="text-danger">*</span></label>
                    <div class="upload-zone border-2 border-dashed rounded-3 p-5 text-center"
                         id="uploadZone"
                         onclick="document.getElementById('fileInput').click()"
                         style="border-color: #d1d5db; cursor: pointer; transition: all 0.2s;">
                        <i class="bi bi-cloud-arrow-up fs-1 text-muted mb-3 d-block"></i>
                        <p class="fw-semibold mb-1" id="dropText">Klik atau drag &amp; drop file di sini</p>
                        <p class="small text-muted mb-0">Format: PDF atau Excel (.pdf, .xlsx, .xls) — Max 10MB</p>
                    </div>
                    <input type="file" name="file" id="fileInput" accept=".pdf,.xlsx,.xls" class="d-none" required />
                </div>

                <!-- Keterangan (optional) -->
                <div class="mb-3">
                    <label for="keterangan" class="form-label">Keterangan (Opsional)</label>
                    <input type="text" name="keterangan" id="keterangan" class="form-control" placeholder="Contoh: KKJ Rev 2.1, KKJ Operator Level, dll" />
                </div>

                <!-- Buttons -->
                <div class="d-flex justify-content-end gap-2 mt-4">
                    <a href="@Url.Action("KkjMatrix", "Admin")" class="btn btn-light">Batal</a>
                    <button type="submit" class="btn btn-primary px-4" id="btnUpload" disabled>
                        <i class="bi bi-check2-all me-2"></i>Upload File
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Format info -->
    <div class="card border-0 shadow-sm mt-4">
        <div class="card-header border-0">
            <h6 class="mb-0 fw-bold"><i class="bi bi-info-circle me-2"></i>Persyaratan File</h6>
        </div>
        <div class="card-body">
            <ul class="mb-0">
                <li>Format file: <strong>PDF (.pdf) atau Excel (.xlsx, .xls)</strong></li>
                <li>Ukuran maksimal: <strong>10MB</strong></li>
                <li>Satu atau lebih file dapat di-upload per bagian</li>
                <li>File baru dengan tujuan sama akan mengarsipkan file lama</li>
            </ul>
        </div>
    </div>
</div>

<style>
    .upload-zone:hover, .upload-zone.drag-over {
        border-color: #2563eb !important;
        background-color: #eff6ff;
    }
</style>

<script>
    const fileInput = document.getElementById('fileInput');
    const uploadZone = document.getElementById('uploadZone');
    const dropText = document.getElementById('dropText');
    const btnUpload = document.getElementById('btnUpload');

    fileInput.addEventListener('change', function() {
        if (this.files.length > 0) {
            const file = this.files[0];
            const sizeInMB = (file.size / (1024 * 1024)).toFixed(2);
            dropText.textContent = `File: ${file.name} (${sizeInMB}MB)`;
            uploadZone.style.borderColor = '#16a34a';
            uploadZone.style.backgroundColor = '#f0fdf4';
            btnUpload.disabled = false;
        }
    });

    uploadZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadZone.classList.add('drag-over');
    });
    uploadZone.addEventListener('dragleave', () => uploadZone.classList.remove('drag-over'));
    uploadZone.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadZone.classList.remove('drag-over');
        const file = e.dataTransfer.files[0];
        if (file && ['.pdf', '.xlsx', '.xls'].some(ext => file.name.toLowerCase().endsWith(ext))) {
            const dt = new DataTransfer();
            dt.items.add(file);
            fileInput.files = dt.files;
            const sizeInMB = (file.size / (1024 * 1024)).toFixed(2);
            dropText.textContent = `File: ${file.name} (${sizeInMB}MB)`;
            uploadZone.style.borderColor = '#16a34a';
            uploadZone.style.backgroundColor = '#f0fdf4';
            btnUpload.disabled = false;
        } else {
            dropText.textContent = 'Hanya PDF atau Excel yang didukung!';
            uploadZone.style.borderColor = '#dc2626';
        }
    });
</script>
```

### AdminController.KkjUpload() POST Action

**Source:** Pattern verified against AdminController.ImportWorkers() (line 3776+) and interview upload pattern (line ~1440).

```csharp
// Controllers/AdminController.cs

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> KkjUpload(IFormFile file, string keterangan, int bagianId)
{
    if (file == null || file.Length == 0)
    {
        TempData["Error"] = "Pilih file terlebih dahulu.";
        return RedirectToAction("KkjUpload", new { bagianId });
    }

    // Validate file extension
    var allowedExtensions = new[] { ".pdf", ".xlsx", ".xls" };
    var fileExtension = Path.GetExtension(file.FileName).ToLower();

    if (!allowedExtensions.Contains(fileExtension))
    {
        TempData["Error"] = "Hanya file PDF atau Excel yang didukung.";
        return RedirectToAction("KkjUpload", new { bagianId });
    }

    // Validate file size (max 10MB)
    const long maxFileSize = 10 * 1024 * 1024; // 10MB
    if (file.Length > maxFileSize)
    {
        TempData["Error"] = "Ukuran file terlalu besar (max 10MB).";
        return RedirectToAction("KkjUpload", new { bagianId });
    }

    // Verify bagian exists
    var bagian = await _context.KkjBagians.FindAsync(bagianId);
    if (bagian == null)
    {
        TempData["Error"] = "Bagian tidak ditemukan.";
        return RedirectToAction("KkjMatrix");
    }

    try
    {
        // Create storage directory if not exists
        var storageDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "kkj", bagianId.ToString());
        Directory.CreateDirectory(storageDir);

        // Generate safe filename: {timestamp}_{originalName}
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var originalName = Path.GetFileNameWithoutExtension(file.FileName);
        var safeName = $"{timestamp}_{originalName}{fileExtension}";
        var filePath = Path.Combine(storageDir, safeName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Optional: Create KkjFile DB record for metadata tracking
        var currentUser = await GetCurrentUserAsync();
        var kkjFile = new KkjFile
        {
            BagianId = bagianId,
            FileName = file.FileName,
            FilePath = $"/uploads/kkj/{bagianId}/{safeName}",
            FileSizeBytes = file.Length,
            FileType = fileExtension.TrimStart('.'),
            Keterangan = keterangan,
            UploadedAt = DateTimeOffset.UtcNow,
            UploaderName = currentUser?.FullName ?? "Unknown",
            IsArchived = false
        };
        _context.KkjFiles.Add(kkjFile);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"File '{file.FileName}' berhasil di-upload.";
        return RedirectToAction("KkjMatrix", new { bagian = bagian.Name });
    }
    catch (Exception ex)
    {
        TempData["Error"] = $"Error: {ex.Message}";
        return RedirectToAction("KkjUpload", new { bagianId });
    }
}

// GET form page
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> KkjUpload(int? bagianId)
{
    var bagians = await _context.KkjBagians.OrderBy(b => b.DisplayOrder).ToListAsync();
    ViewBag.Bagians = bagians;
    ViewBag.SelectedBagianId = bagianId;
    return View();
}
```

### CMP/Kkj.cshtml (File List View)

**Source:** Based on Phase 89 CMPController.Kkj() and Views/CMP/Kkj.cshtml rewrite (commit abf7e4b).

```html
<!-- Views/CMP/Kkj.cshtml -->
@{
    ViewData["Title"] = "KKJ Matrix";
    var bagians = ViewBag.AllBagians as List<KkjBagian>;
    var selectedBagian = ViewBag.SelectedBagian as string;
    var files = ViewBag.Files as List<KkjFile>;
}

<div class="container-fluid px-4 py-4">
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a href="@Url.Action("Index", "CMP")">CMP Hub</a></li>
            <li class="breadcrumb-item active">KKJ Matrix</li>
        </ol>
    </nav>

    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2 class="fw-bold">KKJ Matrix</h2>
    </div>

    <!-- Bagian selector (dropdown for L1-L4, badge for L5-L6) -->
    @if (bagians.Count > 1)
    {
        <div class="mb-4">
            <label for="bagianSelect" class="form-label fw-semibold">Pilih Bagian:</label>
            <select class="form-select" id="bagianSelect" onchange="location.href=`@Url.Action("Kkj", "CMP")?section=${this.value}`">
                <option value="">-- Semua Bagian --</option>
                @foreach (var b in bagians.OrderBy(x => x.DisplayOrder))
                {
                    var selected = b.Name == selectedBagian ? "selected" : "";
                    <option value="@b.Name" @Html.Raw(selected)>@b.Name</option>
                }
            </select>
        </div>
    }
    else if (bagians.Count == 1)
    {
        <div class="mb-4">
            <span class="badge bg-primary fs-6">@bagians[0].Name</span>
        </div>
    }

    <!-- Files list -->
    @if (files != null && files.Count > 0)
    {
        <div class="card border-0 shadow-sm">
            <div class="card-header bg-light">
                <h6 class="mb-0 fw-semibold"><i class="bi bi-file-earmark me-2"></i>Dokumen KKJ Matrix</h6>
            </div>
            <div class="table-responsive">
                <table class="table table-sm table-hover mb-0">
                    <thead class="bg-light">
                        <tr>
                            <th class="p-3">Nama File</th>
                            <th class="p-3">Tipe</th>
                            <th class="p-3">Ukuran</th>
                            <th class="p-3">Tanggal Upload</th>
                            <th class="p-3">Keterangan</th>
                            <th class="p-3 text-center">Aksi</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var f in files.OrderByDescending(x => x.UploadedAt))
                        {
                            <tr>
                                <td class="p-3"><i class="bi bi-file me-2"></i>@f.FileName</td>
                                <td class="p-3"><span class="badge bg-info">@f.FileType.ToUpper()</span></td>
                                <td class="p-3">@((f.FileSizeBytes / 1024.0).ToString("F2")) KB</td>
                                <td class="p-3"><small>@f.UploadedAt.ToString("dd MMM yyyy HH:mm")</small></td>
                                <td class="p-3"><small>@f.Keterangan</small></td>
                                <td class="p-3 text-center">
                                    <a href="@Url.Action("DownloadFile", new { fileId = f.Id })" class="btn btn-sm btn-primary" title="Download">
                                        <i class="bi bi-download"></i>
                                    </a>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <i class="bi bi-info-circle me-2"></i>Belum ada file KKJ Matrix untuk bagian ini.
        </div>
    }
</div>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Table-based KKJ editor (spreadsheet UI) | File-based document management (PDF/Excel upload) | Phase 90 | Shift from structured data entry to document storage; simplifies admin workflow, reduces schema complexity |
| KkjMatrixItem, KkjColumn, KkjTargetValue DB tables | KkjFile metadata table (optional) + filesystem storage | Phase 90 | Database schema simplified; file I/O replaces SQL queries; archive via filesystem subfolder or IsArchived flag |
| PositionTargetHelper with GetTargetLevel() query | Files only; no competency level lookup | Phase 90 | Assessment flow no longer depends on KKJ matrix data; removes complexity from assessment scoring |
| Phase 88 (Excel Import to DB) | Phase 88 obsoleted; file upload replaces import | Phase 90 | One-way document upload instead of bidirectional data sync; simpler, fewer edge cases |
| Multi-step inline editing (select cell, edit, save) | Single file upload per action | Phase 90 | Simpler UX; no cell validation, no bulk save dialog |

**Deprecated/outdated:**
- KkjMatrixItem model: Drops in Phase 90 migration (no longer needed for file-based system)
- KkjColumn, KkjTargetValue models: Dropped in Phase 90 migration
- PositionColumnMapping model: Dropped in Phase 90 migration (no more position-to-column mapping)
- PositionTargetHelper.cs: Deleted entirely (no more GetTargetLevel logic)
- Phase 88 (KKJ Matrix Excel Import): Obsoleted; roadmap should remove it

## Open Questions

1. **KkjFile DB Model vs Filesystem Scan Only?**
   - What we know: CONTEXT.md marks "file storage approach" as Claude's discretion. Filesystem-only means scanning wwwroot/uploads/kkj/ at runtime; DB model means metadata lookup.
   - What's unclear: Does user prefer DB model (future-safe, queryable history) or filesystem-only (simpler, fewer tables)?
   - Recommendation: Implement KkjFile model (low cost, high value). Enables archive queries, uploader tracking, soft-delete without disk I/O. If user prefers filesystem scan later, can migrate by reading KkjFile table scan instead.

2. **Archive Strategy: Filesystem Subfolder or DB Flag?**
   - What we know: CONTEXT.md says "move to archive/history." Unclear if move means mv to history/ subfolder or set IsArchived=true in KkjFile.
   - What's unclear: Should archived files be hidden from list by default, or shown with strikethrough? Is history view separate page or tab within KkjMatrix?
   - Recommendation: Use IsArchived flag in KkjFile (reversible, queryable). Show active files by default; add "View History" link per bagian to show archived files.

3. **Cleanup of Legacy KKJ References?**
   - What we know: Need to grep codebase for KkjMatrixItem, KkjColumn, GetTargetLevel, remove all references before migration.
   - What's unclear: Are there references in SeedMasterData, test data setup, or other places not yet discovered?
   - Recommendation: In Phase 90 Task 1 (DB/Model cleanup), run comprehensive grep for each model/method name. List all references. Verify each one is removed or safe to drop.

4. **KkjSectionSelect.cshtml: Keep or Delete?**
   - What we know: CONTEXT.md marks as Claude's discretion. File used in Phase 89 for bagian selection redirect (now removed).
   - What's unclear: Is it referenced elsewhere, or can it be safely deleted?
   - Recommendation: Check git log for usage. If last commit is Phase 89 redirect removal, delete it. Otherwise, investigate references.

## Validation Architecture

**Test Framework Status:** No automated test infrastructure exists in this codebase (xUnit project files not found). Phase 90 contributes to manual QA only.

### Validation Strategy

Phase 90 does not introduce new automated test requirements (per REQUIREMENTS.md, automated UI tests deferred to v3.1). Validation is manual:

1. **Admin/KkjMatrix** — Verify:
   - Bagian tabs render correctly (dynamic from KkjBagian.DisplayOrder)
   - File list table shows all active files per bagian
   - Upload button navigates to KkjUpload form
   - Delete button removes file (verify filesystem + DB)
   - View History shows archived files
   - Bagian CRUD (rename, delete) still works

2. **Admin/KkjUpload** — Verify:
   - Form shows all available bagians
   - File picker accepts PDF/Excel only (drag-drop + click)
   - Size validation rejects >10MB files
   - Successful upload redirects to KkjMatrix at uploaded bagian's tab
   - Error messages display if validation fails

3. **CMP/Kkj Worker View** — Verify:
   - L1-L4 users see all bagians in dropdown
   - L5-L6 users see only own bagian (no dropdown, badge only)
   - File list shows all active files
   - Download button works (no errors)
   - URL manipulation (e.g., ?section=GAST for L5 user in RFCC) is blocked

4. **Database Cleanup** — Verify:
   - Migration applies cleanly: KkjMatrices, KkjColumns, KkjTargetValues, PositionColumnMappings dropped
   - Build succeeds with no model references
   - Assessment flow page loads (no GetTargetLevel errors)

### Manual Test Plan (High-Level)

| Action | Expected Result | Role | Verification |
|--------|-----------------|------|--------------|
| Admin navigates to Kelola Data > KKJ Matrix | Page loads with bagian tabs | Admin | Tabs render, no errors |
| Admin clicks Upload button on tab | Navigates to KkjUpload form | Admin | Form page loads, bagian field pre-filled if clicked from tab |
| Admin selects PDF file, enters title, submits | File saved, redirect to KkjMatrix at tab | Admin | File appears in list; filesystem path correct |
| Admin uploads 2nd file to same bagian | Both files show in list (not overwritten) | Admin | Multiple files visible; old file not deleted |
| Admin clicks delete on file | File removed from list | Admin | File no longer in wwwroot/uploads, KkjFile.IsArchived=true |
| L1 user navigates to CMP/Kkj | Bagian dropdown shows all bagians | L1 | Dropdown options correct |
| L5 user navigates to CMP/Kkj | Single bagian shown (own), no dropdown | L5 | Badge shows own bagian; no dropdown |
| L5 user attempts URL ?section=GAST (different bagian) | Page rejects, shows error or redirects to own bagian | L5 | Role check prevents cross-bagian access |
| Exam/Assessment page loads | No GetTargetLevel errors | Any | Assessment works after KKJ table drop |

**Result:** Manual verification checklist passed = Phase 90 QA complete.

## Sources

### Primary (HIGH confidence)

- **Phase 89 RESEARCH.md & SUMMARY.md** — KKJ Matrix Dynamic Columns design verified, AdminController KkjBagian CRUD patterns, CMPController role-based filtering patterns, Views/CMP/Kkj.cshtml rewrite with dynamic columns confirmed
- **CONTEXT.md (Phase 90)** — Locked decisions on file format, storage, UI layout, permissions documented directly by user discussion
- **AdminController.cs (ImportWorkers pattern)** — Verified IFormFile handling (lines 3776+), file streaming (CopyToAsync), Path.Combine usage, TempData feedback, validation pattern
- **AdminController.cs (Interview upload pattern)** — Verified directory creation (wwwroot/uploads/interviews), safe filename generation (timestamp + original name), IFormFile size checking
- **ApplicationDbContext.cs** — Verified current KkjBagian, KkjMatrixItem, KkjColumn, KkjTargetValue, PositionColumnMapping configuration and relationships

### Secondary (MEDIUM confidence)

- **Phase 89-04-SUMMARY.md** — CMPController.Kkj() rewrite verified: role-based bagian access (userLevel threshold <=4), ViewBag.AllBagians pattern, URL validation for L5/L6 users
- **.NET documentation (Path.Combine, Directory.CreateDirectory, IFormFile)** — Standard APIs documented on Microsoft docs; usage patterns verified in existing codebase

### Tertiary (LOW confidence, deferred to planner)

- File archival UI design specifics — CONTEXT.md defers to Claude's discretion; planner will decide between history subfolder vs DB IsArchived flag vs separate page

## Metadata

**Confidence breakdown:**
- **Standard Stack: HIGH** — All libraries (.NET, EF Core, IFormFile, Razor) are existing project standards verified in code
- **Architecture: HIGH** — Phase 89 established patterns; Phase 90 reuses file upload pattern from ImportWorkers + role-based access from Phase 89
- **Pitfalls: HIGH** — Common file upload mistakes documented from 20+ years of web security research; validated against existing codebase patterns
- **Validation: MEDIUM** — No automated tests exist yet; manual test plan based on Phase 89 QA patterns and REQUIREMENTS.md scope

**Research date:** 2026-03-02
**Valid until:** 2026-03-09 (7 days — covers Phase 90 planning and execution; file handling unlikely to change)

---
*Phase 90: KKJ Matrix Admin Full Rewrite — Document-Based Page*
*Research completed: 2026-03-02*
