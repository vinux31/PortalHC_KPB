# Phase 241: Seed Data UAT - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-24
**Phase:** 241-seed-data-uat
**Areas discussed:** Konten Soal, Completed Assessment, Seed Mechanism, Idempotency & Reset, User Assignment, Tanggal & Jadwal, Coach-Coachee Detail

---

## Konten Soal

| Option | Description | Selected |
|--------|-------------|----------|
| Realistis kilang | Soal terkait operasi Alkylation/kilang nyata | |
| Dummy placeholder | Soal generic "Pertanyaan 1", "Pertanyaan 2" | |
| Semi-realistis | Judul & opsi terlihat nyata tapi konten tidak harus akurat | ✓ |

**User's choice:** Semi-realistis
**Notes:** Konten terlihat nyata untuk demo/screenshot tapi tidak perlu akurat secara teknis kilang

| Option | Description | Selected |
|--------|-------------|----------|
| Nama generik kilang | "Proses Distilasi", "Keselamatan Kerja", dll | ✓ |
| Kode singkat | ET-01, ET-02, ET-03, ET-04 | |
| Claude tentukan | Serahkan ke Claude | |

**User's choice:** Nama generik kilang

---

## Completed Assessment

| Option | Description | Selected |
|--------|-------------|----------|
| Lulus skor tinggi | Skor 80/100, IsPassed=true, sertifikat | |
| Lulus pas-pasan | Skor 60/100, batas lulus | |
| Gagal | Skor 40/100, tanpa sertifikat | |
| Dua-duanya | 1 lulus + 1 gagal | ✓ |

**User's choice:** Dua-duanya — seed 2 completed assessment

| Option | Description | Selected |
|--------|-------------|----------|
| 1 tahun | ValidUntil = 1 tahun dari completed | ✓ |
| Sudah expired | Langsung bisa test renewal | |
| Keduanya | 1 aktif + 1 expired | |

**User's choice:** 1 tahun

| Option | Description | Selected |
|--------|-------------|----------|
| Lengkap dengan jawaban | Seed UserResponses untuk review & radar chart | ✓ |
| Result saja | Cukup AssessmentResult dengan skor | |

**User's choice:** Lengkap dengan jawaban

---

## Seed Mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| Extend SeedData.cs | Tambah method di file existing | ✓ |
| File terpisah | Buat SeedUatData.cs baru | |
| SQL script | Raw SQL seed script | |

**User's choice:** Extend SeedData.cs

---

## Idempotency & Reset

| Option | Description | Selected |
|--------|-------------|----------|
| Skip jika ada | Check exists → skip, sama seperti CreateUsersAsync | ✓ |
| Delete & recreate | Hapus lama, seed ulang | |
| Flag-based | Gunakan marker di DB | |

**User's choice:** Skip jika ada

---

## User Assignment

| Option | Description | Selected |
|--------|-------------|----------|
| Sesuai requirements saja | Reguler: Rino+Iwan. Proton: Rino saja | ✓ |
| Tambah Iwan di Proton | Iwan juga di Proton T1 | |
| Tambah user baru | Seed user tambahan | |

**User's choice:** Sesuai requirements saja

---

## Tanggal & Jadwal

| Option | Description | Selected |
|--------|-------------|----------|
| Relative dari startup | CreatedAt=UtcNow, jadwal=UtcNow+7d | ✓ |
| Fixed date | Hardcode tanggal spesifik | |

**User's choice:** Relative dari startup

---

## Coach-Coachee Detail

| Option | Description | Selected |
|--------|-------------|----------|
| Mapping + Track Assignment | Seed keduanya agar Proton coaching langsung testable | ✓ |
| Mapping saja | Hanya CoachCoacheeMapping | |

**User's choice:** Mapping + Track Assignment

---

## Claude's Discretion

- Nama spesifik soal dan opsi jawaban
- Distribusi jawaban benar/salah pada completed assessment
- Nomor sertifikat format

## Deferred Ideas

None — discussion stayed within phase scope
