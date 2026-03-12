---
phase: quick-23
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/ProtonDataController.cs
  - Controllers/CDPController.cs
autonomous: true
requirements: [QUICK-23]

must_haves:
  truths:
    - "Silabus tables on ProtonData/Index sort alphabetically by name instead of by Urutan"
    - "Silabus tables on CDP/PlanIdp sort alphabetically by name instead of by Urutan"
  artifacts:
    - path: "Controllers/ProtonDataController.cs"
      provides: "Alphabetical ordering in Index action"
      contains: "NamaKompetensi"
    - path: "Controllers/CDPController.cs"
      provides: "Alphabetical ordering in PlanIdp action"
      contains: "NamaKompetensi"
  key_links: []
---

<objective>
Sort silabus tables alphabetically by name fields instead of by numeric Urutan field on both ProtonData/Index and CDP/PlanIdp pages.

Purpose: Users expect alphabetical ordering for easier lookup.
Output: Two controller files updated with alphabetical OrderBy.
</objective>

<context>
@Controllers/ProtonDataController.cs
@Controllers/CDPController.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Change OrderBy from Urutan to alphabetical name fields</name>
  <files>Controllers/ProtonDataController.cs, Controllers/CDPController.cs</files>
  <action>
In Controllers/ProtonDataController.cs Index action (around lines 148, 153, 155):
- Change `.OrderBy(k => k.Urutan)` to `.OrderBy(k => k.NamaKompetensi)`
- Change `.OrderBy(s => s.Urutan)` to `.OrderBy(s => s.NamaSubKompetensi)`
- Change `.OrderBy(d => d.Urutan)` to `.OrderBy(d => d.NamaDeliverable)`

In Controllers/CDPController.cs PlanIdp action (around lines 120, 125, 127):
- Same three changes: Urutan to NamaKompetensi / NamaSubKompetensi / NamaDeliverable
  </action>
  <verify>
    <automated>cd "C:/Users/Administrator/Desktop/PortalHC_KPB" && dotnet build --no-restore 2>&1 | tail -5</automated>
  </verify>
  <done>Both controllers use alphabetical OrderBy on all 3 silabus levels. Build succeeds.</done>
</task>

</tasks>

<verification>
- `grep -n "OrderBy.*Urutan" Controllers/ProtonDataController.cs Controllers/CDPController.cs` returns no matches in the silabus query sections
- `grep -n "OrderBy.*Nama" Controllers/ProtonDataController.cs Controllers/CDPController.cs` shows 3 matches per file
- `dotnet build` succeeds
</verification>

<success_criteria>
Silabus tables sort alphabetically by NamaKompetensi, NamaSubKompetensi, and NamaDeliverable on both pages. Build passes.
</success_criteria>

<output>
After completion, create `.planning/quick/23-sort-silabus-tables-alphabetically-on-pr/23-SUMMARY.md`
</output>
