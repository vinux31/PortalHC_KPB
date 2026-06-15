---
phase: 387-post-lisensor-assessment-polish
reviewed: 2026-06-16T07:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - Controllers/AssessmentAdminController.cs
  - Controllers/CMPController.cs
  - Hubs/AssessmentHub.cs
  - Views/CMP/Results.cshtml
  - Views/CMP/ExamSummary.cshtml
  - HcPortal.Tests/PostLisensorPolishTests.cs
findings:
  critical: 0
  warning: 1
  info: 0
  total: 1
status: issues_found
---

# Phase 387: Code Review Report

**Reviewed:** 2026-06-16T07:00:00Z
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

Enam file sumber ditinjau. Lima dari enam perubahan bersih: WR-01/WR-02 guard chain di `SubmitEssayScore` benar secara logika dan urutan; WR-02 menggunakan nav-path `PackageQuestion.AssessmentPackage.AssessmentSessionId` yang valid (keduanya terdefinisi di model). Retry loop cert (PXF-08) benar: `certSaved` di-set setelah `ExecuteUpdateAsync` yang idempoten, `certError` membaca `updatedSession` hasil re-fetch dari DB sehingga refleksi aktual DB. Guard `answers.ContainsKey(q.Id)` (PXF-12) tepat melingkupi blok upsert. Timer guard `SaveTextAnswer` adalah mirror verbatim `SaveMultipleAnswer` — logika dan timezone UTC konsisten. Perubahan Razor di `Results.cshtml` dan `ExamSummary.cshtml` menggunakan `List<T>` (bukan `IEnumerable`) sehingga indeks `[oi]` aman. Satu temuan warning: `workerName` dalam broadcast PXF-10 selalu mengembalikan `"Unknown"` karena sesi tidak di-load dengan `.Include(s => s.User)`.

---

## Warnings

### WR-01: `workerName` pada broadcast PXF-10 selalu `"Unknown"` — nav prop `session.User` tidak dimuat

**File:** `Controllers/AssessmentAdminController.cs:3822`

**Issue:** `session` dimuat via `FindAsync(sessionId)` di baris 3612, tanpa `.Include(s => s.User)`. Properti navigasi `session.User` oleh karena itu selalu `null`. Ekspresi `session.User?.FullName ?? "Unknown"` di baris 3822 jatuh ke fallback `"Unknown"` setiap saat. Komentar di baris 3816 ("session.User nav → null-safe via FindAsync") keliru — `FindAsync` hanya memuat entitas root, bukan nav-prop. Analog yang benar ada di baris 3208 (`workerAnswerEdited`) yang secara eksplisit menggunakan `.Include(s => s.User)`. Dampak: kolom "Nama Peserta" di tab Monitoring selalu tampil kosong/`"Unknown"` saat HC menyelesaikan penilaian essay manual, bukan nama peserta aktual.

**Fix:**

```csharp
// Ganti di FinalizeEssayGrading (L3612):
// Sebelum:
var session = await _context.AssessmentSessions.FindAsync(sessionId);

// Sesudah:
var session = await _context.AssessmentSessions
    .Include(s => s.User)
    .FirstOrDefaultAsync(s => s.Id == sessionId);
```

Tidak ada perubahan lain yang diperlukan — `session.User?.FullName ?? "Unknown"` di baris 3822 sudah null-safe.

---

_Reviewed: 2026-06-16T07:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
