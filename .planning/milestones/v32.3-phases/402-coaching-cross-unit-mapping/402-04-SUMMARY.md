# Plan 402-04 Summary — Assign Modal Cross-Unit UI

**Status:** COMPLETE — auto-tasks + UAT PASSED (combined live browser session 2026-06-19, @5270).
**UAT result:** PASS — IC-1 (select coach Rustam/GAST → checklist filtered to GAST, "Filter Seksi" hidden, Bagian Penugasan=GAST+disabled), IC-2 (Iwan multi-unit→per-row dropdown default Alkylation primary; Moch/Arsyad single→no dropdown), IC-3 (Iwan+Arsyad checked together, neither disabled), IC-4 (submit → DB: iwan3 AssignmentUnit=Amine [per-row pick] + arsyad=Alkylation, both Section=GAST, 1 batch to Rustam — CXU-02/03/04 end-to-end through real server). Rustam coachee count 1→3. Fixture seeded+restored (0 residue).
**Migration:** FALSE
**Commit:** 4946ff59

## Objective
Frontend modal assign rewire (CoachCoacheeMapping.cshtml): IC-1 coach-first auto-scope, IC-2 conditional per-row unit dropdown, IC-3 relax single-unit lock → Bagian-level, IC-4 per-coachee AssignmentUnits payload map. + fill e2e CXU-01/03/04.

## What Was Built
**T1 — markup:**
- `@{ coacheeUnitsDict = ViewBag.CoacheeUnits as Dictionary<string,List<string>> }`; per coachee-item `data-units='@Html.Raw(JsonSerializer.Serialize(cUnits))'` (replaced scalar `data-unit`).
- Conditional per-row `<select class="coachee-unit-select" data-coachee-id>` rendered only when `cUnits.Count > 1` (options primary-first; default = units[0]). Single-unit → no select.
- H-2 prompt `<p id="coacheePrompt">Pilih coach terlebih dahulu...`; H-1 hint replaces old lock text (now informational, visible).
- Manual "Filter Seksi Coachee" wrapper `id="coacheeSectionFilterGroup"` (JS toggles). Batch `#assignAssignmentUnit` select REMOVED (per-row replaces). `#assignAssignmentSection` kept (auto-set+disabled by JS).

**T2 — JS:**
- Coach `<select>` onchange → `updateCoachSuggestHint(); applyCoachScope();`. New `applyCoachScope()`: filters checklist to coach.Section, hides prompt + manual filter group, auto-sets+disables `#assignAssignmentSection` = coach.Section, unchecks on coach change (IC-1).
- `updateAssignmentDefaults()` → no-op (removed AF-2 single-unit lock + auto-fill, IC-3).
- `submitAssign()` rewritten: Bagian-level E-2 backstop (all checked ⊆ coach.Section), build `assignmentUnits` map from `.coachee-unit-select` or single `data-units` (E-3 if missing), payload `AssignmentUnits` map + `AssignmentSection=coach.Section` (IC-4); dropped DEAD warning/confirm branch; preserved `appUrl()` + RequestVerificationToken.
- `filterAssignmentUnits()` null-guarded (`if (!unitSelect) return`) — assign batch select gone, edit modal cascade intact.

**T3 — e2e:** filled CXU-01 (filter group hidden + Bagian auto-set+disabled + visible items match section), CXU-03 (per-row select iff units>1, none for single), CXU-04 (two cross-unit coachees check together, none disabled), all with data-guard skips; CXU-05 stub → Plan 03 UAT. Port 5270 documented.

## Key Files
**modified:** `Views/Admin/CoachCoacheeMapping.cshtml`, `tests/e2e/coaching-crossunit-402.spec.ts`

## Verification
- `dotnet build` → Build succeeded, 0 error.
- `npx playwright test coaching-crossunit-402 --list` → 4 CXU tests, no parse error.
- xUnit suite unaffected (view+e2e only) — last full run 540/0/6 (post-402-03).
- Acceptance greps: data-units(1), old scalar data-unit(0), coachee-unit-select(2), H-2(1), coacheeSectionFilterGroup(2), applyCoachScope(1), AssignmentUnits map(1), old lock selectedUnits.size>1(0), CSRF(6), e2e skips(6).

## Deviations
- UAT checkpoint COMBINED with Plan 03 (CDP union) per user decision — one multi-unit fixture + one browser session covers both CDP union (Plan 03) and assign modal (Plan 04).

## Self-Check: PASSED (code) / UAT PENDING (combined w/ 402-03)
Coach-first scope + conditional per-row select + relaxed lock + payload map wired; CSRF/appUrl preserved; edit cascade null-guarded; build + e2e parse green.

## Combined UAT (after this plan)
Fixture: coach w/ 2 active UserUnits in 1 Bagian + coachees mapped per unit (Seed Workflow snapshot/restore). Verify:
- CDP (402-03): dropdown enabled, "Semua Unit" default, union of both units' coachees, narrow per-unit, export.
- Modal (402-04): coach-first scope, Bagian auto-set+disabled, per-row dropdown multi-unit only, two cross-unit coachees assigned together → 2 rows diff AssignmentUnit same Section.
- Optional: crafted cross-Bagian POST rejected (CXU-02 server).
- Run e2e @5270.
