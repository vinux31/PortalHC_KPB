---
phase: quick-11
plan: 1
type: execute
wave: 1
depends_on: []
files_modified:
  - Models/TrainingRecord.cs
  - Models/CreateTrainingRecordViewModel.cs
  - Views/CMP/CreateTrainingRecord.cshtml
  - Views/CMP/RecordsWorkerList.cshtml
  - Controllers/CMPController.cs
autonomous: true
must_haves:
  truths:
    - "CreateTrainingRecord form shows a Kota text input field"
    - "Kota value is persisted to the TrainingRecords table when the form is submitted"
    - "Page title and heading say 'Create Training' (not 'Create Training Offline')"
    - "RecordsWorkerList button says 'Create Training' (not 'Create Training Offline')"
  artifacts:
    - path: "Models/TrainingRecord.cs"
      provides: "Kota property on TrainingRecord entity"
      contains: "public string? Kota"
    - path: "Models/CreateTrainingRecordViewModel.cs"
      provides: "Kota property on ViewModel"
      contains: "public string? Kota"
    - path: "Views/CMP/CreateTrainingRecord.cshtml"
      provides: "Kota input field in form, updated page title"
      contains: "Create Training"
    - path: "Views/CMP/RecordsWorkerList.cshtml"
      provides: "Renamed button text"
      contains: "Create Training"
  key_links:
    - from: "Views/CMP/CreateTrainingRecord.cshtml"
      to: "Models/CreateTrainingRecordViewModel.cs"
      via: "asp-for Kota tag helper"
      pattern: "asp-for=\"Kota\""
    - from: "Controllers/CMPController.cs"
      to: "Models/TrainingRecord.cs"
      via: "Kota mapping in POST action"
      pattern: "Kota = model\\.Kota"
---

<objective>
Add a "Kota" (City) text input field to the CreateTrainingRecord form and persist it to the database. Also rename the page from "Create Training Offline" to "Create Training" everywhere it appears.

Purpose: The form needs a city field to record where training took place. The "Offline" suffix in the page name is no longer accurate and should be removed.
Output: Updated model, ViewModel, view, controller, and a new EF Core migration for the Kota column.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/phases/19-hc-create-training-record-certificate-upload/19-01-SUMMARY.md
@Models/TrainingRecord.cs
@Models/CreateTrainingRecordViewModel.cs
@Views/CMP/CreateTrainingRecord.cshtml
@Views/CMP/RecordsWorkerList.cshtml
@Controllers/CMPController.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add Kota field to model, ViewModel, controller mapping, and create migration</name>
  <files>
    Models/TrainingRecord.cs
    Models/CreateTrainingRecordViewModel.cs
    Controllers/CMPController.cs
  </files>
  <action>
1. In `Models/TrainingRecord.cs`, add a nullable string property after the existing v1.6 fields block (after `NomorSertifikat`):
   ```csharp
   public string? Kota { get; set; }           // City where training took place
   ```

2. In `Models/CreateTrainingRecordViewModel.cs`, add an optional Kota property. Place it after Penyelenggara (since Kota is related to location). Not required — just a plain optional text field:
   ```csharp
   [Display(Name = "Kota")]
   public string? Kota { get; set; }
   ```

3. In `Controllers/CMPController.cs`, find the POST `CreateTrainingRecord` action. In the `new TrainingRecord { ... }` block (around line 1176-1190), add `Kota = model.Kota,` to the object initializer — place it after the `Penyelenggara` mapping line.

4. Create the EF Core migration:
   ```bash
   dotnet ef migrations add AddKotaToTrainingRecord
   ```
   Then apply:
   ```bash
   dotnet ef database update
   ```

5. Run `dotnet build` to confirm zero errors.
  </action>
  <verify>
    `dotnet build` produces 0 errors. New migration file exists in Migrations/ folder. Database has the Kota column on TrainingRecords table.
  </verify>
  <done>
    TrainingRecord.Kota property exists, CreateTrainingRecordViewModel.Kota property exists, POST action maps model.Kota to record.Kota, migration created and applied, build clean.
  </done>
</task>

<task type="auto">
  <name>Task 2: Add Kota input to form view and rename page title and button from "Create Training Offline" to "Create Training"</name>
  <files>
    Views/CMP/CreateTrainingRecord.cshtml
    Views/CMP/RecordsWorkerList.cshtml
  </files>
  <action>
1. In `Views/CMP/CreateTrainingRecord.cshtml`:

   a. **Rename page title** — Change line 4 from:
      ```
      ViewData["Title"] = "Create Training Offline";
      ```
      to:
      ```
      ViewData["Title"] = "Create Training";
      ```

   b. **Rename heading** — Change line 13 `<h2>` from `Create Training Offline` to `Create Training`.

   c. **Add Kota input field** — In the "Data Training" card body (the `<div class="row g-3">` block starting around line 48), add a Kota text input. Place it as a new `col-md-6` div right after the Penyelenggara field (after the closing `</div>` on approximately line 61), before the Kategori field. Use the same pattern as other text inputs:
      ```html
      <!-- Kota -->
      <div class="col-md-6">
          <label asp-for="Kota" class="form-label fw-bold">Kota</label>
          <input asp-for="Kota" class="form-control" placeholder="Masukkan kota pelatihan" />
      </div>
      ```
      This keeps Kota next to Penyelenggara since both are location/organization details. No `<span asp-validation-for>` needed since the field is optional (not required).

2. In `Views/CMP/RecordsWorkerList.cshtml`:

   **Rename button text** — Change line 30 from:
   ```
   <i class="bi bi-plus-lg me-2"></i>Create Training Offline
   ```
   to:
   ```
   <i class="bi bi-plus-lg me-2"></i>Create Training
   ```

3. Run `dotnet build` to confirm zero errors.
  </action>
  <verify>
    `dotnet build` produces 0 errors. Grep for "Create Training Offline" in Views/ returns zero matches. Grep for "asp-for=\"Kota\"" in CreateTrainingRecord.cshtml returns a match.
  </verify>
  <done>
    CreateTrainingRecord.cshtml shows "Create Training" as title and heading, contains a Kota text input field in the Data Training section. RecordsWorkerList.cshtml button reads "Create Training". No remaining "Create Training Offline" text in any view file.
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` — 0 errors
2. `grep -r "Create Training Offline" Views/` — returns nothing (all renamed)
3. `grep -r "Kota" Models/TrainingRecord.cs` — shows property exists
4. `grep -r "Kota" Models/CreateTrainingRecordViewModel.cs` — shows property exists
5. `grep -r "Kota = model.Kota" Controllers/CMPController.cs` — shows mapping exists
6. `grep -r "asp-for=\"Kota\"" Views/CMP/CreateTrainingRecord.cshtml` — shows input exists
7. New migration file exists in Migrations/ for AddKotaToTrainingRecord
</verification>

<success_criteria>
- Kota text input visible on CreateTrainingRecord form in the Data Training section
- Submitting the form persists Kota value to the TrainingRecords table
- Page title, heading, and RecordsWorkerList button all say "Create Training" (no "Offline")
- Build passes with zero errors
- EF Core migration created and applied
</success_criteria>

<output>
After completion, create `.planning/quick/11-add-kota-field-to-createtrainingrecord-a/11-SUMMARY.md`
</output>
