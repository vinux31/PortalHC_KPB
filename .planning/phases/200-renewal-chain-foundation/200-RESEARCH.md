# Phase 200: Renewal Chain Foundation - Research

**Researched:** 2026-03-19
**Domain:** EF Core migrations + LINQ query enhancement (ASP.NET Core / SQL Server)
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Renewal Chain Logic:**
- Semua renewal attempt tercatat di chain, baik gagal maupun lulus. IsRenewed hanya true jika ada renewal yang IsPassed==true (AssessmentSession) atau ada TrainingRecord renewal (selalu dianggap lulus).
- Satu renewal session/record hanya menunjuk ke SATU sertifikat asal — RenewsSessionId ATAU RenewsTrainingId, tidak keduanya sekaligus.
- Renewal bisa multi-level chain (A → B → C). Sertifikat hasil renewal bisa di-renew lagi.
- Renewal selalu via assessment baru ATAU TrainingRecord baru — kedua jalur didukung.
- TrainingRecord juga punya RenewsTrainingId (FK self) dan RenewsSessionId (FK ke AssessmentSession), mirror dari AssessmentSession. Chain bisa: Training → Training, Training → Assessment, Assessment → Assessment, Assessment → Training.
- TrainingRecord selalu dianggap "lulus" (data manual HC, tidak punya konsep IsPassed).

**FK Design:**
- Kedua FK nullable di masing-masing tabel (AssessmentSession dan TrainingRecord).
- Constraint salah-satu-saja (XOR) divalidasi di application code, bukan CHECK constraint di DB — konsisten dengan pattern existing.
- ON DELETE SET NULL — jika sertifikat asal dihapus, renewal FK jadi NULL, chain putus tapi data tetap ada.
- Index strategy: Claude's discretion berdasarkan query pattern.

**IsRenewed Flag Behavior:**
- Hanya bool IsRenewed di SertifikatRow, tanpa info tambahan (detail via Certificate History modal Phase 203).
- BuildSertifikatRowsAsync cek kedua tabel: sertifikat renewed jika ada AssessmentSession (IsPassed==true) ATAU TrainingRecord yang me-renew-nya.
- Sertifikat tanpa renewal yang lulus tetap IsRenewed=false meskipun ada attempt gagal.

### Claude's Discretion
- Index strategy untuk FK columns.
- Query optimization di BuildSertifikatRowsAsync (batch vs per-row lookup).

### Deferred Ideas (OUT OF SCOPE)
Tidak ada — diskusi tetap dalam scope phase.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| RENEW-01 | AssessmentSession memiliki field RenewsSessionId (FK self) dan RenewsTrainingId (FK ke TrainingRecord) untuk tracking renewal chain. TrainingRecord juga mendapat field mirror yang sama. | Migration pattern dengan nullable FK + OnDelete SetNull + index — lihat Architecture Patterns |
| RENEW-02 | Sertifikat dianggap "sudah di-renew" hanya jika ada AssessmentSession dengan RenewsSessionId/RenewsTrainingId mengarah ke sertifikat tersebut DAN IsPassed==true, ATAU ada TrainingRecord renewal | Batch lookup pattern di BuildSertifikatRowsAsync — lihat Architecture Patterns dan Code Examples |
</phase_requirements>

---

## Summary

Phase 200 menambahkan infrastruktur data yang mendasari seluruh fitur renewal di milestone v7.7. Dua bagian utama: (1) migrasi DB menambah FK columns ke AssessmentSession dan TrainingRecord, (2) enhancement BuildSertifikatRowsAsync untuk meng-compute flag IsRenewed per sertifikat.

Kode yang ada sudah matang untuk kedua pekerjaan ini. Migration pattern dengan nullable FK + AddForeignKey + CreateIndex sudah konsisten di seluruh codebase (contoh terbaru: AddParentAndSignatoryToAssessmentCategory). BuildSertifikatRowsAsync sudah batch-load data kedua tabel; menambah lookup renewal cukup menambahkan dua HashSet dari query terpisah, tanpa mengubah struktur query yang ada.

Self-referencing FK (AssessmentSession → AssessmentSession) perlu perhatian khusus karena SQL Server membatasi multiple cascade paths — solusinya adalah OnDelete SetNull yang sudah menjadi keputusan desain yang terkunci, dan ini aman.

