---
phase: 393-backend-core-inject
type: security-audit
asvs_level: 2
block_on: critical
threats_total: 13
threats_closed: 13
threats_open: 0
unregistered_flags: 0
verdict: SECURED
audited: 2026-06-17
auditor: gsd-security-auditor
files_audited:
  - Services/InjectAssessmentService.cs
  - Models/InjectAssessmentDtos.cs
  - HcPortal.Tests/InjectAssessmentServiceTests.cs
  - Program.cs
  - Services/GradingService.cs (delegasi, read-only verify cert gate)
  - Data/ApplicationDbContext.cs (UNIQUE index verify)
---

# Phase 393 — Backend Core Inject — Security Audit (SECURITY.md)

**Milestone:** v32.2 "Inject Hasil Assessment Manual"
**Phase:** 393-backend-core-inject (INJ-01/INJ-02)
**ASVS Level:** 2 (default) | **block_on:** critical
**Verdict:** SECURED — 13/13 threats closed, 0 open, 0 unregistered flags.

Audit ini MEMVERIFIKASI bahwa mitigasi yang dideklarasikan di tiga blok `<threat_model>` PLAN (393-01/02/03) HADIR di kode terimplementasi. Bukan scan kerentanan baru. File implementasi READ-ONLY.

---

## Scope & Method

- 13 threat dari 3 register PLAN (T-393-01..04 Plan 01, T-393-05..10 Plan 02, T-393-11..13 Plan 03).
- Disposition: 12× `mitigate`, 1× `accept` (T-393-02, di-scope ke Plan 01 saja).
- Tiap `mitigate` → grep pola mitigasi di file yang dikutip mitigation plan → ditemukan = CLOSED.
- T-393-02 (`accept` di Plan 01) → CLOSED via verifikasi bahwa validasi yang ditangguhkan benar-benar HADIR di Plan 02 pre-flight (logged di Accepted Risks Log di bawah).
- SUMMARY 393-01/02/03 dicek untuk section `## Threat Flags` → tidak ada → 0 unregistered flags.

**Catatan boundary (sesuai constraints):** RBAC actor (Admin/HC) di-defer ke Phase 394 controller (V4 partial — service tidak punya HttpContext, by design). Service dengan BENAR menerima identitas actor (`actorUserId`/`actorName`) sebagai parameter eksplisit. Ini bukan gap — boundary HTTP belum aktif di lapisan service ini.

---

## Threat Verification

