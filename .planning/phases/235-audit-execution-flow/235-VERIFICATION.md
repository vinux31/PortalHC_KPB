---
phase: 235-audit-execution-flow
verified: 2026-03-22T16:00:00Z
status: human_needed
score: 9/9 must-haves verified
human_verification:
  - test: "Re-submit setelah reject — pastikan StatusHistory bertipe 'Re-submitted' tercatat di DB"
    expected: "DeliverableStatusHistory entry dengan StatusType='Re-submitted' muncul setelah Coach upload ulang evidence untuk deliverable yang sebelumnya di-reject"
    why_human: "Memerlukan multi-step flow: approval chain selesai → rejection → resubmit. Tidak tersedia via code grep."
  - test: "Race condition guard ApproveDeliverable — concurrent approve dari dua role"
    expected: "Approver kedua mendapat TempData error 'Deliverable sudah diproses oleh approver lain.' dan redirect, bukan error 500"
    why_human: "Memerlukan simulasi concurrent HTTP request — tidak bisa diverifikasi via single browser atau code analysis saja."
  - test: "HC Review Notifikasi ke Coach — cek tabel Notifications di DB"
    expected: "Setelah HC review deliverable, Coach menerima notifikasi bertipe 'HC_REVIEW_COMPLETE' di tabel UserNotifications"
    why_human: "Memerlukan full approval chain (SrSpv + SH approve terlebih dahulu) sebelum HC review bisa dipanggil."
  - test: "Resubmit Notifikasi ke Reviewer — cek tabel Notifications di DB"
    expected: "Setelah Coach resubmit, section reviewer (RoleLevel=4) menerima notifikasi bertipe 'COACH_EVIDENCE_RESUBMITTED'"
    why_human: "Memerlukan reject + resubmit flow — data test yang sesuai belum tersedia saat UAT."
  - test: "Upload Evidence Rollback — file save failure"
    expected: "Progress status TIDAK berubah jika file gagal disimpan. User melihat pesan error TempData dan redirect ke halaman Deliverable."
    why_human: "Memerlukan simulasi kondisi failure (disk full atau folder tidak writable) — tidak bisa disimulasi via browser normal."
---

# Phase 235: Audit Execution Flow — Verification Report

**Phase Goal:** Audit execution flow — evidence submission, approval chain, notifications, PlanIdp access
**Verified:** 2026-03-22T16:00:00Z
**Status:** human_needed
**Re-verification:** Tidak — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | Coach upload evidence menghasilkan DeliverableStatusHistory entry 'Submitted' atau 'Re-submitted' | VERIFIED | `CDPController.cs:1252-1253` — `uploadStatusType = wasRejected ? "Re-submitted" : "Submitted"` + `RecordStatusHistory(progress.Id, uploadStatusType, ...)` |
| 2 | Seed ProtonDeliverableProgress menghasilkan DeliverableStatusHistory entry 'Pending' awal | VERIFIED | `AdminController.cs:6847` + `ProtonDataController.cs:515` — keduanya memiliki `StatusType = "Pending"` dalam loop foreach setelah flush SaveChangesAsync |
| 3 | Resubmit setelah reject mempertahankan path file lama di EvidencePathHistory | VERIFIED | `CDPController.cs:1223-1229` — block JSON serialize path lama ke `EvidencePathHistory` sebelum overwrite `EvidencePath` |
| 4 | Upload gagal tidak mengubah status deliverable — rollback bersih | VERIFIED | `CDPController.cs:1212-1220` — try-catch di sekitar `FileUploadHelper.SaveFileAsync`, on catch: TempData["Error"] + redirect tanpa mutasi progress |
| 5 | Dua approver approve bersamaan — hanya satu sukses, kedua mendapat error 'sudah diproses' | VERIFIED | `CDPController.cs:830-848` — reload `AsNoTracking()` fresh status + `stillCanApprove` check + TempData["Error"] = "Deliverable sudah diproses oleh approver lain." |
| 6 | HC Review menghasilkan notifikasi ke Coach | VERIFIED | `CDPController.cs:1122-1145` — try-catch block dengan `_notificationService.SendAsync(coachMappingForHC.CoachId, "HC_REVIEW_COMPLETE", ...)` |
| 7 | Resubmit setelah reject menghasilkan notifikasi khusus 'Re-submitted' ke reviewers | VERIFIED | `CDPController.cs:1266,1286` — conditional block `if (wasRejected)` dengan type `"COACH_EVIDENCE_RESUBMITTED"` |
| 8 | Coach hanya melihat guidance sesuai Bagian coachee yang di-map ke mereka | VERIFIED | `CDPController.cs:156-172` — `isCoach` branch pada guidanceQuery dengan 3-way join `CoachCoacheeMappings -> ProtonTrackAssignments -> ProtonKompetensiList` dan filter distinct Bagian |
| 9 | Coachee tidak bisa akses admin guidance management tab | VERIFIED | `Views/CDP/PlanIdp.cshtml` — tidak ada admin management tab. Hanya 2 tab read-only (Silabus dan Coaching Guidance). Management ada di ProtonDataController dengan `[Authorize(Roles="Admin,HC")]` |

