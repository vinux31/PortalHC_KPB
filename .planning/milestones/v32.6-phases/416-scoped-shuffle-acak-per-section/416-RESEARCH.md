# Phase 416: Scoped Shuffle (Acak per-Section) - Research

**Researched:** 2026-06-23
**Domain:** Refactor pure-function algorithm (ShuffleEngine) — partitioned/scoped randomization with backward-compatibility invariant
**Confidence:** HIGH (semua klaim VERIFIED dari kode lokal + spec milestone; nol dependensi eksternal baru)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-416-01:** SATU saklar `ShuffleEnabled` per-Section (kolom existing dari 415) meng-gate acak **SOAL + OPSI sekaligus** untuk Section itu (ON = soal & opsi Section diacak; OFF = urut `Question.Order`). TIDAK dipecah jadi 2 toggle terpisah. **migration=FALSE dipertahankan.** Induk (assessment) tetap 2 toggle terpisah: `ShuffleQuestions` + `ShuffleOptions`.
- **D-14 (precedence, locked spec):** induk = saklar utama. Induk `ShuffleQuestions` OFF → SEMUA section terurut (toggle per-Section diabaikan). Induk ON → tiap Section ikut `ShuffleEnabled`-nya. Sama untuk `ShuffleOptions` (di-gate `ShuffleEnabled` per-Section yang sama).
- **D-416-02:** Reshuffle (`ReshufflePackage` & `ReshuffleAll`) = **RE-ROLL** — ambil sampling baru lintas-paket DALAM batas Section, deterministik by `workerIndex` (urutan sibling sorted). Peserta bisa dapat SET soal berbeda per-Section. Section-aware: soal tak bocor antar-Section.
- **D-416-03:** ET-coverage = **best-effort + peringatan ke HC**. Jamin 1 soal per ET sampai kuota K (jumlah soal Section) habis, sisanya balanced (sama perilaku K-min existing). TAMBAH peringatan ke HC saat kelola Section bila `K < jumlah distinct ET` di Section. **TIDAK blokir** mulai ujian. Peringatan = sinyal, bukan error.
- **D-416-04:** Section tetap **OPSIONAL**. Assessment tanpa Section (semua `SectionId=null`) = **perilaku lama identik** (acak global ET-aware, 1 toggle ngocok semua). Grup "Lainnya" (null section di paket bersection) ikut toggle induk, selalu di urutan terakhir, ET-aware komposit `(null, ET)` (D-15, locked).
- **D-416-05:** Backward-compat WAJIB dibuktikan **PENUH**: (1) golden-order regression — all-null = urutan IDENTIK baseline pra-416; (2) determinisme `workerIndex`; (3) reshuffle section-aware tak bocor antar-Section; (4) Playwright UAT real-browser acak per-Section. Bukan cuma smoke.

### Claude's Discretion
- Bentuk refactor `ShuffleEngine` (signature persis `BuildSectionQuestionAssignment`, apakah `BuildQuestionAssignment` lama dipertahankan sebagai jalur all-null vs di-wrap) — planner/researcher putuskan, asal kunci komposit `(SectionNumber, ET)` + jalur all-null = output identik baseline.
- Tempat & wording peringatan ET-coverage (D-416-03) di UI kelola Section.

### Deferred Ideas (OUT OF SCOPE)
- **2 saklar terpisah per-Section** (acak-soal vs acak-opsi independen per-Section) — ditolak di 416 (butuh kolom DB baru → migration + UI nambah, manfaat kecil). Angkat lagi hanya bila HC benar-benar butuh kontrol acak-opsi per-Section independen.
- Todo "cleanup data test lokal pasca-367" — reviewed, not folded (ops, bukan scope 416).
- OUT (fase lain): pagination section-aware (417), opsi dinamis A–F render/grading (418), export label Section (419).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **SHF-01** | Pengacakan soal hanya terjadi di dalam Section (soal tidak melompat antar-Section); assessment tanpa Section berperilaku sama seperti sekarang. | `BuildSectionQuestionAssignment` per-Section + concat urut SectionNumber; jalur all-null = `BuildCrossPackageAssignment` verbatim (golden-order regression). Lihat **Architecture Patterns → Pattern 1/2**. |
| **SHF-02** | HC dapat menyalakan/mematikan acak per-Section; toggle level-assessment induk, toggle per-Section anak. | Precedence D-14 di-gate di entry point; `ShuffleEnabled` per-Section sudah ada (Phase 415, kolom + UI). Lihat **Pattern 3 (precedence gating)**. |
| **SHF-03** | Untuk >1 paket, tiap Section diisi dari gabungan Section padanan lintas-paket lalu diacak/sampling dalam batas Section; jaminan cakupan ET per-Section. | Pooling lintas-paket per-Section (D-09) + Phase-1 ET-aware kunci komposit `(SectionNumber, ET)` per-Section. Lihat **Pattern 1**. K dijamin sama antar-paket-saudara (D-13, sudah di-enforce 415). |
| **SHF-04** | Reshuffle (per-paket & semua peserta) menghormati batas Section. | `ReshufflePackage`/`ReshuffleAll`/`CreateEagerAssignmentsAsync` semua delegate ke entry section-aware yang sama → otomatis section-aware. Lihat **Architecture Patterns → Integration Points**. |
</phase_requirements>

---

## Summary

Phase 416 adalah **refactor algoritma fungsi-murni** (`Helpers/ShuffleEngine.cs`), bukan fitur baru ber-DB. Inti: generalisasi distribusi soal cross-package yang ada — yang saat ini memandang seluruh assessment sebagai **satu kolam soal global** — menjadi distribusi **per-Section** dengan kunci komposit `(SectionNumber, ET)`. Mental model spec (§6.1) sangat presisi: *"Tanpa section = 1 section raksasa = persis perilaku sekarang."* Itu satu-satunya invariant yang menjaga backward-compat: bila semua `SectionId=null`, jalur baru HARUS hasilkan urutan ID byte-identik dengan algoritma pra-416.

