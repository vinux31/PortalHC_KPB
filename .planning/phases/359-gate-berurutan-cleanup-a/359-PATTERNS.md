# Phase 359: Gate Berurutan + Cleanup (A) - Pattern Map

**Mapped:** 2026-06-10
**Files analyzed:** 9 (8 modified + 0–1 new helper at planner's discretion)
**Analogs found:** 9 / 9 (all changes are in-place edits of existing files; analogs are same-file or sibling-file)

> Stack: ASP.NET Core MVC + Razor + Bootstrap 5.3 (Portal HC KPB). Bahasa Indonesia. No migration.
> Every "new" behavior in this phase already has a near-identical analog IN THE SAME FILE — reuse verbatim, don't invent.

---

## File Classification

| File (modified) | Role | Data Flow | Closest Analog | Match Quality |
|-----------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` (CreateAssessment POST `:844`, loop `:1335`) | controller | request-response (batch create) | `BackfillProtonPenanda` `:3791` (same file) + `GetEligibleCoachees` `:1407` | exact (same-file skip-summary + eligibility) |
| `Controllers/CoachMappingController.cs` (assign `:457`, MarkGraduated `:1112`, GetEligibleCoachees `:1343`) | controller | request-response (JSON) | assign warning block `:496-564` (same file, soft→hard) + `MarkMappingCompleted` `:1123-1135` | exact (gate already half-present) |
| `Services/ProtonCompletionService.cs` (extend OR new `ProtonYearGate`) | service | CRUD/query | `GetPassedYearsAsync` `:92` (same file) | exact (cross-year basis) |
| `Helpers/CoacheeEligibilityCalculator.cs` (reuse, no edit) | utility | transform (pure) | itself `:14` | exact (reuse as-is) |
| `Controllers/CDPController.cs` (`:376/542/592/3517`) | controller | read/projection | n/a — deletion of bindings | exact (prune in place) |
| `Models/CDPDashboardViewModel.cs` (prune fields `:28/49-50/90`) | model | n/a | n/a — field removal | exact |
| `Models/ProtonModels.cs` → `ProtonTimelineNode.CompetencyLevel` | model | n/a | n/a — field removal | exact |
| `Views/CDP/Shared/_CoacheeDashboardPartial.cshtml` (`:36-47, 94-104`) | component (Razor) | n/a | existing badge styles in `_CoachingProtonContentPartial.cshtml:197-204` | exact (badge reuse) |
| `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` (`:57-83, 190-204, 221-264`) | component (Razor) | n/a | sibling badges `:197-204` | exact |
| `Views/CDP/HistoriProtonDetail.cshtml` (`:91-97`) | component (Razor) | n/a | n/a — block removal | exact |
| `HcPortal.Tests/*` (new gate-logic tests) | test | n/a | `CoacheeEligibilityCalculatorTests.cs` (pure) + `ProtonCompletionServiceTests.cs` (real-SQL fixture) | exact |

---

## Pattern Assignments

### `Controllers/AssessmentAdminController.cs` — gate server-side di CreateAssessment POST (D-01/D-02/D-03/D-04/D-07)

**Analog A (skip-with-summary):** `BackfillProtonPenanda` `:3791-3889` (SAME FILE — gold standard for "loop + counters + skip + TempData summary + audit warn-only").
**Analog B (per-unit eligibility 100%):** `CoachMappingController.GetEligibleCoachees` `:1382-1422` (per-unit resolve + `IsEligiblePerUnit`).

**Where to hook:** the standard session loop is at `:1335-1373`. Currently it builds `sessions` unconditionally. Insert a per-`userId` server-side gate INSIDE the loop (or a pre-pass) so ineligible workers are SKIPPED and counted, eligible ones still get a session. `protonTahunKe` is already resolved at `:1187`; the `Category=="Assessment Proton"` branch is `:1363-1370`.

**Counter + summary pattern to COPY** (from `BackfillProtonPenanda` `:3799` + `:3879`):
```csharp
int created = 0, alreadyExists = 0, notEligible = 0, skipped = 0;
// ... loop with `continue` on each skip-reason, incrementing the matching counter ...
TempData["Success"] = $"Backfill selesai: {created} penanda dibuat, {alreadyExists} dilewati, {notEligible} belum 100%, {skipped} tanpa assignment.";
```
For S1 copy (UI-SPEC): on partial skip use `TempData["Warning"]` (visible even on partial success), on all-eligible use `TempData["Success"]`. Copy strings verbatim from 359-UI-SPEC Copywriting Contract.

**Per-unit 100% gate to COPY** (from `GetEligibleCoachees` `:1407-1421`) — resolve coachee unit then scope statuses, then call helper:
```csharp
var unitDeliverableIds = await _context.ProtonDeliverableList
    .Where(d => d.ProtonSubKompetensi!.ProtonKompetensi!.ProtonTrackId == protonTrackId
             && d.ProtonSubKompetensi!.ProtonKompetensi!.Unit!.Trim() == resolvedUnit.Trim())
    .Select(d => d.Id).ToListAsync();
var expectedCount = unitDeliverableIds.Count;
var myStatuses = progressRecords
    .Where(p => p.CoacheeId == coacheeId && unitDeliverableIds.Contains(p.ProtonDeliverableId))
    .Select(p => p.Status).ToList();
if (CoacheeEligibilityCalculator.IsEligiblePerUnit(myStatuses, expectedCount)) { /* eligible */ }
```
> D-08 fallback (Tahun 3, 0 deliverable → eligible): mirror `GetEligibleCoachees:1364-1373` (`if (!trackDeliverableIds.Any()) → all eligible`). Keep that branch so interview-only Tahun 3 stays eligible until silabus filled.

**Existing Proton-success service call already present** in this file (reuse, do NOT duplicate): `_protonCompletionService.EnsureAsync(...)` at `:3649` and `:3758`. `ProtonCompletionService` is already DI-injected here (`:30`, `:43`).

**Validation/early-return convention to MATCH** (from `:1167-1186`): on bad input, set `TempData["Error"]`, rebuild `ViewBag.Users`/`ViewBag.Categories`, `return View("CreateAssessment", model)`.

**Transaction convention** (from `:1379`): `var transaction = await _context.Database.BeginTransactionAsync();` with 3-attempt retry on UNIQUE — keep; gate runs BEFORE building `sessions`.

---

### `Controllers/CoachMappingController.cs` — cross-year hard-block on assign + graduation gate (D-04/D-05/D-08/D-10)

**Analog (cross-year, soft → hard):** the EXISTING progression-warning block at `:496-564`. It already (a) finds `prevTrack` by `TrackType` + `Urutan == requestedTrack.Urutan - 1` (`:503-506`), (b) batch-loads prev-assignment progress (`:519-535`), (c) computes `incompleteCoachees` (`:537-553`). **Phase 359 changes the verdict from soft warning to hard block.**

CURRENT (soft, D-09 — REPLACE for Phase 359 D-05):
```csharp
// :555-561  soft warning-override — Phase 359 makes this a HARD BLOCK
if (incompleteCoachees.Any() && !req.ConfirmProgressionWarning)
{
    return Json(new { success = false, warning = true,
        message = $"{incompleteCoachees.Count} coachee belum menyelesaikan {prevTrack.DisplayName}. Tetap lanjutkan?",
        incompleteCount = incompleteCoachees.Count });
}
```
**Phase 359 target:** drop the `!req.ConfirmProgressionWarning` escape → hard-block when `incompleteCoachees.Any()`. Use S2 copy: `"Tidak bisa assign Tahun {N}: Tahun {N-1} ({TrackType}) belum lulus untuk coachee ini."` Return `Json(new { success = false, message = ... })` (no `warning=true`, no override).
> **Decision for planner (D-03/D-04 consistency):** spec defines "Tahun N-1 lulus" = ada `ProtonFinalAssessment` (penanda) untuk prev assignment, NOT just all-deliverable-approved. The current `:548-552` checks only progress `Approved`. Align the gate to penanda-based check (reuse `ProtonCompletionService.GetPassedYearsAsync` or `IsYearCompletedAsync:1096` which already ANDs `hasFinalAssessment`). Prefer `GetPassedYearsAsync` for parity with CreateAssessment gate.
> **D-06 bypass-exempt + D-07 renewal-exempt:** add a logical skip condition (e.g. assignment flagged bypass, or request is renewal `RenewsSessionId`) BEFORE the hard-block, so future Phase 360 bypass + renewal paths are not blocked. Implement the exempt CONDITION now; full bypass logic = Phase 360.

**JSON error convention to MATCH** (already pervasive in this file, e.g. `:460/463/471/489`): `return Json(new { success = false, message = "..." });`

**Graduation gate (D-10) — ALREADY PRESENT, verify/extend:** `MarkMappingCompleted` `:1123-1135` already blocks when Tahun 3 not complete:
```csharp
// :1130-1135  graduation gate already enforces Tahun 3 done
bool tahun3Complete = await IsYearCompletedAsync(tahun3Assignment.Id);
if (!tahun3Complete)
{
    TempData["Error"] = "Tahun 3 belum selesai — semua deliverable harus Approved dan final assessment harus ada.";
    return RedirectToAction("CoachCoacheeMapping");
}
```
**Phase 359 D-10:** confirm this is sufficient. The spec wants "Tahun 3 sudah lulus = penanda ada". `IsYearCompletedAsync:1102-1105` already ANDs `allApproved && hasFinalAssessment` — penanda IS the final assessment, so the gate is correct. Planner: align the message to S2 copy `"Tidak bisa menandai lulus (graduated): Tahun 3 belum lulus untuk pekerja ini."` and treat as verify-only unless a gap is found.

**DI note for planner:** `CoachMappingController` constructor (`:20-31`) does NOT inject `ProtonCompletionService`. If the cross-year helper is reused here, ADD it to the constructor (it is already `AddScoped` in `Program.cs:57`). `using HcPortal.Services;` already present (`:7`).

**Transaction + duplicate-race convention to MATCH** (from `:578` + `:641-651`): `await using var tx = await _context.Database.BeginTransactionAsync();` + catch `DbUpdateException` on `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` with friendly message (no raw `dbEx.Message` leak — Phase 334 D6).

---

### `Services/ProtonCompletionService.cs` — cross-year gate basis (D-03; helper choice = Claude's Discretion)

**Analog:** `GetPassedYearsAsync` `:92-101` (SAME FILE, pure no-gate query). Returns `List<TahunKe>` for (coacheeId, trackType) via `ProtonFinalAssessments JOIN ProtonTrackAssignments JOIN ProtonTracks`.

**Reuse path (preferred):** a gate at call-site checks `(await GetPassedYearsAsync(coacheeId, trackType)).Contains(prevTahunKe)`. `ProtonTrack` has `TrackType` (`ProtonModels.cs:12`), `TahunKe` (`:14`), `Urutan` (`:18`) — enough to resolve "prev year".

**New-method path (if planner prefers `ProtonYearGate`):** copy the LINQ join shape from `GetPassedYearsAsync` `:94-100`. Keep the class doc convention (XML `<summary>` explaining gate/no-gate, like `:88-91`).

> Class is DI-registered (`Program.cs:57`) and constructed with `(ApplicationDbContext, ILogger<ProtonCompletionService>)` (`:23-29`). No new dependency needed.

---

### `Helpers/CoacheeEligibilityCalculator.cs` — REUSE AS-IS (D-02)

**No edit.** `IsEligiblePerUnit(myProgressStatuses, expectedCount)` `:14-19`: `expectedCount <= 0 → false`; count mismatch → false; else all `== "Approved"`. Call it from the CreateAssessment gate exactly as `GetEligibleCoachees:1420` does.

---

### Level cleanup — `Controllers/CDPController.cs` (D-11/D-12)

**No analog — these are prune/delete edits.** Four read sites bind `CompetencyLevelGranted`:
- `:376` — `subModel.CompetencyLevelGranted = finalAssessment?.CompetencyLevelGranted;` (CoacheeDashboardSubModel). Remove.
- `:542` — `CompetencyLevelGranted = finalAssessment?.CompetencyLevelGranted` (CoacheeProgressRow init). Remove.
- `:574-594` — entire trend-chart computation block (`scopedCompletedAssessments`, `trendLabels`, `trendValues` incl. `.Average(fa => (double)fa.CompetencyLevelGranted)` at `:592`). Remove.
- `:627-628` — `TrendLabels = trendLabels, TrendValues = trendValues,` in `ProtonProgressSubModel` init. Remove.
- `:3517` — `CompetencyLevel = hasAssessment ? fa!.CompetencyLevelGranted : null` (`ProtonTimelineNode`). Remove.

> `:377` `subModel.CurrentStatus = finalAssessment != null ? "Completed" : "In Progress";` STAYS — it is the penanda-presence status that drives the "Lulus/Selesai" badge. `HasFinalAssessment` (`:541`) STAYS.

### Level cleanup — `Models/CDPDashboardViewModel.cs` (D-12 prune)
Remove: `CoacheeDashboardSubModel.CompetencyLevelGranted` (`:28`), `ProtonProgressSubModel.TrendLabels`/`TrendValues` (`:49-50`), `CoacheeProgressRow.CompetencyLevelGranted` (`:90`). Keep `CurrentStatus` (`:29`) and `HasFinalAssessment` (`:89`).

### Level cleanup — `Models/ProtonModels.cs`
Remove `ProtonTimelineNode.CompetencyLevel` (bound at `CDPController:3517`, rendered at `HistoriProtonDetail.cshtml:91-97`). `Status` (`= "Lulus" / "Dalam Proses"` at `:3516`) STAYS as the badge driver.

---

### Level cleanup — Views (D-11 / S3 / S4)

**`Views/CDP/Shared/_CoacheeDashboardPartial.cshtml`**
- `:36-47` "Competency Level" card (`bi-award-fill text-warning` + `h3 "Level N"`). REPLACE with "Status Proton" stat: badge `"Lulus"` (`badge bg-success`, `bi-award-fill me-1`, no number) when `Model.CurrentStatus == "Completed"`, else `"Belum Lulus"` (`badge bg-secondary bg-opacity-25 text-secondary`). Copy badge markup verbatim from sibling `_CoachingProtonContentPartial.cshtml:193-203`.
- `:94-104` success alert: keep the `progressPercent == 100` "Congratulations" alert (`:96-98`), DELETE the `@if (Model.CompetencyLevelGranted.HasValue) { <span>Competency Level N granted</span> }` inner block (`:99-103`).

**Badge style to COPY (S3 pass/pending/no-track)** — already exists at `_CoachingProtonContentPartial.cshtml:191-204`:
```cshtml
@if (row.HasFinalAssessment)
{
    <span class="badge bg-success"><i class="bi bi-award-fill me-1"></i>Lulus</span>   @* was: Level @row.CompetencyLevelGranted *@
}
else if (row.TotalDeliverables == 0)
{
    <span class="badge bg-light text-muted">No track</span>                            @* UNCHANGED *@
}
else
{
    <span class="badge bg-secondary bg-opacity-25 text-secondary">In Progress</span>   @* UNCHANGED *@
}
```
The ONLY change at `:193-195` is dropping `<i ...>Level @row.CompetencyLevelGranted` → `<i ...>Lulus`.

**`Views/CDP/Shared/_CoachingProtonContentPartial.cshtml`**
- `:57-78` trend-chart card (`col-lg-8`, "Competency Level Granted Over Time", `canvas#protonTrendChart`). DELETE the whole `col-lg-8` block. Promote `Deliverable Status` doughnut (`:79-98`, `col-lg-4`) to a wider column or keep as-is per planner — do NOT leave an empty placeholder (UI-SPEC: no "no data" panel).
- `:190-204` level table cell — change `Level @row.CompetencyLevelGranted` → `Lulus` (see snippet above).
- `:221-259` Chart.js `protonTrendChart` init block (`@if (Model.TrendLabels.Any()) { ... type:'line' ... }`). DELETE. Leave the doughnut/bottleneck inits (`:261+`) intact.

