---
phase: 342
slug: manageorganization-page-fixes
status: finalized
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-03
finalized: 2026-06-03
---

# Phase 342 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Finalized by planner — per-task verification map below.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + EF Core InMemory 8.0.0 (HcPortal.Tests) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj (existing) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Compile gate (JS/view)** | `dotnet build HcPortal.csproj --nologo --verbosity minimal` |
| **Estimated runtime** | ~2-5 seconds (full suite baseline 20 tests + 6 baru = 26) |

---

## Sampling Rate

- **After every task commit:** `dotnet build HcPortal.csproj` (compile gate) + filtered test for affected unit
- **After every plan wave:** Full suite `dotnet test HcPortal.Tests`
- **Before `/gsd-verify-work`:** Full suite green (26/26) + browser smoke (Plan 02 Task 4) + manual UAT (Plan 03 Task 3)
- **Max feedback latency:** ~30 seconds (build + filtered test)

---

## Per-Task Verification Map

Test type legend: `unit` (xUnit automated), `compile` (dotnet build gate — JS/view not compile-checked but build verifies view refs), `manual` (browser checkpoint).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| P01-T1 | 342-01 | 1 | ORG-TREE-02 | T-342-04 | Dup-name per-parent server-authoritative (AnyAsync + ParentId) | compile | `dotnet build HcPortal.csproj --nologo --verbosity minimal` | ✅ OrganizationController.cs | ⬜ pending |
| P01-T2 | 342-01 | 1 | ORG-TREE-07 | T-342-01/02/03/06 | PreviewEditCascade CSRF+role+count-mirror (A1 full-accuracy) | compile | `dotnet build HcPortal.csproj --nologo --verbosity minimal` | ✅ OrganizationController.cs | ⬜ pending |
| P02-T1 | 342-02 | 2 | ORG-TREE-01/03/04/05/10 | T-342-07 | escapeHtml data-attr (XSS) + pre-order DFS + level cap + badge | compile | `dotnet build HcPortal.csproj --nologo --verbosity minimal` | ✅ orgTree.js | ⬜ pending |
| P02-T2 | 342-02 | 2 | ORG-TREE-06/07/09 | T-342-08/09 | cascade-confirm flow (CSRF via ajaxPost) + anti-circular dropdown | compile | `dotnet build HcPortal.csproj --nologo --verbosity minimal` | ✅ orgTree.js | ⬜ pending |
| P02-T3 | 342-02 | 2 | ORG-TREE-05/06/07/08/10 | T-342-10 | CSS palette + legend + path + cascade modal markup + async init | compile | `dotnet build HcPortal.csproj --nologo --verbosity minimal` | ✅ ManageOrganization.cshtml | ⬜ pending |
| P02-T4 | 342-02 | 2 | ORG-TREE-01/03/04/05/06/07/08/09/10 | T-342-07/08/09 | Browser smoke 10-check (render + 9 UX + regression drag/toggle) | manual | browser `http://localhost:5277/Admin/ManageOrganization` | — | ⬜ pending |
| P03-T1 | 342-03 | 3 | ORG-TREE-02 | T-342-13 | Dup-name accept-beda/reject-sama (Pitfall 5 casing identik) | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` | ❌ Wave 0 (create) | ⬜ pending |
| P03-T2 | 342-03 | 3 | ORG-TREE-07 | T-342-12 | preview count == actual (Level 0 + Level 1) + early-return D-04 | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` | ❌ Wave 0 (create) | ⬜ pending |
| P03-T3 | 342-03 | 3 | ORG-TREE-01..10 | T-342-14 | Manual UAT 10-skenario (cascade count modal == DB aktual) | manual | browser `http://localhost:5277/Admin/ManageOrganization` + DB query | — | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Sampling continuity check:** No 3 consecutive code-producing tasks without an automated verify. Plan 01 (compile gate both tasks) → Plan 02 (compile gate T1-T3, manual T4) → Plan 03 (unit T1-T2, manual T3). xUnit closes ORG-TREE-02 + ORG-TREE-07 backend correctness; JS/view requirements covered by compile gate (build) + browser smoke + manual UAT (no JS unit harness in repo — documented gap).

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` — file BARU (verified does not exist 2026-06-03). Created in Plan 03 Task 1. Covers ORG-TREE-02 (Pattern A) + ORG-TREE-07 (Pattern B + C).
- [x] `AdminBaseController` constructor verified (L20-30) — assigns fields only, NO dereference → `new OrganizationController(ctx, null!, auditLog, null!)` safe for Add/Edit/PreviewEditCascade (none call `_userManager`/`_env`).
- [x] Fixture pattern source verified: `HcPortal.Tests/OrgLabelControllerTests.cs` L27-90 (InMemory Guid DB + JsonResult reflection helpers + UserManager null-substitute).
- [ ] Add/Edit dup-check tests need `X-Requested-With: XMLHttpRequest` header in fixture HttpContext → IsAjaxRequest() true → Json response (assertable). PreviewEditCascade AJAX-only → no header needed. (Documented in Plan 03 interfaces.)
- Framework install: NONE — xUnit 2.9.3 + InMemory 8.0.0 already in HcPortal.Tests.csproj.

*JS-side (pre-order DFS, escape, level cap, path preview, cascade modal, label fetch): browser smoke (Plan 02 Task 4) + manual UAT (Plan 03 Task 3) — no JS unit harness in repo. Pre-order DFS logic NOT extracted to xUnit (would require JS→C# port; deferred — browser verify sufficient for Phase 342, formal Playwright = Phase 344 TEST-06).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pre-order DFS dropdown order | ORG-TREE-01 | No JS unit harness | Browser: open Tambah/Edit modal, verify dropdown parent→child→sibling indent order |
| Parent nonaktif visible greyed | ORG-TREE-03 | DOM render | Toggle parent inactive, verify suffix " (nonaktif)" + grey #999 in dropdown |
| Escape data-attribute | ORG-TREE-04 | DOM event + XSS | Unit name with quote (`O'Brien`), open Hapus, verify name correct + zero console error |
| Icon palette level 3-5 | ORG-TREE-05 | CSS render | Verify level 3+ nodes get distinct color (green/yellow/red) if depth available |
| Path breadcrumb real-time | ORG-TREE-06 | DOM event | Select parent, verify "Path: ... → (unit baru di sini)" updates live |
| Cascade confirm modal | ORG-TREE-07 | Bootstrap modal + live DB | Edit unit with >0 users, verify modal 4-line count == DB count, Lanjut applies cascade |
| Legend render | ORG-TREE-08 | DOM + label fetch | Verify card-header legend swatches + labels from GetLevelLabels |
| Modal title dynamic | ORG-TREE-09 | DOM + label fetch | Tambah at root→"Tambah Bagian", under Bagian→"Tambah Unit", under Unit→"Tambah Sub-unit" |
| Tier badge per row | ORG-TREE-10 | DOM + label fetch | Verify each row badge tier label, color = level palette (D-03) |
| Regression drag/toggle/delete | (Pitfall 2) | Live interaction | Drag-reorder sibling, toggle active, delete unit — all work, zero console error |

---

## Validation Sign-Off

- [x] All code-producing tasks have `<automated>` verify (compile gate or unit) or Wave 0 dependency (planner)
- [x] Sampling continuity: no 3 consecutive code tasks without automated verify
- [x] Wave 0 covers MISSING references (OrganizationControllerTests.cs created Plan 03 Task 1)
- [x] No watch-mode flags (all commands single-run)
- [x] Feedback latency < 30s (build + filtered test)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** finalized by planner 2026-06-03
