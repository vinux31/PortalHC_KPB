# Phase 146: Excel Import Update - Context

**Gathered:** 2026-03-10
**Status:** Ready for planning

<domain>
## Phase Boundary

HC can import questions with optional Sub Kompetensi column via Excel template. Update the download template and parsing logic. Old templates (without Sub Kompetensi) must still work without errors.

</domain>

<decisions>
## Implementation Decisions

### Column Placement
- Sub Kompetensi is Column G (after Correct) — columns A-F unchanged
- Old 6-column templates remain backward compatible; parser checks if column G exists

### Normalization Rules
- Title Case normalization: "instrumentasi" → "Instrumentasi", "PROSES KONTROL" → "Proses Kontrol"
- Trim + collapse whitespace: "  Instrumentasi  " → "Instrumentasi", "Proses  Kontrol" → "Proses Kontrol"
- Free-text input — no validation against a master list
- No length/character restrictions — accept and normalize anything
- Blank/empty Sub Kompetensi → NULL in database (not empty string)

### Input Methods
- Both Excel upload AND paste-text (tab-separated) support Sub Kompetensi as optional 7th column
- 6-column input (either method) = old format, works fine, SubCompetency = NULL
- 7-column input = new format, 7th value parsed as Sub Kompetensi

### Template Design
- Example row value: "Sub Kompetensi x.x" (generic, not domain-specific)
- Add instruction row: "Kolom Sub Kompetensi: opsional, isi nama sub-kompetensi. Kosongkan jika tidak ada."

### Cross-Package Consistency
- No hard validation across packages — HC naturally uses same Sub Kompetensi set per assessment
- Soft warning if Sub Kompetensi set differs from sibling packages (don't block import)

### Preview & Error Handling
- No preview step — import directly like current behavior
- Sub Kompetensi errors don't block row import; normalize and accept

### Claude's Discretion
- Warning message wording for cross-package mismatch
- Exact Title Case implementation (handling edge cases like acronyms)
- Row-level error message format for import summary

</decisions>

<specifics>
## Specific Ideas

- Example value "Sub Kompetensi x.x" — user wants generic placeholder, not domain-specific
- Template instruction text in Indonesian, consistent with existing "Kolom Correct: isi dengan huruf A, B, C, atau D" instruction

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AdminController.cs:5068` DownloadQuestionTemplate — current 6-column template builder with ClosedXML
- `AdminController.cs:5132` ImportPackageQuestions POST — Excel + paste-text parser with duplicate detection
- `AdminController.cs:5357` ExtractPackageCorrectLetter + MakePackageFingerprint helpers

### Established Patterns
- Template: headers row (bold, green bg) + example row (italic, gray) + instruction row (italic, dark red)
- Import: parse rows → validate → deduplicate via fingerprint → create PackageQuestion + PackageOptions → audit log
- Cross-package validation already exists (line 5220-5258) for question count matching — can extend for Sub Kompetensi warning

### Integration Points
- PackageQuestion.SubCompetency property (added in Phase 145) — nullable string column
- Import result messages via TempData["Success"], TempData["Warning"], TempData["Error"]
- Paste-text parser splits on \t, currently expects 6 cells — extend to accept 6 or 7

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 146-excel-import-update*
*Context gathered: 2026-03-10*
