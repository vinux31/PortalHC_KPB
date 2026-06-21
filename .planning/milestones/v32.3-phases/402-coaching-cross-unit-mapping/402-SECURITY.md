---
phase: 402-coaching-cross-unit-mapping
audited: 2026-06-21
asvs_level: L1
threats_total: 13
threats_closed: 13
threats_open: 0
verdict: SECURED
re_audit: true
re_audit_reason: "code-review re-trigger (dbe2c794) extracted two previously-inline mitigations in CDPController.cs into pure static seams (CoerceCoachUnitScope + CoacheeMatchesUnitScope). Re-audit confirms behavior-identical refactor; all 13 threats remain CLOSED at updated line numbers."
---

# Phase 402 Security Audit (Re-Audit 2026-06-21)

**Phase:** 402 — coaching-cross-unit-mapping
**Branch:** ITHandoff
**ASVS Level:** L1
**Auditor:** gsd-security-auditor
**Prior audit:** 2026-06-19 (SECURED 13/13)
**Re-audit trigger:** `/code-review 402` re-trigger committed `dbe2c794`/`ae3af872` — extracted inline
mitigations into pure static seams (`CoerceCoachUnitScope`, `CoacheeMatchesUnitScope`) in
`CDPController.cs` and added unit-test coverage. Line numbers from the prior audit are now stale
for T-402-08/09/10; this document supersedes them with verified current locations.

## Threat Verification

