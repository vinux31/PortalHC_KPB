# Phase 403: OrganizationController Cascade/Guard UserUnits-Aware - Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core 8 MVC controller — cascade rename/reparent + delete/deactivate guard pada junction `UserUnits` (EF Core 8, SQL Server)
**Confidence:** HIGH (semua temuan diverifikasi langsung di codebase: `Controllers/OrganizationController.cs`, `Models/UserUnit.cs`, `HcPortal.Tests/OrganizationControllerTests.cs`, `wwwroot/js/orgTree.js`, `Data/ApplicationDbContext.cs`)

## Summary

Fase 403 menambah kesadaran `UserUnits` (junction multi-unit dari Phase 399) ke 4 operasi di `OrganizationController` yang saat ini **hanya** menyentuh scalar `Users.Unit`/`Users.Section`: (1) rename cascade `EditOrganizationUnit`, (2) reparent split-detection + hard-block, (3) delete/deactivate guard, (4) `PreviewEditCascade` count. Seluruh perubahan **terisolasi di satu controller** (+ 2 file UI pendukung untuk baris preview D-03), sesuai cluster file disjoint Wave-1.

Kabar baik: codebase sudah punya **semua primitif** yang dibutuhkan. `_context.UserUnits` (DbSet sejak 399), helper root-ancestor walk (sudah ada inline L237-247), `GetSectionUnitsDictAsync()` untuk map unit→Bagian, pola transaksi `BeginTransactionAsync` (idiom persis dari `WorkerController` 399), dan — kritikal — **`OrganizationControllerTests.cs` sudah ada** dengan seam test yang persis cocok (in-memory ctx + `MakeController()` + `null!` substitute untuk `_userManager`/`_env` + `IsAjaxRequest`-forced JSON). Tidak perlu membangun infrastruktur test baru.

