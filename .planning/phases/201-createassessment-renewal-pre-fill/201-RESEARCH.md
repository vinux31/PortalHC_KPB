# Phase 201: CreateAssessment Renewal Pre-fill - Research

**Researched:** 2026-03-19
**Domain:** ASP.NET Core MVC — Controller query param handling, ViewBag pre-fill, form UX (Bootstrap)
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Pre-fill Behavior:**
- Field yang di-pre-fill: Title, Category (+ SubCategory jika ada), dan peserta (UserId pemilik sertifikat)
- Semua field pre-fill editable — HC bisa ubah Title, Category, atau peserta sebelum submit
- Pre-fill 1 orang per renewal (bulk renewal di Phase 202)
- Jika sertifikat asal dari TrainingRecord: hanya pre-fill Title (TrainingTitle). Category dan peserta harus dipilih manual karena TR tidak punya Category/SubCategory

**Renewal Mode UX:**
- Info banner Bootstrap alert-info di atas form: "Renewal sertifikat: [Title] — [Nama Peserta]"
- GenerateCertificate otomatis dicentang saat mode renewal aktif, tapi user bisa uncheck
- ValidUntil menjadi required (tanda *) di mode renewal. Pre-fill dari ValidUntil sertifikat asal + 1 tahun (jika ada). User bisa edit
- Tombol "Batalkan Renewal" di banner — menghapus query param dan reload sebagai CreateAssessment biasa

**Entry Point & Query Param:**
- Phase 201 hanya menyiapkan CreateAssessment agar bisa menerima query param. Belum ada tombol Renew di UI
- Format: /Admin/CreateAssessment?renewSessionId=123 ATAU ?renewTrainingId=456 (salah satu, XOR)
- Access: Admin dan HC (mengikuti [Authorize(Roles = "Admin, HC")] yang sudah ada)
- Testing via URL manual

**Edge Cases:**
- Sertifikat yang sudah pernah di-renew tetap boleh di-renew lagi (multi-level chain A → B → C)
- Query param invalid (ID tidak ditemukan di DB): redirect ke CreateAssessment biasa + TempData warning
- Peserta (pemilik sertifikat asal) sudah tidak aktif: pre-fill tetap jalan, user terseleksi. HC bisa hapus dan ganti
- Category dari sertifikat asal sudah dinonaktifkan: pre-fill Category tetap. Dropdown sementara menampilkan category tersebut. HC bisa ganti ke category lain

**POST Behavior:**
- AssessmentSession yang dibuat menyimpan RenewsSessionId atau RenewsTrainingId sesuai query param
- XOR validation di application code: hanya satu FK yang boleh terisi

### Claude's Discretion

- Bagaimana menangani SubCategory pre-fill jika parent category berubah
- Exact styling banner renewal (warna, icon, posisi)
- Handling jika ValidUntil sertifikat asal null (tidak punya expiry date)

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| RENEW-03 | CreateAssessment menerima query param renewSessionId/renewTrainingId — pre-fill Title, Category, peserta, GenerateCertificate=true, ValidUntil wajib | GET action perlu parameter opsional; model diisi dari DB query; ViewBag diisi pre-fill data; POST menyimpan FK renewal |
</phase_requirements>

---

## Summary

Phase ini menambahkan "renewal mode" ke form CreateAssessment yang sudah ada. Saat HC/Admin membuka `/Admin/CreateAssessment?renewSessionId=123` atau `?renewTrainingId=456`, controller GET membaca source certificate dari DB, mengisi ViewBag dengan data pre-fill, dan view menampilkan banner info + field yang sudah terisi. POST menyimpan FK renewal ke AssessmentSession.

Implementasi sepenuhnya di dalam codebase yang sudah ada — tidak ada library baru, tidak ada migrasi DB baru (kolom `RenewsSessionId`/`RenewsTrainingId` sudah ada dari Phase 200). Semua perubahan adalah additive: parameter opsional ditambah ke GET signature, kondisi renewal ditambah ke view via ViewBag boolean, hidden field di form POST.

**Primary recommendation:** Gunakan ViewBag boolean `IsRenewalMode` sebagai flag tunggal yang mengontrol semua conditional rendering di view — banner, required marker, auto-check GenerateCertificate, hidden fields RenewsSessionId/RenewsTrainingId.

## Standard Stack

