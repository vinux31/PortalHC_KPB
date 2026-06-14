# Phase 353: Admin Backend Gambar (CRUD + Sync + Atomic Delete) - Pattern Map

**Mapped:** 2026-06-08
**Files analyzed:** 7 modify-targets (brownfield, all in-place edits) + 2 new test files
**Analogs found:** 7 / 9 (2 compose-from-primitives — flagged below)

> Brownfield phase: NO new files except tests. Every controller/view item is an in-place edit to existing code. "Analog" here means: the existing live pattern in *this same repo* the executor copies verbatim then extends. Line numbers are CURRENT (read 2026-06-08); they will shift as edits are applied — match by symbol name, not number.

---

## File Classification

| File (modify unless noted) | Role | Data Flow | Closest Analog | Match Quality |
|----------------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` → CreateQuestion POST (L6067) | controller | file-I/O + CRUD (request-response) | TrainingAdminController AddTraining cert flow (L205→L307) | role+flow match |
| `Controllers/AssessmentAdminController.cs` → EditQuestion GET JSON (L6214) | controller | request-response (read) | same method, existing JSON shape (L6214-6230) | exact (self) |
| `Controllers/AssessmentAdminController.cs` → EditQuestion POST (L6241) | controller | file-I/O + CRUD | TrainingAdminController EditTraining (L475→L524) + self (L6302 RemoveRange) | role match — **option-image preserve = NO direct analog (OQ1)** |
| `Controllers/AssessmentAdminController.cs` → DeleteQuestion POST (L6377) | controller | file-I/O delete (tx) | CDPController.DeleteCoachingSession (L2433-2567, Phase 333) | role+flow match — **+ ref-count = compose** |
| `Controllers/AssessmentAdminController.cs` → DeletePackage POST (L5457) | controller | file-I/O delete (cascade) | CDPController.DeleteCoachingSession (Phase 333) + self DeletePackage cascade (L5468-5487) | role match — **+ ref-count = compose (D-11)** |
| `Controllers/AssessmentAdminController.cs` → SyncPackagesToPost (L5337) | service (private) | transform (deep-clone, DB-only) | self, the deep-clone block (L5370-5384) | exact (self) — 2-line add |
| `Views/Admin/ManagePackageQuestions.cshtml` → form + fields + JS (L122, L150-163, L399) | view | request-response (multipart) | self form (L122) + ImportPackageQuestions.cshtml enctype (L76) + self populateEditForm (L399) | role match |
| `Views/Admin/_PreviewQuestion.cshtml` → add `<img>` (after L17, in loop L60) | component (partial) | request-response (render) | self render structure (L17, L54-61) | exact (self) — additive |
| `HcPortal.Tests/QuestionImageSyncTests.cs` (**NEW**) + `QuestionImageRefCountTests.cs` (**NEW**) | test | — | FileUploadHelperTests.cs (MakeFile L19, MakeTempDir L41) | role match — **+ EF InMemory DbContext = compose** |

---

## Shared Patterns

> These cross-cut multiple edit points. Executor applies the SAME shape at every applicable site.

### SHARED-1: Atomic file delete (Phase 333 gold pattern) + ref-count (D-10) — INTI phase

**Source:** `Controllers/CDPController.cs:2456-2563` (DeleteCoachingSession).
**Apply to:** DeleteQuestion (C-04), EditQuestion replace (C-04), DeletePackage (D-11).

The Phase 333 skeleton — copy verbatim, then insert the ref-count guard before each `File.Delete`:

```csharp
// 1. OUTER-scope declaration (CDPController.cs:2457) — visible after CommitAsync
List<string>? pathsToDelete = null;

