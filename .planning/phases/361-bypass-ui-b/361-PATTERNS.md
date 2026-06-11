# Phase 361: Bypass UI (B) - Pattern Map

**Mapped:** 2026-06-11
**Files analyzed:** 4 (2 modified + 2 new)
**Analogs found:** 4 / 4 (all files have a strong in-codebase analog)

> **Phase nature:** UI-only. New files copy patterns from a single dominant analog (`Views/ProtonData/Override.cshtml`) plus 3 supporting analogs (nav-tabs, coach-list, seed/e2e infra). NO new domain logic — backend 360 (`ProtonDataController.cs:1499-1684`) is the authority for every decision. Planner: every excerpt below is verbatim from a verified source; copy the structure, swap the entity names.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/ProtonData/Override.cshtml` (modify — wrap Tab1, add Tab2 + wizard modal + confirm modal + pending panel + Tab2 IIFE) | view (Razor) | request-response (fetch JSON) | **self** — existing Tab1 markup + JS in same file | exact (same file, same page) |
| `Controllers/ProtonDataController.cs` (modify — `Override()` GET +coach ViewBag; `BypassPendingList` SELECT extend D-18) | controller | request-response (GET render + JSON) | **self** — `Override()` `:231-243` + `BypassPendingList` `:1534-1559` | exact (extend existing methods) |
| `tests/e2e/proton-bypass.spec.ts` (new) | test (e2e) | event-driven (UI flow assert) | `tests/e2e/image-in-assessment.spec.ts` | exact (serial UAT + DB snapshot/restore) |
| `.planning/seeds/361-bypass-fixtures.sql` (new — path = planner discretion, `.planning/seeds/` or `tests/sql/`) | migration/fixture (SQL seed) | batch (WIPE-AND-INSERT) | `.planning/seeds/313-timer-fixtures.sql` | exact (multi-state fixture pattern) |

**Match quality note:** This is the rare phase where the best analog for the two modified files is the file itself. Planner should treat existing Tab1 JS (`Override.cshtml:186-445` IIFE) as the canonical template for the Tab2 IIFE — same fetch shape, same `escHtml`, same spinner, same `appUrl`. Do NOT invent new conventions.

---

## Pattern Assignments

### `Views/ProtonData/Override.cshtml` (view, request-response)

**Analog:** self (Tab1 markup + JS) + `Views/Admin/ManageAssessment.cshtml` (nav-tabs shell only)

**Imports / page setup pattern** (`Override.cshtml:1-7, 22`):
```cshtml
@using HcPortal.Models
@{
    ViewData["Title"] = "Deliverable Progress Override";
    ViewData["ContainerClass"] = "container-fluid";
    var allTracks = ViewBag.AllTracks as List<ProtonTrack> ?? new List<ProtonTrack>();
    // [NEW 361] var allCoaches = ViewBag.AllCoaches as List<ApplicationUser> ?? new List<ApplicationUser>();
}
@* AntiForgeryToken for POST actions — already present, reuse for Tab2 *@
@Html.AntiForgeryToken()
```
> CRITICAL: `@Html.AntiForgeryToken()` at `:22` already renders the hidden `__RequestVerificationToken` input. Tab2 fetch reuses it — do NOT add a second token.

**Nav-tabs shell pattern** (copy structure from `ManageAssessment.cshtml:68-92`, adapt to 2 tabs):
```html
<!-- Source: ManageAssessment.cshtml:68-87 (nav-tabs) — Tab1 markup goes UNCHANGED inside #pane-deliverable -->
<ul class="nav nav-tabs mb-0" id="overrideTabs" role="tablist">
  <li class="nav-item" role="presentation">
    <button class="nav-link active" id="tab-deliverable" data-bs-toggle="tab"
            data-bs-target="#pane-deliverable" type="button" role="tab"
            aria-controls="pane-deliverable" aria-selected="true">
      <i class="bi bi-pencil-square me-1"></i> Override Deliverable
    </button>
  </li>
  <li class="nav-item" role="presentation">
    <button class="nav-link" id="tab-bypass" data-bs-toggle="tab"
            data-bs-target="#pane-bypass" type="button" role="tab"
            aria-controls="pane-bypass" aria-selected="false">
      <i class="bi bi-arrow-left-right me-1"></i> Bypass Tahun
    </button>
  </li>
