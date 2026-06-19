# Phase 392: Perbaikan CreateWorker + Audit Field - Research

**Researched:** 2026-06-17
**Domain:** ASP.NET Core MVC Razor view fix (`Views/Admin/CreateWorker.cshtml`) — buka kunci field AD-mode + validasi inline per-field + aktivasi validasi client-side + verifikasi Playwright end-to-end. VIEW-ONLY, 0 migration, 0-diff controller/model.
**Confidence:** HIGH (semua klaim diverifikasi langsung terhadap file live di codebase pada sesi ini)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions (D-01 .. D-08)
- **D-01:** Hapus `readonly="@(isAdMode ? "readonly" : null)"` **unconditional** DAN `bg-light` ternary dari input FullName & Email → editable di SEMUA mode. AD auth tetap aktif. (Bug utama F-VIEW-07 HIGH.)
- **D-02:** Reword teks info `@if(isAdMode)` pada FullName & Email dari "Dikelola oleh AD — akan disinkronkan saat login" (kini kontradiktif krn field editable) menjadi pengingat akurat: editable sekarang + AD overwrite Nama/Email saat login pertama + ingatkan cocokkan akun AD. Wording final = Claude's Discretion (proposal exact di bawah).
- **D-03:** Tambah `type="email"` eksplisit pada input Email. Model pakai `[EmailAddress]` (BUKAN `[DataType(DataType.EmailAddress)]`) → `asp-for` TIDAK auto-render `type=email`. Verifikasi tak ada atribut `type` dobel.
- **D-04:** Tambah `<span asp-validation-for="X" class="text-danger small">` **HANYA** ke Position/Directorate/Section(select)/Unit(select). **JANGAN** duplikasi span existing (FullName/Email/NIP/JoinDate/Password/ConfirmPassword). Field org tetap OPTIONAL (jangan tambah `required`). Span Role = opsional/diskresi.
- **D-05:** Aktifkan validasi client-side (PURE VIEW): bungkus blok `<script>` bawah (shared-cascade.js + `initSectionUnitCascade` + `initFormLoading`) ke dalam `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") <scripts dipindah> }` agar jQuery (footer `_Layout`) + jquery-validation load sebelum unobtrusive validation + cascade init. CAUTION: partial WAJIB di dalam `@section Scripts`, BUKAN inline di body.
- **D-06:** Verifikasi = Playwright e2e mode lokal AD-OFF (`Authentication__UseActiveDirectory=false`, `--workers=1`) buktikan field editable + cascade Section→Unit runtime + create submission sukses (record tersimpan + redirect ManageWorkers + TempData Success). PLUS guard STATIK source-grep bahwa `.cshtml` hasil-fix TAK punya `readonly=` & `bg-light` ternary di input FullName/Email (AD-off runtime tak bisa exercise bug readonly-mode-AD → source-grep buktikan penghapusan unconditional by construction).
- **D-07:** Cleanup test = email unik per-run (mis. `e2e-cw-{timestamp}@local.test`) + teardown via jalur DeleteWorker POST (Identity cascade hapus AspNetUserRoles), teardown jalan walau test gagal + 1 entri SEED_JOURNAL.
- **D-08:** Scope = `CreateWorker.cshtml` SAJA. `WorkerController.cs` + `ManageUserViewModel.cs` UNCHANGED (git 0-diff verify). `EditWorker.cshtml` TIDAK disentuh.

### Claude's Discretion
- Wording final teks info AD (D-02) — rekonsiliasi editable + diselaraskan-AD-saat-login + ingatkan-cocokkan-akun-AD. **(Proposal exact disediakan di §Code Examples.)**
- Apakah tambah span Role (D-04) — error unreachable; boleh skip atau tambah demi konsistensi. **(Rekomendasi: tambah demi konsistensi visual; risiko nol.)**
- Format persis email-unik + mekanisme teardown (D-07). **(Proposal disediakan di §Validation Architecture.)**
- Apakah catatan pengingat AD ditulis terpisah atau nyatu di teks info reword (D-02 sudah menutup ini → nyatu).

### Deferred Ideas (OUT OF SCOPE)
- AD provisioning / membuat login AD benar-benar jalan untuk akun baru — kerja auth/provisioning terpisah.
- `EditWorker.cshtml` punya pola readonly+bg-light identik (L68-80, **VERIFIED**) — sengaja TIDAK diperbaiki (divergensi sadar; kunci AD di Edit defensible).
- `shared-cascade.js` placeholder "-- Pilih Unit --" hard-coded (i18n drift vs `@OrgLabels.GetLabel(1)`) — file JS shared, bukan view-only phase ini.
- Email↔AD-username mapping risk — laten, butuh kerja auth (OUT of scope).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| WRKR-01 | HC/Admin dapat **mengetik** Nama Lengkap & Email di semua environment (termasuk `UseActiveDirectory=true`) — field tak lagi `readonly`, AD auth tetap aktif | D-01 edit (hapus `readonly` + `bg-light` L62-64/73-75) + D-02 reword teks info; verifikasi via source-grep (D-06) krn AD-off runtime tak bisa exercise readonly-mode-AD |
| WRKR-02 | Email validasi format (`type="email"`) + setiap field tampil **pesan validasi inline per-field** (Nama Lengkap, Email, Jabatan, Directorate, Bagian, Unit) — bukan cuma ringkasan atas | D-03 (`type="email"` Email L73) + D-04 (4 span org baru) + D-05 (aktifkan client-side agar inline live, bukan hanya pasca-POST) |
| WRKR-03 | SEMUA field berfungsi end-to-end terverifikasi runtime + submission create pekerja baru SUKSES (record tersimpan, redirect ke daftar) | D-05 (cascade Section→Unit ter-load benar) + D-06 Playwright runtime (editable + cascade + create→redirect ManageWorkers + TempData Success) + D-07 cleanup |
</phase_requirements>

## Summary

Phase 392 adalah perbaikan view murni satu file (`Views/Admin/CreateWorker.cshtml`) yang menutup 5 temuan audit: (1) field Nama/Email read-only di mode AD sehingga form tak terpakai (F-VIEW-07 HIGH), (2) teks info AD menyesatkan pasca-unlock, (3) Email tanpa `type="email"`, (4) 4 field organisasi tanpa span validasi inline (F-NEW-01), dan (5) halaman tidak memuat validator JS sama sekali sehingga pesan inline hanya muncul setelah POST gagal (F-NEW-03 HIGH). Semua perbaikan adalah markup/Razor — controller (`WorkerController.CreateWorker`) dan model (`ManageUserViewModel`) sudah benar dan **harus** tetap 0-diff.

Semua line anchor di CONTEXT.md sudah **diverifikasi terhadap file live** pada sesi ini dan **akurat** (lihat tabel anchor di bawah). Pola reuse untuk validasi client-side (`@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") ... }`) terbukti persis di `Views/Account/Settings.cshtml:137-146`, dan urutan load aman karena `_Layout.cshtml` memuat jQuery di L241-243 lalu `@RenderSectionAsync("Scripts")` di L267 (jQuery → partial → cascade init). Lib jquery-validation ada di `wwwroot/lib/`. `initSectionUnitCascade` di `shared-cascade.js` murni vanilla DOM (tidak butuh jQuery), jadi aman dipindah ke dalam `@section Scripts`.

