# Phase 343: Integrasi App-wide (label tier dynamic) - Research

**Researched:** 2026-06-03
**Domain:** ASP.NET Core MVC Razor refactor — swap hardcoded display string `"Bagian"`/`"Unit"`/`"Sub-unit"` → `@OrgLabels.GetLabel(N)` via global `@inject` di `_ViewImports.cshtml`
**Confidence:** HIGH (mechanism + occurrence audit verified langsung dari codebase via grep + Read)

## Summary

Phase 343 adalah **refactor display-string app-wide**, bukan fitur baru. Mekanisme sudah siap 100% dari Phase 340: `IOrgLabelService` ter-register di `Program.cs` (L17 `AddMemoryCache()`, L65 `AddScoped<...>`), `GetLabel(int)` cached + fallback `"Level {N}"`, seed default Level 0=Bagian/1=Unit/2=Sub-unit ada di `Data/SeedData.cs`. Yang belum: 1 baris `@inject` di `Views/_ViewImports.cshtml` (file ada, belum punya custom @inject), lalu swap occurrence display string di view + (sedikit) controller.

Inti risiko phase ini **bukan teknis** — `@inject` + `@OrgLabels.GetLabel(0)` itu trivial. Inti risikonya adalah **signal-vs-noise**: grep `Bagian|Unit` menghasilkan ~200+ match tapi mayoritas adalah NOISE (nama unit data, property access `.Bagian`/`.Unit`, JS variable `selectedBagian`, element id `filterBagian`, audit-log body, Excel export header, import-doc field-description). Hanya subset kecil (~50 occurrence di ~18 view) yang benar-benar display tier-label yang harus diganti. Deliverable utama (SC1) adalah audit table pre-classified di bawah, supaya planner langsung punya file:line list dengan verdict.

**Primary recommendation:** Plan 2 fase logis — (A) tambah 1 baris `@inject` ke `_ViewImports.cshtml` + swap REPLACE occurrences per-area mengikuti audit table di bawah; (B) audit-only documentation untuk area tanpa view (Worker/CoachMapping/Renewal/DocumentAdmin Views folder = 0 file, formnya di `Views/Admin/`). Controller swap (ORG-INTEG-02) minimal — hampir semua controller match adalah Excel-header/audit/validation = SKIP. JANGAN ganti element id, JS var, property access, nama data, atau import-doc table.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (@inject placement):** Tambah `@inject HcPortal.Services.IOrgLabelService OrgLabels` **global di `Views/_ViewImports.cshtml`** (1 baris, semua view dapat `OrgLabels` otomatis). Spec §5 aligned. Service Scoped + IMemoryCache. File `_ViewImports.cshtml` sudah ada (belum ada @inject custom). NO per-view @inject repetitif.
- **D-02 (Audit filosofi ganti-vs-skip):** **Pragmatic high-value** — ganti HANYA display label yang berubah makna user-facing saat rename: filter label (`<label>Bagian</label>`), table header (`<th>Bagian</th>`), form field label, breadcrumb, dropdown label. SKIP: help/doc table (deskripsi field), technical context, ambiguous. Fokus SC2 (rename muncul di page), minimize risk over-replace.
- **D-03 (Scope area realistis):** **Audit-driven actual** — scope = occurrence grep aktual. Worker/CoachMapping/Renewal/DocumentAdmin Views folder = "Bagian"/"Unit" 0 occurrence → **audit-only** (dokumentasi temuan di SC1, no forced change). TIDAK paksa 7-area kalau Views kosong.
- **D-04 (Occurrence ambigu):** **Claude's discretion + rule**, tiap keputusan dicatat di audit deliverable SC1. Rule: ganti display label user-facing jelas; SKIP deskripsi nama-field-data (mis. ProtonData/ImportSilabus `<td>Bagian</td>` tabel panduan import = deskripsi kolom, static), migration/import doc; combined phrase ("Bagian/Unit") → `GetLabel(0)`/`GetLabel(1)` per-part ATAU skip bila awkward.

### Claude's Discretion
- Audit deliverable format (SC1): markdown table cukup (planner picks format).
- Controller @inject `_orgLabels`: hanya controller yang punya display string TempData/ViewBag (audit per controller; mayoritas "Bagian"/"Unit" = query/audit/prop, skip).
- Urutan plan: audit dulu lalu apply, atau per-area plan — planner decides.
- Apakah perlu 1 plan audit-only + 1 plan apply, atau gabung — planner.

### Deferred Ideas (OUT OF SCOPE)
- Formal xUnit + Playwright E2E 5 scenario + manual UAT + regression smoke → **Phase 344** (TEST-01..06, ORG-INTEG-03).
- ManageOrganization page (JS-fetch path) — done Phase 342, NOT re-touched here.
- Schema/migration change (none). NO migration this phase (CLAUDE.md).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ORG-INTEG-01 | View Razor di area target ganti hardcoded `"Bagian"/"Unit"` pakai `@inject IOrgLabelService OrgLabels` + `@OrgLabels.GetLabel(N)` | §"Mekanisme @inject" (verified Program.cs L65 + namespace `HcPortal.Services`) + Audit Table (kolom verdict REPLACE = target ORG-INTEG-01) |
| ORG-INTEG-02 | Controller display string ke response/TempData/ViewBag dynamic via `_orgLabels.GetLabel(N)`. Audit log body + test literal tetap statis | §"Controller Audit" — hasil: **near-zero REPLACE**; semua controller match = Excel header / audit body / validation msg / property interpolation = SKIP. Lihat tabel controller di bawah |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Resolve label tier (Bagian/Unit/Sub-unit) | Frontend Server (Razor render) | API/Backend (controller) | Display string di-resolve saat server-render view via `@OrgLabels.GetLabel(N)`; controller hanya untuk string yang sudah di-compose di server sebelum kirim ke response |
| Label data source + cache | Database / Storage | — | `OrganizationLevelLabels` tabel, di-cache IMemoryCache key `OrgLabels:All`, invalidate on mutation (Phase 340) |
| Label rename (mutation) | API/Backend (Phase 341 CRUD page) | — | Out of scope Phase 343 — sudah ada di `/Admin/ManageOrgLevelLabels` |
| Unit NAME (data, e.g. "RFCC LPG Treating Unit") | Database / Storage | — | Data, BUKAN tier label — JANGAN tersentuh swap |

