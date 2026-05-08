# Phase 314: Fix Regenerate Token untuk Status Upcoming - Research

**Researched:** 2026-05-08
**Domain:** ASP.NET Core MVC defensive bug investigation + EF Core LINQ translation + frontend fetch error propagation + ASP.NET Core Identity session model
**Confidence:** HIGH (mayoritas finding terverifikasi langsung dari file repo + cross-cek 10+ usage `Schedule.Date` di codebase)
**Researcher input:** integrated dengan CONTEXT.md (39 keputusan locked, 11 area)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Repro & Investigation Strategy**
- **D-01:** Repro via URL Dev (`http://10.55.3.3/KPB-PortalHC`). Buat assessment sendiri (Status=Upcoming, IsTokenRequired=true, peserta=admin@pertamina.com → 0 worker yang sudah masuk ujian). Confirm bug exists di Dev dulu, baru replicate kondisi minimal di lokal untuk fix.
- **D-02:** Capture exception dari server log Dev sebagai langkah pertama. Stacktrace pinpoint root cause langsung. Fallback: kalau log tidak accessible, iterate 4 hipotesis manual.
- **D-03:** RESEARCH.md format Standard — tabel 4 hipotesis (NRE Schedule / AuditLog FK / Concurrency / Frontend) dengan kolom Evidence/Status (CONFIRMED/RULED OUT/INCONCLUSIVE) + root cause section + fix proposal.
- **D-04:** Plan structure split sesuai ROADMAP literal: Plan 01 (Repro+RESEARCH) + Plan 02 (Patch backend + frontend + smoke test).

**Patch Philosophy (Backend)**
- **D-05:** Defensive coverage 4 hipotesis — fix root cause yang terbukti dari stacktrace + tambah guard untuk 3 hipotesis lain.
- **D-06:** Audit log atomicity = try-catch swallow + LogWarning fallback (Phase 306 D-10 pattern).
- **D-17:** Transaction wrap loop sibling update + SaveChangesAsync dalam `_context.Database.BeginTransactionAsync()` → atomic commit/rollback. Audit log try-catch (D-06) berlaku **di luar** transaction utama.
- **D-18:** Tidak perlu re-fetch / TOCTOU guard. Race window sempit + last-writer-wins acceptable.

**Frontend Response Handler & Error UX**
- **D-07:** Fix `.catch()` agar parse server error body — `success: false` → throw error dengan `data.message`.
- **D-08:** Patch 3 view sekaligus — AssessmentMonitoring.cshtml + AssessmentMonitoringDetail.cshtml + ManageAssessment.cshtml line 456.
- **D-09:** alert() native + propagate server message. Format: `'Gagal regenerate token: {pesan server}. Coba lagi atau hubungi IT.'`.
- **D-10:** Wording = server message langsung passthrough (technical untuk Admin/HC role).
- **D-11:** Handle non-JSON 5xx via `response.ok` check + fallback `r.text()`.
- **D-12:** Server-side return error specific by exception type (DbUpdateException / NullReferenceException / generic Exception).

**Logging & Telemetry**
- **D-19:** `hasStarted` definition = `siblings.Any(s => s.StartedAt != null)`.
- **D-20:** Extended structured logging di catch block dengan {Id, Status, HasStarted, SiblingCount, IsTokenRequired}.
- **D-21:** `LogInformation` di success path dengan {Id, Count, ActorName}.

**Open + Active Worker Behavior**
- **D-22:** Allow regen tapi warn admin di confirm dialog kalau Open + siblingStartedCount > 0.
- **D-23:** Warning wording dengan {N} = `siblings.Count(s => s.StartedAt != null)`.

**Smoke Test (E2E Playwright)**
- **D-13:** Mechanism = Playwright E2E dengan dedicated fixture (Phase 312/313 pattern). 3 skenario.
- **D-14:** Seeding via UI Admin setup hook — login admin → /Admin/CreateAssessment → 3 fixture title `Phase 314 Token Fixture {Upcoming0|UpcomingPartial|OpenRunning}`.
- **D-15:** Assertion comprehensive (a) AccessToken DB berubah, (b) sibling sessions ter-update, (c) AuditLogs row exists, (d) UI alert.
- **D-16:** File baru `tests/e2e/admin-assessment-token.spec.ts` (konsisten Phase 312 pattern).
- **D-24:** Skenario test #3 (Open running) verify worker dengan token lama dapat error invalid saat next request. **Asumsi perlu di-verify research:** apakah token lama benar-benar invalidate worker session, atau session worker sudah established via cookie/identity sehingga token cuma untuk login flow.

**Pre-condition Validation Order & Scope**
- **D-25:** Block Status `Cancelled` + `Completed`.
- **D-26:** Role validation cukup `[Authorize(Roles="Admin, HC")]` existing.
- **D-27:** Tidak perlu separate schedule-passed guard.
- **D-28:** Order validation = extend existing flow linear, tidak extract ke helper.

**Sibling Group Key Matching Robustness**
- **D-29:** Title matching tetap as-is (SQL Server CI_AS).
- **D-30:** `Schedule.Date` matching as-is (UTC consistent).
- **D-31:** Category matching as-is (asumsi `[Required]` non-null — verify research).
- **D-32:** Sibling group key matching = audit-only di RESEARCH.md.

**Sibling List Edge Cases**
- **D-33:** Sibling 0-row → throw error explanatory + abort regen.
- **D-34:** Sibling 1-row (Online single) = allow regen normal.
- **D-35:** `LogWarning` untuk anomalous sibling count.
- **D-36:** Test coverage 1-sibling implicit via skenario #3.

**Schedule Invalid Defensive**
- **D-37:** Schedule MinValue (`'0001-01-01'`) tidak guard upfront — audit dulu di RESEARCH.
- **D-38:** Conditional defensive guard di Plan 02 KALAU RESEARCH confirm legacy MinValue rows.
- **D-39:** Plan 01 RESEARCH.md include 5 comprehensive DB sample queries (a-e).

### Claude's Discretion

- Stacktrace parsing logic untuk RESEARCH.md — exact format tabel/markdown adjustment sesuai temuan investigasi
- Error wording kontekstual untuk SC #5 — exact phrasing bahasa Indonesia
- Spinner/disable button visual saat regen in-flight — replicate Detail view pattern ke 2 view lain kalau perlu
- Test fixture cleanup — Playwright afterEach delete vs leave-as-is
- Inline `data-started-count` rendering vs lightweight GET endpoint untuk D-23 wording

### Deferred Ideas (OUT OF SCOPE)

- TOCTOU re-fetch + version check (D-18 alternative)
- Concurrency token (RowVersion / [Timestamp]) — schema change
- Pessimistic lock SQL `WITH (UPDLOCK)`
- Toast/Bootstrap banner UX (D-09 alternative)
- Extract reusable `regenerateTokenWithErrorHandling()` JS helper
- Server-side cookie/session invalidate on token regen (D-24 followup)
- `HttpContext.TraceIdentifier` correlation ID di logging
- Hybrid Playwright + manual UAT
- Title trim/lowercase normalize (D-29 alternative)
- Category null-coalesce matching (D-31 alternative)
- `EnsureCanRegenerateAsync()` helper extraction (D-28 alternative)
- Ownership scope check granular (D-26 alternative)
- Token uniqueness collision retry
- `UpdatedBy` field di sibling row — schema change
- Data cleanup script untuk Schedule MinValue legacy rows

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| TKN-01 | `RegenerateToken()` (line 2427-2475) berhasil meregenerasi token untuk assessment status `Upcoming` dengan `IsTokenRequired=true` dan **0 worker yang sudah masuk ujian**. Investigative: repro bug → identify root cause (hipotesis: NRE Schedule.Date / AuditLog FK / concurrency / frontend handler) → patch minimal → frontend propagasi error message detail. | Backend fix di method body (verified line 2427-2475 `RegenerateToken`); audit log integration via `_auditLog.LogAsync` (verified `Services/AuditLogService.cs:21-42`); transaction pattern existing di same controller (`BeginTransactionAsync` line 1965, 2051, 2195); frontend handler 3 view (verified `AssessmentMonitoring.cshtml:396-419`, `AssessmentMonitoringDetail.cshtml:1004-1033`, `ManageAssessment.cshtml:447-471`); test infrastructure ready (verified `tests/helpers/accounts.ts` admin fixture, `tests/e2e/` Playwright structure). |

</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia wajib** untuk semua text user-facing (alert message, TempData, dialog).
- **Develop workflow lokal → Dev (10.55.3.3) → Production.** Verifikasi lokal `dotnet build` + `dotnet run` (`http://localhost:5277`) sebelum commit. Promosi ke Dev/Prod = tanggung jawab Team IT.
- **Tidak boleh edit kode/DB langsung di server Dev/Prod.** Tidak push tanpa verifikasi lokal.
- **Tidak ada migration DB di Phase 314** (no schema change — confirmed di CONTEXT D-37/D-38; defensive guards di code only).
- **Seed Data Workflow:** Test fixture untuk Playwright D-14 berpotensi masuk klasifikasi `temporary + local-only`. Wajib snapshot DB lokal sebelum insert + restore setelah test + catat di `docs/SEED_JOURNAL.md`. Lihat §Validation Architecture / Seeding strategy below.

---

## Summary

Phase 314 adalah investigative bug fix untuk endpoint `RegenerateToken(int id)` di `Controllers/AssessmentAdminController.cs:2427-2475` yang gagal saat trigger condition `Status='Upcoming' + IsTokenRequired=true + 0 worker sudah masuk ujian`. Plan 01 (Repro+RESEARCH) capture exception dari server log Dev → validasi 4 hipotesis (NRE Schedule.Date / AuditLog FK / Concurrency / Frontend handler) → tulis root cause + fix proposal. Plan 02 (Patch+test) apply defensive backend patch (root cause + 3 guard hipotesis lain), frontend error propagation di 3 view, Playwright E2E smoke test 3 skenario.

Riset ini menghasilkan **4 prediksi pre-repro berdasarkan code-reading evidence** (kode existing, schema model, EF Core behavior, ASP.NET Core Identity semantic) yang men-skor 3 dari 4 hipotesis dari ROADMAP sebagai **probable RULED OUT**, dan 1 hipotesis (Frontend handler) sebagai **probable CONFIRMED**. Tabel evidence di §Hipotesis Tabel akan di-finalize oleh Plan 01 saat repro selesai dan stacktrace ada — pre-repro analisis bisa SALAH; stacktrace adalah ground truth.

