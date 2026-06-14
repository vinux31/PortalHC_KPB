# Phase 353: Admin Backend Gambar (CRUD + Sync + Atomic Delete) - Research

**Researched:** 2026-06-08
**Domain:** ASP.NET Core 8 MVC multipart upload + EF Core shared-file atomic delete (brownfield Portal HC)
**Confidence:** HIGH (semua temuan dari pembacaan kode aktual di repo + pola Phase 333/352 yang sudah live)

## Summary

Phase 353 adalah pekerjaan wiring murni di **satu controller** (`AssessmentAdminController.cs`) + **satu view** (`ManagePackageQuestions.cshtml`) + **satu partial** (`_PreviewQuestion.cshtml`). Semua fondasi sudah ada: kolom DB `ImagePath`/`ImageAlt` di `PackageQuestion` + `PackageOption` (migration applied), `FileUploadHelper.ValidateImageFile` + `SaveFileAsync` (Phase 352), pola atomic delete Phase 333 (`DeleteCoachingSession`), dan akses `_env.WebRootPath` lewat `AdminBaseController`. Tidak ada library baru, tidak ada migration baru, tidak ada perubahan DI. [VERIFIED: pembacaan kode repo]

Tantangan teknisnya bukan "apa" (sudah dikunci di CONTEXT.md) tapi "bagaimana mengikat" 5 area: (1) menambah parameter `IFormFile?` + alt + checkbox hapus ke signature `CreateQuestion`/`EditQuestion` POST yang saat ini text-only, (2) menjalankan **reference-count** sebelum `File.Delete` karena Pre↔Post share 1 file fisik (D-10), (3) menyalin `ImagePath`+`ImageAlt` di deep-clone `SyncPackagesToPost` (string copy, **bukan** file op — SYN-01/C-03), (4) memperluas JSON `EditQuestion` GET dengan path+alt untuk prefill thumbnail (D-06), (5) `_PreviewQuestion` tambah `<img>`, dan (6) `DeletePackage` kumpul path sebelum tx + ref-count + delete post-commit (D-11).

**Primary recommendation:** Replikasi persis pola Phase 333 (`List<string>? pathsToDelete` outer-scope → build before/inside tx → `File.Delete` POST `CommitAsync` dengan inner try/catch warn-only) DAN tambahkan ref-count `COUNT` query SETELAH `CommitAsync` SEBELUM tiap `File.Delete`. Untuk binding multipart, pakai parameter bernama konvensi `[name]Image` / `[name]ImageAlt` / `remove[Name]Image` yang match atribut `name=` di Razor — ASP.NET model binding mengikat `IFormFile?` per nama field otomatis tanpa DTO. `enctype="multipart/form-data"` WAJIB (D-02), tanpa itu semua `IFormFile?` = null diam-diam.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Validasi gambar (format/size/magic-byte) | API/Backend (`FileUploadHelper.ValidateImageFile`) | — | Keamanan tak boleh client-only; helper Phase 352 sudah ada |
| Simpan file fisik ke disk | API/Backend (`SaveFileAsync` → `wwwroot/uploads/questions/{pkgId}`) | — | Server owns filesystem; folder auto-create |
| Thumbnail instan saat pilih file (D-07a) | Browser/Client (vanilla JS FileReader) | — | UX pre-submit, tanpa round-trip |
| Render gambar tersimpan (D-07b, RND-04) | Frontend Server (Razor `_PreviewQuestion`) | — | Server-rendered partial, konsisten app |
| Reference-count sebelum File.Delete (D-10) | API/Backend (EF `COUNT` query) | DB | Shared-file safety = invariant data, harus server |
| Sync Pre→Post ImagePath/ImageAlt (SYN-01) | API/Backend (`SyncPackagesToPost` deep-clone) | DB | String copy dalam tx, no file op |
| Atomic file delete (SYN-02) | API/Backend (pola Phase 333) | DB | Konsistensi tx↔disk |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller + model binding multipart | Stack existing, tidak boleh ganti [VERIFIED: HcPortal.Tests.csproj net8.0] |
| EF Core | 8.0.0 | Query ref-count + RemoveRange + tx | Existing ORM [VERIFIED: csproj] |
| Bootstrap | 5.3.0 | UI form + thumbnail state | Existing CDN di `_Layout` [CITED: 353-UI-SPEC.md] |
| Bootstrap Icons | 1.10.0 | `bi-image`, `bi-x-lg`, `bi-check-circle-fill` | Existing [CITED: UI-SPEC] |
| Vanilla JS (FileReader) | browser-native | Thumbnail instan client-side (D-07a) | Tidak ada framework JS di app [VERIFIED: view pakai plain `<script>`] |