**Catatan tier:** Phase ini murni **Frontend Server (SSR)** tier. Tidak ada perubahan browser/JS logic (element id & JS var tetap literal), tidak ada perubahan API endpoint, tidak ada perubahan DB/schema. Ini menyederhanakan risiko: hanya teks yang dirender server yang berubah.

## Standard Stack

Tidak ada library baru. Semua sudah ada di project (Phase 340).

### Core
| Komponen | Versi | Purpose | Why Standard |
|----------|-------|---------|--------------|
| `IOrgLabelService` / `OrgLabelService` | Phase 340 | `GetLabel(int)` cached + fallback `"Level {N}"` | [VERIFIED: Services/OrgLabelService.cs L31-35] satu-satunya sumber label dinamis |
| ASP.NET Core Razor `@inject` | net (existing) | DI service ke view via `_ViewImports.cshtml` | [CITED: spec §5 L488-491] pattern resmi ASP.NET Core untuk view-level DI |
| `IMemoryCache` | existing | Backing cache `OrgLabels:All` | [VERIFIED: Program.cs L17 `AddMemoryCache()`] |

### Supporting
| Komponen | Purpose | When to Use |
|----------|---------|-------------|
| `_ViewImports.cshtml` | Global @inject host | [VERIFIED: file ada, isi 5 baris @using + @addTagHelper, BELUM ada @inject custom] — tambah 1 baris di sini (D-01) |

**Installation:** NONE. Tidak ada `dotnet add package`. Tidak ada migration (CLAUDE.md: NO migration this phase). Verifikasi cukup `dotnet build`.

## Mekanisme @inject (Verified)

**Baris yang harus ditambah ke `Views/_ViewImports.cshtml`** (D-01, spec §5 L490):
```razor
@inject HcPortal.Services.IOrgLabelService OrgLabels
```
- [VERIFIED: Services/IOrgLabelService.cs L1] namespace = `HcPortal.Services` (BUKAN `HcPortal` saja). `_ViewImports.cshtml` saat ini `@using HcPortal` + `@using HcPortal.Models` — jadi tetap pakai fully-qualified `HcPortal.Services.IOrgLabelService` ATAU tambah `@using HcPortal.Services` dulu lalu `@inject IOrgLabelService OrgLabels`. **Rekomendasi: fully-qualified 1 baris** (persis seperti spec §5, minimal diff).
- [VERIFIED: Program.cs L65] `builder.Services.AddScoped<HcPortal.Services.IOrgLabelService, HcPortal.Services.OrgLabelService>();` — sudah register, @inject akan resolve.
- [VERIFIED: OrgLabelService.cs L31-35] `GetLabel(level)` → `dict.TryGetValue(level, out var label) ? label : $"Level {level}"`. Fallback aman bila tabel kosong.
- [VERIFIED: SeedData.cs L121-123] default Level 0="Bagian", 1="Unit", 2="Sub-unit" → mapping `GetLabel(0)`/`(1)`/`(2)` konsisten dengan convention CONTEXT.

**Pola swap di view:**
```razor
@* SEBELUM *@
<label class="form-label">Bagian</label>
<th>Bagian</th>
<option value="">Semua Bagian</option>

@* SESUDAH *@
<label class="form-label">@OrgLabels.GetLabel(0)</label>
<th>@OrgLabels.GetLabel(0)</th>
<option value="">Semua @OrgLabels.GetLabel(0)</option>
```
[CITED: spec §4.8 L456] "View Razor: ganti hardcoded string ... jadi `@OrgLabels.GetLabel(N)`".

---

## SC1 DELIVERABLE — Signal-vs-Noise Occurrence Audit (CORE)

> Hasil grep `Bagian|Unit|Sub-unit` lalu Read context per occurrence. Verdict per D-02/D-04.
> **REPLACE** = display tier-label user-facing → swap ke `@OrgLabels.GetLabel(N)`.
> **SKIP** = noise (data name / property / id / JS var / audit body / import-doc / Excel header / technical).
> **AMBIGUOUS** = perlu keputusan eksplisit (dicatat + alasan).

### Folder Structure Finding (validasi D-03)
| Views folder | Status |
|--------------|--------|
| `Views/Worker/` | [VERIFIED] **KOSONG (0 file)** — Worker forms ada di `Views/Admin/CreateWorker.cshtml`, `EditWorker.cshtml`, `WorkerDetail.cshtml`, `ManageWorkers.cshtml`, `ImportWorkers.cshtml` |
| `Views/CoachMapping/` | [VERIFIED] **KOSONG (0 file)** — mapping view ada di `Views/Admin/CoachCoacheeMapping.cshtml` |
| `Views/Renewal/` | [VERIFIED] **KOSONG (0 file)** — renewal view ada di `Views/Admin/RenewalCertificate.cshtml` |
| `Views/DocumentAdmin/` | [VERIFIED] **KOSONG (0 file)** — KKJ/CPDP view ada di `Views/Admin/Kkj*.cshtml`, `Cpdp*.cshtml`, `Views/CMP/DokumenKkj.cshtml` |

**Kesimpulan D-03:** 4 dari 7 "area" tidak punya Views folder sendiri. Audit aktual scope-nya ke folder yang BENAR ADA file: `Views/CMP/`, `Views/CDP/`, `Views/ProtonData/`, `Views/Admin/`, plus `Views/Account/` (Profile/Settings — temuan tambahan tidak ada di spec 7-area). Worker/CoachMapping/Renewal/DocumentAdmin = documented as audit-only (form-nya tercakup di Views/Admin).

### REPLACE Targets (display tier-label → swap)

