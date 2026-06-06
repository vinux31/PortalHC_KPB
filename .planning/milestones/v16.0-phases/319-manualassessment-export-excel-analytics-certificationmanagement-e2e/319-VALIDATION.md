---
phase: 319
slug: manualassessment-export-excel-analytics-certificationmanagement-e2e
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-05-12
---

# Phase 319 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Phase 319 = admin features E2E (ManualAssessment + ManageCategories + Export Excel + Analytics + CertificationManagement). Tests = Playwright E2E append ke `tests/e2e/exam-types.spec.ts`.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.49+ (existing — Phase 315-318 foundation) |
| **Config file** | `tests/playwright.config.ts` |
| **Quick run command** | `cd tests && npx playwright test exam-types --grep "FLOW [TUVWX]"` |
| **Full suite command** | `cd tests && npx playwright test exam-types.spec.ts --reporter=list` |
| **Estimated runtime** | ~4-5 minutes (49 Phase 318 baseline + 24 Phase 319 new = ~73 cumulative) |

---

## Sampling Rate

- **After every task commit:** Run grep'd FLOW subset (`--grep "FLOW T"` / `"FLOW U"` / etc.)
- **After every plan wave:** Run full Plan suite (`--grep "FLOW T|FLOW U"` etc.)
- **Before `/gsd-verify-work`:** Full `exam-types.spec.ts` must be green (≥73/73)
- **Max feedback latency:** 60s per sub-test; 5 minutes full suite

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 319-01-01 | 01 | 1 | QA-09 | T-319-01 / — | Helper signatures + typed shapes exported | TypeScript compile | `cd tests && npx tsc --noEmit` | ✅ | ⬜ pending |
| 319-01-02 | 01 | 1 | QA-09 | T-319-02 / auth gating | W0.T0 TomSelect smoke OK | E2E | `cd tests && npx playwright test exam-types --grep "W0.T0"` | ❌ W0 | ⬜ pending |
| 319-01-03 | 01 | 1 | QA-09 | T-319-03 / DB INSERT verify | FLOW T 6 sub-tests HIJAU | E2E | `cd tests && npx playwright test exam-types --grep "FLOW T"` | ❌ W0 | ⬜ pending |
| 319-02-01 | 02 | 2 | QA-09 | T-319-04 / duplicate-name reject | FLOW U 4 sub-tests HIJAU | E2E | `cd tests && npx playwright test exam-types --grep "FLOW U"` | ❌ W0 | ⬜ pending |
| 319-03-01 | 03 | 3 | QA-09 | T-319-05 / Excel byte threshold | W0.V0 + W0.W0 + V1-V3 + W1-W4 HIJAU | E2E | `cd tests && npx playwright test exam-types --grep "FLOW V\|FLOW W\|W0.V0\|W0.W0"` | ❌ W0 | ⬜ pending |
| 319-04-01 | 04 | 4 | QA-09 | T-319-06 / CDP variant resolution | W0.X0 + X1-X3 HIJAU | E2E | `cd tests && npx playwright test exam-types --grep "FLOW X\|W0.X0"` | ❌ W0 | ⬜ pending |
| 319-04-02 | 04 | 4 | QA-09 | — | REQUIREMENTS QA-09 + ROADMAP Phase 319 sync | docs grep | `grep -c "QA-09" .planning/REQUIREMENTS.md` ≥2 | ✅ | ⬜ pending |
| 319-04-03 | 04 | 4 | QA-09 | — | Final regression gate ≥73/73 | E2E | `cd tests && npx playwright test exam-types.spec.ts --reporter=list` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] **W0.T0** (Plan 01) — TomSelect `#WorkerSelect` interaction smoke (verify Pattern 1 reliable; fallback to `page.evaluate` kalau Pattern 2 needed)
- [ ] **W0.V0** (Plan 03) — `/AssessmentAdmin/ExportCategoriesExcel` endpoint reachable + content-type confirms `.xlsx` MIME + bytes>2048
- [ ] **W0.W0** (Plan 03) — Analytics JSON response shape captured (verify camelCase serialization, datasets/labels structure)
- [ ] **W0.X0** (Plan 04) — `/CDP/CertificationManagement` page loads HTTP 200 + DOM markers present

*If Wave 0 fails: research deviation recorded, fallback strategies documented per RESEARCH §811-823.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ManualAssessment edit form validation messages render | QA-09 | UI feedback timing-sensitive | HC: nav `/TrainingAdmin/EditManualAssessment/{id}` → submit invalid score 150 → verify `.text-danger` inline OR `.alert-warning` |
| Analytics chart visual rendering | QA-09 | Chart.js v4 canvas pixel inspection out-of-scope automated | HC: nav `/CMP/AnalyticsDashboard` → visually confirm horizontal bar chart legible + tooltips work on hover |
| Export Excel content correctness | QA-09 | File parsing requires `xlsx` library dependency | HC: download `/AssessmentAdmin/ExportCategoriesExcel` → open di Excel → verify headers + rows match DB |
| CertificationManagement CDP filter UX | QA-09 | Form interaction timing | HC: nav `/CDP/CertificationManagement` → input filter judul → verify result narrows correctly |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (W0.T0, W0.V0, W0.W0, W0.X0 cover YELLOW A1-A4 per RESEARCH)
- [x] Sampling continuity: 4 plans × ≥1 automated verify each — no 3 consecutive tasks without automated verify
- [x] Wave 0 covers MISSING references (Excel endpoint discovery, Analytics shape, CDP variant)
- [x] No watch-mode flags (`--reporter=list` only)
- [x] Feedback latency < 60s per sub-test, 5 minutes full suite
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-05-12 (plan-phase orchestrator)
