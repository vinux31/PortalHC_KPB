# CreateAssessment Form Fixes (12 Poin) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 12 issues in the CreateAssessment form for Standard and Pre-Post Test modes — covering UI layout, validation consistency, success feedback, SamePackage logic, and Proton guard.

**Architecture:** Changes span 3 layers: Model (add SamePackage column), Controller (fix TempData keys, add server validation, build Pre-Post success data, auto-copy packages), View (reorganize Step 3 layout into 4 groups, update summary, update success modal, add Proton guard). Existing `CopyPackagesFromPre` action is reused for SamePackage logic.

**Tech Stack:** ASP.NET Core MVC, EF Core (SQLite), Razor Views, vanilla JavaScript, Bootstrap 5

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `Models/AssessmentSession.cs` | Modify | Add `SamePackage` bool property |
| `Controllers/AssessmentAdminController.cs` | Modify | Fix TempData keys, add EWCD server validation, build Pre-Post success data, auto-copy packages when SamePackage |
| `Views/Admin/CreateAssessment.cshtml` | Modify | Reorganize Step 3 layout, update JS toggle/validation/summary, update success modal, add Proton guard, reset logic |
| `Views/Admin/ManagePackages.cshtml` | Modify | Lock UI when SamePackage=true on Post-Test |
| Migration file (auto-generated) | Create | Add SamePackage column |

---

### Task 1: Fix TempData Key + Pre-Post Success Redirect (Poin 1, 2)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs:1126-1137`

- [ ] **Step 1: Fix TempData["Success"] → TempData["SuccessMessage"]**

In `Controllers/AssessmentAdminController.cs`, find line 1126:
```csharp
TempData["Success"] = $"Assessment Pre-Post Test '{model.Title}' berhasil dibuat untuk {UserIds.Count} peserta ({preSessions.Count + postSessions.Count} sesi).";
return RedirectToAction("ManageAssessment");
```

Replace with:
```csharp
TempData["SuccessMessage"] = $"Assessment Pre-Post Test '{model.Title}' berhasil dibuat untuk {UserIds.Count} peserta ({preSessions.Count + postSessions.Count} sesi).";

// Build success modal data (same pattern as Standard mode)
var pptCreatedSessions = new List<object>();
for (int i = 0; i < preSessions.Count; i++)
{
    var assignedUser = userDictionary[UserIds[i]];
    pptCreatedSessions.Add(new
    {
        PreId = preSessions[i].Id,
        PostId = postSessions[i].Id,
        UserId = UserIds[i],
        UserName = assignedUser.FullName ?? UserIds[i],
        UserEmail = assignedUser.Email ?? ""
    });
}

TempData["CreatedAssessment"] = System.Text.Json.JsonSerializer.Serialize(new
{
    Count = UserIds.Count,
    Title = model.Title,
    Category = model.Category,
    IsPrePostTest = true,
    PreSchedule = PreSchedule!.Value.ToString("dd MMMM yyyy HH:mm"),
    PostSchedule = PostSchedule!.Value.ToString("dd MMMM yyyy HH:mm"),
    PreDurationMinutes = PreDurationMinutes!.Value,
    PostDurationMinutes = PostDurationMinutes!.Value,
    Status = "Upcoming",
    IsTokenRequired = model.IsTokenRequired,
    AccessToken = model.AccessToken,
    SamePackage = SamePackage,
    Sessions = pptCreatedSessions
});

return RedirectToAction("CreateAssessment");
```

- [ ] **Step 2: Verify Standard mode TempData is unchanged**

Confirm lines 1311-1324 still use `TempData["CreatedAssessment"]` with the existing structure. No changes needed there, but add `IsPrePostTest = false` for the JS to distinguish:

Find in the Standard success serialization block (~line 1311):
```csharp
TempData["CreatedAssessment"] = System.Text.Json.JsonSerializer.Serialize(new
{
    Count = createdSessions.Count,
    Title = model.Title,
    Category = model.Category,
    Schedule = model.Schedule.ToString("dd MMMM yyyy"),
```

Add `IsPrePostTest = false` after `Category`:
```csharp
TempData["CreatedAssessment"] = System.Text.Json.JsonSerializer.Serialize(new
{
    Count = createdSessions.Count,
    Title = model.Title,
    Category = model.Category,
    IsPrePostTest = false,
    Schedule = model.Schedule.ToString("dd MMMM yyyy"),
```

- [ ] **Step 3: Build and run to verify no compile errors**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "fix: TempData key mismatch + add Pre-Post success modal data (poin 1,2)"
```

---

### Task 2: Add SamePackage Column to Model + Migration (Poin 8 - part 1)

**Files:**
- Modify: `Models/AssessmentSession.cs`
- Create: Migration file (auto-generated)

- [ ] **Step 1: Add SamePackage property to AssessmentSession model**

In `Models/AssessmentSession.cs`, after the `HasManualGrading` property (line 154), add:

```csharp
/// <summary>
/// True jika Post-Test ini menggunakan paket soal yang sama dengan Pre-Test linked-nya.
/// Hanya relevan untuk session dengan AssessmentType == "PostTest".
/// Saat true, ManagePackages di-lock dan package otomatis disalin dari Pre-Test.
/// </summary>
public bool SamePackage { get; set; } = false;
```

- [ ] **Step 2: Create EF migration**

Run:
```bash
dotnet ef migrations add AddSamePackageToAssessmentSession
```
Expected: Migration file created in `Migrations/` folder

- [ ] **Step 3: Apply migration**

Run:
```bash
dotnet ef database update
```
Expected: Database updated successfully

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Models/AssessmentSession.cs Migrations/
git commit -m "feat: add SamePackage column to AssessmentSession (poin 8)"
```

---

### Task 3: Server-Side EWCD Validation + SamePackage in Controller (Poin 7, 8 - part 2)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs:820-894`

- [ ] **Step 1: Add EWCD server validation for Standard mode**

Find line 820-821:
```csharp
// ExamWindowCloseDate is optional — remove from ModelState to prevent accidental validation failure
ModelState.Remove("ExamWindowCloseDate");
```

