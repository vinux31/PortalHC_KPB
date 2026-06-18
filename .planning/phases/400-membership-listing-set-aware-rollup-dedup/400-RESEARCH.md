# Phase 400: Membership Listing Set-Aware + Rollup Dedup (MU-06) - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (SQL Server) — read/filter-path refactor (set-aware membership predicate + contextual display column)
**Confidence:** HIGH (semua touchpoint diverifikasi langsung di kode live branch ITHandoff; 0 spekulasi)

## Summary

Phase 400 mengubah predikat filter unit dari scalar `u.Unit == unitFilter` menjadi keanggotaan set-aware di **4 query-path** (bukan 3 — lihat temuan kritis di bawah), plus membuat kolom `WorkerTrainingStatus.Unit` kontekstual di tabel CMP records team. Tidak ada migration (D-09 CONTEXT: read/filter-path murni). Scope kecil dan terkunci 6 keputusan (D-01..D-06).

**Temuan kritis #1 (BLOCKER kompilasi):** Decision D-01 menulis predikat sebagai `u.UserUnits.Any(uu => ...)`. **Navigation property `ApplicationUser.UserUnits` TIDAK ADA.** DbContext memasang relasi `UserUnit.HasOne(uu => uu.User).WithMany()` — `.WithMany()` **tanpa argumen** = tidak ada collection-navigation balik di `ApplicationUser`. Predikat HARUS ditulis sebagai **correlated subquery** terhadap `_context.UserUnits`: `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)`. Ini bukan re-decide D-01 (semantik `.Any` set-aware tetap) — hanya bentuk ekspresi C# yang benar agar compile + translate ke SQL `EXISTS`. [VERIFIED: Models/ApplicationUser.cs:1-73 tak ada UserUnits; Data/ApplicationDbContext.cs:343-344 `.WithMany()` no-arg]

**Temuan kritis #2 (scope creep terverifikasi):** Set-aware predikat menyentuh **4 consumer GetWorkersInSection**, bukan 3 yang disebut CONTEXT. Consumer ke-4 = `AssessmentAdminController.cs:278` (`ManageAssessmentTab` — area Kelola Data Section C) yang juga meneruskan `unit`. Ia akan otomatis mewarisi perilaku set-aware. Ini benign (konsisten dengan tujuan MU-06) tapi WAJIB masuk verification scope agar tak ada regresi diam-diam. [VERIFIED: grep GetWorkersInSection 4 call-site dengan unitFilter non-null]

**Primary recommendation:** Implement predikat set-aware via `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)` di `GetWorkersInSection:254-255`, `ManageWorkers:202-204`, `ExportWorkers:300-301`. Tambah 1 batch-load dict `UserUnits` (active-only) di `GetWorkersInSection` persis pola `WorkerController:224-232` untuk kolom kontekstual D-02. Unit-test predikat + dedup + contextual-string via EF InMemory (pola `WorkerDataServiceSearchTests`), simpan verifikasi SQL-real ke Phase 404.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** 1 baris per pekerja (predikat `.Any`, BUKAN gandakan baris). Set-aware = ubah predikat filter unit dari scalar `u.Unit == unitFilter` jadi keanggotaan `u.UserUnits.Any(uu => uu.Unit == unitFilter && uu.IsActive)`. Pekerja {X,Y}: filter unit-X tampil, filter unit-Y tampil, tanpa filter tampil **1 baris**. Dedup rollup otomatis (1 pekerja = 1 baris → denominator/completion% inheren benar). Baris-per-unit (gandakan) **DITOLAK**.
- **D-02:** Kolom Unit (`WorkerTrainingStatus.Unit`) **kontekstual**. Saat difilter unit-X → tampil `unitFilter`. Saat tanpa filter unit → tampil semua unit primary-first comma-join. `Unit = !string.IsNullOrEmpty(unitFilter) ? unitFilter : <semua unit aktif primary-first comma-join>`.
- **D-03:** Scope keanggotaan = `IsActive = true` saja. Unit yg sudah deactivate (MU-07) TIDAK muncul. Predikat: `uu.Unit == unitFilter && uu.IsActive`.
- **D-04:** `GetWorkersInSection` load `UserUnits` untuk users hasil filter (query dict `userId → units`, pola `WorkerController.ManageWorkers` `:221-231`). Primary-first ordering: `OrderByDescending(uu => uu.IsPrimary).ThenBy(uu => uu.Unit)`.
- **D-05:** Pekerja 0 baris UserUnits aktif (legacy/`Unit=null`) → fallback `WorkerTrainingStatus.Unit = user.Unit ?? "---"` (perilaku existing dipertahankan, 399 D-09).
- **D-06:** Kontekstual (D-02) berlaku **hanya** untuk `WorkerTrainingStatus.Unit` (kolom `_RecordsTeamBody.cshtml:27`). **ManageWorkers TIDAK di-rework display-nya** — cukup ubah predikat filter `WorkerController.cs:202-204` + `ExportWorkers:300-301`; badge display biarkan apa adanya.

### Claude's Discretion
- Bentuk perubahan `WorkerTrainingStatus` (set `.Unit` kontekstual in-place vs tambah field `UnitsCsv`/`AllUnits`) — ikut idiom model existing, minimal. → **Rekomendasi research: in-place set `.Unit`** (view `_RecordsTeamBody.cshtml:27` byte-stable, model tak berubah, sesuai UI-SPEC).
- `data-unit` (`_RecordsTeamBody.cshtml:18`) — tak ada pembaca client-side. Boleh ikut nilai kontekstual atau biarkan. → **Rekomendasi: biarkan `data-unit="@worker.Unit"`** (otomatis ikut nilai kontekstual, zero markup churn). [VERIFIED: RecordsTeam.cshtml filter unit server-side via param ke RecordsTeamPartial; JS `:408` hanya hitung `.worker-row` length, tak baca data-unit]
- OR-fallback scalar `u.Unit == unitFilter` di predikat — backfill 399 sudah cover semua `Unit` non-null + invariant #3 mirror dijaga write-through. **Lean: `.Any()` murni.** → **Rekomendasi: `.Any()` murni** (lihat analisis "OR-fallback" di Common Pitfalls — backfill 399 + invariant mirror membuat fallback redundan; tambah hanya bila ditemukan baris anomali saat verifikasi DB lokal).
- Styling/format koma-join ikut idiom Phase 399 (primary-first comma-join, teks polos).

