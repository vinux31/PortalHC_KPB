---
phase: quick-15
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/AdminController.cs
  - Controllers/CMPController.cs
  - Views/Admin/ManagePackages.cshtml
  - Views/Admin/ImportPackageQuestions.cshtml
  - Views/Admin/PreviewPackage.cshtml
  - Views/Admin/ManageAssessment.cshtml
autonomous: true
requirements: [QUICK-15]

must_haves:
  truths:
    - "Clicking 'Manage Packages' from Admin/ManageAssessment dropdown opens ManagePackages page"
    - "Creating, deleting, importing, and previewing packages all work under /Admin/ URLs"
    - "Back buttons on ManagePackages, ImportPackageQuestions, and PreviewPackage return to Admin/ManageAssessment"
    - "CMP views for packages are removed (no dead files)"
  artifacts:
    - path: "Views/Admin/ManagePackages.cshtml"
      provides: "Package list/create/delete UI under Admin controller"
    - path: "Views/Admin/ImportPackageQuestions.cshtml"
      provides: "Import questions UI under Admin controller"
    - path: "Views/Admin/PreviewPackage.cshtml"
      provides: "Package preview UI under Admin controller"
  key_links:
    - from: "Views/Admin/ManageAssessment.cshtml (dropdown)"
      to: "Admin/ManagePackages"
      via: "asp-action=ManagePackages asp-controller=Admin"
    - from: "Views/Admin/ManagePackages.cshtml (Back button)"
      to: "Admin/ManageAssessment"
      via: "asp-action=ManageAssessment asp-controller=Admin asp-route-tab=assessment"
---

<objective>
Move Package Management (ManagePackages, CreatePackage, DeletePackage, PreviewPackage, ImportPackageQuestions) from CMPController into AdminController and relocate their views from Views/CMP/ to Views/Admin/. Add a "Manage Packages" entry to the ManageAssessment dropdown so the feature is actually accessible from the Admin area.

Purpose: The features exist in CMPController but are not reachable from Admin/ManageAssessment — the intended entry point. They belong in AdminController alongside ManageQuestions and other assessment admin actions.
Output: 3 views moved to Views/Admin/, 5 actions moved to AdminController, ManageAssessment dropdown updated, CMP views deleted.
</objective>

<execution_context>
@C:/Users/Administrator/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/Administrator/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md

## Key interfaces

From Controllers/CMPController.cs (lines 1847–2226) — the Package Management region to move:
- `ManagePackages(int assessmentId)` GET — loads packages, assignment counts, sets ViewBag
- `CreatePackage(int assessmentId, string packageName)` POST — creates AssessmentPackage
- `DeletePackage(int packageId)` POST — cascades through responses/assignments/questions/options
- `PreviewPackage(int packageId)` GET — returns `View(pkg.Questions.OrderBy(q => q.Order).ToList())`
- `ImportPackageQuestions(int packageId)` GET/POST — full Excel/paste import with deduplication
- Private helpers used by ImportPackageQuestions (in CMPController lines 1138–1157):
  - `ExtractCorrectLetter(string raw)` — normalizes "A.", "Option A", "A" → "A"
  - `NormalizeText(string s)` — trims + collapses whitespace + toLower
  - `MakeFingerprint(string q, string a, string b, string c, string d)` — dedup fingerprint

AdminController already has: `using ClosedXML.Excel;`, `_context`, `_userManager`, `_auditLog`.
The helpers `ExtractCorrectLetter`, `NormalizeText`, `MakeFingerprint` do NOT exist yet in AdminController — they must be added.

ManageAssessment.cshtml dropdown (around line 257) currently has:
```html
<li>
    <a class="dropdown-item" asp-action="ManageQuestions" asp-controller="Admin"
       asp-route-id="@group.RepresentativeId">
        <i class="bi bi-list-check me-2"></i>Manage Questions
    </a>
</li>
```
"Manage Packages" entry must be added after this item.

ManagePackages.cshtml Back button currently points to CMP/Assessment — must change to Admin/ManageAssessment.
</context>

<tasks>

<task type="auto">
  <name>Task 1: Move package actions and helpers into AdminController, remove from CMPController</name>
  <files>Controllers/AdminController.cs, Controllers/CMPController.cs</files>
  <action>
In AdminController.cs, insert a new `#region Package Management` block immediately before the closing `#endregion` of the `#region Question Management (Admin)` block (around line 4365, before `#endregion` / closing brace).