Replace with:
```csharp
// ExamWindowCloseDate validation
ModelState.Remove("ExamWindowCloseDate"); // Remove model binding error first
if (!isPrePostMode)
{
    if (!model.ExamWindowCloseDate.HasValue)
    {
        ModelState.AddModelError("ExamWindowCloseDate", "Tanggal tutup ujian wajib diisi.");
    }
    else if (model.ExamWindowCloseDate < model.Schedule)
    {
        ModelState.AddModelError("ExamWindowCloseDate", "Tanggal tutup ujian tidak boleh sebelum tanggal jadwal.");
    }
}
```

- [ ] **Step 2: Add EWCD server validation for Pre-Post mode**

Find in the Pre-Post validation block (after line 889, PostSchedule validation):
```csharp
// D-06: Schedule Post harus setelah Pre (T-297-02)
if (PreSchedule.HasValue && PostSchedule.HasValue && PostSchedule <= PreSchedule)
    ModelState.AddModelError("PostSchedule", "Jadwal Post-Test harus setelah jadwal Pre-Test.");
```

Add after it:
```csharp
// EWCD wajib untuk Pre-Post
if (!PreExamWindowCloseDate.HasValue)
    ModelState.AddModelError("PreExamWindowCloseDate", "Batas waktu pengerjaan Pre-Test wajib diisi.");
if (!PostExamWindowCloseDate.HasValue)
    ModelState.AddModelError("PostExamWindowCloseDate", "Batas waktu pengerjaan Post-Test wajib diisi.");
```

- [ ] **Step 3: Set SamePackage on Post sessions**

In the Pre-Post creation block, find where postSession is created (~line 1088):
```csharp
var postSession = new AssessmentSession
{
    ...
    RenewsSessionId = model.RenewsSessionId,
    RenewsTrainingId = model.RenewsTrainingId
};
```

Add `SamePackage = SamePackage` to the object initializer:
```csharp
var postSession = new AssessmentSession
{
    ...
    RenewsSessionId = model.RenewsSessionId,
    RenewsTrainingId = model.RenewsTrainingId,
    SamePackage = SamePackage
};
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat: add EWCD server validation + SamePackage flag on Post session (poin 7, 8)"
```

---

### Task 4: Auto-Copy Packages When SamePackage=true (Poin 8 - part 3)

**Files:**
- Modify: `Controllers/AssessmentAdminController.cs` (ManagePackages GET + new trigger in package creation)

- [ ] **Step 1: Add SamePackage info to ManagePackages GET**

In `ManagePackages` action (line 3817), after the `ViewBag.PreSessionId` block (line 3871), add:

```csharp
// SamePackage lock: jika Post-Test dengan SamePackage=true, lock editing
ViewBag.IsSamePackageLocked = isPostSession && assessment.SamePackage;
```

- [ ] **Step 2: Add auto-copy trigger when Pre-Test package changes and SamePackage=true**

In the `CreatePackage` action (after `await _context.SaveChangesAsync()` at line 3969), add:

```csharp
// Auto-sync: jika ini Pre-Test session dan ada Post-Test dengan SamePackage=true, trigger copy
if (assessment.AssessmentType == "PreTest" && assessment.LinkedSessionId.HasValue)
{
    var postSession = await _context.AssessmentSessions.FindAsync(assessment.LinkedSessionId.Value);
    if (postSession != null && postSession.SamePackage)
    {
        await SyncPackagesToPost(assessment.Id, postSession.Id);
    }
}
```

- [ ] **Step 3: Add similar auto-sync to DeletePackage action**

In `DeletePackage` action, after `await _context.SaveChangesAsync()` (line 4010), add:

```csharp
// Auto-sync: jika ini Pre-Test session, sync ke Post jika SamePackage=true
var deletedFromSession = await _context.AssessmentSessions.FindAsync(assessmentId);
if (deletedFromSession?.AssessmentType == "PreTest" && deletedFromSession.LinkedSessionId.HasValue)
{
    var postSession = await _context.AssessmentSessions.FindAsync(deletedFromSession.LinkedSessionId.Value);
    if (postSession != null && postSession.SamePackage)
    {
        await SyncPackagesToPost(deletedFromSession.Id, postSession.Id);
    }
}
```

- [ ] **Step 4: Extract SyncPackagesToPost helper method**

Add a private helper method in the controller (near the CopyPackagesFromPre action):

```csharp
/// <summary>
/// Deep-clones all packages+questions+options from Pre-Test session to Post-Test session.
/// Deletes existing Post packages first. Reuses logic from CopyPackagesFromPre.
/// </summary>
private async Task SyncPackagesToPost(int preSessionId, int postSessionId)
{
    // Hapus paket Post yang ada
    var existingPostPkgs = await _context.AssessmentPackages
        .Include(p => p.Questions).ThenInclude(q => q.Options)
        .Where(p => p.AssessmentSessionId == postSessionId)
        .ToListAsync();

    foreach (var pkg in existingPostPkgs)
    {
        foreach (var q in pkg.Questions)
            _context.PackageOptions.RemoveRange(q.Options);
        _context.PackageQuestions.RemoveRange(pkg.Questions);
    }
    _context.AssessmentPackages.RemoveRange(existingPostPkgs);

    // Deep clone Pre packages ke Post
    var prePkgs = await _context.AssessmentPackages
        .Include(p => p.Questions).ThenInclude(q => q.Options)
        .Where(p => p.AssessmentSessionId == preSessionId)
        .OrderBy(p => p.PackageNumber)
        .ToListAsync();

    foreach (var prePkg in prePkgs)
    {
        var newPkg = new AssessmentPackage
        {
            AssessmentSessionId = postSessionId,
            PackageName = prePkg.PackageName,
            PackageNumber = prePkg.PackageNumber
        };
        foreach (var q in prePkg.Questions)
        {
            var newQ = new PackageQuestion
            {
                QuestionText = q.QuestionText,
                Order = q.Order,
                ScoreValue = q.ScoreValue,
                QuestionType = q.QuestionType,
                ElemenTeknis = q.ElemenTeknis,
                Rubrik = q.Rubrik,
                MaxCharacters = q.MaxCharacters,
                Options = q.Options.Select(o => new PackageOption
                {
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
            newPkg.Questions.Add(newQ);
        }
        _context.AssessmentPackages.Add(newPkg);
    }

    await _context.SaveChangesAsync();
}
```

- [ ] **Step 5: Refactor CopyPackagesFromPre to use SyncPackagesToPost**

Replace the body of `CopyPackagesFromPre` (lines 3890-3939) with:

```csharp
int preSessionId = postSession.LinkedSessionId.Value;
await SyncPackagesToPost(preSessionId, postSessionId);

var preCount = await _context.AssessmentPackages.CountAsync(p => p.AssessmentSessionId == preSessionId);
TempData["Success"] = $"Berhasil menyalin {preCount} paket soal dari Pre-Test.";
return RedirectToAction("ManagePackages", new { assessmentId = postSessionId });
```

- [ ] **Step 6: Also add auto-sync hooks to CreateQuestion, EditQuestion, DeleteQuestion, and ImportExcelQuestions**

For each action that modifies questions within a package, add after SaveChangesAsync:

```csharp
// Auto-sync to Post if SamePackage=true
var parentSession = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
if (parentSession?.AssessmentType == "PreTest" && parentSession.LinkedSessionId.HasValue)
{
    var linkedPost = await _context.AssessmentSessions.FindAsync(parentSession.LinkedSessionId.Value);
    if (linkedPost != null && linkedPost.SamePackage)
    {
        await SyncPackagesToPost(parentSession.Id, linkedPost.Id);
    }
}
```

Find these actions by searching for `CreateQuestion`, `EditQuestion`, `DeleteQuestion` POST actions in the controller. Add the sync block after each `SaveChangesAsync()`.

- [ ] **Step 7: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add Controllers/AssessmentAdminController.cs
git commit -m "feat: auto-sync packages Pre→Post when SamePackage=true (poin 8)"
```

---

### Task 5: Lock ManagePackages View for SamePackage Post-Test (Poin 8 - part 4)

**Files:**
- Modify: `Views/Admin/ManagePackages.cshtml`

- [ ] **Step 1: Add lock banner at top of ManagePackages view**

After the TempData alerts block (line 27), add:

```html
@if (ViewBag.IsSamePackageLocked == true)
{
    <div class="alert alert-info d-flex align-items-center mb-3" role="alert">
        <i class="bi bi-lock-fill me-2 fs-5"></i>
        <div>
            <strong>Paket soal disinkronkan dari Pre-Test.</strong>
            Perubahan soal hanya bisa dilakukan di sesi Pre-Test. Paket di halaman ini otomatis terupdate.
            @if (ViewBag.PreSessionId != null)
            {
                <a asp-action="ManagePackages" asp-route-assessmentId="@ViewBag.PreSessionId" class="alert-link ms-2">
                    <i class="bi bi-arrow-right me-1"></i>Buka Pre-Test
                </a>
            }
        </div>
    </div>
}
```

- [ ] **Step 2: Hide create/delete/edit buttons when locked**

Wrap the create package form and delete buttons with a condition. Find the create package button/form and wrap:

```html
@if (ViewBag.IsSamePackageLocked != true)
{
    <!-- existing create package form -->
}
```

Similarly, hide the "Copy dari Pre-Test" button (line 46-49) when locked — it's already synced automatically:

```html
@if (ViewBag.IsSamePackageLocked != true && ViewBag.IsPostSession == true)
{
    <!-- existing copy button block -->
}
```

Hide delete buttons on individual package cards when locked.

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Views/Admin/ManagePackages.cshtml
git commit -m "feat: lock ManagePackages UI when SamePackage=true on Post-Test (poin 8)"
```

---

### Task 6: Reorganize Step 3 Layout + Hide/Show for Pre-Post (Poin 3, 3+, 4, 11, 12)

**Files:**
- Modify: `Views/Admin/CreateAssessment.cshtml` (Step 3 HTML section, lines 335-560)

- [ ] **Step 1: Restructure Step 3 HTML into 4 groups**

Replace the entire Step 3 content (inside `<div id="step-3" class="step-panel d-none">`) with the new grouped layout. The structure:

```html
<div id="step-3" class="step-panel d-none">
    <div class="card shadow-sm border-0">
        <div class="card-body p-4">
            <h5 class="fw-bold mb-4"><i class="bi bi-gear me-2 text-primary"></i>Langkah 3: Settings</h5>

            <!-- ========== GROUP A: Jadwal & Waktu ========== -->
            <div class="card mb-4">
                <div class="card-header bg-light">
                    <h6 class="mb-0"><i class="bi bi-calendar-event me-2"></i>Jadwal & Waktu</h6>
                </div>
                <div class="card-body">
                    <!-- Standard Jadwal Section -->
                    <div id="standard-jadwal-section">
                        <div class="row g-3">
                            <div class="col-md-6">
                                <label class="form-label fw-bold">Tanggal Jadwal <span class="text-danger">*</span></label>
                                <input type="date" id="schedDateInput" name="ScheduleDate" class="form-control" />
                                <div class="invalid-feedback" id="schedDateError">Tanggal jadwal wajib diisi.</div>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label fw-bold">Waktu Jadwal <span class="text-danger">*</span></label>
                                <input type="time" id="schedTimeInput" name="ScheduleTime" class="form-control" value="08:00" />
                                <div class="invalid-feedback" id="schedTimeError">Waktu jadwal wajib diisi.</div>
                            </div>
                            <div class="col-md-6" id="durationFieldWrapper">
                                <label asp-for="DurationMinutes" class="form-label fw-bold">
                                    Durasi (Menit) <span class="text-danger">*</span>
                                </label>
                                <input asp-for="DurationMinutes" type="number" class="form-control" min="1" max="480" />
                                <div class="form-text text-muted">1–480 menit (maks 8 jam)</div>
                                <div class="invalid-feedback">Durasi (menit) wajib diisi.</div>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label fw-bold">
                                    <i class="bi bi-calendar-x text-danger me-1"></i>Tanggal Tutup Ujian <span class="text-danger">*</span>
                                </label>
                                <input type="date" id="ewcdDateInput" class="form-control" />
                                <div class="invalid-feedback" id="ewcdDateError">Tanggal tutup ujian wajib diisi.</div>
                            </div>
                            <div class="col-md-6">
                                <label class="form-label fw-bold">
                                    <i class="bi bi-clock text-danger me-1"></i>Waktu Tutup Ujian <span class="text-danger">*</span>
                                </label>
                                <input type="time" id="ewcdTimeInput" class="form-control" value="23:59" />
                                <div class="invalid-feedback" id="ewcdTimeError">Waktu tutup ujian wajib diisi.</div>
                            </div>
                        </div>
                    </div>

                    <!-- Hidden schedule combiner -->
                    <input type="hidden" asp-for="Schedule" id="schedHidden" />
                    <input type="hidden" asp-for="ExamWindowCloseDate" id="ewcdHidden" />

                    <!-- Pre-Post Test Jadwal Section -->
                    <div id="ppt-jadwal-section" class="collapse">
                        <div class="card mb-3">
                            <div class="card-header bg-light fw-semibold">
                                <i class="bi bi-clock"></i> Jadwal Pre-Test
                            </div>
                            <div class="card-body">
                                <div class="row g-2">
                                    <div class="col-md-4">
                                        <label class="form-label small fw-semibold text-muted">Tanggal &amp; Waktu <span class="text-danger">*</span></label>
                                        <input type="datetime-local" name="PreSchedule" id="preSchedule" class="form-control" />
                                    </div>
                                    <div class="col-md-4">
                                        <label class="form-label small fw-semibold text-muted">Durasi (menit) <span class="text-danger">*</span></label>
                                        <input type="number" name="PreDurationMinutes" id="preDurationMinutes" class="form-control" min="1" max="480" />
                                    </div>
                                    <div class="col-md-4">
                                        <label class="form-label small fw-semibold text-muted">Batas Waktu Pengerjaan <span class="text-danger">*</span></label>
                                        <input type="datetime-local" name="PreExamWindowCloseDate" id="preExamWindowCloseDate" class="form-control" />
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="card mb-3">
                            <div class="card-header bg-light fw-semibold">
                                <i class="bi bi-clock-history"></i> Jadwal Post-Test
                            </div>
                            <div class="card-body">
                                <div class="row g-2">
                                    <div class="col-md-4">
                                        <label class="form-label small fw-semibold text-muted">Tanggal &amp; Waktu <span class="text-danger">*</span></label>
                                        <input type="datetime-local" name="PostSchedule" id="postSchedule" class="form-control" />
                                    </div>
                                    <div class="col-md-4">
                                        <label class="form-label small fw-semibold text-muted">Durasi (menit) <span class="text-danger">*</span></label>
                                        <input type="number" name="PostDurationMinutes" id="postDurationMinutes" class="form-control" min="1" max="480" />
                                    </div>
                                    <div class="col-md-4">
                                        <label class="form-label small fw-semibold text-muted">Batas Waktu Pengerjaan <span class="text-danger">*</span></label>
                                        <input type="datetime-local" name="PostExamWindowCloseDate" id="postExamWindowCloseDate" class="form-control" />
                                    </div>
                                </div>
                                <div class="mt-3">
                                    <div class="form-check">
                                        <input type="checkbox" name="SamePackage" value="true" id="samePackageCheck" class="form-check-input" />
                                        <label class="form-check-label" for="samePackageCheck">Gunakan paket soal yang sama untuk Pre dan Post</label>
                                    </div>
                                    <div id="samePackageBadge" class="mt-2 d-none">
                                        <span class="badge bg-info"><i class="bi bi-info-circle"></i> Paket soal Post-Test akan otomatis disinkronkan dari Pre-Test</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ========== GROUP B: Pengaturan Ujian ========== -->
            <div class="card mb-4">
                <div class="card-header bg-light">
                    <h6 class="mb-0"><i class="bi bi-sliders me-2"></i>Pengaturan Ujian</h6>
                </div>
                <div class="card-body">
                    <div class="row g-3">
                        <!-- Status (hidden when Pre-Post) -->
                        <div class="col-md-6" id="statusFieldWrapper">
                            <label asp-for="Status" class="form-label fw-bold">Status <span class="text-danger">*</span></label>
                            <select asp-for="Status" class="form-select" id="Status">
                                <option value="">-- Pilih Status --</option>
                                <option value="Open">Open</option>
                                <option value="Upcoming">Upcoming</option>
                            </select>
                            <div class="form-text text-muted">Default: Open (tersedia langsung)</div>
                            <div class="invalid-feedback">Status wajib dipilih.</div>
                        </div>

                        <!-- Pass Percentage -->
                        <div class="col-md-6" id="passPercentageWrapper">
                            <label asp-for="PassPercentage" class="form-label fw-bold">
                                Pass Percentage (%) <span class="text-danger">*</span>
                            </label>
                            <input asp-for="PassPercentage" type="number" class="form-control" min="0" max="100" id="PassPercentage" />
                            <div class="form-text text-muted">Nilai minimum untuk lulus (0–100)</div>
                        </div>

                        <!-- Token -->
                        <div class="col-md-6">
                            <label class="form-label fw-bold">
                                <i class="bi bi-shield-lock text-primary me-1"></i>Security Token
                            </label>
                            <div class="form-check form-switch mb-2">
                                <input class="form-check-input" type="checkbox" asp-for="IsTokenRequired" id="IsTokenRequired" />
                                <label class="form-check-label" for="IsTokenRequired">Wajib token untuk memulai ujian</label>
                            </div>
                            <div id="tokenSection" class="d-none">
                                <label asp-for="AccessToken" class="form-label fw-bold">Access Token</label>
                                <div class="input-group">
                                    <input asp-for="AccessToken" class="form-control font-monospace" placeholder="Contoh: A1B2C3" maxlength="6" id="AccessToken" pattern="[A-Za-z0-9]{6}" title="6 karakter alfanumerik" style="text-transform:uppercase;" />
                                    <button class="btn btn-outline-secondary" type="button" onclick="generateToken()">
                                        <i class="bi bi-shuffle"></i> Generate
                                    </button>
                                </div>
                                <span asp-validation-for="AccessToken" class="text-danger"></span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ========== GROUP C: Sertifikat ========== -->
            <div class="card mb-4">
                <div class="card-header bg-light">
                    <h6 class="mb-0"><i class="bi bi-award me-2"></i>Sertifikat</h6>
                </div>
                <div class="card-body">
                    <div class="row g-3">
                        <div class="col-md-6">
                            <label class="form-label fw-bold">Terbitkan Sertifikat</label>
                            <div class="form-check form-switch">
                                <input asp-for="GenerateCertificate" class="form-check-input" type="checkbox" id="GenerateCertificate" />
                                <label class="form-check-label" for="GenerateCertificate">Aktifkan</label>
                            </div>
                            <div class="form-text text-muted">Peserta yang lulus dapat melihat dan mencetak sertifikat</div>
                            <div id="preTestWarning" class="alert alert-warning py-2 px-3 mt-2 d-none" role="alert">
                                <i class="bi bi-exclamation-triangle-fill me-1"></i>
                                Judul mengandung "Pre Test". Pre Test biasanya tidak menerbitkan sertifikat.
                            </div>
                            <div id="prePostCertNote" class="form-text text-info d-none">
                                <i class="bi bi-info-circle me-1"></i>Sertifikat hanya diterbitkan untuk peserta yang lulus Post-Test
                            </div>
                        </div>

                        <div class="col-md-6">
                            @if (ViewBag.RenewalValidUntilWarning != null)
                            {
                                <div class="alert alert-warning py-2 mb-2">
                                    <i class="bi bi-exclamation-triangle me-1"></i>@ViewBag.RenewalValidUntilWarning
                                </div>
                            }
                            <label asp-for="ValidUntil" class="form-label fw-bold">
                                Tanggal Expired Sertifikat
                                @if (ViewBag.IsRenewalMode == true)
                                {
                                    <span class="text-danger">*</span>
                                }
                                else
                                {
                                    <span class="text-muted">(opsional)</span>
                                }
                            </label>
                            <input asp-for="ValidUntil" type="date" class="form-control" id="ValidUntil"
                                   min="@DateTime.Today.ToString("yyyy-MM-dd")" />
                            <div class="form-text">Kosongkan jika sertifikat tidak memiliki batas waktu.</div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ========== GROUP D: Opsi Lainnya ========== -->
            <div class="card mb-4">
                <div class="card-header bg-light">
                    <h6 class="mb-0"><i class="bi bi-toggles me-2"></i>Opsi Lainnya</h6>
                </div>
                <div class="card-body">
                    <div class="col-md-6">
                        <label class="form-label fw-bold">Izinkan Review Jawaban</label>
                        <div class="form-check form-switch">
                            <input asp-for="AllowAnswerReview" class="form-check-input" type="checkbox" id="AllowAnswerReview" />
                            <label class="form-check-label" for="AllowAnswerReview">Aktifkan review jawaban setelah selesai</label>
                        </div>
                    </div>
                </div>
            </div>

            <!-- placeholder for old position (removed) -->
            <div id="durationFieldWrapperPlaceholder" style="display:none;"></div>

            <!-- Navigation -->
            <div class="d-flex justify-content-between mt-4">
                <button type="button" class="btn btn-outline-secondary btn-prev" id="btnPrev3">
                    <i class="bi bi-arrow-left me-1"></i>Sebelumnya
                </button>
                <div class="d-flex flex-column align-items-end gap-2">
                    <button type="button" class="btn btn-primary btn-next" id="btnNext3">
                        Selanjutnya <i class="bi bi-arrow-right ms-1"></i>
                    </button>
                    <button type="button" class="btn btn-outline-primary btnBackToConfirm d-none" id="btnBackToConfirm-3">
                        <i class="bi bi-arrow-left me-1"></i>Kembali ke Konfirmasi
                    </button>
                </div>
            </div>
        </div>
    </div>
</div>
```

