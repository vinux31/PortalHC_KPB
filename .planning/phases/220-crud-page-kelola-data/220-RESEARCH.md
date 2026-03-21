# Phase 220: CRUD Page Kelola Data - Research

**Researched:** 2026-03-21
**Domain:** ASP.NET Core MVC — Admin CRUD page untuk hierarki OrganizationUnit
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Tampilan Tabel**
- D-01: Collapsible tree table — Bagian bisa di-expand/collapse untuk menampilkan/menyembunyikan Unit
- D-02: Default state: semua Bagian collapsed saat halaman dibuka
- D-03: 3 kolom utama: Nama (indented), Level (Bagian/Unit), Status (badge Aktif/Nonaktif)
- D-04: Pattern mengikuti ManageCategories — indented rows dengan ikon `↳`, tombol aksi per baris (btn-group), breadcrumb ke Kelola Data

**Operasi CRUD**
- D-05: Form tambah: input Nama + dropdown Parent (atau kosong untuk root). Level dan DisplayOrder otomatis dihitung
- D-06: Form tambah sebagai collapse panel di atas tabel (pattern ManageCategories — tombol "Tambah" toggle form)
- D-07: Edit via inline form yang muncul di halaman (pattern ManageCategories — redirect ke halaman yang sama dengan form edit terbuka)
- D-08: Edit form memiliki dropdown "Induk" untuk pindah parent — validasi anti-circular reference
- D-09: Dua level penghapusan: (1) Toggle aktif/nonaktif (soft-delete), (2) Hapus permanen via modal konfirmasi
- D-10: Block hapus permanen jika node punya children aktif atau user/file ter-assign
- D-11: Block toggle nonaktif jika node punya children aktif

**Reorder & Move**
- D-12: Tombol panah ↑↓ per baris untuk reorder node dalam parent yang sama — server-side, tanpa JS kompleks
- D-13: Pindah parent via dropdown di form edit (sama seperti ManageCategories "Kategori Induk")

**Posisi di Kelola Data**
- D-14: Card baru di Section A "Data Management" — posisi setelah Manajemen Pekerja (card kedua)
- D-15: Nama card: "Struktur Organisasi" dengan subtitle "Kelola hierarki Bagian dan Unit kerja"
- D-16: Role akses: `[Authorize(Roles = "Admin, HC")]` — sama seperti ManageWorkers dan ManageCategories
- D-17: Icon: `bi-diagram-3` (hierarki)

### Claude's Discretion
- Exact styling dan spacing
- Empty state message saat belum ada data
- Alert success/error setelah operasi CRUD
- Implementasi JS untuk expand/collapse

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CRUD-01 | Halaman Struktur Organisasi di Kelola Data Section A — indented table view | Pattern ManageCategories sudah diverifikasi; OrganizationUnit model sudah tersedia di DB |
| CRUD-02 | Tambah node baru di level manapun (root, bagian, unit, sub-unit) | AddCategory pattern: ParentId nullable, Level dihitung otomatis dari parent |
| CRUD-03 | Edit nama node | EditCategory GET/POST pattern: ViewBag.EditCategory + redirect ke view yang sama |
| CRUD-04 | Pindahkan node ke parent lain (children ikut pindah, validasi anti-circular reference) | Dropdown parent di edit form; circular reference check server-side |
| CRUD-05 | Soft-delete node (block jika punya children aktif atau user ter-assign) | ToggleCategoryActive + DeleteCategory pattern; tambah cek ApplicationUser.Section/Unit |
| CRUD-06 | Reorder node dalam parent yang sama | Tombol ↑↓ via ReorderOrganizationUnit action — swap DisplayOrder antar node |
</phase_requirements>

---

## Summary

Phase 220 membangun halaman admin `ManageOrganization` di Kelola Data untuk mengelola hierarki OrganizationUnit. Semua keputusan desain mengikuti pola `ManageCategories` yang sudah ada di codebase, sehingga implementasi bersifat copy-adapt bukan membangun dari nol.

