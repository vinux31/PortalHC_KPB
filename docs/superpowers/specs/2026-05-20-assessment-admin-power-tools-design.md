# Milestone v3.18 — Assessment Admin Power Tools

**Status**: Design (pending implementation plan)
**Tanggal**: 2026-05-20
**Penulis**: Brainstorm sesi Claude + Rino
**Estimasi**: 2 phase, kompleksitas medium–tinggi

---

## 1. Tujuan & Ringkasan

Lengkapi tooling admin assessment dengan dua kemampuan:

1. **Phase 1 — Export Per-Peserta**: extend `ExportAssessmentResults` agar file Excel berisi 1 sheet summary + N sheet per peserta (data per peserta lengkap dengan radar/spider chart).
2. **Phase 2 — Edit Jawaban Peserta**: halaman khusus admin/HC untuk mengubah jawaban MC/MA peserta Completed dengan recompute otomatis (Score, IsPassed, ElemenTeknis, sertifikat, TrainingRecord) dan audit trail granular.

**Deferred** ke milestone berikutnya: Fix Kunci Soal Global (cascade re-grade semua session yang pakai soal X). Diaktifkan bila kasus muncul.

---

## 2. Konteks & Asumsi yang Sudah Diverifikasi

- `ExportAssessmentResults` existing di `Controllers/AssessmentAdminController.cs:3651` — generate 1 sheet "Results" dengan tabel ringkas (Name/NIP/Jumlah Soal/Status/Score/Result/Completed At).
- Spider chart per peserta sudah ada di view `Views/CMP/Results.cshtml` pakai Chart.js radar — data sumber `ElemenTeknisScores`.
- Tabel `SessionElemenTeknisScores` sudah persisted setelah grading (Models/SessionElemenTeknisScore.cs). Field: `ElemenTeknis`, `CorrectCount`, `QuestionCount`.
- Tabel jawaban peserta = `PackageUserResponses` (bukan `AssessmentAttemptHistory`). Field: `AssessmentSessionId`, `PackageQuestionId`, `PackageOptionId`, `TextAnswer`, `EssayScore`.
- `Services/GradingService.cs::GradeAndCompleteAsync` lakukan: compute score, insert `SessionElemenTeknisScores`, update session, set `IsCompleted` di `UserPackageAssignment`, insert `TrainingRecord` (skip Pre), generate `NomorSertifikat`, notify group completion. **Status guard line 195** (WHERE Status != Completed/PendingGrading) blokir pemanggilan ulang untuk session Completed.
- `AuditLog` (generic) + `ExamActivityLog` (per-session timeline) sudah ada. `ExamActivityLog` tidak menyimpan actor — Phase 2 perlu tabel dedicated.
- `AssessmentSession.UpdatedAt` ada dan bisa dipakai sebagai concurrency token (tanpa rowversion).
- Lib yang ada di `HcPortal.csproj`: `ClosedXML 0.105.0`, `QuestPDF 2026.2.2`. **`SkiaSharp` belum ada — perlu ditambahkan untuk Phase 1.**
- SignalR group monitoring = `monitor-{batchKey}`. Existing signal: `progressUpdate`, `workerStarted`, `workerSubmitted`. Cache key per session: `exam-status-{id}`.
- Pre/Post linking via `LinkedSessionId` + `SamePackage`. Data `PackageUserResponses` tetap terpisah per session — edit Pre tidak otomatis cascade ke Post.
- Tidak ada project test (`Tests/`) — manual UAT only.

---

## 3. Phase 1 — Export Per-Peserta

### 3.1 Output Structure

```
Workbook
├── Sheet "Summary"  (rename dari "Results", breaking change accepted)
└── Sheet "[NIP]_[NamaPeserta]" × N  (Completed + Abandoned only)
```

Filter peserta yang dapat sheet: `Status ∈ { "Completed", "Abandoned" }`. Skip `InProgress`, `Not Started`, `Cancelled`.

