# Phase 367: Delete Records Cascade Overhaul - Discussion Log

> **Audit trail only.** Jangan dipakai sbg input planning/research/execution. Keputusan ada di CONTEXT.md — log ini simpan alternatif yang dipertimbangkan.

**Date:** 2026-06-12
**Phase:** 367-delete-records-cascade-overhaul
**Areas discussed:** Badge count fix, Duplicate-guard strictness, Preview modal friction, Dep 366 sequencing

**Catatan:** Phase berat pre-spec (Spec C FINAL 2026-06-10, 6 kebijakan user §2 + phase split §3.3b locked). Diskusi HANYA area yang spec sisakan "pilih saat planning". ADVISOR_MODE off (no USER-PROFILE.md).

---

## Badge count fix (#16/#17)

| Option | Description | Selected |
|--------|-------------|----------|
| Recompute = baris tampil | Count per jenis = jumlah baris tampil di tab (online+manual+training sesuai list); badge selalu cocok list | ✓ |
| Relabel jujur | Biarkan count lama, ubah label jujur soal apa yang dihitung | |
| Keduanya | Recompute + perjelas label | |

**User's choice:** Recompute = baris tampil
**Notes:** Spec eksplisit "pilih saat planning"; acceptance netral = angka badge & isi list tak boleh kontradiksi. Recompute paling tak ambigu untuk admin.

---

## Duplicate-guard strictness (#12/#14)

| Option | Description | Selected |
|--------|-------------|----------|
| Exact (user+judul+tanggal) | Tolak/skip hanya bila tanggal PERSIS sama; false-positive minim | ✓ |
| Toleran ±1 hari | Duplikat bila tanggal ±1 hari (selaras mirror); tangkap lebih banyak tapi bisa tolak entry sah | |
| Abaikan tanggal | user+judul saja; paling agresif | |

**User's choice:** Exact (user+judul+tanggal)
**Notes:** Dibedakan tegas dari heuristik mirror #15 (±1 hari) yang HANYA untuk kandidat preview, BUKAN guard create. Perilaku per pintu (reject single / skip-report import-backfill) sudah locked di spec §3.3.

---

## Preview modal friction

| Option | Description | Selected |
|--------|-------------|----------|
| Tombol 'Hapus Semua' saja | Preview tampilkan korban persis → 1 klik konfirmasi | ✓ |
| Ketik-konfirmasi bila cascade besar | Wajib ketik kata bila korban > ambang | |
| Selalu ketik-konfirmasi | Semua hapus cascade wajib ketik | |

**User's choice:** Tombol 'Hapus Semua' saja
**Notes:** Andalkan admin baca preview; mitigasi = preview eksplisit + audit log + snapshot DB UAT. UI hint=yes → pertimbangkan /gsd-ui-phase untuk kontrak modal.

---

## Dep 366 sequencing

| Option | Description | Selected |
|--------|-------------|----------|
| Asumsi 366 land dulu | Plan 367 referensi helper image 366 precondition; eksekusi gated setelah 366 ship; preserve helper | ✓ |
| 367 self-contained image | 367 handle image-cleanup sendiri, decouple dari 366 | |
| Plan 367 sekarang, flag blocked | Tulis plan lengkap, tandai BLOCKED-until-366 | |

**User's choice:** Asumsi 366 land dulu
**Notes:** Jaga separasi scope, no dobel logika vs 366. Planning 367 boleh jalan sekarang (doc-only, aman paralel sesi 364/371); eksekusi nunggu 366.

## Claude's Discretion

- Inventaris pola ActionUrl notif TrainingRecord (L-05) — researcher.
- Struktur internal RecordCascadeDeleteService — §3.1 blueprint, detail bebas.

## Deferred Ideas

- Phase 368 (#21-27); backlog 999.6 impersonate; soft-delete/undo (ditolak); todo cleanup-post-367-ship (reviewed, not folded).