- [ ] **Step 2: Build to verify Razor compiles**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Views/Admin/CreateAssessment.cshtml
git commit -m "refactor: reorganize Step 3 into 4 groups + move EWCD to Jadwal group (poin 3, 3+)"
```

---

### Task 7: Update JavaScript Toggle/Validation for Pre-Post Mode (Poin 3, 4, 11, 12)

**Files:**
- Modify: `Views/Admin/CreateAssessment.cshtml` (Script section)

- [ ] **Step 1: Update Pre-Post toggle to hide Status, EWCD, and show cert note**

Find the Pre-Post toggle handler (line ~1584):
```javascript
typeSelect.addEventListener('change', function() {
    if (this.value === 'PrePostTest') {
        pptSection.classList.add('show');
        if (stdSection) stdSection.classList.add('d-none');
    } else {
        pptSection.classList.remove('show');
        if (stdSection) stdSection.classList.remove('d-none');
    }
});
```

Replace with:
```javascript
typeSelect.addEventListener('change', function() {
    var statusWrapper = document.getElementById('statusFieldWrapper');
    var certNote = document.getElementById('prePostCertNote');

    if (this.value === 'PrePostTest') {
        pptSection.classList.add('show');
        if (stdSection) stdSection.classList.add('d-none');
        // Poin 4: hide Status dropdown
        if (statusWrapper) statusWrapper.classList.add('d-none');
        // Poin 12: show cert note
        if (certNote) certNote.classList.remove('d-none');
    } else {
        pptSection.classList.remove('show');
        if (stdSection) stdSection.classList.remove('d-none');
        if (statusWrapper) statusWrapper.classList.remove('d-none');
        if (certNote) certNote.classList.add('d-none');
    }
});
```

- [ ] **Step 2: Add Proton guard to disable Pre-Post when Proton selected (Poin 11)**

In the category change handler (inside WizardController, after `applyProtonMode(isProton)`), add:

```javascript
// Poin 11: Proton tidak support Pre-Post Test
var typeSelect = document.getElementById('assessmentTypeInput');
if (typeSelect) {
    if (isProton) {
        typeSelect.value = 'Standard';
        typeSelect.disabled = true;
        typeSelect.title = 'Assessment Proton hanya mendukung tipe Standard';
        // Trigger change to reset UI
        typeSelect.dispatchEvent(new Event('change'));
    } else {
        typeSelect.disabled = false;
        typeSelect.title = '';
    }
}
```

Also add on page load (after the existing `if (categorySelect.value === 'Assessment Proton')` block):

```javascript
// Init: disable type select if Proton already selected
if (categorySelect && categorySelect.value === 'Assessment Proton') {
    var ts = document.getElementById('assessmentTypeInput');
    if (ts) { ts.value = 'Standard'; ts.disabled = true; ts.title = 'Assessment Proton hanya mendukung tipe Standard'; }
}
```

- [ ] **Step 3: Update Step 3 validation to skip Status when Pre-Post**

In `validateStep(n)` for `n === 3`, the Status validation block:
```javascript
var status = document.getElementById('Status');
if (!status || !status.value) {
    if (status) status.classList.add('is-invalid');
    valid = false;
}
```

For the Pre-Post branch, this already exists (line 904-908). Update it to skip when Status wrapper is hidden:

```javascript
// Status still required (only if visible)
var statusWrapper = document.getElementById('statusFieldWrapper');
if (!statusWrapper || !statusWrapper.classList.contains('d-none')) {
    var status = document.getElementById('Status');
    if (!status || !status.value) {
        if (status) status.classList.add('is-invalid');
        valid = false;
    }
}
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Views/Admin/CreateAssessment.cshtml
git commit -m "feat: hide Status/EWCD in Pre-Post, Proton guard, cert note (poin 3, 4, 11, 12)"
```

---

### Task 8: Update Summary Step 4 (Poin 5, 6)

**Files:**
- Modify: `Views/Admin/CreateAssessment.cshtml` (Step 4 HTML + populateSummary JS)

- [ ] **Step 1: Add tipe badge to summary card Kategori & Judul**

In Step 4 HTML, after `<p class="mb-0"><strong>Judul:</strong> <span id="summary-title"></span></p>` (line 579), add:

```html
<p class="mb-0"><strong>Tipe:</strong> <span id="summary-type"></span></p>
```

- [ ] **Step 2: Replace Settings summary with conditional layout**

Replace the Settings card body (lines 608-616) with:

```html
<div class="card-body">
    <!-- Standard mode summary -->
    <div id="summary-standard-settings">
        <dl class="row mb-0">
            <dt class="col-sm-4">Jadwal</dt><dd class="col-sm-8" id="summary-schedule"></dd>
            <dt class="col-sm-4">Durasi</dt><dd class="col-sm-8" id="summary-duration"></dd>
            <dt class="col-sm-4">Status</dt><dd class="col-sm-8" id="summary-status"></dd>
            <dt class="col-sm-4">Tutup Ujian</dt><dd class="col-sm-8" id="summary-ewcd"></dd>
            <dt class="col-sm-4">Token</dt><dd class="col-sm-8" id="summary-token"></dd>
            <dt class="col-sm-4">Pass Percentage</dt><dd class="col-sm-8" id="summary-pass"></dd>
            <dt class="col-sm-4">Valid Until</dt><dd class="col-sm-8" id="summary-validuntil"></dd>
        </dl>
    </div>

    <!-- Pre-Post mode summary -->
    <div id="summary-ppt-settings" class="d-none">
        <div class="row g-3 mb-3">
            <div class="col-md-6">
                <div class="card border-primary">
                    <div class="card-header bg-primary bg-opacity-10 py-2">
                        <strong><i class="bi bi-clock me-1"></i>Pre-Test</strong>
                    </div>
                    <div class="card-body py-2">
                        <small><strong>Jadwal:</strong> <span id="summary-pre-schedule"></span></small><br/>
                        <small><strong>Durasi:</strong> <span id="summary-pre-duration"></span></small><br/>
                        <small><strong>Batas Waktu:</strong> <span id="summary-pre-ewcd"></span></small>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="card border-success">
                    <div class="card-header bg-success bg-opacity-10 py-2">
                        <strong><i class="bi bi-clock-history me-1"></i>Post-Test</strong>
                    </div>
                    <div class="card-body py-2">
                        <small><strong>Jadwal:</strong> <span id="summary-post-schedule"></span></small><br/>
                        <small><strong>Durasi:</strong> <span id="summary-post-duration"></span></small><br/>
                        <small><strong>Batas Waktu:</strong> <span id="summary-post-ewcd"></span></small>
                    </div>
                </div>
            </div>
        </div>
        <dl class="row mb-0">
            <dt class="col-sm-4">Status</dt><dd class="col-sm-8">Upcoming</dd>
            <dt class="col-sm-4">Token</dt><dd class="col-sm-8" id="summary-ppt-token"></dd>
            <dt class="col-sm-4">Pass Percentage</dt><dd class="col-sm-8" id="summary-ppt-pass"></dd>
            <dt class="col-sm-4">Paket Soal Sama</dt><dd class="col-sm-8" id="summary-ppt-samepackage"></dd>
            <dt class="col-sm-4">Valid Until</dt><dd class="col-sm-8" id="summary-ppt-validuntil"></dd>
        </dl>
    </div>
