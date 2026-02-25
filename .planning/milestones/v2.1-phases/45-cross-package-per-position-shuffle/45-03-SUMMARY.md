---
phase: 45-cross-package-per-position-shuffle
plan: 03
status: complete
---

# Summary: Plan 03 — Reshuffle + ImportValidation + ManagePackages Panel

## What was done

### Fix 1: ReshufflePackage
- Removed "Select a different package" block and all single-package logic (otherPackages, selectedPackage variable, question/option shuffle loops)
- Replaced with `BuildCrossPackageAssignment(packages, rng)` call
- Uses `sentinelPackage = packages.First()`, `ShuffledOptionIdsPerQuestion = "{}"`
- Audit log message updated: `"Reshuffled package (cross-package) for user ... {N} questions from {M} packages"`
- Return: `packageName = $"Cross-package ({packages.Count} paket)"`

### Fix 2: ReshuffleAll
- Per-session loop: replaced selectedPackage selection + shuffle blocks with `BuildCrossPackageAssignment(packages, rng)` call
- `rng` declared once before foreach, reused across all sessions
- Each session gets independent draw (independent slot-list per worker per user decision)
- Status: `$"Reshuffled (cross-package, {packages.Count} paket)"`

### Fix 3: ImportPackageQuestions cross-package validation
- Added validation block before "Validate and persist rows" loop
- Loads sibling packages (same Title/Category/Date) that already have questions
- Computes `validRowCount` from `rows` list using same field validation logic as persist loop
- If mismatch: `TempData["Error"] = "Jumlah soal tidak sama dengan paket lain. {name}: {n} soal. Harap masukkan {n} soal."`
- Pattern: `TempData + RedirectToAction` (consistent with existing error handling in this action)

### Fix 4: ManagePackages.cshtml summary panel
- Added @{} block computing `hasMismatch`, `referenceCount`, `isMultiPackage`, `modeLabel`
- Panel inserted above main `.row` div (after TempData alerts)
- Shows: Mode label (Single Package / Multi-Package N paket), per-package badges (OK=green, Warning=yellow, Kosong=grey), mismatch warning when counts differ
- No new dependencies (Bootstrap 5 + Bootstrap Icons already in use)

## Verification
- `BuildCrossPackageAssignment` called 3 times total: StartExam, ReshufflePackage, ReshuffleAll
- `ShuffledOptionIdsPerQuestion = "{}"` in all 3 paths
- `Jumlah soal tidak sama` message present in ImportPackageQuestions
- `Multi-Package` and `modeLabel` present in ManagePackages.cshtml
