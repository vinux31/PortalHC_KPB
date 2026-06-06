---
phase: 341
slug: label-crud-page
status: draft
nyquist_compliant: false
wave_0_complete: true
created: 2026-06-03
---

# Phase 341 — Validation Strategy

> Per-phase validation contract for Phase 341 (Label CRUD Page) — to be finalized by planner. Skeleton generated from RESEARCH.md §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelControllerTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Estimated runtime** | ~1–3 seconds |
| **InMemory provider** | Microsoft.EntityFrameworkCore.InMemory 8.0.0 (Phase 340 P3 commit `43e94655`) |
| **Test isolation** | per-`[Fact]` `Guid.NewGuid()` InMemory DB (no cross-test state) — replicate `OrgLabelServiceTests.MakeServiceWithCtx` factory |

---

## Sampling Rate

- **After every task commit:** Quick filter on new tests (~1s feedback)
- **After every plan wave:** Full suite (~3s)
- **Before `/gsd-verify-work`:** Full suite green
- **Max feedback latency:** ~3 seconds

---

## Per-Task Verification Map

*Planner populates this section per task in PLAN.md files. Skeleton entries (to be confirmed/expanded):*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 341-01-T1 | 01 | 1 | ORG-LABEL-04 | T-341-02 | Controller actions wired with `[Authorize(Roles="Admin,HC")]` | build | `dotnet build` | ⬜ | ⬜ pending |
| 341-01-T2 | 01 | 1 | ORG-LABEL-04, 06 | T-341-04, 05 | ViewModel + GET/POST inline validation | unit/build | `dotnet test --filter OrgLabelControllerTests` | ⬜ | ⬜ pending |
| 341-02-T1 | 02 | 2 | ORG-LABEL-04 | T-341-01 | Razor view + AntiForgery token + buffer row + modal | manual visual | open `/Admin/ManageOrgLevelLabels` browser | ⬜ | ⬜ pending (MANUAL) |
| 341-02-T2 | 02 | 2 | ORG-LABEL-04 | T-341-09 | JS fetch + shared-toast + reload | manual visual | submit edit → toast + reload | ⬜ | ⬜ pending (MANUAL) |
| 341-02-T3 | 02 | 2 | ORG-LABEL-04 | — | Admin card added Views/Admin/Index.cshtml | grep | `grep -c "ManageOrgLevelLabels" Views/Admin/Index.cshtml` | ⬜ | ⬜ pending |
| 341-03-T1 | 03 | 3 | ORG-LABEL-04, 06 | T-341-04 | xUnit Controller actions test ModelState + service behavior | unit | `dotnet test --filter OrgLabelControllerTests` | ⬜ | ⬜ pending |
| 341-03-T2 | 03 | 3 | ORG-LABEL-05 | — | Audit log entry creation verified per mutation | unit (existing Phase 340 13 [Fact] cover) | `dotnet test --filter OrgLabelServiceTests` | ✅ Phase 340 | ✅ green |
| 341-03-T3 | 03 | 3 | ORG-LABEL-04 (Admin/HC vs Coach 403) | T-341-02 | Role-based 403 verified live | manual curl | login as Coach role → GET `/Admin/ManageOrgLevelLabels` → 403 | ⬜ | ⬜ pending (MANUAL) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements:
- xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 — already installed
- Microsoft.EntityFrameworkCore.InMemory 8.0.0 — installed Phase 340 P3
- `OrgLabelServiceTests.MakeServiceWithCtx` factory — reusable pattern

No new Wave 0 setup required.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Razor view renders correctly with buffer row + Bootstrap modal markup | ORG-LABEL-04 | HTML/CSS visual rendering not amenable to automated assert; `WebApplicationFactory` + headless harness out-of-proportion | Open browser at `http://localhost:5277/Admin/ManageOrgLevelLabels` (Admin role login), confirm: 4 rows table (Level 0/1/2 + buffer Level 3 "(belum diset)"), Edit modal opens Level disabled + Label input, Delete button visible only on highest unused level |
| AJAX fetch POST + toast + reload | ORG-LABEL-04 | Browser DOM + cookie session interaction outside xUnit scope | Submit Edit modal label change → confirm toast renders top-right with success message + page reloads showing new label |
| Admin/HC role access vs Coach 403 | ORG-LABEL-04 | Role-based authz tests need cookie auth Identity harness not present (same precedent as Phase 340 G7/G8) | Login as Coach role → navigate `/Admin/ManageOrgLevelLabels` → expect 403 or redirect to AccessDenied; login as HC role → expect 200 |
| Audit log written per mutation | ORG-LABEL-05 | Already automated in Phase 340 13 [Fact] (`UpdateAsync_KnownLevel_UpdatesRowAndInvalidatesCacheAndLogsAudit` etc.) | N/A — automated coverage exists |
| Native `confirm()` delete dialog | ORG-LABEL-04 | Browser dialog interaction | Click Delete on highest unused level → native confirm appears → OK → row removed |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or documented Manual-Only justification
- [ ] Sampling continuity: no 3 consecutive automated-eligible tasks without automated verify
- [ ] Wave 0 covers all MISSING references (none — existing infra)
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter (planner sets after final task map populated)

**Approval:** pending (skeleton — planner finalizes per-task map after PLAN.md generation).