**Primary recommendation:** Implementasikan 4 perubahan inline di `OrganizationController.cs` mengikuti pola scalar yang sudah ada persis di sampingnya (rename `UserUnits` = mirror dari rename `Users` L218-219; guard `UserUnits` = clause OR tambahan di guard `Users` L391/L447; split-detection = reuse root-ancestor walk L237-247 + `GetSectionUnitsDictAsync`). Wrap `EditOrganizationUnit` dalam `BeginTransactionAsync`. Uji via static-friendly controller-method tests di `OrganizationControllerTests.cs` dengan **satu peringatan transaksi InMemory** (lihat Pitfall 1) + UAT DB lokal. Invariant test SQL-riil = Phase 404, jangan duplikat.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 (granularitas blok reparent — ORG-02):** Blok **HANYA saat split**. Reparent unit ke Bagian lain tetap **diizinkan** + auto-update `Section` untuk pekerja **single-unit** (pertahankan perilaku cascade existing `EditOrganizationUnit` L235-265). **HARD-BLOCK** hanya bila ada pekerja yang, selain unit yang di-reparent, **juga punya `UserUnits` aktif lain di Bagian berbeda** dari Bagian tujuan → membership-nya akan terpecah >1 Bagian (langgar Invariant #1). Minimal behavior change.
- **D-01a:** Deteksi split berbasis **`UserUnits` aktif (`IsActive=true`)** saja — membership ter-deactivate tak dihitung. Pesan blok sebut pekerja mana yang akan terpecah (NIP/nama + unit lintas-Bagian-nya) supaya operator bisa selesaikan manual dulu.
- **D-01b:** Reparent **tidak** mengubah nama unit → baris `UserUnits.Unit` tak berubah isinya saat reparent; satu-satunya efek data pada pekerja single-unit = `Section` mirror berubah (UserUnits tak punya kolom Section → tak ada update baris junction saat reparent). Tambahan 403 untuk reparent = **logika deteksi-split + blok** saja (cascade Section existing dipertahankan).
- **D-02 (cakupan + pesan delete/deactivate guard — ORG-01):** **Delete + Deactivate**, pesan spesifik. Kedua guard di-UserUnits-aware: `DeleteOrganizationUnit` (L447, saat ini `Users.AnyAsync(u => u.Section==name || u.Unit==name)`) **dan** `ToggleOrganizationUnitActive` deactivate-branch (L391, saat ini `Users.AnyAsync(u => u.Unit==name)`). Tambah scan `UserUnits` aktif (membership **sekunder**) — unit yang jadi membership sekunder pekerja **tak bisa** dihapus/dinonaktifkan.
- **D-02a:** Pesan blok **spesifik** — eksplisit bila karena membership **sekunder** (krn sekunder TAK terlihat di scalar `Users.Unit` → operator butuh tahu kenapa terblok). Boleh tetap gaya pesan existing tapi diperjelas konteks sekunder.
- **D-02b:** Guard berbasis `UserUnits` **aktif (`IsActive=true`)**. Pertahankan guard existing lain (children aktif, KKJ/CPDP files, ProtonKompetensi/CoachingGuidance) **tanpa perubahan**.
- **D-03 (PreviewEditCascade — ORG-02):** **Line terpisah.** Tambah field `affectedUserUnitsCount` di payload JSON `PreviewEditCascade` (L283-334) + tampil baris sendiri di modal preview (mis. "X baris keanggotaan unit"). Konsep beda dari scalar user-count (mirror) → jangan digabung; transparan & **preview == actual**.
- **D-03a:** Hitungan UserUnits di preview WAJIB persis sama dengan yang di-update `EditOrganizationUnit` aktual (rename: count `UserUnits.Unit==oldName`; reparent tanpa rename: 0 baris UserUnits per D-01b). Hitung hanya saat `nameChanged` mengubah baris `UserUnits` (rename Level>=1).
- **D-04 (atomicity + recompute inline — ORG-01):** **Transaksi + recompute inline.** Wrap seluruh cascade `EditOrganizationUnit` (rename scalar existing + rename baris `UserUnits` + reparent Section + deteksi-split) dalam `BeginTransactionAsync` (pola atomic 399 Edit). **Recompute mirror INLINE** di `OrganizationController` — **JANGAN reuse helper di `WorkerController`** (jaga file terisolasi Wave-1). Mirror primary-holder sudah ter-update existing L219 (`Users.Unit = name.Trim()`); 403 menambah rename baris `UserUnits.Unit==oldName` (semua, incl sekunder) + verifikasi mirror tetap konsisten. **Rename TAK mengubah `IsPrimary` flag** → tak perlu promote/recompute primary, cukup rename string + jaga mirror==baris-primary.

### Claude's Discretion

- Bentuk persis query deteksi-split (map unit-lain pekerja → Bagian via `OrganizationUnits`/`GetSectionUnitsDictAsync`) — selama berbasis `UserUnits` aktif + bandingkan Bagian tujuan (root ancestor L237-247) vs Bagian unit-lain.
- Wording final pesan blok (D-01a, D-02a) — ikut idiom pesan Indonesia existing di controller.
- Markup/teks baris baru preview UserUnits di view (D-03) — ikut idiom modal preview existing (ORG-TREE-07).
- Apakah scan guard pakai `_context.UserUnits.AnyAsync(...)` correlated atau join — bebas, asal benar (PITFALL: gunakan `_context.UserUnits`, BUKAN nav-prop `u.UserUnits` — lesson 400-01).

### Deferred Ideas (OUT OF SCOPE)

- **Mutasi-Bagian sebagai fitur first-class** (pindahkan pekerja split antar-Bagian otomatis saat reparent) — out-of-scope milestone (spec §8). 403 cukup hard-block + arahkan operator.
- **Test invariant SQL-riil reparent/delete/rename multi-unit** — Phase 404 (QA-01..04), WAJIB SQLEXPRESS (EF-InMemory tak enforce filtered-unique). 403 cukup gate lokal `dotnet build`+`dotnet run`+DB lokal+Playwright (CLAUDE.md).
- **CMP analytics/renewal per-unit** — out-of-scope (D1=b primary).
- File LAIN selain `OrganizationController` (+ view ManageOrganization untuk D-03) JANGAN disentuh. **JANGAN sentuh** `WorkerController`/`WorkerDataService` (Phase 400), `CoachMapping`/`CDP`/`ProtonData` (Phase 401) — Wave-1 paralel worktree.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **ORG-01** | Rename/reparent unit cascade ke `UserUnits.Unit` (+ recompute primary-mirror); delete-guard scan `UserUnits` (termasuk membership **sekunder**, bukan hanya scalar `Users.Unit`) agar unit yang masih dipakai tak terhapus. | §"Pattern 1" (rename cascade UserUnits), §"Pattern 3" (guard scan), §"Code Examples" #1/#3. Mirror sudah ter-update L219; tinggal tambah rename baris `UserUnits` + OR-clause di 2 guard. |
| **ORG-02** | Reparent unit lintas-Bagian **hard-BLOCK** bila pekerja `UserUnits` terpecah >1 Bagian (Invariant #1); `PreviewEditCascade` hitung baris `UserUnits` terdampak agar preview == actual. | §"Pattern 2" (split-detection query) + §"Code Examples" #2 (reuse root-ancestor walk L237-247 + `GetSectionUnitsDictAsync`); §"Pattern 4" (preview parity, +`affectedUserUnitsCount`). |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Rename cascade ke `UserUnits.Unit` + mirror | API/Backend (`OrganizationController.EditOrganizationUnit`) | Database (`UserUnits` rows, `Users.Unit` mirror) | Mutasi data denormalisasi = tanggung jawab controller yang sudah memiliki cascade scalar existing; tetap inline (D-04a, jaga isolasi). |
| Split-detection + reparent hard-block | API/Backend (`EditOrganizationUnit` pre-mutasi guard) | Database (read `UserUnits`+`OrganizationUnits`) | Validasi invariant org = server-authoritative; tak boleh dipercayakan ke client. |
| Delete/deactivate guard scan `UserUnits` | API/Backend (`DeleteOrganizationUnit` L447, `ToggleOrganizationUnitActive` L391) | Database (read `UserUnits`) | Guard referential = backend; pola OR-clause ke guard `Users` existing. |
| Preview impact count `affectedUserUnitsCount` | API/Backend (`PreviewEditCascade`) | Frontend/Client (modal `cascadeConfirmModal` + `orgTree.js`) | Count = server (parity ke actual); render baris = view + JS (read-only display). |

**Catatan tier:** Tak ada logika baru di tier client. `orgTree.js` hanya menambah satu baris display + satu term di penjumlahan `total` (lihat Pitfall 4 — file ini di luar 2 file deklarasi, harus diketahui planner).

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore | 8.0.0 | ORM, LINQ→SQL, transaksi | [VERIFIED: `*.csproj` grep] Sudah dipakai project; tidak ada upgrade. |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Provider produksi (SQL Server / SQLEXPRESS) | [VERIFIED: csproj] DB lokal `HcPortalDB_Dev` SQLEXPRESS. |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Provider unit test | [VERIFIED: test bin] Dipakai `OrganizationControllerTests` + `UserUnitsWriteThroughTests`. |
| .NET SDK | 8.0.418 | Build/run | [VERIFIED: `dotnet --version`] |
| xUnit | (project) | Test framework | [VERIFIED: `using Xunit;` di seluruh test] |

### Supporting (sudah ada di codebase — TIDAK install apa pun)
| Asset | Lokasi | Purpose |
|-------|--------|---------|
| `_context.UserUnits` (DbSet `UserUnit`) | `Models/UserUnit.cs`, `Data/ApplicationDbContext.cs` | Junction; kolom `UserId`,`Unit`(name-string ≤200),`IsPrimary`,`IsActive`. [VERIFIED: file read] |
| `GetSectionUnitsDictAsync()` | `Data/ApplicationDbContext.cs:121-136` | Map `Bagian→List<unit-anak>` (IsActive). [VERIFIED] Untuk deteksi-split. |
| `GetUnitsForSectionAsync(section)` | `Data/ApplicationDbContext.cs:109-119` | List unit anak satu Bagian. [VERIFIED] |
| Root-ancestor walk | `OrganizationController.cs:237-247` (inline existing) | Cari Bagian (Level 0) dari parentId. [VERIFIED] Reuse untuk "Bagian tujuan" reparent. |
| `BeginTransactionAsync` idiom | `WorkerController.cs:464,665,806` | `using var tx = await _context.Database.BeginTransactionAsync(); ...; await tx.CommitAsync();` [VERIFIED] Pola D-04. |
| `OrganizationControllerTests.MakeController()` | `HcPortal.Tests/OrganizationControllerTests.cs:19-33` | Seam test in-memory siap pakai. [VERIFIED] |

**Installation:** Tidak ada. `npm install`/`dotnet add package` N/A — semua dependency sudah ada (migration=FALSE).

**Version verification:** `dotnet --version` → 8.0.418 [VERIFIED 2026-06-18]. EF Core 8.0.0 [VERIFIED via csproj grep]. Tidak ada package baru.

## Architecture Patterns

### System Architecture Diagram

```
                          ┌─────────────────────────────────────────────────┐
  Admin/HC (browser)      │  ManageOrganization.cshtml + orgTree.js         │
  klik "Simpan" Edit ───► │  submitUnitModal()  ── AJAX POST ──┐            │
                          └────────────────────────────────────┼────────────┘
                                                                │
        ┌───────────────────────────────────────────────────────┘
        ▼  (1) ALWAYS first on Edit
  POST PreviewEditCascade(id,name,parentId)
        │  nameChanged? Level0→Section* ; Level≥1→Unit*  + affectedUserUnitsCount (D-03)
        │  parentChanged && !nameChanged → reparent counts (UserUnits=0, D-01b)
        ▼
   JSON { nameChanged, parentChanged, affectedUsersCount, ..., affectedUserUnitsCount }
        │
   total>0 ? showCascadeConfirm(modal) ──Batal──► abort
        │ Lanjut
        ▼  (2) actual mutation
  POST EditOrganizationUnit(id,name,parentId)
        │
        │  ┌── BeginTransactionAsync (D-04) ─────────────────────────────────┐
        │  │  a. dup-check / circular guard / Level recompute (existing)      │
        │  │  b. RENAME (oldName≠new):                                        │
        │  │       Level0 → Users.Section/Mappings/Kompetensi/Guidance        │
        │  │       Level≥1 → Users.Unit (L219, mirror)  ◄── existing          │
        │  │                + UserUnits.Unit==oldName ALL rows  ◄── NEW 403   │
        │  │  c. REPARENT (oldParent≠new, Level≥1):                           │
        │  │       SPLIT-DETECT (NEW 403) ─ ada worker terpecah? ─► ROLLBACK  │
        │  │              + Json{success:false, pesan sebut NIP/nama}          │
        │  │       else: Users.Section=newSection (existing) ; UserUnits=NO-OP │
        │  │  d. SaveChanges + CommitAsync                                     │
        │  └──────────────────────────────────────────────────────────────────┘
        ▼
   JSON { success, message }

  POST DeleteOrganizationUnit(id)          POST ToggleOrganizationUnitActive(id)
   guard children/KKJ/CPDP (existing)       guard children-aktif (existing)
   guard Users.Section||Unit (L447)         deactivate: guard Users.Unit (L391)
        + UserUnits aktif (NEW 403, D-02)         + UserUnits aktif (NEW 403, D-02)
   guard ProtonData (existing)
```

### Component Responsibilities
| File | Implementasi |
|------|--------------|
| `Controllers/OrganizationController.cs` | SEMUA logika: rename UserUnits, split-detect, guard scan, preview count, tx wrap. **File utama (terisolasi).** |
| `Views/Admin/ManageOrganization.cshtml` | Tambah `<li>` baris "X baris keanggotaan unit" di `#cascadeConfirmModal` (~L226). |
| `wwwroot/js/orgTree.js` | (lihat Pitfall 4) Set `cascadeUserUnits` di `showCascadeConfirm()` (L361-364) + tambah `affectedUserUnitsCount` ke `total` (L426-427). |

### Pattern 1: Rename cascade ke `UserUnits.Unit` (mirror dari pola scalar existing)
**What:** Saat rename unit Level≥1, selain update scalar `Users.Unit` (existing L218-219), update **semua** baris `UserUnits.Unit==oldName` (termasuk sekunder).
**When to use:** Cabang `if (oldName != name.Trim())` → `else` (Level≥1), tepat setelah update `Users.Unit` (L220).
**Example:**
```csharp
// Source: pola dari OrganizationController.cs:218-219 (scalar), diperluas ke junction
// Level >= 1 cabang, setelah: foreach (var u in affectedUsers) u.Unit = name.Trim();
var affectedUnitRows = await _context.UserUnits
    .Where(uu => uu.Unit == oldName)        // SEMUA baris (incl sekunder), incl IsActive=false
    .ToListAsync();
foreach (var uu in affectedUnitRows) uu.Unit = name.Trim();   // rename string; IsPrimary TIDAK disentuh (D-04)
int cascadedUserUnits = affectedUnitRows.Count;   // dipakai pesan + parity ke preview
```
**Catatan kritikal (D-03a parity):** rename meng-update **SEMUA** baris `UserUnits.Unit==oldName` **tanpa filter `IsActive`** (string rename tetap perlu walau membership ter-deactivate — kalau tidak, baris non-aktif menyimpan nama lama yang menjadi orphan saat unit di-reactivate). Maka preview HARUS hitung sama: `CountAsync(uu => uu.Unit==oldName)` tanpa filter IsActive. **Justifikasi:** mirror `Users.Unit` di L218 juga tak memfilter status; konsistensi penuh = count semua baris. (Catat ini sebagai keputusan eksplisit ke planner — lihat Assumptions A1.)
**Mirror konsistensi (Invariant #3):** rename tidak mengubah `IsPrimary` dan mengubah `oldName→newName` di baris primary maupun di scalar `Users.Unit` (L219) secara bersamaan dalam tx yang sama → mirror tetap `Users.Unit == baris IsPrimary`. Tidak perlu recompute primary.

### Pattern 2: Split-detection query (reuse root-ancestor walk + GetSectionUnitsDictAsync)
**What:** Sebelum reparent unit `oldName` ke Bagian tujuan `newSectionName`, deteksi pekerja yang akan terpecah: pekerja anggota `oldName` (UserUnits aktif) yang **juga** punya `UserUnits` aktif lain yang Bagian-nya ≠ `newSectionName`.
**When to use:** Cabang reparent `if (oldParentId != parentId && unit.Level >= 1)` (L235), **sebelum** mutasi Section existing — kalau split terdeteksi → return early `success:false` (di dalam tx, rollback otomatis saat dispose).
**Example:**
```csharp
// Source: reuse root-ancestor walk OrganizationController.cs:237-247 + GetSectionUnitsDictAsync (ApplicationDbContext.cs:121)
// newSectionName sudah dihitung oleh walk existing (L238-247).

// 1. Anggota unit yang di-reparent (UserUnits aktif) — D-01a IsActive only
var memberIds = await _context.UserUnits
    .Where(uu => uu.Unit == oldName && uu.IsActive)
    .Select(uu => uu.UserId)
    .Distinct()
    .ToListAsync();

// 2. Map unit-name → Bagian (untuk semua unit aktif). GetSectionUnitsDictAsync = Bagian→units; balik jadi unit→Bagian.
var dict = await _context.GetSectionUnitsDictAsync();          // Dictionary<Bagian, List<unitName>>
var unitToSection = dict
    .SelectMany(kv => kv.Value.Select(u => (Unit: u, Section: kv.Key)))
    .ToDictionary(x => x.Unit, x => x.Section);

// 3. Untuk tiap member, cek apakah ada UserUnits aktif LAIN (≠oldName) yang Bagian-nya ≠ newSectionName
var otherRows = await _context.UserUnits
    .Where(uu => memberIds.Contains(uu.UserId) && uu.IsActive && uu.Unit != oldName)
    .ToListAsync();

var splitUsers = otherRows
    .Where(uu => unitToSection.TryGetValue(uu.Unit, out var sec) && sec != newSectionName)
    .Select(uu => uu.UserId)
    .Distinct()
    .ToList();

if (splitUsers.Any())
{
    // ambil nama/NIP untuk pesan D-01a
    var names = await _context.Users
        .Where(u => splitUsers.Contains(u.Id))
        .Select(u => (u.NIP ?? "") + " - " + (u.FullName ?? u.UserName))
        .ToListAsync();
    var msg = "Tidak dapat memindahkan unit ke Bagian lain: " + names.Count +
        " pekerja akan terpecah ke >1 Bagian (" + string.Join("; ", names) +
        "). Selesaikan keanggotaan lintas-Bagian pekerja tersebut terlebih dahulu.";
    // return early — di dalam tx → rollback otomatis saat using-dispose tanpa Commit
}
```
**When to use vs not:** Hanya relevan saat Bagian tujuan **berbeda** dari Bagian asal unit. Bila reparent masih dalam Bagian yang sama (mis. pindah ke sub-parent lain di Bagian yang sama, `newSectionName` == Bagian asal), tidak ada split → tidak perlu blok. Untuk org 2-level (Bagian L0 → Unit L1), reparent unit = ganti `ParentId` ke Bagian lain, jadi `newSectionName` ≈ selalu beda kecuali ParentId tak berubah (sudah dikenai guard `oldParentId != parentId`).

### Pattern 3: Delete/deactivate guard scan `UserUnits` (OR-clause tambahan)
**What:** Perluas guard `Users` existing dengan scan `UserUnits` aktif. Pesan blok membedakan kasus sekunder.
**When to use:** `DeleteOrganizationUnit` L447 dan `ToggleOrganizationUnitActive` L391.
**Example:**
```csharp
// DeleteOrganizationUnit — Source: OrganizationController.cs:447 diperluas
bool hasUsers = await _context.Users.AnyAsync(u => u.Section == unit.Name || u.Unit == unit.Name);
bool hasUnitMembership = await _context.UserUnits.AnyAsync(uu => uu.Unit == unit.Name && uu.IsActive);  // NEW (D-02b)
if (hasUsers || hasUnitMembership)
{
    // pesan D-02a: bila !hasUsers && hasUnitMembership → eksplisit "membership sekunder"
    var why = (!hasUsers && hasUnitMembership)
        ? "Unit ini masih menjadi keanggotaan (sekunder) sejumlah pekerja yang tidak terlihat di unit utama mereka. Lepaskan keanggotaan unit ini dari pekerja terlebih dahulu."
        : "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu.";
    // return Json/TempData ...
}
```
**Edge — Level 0 (Bagian):** `UserUnits.Unit` HANYA berisi nama unit Level≥1 (anak Bagian; invariant #1). Nama Bagian (Level 0) **tidak akan pernah** match `UserUnits.Unit`. Karena `Delete`/`Toggle` guard pakai `unit.Name` apa adanya, scan `UserUnits` untuk Bagian Level-0 akan selalu `false` — **tidak salah, hanya no-op**. Boleh tetap dijalankan tanpa cabang Level (lebih simpel + tahan kalau nanti ada Sub-unit). Catat di komentar bahwa untuk Level 0 scan ini selalu kosong by-design. **Justifikasi:** `ToggleOrganizationUnitActive` L388-391 sudah cabang Level (Level0→Section, Level≥1→Unit); scan `UserUnits` cukup ditambah di cabang Level≥1 (atau unconditional — hasil sama). `DeleteOrganizationUnit` L447 tidak cabang Level (`Section==name || Unit==name`); tambah `|| hasUnitMembership` unconditional aman.

### Pattern 4: PreviewEditCascade parity (+affectedUserUnitsCount)
**What:** Tambah `affectedUserUnitsCount` ke payload JSON, hitung **persis** seperti yang di-update `EditOrganizationUnit`.
**When to use:** `PreviewEditCascade` L283-334.
**Example:**
```csharp
// Source: OrganizationController.cs:298-324 diperluas
int affectedUserUnits = 0;
if (nameChanged && unit.Level >= 1)                        // Level 0 rename → UserUnits tak tersentuh (D-03a)
    affectedUserUnits = await _context.UserUnits.CountAsync(uu => uu.Unit == oldName);  // ALL rows (match Pattern 1)
// reparent tanpa rename → UserUnits TIDAK berubah → affectedUserUnits = 0 (D-01b)

return Json(new {
    nameChanged, parentChanged,
    affectedUsersCount = affectedUsers,
    affectedMappingsCount = affectedMappings,
    affectedKompetensiCount = affectedKompetensi,
    affectedGuidanceCount = affectedGuidance,
    affectedUserUnitsCount = affectedUserUnits        // NEW (D-03)
});
```
**Parity rule (KRITIKAL — ORG-02 SC#4):** Filter di preview HARUS identik filter di `EditOrganizationUnit` Pattern 1. Pattern 1 = `Where(uu => uu.Unit==oldName)` tanpa `IsActive` → preview = `CountAsync(uu => uu.Unit==oldName)` tanpa `IsActive`. Test parity (lihat Validation) memanggil preview lalu Edit pada ctx yang sama dan assert `pUserUnits == actualUserUnitsRenamed`.

### Anti-Patterns to Avoid
- **Nav-prop `u.UserUnits`:** `ApplicationUser` TIDAK punya nav-prop `UserUnits` (hanya `UserUnit.User` satu arah, `Models/UserUnit.cs:30`). Pakai `_context.UserUnits` correlated (CS1061 lesson 400-01). [VERIFIED: `UserUnit.cs` tidak ada balik-nav di `ApplicationUser`.]
- **Recompute primary saat rename:** rename TIDAK mengubah set unit/IsPrimary (D-04). Jangan panggil logika promote/recompute — itu hanya untuk hapus-unit (Phase 399 domain). Cukup rename string.
- **Reuse helper WorkerController:** D-04a melarang — bikin compile-dependency lintas-controller yang merusak isolasi worktree Wave-1. Recompute mirror inline (tapi di sini "recompute" = no-op karena rename menjaga mirror by construction).
- **Filter `IsActive` di rename/preview:** salah untuk rename string (orphan saat reactivate) DAN merusak parity. `IsActive` HANYA dipakai di split-detection (D-01a) dan delete/deactivate guard (D-02b), BUKAN di rename/preview.
- **Blok semua reparent ber-anggota:** ditolak eksplisit (D-01, SPECIFICS). Hanya split nyata >1 Bagian.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Map unit→Bagian | Walk ParentId manual per-unit dalam loop | `GetSectionUnitsDictAsync()` (DbContext) lalu balik dict | Sudah ada, 2 query, no N+1. [VERIFIED ApplicationDbContext.cs:121] |
| Cari Bagian tujuan reparent | Query baru | Root-ancestor walk existing L237-247 (sudah menghitung `newSectionName`) | Sudah ada inline, reuse hasilnya. |
| Transaksi atomic | `try/catch` manual + SaveChanges berulang | `using var tx = await _context.Database.BeginTransactionAsync(); ...; await tx.CommitAsync();` | Pola persis 399 (WorkerController.cs:464/665/806). [VERIFIED] |
| Test seam controller | Bikin harness baru / WebApplicationFactory | `OrganizationControllerTests.MakeController()` existing | Sudah ada in-memory + null-substitute + JSON-extractor helper. [VERIFIED] |
| AJAX/non-AJAX response | Pesan kustom baru | Pola `IsAjaxRequest() ? Json(...) : TempData+Redirect` existing | Konsisten seluruh controller. |

**Key insight:** Fase ini ~90% reuse pola yang sudah ada **persis di baris sebelahnya**. Setiap penambahan UserUnits adalah mirror struktural dari operasi scalar yang sudah teruji. Risiko terbesar bukan "bagaimana menulis" tapi "parity preview==actual" dan "transaksi InMemory" (Pitfall 1).

## Runtime State Inventory

> Bukan rename/refactor string-wide — ini perubahan logika cascade/guard pada data existing. Bagian ini sebagian besar N/A, tapi dicatat eksplisit:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `UserUnits` rows di `HcPortalDB_Dev` (6 baris backfill dari 399, semua IsPrimary). Saat rename unit yang punya membership, baris `UserUnits.Unit` ikut berubah (itu memang tujuan fase). | Code edit (cascade) — TIDAK ada migrasi data terpisah; cascade berjalan saat operasi rename. |
| Live service config | None — verified: tidak ada service eksternal/n8n/scheduler yang menyimpan nama unit. Nama unit hanya di DB (`OrganizationUnits` + denormalisasi). | None. |
| OS-registered state | None — verified: tak ada task/registry yang embed nama unit. | None. |
| Secrets/env vars | None — verified: tak ada secret/env var bernama unit. | None. |
| Build artifacts | None — migration=FALSE, tak ada package rename, tak ada egg-info/binary baru. | None. |

**Catatan:** Phase 404 (QA) yang akan menguji invariant pasca-cascade di SQL riil dengan fixture multi-unit; 403 hanya menggunakan data backfill existing untuk UAT lokal.

## Common Pitfalls

### Pitfall 1: InMemory provider menolak `BeginTransactionAsync` (TransactionIgnoredWarning)
**What goes wrong:** D-04 membungkus `EditOrganizationUnit` dalam `BeginTransactionAsync`. EF Core InMemory provider **tidak mendukung transaksi** dan secara default melempar `InvalidOperationException` (`TransactionIgnoredWarning` dipromosikan jadi error) saat `BeginTransactionAsync` dipanggil. Test `OrganizationControllerTests` existing memanggil `ctrl.EditOrganizationUnit(...)` langsung pada InMemory ctx → setelah D-04, panggilan itu akan **gagal** kecuali ditangani.
**Why it happens:** [VERIFIED] Tidak ada `ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` di `ApplicationDbContext` atau test (grep kosong). Phase 399 menghindari ini dengan **menguji static helper `SyncUserUnitsAsync` langsung** (bukan transaction-wrapped `EditWorker`) — [VERIFIED: `UserUnitsWriteThroughTests` tak pernah panggil `EditWorker`].
**How to avoid (pilih satu, rekomendasi A):**
- **(A — REKOMENDASI) Test helper-level, bukan action-level.** Ekstrak logika cascade rename UserUnits + split-detection ke method/static helper yang **tidak** memanggil transaksi (transaksi tetap di action). Uji helper langsung (pola 399). Action di-cover via UAT DB lokal + Playwright. **Konsisten dengan pola proyek 399, paling rendah-risiko.**
- **(B) Suppress warning di `MakeController()`.** Tambah `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` ke `DbContextOptionsBuilder` di test. InMemory akan **mengabaikan** transaksi (no-op) tapi tetap menjalankan operasi → preview==actual tetap teruji, hanya atomicity tak teruji in-memory (atomicity = domain SQL riil Phase 404). Mengubah file test saja, bukan produksi.
- **(C) Pakai SQLite in-memory** (provider relational, dukung transaksi) untuk test action utuh. Lebih berat; tidak cocok dengan `MakeController()` existing yang pakai InMemory. Tidak direkomendasikan untuk konsistensi.
**Warning signs:** `dotnet test` lolos build tapi test merah dengan `System.InvalidOperationException: ... transactions are not supported by the in-memory store`.
**Implikasi planner:** preview-parity tests (yang HARUS memanggil baik `PreviewEditCascade` maupun `EditOrganizationUnit`) memerlukan salah satu dari (B) atau (A-dengan-helper). Rekomendasikan **B** untuk preview-parity test spesifik (paling sederhana, hanya 1 baris di `MakeController`), **A** untuk unit-level logic (split-detection bisa diuji sebagai helper murni). `PreviewEditCascade` sendiri TIDAK pakai transaksi → aman diuji langsung tanpa perubahan.

### Pitfall 2: Parity drift preview vs actual (filter IsActive tidak konsisten)
**What goes wrong:** Preview hitung `CountAsync(uu => uu.Unit==oldName && uu.IsActive)` tapi rename update `Where(uu => uu.Unit==oldName)` (tanpa IsActive) → preview < actual saat ada baris non-aktif. Melanggar ORG-02 SC#4 (preview==actual).
**Why it happens:** Bingung antara "rename semua baris (string fix)" vs "guard hanya aktif". Rename TIDAK boleh filter IsActive; guard HARUS filter IsActive.
**How to avoid:** Tetapkan satu aturan: **rename + preview = SEMUA baris (`uu.Unit==oldName`, no IsActive filter)**; **split-detect + delete/deactivate guard = aktif saja (`&& uu.IsActive`)**. Test parity yang menyeed baris IsActive=false untuk membedakan keduanya (discriminating fixture).
**Warning signs:** Test parity hijau dengan fixture semua-aktif, tapi UAT DB lokal modal menunjukkan angka berbeda dari hasil aktual saat ada membership non-aktif.

### Pitfall 3: Pesan blok reparent tak menyebut pekerja (D-01a violation)
**What goes wrong:** Hard-block reparent tanpa menyebut NIP/nama pekerja yang terpecah → operator tak tahu siapa yang harus diselesaikan dulu.
**How to avoid:** Selalu query nama/NIP `splitUsers` dan masukkan ke pesan (Pattern 2). UAT verifikasi pesan menyebut pekerja konkret.

### Pitfall 4: `orgTree.js` di luar dua file yang dideklarasikan
**What goes wrong:** CONTEXT menyebut file yang boleh disentuh = `OrganizationController.cs` + `Views/Admin/ManageOrganization.cshtml`. Tapi populasi `cascadeUsers`/`cascadeMappings` dan penjumlahan `total` (yang menentukan apakah modal muncul) ada di **`wwwroot/js/orgTree.js`** (L361-364, L426-427), bukan di view. Tanpa edit `orgTree.js`, baris `affectedUserUnitsCount` di modal **tidak akan terisi** dan tidak ikut `total` → modal mungkin tak muncul saat satu-satunya dampak adalah UserUnits.
**Why it happens:** Modal markup di `.cshtml`, tapi logika fetch+populate di file JS terpisah.
**How to avoid:** Planner HARUS memasukkan `wwwroot/js/orgTree.js` ke daftar file yang disentuh fase ini (3 file total, bukan 2). Edit minimal & aman (read-only display + 1 term penjumlahan), tidak melanggar isolasi Wave-1 karena 400/401 tidak menyentuh `orgTree.js`. **Flagged sebagai Open Question O1 untuk konfirmasi.**
**Warning signs:** UAT: rename unit yang hanya punya membership sekunder (tak ada scalar user) → modal preview tak muncul / baris UserUnits kosong.

### Pitfall 5: Casing InMemory vs SQL Server
**What goes wrong:** [VERIFIED komentar `OrganizationControllerTests.cs:4`] InMemory case-sensitive, SQL Server collation default case-insensitive. String-match `uu.Unit == oldName` di test harus pakai casing IDENTIK.
**How to avoid:** Seed fixture dengan casing persis sama; jangan andalkan case-insensitivity di unit test.

## Code Examples

### #1 Rename cascade UserUnits (di EditOrganizationUnit, cabang Level≥1)
```csharp
// Source: OrganizationController.cs:216-231 (else/Level>=1), tambahan setelah baris 220
else
{
    var affectedUsers = await _context.Users.Where(u => u.Unit == oldName).ToListAsync();
    foreach (var u in affectedUsers) u.Unit = name.Trim();   // existing — mirror primary
    cascadedUsers += affectedUsers.Count;
    // ... existing mappings/kompetensi/guidance ...

    // NEW 403 (D-04, ORG-01): rename SEMUA baris UserUnits (incl sekunder, incl non-aktif).
    // IsPrimary TIDAK disentuh → mirror tetap konsisten (baris primary ikut rename = Users.Unit ikut rename).
    var affectedUnitRows = await _context.UserUnits.Where(uu => uu.Unit == oldName).ToListAsync();
    foreach (var uu in affectedUnitRows) uu.Unit = name.Trim();
    cascadedUserUnits += affectedUnitRows.Count;
}
```

### #2 Split-detection + hard-block (di cabang reparent, sebelum mutasi Section)
Lihat Pattern 2 (lengkap). Kunci: pakai `newSectionName` yang sudah dihitung walk L238-247; `_context.GetSectionUnitsDictAsync()` untuk map unit→Bagian; filter `IsActive` (D-01a); query nama untuk pesan (D-01a).

### #3 Guard scan UserUnits (delete + deactivate)
Lihat Pattern 3 (lengkap). `_context.UserUnits.AnyAsync(uu => uu.Unit == name && uu.IsActive)`; OR ke guard `Users` existing; pesan bedakan sekunder (D-02a).

### #4 Transaksi wrap (D-04)
```csharp
// Source: idiom WorkerController.cs:665. Bungkus dari awal mutasi sampai SaveChanges.
using var tx = await _context.Database.BeginTransactionAsync();
try
{
    // ... rename scalar + rename UserUnits + split-detect (return early bila split → tx dispose tanpa commit = rollback)
    // ... reparent Section existing
    unit.Name = name.Trim();
    await _context.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();   // opsional — dispose tanpa commit sudah rollback; eksplisit lebih jelas
    throw;
}
```
**Catatan return-early di dalam tx:** bila split terdeteksi, `return Json(...)` keluar dari method → `using var tx` dispose tanpa `CommitAsync` → rollback otomatis (tidak ada mutasi yang ter-commit, walau split-detect dijalankan sebelum mutasi apa pun sehingga tak ada yang perlu di-rollback). Aman.

## State of the Art

| Old Approach (pre-403) | Current Approach (403) | Impact |
|------------------------|------------------------|--------|
| Cascade rename hanya scalar `Users.Unit`/`Section` | + rename `UserUnits.Unit` (semua baris) | Membership sekunder ikut ter-rename; tak ada orphan nama unit. |
| Delete/deactivate guard hanya scalar `Users.Unit` | + scan `UserUnits` aktif | Unit yang dipakai sebagai membership sekunder tak bisa dihapus diam-diam. |
| Reparent cross-Bagian bebas (cascade Section saja) | + hard-block bila split >1 Bagian | Invariant #1 (1 Bagian/akun) terjaga. |
| Preview tak hitung UserUnits | + `affectedUserUnitsCount` | preview==actual; transparansi ke operator. |
| Tak ada transaksi di EditOrganizationUnit | `BeginTransactionAsync` wrap | Atomicity rename+mirror (no desync window). |

**Deprecated/outdated:** Tidak ada — fase ini menambah, tidak menghapus. Semua guard existing dipertahankan (D-02b).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Rename meng-update **SEMUA** baris `UserUnits.Unit==oldName` **tanpa filter `IsActive`** (mirror perilaku scalar `Users.Unit` L218 yang juga tak filter status), dan preview menghitung sama. | Pattern 1, Pattern 4, Pitfall 2 | Bila seharusnya hanya baris aktif yang di-rename, parity rule berubah (preview & rename keduanya pakai `&& IsActive`). Keputusan ini menjaga konsistensi string + tak ada orphan saat reactivate, tapi planner/user sebaiknya konfirmasi. **Mitigasi:** apa pun pilihannya, preview HARUS pakai filter yang sama dengan rename (parity). |
| A2 | Untuk org 2-level, scan `UserUnits` saat delete/deactivate **Bagian (Level 0)** selalu kosong (nama Bagian tak pernah ada di `UserUnits.Unit`) → no-op aman. | Pattern 3 (Edge Level 0) | Bila suatu saat ada Sub-unit (Level 2) atau nama Bagian kebetulan sama dengan nama unit, scan bisa mengembalikan hasil tak terduga. Risiko rendah (org 2-level dikonfirmasi, accepted-OK STATE.md). |
| A3 | Reparent dalam org 2-level berarti unit Level-1 pindah ke Bagian (Level-0) lain → `newSectionName` umumnya berbeda dari Bagian asal; split-detect relevan. | Pattern 2 | Bila ada level antara, "Bagian tujuan" = root-ancestor (walk sudah menanganinya); logika tetap benar. |

## Open Questions

1. **O1 — `wwwroot/js/orgTree.js` masuk scope?** (lihat Pitfall 4)
   - What we know: Baris preview UserUnits (D-03) tak akan terisi/terhitung tanpa edit `orgTree.js` L361-364 + L426-427. CONTEXT hanya menyebut `OrganizationController.cs` + `ManageOrganization.cshtml`.
   - What's unclear: Apakah edit `orgTree.js` diizinkan (file ini tak disentuh 400/401 → tak melanggar isolasi Wave-1).
   - Recommendation: **YA, masukkan `orgTree.js` ke scope** (3 file total). Edit minimal (read-only display + 1 term `total`). Aman secara isolasi. Planner konfirmasi di plan.

2. **O2 — Filter IsActive pada rename (A1).**
   - What we know: scalar `Users.Unit` L218 tak filter status; konsistensi penuh → rename semua baris.
   - What's unclear: apakah user ingin membership non-aktif tetap menyimpan nama lama (histori) atau ikut ter-rename.
   - Recommendation: **rename SEMUA baris** (no IsActive filter) — hindari orphan saat reactivate; preview match. Bila user keberatan, ubah keduanya (rename+preview) ke `&& IsActive` serempak (parity terjaga).

3. **O3 — Pesan blok reparent: NIP saja atau NIP+nama+unit-lintas-Bagian?**
   - What we know: D-01a minta "NIP/nama + unit lintas-Bagian-nya".
   - Recommendation: sertakan `NIP - FullName` + (opsional) nama unit lintas-Bagian + Bagian-nya. Discretion wording (CONTEXT). Batasi panjang bila banyak pekerja (mis. tampilkan 5 pertama + "dan N lainnya").

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/run | ✓ | 8.0.418 | — |
| EF Core 8 (SqlServer/InMemory) | runtime + test | ✓ | 8.0.0 | — |
| SQL Server (SQLEXPRESS) `HcPortalDB_Dev` | UAT DB lokal | ✓ (per CLAUDE.md/SEED_WORKFLOW) | — | — |
| `UserUnits` table + backfill | semua operasi 403 | ✓ | migration `AddUserUnitsTable` applied (399, `fc015f4d`) | — |
| Playwright | UAT UI (D-03 modal) bila ada | ✓ (dipakai 399/400) | — | UAT manual browser |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — semua tersedia (migration=FALSE, tak ada package baru).

## Validation Architecture

> nyquist_validation: tidak di-set `false` di config → **diasumsikan ENABLED**. (Tidak ada `.planning/config.json` workflow.nyquist_validation=false yang ditemukan; treat as enabled.)

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (net8.0) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (no special config) |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationController" --nologo` |
| Full suite command | `dotnet test --nologo` |
| Existing seam | `HcPortal.Tests/OrganizationControllerTests.cs` — `MakeController()` (InMemory + null-substitute + JSON-extractor `GetSuccess`/`GetInt`/`GetBool`) [VERIFIED, siap pakai] |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ORG-01 | Rename Level≥1 → `UserUnits.Unit` semua baris ter-rename (incl sekunder) + mirror konsisten | unit | `dotnet test --filter "FullyQualifiedName~OrganizationController" -x` | ❌ Wave 0 (extend `OrganizationControllerTests.cs`) |
| ORG-01 | Delete-guard tolak unit dengan membership sekunder aktif | unit | idem | ❌ Wave 0 |
| ORG-01 | Deactivate-guard (Toggle) tolak unit dengan membership sekunder aktif | unit | idem | ❌ Wave 0 |
| ORG-02 | Reparent cross-Bagian: split worker → BLOCK + pesan sebut pekerja | unit | idem | ❌ Wave 0 |
| ORG-02 | Reparent single-unit worker (no split) → ALLOW + Section ter-update (regresi existing) | unit | idem | ❌ Wave 0 |
| ORG-02 | `PreviewEditCascade.affectedUserUnitsCount == actual UserUnits renamed` (parity, +fixture IsActive=false discriminating) | unit | idem | ❌ Wave 0 (lihat Pitfall 1 — preview tak pakai tx, aman; bila test memanggil Edit juga → opsi B suppress warning) |
| ORG-01/02 | Rename propagasi DB + delete/reparent tolak + preview cocok | manual UAT | `dotnet run` localhost:5277 + cek `HcPortalDB_Dev` + Playwright modal D-03 | manual (SC#5) |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~OrganizationController" --nologo` (cepat, <30s)
- **Per wave merge:** `dotnet test --nologo` (full suite — baseline ≥507 saat ini, jangan regresi; 3 skip SQLEXPRESS-gated milik 404 dipertahankan)
- **Phase gate:** Full suite hijau + `dotnet run` localhost:5277 + UAT DB lokal (rename/delete/reparent/preview) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] Extend `HcPortal.Tests/OrganizationControllerTests.cs` — tambah ~6 test untuk ORG-01/02 (rename UserUnits, 2 guard, split-block, no-split-allow, preview parity). Reuse `MakeController()` + helper extractor existing.
- [ ] **Decision per Pitfall 1:** untuk test parity yang memanggil `EditOrganizationUnit` (transaction-wrapped pasca-D-04), tambah `.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))` di `MakeController()` **(opsi B)** ATAU ekstrak cascade ke helper non-transaksi & uji helper **(opsi A)**. Rekomendasi: B untuk parity test, A untuk split-detection logic.
- [ ] Tidak perlu framework install (xUnit + InMemory sudah ada). Tidak perlu `conftest`-equivalent (xUnit fixture inline via `MakeController`).
- [ ] SQL-riil invariant test = **Phase 404 (QA-01..04), JANGAN duplikat di 403.**

*(Catatan: SQLEXPRESS-gated multi-unit invariant tests sengaja DEFER ke 404 karena EF-InMemory tak enforce filtered-unique index — tapi logika 403 [rename/guard/split-detect] adalah correctness-of-query yang tetap valid in-memory.)*

## Security Domain

> `security_enforcement`: tidak ditemukan `false` di config → diperlakukan ENABLED. Fase ini mutasi struktur org (Admin/HC only), berikut domain keamanan singkat:

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Perubahan terisolasi 1 controller; tier ownership jelas (Map di atas). |
| V2 Authentication | no (inherit) | `[Authorize(Roles="Admin, HC")]` sudah di semua action mutasi — **pertahankan, jangan hapus**. [VERIFIED L31/72/127/281/362/414] |
| V4 Access Control | yes | Semua endpoint `OrganizationController` `[Authorize(Roles="Admin, HC")]` — tak ada surface baru, tak ada relaksasi. |
| V5 Input Validation | yes | `name` di-`Trim()` + dup-check existing; split-detection berbasis data DB (bukan input mentah). String unit dari DB (org-tree), bukan user-supplied bebas. |
| V6 Cryptography | no | Tak ada kripto. |
| V7 Error Handling/Logging | yes | `AuditLogService` sudah dipakai (DeleteOrganizationUnit L488). Pertimbangkan audit-log untuk rename cascade + block reparent (existing audit hanya di Delete). Discretion. |
| V13 Web Services (CSRF) | yes | `[ValidateAntiForgeryToken]` di semua POST — **pertahankan**. [VERIFIED L73/128/282/363/415] |

### Known Threat Patterns for ASP.NET MVC + EF Core
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Mass-assignment via reparent (pindah unit ke Bagian tak sah) | Tampering | parentId divalidasi via `FindAsync` + circular guard `IsDescendantAsync` (existing); split-detect tambahan menjaga invariant #1. |
| Privilege check bypass | Elevation | `[Authorize(Roles="Admin, HC")]` di-pertahankan; tak ada endpoint baru. |
| CSRF pada mutasi org | Tampering | `[ValidateAntiForgeryToken]` dipertahankan di semua POST. |
| SQL injection | Tampering | Parameterized via EF LINQ (`Where(uu => uu.Unit == oldName)`); tak ada raw SQL. |
| Info-leak di pesan blok (NIP/nama pekerja) | Info Disclosure | D-01a sengaja menampilkan NIP/nama ke operator Admin/HC (sudah authorized; bukan publik) — acceptable; pesan hanya muncul untuk role Admin/HC. |

**Catatan keamanan:** Tidak ada perubahan authz/authn. Semua kontrol existing (RBAC + antiforgery) WAJIB dipertahankan utuh — verification step harus mengecek tidak ada `[Authorize]`/`[ValidateAntiForgeryToken]` yang hilang dari action yang disentuh.

## Sources

### Primary (HIGH confidence)
- `Controllers/OrganizationController.cs` (read penuh 1-579) — semua touchpoint L130-277 (EditOrganizationUnit), L283-334 (PreviewEditCascade), L364-410 (ToggleOrganizationUnitActive), L416-498 (DeleteOrganizationUnit), L336-358 (IsDescendantAsync/UpdateChildrenLevelsAsync).
- `Models/UserUnit.cs` — schema junction (UserId/Unit≤200/IsPrimary/IsActive, nav satu-arah `User`, TIDAK ada balik-nav di ApplicationUser).
- `HcPortal.Tests/OrganizationControllerTests.cs` — seam test existing (`MakeController`, JSON-extractor, pola preview-parity ORG-TREE-07, komentar casing Pitfall 5).
- `HcPortal.Tests/UserUnitsWriteThroughTests.cs` — pola test static-helper (menghindari transaksi InMemory).
- `wwwroot/js/orgTree.js:358-439` — `showCascadeConfirm` + `submitUnitModal` (populasi modal + penjumlahan total → Pitfall 4).
- `Views/Admin/ManageOrganization.cshtml:211-237` — `#cascadeConfirmModal` markup (titik insert baris D-03).
- `Data/ApplicationDbContext.cs:109-136` — `GetUnitsForSectionAsync` + `GetSectionUnitsDictAsync` (map unit↔Bagian).
- `WorkerController.cs:464/665/806` — idiom `BeginTransactionAsync` (pola D-04).
- `.planning/REQUIREMENTS.md` (ORG-01/02), `.planning/ROADMAP.md` (Phase 403 5 SC), `.planning/STATE.md` ([399-01..04], [400-01] lesson nav-prop), spec §5 Fase 403 + §7 invariants + §10 risiko (b).
- `dotnet --version` → 8.0.418; `*.csproj` grep → EF 8.0.0 (VERIFIED 2026-06-18).

### Secondary (MEDIUM confidence)
- EF Core InMemory `TransactionIgnoredWarning` behavior — pengetahuan EF Core 8 + diverifikasi tidak-ada-suppress di codebase (grep kosong) + diverifikasi 399 menghindarinya dengan static-helper test. (Konfirmasi final saat `dotnet test` dijalankan executor.)

### Tertiary (LOW confidence)
- None.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua sudah ada di codebase, diverifikasi langsung (no new install, migration=FALSE).
- Architecture/patterns: HIGH — setiap penambahan UserUnits adalah mirror struktural dari operasi scalar yang sudah ada persis di baris sebelahnya (rename L218, guard L391/447, preview L298-324, walk L237-247).
- Pitfalls: HIGH untuk Pitfall 4/5 (terbaca langsung di file); MEDIUM-HIGH untuk Pitfall 1 (InMemory transaksi — diverifikasi via grep kosong + pola 399, final-confirm saat executor jalankan `dotnet test`).
- Validation: HIGH — seam test `OrganizationControllerTests.cs` existing siap di-extend.

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stabil — codebase internal, .NET 8 LTS, tak ada dependency fast-moving). Re-verify bila Phase 400/401 yang berjalan paralel ternyata menyentuh `OrganizationController`/`orgTree.js`/`ManageOrganization.cshtml` (tidak seharusnya — cluster disjoint).
