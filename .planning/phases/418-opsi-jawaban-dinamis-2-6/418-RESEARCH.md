# Phase 418: Opsi Jawaban Dinamis 2–6 - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core 8 MVC form-contract refactor (param diskret → list opsi dinamis) + Razor render A–F + EF Core FK-restrict guard. Internal refactor, no external libs, migration=FALSE.
**Confidence:** HIGH (semua klaim diverifikasi langsung di kode live, file:line dikoreksi dari scout)

## Summary

Fase 418 mengubah authoring soal dari terkunci A–D menjadi **2–6 opsi dinamis**. Verifikasi kode menegaskan bahwa **lapisan data & grading sudah 100% dinamis** (`PackageOption` tanpa field huruf, `GradingService` by `PackageOption.Id`, deep-clone sync count-agnostik, import path 415 sudah A–F). Yang tersisa murni **lapisan presentasi + kontrak HTTP**: (1) refactor `CreateQuestion`/`EditQuestion` POST dari 16 param diskret (`optionA..D` + `correctA..D` + `optionA..DImage`/`Alt`/`removeOption..Image`) menjadi binding list ≤6; (2) form authoring + form inject jadi baris dinamis dengan tombol Tambah/Hapus; (3) perluas array huruf render `{A..D}`→`{A..F}` di 5 view + perbaiki bug modulo PreviewPackage; (4) validator max-6; (5) **harden edit-shrink** (tutup hazard 999.14 FK-Restrict 500).

Temuan kritis yang mengubah strategi planner: **(a)** Form authoring `#questionForm` submit **full-page POST** (bukan AJAX) — jadi UX error edit-shrink WAJIB pakai pola `TempData["Error"]` + `RedirectToAction` yang sudah ada (render `alert-danger` di `ManagePackageQuestions.cshtml:75-77`), BUKAN respons JSON. **(b)** Ada **guard H3 eksisting di `EditQuestion` POST (`AssessmentAdminController.cs:7972`)** yang menolak edit soal >4 opsi — guard ini adalah placeholder "edit 5–6 menyusul" dan **WAJIB dihapus** oleh fase ini, lalu loop A–D (`:8061-8097`) diganti loop dinamis A–F. **(c)** Form inject di-parse **client-side** (JS membaca `option_A..D` → `injQuestions[]` → `#QuestionsJson`); refactor inject murni JS, **nol perubahan kontrak server**. **(d)** Import path A–F + `ExtractPackageCorrectLetter` (`ABCDEF`) + `MakePackageFingerprint` 8-param **sudah selesai di Phase 415** — fase 418 tidak menyentuh import.

**Primary recommendation:** Bind opsi dengan **`List<OptionInput>` indexed-property model binding** (`Options[0].Text`, `Options[0].IsCorrect`, `Options[0].ImageAlt`, file `Options[0].Image`, `Options[0].RemoveImage`) — bukan `IFormCollection` parse. Pertahankan `id="option_{letter}"` (A–F) untuk backward-compat e2e + `populateEditForm`. Radio MC pakai **satu `name` grup** (`name="correctIndex"` value=index) menggantikan `correctA..D` per-index. Edit-shrink guard = query existence `PackageUserResponse.PackageOptionId ∈ removedOptionIds` SEBELUM `SaveChangesAsync`, tolak via `TempData["Error"]` (full-page). Render: cukup perluas array literal `{A,B,C,D}`→`{A,B,C,D,E,F}` (semua site sudah index-derived dgn fallback numerik), kecuali PreviewPackage yang perlu fix modulo.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Bind opsi dinamis dari form | API/Backend (controller action) | — | Kontrak HTTP `CreateQuestion`/`EditQuestion` POST; otoritas validasi di server |
| Tambah/hapus baris + re-letter | Browser/Client (vanilla JS) | — | Interaksi form murni client; tak ada round-trip per baris |
| Validasi min-2/max-6 | API/Backend (`QuestionOptionValidator` pure) | Browser (UX cepat) | Server-authoritative (T-298 pattern); client hanya nicety |
| Edit-shrink guard (FK-Restrict) | API/Backend (EF query pre-SaveChanges) | — | Data integrity; HANYA bisa diputuskan dgn query DB |
| Render huruf A–F | Frontend Server (Razor `.cshtml`) | — | Letters display-only, dihitung saat render dari posisi |
| Grading | API/Backend (`GradingService` by Id) | — | **Tidak berubah** — sudah agnostik jumlah/huruf opsi |
| Inject authoring opsi | Browser/Client (JS `injQuestions[]`) | — | Client-state only, di-serialize ke `#QuestionsJson`; tak ada endpoint per-soal |

## Standard Stack

Tidak ada dependency baru. Fase ini murni refactor di stack yang sudah ada.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller action + model binding `List<T>` indexed | [VERIFIED: stack proyek; `HcPortal.csproj`] |
| EF Core | (proyek) | Query existence FK guard + `Remove`/`RemoveRange` | [VERIFIED: `ApplicationDbContext.cs`] |
| Razor | net8.0 | Render huruf A–F server-side | [VERIFIED: `Views/CMP/*.cshtml`] |
| Bootstrap 5 + Bootstrap Icons | (lokal) | input-group, form-check, alert, `bi-plus-circle`/`bi-x-lg` | [VERIFIED: 418-UI-SPEC.md + markup eksisting] |
| Vanilla JS | — | Tambah/hapus baris, re-letter, FileReader thumbnail | [VERIFIED: `ManagePackageQuestions.cshtml` `@section Scripts`] |

### Supporting (test)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | (HcPortal.Tests) | Pure unit: validator min2/max6, guard logic | Validasi REQ acceptance [VERIFIED: `HcPortal.Tests.csproj`] |
| Playwright | (tests/) | e2e form add/remove, render A–F, edit-shrink blocked | UI/JS per lesson 354 [VERIFIED: `tests/playwright.config.ts`] |

**Installation:** tidak ada. `dotnet build` + `dotnet test` + `dotnet run @5277` + `npx playwright test`.

**Version verification:** N/A — tidak ada paket baru. [VERIFIED: no new packages]

## Architecture Patterns

### System Architecture Diagram (alur data opsi dinamis)