### Core (sudah ada di project)

| Komponen | Versi | Purpose | Catatan |
|----------|-------|---------|---------|
| ASP.NET Core MVC | .NET 8 | Controller + View | Sudah dipakai seluruh project |
| Entity Framework Core | 8.x | DB query AssessmentSession + TrainingRecord | Sudah configured |
| Bootstrap 5 | 5.x | Alert-info banner, conditional required styling | Sudah dipakai di CreateAssessment.cshtml |
| Razor/cshtml | - | Conditional rendering berdasarkan ViewBag | Pattern sudah ada di view |

Tidak ada package baru yang perlu di-install.

## Architecture Patterns

### Pattern 1: Optional Query Param di GET Action

Tambahkan parameter opsional nullable ke signature GET action yang sudah ada.

```csharp
// Source: AdminController.cs lines 947-989 (existing), dimodifikasi
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> CreateAssessment(int? renewSessionId = null, int? renewTrainingId = null)
{
    // ... existing ViewBag setup tidak berubah ...

    bool isRenewalMode = false;

    if (renewSessionId.HasValue)
    {
        var sourceSession = await _context.AssessmentSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == renewSessionId.Value);

        if (sourceSession == null)
        {
            TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
            return RedirectToAction("CreateAssessment");
        }

        isRenewalMode = true;
        model.Title = sourceSession.Title;
        model.Category = sourceSession.Category;
        model.GenerateCertificate = true;
        if (sourceSession.ValidUntil.HasValue)
            model.ValidUntil = sourceSession.ValidUntil.Value.AddYears(1);

        ViewBag.SelectedUserIds = new List<string> { sourceSession.UserId };
        ViewBag.RenewalSourceTitle = sourceSession.Title;
        ViewBag.RenewalSourceUserName = sourceSession.User?.FullName ?? "";
        ViewBag.RenewsSessionId = renewSessionId.Value;
    }
    else if (renewTrainingId.HasValue)
    {
        var sourceTraining = await _context.TrainingRecords
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == renewTrainingId.Value);

        if (sourceTraining == null)
        {
            TempData["Warning"] = "Sertifikat asal tidak ditemukan.";
            return RedirectToAction("CreateAssessment");
        }

        isRenewalMode = true;
        model.Title = sourceTraining.Judul ?? "";
        model.GenerateCertificate = true;
        // Category dan peserta tidak di-pre-fill untuk TrainingRecord (per keputusan)
        if (sourceTraining.ValidUntil.HasValue)
            model.ValidUntil = sourceTraining.ValidUntil.Value.AddYears(1);

        ViewBag.RenewalSourceTitle = sourceTraining.Judul ?? "";
        ViewBag.RenewalSourceUserName = sourceTraining.User?.FullName ?? "";
        ViewBag.RenewsTrainingId = renewTrainingId.Value;
    }

    ViewBag.IsRenewalMode = isRenewalMode;

    return View(model);
}
```

### Pattern 2: Category Pre-fill dengan Inactive Category

AssessmentSession dari Phase 200 menyimpan `Category` sebagai string (nama kategori), bukan FK integer. Dropdown di view render dari `ViewBag.Categories` yang hanya include `IsActive == true`. Jika category asal sudah dinonaktifkan, string pre-fill dari `model.Category` tidak akan match option manapun — dropdown tetap kosong (fallback ke "-- Pilih Kategori --").

**Solusi:** Di GET controller, cek apakah category asal masih active. Jika tidak, tambahkan entry sementara ke `ViewBag.Categories` atau cukup biarkan Razor render `selected` attribute pada option yang tidak ada (browser akan ignore). Pendekatan paling aman: inject category inactive ke list terpisah `ViewBag.InactivePrefilledCategory` dan render sebagai `<option disabled selected>` di view dengan label "(tidak aktif — ganti kategori)".

### Pattern 3: Hidden Fields di Form POST

Untuk menyimpan renewal FK setelah form di-submit, tambahkan hidden input di form:

```html
@if (ViewBag.RenewsSessionId != null)
{
    <input type="hidden" name="RenewsSessionId" value="@ViewBag.RenewsSessionId" />
}
@if (ViewBag.RenewsTrainingId != null)
{
    <input type="hidden" name="RenewsTrainingId" value="@ViewBag.RenewsTrainingId" />
}
```