Engine sudah pure (hanya `System`/`Linq`/`HcPortal.Models`, no EF, no DB) dan punya 3 fase internal (`BuildCrossPackageAssignment`: Phase 1 jamin 1-soal-per-ET, Phase 2 balanced fill, Phase 3 Fisher-Yates). Strategi termudah & teraman: **iterasi Section urut `SectionNumber` → panggil sub-fungsi per-Section yang membungkus algoritma cross-package existing pada subset soal Section itu → concat hasil (Section 1 → 2 → … → "Lainnya")**. Untuk jalur all-null (legacy), semua soal jatuh ke satu grup "Lainnya" tunggal, sehingga sub-fungsi dipanggil sekali atas seluruh kolam = output identik baseline. Ini memenuhi golden-order regression **secara konstruksi**, bukan kebetulan.

Tiga integration point memanggil engine dengan pola IDENTIK (sama `workerIndex` semantics, sama `Random.Shared`): `CMPController.StartExam` (~L1130), `AssessmentAdminController.ReshufflePackage`/`ReshuffleAll` (~L6044/L6158), dan `CreateEagerAssignmentsAsync` (AddParticipantsLive, ~L2554). Bila refactor mempertahankan signature entry `BuildQuestionAssignment(packages, shuffleQuestions, workerIndex, rng)` dan menambah jalur section-aware DI DALAMNYA (membaca `Section`/`SectionNumber`/`ShuffleEnabled` dari `PackageQuestion`/`AssessmentPackageSection` yang sudah ter-`Include`), maka SHF-04 + AddParticipantsLive drift-free terpenuhi **otomatis** dengan perubahan minimal di call-site — tiga call-site cukup memastikan `Section` ter-`Include`.

**Primary recommendation:** Refactor `ShuffleEngine.BuildQuestionAssignment` menjadi section-partitioning wrapper di sekitar sub-fungsi `BuildSectionQuestionAssignment` (kunci komposit `(SectionNumber, ET)`), pertahankan signature entry-point publik agar 3 call-site nyaris tak berubah (hanya pastikan `.ThenInclude(q => q.Section)` ter-load), buktikan all-null golden-order regression via test paritas terhadap output `BuildCrossPackageAssignment` lama yang dibekukan, dan gate option-shuffle per-Section via `ShuffleEnabled` (D-416-01) bukan flag induk tunggal.

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Distribusi & acak soal per-Section (kunci `(SectionNumber, ET)`) | Helper murni (`ShuffleEngine`) | — | Sudah pure-by-design (no EF/DB); unit-testable tanpa DB; single source of truth (Phase 373). [VERIFIED: Helpers/ShuffleEngine.cs] |
| Acak opsi per-Section di-gate `ShuffleEnabled` | Helper murni (`ShuffleEngine.BuildOptionShuffle` + caller filter) | — | Logika gating per-soal; option dict tetap pure. [VERIFIED: ShuffleEngine.cs:67] |
| Load packages+questions+sections+options | API/Backend (Controllers) | Database (EF Include) | EF `.Include().ThenInclude()` memuat graf; engine terima objek in-memory. [VERIFIED: CMPController.cs:1050] |
| Wire assignment + persist `UserPackageAssignment` | API/Backend (StartExam, Reshuffle*, EagerAssign) | Database | Server-authoritative; assignment di-lock setelah load pertama. [VERIFIED: CMPController.cs:1143] |
| Peringatan ET-coverage (K < distinct-ET) saat kelola Section | API/Backend (`ManagePackageQuestions` GET → ViewBag) | Frontend (Razor render warning) | Hitung di controller (punya `pkg.Questions` + `sections`); render di view. [VERIFIED: AssessmentAdminController.cs:7637] |
| Precedence induk/anak (D-14) | Helper murni (`ShuffleEngine` entry) | — | Logika murni: induk OFF → semua ordered; induk ON → per-Section. [CITED: spec §6.3] |

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET / C# | net8.0 | Engine + controllers | Stack project existing (HcPortal.csproj). [VERIFIED: HcPortal.Tests.csproj:4] |
| EF Core | 8.0.0 | Load package/section/question graph | Sudah dipakai semua call-site; tak ada perubahan skema (migration=FALSE). [VERIFIED: HcPortal.Tests.csproj:13] |
| `System.Random` (`Random.Shared`) | BCL | Fisher-Yates + sampling | Sudah dipakai engine; determinisme via seed di test (`new Random(42)`), bukan di produksi. [VERIFIED: ShuffleEngine.cs:25, CMPController.cs:1125] |

### Supporting (test)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Unit test engine murni (no DB) | Suite BARU `BuildSectionQuestionAssignment` (§12 spec: tulis baru, jangan retrofit). [VERIFIED: HcPortal.Tests.csproj:15] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner | Existing. [VERIFIED: csproj:14] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | (opsional) integration test call-site | Tersedia bila perlu uji wiring StartExam; engine sendiri TAK butuh. [VERIFIED: csproj:12] |
| @playwright/test | (existing `tests/`) | Real-browser UAT acak per-Section (D-416-05) | Wajib per lesson 354 (Razor/JS) + D-416-05. Pola: `tests/e2e/shuffle.spec.ts`. [VERIFIED: tests/e2e/shuffle.spec.ts] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Wrapper di `BuildQuestionAssignment` (pertahankan signature) | Entry point baru `BuildSectionQuestionAssignment` publik + ubah semua call-site | Spec §6.2 menyebut nama `BuildSectionQuestionAssignment` untuk **sub-fungsi per-section**; entry lama tetap iterate. Ubah signature publik = 3 call-site berubah lebih banyak + risiko drift. Pertahankan entry → minimal blast radius. **Rekомendasi: pertahankan entry, tambah sub-fungsi.** |
| Ekstrak `IQuestionSequence`/`SectionAwareQuestionProvider` (saran spec §13) | Refactor langsung di ShuffleEngine | Abstraksi TIDAK diekstrak di 415 (verified: tak ada file). 416 scope = shuffle saja; abstraksi penuh menyentuh pagination (417). **Rekомendasi: jangan over-abstract di 416**; cukup section-partition di ShuffleEngine. Pagination 417 boleh ekstrak bila perlu. |

**Installation:** Tidak ada paket baru. `dotnet build` + `dotnet test` cukup. **migration=FALSE** (reuse `AssessmentPackageSection.ShuffleEnabled` dari Phase 415). [VERIFIED: Models/AssessmentPackage.cs:53]

