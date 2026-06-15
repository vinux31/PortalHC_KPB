---
phase: 364
slug: restore-baseline-regresi-e2e-exam
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-11
validated: 2026-06-12
---

# Phase 364 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Test-only phase: the deliverable IS the automated regression tests (the two e2e specs). Validation = those specs + the xUnit suite run green/documented.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (e2e, tests/) + xUnit (HcPortal.Tests) |
| **Config file** | tests/playwright.config.ts |
| **Quick run command** | `npx playwright test exam-taking.spec.ts exam-types.spec.ts --workers=1` (dari tests/, app live @5277, AD off + `lpc:` conn override) |
| **Full suite command** | `dotnet test HcPortal.Tests` + 2 spec target full run @localhost:5277 |
| **Estimated runtime** | e2e 2 spec ~5 menit (`--workers=1`); dotnet test ~30 detik |

> ⚠️ Harness gap: config sets no `workers` → multi-file run defaults to 2 workers = pecah isolasi DB shared (BACKUP/seed/RESTORE tunggal). Gate WAJIB `--workers=1`. Recommend pin `workers: 1`. Env: app butuh `lpc:` shared-memory conn override + SQLBrowser running (lihat 364-03-SUMMARY / reference_local_e2e_sql_env_fix).

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error (e2e run hanya pada gate task — mahal)
- **After every plan wave:** run spec yang disentuh wave tsb @5277 (`--workers=1`)
- **Before `/gsd-verify-work`:** kedua spec PASS/documented 1x full run + `dotnet test` hijau (D-08/D-15)
- **Max feedback latency:** ~5 menit (full e2e 2 spec, `--workers=1`)

---

## Per-Task Verification Map

| Req (SC) | Plan | Behavior | Test Type | Automated Command | Evidence | Status |
|----------|------|----------|-----------|-------------------|----------|--------|
| SC#1 — titles comply REST-06 | 364-02 | semua standard-create lolos validator (no reject) | e2e + grep | `npx playwright test exam-types.spec.ts --workers=1` | exam-types creates PASS (W0/K1/L1…); grep `uniqueTitle('Pre Test ` = 10+10 | ✅ green |
| SC#2 — fix per-flow, FLOW P exempt | 364-01 / 03 | baseline D-10 klasifikasi + FLOW P (PrePost) lolos exempt | e2e + diagnostic | gate run + 364-01-SUMMARY | FLOW P PASS; baseline table di 364-01-SUMMARY | ✅ green |
| SC#3 — LinkedGroupId isolation (D-11) | 364-02 | `AssessmentSessions.LinkedGroupId IS NULL` utk auto-pair Phase 338 | e2e DB assert | FLOW K K1 `expect(linkedNull).toBe(1)` | gate K1 PASS | ✅ green |
| SC#4 — both pass OR documented (D-09) | 364-03 | exam-types restored; exam-taking + L6 documented non-judul | e2e + dotnet | `... --workers=1` + `dotnet test HcPortal.Tests` | e2e 78 pass / 77 skip / 0 fail; dotnet 227/227 | ✅ documented |
| D-15 gate | 364-03 | satu full run + xUnit hijau | e2e + dotnet | gate commands above | e2e 0 fail + dotnet 227/227 | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · ✅ documented (SC#4 alternative path D-09)*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — Playwright config + global setup/teardown (BACKUP/RESTORE DB otomatis) + dbSnapshot helper sudah ada. Tidak ada framework baru. Phase 364 menambah 1 asersi DB (D-11) + reuse lifecycle yang ada.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions / Status |
|----------|-------------|------------|----------------------------|
| Baseline diagnosa pre-edit (D-10) | SC#2 | Run e2e as-is → klasifikasi failure judul vs non-judul (hasil dicatat, bukan asersi) | DONE — 364-01-SUMMARY classification table (exam-types W0=TITLE, exam-taking A1=NON-TITLE) |

---

## Deferred (tracked, NOT a Nyquist gap)

Automated-coverage deferrals — backlogged, di luar scope Phase 364 (SC#4 documented-path D-09):

| Item | Backlog | Why deferred |
|------|---------|--------------|
| exam-taking 10 create flows (A–J) `test.fixme` | **999.7** | `/Admin/CreateAssessment` kini wizard 4-langkah; flat-form create usang → butuh migrasi wizard-nav (bukan test-gen, itu rework flow). |
| exam-types L6 essay finalize `test.fixme` | **999.8** | Production bug suspect (`AssessmentSessions.Score`=0 setelah grade+finalize). Produksi TIDAK diubah (D-06) → diagnosa di backlog. |
| exam-taking Phase 313 block | — | Self-skip tanpa `.planning/seeds/313-timer-fixtures.sql` (REQ TMR-01, terpisah dari matrix seed). |

---

## Validation Audit 2026-06-12

| Metric | Count |
|--------|-------|
| In-scope requirements (SC#1-4 + D-15) | 5 |
| COVERED (automated/documented green) | 5 |
| MISSING (test-gen needed) | 0 |
| Gaps filled this audit | 0 (no auditor spawn — every in-scope SC already has automated/documented verification) |
| Deferred to backlog (999.7 / 999.8) | 2 flow-groups (not Nyquist gaps — accepted SC#4 documented path) |

**Verdict:** Nyquist-compliant for phase scope. exam-types is the restored automated regression baseline; exam-taking automated coverage resumes after the 999.7 wizard migration.
