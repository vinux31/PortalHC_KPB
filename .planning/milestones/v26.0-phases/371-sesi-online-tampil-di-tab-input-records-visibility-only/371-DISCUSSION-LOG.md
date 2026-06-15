# Phase 371: Sesi Online Tampil di Tab Input Records - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-11
**Phase:** 371-sesi-online-tampil-di-tab-input-records-visibility-only
**Areas discussed:** Cakupan status online, Aksi row online, Label status row online, Empty-state copy, Tanggal/Detail sesi belum selesai, Counter rekap worker, Pre-Post online 2 row

---

## Cakupan status online

| Option | Description | Selected |
|--------|-------------|----------|
| Semua status (Recommended) | Open/InProgress/Abandoned/Cancelled/Completed/Menunggu Penilaian — 367 butuh surface sesi stale | ✓ |
| Hanya yang ada hasil | Completed + Menunggu Penilaian saja — lebih bersih tapi sesi stale tak terlihat | |

**User's choice:** Semua status

---

## Aksi row online

| Option | Description | Selected |
|--------|-------------|----------|
| Tombol Lihat hasil (Recommended) | Link ke hasil exam untuk Completed/Menunggu Penilaian; belum selesai tanpa tombol | ✓ |
| Kosong total | Murni placeholder 367 | |

**User's choice:** Tombol Lihat hasil

---

## Label status row online

| Option | Description | Selected |
|--------|-------------|----------|
| Derivasi lengkap (Recommended) | Pola DeriveUserStatus 6-way + warna badge per kondisi | ✓ |
| Kamu yang putuskan | Claude's Discretion | |

**User's choice:** Derivasi lengkap

---

## Empty-state copy

| Option | Description | Selected |
|--------|-------------|----------|
| "Belum ada record untuk pekerja ini." (Recommended) | Drop kata "manual"; tombol Tambah tetap | ✓ |
| Kamu yang putuskan | Claude pilih wording | |

**User's choice:** "Belum ada record untuk pekerja ini."

---

## Tanggal/Detail sesi belum selesai (area tambahan)

| Option | Description | Selected |
|--------|-------------|----------|
| Default wajar (Recommended) | Tanggal = Schedule tanpa prefix, Detail "—"; badge status cukup | ✓ |
| Prefix 'jadwal:' | Penanda eksplisit di kolom Tanggal | |

**User's choice:** Default wajar

---

## Counter rekap worker (area tambahan)

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan (Recommended) | Sudah include online sejak dulu; service tak disentuh (view-only) | ✓ |
| Sesuaikan | Keluar scope view-only | |

**User's choice:** Biarkan

---

## Pre-Post online 2 row (area tambahan)

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan 2 row (Recommended) | Per session; grouping = kompleksitas besar tak sepadan; 367 delete per-session | ✓ |
| Penanda pasangan | Badge Pre/Post kecil di judul | |

**User's choice:** Biarkan 2 row

---

## Claude's Discretion

- Wording title + ikon tombol Lihat hasil
- Posisi badge "Assessment Online" mengikuti pola kolom Tipe existing
- Sort tetap OrderByDescending(Date)
- Route hasil exam = verifikasi route admin eksisting saat research/planning (jangan bikin halaman baru)

## Deferred Ideas

- Tombol hapus online → Phase 367
- Grouping visual pasangan Pre-Post → fase UX terpisah bila dibutuhkan