Sheet name sanitization:
- Excel limit: 31 karakter, exclude `\ / ? * [ ] :`
- Format: `{NIP}_{FullName}` — NIP di depan agar collision-free (NIP unique per worker). Truncate FullName dari belakang kalau total > 31 char.
- Collision guard: append `(2)`, `(3)`, … (fallback, harusnya tidak terpicu karena NIP unique)

### 3.2 Isi Sheet Peserta — 2 Variant

**Variant A — Online assessment (`IsManualEntry == false`)**

```
Header:
  Nama Lengkap (NIP)
  Started At      | timestamp (dd MMM yyyy HH:mm)
  Completed At    | timestamp atau "—" (untuk Abandoned)
  Durasi Aktual   | (ElapsedSeconds / 60) menit  ← bukan DurationMinutes (itu batas waktu)
  Tipe Assessment | PreTest / PostTest / —

Section 1: Analisis Elemen Teknis  (skip kalau SessionElemenTeknisScores kosong)
  Tabel | Elemen Teknis | Benar | Total | Persentase |
  PNG Spider Chart embedded  (skip kalau jumlah elemen < 3)

Section 2: Detail Jawaban  (MC + MA only)
  Tabel | No | Soal | Tipe | Jawaban Peserta | Jawaban Benar | Status (✓/✗) |
  - MC row: 1 baris, kolom Jawaban = single option
  - MA row: 1 baris, kolom Jawaban = comma-separated selected options
  - Essay row: skip dengan note "Essay – manual grading (lihat Penilaian Essay)"
  - Soal tanpa `PackageUserResponses` (Abandoned skip soal): kolom "Jawaban Peserta" = `"Tidak dijawab"`, Status = ✗
```

**Variant B — Manual entry assessment (`IsManualEntry == true`)**

```
Header (sama)

Section: Info Sertifikasi Manual
  | Penyelenggara | Kota | Sub Kategori | Tipe Sertifikat |
  | Link Sertifikat | hyperlink ManualSertifikatUrl |

Skip: Elemen Teknis, Spider Chart, Detail Jawaban
```

### 3.3 Sumber Data

- ElemenTeknis: query `SessionElemenTeknisScores` (no recompute, sudah persisted)
- Jawaban per soal: query `PackageUserResponses` join `PackageQuestions` + `PackageOptions`
- Durasi: `AssessmentSession.ElapsedSeconds`
- Tipe: `AssessmentSession.AssessmentType`
- Manual fields: `AssessmentSession.Penyelenggara/Kota/SubKategori/CertificateType/ManualSertifikatUrl`

### 3.4 Spider Chart Rendering

- Library baru: `SkiaSharp` (MIT license, tambah `PackageReference` ke `HcPortal.csproj`)
- Render: 500×500 px PNG, radar style:
  - Grid radial 0/25/50/75/100
  - Label tiap elemen di tepi (truncate >20 char dengan ellipsis, konsisten dengan `Results.cshtml:274`)
  - Polygon fill semi-transparan
- Embed: `worksheet.AddPicture(stream).MoveTo(targetCell)`
- Skip kondisi: ElemenTeknis kosong, atau jumlah elemen < 3

### 3.5 Performance

- PNG generate parallel via `Task.WhenAll` (CPU-bound, OK untuk pool default)
- Estimate: 5–15 detik response time untuk 50 peserta
- File size estimate: 3–5 MB per 50 peserta
- Streaming response tidak dipakai — Excel butuh full file

### 3.6 Permission

`[Authorize(Roles = "Admin, HC")]`. HC = full sama dengan Admin (tidak ada area restriction).

### 3.7 Breaking Change

Nama sheet utama berubah dari `"Results"` → `"Summary"`. Dokumentasikan di release notes.

---

## 4. Phase 2 — Edit Jawaban Peserta

### 4.1 Scope

- **Yang di-edit**: jawaban `MultipleChoice` + `MultipleAnswer` (auto-graded).
- **Yang skip**: Essay (sudah ada UI Penilaian Essay existing dari Phase 298-05).
- **Status yang eligible**: `Completed` only.
- **Tipe session yang skip**: `IsManualEntry == true`, Assessment Proton Tahun 3 (interview manual).

