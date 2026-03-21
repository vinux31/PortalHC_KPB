# Phase 212: Tipe Filter, Renewal Flow, AddTraining Renewal - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-21
**Phase:** 212-tipe-filter-renewal-flow-addtraining-renewal
**Areas discussed:** Tipe Filter UI, Renewal Flow Popup, Bulk Renew Mixed-Type, AddTraining Renewal Mode

---

## Tipe Filter UI

| Option | Description | Selected |
|--------|-------------|----------|
| Sebelum Status | Urutan: Bagian > Unit > Kategori > Sub Kategori > Tipe > Status | ✓ |
| Setelah Status | Di akhir baris, sebelum tombol Reset | |
| Baris baru di bawah | Filter Tipe di baris kedua | |

**User's choice:** Sebelum Status
**Notes:** None

| Option | Description | Selected |
|--------|-------------|----------|
| Semua / Assessment / Training | 3 opsi sederhana, default 'Semua Tipe' | ✓ |
| Semua / Assessment / Training / Campuran | Tambah opsi Campuran untuk mixed chain | |

**User's choice:** Semua / Assessment / Training

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, update counts | Summary cards ikut berubah saat filter Tipe dipilih | ✓ |
| Tidak, counts tetap total | Summary cards selalu total semua tipe | |

**User's choice:** Ya, update counts

---

## Renewal Flow Popup

| Option | Description | Selected |
|--------|-------------|----------|
| Modal Bootstrap | Modal kecil dengan 2 tombol pilihan metode | ✓ |
| Dropdown inline | Split-button dropdown | |
| Context menu | Klik kanan/long-press | |

**User's choice:** Modal Bootstrap

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, langsung (Assessment only) | Popup hanya untuk Training | |
| Popup juga untuk Assessment | Konsistensi: semua tipe tampilkan popup | ✓ |

**User's choice:** Popup untuk semua tipe (Assessment maupun Training)
**Notes:** User menginginkan konsistensi UX — semua tipe punya pilihan metode renewal

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, langsung redirect | Klik pilihan → langsung ke halaman tujuan | ✓ |
| Konfirmasi lagi | Ringkasan + tombol Lanjutkan | |

**User's choice:** Langsung redirect

---

## Bulk Renew Mixed-Type

| Option | Description | Selected |
|--------|-------------|----------|
| Pisah otomatis | Sistem otomatis pisahkan per tipe | |
| Blokir campuran | Tampilkan error, user harus filter dulu | ✓ |
| Semua ke Assessment | Tetap seperti sekarang | |

**User's choice:** Blokir campuran
**Notes:** User prefer explicit filtering via Tipe dropdown

| Option | Description | Selected |
|--------|-------------|----------|
| Toast/alert di atas tabel | Warning muncul di atas tabel | |
| Di dalam modal konfirmasi | Modal terbuka, isi pesan error, disable tombol | ✓ |

**User's choice:** Di dalam modal konfirmasi

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, popup pilihan | Bulk renew same-type tetap tampilkan popup metode | ✓ |
| Tidak, langsung sesuai tipe | Langsung ke form sesuai tipe | |

**User's choice:** Ya, popup pilihan (konsisten dengan single renew)

---

## AddTraining Renewal Mode

| Option | Description | Selected |
|--------|-------------|----------|
| Title + Category + Peserta | 3 field utama, konsisten dengan CreateAssessment | ✓ |
| Title + Category saja | Tanpa peserta | |
| Semua field yang bisa dimatch | Maksimal prefill | |

**User's choice:** Title + Category + Peserta

| Option | Description | Selected |
|--------|-------------|----------|
| Query string | URL params, konsisten dengan CreateAssessment | ✓ |
| TempData/Session | Server-side, URL bersih | |

**User's choice:** Query string

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, banner info | Banner kuning di atas form | ✓ |
| Tidak perlu | Form biasa saja | |

**User's choice:** Ya, banner info

| Option | Description | Selected |
|--------|-------------|----------|
| Satu form multi-peserta | Semua peserta di satu form | ✓ |
| Satu form per peserta | Redirect terpisah per peserta | |

**User's choice:** Satu form multi-peserta (pola Phase 210)

---

## Claude's Discretion

- Exact modal styling dan animation
- Query string parameter naming
- Validasi edge cases
- Banner styling details

## Deferred Ideas

None
