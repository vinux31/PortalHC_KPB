---
phase: 351
slug: worker-detail-cross-surface-filter-consistency
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-05
---

# Phase 351 — Validation Strategy

> Per-phase validation contract. Derived from 351-RESEARCH.md §Validation Architecture (commit 71036e03).
> Mostly view/JS — Playwright is the primary automated path; SF-04 option-building is the only unit-testable slice.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework (unit)** | xUnit 2.9.3 + EF Core InMemory 8.0.0 (`HcPortal.Tests`) |
| **Framework (e2e)** | @playwright/test (`testDir: ./e2e`, `baseURL: http://localhost:5277`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (unit) · `tests/playwright.config.ts` (e2e) |
| **Quick run command** | `dotnet build` + `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **e2e command** | `cd tests && npx playwright test e2e/cmp-records-351.spec.ts` (after `dotnet run` on :5277) |
| **Estimated runtime** | unit ~1–3s · e2e ~30–60s + seed snapshot/restore |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + `dotnet test HcPortal.Tests` (full unit suite, currently 109/109)
- **After every plan wave:** `dotnet run` on :5277 + targeted `cmp-records-351.spec.ts` group
- **Before `/gsd-verify-work`:** full `dotnet test` green + `cmp-records-351.spec.ts` green + Phase 346/350 regression green + `dotnet run` localhost:5277 eyeball (CLAUDE.md Develop Workflow)
- **Max feedback latency:** ~3 seconds (unit quick run)

---

## Per-Task Verification Map

> Plan/task IDs finalized by planner. Seeded from RESEARCH §Phase Requirements → Test Map.

| Plan area | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|-----------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| Worker Detail counter+empty-state | 1 | SF-03 | — | view-only; no auth/data change | e2e | `npx playwright test e2e/cmp-records-351.spec.ts -g "0-match"` | ❌ W0 add | ⬜ pending |
| Kategori actual-distinct options | 1 | SF-04 | T-351-AC (preserve RecordsWorkerDetail authz) | Options from same authorized `unifiedRecords`; authz `:543-556` byte-identical | unit (if helper extracted) + e2e | `dotnet test --filter ActualCategories` + Playwright | ❌ W0 add | ⬜ pending |
| My Records parity (Kategori+Tipe+data-category) | 1 | SF-05 | — | Tipe value-map = surface `data-type` (assessment/training), NOT "Assessment Online" | e2e | `npx playwright test ... -g "parity"` | ❌ W0 add | ⬜ pending |
| Back-nav → Team tab activator | 1 | SF-07 | — | sessionStorage-primary; Records.cshtml #team→tab activator (~5 lines); RecordsTeam untouched | e2e | `npx playwright test ... -g "back-nav"` | ❌ W0 add | ⬜ pending |
| Regression | gate | — | — | Phase 346/350 Team View + My Records counters unbroken | e2e | `npx playwright test e2e/cmp-records-346.spec.ts e2e/cmp-records-350.spec.ts` | ✅ exists | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/cmp-records-351.spec.ts` — NEW; SF-03 (0-match message+counter) + SF-04 (legacy-Kategori filters) + SF-05 (My Records Kategori/Tipe parity, Tipe value-map) + SF-07 (Back → Team tab active + restored dateFrom/searchScope). Model `cmp-records-350.spec.ts`; reuse `loginAny`/`accounts`/`dbSnapshot`.
- [ ] `tests/sql/cmp351-seed.sql` — NEW; `[PENDING351]` worker record with free-text Kategori NOT in master `AssessmentCategories` (e.g. `'Legacy-FreeText-351'`) to prove SF-04 option appears + row filters. temporary+local-only; SEED_JOURNAL; restore afterAll.
- [ ] (Optional) xUnit `[Fact]`s for `BuildActualCategories(IEnumerable<UnifiedTrainingRecord>)` IF the SF-04 option helper is extracted (records `"OJT"`/`"ojt"`/`"Legacy"`/null/"" → `["Legacy","OJT"]` case-insensitive dedupe ordered).
- [ ] Confirm `tests/e2e/helpers/accounts.ts` + `dbSnapshot.ts` + `e2e/global.setup.ts` exist locally (referenced by committed specs; used successfully in Phase 350 e2e run).
- [ ] Framework install: NONE — xUnit + Playwright already wired.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| SF-03 counter/empty-state visual + aria-live announce | SF-03 | DOM-render + SR announce; Playwright asserts DOM but final eyeball per Develop Workflow | `dotnet run` :5277 → Worker Detail → filter to 0 → "Tidak ada hasil untuk filter ini." + "Menampilkan 0 dari Y" |
| SF-07 full round-trip feel (date range + scope survive Back) | SF-07 | UX continuity across navigation | Team View → set dateFrom/dateTo/searchScope → open worker → Back to Team View → all filters intact + Team tab active |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (spec + seed)
- [ ] No watch-mode flags
- [ ] Feedback latency < 3s (unit quick run)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
