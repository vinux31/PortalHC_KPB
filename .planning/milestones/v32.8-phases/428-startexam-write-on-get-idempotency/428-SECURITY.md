---
phase: 428
slug: startexam-write-on-get-idempotency
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-25
---

# Phase 428 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Verifies EXSEC-02 (GET CMP/StartExam idempotency). Migration=FALSE — no new schema/route/network surface.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Browser (worker/admin) → CMP/StartExam GET | Untrusted input: session `id` + identity (auth cookie) + impersonation state (session). GET must not mutate DB state as a side-effect of navigation/refresh/prefetch. | Session id (int), auth principal, impersonation keys |
| Impersonation (admin/HC view-as-user) → StartExam | Admin/HC access StartExam for monitoring/debug; must be read-only (no worker-state writes). | Effective-user resolution, exam skeleton (questions, not results) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-428-01 | Tampering | GET StartExam write-on-GET (Upcoming→Open persist) | mitigate | Persist block removed — no `assessment.Status = "Open"` anywhere in `CMPController.cs` (grep: 0 matches); transition computed in-memory. No `SaveChangesAsync` for the Upcoming→Open transition in the StartExam GET path. `CMPController.cs:922-933` | closed |
| T-428-02 | Elevation of Privilege | Impersonation writes worker state (StartedAt/Status) | accept (pre-mitigated) | Only un-guarded write (persist-Open) removed. InProgress write `CMPController.cs:1015` and assignment-create `CMPController.cs:1100` both retain `!_impersonationService.IsImpersonating()` guard. Impersonate StartExam now 100% read-only — no new surface. | closed |
| T-428-03 | Tampering | Bypass time-gate (access session before scheduled time) | mitigate | Time-gate `assessment.Status == "Upcoming" && assessment.Schedule > nowWib` at `CMPController.cs:929` (grep: exactly 1 match), in-memory via `nowWib` (`:925`), no DB write. T3 (`StartExam_Upcoming_NotYetTime_BlocksAndNoWrite`) proves block + Status unchanged. | closed |
| T-428-04 | Tampering | Regression of token-gate (EXSEC-01) / GRDF-01 during refactor | mitigate | Gate order preserved (R-1): time-gate `:929` → Completed `:935` → GRDF-01 `:944-951` → token-gate EXSEC-01 `:958-966` (`TokenVerifiedAt == null`) → exam-window `:969` → duration `:976` → abandoned `:983`. T4 (GRDF-01) + T6 (token-gate) prove both still block. | closed |
| T-428-05 | Information Disclosure | Effective-open renders exam skeleton to impersonator pre-start | accept | Read-only exam-skeleton render during impersonation is intended monitoring behavior (Admin/HC may access StartExam for debugging/monitoring). No worker answer/result data leaked (question skeleton only). Consistent with existing admin token-gate bypass. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-428-01 | T-428-02 | Impersonation EoP write-of-worker-state pre-mitigated by existing `!IsImpersonating()` guards on the two remaining writes (InProgress `:1015`, assignment-create `:1100`). The only previously un-guarded write (Upcoming→Open persist) was REMOVED this phase, so the impersonation path is now 100% read-only. No residual write surface. | gsd-security-auditor (planner disposition) | 2026-06-25 |
| AR-428-02 | T-428-05 | Effective-open exam-skeleton render to an impersonating Admin/HC is intended monitoring behavior. Only the question skeleton is rendered (no worker answers/results). Consistent with the existing admin token-gate bypass already accepted in prior phases. | gsd-security-auditor (planner disposition) | 2026-06-25 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-25 | 5 | 5 | 0 | gsd-security-auditor |

### Verification Evidence

- **T-428-01** — `grep 'assessment.Status = "Open"' Controllers/CMPController.cs` → 0 matches. The old auto-transition persist block (Status="Open" + UpdatedAt + SaveChangesAsync) is gone; replaced by in-memory effective-status (`CMPController.cs:922-933`). SaveChangesAsync calls in StartExam GET (`:1019`, `:1105`) are only the in-scope justStarted/assignment writes (both impersonation-guarded), not the status transition.
- **T-428-02** — `CMPController.cs:1015` `if (justStarted && !_impersonationService.IsImpersonating())` (InProgress write) and `:1100` `if (!_impersonationService.IsImpersonating())` (assignment-create) verified intact. Test T1 (`StartExam_Impersonate_TimeArrivedUpcoming_RendersWithoutPersisting`) asserts Status stays "Upcoming" + StartedAt null via fresh-context DB reload.
- **T-428-03** — `grep 'assessment.Schedule > nowWib'` → exactly 1 match at `:929`. In-memory (`nowWib = DateTime.UtcNow.AddHours(7)` at `:925`), no DB write. Test T3 proves redirect to Assessment + Status unchanged.
- **T-428-04** — Gate ordering in source confirms time-gate → Completed → GRDF-01 → token-gate (EXSEC-01 `TokenVerifiedAt == null` block at `:961-965`). Tests T4 (GRDF-01) and T6 (token-gate regression) both assert Redirect("Assessment") + correct TempData error + Status unchanged.
- **migration=FALSE** — Phase 428 commits (a3d133fc, b7ca8caf) touch only `Controllers/CMPController.cs` + `HcPortal.Tests/StartExamIdempotencyTests.cs`. No Migrations/ files; latest migration (AddTokenVerifiedAt, 2026-06-24) is from phase 427. No new schema/route/network surface.
- **Test coverage** — `StartExamIdempotencyTests.cs`: 6 `[Fact]` (grep count = 6), `[Trait("Category","Integration")]`, `IClassFixture<RetakeServiceFixture>`, `ImpersonationKeys.Mode` all present. SUMMARY reports 6/6 pass real-SQL; full suite 784/0/2 no regression.

### Threat Flags (from SUMMARY ## Threat Surface Scan)

No new attack surface declared by executor. Change is REDUCING side-effect (removes one un-guarded write-on-GET). No route/endpoint/schema/network added. No unregistered flags. ASVS L1 access gating (V4) proven intact via T3/T4/T6; GET idempotency (best-practice) restored via T1/T2. block_on=high → no High/Critical threats → not blocking.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-25
