---
phase: 418-opsi-jawaban-dinamis-2-6
fixed_at: 2026-06-24T00:00:00Z
review_path: .planning/phases/418-opsi-jawaban-dinamis-2-6/418-REVIEW.md
iteration: 1
findings_in_scope: 5
fixed: 4
documented: 1
skipped: 0
status: all_fixed
---

# Phase 418: Laporan Perbaikan Code Review — Opsi Jawaban Dinamis 2–6

**Diperbaiki:** 2026-06-24
**Review sumber:** `.planning/phases/418-opsi-jawaban-dinamis-2-6/418-REVIEW.md`
**Iterasi:** 1

**Ringkasan:**
- Temuan dalam scope: 5 (1 Warning + 4 Info)
- Diperbaiki (Info): 4
- Didokumentasikan (Warning, sesuai instruksi — bukan redesign): 1
- Dilewati: 0

**Verifikasi global:**
- `dotnet build HcPortal.csproj` → **0 Error** (24 warning, semuanya nullable-ref pre-existing di file tak terkait; tidak ada yang diperkenalkan oleh fix ini).
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OptionValidation|FullyQualifiedName~EditShrinkGuard"` → **17 Passed / 0 Failed / 0 Skipped** (GREEN, dijalankan 2× — sebelum & sesudah perubahan helper IN-01, hasil identik).
- migration=FALSE dipatuhi: tidak ada perubahan di `Migrations/` atau `Data/`.
- Authz / antiforgery / guard edit-shrink D-418-02 / grading **tidak disentuh** (WR-01 hanya komentar; IN-01 menjaga behavior identik).
- Tidak ada `@<digit>`/`@<identifier>` tak ter-escape yang diperkenalkan di `<script>`.

## Issue Diperbaiki

### IN-03: `#injAuthError` tidak punya `role="alert"`

**File dimodifikasi:** `Views/Admin/_InjectQuestionForm.cshtml`
**Commit:** `94d88165`
**Perbaikan diterapkan:** Menambahkan `role="alert"` pada container error form inject `#injAuthError` agar diumumkan screen reader, paritas dengan `#authError` authoring (UI-SPEC a11y). Hanya atribut markup, tanpa perubahan logika.

### IN-02: `resetForm()` tidak menyusutkan baris ekstra setelah batal edit soal 5–6 opsi

**File dimodifikasi:** `Views/Admin/ManagePackageQuestions.cshtml`
**Commit:** `4c76a0ab`
**Perbaikan diterapkan:** Menambahkan `ensureRowCount(4)` di `resetForm()` SEBELUM `IMAGE_FIELDS.forEach(resetImageField)`. Saat HC membatalkan edit soal 5/6 opsi, baris opsi kini disusutkan kembali ke 4 default (A–D) sehingga tidak ada baris ekstra tersisa di mode "Tambah Soal". Penempatan sebelum loop reset gambar memastikan `IMAGE_FIELDS` dibangun ulang oleh `reletterRows()` (di dalam `ensureRowCount`) untuk jumlah baris yang benar.

### IN-04: Komentar `ensureRowCount` menyebut rentang "(1..6)" padahal di-clamp ke MIN_OPTIONS (2)

**File dimodifikasi:** `Views/Admin/ManagePackageQuestions.cshtml`
**Commit:** `4c76a0ab` (digabung dengan IN-02 — perubahan di file yang sama)
**Perbaikan diterapkan:** Mengganti komentar "(1..6)" → "(2..6)" dan menambahkan klarifikasi "di-clamp ke MIN_OPTIONS (2, D-418-01)". Murni dokumentasi; kode `Math.max(MIN_OPTIONS, ...)` sudah benar dan tidak diubah.

### IN-01: `.Distinct()` redundan setelah `.Intersect()`

**File dimodifikasi:** `Helpers/OptionShrinkGuard.cs`
**Commit:** `e094ce48`
**Perbaikan diterapkan:** Menghapus `.Distinct()` setelah `.Intersect()` di `FindBlockedOptionIds` karena LINQ `Intersect` sudah mengembalikan elemen distinct (set-semantics). Behavior identik — diverifikasi oleh `EditShrinkGuardLogicTests` + `OptionValidationTests` yang tetap 17/17 GREEN setelah perubahan. Kontrak dokumentasi tetap valid (hasil tetap distinct dari `Intersect`).

## Issue Didokumentasikan (Warning — TIDAK di-redesign)

### WR-01: Hapus opsi-tengah yang sudah dijawab tidak diblokir guard — jawaban peserta di-relabel diam-diam

**File:** `Controllers/AssessmentAdminController.cs:8120-8153` (loop upsert posisional) berinteraksi dengan guard `:8030-8069`
**Commit:** `903a123a`
**Severity:** MED (Warning), **pre-existing** (sudah ada sejak soal 4-opsi, bukan diperkenalkan Phase 418).

**Keputusan:** TIDAK di-redesign. Sesuai instruksi objektif, ini konsekuensi sah dari mekanisme upsert-posisional yang **dikunci spec** (preserve `PackageOption.Id` by posisi, RESEARCH Pattern 2). Fix penuh = editing opsi berbasis **IDENTITAS** = keputusan produk di luar scope 418. Yang dilakukan adalah **mendokumentasikan**, bukan mengubah perilaku:

1. **Komentar kode** ditambahkan di dekat guard edit-shrink / loop upsert (`AssessmentAdminController.cs` ~8030) yang menjelaskan: guard hanya menangkap penghapusan opsi dari slot EKOR (`i >= keep`); penghapusan opsi TENGAH membuat opsi di bawahnya geser naik posisi sehingga record-Id bertahan tapi teksnya di-relabel → guard tidak menyala → makna jawaban peserta bisa berubah senyap; bukan crash (FK-Restrict 500 tetap tertutup); fix penuh butuh editing berbasis identitas; jangan perketat tanpa konfirmasi D-418-02.
2. **Entry backlog** ditambahkan ke `.planning/ROADMAP.md` bagian 999.x: **Phase 999.15** (nomor bebas berikutnya setelah 999.14), judul "Edit soal: hapus opsi-tengah yang sudah dijawab me-relabel jawaban peserta secara senyap (upsert posisional)", severity MED, pre-existing, mengikuti format entry 999.x lain (Goal + Context + opsi fix termasuk pendekatan identity-based + Requirements/Plans TBD).

Tidak ada perubahan pada guard, grading, atau mekanisme upsert. Build + test tetap hijau (perubahan komentar-saja + Markdown).

## Riwayat Commit (atomik per-temuan)

| Commit | Temuan | Ringkasan |
|--------|--------|-----------|
| `903a123a` | WR-01 | docs: dokumentasikan batasan upsert posisional + backlog 999.15 |
| `94d88165` | IN-03 | fix: `role="alert"` pada `#injAuthError` |
| `4c76a0ab` | IN-02 + IN-04 | fix: `resetForm` susutkan baris ke 4 + koreksi komentar `ensureRowCount` (same-file) |
| `e094ce48` | IN-01 | fix: hapus `.Distinct()` redundan di `OptionShrinkGuard` |

> Catatan: IN-02 dan IN-04 keduanya di `Views/Admin/ManagePackageQuestions.cshtml`, sehingga digabung dalam satu commit atomik (`--files` mem-commit seluruh file; pemisahan per-baris tidak mungkin tanpa staging parsial). Keempat temuan lain ter-commit terpisah.

---

_Diperbaiki: 2026-06-24_
_Fixer: Claude (gsd-code-fixer)_
_Iterasi: 1_
