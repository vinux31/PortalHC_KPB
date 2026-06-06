# Phase 343: Integrasi App-wide (label tier dynamic) - Pattern Map

**Mapped:** 2026-06-03
**Files analyzed:** 1 global view-imports + ~25 view file (REPLACE) + 0 controller (audit-only) = sumber otoritatif RESEARCH.md REPLACE table
**Analogs found:** 25 / 25 (semua REPLACE target punya analog konkret in-codebase; @inject punya analog persis di `_Layout.cshtml`)

> Phase 343 = **refactor display-string**, bukan fitur baru. Tidak ada file BARU yang dibuat — hanya 1 baris `@inject` ditambah ke `_ViewImports.cshtml` + swap teks visible di view existing. Tabel REPLACE di `343-RESEARCH.md` (L118-209) adalah sumber file:line | snippet | tier | verdict yang otoritatif; PATTERNS.md ini memetakan tiap kelompok file ke analog + excerpt BEFORE/AFTER konkret.

---

## File Classification

Dikelompokkan per role/area dari REPLACE table RESEARCH.md (L118-209). Semua "Modified", tidak ada "New".

| File yang Dimodifikasi | Role | Data Flow | Closest Analog | Match Quality |
|------------------------|------|-----------|----------------|---------------|
| `Views/_ViewImports.cshtml` | global view-imports | DI / SSR | `Views/Shared/_Layout.cshtml:5-8` (@inject fully-qualified existing) | exact |
| `Views/CMP/AnalyticsDashboard.cshtml` | CMP view (filter + table) | request-response (server-render) | self-analog (BEFORE = baris existing) | exact |
| `Views/CMP/RecordsTeam.cshtml` | CMP view (filter + table) | request-response | `Views/CMP/AnalyticsDashboard.cshtml` (sibling CMP filter) | exact |
| `Views/CDP/CertificationManagement.cshtml` | CDP view (filter) | request-response | CMP filter pattern | exact |
| `Views/CDP/CoachingProton.cshtml` | CDP view (dropdown) | request-response | CDP filter sibling | exact |
| `Views/CDP/HistoriProton.cshtml` | CDP view (filter + table) | request-response | CDP filter sibling | exact |
| `Views/CDP/HistoriProtonDetail.cshtml` | CDP view (definition-list) | request-response | `Views/Admin/WorkerDetail.cshtml` (detail field label) | role-match |
| `Views/CDP/PlanIdp.cshtml` | CDP view (form) | request-response | `Views/ProtonData/Index.cshtml` (form placeholder) | exact |
| `Views/CDP/Shared/_CoachingProtonPartial.cshtml` | partial view (form) | request-response | parent CDP view (inherits @inject) | exact |
| `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` | partial view (table) | request-response | parent CDP view (inherits @inject) | exact |
| `Views/ProtonData/Index.cshtml` | ProtonData view (form) | request-response | `Views/CDP/PlanIdp.cshtml` (form placeholder sibling) | exact |
| `Views/ProtonData/Override.cshtml` | ProtonData view (form) | request-response | `Views/ProtonData/Index.cshtml` (sibling) | exact |
| `Views/Admin/CoachCoacheeMapping.cshtml` | Admin view (table + form, combined-phrase) | request-response | Pattern 2 combined-text | exact |
| `Views/Admin/CreateWorker.cshtml` | Admin view (form placeholder) | request-response | `Views/Admin/EditWorker.cshtml` (twin) | exact |
| `Views/Admin/EditWorker.cshtml` | Admin view (form placeholder) | request-response | `Views/Admin/CreateWorker.cshtml` (twin) | exact |
| `Views/Admin/ManageWorkers.cshtml` | Admin view (filter + table) | request-response | CMP filter pattern | exact |
| `Views/Admin/WorkerDetail.cshtml` | Admin view (detail field) | request-response | `<td class="text-muted">` field-label pattern | exact |
| `Views/Admin/RenewalCertificate.cshtml` | Admin view (filter) | request-response | CMP filter pattern | exact |
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` | partial view (filter + table) | request-response | parent Admin view (inherits @inject) | exact |
| `Views/Admin/EditAssessment.cshtml` | Admin view (table + dropdown) | request-response | `Views/Admin/CreateAssessment.cshtml` (twin) | exact |
| `Views/Admin/CreateAssessment.cshtml` | Admin view (dropdown) | request-response | `Views/Admin/EditAssessment.cshtml` (twin) | exact |
| `Views/Admin/CpdpUpload.cshtml` | Admin view (selector) | request-response | `Views/Admin/KkjUpload.cshtml` (twin) | exact |
| `Views/Admin/KkjUpload.cshtml` | Admin view (selector) | request-response | `Views/Admin/CpdpUpload.cshtml` (twin) | exact |
| `Views/Account/Profile.cshtml` | Account view (profile field) | request-response | `<div>` field-label pattern | exact |
| `Views/Account/Settings.cshtml` | Account view (settings field) | request-response | `Views/Account/Profile.cshtml` (sibling) | exact |
| `Views/Admin/CpdpFiles.cshtml`, `Views/Admin/KkjMatrix.cshtml` | Admin view (button) | request-response | Pattern 2 combined-text | **AMBIGUOUS** (planner discretion — lihat §AMBIGUOUS) |

**Controller (ORG-INTEG-02):** 0 file REPLACE (audit-only). Semua match controller = Excel header / audit body / validation / property interpolation = SKIP per RESEARCH.md L244-262. Lihat §"Controller — No-Actionable Audit".

---

## Pattern Assignments

### `Views/_ViewImports.cshtml` (global view-imports, DI/SSR) — FOUNDATION

**Analog:** `Views/Shared/_Layout.cshtml:5-8` (sudah ada 4 `@inject` fully-qualified — pola identik yang dibutuhkan)

**Analog excerpt** (`Views/Shared/_Layout.cshtml:5-8` — konvensi @inject existing yang HARUS diikuti):
```razor
@inject UserManager<ApplicationUser> UserManager
@inject SignInManager<ApplicationUser> SignInManager
@inject Microsoft.Extensions.Caching.Memory.IMemoryCache _maintenanceCache
@inject HcPortal.Data.ApplicationDbContext _maintenanceDb
```
> Catatan: baris ke-4 `@inject HcPortal.Data.ApplicationDbContext _maintenanceDb` = pola fully-qualified-namespace persis seperti yang dibutuhkan Phase 343 (`HcPortal.Services.IOrgLabelService`). Tidak perlu tambah `@using HcPortal.Services` — cukup fully-qualified 1 baris.

**BEFORE** (`Views/_ViewImports.cshtml:1-5` — VERIFIED, belum ada @inject custom):
```razor
@using HcPortal
@using HcPortal.Models
@using HcPortal.Models.Guide
@using System.Text.Json
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

