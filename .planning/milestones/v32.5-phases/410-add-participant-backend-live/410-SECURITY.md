---
phase: 410-add-participant-backend-live
milestone: v32.5
secured_at: 2026-06-21
asvs_level: 1
block_on: HIGH
threats_total: 13
threats_closed: 13
threats_open: 0
status: SECURED
---

# Phase 410: Security Verification Report

**Phase:** 410 — Add-Participant Backend Live (AJAX)
**Milestone:** v32.5 (Flexible Add/Remove Peserta)
**ASVS Level:** 1
**Block-on:** HIGH severity
**Verified:** 2026-06-21
**Method:** Disposition-driven verification of the `<threat_model>` registers in `410-01-PLAN.md` (T-410-01..10) and `410-02-PLAN.md` (T-410T-01..03) against the implemented code in `Controllers/AssessmentAdminController.cs` and `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs`. Implementation files read-only.

## Summary

Phase 410 adds two AJAX endpoints (`AddParticipantsLive` POST, `GetEligibleParticipantsToAdd` GET) plus two private helpers (`BuildReadyParticipantSession`, `CreateEagerAssignmentsAsync`). Every `mitigate` threat in the PLAN threat models was confirmed present in code at a specific file:line. Every `accept` threat was confirmed to be genuinely low-risk and explicitly documented (CONTEXT D-01/D-02/D-04 + REVIEW IN-02/IN-03), not a silent gap. The test plan threats (DB isolation + anti-tautology) are also satisfied.

**No HIGH-severity threat is unmitigated.** `threats_open: 0`. Phase passes the HIGH block-on gate.

Defense in depth on authorization: the base class `AdminBaseController` carries class-level `[Authorize]` (`Controllers/AdminBaseController.cs:12`), and both Phase 410 endpoints additionally carry `[Authorize(Roles = "Admin, HC")]`.

## Threat Verification — Production (410-01-PLAN.md)

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-410-01 | Spoofing/Tampering (CSRF) | mitigate | CLOSED | `[ValidateAntiForgeryToken]` on `AddParticipantsLive` — `AssessmentAdminController.cs:2355`. (GET picker is a safe read; no token required.) |
| T-410-02 | Elevation (authz) | mitigate | CLOSED | `[Authorize(Roles = "Admin, HC")]` on BOTH endpoints — GET `:2293`, POST `:2354`. Reinforced by base-class `[Authorize]` (`AdminBaseController.cs:12`). |
| T-410-03 | Tampering (batch IDOR via forged batchKey/LinkedGroupId) | mitigate | CLOSED | Server-authoritative: `rep` resolved from `sessionId` (`:2297`, `:2364`); batch-key derived server-side as `Title+Category+Schedule.Date` (`:2303`, `:2379`); Pre/Post reps re-resolved server-side (`:2410-2415`); `linkedGroupId = rep.LinkedGroupId!.Value` from server `rep` (`:2428`). No client `batchKey`/`LinkedGroupId` is trusted. |
| T-410-04 | Tampering (corrupt Proton state) | mitigate | CLOSED | `if (rep.Category == "Assessment Proton") return BadRequest(...)` fires BEFORE any write — `:2368-2369` (Langkah 2, before tx). |
| T-410-05 | Tampering (window bypass) | mitigate | CLOSED | Server-authoritative `if (rep.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > rep.ExamWindowCloseDate.Value) return BadRequest("Window ujian sudah tutup, tidak bisa tambah peserta.")` — `:2372-2373` (WIB = UTC+7). |
| T-410-06 | Tampering (mass-assignment / over-posting) | mitigate | CLOSED | Action binds primitives only: `AddParticipantsLive(int sessionId, List<string> userIds)` — `:2356`. No `AssessmentSession`/ViewModel model-binding. All session fields set server-side from `rep` via `BuildReadyParticipantSession` (`:2322-2346`), not from the client. |
| T-410-07 | DoS (mass-add) | mitigate | CLOSED | Cap-50: `if (userIds.Count > 50) return BadRequest("Maksimal 50 peserta per permintaan.")` — `:2361-2362`. Null/empty guard at `:2359-2360`. |
| T-410-08 | Information Disclosure (IDOR — assign arbitrary user) | accept | CLOSED | Accept verified + documented (CONTEXT D-02 `:25-28`; REVIEW IN-02). Not an IDOR by design — eligibility is an explicit user decision ("semua pekerja IsActive"). Still validates `IsActive` (`:2310`) + user-exists (`:2387-2392`). Idempotency (`:2378-2384`) prevents duplicate-session harm. Low risk. |
| T-410-09 | Tampering (stale-inherit from removed rep) | accept | CLOSED | Accept verified + documented (PLAN T-410-09; REVIEW IN-03). Config fields don't change on soft-remove, so inherited config stays valid. Optional hardening deferred. Low risk. |
| T-410-10 | Information Disclosure (XSS on name/NIP in JSON) | accept | CLOSED | Accept verified. 410 emits JSON only (`:2494-2500`), never renders DOM. DOM rendering is Phase 412 (carry `.textContent` per T-409-10). Not a 410 surface. |

## Threat Verification — Tests (410-02-PLAN.md)

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-410T-01 | Tampering (test corrupts HcPortalDB_Dev) | mitigate | CLOSED | Write-path uses disposable `FlexibleParticipantAddFixture` → `HcPortalDB_Test_{guid}` with `EnsureDeletedAsync` teardown (REVIEW-FIX confirms "No Dev/Prod DB touched"). HcPortalDB_Dev untouched. |
| T-410T-02 | Repudiation (tautological tests → false confidence, lesson 999.12) | mitigate | CLOSED | Read-path drives REAL `GetEligibleParticipantsToAdd` (`FlexibleParticipantAddLiveTests.cs:109,124,150,172,186`); write-path drives REAL `AddParticipantsLive` against SQLEXPRESS (`:346,404,448,486,539,598`) asserting real columns. No replica predicate (`WindowAllowsAddition` absent). |
| T-410T-03 | Information Disclosure (SQLEXPRESS credential leak) | accept | CLOSED | Integrated Security (Windows auth, local) — no secret literal; same posture as existing fixture. Low risk. |

## Defense-in-Depth Notes (beyond declared register)

- **Atomic integrity / no partial writes:** all session + UPA creation wrapped in `using var transaction = await _context.Database.BeginTransactionAsync()` with `await transaction.RollbackAsync()` on any exception (`:2420-2466`). Eager UPA runs inside the tx (`:2453`); rollback verified by test T7 design (0-write on reject) and atomic intent.
- **Non-repudiation (audit):** `await _auditLog.LogAsync(actorId, actorName, "AddParticipantLive", ...)` written INSIDE the tx, before `CommitAsync` (`:2456-2459`) — audit and data commit atomically (audit cannot survive a rolled-back write, and a committed write always carries its audit).
- **Idempotency:** duplicate-session guard (`:2378-2384`) — users already in the batch (any `RemovedAt` state) are skipped, not re-created.
- **Side-effect ordering:** notifications fire only POST-commit (`:2469-2478`), so a rolled-back tx emits no notification (spec §G).

## Verdict

All 10 production threats (T-410-01..10) and 3 test threats (T-410T-01..03) are accounted for: 8 `mitigate` confirmed in code at file:line, 5 `accept` confirmed low-risk and explicitly documented. No HIGH-severity threat is left unmitigated.

**threats_open: 0**

---

_Secured: 2026-06-21_
_Auditor: Claude (gsd-security-auditor)_
_ASVS Level 1 · block-on HIGH_
