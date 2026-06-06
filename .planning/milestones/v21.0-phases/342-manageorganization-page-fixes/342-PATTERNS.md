# Phase 342: ManageOrganization Page Fixes - Pattern Map

**Mapped:** 2026-06-03
**Files analyzed:** 3 (all MODIFY — no new source files except optional test file)
**Analogs found:** 3 / 3 (self-analog + Phase 341/340 sibling for every new unit)
**Language note:** Prose Bahasa Indonesia; code/path English (CLAUDE.md).

> **Konsep kunci:** Phase ini hampir seluruhnya **extend-in-place** (RESEARCH "Key insight"). Analog terdekat untuk tiap file adalah **dirinya sendiri** (pola helper existing yang harus dipertahankan) + **sibling Phase 340/341** untuk unit yang benar-benar baru (PreviewEditCascade action, Bootstrap confirm modal, label fetch). Jangan rewrite — mirror pola yang sudah konsisten dan kembalikan satu fungsi menyimpang (escape inline, filter isActive, level cap, sort flat) ke pola itu.

## File Classification

| Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---------------|------|-----------|----------------|---------------|
| `Controllers/OrganizationController.cs` | controller | request-response (CRUD + read-only preview count) | itself: `EditOrganizationUnit` L128-275 (cascade) + `AddOrganizationUnit` L74-122 (dup-check); `OrgLabelController` POST actions (Phase 341) | exact (self) |
| `wwwroot/js/orgTree.js` | client utility (tree view + AJAX) | request-response (fetch) + transform (tree render) | itself: `populateParentDropdown`/`renderNode`/`submitUnitModal`/`getDescendantIds` + `ManageOrgLevelLabels.cshtml` inline JS (fetch + Bootstrap modal, Phase 341) | exact (self) |
| `Views/Admin/ManageOrganization.cshtml` | view (Razor) | request-response (markup shell) | itself: card-header L114-122 + `unitModal`/`deleteModal` L134-187 + CSS palette L37-39; `ManageOrgLevelLabels.cshtml` Bootstrap modal L91-144 (Phase 341) | exact (self) |

**Backend touch:** hanya ORG-TREE-02 (dup-check) + ORG-TREE-07 (PreviewEditCascade) menyentuh `OrganizationController`. 8 requirement sisanya pure client-tier (JS + CSS + markup).

---

## Pattern Assignments

### `Controllers/OrganizationController.cs` (controller, request-response)

**Analog:** dirinya sendiri (`EditOrganizationUnit`, `AddOrganizationUnit`) + `Controllers/OrgLabelController.cs` (Phase 341 JSON action shape).

#### A1. Dup-name per-parent edit (ORG-TREE-02) — 2 baris, in-place

