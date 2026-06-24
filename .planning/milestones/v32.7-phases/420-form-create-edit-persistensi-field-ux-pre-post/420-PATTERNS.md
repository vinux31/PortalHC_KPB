# Phase 420: Form Create/Edit — Persistensi Field + UX Pre-Post - Pattern Map

**Mapped:** 2026-06-22
**Files analyzed:** 6 (3 MODIFY produksi + 2 NEW test + 1 doc-only)
**Analogs found:** 6 / 6 (semua analog di-dalam codebase yang sama — fase ini REDESIGN file existing, bukan greenfield)

> Catatan: ini fase TERIKAT KODE NYATA. Setiap analog di bawah punya `file:line` yang sudah diverifikasi langsung (Read) pada sesi 2026-06-22. Pola kanonik untuk SETIAP perubahan SUDAH ada di codebase — tugasnya MENIRU pola yang sudah terbukti, bukan menciptakan mekanisme baru. Backward-compat mode Standard WAJIB (lihat Shared Pattern: Backward-Compat Guard).

## File Classification

| File (MODIFY/NEW) | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/Admin/CreateAssessment.cshtml` (MODIFY) | view (Razor SSR + inline JS) | request-response (form bind) + event-driven (JS toggle) | (self) `CreateAssessment.cshtml` blok existing (kartu Pre/Post `:429-484`, JS toggle `:1986-2033`, shuffle `:536-551`) | exact (idiom internal) |
| `Views/Admin/EditAssessment.cshtml` (MODIFY) | view (Razor SSR) | request-response (form bind) | `CreateAssessment.cshtml:536-551` (blok shuffle `asp-for`) | exact |
| `Controllers/AssessmentAdminController.cs` (MODIFY) | controller (MVC action) | request-response / CRUD (build + sibling-loop persist) | bulk-add explicit copy `AssessmentAdminController.cs:2169-2192`; manual filter `TrainingAdminController.cs:990-994` | exact (role+flow) |
| `HcPortal.Tests/FormPersistence420Tests.cs` (NEW) | test (xUnit integration real-SQL) | CRUD (persist→reread invariant) | `ShuffleCreatePersistenceTests.cs` + `RetakeSettingsEndpointTests.cs` | exact |
| `HcPortal.Tests/EditGuardRedirect420Tests.cs` (NEW) | test (xUnit integration + action-invoke) | request-response (guard/redirect result) | `RetakeExamEndpointTests.cs` (FakeUserStore action-invoke) + `RetakeSettingsEndpointTests.cs` (replika body) | exact |
| `tests/e2e/form-prepost-ux-420.spec.ts` + `form-persistence-420.spec.ts` (NEW) | test (Playwright e2e) | event-driven (mode toggle render) + request-response (POST persist lifecycle) | `tests/e2e/shuffle.spec.ts` | exact |
| `Models/AssessmentSession.cs` (MODIFY — doc only) | model (XML-doc) | N/A (komentar saja, FORM-10) | (self) `:165-173` | n/a (no behavior) |

**Catatan klasifikasi:** Tidak ada file produksi BARU. Semua "fix" = TAMBAH BARIS di lokasi pasti (FORM-01/02/03/04), PINDAH/ANGKAT guard (FORM-05/06), atau PERLUAS markup+JS toggle (FORM-07..11) + RENAME atomik (FORM-10). Test = 2 file xUnit baru + 1-2 spec Playwright baru, semuanya dari template existing.

---

## Pattern Assignments

### `Controllers/AssessmentAdminController.cs` (controller, CRUD/request-response)

Empat lokasi persistensi + dua guard/redirect. Setiap lokasi sudah dipetakan ke analog di dalam file yang sama.

**Analog penyalinan eksplisit (pola kanonik FORM-02/03 — VERIFIED benar):** `AssessmentAdminController.cs:2169-2192` (bulk-add `newSessions`)

```csharp
// Source: AssessmentAdminController.cs:2181-2188 (bulk-add — SUDAH BENAR, replikasi ini)
ShuffleQuestions = savedAssessment.ShuffleQuestions,
ShuffleOptions = savedAssessment.ShuffleOptions,
// v32.4 RTK-01: pekerja baru mewarisi policy retake dari sibling existing (bukan EF-default diam-diam).
AllowRetake = savedAssessment.AllowRetake,
MaxAttempts = savedAssessment.MaxAttempts,
RetakeCooldownHours = savedAssessment.RetakeCooldownHours,
GenerateCertificate = savedAssessment.GenerateCertificate,
ExamWindowCloseDate = savedAssessment.ExamWindowCloseDate,
```

#### FORM-02 — Retake config tersimpan saat Create (3 jalur)

**Lokasi-1 (standard build):** `AssessmentAdminController.cs:1467-1491` — object-init `new AssessmentSession`. Saat ini menulis `ShuffleQuestions/ShuffleOptions :1479-1480`, `GenerateCertificate/ValidUntil :1481-1483`, TAPI **TIDAK** `AllowRetake/MaxAttempts/RetakeCooldownHours`. FIX = sisipkan 3 baris (mirror `:2184-2186`) setelah `ShuffleOptions`:
```csharp
// SISIP di AssessmentAdminController.cs:~1480 (setelah ShuffleOptions = model.ShuffleOptions,)
AllowRetake = model.AllowRetake,
MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),
RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
```

**Lokasi-2 (Pre build):** `AssessmentAdminController.cs:1243-1263`. Pre = baseline (D-03) → retake TAK bermakna. Set EKSPLISIT:
```csharp
// SISIP di blok preSession (:1243-1263) — D-03: Pre baseline, retake OFF eksplisit
AllowRetake = false,
MaxAttempts = model.MaxAttempts,        // tetap salin untuk konsistensi grup (nilai disimpan, perilaku OFF)
RetakeCooldownHours = model.RetakeCooldownHours,
```

**Lokasi-3 (Post build):** `AssessmentAdminController.cs:1279-1303`. Post = relevan retake → salin penuh dari model + clamp:
```csharp
// SISIP di blok postSession (:1279-1303)
AllowRetake = model.AllowRetake,
MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5),
RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168),
```
> ⚠ Pitfall #1 (RESEARCH): jangan perbaiki SATU jalur dan lupa dua lainnya. Checklist 3-lokasi WAJIB di plan.

#### FORM-03 + FORM-04 — Retake + ValidUntil tersimpan saat Edit standard

**Lokasi:** `AssessmentAdminController.cs:2072-2089` (`foreach (var sibling in siblings)`). Saat ini menulis `ShuffleQuestions/Options :2084-2085`, `GenerateCertificate :2086`, `ExamWindowCloseDate :2087` — TAPI **bukan** `ValidUntil` (FORM-04) dan **bukan** retake×3 (FORM-03, no-op).
```csharp
// SISIP di dalam foreach sibling (:2072-2089), setelah sibling.ExamWindowCloseDate = model.ExamWindowCloseDate; (:2087)
sibling.ValidUntil = model.ValidUntil;                                       // FORM-04
sibling.AllowRetake = model.AllowRetake;                                     // FORM-03
sibling.MaxAttempts = Math.Clamp(model.MaxAttempts, 1, 5);                   // FORM-03 + clamp (V5)
sibling.RetakeCooldownHours = Math.Clamp(model.RetakeCooldownHours, 0, 168); // FORM-03 + clamp
```

#### FORM-01 — Shuffle write Edit (sudah benar; tinggal RENDER di view)

Write SUDAH ada di Edit standard `:2084-2085` dan Pre-Post `:1852-1853` (shared loop). Akar bug E-01 BUKAN di controller — melainkan view Edit tak merender checkbox → bind `false`. Lihat assignment `EditAssessment.cshtml` (L-1). Controller TIDAK diubah untuk FORM-01.

**Referensi Pre-Post shared loop (sudah benar utk shuffle):** `AssessmentAdminController.cs:1846-1858`
```csharp
foreach (var s in allGroupSessions) {
    s.PassPercentage = model.PassPercentage;
    s.AllowAnswerReview = model.AllowAnswerReview;
    s.ShuffleQuestions = model.ShuffleQuestions;  // ← Pre-Post sudah tulis shuffle
    s.ShuffleOptions = model.ShuffleOptions;
    ...
}
```

#### FORM-05 — Guard lock Completed (group-aware, ANGKAT sebelum cabang Pre-Post)

**Bug:** cabang Pre-Post `return :2001` mendahului guard `Status=="Completed"` `:2006-2010` → guard tak pernah tercapai untuk Pre-Post.

**Analog struktur guard:** guard existing `AssessmentAdminController.cs:2006-2010`:
```csharp
// Source: AssessmentAdminController.cs:2006-2010 (existing, single-mode only — VERIFIED tak terjangkau Pre-Post)
if (assessment.Status == "Completed") {
    TempData["Error"] = "Cannot edit completed assessments.";
    return RedirectToAction("ManageAssessment");
}
```
**FIX:** ANGKAT guard ke ATAS `:1821` (sebelum `if ((assessment.AssessmentType == "PreTest" || "PostTest") && LinkedGroupId.HasValue)`), buat group-aware. Pola query group-aware sudah dipakai di branch Pre-Post `:1838-1840`:
```csharp
// TEMPATKAN setelah null-check (:1818), SEBELUM cabang Pre-Post (:1821)
bool isCompleted = assessment.Status == "Completed";
if (assessment.LinkedGroupId.HasValue) {
    isCompleted = await _context.AssessmentSessions
        .AnyAsync(a => a.LinkedGroupId == assessment.LinkedGroupId && a.Status == "Completed");
}
if (isCompleted) {
    TempData["Error"] = "Tidak dapat mengubah assessment yang sudah Completed.";
    return RedirectToAction("ManageAssessment");
}
```
> Assumption A2 (RESEARCH): default group-aware (blokir bila SATU sesi grup Completed). Open Question #1 — konfirmasi di plan. JANGAN reuse `AssessmentEditEligibility.IsEditableAsync` (semantik TERBALIK — `true` HANYA bila Completed; lihat Anti-Patterns).

#### FORM-06 — Redirect GET Edit sesi manual

**Analog filter:** `TrainingAdminController.cs:990-994` (EditManualAssessment GET sudah filter `IsManualEntry`):
```csharp
// Source: TrainingAdminController.cs:992-994 (VERIFIED — pola filter manual)
var session = await _context.AssessmentSessions
    .Include(s => s.User)
    .FirstOrDefaultAsync(s => s.Id == id && s.IsManualEntry);
