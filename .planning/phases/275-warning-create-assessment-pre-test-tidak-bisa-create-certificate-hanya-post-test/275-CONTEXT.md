---
phase: 275
name: Warning create assessment — pre test tidak bisa create certificate, hanya post test
created: 2026-03-28
source: auto-discuss
---

# Phase 275 Context

## Phase Goal
Menambahkan warning/validasi di form Create Assessment: jika assessment bertipe "Pre Test", maka sertifikat tidak bisa di-generate (GenerateCertificate harus disabled). Hanya assessment bertipe "Post Test" yang bisa generate sertifikat.

## Current State
- Model `AssessmentSession` sudah punya field `GenerateCertificate` (bool)
- Form `CreateAssessment.cshtml` punya checkbox "Terbitkan Sertifikat" dengan toggle switch
- **Tidak ada** konsep pre-test / post-test di model atau view saat ini
- Kategori assessment dikelola via `AssessmentCategory` model (parent-child hierarchy)

## Decisions

### 1. Konsep Pre-Test / Post-Test
**Status:** Perlu input user
**Gray area:** Bagaimana pre-test vs post-test dibedakan? Apakah ini:
- (a) Field baru `AssessmentType` (enum: PreTest/PostTest) di `AssessmentSession`
- (b) Berdasarkan sub-kategori yang sudah ada
- (c) Berdasarkan nama/judul assessment
- (d) Atau konsep lain?

**[auto] Selected:** Opsi (a) — field baru karena paling eksplisit dan reliable. Tapi ini butuh konfirmasi user karena menyangkut domain knowledge.

### 2. Behavior Warning
**Decision:** Saat admin memilih tipe "Pre Test" di form Create Assessment:
- Checkbox "Terbitkan Sertifikat" otomatis di-uncheck dan disabled
- Tampilkan info text: "Pre Test tidak dapat menerbitkan sertifikat. Hanya Post Test yang bisa menerbitkan sertifikat."
- Backend juga enforce: jika tipe PreTest, force GenerateCertificate = false

### 3. Scope
**Decision:** Hanya warning/validasi di form create. Tidak mengubah:
- Flow ujian
- Grading
- Halaman results
- Assessment yang sudah ada (backward compatible, default = PostTest atau null)

## Reusable Assets
- `Views/Admin/CreateAssessment.cshtml` — form wizard multi-step, JS validation sudah ada
- `Controllers/AdminController.cs` — POST CreateAssessment action
- `Models/AssessmentSession.cs` — model utama

## Open Questions (for user)
1. Apakah benar perlu field baru "Tipe Assessment" (Pre Test / Post Test)? Atau ini merujuk ke sesuatu yang sudah ada di sistem?
2. Apakah assessment yang sudah ada perlu di-migrate ke tipe tertentu?

## Deferred Ideas
- None
