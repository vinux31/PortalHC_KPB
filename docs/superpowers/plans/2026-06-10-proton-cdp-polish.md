# PROTON CDP Polish Implementation Plan (Phase 362)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tutup 6 gap UI/navigasi/role PROTON (G-01/04/05/09/10/12) dari gap-analysis, tanpa perubahan DB.

**Architecture:** Edit Razor view/partial (Dashboard content partial, Deliverable, CertificationManagement) + 1 attribute + 1 export action di CDPController. Chart fix = guard init agar nunggu Chart.js. Export/search Dashboard = tambah ke content partial + action ClosedXML (pola `ExportHistoriProton`). Navigasi = lengkapi cabang Referer.

**Tech Stack:** ASP.NET Core MVC (Razor `.cshtml`), Chart.js (CDN via `_Layout.cshtml:238`), ClosedXML (Excel), xUnit (role test reflection), Playwright (UAT live @localhost:5277).

**Verifikasi global tiap selesai:** `dotnet build` 0 error. Migration: NONE. CLAUDE.md Develop Workflow (cek lokal sebelum commit).

---

## Refinement dari investigasi kode (penting — baca dulu)

Saat baca kode aktual, 2 gap lebih kecil dari dugaan awal:
- **G-05** sudah SEBAGIAN ditangani: `Views/CDP/Deliverable.cshtml` pakai header `Referer` untuk breadcrumb (L10-17) + tombol balik (L452-464), sudah handle `CoachingProton` + `ProtonData`. Yang KURANG = cabang **Dashboard** (drill dari Dashboard → fallback ke PlanIdp). Fix = tambah cabang Dashboard.
- **G-09** beneran hardcode: `Views/CDP/CertificationManagement.cshtml` L11 breadcrumb "CDP" + L31-33 "Kembali ke CDP" → `/CDP/Index`. Fix = arahkan ke entry Admin.

**File chart/tabel Dashboard sebenarnya** = `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` (di-render Dashboard via `_CoachingProtonPartial` + di-reload AJAX oleh `FilterCoachingProton`). BUKAN `Dashboard.cshtml` (yang cuma 157 baris wrapper).

**⚠️ Konflik sesi paralel:** `CDPController.cs` sedang/baru diedit sesi v25.0 (line bergeser ~26 baris sejak awal sesi). Sebelum commit task yang sentuh `CDPController.cs` (G-04, G-12), `git pull`/cek `git status` + re-grep line. ROADMAP.md entry Phase 362 ditambah TERAKHIR setelah sesi 359 settle.

---

## File Structure

| File | Tanggung jawab | Gap |
|------|----------------|-----|
| `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml` | chart init guard + tombol Export + search box + filter JS tabel | G-01, G-04, G-10 |
| `Controllers/CDPController.cs` | action `ExportDashboardProgress` baru + ubah attribute `ExportHistoriProton` | G-04, G-12 |
| `Views/CDP/Deliverable.cshtml` | tambah cabang Dashboard di Referer breadcrumb+back | G-05 |
| `Views/CDP/CertificationManagement.cshtml` | breadcrumb+back ke Admin | G-09 |
| `tests/.../CDPControllerAuthTests.cs` (atau file test reflection existing) | xUnit role attribute G-12 | G-12 |

---

## Plan 01 — Dashboard (G-01 chart race, G-04 export, G-10 search)

### Task 1: Fix chart race (G-01)

**Files:**
- Modify: `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml:199-263`

**Konteks:** script L200-262 = IIFE yang panggil `new Chart(...)` langsung. Saat render awal Dashboard, IIFE jalan SEBELUM `_Layout.cshtml:238` (`<script src=".../chart.js">`) ter-parse → `ReferenceError: Chart is not defined`. Saat reload AJAX (`FilterCoachingProton`), Chart sudah ada. Fix harus handle DUA kasus.

- [ ] **Step 1: Bungkus init dalam guard**

Ganti pembungkus IIFE. Baris awal `<script>\n(function () {` (L199-200) dan penutup `})();` (L262) jadi pola berikut — isi `@if (...) { <text>...</text> }` di tengah DIBIARKAN sama persis:

