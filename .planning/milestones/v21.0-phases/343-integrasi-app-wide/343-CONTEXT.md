# Phase 343: Integrasi App-wide - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning
**Milestone:** v21.0 (ManageOrganization Overhaul + Level Label CRUD)
**Predecessor:** Phase 342 SHIPPED LOCAL (ManageOrganization Page Fixes); Phase 341 (Label CRUD), Phase 340 (OrgLabelService)

<domain>
## Phase Boundary

Ganti hardcoded display string "Bagian"/"Unit"/"Sub-unit" → `@inject IOrgLabelService` + `@OrgLabels.GetLabel(N)` di Razor view (+ controller display string) app-wide, supaya rename label via page CRUD (Phase 341) muncul di SEMUA page Portal HC, bukan hanya ManageOrganization. ORG-INTEG-01 (view) + ORG-INTEG-02 (controller display string).

**Level mapping convention:** `GetLabel(0)`="Bagian", `GetLabel(1)`="Unit", `GetLabel(2)`="Sub-unit" (cached, fallback "Level N" bila tabel kosong).

**SKIP (spec §4.8 — JANGAN ganti):**
- Audit log message body (stabil untuk debug).
- Model/DB column/property name (`User.Section`, `User.Unit`, `.Bagian`, `.AssignmentUnit` — schema unchanged).
- Migration script literals.
- Test strings (xUnit + Playwright — label final deterministik).
- Unit NAMES (e.g. "RFCC LPG Treating Unit") — itu data, bukan tier label.

**Out of scope:**
- Formal test + UAT 5 scenario → Phase 344 (TEST-01..06, ORG-INTEG-03 regression).
- ManageOrganization page label (Phase 342 — pakai JS-fetch GetLevelLabels, BUKAN @inject).
- Schema/migration change (none).
</domain>

<decisions>
## Implementation Decisions

### Discussed (gray areas resolved this session)

- **D-01 (@inject placement):** Tambah `@inject HcPortal.Services.IOrgLabelService OrgLabels` **global di `Views/_ViewImports.cshtml`** (1 baris, semua view dapat `OrgLabels` otomatis). Spec §5 aligned. Service Scoped + IMemoryCache (murah, 1 inject app-wide OK). File `_ViewImports.cshtml` sudah ada (belum ada @inject custom). NO per-view @inject repetitif.
- **D-02 (Audit filosofi ganti-vs-skip):** **Pragmatic high-value** — ganti HANYA display label yang berubah makna user-facing saat rename: filter label (`<label>Bagian</label>`), table header (`<th>Bagian</th>`), form field label, breadcrumb, dropdown label. SKIP: help/doc table (deskripsi field), technical context, ambiguous. Fokus SC2 (rename muncul di page), minimize risk over-replace.
- **D-03 (Scope area realistis):** **Audit-driven actual** — scope = occurrence grep aktual. Konsentrasi: `Views/CMP/*` (filter+th, mis. AnalyticsDashboard L84/94/548, RecordsTeam L20/50/135/136), `Views/CDP/*`, `Views/ProtonData/*`, `Views/Admin/*` (org/worker forms). Worker/CoachMapping/Renewal/DocumentAdmin Views folder = "Bagian"/"Unit" 0 occurrence → **audit-only** (dokumentasi temuan di SC1, no forced change; cek juga apakah view-nya di Views/Admin/ atau partial). TIDAK paksa 7-area kalau Views kosong.
- **D-04 (Occurrence ambigu):** **Claude's discretion + rule**, tiap keputusan dicatat di audit deliverable SC1. Rule: ganti display label user-facing jelas; SKIP deskripsi nama-field-data (mis. ProtonData/ImportSilabus `<td>Bagian</td>` di tabel panduan import = deskripsi kolom, static), migration/import doc; combined phrase ("Bagian/Unit") → `GetLabel(0)`/`GetLabel(1)` per-part ATAU skip bila awkward.

### Spec-locked (spec §4.8 + §5 — planner implement as given)
- View: `@OrgLabels.GetLabel(N)` ganti hardcoded display string.
- Controller display string ke response/TempData/ViewBag → `_orgLabels.GetLabel(N)` (ORG-INTEG-02). Inject `IOrgLabelService` ke controller bila belum (cek per controller).
- Service registration Program.cs L65 sudah ada (Phase 340). `_ViewImports.cshtml` global @inject (D-01).

