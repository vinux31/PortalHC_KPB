---
phase: 423-certificate-issuance-consistency
security_reviewed: 2026-06-24
asvs_level: 1
block_on: high
threats_total: 12
threats_closed: 12
threats_open: 0
status: SECURED
---

# 423 Security Audit — SECURED

**Phase:** 423 — Certificate Issuance Consistency
**ASVS Level:** 1
**Threats Closed:** 12/12
**Threats Open:** 0

---

## Threat Verification

| Threat ID | Kategori | Disposisi | Status | Bukti Kode |
|-----------|----------|-----------|--------|------------|
| T-423-01 | Spoofing | mitigate | CLOSED | `Helpers/CertIssuanceRules.cs:15-18` — `ShouldIssueCertificate` menolak `AssessmentType == "PreTest"` via konstanta `AssessmentConstants.AssessmentType.PreTest`; dipakai di 4 site (GradingService:301, GradingService:553, AssessmentAdminController:3919, TrainingAdminController melalui helper). Pure-tested `CertIssuanceRulesTests`. |
| T-423-02 | Tampering | mitigate | CLOSED | `Helpers/CertNumberHelper.cs:51-79` — `TryAssignNextSeqAsync`: filtered `WHERE NomorSertifikat==null` + `ExecuteUpdateAsync` + `catch IsDuplicateKeyException` + jitter `Task.Delay(Random.Shared.Next(10,60))` di atas filtered unique index `IX_AssessmentSessions_NomorSertifikat_Unique`. WR-01 sudah diperbaiki: `updated==0` → `AnyAsync` re-query (tidak anggap sukses buta). |
| T-423-03 | Information Disclosure | accept | CLOSED | `CertIssuanceRules.cs` EF-free; hanya hitung `bool`/`DateOnly?`/`string`. Tidak ada DbContext/data sensitif di-ekspos. Diterima per catatan accepted risks di bawah. |
| T-423-04 | Denial of Service | accept | CLOSED | Cap 8 attempts + jitter 10-60ms bounded; `return false` non-destruktif (worker tidak terblok, sesi tetap `Completed`). Diterima per catatan accepted risks di bawah. |
| T-423-05 | Spoofing | mitigate | CLOSED | `CertIssuanceRules.ShouldIssueCertificate(session)` server-authoritative di semua 4 site: SITE 1 `GradingService.GradeAndCompleteAsync:301`, SITE 2 `GradingService.RegradeAfterEditAsync:553`, SITE 3 `AssessmentAdminController.FinalizeEssayGrading:3919` (CR-01 fix sudah di-sinkron `session.IsPassed=isPassed` sebelum gate di :3906), SITE 4 `TrainingAdminController.AddManualAssessment:720` (`ResemblesAutoCertFormat` reject). Integration test `CertIssuanceIntegrationTests` membuktikan `NomorSertifikat==null` untuk sesi PreTest di site nyata. |
| T-423-06 | Elevation/Tampering | mitigate | CLOSED | `AssessmentAdminController.cs:1030-1049` — `HasActiveCertForTitleAsync` dipanggil **UNCONDITIONAL** di luar cabang `if(!ConfirmDuplicateTitle)`. Guard berada di blok terpisah setelah double-renewal block (:1014). Definisi di :5942. Renewal dikecualikan via `!isRenewalModePost` (mencakup `RenewsSessionId`, `RenewsTrainingId`, `RenewalFkMap`). Guard tidak bisa di-bypass lewat `ConfirmDuplicateTitle`. |
| T-423-07 | Tampering | mitigate | CLOSED | `TrainingAdminController.cs:720` — `CertIssuanceRules.ResemblesAutoCertFormat(wc.NomorSertifikat)` menolak format yang menyerupai auto dengan `ModelState.AddModelError` (pesan menampilkan nomor offending setelah fix WR-02). `SaveChangesAsync` dibungkus `try/catch DbUpdateException when IsDuplicateKeyException` di :785 → `ModelState.AddModelError` ramah (bukan HTTP 500). |
| T-423-08 | Tampering/DoS | mitigate | CLOSED | `CertNumberHelper.TryAssignNextSeqAsync:51-79` — filtered `WHERE NomorSertifikat==null` + `ExecuteUpdateAsync` + retry cap 8 + jitter 10-60ms. `return false` non-destruktif: sesi tetap `Completed`, `UpdatedAt` di-stamp, predikat queryable `IsPassed==true && GenerateCertificate && AssessmentType!="PreTest" && NomorSertifikat==null` memungkinkan HC recovery manual. `maxCertAttempts` literal = 0 di `GradingService` (loop inline dihapus). |
| T-423-09 | Elevation | mitigate | CLOSED | `FinalizeEssayGrading`: `[HttpPost]` + `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` di :3767-3769. `AddManualAssessment`: `[HttpPost]` + `[ValidateAntiForgeryToken]` + `[Authorize(Roles="Admin, HC")]` di TrainingAdminController:686-688. Kedua endpoint utuh pasca-refactor. |
| T-423-10 | Information Disclosure | accept | CLOSED | Badge umur `PendingAgeBadgeClass` hanya menampilkan jumlah hari ke HC yang sudah ber-RBAC akses halaman (`[Authorize(Roles="Admin, HC")]` existing). Tidak ada data sensitif baru yang ter-ekspos. Diterima per catatan accepted risks di bawah. |
| T-423-11 | Tampering | mitigate | CLOSED | View-only — tidak ada write/auto-finalize di `EssayGrading.cshtml:48` maupun `AssessmentMonitoringDetail.cshtml:285,459`. Badge murni render Razor server-side (`@pCls`, `@pDays` auto-escaped). Tidak ada `Html.Raw` pada data pengguna. Status `PendingGrading` tidak berubah setelah GET (terkonfirmasi UAT @5270). |
| T-423-12 | Elevation | mitigate | CLOSED | Halaman `EssayGrading` dan `AssessmentMonitoringDetail` sudah dilindungi `[Authorize(Roles="Admin, HC")]` existing — tidak diubah fase ini. Badge tidak menambah attack surface baru. |