### 4.2 Helper Eligibility

```csharp
bool IsEditable(AssessmentSession s)
{
    return s.Status == "Completed"
        && !s.IsManualEntry
        && !(s.Category == "Assessment Proton" && s.TahunKe == "Tahun 3")
        && /* UserPackageAssignment exists for s.Id */;
}
```

Dipakai di:
- GET `/AssessmentAdmin/EditPesertaAnswers/{id}` — block kalau false
- POST `/AssessmentAdmin/SubmitEditAnswers` — double check sebelum save
- View `AssessmentMonitoringDetail.cshtml` — hide item dropdown kalau false

### 4.3 UI — Dropdown Aksi di Per-User Table

Hybrid layout: `View Results` (primary) + `Activity Log` (icon) tetap inline. Sisanya pindah dropdown ⋮:

```
| Name | Progress | Status | Score | Result | Completed At | Actions                          |
| ...  | ...      | ...    | ...   | ...    | ...          | [View Results] [🕐] [⋮ ▼]        |
                                                                              ├─ ✏️ Edit Jawaban
                                                                              ├─ 🔄 Reset
                                                                              ├─ ❌ Akhiri Ujian
                                                                              └─ 🔀 Reshuffle
```

Conditional render dropdown item per status (sesuai logic existing):
- `Edit Jawaban` → hanya kalau `IsEditable(session)` true
- `Reset` → semua kecuali `Cancelled`
- `Akhiri Ujian` → `InProgress` only
- `Reshuffle` → Package mode + `Not started`/`Abandoned`

A11y: ARIA `aria-label="Aksi lain untuk {nama}"`, keyboard Tab/Enter/Esc.
Mobile: Bootstrap `dropdown-menu-end` + auto-flip.

### 4.4 Edit Jawaban Page

Route: `GET /AssessmentAdmin/EditPesertaAnswers/{sessionId}`

Layout:

```
← Back to Monitoring

Edit Jawaban — {FullName} (NIP {NIP})

Info session:
  Title: {Title} | Kategori: {Category} | Schedule: {Schedule}
  Skor saat ini: {Score}%  | Status: {Pass/Fail}

⚠️ Notice:
  Edit jawaban akan recompute skor + spider otomatis.
  Aksi ini di-log audit. Hasil tidak ditampilkan ke peserta.

[Hidden field: UpdatedAt = {session.UpdatedAt}]  (concurrency token)
[Anti-forgery token]

Per soal (MC + MA):
  Soal {N} ({Tipe}) — {QuestionText}
    Saat ini: {opsi peserta sekarang}
    Jawaban benar: {correct options}
    Status: {✓ Benar / ✗ Salah}
    [Pilih jawaban baru: dropdown (MC) atau checkbox group (MA)]
    Alasan: [dropdown: SoalSalah / KunciSalah / BugSistem / PermintaanPeserta / Lainnya]
    Catatan: [textarea, required kalau ReasonCode == "Lainnya"]

Soal Essay: disabled row dengan note "Essay – manual grading via Penilaian Essay"

[Cancel]  [Save & Recompute]
```

Behavior frontend:
- Tiap soal punya state "dirty" (highlighted kalau diubah)
- Reason required hanya untuk soal yang diubah
- Submit kirim diff (soal yang berubah saja)
- Validation client: dirty soal tanpa reason → block + highlight

### 4.5 POST Submit Flow

Route: `POST /AssessmentAdmin/SubmitEditAnswers`