```
**Lokasi FIX:** GET `EditAssessment :1682-1692`. Setelah null-check `:1692`, SEBELUM Pre-Post detect `:1694`:
```csharp
// SISIP di AssessmentAdminController.cs:~1693 (setelah null-check, sebelum bool isPrePost = ...)
if (assessment.IsManualEntry)
    return RedirectToAction("EditManualAssessment", "TrainingAdmin", new { id });
```

#### FORM-10 — Rename `AssessmentTypeInput` → `CreationMode` (atomik)

**Surface (VERIFIED grep 2026-06-22 — 8 ref controller + 9 ref view):**
- Controller `AssessmentAdminController.cs`: `:840` ViewBag, `:867` param `string? AssessmentTypeInput`, `:878` compare, `:925` `isPrePostMode`, `:1040-1043` validasi + `ModelState.AddModelError("AssessmentTypeInput",...)`. (grep = 7 hit dalam file + param decl.)
- View `CreateAssessment.cshtml`: `:215` `<select name="AssessmentTypeInput" id="assessmentTypeInput">` + 8 JS `getElementById('assessmentTypeInput')` (`:1013,:1140,:1403,:1431,:1565,:1934,:1986,:2078`).
- **Binding pair WAJIB rename bersamaan:** `name="AssessmentTypeInput"` (HTML) ↔ param `AssessmentTypeInput` (controller). `id="assessmentTypeInput"` (JS getElementById) boleh rename terpisah, tapi paling aman konsisten.
- XML-doc `AssessmentSession.cs:170-171` usang (hanya sebut PreTest/PostTest/null) → perbarui jadi 4-nilai (PreTest/PostTest/Standard/Manual).
> ⚠ Pitfall #3: rename PARSIAL = binding putus (mode selalu ke-baca Standard, param null). Rename atomik dalam SATU commit + build + e2e smoke (pilih Pre-Post → assert sub-kartu muncul) sebagai gate. JANGAN rename kolom DB `AssessmentType` (tak tersentuh).

---

### `Views/Admin/EditAssessment.cshtml` (view, request-response)

#### FORM-01 / L-1 — Render toggle Acak Soal & Pilihan (akar bug E-01)

**Bug:** `EditAssessment.cshtml` TIDAK merender shuffle sama sekali (grep `ShuffleQuestions`/`ShuffleOptions` = 0). Checkbox absen → POST bind ke `false` → reset OFF tiap Edit (silent data-loss).

**Analog (copy IDIOM + COPY persis):** `CreateAssessment.cshtml:536-551`:
```html
<!-- Source: CreateAssessment.cshtml:536-551 — COPY ke EditAssessment Group "Pengaturan Ujian", sebaris kartu Retake (:414-438) -->
<div class="col-md-6">
  <label class="form-label fw-bold">
    <i class="bi bi-shuffle text-primary me-1"></i>Pengacakan Soal &amp; Jawaban
  </label>
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
**Penempatan:** dalam Group "Pengaturan Ujian" Edit, berdampingan kartu Retake existing `EditAssessment.cshtml:414-438` (struktur `col-md-6 mb-3` + `form-check form-switch` + `id="retakeFieldsEdit"` sudah ada — tiru style `fw-semibold` Edit, copy ON/OFF semantik dari Create). `asp-for` otomatis emit hidden fallback `value="false"` → unchecked bind benar (BUKAN data-loss). Sesi existing punya nilai tersimpan; `View(assessment) :1801` mengirim entity → `checked` terisi dari `Model.ShuffleQuestions`.