### Supporting (semua SUDAH ADA — reuse, jangan tulis ulang)
| Asset | Lokasi | Purpose | Catatan |
|-------|--------|---------|---------|
| `FileUploadHelper.ValidateImageFile(IFormFile?)` | `Helpers/FileUploadHelper.cs:45` | Validasi JPG/PNG ≤5MB + magic-byte | Return `(bool IsValid, string? Error)`; null/empty file → valid (true,null) [VERIFIED] |
| `FileUploadHelper.SaveFileAsync(file, webRootPath, subFolder, logger?)` | `:75` | Simpan + return relative URL `/{subFolder}/{safeName}` | Auto-create dir, strip path traversal, timestamp+GUID prefix anti-collision [VERIFIED] |
| `FileUploadHelper.DeleteFile(webRootPath, relativeUrl)` | `:114` | Hapus 1 file dari relative URL (null-safe) | ADA tapi **tidak dipakai** untuk Phase 353 atomic flow — lihat Pitfall 5 |
| `_env.WebRootPath` | `AdminBaseController.cs:18` (`protected readonly`) | Root fisik untuk File.Delete | Diturunkan ke `AssessmentAdminController` via base ctor [VERIFIED] |
| Kolom `ImagePath` (nvarchar max null) + `ImageAlt` (nvarchar 255 null) | `Models/AssessmentPackage.cs:60,64,89,93` | Di `PackageQuestion` + `PackageOption` | Migration `20260606030844` applied [VERIFIED: model + C-05] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Parameter `IFormFile?` per-field di signature | DTO/ViewModel `[FromForm]` model class | DTO lebih rapi TAPI menyimpang dari pola existing CreateQuestion (flat params). Discretion planner (D-67 CONTEXT). Rekomendasi: flat params, konsisten file ini |
| `FileUploadHelper.DeleteFile` | Inline `File.Delete` pola 333 | DeleteFile TIDAK ref-count → akan hapus shared file yang masih dipakai Post. **JANGAN pakai DeleteFile** untuk delete path; pakai pola 333 + ref-count manual (lihat Pitfall 5) |

**Installation:** Tidak ada. Semua package sudah terpasang. Tidak ada migration baru (kolom sudah ada). [VERIFIED: C-05 + model]

## Architecture Patterns

### System Architecture Diagram

```
ADMIN BROWSER                          ASP.NET CONTROLLER (AssessmentAdminController.cs)
───────────                            ──────────────────────────────────────────────
#questionForm                          CreateQuestion / EditQuestion POST
 enctype=multipart  ──upload──────────▶  bind: questionImage?, optionAImage?..D
 [file pick]                              + questionImageAlt, optionAImageAlt..D
   │                                      + removeQuestionImage(bool), removeOptionAImage..D
   ▼ JS FileReader                            │
 [thumbnail 46px instan]                      ▼ per item:
                                          ValidateImageFile(file) ──invalid──▶ TempData["Error"] + redirect (repopulate)
                                              │ valid
                                              ▼ resolve niat (D-05): file baru menang
                                          ┌── new file? ─yes─▶ SaveFileAsync → newPath; oldPath → delete-candidate
                                          ├── remove checked & no file? ─▶ ImagePath=null; oldPath → delete-candidate
                                          └── else ─▶ keep ImagePath
                                              │
                                              ▼ SaveChangesAsync  (+ tx untuk DeleteQuestion/DeletePackage)
                                              ▼ CommitAsync (jika tx)
                                              ▼ POST-COMMIT untuk tiap delete-candidate path:
                                                  COUNT PackageQuestion + PackageOption WHERE ImagePath==path  (ref-count D-10)
                                                  count==0 ─▶ File.Delete(physical)  [inner try/catch warn-only, pola 333]
                                                  count>0  ─▶ SKIP (masih dipakai Post/lain)
                                              │
                                              ▼ auto-sync (jika Pre+SamePackage):
                                          SyncPackagesToPost(preId, postId)
                                              deep-clone newQ/newOpt + ImagePath + ImageAlt (string copy, NO file op) ── SYN-01

PREVIEW                                 PreviewQuestion GET → _PreviewQuestion.cshtml
[loadPreview AJAX] ◀──partial html──── render <img src=ImagePath alt=ImageAlt> (RND-04)

EDIT PREFILL                            EditQuestion GET (X-Requested-With: XMLHttpRequest)
[loadEditForm AJAX] ◀──json──────────── Json{...imagePath, imageAlt, options[{imagePath,imageAlt}]} (D-06)
   ▼ JS render <img src> thumbnail lama + isi alt; <input file> tetap kosong (D-03)
```

