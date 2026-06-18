# Phase 389: CoachCoacheeMapping Redesign — Accordion Card per Coach (DSN-01/02/03) - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 2 (1 MODIFY + 1 CREATE)
**Analogs found:** 2 / 2 (both exact, in-repo)

> Scope guard: this phase is **view + test only**. No controller/endpoint/JS-contract analogs needed — all
> AJAX, modal IDs, and structural JS selectors are PARITY-LOCKED (D-12). The two files below are the ONLY
> writable surfaces. Every excerpt is verbatim from the live repo (read this session), with file:line refs.

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/Admin/CoachCoacheeMapping.cshtml` (MODIFY: toolbar L48-61 + `else`-block L228-347) | view (Razor MVC + inline JS) | request-response / server-render loop | **self** (idioms already in this file) + `Views/Admin/ManageWorkers.cshtml:251-254` (avatar) + `Views/Shared/_Guide/_AccordionItem.cshtml` (collapse-toggle a11y) | exact (multi-source in-repo idiom) |
| `tests/e2e/coachcoacheemapping-389.spec.ts` (CREATE) | test (Playwright e2e parity) | request-response (UI assertion) | `tests/e2e/coachworkload-388.spec.ts` (sibling milestone v32.1 parity spec) | exact (sibling, same phase family) |

---

## Pattern Assignments

### `Views/Admin/CoachCoacheeMapping.cshtml` (view, server-render loop)

This file is its **own best analog** — the card idiom, badge logic, and collapse already live here. The
redesign re-arranges existing markup, it does not invent. Pull excerpts from FOUR in-repo sources below.

#### Excerpt A — Avatar-initial circle (DSN-01 / D-03) — VERBATIM from `ManageWorkers.cshtml:251-254`

```cshtml
<div class="avatar-initial rounded-circle bg-primary text-white me-2 d-flex align-items-center justify-content-center fw-bold"
     style="width: 36px; height: 36px; font-size: 0.8rem;">
    @(user.FullName.Length > 0 ? user.FullName.Substring(0, 1).ToUpper() : "?")
</div>
```
**Adapt for 389:** swap `user.FullName` → `((string)group.CoachName)` with the cast/fallback form from
RESEARCH §Technique:
`@( ((string)group.CoachName).Length > 0 ? ((string)group.CoachName).Substring(0,1).ToUpper() : "?" )`.
Keep `bg-primary` NEUTRAL (NOT load/section-tinted — D-03; the badge already carries threshold color).
Add `aria-hidden="true"` (decorative — coach name is adjacent visible text).

#### Excerpt B — Card `border-0 shadow-sm` + `card-header` + `card-body p-0` — VERBATIM from THIS file, import-results card L132-136

```cshtml
<div class="card border-0 shadow-sm mb-4">
    <div class="card-header border-0">
        <h6 class="mb-0 fw-bold">Hasil Import — @importResults.Count baris diproses</h6>
    </div>
    <div class="card-body p-0">
        <div class="table-responsive" style="max-height: 400px; overflow-y: auto;">
            <table class="table table-sm align-middle mb-0">
```
**Adapt for 389:** the coach card uses `mb-3` (D-01 inter-card rhythm) not `mb-4`; `card-body p-0` +
`table-responsive` + `table align-middle mb-0` is the exact mini-table wrapper for D-06 (drop the
`max-height/overflow-y` — that was import-only). This is the app's canonical "card wrapping a flush table".

#### Excerpt C — Card-header-as-collapse-toggle a11y idiom — VERBATIM from `_Guide/_AccordionItem.cshtml:28-35`

```cshtml
<button class="accordion-button collapsed guide-list-btn @btnVariant"
        type="button" data-bs-toggle="collapse"
        data-bs-target="#@collapseId" aria-expanded="false">
    @Model.Title
    ...
