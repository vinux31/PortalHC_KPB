# Phase 392: Perbaikan CreateWorker + Audit Field - Pattern Map

**Mapped:** 2026-06-17
**Files analyzed:** 2 (1 MODIFY view, 1 CREATE e2e spec)
**Analogs found:** 2 / 2 (both exact in-repo analogs, all excerpts verified against live files this session)

> VIEW-ONLY phase. The only production file changed is `Views/Admin/CreateWorker.cshtml`. The only new file is `tests/e2e/createworker-392.spec.ts`. Controller/model are FROZEN (git 0-diff). Highest-value output: (a) the exact `Settings.cshtml` `@section Scripts` block, (b) one verbatim existing validation span from `CreateWorker.cshtml`, (c) the closest existing Playwright spec with its login + teardown excerpts.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/Admin/CreateWorker.cshtml` (MODIFY) | view (Razor MVC form) | request-response (form GET render / POST submit) | self (existing spans + info-text in same file) + `Views/Account/Settings.cshtml` (for `@section Scripts`) | exact (in-file + sibling view) |
| `tests/e2e/createworker-392.spec.ts` (CREATE) | test (Playwright e2e) | request-response + transform (form-fill → submit → DB assert → teardown) | `tests/e2e/assessment-title-flexible.spec.ts` (login + navigate + CreateWorker-sibling form) + `tests/e2e/delete-records-cascade.spec.ts` (serial + afterAll DB teardown) | exact (role + data flow) |

---

## Pattern Assignments

### `Views/Admin/CreateWorker.cshtml` (view, request-response) — MODIFY

This view is its own best analog for spans/info-text (copy verbatim from neighbouring fields). For the `@section Scripts` wrapper, the analog is a sibling view (`Settings.cshtml`).

#### Analog 1 (in-file): validation span — copy verbatim for the 4 new org spans (D-04)

**Source:** `Views/Admin/CreateWorker.cshtml:69` (FullName span — identical at L80 Email, L85 NIP, L90 JoinDate).

```html
<span asp-validation-for="FullName" class="text-danger small"></span>
```

The 4 new org spans MUST match this exactly — same class `text-danger small`, no spacing/margin class, placed immediately after the input/select inside its `col-md-6`:

```html
<span asp-validation-for="Position" class="text-danger small"></span>     <!-- after <input> L107 -->
<span asp-validation-for="Directorate" class="text-danger small"></span>  <!-- after <input> L111 -->
<span asp-validation-for="Section" class="text-danger small"></span>      <!-- after </select> L117 -->
<span asp-validation-for="Unit" class="text-danger small"></span>         <!-- after </select> L123 -->
```

Role span (optional, Claude's Discretion — recommended for consistency):
```html
<span asp-validation-for="Role" class="text-danger small"></span>         <!-- after the Role <div class="form-text"> ~L146 -->
```

**CRITICAL (F-NEW-01 HIGH):** Grep `asp-validation-for` in the file BEFORE inserting. The 6 spans that ALREADY exist — DO NOT duplicate: FullName (L69), Email (L80), NIP (L85), JoinDate (L90), Password (L158), ConfirmPassword (L168). Only the 4 org fields (Position/Directorate/Section/Unit) lack a span. Adding any duplicate renders two error messages.

#### Analog 2 (in-file): the FullName/Email input + AD info-text — its own analog for the reword (D-01 + D-02 + D-03)

**Source (CURRENT, verified live L61-80):**
```html
<label asp-for="FullName" class="form-label fw-semibold"></label>
<input asp-for="FullName" class="form-control @(isAdMode ? "bg-light" : "")"
       placeholder="Masukkan nama lengkap"
       readonly="@(isAdMode ? "readonly" : null)" />
@if (isAdMode)
{
    <div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>Dikelola oleh AD — akan disinkronkan saat login</div>
}
<span asp-validation-for="FullName" class="text-danger small"></span>
...
<input asp-for="Email" class="form-control @(isAdMode ? "bg-light" : "")"
       placeholder="contoh@pertamina.com"
       readonly="@(isAdMode ? "readonly" : null)" />
