# Quick Task 007: Fix KKJ Matriks Table Header Misalignment

**Date:** 2026-02-20
**Commit:** 1d6b373
**File changed:** `Views/CMP/Kkj.cshtml`

## What Was Done

Fixed two bugs in the `<thead>` of the KKJ Matriks table that caused every data column from position 1 onward to display under the wrong label.

### Bug 1 — Wrong colspan on row 1 group header

The "CSU Process - GSH, Alkylation & Sour Treating" `<th>` had `colspan="11"` but the table has 15 position columns (data-pos 0–14). Changed to `colspan="15"`.

### Bug 2 — Row 2 missing two position headers + wrong ARU grouping

The `<tr class="matrix-header">` (row 2) was missing "Sr Spv GSH & Alky" and "Shift Spv GSH & Alky" headers, causing a 2-column shift that misaligned all subsequent labels. Additionally, "Panelman ARU & Sour" and "Operator ARU & Sour" each appeared as two separate `<th>` with identical text instead of a single `<th colspan="2">`.

**Before (13 logical columns, wrong):**
- Section Head | [Panelman GSH x2] | [Operator GSH x2] | Shift Spv ARU | Panelman ARU | Panelman ARU | Operator ARU | Operator ARU | Sr Spv Facility | Jr Analyst | Officer HSE

**After (15 logical columns, correct):**
- Section Head (1) | Sr Spv GSH (1) | Shift Spv GSH (1) | Panelman GSH colspan=2 (2) | Operator GSH colspan=2 (2) | Shift Spv ARU (1) | Panelman ARU colspan=2 (2) | Operator ARU colspan=2 (2) | Sr Spv Facility (1) | Jr Analyst (1) | Officer HSE (1)
- Total: 1+1+1+2+2+1+2+2+1+1+1 = 15 columns

## Files Modified

| File | Change |
|------|--------|
| `Views/CMP/Kkj.cshtml` | Row 1: colspan 11→15; Row 2: added 2 missing `<th>`, replaced 4 duplicate `<th>` with 2 `<th colspan="2">` |

## Verification

- Build: C# compilation clean (0 errors; pre-existing CS8602 warnings in CDPController unrelated)
- Column count: Row 2 now totals exactly 15 `<th>` logical columns, matching the 15 data-pos cells in each tbody row