| file:line | snippet | tier | verdict | reason |
|-----------|---------|------|---------|--------|
| Views/CMP/AnalyticsDashboard.cshtml:84 | `<label for="filterBagian">Bagian</label>` | 0 | REPLACE | filter label visible text (id `filterBagian` tetap literal) |
| Views/CMP/AnalyticsDashboard.cshtml:86 | `<option value="">Semua Bagian</option>` | 0 | REPLACE | dropdown label → `Semua @OrgLabels.GetLabel(0)` |
| Views/CMP/AnalyticsDashboard.cshtml:94 | `<label for="filterUnit">Unit</label>` | 1 | REPLACE | filter label |
| Views/CMP/AnalyticsDashboard.cshtml:96 | `<option value="">Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/CMP/AnalyticsDashboard.cshtml:543 | `<h6>Perbandingan Antar Kelompok (Bagian)</h6>` | 0 | REPLACE | heading parenthetical tier label |
| Views/CMP/AnalyticsDashboard.cshtml:548 | `<th>Bagian</th>` | 0 | REPLACE | table header |
| Views/CMP/RecordsTeam.cshtml:20 | `<label>Bagian</label>` | 0 | REPLACE | filter label |
| Views/CMP/RecordsTeam.cshtml:24 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/CMP/RecordsTeam.cshtml:41 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label (2nd selector) |
| Views/CMP/RecordsTeam.cshtml:50 | `<label>Unit</label>` | 1 | REPLACE | filter label |
| Views/CMP/RecordsTeam.cshtml:52 | `<option>Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/CMP/RecordsTeam.cshtml:135 | `<th class="p-3">Bagian</th>` | 0 | REPLACE | table header |
| Views/CMP/RecordsTeam.cshtml:136 | `<th class="p-3">Unit</th>` | 1 | REPLACE | table header |
| Views/CDP/CertificationManagement.cshtml:89 | `<label>Bagian</label>` | 0 | REPLACE | filter label |
| Views/CDP/CertificationManagement.cshtml:97 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/CDP/CertificationManagement.cshtml:105 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label (branch lain) |
| Views/CDP/CertificationManagement.cshtml:110 | `<label>Unit</label>` | 1 | REPLACE | filter label |
| Views/CDP/CertificationManagement.cshtml:112 | `<option>Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/CDP/CoachingProton.cshtml:100 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/CDP/CoachingProton.cshtml:120 | `<option>Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/CDP/HistoriProton.cshtml:34 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label (disabled branch) |
| Views/CDP/HistoriProton.cshtml:51 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label (enabled branch) |
| Views/CDP/HistoriProton.cshtml:30 | `<label>Bagian</label>` | 0 | REPLACE | filter label |
| Views/CDP/HistoriProton.cshtml:60 | `<label>Unit</label>` | 1 | REPLACE | filter label |
| Views/CDP/HistoriProton.cshtml:62 | `<option>Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/CDP/HistoriProton.cshtml:109 | `<th>Unit</th>` | 1 | REPLACE | table header |
| Views/CDP/HistoriProtonDetail.cshtml:36 | `<dt>Unit</dt>` | 1 | REPLACE | definition-list field label |
| Views/CDP/HistoriProtonDetail.cshtml:84 | `<small>Unit</small>` | 1 | REPLACE | field label |
| Views/CDP/PlanIdp.cshtml:72 | `<label>Bagian</label>` | 0 | REPLACE | form field label |
| Views/CDP/PlanIdp.cshtml:86 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder → `-- Pilih @OrgLabels.GetLabel(0) --` |
| Views/CDP/PlanIdp.cshtml:91 | `<label>Unit</label>` | 1 | REPLACE | form field label |
| Views/CDP/PlanIdp.cshtml:94 | `<option>-- Pilih Unit --</option>` | 1 | REPLACE | placeholder |
| Views/CDP/Shared/_CoachingProtonPartial.cshtml:29 | `<label>Unit</label>` | 1 | REPLACE | partial form label |
| Views/CDP/Shared/_CoachingProtonPartial.cshtml:35 | `<option>Semua Unit</option>` | 1 | REPLACE | partial dropdown label |
| Views/CDP/Shared/_CertificationManagementTablePartial.cshtml:19 | `<th>Bagian</th>` | 0 | REPLACE | partial table header |
| Views/CDP/Shared/_CertificationManagementTablePartial.cshtml:20 | `<th>Unit</th>` | 1 | REPLACE | partial table header |
| Views/ProtonData/Index.cshtml:79 | `<label>Bagian</label>` | 0 | REPLACE | form field label |
| Views/ProtonData/Index.cshtml:81 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/ProtonData/Index.cshtml:85 | `<label>Unit</label>` | 1 | REPLACE | form field label |
| Views/ProtonData/Index.cshtml:87 | `<option>-- Pilih Unit --</option>` | 1 | REPLACE | placeholder |
| Views/ProtonData/Index.cshtml:230 | `<label>Bagian</label>` | 0 | REPLACE | guidance form label |
| Views/ProtonData/Index.cshtml:232 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/ProtonData/Index.cshtml:236 | `<label>Unit</label>` | 1 | REPLACE | guidance form label |
| Views/ProtonData/Index.cshtml:238 | `<option>-- Pilih Unit --</option>` | 1 | REPLACE | placeholder |
| Views/ProtonData/Override.cshtml:29 | `<label>Bagian</label>` | 0 | REPLACE | form field label |
| Views/ProtonData/Override.cshtml:31 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/ProtonData/Override.cshtml:35 | `<label>Unit</label>` | 1 | REPLACE | form field label |
| Views/ProtonData/Override.cshtml:37 | `<option>-- Pilih Unit --</option>` | 1 | REPLACE | placeholder |
| Views/Admin/CoachCoacheeMapping.cshtml:235 | `<th>Bagian Penugasan</th>` | 0 | REPLACE | table header (combined w/ "Penugasan" suffix → `@OrgLabels.GetLabel(0) Penugasan`) |
| Views/Admin/CoachCoacheeMapping.cshtml:236 | `<th>Unit Penugasan</th>` | 1 | REPLACE | table header → `@OrgLabels.GetLabel(1) Penugasan` |
| Views/Admin/CoachCoacheeMapping.cshtml:435 | `<label>Bagian Penugasan</label>` | 0 | REPLACE | form label |
| Views/Admin/CoachCoacheeMapping.cshtml:437 | `<option>— Pilih Bagian —</option>` | 0 | REPLACE | placeholder |
| Views/Admin/CoachCoacheeMapping.cshtml:445 | `<label>Unit Penugasan</label>` | 1 | REPLACE | form label |
| Views/Admin/CoachCoacheeMapping.cshtml:503 | `<label>Bagian Penugasan</label>` | 0 | REPLACE | form label (2nd modal) |
| Views/Admin/CoachCoacheeMapping.cshtml:505 | `<option>— Pilih Bagian —</option>` | 0 | REPLACE | placeholder (2nd modal) |
| Views/Admin/CoachCoacheeMapping.cshtml:513 | `<label>Unit Penugasan</label>` | 1 | REPLACE | form label (2nd modal) |
| Views/Admin/CreateWorker.cshtml:116 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/Admin/CreateWorker.cshtml:122 | `<option>-- Pilih Unit --</option>` | 1 | REPLACE | placeholder |
| Views/Admin/EditWorker.cshtml:121 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/Admin/EditWorker.cshtml:127 | `<option>-- Pilih Unit --</option>` | 1 | REPLACE | placeholder |
| Views/Admin/ManageWorkers.cshtml:132 | `<label>Bagian</label>` | 0 | REPLACE | filter label |
| Views/Admin/ManageWorkers.cshtml:134 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/Admin/ManageWorkers.cshtml:152 | `<label>Unit</label>` | 1 | REPLACE | filter label |
| Views/Admin/ManageWorkers.cshtml:154 | `<option>Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/Admin/ManageWorkers.cshtml:226 | `<th>Bagian</th>` | 0 | REPLACE | table header |
| Views/Admin/ManageWorkers.cshtml:227 | `<th>Unit</th>` | 1 | REPLACE | table header |
| Views/Admin/WorkerDetail.cshtml:89 | `<td class="text-muted">Bagian</td>` | 0 | REPLACE | detail field label (left col) |
| Views/Admin/WorkerDetail.cshtml:102 | `<td class="text-muted">Unit</td>` | 1 | REPLACE | detail field label |
| Views/Admin/RenewalCertificate.cshtml:55 | `<label>Bagian</label>` | 0 | REPLACE | filter label |
| Views/Admin/RenewalCertificate.cshtml:57 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/Admin/RenewalCertificate.cshtml:65 | `<label>Unit</label>` | 1 | REPLACE | filter label |
| Views/Admin/RenewalCertificate.cshtml:67 | `<option>Semua Unit</option>` | 1 | REPLACE | dropdown label |
| Views/Admin/Shared/_TrainingRecordsTab.cshtml:37 | `<label>Bagian</label>` | 0 | REPLACE | partial filter label |
| Views/Admin/Shared/_TrainingRecordsTab.cshtml:45 | `<option>Semua Bagian</option>` | 0 | REPLACE | partial dropdown label |
| Views/Admin/Shared/_TrainingRecordsTab.cshtml:87 | `<label>Unit</label>` | 1 | REPLACE | partial filter label |
| Views/Admin/Shared/_TrainingRecordsTab.cshtml:94 | `<option>Semua Unit</option>` | 1 | REPLACE | partial dropdown label |
| Views/Admin/Shared/_TrainingRecordsTab.cshtml:194 | `<th>Unit</th>` | 1 | REPLACE | partial table header |
| Views/Admin/EditAssessment.cshtml:522 | `<th>Bagian</th>` | 0 | REPLACE | table header |
| Views/Admin/EditAssessment.cshtml:598 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/Admin/CreateAssessment.cshtml:264 | `<option>Semua Bagian</option>` | 0 | REPLACE | dropdown label |
| Views/Admin/CpdpUpload.cshtml:39 | `Pilih Bagian` (label text) | 0 | REPLACE | selector label |
| Views/Admin/CpdpUpload.cshtml:42 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/Admin/KkjUpload.cshtml:39 | `Pilih Bagian` (label text) | 0 | REPLACE | selector label |
| Views/Admin/KkjUpload.cshtml:42 | `<option>-- Pilih Bagian --</option>` | 0 | REPLACE | placeholder |
| Views/Account/Profile.cshtml:78 | `<div>Bagian</div>` | 0 | REPLACE | profile field label |
| Views/Account/Profile.cshtml:84 | `<div>Unit</div>` | 1 | REPLACE | profile field label |
| Views/Account/Settings.cshtml:96 | `<label>Bagian</label>` | 0 | REPLACE | settings field label |
| Views/Account/Settings.cshtml:114 | `<label>Unit</label>` | 1 | REPLACE | settings field label |

