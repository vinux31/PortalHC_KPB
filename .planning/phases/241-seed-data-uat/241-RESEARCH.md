# Phase 241: Seed Data UAT - Research

**Researched:** 2026-03-24
**Domain:** ASP.NET Core — idempotent seed data via SeedData.cs extension
**Confidence:** HIGH

## Summary

Phase ini murni task ekstensi `Data/SeedData.cs`: menambah method `SeedUatDataAsync()` yang dipanggil dari `InitializeAsync` dengan guard `IsDevelopment()`. Seluruh pola (idempotency, IsDevelopment check, Console.WriteLine logging) sudah ada dan tinggal diikuti.

Entity chain untuk assessment reguler adalah: `AssessmentSession` → `AssessmentPackage` → `PackageQuestion` + `PackageOption` → `UserPackageAssignment` (satu per peserta) → `PackageUserResponse` (satu per soal per peserta). Untuk Proton: entity yang sama kecuali `UserPackageAssignment` dan `PackageUserResponse` hanya untuk Rino. Untuk CoachCoacheeMapping + ProtonTrackAssignment: dua entity terpisah sederhana.

`NomorSertifikat` di-generate oleh `CertNumberHelper.Build(seq, date)` dengan format `KPB/{seq:D3}/{ROMAN-MONTH}/{YEAR}`. Seed perlu mengikuti pola ini agar konsisten dengan aplikasi. Unique index `IX_AssessmentSessions_NomorSertifikat_Unique` berlaku — seed harus memakai nomor yang belum dipakai.

**Primary recommendation:** Extend `SeedData.cs` dengan satu method `SeedUatDataAsync()`, strukturkan secara linear (session → package → questions → options → assignments → responses → ET scores), guard dengan check nama assessment sudah ada.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Soal semi-realistis — judul dan opsi terlihat nyata terkait operasi kilang/Alkylation, tapi konten tidak harus akurat secara teknis
- **D-02:** 4 Elemen Teknis dengan nama generik kilang: "Proses Distilasi", "Keselamatan Kerja", "Operasi Pompa", "Instrumentasi" (atau sejenisnya)
- **D-03:** 15 soal dengan 4 opsi masing-masing, ET di-assign merata ke soal
- **D-04:** Seed 2 completed assessment untuk Rino: 1 lulus (skor tinggi ~80) + 1 gagal (skor rendah ~40)
- **D-05:** Keduanya lengkap dengan UserResponses (jawaban per soal) agar review jawaban dan radar chart ET bisa ditest
- **D-06:** Assessment yang lulus: sertifikat ter-generate dengan ValidUntil = 1 tahun dari tanggal completed
- **D-07:** Assessment yang gagal: tanpa sertifikat, IsPassed=false
- **D-08:** Extend `Data/SeedData.cs` — tambah method `SeedUatDataAsync()` dipanggil dari `InitializeAsync`, konsisten dengan pattern existing
- **D-09:** Guard `IsDevelopment()` sama seperti `CreateUsersAsync`
- **D-10:** Skip jika data sudah ada (check by nama assessment) — pattern sama dengan `CreateUsersAsync` yang check `FindByEmailAsync`
- **D-11:** Assessment reguler "OJT Proses Alkylation Q1-2026": peserta Rino + Iwan sesuai requirements
- **D-12:** Assessment Proton Tahun 1 & Tahun 3: hanya Rino, tidak ada user tambahan
- **D-13:** Semua tanggal relative dari waktu startup — `CreatedAt = DateTime.UtcNow`, jadwal assessment = UtcNow + 7 hari, sehingga selalu valid kapan pun app dijalankan
- **D-14:** Seed CoachCoacheeMapping Rustam→Rino + ProtonTrackAssignment aktif, agar Proton coaching flow langsung bisa ditest

### Claude's Discretion
- Nama spesifik soal dan opsi jawaban (selama semi-realistis kilang)
- Distribusi jawaban benar/salah pada completed assessment
- Nomor sertifikat format (mengikuti pattern KPB/SEQ/BULAN/TAHUN yang sudah ada)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore | Project-installed | ORM untuk insert seed data | Sudah dipakai di SeedData.cs |
| HcPortal.Helpers.CertNumberHelper | Internal | Generate NomorSertifikat | Helper ekstrak dari controller — reusable |
| System.Text.Json | Built-in .NET | Serialize ShuffledQuestionIds / ShuffledOptionIdsPerQuestion | Sudah dipakai di UserPackageAssignment |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.AspNetCore.Identity | Project-installed | Lookup user by email untuk mendapat UserId | Untuk mendapatkan UserId Rustam, Rino, Iwan |