Verifikasi WAJIB Playwright runtime (lesson Phase 354: Razor + cascade JS dinamis tak terbukti oleh grep+build) di mode lokal AD-OFF — satu-satunya mode login lokal yang jalan — PLUS guard statik source-grep untuk membuktikan penghapusan `readonly`/`bg-light` unconditional (AD-off runtime tak bisa menguji bug readonly-mode-AD). Cleanup test pakai email unik per-run + teardown via jalur DeleteWorker POST (Identity cascade hapus AspNetUserRoles — **VERIFIED** `_userManager.DeleteAsync` L656) yang jalan di `afterAll`/`finally` walau test gagal, + 1 entri `docs/SEED_JOURNAL.md`.

**Primary recommendation:** Edit 5 cluster markup di `CreateWorker.cshtml` (hapus readonly+bg-light FullName/Email; reword 2 teks info AD; `type="email"` Email; 4 span org; pindah blok script bawah ke `@section Scripts` + tambah `_ValidationScriptsPartial`), lalu kunci dengan 1 spec Playwright AD-off (`--workers=1`) self-cleaning + 1 source-grep guard, 0-diff controller/model, 0 migration.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Buka kunci field (editable) | Frontend Server (Razor view) | — | `readonly`/`bg-light` adalah atribut markup yang dirender server-side via ternary `isAdMode` di view; tidak ada logika controller/model yang terlibat |
| Validasi format email (`type=email`) | Browser (HTML5 native) | Frontend Server (markup) | `type="email"` = constraint validation HTML5 di browser; view hanya emit atribut |
| Validasi inline per-field (live) | Browser (jquery-validation-unobtrusive) | Frontend Server (data-val-* dari TagHelper) | TagHelper emit `data-val-*` attribute server-side; jquery-validation-unobtrusive baca + enforce di browser sebelum submit |
| Validasi server-authoritative (Required/Email/Compare/Section/Unit) | API/Backend (WorkerController + ModelState) | — | **FROZEN** — sudah ada (L216-254). Span inline = surface untuk pesan server ini juga |
| Cascade Bagian→Unit | Browser (`shared-cascade.js` vanilla JS) | API/Backend (`ViewBag.SectionUnitsJson` seed data) | Controller serialize dict (L204/244); JS bangun option Unit dari pilihan Section di browser |
| Persist pekerja baru + redirect | API/Backend (WorkerController POST) | — | **FROZEN** — `_userManager.CreateAsync` + `AddToRoleAsync` + `RedirectToAction("ManageWorkers")` + `TempData["Success"]` (L279-300) |
| Teardown test worker (cascade roles) | API/Backend (Identity `DeleteAsync`) | Test harness (Playwright POST) | **FROZEN** — `_userManager.DeleteAsync` L656 cascade AspNetUserRoles; test trigger via DeleteWorker form POST |

## Verified Line Anchors (file live, 2026-06-17)

> CONTEXT.md anchor adalah hipotesis. Berikut **hasil verifikasi langsung** terhadap `Views/Admin/CreateWorker.cshtml` (206 baris). Semua akurat — gunakan ini sebagai sumber kebenaran.

| Target edit | CONTEXT anchor | VERIFIED anchor | Catatan |
|-------------|----------------|-----------------|---------|
| `isAdMode` declaration | L6 | **L6** ✓ | `var isAdMode = Config.GetValue<bool>("Authentication:UseActiveDirectory", false);` |
| `<div asp-validation-summary>` existing | L34-46 | **L34-46** ✓ | Bukan `asp-validation-summary` TagHelper; ini `@if (!ViewData.ModelState.IsValid)` manual `<ul>` loop. Tetap surface semua error. JANGAN ubah |
| `<form id="createWorkerForm">` | — | **L48** | `asp-action="CreateWorker" asp-controller="Worker"` |
| Input FullName (readonly+bg-light) | ~L62-64 | **L62-64** ✓ | `class="@(isAdMode ? "bg-light" : "")"` L62; `readonly="@(isAdMode ? "readonly" : null)"` L64 |
| Teks info AD FullName | ~L65-68 | **L65-68** ✓ | `@if (isAdMode) { <div class="form-text text-info">...Dikelola oleh AD...</div> }` |
| Span FullName existing | L69 | **L69** ✓ | JANGAN duplikasi |
| Input Email (readonly+bg-light) | ~L73-75 | **L73-75** ✓ | `class` L73; `readonly` L75. **TIDAK ada `type` attribute** → asp-for render `type="text"` (D-03 valid) |
| Teks info AD Email | ~L76-79 | **L76-79** ✓ | Sama "Dikelola oleh AD..." |
| Span Email existing | L80 | **L80** ✓ | JANGAN duplikasi |
| Span NIP existing | L85 | **L85** ✓ | JANGAN duplikasi |
| Span JoinDate existing | L90 | **L90** ✓ | JANGAN duplikasi |
| Input Position (NO span) | ~L106-108 | **L106-107** ✓ | `<input asp-for="Position" ...>` L107; tambah span SETELAH L107 |
| Input Directorate (NO span) | ~L110-112 | **L110-111** ✓ | `<input asp-for="Directorate" ...>` L111; tambah span SETELAH L111 |
| Select Section (NO span) | ~L113-117 | **L114-117** ✓ | `<select asp-for="Section" id="sectionSelect">...</select>`; tambah span SETELAH `</select>` L117 |
| Select Unit (NO span) | ~L119-123 | **L120-123** ✓ | `<select asp-for="Unit" id="unitSelect">...</select>`; tambah span SETELAH `</select>` L123 |
| Select Role (span opsional) | — | **L139-147** | `<select asp-for="Role">` L140; span opsional SETELAH L146 `<div class="form-text">` |
| Span Password existing | L158 | **L158** ✓ | dalam `@if (!isAdMode)` block; JANGAN duplikasi |
| Span ConfirmPassword existing | L168 | **L168** ✓ | dalam `@if (!isAdMode)` block; JANGAN duplikasi |
| Blok script bawah (D-05 pindah) | ~L194-205 | **L194-205** ✓ | L194 `<script src="~/js/shared-cascade.js">`, L195 `shared-loading.js`, L196-205 inline `initSectionUnitCascade(...)` + `initFormLoading(...)` |

**Penting:** Span `asp-validation-for` yang SUDAH ADA = **6** (FullName L69, Email L80, NIP L85, JoinDate L90, Password L158, ConfirmPassword L168). Yang KURANG = **4** (Position, Directorate, Section, Unit) + Role (opsional). Executor WAJIB grep `asp-validation-for` sebelum insert untuk hindari duplikat (F-NEW-01 HIGH).

