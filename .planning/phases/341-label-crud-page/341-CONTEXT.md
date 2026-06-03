# Phase 341: Label CRUD Page - Context

**Gathered:** 2026-06-03
**Status:** Ready for planning
**Milestone:** v21.0 (ManageOrganization Overhaul + Level Label CRUD)
**Predecessor:** Phase 340 SHIPPED LOCAL (Foundation — Tabel + Service + Cache + xUnit + IT Handoff)

<domain>
## Phase Boundary

Deliver page `/Admin/ManageOrgLevelLabels` untuk role Admin + HC: render tabel level → edit modal Label → add buffer-row Level baru → delete level tertinggi yang tidak dipakai. Wire ke `OrgLabelService` Phase 340 (UpdateAsync/AddAsync/DeleteAsync sudah ada + sudah ter-test 11 [Fact]).

**REQ delivered:** ORG-LABEL-04 (page CRUD + permission), ORG-LABEL-05 (audit log — already wired via Phase 340 service), ORG-LABEL-06 (server-side validation).

**Out of scope (Phase 342/343):**
- Integrasi label ke `Views/Admin/ManageOrganization.cshtml` tree row badge (Phase 342)
- Integrasi label app-wide (Phase 343)
- Bug fixes ManageOrganization tree (Phase 342)
- Cascade rename label propagation cross-page (Phase 343)

</domain>

<decisions>
## Implementation Decisions

### Save UX Pattern
- **D-01:** Save UX = fetch + JSON pattern. Match `Controllers/OrganizationController.cs` neighbor convention (`return Json(new { success, message })`). Modal submit → JS fetch POST → response body → toast + UI update. Avoid HTMX (only `ManageAssessment.cshtml` uses HTMX from Phase 311). Avoid Razor POST + full page redirect (admin convention rare).

### UI Composition
- **D-02:** Add UI = buffer row only. Drop "+ Tambah Level Baru" header button from spec §4.3. Tabel selalu render `0..max(used, configured)+1` baris; last row "(belum diset)" + inline "Tambah" button cell. Single mental model, no redundant entry point.
- **D-04:** Navigation = card baru di `Views/Admin/Index.cshtml`. Match ManageOrganization card pattern (L38). NO sidebar nesting, NO new "Pengaturan" section.
- **C-01:** Edit modal — Level field `disabled` (PK readonly), only Label editable. Indonesian copy.
- **C-02:** Buffer row markup: `<td><em class="text-muted">(belum diset)</em></td><td><button class="btn btn-sm btn-outline-primary">+ Tambah</button></td>`.

### Confirmation & Feedback
- **D-03:** Delete confirm = native `confirm('Hapus label Level {N} "{label}"? Tidak bisa diundo.')`. Match codebase convention (`Views/Admin/AssessmentMonitoringDetail.cshtml:326` × 6 instances). NO SweetAlert dependency.
- **D-07:** Toast feedback = reuse `wwwroot/js/shared-toast.js` `showToast(message, type)` where `type='success'|'danger'`. Include `<script src="~/js/shared-toast.js"></script>` di view (match `Views/Admin/ManageOrganization.cshtml:191`).

### Server-side Validation
- **D-05:** Validation = inline controller checks + service throw catch. NO DataAnnotations DTO, NO FluentValidation. Pattern: replicate `OrganizationController.AddOrganizationUnit` (`if (string.IsNullOrWhiteSpace(...)) return Json({success=false, message="..."})`). Service already throws `InvalidOperationException` for known-bad inputs — controller catches → friendly JSON.
  - Required + non-whitespace → controller `IsNullOrWhiteSpace` check
  - Max 50 char → controller `label.Length > 50` check + JSON reject
  - Unique across rows → controller `_context.OrganizationLevelLabels.AnyAsync(l => l.Label == label && l.Level != currentLevel)` check
- **D-08:** Add Level server constraint = enforce `level == GetMaxConfiguredLevel() + 1`. Reject arbitrary int dengan `Json({success=false, message="Hanya level berikutnya (Level N+1) yang bisa ditambahkan."})`. Prevents user submitting `level=99` from devtools.

