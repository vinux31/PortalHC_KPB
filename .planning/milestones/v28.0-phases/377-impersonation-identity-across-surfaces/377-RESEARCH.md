# Phase 377: Impersonation Identity Across Surfaces - Research

**Researched:** 2026-06-14
**Domain:** ASP.NET Core MVC identity-resolution saat impersonation (session-based), EF Core query scoping
**Confidence:** HIGH (bug di codebase existing; semua temuan diverifikasi via baca kode langsung)

> Catatan bahasa: narasi Bahasa Indonesia per CLAUDE.md. Semua identifier kode, path file, nama method, dan SQL ditulis verbatim/English.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 Full-fidelity (effective user = X):** worker-data read surfaces resolve identitas X penuh — **data, authz/ownership, DAN scope** ikut X. "Impersonate X = lihat persis seperti X login". Batasan akses X berlaku (Coachee X tak lihat team-view; Coach/SectionHead X lihat tim sesuai `X.Section`). Effective role-level + effective user-id digabung jadi satu identitas efektif.
- **D-02 Boundary = worker-data READ surfaces saja:** resolusi identitas hanya ke jalur read worker-data-diri; attribute `[Authorize(Roles=...)]` ASP.NET TETAP pakai principal asli (admin) — **OUT of scope**. Admin tetap bisa akses halaman admin saat impersonasi (pre-existing). Audit SC1 fokus call-site worker-data.
- **D-03 Mode "role" (tanpa target user):** `TargetUserId` null → **effective user = null → worker-data surface tampil KOSONG (0 record) + hint** "Pilih user spesifik untuk melihat data worker". TIDAK menampilkan data admin (akar bug), TIDAK redirect/hide. Mode role tetap berguna untuk preview UI per role-level (sudah jalan via `GetEffectiveRoleLevel`).
- **D-04 Target user null/terhapus** (`FindByIdAsync` null): **auto-`Stop()` impersonasi + redirect `/Admin/Index` + pesan** "User yang di-impersonate tidak ditemukan". Konsisten dengan pola auto-expire `ImpersonationMiddleware`.
- **D-05 Unify ke 1 sumber kebenaran:** `GetCurrentUserRoleLevelAsync()` dibuat impersonation-aware (return **effective USER + effective ROLE-LEVEL**); konsolidasi call-site `GetEffectiveRoleLevel()` existing (HomeController:53, CMPController:88). Pola "shared core kill drift" (363/365/366).
- **D-06 Fix-set = SEMUA self-worker-data read surfaces:** Records + RecordsWorkerDetail (own) + Results/Certificate/CertificatePdf (ownership = X) + Home GetProgress/upcoming-events + exam StartExam/exam-taking + sisanya yang ditemukan audit. Kriteria: jalur read yang resolve identitas worker untuk data-diri. **Audit SC1 = enumerator otoritatif**.
- **D-07 Deliverable audit (SC1):** peta call-site di **`377-AUDIT.md`** di phase dir. Tabel: `file:line` | surface | jenis read | impersonation-aware sekarang? | in-scope? | aksi fix.

### Claude's Discretion
- **Arsitektur fix konkret:** bentuk helper terpusat (mis. `ImpersonationService.GetEffectiveUserId()` / `GetEffectiveUserAsync()`, atau ubah `GetCurrentUserRoleLevelAsync` per-controller vs ekstrak shared helper). CMP & CDP punya `GetCurrentUserRoleLevelAsync` terpisah; planner putuskan konsolidasi vs paralel-konsisten.
- **Lokasi fallback D-04:** di middleware (`SetContextItems`) atau di helper resolusi — pilih yang paling DRY.
- **Bentuk hint mode-role (D-03):** copy & penempatan UI minor.
- **Strategi test SC4 (no-regression):** xUnit/e2e coverage normal vs impersonate.

### Deferred Ideas (OUT OF SCOPE)
- **Copy/UX banner & hint mode-role** — penyempurnaan teks minor.
- **Full sandbox login-as-X** (swap identitas di SEMUA call-site termasuk non-worker-data + override `[Authorize]`) — **DITOLAK untuk 377** (scope luas, risiko regresi tinggi). Fase tersendiri bila perlu.
- Todo `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` — tidak di-fold (false-positive).
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| IMP-01 | Surface worker-data (CMP/Records, Assessment, Home progress) menampilkan data user impersonated — bukan admin asli — saat impersonasi aktif (banner "Anda melihat sebagai X" jujur). | Akar bug terkonfirmasi: resolver `GetCurrentUserRoleLevelAsync()` + `GetUserAsync(User)` resolve `User` principal yang TIDAK di-swap saat impersonasi. Fix arsitektur lihat §Architecture Patterns (effective-user resolver) + §Code Examples. |
| IMP-02 | Audit cakupan semua call-site `GetCurrentUserRoleLevelAsync` / `_userManager.GetUserAsync(User)` lintas controller — petakan & perbaiki yang abaikan impersonasi. | §Audit Call-Site Inventory di bawah = bahan mentah lengkap untuk `377-AUDIT.md` (D-07). Sudah diklasifikasi in-scope vs out-of-scope. |
</phase_requirements>

## Summary

Bug-nya struktural dan sederhana akarnya: ASP.NET Core **tidak menukar `User` ClaimsPrincipal** saat impersonasi di proyek ini. Impersonasi diimplementasi sebagai **state session + `HttpContext.Items`** (`ImpersonationMiddleware` set role-level/view/name target), bukan re-issue cookie. Akibatnya setiap `_userManager.GetUserAsync(User)` mengembalikan **admin asli**, dan setiap surface worker-data yang query `user.Id` membaca data admin, bukan data X. Banner "Anda melihat sebagai X" jadi bohong.