Perbedaan utama dari ManageCategories: (1) collapsible tree dengan expand/collapse per Bagian menggunakan Bootstrap collapse + JavaScript ringan, (2) dua level delete (soft-delete toggle + hard-delete), (3) tombol reorder ↑↓ untuk swap DisplayOrder, (4) validasi circular reference saat pindah parent.

OrganizationUnit model sudah selesai di Phase 219 dengan data 4 Bagian dan 19 Unit terseed. ApplicationDbContext sudah mengonfigurasi `DeleteBehavior.Restrict` dan unique index pada `Name`. Phase ini murni UI + controller logic.

**Primary recommendation:** Copy-adapt ManageCategories.cshtml dan action pattern ManageCategories/AddCategory/EditCategory/ToggleCategoryActive/DeleteCategory ke versi OrganizationUnit — tambahkan collapsible, reorder, dan validasi spesifik OrganizationUnit.

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | (project existing) | Controller + View pattern | Sudah dipakai di seluruh project |
| Bootstrap 5 | (project existing) | Collapse, modal, badge, btn-group | Sudah dipakai, tidak perlu tambah dependency |
| Bootstrap Icons | (project existing) | `bi-diagram-3`, `bi-arrow-return-right`, dll | Sudah dipakai di ManageCategories |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tom-Select | 2.x via CDN | Dropdown parent yang searchable | Jika daftar unit sangat panjang; opsional untuk phase ini |

**Installation:** Tidak ada tambahan — semua library sudah tersedia di project.

---

## Architecture Patterns

### Recommended Project Structure

```
Controllers/
└── AdminController.cs          — tambah 7 actions baru di region Organization Management

Views/Admin/
└── ManageOrganization.cshtml   — halaman baru (copy-adapt ManageCategories.cshtml)

Views/Admin/
└── Index.cshtml                — tambah card Struktur Organisasi di Section A
```

### Pattern 1: ManageOrganization GET Action

**What:** Load semua OrganizationUnit dari root, Include Children recursively, return ke view
**When to use:** GET request ke /Admin/ManageOrganization

```csharp
// Pattern dari ManageCategories line 801-806
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ManageOrganization()
{
    var roots = await _context.OrganizationUnits
        .Include(u => u.Children.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
            .ThenInclude(c => c.Children.OrderBy(gc => gc.DisplayOrder).ThenBy(gc => gc.Name))
        .Where(u => u.ParentId == null)
        .OrderBy(u => u.DisplayOrder).ThenBy(u => u.Name)
        .ToListAsync();

    // Potential parents untuk dropdown
    ViewBag.PotentialParents = await _context.OrganizationUnits
        .OrderBy(u => u.Level).ThenBy(u => u.DisplayOrder)
        .ToListAsync();

    return View(roots);
}
```

### Pattern 2: AddOrganizationUnit POST Action

**What:** Tambah node baru, hitung Level otomatis dari parent
**When to use:** POST dari form tambah

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddOrganizationUnit(string name, int? parentId)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        TempData["Error"] = "Nama tidak boleh kosong.";
        return RedirectToAction("ManageOrganization");
    }

    // Hitung Level otomatis
    int level = 0;
    if (parentId.HasValue)
    {
        var parent = await _context.OrganizationUnits.FindAsync(parentId.Value);
        if (parent == null) { TempData["Error"] = "Parent tidak ditemukan."; return RedirectToAction("ManageOrganization"); }
        level = parent.Level + 1;
    }

    // Hitung DisplayOrder: max di parent yang sama + 1
    var maxOrder = await _context.OrganizationUnits
        .Where(u => u.ParentId == parentId)
        .MaxAsync(u => (int?)u.DisplayOrder) ?? 0;

    var unit = new OrganizationUnit
    {
        Name = name.Trim(),
        ParentId = parentId,
        Level = level,
        DisplayOrder = maxOrder + 1,
        IsActive = true
    };
    _context.OrganizationUnits.Add(unit);
    await _context.SaveChangesAsync();

    TempData["Success"] = "Unit berhasil ditambahkan.";
    return RedirectToAction("ManageOrganization");
}
```

### Pattern 3: Validasi Anti-Circular Reference

**What:** Saat edit pindah parent, pastikan parent baru bukan descendant dari node yang sedang diedit
**When to use:** EditOrganizationUnit POST — sebelum SaveChanges

```csharp
// Helper: apakah targetId adalah descendant dari nodeId?
private async Task<bool> IsDescendantAsync(int nodeId, int targetId)
{
    // Walk up the tree dari targetId ke root
    var current = await _context.OrganizationUnits.FindAsync(targetId);
    while (current != null)
    {
        if (current.Id == nodeId) return true;
        if (!current.ParentId.HasValue) break;
        current = await _context.OrganizationUnits.FindAsync(current.ParentId.Value);
    }
    return false;
}