```
[HC di browser /Admin/ManagePackageQuestions]
   │  isi form: N baris opsi (2–6), klik radio/checkbox benar, (opsi) gambar
   │  JS: addOptionBtn / removeBtn → re-letter A–F → renumber id/name index
   ▼
[FORM full-page POST  multipart/form-data]   ← BUKAN AJAX (kunci UX error)
   │  Options[0..n].Text, Options[i].IsCorrect (radio: name="correctIndex"),
   │  Options[i].Image (file), Options[i].ImageAlt, Options[i].RemoveImage
   ▼
[CreateQuestion / EditQuestion POST  (AssessmentAdminController)]
   │  1. whitelist QuestionType
   │  2. validasi gambar fail-fast (loop semua file)
   │  3. validasi Section IDOR (415)
   │  4. correctCount gate (MC==1 / MA>=2)
   │  5. QuestionOptionValidator.ValidateQuestionOptions  ← +max-6
   │  6. [EDIT ONLY] EDIT-SHRINK GUARD (D-418-02):
   │       removedOptionIds = opsi existing yg akan dihapus
   │       if PackageUserResponses.Any(r => removedOptionIds.Contains(r.PackageOptionId))
   │          → TempData["Error"] = "Opsi ... sudah dijawab..." ; RETURN redirect
   │  7. upsert opsi in-place (preserve Id + ImagePath) loop dinamis 0..n
   │  8. SaveChangesAsync                           ← tak lagi lempar FK-Restrict 500
   │  9. SyncToPostIfSamePackageAsync (clone opsi 5–6 otomatis)
   ▼
[DB: PackageQuestion 1─* PackageOption]  (grading by PackageOption.Id, tak berubah)
   ▼
[Render ujian/hasil/preview]  StartExam/Results/ExamSummary/_PreviewQuestion/PreviewPackage
   huruf = posisi → {A,B,C,D,E,F}[oi] (fallback (oi+1))  ── display-only, post-shuffle
```

Jalur INJECT terpisah:
```
[Form Inject _InjectQuestionForm] → JS baca option_A..F (client) → injQuestions[]
   → #QuestionsJson → InjectAssessment POST (server deserialize, tak ada param diskret)
```

### Recommended Project Structure (file tersentuh — semua sudah ada)
```
Controllers/AssessmentAdminController.cs   # CreateQuestion/EditQuestion POST refactor + guard
Helpers/QuestionOptionValidator.cs         # +max-6
Models/                                    # (opsional) OptionInput binding model baru
Views/Admin/ManagePackageQuestions.cshtml  # baris dinamis + JS addOption/remove/re-letter + populateEditForm + IMAGE_FIELDS
Views/Admin/_InjectQuestionForm.cshtml     # baris dinamis (tanpa gambar)
Views/Admin/InjectAssessment.cshtml        # JS injAddQuestionBtn + injResetAuthoringForm loop dinamis
Views/CMP/StartExam.cshtml                 # array {A..F} (2 site: :137 MA, :170 MC)
Views/CMP/Results.cshtml                   # array {A..F} (:363)
Views/CMP/ExamSummary.cshtml               # array {A..F} (:57)
Views/Admin/_PreviewQuestion.cshtml        # array {A..F} (:50)
Views/Admin/PreviewPackage.cshtml          # FIX modulo bug (:6 array, :62 index)
tests/e2e/helpers/wizardSelectors.ts       # tambah option_E/F + correct group selector
```

### Pattern 1: List<OptionInput> Indexed Model Binding (REKOMENDASI — flag #1 + #2)
**What:** Ganti 16 param diskret dengan satu `List<OptionInput>` yang di-bind via indexed form-name convention ASP.NET Core.
**When to use:** Selalu — ini pola idiomatik MVC untuk koleksi variabel; lebih bersih dari `IFormCollection` parse manual.

```csharp
// Source: [CITED: ASP.NET Core model binding — collections; learn.microsoft.com/aspnet/core/mvc/models/model-binding]
// Model baru (Models/OptionInput.cs)
public class OptionInput
{
    public string? Text { get; set; }
    public bool IsCorrect { get; set; }
    public IFormFile? Image { get; set; }      // authoring only
    public string? ImageAlt { get; set; }
    public bool RemoveImage { get; set; }
}

// Action signature (CreateQuestion / EditQuestion POST) — ganti optionA..D + correctA..D + 12 param gambar
public async Task<IActionResult> CreateQuestion(
    int packageId, string questionText, string questionType, int scoreValue,
    string? elemenTeknis, string? rubrik, int maxCharacters,
    IFormFile? questionImage, string? questionImageAlt,
    List<OptionInput> options,          // ← binding dinamis ≤6
    int? sectionId = null) { ... }
```

Markup (Razor loop dinamis, template di JS untuk baris tambahan):
```html
<!-- name index-based; id letter-based (backward-compat e2e + populateEditForm) -->
<input type="text" class="form-control" name="options[@i].Text" id="option_@letter"
       placeholder="Opsi @letter" aria-label="Teks opsi @letter" />
```

**Flag #1 — radio `name` MC dinamis (KEYSTONE):**
Pola lama `name="correctA/B/C/D"` adalah **4 grup radio terpisah** — itu kebetulan bekerja karena `applyQTypeSwitch` meng-uncheck semua saat non-MA, tapi secara HTML single-select TIDAK dijamin lintas grup. Untuk dinamis, gunakan **satu `name` grup**: `name="correctIndex"` `value="@i"` untuk radio (MC) → browser native single-select. Checkbox MA tetap per-index `name="options[@i].IsCorrect"`. Controller resolusi: untuk MC baca `correctIndex` (int) → set `options[correctIndex].IsCorrect=true`; untuk MA baca tiap `options[i].IsCorrect`. **Alternatif** (lebih seragam, direkomendasikan): satu set checkbox/radio `class="correct-input"` dengan `name="options[@i].IsCorrect"` value="true" + radio share-name via `data-mc-group` yang di-toggle JS — tapi indexed `IsCorrect` lebih lurus dengan binding model. Putuskan di plan keystone; uji single-select MC lintas 6 baris.

**Flag #2 — konvensi `id`:** Pertahankan **`id="option_{letter}"`** (A–F), bukan `option_{index}`. Alasan: (1) `wizardSelectors.ts:125-126` + e2e `option-validation-386.spec.ts` sudah pin `#option_A..D`/`#correct_A..D`; (2) `populateEditForm` mengisi by id letter. Letter dihitung dari posisi (index 0→A) jadi tetap konsisten dengan binding index. Tambah `#option_E`/`#option_F` saat baris ≥5.

