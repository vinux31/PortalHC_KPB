# Phase 341: Label CRUD Page — Research

**Researched:** 2026-06-03
**Domain:** ASP.NET Core 8 MVC admin CRUD page — server-rendered Razor table + Bootstrap 5 modal + JSON fetch mutation + xUnit InMemory testing
**Confidence:** HIGH (semua pattern verified di codebase langsung — bukan ekstrapolasi)
**Predecessor:** Phase 340 SHIPPED LOCAL (Service + Cache + 13 [Fact] PASS + endpoint `GET /Admin/GetLevelLabels` 200 verified)

---

## Summary

Phase 341 mengkonsumsi `OrgLabelService` Phase 340 (UpdateAsync/AddAsync/DeleteAsync sudah jadi + ter-test 11 mutation [Fact]) untuk membangun page CRUD `/Admin/ManageOrgLevelLabels`. Tidak ada teknologi baru — semua pattern (JSON action, fetch+antiforgery, Bootstrap modal, shared-toast, native `confirm()`, server-render Razor ViewModel) tersedia di neighbor codebase dan tinggal di-replicate verbatim. Tidak ada migration, tidak ada seed change, tidak ada IT handoff. Zero schema change.

**Primary recommendation:** Plan 01 controller actions + ViewModel (45-60 LoC delta `OrgLabelController.cs` + 25 LoC `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs`); Plan 02 view + JS (`Views/Admin/ManageOrgLevelLabels.cshtml` ~200 LoC inline `<script>` + 1 line di `Views/Admin/Index.cshtml`); Plan 03 xUnit controller tests (4-5 [Fact] target `OrgLabelControllerTests`). Sequential strict — Plan 02 needs Plan 01 actions wired, Plan 03 needs both. Total ~1 hari kerja sesuai ROADMAP estimate.

---

## Project Constraints (from CLAUDE.md)

| Directive | Source | Impact on Plan |
|-----------|--------|----------------|
| **Bahasa Indonesia** untuk semua UI copy + commit message | `CLAUDE.md` L3 | Toast text, modal title, breadcrumb, button label, error message, audit description ALL Indonesian (sudah konsisten Phase 340 audit log: `"Level {N}: '{old}' → '{new}'"` literal English service-side, tapi UI text Indonesian) |
| **Develop Workflow Lokal → Dev → Prod** | `CLAUDE.md` L9-19 + `docs/DEV_WORKFLOW.md` | Plan output = lokal verify (`dotnet build` + `dotnet run` + manual browser test `http://localhost:5277`) + commit + push. **NO push to Dev/Prod, NO migration needed (Phase 341 zero schema change).** |
| **Seed Workflow** | `CLAUDE.md` L21-31 + `docs/SEED_WORKFLOW.md` | **N/A** — Phase 341 tidak menyentuh seed (existing seed 3 row Phase 340 sufficient). Tidak ada snapshot DB needed. |
| **No edit kode/DB di server Dev/Prod** | `CLAUDE.md` L17 | All work lokal. IT handoff = **NOT NEEDED** (no migration). |
| **No push tanpa verifikasi lokal** | `CLAUDE.md` L17 | Tasks must include lokal verify steps sebelum commit. |

---

## User Constraints (from CONTEXT.md)

### Locked Decisions (10 D-decisions verbatim)

- **D-01 Save UX = fetch + JSON pattern** — replicate `OrganizationController.AddOrganizationUnit`/`EditOrganizationUnit` JSON return. Avoid HTMX (only `ManageAssessment.cshtml` uses HTMX from Phase 311). Avoid Razor POST + full page redirect.
- **D-02 Add UI = buffer row only** — drop "+ Tambah Level Baru" header button from spec §4.3. Tabel selalu render `0..max(used, configured)+1` baris; last row "(belum diset)" + inline "Tambah" button cell. Single mental model.
- **D-03 Delete confirm = native `confirm()`** — match codebase (`AssessmentMonitoringDetail.cshtml:326` × 6 instances). Format: `confirm('Hapus label Level {N} "{label}"? Tidak bisa diundo.')`. NO SweetAlert.
- **D-04 Navigation = card baru di `Views/Admin/Index.cshtml`** — match ManageOrganization card pattern (L38). NO sidebar nesting, NO new "Pengaturan" section.
- **D-05 Validation = inline controller checks + service catch** — NO DataAnnotations DTO, NO FluentValidation. Pattern: replicate `OrganizationController.AddOrganizationUnit` (`if (string.IsNullOrWhiteSpace(...)) return Json({success=false, message="..."})`). Service throws `InvalidOperationException` for known-bad inputs → controller catches → friendly JSON.
  - Required + non-whitespace → controller `IsNullOrWhiteSpace` check
  - Max 50 char → controller `label.Length > 50` check + JSON reject
  - Unique across rows → controller `_context.OrganizationLevelLabels.AnyAsync(l => l.Label == label && l.Level != currentLevel)`
- **D-06 Antiforgery = `[ValidateAntiForgeryToken]`** controller attribute on POST actions + JS form-data POST with `__RequestVerificationToken`. Replicate `wwwroot/js/orgTree.js` `getAntiForgeryToken()` pattern (L8-11). Hidden form input via `@Html.AntiForgeryToken()` di Razor view.
- **D-07 Toast feedback = reuse `wwwroot/js/shared-toast.js`** `showToast(message, type)` where `type='success'|'danger'`. Include `<script src="~/js/shared-toast.js"></script>` di view (match `ManageOrganization.cshtml:191`).
- **D-08 Add Level server constraint** = enforce `level == GetMaxConfiguredLevel() + 1`. Reject arbitrary int dengan friendly JSON. Prevents user submitting `level=99` from devtools.
- **D-09 Controller split = extend existing `Controllers/OrgLabelController.cs`** dengan 4 actions baru:
  - `GET ManageOrgLevelLabels` → render page (ViewModel)
  - `POST UpdateLevelLabel` → call `OrgLabelService.UpdateAsync`
  - `POST AddLevelLabel` → call `OrgLabelService.AddAsync` (after D-08 check)
  - `POST DeleteLevelLabel` → call `OrgLabelService.DeleteAsync` (after highest-unused check)

  Each new action → method-level `[Authorize(Roles="Admin,HC")]`. Keep existing `GetLevelLabels` JSON endpoint class-level `[Authorize]` (any authenticated user, per Phase 340 D-03).
- **D-10 Page render = server-render Razor model-bound table.** Build `ManageOrgLevelLabelsViewModel { List<LabelRowVM> Rows; int MaxConfigured; int MaxUsed; }` di controller GET. View renders full `Rows` on initial load. AJAX hanya untuk 3 mutation → JSON → `showToast` + `window.location.reload()`.

### Claude's Discretion (3 C-items)

- **C-01** Edit modal — Level field `disabled` (PK readonly), only Label editable. Indonesian copy.
- **C-02** Buffer row markup: `<td><em class="text-muted">(belum diset)</em></td><td><button class="btn btn-sm btn-outline-primary">+ Tambah</button></td>`.
- **C-03** JS client-side preview validation (max 50 length + trim non-whitespace) sebelum submit — planner decides. Nice-to-have parity dengan ManageOrganization, tapi non-blocking karena server side authoritative.

