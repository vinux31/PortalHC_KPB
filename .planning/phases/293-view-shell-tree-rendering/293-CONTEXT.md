# Phase 293: View Shell & Tree Rendering — Context

## Phase Goal
Ganti ManageOrganization.cshtml (520 baris Razor loops) dengan ~130 baris shell + recursive tree rendering dari JSON endpoint `GetOrganizationTree`.

## Requirements
- TREE-01: Tree view dengan indentasi visual per level
- TREE-02: Expand/collapse per node dan semua sekaligus
- TREE-03: Badge status Aktif/Nonaktif per node
- TREE-04: Kedalaman unlimited (recursive rendering)

## Decisions

### 1. Visual Style: Nested List dengan Garis Penghubung
- **Keputusan:** Gunakan `ul/li` nested list dengan garis vertikal/horizontal antar node (tree lines via CSS `border-left` + `::before`)
- **Bukan:** Table-based (current) atau card-based
- **Alasan:** Tampilan tree klasik yang jelas menunjukkan hierarki, lebih compact dari card

### 2. Default Expand State: Level 0 + Level 1 Terbuka
- **Keputusan:** Saat page load, Bagian (level 0) dan Unit (level 1) sudah visible. Sub-unit (level 2+) collapsed.
- **Alasan:** User langsung lihat struktur utama tanpa perlu klik, tapi detail sub-unit tidak membanjiri layar

### 3. Expand/Collapse All: Toggle Tunggal
- **Keputusan:** Satu tombol yang berganti label — "Expand All" ↔ "Collapse All" sesuai state tree saat ini
- **Bukan:** Dua tombol terpisah atau icon-only
- **Alasan:** Clean, satu tombol saja, state jelas dari label

### 4. Badge Status: Pill + Dimmed Node
- **Keputusan:** Badge pill hijau "Aktif" / merah "Nonaktif" + seluruh node nonaktif di-dimmed (opacity 0.5)
- **Bukan:** Dot indicator tanpa teks, atau badge tanpa dimming
- **Alasan:** Double visual cue — badge untuk status eksplisit, dimming untuk membedakan secara visual pada pandangan pertama

## Existing Assets
- `Views/Admin/ManageOrganization.cshtml` — 520 baris, akan diganti
- `wwwroot/js/orgTree.js` — 31 baris AJAX helpers (Phase 292), akan di-extend
- `Controllers/OrganizationController.cs` — `GetOrganizationTree` endpoint sudah return JSON recursive
- `Models/OrganizationUnit.cs` — Recursive model (Id, Name, ParentId, Level, DisplayOrder, IsActive, Children)

## Claude's Discretion
- CSS implementation details untuk tree lines (border vs pseudo-element)
- Animasi expand/collapse (transition duration, easing)
- Icon choice per level (building, diagram, dot — atau seragam)
- Loading state saat fetch JSON
- Struktur internal fungsi JS untuk recursive render

## Deferred Ideas
_Tidak ada_
