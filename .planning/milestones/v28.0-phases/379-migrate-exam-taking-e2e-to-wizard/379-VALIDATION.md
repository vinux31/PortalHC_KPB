---
phase: 379
slug: migrate-exam-taking-e2e-to-wizard
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-14
validated: 2026-06-14
---

# Phase 379 — Validation Strategy

> Per-phase validation contract. **Untuk fase ini, suite e2e ADALAH deliverable** —
> validasi = suite `exam-taking.spec.ts` berjalan HIJAU (`--workers=1`). Bukti: `379-RUN-EVIDENCE.md`.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (TypeScript) — target suite migrasi |
| **Config file** | `tests/playwright.config.ts` · helpers `tests/e2e/helpers/examTypes.ts` + `wizardSelectors.ts` |
| **Quick run command** | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1 -g "Flow K"` |
| **Full suite command** | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` |
| **Measured runtime** | 6.4m (serial, A-K + 313; G timer deterministik ~1.0m) |
| **Env prasyarat** | SQLBrowser + `MSSQL$SQLEXPRESS` Running + `Authentication__UseActiveDirectory=false dotnet run` (app @localhost:5277 Development). `lpc:` override TIDAK diperlukan. |

---

## Sampling Rate

- **After every flow migrated:** run `--workers=1 -g "Flow X"` → hijau sebelum lanjut (Plan 02-05, dilakukan).
- **After helper extension:** subset pakai extension (B token, E proton, G timer, K essay) — dilakukan.
- **Phase gate (D-03):** FULL suite hijau, bukti `379-RUN-EVIDENCE.md` (75 passed, 7 skip-313, 0 fail).

---

## Per-Task Verification Map

| Flow | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | Status |
|------|------|------|-------------|-----------------|-----------|-------------------|--------|
| A Legacy lifecycle | 02 | 2 | E2E-01 (SC1) | uniqueTitle isolasi + teardown restore | e2e | `... -g "Flow A"` (16/16) | ✅ green |
| B Token | 02 | 2 | E2E-01 (SC1) | token extension additive, no prod surface | e2e | `... -g "Flow B"` (6/6) | ✅ green |
| C Force-close 2 worker | 02 | 2 | E2E-01 (SC1) | AkhiriUjian kebab, no prod change | e2e | `... -g "Flow C"` (8/8) | ✅ green |
| D Paste-import + reshuffle | 03 | 3 | E2E-01 (SC1) | importQuestionsViaPaste additive | e2e | `... -g "Flow D"` (8/8) | ✅ green |
| E Proton T3 interview (FULL) | 03 | 3 | E2E-01 (SC1, D-02) | seed eligibility temporary local-only (Bypass), restore Plan 06; antiforgery auto | e2e | `... -g "Flow E"` (5/5, no skip) | ✅ green |
| F Multi-worker | 04 | 4 | E2E-01 (SC1) | iwan3 assigned, teardown restore | e2e | `... -g "Flow F"` (7/7) | ✅ green |
| G Timer expired (deterministik) | 04 | 4 | E2E-01 (SC1, D-03) | waitForFunction event-driven (no 70s sleep) | e2e | `... -g "Flow G"` (4/4) | ✅ green |
| H Real-time monitoring | 04 | 4 | E2E-01 (SC1, D-03) | auto-retry assert (no 12s sleep) + JSON poll | e2e | `... -g "Flow H"` (9/9) | ✅ green |
| I Edit assessment | 05 | 5 | E2E-01 (SC1) | edit-form flat SURVIVE | e2e | `... -g "Flow I"` (6/6) | ✅ green |
| J Abandon & reset | 05 | 5 | E2E-01 (SC1) | reset via kebab, teardown restore | e2e | `... -g "Flow J"` (9/9) | ✅ green |
| **K Essay GRADE-01** | 05 | 5 | E2E-01 (**SC3**) | DB-scalar localhost-only guard; assert Score===80 (bukti fix 376) | e2e + DB-assert | `... -g "Flow K"` (7/7, K6 Score=80) | ✅ green |
| FULL suite (gate) | 06 | 6 | E2E-01 (**SC2**) | full serial, teardown RESTORE | e2e | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` | ✅ **75 passed / 7 skip-313 / 0 fail** |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] **W0-1 ProtonTrack Tahun 3** (Flow E, D-02): COUNT=2 (track ADA). **Gap ditemukan Plan 03:** eligibility coachee ≠ existence → seed 1-baris assignment Bypass (track interview-only 0-deliverable + Phase 360 cross-year exempt). Restored Plan 06 (count Bypass=0). (Open-Q1 + lanjutan)
- [x] **W0-2 paste-import route** (Flow D3): VALID — `ImportPackageQuestions.cshtml` `textarea[name="pasteText"]` (tab kedua); helper `importQuestionsViaPaste`; format 6-kolom backward-compat MC. (Open-Q2)
- [x] **W0-3 `#examExpiredModal`** (Flow G): ADA (StartExam:300) → Flow G event-driven `waitForFunction(modal.show / Results URL)`. (Open-Q3)
- [x] Helper extension additive: token (B), proton/interview + STEP2 container + STEP3 hide-guard (E), paste (D3), duration deterministik (G). Signature existing UTUH.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Bukti FULL green run lokal (D-03) | E2E-01 (SC2) | Butuh env lokal lengkap (SQLBrowser/AD=false) | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` → DONE: 75 passed (lihat 379-RUN-EVIDENCE.md) |

---

## Validation Sign-Off

- [x] Semua flow A-J di-migrasi (flat-form `.fixme` dihapus, count=0) — SC1
- [x] Flow K essay baru hijau + assert Score teragregasi (DB Score===80) — SC3
- [x] Full suite `exam-taking.spec.ts --workers=1` hijau (75 passed/0 fail), bukti `379-RUN-EVIDENCE.md` — SC2 (D-03)
- [x] Helper extension additive (no signature refactor existing examTypes.ts/wizardSelectors.ts) — D-04
- [x] Seed temporary local-only di-restore (SEED_WORKFLOW); ProtonTrackAssignments Bypass T3 count=0; journal `cleaned`
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** ✅ VALIDATED 2026-06-14 — fase 379 e2e migration DoD terpenuhi (SC1+SC2+SC3 hijau, bukti dilampirkan, seed lifecycle bersih).