await using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // ... build pathsToDelete INSIDE tx (collection only, NO File.Delete) ...
    // ... RemoveRange / set ImagePath=null ...
    await _context.SaveChangesAsync();
    // ... audit log ...
    await tx.CommitAsync();
}
catch (DbUpdateException dbEx)            // CDPController.cs:2532 — disposal auto-rollback, NO explicit RollbackAsync
{
    _logger.LogWarning(dbEx, "...");
    TempData["Error"] = "...";
    return RedirectToAction(...);
}
catch (Exception ex)                      // CDPController.cs:2539 — generic fallback, friendly TempData, NO throw
{
    _logger.LogError(ex, "...");
    TempData["Error"] = "...";
    return RedirectToAction(...);
}

// 2. File.Delete loop POST CommitAsync — inner try/catch warn-only PER FILE (CDPController.cs:2547-2563)
if (pathsToDelete != null && pathsToDelete.Count > 0)
{
    foreach (var relUrl in pathsToDelete)
    {
        // ↓↓↓ D-10 REF-COUNT INSERT (NEW — not in 333; see SHARED-2) ↓↓↓
        try
        {
            var physical = Path.Combine(_env.WebRootPath,
                relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
        }
        catch (Exception fex)
        {
            _logger.LogWarning(fex, "File.Delete post-commit failed (question image): {Path}", relUrl);
        }
    }
}
```

Key invariants from 333 (preserve verbatim): outer-scope `List<string>?`, build-inside-tx, delete-after-commit, `await using var tx` (auto-rollback on dispose — no explicit `RollbackAsync`), inner try/catch warn-only per file, `Path.Combine(_env.WebRootPath, relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))` for physical path.

### SHARED-2: Reference-count guard (D-10) — NO ANALOG, compose from primitive

**Source:** none in repo (genuinely new — RESEARCH §233 "only genuinely new logic"). Closest primitive = `AnyAsync` queries used throughout (e.g. CDPController.cs:2469 `_context.CoachingSessions.AnyAsync(...)`).
**Apply to:** inside the `foreach (var relUrl in pathsToDelete)` loop of SHARED-1, BEFORE `File.Delete`, AFTER `CommitAsync`.

```csharp
// RESEARCH Pattern 2 (line 141-156). Runs AFTER DB final so deleted/nulled rows are NOT counted.
bool stillUsedQ = await _context.PackageQuestions.AnyAsync(q => q.ImagePath == relUrl);
bool stillUsedO = await _context.PackageOptions.AnyAsync(o => o.ImagePath == relUrl);
if (stillUsedQ || stillUsedO) continue;   // shared file still referenced by Post/other → SKIP physical delete
// else fall through to File.Delete (SHARED-1 try/catch)
```

CRITICAL ordering (Pitfall 2 + OQ2): ref-count MUST run after `SaveChangesAsync`/`CommitAsync` AND after auto-sync (`SyncPackagesToPost`), otherwise the just-deleted/just-synced rows are still counted and the file is never cleaned (orphan-permanent) OR a shared file gets wrongly deleted. Sequence: SaveChanges → CommitAsync → auto-sync → ref-count → File.Delete.

### SHARED-3: Validate-before-save upload gate (C-01/C-02, D-08)

**Source:** `Controllers/TrainingAdminController.cs:205-207` (validate) + `:307-311` (save).
**Apply to:** CreateQuestion POST, EditQuestion POST — once per uploaded file, validate ALL before any SaveFileAsync (fail-fast).

```csharp
// Validate (TrainingAdminController.cs:205) — Phase 353 swaps ValidateCertificateFile → ValidateImageFile (C-01)
var (imgValid, imgErr) = FileUploadHelper.ValidateImageFile(questionImage);
if (!imgValid)
{
    TempData["Error"] = imgErr;   // D-08: TempData error + redirect (NOT inline). Surface helper message verbatim.
    return RedirectToAction("ManagePackageQuestions", new { packageId });
}
// ... repeat for optionAImage..D ...

// Save (TrainingAdminController.cs:307) — C-02 subFolder = $"uploads/questions/{packageId}"
var savedUrl = await FileUploadHelper.SaveFileAsync(
    questionImage, _env.WebRootPath, $"uploads/questions/{packageId}", _logger);
