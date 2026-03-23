# Phase 240: Alarm Sertifikat Expired - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-23
**Phase:** 240-alarm-sertifikat-expired
**Areas discussed:** Posisi & desain banner, Deduplikasi notifikasi, Template & konten notifikasi, Data source sertifikat

---

## Posisi & Desain Banner

| Option | Description | Selected |
|--------|-------------|----------|
| Paling atas halaman | Sebelum greeting dan semua konten dashboard | |
| Setelah greeting | Banner muncul setelah sapaan, sebelum card progress/upcoming | ✓ |
| Sidebar / card terpisah | Card di samping konten dashboard | |

**User's choice:** Setelah greeting
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Selalu tampil | Banner tidak bisa ditutup — hilang otomatis jika semua sertifikat sudah diurus | ✓ |
| Bisa di-dismiss per sesi | User bisa tutup banner, tapi muncul lagi saat reload | |

**User's choice:** Selalu tampil
**Notes:** —

---

## Deduplikasi Notifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| Cek by Type + SourceId | Satu notifikasi per sertifikat expired per user HC/Admin | ✓ |
| Cek by Type + tanggal | Satu notifikasi rangkuman per hari per HC/Admin | |

**User's choice:** Cek by Type + SourceId
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan saja | Notifikasi lama tetap ada di history bell | ✓ |
| Auto-delete saat renew | Hapus notifikasi CERT_EXPIRED saat sertifikat di-renew | |

**User's choice:** Biarkan saja
**Notes:** —

---

## Template & Konten Notifikasi

| Option | Description | Selected |
|--------|-------------|----------|
| RenewalCertificate | Klik notifikasi langsung ke Admin/RenewalCertificate | ✓ |
| Detail pekerja | Klik notifikasi ke halaman detail/record pekerja | |

**User's choice:** RenewalCertificate
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| Per-sertifikat | Satu notifikasi per sertifikat: "Sertifikat [Judul] milik [Nama Pekerja] telah expired" | ✓ |
| Ringkasan harian | Satu notifikasi rangkuman: "3 sertifikat pekerja telah expired" | |

**User's choice:** Per-sertifikat
**Notes:** —

---

## Data Source Sertifikat

| Option | Description | Selected |
|--------|-------------|----------|
| TrainingRecord + AssessmentSession | Kedua sumber data dihitung — konsisten dengan RenewalCertificate page | ✓ |
| TrainingRecord saja | Hanya training manual/import | |

**User's choice:** TrainingRecord + AssessmentSession
**Notes:** —

| Option | Description | Selected |
|--------|-------------|----------|
| HC dan Admin saja | Sesuai requirement — hanya role yang bertanggung jawab | ✓ |
| Semua role | Worker juga bisa lihat banner untuk sertifikat miliknya | |

**User's choice:** HC dan Admin saja
**Notes:** —

---

## Claude's Discretion

- Query optimization strategy
- Banner HTML/CSS styling details
- Template registration approach

## Deferred Ideas

None
