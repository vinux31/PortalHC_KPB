# Phase 420: Form Create/Edit — Persistensi Field + UX Pre-Post - Research

**Researched:** 2026-06-22
**Domain:** ASP.NET Core MVC (.NET 8) Razor form binding + Bootstrap 5.3 redesign (BUKAN greenfield — bug-fix + UX redesign pada form existing)
**Confidence:** HIGH (semua temuan diverifikasi langsung terhadap source file:line dalam sesi ini)

> Catatan untuk semua agent hilir: ini fase yang TERIKAT KODE NYATA. Setiap klaim di bawah punya `file:line` yang sudah diverifikasi (grep/Read) pada sesi 2026-06-22. JANGAN riset framework dari nol — gunakan peta file:line ini sebagai basis tugas.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**D-01 — Fix bug "Acak Soal/Pilihan reset OFF tiap Edit" (E-01, FORM-01)**
Render toggle Acak Soal & Acak Pilihan di `EditAssessment.cshtml`, terisi dari `Model` (nilai tersimpan). Pertahankan write di POST Edit. Hasil: shuffle dapat diatur konsisten via 3 jalur (Create + Edit + ManagePackages/`UpdateShuffleSettings`) tanpa saling menimpa secara diam-diam.
- Catatan integrasi: pastikan Edit & `UpdateShuffleSettings` tidak race — nilai yang dirender di Edit harus mencerminkan state terkini; saat HC submit Edit, nilai checkbox = sumber kebenaran untuk sesi + sibling.

**D-02 — Penyajian scope setelan di mode Pre-Post (FORM-08)**
Pisah jadi dua sub-kartu di Step 3 saat mode Pre-Post aktif:
- "Setelan Post-Test" → Nilai Lulus (PassPercentage), Sertifikat (GenerateCertificate + ValidUntil), Ujian Ulang (lihat D-03).
- "Setelan Bersama Pre & Post" → Acak Soal/Pilihan, Izinkan Review Jawaban, Token.
- Mode Standard: layout setelan tetap seperti sekarang (tak ada pemisahan).

**D-03 — Retake & Nilai-Lulus untuk Pre baseline (FORM-11)**
Mode Pre-Post → kontrol Ujian Ulang DISEMBUNYIKAN (retake hanya relevan untuk Post); PassPercentage untuk Post; Pre = baseline murni (tanpa lulus/gagal/retake). Baris Status/PassPercentage yang sebelumnya timpang dirapikan. Mode Standard: kontrol retake/pass tetap tampil seperti sekarang.

**D-04 — Letak SamePackage + input standard tersembunyi (FORM-07, FORM-09)**
- SamePackage pindah ke header section Pre-Post (dekat pemilih Tipe Assessment / di atas kartu Pre & Post), bukan terkubur di kartu Post.
- Input jadwal/durasi/EWCD standard TIDAK ikut ter-POST saat mode Pre-Post (hapus dari payload yang dikirim, bukan sekadar `d-none`). Saat Standard, input Pre/Post yang tidak terpakai juga tidak dikirim.

### Claude's Discretion
- **FORM-10 (rename `AssessmentTypeInput`):** rename parameter/penanda internal agar tidak rancu dengan kolom DB `AssessmentType` (mis. `CreationMode` Standard/PrePostTest); label UI "Tipe Assessment" boleh tetap; perbarui XML-doc `AssessmentSession.AssessmentType` yang usang. Pendekatan teknis bebas asal binding tidak putus.
- **FORM-05 (lock Completed):** blok perubahan metadata bila sesi target — atau, untuk Pre-Post, grup pasangannya — sudah `Completed` (group-aware default). Planner tentukan presisi & posisi guard (idealnya sebelum cabang Pre-Post di POST Edit; lihat audit E-04).
- **FORM-02/03/04/06 (perbaikan persistensi/redirect):** ikuti pola existing — FORM-02 mirror penyalinan eksplisit pada bulk-add (`AssessmentAdminController.cs:2184-2186`); FORM-06 mirror filter `IsManualEntry` di `TrainingAdminController` EditManualAssessment GET (`:994`).
- **Backward-compat WAJIB:** mode Standard tidak berubah perilaku; hanya mode Pre-Post mendapat layout baru (sub-kartu + SamePackage header + sembunyikan retake).