Plus 3 planner-decides items (not enumerated as C-#):
- ViewModel placement: `Models/ViewModels/` atau inline di Controllers — **OQ#1 resolved below**
- Inline JS vs separate `wwwroot/js/orgLabelCrud.js` — **OQ#3 resolved below**
- Exact Bootstrap modal markup `aria-*` attrs — **OQ#2 resolved below**

### Deferred Ideas (OUT OF SCOPE)

| Item | Reason | Defer To |
|------|--------|----------|
| Integrasi label ke `Views/Admin/ManageOrganization.cshtml` tree row badge | Out of phase scope | Phase 342 |
| Integrasi label app-wide 7 area page | Out of phase scope | Phase 343 |
| Bug fixes ManageOrganization tree | Out of phase scope | Phase 342 |
| Cascade rename label propagation cross-page | Out of phase scope | Phase 343 |
| HTMX swap pattern | Conflict dengan D-01 | Never (D-01 lock) |
| Optimistic concurrency token `UpdatedAt` ETag | Single-admin low-volume, last-write-wins accept | Permanent skip |
| SweetAlert/Toastr | Not in package list | Permanent skip |
| Server-rendered PartialView swap | Keep simple, just reload | Permanent skip |

---

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **ORG-LABEL-04** | Page `/Admin/ManageOrgLevelLabels` Admin+HC access; tabel level auto-detect max depth + buffer 1; edit modal per row; delete hanya untuk level tertinggi tidak dipakai | Controller pattern `OrganizationController.ManageOrganization` L29-54 (route auto via `[Route("Admin/[action]")]` L11); ViewModel pattern `Models/ViewModels/CMPRecordsViewModel.cs`; modal pattern `ManageOrganization.cshtml` L133-187; `GetMaxConfiguredLevel`/`GetMaxUsedLevelAsync` Phase 340 service ready |
| **ORG-LABEL-05** | Audit log UserId + before/after label | **Already implemented Phase 340 service** (`OrgLabelService.cs` L62-69, L89-96, L111-118 × 3 mutations). Controller just resolves `actorName` = `$"{currentUser.NIP} - {currentUser.FullName}"` pattern (`OrganizationController.cs:425-428`) + passes to service |
| **ORG-LABEL-06** | Validation server-side: required + non-whitespace + unique across levels + max 50 char | D-05 inline pattern matches `OrganizationController.AddOrganizationUnit` L76-93 (3 checks: empty / duplicate name / parent circular). Unique check uses `AnyAsync` filter `l.Label == label && l.Level != currentLevel` |

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Page render (GET) | API/Backend (MVC controller + Razor) | — | Server-rendered Razor table (D-10), no SPA hydration |
| Mutation actions (Update/Add/Delete) | API/Backend (controller action) | — | All validation server-side authoritative; client preview validation optional (C-03) |
| Validation enforcement | API/Backend (inline controller checks + service throw) | Browser (optional preview) | D-05 server-side authoritative; D-06 prevents CSRF; D-08 prevents arbitrary level |
| Cache invalidation | Service layer (`OrgLabelService`) | — | Already wired Phase 340 — no controller-level cache call needed |
| Audit logging | Service layer (`OrgLabelService` → `AuditLogService`) | — | Already wired Phase 340 — controller resolves `actorUserId + actorName` and passes |
| Toast notification | Browser (shared-toast.js DOM injection) | — | Client-side ephemeral UI feedback after AJAX success |
| Delete confirmation | Browser (native `confirm()`) | — | D-03 zero-dependency client-side gate |
| Navigation entry | Frontend View (Razor card in Admin/Index) | — | D-04 static card, role-gated via `@if (User.IsInRole...)` |

**No mis-assignment risk:** All capabilities sit naturally in MVC backend tier; only display feedback is browser-tier.

---

## Standard Stack

### Core (existing — no install needed)

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| ASP.NET Core MVC | 8.0 | Controller + Razor view + DI | `HcPortal.csproj` net8.0 [VERIFIED: HcPortal.Tests.csproj L4] |
| Microsoft.AspNetCore.Identity | 8.0 | `UserManager<ApplicationUser>` for `actorName` resolution | Used in `OrganizationController` L425 [VERIFIED: codebase grep] |
| Microsoft.EntityFrameworkCore | 8.0 | `_context.OrganizationLevelLabels.AnyAsync` for unique validation | Used in `OrgLabelService` [VERIFIED: Services/OrgLabelService.cs L74] |
| Bootstrap | 5.3.0 | Modal + form + button styling | Used in `ManageOrganization.cshtml` [VERIFIED: codebase] |
| Bootstrap Icons | (in layout) | `bi-pencil-square`, `bi-trash`, `bi-plus-circle` | Used codebase-wide [VERIFIED] |

### Service (Phase 340 — ready to consume)

| Method | Signature | Use in Phase 341 |
|--------|-----------|------------------|
| `GetAll()` | `IReadOnlyDictionary<int,string>` cached | Controller GET — build `Rows` list |
| `GetMaxConfiguredLevel()` | `int` (cache, sync) | D-08 constraint check + buffer row calc |
| `GetMaxUsedLevelAsync()` | `Task<int>` (live DB) | Buffer row max calc + delete eligibility |
| `UpdateAsync(level, label, userId, actorName)` | `Task` | POST UpdateLevelLabel handler |
| `AddAsync(level, label, userId, actorName)` | `Task` (throws if exists) | POST AddLevelLabel handler |
| `DeleteAsync(level, userId, actorName)` | `Task` (throws if not found) | POST DeleteLevelLabel handler |

**All 3 mutation methods already invoke `_cache.Remove("OrgLabels:All")` + `_auditLog.LogAsync(...)` internally** [VERIFIED: Services/OrgLabelService.cs L60-69, L87-96, L109-118]. Controller does NOT need to duplicate cache/audit calls.

### Testing (existing infra)

| Library | Version | Purpose | Source |
|---------|---------|---------|--------|
| xUnit | 2.9.3 | `[Fact]` test framework | [VERIFIED: HcPortal.Tests.csproj L14] |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | In-memory `ApplicationDbContext` for unit tests | [VERIFIED: HcPortal.Tests.csproj L12] |
| Microsoft.NET.Test.Sdk | 17.13.0 | Test runner SDK | [VERIFIED: HcPortal.Tests.csproj L13] |

**No new package install needed.** Pattern proven by `OrgLabelServiceTests.cs` 13 [Fact] PASS Phase 340 [VERIFIED: dotnet test 31/31 PASS commit `06582a9b`].

### Alternatives Considered & Rejected

| Instead of | Could Use | Tradeoff | Decision |
|------------|-----------|----------|----------|
| Server-render Razor | HTMX swap (Phase 311 pattern) | Reduces page reload | **D-01 lock**: HTMX isolated to ManageAssessment only; codebase admin convention = fetch+JSON+reload |
| Inline controller validation | FluentValidation library | Cleaner DSL | **D-05 lock**: codebase has zero FluentValidation usage; adds dependency for 3-rule validation |
| Bootstrap modal | SweetAlert delete confirm | Better UX visual | **D-03 lock**: SweetAlert not in package list; native `confirm()` proven by 6 codebase instances |
| `WebApplicationFactory` integration test | Direct controller instantiation | Closer to real HTTP | **Phase 340 G7/G8 precedent**: WebApplicationFactory not in repo; live `curl` + manual smoke canonical for endpoint testing |

---

## Existing Code Insights

### Files to extend (3)

| File | Lines today | Delta estimate | What changes |
|------|------------|---------------|---------------|
| `Controllers/OrgLabelController.cs` | 32 | +110 LoC | Add 4 actions (Index/Update/Add/Delete) — extend per D-09 |
| `Views/Admin/Index.cshtml` | ~85 | +14 LoC (1 card block) | Add 1 admin card after ManageOrganization card L37-49 — per D-04 |
| `Models/ViewModels/` directory | 1 file (CMPRecordsViewModel.cs) | +1 file (~25 LoC) | New `ManageOrgLevelLabelsViewModel.cs` — OQ#1 resolved (namespace established) |

### Files to create (2)

| File | Estimate | Purpose |
|------|----------|---------|
| `Views/Admin/ManageOrgLevelLabels.cshtml` | ~200 LoC | Server-render table + 2 modals (Edit/Add) + inline `<script>` block |
| `HcPortal.Tests/OrgLabelControllerTests.cs` | ~250 LoC | 4-5 [Fact] permission + validation (see Validation Architecture §) |

### Existing patterns to replicate verbatim

**Pattern 1 — JSON success/failure return (D-01):**
Source: `Controllers/OrganizationController.cs:74-122` `AddOrganizationUnit`
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddOrganizationUnit(string name, int? parentId)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        if (IsAjaxRequest())
            return Json(new { success = false, message = "Nama tidak boleh kosong." });
        TempData["Error"] = "Nama tidak boleh kosong.";
        return RedirectToAction("ManageOrganization");
    }

    bool duplicate = await _context.OrganizationUnits.AnyAsync(u => u.Name == name.Trim());
    if (duplicate)
    {
        if (IsAjaxRequest())
            return Json(new { success = false, message = "Nama unit sudah digunakan." });
        // ...
    }
    // ...
    return Json(new { success = true, message = "Unit berhasil ditambahkan." });
}
```
**Adaptation for Phase 341:** Since AJAX is the only call path (D-01 fetch+JSON), the `IsAjaxRequest()` branch becomes the single response path — TempData fallback can be dropped for the 3 mutation actions (no full-form non-AJAX submit exists). Optionally keep TempData branch for defense-in-depth.

**Pattern 2 — Audit log actor resolution (D-09 service-call prep):**
Source: `Controllers/OrganizationController.cs:425-428`
```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
// Then pass: await _orgLabels.UpdateAsync(level, label, currentUser?.Id ?? "", actorName);
```

**Pattern 3 — Antiforgery JS submit (D-06):**
Source: `wwwroot/js/orgTree.js:8-26`
```js
function getAntiForgeryToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}

