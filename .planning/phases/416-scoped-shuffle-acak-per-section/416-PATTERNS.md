# Phase 416: Scoped Shuffle (Acak per-Section) - Pattern Map

**Mapped:** 2026-06-23
**Files analyzed:** 8 (2 core refactor + 1 new unit-test + 1 new e2e-test + 4 call-site/view modifications)
**Analogs found:** 8 / 8 (semua punya analog persis di repo — fase 373/375/415)

> **Sifat fase:** 80% reuse / 20% partisi. Bukan fitur baru ber-DB — refactor fungsi-murni `Helpers/ShuffleEngine.cs` + suite test baru. **migration=FALSE.** Algoritma inti `BuildCrossPackageAssignment` DIPERTAHANKAN verbatim sebagai callee per-section. Yang berubah hanya **cakupan input** (kolam global → slice per-Section) + **concat output**.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/ShuffleEngine.cs` | utility (pure engine) | transform (deterministic) | *self* (refactor in-place) | exact (modify) |
| `HcPortal.Tests/SectionScopedShuffleTests.cs` | test (unit, pure) | transform | `HcPortal.Tests/ShuffleEngineTests.cs` | exact |
| `tests/e2e/scoped-shuffle.spec.ts` | test (e2e, Playwright) | request-response (browser) | `tests/e2e/shuffle.spec.ts` | exact |
| `Controllers/CMPController.cs` (StartExam wiring) | controller | request-response | *self* `:1050-1138` | exact (modify) |
| `Controllers/AssessmentAdminController.cs` (Reshuffle* + EagerAssign wiring) | controller | request-response / batch | *self* `:2546-2556 / 6022-6046 / 6102-6160` | exact (modify) |
| `Controllers/AssessmentAdminController.cs` (ET-coverage warning, ManagePackageQuestions GET) | controller | request-response | *self* `:7637-7663` | exact (modify) |
| `Views/Admin/ManagePackageQuestions.cshtml` (ET-warning render) | view (Razor) | request-response | *self* (Section panel render) | role-match (modify) |
| `Helpers/SectionStructureComparer.cs` (REUSE, no change) | utility | transform | *self* (consume only) | reuse-as-is |

> **Anti-pattern locked by RESEARCH (A1 + Anti-Patterns):** JANGAN ubah signature publik `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)`. 3 call-site memanggilnya IDENTIK. Inject logika section DI DALAM entry-point → blast radius minimal. JANGAN ekstrak `IQuestionSequence`/`SectionAwareQuestionProvider` (saran spec §13) di 416 — itu untuk pagination 417.

---

## Pattern Assignments

### `Helpers/ShuffleEngine.cs` (utility, transform) — REFACTOR

**Analog:** *self* — generalisasi `BuildCrossPackageAssignment` jadi callee per-Section. Pertahankan signature entry `BuildQuestionAssignment`.

**Existing structure to preserve** (`ShuffleEngine.cs:39-60` entry + `:91-232` core):
- `BuildQuestionAssignment(...)` — entry, gate ON/OFF, signature WAJIB tetap.
- `BuildCrossPackageAssignment(packages, rng)` — ON-path canonical (Phase 1 1-per-ET, Phase 2 balanced, Phase 3 Fisher-Yates). **Jangan tulis ulang** — jadikan callee per-section slice.
- `BuildOptionShuffle(questions, shuffleOptions, rng)` — `:67-78`, gate per-soal di caller.
- `Shuffle<T>(list, rng)` — `:25-32` Fisher-Yates, reuse apa adanya.

**Fisher-Yates pattern to reuse verbatim** (`ShuffleEngine.cs:25-32`):
```csharp
public static void Shuffle<T>(List<T> list, Random rng)
{
    for (int i = list.Count - 1; i > 0; i--)
    {
        int j = rng.Next(i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}
```

**ET-aware Phase 1/2 core to wrap (NOT rewrite)** (`ShuffleEngine.cs:117-231`):
```csharp
int K = packages.Min(p => p.Questions.Count);          // :117 — per-section: Min atas slice section
if (K == 0) return new List<int>();                     // :118-119 — guard count==0, skip (defensif, OQ#2)
// Phase 1: 1-per-ET (etGroups distinct) — :170-186
// Phase 2: balanced round-robin + fallback "ambil sisa" :213-226 — DIBATASI ke slice section (Pitfall 1)
// Phase 3: Shuffle(selectedList, rng); — :230
```

**Refactor shape (entry partition + concat)** — sketsa RESEARCH Pattern 1 (`416-RESEARCH.md:172-202`):
```csharp
public static List<int> BuildQuestionAssignment(
    List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
{
    if (packages.Count == 0) return new List<int>();
    // partisi key = SectionNumber (null → SectionStructureComparer.KeyOf → LainnyaKey)
    // urut: angka dulu, "Lainnya" TERAKHIR (D-15) — pola SectionStructureComparer:30-33
    //   .OrderBy(k => k == SectionStructureComparer.LainnyaKey).ThenBy(k => k)
    // FOR EACH section (urut) → BuildSectionQuestionAssignment(sectionSlice, sectionShuffle, workerIndex, rng)
    //   sectionShuffle = shuffleQuestions && ResolveSectionShuffle(...)   ← precedence D-14
    // CONCAT: Sec1 → Sec2 → … → "Lainnya"
}
```

**Invariant kritis (golden-order all-null):** bila SEMUA `SectionId=null` → 1 grup tunggal "Lainnya" → sub-fungsi dipanggil SEKALI atas seluruh kolam = panggilan identik `BuildCrossPackageAssignment` lama → output **byte-identik baseline**. JANGAN konsumsi `rng.Next` di luar sub-fungsi untuk jalur all-null (partisi & sort pakai operasi non-RNG deterministik) — Pitfall 2 (RNG stream drift). [`416-RESEARCH.md:146,221,264-268`]

**Precedence gating (D-14)** — RESEARCH Pattern 3 (`416-RESEARCH.md:226-231`):
```csharp
bool sectionShuffleQuestions = parentShuffleQuestions && section.ShuffleEnabled;
bool sectionShuffleOptions   = parentShuffleOptions   && section.ShuffleEnabled; // SATU gate (D-416-01)
// "Lainnya" (null section, no row) → effective = parent flag (D-15)
```

**Pitfall 3 (Section Include):** Baca `q.SectionId` (kolom SKALAR — `AssessmentPackage.cs:99`, SELALU ter-load), BUKAN `q.Section.SectionNumber` (navigasi, butuh `.ThenInclude`). Map `SectionId → SectionNumber + ShuffleEnabled` via lookup dari `AssessmentPackageSection` yang di-load sekali (hindari N+1). Default approach A2 RESEARCH.

---

### `HcPortal.Tests/SectionScopedShuffleTests.cs` (test, unit pure) — NEW

**Analog:** `HcPortal.Tests/ShuffleEngineTests.cs` (Phase 373) — pure, no DB, no fixture, no `[Trait("Category","Integration")]`. Fixed-seed determinism `new Random(42)`.

**Class header + fixed-seed convention to replicate** (`ShuffleEngineTests.cs:10-15`):
```csharp
/// Phase 373 SHUF-04/05/06/07/08 — pure unit tests for ShuffleEngine.
/// No DB, no fixture, no [Trait("Category","Integration")] — the engine is pure so packages
/// are built in-memory. ON-path determinism asserted via fixed seed (new Random(42)).
public class ShuffleEngineTests
```

**In-memory fixture builder to EXTEND** (`ShuffleEngineTests.cs:18-30`) — tambah `sectionNumber` + per-section `ShuffleEnabled`:
```csharp
// existing helper — extend signature to accept (int? sectionNumber) per question + Section rows.
private static AssessmentPackage Pkg(int packageNumber, params (int id, int order, string? et)[] qs)
{
    var p = new AssessmentPackage { PackageNumber = packageNumber, Id = packageNumber };
    foreach (var (id, order, et) in qs)
        p.Questions.Add(new PackageQuestion
        {
            Id = id, Order = order, ElemenTeknis = et,
            Options = { new PackageOption { Id = id * 10 }, new PackageOption { Id = id * 10 + 1 } }
        });
    return p;
}
```
> Extend: terima `sectionNumber` per soal → set `q.SectionId` + buat `AssessmentPackageSection { SectionNumber, ShuffleEnabled }` (model `AssessmentPackage.cs:34-57`). Wire `q.Section` nav untuk pure-test (no DB → harus set objek langsung).

**Seed-stable determinism assertion pattern** (`ShuffleEngineTests.cs:120-126`):
```csharp
var a = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
var b = ShuffleEngine.BuildQuestionAssignment(packages, true, 0, new Random(42));
Assert.Equal(a, b);                                          // seed-stable
Assert.Equal(new HashSet<int> { 10, 11, 12, 13 }, a.ToHashSet()); // contains ALL ids
```

**K-min sampling assertion pattern** (`ShuffleEngineTests.cs:128-140`):
```csharp
var p1 = Pkg(1, (10, 1, "ET-A"), (11, 2, "ET-B"), (12, 3, "ET-A"));
var p2 = Pkg(2, (20, 1, "ET-A"), (21, 2, "ET-B"));   // K=min=2
Assert.Equal(2, a.Count);  // K = min(3,2) = 2 (sampling, not full)
```

**Golden-order regression pattern (NEW, D-416-05 #1)** — RESEARCH Pattern 2 (`416-RESEARCH.md:210-219`):
```csharp
[Fact]
public void AllNullSection_ProducesIdenticalOrderToLegacyBaseline()
{
    var packages = BuildAllNullFixture();                  // semua SectionId=null, ET bervariasi, ≥2 paket
    var baseline = LegacyBuildCrossPackageAssignment(Clone(packages), new Random(42));
    var actual   = ShuffleEngine.BuildQuestionAssignment(Clone(packages), true, 0, new Random(42));
    Assert.Equal(baseline, actual);                        // byte-identik (RNG sequence sama)
}
```
> `LegacyBuildCrossPackageAssignment` = bekukan output core lama (snapshot list literal sekali, atau capture sebelum refactor). Wave-0 gap RESEARCH `:440`.

**Test method map (RESEARCH Validation Architecture `:418-429`)** — planner WAJIB cover semua:
- `AllNullSection_ProducesIdenticalOrderToLegacyBaseline` (SHF-01 golden-order)
- `ScopedShuffle_NoCrossSectionLeak` (SHF-01 — assert tiap ID ∈ section-nya)
- `SectionOrder_LainnyaAlwaysLast` (SHF-01, D-15)
- `Precedence_ParentOff_AllOrdered` (SHF-02, D-14)
- `Precedence_ParentOn_PerSectionToggle` (SHF-02)
- `OptionShuffle_GatedPerSection` (SHF-02, D-416-01)
- `MultiPackage_EtCoveragePerSection` (SHF-03)
- `EtSpanningSections_CoveredIndependently` (SHF-03)
- `Determinism_WorkerIndexStable` (SHF-03)
- `Reshuffle_SectionIsolation` (SHF-04)

> **Pitfall 6 (kontrak golden-order):** `ShuffleEngineTests.cs` (~180 metode Phase 373) WAJIB tetap hijau. Bila merah → jalur all-null SALAH, perbaiki ENGINE bukan tes. Spec §12: JANGAN retrofit tes lama; suite BARU hanya skenario ber-section. [`416-RESEARCH.md:288-292`]

---

### `tests/e2e/scoped-shuffle.spec.ts` (test, Playwright e2e) — NEW

**Analog:** `tests/e2e/shuffle.spec.ts` (Phase 375).

**Imports + serial mode + DB snapshot lifecycle** (`shuffle.spec.ts:26-41, 93-111`):
```typescript
import { test, expect, type Page } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';
import { createAssessmentViaWizard, createDefaultPackage, addQuestionViaForm } from './helpers/examTypes';

test.describe.configure({ mode: 'serial' });

test.beforeAll(async () => {
    const dir = (await db.queryString(
        "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre416-${ts}.bak`;
    await db.backup(snapshotPath);
});
test.afterAll(async () => { /* db.restore(snapshotPath) + unlink — pola :102-111 */ });
```

**SQL state-injection helper** (`shuffle.spec.ts:45-47`):
```typescript
async function execSql(sql: string): Promise<number> {
    return db.queryScalar(`${sql}; SELECT @@ROWCOUNT;`);
}
```

**Wizard-create + arrive ManagePackages** (`shuffle.spec.ts:52-71`) — reuse verbatim (auth `admin@pertamina.com / 123456`, `futureDate()` `:33-37`).

**Conventions WAJIB:** `--workers=1` (DB isolation, `playwright.config fullyParallel:false`), seed=temporary+local-only, BACKUP→RESTORE `HcPortalDB_Dev` (CLAUDE.md SEED_WORKFLOW). UAT inti (D-416-05 #4): assessment ber-section → soal teracak **DALAM** section, tak lompat antar-section; + bandingkan distribusi peserta AddParticipantsLive vs awal (Pitfall 5).

---

### `Controllers/CMPController.cs` (controller, request-response) — StartExam wiring

**Analog:** *self* `:1050-1138`. Wiring call DIPERTAHANKAN — hanya pastikan section-data ter-load.

**Wiring pattern (unchanged shape)** (`CMPController.cs:1130-1138`):
```csharp
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
    packages, assessment.ShuffleQuestions, workerIndex, rng);   // signature DIPERTAHANKAN
var assignedQuestions = packages.SelectMany(p => p.Questions)
    .Where(q => shuffledIds.Contains(q.Id));
var optionShuffleDict = ShuffleEngine.BuildOptionShuffle(
    assignedQuestions, assessment.ShuffleOptions, rng);          // 416: gate per-section di dalam
```

**Load block — TAMBAH section access** (`CMPController.cs:1050-1055`, assignment block BELUM include Section):
```csharp
var packages = await _context.AssessmentPackages
    .Include(p => p.Questions)
        .ThenInclude(q => q.Options)
    // 416: Opsi A .ThenInclude(q => q.Section) | Opsi B (preferred) load sections map SectionId→(SectionNumber,ShuffleEnabled)
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
    .OrderBy(p => p.PackageNumber)
    .ToListAsync();
```

**workerIndex determinism (invariant, JANGAN ubah)** (`CMPController.cs:1047-1048`):
```csharp
var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();
int workerIndex = sortedSiblingIds.IndexOf(id);
```

> 415 SEC-04 re-guard (`CMPController.cs:1069-1123`) sudah `.ThenInclude(q => q.Section)` di guard block (`:1086`) + reuse `SectionStructureComparer`. 416 TIDAK ubah guard — assignment hanya jalan setelah guard lolos.

---

### `Controllers/AssessmentAdminController.cs` (controller) — Reshuffle* + EagerAssign wiring

**Analog:** *self* — 3 call-site IDENTIK dengan StartExam. **WAJIB seragam** (Pitfall 5: jangan lupa EagerAssign — di AssessmentAdmin, bukan CMP).

**Call-site 1 — `CreateEagerAssignmentsAsync`** (`:2546-2556`, AddParticipantsLive eager):
```csharp
var packages = await _context.AssessmentPackages
    .Include(p => p.Questions).ThenInclude(q => q.Options)   // 416: + section access
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
    .OrderBy(p => p.PackageNumber).ToListAsync();
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(packages, s.ShuffleQuestions, workerIndex, rng);
var assignedQuestions = packages.SelectMany(p => p.Questions).Where(q => shuffledIds.Contains(q.Id));
var optionShuffleDict = ShuffleEngine.BuildOptionShuffle(assignedQuestions, s.ShuffleOptions, rng);
```

**Call-site 2 — `ReshufflePackage`** (`:6022-6046`) + **Call-site 3 — `ReshuffleAll`** (`:6102-6160`): pola load + wire IDENTIK. `ReshuffleAll` ber-loop per-session dengan `sessionPackages` slice type-aware (`:6149-6160`).

**workerIndex parity (3 call-site — invariant)**:
- EagerAssign `:2544`: `siblingSessionIds.OrderBy(x => x).ToList().IndexOf(s.Id)`
- ReshufflePackage `:6042-6043`: `sortedSiblingIds.IndexOf(sessionId)`
- ReshuffleAll `:6146-6148`: `typeSiblingIds.IndexOf(session.Id)` (type-aware grouping `:6098-6101`)

> **Checklist plan (Pitfall 5):** ketiga call-site WAJIB pakai jalur load + engine yang SAMA → SHF-04 + AddParticipantsLive drift-free OTOMATIS karena entry signature dipertahankan. Auth sudah `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (`:5990-5992`) — tak berubah.

---

### `Controllers/AssessmentAdminController.cs` + `Views/Admin/ManagePackageQuestions.cshtml` — ET-coverage warning (D-416-03)

**Analog:** *self* — `ManagePackageQuestions` GET `:7637-7663` (sudah load `pkg.Questions` + `sections`).

**Existing GET to extend** (`AssessmentAdminController.cs:7637-7663`):
```csharp
var sections = await _context.AssessmentPackageSections
    .Where(s => s.AssessmentPackageId == packageId)
    .OrderBy(s => s.SectionNumber)
    .ToListAsync();
ViewBag.Sections = sections;
// 416 D-416-03: tambah ViewBag.SectionEtWarnings (non-blocking, sinyal)
```

**ET-warning computation (NEW)** — RESEARCH `:333-343`:
```csharp
ViewBag.SectionEtWarnings = sections.Select(s => {
    var qs = pkg.Questions.Where(q => q.SectionId == s.Id).ToList();
    int k = qs.Count;
    int distinctEt = qs.Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                       .Select(q => q.ElemenTeknis!).Distinct().Count();
    return new { s.SectionNumber, s.Name, K = k, DistinctEt = distinctEt, Warn = distinctEt > k };
}).Where(x => x.Warn).ToList();
```

**View render target** (`ManagePackageQuestions.cshtml:8-11`) — sudah konsumsi `ViewBag.Sections`, render alert per-section di panel Section. Teks BI (CLAUDE.md): "Section {n}: {distinctEt} Elemen Teknis tapi hanya {K} soal — sebagian ET tak terjamin muncul." **Non-blocking**: D-416-03 TOLAK blokir mulai ujian.

---

## Shared Patterns

### "Lainnya" null-safe key (REUSE — JANGAN re-introduce null-key 500)
**Source:** `Helpers/SectionStructureComparer.cs:18-22, 30-33`
**Apply to:** ShuffleEngine partition + any section grouping
```csharp
public const int LainnyaKey = int.MinValue;
public static int KeyOf(int? sectionNumber) => sectionNumber ?? LainnyaKey;
// "Lainnya" selalu terakhir (D-15):
//   .OrderBy(k => k == LainnyaKey).ThenBy(k => k)
```
> Bug laten null-key (`Dictionary<int?,int>` lempar saat key null → 500) sudah di-fix 415 (escalation validate-phase). JANGAN bangun `Dictionary<int?,...>` baru. [RESEARCH Don't Hand-Roll `:248`]

### Fixed-seed determinism (test)
**Source:** `ShuffleEngineTests.cs:13` ("ON-path determinism asserted via fixed seed `new Random(42)`")
**Apply to:** semua unit test ShuffleEngine baru
```csharp
new Random(42)   // test-time determinism; produksi pakai Random.Shared (non-seeded, by design)
```
> Golden-order = HANYA test-time invariant. Produksi (`Random.Shared`, `CMPController.cs:1125`) tetap acak antar peserta. [RESEARCH `:267`]

### Section-aware in-memory fixture (DB-backed test)
**Source:** `HcPortal.Tests/SectionFixture.cs` (disposable SQLEXPRESS `HcPortalDB_Test_{guid}` + `MigrateAsync`) + `SectionMismatchGuardTests.AddPackageWithSectionsAsync` (`:172-201`)
**Apply to:** integration test `EagerAssign_SectionAwareParity` (bila butuh real-SQL) ATAU adaptasi pola seed Section ke fixture in-memory pure
```csharp
// AddPackageWithSectionsAsync(ctx, sessionId, pkgNumber, dist) — dist: (int? sectionNumber, int count)[]
//   buat AssessmentPackageSection row utk key non-null + set q.SectionId
//   null sectionNumber → "Lainnya" (SectionId tetap null)
```
> Engine unit-test TIDAK butuh DB (pure). Integration parity test boleh pakai `SectionFixture` (SQLEXPRESS) atau `EntityFrameworkCore.InMemory` (sudah tersedia, csproj `:12`).

### Antiforgery + role auth (existing, tak berubah)
**Source:** `AssessmentAdminController.cs:5990-5992`
**Apply to:** Reshuffle endpoints (sudah ada, 416 tak tambah endpoint)
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```

---

## No Analog Found

*(Tidak ada — semua file 416 punya analog persis di repo.)*

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| — | — | — | Semua kebutuhan tercover analog fase 373 (engine+test), 375 (e2e), 415 (Section fixture+comparer+guard). |

---

## Metadata

**Analog search scope:** `Helpers/`, `HcPortal.Tests/`, `tests/e2e/`, `tests/helpers/`, `Controllers/`, `Views/Admin/`, `Models/`
**Files scanned:** 14 (ShuffleEngine.cs, ShuffleEngineTests.cs, SectionStructureComparer.cs, SectionFixture.cs, SectionMismatchGuardTests.cs, shuffle.spec.ts, AssessmentPackage.cs, CMPController.cs StartExam block, AssessmentAdminController.cs 4 call-sites + GET, ManagePackageQuestions.cshtml, test/e2e directory listings)
**Pattern extraction date:** 2026-06-23
**migration:** FALSE (reuse `AssessmentPackageSection.ShuffleEnabled` dari Phase 415)
