# Phase 370: Hapus Window 7-Hari (Tampilan Default Tanpa Batas) - Research

**Researched:** 2026-06-11
**Domain:** ASP.NET Core MVC controller query-layer cleanup (EF Core 8, xUnit) — retire helper + 2 call sites + 1 test file
**Confidence:** HIGH

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Helper `ApplySevenDayWindow` **hapus total** — definisi (`AssessmentAdminController.cs:2820-2826`) + 2 call site (`:123`, `:2873`) + var `sevenDaysAgo` (`:116`, `:2870`). Komentar 260611-m9r di kedua method dibersihkan/diperbarui (termasuk komentar 90-review lama "7-day window is intentional" di AssessmentMonitoring — sudah tidak berlaku).
- **D-02:** File `HcPortal.Tests/AssessmentSearchWindowTests.cs` (3 [Fact] uji helper) **dihapus utuh** — helper hilang, test tak bisa compile. Tidak di-repurpose.
- **D-03:** Badge counter (`ViewBag.OpenCount/UpcomingCount/ClosedCount`) **biarkan all-time** — tanpa window, ClosedCount mencakup semua sesi historis. Zero kode ekstra.
- **D-04:** Pengganti 3 test lama = **grep-guard + UAT** — grep zero sisa `sevenDaysAgo`/`ApplySevenDayWindow` di kedua method (dan codebase) + full suite hijau + UAT @5277 (sesi >7 hari tampil di default view, search tetap jalan, pagination Tab Assessment jalan). TIDAK ada test unit/integration baru.
- **D-05:** `AssessmentMonitoring` query ditambah `.AsNoTracking()` (`:2872`) — 1 baris, read-only method, selaras pola Phase 311 di Tab Assessment.
- **D-06:** UAT pakai **data legacy existing** DB lokal (12 InProgress + 9 Open legacy + Post Test OJT >7 hari) — zero seed, zero snapshot/restore (read-only verification).

### Claude's Discretion
- Wording komentar pengganti di 2 method (jejak keputusan Phase 370 boleh 1 baris singkat atau tanpa komentar — yang penting komentar stale 260611-m9r/90-review hilang).
- Urutan langkah edit vs hapus test (kompilasi tetap hijau di tiap commit).

### Deferred Ideas (OUT OF SCOPE)
- **Pagination AssessmentMonitoring** — Monitoring render semua group tanpa paging; tanpa window halaman memanjang seiring waktu. Tambah paging = kapabilitas baru + ubah view (roadmap 370: view tak berubah). Kandidat fase perf/UX nanti. Mitigasi sementara: default filter Aktif menyembunyikan Closed.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| URG-02 | Window 7-hari dihapus dari tampilan default `ManageAssessmentTab_Assessment` + `AssessmentMonitoring` — semua sesi tampil tanpa batas umur (filter status default "Aktif" + hide-Closed CIL-02 tetap; search behavior quick 260611-m9r tidak regresi). | Grep sweep [VERIFIED] mengonfirmasi hanya 5 referensi production-code di 1 file + 1 test file; CIL-02/MAP-15/CIL-01 semua downstream (operasi di list ter-grup, bukan query window) sehingga utuh setelah window dilepas; data legacy DB lokal [VERIFIED] mencukupi UAT. |
</phase_requirements>

## Summary

Phase ini adalah **pembersihan query-layer yang sangat terbatas (surgical removal)**, bukan eksplorasi fitur. Tugasnya: hapus satu helper static `ApplySevenDayWindow` + dua var `sevenDaysAgo` + dua call site di `AssessmentAdminController.cs`, tambah satu `.AsNoTracking()` di `AssessmentMonitoring`, hapus utuh file test `AssessmentSearchWindowTests.cs`, dan bersihkan komentar usang. Semua keputusan sudah terkunci di CONTEXT.md — research ini memverifikasi bahwa keputusan-keputusan itu aman dieksekusi dan memetakan jalur verifikasi.

Hasil investigasi paling penting: **grep sweep [VERIFIED]** membuktikan bahwa `ApplySevenDayWindow`/`sevenDaysAgo` HANYA muncul di 5 baris production code (semua di `AssessmentAdminController.cs`) + 1 file test. **Tidak ada** referensi di view (`.cshtml`), JavaScript, e2e specs, atau service lain. Filter status (CIL-02), badge counter (CIL-01), dan MAP-15 status="All" beroperasi pada list yang **sudah di-grup** (downstream dari window query), sehingga semua tetap utuh tanpa perubahan setelah window dilepas. Konfirmasi dari DB lokal: 58 sesi total, 55 di antaranya >7 hari (saat ini tersembunyi), termasuk 12 InProgress + 8 Open legacy + 22 OJT — **data UAT sudah tersedia, zero seed** (D-06 terbukti viable).

