# Phase 323: Fix Cascade Bug AssessmentEditLogs - Pattern Map

**Mapped:** 2026-05-26
**Files analyzed:** 3 (1 MODIFY backend, 1 CREATE spec, 1 APPEND journal)
**Analogs found:** 3 / 3 (semua exact-match — pattern Phase 312 + Phase 321 + SEED_WORKFLOW existing)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` (3 spot edit: ~L2071 / ~L2215 / ~L2348) | controller | request-response (POST cascade delete) | **same file** L2073-2080 (`PackageUserResponses` cascade block Phase 312) | exact — copy 3x dengan filter `AssessmentSessionId == id` / `siblingIds.Contains(...)` / `groupIds.Contains(...)` |
| `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` (NEW) | test (e2e Playwright spec) | request-response (browser-driven HTTP POST → DB assert) | `tests/e2e/edit-peserta-answers.spec.ts` (Phase 321) | exact — same fixture `accounts.admin`, same `loginAny`, same env var session ID pattern |
| `docs/SEED_JOURNAL.md` (APPEND 1 row) | docs (audit trail) | log-append | existing journal entries (50+ rows since Phase 313) | exact — fixed 7-column markdown table format |

---

## Pattern Assignments

### 1. `Controllers/AssessmentAdminController.cs` — 3 Endpoint Patch (controller, request-response)

**Analog:** SAME FILE — `PackageUserResponses` cascade block (Phase 312 pattern, verified live di L2073-2080, L2215-2220, L2348-2352)

---

#### 1.1 Imports / using statements (NO CHANGE)

File header sudah punya semua `using` yang dibutuhkan (`Microsoft.EntityFrameworkCore`, `_context.AssessmentEditLogs` dbset auto-available via existing DbContext registration). Tidak perlu tambah using baru.

---

#### 1.2 Auth pattern (NO CHANGE — preserve existing)

3 endpoint sudah punya guard `[HttpPost]` + `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` + body `EnsureCanDeleteAsync(...)` (Phase 312 role-tier guard). JANGAN modifikasi attribute / guard. EditLogs block sisip DALAM transaction scope existing.

```csharp
// Source: Controllers/AssessmentAdminController.cs:2008-2013 (DeleteAssessment header — example, sama pola 3 endpoint)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessment(int id)
{
    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
    // ... try-catch wrap ...
```

---

#### 1.3 Core cascade pattern (COPY EXACT — Phase 312 template)

**Reference template (existing `PackageUserResponses` block — `DeleteAssessment` L2072-2080):**

```csharp
// Source: Controllers/AssessmentAdminController.cs:2072-2080 (EXISTING, COPY THIS STRUCTURE)
// Delete PackageUserResponses (Restrict FK — must be removed before session)
var pkgResponses = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == id)
    .ToListAsync();
if (pkgResponses.Any())
{
    logger.LogInformation($"Deleting {pkgResponses.Count} package user responses");
    _context.PackageUserResponses.RemoveRange(pkgResponses);
}
```

**NEW block to add (Phase 323 — sisip SEBELUM `pkgResponses` di L2071-an, per D-01):**

```csharp
// PHASE 323: Delete AssessmentEditLogs (Restrict FK — must be removed before session)
var editLogs = await _context.AssessmentEditLogs
    .Where(e => e.AssessmentSessionId == id)
    .ToListAsync();
if (editLogs.Any())
{
    logger.LogInformation($"Deleting {editLogs.Count} assessment edit logs");
    _context.AssessmentEditLogs.RemoveRange(editLogs);
}
```

**Untuk `DeleteAssessmentGroup` (~L2215) — multi-session variant:**
- Predicate berubah: `e => siblingIds.Contains(e.AssessmentSessionId)`
- Variable name: `allEditLogs`
- Log message: `$"DeleteAssessmentGroup: deleting {allEditLogs.Count} edit logs across {siblingIds.Count} sessions"`

**Untuk `DeletePrePostGroup` (~L2348) — LinkedGroupId variant:**
- Predicate: `e => groupIds.Contains(e.AssessmentSessionId)`
- Variable name: `allEditLogs`
- Log message: `$"DeletePrePostGroup: deleting {allEditLogs.Count} edit logs across {groupIds.Count} sessions (LinkedGroupId={linkedGroupId})"`

---

#### 1.4 Snapshot capture pattern (COPY EXACT — Phase 312 template)

**Reference template (existing `preDeleteResponseCount` di `DeleteAssessment` L2056-2057):**

```csharp
// Source: Controllers/AssessmentAdminController.cs:2056-2057 (EXISTING)
int preDeleteResponseCount = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == id);
```

**NEW snapshot to add (sisip TEPAT SETELAH existing `preDeleteResponseCount` capture, SEBELUM HC re-check guard L2062):**

```csharp
// PHASE 323: snapshot EditLog count SEBELUM cascade (sama pola preDeleteResponseCount)
int preDeleteEditLogsCount = await _context.AssessmentEditLogs
    .CountAsync(e => e.AssessmentSessionId == id);