> Assumption A1: Pre-Post group di EditAssessment memakai Group "Pengaturan Ujian" SHARED yang sama dengan single-mode → render shuffle cukup SEKALI. Verifikasi anchor saat plan (Pre-Post hanya beda di tab jadwal `:213-310`).

---

### `Views/Admin/CreateAssessment.cshtml` (view + inline JS, request-response + event-driven)

Empat perubahan UX Pre-Post (L-2..L-5) + rename (L-6/FORM-10). Mode Standard markup/payload TIDAK boleh berubah.

#### Idiom kartu kanonik (PAKAI persis — analog Group B `:488-579`)
```html
<!-- Source: CreateAssessment.cshtml:489-493 (Group card kanonik) -->
<div class="card mb-4">
  <div class="card-header bg-light"><h6 class="mb-0"><i class="bi bi-sliders me-2"></i>Pengaturan Ujian</h6></div>
  <div class="card-body"><div class="row g-3"> ...field col-md-6... </div></div>
</div>
```
#### Idiom SUB-kartu nested (D-02 / FORM-08 — analog kartu Pre/Post `:429-451`)
```html
<!-- Source: CreateAssessment.cshtml:429-433 (nested sub-card — PAKAI utk "Setelan Post-Test" / "Setelan Bersama Pre & Post") -->
<div class="card mb-3">
  <div class="card-header bg-light fw-semibold"><i class="bi bi-clock-history"></i> Setelan Post-Test</div>
  <div class="card-body"> ... </div>
</div>
```