</div>
```

- [ ] **Step 3: Update populateSummary() JavaScript**

Replace the `populateSummary()` function with updated version that handles both modes:

In the `populateSummary()` function, add at the top (after existing Category/Title population):

```javascript
// Tipe badge (poin 5)
var typeSelect = document.getElementById('assessmentTypeInput');
var isPrePost = typeSelect && typeSelect.value === 'PrePostTest';
var summType = document.getElementById('summary-type');
if (summType) {
    if (isPrePost) {
        summType.innerHTML = '<span class="badge bg-info">Pre-Post Test</span>';
    } else {
        summType.innerHTML = '<span class="badge bg-secondary">Standard</span>';
    }
}
```

Replace the Settings population section with:

```javascript
// Toggle summary sections
var stdSettings = document.getElementById('summary-standard-settings');
var pptSettings = document.getElementById('summary-ppt-settings');

if (isPrePost) {
    if (stdSettings) stdSettings.classList.add('d-none');
    if (pptSettings) pptSettings.classList.remove('d-none');

    // Pre-Post mini-cards
    var preSched = document.getElementById('preSchedule');
    var postSched = document.getElementById('postSchedule');
    var preDur = document.getElementById('preDurationMinutes');
    var postDur = document.getElementById('postDurationMinutes');
    var preEwcd = document.getElementById('preExamWindowCloseDate');
    var postEwcd = document.getElementById('postExamWindowCloseDate');

    var el = document.getElementById('summary-pre-schedule');
    if (el) el.textContent = preSched && preSched.value ? preSched.value.replace('T', ' ') : '-';
    el = document.getElementById('summary-pre-duration');
    if (el) el.textContent = preDur && preDur.value ? preDur.value + ' menit' : '-';
    el = document.getElementById('summary-pre-ewcd');
    if (el) el.textContent = preEwcd && preEwcd.value ? preEwcd.value.replace('T', ' ') : '-';

    el = document.getElementById('summary-post-schedule');
    if (el) el.textContent = postSched && postSched.value ? postSched.value.replace('T', ' ') : '-';
    el = document.getElementById('summary-post-duration');
    if (el) el.textContent = postDur && postDur.value ? postDur.value + ' menit' : '-';
    el = document.getElementById('summary-post-ewcd');
    if (el) el.textContent = postEwcd && postEwcd.value ? postEwcd.value.replace('T', ' ') : '-';

    // Shared fields
    var tokenEl = document.getElementById('IsTokenRequired');
    var accessTokenEl = document.getElementById('AccessToken');
    el = document.getElementById('summary-ppt-token');
    if (el) el.textContent = (tokenEl && tokenEl.checked) ? 'Ya — ' + (accessTokenEl ? accessTokenEl.value : '') : 'Tidak';

    var passEl = document.getElementById('PassPercentage');
    el = document.getElementById('summary-ppt-pass');
    if (el) el.textContent = (passEl && passEl.value) ? passEl.value + '%' : '-';

    var samePackageEl = document.getElementById('samePackageCheck');
    el = document.getElementById('summary-ppt-samepackage');
    if (el) el.textContent = (samePackageEl && samePackageEl.checked) ? 'Ya' : 'Tidak';

    var validUntilEl = document.getElementById('ValidUntil');
    el = document.getElementById('summary-ppt-validuntil');
    if (el) el.textContent = (validUntilEl && validUntilEl.value) ? validUntilEl.value : 'Tidak ada';
} else {
    if (stdSettings) stdSettings.classList.remove('d-none');
    if (pptSettings) pptSettings.classList.add('d-none');

    // Standard mode — keep existing logic
    var schedDate = document.getElementById('schedDateInput');
    var schedTime = document.getElementById('schedTimeInput');
    var summSchedule = document.getElementById('summary-schedule');
    var schedVal = (schedDate && schedDate.value) ? schedDate.value : '';
    var timeVal = (schedTime && schedTime.value) ? schedTime.value : '08:00';
    if (summSchedule) summSchedule.textContent = schedVal ? (schedVal + ' ' + timeVal + ' WIB') : '-';

    var durEl = document.getElementById('DurationMinutes');
    var summDur = document.getElementById('summary-duration');
    if (summDur) summDur.textContent = (durEl && durEl.value) ? (durEl.value + ' menit') : '-';

    var statusEl = document.getElementById('Status');
    var summStatus = document.getElementById('summary-status');
    if (summStatus) summStatus.textContent = statusEl ? statusEl.value : '-';

    var ewcdDateEl = document.getElementById('ewcdDateInput');
    var ewcdTimeEl = document.getElementById('ewcdTimeInput');
    var summEwcd = document.getElementById('summary-ewcd');
    if (summEwcd) summEwcd.textContent = (ewcdDateEl && ewcdDateEl.value) ? (ewcdDateEl.value + ' ' + (ewcdTimeEl ? ewcdTimeEl.value : '23:59')) : '-';

    var tokenEl = document.getElementById('IsTokenRequired');
    var accessTokenEl = document.getElementById('AccessToken');
    var summToken = document.getElementById('summary-token');
    if (summToken) summToken.textContent = (tokenEl && tokenEl.checked) ? 'Ya — ' + (accessTokenEl ? accessTokenEl.value : '') : 'Tidak';

    var passEl = document.getElementById('PassPercentage');
    var summPass = document.getElementById('summary-pass');
    if (summPass) summPass.textContent = (passEl && passEl.value) ? (passEl.value + '%') : '-';

    var validUntilEl = document.getElementById('ValidUntil');
    var summValidUntil = document.getElementById('summary-validuntil');
    if (summValidUntil) summValidUntil.textContent = (validUntilEl && validUntilEl.value) ? validUntilEl.value : 'Tidak ada';
}
```

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Views/Admin/CreateAssessment.cshtml
git commit -m "feat: update summary Step 4 with tipe badge + Pre-Post mini-cards (poin 5, 6)"
```