**Primary recommendation:** Gunakan dua migration terpisah (satu per tabel) atau satu migration gabungan — gabungan lebih bersih karena kedua tabel saling FK silang. Resolusi IsRenewed menggunakan dua HashSet in-memory (satu untuk AS renewal IDs, satu untuk TR renewal IDs) yang dibangun sebelum loop mapping rows.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| EF Core (Microsoft.EntityFrameworkCore) | Sudah terpasang (lihat csproj) | ORM + migration scaffolding | Stack existing project |
| EF Core SQL Server | Sudah terpasang | SQL Server provider | Database target project |

Tidak ada library baru yang diperlukan untuk phase ini — semua pekerjaan menggunakan stack EF Core + LINQ yang sudah ada.

---

## Architecture Patterns

### Pola Migration: Nullable FK dengan ON DELETE SET NULL

Pola ini konsisten di codebase. Contoh terbaru (Phase 195 — AddParentAndSignatoryToAssessmentCategory):

```csharp
// Migration Up()
migrationBuilder.AddColumn<int>(
    name: "RenewsSessionId",
    table: "AssessmentSessions",
    type: "int",
    nullable: true);

migrationBuilder.AddColumn<int>(
    name: "RenewsTrainingId",
    table: "AssessmentSessions",
    type: "int",
    nullable: true);

migrationBuilder.CreateIndex(
    name: "IX_AssessmentSessions_RenewsSessionId",
    table: "AssessmentSessions",
    column: "RenewsSessionId");

migrationBuilder.CreateIndex(
    name: "IX_AssessmentSessions_RenewsTrainingId",
    table: "AssessmentSessions",
    column: "RenewsTrainingId");

migrationBuilder.AddForeignKey(
    name: "FK_AssessmentSessions_AssessmentSessions_RenewsSessionId",
    table: "AssessmentSessions",
    column: "RenewsSessionId",
    principalTable: "AssessmentSessions",
    principalColumn: "Id",
    onDelete: ReferentialAction.SetNull);

migrationBuilder.AddForeignKey(
    name: "FK_AssessmentSessions_TrainingRecords_RenewsTrainingId",
    table: "AssessmentSessions",
    column: "RenewsTrainingId",
    principalTable: "TrainingRecords",
    principalColumn: "Id",
    onDelete: ReferentialAction.SetNull);
```

### Pola OnModelCreating: Konfigurasi FK Tanpa Navigation Property

Pola protonTrackId pada AssessmentSession menunjukkan cara menambah FK tanpa navigation property (karena cascade complications). Untuk renewal chain, sama — cukup konfigurasi FK behavior, tanpa menambah navigation property:

```csharp
// Di ApplicationDbContext.OnModelCreating — dalam blok AssessmentSession entity
entity.Property(a => a.RenewsSessionId).IsRequired(false);
entity.Property(a => a.RenewsTrainingId).IsRequired(false);

entity.HasOne<AssessmentSession>()
    .WithMany()
    .HasForeignKey(a => a.RenewsSessionId)
    .OnDelete(DeleteBehavior.SetNull);

entity.HasOne<TrainingRecord>()
    .WithMany()
    .HasForeignKey(a => a.RenewsTrainingId)
    .OnDelete(DeleteBehavior.SetNull);
```

TrainingRecord mendapat konfigurasi serupa di blok TrainingRecord entity.

### Pola BuildSertifikatRowsAsync: Batch Lookup untuk IsRenewed

Pendekatan yang benar adalah batch lookup — load semua renewal IDs sekaligus sebelum mapping rows, bukan per-row query (N+1 problem). Pattern ini konsisten dengan cara BuildSertifikatRowsAsync sudah menggunakan ToListAsync() untuk batch queries:

```csharp
// SETELAH query trainingAnon dan assessmentAnon, SEBELUM mapping ke SertifikatRow

// Build renewal lookup: mana saja sertifikat yang sudah di-renew dengan lulus?
// "Renewed" = ada AS row dengan IsPassed==true yang menunjuk ke sertifikat ini
// ATAU ada TR row (selalu lulus) yang menunjuk ke sertifikat ini

// Set 1: AssessmentSession IDs yang sudah di-renew oleh assessment lain (IsPassed==true)
var renewedByAssessmentSessionIds = await _context.AssessmentSessions
    .Where(a => a.RenewsSessionId.HasValue && a.IsPassed == true)
    .Select(a => a.RenewsSessionId!.Value)
    .ToHashSetAsync();

// Set 2: AssessmentSession IDs yang sudah di-renew oleh training record
var renewedByTrainingAsSessionIds = await _context.TrainingRecords
    .Where(t => t.RenewsSessionId.HasValue)
    .Select(t => t.RenewsSessionId!.Value)
    .ToHashSetAsync();

// Set 3: TrainingRecord IDs yang sudah di-renew oleh assessment lain (IsPassed==true)
var renewedByAssessmentTrainingIds = await _context.AssessmentSessions
    .Where(a => a.RenewsTrainingId.HasValue && a.IsPassed == true)
    .Select(a => a.RenewsTrainingId!.Value)
    .ToHashSetAsync();

// Set 4: TrainingRecord IDs yang sudah di-renew oleh training record lain
var renewedByTrainingTrainingIds = await _context.TrainingRecords
    .Where(t => t.RenewsTrainingId.HasValue)
    .Select(t => t.RenewsTrainingId!.Value)
    .ToHashSetAsync();

// Gabung: semua AS IDs yang di-renew
var renewedAssessmentSessionIds = new HashSet<int>(
    renewedByAssessmentSessionIds.Union(renewedByTrainingAsSessionIds));

// Gabung: semua TR IDs yang di-renew
var renewedTrainingRecordIds = new HashSet<int>(
    renewedByAssessmentTrainingIds.Union(renewedByTrainingTrainingIds));
```

