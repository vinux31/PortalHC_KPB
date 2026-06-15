# Phase 353: Admin Backend Gambar - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-08
**Phase:** 353-admin-backend-gambar-crud-sync-atomic-delete
**Areas discussed:** Layout form upload, Edit (tampil & hapus/ganti), Preview sebelum simpan, Feedback validasi gagal

---

## Layout Form Upload

| Option | Description | Selected |
|--------|-------------|----------|
| A: Inline kontekstual | Gambar soal bawah textarea + gambar opsi inline tiap baris A-D | ✓ |
| B: Section terpisah | Satu blok 'Gambar' kumpulkan soal + 4 opsi sekaligus | |
| C: Collapse toggle | Tombol '+ Tambah gambar' per item, field tersembunyi default | |

**User's choice:** A — Inline kontekstual
**Notes:** User minta mockup HTML real untuk membandingkan. Dibuat `docs/mockup-presentasi/353-layout-form-gambar-mockup.html` (3 opsi side-by-side, contoh diagram pompa). User klarifikasi dulu bahwa page ini = form admin tambah/edit soal (`ManagePackageQuestions.cshtml`), bukan preview. Lalu pilih Inline.

---

## Edit — Tampil & Hapus/Ganti

| Option | Description | Selected |
|--------|-------------|----------|
| Thumbnail + checkbox hapus + file ganti | Tampil thumbnail lama; checkbox hapus; file baru=ganti | ✓ |
| Tanpa checkbox — ganti saja | Hanya ganti, tak bisa hapus (langgar IMG-06) | |

**Konflik hapus+file-baru:**

| Option | Description | Selected |
|--------|-------------|----------|
| File baru menang | Pilih file baru abaikan checkbox hapus, gambar baru tersimpan | ✓ |
| Hapus menang | Checkbox hapus prioritas, file baru diabaikan | |

**User's choice:** Thumbnail + checkbox hapus + ganti; file baru menang saat konflik.
**Notes:** `<input type=file>` tak bisa prefill → thumbnail dari ImagePath. JSON EditQuestion GET perlu +imagePath+imageAlt (Gap 3).

---

## Preview

| Option | Description | Selected |
|--------|-------------|----------|
| Keduanya: thumbnail JS + preview render | FileReader instan saat pilih file + _PreviewQuestion render setelah simpan | ✓ |
| Cukup preview render setelah simpan | Hanya _PreviewQuestion render, tanpa thumbnail instan | |

**User's choice:** Keduanya.
**Notes:** _PreviewQuestion belum punya `<img>` → tambah (RND-04).

---

## Feedback Validasi Gagal

| Option | Description | Selected |
|--------|-------------|----------|
| Alert atas (TempData) | Server validasi gagal → TempData[Error] alert merah + repopulate | ✓ |
| Inline dekat field | Error di bawah field gagal, butuh client JS tambahan | |

**User's choice:** Alert atas (TempData).
**Notes:** Konsisten pola existing CreateQuestion/EditQuestion.

## Claude's Discretion
- Nama field form, DTO multipart binding, struktur JS thumbnail handler, urutan operasi POST.

## Deferred Ideas
- Render 6 layar peserta → Phase 354. Test/UAT → Phase 355. Server-side resize → ditolak (352 D-04).
