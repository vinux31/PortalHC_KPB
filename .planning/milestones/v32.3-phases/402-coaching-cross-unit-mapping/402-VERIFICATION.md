---
phase: 402-coaching-cross-unit-mapping
verified: 2026-06-19T00:00:00Z
status: passed
score: 9/9
overrides_applied: 0
deferred:
  - truth: "Nyquist validation document finalized (402-VALIDATION.md status=compliant)"
    addressed_in: "Phase 404"
    evidence: "Phase 404 goal: run /gsd-validate-phase with SQL-real fixture covering all CXU invariants; 402-VALIDATION.md is a pre-execution template, not evidence of missing tests — all 8+1skip facts run and green"
  - truth: "SQL-real single-active unique index invariant test for coaching cross-unit"
    addressed_in: "Phase 404"
    evidence: "Phase 404 SC-2: test invariant single-active di SQL riil (QA-03); CrossUnitAssignTests.cs line 93 has [Fact(Skip='deferred to Phase 404 QA-03')] stub — intentionally deferred per Pitfall-5"
human_verification: []
---

# Phase 402: Coaching Cross-Unit Mapping — Verification Report

**Phase Goal:** HC dapat memetakan 1 coach memegang coachee lintas-unit selama masih 1 Bagian — daftar eligible = semua coachee Bagian itu, server menolak cross-Bagian, AssignmentUnit di-set per-coachee dari unit coachee yang dipilih, dan coach ber-akun multi-unit melihat/meng-export semua coachee-nya di seluruh unit dalam Bagian.
**Verified:** 2026-06-19
**Status:** PASSED
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (Roadmap Success Criteria)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | HC memilih Bagian → memilih coach → daftar coachee eligible = semua coachee di Bagian itu (lintas unit), bukan hanya unit coach (CXU-01) | VERIFIED | `ViewBag.CoacheeUnits` di-query dari `_context.UserUnits` per eligible coachee + view renders `data-units` JSON per baris; `applyCoachScope()` filters checklist ke coach.Section; UAT 402-04: checklist GAST → filtered correctly |
| 2 | Server menolak coachee cross-Bagian — guard baru di endpoint assign mem-enforce coachee ⊆ Bagian coach (CXU-02) | VERIFIED | `CoacheeSectionMatchesCoach(_context, cid, coach.Section)` loop di :562; error message "N coachee bukan anggota Bagian coach (cross-Bagian ditolak)" at :565; CrossUnitAssignTests 2 facts GREEN; UAT DB write confirmed (iwan+arsyad, same Section=GAST) |
| 3 | AssignmentUnit di-set per-coachee dari unit coachee yang dipilih (payload map coacheeId->unit + dropdown per-baris dari coachee.UserUnits, tiap unit divalidasi per PSU-03) (CXU-03) | VERIFIED | `CoachAssignRequest.AssignmentUnits Dictionary<string,string>?` at :1920; per-coachee loop resolves `resolvedUnits[id]` at :689; `ValidateAssignmentUnitInUserUnits` per-coachee; `.coachee-unit-select` per-row in markup; submitAssign builds `AssignmentUnits` map; UAT DB: iwan3.AssignmentUnit=Amine, arsyad.AssignmentUnit=Alkylation |
| 4 | Lock JS "satu batch = satu unit" di-relax ke level Bagian (boleh multi-unit dalam 1 Bagian dalam satu batch) (CXU-04) | VERIFIED | `selectedUnits.size > 1` removed (grep returns 0); `updateAssignmentDefaults()` is no-op; Bagian-level E-2 backstop in submitAssign instead; UAT IC-3: Iwan+Arsyad checked together, neither disabled |
| 5 | Coach/Supervisor ber-akun multi-unit melihat & meng-export semua coachee di seluruh unit-nya dalam Bagian (CDPController self-scope union, CXU-05) | VERIFIED | `section = user.Section; unit = user.Unit;` removed (count=0) at :305+:326; `lockedUnit = user.Unit` removed (count=0) at :647; 3x coach.UserUnits queries; `UnitFilterEnabled = unitFilterEnabled` at :718; view `!Model.UnitFilterEnabled && Model.RoleLevel >= 5`; UAT 402-03: CDP dropdown enabled, union=2 coachees (Iwan@Amine + Rino@Alkylation), narrow=1 |
| 6 | dotnet build 0 error + dotnet run + Playwright/UAT: assign cross-unit dalam 1 Bagian sukses; cross-Bagian ditolak server; coach multi-unit lihat semua coachee (all REQ) | VERIFIED | `Build succeeded. 0 Error(s)` confirmed live; suite 540/0/6 (no regression); e2e spec parses 4 tests; combined UAT @5270 PASS 402-03+402-04 (2026-06-19, fixture seeded+restored) |
| 7 | Static seam CoacheeSectionMatchesCoach exists, testable via InMemory DbContext, returns false when coachee.Section != coach.Section | VERIFIED | `public static async Task<bool> CoacheeSectionMatchesCoach` at :66; 3 facts in CrossUnitAssignTests.cs (same/cross/empty-section) all GREEN |
| 8 | CoachAssignRequest carries per-coachee AssignmentUnits map alongside legacy AssignmentUnit | VERIFIED | `public Dictionary<string, string>? AssignmentUnits` at :1920; legacy `public string? AssignmentUnit` retained (count=2 occurrences, the field + param) |
| 9 | RED tests for CXU-02/03/01/05 logic seams exist and pass; e2e skeleton with data-guard skip and port 5270 | VERIFIED | CrossUnitAssignTests.cs: 5 facts + 1 [Fact(Skip)] stub; CdpCoachUnionScopeTests.cs: 3 facts; total 8 pass + 1 skip = 9; e2e: 4 CXU tests, port 5270 documented |

