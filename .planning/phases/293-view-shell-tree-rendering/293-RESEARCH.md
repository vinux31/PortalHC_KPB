# Phase 293: View Shell & Tree Rendering — Research

**Researched:** 2026-04-02
**Domain:** ASP.NET MVC Razor view refactoring + vanilla JS recursive tree rendering
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (dari CONTEXT.md)

### Locked Decisions
1. **Visual Style:** `ul/li` nested list dengan garis vertikal/horizontal antar node (tree lines via CSS `border-left` + `::before`). Bukan table-based (current) atau card-based.
2. **Default Expand State:** Level 0 (Bagian) dan Level 1 (Unit) sudah visible saat page load. Level 2+ collapsed.
3. **Expand/Collapse All:** Satu tombol toggle `#btn-expand-all` yang berganti label "Expand All" ↔ "Collapse All".
4. **Badge Status:** Badge pill hijau "Aktif" / merah "Nonaktif" + seluruh node nonaktif di-dimmed dengan `opacity: 0.5`.

### Claude's Discretion
- CSS implementation details untuk tree lines (border vs pseudo-element)
- Animasi expand/collapse (transition duration, easing)
- Icon choice per level (building, diagram, dot)
- Loading state saat fetch JSON
- Struktur internal fungsi JS untuk recursive render

### Deferred Ideas (OUT OF SCOPE)
_Tidak ada_
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TREE-01 | Admin/HC dapat melihat struktur organisasi sebagai tree view dengan indentasi visual per level | CSS `padding-left: 24px * level` per `.tree-children`; tree lines via `border-left` + `::before` |
| TREE-02 | Admin/HC dapat expand/collapse node individual dan semua node sekaligus | JS: `data-expanded` dataset pada `.tree-node`; tombol `#btn-expand-all` toggle semua |
| TREE-03 | Setiap node menampilkan badge status Aktif/Nonaktif | `bg-success` / `bg-danger` pill badge; node nonaktif `opacity: 0.5` |
| TREE-04 | Tree view mendukung kedalaman unlimited (recursive rendering) | JS recursive `renderNode(node)` yang dipanggil untuk setiap `node.children` |
</phase_requirements>

---

## Summary

Phase 293 adalah refaktor besar `ManageOrganization.cshtml` dari ~520 baris 3-level Razor loops menjadi ~130 baris shell HTML + JS rendering dinamis. Endpoint `GetOrganizationTree` sudah ada (Phase 292) dan mengembalikan **flat list** JSON (bukan nested), sehingga JS harus membangun struktur tree dari flat data sebelum merender.

Pekerjaan utama ada di dua tempat: (1) view Razor dikurangi menjadi shell minimal — hanya breadcrumb, header, alert TempData, kontainer tree, dan `@section Scripts`; (2) `orgTree.js` di-extend dengan fungsi `buildTree()`, `renderNode()`, dan event handler expand/collapse. UI-SPEC sudah sangat detail dan menjadi panduan implementasi langsung.

**Primary recommendation:** Bangun `renderTree(flatList)` di `orgTree.js` dengan dua tahap — (1) `buildTree(flat)` mengubah flat array menjadi nested via Map, (2) `renderNode(node, level)` rekursif menghasilkan `<li class="tree-node">`. View hanya menjadi shell HTML dengan `#org-tree-container` dan script init.

---

## Standard Stack

### Core

| Library | Versi | Purpose | Why Standard |
|---------|-------|---------|--------------|
| Bootstrap 5 | CDN (existing) | Layout, badge, spinner, button | Sudah dipakai seluruh proyek |
| Bootstrap Icons | CDN (existing) | `bi-building`, `bi-diagram-3`, `bi-dot`, `bi-chevron-right` | Sudah dipakai seluruh proyek |
| Vanilla JS (ES2020) | — | Tree rendering, AJAX, expand/collapse | Keputusan v13.0: tidak ada SPA framework/bundler |

### File yang Dimodifikasi

| File | Perubahan | Scope |
|------|-----------|-------|
| `Views/Admin/ManageOrganization.cshtml` | Dikurangi dari ~520 ke ~130 baris; hapus 3 Razor loop, pertahankan shell | Major rewrite |
| `wwwroot/js/orgTree.js` | Ditambah fungsi tree: `buildTree`, `renderNode`, `initTree`, event handlers | Extension |

**Tidak ada library baru yang perlu di-install.**

---

## Architecture Patterns

### Pola 1: Flat-to-Tree Transform

