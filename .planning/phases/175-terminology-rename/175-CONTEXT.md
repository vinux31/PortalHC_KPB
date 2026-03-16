# Phase 175: Terminology Rename - Context

**Gathered:** 2026-03-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Rename all user-facing "Sub Kompetensi" text to "Elemen Teknis" in assessment-related UI only. This covers the Results page spider web chart, the import template Excel generation, the import page hint, and the cross-package warning message. Proton/Silabus "Sub Kompetensi" references are explicitly out of scope (different domain context).

</domain>

<decisions>
## Implementation Decisions

### Rename scope
- Only assessment-related user-facing strings are changed
- DB column name (`SubCompetency`) stays unchanged — no migration needed
- C# class/variable names stay unchanged — internal code only
- Proton/CDP "Sub Kompetensi" references untouched — different domain

### Files to modify
- `Views/CMP/Results.cshtml` — section title, HTML comment, table header (3 changes)
- `Controllers/AdminController.cs` — template header, example row, help text, warning message (4 changes)
- `Views/Admin/ImportPackageQuestions.cshtml` — hint text (1 change)

### Claude's Discretion
- None — all changes are exact string replacements with no ambiguity

</decisions>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above. The 7 TERM requirements in REQUIREMENTS.md specify exact before/after strings.

</canonical_refs>

<code_context>
## Existing Code Insights

### Files and Line References
- `Views/CMP/Results.cshtml:106,111,124` — spider web section comment, title, table header
- `Controllers/AdminController.cs:5398,5416,5430,5738` — Excel template generation and import validation
- `Views/Admin/ImportPackageQuestions.cshtml:27` — import page format hint

### Established Patterns
- All assessment "Sub Kompetensi" strings are user-facing display text (not keys or identifiers)
- The `SubCompetency` field on `PackageQuestion` model stores arbitrary text entered by users — renaming the label doesn't affect stored data

### Integration Points
- No integration changes needed — this is purely a display label rename

</code_context>

<specifics>
## Specific Ideas

No specific requirements — straightforward text replacement.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 175-terminology-rename*
*Context gathered: 2026-03-16*
