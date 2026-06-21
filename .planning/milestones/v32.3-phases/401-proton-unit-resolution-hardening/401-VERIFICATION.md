---
phase: 401-proton-unit-resolution-hardening
verified: 2026-06-19T10:30:00Z
status: passed
score: 6/6 must-haves verified
overrides_applied: 0
re_verification:
  previous_status: none
  previous_score: n/a
deferred:
  - truth: "Live cross-surface multi-unit fixture (T1@X→T2@Y resolve per surface; empty-AssignmentUnit skip+audit-warn live in DB) — SC #6 broad live-DB sweep"
    addressed_in: "Phase 404"
    evidence: "Phase 404 SC #1/#3 (QA-01/QA-04): test multi-unit SQL riil SQLEXPRESS fixture pekerja {X,Y} 1 Bagian + PROTON Tahun1@X→Tahun2@Y + invariant AssignmentUnit ∈ coachee.UserUnits di setiap junction-write"
  - truth: "Deep SQL-real single-active integration assertions (HTTP context + filtered-unique index enforcement)"
    addressed_in: "Phase 404"
    evidence: "2 [Fact(Skip)] explicitly deferred to Phase 404 QA-01 (ProtonUnitResolveTests:70, UnitUnresolvedAuditTests:73); Phase 404 SC #2 (QA-03) single-active SQL riil"
---

# Phase 401: PROTON Unit-Resolution Hardening Verification Report

**Phase Goal:** Resolusi unit PROTON selalu berbasis `AssignmentUnit` eksplisit (bukan fallback `User.Unit` ambigu saat multi-unit) — coachee di-PROTON-kan di unit non-primary tetap tampil/ter-gate di unit yang benar; pekerja bisa Tahun1@X → Tahun2@Y sekuensial dgn sertifikat tiap unit utuh; tanpa data-loss dari clobber/reactivation. 0 migration.
**Verified:** 2026-06-19T10:30:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths (ROADMAP Success Criteria + PLAN must_haves merged)

| #   | Truth   | Status     | Evidence       |
| --- | ------- | ---------- | -------------- |
| 1 | (SC1/PSU-01) Unit PROTON di-resolve dari `AssignmentUnit`, fallback `User.Unit` dibuang di semua resolver | ✓ VERIFIED | `Select(u => u.Unit)` = **0** matches across all Controllers; 5 resolvers AssignmentUnit-only: CoachMapping GetEligibleCoachees (`:1497`), AutoCreateProgress (`:1553`), AssessmentAdmin cert-gate (`:1414`), CDP defensive ×2 (`:534`,`:1712-block`); single-active invariant preserved (duplicate-active guard `:1105-1108` + IX index untouched) |
| 2 | (SC2/PSU-02) Filter coachee/tim/bypass surface PROTON pakai `AssignmentUnit` (BypassList, coachee-scope CDP) | ✓ VERIFIED | CDP `u.Unit == unit` = **0**; 4 sites swapped: `:491` (batch `unitByCoachee` dict, OrdinalIgnoreCase+Trim), `:1599`/`:1611`/`:4264` (EXISTS subquery). BypassList `x.u.Unit == unit` = **0**, AssignmentUnit subquery `:1522`; bagian/trackId filters intact |
| 3 | (SC3/PSU-03) `AssignmentUnit` ∈ `coachee.UserUnits` di Assign/Edit/Import; bypass `TargetUnit` ∈ worker.UserUnits + org-tree | ✓ VERIFIED | Shared helper called at Assign `:533`, Edit `:793`, Import-new `:374`, Import-reactivate `:399`; Bypass `ProtonDataController:1662` + org-tree `GetSectionUnitsDictAsync :1656`. Edit recent fix (58bb6b11) rejects empty unit (helper returns false on empty → reject before tx) |
| 4 | (SC4/PSU-04+PSU-05) Cleanup + Import no-clobber; 6 read-path skip + audit-warn on empty (gate tak boleh terbit session/cert dgn primary) | ✓ VERIFIED | Cleanup preserve-gate `:957-966` runs BEFORE last-resort clobber `:968-983`; Import-reactivate clobber line removed (`:410` preserve comment). Gate channels: persisted `ProtonUnitUnresolved` AuditLog at GetEligibleCoachees `:1505` + cert-gate `:1420`; read-path LogWarning-only (no `_auditLog`) at AutoCreateProgress `:1562` + CDP defensive `:537`/`:1733` |
| 5 | (SC5/PSU-07) Reactivation: (a) no clobber to primary; (b) validasi ∈ UserUnits sebelum reaktivasi; AF-4 window untouched | ✓ VERIFIED | Reactivate guard `:1111` rejects released-unit before `IsActive=true`; Import-reactivate preserve+validate `:399-415`; AF-4 `EF.Functions.DateDiffSecond` count = **exactly 2** (`:1142-1143`) untouched. PSU-07c (ProtonTrackAssignment unit-match) is out-of-scope per plan (RESEARCH Open Q2) — deferred to Phase 404 |
| 6 | (SC6) `dotnet build` 0 error + D-01 indicator renders live; coachee empty-AssignmentUnit skip+audit-warn | ✓ VERIFIED | `dotnet build` → 0 Error(s). D-01 computed in GET `:187-200` + rendered alert-warning view `:102-106`; **human-verified live via Playwright this session** (401-03-SUMMARY): alert ABSENT@orphan=0, PRESENT@orphan=1 (count "1 mapping aktif", `bi-exclamation-triangle`, screenshot `401-03-d01-present.png`, DB snapshot→seed→restore). Broad cross-surface multi-unit live fixture deferred to Phase 404 |