// Dalam EditOrganizationUnit POST:
if (newParentId.HasValue && await IsDescendantAsync(id, newParentId.Value))
{
    TempData["Error"] = "Tidak dapat memindahkan node ke descendant-nya sendiri (circular reference).";
    return RedirectToAction("ManageOrganization");
}
```

### Pattern 4: ToggleOrganizationUnitActive (Soft-Delete)

**What:** Toggle IsActive, block jika punya children aktif (D-11)
**When to use:** POST dari tombol toggle per baris

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleOrganizationUnitActive(int id)
{
    var unit = await _context.OrganizationUnits
        .Include(u => u.Children)
        .FirstOrDefaultAsync(u => u.Id == id);
    if (unit == null) return NotFound();

    // Block nonaktifkan jika punya children aktif (D-11)
    if (unit.IsActive && unit.Children.Any(c => c.IsActive))
    {
        TempData["Error"] = "Nonaktifkan semua unit di bawahnya terlebih dahulu.";
        return RedirectToAction("ManageOrganization");
    }

    unit.IsActive = !unit.IsActive;
    await _context.SaveChangesAsync();
    TempData["Success"] = $"Status berhasil diubah menjadi {(unit.IsActive ? "Aktif" : "Nonaktif")}.";
    return RedirectToAction("ManageOrganization");
}
```

### Pattern 5: DeleteOrganizationUnit (Hard Delete)

**What:** Hapus permanen, block jika ada children aktif atau user ter-assign
**When to use:** POST dari modal konfirmasi hapus

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteOrganizationUnit(int id)
{
    var unit = await _context.OrganizationUnits
        .Include(u => u.Children)
        .Include(u => u.KkjFiles)
        .Include(u => u.CpdpFiles)
        .FirstOrDefaultAsync(u => u.Id == id);
    if (unit == null) return NotFound();

    // Block jika punya children aktif (D-10)
    if (unit.Children.Any(c => c.IsActive))
    {
        TempData["Error"] = "Hapus atau nonaktifkan unit di bawahnya terlebih dahulu.";
        return RedirectToAction("ManageOrganization");
    }

    // Block jika ada file ter-assign
    if (unit.KkjFiles.Any() || unit.CpdpFiles.Any())
    {
        TempData["Error"] = "Unit ini masih memiliki file KKJ/CPDP yang ter-assign. Hapus file terlebih dahulu.";
        return RedirectToAction("ManageOrganization");
    }

    // Cek ApplicationUser ter-assign (Section/Unit sebagai string)
    var unitName = unit.Name;
    var hasAssignedUsers = await _context.Users.AnyAsync(u => u.Section == unitName || u.Unit == unitName);
    if (hasAssignedUsers)
    {
        TempData["Error"] = "Unit ini masih memiliki pekerja yang ter-assign. Pindahkan pekerja terlebih dahulu.";
        return RedirectToAction("ManageOrganization");
    }

    _context.OrganizationUnits.Remove(unit);
    await _context.SaveChangesAsync();
    TempData["Success"] = "Unit berhasil dihapus.";
    return RedirectToAction("ManageOrganization");
}
```

### Pattern 6: ReorderOrganizationUnit (Swap DisplayOrder)

**What:** Swap DisplayOrder antara node dan sibling sebelum/sesudahnya
**When to use:** POST dari tombol ↑ atau ↓ per baris

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ReorderOrganizationUnit(int id, string direction)
{
    var unit = await _context.OrganizationUnits.FindAsync(id);
    if (unit == null) return NotFound();

    var siblings = await _context.OrganizationUnits
        .Where(u => u.ParentId == unit.ParentId)
        .OrderBy(u => u.DisplayOrder)
        .ToListAsync();

    var idx = siblings.FindIndex(u => u.Id == id);
    OrganizationUnit? swapTarget = direction == "up" && idx > 0
        ? siblings[idx - 1]
        : direction == "down" && idx < siblings.Count - 1
            ? siblings[idx + 1]
            : null;

    if (swapTarget != null)
    {
        (unit.DisplayOrder, swapTarget.DisplayOrder) = (swapTarget.DisplayOrder, unit.DisplayOrder);
        await _context.SaveChangesAsync();
    }

    return RedirectToAction("ManageOrganization");
}
```

