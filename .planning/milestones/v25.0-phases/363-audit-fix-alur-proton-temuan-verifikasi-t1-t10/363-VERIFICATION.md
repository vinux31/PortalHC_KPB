---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
verified: 2026-06-11T08:45:00Z
status: passed
score: 10/10
overrides_applied: 1
overrides:
  - must_have: "Cores internal static (testable via InternalsVisibleTo)"
    reason: "Proyek tidak punya [assembly: InternalsVisibleTo(\"HcPortal.Tests\")] — public static dipakai konsisten dengan konvensi proyek (CMPController:3969, Phase 351-02). Visibility lebih lebar dari plan tapi zero behavior change."
    accepted_by: "gsd-verifier (auto-fix Rule 3 documented di SUMMARY 363-01 dan 363-06)"
    accepted_at: "2026-06-11T08:00:00Z"
---

# Phase 363: Audit Fix Alur PROTON T1-T10 — Verification Report

**Phase Goal:** Audit Fix Alur PROTON — perbaiki 10 temuan verifikasi adversarial T1-T10 (3 HIGH: T1 notif allApproved miss di ApproveFromProgress, T2 reject chain divergen/HCApprovalStatus survive, T3 loophole gate reaktivasi; MED: T4 silent penanda miss, T6 asimetri ValidUntil, T7 race-guard modal, T8 evidence history; LOW: T5 Belum Mulai, T9 log defensive; T10 by-design doc).
**Verified:** 2026-06-11T08:45:00Z
**Status:** PASSED
**Re-verification:** Tidak — verifikasi awal.

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | T1: ApproveFromProgress kini kirim COACH_ALL_COMPLETE ke HC saat approve deliverable terakhir | VERIFIED | `ApproveDeliverableCoreAsync` dipanggil dari ApproveFromProgress (:2038); `DispatchApproveNotificationsAsync` dipanggil dari kedua endpoint (:875, :2043); UAT Plan 07: COACH_ALL_COMPLETE tercatat di UserNotifications |
| 2 | T2: RejectFromProgress kini reset full chain termasuk HCApprovalStatus | VERIFIED | `RejectDeliverableCoreAsync` dipanggil dari RejectFromProgress (:2098); HCApprovalStatus="Pending" ada di 3 lokasi (:1089, :1362, :2303); HCReviewedById=null 3 lokasi; anomali SrSpvApprovedById=user.Id hilang (grep 0 match) |
| 3 | T3: Reaktivasi assignment inactive cross-year kini kena year-gate | VERIFIED | `activeForRequestedTrack` (filter IsActive) gantikan `hasForRequestedTrack`; `reactExempt` (inactive+Bypass exempt) ada; `IsPrevYearPassedAsync` masih dipanggil; UAT Plan 07: iwan3 asg INACTIVE Origin=null blocked, widyadhana INACTIVE Origin=Bypass lanjut |
| 4 | T4: Lulus exam saat assignment nonaktif → AuditLog PROTON_PENANDA_MISS + HC bell (tidak silent) | VERIFIED | `PROTON_PENANDA_MISS` ada di ProtonCompletionService.cs (:53 audit, :71 notif); RoleLevel==2 HC filter (:58); idempotent path tidak diubah; test ProtonCompletionMissTests 2/2 PASS |
| 5 | T5: Badge "Belum Mulai" reachable di HistoriProton dan ExportHistoriProton | VERIFIED | `BuildBelumMulaiRowsAsync` dipanggil dari kedua method (:3302, :3455); definisi helper :3504; "Belum Mulai" string ada (:3550); view tidak diubah; UAT Plan 07: arsyad tampil badge Belum Mulai + filter + export 200 |
| 6 | T6: Regrade Fail→Pass tidak lagi hardcode ValidUntil +3 tahun | VERIFIED | `AddYears(3)` = 0 match di GradingService.cs; `SetProperty(r => r.ValidUntil, validUntil)` = 0 match; NomorSertifikat-only SetProperty tetap (:520); revoke null tetap (:483); komentar T6/D-10 (:516); UAT Plan 07: ValidUntil=NULL setelah regrade |
| 7 | T7: Race-guard sekarang aktif di jalur modal (ApproveFromProgress) | VERIFIED | Terabsorb ke dalam `ApproveDeliverableCoreAsync` (reload-fresh + stillCanApprove, Plan 01 Pattern B); dipanggil dari ApproveFromProgress — race-guard otomatis berlaku. Test ProtonApproveRejectParityTests `ApproveCore_RaceGuard_RejectsStaleSecondApprove` PASS |
| 8 | T8: SubmitEvidenceWithCoaching kini append EvidencePath lama ke EvidencePathHistory sebelum overwrite | VERIFIED | `AppendEvidencePathHistory` public static :3677; dipanggil UploadEvidence (:1342) + SubmitEvidenceWithCoaching (:2320, sebelum overwrite :2322); tidak ada File.Delete baru; test ProtonHistoriAndEvidenceTests 3/3 PASS |
| 9 | T9: Prev-track resolution null saat Urutan>1 kini emit log warning di dua titik | VERIFIED | "Urutan tidak kontigu" ada di CoachMappingController.cs (:512) dan AssessmentAdminController.cs (:1356); log-only tanpa throw/block |
| 10 | T10: BackfillProtonPenanda tanpa year-gate didokumentasikan by-design (zero logic change) | VERIFIED | Komentar T10/D-13 ada di AssessmentAdminController.cs (:3971); 363-FINDINGS.md T10 ada catatan "RESOLVED by-design (D-13)"; nol perubahan logic |

