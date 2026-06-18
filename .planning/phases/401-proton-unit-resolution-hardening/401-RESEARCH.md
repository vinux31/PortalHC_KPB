# Phase 401: PROTON Unit-Resolution Hardening - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core 8 MVC + EF Core 8 (net8.0) — PROTON unit-resolution hardening; 0 migration; pure resolver/validator/filter/read-path changes in 5 disjoint cluster files
**Confidence:** HIGH (all code sites verified line-by-line against current source; UserUnit junction contract verified; test infra verified)

## Summary

Phase 401 menghapus fallback `User.Unit` ambigu dari resolusi unit PROTON sehingga unit selalu diturunkan dari `CoachCoacheeMapping.AssignmentUnit` eksplisit, sekarang pekerja boleh anggota >1 Unit dalam 1 Bagian (junction `UserUnits` dari Phase 399). Ini adalah hardening phase **0 migration**: tidak ada schema baru, `ProtonTrackAssignment` tetap 7 kolom tanpa Unit, dan invariant single-active (`IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`) + E8 bypass tidak disentuh. Semua perubahan terlokalisir di 5 file cluster yang disjoint (Wave-1 paralel dengan 400 & 403).

Semua decision sudah LOCKED di `401-CONTEXT.md` (D-01..D-05). Research ini meng-konfirmasi **setiap site kode** masih persis seperti yang dideskripsikan CONTEXT/spec (zero drift terdeteksi), meng-enumerasi 5 resolver + 6 read-path + 4 filter-axis + validasi ∈UserUnits + 2 no-clobber + reactivation guard dengan `file:line` aktual, dan memetakan pola implementasi konkret untuk tiap site. Temuan paling penting: (1) `ApplicationUser` TIDAK punya navigation collection `UserUnits` — validasi `∈ coachee.UserUnits` WAJIB query `_context.UserUnits.Where(uu => uu.UserId == coacheeId && uu.IsActive)` langsung (bukan nav property); (2) `AssessmentAdminController.cs:1411-1414` adalah BLOCKING gate yang menerbitkan `AssessmentSession`+`NomorSertifikat` — drop fallback di sini = inti D-02; (3) ProtonBypassService MENULIS `AssignmentUnit = req.TargetUnit` di 2 site (`:449`, `:465`), jadi validasi `TargetUnit ∈ worker.UserUnits` melindungi junction-write nyata.

**Primary recommendation:** Implement sebagai 5 task disjoint per-file (cluster), tiap task ekstrak resolver/validator ke pola helper testable-seam ala 399 (`public static` di controller, EF-InMemory unit-test) untuk PSU-01/03/05 logic, plus SQL-real integration test minimal untuk filter-axis (deep multi-unit SQL-real test = Phase 404). Drop fallback `?? User.Unit` jadi `AssignmentUnit`-only di 5 resolver; perketat skip ke "AssignmentUnit kosong saja"; channel audit hybrid by-path (D-03); preserve `AssignmentUnit` di Cleanup/Import-reactivate (D-04/PSU-04); validasi `∈ UserUnits` di Assign/Edit/Import/bypass; D-01 indikator UI on-demand reuse pola `CleanupReport`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Resolusi unit PROTON (drop fallback) | API/Backend (Controller + Service) | Database (query `CoachCoacheeMappings`/`UserUnits`) | Server-authoritative; unit di-resolve runtime dari mapping aktif, bukan disimpan |
| Validasi `AssignmentUnit ∈ coachee.UserUnits` | API/Backend (write-path Assign/Edit/Import/bypass) | Database (`UserUnits` junction read) | Junction-write guard; jaga Invariant #4 di server (jangan trust client) |
| Filter-axis swap (read-path listing) | API/Backend (Controller query) | Database (`CoachCoacheeMappings` join) | Filter axis = data query concern; coachee muncul di unit AssignmentUnit-nya |
| Skip + audit-warn (6 read-path) | API/Backend (Controller/Service) | Logging infra (ILogger) + AuditLogs (DB) | Hybrid: read-path=ILogger (volume), gate-block=AuditLog persisted (langka) |
| Indikator UI orphan-unit mapping (D-01) | API/Backend (compute on-demand di GET action) | Frontend Server (Razor view render alert) | Data dihitung server-side; view hanya render badge/alert Bootstrap 5 |
| Gate-eligibility BLOCK penerbitan session/cert (D-02) | API/Backend (`CreateAssessment` gate) | Database (`AssessmentSession`+`NomorSertifikat` write) | Penerbitan cert = backend authoritative; gate tahan session bila unit tak ter-resolve |

## Standard Stack

### Core (sudah terpasang — JANGAN tambah dependency baru)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore (SqlServer) | 8.0.0 | ORM, query `CoachCoacheeMappings`/`UserUnits`/`ProtonTrackAssignments` | Stack project; `[VERIFIED: HcPortal.csproj]` |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | `ApplicationUser`, `UserManager` | `[VERIFIED: HcPortal.csproj]` |
| ClosedXML | (terpasang) | Import/export Excel mapping | Sudah dipakai import coach-coachee `[VERIFIED: codebase]` |
| ILogger<T> | (built-in) | `_logger.LogWarning` read-path skip (D-03) | Channel murah, app-log `[VERIFIED: CoachMappingController.cs:512]` |
| AuditLogService | (internal) | `_auditLog.LogAsync` gate-block persisted (D-03) | `LogAsync(actorUserId, actorName, actionType, description, int? targetId, string? targetType)` `[VERIFIED: Services/AuditLogService.cs:21-42]` |
| xUnit + EF-InMemory | (terpasang) | Unit-test resolver/validator (testable seam) | Pola 399 `[VERIFIED: HcPortal.Tests/UserUnitsWriteThroughTests.cs]` |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `_context.UserUnits.Where(...)` direct query | Navigation property `coachee.UserUnits` | **TIDAK BISA** — `ApplicationUser` tak punya nav collection; DbContext config pakai `.WithMany()` tanpa inverse `[VERIFIED: ApplicationDbContext.cs:343-346]`. Direct query satu-satunya jalan. |
| Validasi `∈ UserUnits` di controller | Validasi di `ProtonBypassService` (service-level) | Untuk bypass `TargetUnit`: D-04/CONTEXT menunjuk `ProtonDataController.cs:1638` (controller), tapi service tetap menulis di `:449`/`:465` — pertimbangkan defensive dual-check ala E8 pattern |