```
1. [Authorize(Roles = "Admin, HC")] + [ValidateAntiForgeryToken]
2. Load session by Id
3. Eligibility check: IsEditable(session) → kalau false: 400 + TempData error + redirect
4. Concurrency check: form.UpdatedAt == session.UpdatedAt
   - Kalau stale: TempData["Error"] = "Sesi sudah diubah admin lain. Refresh." + redirect back
5. Begin transaction: _context.Database.BeginTransactionAsync()
6. Loop tiap soal diubah dalam payload:
   a. Validate `ReasonCode` ∈ {SoalSalah, KunciSalah, BugSistem, PermintaanPeserta, Lainnya}; jika `ReasonCode == "Lainnya"` maka `ReasonText` wajib non-empty (server-side double-check)
   b. Load PackageUserResponse(s) untuk question itu
   c. Capture OldAnswer:
      - MC: PackageOptionId tunggal
      - MA: List<PackageOptionId>
      - Snapshot text via PackageOption.OptionText join
   d. Update PackageUserResponse:
      - MC: update existing row's PackageOptionId
      - MA: delete-all + insert-new (konsisten dengan pattern AssessmentHub.SaveMultipleAnswer)
   e. Capture NewAnswer (same shape)
   f. Insert AssessmentEditLog entry (lihat schema di 4.7)
7. Capture session.Score, session.IsPassed (sebelum recompute) → oldScore/oldIsPassed
8. Call GradingService.RegradeAfterEditAsync(session) → returns (newScore, newIsPassed)
9. Cascade sertifikat + TrainingRecord (lihat 4.8)
10. Insert AuditLog: ActionType="EditAssessmentAnswer", Description="Edit N jawaban session ID=X, score Y→Z"
11. Invalidate cache: _cache.Remove($"exam-status-{session.Id}")
12. Update session.UpdatedAt = DateTime.UtcNow (sudah terjadi di step 8)
13. Commit transaction
14. SignalR broadcast monitor-{batchKey}: SendAsync("workerAnswerEdited", { sessionId, oldScore, newScore, oldIsPassed, newIsPassed })
15. TempData["Success"] = "Edit {N} jawaban berhasil. Score: {Y} → {Z}, {Pass→Pass / Pass→Fail / Fail→Pass / Fail→Fail}"
16. Redirect AssessmentMonitoringDetail
```

Rollback: exception di step 5–13 → transaction rollback + TempData error + log.

### 4.6 Konfirmasi Modal Pass↔Fail Flip

Backend `GET /AssessmentAdmin/PreviewEditScore?sessionId={id}` (dry-run) menerima draft answers, return preview `(newScore, newIsPassed)` tanpa persist. Frontend panggil sebelum submit untuk deteksi flip threshold. Kalau flip terdeteksi, tampilkan modal:

```
⚠️ Edit ini akan ubah hasil: {Pass → Fail}

{Kalau Pass → Fail dan NomorSertifikat != null:}
Sertifikat existing (No: {NomorSertifikat}) akan dicabut.

{Kalau Fail → Pass dan session.GenerateCertificate:}
Sertifikat baru akan diterbitkan otomatis.

[Cancel] [Lanjut]
```

### 4.7 Tabel Baru: `AssessmentEditLog`

Migration: `AddAssessmentEditLogs.cs`.

```csharp
public class AssessmentEditLog
{
    public int Id { get; set; }
    public int AssessmentSessionId { get; set; }
    public int PackageQuestionId { get; set; }

    // Snapshot supaya audit tetap readable kalau soal/option dihapus
    public string QuestionTextSnapshot { get; set; } = "";
    public string OldAnswerJson { get; set; } = "[]";       // List<int> PackageOption.Id (MA = multi)
    public string OldAnswerTextSnapshot { get; set; } = ""; // "A. On Job Training"
    public string NewAnswerJson { get; set; } = "[]";
    public string NewAnswerTextSnapshot { get; set; } = "";

    public int? OldScore { get; set; }
    public int? NewScore { get; set; }
    public bool? OldIsPassed { get; set; }
    public bool? NewIsPassed { get; set; }

    public string ActorUserId { get; set; } = "";
    public string ActorName { get; set; } = "";   // "NIP - FullName"
    public string ActorRole { get; set; } = "";   // "Admin" / "HC"

    public DateTime EditedAt { get; set; } = DateTime.UtcNow;

    public string ReasonCode { get; set; } = "";  // SoalSalah / KunciSalah / BugSistem / PermintaanPeserta / Lainnya
    public string? ReasonText { get; set; }       // free text, required kalau ReasonCode == "Lainnya"
}
```