@if (isAdMode)
{
    <div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>Dikelola oleh AD — akan disinkronkan saat login</div>
}
<span asp-validation-for="Email" class="text-danger small"></span>
```

**Transform to apply (matching the unchanged neighbour NIP at L84 — plain `class="form-control"`, no ternary, no `readonly`):**
- Remove `@(isAdMode ? "bg-light" : "")` from the `class` (both FullName L62 and Email L73) → plain `class="form-control"`.
- Remove `readonly="@(isAdMode ? "readonly" : null)"` entirely (both L64 and L75).
- Add `type="email"` explicitly to the Email input (L73): `<input asp-for="Email" type="email" class="form-control" ... />`. Verified valid — model uses `[EmailAddress]` not `[DataType(DataType.EmailAddress)]`, so `asp-for` does NOT auto-render `type=email`; the input currently has NO `type` attr → renders `type="text"`. Explicit `type="email"` is meaningful and produces NO duplicate (explicit attribute wins over TagHelper).
- Reword the `@if(isAdMode)` info-text in BOTH blocks (keep the `form-text text-info` + `bi-info-circle me-1` treatment unchanged — only the text changes). LOCKED copy (UI-SPEC Copywriting / D-02), use `&amp;` for the ampersand in markup:
  > Isi sesuai akun AD Pertamina pekerja. Nama &amp; Email akan diselaraskan otomatis dari Active Directory saat pekerja login pertama kali.

The `bi-info-circle me-1` + `form-text text-info` wrapper is its own in-file analog — reuse it verbatim, swap only the inner text string.

#### Analog 3 (sibling view): `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") ... }` (D-05)

**Source:** `Views/Account/Settings.cshtml:137-146` — VERBATIM, read live this session:
```cshtml
@section Scripts {
    @await Html.PartialAsync("_ValidationScriptsPartial")
    <script>
        $(document).ready(function() {
            setTimeout(function() {
                $('.alert').fadeOut('slow');
            }, 5000);
        });
    </script>
}
```

This is the exact wrapper pattern to copy. For CreateWorker, the body of the `<script>` differs: take the EXISTING inline script block (`CreateWorker.cshtml:194-205` — `shared-cascade.js` + `shared-loading.js` + `initSectionUnitCascade(...)` + `initFormLoading(...)`) and MOVE it inside the `@section Scripts` right after the partial. Resulting structure:

```cshtml
@section Scripts {
    @await Html.PartialAsync("_ValidationScriptsPartial")
    <script src="~/js/shared-cascade.js"></script>
    <script src="~/js/shared-loading.js"></script>
    <script>
        initSectionUnitCascade({
            sectionUnits: @Html.Raw(ViewBag.SectionUnitsJson ?? "{}"),
            sectionId: 'sectionSelect',
            unitId: 'unitSelect',
            currentSection: "@(Model.Section ?? "")",
            currentUnit: "@(Model.Unit ?? "")"
        });
        initFormLoading('createWorkerForm', 'Menyimpan...');
    </script>
}
```

**CAUTION (F-NEW-10):** The partial MUST sit inside `@section Scripts`, NOT inline in the body. `_Layout.cshtml` loads jQuery at L241 then renders `@RenderSectionAsync("Scripts")` at L267 — so a section-mounted partial loads AFTER jQuery. An inline-in-body partial would load before jQuery → `$ is not defined`. Keep `currentSection`/`currentUnit` Razor interpolation intact so POST-reload restores selections. Convention: place `@section Scripts` at the end of the file.

`_ValidationScriptsPartial.cshtml` content (2 lines, both lib paths verified present in `wwwroot/lib/`):
```html
<script src="~/lib/jquery-validation/dist/jquery.validate.min.js"></script>
<script src="~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js"></script>
```

---

### `tests/e2e/createworker-392.spec.ts` (test, request-response + transform) — CREATE

Two analogs combine: one for the login + navigate + form-on-CreateWorker shape, one for the serial + `afterAll` DB teardown shape.

#### Analog A (primary): `tests/e2e/assessment-title-flexible.spec.ts` — login + navigate + same admin form area

This is the closest spec by data flow: it logs in as admin via the accounts fixture and drives `/Admin/CreateAssessment` (a sibling admin form). Copy its login helper verbatim (inline `loginAny`/`loginAdmin` with the `Promise.all` + `waitForURL`-off-login pattern — preferred over `helpers/auth.ts login()` because `auth.ts` hard-waits `**/Home/**` which an admin landing on a different page would miss).

**Login helper (verbatim, lines 6-18):**
```ts
import { test, expect, type Page } from '@playwright/test';
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
```

**Navigate + form-fill pattern (verbatim shape, lines 21-34):**
```ts
test.beforeEach(async ({ page }) => {
  await loginAny(page, 'admin');
  await page.goto('/Admin/CreateAssessment');
  await page.locator('#Title').waitFor({ state: 'visible', timeout: 15_000 });
});

test('...', async ({ page }) => {
  const unique = 'ZZ Judul Unik ' + Date.now();  // unique-per-run pattern (reuse for unique email)
  await page.fill('#Title', unique);
  await page.click('#btnCheckTitle');
  // expect(...) assertions
});
```

