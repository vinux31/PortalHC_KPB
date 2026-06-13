# Phase 372: Data Foundation + Propagasi Toggle - Research

**Researched:** 2026-06-13
**Domain:** ASP.NET Core 8 MVC + EF Core (SQL Server) + Razor — entity column add + EF migration (bool default) + form binding di 3 create-loop + sibling propagation di EditAssessment + 2 Bootstrap form-switch toggle di wizard Step 3 + summary Step 4
**Confidence:** HIGH (semua temuan terverifikasi langsung terhadap live code; tidak ada `[ASSUMED]`)

## Summary

Phase 372 adalah pondasi data murni untuk fitur Shuffle Toggle: menambah 2 kolom bool (`ShuffleQuestions`, `ShuffleOptions`) ke entity `AssessmentSession`, satu EF migration dengan `defaultValue: true` (baris lama otomatis ON), set kedua flag eksplisit dari form di SEMUA jalur create/propagate session, plus 2 toggle `form-switch` di wizard `CreateAssessment.cshtml` Langkah 3 Grup B dan baris status di summary Langkah 4. Tidak menyentuh engine baca shuffle (Phase 373) atau ManagePackages (Phase 374).

Proyek ini punya pola yang SANGAT JELAS dan dapat ditiru 1:1 untuk SEMUA aspek phase ini: kolom analog `AllowAnswerReview` (bool default true) sudah ada di entity + Fluent API `HasDefaultValue(true)` + migration `bit NOT NULL defaultValue:true`. Form model yang di-bind di `CreateAssessment`/`EditAssessment` POST adalah entity `AssessmentSession` itu sendiri (BUKAN ViewModel terpisah). Toggle UI meniru `IsTokenRequired` di view (`asp-for` checkbox + hidden companion). Test migration-default punya pola gold-standard di `OrgLabelMigrationIntegrationTests` (real SQL disposable DB + `MigrateAsync`).

