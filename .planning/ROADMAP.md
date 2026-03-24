# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0** - Phases 223-227 (shipped)
- ✅ **v8.1** - Phases 228-232 (shipped)
- ✅ **v8.2** - Phases 233-238 (shipped)
- ✅ **v8.3** - Phase 239 (shipped)
- ✅ **v8.4** - Phase 240 (shipped)
- 📋 **v8.5** - Phases 241-247 (defined, pending execution)
- ✅ **v8.6 Codebase Audit & Hardening** - Phases 248-252 (shipped 2026-03-24)

## Phases

### v8.5 UAT Seed & Verification (Phases 241-247)

- [ ] Phase 241: Seed Data UAT
  - **Goal:** Menyediakan seluruh data prasyarat UAT di environment Development sehingga semua fase UAT berikutnya (242-246) dapat dieksekusi tanpa setup manual
  - **Scope:** Extend SeedData.cs dengan coach-coachee mapping, sub-kategori, assessment reguler + Proton, paket soal + 15 soal, dan completed assessment dengan sertifikat
  - **Plans:** 2 plans
  - Plans:
    - [x] 241-01-PLAN.md — Seed entry point + coach-coachee + kategori + assessment reguler open dengan 15 soal
    - [x] 241-02-PLAN.md — Completed assessment (lulus+gagal) + Assessment Proton Tahun 1 & 3

<details>
<summary>✅ v8.6 Codebase Audit & Hardening (Phases 248-252) — SHIPPED 2026-03-24</summary>

- [x] Phase 248: UI & Annotations (1/1 plans) — completed 2026-03-24
- [x] Phase 249: Null Safety & Input Validation (2/2 plans) — completed 2026-03-24
- [x] Phase 250: Security & Performance (1/1 plans) — completed 2026-03-24
- [x] Phase 251: Data Integrity & Logic (2/2 plans) — completed 2026-03-24
- [x] Phase 252: XSS Escape AJAX Approval Badge (1/1 plans) — completed 2026-03-24

Full details: `.planning/milestones/v8.6-ROADMAP.md`

</details>

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 241. Seed Data UAT | v8.5 | 2/2 | Complete   | 2026-03-24 |
| 248. UI & Annotations | v8.6 | 1/1 | Complete | 2026-03-24 |
| 249. Null Safety & Input Validation | v8.6 | 2/2 | Complete | 2026-03-24 |
| 250. Security & Performance | v8.6 | 1/1 | Complete | 2026-03-24 |
| 251. Data Integrity & Logic | v8.6 | 2/2 | Complete | 2026-03-24 |
| 252. XSS Escape AJAX Approval Badge | v8.6 | 1/1 | Complete | 2026-03-24 |
