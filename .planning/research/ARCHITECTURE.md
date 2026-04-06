# Architecture Patterns — v14.0 Assessment Enhancement

**Domain:** ASP.NET Core MVC — CMP Assessment System Enhancement
**Researched:** 2026-04-06
**Confidence:** HIGH (kode sumber langsung dibaca)

---

## Arsitektur Saat Ini (Baseline)

### Komponen Utama

| Komponen | File | Tanggung Jawab |
|----------|------|----------------|
| `AssessmentAdminController` | Controllers/AssessmentAdminController.cs (~3700 baris) | Admin CRUD session, package, monitoring, bulk actions, reset, export |
| `CMPController` | Controllers/CMPController.cs | Worker exam flow: Assessment list, VerifyToken, StartExam, SaveAnswer, SubmitExam, Results, Certificate |
| `AssessmentHub` | Hubs/AssessmentHub.cs | SignalR: JoinBatch, LogPageNav, OnConnected/Disconnected |
| `AssessmentSession` | Models/AssessmentSession.cs | Record per-user per-ujian; Status (Open/Upcoming/InProgress/Completed); Score, IsPassed, ElapsedSeconds, LastActivePage |
| `AssessmentPackage` | Models/AssessmentPackage.cs | Kumpulan soal dalam satu paket; FK ke AssessmentSession |
| `PackageQuestion` | Models/AssessmentPackage.cs | Soal individual; Order, ScoreValue, ElemenTeknis |
| `PackageOption` | Models/AssessmentPackage.cs | Pilihan jawaban; IsCorrect flag |
| `UserPackageAssignment` | Models/UserPackageAssignment.cs | Per-user shuffle (JSON ShuffledQuestionIds, ShuffledOptionIdsPerQuestion); IsCompleted |
| `PackageUserResponse` | Models/PackageUserResponse.cs | Jawaban tersimpan per soal; SessionId + QuestionId + OptionId |
| `SessionElemenTeknisScore` | Models/SessionElemenTeknisScore.cs | Breakdown skor per elemen teknis |
| `ExamActivityLog` | Models/ExamActivityLog.cs | Audit trail: started, page_nav, disconnected, reconnected, submitted |

### Data Flow Exam (Saat Ini)

```
HC CreateAssessment
  → AssessmentSession (per pekerja, per jadwal)
  → AssessmentPackage (soal-soal)
  → UserPackageAssignment (shuffle per-user, di-generate saat StartExam)

Worker flow:
  GET Assessment list → VerifyToken → StartExam
    → UserPackageAssignment.GetShuffledQuestionIds()
    → Render soal (1 per halaman, paginator)
    ← SaveAnswer (AJAX, upsert PackageUserResponse)
    ← UpdateSessionProgress (AJAX, ElapsedSeconds, LastActivePage)
    → ExamSummary (review jawaban)
    → SubmitExam
      → hitung Score dari PackageUserResponse vs PackageOption.IsCorrect
      → tulis SessionElemenTeknisScore
      → set Status = "Completed", IsPassed, Score
      → log ExamActivityLog "submitted"
    → Results (tampil skor, review, ET breakdown)
    → Certificate (jika GenerateCertificate + IsPassed)

SignalR (real-time):
  Worker JS → Hub.LogPageNav(sessionId, page) → ExamActivityLog
  OnConnect → log "reconnected", OnDisconnect → log "disconnected"
  HC monitor → JoinMonitor(batchKey) → menerima push events
```

---

## Fitur Baru dan Titik Integrasi

### 1. Assessment Type System (Standard / PrePostTest / Interview)

**Model yang DIMODIFIKASI:**

`AssessmentSession` — tambah kolom baru:
```csharp
// Tipe assessment
public string AssessmentType { get; set; } = "Standard"; // "Standard" | "PrePostTest" | "Interview"

// Pre-Post linking
public Guid? LinkedGroupId { get; set; }        // GUID sama antara Pre dan Post
public int? LinkedSessionId { get; set; }        // FK silang Pre ↔ Post
public string? AssessmentPhase { get; set; }     // "Pre" | "Post" | null
```