async function ajaxPost(url, data = {}) {
    const params = new URLSearchParams(data);
    params.append('__RequestVerificationToken', getAntiForgeryToken());
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'X-Requested-With': 'XMLHttpRequest'
        },
        body: params.toString()
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
}
```
**Adaptation:** Phase 341 inline `<script>` can either (a) reference `~/js/orgTree.js` to reuse `getAntiForgeryToken` / `ajaxPost` (DRY but adds coupling), or (b) inline a minimal 8-line copy (zero coupling, matches admin convention where small pages inline JS). **Recommendation: option (b) — inline minimal helper** (OQ#3 resolved below).

**Pattern 4 — Bootstrap modal markup (C-01, OQ#2):**
Source: `Views/Admin/ManageOrganization.cshtml:133-164`
- `<div class="modal fade" id="unitModal" tabindex="-1" aria-labelledby="unitModalLabel" aria-hidden="true">`
- `<div class="modal-dialog">` (no `modal-lg`)
- `<div class="modal-header bg-primary text-white">` + `<h5 class="modal-title">` + `<button class="btn-close btn-close-white" data-bs-dismiss="modal">`
- Body: `<input type="hidden" id="...Id" value="" />` + form-control inputs with `<div class="invalid-feedback">`
- Footer: `<button class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>` + `<button class="btn btn-primary" onclick="submitX()"><i class="bi bi-save me-1"></i>Simpan</button>`

**Adaptation for Phase 341:**
- Edit modal id=`labelEditModal`, hidden input `labelEditLevel`, disabled input `labelEditLevelDisplay`, editable input `labelEditValue` with `maxlength="50"` + `<div class="invalid-feedback">` placeholder.
- Add modal id=`labelAddModal`, hidden input `labelAddLevel` (server-injected `MaxConfigured+1`), disabled input `labelAddLevelDisplay`, editable input `labelAddValue` maxlength=50.

**Pattern 5 — Native `confirm()` delete (D-03):**
Source: `Views/Admin/AssessmentMonitoringDetail.cshtml:325-333` (× 6 instances codebase-wide)
```html
<form asp-action="ResetAssessment" asp-controller="AssessmentAdmin" method="post" class="m-0"
      onsubmit="return confirm('Reset sesi ini? ...')">
    @Html.AntiForgeryToken()
    <input type="hidden" name="id" value="@session.Id" />
    <button type="submit" class="dropdown-item">...</button>
</form>
```
**Adaptation for Phase 341:** Since D-01 says fetch+JSON (not full form POST), delete button onclick → JS `confirm()` then `fetch()` then `showToast()` + `reload()`. NOT raw form POST. CONTEXT.md format: `confirm('Hapus label Level {N} "{label}"? Tidak bisa diundo.')`.

**Pattern 6 — Toast feedback (D-07):**
Source: `wwwroot/js/shared-toast.js:1-15`
- `showToast(message, type)` where `type` matches Bootstrap alert variant.
- **OQ#4 resolved:** `shared-toast.js` builds `'alert alert-' + type` — accepts ANY Bootstrap variant including `'success'`, `'danger'`, `'warning'`, `'info'`. But icon is hardcoded ternary `type === 'success' ? 'check-circle' : 'exclamation-triangle'` — so non-success types ALL get exclamation icon. Use `'danger'` (not `'error'`) for consistency with Bootstrap conventions and codebase usage.

**Pattern 7 — Admin card grid (D-04):**
Source: `Views/Admin/Index.cshtml:35-49` (ManageOrganization card)
```html
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("ManageOrganization", "Organization")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-diagram-3 fs-5 text-primary"></i>
                    <span class="fw-bold">Organization Structure</span>
                </div>
                <small class="text-muted">Kelola hierarki Bagian dan Unit kerja dengan tampilan tree</small>
            </div>
        </div>
    </a>
</div>
}
```
**Adaptation for Phase 341:** Insert after L49 (closing `}` of ManageOrganization card block):
```html
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
<div class="col-md-4">
    <a href="@Url.Action("ManageOrgLevelLabels", "OrgLabel")" class="text-decoration-none">
        <div class="card shadow-sm h-100 border-0">
            <div class="card-body">
                <div class="d-flex align-items-center gap-2 mb-2">
                    <i class="bi bi-tags fs-5 text-primary"></i>
                    <span class="fw-bold">Label Tier Organisasi</span>
                </div>
                <small class="text-muted">Kelola nama tier organisasi (Bagian/Unit/Sub-unit) tanpa edit kode</small>
            </div>
        </div>
    </a>
</div>
}
```

**Pattern 8 — ViewModel placement (OQ#1):**
Established convention `Models/ViewModels/CMPRecordsViewModel.cs` namespace `HcPortal.Models.ViewModels` (Phase v20.0). Most other ViewModels live in flat `Models/` namespace `HcPortal.Models`. Both conventions co-exist. **Recommendation:** use `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` namespace `HcPortal.Models.ViewModels` — matches newest pattern (post-v20.0) + keeps Models root clean. Controller adds `using HcPortal.Models.ViewModels;`.

### Pattern 9 — Razor view resolution for non-Admin controller name

Source: `Controllers/OrganizationController.cs:24-27`
```csharp
protected new ViewResult View() => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml");
protected new ViewResult View(object? model) => base.View("~/Views/Admin/" + ControllerContext.ActionDescriptor.ActionName + ".cshtml", model);
// ...
```
**Critical insight:** `OrganizationController` overrides `View()` because its controller name "Organization" doesn't match Razor's default `Views/Organization/...` lookup — it points views back to `Views/Admin/`.

**Adaptation for Phase 341:** `OrgLabelController` similarly needs `View()` override to resolve `Views/Admin/ManageOrgLevelLabels.cshtml`. **Critical** — without this, `return View(vm)` will fail-404 because Razor looks in `Views/OrgLabel/`. Add the same 4-overload override to `OrgLabelController.cs`.

Alternative: `return View("~/Views/Admin/ManageOrgLevelLabels.cshtml", vm);` explicit path per action — works but duplicates the path string. Recommend override pattern matching `OrganizationController` for consistency.

### Patterns to Avoid

- **HTMX `hx-*` attributes** — only used in `ManageAssessment.cshtml` (Phase 311 isolated). D-01 lock.
- **`[Required]` / `[StringLength]` DataAnnotations on DTO** — codebase admin pattern = inline checks. D-05 lock.
- **SweetAlert / Toastr** — not in package list. D-03/D-07 lock.
- **Optimistic concurrency `[Timestamp]` byte[] RowVersion** — single-admin volume; last-write-wins accept (no concurrency token in CONTEXT.md).
- **PartialView swap (`PartialAsync` in fetch response body)** — D-10 lock = full page reload after success.
- **Inline `@Url.Action` inside JS literal** — XSS risk. Use `data-url` attribute on button OR hardcode `/Admin/UpdateLevelLabel` literal since route is stable (`[Route("Admin/[action]")]` on controller).

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Antiforgery token JS extraction | Custom `getCookie('XSRF-TOKEN')` parser | `document.querySelector('input[name="__RequestVerificationToken"]').value` per `orgTree.js:8-11` | Standard ASP.NET Core pattern; hidden input rendered by `@Html.AntiForgeryToken()` |
| Toast notification | New `<div>`/CSS animation from scratch | `wwwroot/js/shared-toast.js` `showToast(msg, type)` | Already exists, 15 lines, Bootstrap-styled, auto-dismiss 3.5s |
| Audit log writer | Direct `_context.AuditLogs.Add(...)` in controller | `OrgLabelService.UpdateAsync/AddAsync/DeleteAsync` internal `_auditLog.LogAsync` call | Already wired Phase 340. Controller-side audit = duplicate log entries. |
| Cache invalidation | `_cache.Remove("OrgLabels:All")` in controller | Service mutations already invalidate | Already wired Phase 340 (`OrgLabelService.cs:60,87,109`). |
| Actor name resolver | New helper | `OrganizationController.cs:425-428` pattern: `$"{NIP} - {FullName}"` with whitespace fallback to `FullName` then `"Unknown"` | Established convention; matches existing AuditLog `ActorName` format |
| Fetch helper | New `XMLHttpRequest` wrapper | `orgTree.js:13-26` `ajaxPost(url, data)` (reuse or inline-copy minimal version) | Proven pattern; sets `X-Requested-With` header for `IsAjaxRequest()` server check |
| Delete confirm dialog | Custom modal | Native `confirm()` per `AssessmentMonitoringDetail.cshtml:326` | 6 codebase instances; zero dependency; accessible by default |
| Unique label validation | Custom store/index | `_context.OrganizationLevelLabels.AnyAsync(l => l.Label == label && l.Level != currentLevel)` | EF Core handles indexing; pattern matches `OrganizationController.cs:85,149` |

**Key insight:** Phase 340 service layer did the heavy lifting — controller is a 4-action thin adapter (validate → call service → return Json). No business logic in controller.

---

## Runtime State Inventory

Phase 341 is **NEW capability (page + actions), not rename/refactor/migration**. Section not applicable.

For completeness:
- **Stored data:** Phase 340 seeded 3 rows in `OrganizationLevelLabels` table. Phase 341 mutates these via service. No new stored data category introduced.
- **Live service config:** None changed.
- **OS-registered state:** None changed.
- **Secrets/env vars:** None changed.
- **Build artifacts:** Standard incremental rebuild (`bin/Debug/net8.0/HcPortal.dll`) — no rename, no artifact migration needed.

---

## Common Pitfalls

### Pitfall 1: Razor view resolution 404 because controller name ≠ view folder

**What goes wrong:** `OrgLabelController` lives in `Controllers/OrgLabelController.cs`. Razor default lookup is `Views/OrgLabel/ManageOrgLevelLabels.cshtml`. We put view in `Views/Admin/ManageOrgLevelLabels.cshtml`. Without override → 404 "view not found".
**Why it happens:** Codebase convention puts all admin-area views under `Views/Admin/` regardless of controller class.
**How to avoid:** Add 4 `View()` overrides at top of `OrgLabelController` matching `OrganizationController.cs:24-27` pattern, OR use explicit path `return View("~/Views/Admin/ManageOrgLevelLabels.cshtml", vm)`.
**Warning signs:** Manual `curl http://localhost:5277/Admin/ManageOrgLevelLabels` returns 404 instead of 200; dev console no error (controller hit, view lookup failed).