**Installation:** Tidak ada package baru dibutuhkan — semua dependency sudah ada.

## Architecture Patterns

### Recommended Project Structure
```
Data/
└── SeedData.cs        # Extend dengan method SeedUatDataAsync()
Helpers/
└── CertNumberHelper.cs  # Gunakan Build() dan GetNextSeqAsync()
```

### Pattern 1: Idempotent Guard by Name
**What:** Cek apakah AssessmentSession dengan Title tertentu sudah ada sebelum seed.
**When to use:** Selalu — mencegah duplikasi data saat app restart.
**Example:**
```csharp
// Source: SeedData.cs existing pattern (AnyAsync check)
if (await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Proses Alkylation Q1-2026"))
{
    Console.WriteLine("UAT-SEED: AssessmentSession sudah ada, skip.");
    return;
}
```

### Pattern 2: Entity Chain Assembly
**What:** Buat entity dari parent ke child, SaveChangesAsync setelah setiap layer agar Id tersedia untuk FK child.
**When to use:** Saat entity child memerlukan Id parent sebagai FK.
**Example:**
```csharp
// Source: SeedData.cs SeedOrganizationUnitsAsync pattern
var session = new AssessmentSession { Title = "...", ... };
context.AssessmentSessions.Add(session);
await context.SaveChangesAsync(); // session.Id sekarang terisi

var package = new AssessmentPackage { AssessmentSessionId = session.Id, ... };
context.AssessmentPackages.Add(package);
await context.SaveChangesAsync(); // package.Id sekarang terisi
```

### Pattern 3: UserPackageAssignment ShuffledQuestionIds
**What:** Field `ShuffledQuestionIds` dan `ShuffledOptionIdsPerQuestion` adalah JSON yang mewakili urutan soal dan opsi per user.
**When to use:** Saat membuat UserPackageAssignment untuk completed session.
**Example:**
```csharp
// Untuk completed session — gunakan urutan natural (tidak di-shuffle untuk seed)
var questionIds = questions.Select(q => q.Id).ToList();
var shuffledOpts = questions.ToDictionary(
    q => q.Id.ToString(),
    q => q.Options.Select(o => o.Id).ToList()
);
assignment.ShuffledQuestionIds = JsonSerializer.Serialize(questionIds);
assignment.ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(shuffledOpts);
```

### Pattern 4: Completed Assessment dengan NomorSertifikat
**What:** Untuk assessment yang lulus, set fields Status="Completed", IsPassed=true, Score=80, CompletedAt, ValidUntil, NomorSertifikat.
**When to use:** D-04, D-06.
**Example:**
```csharp
// Source: CertNumberHelper.Build() dan GetNextSeqAsync()
var certNow = DateTime.UtcNow;
var nextSeq = await CertNumberHelper.GetNextSeqAsync(context, certNow.Year);
session.NomorSertifikat = CertNumberHelper.Build(nextSeq, certNow);
session.ValidUntil = certNow.AddYears(1);
session.IsPassed = true;
session.Score = 80;
session.Status = "Completed";
session.CompletedAt = certNow;
```

### Pattern 5: SessionElemenTeknisScore
**What:** Satu record per ElemenTeknis per AssessmentSession — merangkum hasil per ET untuk radar chart.
**When to use:** Wajib seed agar radar chart di records view berfungsi (D-05).
**Example:**
```csharp
// Dengan 15 soal dan 4 ET (masing-masing 3-4 soal), hitung benar per ET
// Lulus (~80): ~12 soal benar — distribusi: ET1=3/4, ET2=3/4, ET3=3/4, ET4=3/3
// Gagal (~40): ~6 soal benar — distribusi: ET1=2/4, ET2=1/4, ET3=2/4, ET4=1/3
context.SessionElemenTeknisScores.AddRange(new[]
{
    new SessionElemenTeknisScore { AssessmentSessionId = sessionId, ElemenTeknis = "Proses Distilasi", CorrectCount = 3, QuestionCount = 4 },
    // ...
});
```