**Primary recommendation:** **Eksekusi repro dulu (D-01/D-02) sebelum tulis patch.** Pre-repro analisis menunjukkan root cause **paling mungkin = frontend handler generic .catch()** yang menyembunyikan server message asli — bukan bug backend. Tapi jangan commit ke hipotesis ini sebelum stacktrace ada. Defensive backend patch (D-05) tetap dipasang regardless karena 4 hipotesis lain are valid future-proofing concerns.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Token regeneration logic (loop sibling update + AccessToken assign) | API / Backend (Controller body) | Database (AssessmentSessions UPDATE) | Server-side wajib (security: token generation pakai `RandomNumberGenerator`, tidak boleh client-side). Existing pattern preserved. |
| Sibling group lookup (`Title + Category + Schedule.Date` matching) | API / Backend (EF Core LINQ) | Database (SQL `CAST(Schedule AS date)` translation) | Pure server-side query, EF Core translate `DateTime.Date` ke SQL function `CAST(... AS date)` (verified used 10+ kali di codebase). |
| Atomicity guarantee (multi-row UPDATE + AuditLog) | API / Backend (`BeginTransactionAsync`) | Database (transaction commit/rollback) | D-17 explicit transaction wrap untuk siblings + SaveChanges; AuditLog di luar transaction (audit fail tidak rollback token). |
| AuditLog write (success + failure paths) | API / Backend (`AuditLogService.LogAsync`) | Database (AuditLogs INSERT) | Server-side audit integrity, no client-side write allowed. |
| Pre-condition validation (Status block Cancelled/Completed) | API / Backend (Controller body guard) | — | D-25 server-side reject; UI hide button is UX, server is authority. |
| Token generation (cryptographic randomness) | API / Backend (`RandomNumberGenerator.Create`) | — | Existing `GenerateSecureToken()` (line 2478-2492) — TIDAK diubah. |
| Error message propagation server → client JSON | API / Backend (return Json `{success, message}`) | Frontend Browser (parse JSON, display alert) | D-12 specific exception → server message; D-07/D-11 frontend `.then(r.json())` chain handle `success: false`. |
| Confirm dialog (regen + warning untuk Open+ActiveWorker) | Frontend Browser (vanilla JS `confirm()`) | Frontend Server (Razor `data-started-count` attribute) | D-22/D-23 dialog warning client-side; data input ke client via Razor `data-*` attributes saat render Monitoring view. |
| `appUrl()` URL prefix injection | Frontend Browser (helper) | Frontend Server (layout) | Existing helper, preserve untuk path-prefix safety (Phase 312 WR-02 lesson). |
| Smoke test 3 skenario | Test Layer (Playwright E2E) | API/Backend + Database (test fixture seeding via UI) | D-14 seeding via UI Admin = real flow integration. |

## Standard Stack

### Core (sudah ada di project — TIDAK install lib baru)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | .NET 8 | Controller actions, AntiForgery, Authorize attribute | [VERIFIED: Controllers/AssessmentAdminController.cs] |
| Entity Framework Core | 8.x | LINQ to SQL Server translation untuk `Schedule.Date` matching, `BeginTransactionAsync` | [VERIFIED: usage line 1965, 2051, 2195 + 10+ `Schedule.Date` queries] |
| ASP.NET Core Identity | .NET 8 | `_userManager.GetUserAsync(User)` untuk capture actor | [VERIFIED: AssessmentAdminController.cs line 2457] |
| `System.Security.Cryptography.RandomNumberGenerator` | .NET 8 BCL | `GenerateSecureToken()` 6-char alphanumeric (exclude 0/O/1/I/L) | [VERIFIED: line 2478-2492] |
| `Microsoft.Extensions.Logging` | 8.x | `_logger.LogError` / `LogWarning` / `LogInformation` (D-20/D-21/D-35) | [VERIFIED: existing line 2471-2472] |
| Bootstrap 5 | existing | Frontend modal/UX (regen confirm dialog) | [VERIFIED: existing layout] |
| Playwright | (existing tests/) | E2E smoke test 3 skenario | [VERIFIED: tests/e2e/assessment.spec.ts:1-80] |

### Supporting (existing helpers/services)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `AuditLogService` | internal | `_auditLog.LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` | Success entry (D-21) + tidak ada failure entry untuk regen (per D-06 swallow only, no `RegenerateTokenBlocked` action like Phase 312 D-03 — Phase 314 silent on guard fail karena UI flow sudah explanatory) |
| `appUrl()` JS helper | layout | Frontend URL prefix injection | All 3 view fetch URL [VERIFIED: existing usage line 400, 456, 1012] |
| `accounts.admin` Playwright fixture | tests/helpers/accounts.ts | `admin@pertamina.com` / `123456` login untuk E2E | D-14 seeding + D-13 E2E run [VERIFIED: tests/helpers/accounts.ts:2] |
| `wizardSelectors` helper | tests/e2e/helpers/ | Centralized selector constants | Pattern Phase 307/308. Phase 314 boleh extend dengan `tokenRegen` namespace. |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Standard `BeginTransactionAsync` (D-17) | Skip transaction, rely on EF Core implicit | EF Core implicit transaction sudah cover single SaveChangesAsync, tapi D-17 explicit untuk clarity dan future extension. **D-17 standard wins.** |
| Generic `catch (Exception)` (existing) | D-12 split per exception type (`DbUpdateException`, `NullReferenceException`, generic) | D-12 better diagnosability, slightly more diff. **D-12 standard wins** per CONTEXT lock. |
| Plain `confirm()` browser native (D-22) | Bootstrap 5 modal dialog | Plain confirm preserves consistency dengan existing 3 view (D-09 alert() native). Modal = scope creep. **Plain confirm + warning wording (D-23) wins.** |
| Hardcoded URL `/Admin/RegenerateToken/${id}` | `appUrl()` helper | `appUrl()` mandatory per Phase 312 WR-02 lesson (path-prefix bug). **`appUrl()` wins.** |

**Installation:** None required — semua library sudah ada.

**Version verification:** N/A (no new dependencies). Existing stack verified via `dotnet build` (Phase 311/312/313 baseline 0 errors, 92 warnings).

## System Architecture Diagram

```
[Admin user (admin@pertamina.com)]
        │  klik "Regenerate Token" di button .btn-regenerate-token
        ▼
[Browser — 1 dari 3 view] ──── confirm() dialog (D-22 warning kalau Open+ActiveWorker)
   │ Views/Admin/AssessmentMonitoring.cshtml:396-419
   │ Views/Admin/AssessmentMonitoringDetail.cshtml:1004-1033
   │ Views/Admin/ManageAssessment.cshtml:447-471
        │  POST /Admin/RegenerateToken/{id}
        │  Headers: RequestVerificationToken (CSRF)
        ▼
[ASP.NET Core MVC Pipeline]
   │ [Authorize(Roles="Admin, HC")]   ← authentication + authorization
   │ [ValidateAntiForgeryToken]       ← CSRF check
        ▼
[AssessmentAdminController.RegenerateToken(int id)]  line 2427-2475
        │
        ├─[1] FindAsync(id) ──► null? → return 200 JSON {success:false, message:"Assessment not found."}
        │
        ├─[2] !IsTokenRequired? → return 200 JSON {success:false, message:"This assessment does not require a token."}
        │
        ├─[3] (NEW D-25) Status==Cancelled||Completed? → return 200 JSON {success:false, message:"Tidak bisa regenerate token..."}
        │
        ├─[4] try {
        │       BeginTransactionAsync()  (NEW D-17)
        │       newToken = GenerateSecureToken()  ← `System.Security.Cryptography.RandomNumberGenerator`
        │       siblings = Where(Title+Category+Schedule.Date).ToListAsync()
        │
        │       (NEW D-33) siblings.Count == 0? → throw / return error explanatory
        │
        │       foreach sibling: AccessToken = newToken; UpdatedAt = now
        │       SaveChangesAsync()
        │       transaction.Commit()
        │
        │       (D-06) try { _auditLog.LogAsync("RegenerateToken", ...) }
        │              catch (auditEx) { _logger.LogWarning(auditEx, ...) }
        │
        │       (D-21) _logger.LogInformation("RegenerateToken success ...")
        │       return 200 JSON {success:true, token:newToken, message:"..."}
        │     }
        │
        └─[5] catch (DbUpdateException ex)        → 500 JSON {success:false, message:"Database error: " + ex.Message}
              catch (NullReferenceException ex)   → 500 JSON {success:false, message:"Data assessment tidak lengkap..."}
              catch (Exception ex)                → 500 JSON {success:false, message: ex.Message}
              + (D-20) _logger.LogError(ex, "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}", ...)

        ▼
[Browser — fetch().then().catch() chain]
   │ (D-11) if (!r.ok) → r.text() → Promise.reject  (handle 5xx)
   │ (D-07) if (data.success === false) → throw data.message
   │ catch(err) → alert('Gagal regenerate token: ' + err)  (D-09)
        ▼
[Admin sees alert with server message OR success token]
```

**Component Responsibilities (file-to-implementation mapping)**

