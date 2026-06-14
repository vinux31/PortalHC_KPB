# Requirements: v28.0 Assessment & Records Bug Fixes

**Milestone goal:** Fix 4 bug fungsional outstanding di domain assessment & records (promote dari backlog 999.x): grading essay, impersonasi identity, routing CMP orphan, dan migrasi test net exam-taking.

**Sumber:** backlog 999.8 (Phase 364 capture) + 999.6 (brainstorm delete-records) + 999.10 (Phase 368 UAT) + 999.7 (Phase 364 capture). Bug-fix di codebase existing — no domain research.

---

## v28.0 Requirements

### GRADE — Assessment Grading Correctness (dari 999.8)

- [x] **GRADE-01**: Finalize assessment essay-only mengagregasi skor manual essay ke `AssessmentSessions.Score` (saat ini `Score=0` walau HC nilai + finalize, badge "Sudah Dinilai"). Diagnose root cause dulu (GradingService finalize path vs hook Proton completion Phase 358) sebelum fix.
- [x] **GRADE-02**: Agregasi skor konsisten antara jalur essay-only dan mixed (MC+MA+essay) — regression test xUnit/e2e kedua jalur (mixed `Score` sudah benar, essay-only tidak).

### IMP — Impersonation Identity Correctness (dari 999.6)

- [ ] **IMP-01**: Surface worker-data (CMP/Records, Assessment, Home progress) menampilkan data milik user yang di-impersonate — bukan admin asli — saat impersonasi aktif (banner "Anda melihat sebagai X" jujur).
- [ ] **IMP-02**: Audit cakupan semua call-site `GetCurrentUserRoleLevelAsync` / `_userManager.GetUserAsync(User)` lintas controller (Records/Assessment/Home/dst) — petakan & perbaiki yang mengabaikan identitas impersonasi.

### CMPRT — CMP Routing Hygiene (dari 999.10)

- [x] **CMPRT-01**: `GET /CMP/CertificationManagement` (direct-URL) tidak lagi mengembalikan 500 "view not found" — redirect ke route CDP canonical ATAU hapus action orphan `CMPController.CertificationManagement` + helper rows dead (audit dulu apakah ada link/test yang menunjuk route CMP).

### E2E — Exam-Taking Test Coverage (dari 999.7)

- [ ] **E2E-01**: 10 create flow di `tests/e2e/exam-taking.spec.ts` (A-J `.fixme`) dimigrasi dari flat-form usang ke wizard CreateAssessment 4-langkah; spec hijau (regression net untuk flow exam termasuk essay yang disentuh GRADE-01).

---

## Future Requirements (deferred)

- Label residu "Backfill/Restore" di UI BulkBackfill (kosmetik) — backlog 999.9.
- 362 PROTON CDP Polish formal verification + G-01 chart race (domain CDP/Proton, beda tema).

## Out of Scope (v28.0)

- **999.9 label kosmetik** — LOW, beda tema (UI polish, bukan bug fungsional).
- **362 issues** — domain CDP/Proton, bukan Assessment/Records.
- **Test debt di luar exam-taking** (mis. essay finalize L6 `.fixme` di-cover oleh GRADE-02 spesifik, bukan full harness rewrite).
- **Push IT** — handoff terpisah (bundle v24-v28 saat siap).

---

## Traceability

| REQ | Phase | Status |
|-----|-------|--------|
| GRADE-01 | 376 | Complete |
| GRADE-02 | 376 | Complete |
| IMP-01 | 377 | pending |
| IMP-02 | 377 | pending |
| CMPRT-01 | 378 | Complete |
| E2E-01 | 379 | pending |

**Coverage:** 6/6 REQ mapped → 4 phase (376-379). 0 orphan.
