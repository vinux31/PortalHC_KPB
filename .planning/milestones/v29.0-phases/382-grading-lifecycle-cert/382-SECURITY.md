# Phase 382 — Grading / Lifecycle / Cert — Security Verification

**Phase:** 382 — grading-lifecycle-cert
**Verified:** 2026-06-15
**Verifier:** gsd-security-auditor
**ASVS Level:** 1
**Block-on:** high
**Threats:** 12 registered (11 mitigate + 1 accept) — **12/12 CLOSED**, threats_open: 0

Verifies that threat mitigations declared in the Phase 382 PLAN threat models exist in the
implemented code. Implementation files were treated READ-ONLY; this file is the only artifact created.

---

## Threat Verification (by disposition)

| Threat ID | Category | Disposition | Status | Evidence (file:line) |
|-----------|----------|-------------|--------|----------------------|
| T-382-01 | Tampering / Integrity (stale-answer score) | mitigate | CLOSED | `Services/GradingService.cs:87-90` `finalByQuestion = allResponses.Where(PackageOptionId.HasValue).GroupBy(PackageQuestionId).ToDictionary(g => g.OrderByDescending(r => r.SubmittedAt).First())`; consumed at L107 + L162 via `TryGetValue` (replaced `FirstOrDefault` no-ORDER-BY) |
| T-382-02 | Tampering / Elevation (resurrection) | mitigate | CLOSED | `Services/GradingService.cs:254-257` non-essay `ExecuteUpdateAsync` WHERE `Status != S.Completed && != S.Abandoned && != S.Cancelled && != S.PendingGrading`; `rowsAffected==0 → return false` (L266+) |
| T-382-03 | Business Logic (V11) | mitigate | CLOSED | `Services/GradingService.cs:214-217` essay branch WHERE adds `Status != S.Abandoned && != S.Cancelled` (terminal session cannot be set PendingGrading); `essayRowsAffected==0 → return false` (L227-233) |
| T-382-04 | Tampering / Elevation (resurrection via SubmitExam) | mitigate | CLOSED | `Controllers/CMPController.cs:1605-1612` early guard expanded to terminal set `{Completed,Abandoned,Cancelled,PendingGrading}` + `WriteSubmitBlockedAuditAsync` (audit) + redirect |
| T-382-05 | Tampering (overwrite graded via late abandon, TOCTOU) | mitigate | CLOSED | `Controllers/CMPController.cs:1283-1294` atomic `ExecuteUpdateAsync` WHERE `(Status == S.InProgress \|\| S.Open)` + `rowsAffected==0` reject branch (TOCTOU read-check-then-SaveChanges removed) |
| T-382-06 | Spoofing (abandon other worker's session) | mitigate | CLOSED | `Controllers/CMPController.cs:1284` WHERE includes `a.UserId == user.Id` inside the atomic ExecuteUpdate (ownership enforced in WHERE, not just early `Forbid()`) |
| T-382-07 | Tampering (timer bypass, Standard) | mitigate | CLOSED | `Controllers/CMPController.cs:4468-4473` `ShouldEnforceSubmitTimer` = blocklist (skip only `AssessmentType.Manual` / null/empty); applied at L4514 `if (!ShouldEnforceSubmitTimer(...)) return null;`. "Standard" now enforced (was dead-code allowlist) |
| T-382-08 | Tampering (spoofed isAutoSubmit) | mitigate | CLOSED | `Controllers/CMPController.cs:1619-1627` `serverTimerExpired` computed server-side; incomplete-gate is `if (!serverTimerExpired)` — raw client `isAutoSubmit` removed from gate condition (no longer sole bypass) |
| T-382-09 | Elevation (proctoring / token bypass) | mitigate | CLOSED | `ShouldGateMissingStart(IsTokenRequired, StartedAt)` (`CMPController.cs:4495-4496`) wired in SaveAnswer L373-374 (Json reject) + SubmitExam L1596-1599 (redirect) — token-required && StartedAt==null rejected |
| T-382-10 | DoS (token one-shot loss on retry) | mitigate | CLOSED | `Controllers/CMPController.cs:4546` `TempData.Keep(tempKey)` (peek, no pre-grading remove) + L1762-1764 `if (ShouldConsumeAutoSubmitToken(graded)) TempData.Remove(...)` — token consumed only on grading success |
| T-382-11 | Information / Integrity (misleading credential state) | mitigate | CLOSED | `Models/CertificationManagementViewModel.cs:59-60` `if (validUntil == null) return CertificateStatus.Aktif;` (flipped from `Expired`); Permanent branch L57-58 unchanged; single-source consumed by AdminBase/Renewal/CDP via Status enum |
| T-382-12 | Integrity (cross-surface drift) | accept | CLOSED | Accepted risk — see Accepted Risks Log below. Drift-check verified: no consumer independently re-derives status from raw ValidUntil for null certs |

**Score: 12/12 threats closed. threats_open: 0.**

---

## Accepted Risks Log

### AR-382-01 (T-382-12) — Cross-surface drift on cert status derivation

**Threat:** A consumer surface independently re-derives certificate status from raw `ValidUntil`
instead of consuming the single-source `DeriveCertificateStatus` enum, causing silent drift after the
CERT-01 flip (null → Aktif).

**Disposition:** ACCEPT — verified low residual risk, no HIGH severity.

**Rationale / verification evidence:**
- `DeriveCertificateStatus` is single-source; AdminBaseController worklist
  (`Status == Expired || Status == AkanExpired`), RenewalController tally, and CDPController tally all
  consume the `Status` enum — they auto-cohere with the helper flip with no edit. (Verified in
  382-03-SUMMARY + VERIFICATION key-link table; consumers not modified.)
- HomeController badge (`HomeController.cs:214-220`) and expiry notifications
  (`HomeController.cs:121-126`) filter `a.ValidUntil.HasValue` / `t.ValidUntil.HasValue` directly in the
  WHERE clause. A null-ValidUntil cert is structurally excluded before any `< today` comparison — it
  never reaches a raw re-derivation, so it cannot be miscounted as Expired/AkanExpired. This is the
  intended behavior (null cert = active/permanent, not part of expiry worklists).
- `CertAlertConsistencyTests` (4 facts) locks the consumer predicate-mirror (null-cert not counted as
  Expired/AkanExpired in tally + worklist). e2e #12 DB-asserts `IsPassed=1 + ValidUntil IS NULL` and CMP
  dashboard not labeled "Expired".

**Residual risk:** A FUTURE new consumer surface could re-derive status from raw `ValidUntil` and drift.
Mitigated by convention (single-source helper) + CertAlertConsistencyTests guard; not a current defect.

**Severity:** Info / Low. Below block-on threshold (high).

---

## Unregistered Flags

None. No `## Threat Flags` section present in any Phase 382 SUMMARY (382-01/02/03) — executors flagged
no new attack surface during implementation.

---

## Notes (non-blocking, informational)

The VERIFICATION report records 4 advisory anti-patterns (IN-01/IN-02/IN-03/WR-01). None map to a
registered threat and none are security defects:
- IN-01 `GradingService.cs:475` `RegradeAfterEditAsync` literal `"Completed"` (out of phase scope; value
  identical to `S.Completed`).
- IN-02 `elapsed >= allowed` recomputed at 2 sites (drift risk, not a current bug).
- IN-03 essay-branch log message omits Abandoned/Cancelled (operator diagnosis cosmetics).
- WR-01 answer rows persisted even when a concurrent request turns the session terminal post-pre-check —
  no correctness/security defect because the GradingService atomic guard (T-382-02/03) still blocks
  resurrection; rows are inert.

One open HUMAN-VERIFY item exists in VERIFICATION (visual dashboard rendering of cert null = "Aktif/
Permanen" across CMP/CDP/Renewal + Home badge). This is a UAT visual confirmation, not a security gap —
DB-level + predicate-mirror coherence are already proven by automated tests.

**Migration:** false (confirmed — `ef migrations add _verify_382` → empty Up/Down; no
`Migrations/`/`*ModelSnapshot.cs` diff across the phase). No new attack surface from schema change.

---

_Verified by gsd-security-auditor — Phase 382, ASVS Level 1, block-on: high._
