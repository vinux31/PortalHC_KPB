# Phase 314 — Root Cause Finalized

**Captured:** 2026-05-08T09:30 (cross-validated Dev `10.55.3.3` + lokal `localhost:5277`)
**Tester:** admin@pertamina.com via Playwright MCP

## Verdict

**Root cause = ROUTING MISMATCH dari Phase 287 controller split (BUKAN data-shape exception).**

## Evidence Cross-Environment

| Env | Fixture | URL POST | Result |
|-----|---------|----------|--------|
| **Dev** (`10.55.3.3`) | id=6, Status=Upcoming, Token=ATBDF5 | `POST /KPB-PortalHC/Admin/RegenerateToken/6` | **404 Not Found** |
| **Lokal** (`localhost:5277`) | id=150, Status=Upcoming, Token=QKYJWN | `POST /Admin/RegenerateToken/150` | **404 Not Found** |

**Bug konsisten di Dev + lokal** → Hipotesis 5 (Dev binary stale) **RULED OUT**. Bug = code-level di main branch current.

## Root Cause Analysis

### Phase 287 Refactor Side-Effect

Commit `0620b545` (`refactor(287-01): remove assessment code from AdminController + fix redirects`) memindahkan method `RegenerateToken(int id)` dari `AdminController` ke `AssessmentAdminController`. Tapi **frontend URL hardcode tidak di-update** untuk match attribute routing baru.

### Routing Mismatch

**Frontend hardcode (Views/Admin/AssessmentMonitoring.cshtml:400 + Views/Admin/ManageAssessment.cshtml:456):**

```javascript
fetch(appUrl('/Admin/RegenerateToken/' + id), { method: 'POST', ... })
// Generates: POST /Admin/RegenerateToken/{id}
```

**Backend route registration di `Program.cs:202-204`:**

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
```

**Backend attribute route di `Controllers/AssessmentAdminController.cs:19`:**

```csharp
[Route("Admin/[action]")]
public class AssessmentAdminController : AdminBaseController
```

### Resolution Path Failure

URL `/Admin/RegenerateToken/150` masuk ASP.NET Core routing:

1. **Conventional route** `{controller}/{action}/{id?}` match: `controller=Admin, action=RegenerateToken, id=150` → mencari `AdminController.RegenerateToken(150)` → **TIDAK ada** (moved ke `AssessmentAdminController` di Phase 287, original controller method dihapus per commit `0620b545`)
2. **Attribute route** `Admin/[action]` di `AssessmentAdminController` → match literal `Admin/RegenerateToken` only (no `{id}` placeholder) → URL `/Admin/RegenerateToken/150` punya `/150` ekstra segment → **TIDAK match**

**Result:** Both routing strategies miss → 404 Not Found.

## Frontend Generic Error (Hipotesis 4 Confirmed)

Server return 404 (transport-level error), tapi frontend handler line 414 di kedua view menampilkan generic alert "Gagal regenerate token. Periksa koneksi jaringan." karena fetch catch handler tidak distinguish HTTP status:

```javascript
.catch(function (err) {
    alert('Gagal regenerate token. Periksa koneksi jaringan.');
});
```

User mendapat misleading message ("network issue" padahal server 404 = endpoint missing).

## Fix Options

| # | Solusi | File | Diff Size | Risk |
|---|--------|------|-----------|------|
| **A** | **Backend method-level route**: tambah `[Route("RegenerateToken/{id}")]` di `Controllers/AssessmentAdminController.cs:2424` setelah `[HttpPost]` | 1 file, 1 baris | Low | **RECOMMEND** — isolated, no frontend touch |
| B | Frontend URL → query string: `appUrl('/Admin/RegenerateToken?id=' + id)` di 2 view | 2 file, 2 baris | Low | Affect URL contract; cosmetic break |
| C | Controller-level route + id placeholder: `[Route("Admin/[action]/{id?}")]` | 1 file, 1 baris BUT affect ALL actions di controller | Medium | Possible regression action lain |

## Revisi Plan 02 Scope (Implications)

**Original Plan 02 design** (per CONTEXT.md): 8 layered defensive guards (D-05/D-06/D-12/D-17/D-20/D-21/D-25/D-33) di method body + frontend message propagate (D-07).

**Aktual root cause:** Routing mismatch — method body never executed (404 di routing layer).

**Revisi recommendation:**

1. **Primary fix (NEW, KRITIKAL):** Tambah `[Route("RegenerateToken/{id}")]` di `Controllers/AssessmentAdminController.cs:2424` setelah `[HttpPost]` line. Ini fix actual bug.
2. **D-07 frontend handler propagate** — tetap valid future-proof, tambah handler 404/non-JSON case dengan wording "Endpoint tidak tersedia, hubungi admin sistem" untuk discriminate dari network issue genuine.
3. **D-05/D-06/D-12/D-17/D-20/D-21/D-25/D-33 (8 layered guards)** — tetap valid sebagai defense-in-depth untuk method body (currently never executed). Setelah Fix #1 landed, semua guards ini jadi reachable + relevant. Tetap di-include di Plan 02.
4. **D-37/D-38 conditional Schedule MinValue guard** — drives by data shape baseline. Plan 01 Task 3 SQL queries TIDAK kritikal karena root cause sudah found via routing analysis. Tetap useful untuk D-37/D-38 conditional inclusion.

## Audit Implications

- **REQUIRES SECONDARY VIEW SCAN:** apakah ada hardcode URL `/Admin/{action}/{id}` di view lain yang juga affected post-Phase 287 split? (e.g., DeleteAssessment, EditAssessment via JS fetch). Plan 02 Task 1 add scope: grep-audit semua `fetch(appUrl('/Admin/` patterns di Views/Admin/*.cshtml.

## Action Items (Revised)

1. **IMMEDIATE PATCH (lokal-first per CLAUDE.md DEV_WORKFLOW):**
   - Edit `Controllers/AssessmentAdminController.cs:2424` add line: `[Route("RegenerateToken/{id}")]`
   - Test lokal: re-run repro Step 1 → expect 200 + token rotated
   - Commit: `fix(314): add method-level route to RegenerateToken (Phase 287 split aftermath, fixes 404)`

2. **AUDIT SECONDARY:** grep `Views/Admin/*.cshtml` untuk pattern `fetch(appUrl('/Admin/.+/' + id)` → fix all matches similarly

3. **PUSH ke Team IT:** request Dev redeploy setelah commit landed di main

4. **Re-validate Dev:** Step 1 repro post-deploy

## Hypotheses Update

| Hipotesis | Pre-repro Status | Post-repro Status |
|-----------|-----------------|-------------------|
| H1: NRE Schedule.Date | likely | **RULED OUT** — method body never executed |
| H2: DbUpdateException AuditLog FK | possible | **RULED OUT** — same reason |
| H3: SqlException concurrency | possible | **RULED OUT** — same reason |
| H4: Frontend handler hide message | likely | **PARTIALLY CONFIRMED** — handler IS generic, MISLEADING for 404 case |
| H5: Dev binary stale | NEW post-Step 1 | **RULED OUT** — lokal juga repro |
| **H6 (BARU): Routing mismatch from Phase 287 controller split** | not predicted | **CONFIRMED** — root cause validated cross-env |