**Score:** 10/10 truths verified (1 override applied untuk visibility cores public vs internal)

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Controllers/CDPController.cs` | Static cores + rewire FromProgress modal + Belum Mulai + AppendEvidencePathHistory | VERIFIED | Semua 4 static helpers ada (ApproveDeliverableCoreAsync, RejectDeliverableCoreAsync, AddDeliverableStatusHistory, AppendEvidencePathHistory); BuildBelumMulaiRowsAsync instance; 2 endpoint rewired per core |
| `Services/ProtonCompletionService.cs` | Ctor +2 dep; surface miss di no-assignment branch | VERIFIED | PROTON_PENANDA_MISS di audit + notif; RoleLevel==2 HC filter; strict IsActive dipertahankan |
| `Services/GradingService.cs` | RegradeAfterEditAsync Fail→Pass: NomorSertifikat only, komentar T6/D-10 | VERIFIED | AddYears(3) dihapus; SetProperty ValidUntil,validUntil dihapus; NomorSertifikat tetap; komentar ada |
| `Controllers/CoachMappingController.cs` | activeForRequestedTrack + reactExempt + T9 log-warn | VERIFIED | activeForRequestedTrack ada (IsActive filter); hasForRequestedTrack hilang; reactExempt ada; T9 log-warn ada |
| `Controllers/AssessmentAdminController.cs` | T9 log-warn CreateAssessment + T10 by-design comment | VERIFIED | Dua grep: "Urutan tidak kontigu" di AssessmentAdminController + "T10/D-13" ada |
| `HcPortal.Tests/ProtonApproveRejectParityTests.cs` | 6 [Fact] pin tests (4 plan01 + 2 plan02) | VERIFIED | File ada; 6 Fact terverifikasi (RejectCore_ResetsFullChain_IncludingHC, ApproveCore_LastDeliverable, ApproveCore_NotLast, ApproveCore_RaceGuard, Reject_ThenResubmit, FromProgress_And_Deliverable_RejectCore); Trait Integration |
| `HcPortal.Tests/ProtonCompletionMissTests.cs` | 2 [Fact] surface-on-miss + idempotent | VERIFIED | File ada; 2 Fact: EnsureAsync_NoActiveAssignment_SurfacesAuditAndHCNotif + EnsureAsync_PenandaAlreadyExists_DoesNotSurface |
| `HcPortal.Tests/ProtonYearGateIntegrationTests.cs` | +3 Fact reactivation gate (blocked/bypass-exempt/active-skip) | VERIFIED | 3 Fact baru ada: Reactivation_InactiveNonBypass_NoPrevYear_Blocked, Reactivation_InactiveBypass_Exempt, Reactivation_ActiveAssignment_SkipsGate |
| `HcPortal.Tests/ProtonHistoriAndEvidenceTests.cs` | 3 [Fact] append-history + belum-mulai set | VERIFIED | File ada; 3 Fact: AppendEvidencePathHistory_AppendsOldPath, AppendEvidencePathHistory_NoOp_WhenEmptyPath, BelumMulai_SetComputation |
| `docs/SEED_JOURNAL.md` | Entry 363 dengan snapshot + status cleaned | VERIFIED | Entry 363 ada di baris 172; snapshot C:\Temp\HcPortalDB_Dev_pre363uat_20260611.bak; status cleaned |
| `HcPortal.Tests/FakeNotificationService.cs` | File shared (lifted dari BypassTests) | VERIFIED | File ada di daftar Glob HcPortal.Tests |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| CDPController.ApproveDeliverable (:865) | ApproveDeliverableCoreAsync | direct call | WIRED | Grep konfirmasi call site + definisi |
| CDPController.RejectDeliverable (:928) | RejectDeliverableCoreAsync | direct call | WIRED | Grep konfirmasi call site + definisi |
| CDPController.ApproveFromProgress (:2038) | ApproveDeliverableCoreAsync + DispatchApproveNotificationsAsync | direct call (T1/T7) | WIRED | 2 call sites ApproveCore; Dispatch 2 call sites |
| CDPController.RejectFromProgress (:2098) | RejectDeliverableCoreAsync | direct call (T2) | WIRED | 2 call sites RejectCore; JSON return newStatus="Rejected" (:2111) |
| UploadEvidence wasRejected (:1362) | HCApprovalStatus="Pending" reset (D-03 belt-braces) | inline reset | WIRED | Grep: 3 lokasi HC reset terkonfirmasi |
| SubmitEvidenceWithCoaching resubmit (:2303) | HCApprovalStatus="Pending" reset (D-03) | inline reset | WIRED | Grep terkonfirmasi |
| SubmitEvidenceWithCoaching (:2320) | AppendEvidencePathHistory (T8) | static call SEBELUM overwrite | WIRED | :2320 sebelum :2322 (per review Summary 363-06) |
| ProtonCompletionService.EnsureAsync no-assignment branch | _auditLog.LogAsync + _notificationService.SendAsync | surface inside branch (T4) | WIRED | PROTON_PENANDA_MISS di :53 (audit) dan :71 (notif) |
| CoachMappingController cabang-1 (:533) | activeForRequestedTrack (IsActive filter) + reactExempt | refined gate predicate (T3) | WIRED | activeForRequestedTrack ada; reactExempt ada; IsPrevYearPassedAsync masih dipanggil :556 |
| HistoriProton + ExportHistoriProton | BuildBelumMulaiRowsAsync | shared helper call (T5) | WIRED | 2 call sites :3302 + :3455; definisi :3504 |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| ApproveDeliverableCoreAsync | allApproved | orderedProgresses.All(p => p.Status == "Approved") — EF query dari DB | Ya — computed dari real DB rows | FLOWING |
| RejectDeliverableCoreAsync | chain reset | EF tracked progress object dari DB | Ya — DB mutations langsung | FLOWING |
| BuildBelumMulaiRowsAsync | belumMulaiIds | activeMappingCoacheeIds.Except(coacheeIdsWithAssignments) dari DB query | Ya — real mapping + assignment DB | FLOWING |
| ProtonCompletionService no-assignment | PROTON_PENANDA_MISS | hcIds dari DB (RoleLevel==2), audit dari _auditLog | Ya — real HC users dari DB | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Command/Method | Result | Status |
|----------|----------------|--------|--------|
| ApproveDeliverableCoreAsync ada di CDPController | grep `ApproveDeliverableCoreAsync` CDPController.cs | Definisi :981 + 2 call sites :865, :2038 | PASS |
| RejectDeliverableCoreAsync ada + dipanggil dari 2 endpoint | grep `RejectDeliverableCoreAsync` CDPController.cs | Definisi :1065 + 2 call sites :928, :2098 | PASS |
| HCApprovalStatus="Pending" ada di reject core + 2 resubmit | grep `HCApprovalStatus = "Pending"` CDPController.cs | 3 lokasi :1089, :1362, :2303 | PASS |
| ValidUntil hardcode DIHAPUS dari GradingService | grep `AddYears(3)` GradingService.cs | 0 match | PASS |
| reactExempt gate reaktivasi ada | grep `reactExempt` CoachMappingController.cs | :538 (komentar), :547 (query), :550 (if) | PASS |
| T9 log-warn di 2 titik | grep `Urutan tidak kontigu` Controllers/ | 2 match (CoachMapping :512, AssessmentAdmin :1356) | PASS |
| T10 by-design terdokumentasi | grep `T10/D-13` AssessmentAdminController.cs | :3971 ada | PASS |
| Full test suite green 228/228 | dotnet test (Plan 07 Task 1) | 228/228 PASS (214 regresi + 14 baru) | PASS |
| UAT live T1/T2/T3/T5/T6 | Plan 07 Task 2 Playwright + human sign-off | "approved" | PASS |
| Seed journal 363 cleaned | SEED_JOURNAL.md entry | status=cleaned, snapshot dikonfirmasi | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| T1 (notif HC allApproved miss) | 363-01, 363-02, 363-07 | ApproveFromProgress kirim COACH_ALL_COMPLETE saat approve terakhir | SATISFIED | ApproveDeliverableCoreAsync + DispatchApproveNotificationsAsync dipakai di kedua endpoint; UAT T1 PASS |
| T2 (reject chain divergen) | 363-01, 363-02, 363-07 | RejectFromProgress reset full chain termasuk HCApprovalStatus | SATISFIED | RejectDeliverableCoreAsync di-call dari RejectFromProgress; HC reset di 3 lokasi; UAT T2 PASS |
| T3 (loophole gate reaktivasi) | 363-05, 363-07 | Reaktivasi inactive cross-year wajib lewat year-gate | SATISFIED | activeForRequestedTrack + reactExempt + IsPrevYearPassedAsync; UAT T3 PASS |
| T4 (penanda silent miss) | 363-03 | Penanda miss → AuditLog + HC bell notification | SATISFIED | PROTON_PENANDA_MISS di ProtonCompletionService; test 2/2 PASS |
| T5 (Belum Mulai unreachable) | 363-06, 363-07 | Status Belum Mulai reachable di list + export | SATISFIED | BuildBelumMulaiRowsAsync dipanggil dari kedua method; UAT T5 PASS |
| T6 (asimetri ValidUntil) | 363-04, 363-07 | Regrade Fail→Pass tidak hardcode ValidUntil +3thn | SATISFIED | AddYears(3) hilang; NomorSertifikat-only; komentar T6/D-10; UAT T6 PASS |
| T7 (race-guard modal) | 363-01, 363-02 | Concurrent approvers di modal diblok race-guard | SATISFIED | Race-guard terabsorb ke ApproveDeliverableCoreAsync (Pattern B reload-fresh); parity test RaceGuard PASS |
| T8 (evidence history drift) | 363-06 | SubmitEvidenceWithCoaching append history sebelum overwrite | SATISFIED | AppendEvidencePathHistory dipanggil :2320 sebelum overwrite :2322; test 3/3 PASS |
| T9 (log defensive prev-track) | 363-05 | Log warning saat Urutan non-kontigu di 2 titik | SATISFIED | 2 grep match CoachMapping + AssessmentAdmin |
| T10 (by-design BackfillPenanda) | 363-05 | BackfillProtonPenanda tanpa year-gate didokumentasikan | SATISFIED | Komentar T10/D-13 ada; 363-FINDINGS.md T10 RESOLVED |

**Catatan REQUIREMENTS.md:** T1-T10 adalah requirement internal fase 363 (audit findings) — tidak dipetakan secara eksplisit ke REQUIREMENTS.md yang berisi PCOMP/PBYP/URG untuk milestone v25.0-v26.0. Temuan T1-T10 adalah bug baru yang ditemukan post-ship fase 358-361, berada di luar traceability table REQUIREMENTS.md. Tidak ada orphaned requirements.

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| (none) | — | — | — |

Tidak ada TODO/FIXME/placeholder/hardcoded empty data yang ditemukan pada file-file yang dimodifikasi fase ini. Komentar "JANGAN ubah cabang 1" (lama) sudah diganti dengan dokumentasi desain baru (terkonfirmasi grep 0 match).

---

### Human Verification Required

Tidak ada. Seluruh human verification sudah selesai di Plan 07 Task 2 dengan sign-off "approved" untuk T1/T2/T3/T5/T6. T4/T8 diverifikasi via integration test. T9/T10 diverifikasi via code review (log-warn + komentar by-design).

---

## Gaps Summary

Tidak ada gap. Semua 10 temuan T1-T10 telah ditutup:

- **3 HIGH** (T1, T2, T3): Fixed struktural via shared cores (ApproveDeliverableCoreAsync / RejectDeliverableCoreAsync) dan reactivation gate — drift tidak bisa berulang karena satu jalur kode.
- **4 MED** (T4, T6, T7, T8): Fixed via ctor injection surface-miss, surgical ValidUntil deletion, race-guard absorbed ke core, AppendEvidencePathHistory shared helper.
- **3 LOW** (T5, T9, T10): Fixed via BuildBelumMulaiRowsAsync query union, log-warn defensive, by-design comment.

Override satu item (visibility `public static` vs `internal static`) adalah deviasi yang terdokumentasi dan konsisten dengan konvensi proyek.

---

_Verified: 2026-06-11T08:45:00Z_
_Verifier: Claude (gsd-verifier)_