### Deferred Ideas (OUT OF SCOPE)
- **Baris-per-unit (gandakan)** — ditolak (D-01). Roster grouped-by-unit eksplisit = fitur/phase tersendiri (butuh JOIN + dedup eksplisit + view group baru).
- **CMP analytics/renewal per-unit akurat** — out-of-scope milestone (D1=b primary; butuh kolom unit-at-issue + migration ke-2). Phase 400 hanya **verifikasi tak ada drift**.
- Cleanup DB test lokal pasca-367 (`2026-06-11-one-time-cleanup-...`) — tidak di-fold (di luar scope read/filter-path).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MU-06 | Listing keanggotaan set-aware (pekerja multi-unit muncul di tiap unit-nya) + rollup tingkat Bagian dedup (completion%/denominator tidak hitung ganda); CMP analytics/renewal TIDAK diubah (D1=b primary). 0 migration. | **Set-aware:** subquery `_context.UserUnits.Any(uu => uu.UserId==u.Id && uu.Unit==unitFilter && uu.IsActive)` di 3 predikat (Architecture Pattern 1). **Dedup by-construction:** `.Any()` = 1 row/worker, no JOIN fan-out (Pattern 2; pagination `workerList.Count` `:824` tetap akurat). **Kolom kontekstual:** batch-load dict + contextual string builder (Pattern 3). **No-drift D1=b:** analytics path `CMPController:2581` pakai `s.User!.Unit` scalar langsung (BUKAN GetWorkersInSection) → predikat set-aware tak menyala (Pattern 4, by-construction). |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Set-aware membership filter (3 predikat) | API/Backend (EF subquery → SQL `EXISTS`) | Database | Filter HARUS jalan di SQL sebelum `.ToListAsync()` materialize; junction query = tanggung jawab data layer |
| Kolom Unit kontekstual (D-02) | API/Backend (`WorkerDataService` set `.Unit`) | — | Server compute, view render as-is (UI-SPEC: cell value-driven, markup byte-stable) |
| Rendering 1 cell teks | Frontend Server (Razor `_RecordsTeamBody.cshtml:27`) | — | Tak ada perubahan markup; hanya nilai data berubah |
| Dedup rollup (completion%/denominator) | API/Backend (by-construction `.Any` 1-row/worker) | — | Dedup inheren dari D-01; bukan post-hoc `Distinct` |
| No-drift analytics (D1=b) | Database (existing `s.User.Unit` scalar GroupBy) | — | Analytics path TIDAK lewat GetWorkersInSection → tak terdampak, verifikasi pasif |

## Standard Stack

Brownfield — stack TERKUNCI. **TIDAK ADA dependency baru.** Phase 400 murni mengedit C# query logic + 0 perubahan markup view.

### Core (existing — verified versions)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Query translation predikat set-aware → SQL `EXISTS` | Project ORM [VERIFIED: HcPortal.csproj:18] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Unit-test query-path (`WorkerDataServiceSearchTests` pola) | Test project provider [VERIFIED: HcPortal.Tests.csproj:12] |
| ASP.NET Core MVC (net8.0) | 8.0 | Controller + Razor server-render | Project framework [VERIFIED: HcPortal.csproj:4] |
| xunit | 2.9.3 | Unit test framework | Project test runner [VERIFIED: HcPortal.Tests.csproj:15] |

**Installation:** Tidak ada. `dotnet build` + `dotnet run` (localhost:5277) sesuai CLAUDE.md Develop Workflow.

**Version verification:** Tidak relevan — 0 package baru. Semua versi dibaca langsung dari `.csproj`. [VERIFIED: HcPortal.csproj, HcPortal.Tests.csproj]

## Architecture Patterns

### System Architecture Diagram (data flow set-aware filter)

```
[User pilih unitFilter di RecordsTeam.cshtml dropdown]
        │ (server-side filter — JS kirim ?unit=X ke RecordsTeamPartial, BUKAN client filter)
        ▼
[CMPController.RecordsTeamPartial :819]  ──┐
[CMPController.ExportRecordsTeam* :710/:771]├──► GetWorkersInSection(section, unit=X, ...)
[AssessmentAdminController :278]           ──┘        │
                                                      ▼
                          ┌──────────────────────────────────────────────┐
                          │ WorkerDataService.GetWorkersInSection         │
                          │                                                │
                          │ usersQuery = _context.Users.Where(IsActive)   │
                          │   .Where(Section == section)                   │
                          │   ┌─ unitFilter? ─────────────────────────┐   │
                          │   │ SET-AWARE (NEW):                       │   │
                          │   │ .Where(u => _context.UserUnits.Any(    │   │  ──► SQL: WHERE EXISTS(
                          │   │   uu => uu.UserId==u.Id &&             │   │           SELECT 1 FROM UserUnits
                          │   │   uu.Unit==unitFilter && uu.IsActive)) │   │           WHERE UserId=Users.Id
                          │   └────────────────────────────────────────┘   │           AND Unit=@p AND IsActive=1)
                          │   .ToListAsync()  ──► 1 row/worker (no fan-out) │
                          │                                                │
                          │ unitsByUser = _context.UserUnits               │  ──► 1 batch query (no N+1)
                          │   .Where(userIds.Contains && IsActive)         │      group-by userId → dict
                          │   .GroupBy(UserId) primary-first ordering      │
                          │                                                │
                          │ foreach user: worker.Unit =                    │
                          │   unitFilter ?? <units primary-first join>     │  ◄── D-02 contextual
                          │   ?? user.Unit  (D-05 fallback)                │
                          └──────────────────────────────────────────────┘
                                                      │
                                                      ▼
                          [List<WorkerTrainingStatus> — 1 entry/worker]
                                                      │
                          paging by workerList.Count (:824) ── akurat (1 row/worker)
                                                      ▼
                          [_RecordsTeamBody.cshtml :27 render @worker.Unit — markup unchanged]

SEPARATE / UNTOUCHED PATH (D1=b no-drift):
[CMPController analytics :2579-2589] ──► baseQuery.Where(s.User!.Unit == unit).GroupBy(s.User.Section)
        (langsung AssessmentSession.User.Unit scalar mirror — TIDAK lewat GetWorkersInSection → 0 drift)
[CMPController Team View :543] ──► GetWorkersInSection(sectionFilter) tanpa unitFilter
        (predikat set-aware tak menyala krn unitFilter null → 0 drift)
```

