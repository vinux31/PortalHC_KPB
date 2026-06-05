# Phase 350: Team View Server-Side Search Scope + Export Parity - Research

**Researched:** 2026-06-05
**Domain:** ASP.NET Core MVC search predicate + EF Core in-memory worker filter + Excel export parity (ClosedXML) + xUnit/Playwright validation
**Confidence:** HIGH (semua touch point code-verified `file:line` di sesi ini; tidak ada klaim `[ASSUMED]` yang dipromosikan jadi keputusan)

## Summary

Phase 350 sudah **sangat ter-ground** lewat `350-CONTEXT.md` (D-01..D-08) dan audit spec. Riset ini **mengkonfirmasi mekanisme** dengan membaca kode aktual, bukan menurunkan ulang desain. Tiga temuan utama dikonfirmasi by-source:

1. **SF-01 (predikat):** Blok post-load `WorkerDataService.cs:402-417` sekarang HANYA cek `t.Judul`. Pola union assessment yang harus di-mirror **sudah ada persis** di Category filter `:373-381` (`w.AssessmentSessions.Any(a => ... a.Category ...)`). `AssessmentSession.Title` adalah `string` non-nullable (default `""`, `AssessmentSession.cs:13`) tapi tetap pakai `!string.IsNullOrEmpty()` untuk konsistensi. `w.AssessmentSessions` per worker **sudah di-date-filter** di `sessionsByUser` (`:283-293`) lalu di-assign ke `worker.AssessmentSessions` (`:350`) — jadi search title otomatis date-aware **tanpa kode tambahan**. Diff = ±5 baris, tidak menyentuh badge count → D-07 preserved.

2. **SF-06 (export Category symmetry):** `ExportRecordsTeamAssessment` memanggil `GetWorkersInSection(...searchScope)` (`CMPController.cs:670`) lalu `GetAllWorkersHistory(category: null)` (`:677`). Begitu predikat SF-01 masuk, worker-list yang benar dikembalikan → Export Assessment **otomatis tidak lagi kosong** (D-06, tidak butuh kerja terpisah). Sisa SF-06 = simetri Category untuk **baris** assessment. Saat ini `GetAllWorkersHistory` meng-apply `category` HANYA ke training rows (`:217-218`); current `AssessmentSessions` projection (`:146-156`) **tidak memuat `a.Category`** dan `AssessmentAttemptHistory` **tidak punya kolom Category** (`:116`).

3. **Recommended SF-06 mechanism:** Filter di **controller** `ExportRecordsTeamAssessment` (BUKAN ubah signature `GetAllWorkersHistory`). Lihat rasional di "Architecture Patterns / Pattern 3". Alasan: `GetAllWorkersHistory` punya 3 caller dan signature-nya sensitif; controller-level filter adalah perubahan terisolasi, dan keputusan "drop archived saat Category aktif" memang murni keputusan view-export (bukan domain service).

**Primary recommendation:** Mirror persis pola Category-union `:373-381` ke blok search `:402-417` (tambah `assessmentMatch`), ubah 2 string + 1 label dropdown di `RecordsTeam.cshtml` (value internal tetap), dan untuk SF-06 tambahkan opsi `category` ke current-session projection `GetAllWorkersHistory` + filter+drop-archived **di controller** `ExportRecordsTeamAssessment`. Folded tests: 4 xUnit `[Fact]` baru di `WorkerDataServiceSearchTests.cs` + Playwright UAT model `cmp-records-346.spec.ts`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Search predicate "judul assessment cocok" | API/Backend (`WorkerDataService.GetWorkersInSection`) | — | Single source dipakai partial + 2 export; D-07 invariant ada di tier ini |
| Dropdown label + placeholder (micro-copy) | Frontend Server (Razor `RecordsTeam.cshtml`) | — | Text-only, value internal tetap; UI-SPEC mengunci 3 string |
| Export worker pre-filter | API/Backend (controller orchestrasi `GetWorkersInSection` → `GetAllWorkersHistory`) | — | Sama predicate dengan on-screen → WYSIWYG |
| Export Category symmetry (drop archived) | API/Backend (controller `ExportRecordsTeamAssessment`) | Service (`GetAllWorkersHistory` projection) | Keputusan view-export, bukan domain rule; isolasi di controller |
| Export link querystring (search/searchScope propagation) | Browser/Client (vanilla JS `updateExportLinks`) | — | **Sudah benar** (`:329-346`) — zero change |

## Standard Stack

Tidak ada dependency baru. Semua dipakai-ulang verbatim (no migration, no new package).

