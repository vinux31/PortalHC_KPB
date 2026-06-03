# Phase 342: ManageOrganization Page Fixes - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning
**Milestone:** v21.0 (ManageOrganization Overhaul + Level Label CRUD)
**Predecessor:** Phase 341 SHIPPED LOCAL (Label CRUD Page — OrgLabelController + view + tests)

<domain>
## Phase Boundary

Page `/Admin/ManageOrganization` clean dari **4 bug** + **4 inovasi UX**, ORG-TREE-01..10:

**Bug fixes:**
- ORG-TREE-01: Dropdown induk modal Tambah/Edit terurut pre-order DFS (parent → keturunan → sibling), bukan flat per level (Bug B).
- ORG-TREE-02: Validasi nama unit unique **per-parent**, bukan global ("Operations" boleh di 2 Bagian beda).
- ORG-TREE-03: Parent nonaktif visible di dropdown (suffix " (nonaktif)" + grey), tidak disembunyikan.
- ORG-TREE-04: `openDeleteModal` pakai `data-name` attribute + event delegation (fix Bug #3 escape kotor).
- ORG-TREE-05: Icon color palette extend level 3-5 (cycling 6 warna), tidak cap hardcoded level 2 (Bug #4).

**UX innovations:**
- ORG-TREE-06: Path breadcrumb real-time saat pilih induk ("RFCC → LPG Treating → (unit baru)").
- ORG-TREE-07: `POST /Admin/PreviewEditCascade` count affected Users/Mappings/Kompetensi/Guidance + modal konfirmasi sebelum apply Edit.
- ORG-TREE-08: Legend warna ↔ tier label di card header.
- ORG-TREE-09: Modal title dynamic ("Tambah Bagian"/"Tambah Unit"/"Tambah Sub-unit", fallback "Level N").
- ORG-TREE-10: Tree row badge tier label per node.

**Out of scope:**
- Integrasi label app-wide di 7 area page (Phase 343 — ORG-INTEG-01/02)
- Test + UAT formal (Phase 344 — TEST-01..06)
- Perubahan schema/migration (none — consume Phase 340 service)

**KEY codebase finding:** `EditOrganizationUnit` (OrganizationController.cs L191-263) SUDAH cascade rename + reparent ke denormalized fields (Users.Section/Unit, CoachCoacheeMappings, ProtonKompetensiList, CoachingGuidanceFiles). ORG-TREE-07 = **warning/preview layer saja** (count sebelum apply) — cascade actual sudah jalan, NO data-integrity fix needed.
</domain>

<decisions>
## Implementation Decisions

### Discussed (gray areas resolved this session)

- **D-01 (Sumber label tier):** legend + tree row badge + modal title pakai **JS fetch `GET /Admin/GetLevelLabels`** (Phase 340 endpoint, client-render). Page sudah AJAX-driven (orgTree.js fetch tree) → label dari endpoint sama natural; label live tanpa server-inject. Server `@inject IOrgLabelService` app-wide = scope Phase 343 (page lain), BUKAN page ini. Spec §4 line 114 align.
- **D-02 (UI konfirmasi cascade ORG-TREE-07):** **Bootstrap modal** dengan 4-line count breakdown (user/mapping/kompetensi/guidance) + tombol Batal / Lanjut Simpan. Per spec mockup §4.6 L318-326. Native `confirm()` Phase 341 D-03 reserved untuk single-line delete; multi-count warning butuh modal yang readable.
- **D-03 (Visual badge tier per row):** **Reuse palette warna level** — badge background pakai warna level yang sama dengan icon (CSS `.org-node-icon.level-0..5` sudah ada / extend). Link visual badge↔icon, perkuat sistem warna tier (legend + icon + badge koheren).
- **D-04 (Trigger PreviewEditCascade):** **Selalu call** endpoint saat user klik Simpan di Edit modal; endpoint early-return `{nameChanged:false, parentChanged:false}` kalau no change (murah); modal konfirmasi muncul HANYA kalau ada `affected*Count > 0`, lalu lanjut `EditOrganizationUnit`. Server authoritative (count akurat, reuse logic identik EditOrganizationUnit cascade); no client-side duplicate logic.

### Spec-locked (verbatim dari design spec §4.6/§4.7 — planner: implement as given, BUKAN gray area)

- Pre-order DFS sort: `flattenTreePreOrder(roots)` + `populateParentDropdown(excludeId)` — code verbatim spec L263-291.
- Inactive parent fix: hapus `.filter(u => u.isActive)`, ganti suffix " (nonaktif)" + `opt.style.color='#999'` (spec L294).
- Escape fix: `data-id`/`data-name`/`data-child-count` attribute + `container.addEventListener('click', ...closest('.js-delete-trigger'))` event delegation (spec L328-350).
- Level cap palette: 6-warna cycling CSS `.org-node-icon.level-0..5`, level 6+ fallback level-5 (spec L352-364).
- Dup-name per-parent: `AnyAsync(u => u.Name == name.Trim() && u.ParentId == parentId [&& u.Id != id])` (spec L368-393).
- `PreviewEditCascade(int id, string name, int? parentId)` endpoint: `[HttpPost][Authorize(Roles="Admin, HC")][ValidateAntiForgeryToken]`, count reuse EditOrganizationUnit Section/Unit branch logic (spec L395-436+).
- Modal title dynamic: label level N+1 dari GetLevelLabels, fallback "Level N+1" (spec L255-259).

### Claude's Discretion
- Path breadcrumb (ORG-TREE-06) markup/styling: `<div class="text-muted small mt-1" id="unitModalPath">` render on `select` change — planner picks exact render string.
- Bootstrap modal id/aria conventions untuk cascade confirm modal — replicate Phase 341 modal pattern.
- Badge markup (`<span class="badge ...">`) exact class — planner picks per palette reuse D-03.
- Apakah legend/badge label di-cache di JS (1 fetch on page load) vs re-fetch — planner decides (1 fetch on load cukup, page reload after CRUD).

### Folded Todos
None — `todo match-phase 342` returned 0 matches.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec (PRIMARY — implementation locked here)
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.6 "Page ManageOrganization changes" (L237-364) — legend, badge, modal title, pre-order DFS, path preview, cascade warning, escape fix, level cap (ALL code verbatim)
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.7 "Controller fixes" (L366+) — dup-name per-parent + PreviewEditCascade endpoint code
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §3 "Goals" (L13-40) — bug inventory + UX goals reasoning

### Milestone
- `.planning/milestones/v21.0-ROADMAP.md` §"Phase 342" (L61-72) — goal + 5 success criteria
- `.planning/milestones/v21.0-REQUIREMENTS.md` ORG-TREE-01..10 (L24-33) — REQ definitions

### Phase 340/341 deliverables (consume)
- `Services/IOrgLabelService.cs` / `Services/OrgLabelService.cs` — `GetLabel(int)`/`GetAll()` for label render
- `Controllers/OrgLabelController.cs` — `GET /Admin/GetLevelLabels` endpoint (any-auth JSON, D-01 source)
- `.planning/phases/341-label-crud-page/341-CONTEXT.md` — D-01 fetch+JSON, D-03 native confirm, D-07 shared-toast conventions

### Codebase patterns (replicate/extend)
- `Views/Admin/ManageOrganization.cshtml` — shell + AJAX tree (v13.0 refactor); add legend + dynamic title
- `wwwroot/js/orgTree.js` — buildTree/renderNode/populateParentDropdown/openDeleteModal/getAntiForgeryToken; fix sort + path preview + cascade warning + escape + level cap
- `Controllers/OrganizationController.cs` — `EditOrganizationUnit` L128-275 (cascade already exists), `AddOrganizationUnit` L74-122, dup-name fix + new PreviewEditCascade

### Developer workflow
- `docs/DEV_WORKFLOW.md` — Lokal → Dev → Prod SOP
- `CLAUDE.md` — Bahasa Indonesia, Seed/Migration workflow (no migration this phase)
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrgLabelService.GetAll()` / `GET /Admin/GetLevelLabels` — label dict source untuk legend/badge/title (D-01)
- `wwwroot/js/orgTree.js` — existing `buildTree(_flatUnits)`, `renderNode`, `populateParentDropdown`, `getAntiForgeryToken`, `escapeHtml`, AJAX tree fetch — extend in-place (jangan rewrite)
- `EditOrganizationUnit` cascade logic (L191-263) — IDENTICAL count logic untuk reuse di PreviewEditCascade (DRY)
- `IsDescendantAsync` / `UpdateChildrenLevelsAsync` (OrganizationController) — circular-ref guard sudah ada
- `wwwroot/js/shared-toast.js` `showToast(msg, type)` — feedback after AJAX (D-07 Phase 341)
- Existing CSS `.org-node-icon.level-0/1/2` — extend level-3/4/5 (D-03 palette reuse)

### Established Patterns
- AJAX tree: orgTree.js fetch `GET /Admin/GetOrganizationTree` → buildTree → renderNode (client-render). Label fetch follows same client pattern (D-01).
- JSON action return: `Json(new { success, message })` + `IsAjaxRequest()` dual-response (OrganizationController convention)
- Antiforgery: `getAntiForgeryToken()` JS + `[ValidateAntiForgeryToken]` (Phase 341 D-06 parity)
- SortableJS drag-reorder (v13.0) — JANGAN break saat tambah badge/legend (regression concern Phase 344)

### Integration Points
- `Views/Admin/ManageOrganization.cshtml` card header → tambah legend block
- orgTree.js `renderNode` → tambah badge span + data-* attributes
- orgTree.js modal open handlers → dynamic title + path preview + cascade confirm modal
- `OrganizationController` → 2 dup-check edits + 1 new PreviewEditCascade action
- Page reads `GET /Admin/GetLevelLabels` on load → populate label map untuk JS render
</code_context>

<specifics>
## Specific Ideas

- Legend layout: card header inline `▣ Bagian ▣ Unit ▣ Sub-unit` (spec L243-246 mockup), warna swatch = palette level color.
- Badge: `RFCC [Bagian] 2 unit [Aktif] ⋮` (spec L251-252), badge color = level palette (D-03).
- Cascade modal copy (spec L319-325): "Perubahan ini akan mempengaruhi: N user, N mapping coach-coachee, N kompetensi PROTON, N file panduan. Lanjutkan?" [Batal] [Lanjut Simpan].
- Path preview: "Path: RFCC → LPG Treating → (unit baru di sini)" (spec L299).
- ALL UI copy Bahasa Indonesia (CLAUDE.md).
</specifics>

<deferred>
## Deferred Ideas

- App-wide label integration di 7 area page (CMP/CDP/Worker/CoachMapping/ProtonData/Renewal/DocumentAdmin) — **Phase 343** (ORG-INTEG-01/02), pakai `@inject IOrgLabelService` server-render.
- Formal xUnit (pre-order DFS, dup per-parent, PreviewEditCascade accuracy) + Playwright E2E + manual UAT 5 scenario — **Phase 344** (TEST-01..06). Phase 342 = browser smoke verify saja.
- Regression smoke drag-reorder/toggle/delete/add — **Phase 344** ORG-INTEG-03.

### Reviewed Todos (not folded)
None — no todo matches for Phase 342.
</deferred>

---

*Phase: 342-manageorganization-page-fixes*
*Context gathered: 2026-06-03*