Kemudian saat mapping rows:

```csharp
// Saat membuat trainingRows:
IsRenewed = renewedTrainingRecordIds.Contains(t.Id)

// Saat membuat assessmentRows:
IsRenewed = renewedAssessmentSessionIds.Contains(a.Id)
```

### Anti-Patterns yang Harus Dihindari

- **Per-row renewal lookup (N+1):** Jangan query DB per-sertifikat untuk cek renewal — gunakan batch HashSet seperti di atas.
- **Navigation property ke renewal target:** Tidak perlu — cukup FK scalar property. Navigation property akan menyebabkan include chain yang rumit.
- **CHECK constraint XOR di DB:** Keputusan terkunci: validasi XOR di application code saja.
- **Cascade Delete:** Jangan gunakan — harus SetNull sesuai keputusan desain.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Self-FK EF config | Konfigurasi manual SQL | `entity.HasOne<AssessmentSession>().WithMany().HasForeignKey(...)` | EF sudah handle self-referencing FK dengan baik |
| HashSet dari query | Loop + Contains() | `ToHashSetAsync()` via EF | Built-in, optimal, satu round trip |
| Migration generation | Tulis migration manual | `dotnet ef migrations add` | Scaffolding otomatis dari model diff |

---

## Common Pitfalls

### Pitfall 1: Multiple Cascade Paths pada Self-FK

**Yang bisa salah:** SQL Server menolak ON DELETE CASCADE pada self-referencing FK karena multiple cascade paths dari satu tabel ke dirinya sendiri.

**Kenapa terjadi:** AssessmentSession sudah punya cascade dari User. Menambah cascade pada FK RenewsSessionId akan membuat SQL Server error "Introducing FOREIGN KEY constraint... may cause cycles or multiple cascade paths."

**Cara menghindari:** Selalu gunakan `OnDelete(DeleteBehavior.SetNull)` untuk kedua FK renewal — sudah menjadi keputusan desain yang terkunci. Jangan gunakan Cascade.

**Warning signs:** Error saat `dotnet ef database update` dengan pesan "cycles or multiple cascade paths".

### Pitfall 2: Cross-FK antara AssessmentSessions dan TrainingRecords

**Yang bisa salah:** Kedua tabel saling FK satu sama lain (AS → TR via RenewsTrainingId, TR → AS via RenewsSessionId). SQL Server bisa menolak jika keduanya pakai Cascade.

**Cara menghindari:** SetNull pada semua FK renewal di kedua tabel — sudah konsisten dengan keputusan desain.

### Pitfall 3: Lupa Menambah Konfigurasi di OnModelCreating

**Yang bisa salah:** Property ditambah ke model C# tapi lupa dikonfigurasi di `ApplicationDbContext.OnModelCreating`, sehingga EF tidak menghasilkan FK constraint yang benar di migration.

**Cara menghindari:** Setiap kali menambah FK property ke model, selalu tambahkan konfigurasi di `OnModelCreating` sebelum generate migration. Ikuti pola existing di blok `AssessmentSession` entity dan `TrainingRecord` entity.

### Pitfall 4: ToHashSetAsync() Availability

**Yang bisa salah:** `ToHashSetAsync()` memerlukan `using Microsoft.EntityFrameworkCore` atau tersedia via EF Core `IQueryable` extensions. Jika tidak, perlu `.ToListAsync()` kemudian `new HashSet<int>(list)`.

**Cara menghindari:** Cek apakah `ToHashSetAsync()` tersedia di versi EF Core yang digunakan. Jika tidak, gunakan `.ToListAsync()` + `new HashSet<int>(...)`. Kedua cara menghasilkan performa yang sama.