**Current `AddOrganizationUnit` dup-check (L85)** — GLOBAL (Bug #1):
```csharp
bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim());
```
**Target (spec §4.7 L368-393):**
```csharp
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId);
```

**Current `EditOrganizationUnit` dup-check (L149)** — GLOBAL minus self:
```csharp
bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim() && u.Id != id);
```
**Target:**
```csharp
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId && u.Id != id);
```
> `parentId` di Edit = nilai BARU dari form (rename+reparent 1 submit aman). EF: `u.ParentId == parentId` dengan `parentId == null` → `IS NULL` (root dup-check). Pitfall 5: test pakai casing IDENTIK (InMemory case-sensitive, SQL Server CI).

#### A2. PreviewEditCascade NEW action (ORG-TREE-07)

**Auth/attribute pattern** — copy verbatim dari `EditOrganizationUnit` L125-128 + `OrgLabelController.UpdateLevelLabel` L104-107:
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PreviewEditCascade(int id, string name, int? parentId)
```
Tempatkan SETELAH `EditOrganizationUnit` (L275), SEBELUM `IsDescendantAsync` (L277).

**Count predikat — MIRROR PERSIS dari mutasi aktual.** Ini adalah analog terpenting: predikat COUNT harus identik field-by-field dengan loop mutasi `EditOrganizationUnit` agar preview tidak drift (Pitfall 1). Sumber mutasi aktual yang di-mirror:

`EditOrganizationUnit` cascade rename **Level 0** (L198-213):
```csharp
var affectedUsers = await _context.Users.Where(u => u.Section == oldName).ToListAsync();        // → CountAsync(u => u.Section == oldName)
var affectedMappings = await _context.CoachCoacheeMappings.Where(m => m.AssignmentSection == oldName)...  // → m.AssignmentSection == oldName
var affectedKompetensi = await _context.ProtonKompetensiList.Where(k => k.Bagian == oldName)...   // → k.Bagian == oldName
var affectedGuidance = await _context.CoachingGuidanceFiles.Where(g => g.Bagian == oldName)...     // → g.Bagian == oldName
```
`EditOrganizationUnit` cascade rename **Level ≥1** (L216-229):
```csharp
... u.Unit == oldName / m.AssignmentUnit == oldName / k.Unit == oldName / g.Unit == oldName
```
`EditOrganizationUnit` cascade **reparent** (L233-263) — update Users+Mappings+Kompetensi+Guidance via `u.Unit == oldName`. **FLAG A1 (RESEARCH Pitfall 1):** spec PreviewEditCascade L433-438 HANYA count `affectedUsers` untuk reparent (under-report mappings/kompetensi/guidance). Planner decide: (a) spec-verbatim under-report, atau (b) lengkapi 4 count agar 1:1 — research rekomendasi (b) + test count==actual. Mutasi reparent aktual L249-261 menyentuh KEEMPAT field.

**Early-return no-change (D-04)** — pola dari `EditOrganizationUnit` guard structure:
```csharp
var trimmed = (name ?? "").Trim();
bool nameChanged = !string.IsNullOrEmpty(trimmed) && unit.Name != trimmed;
bool parentChanged = unit.ParentId != parentId;   // server authoritative, Pitfall 6
if (!nameChanged && !parentChanged)
    return Json(new { nameChanged = false, parentChanged = false });
```

**JSON return shape** — pola `Json(new { ... })` dari `OrgLabelController` L130 / `EditOrganizationUnit` L272:
```csharp
return Json(new {
    nameChanged, parentChanged,
    affectedUsersCount = affectedUsers,
    affectedMappingsCount = affectedMappings,
    affectedKompetensiCount = affectedKompetensi,
    affectedGuidanceCount = affectedGuidance
});
```
> **Catatan dual-response:** `EditOrganizationUnit` punya `IsAjaxRequest()` dual-path (Json vs RedirectToAction). PreviewEditCascade **AJAX-only** (spec hanya Json) → cukup `Json(...)`, tidak perlu cabang non-AJAX. PreviewEditCascade dan dup-check Add/Edit **tidak** pakai `_userManager` → fixture test boleh `null!` (RESEARCH Validation Architecture).

---

### `wwwroot/js/orgTree.js` (client utility, request-response + transform)

**Analog:** dirinya sendiri (helper existing) + `ManageOrgLevelLabels.cshtml` inline JS (Phase 341 fetch+modal).

#### B1. Pre-order DFS sort + inactive visible (ORG-TREE-01, 03) — replace `populateParentDropdown` L287-303

**Current (L287-303)** — flat sort + filter isActive (Bug B + parent nonaktif disembunyikan):
```javascript
_flatUnits
    .filter(u => u.isActive && !excludeIds.has(u.id))          // ← hapus u.isActive
    .sort((a, b) => a.level - b.level || a.displayOrder - b.displayOrder)   // ← ganti pre-order DFS
    .forEach(u => {
        const indent = ' '.repeat(u.level * 4);
        ...
    });
```
**Target:** tambah helper `flattenTreePreOrder(roots)` (baru, sebelum `populateParentDropdown`) + refactor body (spec §4.6 L263-291). Reuse `buildTree(_flatUnits)` (L47) + `getDescendantIds(excludeId)` (L275). Suffix ` (nonaktif)` + `opt.style.color='#999'` untuk inactive. Pertahankan `' '.repeat()` NBSP indent existing L297 (RESEARCH A3 — jangan turun ke spasi biasa yang collapse di `<option>`).

#### B2. Escape fix — data-attribute + event delegation (ORG-TREE-04) — `renderNode` L150 + listener dekat L406

**Current (L150)** — inline onclick string interpolation (Bug #3, escape kotor saat nama ada kutip):
```javascript
<li><a class="dropdown-item text-danger" href="#" onclick="event.preventDefault(); openDeleteModal(${node.id}, '${escapeHtml(node.name)}', ${hasChildren ? node.children.length : 0})">...</a></li>
```
**Target markup (spec §4.6 L328-350):**
```javascript
<a class="dropdown-item text-danger js-delete-trigger" href="#"
   data-id="${node.id}" data-name="${escapeHtml(node.name)}"
   data-child-count="${hasChildren ? node.children.length : 0}">...</a>
```
**Target listener** — replikasi pola event delegation existing di DOMContentLoaded L406-442 (`container.addEventListener('click', e => e.target.closest(...))`):
```javascript
container.addEventListener('click', e => {
    const del = e.target.closest('.js-delete-trigger');
    if (!del) return;
    e.preventDefault();
    openDeleteModal(parseInt(del.dataset.id), del.dataset.name, parseInt(del.dataset.childCount));
});
```
> `openDeleteModal(id, name, childCount)` signature L375 TIDAK berubah. Add/Edit/Toggle item L146-148 boleh tetap inline (`${node.id}` numeric, aman).

#### B3. Level palette cap fix + tier badge (ORG-TREE-05, 10) — `renderNode` L123 + L171-173

**Current levelClass (L123)** — cap di level-2 (Bug #4):
```javascript
const levelClass = level <= 2 ? `level-${level}` : 'level-2';
```
**Target (Pitfall 7 — JS + CSS keduanya):**
```javascript
const levelClass = 'level-' + (level <= 5 ? level : 5);
```
**Badge tier** — sisip di area antara `tree-label` (L171) dan `badge Aktif/Nonaktif` (L173). **JANGAN ubah `<li class="tree-node" data-id>` (L165) atau `.drag-handle` (L168)** — Pitfall 2 (SortableJS pakai `handle:'.drag-handle'` L237 + `li.dataset.id` L248). Label dari `getLabelForLevel(level)` (D-01), warna reuse level palette (D-03):
```javascript
const tierLabel = getLabelForLevel(level);
const tierBadge = `<span class="badge org-tier-badge level-${level <= 5 ? level : 5}">${escapeHtml(tierLabel)}</span>`;
```

#### B4. Label fetch map (D-01) — NEW, analog `ajaxGet` L28-34 + Phase 341 fetch

**Pattern: 1 fetch on-load, cache module-var** (RESEARCH Pattern 2). Pakai `ajaxGet('GetLevelLabels')` existing (L28). Konsumsi endpoint `OrgLabelController.GetLevelLabels` (L42-48) yang return `{ "0":"Bagian", "1":"Unit", ... }`:
```javascript
let labelMap = null;
async function fetchLabels() { if (!labelMap) labelMap = await ajaxGet('GetLevelLabels'); }
function getLabelForLevel(level) { return (labelMap && labelMap[level]) || `Level ${level}`; }
```
**Init sequence (Pitfall 4 — labelMap MUST ready sebelum renderNode):** ganti `document.addEventListener('DOMContentLoaded', initTree)` di view L194 → wrapper `await fetchLabels(); await initTree(); renderLegend();` ATAU panggil `await fetchLabels()` di awal `initTree` (L205) sebelum `buildTree`/`renderStats`. `initTree` dipanggil ulang post-CRUD (L355/L367/L394) → guard `if (!labelMap)` cukup fetch sekali.

#### B5. Modal title dynamic + path breadcrumb (ORG-TREE-09, 06) — `openAddModal` L307 / `openEditModal` L321 + select listener

**Current modal title hardcoded** — `openAddModal` L310 `'Tambah Unit'`, `openEditModal` L326 `'Edit Unit'`. **Target:** compute `childLevel = parent ? parent.level+1 : 0` via `findUnit()` (existing L271), label dari `getLabelForLevel`. Walk parent chain via `findUnit(cur.parentId)` (reuse L271) untuk path. Wire `select#unitModalParent` `change` listener sekali on-load (bukan tiap open) — analog pola listener L406.

#### B6. Cascade-confirm flow (ORG-TREE-07, D-02/D-04) — modify `submitUnitModal` L335-359

**Current (L335-359)** — langsung POST Edit/Add tanpa preview. **Target Edit branch:** ALWAYS `await ajaxPost('PreviewEditCascade', {id, name, parentId})` (reuse `ajaxPost` L13), jumlahkan 4 count, jika `total > 0` → `await showCascadeConfirm(pv)` (Bootstrap modal Promise<bool>), jika `false` abort. Lalu lanjut POST `EditOrganizationUnit` existing (L352) + `showToast` + `initTree` persis seperti sekarang (L353-355). **`showCascadeConfirm` helper** baru — replikasi pola show/hide Bootstrap modal Phase 341 (`new bootstrap.Modal(el).show()` L173/L181) + `bootstrap.Modal.getInstance(el).hide()`, isi 4 baris dari `affected*Count` ke markup modal baru, resolve Promise di tombol [Lanjut Simpan]/[Batal].

---

### `Views/Admin/ManageOrganization.cshtml` (view, request-response)

**Analog:** dirinya sendiri (CSS palette, card-header, modal markup) + `Views/Admin/ManageOrgLevelLabels.cshtml` (Phase 341 Bootstrap modal).

#### C1. CSS palette extend level-3/4/5 (ORG-TREE-05) — tambah setelah L39

**Current palette (L37-39)** — hanya level-0/1/2:
```css
.org-node-icon.level-0 { background: rgba(13,110,253,0.1); color: #0d6efd; }
.org-node-icon.level-1 { background: rgba(102,126,234,0.1); color: #667eea; }
.org-node-icon.level-2 { background: rgba(13,202,240,0.1); color: #0dcaf0; }
```
**Target (spec §4.6 L356-363) — 6-warna cycling:**
```css
.org-node-icon.level-3 { background: rgba(25,135,84,0.1);  color: #198754; }
.org-node-icon.level-4 { background: rgba(255,193,7,0.1);  color: #b45309; }
.org-node-icon.level-5 { background: rgba(220,53,69,0.1);  color: #dc3545; }
```
Tambah juga `.org-tier-badge.level-N` (badge B3) + `.org-legend-swatch.level-N` (legend C2) yang reuse warna palette ini (D-03, Claude's discretion exact class).

#### C2. Legend block (ORG-TREE-08) — card-header L114-122

**Current card-header (L114-122)** — 2 kolom flex: kiri "Hierarki Organisasi" dot, kanan "Drag untuk reorder". **Target:** tambah `<div id="org-legend">` (baris ke-3 di bawah header row, RESEARCH Open Q2 recommendation). Diisi JS `renderLegend()` dari `labelMap` (B4). Mockup spec L240-246 inline `▣ Bagian ▣ Unit ▣ Sub-unit`, swatch = level palette.

#### C3. Path preview div (ORG-TREE-06) — unitModal body dekat L153

**Current modal body (L148-153)** — select `unitModalParent` tanpa path hint. **Target:** tambah `<div class="text-muted small mt-1" id="unitModalPath"></div>` setelah select L153 (Claude's discretion exact string per CONTEXT).

#### C4. Cascade-confirm modal markup (ORG-TREE-07) — NEW, dekat deleteModal L166-187

**Analog (replikasi struktur):** existing `deleteModal` L166-187 (modal-header warning + modal-body + footer Batal/Konfirmasi) + Phase 341 `labelEditModal` L91-116. **Target:** modal id baru (mis. `cascadeConfirmModal`), header warning, body 4 baris breakdown (`N user`, `N mapping coach-coachee`, `N kompetensi PROTON`, `N file panduan` — spec L319-325 copy Bahasa Indonesia), footer `[Batal]` + `[Lanjut Simpan]`. Aria conventions replikasi `deleteModal`/Phase 341 (Claude's discretion).

> **No view change di @section Scripts** selain markup — label fetch dipanggil dari orgTree.js (B4). View hanya ganti `DOMContentLoaded` callback L194 ke wrapper async (atau biarkan dan pindahkan fetch ke dalam `initTree`).

---

## Shared Patterns

### Anti-forgery (semua POST)
**Source:** `orgTree.js` `getAntiForgeryToken()` L8-11 + `ajaxPost()` L13-26 (append `__RequestVerificationToken`); server `[ValidateAntiForgeryToken]` (`EditOrganizationUnit` L127 / `OrgLabelController` L106); view `@Html.AntiForgeryToken()` (ManageOrganization L124).
**Apply to:** PreviewEditCascade action (server attr) + `submitUnitModal` preview call (client, reuse `ajaxPost`).
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
```

### JSON dual/AJAX response
**Source:** `EditOrganizationUnit` L271-274 (`IsAjaxRequest()` → `Json(new { success, message })`); `OrgLabelController.UpdateLevelLabel` L130 (AJAX-only `Json(new { success, message })`).
**Apply to:** PreviewEditCascade (AJAX-only, cukup `Json(...)`); dup-check edits keep existing dual-response.

### Bootstrap modal show/hide
**Source:** `orgTree.js` L318/L332/L385 (`new bootstrap.Modal(el).show()`), L353/L392 (`bootstrap.Modal.getInstance(el).hide()`); Phase 341 `ManageOrgLevelLabels.cshtml` L173/L181 (`new bootstrap.Modal(...).show()`).
**Apply to:** `showCascadeConfirm` helper (B6) + cascade-confirm modal markup (C4).

### Toast feedback
**Source:** `shared-toast.js` `showToast(msg, type)` (loaded ManageOrganization L191); usage `orgTree.js` L354/L366/L393 (`showToast(result.message, result.success ? 'success' : 'danger')`).
**Apply to:** post-Edit feedback di `submitUnitModal` (sudah ada, jangan ubah).

### Label fetch (D-01)
**Source:** `OrgLabelController.GetLevelLabels` L42-48 (`GET /Admin/GetLevelLabels` → `{ "0":"Bagian", ... }`); client `ajaxGet` `orgTree.js` L28-34.
**Apply to:** legend (C2), tier badge (B3), modal title (B5) — semua via `labelMap`/`getLabelForLevel` (B4).

### Anti-circular guard (defense-in-depth)
**Source (client):** `getDescendantIds(id)` `orgTree.js` L275-285 + exclude di `populateParentDropdown` L290-291. **Source (server):** `IsDescendantAsync` `OrganizationController` L277-286 + `parentId == id` check L160.
**Apply to:** pre-order DFS dropdown (B1, Pitfall 3) — client exclude + server hard-reject tetap.

---

## No Analog Found

Tidak ada file tanpa analog. Setiap unit baru punya analog konkret:

| New Unit | Role | Data Flow | Analog (bukan "no analog") |
|----------|------|-----------|----------------------------|
| `PreviewEditCascade` action | controller | read-only count | `EditOrganizationUnit` cascade L191-263 (predicate mirror) + `OrgLabelController` JSON action L103-136 |
| `flattenTreePreOrder` / `fetchLabels` / `getLabelForLevel` / `showCascadeConfirm` JS | client util | transform / fetch | `buildTree` L47, `ajaxGet` L28, `getDescendantIds` L275, Bootstrap modal L318/Phase 341 |
| cascade-confirm modal markup | view | markup | `deleteModal` L166-187 + Phase 341 `labelEditModal` L91-116 |

**Test file (optional, RESEARCH Validation Architecture — Wave 0 gap):** `HcPortal.Tests/OrganizationControllerTests.cs` BELUM ADA. Analog langsung: `HcPortal.Tests/OrgLabelControllerTests.cs` fixture (InMemory `Guid.NewGuid()` DB, `JsonResult` reflection `GetSuccess`/`GetInt`, `UserManager null!`). Verifikasi dulu `AdminBaseController` ctor tidak men-dereference userManager/env saat null sebelum pakai `null!`.

---

## Metadata

**Analog search scope:** `Controllers/` (OrganizationController, OrgLabelController), `wwwroot/js/` (orgTree.js, shared-toast.js usage), `Views/Admin/` (ManageOrganization.cshtml, ManageOrgLevelLabels.cshtml), `HcPortal.Tests/` (OrgLabelControllerTests pattern per RESEARCH).
**Files scanned (read in full this session):** 5 source files (orgTree.js 469L, OrganizationController.cs 521L, OrgLabelController.cs 209L, ManageOrganization.cshtml 197L, ManageOrgLevelLabels.cshtml 229L) + CONTEXT.md + RESEARCH.md (745L).
**Line numbers:** authoritative dari pembacaan file aktual sesi 2026-06-03 (sedikit bergeser dari spec 2026-06-02; semua blok target terverifikasi ada).
**Pattern extraction date:** 2026-06-03