### Deferred Ideas (OUT OF SCOPE)
- **Overlap v32.6 (branch main):** redesign form yang sama disentuh Section/Opsi-Dinamis (fase 415-419 di main). Rekonsiliasi saat merge — JANGAN tarik scope Section/Opsi-Dinamis ke fase 420.
- Tidak ada scope creep lain — diskusi tetap dalam batas fase (form persistensi + UX Pre-Post).
- Logika retake (421), SamePackage sync/toggle backend (422), aturan terbit cert (423), grading/gating Pre→Post (424) = fase berikutnya. Fase 420 HANYA form/binding/layout.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support (file:line bukti + tempat fix) |
|----|-------------|------------------------------------------------|
| **FORM-01** | Acak Soal/Pilihan tidak reset OFF saat Edit [E-01, HIGH] | `EditAssessment.cshtml` ZERO shuffle (grep `shuffle\|acak`=0, **VERIFIED**); POST Edit menulis shuffle tanpa input → bind ke `false`. Fix: render dua `asp-for` switch di Edit Group "Pengaturan Ujian" (anchor sekitar `:414-438`, sebaris retake), copy idiom Create `CreateAssessment.cshtml:536-551`. Write existing di `AssessmentAdminController.cs:2084-2085` (std) & `:1852-1853` (Pre-Post) TETAP. |
| **FORM-02** | Retake config tersimpan saat Create (tidak default) [FLD-5.2-08] | Tiga jalur build Create TIDAK menyalin retake: std `:1467-1491`, Pre `:1243-1263`, Post `:1279-1303`. Fix: tambah 3 field `AllowRetake/MaxAttempts/RetakeCooldownHours = model.*` (std) + sesuai D-03 untuk Pre/Post. Pola mirror bulk-add `:2184-2186`. |
| **FORM-03** | Retake config tersimpan saat Edit (bukan no-op) [E-03] | Std loop `:2072-2089` TIDAK menulis retake (no-op; satu-satunya writer = `UpdateRetakeSettings`). Fix: tambah `sibling.AllowRetake/MaxAttempts/RetakeCooldownHours = model.*` + Clamp. |
| **FORM-04** | ValidUntil tersimpan di jalur standard Edit [E-05] | Std loop `:2072-2089` menulis `GenerateCertificate`(:2086)+`ExamWindowCloseDate`(:2087) TAPI **bukan** `ValidUntil` (**VERIFIED** absent). Fix: tambah `sibling.ValidUntil = model.ValidUntil;`. |
| **FORM-05** | Sesi Completed terkunci dari Edit (group-aware Pre-Post) [E-04] | Cabang Pre-Post POST `return :2001` SEBELUM guard `Status=="Completed"` `:2006-2010` (**VERIFIED**). Fix: angkat guard ke sebelum cabang Pre-Post (`:1821`), group-aware (cek anchor/sibling). |
| **FORM-06** | Edit sesi manual diarahkan ke form manual [E-08] | GET `EditAssessment :1684-1686` TANPA filter `IsManualEntry` (**VERIFIED**) vs `TrainingAdminController.cs:994` (`s.Id==id && s.IsManualEntry`). Fix: di GET, jika `assessment.IsManualEntry` → `RedirectToAction("EditManualAssessment","TrainingAdmin", new{id})`. |
| **FORM-07** | SamePackage di tingkat-pasangan, bukan di kartu Post [FORM-PP-01] | `CreateAssessment.cshtml:475` checkbox `name="SamePackage"` di DALAM kartu Post (`:452-483`). Fix: pindah ke header section Pre-Post (di bawah Type select `:213-223`, di atas `#ppt-jadwal-section`). |
| **FORM-08** | Setelan Pre-Post bertanda scope / dikelompokkan [FORM-PP-02] | Group B/C/D satu-instance dua-sesi tanpa scope. Fix: dua sub-kartu D-02 (lihat L-2). |
| **FORM-09** | Input standard tersembunyi tidak ter-POST saat Pre-Post [FORM-PP-03] | `#standard-jadwal-section :382-421` + hidden combiner `Schedule`/`ExamWindowCloseDate :424-425` tetap di DOM & ter-POST (cuma `d-none :2003`). Fix: disable/strip dari payload (lihat L-5). |
| **FORM-10** | Penamaan tipe assessment konsisten dgn kolom DB [FLD-5.2-01] | `AssessmentTypeInput` (Standard/PrePostTest) ≠ kolom DB `AssessmentType` (PreTest/PostTest/Standard/Manual). 8 ref controller + 9 ref view (lihat Runtime State Inventory). XML-doc `AssessmentSession.cs:170-171` usang (**VERIFIED**). |
| **FORM-11** | Tata-letak Pre-Post rapi (Status/PassPct, retake/pass tak ke Pre) [FORM-PP-05/06] | `statusFieldWrapper :496` disembunyikan saat Pre-Post (JS `:2005`), tetangga `PassPercentage :508` jadi setengah-baris asimetris. Fix: sub-kartu Post (L-2/L-3). |
</phase_requirements>

## Summary

Fase 420 BUKAN proyek baru — ini perbaikan pola "field dirender tapi tak tersimpan" plus redesign tata-letak form Pre-Post yang sudah ada (`Views/Admin/CreateAssessment.cshtml` 2086 baris + `Views/Admin/EditAssessment.cshtml` 940 baris + `Controllers/AssessmentAdminController.cs`). Stack sudah terkunci: ASP.NET Core MVC tag-helper (`asp-for`) + Bootstrap 5.3.0 CDN + Bootstrap Icons 1.10.0. Tidak ada library baru, tidak ada migration (binding/view/controller saja). Akar masalah teknis seragam: **mismatch antara field yang dirender form dan field yang disalin/ditulis controller saat membangun atau memperbarui `AssessmentSession`** — kadang field dirender tapi tak ditulis (E-03/E-05), kadang field ditulis tapi tak dirender (E-01, paling berbahaya: bind ke `false` → silent data-loss).

Semua 11 temuan sudah diverifikasi langsung ke source dalam sesi ini (file:line di tabel Phase Requirements). Tiga temuan persistensi (FORM-01/02/03/04) bisa diperbaiki dengan menambah baris penyalinan eksplisit di tiga lokasi yang sudah dikenal pasti; pola kanonik sudah ada di codebase (`bulk-add :2184-2186` menyalin retake eksplisit). Dua temuan guard/redirect (FORM-05/06) butuh memindahkan/menambah guard di posisi yang tepat (cabang Pre-Post `return :2001` mendahului guard `:2006` = E-04; GET tanpa filter manual = E-08). Lima temuan UX Pre-Post (FORM-07..11) adalah redesign view + perluasan JS toggle yang sudah dikunci kontraknya di `420-UI-SPEC.md` (L-1..L-6 + Visibility Matrix).

**Risiko utama = regresi mode Standard.** Mode Standard WAJIB tidak berubah perilaku (backward-compat). Toggle JS `:1986-2033` adalah satu-satunya pengendali per-mode; memperluasnya (sub-kartu, hide-retake, strip-payload) harus menjaga jalur Standard menghasilkan DOM + payload identik. Test infrastruktur sudah matang: pola xUnit real-SQL `[Trait("Category","Integration")]` (mereplikasi body persistensi controller) + Playwright e2e serial dengan DB snapshot/restore. Manfaatkan keduanya, JANGAN bangun harness baru.

**Primary recommendation:** Perbaiki persistensi (FORM-01..04) sebagai wave pertama (paling berisiko-tinggi tapi paling kecil-perubahannya — tambah baris di lokasi pasti + xUnit persistence test per field), lalu guard/redirect (FORM-05/06), lalu redesign view+JS Pre-Post (FORM-07..11) sebagai wave terakhir dengan Playwright per-mode. Rename `AssessmentTypeInput`→`CreationMode` (FORM-10) dilakukan atomik di SATU sapuan (8 ref controller + 9 ref view) untuk menghindari binding putus.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Persistensi shuffle/retake/ValidUntil (Create+Edit) | API/Backend (`AssessmentAdminController` build/update loops) | — | Penulisan field ke `AssessmentSession` = server-authoritative; checkbox absen bukan alasan default-false. |
| Render field dari state tersimpan (Edit) | Frontend SSR (Razor `asp-for` di EditAssessment.cshtml) | — | `asp-for="X"` mengisi nilai dari `Model.X`; akar E-01 = field tidak dirender sama sekali. |
| Toggle visibilitas per-mode (Standard/Pre-Post) | Browser/Client (JS `:1986-2033`) | Frontend SSR (markup d-none default) | Per-mode = interaksi runtime; satu-satunya pengendali. Strip-payload (D-04) juga di sini. |
| Guard lock Completed | API/Backend (POST Edit guard, pre-cabang) | Frontend SSR (GET redirect manual) | Lock metadata = keputusan server; client hide hanya UX. |
| Redirect manual-entry | API/Backend (GET `EditAssessment`) | — | Routing keputusan = server (`IsManualEntry` filter). |
| Sibling propagation (shuffle/retake/cert) | API/Backend (foreach siblings) | — | Group consistency = server transaction. |