## Standard Stack

> Phase view-only — tidak ada paket baru. Semua dependency sudah ter-vendor di repo. Tabel ini mendokumentasi yang DIPAKAI (bukan diinstal).

### Core (sudah ter-vendor, VERIFIED)
| Library | Lokasi | Purpose | Status |
|---------|--------|---------|--------|
| jQuery 3.7.1 | `_Layout.cshtml` L241-243 (CDN) | Prasyarat jquery-validation | **VERIFIED ada** — load di footer sebelum `@RenderSectionAsync("Scripts")` L267 |
| jquery-validation | `wwwroot/lib/jquery-validation/dist/jquery.validate.min.js` | Client-side validation engine | **VERIFIED ada** (referenced `_ValidationScriptsPartial.cshtml` L1) |
| jquery-validation-unobtrusive | `wwwroot/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js` | Baca `data-val-*` → wire validation | **VERIFIED ada** (referenced `_ValidationScriptsPartial.cshtml` L2) |
| `shared-cascade.js` | `wwwroot/js/shared-cascade.js` | `initSectionUnitCascade` + `togglePassword` (vanilla DOM, no jQuery dep) | **VERIFIED** — `document.getElementById`/`addEventListener`, tak ada `$` |
| `shared-loading.js` | `wwwroot/js/shared-loading.js` | `initFormLoading` (form submit spinner) | Sudah dipakai L195/L204 |
| Microsoft.AspNetCore.Mvc.TagHelpers | `_ViewImports.cshtml` `@addTagHelper *` | `asp-for`/`asp-validation-for` → emit `data-val-*` | **VERIFIED** registered |

**Tidak ada `npm install` / `dotnet add package`.** Pure view edit.

**Version verification:** N/A — tidak ada paket NuGet/npm baru. Semua aset sudah di `wwwroot/lib/` + CDN di `_Layout`.

## Architecture Patterns

### System Architecture Diagram (flow halaman CreateWorker)

```
[HC/Admin browser]
       │  GET /Admin/CreateWorker  (Route: Admin/[action] → WorkerController.CreateWorker GET L197)
       ▼
[WorkerController.CreateWorker GET]
       │  model = new ManageUserViewModel { Role="Coachee" }
       │  ViewBag.SectionUnitsJson = serialize(GetSectionUnitsDictAsync)   ← seed cascade data (L204)
       ▼
[Razor render CreateWorker.cshtml]   ← isAdMode = Config("Authentication:UseActiveDirectory")
       │  - input FullName/Email  (SETELAH FIX: editable, type=email pd Email)
       │  - data-val-* attrs dari TagHelper  (Required FullName/Email/Role, Email format, Password minlen, Compare)
       │  - @section Scripts: _ValidationScriptsPartial + initSectionUnitCascade + initFormLoading  (SETELAH FIX)
       ▼
[Browser DOM + jQuery footer L241 + jquery-validation (section) ]
       │  initSectionUnitCascade(SectionUnitsJson)  → Section change → rebuild Unit options
       │  jquery-validation-unobtrusive  → inline span terisi LIVE saat blur/submit
       │  user isi field → klik Simpan
       ▼  POST /Admin/CreateWorker  (anti-forgery)
[WorkerController.CreateWorker POST L212]
       │  ┌─ !useAD && Password kosong → ModelState["Password"] error (L218)
       │  ├─ Section tak di OrganizationUnits → ModelState["Section"] (L227)
       │  ├─ Unit tak valid utk Section → ModelState["Unit"] (L234)
       │  ├─ !ModelState.IsValid → re-render View (+ re-seed SectionUnitsJson L244)   ──┐ (span inline surface error)
       │  ├─ Email sudah terdaftar → ModelState["Email"] + re-render (L252)              │
       │  └─ Success: CreateAsync + AddToRoleAsync + audit + TempData["Success"]         │
       ▼                                                                                 │
[RedirectToAction("ManageWorkers")]  ← TempData Success dirender di _Layout L225  ◄──────┘ (gagal balik ke view ini)
```

### Pattern 1: Aktivasi client-side validation via `@section Scripts` + `_ValidationScriptsPartial`
**What:** ASP.NET Core unobtrusive validation butuh `jquery.validate` + `jquery.validate.unobtrusive` ter-load SETELAH jQuery. Pola standar = partial `_ValidationScriptsPartial` di dalam `@section Scripts` (di-render di `_Layout` L267, SETELAH jQuery L241).
**When to use:** Setiap halaman form yang mau validasi inline live (sebelum submit), bukan hanya pasca-POST.
**Preseden persis (VERIFIED):** `Views/Account/Settings.cshtml:137-146` (lihat §Code Examples).
**CAUTION (F-NEW-10):** Partial WAJIB di dalam `@section Scripts`, BUKAN inline di body — kalau inline di body, ia load sebelum jQuery footer → `$ is not defined`.

### Pattern 2: Cascade Section→Unit vanilla JS dipindah ke `@section Scripts`
**What:** `initSectionUnitCascade` (`shared-cascade.js`) murni `document.*`/`addEventListener` — TIDAK bergantung jQuery. Memindahkannya dari body inline ke `@section Scripts` aman (tetap jalan setelah DOM siap karena section dirender di akhir body, sebelum `</body>`).
**Catatan:** `currentSection`/`currentUnit` di-inject via Razor interpolation (`@(Model.Section ?? "")`) — pertahankan saat dipindah agar re-render pasca-POST tetap restore pilihan.

### Recommended edit structure (5 cluster, 1 file)
```
Views/Admin/CreateWorker.cshtml
├── Cluster A (L62-79): FullName + Email     → hapus readonly+bg-light (D-01) + reword 2 teks info AD (D-02) + type=email Email (D-03)
├── Cluster B (L107):   Position             → +span asp-validation-for (D-04)
├── Cluster C (L111):   Directorate          → +span asp-validation-for (D-04)
├── Cluster D (L117):   Section </select>    → +span asp-validation-for (D-04)
├── Cluster E (L123):   Unit </select>       → +span asp-validation-for (D-04)
├── Cluster F (L146):   Role (opsional)      → +span asp-validation-for (D-04 diskresi)
└── Cluster G (L194-205): blok script bawah  → bungkus ke @section Scripts + _ValidationScriptsPartial (D-05)
```