**Controller yang DIMODIFIKASI — `AssessmentAdminController`:**

- `CreateAssessment GET/POST`: tambah dropdown `AssessmentType`, logic buat 2 session jika PrePostTest (satu Pre, satu Post, share LinkedGroupId, set LinkedSessionId silang)
- `EditAssessment`: batasi edit untuk PrePostTest (phase tidak bisa diubah setelah dibuat)
- `ResetAssessment`: jika session adalah Pre dalam PrePostTest, cascade reset Post juga (query via LinkedSessionId)
- `DeleteAssessment`: cascade ke pasangan via LinkedGroupId
- `AssessmentMonitoring`: grup tampilan Pre+Post jadi satu baris expand (filter via LinkedGroupId)
- `AssessmentMonitoringDetail`: tampil Pre dan Post side-by-side

**Controller yang DIMODIFIKASI — `CMPController`:**

- `Assessment` list view: tampil 2 card terpisah untuk Pre dan Post, dengan badge "Pre-Test" / "Post-Test", indikasi status Pre sebelum Post bisa dimulai
- `StartExam`: validasi urutan — Post tidak bisa dimulai sebelum Pre "Completed"
- `Results`: jika session adalah bagian PrePostTest, tampil link "Lihat Comparison Pre vs Post"
- `PrePostComparison` (BARU): action baru, query Pre + Post via LinkedGroupId, tampil side-by-side skor + gain score

**Tidak ada perubahan pada:**
- `UserPackageAssignment` — shuffle tetap per-session
- `PackageUserResponse` — jawaban tetap per-session
- `AssessmentHub` — SignalR tetap per-session, batchKey tidak berubah
- `ExamActivityLog` — tetap per-session

---

### 2. Question Types (True/False, Multiple Answer, Essay, Fill in the Blank)

**Model yang DIMODIFIKASI:**

`PackageQuestion` — tambah:
```csharp
public string QuestionType { get; set; } = "MultipleChoice";
// "MultipleChoice" | "TrueFalse" | "MultipleAnswer" | "Essay" | "FillInTheBlank"
```

`PackageUserResponse` — tambah:
```csharp
public string? TextAnswer { get; set; }  // untuk Essay dan FillInTheBlank
// PackageOptionId tetap ada untuk MultipleChoice, TrueFalse, MultipleAnswer
// MultipleAnswer: satu baris per opsi yang dipilih (multiple rows per question)
```

**Controller yang DIMODIFIKASI — `AssessmentAdminController`:**

- `ImportPackageQuestions`: parse kolom `QuestionType` dari template Excel; validasi opsi sesuai tipe
- `ManagePackages` / `PreviewPackage`: render UI soal sesuai tipe
- Tambah action `GradeEssay(sessionId, questionId, score)` — HC input nilai manual untuk Essay
- `ExportAssessmentResults`: sertakan TextAnswer dalam export

**Controller yang DIMODIFIKASI — `CMPController`:**

- `StartExam` view: render soal berbeda per QuestionType (checkbox untuk MultipleAnswer, radio TrueFalse, textarea Essay, text input Fill)
- `SaveAnswer` (AJAX): bedakan antara OptionId-based vs TextAnswer-based; untuk MultipleAnswer perlu bulk upsert
- `SubmitExam`: scoring logic per tipe:
  - `MultipleChoice`: tetap via PackageOption.IsCorrect
  - `TrueFalse`: via PackageOption.IsCorrect (hanya 2 opsi)
  - `MultipleAnswer`: semua opsi dipilih harus cocok (exact match semua yang IsCorrect)
  - `Essay`: skor awal 0, tunggu HC grading via `GradeEssay`
  - `FillInTheBlank`: compare TextAnswer dengan jawaban kunci (case-insensitive, trim)
- `Results` view: tampil jawaban teks untuk Essay/Fill

**Perhatian desain SaveAnswer untuk MultipleAnswer:**