</ul>
<div class="tab-content pt-2" id="overrideTabsContent">
  <div class="tab-pane fade show active" id="pane-deliverable" role="tabpanel">
    <!-- EXISTING Tab1 markup (filter card :24-65 + #overrideTableContainer :67-72 + overrideModal :75-165) UNCHANGED -->
  </div>
  <div class="tab-pane fade" id="pane-bypass" role="tabpanel">
    <!-- NEW Tab2: pending panel (always) + filter cascade + worker table container + wizard modal + confirm modal -->
  </div>
</div>
```
> **ANTI-PATTERN (verified RESEARCH Pitfall 5):** `ManageAssessment.cshtml:97-103` uses HTMX (`hx-get`/`hx-trigger`) for tab loading. Override is pure vanilla fetch. Copy ONLY the `nav-tabs`/`tab-pane` markup from ManageAssessment — NOT the HTMX attributes. Tab2 lazy-load uses `shown.bs.tab` event + `fetch` (see JS below).

**Filter cascade markup pattern** (copy `Override.cshtml:24-65` verbatim into Tab2 with distinct ids, e.g. `bypassBagian`/`bypassUnit`/`bypassTrack`/`btnLoadBypass`):
```html
<!-- Source: Override.cshtml:26-62 — reuse for Tab2 worker filter (D-14) and wizard step-3 TargetUnit (D-11) -->
<label class="form-label fw-semibold">@OrgLabels.GetLabel(0)</label>
<select class="form-select" id="bypassBagian"><option value="">-- Pilih @OrgLabels.GetLabel(0) --</option></select>
<!-- Unit + Track dropdowns disabled until parent selected; btn disabled until track chosen -->
<button type="button" class="btn btn-primary w-100" id="btnLoadBypass" disabled>
  <i class="bi bi-search me-1"></i>Muat Data
</button>
```
> `@OrgLabels.GetLabel(0)`=Bagian, `GetLabel(1)`=Unit — dynamic labels, NEVER hardcode (verified `:29,35`).

**Wizard modal shell** (copy `overrideModal` structure `Override.cshtml:75-165` — `modal fade` + `modal-lg` + `modal-header`/`modal-body`/`modal-footer`; replace single form with 3 step `<div>`s toggled by JS + step indicator + Lanjut/Kembali/Jalankan Bypass footer).

**Empty-state placeholder pattern** (`Override.cshtml:67-72` — reuse for pre-filter worker table + 0-pending panel D-16):
```html
<div class="text-center py-5 text-muted">
  <i class="bi bi-arrow-left-right fs-1 mb-3 d-block opacity-50"></i>
  <p>Pilih Bagian, Unit, dan Track lalu klik Muat Data untuk menampilkan pekerja.</p>
</div>
```

**Tab2 JS IIFE — cascade filter pattern** (copy `Override.cshtml:170-219`):
```javascript
// Source: Override.cshtml:170, 191-215 [VERIFIED]
const orgStructure = @Html.Raw(ViewBag.SectionUnitsJson ?? "{}");  // already injected for Tab1 — reuse same const
// Bagian change → repopulate Unit from orgStructure[bagian], enable/disable downstream:
document.getElementById('bypassBagian').addEventListener('change', function () {
    var unitSelect = document.getElementById('bypassUnit');
    unitSelect.innerHTML = '<option value="">-- Pilih @OrgLabels.GetLabel(1) --</option>';
    if (this.value && orgStructure[this.value]) {
        orgStructure[this.value].forEach(function (u) {
            var opt = document.createElement('option'); opt.value = u; opt.textContent = u;
            unitSelect.appendChild(opt);
        });
    }
    unitSelect.disabled = !this.value;
    // reset Track + disable Muat Data button (mirror :204-206)
});
```
> NOTE: `orgStructure` is declared once at `:170` (outside the Tab1 IIFE). Tab2 IIFE can reference it directly — do NOT re-inject. Wizard step-3 TargetUnit cascade (D-11) reuses the SAME `orgStructure` constant.

**Tab2 JS IIFE — fetch GET + render pattern** (copy `Override.cshtml:226-252`):
```javascript
// Source: Override.cshtml:237-251 [VERIFIED] — appUrl() WAJIB (Dev sub-path /KPB-PortalHC)
async function loadBypassWorkers() {
    var container = document.getElementById('bypassTableContainer');
    container.innerHTML = '<div class="text-center py-4"><span class="spinner-border text-primary"></span></div>';
    try {
        var url = appUrl('/ProtonData/BypassList?bagian=' + encodeURIComponent(bagian)
            + '&unit=' + encodeURIComponent(unit) + '&trackId=' + encodeURIComponent(trackId));
        var resp = await fetch(url);
        var rows = await resp.json();   // BypassList returns array (NO {success} wrapper — see contract note)
        renderWorkerTable(rows);
    } catch (err) {
        container.innerHTML = '<div class="alert alert-danger">Terjadi kesalahan jaringan. Silakan coba lagi.</div>';
    }
}
```
> **CONTRACT DIFFERENCE (verified):** `OverrideList` (Tab1) returns `{ success, coachees, ... }`. `BypassList`/`BypassPendingList` (Tab2) return a **bare array** (`:1529,1558`) — no `.success` wrapper. Render directly from the array. `BypassDetail`/`BypassSave`/`BypassConfirm`/`BypassCancelPending` DO return `{ success, message, ... }`. Don't assume one shape for all.

**Tab2 JS IIFE — POST fetch + AntiForgery header pattern** (copy `Override.cshtml:402-433`):
```javascript
// Source: Override.cshtml:415-420 + _Layout.cshtml:54-55 [VERIFIED]
var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
var resp = await fetch(appUrl('/ProtonData/BypassSave'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
    body: JSON.stringify(payload)   // BypassSaveRequest shape — see Shared Patterns
});
var data = await resp.json();
if (data.success) { /* close modal, toast green, refresh table+panel (D-04) */ }
else { /* toast red data.message verbatim (D-20) */ }
```
> `BypassConfirm`/`BypassCancelPending` payload = `{ PendingId: <int> }` (verified `:92-95`). Same header/fetch shape.

**Spinner-on-button anti-double-click pattern** (copy `Override.cshtml:410-413, 432-433` — D-21):
```javascript
// Source: Override.cshtml:411-412, 432-433 [VERIFIED]
btn.disabled = true;
btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Menyimpan...';
// ... await fetch ...
btn.disabled = false;
btn.innerHTML = '<i class="bi bi-save me-1"></i>Jalankan Bypass';
```

**escHtml XSS-safe render helper** (copy `Override.cshtml:437-444` verbatim — WAJIB for ALL server data in innerHTML: nama, reason, track, coach):
```javascript
// Source: Override.cshtml:437-444 [VERIFIED] — Security: V5 XSS mitigation
function escHtml(str) {
    if (!str) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}
```

**Deep-link + programmatic tab activation pattern** (D-05/D-07/D-08 — from `ManageAssessment.cshtml:225` `bootstrap.Tab().show()`):
```javascript
// Programmatic tab show (deep-link):
new bootstrap.Tab(document.getElementById('tab-bypass')).show();
// Lazy-load Tab2 once on first activation (D-07):
document.getElementById('tab-bypass').addEventListener('shown.bs.tab', loadTab2Once);
// Update URL param on switch (D-08) — NO server round-trip:
history.replaceState(null, '', appUrl('/ProtonData/Override?tab=bypass'));
// On page load: read URLSearchParams; tab=bypass → show Tab2; pending={id} → auto-open confirm modal (D-05)
```

> **NEW HELPER REQUIRED (RESEARCH Pitfall 1 — verified):** There is NO global `showToast()` in the codebase. The toast at `_Layout.cshtml:289-291` lives INSIDE `if (document.body.dataset.impersonating === 'true')` (block opens `:282`-context) — it is NOT reusable. Planner MUST write a small `showToast(message, variant)` helper using the same markup shape (`toast align-items-center text-bg-{variant} border-0 show position-fixed top-0 end-0`), variants: `success` (green), `danger` (red, verbatim `data.message`), `warning`/`info` (yellow, stale deep-link D-06). Calling an undefined `showToast()` → silent `ReferenceError`.

---

### `Controllers/ProtonDataController.cs` (controller, request-response)

**Analog:** self — `Override()` GET (`:231-243`) + `BypassPendingList` (`:1534-1559`) + coach-list pattern from `CoachMappingController.cs:146-149`

**`Override()` GET — current ViewBag pattern to EXTEND** (`:231-243`):
```csharp
// Source: ProtonDataController.cs:231-243 [VERIFIED] — existing; add coach list (Pitfall 2)
public async Task<IActionResult> Override()
{
    var user = await _userManager.GetUserAsync(User);
    if (user == null) return Challenge();

    var tracks = await _context.ProtonTracks.OrderBy(t => t.Urutan).ToListAsync();
    ViewBag.AllTracks = tracks;

    var sectionUnitsDictOverride = await _context.GetSectionUnitsDictAsync();
    ViewBag.SectionUnitsJson = System.Text.Json.JsonSerializer.Serialize(sectionUnitsDictOverride);

    // ===== NEW (D-12, Pitfall 2 Opsi A — recommended) =====
    // var coachRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);
    // ViewBag.AllCoaches = coachRoleUsers.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();

    return View();
}
```

**Coach-list pattern to copy** (`CoachMappingController.cs:146-149` — verbatim, this is the proven idiom):
```csharp
// Source: CoachMappingController.cs:146-149 [VERIFIED] — D-12 coach dropdown source (Opsi A)
var coachRoleUsers = await _userManager.GetUsersInRoleAsync(UserRoles.Coach);
ViewBag.EligibleCoaches = coachRoleUsers
    .Where(u => u.IsActive)
    .OrderBy(u => u.FullName).ToList();
```

**`BypassPendingList` SELECT extend pattern (D-18)** — modify the anonymous projection at `:1545-1556`. EXISTING fields stay byte-for-byte (do NOT alter the field contract); ADD new fields + a LEFT JOIN for coach name:
```csharp
// Source: ProtonDataController.cs:1536-1556 [VERIFIED] — extend select, query-only, NO migration
var rows = await (from p in _context.PendingProtonBypasses
                  where p.Status == "Menunggu" || p.Status == "Siap"
                  join u in _context.Users on p.CoacheeId equals u.Id into uj from u in uj.DefaultIfEmpty()
                  join ts in _context.ProtonTracks on p.SourceProtonTrackId equals ts.Id
                  join tt in _context.ProtonTracks on p.TargetProtonTrackId equals tt.Id
                  join s in _context.AssessmentSessions on p.LinkedAssessmentSessionId equals s.Id into sj
                  from s in sj.DefaultIfEmpty()
                  // ===== NEW (D-18): LEFT JOIN Users for target coach name (TargetCoachId nullable) =====
                  // join c in _context.Users on p.TargetCoachId equals c.Id into cj from c in cj.DefaultIfEmpty()
                  orderby p.CreatedAt descending
                  select new
                  {
                      id = p.Id, coacheeId = p.CoacheeId,
                      nama = u != null ? (u.FullName ?? u.UserName ?? p.CoacheeId) : p.CoacheeId,
                      sourceTrack = ts.DisplayName, targetTrack = tt.DisplayName,
                      targetUnit = p.TargetUnit, status = p.Status,
                      hasilExam = s != null ? s.IsPassed : null,    // bool? — EXISTING, keep
                      createdAt = p.CreatedAt,
                      // ===== NEW (D-18) — column names VERIFIED real =====
                      skorExam = s != null ? s.Score : (int?)null,         // AssessmentSession.Score (int?)  :26
                      tanggalExam = s != null ? s.CompletedAt : null,      // AssessmentSession.CompletedAt    :39
                      reason = p.Reason,                                   // PendingProtonBypass.Reason       :243
                      targetCoachId = p.TargetCoachId,                     // string?                          :242
                      // targetCoachNama = c != null ? (c.FullName ?? c.UserName) : null
                  }).ToListAsync();
```
> **VERIFIED column names (RESEARCH Pitfall 4):** `AssessmentSession.Score` (int? `:26`), `AssessmentSession.IsPassed` (bool? `:38`), `AssessmentSession.CompletedAt` (DateTime? `:39`); `PendingProtonBypass.Reason` (`:243`), `TargetCoachId` (string? `:242`). Do NOT write `s.Skor` or `s.Completed` — they don't exist.
> **Existing s-join already null-safe:** `p.LinkedAssessmentSessionId` is non-nullable int (`:244`) but join uses `DefaultIfEmpty()` (`:1542-1543`) — `s` can be null, all `s != null ?` guards already in place. Only ADD the coach LEFT JOIN.

**Endpoints UI consumes (UNCHANGED — do not touch except `BypassPendingList`):**
- `BypassList` `:1501` GET → bare array `{coacheeId, nama, trackId, trackAktif, progressApproved, progressTotal, finalAda}`
- `BypassDetail` `:1563` GET → `{success, sourceTrackId, sourceTahun, sourceTahunKe, sourceComplete, sourceHasFinal, eligibleModes[]}` (eligibleModes logic `:1590-1593`)
- `BypassSave` `:1610` POST `[ValidateAntiForgeryToken]` → `{success, message, pendingId, showAttachPackageReminder}`
- `BypassConfirm` `:1653` / `BypassCancelPending` `:1671` POST `[ValidateAntiForgeryToken]` → `{success, message}`

---

### `tests/e2e/proton-bypass.spec.ts` (test, event-driven)

**Analog:** `tests/e2e/image-in-assessment.spec.ts` (serial UAT + DB snapshot/restore)

**Imports + serial config + snapshot lifecycle pattern** (`image-in-assessment.spec.ts:21-60`):
```typescript
// Source: tests/e2e/image-in-assessment.spec.ts:21-59 [VERIFIED]
import { test, expect } from '@playwright/test';
import * as db from '../helpers/dbSnapshot';
import { login } from '../helpers/auth';

let snapshotPath: string;
test.describe.configure({ mode: 'serial' });

test.describe('Phase 361 — bypass tahun UI (UAT end-to-end)', () => {
  test.beforeAll(async () => {
    // C:\Temp blocked by SQL service account — resolve default backup dir:
    const dir = (await db.queryString(
      "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(260))"
    )).replace(/[\\/]+$/, '').replace(/\\/g, '/');
    const ts = new Date().toISOString().replace(/[:.]/g, '-');
    snapshotPath = `${dir}/HcPortalDB_Dev-pre361-${ts}.bak`;
    await db.backup(snapshotPath);
    await db.execScript(/* absolute path to 361-bypass-fixtures.sql */);   // seed multi-state workers
  });

  test.afterAll(async () => {
    if (!snapshotPath) return;
    await db.restore(snapshotPath);   // restore on success OR failure (SEED_WORKFLOW)
  });
  // ... tests ...
});
```

**Login + deep-link nav pattern** (`auth.ts:4-11` + RESEARCH Code Examples):
```typescript
// Source: tests/helpers/auth.ts:4-11 + accounts.ts:2-3 [VERIFIED]
await login(page, 'hc');                              // hc = meylisa.tjiang@pertamina.com / 123456
await page.goto('/ProtonData/Override?tab=bypass');   // baseURL http://localhost:5277 (config)
await expect(page.locator('#pane-bypass')).toBeVisible();
// deep-link pending: goto ?tab=bypass&pending={id} → expect confirm modal auto-open (D-05)
```
> **Account roles available** (`accounts.ts`): `admin`, `hc`, `coachee`, `coach`, etc. — all pwd `123456`. Use `hc` (Authorize Roles="Admin,HC" `:97`). For D-24 re-grade trigger use `admin` editing nilai via `/AssessmentAdmin/EditPesertaAnswers`.
> **Ops note (CLAUDE.md + MEMORY 355):** app must run with `Authentication__UseActiveDirectory=false dotnet run` @5277 before spec (appsettings handoff AD=true).

---

### `.planning/seeds/361-bypass-fixtures.sql` (migration/fixture, batch)

**Analog:** `.planning/seeds/313-timer-fixtures.sql`

**WIPE-AND-INSERT idempotent + THROW guard + BEGIN TRAN pattern** (`313-timer-fixtures.sql:44-119`):
```sql
-- Source: 313-timer-fixtures.sql:44-119 [VERIFIED] — idempotent re-run safe
SET NOCOUNT ON;
SET XACT_ABORT ON;            -- runtime error → auto-rollback

-- Resolve referenced user + THROW guard (anti-pattern Phase 309 FK-NULL):
DECLARE @UserId NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = '<fixture-coachee>@pertamina.com');
IF @UserId IS NULL
BEGIN
    THROW 50001, 'User <fixture> tidak ditemukan — abort. Seed user dulu.', 1;
