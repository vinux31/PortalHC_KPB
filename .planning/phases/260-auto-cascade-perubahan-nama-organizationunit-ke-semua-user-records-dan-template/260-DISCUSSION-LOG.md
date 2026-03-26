# Phase 260: Auto-cascade perubahan nama OrganizationUnit - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-26
**Phase:** 260-auto-cascade-perubahan-nama-organizationunit-ke-semua-user-records-dan-template
**Areas discussed:** Cascade timing, Scope fields, Template import, Notifikasi, Deactivate handling, Validasi runtime, Reparent

---

## Kapan Cascade Terjadi

| Option | Description | Selected |
|--------|-------------|----------|
| Langsung saat rename | Saat admin klik Save, semua user records langsung diupdate dalam satu transaksi | ✓ |
| Konfirmasi dulu | Preview 'X user akan terpengaruh' sebelum cascade | |
| Manual trigger | Tombol 'Sinkronisasi' terpisah | |

**User's choice:** Langsung saat rename (Recommended)

---

## Scope Field yang Di-cascade

| Option | Description | Selected |
|--------|-------------|----------|
| ApplicationUser.Section & Unit | Field utama filtering | ✓ |
| CoachCoacheeMapping override fields | AssignmentSection dan AssignmentUnit | ✓ |
| Directorate tetap free-text | Tidak terhubung ke OrganizationUnit | ✓ |

**User's choice:** Semua dipilih — cascade Section/Unit dan CoachCoacheeMapping, Directorate tetap free-text
**Notes:** User bertanya apakah tab di KkjMatrix otomatis muncul jika tambah Bagian baru — jawaban: ya, sudah dinamis via GetAllSectionsAsync()

---

## Template Import Dinamis

| Option | Description | Selected |
|--------|-------------|----------|
| Query dinamis | Ganti hardcoded dengan GetAllSectionsAsync() | ✓ |
| Kamu yang tentukan | Claude pilih pendekatan terbaik | |

**User's choice:** Query dinamis (Recommended)

---

## Notifikasi & Audit Trail

| Option | Description | Selected |
|--------|-------------|----------|
| TempData flash message | Pesan sukses: 'X user dan Y mapping terupdate' | ✓ |
| Log ke database | Tabel audit terpisah | |
| Tidak perlu | Tanpa notifikasi | |

**User's choice:** TempData flash message (Recommended)

---

## Deactivate vs Delete

| Option | Description | Selected |
|--------|-------------|----------|
| Biarkan, tampilkan warning | User tetap punya Section/Unit lama, tampilkan info | |
| Blokir deactivate jika ada user | Tidak bisa deactivate unit yang masih punya user aktif | ✓ |
| Auto-clear field user | Kosongkan Section/Unit user | |

**User's choice:** Blokir deactivate jika ada user

---

## Validasi saat Login/Akses

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak perlu | Dengan cascade dan blokir deactivate, data selalu sinkron | ✓ |
| Warning di profile | Peringatan jika Section/Unit tidak ditemukan | |
| Blokir akses | User dengan Section/Unit invalid tidak bisa akses | |

**User's choice:** Tidak perlu (Recommended)

---

## Reparent (Pindah Unit)

| Option | Description | Selected |
|--------|-------------|----------|
| Ya, auto-update Section | User yang Unit-nya dipindah, Section ikut berubah | ✓ |
| Tidak, hanya update Unit | Hanya Unit name, Section tetap | |
| Blokir reparent jika ada user | Tidak bisa pindah unit yang punya user | |

**User's choice:** Ya, auto-update Section (Recommended)

---

## Claude's Discretion

- Pendekatan teknis cascade query (raw SQL vs LINQ)
- Error handling dan rollback
- Urutan operasi dalam transaksi

## Deferred Ideas

None
