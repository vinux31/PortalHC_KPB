---
phase: 386-assessmentadmincontroller-hardening
reviewed: 2026-06-16T00:00:00Z
depth: standard
files_reviewed: 7
files_reviewed_list:
  - Helpers/QuestionOptionValidator.cs
  - Helpers/AssessmentScoreAggregator.cs
  - Helpers/ExcelExportHelper.cs
  - Controllers/AssessmentAdminController.cs
  - HcPortal.Tests/OptionValidationTests.cs
  - HcPortal.Tests/EssayEmptyPendingParityTests.cs
  - HcPortal.Tests/PdfAnswerCellTests.cs
findings:
  critical: 0
  warning: 2
  info: 1
  total: 3
status: issues_found
---

# Phase 386: Code Review Report

**Reviewed:** 2026-06-16
**Depth:** standard
**Files Reviewed:** 7
**Status:** issues_found

## Summary

Review mencakup 5 gelombang perubahan pada Phase 386: (1) helper murni `QuestionOptionValidator` + `BuildAnswerCell`, (2) wiring validasi PXF-02 ke `CreateQuestion`/`EditQuestion`, (3) penyeragaman predikat pending-essay 4 titik (PXF-04), (4) upsert + status-guard di `SubmitEssayScore`, dan (5) penarikan `GeneratePerPesertaPdf` + `AddDetailPerSoalSheet` ke shared display helper. Kode secara keseluruhan terstruktur dengan baik, komentar memadai, dan parity 4 surface essay-pending sudah benar. Dua temuan Warning ditemukan — keduanya terkait gap validasi input pada `SubmitEssayScore` yang diperkenalkan oleh upsert baru.

---

## Warnings

### WR-01: `SubmitEssayScore` tidak memvalidasi bahwa `questionId` bertipe Essay — upsert bisa membuat baris EssayScore pada soal MC/MA

**File:** `Controllers/AssessmentAdminController.cs:3542-3548`

**Issue:** Setelah upsert diperkenalkan (PXF-04 D-08), `question` dimuat hanya untuk validasi rentang skor (`ScoreValue`). Tidak ada pengecekan `question.QuestionType == "Essay"`. HC (Admin/HC role) yang mengirim request crafted `POST /Admin/SubmitEssayScore?sessionId=X&questionId=Y_MC&score=5` akan: (a) melewati status-guard karena sesi bertipe PendingGrading valid, (b) membuat baris `PackageUserResponse` baru dengan `EssayScore=5` pada soal MC/MA — korupsi data yang tidak terdeteksi karena `FinalizeEssayGrading` hanya menjumlah essay-score via `AssessmentScoreAggregator.Compute` case Essay. Dampak: penambahan skor tak sah pada session. Pra-Phase 386, `FirstOrDefault` pada baris yang ada memberikan perlindungan implisit (baris MC tidak punya EssayScore, tapi path update masih bisa terkena). Upsert baru menghilangkan perlindungan alamiah itu.

**Fix:** Tambahkan guard tipe soal setelah baris 3545:
```csharp
if (question.QuestionType != "Essay")
    return Json(new { success = false, message = "Soal ini bukan tipe Essay." });
```

---

### WR-02: `SubmitEssayScore` tidak memvalidasi bahwa `questionId` milik `sessionId` (cross-session tampering)

**File:** `Controllers/AssessmentAdminController.cs:3543-3548`

**Issue:** `question` diload dengan `FindAsync(questionId)` tanpa memverifikasi bahwa soal tersebut berasal dari paket yang dimiliki `sessionId`. HC dapat mengirimkan `questionId` dari sesi/paket lain yang juga `PendingGrading`, lalu upsert membuat baris `PackageUserResponse` yang menautkan `sessionId` dan `questionId` lintas-sesi. Ini mengakibatkan korupsi data: `pendingCount` pada sesi korban menurun; `FinalizeEssayGrading` akan menghitung skor dari soal lintas-paket. Pra-Phase 386, path ini kurang berbahaya karena hanya me-update EssayScore pada baris yang sudah ada (dan baris lintas-sesi hanya ada bila peserta memang mengerjakan sesi itu). Upsert baru memungkinkan penciptaan baris lintas-sesi dari nol.

**Fix:** Validasi kepemilikan via `UserPackageAssignment` sebelum upsert:
```csharp
var assignment = await _context.UserPackageAssignments
    .FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
if (assignment == null)
    return Json(new { success = false, message = "Assignment tidak ditemukan." });
var shuffledIds = assignment.GetShuffledQuestionIds();
if (!shuffledIds.Contains(questionId))
    return Json(new { success = false, message = "Soal tidak termasuk dalam sesi ini." });
```
Catatan: `FinalizeEssayGrading` sudah melakukan validasi ini via `shuffledIds` (L3638-3641). `SubmitEssayScore` seharusnya mengikuti pola yang sama.

---

## Info

### IN-01: Komentar nomor langkah di `SubmitEssayScore` tidak berurutan — langkah 3 dilabeli komentar "5"

**File:** `Controllers/AssessmentAdminController.cs:3571`

**Issue:** Blok `// 5. Cek berapa Essay masih pending` muncul sebagai langkah ketiga dalam fungsi setelah restrukturisasi Phase 386 (langkah 1 = status-guard, 2 = load question, 3 = upsert, kemudian langkah ini). Penomoran lama (`// 5.`) berasal dari kode pra-386 yang memiliki 5 langkah (1 load response, 2 load question, 3 validasi, 4 save, 5 cek pending). Setelah refaktor, langkah ini sebenarnya adalah langkah 4.

**Fix:** Ubah komentar menjadi `// 4. Cek berapa Essay masih pending` agar penomoran konsisten dengan urutan eksekusi aktual.

---

_Reviewed: 2026-06-16_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
