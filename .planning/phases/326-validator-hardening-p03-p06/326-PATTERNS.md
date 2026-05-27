# Phase 326: Validator Hardening (P03 + P06) — Pattern Map

**Mapped:** 2026-05-27
**Files analyzed:** 4 file modifikasi
**Analogs found:** 4 / 4 (semua exact match dalam file yang sama atau sibling Razor view)

---

## File Classification

| File | Action | Role | Data Flow | Closest Analog | Match Quality |
|------|--------|------|-----------|----------------|---------------|
| `Controllers/TrainingAdminController.cs` | MODIFY (insertion 3 lokasi: L254+L264 Add POST, L408+L442 Edit GET+POST) | controller | request-response (form POST + DB query) | self (L241-254 `srcAlreadyRenewed` symmetric branch) | exact (same file, same flow, same pattern) |
| `Models/EditTrainingRecordViewModel.cs` | MODIFY (extend +3 field nullable) | model (ViewModel) | request-response | `Models/CreateTrainingRecordViewModel.cs:59-60` | exact (sibling VM, mirror field) |
| `Views/Admin/EditTraining.cshtml` | MODIFY (insertion section L125-L127 + span L153) | view (Razor) | request-response | self (L42-125 card "Data Training" + L127-179 "Data Sertifikat" + L37-40 hidden inputs + L52/L59/L78 field-level error span) | exact (same file pattern reuse) |
| `Views/Admin/AddTraining.cshtml` | MODIFY (tambah span asp-validation-for L198+) | view (Razor) | request-response | self L198 area + EditTraining L52/L59 pattern | role-match (cross-file pattern transfer) |

---

## Confirmed Open Item (D-08): AssessmentSession Date Field

**Source:** `Models/AssessmentSession.cs` (read 2026-05-27)

**Verdict:** Tidak ada field bernama `Tanggal` atau `TanggalMulai`. Field tanggal yang ada:

| Field | Tipe | Semantik | Verdict untuk DAG check |
|-------|------|----------|-------------------------|
| `Schedule` (L18) | `DateTime` non-null | Jadwal/schedule ujian (timestamp ujian dimulai) | **CANDIDATE** — Bukan tanggal sertifikat issued, tapi semantik "tanggal sertifikat ada" untuk AS = tanggal session jadwal (ujian dilaksanakan = tanggal sertifikat lahir kalau passed) |
| `CompletedAt` (L39) | `DateTime?` nullable | Timestamp completion ujian | Risk: NULL kalau session belum complete — validator akan throw / lolos salah |
| `StartedAt` (L40) | `DateTime?` nullable | Timestamp ujian dimulai aktual | Sama risk null |
| `CreatedAt` (L86) | `DateTime` non-null UtcNow default | Audit field — timestamp record dibuat di DB | Bukan tanggal semantik sertifikat |
| `ValidUntil` (L65) | `DateTime?` nullable | Expiry sertifikat | BUKAN issued date |

**Rekomendasi PATTERNS untuk planner:** Pakai **`Schedule`** (L18, `DateTime` non-null) sebagai field setara dengan `TrainingRecord.Tanggal`. Rasional:
1. Non-null guarantee (no NullReferenceException risk di validator)
2. Semantik = tanggal sertifikat issued untuk AS context (saat schedule itulah ujian dilaksanakan dan sertifikat lahir jika passed)
3. `CompletedAt` lebih akurat tapi nullable — risk kalau renewal source AS belum complete (edge case anomali tapi possible kalau admin renew prematur)

**Alternatif fallback pattern (defensive):**
```csharp
var srcAsDate = srcAs.CompletedAt ?? srcAs.Schedule;
if (srcAsDate >= model.Tanggal) ModelState.AddModelError(...);
```

**Open lock:** Plan-phase WAJIB lock pilihan single-field vs fallback pattern. Default rekomendasi: **`Schedule` single field** (sederhana + non-null + semantik OK).

---

## Pattern Assignments

### 1. `Controllers/TrainingAdminController.cs` (controller, request-response)

**Analog:** self — symmetric branch pattern di L241-254

#### Pattern A: P03 Add Validator (insertion setelah L254 srcAlreadyRenewed)

