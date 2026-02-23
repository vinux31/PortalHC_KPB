# Phase 30: Import Deduplication - Context

**Gathered:** 2026-02-23
**Status:** Ready for planning

<domain>
## Phase Boundary

When HC imports package questions (via Excel upload or paste), rows that already exist in the current package are skipped — only new questions are added. A skip count is reported in the result message. Scope is per-package only.

</domain>

<decisions>
## Implementation Decisions

### Feedback messaging

- **Success banner format:** Green success banner always (not downgraded to warning) — "X questions imported. Y duplicates skipped."
- **Detail level:** Count only. No list of which questions were skipped.
- **Path-specific messages:**
  - Excel upload: "Imported from file: X added, Y skipped."
  - Paste: "X added, Y skipped."
- **All-duplicates edge case:** Stay on the import page (no redirect). Show a **yellow warning banner**: "All questions were already in the package. Nothing was added."
- **Empty/malformed import (0 valid rows):** Also stay on import page. Show a distinct message — different from the all-duplicates message so HC knows what happened.

### Match precision

- **Case-insensitive:** Yes (from roadmap)
- **Whitespace normalization:** Trim leading/trailing spaces AND collapse internal whitespace runs — "Apa  tujuan" equals "Apa tujuan"
- **Punctuation:** Claude's discretion — choose whatever is most practical given the implementation
- **Duplicate definition:** BOTH question text AND all answer options must match (case-insensitive + normalized). If question text is the same but any option differs → treated as a different (new) question and gets imported normally.

### Behavior on full-duplicate import

- **All duplicates:** Stay on import page with yellow warning banner
- **Some duplicates, some new:** Redirect to ManagePackages with green success banner showing both counts
- **Visible detail:** Count in banner is sufficient — no collapsible list of skipped questions

### Scope of check

- **Package scope:** Check against questions in the current package only. Same question text can legitimately exist in different packages of the same assessment.
- **Self-deduplication:** The import file itself is also deduplicated — if HC's Excel/paste contains two identical rows, only one is imported (and the skip count reflects in-file duplicates too).
- **Preview mode:** Claude's discretion — decide whether dedup runs during preview or only on save, based on existing preview implementation complexity.

### Claude's Discretion

- Punctuation normalization approach (strip or ignore)
- Preview mode dedup behavior (preview vs save-only)
- Exact wording for the empty/malformed 0-valid-rows message

</decisions>

<specifics>
## Specific Ideas

- The "all duplicates" edge case should stay on the import page — HC likely uploaded the wrong file, so they need to fix it without navigating back
- Deduplication is per-package, not per-assessment — packages are intentionally independent so HC can have variant packages with overlapping questions

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 30-import-deduplication*
*Context gathered: 2026-02-23*