> **Catatan partial views:** `_CoachingProtonPartial.cshtml`, `_CertificationManagementTablePartial.cshtml`, `_TrainingRecordsTab.cshtml`, `_RecordsTeamBody.cshtml` — partial **mewarisi `@inject` dari `_ViewImports.cshtml`** (D-01 global), jadi `@OrgLabels` available di partial tanpa @inject ulang. [CITED: ASP.NET Core — `_ViewImports` applies hierarchically to all views & partials under its folder tree; `Views/_ViewImports.cshtml` covers semua subfolder]. **VERIFY saat build** (lihat Pitfall 5).

### AMBIGUOUS (keputusan eksplisit + alasan)

| file:line | snippet | tier | verdict | reasoning |
|-----------|---------|------|---------|-----------|
| Views/Admin/CpdpFiles.cshtml:64, :87 | `Tambah Bagian` / `Hapus Bagian` (button) | 0 | **REPLACE (rekomendasi)** | Button referensi tier root (KkjBagianAdd bikin OrganizationUnit Level 0). User-facing, ikut rename. Tapi JS func `addBagian()`/`deleteBagian()` & toast `'Bagian berhasil dihapus'` (L172, L223, L241) → SKIP (JS identifier + toast literal). **Risiko low-medium**: planner boleh defer kalau mau minimize scope (CONTEXT D-02 "minimize risk over-replace") |
| Views/Admin/KkjMatrix.cshtml:64, :87 | `Tambah Bagian` / `Hapus Bagian` (button) | 0 | **REPLACE (rekomendasi)** | sama dengan CpdpFiles (file kembar). JS func + toast = SKIP |
| Views/Admin/Index.cshtml:45 | `Kelola hierarki Bagian dan Unit kerja dengan tampilan tree` | combined | **SKIP** | deskripsi card admin panel = sentence prose, combined "Bagian dan Unit kerja"; awkward kalau di-split. Static descriptive text, bukan field label (D-04 combined-phrase rule → skip bila awkward) |
| Views/Admin/Index.cshtml:61 | `Kelola nama tier organisasi (Bagian/Unit/Sub-unit) tanpa edit kode` | combined | **SKIP** | INI deskripsi card ManageOrgLevelLabels — menjelaskan APA yang label di-edit; harus tetap literal default supaya self-documenting (kalau dinamis → "Kelola nama tier (Direktorat/...)" jadi tautologis/membingungkan) |
| Views/Admin/ManageOrgLevelLabels.cshtml:17 | `Ubah nama tampilan tier (Bagian/Unit/Sub-unit/...)` | combined | **SKIP** | sama — ini SUBTITLE page CRUD label itu sendiri; harus default literal, bukan output GetLabel (circular) |
| Views/Admin/ManageOrganization.cshtml:115 | `Kelola hierarki Bagian dan Unit kerja` | combined | **SKIP** | Phase 342 page (out of scope CONTEXT). Subtitle prose combined |
| Views/Admin/ManageOrganization.cshtml:159 | `<h5>Tambah Unit</h5>` modal title | 1 | **SKIP** | Phase 342 sudah handle modal title dynamic via JS (ORG-TREE-09 done). JANGAN double-handle (CONTEXT out-of-scope: ManageOrganization page) |
| Views/Admin/EditAssessment.cshtml:606 | `<option>Lainnya (Tanpa Bagian)</option>` | 0 | **AMBIGUOUS → REPLACE-careful or SKIP** | "Tanpa Bagian" = "tanpa tier-0". Bisa `Lainnya (Tanpa @OrgLabels.GetLabel(0))`. Low value, planner discretion. Default: REPLACE untuk konsistensi |
| Views/Admin/CreateAssessment.cshtml:272 | `<option>Lainnya (Tanpa Bagian)</option>` | 0 | **AMBIGUOUS → REPLACE-careful or SKIP** | sama dengan EditAssessment:606 |
| Views/CDP/CoachingProton.cshtml:95 (comment) | `@* Bagian dropdown — HC/Admin ... *@` | 0 | **SKIP** | Razor comment, tidak dirender |

