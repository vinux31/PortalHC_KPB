# Phase 342: ManageOrganization Page Fixes - Research

**Researched:** 2026-06-03
**Domain:** ASP.NET Core MVC (net8.0) — vanilla JS tree UI + EF Core cascade preview + xUnit/InMemory tests
**Confidence:** HIGH (semua klaim diverifikasi langsung terhadap file codebase aktual sesi ini)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 (Sumber label tier):** legend + tree row badge + modal title pakai **JS fetch `GET /Admin/GetLevelLabels`** (Phase 340 endpoint, client-render). Server `@inject IOrgLabelService` app-wide = scope Phase 343, BUKAN page ini.
- **D-02 (UI konfirmasi cascade ORG-TREE-07):** **Bootstrap modal** dengan 4-line count breakdown (user/mapping/kompetensi/guidance) + tombol Batal / Lanjut Simpan. Native `confirm()` reserved untuk single-line delete.
- **D-03 (Visual badge tier per row):** **Reuse palette warna level** — badge background pakai warna level sama dengan icon (CSS `.org-node-icon.level-0..5`, extend ke level-5).
- **D-04 (Trigger PreviewEditCascade):** **Selalu call** endpoint saat user klik Simpan di Edit modal; endpoint early-return `{nameChanged:false, parentChanged:false}` kalau no change; modal konfirmasi muncul HANYA kalau ada `affected*Count > 0`, lalu lanjut `EditOrganizationUnit`. Server authoritative; no client-side duplicate logic.

### Spec-locked (verbatim §4.6/§4.7 — implement as given, BUKAN gray area)
- Pre-order DFS sort: `flattenTreePreOrder(roots)` + `populateParentDropdown(excludeId)` — code verbatim spec L263-291.
- Inactive parent fix: hapus `.filter(u => u.isActive)`, ganti suffix " (nonaktif)" + `opt.style.color='#999'` (spec L294).
- Escape fix: `data-id`/`data-name`/`data-child-count` attribute + event delegation `container.addEventListener('click', ...closest('.js-delete-trigger'))` (spec L328-350).
- Level cap palette: 6-warna cycling CSS `.org-node-icon.level-0..5`, level 6+ fallback level-5 (spec L352-364).
- Dup-name per-parent: `AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId [&& u.Id != id])` (spec L368-393).
- `PreviewEditCascade(int id, string name, int? parentId)`: `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]`, count reuse EditOrganizationUnit Section/Unit branch logic (spec L395-448).
- Modal title dynamic: label level N+1 dari GetLevelLabels, fallback "Level N+1" (spec L255-259).

### Claude's Discretion
- Path breadcrumb (ORG-TREE-06) markup/styling: `<div class="text-muted small mt-1" id="unitModalPath">` render on `select` change — planner picks exact render string.
- Bootstrap modal id/aria conventions untuk cascade confirm modal — replicate Phase 341 modal pattern.
- Badge markup (`<span class="badge ...">`) exact class — planner picks per palette reuse D-03.
- Legend/badge label di-cache di JS (1 fetch on page load) vs re-fetch — planner decides (1 fetch on load cukup, page reload after CRUD).