**Score:** 9/9 truths verified

---

### Required Artifacts

| Artifact | Menyediakan | Status | Detail |
|----------|-------------|--------|--------|
| `Models/ProtonModels.cs` | EvidencePathHistory column di ProtonDeliverableProgress | VERIFIED | Line 140: `public string? EvidencePathHistory { get; set; }` |
| `Controllers/CDPController.cs` | RecordStatusHistory di UploadEvidence, EvidencePathHistory, upload rollback, race guard, HC_REVIEW_COMPLETE notif, COACH_EVIDENCE_RESUBMITTED notif, dedup fix | VERIFIED | Semua pattern ditemukan di lokasi yang diharapkan |
| `Controllers/AdminController.cs` | StatusHistory 'Pending' insert di AutoCreateProgressForAssignment | VERIFIED | Line 6847: `StatusType = "Pending"` dalam foreach loop setelah SaveChangesAsync |
| `Controllers/ProtonDataController.cs` | StatusHistory 'Pending' insert di silabus seed | VERIFIED | Line 515: `StatusType = "Pending"` dalam seed loop |
| `Views/CDP/PlanIdp.cshtml` | Tab access control read-only — tidak ada admin management tab | VERIFIED | File hanya memiliki 2 tab read-only tanpa admin-gated sections |

---

### Key Link Verification

| From | To | Via | Status | Detail |
|------|----|-----|--------|--------|
| `CDPController.UploadEvidence` | `RecordStatusHistory` | call sebelum SaveChangesAsync | WIRED | Line 1253: `RecordStatusHistory(progress.Id, uploadStatusType, ...)` lalu `await _context.SaveChangesAsync()` line 1255 |
| `AdminController.AutoCreateProgressForAssignment` | `DeliverableStatusHistories.Add` | loop setelah SaveChangesAsync flush | WIRED | Line 6842-6854: foreach loop + second `SaveChangesAsync()` |
| `CDPController.ApproveDeliverable` | `ProtonDeliverableProgresses reload` | fresh DB query before status write | WIRED | Line 830-848: `AsNoTracking()` reload + `stillCanApprove` re-check |
| `CDPController.HCReviewDeliverable` | `_notificationService` | SendAsync call | WIRED | Line 1136-1143: `await _notificationService.SendAsync(...)` dengan type `HC_REVIEW_COMPLETE` |
| `CDPController.UploadEvidence` | `COACH_EVIDENCE_RESUBMITTED` | wasRejected conditional | WIRED | Line 1266: `if (wasRejected)` block dengan SendAsync type `COACH_EVIDENCE_RESUBMITTED` |
| `Views/CDP/PlanIdp.cshtml` | `ViewBag.UserLevel` | Razor conditional rendering | WIRED | Line 14: `var userLevel = (int)(ViewBag.UserLevel ?? 0)` digunakan di L16 `isL4Locked` check |
| `CreateHCNotificationAsync` | dedup exact match | `n.Message ==` | WIRED | Line 1072: `.AnyAsync(n => n.UserId == hc.Id && n.Type == "COACH_ALL_COMPLETE" && n.Message == expectedMessage)` |

---

### Requirements Coverage

| Requirement | Source Plan | Deskripsi | Status | Evidence |
|-------------|-------------|-----------|--------|---------|
| EXEC-01 | 235-01 | Audit Evidence submission flow end-to-end — upload, reject+resubmit, multi-file handling | SATISFIED | UploadEvidence: try-catch rollback, EvidencePathHistory JSON, RecordStatusHistory Submitted/Re-submitted, wasRejected flag |
| EXEC-02 | 235-02 | Audit Approval chain — edge cases (concurrent approve, Admin override, partial approval) | SATISFIED | ApproveDeliverable: AsNoTracking reload + stillCanApprove re-check + TempData error pada race loss |
| EXEC-03 | 235-01 | Audit DeliverableStatusHistory — completeness di setiap state transition termasuk initial Pending | SATISFIED | RecordStatusHistory di UploadEvidence + AdminController seed + ProtonDataController seed |
| EXEC-04 | 235-02 | Audit Notifikasi — semua Proton notification triggers (evidence submit, approve, reject, HC review, final assessment) | SATISFIED | HC_REVIEW_COMPLETE notif di HCReviewDeliverable, COACH_EVIDENCE_RESUBMITTED di UploadEvidence, dedup exact match fix |
| EXEC-05 | 235-03 | Audit PlanIdp view — silabus display accuracy, guidance tabs, role-based access correctness | SATISFIED | D-20 Coach guidance scoping via CoachCoacheeMappings 3-way join, D-22 N/A (no admin tab di PlanIdp) |