**AFTER** (tambah 1 baris di akhir — D-01, spec §5 L490):
```razor
@using HcPortal
@using HcPortal.Models
@using HcPortal.Models.Guide
@using System.Text.Json
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@inject HcPortal.Services.IOrgLabelService OrgLabels
```

**Service call-site convention** (`Services/OrgLabelService.cs:31-35` + `Services/IOrgLabelService.cs:12` — VERIFIED signature):
```csharp
// IOrgLabelService.cs:12 — signature
string GetLabel(int level);

// OrgLabelService.cs:31-35 — return semantics: dict lookup, fallback "Level {N}"
public string GetLabel(int level)
{
    var dict = GetAll();
    return dict.TryGetValue(level, out var label) ? label : $"Level {level}";
}
```
> Return semantics: `GetLabel(0)`="Bagian", `GetLabel(1)`="Unit", `GetLabel(2)`="Sub-unit" (seed `Data/SeedData.cs:121-123`). Fallback `"Level {N}"` bila tabel kosong (aman, tidak throw). Service Scoped + IMemoryCache (Program.cs L17 + L65) — 1 inject global murah.

> **Inheritance ke partial (Pitfall 5, A1):** `Views/_ViewImports.cshtml` berlaku hierarkis ke SEMUA view + partial di bawah `Views/` (termasuk `Views/CDP/Shared/_*.cshtml`, `Views/Admin/Shared/_*.cshtml`). `@OrgLabels` available di partial TANPA @inject ulang. Verify via `dotnet build` (compile-time error bila resolve gagal).

---

### `Views/CMP/AnalyticsDashboard.cshtml` (CMP view, filter + table) — CANONICAL FILTER PATTERN

**Analog:** self (BEFORE = baris existing yang VERIFIED). Ini adalah analog kanonik untuk SEMUA filter-label + dropdown REPLACE (CMP/CDP/Admin filter siblings copy dari sini).

