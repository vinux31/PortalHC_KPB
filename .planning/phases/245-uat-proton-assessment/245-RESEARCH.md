# Phase 245: UAT Proton Assessment - Research

**Researched:** 2026-03-24
**Domain:** UAT — Validasi alur Assessment Proton Tahun 1/2 (online exam) dan Tahun 3 (interview) end-to-end
**Confidence:** HIGH

## Summary

Phase 245 adalah fase UAT murni — tidak ada implementasi fitur baru. Semua kode sudah ada; tugas planner adalah menyusun checklist code review + human browser test yang memverifikasi alur Assessment Proton Tahun 1/2 (ujian online) dan Tahun 3 (interview) berjalan benar dari awal hingga sertifikat Proton dapat diakses worker.

Ada satu temuan kritis dari inspeksi kode: seed data di `SeedData.cs` membuat Tahun 3 dengan `DurationMinutes = 120`, bukan 0. Ini tidak memengaruhi assessment yang dibuat via form (controller meng-override ke 0 di line 1481), tetapi session Tahun 3 yang di-seed langsung ke DB punya `DurationMinutes = 120`. Detection di `SubmitInterviewResults` menggunakan `TahunKe == "Tahun 3"` (bukan DurationMinutes), sehingga flow masih berjalan — namun ini adalah inkonsistensi yang perlu dicatat.

Alur Tahun 3 interview adalah jalur yang paling kritis karena belum pernah di-UAT sebelumnya. Idempotency guard sudah ada di line ~2537 (`AnyAsync` check sebelum add `ProtonFinalAssessment`). Akses sertifikat Proton dari sisi worker tersedia via `CDPController.HistoriProton` dan `HistoriProtonDetail`.

**Primary recommendation:** Gunakan pola UAT Phase 242-244 — code review semua PROT-01 s/d PROT-04, flag item yang butuh interaksi UI untuk human verification, prioritaskan alur Tahun 3 karena belum pernah divalidasi.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Code review + browser walkthrough — meskipun exam flow sama dengan reguler (Phase 243), tetap verifikasi di browser untuk Proton-specific behavior (track selection, category "Assessment Proton")
- **D-02:** Pakai seed data yang sudah disiapkan Phase 241 (assessment Proton Tahun 1 untuk Rino) — tidak perlu buat assessment baru dari nol
- **D-03:** Code review + browser walkthrough — flow interview belum pernah di-UAT sebelumnya
- **D-04:** 4 skenario browser test Tahun 3:
  1. HC input lulus (5 aspek, judges, notes, IsPassed=true) → verifikasi ProtonFinalAssessment auto-created
  2. HC input gagal (IsPassed=false) → verifikasi TIDAK ada ProtonFinalAssessment
  3. Upload supporting document → verifikasi file tersimpan di /uploads/interviews/
  4. Edit hasil interview yang sudah di-submit → verifikasi data terupdate
- **D-05:** Verifikasi 3 item:
  1. ProtonFinalAssessment record dibuat otomatis dengan data benar (CoacheeId, ProtonTrackAssignmentId, Status, CompetencyLevel)
  2. Peserta (worker) bisa mengakses/download sertifikat Proton
  3. Idempotency guard — submit ulang tidak buat duplicate ProtonFinalAssessment
- **D-06:** Pattern sama seperti Phase 242-244: Claude code review semua PROT-01 s/d PROT-04, item yang butuh interaksi UI di-flag untuk human verification di browser

### Claude's Discretion

- Urutan code review items
- Detail checklist untuk human verification items
- Pengelompokan items per plan

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PROT-01 | Admin dapat membuat assessment Proton Tahun 1/2 (online exam) dengan track selection dan flow ujian berjalan normal | CreateAssessment di AdminController.cs line ~1232 mendeteksi Category="Assessment Proton" + ProtonTrackId; seed SEED-05 sudah ada |
| PROT-02 | Admin dapat membuat assessment Proton Tahun 3 (interview, durasi=0) tanpa paket soal | Server override DurationMinutes=0 di line 1481 ketika TahunKe="Tahun 3"; seed SEED-06 sudah ada |
| PROT-03 | HC dapat input hasil interview Tahun 3 (5 aspek penilaian skor 1-5, judges, catatan, IsPassed manual) | SubmitInterviewResults action di AdminController.cs line ~2446; form ada di AssessmentMonitoringDetail.cshtml line ~434 |
| PROT-04 | ProtonFinalAssessment auto-created saat interview Tahun 3 lulus, sertifikat di-generate | Logic di AdminController.cs line 2528-2553; akses dari worker via CDPController.HistoriProton |