For CreateWorker, swap `/Admin/CreateAssessment` → `/Admin/CreateWorker` and use the verified field selectors (`asp-for` → `id` = property name): `#FullName`, `#Email`, `#sectionSelect`, `#unitSelect`, `#passwordField`, `#confirmPasswordField`.

**Account fixture (`tests/helpers/accounts.ts`):** `accounts.admin = { email: 'admin@pertamina.com', password: '123456', role: 'Admin' }`. Use this for `/Admin/*`.

#### Analog B: `tests/e2e/delete-records-cascade.spec.ts` — serial mode + `afterAll` DB teardown + dbSnapshot helper

Copy its serial-mode declaration, its `db.queryString` worker-id resolution, and its `afterAll` teardown shape. Note: this analog uses full BACKUP/RESTORE because its seed is complex multi-table. **D-07 deliberately chose the lighter unique-email + DeleteWorker path** — so copy the STRUCTURE (serial + `afterAll` always-runs + `db.queryString` to resolve the worker Id), NOT the backup/restore.

**Serial mode + DB-resolve worker id (verbatim shapes, lines 30, 42):**
```ts
import * as db from '../helpers/dbSnapshot';

test.describe.configure({ mode: 'serial' });

// resolve worker Id by email (reuse for teardown target)
workerId = await db.queryString(`SELECT TOP 1 Id FROM Users WHERE Email='${WORKER_EMAIL}'`);
```

**`afterAll` always-runs teardown shape (lines 72-75):**
```ts
test.afterAll(async () => {
  if (!snapshotPath) return;   // delete-records uses restore; CreateWorker swaps this for DeleteWorker POST
  await db.restore(snapshotPath);
});
```

**`dbSnapshot` helper API available (`tests/helpers/dbSnapshot.ts`):**
- `db.queryString(sql): Promise<string>` — single string scalar (resolve `Id` by `Email`, assert row exists). Note table alias `Users` works in these queries (used live in delete-records L42).
- `db.queryScalar(sql): Promise<number>` — single numeric scalar (e.g. `COUNT(*)` assertions).
- `db.backup(path)` / `db.restore(path)` — full BACKUP/RESTORE (NOT used for D-07; lighter path chosen).
- Connection is hard-coded to `localhost\SQLEXPRESS` / `HcPortalDB_Dev`, localhost-only guard, Integrated Security.

**Teardown mechanism for D-07 (planner to finalize — important nuance):** The DeleteWorker UI button (`#delete-@user.Id` form, POST `DeleteWorker`, anti-forgery + hidden `id`, verified `ManageWorkers.cshtml:293-300`) only renders in the **inactive-worker `else` branch**. A freshly-created worker is **active** → its row shows the *deactivate* form (`if` branch L278-281), NOT the delete form. So a single-step "click `#delete-{id}`" will not find the button on a fresh active worker. Recommended teardown options for the planner:
- **Option A (deactivate → delete):** submit `ReactivateWorker`/`DeactivateWorker` to flip the worker inactive so the `#delete-{id}` form renders, then submit it. More UI steps.
- **Option B (request-context POST, recommended for robustness):** POST to `/Admin/DeleteWorker` via Playwright `page.request`/an authenticated APIRequestContext with the anti-forgery token (Identity `_userManager.DeleteAsync` cascades `AspNetUserRoles` — verified `WorkerController.cs:656`). Avoids the active/inactive UI gating entirely.
- **DO NOT** raw-SQL `DELETE FROM AspNetUsers` — skips Identity cascade → orphan `AspNetUserRoles` rows (F-NEW-07).

Either way: teardown MUST live in `test.afterAll`/`finally` (runs even on test failure), and the email is unique-per-run (`e2e-cw-${Date.now()}@local.test`) so re-runs never collide with "Email sudah terdaftar". Log one `docs/SEED_JOURNAL.md` entry (active → cleaned).

---

## Shared Patterns

### Client-side validation activation (`@section Scripts` + `_ValidationScriptsPartial`)
**Source:** `Views/Account/Settings.cshtml:137-146` (wrapper) + `Views/Shared/_ValidationScriptsPartial.cshtml` (lib includes) + `Views/Shared/_Layout.cshtml:241` jQuery / L267 `@RenderSectionAsync("Scripts")` (ordering proof).
**Apply to:** `CreateWorker.cshtml` (this phase). Standard ASP.NET Core MVC pattern (same as `dotnet new mvc`). The partial MUST be section-mounted, never inline-in-body.