### Security & Permissions
- **D-06:** Antiforgery = `[ValidateAntiForgeryToken]` controller attribute on POST actions + JS form-data POST with `__RequestVerificationToken` value from hidden form input. Replicate `wwwroot/js/orgTree.js` pattern (`document.querySelector('input[name="__RequestVerificationToken"]').value`). Hidden form input rendered via `@Html.AntiForgeryToken()` di Razor view.
- **D-09:** Controller split = **extend existing** `Controllers/OrgLabelController.cs` (Phase 340) dengan 4 actions baru:
  - `GET ManageOrgLevelLabels` → render page (model-bound ViewModel)
  - `POST UpdateLevelLabel` → call `OrgLabelService.UpdateAsync`
  - `POST AddLevelLabel` → call `OrgLabelService.AddAsync` (after D-08 constraint check)
  - `POST DeleteLevelLabel` → call `OrgLabelService.DeleteAsync` (after highest-unused check)

  Each new action → method-level `[Authorize(Roles="Admin,HC")]`. Keep existing `GetLevelLabels` JSON endpoint with class-level `[Authorize]` (any authenticated user, per Phase 340 D-03 public-display info).

### Render Mode
- **D-10:** Page render = server-render Razor model-bound table. Build `ManageOrgLevelLabelsViewModel { List<LabelRowVM> Rows; int MaxConfigured; int MaxUsed; }` di controller GET. View renders full `Rows` collection on initial load. AJAX hanya untuk 3 mutation actions (Update/Add/Delete) → response Json → JS `showToast` + `window.location.reload()` (simplest reload pattern, matches Phase 340 "reload page" spec literal §4.3).

### Claude's Discretion
- **C-03:** JS client-side preview validation (max 50 length + trim non-whitespace) sebelum submit — planner decides. Nice-to-have parity dengan ManageOrganization, tapi non-blocking karena server side authoritative.
- ViewModel class location: `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` atau inline di Controllers — planner decides per existing convention.
- Inline JS vs separate `wwwroot/js/orgLabelCrud.js` — planner decides. ManageAssessment uses inline + shared toast; ManageOrganization uses `orgTree.js`. Either match acceptable.
- Bootstrap modal markup pattern — replicate ManageOrganization edit modal markup verbatim, planner picks exact `aria-*` attrs.

</decisions>

<specifics>
## Specific Ideas

**Layout reference (per spec §4.3):**
```
Kelola Data / Kelola Label Tier Organisasi

Info banner: ℹ️ Label ini hanya nama tampilan tier. Mengubah label
             TIDAK mengubah struktur data atau tier. Cascade backend
             tetap berlaku berdasarkan level numerik.

Tabel:
| Level | Label saat ini    | Aksi              |
| 0     | Bagian            | [Edit]            |
| 1     | Unit              | [Edit]            |
| 2     | Sub-unit          | [Edit] [Delete]   |   ← Delete only if highest unused
| 3     | (belum diset)     | [+ Tambah]        |   ← Buffer row max+1
```

**Delete button visibility rule:**
- Show Delete ONLY pada `level == maxConfigured` AND `OrganizationUnits.AnyAsync(u => u.Level == level)` = false
- Show disabled Delete (tooltip "Tidak bisa dihapus, masih dipakai unit") pada level yang dipakai (highest tapi referenced)
- Hide Delete pada bukan-highest level

**Edit modal exact UX (per spec §4.3):**
- Title: `Edit Label Tier Level {N}`
- Field 1: Level (disabled, value `{N}`)
- Field 2: Label (text input, value current label, maxlength=50, required)
- Submit → POST UpdateLevelLabel → toast success "Label level {N} berhasil diubah menjadi '{newLabel}'" → reload page
- Cancel → close modal, no change

**Add modal (Tambah button → modal "Tambah Level Tier"):**
- Title: `Tambah Level Tier {N+1}` (use `MaxConfigured + 1`)
- Field 1: Level (disabled, value `{MaxConfigured + 1}`)
- Field 2: Label (text input, empty, maxlength=50, required)
- Submit → POST AddLevelLabel → toast success "Level {N+1} '{label}' berhasil ditambahkan" → reload page