### Pattern 7: Collapsible Tree di View (JS)

**What:** Expand/collapse per Bagian (Level 0) menggunakan Bootstrap collapse + data attributes
**When to use:** ManageOrganization.cshtml, perbedaan utama dari ManageCategories

```javascript
// Setiap row Bagian punya data-bs-toggle="collapse" ke tbody dengan ID unik
// Default collapsed (D-02): aria-expanded="false"
document.addEventListener('DOMContentLoaded', function () {
    // Semua Bagian collapsed by default — pastikan tbody children tidak di-render expanded
    // Tidak perlu JS tambahan jika Bootstrap collapse dipakai dengan benar
});
```

```html
<!-- Row Bagian (Level 0) -->
<tr data-bs-toggle="collapse" data-bs-target="#children-@unit.Id"
    aria-expanded="false" style="cursor:pointer;">
    <td><i class="bi bi-chevron-right me-2 collapse-icon"></i>@unit.Name</td>
    ...
</tr>

<!-- Rows Unit (Level 1) — wrapped dalam tbody collapse -->
<tbody class="collapse" id="children-@unit.Id">
    @foreach (var child in unit.Children) { ... }
</tbody>
```

### Anti-Patterns to Avoid

- **Merender semua children tanpa collapse:** Tabel bisa sangat panjang (19+ unit). Wajib collapsible per Bagian.
- **Menggunakan tbody sebagai collapse target:** Bootstrap collapse bekerja pada elemen apa pun termasuk tbody — ini adalah pendekatan yang valid.
- **Level dihitung di client-side:** Level harus dihitung server-side dari parent saat AddOrganizationUnit.
- **Tidak meng-Include Children saat ToggleActive/Delete:** Akan menyebabkan bug — children tidak terdeteksi.
- **Hard-delete tanpa cek ApplicationUser:** User punya Section/Unit sebagai string — cek `u.Section == unitName || u.Unit == unitName` diperlukan.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Collapsible rows | Custom accordion JS | Bootstrap collapse pada tbody | Sudah tersedia, zero dependency tambahan |
| Dropdown parent | Custom autocomplete | HTML select + optional Tom-Select | Tom-Select sudah ada via CDN di project |
| Anti-forgery token | Custom CSRF | `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` | Pattern sudah dipakai di semua POST action |
| Delete confirmation | Custom confirm dialog | Bootstrap modal (sudah ada di ManageCategories) | Copy langsung modal dari ManageCategories |
| Audit log | Custom logging | `_auditLog.LogAsync()` | Sudah dipakai di semua CRUD action AdminController |

---

## Common Pitfalls

### Pitfall 1: Circular Reference saat Edit Parent
**What goes wrong:** Admin memindahkan Bagian X ke Unit Y yang merupakan child-nya sendiri — infinite loop saat query.
**Why it happens:** Self-referential foreign key tanpa validasi.
**How to avoid:** Implementasi `IsDescendantAsync` helper (lihat Pattern 3) sebelum SaveChanges di EditOrganizationUnit POST.
**Warning signs:** Stack overflow atau infinite loop saat query tree.