### SKIP (noise — JANGAN ganti)

Kategori + contoh (representatif, bukan exhaustive — semua match selain REPLACE/AMBIGUOUS di atas = SKIP):

| Kategori noise | Contoh file:line | Reason |
|----------------|------------------|--------|
| **Import/doc field-description table** | ProtonData/ImportSilabus.cshtml:205-206 (`<td>Bagian</td><td>Nama bagian...</td>`), Admin/ImportWorkers.cshtml:205-206 | Tabel panduan import = deskripsi kolom Excel (header file = literal "Bagian"). [CITED: CONTEXT D-04 explicit SKIP] |
| **Element id / `for=` attr** | `id="filterBagian"`, `for="filterBagian"`, `id="silabusBagian"`, `id="overrideBagian"` | Identifier — JS bergantung pada id literal. HANYA visible text yang berubah (Pitfall 1) |
| **JS variable / function name** | `selectedBagian`, `currentBagian`, `unitsByBagian`, `onBagianChange()`, `addBagian()`, `bagianSelects[]` | Kode JS, bukan display. Ganti = break |
| **ViewBag/property access** | `ViewBag.Bagian`, `ViewBag.AllBagian`, `ViewBag.SelectedBagianId`, `@row.Bagian`, `@w.Unit`, `@Model.Unit`, `@node.Unit`, `bagian.Name`, `bagian.Id` | C# property/data access. Schema unchanged (CONTEXT D-02 SKIP) |
| **Unit NAME (data)** | `RFCC LPG Treating...` (ImportWorkers:206 example col), `@bagian.Name` render | Data, bukan tier label (CONTEXT D-02 SKIP) |
| **`@foreach` loop var** | `foreach (var b in allBagian)`, `foreach (var bagian in bagians)` | Iterator var |
| **Razor comment** | `@* Bagian dropdown ... *@`, `// Cascade: Bagian -> Unit` | Tidak dirender |
| **`_PSign.cshtml:42** | `<div>@Model.Unit</div>` | Render NILAI unit user (data), bukan label "Unit" |
| **CpdpFileHistory:3 / KkjFileHistory:3** | `var bagian = ViewBag.Bagian as OrganizationUnit` | C# var decl |

### Controller Audit (ORG-INTEG-02)

> Grep `"...Bagian..."` + `"...Sub-unit..."` di Controllers/. Hasil: **near-zero REPLACE**.

| Controller:line | snippet | verdict | reason |
|-----------------|---------|---------|--------|
| CDPController.cs:3681, 4015, 4128, 4205 | `"No","Nama","Bagian","Unit",...` | SKIP | Excel/CSV export header array → file kolom literal (export schema deterministik, bukan UI display) |
| CMPController.cs:3112, 3607, 3880 | `new[] {"Bagian","Kategori",...}` | SKIP | Excel export header |
| CoachMappingController.cs:1232 | `"Bagian Penugasan","Unit Penugasan"` | SKIP | Excel export header |
| ProtonDataController.cs:126, 759, 805, 891 | `$"{g.Bagian}|{g.Unit}|..."`, header arrays | SKIP | string interpolation key (126) + Excel header (759/805/891) |
| WorkerController.cs:174, 850 | export header `{"...","Bagian","Unit",...}` | SKIP | Excel export header + template header |
| WorkerController.cs:227, 363 | `$"Bagian '{model.Section}' tidak ditemukan..."` | SKIP-recommended | ModelState validation error. Spec §4.8 SKIP audit-log body; validation msg = gray. **Default SKIP** (deterministik error, low value, CONTEXT D-02 minimize risk). Planner discretion bila mau REPLACE |
| WorkerController.cs:878 | `$"Kolom Bagian: {string.Join(...)}"` | SKIP | Excel cell template instruction text |
| DocumentAdminController.cs:106, 287 | `TempData["Error"]="Bagian tidak ditemukan."` / `Json(...message="Bagian tidak ditemukan.")` | **AMBIGUOUS → SKIP-recommended** | INI yang paling dekat ORG-INTEG-02 target (display string ke response). TAPI: (1) "Bagian" di sini = root tier yang user kelola di KKJ/CPDP file mgmt, (2) low-frequency error path, (3) CONTEXT D-02 "minimize risk over-replace". **Default SKIP**; kalau planner mau strict ORG-INTEG-02 → inject `_orgLabels` ke DocumentAdminController + ganti L106/287/299/315/380 message → `_orgLabels.GetLabel(0)`. Lihat AMBIGUOUS note |
| DocumentAdminController.cs:151, 165, 353, 357, 364, 498, 512, 570 | log/`_logger`/audit `"...bagian..."` | SKIP | Audit log + ILogger body (CONTEXT D-02 explicit SKIP — stabil untuk debug) |
| DocumentAdminController.cs:263 | `Name = "Bagian Baru"` | SKIP | Default NAMA OrganizationUnit baru (data value, bukan label) |
| OrgLabelController.cs:41 | `// Response example: {"0":"Bagian",...}` | SKIP | Code comment |

**Rekomendasi controller (ORG-INTEG-02):** Mayoritas SKIP. **Satu-satunya kandidat REPLACE jelas-bermanfaat = DocumentAdminController TempData/Json messages (L106, 287, 299, 315, 380)** — tapi CONTEXT D-02 "pragmatic high-value + minimize over-replace" condong ke SKIP. **Planner decision:** apakah ORG-INTEG-02 dipenuhi via "audited, near-zero actionable" (dokumentasi bahwa semua adalah export/audit/validation = legit SKIP) ATAU minimal-1 DocumentAdmin inject untuk konkret memenuhi REQ. Rekomendasi penulis: dokumentasikan audit ini sebagai pemenuhan ORG-INTEG-02 (semua display-bearing string adalah export header/audit/validation yang per-spec memang SKIP), dengan opsi DocumentAdmin inject sebagai stretch.

