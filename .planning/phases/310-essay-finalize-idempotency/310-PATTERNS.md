# Phase 310: Essay Finalize Idempotency - Pattern Map

**Mapped:** 2026-05-02
**Files analyzed:** 5 (4 modified + 1 reference-only)
**Analogs found:** 5 / 5 (100% — all targets memiliki pasangan canonical existing)
**Bahasa:** Bahasa Indonesia untuk user-facing copy (CLAUDE.md mandate)

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` (L2712-2833 `FinalizeEssayGrading`) | controller (HTTP POST handler) | request-response + transactional CRUD with side-effects | `Services/GradingService.cs` L189-227 (essay branch + non-essay branch) | **EXACT** — pattern persis (capture rowsAffected + gate side-effects) |
| `Services/WorkerDataService.cs` (L313-345 `NotifyIfGroupCompleted`) | service (broadcast notifier) | event-driven + dedup CRUD | `Controllers/AssessmentAdminController.cs` L2794-2796 (TrainingRecord `AnyAsync` dedup guard) | **role-match** — pattern AnyAsync-before-insert |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419 (button gate D-02) | view (Razor conditional + Bootstrap tooltip) | render-time conditional | `Views/Admin/AddTraining.cshtml` L394-397 (tooltip init) + existing button hide pattern (`display: none` ternary L415) | **role-match** — Bootstrap tooltip + Razor conditional ternary |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` L1331-1359 (JS handler upgrade D-03/D-04) | view (inline JS fetch handler) | request-response + DOM injection | `Views/Admin/AssessmentMonitoringDetail.cshtml` L1383-1389 (Phase 302 extra-time alert injection — same file) | **EXACT** — pattern di file yang sama, copy verbatim |
| `Models/AssessmentMonitoringViewModel.cs` L47-63 (`MonitoringSessionViewModel` extension) | model (ViewModel) | property bag | `Models/AssessmentMonitoringViewModel.cs` L52-62 (existing properties dengan summary `///`) | **EXACT** — file yang sama, tinggal append 2 properti |
| `Controllers/AssessmentAdminController.cs` L2559-2591 (mapper extension untuk `Status` + `NomorSertifikat`) | controller (LINQ projection mapper) | transform | Same block, existing `IsPassed`/`Score`/`CompletedAt` mapping | **EXACT** — copy line, tambah 2 baris |

---

## Pattern Assignments

### 1. `Controllers/AssessmentAdminController.cs` — `FinalizeEssayGrading` (controller, request-response + atomic CRUD)

**Analog:** `Services/GradingService.cs` L189-227 (essay flow branch — Phase 309-03 canonical)

**Imports pattern** (L1-15 — already present di AssessmentAdminController, no change needed):
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HcPortal.Models;
using HcPortal.Data;
using HcPortal.Services;
using HcPortal.Helpers;
```

**Auth pattern** (L2713-2715 — preserve as-is):
```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> FinalizeEssayGrading(int sessionId)
```

**Constants pattern (WAJIB pakai `AssessmentConstants`)** — replace literal strings di L2719 + L2785 + L2788:
```csharp
// WAJIB — pakai constant, bukan literal
AssessmentConstants.AssessmentStatus.Completed       // = "Completed"
AssessmentConstants.AssessmentStatus.PendingGrading  // = "Menunggu Penilaian"
AssessmentConstants.AssessmentStatus.Open            // = "Open"
AssessmentConstants.AssessmentStatus.Upcoming        // = "Upcoming"
```

**Core pattern (CANONICAL — copy from `Services/GradingService.cs` L195-212):**
```csharp
// Source: Services/GradingService.cs L195-212 — Phase 309-03 verified canonical
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
    return false;   // skip side-effects (audit + cert + notif)
}
```

**Adopt to FinalizeEssayGrading** (replace L2783-2790 — capture return value yang saat ini di-throw away):
```csharp
// PATTERN: capture rowsAffected, gate semua side-effect
var rowsAffected = await _context.AssessmentSessions
    .Where(s => s.Id == sessionId
                && s.Status == AssessmentConstants.AssessmentStatus.PendingGrading)
    .ExecuteUpdateAsync(s => s
        .SetProperty(r => r.Score, finalPercentage)
        .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.Completed)
        .SetProperty(r => r.IsPassed, isPassed)
        .SetProperty(r => r.CompletedAt, DateTime.UtcNow));

