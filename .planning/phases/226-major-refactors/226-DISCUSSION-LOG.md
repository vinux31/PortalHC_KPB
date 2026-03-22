# Phase 227: Major Refactors - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-22
**Phase:** 227-major-refactors
**Areas discussed:** Question Bank CRUD & Navigasi, Import & Pemilihan Soal, Migrasi Legacy Path, NomorSertifikat Timing

---

## Question Bank CRUD & Navigasi

| Option | Description | Selected |
|--------|-------------|----------|
| CMP Hub | Tambah card di CMP Hub karena Question Bank terkait assessment | |
| Kelola Data (Admin) | Tambah di Admin/Index hub bersama data master lainnya | |
| Keduanya | Link di CMP Hub + juga muncul di Kelola Data | |

**User's choice:** "Question Bank kayaknya tidak perlu" — soal tetap dikelola di ManagePackages saja.
**Notes:** User tidak merasa perlu halaman Question Bank terpisah. Soal tetap terikat ke assessment session.

---

## Import & Pemilihan Soal (QBNK-02/03)

| Option | Description | Selected |
|--------|-------------|----------|
| Skip semua QBNK | Import sudah ada di ImportPackageQuestions, tidak perlu bank cross-assessment | ✓ |
| Keep QBNK-03 saja | Cross-assessment reuse tanpa halaman bank terpisah | |
| Keep QBNK-02 dan QBNK-03 | Import ke bank + cross-assessment reuse | |

**User's choice:** Skip semua QBNK
**Notes:** ImportPackageQuestions yang ada sudah cukup. Tidak perlu fitur reuse soal antar assessment.

---

## Migrasi Legacy Path

### Strategi migrasi

| Option | Description | Selected |
|--------|-------------|----------|
| Data migration script | Convert semua legacy data ke package format, hapus legacy code | ✓ |
| Freeze legacy, new only package | Session lama read-only, baru wajib package | |
| Hard cutoff + drop tables | Migrasi + drop, data yang gagal hilang | |

### Session aktif

| Option | Description | Selected |
|--------|-------------|----------|
| Tidak ada / tidak tahu | Script akan cek dan report | |
| Mungkin ada | Perlu cek dulu, handle khusus | ✓ |

### Post-migrasi

| Option | Description | Selected |
|--------|-------------|----------|
| Drop tabel setelah verifikasi | Drop tabel legacy + hapus legacy code setelah verifikasi | ✓ |
| Keep tabel tapi kosongkan | Tabel tetap ada, data dipindah | |
| Keep tabel sebagai archive | Data lama tetap sebagai backup | |

**User's choice:** Migration script → cek session aktif → drop tabel setelah verifikasi
**Notes:** Full migration path — no legacy code preserved.

---

## NomorSertifikat Timing

### Bad data handling

| Option | Description | Selected |
|--------|-------------|----------|
| Nullify semua yang belum lulus | Set NULL untuk IsPassed != true | ✓ |
| Keep as-is, fix forward only | Data lama tidak diubah | |
| Nullify + re-sequence | Nullify + re-generate nomor urut | |

### Manual entry

| Option | Description | Selected |
|--------|-------------|----------|
| Manual entry tetap boleh | AddTraining/ImportTraining tetap terima NomorSertifikat custom | ✓ |
| Semua harus auto-generate | NomorSertifikat selalu auto-generate | |

**User's choice:** Nullify bad data + keep manual entry
**Notes:** Auto-generate hanya di assessment flow (SubmitExam + IsPassed). Manual entry dari AddTraining/ImportTraining tetap diterima.

---

## Claude's Discretion

- Urutan eksekusi tasks
- Migration script details (batch size, rollback)
- Verification queries

## Deferred Ideas

- Question Bank terpisah (QBNK-01/02/03) — user decided not needed now
