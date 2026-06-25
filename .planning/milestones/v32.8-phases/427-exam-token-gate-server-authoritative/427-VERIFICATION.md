---
phase: 427-exam-token-gate-server-authoritative
verified: 2026-06-24T13:50:34Z
status: passed
score: 4/4
overrides_applied: 0
---

# Phase 427: Exam Token-Gate Server-Authoritative — Verification Report

**Phase Goal:** Verifikasi token masuk ujian menjadi server-authoritative & persisten — disimpan di kolom DB `AssessmentSession.TokenVerifiedAt` alih-alih `TempData.Peek`, dan di-reset saat retake/ResetExam agar gate token konsisten "minta ulang" pada percobaan baru.
**Verified:** 2026-06-24T13:50:34Z
**Status:** PASSED
**Re-verification:** Tidak — initial verification.

---

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
|----|-------|--------|----------|
| 1  | Gate `StartExam` membaca `TokenVerifiedAt` (server-authoritative), BUKAN `TempData.Peek` | VERIFIED | `CMPController.cs:967` — `if (assessment.TokenVerifiedAt == null)` menggantikan `TempData.Peek(...)`. Outer guard `IsTokenRequired && UserId==user.Id && StartedAt==null` dipertahankan persis. Grep `TempData.Peek` pada gate = 0 hit. |
| 2  | `VerifyToken` sukses men-stamp `TokenVerifiedAt=DateTime.UtcNow` + persist DB | VERIFIED | `CMPController.cs:902-903` — `assessment.TokenVerifiedAt = DateTime.UtcNow; await _context.SaveChangesAsync();` hanya pada jalur token-required sukses (D-03). Cabang not-required tidak di-stamp. |
| 3  | Retake/Reset me-reset `TokenVerifiedAt=null` single-source di `RetakeService.ExecuteAsync` → gate re-arm | VERIFIED | `RetakeService.cs:127` — `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` di `ExecuteUpdateAsync` chain yang sama dengan reset `StartedAt`. Komentar XML diperbarui (D-01). `TempData.Remove` token dihapus dari `CMPController.RetakeExam:2587-2588` (komentar D-01) dan `AssessmentAdminController.ResetAssessment:4409-4410` (komentar D-01). |
| 4  | Migration `AddTokenVerifiedAt` applied (kolom hadir); sesi InProgress lama tidak terkunci (guard `StartedAt==null` utuh) | VERIFIED | File `Migrations/20260624133656_AddTokenVerifiedAt.cs` ada — `AddColumn<DateTime>("TokenVerifiedAt", nullable: true)`. Snapshot `ApplicationDbContextModelSnapshot.cs:563-564` memuat `TokenVerifiedAt`. Guard `assessment.StartedAt == null` pada outer gate (`CMPController.cs:964`) UTUH — sesi InProgress (`StartedAt!=null`) bypass gate token. |