**`Views/CDP/HistoriProtonDetail.cshtml`**
- `:91-97` `@if (node.CompetencyLevel.HasValue) { ... "Level Kompetensi" ... @node.CompetencyLevel }` block. DELETE. The "Lulus/Dalam Proses" status is rendered elsewhere from `node.Status`.

---

### Tests (Claude's Discretion — strategy)

**Analog A (pure helper, no DbContext):** `HcPortal.Tests/CoacheeEligibilityCalculatorTests.cs` — plain `[Fact]`, `using HcPortal.Helpers; using Xunit;`, asserts on pure inputs. Use this shape for any extracted cross-year gate predicate (e.g. "prevYear in passedYears → allow").
```csharp
[Fact] // Tahun 1 → no prereq → allowed
public void Year1_NoPrereq_Allowed() => Assert.True(ProtonYearGate.IsAllowed(prevTahun: null, passedYears: new List<string>()));
```
**Analog B (real-SQL disposable fixture, DI service):** `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-71` — `ProtonCompletionFixture : IAsyncLifetime` spins `HcPortalDB_Test_<guid>` on `localhost\SQLEXPRESS`, runs `MigrateAsync()`, drops on dispose; test class is `[Trait("Category","Integration")] : IClassFixture<ProtonCompletionFixture>`. Use this for any gate test needing real EF joins (cross-year via `GetPassedYearsAsync`). Does NOT touch `HcPortalDB_Dev` → no SEED_WORKFLOW snapshot needed.