### Recommended Touch Points (file → tanggung jawab)
```
Controllers/AssessmentAdminController.cs
├── CreateQuestion POST  (L6067)  # +IFormFile? params, validate, SaveFileAsync, set ImagePath/Alt
├── EditQuestion GET JSON (L6214) # +imagePath/imageAlt question + per option (D-06)
├── EditQuestion POST    (L6241)  # +params, D-05 resolve, replace/remove + ref-count delete post-save
├── DeleteQuestion POST  (L6377)  # +collect paths, tx, ref-count + File.Delete post-commit (C-04)
├── DeletePackage POST   (L5457)  # +collect ALL paths before tx, ref-count + delete post-commit (D-11)
└── SyncPackagesToPost   (L5337)  # +ImagePath/ImageAlt di newQ (L5370) + newOpt (L5379) — string copy (SYN-01)
Views/Admin/ManagePackageQuestions.cshtml
├── #questionForm (L122)          # +enctype="multipart/form-data" (D-02)
├── question image field          # bawah textarea questionText (L142) — .img-drop block (D-01)
├── option A-D image field        # inline bawah tiap input-group (L150-163) (D-01)
└── @section Scripts (L278)       # +FileReader thumbnail handler + populateEditForm prefill img/alt
Views/Admin/_PreviewQuestion.cshtml
├── question <img> (after L17)    # img-fluid max-height 240px (RND-04)
└── option <img> (in loop L54)    # img-fluid max-height 120px (RND-04)
```

### Pattern 1: Multipart per-field binding (fixed A-D layout)
**What:** ASP.NET MVC mengikat `IFormFile?` per nama field otomatis bila `enctype="multipart/form-data"`. Tidak butuh DTO untuk layout fixed A-D.
**When to use:** CreateQuestion/EditQuestion POST.
**Example:**
```csharp
// Source: pola existing CreateQuestion signature (AssessmentAdminController.cs:6067) diperluas
public async Task<IActionResult> CreateQuestion(
    int packageId, string questionText, string questionType, int scoreValue,
    string? elemenTeknis, string? rubrik, int maxCharacters,
    string? optionA, string? optionB, string? optionC, string? optionD,
    bool correctA, bool correctB, bool correctC, bool correctD,
    // ── Phase 353 tambahan ──
    IFormFile? questionImage, string? questionImageAlt,
    IFormFile? optionAImage, IFormFile? optionBImage, IFormFile? optionCImage, IFormFile? optionDImage,
    string? optionAImageAlt, string? optionBImageAlt, string? optionCImageAlt, string? optionDImageAlt)
// Razor: <input type="file" name="questionImage"> + <input type="text" name="questionImageAlt">
//        <input type="file" name="optionAImage"> dst.
// EDIT POST tambahan: bool removeQuestionImage, bool removeOptionAImage..D (checkbox value="true")
```
Catatan: nama field = nama parameter (case-insensitive). Checkbox hapus pakai `bool` (unchecked → false otomatis). Alt text ke `ImageAlt` (cap 255 via MaxLength model — trim/truncate manual disarankan). [VERIFIED: signature existing + ASP.NET binding convention]

### Pattern 2: Reference-count sebelum File.Delete (D-10 — INTI phase)
**What:** Karena Pre & Post simpan `ImagePath` STRING yang sama (1 file fisik, banyak baris DB — C-03), hapus fisik HANYA bila tak ada baris lain pakai path itu.
**When to use:** DeleteQuestion, replace gambar (EditQuestion), DeletePackage — SETELAH `CommitAsync`/`SaveChangesAsync`, SEBELUM tiap `File.Delete`.
**Example:**
```csharp
// Source: gabungan pola 333 (CDPController.cs:2547) + ref-count baru D-10
// Jalan SETELAH SaveChanges/CommitAsync (DB sudah final: path lama sudah null/baris sudah dihapus)
foreach (var relUrl in pathsToDelete) // delete-candidate yang dikumpulkan sebelum/dalam tx
{
    bool stillUsedQ = await _context.PackageQuestions.AnyAsync(q => q.ImagePath == relUrl);
    bool stillUsedO = await _context.PackageOptions.AnyAsync(o => o.ImagePath == relUrl);
    if (stillUsedQ || stillUsedO) continue; // masih dipakai Post/lain → SKIP hapus fisik
    try
    {
        var physical = Path.Combine(_env.WebRootPath,
            relUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(physical)) System.IO.File.Delete(physical);
    }
    catch (Exception fex)
    {
        _logger.LogWarning(fex, "File.Delete post-commit failed (question image): {Path}", relUrl);
    }
}
```
KRITIS: query ref-count harus jalan SETELAH DB final agar baris yang baru dihapus/di-null TIDAK ikut terhitung. `AnyAsync` cukup (lebih murah dari `CountAsync`); pakai EITHER `Any` 2 tabel ATAU `Count`==0 — keduanya valid (CONTEXT D-10 sebut `COUNT==0`). [VERIFIED: pola 333 + skema entity]

