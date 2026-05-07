# Phase 312: Admin Full-Delete Assessment Room - Research

**Researched:** 2026-05-07
**Domain:** ASP.NET Core MVC role-tier guard + Bootstrap 5 modal + AuditLog convention
**Confidence:** HIGH (mayoritas finding terverifikasi langsung di file repo)
**Researcher input:** integrated dengan CONTEXT.md (4 keputusan locked)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01 — HC button visibility = HIDE entirely.** Tombol Hapus untuk role HC tidak di-render sama sekali jika `assessment.Status == "Completed"` ATAU `responseCount > 0`. Pakai pattern `@if (User.IsInRole("Admin") || canHcDelete)`. Tidak disabled-with-tooltip.
- **D-02 — Confirm dialog = 2-step modal dengan impact preview.** Step 1: tampilkan Status, jumlah peserta dengan response, jumlah sertifikat affected, jumlah packages, jumlah attempt history (untuk PrePost: kedua sesi terpisah). Step 2: tombol "Hapus permanen" + warning "Tidak bisa di-undo" + cascade enumeration. Replace `onclick="return confirm(...)"` existing.
- **D-03 — AuditLog log failed attempts.** Action name `"DeleteAssessmentBlocked"` / `"DeleteAssessmentGroupBlocked"` / `"DeletePrePostGroupBlocked"`. Description: actor NIP+name, target SessionId, reason (Status atau ResponseCount yg trigger), endpoint. Successful entries: tetap action name lama tapi description sertakan `Status=...` dan `ResponseCount=...`.
- **D-04 — Scope = 3 method delete.** `DeleteAssessment()` (line 2022), `DeleteAssessmentGroup()` (line 2127), `DeletePrePostGroup()` (line 2232). Smoke test 6 skenario (5 SC + 1 Pre-Post path).

### Claude's Discretion

- Exact modal markup (Bootstrap 5 modal vs custom — tetap ikut existing convention).
- Helper method untuk impact summary — inline di partial via ViewBag vs extract ke action endpoint JSON.
- Helper method `IsAdmin()` — inline `User.IsInRole("Admin")` vs private method DRY.
- Exact text wording confirm dialog (Bahasa Indonesia, formal-ish, ikut tone Phase 304 polish).

### Deferred Ideas (OUT OF SCOPE)

- Soft delete / archive flag (cascade hard-delete tetap)
- Restore / undo functionality (sengaja irreversible)
- Worker / Coach role permissions (hanya Admin & HC tier)
- Phase 313 integration test sharing
- Bulk delete UI

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DEL-01 | Akun **Admin** dapat full-delete assessment room termasuk yang berstatus `Completed` dan/atau yang sudah ada response peserta. Role **HC** dilarang menghapus assessment Completed atau yang sudah punya response peserta. AuditLog entry sertakan Status & ResponseCount. | Body method guard di 3 controller actions (verified line 2022/2127/2232), UI conditional render di `_AssessmentGroupsTab.cshtml:240-275` (verified), AuditLog signature stable verified di `Services/AuditLogService.cs:21`, response count via `_context.PackageUserResponses.Where(r => r.AssessmentSessionId == id).CountAsync()` (existing pattern di line 2049-2051). |

</phase_requirements>

## Project Constraints (from CLAUDE.md)

- **Bahasa Indonesia wajib** untuk semua text user-facing (modal, TempData messages, error messages).
- **Develop workflow lokal → Dev (10.55.3.3) → Production.** Verifikasi lokal `dotnet build` + `dotnet run` (`http://localhost:5277`) sebelum commit. Promosi ke Dev/Prod = tanggung jawab Team IT.
- **Tidak boleh edit kode/DB langsung di server Dev/Prod.** Tidak push tanpa verifikasi lokal.
- **Tidak ada migration DB di Phase 312** (cascade hard-delete tetap, tidak ubah schema).

---

## Phase Summary

Phase 312 menambah role-tier guard di body 3 method delete (`DeleteAssessment`, `DeleteAssessmentGroup`, `DeletePrePostGroup` di `Controllers/AssessmentAdminController.cs`) tanpa mengubah `[Authorize(Roles = "Admin, HC")]` attribute. Admin override status guard, HC blocked dari Completed/with-response. Bersamaan: refactor 2 form `<form onclick="return confirm(...)">` di `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` jadi modal Bootstrap 5 2-step (impact preview → final confirm) menggantikan plain `confirm()` browser native. AuditLog di-extend untuk sukses (sertakan `Status=` + `ResponseCount=`) DAN failure (action `{Action}Blocked`). Smoke test 6 skenario via Playwright existing infrastructure di `tests/e2e/`.