### Core (existing — verified in-repo)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller + Razor view | [VERIFIED: HcPortal.Tests.csproj `net8.0`] stack proyek |
| EF Core | 8.0.0 | `_context.AssessmentSessions` query + date push-down | [VERIFIED: csproj] existing query path |
| ClosedXML (`XLWorkbook`) | (existing) | Excel export `ExcelExportHelper` | [VERIFIED: `CMPController.cs:682`] export sudah pakai ini |
| Bootstrap | 5.3 | `form-select`/`form-control` dropdown + search | [CITED: 350-UI-SPEC.md] reuse verbatim |

### Test stack (existing — verified)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| xUnit | 2.9.3 | `[Fact]` predicate tests | [VERIFIED: HcPortal.Tests.csproj] |
| EF Core InMemory | 8.0.0 | `UseInMemoryDatabase(Guid)` per-test isolation | [VERIFIED: csproj + `WorkerDataServiceSearchTests.cs:21`] |
| @playwright/test | ^1.58.2 | Team View UAT e2e | [VERIFIED: tests/package.json] |
| exceljs | ^4.4.0 | Parse exported XLSX di Playwright (opsional, untuk export-content assertion) | [VERIFIED: tests/package.json] sudah dependency |

**Installation:** None. `dotnet build` + `npx playwright test` cukup.

## Architecture Patterns

### System Architecture Diagram

```
                       ┌──────────────────────────────────────────────┐
  Browser (Team View)  │  RecordsTeam.cshtml                           │
  user ketik search ──▶│   #teamSearch  +  #searchScope (Nama/         │
                       │                  Training/Keduanya)           │
                       │   filterTeamTable() ──▶ fetch RecordsTeamPartial│
                       │   updateExportLinks() ──▶ set href Export (qs: │
                       │     search, searchScope, category, dates...)   │
                       └───────┬───────────────────────┬───────────────┘
                               │ AJAX (on-screen)       │ click Export (WYSIWYG)
                               ▼                        ▼
         CMPController.RecordsTeamPartial:753   CMPController.ExportRecordsTeam{Assessment,Training}
                               │                        │   :652 / :704
                               │                        │
                               └──────────┬─────────────┘  both call ↓ same predicate
                                          ▼
                  WorkerDataService.GetWorkersInSection :242
                   ├─ SQL pre-narrow "Nama"            :257-264
                   ├─ date-filter sessions→sessionsByUser :283-293
                   ├─ build workerList (badge counts)  :312-370   ◀── D-07: counts frozen here
                   ├─ Category narrow (union assess)   :373-381   ◀── PATTERN TO MIRROR
                   └─ POST-LOAD search "Training"/"Keduanya" :402-417 ◀── SF-01 ADD assessmentMatch
                                          │
                            returns List<WorkerTrainingStatus> (filtered workers, counts intact)
                                          │
          Export only ─────────────────▶ filteredIds ──▶ GetAllWorkersHistory(workerIds, ...) :92
                                          ├─ archived AssessmentAttemptHistory (NO Category col) :104-131
                                          ├─ current AssessmentSessions (.Category exists, NOT projected) :134-199
                                          └─ training rows (category filter applied) :206-220
                                          │
                       ExportRecordsTeamAssessment :673  ◀── SF-06 ADD: drop archived + narrow current by Category
                       ExportRecordsTeamTraining   :725  (already category-filtered :729)
```

### Recommended Touch-Point Map (no new files for code; new test files folded)
```
Services/WorkerDataService.cs        # SF-01 predicate :402-417 ; SF-06 projection :146-156 (+ a.Category)
Controllers/CMPController.cs          # SF-06 :673-680 (drop archived + Category narrow current)
Views/CMP/RecordsTeam.cshtml          # SF-02 micro-copy :96 placeholder, :102 label (value tetap)
HcPortal.Tests/WorkerDataServiceSearchTests.cs  # +4 [Fact] (folded)
tests/e2e/cmp-records-350.spec.ts     # NEW Playwright UAT (model: cmp-records-346.spec.ts)
tests/sql/cmp350-seed.sql             # NEW seed (model: cmp346-seed.sql) — assessment w/ distinct title
```