### Pattern 3: Sync Pre→Post shared-file (SYN-01 — string copy SAJA)
**What:** Tambah 2 baris di deep-clone `SyncPackagesToPost`. TIDAK ada file op — hanya salin string path+alt.
**Example:**
```csharp
// Source: SyncPackagesToPost (AssessmentAdminController.cs:5370-5383)
var newQ = new PackageQuestion
{
    QuestionText = q.QuestionText, Order = q.Order, ScoreValue = q.ScoreValue,
    QuestionType = q.QuestionType, ElemenTeknis = q.ElemenTeknis,
    Rubrik = q.Rubrik, MaxCharacters = q.MaxCharacters,
    ImagePath = q.ImagePath,  // ← TAMBAH (SYN-01, C-03 shared-file)
    ImageAlt  = q.ImageAlt,   // ← TAMBAH
    Options = q.Options.Select(o => new PackageOption
    {
        OptionText = o.OptionText, IsCorrect = o.IsCorrect,
        ImagePath = o.ImagePath, // ← TAMBAH
        ImageAlt  = o.ImageAlt    // ← TAMBAH
    }).ToList()
};
```
PENTING: `SyncPackagesToPost` menghapus paket Post lama dengan `RemoveRange` (L5345-5351). Karena Post pakai path SAMA dengan Pre (shared), penghapusan baris Post di sini TIDAK boleh `File.Delete` — ref-count akan melihat Pre masih pakai → otomatis aman. Sync sendiri murni DB. [VERIFIED: kode L5337-5391]

### Pattern 4: EditQuestion GET JSON prefill (D-06)
**Example:**
```csharp
// Source: EditQuestion GET JSON (AssessmentAdminController.cs:6214-6230) diperluas
return Json(new {
    id = q.Id, order = q.Order, questionText = q.QuestionText,
    questionType = q.QuestionType ?? "MultipleChoice", scoreValue = q.ScoreValue,
    affectedSessions, elemenTeknis = q.ElemenTeknis, rubrik = q.Rubrik, maxCharacters = q.MaxCharacters,
    imagePath = q.ImagePath, imageAlt = q.ImageAlt,   // ← TAMBAH (D-06)
    options = q.Options.OrderBy(o => o.Id).Select(o => new {
        optionText = o.OptionText, isCorrect = o.IsCorrect,
        imagePath = o.ImagePath, imageAlt = o.ImageAlt  // ← TAMBAH (D-06)
    }).ToList()
});
```
JS `populateEditForm` (view L399) lalu set `<img src=data.imagePath>` thumbnail + isi alt input; `<input type=file>` tetap kosong (D-03 browser security). [VERIFIED: JSON shape existing]

### Pattern 5: Client FileReader thumbnail (D-07a)
```javascript
// Source: vanilla JS konsisten dengan @section Scripts existing (view L278+)
input.addEventListener('change', function () {
    var file = this.files && this.files[0];
    if (!file) return;
    var reader = new FileReader();
    reader.onload = function (e) {
        thumbImg.src = e.target.result;   // <img> 46px di field
        fieldEl.classList.add('has-img'); // flip green state
        metaEl.textContent = file.name + ' · ' + (file.size/1024).toFixed(0) + ' KB';
    };
    reader.readAsDataURL(file);
});
```
Pure client, tanpa round-trip. Saat picker pilih file → uncheck+disable "Hapus" checkbox (mirror D-05, optional UI nicety). [CITED: UI-SPEC Interaction Contract]

### Anti-Patterns to Avoid
- **`File.Delete` tanpa ref-count:** hapus shared file yang masih dipakai Post → gambar Post broken. SELALU ref-count dulu (D-10).
- **`File.Delete` di dalam tx / sebelum CommitAsync:** kalau tx rollback, file sudah hilang tapi DB masih punya path → broken. Pola 333: delete POST commit.
- **Lupa `enctype`:** `IFormFile?` jadi null tanpa error → upload "diam-diam" gagal. D-02 contract.
- **Throw saat File.Delete gagal:** harus warn-only per file (inner try/catch). Disk cleanup ≠ data integrity.
- **Tambah kolom gambar ke Excel import:** D-09 text-only, di luar scope.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Validasi format/size/magic-byte | Cek ekstensi manual | `FileUploadHelper.ValidateImageFile` | Sudah ada Phase 352, magic-byte anti-spoof [VERIFIED] |
| Simpan file + anti-collision name | `File.WriteAllBytes` manual | `FileUploadHelper.SaveFileAsync` | Sudah handle timestamp+GUID, path-traversal strip, auto-mkdir [VERIFIED] |
| Atomic delete tx↔disk | Logika tx baru | Pola Phase 333 (`DeleteCoachingSession`) | Sudah teruji live, inner try/catch warn-only [VERIFIED] |
| Akses path fisik | Hardcode `wwwroot` | `_env.WebRootPath` (AdminBaseController) | DI-injected, env-aware [VERIFIED] |
| Feedback error | Inline validation JS | `TempData["Error"]` + redirect (pola existing) | D-08, konsisten CreateQuestion |