```html
<script>
(function () {
    function initProtonCharts() {
        @if (Model.StatusData.Any(d => d > 0))
        {
            <text>
            var protonStatusLabels = @Json.Serialize(Model.StatusLabels);
            var protonStatusData = @Json.Serialize(Model.StatusData);
            var protonStatusCtx = document.getElementById('protonStatusChart');
            if (protonStatusCtx) {
                new Chart(protonStatusCtx.getContext('2d'), {
                    type: 'doughnut',
                    data: { labels: protonStatusLabels, datasets: [{ data: protonStatusData, backgroundColor: ['rgba(40, 167, 69, 0.8)','rgba(23, 162, 184, 0.8)','rgba(255, 193, 7, 0.8)','rgba(220, 53, 69, 0.8)','rgba(173, 181, 189, 0.8)'] }] },
                    options: { responsive: true, plugins: { legend: { position: 'bottom' } } }
                });
            }
            </text>
        }
        @if (Model.BottleneckLabels.Any())
        {
            <text>
            var bottleneckLabels = @Json.Serialize(Model.BottleneckLabels);
            var bottleneckValues = @Json.Serialize(Model.BottleneckValues);
            var bottleneckCtx = document.getElementById('bottleneckChart');
            if (bottleneckCtx) {
                new Chart(bottleneckCtx.getContext('2d'), {
                    type: 'bar',
                    data: { labels: bottleneckLabels, datasets: [{ label: 'Hari Pending', data: bottleneckValues, backgroundColor: 'rgba(220, 53, 69, 0.7)', borderColor: 'rgba(220, 53, 69, 1)', borderWidth: 1 }] },
                    options: { indexAxis: 'y', responsive: true, plugins: { legend: { display: false } }, scales: { x: { beginAtZero: true, title: { display: true, text: 'Jumlah Hari Pending' } } } }
                });
            }
            </text>
        }
    }
    if (typeof Chart !== 'undefined') {
        initProtonCharts();
    } else {
        window.addEventListener('load', initProtonCharts);
    }
})();
</script>
```

Logika: `initProtonCharts` simpan kode lama. Kalau `Chart` sudah ada (kasus AJAX reload) → langsung jalan. Kalau belum (kasus render awal, layout script belum parse) → tunggu event `load` (fire setelah SEMUA script termasuk `_Layout:238`).

- [ ] **Step 2: Build**

Run: `dotnet build`
Expected: 0 error.

- [ ] **Step 3: UAT Playwright — chart render tanpa error**

Jalankan app (`localhost:5277`), login admin, navigate `/CDP/Dashboard`. Cek:
- `browser_console_messages` level error → TIDAK ada `Chart is not defined`.
- `browser_evaluate` cek canvas `protonStatusChart` ke-render: `document.getElementById('protonStatusChart').width > 0` dan ada chart instance (atau visual: doughnut tampil).
Expected: 0 console error, chart tampil.

- [ ] **Step 4: Commit**

```bash
git add Views/CDP/Shared/_CoachingProtonContentPartial.cshtml
git commit -m "fix(362): chart init guard di Dashboard partial — tunggu Chart.js (G-01)"
```

---

### Task 2: Export Excel Dashboard — action controller (G-04)

**Files:**
- Modify: `Controllers/CDPController.cs` (tambah action `ExportDashboardProgress`)

**Konteks:** Dashboard tabel "Team Deliverable Progress" = `Model.CoacheeRows` (dibangun oleh logika yang sama dipakai `FilterCoachingProton` / `BuildProtonProgressSubModelAsync`). Export harus reuse builder itu + filter params, output Excel pakai ClosedXML (pola sudah ada di `ExportHistoriProton`).

- [ ] **Step 1: Re-grep anchor (file digeser sesi paralel)**

Run: `git status` lalu cek `Controllers/CDPController.cs` belum ada konflik. Grep posisi `FilterCoachingProton` + `BuildProtonProgressSubModelAsync` + `using ClosedXML`:
```
grep -n "BuildProtonProgressSubModelAsync\|FilterCoachingProton\|ClosedXML\|RolesCoachAndAbove" Controllers/CDPController.cs
```
Catat signature `BuildProtonProgressSubModelAsync` (parameter user+role) + bentuk `CoacheeRows` (field: CoacheeName, TrackType, TahunKe, ProgressPercent, Approved, Submitted, Rejected, HasFinalAssessment — sesuai `_CoachingProtonContentPartial.cshtml:117-185`).

- [ ] **Step 2: Tambah action `ExportDashboardProgress`**