`GetOrganizationTree` mengembalikan flat array:
```json
[
  { "id": 1, "name": "Refining", "parentId": null, "level": 0, "displayOrder": 1, "isActive": true },
  { "id": 2, "name": "RFCC NHT", "parentId": 1, "level": 1, "displayOrder": 1, "isActive": true }
]
```

JS harus mengubahnya menjadi nested sebelum render:

```javascript
// Sumber: pola standar flat-to-tree dengan Map
function buildTree(flatList) {
    const map = new Map();
    const roots = [];
    flatList.forEach(u => { u.children = []; map.set(u.id, u); });
    flatList.forEach(u => {
        if (u.parentId === null) {
            roots.push(u);
        } else {
            const parent = map.get(u.parentId);
            if (parent) parent.children.push(u);
        }
    });
    return roots;
}
```

Catatan: Data sudah diurutkan oleh backend (`OrderBy Level, DisplayOrder, Name`), sehingga urutan sibling sudah benar saat Map diisi.

### Pola 2: Recursive `renderNode`

```javascript
// Recursive render — mendukung kedalaman unlimited (TREE-04)
function renderNode(node, level) {
    const isExpanded = level < 2; // Level 0 + 1 default expanded (CONTEXT Decision #2)
    const hasChildren = node.children && node.children.length > 0;
    const dimmed = !node.isActive ? 'style="opacity:0.5"' : '';

    const icon = level === 0 ? 'bi-building' : level === 1 ? 'bi-diagram-3' : 'bi-dot';
    const badge = node.isActive
        ? '<span class="badge rounded-pill bg-success badge-status">Aktif</span>'
        : '<span class="badge rounded-pill bg-danger badge-status">Nonaktif</span>';

    const chevron = hasChildren
        ? `<i class="bi bi-chevron-right tree-chevron${isExpanded ? ' expanded' : ''}"></i>`
        : '<span class="tree-chevron-placeholder"></span>';

    let childrenHtml = '';
    if (hasChildren) {
        const childItems = node.children.map(c => renderNode(c, level + 1)).join('');
        const display = isExpanded ? '' : 'style="display:none"';
        childrenHtml = `<ul class="tree-children" ${display}>${childItems}</ul>`;
    }

    return `
        <li class="tree-node" data-expanded="${isExpanded}" data-id="${node.id}">
            <div class="tree-row d-flex align-items-center gap-2" ${dimmed}>
                ${chevron}
                <i class="bi ${icon}"></i>
                <span class="tree-label">${escapeHtml(node.name)}</span>
                ${badge}
            </div>
            ${childrenHtml}
        </li>`;
}
```

### Pola 3: Expand/Collapse Handler

```javascript
// Event delegation — satu listener untuk semua node
document.getElementById('org-tree-container').addEventListener('click', function(e) {
    const row = e.target.closest('.tree-row');
    if (!row) return;
    const node = row.closest('.tree-node');
    const children = node.querySelector('.tree-children');
    if (!children) return;
    const isExpanded = node.dataset.expanded === 'true';
    children.style.display = isExpanded ? 'none' : '';
    node.dataset.expanded = isExpanded ? 'false' : 'true';
    row.querySelector('.tree-chevron')?.classList.toggle('expanded', !isExpanded);
    updateExpandAllButton();
});
```

### Pola 4: Expand All / Collapse All Toggle

Toggle logic sesuai CONTEXT Decision #3 — cek apakah ada node yang collapsed:

```javascript
document.getElementById('btn-expand-all').addEventListener('click', function() {
    const nodes = document.querySelectorAll('.tree-node');
    const hasCollapsed = Array.from(nodes).some(n => n.dataset.expanded === 'false' && n.querySelector('.tree-children'));
    nodes.forEach(node => {
        const children = node.querySelector('.tree-children');
        if (!children) return;
        const expand = hasCollapsed;
        children.style.display = expand ? '' : 'none';
        node.dataset.expanded = expand ? 'true' : 'false';
        node.querySelector('.tree-row .tree-chevron')?.classList.toggle('expanded', expand);
    });
    this.textContent = hasCollapsed ? 'Collapse All' : 'Expand All';
});
```

### Pola 5: View Shell (~130 baris)

View Razor yang dipertahankan hanya berisi:
1. `@{ ViewData["Title"] = ...; }` — tidak perlu `@model` lagi
2. Breadcrumb
3. Header dengan tombol `#btn-expand-all`
4. Alert TempData (Success / Error)
5. `<div id="org-tree-container">` — tempat JS merender
6. `@section Scripts` — load `orgTree.js` + panggil `initTree()`