**Key insight:** Phase 353 hampir nol kode "baru" — 90% adalah memanggil helper existing di titik yang tepat + menyalin pola 333. Satu-satunya logika genuinely baru = ref-count query (D-10), dan itu pun 2 baris `AnyAsync`.

## Runtime State Inventory

> Phase ini menulis/menghapus file fisik di disk + baris DB. Bukan rename/migration murni, tapi ada state runtime relevan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Kolom `ImagePath`/`ImageAlt` di `PackageQuestion`+`PackageOption` (migration `20260606030844` applied lokal, re-applied 2026-06-08) | None — kolom siap, no migration baru |
| Live service config | None — fitur ini self-contained di app, tak ada service eksternal | None — verified by pembacaan controller |
| OS-registered state | File fisik di `wwwroot/uploads/questions/{packageId}/` (dibuat `SaveFileAsync`); shared antar Pre↔Post (1 file, N baris) | Ref-count D-10 lindungi dari hapus prematur |
| Secrets/env vars | None — tidak ada secret/env var baru | None |
| Build artifacts | None — tidak ada package/egg baru | None |

**Orphan risk terdokumentasi (DEFER, bukan bug):** Cascade besar `DeleteAssessment`/`DeleteAssessmentGroup`/`DeletePrePostGroup` (L2069/L2257/L2443) hapus soal→opsi TANPA file cleanup → file orphan di disk. Sengaja di luar scope (D-12). Severity rendah (sampah disk, bukan corruption). Akan dibersihkan phase cleanup terpisah. [CITED: CONTEXT D-12 + deferred]

## Common Pitfalls

### Pitfall 1: Lupa `enctype="multipart/form-data"`
**What goes wrong:** Semua `IFormFile?` = null, alt text tetap masuk, gambar "hilang" tanpa error.
**Why:** Default form encoding `application/x-www-form-urlencoded` tidak kirim binary.
**How to avoid:** D-02 — tambah `enctype` di `#questionForm` (L122). Test: upload → cek `questionImage != null` di breakpoint/log.
**Warning signs:** Alt text tersimpan tapi ImagePath null.

### Pitfall 2: Ref-count menghitung baris yang baru dihapus
**What goes wrong:** Query ref-count jalan SEBELUM SaveChanges → baris yang sedang dihapus masih kehitung → `count>0` → file tak pernah dihapus (orphan permanen).
**Why:** EF change-tracker belum flush ke DB.
**How to avoid:** Jalankan ref-count SETELAH `SaveChangesAsync`/`CommitAsync`. Untuk replace (EditQuestion): set `ImagePath=newPath` + SaveChanges DULU, baru ref-count old path.
**Warning signs:** File lama tetap di disk setelah replace/delete tunggal (non-shared).

### Pitfall 3: Konflik D-05 (hapus checked + file baru) salah resolve
**What goes wrong:** Checkbox "Hapus" diproses lebih dulu → set null → file baru tak tersimpan, atau dua-duanya jalan.
**Why:** Urutan operasi handler.
**How to avoid:** D-05 — cek `newFile != null` DULU. Jika ada file baru: SaveFileAsync, set path baru, abaikan checkbox, old path → delete-candidate. Jika TIDAK ada file baru DAN checkbox checked: set null, old path → delete-candidate.
**Warning signs:** Centang hapus + pilih file → gambar hilang bukan terganti.

### Pitfall 4: File.Delete shared file menghancurkan Post
**What goes wrong:** Hapus soal Pre → `File.Delete` path → gambar Post (share path sama) jadi broken `<img>`.
**Why:** Lupa ref-count (D-10) untuk shared-file C-03.
**How to avoid:** SELALU `AnyAsync(q=>q.ImagePath==path) || AnyAsync(o=>o.ImagePath==path)` == false sebelum delete.
**Warning signs:** Gambar Post hilang setelah edit/hapus Pre.

### Pitfall 5: Pakai `FileUploadHelper.DeleteFile` (tanpa ref-count)
**What goes wrong:** `DeleteFile` langsung `File.Delete` tanpa cek referensi → hapus shared file.
**Why:** Helper ini dipakai cert (1:1 file:row), bukan shared-file.
**How to avoid:** JANGAN pakai `DeleteFile` untuk Phase 353. Pakai inline pola 333 + ref-count manual.
**Warning signs:** Lihat Pitfall 4.

