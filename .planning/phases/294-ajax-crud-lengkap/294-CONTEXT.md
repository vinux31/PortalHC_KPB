# Phase 294: AJAX CRUD Lengkap - Context

**Gathered:** 2026-04-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin/HC dapat melakukan seluruh operasi CRUD pada struktur organisasi via modal tanpa page reload — Add, Edit, Toggle, Delete semuanya AJAX dengan feedback toast. Collapsible Add form dan inline Edit card Razor dihapus, diganti modal.

</domain>

<decisions>
## Implementation Decisions

### Modal Add/Edit
- **D-01:** Satu modal shared untuk Add dan Edit — judul berubah dinamis ("Tambah Unit" / "Edit Unit"), form field sama (nama + parent dropdown)
- **D-02:** Dropdown parent diisi dari data flat JSON tree yang sudah di-load (bukan fetch ulang) — exclude node sendiri + descendants saat Edit
- **D-03:** Saat Add dari dropdown node, default parent = node yang diklik. Saat Add dari tombol header, default parent = root (kosong)
- **D-04:** Hapus collapsible Add form panel (collapse card) — tombol "Tambah Unit" di header langsung buka modal
- **D-05:** Hapus inline Edit card Razor (card kuning) — Edit hanya lewat modal AJAX. ViewBag.EditUnit tidak diperlukan lagi

### Action Dropdown per Node
- **D-06:** Icon ⋮ (three-dot/kebab) di kanan setiap tree row, klik buka Bootstrap dropdown menu
- **D-07:** 4 menu item: Add Child, Edit, Toggle Aktif/Nonaktif, Hapus
- **D-08:** "Add Child" = buka modal Add dengan parent pre-set ke node ini

### Toggle Behavior
- **D-09:** Toggle langsung tanpa konfirmasi dialog — klik Toggle → AJAX call → badge berubah + toast notification
- **D-10:** Sebelum toggle/delete parent yang punya children: JS cek dan tampilkan warning khusus di UI (misal "Unit ini memiliki X sub-unit")

### Delete Behavior
- **D-11:** Reuse modal #deleteModal yang sudah ada — ganti form POST submit menjadi ajaxPost() call
- **D-12:** Sebelum delete parent yang punya children: warning di frontend (sama seperti toggle)

### Tree Refresh Strategy
- **D-13:** Full re-render via initTree() setelah setiap CRUD operation — fetch ulang GetOrganizationTree, rebuild tree
- **D-14:** Preserve expand/collapse state — simpan set node ID yang expanded sebelum re-render, restore setelah render

### Claude's Discretion
- Animasi/transition saat modal muncul (Bootstrap default atau custom)
- Exact positioning dropdown relative to kebab icon
- Loading spinner di modal saat submit
- Validation feedback style di modal form (Bootstrap validation classes)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Code — Controller & Model
- `Controllers/OrganizationController.cs` — POST actions (Add, Edit, Toggle, Delete) dengan dual-response JSON/redirect
- `Controllers/AdminBaseController.cs` — Base class OrganizationController
- `Models/OrganizationUnit.cs` — Entity: Id, Name, ParentId, Level, DisplayOrder, IsActive, Children

### Existing Code — View & JS
- `Views/Admin/ManageOrganization.cshtml` — Current view: collapsible Add form, inline Edit card, delete modal, tree container
- `wwwroot/js/orgTree.js` — ajaxPost(), ajaxGet(), buildTree(), renderNode(), initTree(), event listeners
- `wwwroot/js/shared-toast.js` — showToast(message, type) helper, sudah dipakai di project

### Prior Phase Context
- `.planning/phases/292-backend-ajax-endpoints/292-CONTEXT.md` — Keputusan dual-response, JSON format {success, message}, CSRF pattern
- `.planning/phases/293-view-shell-tree-rendering/293-CONTEXT.md` — Visual style tree, expand/collapse, badge status, default expand state

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ajaxPost(url, data)` di orgTree.js — AJAX POST dengan CSRF token, return JSON
- `ajaxGet(url)` di orgTree.js — AJAX GET dengan X-Requested-With header
- `initTree()` di orgTree.js — fetch + render tree, sudah handle loading spinner + empty state + error
- `showToast(message, type)` di shared-toast.js — toast notification (success/danger)
- `buildTree(flatList)` di orgTree.js — convert flat array ke tree hierarchy
- Delete modal #deleteModal sudah ada di view — tinggal ganti submit mechanism

### Established Patterns
- Bootstrap 5 modal pattern sudah dipakai di 7+ admin views (ManageCategories, CoachCoacheeMapping, dll)
- Bootstrap dropdown-menu sudah dipakai di ManageCategories, AssessmentMonitoring
- `renderNode(node, level)` di orgTree.js — recursive node rendering, return HTML string
- Dual-response `{success: bool, message: string}` dari backend (Phase 292)

### Integration Points
- Modal dan dropdown di-render oleh JS dalam `renderNode()` — bukan Razor
- CRUD POST ke existing endpoints: AddOrganizationUnit, EditOrganizationUnit, ToggleOrganizationUnit, DeleteOrganizationUnit
- Tree refresh via initTree() setelah CRUD success
- Expand state disimpan sebelum initTree() dan di-restore setelah render

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 294-ajax-crud-lengkap*
*Context gathered: 2026-04-03*
