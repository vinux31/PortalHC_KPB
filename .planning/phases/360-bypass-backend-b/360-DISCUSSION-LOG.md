# Phase 360: Bypass Backend (B) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-10
**Phase:** 360-bypass-backend-b
**Areas discussed:** CL-B(b) konfig exam, Exempt gate antar-tahun, Penempatan logic + transaksi, Guard rail pending

---

## Gray area selection

| Option | Description | Selected |
|--------|-------------|----------|
| CL-B(b): konfig exam | Seberapa lengkap BypassSave bikin AssessmentSession source-year | ✓ |
| Exempt gate antar-tahun | Mekanisme nandain assignment hasil bypass biar lolos cross-year gate | ✓ |
| Penempatan logic + tx | ProtonBypassService vs inline + transaksi | ✓ |
| Guard rail pending | Aturan edge pending (dobel, stale confirm, race) | ✓ |

**User's choice:** Semua 4 area.

---

## CL-B(b) konfigurasi exam

User awalnya minta penjelasan awam ("saya tidak paham ini"). Dijelaskan ulang dengan analogi (pesan kopi sekali sebut vs buka order dulu).

| Option | Description | Selected |
|--------|-------------|----------|
| A — Konfig penuh inline | Wizard kumpulin paket+jadwal+durasi+KKM, sesi langsung siap | |
| B — Dua langkah (sesi bare + paket terpisah) | BypassSave bikin sesi tanpa paket, HC lampirkan paket via Kelola Assessment | ✓ |
| C — Pilih assessment existing | Tunjuk sesi existing, assign worker | |

**User's choice:** B — Dua langkah.
**Notes:** Worker exam jadi 2-step; wajib pengingat TempData (D-02). Klarifikasi: CL-B(b) source selalu online Tahun 1/2 (D-03).

---

## Exempt gate antar-tahun

Dijelaskan awam: gate = gerbang aturan urutan (359), bypass = pengecualian resmi, masalah = sistem lupa siapa lewat bypass (terutama CL-C geser track).

| Option | Description | Selected |
|--------|-------------|----------|
| A — Stempel permanen | Tambah kolom Origin di ProtonTrackAssignment, gate cek stempel | ✓ |
| B — Andalkan penanda lulus | Tanpa kolom; CL-C geser track bisa nyangkut | |

**User's choice:** A — Stempel permanen.
**Notes:** Migration #2 melebar (tabel + kolom). Exempt CUMA cross-year, bukan gate 100% (D-05). Isi 2 titik exempt (D-06). Renewal tetap session-based (D-07).

---

## Penempatan logic + transaksi

Dijelaskan: transaksi all-or-nothing = pola tetap (gak diputus); yang diputus = di mana logic ditaruh.

| Option | Description | Selected |
|--------|-------------|----------|
| A — Service terpisah (ProtonBypassService) | Testable + shared controller & GradingService hook | ✓ |
| B — Inline di controller | Cepat ditulis, susah dites | |
| Serahkan ke planner | Discretion | |

**User's choice:** A — Service terpisah.
**Notes:** Pola ProtonCompletionService (358). Risiko: AutoCreateProgressForAssignment + coach-mapping create private di CoachMappingController → perlu extract (D-08, code_context risiko).

---

## Guard rail pending (multiSelect)

| Option | Description | Selected |
|--------|-------------|----------|
| Blok dobel pending | Tolak bypass baru kalau worker punya pending aktif | ✓ |
| Cek ulang saat konfirmasi | Validasi assignment/exam/status sebelum eksekusi BypassConfirm | ✓ |
| Konfirmasi anti-dobel | Transisi Siap→Selesai atomik | ✓ |

**User's choice:** Ketiganya.
**Notes:** Blok-dobel berlaku SEMUA mode bukan cuma B(b) (D-10). Re-grade Pass→Fail → pending balik Menunggu sudah locked spec §7 (tidak perlu diputus).

---

## Verifikasi (user minta "check ulang sudah benar, ada miss?")

Scout kode konfirmasi 4 keputusan implementable + temukan 6 klarifikasi (D-03, D-05, D-06, D-07, D-10, code_context risiko) + 4 detail spec di-baking (D-13..D-16). Tidak ada kontradiksi.

## Claude's Discretion

Default jadwal/durasi/KKM sesi bare; penamaan/struktur ProtonBypassService; mekanisme extract bootstrap+coach-mapping; strategi test.

## Deferred Ideas

Semua UI → Phase 361; audit Tab1 → backlog; undo executed → out (Opsi C); level → dibuang.