### Pattern 6: CoachCoacheeMapping Unique Index Constraint
**What:** Index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` memaksa hanya 1 active mapping per coachee.
**When to use:** Seed harus check sebelum insert.
**Example:**
```csharp
var mappingExists = await context.CoachCoacheeMappings
    .AnyAsync(m => m.CoacheeId == rinoId && m.IsActive);
if (!mappingExists)
{
    context.CoachCoacheeMappings.Add(new CoachCoacheeMapping
    {
        CoachId = rustamId,
        CoacheeId = rinoId,
        IsActive = true,
        StartDate = DateTime.UtcNow
    });
    await context.SaveChangesAsync();
}
```

### Anti-Patterns to Avoid
- **Tidak SaveChangesAsync setelah parent sebelum insert child:** EF akan gagal dengan FK violation karena Id belum ter-generate.
- **Set NomorSertifikat manual tanpa GetNextSeqAsync:** Dapat conflict dengan unique index jika ada cert lain.
- **Seed SessionElemenTeknisScore sebelum SaveChanges AssessmentSession:** AssessmentSessionId belum ada.
- **Skip UserPackageAssignment untuk completed session:** Review jawaban dan radar chart tidak akan berfungsi.
- **Insert CoachCoacheeMapping duplikat tanpa check:** Unique index active-per-coachee akan throw DbUpdateException.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Format NomorSertifikat | Custom string format | `CertNumberHelper.Build()` + `GetNextSeqAsync()` | Helper sudah handle sequence, roman month, format KPB/{seq}/{month}/{year} dan unique index |
| JSON shuffle fields | Custom serializer | `JsonSerializer.Serialize()` langsung | Pattern yang sama dengan UserPackageAssignment helper methods |

## Common Pitfalls

### Pitfall 1: ProtonTrack tidak ada di DB saat seed
**What goes wrong:** `ProtonTrackAssignment.ProtonTrackId` referensi ke `ProtonTrack` yang belum di-seed — FK violation.
**Why it happens:** ProtonTrack di-seed secara terpisah (via admin UI atau migration lain), bukan di SeedData.cs.
**How to avoid:** Query ProtonTrack dari DB terlebih dahulu (`FindAsync` atau `FirstOrDefaultAsync`). Jika tidak ada, log warning dan skip seed ProtonTrackAssignment — atau seed ProtonTrack minimal terlebih dahulu.
**Warning signs:** Exception "FK constraint failed" pada insert ProtonTrackAssignment.

### Pitfall 2: User tidak ditemukan karena CreateUsersAsync gagal
**What goes wrong:** `FindByEmailAsync("rustam.nugroho@pertamina.com")` return null jika user belum ter-seed.
**Why it happens:** `SeedUatDataAsync` dipanggil setelah `CreateUsersAsync`, tapi jika CreateUsersAsync gagal sebagian, user mungkin belum ada.
**How to avoid:** Guard: jika user null, log warning dan return early. Jangan null-dereference.
**Warning signs:** NullReferenceException pada `rinoId = rino.Id`.

### Pitfall 3: AssessmentAttemptHistory tidak di-seed
**What goes wrong:** Jika UAT tests memeriksa riwayat attempt, AttemptHistory tidak akan ada.
**Why it happens:** AssessmentAttemptHistory biasanya diisi oleh SubmitExam flow, tidak otomatis.
**How to avoid:** Seed `AssessmentAttemptHistory` untuk kedua completed session Rino — 1 record lulus, 1 record gagal.
**Warning signs:** Halaman "Riwayat Ujian" Rino kosong setelah seed.

### Pitfall 4: PackageUserResponse FK ke AssessmentSession adalah Restrict
**What goes wrong:** Jika seed AssessmentSession lalu delete dan re-seed, delete akan gagal karena PackageUserResponse FK adalah Restrict.
**Why it happens:** DbContext config: `OnDelete(DeleteBehavior.Restrict)` pada PackageUserResponse → AssessmentSession.
**How to avoid:** Idempotency guard cukup — skip keseluruhan jika sudah ada. Jangan delete-recreate.
**Warning signs:** FK violation saat mencoba reset data seed.

### Pitfall 5: SessionElemenTeknisScore unique constraint
**What goes wrong:** Duplicate insert jika seed dipanggil sebagian.
**Why it happens:** Unique index `IX_SessionElemenTeknisScores_AssessmentSessionId_ElemenTeknis` — satu record per (SessionId, ElemenTeknis).
**How to avoid:** Guard idempotency di awal method menggunakan check nama assessment. Jika assessment sudah ada, seluruh method skip.

### Pitfall 6: AccessToken wajib diisi pada AssessmentSession
**What goes wrong:** AssessmentSession.AccessToken default `""` — tapi beberapa view mungkin menggunakannya.
**Why it happens:** Field non-nullable string.
**How to avoid:** Set AccessToken ke Guid pendek atau string dummy seperti `"UAT-TOKEN-001"` agar konsisten.

## Code Examples

### Struktur SeedUatDataAsync yang Direkomendasikan
```csharp
// Source: pola SeedData.cs existing
public static async Task SeedUatDataAsync(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context)
{
    // Guard idempotency
    if (await context.AssessmentSessions.AnyAsync(s => s.Title == "OJT Proses Alkylation Q1-2026"))
    {
        Console.WriteLine("UAT-SEED: Data UAT sudah ada, skip.");
        return;
    }

    // Lookup users
    var rino = await userManager.FindByEmailAsync("rino.prasetyo@pertamina.com");
    var iwan = await userManager.FindByEmailAsync("iwan3@pertamina.com");
    var rustam = await userManager.FindByEmailAsync("rustam.nugroho@pertamina.com");
    if (rino == null || iwan == null || rustam == null)
    {
        Console.WriteLine("UAT-SEED: User tidak ditemukan, skip.");
        return;
    }

    var now = DateTime.UtcNow;

    // 1. Coach-Coachee Mapping
    await SeedCoachCoacheeMappingAsync(context, rustam.Id, rino.Id, now);

    // 2. ProtonTrackAssignment
    await SeedProtonTrackAssignmentAsync(context, rino.Id, rustam.Id, now);

    // 3. AssessmentCategory (sub-kategori)
    await SeedAssessmentCategoriesAsync(context);

    // 4. Assessment reguler (open/upcoming) + package + 15 soal
    await SeedRegularAssessmentAsync(context, rino.Id, iwan.Id, now);

    // 5. Assessment reguler completed (lulus) untuk Rino
    await SeedCompletedAssessmentPassAsync(context, rino.Id, now);

    // 6. Assessment reguler completed (gagal) untuk Rino
    await SeedCompletedAssessmentFailAsync(context, rino.Id, now);

    // 7. Assessment Proton
    await SeedProtonAssessmentsAsync(context, rino.Id, now);

    Console.WriteLine("UAT-SEED: Selesai seed data UAT.");
}
```

### Lookup ProtonTrack
```csharp
// ProtonTrack perlu di-query dari DB karena Id tidak diketahui sebelumnya
var protonTrack = await context.ProtonTracks
    .FirstOrDefaultAsync(t => t.TrackType == "Operator" && t.TahunKe == "Tahun 1");
