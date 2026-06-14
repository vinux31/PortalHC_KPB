---
phase: 378-fix-cmp-certificationmanagement-route-500
plan: 01
asvs_level: 1
audited: 2026-06-14
threats_total: 4
threats_closed: 4
threats_open: 0
block_on: high
result: SECURED
---

# Security Audit — Phase 378 Plan 01

**Phase:** 378 — Fix CMP CertificationManagement Route 500
**ASVS Level:** 1
**Threats Closed:** 4 / 4
**Threats Open:** 0

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence |
|-----------|----------|-------------|--------|----------|
| T-378-01 | Tampering / Open-Redirect | mitigate | CLOSED | `Controllers/CMPController.cs:3589–3590` — `RedirectToAction("CertificationManagement", "CDP")` uses fixed string literals only. No user-supplied parameter flows into either argument (`int page` param was dropped from the action signature; action now `public IActionResult CertificationManagement()` with no parameters). No open-redirect surface. |
| T-378-02 | Elevation of Privilege | accept | CLOSED | All 6 dead public methods (`GetCascadeOptions`, `GetSubCategories`, `FilterCertificationManagement`, `CertificationManagementDetail`, `FilterCertificationManagementDetail`, `ExportSertifikatExcel`) are absent from `Controllers/CMPController.cs` (grep returns 0 occurrences for each). Two orphan private builders (`BuildSertifikatGroups`, `BuildGroupViewModel`) are also absent. Attack surface reduced, not expanded. CDP analogues retain `[Authorize]` class-level (`CDPController:30`) and method-level attributes. `ExportSertifikatDetailExcel` + `BuildSertifikatRowsAsync` (callee) are preserved intact in CMPController. |
| T-378-03 | Information Disclosure | accept | CLOSED | `Controllers/CDPController.cs:3704` — `CertificationManagement(int page = 1)` is the redirect target and is UNCHANGED by this phase. CDPController carries class-level `[Authorize]` at line 30. Role-scoped data access via `BuildSertifikatRowsAsync(l5OwnDataOnly: true)` (CDPController:3706) is intact. Redirect does not loosen authorization; users who previously received 500 now reach the identical role-gated view they would have reached via the productive entry point at `Views/CMP/Index.cshtml:98`. |
| T-378-04 | Denial of Service (redirect loop) | accept | CLOSED | CMP redirect is one-way: `CMPController.CertificationManagement` (line 3590) calls `RedirectToAction("CertificationManagement","CDP")`. CDPController.CertificationManagement (line 3704) contains no `RedirectToAction` referencing CMP — it calls `BuildSertifikatRowsAsync` and returns `View(vm)` directly. Grep of CDPController.cs for `RedirectToAction` confirms zero occurrences pointing back to CMP. E2E test Y0 (`tests/e2e/exam-types.spec.ts:2051`) asserts `status == 200` after following the redirect, proving single-hop resolution with no loop. |

## Accepted Risks Log

| Threat ID | Rationale | Owner |
|-----------|-----------|-------|
| T-378-02 | Deletion of zero-caller dead endpoints reduces attack surface; CDP canonical endpoints with full authorization remain. No consumer loses access. | Dev |
| T-378-03 | CDP CertificationManagement authorization is unchanged and retains existing role scoping. Redirect consolidates two entry points into the same role-gated view. | Dev |
| T-378-04 | One-way redirect confirmed by code inspection and E2E assertion (status 200, URL contains /CDP/CertificationManagement). | Dev |

## Unregistered Flags

None. SUMMARY.md `## Threat Flags` section contains no entries beyond the four registered threats. The single deviation noted in SUMMARY (partial view `_SertifikatGroupTablePartial.cshtml` never existed in the repo — D-05 no-op) has no security implication.

## Verification Commands Run

- `grep RedirectToAction Controllers/CMPController.cs` — confirms `RedirectToAction("CertificationManagement", "CDP")` at line 3590 with fixed literals; action signature has no parameters.
- `grep FilterCertificationManagement|CertificationManagementDetail|GetCascadeOptions|GetSubCategories|ExportSertifikatExcel Controllers/CMPController.cs` — 0 occurrences of deleted methods (only `ExportSertifikatDetailExcel` and `BuildSertifikatRowsAsync` remain, both live callee/caller pair).
- `grep [Authorize] Controllers/CDPController.cs` — class-level `[Authorize]` present at line 30; method-level role attributes present throughout.
- `grep CertificationManagement Controllers/CDPController.cs` — action at line 3704 renders View directly; no RedirectToAction back to CMP.
- `grep CDP/CertificationManagement tests/e2e/exam-types.spec.ts` — Y0 at line 2053 asserts `expect(page.url()).toContain('/CDP/CertificationManagement')` and line 2051 asserts `expect(response?.status()).toBe(200)`.