**Installation:** Tidak ada — semua sudah terpasang. **0 package baru, 0 migration.**

**Version verification:** net8.0 + EF Core 8.0.0 `[VERIFIED: HcPortal.csproj — TargetFramework net8.0, EntityFrameworkCore.SqlServer 8.0.0]`. Global `dotnet ef` CLI v10 (catatan 399) TIDAK relevan untuk 401 (no migration).

## Architecture Patterns

### System Architecture Diagram (alur resolusi unit PROTON pasca-401)

```
                    PROTON unit resolution (semua surface)
                                  │
              ┌───────────────────┴───────────────────┐
              ▼                                         ▼
   [WRITE-PATH: Assign/Edit/Import/Bypass]      [READ-PATH: resolver/filter/listing]
              │                                         │
   validasi AssignmentUnit ∈ coachee.UserUnits   resolve = AssignmentUnit (active mapping)
   (∈ org-tree + ∈ UserUnits IsActive)                  │  [DROP ?? User.Unit fallback]
              │                                         ▼
   ┌──────────┴──────────┐                 ┌────────────┴────────────┐
   ▼                     ▼                 ▼                          ▼
 valid → tulis      invalid → reject   AssignmentUnit kosong?    AssignmentUnit ada?
 AssignmentUnit     (per-row Import /        │                        │
 (no clobber)       message Assign)    ┌─────┴─────┐                  ▼
                                       ▼           ▼            gunakan unit, resolve deliverable
                          READ-PATH skip     GATE-ELIGIBILITY    per-unit (filter Unit==resolved)
                          (exclude dari list) (BLOCK penerbitan
                                │             session/cert)
                          ILogger.LogWarning      │
                          (D-03 app-log)    AuditLog persisted +
                                            ILogger.LogWarning (D-03)
                                                  │
                                            indikator UI on-demand (D-01)
                                            di halaman CoachCoacheeMapping
                                            (query mapping aktif AssignmentUnit
                                             kosong / ∉ UserUnits aktif)
```

### Component Responsibilities (file → site → REQ)

| File | Site(s) | Capability | REQ |
|------|---------|-----------|-----|
| `Controllers/CoachMappingController.cs` | `:1409-1419` GetEligibleCoachees (gate) | resolver drop-fallback + skip+gate-block | PSU-01/05 |
| `Controllers/CoachMappingController.cs` | `:1456-1485` AutoCreateProgressForAssignment | resolver drop-fallback + skip+warn | PSU-01/05 |
| `Controllers/CoachMappingController.cs` | `:468-474` Assign validasi | validasi ∈ UserUnits per-coachee | PSU-03 |
| `Controllers/CoachMappingController.cs` | `:719-749` Edit validasi + unit-change | validasi ∈ UserUnits (jaga rebuild Phase 129) | PSU-03 |
| `Controllers/CoachMappingController.cs` | `:325-373` Import (new + reactivate) | validasi ∈ UserUnits + no-clobber reactivate | PSU-03/04/07a |
| `Controllers/CoachMappingController.cs` | `:880-907` CleanupCoachCoacheeMappingOrg | no-clobber `AssignmentUnit`→primary | PSU-04 |
| `Controllers/CoachMappingController.cs` | `:1017-1097` Reactivate | validasi ∈ UserUnits + preserve (window AF-4 utuh) | PSU-07 |
| `Controllers/CoachMappingController.cs` | `:42-165` CoachCoacheeMapping GET | compute indikator orphan-unit (D-01) | PSU-05 |
| `Controllers/AssessmentAdminController.cs` | `:1411-1414` CreateAssessment gate | resolver drop-fallback + **BLOCK** penerbitan (D-02) | PSU-01/05 |
| `Controllers/CDPController.cs` | `:491,:1586,:1596,:4248` | filter-axis swap `u.Unit`→AssignmentUnit | PSU-02 |
| `Controllers/CDPController.cs` | `:515-526`, `:1708-1719` | defensive-filter ×2 resolver drop-fallback + skip | PSU-01/05 |
| `Controllers/ProtonDataController.cs` | `:1517` BypassList | filter-axis swap `x.u.Unit`→AssignmentUnit | PSU-02 |
| `Controllers/ProtonDataController.cs` | `:1638` BypassSave | validasi `TargetUnit ∈ worker.UserUnits` + org | PSU-03 |
| `Services/ProtonBypassService.cs` | `:104-118`, `:226-234` | E8 single-active (TIDAK disentuh — confirm utuh) | invariant #2 |
| `Services/ProtonBypassService.cs` | `:449`, `:465` | junction-write `AssignmentUnit = TargetUnit` (defensive validasi optional) | PSU-03 |

### Pattern 1: Drop fallback `?? User.Unit` di resolver (PSU-01)

**What:** Hapus cabang fallback ke `User.Unit`; resolusi hanya dari `AssignmentUnit` mapping aktif.
**When to use:** Ke-5 resolver site.

Current shape (GetEligibleCoachees, `CoachMappingController.cs:1409-1419` — DROP yang ditandai):
```csharp
// Source: VERIFIED Controllers/CoachMappingController.cs:1409-1419 (current)
var assignmentUnit = await _context.CoachCoacheeMappings
    .Where(m => m.CoacheeId == coacheeId && m.IsActive)
    .Select(m => m.AssignmentUnit)
    .FirstOrDefaultAsync();
var resolvedUnit = assignmentUnit;
if (string.IsNullOrWhiteSpace(resolvedUnit))           // <-- DROP cabang ini (PSU-01)
    resolvedUnit = await _context.Users                // <-- DROP
        .Where(u => u.Id == coacheeId)                 // <-- DROP
        .Select(u => u.Unit)                           // <-- DROP
        .FirstOrDefaultAsync();                        // <-- DROP
if (string.IsNullOrWhiteSpace(resolvedUnit)) continue; // <-- perketat: skip bila AssignmentUnit kosong (D-02)
```