### Pitfall 2: Antiforgery 400 from missing `[ValidateAntiForgeryToken]` OR missing JS token append

**What goes wrong:** AJAX POST without `__RequestVerificationToken` in body → ASP.NET Core 400 "anti-forgery token not present". Or: token present but missing `@Html.AntiForgeryToken()` in view → no input to read from.
**Why it happens:** Two-sided contract; either side missing fails silently with confusing 400.
**How to avoid:**
1. View MUST include `@Html.AntiForgeryToken()` once (or per form).
2. JS MUST append `__RequestVerificationToken` to URLSearchParams body before POST.
3. Controller actions MUST have `[ValidateAntiForgeryToken]` attribute (D-06).
**Warning signs:** Network tab shows POST → 400 Bad Request with "anti-forgery" in response body.

### Pitfall 3: Trim whitespace BEFORE validation (label = "   Bagian   " case)

**What goes wrong:** User submits label with leading/trailing whitespace. `IsNullOrWhiteSpace` catches all-whitespace but lets `"  X  "` through. Uniqueness check then misses dup because `"Bagian" != "  Bagian  "` literally.
**Why it happens:** EF query is exact string match by default (varies by collation, but EF translates to SQL `=`).
**How to avoid:** `label = label.Trim();` IMMEDIATELY after `IsNullOrWhiteSpace` guard, BEFORE length check + uniqueness check + service call. Pattern from `OrganizationController.cs:85` `u.Name == name.Trim()`.
**Warning signs:** Two rows have visually same label but DB shows different strings.

### Pitfall 4: Service throws but controller doesn't catch → 500 Internal Server Error

**What goes wrong:** `UpdateAsync` throws `InvalidOperationException("Level 99 not configured")` for unknown level. Without controller try/catch, response is 500 with stack trace (info leak risk + bad UX).
**Why it happens:** Service follows fail-fast pattern (Phase 340 D-decision); controller is the boundary that translates to user-facing JSON.
**How to avoid:** Wrap each service call in try/catch:
```csharp
try {
    await _orgLabels.UpdateAsync(level, label, userId, actorName);
    return Json(new { success = true, message = $"Label level {level} berhasil diubah." });
} catch (InvalidOperationException ex) {
    return Json(new { success = false, message = ex.Message });
}
```
**Warning signs:** Network tab POST returns 500; UI shows generic error instead of friendly Indonesian message.

### Pitfall 5: Modal id collision when 2 modals on same page

**What goes wrong:** Bootstrap modals selected by id. If Edit modal `id="labelModal"` and Add modal also `id="labelModal"` (copy-paste oversight), `bootstrap.Modal.getInstance` picks first occurrence; second modal silently broken.
**Why it happens:** Copy-paste between modal templates.
**How to avoid:** Use distinct ids: `labelEditModal` vs `labelAddModal`; distinct submit button ids; distinct hidden input ids. Replicate `ManageOrganization.cshtml` 2-modal convention (`unitModal` + `deleteModal` L134/L167).
**Warning signs:** Clicking "Tambah" opens Edit modal (or nothing); console shows Bootstrap modal error.

### Pitfall 6: `window.location.reload()` after success loses scroll position + form draft

**What goes wrong:** Mid-page edit on long table → reload jumps to top, loses context. Cumulative UX friction.
**Why it happens:** D-10 reload pattern is intentional simplicity tradeoff.
**How to avoid:** Accept tradeoff per D-10 (single-admin low-volume). Alternative future: switch to partial swap or row in-place update — but OUT OF SCOPE Phase 341.
**Warning signs:** User reports "page jumps after save"; acceptable per CONTEXT.md decisions.

### Pitfall 7: Mid-tier delete bypass via devtools (T-341-05)

**What goes wrong:** User submits `level=0` to DeleteLevelLabel even though Delete button only renders for highest. Without server check, level 0 gets deleted → all level-0 organization units now have no label.
**Why it happens:** Client-side button visibility ≠ server-side authorization.
**How to avoid:** DeleteLevelLabel controller MUST re-check:
```csharp
int maxConfig = _orgLabels.GetMaxConfiguredLevel();
if (level != maxConfig)
    return Json(new { success = false, message = "Hanya level tertinggi yang bisa dihapus." });

bool isUsed = await _context.OrganizationUnits.AnyAsync(u => u.Level == level);
if (isUsed)
    return Json(new { success = false, message = "Level masih dipakai unit, tidak bisa dihapus." });
```
**Warning signs:** Manual `curl` POST `level=0` succeeds in dev; mitigation in T-341-05.

### Pitfall 8: AddAsync race — two HC admins click "Tambah" simultaneously

**What goes wrong:** Both submit `level=3`. First inserts; second hits service `AnyAsync` check → already exists → `InvalidOperationException`. Controller surfaces error → user confused.
**Why it happens:** No DB-level unique constraint on Level (Phase 340 entity primary key is Level — actually IS unique by EF convention). Service-side `AnyAsync` is race-prone between check and Add.
**How to avoid:** Phase 340 used Level as PK (per `OrganizationLevelLabel.cs:9` `public int Level { get; set; }` + EF migration verified Phase 340 §340-VALIDATION). Second insert will throw EF `DbUpdateException` for duplicate PK. Controller can catch BOTH `InvalidOperationException` (service-thrown) AND `DbUpdateException` (DB-thrown race winner) → friendly message "Level sudah ada, refresh halaman."
**Warning signs:** Concurrent test reproduces 500 error instead of friendly JSON. **Mitigation in Plan 03 test optional** (low probability single-admin); accept.

### Pitfall 9: Unique check case-sensitivity mismatch with SQL Server collation

**What goes wrong:** Default SQL Server collation `SQL_Latin1_General_CP1_CI_AS` is case-insensitive. EF `AnyAsync(l => l.Label == "bagian")` matches existing `"Bagian"` row server-side. BUT InMemory provider used in tests is case-SENSITIVE. Test pass ≠ prod behavior.
**Why it happens:** EF InMemory provider doesn't simulate SQL collation.
**How to avoid:** Document this gap in test. For prod safety, use explicit comparison: `EF.Functions.Collate(l.Label, "SQL_Latin1_General_CP1_CS_AS") == label` IF case-sensitive uniqueness desired. Otherwise accept current default (case-insensitive match) and adjust unit test expectation accordingly (test with same case as seed).
**Warning signs:** Unit test passes with `"BAGIAN"` not duplicate; manual SQL test in Dev rejects. **Recommendation:** Phase 341 accept default case-insensitive (matches user mental model: "Bagian" and "bagian" SHOULD be considered duplicate).

---

## Code Examples

### Example 1 — Controller GET action (Plan 01)

```csharp
// GET /Admin/ManageOrgLevelLabels
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> ManageOrgLevelLabels()
{
    var labels = _orgLabels.GetAll();
    int maxConfig = _orgLabels.GetMaxConfiguredLevel();
    int maxUsed = await _orgLabels.GetMaxUsedLevelAsync();
    int displayMax = Math.Max(maxConfig, maxUsed);

    // Pre-compute which levels are "in use" for the disable-Delete decision
    var usedLevels = await _context.OrganizationUnits
        .Select(u => u.Level)
        .Distinct()
        .ToListAsync();
    var usedSet = new HashSet<int>(usedLevels);

    var rows = new List<LabelRowVM>();
    for (int level = 0; level <= displayMax; level++)
    {
        bool hasLabel = labels.TryGetValue(level, out var lbl);
        bool isHighest = (level == maxConfig);
        bool isUsed = usedSet.Contains(level);
        rows.Add(new LabelRowVM
        {
            Level = level,
            Label = hasLabel ? lbl! : null,
            IsHighest = isHighest,
            IsUsed = isUsed,
            CanDelete = hasLabel && isHighest && !isUsed
        });
    }

    // Buffer row (level = displayMax + 1) "(belum diset)"
    rows.Add(new LabelRowVM
    {
        Level = displayMax + 1,
        Label = null,
        IsHighest = false,
        IsUsed = false,
        CanDelete = false
    });

    var vm = new ManageOrgLevelLabelsViewModel
    {
        Rows = rows,
        MaxConfigured = maxConfig,
        MaxUsed = maxUsed,
        NextAddLevel = displayMax + 1
    };
    return View(vm);
}
```

