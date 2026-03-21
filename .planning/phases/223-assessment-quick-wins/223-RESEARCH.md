# Phase 223: Assessment Quick Wins - Research

**Researched:** 2026-03-22
**Domain:** ASP.NET Core MVC — Assessment data integrity, EF Core migrations, TrainingRecord lifecycle
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Buat tabel `SessionElemenTeknisScore` dengan kolom: SessionId, ElemenTeknis, Score, MaxScore, QuestionCount, CorrectCount
- **D-02:** Populate tabel saat `SubmitExam` dan `GradeFromSavedAnswers` — hitung dari PackageQuestion.ElemenTeknis grouping
- **D-03:** Tampilkan breakdown skor per ElemenTeknis di halaman AssessmentResults (section tambahan: nama ET, benar/total soal, skor)
- **D-04:** Status valid hanya 3+1: `Passed`, `Valid`, `Expired`, dan `Failed` (khusus assessment gagal)
- **D-05:** Hapus `Wait Certificate` — tidak dipakai lagi
- **D-06:** Lifecycle Training Manual: Import/Add → `Passed` (tanpa sertifikat) atau `Valid` (dengan ValidUntil) → `Expired` (saat ValidUntil < now)
- **D-07:** Lifecycle Assessment: Lulus tanpa sertifikat → `Passed`. Lulus dengan sertifikat (GenerateCertificate=true) → `Valid` → `Expired`. Gagal → `Failed`
- **D-08:** Dokumentasikan lifecycle di komentar model TrainingRecord.cs
- **D-09:** Tambah field `SubmittedAt` (DateTime?) ke model UserResponse
- **D-10:** Isi `SubmittedAt = DateTime.UtcNow` saat SaveLegacyAnswer dipanggil
- **D-11:** Tambah komentar dokumentasi di model AssessmentSession.AccessToken menjelaskan: shared token by design — common exam room pattern, bukan security vulnerability

### Claude's Discretion
- Migration strategy untuk data existing dengan Status "Wait Certificate"
- Struktur HTML/CSS breakdown ET di halaman AssessmentResults
- Penanganan soal tanpa ElemenTeknis tag saat hitung skor per ET

### Deferred Ideas (OUT OF SCOPE)
- **Tab-switch detection (AINT-02, AINT-03):** Tidak diimplementasi di Phase 223 — sudah ada mekanisme tab-switch warning di sistem saat ini.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| AINT-01 | Skor ElemenTeknis per session dipersist ke database (tabel SessionElemenTeknisScore) saat SubmitExam dan GradeFromSavedAnswers | Model baru + EF migration + logic grouping sudah ada di Results action |
| AINT-04 | UserResponse (legacy path) memiliki field SubmittedAt timestamp yang terisi saat SaveLegacyAnswer | Field belum ada di UserResponse — perlu migration + update SaveLegacyAnswer dan SubmitExam legacy path |
| CLEN-01 | TrainingRecord.Status lifecycle terdefinisi jelas — hapus ambiguitas Passed/Valid, transisi terdokumentasi | "Wait Certificate" masih ada di 4 view files + 1 service; perlu cleanup menyeluruh |
| CLEN-05 | Shared AccessToken tetap as-is (documented decision — common exam room pattern) | Tinggal tambah XML doc comment di AssessmentSession.AccessToken |
</phase_requirements>

---

## Summary

Phase 223 adalah kumpulan perbaikan integritas data assessment yang berdiri sendiri dan tidak saling bergantung. Empat requirement dapat diimplementasi secara paralel karena menyentuh bagian kode yang berbeda.

**AINT-01** memerlukan tabel baru `SessionElemenTeknisScore` dan logic persist di dua method grading (SubmitExam package path + GradeFromSavedAnswers package path). Penting: logic hitung ET sudah ada di action `Results` (computed on-the-fly dari PackageUserResponses) — tabel baru hanya untuk persist ke DB agar bisa diquery ulang tanpa menghitung ulang. Halaman `Results.cshtml` dan `AssessmentResultsViewModel` sudah punya section ElemenTeknis yang berfungsi — bagian view TIDAK perlu diubah.