</button>
<div id="@collapseId" class="accordion-collapse collapse" data-bs-parent="">
```
**Why this analog:** it is the in-repo proof that a `<button ... data-bs-toggle="collapse" aria-expanded="false">`
trigger paired with `<div id="..." class="collapse">` (note `data-bs-parent=""` = empty → NOT single-open)
is the established idiom. RESEARCH Open-Question #1 recommends `<button type="button" class="card-header
w-100 text-start border-0 bg-white d-flex ...">` for native Enter+Space focus — this analog confirms a real
`<button>` toggle is idiomatic here. (Alternative `<div role="button" tabindex="0">` is also allowed by
UI-SPEC; assert keyboard via Playwright either way — Phase 354 lesson.)
**For 389:** id pair moves from the OLD `<tr class="table-primary">` (L250) to the card-header; body is
`class="collapse"` **WITHOUT `show`** (D-05, default closed) and **WITHOUT `data-bs-parent`** (independent —
contrast `PlanIdp.cshtml:304` which DOES use `data-bs-parent` for single-open; that is the anti-pattern here).

#### Excerpt D — Current collapse trigger + badge threshold (DSN-01/02 / D-04, D-14) — VERBATIM from THIS file L250-264 (the block being MOVED)

```cshtml
<tr class="table-primary" style="cursor:pointer;" data-bs-toggle="collapse" data-bs-target="#collapse-@idx">
    <td colspan="8" class="fw-semibold">
        <i class="bi bi-chevron-down me-2 small"></i>
        @group.CoachName
        @if (!string.IsNullOrEmpty((string)group.CoachSection))
        {
            <span class="text-muted fw-normal ms-1">— @group.CoachSection</span>
        }
    </td>
    <td class="text-center">
        @{
            int activeCount = (int)group.ActiveCount;
            string badgeClass = activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark";
        }
        <span class="badge @badgeClass">@activeCount</span>
    </td>
    <td></td>
</tr>
```
**PARITY-CRITICAL (D-04):** copy the `activeCount >= 8 ? "bg-danger" : activeCount >= 5 ? "bg-warning text-dark" : "bg-info text-dark"`
ternary **verbatim** — hardcoded 5/8, do NOT link to any configurable CoachWorkload threshold.
**Move plan (D-14, H-12/H-13/H-15):** lift `data-bs-toggle="collapse" data-bs-target="#collapse-@idx"`,
`@group.CoachName`, the section span, the badge `@{...}` block, and `style="cursor:pointer"` into the new
`card-header`; the chevron `<i class="bi bi-chevron-down">` stays (rotates via scoped CSS, D-05). Keep
`var idx = 0;` (L245) + `idx++;` (L345) so each `#collapse-@idx` is unique.

#### Excerpt E — Coachee mini-row + Aksi block (DSN-02 / D-06/07/08/09) — VERBATIM from THIS file L273-342 (the block being REWRITTEN into the mini-table)

