# Phase 297: Admin Pre-Post Test - Research

**Researched:** 2026-04-07
**Domain:** ASP.NET Core MVC — Assessment session management, dual-session creation, monitoring grouping
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Create Assessment Flow**
- D-01: Dropdown AssessmentType di form CreateAssessment: "Standard" (default) dan "Pre-Post Test". Saat pilih Pre-Post, form expand menampilkan dual-section jadwal
- D-02: Dual-section expand: 2 section "Pre-Test" dan "Post-Test", masing-masing punya input Schedule, DurationMinutes, dan ExamWindowCloseDate sendiri
- D-03: Field sharing: Title, Category, PassPercentage, IsTokenRequired, AccessToken, AllowAnswerReview, Status = shared (1x input). Schedule, DurationMinutes, ExamWindowCloseDate = per Pre/Post. GenerateCertificate dan ValidUntil = hanya relevan untuk Post
- D-04: Peserta sama untuk Pre dan Post — HC pilih peserta sekali, otomatis di-assign ke Pre DAN Post
- D-05: 2 session per peserta — setiap peserta dapat 1 Pre session + 1 Post session. 10 peserta = 20 session total
- D-06: Validasi jadwal: Schedule Pre harus sebelum Schedule Post. Frontend disable tanggal Post < Pre, backend enforce

**Paket Soal**
- D-07: Paket soal dikelola via ManagePackages (flow existing) — CreateAssessment hanya buat session
- D-08: Di ManagePackages Post ada tombol "Copy dari Pre-Test" untuk clone semua paket Pre ke Post
- D-09: Checkbox "Gunakan paket soal yang sama" di CreateAssessment — saat checked, UI realtime mirror

**Monitoring Display**
- D-10: AssessmentMonitoring menampilkan grup Pre-Post sebagai 1 baris parent expandable dengan badge "Pre-Post"
- D-11: Parent row stat gabungan: total peserta, completed (Post saja), passed (Post)
- D-12: Sub-row Pre/Post masing-masing punya link ke AssessmentMonitoringDetail
- D-13: Aksi bulk (Akhiri Semua Ujian, Reshuffle All) berlaku per-phase, bukan per-grup
- D-14: ManageAssessment menampilkan Pre-Post sebagai 1 card dengan badge khusus
- D-15: EditAssessment untuk Pre-Post menggunakan Tab Pre / Post di dalam halaman edit

**Cascade & Delete**
- D-16: Reset Pre-Test TIDAK otomatis cascade ke Post-Test
- D-17: Block reset Pre jika Post sudah Completed — HC harus reset Post dulu
- D-18: Delete grup = hapus kedua session (Pre + Post) + semua paket soal, jawaban, assignment, dan responses
- D-19: Tidak bisa hapus Pre saja atau Post saja — hapus harus per-grup

**Sertifikat & TrainingRecord**
- D-20: Pre session: GenerateCertificate = false, NomorSertifikat = null
- D-21: Post session: GenerateCertificate = pilihan HC
- D-22: TrainingRecord hanya dari Post-Test

**Renewal**
- D-23: Renewal dari sertifikat Pre-Post: HC bebas pilih tipe (Standard atau Pre-Post Test)
- D-24: RenewsSessionId pada renewal Post session baru = Id Post session lama

**Data Model & Linking**
- D-25: Pakai kedua kolom: LinkedGroupId (batch group ID) + LinkedSessionId (pair lookup per peserta)
- D-26: AssessmentType values: 'PreTest' untuk Pre session, 'PostTest' untuk Post session, null untuk Standard
- D-27: AssessmentPhase tidak dipakai untuk Phase 297 — tetap null
- D-28: 2 session per peserta di tabel AssessmentSessions

**Status Lifecycle**
- D-29: Status grup di monitoring derived (dihitung dinamis), bukan disimpan sebagai field
- D-30: Pre-Test status ikuti lifecycle standard: Upcoming/Open/InProgress/Completed/Cancelled