---

## Architecture Patterns

### System Architecture Diagram (data flow label render)

```
[User edit label via /Admin/ManageOrgLevelLabels (Phase 341)]
            │  UpdateAsync(level, newLabel)
            ▼
   OrganizationLevelLabels (DB) ──┐
            │                     │ _cache.Remove("OrgLabels:All")
            ▼                     ▼
   OrgLabelService.GetAll() ◄── IMemoryCache (key "OrgLabels:All", no-TTL)
            │  GetLabel(N) → dict[N] ?? "Level {N}"
            ▼
  ┌─────────────────────────────────────────────┐
  │  @inject (Views/_ViewImports.cshtml, 1 line) │  ← Phase 343 ADD
  │  OrgLabels available di SEMUA view + partial  │
  └─────────────────────────────────────────────┘
            │
   ┌────────┼────────┬─────────┬──────────┐
   ▼        ▼        ▼         ▼          ▼
 CMP/*    CDP/*   ProtonData/* Admin/*  Account/*
 filter   filter   form        form     profile
 <label>  <option> <label>     <th>     <div>
 @OrgLabels.GetLabel(0/1/2)  ← Phase 343 SWAP (REPLACE rows above)
            │
            ▼
   [Rendered HTML — label baru muncul app-wide tanpa redeploy]
```
Trace use-case SC2: HC rename "Bagian"→"Direktorat" → cache invalidate → next page render `GetLabel(0)` return "Direktorat" → muncul di CMP filter + Worker form + CDP assignment.

### Pattern 1: Visible-text-only swap (id/attr/var tetap literal)
**What:** Ganti HANYA teks di antara tag (`>Bagian<`), JANGAN sentuh `id=`, `for=`, `name=`, JS var.
**When:** Setiap REPLACE row.
**Example:**
```razor
@* BENAR — id literal, hanya visible text dinamis *@
<label for="filterBagian" class="form-label">@OrgLabels.GetLabel(0)</label>
<select id="filterBagian">
    <option value="">Semua @OrgLabels.GetLabel(0)</option>
</select>

@* SALAH — jangan: id="filter@OrgLabels..." atau name="@OrgLabels..." *@
```

### Pattern 2: Combined-text concatenation
**What:** Untuk "Bagian Penugasan" / "Semua Bagian" / "-- Pilih Bagian --" → label dinamis + suffix/prefix literal.
**Example:**
```razor
<th>@OrgLabels.GetLabel(0) Penugasan</th>
<option value="">Semua @OrgLabels.GetLabel(0)</option>
<option value="">-- Pilih @OrgLabels.GetLabel(0) --</option>
```

### Anti-Patterns to Avoid
- **Find-replace `Bagian`→`@OrgLabels.GetLabel(0)` global:** akan merusak id, JS var, property access, nama data. WAJIB per-occurrence dari audit table.
- **Ganti import-doc/help table:** deskripsi kolom Excel harus literal (header file = "Bagian").
- **Ganti subtitle page Label CRUD itu sendiri** (ManageOrgLevelLabels:17, Index:61): circular/membingungkan.
- **Re-touch ManageOrganization page:** Phase 342 territory, modal title sudah dynamic.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Resolve label tier | Inline `ViewBag.Label0 ?? "Bagian"` di tiap view | `@OrgLabels.GetLabel(N)` | Sudah ada cached + fallback (Phase 340) |
| Inject service ke view | `@inject` per-view di 18 file | 1 baris global `_ViewImports.cshtml` | D-01; ASP.NET hierarchical view imports |
| Fallback bila tabel kosong | `try/catch` per view | Service sudah return `"Level {N}"` | OrgLabelService.cs L34 |

**Key insight:** Seluruh "mesin" sudah jadi. Phase 343 hanya plumbing 1 baris @inject + swap teks. Bahaya satu-satunya = over-replace (noise jadi target).

## Runtime State Inventory

> Phase ini = source-code edit murni (display string swap). Bukan rename DB/data/service-config.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **None** — label data di `OrganizationLevelLabels` TIDAK berubah (seed 0/1/2 tetap). Swap hanya mengganti hardcode VIEW jadi baca dari tabel yang sudah ada. | None |
| Live service config | **None** — tidak ada service eksternal | None |
| OS-registered state | **None** | None |
| Secrets/env vars | **None** | None |
| Build artifacts | **None khusus** — `dotnet build` recompile Razor views otomatis. Tidak ada egg-info/binary rename. | `dotnet build` verify compile |

**Catatan penting:** Yang kelihatan seperti "runtime state" sebenarnya BUKAN target rename — `User.Section`/`User.Unit` (DB column), nama OrganizationUnit ("RFCC..."), element id, JS var. Semua itu **SKIP** (lihat audit). Tidak ada migrasi data, tidak ada re-register apa pun.

## Common Pitfalls

### Pitfall 1: Over-replace merusak element id / JS hook
**What goes wrong:** Mengganti `id="filterBagian"` atau JS `selectedBagian` → JS event listener & cascade dropdown putus, filter mati.
**Why:** grep "Bagian" cocok dengan id, var, property — semuanya non-display.
**How to avoid:** Hanya ganti teks DI ANTARA tag (`>...<`) di `<label>/<th>/<option>/<td>/<dt>/<div>` yang ada di REPLACE table. Verifikasi grep residual setelah swap (lihat Validation).
**Warning signs:** Dropdown cascade Bagian→Unit berhenti bekerja; console JS error "getElementById null".

### Pitfall 2: Salah level mapping
**What goes wrong:** Pakai `GetLabel(1)` untuk "Bagian".
**How to avoid:** Bagian=0, Unit=1, Sub-unit=2 (SeedData.cs L121-123). Audit table sudah pre-isi kolom tier.

### Pitfall 3: Mengganti import-doc / Excel header (controller)
**What goes wrong:** Ganti Excel export header `"Bagian"` → file Excel yang di-generate jadi inkonsisten dgn template import (header import file user TETAP "Bagian"). Import break.
**How to avoid:** SKIP semua controller header array + ImportSilabus/ImportWorkers `<td>` doc table.