### Pitfall 2: Cascade Nonaktif Tidak Ter-block
**What goes wrong:** Admin menonaktifkan Bagian padahal masih punya Unit aktif — Unit masih muncul sebagai aktif tapi Bagian-nya nonaktif.
**Why it happens:** Toggle tidak memeriksa children.
**How to avoid:** Cek `unit.Children.Any(c => c.IsActive)` di ToggleActive — block dan tampilkan pesan error (D-11).

### Pitfall 3: Unique Name Constraint Tidak Ditangani
**What goes wrong:** Dua Bagian bernama sama menyebabkan DbUpdateException dari unique index di `OrganizationUnits.Name`.
**Why it happens:** ApplicationDbContext sudah mengonfigurasi `HasIndex(u => u.Name).IsUnique()`.
**How to avoid:** Cek nama duplikat sebelum insert/update (`AnyAsync(u => u.Name == name && u.Id != id)`).

### Pitfall 4: DisplayOrder Tidak Konsisten setelah Delete
**What goes wrong:** Setelah node dihapus, DisplayOrder sibling-nya menjadi tidak berurutan (gap) — tombol ↑↓ berperilaku aneh.
**Why it happens:** Delete tidak re-normalize DisplayOrder.
**How to avoid:** Reorder action cukup swap nilai — tidak perlu normalisasi. Gap tidak menyebabkan bug, hanya mempengaruhi estetika urutan angka.

### Pitfall 5: User Check menggunakan String Match
**What goes wrong:** Cek `u.Section == unitName` case-sensitive atau gagal jika ada spasi trailing.
**Why it happens:** ApplicationUser.Section disimpan sebagai string, bukan FK.
**How to avoid:** Gunakan `u.Section == unit.Name || u.Unit == unit.Name` — data konsisten karena diisi dari dropdown yang sama. Pertimbangkan `.Trim()` saat simpan nama.

---

## Code Examples

### Query Tree untuk View
```csharp
// Source: pola dari AdminController SetCategoriesViewBag (line 757-781)
var roots = await _context.OrganizationUnits
    .Include(u => u.Children.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name))
        .ThenInclude(c => c.Children.OrderBy(gc => gc.DisplayOrder).ThenBy(gc => gc.Name))
    .Where(u => u.ParentId == null)
    .OrderBy(u => u.DisplayOrder).ThenBy(u => u.Name)
    .ToListAsync();
```

### Indented Row di View
```razor
@* Source: pola dari ManageCategories.cshtml line 306-313 *@
<td class="p-3 @(unit.Level == 0 ? "" : unit.Level == 1 ? "ps-4" : "ps-5")">
    @if (unit.Level > 0)
    {
        <i class="bi bi-arrow-return-right text-muted me-2 small"></i>
    }
    @unit.Name
</td>
```

### Delete Modal (copy dari ManageCategories)
```razor
@* Source: ManageCategories.cshtml line 429-453 *@
<div class="modal fade" id="deleteModal" ...>
    <!-- Sama persis dengan ManageCategories, ganti action ke DeleteOrganizationUnit -->
    <form id="deleteForm" method="post">
        @Html.AntiForgeryToken()
        <input type="hidden" name="id" id="deleteModalId" />
        <button type="submit" class="btn btn-danger">Hapus</button>
    </form>
</div>
```

