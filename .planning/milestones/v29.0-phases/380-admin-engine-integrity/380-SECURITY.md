---
phase: 380
slug: admin-engine-integrity
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-14
---

# Phase 380 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| worker browser → CMPController.StartExam (GET) | Authenticated worker triggers exam-start; controller mutates session state (StartedAt/Status) and builds question assignment | exam session state, question set |
| engine (pure) → controller | ShuffleEngine returns the question set the controller persists as the worker's exam | question id list |
| worker browser → CMPController.VerifyToken (POST) | Untrusted token input crosses the access gate; correct comparison decides exam entry | access token |
| any-authenticated client → AssessmentAdminController.AddExtraTime (POST) | Admin-write endpoint reachable by any authenticated user (incl. worker) absent a role gate | exam time grant |
| admin browser → AssessmentAdminController.EditAssessment (POST) | Admin writes token data the worker gate later compares against | access token (Pre/Post) |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-380-01 | Tampering/Integrity (silent corruption) | ShuffleEngine.BuildCrossPackageAssignment ON-path: empty pkg ⇒ K=Min=0 ⇒ batch-wide 0% Fail | mitigate | D-04 empty-package `.Where(...)` filter before Count==1/K=Min (`ShuffleEngine.cs:99-103`); unit facts `On_MultiPackage_OneEmpty` / `On_AllPackagesEmpty` | closed |
| T-380-02 | Tampering/Integrity (false fail-state) | StartExam writes Status/StartedAt then renders empty exam → auto-grade 0% | mitigate | D-05 all-empty `AnyAsync` guard hoisted before justStarted write (`CMPController.cs:970-990`); zero state mutation on block | closed |
| T-380-03 | DoS (empty set) | engine DivideByZero / empty K on all-empty | accept | engine returns `[]` safely (K==0 guard before modulo); controller guard converts to friendly UX. No HIGH severity | closed |
| T-380-04 | **Elevation of Privilege (HIGH)** | AddExtraTime lacks role gate — any authed user (incl. worker) grants own extra exam time | mitigate | D-02 `[Authorize(Roles = "Admin, HC")]` (`AssessmentAdminController.cs:6880`); reflection-authz test asserts exact roles | closed |
| T-380-05 | Tampering / business-logic abuse | AddExtraTime unbounded accumulation breaks timed-exam integrity | mitigate | D-03 per-session cap `currentExtra + minutes <= DurationMinutes`, reject-whole-batch (atomic, JSON) before accumulation (`:6911-6918`); 4 cap unit facts | closed |
| T-380-06 | Integrity / access gate (TOK-01) | VerifyToken single-side compare locks out workers despite correct input | mitigate | D-01a both-sides `.Trim().ToUpperInvariant()` via `AccessTokenMatches` (`CMPController.cs:850-851`, wired :883); D-01b uppercase-on-write 3 Pre/Post sites; 5 token unit facts | closed |
| T-380-07 | Spoofing (token guessing) | worker brute-forces access token at VerifyToken | accept | out of scope this phase — no rate-limit change; uppercase access codes not crypto secrets; pre-existing posture unchanged | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-380-01 | T-380-03 | All-empty engine input returns `[]` (no exception — K==0 guard precedes modulo); converted to friendly redirect by T-380-02 controller guard. Not a security defect; UX-handled. | gsd-security-auditor + user | 2026-06-14 |
| AR-380-02 | T-380-07 | Token brute-force out of scope; tokens are short uppercase access codes (exam gate convenience), not cryptographic secrets. No rate-limit change requested; pre-existing posture unchanged. Note for future audit backlog if exam-token strength is ever in scope. | gsd-security-auditor + user | 2026-06-14 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-14 | 7 | 7 | 0 | gsd-security-auditor (sonnet) |

**Audit notes:** 5 mitigate + 2 accept. HIGH threat T-380-04 (AddExtraTime privilege escalation) CLOSED — `[Authorize(Roles="Admin, HC")]` present + reflection-tested. Intentional deviation verified: mitigation logic for T-380-05/06 lives in pure static helpers (`ExtraTimeWithinCap` :6876, `AccessTokenMatches` :850) and is correctly wired into the live call sites (:6915, :883); semantics identical to the inline plan form. Auditor re-ran `dotnet test --filter "AddExtraTime|VerifyToken|ShuffleEngine"` → 26 passed / 0 failed. Implementation files not modified during audit. `block_on: high` — no blocker.

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-14