| Threat ID | Category | Disposition | Status | Evidence (file:line) |
|-----------|----------|-------------|--------|----------------------|
| T-393-01 | Spoofing | mitigate | CLOSED | `InjectAssessmentService.cs:42` — `InjectBatchAsync(InjectRequest req, string actorUserId, string actorName)`; actor tidak di-resolve di service, dipakai apa adanya di audit (`:59`, `:299`, `:312`) + `CreatedBy = actorUserId` (`:127`). Identitas di-inject dari caller. |
| T-393-02 | Tampering (score/cert/date) | accept (Plan 01) → mitigated Plan 02 | CLOSED | Plan 01 surface = kontrak POCO, nol write (`InjectAssessmentDtos.cs` — tidak ter-attach DbContext). Validasi nilai yang ditangguhkan HADIR di `InjectAssessmentService.cs:341-434` (`PreflightValidateAsync`): EssayScore `:389`, tanggal `:356-361`, cert manual `:407-431`. Lihat Accepted Risks Log. |
| T-393-03 | Repudiation (jejak inject) | mitigate | CLOSED | `InjectResult` membawa `SuccessSessionIds`/`SkippedNips`/`PerRowErrors` (`InjectAssessmentDtos.cs:76-78`); AuditLog 3 ActionType: `ManualInject` (`:301`), `ManualInjectSkipped` (`:314`), `ManualInjectRejected` (`:61`). |
| T-393-04 | Information Disclosure (test DB→Dev) | mitigate | CLOSED | `InjectAssessmentServiceTests.cs:26` — `DbName = $"HcPortalDB_Test_{Guid.NewGuid():N}"`; `:55` `EnsureDeletedAsync` di DisposeAsync. `HcPortalDB_Dev` tak disebut/disentuh. |
| T-393-05 | Tampering (EssayScore range) | mitigate | CLOSED | Pre-flight `:389` `EssayScore.Value < 0 || > qSpec.ScoreValue` (rentang 0..ScoreValue, BUKAN 0..100). Skor final DIHITUNG `GradeAndCompleteAsync` (`:224`) + `AssessmentScoreAggregator.Compute` (`:236`) — input persen tidak diterima. Grep `0\.\.100` = 0×. |
| T-393-06 | Tampering (double-cert/collision) | mitigate | CLOSED | UNIQUE index `IX_AssessmentSessions_NomorSertifikat_Unique` (`ApplicationDbContext.cs:226-229`) + pre-flight D-09 intra-batch (`:418`) + DB collision (`:424-429`) + retry 3× `IsDuplicateKeyException` (`:280`). |
| T-393-07 | Tampering (cert non-passing) | mitigate | CLOSED | `GradeAndCompleteAsync` gate cert pada `isPassed` (`GradingService.cs:287` `if (session.GenerateCertificate && isPassed)`); unified cert step inject juga gated `if (passedNow == true)` (`InjectAssessmentService.cs:262`). |
| T-393-08 | Integrity (partial write mid-batch) | mitigate | CLOSED | Single `BeginTransactionAsync` (`:88`); catch → `RollbackAsync` (`:330`) → 0 committed. WR-01 fix: `graded` bool ditangkap + `throw` bila false (`:224-226`) → masuk catch → rollback. |
| T-393-09 | Repudiation (audit lost on rollback) | mitigate | CLOSED | Audit sukses+skip via `_context.AuditLogs.Add` IN-TX (`:297`, `:310`) sebelum `SaveChangesAsync` (`:321`) + `CommitAsync` (`:322`) — ikut rollback. Satu-satunya `LogAsync` (`:289`) ADA DI KOMENTAR menjelaskan kenapa LogAsync TIDAK dipakai (anti commit-parsial). |
| T-393-10 | Tampering (fake data for worker) | mitigate | CLOSED | `IsManualEntry = true` (`:109`) + AuditLog `ManualInject` dengan actor+NIP+skor+sessionId (`:297-306`) — transparansi INJ-02. |
| T-393-11 | Information Disclosure (test DB leak/Dev) | mitigate | CLOSED | Fixture disposable `HcPortalDB_Test_{guid}` (`InjectAssessmentServiceTests.cs:26`) + `EnsureDeletedAsync` (`:55`); semua query test scoped ke `sessionId`/`Title` (mis. `:318`, `:351`, `:457`) — caveat shared-DB dihormati, DB Dev untouched. |
| T-393-12 | Integrity verification (rollback benar 0-write) | mitigate | CLOSED | Assert eksplisit read-after-commit `CountAsync(...batch...) == 0` untuk tiap reject/rollback: SC2a `:318`, SC2b `:351`, SC5 collision `:527`, range `:546`, future `:565`. |
| T-393-13 | Audit verification (count tak terkontaminasi) | mitigate | CLOSED | SC4 assert `Count("ManualInject" scoped session) == 3` (`:434`) AND skip pakai ActionType terpisah `ManualInjectSkipped` (`:454`); count ManualInject untuk NIP-skip tetap 1 (`:457-458`). |

---

## Unregistered Flags

None. SUMMARY 393-01, 393-02, dan 393-03 tidak memuat section `## Threat Flags` — tidak ada attack surface baru yang dideteksi executor selama implementasi. Tidak ada flag yang perlu dipetakan/di-log.

---

## Accepted Risks Log

| Threat ID | Risk | Acceptance Rationale | Mitigation Status |
|-----------|------|----------------------|-------------------|
| T-393-02 | DTO field liar (score/cert/date) di-terima service tanpa validasi pada SURFACE Plan 01. | Plan 01 SENGAJA hanya menetapkan kontrak (POCO DTO + skeleton). Tidak ada operasi write di Plan 01 → permukaan serangan nihil. Validasi nilai dialihkan ke Plan 02 pre-flight (D-03/D-06/D-07/D-09). | TERTUTUP di Plan 02: `PreflightValidateAsync` (`InjectAssessmentService.cs:341-434`) menolak-semua bila ada nilai invalid SEBELUM transaction. DTO POCO tidak ter-attach EF → tidak ada mass-assignment. Risiko residual: nol. |

---

## ASVS Level 2 Notes

- **V4 Access Control (partial):** RBAC actor di-defer ke Phase 394 controller — by design (service tanpa HttpContext, terima actor identity sebagai parameter eksplisit). Tidak diaudit di phase ini; akan jadi scope audit Phase 394.
- **V7 Error Handling & Logging:** Audit in-tx 3 ActionType memenuhi non-repudiation; rollback membuang audit parsial (konsisten transaksional).
- **V5 Validation:** Pre-flight reject-all (server-side) memvalidasi NIP, opsi, range skor essay, tanggal, dan keunikan nomor sertifikat sebelum write apa pun.

---

## Verdict

**SECURED.** Semua 13 threat dari register PLAN tertutup di kode terimplementasi (12 mitigate ter-verifikasi + 1 accept ter-justifikasi & ditindaklanjuti Plan 02). 0 threat terbuka, 0 unregistered flag. Tidak ada gap implementasi untuk di-eskalasi.

Catatan untuk milestone: boundary RBAC (V4) aktif di Phase 394 controller — pastikan audit phase tersebut memverifikasi enforcement Admin/HC pada caller `InjectBatchAsync`.

---
*Audited: 2026-06-17 — gsd-security-auditor*
*Phase: 393-backend-core-inject (v32.2)*