| Threat ID  | Category             | Disposition      | Status | Evidence |
|------------|----------------------|------------------|--------|----------|
| T-402-01   | Tampering            | mitigate         | CLOSED | `CoachMappingController.cs:579-601` — every map entry `(req.AssignmentUnits[cid] ?? req.AssignmentUnit)` validated via org-tree membership + `ValidateAssignmentUnitInUserUnits` per-coachee; no path bypasses the loop. |
| T-402-02   | Repudiation          | accept           | CLOSED | `HcPortal.Tests/CrossUnitAssignTests.cs:106-107` — `[Fact(Skip = "deferred to Phase 404 QA-03 — SQL-real single-active invariant")]` stub present, documented, not silently omitted. |
| T-402-03   | Elevation/Tampering  | mitigate         | CLOSED | `CoachMappingController.cs:570-583` — server loads `coach.Section` authoritatively; rejects if `AssignmentSection != coach.Section` (OrdinalIgnoreCase, line 574); then loops all coacheeIds via `CoacheeSectionMatchesCoach` (line 580), returning 400-friendly on any cross-Bagian match. Client filter is UX-only. |
| T-402-04   | Tampering            | mitigate         | CLOSED | `CoachMappingController.cs:591-601` — `ValidateAssignmentUnitInUserUnits(_context, cid, unit)` called per-coachee (line 599); also checked against `validUnits` (org-tree, line 597). Rejects unit not in coachee.UserUnits active set. |
| T-402-05   | Tampering            | mitigate         | CLOSED | `CoachMappingController.cs:574` — `!string.Equals(req.AssignmentSection!.Trim(), coach.Section.Trim(), StringComparison.OrdinalIgnoreCase)` rejects any spoofed AssignmentSection. Mapping creation at line 706 uses `coach.Section!.Trim()` directly (not the client-supplied field). |
| T-402-06   | Tampering (CSRF)     | mitigate         | CLOSED | `CoachMappingController.cs:552` — `[ValidateAntiForgeryToken]` on `CoachCoacheeMappingAssign`. `Views/Admin/CoachCoacheeMapping.cshtml:30` — `@Html.AntiForgeryToken()`; JS `submitAssign()` at line 826 sends `RequestVerificationToken` header from the hidden field. |
| T-402-07   | Info Disclosure      | accept           | CLOSED | `CoachMappingController.cs:780,787,790` — `DbUpdateException` detail logged only via `_logger.LogWarning(dbEx, ...)` / `_logger.LogError(ex, ...)`; JSON response returns friendly strings only ("Coachee sudah memiliki coach aktif…" / "Gagal menyimpan assignment. Operasi dibatalkan."). No raw `dbEx.Message` exposed. |
| T-402-08   | Info Disclosure      | mitigate         | CLOSED | **SEAM REFACTORED — evidence updated.** `CDPController.cs:297-303` — `CoerceCoachUnitScope(IEnumerable<string> coachActiveUnits, string? requestedUnit)`: blank → null (union); owned unit → returned unchanged; foreign unit → null (union, no cross-coach leak). Called at `:341` (FilterCoachingProton) and `:374` (ExportDashboardProgress). `coachActiveUnits` loaded from `_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive).Select(uu => uu.Unit)` (authoritative junction). Section locked to `user.Section` at both call sites (`:334`/`:367`). No foreign-Bagian or foreign-unit coachee data exposed. |
| T-402-09   | Info Disclosure      | mitigate         | CLOSED | **LINE NUMBERS UPDATED.** `CDPController.cs:711-716` — `availableUnits` for coaching-role = `_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive).Select(uu => uu.Unit).Distinct()` (NOT `GetUnitsForSectionAsync`). Override happens inside `if (UserRoles.IsCoachingRole(roleLevel))` block; the broader `GetUnitsForSectionAsync` result (line 705) is replaced before it reaches the view. Dropdown cannot expose units the coach is not a member of. |
| T-402-10   | Tampering            | accept (guarded) | CLOSED | **SEAM REFACTORED — evidence updated.** `CDPController.cs:311-316` — `CoacheeMatchesUnitScope(string? coacheeAssignmentUnit, string? scopeUnit)`: null/blank scope → true (union); else OrdinalIgnoreCase+Trim. Called at `:554` in the post-filter: `coacheeUsers.Where(u => CoacheeMatchesUnitScope(unitByCoachee.GetValueOrDefault(u.Id, ""), unit))`. `unitByCoachee` is projected from `m.AssignmentUnit` sourced from `_context.CoachCoacheeMappings` (active mappings, line 548-552) — NOT scalar `User.Unit`. Phase 401 PSU-02 axis preserved: the refactor introduced a seam over the same AssignmentUnit-axis logic; the query feeding it (line 548-552) is unchanged. |
| T-402-11   | Tampering            | mitigate         | CLOSED | Server-side guards (T-402-03/04) are authoritative. `CoachMappingController.cs:570-601` validates cross-Bagian and per-coachee unit on every POST regardless of client-supplied map. No server trust placed in client filter (UX-only). |
| T-402-12   | Tampering (CSRF)     | mitigate         | CLOSED | Same evidence as T-402-06. `[ValidateAntiForgeryToken]` at `CoachMappingController.cs:552`; `RequestVerificationToken` header in `submitAssign` at `CoachCoacheeMapping.cshtml:826`. |
| T-402-13   | Info Disclosure      | accept           | CLOSED | `Views/Admin/CoachCoacheeMapping.cshtml:447` — `data-units` rendered via `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(cUnits))` (JSON-encoded, HTML-attribute safe; System.Text.Json default encoder escapes `<`/`>`/`&`/`'`). Modal page is under `AdminBaseController` [Authorize] + action-level `[Authorize(Roles = "Admin, HC")]` on all write paths. Non-sensitive org unit names already visible in other admin views. Accept basis confirmed. |

## Seam Refactor Analysis

The `/code-review 402` re-trigger extracted two previously-inline LINQ/comparison blocks into
named pure static methods. This audit confirms the refactor is behavior-identical:

### CoerceCoachUnitScope (CDPController.cs:297-303)
- **Behavior:** blank requested → null; owned unit (OrdinalIgnoreCase+Trim match against caller-supplied
  `coachActiveUnits`) → return unchanged; foreign unit → null.
- **Caller-supplied authority:** callers at `:338-341` and `:371-374` load `coachActiveUnits` from
  `_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive).Select(uu => uu.Unit)` —
  same authoritative junction used pre-refactor. The coercion is not weakened.