#### FORM-07 / L-4 — SamePackage pindah ke header section Pre-Post

**Analog (markup existing yang DIPINDAH):** `CreateAssessment.cshtml:473-481` (checkbox `SamePackage` + badge sinkron, saat ini di DALAM kartu Post):
```html
<!-- Source: CreateAssessment.cshtml:474-480 — PINDAH ke header section Pre-Post (di bawah Type select :213-223, di atas #ppt-jadwal-section) -->
<div class="form-check">
  <input type="checkbox" name="SamePackage" value="true" id="samePackageCheck" class="form-check-input" />
  <label class="form-check-label" for="samePackageCheck">Gunakan paket soal yang sama untuk Pre dan Post</label>
</div>
<div id="samePackageBadge" class="mt-2 d-none">
  <span class="badge bg-info"><i class="bi bi-info-circle"></i> Paket soal Post-Test akan otomatis disinkronkan dari Pre-Test</span>
</div>
```
Pertahankan listener JS existing `:2027-2032` (toggle badge `d-none`). Hanya tampil saat Pre-Post.

#### FORM-08 / FORM-11 / L-2 / L-3 — Dua sub-kartu + sembunyikan retake saat Pre-Post

Saat `value==='PrePostTest'`, sajikan Group B sebagai 2 sub-kartu (idiom nested di atas):
- **Sub-kartu "Setelan Post-Test"** (`bi-clock-history`): PassPercentage `:508-514` (relabel "Nilai Lulus Post-Test"), Sertifikat (`GenerateCertificate :591` + `ValidUntil`), blok Ujian Ulang `:552-576` **DISEMBUNYIKAN** (D-03).
- **Sub-kartu "Setelan Bersama Pre & Post"** (`bi-arrow-left-right`/`bi-link-45deg`): Shuffle `:536-551`, AllowAnswerReview (pindah dari Group D `:638`), Token `:516-534`.
Mode Standard: layout Group B/C/D **tetap** (satu `row g-3`). Sub-kartu HANYA muncul Pre-Post.