### Example 2 — POST UpdateLevelLabel action (Plan 01)

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> UpdateLevelLabel(int level, string label)
{
    if (string.IsNullOrWhiteSpace(label))
        return Json(new { success = false, message = "Label tidak boleh kosong." });

    label = label.Trim();

    if (label.Length > 50)
        return Json(new { success = false, message = "Label maksimal 50 karakter." });

    bool duplicate = await _context.OrganizationLevelLabels
        .AnyAsync(l => l.Label == label && l.Level != level);
    if (duplicate)
        return Json(new { success = false, message = $"Label '{label}' sudah dipakai level lain." });

    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";

    try
    {
        await _orgLabels.UpdateAsync(level, label, currentUser?.Id ?? "", actorName);
        return Json(new { success = true, message = $"Label level {level} berhasil diubah menjadi '{label}'." });
    }
    catch (InvalidOperationException ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
```

### Example 3 — POST AddLevelLabel (D-08 constraint) (Plan 01)

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddLevelLabel(int level, string label)
{
    // D-08: prevent arbitrary level injection
    int expectedNext = _orgLabels.GetMaxConfiguredLevel() + 1;
    if (level != expectedNext)
        return Json(new { success = false, message = $"Hanya level berikutnya (Level {expectedNext}) yang bisa ditambahkan." });

    if (string.IsNullOrWhiteSpace(label))
        return Json(new { success = false, message = "Label tidak boleh kosong." });
    label = label.Trim();
    if (label.Length > 50)
        return Json(new { success = false, message = "Label maksimal 50 karakter." });

    bool duplicate = await _context.OrganizationLevelLabels.AnyAsync(l => l.Label == label);
    if (duplicate)
        return Json(new { success = false, message = $"Label '{label}' sudah dipakai level lain." });

    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";

    try
    {
        await _orgLabels.AddAsync(level, label, currentUser?.Id ?? "", actorName);
        return Json(new { success = true, message = $"Level {level} '{label}' berhasil ditambahkan." });
    }
    catch (InvalidOperationException ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
    catch (DbUpdateException)
    {
        return Json(new { success = false, message = "Level sudah ada, silakan refresh halaman." });
    }
}
```

### Example 4 — POST DeleteLevelLabel (Plan 01)

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteLevelLabel(int level)
{
    int maxConfig = _orgLabels.GetMaxConfiguredLevel();
    if (level != maxConfig)
        return Json(new { success = false, message = "Hanya level tertinggi yang bisa dihapus." });

    bool isUsed = await _context.OrganizationUnits.AnyAsync(u => u.Level == level);
    if (isUsed)
        return Json(new { success = false, message = "Level masih dipakai unit, tidak bisa dihapus." });

    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";

    try
    {
        await _orgLabels.DeleteAsync(level, currentUser?.Id ?? "", actorName);
        return Json(new { success = true, message = $"Level {level} berhasil dihapus." });
    }
    catch (InvalidOperationException ex)
    {
        return Json(new { success = false, message = ex.Message });
    }
}
```

### Example 5 — ViewModel (Plan 01)

```csharp
namespace HcPortal.Models.ViewModels
{
    public class ManageOrgLevelLabelsViewModel
    {
        public List<LabelRowVM> Rows { get; set; } = new();
        public int MaxConfigured { get; set; }
        public int MaxUsed { get; set; }
        public int NextAddLevel { get; set; }
    }

    public class LabelRowVM
    {
        public int Level { get; set; }
        public string? Label { get; set; }  // null = "(belum diset)" buffer row
        public bool IsHighest { get; set; }
        public bool IsUsed { get; set; }
        public bool CanDelete { get; set; }
    }
}
```

### Example 6 — View skeleton (Plan 02)

```html
@model HcPortal.Models.ViewModels.ManageOrgLevelLabelsViewModel
@{
    ViewData["Title"] = "Kelola Label Tier Organisasi";
}

<div class="container-fluid px-4 py-4">
    <nav aria-label="breadcrumb" class="mb-3">
        <ol class="breadcrumb">
            <li class="breadcrumb-item"><a href="@Url.Action("Index", "Admin")">Kelola Data</a></li>
            <li class="breadcrumb-item active" aria-current="page">Label Tier Organisasi</li>
        </ol>
    </nav>

    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h2 class="fw-bold mb-1"><i class="bi bi-tags text-primary me-2"></i>Kelola Label Tier Organisasi</h2>
            <p class="text-muted mb-0">Ubah nama tampilan tier (Bagian/Unit/Sub-unit/...)</p>
        </div>
    </div>

    <div class="alert alert-info">
        <i class="bi bi-info-circle me-2"></i>
        Label ini hanya nama tampilan tier. Mengubah label TIDAK mengubah struktur data atau tier.
        Cascade backend tetap berlaku berdasarkan level numerik.
    </div>

    @Html.AntiForgeryToken()

    <div class="card border-0 shadow-sm">
        <div class="card-body">
            <table class="table table-hover mb-0">
                <thead>
                    <tr>
                        <th style="width:120px;">Level</th>
                        <th>Label saat ini</th>
                        <th style="width:200px;">Aksi</th>
                    </tr>
                </thead>
                <tbody>
                @foreach (var row in Model.Rows)
                {
                    <tr>
                        <td>@row.Level</td>
                        @if (row.Label == null)
                        {
                            <td><em class="text-muted">(belum diset)</em></td>
                            <td>
                                @if (row.Level == Model.NextAddLevel)
                                {
                                    <button class="btn btn-sm btn-outline-primary"
                                            onclick="openAddModal(@row.Level)">
                                        <i class="bi bi-plus-circle me-1"></i>Tambah
                                    </button>
                                }
                            </td>
                        }
                        else
                        {
                            <td>@row.Label</td>
                            <td>
                                <button class="btn btn-sm btn-outline-secondary"
                                        onclick="openEditModal(@row.Level, '@Html.Raw(Json.Serialize(row.Label).ToString().Trim('"'))')">
                                    <i class="bi bi-pencil-square me-1"></i>Edit
                                </button>
                                @if (row.CanDelete)
                                {
                                    <button class="btn btn-sm btn-outline-danger ms-1"
                                            onclick="confirmDelete(@row.Level, '@Html.Raw(Json.Serialize(row.Label).ToString().Trim('"'))')">
                                        <i class="bi bi-trash me-1"></i>Delete
                                    </button>
                                }
                                else if (row.IsHighest && row.IsUsed)
                                {
                                    <button class="btn btn-sm btn-outline-danger ms-1" disabled
                                            data-bs-toggle="tooltip"
                                            title="Tidak bisa dihapus, masih dipakai unit">
                                        <i class="bi bi-trash me-1"></i>Delete
                                    </button>
                                }
                            </td>
                        }
                    </tr>
                }
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modals (Edit + Add) per Pattern 4 -->
<!-- ... see CONTEXT.md §Specific Ideas for exact UX copy ... -->

