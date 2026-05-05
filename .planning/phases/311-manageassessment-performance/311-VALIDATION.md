---
phase: 311
slug: manageassessment-performance
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-05
---

# Phase 311 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Phase 311 = backend perf optimization. Test infrastructure: .NET build (compile gate) + Playwright TypeScript E2E (smoke parity). No .NET test project di repo, jadi unit-test verification scope = build + manual UAT + Playwright smoke.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build 8.x (compile gate) + Playwright 1.x TypeScript E2E (smoke) |
| **Config file** | `HcPortal.csproj` + `tests/playwright.config.ts` |
| **Quick run command** | `dotnet build --nologo -v q` (≤ 10s when warm) |
| **Full suite command** | `dotnet build && cd tests && npx playwright test --grep "ManageAssessment\|Phase 311" --reporter=list` |
| **Estimated runtime** | dotnet build ~10s warm / ~30s cold; Playwright smoke ~30s |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --nologo -v q` (compile gate ≤ 92 warnings, 0 errors — Phase 309 baseline preserved)
- **After every plan wave:** Run smoke: `dotnet build && cd tests && npx playwright test --grep "ManageAssessment" --list`
- **Before `/gsd-verify-work`:** Full suite must be green + manual UAT walkthrough Step-by-step pre/post baseline numbers documented
- **Max feedback latency:** 60 seconds (build + Playwright list)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 311-01-01 | 01 | 0 | PERF-01 | — | Baseline T1..T5 captured before patch | manual | `dotnet run` + curl + grep `_logger.LogInformation("ManageAssessment perf breakdown` di log output | ❌ W0 | ⬜ pending |
| 311-01-02 | 01 | 0 | PERF-01 | — | STOP gate Skenario A/B/C decision committed in artifact | manual | Read `.planning/phases/311-manageassessment-performance/311-BASELINE.md` confirm decision recorded | ❌ W0 | ⬜ pending |
| 311-02-01 | 02 | 1 | PERF-01 SC #4 | — | Migration generated dengan 2 indexes (D-05, D-06) | unit | `grep -E "IX_AssessmentSessions_(ExamWindowCloseDate\|LinkedGroupId)" Migrations/*_AddManageAssessmentPerfIndexes.cs` | ✅ | ⬜ pending |
| 311-02-02 | 02 | 1 | PERF-01 SC #4 | — | Migration applied ke Dev DB | unit | `dotnet ef migrations list \| grep AddManageAssessmentPerfIndexes` shows applied | ✅ | ⬜ pending |
| 311-02-03 | 02 | 1 | PERF-01 SC #2 | — | `.AsNoTracking()` ditambahkan di managementQuery L66 | unit | `grep -n "AsNoTracking" Controllers/AssessmentAdminController.cs` ≥ 1 occurrence di action ManageAssessment | ✅ | ⬜ pending |
| 311-02-04 | 02 | 1 | PERF-01 SC #3 | — | `.Include(a => a.User)` L88 dihapus | unit | `grep -n "Include(a => a.User)" Controllers/AssessmentAdminController.cs` di action ManageAssessment = 0 occurrence | ✅ | ⬜ pending |
| 311-02-05 | 02 | 1 | PERF-01 SC #5 | — | IMemoryCache `GetOrCreateAsync("assessment_categories_distinct", ...)` ditambahkan dengan TTL 5 menit | unit | `grep -n "assessment_categories_distinct" Controllers/AssessmentAdminController.cs` ≥ 1 + `grep "AbsoluteExpirationRelativeToNow"` di nearby lines | ✅ | ⬜ pending |
| 311-02-06 | 02 | 1 | PERF-01 D-03 | — | `_cache.Remove("assessment_categories_distinct")` di AddCategory/EditCategory/DeleteCategory actions | unit | `grep -c "_cache.Remove(\"assessment_categories_distinct\")" Controllers/AssessmentAdminController.cs` ≥ 3 | ✅ | ⬜ pending |
| 311-02-07 | 02 | 1 | PERF-01 | — | Build success: 0 errors, ≤ 92 warnings (preserve Phase 309 baseline) | unit | `dotnet build --nologo \| grep -E "(0 Error\|[0-9]+ Warning)"` | ✅ | ⬜ pending |
| 311-02-08 | 02 | 1 | PERF-01 SC #1, SC #6 | — | Post-patch measurement: T1..T5 + total p95 dengan baseline data | manual | Run pre/post 5x, compute (baseline_p95 - postpatch_p95) / baseline_p95 ≥ 0.30 documented di SUMMARY.md | ❌ W0 | ⬜ pending |
| 311-02-09 | 02 | 1 | PERF-01 SC #7 | — | Smoke test parity: tab=assessment + tab=training + tab=history page load 200 OK + grouping count identik pre/post | E2E + manual | `npx playwright test --grep "ManageAssessment smoke parity"` PASS + manual checklist | ⚠️ Wave 0 may need new test | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] **`311-BASELINE.md`** — artifact untuk capture baseline T1..T5 numbers + Skenario A/B/C decision (created Wave 0 Task 1)
- [ ] **Stopwatch instrumentation per-segment** — 5 Stopwatch instances inline di action ManageAssessment (T1=Assessment query L66-110, T2=GetWorkersInSection L210, T3=GetAllWorkersHistory L212, T4=GetAllSectionsAsync+GetUnitsForSectionAsync L220-223, T5=Distinct Categories L172-176). Logging via `_logger.LogInformation("ManageAssessment perf breakdown: T1={T1}ms T2={T2}ms ... total={Total}ms tab={Tab}", ...)` (created Wave 0 Task 1; D-13 says permanent — not removed post-validation)
- [ ] **`tests/e2e/manageassessment.smoke.spec.ts`** — Playwright smoke test 3 tab parity (kalau belum ada existing spec covering ManageAssessment)
- [ ] **STOP gate human verification** — orchestrator pause execute setelah Wave 0 baseline complete, surface T1..T5 numbers ke user, user confirm Skenario A (proceed Wave 1) atau Skenario B/C (STOP, expand scope discuss)

*If none: tidak applicable — Phase 311 wajib Wave 0 baseline-first per D-16.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pre-patch baseline T1..T5 measurement | PERF-01 SC #1 | Butuh local app `dotnet run`, login admin, navigate ke `/Admin/ManageAssessment` (5x cold filter), capture log entries from Serilog/Console output | (1) `dotnet run --no-launch-profile` (port 5000) (2) Login `admin@pertamina.com` (UseActiveDirectory=false untuk test) (3) Curl/browser hit `/Admin/ManageAssessment?tab=assessment&page=1&pageSize=20` 5x (4) Grep log untuk "ManageAssessment perf breakdown" entries (5) Skip first run (JIT warmup), record median per segment di `311-BASELINE.md` |
| STOP gate decision (Skenario A/B/C) | PERF-01 D-16 | Decision membutuhkan user judgment dengan data konkret di tangan — gak bisa automated | User review T1..T5 numbers, decide: A (T1 dominan >60% → proceed Wave 1) / B (T2/T3 dominan >50% → STOP, expand scope decision) / C (mixed → user decide explicit). Approval signature di `311-BASELINE.md` |
| Post-patch p95 measurement | PERF-01 SC #6 | Same infrastructure as baseline (manual app run + log grep) | Same as pre-patch + compute improvement ratio. Document hasil di `311-02-SUMMARY.md` Section "Performance Results" |
| Smoke test parity (3 tabs) | PERF-01 SC #7 | Visual verification via browser (grouping count + pagination headers identik) — Playwright smoke covers programmatic parity tapi visual checklist bisa surface CSS regression atau Razor view shape change | (1) Pre-patch screenshot tab=assessment, tab=training, tab=history (2) Post-patch identical screenshots (3) Diff visually (no diff tooling required — eyeball OK karena perf phase tidak touch view) |
| Cache invalidation behavior | PERF-01 D-03 | E2E test untuk Category CRUD invalidation requires fresh AddCategory→ManageAssessment→EditCategory→ManageAssessment cycle dengan time-bounded assertion | Manual: (1) Add new category via /Admin/ManageCategories (2) Navigate ke /Admin/ManageAssessment, confirm dropdown shows new category immediately (no 5min wait) (3) Edit category name, confirm change appears immediately. Documented di `311-UAT.md` |

*If none: not applicable — Phase 311 manual UAT scope significant karena perf measurement + cache invalidation behavior butuh integration testing.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify atau Wave 0 dependencies (Tasks 311-01-01, 311-01-02, 311-02-08 = manual scope, sisanya automated grep/build)
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify (Wave 1 Tasks 311-02-01 sampai 311-02-07 semua automated, Task 311-02-08 manual = 1 manual after 7 automated, OK)
- [ ] Wave 0 covers all MISSING references (baseline artifact + Stopwatch instrumentation + smoke spec)
- [ ] No watch-mode flags (Playwright `--reporter=list`, dotnet build no `--watch`)
- [ ] Feedback latency < 60s (dotnet build ~10s + Playwright list ~10s)
- [ ] `nyquist_compliant: true` set in frontmatter (set saat planner finalize plans)

**Approval:** pending — awaiting Wave 0 instrumentation tasks created in PLAN.md.

---

## Nyquist 8-Dimension Coverage

| # | Dimension | Coverage Plan |
|---|-----------|---------------|
| 1 | **Functional** | Smoke test parity 3 tabs (Tasks 311-02-09); ViewBag shape preservation verified via grep `ViewBag.ManagementData/Categories/etc.` count pre/post |
| 2 | **Boundary** | Edge cases: search="" (no filter), category=null (all), statusFilter=All vs Open vs null (default exclude Closed). Smoke E2E + manual UAT cover. |
| 3 | **Error** | EF Core query failure: dataset 0 rows (manual UAT happy path), DB unreachable (out of scope — infra phase). Cache miss → factory exception → fallback. |
| 4 | **Concurrency** | IMemoryCache stampede risk documented (Pitfall 4 from RESEARCH.md). Decision: accept stampede (admin endpoint low concurrency). Verified via Assumption A7. NO code-side test untuk stampede karena .NET 8 IMemoryCache TIDAK punya native protection. |
| 5 | **Idempotency** | Cache invalidation idempotency: `_cache.Remove(key)` aman dipanggil multiple times (no exception). Post-CRUD invalidation di 3 actions (Add/Edit/Delete) = 1 call each, no double-invalidate concern. |
| 6 | **Backward Compat** | ViewBag shape preservation = HARD requirement. Compile gate (dotnet build) + Razor view rendering smoke (Playwright tab=assessment hits `/Admin/ManageAssessment`, expects no `MissingMemberException` di view). Grep `ViewBag.\\(ManagementData\\|Categories\\|SelectedCategory\\|ActiveTab\\|...\\)` count harus identik pre/post. |
| 7 | **Performance** | PRIMARY DIMENSION — pre/post baseline measurement (D-12 protocol: 5 runs cold, skip first JIT warmup, p95 from runs 2-5). Acceptance: `(baseline_p95 - postpatch_p95) / baseline_p95 ≥ 0.30`. D-16 breakdown identifies bottleneck per-segment to ensure optimization targets correct path. |
| 8 | **Validation Coverage** | Self-referential — this VALIDATION.md per se. nyquist_compliant flag set true saat all 7 prior dimensions diaddress oleh tasks di PLAN.md. |

---

## Dependencies

- Read `.planning/phases/311-manageassessment-performance/311-RESEARCH.md` Section "Validation Architecture" untuk full Nyquist breakdown
- Read `.planning/phases/311-manageassessment-performance/311-CONTEXT.md` D-11..D-16 untuk measurement methodology lock
- Read `.planning/REQUIREMENTS.md` PERF-01 untuk authoritative acceptance criteria

---

*Generated: 2026-05-05 — automatic from RESEARCH.md "## Validation Architecture" section trigger*
