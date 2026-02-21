---
phase: quick-8
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Views/CDP/Index.cshtml
  - Controllers/CDPController.cs
  - Views/CDP/Coaching.cshtml
autonomous: true
must_haves:
  truths:
    - "CDP Index page no longer shows a Laporan Coaching card"
    - "Navigating to /CDP/Coaching returns 404 or redirects (action removed)"
    - "CDP Index page layout remains clean with 4 cards (no gaps or broken grid)"
    - "Progress & Tracking page is unaffected (its inline coaching report modal stays)"
  artifacts:
    - path: "Views/CDP/Index.cshtml"
      provides: "CDP hub without Laporan Coaching card"
      contains: "Plan IDP"
    - path: "Controllers/CDPController.cs"
      provides: "Controller without Coaching/CreateSession/AddActionItem actions"
  key_links:
    - from: "Views/CDP/Index.cshtml"
      to: "Controllers/CDPController.cs"
      via: "Url.Action calls for remaining 4 cards"
      pattern: "Url\\.Action\\(\"(PlanIdp|Progress|Dashboard|ProtonMain)\""
---

<objective>
Remove the "Laporan Coaching" feature entirely from the CDP module.

Purpose: The Progress & Tracking section already serves the same purpose (coaches upload evidence for coachees there), making the standalone Laporan Coaching page redundant. Removing it declutters the CDP hub.

Output: CDP Index shows 4 cards instead of 5, Coaching controller actions removed, Coaching.cshtml view deleted.
</objective>

<execution_context>
@C:/Users/rinoa/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/rinoa/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@Views/CDP/Index.cshtml
@Controllers/CDPController.cs
@Views/CDP/Coaching.cshtml
</context>

<tasks>

<task type="auto">
  <name>Task 1: Remove Laporan Coaching card from CDP Index page</name>
  <files>Views/CDP/Index.cshtml</files>
  <action>
In Views/CDP/Index.cshtml, delete the entire "Laporan Coaching" card block (lines 37-56, the `<!-- Laporan Coaching -->` comment through the closing `</div>` of `col-12 col-md-6 col-lg-3`).

After removal, the grid will have 4 cards (Plan IDP, Progress & Tracking, Dashboard Monitoring, Proton Main) which fits cleanly in a 4-column `col-lg-3` grid. No layout adjustment needed.
  </action>
  <verify>Open Views/CDP/Index.cshtml and confirm: no "Laporan Coaching" text, no `Url.Action("Coaching"`, exactly 4 card blocks remain.</verify>
  <done>CDP Index page has exactly 4 cards: Plan IDP, Progress & Tracking, Dashboard Monitoring, Proton Main. No reference to Laporan Coaching or the Coaching action.</done>
</task>

<task type="auto">
  <name>Task 2: Remove Coaching controller actions and delete Coaching view</name>
  <files>Controllers/CDPController.cs, Views/CDP/Coaching.cshtml</files>
  <action>
1. In Controllers/CDPController.cs, remove the following three action methods entirely:
   - `Coaching` (GET, ~line 632-751) — the main page action with role-based filtering, coachee list, etc.
   - `CreateSession` (POST, ~line 753-798) — creates a CoachingSession record
   - `AddActionItem` (POST, ~line 800-832) — adds ActionItem to a session

   These are contiguous methods. Remove from line 632 (`public async Task<IActionResult> Coaching(`) through line 832 (closing brace of AddActionItem, the line before `public async Task<IActionResult> ProtonMain()`).

   IMPORTANT: Do NOT remove:
   - The `ProtonMain` action (starts right after AddActionItem)
   - Any `using` statements at the top (CoachingSession/ActionItem models may be used elsewhere in Progress)
   - The coaching report modal in Progress.cshtml (that's a different feature within Progress & Tracking)

2. Delete the file Views/CDP/Coaching.cshtml entirely.

3. Do NOT delete Models/CoachingLog.cs, Models/CoachingSession.cs, or any DB model files — the CoachingSessions table and related models may still be referenced by the Progress & Tracking coaching report feature and the database schema. Only the standalone page and its controller actions are removed.
  </action>
  <verify>
    - Confirm `Coaching` action method no longer exists in CDPController.cs: search for "IActionResult Coaching" should return no matches
    - Confirm `CreateSession` action method no longer exists: search for "IActionResult CreateSession" should return no matches
    - Confirm `AddActionItem` action method no longer exists: search for "IActionResult AddActionItem" should return no matches
    - Confirm Views/CDP/Coaching.cshtml no longer exists
    - Confirm `ProtonMain` action still exists in CDPController.cs
    - Run `dotnet build` to verify no compile errors
  </verify>
  <done>CDPController has no Coaching, CreateSession, or AddActionItem actions. Coaching.cshtml view file is deleted. Project compiles successfully. ProtonMain and all other CDP actions remain intact.</done>
</task>

</tasks>

<verification>
1. `dotnet build` compiles without errors
2. CDP Index page (`/CDP/Index`) shows exactly 4 cards — no Laporan Coaching
3. `/CDP/Coaching` URL returns 404 (action removed)
4. `/CDP/Progress` still works and its inline coaching report modal is unaffected
5. No remaining references to `Url.Action("Coaching"` in any .cshtml file under Views/CDP/
</verification>

<success_criteria>
- Laporan Coaching card removed from CDP Index
- Coaching.cshtml view deleted
- Coaching, CreateSession, AddActionItem controller actions removed
- Project builds successfully
- Progress & Tracking feature unaffected
</success_criteria>

<output>
After completion, create `.planning/quick/8-remove-laporan-coaching-from-cdp/8-SUMMARY.md`
</output>