Sudah ada solusi setengah jalan: `ImpersonationService.GetEffectiveRoleLevel()` + `GetEffectiveSelectedView()` resolve **role-level/view efektif** dari `HttpContext.Items`, dan dipakai di HomeController:53 + CMPController:88. Tapi **tak ada `GetEffectiveUser`** — itu gap-nya (split-brain: role-level efektif tapi identitas user asli). Middleware sudah memanggil `UserManager.FindByIdAsync(targetUserId)` di `SetContextItems` (L135) — jadi target user sudah di-resolve sekali per-request, hanya belum di-expose ke controller untuk query data.

**Primary recommendation:** Tambah resolver effective-user terpusat di `ImpersonationService` (`GetEffectiveUserIdAsync()` + `GetEffectiveUserAsync(UserManager)`), lalu **rewrite `GetCurrentUserRoleLevelAsync()` (CMP + CDP) jadi impersonation-aware** sehingga ~14 call-site self-read terfix otomatis di hulu. Surface yang **bypass resolver** (langsung `GetUserAsync(User)`: HomeController:38, CMPController:203/611/867) di-route ke resolver baru juga. Mode-role (D-03) → effective user null → surface tampil kosong. Fallback null/terhapus (D-04) paling DRY di middleware `SetContextItems` (sudah `FindByIdAsync` di sana). Non-impersonate (D-01/SC4): resolver short-circuit ke `GetUserAsync(User)` persis seperti hari ini. Migration=false.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Resolve identitas efektif (user X vs admin) | API/Backend — `Services/ImpersonationService` | Middleware (`ImpersonationMiddleware` sudah resolve target user 1x/request ke `HttpContext.Items`) | Single source of truth (D-05); service punya akses `IHttpContextAccessor` + session, sama seperti `GetEffectiveRoleLevel()` existing |
| Konsumsi identitas efektif untuk query worker-data | API/Backend — Controllers (CMP/CDP/Home) | `Services/WorkerDataService` (konsumen `user.Id`/`user.Section` — auto-benar bila hulu di-resolve) | Controller adalah seam read worker-data; WorkerDataService agnostik identitas (terima param) |
| Authz `[Authorize(Roles=...)]` ASP.NET attribute gating | Framework (ASP.NET auth pipeline, principal asli) | — | **OUT of scope per D-02** — tetap pakai admin principal; jangan disentuh |
| Ownership authz worker-data (owner∥L1-3∥L4-section) | API/Backend — `CMPController.IsResultsAuthorized` (pure static) | — | Sudah pure & testable; full-fidelity (D-01) = passing `currentUserId`=X.Id ke helper ini |
| Fallback target null/terhapus → Stop+redirect | Middleware (`SetContextItems`) | Resolver helper | DRY: middleware sudah `FindByIdAsync`; deteksi null di sana 1x |

## Standard Stack

Bug-fix di codebase existing — **tidak ada library baru, tidak ada migration**. Stack terverifikasi dari `HcPortal.csproj` / `HcPortal.Tests.csproj`.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | net8.0 | Controllers, middleware pipeline | [VERIFIED: `HcPortal.Tests.csproj` TargetFramework net8.0] — framework proyek |
| Microsoft.AspNetCore.Identity (`UserManager<ApplicationUser>`) | net8.0 (bundled) | Resolusi user dari principal & `FindByIdAsync` | [VERIFIED: dipakai `ImpersonationMiddleware.cs:134`, semua controller] |
| EF Core | 8.0.0 | Query `AssessmentSessions`/`TrainingRecords` by `UserId` | [VERIFIED: `HcPortal.Tests.csproj` EntityFrameworkCore.* 8.0.0] |

### Supporting (test)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| xunit | 2.9.3 | Unit/integration test | [VERIFIED: `HcPortal.Tests.csproj`] — semua test existing |
| Microsoft.EntityFrameworkCore.InMemory | 8.0.0 | Integration test EF (service-level) | [VERIFIED: csproj] — pola test InMemory (mis. `RecordCascadeServiceTests`) |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | Integration test real-SQL | [VERIFIED: csproj] — pola real-SQL fixture (365/366) |
| @playwright/test | (tests/e2e) | E2E impersonate→Records flow | [VERIFIED: `tests/e2e/impersonation.spec.ts` ada — 10 test Phase 283] |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Session+`HttpContext.Items` impersonation (existing) | Cookie re-sign / `IClaimsTransformation` swap principal | DITOLAK eksplisit di CONTEXT (Deferred: full sandbox login-as-X). Akan override `[Authorize]` (langgar D-02), risiko regresi tinggi, scope luas. Jangan. |
| Moq untuk mock `UserManager`/`ISession` | Test seam pure-logic / InMemory | [VERIFIED: tidak ada Moq di `HcPortal.Tests.csproj`] — proyek pakai pure-static + reflection + InMemory. Pertahankan pola itu (lihat §Validation Architecture). |

**Installation:** — (tidak ada paket baru)

## Architecture Patterns

### System Architecture Diagram (alur identitas saat request worker-data)

```
[Admin login + StartImpersonation(user X)]
        │  (session: Mode=user, TargetUserId=X.Id)
        ▼
GET /CMP/Records  ──────────────────────────────────────────────┐
        │                                                         │
        ▼                                                         │
ASP.NET auth pipeline                                             │
  User principal = ADMIN  (TIDAK di-swap)  ◄── inti bug          │
        │                                                         │
        ▼                                                         │
ImpersonationMiddleware (Program.cs:211, setelah UseAuth)        │
  IsImpersonating? ──no──► _next (normal)                         │
        │yes                                                      │
        ▼                                                         │
  IsExpired? ──yes──► Stop() + redirect /Admin/Index (pola D-04) │
        │no                                                       │
        ▼                                                         │
  SetContextItems():                                              │
    FindByIdAsync(TargetUserId) ──► targetUser                   │
      ├─ HttpContext.Items["ImpersonateTargetRoleLevel"] = lvl(X)│
      ├─ HttpContext.Items["ImpersonateTargetSelectedView"]      │
      └─ [GAP] tak set identitas user X buat data query ◄── FIX  │
        │                                                         │
        ▼                                                         │
CMPController.Records:481                                         │
  ┌────────────────────────────────────────────────────┐        │
  │ SEKARANG: GetCurrentUserRoleLevelAsync()            │        │
  │   → _userManager.GetUserAsync(User)  = ADMIN ◄ BUG  │        │
  │ USULAN: resolver impersonation-aware               │ ◄──────┘
  │   IsImpersonating + mode=user → effective user = X  │
  │   IsImpersonating + mode=role → effective user = null (D-03) │
  │   not impersonating → GetUserAsync(User) (SC4)      │
  └────────────────────────────────────────────────────┘
        │ user.Id = X.Id (efektif)
        ▼
WorkerDataService.GetUnifiedRecords(X.Id)  ──► data X ✓
IsResultsAuthorized(owner, X.Id, lvl(X), X.Section, ...) ──► authz X ✓
```

