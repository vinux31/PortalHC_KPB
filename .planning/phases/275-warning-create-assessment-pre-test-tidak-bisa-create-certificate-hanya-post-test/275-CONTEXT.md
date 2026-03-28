---
phase: 275
name: Warning create assessment — pre test tidak bisa create certificate, hanya post test
created: 2026-03-28
source: discuss
---

# Phase 275 Context

## Phase Goal
Tambahkan warning text dinamis di form Create Assessment: jika judul mengandung "pre test"/"pretest" dan checkbox "Terbitkan Sertifikat" dicentang, tampilkan warning bahwa pre-test biasanya tidak menerbitkan sertifikat.

## Decisions

### 1. Deteksi Pre-Test: Berdasarkan Judul (Opsi C)
**Decision:** Deteksi kata "pre test" / "pretest" di field judul assessment via JavaScript. Tidak ada field baru, tidak ada perubahan model/database. Murni frontend.

### 2. Behavior: Warning Text Non-Blocking
**Decision:** Warning muncul saat KEDUA kondisi terpenuhi:
- Judul mengandung "pre test" atau "pretest" (case-insensitive)
- Checkbox "Terbitkan Sertifikat" dicentang

Warning text kuning (alert-warning) di bawah checkbox:
> ⚠️ "Judul mengandung 'Pre Test'. Pre Test biasanya tidak menerbitkan sertifikat."

Admin tetap bisa override — warning tidak blocking, tidak disable checkbox.

### 3. Scope
**Decision:** Murni frontend JS di `CreateAssessment.cshtml`. Tidak mengubah:
- Model / database / migration
- Backend validation
- Flow ujian, grading, results
- Assessment yang sudah ada

## Reusable Assets
- `Views/Admin/CreateAssessment.cshtml` — form wizard, field Title (id="Title"), checkbox GenerateCertificate
- JS validation pattern sudah ada di file yang sama

## Deferred Ideas
- None