#### FORM-09 / L-5 — Eliminasi input standard dari POST (disable, BUKAN d-none)

**Anti-pattern existing (JANGAN ulangi):** `#standard-jadwal-section` cuma `d-none` `:2003` tapi TETAP ter-POST. Plus hidden combiner `Schedule` `:424` + `ExamWindowCloseDate` `:425` (`#ewcdHidden`) di LUAR section → disable section saja TAK cukup.
```javascript
// FIX di JS toggle (perluas :1996-2018): set disabled saat Pre-Post, lepas saat Standard
// Cabang Pre-Post: matikan #standard-jadwal-section + #schedHidden(:424) + #ewcdHidden(:425)
//   stdSection.querySelectorAll('input,select').forEach(el => el.disabled = true);
//   document.getElementById('ewcdHidden').disabled = true; // + Schedule hidden combiner
// Cabang else (Standard): el.disabled = false (KEMBALIKAN semua) — backward-compat WAJIB
```
> ⚠ Pitfall #4+#5: pakai `disabled` (HTML standar: elemen disabled tak ter-submit, juga a11y-correct), BUKAN `d-none`. Disable juga hidden combiner `Schedule`/`ExamWindowCloseDate` `:424-425`, bukan hanya section visible.

#### JS toggle — analog yang DIPERLUAS (event-driven)

**Analog:** `CreateAssessment.cshtml:1996-2018` (listener `change` tunggal pengendali per-mode):
```javascript
// Source: CreateAssessment.cshtml:2001-2017 — PERLUAS cabang if/else; TIAP elemen di-toggle WAJIB punya aksi balik di else
if (this.value === 'PrePostTest') {
    pptSection.classList.add('show');
    if (stdSection) stdSection.classList.add('d-none');
    if (statusWrapper) statusWrapper.classList.add('d-none');
    if (certNote) certNote.classList.remove('d-none');
    if (statusEl) statusEl.value = 'Upcoming';
    // [BARU] render 2 sub-kartu (L-2), hide retake (L-3), show SamePackage header (L-4), disable std inputs (L-5)
} else {
    pptSection.classList.remove('show');
    if (stdSection) stdSection.classList.remove('d-none');
    if (statusWrapper) statusWrapper.classList.remove('d-none');
    if (certNote) certNote.classList.add('d-none');
    if (statusEl) statusEl.value = '';
    // [BARU] AKSI BALIK SEMUA: layout tunggal, retake tampil, SamePackage hidden, std inputs enabled
}
```
> ⚠ Pitfall #2: cabang `else` (Standard) HARUS mengembalikan SEMUA elemen ke state existing → DOM+payload Standard identik. Visibility Matrix (UI-SPEC) = sumber kebenaran.