Risiko teknis mendekati nol. Satu-satunya pitfall nyata adalah **urutan commit** (hapus test SEBELUM/BERSAMAAN dengan hapus helper agar tiap commit kompilasi hijau) dan **AD lokal** (`Authentication__UseActiveDirectory=false dotnet run` wajib untuk login admin @5277, pitfall berulang dari Phase 355).

**Primary recommendation:** Eksekusi sebagai 1 plan atomik — edit 2 method (hapus window + AsNoTracking) + hapus helper + hapus file test dalam satu commit logis (build+suite hijau di commit itu), lalu grep-guard + UAT @5277 pakai data legacy existing. Tidak ada library baru, tidak ada migration, tidak ada perubahan view.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Filter window umur sesi (dihapus) | API / Backend (controller query) | — | Logika `WHERE (ExamWindowCloseDate ?? Schedule) >= cutoff` di IQueryable EF Core; murni server-side, tidak ada JS/view yang menduplikasi |
| Filter status default "Aktif"/hide-Closed (CIL-02) | API / Backend (post-group in-memory) | Frontend (dropdown selStatus) | Beroperasi pada `grouped` list SETELAH window+grouping; independen dari window |
| Badge counter (CIL-01) | API / Backend | Frontend (badge render) | Dihitung dari `grouped` SEBELUM filter status; jadi all-time setelah window hilang (D-03) |
| Pagination Tab Assessment | API / Backend (`PaginationHelper.Calculate`) | Frontend (HTMX hx-include) | In-memory post-grouping; dataset membesar tapi mekanisme tak berubah |
| Search (260611-m9r — TIDAK boleh regresi) | API / Backend (`.Where(Contains)`) | — | Setelah window hilang, search beroperasi atas full table; behavior "search menjangkau semua sesi" justru menjadi default permanen — search-skip-window jadi no-op natural |

## Standard Stack

Tidak ada dependency baru. Phase ini murni edit kode existing.

### Core (existing, dipakai apa adanya)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller `AssessmentAdminController` | [VERIFIED: HcPortal.Tests.csproj TargetFramework net8.0] Stack proyek |
| EF Core | 8.0.0 | `IQueryable<AssessmentSession>` + `.AsNoTracking()` | [VERIFIED: HcPortal.Tests.csproj PackageReference] `.AsNoTracking()` read-only pattern Phase 311 |
| xUnit | 2.9.3 | Test framework (file yang dihapus) | [VERIFIED: HcPortal.Tests.csproj] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | [VERIFIED: HcPortal.Tests.csproj] |

**Installation:** Tidak ada. Zero `npm install` / `dotnet add package`.

## Architecture Patterns

### System Architecture Diagram

```
HTTP GET /Admin/ManageAssessmentTab_Assessment (HTMX partial, [Authorize Admin,HC])
   │
   ▼
_context.AssessmentSessions.AsNoTracking().AsQueryable()
   │
   ▼
[DIHAPUS] ApplySevenDayWindow(query, search, sevenDaysAgo)  ← window 7-hari
   │       (D-01: hapus var sevenDaysAgo :116 + call :123 + helper :2820-2826)
   ▼
.Where(search Contains)  ──┐
.Where(category)           │  (TETAP — tak disentuh)
   ▼                       │
.OrderByDescending(Schedule).Select(proj).ToListAsync()
   ▼
GroupBy (PrePost by LinkedGroupId | Standard by Title,Category,Date)
   ▼
grouped = prePostGrouped.Concat(standardGrouped).OrderByDescending(Schedule)
   ▼
CIL-01 badge: OpenCount/UpcomingCount/ClosedCount  (all-time, D-03 — TETAP)
   ▼
CIL-02 hide-Closed default (status & search kosong)  (TETAP)
   ▼
PaginationHelper.Calculate → ViewBag.ManagementData → PartialView

HTTP GET /Admin/AssessmentMonitoring (full view, [Authorize Admin,HC])
   │
   ▼
_context.AssessmentSessions.AsQueryable()  →  [D-05] .AsNoTracking() ditambah :2872
   │
   ▼
[DIHAPUS] ApplySevenDayWindow(query, search, sevenDaysAgo)  ← window 7-hari
   │       (D-01: hapus var sevenDaysAgo :2870 + call :2873 + komentar 90-review :2865-2868)
   ▼
.Where(search Contains Title|Category) → .Where(category) → ToListAsync()
   ▼
GroupBy → CIL-01 badge → CIL-02 hide-Closed + MAP-15 status="All"  (semua TETAP)
   ▼
full View (TANPA pagination — out of scope, deferred)
```

