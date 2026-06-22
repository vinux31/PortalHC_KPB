# Phase 405: Backend Core — Data + RetakeRules + RetakeService + Refactor Reset + Config Endpoint - Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core MVC + EF Core 8.0.0 (SQL Server) — assessment retake engine (backend, no UI)
**Confidence:** HIGH (semua anchor file:line + signature diverifikasi langsung dari repo; D-01 dibuktikan via query DB live)

## Summary

Phase 405 membangun fondasi backend ujian ulang dengan memproduktisasi mesin `ResetAssessment` HC yang sudah ada (`AssessmentAdminController.cs:4192`) menjadi `RetakeService` bersama, sambil menambal lubang data-loss per-soal (tabel snapshot baru `AssessmentAttemptResponseArchive`). Semua signature helper/aggregator yang dirujuk plan superpowers sudah diverifikasi nyata di repo: `AssessmentScoreAggregator.IsQuestionCorrect(PackageQuestion, IEnumerable<PackageUserResponse>) → bool?` dan `BuildAnswerCell(...) → string` (`Helpers/AssessmentScoreAggregator.cs:73,110`). Pola mirror `ShuffleToggleRules` (pure, no-DI) dan `UpdateShuffleSettings` (sibling propagation Title/Category/Schedule.Date) sudah terbukti dan langsung dapat ditiru.

Temuan paling kritis ada di **D-01 (counting era-retake)**. Spec asli meng-`count(UserId,Title,Category)` polos dari `AssessmentAttemptHistory`, tapi query DB live membuktikan ada **5 baris legacy** (HC-reset Jan–Apr 2026, semua `AttemptNumber=1`) yang akan langsung memakan cap `MaxAttempts` jika dihitung polos — pekerja yang pernah di-HC-reset 2× akan terkunci begitu `AllowRetake` di-ON. Mekanisme yang direkomendasikan: **hitung hanya `AssessmentAttemptHistory` yang punya ≥1 baris anak `AssessmentAttemptResponseArchive`** — baris legacy mustahil punya snapshot (tabelnya baru lahir di migration ini), jadi natural-excluded tanpa kolom diskriminator atau date-cutoff. Robust, testable, zero-data-migration.

