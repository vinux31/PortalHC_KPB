---
phase: quick-12
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Controllers/CMPController.cs
  - Views/CMP/Assessment.cshtml
autonomous: true
must_haves:
  truths:
    - "Abandoned sessions appear in Riwayat Ujian table with 'Dibatalkan' badge"
    - "Completed+Passed sessions still show 'Lulus' green badge"
    - "Completed+Failed sessions still show 'Tidak Lulus' red badge"
    - "Abandoned rows display dash for Score and Tanggal Selesai since those are null"
    - "Detail button on Abandoned rows links to Results page (existing behavior)"
  artifacts:
    - path: "Controllers/CMPController.cs"
      provides: "Expanded completedHistory query including Abandoned sessions"
      contains: 'Status == "Abandoned"'
    - path: "Views/CMP/Assessment.cshtml"
      provides: "Three-way status badge (Lulus/Tidak Lulus/Dibatalkan)"
      contains: "Dibatalkan"
  key_links:
    - from: "Controllers/CMPController.cs"
      to: "Views/CMP/Assessment.cshtml"
      via: "ViewBag.CompletedHistory includes Status field"
      pattern: "a\\.Status"
---

<objective>
Fix Riwayat Ujian (Exam History) table on the worker Assessment page so that Abandoned sessions
(self-abandoned by worker) appear alongside Completed sessions. Add a third status badge
"Dibatalkan" (orange/warning) for Abandoned rows. Keep existing Lulus/Tidak Lulus badges intact.

Purpose: Workers currently see no trace of exams they abandoned — the history table only queries
Status == "Completed". This makes it look like Riwayat Ujian is "not updating."

Output: Updated controller query + view with three-way status rendering.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@Controllers/CMPController.cs (lines 247-261 — completedHistory query)
@Views/CMP/Assessment.cshtml (lines 528-579 — Riwayat Ujian table)
</context>

<tasks>

<task type="auto">
  <name>Task 1: Expand completedHistory query to include Abandoned sessions and add Status to projection</name>
  <files>Controllers/CMPController.cs</files>
  <action>
  In the Assessment action method (~line 247-261), modify the completedHistory query:

  1. Change the Where clause from:
     `.Where(a => a.UserId == userId && a.Status == "Completed")`
     to:
     `.Where(a => a.UserId == userId && (a.Status == "Completed" || a.Status == "Abandoned"))`

  2. Change the OrderByDescending to sort by CompletedAt first (descending), then by UpdatedAt descending
     as a fallback for Abandoned sessions that have null CompletedAt:
     `.OrderByDescending(a => a.CompletedAt ?? a.UpdatedAt)`

  3. Add `a.Status` to the Select projection so the view can distinguish Completed from Abandoned:
     ```csharp
     .Select(a => new
     {
         a.Id,
         a.Title,
         a.Category,
         a.CompletedAt,
         a.Score,
         a.IsPassed,
         a.Status
     })
     ```

  Do NOT change any other part of the controller. Do NOT add migrations or model changes.
  </action>
  <verify>Build succeeds: `dotnet build` with no errors.</verify>
  <done>completedHistory query returns both Completed and Abandoned sessions, with Status field in projection.</done>
</task>

<task type="auto">
  <name>Task 2: Update Riwayat Ujian view with three-way status badge and null-safe rendering</name>
  <files>Views/CMP/Assessment.cshtml</files>
  <action>
  In the Riwayat Ujian table section (~line 528-579), update the foreach loop rendering:

  1. Replace the existing two-way IsPassed badge logic (lines 557-565) with three-way logic:
     ```razor
     @if (item.Status == "Abandoned")
     {
         <span class="badge text-bg-warning"><i class="bi bi-slash-circle me-1"></i>Dibatalkan</span>
     }
     else if (item.IsPassed == true)
     {
         <span class="badge text-bg-success"><i class="bi bi-check-circle-fill me-1"></i>Lulus</span>
     }
     else
     {
         <span class="badge text-bg-danger"><i class="bi bi-x-circle-fill me-1"></i>Tidak Lulus</span>
     }
     ```

  2. The existing null handling for CompletedAt and Score already uses ternary with "---" fallback,
     which correctly handles Abandoned rows (where both are null). Verify these lines still read:
     - CompletedAt: `@(item.CompletedAt != null ? ((DateTime)item.CompletedAt).ToString("dd MMM yyyy") : "---")`
     - Score: `@(item.Score != null ? item.Score + "%" : "---")`
     If they use a different dash character, keep the existing one. The point is: null values render
     as a dash, not as errors.

  3. Change the column header "Tanggal Selesai" to "Tanggal" (shorter, since Abandoned sessions
     have no completion date — "Tanggal" is more generic). Actually, keep "Tanggal Selesai" as-is
     since it's clear enough and Abandoned rows will just show "---".

  Do NOT change the table structure, Detail button, or any other section of the view.
  </action>
  <verify>
  Run the app (`dotnet run`), log in as a worker who has at least one Abandoned session,
  navigate to /CMP/Assessment, scroll to Riwayat Ujian section. Verify:
  - Abandoned sessions appear with orange "Dibatalkan" badge
  - Completed sessions still show green "Lulus" or red "Tidak Lulus"
  - Score and Tanggal Selesai show "---" for Abandoned rows
  </verify>
  <done>
  Riwayat Ujian table renders all three session states correctly: Lulus (green), Tidak Lulus (red),
  Dibatalkan (orange/warning). Null Score and CompletedAt display as dashes.
  </done>
</task>

</tasks>

<verification>
1. `dotnet build` succeeds with no errors or warnings related to changed files
2. Worker with Abandoned session sees it in Riwayat Ujian with "Dibatalkan" badge
3. Worker with Completed+Passed session still sees "Lulus" badge
4. Worker with ForceClose session (Completed, Score=0, IsPassed=false) sees "Tidak Lulus" badge
5. No regression in active assessment list (Open/Upcoming filter unchanged)
</verification>

<success_criteria>
- Abandoned sessions visible in Riwayat Ujian with "Dibatalkan" (warning/orange) badge
- Existing Lulus/Tidak Lulus badges unchanged for Completed sessions
- Null CompletedAt and Score render as dashes, no runtime errors
- Build passes cleanly
</success_criteria>

<output>
After completion, create `.planning/quick/12-fix-riwayat-ujian-not-updating-and-add-p/12-SUMMARY.md`
</output>
