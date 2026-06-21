# Phase 402: Coaching Cross-Unit Mapping - Research

**Researched:** 2026-06-19
**Domain:** ASP.NET Core 8 MVC (controller endpoints + Razor view + vanilla JS) — cross-unit coach-coachee assign + CDP self-scope, multi-unit (within 1 Bagian). 0 migration.
**Confidence:** HIGH (semua temuan terverifikasi terhadap kode live; tidak ada lib/versi baru, semata reshape kode existing)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (research HOW, not WHETHER)
- **D-01:** Dropdown unit **INLINE per-baris, KONDISIONAL** — muncul HANYA bila `coachee.UserUnits` aktif > 1; coachee single-unit auto-pakai unit-nya (tanpa dropdown). Ganti `data-unit="@coachee.Unit"` scalar (view :442) jadi expose UserUnits set. Bentuk (data-attr JSON vs server-render `<select>`) = Claude's discretion.
- **D-01b:** Relax JS lock (CXU-04) — hapus AF-2 lock single-unit (`updateAssignmentDefaults` :715-765, lock :729-738) + backstop submit `selectedUnits.size > 1` (:777-784); aturan baru = **level Bagian** (semua coachee tercentang harus 1 Bagian = coach.Section; multi-unit dibolehkan). Hint lock lama (:463-465) diganti.
- **D-02:** Default unit = coachee **PRIMARY**, bisa diubah ke unit lain ∈ `coachee.UserUnits` aktif. Tiap pick divalidasi server-side via `ValidateAssignmentUnitInUserUnits` (helper 401-01) **per-coachee** (perketat loop :531-535 yang kini pakai 1 `req.AssignmentUnit`).
- **D-03:** **Coach-first auto-scope** — pilih coach → coachee checklist auto-filter ke `coachee.Section == coach.Section` + `AssignmentSection` auto-lock = coach.Section. Server guard (CXU-02) **TOLAK** coachee `coachee.Section != coach.Section` di `CoachCoacheeMappingAssign` (saat ini ABSEN).
- **D-03b:** Eligible loader set-aware. Mekanisme (client-filter by `data-section` + server-enforce, vs AJAX endpoint per-coach) = Claude's discretion; **server WAJIB enforce CXU-02** apa pun mekanisme client.
- **D-04 (CXU-05):** Self-scope coaching-role `unit = user.Unit` di `CDPController.cs:305` (FilterCoachingProton), `:326` (ExportDashboardProgress), `:647` (lockedUnit) → expand ke **union `IN(coach.UserUnits)`**. Post-filter `:490-503` SUDAH AssignmentUnit-aware (401/PSU-02) — **JANGAN sentuh**. Catatan akurasi: spec sebut "636" tapi `:636` = statusData doughnut, BUKAN self-scope; site riil = `:647`.
- **D-05:** Reshape `CoachAssignRequest` (:1863, kini single `AssignmentUnit` :1870) → bawa map per-coachee `coacheeId→unit`. JS `submitAssign` kirim map. `AssignmentSection` tetap single = coach.Section. Shape map (Dictionary vs List of pairs) = Claude's discretion.

### Claude's Discretion
- Bentuk expose `coachee.UserUnits` ke client (data-attr JSON vs server-render dropdown per-baris).
- Shape payload map `coacheeId→unit` (Dictionary<string,string> vs List<{coacheeId,unit}>).
- Mekanisme eligible-loader set-aware (client-filter + server-enforce vs AJAX endpoint per-coach) — server-enforce CXU-02 WAJIB.
- Mekanisme union-expand self-scope CDP (handling `unit` kosong utk coaching-role).
- Teks hint pengganti pesan lock lama (:463-465).

