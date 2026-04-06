---
phase: 296-data-foundation-gradingservice-extraction
verified: 2026-04-06T09:00:00Z
status: human_needed
score: 9/9 must-haves verified
human_verification:
  - test: "Jalankan dotnet ef database update dan verifikasi kolom baru muncul di tabel DB"
    expected: "7 kolom baru (AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading, QuestionType, TextAnswer) ada di database"
    why_human: "Verifikasi migration sudah diapply ke database aktual — tidak bisa dicek dari kode saja"
  - test: "Submit ujian via CMPController.SubmitExam() dengan soal MultipleChoice dan cek hasil grading"
    expected: "Skor terhitung benar, session.Status = 'Completed', TrainingRecord dibuat, NomorSertifikat ter-generate jika applicable"
    why_human: "End-to-end flow grading memerlukan database aktif dan session test yang valid"
  - test: "Admin klik AkhiriUjian() untuk session aktif dan verifikasi tidak ada regresi"
    expected: "GradingService dipanggil, session selesai, cache dihapus, SignalR push terkirim"
    why_human: "Perilaku real-time (SignalR) dan cache invalidation tidak bisa diverifikasi secara statis"
  - test: "Submit soal bertipe MultipleAnswer atau Essay dan verifikasi error handling"
    expected: "NotImplementedException dilempar dan ditangani oleh controller dengan pesan yang tepat (bukan crash 500)"
    why_human: "Behavior exception handling di production path memerlukan runtime test"
---

# Phase 296: Data Foundation + GradingService Extraction — Laporan Verifikasi

**Phase Goal:** Fondasi teknis untuk v14.0 — migrasi DB backward-compatible (kolom baru nullable/default) dan ekstraksi GradingService sebagai komponen terpusat yang menggantikan logika grading duplikat di AssessmentAdminController dan CMPController

**Verified:** 2026-04-06T09:00:00Z
**Status:** human_needed
**Re-verification:** Tidak — verifikasi awal

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|---------|
| 1 | AssessmentSession memiliki 5 kolom baru: AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading | VERIFIED | `Models/AssessmentSession.cs` baris 131,136,142,148,154 — semua property ada dan nullable/default |
| 2 | PackageQuestion memiliki kolom QuestionType string nullable | VERIFIED | `Models/AssessmentPackage.cs` baris 48: `public string? QuestionType { get; set; }` |
| 3 | PackageUserResponse memiliki kolom TextAnswer string nullable | VERIFIED | `Models/PackageUserResponse.cs` baris 29: `public string? TextAnswer { get; set; }` |
| 4 | Semua kolom baru nullable atau punya default value — tidak ada breaking change | VERIFIED | Migration baris 37-42: `HasManualGrading` punya `defaultValue: false`; semua kolom lain nullable |
| 5 | GradingService terdaftar di DI container dan bisa di-inject | VERIFIED | `Program.cs` baris 55: `builder.Services.AddScoped<HcPortal.Services.GradingService>()` |
| 6 | GradeAndCompleteAsync menghitung skor, update session, buat TrainingRecord, generate NomorSertifikat, kirim notifikasi | VERIFIED | `Services/GradingService.cs` 237 baris — semua 6 langkah ada dengan implementasi substansial |
| 7 | Switch-case per QuestionType ada — hanya MultipleChoice diimplementasi, sisanya throw NotImplementedException | VERIFIED | `GradingService.cs` baris 86-103: switch-case dengan `case "MultipleChoice"` implemented, MA+Essay throw NotImplementedException |
| 8 | AssessmentAdminController.AkhiriUjian() dan AkhiriSemuaUjian() memanggil GradeAndCompleteAsync(), bukan GradeFromSavedAnswers() | VERIFIED | 2 pemanggilan di baris 2256 dan 2334; GradeFromSavedAnswers: 0 matches |
| 9 | CMPController.SubmitExam() memanggil GradeAndCompleteAsync(), bukan inline grading logic | VERIFIED | `Controllers/CMPController.cs` baris 1516: `_gradingService.GradeAndCompleteAsync(assessment)` |

**Score:** 9/9 truths verified

---

## Required Artifacts