### Pattern 1: AsNoTracking pada read-only IQueryable (D-05)
**What:** Tambah `.AsNoTracking()` di chain start `AssessmentMonitoring` query.
**When to use:** Method read-only tanpa `SaveChanges` (Monitoring murni baca). Tab Assessment sudah punya ini sejak Phase 311.
**Example:**
```csharp
// Source: AssessmentAdminController.cs:120-122 (pola existing Tab Assessment, Phase 311)
var managementQuery = _context.AssessmentSessions
    .AsNoTracking()
    .AsQueryable();
// AssessmentMonitoring:2872 saat ini HANYA .AsQueryable() — tambah .AsNoTracking() (D-05)
```
[VERIFIED: AssessmentAdminController.cs:120-122 vs :2872]

### Pattern 2: Penghapusan window — chain sebelum & sesudah
**What:** Kondisi awal (260611-m9r) vs target.
**Example:**
```csharp
// SEBELUM (Tab Assessment :115-123):
var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);                       // :116 — HAPUS
var managementQuery = _context.AssessmentSessions.AsNoTracking().AsQueryable();
managementQuery = ApplySevenDayWindow(managementQuery, search, sevenDaysAgo); // :123 — HAPUS

// SESUDAH:
var managementQuery = _context.AssessmentSessions.AsNoTracking().AsQueryable();
// (langsung ke .Where(search...) yang sudah ada di :125)
```
[VERIFIED: AssessmentAdminController.cs:115-136]

### Anti-Patterns to Avoid
- **Menyentuh CIL-02 / CIL-01 / MAP-15:** JANGAN. Semua di luar scope (CONTEXT.md eksplisit: "TIDAK disentuh fase ini"). Mereka downstream dari window dan otomatis utuh.
- **Menambah pagination ke Monitoring:** OUT OF SCOPE (deferred). View tidak berubah.
- **Repurpose test lama jadi test "no-filter":** D-04 eksplisit menolak — controller butuh DbContext, perilaku "tidak ada filter" tidak bermakna diuji unit.
- **Memperpanjang window (mis. 30/90 hari) atau bikin configurable:** Keputusan user verbatim "7 hari jadi tanpa batas" — hapus PENUH, bukan diperpanjang.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Verifikasi window hilang | Test unit baru "no-window" | Grep-guard + full suite + UAT (D-04) | Controller butuh DbContext; "absence of filter" tak punya unit assertion bermakna |
| Pagination Monitoring | Paging baru di Monitoring | (deferred) | Ubah view = keluar scope; default filter Aktif sudah mitigasi |

**Key insight:** Ini phase **pengurangan kode**, bukan penambahan. Solusi terbaik = menghapus seminimal mungkin sambil menjaga kompilasi & suite hijau di tiap commit. Tidak ada yang perlu "dibangun".

## Common Pitfalls

### Pitfall 1: Urutan commit memecah kompilasi
**What goes wrong:** Hapus helper `ApplySevenDayWindow` dulu tanpa hapus `AssessmentSearchWindowTests.cs` → test project gagal compile (test memanggil `AssessmentAdminController.ApplySevenDayWindow`).
**Why it happens:** Test di project terpisah (`HcPortal.Tests`) me-reference method static public dari `HcPortal`.
**How to avoid:** Hapus file test BERSAMAAN/SEBELUM hapus helper dalam satu commit logis. Discretion D mengizinkan urutan, tapi tiap commit HARUS build+test hijau.
**Warning signs:** `dotnet build` error `CS0117: 'AssessmentAdminController' does not contain a definition for 'ApplySevenDayWindow'` di test project.

### Pitfall 2: AD lokal blok login admin @5277
**What goes wrong:** `dotnet run` polos → `appsettings.json` `UseActiveDirectory=true` → login admin@pertamina.com gagal (LDAP pertamina tak terjangkau dari lokal).
**Why it happens:** `appsettings.json` handoff IT pakai AD=true [VERIFIED: appsettings.json:13]; `appsettings.Development.json` tidak override flag ini.
**How to avoid:** UAT WAJIB jalankan `Authentication__UseActiveDirectory=false dotnet run` lalu buka `http://localhost:5277`. Login admin@pertamina.com / 123456 (kredensial UAT lokal).
**Warning signs:** Redirect balik ke `/Account/Login` setelah submit; pitfall identik tercatat Phase 355.