if (protonTrack == null)
{
    Console.WriteLine("UAT-SEED: ProtonTrack 'Operator Tahun 1' tidak ditemukan, skip ProtonTrackAssignment.");
    return;
}
```

### PackageUserResponse untuk Completed Session
```csharp
// Buat response per soal — untuk lulus, pilih IsCorrect=true sebagian besar
foreach (var question in questions)
{
    var isCorrect = correctAnswerCount > 0; // logika distribusi skor
    if (isCorrect) correctAnswerCount--;
    var chosenOption = question.Options.First(o => o.IsCorrect == isCorrect);
    context.PackageUserResponses.Add(new PackageUserResponse
    {
        AssessmentSessionId = session.Id,
        PackageQuestionId = question.Id,
        PackageOptionId = chosenOption.Id,
        SubmittedAt = now
    });
}
await context.SaveChangesAsync();
```

## Runtime State Inventory

> Tidak relevan — ini fase greenfield seed, bukan rename/refactor.

## Environment Availability

> Fase ini adalah code-only change (extend SeedData.cs). Tidak ada external dependencies baru.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| SQL Server / LocalDB | EF Core seed | Assumed (dev environment) | — | — |
| ProtonTrack rows | ProtonTrackAssignment seed | Depends on prior seed | DB query | Log warning + skip |

**Missing dependencies dengan fallback:**
- ProtonTrack rows di DB: jika tidak ada, seed ProtonTrackAssignment di-skip dengan log warning — tidak block seluruh seed.

## Validation Architecture

> `workflow.nyquist_validation` tidak dikonfigurasi secara eksplisit — diperlakukan sebagai enabled. Namun untuk fase seed data, tidak ada unit test yang relevan — validasi dilakukan secara manual via browser (UAT flow 242-246).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual verification (browser UAT) |
| Config file | none |
| Quick run command | `dotnet run` lalu login sebagai Rino |
| Full suite command | UAT flows 242-246 |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| (none) | Data UAT ter-seed saat app start di Dev | manual-only | login browser, cek data | — |

### Sampling Rate
- **Per task commit:** Jalankan `dotnet run` dan verifikasi Console.WriteLine output
- **Phase gate:** Semua UAT flows 242-246 dapat dieksekusi tanpa setup manual

### Wave 0 Gaps
- Tidak ada test file yang perlu dibuat — validasi via browser UAT flows.

## Open Questions

1. **ProtonTrack sudah ada di DB Development?**
   - What we know: ProtonTrack di-seed via admin UI atau migration lain, bukan di SeedData.cs
   - What's unclear: Apakah rows Operator Tahun 1 dan Tahun 3 sudah ada di DB dev saat ini
   - Recommendation: Seed harus query ProtonTrack dan guard jika tidak ditemukan. Alternatif: tambah seed ProtonTrack minimal di SeedUatDataAsync jika belum ada.

2. **AssessmentAttemptHistory perlu di-seed?**
   - What we know: Model ada (Phase 46), view "Riwayat Ujian" membacanya
   - What's unclear: UAT flows 242-246 apakah akan test halaman riwayat ujian
   - Recommendation: Seed 2 record AttemptHistory untuk Rino (1 lulus, 1 gagal) agar flow riwayat testable.

3. **Sub-kategori AssessmentCategory untuk assessment reguler?**
   - What we know: AssessmentCategory punya hierarki parent-child (Phase 195). AssessmentSession.Category adalah string sederhana, bukan FK ke AssessmentCategory.
   - What's unclear: Context menyebut "sub-kategori" sebagai salah satu yang perlu di-seed
   - Recommendation: Seed beberapa AssessmentCategory rows (parent "Assessment OJT" + sub "Alkylation") sebagai master data, terpisah dari AssessmentSession.Category field.

## Sources

### Primary (HIGH confidence)
- `Data/SeedData.cs` — Pola idempotent seeding, IsDevelopment guard, CreateUsersAsync pattern
- `Models/AssessmentSession.cs` — Semua field yang perlu diisi untuk completed session
- `Models/AssessmentPackage.cs`, `PackageQuestion`, `PackageOption` — Entity chain soal
- `Models/UserPackageAssignment.cs` — ShuffledQuestionIds / ShuffledOptionIdsPerQuestion JSON fields
- `Models/PackageUserResponse.cs` — FK constraints (Restrict ke AssessmentSession)
- `Models/CoachCoacheeMapping.cs` — Unique index active-per-coachee
- `Models/ProtonModels.cs` — ProtonTrack, ProtonTrackAssignment structure
- `Models/SessionElemenTeknisScore.cs` — Unique index per (SessionId, ElemenTeknis)
- `Helpers/CertNumberHelper.cs` — Build(), GetNextSeqAsync(), format KPB/{seq:D3}/{roman}/{year}
- `Data/ApplicationDbContext.cs` — Semua DbSet definitions dan FK / index constraints

### Secondary (MEDIUM confidence)
- `241-CONTEXT.md` — Semua keputusan locked (D-01 s/d D-14)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah ada di project
- Architecture: HIGH — pola sudah established di SeedData.cs
- Pitfalls: HIGH — FK constraints dibaca langsung dari ApplicationDbContext

**Research date:** 2026-03-24
**Valid until:** 2026-04-24 (stable — tidak ada dependency eksternal yang berubah cepat)