Saat ini `SaveAnswer` melakukan upsert tunggal (SessionId + QuestionId). Untuk MultipleAnswer, satu pertanyaan bisa punya N respons. Pilihan terbaik: endpoint terpisah `SaveMultipleAnswer(sessionId, questionId, int[] optionIds)` yang DELETE lama + INSERT baru dalam satu transaksi.

**Integrasi dengan `UserPackageAssignment`:**

`ShuffledOptionIdsPerQuestion` tetap relevan untuk TrueFalse dan MultipleAnswer (opsi tetap diacak). Untuk Essay/FillInTheBlank, shuffle opsi tidak ada — field JSON bisa diisi `[]` untuk soal tipe tersebut.

---

### 3. Mobile Optimization (Exam UI)

**Tidak ada perubahan model atau controller.**

Semua perubahan di Views dan CSS/JS:

- `Views/CMP/StartExam.cshtml`: refactor layout — bottom navigation bar (prev/next), touch-friendly button sizing, swipe gesture JS
- `wwwroot/css/exam.css` (baru atau modifikasi site.css): media queries untuk viewport mobile, `.exam-option` touch target min 44x44px
- `wwwroot/js/exam-mobile.js` (baru): swipe detection (vanilla touch events), bottom nav logic
- Timer display: pindahkan ke sticky header yang tidak tertutup keyboard virtual
- Pagination sudah ada di `StartExam` — tidak perlu perubahan server-side

**Integrasi dengan SignalR:**

`AssessmentHub.LogPageNav` dipanggil dari JS — tidak berubah. Swipe navigation hanya memicu page nav yang sama.

---

### 4. Advanced Reporting

**Tidak memerlukan tabel baru** — semua data sudah ada:
- `PackageUserResponse` — jawaban per soal per user
- `PackageQuestion.ElemenTeknis` — tag soal
- `AssessmentSession.ElapsedSeconds + ExamActivityLog(page_nav)` — data waktu
- `SessionElemenTeknisScore` — breakdown per ET sudah terhitung
- `AssessmentSession.LinkedGroupId` — Pre-Post linking

**ViewModel BARU yang dibutuhkan:**

```csharp
// Item Analysis per soal
public class ItemAnalysisReport
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; }
    public string? ElemenTeknis { get; set; }
    public int AttemptCount { get; set; }
    public int CorrectCount { get; set; }
    public double DifficultyIndex { get; set; }   // p = CorrectCount/AttemptCount
    public double DiscriminationIndex { get; set; } // D = (top27% correct - bottom27% correct) / (0.27 * N)
}

// Pre-Post Gain Score
public class PrePostGainScoreRow
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public int PreScore { get; set; }
    public int PostScore { get; set; }
    public int GainScore { get; set; }
    public double GainPercentage { get; set; }
}
```

**Controller yang DIMODIFIKASI — `AssessmentAdminController`:**

- Tambah `ItemAnalysisReport(string title, string category, DateTime scheduleDate)` — query PackageUserResponse + join PackageOption, hitung difficulty dan discrimination index
- Tambah `PrePostGainReport(Guid linkedGroupId)` — query dua sesi via LinkedGroupId, side-by-side + gain
- Tambah `ExportItemAnalysis(...)` — Excel via ClosedXML (pola sama ExportAssessmentResults)

---

### 5. Accessibility (WCAG)

**Tidak ada perubahan model atau controller**, kecuali satu kolom baru:

`AssessmentSession.ExtraTimeMinutes` (nullable int) — HC set saat create/edit, countdown JS menggunakan `DurationMinutes + ExtraTimeMinutes`.

Semua perubahan lain di Views:

- `Views/CMP/StartExam.cshtml`: tambah `aria-label`, `role="radio"`, `aria-checked`, skip-to-content link, keyboard navigation soal (arrow keys di JS)
- `Views/Shared/_Layout.cshtml`: pastikan focus management pada state transisi
- Timer: tambah `aria-live="polite"` region untuk screen reader
- Font size control: JavaScript toggle class di `<body>`, persisten ke localStorage

---

## Komponen Baru vs Komponen Dimodifikasi

### Komponen BARU (belum ada)

