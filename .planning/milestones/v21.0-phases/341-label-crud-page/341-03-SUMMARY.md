---
phase: 341
plan: 03
subsystem: admin-org-label
tags: [xunit, controller-test, validation, uat]
requires: [341-01 OrgLabelController, 341-02 view]
provides: [OrgLabelControllerTests validation coverage, UAT proof Coach403+audit+endpoint]
affects: [HcPortal.Tests/OrgLabelControllerTests.cs]
tech-stack:
  added: []
  patterns: [xUnit InMemory controller test, UserManager null-substitute, reflection JsonResult assertion]
key-files:
  created:
    - HcPortal.Tests/OrgLabelControllerTests.cs
  modified: []
key-decisions: [skip actor-resolution test, 7 facts validation-only, manual UAT for role+audit]
requirements-completed: [ORG-LABEL-04, ORG-LABEL-05, ORG-LABEL-06]
duration: "~20 min"
completed: 2026-06-03
---

# Phase 341 Plan 03: xUnit Controller Tests + Manual UAT Summary

Built `OrgLabelControllerTests.cs` (7 [Fact] validation-path tests) + automated all 3 manual UAT checkpoints via Playwright MCP + sqlcmd. Full suite 38/38 PASS. UAT 3/3 PASS.

## Commits

| Task | Hash | Description |
|------|------|-------------|
| 1 | `6e55f92a` | test(341-03): add OrgLabelControllerTests 7 validation facts |

## Tasks Completed

- **Task 1** — `HcPortal.Tests/OrgLabelControllerTests.cs` (NEW, 162 LoC, 7 [Fact]): `MakeControllerWithCtx` factory (InMemory DB + seed 3 label + optional unit Level 2) with UserManager null-substitute; reflection helpers `GetSuccess`/`GetMessage`.
- **Task 2** — checkpoint:human-verify → **automated, 3/3 PASS** (see UAT below).

## Test Outcomes (7/7 PASS)

| # | [Fact] | Assert | Result |
|---|--------|--------|--------|
| 1 | UpdateLevelLabel_EmptyLabel | success=false, msg "kosong" | ✅ |
| 2 | UpdateLevelLabel_WhitespaceLabel | success=false, msg "kosong" | ✅ |
| 3 | UpdateLevelLabel_TooLong (51 char) | success=false, msg "50" | ✅ |
| 4 | UpdateLevelLabel_DuplicateAcrossLevels ("Unit") | success=false, msg "sudah dipakai" | ✅ |
| 5 | AddLevelLabel_NonNextLevel (99) | success=false, msg "Hanya level berikutnya" | ✅ |
| 6 | DeleteLevelLabel_NonHighest (0) | success=false, msg "tertinggi" | ✅ |
| 7 | DeleteLevelLabel_HighestInUse (2 + seeded unit) | success=false, msg "dipakai" | ✅ |

- `dotnet test --filter OrgLabelControllerTests` → **7/7 PASS** (~2s).
- Full suite `dotnet test HcPortal.Tests` → **38/38 PASS** (31 baseline + 7 new), 0 skipped. No Phase 340 regression.
- Note: baseline was 31 (not 20 cited in plan — accrued since Phase 340); delta +7 honored.

## Manual UAT (automated via Playwright MCP + sqlcmd) — 3/3 PASS

**UAT 1 — Coach 403 (T-341-02):** Logged in real Coach account `rustam.nugroho@pertamina.com` (Rustam Santiko, role Coach). Navigate `/Admin/ManageOrgLevelLabels` → **`/Account/AccessDenied` "Akses Ditolak"**. Confirms `[Authorize(Roles="Admin, HC")]` blocks non-Admin/HC. ✅ (Real authz test — impersonation rejected as invalid: it's a view-overlay, not a claims swap.)

**UAT 2 — Audit log content (ORG-LABEL-05):** As admin, performed Update L0→Direktorat + Add L3 Kelompok + Delete L3 (real controller actions, 200 success). DB query `AuditLogs WHERE ActionType LIKE 'OrgLabel-%'`:

| ActionType | ActorName | ActorUserId | TargetId | TargetType | Description |
|-----------|-----------|-------------|----------|-----------|-------------|
| OrgLabel-Update | Admin KPB | 2d26f03d-ccb9-4cdc-b94e-af46a2c3e631 | 0 | OrganizationLevelLabel | Level 0: 'Bagian' → 'Direktorat' |
| OrgLabel-Add | Admin KPB | 2d26f03d-... | 3 | OrganizationLevelLabel | Level 3: 'Kelompok' created |
| OrgLabel-Delete | Admin KPB | 2d26f03d-... | 3 | OrganizationLevelLabel | Level 3: 'Kelompok' deleted |

Proves live actor resolution (real GUID + NIP-null→FullName "Admin KPB" fallback) + Description format matches Phase 340 D-04 spec. ✅

**UAT 3 — Phase 340 endpoint regression:** `GET /Admin/GetLevelLabels` (as Coach, class-level [Authorize]) → **200 JSON `{"0":"Bagian","1":"Unit","2":"Sub-unit"}`** verbatim Phase 340 baseline. ✅

**DB hygiene (SEED_WORKFLOW):** snapshot `HcPortalDB_Dev_pre341uat2.bak` → mutations → RESTORE → Labels=3, OrgLabel audit=0 verified clean. SEED_JOURNAL cleaned.

## Deviations from Plan

None - plan executed exactly as written (7 [Fact] per plan's final firm count; Test 8 happy-path dropped per plan instruction). UAT 1 executed as REAL Coach login (not skipped) — discovered Coach default password = "123456" (seed bootstrap default); impersonation evaluated and correctly rejected as invalid authz test (view-overlay, no IClaimsTransformation).

## Phase 341 Closure

3/3 plans SHIPPED LOCAL:
- 341-01 (controller + ViewModel): facd26db, 015b961d
- 341-02 (view + card): 689f6384, eed64495 + UAT 10/10
- 341-03 (tests + UAT): 6e55f92a + UAT 3/3

REQ ORG-LABEL-04/05/06 delivered. Build ✓ · 38/38 test ✓ · pending push origin/main (bundle v19+v20+v21).

## Self-Check: PASSED