---

### `HcPortal.Tests/FormPersistence420Tests.cs` (NEW — xUnit integration real-SQL)

Cover FORM-02/03/04 (+ FORM-01 invariant). **Template kanonik:** `ShuffleCreatePersistenceTests.cs` (persist→reread real-SQL) + `RetakeSettingsEndpointTests.cs` (replika body endpoint atas grup real).

**Pola fixture + reread (analog `ShuffleCreatePersistenceTests.cs:23-73`):**
```csharp
// Source: ShuffleCreatePersistenceTests.cs:23-31 + 59-73
[Trait("Category", "Integration")]
public class FormPersistence420Tests : IClassFixture<RetakeServiceFixture>  // atau ProtonCompletionFixture
{
    private readonly RetakeServiceFixture _fixture;
    public FormPersistence420Tests(RetakeServiceFixture fixture) => _fixture = fixture;
    private ApplicationDbContext NewCtx() => new ApplicationDbContext(_fixture.Options);
    // PersistAndReadAsync: Add → SaveChangesAsync → reread AsNoTracking().FirstAsync(Id==id)
}
```
**Replika object-init create VERBATIM (analog `ShuffleCreatePersistenceTests.cs:46-57`):** salin SHAPE build-loop controller (std `:1467-1491`, Pre `:1243-1263`, Post `:1279-1303` SETELAH fix) — termasuk 3 field retake + ValidUntil. Pendekatan data-level (project ini TAK punya WebApplicationFactory).
**Replika sibling-loop Edit VERBATIM (analog `RetakeSettingsEndpointTests.cs:86-110`):** query siblings by key (`Title/Category/Schedule.Date`), foreach set field, SaveChanges, reread, assert 3 sesi ikut + clamp `[1,5]`/`[0,168]`.
**Assertion shape (analog `ShuffleCreatePersistenceTests.cs:76-103`):** ON→true, OFF→false-not-forced, independensi round-trip. Buat versi untuk retake (Create std persist, Create Pre=false, Create Post=model, Edit persist, ValidUntil persist).
> Quick run: `dotnet test --filter "Category!=Integration"` skip SQL. Full: `dotnet test` @SQLEXPRESS `HcPortalDB_Dev`.

---

### `HcPortal.Tests/EditGuardRedirect420Tests.cs` (NEW — xUnit integration / action-invoke)

Cover FORM-05 (Completed lock group-aware) + FORM-06 (manual redirect). **Template:** `RetakeExamEndpointTests.cs` (action-invoke controller dengan FakeUserStore + StubSession + real-SQL fixture).

**Pola action-invoke (analog `RetakeExamEndpointTests.cs:36-72`):**
```csharp
// Source: RetakeExamEndpointTests.cs:36-42 + FakeUserStore :47-72
[Trait("Category", "Integration")]
public class EditGuardRedirect420Tests : IClassFixture<RetakeServiceFixture>
{
    // FakeUserStore (FindByIdAsync key-by-id) + StubSession + DefaultHttpContext;
    // deps controller lain di-null!-substitute (action tak deref).
    // FORM-05: seed grup dgn 1 sesi Status="Completed" → invoke POST EditAssessment → assert RedirectToActionResult("ManageAssessment") + TempData["Error"].
    // FORM-06: seed sesi IsManualEntry=true → invoke GET EditAssessment → assert RedirectToActionResult("EditManualAssessment","TrainingAdmin").
}
```
**Alternatif (lebih ringan untuk FORM-05 group-aware query):** replika body guard atas grup real seperti `RetakeSettingsEndpointTests.cs:91-110` (seed sesi LinkedGroupId sama, satu Completed → assert `AnyAsync(...Completed)` true). Action-invoke dipakai bila perlu assert `RedirectToActionResult` aktual.

---

### `tests/e2e/form-prepost-ux-420.spec.ts` + `form-persistence-420.spec.ts` (NEW — Playwright)

Cover FORM-07/08/09/10/11 (render per-mode) + FORM-01 lifecycle + regresi Standard. **Template kanonik:** `tests/e2e/shuffle.spec.ts`.