Razor loops di baris 173–470 dihapus seluruhnya. Modal hapus di baris 481–503 **dipertahankan** (masih dipakai sampai Phase 294 menggantinya dengan AJAX modal).

Form Tambah (collapse) dan Form Edit **dipertahankan** karena belum diganti AJAX (scope Phase 294).

### Struktur Internal orgTree.js (setelah Phase 293)

```
orgTree.js
├── getAntiForgeryToken()     [Phase 292 — tidak berubah]
├── ajaxPost()                [Phase 292 — tidak berubah]
├── ajaxGet()                 [Phase 292 — tidak berubah]
├── escapeHtml()              [Phase 293 — baru, XSS safety]
├── buildTree(flatList)       [Phase 293 — baru]
├── renderNode(node, level)   [Phase 293 — baru]
├── renderTree(container, roots) [Phase 293 — baru]
├── updateExpandAllButton()   [Phase 293 — baru]
└── initTree()                [Phase 293 — baru, dipanggil dari view]
```

### Recommended Project Structure (perubahan)

```
Views/Admin/
└── ManageOrganization.cshtml   # Dikurangi dari 520 → ~130 baris

wwwroot/js/
└── orgTree.js                  # Extended: +buildTree, +renderNode, +initTree
```

### Anti-Patterns to Avoid

- **Jangan gunakan `innerHTML +=` dalam loop** — setiap assignment me-reparse seluruh DOM. Bangun string terlebih dahulu, assign sekali dengan `container.innerHTML = treeHtml`.
- **Jangan gunakan rekursif Razor partial** — Razor tidak support rekursif via `@Html.Partial()` tanpa helper khusus; JS recursive jauh lebih tepat untuk ini.
- **Jangan hilangkan `escapeHtml()`** — nama unit diinput user, rentan XSS jika langsung dimasukkan ke `innerHTML`.
- **Jangan gunakan `max-height: auto` langsung** — transisi CSS ke `auto` tidak bisa di-animate. Gunakan `display: none/block` (sederhana) atau `max-height` dengan nilai numerik besar.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Flat-to-tree | Custom recursive DB query | JS `buildTree()` dari flat endpoint | Backend sudah mengembalikan flat, transform di JS lebih fleksibel |
| XSS escape | Regex replace | `escapeHtml()` 5-baris (replace `&<>"'`) | DOM XSS jika nama unit mengandung `<script>` |
| Event per-node | Loop addEventListener | Event delegation pada container | Performa dan kesiapan untuk node yang di-render ulang (Phase 294+) |

---

## Common Pitfalls

### Pitfall 1: `GetOrganizationTree` mengembalikan flat, bukan nested

**Yang salah:** Mengasumsikan endpoint mengembalikan array recursive dengan `children` populated.
**Kenyataannya:** Endpoint Line 62-68 mengembalikan flat list dengan `.Select(u => new { u.Id, u.Name, u.ParentId, u.Level, u.DisplayOrder, u.IsActive })` — tidak ada `children`.
**Pencegahan:** Jalankan `buildTree(flat)` setelah fetch, sebelum `renderTree()`.

### Pitfall 2: View masih membutuhkan `@model` dan `ViewBag.PotentialParents`

**Yang salah:** Menghapus `@model` dan controller action ManageOrganization sepenuhnya.
**Kenyataannya:** Form Tambah dan Form Edit yang dipertahankan masih menggunakan `ViewBag.PotentialParents` dropdown. Controller action `ManageOrganization` masih harus mengembalikan view dengan ViewBag.
**Pencegahan:** Pertahankan form-form tersebut dan tetap kirim `ViewBag.PotentialParents` dari controller. Ubah `@model` dari `List<OrganizationUnit>` menjadi tidak ada (atau `dynamic`) karena tree data sudah via AJAX.

### Pitfall 3: Animasi expand/collapse `max-height: auto`

**Yang salah:** CSS `transition: max-height 200ms` dengan target `max-height: auto`.
**Kenyataannya:** Browser tidak bisa interpolate ke `auto`. Animasi tidak akan jalan.
**Pencegahan:** Gunakan `display: none/block` (tanpa animasi, sederhana) atau `max-height: 9999px` sebagai nilai batas atas (UI-SPEC menyebut `max-height` transition, tapi ini perlu workaround).

### Pitfall 4: Modal hapus di-remove terlalu awal