### Pattern 4: POST — Simpan FK dan ValidUntil Required

Di POST action, setelah ModelState check yang sudah ada:

```csharp
// Setelah ModelState.Remove("ValidUntil") yang existing:
// Override: di mode renewal, ValidUntil wajib diisi
bool isRenewalModePost = model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue;
if (isRenewalModePost && !model.ValidUntil.HasValue)
{
    ModelState.AddModelError("ValidUntil", "Tanggal expired sertifikat wajib diisi untuk renewal.");
}
// XOR validation
if (model.RenewsSessionId.HasValue && model.RenewsTrainingId.HasValue)
{
    ModelState.AddModelError("", "Hanya satu renewal FK yang boleh diisi (XOR constraint).");
}
```

Catatan: `ModelState.Remove("ValidUntil")` di baris 1064 harus dipindah SEBELUM block renewal validation, atau cukup jangan Remove jika renewal mode aktif. Paling clean: hanya Remove jika bukan renewal mode.

### Pattern 5: Banner Renewal di View

Posisi: langsung setelah blok alert TempData yang sudah ada, sebelum wizard nav-pills.

```html
@if (ViewBag.IsRenewalMode == true)
{
    <div class="alert alert-info d-flex align-items-center justify-content-between mb-3" role="alert">
        <div>
            <i class="bi bi-arrow-repeat me-2"></i>
            <strong>Mode Renewal:</strong> @ViewBag.RenewalSourceTitle
            @if (!string.IsNullOrEmpty(ViewBag.RenewalSourceUserName as string))
            {
                <span> — @ViewBag.RenewalSourceUserName</span>
            }
        </div>
        <a href="@Url.Action("CreateAssessment", "Admin")" class="btn btn-sm btn-outline-secondary ms-3">
            Batalkan Renewal
        </a>
    </div>
}
```

### Pattern 6: ValidUntil Required di Renewal Mode

Label ValidUntil sudah ada di Step 3. Ubah conditional:

```html
<label asp-for="ValidUntil" class="form-label fw-bold">
    Tanggal Expired Sertifikat
    @if (ViewBag.IsRenewalMode == true)
    {
        <span class="text-danger">*</span>
    }
    else
    {
        <span class="text-muted">(opsional)</span>
    }
</label>
```

### Pattern 7: SubCategory Pre-fill (Claude's Discretion)

AssessmentSession hanya menyimpan `Category` (string nama), bukan `SubCategoryId`. Di view, SubCategory dropdown di-populate via JavaScript setelah parent Category dipilih. Karena pre-fill Category di-set di `model.Category`, dropdown parent sudah ter-select. JavaScript yang sudah ada di view seharusnya otomatis trigger SubCategory load jika ada event handler `change` pada Category select.

**Rekomendasi:** Jika JavaScript wizard sudah memiliki `initFromModel()` atau sejenisnya yang fire event pada Category select saat page load, SubCategory akan ter-populate otomatis. Jika tidak, tambahkan `document.getElementById('Category').dispatchEvent(new Event('change'))` di inline script setelah page load di renewal mode.

### Anti-Patterns to Avoid

- **Jangan gunakan TempData untuk menyimpan pre-fill data antar redirect** — pre-fill harus di-compute fresh setiap GET, bukan dari cache TempData
- **Jangan hapus ModelState.Remove("ValidUntil") secara kondisional berdasarkan hidden field** setelah model binding — lakukan validasi ValidUntil di application code eksplisit seperti pattern di atas
- **Jangan hardcode kategori inactive** ke dropdown di controller — cukup biarkan `model.Category` terisi; view `selected="@(Model.Category == cat.Name)"` pada option yang ada akan bekerja

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| User pre-selection di wizard | Custom JS selection state | ViewBag.SelectedUserIds yang sudah ada | Form sudah render `checked` dari list ini |
| Category dropdown option selection | Custom JS atau hidden field | `model.Category` + Razor `selected` binding yang sudah ada | Razor asp-for binding handle ini |
| ValidUntil +1 tahun kalkulasi | JS kalkulasi di frontend | C# `DateTime.AddYears(1)` di controller | Lebih reliable, tidak tergantung JS |

**Key insight:** Hampir semua mekanisme yang dibutuhkan sudah ada — ViewBag.SelectedUserIds, model.Category binding, TempData warning pattern. Phase ini adalah konfigurasi/extension, bukan pembangunan dari nol.