### Deferred Ideas (OUT OF SCOPE)
- App-wide label integration di 7 area page (CMP/CDP/Worker/CoachMapping/ProtonData/Renewal/DocumentAdmin) — **Phase 343** (ORG-INTEG-01/02), pakai `@inject IOrgLabelService` server-render.
- Formal xUnit (pre-order DFS, dup per-parent, PreviewEditCascade accuracy) + Playwright E2E + manual UAT 5 scenario — **Phase 344** (TEST-01..06). Phase 342 = browser smoke verify saja.
- Regression smoke drag-reorder/toggle/delete/add — **Phase 344** ORG-INTEG-03.
- Search box tree, drag-reparent across parents, stats Aktif/Nonaktif, Tom Select, i18n — future/out-of-scope (REQUIREMENTS L54-71).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ORG-TREE-01 | Dropdown induk pre-order DFS sort (Bug B) | `flattenTreePreOrder` + refactor `populateParentDropdown` (orgTree.js L287-303). `buildTree` + `getDescendantIds` sudah ada (L47-60, L275-285). Code Example #1. |
| ORG-TREE-02 | Dup-name validation per-parent not global | Controller fix di `AddOrganizationUnit` L85 + `EditOrganizationUnit` L149. Code Example #5. Pitfall 5. |
| ORG-TREE-03 | Parent nonaktif visible (suffix `(nonaktif)` + grey) | Hapus `.filter(u => u.isActive)` di `populateParentDropdown` L294; suffix + `opt.style.color='#999'`. Code Example #1. |
| ORG-TREE-04 | openDeleteModal data-name + event delegation (escape fix Bug #3) | Refactor `renderNode` dropdown markup L150 + `getDescendantIds` container listener. `openDeleteModal(id, name, childCount)` L375 signature unchanged. Code Example #4. |
| ORG-TREE-05 | Icon palette level 3-5 cycling 6 warna (Bug #4) | CSS `.org-node-icon.level-0/1/2` ada (view L37-39), extend 3/4/5; JS `levelClass` L123 fix `level <= 5`. Code Example #6. |
| ORG-TREE-06 | Path breadcrumb real-time on parent select | Vanilla `select#unitModalParent` change listener + `<div id="unitModalPath">`. Walk parent chain via `_flatUnits`. Code Example #7. |
| ORG-TREE-07 | POST /Admin/PreviewEditCascade + Bootstrap modal confirm | New action reuse EditOrganizationUnit cascade predicates (L196-263) via COUNT queries. Code Example #2 + #3. Pitfall 1 (predicate drift). |
| ORG-TREE-08 | Legend color-to-tier-label in card header | View card-header L114-122; render dari `GetLevelLabels` fetch. Code Example #8. |
| ORG-TREE-09 | Modal title dynamic (Tambah Bagian/Unit/Sub-unit, fallback Level N) | `openAddModal`/`openEditModal` L307/L321 set `unitModalLabel`; compute level dari parent. Code Example #7. |
| ORG-TREE-10 | Tree row badge tier label per node | `renderNode` L164-177 tambah badge span; warna = level palette (D-03). Code Example #6. |
</phase_requirements>

## Summary

Phase 342 adalah **fix + UX-enhancement page tunggal** (`/Admin/ManageOrganization`) tanpa perubahan schema/migration. Semua perubahan terkonsentrasi di 3 file existing: `Controllers/OrganizationController.cs` (2 dup-check edit + 1 action baru), `wwwroot/js/orgTree.js` (sort/escape/level-cap/path/title/cascade-modal), dan `Views/Admin/ManageOrganization.cshtml` (legend block + CSS palette extend + cascade-confirm modal markup + label fetch init). Endpoint dan service Phase 340 (`GET /Admin/GetLevelLabels`, `OrgLabelService`) sudah live dan dikonsumsi via JS fetch (D-01). Test project `HcPortal.Tests` (xUnit 2.9.3 + EF InMemory 8.0.0) sudah ada dengan pattern fixture yang bisa direplikasi.

**Temuan terpenting (diverifikasi):** `EditOrganizationUnit` (OrganizationController.cs L191-263) **SUDAH** melakukan cascade rename + reparent ke 4 denormalized field-pair. ORG-TREE-07 PreviewEditCascade adalah **warning/preview layer saja** — tidak ada perbaikan data-integrity yang dibutuhkan. Akurasi preview tergantung pada penggunaan **predikat COUNT yang IDENTIK** dengan loop mutasi aktual (lihat Pitfall 1). Predikat aktual sudah diverifikasi field-by-field terhadap model: `Users.Section/Unit`, `CoachCoacheeMappings.AssignmentSection/AssignmentUnit`, `ProtonKompetensiList.Bagian/Unit`, `CoachingGuidanceFiles.Bagian/Unit`.

**Catatan akurasi:** Spec ditulis 2026-06-02; line number di file aktual sudah sedikit bergeser dari yang dirujuk spec, tapi semua blok kode target masih ada dan terverifikasi. Line number di RESEARCH.md ini adalah hasil pembacaan file **aktual sesi 2026-06-03** (authoritative).

**Primary recommendation:** Implement secara in-place (extend, jangan rewrite) mengikuti spec §4.6/§4.7 verbatim. Plan PreviewEditCascade dengan predikat COUNT yang di-mirror persis dari blok mutasi EditOrganizationUnit, dan tambahkan unit test yang membandingkan count preview vs hasil mutasi aktual pada DB InMemory yang sama untuk mengunci kesetaraan.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Pre-order DFS dropdown sort (ORG-TREE-01) | Browser/Client (orgTree.js) | — | Data tree sudah di client (`_flatUnits` dari `GetOrganizationTree`); sorting murni view-layer. |
| Dup-name validation (ORG-TREE-02) | API/Backend (OrganizationController) | — | Authoritative uniqueness check butuh DB query; client tidak boleh otoritatif. |
| Inactive parent visibility (ORG-TREE-03) | Browser/Client | — | `isActive` flag sudah ada di `_flatUnits`; murni rendering decision. |
| Escape/event delegation (ORG-TREE-04) | Browser/Client | — | XSS-hardening + DOM event handling, client-only. |
| Icon palette extend (ORG-TREE-05) | Browser/Client (CSS + JS class) | — | Pure presentational. |
| Path breadcrumb (ORG-TREE-06) | Browser/Client | — | Derived dari tree state in-memory. |
| Cascade preview count (ORG-TREE-07) | API/Backend (new action) | Browser/Client (modal render) | COUNT harus authoritative dari DB, identik dengan mutasi aktual; client hanya render + gate submit. |
| Legend (ORG-TREE-08) | Browser/Client | API (label fetch) | Render client; label source = `GET /Admin/GetLevelLabels`. |
| Modal title dynamic (ORG-TREE-09) | Browser/Client | API (label fetch) | Level→label resolution client-side dengan map dari endpoint. |
| Tree row badge (ORG-TREE-10) | Browser/Client | API (label fetch) | Render client di `renderNode`; label dari fetch map. |

**Catatan tier:** Hanya ORG-TREE-02 dan ORG-TREE-07 menyentuh backend. Sisanya 8 requirement adalah pure client-tier (JS + CSS + view markup). Planner harus memastikan tidak ada uniqueness/count logic yang dianggap authoritative di client.

## Standard Stack

### Core (semua sudah terpasang — NO new dependency)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controller actions + Razor views | Stack existing project [VERIFIED: HcPortal.csproj TargetFramework net8.0] |
| EF Core | 8.0.x | `AnyAsync`/`CountAsync` query | ORM existing [VERIFIED: ApplicationDbContext DbSets] |
| Bootstrap 5.3 | bundled | Modal, badge, form | Pola modal existing (`unitModal`, `deleteModal`) [VERIFIED: ManageOrganization.cshtml L134-187] |
| Bootstrap Icons | bundled | `bi-*` ikon tree | Existing [VERIFIED: orgTree.js renderNode] |
| SortableJS | 1.15.7 (CDN) | Drag-reorder tree | Existing, JANGAN break [VERIFIED: ManageOrganization.cshtml L190] |
| vanilla JS (fetch) | — | AJAX tree + label fetch | `ajaxGet`/`ajaxPost`/`getAntiForgeryToken` existing [VERIFIED: orgTree.js L8-34] |

### Supporting (test only)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xUnit | 2.9.3 | Unit test framework | Validation Architecture section [VERIFIED: HcPortal.Tests.csproj] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | In-memory DbContext untuk test | Pre-order DFS / dup-name / preview-count test [VERIFIED: HcPortal.Tests.csproj L12] |

**Installation:** None — semua dependency sudah ada. Phase ini **NO `dotnet add package`, NO migration** (CONTEXT.md Out of scope; REQUIREMENTS L67 Cascade rename approach dipertahankan).

**Version verification:** Tidak ada paket baru untuk diverifikasi ke registry. Semua versi di atas dibaca langsung dari `.csproj` aktual.

## Architecture Patterns

### System Architecture Diagram

```
[Page load /Admin/ManageOrganization]
        │
        ├──> initTree()  ──fetch──> GET /Admin/GetOrganizationTree ──> _flatUnits (flat list {id,name,parentId,level,displayOrder,isActive})
        │         │
        │         └──> buildTree(_flatUnits) ──> roots ──> renderNode(node, level) per node
        │                                                        │
        │                                                        ├──> badge tier (ORG-TREE-10) ── label dari labelMap
        │                                                        ├──> icon levelClass 0..5 (ORG-TREE-05)
        │                                                        └──> dropdown action: data-id/data-name/data-child-count (ORG-TREE-04)
        │
        └──> fetchLabels() ──fetch──> GET /Admin/GetLevelLabels ──> labelMap { "0":"Bagian", "1":"Unit", ... }
                  │                                                        │
                  │                                                        ├──> legend card-header (ORG-TREE-08)
                  │                                                        └──> getLabelForLevel(n) helper (badge + modal title)
                  │
                  └──> [CRITICAL: labelMap harus ready SEBELUM renderNode jalan — lihat Pitfall 4]

[User klik "Tambah/Edit" di kebab]
        │
        ├──> openAddModal(parentId) / openEditModal(id)
        │         ├──> populateParentDropdown(excludeId)
        │         │         ├──> flattenTreePreOrder(buildTree(_flatUnits))  (ORG-TREE-01 pre-order DFS)
        │         │         ├──> filter !excludeIds (exclude self + descendants — anti circular, Pitfall 3)
        │         │         └──> option text: indent + name + (nonaktif suffix)  (ORG-TREE-03)
        │         ├──> set modal title dinamis dari level+1 → labelMap (ORG-TREE-09)
        │         └──> select.change ──> render path breadcrumb (ORG-TREE-06)
        │
        └──> [Edit] user klik Simpan
                  │
                  ├──> ALWAYS POST /Admin/PreviewEditCascade {id, name, parentId}   (D-04)
                  │         └──> server COUNT (identik EditOrganizationUnit predicate — Pitfall 1)
                  │                  └──> {nameChanged, parentChanged, affectedUsersCount, affectedMappingsCount, affectedKompetensiCount, affectedGuidanceCount}
                  │
                  ├──> if any affected*Count > 0 ──> show Bootstrap cascade-confirm modal (D-02, 4-line breakdown)
                  │         └──> user [Lanjut Simpan] ──┐
                  │         └──> user [Batal] ──> abort  │
                  │                                       ▼
                  └──> else (no impact) ──────────> POST /Admin/EditOrganizationUnit (existing cascade jalan)
                                                            └──> initTree() refresh + showToast
```

### Recommended File Touch Map (extend in-place — DO NOT rewrite)
```
Controllers/OrganizationController.cs
  ├── AddOrganizationUnit L85        # dup-check: tambah && u.ParentId == parentId
  ├── EditOrganizationUnit L149      # dup-check: tambah && u.ParentId == parentId (sebelum && u.Id != id)
  └── + PreviewEditCascade (new)     # action baru setelah EditOrganizationUnit, sebelum private helpers

wwwroot/js/orgTree.js
  ├── + flattenTreePreOrder (new)    # helper sebelum populateParentDropdown
  ├── populateParentDropdown L287    # refactor: pre-order DFS + inactive visible
  ├── renderNode L118-178            # escape fix (L150), levelClass (L123), badge (L164-177)
  ├── + fetchLabels / labelMap (new) # init label sebelum render (Pitfall 4)
  ├── openAddModal L307 / openEditModal L321  # dynamic title + populate + path init
  ├── + path breadcrumb listener (new)
  └── + cascade-confirm flow di submitUnitModal L335 (Edit branch)

Views/Admin/ManageOrganization.cshtml
  ├── CSS .org-node-icon.level-3/4/5 (new, after L39)
  ├── card-header L114-122           # tambah legend block
  ├── + cascade-confirm modal markup (new, dekat deleteModal)
  └── @section Scripts L189-196      # (label fetch dipanggil dari orgTree.js init, no view change selain markup)
```

### Pattern 1: Dual-response AJAX action (existing convention — replicate)
**What:** Setiap POST action cek `IsAjaxRequest()` lalu return `Json(new { success, message })` untuk AJAX atau `RedirectToAction` untuk non-AJAX.
**When to use:** PreviewEditCascade — page selalu AJAX, jadi cukup `Json(...)` (Preview tidak punya non-AJAX path; spec L400-448 hanya return Json).
**Example:** Lihat `EditOrganizationUnit` L271-274 [VERIFIED: OrganizationController.cs].

### Pattern 2: JS fetch label map sekali on-load, render dari cache
**What:** `const labelMap = await ajaxGet('GetLevelLabels')` sekali, simpan di module-level var, helper `getLabelForLevel(n)` baca map.
**When to use:** Legend, badge, modal title (D-01). 1 fetch on load cukup (page reload after CRUD per Claude's discretion).
**Example:** Pola `ajaxGet` existing [VERIFIED: orgTree.js L28-34].

### Anti-Patterns to Avoid
- **Client-side authoritative uniqueness/count:** ORG-TREE-02 dan ORG-TREE-07 wajib server-authoritative. JS tidak boleh menghitung "23 user" sendiri (data user tidak ada di client).
- **Rewrite orgTree.js / OrganizationController dari nol:** extend in-place. File punya pola event-delegation expand/collapse, Sortable init, dan toast yang harus dipertahankan.
- **Hardcode label "Bagian"/"Unit" di JS render baru:** harus dari `labelMap` (D-01) — kecuali `renderStats` existing (L79-107) yang OUT of scope phase ini (itu label statis stat-bar, bukan tier badge).
- **Inline `onclick="...'${escapeHtml(node.name)}'..."`:** ini justru Bug #3 yang diperbaiki (escape kotor saat name punya kutip). Ganti dengan data-attribute + delegation.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cascade impact count | Re-query entitas penuh lalu `.Count` di memory | `CountAsync` per field-pair (single round-trip) | Spec L451: preview pakai COUNT, bukan reload entitas. Hemat overhead. |
| HTML-escape nama unit | Manual replace di string template | `escapeHtml()` existing (orgTree.js L38-45) untuk `data-name` value | Sudah ada + teruji. |
| Anti-forgery token | Generate manual | `getAntiForgeryToken()` (orgTree.js L8-11) + `@Html.AntiForgeryToken()` (view L124) | Convention existing. |
| Toast feedback | Custom alert | `showToast(msg, type)` (shared-toast.js, loaded view L191) | Convention Phase 341 D-07. |
| Descendant set (anti-circular) | Tulis ulang walk | `getDescendantIds(id)` existing (orgTree.js L275-285) | Sudah ada + dipakai populateParentDropdown. Pitfall 3. |
| Modal show/hide | Manual class toggle | `new bootstrap.Modal(el).show()` / `.getInstance(el).hide()` | Pola existing (orgTree.js L318, L353). |

**Key insight:** Phase ini 90% adalah re-wiring helper yang SUDAH ADA. Bug muncul justru dari satu fungsi yang menyimpang dari pola (escape inline di L150, filter isActive di L294, level cap di L123, sort flat di L295). Perbaikan = kembalikan ke pola yang konsisten, bukan menambah abstraksi baru.

## Runtime State Inventory

> Phase ini bukan rename/migration data. Tidak ada string yang di-rename di datastore.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — tidak ada penggantian string identifier di DB. ORG-TREE-02 mengubah **logika validasi** (predikat dup-check), bukan data existing. | None |
| Live service config | None — tidak ada konfigurasi service eksternal tersentuh. `GET /Admin/GetLevelLabels` sudah live (Phase 340). | None |
| OS-registered state | None — no scheduled task / process. | None |
| Secrets/env vars | None. | None |
| Build artifacts | None baru. Test project `HcPortal.Tests` sudah ter-build; test baru hanya menambah file `.cs`. | None |

**Verified by:** Pembacaan CONTEXT.md "Out of scope" + REQUIREMENTS L63-71 (no schema change) + tidak ada DbSet/model baru di scope.

## Common Pitfalls

### Pitfall 1: Drift predikat preview-count vs cascade-mutate aktual (TERTINGGI)
**What goes wrong:** PreviewEditCascade menampilkan "23 user" tapi EditOrganizationUnit aktual meng-update 25 (atau sebaliknya) → user tertipu.
**Why it happens:** Predikat COUNT di Preview ditulis terpisah dari predikat loop di Edit; mudah menyimpang (mis. lupa branch `Level == 0` vs `Level >= 1`, atau lupa reparent count).
**How to avoid:** Mirror persis 4 field-pair per branch. Predikat aktual (terverifikasi OrganizationController.cs L196-263):

| Branch | Field di-mutate (Edit) | Predikat COUNT (Preview) |
|--------|------------------------|--------------------------|
| `Level == 0` (rename) | `Users.Section`, `CoachCoacheeMappings.AssignmentSection`, `ProtonKompetensiList.Bagian`, `CoachingGuidanceFiles.Bagian` == `oldName` | `CountAsync(u => u.Section == oldName)` dst. |
| `Level >= 1` (rename) | `Users.Unit`, `CoachCoacheeMappings.AssignmentUnit`, `ProtonKompetensiList.Unit`, `CoachingGuidanceFiles.Unit` == `oldName` | `CountAsync(u => u.Unit == oldName)` dst. |
| reparent (`oldParentId != parentId && Level >= 1`, `!nameChanged`) | `Users.Unit == oldName` (Section diubah ke ancestor baru) | `affectedUsers += CountAsync(u => u.Unit == oldName)` |

**Catatan ketidaksempurnaan spec (FLAG untuk planner):** Blok reparent aktual (Edit L247-262) meng-update Users/Mappings/Kompetensi/Guidance saat reparent — tapi spec PreviewEditCascade (L433-438) HANYA menambah `affectedUsers` untuk reparent (tidak menambah affectedMappings/Kompetensi/Guidance). Ini berarti preview count reparent **under-reports** mapping/kompetensi/guidance dibanding mutasi aktual. **Keputusan planner:** baik (a) ikut spec verbatim (under-report reparent-only secondary fields, acceptable karena rename adalah kasus dominan) ATAU (b) lengkapi reparent count agar 1:1 dengan mutasi. Rekomendasi research: pilih (b) untuk akurasi penuh, dan TULIS test yang membandingkan count vs mutasi aktual (Validation Architecture) — test akan gagal kalau (a) dipilih saat reparent menyentuh secondary fields. Tandai sebagai `[ASSUMED]` resolution → konfirmasi planner. [ASSUMED]
**Warning signs:** Test "preview count == actual mutated count" gagal pada skenario reparent dengan mapping/kompetensi terdampak.

### Pitfall 2: SortableJS drag rusak setelah tambah badge/legend
**What goes wrong:** Drag-reorder berhenti bekerja atau `orderedIds` salah setelah menambah `<span>` badge ke row.
**Why it happens:** Sortable pakai `handle: '.drag-handle'` (L237) dan membaca `li.dataset.id` (L248). Badge span yang disisipkan di dalam `.tree-row` aman SELAMA: (a) `.drag-handle` (L168) tetap ada, (b) `data-id` tetap di `<li class="tree-node">` (L165), bukan dipindah.
**How to avoid:** Sisipkan badge di area existing antara `tree-label` dan `badge Aktif/Nonaktif` (renderNode L171-173). JANGAN ubah struktur `<li>` atau `.drag-handle`. JANGAN bungkus row dengan elemen baru.
**Warning signs:** Drag tidak bisa di-grab, atau ReorderBatch error "Jumlah item tidak sesuai" (controller L501).

### Pitfall 3: Pre-order DFS dropdown lupa exclude self + descendants (circular reparent)
**What goes wrong:** Edit modal mengizinkan user memilih dirinya sendiri / sub-unitnya sebagai induk → circular reference.
**Why it happens:** `flattenTreePreOrder` membangun ulang urutan dari `buildTree(_flatUnits)`; jika `.filter(!excludeIds.has(u.id))` tidak diterapkan ke hasil flatten, exclude hilang.
**How to avoid:** Spec L280-281 sudah benar — `flattenTreePreOrder(roots).filter(u => !excludeIds.has(u.id))`. `excludeIds` dibangun dari `getDescendantIds(excludeId)` + `add(excludeId)` (existing L290-291). Server tetap punya guard kedua: `IsDescendantAsync` (L168, L277-286) + `parentId == id` check (L160). Defense-in-depth: client UX exclude + server hard-reject.
**Warning signs:** Dropdown Edit menampilkan node yang sedang di-edit, atau anaknya.

### Pitfall 4: Label fetch timing — labelMap belum ready saat renderNode jalan
**What goes wrong:** Badge tier (ORG-TREE-10) render `undefined`/`Level N` karena `labelMap` belum ter-fetch saat `renderNode` dipanggil.
**Why it happens:** `initTree()` (L192) async dan langsung render setelah `GetOrganizationTree`. Jika label fetch jalan paralel tanpa await, race condition.
**How to avoid:** Init sequence yang benar:
```js
document.addEventListener('DOMContentLoaded', async () => {
  await fetchLabels();   // set labelMap module-var DULU
  await initTree();      // renderNode baca labelMap yang sudah ready
  renderLegend();        // legend dari labelMap
});
```
Saat ini view memanggil `initTree` langsung (L194 `document.addEventListener('DOMContentLoaded', initTree)`). Ganti ke wrapper async yang fetch label dulu. Atau: gabung fetch ke dalam `initTree` (await label sebelum `renderStats`/`buildTree`). `initTree` juga dipanggil ulang setelah CRUD (L355, L367, L394) — labelMap sudah cached, jadi cukup fetch sekali on-load (guard `if (!labelMap) await fetchLabels()`).
**Warning signs:** Badge kosong / "Level 0" saat first paint, benar setelah refresh.

### Pitfall 5: Dup-name per-parent — collation case-sensitivity SQL Server vs InMemory
**What goes wrong:** Test InMemory lolos tapi behavior beda di SQL Server, atau sebaliknya. `AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId)` — perbandingan string.
**Why it happens:** SQL Server default collation **case-insensitive** (`Latin1_General_CI_AS`) → "Operations" == "operations" dianggap duplikat. EF InMemory provider **case-sensitive** (ordinal) → "Operations" != "operations". (Pitfall 9 lineage Phase 341.)
**How to avoid:** Untuk **test correctness phase ini** (per-parent acceptance): gunakan nama dengan **casing identik** di skenario "ditolak di parent sama" dan "diterima di parent beda" agar deterministik di kedua provider. JANGAN tulis test yang bergantung pada case-insensitivity (mis. "Operations" vs "operations" → reject) di InMemory — itu akan false-pass. Dokumentasikan asumsi di test comment. Behavior produksi (SQL Server CI) adalah authoritative; case-insensitive dup-block adalah by-design dan tidak diubah phase ini.
**Warning signs:** Test pass lokal InMemory, gagal/beda di Dev SQL Server.

### Pitfall 6: ParentId form value "" → int? parsing
**What goes wrong:** `unitModalParent` value "" (root option, view L151) dikirim sebagai string kosong; binding ke `int? parentId` di PreviewEditCascade harus null, bukan error.
**Why it happens:** `submitUnitModal` (L338) ambil `.value` mentah; existing Add/Edit sudah handle (model binder konversi "" → null untuk `int?`). PreviewEditCascade signature `int? parentId` (spec L400) sama → aman. Tapi saat JS membandingkan `parentChanged`, hati-hati `"" !== null` di sisi client jika ada logic client.
**How to avoid:** Biarkan SERVER yang tentukan `parentChanged` (`unit.ParentId != parentId`, spec L407) — `parentId` null dari binder. JANGAN compute parentChanged di client (D-04: server authoritative).
**Warning signs:** PreviewEditCascade selalu report `parentChanged: true` untuk root unit yang tidak berubah.

### Pitfall 7: Level cap fix — JS class vs CSS keduanya harus extend
**What goes wrong:** Tambah CSS `.level-3/4/5` tapi JS `levelClass` (L123) masih `level <= 2 ? ... : 'level-2'` → level 3 tetap pakai warna level-2.
**Why it happens:** Dua tempat: CSS (view L37-39) DAN JS class generator (orgTree.js L123). Keduanya harus diubah.
**How to avoid:** JS: `const levelClass = 'level-' + (level <= 5 ? level : 5);` (cycling/cap di 5). CSS: tambah `.org-node-icon.level-3/4/5` dengan 6 warna palette spec L357-363.
**Warning signs:** Level 3+ node icon warnanya sama dengan level 2.

## Code Examples

### Code Example #1: Pre-order DFS dropdown + inactive visible (ORG-TREE-01, 03)
```javascript
// Source: spec §4.6 L263-291 (verbatim) — replace orgTree.js populateParentDropdown L287-303
function flattenTreePreOrder(roots) {
    const out = [];
    function walk(node, depth) {
        out.push({ ...node, depth });
        (node.children || []).forEach(c => walk(c, depth + 1));
    }
    roots.forEach(r => walk(r, 0));
    return out;
}

function populateParentDropdown(excludeId) {
    const select = document.getElementById('unitModalParent');
    select.innerHTML = '<option value="">— Tidak ada (root) —</option>';
    const excludeIds = excludeId ? getDescendantIds(excludeId) : new Set();  // existing helper L275
    if (excludeId) excludeIds.add(excludeId);

    const roots = buildTree(_flatUnits);                 // existing helper L47
    flattenTreePreOrder(roots)
        .filter(u => !excludeIds.has(u.id))              // Pitfall 3: anti-circular
        .forEach(u => {
            const indent = ' '.repeat(u.depth * 4); // NBSP — match existing indent style L297
            const suffix = u.isActive ? '' : ' (nonaktif)';  // ORG-TREE-03
            const opt = document.createElement('option');
            opt.value = u.id;
            opt.textContent = indent + u.name + suffix;
            if (!u.isActive) opt.style.color = '#999';
            select.appendChild(opt);
        });
}
```
**Catatan:** `buildTree` me-mutate `_flatUnits` (set `.children`), aman karena dipanggil ulang tiap render. Spec pakai `' '.repeat()` (spasi biasa) — research rekomendasikan `' '` (NBSP) agar konsisten dengan indent existing L297 (spasi biasa collapse di HTML option).

### Code Example #2: PreviewEditCascade endpoint (ORG-TREE-07)
```csharp
// Source: spec §4.7 L395-448 — new action di OrganizationController.cs (setelah EditOrganizationUnit L275)
// Predikat field-pair DIVERIFIKASI terhadap mutasi aktual L196-263.
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> PreviewEditCascade(int id, string name, int? parentId)
{
    var unit = await _context.OrganizationUnits.FindAsync(id);
    if (unit == null) return Json(new { error = "Unit tidak ditemukan." });

    var trimmed = (name ?? "").Trim();
    bool nameChanged = !string.IsNullOrEmpty(trimmed) && unit.Name != trimmed;
    bool parentChanged = unit.ParentId != parentId;     // server authoritative, Pitfall 6
    if (!nameChanged && !parentChanged)
        return Json(new { nameChanged = false, parentChanged = false });  // D-04 early-return

    int affectedUsers = 0, affectedMappings = 0, affectedKompetensi = 0, affectedGuidance = 0;
    string oldName = unit.Name;

    if (nameChanged)
    {
        if (unit.Level == 0)
        {
            affectedUsers      += await _context.Users.CountAsync(u => u.Section == oldName);
            affectedMappings   += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentSection == oldName);
            affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Bagian == oldName);
            affectedGuidance   += await _context.CoachingGuidanceFiles.CountAsync(g => g.Bagian == oldName);
        }
        else
        {
            affectedUsers      += await _context.Users.CountAsync(u => u.Unit == oldName);
            affectedMappings   += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentUnit == oldName);
            affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Unit == oldName);
            affectedGuidance   += await _context.CoachingGuidanceFiles.CountAsync(g => g.Unit == oldName);
        }
    }

    if (parentChanged && unit.Level >= 1 && !nameChanged)
    {
        // Reparent affected (lihat Pitfall 1: spec hanya count users; planner decide apakah lengkapi)
        affectedUsers += await _context.Users.CountAsync(u => u.Unit == oldName);
        // [ASSUMED] untuk akurasi 1:1, tambahkan juga mappings/kompetensi/guidance reparent:
        // affectedMappings   += await _context.CoachCoacheeMappings.CountAsync(m => m.AssignmentUnit == oldName);
        // affectedKompetensi += await _context.ProtonKompetensiList.CountAsync(k => k.Unit == oldName);
        // affectedGuidance   += await _context.CoachingGuidanceFiles.CountAsync(g => g.Unit == oldName);
    }

    return Json(new {
        nameChanged,
        parentChanged,
        affectedUsersCount = affectedUsers,
        affectedMappingsCount = affectedMappings,
        affectedKompetensiCount = affectedKompetensi,
        affectedGuidanceCount = affectedGuidance
    });
}
```

### Code Example #3: Cascade-confirm flow di submitUnitModal (ORG-TREE-07, D-04)
```javascript
// Source: spec §4.6 L315-326 frontend pattern — modify orgTree.js submitUnitModal L335 (Edit branch)
async function submitUnitModal() {
    const id = document.getElementById('unitModalId').value;
    const name = document.getElementById('unitModalName').value.trim();
    const parentId = document.getElementById('unitModalParent').value;
    if (!name) { document.getElementById('unitModalName').classList.add('is-invalid'); return; }

    const isEdit = id !== '';
    if (isEdit) {
        // D-04: ALWAYS call preview
        const pv = await ajaxPost('PreviewEditCascade', { id, name, parentId });
        const total = (pv.affectedUsersCount || 0) + (pv.affectedMappingsCount || 0)
                    + (pv.affectedKompetensiCount || 0) + (pv.affectedGuidanceCount || 0);
        if (total > 0) {
            // populate + show Bootstrap cascade-confirm modal (D-02, 4-line breakdown)
            const proceed = await showCascadeConfirm(pv);  // returns Promise<bool>
            if (!proceed) return;  // user Batal
        }
    }

    const url = isEdit ? 'EditOrganizationUnit' : 'AddOrganizationUnit';
    const data = isEdit ? { id, name, parentId } : { name, parentId };
    try {
        const result = await ajaxPost(url, data);
        bootstrap.Modal.getInstance(document.getElementById('unitModal')).hide();
        showToast(result.message, result.success ? 'success' : 'danger');
        if (result.success) await initTree();
    } catch { showToast('Terjadi kesalahan. Silakan coba lagi.', 'danger'); }
}
```
**Catatan planner:** `showCascadeConfirm(pv)` adalah helper baru yang isi 4 baris (`affectedUsersCount` dst → label "N user", "N mapping coach-coachee", "N kompetensi PROTON", "N file panduan", spec L319-325) ke modal markup baru, lalu return Promise yang resolve `true` saat [Lanjut Simpan], `false` saat [Batal]. Replicate pola Phase 341 modal.

### Code Example #4: Escape fix — data-attribute + event delegation (ORG-TREE-04)
```javascript
// Source: spec §4.6 L328-350 — di renderNode L150 ganti inline onclick:
// SEBELUM (Bug #3):
//   onclick="...openDeleteModal(${node.id}, '${escapeHtml(node.name)}', ...)"
// SESUDAH — markup:
`<a class="dropdown-item text-danger js-delete-trigger" href="#"
    data-id="${node.id}"
    data-name="${escapeHtml(node.name)}"
    data-child-count="${hasChildren ? node.children.length : 0}">
  <i class="bi bi-trash me-2"></i>Hapus
</a>`

// listener (tambah di DOMContentLoaded block, dekat L406 container.addEventListener):
container.addEventListener('click', e => {
    const del = e.target.closest('.js-delete-trigger');
    if (!del) return;
    e.preventDefault();
    openDeleteModal(parseInt(del.dataset.id), del.dataset.name, parseInt(del.dataset.childCount));
});
```
**Catatan:** `openDeleteModal(id, name, childCount)` signature L375 TIDAK berubah — hanya cara pemanggilan. Pertahankan `escapeHtml` di `data-name` value (HTML-attribute escape tetap perlu). Add/Edit/Toggle dropdown item lain (L146-148) boleh tetap inline onclick (tidak ada string interpolation berbahaya — hanya `${node.id}` numeric).

### Code Example #5: Dup-name per-parent (ORG-TREE-02)
```csharp
// Source: spec §4.7 L368-393 — OrganizationController.cs
// AddOrganizationUnit L85: SEBELUM
//   bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim());
// SESUDAH:
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId);

// EditOrganizationUnit L149: SEBELUM
//   bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim() && u.Id != id);
// SESUDAH:
bool duplicate = await _context.OrganizationUnits
    .AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId && u.Id != id);
```
**Catatan:** Pada Edit, `parentId` adalah nilai BARU dari form (rename+reparent dalam 1 submit aman, spec L393). EF translate `u.ParentId == parentId` dengan `parentId` null → `IS NULL` (root-level dup-check). Pitfall 5 untuk casing test.

### Code Example #6: Level palette extend + badge tier (ORG-TREE-05, 10)
```css
/* Source: spec §4.6 L356-363 — ManageOrganization.cshtml, tambah setelah L39 */
.org-node-icon.level-3 { background: rgba(25,135,84,0.1);  color: #198754; }
.org-node-icon.level-4 { background: rgba(255,193,7,0.1);  color: #b45309; }
.org-node-icon.level-5 { background: rgba(220,53,69,0.1);  color: #dc3545; }
```
```javascript
// orgTree.js renderNode L123 — level cap fix (Pitfall 7):
const levelClass = 'level-' + (level <= 5 ? level : 5);

// badge tier (ORG-TREE-10) — sisip antara tree-label dan badge Aktif/Nonaktif (renderNode L171-173).
// D-03: warna badge reuse level palette. label dari labelMap (D-01).
const tierLabel = getLabelForLevel(level);   // helper: labelMap[level] ?? `Level ${level}`
const tierBadge = `<span class="badge org-tier-badge level-${level <= 5 ? level : 5}">${escapeHtml(tierLabel)}</span>`;
// JANGAN ubah struktur <li>/.drag-handle (Pitfall 2).
```
**Catatan planner:** `.org-tier-badge` class baru untuk styling badge yang mereuse warna level (background rgba + text color). Pilih exact CSS per palette reuse D-03 (Claude's discretion).

### Code Example #7: Modal title dynamic + path breadcrumb (ORG-TREE-09, 06)
```javascript
// getLabelForLevel helper (label fetch — Pitfall 4)
let labelMap = null;
async function fetchLabels() { if (!labelMap) labelMap = await ajaxGet('GetLevelLabels'); }
function getLabelForLevel(level) { return (labelMap && labelMap[level]) || `Level ${level}`; }

// Modal title dynamic (ORG-TREE-09) — di openAddModal L307 / select.change:
// child level = (parent ? parent.level + 1 : 0)
function setModalTitleForParent(parentIdVal) {
    const parent = parentIdVal ? findUnit(parseInt(parentIdVal)) : null;  // findUnit existing L271
    const childLevel = parent ? parent.level + 1 : 0;
    const verb = document.getElementById('unitModalId').value ? 'Edit' : 'Tambah';
    document.getElementById('unitModalLabel').textContent = `${verb} ${getLabelForLevel(childLevel)}`;
}

// Path breadcrumb (ORG-TREE-06) — select#unitModalParent change listener:
function renderModalPath(parentIdVal) {
    const el = document.getElementById('unitModalPath');   // <div id="unitModalPath"> baru di modal markup
    if (!parentIdVal) { el.textContent = ''; return; }
    const chain = [];
    let cur = findUnit(parseInt(parentIdVal));
    while (cur) { chain.unshift(cur.name); cur = cur.parentId ? findUnit(cur.parentId) : null; }
    el.textContent = 'Path: ' + chain.join(' → ') + ' → (unit baru di sini)';
}
// wire: document.getElementById('unitModalParent').addEventListener('change', e => {
//   setModalTitleForParent(e.target.value); renderModalPath(e.target.value);
// });  // pasang sekali on-load, bukan tiap open
```
**Catatan:** Tambah `<div class="text-muted small mt-1" id="unitModalPath"></div>` di modal body dekat `unitModalParent` select (view L153). Planner picks exact string (Claude's discretion).

### Code Example #8: Legend card-header (ORG-TREE-08)
```javascript
// render dari labelMap (D-01) ke card-header (view L114-122).
function renderLegend() {
    const host = document.getElementById('org-legend');  // <div id="org-legend"> baru di card-header
    if (!host || !labelMap) return;
    const levels = Object.keys(labelMap).map(Number).sort((a,b)=>a-b);
    host.innerHTML = levels.map(lv =>
        `<span class="org-legend-item"><span class="org-legend-swatch level-${lv <= 5 ? lv : 5}"></span>${escapeHtml(labelMap[lv])}</span>`
    ).join('');
}
```
**Catatan:** Swatch warna = level palette (CSS reuse `.org-node-icon.level-N` background, atau class `.org-legend-swatch.level-N` baru). Card-header existing (view L114-122) punya 2 kolom flex; tambah legend block di bawahnya atau di kolom kiri (spec L240-246 mockup inline `▣ Bagian ▣ Unit ▣ Sub-unit`).

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Inline `onclick` string interpolation untuk delete | data-attribute + event delegation | Phase 342 (Bug #3 fix) | XSS-safe, name dengan kutip tidak break |
| Dropdown sort flat `level → displayOrder` | pre-order DFS flatten | Phase 342 (Bug B) | Hierarki dropdown intuitif |
| Dup-name global | dup-name per-parent | Phase 342 (Bug #1) | "Operations" boleh di 2 Bagian |
| Tier hanya warna icon (no text) | badge tier label + legend | Phase 342 (ORG-TREE-08/10) | Label tier eksplisit, sumber `GetLevelLabels` |
| Edit cascade silent (no preview) | PreviewEditCascade modal | Phase 342 (ORG-TREE-07) | User lihat dampak sebelum apply |

**Deprecated/outdated:** Tidak ada — phase ini menambah/memperbaiki, tidak men-deprecate fitur existing. `renderStats` stat-bar (orgTree.js L79-107) label "Bagian"/"Total Unit"/"Aktif" tetap statis (bukan tier badge, OUT of scope D-01).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Spec PreviewEditCascade reparent hanya count `affectedUsers`; research rekomendasikan lengkapi mappings/kompetensi/guidance untuk akurasi 1:1 dengan mutasi aktual | Pitfall 1, Code Example #2 | Preview count under-report saat reparent menyentuh secondary fields. LOW risk (rename adalah kasus dominan; reparent jarang). Planner pilih spec-verbatim vs akurasi-penuh. |
| A2 | 1 fetch label on-load cukup (page reload after CRUD) — tidak perlu re-fetch | Pattern 2, Pitfall 4 | Stale label kalau label di-rename di tab lain tanpa reload. Acceptable (CONTEXT Claude's discretion + spec L503 manual refresh OK). |
| A3 | NBSP ` ` untuk indent dropdown (vs spasi biasa di spec) agar tidak collapse di HTML option | Code Example #1 | Kosmetik. Spec pakai spasi biasa; research rekomendasikan NBSP konsisten dengan existing L297. |

**Catatan:** Semua klaim teknis lain (line number, field name, signature, predikat cascade, test infra) DIVERIFIKASI langsung dari file aktual sesi ini — bukan assumed.

## Open Questions (RESOLVED)

1. **PreviewEditCascade reparent secondary-field count (A1)** — RESOLVED: pilih opsi (b) full-accuracy 4 field-pair (Plan 01-T2 A1 DECISION; enforced by Plan 03 preview==actual test).
   - What we know: mutasi aktual Edit reparent (L247-262) update Users+Mappings+Kompetensi+Guidance; spec preview L433-438 hanya count Users.
   - What's unclear: apakah under-report reparent secondary fields acceptable (spec verbatim) atau harus 1:1.
   - Recommendation: lengkapi count (akurasi penuh) + test count==actual; jika planner pilih spec-verbatim, kalibrasi test reparent agar hanya assert Users count.

2. **Legend placement di card-header** — RESOLVED: baris terpisah di bawah card-header row (Plan 02-T3 Step 2).
   - What we know: card-header punya 2 kolom flex (view L114-122): kiri "Hierarki Organisasi" dot, kanan "Drag untuk reorder".
   - What's unclear: legend jadi baris ke-3 di card-header, atau inline di kolom kanan.
   - Recommendation: baris terpisah `<div id="org-legend">` di bawah header row (spec mockup L240-246 menunjukkan legend block terpisah). Claude's discretion.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | build + test | ✓ (asumsi — project ter-build, ada bin/Debug/net8.0) | net8.0 | — |
| GET /Admin/GetLevelLabels | label fetch (D-01) | ✓ live | Phase 340 | hardcoded fallback `Level N` di JS |
| OrgLabelService | label source | ✓ | Phase 340 | service registered Program.cs |
| EF InMemory provider | unit test | ✓ | 8.0.0 | — |
| SortableJS CDN | drag-reorder | ✓ | 1.15.7 | existing, jangan break |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** `GetLevelLabels` unreachable → JS `getLabelForLevel` fallback `Level N` (graceful degrade, badge tetap render).

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia** untuk semua UI copy + commit message + response. Code/path/technical term tetap English.
- **Develop Workflow:** Lokal → Dev (10.55.3.3) → Prod. Verifikasi lokal `dotnet build` + `dotnet run` (`http://localhost:5277`) + cek DB lokal + Playwright bila ada SEBELUM commit/push.
- **No migration this phase** — REQUIREMENTS L67 (Cascade rename dipertahankan, no schema change). Tidak perlu `dotnet ef migrations` / IT_NOTIFY migration flag.
- **Seed Data Workflow:** tidak relevan (no seed baru phase ini). Jika butuh seed test temporary, snapshot DB + journal + restore (docs/SEED_WORKFLOW.md).
- **Jangan edit kode/DB langsung di Dev/Prod. Jangan push tanpa verifikasi lokal.**

## Validation Architecture

> nyquist_validation: true (config) — section wajib.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 [VERIFIED: HcPortal.Tests.csproj L14] |
| Config file | none (xUnit auto-discovery; `IsTestProject=true` L8) |
| In-memory DB | Microsoft.EntityFrameworkCore.InMemory 8.0.0 [VERIFIED: L12] |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationController"` |
| Full suite command | `dotnet test` (dari root) |

**Catatan scope:** Test formal (xUnit pre-order DFS, dup-per-parent, preview accuracy) secara FORMAL adalah Phase 344 (TEST-03/04, REQUIREMENTS L45-46; CONTEXT Deferred). Namun nyquist_validation aktif → Phase 342 SEBAIKNYA tetap menulis test backend yang murah (pre-order DFS via testable JS-mirror tidak praktis di xUnit; fokus ke **backend** dup-name + preview-count yang langsung testable). Planner decide: tulis test backend di Phase 342 (sampling rate per commit) ATAU tandai sebagai Wave 0 gap yang di-handoff ke Phase 344. Rekomendasi: tulis test **dup-name per-parent** + **preview-count==actual** di Phase 342 (cheap, langsung mengunci ORG-TREE-02 + ORG-TREE-07 correctness); pre-order DFS adalah JS — verifikasi via Playwright smoke (Phase 344) atau extract logic ke fungsi murni jika ingin xUnit.

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ORG-TREE-02 | dup-name per-parent: same name OK di parent beda, reject di parent sama | unit (controller, InMemory) | `dotnet test --filter "AddOrganizationUnit_DuplicateNameSameParent"` | ❌ Wave 0 |
| ORG-TREE-02 | Edit rename: reject dup di parent sama (exclude self) | unit | `dotnet test --filter "EditOrganizationUnit_DuplicateNameSameParent"` | ❌ Wave 0 |
| ORG-TREE-07 | preview count == actual mutated count (rename Level 0) | unit | `dotnet test --filter "PreviewEditCascade_RenameLevel0_CountMatchesActual"` | ❌ Wave 0 |
| ORG-TREE-07 | preview count == actual mutated count (rename Level 1+) | unit | `dotnet test --filter "PreviewEditCascade_RenameLevel1_CountMatchesActual"` | ❌ Wave 0 |
| ORG-TREE-07 | no-change early-return `{nameChanged:false, parentChanged:false}` | unit | `dotnet test --filter "PreviewEditCascade_NoChange_EarlyReturn"` | ❌ Wave 0 |
| ORG-TREE-01 | pre-order DFS order correctness (3-level multi-root) | unit (jika logic di-extract) ATAU Playwright (Phase 344) | — | ❌ Wave 0 / defer 344 |
| ORG-TREE-05/06/08/09/10 | visual/UX (badge, legend, title, path, palette) | manual smoke / Playwright | browser `http://localhost:5277` | manual |

### Assertion Patterns (replicate dari OrgLabelControllerTests pattern existing)

**Fixture factory (InMemory + UserManager null-substitute):**
```csharp
// Source pattern: HcPortal.Tests/OrgLabelControllerTests.cs L27-64 (VERIFIED existing)
private static (OrganizationController ctrl, ApplicationDbContext ctx) MakeController()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())   // isolasi per-test
        .Options;
    var ctx = new ApplicationDbContext(options);
    // seed OrganizationUnits + Users/Mappings/Kompetensi/Guidance sesuai skenario
    ctx.SaveChanges();
    var auditLog = new AuditLogService(ctx);
    #pragma warning disable CS8625
    var ctrl = new OrganizationController(ctx, null!, auditLog, null!);  // UserManager/env null
    #pragma warning restore CS8625
    ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
    return (ctrl, ctx);
}
private static bool GetSuccess(IActionResult r) {
    var j = Assert.IsType<JsonResult>(r);
    return (bool)j.Value!.GetType().GetProperty("success")!.GetValue(j.Value)!;
}
private static int GetInt(IActionResult r, string prop) {
    var j = Assert.IsType<JsonResult>(r);
    return (int)j.Value!.GetType().GetProperty(prop)!.GetValue(j.Value)!;
}
```
**Catatan UserManager null:** Add/Edit dup-check return SEBELUM `_userManager` digunakan (controller tidak panggil GetUserAsync di Add/Edit — verifikasi: EditOrganizationUnit hanya pakai `_userManager` di DeleteOrganizationUnit L425, BUKAN Edit). PreviewEditCascade juga tidak pakai `_userManager`. Jadi `null!` aman untuk semua test phase ini. Jika `AdminBaseController` ctor men-dereference userManager, ganti dengan substitusi minimal (cek constructor base sebelum plan).

**Pattern A — dup-name per-parent (ORG-TREE-02):**
```csharp
[Fact]
public async Task AddOrganizationUnit_SameNameDifferentParent_Accepted()
{
    var (ctrl, ctx) = MakeController();
    // 2 Bagian (root), masing-masing punya "Operations" sebagai child → harus diizinkan
    ctx.OrganizationUnits.AddRange(
        new OrganizationUnit { Id=1, Name="RFCC", Level=0, ParentId=null, IsActive=true },
        new OrganizationUnit { Id=2, Name="HSC",  Level=0, ParentId=null, IsActive=true },
        new OrganizationUnit { Id=3, Name="Operations", Level=1, ParentId=1, IsActive=true });
    ctx.SaveChanges();
    // Tambah "Operations" di bawah HSC (parent 2) → tidak duplikat (parent beda)
    var result = await ctrl.AddOrganizationUnit("Operations", 2);
    Assert.True(GetSuccess(result));
}

[Fact]
public async Task AddOrganizationUnit_SameNameSameParent_Rejected()
{
    var (ctrl, ctx) = MakeController();
    ctx.OrganizationUnits.AddRange(
        new OrganizationUnit { Id=1, Name="RFCC", Level=0, ParentId=null, IsActive=true },
        new OrganizationUnit { Id=3, Name="Operations", Level=1, ParentId=1, IsActive=true });
    ctx.SaveChanges();
    var result = await ctrl.AddOrganizationUnit("Operations", 1);   // parent sama
    Assert.False(GetSuccess(result));
}
// Pitfall 5: gunakan casing IDENTIK ("Operations" vs "Operations") — JANGAN andalkan
// case-insensitivity (InMemory case-sensitive, SQL Server CI). Beda casing = false-pass di InMemory.
```

**Pattern B — preview count == actual (ORG-TREE-07, kunci Pitfall 1):**
```csharp
[Fact]
public async Task PreviewEditCascade_RenameLevel0_CountMatchesActual()
{
    var (ctrl, ctx) = MakeController();
    ctx.OrganizationUnits.Add(new OrganizationUnit { Id=1, Name="RFCC", Level=0, ParentId=null, IsActive=true });
    // seed denormalized: N user Section="RFCC", N mapping, N kompetensi, N guidance
    ctx.Users.AddRange(/* 3 user dgn Section="RFCC" */);
    ctx.CoachCoacheeMappings.AddRange(/* 2 AssignmentSection="RFCC" */);
    ctx.ProtonKompetensiList.AddRange(/* 4 Bagian="RFCC" */);
    ctx.CoachingGuidanceFiles.AddRange(/* 1 Bagian="RFCC" */);
    ctx.SaveChanges();

    // ACT 1: preview (read-only count)
    var preview = await ctrl.PreviewEditCascade(1, "Refinery Complex", null);
    int pUsers = GetInt(preview, "affectedUsersCount");      // expect 3
    int pMap   = GetInt(preview, "affectedMappingsCount");   // expect 2
    int pKomp  = GetInt(preview, "affectedKompetensiCount"); // expect 4
    int pGuid  = GetInt(preview, "affectedGuidanceCount");   // expect 1

    // ACT 2: actual edit (mutasi) — fresh ctx ATAU re-query count setelah Edit
    await ctrl.EditOrganizationUnit(1, "Refinery Complex", null);
    int aUsers = ctx.Users.Count(u => u.Section == "Refinery Complex");
    // ... dst per field
    // ASSERT: preview count == jumlah baris yang BERUBAH ke nama baru
    Assert.Equal(pUsers, aUsers);   // kunci akurasi: preview == actual
    // (mappings/kompetensi/guidance idem)
}
```
**Catatan implementasi test B:** karena Preview dan Edit berbagi `ctx` yang sama, urutkan: preview DULU (sebelum mutasi), capture count, lalu Edit, lalu hitung baris yang sekarang bernama baru. Equality `pUsers == aUsers` mengunci predikat tidak drift (Pitfall 1). Untuk skenario reparent, sesuaikan dengan keputusan A1.

**Pattern C — early-return no-change (D-04):**
```csharp
[Fact]
public async Task PreviewEditCascade_NoChange_ReturnsEarlyFalseFlags()
{
    var (ctrl, ctx) = MakeController();
    ctx.OrganizationUnits.Add(new OrganizationUnit { Id=1, Name="RFCC", Level=0, ParentId=null, IsActive=true });
    ctx.SaveChanges();
    var result = await ctrl.PreviewEditCascade(1, "RFCC", null);   // nama+parent sama
    var j = Assert.IsType<JsonResult>(result);
    Assert.False((bool)j.Value!.GetType().GetProperty("nameChanged")!.GetValue(j.Value)!);
    Assert.False((bool)j.Value!.GetType().GetProperty("parentChanged")!.GetValue(j.Value)!);
}
```

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrganizationController"` (target < 5s, InMemory).
- **Per wave merge:** `dotnet test` (full suite — sertakan OrgLabel tests existing 18 test, jangan regress).
- **Phase gate:** full suite green + `dotnet build` + browser smoke (`http://localhost:5277`) per CLAUDE.md step 3, sebelum commit/push.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/OrganizationControllerTests.cs` — file BARU (belum ada). Covers ORG-TREE-02 (Pattern A) + ORG-TREE-07 (Pattern B + C).
- [ ] Verifikasi `AdminBaseController` constructor TIDAK men-dereference `userManager`/`env` saat null (cek sebelum pakai `null!` di fixture). Jika men-dereference, sediakan substitusi minimal.
- [ ] Pre-order DFS (ORG-TREE-01): JS-only. Opsi (a) extract `flattenTreePreOrder` ke fungsi murni + test JS (tidak ada JS test runner di project — defer); opsi (b) Playwright smoke Phase 344. Rekomendasi: dokumentasikan order expectation di kode + Playwright defer 344.
- Framework install: NONE — xUnit + InMemory sudah terpasang.

*(Tidak ada gap framework — hanya file test baru + 1 verifikasi constructor.)*

## Sources

### Primary (HIGH confidence — dibaca langsung sesi ini)
- `Controllers/OrganizationController.cs` (full, 521 baris) — cascade L191-263, dup-check L85/L149, IsDescendantAsync L277, dual-response pattern
- `wwwroot/js/orgTree.js` (full, 469 baris) — populateParentDropdown L287, renderNode L118, getDescendantIds L275, escape bug L150, level cap L123, init L401
- `Views/Admin/ManageOrganization.cshtml` (full, 197 baris) — card-header L114, CSS palette L37-39, modal L134, scripts L189
- `Controllers/OrgLabelController.cs` — GET /Admin/GetLevelLabels L42-48 (label source D-01)
- `Services/IOrgLabelService.cs` — service contract
- `HcPortal.Tests/OrgLabelControllerTests.cs` + `OrgLabelServiceTests.cs` — fixture/assertion pattern (InMemory, JsonResult reflection, UserManager null)
- `HcPortal.Tests/HcPortal.Tests.csproj` — xUnit 2.9.3, InMemory 8.0.0
- `Models/CoachCoacheeMapping.cs` (AssignmentSection L39, AssignmentUnit L44), `Models/ProtonModels.cs` (ProtonKompetensi.Bagian/Unit L30-31, CoachingGuidanceFile.Bagian/Unit L193-194), `Models/ApplicationUser.cs` (Section L28, Unit L33), `Models/OrganizationUnit.cs`
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.6/§4.7 (PRIMARY spec)
- `.planning/config.json` — nyquist_validation true, commit_docs true

### Secondary (MEDIUM confidence)
- `.planning/phases/342-manageorganization-page-fixes/342-CONTEXT.md` — D-01..D-04 + spec-locked items
- `.planning/milestones/v21.0-REQUIREMENTS.md` — ORG-TREE-01..10 definitions

### Tertiary (LOW confidence)
- None — no WebSearch needed (closed-system codebase research).

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua versi dibaca dari .csproj aktual, no new dependency.
- Architecture: HIGH — cascade predikat + line number + signature diverifikasi field-by-field terhadap file aktual.
- Pitfalls: HIGH — Pitfall 1 (predicate drift) + Pitfall 5 (collation) + Pitfall 2 (Sortable) grounded di kode aktual; A1 reparent-count adalah satu-satunya ambiguitas (FLAG planner).

**Research date:** 2026-06-03
**Valid until:** 2026-06-17 (stable — closed codebase, no fast-moving external dependency; re-verify line numbers jika file berubah sebelum planning).
