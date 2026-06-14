# Phase 373: Shuffle Engine (read logic + reshuffle) - Research

**Researched:** 2026-06-13
**Domain:** ASP.NET Core MVC exam engine — pure-core extraction + deterministic question/option distribution (.NET 8, EF Core 8, xUnit)
**Confidence:** HIGH (semua temuan diverifikasi langsung di kode live session ini)

<user_constraints>
## User Constraints (from 373-CONTEXT.md)

### Locked Decisions (research HOW, not WHETHER)
- **D-01:** Ekstrak SATU shared pure core (nama/lokasi = diskresi planner, asal pure & testable tanpa DB) berisi (a) distribusi soal per flag `ShuffleQuestions`, (b) option-shuffle per flag `ShuffleOptions`. Dipakai `StartExam` (CMPController) + `ReshufflePackage` + `ReshuffleAll` (AssessmentAdminController). HAPUS duplikasi `BuildCrossPackageAssignment`. ON-path dipertahankan verbatim.
- **D-01a:** Jalur ON WAJIB dipertahankan verbatim (SC#1). Core terima `Random` param (sudah begitu); OFF tak butuh rng.
- **D-02:** OFF + ≥2 paket = "filter dulu, baru modulo": daftar paket-ber-soal (`Questions.Count > 0`, `OrderBy(PackageNumber)`); index worker = posisi sibling session `OrderBy(s => s.Id)`; paket = `daftarBerSoal[index % daftarBerSoal.Count]`; isi urut `q.Order`. Guard paket kosong SEBELUM modulo. JANGAN `assignmentCount % n` / "urutan buka".
- **D-02a:** Peserta baru (`Id` lebih besar) di-append di akhir → tak menggeser worker lama.
- **D-03:** OFF multi-paket, `currentQuestionCount` = jumlah soal paket worker itu; rekomputasi deterministik → guard `SavedQuestionCount` tak salah-trigger. Pertahankan mekanisme guard existing.
- **D-04:** `ReshufflePackage` + `ReshuffleAll` rebuild via core, hormati KEDUA flag (OFF question = deterministic rebuild idempotent; ShuffleOptions ON = build optionShuffleDict, FIX hard-code `"{}"`; OFF = `"{}"`).
- **D-04a:** Pertahankan guard "hanya Not started/Abandoned" — tidak dilonggarkan ke InProgress.
- **D-05:** OFF + ≥2 paket = tiap worker dapat paket UTUH (seluruh soal, urut `q.Order`), TIDAK dipotong ke K-min (beda dari ON ≥2 yang sampling K). Warning §9 = Phase 374.
- **D-06:** Acak Pilihan independen penuh. ON → optionShuffleDict per soal; OFF → `"{}"` → view fallback DB order via `ViewBag.OptionShuffle`.
- **D-06a (grading safety):** Grading pakai `PackageOption.Id`. `GetShuffledQuestionIds()` dipakai grading di KEDUA mode → jalur grading tak berubah.
- **D-07:** Fix komentar stale `CMPController.cs:1054`.

### Claude's Discretion
- Lokasi & nama exact shared core + signature method (input `List<AssessmentPackage>` atau DTO + flag + worker index + `Random`).
- Bentuk DTO/abstraksi input core agar pure.
- Pembagian test: sebagian unit core di 373 (Wave 0 self-check ekstraksi) ATAU ditahan penuh ke Phase 375 = keputusan planner.

### Deferred Ideas (OUT OF SCOPE — Phase 373)
- UI warning §9 (render) → Phase 374.
- UI ManagePackages toggle + endpoint `UpdateShuffleSettings` + lock + reminder Pre/Post + hide Proton/Manual → Phase 374.
- xUnit mode-matrix penuh + Playwright UAT → Phase 375.
- Memindah setting `SamePackage` → out of scope (spec §12).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SHUF-04 | Acak Soal ON = perilaku existing (1 paket acak; ≥2 sampling K + ET-balanced + acak) | ON-path = `BuildCrossPackageAssignment` existing (CMPController:1230). Pindah verbatim ke core. §"ON-Path Algorithm" di bawah. |
| SHUF-05 | Acak Soal OFF + 1 paket = semua soal urut `q.Order` (semua peserta identik) | Baru. `packages[0].Questions.OrderBy(q=>q.Order).Select(q=>q.Id)` tanpa Fisher-Yates. §"OFF-Path Spec". |
| SHUF-06 | Acak Soal OFF + ≥2 paket = 1 paket utuh/worker, round-robin index-session-stabil, urut `q.Order`, guard paket kosong | Baru. Algoritma D-02 lengkap di §"OFF-Path Spec" + §"Worker Index Resolution". |
| SHUF-07 | Acak Pilihan independen — ON `optionShuffleDict`, OFF `"{}"` (view fallback DB) | Mekanisme option-shuffle existing (CMPController:982-989 build, :1144-1157 parse, view :126-162 fallback). §"Option Shuffle Path". |
| SHUF-08 | Resume stale-count guard deterministik mode OFF (rekomputasi konsisten) | Guard existing CMPController:1027-1040. §"Resume Stale-Count Guard". Determinisme dari D-02. |
| SHUF-09 | `ReshufflePackage`/`ReshuffleAll` hormati KEDUA flag (fix bug hard-code opsi `"{}"`) | Bug nyata di AssessmentAdminController:5119 & :5213. §"Reshuffle Endpoints". |
| SHUF-15 | Cleanup komentar stale `CMPController.cs:1054` | Komentar 1-baris. §"Cleanup". |
</phase_requirements>

## Summary

Phase 373 adalah **refactor + read-logic engine**, bukan fitur baru dari nol. Fondasi data (kolom `ShuffleQuestions`/`ShuffleOptions` di `AssessmentSession` + propagasi sibling) sudah live dari Phase 372. Phase ini menambahkan **gerbang flag** di 3 call-site (`StartExam`, `ReshufflePackage`, `ReshuffleAll`) dan mengekstrak satu **pure static core** yang menggantikan 2 salinan `BuildCrossPackageAssignment`.

**Temuan kritis (mengubah strategi dedup):** Dua salinan `BuildCrossPackageAssignment` **TIDAK verbatim identik** seperti diasumsikan CONTEXT D-01. Single-package branch + fallback slot-list + Phase 1 (one-per-ET) identik, tapi **Phase 2 berbeda algoritma**: CMPController (`:1318-1357`) pakai **round-robin per-ElemenTeknis** (`basePerET = remaining / M_etGroups`), sedangkan AssessmentAdminController (`:5338-5388`) pakai **slot-distribution per-package** (`baseCount = remaining / N_packages`) + redistribusi paket-habis. Karena spec D-01a + SC#1 mengunci "perilaku StartExam tidak berubah", **versi CMPController = canonical** yang harus dipindah ke core. Reshuffle akan berubah perilaku ON-nya (menyatu ke versi CMPController) — ini benar dan diinginkan (konsistensi reshuffle↔StartExam), tapi planner WAJIB sadar ini bukan "no-op move".

Entitas (`AssessmentPackage`/`PackageQuestion`/`PackageOption`) adalah **POCO murni** (navigation = `ICollection`, instantiable tanpa DB). Maka core dapat menerima `List<AssessmentPackage>` langsung dan diuji unit dengan objek in-memory — **tidak butuh DTO**, konsisten dengan signature existing `BuildCrossPackageAssignment(List<AssessmentPackage>, Random)`. Pola helper-statis project (`Helpers/ImageFileCleanup`, `Models/QuestionTypeLabels`) jadi cetakan.

**Primary recommendation:** Buat `Helpers/ShuffleEngine.cs` (static class) berisi 3 method pure: `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` (gabung ON-path canonical CMPController + OFF-path D-02/D-05), `BuildOptionShuffle(questions, shuffleOptions, rng)` (ON dict / OFF empty), dan helper `Shuffle<T>` (pindah dari 2 controller). 3 call-site delegasi ke core. Tulis Wave-0 unit test (`ShuffleEngineTests.cs`, pure `[Theory]`/`[Fact]` tanpa DB) untuk self-check ekstraksi (determinisme OFF, guard paket kosong, ON-seed-stabil) — DIREKOMENDASIKAN di 373 (bukan ditahan ke 375), karena core tak akan ada lagi setelah 374/375 menyentuh file yang sama; menguji saat ekstraksi = Nyquist sampling yang benar.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Distribusi soal per flag (ON/OFF, 1/≥2 paket) | API/Backend (pure helper `ShuffleEngine`) | — | Logika murni, in-process, tanpa I/O. Dipanggil controller; testable tanpa DB. |
| Option-shuffle per flag | API/Backend (pure helper) | — | Sama; dict dibangun in-memory, di-serialize JSON. |
| Worker index resolution (posisi sibling `OrderBy(Id)`) | API/Backend (controller — perlu query EF) | Pure helper (terima index sbg param) | Query sibling = DB-bound (tetap di controller); core terima index sbg param agar tetap pure. |
| Baca flag dari session peserta | API/Backend (controller — `assessment.ShuffleQuestions`) | — | Flag sudah di-load (`StartExam:862`). Reshuffle load dari `assessment`/`session`. |
| Persist assignment (`ShuffledQuestionIds`, `ShuffledOptionIdsPerQuestion`) | Database/Storage (EF Core) | — | Tidak berubah; core hanya menghasilkan `List<int>` + dict. |
| Render opsi sesuai shuffle | Browser/View (Razor `StartExam.cshtml`) | — | View baca `ViewBag.OptionShuffle`; OFF `"{}"` → fallback urutan VM (DB order). Tidak diubah Phase 373. |
| Grading by `PackageOption.Id` | API/Backend (`GradingService`) | — | Pakai `GetShuffledQuestionIds()` di kedua mode → tak berubah (D-06a). |

## Standard Stack

Tidak ada library baru. Semua in-house / sudah ada.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK | 8.0.418 | Runtime/build | [VERIFIED: `dotnet --version` session ini] target `net8.0` di csproj. |
| EF Core | 8.0.0 | Query paket/sibling (tetap di controller) | [VERIFIED: HcPortal.Tests.csproj] migration=false; tak ada perubahan skema. |
| xUnit | 2.9.3 | Unit/integration test | [VERIFIED: HcPortal.Tests.csproj] framework test project. |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Real-SQL integration (Phase 372 fixture) | [VERIFIED: csproj] Hanya untuk test integration; core unit test TIDAK butuh ini. |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.Text.Json` (BCL) | net8.0 | Serialize `ShuffledQuestionIds`/`ShuffledOptionIdsPerQuestion` | Sudah dipakai di StartExam/reshuffle (verbatim). |
| `Random.Shared` (BCL) | net8.0 | RNG untuk ON-path | Dipakai di 3 call-site existing; core terima `Random` sbg param (deterministik via seed di test). |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `List<AssessmentPackage>` langsung sbg input core | DTO ringkas (`record PackageDto(int Id, int PackageNumber, List<QuestionDto>)`) | DTO lebih "murni" konseptual TAPI menambah mapping boilerplate di 3 call-site + duplikasi shape. POCO sudah instantiable tanpa DB → DTO tidak diperlukan. **Rekomendasi: pakai `List<AssessmentPackage>` langsung** (konsisten signature existing, zero mapping). [ASSUMED→diskresi planner] |
| `Helpers/ShuffleEngine.cs` static | `Services/ShuffleEngine` (DI service) | Service = overkill untuk fungsi murni stateless tanpa dependency. Pola project untuk logika murni = static helper (`ImageFileCleanup`, `QuestionTypeLabels`). **Rekomendasi: static class di `Helpers/`.** |

**Installation:** Tidak ada. `dotnet build` + `dotnet test` cukup.

## Live Line-Number Map (re-grep VERIFIED session ini — masih bisa drift di execute-time)

> File-overlap v25.0: `CMPController.cs` + `AssessmentAdminController.cs` area sibuk (367/368). Line numbers di bawah diverifikasi **2026-06-13 session ini**; executor WAJIB re-grep anchor (bukan line number) saat execute.

### CMPController.cs
| Landmark | Line (verified) | Anchor string (untuk re-grep) |
|----------|----------------|-------------------------------|
| `StartExam` entry | 860 | `public async Task<IActionResult> StartExam(int id)` |
| Sibling session lookup | 949-954 | `var siblingSessionIds = await _context.AssessmentSessions` |
| Packages query `OrderBy(PackageNumber)` | 956-961 | `.OrderBy(p => p.PackageNumber)` (dalam StartExam) |
| Assignment null check (build branch start) | 973 | `if (assignment == null)` |
| Build ON: `BuildCrossPackageAssignment(packages, rng)` | 978 | `var shuffledIds = BuildCrossPackageAssignment(packages, rng)` |
| Build optionShuffleDict | 982-989 | `var optionShuffleDict = new Dictionary<int, List<int>>()` |
| `SavedQuestionCount = shuffledIds.Count` | 1003 | `assignment.SavedQuestionCount = shuffledIds.Count` |
| `currentQuestionCount` (resume) | 1023 | `int currentQuestionCount = assignment.GetShuffledQuestionIds().Count` |
| Stale-count guard | 1027-1040 | `assignment.SavedQuestionCount.Value != currentQuestionCount` |
| **Stale comment (D-07/SHUF-15)** | 1054 | `// Options in original DB order — option shuffle removed per user decision` |
| Opsi base DB-order build (VM) | 1055-1061 | `var opts = q.Options.OrderBy(o => o.Id).Select(...)` |
| ViewBag.OptionShuffle parse | 1144-1157 | `var optionShuffleRaw = assignment?.ShuffledOptionIdsPerQuestion ?? "{}"` |
| `Shuffle<T>` helper | 1211-1219 | `private static void Shuffle<T>(List<T> list, Random rng)` |
| `BuildCrossPackageAssignment` (CANONICAL ON-path) | 1230-1362 | `private static List<int> BuildCrossPackageAssignment(List<AssessmentPackage> packages, Random rng)` |

### AssessmentAdminController.cs
| Landmark | Line (verified) | Anchor string |
|----------|----------------|---------------|
| `ReshufflePackage` entry | 5065 | `public async Task<IActionResult> ReshufflePackage(int sessionId)` |
| Reshuffle guard "Not started/Abandoned" | 5083-5084 | `if (userStatus != "Not started" && userStatus != "Abandoned")` |
| Packages query `OrderBy(PackageNumber)` | 5093-5098 | `.OrderBy(p => p.PackageNumber)` (dalam ReshufflePackage) |
| `BuildCrossPackageAssignment` call | 5110 | `var shuffledIds = BuildCrossPackageAssignment(packages, rng)` |
| **Hard-code `"{}"` (BUG, D-04)** | 5119 | `ShuffledOptionIdsPerQuestion = "{}"` (dalam ReshufflePackage) |
| `ReshuffleAll` entry | 5146 | `public async Task<IActionResult> ReshuffleAll(string title, string category, DateTime scheduleDate)` |
| ReshuffleAll guard "Not started" | 5191 | `if (userStatus != "Not started")` |
| `BuildCrossPackageAssignment` call (loop) | 5201 | `var sessionShuffledIds = BuildCrossPackageAssignment(packages, rng)` |
| **Hard-code `"{}"` (BUG, D-04)** | 5213 | `ShuffledOptionIdsPerQuestion = "{}"` (dalam ReshuffleAll) |
| `Shuffle<T>` helper (DUPLIKAT) | 5241-5248 | `private static void Shuffle<T>(List<T> list, Random rng)` |
| `BuildCrossPackageAssignment` (DUPLIKAT — Phase 2 BEDA) | 5250-5393 | `private static List<int> BuildCrossPackageAssignment(List<AssessmentPackage> packages, Random rng)` |

### View / Model
| Landmark | File:Line | Note |
|----------|-----------|------|
| Option-shuffle view fallback (MA) | `Views/CMP/StartExam.cshtml:126-129` | `optShuffle.ContainsKey(q.QuestionId) ? ... : q.Options.ToList()` |
| Option-shuffle view fallback (MC) | `Views/CMP/StartExam.cshtml:159-162` | Sama; fallback = `q.Options.ToList()` = urutan VM = DB order. |
| `ShuffleQuestions`/`ShuffleOptions` kolom | `Models/AssessmentSession.cs:38-42` | live, default `true`. |
| `GetShuffledQuestionIds()` / `GetShuffledOptionIds()` | `Models/UserPackageAssignment.cs:60-94` | dipakai grading + view; tak berubah. |
| Grading pakai `GetShuffledQuestionIds()` | `Services/GradingService.cs:70, 339` | D-06a aman. |

## Architecture Patterns

### System Data-Flow Diagram

```
                          ┌─────────────────────────────────────────────┐
  Peserta buka ujian ───► │ CMPController.StartExam (:860)               │
                          │  1. load assessment (flag ShuffleQuestions/  │
                          │     ShuffleOptions sudah ter-propagate 372)  │
                          │  2. query sibling sessions (:949)            │
                          │  3. query packages OrderBy(PackageNumber)    │
                          │  4. assignment == null? ──► build branch     │
                          └───────────────┬─────────────────────────────┘
                                          │ workerIndex = sortedSiblingIds.IndexOf(id)
                                          │ flags + packages + rng
                                          ▼
                          ┌─────────────────────────────────────────────┐
   HC klik Reshuffle ───► │   ShuffleEngine (Helpers/ — PURE, NO DB)    │ ◄─── HC klik
   (single)               │                                             │      ReshuffleAll
   AssessmentAdmin        │  BuildQuestionAssignment(                   │      (loop per session)
   .ReshufflePackage      │     packages, shuffleQuestions,             │
   (:5065)                │     workerIndex, rng) ──► List<int>         │
                          │   ├─ ON + 1pkg  → Fisher-Yates q.Order      │
                          │   ├─ ON + ≥2pkg → ET-aware sampling K       │
                          │   │              (CANONICAL = CMPController) │
                          │   ├─ OFF + 1pkg → q.Order (no shuffle)      │
                          │   └─ OFF + ≥2pkg→ filter-then-modulo:       │
                          │        pkgWithQ = pkgs.Where(Q>0)           │
                          │                       .OrderBy(PackageNumber)│
                          │        chosen = pkgWithQ[wIdx % count]      │
                          │        ids = chosen.Q.OrderBy(Order)        │
                          │                                             │
                          │  BuildOptionShuffle(                        │
                          │     questions, shuffleOptions, rng)         │
                          │   ├─ ON  → dict{qId: shuffled optIds}       │
                          │   └─ OFF → empty dict (→ serialize "{}")    │
                          └───────────────┬─────────────────────────────┘
                                          │ List<int> + Dictionary
                                          ▼
                          ┌─────────────────────────────────────────────┐
                          │ Controller: persist UserPackageAssignment    │
                          │  ShuffledQuestionIds = JSON(ids)             │
                          │  ShuffledOptionIdsPerQuestion = JSON(dict)   │
                          │  SavedQuestionCount = ids.Count              │
                          └───────────────┬─────────────────────────────┘
                                          ▼
            ┌──────────────────────────┐     ┌────────────────────────────┐
            │ StartExam.cshtml render  │     │ GradingService (by Opt.Id) │
            │  ViewBag.OptionShuffle   │     │  GetShuffledQuestionIds()  │
            │  OFF "{}" → DB order     │     │  unchanged (D-06a)         │
            └──────────────────────────┘     └────────────────────────────┘
```

### Recommended Structure (file baru + edit)
```
Helpers/
└── ShuffleEngine.cs          # BARU — static class, pure, no DB. Host BuildQuestionAssignment +
                              #   BuildOptionShuffle + Shuffle<T>. Pindahan canonical BuildCrossPackageAssignment.
Controllers/
├── CMPController.cs          # EDIT — StartExam delegasi ke core; hapus BuildCrossPackageAssignment +
                              #   Shuffle<T> lokal; fix komentar :1054; tambah workerIndex resolution.
└── AssessmentAdminController.cs  # EDIT — ReshufflePackage + ReshuffleAll delegasi; hapus duplikat
                              #   BuildCrossPackageAssignment + Shuffle<T>; fix hard-code "{}" → BuildOptionShuffle.
HcPortal.Tests/
└── ShuffleEngineTests.cs     # BARU (Wave 0) — pure unit test, no DB, no Trait("Integration").
```

### Pattern 1: Pure static helper extracted from controller (project-standard)
**What:** Logika murni stateless dipindah ke `static class` di `Helpers/` (atau `Models/` untuk label). Controller delegasi.
**When to use:** Logika in-process tanpa I/O yang perlu dedup + testable. Persis kasus ShuffleEngine.
**Example (cetakan `ImageFileCleanup` — diadaptasi murni in-memory):**
```csharp
// Source: Helpers/ImageFileCleanup.cs (project) — pola static helper extracted
namespace HcPortal.Helpers
{
    public static class ShuffleEngine
    {
        // Fisher-Yates (pindahan dari CMPController:1212 + AssessmentAdminController:5241)
        public static void Shuffle<T>(List<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// ON-path = CANONICAL pindahan verbatim dari CMPController.BuildCrossPackageAssignment (:1230-1362).
        /// OFF-path = D-02/D-05 (filter-then-modulo, paket utuh urut q.Order).
        public static List<int> BuildQuestionAssignment(
            List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
        {
            if (packages.Count == 0) return new List<int>();

            if (shuffleQuestions)
                return BuildCrossPackageAssignment(packages, rng);   // verbatim canonical

            // ---- OFF ----
            if (packages.Count == 1)
            {
                var q = packages[0].Questions;
                if (q == null || q.Count == 0) return new List<int>();
                return q.OrderBy(x => x.Order).Select(x => x.Id).ToList();   // SHUF-05: urut, no shuffle
            }
            // OFF + ≥2: filter-then-modulo (D-02), paket utuh (D-05)
            var packagesWithQuestions = packages
                .Where(p => p.Questions != null && p.Questions.Count > 0)   // D-02b guard SEBELUM modulo
                .OrderBy(p => p.PackageNumber)
                .ToList();
            if (packagesWithQuestions.Count == 0) return new List<int>();
            var chosen = packagesWithQuestions[workerIndex % packagesWithQuestions.Count];
            return chosen.Questions.OrderBy(x => x.Order).Select(x => x.Id).ToList();  // SHUF-06 utuh
        }
    }
}
```
> CATATAN: kode di atas adalah **bentuk yang direkomendasikan**, bukan diktat — planner final-kan signature. ON-path body (`BuildCrossPackageAssignment`) WAJIB dipindah dari CMPController **byte-for-byte** (termasuk single-pkg Fisher-Yates, K=min, ET Phase 1/2/3, fallback slot-list).

### Pattern 2: Worker index resolution (controller-side, sebelum panggil core)
**What:** Index stabil worker = posisi `id` (session ini) dalam sibling Ids yang di-`OrderBy(Id)`.
**Where:** StartExam (per-session), ReshufflePackage (per-session), ReshuffleAll (per-loop-iteration).
```csharp
// StartExam: siblingSessionIds query (:949) saat ini TANPA explicit OrderBy.
// PLANNER WAJIB: tambah .OrderBy(s => s.Id) ATAU sort di memory sebelum IndexOf.
var sortedSiblingIds = siblingSessionIds.OrderBy(x => x).ToList();
int workerIndex = sortedSiblingIds.IndexOf(id);   // id = session peserta ini

// ReshuffleAll: loop `foreach (var session in sessions)` — sort siblingSessionIds sekali,
//   workerIndex = sortedIds.IndexOf(session.Id) per iterasi.
```
> **Catatan penting:** `workerIndex` HANYA dipakai di jalur OFF+≥2. Jalur ON mengabaikannya (boleh kirim 0). Tapi resolusinya tetap dihitung di controller karena butuh query EF (tidak boleh masuk core — menjaga purity).

### Anti-Patterns to Avoid
- **`assignmentCount % n` / "urutan buka" (D-02c):** Index worker berdasarkan jumlah assignment yang sudah ada / urutan buka ujian → bergeser saat reshuffle/resume → worker dapat paket beda → guard `SavedQuestionCount` false-trigger "Soal ujian telah berubah". JANGAN. Pakai `OrderBy(Id)` stabil.
- **Skip paket kosong SETELAH modulo (D-02b):** `packages[index % packages.Count]` lalu cek `.Questions.Count == 0` → distribusi tidak seimbang + index bisa jatuh ke paket kosong berulang. Filter SEBELUM modulo.
- **Memotong OFF+≥2 ke K-min (melanggar D-05):** OFF = paket utuh; JANGAN reuse `K = packages.Min(...)` dari ON-path.
- **Mengasumsikan 2 `BuildCrossPackageAssignment` verbatim → blind copy salah satu:** Phase 2 berbeda. Pindahkan **versi CMPController** (canonical, menjaga SC#1).
- **Menaruh query EF di dalam core:** Memecah purity → tak bisa unit-test tanpa DB. Semua query (sibling, packages) tetap di controller.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Acak urutan list | Custom sort/`OrderBy(rng.Next())` baru | `ShuffleEngine.Shuffle<T>` (Fisher-Yates existing) | Sudah ada, unbiased, dipakai konsisten. `OrderBy(rng.Next())` = bias + tak deterministik dgn seed. |
| ON-path ET-aware distribution | Tulis ulang algoritma sampling | Pindahkan `BuildCrossPackageAssignment` CMPController verbatim | SC#1 mengunci perilaku ON tak berubah. Tulis ulang = risiko drift = melanggar D-01a. |
| Serialize assignment | Format custom | `System.Text.Json.JsonSerializer` (existing) | Model `GetShuffledQuestionIds()`/`GetShuffledOptionIds()` sudah parse format ini. |
| Worker→paket determinisme | RNG/hash/counter | `OrderBy(Id)` index + modulo | Satu-satunya cara stabil lintas resume/reshuffle (D-02). |

**Key insight:** Phase ini adalah **konsolidasi**, bukan invensi. Tiap potongan logika (Fisher-Yates, ET-distribution, option-dict build, JSON persist, grading-by-Id) **sudah ada dan teruji di produksi**. Risiko utama = (1) salah pilih salinan canonical saat dedup, (2) memecah determinisme OFF, (3) regresi ON-path. Bukan menulis algoritma baru.

## Common Pitfalls

### Pitfall 1: Dua `BuildCrossPackageAssignment` diasumsikan identik
**What goes wrong:** Planner/executor copy salinan AssessmentAdminController (Phase 2 = per-package) ke core → perilaku ON StartExam berubah → SC#1 gagal, regresi senyap di exam beneran.
**Why it happens:** CONTEXT D-01 menulis "VERBATIM di BOTH" — faktanya Phase 2 divergen (per-ET vs per-package).
**How to avoid:** Pindahkan **versi CMPController.cs:1230-1362** sebagai canonical. Verifikasi diff vs AssessmentAdminController:5250-5393 untuk konfirmasi hanya Phase 2 (`:1318-1357` vs `:5338-5388`) yang beda. Setelah merge, reshuffle ON akan ikut versi CMPController (benar — konsistensi reshuffle↔StartExam).
**Warning signs:** Test ON ≥2 paket dengan seed tetap menghasilkan urutan beda sebelum/sesudah refactor pada call-site reshuffle.

### Pitfall 2: `siblingSessionIds` di StartExam tidak di-`OrderBy(Id)`
**What goes wrong:** Query `:949-954` `.Select(s => s.Id).ToListAsync()` TANPA `OrderBy`. SQL Server tanpa `ORDER BY` = urutan tak dijamin → `IndexOf(id)` bisa beda antar request → workerIndex tak stabil → melanggar D-02.
**Why it happens:** Urutan default sering tampak stabil di dev (clustered index) tapi tidak dijamin kontraktual.
**How to avoid:** Eksplisit `.OrderBy(s => s.Id)` di query sibling ATAU sort hasil di memory (`siblingSessionIds.OrderBy(x => x)`) sebelum `IndexOf`. Lakukan SAMA di ReshufflePackage + ReshuffleAll.
**Warning signs:** Test integration multi-session OFF≥2 kadang lulus kadang gagal (flaky) → urutan SQL tak deterministik.

### Pitfall 3: Resume false-trigger "Soal telah berubah" (SHUF-08)
**What goes wrong:** Saat resume, `currentQuestionCount = assignment.GetShuffledQuestionIds().Count` (:1023) dibanding `SavedQuestionCount` (:1027). Untuk OFF≥2, jika rekomputasi worker→paket tidak deterministik (mis. Pitfall 2), worker bisa "pindah paket" dengan jumlah soal beda → guard memicu reset.
**Why it happens:** Guard membandingkan COUNT, bukan isi. Tapi count berubah hanya jika worker pindah ke paket lain berukuran beda — itu hanya terjadi bila index tidak stabil.
**How to avoid:** Determinisme D-02 (OrderBy(Id) + filter-then-modulo) menjamin count konsisten → guard aman. **Pertahankan guard apa adanya (D-03).** Catatan: pada resume, assignment SUDAH ada (`assignment != null` di :973) → core TIDAK dipanggil ulang; `ShuffledQuestionIds` dibaca dari DB. Jadi guard hanya membandingkan count tersimpan vs count tersimpan = aman selama assignment tidak dihapus. Risiko nyata hanya di reshuffle (yang sengaja rebuild) + paket yang ukurannya berubah HC — di luar scope OFF determinisme.
**Warning signs:** Worker OFF≥2 yang resume kena TempData "Soal ujian telah berubah" tanpa HC mengubah paket.

### Pitfall 4: View fallback opsi tidak ter-verifikasi untuk OFF
**What goes wrong:** OFF `ShuffleOptions` simpan `"{}"`. View (`StartExam.cshtml:126-129`/`:159-162`) cek `optShuffle.ContainsKey(q.QuestionId)`. Jika dict kosong → fallback `q.Options.ToList()`. Tapi VM membangun opts dengan `OrderBy(o => o.Id)` (:1055) → urutan DB. Aman. RISIKO: jika seseorang mengubah build opts VM (misal menambah shuffle di VM), fallback rusak.
**How to avoid:** Jangan ubah build opts VM (:1055-1061). Pastikan OFF benar-benar serialize `"{}"` (bukan dict berisi identity-order) agar view fallback. Test: OFF → `ShuffledOptionIdsPerQuestion == "{}"`.
**Warning signs:** OFF opsi tetap teracak di browser (berarti dict tak kosong).

### Pitfall 5: File-overlap v25.0 (367/368) → konflik merge
**What goes wrong:** `CMPController.cs` + `AssessmentAdminController.cs` disentuh phase lain v25.0. Line numbers geser; edit bertabrakan.
**How to avoid:** Re-grep anchor string (bukan line number) saat execute. Koordinasi merge sebelum `/gsd-execute-phase 373` (CONTEXT §"Constraint Koordinasi"). Sequential strict 372→373→374→375.
**Warning signs:** `dotnet build` gagal post-edit dengan error di region yang seharusnya tak disentuh.

## Code Examples

### ON-Path Algorithm (CANONICAL — pindah verbatim dari CMPController:1230-1362)
```csharp
// Source: Controllers/CMPController.cs:1230-1362 (VERIFIED session ini) — CANONICAL untuk core
// Struktur (RINGKAS — pindahkan body LENGKAP byte-for-byte):
//   if packages.Count == 0 → []
//   if packages.Count == 1 → singlePkg.OrderBy(q.Order).Select(Id) → Shuffle → return
//   K = packages.Min(p => p.Questions.Count); if K==0 → []
//   etGroups = distinct non-null ElemenTeknis
//   if etGroups.Count == 0 → fallback slot-list (per-package even split + Fisher-Yates) → return
//   Phase 1: one question per ET group (best-effort, cap K)
//   Phase 2: fill remaining quota, basePerET = remaining / etGroups.Count  ◄── BEDA dari AdminController!
//            extraETs = etGroups.OrderBy(rng).Take(remainder); per-ET take quota
//            fallback NULL-ET jika kurang
//   Phase 3: Shuffle(selectedList, rng) → return
```
> AssessmentAdminController:5250-5393 Phase 2 (`:5338`) memakai `baseCount = remaining / N_packages` + slot redistribution — **JANGAN dipakai**. Hanya CMPController versi yang masuk core.

### OFF-Path Spec (baru — SHUF-05/06)
```csharp
// OFF + 1 paket (SHUF-05): identik semua peserta, urut q.Order, NO shuffle
return packages[0].Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();

// OFF + ≥2 paket (SHUF-06): filter-then-modulo, paket UTUH (D-05)
var pkgWithQ = packages.Where(p => p.Questions.Count > 0)   // D-02b: filter SEBELUM modulo
                       .OrderBy(p => p.PackageNumber).ToList();
if (pkgWithQ.Count == 0) return new List<int>();
var chosen = pkgWithQ[workerIndex % pkgWithQ.Count];        // D-02: index-session-stabil
return chosen.Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();  // utuh, urut
```

### Option Shuffle (SHUF-07 — independen dari Acak Soal)
```csharp
// Source pola: CMPController:982-989 (build ON existing)
public static Dictionary<int, List<int>> BuildOptionShuffle(
    IEnumerable<PackageQuestion> questions, bool shuffleOptions, Random rng)
{
    var dict = new Dictionary<int, List<int>>();
    if (!shuffleOptions) return dict;   // OFF → kosong → caller serialize "{}"  → view fallback DB
    foreach (var q in questions)
    {
        var optionIds = q.Options.Select(o => o.Id).ToList();
        Shuffle(optionIds, rng);
        dict[q.Id] = optionIds;
    }
    return dict;
}
// Caller: ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(dict);  // {} bila OFF
// PENTING (D-06): questions = HANYA soal yang ditugaskan ke worker (untuk OFF≥2 = paket worker itu),
//   bukan SEMUA paket. Existing StartExam:983 pakai packages.SelectMany(p=>p.Questions) (semua) —
//   itu OK saat ON (semua soal mungkin terpilih) tapi untuk OFF≥2 hanya soal paket worker yang relevan.
//   Aman bila dict berisi extra qId (view hanya pakai key yang cocok). Planner putuskan: build dict
//   dari soal terpilih (shuffledIds) ATAU semua — keduanya benar karena view lookup by qId.
```

### Reshuffle (SHUF-09 — fix bug `"{}"`)
```csharp
// Source: AssessmentAdminController.cs:5109-5120 (ReshufflePackage) — SEBELUM:
//   var shuffledIds = BuildCrossPackageAssignment(packages, rng);   ← selalu acak (abaikan flag)
//   ShuffledOptionIdsPerQuestion = "{}"                              ← BUG: selalu kosong
// SESUDAH:
var assessment = ...;  // sudah di-load (:5067), punya .ShuffleQuestions/.ShuffleOptions
int workerIndex = sortedSiblingIds.IndexOf(sessionId);
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
    packages, assessment.ShuffleQuestions, workerIndex, Random.Shared);
var assignedQs = packages.SelectMany(p => p.Questions).Where(q => shuffledIds.Contains(q.Id));
var optDict = ShuffleEngine.BuildOptionShuffle(assignedQs, assessment.ShuffleOptions, Random.Shared);
// ... ShuffledOptionIdsPerQuestion = JsonSerializer.Serialize(optDict);   ← fix
// ReshuffleAll (:5201-5213) idem, per session di loop; workerIndex = sortedIds.IndexOf(session.Id).
// D-04a: guard "Not started/Abandoned" (:5083 / :5191) DIPERTAHANKAN apa adanya.
```

### Cleanup (SHUF-15 / D-07)
```csharp
// CMPController.cs:1054 — SEBELUM:
//   // Options in original DB order — option shuffle removed per user decision
// SESUDAH (cerminkan realita — opsi aktif, digerbang ShuffleOptions):
//   // Options in DB order here (base list); per-user reorder applied in view via
//   // ViewBag.OptionShuffle when ShuffleOptions=ON. OFF stores "{}" → view falls back to this DB order.
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single-package per-position shuffle | Cross-package `BuildCrossPackageAssignment` + sentinel FK | v2.1 Phase 45 | Basis ON-path sekarang. |
| Opsi diacak (dicabut) | Opsi dicabut `d777d6b9` lalu dihidupkan lagi `e6ddffd6` via `ViewBag.OptionShuffle` | v3.0 Phase 91 (fix 91-01) | Komentar :1054 jadi STALE (SHUF-15). |
| Acak hardcoded selalu aktif | Flag `ShuffleQuestions`/`ShuffleOptions` per-assessment | v27.0 Phase 372 (data) → 373 (read logic) | Phase ini = gerbang flag. |

**Deprecated/outdated:**
- Komentar `CMPController.cs:1054` ("option shuffle removed per user decision") — STALE; opsi aktif & digerbang flag (SHUF-15 fix).
- Non-package legacy exam path — sudah mati (Phase 227 CLEN-02, `CMPController.cs:1161`). Tidak relevan.

## Runtime State Inventory

> Phase 373 = code-only refactor (migration=false). Tidak ada rename/migrasi data. Inventory tetap diisi eksplisit:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `UserPackageAssignment` rows existing menyimpan `ShuffledQuestionIds`/`ShuffledOptionIdsPerQuestion` dari assignment LAMA (mode ON, sebelum flag). Tidak di-rebuild oleh Phase 373 (assignment hanya dibangun saat null / reshuffle). | None — assignment lama tetap valid (semua flag lama = ON via migration default 372). Tidak ada migrasi data. |
| Live service config | None — tidak ada config eksternal. | None. |
| OS-registered state | None. | None. |
| Secrets/env vars | None disentuh. | None. |
| Build artifacts | `Helpers/ShuffleEngine.cs` baru → re-compile. Tidak ada egg-info/binary terdaftar. | `dotnet build` (otomatis). |

**Verifikasi:** Migration=false dikonfirmasi (ROADMAP:135, CONTEXT:16). Tidak ada kolom/skema berubah; flag sudah dibuat Phase 372.

## Validation Architecture

> nyquist_validation = true (config.json:15) → section WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (+ Microsoft.NET.Test.Sdk 17.13.0) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0, ProjectReference ke HcPortal) |
| Quick run command | `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` (hanya core unit, no DB) |
| Full suite (no SQL) | `dotnet test --filter "Category!=Integration"` |
| Full suite (with real SQL) | `dotnet test` (butuh SQL Server lokal up — Phase 372 fixture `ProtonCompletionFixture`) |

### Test Pattern (project conventions)
- **Pure core unit (DIREKOMENDASIKAN untuk 373 core):** TANPA `[Trait("Category","Integration")]`, TANPA fixture/DB. Cetakan = `QuestionTypeLabelsTests.cs` (`[Theory]`/`[InlineData]`, panggil static langsung). Feed `new AssessmentPackage { Questions = { new PackageQuestion {...} } }` in-memory.
- **Real-SQL integration (sudah ada dari 372, lanjut di 375):** `[Trait("Category","Integration")]` + `IClassFixture<ProtonCompletionFixture>`, `new ApplicationDbContext(_fixture.Options)`. Cetakan = `ShuffleCreatePersistenceTests.cs`, `ShufflePropagationTests.cs`.
- **Deterministik ON:** kirim `new Random(seed)` tetap → assert urutan deterministik. OFF tidak butuh rng.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SHUF-04 | ON 1 paket → semua soal, acak (seed-stabil) | unit (pure) | `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` | ❌ Wave 0 |
| SHUF-04 | ON ≥2 paket → sampling K-min, ET-balanced (seed-stabil) | unit (pure) | idem | ❌ Wave 0 |
| SHUF-05 | OFF 1 paket → urut `q.Order`, identik semua worker (no rng) | unit (pure) | idem | ❌ Wave 0 |
| SHUF-06 | OFF ≥2 → worker[i] dapat `pkgWithQ[i % count]` utuh urut Order; worker beda index → paket beda; index stabil (append peserta baru tak geser) | unit (pure) | idem | ❌ Wave 0 |
| SHUF-06 | Guard paket kosong: paket `Questions.Count==0` di-exclude SEBELUM modulo | unit (pure) | idem | ❌ Wave 0 |
| SHUF-07 | ON → dict non-kosong per soal; OFF → dict kosong (serialize `"{}"`) | unit (pure) | idem | ❌ Wave 0 |
| SHUF-07 | Independensi: ShuffleQuestions OFF + ShuffleOptions ON → soal urut tapi opsi teracak | unit (pure) | idem | ❌ Wave 0 |
| SHUF-08 | OFF≥2 rekomputasi deterministik → `BuildQuestionAssignment` dipanggil 2x dgn input sama → output identik (count stabil → guard tak trigger) | unit (pure) | idem | ❌ Wave 0 |
| SHUF-09 | Reshuffle hormati flag: assert delegasi ke core + optDict bukan `"{}"` saat ShuffleOptions ON | integration (real-SQL) ATAU controller-shape unit | `dotnet test` | ⚠️ defer 375 (full) / Wave 0 shape opsional |
| SHUF-15 | Komentar `:1054` cleanup | grep (manual/verifier) | `rg "option shuffle removed" Controllers/CMPController.cs` → 0 match | N/A (no test) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~ShuffleEngine"` (core unit, < 5s, no DB).
- **Per wave merge:** `dotnet test --filter "Category!=Integration"` (semua unit hijau).
- **Phase gate:** `dotnet build` (0 err) + `dotnet test` full (dengan SQL up untuk Phase 372 integration tetap hijau = no-regression) + `dotnet run` localhost:5277 smoke (CLAUDE.md Develop Workflow). UAT browser penuh = Phase 375.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ShuffleEngineTests.cs` — pure unit, covers SHUF-04/05/06/07/08 (+ guard paket kosong, determinisme, independensi). **REKOMENDASI: tulis di 373** (core ekstraksi = saat paling tepat menguji; file 374/375 menyentuh controller yang sama → menguji belakangan = sampling terlambat).
- [ ] (Opsional) Controller-shape assertion untuk SHUF-09 reshuffle — atau tahan penuh ke Phase 375 mode-matrix (CONTEXT diskresi). **REKOMENDASI: minimal Wave-0 assert optDict bukan `"{}"`** karena ini fix BUG existing (bukan sekadar fitur baru) → butuh bukti regresi tertutup.
- [ ] Framework install: tidak perlu — xUnit + fixture sudah ada.

*(SHUF-15 = komentar, diverifikasi via grep di verifier, bukan unit test.)*

## Security Domain

> `security_enforcement` tidak eksplisit di config.json (absent = enabled); Phase 372 punya SECURITY.md → milestone aktif. Phase 373 = read-logic/refactor, attack surface minimal.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tidak ada perubahan auth; StartExam ownership check existing (:869) tidak disentuh. |
| V3 Session Management | no | — |
| V4 Access Control | yes (preserve) | StartExam: owner/Admin/HC check (:869) DIPERTAHANKAN. Reshuffle: `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` (:5062-5064, :5143-5145) DIPERTAHANKAN. Refactor TIDAK boleh melonggarkan. |
| V5 Input Validation | yes (low) | Input core = entity dari DB (bukan user-supplied langsung). `workerIndex % count` — pastikan `count > 0` (guard paket kosong) agar tak DivideByZero. |
| V6 Cryptography | no | `Random.Shared` cukup (acak ujian, bukan kriptografis — sudah preseden produksi). |

### Known Threat Patterns for ASP.NET Core MVC exam-engine refactor
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| DivideByZero saat semua paket kosong (`workerIndex % 0`) | Denial of Service | Guard `pkgWithQ.Count == 0 → return []` SEBELUM modulo (D-02b). |
| Melonggarkan auth saat refactor reshuffle | Elevation of Privilege | Pertahankan `[Authorize]`+`[ValidateAntiForgeryToken]`+guard "Not started/Abandoned" (D-04a) verbatim. |
| Worker melihat paket worker lain via index manipulation | Information Disclosure | `workerIndex` dihitung server-side dari `id` session milik peserta (`IndexOf` di sibling), bukan dari input klien. Tidak ada parameter klien yang mempengaruhi index. |
| Grading bocor via posisi opsi | Tampering | Grading by `PackageOption.Id` (D-06a), bukan posisi huruf — acak opsi tak pengaruh nilai. Tak berubah. |

**Catatan:** Tidak ada endpoint baru, tidak ada input user baru, tidak ada perubahan skema. Risiko keamanan = regresi kontrol existing (mitigasi: preserve verbatim). Security review penuh = `/gsd-secure-phase 373` pasca-implement (pola 372).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build/test/run | ✓ | 8.0.418 | — |
| SQL Server lokal (SQLEXPRESS HcPortalDB_Dev) | Phase 372 integration tests (no-regression gate) + `dotnet run` | ✓ (per project setup) | — | Core unit test TIDAK butuh; jalankan `--filter "Category!=Integration"` jika SQL down |
| Playwright | UAT browser | N/A Phase 373 | — | UAT penuh = Phase 375 |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Core unit test (`ShuffleEngineTests`) berjalan tanpa SQL — gate utama 373 tidak butuh DB up.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Lokasi core `Helpers/ShuffleEngine.cs` static (vs `Services/`) | Standard Stack / Structure | Rendah — diskresi planner eksplisit (CONTEXT). Pola project mendukung Helpers/static. |
| A2 | Input core = `List<AssessmentPackage>` langsung (tanpa DTO) | Alternatives | Rendah — POCO instantiable tanpa DB (diverifikasi). DTO opsional bila planner mau lebih ketat. |
| A3 | Wave-0 core unit test ditulis di 373 (bukan ditahan 375) | Validation Architecture | Sedang — CONTEXT diskresi. Bila ditahan, ekstraksi 373 tak punya self-check → risiko regresi terdeteksi telat. Rekomendasi kuat: tulis di 373. |
| A4 | Build optionDict dari semua soal vs hanya soal terpilih untuk OFF≥2 — keduanya benar (view lookup by qId) | Code Examples (Option Shuffle) | Rendah — view hanya pakai key cocok; extra key tak berefek. Planner pilih yang lebih bersih. |
| A5 | `security_enforcement` enabled (absent=enabled) → Security Domain disertakan | Security Domain | Rendah — Phase 372 punya SECURITY.md; konsisten milestone. |

## Open Questions

1. **Penempatan resolusi `workerIndex` di ReshuffleAll loop.**
   - What we know: ReshuffleAll iterasi `foreach (var session in sessions)` (:5177). `sessions` di-query TANPA OrderBy (:5148-5153). Perlu sort sibling Ids sekali, `IndexOf(session.Id)` per iterasi.
   - What's unclear: apakah `sessions` (semua sibling, termasuk Completed/InProgress yang di-skip) = himpunan yang sama dengan `siblingSessionIds` di StartExam. Jika beda himpunan → index bisa beda antara StartExam dan ReshuffleAll → worker pindah paket saat reshuffle.
   - Recommendation: Hitung `workerIndex` dari **himpunan sibling yang IDENTIK** di ketiga call-site: `siblingSessionIds.OrderBy(Id)` (semua sibling sessions grup, TANPA filter status). Pastikan StartExam:949 dan ReshuffleAll:5148 query himpunan yang sama (key Title+Category+Schedule.Date). Planner verifikasi parity ini — ini krusial untuk determinisme lintas StartExam↔reshuffle (D-02a).

2. **optionDict scope untuk OFF≥2 (A4).** Lihat Assumptions. Planner putuskan build dari soal terpilih (`shuffledIds`) — lebih bersih & hemat — atau semua. Tidak memblokir.

## Sources

### Primary (HIGH confidence — verified in code this session)
- `Controllers/CMPController.cs:860-1176, 1211-1362` — StartExam + Shuffle + BuildCrossPackageAssignment (canonical ON-path).
- `Controllers/AssessmentAdminController.cs:5061-5393` — ReshufflePackage + ReshuffleAll + duplikat BuildCrossPackageAssignment (Phase 2 divergen).
- `Models/UserPackageAssignment.cs`, `Models/AssessmentPackage.cs`, `Models/AssessmentSession.cs:38-42` — entity shape (POCO, instantiable tanpa DB).
- `Views/CMP/StartExam.cshtml:120-168` — option-shuffle view fallback (DB order saat `"{}"`).
- `Services/GradingService.cs:60-94, 339` — grading by `GetShuffledQuestionIds()` (D-06a aman).
- `Helpers/ImageFileCleanup.cs`, `Models/QuestionTypeLabels.cs` — pola static-helper extracted project.
- `HcPortal.Tests/ShuffleCreatePersistenceTests.cs`, `ShufflePropagationTests.cs`, `MarkMappingCompletedTests.cs`, `QuestionTypeLabelsTests.cs` — konvensi test (pure vs real-SQL integration).
- `HcPortal.Tests/HcPortal.Tests.csproj` — framework versi.
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` §6/§8/§10/§11/§13 — spec terkunci.
- `.planning/config.json` — nyquist_validation=true.
- `dotnet --version` → 8.0.418.

### Secondary (MEDIUM)
- `.planning/ROADMAP.md:110-143` — deskripsi + SC Phase 373/375.
- `.planning/REQUIREMENTS.md:59-74` — SHUF-01..16 mapping.
- `.planning/phases/372-*/` summaries — konteks Phase 372 (kolom + propagasi live).

### Tertiary (LOW)
- (none) — semua klaim arsitektur diverifikasi langsung di kode.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — versi & file diverifikasi langsung.
- Architecture (core shape, call-sites, data flow): HIGH — 3 call-site + entity + view + grading semua dibaca penuh.
- Dedup finding (Phase 2 divergen): HIGH — kedua salinan dibaca, divergensi Phase 2 dikonfirmasi (CMPController per-ET `:1318` vs AdminController per-package `:5338`).
- Determinisme OFF / worker index: HIGH untuk algoritma; MEDIUM untuk parity himpunan sibling lintas call-site (Open Question #1 — planner verifikasi saat plan).
- Pitfalls: HIGH — diturunkan dari kode aktual + spec D-02c.
- Validation: HIGH — pola test 372 + helper-test 357 jadi cetakan langsung.

**Research date:** 2026-06-13
**Valid until:** ~2026-06-20 (7 hari — file-overlap v25.0 aktif; line numbers cepat drift. Re-grep anchor saat execute, JANGAN andalkan line number.)
