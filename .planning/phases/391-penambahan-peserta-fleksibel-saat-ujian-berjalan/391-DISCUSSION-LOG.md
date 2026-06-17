# Phase 391: Penambahan Peserta Fleksibel saat Ujian Berjalan - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-17
**Phase:** 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
**Areas discussed:** Status sesi peserta baru, Aturan boleh-tambah (guard Completed), Proteksi sesi berjalan, Notice UX

---

## Status sesi peserta baru (PART-01)

| Option | Description | Selected |
|--------|-------------|----------|
| Set siap-mulai sesuai jadwal | Open jika window buka / Upcoming jika belum tiba; bukan warisi status peserta lain | ✓ |
| Warisi status induk (current) | Simpel; risiko peserta baru ber-status InProgress padahal belum mulai | |

**User's choice:** Set siap-mulai sesuai jadwal.
**Notes:** Konsisten dengan cabang Pre-Post yang sudah set "Upcoming".

---

## Aturan boleh-tambah / guard Completed (PART-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Selama window ujian belum tutup | Boleh tambah selama ExamWindowCloseDate/jadwal+durasi belum lewat, walau ada sesi Completed | ✓ |
| Selama ada ≥1 sesi belum Completed | Boleh selama grup belum sepenuhnya selesai | |
| Selalu boleh untuk operasi tambah | Buang batasan status untuk tambah | |

**User's choice:** Selama window ujian belum tutup.
**Notes:** Guard `Completed` (L1992) tak lagi salah-blokir penambahan.

---

## Proteksi sesi yang sedang berjalan (PART-01c / PART-04)

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, lindungi yang sedang ujian | Sesi InProgress (StartedAt!=null, belum Completed) tidak ikut diubah Status/Schedule/Duration; hanya sesi belum-mulai ikut update; benerin teks warning | ✓ |
| Tidak, biarkan seperti sekarang | Simpan menimpa semua termasuk yang sedang ujian | |
| Anggap di luar Phase 391 | Catat sebagai ide untuk nanti | |

**User's choice:** Ya, lindungi yang sedang ujian.
**Notes:** Awalnya user tidak paham ("maksutnya apa ini saya tidak paham") → dijelaskan ulang dengan skenario konkret (2 dari 5 peserta sedang ujian, HC tambah 3 baru → simpan menimpa data 2 yang sedang ujian → timer/status kacau). Setelah penjelasan, user pilih lindungi.

---

## Notice UX (PART-03)

| Option | Description | Selected |
|--------|-------------|----------|
| Info non-blocking setelah simpan | Pesan netral di halaman tujuan; tanpa friksi | ✓ |
| Info + konfirmasi ringan sebelum simpan | Tampilkan jumlah peserta sedang mengerjakan sebelum klik simpan (1 klik ekstra) | |

**User's choice:** Info non-blocking setelah simpan.
**Notes:** Sesuai keputusan milestone = fleksibel tanpa friksi.

## Claude's Discretion

- Penentuan Open vs Upcoming (ikuti pola StartExam), penempatan cek window, bentuk proteksi sesi-berjalan, mekanisme TempData notice.

## Deferred Ideas

- Dialog konfirmasi opsional (friksi) — REQUIREMENTS Future.
- Bulk import peserta-ke-assessment via Excel — REQUIREMENTS Future.
- Reviewed todo "cleanup data test Phase 367" — tidak di-fold (tak terkait).