**Source pattern (L241-254):**
```csharp
if (model.RenewsTrainingId.HasValue)
{
    var srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(t => t.RenewsTrainingId == model.RenewsTrainingId)
        || await _context.AssessmentSessions.AnyAsync(a => a.RenewsTrainingId == model.RenewsTrainingId && a.IsPassed == true);
    if (srcAlreadyRenewed)
        ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
}
if (model.RenewsSessionId.HasValue)
{
    var srcAlreadyRenewed = await _context.TrainingRecords.AnyAsync(t => t.RenewsSessionId == model.RenewsSessionId)
        || await _context.AssessmentSessions.AnyAsync(a => a.RenewsSessionId == model.RenewsSessionId && a.IsPassed == true);
    if (srcAlreadyRenewed)
        ModelState.AddModelError("", "Sertifikat ini sudah di-renew sebelumnya.");
}
```

**To mirror (insertion setelah L254, sebelum L255 mixed-type bulk validation):**
```csharp
// P03: DAG enforcement — tanggal renewal harus > tanggal source (monotonic)
if (model.RenewsTrainingId.HasValue)
{
    var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
    if (src != null && src.Tanggal >= model.Tanggal)
        ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
}
if (model.RenewsSessionId.HasValue)
{
    var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
    if (srcAs != null && srcAs.Schedule >= model.Tanggal)
        ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
}
```

**Adaptation notes:**
- Sama: pattern double-branch `if (RenewsTrainingId.HasValue) {...} if (RenewsSessionId.HasValue) {...}` (parity sempurna)
- Sama: `await _context.{Table}.FindAsync(id)` lookup single record
- Sama: `ModelState.AddModelError("", message)` summary-level (key `""` = tampil di `<div asp-validation-summary="All">` di view)
- Beda: pakai `FindAsync(id.Value)` bukan `AnyAsync(predicate)` karena perlu field `.Tanggal` / `.Schedule` dari record
- Beda: `srcAs.Schedule` (AssessmentSession) bukan `srcAs.Tanggal` (yang tidak ada)

**Risk/pitfall:**
- Ordering matter: insertion HARUS setelah L254 (srcAlreadyRenewed) tapi SEBELUM L264 (`if (!ModelState.IsValid)`) — else validator skip kalau ModelState sudah invalid (acceptable, tapi spec mengatur ordering jelas)
- Null check `if (src != null)` mandatory — `FindAsync` return null kalau FK invalid (defense kalau form tampering)
- `>=` strict (per D-10) — same-day reject ikut error path

#### Pattern B: P06 Add Validator (insertion sebelum L264)

**No direct analog di file** — pattern baru tapi trivial synchronous check.

**To insert (sebelum L264 `if (!ModelState.IsValid)`):**
```csharp
// P06: Permanent + ValidUntil mutual exclusion
if (model.CertificateType == "Permanent" && model.ValidUntil != null)
    ModelState.AddModelError("ValidUntil", "Sertifikat Permanent tidak boleh punya tanggal expired.");
```

**Adaptation notes:**
- Key `"ValidUntil"` (field-level, BUKAN summary `""`) per D-05 — tampil di `<span asp-validation-for="ValidUntil">` di view
- String value `"Permanent"` match dropdown option di `Views/Admin/EditTraining.cshtml:144` + `AddTraining.cshtml:189`
- No DB query — pure sync check

**Risk/pitfall:**
- Case sensitivity: `"Permanent"` capital-P — match exact dropdown value (verifikasi di view L144 `<option value="Permanent">Permanent</option>`)
- Ordering: BEBAS antara D-01 dan L264 (cluster semua validator P03+P06 sebelum L264 gate)

#### Pattern C: GetTraining Edit Populate RenewalSourceTitle (insertion sebelum L434 `await SetTrainingCategoryViewBag()`)

**Source pattern (L276-285 AddTraining ViewBag populate):**
```csharp
if (model.RenewsTrainingId != null)
{
    var src = await _context.TrainingRecords.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == model.RenewsTrainingId);
    if (src != null) { ViewBag.RenewalSourceTitle = src.Judul ?? ""; ViewBag.RenewalSourceUserName = src.User?.FullName ?? ""; }
}
else if (model.RenewsSessionId != null)
{
    var src = await _context.AssessmentSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == model.RenewsSessionId);
    if (src != null) { ViewBag.RenewalSourceTitle = src.Title ?? ""; ViewBag.RenewalSourceUserName = src.User?.FullName ?? ""; }
}
```

