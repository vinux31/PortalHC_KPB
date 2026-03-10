# Phase 146: Excel Import Update - Research

**Researched:** 2026-03-10
**Domain:** ClosedXML Excel template + import parsing, C# string normalization
**Confidence:** HIGH

## Summary

This phase extends the existing Excel question import system (AdminController lines 5068-5373) to support an optional 7th column "Sub Kompetensi". The codebase is well-understood: DownloadQuestionTemplate builds a 6-column XLSX via ClosedXML, and ImportPackageQuestions parses both Excel files and tab-separated paste text. The PackageQuestion.SubCompetency nullable string field already exists from Phase 145.

The changes are straightforward: add column G to the template, extend the row tuple to include a 7th optional field, implement Title Case normalization, and update the view's format reference. Backward compatibility is achieved by checking column/cell count (6 = old format, 7+ = new format).

**Primary recommendation:** Extend the existing tuple-based row parsing to a 7-element tuple, with SubCompetency defaulting to null when the 7th element is absent.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Sub Kompetensi is Column G (after Correct) -- columns A-F unchanged
- Old 6-column templates remain backward compatible; parser checks if column G exists
- Title Case normalization: "instrumentasi" -> "Instrumentasi", "PROSES KONTROL" -> "Proses Kontrol"
- Trim + collapse whitespace: "  Instrumentasi  " -> "Instrumentasi", "Proses  Kontrol" -> "Proses Kontrol"
- Free-text input -- no validation against a master list
- No length/character restrictions -- accept and normalize anything
- Blank/empty Sub Kompetensi -> NULL in database (not empty string)
- Both Excel upload AND paste-text support Sub Kompetensi as optional 7th column
- 6-column input = old format, SubCompetency = NULL; 7-column input = new format
- Example row value: "Sub Kompetensi x.x" (generic, not domain-specific)
- Instruction row: "Kolom Sub Kompetensi: opsional, isi nama sub-kompetensi. Kosongkan jika tidak ada."
- No preview step -- import directly like current behavior
- Sub Kompetensi errors don't block row import; normalize and accept
- Soft warning if Sub Kompetensi set differs from sibling packages (don't block import)

### Claude's Discretion
- Warning message wording for cross-package mismatch
- Exact Title Case implementation (handling edge cases like acronyms)
- Row-level error message format for import summary

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| SUBTAG-01 | HC dapat import soal dengan kolom opsional "Sub Kompetensi" di template Excel | Template modification (column G header, example, instruction row) |
| SUBTAG-03 | Import logic memparse, menormalisasi (trim/case), dan menyimpan Sub Kompetensi per soal -- backward compatible | Row parsing extension, NormalizeSubCompetency helper, backward-compatible column detection |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | Already in project | Excel template generation + parsing | Already used in DownloadQuestionTemplate and ImportPackageQuestions |
| System.Globalization.TextInfo | .NET built-in | Title Case conversion | `TextInfo.ToTitleCase()` handles most cases natively |

No new dependencies needed.

## Architecture Patterns

### Current Code Structure (to extend)
```
AdminController.cs
  L5068  DownloadQuestionTemplate()     -- builds 6-col XLSX
  L5132  ImportPackageQuestions(POST)    -- parses Excel + paste-text
  L5153  rows tuple: (Question, OptA, OptB, OptC, OptD, Correct)
  L5293  creates PackageQuestion + PackageOptions
  L5357  ExtractPackageCorrectLetter()  -- helper
  L5368  NormalizePackageText()         -- helper
  L5371  MakePackageFingerprint()       -- helper
```

### Pattern 1: Title Case Normalization Helper
**What:** A static helper to normalize SubCompetency values
**When to use:** During import row processing, before saving to DB
**Example:**
```csharp
private static string? NormalizeSubCompetency(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw)) return null;
    // Collapse whitespace + trim
    var cleaned = System.Text.RegularExpressions.Regex.Replace(raw.Trim(), @"\s+", " ");
    // Title Case via TextInfo
    return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
}
```

### Pattern 2: Backward-Compatible Column Detection
**What:** Check column count to determine format version
**Excel path:** Check if `row.Cell(7)` has a non-empty value (or check header row for "Sub Kompetensi")
**Paste path:** Check `cells.Length >= 7` -- if so, parse 7th cell as SubCompetency

### Pattern 3: Extended Row Tuple
**What:** Change tuple from 6 to 7 elements
```csharp
// Before
List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct)> rows;
// After
List<(string Question, string OptA, string OptB, string OptC, string OptD, string Correct, string? SubCompetency)> rows;
```

### Anti-Patterns to Avoid
- **Don't validate SubCompetency against a master list** -- it's free-text by design
- **Don't use empty string for missing SubCompetency** -- must be NULL
- **Don't change existing error message format for columns A-F** -- only add SubCompetency-related messages

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Title Case | Manual char-by-char casing | `TextInfo.ToTitleCase(str.ToLowerInvariant())` | Handles Unicode, word boundaries correctly |
| Whitespace collapse | Manual string splitting/joining | `Regex.Replace(s, @"\s+", " ")` | Already used in NormalizePackageText |

## Common Pitfalls

### Pitfall 1: TextInfo.ToTitleCase doesn't lowercase ALL-CAPS words
**What goes wrong:** `ToTitleCase("PROSES KONTROL")` returns "PROSES KONTROL" (unchanged)
**Why it happens:** .NET's ToTitleCase preserves all-caps words, treating them as acronyms
**How to avoid:** Always call `.ToLowerInvariant()` first, then `.ToTitleCase()` -- this is already shown in the helper above

### Pitfall 2: Excel empty cells vs whitespace
**What goes wrong:** `row.Cell(7).GetString()` returns "" for empty cells, but could return whitespace
**How to avoid:** Use `string.IsNullOrWhiteSpace()` check, which handles both cases, then return null

### Pitfall 3: Paste-text header row detection
**What goes wrong:** Current header detection checks `firstCells[5].Trim().ToLower() == "correct"` -- needs to also handle 7-column headers
**How to avoid:** Header detection already works (checks column index 5), but update the "expected N columns" error message from hardcoded "6" to "at least 6"

### Pitfall 4: Cross-package SubCompetency comparison
**What goes wrong:** Comparing sets of SubCompetency values across sibling packages requires querying sibling package questions
**How to avoid:** Query sibling packages' questions' SubCompetency values (distinct, non-null), compare with current import batch. Use existing sibling query pattern from lines 5220-5258.

## Code Examples

### Template Extension (DownloadQuestionTemplate)
```csharp
// Add 7th header
var headers = new[] { "Question", "Option A", "Option B", "Option C", "Option D", "Correct", "Sub Kompetensi" };

// Add 7th example value
var example = new[] { /* ...existing 6... */, "Sub Kompetensi x.x" };

// Add 2nd instruction row (row 4, or append to existing row 3)
ws.Cell(4, 1).Value = "Kolom Sub Kompetensi: opsional, isi nama sub-kompetensi. Kosongkan jika tidak ada.";
ws.Cell(4, 1).Style.Font.Italic = true;
ws.Cell(4, 1).Style.Font.FontColor = XLColor.DarkRed;
```

### Excel Parsing Extension
```csharp
// Inside Excel row loop, after parsing cells 1-6:
string? subComp = null;
var cell7 = row.Cell(7).GetString().Trim();
if (!string.IsNullOrWhiteSpace(cell7))
    subComp = cell7;
rows.Add((q, a, b, c, d, cor, subComp));
```

### Paste-Text Parsing Extension
```csharp
// Change minimum column check
if (cells.Length < 6)
{
    errors.Add($"Row {i + 1}: expected at least 6 columns, got {cells.Length}.");
    continue;
}
string? subComp = cells.Length >= 7 ? cells[6].Trim() : null;
if (string.IsNullOrWhiteSpace(subComp)) subComp = null;
rows.Add((cells[0].Trim(), cells[1].Trim(), cells[2].Trim(),
          cells[3].Trim(), cells[4].Trim(), cells[5].Trim().ToUpper(), subComp));
```

### Saving SubCompetency
```csharp
var newQ = new PackageQuestion
{
    AssessmentPackageId = packageId,
    QuestionText = q,
    Order = order++,
    ScoreValue = 10,
    SubCompetency = NormalizeSubCompetency(subComp)  // nullable
};
```

### View Format Reference Update
```html
<code class="ms-2">Question | Option A | Option B | Option C | Option D | Correct | Sub Kompetensi (opsional)</code>
```

### Cross-Package SubCompetency Warning
```csharp
// After import loop, check sibling packages for SubCompetency mismatch
if (added > 0 && siblingPackagesWithQuestions.Any())
{
    var importedSubs = rows.Where(r => r.SubCompetency != null)
        .Select(r => NormalizeSubCompetency(r.SubCompetency)!)
        .Distinct().OrderBy(s => s).ToList();

    if (importedSubs.Any())
    {
        var siblingSubs = await _context.PackageQuestions
            .Where(q => siblingPackageIds.Contains(q.AssessmentPackageId) && q.SubCompetency != null)
            .Select(q => q.SubCompetency!)
            .Distinct().ToListAsync();

        if (siblingSubs.Any())
        {
            var missing = siblingSubs.Except(importedSubs).ToList();
            var extra = importedSubs.Except(siblingSubs).ToList();
            if (missing.Any() || extra.Any())
                TempData["Warning"] = $"Sub Kompetensi berbeda dari paket lain. ...";
        }
    }
}
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test suite in project) |
| Config file | none |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SUBTAG-01 | Template has 7th column "Sub Kompetensi" | manual | Download template, open in Excel, verify column G | N/A |
| SUBTAG-03 | Import parses + normalizes SubCompetency, backward compatible | manual | Import 6-col file (null SubComp), import 7-col file (normalized values) | N/A |

### Sampling Rate
- **Per task commit:** Manual browser verification
- **Per wave merge:** Full import flow test (both file and paste, both 6-col and 7-col)
- **Phase gate:** All 4 success criteria verified manually

### Wave 0 Gaps
None -- no automated test infrastructure in this project; manual testing is the established pattern.

## Sources

### Primary (HIGH confidence)
- Direct code reading: AdminController.cs lines 5068-5373 (template + import logic)
- Direct code reading: Models/AssessmentPackage.cs (SubCompetency property)
- Direct code reading: Views/Admin/ImportPackageQuestions.cshtml (import UI)
- .NET documentation: System.Globalization.TextInfo.ToTitleCase behavior

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - no new libraries, extending existing ClosedXML code
- Architecture: HIGH - clear extension of existing patterns, all code reviewed
- Pitfalls: HIGH - TextInfo.ToTitleCase ALL-CAPS behavior is well-documented .NET gotcha

**Research date:** 2026-03-10
**Valid until:** 2026-04-10 (stable, no external dependencies changing)