**Audit log behavior (already implemented Phase 340):**
- Update → `ActorUserId={userId}`, `ActionType="OrgLabel-Update"`, `Description="Level 0: 'Bagian' → 'Direktorat'"`, `TargetId=0`, `TargetType="OrganizationLevelLabel"`
- Add → similar with `OrgLabel-Add`
- Delete → similar with `OrgLabel-Delete`

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.3 "Page /Admin/ManageOrgLevelLabels" — layout, modal UX, delete rule, permission
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.4 "Validation rules" — required/unique/max 50 + reasoning
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §2 "Goals & Non-Goals" — scope guardrails

### Milestone
- `.planning/milestones/v21.0-REQUIREMENTS.md` ORG-LABEL-04/05/06 — REQ definitions
- `.planning/milestones/v21.0-ROADMAP.md` §"Phase 341: Label CRUD Page" — 5 success criteria

### Phase 340 deliverables (depends_on)
- `Services/IOrgLabelService.cs` — interface 7 method (UpdateAsync/AddAsync/DeleteAsync ready to consume)
- `Services/OrgLabelService.cs` — impl with cache invalidate + audit log on each mutation
- `Controllers/OrgLabelController.cs` — existing class with `[Authorize]` + `[Route("Admin/[action]")]` + `GetLevelLabels` action; D-09 says extend this file
- `Models/OrganizationLevelLabel.cs` — entity (Level/Label/UpdatedAt/UpdatedBy)
- `.planning/phases/340-foundation-org-label-table-service-cache/340-CONTEXT.md` — 12 D-decisions (D-01..D-12) from Phase 340