**Score:** 6/6 truths verified

### Deferred Items

Items not fully covered in this phase but explicitly addressed in later milestone phases (Step 9b). Do NOT count as gaps.

| # | Item | Addressed In | Evidence |
|---|------|-------------|----------|
| 1 | Live cross-surface multi-unit fixture (T1@X→T2@Y per surface; empty-AssignmentUnit live skip+audit sweep) — SC #6 broad live-DB | Phase 404 | SC #1/#3 (QA-01/QA-04): multi-unit SQL-riil SQLEXPRESS fixture + junction-write invariants |
| 2 | Deep SQL-real single-active integration (HTTP+filtered-unique index) | Phase 404 | 2 `[Fact(Skip)]` → Phase 404 QA-01 (ProtonUnitResolveTests:70, UnitUnresolvedAuditTests:73); SC #2 (QA-03) |
| 3 | PSU-07c — Reactivate `ProtonTrackAssignment` unit-match (not "last assignment") | Phase 404 | Out-of-scope per 401-03 plan (RESEARCH Open Q2); Phase 404 SC #2 single-active covers reactivate path SQL-real |

### Required Artifacts

| Artifact | Expected    | Status | Details |
| -------- | ----------- | ------ | ------- |
| `Controllers/CoachMappingController.cs` | helper + 5 call sites + resolvers + no-clobber + D-01 | ✓ VERIFIED | `public static ValidateAssignmentUnitInUserUnits` `:52-62` (junction-only, active, empty→false, Trim+OrdinalIgnoreCase); 7 occurrences (def+6 sites); both resolvers AssignmentUnit-only; D-01 GET compute |
| `Controllers/AssessmentAdminController.cs` | cert-gate AssignmentUnit-only + persisted audit | ✓ VERIFIED | `:1414` resolver, `:1420` ProtonUnitUnresolved audit, `:1424` LogWarning, gateSkippedNotHundred++ intact |
| `Controllers/CDPController.cs` | 4 filter sites + 2 defensive resolvers | ✓ VERIFIED | `:491/:1599/:1611/:4264` axis swap; `:534/:1712` defensive drop-fallback; userUnits129 deleted (0) |
| `Controllers/ProtonDataController.cs` | BypassList axis + BypassSave TargetUnit validation | ✓ VERIFIED | `:1522` axis, `:1656` org-tree, `:1662` ∈UserUnits, non-empty `:1644` retained |
| `Views/Admin/CoachCoacheeMapping.cshtml` | D-01 orphan alert | ✓ VERIFIED | `:99-107` alert-warning, auto-encoded `@orphanCount` (no Html.Raw/XSS), btn-close dismiss |
| 7 xUnit test files | PSU-01/02/03/04/05/07 decision primitives | ✓ VERIFIED | All present; 19 passed / 0 failed / 2 skipped (Phase-404 deferrals) |