**Temuan penting yang melebihi cakupan spec:** Spec menyebut "3 create loop + EditAssessment propagation". Live code mengungkap **7 site `new AssessmentSession` + 2 foreach propagation**. Beberapa adalah jalur "tambah peserta saat Edit" (1895/1914/2111) yang tidak disebut spec eksplisit. Planner WAJIB memutuskan apakah jalur-jalur ini masuk scope 372 (rekomendasi: YA untuk konsistensi data — lihat §Don't Hand-Roll & §Open Questions).

**Primary recommendation:** Tiru pola `AllowAnswerReview` end-to-end: (1) entity prop `= true`, (2) Fluent `HasDefaultValue(true)` di `ApplicationDbContext.cs:215`, (3) jalankan `dotnet ef migrations add AddShuffleTogglesToAssessmentSession` (auto-generate `defaultValue:true`), (4) set `= model.ShuffleQuestions`/`= model.ShuffleOptions` di SEMUA 7 site + 2 foreach, (5) toggle view meniru blok Token L500-519, (6) summary meniru baris/JS Token.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (D-01..D-11)

**Wizard Toggle (Langkah 3 CreateAssessment):**
- **D-01:** Penempatan = Grup B "Pengaturan Ujian" di Langkah 3, sejajar `IsTokenRequired`. Pakai pola `form-check form-switch` (`CreateAssessment.cshtml:505-508`). TANPA card/grup baru.
- **D-02:** Label + penjelasan detail (form-text edukatif untuk HC non-teknis), bukan label saja. Copy final = lihat 372-UI-SPEC.md (sudah locked verbatim di sana).
- **D-03:** Default kedua toggle = checked (ON) di wizard.

**Cakupan Pre/Post saat Create:**
- **D-04:** Saat create Pre-Post Test, 1 pasang toggle di Langkah 3 → nilai sama di-set ke loop Pre DAN loop Post. Tidak ada toggle terpisah Pre/Post di wizard. Divergensi Pre≠Post terjadi BELAKANGAN di ManagePackages (Phase 374).

**Langkah 4 Konfirmasi:**
- **D-05:** Status ON/OFF kedua toggle DITAMPILKAN di summary Langkah 4.

**Locked dari Spec:**
- **D-06:** Default dua-duanya ON via `defaultValue:true` — janji "data lama tak berubah".
- **D-07:** Kolom `bit NOT NULL DEFAULT 1` untuk kedua kolom; nama migration `AddShuffleTogglesToAssessmentSession`.
- **D-08:** EF bool trap — `bool` default C# = `false`; migration defaultValue cuma benerin baris LAMA. Form WAJIB set eksplisit di SEMUA loop create.
- **D-09:** Propagasi sibling ikut pola `foreach` `EditAssessment` POST.
- **D-10:** Acak Soal & Acak Pilihan = independen (boleh beda).
- **D-11:** Grading aman — pakai `PackageOption.Id` (bukan posisi huruf). Phase 372 cuma nambah kolom, tak sentuh grading.

### Claude's Discretion
- Display attribute / property naming exact di entity (`[Display(Name=...)]` per spec §4).
- Copy/teks final penjelasan toggle (catatan: SUDAH di-finalkan verbatim di 372-UI-SPEC.md §Copywriting Contract).
- Nama field form / binding di view ↔ model (`name="ShuffleQuestions"` dll).
- Format visual summary Langkah 4 (badge/teks) — UI-SPEC merekomendasikan teks polos "Aktif (ON)"/"Nonaktif (OFF)".

### Deferred Ideas (OUT OF SCOPE)
- **Toggle terpisah Pre vs Post di wizard** — ditolak (D-04).
- Semua logic baca/reshuffle/UI ManagePackages/lock/warning/reminder = scope Phase 373/374/375 (out-of-scope phase ini, BUKAN deferred).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **SHUF-01** | Kolom `ShuffleQuestions` + `ShuffleOptions` (bool) di `AssessmentSession`; migration `defaultValue:true` → baris lama ON dua-duanya. | Pola analog persis: `AllowAnswerReview` (entity `Models/AssessmentSession.cs:33` + Fluent `ApplicationDbContext.cs:215` + migration `20260214011828_AddAssessmentResultFields.cs:14-19`). Migration template literal: `20260311012214_AddGenerateCertificateToAssessmentSession.cs` (single bool, `bit`, `defaultValue:true`, table `AssessmentSessions`). |
| **SHUF-02** | Form CreateAssessment set kedua flag eksplisit (default checked) di SEMUA loop create (standard/Pre/Post) — hindari EF bool-false trap. | 3 site create POST terverifikasi: standard `AssessmentAdminController.cs:1427`, Pre `:1218`, Post `:1252`. Semua sudah set `IsTokenRequired/PassPercentage/AllowAnswerReview = model.*` — shuffle ditambah sama persis. View toggle meniru Token `CreateAssessment.cshtml:500-519`. |
| **SHUF-03** | Ubah toggle propagate ke semua sibling grup (pola `foreach` EditAssessment POST). | 2 foreach propagation terverifikasi: standard `AssessmentAdminController.cs:2016-2031`, Pre-Post `:1797-1806`. Plus 3 site "tambah peserta saat Edit" (1895/1914/2111) yang spec tak sebut — lihat §Open Questions. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Kolom data shuffle (persist flag) | Database / EF entity | — | Field per-session disimpan di tabel `AssessmentSessions`; default DB di-enforce via migration `bit NOT NULL DEFAULT 1`. |
| Migration backfill baris lama | Database (migration DDL) | — | `defaultValue:true` di `AddColumn` → SQL Server isi baris existing dgn `1`. Bukan tanggung jawab kode app. |
| Set flag dari form (create) | API / Backend (controller POST) | — | EF bool-trap: model binder + assignment eksplisit di tiap `new AssessmentSession`. Murni server-side. |
| Propagasi ke sibling (edit) | API / Backend (controller POST) | — | `foreach siblings` set field assessment-level di SETIAP baris. |
| Toggle input + help-text | Frontend Server (Razor view) | Browser (form-switch UX) | `asp-for` checkbox di-render server-side; interaksi switch + companion hidden field native Bootstrap/MVC. |
| Summary read-only Step 4 | Browser (client JS) | Frontend Server (markup `dt/dd`) | `populateSummary()` JS membaca `.checked` dan menulis ke span — pure client-side mirror, tidak roundtrip server. |

**Catatan:** Phase ini lintas 3 tier (DB + Backend + Frontend) tapi setiap capability punya owner tunggal yang jelas. Tidak ada misassignment risk — toggle UI TIDAK boleh menyimpan state sendiri (state hanya ada di posted form → entity).

## Standard Stack

Phase ini TIDAK menambah library/package baru. Semua memakai stack yang sudah terpasang.

### Core (verified terpasang)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 8.0.418 | Runtime + build | `[VERIFIED: dotnet --version]`. Project target `net8.0` (`HcPortal.Tests.csproj:4`). |
| EF Core (SqlServer) | 8.0.0 | ORM + migrations | `[VERIFIED: HcPortal.Tests.csproj:13]`. DbContext = `ApplicationDbContext` (`Data/ApplicationDbContext.cs:10`). |
| EF Core .NET CLI tools | 10.0.3 | `dotnet ef migrations add` | `[VERIFIED: dotnet ef --version]`. Backward-compatible dengan net8 target. |
| Bootstrap | 5.3 | form-switch, grid, card | `[CITED: 372-UI-SPEC.md]`. Admin theme vendored, bukan registry pihak ketiga. |
| Bootstrap Icons | (bi-*) | ikon `bi-shuffle` di sub-heading | `[VERIFIED: CreateAssessment.cshtml:514 bi-shuffle sudah dipakai]`. |
| xUnit | 2.9.3 | test framework | `[VERIFIED: HcPortal.Tests.csproj:15]`. |
| EF Core InMemory | 8.0.0 | unit test (logic) | `[VERIFIED: HcPortal.Tests.csproj:12]`. CATATAN: InMemory TIDAK eksekusi migration DDL — tak bisa test DB default. |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Bind ke entity `AssessmentSession` | ViewModel `CreateAssessmentVm` terpisah | Proyek SUDAH bind entity langsung di SEMUA action (terverifikasi sig POST `:846-852` & `:1766-1769`). Memperkenalkan ViewModel = inkonsistensi + scope creep. JANGAN. |
| Fluent `HasDefaultValue(true)` | Hanya hand-edit migration `defaultValue:true` (pola GenerateCertificate) | `GenerateCertificate` TIDAK punya Fluent default → snapshot (`ApplicationDbContextModelSnapshot.cs:420-421`) tak punya anotasi default → drift. `AllowAnswerReview` PUNYA Fluent → snapshot konsisten. Tiru `AllowAnswerReview`, BUKAN `GenerateCertificate`. |

**Installation:** Tidak ada `npm install` / `dotnet add package`. Stack lengkap.

**Version verification:** `[VERIFIED]` semua versi via file `.csproj` + `dotnet --version`/`dotnet ef --version` di sesi ini (2026-06-13). Tidak ada package baru yang perlu di-cek registry.

## Architecture Patterns

### System Architecture Diagram (data flow Phase 372)

```
                          ┌─────────────────────────────────────────┐
   HC buka wizard  ──────▶│ GET CreateAssessment (controller :651)  │
                          │   new AssessmentSession{...}            │
                          │   ShuffleQuestions/Options unset        │
                          │   → entity default (= true) → checked   │
                          └──────────────────┬──────────────────────┘
                                             │ render
                                             ▼
                  ┌──────────────────────────────────────────────────┐
                  │ CreateAssessment.cshtml                           │
                  │  Langkah 3 Grup B: <input asp-for="Shuffle..."   │
                  │    type=checkbox> + hidden(false) companion      │
                  │  Langkah 4: populateSummary() baca .checked      │
                  │    → tulis "Aktif (ON)"/"Nonaktif (OFF)"         │
                  └──────────────────┬───────────────────────────────┘
                                     │ POST form (checkbox→bool)
                                     ▼
   ┌─────────────────────────────────────────────────────────────────────────┐
   │ POST CreateAssessment(AssessmentSession model, ...)  (:846)               │
   │   model.ShuffleQuestions / model.ShuffleOptions  ← bound dari form        │
   │                                                                           │
   │   ┌── standard mode ──────────┐   ┌── Pre-Post mode ──────────────────┐   │
   │   │ loop :1424                │   │ Pre loop :1216  → preSession      │   │
   │   │  session.Shuffle* =       │   │   .Shuffle* = model.Shuffle*      │   │
   │   │     model.Shuffle*  (NEW) │   │ Post loop :1250 → postSession     │   │
   │   └───────────────────────────┘   │   .Shuffle* = model.Shuffle*      │   │
   │                                    └───────────────────────────────────┘   │
   └────────────────────────────────┬──────────────────────────────────────────┘
                                     │ AddRange + SaveChanges (tx)
                                     ▼
                       ┌───────────────────────────────┐
                       │  tabel AssessmentSessions     │
                       │  ShuffleQuestions bit DEFAULT 1│  ◀── migration backfill baris lama
                       │  ShuffleOptions   bit DEFAULT 1│
                       └───────────────────────────────┘
                                     ▲
                                     │ propagate ke SEMUA sibling
   ┌─────────────────────────────────┴─────────────────────────────────────────┐
   │ POST EditAssessment(AssessmentSession model, ...)  (:1766)                  │
   │   standard branch  foreach sibling :2016 → sibling.Shuffle* = model.Shuffle*│
   │   Pre-Post branch  foreach s :1797      → s.Shuffle*       = model.Shuffle* │
   │   (+ jalur tambah-peserta :1895/:1914/:2111 — lihat Open Questions)         │
   └─────────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| File | Site (line) | Tindakan Phase 372 |
|------|-------------|--------------------|
| `Models/AssessmentSession.cs` | setelah `:36` (dekat `GenerateCertificate`) atau ujung `:193` | Tambah 2 prop bool `= true` + `[Display(...)]`. |
| `Data/ApplicationDbContext.cs` | setelah `:215` (blok "Default values" AssessmentSession) | Tambah 2 `entity.Property(a => a.Shuffle*).HasDefaultValue(true)`. |
| `Migrations/<ts>_AddShuffleTogglesToAssessmentSession.cs` | file baru (auto-gen) | `AddColumn<bool>` × 2, `type:"bit"`, `nullable:false`, `defaultValue:true`. |
| `Controllers/AssessmentAdminController.cs` | `:1427` (standard create loop) | `ShuffleQuestions = model.ShuffleQuestions, ShuffleOptions = model.ShuffleOptions`. |
| ″ | `:1218` (Pre create loop) | idem (D-04: nilai sama Pre & Post). |
| ″ | `:1252` (Post create loop) | idem. |
| ″ | `:2016-2031` (standard sibling foreach) | `sibling.Shuffle* = model.Shuffle*`. |
| ″ | `:1797-1806` (Pre-Post group foreach) | `s.Shuffle* = model.Shuffle*`. |
| ″ | `:1895`/`:1914`/`:2111` (tambah-peserta saat Edit) | **DECIDE (lihat Open Questions)** — set dari `model.*` / representative supaya peserta baru tak OFF. |
| `Views/Admin/CreateAssessment.cshtml` | Grup B `:500-519` (setelah blok Token, kolom baru `col-md-6`) | 2 `form-switch` + sub-heading `bi-shuffle` + 2 help-text (copy verbatim UI-SPEC). |
| ″ | summary standard `:653-661` + Pre-Post `:691-697` | 2 baris `dt/dd` per blok, setelah Pass Percentage. |
| ″ | `populateSummary()` `:1053` (cabang isPrePost `:1119`+ dan standard `:1160`+) | isi span shuffle dari `.checked`, pola Token L1160-1163. |

### Pattern 1: Bool-with-default kolom (analog `AllowAnswerReview`)
**What:** Tambah kolom bool yang baris lama = true, kode app set eksplisit.
**When to use:** SHUF-01.
**Example (verified live triplet — tiru 1:1):**
```csharp
// 1) Models/AssessmentSession.cs:32-33 (entity prop, default = true di C#)
[Display(Name = "Allow Answer Review")]
public bool AllowAnswerReview { get; set; } = true;

// 2) Data/ApplicationDbContext.cs:215 (Fluent → bikin migration auto-emit defaultValue + snapshot konsisten)
entity.Property(a => a.AllowAnswerReview).HasDefaultValue(true);

// 3) Migrations/20260214011828_AddAssessmentResultFields.cs:14-19 (migration DDL hasil generate)
migrationBuilder.AddColumn<bool>(
    name: "AllowAnswerReview",
    table: "AssessmentSessions",
    type: "bit",
    nullable: false,
    defaultValue: true);
```
Untuk Phase 372: ganti `AllowAnswerReview` → `ShuffleQuestions` lalu ulang untuk `ShuffleOptions`. `[Display(Name="Acak Soal")]` / `[Display(Name="Acak Pilihan Jawaban")]` per spec §4.

### Pattern 2: Form bound ke entity, set field assessment-level di tiap create loop
**What:** `AssessmentSession model` di-bind langsung dari form; tiap `new AssessmentSession` mengisi field dari `model.*`.
**When to use:** SHUF-02.
**Example (verified `CreateAssessment.cshtml` standard loop :1435-1439):**
```csharp
var session = new AssessmentSession {
    // ...
    IsTokenRequired = model.IsTokenRequired,
    PassPercentage = model.PassPercentage,
    AllowAnswerReview = model.AllowAnswerReview,
    GenerateCertificate = model.GenerateCertificate,
    // TAMBAH:
    ShuffleQuestions = model.ShuffleQuestions,
    ShuffleOptions = model.ShuffleOptions,
};
```

### Pattern 3: Sibling propagation di EditAssessment
**What:** Fetch semua sibling (key `(Title,Category,Schedule.Date)`), set field shared di SETIAP baris.
**When to use:** SHUF-03.
**Example (verified standard branch :2016-2031):**
```csharp
foreach (var sibling in siblings) {
    sibling.IsTokenRequired = model.IsTokenRequired;
    sibling.PassPercentage = model.PassPercentage;
    sibling.AllowAnswerReview = model.AllowAnswerReview;
    sibling.GenerateCertificate = model.GenerateCertificate;
    // TAMBAH:
    sibling.ShuffleQuestions = model.ShuffleQuestions;
    sibling.ShuffleOptions = model.ShuffleOptions;
}
```
Pre-Post branch (`:1797-1806`) pola identik di `foreach (var s in allGroupSessions)`.

### Pattern 4: form-switch toggle (analog `IsTokenRequired`)
**What:** Bootstrap form-switch dengan `asp-for` checkbox.
**When to use:** wizard UI (D-01).
**Example (verified `CreateAssessment.cshtml:505-508`):**
```html
<div class="form-check form-switch mb-2">
    <input class="form-check-input" type="checkbox" asp-for="IsTokenRequired" id="IsTokenRequired" />
    <label class="form-check-label" for="IsTokenRequired">Wajib token untuk memulai ujian</label>
</div>
```
Copy final + struktur kolom + help-text untuk shuffle = LOCKED verbatim di `372-UI-SPEC.md` (Copywriting Contract). Executor TIDAK boleh memangkas copy.

### Pattern 5: Summary JS mirror (analog Token)
**What:** `populateSummary()` baca `.checked` checkbox → tulis teks ke span summary.
**When to use:** Step 4 summary (D-05).
**Example (verified `:1160-1163` standard cabang):**
```javascript
var tokenEl = document.getElementById('IsTokenRequired');
var summToken = document.getElementById('summary-token');
if (summToken) summToken.textContent = (tokenEl && tokenEl.checked) ? 'Ya — ...' : 'Tidak';
```
Untuk shuffle: `(el && el.checked) ? 'Aktif (ON)' : 'Nonaktif (OFF)'`. WAJIB ditambah di KEDUA cabang `populateSummary()`: isPrePost (`:1119`+) DAN standard (`:1160`+), karena 1 pasang toggle berlaku Pre & Post (D-04).

### Anti-Patterns to Avoid
- **Bikin ViewModel terpisah** untuk wizard — proyek bind entity langsung; ViewModel = inkonsistensi.
- **Hand-edit migration `defaultValue:true` tanpa Fluent** (pola GenerateCertificate) — bikin model-snapshot drift. Tambahkan Fluent `HasDefaultValue(true)` dulu, baru `dotnet ef migrations add`.
- **Andalkan default browser checkbox** untuk state ON — UI-SPEC D-08: nilai harus datang dari model (entity default `= true`), bukan atribut `checked` hardcoded.
- **Badge hijau/merah** untuk ON/OFF di summary — UI-SPEC: pakai teks polos; ON & OFF dua-duanya valid (bukan benar/salah).
- **Set shuffle hanya di 3 create loop** lalu lupa jalur tambah-peserta (1895/1914/2111) — peserta baru yang ditambah saat Edit bisa OFF (EF bool-trap kedua). Lihat Open Questions.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Backfill baris lama → true | UPDATE script manual / loop C# | `migrationBuilder.AddColumn(..., defaultValue: true)` | SQL Server isi baris existing otomatis saat ADD COLUMN dengan DEFAULT. Pola `AllowAnswerReview` & `GenerateCertificate` sudah membuktikan. |
| Generate migration DDL | Tulis `AddColumn` manual dari nol | `dotnet ef migrations add AddShuffleTogglesToAssessmentSession --context ApplicationDbContext` | Auto-emit DDL + update `ApplicationDbContextModelSnapshot.cs`. Hand-writing = lupa snapshot. |
| Checkbox unchecked → false binding | Manual hidden input | `asp-for` tag helper | MVC `asp-for` pada bool non-nullable otomatis emit `<input type="hidden" value="false">` companion → unchecked POST `false` (bukan "tidak terkirim"). |
| Test DB default backfill | Assert di InMemory DB | Real-SQL fixture (`IAsyncLifetime` + `UseSqlServer` + `MigrateAsync`) | InMemory bypass migration pipeline (bangun schema dari model) → TIDAK bisa membuktikan `defaultValue:true`. Pola `OrgLabelMigrationFixture` / `ProtonCompletionFixture`. |

**Key insight:** Setiap aspek phase ini punya pola identik yang sudah hidup di codebase (kolom `AllowAnswerReview` end-to-end + toggle `IsTokenRequired` + summary Token + fixture migration). Phase 372 = "salin pola, ganti nama" — risiko terbesar BUKAN teknis tapi KELENGKAPAN (menemukan SEMUA write-site).

## Runtime State Inventory

> Phase 372 menambah kolom (skema), bukan rename/migrasi data string. Tapi karena ada DB-state, kategori dicek eksplisit.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Tabel `AssessmentSessions` di **DB lokal** (`HcPortalDB_Dev` pada `localhost\SQLEXPRESS`) — semua baris existing harus dapat `ShuffleQuestions=1`/`ShuffleOptions=1` setelah migration. | `dotnet ef database update` lokal → verifikasi baris lama = 1 (data migration via `defaultValue`, BUKAN code edit). |
| Live service config | None — tidak ada config eksternal (n8n/Datadog/dll) yang menyimpan flag shuffle. Verified: fitur baru, belum ada konsumen. | None. |
| OS-registered state | None — tidak ada task scheduler / service yang refer kolom ini. | None. |
| Secrets/env vars | None — tidak ada secret/env var terkait shuffle. (Catatan AD lokal: `Authentication__UseActiveDirectory=false dotnet run` untuk UAT, per memory — relevan saat `dotnet run`, bukan kolom ini.) | None. |
| Build artifacts | `ApplicationDbContextModelSnapshot.cs` — WAJIB ter-update oleh `dotnet ef migrations add` (kalau hand-edit migration, snapshot stale). | Pakai CLI `migrations add` (bukan tulis tangan) → snapshot auto-update. |

**DB Dev & Prod:** Promosi migration ke server Dev (10.55.3.3) + DB Dev/Prod = tanggung jawab Team IT (CLAUDE.md DEV_WORKFLOW step 5). Developer HANYA migrate DB lokal + notifikasi IT dengan commit hash + flag migration.

## Common Pitfalls

### Pitfall 1: EF bool-false trap (assessment BARU malah OFF)
**What goes wrong:** Migration `defaultValue:true` hanya berlaku saat DB meng-insert baris TANPA nilai untuk kolom itu. Tapi EF SELALU mengirim nilai C# (`false` jika tak di-set) saat `SaveChanges` pada object baru → DB default di-bypass → assessment baru tersimpan OFF.
**Why it happens:** `bool` default C# = `false`. `defaultValue` SQL hanya untuk row tanpa kolom dalam INSERT; EF menyertakan kolom.
**How to avoid:** (a) entity prop `= true` (object init default ON), (b) set eksplisit `= model.Shuffle*` di SETIAP `new AssessmentSession` jalur create/propagate. Verified jalur: 1218, 1252, 1427 (+ 1895/1914/2111 jalur tambah-peserta).
**Warning signs:** Buat assessment baru lewat wizard → cek DB lokal: `ShuffleQuestions`/`ShuffleOptions` = 0 padahal toggle dibiarkan ON.

### Pitfall 2: Lupa jalur write-site (lebih dari 3 loop)
**What goes wrong:** Spec menyebut "3 loop create". Live code punya **7 `new AssessmentSession`** + 2 foreach. Mengabaikan jalur tambah-peserta-saat-Edit (1895/1914/2111) → peserta yang ditambah belakangan bisa OFF (bug intermiten yang lolos test "create baru").
**Why it happens:** Spec di-tulis dari memori arsitektur, bukan grep ekshaustif.
**How to avoid:** Planner enumerasi SEMUA `new AssessmentSession` (daftar lengkap di §Runtime/Component table). Untuk jalur tambah-peserta: standard (`:2111`) copy dari `savedAssessment.Shuffle*` (sudah ter-propagate, aman); Pre-Post (`:1895/1914`) copy dari `model.Shuffle*` atau representative `repPre/repPost.Shuffle*`.
**Warning signs:** Tambah peserta ke assessment existing via Edit → peserta baru OFF padahal grup ON.

### Pitfall 3: Model-snapshot drift (hand-edit migration)
**What goes wrong:** Tulis migration `defaultValue:true` manual tanpa Fluent `HasDefaultValue` → `ApplicationDbContextModelSnapshot.cs` tak mencatat default → migration berikutnya bisa men-generate ulang/aneh.
**Why it happens:** Snapshot = source of truth model; hanya `dotnet ef migrations add` + Fluent yang sinkron. `GenerateCertificate` adalah contoh drift existing (snapshot `:420-421` tanpa default).
**How to avoid:** Tambah Fluent `HasDefaultValue(true)` di `ApplicationDbContext.cs:215`, LALU `dotnet ef migrations add` (auto-emit DDL + snapshot benar). Tiru `AllowAnswerReview` (snapshot `:375-377` punya `ValueGeneratedOnAdd()`), BUKAN `GenerateCertificate`.
**Warning signs:** `dotnet ef migrations add` menghasilkan migration kosong/tak terduga, atau snapshot diff tak mengandung anotasi default untuk kolom shuffle.

### Pitfall 4: File-overlap lintas-sesi (v25.0 aktif)
**What goes wrong:** `AssessmentAdminController.cs` dipakai Phase 367/368 (sesi paralel). Eksekusi 372 sebelum 367/368 ship → konflik merge.
**Why it happens:** Worktree paralel, file sama.
**How to avoid:** JANGAN `/gsd-execute-phase 372` sebelum 367/368 ship atau merge dikoordinasi (CONTEXT.md §Constraint Koordinasi). Sequential strict v27.0: 372→373→374→375. JANGAN `/gsd-new-milestone`/`/gsd-complete-milestone` vanilla (clobber STATE/phases v25.0).
**Warning signs:** Diff `AssessmentAdminController.cs` di branch lain menyentuh baris 1200-2130.

## Code Examples

### EF migration workflow (verified DEV_WORKFLOW.md:57-65)
```bash
# 1) Tambah migration (auto-generate DDL + update snapshot)
dotnet ef migrations add AddShuffleTogglesToAssessmentSession --context ApplicationDbContext

# 2) Apply ke DB lokal
dotnet ef database update --context ApplicationDbContext

# 3) Verifikasi lokal (CLAUDE.md)
dotnet build          # tanpa error/warning baru
dotnet run            # cek http://localhost:5277  (AD lokal: Authentication__UseActiveDirectory=false dotnet run)
```

### Verifikasi backfill baris lama (DB lokal)
```sql
-- baris LAMA (dibuat sebelum migration) harus 1 dua-duanya
SELECT TOP 5 Id, ShuffleQuestions, ShuffleOptions FROM AssessmentSessions ORDER BY Id;
```

### Test migration-default (pola gold-standard OrgLabelMigrationIntegrationTests)
```csharp
// Source: HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs (real-SQL disposable)
public class ShuffleMigrationFixture : IAsyncLifetime {
    public string DbName { get; } = $"HcPortalDB_Test_{Guid.NewGuid():N}";
    public DbContextOptions<ApplicationDbContext> Options => _options;
    // _cs = "Server=localhost\\SQLEXPRESS;Database={DbName};Integrated Security=True;TrustServerCertificate=True;..."
    public async Task InitializeAsync() {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(_cs).Options;
        await using var ctx = new ApplicationDbContext(_options);
        await ctx.Database.MigrateAsync();   // jalankan PIPELINE (bukan schema-from-model) → buktikan defaultValue:true
    }
    public async Task DisposeAsync() { /* EnsureDeletedAsync */ }
}

[Trait("Category","Integration")]
public class ShuffleMigrationTests : IClassFixture<ShuffleMigrationFixture> {
    [Fact]
    public async Task Migration_BackfillsOldRows_ToTrue() {
        await using var ctx = new ApplicationDbContext(_fixture.Options);
        // Insert baris via raw SQL TANPA kolom shuffle (simulasi baris lama) → DB default isi 1
        await ctx.Database.ExecuteSqlRawAsync(
            "INSERT INTO AssessmentSessions (UserId,Title,Category,Schedule,DurationMinutes,Status,...) VALUES (...)");
        var row = await ctx.AssessmentSessions.OrderByDescending(s=>s.Id).FirstAsync();
        Assert.True(row.ShuffleQuestions);   // backfill default = true
        Assert.True(row.ShuffleOptions);
    }
}
```
> Catatan: `[Trait("Category","Integration")]` → CI tanpa SQL bisa skip `--filter "Category!=Integration"`.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Migration default hand-edit (GenerateCertificate) | Fluent `HasDefaultValue` + `migrations add` (AllowAnswerReview) | — | Snapshot konsisten; phase 372 PAKAI yang current. |
| InMemory test untuk semua | Real-SQL disposable fixture untuk migration/DB-default (Phase 344+) | Phase 344 (OrgLabel) | Migration default WAJIB diuji real-SQL, bukan InMemory. |

**Deprecated/outdated:** Tidak ada library deprecated relevan. EF Core 8 `AddColumn defaultValue` adalah API stabil/current.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | — |

**Tabel kosong:** SEMUA klaim di research ini diverifikasi langsung terhadap live code (file + line) atau tool (dotnet/ef version) dalam sesi 2026-06-13. Tidak ada klaim `[ASSUMED]`. Tidak ada keputusan yang butuh konfirmasi user — semua sudah locked di CONTEXT.md + UI-SPEC.md, KECUALI satu scope-completeness question (jalur tambah-peserta) di bawah.

## Open Questions

1. **Apakah jalur "tambah peserta saat Edit" (1895/1914/2111) masuk scope Phase 372?**
   - What we know: Spec/CONTEXT menyebut "3 create loop + EditAssessment propagation foreach". Live code punya 3 site TAMBAHAN `new AssessmentSession` di jalur tambah-peserta saat Edit: Pre-Post `newPre` (`:1895`) + `newPost` (`:1914`), standard `newSessions` (`:2111`). Site-site ini saat ini set `AllowAnswerReview/PassPercentage/GenerateCertificate` dari `model.*` / `savedAssessment.*` tapi BELUM shuffle.
   - What's unclear: Apakah planner memperlakukan ini sebagai bagian SHUF-02/03 (konsistensi data) atau menahannya.
   - Recommendation: **MASUKKAN ke scope 372.** Tanpa ini, peserta yang ditambah ke assessment existing lewat Edit akan ter-`false` (EF bool-trap kedua, Pitfall 2) → bug data senyap. Set `newPre/newPost.Shuffle* = model.Shuffle*` (atau `repPre/repPost.Shuffle*`) dan `newSessions.Shuffle* = savedAssessment.Shuffle*` (sudah ter-propagate). Biaya kecil, mencegah bug. Jika planner ragu, angkat ke user — tapi default rekomendasi = sertakan.

2. **GET CreateAssessment (`:684`) perlu set shuffle eksplisit?**
   - What we know: GET model init (`:684-690`) TIDAK set `ShuffleQuestions/Options` — tapi entity default `= true` membuatnya ON otomatis saat render.
   - What's unclear: Apakah eksplisit lebih aman.
   - Recommendation: TIDAK perlu set eksplisit di GET (entity default `= true` cukup; konsisten dgn `AllowAnswerReview` yang juga set di `:689`). Boleh ditambahkan demi kejelasan, tapi bukan keharusan fungsional. Toggle render `checked` dari model default.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/run/migration | ✓ | 8.0.418 | — |
| EF Core CLI (`dotnet ef`) | `migrations add` / `database update` | ✓ | 10.0.3 | — |
| SQL Server (localhost\SQLEXPRESS) | DB lokal + test fixture | ✓ (asumsi running per pola test existing) | — | `dotnet run` AD off untuk UAT |
| HcPortalDB_Dev (lokal) | verifikasi backfill | ✓ (DB shared lokal) | — | snapshot/restore per SEED_WORKFLOW bila seed |

**Missing dependencies with no fallback:** None — semua tooling terpasang & terverifikasi sesi ini.
**Missing dependencies with fallback:** None.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (`HcPortal.Tests.csproj`) |
| Config file | none (xunit auto-discover); SqlServer + InMemory EF providers terpasang |
| Quick run command | `dotnet test --filter "Category!=Integration"` (skip real-SQL, cepat) |
| Full suite command | `dotnet test` (incl. Integration real-SQL) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SHUF-01 | Migration backfill baris lama → `ShuffleQuestions=true`/`ShuffleOptions=true` | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ShuffleMigration"` | ❌ Wave 0 |
| SHUF-01 | Kolom map + queryable lewat EF setelah migration (smoke) | integration (real-SQL) | idem | ❌ Wave 0 |
| SHUF-02 | New assessment via create-loop persist ON (default), TIDAK ter-EF-false-trap | integration (real-SQL) atau controller-level | `dotnet test --filter "FullyQualifiedName~ShuffleCreate"` | ❌ Wave 0 |
| SHUF-02 | New assessment dengan toggle OFF persist OFF (eksplisit false dari form) | integration (real-SQL) | idem | ❌ Wave 0 |
| SHUF-03 | EditAssessment ubah toggle → SEMUA sibling ikut (standard + Pre-Post branch) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~ShufflePropagation"` | ❌ Wave 0 |
| SHUF-03 | Tambah-peserta saat Edit → peserta baru dapat nilai grup (bukan OFF) — JIKA Open Q#1 masuk scope | integration (real-SQL) | idem | ❌ Wave 0 |

> Catatan: SHUF-01 default backfill HARUS real-SQL — InMemory tidak eksekusi migration DDL (bukti: `OrgLabelMigrationIntegrationTests` komentar L11-16). Toggle UI render + summary JS = UAT Playwright (scope Phase 375, BUKAN 372 — CONTEXT §domain), tapi planner boleh smoke-render lokal `dotnet run`.

### Sampling Rate
- **Per task commit:** `dotnet build` (0 error/warning baru) + `dotnet test --filter "Category!=Integration"` (unit cepat)
- **Per wave merge:** `dotnet test` (full incl. real-SQL migration test)
- **Phase gate:** Full suite green + migration applied lokal + baris lama terverifikasi `1` (SQL query) sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ShuffleMigrationTests.cs` (+ `ShuffleMigrationFixture`) — covers SHUF-01 default backfill (pola `OrgLabelMigrationFixture` / reuse `ProtonCompletionFixture` disposable real-SQL)
- [ ] `HcPortal.Tests/ShuffleCreatePersistenceTests.cs` — covers SHUF-02 (ON default + OFF eksplisit persist)
- [ ] `HcPortal.Tests/ShufflePropagationTests.cs` — covers SHUF-03 (sibling propagation standard + Pre-Post)
- [ ] Reuse fixture: bisa pakai `ProtonCompletionFixture` (disposable `HcPortalDB_Test_<guid>` + `MigrateAsync`) — tidak perlu fixture baru jika cukup; bikin baru hanya jika butuh seed AssessmentSession khusus.

*(Framework sudah terpasang — TIDAK perlu install. Hanya file test baru.)*

## Security Domain

> `security_enforcement` tidak di-set false di config.json → enabled. Phase 372 berdampak kecil pada attack surface (tambah kolom bool + form field), tapi dicek.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak menyentuh auth. |
| V3 Session Management | no | — |
| V4 Access Control | yes | POST `CreateAssessment`/`EditAssessment` SUDAH `[Authorize(Roles="Admin, HC")]` (`:844`, `:1764`) + `[ValidateAntiForgeryToken]`. Phase 372 TIDAK menambah endpoint baru (endpoint `UpdateShuffleSettings` = scope 374). Toggle masuk form existing → warisi proteksi. |
| V5 Input Validation | yes | Kolom bool — model binder MVC. `asp-for` checkbox + hidden companion → input ter-bound aman (true/false), tidak ada free-text. Tidak ada injeksi (tidak ada raw SQL dari input user; migration DDL statis). |
| V6 Cryptography | no | — |

### Known Threat Patterns for ASP.NET MVC + EF
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Mass-assignment / over-posting (bind entity langsung) | Tampering | Form hanya expose 2 field bool baru; entity `AssessmentSession` di-bind langsung (pola existing). Risiko: user POST manual field lain. Mitigasi existing: action sudah membatasi via `ModelState.Remove`/validasi eksplisit + field non-form di-set server-side. Phase 372 tidak memperluas surface (cuma 2 bool yang memang dimaksudkan editable). |
| CSRF pada POST toggle | Spoofing | `[ValidateAntiForgeryToken]` sudah ada di kedua POST. |
| Broken access control (non-Admin/HC ubah toggle) | Elevation | `[Authorize(Roles="Admin, HC")]` sudah ada. |

**Catatan:** Endpoint terpisah `UpdateShuffleSettings` (dengan AntiForgery + Authorize + audit + lock guard) adalah scope Phase 374 (SHUF-10/11), BUKAN 372. Phase 372 hanya menumpang form create/edit yang sudah ter-proteksi.

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua respons + copy UI (mandat). Copy toggle/summary = Bahasa Indonesia (sudah locked di UI-SPEC).
- **Develop Workflow:** Lokal → Dev (10.55.3.3) → Prod. Verifikasi lokal WAJIB sebelum push: `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal (+ Playwright bila ada). JANGAN edit kode/DB langsung di Dev/Prod. JANGAN push tanpa verifikasi lokal.
- **Migration handoff:** Promosi migration ke Dev/Prod = tanggung jawab Team IT. Developer notifikasi IT dengan **commit hash + flag migration** (`AddShuffleTogglesToAssessmentSession`, perlu `dotnet ef database update` di DB Dev). Bundle dgn carry-over IT existing.
- **Seed Workflow:** Jika butuh seed data untuk test reproduce di DB lokal — klasifikasi dulu (temporary local-only vs permanent), snapshot DB sebelum insert, catat di `docs/SEED_JOURNAL.md`, restore setelah selesai. (Test real-SQL fixture pakai disposable `HcPortalDB_Test_<guid>` → TIDAK menyentuh DB shared, TIDAK butuh snapshot/restore.)

## Sources

### Primary (HIGH confidence — live code, verified this session)
- `Models/AssessmentSession.cs` (entity; AllowAnswerReview :33, GenerateCertificate :36, struktur kolom)
- `Data/ApplicationDbContext.cs:188-216` (Fluent AssessmentSession; default values :214-216)
- `Migrations/20260214011828_AddAssessmentResultFields.cs:14-19` (bool defaultValue:true analog)
- `Migrations/20260311012214_AddGenerateCertificateToAssessmentSession.cs` (single-bool migration template)
- `Migrations/ApplicationDbContextModelSnapshot.cs:375-377,420-421` (snapshot drift evidence)
- `Controllers/AssessmentAdminController.cs` (7 `new AssessmentSession`: :684,:1218,:1252,:1427,:1895,:1914,:2111; 2 propagation foreach: :1797-1806,:2016-2031; POST sigs :846,:1766)
- `Views/Admin/CreateAssessment.cshtml` (Grup B :472-522, Token :500-519, summary :652-697, populateSummary :1053-1171)
- `HcPortal.Tests/OrgLabelMigrationIntegrationTests.cs` (migration-default test pattern)
- `HcPortal.Tests/ProtonCompletionServiceTests.cs:25-89` (reusable real-SQL fixture)
- `HcPortal.Tests/HcPortal.Tests.csproj` (xUnit 2.9.3, net8.0, EF providers)
- `docs/DEV_WORKFLOW.md:57-119` (migration SOP + commands)
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` (§2/§4/§5/§13)
- `372-CONTEXT.md`, `372-UI-SPEC.md`, `.planning/REQUIREMENTS.md:59-61`, `.planning/config.json`, `CLAUDE.md`
- Tool: `dotnet --version` (8.0.418), `dotnet ef --version` (10.0.3)

### Secondary (MEDIUM confidence)
- (none — semua klaim primary/verified)

### Tertiary (LOW confidence)
- (none)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi terverifikasi langsung via `.csproj` + CLI dalam sesi.
- Architecture / write-sites: HIGH — semua 7 `new AssessmentSession` + 2 foreach di-grep ekshaustif dan dibaca line-by-line.
- Migration pattern: HIGH — triplet analog (`AllowAnswerReview` entity+Fluent+migration) terverifikasi, plus contoh drift (`GenerateCertificate`).
- Pitfalls: HIGH — EF bool-trap + write-site completeness + snapshot drift semua tergrounding pada kode konkret.
- Test pattern: HIGH — fixture real-SQL existing dibaca penuh.

**Research date:** 2026-06-13
**Valid until:** 2026-07-13 (stabil — codebase internal, tidak bergantung ekosistem fast-moving; risiko utama = file-overlap v25.0 yang mengubah line number di `AssessmentAdminController.cs` sebelum eksekusi → planner WAJIB re-grep line di execute-time).