### Pitfall 3: Salah baca grep-guard karena .planning/ docs penuh referensi historis
**What goes wrong:** Grep `sevenDaysAgo` mengembalikan puluhan hit → keliru disangka sisa kode.
**Why it happens:** [VERIFIED: grep sweep] `.planning/` (RESEARCH/PLAN/SUMMARY phase 49/90/311/323/338 + quick 260611-m9r) penuh menyebut `sevenDaysAgo` sebagai dokumentasi historis. Itu BUKAN compiled code.
**How to avoid:** Grep-guard HARUS scope ke kode (`Controllers/`, `HcPortal.Tests/`, `Views/`, `wwwroot/`, `tests/`) ATAU filter glob `*.cs`/`*.cshtml`/`*.ts`. Target: **zero** hit di file kode setelah fase selesai.
**Warning signs:** Grep tanpa scope file ratusan baris — abaikan semua di `.planning/`.

### Pitfall 4: Lupa komentar stale
**What goes wrong:** Helper terhapus tapi komentar "7-day window is intentional for monitoring view" (90-review) atau "Quick fix (260611-m9r)" tertinggal → membingungkan pembaca berikutnya.
**Why it happens:** Komentar ada di blok terpisah dari kode yang dihapus (`:2865-2868` di Monitoring, `:118-119` di Tab Assessment).
**How to avoid:** D-01 eksplisit minta komentar dibersihkan. Hapus/ganti komentar `:118-119` (Tab Assessment) + `:2865-2868` (Monitoring) + `:2816-2819` (header helper).
**Warning signs:** Grep `7-day` / `260611-m9r` / `90-review` masih hit di `Controllers/`.

## Code Examples

### Target akhir Tab Assessment (sekitar :112-125)
```csharp
// Source: AssessmentAdminController.cs:112-125 (target setelah edit)
public async Task<IActionResult> ManageAssessmentTab_Assessment(string? search, int page = 1, int pageSize = 20,
    string? category = null, string? statusFilter = null)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    // (var sevenDaysAgo DIHAPUS)

    // Phase 370: tampilan default tanpa batas umur — window 7-hari dihapus (URG-02).
    var managementQuery = _context.AssessmentSessions
        .AsNoTracking()
        .AsQueryable();
    // (ApplySevenDayWindow call DIHAPUS)

    if (!string.IsNullOrEmpty(search)) { /* ... TETAP ... */ }
```

### Target akhir Monitoring (sekitar :2860-2873)
```csharp
// Source: AssessmentAdminController.cs:2860-2873 (target setelah edit)
public async Task<IActionResult> AssessmentMonitoring(string? search, string? status, string? category)
{
    // (var sevenDaysAgo + komentar 90-review/260611-m9r DIHAPUS)
    // Phase 370: tampilan default tanpa batas umur — window 7-hari dihapus (URG-02).
    var query = _context.AssessmentSessions
        .AsNoTracking()    // D-05: read-only method, selaras Tab Assessment Phase 311
        .AsQueryable();
    // (ApplySevenDayWindow call DIHAPUS)

    if (!string.IsNullOrEmpty(search)) { /* ... TETAP (MAP-23) ... */ }
```

## Runtime State Inventory

Phase ini adalah **refactor/removal** (hapus helper + filter), maka inventory ini wajib.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — tidak ada string/kolom DB bernama "sevenDaysAgo"/"ApplySevenDayWindow". Window murni runtime compute (`DateTime.UtcNow.AddDays(-7)`), bukan data tersimpan. [VERIFIED: grep] | None |
| Live service config | None — tidak ada config UI/service (n8n/Datadog/dll) yang referensi window. Murni in-process. [VERIFIED: grep] | None |
| OS-registered state | None — tidak ada task scheduler/cron/pm2 terkait. [VERIFIED: grep] | None |
| Secrets/env vars | None — window bukan dari env/secret. (Catatan terpisah: `Authentication__UseActiveDirectory` env var dibutuhkan untuk UAT login, bukan terkait window.) | None |
| Build artifacts | `HcPortal.Tests/AssessmentSearchWindowTests.cs` (3 [Fact]) jadi stale setelah helper dihapus — file DIHAPUS utuh (D-02). Tidak ada egg-info/binary lain. [VERIFIED: grep + Glob] | Hapus file test (D-02) |