### Pitfall 6: `ImageAlt` > 255 char melanggar `[MaxLength(255)]`
**What goes wrong:** EF throw `DbUpdateException` saat SaveChanges bila alt text > 255.
**Why:** Kolom `nvarchar(255)`.
**How to avoid:** Trim/truncate `imageAlt` ke 255 di handler sebelum assign, atau validasi server.
**Warning signs:** Error simpan saat alt panjang.

## Code Examples

### Operasi: handler per-item replace/remove dengan delete-candidate (EditQuestion POST)
```csharp
// Pseudocode pola handler per gambar (question + tiap opsi sama bentuk)
var imagePathsToDelete = new List<string>();

// 1. Validasi semua file dulu (fail-fast → TempData["Error"] + redirect, D-08)
foreach (var f in new[]{ questionImage, optionAImage, optionBImage, optionCImage, optionDImage }) {
    var (ok, err) = FileUploadHelper.ValidateImageFile(f);
    if (!ok) { TempData["Error"] = err; return RedirectToAction("ManagePackageQuestions", new { packageId }); }
}

// 2. Per item — resolve niat (D-05: file baru menang)
async Task ApplyImage(Func<string?> getOld, Action<string?> setPath, Action<string?> setAlt,
                      IFormFile? newFile, bool removeChecked, string? alt) {
    if (newFile != null) {                                  // file baru → ganti
        var saved = await FileUploadHelper.SaveFileAsync(newFile, _env.WebRootPath,
                        $"uploads/questions/{packageId}", _logger);
        var old = getOld(); if (!string.IsNullOrEmpty(old)) imagePathsToDelete.Add(old);
        setPath(saved); setAlt(Truncate(alt, 255));
    } else if (removeChecked) {                             // hapus
        var old = getOld(); if (!string.IsNullOrEmpty(old)) imagePathsToDelete.Add(old);
        setPath(null); setAlt(null);
    } else {                                                // keep — alt boleh update
        setAlt(Truncate(alt, 255));
    }
}
// ... panggil ApplyImage untuk q + tiap opsi (opsi: getOld dari PackageOption yang match)
await _context.SaveChangesAsync();
// 3. ref-count + File.Delete POST save (Pattern 2)
```
NOTE: untuk opsi, karena EditQuestion saat ini `RemoveRange` lalu re-add opsi baru (L6302-6320), gambar opsi lama HILANG referensinya kalau opsi di-recreate. **Planner harus putuskan**: pertahankan ImagePath opsi saat re-add (match by posisi A-D) ATAU ubah strategi opsi jadi update-in-place. Ini risiko desain kritis (lihat Open Question 1).

## State of the Art

| Old Approach | Current Approach | When | Impact |
|--------------|------------------|------|--------|
| CreateQuestion text-only | + IFormFile multipart | Phase 353 | Signature diperluas |
| `_PreviewQuestion` teks saja | + `<img>` render | Phase 353 | RND-04 admin |
| `SyncPackagesToPost` clone tanpa gambar | + ImagePath/Alt copy | Phase 353 | SYN-01 |
| DeletePackage/DeleteQuestion no file cleanup | + atomic delete + ref-count | Phase 353 | SYN-02/D-10/D-11 |

**Deprecated/outdated:** Tidak ada. Fitur murni aditif.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Folder `uploads/questions/{packageId}/` (C-02) belum dipakai modul lain → tak ada false-positive ref-count cross-modul | Pattern 2 | Rendah — ref-count query hanya cek PackageQuestion+PackageOption, scoped per kolom; aman walau folder dipakai modul lain |
| A2 | ASP.NET model binding mengikat `IFormFile?` per nama tanpa DTO untuk layout fixed (bukan list dinamis) | Pattern 1 | Rendah — konvensi standar MVC, layout A-D fixed [ASSUMED dari pengetahuan framework, bukan diverifikasi di sesi ini]; planner verifikasi via dotnet run smoke |
| A3 | `EditQuestion` re-add opsi (RemoveRange+re-create) akan kehilangan ImagePath opsi kecuali ditangani eksplisit | Code Examples + OQ1 | TINGGI — bila tak ditangani, edit soal apapun menghapus gambar opsi. Planner WAJIB tangani |

## Open Questions