**Catatan tier:** Tidak ada tugas yang perlu pindah tier. Semua sudah di tier yang benar; bug-nya adalah field hilang/tak-tertulis DI DALAM tier yang benar. Satu-satunya keputusan tier baru: D-04 strip-payload — dikerjakan di Browser/Client (JS disable input sebelum submit), BUKAN server abaikan (server abaikan lebih rapuh karena binding model penuh).

## Standard Stack

**TIDAK ADA package baru.** Ini fase redesign + bug-fix pada stack existing yang terkunci. Tabel di bawah mendokumentasikan stack yang DIPAKAI (bukan yang ditambah).

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | .NET 8.0 (SDK 8.0.418 terverifikasi `dotnet --version`) | Razor view + model binding (`asp-for`) + controller actions | Framework existing proyek; semua form sudah pakai tag-helper. [VERIFIED: dotnet --version] |
| EF Core | 8.0.0 (snapshot ProductVersion) | Persistensi `AssessmentSession` | ORM existing; SaveChangesAsync di controller. [VERIFIED: codebase MEMORY 405-01] |
| Bootstrap | 5.3.0 (CDN `_Layout.cshtml:38`) | Komponen UI: card, form-switch, form-check, badge, alert, collapse, nav-tabs | Design system terkunci (UI-SPEC §Design System). [CITED: 420-UI-SPEC.md] |
| Bootstrap Icons | 1.10.0 (CDN `_Layout.cshtml:39`) | Ikon `bi-*` (bi-shuffle, bi-arrow-repeat, bi-clock-history) | Icon library terkunci. [CITED: 420-UI-SPEC.md] |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | existing (`HcPortal.Tests`) | Unit + integration test persistensi | Tiap field-fix → 1 persistence test (lihat Validation Architecture). [VERIFIED: HcPortal.Tests/*.cs] |
| Playwright | existing (`tests/playwright.config.ts`) | e2e per-mode UI render | Verifikasi sub-kartu/SamePackage/shuffle-render. [VERIFIED: tests/playwright.config.ts] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| JS disable input (D-04 strip-payload) | Server abaikan field saat Pre-Post | Server-abaikan butuh logika cabang penuh di POST; rapuh + tak melindungi nilai stale ikut tervalidasi. JS disable = idiom HTML standar (`disabled` attr tidak ter-submit). |
| Dedicated guard FORM-05 | Reuse `AssessmentEditEligibility.IsEditableAsync` | **JANGAN reuse** — helper itu semantik TERBALIK: ia mengembalikan `true` HANYA bila `Status=="Completed"` (untuk edit jawaban peserta sesi selesai). FORM-05 mau MEMBLOKIR completed. Tulis guard sendiri. [VERIFIED: Helpers/AssessmentEditEligibility.cs:17-27] |

**Installation:** N/A — tidak ada package baru.

**Version verification:** dotnet SDK 8.0.418 terverifikasi via `dotnet --version`. Bootstrap/Icons via CDN pinned existing (`_Layout.cshtml:38-39`), tidak berubah.

## Architecture Patterns

### System Architecture Diagram (alur data form Pre-Post)

```
[HC isi form CreateAssessment Step-3]
            │
            │ pilih "Tipe Assessment" dropdown (name=AssessmentTypeInput, :215)
            ▼
   ┌─────────────────────────────────────┐
   │  JS toggle :1986-2033 (change event) │
   │  if value=='PrePostTest':            │
   │   - show #ppt-jadwal-section          │
   │   - hide #standard-jadwal-section     │──┐ D-04: strip std payload (disable inputs)
   │   - hide #statusFieldWrapper          │  │ L-5: disable Schedule/EWCD/Duration std
   │   - Status='Upcoming'                 │  │
   │   - [BARU] render 2 sub-kartu D-02    │  │
   │   - [BARU] hide retake block D-03     │  │
   │   - [BARU] show SamePackage header    │  │
   └─────────────────────────────────────┘  │
            │ submit (POST)                   │
            ▼                                 │
   ┌─────────────────────────────────────┐  │
   │ POST CreateAssessment(...)           │◄─┘ payload TANPA input standard saat Pre-Post
   │  AssessmentTypeInput=="PrePostTest"  │
   │  → cabang Pre-Post build :1240-1318   │
   │    Pre  :1243-1263 (GenCert=false)    │── [FORM-02 FIX] salin retake? D-03: Pre=false
   │    Post :1279-1303 (GenCert=model)    │── [FORM-02 FIX] salin retake ke Post
   │  else → std build :1467-1491          │── [FORM-02 FIX] salin retake std
   └─────────────────────────────────────┘
            │ SaveChangesAsync → AssessmentSession rows
            ▼
   [DB: AssessmentSessions (Pre+Post linked, atau Standard)]

──────────────── ALUR EDIT (terpisah) ────────────────
[HC buka EditAssessment/{id}]
            │
            ▼
   ┌─────────────────────────────────────┐
   │ GET EditAssessment :1682              │
   │  [FORM-06 FIX] if IsManualEntry →     │── RedirectToAction EditManualAssessment
   │  if Pre-Post group → tab layout :213  │
   │  else → single-mode view              │
   │  [E-01: shuffle TIDAK dirender]       │── [FORM-01 FIX] render asp-for shuffle
   └─────────────────────────────────────┘
            │ submit (POST)
            ▼
   ┌─────────────────────────────────────┐
   │ POST EditAssessment :1808             │
   │  [FORM-05 FIX] guard Completed HERE   │── group-aware, SEBELUM cabang Pre-Post
   │  if Pre-Post → branch :1821..return:2001
   │     shared loop :1846-1858 (shuffle ✓) │
   │     post loop :1870-1879 (ValidUntil✓) │
   │  else → std loop :2072-2089            │
   │     [FORM-04 FIX] + sibling.ValidUntil │
   │     [FORM-03 FIX] + sibling.retake×3   │
   └─────────────────────────────────────┘
            │ SaveChangesAsync (sibling propagation)
            ▼
   [DB updated — shuffle/retake/ValidUntil persisted]
```

### Project Structure (file yang disentuh — TIDAK ada file baru produksi)
```
Controllers/
└── AssessmentAdminController.cs   # POST/GET CreateAssessment + EditAssessment (binding fix FORM-02/03/04/05/06/10)
Views/Admin/
├── CreateAssessment.cshtml        # redesign Pre-Post (sub-kartu, SamePackage header, JS toggle) FORM-07/08/09/11 + rename input FORM-10
└── EditAssessment.cshtml          # render shuffle FORM-01
Models/
└── AssessmentSession.cs           # XML-doc AssessmentType usang :170-171 (FORM-10 doc)
HcPortal.Tests/                    # +persistence tests (FORM-01..04 + guard FORM-05)
tests/e2e/                         # +e2e per-mode render (FORM-07..11)
```

### Pattern 1: Penyalinan eksplisit retake (pola kanonik untuk FORM-02/03)
**What:** Saat membangun/memperbarui `AssessmentSession`, SALIN setiap field config dari `model`/`savedAssessment` secara eksplisit. JANGAN andalkan EF default.
**When to use:** Setiap object-init `new AssessmentSession{}` dan setiap `foreach(sibling)` di Create/Edit.
**Example (pola yang SUDAH BENAR — replikasi ini):**
```csharp
// Source: AssessmentAdminController.cs:2169-2192 (bulk-add newSessions — VERIFIED benar)
var newSessions = filteredNewUserIds.Select(uid => new AssessmentSession
{
    // ... field lain ...
    ShuffleQuestions = savedAssessment.ShuffleQuestions,
    ShuffleOptions = savedAssessment.ShuffleOptions,
    // v32.4 RTK-01: pekerja baru mewarisi policy retake dari sibling existing (bukan EF-default diam-diam).
    AllowRetake = savedAssessment.AllowRetake,
    MaxAttempts = savedAssessment.MaxAttempts,
    RetakeCooldownHours = savedAssessment.RetakeCooldownHours,
    GenerateCertificate = savedAssessment.GenerateCertificate,
    // ...
}).ToList();
```

### Pattern 2: `asp-for` mengisi nilai dari Model (pola kanonik untuk FORM-01)
**What:** Tag-helper `asp-for="ShuffleQuestions"` otomatis emit `id="ShuffleQuestions"` + `name="ShuffleQuestions"` + `checked` sesuai `Model.ShuffleQuestions`. Untuk checkbox, ASP.NET juga emit hidden fallback `name="ShuffleQuestions" value="false"` → unchecked tetap bind ke `false` secara benar (BUKAN data-loss).
**When to use:** Render shuffle di EditAssessment (anchor `:414-438` sebaris retake).
**Example (copy idiom dari Create yang sudah benar):**
```html
<!-- Source: CreateAssessment.cshtml:536-551 (copy ke EditAssessment) -->
<div class="col-md-6">
  <label class="form-label fw-bold"><i class="bi bi-shuffle text-primary me-1"></i>Pengacakan Soal &amp; Jawaban</label>
  <div class="form-check form-switch mb-2">
    <input class="form-check-input" type="checkbox" asp-for="ShuffleQuestions" id="ShuffleQuestions" />
    <label class="form-check-label" for="ShuffleQuestions">Acak Soal</label>
  </div>
  <div class="form-text text-muted mb-2">Saat aktif, urutan dan pemilihan soal diacak ...</div>
  <div class="form-check form-switch mb-2">
    <input class="form-check-input" type="checkbox" asp-for="ShuffleOptions" id="ShuffleOptions" />
    <label class="form-check-label" for="ShuffleOptions">Acak Pilihan Jawaban</label>
  </div>
  <div class="form-text text-muted">Saat aktif, urutan pilihan jawaban (A, B, C, D) diacak ...</div>
</div>
```

### Pattern 3: Guard sebelum cabang return (pola untuk FORM-05/E-04)
**What:** Guard `Status=="Completed"` HARUS dievaluasi SEBELUM cabang yang melakukan mutasi+return. Saat ini guard di `:2006` tak pernah tercapai untuk Pre-Post karena cabang Pre-Post `return :2001`.
**When to use:** Angkat guard ke atas `:1821` (sebelum `if (AssessmentType=="PreTest"||"PostTest")`).
**Example:**
```csharp
// Source: AssessmentAdminController.cs — TEMPATKAN sebelum :1821
// Group-aware: untuk Pre-Post, cek anchor/grup; untuk Standard, cek sesi itu sendiri.
bool isCompleted = assessment.Status == "Completed";
if (assessment.LinkedGroupId.HasValue)
{
    isCompleted = await _context.AssessmentSessions
        .AnyAsync(a => a.LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed");
}
if (isCompleted)
{
    TempData["Error"] = "Tidak dapat mengubah assessment yang sudah Completed.";
    return RedirectToAction("ManageAssessment");
}
```

### Pattern 4: Redirect manual-entry (pola untuk FORM-06/E-08)
**What:** GET `EditAssessment` harus mendeteksi `IsManualEntry` dan redirect ke form yang benar.
**Example:**
```csharp
// Source: AssessmentAdminController.cs:1686 (setelah null-check, sebelum Pre-Post detect :1694)
if (assessment.IsManualEntry)
    return RedirectToAction("EditManualAssessment", "TrainingAdmin", new { id });
// Mirror filter pola TrainingAdminController.cs:994: FirstOrDefaultAsync(s => s.Id == id && s.IsManualEntry)
```

### Anti-Patterns to Avoid
- **Render field tanpa menulis (no-op):** retake dirender di Edit `:420-434` tapi POST std `:2072-2089` tak menulisnya (E-03). Setiap field yang dirender editable WAJIB punya jalur tulis.
- **Menulis field tanpa render (silent data-loss):** shuffle ditulis POST `:2084-2085` tapi tak dirender di Edit (E-01) → checkbox absen → bind `false` → reset OFF. INI yang paling berbahaya.
- **`d-none` saja untuk input yang tak boleh ter-POST:** `#standard-jadwal-section` cuma `d-none` `:2003` tapi tetap ter-submit (FORM-PP-03). Pakai `disabled` agar tidak masuk payload.
- **Reuse helper bersemantik terbalik:** jangan pakai `AssessmentEditEligibility.IsEditableAsync` untuk FORM-05 (semantik berlawanan).
- **Rename parsial `AssessmentTypeInput`:** rename harus atomik 8 ref controller + 9 ref view; rename sebagian = binding putus (form 500/silent-null).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Checkbox unchecked → false binding | Hidden input manual + parsing | `asp-for` tag-helper (auto hidden fallback) | ASP.NET sudah emit `<input type=hidden value=false>` di belakang checkbox; bind benar tanpa kode. [CITED: ASP.NET tag-helper docs] |
| Strip input dari POST | Hapus DOM via JS atau parse payload server | `disabled` attribute pada section non-aktif | HTML standar: elemen `disabled` tidak ikut form submit. Idiom paling sederhana + a11y-correct (UI-SPEC Accessibility §). |
| Sibling propagation | Query+loop baru | Pola existing `foreach(sibling)` `:2072-2089` (std) / `:1846-1858` (Pre-Post) | Loop sudah ada; cukup TAMBAH baris field di dalamnya. |
| Clamp range retake | Validasi if manual | `Math.Clamp(MaxAttempts,1,5)` / `Math.Clamp(RetakeCooldownHours,0,168)` | Pola existing `UpdateRetakeSettings` (MEMORY 405-04) sudah pakai Math.Clamp. |
| Test persistensi field | Harness baru | Pola `[Trait("Category","Integration")]` + `IClassFixture<RetakeServiceFixture>` real-SQL | `ShuffleCreatePersistenceTests`/`RetakeSettingsEndpointTests` sudah jadi template persis. [VERIFIED: HcPortal.Tests/*.cs] |

**Key insight:** Semua "perbaikan" fase ini adalah MENAMBAH BARIS di lokasi yang sudah dikenal pasti atau MEMINDAHKAN guard — bukan membangun mekanisme baru. Bahaya terbesar bukan kompleksitas, melainkan (1) lupa satu dari tiga jalur Create, (2) merusak backward-compat Standard saat memperluas JS toggle, (3) rename parsial. Pola kanonik untuk SETIAP fix sudah ada di codebase.

## Runtime State Inventory

> Fase ini menyentuh rename (`AssessmentTypeInput` → `CreationMode`, FORM-10). Inventarisasi state runtime di luar file source:

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| **Stored data** | `AssessmentType` kolom DB (PreTest/PostTest/Standard/Manual) — **TIDAK di-rename** (FORM-10 hanya rename INPUT form `AssessmentTypeInput`, bukan kolom). Tidak ada data tersimpan bernama "AssessmentTypeInput". | None — kolom DB tak tersentuh; rename murni binding form↔controller param. |
| **Live service config** | None — tidak ada workflow/service eksternal yang mereferensikan `AssessmentTypeInput`. | None (verified: grep hanya menemukan Controllers/Views/docs). |
| **OS-registered state** | None — tidak ada task scheduler/pm2/systemd terkait. | None. |
| **Secrets/env vars** | `E2E_BASE_URL` (playwright baseURL override, `:14`) — TIDAK berhubungan dengan rename. Branch ITHandoff test pakai `E2E_BASE_URL=http://localhost:5270`. | None untuk rename; relevan untuk run e2e (lihat Environment Availability). |
| **Build artifacts** | `HcPortal.Tests/bin/` + `obj/` (stale dll) — normal rebuild. | `dotnet build` regenerate; tidak perlu aksi khusus. |

**Rename surface PERSIS (FORM-10 — semua VERIFIED via grep 2026-06-22):**
- **Controller (8 ref, `AssessmentAdminController.cs`):** `:840` `ViewBag.AssessmentTypeInput=""`, `:867` param `string? AssessmentTypeInput`, `:878` `AssessmentTypeInput != "PrePostTest"`, `:925` `bool isPrePostMode = AssessmentTypeInput == "PrePostTest"`, `:1040-1041` validasi (komentar + if), `:1043` `ModelState.AddModelError("AssessmentTypeInput", ...)`.
- **View (9 ref, `CreateAssessment.cshtml`):** `:215` `<select name="AssessmentTypeInput" id="assessmentTypeInput">` (1 HTML), + 8 JS `getElementById('assessmentTypeInput')` di `:1013, :1140, :1403, :1431, :1565, :1934, :1986, :2078`.
- **Catatan binding:** `name="AssessmentTypeInput"` (HTML) → param `AssessmentTypeInput` (controller) = pasangan binding yang HARUS rename bersamaan. `id="assessmentTypeInput"` (camelCase, dipakai JS `getElementById`) bisa rename terpisah dari `name`, tapi paling aman rename keduanya konsisten. `ModelState.AddModelError("AssessmentTypeInput",...)` key juga ikut bila mau konsisten (tidak wajib untuk binding).
- **XML-doc:** `AssessmentSession.cs:170-171` usang ("PreTest/PostTest/null" — tak sebut Standard/Manual). Perbarui jadi 4-nilai.

**Nothing found in OS-registered/live-service categories:** Verified — grep `AssessmentTypeInput` hanya kena `Controllers/`, `Views/`, dan `docs/` (dokumentasi audit, bukan runtime). [VERIFIED: grep -rn]

## Common Pitfalls

### Pitfall 1: Memperbaiki satu jalur Create, lupa dua lainnya
**What goes wrong:** Menambah penyalinan retake hanya di jalur standard `:1467-1491`, lupa Pre `:1243-1263` & Post `:1279-1303` (atau sebaliknya).
**Why it happens:** Tiga jalur build terpisah jauh di file (~200 baris terpencar); audit FLD-5.2-08 menyebut ketiganya.
**How to avoid:** Checklist 3-lokasi eksplisit di plan. Untuk Pre-Post, ikuti D-03: Pre baseline = `AllowRetake=false` (retake tak bermakna untuk baseline); Post = salin dari model. Standard = salin penuh.
**Warning signs:** xUnit persistence test lulus untuk satu mode tapi gagal mode lain.

### Pitfall 2: Merusak backward-compat Standard saat memperluas JS toggle
**What goes wrong:** Sub-kartu D-02 / hide-retake / strip-payload bocor ke mode Standard → DOM/payload Standard berubah.
**Why it happens:** Toggle `:1986-2033` adalah SATU listener `change`; cabang `else` (Standard) harus mengembalikan SEMUA elemen ke state existing.
**How to avoid:** Visibility Matrix (UI-SPEC) = sumber kebenaran. Tiap elemen yang di-toggle harus punya aksi balik di cabang `else`. Playwright assert mode Standard menghasilkan DOM identik (sub-kartu absen, retake tampil, input std ter-POST).
**Warning signs:** Test Standard create existing (`shuffle.spec.ts`, `assessment.spec.ts`) regresi.

### Pitfall 3: Rename `AssessmentTypeInput` parsial → binding putus
**What goes wrong:** Rename `name=` di view tapi lupa param controller (atau sebaliknya) → mode selalu ke-baca Standard (param null) → Pre-Post tak pernah aktif.
**Why it happens:** 17 ref tersebar (8 controller + 9 view).
**How to avoid:** Rename atomik dalam satu commit; build + 1 e2e smoke (pilih Pre-Post → assert sub-kartu muncul) sebagai gate.
**Warning signs:** Form submit Standard padahal user pilih Pre-Post; ModelState error "Tipe assessment tidak valid".

### Pitfall 4: Strip-payload pakai `d-none` bukan `disabled`
**What goes wrong:** Input standard di-`d-none` saat Pre-Post tapi tetap ter-POST (nilai stale ikut). Ini bug ASAL (FORM-PP-03) — jangan ulangi.
**Why it happens:** `d-none` hanya visual; HTML form tetap submit elemen tersembunyi.
**How to avoid:** Set `disabled` (tidak ter-submit) ATAU hapus `name`. UI-SPEC L-5 + Accessibility § eksplisit: section non-aktif harus `disabled` (juga benar untuk screen-reader).
**Warning signs:** POST payload mengandung `ScheduleDate`/`DurationMinutes`/`Schedule` (hidden combiner) saat mode Pre-Post.

### Pitfall 5: Hidden combiner `Schedule`/`ExamWindowCloseDate` ikut dikirim
**What goes wrong:** Selain `#standard-jadwal-section :382-421`, ada hidden `asp-for="Schedule"` `:424` + `asp-for="ExamWindowCloseDate"` `:425` di LUAR section itu. Disable section saja tak cukup.
**Why it happens:** Combiner hidden terpisah dari section visible.
**How to avoid:** L-5 harus disable `#schedHidden` `:424` + `#ewcdHidden` `:425` juga, bukan hanya `#standard-jadwal-section`.
**Warning signs:** POST Pre-Post masih bawa `Schedule`/`ExamWindowCloseDate` standar.

## Code Examples

### Standar Create build path (lokasi FORM-02 standard) — sebelum fix
```csharp
// Source: AssessmentAdminController.cs:1467-1491 (VERIFIED — TIDAK ada AllowRetake/MaxAttempts/RetakeCooldownHours)
var session = new AssessmentSession
{
    Title = model.Title, Category = model.Category, Schedule = model.Schedule,
    DurationMinutes = model.DurationMinutes, Status = model.Status,
    ShuffleQuestions = model.ShuffleQuestions, ShuffleOptions = model.ShuffleOptions,  // shuffle OK
    GenerateCertificate = model.GenerateCertificate, ValidUntil = model.ValidUntil,    // cert OK
    // ↑ FORM-02 FIX: TAMBAH AllowRetake/MaxAttempts/RetakeCooldownHours = model.* di sini
    AssessmentType = "Standard", CreatedBy = currentUser?.Id, ...
};
```

### Standard Edit loop (lokasi FORM-03 + FORM-04) — sebelum fix
```csharp
// Source: AssessmentAdminController.cs:2072-2089 (VERIFIED — ValidUntil & retake ABSENT)
foreach (var sibling in siblings)
{
    sibling.PassPercentage = model.PassPercentage;
    sibling.AllowAnswerReview = model.AllowAnswerReview;
    sibling.ShuffleQuestions = model.ShuffleQuestions;   // shuffle OK
    sibling.ShuffleOptions = model.ShuffleOptions;
    sibling.GenerateCertificate = model.GenerateCertificate;  // cert OK
    sibling.ExamWindowCloseDate = model.ExamWindowCloseDate;  // EWCD OK
    // ↑ FORM-04 FIX: TAMBAH sibling.ValidUntil = model.ValidUntil;
    // ↑ FORM-03 FIX: TAMBAH sibling.AllowRetake/MaxAttempts/RetakeCooldownHours (+ Math.Clamp)
    sibling.UpdatedAt = now;
}
```

### Pre-Post Edit shared loop (sudah benar utk shuffle, referensi)
```csharp
// Source: AssessmentAdminController.cs:1846-1858 (VERIFIED — shuffle ADA di cabang Pre-Post)
foreach (var s in allGroupSessions)
{
    s.PassPercentage = model.PassPercentage;
    s.AllowAnswerReview = model.AllowAnswerReview;
    s.ShuffleQuestions = model.ShuffleQuestions;  // ← Pre-Post sudah tulis shuffle
    s.ShuffleOptions = model.ShuffleOptions;       //    (E-01 hanya soal RENDER di Edit, bukan write)
    s.UpdatedAt = DateTime.UtcNow;
}
// Post loop :1870-1879 menulis GenerateCertificate + ValidUntil :1878 (Pre-Post sudah benar utk ValidUntil)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Field config implisit (EF default) | Penyalinan eksplisit dari model (anti silent-drop) | v32.4 (RTK-01, `:2184-2186`) | Pola kanonik sudah ada — fase 420 perluas ke 3 jalur Create + Edit. |
| `d-none` untuk sembunyikan input | `disabled` untuk exclude dari payload | fase 420 (D-04) | Memperbaiki anti-pattern FORM-PP-03. |
| Group B/C/D tunggal dua-sesi | Sub-kartu scope-explicit Pre-Post | fase 420 (D-02) | Kejelasan scope (FORM-08). |

**Deprecated/outdated:**
- XML-doc `AssessmentSession.cs:170-171`: hanya menyebut PreTest/PostTest/null — usang sejak Manual + Standard ditambahkan. Perbarui (FORM-10).
- Komentar `:5558` (di luar scope 420, milik fase 422) "key identik StartExam/Reshuffle" faktual salah — catat, jangan perbaiki di sini (SHUF-ISS-01 → 422).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | EditAssessment.cshtml Pre-Post group memakai SHARED Group "Pengaturan Ujian" yang sama dengan single-mode (bukan markup terpisah), jadi render shuffle FORM-01 cukup sekali di Group itu. | Pattern 2 / FORM-01 | LOW — terverifikasi Pre-Post hanya beda di tab jadwal (`:213-310`); Group settings shared. Bila salah, shuffle perlu dirender 2x. |
| A2 | FORM-05 lock Completed default group-aware (cek seluruh grup Pre-Post), bukan hanya anchor. | Pattern 3 / FORM-05 | MEDIUM — CONTEXT D-04 discretion bilang "group-aware default" tapi planner tentukan presisi. Bila user mau anchor-only, ubah query. Konfirmasi di discuss/plan. |
| A3 | Mode Standard EditAssessment tidak punya field Pre-Post yang perlu di-strip (Edit tak punya pemilih tipe — tipe sudah fixed dari Create). | L-5 scope | LOW — Edit tidak mengubah AssessmentType; D-04 strip-payload terutama relevan di CreateAssessment. Edit Pre-Post pakai tab jadwal yang sudah memisah Pre/Post. |
| A4 | Bootstrap checkbox `asp-for` di Edit akan mengisi `checked` dari `Model.ShuffleQuestions` (state tersimpan), bukan default true. | FORM-01 | LOW — `asp-for` standar mengikat ke nilai model. Sesi existing punya nilai tersimpan; `View(assessment)` `:1801` mengirim entity. |

## Open Questions

1. **FORM-05 presisi: group-aware vs anchor-only?**
   - What we know: CONTEXT D-04 = "group-aware default"; audit E-04 = "idealnya group-wide".
   - What's unclear: Apakah memblokir SELURUH metadata grup bila SATU sesi Completed, atau hanya field tertentu?
   - Recommendation: Default group-aware penuh (blokir bila ada sesi Completed di grup). Planner konfirmasi; aman karena Edit metadata pasca-Completed = data-integrity risk.

2. **Apakah PassPercentage perlu disembunyikan untuk Pre, atau cukup direlabel?**
   - What we know: D-03 = "PassPercentage untuk Post; Pre = baseline murni". UI-SPEC L-3 = label "Nilai Lulus Post-Test".
   - What's unclear: Pre session tetap menyimpan PassPercentage (untuk konsistensi grup) atau di-null?
   - Recommendation: Simpan PassPercentage di Pre (grup konsisten) tapi label jelas "Post-Test"; Pre tak menampilkan ambang. Tidak ubah perilaku grading (itu fase 424).

3. **Rename `id="assessmentTypeInput"` (JS) — ikut rename atau biarkan?**
   - What we know: FORM-10 fokus `name=`/param (binding). `id` dipakai 8 JS `getElementById`.
   - Recommendation: Untuk kebersihan, rename `id` + `name` + param konsisten ke `CreationMode`. Tapi minimal-viable = rename `name=` + param saja (binding); `id` boleh tetap bila risiko-averse. Planner pilih.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + run + xUnit | ✓ | 8.0.418 | — |
| SQL Server (SQLEXPRESS lokal `HcPortalDB_Dev`) | xUnit `[Category=Integration]` + run | ✓ (per CLAUDE.md dev workflow) | — | Test non-integration jalan via `--filter "Category!=Integration"` |
| Playwright (chromium) | e2e per-mode | ✓ (`tests/playwright.config.ts`) | existing | `npx playwright install chromium` bila browser absent |
| App runtime @localhost:5270 | Playwright UAT (branch ITHandoff) | runtime | — | port 5270 (5277 dipakai worktree main; `E2E_BASE_URL=http://localhost:5270`) |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Playwright browser (auto-install). xUnit Integration butuh SQLEXPRESS hidup; unit non-integration tetap bisa jalan tanpa DB.

**Catatan CLAUDE.md:** Build+run lokal WAJIB sebelum commit (`dotnet build` + `dotnet run` cek `http://localhost:5270` di branch ITHandoff). JANGAN edit kode/DB di server Dev/Prod. migration=FALSE → notify IT tetap diperlukan dengan flag migration=FALSE saat bundle deploy.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests`) + Playwright (`tests/`) — keduanya existing |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` ; `tests/playwright.config.ts` |
| Quick run command | `dotnet test --filter "Category!=Integration"` (skip SQL-gated, cepat) |
| Full suite command | `dotnet test` (incl. Integration @SQLEXPRESS) + `cd tests && npx playwright test <spec> --workers=1` (E2E_BASE_URL=http://localhost:5270) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FORM-01 | Edit save → reopen: ShuffleQuestions/Options tetap (tidak reset OFF) | e2e + integration | `npx playwright test form-persistence-420.spec.ts --workers=1` ; `dotnet test --filter "EditShufflePersist"` | ❌ Wave 0 |
| FORM-02 | Retake config tersimpan di 3 jalur Create (std/Pre/Post) | integration (real-SQL) | `dotnet test --filter "CreateRetakePersist"` | ❌ Wave 0 (template: ShuffleCreatePersistenceTests) |
| FORM-03 | Retake config tersimpan di Edit (bukan no-op) | integration | `dotnet test --filter "EditRetakePersist"` | ❌ Wave 0 |
| FORM-04 | ValidUntil tersimpan di jalur standard Edit | integration | `dotnet test --filter "EditValidUntilPersist"` | ❌ Wave 0 |
| FORM-05 | Edit sesi Completed (group-aware) ditolak | integration / unit guard | `dotnet test --filter "EditCompletedLockGuard"` | ❌ Wave 0 |
| FORM-06 | GET Edit sesi IsManualEntry → redirect EditManualAssessment | integration (action) | `dotnet test --filter "EditManualRedirect"` | ❌ Wave 0 (template: RetakeExamEndpointTests action-invoke) |
| FORM-07 | SamePackage checkbox di header Pre-Post (bukan kartu Post) | e2e render | `npx playwright test form-prepost-ux-420.spec.ts --workers=1` | ❌ Wave 0 |
| FORM-08 | Dua sub-kartu "Setelan Post-Test" + "Setelan Bersama" muncul saat Pre-Post | e2e render | (same spec) | ❌ Wave 0 |
| FORM-09 | Input standard jadwal/EWCD TIDAK ter-POST saat Pre-Post (disabled) | e2e (assert payload/disabled) | (same spec) | ❌ Wave 0 |
| FORM-10 | Rename CreationMode: binding tetap utuh (Pre-Post terpilih → 2 sesi) | e2e smoke + build | `dotnet build` ; `npx playwright test ...` | ❌ Wave 0 |
| FORM-11 | Retake block hidden + Status/PassPct rapi saat Pre-Post | e2e render | (same spec) | ❌ Wave 0 |
| Regresi Standard | Mode Standard DOM+payload tidak berubah | e2e existing | `npx playwright test shuffle.spec.ts assessment.spec.ts --workers=1` | ✅ existing |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "Category!=Integration"` (quick).
- **Per wave merge:** `dotnet test` (full incl. Integration @SQLEXPRESS) + spec e2e wave terkait `--workers=1` @5270.
- **Phase gate:** Full suite green + Playwright per-mode (Standard regresi + Pre-Post baru) sebelum `/gsd-verify-work`. SEED_WORKFLOW (snapshot→seed→restore) wajib untuk e2e yang menyentuh DB.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/FormPersistence420Tests.cs` — FORM-02/03/04 (real-SQL persistence, template `ShuffleCreatePersistenceTests.cs` + `RetakeSettingsEndpointTests.cs`). Replikasi body build/loop controller VERBATIM.
- [ ] `HcPortal.Tests/EditGuardRedirect420Tests.cs` — FORM-05 (Completed lock group-aware) + FORM-06 (manual redirect; action-invoke template `RetakeExamEndpointTests.cs` dgn FakeUserStore).
- [ ] `tests/e2e/form-persistence-420.spec.ts` — FORM-01 lifecycle (create shuffle ON → Edit save → reopen → masih ON) + SEED_WORKFLOW.
- [ ] `tests/e2e/form-prepost-ux-420.spec.ts` — FORM-07/08/09/10/11 (sub-kartu render, SamePackage header, retake hidden, std input disabled, rename binding) + regresi Standard.
- [ ] Framework install: tidak perlu — xUnit + Playwright sudah ada.

*Catatan: pola test endpoint-persistence di repo ini mereplikasi BODY persistensi controller (bukan WebApplicationFactory penuh) — lihat `ShuffleCreatePersistenceTests.cs:46-57` (`CreateFromModel` replika object-init) + `RetakeSettingsEndpointTests.cs:86-104` (replika loop endpoint). Ikuti idiom ini agar konsisten.*

## Security Domain

> `security_enforcement` tidak di-set false di config → enabled. Fase ini binding/view/controller (no new endpoint, no new schema).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Tier ownership benar (Architectural Responsibility Map); guard server-authoritative (FORM-05). |
| V2 Authentication | no | Tak ada perubahan auth; action existing `[Authorize(Roles="Admin, HC")]` `:1681/:1806` dipertahankan. |
| V4 Access Control | yes | FORM-05 lock = access control (blokir mutasi metadata Completed). FORM-06 redirect = routing-correctness. Pertahankan `[Authorize]` + `[ValidateAntiForgeryToken]` `:1807`. |
| V5 Input Validation | yes | Clamp retake `Math.Clamp(1,5)`/`(0,168)`; validasi `AssessmentTypeInput`/`CreationMode` ∈ {Standard,PrePostTest} `:1040-1043` dipertahankan saat rename. PassPercentage 0-100 `:2037`. |
| V6 Cryptography | no | Tak ada kripto. |

### Known Threat Patterns for ASP.NET MVC form
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Mass-assignment via model binding penuh | Tampering | Action pakai param eksplisit + `model` typed; field sensitif (NomorSertifikat) tidak di-bind dari form. Rename CreationMode jaga param binding tetap eksplisit. |
| Mutasi metadata sesi Completed (E-04) | Tampering | FORM-05 guard server-side (group-aware) SEBELUM cabang mutasi. |
| CSRF pada POST Edit/Create | Spoofing | `[ValidateAntiForgeryToken]` existing `:1807` — JANGAN hapus saat refactor view. |
| Bypass redirect manual via direct POST | Tampering | FORM-06 redirect di GET = UX; guard POST `:2006` (manual selalu Completed) tetap mencegah mutasi (defense-in-depth, audit E-08). |
| Stale standard input ter-POST saat Pre-Post (FORM-09) | Tampering | Disable input (bukan d-none) → tak masuk payload; server cabang Pre-Post tak baca field standard. |

**Catatan:** Tidak ada endpoint baru, tidak ada eskalasi privilege. Risiko utama = data-integrity (Tampering), dimitigasi oleh guard server-authoritative (FORM-05) + binding eksplisit. Secure-phase formal (`gsd-secure-phase 420`) tetap gerbang terpisah.

## Sources

### Primary (HIGH confidence — diverifikasi langsung sesi ini)
- `Controllers/AssessmentAdminController.cs` — POST Create build (std `:1467-1491`, Pre `:1243-1263`, Post `:1279-1303`), POST Edit (Pre-Post `:1821-2001`, std loop `:2072-2089`, guard `:2006`), GET Edit (`:1682-1801`, manual filter absent `:1684-1686`), bulk-add retake `:2169-2192`, AssessmentTypeInput 8 ref. [Read + Grep]
- `Views/Admin/CreateAssessment.cshtml` — Type select `:215`, std jadwal `:382-425`, Pre-Post jadwal+SamePackage `:452-483`, Group B (Token/Shuffle/Retake) `:516-576`, JS toggle `:1986-2033`. [Read]
- `Views/Admin/EditAssessment.cshtml` — shuffle ZERO (grep `shuffle\|acak`=0), retake `:414-438`, cert/EWCD `:441-479`, ValidUntil `:481-492`, Pre-Post tab layout `:213-310`. [Read + Grep]
- `Models/AssessmentSession.cs` — AssessmentType XML-doc usang `:166-177`. [Read]
- `Helpers/AssessmentEditEligibility.cs` — IsEditableAsync semantik terbalik `:17-27`. [Read]
- `Controllers/TrainingAdminController.cs` — EditManualAssessment filter `:990-994`. [Read]
- `HcPortal.Tests/ShuffleCreatePersistenceTests.cs` + `RetakeSettingsEndpointTests.cs` — template persistence test real-SQL. [Read]
- `tests/playwright.config.ts` — baseURL/E2E_BASE_URL `:12-14`. [Read]
- `docs/prepost-audit/2026-06-22-evaluasi-pretest-posttest.md` §5.2.4/5.2.5/5.3 — bukti file:line tiap temuan E-01..E-08/FORM-PP-01..07. [Read]

### Secondary (MEDIUM confidence)
- `.planning/phases/420-.../420-CONTEXT.md` + `420-UI-SPEC.md` — keputusan D-01..D-04 + kontrak L-1..L-6. [Read]
- `.planning/REQUIREMENTS.md` + `.planning/ROADMAP.md` — FORM-01..11 + success criteria. [Read]

### Tertiary (LOW confidence)
- None — semua klaim terverifikasi langsung ke source.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new package; versi terverifikasi (dotnet 8.0.418, Bootstrap CDN pinned).
- Architecture/file:line map: HIGH — setiap temuan dibaca langsung dari source 2026-06-22.
- Pitfalls: HIGH — diturunkan dari pola bug nyata yang sudah diverifikasi (3-jalur, backward-compat, rename parsial).
- Validation Architecture: HIGH — template test existing (`ShuffleCreatePersistenceTests`, `RetakeSettingsEndpointTests`, `shuffle.spec.ts`) terbaca.

**Research date:** 2026-06-22
**Valid until:** 2026-07-22 (kode stabil; valid selama branch ITHandoff tidak rebase besar). Re-verify bila merge v32.6 (branch main) menyentuh CreateAssessment.cshtml.
