---
phase: 363-audit-fix-alur-proton-temuan-verifikasi-t1-t10
reviewed: 2026-06-11T10:59:28Z
depth: standard
files_reviewed: 12
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Controllers/CDPController.cs
  - Controllers/CoachMappingController.cs
  - HcPortal.Tests/FakeNotificationService.cs
  - HcPortal.Tests/ProtonApproveRejectParityTests.cs
  - HcPortal.Tests/ProtonBypassServiceTests.cs
  - HcPortal.Tests/ProtonCompletionMissTests.cs
  - HcPortal.Tests/ProtonCompletionServiceTests.cs
  - HcPortal.Tests/ProtonHistoriAndEvidenceTests.cs
  - HcPortal.Tests/ProtonYearGateIntegrationTests.cs
  - Services/GradingService.cs
  - Services/ProtonCompletionService.cs
findings:
  critical: 0
  warning: 2
  info: 6
  total: 8
status: issues_found
---

# Phase 363: Code Review Report

**Reviewed:** 2026-06-11T10:59:28Z
**Depth:** standard
**Files Reviewed:** 12
**Status:** issues_found

## Summary

Review difokuskan pada perubahan fase 363 (range commit `bb4529ff..aa09e36a`, grep `363-0`): ekstraksi core approve/reject di CDPController + rewire 4 endpoint, surface `PROTON_PENANDA_MISS` di ProtonCompletionService, drop hardcode ValidUntil di GradingService, gate reaktivasi cross-year di CoachMappingController, T9 log-warn + T10 komentar by-design di AssessmentAdminController, serta 7 file test.

Kualitas perubahan secara umum **baik**: core extraction mempertahankan semantik gold-standard (terverifikasi pin tests yang memanggil core asli via fixture SQL riil), HC-reset resubmit konsisten di kedua blok, scoping `BuildBelumMulaiRowsAsync` konsisten dengan cabang `scopedCoacheeIds` per level (<=3 / 4 / 5), nama file evidence di-uniquify timestamp+GUID di kedua jalur sehingga `AppendEvidencePathHistory` tidak pernah menunjuk file yang ter-overwrite, dan DI `ProtonCompletionService` (ctor baru +INotificationService+AuditLogService) sudah ter-register di Program.cs:51/57/67.

Tidak ada temuan Critical dan tidak ada isu keamanan baru. Dua Warning: (1) core approve memutasi entity tracked sebelum race-guard early-return — dirty-state laten di seam yang dirancang reusable; (2) blok surface miss (audit + bell) tidak dibungkus try/catch padahal `AuditLogService.LogAsync` bisa throw di jalur grading pasca point-of-no-return. Enam Info adalah catatan kualitas/jejak desain, termasuk satu edge case yang sudah explicitly accepted di plan (T-363-06e).

## Warnings

### WR-01: ApproveDeliverableCoreAsync memutasi entity tracked SEBELUM race-guard early-return

**File:** `Controllers/CDPController.cs:998-1030`
**Issue:** Field approval per-role (`SrSpvApprovalStatus/ApprovedById/ApprovedAt` atau pasangan SH-nya) di-set pada entity tracked di :998-1010, BARU kemudian race-guard D-10 dicek (:1013-1030). Bila `stillCanApprove == false`, core return `(false, ...)` tanpa `SaveChangesAsync` — entity `progress` tetap **dirty** di scoped DbContext yang dishare satu request. Call site saat ini (ApproveDeliverable :868, ApproveFromProgress :2040) langsung return setelah `!ok` sehingga mutasi tak pernah ter-flush, tapi core ini secara eksplisit dirancang sebagai seam reusable ("state mutation lives in the shared core") — caller masa depan yang memanggil `SaveChangesAsync` apa pun setelah core gagal (mis. menulis audit row) akan diam-diam mem-persist approval parsial dari aktor yang seharusnya ditolak race-guard.
**Fix:** Pindahkan blok mutasi per-role ke SETELAH cek `stillCanApprove` — semantik identik karena guard membaca nilai fresh `AsNoTracking` dari DB (:1013-1017), bukan state in-memory:
```csharp
// Race condition guard — reload fresh status SEBELUM mutasi apa pun (D-10)
var freshStatus = await context.ProtonDeliverableProgresses ... ;
if (freshStatus == null) return (false, "Deliverable tidak ditemukan.", false);
bool stillCanApprove = ...;
if (!stillCanApprove) return (false, "Deliverable sudah diproses oleh approver lain.", false);

// Set per-role approval fields (baru aman dimutasi di sini)
if (isSrSpv) { ... } else if (isSH) { ... }
```

### WR-02: Blok surface PROTON_PENANDA_MISS tidak diisolasi try/catch — bisa mematahkan grading yang sudah sukses