**Pola serial + DB snapshot/restore (analog `shuffle.spec.ts:39-111`):**
```typescript
// Source: shuffle.spec.ts:41 + 93-111
test.describe.configure({ mode: 'serial' });
// beforeAll: db.backup(snapshotPath) ; afterAll: db.restore(snapshotPath)
// import { login } from '../helpers/auth'; login(page,'admin') = admin@pertamina.com (dev lokal)
```
**Pola assert DOM toggle (analog `shuffle.spec.ts:124-150`):** locator card by `hasText`, `toBeVisible`/`toHaveCount(0)`, `toBeChecked`/`toBeDisabled`. Untuk FORM:
- FORM-08: pilih Pre-Post → 2 sub-kartu ("Setelan Post-Test" + "Setelan Bersama Pre & Post") `toBeVisible`; mode Standard → `toHaveCount(0)` (regresi).
- FORM-07: SamePackage checkbox di header `toBeVisible` saat Pre-Post.
- FORM-11: blok Ujian Ulang `toHaveCount(0)`/hidden saat Pre-Post.
- FORM-09: input standard `toBeDisabled` saat Pre-Post (assert tak ter-POST).
- FORM-10: smoke pilih Pre-Post → sub-kartu muncul (binding utuh setelah rename).
- FORM-01 lifecycle: create shuffle ON → Edit save → reopen → masih ON (anti-reset).
> WAJIB `--workers=1` (DB isolation; `playwright.config` fullyParallel:false). Branch ITHandoff: `E2E_BASE_URL=http://localhost:5270`. SEED_WORKFLOW (backup→test→restore) wajib. Regresi Standard: jalankan `shuffle.spec.ts assessment.spec.ts` (DOM+payload identik).

---

### `Models/AssessmentSession.cs` (MODIFY — doc only, FORM-10)

XML-doc `:170-171` usang (hanya PreTest/PostTest/null) → perbarui jadi 4-nilai (PreTest/PostTest/Standard/Manual). TIDAK ada perubahan perilaku/binding. Field shuffle/retake/ValidUntil yang dipakai persistensi: `ShuffleQuestions/ShuffleOptions :38-42` (default true), `AllowRetake/MaxAttempts/RetakeCooldownHours :44-54` (`[Range(1,5)]`/`[Range(0,168)]` sudah ada), `ValidUntil :84`.

---

## Shared Patterns

### Penyalinan eksplisit field (anti silent-drop) — CROSS-CUTTING
**Source:** `AssessmentAdminController.cs:2169-2192` (bulk-add, v32.4 RTK-01)
**Apply to:** SEMUA object-init `new AssessmentSession{}` (3 jalur Create) + SEMUA `foreach(sibling/s)` (Edit std + Pre-Post loop)
**Aturan:** JANGAN andalkan EF default. SALIN setiap field config (`AllowRetake/MaxAttempts/RetakeCooldownHours/ValidUntil/Shuffle*/GenerateCertificate`) eksplisit dari `model`/`savedAssessment`. Checkbox absen ≠ alasan default-false (server-authoritative).

### Clamp range (V5 Input Validation)
**Source:** pola `UpdateRetakeSettings` (MEMORY 405-04) + `RetakeSettingsEndpointTests.cs` clamp
**Apply to:** setiap tulis `MaxAttempts` (`Math.Clamp(_, 1, 5)`) + `RetakeCooldownHours` (`Math.Clamp(_, 0, 168)`) di Create+Edit. `[Range]` model (`AssessmentSession.cs:48/52`) = lapisan kedua; clamp server = lapisan terakhir.

### `asp-for` checkbox bind (anti data-loss)
**Source:** `CreateAssessment.cshtml:542/547` (Shuffle), `:558` (AllowRetake)
**Apply to:** SEMUA toggle yang dirender Edit (terutama shuffle FORM-01). `asp-for` otomatis emit hidden fallback `value="false"` → unchecked bind benar. Render field editable WAJIB ada jalur tulis di controller (hindari no-op E-03), dan field yang ditulis WAJIB dirender (hindari silent reset E-01).