**Score:** 9/9 truths verified

### Deferred Items

Items not yet met but explicitly addressed in later milestone phases.

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | 402-VALIDATION.md nyquist_compliant finalized (still shows draft/false — template state) | Phase 404 | Phase 404 goal includes /gsd-validate-phase with SQL-real fixture; the 8+1skip tests are running and green; template not updated by /gsd-validate-phase which has not run yet |
| 2 | SQL-real single-active unique index invariant test | Phase 404 | Phase 404 SC-2 (QA-03): test single-active di SQL riil; CrossUnitAssignTests.cs:93 [Fact(Skip='deferred to Phase 404 QA-03')] — explicit documented deferral per Pitfall-5 |

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `HcPortal.Tests/CrossUnitAssignTests.cs` | RED unit tests CXU-01/02/03 logic seams | VERIFIED | 5 facts + 1 [Fact(Skip)] stub; CoacheeSectionMatchesCoach referenced 8 times |
| `HcPortal.Tests/CdpCoachUnionScopeTests.cs` | RED unit tests CXU-05 union/narrow scope | VERIFIED | 3 facts (union/narrow/foreign-coerce), all GREEN |
| `Controllers/CoachMappingController.cs` | CoacheeSectionMatchesCoach seam + AssignmentUnits field + CXU-02 guard + CXU-03 loop + ViewBag.CoacheeUnits | VERIFIED | Seam at :66; field at :1920; guard at :551-565; loop with resolvedUnits at :577-609; ViewBag.CoacheeUnits at :194-200 |
| `Controllers/CDPController.cs` | Stop forcing primary at :305/:326/:647; coach.UserUnits AvailableUnits; UnitFilterEnabled | VERIFIED | All 3 forced-primary sites replaced; 3 UserUnits queries present; UnitFilterEnabled wired at :718 |
| `Models/CDPDashboardViewModel.cs` | UnitFilterEnabled bool flag | VERIFIED | `public bool UnitFilterEnabled { get; set; }` at :70 |
| `Views/CDP/Shared/_CoachingProtonPartial.cshtml` | UnitFilterEnabled consumed; old RoleLevel>=5 hardcode removed | VERIFIED | `!Model.UnitFilterEnabled && Model.RoleLevel >= 5` at :31; old hardcode count=0; "Semua Unit" option retained |
| `Views/Admin/CoachCoacheeMapping.cshtml` | data-units JSON + per-row unit select + applyCoachScope + AssignmentUnits payload + lock removed | VERIFIED | data-units=1; scalar data-unit=0; coachee-unit-select=2; applyCoachScope=1; AssignmentUnits: assignmentUnits=1; selectedUnits.size>1=0; CSRF=6; coacheeSectionFilterGroup=2 |
| `tests/e2e/coaching-crossunit-402.spec.ts` | 4 CXU tests, port 5270, data-guard skips | VERIFIED | 4 tests listed by playwright; port 5270 documented; 8 test.skip() data-guards; coachee-unit-select + coacheeSectionFilterGroup referenced |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `HcPortal.Tests/CrossUnitAssignTests.cs` | `CoachMappingController.CoacheeSectionMatchesCoach + ValidateAssignmentUnitInUserUnits` | static method call against InMemory ApplicationDbContext | WIRED | 8 occurrences of CoacheeSectionMatchesCoach in test file; test filter 8 passed / 1 skipped |
| `CoachCoacheeMappingAssign endpoint` | `CoachMappingController.CoacheeSectionMatchesCoach + ValidateAssignmentUnitInUserUnits` | per-coachee server-side validation loop | WIRED | `CoacheeSectionMatchesCoach(_context, cid, coach.Section)` at :562; `resolvedUnits[cid]` dict; `resolvedUnits[id]` in mapping creation at :689 |
| `CoachCoacheeMapping action` | `ViewBag.CoacheeUnits` | `_context.UserUnits GroupBy ToDictionary (primary-first)` | WIRED | `unitsByEligibleCoachee` at :194-200; `ViewBag.CoacheeUnits = unitsByEligibleCoachee` at :200 |
| `submitAssign payload` | `POST /Admin/CoachCoacheeMappingAssign (CoachAssignRequest.AssignmentUnits)` | JSON map built from .coachee-unit-select + single-unit data-units fallback | WIRED | `AssignmentUnits: assignmentUnits` in payload object at view :813; endpoint has `[FromBody] CoachAssignRequest req`; Dictionary field :1920 |
| `coachee-item data-units` | `ViewBag.CoacheeUnits (Plan 02)` | `Html.Raw(JsonSerializer.Serialize(...))` per coachee | WIRED | `data-units='@Html.Raw(System.Text.Json.JsonSerializer.Serialize(cUnits))'` at :447; `coacheeUnitsDict = ViewBag.CoacheeUnits as Dictionary<string,List<string>>` at :439 |
| `CDPController self-scope (FilterCoachingProton / ExportDashboardProgress / BuildProtonProgressSubModelAsync)` | `BuildProtonProgressSubModelAsync base-scope union (post-filter :465-545)` | pass unit=null (do not force user.Unit) so post-filter skipped | WIRED | Forced-primary removed at :305+:326+:673; post-filter lines 465-545 untouched (read verified) |
| `_CoachingProtonPartial.cshtml unit dropdown` | `Model.UnitFilterEnabled + Model.AvailableUnits` | disabled attribute bound to !UnitFilterEnabled | WIRED | `var unitLocked = !Model.UnitFilterEnabled && Model.RoleLevel >= 5` at :31; `Model.AvailableUnits` consumed in @foreach loop |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `Views/Admin/CoachCoacheeMapping.cshtml` (per-row unit select) | `coacheeUnitsDict` (Dict coacheeId->List) | `_context.UserUnits.Where(uu => eligibleCoacheeIds.Contains && IsActive).Select(IsPrimary).ToListAsync()` at CoachMappingController:194-200 | Yes — EF query against UserUnits junction table | FLOWING |
| `Views/CDP/Shared/_CoachingProtonPartial.cshtml` (unit dropdown) | `Model.AvailableUnits` | `_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive).Select(uu => uu.Unit).Distinct().ToListAsync()` at CDPController:686-689 | Yes — EF query against UserUnits for the logged-in coach | FLOWING |
| `CoachCoacheeMappingAssign` (per-coachee mapping create) | `resolvedUnits[id]` | Per-coachee loop: `req.AssignmentUnits?.GetValueOrDefault(cid) ?? req.AssignmentUnit` + validated via `ValidateAssignmentUnitInUserUnits` (UserUnits EF query) | Yes — real per-coachee unit from payload + validated against DB | FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| `dotnet build` 0 error | `dotnet build 2>&1 \| grep "Build succeeded\|error CS"` | `Build succeeded. 0 Error(s)` | PASS |
| CrossUnit + CdpCoachUnion tests green | `dotnet test --filter "FullyQualifiedName~CrossUnit\|FullyQualifiedName~CdpCoachUnion"` | Passed: 8, Skipped: 1, Total: 9 | PASS |
| Full xUnit suite no regression | `dotnet test HcPortal.Tests/...` | Passed: 540, Failed: 0, Skipped: 6, Total: 546 | PASS |
| e2e spec parses 4 tests | `npx playwright test coaching-crossunit-402 --list` | 4 CXU tests listed, no parse error | PASS |
| CoacheeSectionMatchesCoach seam in controller | grep check | 1 match at :66 | PASS |
| AssignmentUnits map field in DTO | grep check | 1 match at :1920 | PASS |
| CXU-02 guard wired in assign endpoint | grep resolvedUnits[id] | 1 match at :689 | PASS |
| Forced-primary removed from CDP :305/:326 | grep count | 0 matches | PASS |
| lockedUnit = user.Unit removed from CDP :647 | grep count | 0 matches | PASS |
| UnitFilterEnabled in ViewModel | grep | 1 match at :70 | PASS |
| Old RoleLevel>=5 hardcode removed from CDP view | grep count | 0 matches | PASS |
| Post-filter :465-545 untouched | read lines 460-549 | No coaching-role unit-force within range; only pre-existing AssignmentUnit-aware filter | PASS |
| data-units JSON in markup (CXU-03) | grep count | 1 match | PASS |
| Old scalar data-unit removed | grep count | 0 matches | PASS |
| Single-unit lock removed from JS | grep selectedUnits.size > 1 | 0 matches | PASS |
| applyCoachScope function present | grep count | 1 match | PASS |
| AssignmentUnits: assignmentUnits in submitAssign | grep count | 1 match | PASS |
| CSRF token preserved | grep count | 6 matches | PASS |
| filterAssignmentUnits null-guard (edit cascade) | read :722-735 | `if (!unitSelect) return;` at :725 | PASS |
| UAT IC-1/2/3/4 assign modal | 402-04-SUMMARY UAT result | PASS — coach Rustam/GAST, checklist filtered, per-row dropdown Iwan multi-unit, cross-unit batch Iwan+Arsyad → DB rows different AssignmentUnit same Section | PASS |
| UAT CXU-05 CDP union | 402-03-SUMMARY UAT result | PASS — dropdown enabled, union 2 coachees (Iwan@Amine+Rino@Alkylation), narrow Amine=1 | PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|---------|
| CXU-01 | 402-01, 402-02, 402-04 | HC memilih coach → eligible = semua coachee Bagian itu (lintas unit) | SATISFIED | ViewBag.CoacheeUnits dict; applyCoachScope client filter; UAT checklist filtered to Bagian coach |
| CXU-02 | 402-01, 402-02 | Server mem-enforce coachee ⊆ Bagian coach (cross-Bagian ditolak) | SATISFIED | CoacheeSectionMatchesCoach guard at :562; CrossUnitAssignTests GREEN; UAT DB write verified |
| CXU-03 | 402-01, 402-02, 402-04 | AssignmentUnit per-coachee dari unit coachee dipilih (map + dropdown + PSU-03 validasi) | SATISFIED | AssignmentUnits field :1920; resolvedUnits loop; per-row .coachee-unit-select; UAT DB: iwan3=Amine, arsyad=Alkylation |
| CXU-04 | 402-04 | Lock JS single-unit-per-batch di-relax ke Bagian-level | SATISFIED | selectedUnits.size>1 removed; updateAssignmentDefaults no-op; E-2 Bagian-level backstop; UAT IC-3 Iwan+Arsyad checked together |
| CXU-05 | 402-01, 402-03 | Self-scope coaching-role multi-unit melihat union semua coachee dalam Bagian | SATISFIED | 3 forced-primary sites removed; UnitFilterEnabled; AvailableUnits=coach.UserUnits; UAT CDP union 2 units |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `tests/e2e/coaching-crossunit-402.spec.ts` | multiple | `test.skip(true/condition, ...)` | Info | By design — data-guard skips for tests requiring multi-unit live fixture; CXU-05 deferred to Plan 03 checkpoint. Not a stub — assertions ARE filled for CXU-01/03/04 with guard-skip only when fixture absent |
| `HcPortal.Tests/CrossUnitAssignTests.cs` | 93 | `[Fact(Skip = "deferred to Phase 404 QA-03")]` | Info | By design — Pitfall-5: SQL-real single-active unique index cannot be tested in InMemory. Explicitly documented per plan |

No blockers found.

### Human Verification Required

None. All must-haves verified programmatically. Both UAT checkpoints (Plan 03 CXU-05 CDP union + Plan 04 IC-1/2/3/4 assign modal) were executed live @5270 with a multi-unit fixture (rustam{Alkylation,Amine} coach + coachees Iwan/Rino/Arsyad) on 2026-06-19. DB rows confirmed. Fixture seeded+restored per SEED_JOURNAL.

### Gaps Summary

No gaps. All 5 CXU requirements (CXU-01..05) are fully implemented, server-authoritative, tested (8+1skip logic-seam facts), and UAT-verified in a live browser session. Two deferred items exist (VALIDATION.md template state + SQL-real single-active test) but are explicitly covered by Phase 404 — they do not block this phase's goal.

---

_Verified: 2026-06-19_
_Verifier: Claude (gsd-verifier)_