**Semua 5 requirement ID terpenuhi. Tidak ada orphaned requirement.**

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Controllers/CDPController.cs` | 1141 | `catch (Exception ex) { _logger.LogWarning(...) }` | Info | Notification failure di HC Review di-swallow ke Warning log — by design (notification tidak boleh break main operation) |

Tidak ada blocker anti-pattern. Catch blocks yang ada adalah intentional (notification failures tidak boleh break operasi utama).

---

### Human Verification Required

5 item memerlukan verifikasi human:

#### 1. Re-submit setelah reject — StatusHistory bertipe 'Re-submitted'

**Test:** Login sebagai Coach. Cari deliverable yang sudah di-reject oleh SrSpv/SH. Upload ulang evidence. Cek tabel `DeliverableStatusHistories` di DB.
**Expected:** Entry baru dengan `StatusType = 'Re-submitted'` muncul (bukan 'Submitted')
**Why human:** Memerlukan multi-step flow (approval chain selesai dulu → rejection → resubmit). Data test yang sesuai belum tersedia saat UAT.

#### 2. Race condition guard ApproveDeliverable — concurrent approve

**Test:** Simulasikan dua approver berbeda (SrSpv dan SectionHead) approve deliverable yang sama secara bersamaan menggunakan dua tab/browser berbeda.
**Expected:** Approver pertama berhasil, approver kedua melihat TempData error "Deliverable sudah diproses oleh approver lain." dan redirect — bukan error 500.
**Why human:** Memerlukan simulasi concurrent HTTP request. Tidak bisa diverifikasi via single browser atau code analysis.

#### 3. HC Review Notifikasi ke Coach

**Test:** Selesaikan full approval chain (SrSpv approve + SH approve), lalu login sebagai HC dan klik HCReview deliverable. Cek tabel `UserNotifications` di DB.
**Expected:** Entry notifikasi dengan `Type = 'HC_REVIEW_COMPLETE'` untuk Coach yang di-map ke coachee tersebut.
**Why human:** Memerlukan full approval chain completion terlebih dahulu. Multi-step flow.

#### 4. Resubmit Notifikasi ke Reviewer

**Test:** Setelah deliverable di-reject, Coach resubmit evidence. Cek tabel `UserNotifications`.
**Expected:** Section reviewer (RoleLevel=4) menerima notifikasi bertipe `COACH_EVIDENCE_RESUBMITTED`.
**Why human:** Memerlukan reject + resubmit flow — data test rejected deliverable belum tersedia saat UAT.

#### 5. Upload Evidence Rollback

**Test:** Simulasikan file save failure (misal: hapus permission folder `uploads/evidence/`, atau isi disk). Upload evidence dari Coach.
**Expected:** Progress status TIDAK berubah (tetap Pending). User melihat pesan error dan redirect ke halaman Deliverable tanpa corrupted state.
**Why human:** Memerlukan simulasi kondisi sistem failure — tidak bisa dilakukan via normal browser testing.

---

### Gaps Summary

Tidak ada gap pada implementasi kode. Semua 9 truths terverifikasi di kode aktual:

- Plan 01 (EXEC-01, EXEC-03): EvidencePathHistory, RecordStatusHistory di UploadEvidence, rollback try-catch, Pending seed di kedua lokasi — semua terpasang.
- Plan 02 (EXEC-02, EXEC-04): Race condition guard AsNoTracking + stillCanApprove, HC_REVIEW_COMPLETE notif, COACH_EVIDENCE_RESUBMITTED notif, dedup exact match — semua terpasang.
- Plan 03 (EXEC-05): Coach guidance scoping via 3-way join, tidak ada admin management tab di PlanIdp — diaudit dan diperbaiki.

Status `human_needed` semata-mata karena 5 item runtime behavior memerlukan verifikasi melalui browser/DB yang tidak bisa dilakukan via code grep. Hasil UAT parsial (235-UAT.md) menunjukkan 3 dari 8 test sudah PASS, 5 skipped karena memerlukan multi-step flows.

Build: Tidak ada `error CS` compiler errors. MSB3027 error hanya karena binary file locked (aplikasi sedang berjalan) — bukan kegagalan kompilasi.

---

_Verified: 2026-03-22T16:00:00Z_
_Verifier: Claude (gsd-verifier)_
