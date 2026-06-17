---
status: resolved
phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
source: [391-VERIFICATION.md]
started: 2026-06-17
updated: 2026-06-17
---

## Current Test

[complete — UAT lokal Playwright dijalankan 2026-06-17 di HcPortalDB_Dev (seed+restore per CLAUDE.md Seed Workflow, 0 residu)]

## Tests

### 1. Alur penambahan peserta ke assessment yang sedang berjalan (end-to-end)
expected: alert biru "Info" menenangkan; peserta baru Open/Upcoming bisa StartExam; peserta InProgress tidak terganggu.
result: PASS — Save tambah Zafrullah ke 'UAT391 Live Add' (Iwan InProgress) → alert **biru "Info"** "Ada peserta yang sedang mengerjakan ujian. Peserta baru tetap dapat ditambahkan..." + "**1 new user(s) assigned**"; DB: Zafrullah Status=**Open** (siap-mulai, AssessmentType=Standard), Iwan **InProgress/06:00/60min UNCHANGED**. (Menemukan + memfix blocker pre-existing AssessmentType NOT NULL, commit 34f102b0.)

### 2. Guard Completed pada EDIT murni tetap ditolak
expected: error "Cannot edit completed assessments." saat Simpan tanpa menambah peserta pada assessment Completed.
result: PASS — Save 'UAT391 Done Edit' (Rino Completed) tanpa tambah peserta → error **"Cannot edit completed assessments."** (jalur edit murni diblokir; jalur penambahan dilonggarkan).

## Summary

total: 2
passed: 2
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps

(none — automated 8/8 + UAT browser lokal 2/2 PASS; seed temporary di-restore, 0 residu, SEED_JOURNAL cleaned)