### Anti-Patterns to Avoid
- **Duplikasi span (F-NEW-01 HIGH):** menambah `<span asp-validation-for>` ke field yang SUDAH punya (FullName/Email/NIP/JoinDate/Password/ConfirmPassword) → 2 pesan error tampil. Grep dulu.
- **Partial inline di body (F-NEW-10):** `@await Html.PartialAsync("_ValidationScriptsPartial")` di tengah body, bukan dalam `@section Scripts` → load sebelum jQuery → runtime error.
- **Menambah `required` ke field org:** mengubah semantik model (Position/Directorate/Section/Unit OPTIONAL). Span hanya surface pesan SERVER (Section/Unit validity); JANGAN tambah `required` attribute.
- **Menyentuh controller/model untuk "memperbaiki" validasi:** semua client-side bisa diaktifkan murni view. Edit controller/model = melanggar D-08 (0-diff wajib).
- **Mengubah `EditWorker.cshtml`:** punya pola identik (L68-80) tapi OUT of scope (divergensi sadar).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Client-side validation engine | Custom JS `onblur` regex per-field | `_ValidationScriptsPartial` (jquery-validation-unobtrusive) | TagHelper sudah emit `data-val-*`; unobtrusive auto-wire semua aturan model (Required/Email/Compare/StringLength) tanpa kode |
| Validasi format email | Regex email manual di JS | `type="email"` (HTML5 native) + `[EmailAddress]` server | Browser native constraint + ModelState server = double-guard, 0 baris JS |
| Cascade Section→Unit | DOM manipulation baru | `initSectionUnitCascade` (`shared-cascade.js`, sudah benar) | Sudah teruji (F-VIEW-10), shared lintas view; jangan rewrite |
| Teardown test (hapus user + roles) | Raw SQL `DELETE FROM AspNetUsers` | POST `/Admin/DeleteWorker` (`_userManager.DeleteAsync`) | Identity cascade AspNetUserRoles otomatis (L656). Raw-SQL skip cascade → orphan rows (F-NEW-07) |
| DB snapshot/restore test | Custom backup script | `tests/helpers/dbSnapshot.ts` (jika perlu) ATAU email-unik self-clean (D-07 pilih ini) | D-07 pilih email-unik+hapus (lebih ringan; cukup utk 1 baris transient) |

**Key insight:** Seluruh phase ini adalah "wire-up aset yang sudah ada", bukan membangun apa pun. Validator JS, cascade JS, Identity cascade-delete — semua sudah ada; phase hanya menghubungkannya di markup.

## Runtime State Inventory

> Phase ini BUKAN rename/refactor/migration — ia menambah/menghapus markup di 1 view. Namun karena melibatkan create user test, satu kategori relevan (data test transient).

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | Test worker baru di `AspNetUsers` + `AspNetUserRoles` (1 baris transient per run, email `e2e-cw-{ts}@local.test`) | Teardown via DeleteWorker POST di `afterAll`/`finally` (Identity cascade) — **self-cleaning** |
| Live service config | None — tidak ada n8n/Datadog/external config menyentuh CreateWorker view | None |
| OS-registered state | None — view edit, tak ada Task Scheduler/pm2/systemd | None |
| Secrets/env vars | `Authentication__UseActiveDirectory=false` (env override saat `dotnet run` lokal; appsettings.json default `true` — **VERIFIED** L14). Tidak diubah, hanya dipakai utk test | None (env var test-time, bukan kode) |
| Build artifacts | None — Razor view di-compile runtime; tak ada egg-info/binary stale | None |

**Catatan kritis:** `appsettings.json` punya `UseActiveDirectory: true` (default Dev/Prod). Test lokal HARUS jalankan app dengan `Authentication__UseActiveDirectory=false` (env override) agar login lokal (email+password) jalan — lihat [[reference_local_e2e_sql_env_fix]]. Di mode AD-off, `isAdMode=false` → `readonly`/`bg-light` ternary memang sudah tidak aktif (`null`/`""`), itulah sebabnya D-06 menambah **source-grep guard** untuk membuktikan penghapusan unconditional yang menjamin editable-saat-AD by construction.

## Common Pitfalls

### Pitfall 1: AD-off runtime tidak bisa exercise bug readonly-mode-AD (F-NEW-04 MED)
**What goes wrong:** Test Playwright jalan di AD-off (`isAdMode=false`). Di mode itu, `readonly="@(isAdMode ? "readonly" : null)"` → `null` → atribut tak dirender, jadi field SUDAH editable bahkan SEBELUM fix. Assertion runtime "field editable" lolos hampa — tak membuktikan apa pun tentang mode AD.
**Why it happens:** Bug hanya manifes saat `isAdMode=true`, tapi mode itu butuh server AD Pertamina (tak ada lokal).
**How to avoid:** Tambah **guard statik source-grep** (D-06): assert file `CreateWorker.cshtml` hasil-fix tak lagi mengandung `readonly=` dan `bg-light` ternary di input FullName/Email. Penghapusan unconditional = editable-saat-AD terjamin by construction. (Runtime assertion tetap berguna untuk membuktikan field bisa diketik + submit jalan; ia hanya tak cukup sendiri.)
**Warning signs:** Plan yang hanya punya assertion `await input.fill(...)` tanpa source-grep guard → tidak membuktikan WRKR-01 untuk mode AD.

### Pitfall 2: Razor + cascade JS dinamis tak terbukti oleh grep+build (lesson Phase 354)
**What goes wrong:** Build hijau + grep markup benar TIDAK menjamin cascade Section→Unit benar-benar membangun option Unit di runtime, atau bahwa `@section Scripts` benar-benar load (urutan jQuery).
**Why it happens:** Render Razor dinamis + eksekusi JS hanya terjadi di browser; compiler tak cek wiring runtime.
**How to avoid:** Playwright runtime WAJIB — pilih Section, assert option Unit muncul (count > 1 / opsi spesifik), lalu submit + assert redirect. (D-06.)
**Warning signs:** "verifikasi" yang hanya `dotnet build` + grep tanpa Playwright.

### Pitfall 3: `type="email"` dobel atau tak bermakna (D-03)
**What goes wrong:** Khawatir `asp-for="Email"` sudah render `type="email"` (dari `[EmailAddress]`), lalu menambah eksplisit → dobel.
**Why it happens:** Kebingungan `[EmailAddress]` (validation attribute) vs `[DataType(DataType.EmailAddress)]` (memengaruhi `type`).
**Reality (VERIFIED):** Model pakai `[EmailAddress]` SAJA (`ManageUserViewModel.cs` L21), TIDAK ada `[DataType(DataType.EmailAddress)]` di mana pun (grep kosong). InputTagHelper ASP.NET Core hanya emit `type="email"` jika ada `[DataType(DataType.EmailAddress)]`; `[EmailAddress]` (sebuah `ValidationAttribute`, bukan `DataTypeAttribute`) TIDAK memengaruhi `type`. Input Email L73-75 saat ini **tak punya atribut `type`** → dirender `type="text"`. Menambah `type="email"` eksplisit = bermakna; TagHelper TIDAK akan emit `type` kedua karena atribut eksplisit menang (no duplicate). [VERIFIED: codebase grep + Read ManageUserViewModel.cs + _ViewImports.cshtml]
**How to avoid:** Tambah `type="email"` eksplisit; verifikasi rendered HTML (Playwright `getAttribute('type')` === 'email') tak dobel.