</phase_requirements>

---

## Standard Stack

Ini fase UAT — tidak ada library baru. Stack yang dipakai adalah stack existing proyek:

| Layer | Teknologi | Catatan |
|-------|-----------|---------|
| Backend | ASP.NET Core (C#), AdminController, CDPController | Sudah ada |
| ORM | Entity Framework Core | `_context.ProtonFinalAssessments`, `AssessmentSessions` |
| Auth | ASP.NET Core Identity | `[Authorize(Roles = "Admin, HC")]` di SubmitInterviewResults |
| View | Razor (.cshtml) | AssessmentMonitoringDetail.cshtml — form interview sudah ada |
| File Upload | IFormFile + wwwroot/uploads/interviews/ | Sudah diimplementasi |

## Architecture Patterns

### Alur PROT-01 (Tahun 1 — Online Exam)

```
Admin → CreateAssessment (Category="Assessment Proton", ProtonTrackId=X, DurationMinutes>0)
      → AssessmentSession.TahunKe = "Tahun 1"
      → Worker → StartExam (via CMPController — sama dengan reguler)
      → SubmitExam → grading → sertifikat (GenerateCertificate=false untuk Proton)
```

Deteksi di `CreateAssessment POST` (line ~1232):
```csharp
bool isProtonYear3Check = model.Category == "Assessment Proton" && model.ProtonTrackId.HasValue;
// Tahun 3 detection menggunakan DurationMinutes == 0 sentinel dari form
```

### Alur PROT-02 (Tahun 3 — Interview, Durasi=0)

```
Admin → CreateAssessment (Category="Assessment Proton", ProtonTrackId=X, DurationMinutes=0)
      → Server override: session.DurationMinutes = 0 (line 1481)
      → AssessmentSession dibuat tanpa paket soal
      → Session.Status = "Open"
```

### Alur PROT-03 (HC Input Interview)

```
HC → AssessmentMonitoringDetail → isProtonInterview = true (TahunKe="Tahun 3")
   → Form dengan 5 aspek (dropdown 1-5), judges (text), notes (textarea), supportingDoc (file)
   → POST SubmitInterviewResults
   → session.InterviewResultsJson diupdate
   → session.IsPassed = isPassed, Status = "Completed"
```

Form field keys untuk aspek (dipakai controller dan view secara konsisten):
- `aspect_Pengetahuan_Teknis`
- `aspect_Kemampuan_Operasional`
- `aspect_Keselamatan_Kerja`
- `aspect_Komunikasi_and_Kerjasama`
- `aspect_Sikap_Profesional`

### Alur PROT-04 (ProtonFinalAssessment Auto-Create + Sertifikat)

```
SubmitInterviewResults (isPassed=true)
  → Cari ProtonTrackAssignment aktif (CoacheeId + ProtonTrackId + IsActive)
  → Idempotency check: AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id)
  → Add ProtonFinalAssessment {CoacheeId, ProtonTrackAssignmentId, Status="Completed",
     CompetencyLevelGranted=0, Notes="Interview Tahun 3 lulus. Assessor: {judges}"}
  → SaveChangesAsync

Worker akses sertifikat:
  → CDPController.HistoriProton (line ~2910)
  → CDPController.HistoriProtonDetail (line ~3234)
  → ProtonFinalAssessments query by CoacheeId
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead |
|---------|-------------|-------------|
| Upload file | Custom file handler | IFormFile sudah ada di SubmitInterviewResults |
| Idempotency | Custom lock | AnyAsync guard sudah ada di line ~2537 |
| Form deserialization | Manual parsing | InterviewResultsDto sudah di-serialize/deserialize via System.Text.Json |

## Common Pitfalls

### Pitfall 1: Seed Data Tahun 3 — DurationMinutes Tidak 0

**Apa yang terjadi:** `SeedData.cs` line ~820 membuat Tahun 3 dengan `DurationMinutes = 120`. Session ini langsung di-insert ke DB tanpa melalui controller yang meng-override ke 0.

**Dampak:** Session Tahun 3 seeded punya `DurationMinutes = 120` di DB. Ini tidak memblokir `SubmitInterviewResults` karena detection menggunakan `TahunKe == "Tahun 3"` (bukan DurationMinutes). Namun PROT-02 mensyaratkan "durasi=0" — secara teknis DB tidak konsisten dengan spec.

**Rekomendasi:** Flag sebagai item yang perlu diverifikasi. Jika dianggap bug, perbaiki seed agar `DurationMinutes = 0`.

**Konfirmasi:** `SubmitInterviewResults` check di line 2462 menggunakan `session.TahunKe != "Tahun 3"` — bukan DurationMinutes — sehingga flow tidak terblokir.

### Pitfall 2: ProtonTrackAssignment Tidak Aktif

**Apa yang terjadi:** Auto-create `ProtonFinalAssessment` membutuhkan `ProtonTrackAssignment` aktif (`IsActive = true`) untuk Rino + ProtonTrackId yang sama. Jika mapping tidak ada atau `IsActive = false`, `ProtonFinalAssessment` TIDAK akan dibuat meskipun `IsPassed = true`.

**Verifikasi:** Pastikan seed Phase 241 sudah membuat `ProtonTrackAssignment` aktif untuk Rino sebelum menjalankan UAT Tahun 3.

**Code:** `AdminController.cs` line 2530-2533.

### Pitfall 3: GroupTahunKe ViewBag Tidak Di-Set

**Apa yang terjadi:** `isProtonInterview` di view diset berdasarkan `ViewBag.GroupTahunKe as string == "Tahun 3"`. Jika ViewBag ini tidak di-set oleh action `AssessmentMonitoringDetail`, form interview tidak akan muncul.

**Verifikasi saat code review:** Cek action `AssessmentMonitoringDetail` di AdminController apakah `ViewBag.GroupTahunKe` di-populate.

### Pitfall 4: Session Tahun 3 Status Harus "Open" untuk Input HC

**Apa yang terjadi:** Jika session Tahun 3 sudah `Completed` (dari submit sebelumnya), form interview masih muncul dan edit bisa dilakukan karena controller tidak mengecek status saat POST. Ini adalah behavior yang diinginkan (D-04 skenario 4 — edit hasil). Namun perlu dikonfirmasi bahwa idempotency guard tidak memblokir update `InterviewResultsJson` (karena itu hanya mengecek `ProtonFinalAssessment`, bukan apakah session sudah Completed).

## Code Examples

### Detection Tahun 3 di SubmitInterviewResults

```csharp
// Source: Controllers/AdminController.cs line ~2462
if (session.Category != "Assessment Proton" || session.TahunKe != "Tahun 3")
{
    TempData["Error"] = "Aksi ini hanya untuk Assessment Proton Tahun 3.";
    return RedirectToAction("ManageAssessment");
}
```

### Idempotency Guard ProtonFinalAssessment

```csharp
// Source: Controllers/AdminController.cs line ~2537-2553
var alreadyExists = await _context.ProtonFinalAssessments
    .AnyAsync(fa => fa.ProtonTrackAssignmentId == assignment.Id);
if (!alreadyExists)
{
    _context.ProtonFinalAssessments.Add(new ProtonFinalAssessment
    {
        CoacheeId = session.UserId,
        ProtonTrackAssignmentId = assignment.Id,
        Status = "Completed",
        CompetencyLevelGranted = 0,
        Notes = $"Interview Tahun 3 lulus. Assessor: {dto.Judges}",
        CreatedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow
    });
}
```

### View Detection Interview Mode

```csharp
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml line ~26
bool isProtonInterview = Model.Category == "Assessment Proton"
    && (ViewBag.GroupTahunKe as string) == "Tahun 3";
```

### Akses ProtonFinalAssessment dari Worker (CDP)

```csharp
// Source: Controllers/CDPController.cs line ~376
var finalAssessment = await _context.ProtonFinalAssessments
    .FirstOrDefaultAsync(fa => fa.ProtonTrackAssignmentId == activeAssignmentId);
```

## Validation Architecture

> Fase ini adalah UAT manual — tidak ada automated test. Nyquist validation tidak berlaku.

Berdasarkan `.planning/REQUIREMENTS.md` catatan Out of Scope: "Automated browser testing — UAT dilakukan manual via browser, bukan Playwright/Selenium."

**Pola validasi Phase 245:**

| Tahap | Metode | Pelaksana |
|-------|--------|-----------|
| Code review PROT-01 s/d PROT-04 | Inspeksi kode oleh Claude | Claude |
| Browser test Tahun 1 exam flow | Manual walkthrough | Human |
| Browser test Tahun 3 skenario lulus | Manual, 4 skenario D-04 | Human |
| Verifikasi ProtonFinalAssessment DB | Manual check | Human |
| Verifikasi akses sertifikat worker | Manual via CDP | Human |

## Environment Availability

Step 2.6: SKIPPED (fase UAT menggunakan codebase dan DB existing, tidak ada external dependencies baru)

Prasyarat yang harus tersedia sebelum UAT:
- App berjalan di Development mode
- Seed data Phase 241 sudah dieksekusi (ProtonTrack, ProtonTrackAssignment, AssessmentSession Tahun 1 + Tahun 3 untuk Rino)
- Akun HC dan Admin aktif untuk login

## Open Questions

1. **ViewBag.GroupTahunKe — Apakah di-set di controller?**
   - Yang diketahui: View menggunakannya untuk menampilkan form interview
   - Yang belum dikonfirmasi: Action `AssessmentMonitoringDetail` di AdminController — apakah populate ViewBag ini
   - Rekomendasi: Verifikasi di code review Plan pertama

2. **Seed Tahun 3 DurationMinutes = 120 vs 0**
   - Yang diketahui: Seed memasukkan 120, spec PROT-02 mensyaratkan 0, detection di controller menggunakan TahunKe bukan DurationMinutes
   - Yang belum dikonfirmasi: Apakah ini dianggap bug atau acceptable inkonsistensi
   - Rekomendasi: Flag dan putuskan saat code review

3. **ProtonFinalAssessment → Sertifikat: apakah ada CertificatePdf generation untuk Proton?**
   - Yang diketahui: `GenerateCertificate = false` pada seed Tahun 1 dan Tahun 3
   - Yang belum dikonfirmasi: Apakah "sertifikat Proton" yang dimaksud PROT-04 adalah ProtonFinalAssessment record itu sendiri (bukan PDF), atau ada PDF generation terpisah
   - Rekomendasi: Verifikasi di code review — cek CDPController HistoriProtonDetail view untuk konfirmasi apa yang ditampilkan ke worker

## Sources

### Primary (HIGH confidence)

- `Controllers/AdminController.cs` line 1232, 1480, 2442-2579 — CreateAssessment Proton detection, SubmitInterviewResults, ProtonFinalAssessment auto-create
- `Views/Admin/AssessmentMonitoringDetail.cshtml` line 26, 434-540 — isProtonInterview detection, form interview
- `Data/SeedData.cs` line 780-842 — SeedProtonAssessmentsAsync
- `Models/ProtonModels.cs` line 207-226 — ProtonFinalAssessment entity
- `Controllers/CDPController.cs` line 376, 510, 2910, 3234 — ProtonFinalAssessment queries, HistoriProton
- `.planning/phases/245-uat-proton-assessment/245-CONTEXT.md` — Locked decisions D-01 s/d D-06

### Secondary (MEDIUM confidence)

- `.planning/REQUIREMENTS.md` — PROT-01 s/d PROT-04 definisi requirement
- Pola UAT Phase 242-244 — digunakan sebagai template struktur plan

## Metadata

**Confidence breakdown:**
- Kode existing (scope UAT): HIGH — semua file dibaca langsung
- Seed data status: HIGH — SeedData.cs dibaca langsung
- Inkonsistensi DurationMinutes seed: HIGH — ditemukan dari inspeksi kode
- Akses sertifikat worker (apa yang ditampilkan): MEDIUM — CDPController dibaca tapi view HistoriProtonDetail belum dibaca penuh

**Research date:** 2026-03-24
**Valid until:** Berlaku selama tidak ada perubahan kode pada AdminController.cs, SeedData.cs, atau AssessmentMonitoringDetail.cshtml