if (savedUrl != null) { q.ImagePath = savedUrl; q.ImageAlt = Truncate(questionImageAlt, 255); }
```

`ValidateImageFile`/`SaveFileAsync` signatures: `Helpers/FileUploadHelper.cs:45` and `:75`. `SaveFileAsync` returns null for null/empty file (safe no-op). `_env.WebRootPath` is `protected readonly` on `AdminBaseController` (inherited by AssessmentAdminController). **Pitfall 6:** ImageAlt is `[MaxLength(255)]` (AssessmentPackage.cs:63/92) — truncate alt to 255 before assign.

### SHARED-4: Per-POST authorization + anti-forgery (existing, do not remove)

**Source:** every POST in this controller, e.g. CreateQuestion `Controllers/AssessmentAdminController.cs:6064-6066`.

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```

All Phase 353 edits keep these attributes unchanged. View keeps `@Html.AntiForgeryToken()` (ManagePackageQuestions.cshtml:123). Security controls (magic-byte, path-traversal strip) live inside the helpers — no new security code needed (RESEARCH §Security Domain).

---

## Pattern Assignments

### CreateQuestion POST (controller, file-I/O + CRUD) — `AssessmentAdminController.cs:6067`

**Analogs:** signature self (L6067-6076) + TrainingAdminController validate/save (L205/L307) + SyncPackagesToPost call self (L6177-6189).

**Signature extension** (mirror existing flat-param style, NOT a DTO — RESEARCH Pattern 1, D-67 discretion → flat recommended):
```csharp
// Add to the existing param list (after correctA..D, L6076):
IFormFile? questionImage, string? questionImageAlt,
IFormFile? optionAImage, IFormFile? optionBImage, IFormFile? optionCImage, IFormFile? optionDImage,
string? optionAImageAlt, string? optionBImageAlt, string? optionCImageAlt, string? optionDImageAlt
```
Razor `name=` must match param name case-insensitively. Binding only works with `enctype="multipart/form-data"` (D-02 / Pitfall 1).

**Flow:** apply SHARED-3 (validate-all → save-each) AFTER existing type/score validation (L6079-6114), set `newQ.ImagePath/ImageAlt` and per-option `ImagePath/ImageAlt` while building `newQ.Options` (L6139-6145), then existing `SaveChangesAsync` (L6149) → existing auto-sync block (L6177-6189). No tx needed here (create only, no delete). No ref-count (nothing deleted).

**Per-option binding note:** options built from `(text, isCorrect)` tuple loop (L6132-6145). To attach images, zip a parallel array of `(IFormFile? img, string? alt)` indexed A-D so each created `PackageOption` gets its image.

---

### EditQuestion GET JSON (controller, read) — `AssessmentAdminController.cs:6214`

**Analog:** the existing `Json(new {...})` block, lines 6214-6230 (exact self).

**Extension (D-06)** — add 2 fields to question object + 2 to each option:
```csharp
return Json(new {
    id = q.Id, order = q.Order, questionText = q.QuestionText,
    questionType = q.QuestionType ?? "MultipleChoice", scoreValue = q.ScoreValue,
    affectedSessions, elemenTeknis = q.ElemenTeknis, rubrik = q.Rubrik, maxCharacters = q.MaxCharacters,
    imagePath = q.ImagePath, imageAlt = q.ImageAlt,                       // ← ADD (D-06)
    options = q.Options.OrderBy(o => o.Id).Select(o => new {
        optionText = o.OptionText, isCorrect = o.IsCorrect,
        imagePath = o.ImagePath, imageAlt = o.ImageAlt                    // ← ADD (D-06)
    }).ToList()
});
```
Entity already Includes Options (L6199). `populateEditForm` JS (view L399) consumes this — see view assignment.

---

### EditQuestion POST (controller, file-I/O + CRUD) — `AssessmentAdminController.cs:6241`

**Analogs:** TrainingAdminController EditTraining replace-file (L475/L524) + SHARED-1/2/3. **Option-image preserve = NO DIRECT ANALOG — compose (OQ1, A3 HIGH risk).**