### Component Responsibilities

| File | Tanggung jawab di fix ini |
|------|---------------------------|
| `Services/ImpersonationService.cs` | TAMBAH effective-user resolver (`GetEffectiveUserIdAsync` / `GetEffectiveUserAsync`). Sumber tunggal (D-05). |
| `Middleware/ImpersonationMiddleware.cs` | (opsi DRY D-04) deteksi `targetUser == null` di `SetContextItems` → `Stop()` + redirect. Sudah `FindByIdAsync` di L135. |
| `Controllers/CMPController.cs` | Rewrite `GetCurrentUserRoleLevelAsync():2388` jadi impersonation-aware; route 3 surface bypass (Assessment:203, ExportRecords:611, StartExam:867) ke resolver. |
| `Controllers/CDPController.cs` | Rewrite `GetCurrentUserRoleLevelAsync():3696` impersonation-aware. **Perlu inject `ImpersonationService`** (belum di constructor). |
| `Controllers/HomeController.cs` | Index:38 + GetProgress/GetUpcomingEvents pakai effective user (sudah inject `ImpersonationService`). Folds split-brain L53. |
| `Services/WorkerDataService.cs` | TIDAK berubah — agnostik (terima `userId`/`section`). |

### Pattern 1: Effective-user resolver terpusat (D-05, shared-core)
**What:** Tambah method di `ImpersonationService` yang mengembalikan identitas user efektif, paralel persis dengan `GetEffectiveRoleLevel()` yang sudah ada.
**When to use:** Setiap titik read worker-data-diri.
**Example:**
```csharp
// Source: usulan, paralel dgn Services/ImpersonationService.cs:100 GetEffectiveRoleLevel()
// [ASSUMED arsitektur — Claude's Discretion; planner finalize]

// Returns effective user id: target X saat mode=user, null saat mode=role atau not-impersonating-user.
public string? GetEffectiveTargetUserId()
{
    if (!IsImpersonating() || IsExpired()) return null;       // not impersonating → null sentinel
    if (GetMode() == "role") return null;                     // D-03: mode role → null → surface kosong
    return GetTargetUserId();                                 // mode=user → X.Id
}
```

```csharp
// Source: usulan rewrite Controllers/CMPController.cs:2388 GetCurrentUserRoleLevelAsync()
// Unify: return effective USER + effective ROLE-LEVEL dari satu sumber.
private async Task<(ApplicationUser? User, int RoleLevel)> GetCurrentUserRoleLevelAsync()
{
    // SC4 / D-01: TIDAK impersonate → perilaku identik hari ini
    if (!_impersonationService.IsImpersonating())
    {
        var real = await _userManager.GetUserAsync(User);
        if (real == null) return (null, 0);
        var roles = await _userManager.GetRolesAsync(real);
        return (real, UserRoles.GetRoleLevel(roles.FirstOrDefault() ?? ""));
    }

    // D-03: mode role → effective user null → surface kosong (caller handle null → 0 record + hint)
    var targetId = _impersonationService.GetEffectiveTargetUserId();
    if (targetId == null) return (null, _impersonationService.GetEffectiveRoleLevel() ?? 0);

    // mode user → effective user X
    var target = await _userManager.FindByIdAsync(targetId);
    if (target == null) return (null, 0);    // D-04 fallback ditangani middleware sebelum sampai sini
    var effLevel = _impersonationService.GetEffectiveRoleLevel()
                   ?? UserRoles.GetRoleLevel((await _userManager.GetRolesAsync(target)).FirstOrDefault() ?? "");
    return (target, effLevel);
}
```

**Caller D-03 (mode role → kosong + hint):** `Records:481` sudah `if (user == null) return RedirectToAction("Login")`. Itu SALAH untuk mode-role (harus tampil kosong + hint, bukan redirect). Caller worker-data perlu dibedakan: `user == null && IsImpersonating() && mode==role` → render view dengan `UnifiedRecords` kosong + hint; bukan redirect. Ini sentuhan kecil tapi WAJIB per D-03.

### Pattern 2: Fallback D-04 di middleware (paling DRY)
**What:** Saat `mode=user` tapi `FindByIdAsync(targetUserId)` null → `Stop()` + redirect, sebelum request sampai controller.
**Example:**
```csharp
// Source: usulan sisipan Middleware/ImpersonationMiddleware.cs:135 (di dalam SetContextItems atau dipindah ke InvokeAsync)
var targetUser = await userManager.FindByIdAsync(targetUserId);
if (targetUser == null)
{
    service.Stop();                                            // D-04: konsisten pola auto-expire L57-66
    var tempData = ...GetTempData(context);
    tempData["ErrorMessage"] = "User yang di-impersonate tidak ditemukan.";
    context.Response.Redirect("/Admin/Index");
    return;   // catatan: SetContextItems sekarang void → ubah jadi return bool / pindah cek ke InvokeAsync
}
```
**Catatan implementasi:** `SetContextItems` saat ini `static Task` dipanggil dari 2 tempat (GET L74, whitelisted-write L107). Redirect harus terjadi di `InvokeAsync` (yang pegang `context.Response`). Planner: pindahkan cek null + redirect ke `InvokeAsync`, atau ubah `SetContextItems` return `bool` (false=sudah redirect, short-circuit `_next`).

