---
phase: 342
slug: manageorganization-page-fixes
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-03
---

# Phase 342 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Skeleton — planner finalizes per-task verification map.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + EF Core InMemory 8.0.0 (HcPortal.Tests) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj (existing) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationControllerTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Estimated runtime** | ~2-5 seconds (full suite baseline 38 tests) |

---

## Sampling Rate

- **After every task commit:** `dotnet build HcPortal.csproj` (compile gate) + filter test for affected unit
- **After every plan wave:** Full suite `dotnet test HcPortal.Tests`
- **Before `/gsd-verify-work`:** Full suite green + browser smoke (Playwright)
- **Max feedback latency:** ~30 seconds (build + filtered test)

---

## Per-Task Verification Map

*Planner finalizes. Derive from RESEARCH §Validation Architecture:*
- Pre-order DFS sort correctness (JS — manual browser + optional unit)
- Per-parent dup-name accept/reject (xUnit: "Operations" allowed in 2 parents, rejected in same parent)
- PreviewEditCascade count accuracy: preview count == actual EditOrganizationUnit cascade count (xUnit, mirror 4 field-pairs)
- PreviewEditCascade early-return on no-change (xUnit)

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (planner fills) | | | ORG-TREE-* | | | unit/manual | `dotnet test ...` | | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` — may not exist yet (verify); create stub fixture (AdminBaseController ctor null-safe per PATTERNS) for dup-name + PreviewEditCascade tests
- [ ] Reuse `OrgLabelControllerTests` fixture pattern (InMemory + JsonResult reflection helpers + UserManager null-substitute) from Phase 341

*JS-side (pre-order DFS, escape, level cap, path preview, cascade modal, label fetch): browser smoke + manual UAT — no JS unit harness in repo.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pre-order DFS dropdown order | ORG-TREE-01 | No JS unit harness | Browser: open Tambah Unit modal, verify dropdown parent→child→sibling order |
| Parent nonaktif visible greyed | ORG-TREE-03 | DOM render | Toggle parent inactive, verify suffix "(nonaktif)" + grey in dropdown |
| Path breadcrumb real-time | ORG-TREE-06 | DOM event | Select parent, verify "Path: ... → (unit baru)" updates |
| Cascade confirm modal | ORG-TREE-07 | Bootstrap modal + live DB | Edit unit with >0 users, verify modal shows count, Lanjut applies |
| Legend + badge render | ORG-TREE-08/10 | DOM + label fetch | Verify legend swatches + per-row badge from GetLevelLabels |
| Icon palette level 3-5 | ORG-TREE-05 | CSS render | Verify level 3+ nodes get distinct color (if depth available) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies (planner)
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter (planner after finalization)

**Approval:** pending