**Version verification:** Tidak ada paket NPM/NuGet baru ditambahkan → tak perlu `npm view`/`dotnet add`. Semua versi di atas dibaca langsung dari `HcPortal.Tests.csproj` (terkini di repo). [VERIFIED: HcPortal.Tests.csproj]

---

## Architecture Patterns

### System Architecture Diagram

```
                         StartExam (peserta buka ujian)
                         ReshufflePackage / ReshuffleAll (HC re-roll)
                         CreateEagerAssignmentsAsync (AddParticipantsLive)
                                   │
                                   │  load packages
                                   ▼
        ┌──────────────────────────────────────────────────────────┐
        │ EF: AssessmentPackages                                    │
        │   .Include(p => p.Questions)                              │
        │     .ThenInclude(q => q.Options)                          │
        │   + .ThenInclude(q => q.Section)   ◄── TAMBAH di 416      │
        └──────────────────────────────────────────────────────────┘
                                   │  List<AssessmentPackage> (in-memory graph)
                                   ▼
        ┌──────────────────────────────────────────────────────────┐
        │ ShuffleEngine.BuildQuestionAssignment(                    │
        │     packages, shuffleQuestions, workerIndex, rng)         │
        │                                                          │
        │  1. PARTITION soal per SectionNumber (urut asc)          │
        │     • soal SectionId=null → grup "Lainnya" (selalu akhir)│
        │  2. PRECEDENCE (D-14):                                   │
        │     • induk shuffleQuestions OFF → semua section ORDERED │
        │     • induk ON → tiap section ikut ShuffleEnabled-nya    │
        │  3. FOR EACH section (urut) →                            │
        │       BuildSectionQuestionAssignment(sectionSlice, ...)  │
        │         ├ ON  : Phase1 1-per-(Section,ET) + Phase2 + FY  │
        │         └ OFF : urut Question.Order (no FY)              │
        │  4. CONCAT hasil: Sec1 → Sec2 → … → "Lainnya"            │
        └──────────────────────────────────────────────────────────┘
                                   │  List<int> shuffledQuestionIds (cross-section, ordered)
                                   ▼
        ┌──────────────────────────────────────────────────────────┐
        │ caller: filter assignedQuestions = id ∈ shuffledIds      │
        │ ShuffleEngine.BuildOptionShuffle(assignedQ, gate, rng)   │
        │   gate per-soal = ShuffleEnabled section soal itu (D-416-01)│
        └──────────────────────────────────────────────────────────┘
                                   │
                                   ▼
        UserPackageAssignment { ShuffledQuestionIds (JSON), ShuffledOptionIdsPerQuestion (JSON) }
                                   │  persist (lock setelah load pertama)
                                   ▼
                         StartExam.cshtml render (urutan + opsi)
```

**Invariant kritis (jalur all-null):** bila SEMUA soal `SectionId=null` → langkah 1 hasilkan **satu grup tunggal** "Lainnya" → langkah 3 panggil sub-fungsi **sekali atas seluruh kolom** = panggilan identik dengan `BuildCrossPackageAssignment(packages, rng)` lama → **output byte-identik baseline**. Inilah golden-order regression secara konstruksi. [CITED: spec §6.1, §15.A "Skenario semua null"]

### Recommended Project Structure
```
Helpers/
├── ShuffleEngine.cs          # REFACTOR: tambah partisi section + sub-fungsi per-section
│                             #   - BuildQuestionAssignment (entry, signature TETAP) → partisi+concat
│                             #   - BuildSectionQuestionAssignment (sub, kunci (SectionNumber,ET))  [BARU, internal/public]
│                             #   - BuildCrossPackageAssignment (existing core, jadi callee per-section)
│                             #   - BuildOptionShuffle (existing; caller gate per-section ShuffleEnabled)
├── SectionStructureComparer.cs  # existing (Phase 415) — KeyOf(null)→LainnyaKey; reuse untuk "Lainnya" key
HcPortal.Tests/
├── ShuffleEngineTests.cs                # existing (Phase 373) — HARUS tetap hijau (all-null path)
├── SectionScopedShuffleTests.cs         # BARU — isolasi section, golden-order, determinisme, (Section,ET)
Controllers/
├── CMPController.cs                      # StartExam ~L1130 — load .ThenInclude(q=>q.Section)
├── AssessmentAdminController.cs         # Reshuffle* ~L6044/L6158 + EagerAssign ~L2554 + ET-warning di ManagePackageQuestions GET ~L7637
tests/e2e/
├── shuffle.spec.ts                      # existing pola UAT
├── scoped-shuffle.spec.ts               # BARU (atau extend) — UAT real-browser acak per-Section (D-416-05)
```

### Pattern 1: Section-partition + per-section ET-aware sampling (SHF-01/03)
**What:** Entry point mempartisi soal lintas-paket berdasarkan `SectionNumber`, lalu untuk tiap Section jalankan algoritma cross-package existing pada subset Section itu (kolam = soal Section padanan dari semua paket). Concat urut.
**When to use:** Selalu — ini jalur tunggal (all-null pun lewat sini, jatuh ke 1 grup "Lainnya").
**Example (sketsa — adaptasi dari ShuffleEngine.cs existing):**
```csharp
// Source: derived from Helpers/ShuffleEngine.cs:39-232 + spec §6.2 [CITED]
public static List<int> BuildQuestionAssignment(
    List<AssessmentPackage> packages, bool shuffleQuestions, int workerIndex, Random rng)
{
    if (packages.Count == 0) return new List<int>();

    // Grup section komposit lintas-paket: kunci = SectionNumber (null → LainnyaKey).
    // Semua paket saudara dijamin punya struktur section identik (D-13 / 415 guard).
    var sectionKeys = packages
        .SelectMany(p => p.Questions)
        .Select(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber))
        .Distinct()
        .OrderBy(k => k == SectionStructureComparer.LainnyaKey) // "Lainnya" terakhir (D-15)
        .ThenBy(k => k)
        .ToList();

    var result = new List<int>();
    foreach (var sk in sectionKeys)
    {
        // Per-section: bangun "sub-packages" hanya berisi soal section ini (slice tiap paket).
        var sectionPackages = SlicePackagesBySection(packages, sk); // pure helper
        // ShuffleEnabled per-section (D-416-01) — "Lainnya" tak punya row → ikut induk (D-15).
        bool sectionShuffle = shuffleQuestions && ResolveSectionShuffle(packages, sk);
        result.AddRange(
            BuildSectionQuestionAssignment(sectionPackages, sectionShuffle, workerIndex, rng));
    }
    return result;
}
```
> **Catatan:** `BuildSectionQuestionAssignment` = pembungkus tipis atas `BuildCrossPackageAssignment` (ON) / urut-Order (OFF) yang sudah ada — beroperasi pada slice section. Phase-1 ET-aware sudah jadi `(SectionNumber, ET)` **secara otomatis** karena tiap panggilan hanya melihat soal satu Section (ET di dalam slice = subset ET global untuk section itu). ET lintas-section ditangani independen tiap slice (ET-SECTION-01). [CITED: spec §6.2, §15.A "ET lintas-section"]