**AINT-04** adalah perubahan kecil: tambah field `SubmittedAt` (DateTime?) ke `UserResponse` model, EF migration, lalu isi di dua tempat: `SaveLegacyAnswer` (upsert existing row + insert new row) dan `SubmitExam` legacy path (upsert existing row). `PackageUserResponse` sudah punya `SubmittedAt` sebagai referensi pola yang tepat.

**CLEN-01** adalah cleanup lifecycle. Status "Wait Certificate" ada di 4 view files (`EditTraining.cshtml`, `AddTraining.cshtml`, `ImportTraining.cshtml`, `Records.cshtml`) dan 1 service (`WorkerDataService.cs` baris 218). Status "Failed" sudah dipakai di kode (GradeFromSavedAnswers baris 2902, WorkerDataService baris 49) tapi belum ada di dropdown form. Migrasi data: update existing rows `Status = 'Wait Certificate'` → `'Passed'` via migration script (karena "Wait Certificate" secara semantis berarti sudah selesai, hanya menunggu sertifikat fisik).

**CLEN-05** adalah satu baris: tambah XML doc comment `<summary>` pada `AssessmentSession.AccessToken`.

**Primary recommendation:** Implementasi keempat item secara berurutan: CLEN-05 (1 menit) → AINT-04 (migration + 2 lokasi kode) → CLEN-01 (cleanup views + data migration) → AINT-01 (model baru + migration + 2 lokasi grading).

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core Migrations | (project version) | Schema change untuk SessionElemenTeknisScore + UserResponse.SubmittedAt | Sudah dipakai di semua phase sebelumnya |
| ASP.NET Core MVC | (project version) | Controller + View pattern | Framework utama proyek |

### Tidak ada library baru yang dibutuhkan
Semua perubahan hanya menggunakan library yang sudah ada di proyek.

---

## Architecture Patterns

### Pattern 1: Model Baru + DbSet + Migration
**Untuk:** AINT-01 — `SessionElemenTeknisScore`

```csharp
// Models/SessionElemenTeknisScore.cs
public class SessionElemenTeknisScore
{
    public int Id { get; set; }

    public int AssessmentSessionId { get; set; }
    [ForeignKey("AssessmentSessionId")]
    public virtual AssessmentSession AssessmentSession { get; set; } = null!;

    /// <summary>Nama elemen teknis. "Lainnya" untuk soal tanpa tag ET.</summary>
    public string ElemenTeknis { get; set; } = "";

    public int CorrectCount { get; set; }
    public int QuestionCount { get; set; }
}
```

Tambah ke `ApplicationDbContext.cs`:
```csharp
public DbSet<SessionElemenTeknisScore> SessionElemenTeknisScores { get; set; }
```

### Pattern 2: ET Score Persist di SubmitExam (Package Path)
**Untuk:** AINT-01 — persist setelah grading loop selesai, sebelum `SaveChangesAsync()`

```csharp
// Setelah grading loop, sebelum SaveChanges pertama di package path SubmitExam
// Group questions by ElemenTeknis dan hitung skor per ET
var etGroups = packageQuestions
    .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis);

foreach (var etGroup in etGroups)
{
    int etCorrect = 0;
    int etTotal = etGroup.Count();
    foreach (var q in etGroup)
    {
        if (answers.ContainsKey(q.Id))
        {
            var sel = q.Options.FirstOrDefault(o => o.Id == answers[q.Id]);
            if (sel != null && sel.IsCorrect) etCorrect++;
        }
    }
    _context.SessionElemenTeknisScores.Add(new SessionElemenTeknisScore
    {
        AssessmentSessionId = id,
        ElemenTeknis = etGroup.Key,
        CorrectCount = etCorrect,
        QuestionCount = etTotal
    });
}
```

Pola yang sama diterapkan di `GradeFromSavedAnswers` (AdminController) — perbedaan: gunakan `responses` dictionary yang sudah ada (PackageUserResponses) untuk lookup jawaban.

### Pattern 3: AddColumn via EF Migration
**Untuk:** AINT-04 — tambah `SubmittedAt` ke `UserResponse`

```csharp
// Models/UserResponse.cs — tambah field:
public DateTime? SubmittedAt { get; set; }
```