Index: `IX_AssessmentEditLogs_SessionId_EditedAt` `(AssessmentSessionId, EditedAt DESC)`.

### 4.8 `GradingService.RegradeAfterEditAsync(session)` (Method Baru)

Tujuan: re-grade session yang sudah Completed setelah edit jawaban.

Implementasi:

1. DELETE existing `SessionElemenTeknisScores WHERE AssessmentSessionId == session.Id`
2. Reuse compute logic dari `GradeAndCompleteAsync` step 1–2 (factor out jadi private method `ComputeScoreAndETInternalAsync(session, overrideAnswers?)` yang return `(totalScore, maxScore, isPassed, etScores)` tanpa side-effect)
   - Signature: `ComputeScoreAndETInternalAsync(AssessmentSession session, IDictionary<int, List<int>>? overrideAnswers = null)`
   - `overrideAnswers == null` → baca semua jawaban dari `PackageUserResponses` (path untuk `RegradeAfterEditAsync` setelah DB tertulis)
   - `overrideAnswers != null` → merge: pakai override untuk `PackageQuestionId` yang ada di dict, fallback ke DB untuk soal sisanya. Path untuk endpoint `PreviewEditScore` (4.6) yang dry-run tanpa persist.
3. Insert `SessionElemenTeknisScores` baru
4. Update session via `ExecuteUpdateAsync` — **status guard berbeda**: WHERE Status == "Completed" (bukan != Completed)
5. Cascade sertifikat + TrainingRecord:
   ```
   if (oldIsPassed && !newIsPassed)  // Pass → Fail
   {
       session.NomorSertifikat = null;
       session.ValidUntil = null;
       UPDATE TrainingRecord SET Status="Failed" WHERE matches
   }
   else if (!oldIsPassed && newIsPassed)  // Fail → Pass
   {
       if (session.GenerateCertificate && session.AssessmentType != "PreTest")
       {
           Generate NomorSertifikat via CertNumberHelper.GetNextSeqAsync (retry 3x)
           Upsert TrainingRecord (if not exists, insert; else update Status="Passed")
       }
   }
   // Pass→Pass, Fail→Fail: no cert/TR change
   ```
6. **Skip** `_workerDataService.NotifyIfGroupCompleted` (sudah trigger pertama kali)
7. Return `(newScore, newIsPassed)`

### 4.9 Activity Log Integration

Tambah tab "Edit History" di modal Activity Log existing (tombol 🕐 di per-user table):

```
[Activity Timeline] [Edit History]
```

Tab "Edit History" tampilkan `AssessmentEditLog` filtered by `AssessmentSessionId`, sort `EditedAt DESC`. Format:

```
[2026-05-20 14:30] Soal #5: "Apa singkatan OJT?"
  [B. On Job Training] → [A. On The Job Training]
  oleh Admin (12345 - Budi)
  Alasan: Kunci jawaban salah
```

### 4.10 Permission

`[Authorize(Roles = "Admin, HC")]`. HC = full sama Admin.

---

## 5. Cross-cutting Concerns

### 5.1 SignalR Broadcast

- Group: `monitor-{batchKey}` (admin/HC monitoring viewer)
- Signal baru Phase 2: `workerAnswerEdited` payload `{ sessionId, workerName, oldScore, newScore, oldIsPassed, newIsPassed, actorName, actorRole }`
  - `workerName` — `User.FullName` peserta (konsisten dengan `workerStarted` / `workerSubmitted` convention)
  - `actorName` — `"NIP - FullName"` admin/HC yang edit (untuk toast notification ke admin lain)
  - `actorRole` — `"Admin"` / `"HC"`