1. **EditQuestion opsi: RemoveRange+re-create menghilangkan ImagePath opsi.**
   - What we know: EditQuestion POST L6302 `RemoveRange(q.Options)` lalu re-add `PackageOption` baru dari `optionA..D` (L6314-6320). Opsi baru TIDAK bawa ImagePath/ImageAlt lama.
   - What's unclear: apakah preserve gambar opsi by-posisi (A→A) saat re-add, ATAU ubah ke update-in-place (cocokkan opsi existing by urutan, update text/correct, apply image).
   - Recommendation: **update-in-place** lebih aman untuk gambar (preserve Id + ImagePath, hanya update bila ada file baru/remove). Bila tetap RemoveRange, harus carry-forward ImagePath opsi lama by posisi + handle delete-candidate untuk opsi yang benar-benar hilang (mis. dari 4 opsi → 3). Planner putuskan + dokumentasikan. Ini risiko regresi tertinggi phase.

2. **Replace gambar: ref-count old path saat shared.**
   - What we know: replace di Pre → SaveChanges path baru → old path mungkin masih dipakai Post (belum di-sync).
   - What's unclear: urutan replace vs auto-sync. Auto-sync (SyncPackagesToPost) jalan SETELAH SaveChanges di CreateQuestion/EditQuestion (L6357-6369), dan sync REPLACE seluruh paket Post (RemoveRange + re-clone dengan path BARU).
   - Recommendation: jalankan ref-count + File.Delete old path SETELAH auto-sync selesai. Setelah sync, Post sudah pakai path baru → old path benar-benar tak terpakai → aman dihapus. Urutan: SaveChanges → auto-sync → ref-count old path → delete. Planner pastikan urutan ini.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | build + run | ✓ (asumsi, project net8.0) | net8.0 | — |
| SQL Server (SQLEXPRESS) HcPortalDB_Dev | DB lokal verifikasi | ✓ (per memory + CLAUDE.md) | — | — |
| `wwwroot/uploads/` writable | SaveFileAsync | ✓ (auto-create dir) | — | — |

**Missing dependencies with no fallback:** None.
Seed data kemungkinan TIDAK diperlukan (ini CRUD, bukan seed) — bila butuh paket Pre+Post linked untuk uji sync, ikuti SEED_WORKFLOW (snapshot→journal→restore). [CITED: CLAUDE.md]

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + EF Core InMemory 8.0.0 + SqlServer 8.0.0 (integration) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~Image"` |
| Full suite command | `dotnet test HcPortal.Tests` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| IMG-01/02 | Validasi upload valid/invalid (JPG/PNG ≤5MB, tolak PDF/oversize/magic) | unit | `dotnet test --filter "FullyQualifiedName~ValidateImageFile"` | ✅ (FileUploadHelperTests.cs — Phase 352, sudah cover) |
| SYN-01 | Sync Pre→Post bawa ImagePath+ImageAlt | integration | `dotnet test --filter "FullyQualifiedName~SyncImage"` | ❌ Wave 0 |
| SYN-02/D-10 | Ref-count: shared file TIDAK dihapus saat 1 ref masih ada; dihapus saat 0 ref | integration | `dotnet test --filter "FullyQualifiedName~RefCount"` | ❌ Wave 0 |
| D-11 | DeletePackage hapus file non-shared, skip shared | integration | `dotnet test --filter "FullyQualifiedName~DeletePackageImage"` | ❌ Wave 0 |
| IMG-05/D-05 | Konflik hapus+file baru → file baru menang | integration | `dotnet test --filter "FullyQualifiedName~ReplaceConflict"` | ❌ Wave 0 |
| RND-04, IMG-07, D-06 | Render preview + prefill JSON | manual/Playwright | UAT lokal `http://localhost:5277` | ❌ (Phase 355 konsolidasi UAT) |