## Common Pitfalls

### Pitfall 1: ModelState.Remove("ValidUntil") Menghapus Error Renewal

**What goes wrong:** Baris 1064 POST action melakukan `ModelState.Remove("ValidUntil")` tanpa kondisi. Jika renewal mode dan HC tidak isi ValidUntil, error validation yang ditambahkan akan dihapus oleh Remove ini.

**Why it happens:** Remove berjalan unconditionally sebelum ModelState.IsValid check.

**How to avoid:** Restruktur urutan: pindahkan `ModelState.Remove("ValidUntil")` ke dalam blok `if (!isRenewalMode)`, atau tambahkan ValidUntil error SETELAH Remove tapi SEBELUM `ModelState.IsValid`.

**Warning signs:** Renewal mode bisa submit tanpa ValidUntil meski validasi sudah ditambahkan.

### Pitfall 2: SelectedUserIds Overwrite di POST Validation Error

**What goes wrong:** Saat POST gagal validasi dan view di-return, controller reload `ViewBag.SelectedUserIds = UserIds ?? new List<string>()` (baris 1079). Ini benar untuk normal mode, tapi di renewal mode, juga perlu reload `ViewBag.IsRenewalMode`, `ViewBag.RenewalSourceTitle`, `ViewBag.RenewsSessionId`, dll.

**How to avoid:** Di setiap return View(model) di POST path, tambahkan reload ViewBag renewal data berdasarkan `model.RenewsSessionId` / `model.RenewsTrainingId`.

### Pitfall 3: Inactive User Pre-fill Tidak Muncul di Dropdown

**What goes wrong:** User query di GET hanya mengambil `Where(u => u.IsActive)`. Jika pemilik sertifikat asal sudah tidak aktif, user tersebut tidak ada di `ViewBag.Users`. Meskipun `ViewBag.SelectedUserIds` berisi userId-nya, checkbox tidak akan ter-render di list.

**How to avoid (per keputusan):** Pre-fill tetap jalan, tapi karena user tidak ada di list, selection tidak akan muncul. HC harus pilih peserta manual. Ini acceptable per keputusan di CONTEXT.md ("HC bisa hapus dan ganti"). Tidak perlu workaround khusus — cukup dokumentasikan di UI jika diperlukan, atau biarkan behaviour default.

### Pitfall 4: ValidUntil Pre-fill Melebihi `min` Attribute

**What goes wrong:** `ValidUntil` input punya `min="@DateTime.Today.ToString("yyyy-MM-dd")"`. Jika sertifikat asal punya ValidUntil yang sudah lewat dan +1 tahun masih di masa depan, ini fine. Tapi jika sertifikat asal ValidUntil null, pre-fill tidak terjadi — field kosong, tapi di renewal mode ValidUntil required. HC harus isi manual.