### Recommended Project Structure
Tidak ada file/folder baru. Edit in-place:
```
Services/ImpersonationService.cs       # + GetEffectiveTargetUserId / GetEffectiveUserAsync
Middleware/ImpersonationMiddleware.cs  # + null-target fallback (D-04)
Controllers/CMPController.cs           # rewrite resolver + 3 bypass surface + D-03 null-handling
Controllers/CDPController.cs           # rewrite resolver + inject ImpersonationService
Controllers/HomeController.cs          # Index/GetProgress effective user + fold L53
HcPortal.Tests/ImpersonationIdentityTests.cs   # BARU (Wave-0)
tests/e2e/impersonation.spec.ts        # + test SC2/SC3 (extend IMP-02 flow)
```

### Anti-Patterns to Avoid
- **Menyentuh `[Authorize(Roles=...)]` / principal swap:** langgar D-02 + masuk Deferred. JANGAN.
- **Fix per-call-site manual tanpa unify:** akan drift (pola yang justru dilarang 363/365/366). Fix di resolver hulu.
- **Mode-role → redirect ke Login:** langgar D-03 (harus tampil kosong + hint). Bedakan null-karena-not-logged-in vs null-karena-mode-role.
- **Apply effective-user ke surface write/admin-scope:** out-of-scope D-02/D-06 + write sudah diblok middleware. Hanya self-worker-data READ.
- **Lupa inject `ImpersonationService` ke CDPController:** [VERIFIED] CDP belum inject — build akan gagal kalau resolver pakai field yang belum ada.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Resolve role-level/view efektif | Re-derive dari session manual di controller | `ImpersonationService.GetEffectiveRoleLevel()` / `GetEffectiveSelectedView()` (sudah ada) | Sudah teruji Phase 283; pola yang harus di-extend, bukan diduplikasi |
| Resolve target user dari id | `_context.Users.FindAsync` ad-hoc | `UserManager.FindByIdAsync` (sudah dipakai middleware L135) | Konsisten Identity; middleware sudah resolve 1x/request |
| Ownership authz worker-data | if-else owner/level/section baru | `CMPController.IsResultsAuthorized` (pure static, sudah ada + ada test) | Single-source REC-04; full-fidelity tinggal kirim `currentUserId`=X.Id |
| Blok write saat impersonasi | Cek per-action POST | `ImpersonationMiddleware` (sudah blok semua non-whitelist POST/PUT/DELETE L84-104) | Sudah menyempit scope audit ke GET saja |

**Key insight:** 80% kerja sudah ada (role-level effective + middleware target-resolve + ownership helper). Fix = **menyambung gap effective-USER** dan **mengarahkan call-site ke satu resolver**, bukan membangun mekanisme impersonasi baru.

## Audit Call-Site Inventory (bahan mentah `377-AUDIT.md` — D-07 / SC1 / IMP-02)

> Metodologi: enumerasi semua `GetUserAsync(User)` + `GetCurrentUserRoleLevelAsync()` + equivalent (`GetUserId(User)`, `User.FindFirstValue`, `GetEffectiveRoleLevel()`) via Grep, lalu triage per kriteria D-06 (self-worker-data READ = in-scope). **POST/write = OUT (diblok middleware, D-02).** **Admin-scope / team-view / `[Authorize]`-gated = OUT.**
> Total raw match `GetUserAsync(User)`: ~120 (mayoritas write-actor di controller admin). Tabel di bawah = yang RELEVAN (in-scope + borderline yang perlu keputusan eksplisit). Planner harus materialize tabel penuh di `377-AUDIT.md`.

### IN-SCOPE — self-worker-data READ (WAJIB fix)

| file:line | surface (action) | verb | jenis read | imp-aware skrg? | aksi fix |
|-----------|------------------|------|-----------|-----------------|----------|
| `CMPController.cs:481` | `Records` | GET | unified records diri (`GetUnifiedRecords(user.Id)`) | ❌ | via resolver (auto) + D-03 null-handling (kosong+hint, bukan redirect) |
| `CMPController.cs:545` | `RecordsWorkerDetail` (own) | GET | unified records `workerId`; own-branch `workerId==user.Id` | ❌ | via resolver (auto); cek other-worker tetap pakai role-level efektif |
| `CMPController.cs:1733` | `Certificate` | GET | ownership `IsResultsAuthorized(...,user.Id,...)` | ❌ | via resolver (auto) |
| `CMPController.cs:1839` | `CertificatePdf` | GET | ownership `IsResultsAuthorized(...,user.Id,...)` | ❌ | via resolver (auto) |
| `CMPController.cs:2080` | `Results` | GET | ownership `IsResultsAuthorized(...,user.Id,...)` | ❌ | via resolver (auto) |
| `CMPController.cs:203` | `Assessment` | GET | **bypass resolver** — `GetUserAsync(User)` → `AssessmentSessions.UserId==userId` | ❌ | route ke resolver (BUKAN auto — direct call) |
| `CMPController.cs:611` | `ExportRecords` (personal) | GET | **bypass resolver** — `GetUnifiedRecords(user.Id)` | ❌ | route ke resolver |
| `CMPController.cs:867` | `StartExam` | GET | **bypass resolver** — `assessment.UserId != user.Id` authz; ADA write-on-GET (L873-877 auto-transition Save) | ❌ | route ke resolver; **catatan write-on-GET di bawah** |
| `HomeController.cs:38` | `Index` | GET | **bypass** — `GetProgress(user.Id)` + `GetUpcomingEvents(user.Id)` | ❌ (split-brain: L53 role-level efektif, identitas asli) | route ke resolver/effective user; fold L53 |
| `CDPController.cs:3859` | `BuildSertifikatRowsAsync` (l5OwnDataOnly) | GET | scoped user-ids by role + own-data L5 | ❌ | via resolver CDP (auto) |
| `CMPController.cs:3870` | `BuildSertifikatRowsAsync` (l5OwnDataOnly) | GET | scoped user-ids by role + own-data L5 | ❌ | via resolver CMP (auto) |