### Inline per-field validation span
**Source:** `Views/Admin/CreateWorker.cshtml:69` (and L80/85/90/158/168).
**Apply to:** the 4 new org spans (+ optional Role span). Class is always exactly `text-danger small`, no spacing class, immediately after the input/select.
**Anti-pattern:** never add a second span to a field that already has one (grep first).

### AD info-text treatment
**Source:** `Views/Admin/CreateWorker.cshtml:67` (`<div class="form-text text-info"><i class="bi bi-info-circle me-1"></i>...`).
**Apply to:** the reworded FullName + Email info-text (identical copy both fields). Reuse wrapper verbatim, swap inner text only.

### Playwright admin login (accounts fixture, login-redirect wait)
**Source:** `tests/e2e/assessment-title-flexible.spec.ts:9-18` (inline helper) + `tests/helpers/accounts.ts` (`accounts.admin`).
**Apply to:** `createworker-392.spec.ts`. Prefer the inline `Promise.all([waitForURL(off-login), click])` over `helpers/auth.ts login()` (which hard-waits `**/Home/**`).

### Self-cleaning DB teardown (serial + afterAll + dbSnapshot)
**Source:** `tests/e2e/delete-records-cascade.spec.ts:30,42,72-75` (structure) + `tests/helpers/dbSnapshot.ts` (`queryString`/`queryScalar`).
**Apply to:** `createworker-392.spec.ts` — copy the serial + afterAll + `db.queryString` structure; swap RESTORE for DeleteWorker POST (D-07).

### Combined-run isolation
**Source:** `[[reference_local_e2e_sql_env_fix]]` + every existing spec header.
**Apply to:** run command `cd tests && npx playwright test e2e/createworker-392.spec.ts --workers=1`. App must run with `Authentication__UseActiveDirectory=false`. Start SQLBrowser before run.

---

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| — | — | — | None. Both files have exact in-repo analogs. The static guards (source-grep that `readonly=`/`bg-light` ternary are absent from FullName/Email; `git diff --quiet Controllers/WorkerController.cs Models/ManageUserViewModel.cs`) are shell/CI checks, not files needing a code analog. |

---

## Metadata

**Analog search scope:** `Views/Admin/`, `Views/Account/`, `Views/Shared/`, `tests/e2e/`, `tests/helpers/`.
**Files scanned (read live this session):** `Views/Admin/CreateWorker.cshtml`, `Views/Account/Settings.cshtml`, `Views/Admin/ManageWorkers.cshtml`, `tests/e2e/assessment-title-flexible.spec.ts`, `tests/e2e/delete-records-cascade.spec.ts`, `tests/helpers/{dbSnapshot,accounts,auth}.ts` + `tests/e2e/*.ts` glob (28 specs enumerated).
**Pattern extraction date:** 2026-06-17

---

## PATTERN MAPPING COMPLETE

**Phase:** 392 - Perbaikan CreateWorker + Audit Field
**Files classified:** 2
**Analogs found:** 2 / 2

### Coverage
- Files with exact analog: 2 (CreateWorker.cshtml — in-file + Settings.cshtml; createworker-392.spec.ts — assessment-title-flexible + delete-records-cascade)
- Files with role-match analog: 0
- Files with no analog: 0

### Key Patterns Identified
- Client-side validation = standard `@section Scripts { @await Html.PartialAsync("_ValidationScriptsPartial") ... }`, partial section-mounted so it loads after jQuery (`_Layout` L241 → L267). Exact precedent: `Settings.cshtml:137-146`.
- Inline validation spans are a single in-file template (`<span asp-validation-for="X" class="text-danger small"></span>`); 6 already exist, only 4 org fields need adding — grep before insert to avoid F-NEW-01 duplicates.
- Playwright admin e2e = accounts-fixture login (`admin@pertamina.com`/`123456`) + inline `Promise.all([waitForURL-off-login, click])`; self-cleaning teardown via serial + `afterAll` + `db.queryString` (dbSnapshot helper), DeleteWorker POST (not raw SQL) for Identity cascade.
- Teardown nuance: `#delete-{id}` form only renders for INACTIVE workers; a fresh active worker needs deactivate-first or a direct authenticated DeleteWorker POST.

### File Created
`.planning/phases/392-perbaikan-createworker-audit-field/392-PATTERNS.md`

### Ready for Planning
Pattern mapping complete. Planner can reference these analog excerpts directly in PLAN.md action steps.