**Edit & Peserta Management**
- D-31: Tambah peserta via EditAssessment = otomatis buat Pre+Post session baru untuk peserta tsb
- D-32: Hapus peserta = hapus kedua session (Pre+Post). Validasi: tidak bisa hapus jika Pre atau Post sudah InProgress/Completed
- D-33: Monitoring grouping: Standard assessment tetap GROUP BY Title+Category+ScheduleDate. Pre-Post pakai GROUP BY LinkedGroupId

### Claude's Discretion
- LinkedGroupId value strategy (ID Pre session pertama vs counter terpisah)
- Exact UI layout dual-section expand di CreateAssessment
- Tab styling di EditAssessment Pre-Post
- Badge visual design untuk Pre-Post di monitoring dan manage
- Copy paket soal implementation detail (deep clone vs reference)

### Deferred Ideas (OUT OF SCOPE)
- Detail gabungan Pre vs Post side-by-side per peserta (Phase 299)
- AssessmentPhase multi-tahap (Phase1/Phase2/Phase3) — kolom sudah ada, belum ada use case
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PPT-01 | HC dapat memilih tipe assessment "Pre-Post Test" saat membuat assessment baru | CreateAssessment GET/POST harus terima AssessmentType field baru; dropdown sudah ada pattern-nya dari Category field |
| PPT-02 | HC dapat mengatur jadwal dan durasi berbeda untuk Pre dan Post | Form perlu 2 set field (SchedulePre, DurationPre, SchedulePost, DurationPost) — dikirim sebagai form parameters tambahan, bukan dari model AssessmentSession standar |
| PPT-03 | HC dapat mencentang "Gunakan paket soal yang sama" untuk copy paket Pre ke Post | Checkbox UI + flag `SamePackage` di POST; flag ini tidak disimpan di DB — hanya sinyal runtime |
| PPT-04 | HC dapat memilih paket soal berbeda untuk Pre dan Post secara independen | ManagePackages sudah independent per assessmentId; tidak ada perubahan backend untuk ini |
| PPT-05 | AssessmentMonitoring menampilkan grup Pre-Post Test sebagai satu entri expandable | AssessmentMonitoring harus deteksi LinkedGroupId dan render parent+sub-row; MonitoringGroupViewModel perlu extend |
| PPT-06 | Reset Pre-Test cascade ke Post-Test (PERHATIAN: D-16 OVERRIDE — reset Pre TIDAK cascade, malah BLOCK jika Post Completed) | ResetAssessment action perlu cek LinkedSessionId + AssessmentType; blokir reset Pre jika LinkedSessionId Post sudah Completed |
| PPT-07 | Hapus grup Pre-Post menghapus kedua sesi tanpa orphan record | DeletePrePostGroup action baru via LinkedGroupId; ikuti pola DeleteAssessmentGroup yang ada |
| PPT-08 | Sertifikat hanya digenerate dari hasil Post-Test | Set GenerateCertificate=false untuk Pre session saat CreateAssessment — GradingService sudah handle correctly |
| PPT-09 | Training Record hanya dari Post-Test | Otomatis dari PPT-08 (Pre tidak generate cert = tidak buat TrainingRecord per GradingService logic) |
| PPT-10 | Pre-Post Test muncul di monitoring dengan status per-phase (Pre/Post) | Sub-row di monitoring menampilkan stat terpisah per AssessmentType |
| PPT-11 | Renewal assessment bebas pilih tipe (Standard atau PrePostTest) | Renewal flow existing tidak perlu diubah — D-23 mengonfirmasi bebas pilih tipe |
</phase_requirements>

---

## Summary

Phase 297 mengimplementasikan fitur Pre-Post Test di sisi admin. Secara arsitektur, ini adalah ekstensi dari sistem assessment yang sudah ada — bukan fitur baru yang berdiri sendiri. Semua pola database, controller, dan view yang dibutuhkan sudah tersedia; yang perlu dilakukan adalah menambah logika "tipe Pre-Post" di lapisan atas.

