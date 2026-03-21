# Phase 223: Assessment Quick Wins - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-21
**Phase:** 223-assessment-quick-wins
**Areas discussed:** Tab-switch display, ET score granularity, Status lifecycle, UserResponse timestamp, AccessToken docs

---

## Tab-switch Display

| Option | Description | Selected |
|--------|-------------|----------|
| Badge angka di kolom | Kolom 'Tab Switch' di tabel peserta dengan badge warna | |
| Icon warning saja | Icon ⚠ di samping nama jika tab-switch > threshold | ✓ (awal) |
| Detail popup | Kolom badge + modal timeline | |

**Follow-up: Threshold**

| Option | Description | Selected |
|--------|-------------|----------|
| 1x atau lebih | Setiap tab-switch ditandai | |
| 3x atau lebih | Hanya 3+ kali | |
| 5x atau lebih | Threshold tinggi | ✓ |

**Follow-up: Perlu implementasi?**

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, implementasi sekarang | Sesuai AINT-02/03 | |
| Rekam saja, tanpa display | Simpan event tapi belum tampilkan | |
| Tunda ke phase lain | Hapus dari scope | ✓ |

**User's choice:** Tunda — sudah ada sistem tab switch warning yang berjalan
**Notes:** User menganggap fitur ini tidak perlu karena sudah ada mekanisme warning di sistem

---

## ET Score Granularity

| Option | Description | Selected |
|--------|-------------|----------|
| Skor per ET saja | SessionId, ElemenTeknis, Score, MaxScore | |
| Skor per ET + jumlah soal | Tambah QuestionCount dan CorrectCount | ✓ |
| Tidak perlu tabel baru | Hitung on-the-fly | |

**Follow-up: Display**

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, tampilkan tabel ET | Breakdown per ET di halaman hasil assessment | ✓ |
| Simpan saja, belum tampilkan | Persist ke DB, display di Phase 224 | |
| Tampilkan di monitoring saja | Hanya untuk HC | |

**User's choice:** Persist dengan QuestionCount/CorrectCount + tampilkan di AssessmentResults
**Notes:** None

---

## Status Lifecycle

| Option | Description | Selected |
|--------|-------------|----------|
| 3 status: Passed, Valid, Expired | Hapus Wait Certificate | ✓ |
| 4 status: termasuk Wait Certificate | Pertahankan Wait Certificate | |
| Biarkan seperti sekarang | Hanya dokumentasi | |

**Follow-up: Assessment TrainingRecord status**

| Option | Description | Selected |
|--------|-------------|----------|
| Tetap Passed/Failed | TrainingRecord dari assessment = historis saja | |
| Ikut lifecycle Valid/Expired | Jika punya sertifikat, status = Valid → Expired | ✓ |

**User's choice:** Assessment dengan sertifikat ikut lifecycle Valid/Expired
**Notes:** User perlu penjelasan tentang hubungan AssessmentSession → TrainingRecord sebelum memilih

---

## UserResponse Timestamp & AccessToken Docs

**User's choice:** Straightforward — tidak perlu diskusi detail
**Notes:** Langsung kerjakan sesuai requirement (AINT-04, CLEN-05)

---

## Claude's Discretion

- Migration strategy untuk data "Wait Certificate" existing
- Layout breakdown ET di AssessmentResults
- Penanganan soal tanpa ElemenTeknis tag

## Deferred Ideas

- Tab-switch detection (AINT-02, AINT-03) — sudah ada mekanisme warning di sistem
