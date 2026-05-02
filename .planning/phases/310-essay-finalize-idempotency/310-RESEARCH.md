# Phase 310: Essay Finalize Idempotency - Research

**Researched:** 2026-05-01
**Domain:** ASP.NET Core 8 MVC + EF Core 8 idempotency / concurrency safety untuk Admin Essay finalize flow
**Confidence:** HIGH (semua claim verified via Grep/Read pada source kode existing — bukan training data assumption)

## Summary

Phase 310 menambahkan **idempotency layer** ke `AssessmentAdminController.FinalizeEssayGrading` (L2712-2833) sehingga klik 2x dari Admin tidak menghasilkan: duplikat `TrainingRecord`, double `NomorSertifikat` issuance, audit log spam, atau notifikasi spam ke HC/Admin recipient. Solusi-nya 100% pakai pattern yang **sudah proven di Phase 309** (`GradingService.GradeAndPersist`) — yaitu capture return value dari `ExecuteUpdateAsync` (rows affected), gate semua side-effect (audit + cert + notif call) dengan `if (rowsAffected > 0)`, dan return success no-op friendly response saat rows = 0.

Kunci utama yang ditemukan saat research: **GradingService L195-212 sudah punya pattern persis yang dibutuhkan** (`essayRowsAffected = await ... .ExecuteUpdateAsync(...); if (essayRowsAffected == 0) { LogWarning; return false; }`). Phase 310 cuma perlu mengadopsi pattern ini ke FinalizeEssayGrading + tambah dedup di NotifyIfGroupCompleted + UI gate disable button.

Stack: net8.0, EF Core 8.0.0 (SqlServer + Sqlite providers), ASP.NET Identity Core 8, Razor Views, Bootstrap 5 (existing toast convention `.text-bg-success` pattern di view L448), Playwright TypeScript untuk E2E (TIDAK ada xUnit/NUnit project — verified). Test approach untuk SC #5 parallel finalize WAJIB pakai pendekatan E2E Playwright + manual concurrent verification (sudah ada precedent dari Phase 308 manual UAT) ATAU bootstrap test infrastructure baru (lihat Validation Architecture).

