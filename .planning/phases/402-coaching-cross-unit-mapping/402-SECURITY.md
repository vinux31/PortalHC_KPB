---
phase: 402-coaching-cross-unit-mapping
audited: 2026-06-19
asvs_level: L1
threats_total: 13
threats_closed: 13
threats_open: 0
verdict: SECURED
---

# Phase 402 Security Audit

**Phase:** 402 — coaching-cross-unit-mapping
**Branch:** ITHandoff
**ASVS Level:** L1
**Auditor:** gsd-security-auditor
**Date:** 2026-06-19

## Threat Verification

| Threat ID  | Category             | Disposition      | Status | Evidence |
|------------|----------------------|------------------|--------|----------|
| T-402-01   | Tampering            | mitigate         | CLOSED | `CoachMappingController.cs:579-588` — every map entry (req.AssignmentUnits[cid] ?? req.AssignmentUnit) validated via org-tree membership + `ValidateAssignmentUnitInUserUnits` per-coachee inside `CoachCoacheeMappingAssign`; no path bypasses the loop. |
| T-402-02   | Repudiation          | accept           | CLOSED | `HcPortal.Tests/CrossUnitAssignTests.cs:92-94` — `[Fact(Skip = "deferred to Phase 404 QA-03 — SQL-real single-active invariant")]` stub present, documented, not silently omitted. |
| T-402-03   | Elevation/Tampering  | mitigate         | CLOSED | `CoachMappingController.cs:557-570` — server loads `coach.Section` authoritatively; rejects if `AssignmentSection != coach.Section` (OrdinalIgnoreCase, line 561); then loops all coacheeIds via `CoacheeSectionMatchesCoach` (line 567), returning 400-friendly on any cross-Bagian match. Client filter is UX-only. |
| T-402-04   | Tampering            | mitigate         | CLOSED | `CoachMappingController.cs:574-588` — `ValidateAssignmentUnitInUserUnits(_context, cid, unit)` called per-coachee (line 586); also checked against `validUnits` (org-tree, line 585). Rejects unit not in coachee.UserUnits active set. |
| T-402-05   | Tampering            | mitigate         | CLOSED | `CoachMappingController.cs:561` — `!string.Equals(req.AssignmentSection!.Trim(), coach.Section.Trim(), StringComparison.OrdinalIgnoreCase)` rejects any spoofed AssignmentSection. Mapping creation at line 693 uses `coach.Section!.Trim()` directly (not the client-supplied field). |
| T-402-06   | Tampering (CSRF)     | mitigate         | CLOSED | `CoachMappingController.cs:539` — `[ValidateAntiForgeryToken]` on `CoachCoacheeMappingAssign`. `Views/Admin/CoachCoacheeMapping.cshtml:826` — JS `submitAssign()` sends `RequestVerificationToken` header from the hidden field rendered by `@Html.AntiForgeryToken()` at line 30. |
| T-402-07   | Info Disclosure      | accept           | CLOSED | `CoachMappingController.cs:767,774,777` — `DbUpdateException` detail logged only via `_logger.LogWarning(dbEx, ...)` / `_logger.LogError(ex, ...)`; JSON response returns friendly strings only ("Coachee sudah memiliki coach aktif…" / "Gagal menyimpan assignment. Operasi dibatalkan."). No raw dbEx.Message exposed. |
| T-402-08   | Info Disclosure      | mitigate         | CLOSED | `CDPController.cs:304-318` (FilterCoachingProton) and `:339-352` (ExportDashboardProgress) — Section locked to `user.Section`; operator-supplied unit validated against coach's own `UserUnits` (query junction), else coerced to null (union); no foreign Bagian or foreign-unit coachee data exposed. |
| T-402-09   | Info Disclosure      | mitigate         | CLOSED | `CDPController.cs:688-692` — `availableUnits` for coaching-role = `_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive)`, NOT `GetUnitsForSectionAsync`. Dropdown cannot expose units the coach is not a member of. |
| T-402-10   | Tampering            | accept (guarded) | CLOSED | `CDPController.cs:519-531` — post-filter (PSU-02) still uses `m.AssignmentUnit` from active `CoachCoacheeMappings` table, not scalar `User.Unit`. 402-03-SUMMARY.md confirms diff hunks only at lines 305/326/647/655/681 — no change inside 465-545. Phase 401 PSU-02 regression guard intact. |
| T-402-11   | Tampering            | mitigate         | CLOSED | Server-side guards (T-402-03/04) are authoritative. `CoachMappingController.cs:557-588` validates cross-Bagian and per-coachee unit on every POST regardless of client-supplied map. No server trust placed in client filter (UX-only). |
| T-402-12   | Tampering (CSRF)     | mitigate         | CLOSED | Same evidence as T-402-06. `[ValidateAntiForgeryToken]` at line 539; `RequestVerificationToken` header in `submitAssign` at `CoachCoacheeMapping.cshtml:826`. (T-402-12 is a dup of T-402-06 with the same mitigation pattern — both CLOSED.) |
| T-402-13   | Info Disclosure      | accept           | CLOSED | `Views/Admin/CoachCoacheeMapping.cshtml:447` — `data-units` rendered via `Html.Raw(JsonSerializer.Serialize(cUnits))` (JSON-encoded, HTML-attribute safe; System.Text.Json default encoder escapes `<`/`>`/`&`/`'`). Modal page is under `AdminBaseController` [Authorize] + action-level `[Authorize(Roles = "Admin, HC")]` on all write paths. Non-sensitive org unit names already visible in other admin views. Accept basis confirmed. |