---

## Shared Patterns

### TempData feedback (S1/S2 rendering)
**Source:** `Views/Shared/_Layout.cshtml:190-228` global alert region. Keys → Bootstrap alert:
`TempData["Success"]`→`alert-success`, `["Warning"]`→`alert-warning`, `["Error"]`→`alert-danger`, `["Info"]`→`alert-info` (all dismissible).
**Apply to:** CreateAssessment skip-summary (Success/Warning), all gate rejections (Error). Do NOT build a new banner. Controllers just set the key + `RedirectToAction`.

### JSON error envelope (assign rejections)
**Source:** `CoachMappingController` `:460/489/651` — `return Json(new { success = false, message = "..." });`
**Apply to:** cross-year hard-block on assign (drop `warning=true`/`ConfirmProgressionWarning` escape).

### No info-leak on catch (security)
**Source:** `AssessmentAdminController:3883-3885` + `CoachMappingController:646-651` (Phase 334 D6) — log full `ex`, return generic Indonesian message; never surface `ex.Message`/`dbEx.Message` to user.
**Apply to:** all new try/catch in gate code.

### Audit log warn-only
**Source:** `AssessmentAdminController:3865-3877` + `:3769-3779` — `await _auditLog.LogAsync(userId, actorName, action, detail, entityId, entityType)`; wrap in inner try/catch so audit failure never breaks the operation. `actorName = string.IsNullOrWhiteSpace(user.NIP) ? (user.FullName ?? "Unknown") : $"{user.NIP} - {user.FullName}";`
**Apply to:** skip-summary batch create + (optionally) gate rejections.