**Score:** 4/4 truths verified

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/AssessmentSession.cs` | Kolom `DateTime? TokenVerifiedAt` setelah `AccessToken` | VERIFIED | Line 107-112: properti dengan XML doc EXSEC-01 + `public DateTime? TokenVerifiedAt { get; set; }` |
| `Migrations/20260624133656_AddTokenVerifiedAt.cs` | Migration aditif nullable, no backfill | VERIFIED | `AddColumn<DateTime>("TokenVerifiedAt", table: "AssessmentSessions", nullable: true)` — datetime2 NULL, tidak ada backfill |
| `Migrations/ApplicationDbContextModelSnapshot.cs` | Memuat `TokenVerifiedAt` | VERIFIED | Line 563-564: `b.Property<DateTime?>("TokenVerifiedAt").HasColumnType("datetime2")` |
| `Controllers/CMPController.cs` | Stamp `TokenVerifiedAt` di VerifyToken + gate baca kolom di StartExam + hapus TempData token | VERIFIED | Line 902: stamp `DateTime.UtcNow`. Line 967: gate `== null`. Seluruh TempData token gate dihapus. |
| `Controllers/AssessmentAdminController.cs` | Hapus `TempData.Remove` token di ResetAssessment | VERIFIED | Line 4409-4410: komentar D-01; tidak ada `TempData.Remove($"TokenVerified_")` |
| `Services/RetakeService.cs` | `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` di `ExecuteUpdateAsync` | VERIFIED | Line 127: setProperty reset null di chain atomik. XML comment line 38-41 diperbarui. |
| `HcPortal.Tests/TokenVerifiedAtTests.cs` | 5 test T1-T5 real-SQL, `[Trait("Category","Integration")]`, `IClassFixture<RetakeServiceFixture>` | VERIFIED | File lengkap 294 baris. 5 `[Fact]` dengan nama eksak sesuai spec. Trait dan fixture benar. |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `CMPController.StartExam` | `AssessmentSession.TokenVerifiedAt` | Gate read pengganti TempData.Peek, di dalam guard `IsTokenRequired && StartedAt==null` | WIRED | `CMPController.cs:964-971`: outer guard `&& assessment.StartedAt == null` → inner `if (assessment.TokenVerifiedAt == null)` → redirect blocked. Pattern `assessment\.TokenVerifiedAt == null` terkonfirmasi. |
| `CMPController.VerifyToken` | `AssessmentSession.TokenVerifiedAt` | Stamp UtcNow + SaveChangesAsync pada jalur token-required sukses | WIRED | `CMPController.cs:901-903`: assignment + `SaveChangesAsync()` sebelum `return Json(...)`. |
| `RetakeService.ExecuteAsync` | `AssessmentSession.TokenVerifiedAt` | `SetProperty` reset null di `ExecuteUpdateAsync` | WIRED | `RetakeService.cs:125-127`: komentar D-01 + `.SetProperty(r => r.TokenVerifiedAt, (DateTime?)null)` dalam satu chain atomik. |

---

### Data-Flow Trace (Level 4)

Tidak applicable — fase ini tidak merender data dinamis ke UI. Perubahan adalah server-authoritative gate logic (write/read kolom DB pada POST/GET controller actions). Aliran data terverifikasi via real-SQL integration tests T1-T5.

---

### Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| T1 (SC#1): gate blokir saat `TokenVerifiedAt=null` | `dotnet test --filter "FullyQualifiedName~TokenVerifiedAtTests"` | 5/5 Passed | PASS |
| T2 (SC#1): gate lolos saat `TokenVerifiedAt` set | (sama) | 5/5 Passed | PASS |
| T3 (SC#2): VerifyToken stamp persist ke DB | (sama) | 5/5 Passed | PASS |
| T4 (SC#3): RetakeService reset null single-source | (sama) | 5/5 Passed | PASS |
| T5 (SC#4): sesi InProgress tidak terkunci pasca-deploy | (sama) | 5/5 Passed | PASS |
| Suite non-Integration (no regression) | `dotnet test --filter "Category!=Integration"` | 544/0/2 | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| EXSEC-01 | 427-01-PLAN.md | Token gate server-authoritative via `TokenVerifiedAt` (ganti TempData) | SATISFIED | Gate StartExam membaca kolom (SC#1 ✓), stamp persist VerifyToken (SC#2 ✓), reset single-source RetakeService (SC#3 ✓), migration applied + no-lockout (SC#4 ✓). 5/5 discriminating tests hijau. |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| — | — | — | — | Tidak ada anti-pattern ditemukan |

Pemeriksaan:
- `grep TokenVerified_ Controllers/ Services/` = **0 hit** — full replacement D-02 terkonfirmasi, tidak ada dead-code TempData token tersisa.
- Tidak ada `return null` / `return {}` / placeholder di file yang dimodifikasi.
- `assessment.StartedAt == null` outer guard masih utuh di CMPController:964 — tidak terhapus.

---

### Human Verification Required

Tidak ada item yang membutuhkan verifikasi manusia untuk tujuan fase ini. Semua success criteria dapat dan telah diverifikasi secara programatik via real-SQL integration tests dan code inspection.

Catatan untuk Tim IT saat deploy:
- **migration=TRUE** — jalankan `dotnet ef database update` di server (migration `AddTokenVerifiedAt`, datetime2 NULL, aditif, zero-downtime, no backfill).
- Sesi ujian yang sedang InProgress tidak akan terkunci (guard `StartedAt==null` bypass gate token untuk sesi yang sudah dimulai).

---

### Gaps Summary

Tidak ada gap. Semua 4 success criteria EXSEC-01 terpenuhi:

1. Gate `StartExam` server-authoritative via kolom DB (bukan TempData.Peek)
2. `VerifyToken` stamp + persist ke DB
3. `RetakeService` reset single-source → gate re-arm
4. Migration applied + no-lockout sesi InProgress lama

---

_Verified: 2026-06-24T13:50:34Z_
_Verifier: Claude (gsd-verifier)_
