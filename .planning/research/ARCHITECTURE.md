# Architecture Research

**Domain:** ASP.NET Core MVC — Tree-view CRUD dengan AJAX + Bootstrap Modal + Drag-drop Reorder
**Researched:** 2026-04-02
**Confidence:** HIGH (berdasarkan analisis langsung kode existing + pola Bootstrap/Vanilla JS yang sudah ada di proyek)

---

## Kondisi Existing (Baseline)

### Masalah Struktural Saat Ini

```
ManageOrganization.cshtml (520 baris)
├── 3x copy-paste identik: root loop / child loop / grandchild loop
├── Tiap aksi = full-page POST redirect (PRG pattern)
├── Edit mode = redirect ke ?editId=N, form muncul di atas tabel
├── Reorder = POST per klik (2 klik per geser 1 posisi)
└── Level hardcoded ke 3 level: root (Bagian) / child (Unit) / grandchild (Unit)
```

### Arsitektur Existing yang Dipertahankan

| Komponen | Status | Catatan |
|----------|--------|---------|
| `OrganizationController.cs` | DIPERTAHANKAN dan DIPERLUAS | Tambah AJAX endpoints, logika cascade tidak berubah |
| `OrganizationUnit` model | TIDAK BERUBAH | Id, Name, ParentId, Level, DisplayOrder, IsActive |
| Cascade rename logic | DIPERTAHANKAN | Tetap di `EditOrganizationUnit` POST |
| Cascade reparent logic | DIPERTAHANKAN | Update `ApplicationUser.Section` saat parent berubah |
| Circular reference check | DIPERTAHANKAN | `IsDescendantAsync` tidak berubah |
| `[ValidateAntiForgeryToken]` | DIPERTAHANKAN | AJAX harus kirim header `RequestVerificationToken` |
| `[Authorize(Roles = "Admin, HC")]` | TIDAK BERUBAH | Semua endpoint sama |

---

## Target Architecture

### System Overview

```
+------------------------------------------------------------------+
|                   Browser (ManageOrganization)                    |
+------------------------------------------------------------------+
|  +-----------------+  +----------------------+  +-----------+    |
|  | Tree View       |  | Bootstrap Modal      |  | SortableJS|    |
|  | (table rows     |  | Add / Edit           |  | Drag-drop |    |
|  | + indent)       |  | (satu modal          |  | reorder   |    |
|  |                 |  |  dual-mode)          |  |           |    |
|  +--------+--------+  +-----------+----------+  +-----+-----+    |
|           |                       |                   |          |
|           +------- orgTree.js (orchestrator) ---------+          |
|                    - state lokal (treeData[])                    |
|                    - AJAX fetch wrapper + CSRF header            |
|                    - DOM re-render setelah tiap aksi             |
+------------------------------------------------------------------+
|                    HTTP (fetch + CSRF header)                     |
+------------------------------------------------------------------+
|               OrganizationController (ASP.NET Core)              |
|  +-------------------+  +-----------------+  +----------------+  |
|  | GET               |  | POST (existing) |  | POST (baru)    |  |
|  | ManageOrganization|  | Add/Edit/Toggle |  | GetOrgTree     |  |
|  | GetOrgTree (JSON) |  | Delete          |  | ReorderBulk    |  |
|  +--------+----------+  +--------+--------+  +-------+--------+  |
|           +---------------------|-------------------+            |
+------------------------------------------------------------------+
|                    ApplicationDbContext (EF Core)                 |
|  OrganizationUnits --> Users (denorm) + CoachCoacheeMappings     |
+------------------------------------------------------------------+
```

### Component Responsibilities

| Komponen | Tanggung Jawab | Implementasi |
|----------|----------------|--------------|
| `ManageOrganization.cshtml` | Shell halaman: breadcrumb, alert, container div, modal markup, script include | Dikurangi dari 520 menjadi ~120 baris |
| `orgTree.js` | State management tree, fetch AJAX, re-render DOM, modal, SortableJS init | File baru di `wwwroot/js/` |
| Bootstrap Modal (satu, dual-mode) | Form Add dan Edit berbagi satu modal, mode diset via JS | Tidak perlu dua modal terpisah |
| `GetOrganizationTree` endpoint | Return flat JSON array untuk JS render tree | Endpoint baru di OrganizationController |
| `AddOrganizationUnit` (AJAX) | Validasi + simpan + return JSON `{success, message, unit}` | Modifikasi existing, tambah JSON path |
| `EditOrganizationUnit` (AJAX) | Validasi + cascade + simpan + return JSON | Cascade logic TIDAK BERUBAH |
| `ToggleOrganizationUnitActive` (AJAX) | Toggle + return JSON `{success, isActive}` | Tambah JSON response path |
| `DeleteOrganizationUnit` (AJAX) | Delete + return JSON `{success, message}` | Tambah JSON response path |
| `ReorderOrganizationUnit` (bulk) | Terima array `[{id, displayOrder}]`, batch update | Ganti logika up/down dengan bulk reorder |