**To mirror di EditTraining GET (insertion antara L433 `ExistingSertifikatUrl = record.SertifikatUrl,` mapping VM dan L434 `await SetTrainingCategoryViewBag()`):**
```csharp
// VM mapping extension Phase 326 (D-06)
RenewsTrainingId = record.RenewsTrainingId,
RenewsSessionId = record.RenewsSessionId,
// ... (di akhir VM constructor)

// Populate RenewalSourceTitle (D-07)
if (model.RenewsTrainingId != null)
{
    var src = await _context.TrainingRecords.FirstOrDefaultAsync(t => t.Id == model.RenewsTrainingId);
    model.RenewalSourceTitle = src?.Judul ?? "(sertifikat sumber tidak ditemukan)";
}
else if (model.RenewsSessionId != null)
{
    var srcAs = await _context.AssessmentSessions.FirstOrDefaultAsync(s => s.Id == model.RenewsSessionId);
    model.RenewalSourceTitle = srcAs?.Title ?? "(sertifikat sumber tidak ditemukan)";
}
```

**Adaptation notes:**
- Sama: pattern lookup TR or AS dengan `FirstOrDefaultAsync` per FK present
- Sama: fallback null-coalesce dengan string default
- Beda: target VM field (`model.RenewalSourceTitle`) bukan `ViewBag.RenewalSourceTitle` — karena Edit form punya VM nyangkut field (D-06 extension)
- Beda: AS field `srcAs.Title` (verified L13 `Title` property AssessmentSession) — NOT `.Judul`
- Beda: tanpa `Include(.User)` karena UI EditTraining tidak display worker name source (cuma title)

**Risk/pitfall:**
- TR field = `Judul`, AS field = `Title` — beda nama (verified Models/AssessmentSession.cs:13 vs TrainingRecord.Judul)
- Fallback string `"(sertifikat sumber tidak ditemukan)"` — verbatim per UI-SPEC copywriting contract L180

#### Pattern D: EditTraining POST Add Validators + Self-Renewal Guard (insertion sebelum L453 `if (!ModelState.IsValid)`)

**Source pattern existing (L453-461 firstError compress):**
```csharp
if (!ModelState.IsValid)
{
    var firstError = ModelState.Values
        .SelectMany(v => v.Errors)
        .Select(e => e.ErrorMessage)
        .FirstOrDefault() ?? "Data tidak valid.";
    TempData["Error"] = firstError;
    return RedirectToAction("ManageAssessment", "AssessmentAdmin", new { tab = "training" });
}
```

**To insert sebelum L453 (setelah L451 closing `}` of editValid check):**
```csharp
// P03 DAG check (mirror Add D-01)
if (model.RenewsTrainingId.HasValue)
{
    var src = await _context.TrainingRecords.FindAsync(model.RenewsTrainingId.Value);
    if (src != null && src.Tanggal >= model.Tanggal)
        ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
}
if (model.RenewsSessionId.HasValue)
{
    var srcAs = await _context.AssessmentSessions.FindAsync(model.RenewsSessionId.Value);
    if (srcAs != null && srcAs.Schedule >= model.Tanggal)
        ModelState.AddModelError("", "Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew.");
}

// P03 Self-renewal guard (D-07 defense kalau form tampering)
if (model.RenewsTrainingId.HasValue && model.RenewsTrainingId.Value == model.Id)
    ModelState.AddModelError("", "Sertifikat tidak boleh renewal dirinya sendiri.");

// P06 Permanent + ValidUntil (mirror Add D-02)
if (model.CertificateType == "Permanent" && model.ValidUntil != null)
    ModelState.AddModelError("ValidUntil", "Sertifikat Permanent tidak boleh punya tanggal expired.");
```

**Adaptation notes:**
- Sama struktur kontrol per Pattern A + Pattern B
- Beda: Edit punya `model.Id` (Add tidak) → self-renewal check feasible HANYA di Edit (per spec line 217)
- Beda hasil display UX: existing flow L453-461 compress jadi `firstError` toast → P06 di Edit BUKAN field-level (acceptable per CONTEXT L177-178 + UI-SPEC L222-224)

**Risk/pitfall:**
- Persist mapping VM → record di L479-490 BELUM include `RenewsTrainingId`/`RenewsSessionId` — JANGAN lupa tambah:
  ```csharp
  record.RenewsTrainingId = model.RenewsTrainingId;
  record.RenewsSessionId = model.RenewsSessionId;
  ```
  Else "Hapus link renewal" button user click → submit form → backend ignore → bug bukan fix