**How to avoid:** Ini adalah intended behavior (per CONTEXT.md Claude's Discretion). Tidak perlu pre-fill jika ValidUntil null — cukup tampilkan field kosong dengan tanda required.

## Code Examples

### GET: Reload ViewBag di POST Validation Error (renewal-aware)

```csharp
// Di setiap return View(model) dalam POST action, tambahkan renewal ViewBag reload:
if (model.RenewsSessionId.HasValue || model.RenewsTrainingId.HasValue)
{
    ViewBag.IsRenewalMode = true;
    ViewBag.RenewsSessionId = model.RenewsSessionId;
    ViewBag.RenewsTrainingId = model.RenewsTrainingId;
    // RenewalSourceTitle/UserName bisa dikosongkan (banner tidak kritis di error state)
    // atau di-query ulang dari DB jika diperlukan
}
```

### View: Hidden Fields untuk POST (letakkan di dalam form, dekat AntiForgery token)

```html
@Html.AntiForgeryToken()
@if (ViewBag.RenewsSessionId != null)
{
    <input type="hidden" name="RenewsSessionId" value="@ViewBag.RenewsSessionId" />
}
@if (ViewBag.RenewsTrainingId != null)
{
    <input type="hidden" name="RenewsTrainingId" value="@ViewBag.RenewsTrainingId" />
}
```

### Controller: AssessmentSession FK Assignment (dalam loop create per user)

```csharp
// Di dalam loop foreach (var userId in UserIds) saat membuat AssessmentSession:
var session = new AssessmentSession
{
    // ... existing fields ...
    RenewsSessionId = model.RenewsSessionId,   // null jika bukan renewal mode
    RenewsTrainingId = model.RenewsTrainingId, // null jika bukan renewal mode
};
```

## State of the Art

| Old Approach | Current Approach | Catatan |
|--------------|------------------|---------|
| Query string tidak ada | renewSessionId / renewTrainingId query params | Baru di Phase 201 |
| ValidUntil selalu opsional | ValidUntil required saat renewal mode | Application-level conditional validation |
| GenerateCertificate default false | GenerateCertificate auto-true di renewal mode | Pre-fill dari controller |

## Open Questions

1. **Inactive user tidak muncul di list peserta**
   - What we know: Query `Where(u => u.IsActive)` tidak return inactive users
   - What's unclear: Apakah HC perlu notifikasi eksplisit bahwa peserta dari sertifikat asal sudah tidak aktif
   - Recommendation: Tidak perlu notifikasi khusus (per keputusan CONTEXT.md — HC bisa hapus dan ganti). Biarkan checkbox kosong tanpa user, HC pilih manual.

2. **SubCategory JavaScript trigger**
   - What we know: Category pre-fill via `model.Category` di-set di controller
   - What's unclear: Apakah JavaScript wizard sudah auto-trigger SubCategory populate saat page load dengan existing model value
   - Recommendation: Investigasi di implementasi. Jika tidak auto-trigger, tambahkan `dispatchEvent(new Event('change'))` pada Category select element di script renewal mode.

## Validation Architecture

> Nyquist validation: cek config.json — jika tidak ada atau true, include section ini.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project convention) |
| Config file | N/A |
| Quick run | Buka URL manual: `/Admin/CreateAssessment?renewSessionId={id}` |
| Full suite | Flow lengkap: GET pre-fill → edit fields → POST → cek DB |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Command |
|--------|----------|-----------|---------|
| RENEW-03a | GET dengan renewSessionId valid → form terisi Title, Category, peserta | Manual browser | URL: `/Admin/CreateAssessment?renewSessionId={valid_id}` |
| RENEW-03b | GET dengan renewTrainingId valid → form terisi Title saja | Manual browser | URL: `/Admin/CreateAssessment?renewTrainingId={valid_id}` |
| RENEW-03c | GenerateCertificate otomatis dicentang | Manual browser | Cek Step 3 checkbox state |
| RENEW-03d | ValidUntil menjadi required di renewal mode | Manual browser | Submit tanpa ValidUntil → expect error |
| RENEW-03e | POST menyimpan RenewsSessionId/RenewsTrainingId | Manual + DB check | Cek AssessmentSession.RenewsSessionId di DB setelah create |
| RENEW-03f | Query param invalid → redirect + warning | Manual browser | URL: `/Admin/CreateAssessment?renewSessionId=99999` |
| RENEW-03g | Tombol Batalkan Renewal → reload tanpa param | Manual browser | Klik button di banner |

### Wave 0 Gaps

None — tidak ada test infrastructure yang perlu dibuat. Testing manual via browser sesuai konvensi project.

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` lines 947-1095 — CreateAssessment GET + POST, dibaca langsung
- `Models/AssessmentSession.cs` — Field RenewsSessionId, RenewsTrainingId, ValidUntil, GenerateCertificate, dikonfirmasi dari source
- `Models/TrainingRecord.cs` — Field Judul, ValidUntil, UserId, RenewsSessionId, RenewsTrainingId, dikonfirmasi dari source
- `Views/Admin/CreateAssessment.cshtml` — Struktur form 4-step wizard, ViewBag patterns, alert patterns, ValidUntil field location

### Secondary (MEDIUM confidence)
- `.planning/phases/201-createassessment-renewal-pre-fill/201-CONTEXT.md` — Semua keputusan desain

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tidak ada library baru, semua dari codebase existing
- Architecture: HIGH — dibaca langsung dari source code
- Pitfalls: HIGH — diidentifikasi dari analisis baris kode spesifik (ModelState.Remove line 1064, SelectedUserIds reload pattern)

**Research date:** 2026-03-19
**Valid until:** 2026-06-19 (stable codebase — tidak ada dependency eksternal yang berubah)
