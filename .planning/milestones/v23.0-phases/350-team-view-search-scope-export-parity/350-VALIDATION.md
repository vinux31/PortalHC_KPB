---
phase: 350
slug: team-view-search-scope-export-parity
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-05
---

# Phase 350 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Derived from 350-RESEARCH.md §Validation Architecture (commit aec1e8ba).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework (unit)** | xUnit 2.9.3 + EF Core InMemory 8.0.0 (`HcPortal.Tests`) |
| **Framework (e2e)** | @playwright/test ^1.58.2 (`tests/`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (unit) · `tests/playwright.config.ts` (e2e) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` |
| **Full suite command** | `dotnet test` (105/105 baseline per Phase 349) |
| **e2e command** | `cd tests && npx playwright test cmp-records-350.spec.ts` |
| **Estimated runtime** | unit ~1–3s (InMemory) · e2e ~30–60s + seed snapshot/restore |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` (fast, InMemory)
- **After every plan wave:** Run `dotnet test` (full xUnit — target green ≥105 + 4 new)
- **Before `/gsd-verify-work`:** Full `dotnet test` green + `npx playwright test cmp-records-350.spec.ts` green (or Playwright MCP manual UAT if runner env-blocked) + `dotnet run` localhost:5277 manual verify per CLAUDE.md Develop Workflow
- **Max feedback latency:** ~3 seconds (unit quick run)

---

## Per-Task Verification Map

> Plan/task IDs finalized by planner. Mapping below is requirement→test seeded from RESEARCH §Phase Requirements → Test Map.

| Plan area | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|-----------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| Search predicate | 1 | SF-01 | T-350-AC (preserve V4 section-lock) | Search broadens *what* is matched, NOT *who* may view (section-lock untouched) | unit | `dotnet test --filter Scope_Training_FiltersByAssessmentTitle` | ❌ W0 add | ⬜ pending |
| Search predicate | 1 | SF-01 | — | union Nama/NIP ∪ Training ∪ Assessment | unit | `dotnet test --filter Scope_Keduanya_Union_IncludesAssessment` | ❌ W0 add | ⬜ pending |
| Search predicate (D-07) | 1 | SF-01 | — | Badge counts (`CompletedAssessments`/`TotalTrainings`) unchanged by search | unit | `dotnet test --filter Search_DoesNotMutate_BadgeCounts_D07` | ❌ W0 add | ⬜ pending |
| Export parity | 2 | SF-06 | T-350-IDOR (preserve L4 lock) | Export worker-list non-empty for assessment-title search; same scope as on-screen | unit | `dotnet test --filter Keduanya_AssessmentTitle_ReturnsWorker_ForExport` | ❌ W0 add | ⬜ pending |
| Export Category narrow | 2 | SF-06 | — | Current sessions narrowed by Category; archived (no Category) dropped when Category set | unit/manual | controller-level — integration or Playwright XLSX parse (stretch) | ❌ W0 optional | ⬜ pending |
| Copy + UAT | 1–2 | SF-01, SF-02 | — | "Judul Kegiatan" option + honest placeholder; search assessment-title → worker appears; export href carries `searchScope`/`search` | e2e | `npx playwright test cmp-records-350.spec.ts` | ❌ W0 add | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — +4 `[Fact]` (SF-01 ×2, D-07 badge-invariant ×1, SF-06 export-list ×1). File EXISTS; helper `Session(...)` already has `.Title`. (Optional: add `Session(id,user,status,isPassed,title,category)` overload — D-08 Claude's Discretion.)
- [ ] `tests/e2e/cmp-records-350.spec.ts` — NEW (model `cmp-records-346.spec.ts:122-149`; reuse `loginAny`, `accounts`, `dbSnapshot`)
- [ ] `tests/sql/cmp350-seed.sql` — NEW (model `cmp346-seed.sql`; seed 1 assessment with distinct title e.g. "[PENDING350] OJT v14.2" for a worker in a section accessible to `manager`/`hc` login; classification temporary + local-only; SEED_JOURNAL append; restore afterAll)
- [ ] Framework install: NONE — xUnit + Playwright already installed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual: dropdown shows "Judul Kegiatan" + honest placeholder renders correctly on Team View | SF-02 | Visual copy render (covered by Playwright text-assert, but final eyeball per Develop Workflow) | `dotnet run` → localhost:5277 → CMP/Records Team View → inspect dropdown option text + search placeholder |
| Export Assessment XLSX content rows match on-screen (full WYSIWYG incl. Category drop-archived) | SF-06 | XLSX content parse is a stretch test; eyeball faster | Apply Category filter + search assessment-title → click Export Assessment → open .xlsx → confirm rows = on-screen workers, archived rows absent when Category set |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (3 new test artifacts)
- [ ] No watch-mode flags
- [ ] Feedback latency < 3s (unit quick run)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