### Key Link Verification

| From | To  | Via | Status | Details |
| ---- | --- | --- | ------ | ------- |
| Assign/Edit/Import/Cleanup/Reactivate | ValidateAssignmentUnitInUserUnits | ∈UserUnits after org-tree, before mutation/tx | ✓ WIRED | 6 call sites in CoachMappingController; Edit `:793` before tx `:805` (Pitfall 4) |
| ProtonDataController.BypassSave | CoachMappingController.ValidateAssignmentUnitInUserUnits | TargetUnit server-side check before service write | ✓ WIRED | `:1662` before `BypassSaveAsync :1670` |
| GetEligibleCoachees / cert-gate | _auditLog.LogAsync(ProtonUnitUnresolved) | gate-block persisted audit | ✓ WIRED | CoachMapping `:1505`, AssessmentAdmin `:1420` |
| AutoCreateProgress / CDP defensive | _logger.LogWarning | read-path skip (no persisted audit) | ✓ WIRED | `:1562`, `:537`, `:1733` — 0 `_auditLog` in read-path windows |
| CoachCoacheeMapping GET | ViewBag.OrphanUnitMappings | on-demand orphan query → view alert | ✓ WIRED | controller `:199` → view `:99` |

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
| -------- | ------------- | ------ | ------------------ | ------ |
| CoachCoacheeMapping view | `ViewBag.OrphanUnitMappings` (int count) | GET query over `_context.CoachCoacheeMappings` + `_context.UserUnits` (active) `:188-199` | ✓ Real DB query | ✓ FLOWING — live-verified PRESENT@orphan=1 / ABSENT@orphan=0 via Playwright |
| BypassList JSON | `rows` | join ProtonTrackAssignments×Users×ProtonTracks + AssignmentUnit subquery filter | ✓ Real DB query `:1511-1541` | ✓ FLOWING |
| CDP coachee scope | `coacheeUsers` / scoped ids | `unitByCoachee` dict + EXISTS subquery on active mappings | ✓ Real DB query | ✓ FLOWING |

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
| -------- | ------- | ------ | ------ |
| Project compiles | `dotnet build HcPortal.csproj -c Debug` | Build succeeded, 0 Error(s) | ✓ PASS |
| Phase 401 decision primitives | `dotnet test --filter "~AssignmentUnitInUserUnits|~ProtonUnitResolve|~CleanupNoClobber|~UnitUnresolvedAudit|~ReactivateUnitValidation|~CertGateAudit|~FilterAxis"` | Passed! 19 passed / 0 failed / 2 skipped | ✓ PASS |
| 0 migration added in phase 401 | `git log -- Migrations/` + `ls -t Migrations/` | Newest migration = AddUserUnitsTable (Phase 399, commit fc015f4d); no 401 migration | ✓ PASS |
| Helper is single-source (no nav-property) | grep `coachee.UserUnits` / `Include(...UserUnits)` | 0 (junction query only) | ✓ PASS |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ---------- | ----------- | ------ | -------- |
| PSU-01 | 401-01/02/04/05/06 | Resolve from AssignmentUnit, drop User.Unit fallback | ✓ SATISFIED | 0 `Select(u => u.Unit)`; 5 resolvers AssignmentUnit-only |
| PSU-02 | 401-05/06 | All filters use AssignmentUnit (BypassList, CDP) | ✓ SATISFIED | 0 `u.Unit == unit` / `x.u.Unit == unit`; 4 CDP + 1 BypassList axis swapped |
| PSU-03 | 401-01/03/06 | Validate ∈ UserUnits at Assign/Edit/Import + bypass TargetUnit + org-tree | ✓ SATISFIED | 8 helper call sites; bypass org-tree + UserUnits at `:1656/:1662` |
| PSU-04 | 401-03 | Cleanup + Import no-clobber (UserUnits-aware) | ✓ SATISFIED | Cleanup preserve-gate `:957`; Import-reactivate clobber removed `:410` |
| PSU-05 | 401-02/03/04/05 | 6 read-path skip + audit-warn on empty; gate no primary-cert | ✓ SATISFIED | Gate=persisted audit (2 sites); read-path=LogWarning-only (3 sites); D-01 indicator |
| PSU-07 | 401-03 | Reactivation no-clobber + validate ∈ UserUnits; AF-4 untouched | ✓ SATISFIED (a,b) | Reactivate guard `:1111`; Import-reactivate preserve `:399-415`; AF-4 count=2. PSU-07c deferred→Phase 404 |