### Codebase patterns (replicate)
- `Controllers/OrganizationController.cs` L67-272 — JSON action return pattern, `[ValidateAntiForgeryToken]` attr, inline validation
- `Views/Admin/ManageOrganization.cshtml` L191 — `<script src="~/js/shared-toast.js">` pattern
- `Views/Admin/Index.cshtml` L38 — admin card pattern (target for D-04 new card)
- `wwwroot/js/orgTree.js` `getAntiForgeryToken()` — antiforgery JS pattern (D-06)
- `wwwroot/js/shared-toast.js` `showToast(message, type)` — toast helper API (D-07)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` L326 — native `confirm()` delete pattern (D-03)

### Developer workflow
- `docs/DEV_WORKFLOW.md` — Lokal → Dev → Prod SOP
- `CLAUDE.md` — Project instructions Bahasa Indonesia, Seed/Migration workflow

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `OrgLabelService` (Phase 340) — all 3 mutation methods (`UpdateAsync`, `AddAsync`, `DeleteAsync`) implemented + tested 8 [Fact]; controller calls directly, no extension needed
- `OrgLabelService.GetMaxConfiguredLevel()` — sync, no DB hit (cache); use untuk D-08 constraint check + buffer row max calc
- `OrgLabelService.GetMaxUsedLevelAsync()` — async DB query; use untuk display logic (buffer row = max(used, configured)+1)
- `OrgLabelService.GetAll()` — cached read; controller GET uses untuk render table rows
- `OrgLabelController` (Phase 340) — extend with new actions; existing `[Route("Admin/[action]")]` covers all URLs
- `AuditLogService.LogAsync` — already integrated INTO `OrgLabelService` mutations (no controller-level audit call needed)
- `wwwroot/js/shared-toast.js` — `showToast(msg, type)` API stable
- `Views/Admin/ManageOrganization.cshtml` — Razor + Bootstrap 5 modal layout pattern reference

### Patterns to Replicate
- **JSON success/failure pattern:** `Controllers/OrganizationController.AddOrganizationUnit` L77-119 — `return Json(new { success=true|false, message="..." })`
- **Antiforgery JS submit:** `wwwroot/js/orgTree.js` — `params.append('__RequestVerificationToken', getAntiForgeryToken())`
- **Modal layout:** `Views/Admin/ManageOrganization.cshtml` (add/edit unit modal markup) — Bootstrap 5 `.modal .modal-dialog` structure
- **Native confirm() delete:** `Views/Admin/AssessmentMonitoringDetail.cshtml:326` — `onsubmit="return confirm('...')"`
- **Toast feedback:** `Views/Admin/ManageOrganization.cshtml:191` + `wwwroot/js/shared-toast.js` — `showToast('Berhasil…', 'success')` setelah AJAX 200
- **Admin card grid:** `Views/Admin/Index.cshtml` L38 (ManageOrganization card) — replicate for D-04 navigation entry

### Patterns to Avoid
- HTMX `hx-*` attributes — only used di `ManageAssessment.cshtml` (Phase 311 isolated). Stay konsisten dengan OrganizationController fetch+JSON pattern.
- `[Required]` / `[StringLength]` DataAnnotations — codebase admin pattern inline checks, tidak DataAnnotations.
- SweetAlert / Toastr libraries — not in package list; native `confirm()` + `wwwroot/js/shared-toast.js` cukup.
- Optimistic concurrency token (`UpdatedAt` ETag check) — single-server low-volume admin, last-write-wins accept.
- Server-rendered partial swap (Razor PartialView) — keep simple, just `window.location.reload()` after successful AJAX.

</code_context>

<threat_surface>
## Threat Surface (preview for planner threat model)

| ID | Category | Component | Severity |
|----|----------|-----------|----------|
| T-341-01 | Tampering | Anti-forgery missing on POST → CSRF | HIGH — mitigate via D-06 `[ValidateAntiForgeryToken]` |
| T-341-02 | Elevation of Privilege | Non-Admin/HC user accesses `/Admin/ManageOrgLevelLabels` | HIGH — mitigate via D-09 method-level `[Authorize(Roles="Admin,HC")]` |
| T-341-03 | Tampering | User submits arbitrary `level` int (e.g., 99) via devtools | MED — mitigate via D-08 server constraint `level == GetMaxConfiguredLevel()+1` for Add |
| T-341-04 | Tampering | User submits label >50 char / whitespace / duplicate via devtools | MED — mitigate via D-05 inline server checks |
| T-341-05 | Tampering | User deletes mid-tier label (not highest) via devtools | MED — mitigate via server check `level == GetMaxConfiguredLevel()` + `!AnyAsync(u => u.Level == level)` before service call |
| T-341-06 | Information Disclosure | Audit log description leaks sensitive field | LOW — accept (label rename = public display info, no PII) |
| T-341-07 | Denial of Service | Cache stampede on rapid edit | LOW — accept (single-admin volume; existing IMemoryCache `GetOrCreate` thread-safe enough) |

</threat_surface>

<open_questions>
## Open Questions for Researcher

1. ViewModel placement: existing convention `Models/ViewModels/` vs inline in Controllers? Scan codebase for prior admin ViewModels.
2. Modal markup: copy exact pattern dari `ManageOrganization.cshtml` add/edit unit modal — note exact id/aria conventions for planner.
3. JS bundling: inline `<script>` block di view OR new `wwwroot/js/orgLabelCrud.js`? Confirm convention.
4. Toast color for "error" vs "danger" — does `shared-toast.js` accept both Bootstrap variants?

</open_questions>

<success_definition>
## Success Definition (planner verifies)

Per ROADMAP §"Phase 341: Label CRUD Page":

1. Page `/Admin/ManageOrgLevelLabels` dapat diakses role Admin + HC (non-role gets 403)
2. Edit modal label "Bagian" → "Direktorat" → submit → toast success + tabel reload menampilkan label baru
3. AuditLog entry tercatat dengan UserId + before/after label (Phase 340 service already does this, validate in UAT)
4. Validation reject: empty input + duplicate label across levels + label >50 char (server-side)
5. Delete tombol hanya muncul untuk level tertinggi yang tidak dipakai unit; disabled untuk yang dipakai (tooltip "Tidak bisa dihapus, masih dipakai")

</success_definition>