Dua deviasi WAJIB dari `ResetAssessment` existing yang harus dibawa `RetakeService`: (1) urutan **claim-transisi-atomik DULU baru archive** (existing archive→delete→claim; harus dibalik — must-fix #4 anti double-archive), dan (2) counting grouping `(UserId,Title,Category)` (existing pakai `(UserId,Title)` saja di `:4242` — konflasi Pre/Post). Migration tunggal `AddRetakeColumnsAndArchive` (D-03) digenerate via global `dotnet ef` v10.0.3 terhadap project EF 8.0.0 (snapshot ProductVersion tetap `8.0.0`), pola identik Phase 399.

**Primary recommendation:** Ikuti struktur 9-task plan superpowers, dengan TIGA koreksi wajib: (a) RetakeService balik urutan jadi claim-first; (b) counting `(UserId,Title,Category)` DAN diskriminator snapshot-presence untuk D-01; (c) `AnswerText` archive jangan pakai `BuildAnswerCell` mentah untuk essay (truncate 300 char) bila ingin full-text — lihat Pitfall 2.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (dari spec — carry-forward, TIDAK di-discuss ulang)
- **10 keputusan D1–D10:** self-service pekerja (HC override) · attempt terakhir = record (in-place reset) · cooldown configurable default 24h (`0`=no jeda) · setting per-assessment · feedback skor+tanda-salah (kunci ditahan) · graded-only (`AssessmentType!="PreTest"`) · cap habis→lock+HC · MaxAttempts default 2 (range 1–5) · riwayat pekerja+HC · full snapshot per-soal.
- **7 must-fix:** (1) clear `TempData[TokenVerified_{id}]` saat retake; (2) exclude `IsManualEntry`; (3) counting `(UserId,Title,Category)` anti-konflasi Pre/Post; (4) claim-transisi-atomik DULU anti double-archive; (5) exclude PendingGrading (IsPassed null); (6) audit `RetakeAssessment` (worker) / `ResetAssessment` (HC); (7) tier `showWrongFlagsOnly` (di Phase 407, tapi `RetakeRules` sediakan flag-nya).
- **Signature/shape:** `RetakeRules.CanRetake` + `ShouldHideRetakeToggle` (pure); `RetakeService.ExecuteAsync(sessionId, initiatedBy, bypassGuards)` return `(bool success, string? error)`; `RetakeArchiveBuilder.Build(attemptHistoryId, questions, responses)` (verdict via `IsQuestionCorrect`, jawaban via `BuildAnswerCell`, beku SEBELUM `RemoveRange`); tabel `AssessmentAttemptResponseArchive` (FK→`AssessmentAttemptHistory` cascade, index `AttemptHistoryId`, `PackageQuestionId` plain int).
- **Target & mirror:** `ResetAssessment` @ `AssessmentAdminController.cs:4192` (refactor → delegasi service, guard HC tetap di controller); `UpdateRetakeSettings` mirror `UpdateShuffleSettings:5556`; `RetakeRules` mirror `Helpers/ShuffleToggleRules.cs` + test mirror `HcPortal.Tests/ShuffleToggleRulesTests.cs`.

### Gray areas yang di-discuss (2026-06-21)
- **D-01 (Hitungan attempt legacy):** `attemptsUsed` hanya hitung percobaan **era-retake** — arsip HC-reset LAMA (pre-v32.4) **TIDAK** pre-consume cap. **Mekanisme = diskresi planner.** (Researcher merekomendasikan snapshot-presence — lihat bagian "D-01 Mechanism Resolution".) ⚠️ Deviasi dari spec yang count polos.
- **D-02 (Eligibility retroaktif):** **Ya, retroaktif.** Saat admin nyalakan `AllowRetake`, sesi yang sudah gagal langsung eligible (tunduk cooldown+cap). `CanRetake` cukup cek flag current + cooldown dari `CompletedAt` — tidak perlu enabled-at timestamp.
- **D-03 (Paket migration):** **Satu migration gabung** `AddRetakeColumnsAndArchive` = 3 kolom + tabel. 1 entri notify-IT. Pola 399.
- **D-04 (Retensi arsip):** **Simpan selamanya** (retain-all, ISO 17024). Hapus hanya via FK ON DELETE CASCADE saat parent dihapus. Tanpa pruning.

### Claude's Discretion
- Mekanisme konkret D-01 (snapshot-presence vs date-cutoff) — pilih paling robust+testable.
- `IRetakeService` DI lifetime (scoped, ikut pola existing), namespace/folder, struktur test.
- Status transient saat claim (`Completed→"Open"`; monitoring lihat "Open" sebentar — diterima).

### Deferred Ideas (OUT OF SCOPE Phase 405)
- UI admin (card config + riwayat HC) = Phase 406. UI worker (tombol/gating/riwayat) = Phase 407. Test menyeluruh + Playwright + security = Phase 408.
- Grading method highest/average, cooldown escalating, default MaxAttempts per-kategori, pre-retake remediation, rotasi AccessToken per-attempt, cap per-tahun — YAGNI.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **RTK-01** | Config fields `AllowRetake`/`MaxAttempts`/`RetakeCooldownHours` di `AssessmentSession` + migration + binding semua jalur create | Sisip setelah `AssessmentSession.cs:42` (ShuffleOptions). EF default tutup semua create-path; **explicit copy hanya di standard add-users `AssessmentAdminController.cs:2166-2185` (dari `savedAssessment`)** — bukan pre/post block (copy dari `model`, tak punya field di 405). Lihat "Implementation Map → RTK-01". |
| **RTK-02** | Tabel `AssessmentAttemptResponseArchive` (FK→AttemptHistory cascade, index) + builder pure `RetakeArchiveBuilder.Build` | Model+DbContext config mirror `AssessmentAttemptHistory` (`ApplicationDbContext.cs:571-581`). Builder pakai aggregator terverifikasi `AssessmentScoreAggregator.cs:73,110`. |
| **RTK-03** | `RetakeRules.CanRetake` + `ShouldHideRetakeToggle` (pure, unit-tested) | Mirror `Helpers/ShuffleToggleRules.cs` (pure no-DI). Semua field input ada di `AssessmentSession` (verified). |
| **RTK-04** | Endpoint `UpdateRetakeSettings` + sibling propagation | Mirror `UpdateShuffleSettings` (`AssessmentAdminController.cs:5552-5612`). Sibling key identik `:5563-5567`. |
| **RTK-06** | Refactor `ResetAssessment` → delegasi service (HC override bypass) | Body inti `:4238-4323` diekstrak; guard `:4199-4236` + `IsResettable:4186` TETAP di controller. `ResetGuardTests` (pure) regresi hijau. |
| **RTK-07** | `RetakeService.ExecuteAsync` — claim atomik DULU, snapshot, archive, delete, audit, SignalR `reason` | Reproduksi `:4238-4323` dengan urutan dibalik (claim-first). DI register di `Program.cs:54`. |
| **RTK-13** | Guards komprehensif service-side (PreTest/IsManualEntry/PendingGrading/Cancelled/Abandoned) | Di-encode dalam `RetakeRules.CanRetake` (worker path) + `bypassGuards` escape-hatch HC. |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Eligibility decision (CanRetake) | Domain helper (pure) | API (re-cek) | `RetakeRules` murni — caller (service + Phase 407 controller) inject fakta; tak sentuh DB. Mirror `ShuffleToggleRules`. |
| Snapshot building (verdict beku) | Domain helper (pure) | — | `RetakeArchiveBuilder` murni — verdict via aggregator terpusat, EF-free, unit-testable. |
| Reset orchestration (claim/archive/delete/audit/SignalR) | API/Service | Database | `RetakeService` butuh DbContext+Hub+Audit — scoped service, dipanggil dua jalur (worker 407 + HC 405). |
| Config persistence + propagation | API (controller) | Database | `UpdateRetakeSettings` = HTTP endpoint (RBAC+AntiForgery+TempData/PRG) — tak bisa di service. |
| Token clear (TempData) | API (controller) | — | TempData HTTP-scoped; service tak punya akses. Caller WAJIB `TempData.Remove` setelah ExecuteAsync. |
| Schema (kolom+tabel) | Database/EF migration | — | Migration tunggal `AddRetakeColumnsAndArchive`. |

## Standard Stack

Tidak ada library baru — semua in-repo. Stack existing:

| Komponen | Versi | Purpose | Why Standard |
|----------|-------|---------|--------------|
| ASP.NET Core MVC | .NET (project) | Controller/endpoint | Stack aplikasi |
| EF Core | 8.0.0 (project pin) | ORM + migration | Snapshot ProductVersion `8.0.0` [VERIFIED: ApplicationDbContextModelSnapshot.cs:20] |
| `dotnet ef` CLI | 10.0.3 (global) | Generate/apply migration | [VERIFIED: `dotnet ef --version` → 10.0.3]. Pola sama Phase 399 — generate via global 10.x, snapshot tetap 8.0.0. |
| xUnit | (project) | Unit + integration test | `HcPortal.Tests/` |
| SignalR | (project) | `sessionReset` broadcast | `IHubContext<AssessmentHub>` [VERIFIED: AssessmentAdminController.cs:27] |

**Installation:** Tidak ada. `dotnet build` + `dotnet test`.

**Migration command (verified working env):**
```bash
# DB lokal: HcPortalDB_Dev @ localhost\SQLEXPRESS, Integrated Security
dotnet ef migrations add AddRetakeColumnsAndArchive
dotnet ef database update    # LOKAL saja; Dev/Prod = tanggung jawab IT (DEV_WORKFLOW)
```
Tidak ada local tool manifest (`.config/dotnet-tools.json` tidak ada) — pakai global `dotnet ef`.

## Architecture Patterns

### System Architecture Diagram

```
WORKER PATH (Phase 407)            HC PATH (Phase 405 — THIS phase refactors)
  CMP/RetakeExam(id)                 AssessmentAdminController.ResetAssessment(id)
    │ AntiForgery + ownership          │ AntiForgery + [Authorize(Admin,HC)]
    │ re-cek CanRetakeAsync            │ guard: IsResettable / Pre-Post block / status  ◄── TETAP di controller
    ▼                                  ▼
    └──────────────►  RetakeService.ExecuteAsync(sessionId, actor, actionType, reason)  ◄──┘
                          │
                          ▼
       ┌──── 1. CLAIM ATOMIK (must-fix #4): ExecuteUpdateAsync WHERE Status NOT IN (Cancelled,Open)
       │         → Status="Open", null-out fields. rowsAffected==0 → ABORT (race lost / double-click)
       │
       ├──── 2. (jika wasCompleted) SNAPSHOT per-soal SEBELUM delete:
       │         questions+responses → RetakeArchiveBuilder.Build(attemptHistoryId, ...)
       │              └─ verdict: AssessmentScoreAggregator.IsQuestionCorrect (bool?)
       │              └─ answer:  AssessmentScoreAggregator.BuildAnswerCell  (string)
       │
       ├──── 3. ARCHIVE: new AssessmentAttemptHistory { AttemptNumber = countWithSnapshot+1 }
       │         + AddRange(snapshot rows)  →  SaveChanges
       │
       ├──── 4. DELETE live: PackageUserResponses + UserPackageAssignment + SessionElemenTeknisScores
       │
       ├──── 5. AUDIT: actionType ("RetakeAssessment" | "ResetAssessment"), reason in description
       │
       └──── 6. SignalR: Clients.User(userId).SendAsync("sessionReset", new { reason })  ◄── reason parameterized

  CALLER (post-success):  TempData.Remove($"TokenVerified_{id}")   ◄── must-fix #1 (HTTP-scoped, bukan di service)
```

### Recommended File Structure
```
Models/
  AssessmentAttemptResponseArchive.cs   # CREATE — entity snapshot per-soal (RTK-02)
  AssessmentSession.cs                   # MODIFY — +3 kolom setelah :42 (RTK-01)
Data/
  ApplicationDbContext.cs                # MODIFY — DbSet after :68 + config near :581 (RTK-02)
Helpers/
  RetakeRules.cs                         # CREATE — pure eligibility (RTK-03)
  RetakeArchiveBuilder.cs                # CREATE — pure snapshot builder (RTK-02)
Services/
  RetakeService.cs (+ IRetakeService)    # CREATE — shared engine (RTK-07)
Controllers/
  AssessmentAdminController.cs           # MODIFY — refactor Reset (:4192) + UpdateRetakeSettings + bulk-add (:2166)
Program.cs                               # MODIFY — register service after :54
Migrations/
  *_AddRetakeColumnsAndArchive.cs        # CREATE via dotnet ef (RTK-01/02)
HcPortal.Tests/
  RetakeRulesTests.cs                    # CREATE — pure unit (mirror ShuffleToggleRulesTests)
  RetakeArchiveBuilderTests.cs           # CREATE — pure unit
  RetakeServiceTests.cs (Integration)    # CREATE — IClassFixture disposable-DB MigrateAsync
```

### Pattern 1: Pure helper, no-DI (RetakeRules / RetakeArchiveBuilder)
**What:** Static class, hanya `System.Linq` + `HcPortal.Models`, EF-free, sinkron — caller suplai fakta.
**When to use:** Keputusan logika yang dipakai DI DUA TEMPAT (service + Phase 407 ViewModel) supaya tak divergen (kill-drift Phase 363/365/376).
**Example:**
```csharp
// Source: Helpers/ShuffleToggleRules.cs (verified)
public static class ShuffleToggleRules
{
    public static bool ShouldHideShuffleToggle(string? category, string? tahunKe, bool isManualEntry)
        => (category == "Assessment Proton" && tahunKe == "Tahun 3") || isManualEntry;
}
// RetakeRules.ShouldHideRetakeToggle mengikuti pola IDENTIK (tapi Proton TETAP retakeable — beda dari shuffle).
```

### Pattern 2: Sibling propagation endpoint (UpdateRetakeSettings ← UpdateShuffleSettings)
**What:** `[HttpPost] [Authorize(Roles="Admin, HC")] [ValidateAntiForgeryToken]` → sibling key `(Title, Category, Schedule.Date)` → foreach set → SaveChanges → audit try/catch warn-only → PRG redirect.
**Example (verified anchor):**
```csharp
// Source: AssessmentAdminController.cs:5563-5567 (sibling key — COPY VERBATIM)
var siblingSessionIds = await _context.AssessmentSessions
    .Where(s => s.Title == assessment.Title &&
                s.Category == assessment.Category &&
                s.Schedule.Date == assessment.Schedule.Date)
    .Select(s => s.Id)
    .ToListAsync();
// Schedule adalah DateTime (AssessmentSession.cs:18) → .Date legal.
```
**Beda dari shuffle:** retake TIDAK punya lock saat ujian mulai (config policy boleh ubah kapan saja) — JANGAN copy `IsShuffleLocked` guard. Hanya guard `ShouldHideRetakeToggle` (PreTest/Manual). Warning `MaxAttempts < attemptsUsed` = non-blocking (tetap simpan).

### Pattern 3: Atomic claim-transition (must-fix #4)
**What:** `ExecuteUpdateAsync` ber-`WHERE` status-guard + cek `rowsAffected==0` → abort. Bypass change-tracker, atomic di DB.
**Example (verified anchor):**
```csharp
// Source: CMPController.cs:1283-1294 (CancelExam) + AssessmentAdminController.cs:4288 (ResetAssessment existing)
var rows = await _context.AssessmentSessions
    .Where(s => s.Id == id && s.Status != "Cancelled" && s.Status != "Open")  // exclude Open → anti re-claim
    .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, "Open") /* + null-out fields */);
if (rows == 0) return new RetakeResult(false, "Sesi tidak dapat direset...");  // race lost
```
**Kunci urutan:** claim DULU. ResetAssessment existing archive→delete→claim (`:4239-4288`); RetakeService HARUS claim→archive→delete (balik). Pakai konstanta `AssessmentConstants.AssessmentStatus.Open/Completed/Cancelled` (`using S = ...` pola GradingService.cs:5) — bukan magic string.

### Anti-Patterns to Avoid
- **Re-grade inline di archive builder:** JANGAN hitung benar/salah manual — pakai `AssessmentScoreAggregator.IsQuestionCorrect` (kill-drift). Verdict inline akan divergen dari Results/PDF/Excel.
- **Archive sebelum claim:** ResetAssessment existing rentan double-archive (double-click) — claim-first menutup ini.
- **Counting `(UserId, Title)` saja:** konflasi Pre+Post ber-Title sama. WAJIB `+Category` (must-fix #3).
- **TempData.Remove di service:** service tak punya HTTP context. Clear di caller (controller/RetakeExam).
- **Status constant magic-string:** pakai `S.Open`/`S.Completed`/`S.Cancelled` (discipline v22.0).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Verdict benar/salah per-soal | Re-grade MC/MA/Essay inline | `AssessmentScoreAggregator.IsQuestionCorrect(q, respForQ)` [Helpers/AssessmentScoreAggregator.cs:73] | bool? (null=essay pending); MA non-empty guard; single-source kill-drift |
| Display jawaban worker | Format opsi/teks manual | `AssessmentScoreAggregator.BuildAnswerCell(q, respForQ)` [:110] | MA join order-by-Id; MC OptionText; Essay truncate 300; "—" untuk kosong |
| Anti double-archive race | Lock/flag manual | `ExecuteUpdateAsync` status-guard + rowsAffected | atomic DB-level (CMPController.cs:1283) |
| Sibling batch propagation | Query manual ad-hoc | Pola `UpdateShuffleSettings:5563` (Title/Category/Schedule.Date) | terbukti konsisten StartExam/Reshuffle/Edit |
| Audit row | new AuditLog inline | `_auditLog.LogAsync(actor, name, type, desc, targetId, "AssessmentSession")` [Services/AuditLogService.cs:21] | SaveChanges internal; konsisten |
| Attempt counting (AttemptNumber) | Window function/SQL kustom | `CountAsync` atas AttemptHistory + filter snapshot-presence | reuse pola existing `:4241`, lihat D-01 |

**Key insight:** Inti retake adalah PEMINDAHAN logika existing `ResetAssessment`, bukan penulisan ulang. Satu-satunya kode benar-benar baru: snapshot builder + claim-first reorder + counting-fix. Sisanya port verbatim.

## D-01 Mechanism Resolution (CRITICAL — deviasi dari spec)

### Bukti masalah (query DB live)
```
SELECT COUNT(*) FROM AssessmentAttemptHistory;  →  5 baris
TOP 5: semua AttemptNumber=1, ArchivedAt 2026-01..04, UserId 4a624dbc...
```
[VERIFIED: sqlcmd HcPortalDB_Dev 2026-06-21]. Ini arsip HC-reset LAMA (pre-v32.4). Dengan spec polos `attemptsUsed = count(UserId,Title,Category)+1`, seorang pekerja yang punya 1 arsip legacy untuk assessment X langsung `attemptsUsed=2` → dengan `MaxAttempts=2` default, `attemptsUsed >= MaxAttempts` → **TERKUNCI seketika** saat `AllowRetake` di-ON. Itu melanggar D-01.

### Rekomendasi: snapshot-presence discriminator
**Hitung hanya `AssessmentAttemptHistory` yang memiliki ≥1 baris anak `AssessmentAttemptResponseArchive`.** Arsip legacy mustahil punya snapshot (tabel `AssessmentAttemptResponseArchives` baru lahir di migration ini), jadi natural-excluded. Hanya arsip yang dibuat oleh `RetakeService` (yang selalu menulis snapshot saat `wasCompleted`) yang dihitung.

**Mengapa lebih robust dari date-cutoff:**
- Tak butuh kolom diskriminator baru atau hardcode tanggal cutoff yang rapuh (clock/DST/timezone).
- Tak butuh data-migration baris legacy (D-04 retain-all terpenuhi — legacy tetap tersimpan, hanya tak dihitung).
- Self-consistent: "punya snapshot" ≡ "dibuat era-retake" by construction.

**Exact query shape (untuk `attemptsUsed`):**
```csharp
// attemptsUsed = jumlah arsip ERA-RETAKE (punya snapshot) + 1 (attempt current)
// grouping (UserId, Title, Category) — must-fix #3 anti-konflasi Pre/Post
int eraRetakeArchives = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == session.UserId
             && h.Title == session.Title
             && h.Category == session.Category
             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
    .CountAsync();
int attemptsUsed = eraRetakeArchives + 1;
```

**CATATAN penting tentang `AttemptNumber` di archive (RTK-07):** `AttemptNumber` yang ditulis ke baris `AssessmentAttemptHistory` baru BOLEH tetap pakai count total existing (`:4241` pola) ATAU count era-retake — keputusan planner. Yang WAJIB pakai snapshot-presence hanyalah `attemptsUsed` untuk `CanRetake` cap-check. Rekomendasi: `AttemptNumber = eraRetakeArchives + 1` agar konsisten ("Percobaan ke-N" yang dilihat pekerja = N era-retake, bukan termasuk HC-reset legacy). Tapi ini affecting display Phase 407; di 405 cukup pastikan field terisi dan monoton naik.

**Test pembukti (WAJIB ada — RTK-13/D-01):**
- `CanRetake_LegacyArchiveWithoutSnapshot_DoesNotConsumeCap`: seed 1 `AssessmentAttemptHistory` TANPA `AssessmentAttemptResponseArchive` child + session failed → `eraRetakeArchives==0` → `attemptsUsed==1 < MaxAttempts=2` → `CanRetake==true`. (Integration, real-SQL — query EXISTS perlu DB.)
- `CanRetake_RetakeEraArchiveWithSnapshot_ConsumesCap`: seed 1 AttemptHistory DENGAN ≥1 archive child → `eraRetakeArchives==1` → `attemptsUsed==2 == MaxAttempts` → `CanRetake==false`.

**Catatan arsitektur:** `RetakeRules.CanRetake` tetap PURE (terima `attemptsUsed` int sebagai param — tak query DB). Logika "hitung snapshot-presence" hidup di `RetakeService.CanRetakeAsync` (DB-aware) yang memanggil `RetakeRules.CanRetake(...)` dengan `attemptsUsed` hasil query di atas. Pemisahan ini menjaga `RetakeRules` unit-testable (semua cabang) DAN counting integration-testable terpisah.

## Implementation Map (per component)

### RTK-01 — 3 kolom config (AssessmentSession + bulk-add)
**File:** `Models/AssessmentSession.cs`
**Insertion point:** setelah line 42 (`public bool ShuffleOptions { get; set; } = true;`)
**Mirror:** kolom shuffle `:38-42` (pola `[Display]` + default).
```csharp
[Display(Name = "Izinkan Ujian Ulang")]      public bool AllowRetake { get; set; } = false;
[Range(1, 5)][Display(Name = "Maksimal Percobaan")] public int MaxAttempts { get; set; } = 2;
[Range(0, 168)][Display(Name = "Jeda Ujian Ulang (jam)")] public int RetakeCooldownHours { get; set; } = 24;
```
**Bulk-add explicit copy — HANYA standard add-users:**
- `AssessmentAdminController.cs:2166-2185` (`newSessions = filteredNewUserIds.Select(uid => new AssessmentSession { ... ShuffleQuestions = savedAssessment.ShuffleQuestions ... })`) — TAMBAH `AllowRetake = savedAssessment.AllowRetake, MaxAttempts = savedAssessment.MaxAttempts, RetakeCooldownHours = savedAssessment.RetakeCooldownHours` (sumber = `savedAssessment`, sesi nyata).
- ⚠️ **JANGAN** tambah di pre/post block `:1944` (`newPre`) / `:1965` (`newPost`) — keduanya copy dari `model` (ViewModel), yang BELUM punya field retake di Phase 405 (binding UI = Phase 406). Di 405 jalur ini AMAN jatuh ke EF default (false/2/24) — itu benar untuk Pre (AllowRetake=false sesuai D6 diagnostik) dan Post (default off sampai admin ON). Menambah `model.AllowRetake` di 405 = compile error (property belum di ViewModel). Catat untuk Phase 406: wire `model` field lalu propagate ke pre/post jika diinginkan.
- Other create-paths (`:684` model, `:1240` preSession, `:1276` postSession, `:1464` session) → EF default cukup (RTK-01 acceptance: "EF default cukup").

### RTK-02 — Tabel snapshot + builder
**File CREATE:** `Models/AssessmentAttemptResponseArchive.cs`
Fields (verified vs spec §4.2): `Id`, `AttemptHistoryId` (int FK), `AttemptHistory` (nav, nullable), `PackageQuestionId` (plain int, no FK), `QuestionText` (string=""), `AnswerText` (string?), `IsCorrect` (bool?), `AwardedScore` (int), `ArchivedAt` (DateTime).

**File MODIFY:** `Data/ApplicationDbContext.cs`
- DbSet setelah line 68 (`public DbSet<AssessmentAttemptHistory> AssessmentAttemptHistory { get; set; }`):
  ```csharp
  public DbSet<AssessmentAttemptResponseArchive> AssessmentAttemptResponseArchives { get; set; }
  ```
- Entity config dalam `OnModelCreating` (mirror block `:571-581`, sisipkan dekat sana):
  ```csharp
  builder.Entity<AssessmentAttemptResponseArchive>(entity =>
  {
      entity.HasIndex(e => e.AttemptHistoryId);
      entity.HasOne(e => e.AttemptHistory).WithMany()
            .HasForeignKey(e => e.AttemptHistoryId)
            .OnDelete(DeleteBehavior.Cascade);
  });
  ```
  (`AssessmentAttemptHistory` config sudah `WithMany()` untuk User nav — pola identik.)

**File CREATE:** `Helpers/RetakeArchiveBuilder.cs` (pure). Signature: `Build(int attemptHistoryId, IEnumerable<PackageQuestion> questions, IEnumerable<PackageUserResponse> responses) → List<AssessmentAttemptResponseArchive>`. Verdict `IsQuestionCorrect`, answer `BuildAnswerCell`, AwardedScore = essay→`EssayScore ?? 0`, MC/MA→`verdict==true ? ScoreValue : 0`. Lihat plan superpowers Task 4 (kode sudah benar) — KECUALI lihat Pitfall 2 (AnswerText essay truncate).

### RTK-03 — RetakeRules (pure)
**File CREATE:** `Helpers/RetakeRules.cs`. Signature `CanRetake(bool allowRetake, string? assessmentType, bool isManualEntry, string status, bool? isPassed, int attemptsUsed, int maxAttempts, int retakeCooldownHours, DateTime? completedAt, DateTime nowUtc) → bool` + `ShouldHideRetakeToggle(string? assessmentType, bool isManualEntry) → bool`. Kode plan superpowers Task 3 sudah benar (cabang sesuai spec §5). Test mirror `HcPortal.Tests/ShuffleToggleRulesTests.cs` (pure, no fixture).

### RTK-04 — UpdateRetakeSettings
**File MODIFY:** `AssessmentAdminController.cs`, sisipkan setelah `UpdateShuffleSettings` (`:5612`).
Mirror `:5552-5612` VERBATIM untuk: atribut `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]` (`:5553-5555`), sibling key (`:5563-5567`), foreach-set+SaveChanges, audit try/catch warn-only (`:5596-5608`), PRG (`:5610-5611`). PERBEDAAN: ganti lock-guard `IsShuffleLocked` dengan `ShouldHideRetakeToggle` guard + `Math.Clamp(maxAttempts,1,5)` / `Math.Clamp(retakeCooldownHours,0,168)`. Kode plan superpowers Task 7 sudah benar.

### RTK-06 — Refactor ResetAssessment
**File MODIFY:** `AssessmentAdminController.cs:4192-4331`.
- KEEP `:4194-4197` (load+null), `:4199-4208` (IsResettable guard), `:4210-4225` (Pre-Post block), `:4227-4236` (status guard), `:4325-4330` (success TempData+redirect).
- REPLACE `:4238-4323` (archive→delete→claim→audit→SignalR inline) dengan satu `await _retakeService.ExecuteAsync(...)` + error-redirect + `TempData.Remove($"TokenVerified_{id}")`.
- Inject `RetakeService` sebagai param ke-13 di constructor (`:33-56`); field `private readonly RetakeService _retakeService;`. Catatan: `_context`/`_userManager`/`_auditLog` datang dari `AdminBaseController` (base) — tak perlu re-inject.
- `IsResettable` (`:4186`) TETAP static (ResetGuardTests pure depend padanya).

### RTK-07 — RetakeService
**File CREATE:** `Services/RetakeService.cs` (+ optional `IRetakeService`). DI deps: `ApplicationDbContext`, `AuditLogService`, `IHubContext<AssessmentHub>`, `ILogger<RetakeService>`. Return `record struct RetakeResult(bool Success, string? Error)`.
**File MODIFY:** `Program.cs` setelah line 54 (`AddScoped<GradingService>`):
```csharp
builder.Services.AddScoped<HcPortal.Services.RetakeService>();
```
**Urutan WAJIB (beda dari ResetAssessment existing):** claim-atomik (1) → snapshot+archive (2,3) → delete (4) → audit (5) → SignalR (6). Existing `:4239-4286` archive/delete DULU lalu claim `:4288` — RetakeService balik. AttemptNumber+counting pakai snapshot-presence (lihat D-01). Audit `LogAsync` setelah delete SaveChanges (pola `:4315`). SignalR payload `new { reason }` (parameterized — client `StartExam.cshtml:1287` abaikan reason, backward-compat aman).

## Common Pitfalls

### Pitfall 1: Claim-transition urutan (anti double-archive)
**What goes wrong:** ResetAssessment existing archive+delete DULU (`:4239-4286`), claim BELAKANGAN (`:4288`). Dua request paralel (double-click "Ujian Ulang") bisa dua-duanya lolos archive → dua baris AttemptHistory + dua snapshot set.
**Why:** Tidak ada gate atomik sebelum archive.
**How to avoid:** RetakeService claim DULU (`ExecuteUpdateAsync WHERE Status NOT IN (Cancelled,Open)`); `rowsAffected==0` → abort SEBELUM menyentuh archive. `Status != "Open"` di WHERE mencegah re-claim sesi yang sudah di-Open oleh request pertama.
**Warning signs:** Dua `AssessmentAttemptHistory` dengan `ArchivedAt` berdekatan untuk satu (UserId,Title,Category) + AttemptNumber sama.

### Pitfall 2: Essay AnswerText truncate 300 char di archive
**What goes wrong:** `BuildAnswerCell` untuk Essay me-truncate `TextAnswer` ke 300 char (`AssessmentScoreAggregator.cs:120` — itu untuk DISPLAY PDF/Excel). Archive idealnya menyimpan jawaban LENGKAP (D10 full snapshot, riwayat ISO 17024).
**Why:** Builder plan superpowers pakai `BuildAnswerCell` mentah → essay >300 char terpotong di arsip permanen.
**How to avoid:** Untuk Essay, simpan `responseForQ.TextAnswer` penuh ke `AnswerText` (atau perbesar/buang truncate). MC/MA tetap `BuildAnswerCell` (OptionText, tak ter-truncate). Planner putuskan: builder cabang per-type, ATAU terima truncate-300 sebagai "display snapshot" (jika riwayat memang display-only). REKOMENDASI: simpan full untuk essay (cheap, audit-safe).
**Warning signs:** Riwayat essay attempt lama berakhir "..." padahal jawaban asli lebih panjang.

### Pitfall 3: Counting (UserId,Title) konflasi Pre/Post (must-fix #3)
**What goes wrong:** ResetAssessment existing `:4242` count `(UserId, Title)` SAJA. Pre+Post ber-Title sama → attempt Pre menghitung cap Post (dan sebaliknya).
**How to avoid:** SELALU `+Category` (atau lebih tepat: Pre=AllowRetake false jadi tak retakeable, tapi counting tetap harus benar). `(UserId, Title, Category)` di SEMUA query attemptsUsed + AttemptNumber.
**Warning signs:** Test `SiblingPrePostFilterTests`-style gagal; pekerja Post terkunci karena attempt Pre.

### Pitfall 4: TempData token clear (must-fix #1)
**What goes wrong:** `StartExam.cshtml`→controller `StartExam` baca token via `TempData.Peek($"TokenVerified_{id}")` (`CMPController.cs:944` — NON-consuming, value bertahan). Setelah retake, flag stale tetap ada → worker re-entry TANPA verifikasi token ulang.
**How to avoid:** Caller (ResetAssessment di 405; RetakeExam di 407) WAJIB `TempData.Remove($"TokenVerified_{id}")` setelah ExecuteAsync sukses. TIDAK bisa di service (TempData HTTP-scoped). `TempData[...]=true` di-set di `CMPController.cs:884,895`.
**Warning signs:** Worker masuk ujian ulang tanpa diminta token meski `IsTokenRequired=true`.

### Pitfall 5: EF default coverage semua create-path
**What goes wrong:** Asumsi salah bahwa EF default menutup SEMUA — pre/post bulk-add copy dari `model` (bukan saved session); di 405 itu OK (default), tapi menambah `model.AllowRetake` = compile error.
**How to avoid:** Explicit copy HANYA di standard add-users `:2166` (sumber `savedAssessment`). Verifikasi: `grep "new AssessmentSession"` → 8 site (`:684,1240,1276,1464,1944,1965,2166`); hanya `:2166` copy dari sibling-session existing.
**Warning signs:** Build error "AssessmentSessionViewModel does not contain AllowRetake" jika salah tambah di pre/post.

### Pitfall 6: Migration snapshot version / filtered-index
**What goes wrong:** Global `dotnet ef` 10.0.3 vs project EF 8.0.0 — snapshot ProductVersion harus tetap `8.0.0` (drift ke 10.x = chain break). Tabel archive TIDAK punya filtered index/unique partial (beda dari `NomorSertifikat`/PendingProtonBypass), jadi tak ada gotcha QUOTED_IDENTIFIER untuk INSERT.
**How to avoid:** Setelah `migrations add`, verifikasi snapshot `ProductVersion` tetap `"8.0.0"` (`ApplicationDbContextModelSnapshot.cs:20`). `Up()` harus berisi 3 `AddColumn` (bit default false / int default 2 / int default 24) + 1 `CreateTable` archive + FK cascade + index AttemptHistoryId. `dotnet build` + `dotnet ef migrations list` (pending). Integration test `MigrateAsync` membuktikan chain utuh.
**Warning signs:** Snapshot ProductVersion berubah 10.x; integration fixture `MigrateAsync` throw "MIGRATION-CHAIN break".

### Pitfall 7: SignalR reason parameterization (backward-compat)
**What goes wrong:** Mengganti hardcode `new { reason = "hc_reset" }` (`:4323`) jadi param `reason` — pastikan client tak break.
**How to avoid:** Client handler `StartExam.cshtml:1287` (`function(payload){...}`) saat ini ABAIKAN `payload.reason` (cuma show modal). Parameterize aman di 405; Phase 407 nanti consume `payload.reason` untuk pesan beda. No client change di 405.

### Pitfall 8: ResetGuardTests purity (regresi RTK-06)
**What goes wrong:** Refactor memindahkan `IsResettable` ke instance/DB → `ResetGuardTests` (pure static, `HcPortal.Tests/ResetGuardTests.cs`) break.
**How to avoid:** `IsResettable(AssessmentSession)` TETAP `public static` (`:4186`). Guard tetap di controller. Test panggil `AssessmentAdminController.IsResettable(new AssessmentSession{...})` tanpa DI.

## Code Examples

### CanRetakeAsync (service — bungkus pure RetakeRules dengan counting DB-aware)
```csharp
// Source: derived — RetakeRules pure (Helpers/ShuffleToggleRules.cs pattern) + counting query (D-01)
public async Task<bool> CanRetakeAsync(int sessionId)
{
    var s = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == sessionId);
    if (s == null) return false;
    int eraRetakeArchives = await _context.AssessmentAttemptHistory
        .Where(h => h.UserId == s.UserId && h.Title == s.Title && h.Category == s.Category
                 && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
        .CountAsync();
    return RetakeRules.CanRetake(
        s.AllowRetake, s.AssessmentType, s.IsManualEntry, s.Status, s.IsPassed,
        attemptsUsed: eraRetakeArchives + 1, s.MaxAttempts, s.RetakeCooldownHours,
        s.CompletedAt, DateTime.UtcNow);
}
```

### Aggregator usage (archive builder verdict — verified signature)
```csharp
// Source: Helpers/AssessmentScoreAggregator.cs:73,110 (verified)
bool? verdict = AssessmentScoreAggregator.IsQuestionCorrect(q, responsesForQ);   // null=essay pending
string answer  = AssessmentScoreAggregator.BuildAnswerCell(q, responsesForQ);    // "—" untuk kosong
```

## Runtime State Inventory

> Phase 405 = greenfield backend + data-model addition (bukan rename). Inventory ringkas relevan:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | 5 baris legacy `AssessmentAttemptHistory` (HC-reset Jan–Apr 2026) di `HcPortalDB_Dev` [VERIFIED query] | TIDAK dimigrasikan/dihapus (D-04 retain). Snapshot-presence discriminator (D-01) menatural-exclude dari cap. Tabel archive baru = kosong saat lahir. |
| Live service config | None — config retake (3 kolom) lahir dari EF default; tak ada state eksternal | None |
| OS-registered state | None | None |
| Secrets/env vars | None — tak ada secret/env baru | None |
| Build artifacts | Migration baru `AddRetakeColumnsAndArchive` + snapshot update; test DB disposable `HcPortalDB_Test_{guid}` regen via MigrateAsync | IT apply migration ke Dev/Prod (migration=TRUE, 1 entri) |

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| HC-reset = data-loss per-soal | Snapshot archive sebelum delete | Phase 405 (this) | Riwayat per-soal beku, tahan edit/hapus soal |
| Counting `(UserId,Title)` | `(UserId,Title,Category)` + snapshot-presence | Phase 405 | Anti-konflasi Pre/Post + anti-cap-legacy |
| archive→delete→claim | claim→archive→delete | Phase 405 | Anti double-archive |
| SignalR `reason` hardcode "hc_reset" | parameterized | Phase 405 | Phase 407 bisa pesan worker_retake |

**Deprecated/outdated:** Tidak ada. Semua adalah penambahan; `ResetAssessment` inline block di-port (bukan dibuang) ke service.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `AttemptNumber` archive boleh pakai count era-retake (bukan total) untuk konsistensi "Percobaan ke-N" pekerja | D-01 / RTK-07 | LOW — display Phase 407; di 405 cukup monoton-naik. Planner konfirmasi semantik. |
| A2 | Pre/post bulk-add (`:1944/:1965`) jatuh ke EF default di 405 adalah perilaku BENAR (bukan bug) | RTK-01 | LOW — Pre=false sesuai D6; Post default-off benar. Phase 406 wire `model` jika perlu propagate. |
| A3 | Truncate-300 essay di `BuildAnswerCell` tidak diinginkan untuk arsip permanen (simpan full) | Pitfall 2 | MEDIUM — jika riwayat memang display-only, truncate OK. Planner/discuss konfirmasi. |

**Catatan:** Tidak ada `[ASSUMED]` pada fakta compliance/security/schema — semua diverifikasi. A1–A3 adalah pilihan desain, bukan fakta tak terverifikasi.

## Open Questions

1. **AttemptNumber semantik (era-retake vs total)**
   - What we know: counting cap WAJIB era-retake (snapshot-presence). AttemptNumber field bebas.
   - What's unclear: apakah "Percobaan ke-N" pekerja menghitung HC-reset legacy?
   - Recommendation: `AttemptNumber = eraRetakeArchives + 1` (konsisten cap). Final di Phase 407 display.

2. **Essay archive full-text vs display-truncate**
   - What we know: `BuildAnswerCell` truncate essay 300 char.
   - Recommendation: simpan `TextAnswer` full untuk Essay di builder (cheap, audit-safe). Konfirmasi via discuss bila ragu.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `dotnet ef` CLI (global) | Generate/apply migration | ✓ | 10.0.3 | Hand-author migration mirror `AddShuffleTogglesToAssessmentSession.cs` |
| SQL Server | DB + integration test | ✓ | localhost\SQLEXPRESS, `HcPortalDB_Dev` | — |
| EF Core (project) | ORM/snapshot | ✓ | 8.0.0 | — |
| sqlcmd | Verifikasi kolom/tabel | ✓ | (`-C -I` flags) | EF migrations list |
| xUnit | unit+integration | ✓ | (project) | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None (semua tersedia).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (project `HcPortal.Tests`) |
| Config file | none — konvensi: integration `[Trait("Category","Integration")]` + `IClassFixture` disposable-DB |
| Quick run command | `dotnet test --filter "Category!=Integration"` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RTK-03 | CanRetake semua cabang (allowRetake/PreTest/Manual/status/isPassed/cap/cooldown) | unit (pure) | `dotnet test --filter "FullyQualifiedName~RetakeRulesTests"` | ❌ Wave 0 |
| RTK-03 | ShouldHideRetakeToggle (PreTest\|\|Manual) | unit (pure) | (same) | ❌ Wave 0 |
| RTK-02 | Build snapshot: MC benar/salah+skor, essay pending null, answer text | unit (pure) | `dotnet test --filter "FullyQualifiedName~RetakeArchiveBuilderTests"` | ❌ Wave 0 |
| RTK-07 | Claim atomik anti-double-archive (rowsAffected==0 abort) | integration (real-SQL) | `dotnet test --filter "FullyQualifiedName~RetakeServiceTests"` | ❌ Wave 0 |
| RTK-07/D-01 | Legacy archive (no snapshot) TIDAK konsumsi cap → CanRetake true | integration (real-SQL) | (same) | ❌ Wave 0 |
| RTK-07/D-01 | Era-retake archive (with snapshot) konsumsi cap → CanRetake false | integration (real-SQL) | (same) | ❌ Wave 0 |
| RTK-07 | Counting (UserId,Title,Category) no-conflate Pre/Post | integration (real-SQL) | (same) | ❌ Wave 0 |
| RTK-07 | Snapshot rows ter-tulis sebelum responses dihapus (count==questions) | integration (real-SQL) | (same) | ❌ Wave 0 |
| RTK-06 | IsResettable manual-block tetap (regresi) | unit (pure) | `dotnet test --filter "FullyQualifiedName~ResetGuardTests"` | ✅ exists |
| RTK-06 | ET cleanup tetap berfungsi (regresi) | integration | `dotnet test --filter "FullyQualifiedName~ResetEtScoreTests"` | ✅ exists |
| RTK-01/02 | Migration chain utuh (kolom+tabel terbentuk) | integration (MigrateAsync) | `dotnet ef migrations list` + any integration fixture | ❌ Wave 0 (terbukti via fixture baru) |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "Category!=Integration"` (unit cepat <30s).
- **Per wave merge:** `dotnet test` (full, termasuk integration real-SQL — butuh SQLEXPRESS hidup).
- **Phase gate:** Full suite green + `dotnet ef migrations list` shows `AddRetakeColumnsAndArchive` pending (NOT applied ke Dev/Prod) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/RetakeRulesTests.cs` — pure, mirror `ShuffleToggleRulesTests.cs` — covers RTK-03
- [ ] `HcPortal.Tests/RetakeArchiveBuilderTests.cs` — pure, mirror plan Task 4 (+ essay full-text assert) — covers RTK-02
- [ ] `HcPortal.Tests/RetakeServiceTests.cs` — `[Trait("Category","Integration")]` + `IClassFixture` disposable-DB `MigrateAsync` (mirror `ResetEtScoreTests`/`EssayFinalizeRecomputeTests`) — covers RTK-07 + D-01 + counting
- [ ] Migration `AddRetakeColumnsAndArchive` harus ada di chain SEBELUM integration fixture `MigrateAsync` (jika tidak → chain break, semua integration gagal)
- Framework install: none (xUnit sudah ada)

## Security Domain

> `security_enforcement` tidak di-set false di config → enabled. Phase 405 backend.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Pure helper + service boundary; eligibility re-validated server-side (jangan percaya client) |
| V2 Authentication | no | Identity existing tak berubah |
| V3 Session Management | partial | TempData token clear (must-fix #1) — re-arm verifikasi token saat retake |
| V4 Access Control | yes | `UpdateRetakeSettings` `[Authorize(Roles="Admin, HC")]`; ResetAssessment idem. Worker path (407) ownership. |
| V5 Input Validation | yes | `Math.Clamp` MaxAttempts(1,5)/Cooldown(0,168) server-side (defense-in-depth atas `[Range]`) |
| V6 Cryptography | no | Tak ada crypto baru |
| V11 Business Logic | yes | Claim-atomik anti double-archive; cap/cooldown server-authoritative; no answer-key leak (kunci ditahan — Phase 407) |

### Known Threat Patterns for ASP.NET Core MVC + retake
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Double-click double-archive | Tampering | claim-transisi-atomik DULU (rowsAffected guard) |
| Cap/cooldown bypass via client | Elevation | re-validate `CanRetakeAsync` server-side (jangan trust client) — Phase 407 enforce; 405 sediakan API |
| CSRF pada config/reset | Tampering | `[ValidateAntiForgeryToken]` (sudah di Reset; wajib di UpdateRetakeSettings) |
| Cross-user reset | Elevation | RBAC Admin/HC (405); ownership UserId (worker 407) |
| Stale token re-entry | Spoofing | `TempData.Remove($"TokenVerified_{id}")` must-fix #1 |
| Legacy archive lock-out (DoS-by-policy) | DoS | snapshot-presence discriminator (D-01) |

## Sources

### Primary (HIGH confidence)
- Repo files (verified via Read/Grep, file:line cited): `AssessmentAdminController.cs` (:4186,:4192-4331,:5552-5612,:2166-2185,:1944-1986,:27-56), `Helpers/AssessmentScoreAggregator.cs` (:73,:110), `Helpers/ShuffleToggleRules.cs`, `Models/AssessmentSession.cs` (:18,:38-42,:44-45,:137,:161), `Models/AssessmentAttemptHistory.cs`, `Models/AssessmentPackage.cs`, `Models/PackageUserResponse.cs`, `Models/UserPackageAssignment.cs` (:60 GetShuffledQuestionIds), `Data/ApplicationDbContext.cs` (:62,:68,:571-581), `Services/GradingService.cs` (:5,:224,:263), `Services/AuditLogService.cs` (:21), `Program.cs` (:54), `CMPController.cs` (:884,:895,:944,:1283), `Views/CMP/StartExam.cshtml` (:1287), `HcPortal.Tests/ResetGuardTests.cs`, `HcPortal.Tests/ResetEtScoreTests.cs`, `HcPortal.Tests/EssayFinalizeRecomputeTests.cs`, `Migrations/20260613095102_AddShuffleTogglesToAssessmentSession.cs`, `Migrations/ApplicationDbContextModelSnapshot.cs` (:20).
- DB query live: `sqlcmd HcPortalDB_Dev` → 5 baris legacy AttemptHistory (D-01 proof).
- Tooling: `dotnet ef --version` → 10.0.3; `appsettings.Development.json:10` (connstr).
- `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` (16 bagian, AUTHORITATIVE).
- `docs/superpowers/plans/2026-06-19-v32.4-phase-405-backend-core.md` (9-task reference).

### Secondary (MEDIUM confidence)
- (none — semua claim primary-verified)

### Tertiary (LOW confidence)
- (none)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua in-repo, versi diverifikasi langsung (EF 8.0.0 snapshot, dotnet ef 10.0.3).
- Architecture: HIGH — pola mirror (ShuffleToggleRules, UpdateShuffleSettings, ResetAssessment) dibaca verbatim; signature aggregator confirmed.
- D-01 resolution: HIGH — masalah dibuktikan via query DB live (5 baris legacy); mekanisme snapshot-presence self-consistent by construction.
- Pitfalls: HIGH — setiap pitfall punya anchor file:line nyata.

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (stable — kode internal, tak bergantung sumber eksternal fast-moving). Re-verify hanya jika `AssessmentAdminController.cs`/`AssessmentScoreAggregator.cs` di-refactor sebelum eksekusi 405.