@section Scripts {
    <script src="~/js/shared-toast.js"></script>
    <script>
        function getAntiForgeryToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]').value;
        }

        async function ajaxPost(url, data) {
            const params = new URLSearchParams(data);
            params.append('__RequestVerificationToken', getAntiForgeryToken());
            const res = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body: params.toString()
            });
            return res.json();
        }

        function openEditModal(level, label) { /* set hidden + display + show */ }
        function openAddModal(level)        { /* set hidden + display + show */ }

        async function submitEdit() {
            const level = document.getElementById('labelEditLevel').value;
            const label = document.getElementById('labelEditValue').value;
            const result = await ajaxPost('/Admin/UpdateLevelLabel', { level, label });
            if (result.success) {
                showToast(result.message, 'success');
                setTimeout(() => window.location.reload(), 600);
            } else {
                showToast(result.message, 'danger');
            }
        }

        async function submitAdd() { /* similar to submitEdit */ }

        async function confirmDelete(level, label) {
            if (!confirm(`Hapus label Level ${level} "${label}"? Tidak bisa diundo.`)) return;
            const result = await ajaxPost('/Admin/DeleteLevelLabel', { level });
            if (result.success) {
                showToast(result.message, 'success');
                setTimeout(() => window.location.reload(), 600);
            } else {
                showToast(result.message, 'danger');
            }
        }
    </script>
}
```

---

## State of the Art

| Old Approach (pre-Phase 311) | Current Codebase Approach | When Changed | Impact |
|------------------------------|---------------------------|--------------|--------|
| Razor form POST + RedirectToAction + TempData | Fetch + JSON + toast + window.reload (D-01) | v12.0 (Phase 292+) | Phase 341 follows |
| Sweet Alert delete confirm | Native `confirm()` (D-03) | v11.0 (consistent codebase-wide) | Phase 341 follows |
| Manual `bootstrap.Modal.getOrCreateInstance` | Standard `<button data-bs-toggle="modal" data-bs-target="#x">` + JS-driven for dynamic open | v10.0+ | Phase 341 follows |

**Deprecated/outdated:**
- HTMX `hx-vals` ancestor inheritance gotcha — Phase 322 learning: avoid `hx-vals` in this codebase. Phase 341 doesn't use HTMX at all.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + Microsoft.EntityFrameworkCore.InMemory 8.0.0 |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` |
| Quick run command | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~OrgLabelControllerTests"` |
| Full suite command | `dotnet test HcPortal.Tests` |
| Estimated runtime | ~1s (quick) / ~4s (full suite, 31 + 5 new tests = 36) |
| Test isolation | per-`[Fact]` `Guid.NewGuid()` InMemory DB (no cross-test state) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|--------------|
| ORG-LABEL-04 | Page renders for Admin+HC role, builds correct Rows + CanDelete flags | unit | `dotnet test --filter "ManageOrgLevelLabels_AsAdmin_ReturnsViewWithRowsAndBufferRow"` | ❌ Wave 0 |
| ORG-LABEL-04 | Page denies non-Admin/HC role (returns 403/Forbid result) | unit (or accept Manual UAT — see note) | `dotnet test --filter "ManageOrgLevelLabels_AsCoach_ReturnsForbid"` | ❌ Wave 0 OR Manual |
| ORG-LABEL-05 | Audit log written on Update (already covered Phase 340 `UpdateAsync_KnownLevel_...LogsAudit`) | unit (existing) | `dotnet test --filter "OrgLabelServiceTests"` | ✅ exists |
| ORG-LABEL-06 | UpdateLevelLabel rejects empty label with friendly JSON | unit | `dotnet test --filter "UpdateLevelLabel_EmptyLabel_ReturnsErrorJson"` | ❌ Wave 0 |
| ORG-LABEL-06 | UpdateLevelLabel rejects label >50 char | unit | `dotnet test --filter "UpdateLevelLabel_TooLong_ReturnsErrorJson"` | ❌ Wave 0 |
| ORG-LABEL-06 | UpdateLevelLabel rejects duplicate label across levels | unit | `dotnet test --filter "UpdateLevelLabel_DuplicateAcrossLevels_ReturnsErrorJson"` | ❌ Wave 0 |
| ORG-LABEL-04 (D-08) | AddLevelLabel rejects level != GetMaxConfiguredLevel+1 | unit | `dotnet test --filter "AddLevelLabel_ArbitraryLevel_Rejected"` | ❌ Wave 0 |
| ORG-LABEL-04 | DeleteLevelLabel rejects mid-tier level | unit | `dotnet test --filter "DeleteLevelLabel_NotHighest_Rejected"` | ❌ Wave 0 |
| ORG-LABEL-04 | DeleteLevelLabel rejects in-use level | unit | `dotnet test --filter "DeleteLevelLabel_StillUsed_Rejected"` | ❌ Wave 0 |
| ORG-LABEL-04 SC2 | End-to-end browser: "Bagian"→"Direktorat" rename happy path | manual (Playwright optional) | `dotnet run` + Playwright MCP smoke | Manual UAT |
| ORG-LABEL-04 SC5 | End-to-end browser: Delete tombol visibility rule | manual | `dotnet run` + Playwright MCP smoke | Manual UAT |
| ORG-LABEL-05 audit | End-to-end browser: AuditLog entry verified post-rename | manual SQL | `sqlcmd ... SELECT TOP 1 * FROM AuditLogs WHERE ActionType='OrgLabel-Update' ORDER BY Id DESC` | Manual UAT |

### Sampling Rate
- **Per task commit:** `dotnet test HcPortal.Tests --filter "OrgLabelControllerTests|OrgLabelServiceTests"` (~2s)
- **Per wave merge:** `dotnet test HcPortal.Tests` (~4s)
- **Phase gate:** Full suite green + manual UAT SC1-SC5 PASS before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `HcPortal.Tests/OrgLabelControllerTests.cs` — 5-8 new [Fact]; uses InMemory DbContext pattern from `OrgLabelServiceTests.MakeServiceWithCtx`; instantiates controller directly with mock `UserManager<ApplicationUser>` via `MockHelpers` from existing Identity test patterns OR uses `ControllerContext` simulation.
- [ ] Test fixture for `ApplicationUser` role simulation — for permission tests. **Alternative:** SKIP automated permission tests for now (escalate to MANUAL like Phase 340 G7/G8 — `WebApplicationFactory + cookie auth` infrastructure is out-of-proportion for 1 phase). MANUAL test = login as Coach via Playwright + verify 403 page. Recommend MANUAL path consistent with Phase 340 precedent.

**Permission test handling decision required by planner:**
- **Option A (recommend):** Permission test = MANUAL UAT (login Coach role, verify 403). Matches Phase 340 G7 escalation precedent.
- **Option B:** Add `Microsoft.AspNetCore.Mvc.Testing` package + `WebApplicationFactory<Program>` fixture. Higher infra cost, gives automated cookie-auth integration tests.

### Test stub sketch

```csharp
// HcPortal.Tests/OrgLabelControllerTests.cs
public class OrgLabelControllerTests
{
    private static (OrgLabelController ctrl, ApplicationDbContext ctx, OrgLabelService svc) MakeController(string userId = "user-1")
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ctx = new ApplicationDbContext(options);
        ctx.OrganizationLevelLabels.AddRange(
            new OrganizationLevelLabel { Level = 0, Label = "Bagian",   UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 1, Label = "Unit",     UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" },
            new OrganizationLevelLabel { Level = 2, Label = "Sub-unit", UpdatedAt = DateTime.UtcNow, UpdatedBy = "system" }
        );
        ctx.SaveChanges();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = new OrgLabelService(ctx, cache, new AuditLogService(ctx));
        var userManager = MockUserManager(userId);  // helper builds UserManager<ApplicationUser> stub
        var ctrl = new OrgLabelController(ctx, userManager, svc, /* env */ null!);
        // Simulate authenticated user for User.Identity:
        ctrl.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                }, "TestAuth"))
            }
        };
        return (ctrl, ctx, svc);
    }

    [Fact]
    public async Task UpdateLevelLabel_EmptyLabel_ReturnsErrorJson()
    {
        var (ctrl, _, _) = MakeController();
        var result = await ctrl.UpdateLevelLabel(0, "");
        var json = Assert.IsType<JsonResult>(result);
        // Reflect into anonymous object — using dynamic OR JObject
        // ... assert success=false, message contains "kosong"
    }

    [Fact]
    public async Task UpdateLevelLabel_TooLong_ReturnsErrorJson() { /* 51-char string */ }

    [Fact]
    public async Task UpdateLevelLabel_DuplicateAcrossLevels_ReturnsErrorJson() { /* try "Unit" on level 0 */ }

    [Fact]
    public async Task AddLevelLabel_ArbitraryLevel_Rejected() { /* level=99 when max=2 */ }

    [Fact]
    public async Task DeleteLevelLabel_NotHighest_Rejected() { /* level=0 when max=2 */ }

    [Fact]
    public async Task DeleteLevelLabel_StillUsed_Rejected()
    {
        var (ctrl, ctx, _) = MakeController();
        ctx.OrganizationUnits.Add(new OrganizationUnit { Name="X", Level=2, IsActive=true });
        ctx.SaveChanges();
        var result = await ctrl.DeleteLevelLabel(2);
        // assert success=false, message contains "dipakai"
    }
}
```

**Note on `JsonResult` value inspection:** Use `((dynamic)json.Value).success` OR `Newtonsoft.Json.Linq.JObject.FromObject(json.Value)["success"]` OR introduce a typed response record. Planner picks simplest approach.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity cookie auth (existing, configured Program.cs) |
| V3 Session Management | yes | ASP.NET Core Identity session cookie (existing) |
| V4 Access Control | yes | `[Authorize(Roles="Admin,HC")]` method-level (D-09) — enforces RBAC |
| V5 Input Validation | yes | Inline `IsNullOrWhiteSpace` + `Length` + `AnyAsync` duplicate check (D-05); D-08 level constraint |
| V6 Cryptography | no | No new crypto — Identity hashing already configured |
| V13 API & Web Service | yes | `[ValidateAntiForgeryToken]` on all POST (D-06) — CSRF protection |
| V14 Configuration | yes | `[Authorize]` class-level + method-level override pattern |

### Known Threat Patterns for ASP.NET Core Admin CRUD

