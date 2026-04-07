---
phase: 298
slug: question-types
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-07
---

# Phase 298 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual testing (tidak ada automated test framework terdeteksi) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual acceptance test per requirement |
| **Estimated runtime** | ~30 seconds (build), manual tests vary |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build` + manual browser verification
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 298-xx-01 | 01 | 1 | QTYPE-02 | — | N/A | Manual UI | Browser: form soal MA checkbox | N/A | ⬜ pending |
| 298-xx-02 | 01 | 1 | QTYPE-03 | — | N/A | Manual UI | Browser: form soal Essay textarea | N/A | ⬜ pending |
| 298-xx-03 | 02 | 1 | QTYPE-05 | — | N/A | Manual | Download template Excel | N/A | ⬜ pending |
| 298-xx-04 | 02 | 1 | QTYPE-06 | — | N/A | Manual | Upload file campur MC+MA+Essay | N/A | ⬜ pending |
| 298-xx-05 | 03 | 2 | QTYPE-07 | — | N/A | Manual UI | Browser: StartExam checkbox+textarea | N/A | ⬜ pending |
| 298-xx-06 | 03 | 2 | QTYPE-08 | T-298-01 | Server validates MA scoring all-or-nothing | Manual | Submit ujian MA, cek skor | N/A | ⬜ pending |
| 298-xx-07 | 04 | 2 | QTYPE-09 | T-298-02 | Status "Menunggu Penilaian" only after Essay | Manual | Submit ujian Essay, cek status | N/A | ⬜ pending |
| 298-xx-08 | 04 | 3 | QTYPE-10 | T-298-03 | Score validation: 0 ≤ score ≤ ScoreValue | Manual UI | HC input skor Essay | N/A | ⬜ pending |
| 298-xx-09 | 04 | 3 | QTYPE-11 | T-298-04 | Recalculate after all Essay graded | Manual | Selesaikan Penilaian, cek total | N/A | ⬜ pending |
| 298-xx-10 | 04 | 3 | QTYPE-13 | — | IsPassed null until all Essay graded | Manual | Cek DB/UI | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- Tidak ada automated test framework — semua pengujian manual via browser
- Pastikan `dotnet build` bersih sebelum setiap task di-commit

*Existing infrastructure covers build verification. Manual testing required for all UI and business logic.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| MA checkbox multi-pilih | QTYPE-02 | UI interaction | Buka form soal, pilih MA, verifikasi checkbox muncul |
| Essay textarea + rubrik | QTYPE-03 | UI interaction | Buka form soal, pilih Essay, verifikasi textarea rubrik muncul |
| Excel template kolom baru | QTYPE-05 | File download | Download template, buka di Excel, cek kolom QuestionType + Rubrik |
| Import campur tipe | QTYPE-06 | File upload + parsing | Upload file universal, cek soal tersimpan benar |
| StartExam UI per tipe | QTYPE-07 | UI interaction | Mulai ujian campuran, cek radio/checkbox/textarea sesuai tipe |
| MA all-or-nothing scoring | QTYPE-08 | Business logic + UI | Submit MA, cek skor 0 atau penuh |
| Status Menunggu Penilaian | QTYPE-09 | State machine | Submit Essay, cek status di monitoring |
| HC input skor Essay | QTYPE-10 | Admin UI | Buka monitoring detail, input skor |
| Recalculate total | QTYPE-11 | Business logic | Nilai semua Essay, klik Selesaikan, cek total |
| IsPassed null | QTYPE-13 | State validation | Cek IsPassed null sebelum semua Essay dinilai |