**File:** `Services/ProtonCompletionService.cs:53-75` (call site: `Services/GradingService.cs:308-315, 542-549`)
**Issue:** Branch miss di `EnsureAsync` kini melakukan side-effect DB: `_auditLog.LogAsync` (yang memanggil `SaveChangesAsync` internal **tanpa** try/catch — AuditLogService.cs:40-41) plus loop dedup `AnyAsync` + `SendAsync`. `NotificationService.SendAsync` menelan exception-nya sendiri (NotificationService.cs:129-133), tapi `LogAsync` dan query dedup tidak. Konsekuensi di dua call site grading:
- `GradeAndCompleteAsync`: hook dipanggil SETELAH session sudah di-mark Completed via ExecuteUpdate (:238-246) — kegagalan insert audit melempar exception ke controller (AkhiriUjian/SubmitExam) dan user melihat error 500 padahal ujian sebenarnya sudah ter-grade.
- `RegradeAfterEditAsync`: kontraknya "CALLER bertanggung jawab open transaction... commit setelahnya" — exception dari blok surface me-rollback SELURUH regrade yang sah hanya karena pelaporan miss gagal.

Sebelum 363-03, branch ini hanya `_logger.LogWarning` (tidak bisa gagal); failure mode ini baru.
**Fix:** Bungkus seluruh blok surface (audit + loop bell) dalam try/catch warn-only, konsisten konvensi notif-dispatch controller:
```csharp
try
{
    await _auditLog.LogAsync("system", "system/grading", "PROTON_PENANDA_MISS", ...);
    var hcIds = await _context.Users...;
    foreach (var hcId in hcIds) { ... }
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Surface PROTON_PENANDA_MISS gagal (Coachee={CoacheeId}, Track={TrackId}) — grading tidak boleh ikut gagal.", coacheeId, protonTrackId);
}
return false;
```

## Info

### IN-01: Diskriminasi error via magic string di endpoint approve/reject

**File:** `Controllers/CDPController.cs:870, 932`
**Issue:** `if (error == "Deliverable tidak ditemukan.") return NotFound();` membandingkan literal pesan yang diproduksi core (:992, :1019, :1072). Bila wording pesan diubah di core, mapping NotFound diam-diam terdegradasi jadi TempData+redirect (graceful, tapi drift tak terdeteksi compiler).
**Fix:** Kembalikan kode error/flag dari tuple core, mis. `(bool ok, bool notFound, string? error, bool allApproved)` atau enum `CoreError.NotFound`, lalu branch pada flag — bukan pada string.

### IN-02: Komentar AF-7 di CoachMappingController sudah basi pasca 363-05

**File:** `Controllers/CoachMappingController.cs:515-516`
**Issue:** Komentar masih klaim "batch pre-load (3 query) menggantikan N+1 loop... Output incompleteCoachees IDENTIK — zero behavior change", padahal 363-05 sengaja MENGUBAH perilaku cabang 1 (filter `IsActive`) dan menambahkan 2 `AnyAsync` per-coachee di dalam loop (:540-550). Klaim "identik/zero behavior change" kini menyesatkan maintainer berikutnya.
**Fix:** Perbarui paragraf AF-7 — catat bahwa sejak 363-05 cabang 1 memfilter IsActive (perubahan perilaku disengaja, T3) dan exempt bypass dievaluasi per-coachee.

### IN-03: Status-guard reject terduplikasi di 2 endpoint, core tanpa guard

**File:** `Controllers/CDPController.cs:910, 2084` (core: :1065-1072)
**Issue:** Guard "hanya Submitted/Approved yang bisa di-reject" hidup di RejectDeliverable (:910) dan RejectFromProgress (:2084), sementara `RejectDeliverableCoreAsync` me-reject status apa pun tanpa cek. Seam anti-drift 363-01 menutup drift state-mutation, tapi guard domainnya sendiri masih bisa drift — caller ketiga di masa depan yang lupa guard bisa me-reject row Pending/Rejected.
**Fix:** Tarik status-guard ke dalam core (return `(false, "Deliverable ini tidak dapat ditolak saat ini.")` bila status bukan Submitted/Approved); endpoint cukup memetakan error ke respons masing-masing. Paritas dengan race-guard approve yang sudah di dalam core.

### IN-04: Dedup bell PROTON_PENANDA_MISS membaca tabel langsung, melewati abstraksi INotificationService

**File:** `Services/ProtonCompletionService.cs:65-67`
**Issue:** Dedup `AnyAsync(UserNotifications...)` mengasumsikan implementasi `INotificationService` menulis ke tabel yang sama — coupling concrete-implementation di balik interface. Efek samping: dengan `FakeNotificationService` di test (tidak persist), jalur dedup tidak pernah ter-exercise (ProtonCompletionMissTests hanya menguji jalur kirim), sehingga regresi dedup tak akan tertangkap suite.
**Fix:** Tambah satu [Fact] dedup memakai `NotificationService` riil terhadap fixture SQL (kirim 2x miss identik → 1 row), atau pindahkan dedup-by-message ke dalam NotificationService/helper bersama (pola CreateHCNotificationAsync D-14).

