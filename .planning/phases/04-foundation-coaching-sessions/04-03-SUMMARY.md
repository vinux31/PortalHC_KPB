---
phase: 04-foundation-coaching-sessions
plan: 03
subsystem: coaching-sessions
tags: [domain-fields, model-update, migration, viewmodel, razor-view]
dependency_graph:
  requires: ["04-01", "04-02"]
  provides: ["domain-specific coaching session fields", "EF Core migration for schema change"]
  affects: ["CoachingSessions table", "CDP/Coaching page modal and cards"]
tech_stack:
  added: []
  patterns: ["EF Core RenameColumn optimization", "ViewBag with List<string> for dropdown", "Razor switch expression for CSS class"]
key_files:
  created:
    - Migrations/20260217053753_UpdateCoachingSessionFields.cs
    - Migrations/20260217053753_UpdateCoachingSessionFields.Designer.cs
  modified:
    - Models/CoachingSession.cs
    - Models/CoachingViewModels.cs
    - Controllers/CDPController.cs
    - Views/CDP/Coaching.cshtml
    - Migrations/ApplicationDbContextModelSnapshot.cs
decisions:
  - "EF Core used RenameColumn for Topic -> SubKompetensi instead of DropColumn+AddColumn — acceptable optimization, same net result"
  - "Migration applied successfully; no existing coaching session rows were affected in development DB"
metrics:
  duration: 3 min
  completed: 2026-02-17
  tasks_completed: 3
  files_modified: 7
---

# Phase 04 Plan 03: Domain Coaching Fields Summary

Replaced generic Topic/Notes fields with 7 domain-specific coaching fields (Kompetensi, SubKompetensi, Deliverable, CoacheeCompetencies, CatatanCoach, Kesimpulan, Result) across CoachingSession model, viewmodel, controller, view, and database schema.

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | Update CoachingSession model and CreateSessionViewModel | 4b2f98a | Models/CoachingSession.cs, Models/CoachingViewModels.cs |
| 2 | Update Coaching.cshtml modal and session cards | 4b2f98a | Views/CDP/Coaching.cshtml |
| 3 | Create and apply EF Core migration | b781a8c | Migrations/20260217053753_UpdateCoachingSessionFields.cs, ApplicationDbContextModelSnapshot.cs |

Note: Tasks 1 and 2 were committed together (4b2f98a) because the Razor view compilation requires both model and view changes to coexist — the build fails if model removes Topic/Notes but view still references them.

## What Was Built

**CoachingSession model** (`Models/CoachingSession.cs`): Removed `Topic` (string) and `Notes` (string?) properties. Added 7 domain-specific string properties with default "":
- `Kompetensi` — dropdown populated from KkjMatrices master data
- `SubKompetensi` — free-text competency description
- `Deliverable` — target deliverable text
- `CoacheeCompetencies` — multi-line textarea for coachee assessment
- `CatatanCoach` — multi-line textarea for coach notes
- `Kesimpulan` — choice: "Kompeten" or "Perlu Pengembangan"
- `Result` — choice: "Need Improvement" / "Suitable" / "Good" / "Excellence"

**CreateSessionViewModel** (`Models/CoachingViewModels.cs`): Same 7-field replacement. CoacheeId, Date, CoachingHistoryViewModel, and AddActionItemViewModel unchanged.

**CDPController** (`Controllers/CDPController.cs`):
- `Coaching()` GET: Added query loading distinct Kompetensi values from `_context.KkjMatrices`, stored in `ViewBag.KompetensiList`
- `CreateSession()` POST: Maps all 7 new fields to the CoachingSession entity; removed Topic/Notes mappings

**Coaching.cshtml** (`Views/CDP/Coaching.cshtml`):
- Create Session Modal: 4-row layout (Coachee+Date, Kompetensi+SubKompetensi+Deliverable, CoacheeCompetencies+CatatanCoach textareas, Kesimpulan+Result dropdowns)
- Session history cards: Kompetensi as card heading; SubKompetensi/Deliverable/Kesimpulan/Result in summary row with color-coded status; CoacheeCompetencies and CatatanCoach as detail text blocks

**EF Core Migration** (`Migrations/20260217053753_UpdateCoachingSessionFields.cs`):
- DropColumn: `Notes` from CoachingSessions
- RenameColumn: `Topic` -> `SubKompetensi` (EF Core optimization)
- AddColumn (nvarchar(max) NOT NULL defaultValue ""): `Kompetensi`, `Deliverable`, `CoacheeCompetencies`, `CatatanCoach`, `Kesimpulan`, `Result`
- Migration applied successfully to local database

## Verification Results

1. `dotnet build -c Release` — Build succeeded, 0 errors, 0 warnings
2. No Topic/Notes references in CoachingSession.cs, CoachingViewModels.cs, or Coaching.cshtml (grep confirmed 0 matches)
3. CreateSession POST maps all 7 new fields (Kompetensi = model.Kompetensi pattern confirmed)
4. Coaching GET sets ViewBag.KompetensiList from KkjMatrices
5. Migration file has DropColumn Notes, RenameColumn Topic->SubKompetensi, AddColumn x6
6. Database updated — migration applied, `dotnet ef database update` reports "already up to date"

## Deviations from Plan

### Auto-fixed Issues

None — plan executed as written.

### Notes on EF Core Migration Behavior

EF Core 7+ optimizes the migration by using `RenameColumn` instead of `DropColumn` + `AddColumn` when column names are similar in context. The migration renamed `Topic` to `SubKompetensi` rather than dropping Topic and adding SubKompetensi separately. This is equivalent in net effect — both columns disappear from the old schema and appear under new names/additions. The Down() migration correctly reverses this by renaming SubKompetensi back to Topic.

## Self-Check: PASSED

Files verified to exist:
- FOUND: Models/CoachingSession.cs — 7 domain fields, no Topic/Notes
- FOUND: Models/CoachingViewModels.cs — CreateSessionViewModel with 7 domain fields
- FOUND: Controllers/CDPController.cs — ViewBag.KompetensiList assignment + 7-field mapping
- FOUND: Views/CDP/Coaching.cshtml — 7 form field names, no Topic/Notes
- FOUND: Migrations/20260217053753_UpdateCoachingSessionFields.cs

Commits verified:
- FOUND: 4b2f98a — feat(04-03): replace Topic/Notes with 7 domain coaching fields
- FOUND: b781a8c — chore(04-03): add migration UpdateCoachingSessionFields