## Accepted Risks Log

| Risk ID   | Threat ID  | Basis |
|-----------|------------|-------|
| AR-402-01 | T-402-02   | InMemory test provider does not enforce filtered unique indexes. Single-active SQL invariant (`IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`) is guaranteed by the real DB. Deferred to Phase 404 QA-03 integration test against SQL Server. Skip stub present in `CrossUnitAssignTests.cs:93`. |
| AR-402-02 | T-402-07   | Raw `DbException` details are already logger-only (no change from prior phases). No new raw-exception exposure surface introduced by Phase 402. |
| AR-402-03 | T-402-10   | Phase 401 PSU-02 post-filter regression is a test-observable regression guard, not a runtime-exploitable vulnerability. Git diff confirms 465-545 untouched. |
| AR-402-04 | T-402-13   | `data-units` JSON in HTML attribute exposes coachee unit membership list. Audience is Admin/HC only (authenticated + role-checked). Unit names are non-sensitive org data already visible elsewhere in the admin surface. System.Text.Json encoding prevents XSS breakout. |

## Unregistered Flags

No unregistered threat flags were identified in SUMMARY.md `## Threat Flags` sections for plans 402-01 through 402-04. All threat flags map to registered T-402-xx IDs.

## Notes

- All 4 code-review warnings (WR-01..WR-04) were fixed before this audit (commits 1667d7b0, c2c2010c, acdf4fa2). The WR-02 fix (`req.CoacheeIds = req.CoacheeIds.Distinct().ToList()` at line 548) additionally closes a secondary attack surface (crafted duplicate-coacheeId payload) which would have caused an unintended batch rollback — not a data-corruption risk, but a denial-of-service against a single assign batch.
- `CoachCoacheeMapping` GET action inherits bare `[Authorize]` from `AdminBaseController`; all data-mutating POST actions carry explicit `[Authorize(Roles = "Admin, HC")]`. The modal's `data-units` rendering is in the GET view, adequately protected.
- T-402-05: Mapping creation writes `coach.Section!.Trim()` (line 693), not the client's `req.AssignmentSection` — so even if the section-equality check were bypassed, the stored value would still be server-authoritative.
