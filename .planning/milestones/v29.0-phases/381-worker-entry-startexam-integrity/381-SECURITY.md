---
phase: 381
slug: worker-entry-startexam-integrity
status: verified
threats_open: 0
asvs_level: 1
created: 2026-06-15
---

# Phase 381 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.
> Worker Entry / StartExam integrity (WSE-04 pool isolation + WSE-05 impersonate read-only).

**ASVS Level:** 1 · **block_on:** high · **Threats:** 13 total — **13 CLOSED / 0 OPEN**.
**Audited:** 2026-06-15 by gsd-security-auditor (sonnet), implementation files READ-ONLY.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| worker browser → CMPController.StartExam GET | `id` route param; owner/role gated (905) | assessment ownership |
| admin (impersonate X) browser → StartExam GET | must be read-only — no mutation of worker X state | session/assignment state |
| HC browser → AssessmentAdminController reshuffle | `[Authorize(Roles="Admin, HC")]` + antiforgery | package assignment |
| StartExam/reshuffle → AssessmentSessions/Packages (EF) | server-authoritative; client never determines question set | exam pool + workerIndex |
| StartExam GET → SignalR monitor group | `workerStarted` broadcast only on real worker start | monitor notification |

---

## Threat Register (Verification)

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-381-01 | Tampering | mitigate | CLOSED | `Helpers/SiblingSessionQuery.cs:14` `SiblingPrePostAwarePredicate` isolates Pre/Post pools (`isPrePost ? s.AssessmentType==assessmentType : (s.AssessmentType!="PreTest" && s.AssessmentType!="PostTest")`). Wired `CMPController.cs:1017-1019` (StartExam sibling-query). Server-authoritative. |
| T-381-02 | Tampering | mitigate | CLOSED | Shared helper identical at 3 round-robin sites: `CMPController.cs:1017`, `AssessmentAdminController.cs:5192` (ReshufflePackage), `~5273/5321-5323` (ReshuffleAll `SiblingKey`+`sortedByKey`). `OrderBy(x=>x).IndexOf(id)` unchanged both sides. |
| T-381-03 | Tampering | accept | CLOSED | non-PrePost (Standard/empty/null) grouped via `!="PreTest" && !="PostTest"`. Unit tests 3 (StandardCaller) + 4 (NullCaller) `SiblingPrePostFilterTests.cs` prove grouping. No behavior change for standard exams. |
| T-381-04 | Elevation/IDOR | accept | CLOSED | owner+role check `CMPController.cs:905` UNCHANGED; Phase 377 resolver (903) ensures effective-user X checked, not admin Id. |
| T-381-05 | Tampering | mitigate | CLOSED | `CMPController.cs:993` `if (justStarted && !_impersonationService.IsImpersonating())` wraps StartedAt/Status write. E2E WSE-05: `StartedAt IS NULL` after impersonate. |
| T-381-06 | Tampering | mitigate | CLOSED | `CMPController.cs:1078` `if (!IsImpersonating())` wraps persist `UserPackageAssignments.Add`+`SaveChangesAsync`. Build runs unconditionally (D-06). E2E: `UPA count=0`. |
| T-381-07 | Spoofing | mitigate | CLOSED | `CMPController.cs:1002` guard wraps SignalR `SendAsync("workerStarted")`. No broadcast during impersonate (verified via DB no-mutation). |
| T-381-08 | Repudiation | mitigate | CLOSED | `LogActivityAsync(id,"started")` `CMPController.cs:1009` inside same guard (1002). No false "started" log during impersonate. |
| T-381-09 | Info Disclosure | accept | CLOSED | `vm.AssignmentId=0` read-only (D-06); view-build no DB re-read. E2E WSE-05 `status<400` + qcard visible. UAT Bagian A APPROVED 2026-06-15. |
| T-381-10 | Elevation | accept | CLOSED | `VerifyToken` (`CMPController.cs:856-891`) zero `IsImpersonating`; writes only `TempData` (no DB mutation). Real token gate = Phase 382 TOK-02 (D-05). |
| T-381-11 | Info Disclosure | mitigate | CLOSED | E2E `impersonation.spec.ts` WSE-05 asserts `impResp.status()<400` + qcard visible — auto-covers Razor `vm.AssignmentId=0` render. 3/3 green, no NRE/500. |
| T-381-12 | Tampering | mitigate | CLOSED | `dotnet ef migrations add` → empty `Up()`/`Down()` → removed → snapshot noise reverted → tree clean. APPROVED 2026-06-15. No `Migrations/` in any 381 commit. |
| T-381-13 | Tampering | mitigate | CLOSED | `globalTeardown` RESTORE + `--workers=1`. SUMMARY: "Teardown RESTORE Layer-4 OK (0 matrix rows), SEED_JOURNAL cleaned" after 3/3 green. |

*Disposition: mitigate (implementation) · accept (documented risk). All 13 CLOSED.*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Residual | Accepted By | Date |
|---------|------------|-----------|----------|-------------|------|
| AR-01 | T-381-03 | Legacy Standard/empty/null grouped as non-PrePost (homogeneous-group assumption post-ISS-04; mixed null+Standard extremely unlikely). Unit tests 3/4 verify logic. | Very Low | user | 2026-06-15 |
| AR-02 | T-381-04 | IDOR via existing owner+role check (905) unchanged; Phase 377 resolver checks effective-user X. | Low | user | 2026-06-15 |
| AR-03 | T-381-09 | Preview `vm.AssignmentId=0` in-memory read-only; view reads string fields only. E2E runtime assert + UAT approval. | Very Low | user | 2026-06-15 |
| AR-04 | T-381-10 | `VerifyToken` TempData-only (not a DB write-site). Real token gate = Phase 382 TOK-02. | Low (deferred 382) | user | 2026-06-15 |

---

## Deferred Items (Out of Scope Phase 381)

| Item | Phase | Note |
|------|-------|------|
| Full PrePost pass/grade E2E (Score/Lulus after pool fix) | 382 | Marked `// DEFERRED pasca-382` in `tests/e2e/exam-taking.spec.ts` |
| Real token gate (StartedAt) in VerifyToken/SaveExam flow | 382 TOK-02 | VerifyToken intentionally unmodified (D-05) |

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-06-15 | 13 | 13 | 0 | gsd-security-auditor (sonnet) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-06-15

---

## Notes

- Migration: FALSE — EF scaffold dry-run empty Up/Down. 381 commits touch only `Helpers/`, `Controllers/`, `HcPortal.Tests/`, `tests/e2e/` — no `Migrations/`.
- xUnit 391/391 green (+7 new). E2E 3/3 green (`--workers=1`, chromium headless, AD off, RESTORE).
- NOT PUSHED (local only) at time of audit.