| Threat ID | Pattern | STRIDE | Standard Mitigation | Phase 341 Status |
|-----------|---------|--------|---------------------|------------------|
| T-341-01 | CSRF on POST mutation | Tampering | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` + JS token append | **MITIGATED** D-06 |
| T-341-02 | Non-Admin/HC accesses page | EoP | `[Authorize(Roles="Admin,HC")]` method-level on all 4 actions | **MITIGATED** D-09 |
| T-341-03 | Arbitrary level injection (Add level=99) | Tampering | Server constraint `level == GetMaxConfiguredLevel() + 1` | **MITIGATED** D-08 |
| T-341-04 | Validation bypass via devtools (empty/long/dup) | Tampering | Inline server-side checks (D-05); client preview optional (C-03) | **MITIGATED** D-05 |
| T-341-05 | Mid-tier delete via devtools | Tampering | Server check `level == GetMaxConfiguredLevel()` + `!AnyAsync(u => u.Level == level)` | **MITIGATED** controller check |
| T-341-06 | Audit log description leaks PII | Info Disclosure | Label rename = public display info, no PII | **ACCEPTED** (low) |
| T-341-07 | Cache stampede on rapid edit | DoS | Single-admin volume; `IMemoryCache GetOrCreate` thread-safe | **ACCEPTED** (low) |
| T-341-08 (new) | Race on concurrent Add same level | Tampering | EF PK unique constraint on Level + catch `DbUpdateException` → friendly message | **MITIGATED** Pitfall 8 |
| T-341-09 (new) | Stored XSS via label field | Tampering / Info Disclosure | Razor `@row.Label` auto-encodes; JS interpolation MUST use `JSON.stringify` or HTML-encode via server-side `Json.Serialize` | **MITIGATED** Razor auto-encode + Example 6 pattern |

### Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Build + test | ✓ | 8.0.x | — |
| SQL Server LocalDB or SQLEXPRESS | Lokal dev DB | ✓ | (existing Phase 340 verified) | — |
| `OrgLabelService` (Phase 340) | Phase 341 actions | ✓ | shipped commit `3a3aa3b9` | — |
| `wwwroot/js/shared-toast.js` | Toast feedback | ✓ | existing | — |
| `wwwroot/js/orgTree.js` antiforgery helper | Reference for inline JS | ✓ | existing (reuse or inline-copy) | — |
| Bootstrap 5 + Bootstrap Icons | Modal + icons | ✓ | existing layout | — |
| `MemoryCache` DI | Phase 340 service dep | ✓ | already registered Program.cs | — |
| Playwright MCP | Manual UAT browser smoke | ✓ (per CONTEXT.md prior phase usage) | — | Manual confirm dialog click |

**Missing dependencies with no fallback:** NONE.
**Missing dependencies with fallback:** NONE.

All infrastructure exists. Phase 341 is pure consumption + UI layer.

---

## DB Handoff (IT)

**EXPLICIT: NONE NEEDED.**

Phase 341 has **ZERO schema change, ZERO migration, ZERO seed change**:
- No `dotnet ef migrations add` step in any plan
- No `Data/SeedData.cs` modification
- No `docs/DB_HANDOFF_IT_2026-MM-DD.html` generation
- No IT notification needed

Planner MUST NOT include "generate IT handoff" task. This is purely an application-tier delivery (controller + view + ViewModel + JS + tests). Promotion to Dev = just `git push` after lokal verify per `docs/DEV_WORKFLOW.md` SOP — IT pulls and rebuilds, no DB step.

---

## Assumptions Log

| # | Claim | Section | Confidence | Risk if Wrong |
|---|-------|---------|-----------|---------------|
| A1 | `OrganizationLevelLabel.Level` is EF PK (single-property primary key) — duplicate insert throws `DbUpdateException` | Pitfall 8 | [VERIFIED: Models/OrganizationLevelLabel.cs L9 + Phase 340 migration verified §340-VALIDATION manual SQL] | If not PK, race condition allows silent dup insert |
| A2 | SQL Server default collation `CI_AS` (case-insensitive) — `AnyAsync(l.Label == "bagian")` matches `"Bagian"` | Pitfall 9 | [CITED: SQL Server default install collation] | If case-sensitive collation, unit may differ across rows differing only in case |
| A3 | `UserManager<ApplicationUser>` resolves `currentUser.NIP` and `currentUser.FullName` properties for actor name | Pattern 2, Example 2 | [VERIFIED: Controllers/OrganizationController.cs:425-428 actual production usage] | If properties renamed, audit logs show "Unknown" |
| A4 | `shared-toast.js` `showToast(msg, 'danger')` produces Bootstrap red alert with exclamation icon | Pattern 6, OQ#4 | [VERIFIED: wwwroot/js/shared-toast.js L7-9 source reading] | If only accepts 'success'/'error', error toasts mis-styled |
| A5 | Razor view discovery — `OrgLabelController.View()` default looks in `Views/OrgLabel/` not `Views/Admin/` | Pitfall 1, Pattern 9 | [VERIFIED: ASP.NET Core MVC convention + `OrganizationController.cs:24-27` override pattern proves this exists] | Without override, 404 on first GET |
| A6 | Existing `OrgLabelController` constructor (Phase 340) takes only `IOrgLabelService` — extending requires adding `ApplicationDbContext` + `UserManager` deps for the new actions | §Existing Code Insights | [VERIFIED: Controllers/OrgLabelController.cs:14-20 current constructor signature] | Planner may forget to add deps; build fails fast |
| A7 | `Models/ViewModels/` namespace `HcPortal.Models.ViewModels` is acceptable convention per `CMPRecordsViewModel.cs` precedent | Pattern 8, OQ#1 | [VERIFIED: Models/ViewModels/CMPRecordsViewModel.cs L3 namespace declaration] | Alternative is flat `Models/` — both work |
| A8 | `dotnet test` runs cleanly in ~4s with 36 tests (31 existing + 5 new) | Validation Architecture | [CITED: Phase 340 final tally 31/31 in 3s per 340-VALIDATION.md L24] | Slight overestimate; non-blocking |
| A9 | `[Authorize(Roles="Admin,HC")]` (with space after comma) is the established convention | §Standard Stack | [VERIFIED: codebase grep — 161 occurrences across 11 controllers per grep output] | Equivalent without space works too; not a real risk |
| A10 | `OrgLabelController` already routes via `[Route("Admin/[action]")]` so new actions auto-route to `/Admin/ManageOrgLevelLabels`, `/Admin/UpdateLevelLabel`, etc. | §Existing Code | [VERIFIED: Controllers/OrgLabelController.cs L12] | If route attr removed, URLs break |

**No `[ASSUMED]` (training-only) claims in this research** — all claims grounded in codebase read or established ASP.NET Core MVC conventions. Assumptions log captures specifically verified facts that planner relies on.

---

## Open Questions — Resolved

### OQ#1 — ViewModel placement

**Q:** `Models/ViewModels/` directory vs inline in Controllers?

**Resolution:** Use `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` namespace `HcPortal.Models.ViewModels`. Precedent: `Models/ViewModels/CMPRecordsViewModel.cs` from v20.0 (latest convention). Controller adds `using HcPortal.Models.ViewModels;`. Keeps Models root clean.

### OQ#2 — Modal markup exact pattern

**Q:** Exact Bootstrap 5 `aria-*` attrs from `ManageOrganization.cshtml`?

**Resolution:** Verified pattern (lines 134-164):
- `<div class="modal fade" id="labelEditModal" tabindex="-1" aria-labelledby="labelEditModalLabel" aria-hidden="true">`
- `<div class="modal-dialog">` (no `modal-lg` / no `modal-dialog-centered` — matches existing)
- `<div class="modal-header bg-primary text-white">` + `<h5 class="modal-title" id="labelEditModalLabel">Edit Label Tier Level X</h5>` + `<button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>`
- Body uses `<input type="hidden" id="labelEditLevel" />` + form-control `<input maxlength="50">` + `<div class="invalid-feedback">` empty placeholder
- Footer `<button class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>` + `<button class="btn btn-primary" onclick="submitEdit()">Simpan</button>`

Replicate verbatim for both Edit (`labelEditModal`) and Add (`labelAddModal`) — distinct ids.

### OQ#3 — JS bundling

**Q:** Inline `<script>` vs separate `wwwroot/js/orgLabelCrud.js`?

**Resolution:** **Inline `<script>` in `@section Scripts`** of `ManageOrgLevelLabels.cshtml`. Rationale:
- Page is ~40-60 lines of JS (open modals + 3 submit handlers + confirmDelete). Below threshold where separate file pays off.
- ManageAssessment.cshtml uses inline + shared-toast; ManageOrganization uses orgTree.js because tree rendering is 200+ LoC + reused tabular layout.
- Single-page consumption — no DRY benefit.
- Keep inline-copy of `getAntiForgeryToken` + `ajaxPost` (8 lines) instead of cross-page coupling to `orgTree.js` (which is opinionated to tree rendering).

If JS grows past ~80 LoC during planning, planner can split out — non-blocking.

### OQ#4 — Toast color naming

**Q:** Does `shared-toast.js` accept both `'danger'` and `'error'`?

**Resolution:** Source `wwwroot/js/shared-toast.js:9` builds class `'alert alert-' + type`. Bootstrap valid alert variants: `success`, `danger`, `warning`, `info`, `primary`, `secondary`, `light`, `dark`. `'error'` is NOT a Bootstrap variant — would produce `alert alert-error` with no styling, defaulting to inherited `<div>` look (invisible alert).

**Use `'danger'`** (matches Bootstrap convention + codebase consistency). Plan 02 instructions must specify `'danger'` literal in JS.

---

## Recommended Plan Breakdown

Per ROADMAP estimate ~1 hari kerja. Recommend 3 sequential plans:

### Plan 01: Controller Actions + ViewModel (foundation)

**Files touched:** 2
- `Models/ViewModels/ManageOrgLevelLabelsViewModel.cs` — NEW, ~25 LoC (per Example 5)
- `Controllers/OrgLabelController.cs` — EXTEND, +~120 LoC (4 new actions per Examples 1-4 + View() overrides per Pattern 9 + constructor deps `ApplicationDbContext`, `UserManager<ApplicationUser>`, keep existing `IOrgLabelService`; consider inheriting `AdminBaseController` for free `IsAjaxRequest` + actor pattern — recommend keeping standalone to avoid scope creep)

**Acceptance:** `dotnet build` PASS, no view yet (manual `curl -H "X-Requested-With: XMLHttpRequest" /Admin/UpdateLevelLabel` returns expected JSON). Implementation complete but no Razor view yet.

**Dependencies:** Phase 340 SHIPPED (✓).

**Threats addressed:** T-341-01 (`[ValidateAntiForgeryToken]`), T-341-02 (`[Authorize(Roles="Admin,HC")]`), T-341-03 (D-08 constraint), T-341-04 (D-05 inline checks), T-341-05 (mid-tier delete check), T-341-08 (catch `DbUpdateException`), T-341-09 (server-side label content trusted; XSS on display = Razor auto-encode in Plan 02).

### Plan 02: View + JS + Navigation Card (UI)

**Files touched:** 2
- `Views/Admin/ManageOrgLevelLabels.cshtml` — NEW, ~200-220 LoC (per Example 6 — breadcrumb + info banner + table loop + 2 modals + inline `<script>`)
- `Views/Admin/Index.cshtml` — EXTEND, +14 LoC (1 admin card block after L49 per Pattern 7)

**Acceptance:** Lokal browser test `http://localhost:5277/Admin` → see new card → click → `/Admin/ManageOrgLevelLabels` 200 → edit "Bagian" → "Direktorat" → toast success + reload shows new label.