### Plan 01 (Data Foundation)

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Models/AssessmentSession.cs` | 5 kolom baru | VERIFIED | AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading — semua ada |
| `Models/AssessmentPackage.cs` | QuestionType pada PackageQuestion | VERIFIED | `public string? QuestionType { get; set; }` baris 48 |
| `Models/PackageUserResponse.cs` | TextAnswer nullable | VERIFIED | `public string? TextAnswer { get; set; }` baris 29 |
| `Migrations/20260406075820_AddAssessmentV14Columns.cs` | 7 AddColumn + 7 DropColumn | VERIFIED | 7 AddColumn di Up(), 7 DropColumn di Down() — termasuk `defaultValue: false` untuk HasManualGrading |

### Plan 02 (GradingService Extraction)

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Services/GradingService.cs` | GradingService concrete class dengan GradeAndCompleteAsync | VERIFIED | 237 baris, substantif — bukan stub |
| `Program.cs` | DI registration GradingService | VERIFIED | `AddScoped<HcPortal.Services.GradingService>()` baris 55 |

### Plan 03 (Controller Wiring)

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Controllers/AssessmentAdminController.cs` | Gunakan GradingService, hapus GradeFromSavedAnswers | VERIFIED | 2 pemanggilan GradeAndCompleteAsync, 0 GradeFromSavedAnswers, field `_gradingService` baris 28 |
| `Controllers/CMPController.cs` | SubmitExam menggunakan GradingService | VERIFIED | 1 pemanggilan GradeAndCompleteAsync baris 1516, field `_gradingService` baris 38 |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `Services/GradingService.cs` | `ApplicationDbContext` | constructor injection | WIRED | `private readonly ApplicationDbContext _context` baris 18 |
| `Services/GradingService.cs` | `IWorkerDataService` | constructor injection | WIRED | `private readonly IWorkerDataService _workerDataService` baris 19 |
| `Program.cs` | `Services/GradingService.cs` | AddScoped registration | WIRED | `AddScoped<HcPortal.Services.GradingService>()` baris 55 |
| `Controllers/AssessmentAdminController.cs` | `Services/GradingService.cs` | constructor injection + method call | WIRED | Field baris 28, 2 pemanggilan GradeAndCompleteAsync |
| `Controllers/CMPController.cs` | `Services/GradingService.cs` | constructor injection + method call | WIRED | Field baris 38, 1 pemanggilan GradeAndCompleteAsync baris 1516 |

---

## Data-Flow Trace (Level 4)

GradingService bukan komponen rendering — ini adalah service layer. Level 4 berlaku terbatas, namun alur data utama diverifikasi:

| Artifact | Data Source | Mengambil dari DB | Status |
|----------|-------------|-------------------|--------|
| `GradingService.GradeAndCompleteAsync` | `UserPackageAssignments`, `PackageQuestions`, `PackageUserResponses` | ExecuteUpdateAsync + AnyAsync (bukan hardcoded) | FLOWING |
| `GradingService` → `TrainingRecord` | DB check via `AnyAsync` sebelum insert | Guard duplikasi real | FLOWING |
| `GradingService` → `NomorSertifikat` | `CertNumberHelper.GetNextSeqAsync` (DB sequence) | DB query nyata dengan retry loop | FLOWING |

---

## Behavioral Spot-Checks

| Behavior | Command | Result | Status |
|----------|---------|--------|--------|
| GradeFromSavedAnswers tidak ada | `grep -n "GradeFromSavedAnswers" Controllers/AssessmentAdminController.cs \| wc -l` | 0 | PASS |
| GradingService ada di DI | `grep "AddScoped.*GradingService" Program.cs` | match baris 55 | PASS |
| Migration punya 7 AddColumn | `grep -c "AddColumn" Migrations/*AddAssessmentV14Columns.cs` | 7 | PASS |
| Migration punya defaultValue false | `grep "defaultValue: false" Migrations/*AddAssessmentV14Columns.cs` | match | PASS |
| Controller calls: 2 di Admin, 1 di CMP | grep count | Admin=2, CMP=1 | PASS |

---

## Requirements Coverage

CATATAN PENTING: `REQUIREMENTS.md` di `.planning/REQUIREMENTS.md` hanya berisi requirements v13.0 (TREE-*, CRUD-*, REORD-*). Requirement IDs FOUND-01 sampai FOUND-09 yang direferensikan oleh phase 296 TIDAK ditemukan di file tersebut. Requirement-requirement ini didokumentasikan di `296-CONTEXT.md` sebagai deferred/future requirements, namun tidak pernah dimasukkan ke REQUIREMENTS.md formal.

Berdasarkan pemetaan dari CONTEXT.md dan PLAN frontmatter:

| Requirement ID | Source Plan | Deskripsi (dari CONTEXT.md / PLAN) | Status | Bukti |
|---------------|-------------|-------------------------------------|--------|-------|
| FOUND-01 | Plan 02 | GradingService terdaftar di DI container | SATISFIED | Program.cs baris 55 |
| FOUND-02 | Plan 01 | QuestionType enum (3 nilai: MC, MA, Essay) di PackageQuestion | SATISFIED | Models/AssessmentPackage.cs baris 48 |
| FOUND-03 | Plan 01 | AssessmentSession.AssessmentType kolom baru | SATISFIED | Models/AssessmentSession.cs baris 131 |
| FOUND-04 | Plan 01 | AssessmentSession.AssessmentPhase, LinkedGroupId, LinkedSessionId | SATISFIED | baris 136, 142, 148 |
| FOUND-05 | Plan 01 | PackageUserResponse.TextAnswer + HasManualGrading + migration | SATISFIED | PackageUserResponse.cs baris 29; Migration 7 kolom |
| FOUND-06 | Plan 02 | GradeAndCompleteAsync dengan semua 6 langkah + switch-case QuestionType | SATISFIED | GradingService.cs 237 baris, substantif |
| FOUND-07 | Plan 03 | AkhiriUjian menggunakan GradingService | SATISFIED | AssessmentAdminController.cs baris 2256 |
| FOUND-08 | Plan 03 | AkhiriSemuaUjian menggunakan GradingService | SATISFIED | AssessmentAdminController.cs baris 2334 |
| FOUND-09 | Plan 03 | SubmitExam menggunakan GradingService | SATISFIED | CMPController.cs baris 1516 |

**ORPHANED (tidak ada di REQUIREMENTS.md formal):** Semua 9 requirement ID (FOUND-01 sampai FOUND-09) tidak terdapat di `.planning/REQUIREMENTS.md`. File REQUIREMENTS.md belum diperbarui untuk v14.0.

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| `Services/GradingService.cs` | 97-103 | `throw new NotImplementedException` untuk MultipleAnswer dan Essay | INFO | Desain eksplisit per D-08 — bukan stub, direncanakan untuk Phase 298 |

Tidak ada anti-pattern yang menghalangi goal. NotImplementedException adalah desain yang disengaja dan terdokumentasi.

---

## Human Verification Required

### 1. Database Migration Applied

**Test:** Jalankan `dotnet ef database update` di environment dev/staging dan periksa schema tabel
**Expected:** Kolom AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading, QuestionType, TextAnswer muncul di tabel yang sesuai
**Why human:** Verifikasi schema database aktual tidak bisa dilakukan secara statis dari file kode

### 2. End-to-End Grading Flow via CMPController

**Test:** Login sebagai worker, submit ujian dengan soal MultipleChoice yang sudah ada, klik submit
**Expected:** Session berubah ke Status=Completed, skor terhitung benar, TrainingRecord dibuat di DB, NomorSertifikat ter-generate jika GenerateCertificate=true dan passed
**Why human:** Alur end-to-end memerlukan database aktif, session valid, dan user authentication

### 3. End-to-End AkhiriUjian via Admin

**Test:** Login sebagai Admin, buka ujian yang sedang berjalan, klik "Akhiri Ujian" untuk satu session
**Expected:** Session selesai, cache dihapus, SignalR push terkirim ke monitor, tidak ada 500 error
**Why human:** Perilaku SignalR real-time dan cache tidak bisa diverifikasi dari analisis kode statis

### 4. NotImplementedException Handling untuk Tipe Soal Non-MC

**Test:** Jika ada paket dengan soal bertipe MultipleAnswer atau Essay (atau buat test data), submit ujian tersebut
**Expected:** Tidak crash dengan 500 unhandled; controller menangani exception dengan pesan yang informatif
**Why human:** Exception handling behavior di production path memerlukan runtime testing; SUMMARY tidak mendokumentasikan bagaimana controller menangani false return dari NotImplementedException

---

## Gaps Summary

Tidak ada gap teknis yang memblokir goal. Semua 9 observable truths VERIFIED secara programatik.

**Catatan non-blocking:**

1. **REQUIREMENTS.md tidak diperbarui** — File `.planning/REQUIREMENTS.md` masih berisi hanya requirement v13.0. Requirement FOUND-01 sampai FOUND-09 untuk v14.0 tidak tercatat di sana. Ini gap dokumentasi, bukan gap implementasi — kode berjalan dengan benar.

2. **NotImplementedException exposure** — Jika ada paket soal dengan tipe MultipleAnswer atau Essay di database yang sudah ada (dari data lama atau import), memanggil GradeAndCompleteAsync akan melempar NotImplementedException yang tidak ter-handle. SUMMARY Plan 03 tidak mendokumentasikan bagaimana controller menangani exception ini. Item human verification #4 perlu dikonfirmasi.

---

_Verified: 2026-04-06T09:00:00Z_
_Verifier: Claude (gsd-verifier)_