Kolom database yang diperlukan (`AssessmentType`, `LinkedGroupId`, `LinkedSessionId`) sudah di-migrate di Phase 296. Tidak ada migrasi database baru yang dibutuhkan di Phase 297. GradingService sudah menangani sertifikat dan TrainingRecord dengan benar — Pre session cukup diset `GenerateCertificate=false`.

Kompleksitas terbesar ada di 3 area: (1) CreateAssessment POST yang harus buat 2 session per user dan mengisi LinkedGroupId/LinkedSessionId dengan benar; (2) AssessmentMonitoring yang harus mendeteksi Pre-Post group dan render UI expandable; (3) DeletePrePostGroup yang harus hapus kedua session secara atomik. Area lainnya (EditAssessment, ManagePackages) adalah modifikasi yang relatif sederhana.

**Primary recommendation:** Implementasi dalam urutan: CreateAssessment → Monitoring → Delete → Edit → ManagePackages Copy. Mulai dari backend model/controller sebelum UI agar dapat ditest secara terisolasi.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | 8.0 (existing) | Controller + View framework | Existing stack proyek |
| Entity Framework Core | 8.0 (existing) | ORM untuk DB operations | Existing stack proyek |
| Bootstrap | 5.3.0 via CDN | UI components (tabs, collapse, badge, modal) | Existing stack proyek per UI-SPEC |
| Bootstrap Icons | 1.10.0 via CDN | Icon set (bi-chevron-down, bi-arrow-left-right, dll) | Existing stack per UI-SPEC |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Vanilla JavaScript | ES6+ | Collapse toggle, min-date validation | Semua interaktivitas frontend di phase ini |

**Installation:** Tidak ada package baru. Semua dependency sudah tersedia. [VERIFIED: codebase grep]

---

## Architecture Patterns

### Pola CreateAssessment untuk Pre-Post Test

**Situasi:** CreateAssessment POST saat ini membuat 1 session per user. Untuk Pre-Post, harus membuat 2 session per user (Pre + Post) dan menghubungkannya.