---

### Task 9: Update Success Modal for Pre-Post (Poin 9)

**Files:**
- Modify: `Views/Admin/CreateAssessment.cshtml` (modal HTML + JS)

- [ ] **Step 1: Add Pre-Post rows to modal table**

In the success modal `<tbody>` (line 650), add conditional rows. Replace the static Schedule/Duration rows with id-based rows that JS will populate:

```html
<tr>
    <td class="fw-semibold text-muted" style="width:140px;">Title</td>
    <td id="modal-title"></td>
</tr>
<tr>
    <td class="fw-semibold text-muted">Category</td>
    <td id="modal-category"></td>
</tr>
<tr id="modal-row-schedule">
    <td class="fw-semibold text-muted">Schedule</td>
    <td id="modal-schedule"></td>
</tr>
<tr id="modal-row-ppt-schedule" class="d-none">
    <td class="fw-semibold text-muted">Pre-Test</td>
    <td id="modal-pre-schedule"></td>
</tr>
<tr id="modal-row-ppt-post-schedule" class="d-none">
    <td class="fw-semibold text-muted">Post-Test</td>
    <td id="modal-post-schedule"></td>
</tr>
<tr id="modal-row-duration">
    <td class="fw-semibold text-muted">Duration</td>
    <td id="modal-duration"></td>
</tr>
<tr id="modal-row-ppt-duration" class="d-none">
    <td class="fw-semibold text-muted">Duration</td>
    <td id="modal-ppt-duration"></td>
</tr>
<tr>
    <td class="fw-semibold text-muted">Status</td>
    <td id="modal-status"></td>
</tr>
<tr>
    <td class="fw-semibold text-muted">Token</td>
    <td id="modal-token"></td>
</tr>
```

- [ ] **Step 2: Update user table header for Pre-Post**

Replace the table header (line 681):
```html
<thead class="table-light">
    <tr><th>#</th><th>Name</th><th>Email</th><th id="modal-action-header">Action</th></tr>
</thead>
```

- [ ] **Step 3: Update the success modal JS to handle Pre-Post data**

In the auto-show success modal script, after `var a = JSON.parse(createdData);`, add Pre-Post detection and branching:

```javascript
var isPrePost = a.IsPrePostTest === true;

if (isPrePost) {
    document.getElementById('modal-header-text').textContent = a.Count + ' Pre-Post assessment(s) created successfully';

    // Show PPT rows, hide standard rows
    document.getElementById('modal-row-schedule').classList.add('d-none');
    document.getElementById('modal-row-duration').classList.add('d-none');
    document.getElementById('modal-row-ppt-schedule').classList.remove('d-none');
    document.getElementById('modal-row-ppt-post-schedule').classList.remove('d-none');
    document.getElementById('modal-row-ppt-duration').classList.remove('d-none');

    document.getElementById('modal-pre-schedule').textContent = a.PreSchedule;
    document.getElementById('modal-post-schedule').textContent = a.PostSchedule;
    document.getElementById('modal-ppt-duration').textContent = 'Pre: ' + a.PreDurationMinutes + ' min | Post: ' + a.PostDurationMinutes + ' min';
} else {
    document.getElementById('modal-header-text').textContent = a.Count + ' assessment(s) created successfully';
    document.getElementById('modal-schedule').textContent = a.Schedule;
    document.getElementById('modal-duration').textContent = a.DurationMinutes + ' minutes';
}
```

