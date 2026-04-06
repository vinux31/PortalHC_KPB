---
phase: 296-data-foundation-gradingservice-extraction
security_auditor: gsd-security-auditor
asvs_level: 1
block_on: open
audit_date: 2026-04-06
result: SECURED
threats_total: 6
threats_closed: 6
threats_open: 0
---

# Security Audit Report — Phase 296

## Summary

**Phase:** 296 — Data Foundation: GradingService Extraction
**ASVS Level:** 1
**Result:** SECURED
**Threats Closed:** 6/6
**Threats Open:** 0/6

---

## Threat Verification

| Threat ID | Category | Disposition | Evidence |
|-----------|----------|-------------|----------|
| T-296-01 | Tampering | accept | Accepted risk: kolom nullable/default, rollback via Down() tersedia. Didokumentasikan di Accepted Risks di bawah. |
| T-296-02 | Tampering | mitigate | Services/GradingService.cs:156 — `.Where(s => s.Id == session.Id && s.Status != "Completed")` pada ExecuteUpdateAsync memblokir double-grading race condition. |
| T-296-03 | Tampering | mitigate | Services/GradingService.cs:218 — ExecuteUpdateAsync WHERE `s.NomorSertifikat == null`; retry loop 3x dengan DbUpdateException catch di baris 224 untuk duplicate key race condition pada sequence generation. |
| T-296-04 | Denial of Service | mitigate | Services/GradingService.cs:181 — `_context.TrainingRecords.AnyAsync(...)` sebelum insert mencegah duplikasi TrainingRecord. |
| T-296-05 | Repudiation | accept | Accepted risk: AuditLog calls dipertahankan di controller (tidak dipindah ke GradingService). Tidak ada perubahan pada audit trail. Dikonfirmasi di 296-03-SUMMARY.md. |
| T-296-06 | Tampering | accept | Accepted risk: Answer upsert logic (PackageUserResponses dari form POST ke DB + SaveChangesAsync) tetap di CMPController sebelum pemanggilan GradingService. GradingService hanya grade dari DB. Dikonfirmasi di 296-03-SUMMARY.md. |

---

## Accepted Risks Log

| Threat ID | Category | Justification | Owner |
|-----------|----------|---------------|-------|
| T-296-01 | Tampering — Migration | Semua kolom baru nullable atau memiliki default value (HasManualGrading: false). Tidak ada perubahan existing data. Rollback tersedia via Down() yang menghapus 7 kolom. Risiko data corruption saat migration dinilai dapat diterima. | Phase 296 executor |
| T-296-05 | Repudiation — Controller refactoring | AuditLog calls dipertahankan di controller setelah refactoring ke GradingService. Tidak ada endpoint baru, tidak ada perubahan trust boundary. Audit trail tidak berubah. | Phase 296 executor |
| T-296-06 | Tampering — SubmitExam answer upsert | Answer upsert logic tidak berubah — tetap di controller sebelum GradingService dipanggil, memastikan DB berisi jawaban terbaru sebelum grading. Pola ini sesuai dengan keputusan desain D-01 dan anti-pattern di RESEARCH.md. | Phase 296 executor |

---

## Unregistered Threat Flags

Tidak ada. 296-03-SUMMARY.md `## Threat Flags` menyatakan: "Tidak ada surface baru. Refactoring murni — tidak ada endpoint baru, tidak ada perubahan trust boundary."

---

## Verification Details

### T-296-02 — Race Condition Double-Grading

File: `Services/GradingService.cs`

Pattern verified: ExecuteUpdateAsync dengan filter `s.Status != "Completed"` (baris 156).
Saat rowsAffected == 0, GradingService return false dan caller melakukan early return tanpa crash.

### T-296-03 — NomorSertifikat Duplicate Race Condition

File: `Services/GradingService.cs`

Pattern verified:
- Loop retry maksimum 3x (maxCertAttempts) dengan CertNumberHelper.GetNextSeqAsync + Build
- ExecuteUpdateAsync WHERE `s.NomorSertifikat == null` (baris 218) — mencegah overwrite
- DbUpdateException catch dengan kondisi `certAttempts < maxCertAttempts && CertNumberHelper.IsDuplicateKeyException(ex)` (baris 224)

### T-296-04 — TrainingRecord Duplicate

File: `Services/GradingService.cs`

Pattern verified: `_context.TrainingRecords.AnyAsync(...)` (baris 181) sebelum Add mencegah duplikasi.
Pola ini identik dengan guard yang ada di controller sebelum ekstraksi.

---

## Files Audited

- `Services/GradingService.cs` — implementasi mitigasi T-296-02, T-296-03, T-296-04
- `Controllers/AssessmentAdminController.cs` — wiring ke GradingService (T-296-05)
- `Controllers/CMPController.cs` — answer upsert sebelum GradingService call (T-296-06)
- `Migrations/20260406075820_AddAssessmentV14Columns.cs` — rollback via Down() (T-296-01)
- `.planning/phases/296-data-foundation-gradingservice-extraction/296-02-SUMMARY.md` — konfirmasi implementasi
- `.planning/phases/296-data-foundation-gradingservice-extraction/296-03-SUMMARY.md` — konfirmasi tidak ada threat surface baru
