# Stack Research

**Domain:** Tree View + Drag-and-Drop + Modal CRUD — ManageOrganization Redesign
**Researched:** 2026-04-02
**Confidence:** HIGH (semua library dikonfirmasi dari npm/GitHub resmi)

---

## Konteks Stack yang Sudah Ada

Proyek ini TIDAK menggunakan SPA framework. Stack eksisting yang relevan:

| Layer | Teknologi |
|-------|-----------|
| Backend | ASP.NET Core 8, C# |
| View Engine | Razor Views (server-rendered) |
| CSS Framework | Bootstrap 5.3 |
| Icons | Bootstrap Icons |
| JS (eksisting) | jQuery (sudah ada, di-load via CDN) |
| JS (eksisting) | Vanilla `fetch` / jQuery AJAX |
| Modal | Bootstrap 5 Modal (sudah dipakai di halaman lain) |

**Constraint penting:** Tidak boleh menambahkan React, Vue, Angular, atau bundler baru (Webpack/Vite).
Semua library baru harus bisa di-load via `<script>` tag langsung, tanpa build step.

---

## Recommended Stack — Penambahan Baru

### Satu Library Baru: SortableJS

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| **SortableJS** | **1.15.7** | Drag-and-drop reorder node dalam level yang sama | Vanilla JS murni (tanpa jQuery), 2.7 juta downloads/minggu di npm, aktif dikembangkan (rilis 1.15.7 sekitar Feb 2026), Bootstrap-agnostic, tidak ada konflik dengan jQuery yang sudah ada, support nested group dengan konfigurasi `group` |

### Tidak Ada Library Tambahan untuk Tree View

| Kebutuhan | Solusi | Alasan Tidak Butuh Library |
|-----------|--------|---------------------------|
| Tree view hierarkis | Bootstrap 5 Collapse + Razor recursive partial | Bootstrap Collapse sudah built-in dan sudah di-load. Recursive `@Html.Partial("_OrgNode", child)` di Razor cukup untuk render nested. Semua library tree view yang tersedia baik sudah abandoned maupun memerlukan bundler. |
| Expand/collapse animasi | Bootstrap 5 Collapse native | `data-bs-toggle="collapse"` sudah punya animasi built-in |
| Modal CRUD | Bootstrap 5 Modal (sudah ada) | Sudah dipakai di halaman lain di proyek ini |
| AJAX requests | jQuery AJAX / fetch (sudah ada) | jQuery sudah di-load, tidak perlu Axios atau library lain |
| Anti-forgery token | ASP.NET `@Html.AntiForgeryToken()` + header | Pattern standar yang sudah dipakai di controller lain |

---

## Installation SortableJS

SortableJS adalah satu-satunya library baru yang perlu ditambahkan.

**Opsi A — CDN (Recommended, konsisten dengan pola proyek):**

```html
<!-- Taruh di bagian @section Scripts { } pada halaman ManageOrganization -->
<script src="https://cdn.jsdelivr.net/npm/sortablejs@1.15.7/Sortable.min.js"></script>
```

**Opsi B — Lokal di wwwroot (untuk lingkungan tanpa internet):**

```
Download: https://cdn.jsdelivr.net/npm/sortablejs@1.15.7/Sortable.min.js
Simpan ke: wwwroot/lib/sortablejs/Sortable.min.js
```

Tidak ada `npm install` yang diperlukan — proyek tidak menggunakan bundler.

---

## Supporting Libraries (Tidak Perlu Ditambahkan)

Semua kebutuhan lain sudah tersedia dari stack eksisting:

| Kebutuhan | Library Eksisting | Cara Pakai |
|-----------|-------------------|------------|
| Tree node expand/collapse | Bootstrap 5 Collapse | `data-bs-toggle="collapse" data-bs-target="#children-{id}"` |
| Modal Add/Edit/Delete | Bootstrap 5 Modal | `data-bs-toggle="modal" data-bs-target="#editModal"` |
| AJAX POST untuk save | jQuery `$.ajax()` | Pattern sudah ada di seluruh proyek |
| AJAX GET untuk load | `fetch()` atau `$.get()` | Pilih salah satu yang konsisten dengan halaman lain |
| Form validation | Bootstrap form validation | `was-validated` class + HTML5 `required` attribute |
| Loading state | Bootstrap spinner | `<div class="spinner-border">` |

---

## Alternatives Considered