**Canonical check:** Setelah semua file diperbarui, runtime tidak menyimpan/cache string lama — window adalah computed value per-request, hilang otomatis saat kode dihapus. Tidak ada migrasi data.

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Window 7-hari inline `.Where(>= sevenDaysAgo)` di 2 method | Helper bersama `ApplySevenDayWindow` (skip saat search) | 2026-06-11 quick 260611-m9r | Helper jadi single source of truth |
| Helper `ApplySevenDayWindow` (skip-on-search) | **Tanpa window sepenuhnya** (default tanpa batas) | Phase 370 (ini) | Default view tampilkan semua sesi; search-skip jadi no-op natural |

**Deprecated/outdated:**
- Komentar 90-review "7-day window is intentional for monitoring view" (`AssessmentMonitoring`): SUDAH TIDAK BERLAKU — hapus.
- Komentar 260611-m9r di kedua call site: usang — hapus.
- Asumsi UAT lama (Phase 338 SUMMARY: "UAT WAJIB seed within window — historical data tidak muncul"): TERBALIK setelah fase ini — sesi historis justru menjadi yang utama tampil.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | Semua klaim diverifikasi via grep/DB query/file read. Tidak ada `[ASSUMED]`. |

**Table kosong:** Seluruh temuan diverifikasi langsung (grep sweep, query DB lokal, baca file). Tidak ada yang butuh konfirmasi user — keputusan sudah locked di CONTEXT.md.

## Open Questions

1. **Apakah `Stopwatch sw` di Tab Assessment masih dipakai setelah var `sevenDaysAgo` dihapus?**
   - What we know: `var sw = Stopwatch.StartNew()` di :115 (Phase 311 telemetry, D-09), terpisah dari `sevenDaysAgo` di :116.
   - What's unclear: Tidak ada — `sw` independen, tetap dipakai (telemetry Phase 311). Hanya `sevenDaysAgo` yang dihapus.
   - Recommendation: Hapus HANYA baris `sevenDaysAgo`, jangan sentuh `sw`.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | `dotnet build`/`test`/`run` | ✓ (asumsi proyek aktif) | net8.0 | — |
| SQL Server (SQLEXPRESS) `HcPortalDB_Dev` | UAT @5277 + data legacy | ✓ | localhost\SQLEXPRESS | — |
| Data legacy >7 hari | UAT default-view (D-06) | ✓ | 58 sesi (55 >7hari: 12 InProgress, 8 Open, 22 OJT) | zero — data cukup [VERIFIED: query DB] |
| Browser/Playwright @5277 | UAT live | ✓ (orchestrator) | — | code-verify bila browser absen |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — data UAT legacy sudah tersedia di DB lokal (zero seed, D-06 terkonfirmasi).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (+ Microsoft.NET.Test.Sdk 17.13.0) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0, EF Core InMemory 8.0.0 + SqlServer 8.0.0) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~ManageAssessment"` (subset Tab Assessment) |
| Full suite command | `dotnet test` (dari root solution) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| URG-02 | Window 7-hari hilang dari default view | manual-only (UAT) | UAT @5277 default view — sesi >7 hari tampil | N/A (D-04: no new unit test; controller butuh DbContext) |
| URG-02 | Search tidak regresi (menjangkau semua sesi) | manual-only (UAT) | UAT @5277 search judul lama → muncul | N/A (perilaku jadi default natural) |
| URG-02 | Test helper lama tidak break suite | regression (delete) | `dotnet test` (suite 229→226) | ❌ Wave 0: DELETE `AssessmentSearchWindowTests.cs` |
| URG-02 | Pagination Tab Assessment jalan | regression (existing) | `tests/e2e/manage-assessment-filter.spec.ts` (FILTER-03, conditional) | ✅ existing |
| URG-02 | AsNoTracking + build hijau | smoke | `dotnet build` | ✅ existing |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~ManageAssessment"` (cek tak ada test ManageAssessment yang break).
- **Per wave merge:** `dotnet test` (full suite — HARUS 226 hijau setelah hapus 3 [Fact]).
- **Phase gate:** Full suite hijau (226) + grep-guard zero sisa di kode + UAT @5277 sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] DELETE `HcPortal.Tests/AssessmentSearchWindowTests.cs` (D-02) — bukan menambah test, tapi menghapus; setelah hapus suite turun dari 229 ke **226** (3 [Fact] hilang). [VERIFIED: grep `[Fact]` count = 3 di file ini; total 190 atribut [Fact]/[Theory] di 30 file, suite expand ke 229 karena beberapa [Theory] punya multiple data rows].
- [ ] Grep-guard skrip (verifikasi, bukan kode produksi): grep `ApplySevenDayWindow|sevenDaysAgo` di `Controllers/ Views/ wwwroot/ tests/ HcPortal.Tests/` → **zero** hit expected.