The block to insert (copy exactly from CMPController lines 1847–2226 but update all internal redirects to use Admin controller explicitly where needed — they already use plain action names so they will resolve to Admin correctly):

```csharp
#region Package Management (Admin)

[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ManagePackages(int assessmentId)
{
    var assessment = await _context.AssessmentSessions
        .Include(a => a.User)
        .FirstOrDefaultAsync(a => a.Id == assessmentId);
    if (assessment == null) return NotFound();

    var packages = await _context.AssessmentPackages
        .Include(p => p.Questions)
        .Where(p => p.AssessmentSessionId == assessmentId)
        .OrderBy(p => p.PackageNumber)
        .ToListAsync();

    var packageIds = packages.Select(p => p.Id).ToList();
    var assignmentCounts = await _context.UserPackageAssignments
        .Where(a => packageIds.Contains(a.AssessmentPackageId))
        .GroupBy(a => a.AssessmentPackageId)
        .Select(g => new { PackageId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.PackageId, x => x.Count);
    ViewBag.AssignmentCounts = assignmentCounts;

    ViewBag.Packages = packages;
    ViewBag.AssessmentTitle = assessment.Title;
    ViewBag.AssessmentId = assessmentId;

    return View();
}

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CreatePackage(int assessmentId, string packageName)
{
    if (string.IsNullOrWhiteSpace(packageName))
    {
        TempData["Error"] = "Package name is required.";
        return RedirectToAction("ManagePackages", new { assessmentId });
    }

    var assessment = await _context.AssessmentSessions.FindAsync(assessmentId);
    if (assessment == null) return NotFound();

    var existingCount = await _context.AssessmentPackages
        .CountAsync(p => p.AssessmentSessionId == assessmentId);

    var pkg = new AssessmentPackage
    {
        AssessmentSessionId = assessmentId,
        PackageName = packageName.Trim(),
        PackageNumber = existingCount + 1
    };
    _context.AssessmentPackages.Add(pkg);
    await _context.SaveChangesAsync();

    TempData["Success"] = $"Package '{packageName}' created.";
    return RedirectToAction("ManagePackages", new { assessmentId });
}

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeletePackage(int packageId)
{
    var pkg = await _context.AssessmentPackages
        .Include(p => p.Questions)
            .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(p => p.Id == packageId);

    if (pkg == null) return NotFound();

    int assessmentId = pkg.AssessmentSessionId;

    var questionIds = pkg.Questions.Select(q => q.Id).ToList();
    if (questionIds.Any())
    {
        var pkgResponses = await _context.PackageUserResponses
            .Where(r => questionIds.Contains(r.PackageQuestionId))
            .ToListAsync();
        if (pkgResponses.Any())
            _context.PackageUserResponses.RemoveRange(pkgResponses);
    }

    var assignments = await _context.UserPackageAssignments
        .Where(a => a.AssessmentPackageId == packageId)
        .ToListAsync();
    if (assignments.Any())
        _context.UserPackageAssignments.RemoveRange(assignments);

    foreach (var q in pkg.Questions)
        _context.PackageOptions.RemoveRange(q.Options);
    _context.PackageQuestions.RemoveRange(pkg.Questions);
    _context.AssessmentPackages.Remove(pkg);

    await _context.SaveChangesAsync();

    try
    {
        var delUser = await _userManager.GetUserAsync(User);
        var delActorName = string.IsNullOrWhiteSpace(delUser?.NIP) ? (delUser?.FullName ?? "Unknown") : $"{delUser.NIP} - {delUser.FullName}";
        await _auditLog.LogAsync(
            delUser?.Id ?? "",
            delActorName,
            "DeletePackage",
            $"Deleted package '{pkg.PackageName}' from assessment [ID={assessmentId}]" +
                (assignments.Any() ? $" ({assignments.Count} assignment(s) removed)" : ""),
            assessmentId,
            "AssessmentPackage");
    }
    catch { /* audit failure must not roll back successful delete */ }

    TempData["Success"] = $"Package '{pkg.PackageName}' deleted.";
    return RedirectToAction("ManagePackages", new { assessmentId });
}

[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> PreviewPackage(int packageId)
{
    var pkg = await _context.AssessmentPackages
        .Include(p => p.Questions.OrderBy(q => q.Order))
            .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(p => p.Id == packageId);

    if (pkg == null) return NotFound();

    var assessment = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
    if (assessment == null) return NotFound();

    ViewBag.PackageName = pkg.PackageName;
    ViewBag.AssessmentTitle = assessment?.Title ?? "";
    ViewBag.AssessmentId = pkg.AssessmentSessionId;

    return View(pkg.Questions.OrderBy(q => q.Order).ToList());
}

[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ImportPackageQuestions(int packageId)
{
    var pkg = await _context.AssessmentPackages
        .Include(p => p.Questions)
        .FirstOrDefaultAsync(p => p.Id == packageId);
    if (pkg == null) return NotFound();

    ViewBag.PackageId = packageId;
    ViewBag.PackageName = pkg.PackageName;
    ViewBag.AssessmentId = pkg.AssessmentSessionId;
    ViewBag.CurrentQuestionCount = pkg.Questions.Count;
    return View();
}

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ImportPackageQuestions(
    int packageId, IFormFile? excelFile, string? pasteText)
{
    var pkg = await _context.AssessmentPackages
        .Include(p => p.Questions)
            .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(p => p.Id == packageId);
    if (pkg == null) return NotFound();

    var existingFingerprints = pkg.Questions.Select(q =>
    {
        var opts = q.Options.OrderBy(o => o.Id).Select(o => o.OptionText).ToList();
        return MakePackageFingerprint(
            q.QuestionText,
            opts.ElementAtOrDefault(0) ?? "",
            opts.ElementAtOrDefault(1) ?? "",
            opts.ElementAtOrDefault(2) ?? "",
            opts.ElementAtOrDefault(3) ?? "");
    }).ToHashSet();
    var seenInBatch = new HashSet<string>();

    List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct)> rows;
    var errors = new List<string>();

    if (excelFile != null && excelFile.Length > 0)
    {
        rows = new List<(string, string, string, string, string, string)>();
        try
        {
            using var stream = excelFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheets.First();
            int rowNum = 1;
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                rowNum++;
                var q   = row.Cell(1).GetString().Trim();
                var a   = row.Cell(2).GetString().Trim();
                var b   = row.Cell(3).GetString().Trim();
                var c   = row.Cell(4).GetString().Trim();
                var d   = row.Cell(5).GetString().Trim();
                var cor = row.Cell(6).GetString().Trim().ToUpper();
                rows.Add((q, a, b, c, d, cor));
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Could not read Excel file: {ex.Message}";
            return RedirectToAction("ImportPackageQuestions", new { packageId });
        }
    }
    else if (!string.IsNullOrWhiteSpace(pasteText))
    {
        rows = new List<(string, string, string, string, string, string)>();
        var lines = pasteText.Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        int startIndex = 0;
        if (lines.Count > 0)
        {
            var firstCells = lines[0].Split('\t');
            if (firstCells.Length >= 6 && firstCells[5].Trim().ToLower() == "correct")
                startIndex = 1;
        }

        for (int i = startIndex; i < lines.Count; i++)
        {
            var cells = lines[i].Split('\t');
            if (cells.Length < 6)
            {
                errors.Add($"Row {i + 1}: expected 6 columns, got {cells.Length}.");
                continue;
            }
            rows.Add((
                cells[0].Trim(), cells[1].Trim(), cells[2].Trim(),
                cells[3].Trim(), cells[4].Trim(), cells[5].Trim().ToUpper()
            ));
        }
    }
    else
    {
        TempData["Error"] = "Please upload an Excel file or paste question data.";
        return RedirectToAction("ImportPackageQuestions", new { packageId });
    }

    // Cross-package count validation
    var targetSession = await _context.AssessmentSessions.FindAsync(pkg.AssessmentSessionId);
    if (targetSession != null)
    {
        var siblingSessionIds = await _context.AssessmentSessions
            .Where(s => s.Title == targetSession.Title &&
                        s.Category == targetSession.Category &&
                        s.Schedule.Date == targetSession.Schedule.Date)
            .Select(s => s.Id)
            .ToListAsync();

        var siblingPackagesWithQuestions = await _context.AssessmentPackages
            .Include(p => p.Questions)
            .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId)
                     && p.Id != packageId
                     && p.Questions.Any())
            .ToListAsync();

        if (siblingPackagesWithQuestions.Any())
        {
            var validRowCount = rows.Count(r =>
            {
                var (rq, ra, rb, rc, rd, rcor) = r;
                var normalizedCor = ExtractPackageCorrectLetter(rcor);
                return !string.IsNullOrWhiteSpace(rq) &&
                       !string.IsNullOrWhiteSpace(ra) && !string.IsNullOrWhiteSpace(rb) &&
                       !string.IsNullOrWhiteSpace(rc) && !string.IsNullOrWhiteSpace(rd) &&
                       new[] { "A", "B", "C", "D" }.Contains(normalizedCor);
            });

            var referencePackage = siblingPackagesWithQuestions.First();
            int expectedCount = referencePackage.Questions.Count;

            if (validRowCount != expectedCount)
            {
                TempData["Error"] = $"Jumlah soal tidak sama dengan paket lain. {referencePackage.PackageName}: {expectedCount} soal. Harap masukkan {expectedCount} soal.";
                return RedirectToAction("ImportPackageQuestions", new { packageId });
            }
        }
    }

    int order = pkg.Questions.Count + 1;
    int added = 0;
    int skipped = 0;
    for (int i = 0; i < rows.Count; i++)
    {
        var (q, a, b, c, d, cor) = rows[i];
        var normalizedCor = ExtractPackageCorrectLetter(cor);
        if (string.IsNullOrWhiteSpace(q))
        {
            errors.Add($"Row {i + 1}: Question text is empty. Skipped.");
            continue;
        }
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b) ||
            string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(d))
        {
            errors.Add($"Row {i + 1}: One or more options are empty. Skipped.");
            continue;
        }
        if (!new[] { "A", "B", "C", "D" }.Contains(normalizedCor))
        {
            errors.Add($"Row {i + 1}: 'Correct' column must be A, B, C, or D. Got '{cor}'. Skipped.");
            continue;
        }

        var fp = MakePackageFingerprint(q, a, b, c, d);
        if (existingFingerprints.Contains(fp) || seenInBatch.Contains(fp))
        {
            skipped++;
            continue;
        }
        seenInBatch.Add(fp);

        var newQ = new PackageQuestion
        {
            AssessmentPackageId = packageId,
            QuestionText = q,
            Order = order++,
            ScoreValue = 10
        };
        _context.PackageQuestions.Add(newQ);
        await _context.SaveChangesAsync();

        int correctIndex = normalizedCor == "A" ? 0 : normalizedCor == "B" ? 1 : normalizedCor == "C" ? 2 : 3;
        var opts = new[] { a, b, c, d };
        for (int oi = 0; oi < opts.Length; oi++)
        {
            _context.PackageOptions.Add(new PackageOption
            {
                PackageQuestionId = newQ.Id,
                OptionText = opts[oi],
                IsCorrect = (oi == correctIndex)
            });
        }
        await _context.SaveChangesAsync();
        added++;
    }

    if (added == 0 && skipped == 0)
    {
        TempData["Warning"] = "No valid questions found in the import. Check the format and try again.";
        return RedirectToAction("ImportPackageQuestions", new { packageId });
    }
    if (added == 0 && skipped > 0)
    {
        TempData["Warning"] = "All questions were already in the package. Nothing was added.";
        return RedirectToAction("ImportPackageQuestions", new { packageId });
    }

    if (excelFile != null && excelFile.Length > 0)
        TempData["Success"] = $"Imported from file: {added} added, {skipped} skipped.";
    else
        TempData["Success"] = $"{added} added, {skipped} skipped.";

    return RedirectToAction("ManagePackages", new { assessmentId = pkg.AssessmentSessionId });
}

// Package import helpers (named with "Package" prefix to avoid collision)
private static string ExtractPackageCorrectLetter(string raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return raw;
    if (raw.Length == 1) return raw;
    if ("ABCD".Contains(raw[0]) && !char.IsLetterOrDigit(raw[1]))
        return raw[0].ToString();
    if (raw.StartsWith("OPTION ") && raw.Length > 7 && "ABCD".Contains(raw[7]))
        return raw[7].ToString();
    return raw;
}

private static string NormalizePackageText(string s)
    => System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();

private static string MakePackageFingerprint(string q, string a, string b, string c, string d)
    => string.Join("|||", new[] { q, a, b, c, d }.Select(NormalizePackageText));

#endregion
```