**BEFORE** (`Views/CMP/AnalyticsDashboard.cshtml:84-97` — VERIFIED via Read):
```razor
<label for="filterBagian" class="form-label form-label-sm mb-1">Bagian</label>
<select id="filterBagian" class="form-select form-select-sm">
    <option value="">Semua Bagian</option>
    @foreach (var s in Model.Sections)
    {
        <option value="@s">@s</option>
    }
</select>
...
<label for="filterUnit" class="form-label form-label-sm mb-1">Unit</label>
<select id="filterUnit" class="form-select form-select-sm" disabled>
    <option value="">Semua Unit</option>
</select>
```

**AFTER** (visible-text-only swap — Pattern 1: `id`/`for` tetap literal):
```razor
<label for="filterBagian" class="form-label form-label-sm mb-1">@OrgLabels.GetLabel(0)</label>
<select id="filterBagian" class="form-select form-select-sm">
    <option value="">Semua @OrgLabels.GetLabel(0)</option>
    @foreach (var s in Model.Sections)
    {
        <option value="@s">@s</option>
    }
</select>
...
<label for="filterUnit" class="form-label form-label-sm mb-1">@OrgLabels.GetLabel(1)</label>
<select id="filterUnit" class="form-select form-select-sm" disabled>
    <option value="">Semua @OrgLabels.GetLabel(1)</option>
</select>
```

**REPLACE rows untuk file ini** (RESEARCH.md L122-127):
- L84 `<label for="filterBagian">Bagian</label>` → `>@OrgLabels.GetLabel(0)<` (tier 0)
- L86 `<option value="">Semua Bagian</option>` → `Semua @OrgLabels.GetLabel(0)` (tier 0)
- L94 `<label for="filterUnit">Unit</label>` → `>@OrgLabels.GetLabel(1)<` (tier 1)
- L96 `<option value="">Semua Unit</option>` → `Semua @OrgLabels.GetLabel(1)` (tier 1)
- L543 `<h6>Perbandingan Antar Kelompok (Bagian)</h6>` → `(@OrgLabels.GetLabel(0))` (tier 0)
- L548 `<th>Bagian</th>` → `<th>@OrgLabels.GetLabel(0)</th>` (tier 0)

> **JANGAN sentuh:** `id="filterBagian"`, `for="filterBagian"`, `id="filterUnit"`, `@s`/`@foreach` loop var, `Model.Sections`. HANYA teks `>...<` di antara tag.

---

### `Views/CDP/PlanIdp.cshtml` (CDP view, form placeholder) — CANONICAL PLACEHOLDER PATTERN

**Analog:** self (BEFORE VERIFIED). Analog kanonik untuk SEMUA form placeholder `-- Pilih X --` (ProtonData/Index, ProtonData/Override, CreateWorker, EditWorker, CpdpUpload, KkjUpload copy pola ini).

**BEFORE** (`Views/CDP/PlanIdp.cshtml:72-95` — VERIFIED via Read):
```razor
<label class="form-label fw-semibold">Bagian</label>
@if (isCoachee && !string.IsNullOrEmpty(coacheeBagian))
{
    <input type="hidden" name="bagian" value="@coacheeBagian" />
    <input type="text" class="form-control bg-light" value="@coacheeBagian" disabled />
}
...
else
{
    <select class="form-select" id="silabusBagian" name="bagian">
        <option value="">-- Pilih Bagian --</option>
    </select>
}
...
<label class="form-label fw-semibold">Unit</label>
<select class="form-select" id="silabusUnit" name="unit"
        disabled="@(string.IsNullOrEmpty(selectedBagian) ? "disabled" : null)">
    <option value="">-- Pilih Unit --</option>
</select>
```

**AFTER** (visible-text-only — Pattern 2 combined-text untuk placeholder):
```razor
<label class="form-label fw-semibold">@OrgLabels.GetLabel(0)</label>
@if (isCoachee && !string.IsNullOrEmpty(coacheeBagian))
{
    <input type="hidden" name="bagian" value="@coacheeBagian" />
    <input type="text" class="form-control bg-light" value="@coacheeBagian" disabled />
}
...
else
{
    <select class="form-select" id="silabusBagian" name="bagian">
        <option value="">-- Pilih @OrgLabels.GetLabel(0) --</option>
    </select>
}
...
<label class="form-label fw-semibold">@OrgLabels.GetLabel(1)</label>
<select class="form-select" id="silabusUnit" name="unit"
        disabled="@(string.IsNullOrEmpty(selectedBagian) ? "disabled" : null)">
    <option value="">-- Pilih @OrgLabels.GetLabel(1) --</option>
</select>
```

