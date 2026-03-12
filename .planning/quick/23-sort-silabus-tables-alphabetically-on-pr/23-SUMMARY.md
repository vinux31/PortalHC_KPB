---
phase: quick-23
plan: "01"
one_liner: "Sort silabus tables alphabetically by name on ProtonData and PlanIdp pages"
---

# Quick Task 23: Sort Silabus Tables Alphabetically

## Changes

Changed silabus table ordering from `Urutan` (numeric insertion order) to alphabetical by name fields on both pages:

**ProtonDataController.cs (Index action):**
- Kompetensi: `.OrderBy(k => k.Urutan)` → `.OrderBy(k => k.NamaKompetensi)`
- SubKompetensi: `.OrderBy(s => s.Urutan)` → `.OrderBy(s => s.NamaSubKompetensi)`
- Deliverable: `.OrderBy(d => d.Urutan)` → `.OrderBy(d => d.NamaDeliverable)`

**CDPController.cs (PlanIdp action):**
- Same 3 changes as above

## Files Modified
- `Controllers/ProtonDataController.cs` (lines 148, 153, 155)
- `Controllers/CDPController.cs` (lines 120, 125, 127)