- Mapping HARUS ditambah karena scope D-07 "user bisa clear renewal" jadi vacuous tanpa persist
- Self-renewal check pakai `RenewsTrainingId.Value == model.Id` (TR↔TR self) — AS self tidak ada karena tabel beda (TR vs AS — FK cross-table tidak bisa point ke diri sendiri secara physical)

---

### 2. `Models/EditTrainingRecordViewModel.cs` (model, request-response)

**Analog:** `Models/CreateTrainingRecordViewModel.cs:59-60`

**Source pattern (CreateTrainingRecordViewModel.cs:59-60):**
```csharp
public int? RenewsTrainingId { get; set; }
public int? RenewsSessionId { get; set; }
```

**To add (di EditTrainingRecordViewModel.cs sebelum closing brace L65, setelah L64 `ExistingSertifikatUrl`):**
```csharp
// Phase 326 D-06: Renewal FK passthrough + display title
public int? RenewsTrainingId { get; set; }
public int? RenewsSessionId { get; set; }
public string? RenewalSourceTitle { get; set; }
```

**Adaptation notes:**
- Sama: 2 FK field nullable int identik dengan Create VM
- Tambahan: `RenewalSourceTitle` string nullable — DISPLAY-ONLY (tidak dibind dari form karena tidak ada input), populated server-side di GetTraining Edit
- Tidak perlu `[Display(Name=...)]` attribute — hidden input passthrough only, label sudah explicit di Razor `<label class="form-label fw-bold">Renewal dari</label>`

**Risk/pitfall:**
- Backward compat OK — tambah 3 nullable field tidak break existing form POST (binding ignore missing field, fallback null)
- `RenewalSourceTitle` JANGAN diberi `[Required]` — display-only, akan null kalau no renewal FK present

---

### 3. `Views/Admin/EditTraining.cshtml` (view, request-response)

**Analog:** self

#### Pattern A: Section Card "Renewal Source" (insertion antara L125-L127)

**Source pattern (EditTraining.cshtml:42-46 card "Data Training" header + L127-131 "Data Sertifikat"):**
```razor
<div class="card border-0 shadow-sm mb-4">
    <div class="card-header bg-white py-3">
        <h5 class="mb-0 fw-bold"><i class="bi bi-journal-text me-2"></i>Data Training</h5>
    </div>
    <div class="card-body">
```

**Source hidden input pattern (L37-40):**
```razor
<input asp-for="Id" type="hidden" />
<input asp-for="WorkerId" type="hidden" />
<input asp-for="WorkerName" type="hidden" />
<input asp-for="ExistingSertifikatUrl" type="hidden" />
```

**To insert (markup contract dari UI-SPEC.md L115-146 verbatim — locked):**
```razor
@if (Model.RenewsTrainingId.HasValue || Model.RenewsSessionId.HasValue)
{
    <div id="renewalSourceSection" class="card border-0 shadow-sm mb-4">
        <div class="card-header bg-white py-3">
            <h5 class="mb-0 fw-bold">
                <i class="bi bi-arrow-repeat me-2 text-primary"></i>Renewal Source
            </h5>
        </div>
        <div class="card-body">
            <div class="row g-3 align-items-end">
                <div class="col-md-9">
                    <label class="form-label fw-bold">Renewal dari</label>
                    <p class="form-control-plaintext mb-0">
                        @(Model.RenewalSourceTitle ?? "(sertifikat sumber tidak ditemukan)")
                    </p>
                </div>
                <div class="col-md-3 text-md-end">
                    <button type="button"
                            class="btn btn-outline-danger btn-sm"
                            onclick="document.getElementById('RenewsTrainingId').value='';document.getElementById('RenewsSessionId').value='';document.getElementById('renewalSourceSection').style.display='none';return false;">
                        <i class="bi bi-x-circle me-1"></i>Hapus link renewal
                    </button>
                </div>
            </div>
            <input type="hidden" id="RenewsTrainingId" name="RenewsTrainingId" value="@Model.RenewsTrainingId" />
            <input type="hidden" id="RenewsSessionId" name="RenewsSessionId" value="@Model.RenewsSessionId" />
        </div>
    </div>
}
```