**Primary recommendation:** Ikuti precedent `akhiriSemuaModal` di `Views/Admin/AssessmentMonitoringDetail.cshtml:501-537` + `GetAkhiriSemuaCounts` action di `Controllers/AssessmentAdminController.cs:3419-3435`. Pattern ini SUDAH exist di repo, sudah teruji untuk impact-preview-via-AJAX modal flow. Tidak ada hal greenfield — Phase 312 = compose existing patterns + add 3 guard checks + 3 audit failure entries.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Role tier guard (Admin override, HC reject) | API / Backend (Controller body) | — | Authorization decision wajib server-side untuk tahan DevTools bypass; D-01 UI hide sekadar UX. Body method, bukan attribute (per SC #2). |
| AuditLog success + failure entries | API / Backend (`AuditLogService`) | — | Audit integrity = single source of truth, server-side write. Tidak boleh client-side. |
| Cascade delete (PackageUserResponses + AttemptHistory + Packages + Sessions) | API / Backend (EF Core RemoveRange) | Database (Cascade FK on UserPackageAssignments) | Existing pattern, verified line 2048-2089. **Tidak diubah** — guard ditambah SEBELUM cascade block dimulai. |
| UI conditional render (Hapus button hide/show) | Frontend Server (Razor partial) | — | Server-rendered Razor — `User.IsInRole("Admin")` dievaluasi server-side saat render `_AssessmentGroupsTab.cshtml`. |
| Impact preview data computation | API / Backend (new HttpGet endpoint) | Frontend Browser (AJAX fetch + render modal body) | Pattern parity dengan `GetAkhiriSemuaCounts` (line 3419-3435) — backend compute via EF Core, frontend fetch on `show.bs.modal` event. |
| Modal markup + 2-step UX | Frontend Browser (Bootstrap 5 + vanilla JS) | — | Pure UI orchestration. Pakai `bootstrap.Modal` API native, pattern parity dengan `editScoreWarningModal` (Phase 306) + `akhiriSemuaModal`. |
| AntiForgeryToken propagation | Frontend Server (Razor) | Frontend Browser (form POST) | Existing forms pakai `@Html.AntiForgeryToken()` — modal-based flow tetap submit via hidden `<form>` (bukan AJAX POST), token otomatis ikut. Tidak perlu CoachingProton-style JS token harvesting. |
| Smoke test 6 skenario | Test Layer (Playwright) | — | Existing `tests/e2e/assessment.spec.ts` infrastructure, login helper sudah support 2 role (`admin`, `hc`). |

## Standard Stack

### Core (sudah ada di project — TIDAK install lib baru)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | .NET 8 | Controller actions, Razor partial render, Authorize attribute | Stack utama project [VERIFIED: Controllers/AssessmentAdminController.cs] |
| EF Core | 8 | Query response count, cascade RemoveRange | Stack utama, AsNoTracking dipakai di Phase 311 [VERIFIED: line 124] |
| Bootstrap 5 | included via _Layout | Modal markup, btn classes, alert | Existing infrastructure di `Views/Shared/_Layout.cshtml` [VERIFIED: 18 file pakai `modal fade`] |
| bootstrap-icons | included | `bi-trash`, `bi-exclamation-triangle`, `bi-info-circle` | Existing convention [VERIFIED] |
| Playwright | ^1.58.2 | Smoke test 6 skenario (admin+open OK / admin+completed OK / hc+open-no-response OK / hc+open-with-response BLOCK / hc+completed BLOCK / hc+prepost+completed BLOCK) | Existing infrastructure di `tests/` [VERIFIED: tests/package.json] |

### Supporting (existing services)

| Service / Helper | Location | Purpose |
|---|---|---|
| `AuditLogService` | `Services/AuditLogService.cs:9` | DI-injected, signature stable: `LogAsync(actorUserId, actorName, actionType, description, targetId?, targetType?)` |
| `_userManager.GetUserAsync(User)` | DI in controller | Get current user untuk actor info; pattern `NIP - FullName` [VERIFIED line 2097, 2202, 2294] |
| `TempData["Success"]` / `TempData["Error"]` | Razor flash convention | Display banner setelah RedirectToAction; pattern existing [VERIFIED 8 occurrences di 3 method] |
| `accounts` test helpers | `tests/helpers/accounts.ts` | `admin` + `hc` keys sudah tersedia [VERIFIED lines 2-3] |

### Alternatives Considered (REJECTED — sudah dilock di CONTEXT)

| Instead of | Could Use | Why Rejected |
|------------|-----------|---------------|
| HC hide button | Disabled button + tooltip | D-01 locked: hide entirely (cleaner UX) |
| 2-step modal | Native browser `confirm()` | D-02 locked: impact preview wajib (destructive transparency) |
| Audit only success | Skip blocked attempts | D-03 locked: log YA (compliance audit) |
| 2-method scope | Skip `DeletePrePostGroup` | D-04 locked: include 3 method (security gap closure) |

**Installation:** TIDAK ADA NuGet/npm package baru. Repo sudah memenuhi requirements.

---

## Architecture Patterns

### System Architecture Diagram (Phase 312 data flow)

```
[HC user clicks "Hapus" dropdown item]
    ↓
[Razor server-side: @if (User.IsInRole("Admin") || canHcDelete)]
    ↓ (HC + Completed/has-response → button NOT rendered → end)
    ↓ (Admin OR HC+open-no-response → render <button data-bs-toggle="modal">)
    ↓
[User clicks Hapus button → bootstrap.Modal.show()]
    ↓
[show.bs.modal event handler → fetch /Admin/GetDeleteImpact?type=...&id=...]
    ↓
[Backend HttpGet GetDeleteImpact: AsNoTracking query →
    return Json({ status, responseCount, certCount, packageCount, attemptCount,
                  sessions: [{label, status, responseCount}] for PrePost })]
    ↓
[Frontend populates Step 1 modal body with impact summary]
    ↓
[User clicks "Lanjutkan" → swap to Step 2 panel (warning + final cascade text)]
    ↓
[User clicks "Hapus permanen" → JS submits hidden <form> via .submit()]
    ↓ (carries @Html.AntiForgeryToken() → POST /Admin/DeleteAssessment etc)
    ↓
[Backend POST action body:
    1. Find session (existing)
    2. NEW: GuardCanDelete(session, responseCount, User) →
        if blocked: AuditLog "{Action}Blocked", TempData["Error"], redirect
    3. Cascade RemoveRange (UNCHANGED) → SaveChangesAsync
    4. AuditLog "{Action}" with Status=X, ResponseCount=Y in description
    5. TempData["Success"], redirect ManageAssessment]
    ↓
[GET /Admin/ManageAssessment renders shell + HTMX loads tab → flash banner]
```

### Recommended Project Structure (no new directories)

```
Controllers/
└── AssessmentAdminController.cs          # 3 method body edits (line 2022, 2127, 2232)
                                          # + 1 new HttpGet GetDeleteImpact
                                          # + optional private helper EnsureCanDelete()
Services/
└── AuditLogService.cs                    # NO CHANGE (signature stable)
Views/Admin/Shared/
└── _AssessmentGroupsTab.cshtml           # 3 form refactor (line 240-275)
                                          # + 3 modal definitions (impact preview)
                                          # + script section: AJAX fetch + 2-step swap
tests/e2e/
└── assessment.spec.ts                    # NEW describe block: FLOW 10 Phase 312 (6 tests)
                                          # OR: tests/e2e/admin-delete.spec.ts (greenfield)
```

### Pattern 1: AJAX-loaded impact preview modal (PRIMARY PRECEDENT)

**Source:** `Views/Admin/AssessmentMonitoringDetail.cshtml:500-537` + Controllers line 3419-3435.

**What:** Modal opens with loading spinner → on `show.bs.modal` event, fetch JSON endpoint → populate body fields → user confirms → submit form.

**Why this pattern (not data-attributes / ViewBag precompute):**
1. **List view perf:** `_AssessmentGroupsTab` renders ≤20 rows. Precomputing impact data untuk semua row = 20× extra count queries pada page load. AJAX-on-demand = 1× query saat user buka modal.
2. **Phase 311 alignment:** Plan 03 baru saja optimize ManageAssessment (AsNoTracking, Categories cache). Adding precompute counts kontradiksi prinsip "minimize page-load query."
3. **Verified precedent:** `akhiriSemuaModal` pakai exact same pattern.

**Example (existing):**
```razor
@* Source: Views/Admin/AssessmentMonitoringDetail.cshtml:501-537 *@
<div class="modal fade" id="akhiriSemuaModal" tabindex="-1" aria-labelledby="akhiriSemuaModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-danger text-white">...</div>
            <div class="modal-body">
                <div id="akhiriSemuaLoading" class="text-center py-3">
                    <span class="spinner-border spinner-border-sm me-2"></span>Memuat data...
                </div>
                <div id="akhiriSemuaContent" style="display:none;">
                    <ul>
                        <li><strong id="akhiriInProgressCount">0</strong> peserta InProgress ...</li>
                    </ul>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <form asp-action="AkhiriSemuaUjian" asp-controller="AssessmentAdmin" method="post" class="d-inline">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="title" value="@Model.Title" />
                    <button type="submit" class="btn btn-danger">Ya, Akhiri Semua</button>
                </form>
            </div>
        </div>
    </div>
</div>
```

```js
// Source: Views/Admin/AssessmentMonitoringDetail.cshtml:937-962
var akhiriModal = document.getElementById('akhiriSemuaModal');
akhiriModal.addEventListener('show.bs.modal', function () {
    fetch(appUrl('/Admin/GetAkhiriSemuaCounts') + '?title=' + encodeURIComponent(hTitle) + ...)
        .then(r => r.json())
        .then(data => {
            document.getElementById('akhiriInProgressCount').textContent = data.inProgressCount || 0;
            // ...
        });
});
```

```csharp
// Source: Controllers/AssessmentAdminController.cs:3419-3435
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetAkhiriSemuaCounts(string title, string category, DateTime scheduleDate)
{
    var sessions = await _context.AssessmentSessions
        .Where(...)
        .ToListAsync();
    int inProgressCount = sessions.Count(s => ...);
    return Json(new { inProgressCount, notStartedCount });
}
```

### Pattern 2: 2-step modal flow (NO direct precedent — propose extension)

**Why not direct precedent:** Existing modals (`editScoreWarningModal`, `akhiriSemuaModal`, `editTypeWarningModal`) semua **single-step** — load data → user clicks confirm → submit. Phase 312 D-02 mensyaratkan **2-step**: Step 1 impact preview, Step 2 final confirm.

**Recommendation:** Single modal dengan 2 panel di-swap via JS (bukan 2 modal). Lebih ringan, sesuai spirit existing patterns.

```html
<div class="modal fade" id="deleteAssessmentModal" tabindex="-1">
    <div class="modal-dialog modal-dialog-scrollable">
        <div class="modal-content">
            <div class="modal-header bg-danger text-white">
                <h5 class="modal-title"><i class="bi bi-trash3 me-2"></i><span id="dam-title">Hapus Assessment</span></h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <!-- STEP 1: Impact Preview (visible by default) -->
            <div class="modal-body" id="dam-step-1">
                <div id="dam-loading" class="text-center py-3">
                    <span class="spinner-border spinner-border-sm me-2"></span>Memuat dampak penghapusan...
                </div>
                <div id="dam-content" style="display:none;">
                    <p class="fw-semibold mb-2">Dampak penghapusan:</p>
                    <ul class="mb-2">
                        <li>Status saat ini: <strong id="dam-status">—</strong></li>
                        <li><strong id="dam-response-count">0</strong> peserta dengan jawaban tersimpan</li>
                        <li><strong id="dam-cert-count">0</strong> sertifikat akan terhapus</li>
                        <li><strong id="dam-package-count">0</strong> paket soal (beserta soal & opsi)</li>
                        <li><strong id="dam-attempt-count">0</strong> riwayat percobaan</li>
                    </ul>
                    <div id="dam-prepost-breakdown" style="display:none;">
                        <!-- For PrePost: 2 sub-blocks Pre + Post separately -->
                    </div>
                </div>
            </div>

            <!-- STEP 2: Final Confirm (hidden by default) -->
            <div class="modal-body" id="dam-step-2" style="display:none;">
                <div class="alert alert-danger mb-3">
                    <i class="bi bi-exclamation-triangle-fill me-2"></i>
                    <strong>Tindakan ini tidak dapat dibatalkan.</strong>
                </div>
                <p>Anda akan menghapus permanen:</p>
                <ul>
                    <li>Sesi assessment beserta semua paket soal, soal, dan opsi</li>
                    <li>Semua jawaban peserta yang sudah tersimpan</li>
                    <li>Riwayat percobaan ujian</li>
                    <li>Penugasan paket ke peserta</li>
                </ul>
                <p class="text-muted small">Lanjutkan?</p>
            </div>

            <div class="modal-footer">
                <!-- Step 1 footer -->
                <div id="dam-footer-1">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                    <button type="button" class="btn btn-danger" id="dam-next-btn" disabled>
                        Lanjutkan <i class="bi bi-arrow-right"></i>
                    </button>
                </div>
                <!-- Step 2 footer -->
                <div id="dam-footer-2" style="display:none;">
                    <button type="button" class="btn btn-secondary" id="dam-back-btn">
                        <i class="bi bi-arrow-left"></i> Kembali
                    </button>
                    <form id="dam-submit-form" method="post" class="d-inline">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="id" id="dam-form-id" />
                        <input type="hidden" name="linkedGroupId" id="dam-form-linkedid" />
                        <button type="submit" class="btn btn-danger">
                            <i class="bi bi-trash3 me-1"></i>Hapus Permanen
                        </button>
                    </form>
                </div>
            </div>
        </div>
    </div>
</div>
```

**Trade-off vs greenfield 2-modal pattern:**
- Single modal + JS panel swap = LESS markup, LESS DOM, simpler state machine, parity dengan existing single-modal precedents.
- 2-modal pattern = lebih "explicit" tapi tidak ada precedent di repo + double Bootstrap.Modal instantiation.
- **Pilih single modal.**

### Pattern 3: Backend role-tier guard (private helper recommended)

**What:** Body method check sebelum cascade. Reject early dengan AuditLog blocked + TempData error + redirect.

**Recommendation:** Extract ke private helper untuk DRY across 3 method.

```csharp
/// <summary>
/// Phase 312 D-01/D-03: Role tier guard untuk delete operations.
/// Returns null jika OK to proceed, atau IActionResult (RedirectToAction) jika blocked.
/// Side-effect: write AuditLog "{action}Blocked" entry pada reject path.
/// </summary>
private async Task<IActionResult?> EnsureCanDeleteAsync(
    string actionName,                    // "DeleteAssessment" / "DeleteAssessmentGroup" / "DeletePrePostGroup"
    int targetId,
    string entityType,                    // "AssessmentSession"
    IList<AssessmentSession> sessionsToCheck)  // 1 untuk single, 2 untuk PrePost, N untuk Group
{
    // Admin override — selalu OK
    if (User.IsInRole("Admin")) return null;

    // HC reject conditions: ada session Completed ATAU ada response peserta
    var sessionIds = sessionsToCheck.Select(s => s.Id).ToList();
    int totalResponseCount = await _context.PackageUserResponses
        .CountAsync(r => sessionIds.Contains(r.AssessmentSessionId));
    bool hasCompleted = sessionsToCheck.Any(s => s.Status == "Completed");
    bool hasResponses = totalResponseCount > 0;

    if (!hasCompleted && !hasResponses) return null;  // HC ok proceed

    // Blocked path — log audit failure
    string reason = hasCompleted && hasResponses
        ? $"Status=Completed, ResponseCount={totalResponseCount}"
        : hasCompleted
            ? "Status=Completed"
            : $"ResponseCount={totalResponseCount}";

    try
    {
        var actor = await _userManager.GetUserAsync(User);
        var actorName = string.IsNullOrWhiteSpace(actor?.NIP)
            ? (actor?.FullName ?? "Unknown")
            : $"{actor.NIP} - {actor.FullName}";
        await _auditLog.LogAsync(
            actor?.Id ?? "",
            actorName,
            $"{actionName}Blocked",
            $"HC role blocked from {actionName} [TargetId={targetId}]: {reason}",
            targetId,
            entityType);
    }
    catch (Exception auditEx)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
        logger.LogWarning(auditEx, "Audit log write failed for {Action}Blocked {Id}", actionName, targetId);
    }

    TempData["Error"] = "Anda tidak memiliki izin untuk menghapus assessment yang sudah Completed atau memiliki jawaban peserta.";
    return RedirectToAction("ManageAssessment");
}
```

### Pattern 4: Success AuditLog with Status + ResponseCount (D-03 + SC #4)

**Existing (line 2098-2104):**
```csharp
await _auditLog.LogAsync(deleteUser?.Id ?? "", deleteActorName, "DeleteAssessment",
    $"Deleted assessment '{assessmentTitle}' [ID={id}]", id, "AssessmentSession");
```

**Phase 312 (extend description only):**
```csharp
await _auditLog.LogAsync(deleteUser?.Id ?? "", deleteActorName, "DeleteAssessment",
    $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount}",
    id, "AssessmentSession");
```

**Capture pattern:** Snapshot `Status` dan `ResponseCount` SEBELUM cascade RemoveRange (data hilang setelah SaveChanges). Reuse session loaded di awal action body.

### Anti-Patterns to Avoid

- **Hand-rolling `Authorization.AuthorizeAsync` / requirement-based policy.** Existing pattern pakai `User.IsInRole("Admin")` directly di body method dan view. Tetap ikut convention. Policy/Requirement adalah over-engineering untuk 1 role check.
- **Removing `[Authorize(Roles = "Admin, HC")]` attribute lalu pakai `[AllowAnonymous]` + manual check.** SC #2 eksplisit: attribute TIDAK diubah. Body guard adalah extra layer, bukan pengganti.
- **Mengubah cascade order RemoveRange.** Existing order (PackageUserResponses → AssessmentAttemptHistory → AssessmentPackages [+ Questions+Options] → AssessmentSessions) sudah teruji 3× (line 2048-2089, 2158-2194, 2256-2286). Restrict FK pada PackageUserResponses → AssessmentSession; UserPackageAssignments cascade-deleted by DB.
- **Pakai `assessment.PackageUserResponses.Count` (lazy load) untuk response count.** Pakai explicit `_context.PackageUserResponses.CountAsync(r => r.AssessmentSessionId == id)` untuk consistency dengan existing pattern (line 2049-2051) dan AsNoTracking compatibility.
- **Inline modal markup di setiap row table.** Ikut precedent `akhiriSemuaModal` — single modal di luar table loop, di-populate via JS dengan id dari clicked button.
- **AJAX POST untuk delete submit.** Pertahankan form POST + RedirectToAction. JS hanya untuk show modal + populate impact + step swap. Submit terakhir pakai `<form>.submit()` agar TempData flash + AntiForgeryToken handling konsisten dengan existing pattern.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Modal show/hide state machine | Custom JS class with `addEventListener` for `data-bs-toggle` | `bootstrap.Modal` API: `new bootstrap.Modal(element).show()` / `.hide()` | Bootstrap 5 sudah include — pattern verified `editScoreWarningModal:290`. |
| Anti-forgery token harvesting for AJAX | `getAntiForgeryToken()` JS helper (CoachingProton pattern) | Hidden `<form>` di modal footer dengan `@Html.AntiForgeryToken()` + `<form>.submit()` | Simpler — server expects form-encoded `__RequestVerificationToken`, no AJAX header juggling. |
| Role check in JS | `if (currentUserRole === 'HC')` di klien | Server-side `@if (User.IsInRole("Admin") || canHcDelete)` di Razor | Authoritative + tahan DevTools tampering. UI hide adalah UX bonus, bukan security. |
| Audit log entity | Custom `AuditLog` row insertion via `_context.AuditLogs.Add` | `_auditLog.LogAsync(...)` service | Service handle `CreatedAt = DateTime.UtcNow` + SaveChangesAsync. Bypass = inconsistent timestamp. |
| Response count query | `assessment.PackageUserResponses.Count` lazy nav | `_context.PackageUserResponses.CountAsync(r => r.AssessmentSessionId == id)` | Existing pattern (line 2049-2051), AsNoTracking-friendly, no extra DB roundtrip via lazy proxy. |
| Cascade delete logic | Re-implement RemoveRange order | Reuse existing 3 method body — guard added BEFORE cascade block | Cascade sudah teruji 3×, FK constraints handled. |

**Key insight:** Phase 312 = 100% pattern composition dari existing repo. Tidak ada satupun komponen yang butuh greenfield. Setiap implementation choice ada precedent file:line yang verified.

---

## Runtime State Inventory

> Phase 312 menambah feature (role guard + audit + UI), bukan rename/refactor. Tidak ada renamed string.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | None — verified by reading 3 method body (line 2018-2317). Tidak ada string identifier yg di-store ke DB. AuditLog action names baru (`{Action}Blocked`) adalah append-only, tidak ada existing row dengan nama tsb. | None |
| Live service config | None — verified by grep "DeleteAssessment" di Controllers/. Tidak ada n8n / external workflow. | None |
| OS-registered state | None — verified by repo scan. Tidak ada Task Scheduler / pm2 / systemd terkait delete action. | None |
| Secrets/env vars | None — auth pakai existing ASP.NET Core Identity + Cookie auth. Tidak ada API key / token baru. | None |
| Build artifacts | None — pure C# + Razor + JS edits. `dotnet build` regen otomatis. Playwright tests recompile via TypeScript. | None |

**Conclusion:** No runtime state migration needed. Phase 312 fully forward-compatible (pre-deploy = old behavior; post-deploy = guard active).

---

## Common Pitfalls

### Pitfall 1: Snapshot Status & ResponseCount BEFORE cascade

**What goes wrong:** Audit success log description shows `Status=` empty atau `ResponseCount=0` karena field di-read SETELAH `SaveChangesAsync()`.

**Why it happens:** Existing AuditLog block ada di line 2094-2109 (setelah SaveChanges). Kalau capture `assessment.Status` setelah save, entity sudah detached/deleted.

**How to avoid:** Capture ke local var SEBELUM cascade RemoveRange:
```csharp
var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(...);
// ... null check ...
string preDeleteStatus = assessment.Status;
int preDeleteResponseCount = await _context.PackageUserResponses
    .CountAsync(r => r.AssessmentSessionId == id);
// ... cascade RemoveRange + SaveChangesAsync ...
// AuditLog uses preDeleteStatus, preDeleteResponseCount
```

**Warning signs:** AuditLog row dengan description `Status= ResponseCount=` (empty values).

### Pitfall 2: Guard placement INSIDE try block (existing convention)

**What goes wrong:** Guard di-place SEBELUM `try {`, kalau guard throw (mis. `_auditLog.LogAsync` failure di blocked path), seluruh action gagal jadi 500 — bukan graceful TempData error.

**How to avoid:** Place guard call DI DALAM try block, AFTER session found check, BEFORE cascade. Guard helper itself sudah wrap `_auditLog.LogAsync` di try/catch internal.

```csharp
try
{
    var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(...);
    if (assessment == null) { ... return ...; }

    // Phase 312 D-01/D-03: role tier guard
    var blockResult = await EnsureCanDeleteAsync("DeleteAssessment", id, "AssessmentSession",
                                                  new[] { assessment });
    if (blockResult != null) return blockResult;

    // ... existing cascade RemoveRange (UNCHANGED) ...
}
catch (Exception ex) { ... }
```

### Pitfall 3: PrePost guard checks BOTH sessions

**What goes wrong:** Guard cek Pre-Test session saja (Completed=false, response=0) tapi Post-Test sudah Completed dengan responses. HC bypass.

**How to avoid:** Pass `groupSessions` (List, length=2) ke guard helper. Guard sum responseCount across all sessionIds dan check `Any(s => s.Status == "Completed")` bukan `First().Status`.

```csharp
// DeletePrePostGroup body:
var groupSessions = await _context.AssessmentSessions
    .Where(a => a.LinkedGroupId == linkedGroupId).ToListAsync();
if (!groupSessions.Any()) { ... return ...; }

var blockResult = await EnsureCanDeleteAsync("DeletePrePostGroup", linkedGroupId,
                                              "AssessmentSession", groupSessions);
if (blockResult != null) return blockResult;
```

**Warning signs:** Smoke test "HC + PrePost dengan 1 sesi Completed" berhasil delete (seharusnya blocked).

### Pitfall 4: Modal di partial view re-rendered per row → duplicate IDs

**What goes wrong:** Place `<div class="modal" id="deleteAssessmentModal">` di dalam `@foreach (var group in managementData)` loop → 20 modal dengan id sama → Bootstrap.Modal pick up wrong instance.

**How to avoid:** Place SINGLE modal di akhir partial (di luar loop), pass row data via button data-attributes:
```html
<button type="button" class="dropdown-item text-danger"
        data-bs-toggle="modal" data-bs-target="#deleteAssessmentModal"
        data-delete-type="single"
        data-delete-id="@group.RepresentativeId"
        data-delete-title="@group.Title">
    <i class="bi bi-trash me-2"></i>Hapus
</button>
```

JS handler reads `event.relatedTarget.dataset.deleteType` (`'single'`/`'group'`/`'prepost'`) → fetch correct endpoint.

### Pitfall 5: HTMX partial re-render dengan modal state

**What goes wrong:** Phase 311 introduced HTMX lazy-load tab. Setelah delete success, `RedirectToAction("ManageAssessment")` reload full shell, BUKAN HTMX swap. Kalau user reload via HTMX (filter/pagination) modal HTML re-rendered → existing modal handler attach lost.

**How to avoid:**
- Modal markup ada di `_AssessmentGroupsTab.cshtml` partial (re-rendered by HTMX swap).
- JS handler attach via event delegation di `document` level ATAU di-bootstrap pada `htmx:afterSwap` event:

```js
document.body.addEventListener('htmx:afterSwap', function(evt) {
    // Re-init modal after partial swap
    var deleteModal = document.getElementById('deleteAssessmentModal');
    if (deleteModal && !deleteModal.dataset.handlerAttached) {
        attachDeleteModalHandler(deleteModal);
        deleteModal.dataset.handlerAttached = 'true';
    }
});
```

**Warning signs:** Modal works pada page load tapi tidak setelah filter/pagination via HTMX.

### Pitfall 6: AntiForgeryToken stale setelah long-lived modal

**What goes wrong:** User buka page, idle 30 menit, klik delete → modal show → submit → 400 "anti-forgery token expired."

**How to avoid:** AntiForgeryToken di Razor adalah valid untuk sesi cookie. Default token timeout sesuai `DataProtection` lifetime (~14 hari di ASP.NET Core default). Tidak perlu explicit refresh untuk timeline ≤ 1 hari. Kalau user idle >1 hari = re-login akan trigger redirect, tidak masuk delete flow.

**No action required** — flag ini hanya kalau Phase 312 perkenalkan single-page-app behavior (tidak relevan, masih full page reload).

---

## Code Examples

### Example 1: Backend guard di DeleteAssessment (line 2022-2046)

```csharp
// Source: derive from existing Controllers/AssessmentAdminController.cs:2022-2046
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessment(int id)
{
    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
    try
    {
        var assessment = await _context.AssessmentSessions.FirstOrDefaultAsync(a => a.Id == id);
        if (assessment == null)
        {
            logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
            TempData["Error"] = "Assessment not found.";
            return RedirectToAction("ManageAssessment");
        }

        // EXISTING D-19 PrePost block — UNCHANGED
        if (assessment.AssessmentType == "PreTest" || assessment.AssessmentType == "PostTest")
        {
            TempData["Error"] = "Sesi ini bagian dari grup Pre-Post Test. Gunakan 'Hapus Grup' untuk menghapus keduanya.";
            return RedirectToAction("ManageAssessment");
        }

        // PHASE 312 (NEW): role tier guard
        var blockResult = await EnsureCanDeleteAsync("DeleteAssessment", id,
                                                     "AssessmentSession", new[] { assessment });
        if (blockResult != null) return blockResult;

        // PHASE 312 (NEW): snapshot Status + ResponseCount untuk audit success
        string preDeleteStatus = assessment.Status;
        int preDeleteResponseCount = await _context.PackageUserResponses
            .CountAsync(r => r.AssessmentSessionId == id);

        var assessmentTitle = assessment.Title;
        // ... existing cascade RemoveRange UNCHANGED (line 2048-2089) ...
        await _context.SaveChangesAsync();

        // PHASE 312 (CHANGED): description extends with Status + ResponseCount
        try
        {
            var deleteUser = await _userManager.GetUserAsync(User);
            var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP)
                ? (deleteUser?.FullName ?? "Unknown")
                : $"{deleteUser.NIP} - {deleteUser.FullName}";
            await _auditLog.LogAsync(
                deleteUser?.Id ?? "",
                deleteActorName,
                "DeleteAssessment",
                $"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount}",
                id,
                "AssessmentSession");
        }
        catch (Exception auditEx) { logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id); }

        TempData["Success"] = $"Assessment '{assessmentTitle}' has been deleted successfully.";
        return RedirectToAction("ManageAssessment");
    }
    catch (Exception ex) { ... }
}
```

### Example 2: HttpGet GetDeleteImpact endpoint

```csharp
// Source: pattern derived from Controllers/AssessmentAdminController.cs:3419-3435 (GetAkhiriSemuaCounts)
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetDeleteImpact(string type, int id)
{
    // type ∈ { "single", "group", "prepost" }
    List<int> sessionIds;
    string status;

    if (type == "single")
    {
        var s = await _context.AssessmentSessions.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.Id, a.Status, a.Title })
            .FirstOrDefaultAsync();
        if (s == null) return NotFound(new { error = "Assessment not found" });
        sessionIds = new() { s.Id };
        status = s.Status;
    }
    else if (type == "group")
    {
        var rep = await _context.AssessmentSessions.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new { a.Id, a.Title, a.Category, a.Schedule })
            .FirstOrDefaultAsync();
        if (rep == null) return NotFound(new { error = "Group not found" });
        var siblings = await _context.AssessmentSessions.AsNoTracking()
            .Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == rep.Schedule.Date)
            .Select(a => new { a.Id, a.Status })
            .ToListAsync();
        sessionIds = siblings.Select(x => x.Id).ToList();
        status = siblings.Any(x => x.Status == "Completed") ? "Completed (1+ sessions)"
               : siblings.Any(x => x.Status == "InProgress") ? "InProgress (1+ sessions)"
               : siblings.First().Status;
    }
    else if (type == "prepost")
    {
        var sessions = await _context.AssessmentSessions.AsNoTracking()
            .Where(a => a.LinkedGroupId == id)
            .Select(a => new { a.Id, a.Status, a.AssessmentType })
            .ToListAsync();
        if (!sessions.Any()) return NotFound(new { error = "Pre-Post group not found" });
        sessionIds = sessions.Select(x => x.Id).ToList();
        status = sessions.Any(x => x.Status == "Completed") ? "Completed (1+ sessions)"
               : sessions.Any(x => x.Status == "InProgress") ? "InProgress (1+ sessions)"
               : sessions.First().Status;
    }
    else return BadRequest(new { error = "Invalid type" });

    int responseCount = await _context.PackageUserResponses.AsNoTracking()
        .CountAsync(r => sessionIds.Contains(r.AssessmentSessionId));
    int packageCount = await _context.AssessmentPackages.AsNoTracking()
        .CountAsync(p => sessionIds.Contains(p.AssessmentSessionId));
    int attemptCount = await _context.AssessmentAttemptHistory.AsNoTracking()
        .CountAsync(h => sessionIds.Contains(h.SessionId));
    // certCount: certificates exist when session.Status == "Completed" AND NomorSertifikat populated
    int certCount = await _context.AssessmentSessions.AsNoTracking()
        .CountAsync(a => sessionIds.Contains(a.Id) && a.NomorSertifikat != null);

    // For prepost, breakdown per session:
    object? prePostBreakdown = null;
    if (type == "prepost")
    {
        prePostBreakdown = await _context.AssessmentSessions.AsNoTracking()
            .Where(a => a.LinkedGroupId == id)
            .Select(a => new {
                a.AssessmentType, a.Status,
                ResponseCount = _context.PackageUserResponses.Count(r => r.AssessmentSessionId == a.Id)
            })
            .ToListAsync();
    }

    return Json(new {
        status, responseCount, certCount, packageCount, attemptCount,
        prePostBreakdown,
        sessionCount = sessionIds.Count
    });
}
```

### Example 3: Razor conditional render in `_AssessmentGroupsTab.cshtml`

```razor
@* Source: replace existing line 248-274 *@
@{
    bool isAdmin = User.IsInRole("Admin");
    string groupStatus = (string)group.Status;
    // Note: responseCount per-row TIDAK di-precompute (perf reason — see Pattern 1).
    // HC visibility cek di server pakai groupStatus only (Status==Completed → hide).
    // Edge case: HC + Open + has-responses → button TAMPIL (HC click → modal show → backend guard reject).
    // Trade-off: HC bisa lihat tombol untuk Open-with-response; backend tetap proteksi.
    // Alternative kalau strict UI hide diperlukan: precompute responseCount per row di partial action
    // (cost: 20× extra count queries — verify dengan user kalau worth it).
    bool canHcDelete = groupStatus != "Completed";  // simplified — adjust if precompute response added
}
<li><hr class="dropdown-divider"></li>
@if ((bool)group.IsPrePostGroup)
{
    @if (isAdmin || canHcDelete)
    {
        <li>
            <button type="button" class="dropdown-item text-danger"
                    data-bs-toggle="modal" data-bs-target="#deleteAssessmentModal"
                    data-delete-type="prepost"
                    data-delete-id="@group.LinkedGroupId"
                    data-delete-title="@group.Title">
                <i class="bi bi-trash3 me-2"></i>Hapus Grup Pre-Post
            </button>
        </li>
    }
}
else
{
    @if (isAdmin || canHcDelete)
    {
        <li>
            <button type="button" class="dropdown-item text-danger"
                    data-bs-toggle="modal" data-bs-target="#deleteAssessmentModal"
                    data-delete-type="group"
                    data-delete-id="@group.RepresentativeId"
                    data-delete-title="@group.Title">
                <i class="bi bi-trash me-2"></i>Hapus Grup
            </button>
        </li>
    }
}
```

> **Open question — see §Open Questions:** SC #3 dan D-01 mensyaratkan hide kalau `responseCount > 0`. Tanpa precompute, UI hanya cek Status. Backend tetap reject + audit blocked → defense-in-depth. Trade-off: UI showing button → user click → modal load → reject. Pengalaman HC slightly degraded (tahu tombol ada baru tahu tidak boleh) tapi tidak ada security impact. Plan-phase decide: (A) inline ResponseCount precompute di partial action (20 extra queries) atau (B) accept UI imperfection + backend guard.

### Example 4: JS modal orchestration (event delegation)

```js
// Source: derive from Views/Admin/AssessmentMonitoringDetail.cshtml:937-962
(function() {
    var deleteModal = document.getElementById('deleteAssessmentModal');
    if (!deleteModal) return;

    deleteModal.addEventListener('show.bs.modal', function(event) {
        var btn = event.relatedTarget;
        var type = btn.dataset.deleteType;     // 'single' | 'group' | 'prepost'
        var id = btn.dataset.deleteId;
        var title = btn.dataset.deleteTitle;

        // Reset to step 1
        document.getElementById('dam-step-1').style.display = '';
        document.getElementById('dam-step-2').style.display = 'none';
        document.getElementById('dam-footer-1').style.display = '';
        document.getElementById('dam-footer-2').style.display = 'none';
        document.getElementById('dam-loading').style.display = '';
        document.getElementById('dam-content').style.display = 'none';
        document.getElementById('dam-next-btn').disabled = true;

        // Configure form action + hidden field
        var form = document.getElementById('dam-submit-form');
        var titleEl = document.getElementById('dam-title');
        if (type === 'single') {
            form.action = '/Admin/DeleteAssessment';
            document.getElementById('dam-form-id').value = id;
            document.getElementById('dam-form-id').name = 'id';
            document.getElementById('dam-form-linkedid').disabled = true;
            titleEl.textContent = 'Hapus Assessment: ' + title;
        } else if (type === 'group') {
            form.action = '/Admin/DeleteAssessmentGroup';
            document.getElementById('dam-form-id').value = id;
            document.getElementById('dam-form-id').name = 'id';
            document.getElementById('dam-form-linkedid').disabled = true;
            titleEl.textContent = 'Hapus Grup Assessment: ' + title;
        } else if (type === 'prepost') {
            form.action = '/Admin/DeletePrePostGroup';
            document.getElementById('dam-form-linkedid').value = id;
            document.getElementById('dam-form-linkedid').name = 'linkedGroupId';
            document.getElementById('dam-form-linkedid').disabled = false;
            document.getElementById('dam-form-id').disabled = true;
            titleEl.textContent = 'Hapus Grup Pre-Post Test: ' + title;
        }

        // Fetch impact preview
        fetch('/Admin/GetDeleteImpact?type=' + encodeURIComponent(type) + '&id=' + encodeURIComponent(id))
            .then(function(r) { return r.json(); })
            .then(function(data) {
                document.getElementById('dam-status').textContent = data.status || '—';
                document.getElementById('dam-response-count').textContent = data.responseCount || 0;
                document.getElementById('dam-cert-count').textContent = data.certCount || 0;
                document.getElementById('dam-package-count').textContent = data.packageCount || 0;
                document.getElementById('dam-attempt-count').textContent = data.attemptCount || 0;

                if (data.prePostBreakdown) {
                    // Render Pre / Post breakdown into #dam-prepost-breakdown
                    // ...
                    document.getElementById('dam-prepost-breakdown').style.display = '';
                }

                document.getElementById('dam-loading').style.display = 'none';
                document.getElementById('dam-content').style.display = '';
                document.getElementById('dam-next-btn').disabled = false;
            })
            .catch(function() {
                document.getElementById('dam-loading').innerHTML =
                    '<span class="text-danger">Gagal memuat data dampak. Tutup dan coba lagi.</span>';
            });
    });

    // Step navigation
    document.getElementById('dam-next-btn').addEventListener('click', function() {
        document.getElementById('dam-step-1').style.display = 'none';
        document.getElementById('dam-step-2').style.display = '';
        document.getElementById('dam-footer-1').style.display = 'none';
        document.getElementById('dam-footer-2').style.display = '';
    });
    document.getElementById('dam-back-btn').addEventListener('click', function() {
        document.getElementById('dam-step-1').style.display = '';
        document.getElementById('dam-step-2').style.display = 'none';
        document.getElementById('dam-footer-1').style.display = '';
        document.getElementById('dam-footer-2').style.display = 'none';
    });
})();
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `<form onclick="return confirm('...')">` plain browser dialog | Bootstrap 5 modal dengan custom markup | This phase (Phase 312) | UI polish + impact preview |
| Single-step modal (load → confirm) | 2-step modal (preview → final confirm) | This phase | Destructive action transparency |
| AuditLog success only | AuditLog success + blocked attempts | This phase (D-03) | Compliance audit completeness |
| `[Authorize(Roles = "...")]` attribute saja | Attribute + body method tier guard | This phase | Granular per-condition authorization |

**Deprecated/outdated dalam Phase 312:**
- Plain `confirm()` browser dialog di 3 form delete — replaced dengan modal Bootstrap.

**Not deprecated (keep as-is):**
- Existing cascade RemoveRange order — proven, performant.
- Existing AntiForgeryToken via `@Html.AntiForgeryToken()` — modal-based flow tetap submit via form.
- Existing TempData["Success"]/"Error"] flash pattern — sukses + reject keduanya pakai pattern ini.

---

## Validation Architecture

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Playwright `^1.58.2` (e2e) + manual UAT (smoke) [VERIFIED: tests/package.json] |
| Config file | `tests/playwright.config.ts` (existing) |
| Quick run command | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 312" --reporter=list` |
| Full suite command | `cd tests && npx playwright test` |
| Backend build verify | `dotnet build` |

### Phase Requirements → Test Map

| AC ID | Behavior | Test Type | Automated Command | File Exists? |
|-------|----------|-----------|-------------------|--------------|
| AC #1 (role guard backend) | HC POST direct ke `/Admin/DeleteAssessment` dengan session Completed → reject + AuditLog blocked | E2E (Playwright drive form submit + assert TempData banner + DB AuditLog query manual) | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "12.1\|12.4\|12.5"` | ❌ Wave 0 — new describe block FLOW 12 |
| AC #2 (Authorize attribute unchanged) | Line 2020/2125/2230 attribute literal `[Authorize(Roles = "Admin, HC")]` | grep verify | `grep -n 'Authorize.*Admin, HC' Controllers/AssessmentAdminController.cs` | ✅ existing pattern |
| AC #3 (UI conditional render) | HC user lihat ManageAssessment → tombol Hapus tidak ada untuk row Status=Completed | E2E (Playwright login as hc + assert button absence) | `cd tests && npx playwright test --grep "12.2"` | ❌ Wave 0 |
| AC #4 (AuditLog Status+ResponseCount di success) | AuditLog row exists dengan ActionType="DeleteAssessment" dan Description LIKE '%Status=%ResponseCount=%' | DB query post-delete (manual UAT step) | manual SQL: `SELECT TOP 5 ActionType, Description, CreatedAt FROM AuditLogs WHERE ActionType LIKE 'Delete%' ORDER BY CreatedAt DESC` | ✅ DB exists |
| AC #5 (cascade utuh) | Pre-delete: count(PackageUserResponses, AttemptHistory, Packages, Sessions); Post-delete: all = 0 untuk session_id terkait | E2E + DB sanity check | manual SQL pre/post counts | ✅ existing pattern |
| AC #6 (smoke test 6 skenario) | Admin+Open OK / Admin+Completed OK / HC+Open(no-response) OK / HC+Completed BLOCK / HC+Open(with-response) BLOCK / HC+PrePost+Completed BLOCK | E2E Playwright | `cd tests && npx playwright test --grep "Phase 312"` | ❌ Wave 0 |

**Bonus AC#3 dim coverage:** AuditLog blocked entries (D-03) → manual SQL `SELECT * FROM AuditLogs WHERE ActionType LIKE '%Blocked'` setelah HC failed attempt, expect 1 row per blocked POST.

### Sampling Rate

- **Per task commit:** `dotnet build` (zero new warnings) + `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 312" --reporter=list`
- **Per wave merge:** Full Playwright suite (`cd tests && npx playwright test`) + manual UAT 6 skenario.
- **Phase gate:** Full suite GREEN + Manual UAT sign-off di `312-UAT.md`.

### Wave 0 Gaps

- [ ] `tests/e2e/assessment.spec.ts` — extend dengan FLOW 12 describe block (6 tests: 12.1-12.6) untuk Phase 312 scenarios. Pakai existing `login(page, 'admin')` / `login(page, 'hc')` helpers.
- [ ] `tests/e2e/helpers/wizardSelectors.ts` — extend dengan selectors untuk `#deleteAssessmentModal`, `#dam-status`, `#dam-response-count`, `#dam-next-btn`, `#dam-back-btn` (atau sama-sama inline di test file kalau scope kecil).
- [ ] Test fixture: butuh seeded data untuk test "HC + Completed" + "HC + Open with response." Plan-phase decide: (a) reuse existing seeded data dari Phase 308 wizard tests, atau (b) explicit setup dalam `test.beforeAll` (login admin + create assessment + force Status=Completed via DB direct ATAU via existing AkhiriUjian endpoint).
- [ ] `312-UAT.md` — manual UAT 6-step Bahasa Indonesia (4-step pattern dari Phase 308 / 310 sebagai template).

**No framework install needed** — Playwright already at `^1.58.2`.

---

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity + Cookie auth (existing, unchanged) |
| V3 Session Management | yes | Cookie-based session (existing, unchanged) |
| V4 Access Control | **yes — primary** | `[Authorize(Roles = "Admin, HC")]` attribute (existing) + body method tier guard (Phase 312 new) |
| V5 Input Validation | yes | EF Core parameterized queries (existing); int parameter binding model binding |
| V6 Cryptography | no | Tidak ada operasi kripto di scope Phase 312 |
| V7 Error Handling | yes | try/catch + structured `_logger.LogError` (existing pattern) |
| V13 API & Web Services | yes | `[ValidateAntiForgeryToken]` (existing, unchanged) |
| V14 Configuration | no | Tidak ada config baru |

### Known Threat Patterns for ASP.NET Core MVC Admin Tier

| Pattern | STRIDE | Standard Mitigation | Phase 312 Coverage |
|---------|--------|---------------------|--------------------|
| Privilege escalation: HC bypass via direct POST (skip UI) | Elevation of Privilege | Server-side body method check (TIDAK hanya UI hide) | ✅ `EnsureCanDeleteAsync` di body |
| CSRF on destructive action | Tampering | `[ValidateAntiForgeryToken]` + form anti-forgery | ✅ existing attribute (line 2021/2126/2231); modal form pertahankan `@Html.AntiForgeryToken()` |
| Audit log tampering / missing audit | Repudiation | Server-side `AuditLogService` write SaveChangesAsync; success + failure both logged | ✅ D-03 covers blocked attempts |
| Information disclosure via error messages | Information Disclosure | Generic TempData["Error"] — tidak expose stack trace | ✅ existing pattern di catch blocks |
| Race condition: 2 admins simultaneous delete same session | Tampering | EF Core SaveChangesAsync default optimistic concurrency on `RowVersion` (tidak ada di AssessmentSession — fallback last-write-wins; akhirnya 1 success 1 NotFound) | ⚠️ existing limitation, tidak dalam scope Phase 312 |
| Mass-assignment via id parameter | Tampering | Action signature `(int id)` — model binding scoped, no mass assignment | ✅ N/A |
| Unauthorized access to GetDeleteImpact endpoint | Information Disclosure | Apply `[Authorize(Roles = "Admin, HC")]` di action attribute | ✅ ikut existing convention |

**Threat: HC bypass via DevTools manipulation.** UI hide button = clientside (tahan casual user). DevTools dapat re-add button + submit → masuk backend route. **Mitigation:** Body method guard adalah PRIMARY defense. UI hide = UX bonus saja.

**Threat: Audit log replay / forgery.** AuditLog di-insert server-side via service, signed dengan `actorUserId` dari `_userManager.GetUserAsync(User)` (cookie-validated identity). Tidak ada API endpoint untuk client write AuditLog.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | `dotnet build` | ✓ | .NET 8 | — |
| Node.js + npm | Playwright tests | ✓ (existing tests pass) | per `tests/package.json` | — |
| Playwright | E2E smoke tests | ✓ | ^1.58.2 [VERIFIED tests/package.json:17] | — |
| SQL Server / SQLite | Local dev DB | ✓ | per `appsettings.Development.json` | — |
| Browser (Chromium/Firefox/Webkit) | Playwright auto-install | ✓ | bundled w/ Playwright | — |

**Missing dependencies:** None.

**Local dev URL:** `http://localhost:5277` (per `Properties/launchSettings.json`, profile `HcPortal`).
**Login admin:** `admin@pertamina.com` / `123456` (per `tests/helpers/accounts.ts:2`).
**Login HC:** `meylisa.tjiang@pertamina.com` / `123456` (per `tests/helpers/accounts.ts:3`).

---

## Risks & Open Questions

### Open Questions

1. **UI conditional render for Open + has-response (HC)**
   - What we know: D-01 says hide untuk `Status==Completed` ATAU `responseCount > 0`. SC #3 echoes ini.
   - What's unclear: `responseCount` per row TIDAK di-precompute di `ManageAssessmentTab_Assessment` action (Phase 311 perf-optimized). Adding precompute = 20× extra count queries per page.
   - Recommendation: **Plan-phase decide** salah satu:
     - (A) Precompute `responseCount` per group di partial action — accept perf cost (~20 queries × <2ms = <40ms overhead).
     - (B) UI hide pakai Status only; HC + Open + has-response → button shown → modal opened → backend reject + audit blocked. Defense-in-depth via backend guard. Trade-off: HC sees button briefly.
     - **Recommended: B** (backend is authoritative; UI imperfection minor; perf preserved).

2. **Modal placement: partial vs shell view**
   - What we know: `_AssessmentGroupsTab.cshtml` partial re-rendered by HTMX. Shell view `ManageAssessment.cshtml` outermost.
   - What's unclear: Modal markup di partial = re-rendered tiap HTMX swap (clean state but JS handler lost), atau di shell = stable but partial doesn't own its UI.
   - Recommendation: Place modal di `_AssessmentGroupsTab.cshtml` partial bottom (after table, before script section). Re-attach JS handler via `htmx:afterSwap` listener (Pitfall 5).

3. **Audit blocked entry PrePost reason format**
   - What we know: D-03 specifies "reason (Status atau ResponseCount yang trigger block)". Tapi PrePost group ada 2 sesi.
   - What's unclear: Reason: `"Status=Completed"` (1 sesi), `"Status=Completed (Pre)"`, atau `"Status=Completed,Completed"`?
   - Recommendation: Format `"Status=PreTest:Open,PostTest:Completed ResponseCount=12"` (explicit per-session). Plan-phase finalize wording.

4. **Smoke test data seeding strategy**
   - What we know: Test "HC + Completed" butuh assessment dengan Status=Completed.
   - What's unclear: Force Status via direct DB (need tests/helpers/db) atau via API (AkhiriUjian)?
   - Recommendation: Use `AkhiriUjian` endpoint via Playwright admin login (existing test pattern). Greenfield DB helper avoided — heavier infra.

### Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| HTMX afterSwap race: JS handler attached before modal element ready | Low | Medium | Use event delegation di `document.body` (bukan `deleteModal.addEventListener`) — handler stable across DOM mutations |
| Cascade order broken if guard accidentally placed AFTER cascade | Low | High | Lint/review checklist: guard call DI ATAS `RemoveRange`. Verify tests cover blocked path returns BEFORE any DB modification |
| AuditLog blocked entry adds log volume (HC casual misclicks) | Low | Low | Volume estimate: <10 blocked attempts per day di production. Negligible. Add index pada `AuditLogs.ActionType` kalau query slow (tidak in-scope) |
| Modal shows stale data (user opens modal → Page doesn't refresh) | Low | Medium | Fetch on every `show.bs.modal` event (no cache) — verified pattern di akhiriSemuaModal |
| Phase 311 HTMX integration: `RedirectToAction` setelah delete reload full shell, bukan partial swap | Medium | Low | Existing 3 method redirect to `ManageAssessment` action — full shell reload OK (data fresh, no stale tab content). User experience parity dengan pre-Phase 311 |

---

## Recommendations

### Concrete File Edits

**Backend (`Controllers/AssessmentAdminController.cs`):**
1. Add private helper `EnsureCanDeleteAsync(string actionName, int targetId, string entityType, IList<AssessmentSession> sessions)` — return `IActionResult?`. Insert near top of controller (after constructor) atau di akhir (before `// REGION: ManageAssessment`).
2. Edit `DeleteAssessment` (line 2022): insert guard call after PrePost block (line 2046), capture preDeleteStatus + preDeleteResponseCount before cascade, extend AuditLog success description (line 2102).
3. Edit `DeleteAssessmentGroup` (line 2127): insert guard call after siblings load (line 2152), capture aggregated `preDeleteResponseCount` + `preDeleteStatusSummary` (e.g., "5 sessions: 3 Open / 2 Completed"), extend AuditLog success description (line 2207).
4. Edit `DeletePrePostGroup` (line 2232): insert guard call after groupSessions load (line 2247), capture aggregated counts/status, extend AuditLog success description (line 2299).
5. Add `GetDeleteImpact` HttpGet action (after RegenerateToken / GetAkhiriSemuaCounts at line ~3435).

**Frontend (`Views/Admin/Shared/_AssessmentGroupsTab.cshtml`):**
6. Replace 2 inline `<form>` di line 248-274 dengan 2 `<button data-bs-toggle="modal">` (single, group, prepost variants) — guarded dengan `@if (User.IsInRole("Admin") || canHcDelete)`. (Note: 3 button = 2 forms existing + 1 yang sudah ada untuk individual session "Hapus" — TBD apakah ada di partial atau bukan; verifikasi saat plan-phase. Currently 240-275 cuma 2 form: DeletePrePostGroup + DeleteAssessmentGroup, BUKAN DeleteAssessment individual. Plan-phase verifikasi apa ada tombol individual delete di partial atau hanya di view lain.)
7. Append `<div class="modal" id="deleteAssessmentModal">` markup (single modal, 2 panel) di akhir partial (sebelum/sesudah pagination).
8. Add `@section Scripts { ... }` atau inline `<script>` block dengan modal handler (event delegation pattern).

**Test (`tests/e2e/assessment.spec.ts`):**
9. Append new `test.describe('FLOW 12: Phase 312 - Admin Full-Delete Role Guard')` block dengan 6 tests (12.1-12.6).

**Manual UAT (`.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md`):**
10. Create new file dengan 6-step Bahasa Indonesia UAT checklist (template: `308-UAT.md` / `310-UAT.md`).

### Creation List (NEW files)

- [ ] `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md` — Manual UAT checklist (6 skenario).

### NO new files needed for:
- Service classes (existing `AuditLogService` cukup).
- Helpers (existing `_userManager`, `User.IsInRole` cukup).
- Migrations (no schema change).
- NuGet/npm packages (stack existing memenuhi).

### Plan Suggestions (untuk Planner)

Per ROADMAP §"Phase 312 Plans 2 plans":

**Plan 312-01 (Backend role guard + audit log extension):**
- Add private helper `EnsureCanDeleteAsync`.
- Edit 3 method delete (inject guard call + extend success AuditLog description).
- Add `GetDeleteImpact` HttpGet action.
- Verify `dotnet build` 0 new warnings.

**Plan 312-02 (Frontend conditional render + modal + smoke test):**
- Edit `_AssessmentGroupsTab.cshtml`: conditional render + modal markup + JS handler.
- Wave 0: extend `tests/e2e/assessment.spec.ts` FLOW 12 + create `312-UAT.md`.
- Wave 1: implementation (modal + JS).
- Manual UAT 6-skenario sign-off.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `responseCount > 0` per row TIDAK di-precompute di Phase 311 partial action | UI conditional render | UI hide imperfect untuk HC + Open + has-response (backend tetap proteksi) |
| A2 | `AssessmentSession.NomorSertifikat` field exists untuk certCount query | GetDeleteImpact code example | Field rename = compile error, plan adjusted |
| A3 | HTMX `htmx:afterSwap` fires reliably setelah Phase 311 partial swap | Pitfall 5 | Modal handler lost setelah filter/pagination — fallback: event delegation di `document.body` |
| A4 | Test data setup pakai `AkhiriUjian` endpoint untuk force Status=Completed (existing API) | Wave 0 Gaps | Alternative: direct DB seed — heavier infra |
| A5 | `[Authorize(Roles = "Admin, HC")]` di `GetDeleteImpact` cukup security-wise (HC bisa lihat impact preview meski tidak boleh delete) | Security threat table | Information disclosure: HC tahu count peserta. Acceptable (HC sudah bisa lihat di ManageAssessment row). |
| A6 | `AssessmentSession.LinkedGroupId` introduced Phase 311 Plan 03 stable, EF Core query reliable | Cascade pattern | Field absence = NRE — verified existing line 2238-2241 pakai field tsb |

**All other claims VERIFIED via direct file read.** No `[CITED]` external sources needed — Phase 312 is fully internal codebase work.

---

## Sources

### Primary (HIGH confidence — verified by direct file read)

- `Controllers/AssessmentAdminController.cs:2018-2317` — 3 method delete bodies, AuditLog signature usage, cascade order
- `Controllers/AssessmentAdminController.cs:114-243` — `ManageAssessmentTab_Assessment` partial action (no responseCount precompute)
- `Controllers/AssessmentAdminController.cs:3419-3435` — `GetAkhiriSemuaCounts` (precedent untuk JSON impact endpoint)
- `Services/AuditLogService.cs:1-44` — `LogAsync` signature confirmed
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:1-310` — partial structure, dropdown menu, 2 existing delete forms
- `Views/Admin/AssessmentMonitoringDetail.cshtml:500-537, 937-962` — `akhiriSemuaModal` precedent (modal markup + AJAX handler)
- `Views/Admin/ManagePackageQuestions.cshtml:255-345` — `editScoreWarningModal` precedent (Bootstrap.Modal API + custom event)
- `Views/Admin/CoachWorkload.cshtml:37-275` + `Views/Admin/Index.cshtml:19-90` — `User.IsInRole("Admin")` Razor precedent
- `tests/e2e/assessment.spec.ts:1-60` — Playwright FLOW 1 pattern (login + describe block)
- `tests/helpers/accounts.ts:1-15` — `admin` + `hc` test accounts confirmed
- `tests/package.json:17` — Playwright `^1.58.2`
- `.planning/config.json` — `nyquist_validation: true`
- `docs/DEV_WORKFLOW.md:1-143` — environment map (lokal/dev/prod)

### Secondary (MEDIUM confidence — pattern derivation)

- D-03 action naming `{Action}Blocked`: NO existing precedent (grep `Blocked` di Controllers/ returned only CONTEXT.md mention). Convention is NEW for this phase — flagged for plan-phase confirmation.
- `tests/helpers/auth.ts` — login pattern existing, BUT Phase 312 needs both admin + hc switching dalam test — pattern confirmed di assessment.spec.ts (login per test)

### Tertiary (LOW confidence — none)

No external WebSearch dilakukan. Phase 312 = internal code work, internal codebase = sufficient evidence.

---

## Metadata

**Confidence breakdown:**
- Standard stack (Bootstrap 5, EF Core, Playwright): HIGH — verified `_Layout.cshtml`, `package.json`, existing usage
- Architecture patterns (modal, AJAX endpoint, role guard): HIGH — verified precedents `akhiriSemuaModal`, `GetAkhiriSemuaCounts`, body-method auth pattern di `_Layout.cshtml`/`CoachWorkload.cshtml`
- Pitfalls (snapshot before cascade, guard placement, PrePost dual-session): HIGH — derived from explicit code reading, all 3 method bodies fully read
- AuditLog `{Action}Blocked` convention: MEDIUM — no direct precedent, recommended naming
- HTMX integration risks: MEDIUM — Phase 311 just shipped, runtime behavior on partial swap not yet UAT'd in Phase 312 context

**Research date:** 2026-05-07
**Valid until:** 2026-06-07 (30 days; stack & patterns stable, codebase under active dev)

---

*Phase: 312-admin-full-delete-assessment-room*
*Research completed: 2026-05-07*
