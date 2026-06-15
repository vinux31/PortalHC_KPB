# Phase 386: AssessmentAdminController Hardening - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-15
**Phase:** 386-assessmentadmincontroller-hardening
**Areas discussed:** Validasi opsi soal (PXF-02), Essay kosong finalize (PXF-04), PDF MA correctness (PXF-05)
**Mode:** Standard (no advisor profile). User memilih "pakai semua default, skip diskusi" → ketiga area di-lock ke rekomendasi (hotfix urgent, scope locked roadmap).

---

## Gray Area Selection

| Option | Description | Selected |
|--------|-------------|----------|
| Validasi opsi soal (PXF-02) | Min opsi ber-isi + opsi gambar-saja + client-side | ✓ |
| Essay kosong finalize (PXF-04) | Definisi kosong + auto-0 + SubmitEssayScore | ✓ |
| PDF MA correctness (PXF-05) | Helper terpusat vs inline SetEquals + tampil semua opsi | ✓ |
| Pakai semua default, skip diskusi | Terima ketiga rekomendasi apa adanya | ✓ |

**User's choice:** Semua dipilih + "pakai semua default, skip diskusi" → interpretasi: percaya rekomendasi, lock default ketiga area, langsung tulis CONTEXT.
**Notes:** Hotfix urgent (~2026-06-17), scope locked roadmap. Tak ada loop tanya per-area (honor skip signal).

---

## PXF-02 — Validasi opsi soal

| Option | Description | Selected |
|--------|-------------|----------|
| Min ≥2 opsi ber-isi (MC & MA) | Standar pilihan ganda | ✓ (D-01) |
| Min ≥1 opsi ber-isi (loose) | Minimal roadmap | |
| "Ber-isi" = ber-teks (selaras persist loop) | Konsisten loop save existing | ✓ (D-02) |
| "Ber-isi" = teks atau gambar (perluas save-loop) | Dukung opsi gambar-saja | (deferred) |
| Opsi benar wajib ber-isi | Tutup akar F-DEV-01 (correctA=true, optionA kosong) | ✓ (D-03) |
| Client-side validation | Nice-to-have UX, server tetap WAJIB | opsional (D-04) |

**User's choice:** Default (D-01..D-04).
**Notes:** Opsi gambar-saja sudah di-drop loop persist existing → bukan regresi; dukungan ditunda Future.

---

## PXF-04 — Essay kosong finalize

| Option | Description | Selected |
|--------|-------------|----------|
| Essay kosong = no-row OR TextAnswer kosong → 0 poin, bukan pending | Definisi konsisten | ✓ (D-05) |
| Predikat pending tunggal di 3 titik (page + finalize-gate + monitoring) | Kill-drift | ✓ (D-06) |
| Auto-0 via Compute (tanpa materialisasi row di GET) | Hindari side-effect on read | ✓ (D-07) |
| Materialisasi 0-row saat page load | Eager (ditolak: write-on-GET) | |
| SubmitEssayScore upsert (create row bila missing) | Hilangkan dead-end "Jawaban tidak ditemukan" | ✓ (D-08) |

**User's choice:** Default (D-05..D-08).
**Notes:** FinalizeEssayGrading no-row sudah lolos; fix utama = predikat pending konsisten + tombol Selesaikan muncul.

---

## PXF-05 — PDF MA correctness

| Option | Description | Selected |
|--------|-------------|----------|
| Helper `IsQuestionCorrect` semua tipe (MC/MA/Essay) | Kill-drift, preseden Excel 4863 + essay 5085 | ✓ (D-09) |
| Inline `SetEquals` MA-only | Perbaikan minimal, tapi drift | |
| Kolom Jawaban tampil SEMUA opsi terpilih (MA) | Bukti resmi lengkap | ✓ (D-10) |
| Jangan sentuh scoring engine | Display-path only | ✓ (D-11) |

**User's choice:** Default (D-09..D-11).
**Notes:** MA tersimpan multi-row → wajib agregasi; scoring `Compute` sudah benar, hanya samakan label display.

---

## Claude's Discretion

- Struktur predikat "essay pending" (extension/helper/static) selama konsisten 3 titik.
- Bentuk pesan validasi PXF-02 (pola `QuestionTypeLabels.Short`).
- Tambah client-side PXF-02 (opsional).
- Format gabung opsi MA di PDF (koma vs newline).
- Struktur unit/Playwright test.

## Deferred Ideas

- Dukungan opsi gambar-tanpa-teks (sentuh save-loop) — Future.
- F-02/F-03/F-01/F-06/F-11/F-13/F-19/F-20/F-22 — Future pasca-acara (F-03 bersinggungan SubmitEssayScore tapi OUT).
- F-18 (export ≥2 paket) — OUT/kondisional.
- Todo "cleanup data test pasca-367" — reviewed, not folded (keyword false-positive).