if (rowsAffected == 0)
{
    var current = await _context.AssessmentSessions.AsNoTracking()
        .FirstOrDefaultAsync(s => s.Id == sessionId);

    _logger.LogInformation(
        "FinalizeEssayGrading: race lost untuk session {SessionId} — return alreadyFinalized.",
        sessionId);

    return Json(new {
        success = true,
        alreadyFinalized = true,
        message = $"Penilaian sudah diselesaikan sebelumnya pada {current?.CompletedAt:dd MMM yyyy HH:mm} WIB",
        score = current?.Score,
        isPassed = current?.IsPassed,
        nomorSertifikat = current?.NomorSertifikat
    });
}
// Audit + cert + notif gated by rowsAffected > 0 (otomatis — di bawah block ini)
```

**D-03 status branching pattern (di awal method, sebelum recalc score)** — pakai `switch` expression untuk per-status BI message:
```csharp
// D-03: friendly no-op kalau sudah Completed (LOCKED — replace L2719 single-line check)
if (session.Status == AssessmentConstants.AssessmentStatus.Completed)
{
    return Json(new {
        success = true,
        alreadyFinalized = true,
        message = $"Penilaian sudah diselesaikan sebelumnya pada {session.CompletedAt:dd MMM yyyy HH:mm} WIB",
        score = session.Score,
        isPassed = session.IsPassed,
        nomorSertifikat = session.NomorSertifikat
    });
}

// D-04: pesan spesifik per status non-PendingGrading (LOCKED literals dari CONTEXT.md D-04)
if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)
{
    var msg = session.Status switch
    {
        AssessmentConstants.AssessmentStatus.Open => "Belum bisa di-finalize. Peserta belum mulai mengerjakan ujian.",
        "InProgress" => "Belum bisa di-finalize. Peserta sedang mengerjakan ujian.",
        "Cancelled"  => "Tidak bisa di-finalize. Session sudah dibatalkan.",
        _            => $"Tidak bisa di-finalize. Status saat ini: {session.Status}."
    };
    return Json(new { success = false, message = msg });
}
```

**Audit log pattern (D-07)** — copy dari `Controllers/AssessmentAdminController.cs` L322-328 (`AddCategory`):
```csharp
// Source: Controllers/AssessmentAdminController.cs L322-328 — proven existing pattern
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";
await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "AddCategory",
    $"Added assessment category '{category.Name}' (DefaultPass: {category.DefaultPassPercentage}%)",
    category.Id, "AssessmentCategory");
```

**Adopt to FinalizeEssayGrading** (after `if (rowsAffected > 0)` gate, before notify call):
```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";

try
{
    await _auditLog.LogAsync(currentUser?.Id ?? "", actorName, "FinalizeEssayGrading",
        $"Session {sessionId} ({session.Title}) finalized: score={finalPercentage}%, isPassed={isPassed}",
        sessionId, "AssessmentSession");
}
catch (Exception ex)
{
    // Audit failure tidak boleh break primary flow (precedent Phase 306 D-10)
    _logger.LogWarning(ex, "FinalizeEssayGrading: audit log failed for session {SessionId}", sessionId);
}
```

**Cert WHERE-clause guard (existing — preserve L2812-2825 as-is):**
```csharp
// Source: Controllers/AssessmentAdminController.cs L2812-2825 — already idempotent
if (session.GenerateCertificate && isPassed)
{
    var certNow = DateTime.Now;
    int certYear = certNow.Year;
    try
    {
        var nextSeq = await CertNumberHelper.GetNextSeqAsync(_context, certYear);
        await _context.AssessmentSessions
            .Where(s => s.Id == sessionId && s.NomorSertifikat == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.NomorSertifikat, CertNumberHelper.Build(nextSeq, certNow)));
    }
    catch (DbUpdateException) { /* race-condition: cert sudah diambil thread lain — skip */ }
}
```

**TrainingRecord AnyAsync guard (existing — preserve L2792-2809):**
```csharp
// Source: Controllers/AssessmentAdminController.cs L2794-2796 — already idempotent
var judul = $"Assessment: {session.Title}";
bool trExists = await _context.TrainingRecords.AnyAsync(t =>
    t.UserId == session.UserId && t.Judul == judul && t.Tanggal == session.Schedule);