Target shape (PSU-01 + PSU-05 read-path skip + gate-block):
```csharp
// Target — resolver = AssignmentUnit only, skip + audit bila kosong
var resolvedUnit = await _context.CoachCoacheeMappings
    .Where(m => m.CoacheeId == coacheeId && m.IsActive)
    .Select(m => m.AssignmentUnit)
    .FirstOrDefaultAsync();
if (string.IsNullOrWhiteSpace(resolvedUnit))
{
    // GATE-ELIGIBILITY (GetEligibleCoachees + AssessmentAdmin gate) = BLOCK + AuditLog persisted (D-03)
    await _auditLog.LogAsync(actor.Id, actor.FullName, "ProtonUnitUnresolved",
        $"Coachee {coacheeId} di-skip dari eligibility: AssignmentUnit kosong (tak boleh resolve dari primary).",
        targetType: "CoachCoacheeMapping");
    _logger.LogWarning("Eligibility skip: coachee {CoacheeId} AssignmentUnit kosong.", coacheeId);
    continue;
}
```

**Catatan untuk read-path (AutoCreateProgress, CDP defensive ×2):** sama persis TAPI channel = `_logger.LogWarning` SAJA (D-03 — JANGAN `_auditLog.LogAsync`, volume tinggi). AutoCreateProgress sudah punya `warnings.Add(...)` list (`:1477`) — perketat pesannya jadi "AssignmentUnit kosong" dan biarkan tetap ILogger/warning-list.

### Pattern 2: Validasi `AssignmentUnit ∈ coachee.UserUnits` (PSU-03)

**What:** Query junction langsung (TIDAK ada nav property).
```csharp
// Source: VERIFIED — UserUnit query shape ala WorkerController.SyncUserUnitsAsync:86
// _context.UserUnits.Where(uu => uu.UserId == X) — ApplicationUser TAK punya nav collection (ApplicationDbContext.cs:343 WithMany() no-inverse)
var coacheeActiveUnits = await _context.UserUnits
    .Where(uu => uu.UserId == coacheeId && uu.IsActive)
    .Select(uu => uu.Unit)
    .ToListAsync();
bool unitValid = coacheeActiveUnits
    .Any(u => string.Equals(u.Trim(), assignmentUnit.Trim(), StringComparison.OrdinalIgnoreCase));
if (!unitValid) { /* reject — per-row Import / message Assign (Claude's Discretion CONTEXT) */ }
```
**When to use:** Assign (`:468-474`, tambah lapis setelah cek org-tree `validUnits`), Edit (`:719-749`, saat `unitChanged`), Import-new + Import-reactivate (`:355-372`), Bypass TargetUnit (`ProtonDataController.cs:1638`).
**Testable seam (rekomendasi):** ekstrak ke `public static bool/Task<bool> ValidateAssignmentUnitInUserUnits(ApplicationDbContext ctx, string coacheeId, string assignmentUnit)` ala pola `WorkerController.ValidateUnitsInSection` (`:63`) → unit-testable dengan EF-InMemory.

### Pattern 3: No-clobber Cleanup (PSU-04) — UserUnits-aware/gated

**What:** `CleanupCoachCoacheeMappingOrg` (`:880-907`) saat ini meng-overwrite `m.AssignmentUnit = userUnit` (= coachee primary `User.Unit`) bila invalid (`:899-900`). Target: JANGAN reset bila `AssignmentUnit` existing masih sah `∈ UserUnits` aktif.
```csharp
// Source: VERIFIED Controllers/CoachMappingController.cs:887-906 (current clobber)
if (isValid) continue;                                  // sudah valid vs org-tree → skip
// Try fix from coachee user record:
if (userValid) {
    m.AssignmentSection = userSec;
    m.AssignmentUnit = userUnit;   // <-- CLOBBER ke primary (PSU-04 data-loss vector)
    autoFixed++; continue;
}
```
Target (gated): SEBELUM clobber ke `userUnit` (primary), cek apakah `m.AssignmentUnit` existing masih `∈ coachee.UserUnits` aktif; bila ya → PRESERVE (jangan reset), hanya perbaiki `AssignmentSection` bila perlu. Catatan: "valid vs org-tree" (`:884-885`) ≠ "∈ UserUnits coachee" — multi-unit coachee bisa punya `AssignmentUnit` unit non-primary yang valid org-tree tapi gagal kondisi lain. Tambah cek `∈ UserUnits` ke definisi `isValid`.

### Pattern 4: Reactivation preserve + validasi (PSU-07) — window AF-4 UTUH

**What:** `CoachCoacheeMappingReactivate` (`:1017-1097`). D-05: JANGAN ubah korelasi `DeactivatedAt ±5s` (`:1052-1076`, AF-4 comment `:1043-1051`). TAMBAH sebelum reaktivasi: validasi `mapping.AssignmentUnit ∈ coachee.UserUnits` aktif (tolak bila unit dilepas). `AssignmentUnit` sudah preserved by default di reactivate kontroler (tidak di-reset) — confirm tidak ada clobber. Import-reactivate (`:355-356`) BERBEDA: ADA clobber `inactiveMapping.AssignmentUnit = coacheeUser.Unit.Trim()` → HAPUS, preserve existing + validasi ∈ UserUnits (D-04).

### Pattern 5: Filter-axis swap (PSU-02)

