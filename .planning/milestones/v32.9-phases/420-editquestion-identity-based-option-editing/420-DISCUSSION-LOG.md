# Phase 420: EditQuestion Identity-Based Option Editing - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-06-25
**Phase:** 420-editquestion-identity-based-option-editing
**Areas discussed:** Carrier Id & anti-tamper, Edit kebenaran opsi terjawab, Definisi "sudah dijawab", Pesan & pelabelan terblok

---

## Gray area selection

| Option | Selected |
|--------|----------|
| Carrier Id & anti-tamper | ✓ |
| Edit kebenaran opsi terjawab | ✓ |
| Definisi "sudah dijawab" | ✓ |
| Pesan & pelabelan terblok | ✓ |

**User's choice:** Semua 4 gray area.

---

## Carrier Id & anti-tamper

| Option | Description | Selected |
|--------|-------------|----------|
| OptionInput.Id + tolak asing | Tambah `int? Id` ke OptionInput (revisi T-418-06); validasi Id ∈ q.Options; baris Id-asing → tolak seluruh edit (fail-closed) | ✓ |
| Param paralel optionIds[] | Pertahankan OptionInput tanpa Id; bawa Id lewat list paralel + validasi + tolak | |
| Id asing = opsi baru | Id asing/kosong diperlakukan opsi baru (ADD), tak ditolak | |

**User's choice:** OptionInput.Id + tolak asing
**Notes:** Revisi sadar atas T-418-06 — Id kini wajib untuk identity; anti-tamper via validasi server eksplisit (Id ∈ q.Options), fail-closed.

---

## Edit kebenaran opsi terjawab

| Option | Description | Selected |
|--------|-------------|----------|
| Izinkan + modal D-09 | Edit teks/IsCorrect opsi terjawab diizinkan (match by Id); andalkan modal affectedSessions D-09 existing | ✓ |
| Izinkan diam-diam | Izinkan tanpa peringatan tambahan | |
| Blok ubah IsCorrect | Teks boleh, IsCorrect opsi terjawab ditolak | |

**User's choice:** Izinkan + modal D-09
**Notes:** Memenuhi SC#3; tak ada gerbang baru, modal peringatan existing cukup.

---

## Definisi "sudah dijawab"

| Option | Description | Selected |
|--------|-------------|----------|
| Semua response (apa adanya) | SEMUA PackageUserResponse menunjuk opsi (apa pun status sesi), konsisten guard existing | ✓ |
| Hanya sesi submitted/selesai | Persempit ke sesi sudah submit/selesai | |

**User's choice:** Semua response (apa adanya)
**Notes:** Fail-safe — makna jawaban manapun tak boleh berubah, apa pun status sesi.

---

## Pesan & pelabelan terblok

| Option | Description | Selected |
|--------|-------------|----------|
| Urutan tersimpan + cuplik teks | Huruf A–F dari OrderBy Id + cuplikan teks opsi terblok (opsi mungkin sudah hilang dari form) | ✓ |
| Huruf saja (urutan tersimpan) | Format existing apa adanya, huruf saja | |
| Posisi tampilan form sekarang | Huruf dari posisi baris form saat ini | |

**User's choice:** Urutan tersimpan + cuplik teks
**Notes:** Opsi terblok kemungkinan sudah hilang dari tampilan form → cuplikan teks membantu HC mengenali; tone pesan ramah existing dipertahankan.

## Claude's Discretion

- Panjang truncate cuplikan teks di pesan D-04.
- Anti-tamper sebagai blok awal terpisah vs menyatu di loop (selama fail-closed sebelum SaveChanges).
- Inline vs helper untuk set-difference removedOptionIds (OptionShrinkGuard tetap dipakai).

## Deferred Ideas

- Reorder opsi (drag) — out of scope.
- Data-repair response historis ter-relabel — defer s/d bukti korupsi nyata (REQUIREMENTS Future).
- Form Inject opsi — di luar jalur bug.
