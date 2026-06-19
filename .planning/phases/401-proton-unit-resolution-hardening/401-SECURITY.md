---
phase: 401
slug: proton-unit-resolution-hardening
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-19
---

# Phase 401 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Phase 401 hardens PROTON unit-resolution: `AssignmentUnit` (explicit, junction-backed) becomes the
> single authoritative axis; the ambiguous `User.Unit` primary fallback is removed everywhere; client-supplied
> units are validated server-side against the `UserUnits` junction + org-tree before any write. 0 migration.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| client → CoachMapping POST (Assign/Edit/Import/Cleanup/Reactivate) | `AssignmentUnit` from form/JSON payload is untrusted; becomes persisted `CoachCoacheeMapping.AssignmentUnit` | unit string (org/identity-sensitive) |
| client → ProtonData POST (BypassSave) | `req.TargetUnit` from payload is written to `AssignmentUnit` via `ProtonBypassService` | unit string |
| coachee data → eligibility / cert-issuing gate | resolved unit gates exam/`AssessmentSession` + `NomorSertifikat` issuance | resolved unit → cert |
| coachee data → CDP / BypassList scope query | resolved unit determines which coachees appear in a coach/HC scope | resolved unit → visibility |

---

## Threat Register

| Threat ID | Category | Component | Disposition | Mitigation | Status |
|-----------|----------|-----------|-------------|------------|--------|
| T-401-validation | Tampering | `ValidateAssignmentUnitInUserUnits` helper + Assign/Edit/Import (CoachMappingController) + BypassSave TargetUnit (ProtonDataController) | mitigate | Server-authoritative `∈ active UserUnits` check via shared static helper (junction-only, never trusts client). Call sites: Assign `:533`, Edit `:793`, Import-new `:374`, Import-reactivate `:399`, Cleanup `:959`, Reactivate `:1111`; Bypass `ProtonDataController:1662` (+ org-tree `:1656`). Edit empty-unit hole (WR-02) FIXED at `:793-794` — helper returns false on empty → reject before tx. Helper `:52-62` is pure read. | closed |
| T-401-silent-primary | Tampering/Repudiation | GetEligibleCoachees gate + AutoCreateProgress read-path + AssessmentAdmin cert-gate + CDP defensive resolvers | mitigate | `User.Unit` primary fallback fully removed — `Select(u => u.Unit)` = **0** across all Controllers, `?? userUnits129.GetValueOrDefault` = **0** in CDP. Empty AssignmentUnit never resolves from primary; gate BLOCKs + persists `ProtonUnitUnresolved` AuditLog (CoachMapping `:1505`, AssessmentAdmin `:1421`). | closed |
| T-401-dataloss | Tampering/DoS-data | CleanupCoachCoacheeMappingOrg + Import-reactivate | mitigate | No-clobber preserve-gate (`:957-966`) runs BEFORE last-resort clobber (`:968-983`); valid non-primary `AssignmentUnit ∈ UserUnits` preserved. Import-reactivate clobber line removed (preserve comment `:399-415`). | closed |
| T-401-reactivate-stale | Tampering | CoachCoacheeMappingReactivate | mitigate | Reactivate rejects when `AssignmentUnit ∉ active UserUnits` (`:1111`) before `IsActive=true`. AF-4 ±5s `EF.Functions.DateDiffSecond` window UNCHANGED — exactly **2** occurrences (`:1142-1143`). | closed |
| T-401-audit-flood | DoS (storage) | AutoCreateProgress read-path | mitigate | Read-path uses `_logger.LogWarning` + `warnings.Add` only (`:1561-1564`); **0** `_auditLog.LogAsync` in the read-path window. | closed |
| T-401-cert-misissue | Tampering | session/cert issuance (AssessmentAdmin gate) | mitigate | Server-authoritative gate excludes coachee from eligible set when unit unresolved → no session/cert row. Resolver `:1414-1415` AssignmentUnit-only; empty → `gateSkippedNotHundred++; continue;` (BLOCK) + persisted audit + LogWarning (`:1416-1426`). | closed |
| T-401-wrong-unit-visibility | Information Disclosure | CDP filter-axis + BypassList | mitigate | Filter by `AssignmentUnit` (active mapping), not primary mirror. CDP sites `:491/:494-500` (normalized dict), `:1599/:1611/:4264` (EXISTS subquery); BypassList `:1522`. `x.u.Unit == unit` / `u.Unit == unit` = **0** in CDP + ProtonData. CDP read-path defensive resolvers warn via ILogger only (`:537/:1733`); the 3 CDP `_auditLog.LogAsync` (`:2498/:2602/:4132`) are unrelated existing session-audit, outside the resolver windows. Section-based authz unchanged. | closed |
| T-401-authz | Elevation/CSRF | all CoachMapping POST endpoints (Assign/Edit/Import/Cleanup/Reactivate) | accept | `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` present and NOT weakened: Import `:241-243`, Assign `:512-514`, Edit `:753-755`, Cleanup `:931-933`, Reactivate `:1094-1096`. | closed |
| T-401-csrf | Spoofing/CSRF | ProtonData BypassSave POST | accept | Class-level `[Authorize(Roles="Admin,HC")]` (`:97`) + action `[HttpPost]` + `[ValidateAntiForgeryToken]` (`:1630-1632`) unchanged. | closed |
| T-401-single-active | Tampering | Reactivate duplicate-check + ProtonBypassService E8 + unique index | accept | Reactivate duplicate-active guard intact (`:1105-1108`); ProtonBypassService E8 single-active check untouched (`:104`, `:223`); `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` not modified (Phase 401 = 0 migration). | closed |
| T-401-perf-N+1 | DoS | CDP filter-axis | accept | Batch `unitByCoachee` dictionary / EXISTS subquery per site — no per-row query inside coachee loops. | closed |
| T-401-novalidation-bypass | Tampering | static helper purity | accept | Helper `:52-62` is a pure read (no write, no state mutation); cannot itself weaken state. Callers retain authz + antiforgery. | closed |