---

## Pola Arsitektur yang Direkomendasikan

### Pola 1: Dual-Response Controller (AJAX + PRG Fallback)

**Apa:** Controller POST cek `Request.Headers["X-Requested-With"]`, kembalikan JSON jika AJAX,
redirect jika bukan.

**Kenapa dipilih:** Tidak perlu endpoint terpisah. Route existing tetap bekerja jika JS dimatikan.

```csharp
// Di OrganizationController
private bool IsAjaxRequest() =>
    Request.Headers["X-Requested-With"] == "XMLHttpRequest";

[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddOrganizationUnit(string name, int? parentId)
{
    // ... validasi dan simpan tetap sama ...

    if (IsAjaxRequest())
        return Json(new {
            success = true,
            message = "Unit berhasil ditambahkan.",
            unit = new { unit.Id, unit.Name, unit.ParentId, unit.Level, unit.DisplayOrder, unit.IsActive }
        });

    TempData["Success"] = "Unit berhasil ditambahkan.";
    return RedirectToAction("ManageOrganization");
}
```

### Pola 2: Flat JSON Tree dengan Client-side Rendering

**Apa:** GET endpoint kembalikan array flat. JS merender tree secara rekursif.

**Kenapa dipilih:** Data flat mudah dimanipulasi di JS (insert, delete, reorder) tanpa re-fetch setelah
setiap operasi. Server tidak perlu tahu tentang tree structure untuk rendering.

```javascript
// orgTree.js
function renderTree(units) {
    const roots = units
        .filter(u => !u.parentId)
        .sort((a, b) => a.displayOrder - b.displayOrder);
    const tbody = document.getElementById('orgTreeBody');
    tbody.innerHTML = '';
    roots.forEach(root => renderNode(root, units, tbody, 0));
}

function renderNode(unit, allUnits, container, depth) {
    container.appendChild(buildRow(unit, depth));
    allUnits
        .filter(u => u.parentId === unit.id)
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .forEach(child => renderNode(child, allUnits, container, depth + 1));
}
```

### Pola 3: Single Bootstrap Modal, Dual Mode

**Apa:** Satu modal `#orgModal`. Tombol "Tambah" set mode=add dan clear form.
Tombol "Edit" set mode=edit dan isi form dari data unit.

**Kenapa dipilih:** View lebih ringkas. Tidak ada library form tambahan. Bootstrap sudah ada di proyek.

```javascript
function openAddModal(parentId = null) {
    document.getElementById('modalTitle').textContent = 'Tambah Unit Baru';
    document.getElementById('orgForm').reset();
    document.getElementById('orgId').value = '';
    document.getElementById('parentIdSelect').value = parentId ?? '';
    bootstrap.Modal.getOrCreateInstance(document.getElementById('orgModal')).show();
}

function openEditModal(unit) {
    document.getElementById('modalTitle').textContent = 'Edit Unit: ' + unit.name;
    document.getElementById('orgId').value = unit.id;
    document.getElementById('nameInput').value = unit.name;
    document.getElementById('parentIdSelect').value = unit.parentId ?? '';
    bootstrap.Modal.getOrCreateInstance(document.getElementById('orgModal')).show();
}
```

### Pola 4: SortableJS untuk Drag-drop Reorder

**Apa:** SortableJS (Vanilla JS, ~50KB) pada tbody. Drop event trigger POST ke `ReorderOrganizationUnit`
dengan payload array `[{id, displayOrder}]`.

**Kenapa SortableJS:** Proyek tidak menggunakan jQuery. SortableJS adalah Vanilla JS murni,
mendukung table rows, ringan, dan mature (npm weekly 3M+ downloads).

