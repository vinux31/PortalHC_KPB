# Phase 407: Worker Self-Service + Gating Tier Feedback + Riwayat Pekerja - Discussion Log

> **Audit trail only.** Do not use as input to planning/research/execution agents.
> Decisions captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-06-22
**Phase:** 407-worker-self-service-gating-tier-feedback-riwayat-pekerja
**Areas discussed:** Cooldown UX, Konfirmasi retake, Tier feedback, Riwayat pekerja gating

---

## Cooldown UX

| Option | Description | Selected |
|--------|-------------|----------|
| Live countdown + disabled | Tombol disabled, teks ticking JS "Bisa ulang dalam 23:14:05" | ✓ |
| Statis + disabled | Tombol disabled, teks statis "Tersedia pada 23 Jun 14:00" | |
| Enabled, validasi server | Tombol selalu enabled, ditolak server saat klik | |

**User's choice:** Live countdown + disabled.
**Notes:** Mirror pola timer ujian existing. Server tetap otoritatif (re-cek CanRetakeAsync).

---

## Konfirmasi retake

| Option | Description | Selected |
|--------|-------------|----------|
| Modal konfirmasi | "Percobaan saat ini akan diarsipkan & mulai dari awal. Lanjut?" sebelum POST | ✓ |
| Langsung tanpa konfirmasi | Klik → POST → redirect StartExam | |

**User's choice:** Modal konfirmasi.
**Notes:** Aksi destruktif (arsip + reset) — cegah klik tak sengaja.

---

## Tier feedback (showWrongFlagsOnly)

| Option | Description | Selected |
|--------|-------------|----------|
| Skor + ✓/✗ + jawaban sendiri | Skor + tanda benar/salah + jawaban dia, tanpa kunci | ✓ (kondisional) |
| Skor + ✓/✗ saja | Sembunyikan jawaban sendiri juga | |
| Skor total saja | Tanpa rincian per-soal | (saat AllowAnswerReview=false) |

**User's choice:** "tergantung hc set boleh review jawaban atau tidak, jika iya berarti opsi 1 (skor total, tanda benar/salah, jawaban yang dia pilih)".
**Notes:** REFINEMENT KUNCI — tier bergantung setting HC `AllowAnswerReview`. AllowAnswerReview=true + gagal+sisa → opsi 1 (showWrongFlagsOnly, tanpa kunci); true + lulus/habis → full review; false → skor saja. Kunci tak pernah bocor selama retake masih mungkin.

---

## Riwayat pekerja gating

| Option | Description | Selected |
|--------|-------------|----------|
| Reuse pola HC, per-soal tunduk gating | Modal/accordion mirip HC, drill-down per-soal ikut tier D-03 | ✓ |
| Daftar ringkas tanpa per-soal | Hanya daftar attempt | |
| Reuse modal HC apa adanya | ⚠ full-leak | |

**User's choice:** Reuse pola HC, per-soal tunduk gating.
**Notes:** Reuse RiwayatUnifier; butuh varian partial ter-gate untuk pekerja (BUKAN `_RiwayatPercobaan` HC yang full-leak).

## Claude's Discretion
- Komputasi tier (helper pure / service), bentuk partial worker-riwayat, format countdown, UX flash redirect.

## Deferred Ideas
None.