### Backward-Compat Guard (mode Standard tak berubah) — RISIKO UTAMA
**Source:** Visibility Matrix `420-UI-SPEC.md` + JS toggle `CreateAssessment.cshtml:1996-2018`
**Apply to:** SEMUA perubahan view+JS (FORM-07..11). Tiap elemen yang di-toggle di cabang `PrePostTest` WAJIB punya AKSI BALIK di cabang `else`. Gate: Playwright assert mode Standard DOM+payload identik (`shuffle.spec.ts`, `assessment.spec.ts` tidak regresi).

### Test fixture real-SQL + serial DB snapshot
**Source:** `[Trait("Category","Integration")]` + `IClassFixture<RetakeServiceFixture>` (xUnit); `mode:'serial'` + `db.backup/restore` (`shuffle.spec.ts:41,93-111`)
**Apply to:** SEMUA test baru. Project ini replika BODY persistensi controller (BUKAN WebApplicationFactory penuh). Ikuti idiom — JANGAN bangun harness baru.

### Security guard (V4 Access Control — pertahankan)
**Source:** `[Authorize(Roles="Admin, HC")] :1681/:1806` + `[ValidateAntiForgeryToken] :1807`
**Apply to:** SEMUA refactor view/controller — JANGAN hapus `[ValidateAntiForgeryToken]` saat refactor form. FORM-05 guard = server-authoritative SEBELUM cabang mutasi (mitigasi Tampering metadata Completed). FORM-09 disable input = mitigasi stale-input Tampering.

---

## Anti-Patterns to Avoid (dari RESEARCH — WAJIB dihindari)

| Anti-Pattern | Lokasi bukti | Hindari dengan |
|--------------|-------------|----------------|
| Render field tanpa jalur tulis (no-op) | retake Edit `:420-434` tak ditulis POST `:2072-2089` (E-03) | Setiap field editable → jalur tulis (FORM-03) |
| Tulis field tanpa render (silent data-loss) | shuffle ditulis `:2084-2085` tak dirender Edit (E-01) | Render `asp-for` di Edit (FORM-01) |
| `d-none` untuk input tak-boleh-POST | `#standard-jadwal-section` `:2003` tetap ter-POST | `disabled` attribute (FORM-09) |
| Reuse `AssessmentEditEligibility.IsEditableAsync` utk FORM-05 | semantik TERBALIK (`true` HANYA bila Completed) `:17-27` | Tulis guard sendiri (FORM-05) |
| Rename `AssessmentTypeInput` parsial | 8 ref controller + 9 ref view tersebar | Rename atomik 1 commit + smoke gate (FORM-10) |
| Perbaiki 1 jalur Create lupa 2 lainnya | std `:1467`, Pre `:1243`, Post `:1279` terpencar | Checklist 3-lokasi (FORM-02) |

---

## No Analog Found

Tidak ada. Semua 6 file memiliki analog terverifikasi DALAM codebase yang sama (fase ini redesign+bugfix file existing, bukan greenfield). Tidak perlu fallback ke RESEARCH framework-patterns.

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdminController, TrainingAdminController), `Views/Admin/` (CreateAssessment, EditAssessment), `Models/` (AssessmentSession), `Helpers/` (AssessmentEditEligibility), `HcPortal.Tests/` (ShuffleCreatePersistenceTests, RetakeSettingsEndpointTests, RetakeExamEndpointTests), `tests/e2e/` (shuffle.spec.ts).
**Files scanned/Read langsung:** 11 (3 view-blok + 6 controller-blok + 3 test-template + model + TrainingAdmin filter).
**Verification:** Setiap file:line di-Read langsung sesi 2026-06-22 (bukan asumsi). `AssessmentTypeInput` surface dikonfirmasi via grep (7 hit dalam controller + param = 8; 9 ref view).
**Pattern extraction date:** 2026-06-22
**Backward-compat anchor:** mode Standard = TIDAK berubah perilaku/DOM/payload (WAJIB). migration=FALSE.
