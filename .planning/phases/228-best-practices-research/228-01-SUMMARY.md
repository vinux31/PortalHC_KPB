---
phase: 228-best-practices-research
plan: "01"
subsystem: docs/research
tags: [research, best-practices, renewal-certificate, assessment-management, exam-monitoring]
dependency_graph:
  requires: []
  provides: [RSCH-01, RSCH-02, RSCH-03]
  affects: [Phase 229, Phase 230, Phase 231]
tech_stack:
  added: []
  patterns: [HTML static docs, CSS custom vars, comparison tables, 3-tier recommendations]
key_files:
  created:
    - docs/research-renewal-certificate.html
    - docs/research-assessment-management.html
    - docs/research-exam-monitoring.html
  modified: []
decisions:
  - "3 platform per topik dipilih berdasarkan relevansi konteks industrial (TalentGuard untuk renewal, Examly untuk assessment, SpeedExam/Exam.net untuk monitoring)"
  - "Rekomendasi Must-fix diprioritaskan: color urgency renewal (Phase 230), filter ManageAssessment (Phase 231), live progress monitoring (Phase 231)"
  - "Question bank terpisah dan per-question analytics didefer ke future milestones (QBNK-*, ITEM-*)"
metrics:
  duration_minutes: 35
  completed_date: "2026-03-22"
  tasks_completed: 3
  tasks_total: 3
  files_created: 3
  files_modified: 0
---

# Phase 228 Plan 01: Best Practices Research — 3 Dokumen HTML

## One-liner

3 dokumen HTML riset best practices (renewal certificate, assessment management, exam monitoring) dengan tabel perbandingan platform dan rekomendasi 3-tier mapped ke Phase 229-231.

## Summary

Plan 01 menghasilkan 3 dokumen HTML riset yang menjadi "lens" untuk audit phases 229-232. Tiap dokumen membandingkan Portal HC KPB dengan 3 platform industri terkemuka, mengidentifikasi gap UX yang konkret, dan memberikan rekomendasi 3-tier dengan target phase yang eksplisit.

**Dokumen yang dibuat:**

| File | Topik | Platform Dibandingkan | Requirement |
|------|-------|----------------------|-------------|
| `docs/research-renewal-certificate.html` | Certificate Renewal UX | Coursera, LinkedIn Learning, TalentGuard | RSCH-01 |
| `docs/research-assessment-management.html` | Assessment/Exam Management | Moodle, Google Forms Quiz, Examly/iamneo.ai | RSCH-02 |
| `docs/research-exam-monitoring.html` | Real-Time Exam Monitoring | SpeedExam, Exam.net, Proctoring Patterns 2024-2025 | RSCH-03 |

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Tulis dokumen riset renewal certificate best practices | 405ef03 | docs/research-renewal-certificate.html (554 baris) |
| 2 | Tulis dokumen riset assessment management best practices | 531be02 | docs/research-assessment-management.html (619 baris) |
| 3 | Tulis dokumen riset exam monitoring best practices | 7feab9f | docs/research-exam-monitoring.html (547 baris) |

## Key Findings per Dokumen

### RSCH-01: Renewal Certificate

- **Gap terbesar:** Tidak ada color-coded urgency (merah/kuning/hijau) pada status sertifikat
- **Must-fix:** Color urgency → Phase 230
- **Should-improve:** Dashboard widget "Expiring Soon" → Phase 229; renewal history chain modal → Phase 230
- **Nice-to-have:** One-click copy nomor sertifikat; automated notification (NOTF-* future)
- **Platform benchmark:** TalentGuard paling relevan karena model sertifikat-nya mirip Portal KPB (expiry + compliance tracking)

### RSCH-02: Assessment Management

- **Gap terbesar:** Filter ManageAssessment perlu validasi (AMGT-05) dan bulk assignment confirmation UX
- **Must-fix:** Perbaiki filter/search ManageAssessment → Phase 231
- **Should-improve:** Bulk assignment confirmation dialog; question bank terpisah (QBNK-* future)
- **Nice-to-have:** Preview assessment sebagai worker; per-question analytics (ITEM-* future)
- **Temuan positif:** Portal KPB sudah lebih advanced dari Google Forms Quiz dalam banyak aspek (timer, attempt management, auto-grading, monitoring)

### RSCH-03: Exam Monitoring

- **Gap terbesar:** Live progress per-peserta (answered/total) belum real-time (AMON-02)
- **Must-fix:** Live progress per peserta → Phase 231
- **Should-improve:** Color urgency pada baris peserta; integrasi flag AINT-02/03 ke monitoring view → Phase 231
- **Nice-to-have:** Token card copy UX (AMON-04) → Phase 231; individual timer extension (future)
- **Temuan positif:** Portal KPB sudah punya Token management, Force Close, dan Bulk Close (AMON-03) yang tidak semua platform miliki

## Deviations from Plan

Tidak ada — plan dieksekusi sesuai spesifikasi.

## Known Stubs

Tidak ada stub. Dokumen adalah konten riset murni, bukan komponen yang merender data dari database.

## Self-Check: PASSED

File exists:
- docs/research-renewal-certificate.html — FOUND
- docs/research-assessment-management.html — FOUND
- docs/research-exam-monitoring.html — FOUND

Commits exist:
- 405ef03 — FOUND
- 531be02 — FOUND
- 7feab9f — FOUND