Catatan: ref-count + File.Delete uji butuh filesystem nyata (temp dir, pola `MakeTempDir` di FileUploadHelperTests.cs:41) + EF SqlServer/InMemory untuk query. Sync/ref-count logic bisa diisolasi via InMemory DB. File.Delete actual butuh temp dir nyata. Pola `MakeTempDir` + `MakeFile` sudah ada — reuse.

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter "~Image"` (quick)
- **Per wave merge:** `dotnet test HcPortal.Tests` (full, target ≥112 baseline hijau)
- **Phase gate:** Full suite green + `dotnet run` localhost:5277 smoke (upload/replace/delete/sync visual) sebelum verify

### Wave 0 Gaps
- [ ] `HcPortal.Tests/QuestionImageSyncTests.cs` — SYN-01 deep-clone copy ImagePath/Alt (InMemory DB)
- [ ] `HcPortal.Tests/QuestionImageRefCountTests.cs` — D-10 shared-file ref-count (skip vs delete) + D-11 DeletePackage (temp dir + InMemory)
- [ ] Reuse `MakeTempDir`/`MakeFile` helper (currently private di FileUploadHelperTests) — pertimbangkan extract ke shared test helper, atau duplikasi (planner putuskan)
- [ ] Framework install: NONE — xUnit + EF InMemory sudah terpasang [VERIFIED: csproj]

*(IMG-01/02 validasi sudah tercover penuh oleh FileUploadHelperTests Phase 352 — tidak perlu test baru untuk validasi murni.)*

## Security Domain

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V4 Access Control | yes | `[Authorize(Roles="Admin, HC")]` + `[ValidateAntiForgeryToken]` di semua POST (existing) |
| V5 Input Validation | yes | `ValidateImageFile` (ekstensi+size+magic-byte), `SaveFileAsync` strip path-traversal |
| V12 File Upload | yes | Allowlist JPG/PNG, cap 5MB, magic-byte anti-spoof, filename sanitize (Phase 325/352) |

### Known Threat Patterns for ASP.NET upload
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Upload executable disamarkan .png | Tampering | Magic-byte check `MatchesMagicByte` (existing) |
| Path traversal filename `../../` | Tampering | `Path.GetFileName` strip + audit log (SaveFileAsync existing) |
| Oversize DoS | DoS | Cap 5MB di `ValidateImageFile`; pertimbangkan cek `[RequestSizeLimit]` global (existing app config) |
| CSRF | Spoofing | `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` (existing form + POST) |
| Akses non-admin | Elevation | `[Authorize(Roles="Admin, HC")]` (existing) |

Semua kontrol sudah ADA via Phase 325/352 helper + atribut existing. Phase 353 hanya WAJIB memanggil `ValidateImageFile` SEBELUM `SaveFileAsync` di tiap titik upload. [VERIFIED]

## Project Constraints (from CLAUDE.md)
- Respond in Bahasa Indonesia.
- Verifikasi lokal WAJIB sebelum push: `dotnet build` + `dotnet run` (cek `http://localhost:5277`) + cek DB lokal + Playwright bila UI.
- JANGAN edit kode/DB langsung di Dev (10.55.3.3)/Prod. Promosi = tanggung jawab Team IT.
- JANGAN push tanpa verifikasi lokal. Notifikasi IT dengan commit hash + flag migration (Phase 353: **TIDAK ada migration baru** — kolom sudah ada).
- Seed Workflow: bila butuh seed (uji sync Pre+Post), klasifikasi→snapshot→journal→restore. Kemungkinan tak perlu (CRUD).

## Sources

### Primary (HIGH confidence)
- `Controllers/AssessmentAdminController.cs` — CreateQuestion L6067, EditQuestion GET/POST L6196/6241, DeleteQuestion L6377, DeletePackage L5457, SyncPackagesToPost L5337
- `Controllers/CDPController.cs:2456-2563` — pola atomic delete Phase 333 (referensi SYN-02/C-04)
- `Controllers/AdminBaseController.cs:18` — `_env.WebRootPath`
- `Helpers/FileUploadHelper.cs` — ValidateImageFile L45, SaveFileAsync L75, DeleteFile L114
- `Models/AssessmentPackage.cs` — entity ImagePath/ImageAlt L60/64/89/93
- `Models/AssessmentConstants.cs` — AllowedImageExtensions, MaxImageFileSizeBytes 5MB, magic-byte
- `Views/Admin/ManagePackageQuestions.cshtml` — form L122, opsi L150-163, JS prefill L399
- `Views/Admin/_PreviewQuestion.cshtml` — render target RND-04
- `HcPortal.Tests/FileUploadHelperTests.cs` — pola test (MakeFile/MakeTempDir) + coverage validasi existing
- `HcPortal.Tests/HcPortal.Tests.csproj` — xUnit 2.9.3 + EF InMemory/SqlServer 8.0.0
- `.planning/config.json` — nyquist_validation: true

### Secondary (MEDIUM confidence)
- `.planning/phases/353-.../353-CONTEXT.md` + `353-UI-SPEC.md` — decisions D-01..D-12, C-01..05

### Tertiary (LOW confidence)
- Pengetahuan framework ASP.NET model binding `IFormFile?` (A2) — diverifikasi via dotnet run smoke saat eksekusi

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua dibaca dari repo, nol library baru
- Architecture/wiring: HIGH — pola 333/352 live, titik integrasi terverifikasi by line number
- Ref-count (D-10): HIGH untuk shape, MEDIUM untuk urutan vs auto-sync (lihat OQ2)
- EditQuestion opsi RemoveRange (A3/OQ1): risiko TINGGI desain — flagged untuk planner
- Pitfalls: HIGH — diturunkan dari skema shared-file + pola tx existing

**Research date:** 2026-06-08
**Valid until:** 2026-07-08 (kode stabil brownfield; valid selama tak ada refactor AssessmentAdminController/FileUploadHelper)