**Batasan penting:** Drag-drop HANYA reorder dalam siblings yang sama (parent sama). Cross-parent
drag diblokir (`group: false`). Pindah parent harus lewat modal Edit karena ada cascade logic di
backend.

```javascript
// Init setelah render tree
Sortable.create(document.getElementById('orgTreeBody'), {
    handle: '.drag-handle',
    animation: 150,
    filter: '.no-drag',       // baris yang tidak boleh di-drag
    onEnd: function(evt) {
        const newOrder = collectSiblingOrders(evt.item);
        saveReorder(newOrder);
    }
});
```

**Catatan:** SortableJS tidak mengenal parent-child relationship di flat table. JS harus
menentukan siblings secara manual dari `data-parent-id` attribute pada setiap row.

---

## Data Flow

### Flow: Load Halaman

```
Browser GET /Admin/ManageOrganization
    |
    v
OrganizationController.ManageOrganization() --> Kembalikan HTML shell kosong
    |
    v
Browser load orgTree.js
    --> fetch GET /Admin/GetOrganizationTree
    --> Terima flat JSON array
    --> renderTree() membangun DOM tabel
```

### Flow: Add Unit (AJAX)

```
User klik "Tambah Unit"
    --> openAddModal()
    --> Bootstrap Modal tampil

User submit form
    --> orgTree.js tangkap submit event, cegah default
    --> fetch POST /Admin/AddOrganizationUnit
        Header X-Requested-With: XMLHttpRequest
        Header RequestVerificationToken: [dari input hidden]
        Body: name=..., parentId=...
    |
    v
OrganizationController.AddOrganizationUnit()
    --> Validasi (nama kosong, duplikat)
    --> Simpan ke DB
    --> return Json({success, message, unit})
    |
    v
orgTree.js terima response
    --> Jika success: push ke treeData[], re-render, tutup modal, tampilkan toast
    --> Jika error: tampilkan pesan di dalam modal (tidak tutup modal)
```

### Flow: Edit + Cascade

```
User klik Edit button di row
    --> openEditModal(unit) --> Modal tampil dengan data ter-prefill

User submit form
    --> fetch POST /Admin/EditOrganizationUnit
    |
    v
Controller:
    --> Validasi (duplikat, circular reference)
    --> Jika nama berubah: cascade update ApplicationUser.Section/Unit
    --> Jika parent berubah: cascade update ApplicationUser.Section
    --> Cascade update CoachCoacheeMappings
    --> Simpan
    --> return Json({success, message, cascadedUsers, cascadedMappings})
    |
    v
orgTree.js:
    --> Update unit di treeData[]
    --> re-render tree
    --> Tampilkan toast dengan info cascade jika ada
```

### Flow: Drag-drop Reorder

```
User drag row ke posisi baru
    |
    v
SortableJS onEnd event
    --> collectSiblingOrders(): ambil semua rows dengan parentId sama
        --> susun berdasarkan posisi DOM saat ini
        --> buat array [{id, displayOrder: newIndex}]
    --> fetch POST /Admin/ReorderOrganizationUnit
        Body: JSON array (application/json, bukan form)
    |
    v
Controller ReorderOrganizationUnit (dimodifikasi):
    --> Terima List<ReorderItem> dari JSON body
    --> Batch update DisplayOrder
    --> return Json({success})
    |
    v
orgTree.js:
    --> Update displayOrder di treeData[] (DOM sudah benar dari SortableJS)
```

### CSRF Token untuk AJAX

```javascript
// Satu token yang diambil dari Razor hidden input
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

async function ajaxPost(url, formData) {
    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': token
        },
        body: new URLSearchParams(formData)
    });
    return response.json();
}
```

---

## Komponen: Baru vs Dimodifikasi vs Tidak Berubah

### Komponen BARU

| File | Deskripsi | Estimasi |
|------|-----------|----------|
| `wwwroot/js/orgTree.js` | State management, AJAX, render, modal, SortableJS init | ~250 baris |
| `OrganizationController.GetOrganizationTree` | GET, return JSON flat array semua OrganizationUnit | ~10 baris |

### Komponen DIMODIFIKASI