### Pattern 1: SF-01 — mirror Category-union ke blok search (post-load, D-07-safe)
**What:** Tambahkan `assessmentMatch` ke predikat scope "Training" dan "Keduanya".
**When to use:** SF-01 implementation. JANGAN pindah ke SQL pre-narrow (asimetri disengaja, audit §3.D).
**Existing pattern being mirrored** (`WorkerDataService.cs:373-381`, verified):
```csharp
// Category narrow — SUDAH union assessment (pola yang di-mirror):
workerList = workerList.Where(w =>
    w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.Kategori) &&
                               string.Equals(t.Kategori, category, StringComparison.OrdinalIgnoreCase))
    || w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Category) &&
                                      string.Equals(a.Category, category, StringComparison.OrdinalIgnoreCase))
).ToList();
```
**Target diff** (`:402-417`, the search block — add assessmentMatch):
```csharp
// Source: verified current code WorkerDataService.cs:402-417
if (!string.IsNullOrEmpty(search) && (searchScope == "Training" || searchScope == "Keduanya"))
{
    var searchLower = search.ToLower();
    workerList = workerList.Where(w =>
    {
        bool trainingMatch = w.TrainingRecords != null &&
            w.TrainingRecords.Any(t => !string.IsNullOrEmpty(t.Judul) &&
                                       t.Judul.ToLower().Contains(searchLower));
        // SF-01 ADD — mirror :378-380, but match a.Title instead of a.Category:
        bool assessmentMatch = w.AssessmentSessions != null &&
            w.AssessmentSessions.Any(a => !string.IsNullOrEmpty(a.Title) &&
                                          a.Title.ToLower().Contains(searchLower));
        if (searchScope == "Training") return trainingMatch || assessmentMatch;
        // Keduanya: union Nama/NIP OR Training OR Assessment
        bool nameMatch =
            (!string.IsNullOrEmpty(w.WorkerName) && w.WorkerName.ToLower().Contains(searchLower)) ||
            (!string.IsNullOrEmpty(w.NIP) && w.NIP.ToLower().Contains(searchLower));
        return nameMatch || trainingMatch || assessmentMatch;
    }).ToList();
}
```
**Why D-07 holds:** filter berjalan di level `workerList` (which/whether worker muncul). `worker.CompletedAssessments`, `worker.TotalTrainings`, `worker.CompletedTrainings` sudah di-set di loop `:312-370` SEBELUM blok ini dan **tidak disentuh**. [VERIFIED: WorkerDataService.cs:325-350]

### Pattern 2: SF-02 — micro-copy text-only (value internal frozen)
**What:** 2 string + 1 label dropdown. Value `<option value="Training">` **TIDAK BOLEH** berubah.
**When to use:** SF-02. Lihat UI-SPEC Copywriting Contract.
**Diff** (`RecordsTeam.cshtml`, verified `:96`, `:102`):
```html
<!-- :96 placeholder (D-04) -->
<input type="text" id="teamSearch" class="form-control"
       placeholder="Cari nama/NIP, judul training, atau judul assessment..." oninput="filterTeamTable()" />
<!-- :102 label opsi tengah (D-01/D-03) — value="Training" TETAP -->
<option value="Training">Judul Kegiatan</option>
<!-- :107 hint — KEEP VERBATIM (D-05): "Menyaring worker yang muncul; jumlah badge per worker tetap utuh." -->
```