---

## Accepted Risks Log

| ID | Ancaman | Alasan Diterima |
|----|---------|-----------------|
| T-423-03 | Information Disclosure — `CertIssuanceRules` helper pure | EF-free; hanya menghasilkan `bool`/`DateOnly?`/`string` dari domain object server-side. Tidak ada data sensitif. Low-value. |
| T-423-04 | Denial of Service — retry loop `TryAssignNextSeqAsync` | Cap 8 attempts + jitter 10-60ms bounded; fallback non-destruktif (return false, worker tidak terblok). Tidak ada loop tak-berhingga. Cukup untuk volume operasional. |
| T-423-10 | Information Disclosure — badge umur PendingGrading | Menampilkan jumlah hari menunggu ke HC yang sudah memiliki akses RBAC halaman. Tidak ada data di luar apa yang HC sudah bisa lihat secara manual. |

---

## Unregistered Flags

Tidak ada threat flag tambahan dari SUMMARY.md yang tidak terpetakan ke threat register di atas.

---

## Catatan Tambahan

- **CR-01 (CRITICAL, FIXED):** `FinalizeEssayGrading` SITE 3 tidak menyinkron `session.IsPassed` in-memory setelah `ExecuteUpdateAsync`, menyebabkan `ShouldIssueCertificate(session)` selalu `false`. Diperbaiki di `AssessmentAdminController.cs:3906-3909` (sinkron `IsPassed`/`Score`/`Status`/`CompletedAt` sebelum gate cert). Paritas SITE 1/2 di `GradingService`.
- **WR-01 (WARNING, FIXED):** `TryAssignNextSeqAsync updated==0` → `return true` ambigu antara idempotent vs sessionId tidak valid. Diperbaiki dengan `AnyAsync` re-query di `CertNumberHelper.cs:69-70`.
- **WR-02 (WARNING, FIXED):** Pesan error CERT-04 menampilkan `UserId` GUID. Diganti dengan nomor offending `wc.NomorSertifikat` yang lebih bermakna bagi HC.
- **IN-01 (INFO, accepted):** `GetNextSeqAsync` load client-side — pre-existing, backlog. Tidak ada dampak correctness.
- **migration=FALSE:** Dikonfirmasi — tidak ada perubahan schema, migration file, atau `DbSet` baru.
- Razor badge output (`@pCls`, `@pDays`) auto-escaped oleh Razor engine — tidak ada XSS surface dari data pengguna.

---

_Security auditor: Claude (gsd-secure-phase)_
_Reviewed: 2026-06-24_
_ASVS Level: 1_