### Pitfall 4: Password client-side tak men-trigger di mode AD-off (semantik model)
**What goes wrong:** Berharap `ConfirmPassword` compare / `Password` required muncul live, tapi `Password` di model TIDAK `[Required]` (hanya `[StringLength(MinimumLength=6)]`); required-nya server-side (`ModelState.AddModelError("Password", ...)` L218, hanya saat `!useAD`).
**Why it happens (VERIFIED):** `ManageUserViewModel.cs` L54-60: `Password` = `[StringLength(100, MinimumLength=6)]` (BUKAN Required); `ConfirmPassword` = `[Compare("Password")]` (BUKAN Required). Jadi client-side hanya enforce: panjang ≥6 (jika diisi) + compare cocok. "Password wajib" datang dari controller saat submit kosong.
**How to avoid:** JANGAN tambah `required` ke Password di view (melanggar semantik + bukan scope). Span Password/ConfirmPassword sudah ada (L158/168) dan akan surface pesan compare/length live + pesan server "harus diisi". Dokumentasikan ke planner agar tak salah-harap.
**Warning signs:** Plan yang berasumsi span Password muncul live untuk "kosong" — itu pesan server, bukan client.

### Pitfall 5: Teardown tak jalan saat test gagal → user residu (D-07)
**What goes wrong:** Test gagal di tengah → DeleteWorker tak terpanggil → user `e2e-cw-*` nempel di DB (melanggar CLAUDE.md Seed Workflow).
**How to avoid:** Teardown di `test.afterAll` / `finally` (bukan di akhir test body). Email unik per-run (timestamp) agar re-run tak bentrok "Email sudah terdaftar". Catat 1 entri `docs/SEED_JOURNAL.md` (active → cleaned).
**Warning signs:** cleanup di akhir `test(...)` body tanpa `afterAll`/`finally`.

## Code Examples

> Semua excerpt CURRENT diverifikasi dari file live. Excerpt PROPOSED = rekomendasi siap-pakai (planner/executor sesuaikan minor).

### D-01 + D-02 + D-03: FullName & Email (Cluster A, L61-80)

**CURRENT (VERIFIED L61-80):**
```html
<label asp-for="FullName" class="form-label fw-semibold"></label>
<input asp-for="FullName" class="form-control @(isAdMode ? "bg-light" : "")"
       placeholder="Masukkan nama lengkap"
       readonly="@(isAdMode ? "readonly" : null)" />
@if (isAdMode)
{
    <div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>Dikelola oleh AD — akan disinkronkan saat login</div>
}
<span asp-validation-for="FullName" class="text-danger small"></span>
...
<label asp-for="Email" class="form-label fw-semibold"></label>
<input asp-for="Email" class="form-control @(isAdMode ? "bg-light" : "")"
       placeholder="contoh@pertamina.com"
       readonly="@(isAdMode ? "readonly" : null)" />
@if (isAdMode)
{
    <div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>Dikelola oleh AD — akan disinkronkan saat login</div>
}
<span asp-validation-for="Email" class="text-danger small"></span>
```

**PROPOSED:**
```html
<label asp-for="FullName" class="form-label fw-semibold"></label>
<input asp-for="FullName" class="form-control"
       placeholder="Masukkan nama lengkap" />
@if (isAdMode)
{
    <div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>Isi sesuai akun AD Pertamina pekerja. Nama &amp; Email akan diselaraskan otomatis dari Active Directory saat pekerja login pertama kali.</div>
}
<span asp-validation-for="FullName" class="text-danger small"></span>
...
<label asp-for="Email" class="form-label fw-semibold"></label>
<input asp-for="Email" type="email" class="form-control"
       placeholder="contoh@pertamina.com" />
@if (isAdMode)
{
    <div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>Isi sesuai akun AD Pertamina pekerja. Nama &amp; Email akan diselaraskan otomatis dari Active Directory saat pekerja login pertama kali.</div>
}
<span asp-validation-for="Email" class="text-danger small"></span>
```