### Pattern 3: SF-06 — Category symmetry untuk baris assessment (RECOMMENDED: controller-level)
**What:** Saat `category` aktif di Export Assessment → narrow current sessions by Category + DROP archived rows. Saat `category` kosong → archived muncul normal (perilaku sekarang).
**Recommended mechanism (Claude's Discretion → controller):**

Step A — extend current-session projection `GetAllWorkersHistory:146-156` agar memuat `a.Category` (anonymous → `AllWorkersHistoryRow.Kategori` di `:186-199`; field `Kategori` **sudah ada** di model `AllWorkersHistoryRow.cs:32`, currently null untuk assessment):
```csharp
// :146-156 add  a.Category  ke anonymous projection
// :186-199 set  Kategori = a.Category  di AllWorkersHistoryRow
```
Step B — filter **di controller** `ExportRecordsTeamAssessment:673-680` setelah call:
```csharp
// Source: target — CMPController.cs:673-680
var (assessmentRows, _) = await _workerDataService.GetAllWorkersHistory(
    workerIds: filteredIds, from: from, to: to, category: null, subCategory: null);

var filtered = assessmentRows;
if (!string.IsNullOrEmpty(category))
{
    // D-07 SF-06: archived (no Category col) di-DROP; current di-narrow by Category (case-insensitive).
    filtered = assessmentRows.Where(r =>
        !string.IsNullOrEmpty(r.Kategori) &&
        string.Equals(r.Kategori, category, StringComparison.OrdinalIgnoreCase)
    ).ToList();
}
// Note: archived rows punya Kategori == null → otomatis ter-drop oleh predikat di atas. Itu by-design D-07.
```

**Rationale (controller vs service):**
- `GetAllWorkersHistory` punya **3 caller** (`CMPController:673`, `:725`, `AssessmentAdminController:308`). Mengubah perilaku `category` untuk assessment di dalam service **berisiko regresi** pada `AssessmentAdminController:308` (History tab "All Workers") yang memanggil tanpa argumen — saat ini archived assessment selalu muncul di sana, dan tidak boleh berubah.
- "Drop archived saat Category aktif" adalah **keputusan view-export** (konsistensi dengan worker-visibility on-screen yang keys ke current `AssessmentSessions.Category`), bukan domain rule. Menaruh di controller membuat scope perubahan terisolasi ke Export Assessment saja.
- Step A (projeksi `a.Category`) tetap perlu di service karena data harus mengalir keluar; tapi itu **additive** (mengisi field yang sudah ada, null sebelumnya) dan tidak mengubah filtering behavior caller lain. [VERIFIED: AllWorkersHistoryRow.cs:32 field exists, currently unset for assessment]

**Alternative considered (new optional param di GetAllWorkersHistory):** bisa, tapi menambah surface signature + butuh audit 3 caller. Controller-level lebih kecil & lebih jelas. Jika planner tetap mau service-side, gunakan param baru `bool narrowAssessmentByCategory = false` agar caller lama backward-compat — tapi RESEARCH merekomendasikan controller.

### Anti-Patterns to Avoid
- **Ganti `value="Training"` → `value="Judul Kegiatan"`/`"Assessment"`** — break server switch `:402`, test, sessionStorage `cmp-records-team-filter`. Hanya teks `<option>` yang berubah. [CITED: 350-UI-SPEC.md "Kontrak label-vs-value"]
- **Pindah search assessment ke SQL pre-narrow `:257-264`** — buang training/assessment-only match + langgar asimetri D-07. [CITED: audit §3.D]
- **Apply Category ke assessment di dalam `GetAllWorkersHistory`** — regresi `AssessmentAdminController:308` History tab. Lakukan di controller.
- **Sentuh `CompletedAssessments`/`TotalTrainings` saat filter** — langgar D-07 badge invariant.
- **`==` / `string.Contains` tanpa `.ToLower()` kedua sisi** — InMemory DB case-sensitive; pola existing pakai `.ToLower().Contains()`. [VERIFIED: WorkerDataServiceSearchTests.cs:5 comment + :404 code]

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Date-awareness search assessment | Re-filter sessions by date di blok search | `worker.AssessmentSessions` (sudah date-filtered `:283-293`→`:350`) | Sudah ter-narrow; double-filter = bug + redundan |
| Export link querystring | Tulis ulang propagasi param | `updateExportLinks` `:329-346` (sudah sertakan search/searchScope) | Sudah by-design F; zero change |
| Excel writing | Manual CSV/string builder | `ExcelExportHelper` + `XLWorkbook` (existing) | Sudah dipakai 2 export endpoint |
| InMemory test DB setup | Custom mock context | `MakeService(out ctx)` + `User/Training/Session` helpers di test file | Helper sudah lengkap (`Session(...).Title`) |
| DB snapshot/restore di e2e | Manual sqlcmd di test | `helpers/dbSnapshot` (`db.backup/restore/execScript`) | Pola SEED_WORKFLOW sudah ada di `cmp-records-346.spec.ts` |

**Key insight:** Semua infrastruktur (predicate pattern, export link JS, Excel helper, test harness, seed/restore) sudah ada. Phase 350 = **delta minimal terhadap pola yang sudah teruji**, bukan bangun baru.

## Common Pitfalls

### Pitfall 1: InMemory case-sensitivity
**What goes wrong:** Test assert gagal karena `Contains` case-sensitive di EF InMemory.
**Why it happens:** InMemory provider tidak emulasi collation SQL Server.
**How to avoid:** `.ToLower()` di kedua sisi (sudah pola existing `:404`, `:408`). Search block sudah `searchLower`; pastikan `a.Title.ToLower()`.
**Warning signs:** Test "Asm K3" vs search "k3" return 0.

### Pitfall 2: Mengubah value dropdown → break backward-compat
**What goes wrong:** sessionStorage `cmp-records-team-filter` lama menyimpan `searchScope: "Training"`; server switch hanya kenal "Training"/"Keduanya"/"Nama".
**Why it happens:** Mengira label = value.
**How to avoid:** Hanya ubah inner text `<option>`. Value frozen. [CITED: 350-UI-SPEC.md]
**Warning signs:** restore filter dari sessionStorage tidak match opsi → reset diam-diam.

### Pitfall 3: SF-06 regresi History tab "All Workers"
**What goes wrong:** Kalau Category dipaksa narrow assessment di `GetAllWorkersHistory`, `AssessmentAdminController:308` (call tanpa argumen) bisa kehilangan archived rows tak terduga.
**Why it happens:** Service punya 3 caller; perubahan domain-wide.
**How to avoid:** Filter di controller `ExportRecordsTeamAssessment` saja (Pattern 3). Step A (projeksi `a.Category`) additive & aman.
**Warning signs:** History tab admin kehilangan archived attempt setelah perubahan.

### Pitfall 4: D-07 invariant tergeser tanpa sadar
**What goes wrong:** Refactor predikat tidak sengaja menyentuh `worker.CompletedAssessments`.
**Why it happens:** Mengira "search assessment" berarti filter per-record.
**How to avoid:** Tambah `[Fact]` invariant yang assert badge count sebelum=sesudah search (lihat Validation Architecture).
**Warning signs:** "Showing N workers" benar tapi badge "Assessment Lulus" per worker berubah.

### Pitfall 5: Null-safety `AssessmentSessions`
**What goes wrong:** NRE saat `w.AssessmentSessions.Any(...)`.
**Why it happens:** Worker tanpa sesi.
**How to avoid:** `AssessmentSessions` di-default `new List<>()` (`WorkerTrainingStatus.cs:51`) + loop selalu assign (`:318-319`,`:350`). Tetap guard `w.AssessmentSessions != null` (mirror `trainingMatch` guard). [VERIFIED]

## Code Examples

### Folded xUnit — assessment-title match di scope "Training" (model: Scope_Training_FiltersByJudul:60-70)
```csharp
// Source: pattern from WorkerDataServiceSearchTests.cs:60-70 (verified)
[Fact]
public async Task Scope_Training_FiltersByAssessmentTitle()
{
    var svc = MakeService(out var ctx);
    ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
    // helper Session(id,user,status,isPassed) sets Title = "Asm " + id; override Title:
    var s = Session(1, "u1", "Completed", true); s.Title = "OJT v14.2 Migas";
    ctx.AssessmentSessions.Add(s);
    await ctx.SaveChangesAsync();
    var result = await svc.GetWorkersInSection("A", search: "ojt v14.2", searchScope: "Training");
    Assert.Single(result);
    Assert.Equal("u1", result[0].WorkerId);
}
```

### Folded xUnit — D-07 badge-count invariant
```csharp
// Source: WorkerTrainingStatus badge fields verified :17,:48
[Fact]
public async Task Search_DoesNotMutate_BadgeCounts_D07()
{
    var svc = MakeService(out var ctx);
    ctx.Users.Add(User("u1", "Budi", "A"));
    var s1 = Session(1, "u1", "Completed", true);  s1.Title = "OJT v14.2";
    var s2 = Session(2, "u1", "Completed", true);  s2.Title = "Lain";   // both passed
    ctx.AssessmentSessions.AddRange(s1, s2);
    ctx.TrainingRecords.Add(Training(1, "u1", "Training X"));
    await ctx.SaveChangesAsync();
    var matched = await svc.GetWorkersInSection("A", search: "ojt", searchScope: "Keduanya");
    Assert.Single(matched);
    // Badge count harus reflect SEMUA sesi worker, bukan hanya yang match search:
    Assert.Equal(2, matched[0].CompletedAssessments); // 2 passed, bukan 1
    Assert.Equal(1, matched[0].TotalTrainings);
}
```

### Folded xUnit — SF-06 export worker-list (previously 0)
```csharp
[Fact]
public async Task Keduanya_AssessmentTitle_ReturnsWorker_ForExport()
{
    var svc = MakeService(out var ctx);
    ctx.Users.AddRange(User("u1", "Budi", "A"), User("u2", "Andi", "A"));
    var s = Session(1, "u1", "Completed", true); s.Title = "OJT v14.2";
    ctx.AssessmentSessions.Add(s);
    await ctx.SaveChangesAsync();
    // Export path memanggil predicate yang sama; sebelum SF-01 ini return 0.
    var ids = (await svc.GetWorkersInSection("A", search: "ojt v14.2", searchScope: "Keduanya"))
              .Select(w => w.WorkerId).ToList();
    Assert.Contains("u1", ids);
}
```

### Playwright UAT — model cmp-records-346.spec.ts:123-149 (Team View search → export href)
```ts
// Source: cmp-records-346.spec.ts:123-149 (verified pattern)
test('Team View — search judul assessment → worker muncul + export href carries scope', async ({ page }) => {
  await loginAny(page, 'manager');           // helpers/accounts.ts (admin/manager/hc available)
  await page.goto('/CMP/Records');
  const teamTab = page.locator('a[href="#pane-team"], #tab-team').first();
  if (await teamTab.count() > 0) await teamTab.click();
  await page.fill('#teamSearch', 'OJT v14.2');     // judul assessment ter-seed (cmp350-seed.sql)
  await page.selectOption('#searchScope', 'Keduanya');
  await page.waitForTimeout(600);                  // debounce + fetch
  await expect(page.locator('#workerCount')).not.toHaveText('0'); // worker pemilik muncul
  const href = await page.locator('#btnExportAssessment').getAttribute('href');
  expect(href).toContain('searchScope=Keduanya');
  expect(href).toContain('search=OJT');
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Search scope hanya Nama/Training (assessment title diabaikan) | Tambah assessment-title ke "Training"/"Keduanya" | Phase 350 (this) | Fix 999.2 |
| Export Assessment kosong saat search judul assessment | Worker-list benar via predicate sama | Phase 350 (D-06) | WYSIWYG export |
| Baris assessment di export tak di-narrow Category | Narrow current + drop archived saat Category aktif | Phase 350 (D-07/SF-06) | Sheet simetris |

**Deprecated/outdated:** Tidak ada. Tidak ada API/library lama yang diganti.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| — | (kosong) | — | — |

**All claims verified or cited — no user confirmation needed.** Semua file:line dibaca langsung di sesi ini. Mekanisme SF-06 (controller vs service) adalah rekomendasi riset dengan rasional; CONTEXT D-07 menyerahkan mekanisme ke Claude's Discretion, jadi bukan asumsi yang butuh konfirmasi — planner bebas memilih, RESEARCH merekomendasikan controller.

## Open Questions

1. **Export Assessment dengan filter Category aktif + archived rows: konfirmasi behavioral expectation di Excel sheet kosong.**
   - What we know: D-07 mengunci "drop archived saat Category aktif". Implementasi: archived `Kategori == null` → ter-drop otomatis.
   - What's unclear: Apakah perlu indikator visual di Excel bahwa archived di-drop (mis. footer note)? CONTEXT tidak meminta — kemungkinan tidak.
   - Recommendation: Jangan tambah note (scope minimal). Cukup dokumentasikan di plan + assert di Playwright/xUnit bahwa archived rows absen saat Category set.

2. **Playwright export-content assertion: cek href saja atau parse XLSX?**
   - What we know: `cmp-records-346.spec.ts` hanya assert export **href** (param propagation), tidak download/parse file. `exceljs` tersedia.
   - What's unclear: Apakah UAT 350 perlu download + parse XLSX untuk verifikasi baris assessment muncul (bukan hanya href)?
   - Recommendation: Minimal = assert href + `#workerCount != 0` (cukup untuk SF-01/SF-02). Opsional stretch = download via `page.waitForEvent('download')` + parse `exceljs` untuk verifikasi baris (SF-06 content). Planner putuskan effort; href-only sudah cukup membuktikan WYSIWYG plumbing.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (dotnet build/test) | xUnit + app build | ✓ (proyek aktif) | net8.0 | — |
| SQL Server lokal (HcPortalDB_Dev, SQLEXPRESS) | Playwright seed/restore + `dotnet run` | ✓ (per CLAUDE.md Dev Workflow) | — | — |
| Node + Playwright | e2e UAT | ✓ | @playwright/test ^1.58.2 | Manual browser UAT via MCP (pola Phase 349) |
| App running localhost:5277 | Playwright precondition | runtime (`dotnet run`) | — | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Playwright runner — jika environment blok (mis. tanpa wifi kantor seperti catatan Phase 311), UAT bisa via Playwright MCP manual (pola Phase 349 UAT 5/5).

## Validation Architecture

`workflow.nyquist_validation: true` [VERIFIED: .planning/config.json] → section wajib.

### Test Framework
| Property | Value |
|----------|-------|
| Framework (unit) | xUnit 2.9.3 + EF Core InMemory 8.0.0 |
| Framework (e2e) | @playwright/test ^1.58.2 |
| Config file (unit) | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Config file (e2e) | `tests/playwright.config.ts` |
| Quick run command (unit) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` |
| Full suite command (unit) | `dotnet test` (105/105 baseline per STATE/Phase 349) |
| e2e run command | `cd tests && npx playwright test cmp-records-350.spec.ts` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SF-01 | Scope "Training" match judul assessment | unit | `dotnet test --filter Scope_Training_FiltersByAssessmentTitle` | ❌ Wave 0 (add) |
| SF-01 | Scope "Keduanya" union assessment | unit | `dotnet test --filter Scope_Keduanya_Union_IncludesAssessment` | ❌ Wave 0 (add) |
| SF-01 | D-07 badge count tidak berubah | unit | `dotnet test --filter Search_DoesNotMutate_BadgeCounts_D07` | ❌ Wave 0 (add) |
| SF-06 | Export worker-list non-empty (search judul assessment) | unit | `dotnet test --filter Keduanya_AssessmentTitle_ReturnsWorker_ForExport` | ❌ Wave 0 (add) |
| SF-06 | Export Assessment narrow Category (drop archived) | unit/manual | controller-level — assert via integration atau Playwright XLSX parse | ❌ Wave 0 (optional) |
| SF-01/02 | UAT: search judul assessment → worker muncul + export href scope | e2e | `npx playwright test cmp-records-350.spec.ts` | ❌ Wave 0 (add) |
| SF-02 | Label dropdown "Judul Kegiatan" + placeholder jujur tampil | e2e | (folded ke spec di atas — assert option text + placeholder) | ❌ Wave 0 (add) |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` (cepat, InMemory).
- **Per wave merge:** `dotnet test` (full xUnit, target tetap green ≥105 + 4 baru).
- **Phase gate:** full `dotnet test` green + `npx playwright test cmp-records-350.spec.ts` green (atau Playwright MCP manual UAT bila runner env-blocked) sebelum `/gsd-verify-work`. + `dotnet run` localhost:5277 manual verify per CLAUDE.md Develop Workflow.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — +4 `[Fact]` (SF-01 ×2, D-07 invariant ×1, SF-06 export-list ×1). File **sudah ada**; tambah method (helper `Session(...)` sudah punya `.Title`).
- [ ] `tests/e2e/cmp-records-350.spec.ts` — NEW (model `cmp-records-346.spec.ts`; reuse `loginAny`, `accounts`, `dbSnapshot`).
- [ ] `tests/sql/cmp350-seed.sql` — NEW (model `cmp346-seed.sql`; seed 1 assessment dengan judul distinct mis. "[PENDING350] OJT v14.2" untuk worker yang section-nya ter-akses oleh `manager`/`hc` login; klasifikasi temporary + local-only; SEED_JOURNAL append; restore afterAll).
- [ ] Framework install: none — xUnit + Playwright sudah terpasang.

*Helper `Session(int,string,string,bool?)` saat ini set `Title="Asm "+id` + `Category=""` (default). Untuk test bermakna: override `.Title`/`.Category` setelah konstruksi (lihat Code Examples), atau tambah overload `Session(id,user,status,isPassed,title,category)`. Pilihan = Claude's Discretion (D-08).*

## Security Domain

`security_enforcement` absent di config → treat as enabled. Namun Phase 350 = perubahan predikat search + micro-copy + export filtering, **tanpa** auth/crypto/session surface baru.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak disentuh |
| V3 Session Management | no | sessionStorage filter = non-sensitive UI state (existing) |
| V4 Access Control | yes (preserve) | Role-level guard existing **dipertahankan**: `roleLevel >= 5 → Forbid()` + L4 section-lock (`CMPController.cs:657-664`, `:709-716`, `:764-765`). Predikat search TIDAK mem-bypass section scope (worker query tetap `u.Section == section`). |
| V5 Input Validation | yes (existing) | `search` di-`.ToLower().Contains()` (parameterized via EF/in-memory LINQ, bukan string concat SQL). Tidak ada injection surface baru. |
| V6 Cryptography | no | Tidak ada |

### Known Threat Patterns for ASP.NET MVC + EF Core
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| SQL injection via search | Tampering | EF Core LINQ (`.Where(...Contains)`) — parameterized; post-load in-memory `.Contains` zero SQL. [VERIFIED: existing pattern] |
| IDOR (export worker lain section) | Elevation | L4 section-lock di controller dipertahankan — predikat tak ubah scope query. |
| Info disclosure via export | Disclosure | Export pre-filter via `GetWorkersInSection(sectionFilter...)` yang sama scope-nya dengan on-screen. |

**Constraint untuk planner:** JANGAN ubah/hapus role-level guard atau section-lock di `ExportRecordsTeam*` / `RecordsTeamPartial`. SF-01/06 hanya memperluas *apa* yang dicari, bukan *siapa* yang boleh lihat.

## Project Constraints (from CLAUDE.md)

| Directive | Implication for Phase 350 |
|-----------|----------------------------|
| Respond Bahasa Indonesia (user-facing) | Micro-copy SF-02 wajib BI; identifier/code English |
| Develop Workflow: verify lokal sebelum commit | `dotnet build` + `dotnet run` localhost:5277 + cek DB lokal + Playwright bila ada — wajib sebelum commit |
| Jangan edit kode/DB langsung di Dev/Prod | Semua test/seed di DB lokal HcPortalDB_Dev saja |
| Promosi Dev/Prod = tanggung jawab IT | Setelah ship lokal, notify IT (commit hash + flag migration). **Phase 350 NO migration** → flag migration = false |
| Seed Workflow: klasifikasi + snapshot + journal + restore | `cmp350-seed.sql` = temporary + local-only; backup sebelum, restore afterAll (sukses/gagal), append SEED_JOURNAL, mark cleaned. Pola sudah di `global.setup.ts`/`cmp-records-346.spec.ts` |
| Bundle v19-v22 NOT PUSHED pending IT | Phase 350 commit menambah ke bundle lokal; jangan push tanpa instruksi user |

## Sources

### Primary (HIGH confidence — read in session)
- `Services/WorkerDataService.cs` — `GetWorkersInSection:242-419` (predicate `:402-417`, Category-union `:373-381`, date-filter `:283-293`, badge counts `:312-370`), `GetAllWorkersHistory:90-220` (archived no-Category `:104-131`, current projection `:134-199`, training category `:217-218`)
- `Controllers/CMPController.cs` — `ExportRecordsTeamAssessment:651-700` (`category:null :677`), `ExportRecordsTeamTraining:703-750` (`category:category :729`), `RecordsTeamPartial:751-788`, list-view caller `:511`
- `Controllers/AssessmentAdminController.cs` — `:273` (caller no-searchScope), `:308` (`GetAllWorkersHistory()` no-arg — regresi-sensitive)
- `Models/AssessmentSession.cs` — `Title:13` (non-null string), `Category:16`
- `Models/AllWorkersHistoryRow.cs` — `Kategori:32` (field exists, unset for assessment)
- `Models/WorkerTrainingStatus.cs` — badge fields `TotalTrainings:17`, `CompletedAssessments:48`, `AssessmentSessions:51`
- `Views/CMP/RecordsTeam.cshtml` — search `:96`, dropdown `:100-104`, hint `:107`, `updateExportLinks:329-346`, `restoreFilterState:305-327`
- `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — harness `:19-45`, scope tests `:49-93`, `Session(...).Title :44`
- `HcPortal.Tests/HcPortal.Tests.csproj` — net8.0, xUnit 2.9.3, EF InMemory 8.0.0
- `tests/e2e/cmp-records-346.spec.ts` — Team View UAT prior-art `:122-149`, SEED_WORKFLOW `:39-62`
- `tests/e2e/global.setup.ts` — snapshot/seed/journal pipeline
- `tests/helpers/accounts.ts` — login fixtures (admin/hc/manager/sectionHead)
- `tests/sql/cmp346-seed.sql` — seed pattern model
- `tests/package.json` — Playwright ^1.58.2, exceljs ^4.4.0
- `.planning/config.json` — `nyquist_validation: true`

### Secondary (project spec — authoritative for design)
- `.planning/phases/350-.../350-CONTEXT.md` — D-01..D-08
- `.planning/phases/350-.../350-UI-SPEC.md` — copywriting contract
- `docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md` — SF-01/02/06 §2, by-design §3
- `.planning/REQUIREMENTS.md`, `.planning/STATE.md`

### Tertiary (LOW confidence)
- None — no WebSearch needed (fully internal codebase phase).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dependency verified in csproj/package.json, no new packages
- Architecture (SF-01 predicate): HIGH — exact diff verified against existing Category-union pattern
- Architecture (SF-06 mechanism): HIGH — controller-level recommendation backed by 3-caller analysis
- Pitfalls: HIGH — derived from code + audit + prior-phase memory
- Validation: HIGH — test harness + e2e prior-art read directly

**Research date:** 2026-06-05
**Valid until:** Stable — internal codebase, no fast-moving deps. Re-verify only if `WorkerDataService.cs`/`CMPController.cs` berubah sebelum plan dieksekusi.