**What:** Sites yang filter `_context.Users.Where(u => u.Unit == unit)` (`CDP:491,1586,1596,4248`; `ProtonData:1517`) → coachee harus muncul di unit AssignmentUnit-nya, bukan `User.Unit` primary.
**Pendekatan:** Filter sekarang berbasis scalar `u.Unit`. Swap ke axis AssignmentUnit perlu korelasi coachee → active-mapping AssignmentUnit. Contoh untuk `:491` (in-memory post-filter, sudah punya `coacheeUsers` + `filteredCoacheeIds`):
```csharp
// Source: VERIFIED CDPController.cs:491 (current) — coacheeUsers.Where(u => u.Unit == unit)
// Target: resolve AssignmentUnit per coachee dari active mapping, lalu filter by resolved unit.
var unitByCoachee = await _context.CoachCoacheeMappings
    .Where(m => m.IsActive && coacheeUsers.Select(u => u.Id).Contains(m.CoacheeId))
    .GroupBy(m => m.CoacheeId)
    .Select(g => new { CoacheeId = g.Key, Unit = g.Select(x => x.AssignmentUnit).FirstOrDefault() })
    .ToDictionaryAsync(x => x.CoacheeId, x => x.Unit);
coacheeUsers = coacheeUsers
    .Where(u => string.Equals(unitByCoachee.GetValueOrDefault(u.Id)?.Trim(), unit.Trim(),
                              StringComparison.OrdinalIgnoreCase))
    .ToList();
```
Untuk `BypassList` (`ProtonData:1517`) yang join ke `ProtonTrackAssignments`: tambah join/subquery ke `CoachCoacheeMappings` aktif untuk filter by AssignmentUnit. **Pertahankan semantik filter, hanya ganti sumbu** (CONTEXT Claude's Discretion). Single-active invariant menjamin tepat 1 active mapping/coachee → `FirstOrDefault` aman (deterministik).

### Pattern 6: Indikator UI on-demand (D-01) — reuse CleanupReport idiom

**What:** Di `CoachCoacheeMapping` GET (`:42-165`, sebelum `return View()` ~`:163`), hitung mapping aktif yang `AssignmentUnit` kosong ATAU `∉ coachee.UserUnits` aktif; surface via `ViewBag.OrphanUnitMappings` (count + daftar). Render alert Bootstrap 5 ala blok `CleanupReport` (`Views/Admin/CoachCoacheeMapping.cshtml:74-95`).
```csharp
// Compute (controller, GET action)
var activeMappings = await _context.CoachCoacheeMappings.Where(m => m.IsActive)
    .Select(m => new { m.Id, m.CoacheeId, m.AssignmentUnit }).ToListAsync();
var coacheeIds = activeMappings.Select(m => m.CoacheeId).Distinct().ToList();
var unitsByCoachee = (await _context.UserUnits
    .Where(uu => coacheeIds.Contains(uu.UserId) && uu.IsActive)
    .Select(uu => new { uu.UserId, uu.Unit }).ToListAsync())
    .GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.Select(x => x.Unit.Trim()).ToList());
var orphans = activeMappings.Where(m =>
    string.IsNullOrWhiteSpace(m.AssignmentUnit) ||
    !unitsByCoachee.GetValueOrDefault(m.CoacheeId, new()).Any(u =>
        string.Equals(u, m.AssignmentUnit!.Trim(), StringComparison.OrdinalIgnoreCase))).ToList();
ViewBag.OrphanUnitMappings = orphans; // view render alert/badge (Bootstrap 5, idiom CleanupReport)
```
Render (view, ikut idiom `:82-94`): alert warning dengan count + (opsional) daftar coachee. **Bentuk badge vs alert daftar = Claude's Discretion (CONTEXT D-01).**

### Anti-Patterns to Avoid
- **Pakai nav property `coachee.UserUnits`:** TIDAK ADA — compile error. Selalu `_context.UserUnits.Where(uu => uu.UserId == ...)`.
- **AuditLog persisted di read-path skip:** banjir tabel `AuditLogs` tiap page-load (D-03). Read-path = ILogger SAJA.
- **Ubah window korelasi `DeactivatedAt ±5s`:** langgar AF-4 (D-05). Hanya TAMBAH validasi unit, jangan re-architect.
- **Reset `AssignmentUnit` ke primary saat masih sah `∈ UserUnits`:** data-loss multi-unit (PSU-04). Preserve unit non-primary yang sah.
- **Tambah kolom Unit ke `ProtonTrackAssignment`:** = migration, out-of-scope (spec §8, D-05). Korelasi DeactivatedAt tetap.
- **Sentuh E8 single-active index/check:** invariant #2 dipertahankan. 401 hanya validasi TargetUnit, bukan count.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Query unit anggota coachee | Custom join Users×OrganizationUnit | `_context.UserUnits.Where(uu => uu.UserId == X && uu.IsActive)` | Junction 399 satu-satunya source; nav property tak ada |
| Validasi unit ∈ org-tree Bagian | Re-implement hierarchy walk | `GetSectionUnitsDictAsync()` / `GetUnitsForSectionAsync(section)` | Primitif siap pakai `[VERIFIED: ApplicationDbContext.cs:109-136]` |
| Audit-log persisted | `_context.AuditLogs.Add(...)` manual | `_auditLog.LogAsync(actor, name, action, detail, targetId, targetType)` | Signature standar, SaveChanges internal `[VERIFIED: AuditLogService.cs:21]` |
| Single-active assignment enforcement | Custom count guard di 401 | Index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` + E8 existing | Sudah ada, JANGAN duplikat `[VERIFIED: ApplicationDbContext.cs:333-336]` |
| Testable resolver/validator | Inline logic dalam action besar | `public static` helper di controller (pola 399) | EF-InMemory unit-test tanpa `InternalsVisibleTo` `[VERIFIED: WorkerController.cs:51-163]` |

**Key insight:** Semua primitif (junction read, org-tree validation, audit, single-active) sudah ada dari Phase 399 + pre-existing. 401 = wiring resolver/validator ke primitif ini + drop fallback, BUKAN bangun infrastruktur baru.

## Runtime State Inventory

> Phase 401 adalah refactor resolusi/validasi runtime — bukan rename/migrasi data. Tidak ada perubahan schema. Namun karena drop-fallback mengubah perilaku terhadap **data mapping produksi existing**, inventory tetap relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `CoachCoacheeMapping.AssignmentUnit` produksi existing — kolom SUDAH terisi untuk mayoritas mapping (Assign selalu set `:579`, Import selalu set `:372`, Bypass selalu set `:449/:465`). Empty-AssignmentUnit = legacy/edge rare. | **TIDAK ADA migrasi data.** Kuantifikasi via SQL query (lihat Risks #10): `SELECT COUNT(*) FROM CoachCoacheeMappings WHERE IsActive=1 AND (AssignmentUnit IS NULL OR LTRIM(RTRIM(AssignmentUnit))='')`. Coachee dgn empty akan di-skip (bukan default primary). |
| Live service config | None — tidak ada konfigurasi eksternal (n8n/Datadog/Task Scheduler) yang menyimpan unit-resolution. Resolusi 100% runtime in-code. | None |
| OS-registered state | None — tidak ada OS-level state. | None |
| Secrets/env vars | None — tidak ada secret/env terkait unit-resolution. | None |
| Build artifacts | None — 0 migration, 0 package baru, tidak ada egg-info/binary terdampak. `dotnet build` re-compile normal. | None — verified by csproj unchanged |

**Catatan kritis:** `UserUnits` junction sudah di-backfill Phase 399 (1 primary-row/pekerja `Unit` non-null). Maka untuk mapping dengan `AssignmentUnit = primary unit`, validasi `∈ UserUnits` PASTI lolos (Invariant #3: primary ∈ UserUnits selalu). Risiko skip hanya untuk mapping yang `AssignmentUnit` kosong (rare) atau menunjuk unit yang sudah dilepas dari `UserUnits` (edge).

## Common Pitfalls

### Pitfall 1: Mengira `coachee.UserUnits` adalah navigation property
**What goes wrong:** Compile error `'ApplicationUser' does not contain a definition for 'UserUnits'`.
**Why it happens:** DbContext config pakai `.WithMany()` tanpa inverse navigation (`ApplicationDbContext.cs:343-346`); `ApplicationUser.cs` hanya punya `TrainingRecords` collection.
**How to avoid:** SELALU `_context.UserUnits.Where(uu => uu.UserId == coacheeId && uu.IsActive)`.
**Warning signs:** Plan menulis `user.UserUnits` atau `.Include(u => u.UserUnits)`.

### Pitfall 2: Filter-axis swap meng-degradasi query ke client-eval N+1
**What goes wrong:** Swap `u.Unit == unit` ke AssignmentUnit naif bisa memaksa per-coachee subquery (N+1) atau client-side evaluation.
**Why it happens:** AssignmentUnit ada di tabel lain (`CoachCoacheeMappings`), butuh join/dictionary batch.
**How to avoid:** Batch-load `unitByCoachee` dictionary sekali (lihat Pattern 5), filter in-memory; ATAU join di query. Mirror pola batch existing (`CDP:515-526` sudah batch-load `mappingUnits129`).
**Warning signs:** `.Where(u => _context.CoachCoacheeMappings.Any(...))` per-row dalam loop.

### Pitfall 3: Memperketat skip terlalu ke "dua-duanya kosong" alih-alih "AssignmentUnit kosong saja"
**What goes wrong:** AutoCreateProgress (`:1475`) + GetEligibleCoachees (`:1419`) saat ini skip bila `resolvedUnit` (post-fallback) kosong — yaitu AssignmentUnit DAN User.Unit dua-duanya kosong. Bila hanya drop fallback tanpa perketat, coachee dgn AssignmentUnit kosong tapi User.Unit terisi akan TETAP lolos (resolve dari primary via sisa kode) ATAU sebaliknya tetap skip benar — tergantung urutan edit.
**Why it happens:** Logika skip lama bergantung pada `resolvedUnit` post-fallback.
**How to avoid:** Setelah drop fallback, `resolvedUnit == AssignmentUnit`. Skip-condition `IsNullOrWhiteSpace(resolvedUnit)` otomatis jadi "AssignmentUnit kosong saja" — INI BENAR (D-02). Pastikan tidak ada residual baca `User.Unit`.
**Warning signs:** Masih ada `.Select(u => u.Unit)` di resolver setelah edit.

### Pitfall 4: Memecah rebuild Phase 129 di Edit path saat menambah validasi
**What goes wrong:** Edit (`:744-790`) punya logic rebuild progress saat `unitChanged`. Menambah validasi `∈ UserUnits` yang return early SEBELUM tx bisa skip rebuild, atau di dalam tx bisa setengah-commit.
**Why it happens:** Validasi harus terjadi sebelum mutasi `mapping.AssignmentUnit` (`:747`).
**How to avoid:** Letakkan validasi `∈ UserUnits` setelah cek org-tree (`:722-726`) dan SEBELUM `BeginTransactionAsync` (`:737`) — return JSON error bila gagal, sebelum tx dibuka. Rebuild flow tak tersentuh.
**Warning signs:** Validasi di dalam `try` setelah `tx` dibuka.

### Pitfall 5: EF-InMemory test lolos padahal filtered-unique/multi-unit gagal di SQL
**What goes wrong:** EF-InMemory tidak enforce filtered-unique index (`IX_UserUnits_UserId_PrimaryUnique`, `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`).
**Why it happens:** InMemory provider abaikan filtered index (catatan 399 Pitfall 3).
**How to avoid:** Resolver/validator logic (PSU-01/03/05) = EF-InMemory OK (pure logic). Single-active assertion + multi-unit SQL-real = `[Trait("Category","Integration")]` fixture `HcPortalDB_Test_<guid>` (deep test = Phase 404 QA, 401 cukup logic test + 1-2 integration smoke).
**Warning signs:** Test multi-unit single-active TANPA `[Trait Integration]`.

## Code Examples

### AuditLogService.LogAsync (gate-block, D-03)
```csharp
// Source: VERIFIED Services/AuditLogService.cs:21-42
// Tersedia di semua controller via AdminBaseController (_auditLog protected, AssessmentAdminController.cs:46 base ctor)
await _auditLog.LogAsync(
    actorUserId: actor.Id,
    actorName: actor.FullName,
    actionType: "ProtonUnitUnresolved",
    description: $"Coachee {uid} di-skip dari penerbitan session/cert: AssignmentUnit kosong.",
    targetType: "CoachCoacheeMapping"); // targetId opsional (mapping id bila tersedia)
```

### Read junction UserUnits aktif (validasi/indikator)
```csharp
// Source: VERIFIED query shape — WorkerController.SyncUserUnitsAsync:86 (_context.UserUnits.Where(uu => uu.UserId == ...))
var activeUnits = await _context.UserUnits
    .Where(uu => uu.UserId == coacheeId && uu.IsActive)
    .Select(uu => uu.Unit)
    .ToListAsync();
```

### Org-tree validation primitif (bypass TargetUnit + Assign)
```csharp
// Source: VERIFIED ApplicationDbContext.cs:109-119, dipakai CoachMappingController.cs:471-474
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
bool inOrgTree = sectionUnitsDict.TryGetValue(section.Trim(), out var validUnits)
                 && validUnits.Contains(unit.Trim());
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `AssignmentUnit ?? User.Unit` resolver fallback | `AssignmentUnit`-only, skip+audit bila kosong | Phase 401 (this) | Multi-unit aman; tak ada resolve ambigu dari primary |
| Filter coachee by scalar `User.Unit` | Filter by `AssignmentUnit` (active mapping) | Phase 401 | Coachee di unit non-primary tampil di unit benar |
| Cleanup/Import reset `AssignmentUnit`→primary | Preserve unit sah `∈ UserUnits` | Phase 401 | Tutup data-loss vector multi-unit (spec §10) |
| Scalar `ApplicationUser.Unit` 1 unit/pekerja | Junction `UserUnits` (>1 unit, 1 primary mirror) | Phase 399 (done) | Foundation 401 |

**Deprecated/outdated:**
- Resolusi unit dari `User.Unit` primary di konteks PROTON: ambigu pasca-multi-unit; diganti AssignmentUnit eksplisit. `User.Unit` tetap sbg mirror primary (read scalar lain jalan terus).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Empty-`AssignmentUnit` pada mapping aktif produksi = rare/legacy (karena semua write-path mengisi-nya) | Risks/Runtime State | LOW — bila ternyata banyak empty, banyak coachee di-skip mendadak. **Mitigasi: WAJIB jalankan SQL count di DB lokal/Dev sebelum execute (lihat Risk #10).** ASUMSI ini perlu konfirmasi data nyata. |
| A2 | Filter-axis swap aman pakai `FirstOrDefault` AssignmentUnit karena single-active menjamin ≤1 mapping aktif/coachee | Pattern 5 | LOW — invariant #2 di-enforce DB index; aman selama index utuh |
| A3 | Bentuk indikator D-01 (badge vs alert daftar) + lokasi presisi = Claude's Discretion, idiom CleanupReport cukup | Pattern 6 | LOW — eksplisit di CONTEXT D-01 sbg discretion |

**Catatan:** Semua DECISION (D-01..D-05) sudah LOCKED di CONTEXT — bukan assumption. A1 adalah satu-satunya yang butuh konfirmasi data nyata sebelum execute.

## Open Questions (RESOLVED)

1. **Validasi bypass `TargetUnit ∈ worker.UserUnits` — di controller (`:1638`) atau service (`:449/:465`)?**
   - What we know: CONTEXT D-04 + spec §5 menunjuk `ProtonDataController.cs:1638` (controller, sekarang non-empty only). Service MENULIS `AssignmentUnit = TargetUnit` di `:449` (ganti coach) + `:465` (keep coach, ganti unit).
   - What's unclear: Apakah cukup validasi di controller saja, atau perlu defensive dual-check di service (ala E8 pattern `:104` + `:226`)?
   - Recommendation: Validasi PRIMARY di controller `:1638` (BypassSave entry, sebelum delegasi service) — konsisten pola V5 existing (`:1631-1639`). OPSIONAL defensive guard di service entry (`BypassSaveAsync`) untuk melindungi `ConfirmBypassAsync` path (baca dari pending). Planner putuskan; both-layer = lebih aman, single-layer = lebih ramping. CONTEXT condong single-layer controller.
   - **RESOLVED:** Single-layer di controller `:1638` (BypassSave entry) per CONTEXT D-04 ("condong single-layer controller"). Diwujudkan di Plan **401-06 Task 2** (`TargetUnit ∈ worker.UserUnits` active + org-tree, sebelum delegasi service). E8 single-active service-layer TIDAK disentuh.

2. **Import-reactivate juga reaktivasi PTA via "last assignment" (`:403-416`), bukan DeactivatedAt-correlated — apakah 401 menyentuh?**
   - What we know: PSU-07c minta "reaktivasi PTA cocok unit dgn mapping". Import-reactivate (`:403-407`) ambil `OrderByDescending(a => a.Id).First()` per coachee — "assignment terakhir", bukan korelasi window.
   - What's unclear: Apakah D-05 (reuse DeactivatedAt window) berlaku juga ke Import-reactivate, atau hanya `CoachCoacheeMappingReactivate`?
   - Recommendation: D-05 secara eksplisit menyebut `CoachCoacheeMappingReactivate` (`:1052-1076`) + AF-4. Untuk Import-reactivate, D-04 fokus pada no-clobber `AssignmentUnit` + validasi ∈UserUnits. True per-unit PTA-match = out-of-scope (butuh migration). **Rekomendasi: Import-reactivate cukup no-clobber + validasi unit (D-04); JANGAN re-architect PTA-selection "last assignment" (sama spirit AF-4).** Planner konfirmasi scope PTA-match di Import.
   - **RESOLVED:** Import-reactivate cukup no-clobber `AssignmentUnit` + validasi ∈UserUnits (D-04); PTA-selection "last assignment" (`:403-407`) TIDAK di-re-architect (spirit AF-4, D-05). Diwujudkan di Plan **401-03 Task 3** (preserve existing AssignmentUnit, hapus clobber `:356`; out-of-scope PTA-match dinyatakan eksplisit).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | `dotnet build` / `dotnet run` | ✓ (asumsi — project net8.0 aktif) | net8.0 | — |
| SQL Server (SQLEXPRESS) lokal | `dotnet run` localhost:5277 + integration test `HcPortalDB_Test_<guid>` | ✓ (HcPortalDB_Dev lokal aktif per 399) | — | — |
| xUnit + EF-InMemory | unit-test resolver/validator | ✓ (terpasang) | — | — |
| Playwright | UI test indikator D-01 (bila ada) | ✓ (terpasang, `tests/e2e/`) | — | Manual UAT browser |

**Missing dependencies with no fallback:** None — semua tooling sudah ada (0 package, 0 migration).
**Missing dependencies with fallback:** None.

**Verifikasi gate (CLAUDE.md Develop Workflow):** `dotnet build` (0 error) + `dotnet run` (cek localhost:5277) + cek DB lokal + Playwright bila ada UI (indikator D-01) → commit. Quick test run: `dotnet test --filter "Category!=Integration"` (skip SQL-real). Full: `dotnet test`. **migration=FALSE** — notify IT commit-hash + flag FALSE saat push milestone.

## Validation Architecture

> Nyquist VALIDATION.md driver. nyquist_validation = enabled (default). 0 migration phase.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net8.0) + EF-InMemory (logic) + SQL-real disposable fixture (`HcPortalDB_Test_<guid>` @ localhost\SQLEXPRESS, `[Trait("Category","Integration")]`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate runsettings) |
| Quick run command | `dotnet test --filter "Category!=Integration"` (skip SQL-real, < 30s) |
| Full suite command | `dotnet test` (termasuk integration SQL-real) |
| UI test | `tests/e2e/*.spec.ts` (Playwright, `--workers=1`) bila indikator D-01 perlu e2e |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PSU-01 | Resolver return null/skip saat AssignmentUnit kosong meski User.Unit terisi (5 resolver) | unit (EF-InMemory) | `dotnet test --filter "FullyQualifiedName~ProtonUnitResolve"` | ❌ Wave 0 |
| PSU-02 | Filter-axis: coachee di unit non-primary (AssignmentUnit≠User.Unit) tampil di unit AssignmentUnit | unit (EF-InMemory) + integration smoke | `dotnet test --filter "FullyQualifiedName~FilterAxis"` | ❌ Wave 0 |
| PSU-03 | `AssignmentUnit ∈ coachee.UserUnits` valid → accept; ∉ → reject (Assign/Edit/Import/bypass) | unit (EF-InMemory, helper) | `dotnet test --filter "FullyQualifiedName~AssignmentUnitInUserUnits"` | ❌ Wave 0 |
| PSU-04 | Cleanup PRESERVE `AssignmentUnit` non-primary yang ∈ UserUnits (tidak clobber ke primary); Import-reactivate preserve | unit (EF-InMemory) | `dotnet test --filter "FullyQualifiedName~NoClobber"` | ❌ Wave 0 |
| PSU-05 | Read-path skip → ILogger.LogWarning (no AuditLog); gate-block → AuditLog persisted + ILogger | unit (CapturingLogger + EF-InMemory AuditLogs count) | `dotnet test --filter "FullyQualifiedName~UnitUnresolvedAudit"` | ❌ Wave 0 (reuse `CapturingLogger.cs`) |
| PSU-07 | Reactivate validasi `AssignmentUnit ∈ UserUnits` aktif sebelum reaktivasi + preserve; window AF-4 ±5s utuh | unit (EF-InMemory) + integration | `dotnet test --filter "FullyQualifiedName~ReactivateUnit"` | ❌ Wave 0 |

### Observable Signals (per PSU REQ — bagaimana membuktikan terpenuhi)
| REQ | Observable Signal | How to Observe |
|-----|-------------------|----------------|
| PSU-01 | Resolver mengembalikan/skip null saat AssignmentUnit kosong walau `User.Unit` di-set | Unit test: seed coachee `User.Unit="X"`, mapping `AssignmentUnit=null` → assert resolver skip (tidak resolve "X"). Grep: 0 `.Select(u => u.Unit)` tersisa di 5 resolver. |
| PSU-02 | Coachee `AssignmentUnit="Y"`, `User.Unit="X"` muncul saat filter `unit=Y`, tidak muncul saat `unit=X` | Unit test filter site; integration smoke; runtime: BypassList/CDP listing filter Y → coachee tampil |
| PSU-03 | Assign/Import/bypass dgn `AssignmentUnit ∉ UserUnits` → ditolak (message/per-row Error) | Unit test helper `ValidateAssignmentUnitInUserUnits`; integration: insert mapping invalid → reject |
| PSU-04 | `CleanupCoachCoacheeMappingOrg` TIDAK mengubah `AssignmentUnit` non-primary sah; Import-reactivate preserve | Unit test: seed mapping `AssignmentUnit=unit-sekunder-sah` → run cleanup → assert `AssignmentUnit` unchanged. DB query before/after. |
| PSU-05 | Read-path: `AuditLogs` count TIDAK naik per page-load; gate-block: `AuditLogs` BERTAMBAH 1 + log warning | Unit test: `CapturingLogger` assert LogWarning; EF-InMemory `_context.AuditLogs.Count()` before/after (read-path=0 delta, gate=1 delta) |
| PSU-07 | Reactivate coachee dgn `AssignmentUnit` unit-dilepas → ditolak; window ±5s correlation tidak berubah | Unit test reactivate; grep: `DateDiffSecond(...) >= -5 && <= 5` (`:1059-1060`) UTUH |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "Category!=Integration"` (logic resolver/validator, < 30s) + `dotnet build` 0 error
- **Per wave merge:** `dotnet test` (full, termasuk SQL-real integration single-active smoke)
- **Phase gate:** Full suite green + `dotnet run` localhost:5277 (cek DB lokal) + Playwright indikator D-01 (bila ada UI) sebelum `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ProtonUnitResolveTests.cs` — PSU-01 resolver skip (5 resolver, AssignmentUnit-only). Ekstrak resolver ke `public static` testable seam dulu.
- [ ] `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs` — PSU-03 validasi helper (Assign/Edit/Import/bypass)
- [ ] `HcPortal.Tests/CleanupNoClobberTests.cs` — PSU-04 preserve non-primary sah
- [ ] `HcPortal.Tests/UnitUnresolvedAuditTests.cs` — PSU-05 channel hybrid (reuse `CapturingLogger.cs` existing)
- [ ] `HcPortal.Tests/ReactivateUnitValidationTests.cs` — PSU-07 validasi unit + window utuh
- [ ] Filter-axis: reuse pattern; integration smoke single-active di fixture existing (`ProtonCompletionFixture` reusable)
- [ ] Framework: TIDAK ada install — xUnit + EF-InMemory + SQL-real fixture sudah ada (`[Trait("Category","Integration")]` pattern `[VERIFIED: HcPortal.Tests/]`)

## Security Domain

> security_enforcement enabled (default). Hardening phase — tidak ada surface attack baru, tapi resolusi unit menyangkut penerbitan cert (authority).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Identity existing; tidak disentuh |
| V3 Session Management | no | Tidak disentuh |
| V4 Access Control | yes | `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` di Assign/Edit/Import/Cleanup/Reactivate/Bypass — PERTAHANKAN existing, jangan lemahkan. Authz Section 100% scalar tak tersentuh (de-risk). `[VERIFIED: :458-459, :863-864, :918, :945-946, :1015-1016]` |
| V5 Input Validation | yes | `AssignmentUnit`/`TargetUnit` divalidasi server-side `∈ UserUnits` + org-tree (jangan trust client). Pola `ValidateUnitsInSection` (mass-assignment guard, 399). |
| V6 Cryptography | no | Tidak ada crypto |
| V7 Error Handling/Logging | yes | D-03 audit channel: read-path ILogger, gate-block AuditLog persisted (queryable compliance). Jangan expose `dbEx.Message` mentah (pola AF-6 existing `:651`). |

### Known Threat Patterns for ASP.NET Core MVC + EF Core

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Cert/session diterbitkan dgn unit salah (resolve dari primary) | Tampering / Repudiation | D-02 gate-BLOCK: tak terbit session/cert bila AssignmentUnit kosong; AuditLog persisted untuk jejak |
| Mass-assignment `AssignmentUnit` ke unit yang bukan milik coachee | Tampering | Server-side validasi `∈ coachee.UserUnits` (PSU-03); jangan percaya payload client |
| Data-loss silent (Cleanup clobber unit sah ke primary) | Tampering / DoS-data | No-clobber gated (PSU-04); preserve unit `∈ UserUnits` |
| Audit-log flooding (read-path persist tiap page-load) | DoS (storage) | D-03 hybrid: read-path = ILogger SAJA (tidak persist) |
| CSRF pada POST mutasi mapping | Spoofing | `[ValidateAntiForgeryToken]` existing — PERTAHANKAN |

## Sources

### Primary (HIGH confidence — verified in this session)
- `Controllers/CoachMappingController.cs` — resolver `:1409-1419`, `:1456-1485`; Assign `:460-668`; Edit `:700-790`; Import `:300-453`; Cleanup `:861-913`; Reactivate `:1013-1097`; GET action `:42-165`; ctor `:17-26`
- `Controllers/AssessmentAdminController.cs` — gate `:1370-1428`; ctor `:33-56` (`_auditLog` via base `:46`); `_logger` `:25`
- `Controllers/CDPController.cs` — filter `:491,:1586,:1596,:4248`; defensive resolver `:511-533`, `:1701-1727`
- `Controllers/ProtonDataController.cs` — BypassList `:1509-1538` (filter `:1517`); BypassSave `:1626-1664` (TargetUnit `:1638`)
- `Services/ProtonBypassService.cs` — E8 `:104-118`, `:226-234`; junction-write `:449`, `:465`; `BypassRequest` `:8-11`; `BypassValidator` pure `:27-46`
- `Models/UserUnit.cs` — schema (Id, UserId, Unit[MaxLength 200], IsPrimary, IsActive, User?)
- `Models/ApplicationUser.cs` — confirm NO `UserUnits` nav collection (`:71` hanya TrainingRecords)
- `Data/ApplicationDbContext.cs` — `DbSet<UserUnit> UserUnits` `:35`; config `:340-359` (`.WithMany()` no-inverse); `GetUnitsForSectionAsync` `:109`, `GetSectionUnitsDictAsync` `:121`; single-active index `:333-336`
- `Controllers/WorkerController.cs` — testable-seam pattern `:44-163` (`SyncUserUnitsAsync`, `ValidateUnitsInSection`, junction query `:86`)
- `Services/AuditLogService.cs` — `LogAsync` signature `:21-42`
- `Views/Admin/CoachCoacheeMapping.cshtml` — CleanupReport idiom `:74-95`
- `HcPortal.Tests/` — `UserUnitsWriteThroughTests.cs` (EF-InMemory pattern), `ProtonBypassServiceTests.cs` + `ProtonCompletionServiceTests.cs` (SQL-real fixture `[Trait Integration]`), `CapturingLogger.cs`
- `HcPortal.csproj` — net8.0, EF Core 8.0.0

### Spec & Decisions (AUTHORITATIVE)
- `.planning/phases/401-proton-unit-resolution-hardening/401-CONTEXT.md` — D-01..D-05 LOCKED
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` — §3, §5 Fase 401, §7 invariant, §8 out-of-scope, §10 risiko
- `.planning/REQUIREMENTS.md:24-29` — PSU-01/02/03/04/05/07
- `.planning/phases/399-.../399-CONTEXT.md` — junction UserUnits kontrak (Name-string, IsPrimary, IsActive)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency terpasang, versi verified di csproj
- Architecture / code sites: HIGH — setiap site dibaca line-by-line, zero drift vs CONTEXT/spec; line ranges dikonfirmasi aktual (beberapa bergeser ±beberapa baris dari deskripsi CONTEXT, dikoreksi di tabel Component Responsibilities)
- Validation architecture: HIGH — test infra (EF-InMemory + SQL-real fixture + CapturingLogger) verified existing
- Pitfalls: HIGH — Pitfall 1 (no nav property) verified via DbContext config; Pitfall 3/4 verified via code flow
- Risk #10 (kuantifikasi empty-AssignmentUnit): MEDIUM — perlu SQL count nyata di DB lokal/Dev sebelum execute (A1)

**Drift notes (line ranges aktual vs CONTEXT — minor, semua dalam ±beberapa baris):**
- GetEligibleCoachees resolver: `:1409-1419` (CONTEXT bilang `:1409-1418` — skip line `:1419`)
- AutoCreateProgress: `:1456-1485` (CONTEXT `:1461-1473` fallback + `:1475-1479` skip — confirmed)
- AssessmentAdmin gate: `:1411-1414` (PERSIS, fallback `:1413-1414`)
- CDP defensive: `:515-526` + `:1708-1719` (PERSIS, fallback `:525-526` / `:1718-1719`)
- Cleanup clobber: `:899-900` `m.AssignmentUnit = userUnit` (PERSIS)
- Import reactivate clobber: `:356`; Import new: `:372` (PERSIS)
- Reactivate window AF-4: comment `:1043-1051`, window `:1052-1076` (PERSIS)

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (kode stabil; branch ITHandoff aktif; re-verify bila Wave-1 paralel 400/403 merge menyentuh file cluster 401 — TIDAK seharusnya, disjoint)