| Komponen | Tipe | Keterangan |
|----------|------|------------|
| `AssessmentSession.AssessmentType` | Kolom DB | "Standard" / "PrePostTest" / "Interview" |
| `AssessmentSession.LinkedGroupId` | Kolom DB | GUID penghubung Pre-Post |
| `AssessmentSession.LinkedSessionId` | Kolom DB | FK Pre ↔ Post |
| `AssessmentSession.AssessmentPhase` | Kolom DB | "Pre" / "Post" / null |
| `AssessmentSession.ExtraTimeMinutes` | Kolom DB | Akomodasi aksesibilitas |
| `PackageQuestion.QuestionType` | Kolom DB | Enum tipe soal sebagai string |
| `PackageUserResponse.TextAnswer` | Kolom DB | Jawaban teks Essay/Fill |
| `CMPController.PrePostComparison` | Action | Side-by-side comparison + gain score worker |
| `AssessmentAdminController.GradeEssay` | Action | HC grading manual Essay |
| `AssessmentAdminController.ItemAnalysisReport` | Action | Laporan item analysis |
| `AssessmentAdminController.PrePostGainReport` | Action | Laporan gain score |
| `AssessmentAdminController.ExportItemAnalysis` | Action | Export Excel item analysis |
| `ItemAnalysisReport` ViewModel | ViewModel | Difficulty + discrimination index |
| `PrePostGainScoreRow` ViewModel | ViewModel | Pre score, Post score, gain |
| `Views/CMP/PrePostComparison.cshtml` | View | Halaman comparison worker |
| `Views/Admin/ItemAnalysisReport.cshtml` | View | Halaman item analysis admin |
| `Views/Admin/PrePostGainReport.cshtml` | View | Halaman gain score admin |
| `wwwroot/js/exam-mobile.js` | JS | Swipe + bottom nav mobile |
| `SaveMultipleAnswer` AJAX endpoint | Action | Bulk save untuk MultipleAnswer |

### Komponen DIMODIFIKASI (sudah ada, perlu update)

| Komponen | Perubahan | Dampak |
|----------|-----------|--------|
| `AssessmentSession` | +5 kolom baru | Migration EF Core, semua nullable — backward-compatible |
| `PackageQuestion` | +QuestionType | Migration EF Core, default "MultipleChoice" — backward-compatible |
| `PackageUserResponse` | +TextAnswer | Migration EF Core, nullable — backward-compatible |
| `AssessmentAdminController.CreateAssessment` | +AssessmentType logic, Pre-Post creation | ~100 baris tambahan |
| `AssessmentAdminController.ResetAssessment` | +cascade ke Post session | ~20 baris tambahan |
| `AssessmentAdminController.AssessmentMonitoring` | +grouping Pre-Post | Query + view perlu redesign |
| `AssessmentAdminController.ImportPackageQuestions` | +parsing QuestionType | Template Excel berubah |
| `CMPController.Assessment` | +badge Pre/Post, status gating | View update |
| `CMPController.StartExam` | +render per QuestionType, +validasi urutan Pre-Post | ~150 baris, view refactor besar |
| `CMPController.SaveAnswer` | +routing ke TextAnswer vs OptionId | Logic fork |
| `CMPController.SubmitExam` | +scoring per QuestionType | Logic cabang per tipe |
| `CMPController.Results` | +link ke PrePostComparison, +Essay pending info | View update minor |
| `Views/CMP/StartExam.cshtml` | Mobile layout, accessibility, tipe soal baru | Refactor besar |
| Template Excel import soal | +kolom QuestionType | Download template baru |

---

## Urutan Build yang Disarankan

Berdasarkan dependensi — fitur di bawah tidak bisa dibuat sebelum fitur di atas selesai.

### Fase 1 — Data Foundation (Tanpa Breaking Changes)
**Tujuan:** Semua migrasi DB selesai dulu; tidak ada logika baru.