**Dependencies:** Plan 01 actions wired.

**Threats addressed:** T-341-09 (Razor `@Html.Raw(Json.Serialize(...))` pattern for JS string interpolation).

### Plan 03: xUnit Controller Tests + Manual UAT (verification)

**Files touched:** 1
- `HcPortal.Tests/OrgLabelControllerTests.cs` — NEW, ~250 LoC (5-7 [Fact] per Validation Architecture stub)

**Acceptance:**
- `dotnet test HcPortal.Tests` → 36/36 PASS (31 existing + 5-7 new)
- Manual UAT 5 SC per ROADMAP §Phase 341:
  - SC1 page Admin+HC 200, Coach 403 — **Manual UAT** (login Coach → expect 403)
  - SC2 edit "Bagian"→"Direktorat" → toast + reload — **Manual UAT** Playwright
  - SC3 AuditLog entry — **Manual SQL**: `sqlcmd ... SELECT TOP 1 * FROM AuditLogs WHERE ActionType='OrgLabel-Update' ORDER BY Id DESC`
  - SC4 validation reject empty / dup / >50 — **Automated [Fact] in Plan 03**
  - SC5 delete button visibility rule — **Manual UAT** Playwright (verify in-use level disabled with tooltip)

**Dependencies:** Plan 01 + Plan 02 shipped.

**Permission test escalation note:** Per Validation Architecture §Wave 0 Gaps, recommend permission tests be MANUAL (matches Phase 340 G7/G8 escalation precedent — adding `Microsoft.AspNetCore.Mvc.Testing` is out-of-proportion). If planner picks Option B (add package), Plan 03 expands +1 task for WebApplicationFactory fixture.

### Parallelization opportunity?

Phase 340 was 3 sequential plans (10 commits over ~1 day). Phase 341 Plans 01+02 are tightly coupled (view consumes controller actions) — sequential strict. Plan 03 could in theory parallelize with Plan 02 (writing tests against Plan 01 stubs while view is built), but workspace cost > savings for ~1 day phase. **Recommend sequential strict 01 → 02 → 03**.

---

## Sources

### Primary (HIGH confidence — direct codebase read)

- `Controllers/OrgLabelController.cs` (Phase 340 — 32 LoC) — controller to extend
- `Services/IOrgLabelService.cs` + `Services/OrgLabelService.cs` (Phase 340) — service contract + impl
- `Controllers/OrganizationController.cs` L11-275, L410-440 — JSON action pattern, antiforgery, actor name resolution
- `Controllers/AdminBaseController.cs` — `IsAjaxRequest()` helper at L32
- `Views/Admin/ManageOrganization.cshtml` L1-200 — Bootstrap modal markup + scripts section
- `Views/Admin/Index.cshtml` L35-49 — admin card pattern (D-04 target location)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` L315-340 — native `confirm()` delete pattern (D-03 reference, but adapted to fetch+JSON not form POST)
- `wwwroot/js/orgTree.js` L1-30 — `getAntiForgeryToken` + `ajaxPost` JS pattern
- `wwwroot/js/shared-toast.js` L1-15 — `showToast` API spec
- `Models/ViewModels/CMPRecordsViewModel.cs` — ViewModel namespace convention (OQ#1)
- `Models/OrganizationLevelLabel.cs` — entity confirms Level is PK
- `HcPortal.Tests/OrgLabelServiceTests.cs` L1-294 — xUnit InMemory pattern reference
- `HcPortal.Tests/HcPortal.Tests.csproj` — verified xUnit 2.9.3 + InMemory 8.0.0
- `Program.cs` L65 — `AddScoped<IOrgLabelService, OrgLabelService>` confirmed
- `.planning/phases/341-label-crud-page/341-CONTEXT.md` — user-locked decisions
- `.planning/milestones/v21.0-REQUIREMENTS.md` ORG-LABEL-04/05/06 — requirement definitions
- `.planning/milestones/v21.0-ROADMAP.md` §Phase 341 — 5 success criteria
- `.planning/phases/340-foundation-org-label-table-service-cache/340-VALIDATION.md` — Phase 340 test infra inventory + escalation precedent
- `.planning/phases/340-foundation-org-label-table-service-cache/340-02-SUMMARY.md` — Phase 340 service ship verification
- `docs/superpowers/specs/2026-06-02-manageorganization-overhaul-design.md` §4.3, §4.4 — page UX + validation spec
- `CLAUDE.md` — project Indonesian + dev workflow constraints

### Secondary (MEDIUM confidence — convention-level)

- ASP.NET Core 8 MVC view discovery convention (`Views/{Controller}/{Action}.cshtml` default) — standard documented behavior, verified by `OrganizationController.View()` override pattern in codebase
- Bootstrap 5 modal a11y conventions (`aria-labelledby`, `aria-hidden`, `data-bs-dismiss`) — standard Bootstrap 5 documented pattern, verified by codebase usage

### Tertiary (LOW confidence — none required)

No findings rely on unverified WebSearch / training-only knowledge. All claims grounded in codebase or established framework conventions.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries verified in `HcPortal.csproj` / `HcPortal.Tests.csproj`
- Architecture: HIGH — all patterns lifted directly from neighbor files
- Pitfalls: HIGH — pitfalls 1-9 grounded in observed codebase behavior + ASP.NET Core known gotchas

**Research date:** 2026-06-03
**Valid until:** 2026-07-03 (30-day stable horizon — no fast-moving deps in scope)

**Pre-submission checklist:**
- [x] All domains investigated (controller, view, JS, validation, security, testing)
- [x] Negative claims verified (e.g., "SweetAlert not in package list" — verified by absence in csproj package list)
- [x] Multiple sources cross-referenced for critical claims
- [x] Confidence levels assigned honestly
- [x] "What might I have missed?" review — verified
- [x] Phase 341 NOT rename/refactor → Runtime State Inventory section marked N/A with reason
- [x] Security domain included (security_enforcement enabled default)
- [x] ASVS categories verified against ASP.NET Core MVC tech stack
- [x] DB_HANDOFF_IT explicit NONE statement included
- [x] Validation Architecture per Nyquist
- [x] All 4 open questions resolved with sources
- [x] All `## User Constraints` section copied verbatim from CONTEXT.md
- [x] Phase requirement IDs mapped to research support per ORG-LABEL-04/05/06