if (!trExists) { /* insert TrainingRecord */ }
```

---

### 2. `Services/WorkerDataService.cs` — `NotifyIfGroupCompleted` (service, event-driven dedup)

**Analog:** `Controllers/AssessmentAdminController.cs` L2794-2796 (TrainingRecord `AnyAsync` dedup guard, same file as caller)

**Imports pattern** (L1-4 — already present, no change):
```csharp
using HcPortal.Data;
using HcPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;   // AnyAsync extension
```

**DI constructor (`_context` already injected — verified L10-25):**
```csharp
private readonly ApplicationDbContext _context;             // ✓ tersedia
private readonly INotificationService _notificationService; // ✓ tersedia
private readonly ILogger<WorkerDataService> _logger;        // ✓ tersedia
```

**Schema reference — `Models/UserNotification.cs` L11-65 (verified field availability):**
```csharp
public string UserId { get; set; }            // [Required] — dedup key 1
public string Type { get; set; }              // [Required, MaxLength 50] — dedup key 2 ("ASMT_ALL_COMPLETED")
public string Title { get; set; }             // [Required, MaxLength 200] — dedup key 3 ("Assessment Selesai")
public string Message { get; set; }           // [Required] — interpolated dengan completedSession.Title
public DateTime CreatedAt { get; set; }       // optional time-window guard
// NOT AVAILABLE: SourceTitle, SourceDate, AssessmentSessionId, Metadata
```

**Core dedup pattern (D-05) — replace L331 inner foreach body:**
```csharp
// BEFORE (L331-344) — current code, no dedup:
foreach (var recipientId in recipientIds)
{
    try
    {
        await _notificationService.SendAsync(
            recipientId,
            "ASMT_ALL_COMPLETED",
            "Assessment Selesai",
            $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian",
            "/CMP/Assessment"
        );
    }
    catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
}

// AFTER — D-05 dedup via AnyAsync lookup BEFORE SendAsync:
foreach (var recipientId in recipientIds)
{
    // PATTERN: AnyAsync-before-insert (analog: TrainingRecord guard di AssessmentAdminController L2794-2796)
    bool alreadySent = await _context.UserNotifications.AnyAsync(n =>
        n.UserId == recipientId
        && n.Type == "ASMT_ALL_COMPLETED"
        && n.Title == "Assessment Selesai"
        && n.Message.Contains(completedSession.Title)
        && n.CreatedAt >= completedSession.Schedule.Date);   // time-window guard cegah cross-session false-positive

    if (alreadySent)
    {
        _logger.LogInformation(
            "NotifyIfGroupCompleted: skip recipient {RecipientId} — sudah ada notif untuk session {Title}",
            recipientId, completedSession.Title);
        continue;
    }

    try
    {
        await _notificationService.SendAsync(
            recipientId,
            "ASMT_ALL_COMPLETED",
            "Assessment Selesai",
            $"Semua peserta assessment \"{completedSession.Title}\" telah menyelesaikan ujian",
            "/CMP/Assessment"
        );
    }
    catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
}
```

**Caveat (RESEARCH.md A1):** `Message.Contains` translate ke `LIKE '%title%'` di SQL Server (collation `CI_AS` default → case-insensitive). Time-window guard `CreatedAt >= Schedule.Date` cegah false-positive lintas hari.

---

### 3. `Views/Admin/AssessmentMonitoringDetail.cshtml` L414-419 (Razor conditional disabled + tooltip)

**Analog (button hide existing pattern — same file L415):**
```cshtml
<!-- Source: Views/Admin/AssessmentMonitoringDetail.cshtml L414-419 — current (no D-02 gate yet) -->
<div id="finalizeSection_@session.Id"
     style="display: @(session.EssayPendingCount == 0 ? "block" : "none")">
    <button class="btn btn-success btn-finalize-grading" data-session-id="@session.Id">
        <i class="bi bi-check-circle me-1"></i>Selesaikan Penilaian
    </button>
