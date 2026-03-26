# Phase 1: Tambahkan tombol hapus worker di halaman ManageWorkers - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-26
**Phase:** 01-tambahkan-tombol-hapus-worker-di-halaman-manageworkers
**Areas discussed:** Posisi & visibilitas tombol, Konfirmasi hapus, Akses role

---

## Posisi & Visibilitas Tombol

| Option | Description | Selected |
|--------|-------------|----------|
| Tampil untuk semua user | Tombol hapus selalu ada di samping Edit dan Deactivate/Reactivate | |
| Hanya user non-aktif | Tombol hapus hanya muncul kalau user sudah di-deactivate (2-step process) | ✓ |
| Tampil semua, beda warna/posisi | Tombol ada tapi terpisah atau lebih kecil | |

**User's choice:** Hanya user non-aktif — flow: HC menekan tombol nonaktifkan, baru bisa klik hapus.
**Notes:** User menegaskan ini fitur untuk role HC dan Admin saja.

---

## Konfirmasi Hapus

| Option | Description | Selected |
|--------|-------------|----------|
| confirm() native | Konsisten dengan pattern confirmDeactivate yang sudah ada | ✓ |
| Modal Bootstrap + ketik nama | Lebih aman untuk aksi destruktif, user harus ketik nama untuk konfirmasi | |

**User's choice:** confirm() native
**Notes:** Konsistensi dengan pattern yang sudah ada lebih diprioritaskan.

---

## Akses Role

| Option | Description | Selected |
|--------|-------------|----------|
| Admin + HC | Kedua role bisa lihat dan klik tombol hapus | ✓ |
| Hanya Admin | HC bisa deactivate tapi tidak bisa hapus permanen | |

**User's choice:** Admin + HC
**Notes:** Konsisten dengan endpoint DeleteWorker yang authorize Admin, HC.

---

## Claude's Discretion

Tidak ada area yang di-defer ke Claude.

## Deferred Ideas

Tidak ada ide yang di-defer.
