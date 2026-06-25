---
phase: 420
plan: 02
one_liner: "Hidden options[i].Id carrier per option row in authoring form — reletter rename + populate from GET JSON opt.id + clone-reset clears hidden (gotcha §2c)"
status: complete
commit: 2a52e10d
---

# Phase 420 Plan 02 — Summary

## What changed
`Views/Admin/ManagePackageQuestions.cshtml`:
- **PATCH A** template: added `<input type="hidden" class="opt-id-input" name="options[@i].Id" id="option_@(letter)_id" value="" />` as first child of `.option-row` (so it clones with the row).
- **PATCH B** `reletterRows()`: rename hidden `name='options['+i+'].Id'` + `id='option_'+L+'_id'` per position (value preserved — reletter only renames).
- **PATCH C** `populateEditForm()`: set hidden Id from GET JSON `opt.id` (`idEl.value = (opt.id != null) ? String(opt.id) : ''`); padding rows beyond opts.length keep empty Id (new).
- **PATCH D** `addOptionRow()` clone-reset: added `if (inp.type === 'hidden') inp.value = '';` — **CRITICAL gotcha §2c**: without it, a cloned new row inherits row[0]'s Id → new option silently UPDATEs option A. Now new rows get empty Id → ADD path.
- removeOptionRow/ensureRowCount untouched (correct by construction — removed row's Id absent from submit = removed candidate).

## Requirements
OPTEDIT-05 (+ enables OPTEDIT-01/02/03 end-to-end via the form).

## Verification
- `dotnet build` → 0 errors (Razor compiles).
- grep AC all pass: `opt-id-input` ×2 (template + reletter selector); `name="options[@i].Id"` ×1; `idInput.name = 'options['` ×1; `(opt.id != null)` ×1; `inp.type === 'hidden'` ×1.
- Real-browser reindex/clone/populate proven by Plan 03 Playwright (add-option-then-save catches the §2c gotcha; controller tests cannot).

## Threat model status
T-420-04 (clone inherits Id → silent overwrite A) mitigated by clone-reset clearing hidden. T-420-01 transferred to server (Plan 01 validates Id; view is only carrier). T-420-06 (XSS) accept — value is integer Id via `.value` (not innerHTML).

migration=FALSE. NOT pushed.