All 6 declared requirement IDs accounted for. No orphaned requirements (REQUIREMENTS.md maps exactly PSU-01/02/03/04/05/07 to Phase 401; PSU-06 does not exist).

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| CDPController.cs | 1599/1611/4264 | `m.AssignmentUnit == unit` raw EF equality (no Trim/OrdinalIgnoreCase) | ⚠️ Warning (WR-01) | Filter-axis case/whitespace-sensitive vs helper normalization — false-negative if stored unit has whitespace or case mismatch under CS collation. Goal achieved (AssignmentUnit axis); edge-case data-dependent. Candidate for Phase 404 SQL-real assertion |
| CoachMappingController.cs | 962-964 | Cleanup preserve-branch counts `autoFixed++` even if section unresolvable | ⚠️ Warning (WR-03) | Misleading cleanup success count; no-clobber preserve behavior itself correct. Non-blocking |

No blocker (Critical) anti-patterns. WR-02 (Edit empty-unit orphan) from REVIEW was **resolved** by recent fix 58bb6b11 — Edit now rejects empty AssignmentUnit (helper returns false on empty).

### Human Verification Required

None blocking. The D-01 indicator render (the one Razor surface flagged per Phase 354 lesson) was already human-verified live via Playwright this session (snapshot→seed→restore, alert ABSENT@orphan=0 / PRESENT@orphan=1, screenshot captured). The broader live cross-surface multi-unit fixture sweep (SC #6) is deliberately deferred to Phase 404 SQL-real UAT and is not a Phase 401 gap.

### Gaps Summary

No gaps. All 6 ROADMAP success criteria and all 6 requirement IDs (PSU-01/02/03/04/05/07) are satisfied in the actual codebase:

- The `ValidateAssignmentUnitInUserUnits` shared helper is the single source (`CoachMappingController:52-62`), reads only active `UserUnits` via direct junction query (no `ApplicationUser` nav property), returns `false` on empty/whitespace (never resolves from primary), and normalizes with `Trim()` + `OrdinalIgnoreCase`.
- The ambiguous `User.Unit` fallback is fully removed (`Select(u => u.Unit)` = 0 across all controllers; `?? userUnits129.GetValueOrDefault` = 0).
- All write-paths (Assign/Edit/Import/Cleanup/Reactivate + bypass TargetUnit) validate ∈ active UserUnits before mutation/transaction, including the recent fix (58bb6b11) making Edit reject an empty AssignmentUnit.
- Read-paths skip + audit-warn (D-03 hybrid channel: gate = persisted `ProtonUnitUnresolved` AuditLog; read-path = LogWarning-only).
- Cleanup and Import-reactivate are no-clobber (preserve valid non-primary unit).
- AF-4 ±5s correlation window (`EF.Functions.DateDiffSecond`) is exactly 2 occurrences — untouched.
- The D-01 orphan-unit indicator computes on-demand and renders a Bootstrap 5 alert-warning, human-verified live.
- 0 migration added in Phase 401 (newest migration is Phase 399 `AddUserUnitsTable`). Build succeeds 0 errors; 19/0/2 targeted tests pass.

Two non-blocking REVIEW warnings (WR-01 filter-axis normalization, WR-03 cleanup count cosmetics) remain as quality-polish candidates; neither defeats the phase goal and both fall naturally under Phase 404's SQL-real integration coverage (QA-04). The Phase-404 deferrals (deep SQL-real single-active, broad live cross-surface fixture, PSU-07c) are intentional and documented, not gaps.

---

_Verified: 2026-06-19T10:30:00Z_
_Verifier: Claude (gsd-verifier)_