Sisipkan setelah action `Dashboard` (atau dekat `FilterCoachingProton`). Sesuaikan cara ambil data dengan builder yang dipakai `FilterCoachingProton` (samakan filter: section/unit/category/track). Pola ClosedXML ikut `ExportHistoriProton`:

```csharp
[HttpGet]
[Authorize(Roles = UserRoles.RolesCoachAndAbove)]
public async Task<IActionResult> ExportDashboardProgress(string? section, string? unit, string? category, string? track)
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();
    var roles = await _userManager.GetRolesAsync(user);
    var userRole = roles.FirstOrDefault() ?? "";

    // Reuse builder yang sama dipakai Dashboard/FilterCoachingProton.
    // CATATAN EKSEKUSI: panggil method builder yang menghasilkan ProtonProgressData (CoacheeRows)
    // dengan filter section/unit/category/track — samakan signature dgn yang dipakai FilterCoachingProton.
    var (progressData, _) = await BuildProtonProgressSubModelAsync(user, userRole, section, unit, category, track);
    var rows = progressData.CoacheeRows;

    using var wb = new ClosedXML.Excel.XLWorkbook();
    var ws = wb.Worksheets.Add("Team Progress");
    var headers = new[] { "No.", "Name", "Track", "Tahun", "Progress", "Approved", "Pending", "Rejected", "Status" };
    for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];
    ws.Row(1).Style.Font.Bold = true;

    for (int i = 0; i < rows.Count; i++)
    {
        var r = rows[i];
        int row = i + 2;
        ws.Cell(row, 1).Value = i + 1;
        ws.Cell(row, 2).Value = r.CoacheeName;
        ws.Cell(row, 3).Value = r.TrackType ?? "";
        ws.Cell(row, 4).Value = r.TahunKe ?? "";
        ws.Cell(row, 5).Value = r.ProgressPercent;
        ws.Cell(row, 6).Value = r.Approved;
        ws.Cell(row, 7).Value = r.Submitted;
        ws.Cell(row, 8).Value = r.Rejected;
        ws.Cell(row, 9).Value = r.HasFinalAssessment ? "Lulus" : (r.TotalDeliverables == 0 ? "No track" : "In Progress");
    }
    ws.Columns().AdjustToContents();

    using var ms = new MemoryStream();
    wb.SaveAs(ms);
    return File(ms.ToArray(),
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"TeamDeliverableProgress_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
}
```

> Kalau signature `BuildProtonProgressSubModelAsync` TIDAK menerima filter (cuma user, role), pakai cara yang sama dengan `FilterCoachingProton` mengambil + memfilter data (lihat body `FilterCoachingProton` saat Step 1) — JANGAN duplikat query, panggil jalur yang sama.

- [ ] **Step 3: Build**

Run: `dotnet build`
Expected: 0 error. Kalau error field `CoacheeRows`/property beda nama → samakan dengan definisi aktual (dari Step 1).

- [ ] **Step 4: Commit**

```bash
git add Controllers/CDPController.cs
git commit -m "feat(362): action ExportDashboardProgress (Excel) untuk Dashboard (G-04)"
```

---

### Task 3: Tombol Export + search box di tabel Dashboard (G-04 UI + G-10)

**Files:**
- Modify: `Views/CDP/Shared/_CoachingProtonContentPartial.cshtml:95-99` (card-header tabel)

**Konteks:** card-header "Team Deliverable Progress" (L96-98). Tambah tombol Export (link ke action Task 2, bawa nilai filter dari `<select id="protonFilterSection/Unit/Category/Track">`) + search box (filter baris tabel client-side by Name/Track).

- [ ] **Step 1: Ganti card-header jadi toolbar**

Ganti blok L96-98:
```html
    <div class="card-header bg-transparent border-0 fw-semibold pt-3">
        <i class="bi bi-table me-2 text-primary"></i>Team Deliverable Progress
    </div>
```
jadi:
```html
    <div class="card-header bg-transparent border-0 pt-3 d-flex flex-wrap justify-content-between align-items-center gap-2">
        <span class="fw-semibold"><i class="bi bi-table me-2 text-primary"></i>Team Deliverable Progress</span>
        <div class="d-flex gap-2">
            <input type="text" id="dashTableSearch" class="form-control form-control-sm" style="max-width: 220px;" placeholder="Cari nama / track...">
            <a id="dashExportBtn" href="@Url.Action("ExportDashboardProgress", "CDP")" class="btn btn-sm btn-outline-success">
                <i class="bi bi-file-earmark-excel me-1"></i>Export Excel
            </a>
        </div>
    </div>
```