| File | Perubahan | Ukuran Perubahan |
|------|-----------|-----------------|
| `Views/Admin/ManageOrganization.cshtml` | Hapus 3 Razor loop. Tambah modal markup + div#orgTreeBody + script tags | 520 -> ~130 baris |
| `OrganizationController.AddOrganizationUnit` | Tambah `IsAjaxRequest()` check, return JSON path | +10 baris |
| `OrganizationController.EditOrganizationUnit` | Tambah JSON response path. Cascade logic TIDAK BERUBAH | +5 baris |
| `OrganizationController.ToggleOrganizationUnitActive` | Tambah JSON response path | +5 baris |
| `OrganizationController.DeleteOrganizationUnit` | Tambah JSON response path | +5 baris |
| `OrganizationController.ReorderOrganizationUnit` | Ganti up/down swap dengan bulk array update | Rewrite (~20 baris) |

### Komponen TIDAK BERUBAH

| Komponen | Alasan |
|----------|--------|
| `OrganizationUnit` model | Tidak perlu field tambahan |
| Cascade rename logic (baris 163-186) | Tetap di server, tidak diekspos ke JS |
| Cascade reparent logic (baris 189-213) | Tetap di server |
| `IsDescendantAsync` | Tetap di server |
| `UpdateChildrenLevelsAsync` | Tetap di server |
| Semua 7 controller yang consume org data | Membaca `ApplicationUser.Section/Unit` (string), bukan FK |
| Semua 19 views yang menampilkan org data | Menggunakan string dari ApplicationUser, bukan join ke OrganizationUnits |

---

## Integration Points

### 7 Controller yang Mengonsumsi Data Organisasi

| Controller | Cara Konsumsi | Dampak Redesign |
|------------|---------------|-----------------|
| `WorkerController` | Dropdown Section/Unit saat Create/Edit worker | Tidak berubah |
| `CoachMappingController` | Filter dan assign berdasarkan Section/Unit | Tidak berubah |
| `AssessmentAdminController` | Filter peserta assessment per unit | Tidak berubah |
| `DocumentAdminController` | FK ke `OrganizationUnit.Id` di KKJ/CPDP | Tidak berubah |
| `CMPController` | Display Section/Unit di profil user | Tidak berubah |
| `CDPController` | Coaching assignment per unit | Tidak berubah |
| `RenewalController` | Filter renewal per unit | Tidak berubah |

**Kesimpulan penting:** Redesign halaman ManageOrganization adalah perubahan terisolasi sepenuhnya.
Data organisasi disimpan sebagai denormalized string di `ApplicationUser.Section/Unit`, bukan sebagai
FK ke OrganizationUnit. Tidak ada downstream impact ke controller atau view manapun.

### 19 Views yang Menampilkan Org Data

Semua 19 views menggunakan `ApplicationUser.Section` dan `ApplicationUser.Unit` (string).
Cascade logic di backend memastikan string ini tetap sinkron saat unit di-rename atau di-reparent.
Redesign tidak mengubah views ini sama sekali.

---

## Urutan Build yang Direkomendasikan

Berdasarkan dependency antar komponen:

```
Phase A: Backend AJAX endpoints (tidak breaking, bisa deploy kapan saja)
  1. Tambah GetOrganizationTree (GET, JSON endpoint baru)
  2. Modifikasi POST actions: AddOrganizationUnit, EditOrganizationUnit,
     ToggleOrganizationUnitActive, DeleteOrganizationUnit -> dual response
  3. Overhaul ReorderOrganizationUnit -> terima bulk JSON array

Phase B: View shell + modal (bergantung pada Phase A)
  4. Sederhanakan ManageOrganization.cshtml: hapus 3 Razor loop
  5. Tambah modal markup (satu modal dual-mode: Add/Edit)
  6. Tambah container div#orgTreeBody dan hidden CSRF token
  7. Tambah script tag untuk orgTree.js dan SortableJS (CDN)

Phase C: orgTree.js - core (bergantung pada Phase A dan B)
  8. Fetch GetOrganizationTree + renderTree (tanpa aksi)
  9. Modal Add + modal Edit dengan AJAX submit
  10. Toggle aktif/nonaktif via AJAX
  11. Delete via AJAX (dengan konfirmasi modal)

Phase D: Drag-drop reorder (bergantung pada Phase C)
  12. SortableJS init pada tbody dengan handle .drag-handle
  13. collectSiblingOrders() function
  14. saveReorder() AJAX call ke ReorderOrganizationUnit
```

**Rationale urutan:**
- Phase A selesai dulu agar Phase C bisa di-test secara independen di browser developer tools
- Phase B memisahkan markup dari JS agar review lebih mudah
- Phase D terakhir karena paling kompleks, tidak blocking feature CRUD