### Pattern 1: Set-aware membership predicate (correlated subquery — NOT navigation)

**What:** Ganti scalar `u.Unit == unitFilter` dengan EXISTS-subquery terhadap junction `UserUnits`.
**When to use:** Ketiga predikat filter unit (`GetWorkersInSection:254-255`, `ManageWorkers:202-204`, `ExportWorkers:300-301`).
**CRITICAL:** Gunakan subquery `_context.UserUnits.Any(...)`, BUKAN `u.UserUnits.Any(...)` — navigation tak ada (lihat Temuan kritis #1).

```csharp
// Source: pola existing _context.UserUnits subquery (AccountController.cs:155, HomeController.cs:61,
//   WorkerController.cs:224 — semua akses UserUnits via _context, bukan nav property)
// GANTI WorkerDataService.cs:254-255:
//   if (!string.IsNullOrEmpty(unitFilter))
//       usersQuery = usersQuery.Where(u => u.Unit == unitFilter);
// MENJADI:
if (!string.IsNullOrEmpty(unitFilter))
    usersQuery = usersQuery.Where(u =>
        _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive));
```
[VERIFIED: subquery pattern compiles & translates — identik gaya `_context.UserUnits.Where(...)` di AccountController.cs:155, WorkerController.cs:224]

EF Core 8 menerjemahkan `_context.X.Any(predicate-correlated-on-outer)` di dalam `Where()` menjadi SQL `WHERE EXISTS (SELECT 1 ...)`. Ini idiom EF Core standar (correlated subquery via DbSet di body Where lambda). [CITED: learn.microsoft.com/ef-core/querying — "you can write a subquery that references the outer query"] [ASSUMED: terjemahan persis ke EXISTS — verifikasi via `dotnet run` + cek SQL log / DB lokal pada Phase 400 task verifikasi, dan SQL-real di Phase 404]

### Pattern 2: Dedup by-construction (no Distinct, no JOIN)

**What:** Karena predikat `.Any()` adalah subquery boolean (bukan JOIN ke UserUnits), `_context.Users` tetap menghasilkan **1 baris per user**. Tak ada fan-out → tak perlu `Distinct(WorkerId)` post-hoc. Rollup Bagian (completion%/pass-rate/denominator) yang dihitung dari `workerList` otomatis dedup.
**When to use:** By-construction — tidak ada kode tambahan. Justru JANGAN tambah `.Distinct()` (anti-pattern, menyamarkan fan-out yang seharusnya tak ada).
**Proof points (verified):**
- `RecordsTeamPartial:824` paginate via `workerList.Count` → akurat (1 row/worker). [VERIFIED: CMPController.cs:824]
- `RecordsTeam.cshtml:408` JS hitung `.worker-row` length → akurat. [VERIFIED]
- `AssessmentAdminController:280` paginate via `fullList.Count` → akurat. [VERIFIED]
- Export team `:711/:772` pakai `filteredWorkers.Select(w => w.WorkerId)` → 1 id/worker, no dup. [VERIFIED]

### Pattern 3: Contextual Unit column + batch-load (no N+1)

**What:** Load 1 dict `userId → List<unit>` (active-only, primary-first), set `worker.Unit` kontekstual.
**When to use:** `GetWorkersInSection` saja (D-06 — ManageWorkers TIDAK dapat ini).

```csharp
// Source: pola WorkerController.cs:224-232 (UserUnitsDict batch-load) — reuse persis, tambah filter IsActive
// Sisipkan SETELAH `var userIds = users.Select(u => u.Id).ToList();` (WorkerDataService.cs:271):
var unitsByUser = (await _context.UserUnits
        .Where(uu => userIds.Contains(uu.UserId) && uu.IsActive)   // D-03 active-only
        .ToListAsync())
    .GroupBy(uu => uu.UserId)
    .ToDictionary(
        g => g.Key,
        g => g.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Unit)   // D-04 primary-first
              .Select(x => x.Unit).ToList());

// Lalu GANTI WorkerDataService.cs:347  `Unit = user.Unit ?? "",`  MENJADI:
Unit = !string.IsNullOrEmpty(unitFilter)
    ? unitFilter                                                    // D-02 filtered → matched unit
    : (unitsByUser.TryGetValue(user.Id, out var uList) && uList.Count > 0
        ? string.Join(", ", uList)                                 // D-02 unfiltered → all active, primary-first
        : (user.Unit ?? "")),                                      // D-05 fallback (view `?? "---"` guard null)
```
[VERIFIED: pola batch-load identik WorkerController.cs:224-232; ordering identik AccountController.cs:159-162]

**N+1 avoidance:** 1 query batch (`userIds.Contains`) sejalan gaya CMP-25 (`trainingsByUser`/`sessionsByUser` di WorkerDataService.cs:283/295). [VERIFIED]

### Pattern 4: No-drift D1=b (verifikasi pasif, by-construction)

**What:** Analytics/renewal TIDAK lewat GetWorkersInSection; mereka query `AssessmentSession.User.Unit` scalar langsung. Team View list (`:543`) panggil GetWorkersInSection **tanpa** unitFilter → predikat set-aware tak menyala.
**When to use:** Verification step — bukan kode. Konfirmasi via diff bahwa tak ada perubahan di path analytics.
**Evidence:**
- `CMPController:2581` `baseQuery.Where(s => s.User!.Unit == unit)` — scalar mirror, terpisah. [VERIFIED]
- `CMPController:2589` `GroupBy(s => s.User!.Section)` — Section-level, tak sentuh unit. [VERIFIED]
- `CMPController:543` `GetWorkersInSection(sectionFilter)` — no unitFilter arg → `if(!string.IsNullOrEmpty(unitFilter))` false → predikat skip. [VERIFIED]

### Anti-Patterns to Avoid
- **`u.UserUnits.Any(...)` (nav property):** TIDAK compile — nav tak ada. Pakai `_context.UserUnits.Any(...)`.
- **`.Distinct()` post-hoc untuk "dedup":** Menyamarkan fan-out. Dengan `.Any()` subquery tak ada fan-out → Distinct = no-op menyesatkan.
- **JOIN ke UserUnits lalu dedup:** Itu pendekatan baris-per-unit yang DITOLAK (D-01); rusak pagination `workerList.Count`.
- **Ubah signature `GetWorkersInSection`:** Tidak perlu — reuse `unitFilter` param existing. Mengubah signature = sentuh 4 caller + interface + fake + tests sia-sia. CONTEXT D-04 & IWorkerDataService comment AssessmentAdminController:266 ("JANGAN ubah GetWorkersInSection signature") konsisten. [VERIFIED: AssessmentAdminController.cs:266]
- **Lupa filter `IsActive` di batch-load dict:** D-03 — unit deactivated (MU-07) tak boleh muncul di kolom kontekstual maupun predikat.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Load semua unit per pekerja | Loop per-user query (N+1) | Batch `_context.UserUnits.Where(userIds.Contains).GroupBy` | Pola CMP-25 existing; 1 query [VERIFIED WorkerDataService.cs:283] |
| Primary-first ordering | Custom sort comparator | `OrderByDescending(x=>x.IsPrimary).ThenBy(x=>x.Unit)` | Idiom 399 (AccountController.cs:159, WorkerController.cs:231) — konsisten lintas surface |
| Pagination count | Hitung manual | `PaginationHelper.Calculate(workerList.Count,...)` | Existing helper, count akurat krn 1-row/worker [VERIFIED CMPController.cs:824] |
| Dedup rollup | `Distinct()` / HashSet | (nothing — by-construction `.Any`) | 1 row/worker inheren; tambahan = noise |

**Key insight:** Seluruh mekanika sudah ada di repo (subquery UserUnits, batch-load dict, primary-first join, pagination). Phase 400 = **rewire 3 predikat + 1 contextual assignment** dengan pola yang sudah teruji di Phase 399. Risiko utama bukan "bagaimana membangun" tapi "memakai bentuk ekspresi C# yang benar" (subquery vs nav) dan "tak menyentuh path yang salah" (analytics).

## Runtime State Inventory

> Bukan rename/refactor string atau migration — ini perubahan query/filter logic murni. Tidak ada runtime state yang ter-cache/ter-stored dengan nilai lama.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — 0 migration; junction `UserUnits` sudah ada & ter-backfill (Phase 399 `AddUserUnitsTable` applied lokal). | None — verified REQUIREMENTS MU-06 "0 migration" + 399 backfill done |
| Live service config | None — tak ada konfigurasi eksternal tersentuh. | None |
| OS-registered state | None. | None |
| Secrets/env vars | None. | None |
| Build artifacts | None — tak ada rename package/namespace. | None |

**Catatan:** Satu-satunya prasyarat data = baris `UserUnits` ter-backfill (semua `Unit` non-null punya 1 primary-row, invariant #3 mirror). Phase 399 sudah apply `AddUserUnitsTable` + backfill di DB lokal (MEMORY: migration `fc015f4d` applied, 6 UserUnits/6 IsPrimary live). Untuk uji multi-unit set-aware butuh fixture pekerja {X,Y} (lihat Validation Architecture + Environment).

## Common Pitfalls

### Pitfall 1: Menulis `u.UserUnits.Any()` (navigation tak ada) — BLOCKER kompilasi
**What goes wrong:** Compile error CS1061 "ApplicationUser does not contain a definition for UserUnits".
**Why it happens:** D-01 CONTEXT menulis pseudo-code `u.UserUnits.Any`; DbContext pasang `.WithMany()` tanpa argumen nav → tak ada `ICollection<UserUnit>` di ApplicationUser.
**How to avoid:** Pakai correlated subquery `_context.UserUnits.Any(uu => uu.UserId == u.Id && ...)`.
**Warning signs:** Build gagal di task pertama. (Mitigasi: ini ditangkap `dotnet build` segera — Develop Workflow step 3.)
[VERIFIED: ApplicationUser.cs:1-73; ApplicationDbContext.cs:343-344]

### Pitfall 2: Consumer ke-4 (AssessmentAdminController:278) terlewat dari verifikasi
**What goes wrong:** ManageAssessmentTab (Kelola Data Section C area) berubah perilaku filter unit tanpa disadari → regresi diam-diam atau hasil verifikasi tak lengkap.
**Why it happens:** CONTEXT menyebut "3 tempat scalar" (`<code_context>` line 76) padahal ada 4 call-site GetWorkersInSection dengan unitFilter.
**How to avoid:** Masukkan `AssessmentAdminController:278` ke daftar consumer terdampak; verifikasi perilaku set-aware-nya benar (atau dokumentasikan kalau memang diinginkan — secara semantik konsisten MU-06).
**Warning signs:** Diff hanya menyentuh 3 dari 4 path; test ManageAssessmentTab tak diperbarui.
[VERIFIED: grep GetWorkersInSection — AssessmentAdminController.cs:278 lewatkan `unit` ke param ke-2]

### Pitfall 3: EF InMemory tak menjamin semantik subquery identik SQL Server
**What goes wrong:** Unit-test pakai EF InMemory hijau, tapi SQL Server menerjemahkan EXISTS berbeda (mis. null-handling, koleksi kosong).
**Why it happens:** InMemory = LINQ-to-Objects, bukan SQL translator. `_context.UserUnits.Any(...)` di InMemory dieksekusi in-memory (selalu jalan); di SQL Server jadi `EXISTS`. Secara logika boolean keduanya setara untuk predikat ini (tak ada agregasi/null-coalesce rumit), jadi risiko rendah — tapi BUKAN bukti SQL-real.
**How to avoid:** Unit-test predikat + dedup + contextual-string di InMemory (cukup untuk logika set-aware boolean). **Defer verifikasi SQL-real (translation EXISTS benar, koleksi kosong → 0 row) ke Phase 404** (sudah punya fixture `UserUnitsBackfillFixture` SQLEXPRESS yang bisa diperluas). Tambahan: verifikasi manual via `dotnet run` + cek DB lokal (Develop Workflow step 3) pada fixture pekerja {X,Y}.
**Warning signs:** Test InMemory hijau tapi belum pernah dijalankan terhadap SQL Server.
[VERIFIED: WorkerDataServiceSearchTests.cs:21 `.UseInMemoryDatabase`; UserUnitsBackfillIntegrationTests.cs:33 SQLEXPRESS fixture existing]

### Pitfall 4: Kolom kontekstual filtered case — `unitFilter` mungkin bukan "exact display"
**What goes wrong:** Saat filtered, D-02 minta kolom = `unitFilter`. Tapi `unitFilter` adalah nilai dropdown (server-validated thd Section di ManageWorkers:171-176; di CMP RecordsTeamPartial tak ada validasi unit-vs-section eksplisit). Bila operator kirim unitFilter yang TAK match pekerja manapun, predikat `.Any` sudah menyaring 0 row → tak ada baris untuk tampilkan nilai salah. Jadi aman by-construction: hanya pekerja yang BENAR anggota unitFilter yang lolos predikat.
**Why it happens:** Kecemasan teoretis bahwa kolom menampilkan unit yang bukan milik pekerja.
**How to avoid:** Tidak ada aksi — predikat `.Any(uu.Unit==unitFilter && uu.IsActive)` menjamin setiap baris yang lolos memang anggota aktif unitFilter, sehingga menampilkan `unitFilter` di kolom selalu benar.
**Warning signs:** —
[VERIFIED: predikat & assignment konsisten]

### Pitfall 5 (Discretion resolusi): OR-fallback scalar redundan
**Analisis:** CONTEXT Discretion menanyakan apakah perlu `u.Unit == unitFilter OR _context.UserUnits.Any(...)`. Backfill 399 `AddUserUnitsTable` membuat 1 primary-row untuk SEMUA pekerja `Unit` non-null, dan write-through `SyncUserUnitsAsync` menjaga invariant #3 (mirror `ApplicationUser.Unit` == primary `UserUnits`). Maka untuk setiap pekerja `Unit` non-null, `_context.UserUnits.Any(uu.Unit == u.Unit && uu.IsActive)` PASTI true → OR-fallback tak menambah baris apa pun. **Rekomendasi: `.Any()` murni** (CONTEXT lean). Tambah fallback HANYA bila verifikasi DB lokal menemukan pekerja `Unit` non-null tanpa baris UserUnits aktif (anomali backfill). Verifikasi ini = 1 query cek di Develop Workflow step 3.
[VERIFIED: WorkerController.cs:82-118 SyncUserUnitsAsync write-through; MEMORY 399 backfill applied + invariant #3]

## Code Examples

### Predikat set-aware (3 lokasi — bentuk identik)
```csharp
// Source: pola _context.UserUnits subquery existing
// WorkerDataService.cs:254-255, WorkerController.cs:202-204, WorkerController.cs:300-301
if (!string.IsNullOrEmpty(unitFilter))
    query = query.Where(u =>
        _context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive));
```

### Contextual Unit assignment (GetWorkersInSection only)
```csharp
// Source: D-02/D-04/D-05 + batch-load WorkerController.cs:224-232
// (lihat Pattern 3 untuk batch-load dict; ini bagian assignment di foreach :340-355)
Unit = !string.IsNullOrEmpty(unitFilter)
    ? unitFilter
    : (unitsByUser.TryGetValue(user.Id, out var uList) && uList.Count > 0
        ? string.Join(", ", uList)
        : (user.Unit ?? "")),
```

### Unit test predikat set-aware (pola WorkerDataServiceSearchTests)
```csharp
// Source: WorkerDataServiceSearchTests.cs:19-58 (MakeService + EF InMemory pattern)
[Fact]
public async Task MultiUnitWorker_AppearsInBothUnitFilters_SetAware()
{
    var svc = MakeService(out var ctx);
    var u = User("u1", "Budi", "A"); u.Unit = "UnitX";   // mirror primary
    ctx.Users.Add(u);
    ctx.UserUnits.AddRange(
        new UserUnit { UserId = "u1", Unit = "UnitX", IsPrimary = true,  IsActive = true },
        new UserUnit { UserId = "u1", Unit = "UnitY", IsPrimary = false, IsActive = true });
    await ctx.SaveChangesAsync();

    Assert.Single(await svc.GetWorkersInSection("A", unitFilter: "UnitX"));  // muncul di X
    Assert.Single(await svc.GetWorkersInSection("A", unitFilter: "UnitY"));  // muncul di Y (set-aware)
    Assert.Single(await svc.GetWorkersInSection("A"));                       // 1 baris tanpa filter (dedup)
}

[Fact]
public async Task InactiveUnit_ExcludedFromFilter_D03()
{
    var svc = MakeService(out var ctx);
    var u = User("u1", "Budi", "A"); u.Unit = "UnitX";
    ctx.Users.Add(u);
    ctx.UserUnits.AddRange(
        new UserUnit { UserId = "u1", Unit = "UnitX", IsPrimary = true,  IsActive = true },
        new UserUnit { UserId = "u1", Unit = "UnitY", IsPrimary = false, IsActive = false }); // deactivated
    await ctx.SaveChangesAsync();
    Assert.Empty(await svc.GetWorkersInSection("A", unitFilter: "UnitY"));   // tak muncul (inactive)
}

[Fact]
public async Task UnfilteredColumn_AllActiveUnits_PrimaryFirst_D02()
{
    var svc = MakeService(out var ctx);
    var u = User("u1", "Budi", "A"); u.Unit = "UnitY";   // primary = Y
    ctx.Users.Add(u);
    ctx.UserUnits.AddRange(
        new UserUnit { UserId = "u1", Unit = "UnitX", IsPrimary = false, IsActive = true },
        new UserUnit { UserId = "u1", Unit = "UnitY", IsPrimary = true,  IsActive = true });
    await ctx.SaveChangesAsync();
    var r = await svc.GetWorkersInSection("A");           // tanpa filter
    Assert.Equal("UnitY, UnitX", r[0].Unit);             // primary-first comma-join
}

[Fact]
public async Task FilteredColumn_ShowsUnitFilter_D02()
{
    var svc = MakeService(out var ctx);
    var u = User("u1", "Budi", "A"); u.Unit = "UnitX";
    ctx.Users.Add(u);
    ctx.UserUnits.AddRange(
        new UserUnit { UserId = "u1", Unit = "UnitX", IsPrimary = true, IsActive = true },
        new UserUnit { UserId = "u1", Unit = "UnitY", IsPrimary = false, IsActive = true });
    await ctx.SaveChangesAsync();
    var r = await svc.GetWorkersInSection("A", unitFilter: "UnitY");
    Assert.Equal("UnitY", r[0].Unit);                    // filtered → matched unit
}
```
[VERIFIED: pola MakeService + ctx.Users.Add + ctx.UserUnits.Add valid — UserUnit ada di DbSet (ApplicationDbContext.cs:35), InMemory mendukung]

## State of the Art

Tidak relevan — brownfield, pola sudah mapan di Phase 399. Tak ada "old vs new approach" eksternal. Pola set-aware via junction adalah konsekuensi langsung desain v32.3 (junction `UserUnits` Phase 399).

**Deprecated/outdated dalam konteks phase ini:**
- Scalar `u.Unit == unitFilter` sebagai satu-satunya filter unit → digantikan set-aware (itulah deliverable MU-06). Tetapi `ApplicationUser.Unit` scalar (mirror primary) TETAP dipakai untuk analytics/cert (D1=b) — bukan deprecated, hanya bukan dasar membership-filter lagi.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | EF Core 8 menerjemahkan `_context.UserUnits.Any(uu => uu.UserId==u.Id && ...)` di dalam `Users.Where()` menjadi SQL `WHERE EXISTS(...)` yang benar | Pattern 1, Pitfall 3 | LOW — idiom EF Core standar (correlated subquery via DbSet); existing codebase pakai `_context.UserUnits.Where` ekstensif. Mitigasi: verifikasi `dotnet run` + DB lokal (Phase 400) + SQL-real (Phase 404). Bila gagal translate → fallback materialize userIds anggota unit lewat 2-step query (load `UserUnits.Where(Unit==filter).Select(UserId)` → `Users.Where(userIds.Contains(Id))`). |
| A2 | EF InMemory boolean-result untuk subquery setara SQL Server untuk predikat ini (no null/empty edge divergence) | Pitfall 3 | LOW — predikat boolean sederhana tanpa agregasi. Defer bukti ke Phase 404 SQL-real. |

**Catatan:** Semua claim lain VERIFIED langsung dari kode live. Hanya 2 ASSUMED, keduanya LOW-risk + punya mitigasi/fallback eksplisit + jalur verifikasi (Develop Workflow step 3 + Phase 404).

## Open Questions

1. **AssessmentAdminController:278 (consumer ke-4) — apakah set-aware diinginkan di sana?**
   - What we know: Ia teruskan `unit` ke GetWorkersInSection → otomatis set-aware setelah perubahan. Secara semantik konsisten MU-06 (pekerja multi-unit muncul di filter tiap unit di Kelola Data juga).
   - What's unclear: CONTEXT hanya menyebut "3 tempat". Apakah perubahan ke-4 ini in-scope eksplisit atau efek samping yang perlu di-flag ke planner/user.
   - Recommendation: **Treat as in-scope benign** (planner masukkan ke verification scope; tak butuh kode tambahan — sudah otomatis). Dokumentasikan di PLAN sebagai consumer ke-4. Bila user mau exclude, butuh keputusan (tapi exclude justru inkonsisten — tak disarankan).

2. **Verifikasi anomali backfill (pekerja Unit non-null tanpa UserUnits aktif) untuk putuskan OR-fallback.**
   - What we know: Backfill 399 + invariant mirror → seharusnya tak ada anomali; lean `.Any()` murni.
   - What's unclear: Apakah DB lokal saat ini 100% bersih (semua Unit non-null punya primary-row aktif).
   - Recommendation: 1 query cek saat Develop Workflow step 3: `SELECT u.Id FROM Users u WHERE u.Unit IS NOT NULL AND NOT EXISTS(SELECT 1 FROM UserUnits uu WHERE uu.UserId=u.Id AND uu.IsActive=1)`. Bila 0 → `.Any()` murni final. Bila >0 → re-evaluate (kemungkinan backfill gap, eskalasi sebelum tambah fallback).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK (`dotnet build`/`run`) | Verifikasi lokal (CLAUDE.md) | ✓ (project aktif) | net8.0 | — |
| SQL Server lokal (SQLEXPRESS) | DB lokal verifikasi + Phase 404 SQL-real | ✓ (junction live, MEMORY 399) | — | — |
| EF Core InMemory 8.0.0 | Unit test Phase 400 | ✓ | 8.0.0 [VERIFIED csproj] | — |
| `UserUnits` ter-backfill | Predikat set-aware bermakna | ✓ (399 `AddUserUnitsTable` applied + backfill) | — | — |
| Fixture pekerja {X,Y} multi-unit | Uji set-aware DB lokal | ✗ (perlu seed temporary) | — | Seed temporary per CLAUDE.md Seed Workflow (snapshot→insert {X,Y}→test→restore) |
| Playwright | UI verifikasi (1 cell teks) | (existing infra repo) | — | UI change minimal (1 cell); Playwright opsional — UAT browser milik Phase 404. Phase 400 cukup unit + `dotnet run` manual cek. |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:**
- Fixture pekerja {X,Y}: gunakan **seed temporary local-only** (CLAUDE.md Seed Workflow — snapshot DB → insert 1 pekerja anggota 2 unit di 1 Bagian → verifikasi filter X & Y + rollup → restore DB → tandai journal cleaned). Klasifikasi: `temporary + local-only` (untuk reproduce/verify, BUKAN prod-required).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + EF Core InMemory 8.0.0 (+ SqlServer 8.0.0 untuk integration) |
| Config file | none — auto-discovery xUnit (HcPortal.Tests.csproj) |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` |
| Full suite command | `dotnet test HcPortal.Tests` (saat ini 366/366 hijau per MEMORY 399) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MU-06 | Pekerja {X,Y} muncul di filter unit-X | unit (InMemory) | `dotnet test --filter "Name~MultiUnitWorker_AppearsInBothUnitFilters"` | ❌ Wave 0 (tambah ke WorkerDataServiceSearchTests.cs) |
| MU-06 | Pekerja {X,Y} muncul di filter unit-Y (set-aware) | unit (InMemory) | (sama test di atas, assert kedua) | ❌ Wave 0 |
| MU-06 | Tanpa filter = 1 baris/pekerja (dedup by-construction) | unit (InMemory) | (sama test, assert `Single` tanpa filter) | ❌ Wave 0 |
| MU-06 | Unit inactive (MU-07) TIDAK muncul (D-03) | unit (InMemory) | `dotnet test --filter "Name~InactiveUnit_ExcludedFromFilter"` | ❌ Wave 0 |
| MU-06 | Kolom kontekstual unfiltered = all-active primary-first join (D-02) | unit (InMemory) | `dotnet test --filter "Name~UnfilteredColumn_AllActiveUnits_PrimaryFirst"` | ❌ Wave 0 |
| MU-06 | Kolom kontekstual filtered = unitFilter (D-02) | unit (InMemory) | `dotnet test --filter "Name~FilteredColumn_ShowsUnitFilter"` | ❌ Wave 0 |
| MU-06 | Pekerja 0-unit-aktif → fallback `user.Unit` (D-05) | unit (InMemory) | `dotnet test --filter "Name~ZeroUnit_Fallback"` | ❌ Wave 0 |
| MU-06 | No-drift D1=b: GetWorkersInSection tanpa unitFilter tak berubah (Team View `:543`) | unit (InMemory) — regresi existing | `dotnet test --filter "Name~Scope_Null_NoFilter_BackwardCompat"` (existing) | ✅ (WorkerDataServiceSearchTests.cs:86) |
| MU-06 | SQL-real: EXISTS translate benar + pagination count akurat di SQL Server | integration (SQLEXPRESS) | **DEFER → Phase 404** (extend `UserUnitsBackfillFixture`) | ❌ (owned by 404) |
| MU-06 | UAT browser: kolom tampil "X" saat filter, "X, Y" tanpa filter | manual/Playwright | **DEFER → Phase 404** (UAT live) | ❌ (owned by 404) |

### Sampling Rate
- **Per task commit:** `dotnet build` (0 error — tangkap Pitfall 1) + `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"`
- **Per wave merge:** `dotnet test HcPortal.Tests` (full suite, pastikan ≥366 + test baru hijau)
- **Phase gate:** Full suite hijau + `dotnet run` localhost:5277 cek manual fixture {X,Y} (filter X & Y muncul, rollup dedup) + cek DB lokal sebelum `/gsd-verify-work`. SQL-real + UAT browser = Phase 404.

### Wave 0 Gaps
- [ ] Tambah ~7 test ke `HcPortal.Tests/WorkerDataServiceSearchTests.cs` (reuse `MakeService`/`User` helper existing; tambah seed `ctx.UserUnits.AddRange(...)`) — covers MU-06 unit-level (set-aware both-units, dedup, IsActive D-03, contextual filtered/unfiltered D-02, fallback D-05).
- [ ] (Opsional, recommended) update `FakeWorkerDataService.cs` hanya bila ada test controller-level baru — saat ini tak perlu (fake return empty, dipakai GradingService test saja). [VERIFIED FakeWorkerDataService.cs:24]
- [ ] Framework install: tidak perlu — InMemory + xUnit sudah ada.

*Catatan Nyquist: observable facts per Success Criterion —*
- *SC#1 (set-aware): `GetWorkersInSection("A", unitFilter:"X").Single()` && `...unitFilter:"Y").Single()` untuk pekerja {X,Y} → kedua hijau = listing set-aware terbukti.*
- *SC#2 (dedup): `GetWorkersInSection("A").Count` untuk pekerja {X,Y} == 1 (bukan 2) → denominator/completion% tak ganda.*
- *SC#3 (no-drift D1=b): existing `Scope_Null_NoFilter_BackwardCompat` tetap hijau + diff menunjukkan path analytics (`CMPController:2581`,`:2589`) & Team View call (`:543`) tak berubah → 0 drift by-construction.*
- *SC#4 (build/run/DB): `dotnet build` 0 error + fixture {X,Y} tampil di filter X & Y + rollup Bagian dedup terverifikasi DB lokal.*

## Project Constraints (from CLAUDE.md)
- **Bahasa:** Selalu respons + copy user-facing dalam Bahasa Indonesia (UI-SPEC sudah enforce; org-label via `@OrgLabels.GetLabel(0/1)`, jangan hardcode "Unit"/"Bagian").
- **Develop Workflow:** Verifikasi lokal WAJIB sebelum commit — `dotnet build` + `dotnet run` (localhost:5277) + cek DB lokal (+ Playwright bila ada UI). Jangan edit kode/DB di Dev/Prod. Jangan push tanpa verifikasi lokal. Promosi ke Dev = tugas IT (notify dengan commit hash + flag migration). **Phase 400 migration = FALSE** → notify IT migration=FALSE saat push.
- **Seed Data Workflow:** Untuk fixture {X,Y} → klasifikasi `temporary + local-only`, snapshot DB lokal sebelum insert, catat `docs/SEED_JOURNAL.md`, restore + tandai `cleaned` setelah test. Jangan biarkan seed temporary nempel lintas sesi.

## Security Domain

> `security_enforcement` default enabled. Phase 400 = read/filter-path, no new endpoint/input/auth surface. Tinjauan ringkas:

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ada perubahan auth |
| V3 Session Management | no | — |
| V4 Access Control | yes (pasif) | RBAC existing dipertahankan — `RecordsTeamPartial:810` `roleLevel>=5 Forbid`, L4 section-lock `:813`; ExportWorkers `[Authorize(Roles="Admin,HC")]:270`. Predikat set-aware TIDAK melonggarkan scope: filter unit dalam Section yang sudah ter-otorisasi. [VERIFIED] |
| V5 Input Validation | yes (pasif) | `unitFilter` dipakai sebagai parameter EF (parameterized → no SQLi); ManageWorkers validasi unit-vs-section `:171-176`. Subquery `.Any(uu.Unit==unitFilter)` = parameter, bukan string-concat. [VERIFIED] |
| V6 Cryptography | no | — |

### Known Threat Patterns for ASP.NET Core 8 + EF Core
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection via unitFilter | Tampering | EF parameterized query (`.Where(uu.Unit==unitFilter)`) — by-default safe [VERIFIED idiom] |
| Broken access control (lihat unit di luar scope) | Elevation | Section-scope + RBAC existing tak diubah; set-aware hanya memperluas dari primary→semua-unit DALAM Section yang sama, tak lintas-Bagian (junction = within-Bagian by design v32.3) |
| Information disclosure (kolom unit bocor unit lain) | Info Disclosure | Kolom kontekstual hanya menampilkan unit milik pekerja itu sendiri (dari UserUnits-nya); tak ada cross-user leak |

**Verdict:** No new threat surface. Predikat memperluas membership-match dalam batas Section/Bagian + RBAC yang sudah ter-otorisasi. Tak perlu kontrol keamanan baru.

## Sources

### Primary (HIGH confidence)
- `Services/WorkerDataService.cs:244-428` — GetWorkersInSection penuh (predikat :254-255, assign :347, batch-load pola CMP-25 :283/295)
- `Services/IWorkerDataService.cs:14` — signature (tak perlu berubah)
- `Controllers/WorkerController.cs:165-336` — ManageWorkers (:202-204), ExportWorkers (:300-301), UserUnitsDict pola (:224-232), SyncUserUnitsAsync (:82-118)
- `Controllers/CMPController.cs:543,710,771,819-836,2579-2611` — 3 consumer + RecordsTeamPartial pagination + analytics path
- `Controllers/AssessmentAdminController.cs:266-281` — consumer ke-4 (temuan kritis #2)
- `Models/ApplicationUser.cs:1-73` + `Models/UserUnit.cs` — konfirmasi TAK ADA nav UserUnits (temuan kritis #1)
- `Data/ApplicationDbContext.cs:35,340-359` — DbSet UserUnits + `.WithMany()` no-arg + index
- `Controllers/AccountController.cs:155-163`, `Controllers/HomeController.cs:61-65` — pola subquery `_context.UserUnits` + primary-first ordering existing
- `Views/CMP/_RecordsTeamBody.cshtml:1-43` + `Views/CMP/RecordsTeam.cshtml:403-419` — cell + count JS (server-side filter)
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs:1-194` — pola unit-test (MakeService InMemory)
- `HcPortal.Tests/FakeWorkerDataService.cs` + `HcPortal.Tests/UserUnitsBackfillIntegrationTests.cs:25-84` — fake + SQL-real fixture existing (Phase 404 extend)
- `HcPortal.csproj` + `HcPortal.Tests.csproj` — versi EF Core 8.0.0 / net8.0 / xUnit 2.9.3 / InMemory 8.0.0
- `400-CONTEXT.md` (D-01..D-06) + `400-UI-SPEC.md` (cell contract) + CLAUDE.md (workflow constraints)

### Secondary (MEDIUM confidence)
- EF Core correlated-subquery → EXISTS translation: idiom standar + dikuatkan oleh ekstensifnya pemakaian `_context.UserUnits` di codebase. [CITED: learn.microsoft.com/ef-core/querying complex/subquery]

### Tertiary (LOW confidence)
- A1/A2 (Assumptions Log) — terjemahan SQL persis & paritas InMemory↔SqlServer: LOW, di-flag untuk verifikasi `dotnet run`/DB lokal (Phase 400) + SQL-real (Phase 404).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — 0 dependency baru, versi dibaca dari .csproj
- Architecture (predikat + batch-load + contextual): HIGH — semua pola sudah ada di repo (399), touchpoint diverifikasi baris-demi-baris
- Pitfalls: HIGH — Pitfall 1 (nav tak ada) & #2 (consumer ke-4) dibuktikan langsung dari kode, bukan dugaan
- SQL translation paritas: MEDIUM — idiom standar tapi belum dieksekusi terhadap SQL Server sesi ini (mitigasi + fallback didokumentasikan)

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stabil — brownfield, kode kunci tak akan banyak berubah sebelum Phase 400 dieksekusi; satu-satunya invalidator = perubahan signature GetWorkersInSection atau model UserUnit oleh phase lain Wave-1, yang tidak diharapkan)