**Proposed reworded Bahasa Indonesia info-text (D-02, final — Claude's Discretion):**
> **"Isi sesuai akun AD Pertamina pekerja. Nama & Email akan diselaraskan otomatis dari Active Directory saat pekerja login pertama kali."**

Rasional: (a) "Isi sesuai akun AD Pertamina pekerja" → mengakui field editable + mengingatkan mencocokkan akun AD (konsekuensi keputusan user: akun selalu utk pekerja pre-exist di AD); (b) "akan diselaraskan otomatis dari Active Directory saat login pertama" → jujur soal overwrite FullName/Email (`AccountController.cs:86-116`). Menggantikan teks lama yang kini kontradiktif (menyiratkan field tak bisa diisi). Teks ini sama untuk FullName & Email.

### D-04: 4 span org (Cluster B-E) + Role opsional (Cluster F)

**Position (sisip SETELAH L107 `<input asp-for="Position" ...>`):**
```html
<span asp-validation-for="Position" class="text-danger small"></span>
```
**Directorate (SETELAH L111):**
```html
<span asp-validation-for="Directorate" class="text-danger small"></span>
```
**Section (SETELAH `</select>` L117):**
```html
<span asp-validation-for="Section" class="text-danger small"></span>
```
**Unit (SETELAH `</select>` L123):**
```html
<span asp-validation-for="Unit" class="text-danger small"></span>
```
**Role (opsional, SETELAH `<div class="form-text">...</div>` L146 — diskresi, direkomendasikan):**
```html
<span asp-validation-for="Role" class="text-danger small"></span>
```
> Span Section/Unit = jaring pengaman pesan server ("Bagian tidak ditemukan" L227 / "Unit tidak valid" L234). Position/Directorate tak punya aturan validasi aktif (StringLength 100 saja) → span jarang terisi, tapi konsisten + harmless. JANGAN tambah `required`.

### D-05: pindah blok script bawah ke `@section Scripts` (Cluster G, L194-205)

**CURRENT (VERIFIED L194-205, inline di body):**
```html
<script src="~/js/shared-cascade.js"></script>
<script src="~/js/shared-loading.js"></script>
<script>
    initSectionUnitCascade({
        sectionUnits: @Html.Raw(ViewBag.SectionUnitsJson ?? "{}"),
        sectionId: 'sectionSelect',
        unitId: 'unitSelect',
        currentSection: "@(Model.Section ?? "")",
        currentUnit: "@(Model.Unit ?? "")"
    });
    initFormLoading('createWorkerForm', 'Menyimpan...');
</script>
```

**PROPOSED (hapus blok inline L194-205, ganti dengan `@section Scripts` di akhir file SETELAH `</div>` penutup container L192):**
```cshtml
@section Scripts {
    @await Html.PartialAsync("_ValidationScriptsPartial")
    <script src="~/js/shared-cascade.js"></script>
    <script src="~/js/shared-loading.js"></script>
    <script>
        initSectionUnitCascade({
            sectionUnits: @Html.Raw(ViewBag.SectionUnitsJson ?? "{}"),
            sectionId: 'sectionSelect',
            unitId: 'unitSelect',
            currentSection: "@(Model.Section ?? "")",
            currentUnit: "@(Model.Unit ?? "")"
        });
        initFormLoading('createWorkerForm', 'Menyimpan...');
    </script>
}
```
> Urutan terjamin: `_Layout` load jQuery L241 → `@RenderSectionAsync("Scripts")` L267 render section ini → `_ValidationScriptsPartial` (jquery.validate + unobtrusive) → shared-cascade/loading → init. Semua setelah jQuery. `@section Scripts` boleh diletakkan di mana saja di file (Razor angkat ke render slot); konvensi taruh di akhir file.

### Preseden D-05: `Views/Account/Settings.cshtml:137-146` (VERIFIED)
```cshtml
@section Scripts {
    @await Html.PartialAsync("_ValidationScriptsPartial")
    <script>
        $(document).ready(function() {
            setTimeout(function() {
                $('.alert').fadeOut('slow');
            }, 5000);
        });
    </script>
}
```
Sumber: `Views/Account/Settings.cshtml` L137-146 (dibaca sesi ini).

### `_ValidationScriptsPartial.cshtml` (VERIFIED isi penuh)
```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
```
Sumber: `Views/Shared/_ValidationScriptsPartial.cshtml` (2 baris; kedua path **VERIFIED ada** di `wwwroot/lib/`).

### `_Layout.cshtml` ordering proof (VERIFIED)
```html
<!-- L241-243 -->
<script src="https://code.jquery.com/jquery-3.7.1.min.js" integrity="..." crossorigin="anonymous"></script>
...
@await RenderSectionAsync("Scripts", required: false)   <!-- L267 -->
```
jQuery (L241) DULU, lalu Scripts section (L267) → partial validation load setelah jQuery. **D-05 ordering valid.**

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Field AD read-only + bg-light (form tak terpakai) | Field editable unconditional (D-01) | Phase 392 | HC bisa buat akun pekerja di semua env |
| Teks "Dikelola oleh AD — disinkronkan saat login" (kontradiktif pasca-unlock) | "Isi sesuai akun AD... diselaraskan otomatis saat login pertama" (D-02) | Phase 392 | UX jujur + ingatkan match akun AD |
| Email `type="text"` (no HTML5 email constraint) | `type="email"` eksplisit (D-03) | Phase 392 | Validasi format browser native |
| 4 field org tanpa span inline (pesan hanya di summary atas) | span per-field (D-04) | Phase 392 | Pesan validasi kontekstual |
| Tanpa validator JS (inline hanya pasca-POST reload) | `_ValidationScriptsPartial` di `@section Scripts` (D-05) | Phase 392 | Validasi LIVE sebelum submit |

**Deprecated/outdated:** N/A — tidak ada API/library usang dipakai. Pola `@section Scripts { _ValidationScriptsPartial }` adalah pola standar ASP.NET Core MVC saat ini (sama di template `dotnet new mvc`).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `_ValidationScriptsPartial` di `@section Scripts` cukup untuk meng-aktifkan unobtrusive validation tanpa konfigurasi tambahan (mis. `ClientValidationEnabled` di startup) | Pattern 1 / D-05 | LOW — preseden Settings.cshtml bekerja dengan pola identik; default ASP.NET Core ClientValidation = enabled. Playwright runtime akan konfirmasi (span terisi saat blur tanpa submit) |
| A2 | TagHelper tidak emit atribut `type` kedua saat `type="email"` eksplisit diberikan (atribut eksplisit menang, no duplicate) | Pitfall 3 / D-03 | LOW — perilaku InputTagHelper terdokumentasi; Playwright `getAttribute('type')` akan konfirmasi single value 'email' |
| A3 | Test worker fresh (tanpa TrainingRecords/AssessmentSessions) tidak men-trigger guard cross-user-renewal (L538) maupun self-delete (L495) di DeleteWorker → cascade bersih | D-07 / Don't Hand-Roll | LOW — worker baru via CreateWorker tak punya record dependen; admin login ≠ worker test (bukan self-delete) |

**Semua 3 asumsi LOW-risk dan akan terkonfirmasi otomatis oleh Playwright runtime + source-grep di Wave verifikasi.** Tidak ada asumsi HIGH-risk yang butuh konfirmasi user sebelum eksekusi.

## Open Questions

1. **Apakah `@section Scripts` di CreateWorker.cshtml akan bentrok dengan section bawaan?**
   - What we know: View ini saat ini TIDAK punya `@section Scripts` (script-nya inline di body L194-205, VERIFIED). `_Layout` punya `@RenderSectionAsync("Scripts", required:false)`.
   - What's unclear: Tidak ada — tak ada section duplikat.
   - Recommendation: Aman menambah `@section Scripts` baru. `dotnet build` akan error jika ada duplikat section (tidak akan, sudah dicek).

2. **Apakah perlu DB snapshot/restore penuh (seperti spec lain) atau cukup email-unik+hapus?**
   - What we know: D-07 memilih email-unik+hapus (lebih ringan, 1 baris transient self-cleaning). Spec lain (`delete-records-cascade`) pakai BACKUP/RESTORE karena seed kompleks lintas tabel.
   - What's unclear: Tidak ada — D-07 sudah memutuskan.
   - Recommendation: Ikuti D-07 (email-unik + DeleteWorker teardown di `afterAll`). Tidak perlu BACKUP/RESTORE penuh. Catat 1 entri SEED_JOURNAL (active→cleaned). `dbSnapshot.queryString` tetap berguna untuk verifikasi DB ("row tersimpan dengan Email/Role benar") + resolve workerId untuk teardown.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK / `dotnet build`+`run` | Build + run app lokal | ✓ (proyek aktif) | net (HcPortal.csproj) | — |
| jQuery 3.7.1 (CDN) | jquery-validation prasyarat | ✓ | 3.7.1 | — (CDN di `_Layout`) |
| jquery-validation lib | client-side validation | ✓ | `wwwroot/lib/jquery-validation/` | — |
| jquery-validation-unobtrusive | wire data-val-* | ✓ | `wwwroot/lib/jquery-validation-unobtrusive/` | — |
| Playwright + `@playwright/test` | e2e runtime verifikasi | ✓ | `tests/package.json` + `tests/node_modules/` | — |
| SQL Server Express lokal (`localhost\SQLEXPRESS`, `HcPortalDB_Dev`) | login lokal + DB verify + teardown | ✓ | `dbSnapshot.ts` SQLCMD_BASE_ARGS | — |
| `sqlcmd` (untuk `dbSnapshot.queryString`/verify) | DB verify row + resolve workerId teardown | ✓ | dipakai semua spec existing | — |
| SQLBrowser service + lpc shared-memory conn | login 500 fix lokal (NTLM loopback) | ⚠ harus distart manual | — | [[reference_local_e2e_sql_env_fix]] — start SQLBrowser sebelum run |
| Admin akun lokal `admin@pertamina.com`/`123456` | login /Admin/* di Playwright | ✓ | `tests/helpers/accounts.ts` | — |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** SQLBrowser/lpc conn override — bukan "missing", tapi prasyarat run lokal (start service + env). Planner harus cantumkan langkah pre-run: start SQLBrowser, jalankan app dengan `Authentication__UseActiveDirectory=false`, `--workers=1`.

## Validation Architecture

> `nyquist_validation: true` (VERIFIED `.planning/config.json`) → section ini WAJIB ada.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright `@playwright/test` (e2e runtime — lesson Phase 354: Razor + cascade JS dinamis WAJIB runtime, grep+build tak cukup) + source-grep guard (statik) + git-diff guard (0-diff controller/model) + `dotnet build` (compile) |
| Config file | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel:false`, project `setup`→`chromium`) |
| Quick run command | `cd tests && npx playwright test e2e/createworker-392.spec.ts --workers=1` |
| Full suite command | `cd tests && npx playwright test --workers=1` (combined WAJIB `--workers=1` — DB isolation, [[reference_local_e2e_sql_env_fix]]) |
| Unit suite (regresi, no-op untuk view) | `dotnet test --filter Category!=Integration` (pastikan tetap hijau; phase view-only tak menambah xUnit) |

### Pre-run setup (lokal)
1. Start SQLBrowser service + pastikan lpc shared-memory conn ([[reference_local_e2e_sql_env_fix]]).
2. Jalankan app: `Authentication__UseActiveDirectory=false dotnet run` (atau set env var lalu `dotnet run`) → `http://localhost:5277`.
3. Login Playwright pakai `admin@pertamina.com` / `123456` (accounts fixture).

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command / Check | File Exists? |
|--------|----------|-----------|---------------------------|-------------|
| WRKR-01 | Field Nama/Email editable (runtime, AD-off) | e2e (Playwright) | `fill('#FullName', ...)` + `fill('#Email', ...)` sukses (tak readonly) | ❌ Wave 0 (`createworker-392.spec.ts`) |
| WRKR-01 | Penghapusan `readonly`/`bg-light` unconditional (jaminan mode AD by construction) | static source-grep | grep `CreateWorker.cshtml`: NO `readonly=` & NO `bg-light` ternary di input FullName/Email | ❌ Wave 0 (langkah grep dalam spec / task) |
| WRKR-02 | Email `type="email"` (no dobel) | e2e | `await page.locator('#Email').getAttribute('type')` === `'email'` | ❌ Wave 0 |
| WRKR-02 | Span validasi inline live per-field | e2e | submit kosong / blur invalid → `.text-danger` terisi untuk Nama/Email tanpa full reload | ❌ Wave 0 |
| WRKR-03 | Cascade Bagian→Unit runtime | e2e | pilih `#sectionSelect` → assert `#unitSelect option` count > 1 / opsi spesifik | ❌ Wave 0 |
| WRKR-03 | Submission create sukses → record + redirect + TempData | e2e | isi semua field → submit → `waitForURL('**/ManageWorkers')` + assert flash Success + DB row (`queryString` Email/Role) | ❌ Wave 0 |
| D-08 | Controller + model 0-diff | git-diff guard | `git diff --quiet Controllers/WorkerController.cs Models/ManageUserViewModel.cs` (exit 0) | ❌ Wave 0 (langkah verifikasi/task) |
| (build) | Compile hijau | `dotnet build` | `dotnet build HcPortal.csproj` 0 error | ✓ (baseline) |

### Sampling Rate
- **Per task commit:** `dotnet build HcPortal.csproj` (0 error) + source-grep guard (readonly/bg-light absen) + `git diff --quiet` controller/model.
- **Per wave merge:** `cd tests && npx playwright test e2e/createworker-392.spec.ts --workers=1` (editable + type=email + cascade + create→redirect+DB+teardown).
- **Phase gate:** Playwright spec hijau + source-grep guard pass + git-diff 0-diff + `dotnet test --filter Category!=Integration` tetap hijau (no regression) sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `tests/e2e/createworker-392.spec.ts` — spec baru (model dari `assessment-title-flexible.spec.ts` utk login+navigate, `delete-records-cascade.spec.ts` utk afterAll cleanup pola). Covers WRKR-01/02/03.
- [ ] Source-grep guard (langkah dalam spec ATAU task verifikasi) — assert `CreateWorker.cshtml` tak punya `readonly=`/`bg-light` ternary di FullName/Email.
- [ ] git-diff 0-diff guard (langkah task) — `WorkerController.cs` + `ManageUserViewModel.cs` unchanged.
- [ ] 1 entri `docs/SEED_JOURNAL.md` (D-07) — temporary + local-only, active→cleaned.
- Framework install: tidak perlu (`tests/node_modules/` + `@playwright/test` sudah ada).

### Playwright spec skeleton (rekomendasi, model dari spec existing)
```ts
// createworker-392.spec.ts — Phase 392 WRKR-01/02/03 (Razor + cascade runtime, lesson Phase 354).
// PRECONDITION: app running http://localhost:5277 (Authentication__UseActiveDirectory=false dotnet run) + DB lokal.
//   Run: cd tests; npx playwright test e2e/createworker-392.spec.ts --workers=1
// Auth: admin@pertamina.com (123456) — dev lokal. Cleanup: email-unik + DeleteWorker POST (Identity cascade roles).
import { test, expect, type Page } from '@playwright/test';
import { accounts } from '../helpers/accounts';
import * as db from '../helpers/dbSnapshot';

const TS = Date.now();
const EMAIL = `e2e-cw-${TS}@local.test`;
const FULLNAME = `E2E CreateWorker ${TS}`;
let workerId = '';

test.describe.configure({ mode: 'serial' });

async function loginAdmin(page: Page) {
  const { email, password } = accounts.admin;
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(u => !u.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Phase 392 — CreateWorker editable + validasi + create', () => {
  test.afterAll(async () => {
    // D-07: teardown WAJIB jalan walau test gagal (afterAll). DeleteWorker → Identity cascade roles.
    try {
      if (!workerId) {
        workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${EMAIL}'`).catch(() => '');
      }
      if (workerId) {
        // Opsi A (rekomendasi): submit DeleteWorker form via authenticated page (anti-forgery + Identity cascade).
        // Opsi B: raw SQL DELETE — DIHINDARI (skip cascade roles, F-NEW-07).
        // (Implementasi: login admin di page baru → goto /Admin/ManageWorkers → submit form #delete-{workerId}.)
      }
    } catch { /* warn-only; SEED_JOURNAL ditandai sesuai hasil */ }
  });

  test('field editable + type=email + cascade + create sukses', async ({ page }) => {
    await loginAdmin(page);
    await page.goto('/Admin/CreateWorker');

    // WRKR-01: editable (AD-off; source-grep guard membuktikan mode-AD by construction)
    await page.fill('#FullName', FULLNAME);
    await page.fill('#Email', EMAIL);

    // WRKR-02: type=email (no dobel)
    expect(await page.locator('#Email').getAttribute('type')).toBe('email');

    // WRKR-03: cascade Bagian→Unit (pilih Section pertama yang ada → Unit options muncul)
    const firstSection = await page.locator('#sectionSelect option:not([value=""])').first().getAttribute('value');
    if (firstSection) {
      await page.selectOption('#sectionSelect', firstSection);
      await expect.poll(async () =>
        page.locator('#unitSelect option:not([value=""])').count()
      ).toBeGreaterThan(0);
    }

    // mode AD-off → password block tampil; isi agar lolos server "Password harus diisi"
    await page.fill('#passwordField', 'Test123!');
    await page.fill('#confirmPasswordField', 'Test123!');

    // WRKR-03: submit → redirect ManageWorkers + flash Success
    await Promise.all([
      page.waitForURL('**/ManageWorkers', { timeout: 15_000 }),
      page.click('button[type="submit"]'),
    ]);
    await expect(page.locator('.alert')).toContainText('berhasil');

    // DB assert: row tersimpan dengan Email + role
    workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${EMAIL}'`);
    expect(workerId).toBeTruthy();
  });
});
```
> Catatan: selector field (`#FullName`, `#Email`, `#sectionSelect`, `#unitSelect`, `#passwordField`, `#confirmPasswordField`) **VERIFIED** dari markup (`asp-for` → id = property name; password id eksplisit L153/163). Planner/executor finalkan detail teardown (Opsi A submit form `#delete-{workerId}` di ManageWorkers, atau panggil DeleteWorker via request context dengan anti-forgery). Validasi inline live (WRKR-02 span) bisa dites di test terpisah (submit kosong → assert `.text-danger` terisi tanpa full navigation).