Insert this block just BEFORE the closing `#endregion` on line 4365 (which closes the Question Management region), keeping the existing `#endregion` and class-closing brace intact.

Then in CMPController.cs, delete the entire `#region Package Management` block (lines 1847–2226) including its `#region` and `#endregion` markers. Do NOT remove the helper methods at lines 1138–1157 (`ExtractCorrectLetter`, `NormalizeText`, `MakeFingerprint`) since CMPController still uses them for its own exam flow.
  </action>
  <verify>
    <automated>cd C:/Users/Administrator/Desktop/PortalHC_KPB && dotnet build --no-restore 2>&1 | tail -5</automated>
  </verify>
  <done>Build succeeds with 0 errors. AdminController has ManagePackages, CreatePackage, DeletePackage, PreviewPackage, ImportPackageQuestions actions. CMPController no longer has these actions.</done>
</task>

<task type="auto">
  <name>Task 2: Move views to Views/Admin/ and wire ManageAssessment dropdown</name>
  <files>Views/Admin/ManagePackages.cshtml, Views/Admin/ImportPackageQuestions.cshtml, Views/Admin/PreviewPackage.cshtml, Views/Admin/ManageAssessment.cshtml</files>
  <action>
Step 1 — Create Views/Admin/ManagePackages.cshtml by copying Views/CMP/ManagePackages.cshtml with one change: update the Back button link from:
```html
<a asp-action="Assessment" asp-route-view="manage" class="btn btn-secondary">
```
to:
```html
<a asp-action="ManageAssessment" asp-controller="Admin" asp-route-tab="assessment" class="btn btn-secondary">
```