### Per-unit eligibility (100% gate)
**Source:** `CoachMappingController.GetEligibleCoachees:1407-1421` + `CoacheeEligibilityCalculator.IsEligiblePerUnit`.
**Apply to:** CreateAssessment server-side gate (D-02). Mirror unit-resolution from `AutoCreateProgressForAssignment:1442-1466` (`.Trim()` both sides).

---

## No Analog Found

None. Every change is an in-place edit of an existing file with a same-file or sibling analog. There is no greenfield file in this phase (a new `ProtonYearGate` helper, if the planner chooses, copies the LINQ shape of `ProtonCompletionService.GetPassedYearsAsync`).

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdminController, CoachMappingController, CDPController), `Services/ProtonCompletionService.cs`, `Helpers/CoacheeEligibilityCalculator.cs`, `Models/CDPDashboardViewModel.cs` + `ProtonModels.cs`, `Views/CDP/Shared/*` + `Views/CDP/HistoriProtonDetail.cshtml`, `Views/Shared/_Layout.cshtml`, `HcPortal.Tests/*`.
**Files scanned:** ~14 source files read in full or in the relevant ranges.
**Pattern extraction date:** 2026-06-10
**Project constraints honored:** no migration (CompetencyLevelGranted stays dormant in DB, only bindings pruned); Bahasa Indonesia copy per CLAUDE.md; verify-locally workflow; gate tests use disposable test DB (not HcPortalDB_Dev).