### Deferred Ideas (OUT OF SCOPE — JANGAN research/plan)
- Kolom Unit di Excel import coach-coachee (per-coachee bulk-set unit non-primary) — defer (401 D-04); import tetap single-unit primary-default.
- Kolom `Unit` di `ProtonTrackAssignment` / PROTON paralel — out-of-scope milestone (migration ke-2, spec §8).
- Multi-Bagian per akun / mutasi cross-Bagian — out-of-scope (Invariant #1).
- Cert/analytics per-unit — D1=b primary, out-of-scope milestone.
- **Test SQL-riil cross-unit + UAT + docs D1=b — Phase 404 (Wave 3), BUKAN 402.** 402 hanya menambah controller-level unit test (InMemory) untuk guard/logic barunya.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **CXU-01** | HC pilih Bagian → pilih coach → coachee eligible = **semua coachee di Bagian coach** (lintas unit), bukan hanya unit coach (set-aware `coachee.Section == coach.Section`). | Loader saat ini global (`CoachMappingController.cs:172-175`, tanpa Section). View sudah punya `data-section` di coach-option (:422) + coachee-item (:442) + fungsi `filterCoacheesBySection` (:703-713). Client-filter by coach.Section + server-enforce CXU-02 SUFFICIENT — **AJAX endpoint TIDAK warranted** (semua data sudah di-DOM). Lihat Finding 3. |
| **CXU-02** | Server **enforce** coachee ⊆ Bagian coach (tolak cross-Bagian) — guard BARU di endpoint assign (saat ini TAK ADA perbandingan coach.Section vs coachee.Section). | Inject di `CoachCoacheeMappingAssign` setelah validasi req-shape, sebelum loop unit (:529). Butuh load `coach.Section` + `coachee.Section` per coachee. Lihat Finding 2. |
| **CXU-03** | `AssignmentUnit` per-coachee dari `coachee.UserUnits` (bukan 1 nilai batch) — reshape payload map `coacheeId→unit` + dropdown per-baris; tiap unit divalidasi per PSU-03. | Helper `ValidateAssignmentUnitInUserUnits` (Finding 1) reuse per-coachee. `CoachAssignRequest` reshape (Finding 5, D-05). `coachee.UserUnits` HARUS di-load via dict terpisah — `ApplicationUser` TAK punya nav `UserUnits` (Finding 4, Pitfall 1). |
| **CXU-04** | Relax JS lock "1 batch = 1 unit" → **level Bagian** (multi-unit dalam 1 Bagian boleh). | Hapus lock :729-738 + backstop :777-784; ganti guard "semua coachee tercentang Bagian sama = coach.Section" (Finding 6). |
| **CXU-05** | Self-scope coaching-role `unit = user.Unit` (paksa primary) → `IN(coach.UserUnits)` di `CDPController` :305/:326/:647. Coach multi-unit lihat + export **semua** coachee lintas unit; filter per-unit tetap jalan. | Base-scope (`BuildProtonProgressSubModelAsync` :465-480) SUDAH union (semua mapped coachee, no unit-filter). Hanya 3 caller (:305/:326/:647) yang **memaksa** narrow ke primary. Lihat Finding 7 — surprisingly minimal change. |
</phase_requirements>

## Summary

Phase 402 adalah **reshape kode existing** murni — tidak ada library baru, tidak ada migration, tidak ada schema. Semua perubahan terbatas di 3 file: `Controllers/CoachMappingController.cs` (endpoint assign + eligible loader), `Controllers/CDPController.cs` (self-scope 3 site), dan `Views/Admin/CoachCoacheeMapping.cshtml` (modal + JS). Fondasi yang dibutuhkan SUDAH ada dari Phase 399/401: tabel junction `UserUnits`, helper validasi `ValidateAssignmentUnitInUserUnits` (testable static seam), dan post-filter CDP yang sudah `AssignmentUnit`-aware.

Tiga temuan kunci yang menyederhanakan plan: **(1)** `ApplicationUser` TIDAK punya navigation collection `UserUnits` (helper sendiri mendokumentasikan ini sebagai "Pitfall 1") — jadi mengekspos `coachee.UserUnits` ke client WAJIB lewat dict terpisah yang di-build di action `CoachCoacheeMapping()`, bukan `@coachee.UserUnits` di Razor. **(2)** Endpoint assign `CoachCoacheeMappingAssign` TIDAK punya two-step warning→confirm flow lagi — progression-warning sudah jadi **HARD-BLOCK** (Phase 401 D-05, baris :621-626; field `ConfirmProgressionWarning` di request masih ada tapi DEAD untuk path assign). Jadi per-coachee unit loop bisa plug-in tanpa khawatir merusak alur dua-langkah. **(3)** Untuk CXU-05, base-scope coach di `BuildProtonProgressSubModelAsync` (:465-480) SUDAH mengembalikan union semua coachee yang dimapping (tidak ada filter unit di base) — yang memaksa narrow-ke-primary hanyalah 3 caller (:305/:326/:647). Fix CXU-05 = berhenti memaksa `unit = user.Unit` di sana + expand `AvailableUnits`/`lockedUnit` UI ke `coach.UserUnits`.

**Primary recommendation:** Pertahankan idiom existing persis. Untuk eligible-list pakai **client-filter by `data-section` (coach.Section) + server-enforce CXU-02** (no AJAX baru — semua coachee sudah di-DOM, coach.Section tersedia client-side via `data-section` option). Untuk unit per-coachee, expose `coachee.UserUnits` sebagai **data-attr JSON** di coachee-item (konsisten idiom `data-section`/`data-unit` existing) + render `<select>` per-baris kondisional via JS saat coach dipilih. Reshape `CoachAssignRequest` dengan `Dictionary<string,string> AssignmentUnits` (coacheeId→unit) sambil **menjaga** field `AssignmentUnit` lama untuk backward-compat fallback (coachee single-unit). Semua guard server adalah **security boundary**; client filter hanya UX.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Eligible-coachee set-aware (CXU-01) | Frontend (Razor + JS client-filter) | API/Backend (server-enforce CXU-02) | Data semua coachee + coach.Section sudah di-DOM; filter UX murni client; server adalah authoritative guard |
| Cross-Bagian reject (CXU-02) | API/Backend (`CoachCoacheeMappingAssign`) | — | **Security boundary** — server MUST enforce coachee.Section == coach.Section terlepas dari client |
| Per-coachee unit picker (CXU-03) | Frontend (Razor `<select>` + JS) | API/Backend (per-coachee validate ∈ UserUnits) | UI pilih unit; server validasi tiap pick via helper 401 |
| Payload reshape map (CXU-03/D-05) | API/Backend (`CoachAssignRequest` + assign loop) | Frontend (`submitAssign` build map) | DTO shape = backend contract; JS produksi map |
| Relax JS lock (CXU-04) | Frontend (vanilla JS `updateAssignmentDefaults`/`submitAssign`) | — | Pure client UX; backstop digant relax-level Bagian |
| Self-scope union (CXU-05) | API/Backend (`CDPController` 3 site + BuildSubModel) | Frontend (unit filter dropdown enable + populate) | Scope resolution = backend; UI dropdown reflect available units |

## Standard Stack

**Tidak ada library baru.** Phase 402 = 100% kode existing.

### Core (sudah terpasang, dipakai apa adanya)
| Komponen | Versi (verified) | Purpose | Sumber |
|----------|------------------|---------|--------|
| .NET SDK | 8.0.418 | Runtime build/run | `dotnet --version` [VERIFIED: shell] |
| ASP.NET Core MVC | net8.0 | Controller + Razor | `HcPortal.csproj` [VERIFIED: csproj] |
| EF Core | 8.0.0 | DbContext / UserUnits query | `HcPortal.Tests.csproj` :12 [VERIFIED] |
| ClosedXML | (existing) | Excel export (ExportDashboardProgress :331) | `CDPController.cs:331` [VERIFIED: grep] |
| Bootstrap 5 | (existing) | Modal + form-select + badge | `CoachCoacheeMapping.cshtml` [VERIFIED] |
| Vanilla JS (no framework) | — | `submitAssign`, `updateAssignmentDefaults`, `filterAssignmentUnits` | `CoachCoacheeMapping.cshtml:636-810` [VERIFIED] |

### Testing (existing — extend, jangan tambah)
| Komponen | Versi (verified) | Purpose |
|----------|------------------|---------|
| xUnit | 2.9.3 | Unit test framework | `HcPortal.Tests.csproj:15` [VERIFIED] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | :14 [VERIFIED] |
| EFCore.InMemory | 8.0.0 | DbContext mock untuk helper/logic test | :12 [VERIFIED] |
| EFCore.SqlServer | 8.0.0 | Real-SQL provider (untuk Phase 404, **bukan 402**) | :13 [VERIFIED] |
| Playwright (@playwright/test) | (existing) | E2E UI; baseURL `http://localhost:5277`, override `E2E_BASE_URL=http://localhost:5270` | `tests/playwright.config.ts:14` [VERIFIED] |

**Installation:** Tidak ada. Semua dependency sudah terpasang.

## Architecture Patterns

### System Architecture Diagram (data flow)

```
┌──────────────────────────────────────────────────────────────────────────┐
│  ASSIGN FLOW (CXU-01/02/03/04)                                             │
└──────────────────────────────────────────────────────────────────────────┘

  HC opens modal
       │
       ▼
  [Razor: CoachCoacheeMapping.cshtml modal]
   • coach <select> with data-section per option (:422)
   • coachee checklist items with data-section + data-units(JSON) (:442 → reshape)
       │
       │ (1) HC picks coach
       ▼
  [JS: onchange coach → NEW filter-by-coach-section]
   • show only coachee-item where data-section == coach.data-section  (CXU-01 client)
   • auto-set + lock AssignmentSection = coach.Section                (D-03)
   • for each shown coachee with >1 active unit → render inline <select> (CXU-03 D-01)
       │
       │ (2) HC checks coachees, picks per-row unit (default primary)
       ▼
  [JS: submitAssign → build payload]
   • CoachId, CoacheeIds[]
   • AssignmentUnits: { coacheeId → unit }   ← NEW map (D-05)
   • AssignmentSection (= coach.Section, single)
   • relaxed backstop: all checked coachee must share Bagian (not unit) (CXU-04)
       │
       ▼  POST /Admin/CoachCoacheeMappingAssign  [FromBody CoachAssignRequest]
  ┌─────────────────────────────────────────────────────────────────────┐
  │ CoachMappingController.CoachCoacheeMappingAssign (:514)              │
  │  a. req-shape validation (:516-523)                                  │
  │  b. NEW CXU-02 GUARD: load coach.Section + each coachee.Section      │
  │     reject if coachee.Section != coach.Section                       │
  │  c. NEW CXU-03 LOOP: foreach coachee → unit = map[coacheeId]         │
  │     validate ∈ org-tree (GetSectionUnitsDict) AND                    │
  │     ValidateAssignmentUnitInUserUnits(ctx, coacheeId, unit)  (401)   │
  │  d. duplicate-active check (:537-554)  — unchanged                   │
  │  e. progression HARD-BLOCK (:560-629)  — unchanged (no warn/confirm) │
  │  f. create mappings: AssignmentUnit = map[coacheeId] (was req.Unit)  │
  │  g. ProtonTrack side-effect + AutoCreateProgress (:648-701) unchanged│
  │  h. audit + notify (:727-746) unchanged (adjust audit detail text)   │
  └─────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│  CDP SELF-SCOPE FLOW (CXU-05)                                              │
└──────────────────────────────────────────────────────────────────────────┘

  Coach opens Dashboard / applies unit filter / exports
       │
       ▼
  Dashboard() :265  →  BuildProtonProgressSubModelAsync(user, role)  unit=null
       │                  └─ base scope (:465-480) = ALL mapped coachees (union) ✓ already
       │                  └─ post-filter (:490-503) SKIPPED when unit null ✓ already
       │
  FilterCoachingProton() :295 / ExportDashboardProgress() :316  (AJAX/export)
       │   CURRENT: coaching-role forces unit = user.Unit (primary)   ← narrows to 1 unit
       │   CXU-05:  do NOT force; pass operator's unit (null = union, value = narrow)
       ▼
  BuildProtonProgressSubModelAsync(user, role, section, unit, ...)
       • unit == null/empty  → post-filter skipped → UNION shown   (CXU-05 default)
       • unit == "UnitY"     → post-filter by AssignmentUnit (:490-503) — UNCHANGED
       • lockedUnit (:647)   → expand: AvailableUnits = coach.UserUnits, no single lock
```

### Recommended Touch Map (3 files, disjoint)
```
Controllers/
├── CoachMappingController.cs    # assign endpoint (:514) + eligible loader (:172-175) + DTO (:1863)
└── CDPController.cs             # self-scope :305 / :326 / :647 (NOT :490-503 post-filter)
Views/Admin/
└── CoachCoacheeMapping.cshtml   # modal markup (:415-507) + JS (:636-810)
HcPortal.Tests/
└── (new) CrossUnitAssignTests.cs  # CXU-02 reject, CXU-03 per-coachee validate, eligible logic
```

### Pattern 1: Static testable seam (reuse 401 pattern)
**What:** Logic baru (CXU-02 Section compare, per-coachee unit resolution) yang perlu di-unit-test sebaiknya diekstrak ke **static method** yang menerima `ApplicationDbContext` + primitif, persis pola `ValidateAssignmentUnitInUserUnits`.
**When to use:** Setiap guard/validasi yang punya cabang logika non-trivial. Menghindari kebutuhan memockup `UserManager`/`HttpContext` (yang tidak dilakukan codebase ini).
**Example:**
```csharp
// Source: CoachMappingController.cs:52-62 (existing 401-01 helper — reuse as-is per-coachee)
public static async Task<bool> ValidateAssignmentUnitInUserUnits(
    ApplicationDbContext context, string coacheeId, string? assignmentUnit)
{
    if (string.IsNullOrWhiteSpace(assignmentUnit)) return false;
    var activeUnits = await context.UserUnits
        .Where(uu => uu.UserId == coacheeId && uu.IsActive)
        .Select(uu => uu.Unit).ToListAsync();
    return activeUnits.Any(u =>
        string.Equals(u.Trim(), assignmentUnit.Trim(), StringComparison.OrdinalIgnoreCase));
}
```
Recommend: tambahkan static seam analog `CoacheeSectionMatchesCoach(ctx, coacheeId, coachSection)` ATAU lakukan compare in-line tapi tetap sediakan test via InMemory yang seed `Users` + assert endpoint behaviour secara reflektif. In-line compare lebih simpel; tapi untuk Nyquist coverage, seam static lebih mudah di-assert.

### Pattern 2: Expose junction set ke client via dict + data-attr JSON
**What:** Karena `ApplicationUser` tak punya nav `UserUnits`, build `Dictionary<string, List<string>>` (coacheeId → active units) di action, serialisasi ke JSON, render sebagai `data-units` di tiap coachee-item.
**When to use:** CXU-03 per-row conditional dropdown.
**Example:**
```csharp
// Controller (in CoachCoacheeMapping action, near :172-175)
var eligibleIds = coacheeRoleUsers.Where(u => u.IsActive && !activeCoacheeIds.Contains(u.Id))
                                   .Select(u => u.Id).ToList();
var unitsByCoachee = (await _context.UserUnits
    .Where(uu => eligibleIds.Contains(uu.UserId) && uu.IsActive)
    .Select(uu => new { uu.UserId, uu.Unit, uu.IsPrimary }).ToListAsync())
    .GroupBy(x => x.UserId)
    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.IsPrimary)  // primary first (D-02 default)
                                    .Select(x => x.Unit.Trim()).ToList());
ViewBag.CoacheeUnits = unitsByCoachee;  // serialize in view
```
```cshtml
@* View — replace data-unit scalar (:442) *@
<div class="form-check coachee-item"
     data-section="@coachee.Section"
     data-units='@Html.Raw(System.Text.Json.JsonSerializer.Serialize(
         (ViewBag.CoacheeUnits as Dictionary<string,List<string>>)?.GetValueOrDefault((string)coachee.Id) ?? new List<string>()))'>
```
Note: pola ini SAMA dengan yang sudah dipakai 401 untuk `OrphanUnitMappings` (`CoachMappingController.cs:191-194` — `unitsByCoachee` GroupBy ToDictionary). Reuse pola itu persis.

### Pattern 3: CXU-05 union = berhenti memaksa primary (bukan tambah query union)
**What:** Karena base-scope coach SUDAH union, "expand ke union" = JANGAN set `unit = user.Unit` di caller. Biarkan `unit` apa adanya (null saat operator tak filter).
**When to use:** CXU-05 di :305, :326. Untuk :647 (lockedUnit UI), ganti `lockedUnit = user.Unit` (single) → `AvailableUnits = coach.UserUnits` + `lockedUnit = null` (dropdown enabled, default "Semua Unit").
**Example:**
```csharp
// CDPController.cs:305 (FilterCoachingProton) — CURRENT:
else if (UserRoles.IsCoachingRole(roleLevel)) { section = user.Section; unit = user.Unit; }
// CXU-05: keep section lock; DON'T force unit. Validate operator's unit ∈ coach.UserUnits if provided.
else if (UserRoles.IsCoachingRole(roleLevel)) {
    section = user.Section;                       // Bagian still locked (Invariant #1)
    if (!string.IsNullOrEmpty(unit)) {           // operator narrowed → validate it's theirs
        var coachUnits = await _context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive)
                                                 .Select(uu => uu.Unit).ToListAsync();
        if (!coachUnits.Any(u => string.Equals(u.Trim(), unit.Trim(), StringComparison.OrdinalIgnoreCase)))
            unit = null;                          // reject foreign unit → fall back to union
    }
    // unit == null → BuildProtonProgressSubModelAsync post-filter SKIPPED → union (CXU-05 default)
}
```

### Anti-Patterns to Avoid
- **Menyentuh post-filter CDP `:490-503`.** Sudah `AssignmentUnit`-aware (Phase 401/PSU-02). Mengubahnya merusak filter per-unit. (CONTEXT D-04 eksplisit: JANGAN.)
- **Mengakses `@coachee.UserUnits` di Razor.** `ApplicationUser` TAK punya nav itu → NullReference/compile error. WAJIB lewat dict (Pitfall 1).
- **Menambah AJAX endpoint baru untuk eligible-list.** Tidak perlu — semua coachee + coach.Section sudah di-DOM. Client-filter + server-enforce cukup (mengurangi surface + round-trip).
- **Mengandalkan client filter sebagai security.** Server `CoachCoacheeMappingAssign` WAJIB enforce CXU-02 (Section match) + CXU-03 (∈ UserUnits) — client hanya UX.
- **Re-introduce single-unit lock di JS.** CXU-04 EKSPLISIT relax ke Bagian-level; jangan tinggalkan sisa `selectedUnits.size > 1` check.
- **Menghapus field `AssignmentUnit` lama dari `CoachAssignRequest`.** Pertahankan untuk backward-compat (coachee single-unit/fallback). Tambah map baru di sampingnya.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Validasi unit ∈ coachee.UserUnits | Query UserUnits manual di assign loop | `ValidateAssignmentUnitInUserUnits` (`:52`) | Single-source 401, sudah di-test (`AssignmentUnitInUserUnitsTests.cs`), Trim+OrdinalIgnoreCase + empty-reject built-in |
| Section→Unit valid set | Query OrganizationUnits manual | `GetSectionUnitsDictAsync()` (`ApplicationDbContext.cs:121`) | Sudah dipakai assign (:525), cascade JS (:639) |
| coachee.UserUnits dict | Loop per coachee N+1 | GroupBy→ToDictionary pola `:191-194` | Pola 401 sudah ada (OrphanUnitMappings); 1 query batch |
| Coach union scope | Query union UserUnits + join mappings | Base-scope `BuildProtonProgressSubModelAsync:465-480` already union | Coach scope tidak filter unit di base — union gratis |
| Section→Unit cascade JS | Tulis cascade baru | `sectionUnitsMap` (:639) + `filterAssignmentUnits` (:680) | Sudah ada, dipakai assign+edit |

**Key insight:** Hampir semua primitif yang dibutuhkan 402 sudah dibangun di 399/401. Tugas 402 adalah **wiring + reshape**, bukan membangun helper baru. Satu-satunya logika baru: (a) CXU-02 Section compare, (b) per-coachee unit map iteration.

## Detailed Findings (verified against live code)

### Finding 1 — Helper `ValidateAssignmentUnitInUserUnits` (CXU-03 reuse)
**Location:** `Controllers/CoachMappingController.cs:52-62` [VERIFIED]
**Signature:** `public static async Task<bool> ValidateAssignmentUnitInUserUnits(ApplicationDbContext context, string coacheeId, string? assignmentUnit)`
**Behavior (exact):**
- `string.IsNullOrWhiteSpace(assignmentUnit)` → returns `false` (empty/whitespace REJECTED — tidak resolve dari primary; D-03/PSU-01).
- Queries `context.UserUnits.Where(uu => uu.UserId == coacheeId && uu.IsActive)` (IsActive scope ONLY — inactive unit ditolak).
- Match via `string.Equals(u.Trim(), assignmentUnit.Trim(), StringComparison.OrdinalIgnoreCase)`.
- Static → testable tanpa controller instance (lihat `AssignmentUnitInUserUnitsTests.cs`).
**Per-coachee reuse:** Loop tiap `(coacheeId, map[coacheeId])` → panggil helper. Reject seluruh batch bila ada satu yang false (konsisten pola loop existing :531-535). Tests `Assign_batch_rejects_when_one_coachee_lacks_unit` (helper test :106) sudah membuktikan primitif ini.

### Finding 2 — `CoachCoacheeMappingAssign` POST flow (CXU-02/03 injection points)
**Location:** `Controllers/CoachMappingController.cs:510-749` [VERIFIED]
**Current flow (order):**
1. `:516-520` — req-shape: null/empty CoachId/CoacheeIds; coach==coachee self-check.
2. `:522-523` — `AssignmentSection` + `AssignmentUnit` (single) wajib non-empty.
3. `:525-528` — `GetSectionUnitsDictAsync` → validasi `AssignmentSection`/`AssignmentUnit` ∈ org-tree.
4. `:530-535` — **PSU-03 loop**: `foreach cid → ValidateAssignmentUnitInUserUnits(ctx, cid, req.AssignmentUnit)` — kini pakai SATU `req.AssignmentUnit` untuk semua coachee.
5. `:537-554` — duplicate-active-mapping check (coachee sudah punya coach aktif → reject dengan nama).
6. `:556-558` — load actor.
7. `:560-629` — **progression HARD-BLOCK** (D-09..D-12). Cari prevTrack same TrackType Urutan-1; bila coachee belum lulus tahun sebelumnya (`IsPrevYearPassedAsync`) → **HARD-BLOCK** (`:621-626`, tanpa warning-override; field `ConfirmProgressionWarning` tidak dibaca di sini lagi — Phase 401 D-05 drop warning-override). Exempt: bypass dua-lapis (:601-611).
8. `:631-641` — create `CoachCoacheeMapping` list; `AssignmentSection`/`AssignmentUnit` = `req.*` (single).
9. `:643-704` — transaksi: AddRange mappings + ProtonTrack side-effect (deactivate beda track, reuse inactive atau create new + `AutoCreateProgressForAssignment` :695).
10. `:706-717` — catch unique-index violation (race) → pesan ramah.
11. `:727-729` — `_auditLog.LogAsync` (detail string mengandung Section/Unit batch).
12. `:731-746` — notify coach per coachee.

**Where to inject CXU-02 + per-coachee unit (precise):**
- **CXU-02 Section guard:** inject sebagai langkah BARU **setelah :523, sebelum :525** (atau setelah :528). Load `coach = await _userManager.FindByIdAsync(req.CoachId)` (atau dari Users dict) + `coacheeSections` per coacheeId. Reject bila ada `coachee.Section != coach.Section`. WAJIB before unit-loop (jaga 1 Bagian = coach.Section, Invariant #1).
- **Per-coachee unit (CXU-03):** ubah loop :530-535 dari `req.AssignmentUnit` → `map[cid]`. Validasi tiap unit: (a) `GetSectionUnitsDict[coach.Section].Contains(map[cid])` (org-tree), (b) `ValidateAssignmentUnitInUserUnits(ctx, cid, map[cid])`. Ubah `:522-523` single-unit guard menjadi: `AssignmentSection` wajib (single), unit map wajib punya entry untuk tiap coachee.
- **Mapping creation:** ubah `:640` `AssignmentUnit = req.AssignmentUnit!.Trim()` → `AssignmentUnit = map[id].Trim()` (per-coachee).
- **Audit text:** `:728` ubah detail agar mencerminkan unit per-coachee (atau ringkas "N coachee, Bagian X, units: …").

**CRITICAL — tidak ada two-step warning→confirm di path assign.** Progression-warning = hard-block. Jadi per-coachee loop plug-in lurus, tanpa state-machine warning. (JS `submitAssign` masih punya cabang `data.warning`/`confirm` di :809-814, tapi server assign TIDAK lagi mengirim `warning:true` — cabang itu DEAD untuk assign. Edit endpoint mungkin masih pakai; verifikasi saat plan, jangan asumsikan.)

### Finding 3 — Eligible-coachee loader (CXU-01 set-aware)
**Location:** `Controllers/CoachMappingController.cs:148-200` (loader :172-175) [VERIFIED]
**Current:** `ViewBag.EligibleCoachees = coacheeRoleUsers.Where(u => u.IsActive && !activeCoacheeIds.Contains(u.Id))` — **GLOBAL** (semua unmapped active coachee, tanpa Section).
**Consumption in view:** `CoachCoacheeMapping.cshtml:15` `eligibleCoachees = ViewBag.EligibleCoachees as IEnumerable<dynamic>`; di-loop :440-457 jadi `coachee-item` dengan `data-section="@coachee.Section"` (:442). Sudah ada filter UX `filterCoacheesBySection()` (:703-713) + dropdown filter section (:429-435).
**Coach.Section client-side:** coach `<option>` punya `data-section="@coach.Section"` (:422). Jadi saat operator pilih coach, `coach.Section` tersedia client-side.
**Recommendation — CLIENT-FILTER + SERVER-ENFORCE (no AJAX):**
- Loader tetap global (atau bisa tetap; tidak perlu diubah — semua coachee dibutuhkan di DOM, JS yang menyaring). **Tidak warranted** menambah AJAX endpoint karena: (a) semua data sudah di-DOM, (b) coach.Section sudah client-side, (c) menghindari round-trip + surface baru.
- Tambah JS: pada `onchange` coach `<select>`, baca `coach.data-section`, sembunyikan coachee-item yang `data-section != coachSection` (mirip `filterCoacheesBySection` tapi sumbernya coach, bukan dropdown manual), auto-set+lock `assignAssignmentSection = coachSection`.
- **Server-enforce CXU-02 WAJIB** (Finding 2) — client filter hanya UX; manipulasi DOM tetap ditolak server.
- Catatan: dropdown manual "Filter Seksi Coachee" (:429) bisa di-redundan/disembunyikan saat coach-first auto-scope aktif (Bagian sudah dikunci oleh coach). Discretion — bisa dipertahankan sebagai no-op atau dihapus. Rekomendasi: sembunyikan/disable (coach-first sudah mengunci Bagian).

### Finding 4 — Expose `coachee.UserUnits` ke client (CXU-03 conditional dropdown)
**KEY FACT (Pitfall 1):** `ApplicationUser` **TIDAK** punya navigation collection `UserUnits`. Diverifikasi: `Models/ApplicationUser.cs` hanya punya scalar `Unit` (:33) + nav `TrainingRecords` (:71) — TIDAK ADA `ICollection<UserUnit>`. Helper sendiri mendokumentasikan: "ApplicationUser TAK punya nav collection — Pitfall 1" (`:48`). [VERIFIED: grep ApplicationUser.cs]
**Current view binding:** `coachee-item` carries `data-unit="@coachee.Unit"` — **primary scalar only** (:442). `eligibleCoachees` adalah `ApplicationUser` (dari `_userManager.GetUsersInRoleAsync`), diakses dynamic (`.Section`, `.Unit`, `.NIP`, `.FullName`, `.Id`).
**What must be loaded/projected:** Build dict terpisah `coacheeId → List<active unit names>` (primary-first untuk D-02 default) di action, serialize ke JSON di view. **Reuse pola 401** `:191-194` (`unitsByCoachee` = `_context.UserUnits.Where(checkCoacheeIds.Contains + IsActive).GroupBy(UserId).ToDictionary`).
**Cleanest approach (discretion, recommend):** `data-units` JSON array per coachee-item (konsisten idiom `data-*` existing). JS saat coach dipilih → untuk tiap coachee yang tampil, bila `units.length > 1` render inline `<select class="coachee-unit-select" data-coachee-id="…">` (default `units[0]` = primary); bila `units.length == 1` simpan unit di hidden/data-attr (auto-pakai). `submitAssign` kumpulkan map dari select per-baris + fallback single-unit.
**Alternative (server-render `<select>` per-baris):** lebih banyak markup Razor; tapi menghindari JS DOM-injection. Discretion. Rekomendasi data-attr JSON (lebih ringan, konsisten).

### Finding 5 — `CoachAssignRequest` reshape (D-05)
**Location:** `Controllers/CoachMappingController.cs:1863-1873` [VERIFIED]
```csharp
public class CoachAssignRequest {
    public string CoachId { get; set; } = "";
    public List<string> CoacheeIds { get; set; } = new();
    public int? ProtonTrackId { get; set; }
    public DateTime? StartDate { get; set; }
    public string? AssignmentSection { get; set; }
    public string? AssignmentUnit { get; set; }              // single — keep for back-compat fallback
    public bool ConfirmProgressionWarning { get; set; }       // DEAD for assign (hard-block); leave
}
```
**Recommendation:** Tambah `public Dictionary<string, string>? AssignmentUnits { get; set; }` (coacheeId→unit). **Pertahankan** `AssignmentUnit` lama sebagai fallback (coachee single-unit bisa kirim hanya map; atau JS isi map untuk semua, fallback ke `AssignmentUnit` bila map-entry hilang). `AssignmentSection` tetap single (= coach.Section). Shape `Dictionary<string,string>` lebih bersih dari `List<{coacheeId,unit}>` untuk lookup O(1) di loop. JSON System.Text.Json deserialize Dictionary native.

### Finding 6 — JS lock relax (CXU-04)
**Location:** `Views/Admin/CoachCoacheeMapping.cshtml:715-795` [VERIFIED]
**Current AF-2 single-unit lock:**
- `updateAssignmentDefaults` (:715-765): pada centang, ambil `lockedUnit = checked[0].data-unit` (:731), disable semua coachee-item yang `data-unit != lockedUnit` (:732-737), show hint (:738). Auto-fill `AssignmentSection`/`AssignmentUnit` bila `units.size == 1` (:751-763) via `sectionUnitsMap`.
- `submitAssign` backstop (:777-784): `selectedUnits` Set dari `data-unit`; bila `size > 1` → alert+abort.
- Hint lama (:463-465): "Satu batch assign hanya untuk satu unit…".
**Changes to relax → Bagian-level:**
- **Remove** unit-lock loop :731-738 (atau ganti jadi lock by **Bagian**: disable coachee-item yang `data-section != coachSection`). Karena coach-first sudah mengunci ke coach.Section, lock-by-section largely redundant; cukup pastikan checklist sudah disaring by coach.Section (Finding 3).
- **Remove/relax** auto-fill single-unit :751-763 — `AssignmentSection` sekarang di-set dari coach.Section (bukan dari unit centang). Unit penugasan tidak lagi 1 nilai batch → per-coachee select.
- **Remove** backstop `selectedUnits.size > 1` :777-784 (atau ganti jadi: semua coachee tercentang harus `data-section == coachSection` — Bagian-level backstop). 
- **Replace** hint :463-465 dengan teks baru (D-05 discretion): mis. "Pilih unit penugasan per coachee multi-unit. Semua coachee harus dalam Bagian yang sama dengan coach."
- **Build per-coachee unit map** di `submitAssign` (:788-795): ganti `AssignmentUnit: assignmentUnit` (single) dengan `AssignmentUnits: { coacheeId: <select value or single-unit> }`. Iterasi `.coachee-checkbox:checked`, untuk tiap → cari `.coachee-unit-select[data-coachee-id]` (multi-unit) atau ambil satu-satunya unit dari `data-units` (single-unit).
- `sectionUnitsMap` (:639) tetap dipakai untuk validasi org-tree client + populate select.

### Finding 7 — CDPController self-scope union (CXU-05)
**Locations [VERIFIED]:**
- `:305` `FilterCoachingProton` — `else if (UserRoles.IsCoachingRole(roleLevel)) { section = user.Section; unit = user.Unit; }`
- `:326` `ExportDashboardProgress` — identical.
- `:647` `BuildProtonProgressSubModelAsync` lockedUnit — `else if (UserRoles.IsCoachingRole(roleLevel)) { lockedSection = user.Section; lockedUnit = user.Unit; }`
- `:490-503` post-filter — **AssignmentUnit-aware (Phase 401/PSU-02), DO NOT TOUCH.**
**How `unit` flows into BuildProtonProgressSubModelAsync:**
- Base scope (`:465-480` Coach branch): `mappedCoacheeIds = mappings WHERE CoachId==user.Id && IsActive` → `scopedCoacheeIds = active assignments ∩ mapped`. **NO unit filter at base.** → base already returns UNION of all coach's coachees across units. `scopeLabel` at :477-479 uses `user.Unit` for label TEXT only (not a filter).
- Post-filter (`:488-503`): only narrows when `unit` non-empty (`:490`), via AssignmentUnit per coachee.
**Answer to research question — does passing unit=null already yield "all"?** **YES.** When `unit` is null/empty, the post-filter (:490) is skipped entirely → coach sees union. The ONLY reason a coach currently sees only-primary is the callers (:305/:326) FORCING `unit = user.Unit`. So:
- **`Dashboard()` initial load (:284)** calls `BuildProtonProgressSubModelAsync(user, role)` with `unit=null` → **already shows union** today. (No change needed there — but the UI dropdown :647 still shows lockedUnit=primary, which is misleading; fix UI.)
- **CXU-05 fix sites:** :305 + :326 (stop forcing primary; validate operator-supplied unit ∈ coach.UserUnits, else null=union) + :647 (lockedUnit → AvailableUnits=coach.UserUnits, lockedUnit=null, dropdown enabled).
**UI consumption (`_CoachingProtonPartial.cshtml:28-47`):** unit dropdown `disabled` when `unitLocked = Model.RoleLevel >= 5` (:31); `selectedUnit = Model.LockedUnit ?? Model.FilterUnit` (:32); options from `Model.AvailableUnits`. For multi-unit coach CXU-05: set `unitLocked = false` (enable dropdown) OR keep disabled but populate with coach.UserUnits — recommend **enable** so coach can narrow by unit; `AvailableUnits = coach.UserUnits` (from `_context.UserUnits WHERE UserId==user.Id && IsActive`); default option "Semua Unit" (union). The `unitLocked` flag is computed in the view via `Model.RoleLevel >= 5` — may need a model flag (e.g. `Model.UnitFilterEnabled`) or change condition. Verify exact ProtonProgressSubModel props during plan.
**coach.UserUnits source:** Query `_context.UserUnits.Where(uu => uu.UserId == user.Id && uu.IsActive)` (ApplicationUser has no nav — same Pitfall 1). Not `user.Unit`.
**Accuracy note:** Spec/ROADMAP say "636" but `:636` = statusData doughnut chart. Real self-scope lock = `:647`. Confirmed [VERIFIED]. Use :305/:326/:647.

## Common Pitfalls

### Pitfall 1: Treating ApplicationUser as if it has UserUnits navigation
**What goes wrong:** `@coachee.UserUnits` in Razor or `user.UserUnits` in controller → NullReference / compile fail. The model has NO such nav.
**Why:** Junction `UserUnit` has `User` nav (UserUnit→ApplicationUser) but reverse collection was intentionally NOT added (helper :48 documents this).
**How to avoid:** ALWAYS query `_context.UserUnits.Where(uu => uu.UserId == id && uu.IsActive)` or build a batch dict (pattern at :191-194). Verify by reading `Models/ApplicationUser.cs` before writing any `.UserUnits` access.
**Warning signs:** "ICollection<UserUnit>" appears nowhere in ApplicationUser.cs.

### Pitfall 2: Assuming a two-step warning→confirm flow exists in assign
**What goes wrong:** Planner builds per-coachee logic around a `ConfirmProgressionWarning` round-trip that no longer functions for assign.
**Why:** Phase 401 D-05 converted progression-warning to HARD-BLOCK (:621-626). Field `ConfirmProgressionWarning` (DTO :1872) + JS `data.warning` branch (:809-814) are DEAD for assign path (server never returns `warning:true` from assign).
**How to avoid:** Treat assign as single-shot. Don't add per-coachee state to a confirm flow. (Edit endpoint may differ — verify separately, don't assume.)
**Warning signs:** No `Json(new { warning = true, ... })` in `CoachCoacheeMappingAssign`.

### Pitfall 3: Modifying the CDP post-filter :490-503
**What goes wrong:** Breaks per-unit narrowing that 401 made AssignmentUnit-aware; coachees show in wrong unit again.
**Why:** CXU-05 is about the self-scope axis (callers forcing primary), NOT the post-filter mechanism.
**How to avoid:** Touch ONLY :305/:326/:647. Leave :490-503 untouched (CONTEXT D-04 explicit).
**Warning signs:** Diff includes lines in 488-545 range.

### Pitfall 4: Letting client-side eligible-filter become the security boundary
**What goes wrong:** A crafted POST assigns a cross-Bagian coachee because server doesn't check Section.
**Why:** Today there's NO `coach.Section == coachee.Section` check server-side (CXU-02 absent).
**How to avoid:** Server `CoachCoacheeMappingAssign` MUST reject cross-Bagian + validate each unit ∈ coachee.UserUnits, independent of client filtering.
**Warning signs:** Section check only in JS, not in controller.

### Pitfall 5: EF-InMemory does not enforce filtered-unique index
**What goes wrong:** A 402 unit test seeds two active mappings for one coachee and passes in-memory, masking the real single-active invariant.
**Why:** InMemory provider ignores `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique`.
**How to avoid:** 402 unit tests target LOGIC (Section compare, unit-∈-UserUnits, map iteration) — not the unique-index invariant. The single-active SQL-real test is Phase 404's job (QA-03). Keep 402 tests scoped to its new guards.
**Warning signs:** A 402 test asserting "only 1 active mapping after race" using InMemory.

## Code Examples

### CXU-02 server guard (Section match) — inject after :523
```csharp
// Load coach + each coachee section. coach.Section is authoritative (Invariant #1: 1 Bagian = coach.Section).
var coach = await _context.Users.Where(u => u.Id == req.CoachId)
                                .Select(u => new { u.Id, u.Section }).FirstOrDefaultAsync();
if (coach == null || string.IsNullOrWhiteSpace(coach.Section))
    return Json(new { success = false, message = "Coach tidak valid / belum punya Bagian." });

// AssignmentSection MUST equal coach.Section (auto-set client-side, but enforce server)
if (!string.Equals(req.AssignmentSection?.Trim(), coach.Section.Trim(), StringComparison.OrdinalIgnoreCase))
    return Json(new { success = false, message = "Bagian penugasan harus sama dengan Bagian coach." });

var coacheeSections = await _context.Users.Where(u => req.CoacheeIds.Contains(u.Id))
    .Select(u => new { u.Id, u.Section }).ToDictionaryAsync(x => x.Id, x => x.Section);
var crossBagian = req.CoacheeIds.Where(id =>
    !string.Equals(coacheeSections.GetValueOrDefault(id)?.Trim(), coach.Section.Trim(),
                   StringComparison.OrdinalIgnoreCase)).ToList();
if (crossBagian.Any())
    return Json(new { success = false, message = $"{crossBagian.Count} coachee bukan anggota Bagian coach (cross-Bagian ditolak)." });
```

### CXU-03 per-coachee unit loop (replace :530-535 + :633-641)
```csharp
// req.AssignmentUnits: Dictionary<string,string> coacheeId→unit (fallback req.AssignmentUnit)
var sectionUnitsDict = await _context.GetSectionUnitsDictAsync();
if (!sectionUnitsDict.TryGetValue(coach.Section.Trim(), out var validUnits))
    return Json(new { success = false, message = "Bagian coach tidak ditemukan di organisasi." });

var resolvedUnits = new Dictionary<string, string>();
foreach (var cid in req.CoacheeIds)
{
    var unit = (req.AssignmentUnits?.GetValueOrDefault(cid) ?? req.AssignmentUnit)?.Trim();
    if (string.IsNullOrWhiteSpace(unit))
        return Json(new { success = false, message = "Unit penugasan wajib untuk tiap coachee." });
    if (!validUnits.Contains(unit))
        return Json(new { success = false, message = $"Unit '{unit}' bukan anak Bagian coach." });
    if (!await ValidateAssignmentUnitInUserUnits(_context, cid, unit))   // PSU-03 helper (401)
        return Json(new { success = false, message = $"Unit '{unit}' bukan anggota unit aktif coachee terpilih." });
    resolvedUnits[cid] = unit;
}
// ... later, create mapping with AssignmentUnit = resolvedUnits[id]
var newMappings = req.CoacheeIds.Select(id => new CoachCoacheeMapping {
    CoachId = req.CoachId, CoacheeId = id, IsActive = true, StartDate = startDate,
    AssignmentSection = coach.Section.Trim(),
    AssignmentUnit = resolvedUnits[id]            // per-coachee (was req.AssignmentUnit single)
}).ToList();
```

### CXU-05 unit dropdown enable (view, _CoachingProtonPartial.cshtml ~:31)
```cshtml
@* CURRENT: var unitLocked = Model.RoleLevel >= 5;  → forces disabled+primary *@
@* CXU-05: enable for coaching-role so coach can narrow; default "Semua Unit" = union *@
@{
    var unitLocked = false;                       // coaching-role can now choose among their units
    var selectedUnit = Model.FilterUnit;          // no LockedUnit force; empty = union
}
@* Model.AvailableUnits must be set to coach.UserUnits in BuildProtonProgressSubModelAsync *@
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single AssignmentUnit per batch | Per-coachee unit map | Phase 402 (this) | Coachee multi-unit dapat unit penugasan berbeda dalam 1 batch |
| Coach scope = primary unit (forced) | Coach scope = union UserUnits | Phase 402 | Coach multi-unit lihat semua coachee lintas unit |
| Coachee eligible = global unmapped | Eligible = coach.Section (set-aware) | Phase 402 | Cross-unit-within-Bagian coaching |
| `AssignmentUnit ?? User.Unit` fallback | `AssignmentUnit` only (∈ UserUnits) | Phase 401 (done) | 402 reuses helper |
| Progression warning→confirm 2-step | Progression HARD-BLOCK | Phase 401 D-05 (done) | 402 assign = single-shot |

**Deprecated/outdated in this area:**
- `ConfirmProgressionWarning` (DTO :1872) + JS `data.warning` branch (:809-814) — DEAD for assign path (hard-block since 401). Leave as-is; don't build on it.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `_CoachingProtonPartial.cshtml` unit dropdown `unitLocked` computed purely from `Model.RoleLevel >= 5` (no other binding) — enabling it for CXU-05 won't break other roles | Finding 7 / CXU-05 example | LOW — verify ProtonProgressSubModel has settable AvailableUnits; may need a new model flag. Confirm during plan by re-reading partial + model. |
| A2 | Edit endpoint (`CoachCoacheeMappingEdit` :755) is out of 402 scope (CONTEXT names only assign + eligible + self-scope) — per-coachee unit is an assign-batch concern; edit is single-mapping | Finding 2 note | LOW — edit already validates unit ∈ UserUnits at :793 (401). If UAT reveals edit also needs multi-unit picker, that's a separate concern. CONTEXT scopes 402 to assign batch. |
| A3 | `Dashboard()` initial coach view already shows union (unit=null path) — only AJAX filter/export + UI dropdown force primary | Finding 7 | LOW — verified base-scope has no unit filter; logically sound. Confirm via live UAT (Phase 404) but logic verified in code. |
| A4 | Client-filter + server-enforce sufficient for eligible-list (no AJAX endpoint needed) | Finding 3 / CXU-01 | LOW — all coachee + coach.Section already in DOM; standard idiom. If coachee count is very large (perf), AJAX could be reconsidered, but current page renders all anyway. |

## Open Questions

1. **Exact `ProtonProgressSubModel` shape for unit dropdown enable (CXU-05 UI).**
   - What we know: view uses `Model.RoleLevel`, `Model.LockedUnit`, `Model.FilterUnit`, `Model.AvailableUnits` (`_CoachingProtonPartial.cshtml:31-47`). `BuildProtonProgressSubModelAsync` sets these (:670-682).
   - What's unclear: cleanest way to signal "enable unit dropdown + populate coach.UserUnits" — overload `AvailableUnits` + null `LockedUnit`, or add a bool flag.
   - Recommendation: set `AvailableUnits = coach.UserUnits`, `LockedUnit = null` for multi-unit coach; change view `unitLocked` from `RoleLevel >= 5` to a model-provided flag (or `LockedUnit != null`). Planner re-reads model props.

2. **Should the manual "Filter Seksi Coachee" dropdown (:429) remain after coach-first auto-scope?**
   - What we know: coach-first auto-locks Bagian = coach.Section, making manual section filter redundant.
   - What's unclear: keep as no-op or remove.
   - Recommendation: disable/hide when coach selected (Bagian already locked); discretion per CONTEXT.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + run | ✓ | 8.0.418 | — |
| Node.js | Playwright (UI verify) | ✓ | v24.14.0 | — |
| SQL Server (SQLEXPRESS) | local DB verify (CLAUDE.md gate) | ✓ | reachable | — |
| Playwright | E2E UI assertions (CXU-01/03/04/05) | ✓ (existing harness) | tests/e2e/* | manual UAT |
| EFCore.SqlServer (test pkg) | Phase 404 real-SQL tests (NOT 402) | ✓ | 8.0.0 | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — full toolchain present. NOTE: branch ITHandoff runs app on **localhost:5270** (not 5277). Playwright must run with `E2E_BASE_URL=http://localhost:5270` (config default is 5277, override via env — `tests/playwright.config.ts:14`).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.13.0 |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| DbContext mock | EFCore.InMemory 8.0.0 (Guid db per test) — pattern in `AssignmentUnitInUserUnitsTests.cs` |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~CrossUnit"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| E2E (UI) | `cd tests && E2E_BASE_URL=http://localhost:5270 npx playwright test` (app on 5270, branch ITHandoff) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command / Assertion | File Exists? |
|--------|----------|-----------|-------------------------------|--------------|
| CXU-01 | Eligible coachee scoped to coach.Section (client filter); server-enforce backstop | unit (server logic) + Playwright (UI filter) | `dotnet test --filter ~CrossUnit` (eligible/section logic) + spec asserts checklist hides cross-Bagian | ❌ Wave 0 (new `CrossUnitAssignTests.cs` + `coaching-crossunit-402.spec.ts`) |
| CXU-02 | Server rejects coachee where coachee.Section != coach.Section | unit | New test: seed coach(SectionA) + coachee(SectionB) → assert assign-guard logic returns reject | ❌ Wave 0 |
| CXU-03 | Per-coachee AssignmentUnit ∈ coachee.UserUnits; map iteration | unit | Reuse + extend `ValidateAssignmentUnitInUserUnits` pattern: per-coachee map, one bad unit rejects | ❌ Wave 0 (extend) |
| CXU-04 | JS lock relaxed to Bagian-level (multi-unit in 1 Bagian allowed) | Playwright | spec: select coachees from 2 units in same Bagian → both stay enabled + submit succeeds | ❌ Wave 0 |
| CXU-05 | Coach multi-unit sees union of coachees; per-unit filter still narrows | unit (scope logic) + Playwright (dashboard) | unit: `BuildProtonProgressSubModel` union when unit=null; spec: coach sees coachees from all their units | ❌ Wave 0 (scope logic test) + manual UAT for dashboard |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~CrossUnit"` (quick, < 30s).
- **Per wave merge:** full `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (must stay green; baseline ~532 per MEMORY).
- **Phase gate:** full suite green + app run on localhost:5270 + Playwright coaching-crossunit spec green + manual DB check (per CLAUDE.md Develop Workflow) before `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/CrossUnitAssignTests.cs` — CXU-02 cross-Bagian reject (static seam or InMemory + reflective), CXU-03 per-coachee unit-map validation (extend helper usage), eligible set-aware logic.
- [ ] `HcPortal.Tests/CdpCoachUnionScopeTests.cs` (or fold into above) — CXU-05 union when unit=null, narrow when unit set, foreign-unit rejected to null.
- [ ] `tests/e2e/coaching-crossunit-402.spec.ts` — CXU-01 (coach-first filter), CXU-03 (per-row unit dropdown conditional), CXU-04 (multi-unit-same-Bagian allowed), CXU-05 (coach dashboard union). Use `E2E_BASE_URL=http://localhost:5270`.
- [ ] Multi-unit fixture seed (local, temporary per Seed Data Workflow): coach + coachees with {UnitX, UnitY} in one Bagian. **Snapshot DB before, restore after** (CLAUDE.md Seed Workflow). NOTE: SQL-real invariant fixtures are Phase 404; 402 e2e fixture is for UI flow only.

*Existing infra covering 402: `AssignmentUnitInUserUnitsTests.cs` (helper reuse — already green), `CDPControllerAuthTests.cs` (authz pattern), `coachcoacheemapping-389.spec.ts` (modal baseline). Framework install: none — present.*

## Security Domain

> security_enforcement absent in config → enabled.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V1 Architecture | yes | Server enforces scope (CXU-02/05); client filter is UX-only |
| V2 Authentication | no (unchanged) | `[Authorize]` existing on all endpoints |
| V4 Access Control | **yes** | `[Authorize(Roles="Admin, HC")]` on assign (:512); CDP role-scope (`IsCoachingRole`); CXU-02 cross-Bagian guard = new access-control rule; CXU-05 coach sees only OWN coachees (base-scope `CoachId==user.Id`) |
| V5 Input Validation | **yes** | Per-coachee unit ∈ coachee.UserUnits (helper); unit ∈ org-tree (GetSectionUnitsDict); Section match; map deserialization (System.Text.Json) |
| V6 Cryptography | no | none |
| V13 API / CSRF | yes | `[ValidateAntiForgeryToken]` on assign (:513); JS sends `RequestVerificationToken` (:801) — preserve in reshaped payload |

### Known Threat Patterns for ASP.NET MVC + multi-unit assign
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Crafted POST assigns cross-Bagian coachee (bypass client filter) | Elevation/Tampering | CXU-02 server guard: reject coachee.Section != coach.Section (Finding 2 / code example) |
| Crafted POST sets AssignmentUnit ∉ coachee.UserUnits (orphan, Invariant #4) | Tampering | Per-coachee `ValidateAssignmentUnitInUserUnits` (helper 401) — reject |
| Coach manipulates CDP `unit` param to view another unit's/Bagian's coachees | Info Disclosure | Base-scope already restricts to `CoachId==user.Id` (:469) + Section locked to user.Section (:305); validate operator `unit` ∈ coach.UserUnits, else null (Finding 7 example) |
| CSRF on reshaped assign payload | Tampering | Keep `[ValidateAntiForgeryToken]` + JS token header (no change to mechanism) |
| Unit-index race (two active mappings) | Tampering | Existing catch on `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` (:706-717) — unchanged |
| Raw DbException leak | Info Disclosure | Existing: friendly message, detail to logger only (:712-713) — preserve |

## Project Constraints (from CLAUDE.md)
- **Respond in Bahasa Indonesia.**
- **Develop Workflow gate (WAJIB):** `dotnet build` + `dotnet run` → verify at **http://localhost:5270** (branch ITHandoff, NOT 5277 — avoid worktree collision) + check local DB + Playwright (UI phase) **before commit**.
- **No editing code/DB directly on Dev/Prod.** No push without local verify.
- **Migration flag = FALSE** for Phase 402 (0 migration). Notify IT with commit hash + migration=FALSE on promotion.
- **Seed Data Workflow:** any local fixture → classify (temporary/local vs permanent/prod), snapshot DB (sqlcmd BACKUP), log in `docs/SEED_JOURNAL.md`, restore after test, mark `cleaned`. Multi-unit coaching fixtures are temporary/local-only.

## Sources

### Primary (HIGH confidence — verified against live code this session)
- `Controllers/CoachMappingController.cs` — helper :52-62; eligible loader :148-200; assign :510-749; DTO :1863-1873 [VERIFIED: Read]
- `Controllers/CDPController.cs` — self-scope :305/:326/:647; BuildProtonProgressSubModelAsync :436-685; post-filter :490-503 [VERIFIED: Read]
- `Views/Admin/CoachCoacheeMapping.cshtml` — modal :415-507; JS :636-810; Razor vars :1-28 [VERIFIED: Read]
- `Views/CDP/Shared/_CoachingProtonPartial.cshtml` — unit dropdown :28-47 [VERIFIED: Read]
- `Models/ApplicationUser.cs` — NO UserUnits nav (Pitfall 1) [VERIFIED: grep]
- `Models/UserUnit.cs`; `Models/UserRoles.cs` (IsCoachingRole/HasSectionAccess :64-74) [VERIFIED: Read]
- `Data/ApplicationDbContext.cs:100-136` — GetSectionUnitsDictAsync / GetUnitsForSectionAsync [VERIFIED: Read]
- `HcPortal.Tests/AssignmentUnitInUserUnitsTests.cs`; `HcPortal.Tests.csproj`; `CDPControllerAuthTests.cs`; `tests/playwright.config.ts` [VERIFIED: Read]
- Environment probes: dotnet 8.0.418, node v24.14.0, SQLEXPRESS reachable [VERIFIED: shell]

### Secondary (project canonical docs)
- `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md` §5 (Fase 402 touchpoints), §6 (dependency), §7 (invariants #1/#4), §8 (out-of-scope) [CITED]
- `.planning/REQUIREMENTS.md` CXU-01..05 acceptance criteria [CITED]
- `.planning/phases/{402,401,399}-*/CONTEXT.md` [CITED]
- `CLAUDE.md` Develop + Seed Workflow [CITED]

## Metadata
**Confidence breakdown:**
- Standard stack: HIGH — no new libs; all versions verified against csproj/shell.
- Architecture / injection points: HIGH — every line cited verified against live code; deviations from spec (line 636 vs 647) confirmed.
- Pitfalls: HIGH — Pitfall 1 (no nav) + Pitfall 2 (no warning flow) directly verified in source.
- CXU-05 mechanism: HIGH (logic) — base-scope union verified; UI flag exact shape MEDIUM (A1, re-verify model props at plan).

**Research date:** 2026-06-19
**Valid until:** ~30 days (stable internal codebase; no external moving parts). Re-verify line numbers if CoachMappingController/CDPController edited before plan.

## RESEARCH COMPLETE
