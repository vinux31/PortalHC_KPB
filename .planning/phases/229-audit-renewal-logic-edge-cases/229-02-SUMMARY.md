---
phase: 229-audit-renewal-logic-edge-cases
plan: 02
subsystem: api
tags: [renewal, certificate, bulk-validation, audit-report, edge-cases]

requires:
  - phase: 229-01
    provides: MapKategori DB lookup, double renewal guard, FK XOR guard

provides:
  - Mixed-type bulk renewal validation guard di CreateAssessment POST dan AddTraining POST
  - HTML audit report docs/audit-renewal-logic-v8.1.html dengan 4 section lengkap
  - Verifikasi D-07 CertificateType (tidak perlu perubahan)
  - Verifikasi empty state EDGE-03 (tidak perlu perubahan)

affects:
  - 230-audit-renewal-ui-cross-page

tech-stack:
  added: []
  patterns:
    - "Mixed-type bulk guard — cek fkMapType validity sebelum ModelState.IsValid di kedua endpoint"
    - "HTML audit report — standalone dokumen dengan Fix table, Verifikasi table, SQL query identifikasi"

key-files:
  created:
    - docs/audit-renewal-logic-v8.1.html
  modified:
    - Controllers/AdminController.cs

decisions:
  - "[Phase 229-02]: Mixed-type bulk validation guard ditambahkan di sisi server — fkMapType harus 'session' atau 'training', tidak boleh kosong atau nilai lain"
  - "[Phase 229-02]: D-07 audit — AssessmentSession tidak perlu field CertificateType; null certificateType + null ValidUntil = Expired adalah behavior yang benar"
  - "[Phase 229-02]: Empty state EDGE-03 sudah ada di _RenewalGroupedPartial.cshtml dengan icon bi-patch-check-fill, tidak perlu perubahan"
  - "[Phase 229-02]: Audit report menggunakan format HTML standalone (mengikuti style audit-v7.7.html) — tidak ada data migration per D-03"

metrics:
  duration: ~20 menit
  completed_date: "2026-03-22"
  tasks_completed: 2
  files_modified: 1
  files_created: 1
---

# Phase 229 Plan 02: Mixed-Type Bulk Validation & HTML Audit Report

Mixed-type bulk renewal validation guard di dua endpoint + HTML audit report v8.1 mendokumentasikan semua 5 fix dan 6 verifikasi dari Phase 229.

## Tasks Completed

### Task 1: Mixed-type bulk validation + D-07 audit + verifikasi empty state

**A. Mixed-Type Bulk Validation (EDGE-01, D-11):**

Ditambahkan guard di dua endpoint:

- `CreateAssessment POST` (baris ~1241): cek apakah `RenewalFkMapType` valid ("session" atau "training") saat bulk renewal (fkMap ada dan UserIds > 1). Jika tidak valid, tambah ModelState error.
- `AddTraining POST` (baris ~5609): cek apakah `fkMapType` valid ("training" atau "session") saat bulk renewal (fkMap ada dan bulkUserIds > 1). Jika tidak valid, tambah ModelState error.

Error message: *"Bulk renewal tidak dapat mencampur tipe Assessment dan Training. Renew per tipe secara terpisah."*

**B. D-07 Audit CertificateType:**

`AssessmentSession` model TIDAK memiliki field `CertificateType`. `DeriveCertificateStatus(a.ValidUntil, null)` dipanggil dengan `certificateType: null`. Karena `null != "Permanent"`, saat `ValidUntil == null` → dikembalikan `Expired`. Ini BENAR: assessment tanpa ValidUntil = perlu renewal. Tidak perlu perubahan.

**C. Verifikasi Empty State (EDGE-03):**

`_RenewalGroupedPartial.cshtml` baris 20-27 sudah memiliki empty state:
- Icon: `bi-patch-check-fill` (hijau, `text-success`)
- Text: "Tidak ada sertifikat yang perlu di-renew"
- Sub-text: "Semua sertifikat aktif atau sudah di-renew."

Sesuai D-12. Tidak perlu perubahan.

**Commit:** `491438b` — feat(229-02): add mixed-type bulk renewal validation guard

### Task 2: Generate HTML audit report data existing (D-02)

File `docs/audit-renewal-logic-v8.1.html` dibuat dengan 4 section:

1. **Section 1 (Fix yang Diterapkan):** 5 fix dalam tabel — MapKategori (D-08), Double Renewal Guard (D-10), FK XOR (D-04), Mixed-Type Bulk (D-11), CDPController MapKategori (Pitfall 1).

2. **Section 2 (Verifikasi OK):** 6 area yang diverifikasi sudah correct — FK 4 kombinasi (LDAT-01), Badge Count (LDAT-02), DeriveCertificateStatus (LDAT-03), GroupKey Decode (LDAT-04), Empty State (EDGE-03), D-07 CertificateType (EDGE-02).

3. **Section 3 (Data Lama):** Penjelasan D-03 (no migration) + 2 SQL query untuk identifikasi record lama tanpa FK renewal yang mungkin bermasalah (TrainingRecords dan AssessmentSessions).

4. **Section 4 (Arsitektur):** Tabel FK model 4 kombinasi valid + tabel validation chain 3 guard.

**Commit:** `1ffe52c` — docs(229-02): generate HTML audit report renewal logic v8.1

## Verification Results

```
grep -n "Bulk renewal tidak dapat mencampur" Controllers/AdminController.cs
1246:                    ModelState.AddModelError("", "Bulk renewal tidak dapat mencampur tipe Assessment dan Training...")
5613:                    ModelState.AddModelError("", "Bulk renewal tidak dapat mencampur tipe Assessment dan Training...")

dotnet build --no-restore → 0 Error(s)

docs/audit-renewal-logic-v8.1.html → exists, 15860 bytes, 8 h2/h3 elements
```

## Deviations from Plan

None — plan dieksekusi persis sesuai rencana.

## Known Stubs

None.

## Self-Check: PASSED

- `Controllers/AdminController.cs` — modified, guard ada di 2 lokasi (baris 1246, 5613)
- `docs/audit-renewal-logic-v8.1.html` — created, 15860 bytes
- Commit `491438b` — exists
- Commit `1ffe52c` — exists
- `dotnet build` — 0 errors