*Tidak ada framework install — xUnit sudah ada. Tidak ada test baru — D-04 grep-guard + UAT menggantikan 3 test lama.*

## Security Domain

> `security_enforcement` tidak di-set eksplisit `false` di config.json — disertakan ringkas.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | yes | Kedua method `[Authorize(Roles = "Admin, HC")]` [VERIFIED: :110, :2859] — TIDAK diubah. Menghapus window tidak memperluas otorisasi (Admin/HC sudah berhak lihat SEMUA sesi). |
| V5 Input Validation | yes (existing) | Search via EF Core `Contains` → parameterized SQL otomatis; tidak disentuh. |
| V6 Cryptography | no | — |

### Known Threat Patterns for ASP.NET Core MVC + EF Core
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Information disclosure (window dilepas → sesi lama tampil) | Information Disclosure | **Accept** — endpoint admin-only `[Authorize Admin,HC]`; Admin/HC memang berhak lihat semua sesi. Window murni perf/UX, BUKAN kontrol akses (selaras threat T-m9r-01 quick task). |
| DoS full-table scan (tanpa window load semua row) | Denial of Service | **Accept** skala saat ini — 58 row lokal; in-memory grouping + pagination Tab Assessment tetap menyempitkan; endpoint admin-only low-volume; `.AsNoTracking()` (D-05) kurangi overhead. Monitoring tanpa paging = deferred risk (catat untuk fase perf bila row membengkak). |
| SQL injection | Tampering | **Mitigate (sudah)** — EF Core parameterize otomatis; jalur search tak diubah. |

## Sources

### Primary (HIGH confidence)
- `Controllers/AssessmentAdminController.cs` (baca langsung :100-230, :2808-2826, :2855-2945, :3020-3064) — target edit, CIL-02/MAP-15/CIL-01 logic
- `HcPortal.Tests/AssessmentSearchWindowTests.cs` (baca utuh) — file yang dihapus, 3 [Fact]
- `HcPortal.Tests/HcPortal.Tests.csproj` — framework xUnit 2.9.3 / net8.0
- `appsettings.json` + `appsettings.Development.json` — AD flag + connection string SQLEXPRESS HcPortalDB_Dev
- Grep sweep `ApplySevenDayWindow|sevenDaysAgo|AssessmentSearchWindowTests` (seluruh repo) — membuktikan hanya 5 baris kode produksi + 1 file test; zero di view/JS/e2e
- Query DB lokal `HcPortalDB_Dev` (System.Data.SqlClient) — inventory: Total=58, Older7d=55, InProgress=12, Open=9, InProgOld=12, OpenOld=8, OJT=22
- `tests/e2e/manage-assessment-filter.spec.ts` (baca utuh) — tidak ada asumsi "sesi lama tersembunyi"; conditional-skip aman
- `.planning/quick/260611-m9r-.../260611-m9r-SUMMARY.md` — perilaku search yang tidak boleh regresi
- `.planning/phases/370-.../370-CONTEXT.md` — keputusan locked D-01..D-06

### Secondary (MEDIUM confidence)
- `.planning/STATE.md` — phase 366/367 belum start (no CONTEXT.md), DB lokal SQLEXPRESS, AD lokal pitfall
- `git status` / `git worktree list` — working tree clean untuk controller+test; tidak ada sesi paralel menyentuh file ini sekarang

### Tertiary (LOW confidence)
- None — semua klaim diverifikasi.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — verified csproj; zero dependency baru
- Architecture: HIGH — baca langsung kedua method + downstream logic; grep membuktikan blast radius minimal
- Pitfalls: HIGH — urutan commit & AD lokal berdasarkan pola Phase 355 + struktur test project; grep noise diverifikasi
- Data UAT: HIGH — query DB lokal langsung mengonfirmasi 55 sesi >7 hari tersedia

**Research date:** 2026-06-11
**Valid until:** 2026-07-11 (stabil — fase removal terbatas; satu-satunya invalidator = sesi paralel 366/367 mulai menyentuh `AssessmentAdminController.cs` sebelum 370 commit)