### Pattern 2: Upsert opsi in-place loop dinamis (preserve Id + gambar)
**What:** Generalisasi loop `for (int i = 0; i < 4; i++)` di `EditQuestion` (`:8070`) menjadi `0..options.Count` (≤6), pertahankan logika 4-cabang yang sudah ada (update/remove/add/skip).
**When:** EditQuestion POST, setelah guard edit-shrink.

```csharp
// Source: [VERIFIED: AssessmentAdminController.cs:8060-8097 — pola eksisting yang digeneralisasi]
var existing = q.Options.OrderBy(o => o.Id).ToList();   // urutan == GET JSON
int max = Math.Min(options.Count, 6);                   // max-6 enforcement
for (int i = 0; i < Math.Max(max, existing.Count); i++)
{
    var inp  = i < max ? options[i] : null;
    var hasText = inp != null && !string.IsNullOrWhiteSpace(inp.Text);
    var slot = i < existing.Count ? existing[i] : null;
    if (slot != null && hasText)  { slot.OptionText = inp!.Text!.Trim(); slot.IsCorrect = inp.IsCorrect; await ApplyOptionImageIntent(slot, inp.Image, inp.ImageAlt, inp.RemoveImage, packageId, imagePathsToDelete); }
    else if (slot != null && !hasText) { /* DIHAPUS — sudah lolos guard edit-shrink */ if(!string.IsNullOrEmpty(slot.ImagePath)) imagePathsToDelete.Add(slot.ImagePath!); _context.PackageOptions.Remove(slot); q.Options.Remove(slot); }
    else if (slot == null && hasText) { var n = new PackageOption{OptionText=inp!.Text!.Trim(), IsCorrect=inp.IsCorrect}; await ApplyOptionImageIntent(n, inp.Image, inp.ImageAlt, inp.RemoveImage, packageId, imagePathsToDelete); q.Options.Add(n); }
}
```

### Pattern 3: Render huruf A–F (trivial array extension)
**What:** Semua render site sudah `oi < letters.Length ? letters[oi] : (oi+1).ToString()`. Cukup ubah literal.
```csharp
// Source: [VERIFIED: StartExam.cshtml:137,170 / Results.cshtml:363 / ExamSummary.cshtml:57 / _PreviewQuestion.cshtml:50]
string[] letters = { "A", "B", "C", "D", "E", "F" };   // dulu {A,B,C,D}
var letter = oi < letters.Length ? letters[oi] : (oi + 1).ToString();
```

### Anti-Patterns to Avoid
- **`IFormCollection` manual parse:** rapuh, tak ter-validate model, kehilangan binding gambar. Pakai `List<OptionInput>`.
- **Respons JSON untuk error edit-shrink:** form ini **full-page POST**, bukan AJAX. JSON error tak akan ter-render. Pakai `TempData["Error"]` + redirect (pola eksisting `ManagePackageQuestions.cshtml:75-77`).
- **`id="option_{index}"`:** memecah e2e `wizardSelectors.ts` + `populateEditForm`. Pertahankan `option_{letter}`.
- **Modulo wrap huruf (`% letters.Length`):** sumber bug PreviewPackage (opsi ke-6 → "A"). JANGAN ditiru; pakai index-derived + fallback.
- **`RemoveRange`/`Remove` opsi tanpa cek FK:** hazard 999.14 → FK-Restrict 500. WAJIB guard dulu.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Bind koleksi opsi dari form | Parser `Request.Form["optionA"]...` manual | `List<OptionInput>` indexed binding | MVC native, ter-validate, dukung file [VERIFIED: pola `EditQuestionRow.Options` `Models/EditPesertaAnswersViewModel.cs:17`] |
| Validasi presence opsi | Inline if di tiap action | `QuestionOptionValidator.ValidateQuestionOptions` (sudah dipakai 2 site) | Kill-drift Create+Edit; pure unit-testable [VERIFIED: `:7773`, `:8005`] |
| Deteksi opsi dijawab | Loop manual responses | `_context.PackageUserResponses.AnyAsync(r => ids.Contains(r.PackageOptionId.Value))` | Pola `affectedSessions` sudah ada `:7877`/`:8107` |
| Clone opsi 5–6 ke Post | Loop manual A–F | `q.Options.Select(o => new PackageOption{...})` (sudah count-agnostik `:6675`) | Sudah dinamis sejak 415 [VERIFIED] |
| Thumbnail gambar opsi | Upload-preview baru | `IMAGE_FIELDS` + `wireImageField`/`prefillImage`/FileReader (sudah ada `:726-816`) | Generalisasi 0..6, jangan bangun ulang |

**Key insight:** Lapisan data/grading/import/sync sudah dinamis sejak 415 + helper grading lama. Fase 418 = murni presentasi + kontrak HTTP + 1 guard. Hampir semua "logika" sudah ada — pekerjaan = generalisasi loop 4→6 dan menambah 1 query guard.

## Caller Audit — CreateQuestion / EditQuestion (kontrak yang berubah)

> Tujuan: setiap pemanggil kontrak HTTP yang di-refactor harus diuji ulang. **Hasil audit (grep produksi):**

| # | Caller | Lokasi | Jenis | Dampak | Aksi |
|---|--------|--------|-------|--------|------|
| 1 | `<form id="questionForm" asp-action="CreateQuestion">` | `ManagePackageQuestions.cshtml:327` | Full-page POST (default action) | **Langsung** — field names berubah | Refactor markup + JS |
| 2 | `populateEditForm()` set `form.action = .../EditQuestion` | `ManagePackageQuestions.cshtml:670` | Form action swap → full-page POST | **Langsung** — sama form, names berubah | Refactor markup + JS |
| 3 | `loadEditForm()` fetch `EditQuestion?questionId` (GET, XHR) | `ManagePackageQuestions.cshtml:655` | AJAX GET (prefill) | **JSON GET shape DIPERTAHANKAN** (`options[]` variable-length `:7897`) — hanya `populateEditForm` consumer berubah | Verifikasi prefill A–F |
| 4 | `populateEditForm(data)` baca `data.options[]` | `ManagePackageQuestions.cshtml:668-722` | JS consumer JSON | **Langsung** — loop hardcoded `[A,B,C,D]` `:694-696` → dinamis 0..n | Refactor JS |
| 5 | e2e `option-validation-386.spec.ts` | `tests/e2e/` | Playwright fill `#option_A..D` | **Langsung bila id berubah** | Pertahankan `option_{letter}` → minim breakage; tambah E/F |
| 6 | `wizardSelectors.questionFormSelectors` | `wizardSelectors.ts:125-132` | Selector pin | **Langsung** | Tambah `optionE/F`, `correctE/F`, `optE/FImgField` |
| — | Form Inject `_InjectQuestionForm` | `:35` | **TIDAK** memanggil CreateQuestion/EditQuestion | Tak ada — client-state `injQuestions[]` → `#QuestionsJson` | Refactor JS terpisah (lihat di bawah) |

