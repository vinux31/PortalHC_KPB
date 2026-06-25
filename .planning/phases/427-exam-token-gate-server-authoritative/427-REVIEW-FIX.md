---
phase: 427-exam-token-gate-server-authoritative
fixed: 2026-06-25
source_review: 427-REVIEW.md
findings_total: 2
fixed_count: 1
deferred_count: 1
status: resolved
---

# Phase 427 — Code Review Fix Log

Sumber: `427-REVIEW.md` (0 Critical, 1 Warning, 1 Info).

## WR-01 — FIXED ✅

**File:** `HcPortal.Tests/RetakeExamEndpointTests.cs`
**Masalah:** Test `RetakeExam_Success_ClearsTokenAndRedirectsToStartExam` (terakhir disentuh Phase 407) masih menyemai `tempData["TokenVerified_{id}"]=true` lalu meng-assert key itu terhapus. Phase 427 memindah re-arm token dari TempData ke kolom DB `TokenVerifiedAt` (single-source `RetakeService.ExecuteAsync`), jadi controller `RetakeExam` tak lagi menyentuh TempData → assertion `Assert.False(true)` akan FAIL pada run real-SQL berikutnya.

**Fix:**
- `SeedSessionAsync` ditambah param opsional `DateTime? tokenVerifiedAt = null` (set kolom `TokenVerifiedAt` saat seed).
- Test sukses kini menyemai sesi dengan `tokenVerifiedAt: UtcNow.AddMinutes(-30)` (sesi sebelumnya sudah verifikasi token), menghapus seeding+assertion TempData, dan meng-assert kolom DB `TokenVerifiedAt == null` pasca-retake (selain `Status == "Open"`).
- Komentar header file (kasus 3) di-update ke kontrak server-authoritative.

**Verifikasi:** `dotnet build` 0 error; `dotnet test --filter RetakeExamEndpointTests` (real-SQL `RetakeServiceFixture`) → **3/3 PASS** (kasus 1 Forbid, kasus 2 NotEligible, kasus 3 sukses+token-reset). Tidak ada regresi pada kasus lain.

## IN-01 — DEFERRED (Info, opsional)

Komentar EXSEC-01 terdistribusi di 4 lokasi merujuk "gantikan TempData" — kode benar, hanya saran konsolidasi komentar agar tak drift saat cleanup milestone. Non-blocking; dibawa sebagai catatan kosmetik (tidak mengubah perilaku).