**REPLACE rows untuk file ini** (RESEARCH.md L150-153):
- L72 `<label>Bagian</label>` → `@OrgLabels.GetLabel(0)` (tier 0) — *catatan: RESEARCH cites L72 (actual) / table says L72; visible label di col-md-3*
- L86 `<option>-- Pilih Bagian --</option>` → `-- Pilih @OrgLabels.GetLabel(0) --` (tier 0)
- L91 `<label>Unit</label>` → `@OrgLabels.GetLabel(1)` (tier 1)
- L94 `<option>-- Pilih Unit --</option>` → `-- Pilih @OrgLabels.GetLabel(1) --` (tier 1)

> **JANGAN sentuh:** `id="silabusBagian"`, `id="silabusUnit"`, `name="bagian"`, `name="unit"`, `@coacheeBagian`, `selectedBagian` (C# var di `disabled=` expression), `value="@coacheeBagian"`.

---

### `Views/Admin/CoachCoacheeMapping.cshtml` (Admin view, combined-phrase "X Penugasan") — CANONICAL COMBINED-TEXT PATTERN

**Analog:** Pattern 2 (combined-text concatenation, RESEARCH.md L311-318).

**BEFORE** (`Views/Admin/CoachCoacheeMapping.cshtml:235-236` — VERIFIED via Read):
```razor
<th>Bagian Penugasan</th>
<th>Unit Penugasan</th>
```

**AFTER** (label dinamis + suffix literal "Penugasan"):
```razor
<th>@OrgLabels.GetLabel(0) Penugasan</th>
<th>@OrgLabels.GetLabel(1) Penugasan</th>
```

**REPLACE rows untuk file ini** (RESEARCH.md L170-177):
- L235 `<th>Bagian Penugasan</th>` → `@OrgLabels.GetLabel(0) Penugasan` (tier 0)
- L236 `<th>Unit Penugasan</th>` → `@OrgLabels.GetLabel(1) Penugasan` (tier 1)
- L435 `<label>Bagian Penugasan</label>` → `@OrgLabels.GetLabel(0) Penugasan` (tier 0)
- L437 `<option>— Pilih Bagian —</option>` → `— Pilih @OrgLabels.GetLabel(0) —` (tier 0) — *catatan: em-dash `—` literal, beda dari `--` di ProtonData*
- L445 `<label>Unit Penugasan</label>` → `@OrgLabels.GetLabel(1) Penugasan` (tier 1)
- L503 `<label>Bagian Penugasan</label>` → `@OrgLabels.GetLabel(0) Penugasan` (tier 0, modal ke-2)
- L505 `<option>— Pilih Bagian —</option>` → `— Pilih @OrgLabels.GetLabel(0) —` (tier 0, modal ke-2)
- L513 `<label>Unit Penugasan</label>` → `@OrgLabels.GetLabel(1) Penugasan` (tier 1, modal ke-2)

> **JANGAN sentuh:** suffix "Penugasan" literal (bukan tier label), em-dash, `name=`/`id=` select, `@foreach` data render.

---

### `Views/Admin/WorkerDetail.cshtml` + `Views/Account/Profile.cshtml` + `Views/CDP/HistoriProtonDetail.cshtml` (detail/definition-list field label)

**Analog:** field-label pattern (`<td class="text-muted">`, `<div>`, `<dt>`/`<small>` yang berisi label, BUKAN nilai data).

**BEFORE** (`Views/Admin/WorkerDetail.cshtml:89,102`):
```razor
<td class="text-muted">Bagian</td>
<td class="text-muted">Unit</td>
```

**AFTER:**
```razor
<td class="text-muted">@OrgLabels.GetLabel(0)</td>
<td class="text-muted">@OrgLabels.GetLabel(1)</td>
```

**REPLACE rows:**
- `WorkerDetail.cshtml:89` `<td>Bagian</td>` → tier 0, :102 `<td>Unit</td>` → tier 1
- `Profile.cshtml:78` `<div>Bagian</div>` → tier 0, :84 `<div>Unit</div>` → tier 1
- `HistoriProtonDetail.cshtml:36` `<dt>Unit</dt>` → tier 1, :84 `<small>Unit</small>` → tier 1
- `Settings.cshtml:96` `<label>Bagian</label>` → tier 0, :114 `<label>Unit</label>` → tier 1

> **KRITIS — JANGAN sentuh sel NILAI di sebelahnya:** sel kanan (mis. `<td>@w.Section</td>`, `<div>@Model.Unit</div>`) adalah DATA user (property access), bukan label. Hanya sel LABEL (kiri/judul) yang di-swap. Lihat SKIP `_PSign.cshtml:42` (`<div>@Model.Unit</div>` = render nilai, BUKAN label).

---

### Sibling views (copy pola dari analog kanonik di atas)

Files berikut mengikuti SALAH SATU dari 3 pola kanonik di atas (filter / placeholder / detail-field). REPLACE rows lengkap di RESEARCH.md L118-209:

| File | Pola yang diikuti | REPLACE rows (RESEARCH.md) |
|------|-------------------|----------------------------|
| `Views/CMP/RecordsTeam.cshtml` | filter (AnalyticsDashboard) | L20/24/41 (Bagian), L50/52 (Unit), L135 `<th class="p-3">Bagian</th>` (0), L136 `<th class="p-3">Unit</th>` (1) |
| `Views/CDP/CertificationManagement.cshtml` | filter | L89/97/105 (Bagian 0), L110/112 (Unit 1) |
| `Views/CDP/CoachingProton.cshtml` | filter dropdown | L100 (Bagian 0), L120 (Unit 1) — **L95 Razor comment = SKIP** |
| `Views/CDP/HistoriProton.cshtml` | filter + table | L30 (label Bagian 0), L34/51 (option Bagian 0), L60 (label Unit 1), L62 (option Unit 1), L109 `<th>Unit</th>` (1) |
| `Views/CDP/Shared/_CoachingProtonPartial.cshtml` | partial filter (inherits @inject) | L29 `<label>Unit</label>` (1), L35 `<option>Semua Unit</option>` (1) |
| `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml` | partial table (inherits @inject) | L19 `<th>Bagian</th>` (0), L20 `<th>Unit</th>` (1) |
| `Views/ProtonData/Index.cshtml` | placeholder (PlanIdp) | L79/81 (form Bagian 0), L85/87 (form Unit 1), L230/232 (guidance Bagian 0), L236/238 (guidance Unit 1) |
| `Views/ProtonData/Override.cshtml` | placeholder | L29/31 (Bagian 0), L35/37 (Unit 1) |
| `Views/Admin/CreateWorker.cshtml` | placeholder | L116 `-- Pilih Bagian --` (0), L122 `-- Pilih Unit --` (1) |
| `Views/Admin/EditWorker.cshtml` | placeholder | L121 (Bagian 0), L127 (Unit 1) |
| `Views/Admin/ManageWorkers.cshtml` | filter + table | L132/134 (Bagian 0), L152/154 (Unit 1), L226 `<th>Bagian</th>` (0), L227 `<th>Unit</th>` (1) |
| `Views/Admin/RenewalCertificate.cshtml` | filter | L55/57 (Bagian 0), L65/67 (Unit 1) |
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` | partial filter + table (inherits @inject) | L37/45 (Bagian 0), L87/94 (Unit 1), L194 `<th>Unit</th>` (1) |
| `Views/Admin/EditAssessment.cshtml` | table + dropdown | L522 `<th>Bagian</th>` (0), L598 `Semua Bagian` (0), L606 `Lainnya (Tanpa Bagian)` (0, **AMBIGUOUS** → default REPLACE) |
| `Views/Admin/CreateAssessment.cshtml` | dropdown | L264 `Semua Bagian` (0), L272 `Lainnya (Tanpa Bagian)` (0, **AMBIGUOUS** → default REPLACE) |
| `Views/Admin/CpdpUpload.cshtml` | selector | L39 label `Pilih Bagian` (0), L42 `-- Pilih Bagian --` (0) |
| `Views/Admin/KkjUpload.cshtml` | selector (twin CpdpUpload) | L39 (0), L42 (0) |
| `Views/Account/Profile.cshtml` | detail field | L78 (Bagian 0), L84 (Unit 1) |
| `Views/Account/Settings.cshtml` | detail/settings field | L96 (Bagian 0), L114 (Unit 1) |

> **Catatan `<option>` tanpa atribut value** (mis. RecordsTeam `<option>Semua Bagian</option>`): swap teks body → `<option>Semua @OrgLabels.GetLabel(0)</option>`. Beberapa option di RESEARCH ditulis tanpa `value=""` — periksa baris aktual saat edit, struktur atribut tetap.

---

## Shared Patterns

### Pattern A: Visible-text-only swap (Pattern 1 — id/attr/var tetap literal)
**Source:** RESEARCH.md §Pattern 1 (L297-309) + Pitfall 1 (L352-356)
**Apply to:** SEMUA REPLACE row
**Rule:** Ganti HANYA teks di antara tag (`>Bagian<` → `>@OrgLabels.GetLabel(0)<`). JANGAN sentuh `id=`, `for=`, `name=`, JS var, property access.
```razor
@* BENAR — id literal, hanya visible text dinamis *@
<label for="filterBagian" class="form-label">@OrgLabels.GetLabel(0)</label>
<select id="filterBagian">
    <option value="">Semua @OrgLabels.GetLabel(0)</option>
</select>
@* SALAH — JANGAN: id="filter@OrgLabels..." atau name="@OrgLabels..." *@
```

### Pattern B: Combined-text concatenation (Pattern 2)
**Source:** RESEARCH.md §Pattern 2 (L311-318)
**Apply to:** "X Penugasan" (CoachCoacheeMapping), "Semua X", "-- Pilih X --", "— Pilih X —", "Lainnya (Tanpa X)"
```razor
<th>@OrgLabels.GetLabel(0) Penugasan</th>
<option value="">Semua @OrgLabels.GetLabel(0)</option>
<option value="">-- Pilih @OrgLabels.GetLabel(0) --</option>
```

### Pattern C: Tier mapping (Pitfall 2)
**Source:** `Data/SeedData.cs:121-123` + RESEARCH.md L358-360
**Apply to:** SEMUA swap — kolom `tier` di REPLACE table sudah pre-isi.
- `GetLabel(0)` = "Bagian" (tier 0)
- `GetLabel(1)` = "Unit" (tier 1)
- `GetLabel(2)` = "Sub-unit" (tier 2) — *tidak ada REPLACE row tier 2 di audit aktual; semua REPLACE = tier 0/1*

### Pattern D: Partial @inject inheritance (Pitfall 5, A1)
**Source:** `Views/_ViewImports.cshtml` (hierarchical) + RESEARCH.md L211, L370-373
**Apply to:** `Views/CDP/Shared/_CoachingProtonPartial.cshtml`, `Views/CDP/Shared/_CertificationManagementTablePartial.cshtml`, `Views/Admin/Shared/_TrainingRecordsTab.cshtml`
**Rule:** TIDAK perlu `@inject` ulang di partial — `_ViewImports.cshtml` di `Views/` berlaku ke semua subfolder. `@OrgLabels` available langsung. Verify via `dotnet build`.

### Anti-Pattern (yang DIHINDARI D-01): per-view @inject repetitif
**Source:** `Views/Account/Login.cshtml:1`, `Views/Admin/CreateWorker.cshtml:2` (existing per-view `@inject ...IConfiguration Config`)
**JANGAN:** tambah `@inject HcPortal.Services.IOrgLabelService OrgLabels` per-view di ~25 file. D-01 mandat 1 baris global di `_ViewImports.cshtml`. (Per-view IConfiguration di Login/CreateWorker adalah pola lama untuk service spesifik-view; OrgLabels app-wide → global.)

---

## PERBEDAAN PATH KRITIS: Phase 343 (@inject server-side) vs Phase 342 (JS-fetch client-side)

> Planner WAJIB tidak mencampur dua jalur resolve label ini. Keduanya membaca dari sumber data yang sama (`OrganizationLevelLabels`) tapi via mekanisme berbeda.

| Aspek | **Phase 343 (THIS PHASE)** | **Phase 342 (DONE — JANGAN sentuh)** |
|-------|----------------------------|--------------------------------------|
| Mekanisme | Server-side `@inject IOrgLabelService` + `@OrgLabels.GetLabel(N)` di Razor | Client-side `ajaxGet('GetLevelLabels')` di `wwwroot/js/orgTree.js` |
| Resolve point | Saat server-render view (SSR) | Saat browser fetch JSON, render JS DOM |
| Call site | `@OrgLabels.GetLabel(0)` di `.cshtml` | `getLabelForLevel(level)` → `labelMap[level]` di `.js` |
| Source endpoint | (tidak ada — direct service call) | `GET /Admin/GetLevelLabels` (Phase 340/341 endpoint) |
| Fallback | `"Level {N}"` di `OrgLabelService.GetLabel` (server) | `` `Level ${level}` `` di `getLabelForLevel` (client) |
| Scope | ~25 view app-wide (Phase 343) | HANYA `Views/Admin/ManageOrganization.cshtml` + `orgTree.js` |

**Bukti Phase 342 JS-path** (`.planning/phases/342-manageorganization-page-fixes/342-02-PLAN.md:162-163` — referensi, BUKAN target Phase 343):
```javascript
let labelMap = null;
async function fetchLabels() { if (!labelMap) labelMap = await ajaxGet('GetLevelLabels'); }
function getLabelForLevel(level) { return (labelMap && labelMap[level]) || `Level ${level}`; }
```

**Implikasi untuk planner:**
- `Views/Admin/ManageOrganization.cshtml` = **OUT OF SCOPE** (sudah dynamic via JS Phase 342). Modal title "Tambah Unit" L159 sudah JS-handled (ORG-TREE-09). JANGAN double-handle. Lihat AMBIGUOUS SKIP.
- Phase 343 TIDAK menyentuh `wwwroot/js/orgTree.js`, TIDAK menggunakan endpoint `GetLevelLabels`, TIDAK menambah JS apa pun. Murni SSR `@inject`.

---

## AMBIGUOUS (keputusan eksplisit per D-04 — planner discretion, dicatat di audit SC1)

| File:line | snippet | tier | Rekomendasi | Reasoning |
|-----------|---------|------|-------------|-----------|
| `Views/Admin/CpdpFiles.cshtml:64,87` | `Tambah Bagian` / `Hapus Bagian` (button) | 0 | **REPLACE button text, SKIP JS func/toast** | User-facing button referensi tier root. JS `addBagian()`/`deleteBagian()` + toast `'Bagian berhasil dihapus'` (L172/223/241) = SKIP (identifier/literal). Risk low-medium; planner boleh defer ke stretch (D-02 minimize over-replace) |
| `Views/Admin/KkjMatrix.cshtml:64,87` | `Tambah Bagian` / `Hapus Bagian` (button) | 0 | **REPLACE button text, SKIP JS func/toast** | Twin CpdpFiles, sama persis |
| `Views/Admin/EditAssessment.cshtml:606` | `<option>Lainnya (Tanpa Bagian)</option>` | 0 | **REPLACE (default)** → `Lainnya (Tanpa @OrgLabels.GetLabel(0))` | Konsistensi; low value, planner boleh SKIP |
| `Views/Admin/CreateAssessment.cshtml:272` | `<option>Lainnya (Tanpa Bagian)</option>` | 0 | **REPLACE (default)** | Sama EditAssessment:606 |

---

## SKIP (noise — JANGAN ganti; reviewer cocokkan whitelist)

Ringkasan kategori dari RESEARCH.md L228-242 + AMBIGUOUS-SKIP (subtitle circular). Planner harus pastikan grep residual TIDAK menandai ini sebagai miss:

| Kategori | Contoh file:line | Reason |
|----------|------------------|--------|
| Subtitle page Label CRUD itu sendiri (circular) | `Index.cshtml:61`, `ManageOrgLevelLabels.cshtml:17`, `ManageOrganization.cshtml:115` | Pitfall 4 — circular/tautologis bila dinamis |
| ManageOrganization page (Phase 342) | `ManageOrganization.cshtml:159` modal title | Out of scope; JS-handled |
| Import/doc field-description table | `ProtonData/ImportSilabus.cshtml:205-206`, `Admin/ImportWorkers.cshtml:205-206` | Deskripsi kolom Excel = literal (header file user = "Bagian") |
| Element id / `for=` attr | `id="filterBagian"`, `id="silabusBagian"`, `id="overrideBagian"` | JS bergantung id literal (Pitfall 1) |
| JS var / function name | `selectedBagian`, `unitsByBagian`, `onBagianChange()`, `addBagian()` | Kode JS — ganti = break |
| ViewBag/property access | `ViewBag.SelectedBagianId`, `@row.Bagian`, `@w.Unit`, `@Model.Unit`, `bagian.Name` | Data/property access — schema unchanged |
| Unit NAME (data) | `RFCC LPG Treating...`, `@bagian.Name` | Data, bukan tier label |
| Razor comment | `@* Bagian dropdown ... *@` (CoachingProton:95) | Tidak dirender |
| Render nilai unit (bukan label) | `_PSign.cshtml:42` `<div>@Model.Unit</div>` | Render NILAI unit user |

---

## Controller — No-Actionable Audit (ORG-INTEG-02)

**Verdict:** 0 file REPLACE wajib. Semua match controller = Excel/CSV export header / audit body / validation msg / property interpolation = SKIP per spec §4.8. Sumber: RESEARCH.md L244-262.

| Controller | Kategori match | Verdict |
|------------|----------------|---------|
| `CDPController.cs` (3681/4015/4128/4205), `CMPController.cs` (3112/3607/3880), `WorkerController.cs` (174/850/878), `ProtonDataController.cs` (126/759/805/891), `CoachMappingController.cs` (1232) | Excel/CSV export header + string interp key | SKIP — header file deterministik (Pitfall 3) |
| `WorkerController.cs` (227/363) | ModelState validation error `$"Bagian '{model.Section}'..."` | SKIP-recommended (D-02 minimize risk) |
| `DocumentAdminController.cs` (151/165/353/357/364/498/512/570) | audit log + ILogger body | SKIP (spec §4.8 stabil debug) |
| `DocumentAdminController.cs` (263) | `Name = "Bagian Baru"` default unit name | SKIP (data value) |
| `OrgLabelController.cs:41` | code comment | SKIP |

**Satu-satunya kandidat REPLACE bila planner ingin bukti konkret ORG-INTEG-02** (Open Q1, RESEARCH.md L257/262/422-425):
- `DocumentAdminController.cs` L106/287/299/315/380 — `TempData["Error"]`/`Json(message=...)` "Bagian tidak ditemukan." → inject `_orgLabels` + `$"{_orgLabels.GetLabel(0)} tidak ditemukan."`
- **Default rekomendasi: SKIP** (D-02 minimize risk; dokumentasikan audit sebagai pemenuhan ORG-INTEG-02). Stretch: 1 inject DocumentAdmin bila planner mau bukti eksplisit.
- Pola ctor-inject (bila dipilih): mirip `Services/OrgLabelService.cs` ctor — `private readonly IOrgLabelService _orgLabels;` + assign di constructor (Program.cs L65 sudah register, DI auto-resolve).

---

## No Analog Found

Tidak ada. Semua REPLACE target punya analog konkret in-codebase:
- @inject → `Views/Shared/_Layout.cshtml:5-8` (pola fully-qualified existing)
- filter/placeholder/detail swap → self/sibling view (BEFORE = baris existing)
- combined-text → Pattern 2 (sudah ada konvensi `"X Penugasan"`)

Tidak perlu fallback ke RESEARCH.md generic pattern — RESEARCH.md SUDAH menyediakan per-occurrence verdict + BEFORE snippet.

---

## Metadata

**Analog search scope:** `Views/**/*.cshtml` (REPLACE table), `Views/Shared/_Layout.cshtml` (@inject analog), `Services/OrgLabelService.cs` + `IOrgLabelService.cs` (call-site signature), `wwwroot/js/orgTree.js` via 342-02-PLAN (path differentiation), `Controllers/**` (ORG-INTEG-02 audit — via RESEARCH.md)
**Files scanned (Read VERIFIED):** OrgLabelService.cs, IOrgLabelService.cs, _ViewImports.cshtml, _Layout.cshtml, CMP/AnalyticsDashboard.cshtml (L80-101), CDP/PlanIdp.cshtml (L68-99), Admin/CoachCoacheeMapping.cshtml (L230-243), 342-02-PLAN.md (L115-169)
**Pattern extraction date:** 2026-06-03
**Authoritative source for file:line REPLACE list:** `343-RESEARCH.md` L118-209 (REPLACE), L213-226 (AMBIGUOUS), L228-262 (SKIP + controller)
