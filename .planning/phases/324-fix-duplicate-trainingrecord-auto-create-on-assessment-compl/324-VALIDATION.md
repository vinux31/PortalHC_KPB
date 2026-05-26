---
phase: 324
slug: fix-duplicate-trainingrecord-auto-create-on-assessment-compl
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-26
---

# Phase 324 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + Playwright (TypeScript) untuk e2e |
| **Config file** | `playwright.config.ts` |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `npx playwright test tests/e2e/phase324/` |
| **Estimated runtime** | ~30s build + ~3min Playwright 7-scenario |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (~30s)
- **After Wave 1 (code edit):** `dotnet build` + manual repro lokal (`http://localhost:5277` worker submit)
- **After Wave 2 (Playwright UAT):** `npx playwright test tests/e2e/phase324/` (~3min)
- **After Wave 3 (data cleanup lokal):** SQL count query before/after + manual `/CMP/Records` browser check
- **Before `/gsd-verify-work`:** Full suite green + screenshot D-08/D-09 captured
- **Max feedback latency:** ~3 minutes (Playwright full suite)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 324-01-01 | 01 | 1 | DUPL-01 (code) | — | Build hijau setelah hapus block | unit | `dotnet build` | ✅ existing | ⬜ pending |
| 324-01-02 | 01 | 1 | DUPL-01 (code) | — | `GradingService.cs` line 268-285 deleted | grep | `grep -c "TrainingRecords.Add" Services/GradingService.cs` (expected: 0) | ✅ existing | ⬜ pending |
| 324-01-03 | 01 | 1 | DUPL-01 (code) | — | `AssessmentAdminController.cs:3404-3421` deleted | grep | `grep -c "trExists" Controllers/AssessmentAdminController.cs` (expected: 0) | ✅ existing | ⬜ pending |
| 324-02-01 | 02 | 2 | DUPL-02 (UAT) | — | Worker submit non-essay → 1 row di /CMP/Records | e2e | `npx playwright test tests/e2e/phase324/submit-non-essay.spec.ts` | ❌ W0 | ⬜ pending |
| 324-02-02 | 02 | 2 | DUPL-02 (UAT) | — | PreTest skip TR (regression guard) | e2e | `npx playwright test tests/e2e/phase324/pretest-skip.spec.ts` | ❌ W0 | ⬜ pending |
| 324-02-03 | 02 | 2 | DUPL-02 (UAT) | — | Essay finalize: no TR insert | e2e | `npx playwright test tests/e2e/phase324/essay-finalize.spec.ts` | ❌ W0 | ⬜ pending |
| 324-02-04 | 02 | 2 | DUPL-02 (UAT) | — | AkhiriUjian single → grading jalan, no TR | e2e | `npx playwright test tests/e2e/phase324/akhiri-single.spec.ts` | ❌ W0 | ⬜ pending |
| 324-02-05 | 02 | 2 | DUPL-02 (UAT) | — | AkhiriSemuaUjian bulk → grading jalan untuk semua, no TR | e2e | `npx playwright test tests/e2e/phase324/akhiri-bulk.spec.ts` | ❌ W0 | ⬜ pending |
| 324-02-06 | 02 | 2 | DUPL-02 (UAT) | — | Regrade Pass→Fail: AssessmentSession.IsPassed update, no TR cascade | e2e | `npx playwright test tests/e2e/phase324/regrade-pass-fail.spec.ts` | ❌ W0 | ⬜ pending |
| 324-02-07 | 02 | 2 | DUPL-02 (UAT) | — | Regrade Fail→Pass: NomorSertifikat generate, no TR cascade | e2e | `npx playwright test tests/e2e/phase324/regrade-fail-pass.spec.ts` | ❌ W0 | ⬜ pending |
| 324-03-01 | 03 | 3 | DUPL-03 (data) | — | Schema verify `TrainingRecords.CreatedAt` column existence (RESEARCH A3 open question) | sql | `sqlcmd -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='TrainingRecords' AND COLUMN_NAME='CreatedAt'"` | ✅ existing | ⬜ pending |
| 324-03-02 | 03 | 3 | DUPL-03 (data) | — | SQL count before cleanup (baseline) | sql | `sqlcmd -Q "SELECT COUNT(*) FROM TrainingRecords WHERE Judul LIKE 'Assessment:%' AND {date_filter}"` | ✅ existing | ⬜ pending |
| 324-03-03 | 03 | 3 | DUPL-03 (data) | — | DB backup via sqlcmd BACKUP DATABASE | sql | manual + journal entry | ✅ existing | ⬜ pending |
| 324-03-04 | 03 | 3 | DUPL-03 (data) | — | Cleanup script idempotent (re-run safe) | sql | run twice, count delta = 0 on 2nd run | ✅ existing | ⬜ pending |
| 324-03-05 | 03 | 3 | DUPL-03 (data) | — | SQL count after cleanup = 0 | sql | same query, expected 0 | ✅ existing | ⬜ pending |
| 324-04-01 | 04 | 3 | DUPL-04 (IT) | — | `docs/DB_HANDOFF_IT_2026-05-26.html` exists with template match | file | `test -f docs/DB_HANDOFF_IT_2026-05-26.html && grep -q "var(--brand)" docs/DB_HANDOFF_IT_2026-05-26.html` | ✅ existing | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/phase324/` folder + 7 spec files (one per UAT scenario)
- [ ] `tests/e2e/helpers/phase324.ts` — login helper, submit helper, records-page assertion helper (reuse pattern Phase 322)
- [ ] `docs/sql/cleanup-2026-05-26-trainingrecord-duplicates.sql` — idempotent script
- [ ] `docs/DB_HANDOFF_IT_2026-05-26.html` — IT handoff doc (template fork 2026-05-13)
- [ ] Existing: `playwright.config.ts`, `dotnet build` infrastructure, sqlcmd CLI (lokal dev env)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pre-fix repro screenshot 2-row state | DUPL-05 (D-08) | Visual proof bug existed | Sebelum apply fix di branch: login worker → submit assessment biasa → buka /CMP/Records → screenshot 2 row "Assessment Online" + "Training Manual" untuk event sama. Simpan ke `docs/screenshots/phase324/before-fix.png`. |
| Post-fix verify screenshot 1-row state | DUPL-05 (D-09) | Visual proof fix works | Setelah Wave 1 + Wave 3 lokal: ulang flow worker (test data baru post-fix), screenshot 1 row "Assessment Online". Simpan ke `docs/screenshots/phase324/after-fix.png`. |
| IT eksekusi Dev cleanup verify | DUPL-04 (D-06) | IT team responsibility | IT eksekusi handoff HTML doc di Dev (10.55.3.3), kirim screenshot/log balik ke developer untuk arsip di SEED_JOURNAL.md. |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (7 Playwright spec + helper + SQL + HTML)
- [ ] No watch-mode flags
- [ ] Feedback latency < 180s (Playwright full suite)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