### BORDERLINE — team-view scope (D-01 full-fidelity menyelesaikan otomatis, perlu konfirmasi planner)

| file:line | surface | verb | catatan |
|-----------|---------|------|---------|
| `CMPController.cs:660` | `ExportRecordsTeamAssessment` | GET | team-view; `if (roleLevel>=5) return Forbid()`. Full-fidelity: impersonate Coachee X (L6) → effective level 6 → Forbid (BENAR per D-01 "Coachee X tak lihat team-view"). `user.Section` jadi X.Section (BENAR untuk L4). |
| `CMPController.cs:721` | `ExportRecordsTeamTraining` | GET | sama seperti 660 |
| `CMPController.cs:774` | `RecordsTeamPartial` | GET | sama; `user.Section` scope L4 = X.Section |
| `CMPController.cs:507` (dalam `Records`) | Team-View tab block | GET | `if (roleLevel<=4)` + `user.Section` filter — effective level/section X mengatur otomatis (D-01) |
| `CMPController.cs:86-88` | `DokumenKkj` | GET | **split-brain**: `GetEffectiveRoleLevel()` (L88) tapi `currentUser.Section` (L99, admin). Borderline worker-data (KKJ docs per-bagian). Planner putuskan in-scope. |

### OUT-OF-SCOPE — write/admin/authz (TIDAK disentuh, D-02)

| pola | contoh file:line | alasan OUT |
|------|------------------|-----------|
| POST write-actor (audit `actor`/`currentUser` untuk log/CreatedBy) | `TrainingAdminController` (343,411,571,...), `CoachMappingController` (435,495,...), `AssessmentAdminController` (425,478,...), `DocumentAdminController`, `OrgLabelController`, `OrganizationController:484`, `WorkerController` (287,467,1060), `AdminController` (65,146,198), `AccountController` (141,175,219) | Write diblok middleware (mode read-only). Actor untuk audit = admin asli (BENAR — admin yang melakukan aksi). |
| Admin-scope read (semua user, bukan diri) | `ProtonDataController` (176,233,...), `AssessmentAdminController` GET monitoring | Bukan self-worker-data; admin/HC lihat semua. `[Authorize]`-gated. |
| `CMPController.cs:3693`, `CDPController.cs:3740` | `ViewBag.UserBagian = ...User.Section` (CertificationManagement) | Admin-scope cert mgmt; bila planner anggap perlu, ikut resolver — tapi default OUT (admin view). |
| `NotificationController.cs:18` `User.FindFirstValue(NameIdentifier)` | notifikasi user | Bukan worker-data read surface; notif milik principal. Default OUT (verifikasi planner). |
| `Services/GuideContentProvider.cs` | konten guide statis per-role | Tak resolve user identity untuk data; mapping role→konten. OUT. |
| `HomeController.cs:329/346` `Guide`/`GuideDetail` | GET | konten guide per-role (bukan data-diri). OUT (atau low-pri bila planner mau role efektif — kosmetik). |

**Resolver definition site (bukan call-site):** `CMPController.cs:2388`, `CDPController.cs:3696` — ini yang DI-rewrite.
**Signature divergence [VERIFIED]:** CMP return `(ApplicationUser? User, int)` (nullable, guard `if(user==null)`); CDP return `(ApplicationUser User, int)` (non-null, pakai `user!`). Konsolidasi harus seragamkan ke **nullable** (mode-role D-03 butuh null path). CDP caller (3740,3859) perlu null-guard ditambah.

## Common Pitfalls

### Pitfall 1: Mode-role di-treat sama dengan "tidak login" → redirect
**What goes wrong:** Resolver return `user==null` untuk BOTH (a) session expired/not-logged-in dan (b) mode-role (D-03). Caller `Records:481` saat ini `if(user==null) RedirectToAction("Login")` — untuk mode-role itu salah (harus tampil kosong+hint).
**Why:** Satu sentinel `null` dipakai dua makna berbeda.
**How to avoid:** Caller cek `_impersonationService.IsImpersonating() && mode=="role"` → render view kosong + hint; selain itu (genuinely null) → redirect/Challenge.
**Warning signs:** Impersonate role HC lalu buka /CMP/Records ter-redirect ke Login (bukan tampil kosong).

### Pitfall 2: CDPController belum inject ImpersonationService → build break
**What goes wrong:** Rewrite CDP resolver pakai `_impersonationService` yang belum ada di constructor (L41).
**Why:** [VERIFIED] CDP constructor tidak punya `ImpersonationService`; hanya CMP (L71) & Home (L26) yang inject.
**How to avoid:** Tambah param constructor + field. Service sudah `AddScoped` di `Program.cs:63` (DI siap).
**Warning signs:** `dotnet build` error CS0103 `_impersonationService` not found.

### Pitfall 3: Write-on-GET di StartExam saat impersonasi
**What goes wrong:** `StartExam:873-877` melakukan `SaveChangesAsync()` (auto-transition Upcoming→Open) di action GET. Saat impersonasi (read-only), ini menulis DB.
**Why:** Middleware hanya blok berdasarkan HTTP verb (GET diizinkan L71). Write tersembunyi di GET lolos.
**How to avoid:** Saat `IsImpersonating()`, skip auto-transition Save (atau guard `if(!IsImpersonating())`). Konsisten dengan semangat read-only. Planner: keputusan eksplisit + test.
**Warning signs:** Impersonate X → buka StartExam → status assessment X berubah di DB padahal "read-only".