---

## Code Examples

### Menambah Property ke AssessmentSession Model

```csharp
// Models/AssessmentSession.cs — tambahkan di bagian bawah, setelah InterviewResultsJson

/// <summary>
/// FK ke AssessmentSession lain yang di-renew oleh session ini.
/// Nullable. Hanya salah satu dari RenewsSessionId/RenewsTrainingId yang boleh diisi.
/// ON DELETE SET NULL — jika sertifikat asal dihapus, FK jadi NULL.
/// </summary>
public int? RenewsSessionId { get; set; }

/// <summary>
/// FK ke TrainingRecord yang di-renew oleh session ini.
/// Nullable. Hanya salah satu dari RenewsSessionId/RenewsTrainingId yang boleh diisi.
/// ON DELETE SET NULL — jika sertifikat asal dihapus, FK jadi NULL.
/// </summary>
public int? RenewsTrainingId { get; set; }
```

### Menambah Property ke TrainingRecord Model

```csharp
// Models/TrainingRecord.cs — tambahkan di bagian bawah

/// <summary>
/// FK ke TrainingRecord lain yang di-renew oleh record ini (self-FK).
/// Nullable. Hanya salah satu dari RenewsTrainingId/RenewsSessionId yang boleh diisi.
/// </summary>
public int? RenewsTrainingId { get; set; }

/// <summary>
/// FK ke AssessmentSession yang di-renew oleh record ini.
/// Nullable. Hanya salah satu dari RenewsTrainingId/RenewsSessionId yang boleh diisi.
/// </summary>
public int? RenewsSessionId { get; set; }
```

### Menambah IsRenewed ke SertifikatRow

```csharp
// Models/CertificationManagementViewModel.cs — tambahkan di SertifikatRow class

/// <summary>
/// True jika ada sesi/record renewal yang lulus mengarah ke sertifikat ini.
/// Dihitung oleh BuildSertifikatRowsAsync. Orthogonal terhadap Status (Expired/AkanExpired).
/// </summary>
public bool IsRenewed { get; set; }
```

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Tidak ada test framework otomatis terdeteksi di project ini |
| Config file | none |
| Quick run command | Build check: `dotnet build` |
| Full suite command | `dotnet build` + manual browser verification |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RENEW-01 | AssessmentSession dan TrainingRecord memiliki kolom FK renewal di DB tanpa error | smoke | `dotnet build && dotnet ef database update` | ❌ Wave 0: verifikasi manual schema DB |
| RENEW-02 | BuildSertifikatRowsAsync mengembalikan IsRenewed=true hanya untuk sertifikat dengan renewal lulus | smoke | `dotnet build` + browser test CDP Certification Management | ❌ Wave 0: manual verification |

### Sampling Rate

- **Per task commit:** `dotnet build` — zero compilation errors
- **Per wave merge:** `dotnet build` + `dotnet ef migrations list` (semua migration applied)
- **Phase gate:** DB schema verified + IsRenewed logic verified via CDP Certification Management page

### Wave 0 Gaps

- [ ] Tidak ada test file baru yang diperlukan — pola verifikasi manual sesuai dengan project convention
- [ ] Jika EF migration gagal apply, cek SQL Server connection string dan verifikasi dengan `dotnet ef database update --verbose`

---

## Sources

### Primary (HIGH confidence)

- Kode existing `Models/AssessmentSession.cs` — struktur model saat ini, target penambahan
- Kode existing `Models/TrainingRecord.cs` — struktur model saat ini, target penambahan
- Kode existing `Models/CertificationManagementViewModel.cs` — SertifikatRow class
- Kode existing `Controllers/CDPController.cs` lines 3187-3336 — BuildSertifikatRowsAsync full implementation
- Kode existing `Data/ApplicationDbContext.cs` — OnModelCreating patterns, semua FK configurations
- Migration `20260318023131_AddParentAndSignatoryToAssessmentCategory.cs` — pola nullable FK + index + AddForeignKey SetNull terbaru

### Secondary (MEDIUM confidence)

- `.planning/phases/200-renewal-chain-foundation/200-CONTEXT.md` — semua keputusan desain sudah terkunci

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru, semua EF Core existing
- Architecture: HIGH — pola migration dan query sudah ada di codebase, tinggal replikasi
- Pitfalls: HIGH — cascade path issue SQL Server adalah masalah well-known, sudah dihindari oleh keputusan SetNull

**Research date:** 2026-03-19
**Valid until:** 2026-04-18 (stable — EF Core patterns tidak berubah cepat)