- [ ] **Step 2: Tambah JS search + export-filter di akhir script partial**

Di dalam script partial (Task 1), tambah blok berikut di dalam `(function () { ... })();` SETELAH pemanggilan chart guard (sebelum penutup `})();`):

```javascript
    // G-10: client-side filter tabel Team Deliverable Progress
    var dashSearch = document.getElementById('dashTableSearch');
    if (dashSearch) {
        dashSearch.addEventListener('input', function () {
            var q = this.value.toLowerCase();
            document.querySelectorAll('table tbody tr').forEach(function (tr) {
                var name = (tr.children[1] ? tr.children[1].textContent : '').toLowerCase();
                var track = (tr.children[2] ? tr.children[2].textContent : '').toLowerCase();
                tr.style.display = (name.indexOf(q) > -1 || track.indexOf(q) > -1) ? '' : 'none';
            });
        });
    }
    // G-04: export bawa nilai filter aktif
    var dashExport = document.getElementById('dashExportBtn');
    if (dashExport) {
        dashExport.addEventListener('click', function (e) {
            var p = new URLSearchParams();
            var s = document.getElementById('protonFilterSection');
            var u = document.getElementById('protonFilterUnit');
            var c = document.getElementById('protonFilterCategory');
            var t = document.getElementById('protonFilterTrack');
            if (s && s.value) p.set('section', s.value);
            if (u && u.value) p.set('unit', u.value);
            if (c && c.value) p.set('category', c.value);
            if (t && t.value) p.set('track', t.value);
            this.href = '@Url.Action("ExportDashboardProgress", "CDP")' + (p.toString() ? ('?' + p.toString()) : '');
        });
    }
```

> Filter `<select id="protonFilter*">` ada di partial induk (`_CoachingProtonPartial.cshtml`) — id sudah dipakai `Dashboard.cshtml` script (L57-60), jadi pasti ada di DOM saat tabel tampil.

- [ ] **Step 3: Build + UAT Playwright**

`dotnet build` (0 error). Navigate `/CDP/Dashboard`:
- Ketik di `#dashTableSearch` → baris tabel terfilter (G-10).
- `#dashExportBtn` ada; klik → file `.xlsx` ter-download (G-04). (Cek `browser_network_requests` ada GET `ExportDashboardProgress` 200.)

- [ ] **Step 4: Commit**

```bash
git add Views/CDP/Shared/_CoachingProtonContentPartial.cshtml
git commit -m "feat(362): tombol Export Excel + search box tabel Dashboard (G-04/G-10)"
```

---

## Plan 02 — Navigasi (G-05 Deliverable, G-09 CertMgmt)

### Task 4: Tambah cabang Dashboard di Referer Deliverable (G-05)

**Files:**
- Modify: `Views/CDP/Deliverable.cshtml:9-18` (breadcrumb) + `:452-464` (tombol balik)

**Konteks:** Referer sudah handle CoachingProton (breadcrumb) + CoachingProton/ProtonData (back). Drill dari Dashboard → fallback ke PlanIdp (salah). Tambah cabang Dashboard.

- [ ] **Step 1: Breadcrumb — tambah cabang Dashboard**

Ganti blok L9-18:
```csharp
@{
    var refererBc = Context.Request.Headers["Referer"].ToString();
    var bcAction = "PlanIdp";
    var bcLabel = "IDP Plan";
    if (refererBc.Contains("/CDP/CoachingProton", StringComparison.OrdinalIgnoreCase))
    {
        bcAction = "CoachingProton";
        bcLabel = "Coaching Proton";
    }
}
```
jadi (tambah `else if` Dashboard):
```csharp
@{
    var refererBc = Context.Request.Headers["Referer"].ToString();
    var bcAction = "PlanIdp";
    var bcLabel = "IDP Plan";
    if (refererBc.Contains("/CDP/CoachingProton", StringComparison.OrdinalIgnoreCase))
    {
        bcAction = "CoachingProton";
        bcLabel = "Coaching Proton";
    }
    else if (refererBc.Contains("/CDP/Dashboard", StringComparison.OrdinalIgnoreCase))
    {
        bcAction = "Dashboard";
        bcLabel = "Dashboard";
    }
}
```