### Pitfall 4: `IsResultsAuthorized` dengan effective user bisa MELONGGARKAN/MENGETATKAN akses tak terduga
**What goes wrong:** Full-fidelity (D-01) ganti `currentUserId` admin (level 1, full) jadi X (mis. level 6, owner-only). Admin yang impersonate Coachee X tak bisa lihat sertifikat worker lain — itu BENAR per D-01, tapi bisa kaget kalau tak diuji.
**Why:** Authz sekarang ikut batasan X (tujuan D-01).
**How to avoid:** Test matrix eksplisit: impersonate-X lihat data-X = OK; impersonate-X lihat data-Y (X bukan owner & level X tak cukup) = Forbid. Itu acceptance, bukan bug.
**Warning signs:** —

### Pitfall 5: Regresi non-impersonate (SC4)
**What goes wrong:** Rewrite resolver mengubah perilaku saat TIDAK impersonate.
**Why:** Logika impersonation-aware tak short-circuit dengan benar.
**How to avoid:** Branch pertama resolver: `if(!IsImpersonating()) return GetUserAsync(User)` persis. Test: snapshot perilaku normal (user login langsung) sebelum & sesudah.
**Warning signs:** Test existing (`ResultsAuthorizationTests`, e2e `cmp-records-*.spec.ts`) merah.

## Code Examples

### Resolusi role-level efektif yang SUDAH ADA (pola untuk di-extend)
```csharp
// Source: Services/ImpersonationService.cs:100 (VERIFIED, existing)
public int? GetEffectiveRoleLevel()
{
    if (!IsImpersonating() || IsExpired()) return null;
    var mode = GetMode();
    if (mode == "role")
    {
        var role = GetTargetRole();
        return role != null ? UserRoles.GetRoleLevel(role) : null;
    }
    // mode == "user": role level stored in HttpContext.Items by middleware
    var ctx = _httpContextAccessor.HttpContext;
    if (ctx?.Items["ImpersonateTargetRoleLevel"] is int level) return level;
    var targetRole = ctx?.Items["ImpersonateTargetRole"]?.ToString();
    return targetRole != null ? UserRoles.GetRoleLevel(targetRole) : null;
}
```

### Middleware sudah resolve target user (1x/request)
```csharp
// Source: Middleware/ImpersonationMiddleware.cs:128-145 (VERIFIED, existing)
else if (mode == "user")
{
    var targetUserId = service.GetTargetUserId();
    if (targetUserId != null)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var targetUser = await userManager.FindByIdAsync(targetUserId);
        if (targetUser != null)            // ◄ titik sisip D-04: else-branch null → Stop+redirect
        {
            var roles = await userManager.GetRolesAsync(targetUser);
            var primaryRole = roles.FirstOrDefault() ?? "Coachee";
            context.Items["ImpersonateTargetRole"] = primaryRole;
            context.Items["ImpersonateTargetRoleLevel"] = UserRoles.GetRoleLevel(primaryRole);
            context.Items["ImpersonateTargetSelectedView"] = targetUser.SelectedView;
        }
    }
}
```

### Surface yang BYPASS resolver (perlu di-route eksplisit)
```csharp
// Source: Controllers/CMPController.cs:203 Assessment (VERIFIED) — bukan via GetCurrentUserRoleLevelAsync
var user = await _userManager.GetUserAsync(User);
var userId = user?.Id ?? "";
var query = _context.AssessmentSessions.Include(a => a.User).Where(a => a.UserId == userId);
// FIX: userId harus = effective user (X.Id) saat impersonasi
```

## Runtime State Inventory

> Bukan rename/refactor/migration phase — ini bug-fix identity-resolution. Tetap diisi untuk ketegasan.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | **None** — Migration=false; tak ada kolom/key/collection di-rename. Session keys (`Impersonate_*`) tak berubah. | none |
| Live service config | **None** — tak ada konfigurasi service eksternal yang menyimpan string terkait. | none |
| OS-registered state | **None** — tak ada task/scheduler/registrasi OS. | none |
| Secrets/env vars | **None** — tak ada secret/env baru. (Catatan: AD lokal — lihat §Environment Availability.) | none |
| Build artifacts | **None** — edit in-place; rebuild normal `dotnet build`. | rebuild standar |

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Role-level impersonation only (banner+read-only) | + Effective USER identity untuk data query | Phase 377 (ini) | Banner jadi jujur; data = X |
| Resolver per-controller (CMP nullable, CDP non-null) divergen | Unify impersonation-aware single-source (D-05) | Phase 377 | Kill split-brain (pola 363/365/366) |

**Deprecated/outdated:** —

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Bentuk API resolver (`GetEffectiveTargetUserId` / `GetEffectiveUserAsync`) | Architecture Pattern 1 | LOW — Claude's Discretion eksplisit; planner finalize bentuk. Logika inti (short-circuit non-impersonate, null untuk mode-role) yang load-bearing, bukan nama method. |
| A2 | Fallback D-04 paling DRY di middleware `SetContextItems`/`InvokeAsync` | Pattern 2 | LOW — CONTEXT beri opsi middleware vs helper; bukti `FindByIdAsync` sudah di middleware mendukung middleware. Planner boleh pilih helper. |
| A3 | `DokumenKkj` (CMP:86) borderline in-scope | Audit Inventory | MED — split-brain nyata, tapi "worker-data-diri" debatable (KKJ = dokumen per-bagian, bukan data-diri murni). Planner/audit putuskan. Tak ganggu SC2/SC3 inti. |
| A4 | `NotificationController`, `Guide`/`GuideDetail`, CertificationManagement = OUT | Audit Inventory | LOW — bukan self-worker-data read murni. Bila planner anggap perlu, naikkan ke in-scope; tak ada data-leak risk (guide=konten role, notif=principal-owned). |
| A5 | Write-on-GET StartExam harus di-skip saat impersonasi | Pitfall 3 | MED — perilaku read-only; tapi keputusan produk (apakah auto-transition tetap jalan untuk admin-debug?). Planner konfirmasi. |

## Open Questions