| Kebutuhan | Dipilih | Alternatif | Kenapa Alternatif Tidak Dipilih |
|-----------|---------|------------|--------------------------------|
| Tree view | Custom CSS + Bootstrap Collapse | **bstreeview** (chniter) | Last publish npm: 2020. Bootstrap 4 only. Tidak dimaintain. |
| Tree view | Custom CSS + Bootstrap Collapse | **bs5treeview** (nhmvienna) | Deprecated resmi — maintainer sendiri pasang notice di README, rekomendasikan ganti ke quercus.js. Last update: Aug 2021. |
| Tree view | Custom CSS + Bootstrap Collapse | **@jbtronics/bs-treeview** | No jQuery (bagus), Bootstrap 5 support (bagus), tapi memerlukan TypeScript + webpack/bundler. Tidak kompatibel dengan pola proyek ini. Version 1.0.6, last update Feb 2024. |
| Tree view | Custom CSS + Bootstrap Collapse | **jsTree** | Desain era jQuery 1.x, CSS opinionated sulit di-override dengan Bootstrap 5, bundle ~200KB |
| Drag-drop | SortableJS | **jQuery UI Sortable** | jQuery UI adalah library terpisah dari jQuery (harus load tambahan), berat, tidak aktif dikembangkan untuk kebutuhan modern |
| Drag-drop | SortableJS | **HTML5 native drag API** | API sangat verbose, tidak ada animasi built-in, sulit handle edge cases (touch device, cross-browser) |
| Drag-drop | SortableJS | **Dragula** | Tidak dimaintain sejak 2021, fitur lebih terbatas dari SortableJS |
| Modal CRUD | Bootstrap 5 Modal | **SweetAlert2 / modal library lain** | Bootstrap Modal sudah ada dan sudah dipakai di proyek. Menambah library baru untuk fungsi yang sudah tersedia = overkill. |

---

## What NOT to Use

| Hindari | Alasan Spesifik | Gunakan Ini |
|---------|-----------------|-------------|
| **jquery.nestedSortable** | Bergantung jQuery UI (bukan jQuery biasa), tidak aktif dimaintain, sulit integrasi dengan Bootstrap 5 | SortableJS 1.15.7 |
| **bstreeview / bs5treeview** | Kedua library ini abandoned. bs5treeview secara resmi deprecated di README oleh maintainernya sendiri | Custom CSS + Bootstrap Collapse |
| **jsTree** | Bundle besar, CSS sulit dioverride dengan Bootstrap 5, gaya desain jQuery 1.x era | Custom Razor recursive partial |
| **Dragula** | Tidak dimaintain aktif sejak 2021, kalah fitur dari SortableJS yang terus diperbarui | SortableJS 1.15.7 |
| **React/Vue tree component** | Membawa SPA dependency ke server-rendered app. Bertentangan dengan arsitektur proyek. | Custom Razor recursive partial |

---

## Stack Patterns by Variant

**Untuk tree node collapse/expand:**
- Render hierarki dengan Razor recursive partial `_OrgNode.cshtml`
- Setiap parent node punya `<ul id="children-{id}" class="collapse show">`
- Toggle button pakai `data-bs-toggle="collapse"` — Bootstrap handles animasi
- Tidak perlu JavaScript custom untuk expand/collapse

**Untuk drag-and-drop (reorder dalam level yang sama, tidak boleh pindah parent):**
- Inisialisasi SortableJS pada setiap `<ul class="org-children">` secara terpisah
- Set `group: false` (atau `group: { name: 'org-{parentId}', pull: false, put: false }`) untuk prevent cross-parent drag
- Event `onEnd` kirim PATCH request dengan urutan baru ke `OrganizationController`
- Gunakan `animation: 150` untuk visual feedback yang smooth

**Untuk modal CRUD (Add Child / Edit / Delete):**
- Satu modal reusable dengan form fields yang bisa diisi via JavaScript
- Tombol Add/Edit populate modal dengan data via `data-*` attributes atau AJAX partial load
- Submit via `$.ajax()` dengan anti-forgery token di header
- Setelah success: `location.reload()` atau replace hanya `#org-tree` section dengan innerHTML baru

---

## Version Compatibility