*Status: open · closed*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-401-1 | T-401-authz / T-401-csrf | All Phase-401-touched POST endpoints retain unchanged `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]`; this phase only changed resolver/validation internals, not the auth surface. Verified present at every cited line. | Phase 401 plan (security_auditor verified) | 2026-06-19 |
| AR-401-2 | T-401-single-active | Single-active invariant is enforced by the pre-existing duplicate-active guard, ProtonBypassService E8 check, and the `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` filtered-unique index — all untouched in Phase 401 (0 migration). Deep SQL-real single-active integration assertions (HTTP context + index enforcement, not testable on EF-InMemory) are explicitly deferred to Phase 404 (2 `[Fact(Skip)]` reference Phase 404). | Phase 401 plan + VERIFICATION | 2026-06-19 |
| AR-401-3 | T-401-perf-N+1 | CDP/BypassList filter-axis uses batched dictionary / EXISTS subquery (one query per site), avoiding per-row N+1 — accepted as adequately mitigated. | Phase 401 plan | 2026-06-19 |
| AR-401-4 | T-401-novalidation-bypass | The shared validation helper is a pure read against the junction and cannot mutate state; no auth attribute is needed on a static method since all callers are authz-protected actions. | Phase 401 plan | 2026-06-19 |

*Accepted risks do not resurface in future audit runs.*

Non-security quality notes (from 401-REVIEW.md, NOT security gaps): WR-01 (CDP filter-axis `m.AssignmentUnit == unit` lacks Trim/OrdinalIgnoreCase at `:1599/:1611/:4264` — false-negative visibility under whitespace/case-sensitive collation) and WR-03 (Cleanup preserve-branch counts `autoFixed++` even when Section unresolvable — cosmetic count). Both are non-blocking, do not weaken any threat mitigation, and fall under Phase 404 SQL-real coverage (QA-04).

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-19 | 12 | 12 | 0 | Claude (gsd-security-auditor) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-19