- Frontend `AssessmentMonitoringDetail` handler update row score/result cell tanpa full reload (konsisten dengan `workerSubmitted` pattern), plus toast "{actorRole} {actorName} edit jawaban {workerName}: {oldScore}→{newScore}, {Pass→Fail|Fail→Pass|...}"

### 5.2 Cache Invalidation

Pattern existing: `_cache.Remove($"exam-status-{id}")` (AssessmentAdminController:3423, 3503).
Phase 2 invoke pattern sama post-edit.

### 5.3 Transaction Scope (Phase 2)

Wrap edit + audit + recompute + cascade dalam single transaction:

```csharp
using var tx = await _context.Database.BeginTransactionAsync();
try {
    // Steps 6–12 dari 4.5
    await tx.CommitAsync();
} catch (Exception ex) {
    await tx.RollbackAsync();
    _logger.LogError(ex, "Edit jawaban gagal untuk session {SessionId}", id);
    TempData["Error"] = "Gagal menyimpan edit. Tidak ada perubahan.";
    return RedirectToAction(...);
}
```

### 5.4 Anti-forgery

POST `SubmitEditAnswers`: `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` di form.

### 5.5 Concurrency

`AssessmentSession.UpdatedAt` sebagai token (sudah ada). Hidden field di form, compare di POST. Stale → redirect back + error.

### 5.6 Error Handling

- Concurrency stale → TempData + redirect back (HTTP 200 + reload UI)
- Re-grade exception → transaction rollback + TempData
- Invalid PackageQuestionId/OptionId (form tampering) → HTTP 400 + log warning
- Eligibility fail → TempData + redirect monitoring

### 5.7 A11y + Mobile

- Dropdown ⋮: ARIA `aria-label="Aksi lain untuk {nama}"`, keyboard nav (Tab/Enter/Esc)
- Bootstrap `dropdown-menu-end` + auto-flip mobile
- Edit form: per-input label, error visible di focus, submit button disabled saat in-flight
- ARIA live region untuk live score preview

### 5.8 Logging

`ILogger<T>` existing pattern:
- `Information` — Phase 2 edit success: `"Edit jawaban session={SessionId} count={N} oldScore={Y} newScore={Z} actor={ActorId}"`
- `Warning` — concurrency stale, eligibility fail, form tampering
- `Error` — exception in re-grade, audit write fail

### 5.9 Performance

- Phase 1: PNG generate parallel `Task.WhenAll`, fallback sequential kalau ada threading issue
- Phase 2: single-session edit, query scope kecil — no optimization khusus

### 5.10 Testing (Manual UAT)

Project tidak punya test infrastructure. Manual UAT only:

**Phase 1 UAT checklist**:
- [ ] Export 1 peserta Completed → verify struktur sheet, chart muncul
- [ ] Export 10 peserta mix Completed/Abandoned → verify N+1 sheets, Abandoned dapat sheet, In Progress/Not Started/Cancelled tidak
- [ ] Export 50+ peserta → verify response < 30 detik, file size masuk akal
- [ ] Export dengan Manual Entry session → verify Variant B sheet (info sertifikat manual, no chart)
- [ ] Export session ElemenTeknis < 3 elemen → verify table tampil, chart skip
- [ ] Buka file di Excel + LibreOffice → verify chart render OK

**Phase 2 UAT checklist**:
- [ ] Edit 1 soal MC, no flip → verify score recompute, spider update di Results.cshtml peserta, audit log entry di tab Edit History
- [ ] Edit 1 soal MA → verify multi-option update
- [ ] Edit menyebabkan Pass → Fail flip (dengan sertifikat existing) → verify modal konfirmasi muncul, NomorSertifikat null setelah save, TrainingRecord status "Failed"
- [ ] Edit menyebabkan Fail → Pass flip → verify NomorSertifikat generate, TrainingRecord status "Passed"
- [ ] 2 admin edit session sama bersamaan → verify yang kedua kena stale message
- [ ] Edit dengan reason kosong → block client + server
- [ ] Edit ReasonCode = "Lainnya" tanpa ReasonText → block
- [ ] Akses Edit page untuk session Cancelled / IsManualEntry / Tahun 3 → block + error
- [ ] Akses Edit page tanpa role Admin/HC → 403
- [ ] SignalR refresh → monitor di tab/browser lain auto-update score cell