- [ ] **Step 2: Tombol balik — tambah cabang Dashboard**

Di blok L452-464, tambah `else if` Dashboard setelah cabang CoachingProton:
```csharp
    if (referer2.Contains("/CDP/CoachingProton", StringComparison.OrdinalIgnoreCase))
    {
        backUrl2 = Url.Action("CoachingProton")!;
        backText2 = "Kembali ke Coaching Proton";
    }
    else if (referer2.Contains("/CDP/Dashboard", StringComparison.OrdinalIgnoreCase))
    {
        backUrl2 = Url.Action("Dashboard")!;
        backText2 = "Kembali ke Dashboard";
    }
    else if (referer2.Contains("/ProtonData", StringComparison.OrdinalIgnoreCase))
    {
        backUrl2 = referer2;
        backText2 = "Kembali";
    }
```

- [ ] **Step 3: Build + UAT**

`dotnet build`. Playwright: dari `/CDP/Dashboard` klik drill ke Deliverable (kalau ada link drill di tabel; kalau tidak, set Referer via klik dari CoachingProton sudah cukup test cabang lain). Cek breadcrumb + tombol balik = "Dashboard" saat referer Dashboard. (Catatan: Referer kosong saat direct-nav → fallback PlanIdp = normal.)

- [ ] **Step 4: Commit**

```bash
git add Views/CDP/Deliverable.cshtml
git commit -m "fix(362): cabang Dashboard di breadcrumb+back Deliverable (G-05)"
```

---

### Task 5: CertMgmt breadcrumb+back ke Admin (G-09)

**Files:**
- Modify: `Views/CDP/CertificationManagement.cshtml:9-14` (breadcrumb) + `:31-33` (tombol balik)

**Konteks:** Dicapai dari Admin/Home, tapi breadcrumb "CDP" + "Kembali ke CDP" → `/CDP/Index` (hub tanpa kartu Cert). Arahkan ke Admin.

- [ ] **Step 1: Breadcrumb ke Kelola Data**

Ganti L9-14:
```html
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a asp-action="Index" asp-controller="CDP">CDP</a></li>
            <li class="breadcrumb-item active">Certification Management</li>
        </ol>
    </nav>
```
jadi:
```html
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a href="/Admin">Kelola Data</a></li>
            <li class="breadcrumb-item active">Certification Management</li>
        </ol>
    </nav>
```

- [ ] **Step 2: Tombol balik ke Admin**

Ganti L31-33:
```html
            <a asp-controller="CDP" asp-action="Index" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-1"></i>Kembali ke CDP
            </a>
```
jadi:
```html
            <a href="/Admin" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-1"></i>Kembali ke Kelola Data
            </a>
```

- [ ] **Step 3: Build + UAT**

`dotnet build`. Playwright navigate `/CDP/CertificationManagement` → breadcrumb "Kelola Data / Certification Management", tombol "Kembali ke Kelola Data" → `/Admin`.

- [ ] **Step 4: Commit**

```bash
git add Views/CDP/CertificationManagement.cshtml
git commit -m "fix(362): CertMgmt breadcrumb+back ke Kelola Data/Admin (G-09)"
```

---

## Plan 03 — Role gating (G-12)

### Task 6: ExportHistoriProton → RolesCoachAndAbove (G-12)

**Files:**
- Modify: `Controllers/CDPController.cs` (attribute di atas `ExportHistoriProton`, saat ini ~L3243)
- Test: file xUnit test reflection role (pola Phase 341/344 — cek file existing di `tests/`)

- [ ] **Step 1: Re-grep line (file digeser sesi paralel)**

```
grep -n "ExportHistoriProton" Controllers/CDPController.cs
```
Catat baris `[Authorize(Roles = UserRoles.RolesReviewerAndAbove)]` tepat DI ATAS `public async Task<IActionResult> ExportHistoriProton(`.

- [ ] **Step 2: Tulis test gagal (xUnit reflection)**