## Security Domain

> `security_enforcement` tidak diset eksplisit di config → default enabled. Phase view-only, surface minimal.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak diubah — login AD/lokal FROZEN (WorkerController/AccountController) |
| V3 Session Management | no | Tak disentuh |
| V4 Access Control | yes (existing) | `[Authorize(Roles="Admin, HC")]` pada CreateWorker GET/POST + DeleteWorker (VERIFIED L196/210/485) — tak diubah |
| V5 Input Validation | yes | `[Required]`/`[EmailAddress]`/`[Compare]`/`[StringLength]` (model) + server Section/Unit validity (controller) + `type=email` (browser). Client-side = UX, server = authoritative (tetap) |
| V6 Cryptography | no | Password hashing oleh Identity `CreateAsync` (FROZEN) — tak hand-roll |

### Known Threat Patterns for ASP.NET Core MVC Razor form
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF pada POST CreateWorker/DeleteWorker | Tampering | `@Html.AntiForgeryToken()` (form L49) + `[ValidateAntiForgeryToken]` (L211/486) — VERIFIED, tak diubah |
| XSS via `@Html.Raw(ViewBag.SectionUnitsJson)` | Tampering/Info | Data = `JsonSerializer.Serialize(dict)` dari OrganizationUnits DB (server-controlled, bukan user input arbitrer). Tetap; tak diperluas oleh phase ini |
| Client-side validation bypass | Tampering | Server ModelState authoritative (L216-254) tetap menolak input invalid walau client-side dilewati — defense tetap utuh |
| Unauthorized worker creation | Elevation | `[Authorize(Roles="Admin, HC")]` (tak diubah) |
| Test data residu (D-07 cleanup) | — (hygiene) | Email-unik + DeleteWorker cascade teardown di `afterAll` + SEED_JOURNAL (CLAUDE.md Seed Workflow) |