### 5.11 Migration

- File: `Migrations/{Timestamp}_AddAssessmentEditLogs.cs`
- Buat tabel `AssessmentEditLogs` (schema di 4.7)
- Index `IX_AssessmentEditLogs_SessionId_EditedAt`
- No data migration

### 5.12 Library Dependency

Tambah ke `HcPortal.csproj`:
```xml
<PackageReference Include="SkiaSharp" Version="3.116.1" />
<PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="3.116.1" />
```

(Versi finalnya verifikasi compat dengan .NET 8 sebelum lock.)

### 5.13 Documentation

- Release notes ringkas (sheet name "Results" → "Summary" breaking, fitur Edit Jawaban baru).
- Tidak update Panduan Peserta (fitur admin, hidden dari peserta per Q7/Q10).

---

## 6. Phase Decomposition

| Phase | Topik | Estimasi tasks | Dependency |
|-------|-------|----------------|------------|
| 1 | Export Per-Peserta Excel + SkiaSharp integration | 4–6 tasks | Tidak ada |
| 2 | Edit Jawaban Peserta + RegradeAfterEditAsync + audit + UI dropdown | 8–10 tasks | Tidak ada (parallel-able dengan Phase 1) |

Phase 1 dan Phase 2 **bisa di-execute paralel** (independent file scope: Phase 1 = `ExportAssessmentResults` + new helper `SpiderChartRenderer`. Phase 2 = new route + new method + new table + view modifications).

---

## 7. Decision Summary (Q&A)

| # | Pertanyaan | Pilihan |
|---|-----------|---------|
| Q1 | Scope milestone | (a) Gabung 1 milestone, 2 phase |
| Q2 | Format Excel | (a) 1 summary + N sheet per peserta |
| Q3 | Isi sheet peserta | Hanya data yang tidak ada di summary + ElemenTeknis + chart + jawaban per soal |
| Q4 | Scope edit | (a) Edit jawaban saja |
| Q5 | Permission edit | (b) Admin + HC |
| Q5b | Reason field | (c) Dropdown preset + free text |
| Q6 | UI flow edit | (a) Page khusus EditPesertaAnswers |
| Q6b | Edit scope | (a)(i) MCQ only, Completed only |
| Q6c | Layout dropdown | (a) Hybrid: View Results + Activity Log inline, sisanya dropdown |
| Q7 | Visibility ke peserta | (d) Hidden, log internal |
| Q8 | Scope HC | (a) Full sama Admin |
| Q9 | Excel chart | (b) Render PNG server-side |
| Q10 | Notify peserta | (a) Total silent |
| Q11 | Fix Kunci Soal Global | (b) Defer |
| Q12 | Peserta non-Completed di export | (c) Completed + Abandoned |
| Q13 | MA vs MC scope | (c) MC + MA auto-graded, Essay skip |
| Q14 | Nama sheet summary | (b) Rename "Summary" |
| Q15 | Sertifikat cascade flip | (a) Auto-cascade |
| Q16 | Activity Log integration | (a) Tab baru "Edit History" |

---

## 8. Out of Scope (Explicit)

- Fix Kunci Soal Global / cascade re-grade — defer ke milestone berikutnya
- Edit jawaban Essay — sudah ditangani Penilaian Essay existing (Phase 298-05)
- Edit untuk session `InProgress` / `Abandoned` / `Cancelled`
- Edit Assessment Proton Tahun 3 (interview manual)
- Bulk edit grid (Excel-like) untuk banyak peserta sekaligus
- Notifikasi email/in-app ke peserta saat hasil diubah
- Workflow approval 2-step (HC → Admin)
- HC area/divisi restriction
- Undo/redo edit
- Diff export → re-export pasca edit hanya refresh dari DB latest