```cshtml
<tr class="@(coachee.IsActive ? "" : "table-light text-muted")" data-mapping-id="@coachee.Id">
    <td>@coachee.CoacheeName</td>
    <td>@coachee.CoacheeNIP</td>
    <td>@(string.IsNullOrEmpty((string)coachee.AssignmentSection) ? "—" : coachee.AssignmentSection)</td>
    <td>@(string.IsNullOrEmpty((string)coachee.AssignmentUnit) ? "—" : coachee.AssignmentUnit)</td>
    <td>@coachee.CoacheePosition</td>
    <td>
        @if (!string.IsNullOrEmpty((string)coachee.ProtonTrack))
        { <span class="badge bg-info text-dark">@coachee.ProtonTrack</span> }
        else { <span class="text-muted">—</span> }
    </td>
    <td>
        @if (coachee.IsActive) { <span class="badge bg-success">Aktif</span> }
        else { <span class="badge bg-secondary">Non-aktif</span> }
    </td>
    <td>@coachee.StartDate.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("id-ID"))</td>
    <td></td>   @* ← OLD "Coachee Aktif" empty cell — DROP THIS (D-07) along with its <th> at L241 *@
    <td>
        <div class="d-flex gap-1">
            <button class="btn btn-sm btn-outline-secondary"
                    onclick="openEditModal(@coachee.Id, '@coachee.CoachId', 0, '@coachee.StartDate.ToString("yyyy-MM-dd")', '@coachee.CoacheeName', '@coachee.AssignmentSection', '@coachee.AssignmentUnit')">
                <i class="bi bi-pencil"></i> Edit
            </button>
            @* D-06 (Phase 356): cek IsCompleted DULU. *@
            @if (coachee.IsCompleted)
            {
                <span class="badge bg-info"><i class="bi bi-award me-1"></i>Graduated</span>
            }
            else if (coachee.IsActive)
            {
                <button class="btn btn-sm btn-outline-danger"
                        onclick="confirmDeactivate(@coachee.Id, '@coachee.CoacheeName')">
                    <i class="bi bi-x-circle"></i> Nonaktifkan
                </button>
                <form method="post" asp-action="MarkMappingCompleted" asp-controller="CoachMapping" class="d-inline">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="mappingId" value="@coachee.Id" />
                    <button type="submit" class="btn btn-sm btn-outline-info"
                            onclick="return confirm('Tandai coachee ini sebagai graduated?')">
                        <i class="bi bi-award"></i> Graduated
                    </button>
                </form>
            }
            else
            {
                <button class="btn btn-sm btn-outline-success"
                        onclick="reactivateMapping(@coachee.Id)">
                    <i class="bi bi-arrow-repeat"></i> Aktifkan
                </button>
                <button class="btn btn-sm btn-outline-danger"
                        onclick="confirmDelete(@coachee.Id, '@coachee.CoacheeName')">
                    <i class="bi bi-trash me-1"></i> Hapus
                </button>
            }
        </div>
    </td>
</tr>
```
**PARITY-CRITICAL — copy ALMOST verbatim, change exactly TWO things:**
1. **Drop** the empty `<td></td>` (OLD L300) AND its `<th class="text-center">Coachee Aktif</th>` (OLD L241) — both halves together (D-07; Pitfall 5: don't half-remove → 9 columns net).
2. Keep `data-mapping-id="@coachee.Id"` on the `<tr>` (H-1 — `submitDelete()` does `tr[data-mapping-id].remove()` at L973-974). The row MUST stay a `<tr>` inside a real `<table>` (Pitfall 1).
**Everything else is FROZEN:** the `openEditModal(...)` 7-arg `onclick` (H-2, escaping as-is — Pitfall 4),
the `if IsCompleted → else if IsActive → else` order (H-7 / Phase 356 D-06), the `MarkMappingCompleted`
form + `@Html.AntiForgeryToken()` (H-4), `confirmDeactivate`/`reactivateMapping`/`confirmDelete` calls
(H-3/H-5/H-6), the `table-light text-muted` non-active class (H-8), `@OrgLabels.GetLabel(0/1) Penugasan`
headers (Pitfall 6), and the `dd MMM yyyy` / `id-ID` culture format.

#### Excerpt F — Toolbar (DSN-03 / D-10, D-11) — VERBATIM from THIS file L48-61 (being NORMALIZED)

```cshtml
<div class="d-flex gap-2">
    <a href="@Url.Action("DownloadMappingImportTemplate", "CoachMapping")" class="btn btn-sm btn-outline-success">
        <i class="bi bi-download me-1"></i>Download Template
    </a>
    <button class="btn btn-sm btn-outline-primary" data-bs-toggle="modal" data-bs-target="#importMappingModal">
        <i class="bi bi-file-earmark-arrow-up me-1"></i>Import Excel
    </button>
    <a asp-controller="CoachMapping" asp-action="CoachCoacheeMappingExport" class="btn btn-sm btn-outline-success">
        <i class="bi bi-file-earmark-excel me-1"></i>Export Excel
    </a>
    <button class="btn btn-sm btn-primary" onclick="document.getElementById('assignModal').querySelector('[data-bs-dismiss]') && null" data-bs-toggle="modal" data-bs-target="#assignModal">
        <i class="bi bi-plus-circle me-1"></i>Tambah Mapping
    </button>
</div>
```
**Changes (D-10/D-11):**
- Wrap the 3 Excel actions (Download Template / Import Excel / Export Excel) in a `<div class="btn-group">`
  with a single consistent outline color (RESEARCH Open-Q #2 recommends `btn-outline-secondary` so the CTA is
  the only accent — Claude's discretion). All `btn-sm`.
- Add `flex-wrap` to the container (`d-flex gap-2 flex-wrap`) for narrow-screen wrap.
- **DELETE** the dead `onclick="document.getElementById('assignModal').querySelector('[data-bs-dismiss]') && null"`
  on "Tambah Mapping" (D-11 / Pitfall 7) — **keep** `data-bs-toggle="modal" data-bs-target="#assignModal"`
  (H-9). Keep "Tambah Mapping" as solo `btn-primary btn-sm`. Keep Import's `data-bs-target="#importMappingModal"`
  (H-10) and both `asp-action` tag-helpers (H-11).

#### Excerpt G — Chevron rotation scoped style (DSN-02 / D-05, Claude's discretion) — render into `@section Styles` (slot confirmed `_Layout.cshtml:44`)

```css
[data-bs-toggle="collapse"] .chevron-toggle { transition: transform .2s ease; }
[data-bs-toggle="collapse"]:not(.collapsed) .chevron-toggle { transform: rotate(180deg); }
```
Add `@section Styles { <style> ... </style> }` (NO shared CSS file for one page — code_context constraint).
Bootstrap toggles `.collapsed` on the trigger; assert the actual rotation at runtime (Assumption A1).

---

### `tests/e2e/coachcoacheemapping-389.spec.ts` (test, e2e parity)

**Analog:** `tests/e2e/coachworkload-388.spec.ts` (sibling milestone v32.1 polish parity spec — same login,
same data-skip discipline, same `--workers=1` run note).

#### Excerpt H — `loginAny` helper + describe/beforeEach scaffold — VERBATIM from `coachworkload-388.spec.ts:17-37`

```typescript
import { test, expect, Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';

async function loginAny(page: Page, accountKey: AccountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}

test.describe('Phase 389 — CoachCoacheeMapping accordion parity (DSN-01/02/03)', () => {
  test.beforeEach(async ({ page }) => {
    await loginAny(page, 'admin');
    await page.goto('/Admin/CoachCoacheeMapping');
    await expect(page.locator('h2', { hasText: 'Coach-Coachee Mapping' })).toBeVisible();
  });
  // ... tests V-01..V-14 ...
});
```
**Adapt:** swap route `/Admin/CoachWorkload` → `/Admin/CoachCoacheeMapping`; swap the H2 text guard
("Coach Workload" → the actual page H2). `accounts.admin` = `admin@pertamina.com / 123456` (VERIFIED
`accounts.ts:2`). **Run note** (copy header comment from 388): app does NOT auto-start (no `webServer` in
`playwright.config.ts`) → `dotnet run` with `Authentication__UseActiveDirectory=false` first, then
`cd tests; npx playwright test coachcoacheemapping-389 --workers=1` (`--workers=1` WAJIB — NTLM/shared-mem SQL).

#### Excerpt I — Data-guard `test.skip` pattern — VERBATIM from `coachworkload-388.spec.ts:70-73`

```typescript
const items = page.locator('.list-group-item.suggestion-card');
const n = await items.count();
test.skip(n === 0, 'no suggestion data (no overload) — parity approve/skip dikunci UAT/Phase 390');
```
**Adapt:** use this idiom for any mutation/branch assertion that needs specific data (e.g. delete-row,
multi-card independence needing ≥2 coaches). For 389: `test.skip(cardCount < 2, 'need ≥2 coach groups for
independent multi-open')`; `test.skip(noDisposableData, 'mutasi penuh = Phase 390')`. Mirror RESEARCH
§Validation note: in Phase 389 this is **smoke parity** (hooks present + collapse/modal open); full mutation
parity is Phase 390.

#### Excerpt J — Filter submit + URL-wait parity — VERBATIM from `coachworkload-388.spec.ts:103-122`

```typescript
const select = page.locator('select[name="section"]');
const optionValues = await select.locator('option').evaluateAll(
  (opts) => opts.map((o) => (o as HTMLOptionElement).value).filter((v) => v !== ''),
);
test.skip(optionValues.length === 0, 'no section options to filter');

await select.selectOption(optionValues[0]);
await Promise.all([
  page.waitForURL(/section=/, { timeout: 15_000 }),
  page.getByRole('button', { name: 'Filter' }).click(),
]);
expect(page.url()).toContain('section=');
```
**Adapt:** matches RESEARCH Test-Plan #14 (filter Seksi → `resetPageAndSubmit` → URL `section=`). Adjust
the submit-button name to this page's "Cari" if different. Use the same `Promise.all([waitForURL, click])`
race for any navigation assertion.

**New assertions to author (no excerpt — derive from RESEARCH §Concrete Test Plan 1-13):** card count ==
coach groups + `.avatar-initial` present (V card render); badge class↔threshold via `evaluate`;
`.collapse.show` count == 0 on load + each header `aria-expanded="false"` (default closed); click header →
`#collapse-0` visible + `aria-expanded="true"`; open card0 AND card1 → both `.show` (independent, no
`data-bs-parent`); open card `thead th` count == 9 + no "Coachee Aktif" th; keyboard `Enter`/`Space` toggle
+ `role="button"`-or-`BUTTON` + `aria-controls`; Edit → `#editModal` visible + `#editCoacheeName` set; Hapus
→ `#deleteModal` open (full row-removal = Phase 390); "Tambah Mapping" is `.btn-primary`, Excel trio in
`.btn-group` all `.btn-sm`, its `onclick` attr null/absent; "Tambah Mapping" click → `#assignModal` visible;
`page.route('**/Admin/CoachCoacheeMappingDeletePreview*')` → confirmDelete → assert path hit (appUrl sub-path).

---

## Shared Patterns

### PathBase-aware AJAX (`appUrl`) — DO NOT TOUCH, verify at runtime
**Source:** `Views/Shared/_Layout.cshtml:54-55`
**Apply to:** all existing `@section Scripts` fetch calls (FROZEN — D-12); the test asserts the prefix.
```javascript
var basePath = '@Url.Content("~/")'.replace(/\/$/, '');
function appUrl(path) { return basePath + (path.startsWith('/') ? path : '/' + path); }
```
Markup rewrite cannot break this (it's a layout helper); never hardcode `/Admin/...` paths in new markup.
The Graduated form is the only non-AJAX action (`asp-action="MarkMappingCompleted"` POST page-reload, H-4).

### `@section Styles` slot — for the chevron-rotation CSS only
**Source:** `Views/Shared/_Layout.cshtml:44` → `@await RenderSectionAsync("Styles", required: false)`
**Apply to:** Excerpt G scoped `<style>` (chevron). No shared `.css` file for a single page.

### Bootstrap collapse toggle (declarative, 0 custom JS)
**Source:** `Views/Shared/_Guide/_AccordionItem.cshtml:28-35` (real `<button>` trigger, empty `data-bs-parent`)
+ this file's own current `<tr ... data-bs-toggle="collapse">` (L250).
**Apply to:** the card-header. Independent cards = NO `data-bs-parent` (contrast `PlanIdp.cshtml:304-317`
which uses `data-bs-parent` deliberately for single-open — that is the explicit anti-pattern for 389, D-05).

---

## No Analog Found

None. Every required pattern has a verbatim in-repo source (this file, `ManageWorkers`, `_AccordionItem`,
`_Layout`, sibling `coachworkload-388.spec.ts`). RESEARCH §Code Examples already supplies the assembled
target markup; the planner/executor should prefer these real analogs over re-deriving.

## Metadata

**Analog search scope:** `Views/Admin/` (target + ManageWorkers), `Views/Shared/_Guide/` + `Views/CDP/`
(accordion a11y idiom), `Views/Shared/_Layout.cshtml` (appUrl + Styles slot), `tests/e2e/` + `tests/helpers/`
(spec template + credentials).
**Files scanned:** 6 (CoachCoacheeMapping.cshtml, ManageWorkers.cshtml, _AccordionItem.cshtml, PlanIdp.cshtml,
_Layout.cshtml, coachworkload-388.spec.ts, accounts.ts).
**Pattern extraction date:** 2026-06-17