Cari file test role existing (`grep -rl "RolesCoachAndAbove\|GetCustomAttributes" tests/`). Tambah test:
```csharp
[Fact]
public void ExportHistoriProton_AllowsCoachAndAbove()
{
    var method = typeof(HcPortal.Controllers.CDPController)
        .GetMethod("ExportHistoriProton");
    Assert.NotNull(method);
    var authz = method!.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
        .FirstOrDefault();
    Assert.NotNull(authz);
    Assert.Equal(HcPortal.Models.UserRoles.RolesCoachAndAbove, authz!.Roles);
}
```

- [ ] **Step 3: Run test — GAGAL**

Run: `dotnet test --filter ExportHistoriProton_AllowsCoachAndAbove`
Expected: FAIL (Roles masih `RolesReviewerAndAbove`).

- [ ] **Step 4: Ubah attribute**

Ganti baris attribute di atas `ExportHistoriProton`:
```csharp
        [Authorize(Roles = UserRoles.RolesReviewerAndAbove)]
```
jadi:
```csharp
        [Authorize(Roles = UserRoles.RolesCoachAndAbove)]
```
(Scope data di dalam action sudah role-aware — Coach hanya coachee ter-map, lihat body action; tidak perlu ubah query.)

- [ ] **Step 5: Run test — LULUS + build**

Run: `dotnet test --filter ExportHistoriProton_AllowsCoachAndAbove` → PASS.
Run: `dotnet build` → 0 error.

- [ ] **Step 6: UAT Playwright (Coach impersonate)**

Login admin → `Admin/Impersonate` jadi Coach → navigate `/CDP/ExportHistoriProton` → HTTP 200 (download), bukan 403. Verifikasi data cuma coachee ter-map (tidak bocor lintas-coach).

- [ ] **Step 7: Commit**

```bash
git add Controllers/CDPController.cs tests/
git commit -m "fix(362): ExportHistoriProton ke RolesCoachAndAbove + test (G-12)"
```

---

## Plan 04 — Penutup

### Task 7: ROADMAP entry + verifikasi akhir

**Files:**
- Modify: `.planning/ROADMAP.md` (entry Phase 362) — HANYA setelah sesi 359 settle

- [ ] **Step 1: Cek sesi 359 sudah settle**

`git status` + `git log --oneline -5`. Pastikan tidak ada perubahan ROADMAP.md uncommitted dari sesi lain. Kalau masih aktif, TUNDA step ini.

- [ ] **Step 2: Tambah entry Phase 362 di ROADMAP.md**

Tambah di bawah Phase 361:
```markdown
### Phase 362: PROTON CDP Polish
**Goal:** Tutup 6 gap UI/navigasi/role PROTON dari gap-analysis (G-01/04/05/09/10/12).
**Depends on:** Tidak ada (independen dari 358-361; semua file beda).
**Migration:** false
**Requirements:** (gap-analysis G-01/04/05/09/10/12)
**Success Criteria:** chart Dashboard render tanpa error; Export+search Dashboard jalan; Deliverable balik ke asal (termasuk Dashboard); CertMgmt balik ke Admin; Coach bisa ExportHistoriProton.
**Spec:** `docs/superpowers/specs/2026-06-10-proton-cdp-polish-design.md`
**Plan:** `docs/superpowers/plans/2026-06-10-proton-cdp-polish.md`
```

- [ ] **Step 3: Verifikasi akhir keseluruhan**

`dotnet build` (0 error) + `dotnet test` (hijau) + grep cek tidak ada residu. UAT 6 gap sudah PASS di task masing-masing.

- [ ] **Step 4: Commit**

```bash
git add .planning/ROADMAP.md
git commit -m "docs(362): ROADMAP entry Phase 362 + verifikasi akhir"
```

---

## Self-Review (coverage spec)

- G-01 → Task 1 ✓
- G-04 → Task 2 (action) + Task 3 (UI) ✓
- G-10 → Task 3 ✓
- G-05 → Task 4 ✓
- G-09 → Task 5 ✓
- G-12 → Task 6 ✓
- ROADMAP + verifikasi → Task 7 ✓
- Migration FALSE: tidak ada task DB ✓
- Konflik sesi paralel: re-grep step di Task 2 & 6 (CDPController.cs), ROADMAP ditunda Task 7 ✓

**Catatan:** signature `BuildProtonProgressSubModelAsync` + nama field `CoacheeRows` belum 100% terverifikasi (Task 2 Step 1 = grep dulu). Kalau beda, sesuaikan di Step 2 — pakai jalur data yang sama dengan `FilterCoachingProton`.
