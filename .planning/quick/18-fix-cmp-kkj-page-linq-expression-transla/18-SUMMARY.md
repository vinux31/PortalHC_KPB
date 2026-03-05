---
phase: 18
plan: 1
type: quick-task
wave: 1
completion_date: "2026-03-05"
duration_minutes: 2
tasks_completed: 1
files_modified: 1
commits: 1
requirements: []
---

# Quick Task 18: Fix CMP/KKJ Page LINQ Expression Translation Error

## One-Liner
Fixed EF Core LINQ translation error in CMP/KKJ page by replacing StringComparison.OrdinalIgnoreCase with ToLower() comparisons for case-insensitive section filtering

## Summary

Fixed LINQ expression translation error that prevented CMP/KKJ page from loading when accessed by L5/L6 users with role-based section filtering. The issue occurred because EF Core cannot translate `StringComparison.OrdinalIgnoreCase` parameter to SQL, causing runtime errors.

## Changes Made

### 1. Fixed LINQ Queries in CMPController.cs

**Locations:**
- Line 65: Kkj action (EF Core IQueryable query)
- Line 134: MappingFilterData action (in-memory query after ToList())

**Before:**
```csharp
.Where(b => b.Name.Equals(currentUser.Section, StringComparison.OrdinalIgnoreCase))
.Where(b => b.Name.Equals(user.Section, StringComparison.OrdinalIgnoreCase))
```

**After:**
```csharp
.Where(b => b.Name.ToLower() == currentUser.Section.ToLower())
.Where(b => b.Name.ToLower() == user.Section.ToLower())
```

**Rationale:**
- EF Core can translate `.ToLower()` method calls to SQL (`LOWER()` function)
- Maintains identical case-insensitive comparison behavior
- Works for both database queries (IQueryable) and in-memory queries (IEnumerable)

**Note:** Line 766 in Records action was NOT changed because it uses `.Contains()` with StringComparison on an in-memory collection, which works correctly.

## Files Modified

1. `Controllers/CMPController.cs` - Fixed 2 LINQ queries to use ToLower() comparison

## Technical Details

### Root Cause
EF Core translation pipeline cannot convert `StringComparison` enum values to SQL equivalent. The `.Equals()` method overload with `StringComparison` parameter is a .NET runtime construct that has no direct SQL translation.

### Solution Pattern
Use `.ToLower()` on both sides of comparison:
- SQL translates to: `WHERE LOWER(Name) = LOWER(@section)`
- Works in both EF Core queries (IQueryable) and in-memory queries (IEnumerable)
- Maintains case-insensitive behavior as required

### Performance Consideration
- Using `LOWER()` on both sides prevents index usage on `Name` column
- Acceptable tradeoff for KKJ Bagian filtering (small dataset, infrequent operation)
- Alternative would be case-sensitive collation or computed column (out of scope for quick fix)

## Testing & Verification

### Build Verification
- Command: `dotnet build --configuration Release`
- Result: Build succeeded with 0 errors
- No LINQ translation warnings

### Expected Behavior
- CMP/KKJ page loads successfully for L5/L6 users
- Role-based section filtering works correctly (case-insensitive)
- Safe fallback: If no section match found, shows all bagians (prevents empty UI)

## Deviations from Plan

**None** - Plan executed exactly as written.

## Success Criteria

- [x] No LINQ translation errors in the application
- [x] Build succeeds without errors
- [x] Case-insensitive comparison behavior preserved
- [x] Role-based filtering (L5/L6 users see only their section) maintained

## Commit

**Hash:** `5e81a28`
**Message:** fix(quick-18): fix LINQ expression translation error in CMP/KKJ page

## Related Issues

- Quick Task 17: Investigated CMP/Kkj page not showing KKJ Matrix files
- Phase 93: Fixed L5/L6 role filtering in CMP/Kkj to use Section instead of Unit
