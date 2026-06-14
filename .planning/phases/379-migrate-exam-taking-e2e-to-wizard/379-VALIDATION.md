---
phase: 379
slug: migrate-exam-taking-e2e-to-wizard
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-14
---

# Phase 379 — Validation Strategy

> Per-phase validation contract. **Untuk fase ini, suite e2e ADALAH deliverable** —
> validasi = suite `exam-taking.spec.ts` berjalan HIJAU (`--workers=1`). Detail dimensi
> diturunkan dari `379-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (TypeScript) — target suite migrasi |
| **Config file** | `playwright.config.ts` · helpers `tests/e2e/helpers/examTypes.ts` + `wizardSelectors.ts` |
| **Quick run command** | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1 -g "Flow A\|Flow K"` |
| **Full suite command** | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` |
| **Estimated runtime** | beberapa menit (serial, 10 flow + K; G timer dibuat deterministik) |
| **Env prasyarat** | SQLBrowser running + conn `lpc:` shared-mem override + `Authentication__UseActiveDirectory=false dotnet run` (app up @localhost:5277) |

---

## Sampling Rate

- **After every flow migrated:** run flow itu `--workers=1 -g "Flow X"` → hijau sebelum lanjut flow berikut.
- **After helper extension:** run subset yang pakai extension (B token, E proton, G timer, K essay).
- **Before `/gsd-verify-work`:** FULL suite `exam-taking.spec.ts --workers=1` hijau (D-03 bukti run dilampirkan).
- **Max feedback latency:** per-flow ~puluhan detik.

---

## Per-Task Verification Map

> Deliverable = test hijau. Tiap flow yang dimigrasi = unit verifikasi tersendiri (un-fixme + run green).
> Wave numbers + Task IDs final diisi planner.

| Task ID | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | Status |
|---------|------|------|-------------|-----------------|-----------|-------------------|--------|
| _diisi planner_ | — | — | E2E-01 | Seed temporary local-only (snapshot/restore), no prod surface | e2e | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1 -g "Flow X"` | ⬜ pending |
| _diisi planner_ | — | — | E2E-01 (SC3) | Flow K assert Score teragregasi >0 (validasi GRADE-01 376) | e2e + DB-assert | `... -g "Flow K"` | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] **W0-1 ProtonTrack Tahun 3 seed** (Flow E, D-02): `SELECT COUNT(*) FROM ProtonTracks WHERE TahunKe='Tahun 3'` — bila 0, seed minimal temporary local-only (global.teardown `db.restore()` aman). (Open-Q1)
- [ ] **W0-2 paste-import route** (Flow D3): konfirmasi `textarea[name="pasteText"]` + route current masih valid; fallback `addQuestionViaForm`×3 bila berbeda. (Open-Q2)
- [ ] **W0-3 `#examExpiredModal`** (Flow G expiry): konfirmasi outcome modal; fallback DB-state/URL-Results bila berbeda. (Open-Q3)
- [ ] Helper extension (examTypes.ts/wizardSelectors.ts) additive: token (B), proton/interview (E), paste (D3 opsional), duration deterministik (G).

*Detail final diisi planner berdasarkan RESEARCH §Validation Architecture + Assumptions Log.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Bukti FULL green run lokal (D-03) | E2E-01 (SC2) | Butuh env lokal lengkap (SQLBrowser/lpc/AD=false) | `npx playwright test tests/e2e/exam-taking.spec.ts --workers=1` → lampirkan output "N passed" (semua A-J + K) |

*Local e2e SQL gotcha (STATE.md): SQLBrowser + `lpc:` shared-mem + `--workers=1` + AD lokal `Authentication__UseActiveDirectory=false dotnet run`.*

---

## Validation Sign-Off

- [ ] Semua flow A-J di-migrasi (flat-form `.fixme` dihapus) — SC1
- [ ] Flow K essay baru hijau + assert Score teragregasi — SC3
- [ ] Full suite `exam-taking.spec.ts --workers=1` hijau, bukti run dilampirkan — SC2 (D-03)
- [ ] Helper extension additive (no signature refactor existing) — D-04
- [ ] Seed temporary local-only di-restore (SEED_WORKFLOW), tak ada residu
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