**Signature:** same additions as CreateQuestion PLUS edit-only checkboxes:
```csharp
bool removeQuestionImage,
bool removeOptionAImage, bool removeOptionBImage, bool removeOptionCImage, bool removeOptionDImage
```
(checkbox unchecked → `false` automatically.)

**D-05 conflict resolution per item** (RESEARCH Code Examples L301-314, Pitfall 3) — file-baru-menang:
```
if (newFile != null)        → SaveFileAsync; add OLD path to pathsToDelete; set new path+alt; IGNORE checkbox
else if (removeChecked)     → add OLD path to pathsToDelete; set ImagePath=null, ImageAlt=null
else                        → keep ImagePath; alt MAY update (truncate 255)
```

**🚩 NO-ANALOG / COMPOSE — option image preservation (OQ1, RESEARCH A3 HIGH):**
Current EditQuestion does `_context.PackageOptions.RemoveRange(q.Options); q.Options.Clear();` (L6302-6303) then re-creates `PackageOption` from `optionA..D` (L6314-6320). The re-created options carry NO `ImagePath`/`ImageAlt` → **every edit silently wipes option images.** There is no existing code that preserves child-collection file refs across a RemoveRange+recreate. Planner MUST decide ONE of:
- **(Recommended) update-in-place:** match existing options by position A-D, update text/IsCorrect, preserve `Id`+`ImagePath` unless a new file/remove applies. Avoids the wipe entirely.
- **(Fallback) carry-forward by position:** before RemoveRange, snapshot `oldOpt[i].ImagePath`; when re-adding, reassign the old path to the same letter unless replaced/removed; options that vanish (4→3) → their path becomes a delete-candidate (subject to ref-count).

Closest related primitive: SyncPackagesToPost option clone (L5379-5383, simple copy) and the existing RemoveRange itself (L6302). Neither solves preservation — this is genuinely new wiring.

**Ordering (OQ2 / Pitfall 2):** set new paths + SaveChanges → auto-sync (L6357-6369) → ref-count old paths → File.Delete. For replace, the old Pre path may still be on Post until sync re-clones with the new path; running ref-count AFTER sync makes the old path truly unreferenced → safe delete.