Update `SaveLegacyAnswer` — dua path (upsert update + insert baru):
```csharp
// Update path (ExecuteUpdateAsync tidak bisa dipakai — tambah SubmittedAt):
// Ganti ExecuteUpdateAsync dengan load + set + save, ATAU
// tambah SetProperty(r => r.SubmittedAt, DateTime.UtcNow) di ExecuteUpdateAsync

var updatedCount = await _context.UserResponses
    .Where(r => r.AssessmentSessionId == sessionId && r.AssessmentQuestionId == questionId)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.SelectedOptionId, optionId)
        .SetProperty(r => r.SubmittedAt, DateTime.UtcNow)  // tambahkan ini
    );

if (updatedCount == 0)
{
    _context.UserResponses.Add(new UserResponse
    {
        AssessmentSessionId = sessionId,
        AssessmentQuestionId = questionId,
        SelectedOptionId = optionId,
        SubmittedAt = DateTime.UtcNow  // tambahkan ini
    });
    await _context.SaveChangesAsync();
}
```

Update `SubmitExam` legacy path upsert:
```csharp
if (existingLegacyDict.TryGetValue(question.Id, out var existingLegacyResponse))
{
    existingLegacyResponse.SelectedOptionId = selectedOptionId;
    existingLegacyResponse.SubmittedAt = DateTime.UtcNow;  // tambahkan ini
}
else
{
    _context.UserResponses.Add(new UserResponse
    {
        AssessmentSessionId = id,
        AssessmentQuestionId = question.Id,
        SelectedOptionId = selectedOptionId,
        SubmittedAt = DateTime.UtcNow  // tambahkan ini
    });
}
```

### Pattern 4: CLEN-01 — Status Lifecycle Cleanup

**Lokasi yang perlu diubah:**

| File | Perubahan |
|------|-----------|
| `Views/Admin/EditTraining.cshtml` (baris 117) | Hapus `<option value="Wait Certificate">`, tambah `<option value="Failed">` |
| `Views/Admin/AddTraining.cshtml` (baris 139) | Hapus `<option value="Wait Certificate">`, tambah `<option value="Failed">` |
| `Views/Admin/ImportTraining.cshtml` (baris 188) | Update keterangan kolom Status di template help text |
| `Views/CMP/Records.cshtml` (baris 178) | Hapus "Wait Certificate" dari switch expression badge |
| `Views/CMP/RecordsWorkerDetail.cshtml` (baris 239) | Hapus "Wait Certificate" dari switch expression |
| `Views/Admin/ManageAssessment.cshtml` (baris 606) | Hapus "Wait Certificate" dari switch expression |
| `Services/WorkerDataService.cs` (baris 218) | Hapus `tr.Status == "Wait Certificate"` dari kondisi "in-progress" |
| `Models/TrainingRecord.cs` | Tambah XML doc comment lifecycle di field `Status` |