### IN-05: Edge ValidUntil Pass→Fail→Pass — accepted per D-10, dicatat sebagai jejak

**File:** `Services/GradingService.cs:479-483, 516-520`
**Issue:** Revoke Pass→Fail men-null `ValidUntil` (:483, pre-existing), dan pasca 363-04 regrade Fail→Pass tidak lagi mengisinya — round-trip Pass→Fail→Pass menghasilkan sertifikat ber-`NomorSertifikat` dengan `ValidUntil` NULL permanen (nilai setup HC ikut hilang saat revoke). Di RenewalController, null diperlakukan `DateOnly.MaxValue` → sertifikat tak pernah masuk radar renewal/expiring. **Status: explicitly accepted** — T-363-06e (363-04-PLAN.md:114, keputusan D-10 locked) + terverifikasi UAT Plan 07 (cert `KPB/005/VI/2026` ValidUntil NULL). Bukan temuan baru; dicatat agar konsumen review hilir tahu ini by-decision, bukan miss.
**Fix:** Tidak perlu aksi sekarang. Bila jadi isu riil, eskalasi fase terpisah sesuai catatan plan (opsi: revoke berhenti men-null ValidUntil karena `NomorSertifikat == null` sudah cukup menekan sertifikat).

### IN-06: Pola predicate-replication di test — menguji salinan logika, bukan kode controller

**File:** `HcPortal.Tests/ProtonYearGateIntegrationTests.cs:172-195`, `HcPortal.Tests/ProtonApproveRejectParityTests.cs:172-185`, `HcPortal.Tests/ProtonHistoriAndEvidenceTests.cs:77-83`
**Issue:** `ReactivationBlockedAsync`, `ApplyResubmitReset`, dan komputasi set "Belum Mulai" mereplikasi logika controller secara manual — bila gate/blok reset di controller berubah tanpa sinkronisasi test, suite tetap hijau (silent drift). Kelemahan ini SUDAH diakui jujur di komentar test (":165-169 jaga sinkron manual"). Mitigasi parsial sudah ada: core approve/reject dan AppendEvidencePathHistory dipanggil langsung (kode asli).
**Fix:** Bila gate reaktivasi disentuh lagi, pertimbangkan ekstraksi predikatnya jadi static method (pola `ProtonYearGate.IsAllowed`) supaya test memanggil kode asli — konsisten dengan arah 363-01.

---

## Catatan Verifikasi (tidak ada temuan)

- **Paritas core approve/reject:** identity-map EF menjamin `progress` di endpoint dan instance yang dimutasi core adalah objek sama (scoped context); `DispatchApproveNotificationsAsync(progress, allApproved)` aman di kedua jalur.
- **`allApproved` empty-list edge (trackId=0 → `.All()` true):** pre-existing dari kode inline lama, dipindah verbatim — bukan regresi fase ini.
- **Admin di approve/reject:** `HasSectionAccess(level) => level == 4` (Models/UserRoles.cs:69) — Admin (level 1) tidak pernah lolos guard, jadi `isSrSpv || isSH` selalu true di semua call site core; cek "Admin can reject cross-section" (:921) adalah dead-path legacy pre-existing.
- **Scoping `BuildBelumMulaiRowsAsync`:** konsisten dengan `scopedCoacheeIds` per cabang level di HistoriProton (:3178-3198) dan Export; `coacheeIdsWithAssignments` mencakup assignment aktif+inactive sehingga coachee ber-histori tidak salah dilabel "Belum Mulai".
- **Evidence file overwrite:** kedua jalur memakai nama file timestamp+GUID (CDPController:2271-2273, FileUploadHelper.cs:87-100) — entry `EvidencePathHistory` selalu menunjuk file fisik distinct (E10 keep-evidence terjaga).
- **AuditLog actor "system":** `ActorUserId` string [Required] tanpa FK ke Users (Models/AuditLog.cs:12-13) — aman; `ActionType` "PROTON_PENANDA_MISS" (19 char) di bawah MaxLength(50).
- **DI:** ctor baru `ProtonCompletionService` ter-resolve — `AuditLogService`/`ProtonCompletionService`/`INotificationService` semua scoped di Program.cs:51/57/67; 3 ctor site test sudah di-update (commit `bac1305d`).
- **T9/T10:** log-warn Urutan tidak kontigu dan komentar by-design Backfill sesuai keputusan D-12/D-13 (log-only, gate tetap dilewati untuk track non-kontigu — disengaja).

---

_Reviewed: 2026-06-11T10:59:28Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