**Adaptation notes:**
- Sama: `card border-0 shadow-sm mb-4` + `card-header bg-white py-3` + `h5 mb-0 fw-bold` (mirror L42, L127)
- Sama: hidden input pattern (raw `<input type="hidden">` BUKAN `asp-for` — pakai raw karena perlu eksplisit `id+name` untuk JS lookup `document.getElementById`)
- Beda: conditional `@if` guard — section render hanya kalau renewal FK present
- Beda: `<p class="form-control-plaintext">` (Bootstrap 5 read-only display) — pattern baru tapi standar Bootstrap

**Risk/pitfall:**
- Insertion point: STRICT antara L125 (closing `</div>` Data Training card) dan L127 (opening `<div>` Data Sertifikat card) — verifikasi pre-insert
- JS handler inline: HARUS single-line (UI-SPEC verification checklist L283 "Inline onclick = single line, no multiline")
- Copy "Hapus link renewal" + "Renewal dari" + "(sertifikat sumber tidak ditemukan)" LOCKED verbatim per UI-SPEC L177-188

#### Pattern B: ValidUntil Field-Level Error Span (insertion setelah L153)

**Source pattern (EditTraining.cshtml:52, 59, 78 — existing field error span):**
```razor
<span asp-validation-for="Judul" class="text-danger small"></span>
<span asp-validation-for="Penyelenggara" class="text-danger small"></span>
<span asp-validation-for="Kategori" class="text-danger small"></span>
```

**Current state L150-154 (verified — NO span exists):**
```razor
<!-- Berlaku Sampai -->
<div class="col-md-6">
    <label asp-for="ValidUntil" class="form-label fw-bold">Berlaku Sampai</label>
    <input asp-for="ValidUntil" type="date" class="form-control" />
</div>
```

**To insert (after L153 `<input asp-for="ValidUntil" ... />`, before closing `</div>` L154):**
```razor
<span asp-validation-for="ValidUntil" class="text-danger small"></span>
```

**Adaptation notes:**
- Pattern identik dengan L52/L59/L78 — `<span asp-validation-for="..." class="text-danger small"></span>`
- Tampil ketika `ModelState.AddModelError("ValidUntil", "...")` di-trigger (P06 fix)
- **Caveat Edit:** UX di Edit actually tampil sebagai TempData toast (compressed firstError) BUKAN field-level karena flow `RedirectToAction` L460. Span tetap tambah untuk consistency + safety (kalau future refactor ke `return View(model)` flow).

**Risk/pitfall:**
- Grep konfirmasi 2026-05-27: `asp-validation-for="ValidUntil"` ABSENT di seluruh `Views/Admin/` — Phase 326 pertama yang tambah
- Cek juga `EditManualAssessment.cshtml:133-134` + `AddManualAssessment.cshtml:217-222` — sama absent. Out-of-scope Phase 326 (assessment manual entry beda flow), tapi catat untuk Phase 327+.

---

### 4. `Views/Admin/AddTraining.cshtml` (view, request-response)

**Analog:** `Views/Admin/EditTraining.cshtml:52,59,78` field-level error span pattern

**Current state L195-199 (verified — NO span exists):**
```razor
<!-- Berlaku Sampai (shared) -->
<div class="col-md-6">
    <label asp-for="ValidUntil" class="form-label fw-bold">Berlaku Sampai <i class="bi bi-question-circle text-muted" data-bs-toggle="tooltip" data-bs-placement="top" title="Tanggal kedaluwarsa sertifikat. Wajib diisi jika tipe bukan Permanent"></i></label>
    <input asp-for="ValidUntil" type="date" class="form-control" />
</div>
```

**To insert (after L198 `<input asp-for="ValidUntil" ... />`, before closing `</div>` L199):**
```razor
<span asp-validation-for="ValidUntil" class="text-danger small"></span>
```

**Adaptation notes:**
- Identik dengan EditTraining Pattern B
- **UX difference (penting):** Add flow PAKAI `return View(model)` (L287) → ModelState preserved → span tampil FIELD-LEVEL native (per UI-SPEC L223-224). Add = full UX field-level error. Edit = compressed toast (acceptable tradeoff).

**Risk/pitfall:**
- L195 comment `<!-- Berlaku Sampai (shared) -->` — "shared" karena field ini muncul di kedua mode renewal/non-renewal AddTraining. Span insertion tetap di L198 area (1 lokasi cukup, bukan duplikat per mode).
- TIDAK tambah label-side `<span class="text-danger">*</span>` — `ValidUntil` bukan required field, hanya conditional invalid kalau Permanent+nonnull