**Yang salah:** Menghapus `#deleteModal` HTML karena dianggap "lama".
**Kenyataannya:** Phase 294 baru menggantinya dengan AJAX modal. Phase 293 masih membutuhkan modal ini karena tidak ada CRUD AJAX yet.
**Pencegahan:** Pertahankan modal hapus dan script `show.bs.modal` event handler.

### Pitfall 5: `escapeHtml` tidak diimplementasi

**Yang salah:** `<span>${node.name}</span>` langsung tanpa escape.
**Risiko:** Jika nama unit mengandung `<`, `>`, atau `"`, akan rusak HTML. Jika mengandung `<script>`, XSS.
**Pencegahan:** Selalu wrap dengan `escapeHtml(node.name)`.

---

## Code Examples

### escapeHtml helper (wajib untuk innerHTML interpolation)

```javascript
function escapeHtml(str) {
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
```

### initTree — entry point yang dipanggil dari view

```javascript
async function initTree() {
    const container = document.getElementById('org-tree-container');
    if (!container) return;

    // Tampilkan loading spinner
    container.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-border text-primary" role="status"></div>
            <div class="mt-2 text-muted small">Memuat struktur organisasi...</div>
        </div>`;

    try {
        const flat = await ajaxGet('/Organization/GetOrganizationTree');
        const roots = buildTree(flat);
        if (roots.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-5">
                    <i class="bi bi-diagram-3 d-block mb-2 fs-3"></i>
                    <strong>Belum ada unit organisasi</strong>
                    <div class="small mt-1">Struktur organisasi masih kosong. Tambah Bagian baru untuk memulai.</div>
                </div>`;
            return;
        }
        const html = `<ul class="tree-root list-unstyled mb-0">${roots.map(r => renderNode(r, 0)).join('')}</ul>`;
        container.innerHTML = html;
        updateExpandAllButton();
    } catch (err) {
        container.innerHTML = `
            <div class="alert alert-danger">
                <i class="bi bi-exclamation-triangle me-2"></i>
                Gagal memuat struktur organisasi. Periksa koneksi dan muat ulang halaman.
            </div>`;
    }
}
```

### CSS tree lines (di dalam `<style>` view atau file CSS proyek)

```css
/* Tree connector lines */
.tree-children {
    border-left: 1px solid #dee2e6;
    margin-left: 12px;
    padding-left: 0;
    list-style: none;
}

.tree-node > .tree-children > .tree-node > .tree-row::before {
    content: '';
    display: inline-block;
    width: 12px;
    border-top: 1px solid #dee2e6;
    vertical-align: middle;
    margin-right: 4px;
}

/* Chevron rotation saat expanded */
.tree-chevron {
    transition: transform 200ms ease;
    color: #0d6efd;
    cursor: pointer;
    width: 16px;
    flex-shrink: 0;
}
.tree-chevron.expanded {
    transform: rotate(90deg);
}

/* Node row hover */
.tree-row {
    padding: 6px 8px;
    border-radius: 4px;
    cursor: pointer;
    min-height: 36px;
}
.tree-row:hover {
    background-color: #e9ecef;
}

/* Indentasi per level via padding pada .tree-children */
.tree-children {
    padding-left: 24px;
}
```

### View Shell Razor (struktur garis besar)

```html
@* TIDAK ADA @model — data diambil via AJAX *@
@{
    ViewData["Title"] = "Struktur Organisasi";
}

<div class="container-fluid px-4 py-4">

    <!-- Breadcrumb -->
    ...

    <!-- Header + btn-expand-all -->
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h2 class="fw-bold mb-1">
                <i class="bi bi-diagram-3 text-primary me-2"></i>Struktur Organisasi
            </h2>
            <p class="text-muted mb-0">Kelola hierarki Bagian dan Unit kerja</p>
        </div>
        <button id="btn-expand-all" class="btn btn-outline-primary btn-sm">Expand All</button>
    </div>

    <!-- TempData alerts (tetap dari Razor — form Tambah/Edit masih full-page) -->
    @if (TempData["Success"] != null) { ... }
    @if (TempData["Error"] != null) { ... }

    <!-- Form Tambah (DIPERTAHANKAN sampai Phase 294) -->
    <div class="collapse ... mb-4" id="addUnitForm"> ... </div>

    <!-- Form Edit (DIPERTAHANKAN sampai Phase 294) -->
    @if (ViewBag.EditUnit != null) { ... }

    <!-- Tree container — JS mengisi ini -->
    <div class="card border-0 shadow-sm">
        <div class="card-body">
            <div id="org-tree-container">
                <!-- populated by initTree() -->
            </div>
        </div>
    </div>

</div>

<!-- Modal hapus (DIPERTAHANKAN sampai Phase 294) -->
<div class="modal fade" id="deleteModal" ...> ... </div>

@section Scripts {
    <script src="~/js/orgTree.js"></script>
    <script>
        document.addEventListener('DOMContentLoaded', initTree);

        // Delete modal handler (tetap)
        var deleteModal = document.getElementById('deleteModal');
        if (deleteModal) {
            deleteModal.addEventListener('show.bs.modal', function(e) {
                var btn = e.relatedTarget;
                document.getElementById('deleteModalId').value = btn.getAttribute('data-id');
                document.getElementById('deleteModalName').textContent = btn.getAttribute('data-name');
            });
        }
    </script>
}
```

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | UAT manual via browser (pola proyek ini) |
| Config file | `.planning/phases/293-view-shell-tree-rendering/293-UAT.md` (dibuat saat plan) |
| Quick run | Buka `/Organization/ManageOrganization`, visual check |
| Full suite | Semua 4 success criteria diverifikasi |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated? | Notes |
|--------|----------|-----------|------------|-------|
| TREE-01 | Tree render dengan indentasi visual per level | Visual/manual | Tidak — browser check | Verifikasi garis connector + indentasi 24px per level |
| TREE-02 | Expand/collapse per node dan Expand All/Collapse All | Interaction/manual | Tidak — browser check | Klik per node + klik tombol, cek label toggle |
| TREE-03 | Badge Aktif (hijau) / Nonaktif (merah) + dimming | Visual/manual | Tidak — browser check | Cek unit nonaktif di DB tampil dengan opacity 0.5 |
| TREE-04 | Unlimited depth render benar | Visual/manual | Tidak — browser check | Jika ada Level 2+ di DB, cek render |

### Wave 0 Gaps

Tidak ada test file baru yang diperlukan — proyek menggunakan UAT manual, bukan automated test suite untuk view layer.

---

## Open Questions

1. **Controller `ManageOrganization` masih kirim `@model`?**
   - Yang diketahui: Action saat ini mengembalikan `View("ManageOrganization", roots)` — `roots` adalah `List<OrganizationUnit>`.
   - Setelah Phase 293: View tidak lagi menggunakan `@model`. Perlu hapus parameter `model` dari `return View(...)`, atau biarkan controller tetap sama (data dikirim tapi tidak digunakan di view).
   - Rekomendasi: Ganti `return View("ManageOrganization", roots)` menjadi `return View("ManageOrganization")` karena model tidak lagi dikonsumsi. Perlu hati-hati dengan `ViewBag.PotentialParents` yang masih dibutuhkan — cek apakah dikirim via ViewBag bukan model.

2. **Form Tambah/Edit: masih butuh `ViewBag.PotentialParents`?**
   - Yang diketahui: Form Tambah dan Edit di view masih menggunakan `ViewBag.PotentialParents` untuk dropdown.
   - Phase 293 mempertahankan form ini — controller harus tetap populate `ViewBag.PotentialParents`.
   - Rekomendasi: Tidak ada perubahan pada controller; hanya bagian `return View(model)` yang diubah menjadi `return View()`.

---

## Environment Availability

Step 2.6: SKIPPED (no external dependencies — phase ini hanya modifikasi Razor view dan JS file yang sudah ada di proyek. Tidak ada CLI tool, service, atau library baru.)

---

## Sources

### Primary (HIGH confidence)
- Kode aktual `Controllers/OrganizationController.cs` baris 57-69 — verified `GetOrganizationTree` return flat list
- Kode aktual `Views/Admin/ManageOrganization.cshtml` — verified 520 baris, 3 Razor loops
- Kode aktual `wwwroot/js/orgTree.js` — verified 31 baris existing helpers
- Kode aktual `Models/OrganizationUnit.cs` — verified field model
- `293-CONTEXT.md` — locked decisions
- `293-UI-SPEC.md` — component inventory dan interaction contract

### Secondary (MEDIUM confidence)
- Pola `buildTree()` flat-to-tree via Map — pola standar JS, banyak referensi konsisten

### Tertiary (LOW confidence)
- Tidak ada

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah ada di proyek, tidak ada yang baru
- Architecture: HIGH — based on actual code audit, bukan asumsi
- Pitfalls: HIGH — pitfall #1 dan #2 verified langsung dari kode controller

**Research date:** 2026-04-02
**Valid until:** 2026-05-02 (stable — tidak ada perubahan framework)