1. Tambah kolom ke `AssessmentSession`: `AssessmentType`, `LinkedGroupId`, `LinkedSessionId`, `AssessmentPhase`, `ExtraTimeMinutes`
2. Tambah `QuestionType` ke `PackageQuestion` (default "MultipleChoice")
3. Tambah `TextAnswer` ke `PackageUserResponse`
4. Buat EF Core migration tunggal; verifikasi semua default backward-compatible
5. Verifikasi sistem existing masih berjalan normal (zero regression test)

**Dependensi:** Tidak ada. Mulai di sini.

---

### Fase 2 — Assessment Type + Pre-Post Test (Admin Side)
**Tujuan:** HC bisa membuat dan mengelola assessment PrePostTest.

1. Update `CreateAssessment` form — tambah dropdown `AssessmentType`
2. Implement logika create PrePostTest: buat 2 `AssessmentSession` record, generate `LinkedGroupId`, set `LinkedSessionId` silang
3. Update `EditAssessment` — batasi edit phase untuk PrePostTest
4. Update `ResetAssessment` — cascade reset ke pasangan via `LinkedSessionId`
5. Update `AssessmentMonitoring` — grup Pre+Post jadi 1 baris expandable
6. Update `DeleteAssessment` — cascade ke pasangan

**Dependensi:** Fase 1. Bisa paralel dengan Fase 3.

---

### Fase 3 — Question Types (Admin + Worker)
**Tujuan:** HC bisa buat soal tipe baru; worker bisa mengerjakan tipe baru.

1. Update template Excel import soal — tambah kolom `QuestionType`
2. Update `ImportPackageQuestions` — parse dan validasi `QuestionType`
3. Update `PreviewPackage` — render UI per tipe soal
4. Update `StartExam` view — render berbeda per QuestionType
5. Update `SaveAnswer` / tambah `SaveMultipleAnswer` (dengan transaksi)
6. Update `SubmitExam` — scoring per QuestionType
7. Tambah `GradeEssay` action

**Dependensi:** Fase 1. Bisa paralel dengan Fase 2.

---

### Fase 4 — Worker Flow Pre-Post + Comparison
**Tujuan:** Worker bisa mengerjakan Pre-Post dan melihat comparison.

1. Update `CMPController.Assessment` list — 2 card dengan badge dan status gating
2. Update `CMPController.StartExam` — validasi urutan (Post hanya setelah Pre Completed)
3. Buat `CMPController.PrePostComparison` action + view
4. Update `CMPController.Results` — tambah link ke PrePostComparison

**Dependensi:** Fase 2.

---

### Fase 5 — Mobile Optimization
**Tujuan:** Exam UI optimal di mobile.

1. Refactor `Views/CMP/StartExam.cshtml` layout — bottom navigation bar
2. Buat `wwwroot/js/exam-mobile.js` — swipe detection, bottom nav logic
3. Tambah/update CSS mobile — media queries, touch targets min 44px
4. Test di mobile viewport

**Dependensi:** Fase 3 selesai (StartExam view sudah stabil setelah Fase 3 agar tidak conflict besar).

---

### Fase 6 — Advanced Reporting
**Tujuan:** HC mendapatkan laporan item analysis dan Pre-Post gain score.

1. Buat ViewModel `ItemAnalysisReport`, `PrePostGainScoreRow`
2. Implement `ItemAnalysisReport` action — hitung difficulty index dan discrimination index
3. Implement `PrePostGainReport` action — query via `LinkedGroupId`, hitung gain score
4. Buat views untuk kedua laporan
5. Tambah export Excel

**Dependensi:** Fase 2 (untuk PrePostGainReport), Fase 3 (untuk ItemAnalysis data tersedia).

---

### Fase 7 — Accessibility
**Tujuan:** Exam UI memenuhi WCAG 2.1 AA dasar.

1. Tambah skip-to-content link di `StartExam.cshtml`
2. Update ARIA attributes di soal radio/checkbox
3. Tambah `aria-live` region untuk timer
4. Implement keyboard navigation soal (arrow keys via JS)
5. Font size control — JS toggle + localStorage
6. Implement `ExtraTimeMinutes` — update countdown JS

