---
phase: 45-cross-package-per-position-shuffle
plan: 01
status: complete
---

# Summary: Plan 01 — Migration + BuildCrossPackageAssignment + StartExam Rewrite

## What was done
- Created `Migrations/20260225100000_ClearUserPackageAssignments.cs` — data migration with `DELETE FROM UserPackageAssignments` in Up(), empty Down() (clean break)
- Added `BuildCrossPackageAssignment` private static helper to `CMPController` (near Shuffle helper):
  - 1 package: returns questions in original DB Order (no shuffle)
  - N packages (N≥2): even distribution (K/N per package, remainder randomly allocated), Fisher-Yates shuffle of slot list, then slot[i] → package[i].Questions[pkgCounter[i]]
- Rewrote StartExam `if (assignment == null)` block: calls `BuildCrossPackageAssignment(packages, rng)`, uses `sentinelPackage.Id` (first package) as FK, sets `ShuffledOptionIdsPerQuestion = "{}"`, saves `SavedQuestionCount = shuffledIds.Count`
- Fixed stale-question check: replaced `assignedPackage.Questions.Count` with `packages.Min(p => p.Questions.Count)` (cross-package count), removed `var assignedPackage` line
- Fixed StartExam ViewModel build: replaced `assignedPackage.Questions.ToDictionary` with `allPackageQuestions` (spans all packages), removed `shuffledOptionIds` lookup, options now rendered in DB order (`OrderBy(o => o.Id)`)

## Key decisions
- `AssessmentPackageId = sentinelPackage.Id` — FK sentinel, no schema change
- `ShuffledOptionIdsPerQuestion = "{}"` — option shuffle removed per user decision
- Stale check uses `packages.Min(p => p.Questions.Count)` to match the cross-package question count at assignment creation

## Artifacts
- `Migrations/20260225100000_ClearUserPackageAssignments.cs` — migration (note: .Designer.cs needs to be generated via `dotnet ef migrations add`)
- `Controllers/CMPController.cs` — BuildCrossPackageAssignment added, StartExam assignment + stale check + ViewModel build updated
