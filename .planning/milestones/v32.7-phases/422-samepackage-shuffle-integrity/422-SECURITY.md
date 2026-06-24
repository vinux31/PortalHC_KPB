---
phase: 422-samepackage-shuffle-integrity
secured_at: 2026-06-23T14:10:00Z
asvs_level: 1
block_on: high
threats_total: 13
threats_closed: 13
threats_open: 0
unregistered_flags: 0
status: secured
verdict: SECURED
---

# Phase 422: Security Verification — SamePackage & Shuffle Integrity

**Verified:** 2026-06-23
**ASVS Level:** 1
**Block on:** high severity
**Verdict:** SECURED — 13/13 threats closed, 0 open

Verification of threat mitigations declared across `422-01-PLAN.md` (T-422-01..04),
`422-02-PLAN.md` (T-422-05..08), and `422-03-PLAN.md` (T-422-09..13). Each `mitigate`
disposition was confirmed present in implemented code by file:line evidence. Implementation
files were read-only; no code was modified during this audit.

Context: code review (`422-REVIEW.md`) spot-checked the security posture (0 critical, 2 warnings,
3 info — all resolved in `422-REVIEW-FIX.md` iteration 1, including the pre-existing IN-01 CSRF gap
on `CreateQuestion`). Backend covered by 57 xUnit tests (green) + UAT 6/6 PASS. This audit confirms
mitigations are present in source, not re-testing them.

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-422-01 | Tampering | mitigate | CLOSED | `Migrations/20260623103224_AddPackageNumberUniqueIndex.cs:17-27` — `migrationBuilder.Sql(@"...")` is a static literal (ROW_NUMBER dedup), no user input / no string interpolation. Injection-safe. |
| T-422-02 | Tampering | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:6142-6151` — PackageNumber server-computed via `MaxAsync()` (`(maxNumber ?? 0) + 1`), never client-supplied. DB-level defense: `Data/ApplicationDbContext.cs:371-373` unique index `IX_AssessmentPackages_SessionId_PackageNumber_Unique`. |
| T-422-03 | Denial of Service | mitigate | CLOSED | `Migrations/...AddPackageNumberUniqueIndex.cs:17-34` — dedup ROW_NUMBER (STEP 1) runs BEFORE `CreateIndex` (STEP 2), so CreateIndex cannot fail on legacy duplicates. Idempotent re-run. `Down()` (`:38-44`) is non-destructive (DropIndex only). EF wraps `Up()` in one migration transaction = atomic. |
| T-422-04 | Information Disclosure | accept | CLOSED | Helpers are EF-free, return only counts/booleans, no sensitive data. `Helpers/PackageSizeAnalysis.cs:28-34` (Compute → counts) + `Helpers/ShuffleToggleRules.cs:11-27` (booleans). Low-value, accepted by design in plan. No SECURITY.md accepted-risks entry required beyond this disposition. |
| T-422-05 | EoP / Tampering | mitigate | CLOSED | Server-authoritative lock `SessionEditLockRules.IsSessionEditLocked` (`Helpers/SessionEditLockRules.cs:21-22`) guards 5 POST endpoints at top before any write: CreatePackage `:6134`, DeletePackage `:6180`, CreateQuestion `:6855`, EditQuestion `:7075`, DeleteQuestion `:7286`. Hard-reject TempData + redirect (no-write). View disable is UX-only. |
| T-422-06 | Tampering | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:6397-6402` — `IsSessionEditLocked` guard at top of `ImportPackageQuestions` rejects Import directly into a locked Post (Pitfall 3 guard-ordering). Import to Pre passes; Pre→Post sync runs at success-path end `:6745`. |
| T-422-07 | Tampering (IDOR) | mitigate | CLOSED | Session resolved server-side via `FindAsync` per endpoint; class-level `[Authorize]` on `AdminBaseController` + method-level `[Authorize(Roles="Admin, HC")]` on every mutating endpoint (CreatePackage `:6119`, DeletePackage `:6165`, Import `:6365`, CreateQuestion `:6813`, EditQuestion `:7028`, DeleteQuestion `:7272`). Admin/HC global scope by design. |
| T-422-08 | Information Disclosure | accept | CLOSED | Sibling type-aware predicate filters sessions by Title/Category/Date/Type only — exposes no new data. `Helpers/SiblingSessionQuery` reused for lock-detection; propagation foreach left unchanged (cross-type by design). Low-value, accepted. |
| T-422-09 | Tampering / CSRF | mitigate | CLOSED | `ToggleSamePackage` has `[HttpPost]` + `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (`Controllers/AssessmentAdminController.cs:6019-6021`); form emits `@Html.AntiForgeryToken()` (`Views/Admin/ManagePackages.cshtml:92`). Mirrors UpdateShuffleSettings. |
| T-422-10 | Tampering | mitigate | CLOSED | Server-side `anyStarted` guard via shared `AnyStartedInPairAsync` (`Controllers/AssessmentAdminController.cs:5904-5911`) called from POST guard `:6037` AND GET ViewBag `:5879` (IN-03 kill-drift — UX-disable and server-reject can't diverge). Reject TempData + no-write `:6038-6042`. Server-authoritative, not just view switch-disabled. |
| T-422-11 | Tampering (mass-assignment) | mitigate | CLOSED | `Controllers/AssessmentAdminController.cs:6022` — endpoint binds explicit scalars `(int assessmentId, bool samePackage)`, server sets `post.SamePackage` (`:6055`); no full-entity model-bind. |
| T-422-12 | Information Disclosure (XSS) | mitigate | CLOSED | All new warning/toast/lock-banner strings are static Razor literals (auto-encoded): `Views/Admin/ManagePackages.cshtml:98,101,151-166`. Confirm-before JS uses single-quote string literals, wrapped in DOMContentLoaded, no interpolated user data (`:466-483`). Live JS interpolations (`:493-494`) are server-side bool/int only, not user strings. |
| T-422-13 | Tampering (dangling UPA) | mitigate | CLOSED | ON-path wrapped in explicit EF transaction (WR-01 fix) `:6052-6066`. Stale Post `UserPackageAssignment` cleanup centralized in `SyncPackagesToPost` (`:5931-5939`) before `RemoveRange`, covering all 7 sync callers (WR-02 fix) — defends the `OnDelete(Restrict)` FK at `Data/ApplicationDbContext.cs:543-546`. `anyStarted` guard further reduces exposure. |

## Code Review Findings Cross-Check (422-REVIEW.md / 422-REVIEW-FIX.md)

| Finding | Severity | Status | Confirmed in code |
|---------|----------|--------|-------------------|
| WR-01 | Warning | FIXED | ON-path explicit transaction `AssessmentAdminController.cs:6052-6066` (try/commit/catch-rollback-rethrow). |
| WR-02 | Warning | FIXED | Centralized stale-Post-UPA cleanup `SyncPackagesToPost:5931-5939` (all 7 callers covered). |
| IN-01 (pre-existing CSRF gap) | Info | FIXED | `TruncateAlt` relocated above attribute trio (`:6809-6810`); `CreateQuestion` now carries `[HttpPost]/[Authorize(Roles="Admin, HC")]/[ValidateAntiForgeryToken]` (`:6812-6815`). CSRF + role + verb restored at method level. |
| IN-03 | Info | FIXED | Shared `AnyStartedInPairAsync:5904-5911` used by both GET ViewBag and POST guard. |
| IN-02 | Info | SKIPPED (by design) | Harmless dead-defensive `Questions != null && Any()` branch; review rated "None required". No security impact. |

## Unregistered Flags

None. No `## Threat Flags` section present in `422-01/02/03-SUMMARY.md` — executors flagged no new
attack surface beyond the registered T-422-01..13.

## Accepted Risks Log

- **T-422-04** (Information Disclosure — pure helpers): accepted by design. Helpers are EF-free and
  return only counts/booleans; no sensitive data exposed. Low-value.
- **T-422-08** (Information Disclosure — sibling type-aware predicate): accepted by design. Predicate
  filters by Title/Category/Date/Type metadata only; exposes no new data.

## Verdict

**SECURED** — All 13 registered threats (T-422-01..13) are CLOSED: 11 `mitigate` confirmed present
in code by file:line, 2 `accept` confirmed as documented low-value risks. The pre-existing IN-01
CSRF gap on `CreateQuestion` was closed this phase. No high-severity gaps. `block_on: high` not
triggered.

**threats_open: 0**

---

_Audited by: Claude (gsd security auditor)_
_ASVS Level 1 — verification by declared disposition_