**Data migration (Claude's Discretion — recommended):** Dalam EF migration, tambahkan SQL:
```sql
UPDATE TrainingRecords SET Status = 'Passed' WHERE Status = 'Wait Certificate';
```

**Reasoning:** "Wait Certificate" secara semantis = training selesai, hanya menunggu sertifikat fisik — paling tepat di-map ke `Passed`.

### Pattern 5: CLEN-05 — AccessToken Documentation

```csharp
// Models/AssessmentSession.cs — ganti baris yang ada:
// public string AccessToken { get; set; } = "";

/// <summary>
/// Token akses untuk masuk ke sesi ujian.
/// DESAIN DISENGAJA: Token ini di-share ke semua peserta dalam satu batch ujian yang sama
/// (common exam room pattern). Ini bukan security vulnerability — peserta ujian memang
/// berada di ruangan yang sama dan mendapat token yang sama dari pengawas.
/// Token hanya mengontrol akses masuk, bukan identitas peserta (identity ditangani ASP.NET Core Identity).
/// </summary>
public string AccessToken { get; set; } = "";
```

### Recommended Project Structure (tidak berubah)
Tidak ada folder baru. File baru hanya `Models/SessionElemenTeknisScore.cs`.

### Anti-Patterns to Avoid
- **Jangan re-compute ET skor dari DB di Results action**: Results action sudah compute on-the-fly dari PackageUserResponses — persist ke `SessionElemenTeknisScore` adalah TAMBAHAN untuk kebutuhan analytics di Phase 224, bukan pengganti. Results view tetap menggunakan computed data dari ViewModel.
- **Jangan hapus legacy path di SaveLegacyAnswer**: Legacy path masih aktif untuk session yang tidak menggunakan packages. Kedua path harus di-update.
- **Jangan ubah kondisi IsExpiringSoon di TrainingRecord**: Property tersebut cek `Status == "Valid"` yang sudah benar.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Schema migration | Manual SQL ALTER TABLE | EF Core Add-Migration | Konsisten dengan semua 40+ migration yang ada |
| Data migration | Terpisah dari schema | Tambahkan `migrationBuilder.Sql()` dalam migration yang sama | Atomik — schema + data change dalam satu transaction |

---

## Common Pitfalls

### Pitfall 1: ExecuteUpdateAsync Tidak Trigger Change Tracker
**What goes wrong:** `ExecuteUpdateAsync` di `SaveLegacyAnswer` bypass EF change tracker — tidak bisa ditambah field `SubmittedAt` dengan cara `SetProperty` jika lupa syntax yang benar.
**Why it happens:** `ExecuteUpdateAsync` menggunakan LINQ expression tree, bukan objek yang di-track.
**How to avoid:** Gunakan `.SetProperty(r => r.SubmittedAt, DateTime.UtcNow)` sebagai parameter tambahan dalam chain yang sama. Sintaks ini sudah digunakan di PackageUserResponse di codebase.
**Warning signs:** Compile error di SetProperty — periksa bahwa field `SubmittedAt` sudah ditambah ke model sebelum update kode controller.

### Pitfall 2: SessionElemenTeknisScore — Duplicate Insert
**What goes wrong:** Jika SubmitExam dipanggil ulang (misal race condition dengan AkhiriUjian), ET scores bisa di-insert dua kali.
**How to avoid:** Tambah guard: cek apakah record ET sudah ada untuk sessionId ini sebelum insert. Alternatif: gunakan unique constraint (SessionId + ElemenTeknis) di migration.
**Recommended:** Tambah unique index `IX_SessionElemenTeknisScore_SessionId_ElemenTeknis` di migration — maka insert duplikat akan fail di DB level, konsisten dengan pola `PackageUserResponse` yang punya unique constraint.

### Pitfall 3: Wait Certificate di WorkerDataService
**What goes wrong:** `WorkerDataService.cs` baris 218 menggunakan `tr.Status == "Wait Certificate"` sebagai kondisi "in-progress". Jika tidak dihapus, logic tersebut tidak akan pernah terpenuhi setelah data migration.
**How to avoid:** Hapus kondisi tersebut dari service. Cek apakah ada logika yang bergantung pada "in-progress" state ini di tempat lain.
**Warning signs:** Search `Wait Certificate` di seluruh codebase setelah cleanup — harus zero results.

### Pitfall 4: GradeFromSavedAnswers Tidak Return Tapi Caller SaveChanges
**What goes wrong:** Komentar di `GradeFromSavedAnswers` menyebutkan "Does NOT call SaveChangesAsync — caller handles it". Jika ET scores di-add ke context di dalam method ini, caller yang akan save — itu sudah benar. Tapi jika ET scores sudah ada (session digrade ulang), insert akan duplikat.
**How to avoid:** Tambah guard delete-before-insert atau unique constraint. Pola yang direkomendasikan: unique constraint di DB, biarkan exception jika session sudah di-grade (tidak akan terjadi di happy path).

---

## Code Examples

### Existing: PackageUserResponse.SubmittedAt (Reference Pattern)
```csharp
// CMPController.cs ~1460 — pola upsert yang sudah ada di package path
if (existingResponses.TryGetValue(q.Id, out var existingResponse))
{
    existingResponse.PackageOptionId = selectedOptId;
    existingResponse.SubmittedAt = DateTime.UtcNow;  // sudah ada
}
else
{
    _context.PackageUserResponses.Add(new PackageUserResponse
    {
        AssessmentSessionId = id,
        PackageQuestionId = q.Id,
        PackageOptionId = selectedOptId,
        SubmittedAt = DateTime.UtcNow  // sudah ada
    });
}
```

### Existing: ElemenTeknis Grouping Logic (Reference Pattern)
```csharp
// CMPController.cs ~2097 — Results action, package path
elemenTeknisScores = examQuestions
    .GroupBy(q => string.IsNullOrWhiteSpace(q.ElemenTeknis) ? "Lainnya" : q.ElemenTeknis!)
    .Select(g => new ElemenTeknisScore
    {
        Name = g.Key,
        Correct = g.Count(q => /* check correct */),
        Total = g.Count(),
        Percentage = Math.Round((double)correct / total * 100, 1)
    })
    .OrderBy(s => s.Name)
    .ToList();
```

### Existing: Fire-and-Forget Pattern (untuk referensi saja)
```csharp
// CMPController.cs ~1638 — LogActivityAsync
private void LogActivityAsync(int sessionId, string eventType, string? detail = null)
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            // ...
            await db.SaveChangesAsync();
        }
        catch { /* swallow */ }
    });
}
```
ET score persist TIDAK menggunakan fire-and-forget — harus synchronous karena merupakan bagian dari business data inti (bukan logging).

---

## State of the Art

| Old Approach | Current Approach | Catatan |
|--------------|------------------|---------|
| ET skor dihitung ulang setiap kali Results dibuka | ET skor dipersist ke DB saat submit | Setelah Phase 223 |
| UserResponse tidak punya timestamp | UserResponse.SubmittedAt tersedia | Setelah Phase 223 |
| "Wait Certificate" sebagai status yang ambigu | Dihapus, replaced dengan `Passed`/`Valid`/`Failed`/`Expired` | Setelah Phase 223 |

---

## Open Questions

1. **GradeFromSavedAnswers — Legacy Path ET Scores**
   - What we know: Legacy path di GradeFromSavedAnswers tidak punya ElemenTeknis (AssessmentQuestion tidak punya field ini).
   - What's unclear: Apakah perlu insert ET scores untuk legacy path?
   - Recommendation: Skip persist untuk legacy path (sama dengan behavior Results action saat ini yang set `legacyEtScores = null`). Dokumentasikan sebagai comment di kode.

2. **Status "Expired" — Siapa yang mengubah?**
   - What we know: `Expired` muncul di CertificationManagement view sebagai option. Tidak ada kode yang otomatis set status ke "Expired" saat ini.
   - What's unclear: Apakah ada background job atau computed property yang set "Expired"?
   - Recommendation: TrainingRecord.IsExpiringSoon sudah ada sebagai computed property cek ValidUntil. Status "Expired" kemungkinan di-set manual atau via RenewalCertificate flow. Tidak perlu diubah di Phase 223 — lifecycle dokumentasi di komentar cukup.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Tidak ada test framework yang terdeteksi di proyek |
| Config file | none |
| Quick run command | `dotnet build` (smoke test) |
| Full suite command | `dotnet build` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| AINT-01 | SessionElemenTeknisScore ter-insert setelah SubmitExam | manual-only (browser test) | `dotnet build` | N/A |
| AINT-04 | UserResponse.SubmittedAt terisi setelah SaveLegacyAnswer | manual-only (DB query) | `dotnet build` | N/A |
| CLEN-01 | "Wait Certificate" tidak muncul di dropdown form | manual-only (browser) | `dotnet build` | N/A |
| CLEN-05 | AccessToken punya komentar dokumentasi | code review | `dotnet build` | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** `dotnet build`
- **Phase gate:** Build hijau + manual browser verification sebelum `/gsd:verify-work`

### Wave 0 Gaps
Tidak ada test infrastructure yang diperlukan — proyek belum menggunakan automated test framework. Verifikasi dilakukan via manual browser testing dan DB query sesuai pola yang telah ditetapkan di proyek ini.

---

## Sources

### Primary (HIGH confidence)
- Codebase langsung — `CMPController.cs`, `AdminController.cs`, `Models/`, `Views/` dibaca dan diverifikasi
- `ApplicationDbContext.cs` — DbSet inventory diverifikasi

### Secondary (MEDIUM confidence)
- Tidak ada sumber eksternal yang diperlukan — semua informasi dari codebase proyek

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru, semua dari codebase existing
- Architecture: HIGH — semua pattern diverifikasi langsung dari kode yang dibaca
- Pitfalls: HIGH — berdasarkan baca kode aktual, bukan asumsi

**Research date:** 2026-03-22
**Valid until:** 2026-04-22 (stabil — perubahan hanya pada codebase lokal)