---

## Shared Patterns

### Shared 1: ModelState Error Key Convention

**Source:** `Controllers/TrainingAdminController.cs:238,246,253` (existing summary-level pattern)

**Apply to:** Semua validator Phase 326

| Key | Display lokasi | Use case |
|-----|----------------|----------|
| `""` (empty string) | `<div asp-validation-summary="All" class="alert alert-danger">` (EditTraining.cshtml:30, AddTraining setara) | Errors yang tidak terikat 1 field — cross-field validation (P03 monotonic, P03 self-renewal, mutual exclusion) |
| `"ValidUntil"` | `<span asp-validation-for="ValidUntil" class="text-danger small">` | Field-level error per single input (P06) |

### Shared 2: Async DB Lookup Pattern

**Source:** `Controllers/TrainingAdminController.cs:243-244` (existing)

**Apply to:** P03 validator (both Add + Edit)

```csharp
var src = await _context.TrainingRecords.FindAsync(id);    // single-record lookup by PK
var srcAs = await _context.AssessmentSessions.FindAsync(id);
```

Use `FindAsync(id)` untuk PK lookup (faster + cached), bukan `FirstOrDefaultAsync(predicate)`. Reserve `FirstOrDefaultAsync` untuk non-PK predicate (e.g., GetTraining Edit lookup yang sudah pakai pattern ini di L278-285).

### Shared 3: Razor Conditional Section Render

**Source:** `Views/Admin/EditTraining.cshtml:165-176` (existing pattern `@if (!string.IsNullOrEmpty(Model.ExistingSertifikatUrl)) {...}`)

**Apply to:** EditTraining renewal source section (Pattern A)

```razor
@if (Model.RenewsTrainingId.HasValue || Model.RenewsSessionId.HasValue)
{
    <!-- conditional markup -->
}
```

Parity dengan existing pattern null-guard guard di Razor — view tidak crash kalau VM field null.

### Shared 4: Error Message String Verbatim Lock

**Source:** CONTEXT D-03 + spec line 203/217/234

**Apply to:** SEMUA `ModelState.AddModelError` call Phase 326

| Message | Key |
|---------|-----|
| `"Tanggal renewal harus lebih besar dari tanggal sertifikat yang di-renew."` | `""` |
| `"Sertifikat tidak boleh renewal dirinya sendiri."` | `""` |
| `"Sertifikat Permanent tidak boleh punya tanggal expired."` | `"ValidUntil"` |

Byte-for-byte match. Checker akan grep verbatim di kode vs CONTEXT/UI-SPEC.

---

## No Analog Found

Tidak ada file Phase 326 yang tanpa analog. Semua 4 file modify punya exact / role-match analog di codebase.

---

## Metadata

**Analog search scope:** `Controllers/TrainingAdmin*`, `Models/*TrainingRecord*`, `Models/AssessmentSession.cs`, `Views/Admin/*Training*.cshtml`, `Views/Admin/*Assessment*.cshtml` (cross-reference ValidUntil span existence)

**Files scanned:** 8 file (4 file modify + 4 file reference: CreateTrainingRecordViewModel, AssessmentSession, AddManualAssessment.cshtml, EditManualAssessment.cshtml)

**Greps executed:** 2 (`asp-validation-for="ValidUntil"` semua Views/Admin → 0 hit, `asp-for="ValidUntil"` → 12 hit across 6 file)

**Critical findings:**
1. **D-08 resolved:** `AssessmentSession.Schedule` (DateTime non-null, L18) → setara dengan `TrainingRecord.Tanggal` untuk DAG check. Alternative defensive: `CompletedAt ?? Schedule`.
2. **ValidUntil span baru:** Phase 326 tambah pertama kali `<span asp-validation-for="ValidUntil">` di kedua AddTraining (L198+) + EditTraining (L153+). Gap di-noted Phase 327+ kalau ingin extend ke ManualAssessment views.
3. **Edit POST mapping gap:** Existing L479-490 tidak persist `RenewsTrainingId`/`RenewsSessionId` → harus ditambah else "Hapus link renewal" button user click → submit → silent ignore. Plan-phase WAJIB include task ini eksplisit.

**Pattern extraction date:** 2026-05-27