</div>
```

**Analog (tooltip init pattern — `Views/Admin/AddTraining.cshtml` L394-397):**
```js
// Source: Views/Admin/AddTraining.cshtml L394-397 — copy verbatim
document.addEventListener('DOMContentLoaded', function() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function(el) { new bootstrap.Tooltip(el); });
});
```

**Adopt to L414-419 (D-02 disabled + tooltip):**
```cshtml
@{
    // D-02: button gate kondisi — pakai AssessmentConstants, BUKAN literal
    var isFinalized = session.Status == AssessmentConstants.AssessmentStatus.Completed;
    // NOTE: gate purely by Status==Completed (RESEARCH Recommendation b — sufficient untuk SC #2)
    // NomorSertifikat null saat Completed-Failed-no-cert masih = sudah finalized
}
<div id="finalizeSection_@session.Id"
     style="display: @(session.EssayPendingCount == 0 ? "block" : "none")">
    <button class="btn btn-success btn-finalize-grading"
            data-session-id="@session.Id"
            @(isFinalized ? "disabled" : "")
            @(isFinalized && session.CompletedAt.HasValue
                ? $"title=\"Sudah selesai pada {session.CompletedAt.Value:dd MMM yyyy HH:mm} WIB\""
                : "")
            @(isFinalized ? "data-bs-toggle=\"tooltip\"" : "")>
        <i class="bi bi-check-circle me-1"></i>Selesaikan Penilaian
    </button>
</div>
```

**Pitfall #6 (RESEARCH.md):** Native `<button disabled>` skip mouseenter event — tooltip TIDAK fire. Mitigation pilihan:
- (a) Wrap button dalam `<span data-bs-toggle="tooltip" title="...">` parent (tooltip attached to wrapper).
- (b) Pakai `aria-disabled="true"` + CSS `pointer-events:none` + class custom (bukan native disabled).

Recommended: pilih (a) — wrap dengan `<span>` untuk preserve tooltip behavior (planner verify saat impl).

---

### 4. `Views/Admin/AssessmentMonitoringDetail.cshtml` L1331-1359 (JS handler upgrade D-03/D-04)

**Analog (CANONICAL — same file L1383-1389, Phase 302 extra-time alert injection):**
```js
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml L1383-1389 — copy verbatim
var alertClass = data.success ? 'alert-success' : 'alert-danger';
var iconClass = data.success ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';
var alertHtml = '<div class="alert ' + alertClass + ' alert-dismissible fade show mb-3" role="alert">'
    + '<i class="bi ' + iconClass + ' me-2"></i>' + data.message
    + '<button type="button" class="btn-close" data-bs-dismiss="alert"></button></div>';
var alertContainer = document.querySelector('.container-fluid');
alertContainer.insertAdjacentHTML('afterbegin', alertHtml);
```

**Extract sebagai helper `showAlert(type, icon, message)` di top of `<script>` block:**
```js
// NEW helper — extract dari L1383-1389 pattern
function showAlert(type, icon, message) {
    var html = '<div class="alert alert-' + type + ' alert-dismissible fade show mb-3" role="alert">'
        + '<i class="bi ' + icon + ' me-2"></i>' + message
        + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>'
        + '</div>';
    var container = document.querySelector('.container-fluid');
    container.insertAdjacentHTML('afterbegin', html);

    // Auto-dismiss 5s untuk info, 7s untuk error (UI-SPEC contract)
    var dismissMs = type === 'danger' ? 7000 : 5000;
    setTimeout(function() {
        var alertEl = container.querySelector('.alert.alert-' + type);
        if (alertEl) {
            var closeBtn = alertEl.querySelector('.btn-close');
            if (closeBtn) closeBtn.click();
        }
    }, dismissMs);
}
```

**Adopt to L1347-1357 (handler upgrade — D-03 alreadyFinalized + D-04 error toast):**
```js
// BEFORE (L1347-1357):
const data = await res.json();
if (data.success) {
    location.reload();
} else {
    alert(data.message);
    this.disabled = false;
}