**Tidak ada pemanggil tersembunyi lain.** Grep `CreateQuestion|EditQuestion` di `*.cs`: hanya definisi action + helper `QuestionOptionValidator` (komentar). Tidak ada controller/service lain yang memanggil action ini secara internal. [VERIFIED: grep produksi 2026-06-24]

**Surface tambahan (bukan caller kontrak, tapi paralel & WAJIB digeneralisasi):**
- Form Inject JS reader: `InjectAssessment.cshtml:1144,1173` (loop `['A','B','C','D']` baca `option_L`/`correct_L`) + `:1139 injResetAuthoringForm` + `:1069 applyQTypeSwitch` (versi inject sendiri). Tidak ada gambar (scope 394). Preview inject `:1476` **sudah** `['A'..'F']` dengan fallback.
- `_InjectQuestionForm.cshtml:35` markup baris (mirror authoring, tanpa blok gambar). `#injAuthError` sudah ada `:75`.

## Edit-Shrink Guard (D-418-02 — tutup hazard 999.14)

**Hazard:** `EditQuestion` POST `AssessmentAdminController.cs:8082-8088` melakukan `_context.PackageOptions.Remove(slot)` saat teks opsi dikosongkan (mis. 4→3). FK `PackageUserResponse.PackageOptionId → PackageOption` = `DeleteBehavior.Restrict` (`ApplicationDbContext.cs:561-564`) → `SaveChangesAsync` (`:8102`) lempar `DbUpdateException` → **500 mentah**. [VERIFIED: kedua file:line]

**Catatan penting:** Saat ini hazard ini **tidak ter-trigger** karena ada **guard H3** (`:7972`) yang menolak edit soal `q.Options.Count > 4` total. Fase 418 **menghapus guard H3** (itu placeholder "edit 5–6 menyusul"), sehingga jalur shrink jadi hidup → guard baru WAJIB dipasang bersamaan.

**Pola guard (server-authoritative, EF query existence SEBELUM SaveChanges):**

```csharp
// Source: [VERIFIED: pola affectedSessions sudah ada AssessmentAdminController.cs:7877 & :8107]
// Tempat: di EditQuestion POST, SETELAH menentukan opsi mana yang akan dihapus (text kosong / konversi Essay),
//         SEBELUM _context.PackageOptions.Remove(...) dan SEBELUM SaveChangesAsync.
// PackageUserResponse.PackageOptionId adalah int? (nullable) → guard .HasValue (Models/PackageUserResponse.cs:19).

// 1. Kumpulkan Id opsi existing yang AKAN dihapus (shrink + konversi Essay)
var existing = q.Options.OrderBy(o => o.Id).ToList();
var removedOptionIds = new List<int>();
if (questionType == "Essay")
    removedOptionIds.AddRange(existing.Select(o => o.Id));        // Essay buang semua opsi
else
    for (int i = 0; i < existing.Count; i++)
        if (i >= max || string.IsNullOrWhiteSpace(options[i].Text)) // posisi tak terisi lagi → dihapus
            removedOptionIds.Add(existing[i].Id);

// 2. Guard: ada response yang mereferensikan salah satu opsi yang akan dihapus?
if (removedOptionIds.Count > 0)
{
    var blocked = await _context.PackageUserResponses
        .Where(r => r.PackageOptionId.HasValue && removedOptionIds.Contains(r.PackageOptionId.Value))
        .Select(r => r.PackageOptionId!.Value)
        .Distinct()
        .ToListAsync();
    if (blocked.Count > 0)
    {
        // huruf display dari posisi opsi yang terblok (untuk pesan jelas)
        TempData["Error"] = "Opsi yang sudah dijawab peserta tidak bisa dihapus. Batalkan perubahan atau pertahankan opsi tersebut.";
        return RedirectToAction("ManagePackageQuestions", new { packageId });
    }
}
// 3. lanjut loop upsert (Remove aman karena tak ada FK referensi)
```