### Card di Index.cshtml (Section A)
```razor
@* Source: pola dari Views/Admin/Index.cshtml line 19-34 *@
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("ManageOrganization", "Admin")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-diagram-3 fs-5 text-primary"></i>
                    <span class="fw-bold">Struktur Organisasi</span>
                </div>
                <small class="text-muted">Kelola hierarki Bagian dan Unit kerja</small>
            </div>
        </div>
    </a>
</div>
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Static OrganizationStructure.cs | OrganizationUnits tabel di database | Phase 219 | Data hierarki kini editable via CRUD |
| KkjBagian entity terpisah | OrganizationUnit sebagai FK di KkjFile/CpdpFile | Phase 219 | Satu sumber kebenaran untuk hierarki |

---

## Open Questions

1. **Collapsible tbody di Bootstrap 5**
   - What we know: Bootstrap collapse bekerja pada elemen non-button dan non-a dengan `data-bs-toggle="collapse"`. Namun `<tbody>` adalah elemen table — perlu diverifikasi bahwa Bootstrap collapse berfungsi pada tbody.
   - What's unclear: Apakah Bootstrap collapse mendukung `<tbody>` secara native atau perlu wrapper `<tr>` + `<td colspan>`.
   - Recommendation: Alternatif aman — bungkus children rows dalam `<tr class="collapse-row"><td colspan="4"><div class="collapse" ...>...</div></td></tr>` atau gunakan CSS `display:none` + JS toggle sederhana tanpa Bootstrap collapse. Uji di browser sebelum commit.

2. **Kedalaman hierarki maksimum**
   - What we know: Data saat ini 2 level (Bagian = Level 0, Unit = Level 1). Model mendukung n-level.
   - What's unclear: Apakah view perlu render lebih dari 2 level (Level 2+) dengan indentasi `ps-5`, `ps-6`, dll.
   - Recommendation: Render 3 level untuk future-proofing, sama seperti ManageCategories yang merender 3 level.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (project tidak menggunakan automated test suite) |
| Config file | None |
| Quick run command | Jalankan app, buka /Admin/ManageOrganization |
| Full suite command | Manual walk-through semua operasi CRUD |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Steps |
|--------|----------|-----------|-------|
| CRUD-01 | Halaman tampil dengan indented table | manual | Buka /Admin/ManageOrganization — verifikasi tabel muncul dengan data dari Phase 219 |
| CRUD-02 | Tambah node di berbagai level | manual | Tambah Bagian baru (no parent), tambah Unit baru (parent = Bagian), verifikasi muncul di tabel |
| CRUD-03 | Edit nama node | manual | Klik Edit pada node, ubah nama, simpan — verifikasi nama berubah |
| CRUD-04 | Pindah parent + anti-circular | manual | Edit Unit, ganti parent ke Bagian lain — verifikasi children ikut. Coba pindah Bagian ke descendant-nya — verifikasi error |
| CRUD-05 | Toggle + block + hard-delete + block | manual | Toggle nonaktif Bagian yang punya Unit aktif — verifikasi error. Toggle Unit — berhasil. Hapus Unit dengan user ter-assign — verifikasi error |
| CRUD-06 | Reorder dengan ↑↓ | manual | Klik ↑ pada Unit kedua — verifikasi urutan berubah |

---

## Sources

### Primary (HIGH confidence)
- `Views/Admin/ManageCategories.cshtml` — view pattern diverifikasi langsung dari codebase
- `Controllers/AdminController.cs` (line 753-951) — CRUD action pattern diverifikasi langsung
- `Models/OrganizationUnit.cs` — entity model diverifikasi
- `Data/ApplicationDbContext.cs` (line 554-566) — DB config: unique index name, DeleteBehavior.Restrict
- `Views/Admin/Index.cshtml` (line 1-80) — Section A card layout diverifikasi

### Secondary (MEDIUM confidence)
- Bootstrap 5 collapse docs — tbody sebagai collapse target perlu verifikasi browser (Open Question 1)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library sudah ada di project, tidak ada dependency baru
- Architecture: HIGH — semua pola diverifikasi langsung dari codebase ManageCategories
- Pitfalls: HIGH — circular reference, unique constraint, user string-check semua berdasarkan kode aktual
- Collapsible tbody: MEDIUM — Bootstrap collapse pada tbody perlu verifikasi browser

**Research date:** 2026-03-21
**Valid until:** Stabil — tidak ada dependency baru, valid hingga codebase berubah signifikan