// AFTER — D-03 + D-04 branching:
const data = await res.json();
if (data.success && data.alreadyFinalized) {
    // D-03: friendly no-op — info toast biru, JANGAN reload (state sudah final)
    var msg = data.message;
    if (data.nomorSertifikat) {
        msg += ' Nomor sertifikat: ' + data.nomorSertifikat + '.';
    }
    showAlert('info', 'bi-info-circle-fill', '<strong>Info:</strong> ' + msg);
    this.disabled = true;   // tetap disabled — sudah final
} else if (data.success) {
    location.reload();
} else {
    // D-04: error spesifik per status (Open/InProgress/Cancelled)
    showAlert('danger', 'bi-x-circle-fill', '<strong>Error:</strong> ' + data.message);
    this.disabled = false;
}
```

**Tooltip activation snippet (add bottom of `<script>` block ~L1397):**
```js
// Source: Views/Admin/AddTraining.cshtml L394-397 — copy
document.addEventListener('DOMContentLoaded', function() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function(el) { new bootstrap.Tooltip(el); });
});
```

---

### 5. `Models/AssessmentMonitoringViewModel.cs` L47-63 (`MonitoringSessionViewModel` extension)

**Analog (same file L52-62 existing properties):**
```csharp
// Source: Models/AssessmentMonitoringViewModel.cs L47-63 — existing pattern
public class MonitoringSessionViewModel
{
    public int Id { get; set; }
    public string UserFullName { get; set; } = "";
    public string UserNIP { get; set; } = "";
    public string UserStatus { get; set; } = "";    // "Not started", "In Progress", "Abandoned", or "Completed"
    public int? Score { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public int QuestionCount { get; set; }
    public int DurationMinutes { get; set; }

    // Essay grading support (Phase 298-05)
    public bool HasManualGrading { get; set; }
    public int EssayPendingCount { get; set; }
}
```

**Adopt — append 2 properti baru (UI-SPEC L171-175):**
```csharp
// Phase 310 D-02 — gate button finalize berdasarkan Status assessment session
public string Status { get; set; } = "";               // mirror AssessmentSession.Status (raw, BUKAN UserStatus yang sudah remap)
public string? NomorSertifikat { get; set; }           // mirror AssessmentSession.NomorSertifikat (nullable)
```

**Mapper extension (`Controllers/AssessmentAdminController.cs` L2573-2587)** — append 2 baris:
```csharp
// Source: Controllers/AssessmentAdminController.cs L2573-2587 (existing mapper)
return new MonitoringSessionViewModel
{
    Id           = a.Id,
    UserFullName = a.User?.FullName ?? "Unknown",
    UserNIP      = a.User?.NIP ?? "",
    UserStatus   = userStatus,
    Score        = a.Score,
    IsPassed     = a.IsPassed,
    CompletedAt  = a.CompletedAt,
    StartedAt    = a.StartedAt,
    QuestionCount = questionCountMap.TryGetValue(a.Id, out var qc) ? qc : 0,
    DurationMinutes = a.DurationMinutes,
    HasManualGrading = a.HasManualGrading,
    EssayPendingCount = essayPendingCountMap.TryGetValue(a.Id, out var ep) ? ep : 0,
    // Phase 310 D-02 — append
    Status       = a.Status ?? "",
    NomorSertifikat = a.NomorSertifikat
};
```

---

## Shared Patterns

### A. Constants Usage (Phase 309 lock — WAJIB)

**Source:** `Models/AssessmentConstants.cs` L13-19, L43-44
**Apply to:** SEMUA file yang baca/tulis status assessment session

```csharp
// WAJIB — pakai constant, BUKAN literal "Completed" / "Menunggu Penilaian"
AssessmentConstants.AssessmentStatus.Completed
AssessmentConstants.AssessmentStatus.PendingGrading
AssessmentConstants.AssessmentStatus.Open
AssessmentConstants.AssessmentStatus.Upcoming

// Helper (opsional, kalau perlu cek "submitted" semantic)
AssessmentConstants.IsAssessmentSubmitted(status)  // returns true untuk Completed OR PendingGrading
```

**Lokasi yang harus di-refactor opportunistic (Phase 310 scope tidak wajib tapi recommended):**
- `Controllers/AssessmentAdminController.cs` L2719 — literal `"Menunggu Penilaian"` → constant
- `Controllers/AssessmentAdminController.cs` L2785 + L2788 — literal `"Menunggu Penilaian"` + `"Completed"` → constant

---

### B. Capture-and-Gate Idempotent ExecuteUpdateAsync

**Source:** `Services/GradingService.cs` L195-212 (canonical) + L231-248 (non-essay branch)
**Apply to:** SEMUA operation dengan side-effect (audit, cert generation, notification) yang bisa di-trigger paralel

```csharp
var rowsAffected = await _context.{Entity}
    .Where(e => e.Id == id && e.Status == ExpectedStatus)   // WHERE-clause = atomic guard
    .ExecuteUpdateAsync(e => e.SetProperty(...));

if (rowsAffected == 0)
{
    _logger.LogInformation("Race condition entity {Id} — skip side-effects.", id);
    return /* friendly no-op response */;
}
// Side-effects HANYA jalan kalau rowsAffected > 0
```

**Apply to Phase 310:**
- `FinalizeEssayGrading` L2783-2790 (status update)
- `FinalizeEssayGrading` L2819-2822 (cert NomorSertifikat — already idempotent via `WHERE NomorSertifikat == null`)

---

### C. AnyAsync Dedup Before Insert

**Source:** `Controllers/AssessmentAdminController.cs` L2794-2796 (TrainingRecord guard, existing canonical)
**Apply to:** SEMUA insert yang bisa di-trigger paralel + tidak punya UNIQUE constraint DB

```csharp
bool exists = await _context.{Entity}.AnyAsync(e =>
    e.{Key1} == value1 && e.{Key2} == value2 && e.{Key3} == value3);
if (exists) { /* skip insert */ continue; }
// Insert ...
```

**Apply to Phase 310:**
- `WorkerDataService.NotifyIfGroupCompleted` L331 — dedup `UserNotifications.AnyAsync` per recipient (D-05)

---

### D. Audit Log Convention (existing — preserve format)

**Source:** `Controllers/AssessmentAdminController.cs` L322-328 (AddCategory pattern)
**Apply to:** SEMUA write action yang butuh forensic audit trail

```csharp
var currentUser = await _userManager.GetUserAsync(User);
var actorName = string.IsNullOrWhiteSpace(currentUser?.NIP)
    ? (currentUser?.FullName ?? "Unknown")
    : $"{currentUser.NIP} - {currentUser.FullName}";

await _auditLog.LogAsync(
    currentUser?.Id ?? "",
    actorName,
    "{ActionType}",                                            // English machine-readable
    $"{Description with key context}",                          // English (consistent existing)
    {targetId},
    "{TargetType}");
```

**Apply to Phase 310:**
- `FinalizeEssayGrading` (D-07) — `actionType="FinalizeEssayGrading"`, `description="Session {sessionId} ({title}) finalized: score={pct}%, isPassed={bool}"`, `targetType="AssessmentSession"`
- WAJIB di-wrap try-catch dengan `_logger.LogWarning` — audit failure tidak boleh break primary flow (Assumption A5)

---

### E. Inline Alert Injection (AJAX response feedback)

**Source:** `Views/Admin/AssessmentMonitoringDetail.cshtml` L1383-1389 (Phase 302 extra-time, same file)
**Apply to:** SEMUA AJAX handler yang butuh feedback ke user (BUKAN page reload)

```js
function showAlert(type, icon, message) {
    var html = '<div class="alert alert-' + type + ' alert-dismissible fade show mb-3" role="alert">'
        + '<i class="bi ' + icon + ' me-2"></i>' + message
        + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>'
        + '</div>';
    document.querySelector('.container-fluid').insertAdjacentHTML('afterbegin', html);
}
```

**Apply to Phase 310:**
- D-03 alreadyFinalized → `showAlert('info', 'bi-info-circle-fill', ...)` (biru muda)
- D-04 status non-terminal → `showAlert('danger', 'bi-x-circle-fill', ...)` (merah)
- Auto-dismiss 5s (info) / 7s (danger) per UI-SPEC

**ANTI-PATTERN (RESEARCH Pitfall #8):** JANGAN pakai `TempData["Info"]` untuk D-03 — TempData hanya render setelah full page reload, response AJAX tidak trigger reload (alreadyFinalized branch tidak `location.reload()`).

---

### F. Bootstrap Tooltip Activation

**Source:** `Views/Admin/AddTraining.cshtml` L394-397 (canonical — copy verbatim)
**Apply to:** SEMUA view yang punya elemen `[data-bs-toggle="tooltip"]`

```js
document.addEventListener('DOMContentLoaded', function() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function(el) { new bootstrap.Tooltip(el); });
});
```

**Apply to Phase 310:**
- `AssessmentMonitoringDetail.cshtml` bottom of `<script>` block (~L1397) — untuk tooltip pada disabled finalize button (D-02)

**Caveat:** Tooltip pada native `<button disabled>` TIDAK fire mouseenter (Pitfall #6). Wrap dengan `<span>` parent ATAU pakai `aria-disabled` + CSS `pointer-events:none`.

---

## No Analog Found

Tidak ada — semua file Phase 310 punya analog di codebase existing. Phase 310 = ZERO new abstractions, 100% composition existing patterns (per RESEARCH key insight).

---

## Cross-Cutting Concerns

### Bahasa Indonesia (CLAUDE.md mandate)

**Apply to:** SEMUA user-facing copy (toast message, tooltip, error message, audit Description = optional English)

| Surface | Language | Source |
|---------|----------|--------|
| Tooltip text (D-02) | Bahasa Indonesia | UI-SPEC L108: `"Sudah selesai pada {dd MMM yyyy HH:mm} WIB"` |
| Toast info (D-03) | Bahasa Indonesia | UI-SPEC L119: `"Penilaian sudah diselesaikan sebelumnya pada {dd MMM yyyy HH:mm} WIB"` |
| Toast error (D-04) | Bahasa Indonesia | UI-SPEC L135-138 (4 strings literal LOCKED) |
| Confirm dialog (existing L1333) | Bahasa Indonesia | EXISTING — preserve as-is |
| Audit Description | English (consistent existing convention) | RESEARCH Open Question #4 — recommendation tetap English |
| `_logger.LogXxx` template | English (machine-readable) | EXISTING convention |

### Format Tanggal (WIB convention Phase 304)

**Apply to:** SEMUA tanggal user-facing

```csharp
// C# Razor / Controller
$"{dateTime:dd MMM yyyy HH:mm} WIB"
// → "02 Mei 2026 14:35 WIB" (BI culture-aware bila culture=id-ID)
```

---

## Metadata

**Analog search scope:**
- `Controllers/` (specifically AssessmentAdminController.cs canonical)
- `Services/` (GradingService, WorkerDataService, AuditLogService, NotificationService)
- `Models/` (AssessmentConstants, UserNotification, AssessmentMonitoringViewModel)
- `Views/Admin/` (AssessmentMonitoringDetail, AddTraining, AddManualAssessment, EditManualAssessment)
- `Views/Shared/_Layout.cshtml` (TempData alert convention reference)
- `Views/CDP/CoachingProton.cshtml` (tooltip re-init pattern reference)

**Files scanned:** ~12 source files + 4 planning docs (CONTEXT, RESEARCH, UI-SPEC, CLAUDE.md)
**Pattern extraction date:** 2026-05-02
**Phase context:** 310-essay-finalize-idempotency
**Bahasa instruction:** id (Bahasa Indonesia mandatory user-facing)

---

*Phase: 310-essay-finalize-idempotency*
*Pattern mapping completed: 2026-05-02*