```

**Per endpoint predicate adjustment:**
- `DeleteAssessment` L2057-an: `e => e.AssessmentSessionId == id`
- `DeleteAssessmentGroup` L2202-an: `e => siblingIds.Contains(e.AssessmentSessionId)`
- `DeletePrePostGroup` L2333-an: `e => groupIds.Contains(e.AssessmentSessionId)`

---

#### 1.5 Audit description format extension (MODIFY existing string)

**Reference template (existing `DeleteAssessment` L2123-2129):**

```csharp
// Source: Controllers/AssessmentAdminController.cs:2123-2129 (EXISTING — modify Description string only)
await _auditLog.LogAsync(
    deleteUser?.Id ?? "",
    deleteActorName,
    "DeleteAssessment",
    $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount}",
    id,
    "AssessmentSession");
```

**NEW (append ` EditLogsCount={preDeleteEditLogsCount}` di tail Description):**

```csharp
// PHASE 323: append EditLogsCount token
await _auditLog.LogAsync(
    deleteUser?.Id ?? "",
    deleteActorName,
    "DeleteAssessment",
    $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount} EditLogsCount={preDeleteEditLogsCount}",
    id,
    "AssessmentSession");
```

Per endpoint format final (D-02):
- `DeleteAssessment` L2127: `"... ResponseCount={N} EditLogsCount={M}"`
- `DeleteAssessmentGroup` L2265: `"... ResponseCount={N} EditLogsCount={M}"` (sesudah `SessionCount=...` existing)
- `DeletePrePostGroup` L2392: `"... ResponseCount={N} EditLogsCount={M}"`

---

#### 1.6 Error handling pattern (NO CHANGE — preserve existing)

Generic try-catch wrap existing di L2140/2278/2404 sudah benar — `using var tx` auto-rollback saat exception, TempData["Error"] set ke pesan flash bilingual. EditLogs block AKAN benefit dari rollback transparent karena sisip DI DALAM scope `tx`.

```csharp
// Source: Controllers/AssessmentAdminController.cs:2140-2145 (EXISTING — DO NOT MODIFY)
catch (Exception ex)
{
    logger.LogError(ex, "Error deleting assessment {Id}", id);
    TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
    return RedirectToAction("ManageAssessment");
}
```

---

#### 1.7 Insertion order summary (D-01 LOCKED)

Per endpoint, urutan akhir di dalam `using var tx`:

```
1. EnsureCanDeleteAsync guard (existing)
2. Capture preDeleteStatus + preDeleteResponseCount (existing)
3. Capture preDeleteEditLogsCount   ◄── NEW (sisip antara #2 dan #4)
4. HC re-check guard (existing)
5. RemoveRange(AssessmentEditLogs)  ◄── NEW (sisip antara #4 dan #6)
6. RemoveRange(PackageUserResponses) (existing)
7. RemoveRange(AssessmentAttemptHistory) (existing)
8. RemoveRange(Packages + Questions + Options) (existing)
9. Remove(AssessmentSession) (existing)
10. SaveChangesAsync + CommitAsync (existing)
11. Audit log description w/ EditLogsCount token (modify existing)
```

---

### 2. `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` (NEW — Playwright spec)

**Analog:** `tests/e2e/edit-peserta-answers.spec.ts` (Phase 321) — exact pattern match

---

#### 2.1 Imports pattern (lines 17-18 dari analog)

```typescript
// Source: tests/e2e/edit-peserta-answers.spec.ts:17-18 (COPY VERBATIM)
import { test, expect, type Page } from '@playwright/test';
import { accounts, AccountKey } from '../helpers/accounts';
```

---

#### 2.2 Env var session ID pattern (lines 20-23 dari analog)

```typescript
// Source: tests/e2e/edit-peserta-answers.spec.ts:20-23 (ADAPT — 3 env var instead of 1)
const SESSION_NO_EDITS = parseInt(process.env.P323_SESSION_NO_EDITS ?? '0', 10);
const SESSION_WITH_EDITS = parseInt(process.env.P323_SESSION_WITH_EDITS ?? '0', 10);
const SESSION_GROUP_REP = parseInt(process.env.P323_SESSION_GROUP_REP ?? '0', 10);
```

Pattern: gunakan `parseInt(process.env.XXX ?? '0', 10)` lalu validate di `beforeAll` (avoid hardcode session ID — Pitfall 4 RESEARCH.md). Env var nama prefix `P323_` untuk discoverability.

---

#### 2.3 Login helper pattern (lines 25-34 dari analog)

```typescript
// Source: tests/e2e/edit-peserta-answers.spec.ts:25-34 (COPY VERBATIM — inline helper, jangan extract)
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

Catatan: PAKAI `input[name="email"]` lowercase (NOT `[name="Email"]`) — verified di `accounts.ts` + login form Razor existing.

---

#### 2.4 Account fixture (helper reuse)

```typescript
// Source: tests/helpers/accounts.ts:2 (REUSE — no new account needed)
// accounts.admin = { email: 'admin@pertamina.com', password: '123456', role: 'Admin' }
await loginAny(page, 'admin');
```

Pakai `'admin'` key sudah cukup untuk 3 skenario (Worker/HC variant tidak dibutuhkan di scope CASCADE-01 — endpoint sudah `[Authorize(Roles="Admin, HC")]`).

---

#### 2.5 Test structure pattern (lines 36-42 dari analog — beforeAll guard)

```typescript
// Source: tests/e2e/edit-peserta-answers.spec.ts:36-42 (ADAPT untuk 3 env var)
test.describe('Phase 323 — Cascade AssessmentEditLogs Delete', () => {

  test.beforeAll(() => {
    if (!SESSION_NO_EDITS || !SESSION_WITH_EDITS || !SESSION_GROUP_REP) {
      throw new Error('Set P323_SESSION_NO_EDITS, P323_SESSION_WITH_EDITS, P323_SESSION_GROUP_REP env vars. See docs/SEED_JOURNAL.md Phase 323 entry.');
    }
  });

  // 3 test cases di sini
});
```

---

#### 2.6 Test case core pattern (3 skenario D-03)

3 test independent — masing-masing login admin, navigate `/Admin/ManageAssessment`, trigger delete via UI button/form, assert success banner muncul (NOT "Gagal menghapus assessment") + row hilang di listing post-redirect.

```typescript
test('a) Session no-edits → delete OK (no regression)', async ({ page }) => {
  await loginAny(page, 'admin');
  await page.goto('/Admin/ManageAssessment');
  // ... trigger delete untuk SESSION_NO_EDITS ...
  // ... assert success banner + row gone ...
});

test('b) Session 1+ edits → delete OK, EditLogs ikut terhapus', async ({ page }) => {
  await loginAny(page, 'admin');
  await page.goto('/Admin/ManageAssessment');
  // ... trigger delete untuk SESSION_WITH_EDITS ...
  // ... assert success banner (NOT regression flash "Gagal menghapus assessment") ...
});

test('c) Group campuran sibling no-edits + edits → delete OK', async ({ page }) => {
  await loginAny(page, 'admin');
  await page.goto('/Admin/ManageAssessment');
  // ... trigger "Hapus Grup" untuk SESSION_GROUP_REP ...
});
```

**Open Question dari RESEARCH.md #1:** Selector tombol Delete + Hapus Grup di partial views `Views/Admin/ManageAssessment*.cshtml` belum di-probe. Plan Wave 0 mini-task: probe selector via Read `Views/Admin/Partials/ManageAssessmentTab_Assessment.cshtml` (atau equivalent post Phase 322) + reference `tests/e2e/manage-assessment-filter.spec.ts` yang sudah navigate halaman ini.

---

### 3. `docs/SEED_JOURNAL.md` — Append Entry (docs, log-append)

**Analog:** Existing 80+ entries since Phase 313 — fixed 7-column markdown table format

---

#### 3.1 Table format (header L7)

```markdown
| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |
```

---

#### 3.2 Reference entry untuk pattern (Phase 313 L9 — closest analog: temporary + AssessmentSessions/Packages/Questions affected)

```markdown
| 2026-05-08 | 313 | temporary + local-only | UAT FLOW 313 timer enforcement (TMR-01) ... | AssessmentSessions(7) prefix `Phase 313 Timer Fixture`; ... | HcPortalDB_Dev.20260508-pre313.bak | cleaned |
```

---

#### 3.3 NEW entry to append (template — fill `{ts}` runtime)

```markdown
| 2026-05-26 | 323 | temporary + local-only | Smoke test cascade AssessmentEditLogs (3 skenario: no-edits / with-edits / group-mixed) — verify FK Restrict cleanup di DeleteAssessment + DeleteAssessmentGroup + DeletePrePostGroup | AssessmentEditLogs(+1-3 row) prefix `[P323 SEED]` FK ke 2-3 existing AssessmentSession id; tidak insert Session/Package/Question baru (pakai existing) | C:/Program Files/Microsoft SQL Server/MSSQL17.SQLEXPRESS/MSSQL/Backup/HcPortalDB_Dev-pre323-{ts}.bak | active |
```

Status flow: `active` saat insert seed → after Playwright run + RESTORE → update inline ke `cleaned`.

---

## Shared Patterns

### Transaction Scope Wrap
**Source:** `Controllers/AssessmentAdminController.cs:2040, :2184, :2313` (3 endpoint existing — Phase 312 WR-01)
**Apply to:** Cascade block EditLogs di 3 endpoint — JANGAN buat `using var tx` baru, sisip DI DALAM scope existing.

```csharp
// Source: L2040 (DeleteAssessment) — IDENTICAL di L2184, L2313
using var tx = await _context.Database.BeginTransactionAsync();
// ... guard + snapshot + cascade chain (Phase 323 inserts here) + SaveChanges + Commit
```

### Logger Acquisition
**Source:** `Controllers/AssessmentAdminController.cs:2017, :2154, :2292` (3 endpoint — per-method instance via RequestServices)
**Apply to:** Log messages di 3 endpoint cascade block — pakai `logger` variable yang sudah declare di method scope.

```csharp
var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
```

### EF Core RemoveRange + `.Any()` Guard
**Source:** `Controllers/AssessmentAdminController.cs:2076-2080` (PackageUserResponses block — Phase 312 D-04 logging pattern)
**Apply to:** AssessmentEditLogs cascade block — D-04 lock skip log saat collection empty.

```csharp
var x = await _context.X.Where(...).ToListAsync();
if (x.Any())
{
    logger.LogInformation($"Deleting {x.Count} ...");
    _context.X.RemoveRange(x);
}
```

### Audit Log Description Append (string interpolation)
**Source:** `Controllers/AssessmentAdminController.cs:2127, :2265, :2392` — Description field di-build via C# `$"..."` interpolation
**Apply to:** Audit log description 3 endpoint — append ` EditLogsCount={preDeleteEditLogsCount}` di tail string. SAFE karena `Description` `nvarchar(max)` (verified `Models/AuditLog.cs:36-37`).

### Playwright Login Fixture Reuse
**Source:** `tests/helpers/accounts.ts:2` + `tests/e2e/edit-peserta-answers.spec.ts:25-34` (Phase 321)
**Apply to:** Phase 323 spec — REUSE `accounts.admin` + inline `loginAny`. Jangan extract helper baru.

### Seed Workflow Snapshot+Restore Lifecycle
**Source:** `docs/SEED_WORKFLOW.md` §5 sqlcmd template + `docs/SEED_JOURNAL.md` table format
**Apply to:** Wave 0 setup spec — BACKUP DATABASE sebelum INSERT, RESTORE FROM DISK setelah Playwright run, update journal status `active` → `cleaned`.

```bash
# Source: docs/SEED_WORKFLOW.md §5.1 (BACKUP template)
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='<default-backup-dir>/HcPortalDB_Dev-pre323-{ts}.bak' WITH INIT"

# Source: docs/SEED_WORKFLOW.md §5.4 (RESTORE template)
sqlcmd -S "localhost\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='<path>.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"
```

---

## No Analog Found

**(None)** — Semua 3 file punya exact analog di codebase. Phase 323 = pure pattern-copy refactor; tidak ada library decision atau pattern baru.

| File | Status |
|------|--------|
| `Controllers/AssessmentAdminController.cs` (cascade block) | Phase 312 pattern same file — exact match |
| `tests/e2e/Phase323_CascadeAssessmentEditLogs.spec.ts` | Phase 321 spec — exact match |
| `docs/SEED_JOURNAL.md` (entry append) | 80+ existing entries — exact format match |

---

## Metadata

**Analog search scope:**
- `Controllers/AssessmentAdminController.cs` (3 cascade endpoint L2017-2410)
- `tests/e2e/*.spec.ts` (9 spec files; primary analog: `edit-peserta-answers.spec.ts`)
- `tests/helpers/accounts.ts`
- `docs/SEED_JOURNAL.md` (80+ existing entries)
- `docs/SEED_WORKFLOW.md` (§5 sqlcmd templates)

**Files scanned via Read tool:** 6 (CONTEXT.md, RESEARCH.md, AssessmentAdminController.cs L2030-2160 + L2180-2310 + L2310-2420, edit-peserta-answers.spec.ts L1-80, accounts.ts, SEED_JOURNAL.md L1-60)

**Pattern extraction date:** 2026-05-26

**Key insight for planner:** Phase 323 risiko terbesar BUKAN pemilihan pattern (semua sudah ada exact-match), melainkan **disiplin urutan insertion** (D-01 EditLogs first; snapshot before RemoveRange — Pitfall 1+2 RESEARCH.md) dan **konsistensi 3x copy** (jangan lupa cabang DeleteAssessmentGroup / DeletePrePostGroup — Pitfall 3). Plan task per endpoint wajib explicit insertion line number.