### Pattern 2: Golden-order backward-compat (all-null) — SHF-01, D-416-05
**What:** Bekukan output `BuildCrossPackageAssignment` lama sebagai "baseline" untuk dataset all-null, lalu assert jalur baru menghasilkan list IDENTIK dengan seed sama.
**When to use:** Test wajib (D-416-05 #1).
**Example:**
```csharp
// Source: pola ShuffleEngineTests.cs:34-43 [VERIFIED] + golden-order strategy
[Fact]
public void AllNullSection_ProducesIdenticalOrderToLegacyBaseline()
{
    var packages = BuildAllNullFixture(); // semua SectionId=null, ET bervariasi, ≥2 paket
    // Baseline: panggil core lama langsung (atau snapshot list literal yg dicatat sekali).
    var baseline = LegacyBuildCrossPackageAssignment(Clone(packages), new Random(42));
    // Jalur baru via entry yang sudah di-refactor:
    var actual   = ShuffleEngine.BuildQuestionAssignment(Clone(packages), true, 0, new Random(42));
    Assert.Equal(baseline, actual); // byte-identik (RNG sequence sama → urutan FY sama)
}
```
> **Pitfall RNG:** golden-order hanya valid bila **jumlah & urutan pemanggilan `rng.Next`** identik antara baseline dan jalur baru. Jika partisi all-null memanggil sub-fungsi sekali atas seluruh kolam (1 grup "Lainnya"), urutan konsumsi RNG harus sama persis dengan baseline. Verifikasi: jalur all-null TIDAK boleh menambah pemanggilan RNG ekstra (mis. sort section, hitung key) yang menggeser stream. Lihat **Common Pitfalls → Pitfall 2**.

### Pattern 3: Precedence induk/anak (D-14) — SHF-02
**What:** Gate di entry: induk `shuffleQuestions==false` → SEMUA section ordered (`Question.Order`), abaikan `ShuffleEnabled` per-section. Induk `true` → tiap section ikut `ShuffleEnabled`-nya. Identik untuk `shuffleOptions` (option dict di-gate `ShuffleEnabled` section yang sama, bukan flag terpisah — D-416-01).
**Example:**
```csharp
// Per-section effective flag = induk AND ShuffleEnabled-section.
bool sectionShuffleQuestions = parentShuffleQuestions && section.ShuffleEnabled;
bool sectionShuffleOptions   = parentShuffleOptions   && section.ShuffleEnabled; // SATU gate (D-416-01)
// "Lainnya" (null section, no row) → ShuffleEnabled dianggap ikut induk → effective = parent flag. (D-15)
```

### Anti-Patterns to Avoid
- **Membangun pool global lalu memfilter per-section setelah Fisher-Yates:** merusak isolasi (soal bisa "bocor" antar-section saat sampling K). Partisi HARUS sebelum sampling. [CITED: spec §14 risk]
- **Mengubah signature `BuildQuestionAssignment` publik:** memaksa 3 call-site berubah serentak + risiko drift workerIndex. Pertahankan signature; injeksi logika section di dalam. [VERIFIED: 3 call-site IDENTIK — CMPController.cs:1130, AssessmentAdminController.cs:6044/6158/2554]
- **Menambah kolom DB / migration:** ditolak D-416-01. `ShuffleEnabled` sudah ada (415). migration=FALSE invariant.
- **Menyimpan PageNumber/section per soal di assignment:** OUT (itu 417). 416 hanya hasilkan `ShuffledQuestionIds` (urutan) + `ShuffledOptionIdsPerQuestion`. [CITED: spec §7.2, D-11]
- **Memblokir mulai ujian saat K < distinct-ET:** ditolak D-416-03. Hanya warning di UI kelola Section. Section kecil = konfigurasi sah.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| ET-aware K-min sampling (Phase 1/2/3) | Algoritma sampling baru dari nol | Reuse `BuildCrossPackageAssignment` existing sebagai callee per-section | Sudah teruji (Phase 373, ~180 test), CANONICAL, preserve SC#1. Tinggal beri subset section. [VERIFIED: ShuffleEngine.cs:91] |
| Fisher-Yates | Implementasi shuffle sendiri | `ShuffleEngine.Shuffle<T>(list, rng)` existing | Single source, sudah benar (i..1, swap). [VERIFIED: ShuffleEngine.cs:25] |
| Kunci "Lainnya" (null section) | `Dictionary<int?,int>` (lempar saat key null) | `SectionStructureComparer.KeyOf(null) → LainnyaKey` | Bug laten null-key sudah di-fix 415 (escalation validate-phase); reuse helper, JANGAN re-introduce null-key 500. [VERIFIED: SectionStructureComparer.cs:19-22] |
| Section-mismatch guard sebelum assign | Re-implement perbandingan struktur | `SectionStructureComparer.MismatchedSections` (sudah dipanggil StartExam re-guard) | Sudah ada di StartExam ~L1110 (415 SEC-04); 416 tak perlu ubah — assignment hanya jalan setelah guard lolos. [VERIFIED: CMPController.cs:1110] |
| Sibling-set + workerIndex | Hitung ulang urutan worker | `SiblingSessionQuery.SiblingPrePostAwarePredicate` + `OrderBy(id)` existing | Invariant Phase 373; ketiga call-site sudah pakai pola identik → konsistensi cross-call. [VERIFIED: CMPController.cs:1038, AssessmentAdminController.cs:6017/6098/2540] |

**Key insight:** 416 adalah **80% reuse, 20% partisi**. Bahaya terbesar adalah *menulis ulang* algoritma ET-aware alih-alih membungkusnya per-section. Algoritma inti tidak boleh berubah; yang berubah hanya **cakupan input** (seluruh kolam → slice per section) dan **concat output**.

---

## Common Pitfalls

### Pitfall 1: Soal bocor antar-Section (silent correctness bug)
**What goes wrong:** Sampling K mengambil soal dari Section lain saat satu Section kekurangan soal, atau Fisher-Yates jalan atas pool global → urutan campur antar-section.
**Why it happens:** Partisi dilakukan setelah sampling, atau fallback "ambil sisa soal manapun" (ShuffleEngine.cs:216-225) lintas-section.
**How to avoid:** Partisi SEBELUM sampling; fallback "ambil sisa" dibatasi pada slice section saja. Tiap `BuildSectionQuestionAssignment` HANYA menerima soal section-nya.
**Warning signs:** Test isolasi: assert setiap ID hasil section X punya `SectionId` yang map ke `SectionNumber` X (atau null untuk "Lainnya"); assert urutan global = blok-blok per section tanpa interleave.

### Pitfall 2: RNG stream drift merusak golden-order
**What goes wrong:** Jalur all-null memanggil `rng.Next` dengan jumlah/urutan berbeda dari baseline → list ID berbeda walau logika "setara".
**Why it happens:** Langkah partisi/sort section (mis. `OrderBy(_ => rng.Next())` tak sengaja, atau hitung key yang konsumsi RNG) menggeser stream sebelum sampling.
**How to avoid:** Untuk all-null (1 grup "Lainnya"), JANGAN konsumsi RNG di luar sub-fungsi. Partisi & sort section pakai operasi deterministik non-RNG. Verifikasi golden-order test dengan seed tetap (`new Random(42)`) — bila gagal, audit setiap `rng.Next`. Catatan: di produksi `Random.Shared` (non-seeded) → golden-order HANYA test-time invariant; produksi tetap acak antar peserta (by design).
**Warning signs:** Test `AllNullSection_ProducesIdenticalOrderToLegacyBaseline` gagal walau set ID sama tapi urutan beda.

### Pitfall 3: `Section` tidak ter-Include → SectionId terbaca null palsu
**What goes wrong:** Engine baca `q.Section?.SectionNumber`; bila navigasi `Section` tak ter-`Include`, lazy-loading mati (atau null) → semua soal jatuh ke "Lainnya" → scoped shuffle senyap tak aktif.
**Why it happens:** Call-site existing hanya `.ThenInclude(q => q.Options)`, bukan `q.Section`.
**How to avoid:** Baca `q.SectionId` (kolom skalar, SELALU ter-load) untuk partisi key, BUKAN `q.Section.SectionNumber` (navigasi). Map `SectionId → SectionNumber` via lookup dari `AssessmentPackageSection` yang ter-load. ATAU tambahkan `.ThenInclude(q => q.Section)` di 3 call-site. **Rekомendasi: pakai `SectionId` skalar untuk partisi + sediakan map SectionNumber dari sections yang di-Include sekali.** [VERIFIED: PackageQuestion.SectionId skalar ada — AssessmentPackage.cs:99; StartExam re-guard sudah `.ThenInclude(q => q.Section)` di guard block CMPController.cs:1086 tapi assignment block L1050 BELUM]
**Warning signs:** UAT: assessment ber-section tapi soal teracak lintas-section (perilaku = all-null).

### Pitfall 4: Option-shuffle masih di-gate flag induk tunggal (bukan per-section)
**What goes wrong:** `BuildOptionShuffle(questions, shuffleOptions, rng)` existing menerima SATU bool global → opsi semua soal teracak/tidak seragam, mengabaikan `ShuffleEnabled` per-section (langgar D-416-01).
**Why it happens:** Call-site existing mem-pass `assessment.ShuffleOptions` global.
**How to avoid:** Filter `assignedQuestions` per-section dan gate dengan `parentShuffleOptions && section.ShuffleEnabled`. Bisa: panggil `BuildOptionShuffle` per-section, atau bangun dict gabungan dengan keputusan per-soal. Caller (3 tempat) wire dict gabungan ke `ShuffledOptionIdsPerQuestion`.
**Warning signs:** Section OFF tapi opsi soalnya teracak (atau sebaliknya).

### Pitfall 5: AddParticipantsLive drift (peserta baru beda assignment)
**What goes wrong:** `CreateEagerAssignmentsAsync` memanggil engine versi lama / load tanpa section → peserta yang ditambah live dapat distribusi beda dari peserta StartExam normal.
**Why it happens:** Tiga call-site harus di-update seragam; mudah lupa EagerAssign (di AssessmentAdminController, bukan CMP).
**How to avoid:** Karena ketiganya memanggil entry `BuildQuestionAssignment` IDENTIK, satu refactor engine + samakan `.ThenInclude(q => q.Section)` (atau pakai SectionId skalar) di ketiga load → otomatis konsisten. **Checklist plan: ketiga call-site WAJIB pakai jalur load + engine yang sama.** [VERIFIED: 3 call-site — CMPController.cs:1050, AssessmentAdminController.cs:6022/6102/2546; spec §15.G AddParticipantsLive WAJIB per-section sama]
**Warning signs:** UAT: tambah peserta live setelah ujian mulai → bandingkan distribusi section-nya vs peserta awal (harus pola-konsisten).

### Pitfall 6: Tes lama (≥2 paket all-null) jadi merah
**What goes wrong:** ~180 metode shuffle existing mengasumsikan list datar; refactor menggeser urutan untuk dataset all-null.
**Why it happens:** Jalur all-null tak benar-benar identik baseline.
**How to avoid:** Spec §12: JANGAN retrofit tes lama; mereka adalah **kontrak golden-order**. Bila `ShuffleEngineTests.cs` (Phase 373) merah → jalur all-null SALAH, perbaiki engine, BUKAN tesnya. Suite BARU hanya untuk skenario ber-section.
**Warning signs:** `ShuffleEngineTests` regresi.

---

## Code Examples

### Membaca toggle & wiring di call-site (pola yang dipertahankan)
```csharp
// Source: CMPController.cs:1130-1138 (StartExam) [VERIFIED] — pola IDENTIK di Reshuffle*/EagerAssign
var shuffledIds = ShuffleEngine.BuildQuestionAssignment(
    packages, assessment.ShuffleQuestions, workerIndex, rng);   // signature DIPERTAHANKAN

var assignedQuestions = packages.SelectMany(p => p.Questions)
    .Where(q => shuffledIds.Contains(q.Id));
var optionShuffleDict = ShuffleEngine.BuildOptionShuffle(
    assignedQuestions, assessment.ShuffleOptions, rng);          // 416: gate per-section di dalam
```
> 416 mengubah **internal** `BuildQuestionAssignment` (partisi) + perlakuan option-gate per-section, **bukan** bentuk panggilan. Load wajib tambah akses ke `SectionId`/sections (lihat Pitfall 3).

### Load packages — tambahan yang diperlukan (3 call-site)
```csharp
// Source: CMPController.cs:1050-1055 [VERIFIED] — assignment block BELUM include Section
var packages = await _context.AssessmentPackages
    .Include(p => p.Questions)
        .ThenInclude(q => q.Options)
    // 416: pastikan partisi punya SectionNumber. Opsi A: .ThenInclude(q => q.Section)
    //      Opsi B (preferred): load sections paket sekali + map SectionId→SectionNumber (hindari N+1).
    .Where(p => siblingSessionIds.Contains(p.AssessmentSessionId))
    .OrderBy(p => p.PackageNumber)
    .ToListAsync();
```

### Reuse SectionStructureComparer key (null-safe "Lainnya")
```csharp
// Source: SectionStructureComparer.cs:19-22 [VERIFIED]
int key = SectionStructureComparer.KeyOf(q.SectionId == null ? (int?)null : sectionNumberOf[q.SectionId.Value]);
// Lainnya selalu terakhir: OrderBy(k => k == LainnyaKey).ThenBy(k => k)  (sama pola comparer)
```

### ET-coverage warning (D-416-03) — controller computation
```csharp
// Source: AssessmentAdminController.cs:7637-7662 (ManagePackageQuestions GET) [VERIFIED]
// Tambah: per Section, hitung distinct ET vs K (jumlah soal section). Warning bila K < distinctET.
ViewBag.SectionEtWarnings = sections.Select(s => {
    var qs = pkg.Questions.Where(q => q.SectionId == s.Id).ToList();
    int k = qs.Count;
    int distinctEt = qs.Where(q => !string.IsNullOrWhiteSpace(q.ElemenTeknis))
                       .Select(q => q.ElemenTeknis!).Distinct().Count();
    return new { s.SectionNumber, s.Name, K = k, DistinctEt = distinctEt, Warn = distinctEt > k };
}).Where(x => x.Warn).ToList();
// View render badge/alert: "Section {n}: {distinctEt} Elemen Teknis tapi hanya {K} soal —
//   sebagian ET tak terjamin muncul." (non-blocking, sinyal)
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Acak global (1 kolam seluruh assessment) | Acak per-Section (partisi `(SectionNumber, ET)`) | Phase 416 (milestone v32.6) | Soal tak melompat antar-Section; legacy all-null = identik |
| Toggle shuffle hanya level-assessment (`ShuffleQuestions`/`ShuffleOptions`) | + toggle `ShuffleEnabled` per-Section (anak, D-14) | 415 (kolom) + 416 (konsumsi) | HC kontrol acak granular per-Section |
| Option-shuffle gate flag induk tunggal | Gate per-Section (`induk ∧ ShuffleEnabled`) | Phase 416 (D-416-01) | Opsi ikut keputusan Section, no kolom baru |

**Deprecated/outdated:**
- Tidak ada deprecation. Algoritma inti `BuildCrossPackageAssignment` DIPERTAHANKAN (jadi callee per-section). Komentar/header `ShuffleEngine.cs` perlu di-update menyebut scoping per-Section.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Mempertahankan signature `BuildQuestionAssignment` (vs entry baru) adalah pendekatan minimal-blast-radius terbaik | Standard Stack → Alternatives | Rendah — discretion user (D-416 Claude's Discretion); planner boleh putuskan entry baru, asal output all-null identik. Bukan compliance/security. |
| A2 | Pakai `q.SectionId` skalar + map SectionNumber (vs `.ThenInclude(q=>q.Section)`) untuk hindari N+1 | Code Examples / Pitfall 3 | Rendah — keduanya benar; pilihan performa. Kebenaran terjaga selama partisi pakai key section yang konsisten. |
| A3 | Suite test BARU terpisah (bukan retrofit ~180 test lama) sesuai §12 | Validation Architecture | Rendah — eksplisit di spec §12 [CITED]. |

**Catatan:** Tidak ada asumsi compliance/retention/security. Semua keputusan inti sudah LOCKED di CONTEXT (D-416-01..05) + spec (D-09/D-14/D-15). Risiko residual murni teknis-implementasi, di-cover test.

---

## Open Questions

1. **Slicing per-section: clone objek vs proyeksi ID?**
   - What we know: `BuildCrossPackageAssignment` menerima `List<AssessmentPackage>` dan baca `.Questions`. Untuk per-section perlu "sub-packages" berisi hanya soal section.
   - What's unclear: apakah lebih bersih membangun `AssessmentPackage` shallow-clone dengan `Questions` ter-filter, atau memfaktorkan ulang core agar terima `List<List<PackageQuestion>>` (per-paket, soal section).
   - Recommendation: faktor ulang core internal agar beroperasi pada koleksi soal per-paket (hindari clone objek EF yang rawan). Keputusan teknis planner; tidak mempengaruhi kontrak/UAT.

2. **Apakah K per-section perlu re-guard runtime di engine, atau cukup andalkan guard 415?**
   - What we know: K per-section dijamin sama antar-paket + K>0 oleh D-13, di-enforce import (415) + StartExam re-guard (CMPController.cs:1092-1122).
   - What's unclear: bila drift lolos guard (edge), engine harus aman (tak crash, tak bocor).
   - Recommendation: engine defensif (`packages.Min` per-section, guard count==0 → skip section, JANGAN throw). Sama pola existing ShuffleEngine.cs:117-119. Tidak menambah blocking baru (D-416-03 best-effort).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | build + test | ✓ (project existing) | net8.0 | — |
| xUnit + Test SDK | unit test engine | ✓ | 2.9.3 / 17.13.0 | — |
| SQL Server lokal (`HcPortalDB_Dev`) | Playwright UAT + cek DB lokal (CLAUDE.md) | ✓ (workflow existing) | — | engine unit-test tak butuh DB |
| Playwright + Chromium | UAT real-browser (D-416-05) | ✓ (`tests/e2e/` ada) | existing | `npx playwright install chromium` bila browser hilang [VERIFIED: shuffle.spec.ts:22] |
| App @ `http://localhost:5277` | UAT (CLAUDE.md verifikasi lokal) | runtime | — | — |

**Missing dependencies with no fallback:** Tidak ada. Phase 416 = perubahan kode murni + test; nol dependensi eksternal baru, nol migration.

**Missing dependencies with fallback:** Tidak ada.

---

## Validation Architecture

> nyquist_validation enabled (config absent → treated as enabled).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (.NET 8) + Playwright (e2e) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` ; `tests/playwright.config.*` |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ShuffleEngine|FullyQualifiedName~SectionScopedShuffle"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` ; e2e: `cd tests && npx playwright test scoped-shuffle.spec.ts --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SHF-01 | All-null = urutan IDENTIK baseline (golden-order) | unit | `dotnet test --filter "AllNullSection_ProducesIdenticalOrderToLegacyBaseline"` | ❌ Wave 0 |
| SHF-01 | Soal tak bocor antar-Section (isolasi: tiap ID hasil ∈ section-nya) | unit | `dotnet test --filter "ScopedShuffle_NoCrossSectionLeak"` | ❌ Wave 0 |
| SHF-01 | Urutan global = blok per-Section, "Lainnya" terakhir (D-15) | unit | `dotnet test --filter "SectionOrder_LainnyaAlwaysLast"` | ❌ Wave 0 |
| SHF-02 | Induk OFF → semua section ordered (per-section toggle diabaikan, D-14) | unit | `dotnet test --filter "Precedence_ParentOff_AllOrdered"` | ❌ Wave 0 |
| SHF-02 | Induk ON → section ShuffleEnabled=OFF tetap urut Order; ON teracak | unit | `dotnet test --filter "Precedence_ParentOn_PerSectionToggle"` | ❌ Wave 0 |
| SHF-02 | Option-shuffle di-gate `ShuffleEnabled` section (D-416-01) | unit | `dotnet test --filter "OptionShuffle_GatedPerSection"` | ❌ Wave 0 |
| SHF-03 | >1 paket: ET-coverage per-Section (1-per-(Section,ET) sampai K) | unit | `dotnet test --filter "MultiPackage_EtCoveragePerSection"` | ❌ Wave 0 |
| SHF-03 | ET lintas-section dijamin tercakup tiap section terpisah | unit | `dotnet test --filter "EtSpanningSections_CoveredIndependently"` | ❌ Wave 0 |
| SHF-03 | Determinisme: workerIndex sama + seed sama → urutan sama | unit | `dotnet test --filter "Determinism_WorkerIndexStable"` | ❌ Wave 0 |
| SHF-04 | Reshuffle = re-roll dalam batas Section (no leak) | unit | `dotnet test --filter "Reshuffle_SectionIsolation"` | ❌ Wave 0 |
| SHF-04 | AddParticipantsLive eager-assign pakai jalur section-aware sama | integration | `dotnet test --filter "EagerAssign_SectionAwareParity"` | ❌ Wave 0 |
| D-416-03 | K < distinct-ET → ViewBag warning (non-blocking, mulai ujian tetap jalan) | unit/integration | `dotnet test --filter "EtCoverageWarning_NonBlocking"` | ❌ Wave 0 |
| D-416-05 | Real-browser: assessment ber-section → soal teracak per-section, tak lompat | e2e (manual-assisted) | `cd tests && npx playwright test scoped-shuffle.spec.ts --workers=1` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~ShuffleEngine|FullyQualifiedName~SectionScopedShuffle"` (engine murni, < 5 dtk) + `dotnet build`.
- **Per wave merge:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (full xUnit — **tes lama Phase 373 WAJIB tetap hijau** = golden-order kontrak).
- **Phase gate:** Full suite green + Playwright `scoped-shuffle.spec.ts` green sebelum `/gsd-verify-work`; cek DB lokal + app @5277 (CLAUDE.md).

### Wave 0 Gaps
- [ ] `HcPortal.Tests/SectionScopedShuffleTests.cs` — covers SHF-01/02/03/04 + D-416-03 (engine murni, fixture in-memory pola `ShuffleEngineTests.Pkg(...)` diperluas dengan `SectionId`/`AssessmentPackageSection`).
- [ ] Helper fixture: builder paket ber-section in-memory (extend `Pkg(...)` ShuffleEngineTests.cs:18 untuk terima sectionNumber + ShuffleEnabled per soal/section).
- [ ] `LegacyBuildCrossPackageAssignment` snapshot/baseline untuk golden-order (bekukan output core lama atau capture list literal sekali).
- [ ] `tests/e2e/scoped-shuffle.spec.ts` — UAT real-browser (D-416-05); pola `tests/e2e/shuffle.spec.ts` (DB backup/restore beforeAll/afterAll, `--workers=1`, seed assessment ber-section via wizard + SQL).
- [ ] (opsional) integration test `CreateEagerAssignmentsAsync` parity — bisa unit bila core di-ekstrak; bila butuh DB pakai `EntityFrameworkCore.InMemory` (sudah tersedia).

*Framework sudah terpasang; tidak perlu install baru.*

---

## Security Domain

> security_enforcement enabled (absent = enabled).

Phase 416 = refactor algoritma server-side internal. **Tidak ada surface input baru** (toggle `ShuffleEnabled` sudah ada + ter-validasi di 415; tak ada endpoint baru). Permukaan serangan praktis nol di luar yang sudah di-secure 415.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ada auth baru; reshuffle endpoints sudah `[Authorize(Roles="Admin,HC")]`. [VERIFIED: AssessmentAdminController.cs:5991] |
| V3 Session Management | no | — |
| V4 Access Control | yes (existing) | Reshuffle + Section CRUD sudah `[Authorize]` + `[ValidateAntiForgeryToken]` + IDOR guard (section milik paket). 416 tak menambah endpoint. [VERIFIED: 5990-5992, CreateQuestion section IDOR 7711] |
| V5 Input Validation | no (new) | `ShuffleEnabled` (bool) di-handle 415; engine terima objek in-memory (tak ada parsing user-input baru). |
| V6 Cryptography | no | `Random` untuk shuffle = non-cryptographic by design (bukan secret); grading by `PackageOption.Id` (posisi opsi tak pengaruhi nilai). [VERIFIED: PackageOption.cs:118-123] |

### Known Threat Patterns for net8.0 / exam-shuffle
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Soal bocor antar-Section (integritas konten ujian) | Tampering / Information Disclosure | Isolasi per-section di engine + test invariant (Pitfall 1) — utama temuan 416 |
| Reshuffle oleh non-HC | Elevation of Privilege | Sudah `[Authorize(Roles="Admin,HC")]` + antiforgery (existing) |
| Manipulasi opsi → ubah nilai | Tampering | Grading by `PackageOption.Id`, bukan posisi huruf → acak opsi TAK pengaruhi skor (sudah aman) |
| Race double-create assignment (double-click) | — (robustness) | Sudah ditangani `DbUpdateException` retry di StartExam (CMPController.cs:1163) — tak berubah 416 |

---

## Sources

### Primary (HIGH confidence)
- `Helpers/ShuffleEngine.cs` (full read) — algoritma inti + 3 fase + entry/option signature. [VERIFIED]
- `Controllers/CMPController.cs` L1025-1254 (StartExam wiring + section re-guard 415). [VERIFIED]
- `Controllers/AssessmentAdminController.cs` L2531-2570 (CreateEagerAssignmentsAsync), L5985-6196 (ReshufflePackage/ReshuffleAll), L6279-6450 (Section CRUD), L7637-7711 (ManagePackageQuestions GET + CreateQuestion). [VERIFIED]
- `Models/AssessmentPackage.cs` (AssessmentPackageSection.ShuffleEnabled, PackageQuestion.SectionId). [VERIFIED]
- `Helpers/SectionStructureComparer.cs` (null-safe LainnyaKey). [VERIFIED]
- `HcPortal.Tests/ShuffleEngineTests.cs` (pola test pure in-memory + fixed-seed determinism). [VERIFIED]
- `HcPortal.Tests/HcPortal.Tests.csproj` (versi framework). [VERIFIED]
- `tests/e2e/shuffle.spec.ts` (pola Playwright UAT existing). [VERIFIED]
- `docs/superpowers/specs/2026-06-22-...-design.md` §6/§6.2/§6.3/§6.4/§7.2/§12/§13/§14/§15.A/§15.E/§15.F/§15.G + Decision Log D-09/D-13/D-14/D-15. [CITED]
- `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md` (baseline toggle induk + reshuffle fix). [CITED]
- `.planning/phases/416-.../416-CONTEXT.md` (D-416-01..05). [VERIFIED]
- `.planning/phases/415-.../415-CONTEXT.md` + `.planning/REQUIREMENTS.md` (SHF-01..04, SEC complete). [VERIFIED]
- `.planning/STATE.md` (roadmap v32.6, sequential by file-overlap, §13 abstraksi belum diekstrak). [VERIFIED]
- `CLAUDE.md` (develop + seed workflow). [VERIFIED]

### Secondary (MEDIUM confidence)
- (tidak ada — semua klaim tervalidasi langsung dari kode/spec lokal)

### Tertiary (LOW confidence)
- (tidak ada)

---

## Project Constraints (from CLAUDE.md)

- **Bahasa:** Selalu respon Bahasa Indonesia; teks UI (warning ET-coverage) dalam BI; kode/identifier English (konsisten existing).
- **Develop workflow:** reproduce+fix di lokal → verifikasi `dotnet build` + `dotnet test` + `dotnet run` @ `http://localhost:5277` + cek DB lokal + Playwright (UI/JS ada) → commit & push → promosi Dev/Prod = tanggung jawab IT. **JANGAN edit kode/DB di Dev/Prod. JANGAN push tanpa verifikasi lokal.**
- **Migration flag:** 416 = **migration=FALSE** (notify IT). Sertakan commit hash + flag saat handoff.
- **Seed data workflow:** UAT Playwright butuh seed → klasifikasi (temporary+local-only) → snapshot DB (`BACKUP DATABASE`) → catat `docs/SEED_JOURNAL.md` → restore + tandai cleaned setelah test. Pola sudah ada di `tests/e2e/shuffle.spec.ts` (beforeAll backup / afterAll restore).

*Project skills:* `.claude/skills/` / `.agents/skills/` — tidak ditemukan SKILL.md untuk repo ini (tak ada direktori skills proyek; konvensi diambil dari CLAUDE.md + pola kode existing).

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — nol paket baru; versi dibaca langsung dari csproj.
- Architecture: HIGH — refactor di atas kode yang dibaca penuh; pola section-partition lurus dari spec §6.2 + algoritma existing.
- Pitfalls: HIGH — semua pitfall diturunkan dari kode aktual (RNG stream, Section Include, option-gate, 3 call-site, golden-order kontrak Phase 373).
- Security: HIGH — tak ada surface baru; existing controls verified.

**Research date:** 2026-06-23
**Valid until:** ~2026-07-23 (stabil — internal refactor; tergantung Phase 415 sudah merge & kolom `ShuffleEnabled` ada, yang sudah terverifikasi di model).