### Pitfall 4: Circular subtitle pada page Label CRUD
**What goes wrong:** Ganti subtitle `ManageOrgLevelLabels:17` "(Bagian/Unit/Sub-unit/...)" jadi dinamis → setelah rename jadi "(Direktorat/Direktorat/...)" membingungkan/tautologis.
**How to avoid:** SKIP combined-phrase subtitle yang FUNGSINYA menjelaskan default tier (Index:61, ManageOrgLevelLabels:17, ManageOrganization:115).

### Pitfall 5: Partial view & @inject inheritance
**What goes wrong:** Asumsi `@OrgLabels` tidak available di partial → tambah @inject duplikat (atau ragu).
**Why:** `_ViewImports.cshtml` berlaku hierarkis ke SEMUA view + partial di bawahnya. `Views/_ViewImports.cshtml` mencakup `Views/**/*.cshtml` termasuk `Views/CDP/Shared/_*.cshtml`, `Views/Admin/Shared/_*.cshtml`.
**How to avoid:** TIDAK perlu @inject ulang di partial. Tapi VERIFY via `dotnet build` (compile-time error jika resolve gagal) + spot-render 1 partial. ViewComponents punya `_ViewImports` terpisah — jika ada label di `Views/Shared/Components/*`, cek terpisah (audit: tidak ditemukan match display di Components/).

### Pitfall 6: Razor `@` di dalam attribute value
**What goes wrong:** Mencoba taruh `@OrgLabels.GetLabel(0)` di dalam `value="..."` atau `placeholder="..."` salah escaping.
**How to avoid:** Untuk attribute pakai `placeholder="@OrgLabels.GetLabel(0)"` (Razor handle). Tapi mayoritas target adalah visible text (`>...<`), bukan attribute — risiko rendah. Tidak ada REPLACE row yang menyentuh attribute value (semua `value=""` placeholder kosong, text di `<option>` body).

## Code Examples

### Tambah @inject (1 baris, ke Views/_ViewImports.cshtml)
```razor
@using HcPortal
@using HcPortal.Models
@using HcPortal.Models.Guide
@using System.Text.Json
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@inject HcPortal.Services.IOrgLabelService OrgLabels   @* ← Phase 343 ADD *@
```
[CITED: spec §5 L490; VERIFIED current _ViewImports.cshtml content]

### Swap di view (representatif)
```razor
@* CMP/AnalyticsDashboard.cshtml L84-86 *@
<label for="filterBagian" class="form-label form-label-sm mb-1">@OrgLabels.GetLabel(0)</label>
<select id="filterBagian" class="form-select form-select-sm">
    <option value="">Semua @OrgLabels.GetLabel(0)</option>
```
[Source: VERIFIED dari AnalyticsDashboard.cshtml:84-86]

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| Hardcode "Bagian"/"Unit" di view | `@inject IOrgLabelService` + `GetLabel(N)` | Rename label sekali (Phase 341) → app-wide tanpa edit kode |
| (n/a) | View-level DI via `_ViewImports.cshtml` | Pattern resmi ASP.NET Core; 1 baris global vs N @inject |

**Deprecated/outdated:** Tidak ada — Phase 343 justru menghapus hardcode lama.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `_ViewImports.cshtml` di `Views/` berlaku ke semua subfolder partial (CDP/Shared, Admin/Shared) tanpa @inject ulang | Pattern/Pitfall 5 | LOW — kalau salah, `dotnet build` langsung gagal compile partial; mudah deteksi. Behavior `_ViewImports` hierarki adalah ASP.NET Core standard |
| A2 | Excel export header di controller HARUS literal (import file user pakai header literal) | Controller Audit / Pitfall 3 | MEDIUM — kalau import logic ternyata tolerant terhadap header dinamis, SKIP-nya tetap aman (tidak ada kerugian biarkan literal). Asumsi konservatif |
| A3 | "Tanpa Bagian" / "Bagian Penugasan" combined-phrase di-render benar via concatenation `@GetLabel(0) Penugasan` | AMBIGUOUS table | LOW — Razor inline expression + literal text adalah pola umum; verify visual |
| A4 | DocumentAdmin TempData/Json "Bagian tidak ditemukan" lebih baik SKIP (low-value, minimize risk) sesuai D-02 | Controller Audit | LOW — keputusan ganti/skip; planner bisa override. Bukan kebenaran teknis |
| A5 | Tidak ada display tier-label di `Views/Shared/Components/*` (ViewComponent) | Pitfall 5 | LOW — grep tidak menemukan match display di Components; tapi ViewComponent punya _ViewImports terpisah jika ada |

## Open Questions (RESOLVED)

1. **ORG-INTEG-02 pemenuhan: dokumentasi-audit vs minimal-inject?**
   - What we know: SEMUA controller display-bearing string = Excel header / audit body / validation / property interp. Per spec §4.8 itu memang kategori SKIP.
   - What's unclear: Apakah REQ ORG-INTEG-02 butuh ≥1 perubahan controller konkret untuk "dianggap dikerjakan", atau cukup audit yang membuktikan tidak ada actionable target.
   - Recommendation: Dokumentasikan audit sebagai pemenuhan (no actionable REPLACE), dengan opsi DocumentAdminController inject (L106/287/299/315/380) sebagai 1 perubahan konkret bila planner ingin bukti eksplisit. Tanyakan saat plan-check bila ragu.
   - **RESOLVED (2026-06-03):** Pemenuhan via dokumentasi-audit (`343-AUDIT.md` §5). Semua controller display-bearing string = legit SKIP (export-header/audit-body/validation). DocumentAdminController inject = DEFAULT SKIP per D-02 "minimize over-replace", dicatat sebagai stretch (no actionable). Diterapkan Plan 01 Task 2.

2. **CpdpFiles/KkjMatrix "Tambah/Hapus Bagian" button — REPLACE atau defer?**
   - What we know: User-facing button referensi tier root; tapi ada JS func + toast literal di file sama.
   - Recommendation: REPLACE button visible text (4 occurrence), SKIP JS/toast. Low-medium risk. Planner boleh defer ke "stretch" bila ingin scope minimal.
   - **RESOLVED (2026-06-03):** REPLACE button visible text (4 occurrence), SKIP JS func + toast literal. Diterapkan Plan 03 Task 3.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (`dotnet build`/`run`) | Verify compile + local render | ✓ (project aktif, CLAUDE.md localhost:5277) | existing | — |
| SQL Server lokal (HcPortalDB) | Render label dari `OrganizationLevelLabels` seed | ✓ (project running) | existing | Service fallback `"Level {N}"` bila tabel kosong |