### Claude's Discretion
- Audit deliverable format (SC1): grep per file + tabel keputusan ganti/skip + reason per occurrence (planner picks format — markdown table cukup).
- Controller @inject `_orgLabels`: hanya controller yang punya display string TempData/ViewBag (audit per controller; mayoritas controller "Bagian"/"Unit" = query/audit/prop, skip).
- Urutan plan: audit dulu (deliverable scope) lalu apply, atau per-area plan — planner decides.
- Apakah perlu 1 plan audit-only + 1 plan apply, atau gabung — planner.

### Folded Todos
None — `todo match-phase 343` returned 0 matches.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec (PRIMARY)
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.8 "Integrasi label seluruh app" (L453-476) — scoping ganti/skip + per-page audit table (7 area)
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §5 "Dependency Registration" (L480-491) — `_ViewImports.cshtml` @inject + Program.cs (already registered)
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §6 "Edge Cases" L499-500 — empty table/level>max fallback "Level N"

### Milestone
- `.planning/milestones/v21.0-ROADMAP.md` §"Phase 343" (L74-85) — goal + 5 success criteria
- `.planning/milestones/v21.0-REQUIREMENTS.md` ORG-INTEG-01/02 (+ ORG-INTEG-03 = Phase 344) (L37-39)

### Phase 340 deliverables (consume)
- `Services/IOrgLabelService.cs` / `Services/OrgLabelService.cs` — `GetLabel(int)` cached + fallback "Level N"
- `Program.cs` L65 — `AddScoped<IOrgLabelService, OrgLabelService>()` (already registered)
- `Views/_ViewImports.cshtml` — global view imports (target D-01 @inject)

### Codebase audit anchors (confirmed display labels — replace targets)
- `Views/CMP/AnalyticsDashboard.cshtml` L84 (`<label for="filterBagian">Bagian</label>`), L94 (Unit filter), L548 (`<th>Bagian</th>`)
- `Views/CMP/RecordsTeam.cshtml` L20/L50 (filter labels), L135/L136 (`<th>Bagian</th>`/`<th>Unit</th>`)
- `Views/ProtonData/ImportSilabus.cshtml` L205 (`<td>Bagian</td>` — SKIP per D-04, import doc deskripsi field)

### Developer workflow
- `docs/DEV_WORKFLOW.md` — Lokal→Dev→Prod SOP
- `CLAUDE.md` — Bahasa Indonesia, no migration this phase
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrgLabelService.GetLabel(N)` — cached, fallback "Level N"; consume via @inject in views + ctor inject in controllers
- `Views/_ViewImports.cshtml` — global @inject host (D-01)
- Phase 341 `GET /Admin/GetLevelLabels` — N/A here (that's JS-fetch path for ManageOrganization); Phase 343 uses server-side @inject

### Established Patterns
- Razor server-render (most views are model-bound + server-render, suitable for @inject GetLabel)
- Controller dual-response Json/Redirect — display strings in TempData/ViewBag are the ORG-INTEG-02 targets

### Integration Points
- `Views/_ViewImports.cshtml` → 1 global @inject line
- Per-view display labels (filter/th/form) → `@OrgLabels.GetLabel(N)`
- Controllers with display TempData/ViewBag → ctor inject `_orgLabels` + `GetLabel(N)`

### Audit noise warning
Grep `Bagian|Unit` is NOISY: most matches = unit names ("LPG Treating Unit"), `.Unit`/`.Bagian` property access, audit log strings, query filters. Only a SMALL subset = tier-label display strings. The audit (SC1) must filter signal from noise per D-02/D-04.
</code_context>

<specifics>
## Specific Ideas

- Replace pattern: `>Bagian<` / `>Unit<` in `<label>`/`<th>`/`<option>` display context → `>@OrgLabels.GetLabel(0)<` / `>@OrgLabels.GetLabel(1)<`.
- SC2 demo (rename "Bagian"→"Direktorat" muncul): minimal 3 page dengan label confirmed — CMP filter (AnalyticsDashboard/RecordsTeam) + CDP assignment + Worker/Admin form. Verifikasi via browser setelah rename.
- ALL UI tetap Bahasa Indonesia (CLAUDE.md); label dinamis = output GetLabel (yang sudah Indonesian).
</specifics>

<deferred>
## Deferred Ideas

- Formal xUnit + Playwright E2E 5 scenario + manual UAT + regression smoke → Phase 344 (TEST-01..06, ORG-INTEG-03).
- ManageOrganization page (JS-fetch path) — done Phase 342, NOT re-touched here.

### Reviewed Todos (not folded)
None — no todo matches for Phase 343.
</deferred>

---

*Phase: 343-integrasi-app-wide*
*Context gathered: 2026-06-03*