Step 2 — Create Views/Admin/ImportPackageQuestions.cshtml by copying Views/CMP/ImportPackageQuestions.cshtml as-is. The `asp-action` tags have no `asp-controller` attribute so they will resolve to the current controller (Admin) automatically. No changes needed.

Step 3 — Create Views/Admin/PreviewPackage.cshtml by copying Views/CMP/PreviewPackage.cshtml as-is. Same reasoning — `asp-action="ManagePackages"` with no controller will resolve to Admin/ManagePackages. No changes needed.

Step 4 — In Views/Admin/ManageAssessment.cshtml, find the "Manage Questions" dropdown item (around line 257):
```html
<li>
    <a class="dropdown-item" asp-action="ManageQuestions" asp-controller="Admin"
       asp-route-id="@group.RepresentativeId">
        <i class="bi bi-list-check me-2"></i>Manage Questions
    </a>
</li>
```
Add a new list item immediately after it (before the `<li><hr class="dropdown-divider"></li>` line):
```html
<li>
    <a class="dropdown-item" asp-action="ManagePackages" asp-controller="Admin"
       asp-route-assessmentId="@group.RepresentativeId">
        <i class="bi bi-collection me-2"></i>Manage Packages
    </a>
</li>
```

Step 5 — Delete Views/CMP/ManagePackages.cshtml, Views/CMP/ImportPackageQuestions.cshtml, and Views/CMP/PreviewPackage.cshtml.
  </action>
  <verify>
    <automated>cd C:/Users/Administrator/Desktop/PortalHC_KPB && dotnet build --no-restore 2>&1 | tail -5 && ls Views/Admin/ManagePackages.cshtml Views/Admin/ImportPackageQuestions.cshtml Views/Admin/PreviewPackage.cshtml && echo "CMP views gone:" && ls Views/CMP/ManagePackages.cshtml 2>&1 || echo "deleted OK"</automated>
  </verify>
  <done>Build passes. Three views exist in Views/Admin/. Three CMP views are deleted. ManageAssessment dropdown contains "Manage Packages" item linking to Admin/ManagePackages.</done>
</task>

</tasks>

<verification>
After both tasks:
1. `dotnet build` passes with 0 errors
2. Navigate to Admin/ManageAssessment — dropdown for any assessment row shows "Manage Packages"
3. Click "Manage Packages" — URL is `/Admin/ManagePackages?assessmentId=X`, page loads correctly
4. Click "Back to Manage" on ManagePackages — returns to Admin/ManageAssessment
5. Create a package, import questions via Excel or paste, preview questions — all work
6. Views/CMP/ManagePackages.cshtml, ImportPackageQuestions.cshtml, PreviewPackage.cshtml no longer exist
</verification>

<success_criteria>
- `dotnet build` exits 0
- Admin/ManageAssessment dropdown has "Manage Packages" entry pointing to Admin controller
- Full package management flow (create/import/preview/delete) accessible from Admin area
- No orphaned views in Views/CMP/ for package management
</success_criteria>

<output>
After completion, create `.planning/quick/15-move-package-questions-and-import-questi/15-SUMMARY.md`
</output>