END;

-- Idempotent cleanup BEFORE BEGIN TRAN (FK-respecting order; title/marker prefix):
DELETE FROM PendingProtonBypasses WHERE Reason LIKE 'Phase 361 Bypass Fixture%';   -- example marker
-- ... cascade cleanup any AssessmentSessions / assignments created by fixture ...

BEGIN TRAN;                   -- explicit boundary (defense-in-depth atop XACT_ABORT)
-- INSERT multi-state worker fixtures (D-23):
--   - komplit (all deliverable Approved + final ada)  → CL-A eligible
--   - partial (some Approved, no final)               → CL-B(a)/(b) eligible
--   - punya final                                     → CL-B blocked (D-D tolak)
--   - exam in-progress (E5)                            → pending Menunggu state
COMMIT;
-- Final verification SELECT (post-COMMIT, read-only) — assert expected row counts
```
> **Seed marker:** Use a distinct, greppable marker on a fixture-owned column (313 used `Title LIKE 'Phase 313 Timer Fixture%'`). For bypass, mark via `Reason` or a synthetic name prefix so cleanup is surgical and never touches real data.
> **SEED_WORKFLOW (CLAUDE.md):** classification = temporary + local-only. Snapshot before, restore after (handled by spec `beforeAll`/`afterAll`). Append `docs/SEED_JOURNAL.md` entry active→cleaned (pattern `global.setup.ts:122-129`).
> **Run command:** `sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i .planning\seeds\361-bypass-fixtures.sql` (or via `db.execScript()` in spec). The `-b` flag makes THROW return non-zero exit.

---

## Shared Patterns

### AntiForgery fetch (cross-cutting — all 3 POST mutations)
**Source:** `Override.cshtml:415-419` + `_Layout.cshtml:54-55`
**Apply to:** `BypassSave`, `BypassConfirm`, `BypassCancelPending` calls in Tab2 JS
```javascript
var basePath = '@Url.Content("~/")'.replace(/\/$/, '');   // _Layout.cshtml:54 (global helper, already defined)
function appUrl(path){ return basePath + (path.startsWith('/')?path:'/'+path); }   // _Layout.cshtml:55
var token = document.querySelector('input[name="__RequestVerificationToken"]').value;
// fetch POST with header 'RequestVerificationToken': token
```
> `appUrl`/`basePath` are GLOBAL (defined in `_Layout.cshtml` head `:53-56`) — Tab2 JS uses them directly, NO redefinition. Backend enforces `[ValidateAntiForgeryToken]` on all 3 (`:1609,1652,1671`).

### BypassSaveRequest payload shape (the JSON Tab2 sends)
**Source:** `ProtonDataController.cs:80-90`
**Apply to:** wizard step-3 submit (`BypassSave`)
```csharp
public class BypassSaveRequest {
    public string CoacheeId;          // wajib (V5 :1616)
    public int SourceProtonTrackId;
    public int TargetProtonTrackId;
    public string TargetUnit;         // WAJIB (360 WR-02, V5 :1622) — cascading dropdown D-11
    public string? TargetCoachId;     // null = pertahankan coach (D-12)
    public string Reason;             // wajib (V5 :1618)
    public string Mode;               // "CL-A"|"CL-B(a)"|"CL-B(b)"|"CL-C" (whitelist :1624)
    public int? DurationMinutes;      // D-09: UI sends null (default murni) — wizard does NOT ask
}
// Confirm/Cancel payload: { PendingId: int }  (:92-95)
```

### Error message surfacing (verbatim toast, D-20)
**Source:** `Services/ProtonBypassService.cs`
**Apply to:** red toast on any `{success:false}` response
- Surface `data.message` verbatim — service already returns friendly Indonesian, NO `ex.Message` leak (D6). Examples: `"Worker sudah punya rencana bypass aktif..."` (`:259`), `"Kondisi rencana sudah berubah... Konfirmasi dibatalkan."` (`:505` stale D-11), `"Pending sudah diproses (klik ganda)."` (`:516` race D-12).
- Do NOT write your own copy for backend rejections.

### Status badge mapping (D-17 descriptive labels)
**Source:** `Override.cshtml:381-389` `statusToBadgeClass` (copy idiom)
**Apply to:** pending panel badges
- DB `Menunggu` → badge `bg-warning text-dark` label **"Menunggu Exam"**; DB `Siap` → badge `bg-success` label **"Siap Dikonfirmasi"**. DB values UNCHANGED — display label only.

---

## No Analog Found

None. All 4 files have a strong in-codebase analog (this is a UI-extension phase, not greenfield).

The only genuinely NEW UI structures (wizard 3-step state machine, 4 closure-mode cards, pending confirm modal) have no exact analog but reuse the Bootstrap primitives (`modal`, `card`, `badge`, step indicator) per `361-UI-SPEC.md`. They are assembled from existing primitives — not copied wholesale from one file. Planner builds these from the UI-SPEC Component Inventory (`361-UI-SPEC.md:194-209`) using the `overrideModal` shell (`:75-165`) as the modal skeleton.

| Structure | Role | Data Flow | Note |
|-----------|------|-----------|------|
| Wizard 3-step modal | view fragment | event-driven (JS state) | No analog; build from `overrideModal` shell + JS step toggling (D-01/D-02) |
| 4 closure-mode cards | view fragment | request-response (`BypassDetail.eligibleModes`) | No analog; Bootstrap `card` + `opacity-50` disabled (D-10) |
| `showToast()` helper | utility (JS) | — | MUST be written new (RESEARCH Pitfall 1 — no global toast exists) |

---

## Metadata

**Analog search scope:** `Views/ProtonData/`, `Views/Admin/`, `Controllers/`, `Models/`, `Services/`, `tests/e2e/`, `tests/helpers/`, `.planning/seeds/`, `Views/Shared/_Layout.cshtml`
**Files scanned (read in full or in part):** `Override.cshtml`, `ProtonDataController.cs` (`:225-274`, `:1499-1684`), `ManageAssessment.cshtml`, `CoachMappingController.cs`, `AssessmentSession.cs`, `ProtonModels.cs`, `_Layout.cshtml`, `313-timer-fixtures.sql`, `auth.ts`, `accounts.ts`, `dbSnapshot.ts`, `image-in-assessment.spec.ts`
**Pattern extraction date:** 2026-06-11
**Confidence:** HIGH — all excerpts verbatim from source; column names + endpoint shapes verified against RESEARCH findings.
