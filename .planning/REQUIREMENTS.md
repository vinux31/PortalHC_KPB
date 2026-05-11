# Requirements: v16.0 QA Test Coverage

**Milestone:** v16.0 QA Test Coverage
**Status:** Active
**Created:** 2026-05-11

## v16.0 Goal

Membangun automated test infrastructure untuk Portal HC sebagai tooling discovery bug end-to-end. Fokus pertama: assessment flow (tipe assessment × tipe soal). Foundation untuk expand test coverage di milestone berikutnya.

## v16.0 Requirements

### QA — Test Infrastructure

- [ ] **QA-01**: Developer dapat menjalankan automated test sweep yang menguji seluruh kombinasi (tipe assessment × tipe soal) end-to-end di lokal, dengan output 1 markdown report yang merangkum semua temuan bug per skenario (severity, screenshot, hypothesis). Harus include DB seed temporary + cleanup otomatis (BACKUP/RESTORE), continue-and-collect bug behavior, dan meta-validation (sentinel skenario verifikasi framework test sendiri).

## Future Requirements (deferred)

- **QA-02** — CI integration: jalankan matrix test otomatis on PR/push (deferred — wait sampai matrix test proven stable)
- **QA-03** — Regression suite: convert subset matrix test jadi smoke regression (deferred)
- **QA-04** — Visual regression: screenshot diff untuk halaman exam, result, monitoring (deferred — butuh tooling Percy/Chromatic)
- **QA-05** — Multi-environment: extend matrix test ke staging/prod (deferred — start lokal saja)
- **QA-06** — Concurrency stress test: simulate banyak peserta paralel di session yang sama (deferred — out-of-scope discovery sweep)
- **QA-07** — Coverage expansion: matrix test untuk flow lain (CDP, IDP, Coaching, Worker management) (deferred — proven foundation dulu)

## Out of Scope (v16.0)

- **Regression suite** — fokus discovery bug, bukan regression. Convert subset jadi regression boleh di milestone berikutnya jika matrix test proven.
- **CI integration** — manual run by developer dulu. CI tunggu matrix test stabil.
- **Server Dev/Prod testing** — lokal saja per `docs/DEV_WORKFLOW.md` aturan dasar.
- **Visual regression** — out of scope, tidak ada Percy/Chromatic infra.
- **Performance/load test** — separate concern, bukan matrix test discovery.
- **Wizard admin create-assessment UI** — diasumsikan working (di-cover spec terpisah jika temuan muncul).

## Traceability

| REQ-ID | Phase | Status |
|--------|-------|--------|
| QA-01 | 315 | Pending |

**Active mapped: 1/1 ✓ — Orphans: 0 — Duplicates: 0**

## Notes

- Scope sengaja sempit (1 REQ, 1 phase) untuk foundation milestone. Expand ke regression/CI/multi-env di milestone berikutnya jika tooling proven.
- Spec design lengkap: `docs/superpowers/specs/2026-05-11-assessment-matrix-test-design.md` (commit `94bacecf`).
- Open questions blocker investigation (5 items) tercatat di spec — akan dijawab di Wave 0 Phase 315.