| File | Responsibility | Phase 314 Action |
|------|----------------|------------------|
| `Controllers/AssessmentAdminController.cs:2427-2475` | `RegenerateToken(int id)` action body | Plan 02: tambah D-25 status block, D-17 transaction wrap, D-12 specific exception catch, D-20 extended log, D-21 success log, D-33 sibling 0-row guard, D-06 audit try-catch swallow |
| `Controllers/AssessmentAdminController.cs:2478-2492` | `GenerateSecureToken()` helper | TIDAK diubah — preserve existing |
| `Services/AuditLogService.cs:21-42` | `LogAsync` API | TIDAK diubah — preserve API |
| `Models/AssessmentSession.cs:18` | `Schedule` non-nullable DateTime | Confirm schema (NRE on `.Date` impossible at struct level — Hipotesis 1) |
| `Models/AuditLog.cs:13` | `ActorUserId [Required] string` | Confirm tidak ada FK constraint (Hipotesis 2) |
| `Data/ApplicationDbContext.cs:488-494` | AuditLog entity config | Confirm: hanya HasIndex + GETUTCDATE default. **TIDAK ADA HasOne FK ke AspNetUsers** |
| `Views/Admin/AssessmentMonitoring.cshtml:396-419` | Regen handler view #1 | Plan 02: D-07/D-11 fetch chain refactor + D-09 alert wording propagate |
| `Views/Admin/AssessmentMonitoringDetail.cshtml:1004-1033` | Regen handler view #2 (regenToken function dengan spinner) | Plan 02: same refactor + preserve spinner pattern + D-22/D-23 warning |
| `Views/Admin/ManageAssessment.cshtml:447-471` | Regen handler view #3 (HTMX integration) | Plan 02: same refactor; preserve HTMX-friendly delegation pattern |
| `tests/e2e/admin-assessment-token.spec.ts` | E2E smoke 3 skenario | NEW file (D-16) — Plan 02 |
| `tests/e2e/helpers/wizardSelectors.ts` | Selector constants | Optional extend dengan `tokenRegen` namespace (Claude's discretion) |

## Hipotesis Tabel — 4 Hipotesis dari ROADMAP (Pre-Repro Code Analysis)

> **Catatan penting:** Tabel ini berisi **pre-repro prediction** berdasarkan code-reading evidence. Plan 01 wajib **finalize** tabel ini setelah repro selesai dan stacktrace ada. Stacktrace adalah **ground truth**; pre-repro analisis bisa salah.

### Hipotesis 1 — NRE pada `assessment.Schedule.Date`

| Aspek | Detail |
|-------|--------|
| **Klaim** | `NullReferenceException` saat akses `assessment.Schedule.Date` di line 2447. |
| **Code site** | `Controllers/AssessmentAdminController.cs:2447` — `&& a.Schedule.Date == assessment.Schedule.Date` |
| **Schema check** | `Models/AssessmentSession.cs:18` — `public DateTime Schedule { get; set; }` (non-nullable struct) [VERIFIED] |
| **Pre-repro Status** | **PROBABLE RULED OUT** — `DateTime` adalah struct non-nullable; `.Date` property pada struct **secara teori** tidak bisa NRE. EF Core materialization pasti populate `Schedule` (DB column non-nullable). **`a.Schedule.Date` di LINQ ekspresi bukan accessor runtime** — di-translate EF Core ke SQL `CAST(a.Schedule AS date) = CAST(@p_schedule AS date)`. NRE di sisi C# in-memory iteration tidak mungkin. |
| **Cross-reference codebase** | Pattern `a.Schedule.Date` digunakan **10+ kali** di repo: line 185, 1041, 1532-1537, 1844, 1869, 1917, 2187, 2447, 2625-2627, 2690, 3252, 3279, 3291. Semua working di happy path Phase 297+ (ManageAssessment, EditAssessment, AssessmentMonitoring grouping). Kalau NRE on `Schedule.Date` valid, **semua endpoint ini akan break sekarang**. Tidak ada bug report semacam itu di STATE.md. |
| **Possible variant** | Bukan NRE pada **`Schedule.Date` itu sendiri**, tapi NRE pada `assessment` (kalau `FindAsync(id)` return null tapi tidak di-guard). **Sudah di-guard** di line 2430-2433 (`if (assessment == null) return Json(...)`). RULED OUT. |
| **Possible variant 2** | NRE pada navigation property — tidak relevan, `Schedule` adalah scalar `DateTime` value type, **bukan navigation property**. RULED OUT. |
| **Final Status** | ⏳ **INCONCLUSIVE pending stacktrace** — Plan 01 must check log Dev. Predicted: **RULED OUT**. |
| **Defensive guard (D-37/D-38)** | `Schedule == DateTime.MinValue` guard kalau RESEARCH §Data Shape Baseline confirm legacy `'0001-01-01'` rows exist. Lihat Query (a) di §Data Shape Baseline. |

### Hipotesis 2 — AuditLog FK violation pada `ActorUserId`

| Aspek | Detail |
|-------|--------|
| **Klaim** | `DbUpdateException` saat `_auditLog.LogAsync(regenUser?.Id ?? "", ...)` (line 2459-2465) karena FK constraint violation kalau `regenUser` null atau `Id` empty. |
| **Code site** | `Controllers/AssessmentAdminController.cs:2459-2465` + `Services/AuditLogService.cs:21-42` + `Models/AuditLog.cs:13` |
| **Schema check** | `Models/AuditLog.cs:13` — `[Required] public string ActorUserId { get; set; } = "";` (non-nullable, but no FK attribute) [VERIFIED] |
| **EF Core config check** | `Data/ApplicationDbContext.cs:488-494`: `builder.Entity<AuditLog>(entity => { HasIndex(CreatedAt); HasIndex(ActorUserId); HasIndex(ActionType); Property(CreatedAt).HasDefaultValueSql("GETUTCDATE()"); });` — **TIDAK ADA `HasOne(...).WithMany().HasForeignKey(...)` config** [VERIFIED] |
| **Pre-repro Status** | **PROBABLE RULED OUT** — Tidak ada FK constraint di DB level dari `AuditLogs.ActorUserId` ke `AspNetUsers.Id`. Hanya `[Required]` C# attribute (translated ke SQL `NOT NULL`). Kolom string non-null bisa accept empty string `""` (length 0). Existing `regenUser?.Id ?? ""` defensive sudah cukup. |
| **Edge case** | Kalau `_userManager.GetUserAsync(User)` cache miss karena DB Dev disconnect transient → `regenUser = null` → `regenUser?.Id ?? ""` = `""` → INSERT row dengan `ActorUserId=""`. **Tidak akan FK-violate** (no FK exists). Kalau ada `[Required]` validation di SaveChanges — empty string passes (string length 0, not null). |
| **Possible variant** | DbUpdateException karena **Description** field length exceed `nvarchar(max)` — TIDAK MUNGKIN, Description tidak di-cap di model (`[Required] public string Description`). Description value = `$"Regenerated access token for '{assessment.Title}' ({assessment.Category}, {assessment.Schedule:yyyy-MM-dd}) — {siblings.Count} sibling(s) updated"` (predictable < 500 char). RULED OUT. |
| **Possible variant 2** | DbUpdateException karena `ActionType` exceed `MaxLength(50)` — `"RegenerateToken"` = 15 chars, fits. RULED OUT. |
| **Final Status** | ⏳ **INCONCLUSIVE pending stacktrace** — Plan 01 must check log Dev. Predicted: **RULED OUT**. |
| **Defensive guard (D-06)** | Wrap `_auditLog.LogAsync` dalam try-catch + LogWarning fallback. Audit failure tidak block response success. Pattern Phase 306 D-10 [VERIFIED: `Controllers/AssessmentAdminController.cs:2407-2410` `DeletePrePostGroup` audit pattern]. |

### Hipotesis 3 — Concurrency race (2 admin klik regen barengan)

| Aspek | Detail |
|-------|--------|
| **Klaim** | Race condition: 2 admin klik regen barengan → loop `foreach` di line 2449-2453 + `SaveChangesAsync` line 2454 → 2 transaksi parallel update siblings yang sama → DbUpdateConcurrencyException atau token mismatch antar siblings. |
| **Code site** | `Controllers/AssessmentAdminController.cs:2440-2454` (loop foreach + SaveChangesAsync, **TIDAK** wrap explicit transaction) |
| **Pre-repro Status** | **PROBABLE RULED OUT untuk trigger condition specific** — Trigger condition spesifik = "0 worker yang sudah masuk ujian" + Status `Upcoming`. Race condition tidak otomatis trigger Status=Upcoming alone. Race butuh 2+ admin clicking simultaneously dalam window milidetik — frequency rare di Pertamina (1-2 admin per shift). |
| **EF Core behavior** | Without `[Timestamp]` / `RowVersion` column, EF Core tidak detect concurrent update — silent last-writer-wins (D-18). Tidak akan throw DbUpdateConcurrencyException kecuali ada concurrency token. **Existing schema tidak punya RowVersion** (deferred per CONTEXT.md). |
| **Cross-reference** | Patterns existing di same controller pakai explicit `BeginTransactionAsync` untuk multi-step writes: line 1184 (Pre-Post create), 1329 (token issuance), 1965 (bulk assign), 2051 (DeleteAssessment), 2195 (DeleteAssessmentGroup). RegenerateToken **tidak** wrap — outlier. D-17 align dengan existing convention. |
| **Trigger condition reproducibility** | "0 worker masuk ujian" → bukan race-causing condition. Concurrency would need specific timing of 2 admin simultaneous click. Tidak konsisten dengan reproducible bug ("trigger condition" implies repeatable). |
| **Final Status** | ⏳ **INCONCLUSIVE pending stacktrace** — Predicted: **RULED OUT** untuk trigger condition. **Mungkin background concern** untuk future, di-address oleh D-17 transaction wrap regardless. |
| **Defensive guard (D-17)** | `using var tx = await _context.Database.BeginTransactionAsync();` wrap loop + SaveChangesAsync + tx.CommitAsync(). Atomic commit/rollback. Audit log di luar transaksi (D-06). |

### Hipotesis 4 — Frontend response handler menyembunyikan server error message

| Aspek | Detail |
|-------|--------|
| **Klaim** | Server return 500 (atau 200 dengan `success: false`) → frontend `.then(r => r.json())` chain entah (a) reject karena response 5xx body bukan JSON, atau (b) ignore `data.message` dan tampil generic alert. Bug tidak di backend — backend bekerja, tapi UX menyembunyikan info. **User experience-nya** = "regen gagal" alert generic, tidak tahu kenapa. |
| **Code site** | 3 view: `AssessmentMonitoring.cshtml:406-417`, `AssessmentMonitoringDetail.cshtml:1016-1028`, `ManageAssessment.cshtml:461-470` |
| **Existing handler analysis** | Semua 3 view pakai pattern: |
| | ```js |
| | .then(function (r) { return r.json(); })   // tidak check r.ok |
| | .then(function (data) { if (data.success) {...} else { alert('Error: ' + data.message); } }) |
| | .catch(function (err) { alert('Gagal regenerate token. Periksa koneksi jaringan.'); })  // generic |
| | ``` |
| **Pre-repro Status** | **PROBABLE CONFIRMED (most likely cause)** — Skenario: |
| | 1. Server hit unhandled exception → return 500 dengan body HTML (developer page) atau plain text |
| | 2. `r.json()` reject karena body bukan JSON parseable |
| | 3. Promise reject masuk `.catch()` → alert generic "Gagal regenerate token. Periksa koneksi jaringan." |
| | 4. Admin tidak tahu root cause |
| | Atau: |
| | 1. Server caught exception via `try-catch` line 2469-2474 → return 200 JSON `{success: false, message: "Gagal regenerate token. Silakan coba lagi."}` (juga generic!) |
| | 2. `data.message` BENAR di-display tapi message-nya sendiri generic |
| | Either way: admin lihat "Gagal regenerate token" tanpa diagnostic info → UX bug, masquerading as backend bug. |
| **Why this matches "trigger condition"** | "Status Upcoming + 0 worker started" mungkin bukan trigger backend bug, tapi **trigger reporting** (admin notice + complain). Bug mungkin happen di Status lain juga, tapi terkubur. Kalau backend stable (no exception), trigger condition tidak relevan ke backend. |
| **Final Status** | ⏳ **INCONCLUSIVE pending stacktrace** — Plan 01 must check **(a)** apakah ada exception di server log saat repro, **(b)** apakah HTTP response 200 (success:false) atau 5xx, **(c)** body HTTP response. Predicted: **CONFIRMED — frontend handler masking server message OR generic catch message di backend D-12 fix**. |
| **Fix scope** | D-07/D-08/D-09/D-11 frontend refactor 3 view + D-12 server-side specific exception (better diagnostic). |

### Konklusi Pre-Repro

| Hipotesis | Pre-Repro Prediction | Confidence |
|-----------|----------------------|------------|
| 1. NRE Schedule.Date | RULED OUT | HIGH (struct non-nullable + 10+ working LINQ usage) |
| 2. AuditLog FK | RULED OUT | HIGH (no FK config in DbContext) |
| 3. Concurrency | RULED OUT (untuk trigger condition specific) | MEDIUM (rare, but not impossible) |
| 4. Frontend handler | **CONFIRMED (probable root cause)** | MEDIUM-HIGH (code analysis match user-reported symptom) |

**Anti-bias warning:** Pre-repro analisis bisa SALAH. Plan 01 wajib treat tabel di atas sebagai hipotesis kerja, bukan kesimpulan. **Stacktrace dari log Dev = ground truth**. Kalau stacktrace pinpoint ke Hipotesis 1/2/3, finalize tabel sesuai evidence aktual, BUKAN ke pre-repro prediction.

## Defensive Coverage Plan (D-05) — All 4 Hipotesis

Per D-05 cumulative philosophy, Plan 02 patch coverage **regardless of root cause**:

1. **D-37/D-38 conditional Schedule MinValue guard** — kalau §Data Shape Baseline Query (a) return COUNT > 0
2. **D-06 AuditLog try-catch swallow + LogWarning** — independent dari root cause, prevents future audit infrastructure issues
3. **D-17 explicit `BeginTransactionAsync`** — atomic siblings update + SaveChanges, align dengan controller convention
4. **D-07/D-08/D-09/D-11 frontend handler refactor** — 3 view propagate server message
5. **D-12 server-side specific exception catches** — better diagnostic per exception type
6. **D-20/D-21 extended logging** — telemetry untuk Plan 01 + future debugging
7. **D-25 pre-condition Status block** — Cancelled/Completed reject explanatory
8. **D-33 sibling 0-row guard** — defensive untuk DbContext quirk

## Architecture Patterns

### Pattern 1: Defensive Audit Log try-catch swallow (D-06)

**What:** Wrap `_auditLog.LogAsync` dalam try-catch, audit failure log warning tapi tidak block response success.

**When to use:** Untuk semua action yang sukses path-nya tidak boleh terganggu oleh audit infrastructure failure.

**Example (verified existing pattern):**
```csharp
// Source: Controllers/AssessmentAdminController.cs:2391-2410 (DeletePrePostGroup)
try
{
    await _auditLog.LogAsync(
        dpgUser?.Id ?? "",
        dpgActorName,
        "DeletePrePostGroup",
        $"Deleted Pre-Post group '{groupTitle}' [LinkedGroupId={linkedGroupId}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount}",
        linkedGroupId,
        "AssessmentSession");
}
catch (Exception auditEx)
{
    logger.LogWarning(auditEx, "Audit log write failed for DeletePrePostGroup {LinkedGroupId}", linkedGroupId);
}
```

### Pattern 2: Explicit Transaction Wrap (D-17)

**What:** Wrap multi-row UPDATE + SaveChangesAsync dalam `_context.Database.BeginTransactionAsync()` untuk atomic commit/rollback.

**When to use:** Multi-step write operations yang harus all-or-nothing.

**Example (verified existing pattern):**
```csharp
// Source: Controllers/AssessmentAdminController.cs:1965-1969 (EditAssessment bulk assign)
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    // transaction auto-rollback on dispose
    throw;
}
```

### Pattern 3: Server-side specific exception catches (D-12)

**What:** Split generic `catch (Exception)` jadi specific exception types untuk better diagnostic.

**When to use:** Endpoint yang return JSON dengan `message` ke client; client butuh actionable info.

**Example (proposed Plan 02):**
```csharp
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "RegenerateToken DB error for session {Id}, ...", id, ...);
    return Json(new { success = false, message = "Database error: " + ex.Message });
}
catch (NullReferenceException ex)
{
    _logger.LogError(ex, "RegenerateToken NRE for session {Id}, ...", id, ...);
    return Json(new { success = false, message = "Data assessment tidak lengkap (sibling/Schedule null). Hubungi IT." });
}
catch (Exception ex)
{
    _logger.LogError(ex, "RegenerateToken generic error for session {Id}, ...", id, ...);
    return Json(new { success = false, message = ex.Message });
}
```

### Pattern 4: Frontend fetch with response.ok + JSON-or-text fallback (D-11)

**What:** Check `response.ok` sebelum `r.json()`; kalau bukan ok, fallback ke `r.text()` untuk capture HTML/plain text 5xx body.

**When to use:** Endpoint yang bisa return non-JSON 5xx (developer page, plain text Kestrel error).

**Example (proposed Plan 02):**
```js
fetch(appUrl('/Admin/RegenerateToken/' + id), {
    method: 'POST',
    headers: { 'RequestVerificationToken': token }
})
.then(function (r) {
    if (!r.ok) {
        return r.text().then(function (t) {
            return Promise.reject(t || ('HTTP ' + r.status));
        });
    }
    return r.json();
})
.then(function (data) {
    if (data.success) {
        alert('Token baru: ' + data.token);
        location.reload();
    } else {
        return Promise.reject(data.message || 'Unknown server error');
    }
})
.catch(function (err) {
    alert('Gagal regenerate token: ' + err + '. Coba lagi atau hubungi IT.');
});
```

### Anti-Patterns to Avoid

- **Path-prefix hardcoded** — Phase 312 WR-02. Pakai `appUrl('/Admin/RegenerateToken/' + id)`, JANGAN `'/Admin/RegenerateToken/' + id`. (Existing kode sudah benar, jangan regress.)
- **Generic .catch() menyembunyikan server message** — current state. D-07/D-09 fix.
- **`r.json()` tanpa `r.ok` check** — current state. D-11 fix.
- **Audit log throw block business action** — Phase 306 D-10. D-06 fix.
- **Generic `catch (Exception)` swallow detail** — current state line 2469-2474. D-12 fix.
- **Generic alert "Gagal regenerate token. Periksa koneksi jaringan."** — current state. D-09 propagate server message.
- **Playwright selector substring match** — Phase 312 WR-03. Pakai dedicated fixture title `Phase 314 Token Fixture {Scenario}` exact match (D-08/D-14).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Cryptographic random token generation | Don't reinvent — `Random.Next()` is predictable | `System.Security.Cryptography.RandomNumberGenerator.Create()` (existing `GenerateSecureToken` line 2478-2492) | Already implemented, audited, exclude ambiguous chars |
| Sibling group lookup with case-insensitive Title match | Don't normalize Title to lowercase manually | SQL Server default collation `CI_AS` (case-insensitive accent-sensitive) | Database does it for free; manual normalization causes untranslatable LINQ + breaks existing matching (D-29) |
| `Schedule.Date` matching | Don't strip time manually with `.Year/.Month/.Day` comparison | EF Core translates `DateTime.Date` ke SQL `CAST(... AS date)` | Already proven 10+ times in codebase; idiomatic |
| AuditLog INSERT | Don't compose entity manually inline | `_auditLog.LogAsync(...)` API (existing pattern) | Encapsulates `CreatedAt`, _context.Add, SaveChangesAsync |
| AntiForgeryToken in fetch headers | Don't hardcode CSRF token | `document.querySelector('input[name="__RequestVerificationToken"]').value` (existing pattern di 3 view) | Razor renders token per page; fetch must read live value |
| Confirm dialog with custom modal | Don't build Bootstrap 5 modal for simple yes/no | Native `confirm()` (D-22 existing) | Existing UX consistency, D-22 only adds wording |
| Concurrency detection | Don't add `[Timestamp] RowVersion` schema column | Skip (D-18 last-writer-wins acceptable, deferred to v16.0+) | Schema change out of scope |
| Token uniqueness collision retry | Don't loop until non-collision | Skip (probability ~1/1B per generation, deferred) | Cost > benefit |

**Key insight:** Phase 314 = compose existing patterns + 8 defensive guards (D-05). NO new infrastructure, NO library install, NO schema change.

## Runtime State Inventory

> Phase 314 = bug fix patch + defensive guards. Mostly code edits, satu kemungkinan code-derived data inconsistency (Schedule MinValue legacy, audit-only).

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **Possible:** AssessmentSessions rows dengan `Schedule = '0001-01-01'` (DateTime.MinValue). To-be-validated by Query (a) di §Data Shape Baseline. Plan 01 RESEARCH execute query → Plan 02 conditional D-38 guard. | Code edit (defensive guard) only. **Data cleanup script untuk legacy rows = OUT OF SCOPE Phase 314** (deferred ke Team IT scope per CONTEXT). |
| Stored data | AccessToken values di AssessmentSessions yang akan berubah ke value baru saat regen (existing behavior, no change). | None — preserve existing |
| Live service config | None — RegenerateToken endpoint tidak punya external service config (no n8n workflow, no scheduled task). | None |
| OS-registered state | None — endpoint POST yang dipanggil on-demand by admin. Tidak ada Windows Task Scheduler / pm2 / systemd reference. | None |
| Secrets/env vars | None — token generation pakai `RandomNumberGenerator` (no API key, no env var). | None |
| Build artifacts | None — code edit only, no package rename, no `*.egg-info` analog. | `dotnet build -c Debug` rebuild after patch (CLAUDE.md mandatory verify). |

**Nothing found in category:** Live service config, OS-registered state, Secrets/env vars, Build artifacts (no rename) — verified via grep + project knowledge of bug fix scope.

## Common Pitfalls

### Pitfall 1: Frontend `.then(r => r.json())` reject tanpa `r.ok` check (CURRENT STATE)
**What goes wrong:** Server return 500 dengan HTML body (developer exception page) → `r.json()` throw SyntaxError → Promise reject → masuk `.catch()` generic alert.
**Why it happens:** Existing 3 view tidak check `r.ok` dulu. Asumsi response selalu JSON.
**How to avoid:** D-11 pattern — check `r.ok`, fallback `r.text()`, surface raw text di alert.
**Warning signs:** Admin report "Gagal regenerate token" generic tanpa info specific. Server log ada exception tapi user tidak tahu.

### Pitfall 2: Generic catch (Exception) di backend mask diagnostic info (CURRENT STATE line 2469-2474)
**What goes wrong:** Exception caught → return generic "Gagal regenerate token. Silakan coba lagi." — admin tidak tahu DB error vs NRE vs business rule violation.
**Why it happens:** Defensive coding to avoid leaking internals, tapi terlalu agresif.
**How to avoid:** D-12 split per exception type. DbUpdateException → "Database error: ...". NullReferenceException → "Data tidak lengkap...". Generic Exception → ex.Message (sudah disanitize EF Core).
**Warning signs:** Same as Pitfall 1 — telemetry hilang.

### Pitfall 3: AuditLog throw bubble up rollback business action
**What goes wrong:** `_auditLog.LogAsync` exception (DB issue, validation, etc.) → exception propagates ke outer try → return error 500 → user lihat regen gagal. **Padahal token sudah berhasil di-update di DB**.
**Why it happens:** No try-catch around audit call.
**How to avoid:** D-06 wrap audit call dalam try-catch swallow, log warning.
**Warning signs:** Bug pattern dari Phase 306 explicitly cited di CONTEXT D-06.

### Pitfall 4: Sibling group key mismatch karena case/whitespace di Title (audit-only)
**What goes wrong:** Hipotetis: Title "OJT Operator" vs "OJT  Operator" (double space) atau "ojt operator" (case) → grouping miss → siblings.Count == 0 → silent partial regen.
**Why it happens:** SQL Server default CI_AS collation case-insensitive tapi tidak whitespace-insensitive.
**How to avoid:** D-32 audit-only di RESEARCH §Data Shape Baseline Query (b). Tidak ubah matching logic kalau bukan root cause (D-29 preserved CI_AS default).
**Warning signs:** Sibling count log unexpected (e.g., 1 saat seharusnya 3 untuk PrePost group). D-33 0-row guard catch worst case.

### Pitfall 5: HTMX-based ManageAssessment view event delegation breaks setelah refactor
**What goes wrong:** Phase 311 introduced HTMX lazy load di ManageAssessment.cshtml line 447-471. Handler pakai `document.body.addEventListener('click', ...)` event delegation karena DOM rows di-swap dari partial. Refactor handler harus preserve delegation pattern.
**Why it happens:** Non-delegated click listener tidak fire untuk dynamically-loaded rows.
**How to avoid:** Plan 02 frontend refactor di ManageAssessment.cshtml MUST keep `document.body.addEventListener('click', ...)` + `evt.target.closest('.btn-regenerate-token')` pattern. Tidak switch ke `querySelectorAll().forEach(addEventListener)` (yang dipakai 2 view lain).
**Warning signs:** Klik regen di ManageAssessment row "no-op" setelah tab switch.

### Pitfall 6: Path-prefix bug saat frontend hardcode URL
**What goes wrong:** `'/Admin/RegenerateToken/' + id` bekerja di lokal (`localhost:5277`) tapi gagal di Dev (`/KPB-PortalHC/Admin/RegenerateToken/...`).
**Why it happens:** ASP.NET Core deployment di Dev pakai sub-path `/KPB-PortalHC` (Phase 262-263 fix).
**How to avoid:** Pakai `appUrl()` helper. Existing 3 view sudah benar — Plan 02 jangan regress.
**Warning signs:** Admin report 404 di Dev tapi works di lokal.

## Code Examples

Verified patterns from official sources atau codebase:

### Backend — Defensive RegenerateToken structure (proposed Plan 02)

```csharp
// Source: Composite of CONTEXT D-25/D-17/D-06/D-12/D-20/D-21/D-33 + existing patterns (line 2391-2410, 1965-1969)
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RegenerateToken(int id)
{
    var assessment = await _context.AssessmentSessions.FindAsync(id);
    if (assessment == null)
        return Json(new { success = false, message = "Assessment not found." });

    if (!assessment.IsTokenRequired)
        return Json(new { success = false, message = "This assessment does not require a token." });

    // D-25 NEW: Status block
    if (assessment.Status == "Cancelled" || assessment.Status == "Completed")
        return Json(new { success = false, message = $"Tidak bisa regenerate token untuk assessment yang sudah {assessment.Status}." });

    // D-37/D-38 CONDITIONAL (only if Query (a) confirms legacy data):
    // if (assessment.Schedule == DateTime.MinValue)
    //     return Json(new { success = false, message = "Schedule assessment tidak valid. Hubungi IT." });

    int siblingCount = 0;
    bool hasStarted = false;
    try
    {
        var newToken = GenerateSecureToken();

        var siblings = await _context.AssessmentSessions
            .Where(a => a.Title == assessment.Title
                     && a.Category == assessment.Category
                     && a.Schedule.Date == assessment.Schedule.Date)
            .ToListAsync();

        siblingCount = siblings.Count;
        hasStarted = siblings.Any(s => s.StartedAt != null);  // D-19

        // D-33 NEW: 0-row defensive
        if (siblings.Count == 0)
        {
            _logger.LogWarning("RegenerateToken: empty sibling group for session {Id}, title={Title}", id, assessment.Title);
            return Json(new { success = false, message = "Data assessment tidak konsisten — sibling group tidak ditemukan. Hubungi IT." });
        }

        // D-17 NEW: explicit transaction
        using var tx = await _context.Database.BeginTransactionAsync();
        foreach (var sibling in siblings)
        {
            sibling.AccessToken = newToken;
            sibling.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        // D-06 NEW: audit try-catch swallow
        var regenUser = await _userManager.GetUserAsync(User);
        var regenActorName = string.IsNullOrWhiteSpace(regenUser?.NIP)
            ? (regenUser?.FullName ?? "Unknown")
            : $"{regenUser.NIP} - {regenUser.FullName}";
        try
        {
            await _auditLog.LogAsync(
                regenUser?.Id ?? "",
                regenActorName,
                "RegenerateToken",
                $"Regenerated access token for '{assessment.Title}' ({assessment.Category}, {assessment.Schedule:yyyy-MM-dd}) — {siblings.Count} sibling(s) updated",
                id,
                "AssessmentSession");
        }
        catch (Exception auditEx)
        {
            _logger.LogWarning(auditEx, "Audit log write failed for RegenerateToken session {Id}", id);
        }

        // D-21 NEW: success info log
        _logger.LogInformation(
            "RegenerateToken success for session {Id}, {Count} siblings updated by {ActorName}",
            id, siblings.Count, regenActorName);

        return Json(new { success = true, token = newToken, message = "Token regenerated successfully." });
    }
    // D-12 NEW: specific exception catches
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex,
            "RegenerateToken DB error for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
            id, assessment.Status, hasStarted, siblingCount, assessment.IsTokenRequired);
        return Json(new { success = false, message = "Database error: " + ex.Message });
    }
    catch (NullReferenceException ex)
    {
        _logger.LogError(ex,
            "RegenerateToken NRE for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
            id, assessment.Status, hasStarted, siblingCount, assessment.IsTokenRequired);
        return Json(new { success = false, message = "Data assessment tidak lengkap (sibling/Schedule null). Hubungi IT." });
    }
    catch (Exception ex)
    {
        // D-20 extended structured logging
        _logger.LogError(ex,
            "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
            id, assessment.Status, hasStarted, siblingCount, assessment.IsTokenRequired);
        return Json(new { success = false, message = ex.Message });
    }
}
```

### Frontend — Refactored handler (3 view) — D-07/D-11 pattern

```js
// Source: D-07/D-11 pattern, replicated dengan adjustments per view (button-list / spinner / HTMX-delegation)
// Pattern di AssessmentMonitoring.cshtml line 396-419 (querySelectorAll + forEach):
document.querySelectorAll('.btn-regenerate-token').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var id = this.getAttribute('data-id');
        var startedCount = parseInt(this.getAttribute('data-started-count') || '0', 10);  // D-23 inline data attr
        var status = this.getAttribute('data-status') || '';
        var msg = 'Regenerate token untuk assessment ini?';
        if (status === 'Open' && startedCount > 0) {
            msg = 'PERINGATAN: ' + startedCount + ' worker sudah masuk ujian. Regenerate token akan invalidate session token mereka — mereka harus login ulang dengan token baru. Jawaban yang sudah disimpan tidak hilang. Lanjutkan?';
        }
        if (!confirm(msg)) return;

        fetch(appUrl('/Admin/RegenerateToken/' + id), {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(function (r) {
            if (!r.ok) {
                return r.text().then(function (t) {
                    return Promise.reject(t || ('HTTP ' + r.status));
                });
            }
            return r.json();
        })
        .then(function (data) {
            if (data.success) {
                alert('Token baru: ' + data.token);
                location.reload();
            } else {
                return Promise.reject(data.message || 'Unknown server error');
            }
        })
        .catch(function (err) {
            alert('Gagal regenerate token: ' + err + '. Coba lagi atau hubungi IT.');
        });
    });
});
```

```js
// Pattern di ManageAssessment.cshtml line 447-471 (HTMX-friendly event delegation, async/await):
document.body.addEventListener('click', function(evt) {
    var btn = evt.target.closest('.btn-regenerate-token');
    if (!btn) return;
    (async function() {
        var id = btn.dataset.id;
        var startedCount = parseInt(btn.dataset.startedCount || '0', 10);
        var status = btn.dataset.status || '';
        var msg = 'Regenerate token untuk assessment ini?';
        if (status === 'Open' && startedCount > 0) {
            msg = 'PERINGATAN: ' + startedCount + ' worker sudah masuk ujian...';
        }
        if (!confirm(msg)) return;

        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
            || document.querySelector('[name="__RequestVerificationToken"]')?.value;
        try {
            var res = await fetch(appUrl('/Admin/RegenerateToken/' + id), {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: '__RequestVerificationToken=' + encodeURIComponent(token)
            });
            if (!res.ok) {
                var text = await res.text();
                throw new Error(text || ('HTTP ' + res.status));
            }
            var data = await res.json();
            if (data.success) {
                alert('Token baru: ' + data.token);
            } else {
                throw new Error(data.message || 'Unknown server error');
            }
        } catch (e) {
            alert('Gagal regenerate token: ' + e.message + '. Coba lagi atau hubungi IT.');
        }
    })();
});
```

## D-24 Open Question — Apakah Token Regen Invalidate Worker Session?

**Klaim D-24:** Skenario test #3 (Open running) verify worker dengan token lama dapat error invalid saat next request.

**Investigation:**

| Aspek | Evidence |
|-------|----------|
| Token usage di code | `Controllers/CMPController.cs:804` — `if (string.IsNullOrEmpty(token) || assessment.AccessToken != token.ToUpper()) return Json(...)` [VERIFIED] |
| Token consumption point | **Login flow only** — token verified pada **`VerifyToken` action saat masuk StartExam pertama kali**, kemudian `TempData[$"TokenVerified_{assessment.Id}"] = true` (line 800, 810) [VERIFIED CMPController.cs:797-811] |
| Session establishment | `StartExam` (line 815-) — setelah token verify, ASP.NET Core Identity cookie auth + `TempData` flag persist session. Page subsequent (submit answer, navigate question) **tidak re-check token** [VERIFIED via grep: `AccessToken` muncul di Controller hanya untuk admin CRUD + login token verify, bukan di SubmitExam path] |
| Comment di Models/AssessmentSession.cs:80-82 | `"Token hanya mengontrol akses masuk, bukan identitas peserta (identity ditangani ASP.NET Core Identity)."` [VERIFIED] |

**Konklusi (HIGH confidence):** Token regen **TIDAK invalidate active worker session**. Worker yang sudah `StartExam` (TempData flag set + Identity cookie) bisa lanjut submit jawaban tanpa re-verify token. Token only gate-keeps initial entry. Token baru hanya berlaku untuk worker yang **belum** klik StartExam.

**Implikasi untuk D-24 test scenario:**
- Skenario test #3 (Open running) **TIDAK BISA** verify "worker dengan token lama dapat error invalid saat next request" sebagaimana klaim asumsi D-24, **karena tidak ada next-request token check**.
- Worker yang sudah masuk ujian (`StartedAt != null`) tidak terdampak token regen — bisa lanjut sampai submit/timeout.
- D-23 wording "akan invalidate session token mereka — mereka harus login ulang dengan token baru" **incorrect** — session sudah established via cookie, tidak invalidate.
- Worker yang belum masuk + dapat token lama dari sticker/whatsapp → akan ditolak login dengan "Token tidak valid" (line 806).

**Recommendation untuk Planner:**

1. **D-23 wording revision (NEW):** Ubah warning text jadi:
   > "PERINGATAN: {N} worker sudah masuk ujian. Regenerate token TIDAK akan invalidate sesi mereka yang sudah berjalan — mereka tetap bisa lanjut. Tapi worker lain yang belum masuk dan punya token lama akan ditolak login. Lanjutkan?"

2. **D-24 test scenario adjustment:** Skenario #3 (Open running) verify (a) admin success regen + (b) AccessToken DB berubah + (c) **AuditLog row exists** — DROP klaim "(c) worker dengan token lama dapat error invalid saat next request". Replace dengan: **(c) worker yang sudah `StartedAt != null` tetap bisa akses StartExam (no logout/redirect)**.

3. **Open enhancement (deferred):** Server-side cookie/session invalidate on token regen — lihat Deferred Ideas. Kalau Pertamina butuh strict invalidation, separate phase di v16.0+.

## Data Shape Baseline (D-39 — 5 Comprehensive DB Sample Queries)

> Plan 01 wajib jalankan 5 query ini di DB Dev (read-only via Team IT atau via DB lokal yang di-snapshot dari Dev). Hasil masuk ke RESEARCH.md update saat repro selesai. Output query → drives D-37/D-38 conditional defensive guard di Plan 02.

### Query (a) — Schedule MinValue legacy validation (D-37/D-38)

```sql
-- Validate D-37: apakah ada legacy data dengan Schedule = '0001-01-01' (DateTime.MinValue)?
SELECT COUNT(*) AS MinValueRowCount
FROM AssessmentSessions
WHERE Schedule = '0001-01-01';

-- Optional drilldown jika count > 0:
SELECT TOP 10 Id, Title, Category, Schedule, Status, IsTokenRequired, CreatedAt
FROM AssessmentSessions
WHERE Schedule = '0001-01-01'
ORDER BY CreatedAt DESC;
```

**Decision rule:**
- `MinValueRowCount = 0` → SKIP D-38 guard (CreateAssessment datepicker UI prevents)
- `MinValueRowCount > 0` → APPLY D-38 guard di Plan 02 + suggest data cleanup script untuk Team IT (deferred Phase 314)

### Query (b) — Title duplicate / case / whitespace audit (D-29 / D-32)

```sql
-- Sample 10 row dengan duplicate Title (case/whitespace test)
WITH TitleGroups AS (
    SELECT Title, COUNT(*) AS RowCount
    FROM AssessmentSessions
    GROUP BY Title
    HAVING COUNT(*) > 1
)
SELECT TOP 10 a.Id, a.Title, a.Category, a.Schedule, a.Status, LEN(a.Title) AS TitleLen,
       CHARINDEX('  ', a.Title) AS DoubleSpaceIdx
FROM AssessmentSessions a
INNER JOIN TitleGroups tg ON a.Title = tg.Title
ORDER BY a.Title, a.CreatedAt;

-- Optional: detect case/whitespace variants (CI_AS may mask):
SELECT Title, LOWER(Title) AS LowerTitle, REPLACE(Title, ' ', '_') AS NoSpaceTitle, COUNT(*) AS Rows
FROM AssessmentSessions
GROUP BY Title, LOWER(Title), REPLACE(Title, ' ', '_')
HAVING COUNT(*) > 1
ORDER BY COUNT(*) DESC;
```

**Decision rule:**
- Hasil duplicate **expected** untuk PrePost pair (Pre+Post sama Title) atau multi-user same Title (peserta berbeda) — TIDAK abnormal
- Kalau ada Title dengan double-space atau case-only diff → audit-only finding, TIDAK ubah matching logic per D-32 (CI_AS preserves)

### Query (c) — Category null/empty audit (D-31)

```sql
-- Validate D-31: apakah ada legacy data dengan Category NULL atau empty string?
SELECT
    SUM(CASE WHEN Category IS NULL THEN 1 ELSE 0 END) AS NullCount,
    SUM(CASE WHEN Category = '' THEN 1 ELSE 0 END) AS EmptyStringCount,
    SUM(CASE WHEN Category IS NOT NULL AND Category <> '' THEN 1 ELSE 0 END) AS ValidCount
FROM AssessmentSessions;

-- Drilldown jika ada:
SELECT TOP 10 Id, Title, Category, Schedule, Status, CreatedAt
FROM AssessmentSessions
WHERE Category IS NULL OR Category = ''
ORDER BY CreatedAt DESC;
```

**Decision rule:**
- `NullCount = 0 AND EmptyStringCount = 0` → SKIP Category null-coalesce (existing matching `a.Category == assessment.Category` safe)
- Kalau ada → audit-only di RESEARCH (D-32). Defer Category null-coalesce defensive ke Deferred Ideas

### Query (d) — Trigger condition distribution (Status='Upcoming' + IsTokenRequired=1 + 0 worker started)

```sql
-- Status distribution untuk trigger condition
SELECT
    Status,
    COUNT(*) AS TotalRows,
    SUM(CASE WHEN IsTokenRequired = 1 THEN 1 ELSE 0 END) AS TokenRequiredRows,
    SUM(CASE WHEN IsTokenRequired = 1 AND StartedAt IS NULL THEN 1 ELSE 0 END) AS TriggerConditionRows
FROM AssessmentSessions
GROUP BY Status
ORDER BY TotalRows DESC;

-- Sample 10 row yang persis matching trigger condition (untuk repro candidate selection)
SELECT TOP 10 Id, Title, Category, Schedule, Status, IsTokenRequired, StartedAt, CreatedAt
FROM AssessmentSessions
WHERE Status = 'Upcoming'
  AND IsTokenRequired = 1
  AND StartedAt IS NULL
ORDER BY CreatedAt DESC;
```

**Decision rule:**
- Plan 01 pilih 1 row dari hasil query ini sebagai **repro candidate** (kalau ada di Dev). Kalau kosong, buat fixture sendiri sesuai D-01.
- Distribution menunjukkan apakah trigger condition rare atau common di production data.

### Query (e) — Sibling group size statistics (D-32)

```sql
-- Sibling group size statistics (avg/min/max per group key)
WITH GroupedSiblings AS (
    SELECT
        Title,
        Category,
        CAST(Schedule AS DATE) AS ScheduleDate,
        COUNT(*) AS SiblingCount
    FROM AssessmentSessions
    GROUP BY Title, Category, CAST(Schedule AS DATE)
)
SELECT
    COUNT(*) AS TotalGroups,
    MIN(SiblingCount) AS MinSiblings,
    MAX(SiblingCount) AS MaxSiblings,
    AVG(CAST(SiblingCount AS FLOAT)) AS AvgSiblings,
    SUM(CASE WHEN SiblingCount = 1 THEN 1 ELSE 0 END) AS GroupsWith1Sibling,
    SUM(CASE WHEN SiblingCount = 2 THEN 1 ELSE 0 END) AS GroupsWith2Siblings,
    SUM(CASE WHEN SiblingCount BETWEEN 3 AND 10 THEN 1 ELSE 0 END) AS GroupsWith3to10,
    SUM(CASE WHEN SiblingCount > 10 THEN 1 ELSE 0 END) AS GroupsWithMoreThan10
FROM GroupedSiblings;

-- Distribution histogram untuk trigger condition specifically
WITH TriggerSiblings AS (
    SELECT
        Title,
        Category,
        CAST(Schedule AS DATE) AS ScheduleDate,
        COUNT(*) AS SiblingCount
    FROM AssessmentSessions
    WHERE Status = 'Upcoming' AND IsTokenRequired = 1
    GROUP BY Title, Category, CAST(Schedule AS DATE)
)
SELECT SiblingCount, COUNT(*) AS GroupCount
FROM TriggerSiblings
GROUP BY SiblingCount
ORDER BY SiblingCount;
```

**Decision rule:**
- `MinSiblings = 1` → 1-sibling case real (Online single without PrePost pair) — D-34 valid, no extra test needed
- `MinSiblings = 0` → indicate data corruption (impossible by definition: assessment itself matches its own group) — D-33 0-row guard justified
- Distribution memberi context untuk D-22/D-23 wording (worst case untuk N worker active berbeda per group size)

**Result template (untuk update by Plan 01):**

```markdown
| Query | Result | Decision |
|-------|--------|----------|
| (a) Schedule MinValue COUNT | TBD by Plan 01 | TBD: SKIP/APPLY D-38 |
| (b) Title duplicates count | TBD | Audit-only finding |
| (c) Category null/empty count | TBD | Audit-only |
| (d) Trigger condition rows | TBD | Repro candidate ID = TBD |
| (e) Min/Avg/Max sibling | TBD | D-33 guard validated |
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `catch (Exception ex)` generic + return generic message | `catch (DbUpdateException) / NullReferenceException / Exception` split + specific message (D-12) | Phase 314 Plan 02 | Better diagnostic, admin tahu root cause |
| `.then(r => r.json())` tanpa `r.ok` check | `if (!r.ok) return r.text().then(...reject)` (D-11) | Phase 314 Plan 02 | Capture HTML 5xx response sebagai diagnostic, bukan generic catch |
| Generic alert "Gagal regenerate token. Periksa koneksi jaringan." | `'Gagal regenerate token: ' + serverMessage + '. Coba lagi atau hubungi IT.'` (D-09) | Phase 314 Plan 02 | Server message propagate end-to-end |
| Single-tier audit log call | Try-catch swallow + LogWarning fallback (D-06, Phase 306 D-10) | v15.0 milestone | Audit infrastructure failure tidak block business action |
| Implicit transaction (single SaveChangesAsync) | Explicit `BeginTransactionAsync` wrap (D-17) | Phase 314 Plan 02 | Align dengan controller convention (line 1965, 2051, 2195) |
| Generic `[Authorize(Roles="Admin, HC")]` | Same + body-method status guard (D-25, Phase 312 pattern) | v15.0 Wave 5 | Prevent admin accidentally regen finished assessment |

**Deprecated/outdated:** None — Phase 314 = pure additive defensive patch, no deprecation.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | EF Core translates `a.Schedule.Date == assessment.Schedule.Date` ke SQL `CAST(... AS date)` server-side, bukan client-side iteration | §Hipotesis 1 NRE | Kalau EF Core Phase 297 upgrade silently break translation (e.g., evaluate client-side), cross-row scan jadi NRE-prone. Mitigation: sudah verified pattern berjalan 10+ kali di codebase tanpa report bug (Phase 297-313 stable). [VERIFIED via grep] |
| A2 | DB Dev `AspNetUsers` table tidak punya FK constraint dari `AuditLogs.ActorUserId` (matching DbContext config absence) | §Hipotesis 2 AuditLog FK | Kalau DB Dev punya FK manual (bukan via migration) → empty string `""` violates → DbUpdateException. Mitigation: Plan 01 verify via SQL `SELECT * FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID('AspNetUsers')` di Dev. [ASSUMED based on DbContext config] |
| A3 | Pertamina admin role concurrency rare (1-2 admin per shift, simultaneous regen click-window milliseconds) | §Hipotesis 3 Concurrency | Kalau Pertamina actually punya 5+ HC role concurrent + sering regen, race condition feasible. Mitigation: D-17 transaction wrap regardless. [ASSUMED based on org context] |
| A4 | Frontend handler bug = paling likely root cause (4 hipotesis prioritization) | §Hipotesis 4 + Konklusi Pre-Repro | Kalau stacktrace pinpoint backend exception lain (e.g., timeout, deadlock, EF Core internal), prediction salah. Plan 01 must not commit to frontend hypothesis pre-repro. [ASSUMED based on code analysis] |
| A5 | Worker session tidak invalidated by token regen — token only login-gate | §D-24 Open Question | Kalau ada middleware/ filter yang re-check `AccessToken` per request (yang tidak ke-find via grep `AccessToken`), asumsi salah. Mitigation: Plan 01 verify via code review SubmitExam path. [VERIFIED via grep `AccessToken` di CMPController.cs only line 804 saat VerifyToken; submit/answer routes tidak re-check] |
| A6 | Test fixture untuk Playwright = `temporary + local-only` per CLAUDE.md Seed Workflow | §Validation Architecture | Kalau fixture jadi recurring CI test data, butuh promote ke `permanent + prod-required` di `Data/SeedData.cs`. Plan 02 validate at task-end. [ASSUMED based on E2E testing best practice + CLAUDE.md classification] |
| A7 | `dotnet build -c Debug` rebuild pasca-patch pass dengan baseline 0 errors, 92 warnings | §Project Constraints | Kalau Phase 311-313 introduce regression baru, baseline shift. Mitigation: Plan 02 pre-task verify baseline + post-task delta. [ASSUMED based on Phase 312/313 STATE.md] |

**Risk mitigation summary:** A1, A2, A5 sudah cross-checked via grep + DbContext config + 10+ usage pattern (HIGH confidence). A3, A4, A6, A7 — assumptions yang BISA salah, di-flag untuk Plan 01 verify saat repro.

## Open Questions (RESOLVED)

> **Status (2026-05-08):** All 5 questions RESOLVED — either scheduled into Plan 01 tasks dengan acceptance criteria explicit, atau resolved via existing finding (D-24 grep, PATTERNS Pitfall 5, D-01 admin fallback). No open blockers.

1. **Stacktrace dari log Dev — 4 hipotesis mana yang CONFIRMED?** — **RESOLVED** — dijadwalkan ke **Plan 01 Task 2** (D-02 capture stacktrace, output ke RESEARCH.md `## Stacktrace Evidence` section dengan acceptance criteria `grep -q "^## Stacktrace Evidence"`). Plan 02 Task 1 verify pre-condition `grep -q "## Root Cause (Finalized" RESEARCH.md` sebelum patch.
   - What we know: Pre-repro analisis prediksi Frontend handler (Hipotesis 4) paling likely
   - What's unclear: Real exception type + stack trace
   - Recommendation: Plan 01 langkah pertama (D-02) — capture log Dev. Tools: Kestrel log file di server, atau Application Insights kalau ter-config.

2. **DB Dev legacy data — apakah Schedule MinValue rows exist?** — **RESOLVED** — dijadwalkan ke **Plan 01 Task 3** (5 SQL queries D-39 dengan output ke RESEARCH.md `## Data Shape Baseline` section). Query (a) result drives D-37/D-38 conditional inclusion di Plan 02 Task 1 (verify via `grep "MinValueCount" RESEARCH.md`).
   - What we know: CreateAssessment datepicker UI prevents at create time
   - What's unclear: Apakah ada rows pre-Phase 192 atau dari migration data legacy
   - Recommendation: Plan 01 jalankan Query (a) di §Data Shape Baseline → drives D-37/D-38

3. **Worker session invalidation behavior post-regen — pertahankan atau enhancement future?** — **RESOLVED** via D-24 finding (HIGH confidence) — token TIDAK invalidate active worker session. Cookie-based ASP.NET Core Identity ticket persists sampai logout/expiry; token verified hanya di login flow `CMPController.VerifyToken` line 804 (grep `AccessToken` confirms tidak ada middleware re-check per request). D-23 wording direvisi accordingly di Plan 01 Task 5. Strict invalidation = separate enhancement v16.0+ (Deferred Ideas).
   - What we know: Current behavior = NOT invalidated (token login-gate only)
   - What's unclear: Apakah Pertamina butuh strict invalidation (force logout) untuk audit/security
   - Recommendation: D-23 wording revision Phase 314 (dokumentasikan current behavior accurate). Strict invalidation = separate enhancement v16.0+ (sudah di Deferred Ideas).

4. **HTMX-based ManageAssessment view — partial render preserves event listener?** — **RESOLVED** via PATTERNS.md Pitfall 5 + Plan 02 Task 4 (preserve `document.body.addEventListener` delegation pattern karena body element stable across HTMX swap). Manual UAT step di Plan 02 Task 7 verify regen handler fire setelah tab switch + filter apply (acceptance: dialog confirm muncul, fetch dispatched).
   - What we know: Phase 311 implemented HTMX lazy load untuk 3 tab (Assessment/Training/History)
   - What's unclear: Apakah tab swap reset `document.body.addEventListener` atau preserve (event delegation pattern depends on body element being stable)
   - Recommendation: Plan 02 manual UAT verify regen handler fire setelah tab switch + setelah filter apply (HTMX swap)

5. **Repro at Dev — admin@pertamina.com peserta sebagai dirinya sendiri, valid?** — **RESOLVED** via D-01 fallback strategy — admin@pertamina.com Admin role allow assign Admin sebagai peserta (verified tidak ada role-based restrict di CreateAssessment Authorize policy, hanya `[Authorize(Roles = "Admin, HC")]`). Fallback: kalau policy reject saat repro, pakai 1 user Coachee fixture yang tidak StartExam (Plan 01 Task 1 instructions cover both paths).
   - What we know: D-01 user explicit: "buat assessmentnya sendiri"
   - What's unclear: Apakah Authorize policy di CreateAssessment allow Admin assign Admin sebagai peserta (vs peserta=Coachee role only)
   - Recommendation: Plan 01 verify saat repro. Fallback: pakai 1 user Coachee fixture yang tidak StartExam.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | Build + run lokal | ✓ | 8.0.x (assumed Phase 311-313 baseline) | — |
| SQL Server Express (`localhost\SQLEXPRESS`) | DB lokal `HcPortalDB_Dev` | ✓ | (existing) | — |
| `sqlcmd` | Snapshot/restore DB lokal (Seed Workflow) | ✓ (asumsi installed dengan SQL Server Express) | (existing) | — |
| Server Dev `http://10.55.3.3/KPB-PortalHC` | D-01 repro environment | TBD by Plan 01 | (Team IT managed) | Replicate trigger condition di lokal kalau Dev offline |
| Server Dev log access (Kestrel/Serilog/file/AppInsights) | D-02 capture exception | TBD by Plan 01 | (Team IT managed) | Iterate 4 hipotesis manual via repro lokal |
| `admin@pertamina.com` Dev account | D-01 login | ✓ | (existing in Dev seed) | — |
| Playwright test runner | E2E smoke 3 skenario (Plan 02) | ✓ | existing in `tests/` | — |

**Missing dependencies with no fallback:** None — semua infra ada atau ada fallback.

**Missing dependencies with fallback:**
- **Server Dev log access** — kalau Team IT tidak provide tail-able log, Plan 01 fallback ke iterate 4 hipotesis manual via repro lokal (D-02 fallback explicit di CONTEXT).
- **Dev environment access** — kalau Dev offline saat repro window, Plan 01 fallback ke replicate trigger condition di lokal (`HcPortalDB_Dev` snapshot from Dev). Per CLAUDE.md "Step 2 — Reproduce di lokal".

## Validation Architecture

> Phase 314 = bug fix + smoke test. Nyquist validation enabled (default).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright (TypeScript) — existing |
| Config file | `tests/playwright.config.ts` (existing) |
| Quick run command | `cd tests && npx playwright test e2e/admin-assessment-token.spec.ts --reporter=list` |
| Full suite command | `cd tests && npx playwright test --reporter=list` |
| Build verification | `dotnet build -c Debug` (CLAUDE.md mandatory) |
| Local run verification | `dotnet run` + manual smoke @ `http://localhost:5277` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| TKN-01 | Status Upcoming + IsTokenRequired=true + 0 worker started → regen success | E2E (skenario #1) | `npx playwright test admin-assessment-token --grep "Upcoming0"` | ❌ Wave 0 (D-16 NEW file) |
| TKN-01 | Status Upcoming + IsTokenRequired=true + sebagian worker started → regen success | E2E (skenario #2) | `npx playwright test admin-assessment-token --grep "UpcomingPartial"` | ❌ Wave 0 |
| TKN-01 | Status Open + worker running → regen success + warning dialog (D-22/D-23) | E2E (skenario #3) | `npx playwright test admin-assessment-token --grep "OpenRunning"` | ❌ Wave 0 |
| TKN-01 | Status Cancelled/Completed → regen blocked dengan TempData explanatory (D-25) | E2E or manual smoke | `npx playwright test admin-assessment-token --grep "StatusBlocked"` (optional) | Optional — bisa cover via manual UAT |
| TKN-01 | AccessToken DB berubah ke value baru (D-15a) | E2E assertion | sqlcmd query post-test | Same file |
| TKN-01 | Sibling sessions ter-update (D-15b) | E2E assertion | sqlcmd query | Same file |
| TKN-01 | AuditLogs row `ActionType='RegenerateToken'` exists (D-15c) | E2E assertion | sqlcmd query | Same file |
| TKN-01 | UI alert `'Token baru: {6-char}'` muncul (D-15d) | E2E UI assertion | Playwright `page.on('dialog')` | Same file |
| TKN-01 | Frontend `.catch()` propagate server message (D-07/D-09) | Manual UAT (Plan 02 checkpoint) | Manual: trigger 500 via DB lock atau invalid Schedule | Manual UAT step di `314-UAT.md` |
| TKN-01 | Build pass | CI check | `dotnet build -c Debug` | Existing |

### Sampling Rate
- **Per task commit:** `dotnet build -c Debug` (must pass 0 errors, ≤ 92 warnings baseline)
- **Per wave merge:** `npx playwright test admin-assessment-token --reporter=list` (3 skenario pass)
- **Phase gate:** Full suite green + manual UAT 5-step Bahasa Indonesia signed-off di `314-UAT.md` before `/gsd-verify-work`

### Wave 0 Gaps

- [ ] `tests/e2e/admin-assessment-token.spec.ts` — NEW file (D-16) covering 3 skenario per D-13/D-14/D-15
- [ ] `tests/e2e/helpers/wizardSelectors.ts` — optional extend dengan `tokenRegen` namespace (Claude's discretion D-08 inline `data-started-count`)
- [ ] `.planning/phases/314-fix-regenerate-token-untuk-status-upcoming/314-UAT.md` — manual UAT 5-step BI (mirror Phase 308/310/312 pattern)

### Seeding Strategy (CLAUDE.md Seed Workflow Compliance)

D-14 seeding via UI Admin setup hook → 3 fixture title `Phase 314 Token Fixture {Upcoming0|UpcomingPartial|OpenRunning}`.

**Klasifikasi:** `temporary + local-only` per CLAUDE.md §Seed Data Workflow → Klasifikasi.

**Mandatory steps (Plan 02 Wave 0 task atau test setup hook):**
1. **Pre-seed:** `sqlcmd -S "localhost\SQLEXPRESS" -E -Q "BACKUP DATABASE HcPortalDB_Dev TO DISK='C:\Temp\HcPortalDB_Dev.<timestamp>.bak' WITH INIT"` (per `docs/SEED_WORKFLOW.md` §5.1)
2. **Catat di `docs/SEED_JOURNAL.md`:** entry `temporary + local-only`, tujuan `Phase 314 Playwright fixture E2E regen token`, dampak `Users(1), Assessments(3), Sessions(3+)`. Status `active`.
3. **Insert via Playwright UI flow:** login admin → 3 kali navigate `/Admin/CreateAssessment` → submit dengan title fixture pattern. UpcomingPartial via DB UPDATE manual `StartedAt = GETUTCDATE()` untuk subset peserta (`tests/seed-helpers/start-session.sql` snippet).
4. **Run E2E test.**
5. **Post-test:** `sqlcmd -S "localhost\SQLEXPRESS" -E -Q "USE master; ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE HcPortalDB_Dev FROM DISK='C:\Temp\HcPortalDB_Dev.<timestamp>.bak' WITH REPLACE; ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;"` (per §5.2)
6. **Update journal:** status `cleaned`, hapus `.bak` file.

**Alternative (lighter weight):** Playwright `afterEach` cleanup via API — DELETE assessments matching fixture title prefix. **Tradeoff:** lebih cepat tapi tidak guaranteed clean state kalau test crash mid-flow. **Recommend: snapshot/restore = source of truth, afterEach API cleanup = optimization.**

**Plan Phase 314 task tambahan (Plan 02 Wave 0):**
- Task: "Setup seed fixture journal entry + snapshot DB"
- Task: "Restore DB + close journal" (post-test)

## Security Domain

> security_enforcement default = enabled. Phase 314 = bug fix endpoint admin-only. Coverage minimal tapi mandatory.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity (existing). Endpoint protected by `[Authorize(Roles="Admin, HC")]` (D-26 preserve) |
| V3 Session Management | yes | ASP.NET Core Identity cookie (existing). Token regen TIDAK invalidate session (per D-24 finding) — documented as accepted behavior |
| V4 Access Control | yes | Body method authorization sudah cukup (D-26). Status block (D-25) prevent admin accidentally regen finished/cancelled assessment |
| V5 Input Validation | yes | `int id` route param model-bind validated by ASP.NET Core; FindAsync(id) return null check existing |
| V6 Cryptography | yes | `RandomNumberGenerator.Create()` existing — JANGAN hand-roll random. (D-undeferred: token uniqueness collision retry deferred — probability ~1/1B, acceptable) |
| V7 Error Handling | yes | D-12 specific exception types, D-20 structured logging, sanitize ex.Message via EF Core default (no raw connection string leak) |
| V11 Business Logic | yes | D-25 status block prevents regen Cancelled/Completed (business rule). D-22 warning dialog for active worker (UX safeguard). |

### Known Threat Patterns for ASP.NET Core MVC Controller

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| CSRF (cross-site request forgery) | Tampering | `[ValidateAntiForgeryToken]` attribute (existing line 2426) — preserve |
| Broken access control (HC bypass status guard) | Elevation of Privilege | `[Authorize(Roles="Admin, HC")]` + body method status guard (D-25) |
| Information disclosure via raw exception message | Information Disclosure | D-12 sanitize: DbUpdateException → "Database error: " + ex.Message (EF Core sanitizes connection strings); generic Exception → ex.Message (acceptable for admin role technical context) |
| Token predictability | Spoofing | `RandomNumberGenerator` cryptographic PRNG (existing) — JANGAN regress ke `Random` |
| Audit log tampering | Repudiation | AuditLog entity = INSERT-only (no UPDATE/DELETE actions exposed). D-06 try-catch swallow tidak compromise integrity (failure = LogWarning, success path complete) |
| Concurrent regen race | Tampering (last-writer-wins) | D-17 transaction wrap. D-18 deferred RowVersion (acceptable per CONTEXT) |
| Path-prefix injection | Tampering | `appUrl()` helper (Phase 312 WR-02 lesson) — preserve di 3 view |

**Compliance check (CLAUDE.md / project security):**
- Bahasa Indonesia text user-facing: D-09/D-10/D-22/D-23/D-25 wording in BI ✓
- No new credential / secret introduction ✓
- No DB schema change (no migration security review needed) ✓
- Cascade behavior tidak diubah (no FK weakening) ✓

## Sources

### Primary (HIGH confidence)
- `Controllers/AssessmentAdminController.cs:2427-2475` (RegenerateToken endpoint) — direct read [VERIFIED]
- `Controllers/AssessmentAdminController.cs:1965-1969, 2051, 2195` (BeginTransactionAsync convention) — grep [VERIFIED]
- `Controllers/AssessmentAdminController.cs:2391-2410` (DeletePrePostGroup audit try-catch pattern) — direct read [VERIFIED]
- `Controllers/CMPController.cs:797-811` (token verify single-shot at login) — direct read [VERIFIED — confirms D-24 finding]
- `Models/AssessmentSession.cs` (full schema with Schedule non-nullable DateTime, AccessToken comment) — direct read [VERIFIED]
- `Models/AuditLog.cs` (ActorUserId Required string, no FK attribute) — direct read [VERIFIED]
- `Data/ApplicationDbContext.cs:488-494` (AuditLog config: HasIndex only, no HasOne FK) — direct read [VERIFIED]
- `Services/AuditLogService.cs:1-44` (LogAsync API) — direct read [VERIFIED]
- `Views/Admin/AssessmentMonitoring.cshtml:380-422` — direct read [VERIFIED]
- `Views/Admin/AssessmentMonitoringDetail.cshtml:990-1035` — direct read [VERIFIED]
- `Views/Admin/ManageAssessment.cshtml:440-471` — direct read [VERIFIED]
- `tests/helpers/accounts.ts` (admin fixture) — direct read [VERIFIED]
- `tests/e2e/assessment.spec.ts` (Playwright test pattern) — direct read [VERIFIED]
- `tests/e2e/helpers/wizardSelectors.ts` (selector helper pattern) — direct read [VERIFIED]
- `docs/DEV_WORKFLOW.md` (lokal → Dev → Prod workflow) — direct read [VERIFIED]
- `docs/SEED_WORKFLOW.md` (snapshot/restore SQL Server, journal format) — direct read [VERIFIED]
- `CLAUDE.md` (project instructions, dev workflow + seed workflow) — direct read [VERIFIED]
- `.planning/REQUIREMENTS.md:63` (TKN-01 acceptance) — direct read [VERIFIED]
- `.planning/ROADMAP.md:254-271` (Phase 314 SC 1-6) — direct read [VERIFIED]
- `.planning/STATE.md` (project state) — direct read [VERIFIED]
- `.planning/phases/313-block-manual-submit-saat-waktu-habis/313-CONTEXT.md` (carry-forward patterns) — direct read [VERIFIED]
- `.planning/phases/312-admin-full-delete-assessment-room/312-CONTEXT.md` + `312-RESEARCH.md` (AuditLog Blocked pattern, fixture title pattern) — direct read [VERIFIED]
- Grep `Schedule.Date` (10+ usage di AssessmentAdminController.cs) — confirms EF Core LINQ translation pattern [VERIFIED]
- Grep `AccessToken` di CMPController (only line 804 VerifyToken) — confirms D-24 finding [VERIFIED]
- Grep `BeginTransactionAsync` (15+ usage codebase) — convention preserved [VERIFIED]

### Secondary (MEDIUM confidence)
- `.planning/phases/306-score-editable-per-question-type/306-01-PLAN.md` (audit try-catch + LogWarning fallback referenced by D-06) — direct read [VERIFIED via Plan content]
- EF Core 8.x LINQ translation behavior for `DateTime.Date` — based on multiple working usage in codebase + standard EF Core 8 documentation knowledge [INFERRED from working patterns]

### Tertiary (LOW confidence)
- Pre-repro hipotesis prediction (frontend handler = probable root cause) — code analysis only, **MUST be validated by Plan 01 stacktrace** [ASSUMED]
- DB Dev legacy data shape (Schedule MinValue, Title duplicates, Category null) — **TBD by Plan 01 Query (a)-(e) execution** [ASSUMED]
- Pertamina admin role concurrency frequency — organizational context [ASSUMED]
- HTMX-based ManageAssessment event delegation behavior post-tab-switch — **TBD by Plan 02 manual UAT** [ASSUMED via Phase 311 design intent]

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — semua library existing, verified via direct file read
- Architecture (Hipotesis 1, 2): HIGH — schema + DbContext + 10+ usage cross-reference
- Architecture (Hipotesis 3): MEDIUM — concurrency RULED OUT untuk trigger condition specific tapi feasible scenario lain
- Architecture (Hipotesis 4): MEDIUM-HIGH — code-reading match user-reported symptom
- D-24 worker session invalidation finding: HIGH — grep + code path tracing
- Pitfalls: HIGH — extracted from existing Phase 306/312 lessons
- Data Shape Baseline (D-39): LOW — TBD by Plan 01 query execution
- Pre-repro hipotesis konklusi: MEDIUM — anti-bias warning, stacktrace adalah ground truth

**Research date:** 2026-05-08
**Valid until:** 2026-06-08 (30 days — codebase stable, EF Core 8 stable, no upcoming framework upgrade)

**Anti-bias reminder for Plan 01:** Pre-repro analisis di tabel hipotesis BISA SALAH. Treat as working hypothesis, not conclusion. Stacktrace dari log Dev = ground truth. Finalize tabel sesuai evidence aktual, BUKAN ke prediction.

---

*Research: Phase 314 fix-regenerate-token-untuk-status-upcoming*
*Researched: 2026-05-08*
*Linked to: CONTEXT.md (39 decisions), ROADMAP.md (SC 1-6 line 254-271), REQUIREMENTS.md TKN-01 line 63*