1. **Mode-role pada surface non-Records (Assessment, Home progress)?**
   - What we know: D-03 spesifik "surface worker-data tampil kosong + hint".
   - What's unclear: hint copy & penempatan per-surface (Home dashboard vs Records vs Assessment list) — Claude's Discretion.
   - Recommendation: planner standardisasi 1 partial hint reusable; minimal Records (bukti bug utama) wajib.

2. **StartExam saat impersonate: boleh masuk exam page (read-only preview) atau Forbid?**
   - What we know: full-fidelity D-01 = act as X for read; StartExam ada write-on-GET.
   - What's unclear: apakah admin boleh "lihat halaman exam X" tanpa mengubah state.
   - Recommendation: izinkan render (read) tapi skip Save auto-transition saat impersonasi (Pitfall 3); konfirmasi planner/discuss.

3. **CertificationManagement (CMP:3693 / CDP:3740) in-scope?**
   - Default OUT (admin-scope cert mgmt). Recommendation: biarkan OUT kecuali audit temukan jalur self-data.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8 | `dotnet build`/`run`/`test` | ✓ (asumsi env dev existing) | net8.0 | — |
| SQL Server lokal (HcPortalDB_Dev) | DB lokal verifikasi + integration real-SQL | ✓ (env existing) | — | InMemory untuk unit |
| SQLBrowser service | e2e login (NTLM loopback) | perlu start manual | — | — |
| Playwright | e2e impersonation.spec.ts | ✓ (tests/e2e existing) | — | — |

**Missing dependencies with no fallback:** none (semua infra existing).
**Catatan e2e (dari STATE.md / reference_local_e2e_sql_env_fix):**
- Start **SQLBrowser** + gunakan `lpc:` shared-memory connection override (NTLM loopback fail tanpa ini).
- Combined Playwright run **WAJIB `--workers=1`** (DB isolation; impersonation pakai session state — paralel akan saling kontaminasi).
- AD lokal: jalankan `Authentication__UseActiveDirectory=false dotnet run` untuk login lokal non-AD (admin@pertamina.com). [CITED: STATE.md reference_dev_credentials / project_355_shipped]

## Validation Architecture