Update the user table row generation to handle Pre-Post sessions with two links:

```javascript
if (a.Sessions && a.Sessions.length > 0) {
    a.Sessions.forEach(function(s, idx) {
        var tr = document.createElement('tr');
        var tdNum = document.createElement('td'); tdNum.textContent = idx + 1; tr.appendChild(tdNum);
        var tdName = document.createElement('td'); tdName.textContent = s.UserName; tr.appendChild(tdName);
        var tdEmail = document.createElement('td'); tdEmail.className = 'text-muted'; tdEmail.textContent = s.UserEmail; tr.appendChild(tdEmail);
        var tdAction = document.createElement('td');

        if (isPrePost) {
            // Two links: Manage Pre + Manage Post
            var preLink = document.createElement('a');
            preLink.href = basePath + '/Admin/ManagePackages?assessmentId=' + s.PreId;
            preLink.className = 'btn btn-sm btn-outline-primary me-1';
            preLink.textContent = 'Pre';
            tdAction.appendChild(preLink);

            if (a.SamePackage) {
                var postBadge = document.createElement('span');
                postBadge.className = 'badge bg-info';
                postBadge.textContent = 'Sync dari Pre';
                tdAction.appendChild(postBadge);
            } else {
                var postLink = document.createElement('a');
                postLink.href = basePath + '/Admin/ManagePackages?assessmentId=' + s.PostId;
                postLink.className = 'btn btn-sm btn-outline-success';
                postLink.textContent = 'Post';
                tdAction.appendChild(postLink);
            }
        } else {
            var link = document.createElement('a');
            link.href = basePath + '/Admin/ManagePackages?assessmentId=' + s.Id;
            link.className = 'btn btn-sm btn-outline-primary';
            link.textContent = 'Manage Packages';
            tdAction.appendChild(link);
        }

        tr.appendChild(tdAction);
        tbody.appendChild(tr);
    });

    // Update main manage button
    var manageBtn = document.getElementById('modal-manage-btn');
    if (isPrePost) {
        var firstPreId = a.Sessions[0].PreId;
        manageBtn.href = basePath + '/Admin/ManagePackages?assessmentId=' + firstPreId;
        manageBtn.innerHTML = '<i class="bi bi-list-check me-1"></i> Manage Pre-Test Packages';
    } else {
        var firstId = a.Sessions[0].Id;
        manageBtn.href = basePath + '/Admin/ManagePackages?assessmentId=' + firstId;
        if (a.Sessions.length > 1) {
            manageBtn.innerHTML = '<i class="bi bi-list-check me-1"></i> Manage Packages (First)';
        }
    }
}
```

Note: Also update the existing link pattern from `ManageQuestions` to `ManagePackages` — the old `ManageQuestions` action was removed in Phase 227.

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Views/Admin/CreateAssessment.cshtml
git commit -m "feat: update success modal for Pre-Post Test sessions (poin 9)"
```

---

### Task 10: Fix "Create Another" Reset (Poin 10)

**Files:**
- Modify: `Views/Admin/CreateAssessment.cshtml` (Create Another button handler)

- [ ] **Step 1: Update Create Another handler**

In the `modal-create-another-btn` click handler (line ~1523), add after the existing reset logic:

```javascript
// Poin 10: reset assessment type to Standard
var typeSelect = document.getElementById('assessmentTypeInput');
if (typeSelect) {
    typeSelect.value = 'Standard';
    typeSelect.dispatchEvent(new Event('change'));
}

// Re-enable type select (may have been disabled by Proton guard)
if (typeSelect) {
    typeSelect.disabled = false;
    typeSelect.title = '';
}

// Reset category
var catSelect = document.getElementById('Category');
if (catSelect) {
    catSelect.value = '';
    catSelect.dispatchEvent(new Event('change'));
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add Views/Admin/CreateAssessment.cshtml
git commit -m "fix: reset assessment type and category on Create Another (poin 10)"
```

---

### Task 11: Manual Smoke Test

- [ ] **Step 1: Start the application**

Run: `dotnet run`

- [ ] **Step 2: Test Standard flow**

Navigate to `http://localhost:5277/Admin/CreateAssessment`
1. Select any non-Proton category
2. Verify Step 3 has 4 grouped cards: Jadwal, Pengaturan Ujian, Sertifikat, Opsi Lainnya
3. Verify EWCD is inside Jadwal group
4. Fill all fields, select user, submit
5. Verify success modal shows with correct data and "Manage Packages" link (not ManageQuestions)

- [ ] **Step 3: Test Pre-Post Test flow**

1. Select "Pre-Post Test" from tipe dropdown
2. Verify: Standard jadwal section hidden, Pre-Post section shown
3. Verify: Status dropdown hidden
4. Verify: EWCD standalone fields hidden
5. Verify: "Sertifikat hanya diterbitkan untuk peserta yang lulus Post-Test" note visible
6. Fill Pre and Post jadwal, check SamePackage
7. Go to Step 4: verify tipe badge shows "Pre-Post Test", two mini-cards for Pre/Post
8. Submit and verify success modal with Pre/Post links
9. Click "Create Another" — verify form resets to Standard

- [ ] **Step 4: Test Proton guard**

1. Select "Assessment Proton" as category
2. Verify tipe dropdown is disabled and set to "Standard"
3. Change category to something else — verify dropdown re-enabled

- [ ] **Step 5: Test SamePackage lock on ManagePackages**

1. Create a Pre-Post Test with SamePackage checked
2. Navigate to Post-Test ManagePackages
3. Verify lock banner shows and create/delete buttons hidden
4. Navigate to Pre-Test ManagePackages, create a package
5. Go back to Post-Test ManagePackages — verify package was auto-synced

- [ ] **Step 6: Commit final adjustments if any**

```bash
git add -A
git commit -m "fix: smoke test adjustments"
```