**Tx note:** EditQuestion currently has no explicit transaction (single SaveChangesAsync L6323). Planner decides whether to wrap in `BeginTransactionAsync` (333 style) when file deletes are involved, OR keep single-SaveChanges + post-save ref-count delete (lighter; delete is post-commit-equivalent since there's one SaveChanges). Either is acceptable per C-04 as long as File.Delete is strictly post-persist + warn-only + ref-counted.

---

### DeleteQuestion POST (controller, file-I/O delete) — `AssessmentAdminController.cs:6377`

**Analogs:** CDPController.DeleteCoachingSession (Phase 333, SHARED-1) + self existing cascade (L6385-6391) + SHARED-2 ref-count.

Current code: removes responses (L6385-6388), `RemoveRange(q.Options)` + `Remove(q)` (L6390-6391), single SaveChanges in try/catch DbUpdateException (L6392-6400), then auto-sync (L6417-6425). NO file cleanup today → orphan.

**Wiring:**
1. BEFORE delete: collect `pathsToDelete` = `q.ImagePath` (if set) + each `o.ImagePath` (if set), from the Included Options (L6380).
2. Keep existing RemoveRange/Remove + SaveChanges.
3. AFTER existing auto-sync block, run SHARED-2 ref-count + SHARED-1 File.Delete loop.

Planner decides tx wrap (333) vs reuse existing single-SaveChanges + post-save delete. Mirror the catch(DbUpdateException) already present (L6396).

---

### DeletePackage POST (controller, file-I/O cascade) — `AssessmentAdminController.cs:5457` (D-11, IN SCOPE)

**Analogs:** Phase 333 (SHARED-1) + self existing cascade (L5468-5487) + SHARED-2 + self auto-sync (L5517-5525).

Current code: removes responses (L5471-5475) + assignments (L5478-5482) + options + questions + package (L5484-5487), single SaveChanges try/catch (L5489-5497), audit (L5499-5512), auto-sync (L5517-5525). NO file cleanup → orphan (D-11 fixes this).

**Wiring:**
1. Collect ALL paths from `pkg.Questions` (each `q.ImagePath`) + `q.Options` (each `o.ImagePath`) BEFORE the RemoveRange (entity Included at L5459-5461).
2. Keep existing cascade + SaveChanges + audit.
3. AFTER auto-sync (L5525), run SHARED-2 ref-count + SHARED-1 File.Delete.

D-11 note: auto-sync re-clones Post with shared paths; ref-count after sync protects any path still shared with the (rebuilt) Post or other packages. If Pre+Post both gone, count==0 → delete.

---

### SyncPackagesToPost (service, transform DB-only) — `AssessmentAdminController.cs:5337` (SYN-01)

**Analog:** exact self, deep-clone block L5370-5384.

**2-line add to `newQ` + 2-line add to option clone (RESEARCH Pattern 3, string copy ONLY, NO file op):**
```csharp
var newQ = new PackageQuestion {
    QuestionText = q.QuestionText, Order = q.Order, ScoreValue = q.ScoreValue,
    QuestionType = q.QuestionType, ElemenTeknis = q.ElemenTeknis,
    Rubrik = q.Rubrik, MaxCharacters = q.MaxCharacters,
    ImagePath = q.ImagePath,   // ← ADD (SYN-01, C-03 shared-file)
    ImageAlt  = q.ImageAlt,    // ← ADD
    Options = q.Options.Select(o => new PackageOption {
        OptionText = o.OptionText, IsCorrect = o.IsCorrect,
        ImagePath = o.ImagePath, // ← ADD
        ImageAlt  = o.ImageAlt    // ← ADD
    }).ToList()
};
```
CRITICAL: the RemoveRange of old Post packages here (L5345-5351) must NOT File.Delete — Post shares Pre's path; ref-count elsewhere protects it. Sync stays pure-DB.

---

### ManagePackageQuestions.cshtml (view, multipart) — form L122, options L150-163, JS L399

**Analogs:** self form (L122) + ImportPackageQuestions.cshtml enctype (L76 `enctype="multipart/form-data"`) + self option loop (L150-163) + self `populateEditForm` (L399-441).

**D-02 form attr** (L122) — add enctype (mirror ImportPackageQuestions.cshtml:76):
```html
<form id="questionForm" asp-action="CreateQuestion" asp-controller="AssessmentAdmin" method="post"
      enctype="multipart/form-data">
```

**File input convention** (mirror ImportPackageQuestions.cshtml:81 `<input type="file" name="excelFile" ...>`): use `name="questionImage"`, `name="optionAImage"` etc. matching controller params. Layout = Opsi A inline (D-01): question image field under textarea (L142), option image field under each `input-group` inside the loop (L150-163). Per UI-SPEC: `.img-drop` block, 46px thumb, Pilih/Ganti `<label class="btn">` wrapping file input, alt-text input, edit-mode "Hapus gambar" checkbox `name="removeQuestionImage"` value="true".

**JS prefill** (populateEditForm, L399) — extend to set `<img src=data.imagePath>` thumbnail + alt input (D-06/D-03); `<input type=file>` stays empty (browser security). Mirror the existing per-option loop at L425-433 (adds image alongside text/correct).

**JS FileReader thumbnail** (D-07a, RESEARCH Pattern 5) — NEW vanilla JS in the existing `@section Scripts` IIFE (L280); no framework, consistent with file's plain JS. On file `change`: render data-URL into 46px `<img>`, flip field to green has-img state, show filename+size meta.

**Preview AJAX** unchanged (loadPreview L370 already fetches `_PreviewQuestion` partial as HTML).

---

### _PreviewQuestion.cshtml (partial, render) — after L17, in loop L60 (D-07b / RND-04)

**Analog:** exact self structure (question text L17, option loop L50-62).

Add `<img>` after question text (L17) and inside each option `<label>` (near L60). Per UI-SPEC: question `img-fluid` max-height 240px, option `img-fluid` max-height 120px, both `rounded border`, `loading="lazy"`, `alt="@Model.ImageAlt"` (empty alt valid for decorative). Render NOTHING when ImagePath null (no placeholder). Model is `PackageQuestion` (L2) with `Options` already available; image props at AssessmentPackage.cs:60/64 (question) and :89/93 (option).

```razor
@if (!string.IsNullOrWhiteSpace(Model.ImagePath))
{
    <img src="@Model.ImagePath" alt="@Model.ImageAlt" class="img-fluid rounded border mb-3"
         style="max-height:240px" loading="lazy" />
}
```

---

### Test files (NEW) — `HcPortal.Tests/QuestionImageSyncTests.cs` + `QuestionImageRefCountTests.cs`

**Analog:** `HcPortal.Tests/FileUploadHelperTests.cs` — `MakeFile` (L19-27), `MakeTempDir` (L41-46), `TestLogger` (L30-39), temp-dir try/finally cleanup (L182-210). **EF InMemory DbContext setup = NO analog in this test file — compose from primitive.**

Framework: xUnit 2.9.3 + EF Core InMemory 8.0.0 (RESEARCH §Validation). Reuse `MakeTempDir`/`MakeFile` (currently private — duplicate into new files OR extract to shared helper, planner's call).

Coverage map (RESEARCH Wave 0):
- `QuestionImageSyncTests.cs` — SYN-01: after SyncPackagesToPost, Post `PackageQuestion`/`PackageOption` carry same `ImagePath`+`ImageAlt` as Pre (InMemory DB, no filesystem).
- `QuestionImageRefCountTests.cs` — D-10: shared file NOT deleted while 1 ref remains; deleted when 0 refs (temp dir + InMemory). D-11: DeletePackage deletes non-shared, skips shared. D-05: replace conflict (file-baru-menang). IMG-01/02 validation already covered by FileUploadHelperTests (L106-174) — no duplication.

**🚩 COMPOSE note:** the ref-count/delete logic lives inside controller methods that need `_context`, `_env`, `_userManager`, `_auditLog`. Pure InMemory DbContext tests the ref-count *query* (AnyAsync) + sync clone in isolation; actual `File.Delete` needs a real temp dir (MakeTempDir pattern). Planner decides isolation strategy (extract ref-count to a testable helper vs integration-test the controller) — there is no existing controller-integration test harness in this test project to copy.

---

## No Analog Found (compose from primitives — planner uses RESEARCH patterns)

| Item | Role | Data Flow | Reason |
|------|------|-----------|--------|
| Reference-count guard (D-10, SHARED-2) | controller | DB query | Genuinely new (RESEARCH §233). Only `AnyAsync` primitive exists; no shared-file ref-count anywhere in repo. Compose per RESEARCH Pattern 2. |
| EditQuestion option-image preservation (OQ1) | controller | CRUD child-collection | RRemoveRange+recreate wipes child file refs; no existing code preserves child file paths across recreate. HIGH regression risk (A3). Compose per RESEARCH Code Examples L319 + OQ1. |

> Both relate: the ref-count is the safety net; the option-preservation is the harder design call. Treat EditQuestion option handling as the riskiest task of the phase.

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdminController, CDPController, TrainingAdminController), `Helpers/FileUploadHelper.cs`, `Models/AssessmentPackage.cs`, `Views/Admin/` (ManagePackageQuestions, _PreviewQuestion, ImportPackageQuestions), `HcPortal.Tests/`.
**Files scanned:** 8 source + 3 views + 1 test.
**Pattern extraction date:** 2026-06-08
**Line-number caveat:** all line refs are as-read 2026-06-08; match by symbol/method name during execution.