| Package | Kompatibel Dengan | Notes |
|---------|-------------------|-------|
| SortableJS 1.15.7 | Bootstrap 5.3 | Tidak ada konflik. SortableJS hanya manipulasi DOM order, tidak menyentuh Bootstrap CSS |
| SortableJS 1.15.7 | jQuery 3.x | Tidak ada konflik. SortableJS adalah vanilla JS, jQuery dan SortableJS berjalan independen |
| Bootstrap 5.3 Collapse | jQuery 3.x | Bootstrap 5 tidak butuh jQuery. Keduanya coexist tanpa masalah |
| SortableJS 1.15.7 | ASP.NET Core 8 anti-forgery | Anti-forgery dihandle di fetch/AJAX request header, SortableJS tidak menyentuh HTTP layer |

---

## Integration Points dengan Kode Eksisting

### Anti-Forgery Token Pattern (sudah dipakai di controller lain)

```javascript
// Ambil token dari hidden input yang di-render Razor
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

fetch('/Organization/Reorder', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': token
    },
    body: JSON.stringify({ items: newOrder })
});
```

### SortableJS Init — Same-Parent Only

```javascript
// Inisialisasi setiap UL yang berisi children
document.querySelectorAll('.org-children').forEach(function(el) {
    Sortable.create(el, {
        animation: 150,
        handle: '.drag-handle',    // Hanya bisa drag dari handle icon
        group: false,              // Prevent cross-parent drag
        onEnd: function(evt) {
            const parentId = evt.from.dataset.parentId;
            const ids = [...evt.from.querySelectorAll(':scope > li')]
                            .map(li => li.dataset.id);
            // Kirim ke server
            postReorder(parentId, ids);
        }
    });
});
```

### Razor Recursive Partial (`_OrgNode.cshtml`)

```csharp
@model OrgNodeViewModel
<li class="list-group-item org-node" data-id="@Model.Id">
    <div class="d-flex align-items-center gap-2">
        <i class="bi bi-grip-vertical drag-handle text-muted" style="cursor:grab"></i>
        @if (Model.Children.Any()) {
            <button class="btn btn-sm btn-link p-0" data-bs-toggle="collapse"
                    data-bs-target="#children-@Model.Id" aria-expanded="true">
                <i class="bi bi-chevron-down"></i>
            </button>
        }
        <span class="flex-grow-1">@Model.Name</span>
        <button class="btn btn-sm btn-outline-primary"
                data-bs-toggle="modal" data-bs-target="#editModal"
                data-id="@Model.Id" data-name="@Model.Name">
            <i class="bi bi-pencil"></i>
        </button>
        <button class="btn btn-sm btn-outline-success"
                data-bs-toggle="modal" data-bs-target="#addChildModal"
                data-parent-id="@Model.Id">
            <i class="bi bi-plus"></i>
        </button>
    </div>
    @if (Model.Children.Any()) {
        <ul id="children-@Model.Id"
            class="collapse show org-children list-group list-group-flush ms-3 mt-1"
            data-parent-id="@Model.Id">
            @foreach (var child in Model.Children) {
                @Html.Partial("_OrgNode", child)
            }
        </ul>
    }
</li>
```

---

## Sources

- [SortableJS npm](https://www.npmjs.com/package/sortablejs) — Version 1.15.7 dikonfirmasi, ~2.7M downloads/week, HIGH confidence
- [SortableJS GitHub](https://github.com/SortableJS/Sortable) — Vanilla JS no framework required, 1,100+ commits, HIGH confidence
- [bs5treeview GitHub deprecated notice](https://github.com/nhmvienna/bs5treeview) — Deprecation notice dikonfirmasi langsung di README, HIGH confidence
- [@jbtronics/bs-treeview npm](https://www.npmjs.com/package/@jbtronics/bs-treeview) — Version 1.0.6 Feb 2024, requires bundler, HIGH confidence
- [bstreeview npm](https://www.npmjs.com/package/bstreeview) — Version 1.2.0 last publish 2020 (Bootstrap 4 only), HIGH confidence
- [Bootstrap 5 Collapse documentation](https://getbootstrap.com/docs/5.3/components/collapse/) — Native component, no extra library needed, HIGH confidence
- [ASP.NET Core fetch + anti-forgery pattern](https://www.binaryintellect.net/articles/96b2cc91-73a8-480b-9785-fb6cbe7d9401.aspx) — MEDIUM confidence

---
*Stack research for: ManageOrganization redesign — Tree View + Drag-Drop + Modal CRUD*
*Researched: 2026-04-02*