> nyquist_validation = true (config.json) → section ini WAJIB.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 (unit/integration) + @playwright/test (e2e) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj` (xUnit); `playwright.config.ts` (e2e) |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ImpersonationIdentity"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| E2E command | `npx playwright test tests/e2e/impersonation.spec.ts --workers=1` (start SQLBrowser + `lpc:` + AD=false dulu) |

### Phase Requirements → Test Map
| Req / SC | Behavior | Test Type | Automated Command | File Exists? |
|----------|----------|-----------|-------------------|-------------|
| SC1 / IMP-02 | `377-AUDIT.md` enumerasi lengkap call-site (manual deliverable; verifikasi via grep parity) | manual + grep | `rg "GetUserAsync\(User\)\|GetCurrentUserRoleLevelAsync" Controllers/` lalu cross-check tiap baris ada di AUDIT | ❌ Wave 0 (doc) |
| SC2 / IMP-01 | Impersonate X → `/CMP/Records` tampil data X (assessment+training), bukan admin | e2e | `npx playwright test tests/e2e/impersonation.spec.ts -g "Records data X" --workers=1` | ❌ Wave 0 (extend existing spec) |
| SC2 (service-level) | Resolver return effective user X saat mode=user | unit | `dotnet test --filter ImpersonationIdentity` | ❌ Wave 0 |
| SC3 / IMP-01 | Assessment + Home progress resolve X | e2e + unit | (e2e nav /CMP/Assessment + /Home) + unit resolver | ❌ Wave 0 |
| SC3 (authz fidelity) | `IsResultsAuthorized` dengan currentUserId=X → owner-only behavior X | unit | `dotnet test --filter ResultsAuthorization` (existing matrix + tambah kasus impersonate) | ✅ (extend `ResultsAuthorizationTests`) |
| SC4 / no-regression | Mode normal (non-impersonate): resolver = `GetUserAsync(User)` identik | unit | `dotnet test --filter ImpersonationIdentity` (branch not-impersonating) | ❌ Wave 0 |
| SC4 | D-03 mode-role → effective user null → 0 record | unit | `dotnet test --filter ImpersonationIdentity` | ❌ Wave 0 |
| SC4 | D-04 target null → Stop+redirect | unit (middleware) atau integration | TBD planner (middleware testable via `DefaultHttpContext`) | ❌ Wave 0 |
| regression net | Suite penuh hijau (cmp-records-*, assessment-*) | full | `dotnet test` + `npx playwright test --workers=1` | ✅ existing |

### Test Seam Recommendation (KRITIS — tidak ada Moq di proyek)
Proyek **tidak punya Moq** [VERIFIED csproj]. Pola test existing: pure-static (`ResultsAuthorizationTests`), reflection-attribute (`CDPControllerAuthTests`), InMemory EF (`RecordCascadeServiceTests`), real-SQL fixture (365/366).

**Rekomendasi seam berlapis:**
1. **Resolver decision sebagai pure-logic** (paling testable): faktor keputusan "given (isImpersonating, mode, targetUserId) → effectiveUserId" ke fungsi murni/static, persis pola `IsResultsAuthorized`. Test `[Theory]` tanpa HTTP:
   - `(false, _, _) → "use real user"` sentinel
   - `(true, "role", _) → null` (D-03)
   - `(true, "user", "X") → "X"` (SC2)
   - `(true, "user", null) → null/error` (D-04 trigger)
2. **`ImpersonationService` dengan fake `IHttpContextAccessor`+`ISession`** (tanpa Moq): `DefaultHttpContext` + in-memory `ISession` stub (impl manual ~30 baris) untuk test `GetEffectiveTargetUserId` end-to-end termasuk `IsExpired`.
3. **Ownership matrix** (extend `ResultsAuthorizationTests`): tambah kasus "impersonate X (currentUserId=X.Id, level=X.level) vs owner Y" untuk mengunci D-01 fidelity.
4. **e2e (SC2/SC3)** extend `tests/e2e/impersonation.spec.ts` IMP-02 flow yang SUDAH ada (impersonate via `SearchUsersApi` + autocomplete): tambah langkah `page.goto('/CMP/Records')` → assert tabel berisi record milik X (bukan admin), `TrainingCount>0` bila X punya training. Memerlukan seed data deterministik X dengan ≥1 assessment + ≥1 training (klasifikasi `temporary+local-only` per SEED_WORKFLOW; snapshot+restore).

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~ImpersonationIdentity|FullyQualifiedName~ResultsAuthorization"`
- **Per wave merge:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (full xUnit)
- **Phase gate:** full xUnit hijau + `npx playwright test tests/e2e/impersonation.spec.ts --workers=1` hijau sebelum `/gsd-verify-work`.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ImpersonationIdentityTests.cs` — resolver decision pure-logic (SC2/SC4/D-03) + `ImpersonationService` fake-context.
- [ ] `377-AUDIT.md` — deliverable SC1 (materialize tabel penuh dari §Audit Inventory).
- [ ] Extend `tests/e2e/impersonation.spec.ts` — SC2/SC3 (impersonate→Records=X data).
- [ ] Extend `HcPortal.Tests/ResultsAuthorizationTests.cs` — kasus impersonate fidelity (SC3 authz).
- [ ] Seed deterministik untuk e2e (user X + assessment + training) — temporary local-only, snapshot/restore (SEED_WORKFLOW).

## Security Domain

> security_enforcement tidak di-set false di config → section disertakan. Fokus: phase ini MENGUBAH authz worker-data (full-fidelity), jadi keamanan = inti.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Tak ubah auth pipeline; `[Authorize]` principal asli (D-02) |
| V3 Session Management | yes | Impersonation pakai session (`Impersonate_*`) + auto-expire 30min (existing). D-04 Stop() saat target hilang. |
| V4 Access Control | **yes (inti)** | Full-fidelity (D-01): effective user X membatasi akses ikut X (Coachee X tak lihat team). `IsResultsAuthorized` ownership single-source. Risiko: salah resolve → admin lihat data salah / X lihat data orang lain. |
| V5 Input Validation | no | Tak ada input baru (id dari session, bukan user input mentah; `targetUserId` di-set saat StartImpersonation oleh Admin). |
| V6 Cryptography | no | Tak ada kripto baru. |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Effective user salah resolve → data-leak (admin/X lihat data orang lain) | Information Disclosure | Resolver single-source + matrix test fidelity (impersonate-X≠lihat-Y kecuali authz X izinkan); `IsResultsAuthorized` dengan currentUserId=X |
| Write-on-GET lolos read-only saat impersonasi (StartExam Save) | Tampering | Guard `if(!IsImpersonating())` sebelum SaveChangesAsync (Pitfall 3) |
| Mode-role bocorkan data admin (akar bug) | Information Disclosure | D-03: effective user null → 0 record, JANGAN fallback ke admin |
| Privilege escalation via impersonate Admin | Elevation of Privilege | Sudah dimitigasi: `StartImpersonation` blok target Admin (AdminController:178) + role-level<2 (L153); `SearchUsersApi` exclude admin (e2e IMP-07) |
| Stale/deleted target user → akses tak terdefinisi | — | D-04 auto-Stop+redirect |

## Sources

### Primary (HIGH confidence)
- `Services/ImpersonationService.cs` (full read) — resolver existing, gap effective-user.
- `Middleware/ImpersonationMiddleware.cs` (full read) — `SetContextItems`, auto-expire, read-only enforcement, `FindByIdAsync`.
- `Controllers/CMPController.cs` (L60-205, 470-765, 1720-2100, 2380-2429, 3860-3895) — Records/Assessment/Results/Certificate/StartExam/resolver/IsResultsAuthorized.
- `Controllers/CDPController.cs` (L1-60, 3690-3742, 3850-3880) — resolver divergen + missing DI.
- `Controllers/HomeController.cs` (L25-72, 320-352) — split-brain Index.
- `Controllers/AdminController.cs` (L120-219) — Start/Stop impersonation lifecycle + admin guard.
- `Program.cs` (L63, 203-211) — DI + middleware order.
- `HcPortal.Tests/*.csproj`, `ResultsAuthorizationTests.cs`, `CDPControllerAuthTests.cs` — pola test (no Moq).
- `tests/e2e/impersonation.spec.ts` (full) — e2e seam existing (IMP-02 flow).
- Grep parity: `GetUserAsync(User)` (~120 match), `GetCurrentUserRoleLevelAsync()` (14 match), `GetEffectiveRoleLevel/GetEffectiveSelectedView/GetTargetUserId`.

### Secondary (MEDIUM confidence)
- `.planning/ROADMAP.md` Phase 999.6 (L713-727) — bukti live 2026-06-10 (AssessmentSessions Id 157+66 admin, Iwan 3 TrainingRecords).
- STATE.md — pola shared-core (363/365/366), e2e SQL env gotcha, no-Moq.

### Tertiary (LOW confidence)
- Asumsi A1-A5 (Assumptions Log) — perlu konfirmasi planner/discuss.

## Metadata

**Confidence breakdown:**
- Akar bug & call-site audit: **HIGH** — diverifikasi baca kode langsung + grep parity + bukti live ROADMAP.
- Arsitektur fix: **HIGH (mekanisme) / MEDIUM (bentuk API)** — Claude's Discretion; logika inti pasti, nama method fleksibel.
- Pitfalls: **HIGH** — write-on-GET StartExam, CDP missing DI, signature divergence semua VERIFIED.
- Test seam: **HIGH** — no-Moq dikonfirmasi csproj; pola pure-logic/InMemory/e2e existing terbukti.

**Research date:** 2026-06-14
**Valid until:** 2026-07-14 (stable — codebase internal, tak bergantung library eksternal yang fast-moving)