**Missing dependencies:** None blocking. Tidak ada package baru, tidak ada migration (CLAUDE.md NO migration this phase).

## Validation Architecture

> nyquist_validation = true (config.json L15). Phase ini = refactor display string; formal test/UAT defer ke Phase 344. Validasi 343 = sampling grep + spot-render (cheap, deterministik).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (existing, `dotnet test`) — TAPI test penuh = Phase 344. Phase 343 pakai build + grep + spot-render |
| Config file | existing test project (lihat `*.Tests`) |
| Quick run command | `dotnet build` (compile-time verify @inject resolve di semua view/partial) |
| Full suite command | `dotnet test` (regression existing; Phase 344 tambah TEST-01..06) |

### Phase Requirements → Validation Map
| Req ID | Behavior | Validation Type | Automated Command | Exists? |
|--------|----------|-----------------|-------------------|---------|
| ORG-INTEG-01 | View pakai `@OrgLabels.GetLabel(N)`, no hardcode display tersisa di REPLACE targets | grep residual + build | (lihat Sampling) | ✅ grep |
| ORG-INTEG-01 | @inject ada di `_ViewImports.cshtml` | grep | `grep "@inject HcPortal.Services.IOrgLabelService" Views/_ViewImports.cshtml` | ✅ |
| ORG-INTEG-02 | Controller display string audited (no actionable / minimal inject) | manual audit (done di research) + build | `dotnet build` | ✅ |
| (SC2 demo) | Rename label muncul di ≥3 page | manual spot-render (browser) | rename via /Admin/ManageOrgLevelLabels → reload CMP filter + Worker form + CDP assignment | manual (Phase 344 formal) |

### Sampling Rate (Nyquist — sample-verify swap benar app-wide)
- **Per task commit (cheap):** `dotnet build` (harus 0 error — membuktikan setiap `@OrgLabels` resolve, termasuk di partial).
- **Per area/wave merge (grep residual):** Untuk tiap file di REPLACE table, grep bahwa visible-text display literal sudah hilang. Contoh pola residual check (PowerShell-friendly via Grep tool):
  - Pattern target hilang: `>Bagian<`, `>Unit<`, `>Semua Bagian<`, `>Semua Unit<`, `-- Pilih Bagian --`, `-- Pilih Unit --`, `<th>Bagian</th>` di file REPLACE.
  - **Whitelist (boleh tetap ada):** id `filterBagian`/`silabusBagian`, JS var, property `.Bagian`, import-doc `<td>Bagian</td>`, subtitle combined SKIP, Excel header controller. Reviewer cocokkan ke SKIP table.
- **Phase gate:** `dotnet build` green + grep residual 0 (di file REPLACE) + spot-render manual 1 page per area (CMP/CDP/ProtonData/Admin) tampil label benar + fallback `"Level N"` tidak muncul (artinya seed ada).

### Wave 0 Gaps
- None untuk Phase 343 (no new test file — formal xUnit/Playwright = Phase 344 deliverable per CONTEXT deferred + TEST-01..06).
- Catatan: Phase 344 akan butuh `tests/` untuk `OrgLabelService.GetLabel` happy+fallback (TEST-01) + Playwright label baru kelihatan 2+ page (TEST-06). BUKAN scope 343.

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua UI + komunikasi. Label dinamis = output `GetLabel` (sudah Indonesian default). Output research ini sudah Bahasa Indonesia.
- **NO migration this phase** — tidak ada `dotnet ef migrations add`. Phase 343 tidak menyentuh schema/DB. Bila notif IT, flag migration = NONE.
- **Workflow Lokal→Dev→Prod:** verify lokal (`dotnet build` + `dotnet run` cek `http://localhost:5277`) sebelum commit/push. Jangan edit di server Dev/Prod.
- **Seed Workflow:** Phase 343 tidak butuh seed temporary (label default sudah ter-seed permanent Phase 340). Bila SC2 demo butuh rename label untuk test → itu mutasi via UI (bukan seed insert), restore label "Bagian" setelah demo.

## Sources

### Primary (HIGH confidence)
- `Services/IOrgLabelService.cs` (namespace `HcPortal.Services`, `GetLabel(int)` signature) — VERIFIED via Read
- `Services/OrgLabelService.cs` L31-35 (fallback `"Level {N}"`, cache key `OrgLabels:All`) — VERIFIED
- `Program.cs` L17 (`AddMemoryCache`), L65 (`AddScoped<IOrgLabelService,OrgLabelService>`) — VERIFIED via Grep
- `Data/SeedData.cs` L121-123 (Level 0=Bagian/1=Unit/2=Sub-unit) — VERIFIED via Grep
- `Views/_ViewImports.cshtml` (current content, no custom @inject) — VERIFIED via Read
- Occurrence audit: full grep `Bagian|Sub-unit` + targeted `>Unit<`/label/th/option grep across `Views/**/*.cshtml` + `Controllers/**/*.cs` — VERIFIED via Grep, classified per-occurrence via Read context
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.8 (L453-476), §5 (L480-491), §6 (L499-500) — CITED via Read
- `.planning/phases/343-integrasi-app-wide/343-CONTEXT.md` (D-01..D-04) — CITED
- `.planning/milestones/v21.0-REQUIREMENTS.md` (ORG-INTEG-01/02), `v21.0-ROADMAP.md` (SC) — CITED

### Secondary (MEDIUM confidence)
- ASP.NET Core `_ViewImports.cshtml` hierarchical inheritance to partials — training knowledge, verifiable via `dotnet build` (A1)

### Tertiary (LOW confidence)
- None — semua claim verified langsung di codebase.

## Metadata

**Confidence breakdown:**
- Mekanisme @inject + service: HIGH — verified Program.cs/service/seed/_ViewImports langsung.
- Occurrence audit (REPLACE list): HIGH — setiap REPLACE row dari grep + context, kolom tier mengikuti seed mapping.
- AMBIGUOUS verdicts: MEDIUM — keputusan ganti/skip (CONTEXT D-04 discretion), bukan kebenaran teknis; planner boleh override.
- Controller ORG-INTEG-02: HIGH (audit fakta) / MEDIUM (interpretasi pemenuhan REQ — lihat Open Q1).

**Research date:** 2026-06-03
**Valid until:** ~30 hari (codebase stable; valid selama view tidak di-refactor besar sebelum Phase 343 dieksekusi). Re-grep cepat bila ada commit menyentuh Views/ antara research dan eksekusi.