- **Test coverage:** `CdpCoachUnionScopeTests.cs` — `Coerce_blank_request_yields_null` (Theory 3
  values), `Coerce_owned_unit_is_kept_case_insensitive`, `Coerce_foreign_unit_yields_null_no_leak`,
  `Coerce_empty_coach_units_yields_null` — 7 assertions covering the three branches.

### CoacheeMatchesUnitScope (CDPController.cs:311-316)
- **Behavior:** null/blank scopeUnit → true (union); else OrdinalIgnoreCase+Trim match against
  coacheeAssignmentUnit.
- **PSU-02 axis:** The query at `:548-552` feeding `unitByCoachee` sources `m.AssignmentUnit` from
  active `CoachCoacheeMappings` — NOT `User.Unit` scalar. This is identical to the pre-refactor
  logic; the seam wraps the same axis.
- **Test coverage:** `CdpCoachUnionScopeTests.cs` — `Match_null_scope_is_union_always_in_scope`
  (Theory 3 values, 3 coachee variants), `Match_narrow_is_case_insensitive_and_trim_tolerant`
  (5 assertions including the code-review gap: `"unity"` vs `"UnitY"` case-fold now explicitly
  tested), plus 3 end-to-end composed facts (`Coach_union_when_unit_null_returns_all_mapped_coachees`,
  `Coach_narrows_when_owned_unit_specified`, `Foreign_unit_coerced_to_null_yields_union`).

## Accepted Risks Log

| Risk ID   | Threat ID  | Basis |
|-----------|------------|-------|
| AR-402-01 | T-402-02   | InMemory test provider does not enforce filtered unique indexes. Single-active SQL invariant (`IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`) is guaranteed by the real DB. Deferred to Phase 404 QA-03 integration test against SQL Server. Skip stub present in `CrossUnitAssignTests.cs:106-107`. |
| AR-402-02 | T-402-07   | Raw `DbException` details are already logger-only (no change from prior phases). No new raw-exception exposure surface introduced by Phase 402. |
| AR-402-03 | T-402-10   | Phase 401 PSU-02 post-filter regression is a test-observable regression guard, not a runtime-exploitable vulnerability. Seam refactor preserves the AssignmentUnit-axis query source (lines 548-552 unchanged). |
| AR-402-04 | T-402-13   | `data-units` JSON in HTML attribute exposes coachee unit membership list. Audience is Admin/HC only (authenticated + role-checked). Unit names are non-sensitive org data already visible elsewhere in the admin surface. System.Text.Json encoding prevents XSS breakout. |

## Unregistered Flags

No unregistered threat flags identified. All items in SUMMARY.md `## Threat Flags` sections for
plans 402-01 through 402-04 map to registered T-402-xx IDs. The code-review re-trigger findings
(CONFIRMED-1: post-filter coverage gap; CONFIRMED-2: ordering assertion) were addressed by adding
`FilterEligibleCoachees` seam + ordering guard in `CrossUnitAssignTests.cs` — these are test
improvements, not new attack surfaces, and are informational only (no threat ID required).

## Notes

- Prior audit evidence for T-402-08 (stale: CDPController.cs:304-318/339-352) and T-402-09
  (stale: CDPController.cs:688-692) and T-402-10 (stale: CDPController.cs:519-531) is superseded
  by this document.
- The WR-02 fix (`req.CoacheeIds = req.CoacheeIds.Distinct().ToList()` at line 561) noted in the
  prior audit remains present (line 561 in current file), closing the duplicate-coacheeId DoS
  surface. This line was not touched by the seam refactor.
- `CoachCoacheeMapping` GET action inherits bare `[Authorize]` from `AdminBaseController`; all
  data-mutating POST actions carry explicit `[Authorize(Roles = "Admin, HC")]`. The modal's
  `data-units` rendering is in the GET view, adequately protected.
- T-402-05: Mapping creation writes `coach.Section!.Trim()` (line 706), not the client's
  `req.AssignmentSection` — so even if the section-equality check were bypassed, the stored value
  would still be server-authoritative.

## Security Audit 2026-06-21 (Re-Audit)
| Metric | Count |
|--------|-------|
| Threats found | 13 |
| Closed | 13 |
| Open | 0 |