**Catatan:** Mengaktifkan client-side validation TIDAK melemahkan keamanan — server-side validation tetap authoritative (defense in depth). `type="email"` = UX/UI hint, bukan security boundary.

## Sources

### Primary (HIGH confidence) — semua dibaca langsung sesi ini
- `Views/Admin/CreateWorker.cshtml` (206 baris, full read) — semua anchor edit
- `Views/Account/Settings.cshtml:120-149` — preseden `@section Scripts { _ValidationScriptsPartial }`
- `Views/Shared/_ValidationScriptsPartial.cshtml` (full) — isi partial
- `Views/Shared/_Layout.cshtml:225-284` — jQuery L241 + `@RenderSectionAsync("Scripts")` L267 (ordering proof)
- `wwwroot/js/shared-cascade.js` (full) — `initSectionUnitCascade` vanilla DOM + `togglePassword`
- `Models/ManageUserViewModel.cs` (full) — annotations: `[Required]` FullName/Email/Role, `[EmailAddress]` Email, `[Compare]` ConfirmPassword, org+Password optional
- `Controllers/WorkerController.cs:194-309` (CreateWorker GET+POST) + `:483-693` (DeleteWorker) — ModelState keys, redirect, `_userManager.DeleteAsync` cascade, guards
- `Views/_ViewImports.cshtml` — `@addTagHelper *` registered; grep `DataType.EmailAddress` = kosong (D-03 valid)
- `Views/Admin/EditWorker.cshtml` (grep L68-80) — pola readonly+bg-light identik (OUT of scope, divergensi sadar)
- `Views/Admin/ManageWorkers.cshtml:285-300` — form DeleteWorker (`#delete-@user.Id`, anti-forgery, hidden id) → teardown UI path
- `tests/playwright.config.ts` + `tests/helpers/{auth,accounts,dbSnapshot}.ts` + `tests/e2e/{assessment-title-flexible,delete-records-cascade,global.setup}.ts` — harness, login fixture, cleanup pola
- `.planning/config.json` — `nyquist_validation:true`, `commit_docs:true`
- `appsettings.json:14` — `UseActiveDirectory: true` (default; lokal override `false`)

### Secondary (MEDIUM confidence)
- N/A — semua diverifikasi dari source primer.

### Tertiary (LOW confidence)
- A1/A2/A3 di Assumptions Log — perilaku framework standar (ClientValidation default-on, TagHelper type-attribute, Identity cascade) yang akan terkonfirmasi oleh runtime di verifikasi. Tidak ada klaim eksternal tak terverifikasi.

## Metadata

**Confidence breakdown:**
- Standard stack (aset ter-vendor): HIGH — semua file/path diverifikasi ada langsung
- Line anchors: HIGH — file dibaca penuh, anchor CONTEXT akurat 1:1
- Architecture/pola D-05: HIGH — preseden Settings.cshtml + ordering _Layout diverifikasi
- D-03 (`type=email` valid, no dobel): HIGH — grep `DataType.EmailAddress` kosong + model annotation dibaca
- Controller/model FROZEN behavior: HIGH — dibaca penuh (read-only)
- Pitfalls: HIGH (P1/P2 dari lesson terdokumentasi + audit; P3/P4 dari source langsung)
- Validation architecture / Playwright: HIGH — harness existing dibaca, spec skeleton dari pola spec nyata
- A1/A2/A3: LOW-risk asumsi, auto-konfirmasi runtime

**Research date:** 2026-06-17
**Valid until:** ~2026-07-17 (30 hari — codebase stabil, view-only; re-verifikasi anchor jika ada commit lain menyentuh `CreateWorker.cshtml` sebelum eksekusi)

## RESEARCH COMPLETE