---

## Anti-Patterns

### Anti-Pattern 1: Drag-drop Cross-Parent

**Yang dilakukan:** Izinkan drag node antar parent berbeda via SortableJS `group` config.

**Kenapa salah:** `EditOrganizationUnit` memiliki cascade logic kompleks (rename Section, reparent
users, update CoachCoacheeMappings) yang harus dieksekusi saat parent berubah. Drag-drop
cross-parent bypass semua ini, membuat data denormalized di `ApplicationUser` jadi stale.

**Lakukan ini:** Cross-parent move hanya via modal Edit. Drag-drop hanya reorder dalam siblings
yang memiliki `parentId` sama. Konfigurasi SortableJS dengan `group: false`.

### Anti-Pattern 2: Re-fetch Tree Setelah Setiap Operasi

**Yang dilakukan:** Setelah setiap AJAX call berhasil, fetch ulang `GetOrganizationTree` dan
re-render seluruh tree dari server.

**Kenapa salah:** Lambat, UX terasa blink atau flicker. Untuk tree kecil (organisasi Pertamina
~50-100 unit) ini overkill dan menambah latency yang tidak perlu.

**Lakukan ini:** Mutasi `treeData` array lokal langsung setelah response sukses, re-render dari
state lokal. Re-fetch hanya saat terjadi error tak terduga atau pertama kali load halaman.

### Anti-Pattern 3: Render Tree Rekursif di Razor Server-side

**Yang dilakukan:** Buat Razor helper atau partial view rekursif untuk render tree.

**Kenapa salah:** Razor partial rekursif di ASP.NET Core tidak native (perlu workaround dengan
`@await Html.PartialAsync`). Tetap menghasilkan full-page render, menghilangkan benefit AJAX.
Juga mempertahankan masalah 520 baris copy-paste.

**Lakukan ini:** Server hanya kembalikan flat JSON array. Rekursi sepenuhnya di `renderNode()` JS.

### Anti-Pattern 4: Tiga Modal Terpisah per Level

**Yang dilakukan:** Buat modal Add-Root, modal Add-Child, modal Add-Grandchild terpisah.

**Kenapa salah:** Triple duplication kembali, persis masalah yang sama dengan view existing.
Tiga modal membutuhkan tiga set event listener dan tiga kali markup.

**Lakukan ini:** Satu modal dengan dropdown "Induk". Level dihitung otomatis dari parent yang
dipilih. Dropdown diisi dari `treeData` saat modal dibuka.

### Anti-Pattern 5: Inline Script di Cshtml

**Yang dilakukan:** Taruh semua JS langsung di `@section Scripts` dalam ManageOrganization.cshtml.

**Kenapa salah:** JS 250+ baris inline sulit di-debug, tidak bisa di-cache browser secara terpisah,
dan tidak bisa di-test dengan isolasi.

**Lakukan ini:** Semua JS di `wwwroot/js/orgTree.js`. View hanya punya script tag untuk load file
tersebut. Pendekatan ini konsisten dengan pattern proyek (lihat file JS lain di `wwwroot/js/`).

---

## Scaling Considerations

| Skala Unit | Pendekatan |
|------------|------------|
| < 100 unit (kondisi saat ini Pertamina KPB) | Client-side render dari flat array sangat cukup. Tidak perlu virtual scroll. |
| 100-500 unit | Tetap aman. renderTree masih O(n). SortableJS bisa handle ratusan rows. |
| > 500 unit | Perlu pertimbangan lazy-load per node atau pagination per Bagian. Di luar scope v13.0. |

---

## Sources

- Analisis langsung: `Controllers/OrganizationController.cs` (360 baris, 2026-04-02)
- Analisis langsung: `Views/Admin/ManageOrganization.cshtml` (520 baris, 2026-04-02)
- Analisis langsung: `Models/OrganizationUnit.cs`
- Grep audit: 11 controllers + 19 views yang mengonsumsi org data
- SortableJS: https://github.com/SortableJS/Sortable — Vanilla JS, no jQuery, weekly 3M+ npm downloads
- ASP.NET Core CSRF + AJAX: `RequestVerificationToken` header pattern (standard, HIGH confidence)

---
*Architecture research for: ManageOrganization Tree View Redesign (v13.0)*
*Researched: 2026-04-02*