**Strategi LinkedGroupId (Claude's Discretion — rekomendasi):**
Gunakan ID dari Pre session pertama sebagai LinkedGroupId untuk semua session dalam batch tersebut. Ini menghindari counter terpisah dan tetap dapat di-query dengan mudah. [ASSUMED]

**Urutan operasi CreateAssessment POST Pre-Post:**
```
1. Validasi form (shared fields + Pre fields + Post fields)
2. Validasi: SchedulePost > SchedulePre (D-06)
3. Build daftar Pre sessions (1 per user) — AssessmentType='PreTest', GenerateCertificate=false
4. Build daftar Post sessions (1 per user) — AssessmentType='PostTest', jadwal/durasi Post
5. AddRange Pre sessions → SaveChanges (untuk dapat Id)
6. Set LinkedGroupId = Pre sessions[0].Id untuk semua sessions
7. Set LinkedSessionId: Pre[i].LinkedSessionId = Post[i].Id, Post[i].LinkedSessionId = Pre[i].Id
8. SaveChanges final
```

**Alternatif — single SaveChanges:** Buat Pre dan Post dalam 1 batch, tapi LinkedGroupId dan LinkedSessionId harus di-update setelah save pertama karena Id belum diketahui sebelum insert. Dua-kali SaveChanges dalam satu transaction adalah pattern yang lebih aman. [ASSUMED]

### Pola Monitoring Pre-Post

**Situasi:** AssessmentMonitoring saat ini GROUP BY (Title, Category, Schedule.Date). Pre-Post group memiliki Schedule berbeda (Pre ≠ Post), sehingga grouping lama akan memisahkan Pre dan Post menjadi 2 baris terpisah — salah.

**Solusi (D-33):** Pre-Post group di-detect via `LinkedGroupId != null` dan di-group by `LinkedGroupId`. Standard assessment tetap GROUP BY (Title, Category, Schedule.Date).

**Query pattern yang diperlukan:**
```csharp
// Ambil semua sessions termasuk AssessmentType dan LinkedGroupId
var allSessions = await query
    .Select(a => new {
        // ... existing fields ...
        a.AssessmentType,
        a.LinkedGroupId,
        // ...
    }).ToListAsync();

// Pisahkan Pre-Post sessions dari Standard sessions
var prePostSessions = allSessions.Where(a => a.LinkedGroupId != null).ToList();
var standardSessions = allSessions.Where(a => a.LinkedGroupId == null).ToList();

// Group Pre-Post by LinkedGroupId
var prePostGroups = prePostSessions.GroupBy(a => a.LinkedGroupId);

// Group Standard by (Title, Category, Schedule.Date)
var standardGroups = standardSessions.GroupBy(a => (a.Title, a.Category, a.Schedule.Date));
```

### Pola MonitoringGroupViewModel Extension

MonitoringGroupViewModel perlu di-extend dengan field Pre-Post:

```csharp
// Extension yang diperlukan di MonitoringGroupViewModel
public bool IsPrePostGroup { get; set; } = false;
public int? LinkedGroupId { get; set; }
public MonitoringGroupViewModel? PreSubRow { get; set; }   // stat Pre sessions saja
public MonitoringGroupViewModel? PostSubRow { get; set; }  // stat Post sessions saja
```

[VERIFIED: codebase — MonitoringGroupViewModel.cs line 1-35, field yang dibutuhkan belum ada]

### Pola DeletePrePostGroup

DeletePrePostGroup harus menggunakan LinkedGroupId (bukan Title+Category+Schedule.Date) untuk find semua sessions yang perlu dihapus:

```csharp
// Find semua sessions dengan LinkedGroupId yang sama
var groupSessions = await _context.AssessmentSessions
    .Where(a => a.LinkedGroupId == linkedGroupId)
    .ToListAsync();

var groupIds = groupSessions.Select(s => s.Id).ToList();

// Hapus dalam urutan yang sama dengan DeleteAssessmentGroup existing:
// PackageUserResponses → AttemptHistory → Packages+Questions+Options → Sessions
```

[VERIFIED: codebase — pola cascade delete ada di DeleteAssessmentGroup baris 1588-1660]

### Pola EditAssessment Pre-Post

EditAssessment GET harus detect apakah session yang di-edit adalah bagian dari Pre-Post group:

```csharp
bool isPrePost = assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest";

if (isPrePost && assessment.LinkedGroupId.HasValue)
{
    // Cari pasangan session (Pre atau Post)
    var pairedSession = await _context.AssessmentSessions
        .FirstOrDefaultAsync(a => a.Id == assessment.LinkedSessionId);
    ViewBag.PairedSession = pairedSession;
    ViewBag.IsPrePostGroup = true;
}
```

### Anti-Patterns to Avoid

- **Grouping Pre-Post sessions dengan pola Title+Category+Date:** Akan memisahkan Pre dan Post menjadi 2 grup karena Schedule berbeda. Gunakan LinkedGroupId.
- **Menyimpan status grup Pre-Post di DB:** Status grup adalah derived (dihitung dari sessions), sesuai D-29.
- **Membuat migration baru untuk kolom Pre-Post:** Semua kolom sudah ada sejak Phase 296.
- **Mengubah GradingService untuk Pre-Post:** GradingService sudah correct — Pre session dengan `GenerateCertificate=false` tidak akan generate cert/training record.
- **Cascade reset Pre ke Post:** D-16 menyatakan TIDAK ada cascade reset. Malah blokir reset Pre jika Post sudah Completed (D-17).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Deep clone paket soal Pre ke Post | Custom serialization/memcopy | EF Core: load + detach + re-insert dengan Id=0 | EF sudah handle graph cloning; simpler, less error-prone |
| Validasi jadwal Pre < Post | Custom date comparison library | Native C# DateTime comparison + HTML `min` attribute | Already available, zero dependency |
| Atomik delete multi-session | Manual retry loop | Single `_context.SaveChangesAsync()` dalam satu transaction | EF transaction sudah handle atomicity |
| Generate LinkedGroupId unik | Counter table atau UUID | Id dari Pre session pertama setelah SaveChanges | Sudah unique (PK), tidak perlu overhead tambahan |

---

## Common Pitfalls

### Pitfall 1: LinkedGroupId Tersedia Setelah SaveChanges Pertama

**What goes wrong:** Kode mencoba set `LinkedGroupId = preSessions[0].Id` sebelum SaveChanges, sehingga Id masih 0 (belum di-assign DB).

**Why it happens:** EF Core assign Id saat SaveChanges dieksekusi dan DB mengembalikan generated key.

**How to avoid:** Dua-kali SaveChanges dalam satu transaction: pertama insert Pre sessions, dapatkan Id, update LinkedGroupId/LinkedSessionId, lalu SaveChanges kedua untuk Post sessions.

**Warning signs:** LinkedGroupId bernilai 0 di DB.

### Pitfall 2: AssessmentMonitoring Memisahkan Pre dan Post

**What goes wrong:** Pre session (SchedulePre = 1 Mei) dan Post session (SchedulePost = 8 Mei) muncul sebagai 2 baris terpisah di monitoring karena GroupBy menggunakan Schedule.Date.

**Why it happens:** Current grouping logic tidak aware tentang Pre-Post linkage.

**How to avoid:** Detect sessions dengan `LinkedGroupId != null` SEBELUM grouping, dan proses mereka dengan GroupBy LinkedGroupId.

**Warning signs:** Pre-Post group muncul sebagai 2 baris di monitoring bukan 1.

### Pitfall 3: EditAssessment Menyalin Schedule Pre ke Post

**What goes wrong:** EditAssessment POST saat ini mengupdate SEMUA siblings dengan `sibling.Schedule = model.Schedule`. Untuk Pre-Post group, ini akan mengubah jadwal Pre ke nilai yang sama dengan yang di-edit, merusak jadwal Post.

**Why it happens:** Edit siblings loop (baris 1317-1332) tidak membedakan Pre dan Post sessions.

**How to avoid:** EditAssessment Pre-Post harus edit Pre session terpisah dari Post session. Tab Pre/Post di view mengirim form ke endpoint berbeda atau dengan parameter yang mengidentifikasi phase mana yang di-edit.

**Warning signs:** Setelah edit jadwal Post, jadwal Pre ikut berubah.

### Pitfall 4: Delete Individual Masih Tersedia di UI

**What goes wrong:** Tombol delete individual muncul di ManageAssessment untuk session yang merupakan bagian Pre-Post group, dan ketika di-klik menghapus hanya Pre atau Post saja — meninggalkan orphan.

**Why it happens:** DeleteAssessment action tidak cek `AssessmentType` atau `LinkedGroupId`.

**How to avoid:** (1) Sembunyikan tombol delete individual di view untuk sessions dengan `AssessmentType != null`. (2) Backend `DeleteAssessment` menolak request jika session adalah bagian Pre-Post group (return error "Gunakan hapus grup").

**Warning signs:** Orphan session (Pre tanpa Post atau sebaliknya) dengan LinkedGroupId yang menunjuk ke session yang sudah dihapus.

### Pitfall 5: Copy Paket Pre ke Post Tidak Deep Clone

**What goes wrong:** Copy menggunakan reference yang sama (AssessmentPackageId sama dipakai oleh Post session), sehingga ketika paket Pre diedit/dihapus, paket Post ikut terpengaruh.

**Why it happens:** Developer lupa bahwa AssessmentPackage.AssessmentSessionId adalah FK — harus buat record baru dengan SessionId Post.

**How to avoid:** Deep clone: load Pre packages dengan Include(Questions).ThenInclude(Options), lalu buat object baru dengan Id=0 dan AssessmentSessionId = postSessionId.

**Warning signs:** Edit soal Pre menyebabkan soal Post berubah.

---

## Code Examples

### CreateAssessment POST — Pola Dua-Fase untuk Pre-Post

```csharp
// Source: Pola existing di CreateAssessment POST baris 975-1055 + ekstensi Pre-Post
// FASE 1: Buat Pre sessions
var preSessions = new List<AssessmentSession>();
var postSessions = new List<AssessmentSession>();

foreach (var userId in UserIds)
{
    var preSession = new AssessmentSession
    {
        // ... shared fields dari model ...
        Schedule = preSchedule,
        DurationMinutes = preDurationMinutes,
        ExamWindowCloseDate = preExamWindowCloseDate,
        AssessmentType = "PreTest",
        GenerateCertificate = false,  // D-20: Pre tidak pernah generate cert
        ValidUntil = null,
        UserId = userId,
        // LinkedGroupId dan LinkedSessionId di-set setelah save
    };
    preSessions.Add(preSession);
}

using var transaction = await _context.Database.BeginTransactionAsync();
_context.AssessmentSessions.AddRange(preSessions);
await _context.SaveChangesAsync();  // Pre sessions mendapat Id di sini

// FASE 2: Set LinkedGroupId = Id Pre session pertama
int linkedGroupId = preSessions[0].Id;

foreach (var userId in UserIds)
{
    var postSession = new AssessmentSession
    {
        // ... shared fields dari model ...
        Schedule = postSchedule,
        DurationMinutes = postDurationMinutes,
        ExamWindowCloseDate = postExamWindowCloseDate,
        AssessmentType = "PostTest",
        GenerateCertificate = model.GenerateCertificate,  // D-21: pilihan HC
        ValidUntil = model.ValidUntil,
        UserId = userId,
        LinkedGroupId = linkedGroupId,
    };
    postSessions.Add(postSession);
}

_context.AssessmentSessions.AddRange(postSessions);
await _context.SaveChangesAsync();  // Post sessions mendapat Id di sini

// FASE 3: Update LinkedSessionId (cross-link Pre <-> Post)
for (int i = 0; i < preSessions.Count; i++)
{
    preSessions[i].LinkedGroupId = linkedGroupId;
    preSessions[i].LinkedSessionId = postSessions[i].Id;
    postSessions[i].LinkedSessionId = preSessions[i].Id;
}
await _context.SaveChangesAsync();
await transaction.CommitAsync();
```

### Monitoring — Deteksi dan Grouping Pre-Post

```csharp
// Source: Pola dari AssessmentMonitoring baris 1807-1838 + ekstensi
var prePostSessions = allSessions.Where(a => a.LinkedGroupId != null).ToList();
var standardSessions = allSessions.Where(a => a.LinkedGroupId == null).ToList();

var prePostGroups = prePostSessions
    .GroupBy(a => a.LinkedGroupId)
    .Select(g =>
    {
        var preSubs = g.Where(a => a.AssessmentType == "PreTest").ToList();
        var postSubs = g.Where(a => a.AssessmentType == "PostTest").ToList();
        var rep = preSubs.OrderBy(a => a.CreatedAt).FirstOrDefault() ?? g.First();

        return new MonitoringGroupViewModel
        {
            RepresentativeId = rep.Id,
            Title = rep.Title,
            Category = rep.Category,
            Schedule = rep.Schedule,
            TotalCount = postSubs.Count,  // D-11: total peserta = count Post sessions
            CompletedCount = postSubs.Count(a => a.IsCompleted),  // D-11: completed dari Post
            PassedCount = postSubs.Count(a => a.IsPassed),
            IsPrePostGroup = true,
            LinkedGroupId = g.Key,
            // ... stat sub-rows ...
        };
    }).ToList();
```

### Deep Clone Paket Soal Pre ke Post

```csharp
// Source: Pola struktur AssessmentPackage/PackageQuestion/PackageOption dari codebase
public async Task CopyPackagesFromPreToPostAsync(int preSessionId, int postSessionId)
{
    // Hapus paket Post yang ada dulu
    var existingPostPkgs = await _context.AssessmentPackages
        .Include(p => p.Questions).ThenInclude(q => q.Options)
        .Where(p => p.AssessmentSessionId == postSessionId)
        .ToListAsync();
    // ... hapus existingPostPkgs ...

    // Load Pre packages
    var prePkgs = await _context.AssessmentPackages
        .Include(p => p.Questions).ThenInclude(q => q.Options)
        .Where(p => p.AssessmentSessionId == preSessionId)
        .OrderBy(p => p.PackageNumber)
        .ToListAsync();

    foreach (var prePkg in prePkgs)
    {
        var newPkg = new AssessmentPackage
        {
            AssessmentSessionId = postSessionId,
            PackageName = prePkg.PackageName,
            PackageNumber = prePkg.PackageNumber,
            // Id = 0 → auto-generated
        };
        foreach (var q in prePkg.Questions)
        {
            var newQ = new PackageQuestion
            {
                QuestionText = q.QuestionText,
                Order = q.Order,
                ScoreValue = q.ScoreValue,
                QuestionType = q.QuestionType,
                ElemenTeknis = q.ElemenTeknis,
                Options = q.Options.Select(o => new PackageOption
                {
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
            newPkg.Questions.Add(newQ);
        }
        _context.AssessmentPackages.Add(newPkg);
    }
    await _context.SaveChangesAsync();
}
```

---

## Runtime State Inventory

Step 2.5: SKIPPED — Phase 297 adalah fitur baru, bukan rename/refactor/migration. Tidak ada runtime state lama yang perlu dimigrasikan.

---

## Environment Availability

Step 2.6: SKIPPED — Phase 297 murni ASP.NET Core MVC code dan view changes. Tidak ada dependency eksternal baru. Semua tools (dotnet, EF migrations) sudah tersedia dari Phase 296.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (tidak ada automated test framework di proyek ini) |
| Config file | none |
| Quick run command | `dotnet run` lalu navigasi manual |
| Full suite command | n/a |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PPT-01 | Dropdown AssessmentType muncul dan "Pre-Post Test" bisa dipilih | manual | — | ❌ manual |
| PPT-02 | Dual-section jadwal expand setelah pilih Pre-Post; Schedule Post > Pre enforced | manual | — | ❌ manual |
| PPT-03 | Checkbox "Gunakan paket soal yang sama" muncul; info badge muncul saat checked | manual | — | ❌ manual |
| PPT-04 | ManagePackages Pre dan Post independent | manual | — | ❌ manual |
| PPT-05 | Monitoring: Pre-Post grup muncul sebagai 1 baris dengan expand | manual | — | ❌ manual |
| PPT-06 | Reset Pre diblokir jika Post Completed; reset Pre berhasil jika Post belum Completed | manual | — | ❌ manual |
| PPT-07 | Delete grup menghapus Pre+Post sessions + semua data terkait tanpa orphan | manual | — | ❌ manual |
| PPT-08 | Pre session tidak generate sertifikat meskipun IsPassed=true | manual | — | ❌ manual |
| PPT-09 | TrainingRecord tidak dibuat dari Pre session | manual | — | ❌ manual |
| PPT-10 | Sub-row Pre/Post di monitoring menampilkan stat masing-masing | manual | — | ❌ manual |
| PPT-11 | Renewal dari Post sertifikat: form pre-fill, bisa pilih Standard atau Pre-Post | manual | — | ❌ manual |

Wave 0 Gaps: None — tidak ada automated test infrastructure yang perlu disiapkan untuk phase ini.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Existing [Authorize(Roles = "Admin, HC")] |
| V3 Session Management | no | — |
| V4 Access Control | yes | [Authorize(Roles = "Admin, HC")] sudah ada di semua action existing; harus diterapkan ke action baru (DeletePrePostGroup, CopyPackagesFromPre) |
| V5 Input Validation | yes | Validasi SchedulePost > SchedulePre di backend (D-06); validasi UserIds; ValidateAntiForgeryToken |
| V6 Cryptography | no | — |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| IDOR — hapus grup Pre-Post milik org lain | Tampering | Verify LinkedGroupId sessions belong to sessions accessible to current HC role |
| CSRF — DeletePrePostGroup POST | Tampering | [ValidateAntiForgeryToken] pada semua POST actions baru |
| Mass assignment — AssessmentType field | Tampering | Bind AssessmentType secara explicit di POST, jangan bind seluruh model mentah |

---

## Open Questions

1. **LinkedGroupId: ID Pre session pertama vs auto-increment counter**
   - What we know: D-25 menyatakan LinkedGroupId = "batch group ID". Tidak ada keputusan tentang nilai konkret.
   - What's unclear: Apakah menggunakan Id Pre session pertama cukup robust? Jika Pre session pertama dihapus (misalnya karena error sebelum commit), LinkedGroupId akan merujuk ke session yang tidak ada.
   - Recommendation: Gunakan Id Pre session pertama sebagai LinkedGroupId. Jika Pre session pertama terhapus, seluruh grup terhapus bersama (D-18 memastikan delete selalu per-grup), sehingga dangling reference tidak mungkin terjadi.

2. **ResetAssessment action: apakah sudah ada di codebase?**
   - What we know: D-16/D-17 menyebut "reset Pre-Test" tapi action `ResetAssessment` tidak terlihat di baris yang dibaca.
   - What's unclear: Apakah reset di-handle oleh action terpisah atau bagian dari EditAssessment/aksi lain?
   - Recommendation: Planner perlu cari action reset di controller sebelum planning task terkait D-17.

3. **ManageAssessment view: bagaimana detect Pre-Post untuk badge?**
   - What we know: D-14 menyatakan ManageAssessment tampilkan 1 card dengan badge Pre-Post.
   - What's unclear: ManageAssessment saat ini GROUP BY (Title+Category+Schedule.Date) — sama seperti monitoring. Apakah query ManageAssessment perlu perubahan serupa dengan monitoring?
   - Recommendation: Ya, ManageAssessment juga perlu deteksi LinkedGroupId dan render 1 card per grup, dengan badge Pre-Post jika IsPrePostGroup.

---

## Sources

### Primary (HIGH confidence)
- `Controllers/AssessmentAdminController.cs` baris 517-1660, 1763-1858, 3002-3082 — CreateAssessment GET/POST, EditAssessment, DeleteAssessment, DeleteAssessmentGroup, AssessmentMonitoring, ManagePackages
- `Models/AssessmentSession.cs` — semua kolom termasuk v14.0 kolom baru
- `Models/AssessmentPackage.cs` — struktur AssessmentPackage, PackageQuestion, PackageOption
- `Models/AssessmentMonitoringViewModel.cs` — MonitoringGroupViewModel dan MonitoringSessionViewModel
- `Services/GradingService.cs` baris 1-60 — GradeAndCompleteAsync contract
- `.planning/phases/297-admin-pre-post-test/297-CONTEXT.md` — semua keputusan D-01 sampai D-33
- `.planning/phases/297-admin-pre-post-test/297-UI-SPEC.md` — design contract Bootstrap 5.3.0

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md` — PPT-01 sampai PPT-11
- `.planning/STATE.md` — accumulated decisions dari milestone sebelumnya

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | LinkedGroupId sebaiknya menggunakan Id Pre session pertama (bukan counter terpisah) | Architecture Patterns | Jika strategi berbeda dipilih, kode CreateAssessment POST perlu adjustment |
| A2 | Dua-kali SaveChanges dalam 1 transaction adalah cara terbaik untuk populate LinkedGroupId | Code Examples | Ada kemungkinan pola lain (output parameter, dll) — tapi dua-kali save sudah proven di codebase |
| A3 | `ResetAssessment` action belum ada di baris controller yang dibaca — mungkin ada di baris lain yang tidak dibaca | Open Questions | Jika sudah ada, planner perlu adapt existing action, bukan buat baru |

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — verified dari codebase
- Architecture: HIGH — verified dari controller patterns existing
- Pitfalls: HIGH — derived dari kode actual yang dibaca
- Monitoring grouping change: HIGH — verified dari query di baris 1807-1838

**Research date:** 2026-04-07
**Valid until:** 2026-05-07 (stable domain)