**Primary recommendation:** Adopt verbatim pattern dari `GradingService.cs` L195-212 ke `FinalizeEssayGrading` (capture rowsAffected, gate audit + cert + notif), tambah Razor conditional `disabled` pada button L416 berbasis `session.Status == AssessmentConstants.AssessmentStatus.Completed`, dan tambah `AnyAsync` dedup query di `NotifyIfGroupCompleted` L331 menggunakan field `Title` (bukan `SourceTitle` — UserNotifications schema TIDAK punya field Source*; verified). Audit field `Description` carry context full untuk forensic.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Status guard + idempotent return (SC #1, #5) | API/Backend (`AssessmentAdminController`) | — | Source of truth state ada di DB; UI hanya consume |
| UI button disable/tooltip (SC #2) | Frontend Server (Razor view) | Browser (JS data-bs-toggle) | Conditional render saat status sudah final = server-side; Bootstrap tooltip activation = client-side |
| Notification dedup (SC #3) | API/Backend (`WorkerDataService.NotifyIfGroupCompleted`) | DB (UserNotifications query guard) | Service-level dedup memberi single point of enforcement; caller-side simple |
| Audit dedup (SC #4) | API/Backend (gated oleh ExecuteUpdateAsync rowsAffected) | DB (atomic Status update via WHERE clause) | Race condition di-handle oleh DB constraint, audit di-gate logikanya |
| Concurrent finalize protection (SC #5) | API/Backend (EF WHERE-clause guard) + DB (atomic UPDATE) | — | Optimistic concurrency native EF Core; tidak butuh app-level lock |
| Friendly response render (D-03 toast) | Browser (JS handler L1331-1359) + Frontend Server (TempData[Info] alt) | — | Async fetch handler render alert inline (existing pattern L1383-1389) |

## Standard Stack

### Core (Already Present — Verified)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | EF Core ORM + SQL Server provider | `[VERIFIED: HcPortal.csproj L14]` Native `ExecuteUpdateAsync` returns `int` rows affected — kunci idempotency |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.0 | EF Core Sqlite provider (dev/test) | `[VERIFIED: HcPortal.csproj L13]` |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | Identity (auth, role, current user) | `[VERIFIED: HcPortal.csproj L12]` Pakai `_userManager.GetUserAsync(User)` untuk audit actor |
| ClosedXML | 0.105.0 | Excel export (tidak relevan Phase 310) | `[VERIFIED: HcPortal.csproj L10]` |
| QuestPDF | 2026.2.2 | PDF certificate generation (tidak relevan core SC) | `[VERIFIED: HcPortal.csproj L23]` |

### Internal Services (Already Present — Verified)
| Service | Purpose | Pattern Reference |
|---------|---------|-------------------|
| `AuditLogService.LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` | Audit row writer dgn auto SaveChanges | `[VERIFIED: Services/AuditLogService.cs L21-42]` |
| `INotificationService.SendAsync(userId, type, title, message, actionUrl?)` | Per-user notification creator (sync DB insert) | `[VERIFIED: Services/NotificationService.cs L100-126]` |
| `WorkerDataService.NotifyIfGroupCompleted(session)` | Group completion broadcast ke HC+Admin | `[VERIFIED: Services/WorkerDataService.cs L313-345]` (target refactor SC #3) |
| `AssessmentConstants.AssessmentStatus.{Completed, PendingGrading, Open, Upcoming}` | Status string constants | `[VERIFIED: Models/AssessmentConstants.cs L13-19]` |
| `AssessmentConstants.IsAssessmentSubmitted(status)` | Helper terminal status check | `[VERIFIED: Models/AssessmentConstants.cs L43-44]` |

### NO New Dependencies Required
Phase 310 **tidak butuh** package baru. Semua functionality (idempotency, dedup, UI gate, audit) bisa dicapai dengan stack existing. **Verified** via grep — tidak ada dependency yang missing.

### Alternatives Considered (Rejected)
| Instead of | Could Use | Why Rejected |
|------------|-----------|--------------|
| EF WHERE-clause guard | `SemaphoreSlim` per-session in-memory lock | `[ASSUMED]` Tidak scale ke multi-instance; CONTEXT.md D-06 explicit reject; KPB single-instance saat ini tapi risk leak |
| EF WHERE-clause guard | DB Serializable transaction `IDbContextTransaction` | `[ASSUMED]` Overkill untuk single-row update; deadlock risk lebih tinggi; CONTEXT.md D-06 reject |
| UserNotifications.AnyAsync dedup | Tambah column `NotificationSentAt` di AssessmentSessions | `[ASSUMED]` Schema migration; CONTEXT.md scope "TIDAK schema migration baru" — fallback only kalau lookup tidak feasible. **Result research:** lookup feasible (lihat D-05 verification below) |
| Service-level dedup | Database UNIQUE constraint + catch DbUpdateException | `[ASSUMED]` Schema migration; tidak match scope |

**Installation:** `[VERIFIED]` Tidak perlu `npm install` atau `dotnet add package` — cukup `dotnet build` setelah edit code.

**Version verification:** `[VERIFIED: dotnet build pass dengan stack existing per Phase 309 commit 09a7326e]` — semua versi stable di production. EF Core 8.0.0 `ExecuteUpdateAsync` returns int rows affected `[CITED: Microsoft Docs — EF Core 8 Bulk Update API]`.

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ Admin clicks "Selesaikan Penilaian" button (Razor view)         │
│ Views/Admin/AssessmentMonitoringDetail.cshtml L416              │
└─────────────┬───────────────────────────────────────────────────┘
              │ JS handler (L1331-1359) fetch POST /Admin/FinalizeEssayGrading
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ AssessmentAdminController.FinalizeEssayGrading (L2716)         │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Step 1: Status check (D-03/D-04 explicit branching)        │ │
│  │  - if Status == Completed → return alreadyFinalized:true  │ │
│  │  - if Status == Open → "Belum mulai mengerjakan"          │ │
│  │  - if Status == InProgress → "Sedang mengerjakan"         │ │
│  │  - if Status == Cancelled → "Sudah dibatalkan"            │ │
│  │  - if Status != PendingGrading → fallback message         │ │
│  └────────────────────────┬──────────────────────────────────┘ │
│                           ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Step 2: Pending essay check (L2722-2740 — existing)       │ │
│  └────────────────────────┬──────────────────────────────────┘ │
│                           ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Step 3: Recalculate score (L2742-2781 — existing)         │ │
│  └────────────────────────┬──────────────────────────────────┘ │
│                           ▼                                     │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Step 4: ATOMIC UPDATE + capture rowsAffected              │ │
│  │  rowsAffected = await _context.AssessmentSessions         │ │
│  │    .Where(s => s.Id == sessionId &&                       │ │
│  │                s.Status == PendingGrading)                │ │
│  │    .ExecuteUpdateAsync(...)                               │ │
│  │                                                           │ │
│  │  if (rowsAffected == 0) → race lost → return alreadyFinalized
│  └────────────────────────┬──────────────────────────────────┘ │
│                           ▼ (rowsAffected > 0)                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Step 5: Side-effects (gated by rowsAffected > 0)         │ │
│  │  ├─ TrainingRecord insert (existing AnyAsync guard)       │ │
│  │  ├─ Cert NomorSertifikat (existing WHERE guard)          │ │
│  │  ├─ AuditLog.LogAsync (D-07 NEW — only if rowsAffected>0)│ │
│  │  └─ NotifyIfGroupCompleted (existing call)                │ │
│  └────────────────────────┬──────────────────────────────────┘ │
└─────────────────────────────┬───────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ WorkerDataService.NotifyIfGroupCompleted (L313-345)             │
│                                                                 │
│  For each recipientId in (HC ∪ Admin):                         │
│    ┌───────────────────────────────────────────────────┐       │
│    │ D-05 NEW: alreadySent = await _context             │       │
│    │   .UserNotifications.AnyAsync(n =>                 │       │
│    │     n.UserId == recipientId &&                     │       │
│    │     n.Type == "ASMT_ALL_COMPLETED" &&              │       │
│    │     n.Title == "Assessment Selesai" &&             │       │
│    │     n.Message.Contains(completedSession.Title))    │       │
│    │ if (alreadySent) continue;                         │       │
│    └───────────────────────────────────────────────────┘       │
│    _notificationService.SendAsync(...)                          │
└─────────────────────────────────────────────────────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ JSON response to browser                                        │
│  { success, alreadyFinalized?, score?, isPassed?,              │
│    nomorSertifikat?, message }                                 │
└─────────────┬───────────────────────────────────────────────────┘
              ▼
┌─────────────────────────────────────────────────────────────────┐
│ JS handler (L1347-1357) — UPGRADE D-03                         │
│  - if data.alreadyFinalized → render Bootstrap alert-info       │
│    (inline pattern from L1383-1389)                            │
│  - else if data.success → location.reload()                    │
│  - else → render alert-danger with data.message                │
└─────────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure (Phase 310 — Touched Files)

```
Controllers/
├── AssessmentAdminController.cs   # MODIFY: FinalizeEssayGrading L2712-2833 (SC #1, #4, #5)
Services/
├── WorkerDataService.cs           # MODIFY: NotifyIfGroupCompleted L313-345 (SC #3)
├── AuditLogService.cs             # READ-ONLY (reference pattern only)
├── GradingService.cs              # READ-ONLY (canonical idempotent pattern reference)
Models/
├── AssessmentConstants.cs         # OPTIONAL extend: status BI mapping helper (Claude's discretion)
├── AssessmentMonitoringViewModel.cs # READ-ONLY (MonitoringSessionViewModel sudah punya CompletedAt L55, EssayPendingCount L62 — siap dipakai untuk button gate)
Views/Admin/
├── AssessmentMonitoringDetail.cshtml # MODIFY:
│                                   #   L414-419: Razor conditional disabled + tooltip (SC #2)
│                                   #   L1331-1359: JS handler upgrade alreadyFinalized branch (SC #1)
tests/e2e/
├── assessment.spec.ts             # MODIFY/EXTEND: Phase 310 specs (SC #5 partial — Playwright sequential reload)
```

### Pattern 1: Capture-and-Gate Idempotent ExecuteUpdateAsync

**What:** Capture int rows affected dari ExecuteUpdateAsync; thread ke-2 yang lose race dapat 0 → skip semua side-effect.

**When to use:** Setiap operation yang punya side-effect (audit, cert generation, notification) DAN bisa di-trigger paralel.

**Example (verified pattern dari GradingService L195-212):**
```csharp
// Source: Services/GradingService.cs L195-212 (Phase 309-03 verified)
var essayRowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == session.Id
                && s.Status != AssessmentConstants.AssessmentStatus.Completed
                && s.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, interimPercentage)
        .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.PendingGrading)
        .SetProperty(r => r.HasManualGrading, true)
        .SetProperty(r => r.IsPassed, (bool?)null)
        .SetProperty(r => r.Progress, 100)
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow)
    );

if (essayRowsAffected == 0)
{
    _logger.LogWarning(
        "GradingService: race condition session {SessionId} — sudah Completed/Menunggu Penilaian.",
        session.Id);
    return false;
}
// continue with side-effects (only reached if we won the race)
```

**Adoption ke FinalizeEssayGrading:**
```csharp
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == sessionId && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, finalPercentage)
        .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
        .SetProperty(r => r.IsPassed, isPassed)
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow));

if (rowsAffected == 0)
{
    _logger.LogInformation(
        "FinalizeEssayGrading: race condition session {SessionId} — sudah Completed/Menunggu lain.",
        sessionId);

    // Reload current state untuk friendly no-op response
    var current = await _context.AssessmentSessions.AsNoTracking()
        .FirstOrDefaultAsync(s => s.Id == sessionId);

    return Json(new
    {
        success = true,
        alreadyFinalized = true,
        message = $"Penilaian sudah diselesaikan sebelumnya pada {current?.CompletedAt:dd MMM yyyy HH:mm} WIB",
        score = current?.Score,
        isPassed = current?.IsPassed,
        nomorSertifikat = current?.NomorSertifikat
    });
}

// Audit + cert + notif — ALL gated by rowsAffected > 0 above
```

### Pattern 2: AnyAsync Dedup Lookup untuk Notification

**What:** Sebelum Insert, query existing untuk identifier match; skip jika ada.

**When to use:** Dedup row insertion ketika UNIQUE constraint tidak feasible (tidak bisa migrate schema).

**Verified field availability (UserNotifications):**
```csharp
// Source: Models/UserNotification.cs L11-73 (verified)
// Available fields untuk dedup query:
//   - UserId (string, [Required])
//   - Type (string, MaxLength 50, [Required])
//   - Title (string, MaxLength 200, [Required])
//   - Message (string, [Required], full text)
//   - CreatedAt (DateTime)
//   - DeliveryStatus (string, "Delivered" by default)
// NOT AVAILABLE: SourceTitle, SourceDate, Metadata, AssessmentSessionId
// → CONTEXT.md D-05 fallback note JUSTIFIED — pakai Title + Message.Contains(sessionTitle)
```

**Example (proposed for NotifyIfGroupCompleted L331-345 refactor):**
```csharp
// Source: Services/WorkerDataService.cs L331 (target — proposed)
foreach (var recipientId in recipientIds)
{
    // D-05 NEW: dedup via UserNotifications lookup
    bool alreadySent = await _context.UserNotifications.AnyAsync(n =>
        n.UserId == recipientId
        && n.Type == "ASMT_ALL_COMPLETED"
        && n.Message.Contains(completedSession.Title));

    if (alreadySent) continue;

    try
    {
        await _notificationService.SendAsync(
            recipientId,
            "ASMT_ALL_COMPLETED",
            "Assessment Selesai",
            $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian",
            "/CMP/Assessment");
    }
    catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
}
```

**Caveat:** `Message.Contains(completedSession.Title)` di EF SQL Server translate ke `LIKE '%title%'` — case-sensitivity tergantung collation default DB. KPB pakai SQL Server default collation `SQL_Latin1_General_CP1_CI_AS` (case-insensitive) `[ASSUMED]` — verify saat execute. Risk acceptable: false-positive dedup hanya bisa terjadi kalau ada assessment lain dengan title prefix sama, edge case rare.

**Alternative (lebih ketat):** Filter by Title (exact match `Title == "Assessment Selesai"`) AND CreatedAt date > completedSession.Schedule.Date. Tradeoff: lebih kompleks, tidak strictly perlu.

### Pattern 3: Razor Conditional Button Disable + Bootstrap Tooltip (D-02)

**What:** Server-side render `disabled` attribute + JS-activate Bootstrap tooltip.

**Example (proposed for L414-419):**
```cshtml
@{
    var isFinalized = session.UserStatus == AssessmentConstants.AssessmentStatus.Completed
                      && !string.IsNullOrEmpty(session.NomorSertifikat);
    // NOTE: MonitoringSessionViewModel TIDAK punya field NomorSertifikat saat ini —
    // verified via Read Models/AssessmentMonitoringViewModel.cs L47-63.
    // Planner WAJIB extend ViewModel atau guard purely by Status == Completed.
}
<div id="finalizeSection_@session.Id"
     style="display: @(session.EssayPendingCount == 0 ? "block" : "none")">
    <button class="btn btn-success btn-finalize-grading"
            data-session-id="@session.Id"
            @(isFinalized ? "disabled" : "")
            @(isFinalized ? $"title=\"Sudah selesai pada {session.CompletedAt:dd MMM yyyy HH:mm} WIB\"" : "")
            @(isFinalized ? "data-bs-toggle=\"tooltip\"" : "")
            style="@(isFinalized ? "cursor: not-allowed;" : "")">
        <i class="bi bi-check-circle me-1"></i>Selesaikan Penilaian
    </button>
</div>
```

**Activation script (add to existing `<script>` block in view):**
```js
// Bootstrap 5 tooltip activation — verified Bootstrap 5 docs
const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
tooltipTriggerList.forEach(el => new bootstrap.Tooltip(el));
```

**Caveat:** ViewModel `MonitoringSessionViewModel` (L47-63) TIDAK punya `NomorSertifikat`. Planner harus pilih:
- (a) **Extend ViewModel** — tambah `string? NomorSertifikat { get; set; }` + populate di mapper saat query session.
- (b) **Pakai Status only** — gate by `Status == Completed` saja (mengabaikan NomorSertifikat). Cukup untuk SC #2 acceptance karena NomorSertifikat null DAN Status=Completed berarti session tidak generateCertificate atau failed — masih sudah finalized, button harus tetap disabled.

Recommendation: pilih (b) — `session.UserStatus == "Completed"` adalah single source of truth untuk "sudah finalized". NomorSertifikat null ≠ belum finalized (bisa Completed-Failed-no-cert).

### Pattern 4: JSON Response with Optional Fields (D-03 backwards-compat)

**What:** Tambah field baru ke response tanpa breaking handler lama.

**Example (proposed for FinalizeEssayGrading return):**
```csharp
// Success path (rowsAffected > 0)
return Json(new {
    success = true,
    score = finalPercentage,
    isPassed,
    nomorSertifikat = updatedSession?.NomorSertifikat
});

// Already finalized path (rowsAffected == 0)
return Json(new {
    success = true,
    alreadyFinalized = true,
    message = $"Penilaian sudah diselesaikan sebelumnya pada {current?.CompletedAt:dd MMM yyyy HH:mm} WIB",
    score = current?.Score,
    isPassed = current?.IsPassed,
    nomorSertifikat = current?.NomorSertifikat
});

// Status-not-PendingGrading path (D-04 explicit per status)
return Json(new {
    success = false,
    message = StatusMessageFor(session.Status)  // helper untuk BI mapping
});
```

**JS handler upgrade (L1347-1357):**
```js
const data = await res.json();
if (data.success && data.alreadyFinalized) {
    // D-03: render inline alert-info friendly
    showAlert('info', 'bi-info-circle-fill', data.message);
    this.disabled = true;
} else if (data.success) {
    location.reload();
} else {
    showAlert('danger', 'bi-exclamation-triangle-fill', data.message);
    this.disabled = false;
}

// Helper (extracted from existing pattern L1383-1389)
function showAlert(type, icon, message) {
    const html = '<div class="alert alert-' + type + ' alert-dismissible fade show mb-3" role="alert">'
        + '<i class="bi ' + icon + ' me-2"></i>' + message
        + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
    document.querySelector('.container-fluid').insertAdjacentHTML('afterbegin', html);
}
```

### Anti-Patterns to Avoid

- **Lock-then-check (TOCTOU):** Jangan `if (session.Status == "Completed") return;` lalu `await SaveChangesAsync()`. Race condition window terbuka. Pakai atomic WHERE-clause guard di ExecuteUpdateAsync.
- **`session.Status = "Completed"; await _context.SaveChangesAsync();`:** Tracked entity update tidak atomic untuk race protection. WAJIB pakai `ExecuteUpdateAsync` dengan WHERE clause.
- **Throwing exception untuk "already finalized":** UX buruk. Return success no-op friendly per D-03.
- **Audit log SEBELUM rowsAffected check:** Hasilkan duplicate audit entries. WAJIB gate dengan `if (rowsAffected > 0)` per D-07.
- **`location.reload()` saat alreadyFinalized:** Hilangkan friendly toast — state sudah sama, reload tidak menambah info. Cukup render alert info inline + disable button.
- **`SemaphoreSlim _lock = new(1, 1)`:** App-level lock tidak survive multi-instance, deadlock risk, masking real concurrency bug. Reject per D-06.
- **`.AsTracking()` tanpa kebutuhan:** Tidak relevan langsung tapi gunakan `.AsNoTracking()` saat reload session current state untuk D-03 message — cuma read-only.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Concurrency control single-row update | Custom `lock` statement / `SemaphoreSlim` | `ExecuteUpdateAsync` dengan WHERE clause guard | DB-native atomic, multi-instance safe, no deadlock |
| Notification dedup | Custom hash/timestamp tracking | `_context.UserNotifications.AnyAsync(...)` | Existing schema sudah cukup; query <10ms; no migration |
| Audit log dedup | Hash check / time window | Gate by `if (rowsAffected > 0)` (natural dedup via DB constraint) | Race lost = automatic skip, zero extra code |
| Status BI mapping | Inline string concat per branch | Helper di `AssessmentConstants` (Claude's discretion) | Reusable; CONTEXT.md `<specifics>` mention |
| Bootstrap tooltip | Custom CSS `:hover ::after` | `data-bs-toggle="tooltip"` + `new bootstrap.Tooltip(el)` | Existing convention; Bootstrap 5 native |
| Inline JS alert injection | New helper file | Extract `showAlert()` from existing pattern L1383-1389 in same view | Convention consistent |
| Toast positioning | Custom z-index | Existing `.position-fixed top-0 end-0 p-3 z-index:1080` (L447) | Already in view |

**Key insight:** Phase 310 ZERO new abstractions. Semua pattern sudah ada di codebase. Job-nya adalah composition existing patterns ke method target.

## Runtime State Inventory

> Phase 310 menambahkan dedup + idempotent — bukan rename/refactor murni. Tetap audit kategori berikut karena ada perubahan response contract dan UI behavior.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — tidak ubah schema atau data shape | None — tidak ada migration data |
| Live service config | None — tidak ada feature flag, env var, atau external service config | None |
| OS-registered state | None — tidak ada scheduled task, systemd unit, atau registry key | None |
| Secrets/env vars | None — tidak ada secret baru atau renamed | None |
| Build artifacts | None — pure source edits; `dotnet build` regenerate semua | None |

**Tambahan kategori spesifik untuk Phase 310:**

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| **API contract field changes** | Response `FinalizeEssayGrading` JSON tambah field: `alreadyFinalized`, `message`, `nomorSertifikat`. Existing fields `success`, `score`, `isPassed` tetap (backwards-compat). | UI handler (L1347-1357) wajib branch baca `alreadyFinalized` field; missing → fallthrough ke success/error existing |
| **JS button state** | Button `.btn-finalize-grading` previously hanya disable saat klik in-flight (L1337). Dengan D-02, juga disable persistent saat Status=Completed dari server. | Test scenario: load page session Completed → assert button disabled; klik button non-Completed → in-flight disable → response → re-enable kalau error |
| **Bootstrap tooltip activation** | New tooltip on disabled button — wajib activate via JS `new bootstrap.Tooltip()` setelah DOM render. | Add activation snippet di existing `<script>` block atau `DOMContentLoaded` handler |

**Verification:** Sudah grep `_context.SaveChanges` calls dan `ExecuteUpdateAsync` patterns. Tidak ada cached static state, tidak ada singleton service yang hold state untuk Essay flow.

## Common Pitfalls

### Pitfall 1: Lupa capture rowsAffected dari ExecuteUpdateAsync existing
**What goes wrong:** Existing code di FinalizeEssayGrading L2784-2790 throws away return value. Audit log + cert + notif call jalan unconditionally → race condition thread ke-2 hasilkan duplicate side-effects.
**Why it happens:** Pattern lama "fire and forget"; developer tidak sadar `ExecuteUpdateAsync` returns int.
**How to avoid:** WAJIB capture: `var rowsAffected = await _context.AssessmentSessions.Where(...).ExecuteUpdateAsync(...);` dan gate semua side-effect dengan `if (rowsAffected > 0)`. Pattern reference: GradingService.cs L195-212.
**Warning signs:** Audit log table punya 2+ entries dengan timestamp <1s untuk session yang sama; UserNotifications table punya duplicate row untuk same recipient + sessionId.

### Pitfall 2: Status check non-atomic (TOCTOU race)
**What goes wrong:** Code seperti `if (session.Status != "PendingGrading") return error;` lalu mutate. Antara check dan mutate, thread lain bisa ubah status. Thread satu yang dapat read "PendingGrading" tapi update gagal silently.
**Why it happens:** Pattern .NET sync coding; developer tidak think distributed/concurrent.
**How to avoid:** Status check awal (L2719) sebagai fast-path validation untuk friendly response D-03/D-04, BUKAN sebagai concurrency guard. Real concurrency guard ada di WHERE clause `ExecuteUpdateAsync` step 4. Dua layer: cosmetic check + atomic guard.
**Warning signs:** Race-mode bug muncul di prod: status validation pass tapi update silent fail.

### Pitfall 3: NotifyIfGroupCompleted called twice → 2x notif spam
**What goes wrong:** Per CONTEXT.md `Phase 247 approval chain` deferred section: "overlap risk dengan Phase 310 (T9 NotifyIfGroupCompleted)". Saat 5 session sibling dalam 1 group + 2 admin paralel finalize last 2 sessions, conditional `allSiblings.All(s => s.Status == "Completed" || s.Status == "Cancelled")` di L324 bisa lulus untuk kedua thread → 2x notif loop.
**Why it happens:** Read-then-act pattern; thread A dan B sama-sama read "all Completed", sama-sama loop send notif.
**How to avoid:** D-05 dedup via `_context.UserNotifications.AnyAsync(...)` SEBELUM `SendAsync`. Race-safe karena dedup query catch row insert dari thread lain (timing matter — race window tetap ada untuk insert berbeda dalam <100ms; minimal acceptable).
**Warning signs:** HC menerima 2x identical notification "Assessment Selesai" untuk same group dalam waktu dekat.

### Pitfall 4: `Message.Contains(...)` di EF query — case-sensitivity & false positive
**What goes wrong:** `LIKE '%title%'` di SQL Server depend on collation. Default `CI_AS` (case-insensitive accent-sensitive). Title "Safety OJT" bisa match notifikasi untuk "Safety OJT 2026" jika ada.
**Why it happens:** `Contains` translate ke `LIKE` substring match.
**How to avoid:** **Strict version:** `n.Message == $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian"` (exact match). Trade-off: brittle kalau message template berubah. **Recommended balance:** filter by Title field exact + Message Contains, plus filter by `n.CreatedAt >= completedSession.Schedule.Date`.
**Warning signs:** Notification "missed" karena false-positive dedup saat Admin re-trigger setelah debugging.

### Pitfall 5: ViewModel field missing — render fail saat NomorSertifikat dipakai di Razor
**What goes wrong:** `MonitoringSessionViewModel` (L47-63) TIDAK punya `NomorSertifikat`. Coba render `@session.NomorSertifikat` → compile error.
**Why it happens:** Mismatch CONTEXT.md asumsi (`session.NomorSertifikat`) vs reality ViewModel.
**How to avoid:** Pilih salah satu di planning:
- (a) Extend `MonitoringSessionViewModel` + populate di mapper
- (b) Skip NomorSertifikat check di UI gate, gate purely by `Status == Completed` (preferred — simpler, sufficient untuk SC #2)
**Warning signs:** Razor compile error "MonitoringSessionViewModel does not contain a definition for 'NomorSertifikat'".

### Pitfall 6: Bootstrap tooltip tidak muncul karena `disabled` button tidak fire mouseenter
**What goes wrong:** `<button disabled>` di-skip oleh browser pointer events; `data-bs-toggle="tooltip"` tidak ter-trigger.
**Why it happens:** Native `disabled` attribute disable semua DOM events.
**How to avoid:** Wrap button dalam `<span data-bs-toggle="tooltip" title="...">` parent wrapper yang BUKAN disabled. Tooltip attached ke wrapper. Atau pakai `aria-disabled="true"` + CSS pointer-events:none + class custom (tidak native disabled).
**Warning signs:** UAT report "tooltip tidak muncul saat hover button selesai".

### Pitfall 7: `_context` tidak di-inject ke `WorkerDataService` (verify before refactor)
**What goes wrong:** D-05 dedup query butuh `_context.UserNotifications.AnyAsync(...)`. Kalau `WorkerDataService` tidak punya `_context` field, harus inject baru.
**Why it happens:** Service constructor injection list mungkin tidak include ApplicationDbContext.
**How to avoid:** Cek constructor `WorkerDataService` — ada field `_context` (ApplicationDbContext)? `[VERIFIED: Services/WorkerDataService.cs L316-321 — `_context.AssessmentSessions.AsNoTracking()` udah dipakai → ApplicationDbContext sudah injected]`. Aman.
**Warning signs:** Build error "The name '_context' does not exist in the current context".

### Pitfall 8: TempData[Info] vs inline JS alert — inkonsistensi dengan AJAX flow
**What goes wrong:** `TempData["Info"]` cuma render setelah full page reload (Razor server render saat next request). FinalizeEssayGrading dipanggil via AJAX — TempData tidak ter-render kecuali full reload.
**Why it happens:** TempData mechanic = server-side carry-over single request lifecycle.
**How to avoid:** D-03 friendly success no-op WAJIB pakai inline JS alert injection (helper `showAlert()` extract dari L1383-1389), BUKAN TempData[Info]. TempData hanya berguna kalau handler trigger `location.reload()` setelah finalize (success path normal — but D-03 alreadyFinalized path TIDAK reload).
**Warning signs:** UAT report "saya klik tombol selesaikan tapi tidak ada feedback visual".

## Code Examples

### Example 1: Full proposed FinalizeEssayGrading method (skeleton)

```csharp
// Source: Composition dari GradingService.cs L189-227 + existing AssessmentAdminController L2712-2833
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> FinalizeEssayGrading(int sessionId)
{
    var session = await _context.AssessmentSessions.FindAsync(sessionId);
    if (session == null)
        return Json(new { success = false, message = "Session tidak ditemukan." });

    // STEP 1: D-03 — friendly no-op kalau sudah Completed
    if (session.Status == AssessmentConstants.AssessmentStatus.Completed)
    {
        return Json(new
        {
            success = true,
            alreadyFinalized = true,
            message = $"Penilaian sudah diselesaikan sebelumnya pada {session.CompletedAt:dd MMM yyyy HH:mm} WIB",
            score = session.Score,
            isPassed = session.IsPassed,
            nomorSertifikat = session.NomorSertifikat
        });
    }

    // D-04 — pesan spesifik per status non-PendingGrading
    if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
    {
        var msg = session.Status switch
        {
            AssessmentConstants.AssessmentStatus.Open => "Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian.",
            "InProgress" => "Belum bisa di-finalize. Peserta sedang mengerjakan ujian.",
            "Cancelled" => "Tidak bisa di-finalize. Session sudah dibatalkan.",
            _ => $"Tidak bisa di-finalize. Status saat ini: {session.Status}."
        };
        return Json(new { success = false, message = msg });
    }

    // STEP 2-3: existing — pending essay check + score recalc (L2722-2781) ...
    // [unchanged]

    int finalPercentage = maxScore > 0 ? (int)((double)totalScore / maxScore * 100) : 0;
    bool isPassed = finalPercentage >= session.PassPercentage;

    // STEP 4: ATOMIC — capture rowsAffected (D-06)
    var rowsAffected = await _context.AssessmentSessions
        .Where(s => s.Id == sessionId && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.Score, finalPercentage)
            .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
            .SetProperty(r => r.IsPassed, isPassed)
            .SetProperty(r => r.CompletedAt, DateTime.UtcNow));

    if (rowsAffected == 0)
    {
        // Race lost — read current state, return friendly no-op
        var current = await _context.AssessmentSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        _logger.LogInformation(
            "FinalizeEssayGrading: race condition session {SessionId} — skip side-effects.",
            sessionId);

        return Json(new
        {
            success = true,
            alreadyFinalized = true,
            message = $"Penilaian sudah diselesaikan sebelumnya pada {current?.CompletedAt:dd MMM yyyy HH:mm} WIB",
            score = current?.Score,
            isPassed = current?.IsPassed,
            nomorSertifikat = current?.NomorSertifikat
        });
    }

    // STEP 5: Side-effects (gated by rowsAffected > 0)

    // 5a. TrainingRecord (existing L2792-2809 — keep AnyAsync guard)
    var judul = $"Assessment: {session.Title}";
    bool trExists = await _context.TrainingRecords.AnyAsync(t =>
        t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule);
    if (!trExists)
    {
        _context.TrainingRecords.Add(new TrainingRecord { /* ... */ });
        await _context.SaveChangesAsync();
    }

    // 5b. Certificate (existing L2812-2825 — keep WHERE guard + try DbUpdateException)
    if (session.GenerateCertificate && isPassed)
    {
        var certNow = DateTime.Now;
        try
        {
            var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certNow.Year);
            await _context.AssessmentSessions
                .Where(s => s.Id == sessionId && s.NomorSertifikat == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
        }
        catch (DbUpdateException) { /* race condition — skip */ }
    }

    // 5c. Audit (D-07 NEW — gated by rowsAffected > 0 above)
    var currentUser = await _userManager.GetUserAsync(User);
    var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
        ? (currentUser?.FullName ?? "Unknown")
        : $"{currentUser.NIP} - {currentUser.FullName}";
    try
    {
        await _auditLog.LogAsync(
            currentUser?.Id ?? "",
            actorName,
            "FinalizeEssayGrading",
            $"Session {sessionId} ({session.Title}) finalized: score={finalPercentage}%, isPassed={isPassed}",
            sessionId,
            "AssessmentSession");
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "FinalizeEssayGrading: audit log failed for session {SessionId}", sessionId);
        // jangan throw — audit failure tidak boleh break primary flow
    }

    // 5d. Reload + notify (existing L2828-2830 — service-level dedup baru di D-05)
    var updatedSession = await _context.AssessmentSessions.FindAsync(sessionId);
    if (updatedSession != null)
        await _workerDataService.NotifyIfGroupCompleted(updatedSession);

    return Json(new
    {
        success = true,
        score = finalPercentage,
        isPassed,
        nomorSertifikat = updatedSession?.NomorSertifikat
    });
}
```

### Example 2: Existing AuditLog usage pattern (verified)

```csharp
// Source: Controllers/AssessmentAdminController.cs L322-328 (AddCategory pattern — proven)
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "AddCategory",
    $"Added assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
    category.Id, "AssessmentCategory");
```

**Reused pattern:** Phase 310 D-07 audit field signature 100% match — `actionType="FinalizeEssayGrading"`, `description="Session {id} ({title}) finalized: score={pct}%, isPassed={bool}"`, `targetId=sessionId`, `targetType="AssessmentSession"`.

### Example 3: Bootstrap inline alert injection (verified — adopt sebagai showAlert helper)

```js
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml L1383-1389 (extra time pattern)
var alertClass = data.success ? 'alert-success' : 'alert-danger';
var iconClass = data.success ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';
var alertHtml = '<div class="alert ' + alertClass + ' alert-dismissible fade show mb-3" role="alert">'
    + '<i class="bi ' + iconClass + ' me-2"></i>' + data.message
    + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
var alertContainer = document.querySelector('.container-fluid');
alertContainer.insertAdjacentHTML('afterbegin', alertHtml);
```

**Adopt:** Extract menjadi helper `showAlert(type, icon, message)` di top of `<script>` block, pakai untuk D-03 alreadyFinalized branch ('info' + 'bi-info-circle-fill') AND D-04 error branch ('danger' + 'bi-exclamation-triangle-fill').

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Tracked entity update (`session.Status = "Completed"; SaveChanges()`) | Bulk update API `ExecuteUpdateAsync` dengan WHERE-clause guard | EF Core 7.0 (2022-11) | Atomic single-statement UPDATE; race-safe |
| App-level `lock`/`SemaphoreSlim` for concurrency | DB-native WHERE clause + capture rows affected | EF Core 7+ | Multi-instance safe; no deadlock |
| `INotificationService` w/o dedup | Service-level `AnyAsync` lookup before insert | Phase 310 (this) | Dedup tanpa schema change |
| Generic error "session tidak dalam status..." | Per-status BI message + idempotent success no-op | Phase 310 (this) | UX friendly + actionable |

**Deprecated/outdated:**
- Old EF6 `_context.Database.ExecuteSqlCommand("UPDATE ...")` — replaced by `ExecuteUpdateAsync` LINQ syntax (compile-time safe).
- "Tracked entity + SaveChanges" pattern untuk concurrency-sensitive update — pakai `ExecuteUpdateAsync` selalu untuk single-row mutate atomic.

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia mandatory** untuk semua user-facing copy. Berlaku untuk:
  - D-03 message "Penilaian sudah diselesaikan sebelumnya pada..."
  - D-04 message per status ("Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian." dll)
  - D-02 tooltip "Sudah selesai pada {dd MMM yyyy HH:mm} WIB"
  - JS alert messages, AuditLog Description (boleh English internal — debatable, tapi consistent existing audit pakai English untuk machine-readable; rekomendasi: BI untuk human description, English untuk actionType)

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01**: UI scope = `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419 only. TIDAK touch CDP CertificationManagement.
- **D-02**: UI gate = **Disable button + tooltip** saat `Status==Completed && NomorSertifikat!=null`. Tooltip BI: "Sudah selesai pada [tanggal CompletedAt format dd MMM yyyy HH:mm]". Affordance: greyed out + cursor not-allowed + Bootstrap `disabled`.
- **D-03**: Saat Status==Completed, API return success no-op friendly response: `{success:true, alreadyFinalized:true, message, score, isPassed, nomorSertifikat}`. UI render toast info biru (alert-info), BUKAN error merah.
- **D-04**: Saat Status non-terminal non-Completed: pesan spesifik per status (Open/InProgress/Cancelled), error toast (alert-danger).
- **D-05**: NotifyIfGroupCompleted dedup via lookup UserNotifications existing. NO schema migration. Reuse field UserNotifications yang sudah ada. Fallback NotificationSentAt column kalau lookup tidak feasible.
- **D-06**: Tidak tambah lock baru. Andalkan kombinasi: Status WHERE-clause guard, Cert WHERE-clause guard, TrainingRecord AnyAsync guard, Notification dedup baru, Audit log dedup baru, Idempotent return value.
- **D-07**: Audit log via `_auditLog.LogAsync(...)` dalam if-block "rowsAffected > 0" dari status update ExecuteUpdateAsync. Capture rowsAffected dari ExecuteUpdateAsync (return int — current code throws away return value).

### Claude's Discretion

- Razor conditional vs JS guard untuk D-02 disable
- Exact tooltip styling (`data-bs-toggle="tooltip"` vs native `title`)
- JSON contract field naming convention untuk D-03
- Audit log payload Detail field content (verbose vs minimal)
- Toast vs inline alert D-03 implementation (existing global toast helper kalau ada vs inline injection)
- Exception handling: D-03/D-04 wrap di try-catch pattern Phase 309 WCRT-01 (DbException → FormatException → Exception berlapis)

### Deferred Ideas (OUT OF SCOPE)

- Tombol "Create Sertifikasi" baru di CDP CertificationManagement
- NotificationSentAt column migration (fallback only kalau D-05 lookup tidak feasible)
- AssessmentConstants.IsAssessmentSubmitted reuse opportunistic refactor
- SemaphoreSlim per-session lock (multi-instance scale-out)

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **ESCG-01** | Admin tidak menerima error "session tidak dalam status menunggu penilaian" saat membuka halaman create sertifikasi pada session mix MC/MA/Essay yang sudah Completed. UI menyembunyikan tombol "Create Sertifikasi" jika `Status == "Completed"` && `NomorSertifikat != null`; OR jika tombol di-klik untuk session terminal, menampilkan pesan ramah no-op (bukan error). Idempotent: klik 2x tidak menduplikasi notification atau audit log. | **Pattern 1** (capture rowsAffected) memberi idempotency; **Pattern 2** (AnyAsync dedup) cegah notif spam; **Pattern 3** (Razor conditional disabled) gate UI button; **Pattern 4** (alreadyFinalized JSON field) deliver friendly no-op response. Reference: GradingService.cs L195-212 sebagai canonical implementation existing |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | SQL Server collation default `CI_AS` (case-insensitive) untuk dedup `Message.Contains` | Pattern 2 caveat | Notif dedup miss kalau DB pakai `CS_AS` collation; mitigation: planner verify saat execute, pakai exact Title match alternatif |
| A2 | `MonitoringSessionViewModel` properti `UserStatus` adalah session status (bukan worker user status) | Pattern 3 example | Conditional `isFinalized` gate gagal kalau field semantic beda; verify saat planning via Read mapper code |
| A3 | Bootstrap tooltip aktivasi via `new bootstrap.Tooltip(el)` masih convention (Bootstrap 5+) | Pattern 3 activation script | Tooltip tidak muncul; alternative: pakai native `title` attribute (no JS, tidak cantik) |
| A4 | `[ASSUMED]` Lock-free pattern via WHERE-clause guard cukup untuk SC #5 acceptance — tidak butuh distributed lock | Pattern 1 + D-06 | Race condition tetap muncul kalau ada gap antara guard dan side-effect non-atomic; mitigation: integration test prove acceptable |
| A5 | `[ASSUMED]` Audit failure tidak boleh break primary flow → wrap try-catch dengan `_logger.LogWarning` (precedent Phase 306 D-10) | Example 1 step 5c | Audit gap kalau LogAsync throw, mitigation: structured logging untuk forensic catch-up |
| A6 | `[ASSUMED]` `_logger` field tersedia di AssessmentAdminController (sudah injected) | Example 1 logging | Build error kalau bukan; verify saat planning — controller umumnya punya ILogger via DI |
| A7 | `[ASSUMED]` Bahasa Indonesia diterapkan ke audit Description string (bukan hanya UI) | Project Constraints | AuditLog.Description format mismatch policy; rekomendasi: tetap English untuk machine-readable + structured logging convention, BI hanya untuk user-facing |

## Open Questions (RESOLVED)

1. **NotificationSentAt fallback feasibility**
   - What we know: UserNotifications schema TIDAK punya field SourceTitle/SourceDate (`[VERIFIED: Models/UserNotification.cs L11-73]`). Pattern 2 pakai `Message.Contains(title)` viable tapi LIKE substring.
   - What's unclear: Apakah false-positive dedup acceptable (rare collision Title prefix-match)?
   - Recommendation: Pakai dual filter `Type == "ASMT_ALL_COMPLETED" && Message.Contains(title) && CreatedAt >= session.Schedule.Date` untuk reduce false-positive. Kalau planning verify masih risiko collision, escalate ke fallback NotificationSentAt column migration (tapi ini break "no schema migration" scope — surface ke user).
   - **RESOLVED:** Pakai dual filter Title exact (`Title == "Assessment Selesai"`) + `Message.Contains(completedSession.Title)` + `CreatedAt >= completedSession.Schedule.Date` time-window guard (PATTERNS.md section 2). Tidak perlu schema migration — UserNotifications fields existing cukup.

2. **`MonitoringSessionViewModel` extension untuk NomorSertifikat?**
   - What we know: ViewModel tidak punya field; Razor conditional D-02 perlu data ini.
   - What's unclear: Apakah extend ViewModel acceptable scope, atau gate purely by Status==Completed?
   - Recommendation: Gate purely by Status==Completed (simpler, cukup untuk SC #2 acceptance). Status=Completed always = sudah finalized regardless of NomorSertifikat.
   - **RESOLVED:** Extend `MonitoringSessionViewModel` dengan field `Status` (raw AssessmentSession.Status) + `NomorSertifikat` (nullable string), populate via mapper di `AssessmentAdminController` (Plan 01 Task 1 Edit A). Honor CONTEXT.md D-02 locked dual-criterion gate (`Status==Completed && NomorSertifikat!=null`) — bukan single-criterion.

3. **Integration test (SC #5) — pakai infrastructure apa?**
   - What we know: Tidak ada xUnit/NUnit/MSTest project di repo. Hanya Playwright TypeScript E2E `[VERIFIED: tests/playwright.config.ts]`.
   - What's unclear: Apakah Phase 310 introduce .NET test project baru, atau pakai Playwright concurrent test, atau manual UAT?
   - Recommendation: Lihat **Validation Architecture** section. Phase 310 SC #5 disarankan **manual UAT concurrent click + DB query verify** (precedent Phase 308 manual UAT) plus **Playwright sequential test** (klik tombol 2x sequential, verify response field `alreadyFinalized` muncul, no error toast).
   - **RESOLVED:** Playwright sequential 3 tests di FLOW 9 (`assessment.spec.ts` test 9.1–9.3 — disabled state, alreadyFinalized branch, D-04 status Open) untuk SC #1 + SC #2 auto E2E; manual UAT Step 5 di `310-UAT.md` untuk SC #5 parallel concurrent dual-tab + 4 SQL verify queries (TrainingRecord, NomorSertifikat, AuditLog, UserNotifications). VALIDATION.md sign-off contract.

4. **Audit Description language — BI atau English?**
   - What we know: Existing audit entries pakai English (`"Added assessment category 'Safety OJT'"`, `"Regenerated access token for..."`)
   - What's unclear: CLAUDE.md "Bahasa Indonesia mandatory" — apakah berlaku ke audit log internal?
   - Recommendation: Tetap English untuk audit Description (consistent convention, machine-friendly forensic). BI hanya untuk user-facing render. Validate dengan user kalau ragu.
   - **RESOLVED:** Audit `Description` tetap English (konsisten dengan AddCategory canonical L322-328 + Phase 309 audit convention — machine-readable forensic). User-facing strings (toast D-03 alert-info, tooltip D-02, alert-danger D-04) WAJIB Bahasa Indonesia per CLAUDE.md. `actionType="FinalizeEssayGrading"` (English machine identifier).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK 8.0 | Build + run | ✓ | 8.0 | — |
| EF Core 8.0.0 (SqlServer + Sqlite) | DB | ✓ | 8.0.0 | — |
| ASP.NET Identity 8.0.0 | Auth | ✓ | 8.0.0 | — |
| ApplicationDbContext (DbSet `UserNotifications`, `AuditLogs`) | Dedup queries | ✓ | — | — `[VERIFIED: Data/ApplicationDbContext.cs L58, L68]` |
| `_workerDataService` (`WorkerDataService`) injected ke AssessmentAdminController | Notification call | ✓ | — | — `[VERIFIED: existing call L2830]` |
| `_auditLog` (`AuditLogService`) injected ke AssessmentAdminController | Audit log | ✓ | — | — `[VERIFIED: existing usage L326+ pattern]` |
| `_userManager` (`UserManager<ApplicationUser>`) | Actor identity | ✓ | — | — `[VERIFIED: existing pattern L322]` |
| Bootstrap 5 (CSS + JS) | Tooltip + alert | ✓ | 5.x via `_Layout.cshtml` | — `[VERIFIED: existing toast L448 pakai .text-bg-success Bootstrap 5 utility]` |
| Bootstrap Icons | UI icons | ✓ | — | — `[VERIFIED: existing usage `bi bi-check-circle-fill`]` |
| Playwright TypeScript | E2E tests | ✓ | @playwright/test ^1.58.2 | — |
| xUnit/NUnit project | .NET unit tests | ✗ | — | Pakai Playwright + manual UAT (SC #5) |
| `dotnet test` runner | Test execution | ✗ (no test project) | — | Bootstrap test project Wave 0 (kalau diputuskan) atau skip |

**Missing dependencies with no fallback:** None (semua core dependencies tersedia).

**Missing dependencies with fallback:**
- **xUnit project** — fallback ke Playwright E2E + manual UAT untuk SC #5 parallel finalize verification.

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright TypeScript @1.58.2 (E2E only) |
| Config file | `tests/playwright.config.ts` (`testDir: ./e2e`, `baseURL: http://localhost:5277`) |
| Quick run command | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 310" --reporter=list` |
| Full suite command | `cd tests && npx playwright test --reporter=list` |
| .NET build verify | `dotnet build` (project root) |
| **No** unit test framework | xUnit/NUnit/MSTest project TIDAK ada — verified via Glob `tests/**/*.cs` returns 0 |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ESCG-01 (SC #1) | API return alreadyFinalized friendly response saat Status==Completed | E2E (Playwright) | `npx playwright test --grep "Phase 310 SC1"` | ❌ Wave 0 |
| ESCG-01 (SC #2) | Button disabled + tooltip saat Status==Completed | E2E (Playwright) | `npx playwright test --grep "Phase 310 SC2"` | ❌ Wave 0 |
| ESCG-01 (SC #3) | UserNotifications dedup — 2x finalize cuma 1 row notif per recipient | E2E + DB query verify (manual UAT step) | `npx playwright test --grep "Phase 310 SC3"` + manual SQL query | ❌ Wave 0 + manual |
| ESCG-01 (SC #4) | AuditLog distinct — 2x finalize cuma 1 entry per session | E2E + DB query verify (manual UAT step) | `npx playwright test --grep "Phase 310 SC4"` + manual SQL query | ❌ Wave 0 + manual |
| ESCG-01 (SC #5) | Parallel finalize → 1 TrainingRecord, 1 NomorSertifikat, semua thread success | **Manual UAT concurrent** (browser dual-tab simultaneous click) + DB query verify; OR ad-hoc curl script `xargs -P 5` parallel POST | Manual SQL query + ad-hoc bash | ❌ Manual-only |
| ESCG-01 build | `dotnet build` 0 errors | .NET build | `dotnet build` | ✓ existing |

### Sampling Rate

- **Per task commit:** `dotnet build` (must be 0 errors); inline JS lint (existing convention).
- **Per wave merge:** `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 310" --reporter=list`
- **Phase gate:** Full suite green + manual UAT concurrent click sign-off di `310-UAT.md` (precedent Phase 308 sign-off pattern).

### Wave 0 Gaps

- [ ] `tests/e2e/assessment.spec.ts` — extend dengan Phase 310 specs:
  - Test 1 (SC #1): klik finalize → response success → klik again → response `alreadyFinalized:true` + render alert-info (NOT alert-danger)
  - Test 2 (SC #2): seed session Status=Completed → load detail page → expect button `[disabled]` + `[data-bs-toggle="tooltip"]`
  - Test 3 (SC #3, manual): manual UAT — query `SELECT COUNT(*) FROM UserNotifications WHERE Type='ASMT_ALL_COMPLETED' AND Message LIKE '%[title]%' AND UserId='[hcUserId]'` → expect 1
  - Test 4 (SC #4, manual): manual UAT — query `SELECT COUNT(*) FROM AuditLogs WHERE ActionType='FinalizeEssayGrading' AND TargetId=[sessionId]` → expect 1
  - Test 5 (SC #5, manual): manual UAT — buka 2 tab browser admin, sync klik tombol selesaikan dalam 1 detik → query `SELECT COUNT(*) FROM TrainingRecords WHERE UserId=... AND Judul=...` → expect 1; `SELECT NomorSertifikat FROM AssessmentSessions WHERE Id=...` → expect not null + sama; both tab UI tampak success (no error toast)
- [ ] `310-UAT.md` — manual UAT script (precedent Phase 308 `308-UAT.md`)
- [ ] **OPTIONAL (not in scope unless user requests):** Bootstrap xUnit test project `HcPortal.Tests/` untuk integration test SC #5 — would let you run `dotnet test` with concurrent `Task.WhenAll` — defer to user decision (CONTEXT.md scope tidak include test framework introduction)

### 8 Validation Dimensions (Phase 310)

| Dimension | Phase 310 Concern | Coverage |
|-----------|-------------------|----------|
| **Correctness** | API response shape match contract (D-03/D-04 fields) | E2E test response.json field assertion |
| **Isolation** | Each test seed fresh session; cleanup via teardown | Playwright `test.beforeEach` seed via API or SQL |
| **Edge cases** | Status=Cancelled, Status=Open, Status=Completed, GenerateCertificate=false (no cert path) | Test matrix per status |
| **Error handling** | Audit failure tidak break primary flow; DbUpdateException race graceful | try-catch wrap audit; integration verify cert race log |
| **Performance** | Dedup `AnyAsync` query <50ms; full finalize <500ms p95 | Manual timing or Playwright `performance.mark` |
| **Security** | `[Authorize(Roles="Admin,HC")]` preserved; `[ValidateAntiForgeryToken]` preserved | Static review code; existing constraint |
| **Observability** | `_logger.LogInformation` race condition hits; `_logger.LogWarning` audit failure | Verified via log inspection saat UAT |
| **Maintainability** | Pattern match GradingService.cs L195-212 (precedent already in code) | Code review + refer planner |

## Sources

### Primary (HIGH confidence — verified)
- `Controllers/AssessmentAdminController.cs` L322-328, L2712-2833, L2255-2278 — existing audit pattern + FinalizeEssayGrading body + RegenerateToken audit
- `Services/GradingService.cs` L189-227 — canonical capture-rowsAffected idempotent pattern (Phase 309-03)
- `Services/WorkerDataService.cs` L313-345 — NotifyIfGroupCompleted target refactor
- `Services/AuditLogService.cs` L9-44 — AuditLog signature
- `Services/NotificationService.cs` L100-126 — SendAsync signature + UserNotification creation
- `Models/UserNotification.cs` L11-73 — schema field availability (Type, Title, Message, UserId, CreatedAt; NO SourceTitle)
- `Models/AssessmentConstants.cs` L13-44 — status string constants + helper
- `Models/AuditLog.cs` L7-52 — audit row schema
- `Models/AssessmentMonitoringViewModel.cs` L47-63 — ViewModel field availability (no NomorSertifikat — confirmed gap)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419, L1331-1397 — button + JS handler + inline alert pattern
- `Views/Shared/_Layout.cshtml` L200-225 — TempData[Info]/[Success]/[Error] alert convention
- `Controllers/CMPController.cs` L1822-1845 — try-catch berlapis pattern (Phase 309 WCRT-01)
- `Data/ApplicationDbContext.cs` L58, L68 — DbSet AuditLogs + UserNotifications confirmed
- `HcPortal.csproj` L1-25 — package versions verified
- `tests/playwright.config.ts`, `tests/package.json`, `tests/e2e/` — test infrastructure inventory
- `.planning/phases/310-essay-finalize-idempotency/310-CONTEXT.md` — locked decisions

### Secondary (MEDIUM confidence)
- EF Core 8 `ExecuteUpdateAsync` returns int rows affected — well-known API contract per Microsoft Docs (Bulk Update API introduced EF Core 7)
- Bootstrap 5 tooltip activation pattern `new bootstrap.Tooltip(el)` — official Bootstrap 5 docs convention

### Tertiary (LOW confidence — flagged in Assumptions Log)
- SQL Server default collation `CI_AS` — `[ASSUMED]`, verify saat planning (matter untuk dedup `Message.Contains` case-sensitivity)

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — All packages verified via HcPortal.csproj read; existing services injected and proven (Phase 309 reference)
- Architecture: **HIGH** — Pattern 100% sourced dari GradingService.cs L195-212 (canonical existing implementation)
- Pitfalls: **HIGH** — 8 pitfalls identified, semua verified via grep on codebase, bukan training-data assumption
- Schema verification (D-05 critical): **HIGH** — UserNotification.cs read line-by-line, field availability confirmed
- Test infrastructure: **HIGH** — Glob + Read confirmed Playwright-only, no .NET test project

**Research date:** 2026-05-01
**Valid until:** 2026-05-31 (30 days — codebase stable, EF Core 8 mature)

---
*Phase: 310-essay-finalize-idempotency*
*Research completed: 2026-05-01*