**Penempatan & wording (Claude's Discretion per CONTEXT):** Rekomendasi **controller pre-check** (bukan service) — `EditQuestion` belum lewat service layer untuk authoring; konsisten dengan gate validasi lain di action yang sama. Wording mengikuti Copywriting Contract UI-SPEC C5 (boleh sertakan huruf/teks opsi terblok). Pakai `TempData["Error"]` + redirect (full-page), BUKAN JSON (form bukan AJAX).

**a11y (flag #3):** Tambah `role="alert"` pada container error (saat ini `alert-danger` di `:77` tanpa role). UI-SPEC menaikkan ini jadi WAJIB.

## Validator Max-6 (OPT-03)

`QuestionOptionValidator.ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)` (`Helpers/QuestionOptionValidator.cs:20`) sudah:
- min-2 (`filled < 2` → reject, `:26`)
- correct-must-have-text (`:29-31`)
- Essay bypass (`:22`)

**Sudah variable-length-capable** — `texts.Count(...)` + loop `i < texts.Length`. **Tambah max-6:**
```csharp
// Source: [VERIFIED: QuestionOptionValidator.cs — tambah setelah filled<2 check]
if (filled > 6)
    return (false, "Maksimal 6 opsi per soal.");
```
**Caller (2 site, keduanya kirim array fixed-4 saat ini):** `:7773` (Create), `:8005` (Edit). Setelah refactor, kirim `options.Select(o => o.Text).ToArray()` + `options.Select(o => o.IsCorrect).ToArray()` (panjang = jumlah baris terisi ≤6). **Import path (`:7488`) tidak panggil validator ini** — struktural max-6 karena Excel hanya 6 kolom (sudah aman 415). Form authoring adalah satu-satunya jalur yang bisa kirim >6 (defense-in-depth).

## Render A–F (OPT-02) — Site Inventory (LINE DIKOREKSI dari scout)

| View | Line(s) | Kondisi saat ini | Aksi |
|------|---------|------------------|------|
| `StartExam.cshtml` | **:137 (MA)** + **:170 (MC)** | 2 array `{A,B,C,D}` + fallback `(oi+1)` | array → `{A..F}` (2 tempat) |
| `Results.cshtml` | **:363** | `{A,B,C,D}` + fallback | array → `{A..F}` |
| `ExamSummary.cshtml` | **:57** | `{A,B,C,D}` + fallback | array → `{A..F}` |
| `_PreviewQuestion.cshtml` | **:50** (+`:56` fallback) | `{A,B,C,D}` + `i<letters.Length?...:(i+1)` | array → `{A..F}` |
| `PreviewPackage.cshtml` | **:6** array, **:62** index | `{A,B,C,D,E}` (5) + `@letters[optIdx % letters.Length]` | **FIX BUG**: array → `{A..F}` (6) **+** ganti `% letters.Length` → `optIdx < letters.Length ? letters[optIdx] : (optIdx+1).ToString()` (no wrap) |
| `InjectAssessment.cshtml` | :1476 (preview) | **sudah** `['A'..'F']` + fallback | tak perlu ubah render; hanya reader `:1144/:1173` |

**Konfirmasi grep:** TIDAK ada site letter-cap opsi lain. `EditAssessment.cshtml:811` + `CreateAssessment.cshtml:868` (`ABCDEFGHJK...`) = generator kode/passcode acak, BUKAN huruf opsi. [VERIFIED: grep `*.cshtml` 2026-06-24]

## Backward-Compat Invariants (C8 UI-SPEC — WAJIB lolos)

| Invariant | Cara verifikasi |
|-----------|-----------------|
| Soal lama 4-opsi render identik di semua layar | Render array superset A–F; 4 opsi → A–D tampil sama (e2e + unit) |
| Edit soal 4-opsi → prefill 4 baris (bukan 6) | `populateEditForm` enumerasi `data.options.length`; tombol Hapus aktif C/D; Tambah enabled |
| Soal lama bergambar opsi → thumbnail prefill identik | `IMAGE_FIELDS` dinamis tetap menemukan field per index |
| Grading nilai soal lama identik | `GradingService` by `PackageOption.Id` tak berubah (regresi test `IsQuestionCorrectTests`/`GradingDedupeTests`) |
| GET JSON `options[]` shape tak berubah | `:7897` `OrderBy(o => o.Id).Select(optionText,isCorrect,imagePath,imageAlt)` — JANGAN tambah field index |
| Import path A–F tetap jalan | Phase 415 — tak disentuh 418 (regresi `InjectExcelImportTests`/`SectionFixRegressionTests` hijau) |

## Runtime State Inventory

> Fase ini bukan rename/refactor string global, tapi refactor kontrak. Inventory tetap relevan untuk "apa yang punya bentuk lama tersimpan".

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | `PackageOption` rows: soal lama punya 2–4 opsi, soal import 415 bisa 5–6 opsi. **Tidak ada field huruf tersimpan** (huruf display-only, `AssessmentPackage.cs:120`). `PackageUserResponse.PackageOptionId` referensi opsi yg sudah dijawab. | None — schema tak berubah. Guard edit-shrink melindungi response existing. |
| Live service config | None — tak ada konfigurasi eksternal. | None (verified — fitur internal MVC). |
| OS-registered state | None. | None. |
| Secrets/env vars | None. | None. |
| Build artifacts | None baru. `dotnet build` regen biasa. | None. |

**Catatan kompatibilitas data live:** ~600 soal live mayoritas 4-opsi; soal hasil import 415 bisa 5–6 opsi tapi **saat ini tak bisa diedit** (guard H3 `:7972`). Setelah 418, soal 5–6 opsi jadi editable. Verifikasi: edit soal 5-opsi (import) tak korup. [VERIFIED: H3 guard + import path]

## Common Pitfalls

### Pitfall 1: Lupa hapus guard H3 — soal 5–6 opsi tetap tak bisa diedit
**What goes wrong:** Refactor loop A–F selesai tapi guard `if (q.Options.Count > 4) return TempData["Error"]` (`:7972`) masih ada → edit soal 5–6 opsi tetap ditolak; OPT-01 gagal untuk soal import.
**Why:** Guard adalah placeholder "menyusul" yang menunjuk fase ini secara eksplisit (komentar `:7968-7976`).
**How to avoid:** Hapus guard H3 BERSAMAAN dengan memasang guard edit-shrink. Keduanya di EditQuestion POST.
**Warning signs:** e2e edit soal 5-opsi → "lebih dari 4 opsi ... belum dapat diedit".

### Pitfall 2: Edit-shrink guard tidak dipasang → 500 mentah saat hapus opsi yg dijawab
**What goes wrong:** Hapus guard H3 tanpa pasang guard 999.14 → shrink opsi yg sudah dijawab → FK-Restrict `DbUpdateException` → 500.
**Why:** `:8086 Remove(slot)` + `:8102 SaveChangesAsync` tanpa cek `PackageUserResponse`.
**How to avoid:** Query existence SEBELUM SaveChanges (Pattern guard di atas).
**Warning signs:** e2e "hapus opsi B yg sudah dijawab peserta" → 500 alih-alih pesan `alert-danger`.

### Pitfall 3: Radio MC dinamis tidak single-select lintas baris (flag #1)
**What goes wrong:** Pertahankan `name="correctA..F"` per-index untuk radio → 6 grup radio terpisah → user bisa centang >1 "benar" untuk MC (lolos client, ditolak server correctCount=1 → UX buruk) atau binding salah.
**Why:** HTML single-select butuh `name` sama.
**How to avoid:** Satu `name="correctIndex"` value=index untuk MC radio; MA checkbox per-index. Putuskan di plan keystone.
**Warning signs:** e2e MC: centang E lalu A → keduanya tetap tercentang.

### Pitfall 4: Empty trailing rows tak diabaikan (model-binding gotcha)
**What goes wrong:** Form kirim 6 baris tapi hanya 3 terisi → bila tak skip teks kosong, tercipta opsi kosong / validasi keliru.
**Why:** Indexed binding mengisi `List<OptionInput>` dengan entri kosong di posisi tak terisi.
**How to avoid:** Mirror aturan import "kosong diabaikan" (spec §171) — di loop persist, skip `string.IsNullOrWhiteSpace(Text)` (sama persis pola Create `:7814` & Edit `:8072`). Validator hitung `filled` saja.
**Warning signs:** Soal tersimpan dengan opsi blank di tengah; jumlah opsi salah.

### Pitfall 5: populateEditForm consume JSON dengan asumsi 4 (flag #2)
**What goes wrong:** `data.options.forEach((opt,i) => { if (i<4) ... })` (`:700`) buang opsi E/F saat prefill edit.
**Why:** Hardcoded `if (i < 4)` + array `[A,B,C,D]`.
**How to avoid:** Enumerasi `data.options.length`; bangun baris dinamis sebelum prefill (addOptionRow sampai jumlah opsi). JSON GET sudah variable-length — jangan ubah server.
**Warning signs:** Edit soal 5-opsi → form hanya tampil 4 baris.

### Pitfall 6: Reasosiasi gambar saat hapus baris tengah (flag #4)
**What goes wrong:** Hapus baris B (yg punya gambar di C) → re-letter menggeser huruf tapi gambar/file-input ikut salah baris bila id gambar berbasis huruf statis.
**Why:** `IMAGE_FIELDS` + thumbnail terikat prefix `optA..D` statis (`:728-731`); `opt{letter}ImgField` name `option{letter}Image`.
**How to avoid:** Saat hapus baris, hapus elemen DOM baris itu utuh (teks + blok gambar) lalu re-letter sisa baris (update prefix id + name index). Gambar terikat ke node baris, bukan ke huruf. Bangun `IMAGE_FIELDS` dinamis 0..n dari DOM aktual (UI-SPEC C6).
**Warning signs:** e2e: C punya gambar, hapus B → gambar pindah ke baris salah / hilang. (Uji eksplisit di test C8.)

### Pitfall 7: Antiforgery hilang saat manipulasi form via JS
**What goes wrong:** Tambah/hapus baris JS tak menyentuh `@Html.AntiForgeryToken()` (`:328`) — aman. Tapi bila membangun form baru via JS (jangan), token hilang → 400.
**How to avoid:** Manipulasi baris di DALAM form existing; token tetap. Action `[ValidateAntiForgeryToken]` (`:7690`, `:7913`).

## Code Examples

### Tambah baris opsi dinamis + re-letter (JS, authoring)
```javascript
// Source: [pola dari ManagePackageQuestions.cshtml @section Scripts — digeneralisasi]
var LETTERS = ['A','B','C','D','E','F'];
function reletterRows() {
  document.querySelectorAll('[data-option-row]').forEach(function(row, i){
    var L = LETTERS[i];
    row.dataset.index = i;
    row.querySelector('[data-letter]').textContent = L;
    var txt = row.querySelector('input[type=text]');
    txt.name = 'options[' + i + '].Text'; txt.id = 'option_' + L;
    txt.placeholder = 'Opsi ' + L; txt.setAttribute('aria-label','Teks opsi '+L);
    var correct = row.querySelector('.correct-input');
    correct.id = 'correct_' + L; correct.setAttribute('aria-label','Opsi '+L+' benar');
    // MC radio: name grup tunggal; MA checkbox: per-index (lihat flag #1)
    // ... update tombol hapus aria-label + IMAGE_FIELDS prefix bila authoring
  });
  // toggle disabled addBtn @6, sembunyikan hapus saat == 2
}
```

### Query existence guard (xUnit-able logic)
```csharp
// Source: [VERIFIED: AssessmentAdminController.cs:7877 pola affectedSessions]
bool blocked = await _context.PackageUserResponses
    .AnyAsync(r => r.PackageOptionId.HasValue && removedOptionIds.Contains(r.PackageOptionId.Value));
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Kontrak `optionA..D` diskret | `List<OptionInput>` indexed binding | Fase 418 (ini) | Form & inject dinamis 2–6 |
| Guard H3 tolak edit >4 opsi | Edit penuh A–F + guard FK-Restrict | Fase 418 | Soal import 5–6 jadi editable; tutup 999.14 |
| Render `{A,B,C,D}` | `{A,B,C,D,E,F}` (sudah index-derived) | Fase 418 | Opsi 5–6 tampil "E"/"F" |
| PreviewPackage `% letters.Length` cap-E | index-derived no-wrap A–F | Fase 418 | Opsi ke-6 tak lagi tampil "A" |

**Deprecated/outdated (oleh fase ini):**
- Guard H3 `AssessmentAdminController.cs:7972` — dihapus.
- `CMPController.ExtractCorrectLetter`/`MakeFingerprint` (`:1395`/`:1413`, `"ABCD"`) — **dead code** (tak ada caller). Bukan scope 418, tapi catat: jangan keliru kira ini jalur import aktif (jalur aktif = `AssessmentAdminController` sudah ABCDEF di 415). [VERIFIED: grep no-caller]

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (HcPortal.Tests) + Playwright (tests/) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` ; `tests/playwright.config.ts` (no webServer — app manual `dotnet run`) |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~OptionValidation"` |
| Full suite command | `dotnet test` ; `cd tests && npx playwright test --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| OPT-03 | Validator tolak <2 opsi | unit | `dotnet test --filter "OptionValidation"` | ✅ `OptionValidationTests.cs` (min-2 ada) |
| OPT-03 | Validator tolak >6 opsi (BARU) | unit | `dotnet test --filter "OptionValidation_MaxSix"` | ❌ Wave 0 — tambah Fact ke `OptionValidationTests.cs` |
| OPT-03 | Validator terima 5 & 6 opsi valid | unit | idem | ❌ Wave 0 |
| OPT-03 | correct-tanpa-teks ditolak (5–6 opsi) | unit | idem | ❌ Wave 0 (extend ke array-6) |
| D-418-02 | Guard edit-shrink: opsi dijawab → tolak (logic) | unit | `dotnet test --filter "EditShrinkGuard"` | ❌ Wave 0 — pure logic (removedOptionIds ∩ responses) ATAU integration SQL |
| D-418-02 | Guard: opsi belum dijawab → boleh hapus | integration (SQL) | `dotnet test --filter "EditShrinkGuard" --filter Category=SqlServer` | ❌ Wave 0 (pola `SectionFixRegressionTests` real-SQL) |
| OPT-01 | Form authoring: tambah baris → 5,6 lalu disabled @6 | e2e | `npx playwright test option-dynamic-418` | ❌ Wave 0 — spec baru |
| OPT-01 | Form: hapus baris (>2) → re-letter; tak boleh <2 | e2e | idem | ❌ Wave 0 |
| OPT-01 (C8 flag#4) | Hapus baris-tengah B saat C punya gambar → gambar tetap di soal benar | e2e | idem | ❌ Wave 0 (KRITIS — jangan terlewat) |
| OPT-01 | Inject form: tambah/hapus baris A–F | e2e | `npx playwright test inject-assessment-418` ATAU extend 394 | ❌ Wave 0 |
| OPT-02 | Render A–F: ambil ujian soal 6-opsi → huruf E,F tampil | e2e | `npx playwright test option-dynamic-418` | ❌ Wave 0 |
| OPT-02 | PreviewPackage 6-opsi → opsi ke-6 "F" (bukan "A") | e2e/unit-render | idem | ❌ Wave 0 (regresi bug modulo) |
| OPT-02 | Grading soal 6-opsi benar (by Id, post-shuffle) | integration | `dotnet test --filter "Grading"` | ✅ `IsQuestionCorrectTests`/`GradingDedupeTests` (regresi; tambah kasus 6-opsi) |
| Backward-compat | Soal 4-opsi: create/edit/render/grade identik | e2e + unit | `option-validation-386` (regresi) + render | ✅ regresi; tambah assert |
| Backward-compat | Edit soal 5-opsi import → prefill 5 baris (flag#2) | e2e | `option-dynamic-418` | ❌ Wave 0 |
| Edit-shrink UX | Hapus opsi dijawab → `alert-danger`, BUKAN 500 | e2e | idem (real-SQL seed response) | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~OptionValidation"` (pure, <5s)
- **Per wave merge:** `dotnet test` (full xUnit) + build 0-err
- **Phase gate:** Full xUnit hijau + `npx playwright test option-dynamic-418 inject-assessment-* option-validation-386 --workers=1` hijau sebelum `/gsd-verify-work`. Playwright real-browser WAJIB (lesson 354 — Razor/JS).

### Wave 0 Gaps
- [ ] `OptionValidationTests.cs` — tambah Fact: `MaxSix_Rejected`, `FiveOptions_Accepted`, `SixOptions_Accepted`, `SixOpt_CorrectWithoutText_Rejected` (extend array ke 6) — covers OPT-03
- [ ] Edit-shrink guard test — pure logic test (`removedOptionIds` ∩ response option ids) ATAU integration real-SQL (pola `SectionFixRegressionTests`/`SubmitResurrectionTests` yg sudah seed `PackageUserResponse`) — covers D-418-02
- [ ] `tests/e2e/option-dynamic-418.spec.ts` — add/remove rows, disabled@6, min-2, re-letter, render A–F, PreviewPackage 6th="F", edit 5-opsi prefill, image-row reassociation (flag#4), edit-shrink blocked message — covers OPT-01/OPT-02/C8/D-418-02
- [ ] Extend `wizardSelectors.ts` — `optionE/F`, `correctE/F`, `optE/FImgField`/`ImageAlt`, `addOptionBtn`, `removeOptionBtn` selectors
- [ ] Grading regresi: tambah kasus 6-opsi ke `IsQuestionCorrectTests`/`GradingDedupeTests` (opsional — grading by Id sudah agnostik, tapi bukti eksplisit)
- [ ] Framework install: tidak perlu — xUnit + Playwright sudah ada.

## Security Domain

`security_enforcement` absent di config → enabled. Fase ini menyentuh authoring (Admin/HC only) + render exam.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | `[Authorize(Roles="Admin, HC")]` sudah ada di action (`:7689`, `:7913`) — tak berubah |
| V3 Session Management | no | tak disentuh |
| V4 Access Control | yes | Pertahankan `[Authorize(Roles="Admin, HC")]` + Section IDOR guard (`:7735`); guard edit-shrink mencegah merusak data peserta lain |
| V5 Input Validation | yes | `QuestionOptionValidator` (min-2/max-6) + whitelist QuestionType + scoreValue 1–100 + `FileUploadHelper.ValidateImageFile` (loop semua file) — pertahankan untuk 6 opsi |
| V6 Cryptography | no | tak ada |
| V12 File Upload | yes | Gambar opsi 5–6 lewat `FileUploadHelper.SaveFileAsync`/`ValidateImageFile` (sudah ada) — JANGAN hand-roll; generalisasi loop validasi `:7719`/`:7941` ke semua file opsi dinamis |

### Known Threat Patterns for ASP.NET Core MVC authoring
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada POST authoring | Tampering | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` (sudah ada — pertahankan saat refactor markup) |
| FK-Restrict 500 → data integrity / DoS-ish error | Denial of Service / Tampering | Guard edit-shrink (D-418-02) — query existence sebelum SaveChanges |
| Upload gambar opsi 5–6 tidak tervalidasi | Tampering | Perluas loop `ValidateImageFile` ke `options[i].Image` (jangan lupa E/F) |
| IDOR Section/Package | Elevation/Info Disclosure | Section IDOR guard `:7735` + packageId scoping (tak berubah) |
| XSS via OptionText di render | Tampering | Razor auto-encode (`@option.OptionText`) — pertahankan; JANGAN `Html.Raw` |
| Mass-assignment opsi via binding | Tampering | `OptionInput` whitelist properti eksplisit (Text/IsCorrect/Image/ImageAlt/RemoveImage) — tak ada Id dari client |

## Project Constraints (from CLAUDE.md)

- Respons & teks user-facing **Bahasa Indonesia**; identifier/path/code English.
- Workflow Lokal → Dev → Prod: verifikasi lokal (`dotnet build` + `dotnet run` @`localhost:5277` + DB lokal + Playwright bila JS/Razor) SEBELUM commit/push. Migration **FALSE** untuk 418 (tak ada perubahan skema) — notify IT migration=FALSE.
- JANGAN edit kode/DB di Dev/Prod. JANGAN push tanpa verifikasi lokal.
- Seed data testing: snapshot DB → catat `docs/SEED_JOURNAL.md` → restore setelah test (pola `SectionFixRegressionTests` real-SQL + e2e backup/restore `option-validation-386.spec.ts`).
- Branch: **main** (per STATE.md milestone v32.6).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `List<OptionInput>` indexed binding lebih disukai daripada `IFormCollection` parse | Pattern 1 | LOW — keduanya bisa; planner punya diskresi (CONTEXT D-418-01). Indexed binding standar MVC. |
| A2 | Radio MC pakai satu `name` grup `correctIndex` (vs per-index IsCorrect) | flag #1 | MED — keystone; salah pilih → single-select MC rusak. Diuji e2e. Planner putuskan. |
| A3 | Edit-shrink guard di controller pre-check (bukan service) | Guard section | LOW — CONTEXT beri diskresi; controller konsisten dgn gate lain di action sama. |
| A4 | `CMPController.ExtractCorrectLetter`/`MakeFingerprint` adalah dead code | State of the Art | LOW — grep no-caller; bukan scope 418 apa pun. |

## Open Questions

1. **Radio MC `name` strategy (flag #1) — keystone**
   - What we know: pola lama `correctA..D` = 4 grup terpisah; binding model lebih bersih dgn `options[i].IsCorrect`.
   - What's unclear: single `name="correctIndex"` (radio native single-select, controller map index→IsCorrect) vs indexed `options[i].IsCorrect` + JS enforce single-check.
   - Recommendation: `correctIndex` untuk MC radio + `options[i].IsCorrect` checkbox MA. Putuskan di plan keystone; uji e2e single-select lintas 6 baris.

2. **Edit-shrink guard: pesan sebut huruf/teks opsi terblok?**
   - What we know: UI-SPEC C5 copy boleh sertakan `{teks/huruf}`.
   - What's unclear: huruf display dari posisi opsi terblok perlu dihitung saat error.
   - Recommendation: pesan generik cukup untuk MVP; sertakan huruf bila murah (posisi opsi diketahui dari `existing` list). Diskresi planner.

3. **Test guard edit-shrink: pure-logic vs integration real-SQL?**
   - What we know: logic inti (`removedOptionIds ∩ response.PackageOptionId`) bisa diisolasi pure; tapi query EF butuh DB.
   - Recommendation: keduanya — pure unit untuk logic intersect + integration real-SQL (pola `SubmitResurrectionTests` yg seed `PackageUserResponse`) untuk membuktikan tak ada 500 + redirect benar.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | build/test/run | ✓ (asumsi — proyek aktif) | net8.0 | — |
| SQL Server (SQLEXPRESS) | integration test + run lokal | ✓ (DB lokal per CLAUDE.md) | — | — |
| Node + Playwright | e2e | ✓ (`tests/` ada, spec 386/394-398) | — | xUnit-only bila Node down (kurang ideal) |

**Missing dependencies with no fallback:** none. **Missing with fallback:** none — semua tool sudah dipakai fase sebelumnya.

## Sources

### Primary (HIGH confidence — verifikasi langsung kode 2026-06-24)
- `Controllers/AssessmentAdminController.cs:7695-8166` — CreateQuestion/EditQuestion POST + GET JSON + ApplyOptionImageIntent + guard H3 + shrink hazard + import path
- `Helpers/QuestionOptionValidator.cs:20-34` — validator min-2/correct-text
- `Data/ApplicationDbContext.cs:561-564` — PackageUserResponse→PackageOption Restrict FK
- `Models/PackageUserResponse.cs:19` (PackageOptionId int?) · `Models/AssessmentPackage.cs:107-131` (PackageOption no-letter) · `Models/EditPesertaAnswersViewModel.cs:17` (List binding precedent)
- `Views/Admin/ManagePackageQuestions.cshtml` :327 form, :390-431 baris opsi, :547-839 JS (populateEditForm :668, IMAGE_FIELDS :726, applyQTypeSwitch :620), :75-77 alert
- `Views/Admin/_InjectQuestionForm.cshtml` :35-50 · `Views/Admin/InjectAssessment.cshtml` :1069,1144,1173,1199,1476 (inject JS)
- `Views/CMP/StartExam.cshtml` :137/:170 · `Results.cshtml` :363 · `ExamSummary.cshtml` :57 · `Views/Admin/_PreviewQuestion.cshtml` :50 · `PreviewPackage.cshtml` :6/:62 (modulo bug)
- `tests/e2e/helpers/wizardSelectors.ts:109-133` · `tests/e2e/option-validation-386.spec.ts` · `HcPortal.Tests/OptionValidationTests.cs`
- `.planning/config.json` (nyquist_validation:true, no security_enforcement key)

### Secondary (MEDIUM — spec/CONTEXT)
- `418-CONTEXT.md` (D-418-01/02) · `418-UI-SPEC.md` (C1–C8 + 4 flags) · spec §8/§5.3/§15.D/§15.G · `.planning/REQUIREMENTS.md` (OPT-01/02/03)

### Tertiary (LOW — training, di-cite)
- [CITED] ASP.NET Core collection model binding (`options[i].Prop` convention) — learn.microsoft.com/aspnet/core/mvc/models/model-binding. Konsisten dgn precedent `EditQuestionRow.Options` di proyek.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — tak ada lib baru; semua tool sudah dipakai fase 386/415/394-398
- Architecture (kontrak HTTP, guard, render): HIGH — semua file:line diverifikasi & dikoreksi dari scout
- Pitfalls: HIGH — guard H3 + modulo bug + full-page-POST + inject-client-side semua dikonfirmasi langsung
- Radio name strategy (flag#1): MEDIUM — keystone keputusan planner, direkomendasikan tapi perlu konfirmasi via e2e

**Koreksi line dari scout (penting untuk planner):**
- `CreateQuestion` signature mulai :7695 (param `optionA..D` :7703); `EditQuestion` POST :7915 (param :7924); GET JSON :7863 (options[] :7897).
- Shrink hazard :8082-8088 (bukan 8082-8087); SaveChanges :8102.
- **Guard H3 baru ditemukan :7972** (tolak >4 opsi) — TIDAK di scout; WAJIB dihapus fase ini.
- Form opsi authoring :390-431 (scout bilang :395 — itu `@foreach`); IMAGE_FIELDS :726-732; populateEditForm :668-722 (arrays :694-696).
- StartExam DUA array :137 (MA) + :170 (MC) — scout sebut :137/:146 (146 itu baris fallback).
- `ExtractPackageCorrectLetter` :7609 **sudah ABCDEF** (415) — tak perlu diubah; scout spec §8.2 menyesatkan (itu sudah selesai 415).

**Research date:** 2026-06-24
**Valid until:** 2026-07-24 (internal codebase, stabil; re-verify hanya bila 417 merge mengubah StartExam.cshtml/AssessmentAdminController.cs line numbers)