**Dependensi:** Fase 3 dan Fase 5 (StartExam view sudah final). Aksesibilitas adalah polesan terakhir.

---

## Diagram Dependensi Antar Fase

```
Fase 1 (Migration DB)
  ├── Fase 2 (Admin PrePostTest)
  │     └── Fase 4 (Worker PrePostTest)
  │           └── Fase 6 (Reporting) ← juga bergantung Fase 3
  └── Fase 3 (Question Types)
        ├── Fase 5 (Mobile)
        ├── Fase 6 (Reporting)
        └── Fase 7 (Accessibility) ← setelah Fase 3 dan 5
```

Fase 2 dan 3 bisa dikerjakan paralel (area kode berbeda). Fase 4, 5, 6, 7 harus menunggu prasyaratnya.

---

## Anti-Pattern yang Harus Dihindari

### Anti-Pattern 1: Tipe Soal sebagai Tabel Master Terpisah
**Apa yang salah:** Membuat tabel `QuestionType` dengan FK dari `PackageQuestion`.
**Sebaiknya:** Enum sebagai string column — 5 tipe sudah diketahui dan stabil.

### Anti-Pattern 2: Satu AssessmentSession untuk Pre+Post
**Apa yang salah:** Menyimpan Pre dan Post dalam satu session dengan flag.
**Kenapa buruk:** Scoring engine, certificate logic, SignalR batchKey, ExamActivityLog semuanya berbasis SessionId tunggal — refactor masif.
**Sudah diputuskan:** 2 session terpisah linked via `LinkedGroupId` — keputusan yang benar.

### Anti-Pattern 3: Auto-grade Essay di SubmitExam
**Apa yang salah:** Mencoba menghitung skor Essay otomatis di `SubmitExam`.
**Sebaiknya:** Essay skor = 0 saat submit, `IsPassed` = null sampai HC menyelesaikan grading via `GradeEssay`.

### Anti-Pattern 4: Logika Comparison di Action Results
**Apa yang salah:** Menumpuk Pre-Post comparison langsung di `Results` action yang sudah ~200 baris.
**Sebaiknya:** Action terpisah `PrePostComparison` dengan ViewModel tersendiri.

### Anti-Pattern 5: SaveMultipleAnswer Tanpa Transaksi
**Apa yang salah:** DELETE jawaban lama + INSERT baru tanpa transaksi database.
**Akibat:** Jika INSERT gagal setelah DELETE, jawaban hilang permanen.
**Sebaiknya:** Bungkus dalam `using var transaction = await _context.Database.BeginTransactionAsync()`.

---

## Pertimbangan Teknis Tambahan

| Concern | Detail |
|---------|--------|
| Index DB untuk LinkedGroupId | Tambah index pada `AssessmentSession.LinkedGroupId` — dipakai di query Monitoring dan GainReport |
| TextAnswer column type | Pastikan tidak ada `MaxLength` terlalu pendek di Essay — gunakan minimal 2000 karakter atau `nvarchar(max)` |
| Item Analysis performa | Query bisa berat untuk batch besar (1000+ responses) — ok untuk batch normal KPB; monitor jika skala bertambah |
| Template Excel import soal | Kolom baru `QuestionType` harus backward-compatible dengan template lama — beri default "MultipleChoice" jika kolom kosong |
| IsPassed untuk Essay | Session dengan soal Essay murni: `IsPassed` null sampai semua Essay di-grade; campuran MC+Essay: hitung skor MC dulu, Essay menyusul |

---

## Sources

- Kode sumber dibaca langsung: `AssessmentSession.cs`, `AssessmentPackage.cs`, `PackageUserResponse.cs`, `UserPackageAssignment.cs`, `AssessmentResultsViewModel.cs`, `ExamActivityLog.cs`, `SessionElemenTeknisScore.cs`, `AssessmentHub.cs`
- Action signatures dari: `AssessmentAdminController.cs`, `CMPController.cs` (grep langsung)
- Keputusan desain dari: `.planning/PROJECT.md` (v14.0 milestone section)
- Confidence: HIGH — semua temuan berdasarkan kode aktual
