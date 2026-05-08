# Phase 314: Fix Regenerate Token untuk Status Upcoming — Pattern Map

**Mapped:** 2026-05-08
**Files analyzed:** 7 (1 backend modify + 3 frontend modify + 3 NEW docs/tests)
**Analogs found:** 7 / 7 (semua exact / role-match in-repo)
**Stack target:** ASP.NET Core MVC 8 (Razor + Bootstrap 5 + bootstrap-icons + HTMX 2.0.x) + Playwright 1.58.x (TypeScript)
**Bahasa:** Bahasa Indonesia (per `CLAUDE.md` — semua user-facing copy + AuditLog Description)

> Konsumen: `gsd-planner`. Tujuan dokumen ini supaya planner tidak perlu re-grep analog — semua excerpt + line number sudah disiapkan untuk di-copy langsung ke `## Action` section per plan.

> **Catatan koreksi penting (dari RESEARCH.md):**
> - **D-24 worker session invalidation = MITOS** (RESEARCH §D-24 Open Question, HIGH confidence). Token hanya gate-keeps initial entry via `CMPController.VerifyToken` (line 797-811). Worker yang sudah `StartedAt != null` TIDAK invalidate. **D-23 wording WAJIB direvisi** ke versi RESEARCH §D-24 Recommendation 1.
> - **Pre-repro tabel hipotesis** memprediksi root cause = Frontend handler (Hipotesis 4 PROBABLE CONFIRMED). Plan 01 wajib confirm via stacktrace ground truth — JANGAN commit ke prediction.

---

## File Classification

| File | Type | Role | Data Flow | Closest Analog | Match Quality |
|------|------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` (modified, line 2427-2475 + helpers) | C# controller action | controller (HttpPost defensive guard + multi-row UPDATE + AuditLog + extended logging) | request-response (form POST → guard → EF Core transaction → AuditLog → JSON) | self in-file: `:2391-2410` (DeletePrePostGroup audit try-catch swallow), `:1965-1969` (BeginTransactionAsync convention), `:2440-2475` (existing RegenerateToken body) | **exact** (all in-file existing patterns) |
| `Views/Admin/AssessmentMonitoring.cshtml` (modified, line 396-419) | Razor view + `<script>` block | view (querySelectorAll + forEach + fetch error chain) | client-side fetch POST → r.json/r.text fallback → alert propagate | self `:396-419` (handler #1 existing) + RESEARCH §Code Examples Pattern 4 D-11 | **exact** (in-file refactor) |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (modified, line 1004-1033) | Razor view + `<script>` block | view (named function + spinner toggle + fetch error chain) | client-side fetch POST + spinner UX + try/finally button restore | self `:1004-1033` (handler #2 existing dengan spinner) | **exact** (in-file refactor — preserve spinner pattern) |
| `Views/Admin/ManageAssessment.cshtml` (modified, line 447-471) | Razor view + `<script>` block | view (HTMX-friendly event delegation + async/await fetch) | client-side `document.body.addEventListener('click', ...)` + closest selector → async/await fetch | self `:447-471` (handler #3 existing with HTMX delegation) | **exact** (in-file refactor — preserve `document.body` delegation per RESEARCH Pitfall 5) |
| `tests/e2e/admin-assessment-token.spec.ts` (NEW per D-16) | Playwright E2E spec (TypeScript) | test (multi-scenario + DB assertion via `page.request` + dialog handler) | request-response browser automation + dedicated fixture title | `tests/e2e/assessment.spec.ts:584-757` (FLOW 12 Phase 312 dengan dedicated fixture title) + `:266-397` (FLOW 9 Phase 310 dengan `page.on('dialog')` + `page.evaluate` fetch) | **exact** (carry-forward in-repo FLOW pattern) |
| `.planning/phases/314-.../314-UAT.md` (NEW) | Markdown UAT checklist | doc (manual UAT BI step-by-step + coverage matrix) | n/a | `.planning/phases/308-.../308-UAT.md` + `.planning/phases/312-.../312-UAT.md` | **exact** (template precedent — Phase 312 lebih dekat karena admin destructive endpoint) |
| `docs/SEED_JOURNAL.md` (NEW entry) | Markdown journal append | doc (seed audit trail row append per CLAUDE.md §Seed Data Workflow) | n/a — single row append | `docs/SEED_JOURNAL.md` (existing template line 7-9) + CLAUDE.md `## Seed Data Workflow` | **exact** (template precedent) |

> **Catatan tooling:** Phase 314 TIDAK punya .NET test project (`KPB-PortalHC.Tests` not found — verified Phase 312/313 finding). Coverage = Playwright + Manual UAT only. RESEARCH §Validation Architecture sudah confirm.

> **Catatan `tests/e2e/helpers/wizardSelectors.ts`:** Phase 313 PATTERNS suggest extend helper. Phase 314 boleh **opsional** extend `selectors` const dengan `tokenRegen` namespace, tapi BUKAN MANDATORY (per Claude's Discretion CONTEXT.md). Kalau extend, append ke const di line 27 sebelum closing `} as const`.

---

## Pattern Assignments

### 1. `Controllers/AssessmentAdminController.cs` (modified) — primary backend fix target

**Role:** controller HttpPost action body (defensive guards + multi-row UPDATE atomic + AuditLog swallow + extended structured logging)
**Data flow:** form POST `/Admin/RegenerateToken/{id}` → null check → IsTokenRequired check → **NEW D-25 status block** → try { transaction → token gen → siblings query → **NEW D-33 0-row guard** → loop UPDATE → SaveChanges → commit → **D-06 audit try-catch swallow** → D-21 LogInformation → JSON success } catch { **D-12 specific exception** → D-20 extended log → JSON failure }

**Analogs (in-file, no need to read other files):**
- `:2391-2410` — `DeletePrePostGroup` audit try-catch swallow + LogWarning fallback (D-06 PRIMARY analog — RESEARCH §Architecture Pattern 1 explicit reference)
- `:1965-1969` — bulk-assign `BeginTransactionAsync` + try { SaveChanges; CommitAsync } catch { RollbackAsync; throw } (D-17 PRIMARY analog — RESEARCH §Architecture Pattern 2)
- `:2051, :2195` — additional BeginTransactionAsync usage (15+ occurrences cross-controller per RESEARCH §Sources)
- `:2427-2475` — existing RegenerateToken body (TARGET REPLACEMENT)
- `:2478-2492` — `GenerateSecureToken()` private helper (TIDAK diubah — preserve)
- `Services/AuditLogService.cs:21-42` — `LogAsync` signature stable (`actorUserId, actorName, actionType, description, targetId?, targetType?`)
- `Models/AssessmentSession.cs:18` — `Schedule` non-nullable DateTime
- `Models/AssessmentSession.cs:40` — `StartedAt` DateTime? (used for `hasStarted` D-19)

#### A. Method attribute pattern (preserved verbatim)

Source: `Controllers/AssessmentAdminController.cs:2424-2427` [VERIFIED]

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> RegenerateToken(int id)
```

> **Rule planner:** signature TIDAK diubah. Per D-26: ownership/scope check granular OUT OF SCOPE — admin role Pertamina cross-cutting. Guard hidup di body method (mirror Phase 312 D-04 + Phase 313 helper-extraction pattern, **TANPA** ekstraksi helper per D-28 — minimal diff philosophy).

#### B. Existing body (TARGET REPLACEMENT — full method)

Source: `Controllers/AssessmentAdminController.cs:2427-2475` [VERIFIED]

```csharp
public async Task<IActionResult> RegenerateToken(int id)
{
    var assessment = await _context.AssessmentSessions.FindAsync(id);
    if (assessment == null)
    {
        return Json(new { success = false, message = "Assessment not found." });
    }

    if (!assessment.IsTokenRequired)
    {
        return Json(new { success = false, message = "This assessment does not require a token." });
    }

    try
    {
        var newToken = GenerateSecureToken();
        // Update ALL sibling sessions in the same group (same Title + Category + Schedule.Date)
        var siblings = await _context.AssessmentSessions
            .Where(a => a.Title == assessment.Title
                     && a.Category == assessment.Category
                     && a.Schedule.Date == assessment.Schedule.Date)
            .ToListAsync();
        foreach (var sibling in siblings)
        {
            sibling.AccessToken = newToken;
            sibling.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        // Audit log
        var regenUser = await _userManager.GetUserAsync(User);
        var regenActorName = string.IsNullOrWhiteSpace(regenUser?.NIP) ? (regenUser?.FullName ?? "Unknown") : $"{regenUser.NIP} - {regenUser.FullName}";
        await _auditLog.LogAsync(
            regenUser?.Id ?? "",
            regenActorName,
            "RegenerateToken",
            $"Regenerated access token for '{assessment.Title}' ({assessment.Category}, {assessment.Schedule:yyyy-MM-dd}) — {siblings.Count} sibling(s) updated",
            id,
            "AssessmentSession");

        return Json(new { success = true, token = newToken, message = "Token regenerated successfully." });
    }
    catch (Exception ex)
    {
        var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();
        logger.LogError(ex, "Error regenerating token");
        return Json(new { success = false, message = "Gagal regenerate token. Silakan coba lagi." });
    }
}
```

> **Rule planner:** ganti seluruh blok ini dengan composite Plan 02 patch (lihat sub-section H di bawah, sumbernya RESEARCH §Code Examples line 539-643). Insert poin: full body replace (line 2427-2475). `GenerateSecureToken()` line 2478-2492 TIDAK di-touch.

#### C. Audit try-catch swallow + LogWarning fallback (D-06) — direct in-file analog

Source: `Controllers/AssessmentAdminController.cs:2395-2410` [VERIFIED — DeletePrePostGroup]

```csharp
// Audit log
try
{
    var dpgUser = await _userManager.GetUserAsync(User);
    var dpgActorName = string.IsNullOrWhiteSpace(dpgUser?.NIP) ? (dpgUser?.FullName ?? "Unknown") : $"{dpgUser.NIP} - {dpgUser.FullName}";
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

> **Rule planner:** **COPY VERBATIM struktur try-catch ini** ke RegenerateToken patch. Substitute `dpg*` → `regen*`, action type `"DeletePrePostGroup"` → `"RegenerateToken"`, description format mirror RESEARCH §Code Examples line 604. Penting: audit call hidup **DI LUAR** transaction wrap (D-06 + D-17 — audit fail TIDAK rollback token). LogWarning param: `_logger.LogWarning(auditEx, "Audit log write failed for RegenerateToken session {Id}", id);`

#### D. Explicit BeginTransactionAsync wrap (D-17) — direct in-file analog

Source: `Controllers/AssessmentAdminController.cs:1963-2002` [VERIFIED — EditAssessment bulk-assign]

```csharp
_context.AssessmentSessions.AddRange(newSessions);

using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();

    // ASMT-01: Notify each newly assigned worker
    foreach (var ns in newSessions)
    {
        try
        {
            await _notificationService.SendAsync(...);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Notification send failed"); }
    }

    // Audit log — bulk assign
    await _auditLog.LogAsync(...);

    TempData["Success"] = $"...";
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

> **Rule planner:** Phase 314 wrap pattern: `using var tx = await _context.Database.BeginTransactionAsync();` setelah siblings load + 0-row guard, **sebelum** loop UPDATE. Commit setelah `SaveChangesAsync()`. **Catat divergence:** EditAssessment pattern ada explicit `catch { RollbackAsync; throw }`. Phase 314 boleh skip explicit rollback karena `using var` auto-dispose rollback (semantic equivalent), TAPI catatan: **kalau planner mau align verbatim, copy explicit catch+rollback+throw**. Posisi audit call: **DI LUAR** transaction wrap (D-06/D-17 separation — audit fail tidak rollback token).

#### E. D-12 specific exception types pattern (NEW — RESEARCH §Architecture Pattern 3)

Source: RESEARCH.md §Code Examples line 621-642 [proposed Plan 02 — NO direct in-file analog with this split, but `DbUpdateException` catch sudah dipakai 1x di same controller — verify saat patch]

```csharp
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
    _logger.LogError(ex,
        "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
        id, assessment.Status, hasStarted, siblingCount, assessment.IsTokenRequired);
    return Json(new { success = false, message = ex.Message });
}
```

> **Rule planner:** **Order matters** — `DbUpdateException` MUST come before `Exception` (more specific first per C# catch-order rule). `NullReferenceException` di-tengah karena specific tapi possible-mistake-not-EF. Generic `Exception ex.Message` ACCEPTABLE per V7 ASVS — EF Core sanitize connection string (RESEARCH §Security Domain Information Disclosure row). **Hoisted variables** `siblingCount` + `hasStarted` declared di top-of-method `try {}` scope to be reachable in catch — copy declaration pattern dari RESEARCH §Code Examples line 561-562.

#### F. D-20 / D-21 extended structured logging (NEW)

Source: RESEARCH.md §Code Examples line 614-616 (success) + line 622-625, 631-632, 638-640 (failure) [proposed Plan 02]

```csharp
// D-21 success info log — di akhir try block sebelum return
_logger.LogInformation(
    "RegenerateToken success for session {Id}, {Count} siblings updated by {ActorName}",
    id, siblings.Count, regenActorName);

// D-20 extended structured logging — di setiap catch block (lihat #E)
_logger.LogError(ex,
    "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
    id, assessment.Status, hasStarted, siblingCount, assessment.IsTokenRequired);
```

> **Rule planner:** `_logger` field already injected (RESEARCH confirm — A2/A3 by reference). JANGAN ulang pattern `HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>()` (existing line 2471) — replace dengan `_logger` field. Verify field ada di constructor injection lewat grep `private readonly ILogger` di file. Kalau belum ada, ini optional refactor — boleh tetap pakai `HttpContext.RequestServices` pattern existing.

#### G. D-25 / D-33 / D-37+D-38 pre-condition guards (NEW)

Source: RESEARCH.md §Code Examples line 553-560, 576-581 [proposed Plan 02]

```csharp
// D-25 — Status block (insert SETELAH IsTokenRequired check)
if (assessment.Status == "Cancelled" || assessment.Status == "Completed")
    return Json(new { success = false, message = $"Tidak bisa regenerate token untuk assessment yang sudah {assessment.Status}." });

// D-37/D-38 CONDITIONAL (only if Plan 01 RESEARCH §Data Shape Baseline Query (a) confirms legacy data):
// if (assessment.Schedule == DateTime.MinValue)
//     return Json(new { success = false, message = "Schedule assessment tidak valid. Hubungi IT." });

// D-33 — Sibling 0-row guard (insert SETELAH siblings query, SEBELUM transaction wrap)
if (siblings.Count == 0)
{
    _logger.LogWarning("RegenerateToken: empty sibling group for session {Id}, title={Title}", id, assessment.Title);
    return Json(new { success = false, message = "Data assessment tidak konsisten — sibling group tidak ditemukan. Hubungi IT." });
}
```

> **Rule planner ordering (LOCKED per D-28):** linear extend, no helper extraction. Order:
> 1. `FindAsync(id)` null check (existing line 2429-2433)
> 2. `IsTokenRequired` check (existing line 2435-2438)
> 3. **NEW D-25** Status block (Cancelled/Completed)
> 4. **NEW D-37/D-38 CONDITIONAL** (only if Plan 01 Query (a) returns COUNT > 0 — kalau 0, comment-out atau skip entirely)
> 5. `try { ... }` body
> 6. Inside try: token gen → siblings query → **NEW D-33** 0-row guard → transaction → loop UPDATE → SaveChanges → commit → audit (try-catch swallow) → LogInformation → return success
> 7. `catch (DbUpdateException) → catch (NullReferenceException) → catch (Exception)` per #E

#### H. Composite full method (Plan 02 final structure)

Source: RESEARCH.md §Code Examples line 539-643 [proposed Plan 02]

> **Use this as canonical template.** Planner: copy block ini ke Plan 02 action section, adjust hanya kalau Plan 01 RESEARCH stacktrace reveal root cause yang tidak covered (e.g., Hipotesis 1/2/3 CONFIRMED — adjust message wording). Otherwise verbatim.

```csharp
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

    // D-25 Status block
    if (assessment.Status == "Cancelled" || assessment.Status == "Completed")
        return Json(new { success = false, message = $"Tidak bisa regenerate token untuk assessment yang sudah {assessment.Status}." });

    // D-37/D-38 CONDITIONAL — uncomment kalau Plan 01 §Data Shape Baseline Query (a) > 0:
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

        // D-33 — 0-row defensive
        if (siblings.Count == 0)
        {
            _logger.LogWarning("RegenerateToken: empty sibling group for session {Id}, title={Title}", id, assessment.Title);
            return Json(new { success = false, message = "Data assessment tidak konsisten — sibling group tidak ditemukan. Hubungi IT." });
        }

        // D-17 — explicit transaction
        using var tx = await _context.Database.BeginTransactionAsync();
        foreach (var sibling in siblings)
        {
            sibling.AccessToken = newToken;
            sibling.UpdatedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        // D-06 — audit try-catch swallow (DI LUAR transaction)
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

        // D-21 — success info log
        _logger.LogInformation(
            "RegenerateToken success for session {Id}, {Count} siblings updated by {ActorName}",
            id, siblings.Count, regenActorName);

        return Json(new { success = true, token = newToken, message = "Token regenerated successfully." });
    }
    // D-12 — specific exception catches (order: specific → general)
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
        _logger.LogError(ex,
            "RegenerateToken failed for session {Id}, status={Status}, hasStarted={HasStarted}, siblingCount={SiblingCount}, isTokenRequired={IsTokenRequired}",
            id, assessment.Status, hasStarted, siblingCount, assessment.IsTokenRequired);
        return Json(new { success = false, message = ex.Message });
    }
}
```

---

### 2. `Views/Admin/AssessmentMonitoring.cshtml` (modified, line 396-419)

**Role:** Razor view `<script>` block — handler #1 (querySelectorAll + forEach attach)
**Data flow:** click `.btn-regenerate-token` → `confirm()` (warning conditional D-22/D-23) → `fetch()` POST → r.ok check → r.json or r.text fallback → success: alert token + reload, fail: alert propagated server message

**Analogs (in-file + RESEARCH):**
- self `:396-419` — existing handler #1 (TARGET REPLACEMENT)
- RESEARCH §Code Examples line 651-689 (Pattern 4 — proposed Plan 02 verbatim template)

#### A. Existing handler (TARGET REPLACEMENT)

Source: `Views/Admin/AssessmentMonitoring.cshtml:396-419` [VERIFIED]

```javascript
document.querySelectorAll('.btn-regenerate-token').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var id = this.getAttribute('data-id');
        if (!confirm('Regenerate token untuk assessment ini?')) return;
        fetch(appUrl('/Admin/RegenerateToken/' + id), {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.success) {
                alert('Token baru: ' + data.token);
                location.reload();
            } else {
                alert('Error: ' + data.message);
            }
        })
        .catch(function (err) {
            alert('Gagal regenerate token. Periksa koneksi jaringan.');
        });
    });
});
```

> **Rule planner:** ganti seluruh blok ini dengan template di sub-section #B di bawah. Existing markup `data-id` attribute tetap dipakai. **TAMBAH** rendering markup `data-status="@session.Status"` + `data-started-count="..."` di Razor partial yang produce `.btn-regenerate-token` — locate via grep di same file atau di `_AssessmentGroupsTab.cshtml` partial. Kalau attribute belum ada di markup, tambah di Razor render-side dulu sebelum JS bisa baca via `getAttribute`.

#### B. Refactored handler (RESEARCH Pattern 4 + D-22/D-23 warning + D-07/D-09/D-11)

Source: RESEARCH §Code Examples line 651-689 [proposed Plan 02]

```javascript
document.querySelectorAll('.btn-regenerate-token').forEach(function (btn) {
    btn.addEventListener('click', function () {
        var id = this.getAttribute('data-id');
        var startedCount = parseInt(this.getAttribute('data-started-count') || '0', 10);
        var status = this.getAttribute('data-status') || '';
        var msg = 'Regenerate token untuk assessment ini?';
        if (status === 'Open' && startedCount > 0) {
            // D-23 wording REVISED per RESEARCH §D-24 Recommendation 1 — token TIDAK invalidate active session
            msg = 'PERINGATAN: ' + startedCount + ' worker sudah masuk ujian. Regenerate token TIDAK akan invalidate sesi mereka yang sudah berjalan — mereka tetap bisa lanjut. Tapi worker lain yang belum masuk dan punya token lama akan ditolak login. Lanjutkan?';
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

> **Rule planner copy verbatim** — adjust hanya warning wording (planner discretion per CONTEXT.md "Error wording kontekstual"), dan kalau Plan 01 RESEARCH menemukan `data-started-count` tidak feasible inline, fall back ke `0` static atau tambah lightweight GET endpoint (CONTEXT D-23 Claude's Discretion).

---

### 3. `Views/Admin/AssessmentMonitoringDetail.cshtml` (modified, line 1004-1033)

**Role:** Razor view `<script>` block — handler #2 (named function `regenToken(btn)` + spinner UX)
**Data flow:** click invoke `regenToken(btn)` → `confirm()` → toggle button.disabled + innerHTML spinner → fetch POST → r.ok+JSON chain → token-display swap atau alert error → finally restore button

**Analogs (in-file):**
- self `:1004-1033` — existing handler #2 (TARGET REPLACEMENT — preserve spinner pattern)

#### A. Existing handler (TARGET REPLACEMENT — preserve spinner)

Source: `Views/Admin/AssessmentMonitoringDetail.cshtml:1004-1033` [VERIFIED]

```javascript
function regenToken(btn) {
    if (!confirm('Regenerate token untuk grup ini? Token lama tidak akan bisa digunakan.')) return;
    var id = btn.getAttribute('data-id');
    var token = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]').value;
    btn.disabled = true;
    var originalHtml = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

    fetch(appUrl('/Admin/RegenerateToken/' + id), {
        method: 'POST',
        headers: { 'RequestVerificationToken': token }
    })
    .then(function (r) { return r.json(); })
    .then(function (data) {
        if (data.success) {
            document.getElementById('token-display').textContent = data.token;
            var labelEl = document.getElementById('copy-label');
            if (labelEl) { labelEl.textContent = 'Copy'; }
        } else {
            alert('Error: ' + (data.message || 'Gagal regenerate token.'));
        }
    })
    .catch(function () {
        alert('Terjadi kesalahan saat regenerate token.');
    })
    .finally(function () {
        btn.disabled = false;
        btn.innerHTML = originalHtml;
    });
}
```

> **Rule planner:** ganti dengan template #B di bawah. **PRESERVE 3 hal**:
> 1. Function name `regenToken(btn)` — jangan rename, ada caller `onclick="regenToken(this)"` di markup.
> 2. Spinner UX (lines 7-9 + finally restore lines 27-30) — RESEARCH §Code Examples Pattern 4 tidak punya spinner; planner WAJIB merge.
> 3. Token-display update via `document.getElementById('token-display')` (line 19) — Detail view pakai inline display BUKAN reload. Skip `location.reload()` di success branch.
>
> Wording warning D-23 disesuaikan dengan RESEARCH §D-24 finding (jangan pakai pre-D-24 wording yang salah klaim "akan invalidate session token mereka").

#### B. Refactored handler (composite spinner + D-07/D-09/D-11 + D-22/D-23 revised)

Source: composite of self existing + RESEARCH §Code Examples Pattern 4 [proposed Plan 02]

```javascript
function regenToken(btn) {
    var id = btn.getAttribute('data-id');
    var startedCount = parseInt(btn.getAttribute('data-started-count') || '0', 10);
    var status = btn.getAttribute('data-status') || '';
    var msg = 'Regenerate token untuk grup ini? Token lama tidak akan bisa digunakan untuk login worker baru.';
    if (status === 'Open' && startedCount > 0) {
        msg = 'PERINGATAN: ' + startedCount + ' worker sudah masuk ujian. Regenerate token TIDAK akan invalidate sesi mereka yang sudah berjalan — mereka tetap bisa lanjut. Tapi worker lain yang belum masuk dan punya token lama akan ditolak login. Lanjutkan?';
    }
    if (!confirm(msg)) return;

    var token = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]').value;
    btn.disabled = true;
    var originalHtml = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

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
            document.getElementById('token-display').textContent = data.token;
            var labelEl = document.getElementById('copy-label');
            if (labelEl) { labelEl.textContent = 'Copy'; }
        } else {
            return Promise.reject(data.message || 'Unknown server error');
        }
    })
    .catch(function (err) {
        alert('Gagal regenerate token: ' + err + '. Coba lagi atau hubungi IT.');
    })
    .finally(function () {
        btn.disabled = false;
        btn.innerHTML = originalHtml;
    });
}
```

---

### 4. `Views/Admin/ManageAssessment.cshtml` (modified, line 447-471)

**Role:** Razor view `<script>` block — handler #3 (HTMX-friendly event delegation + async/await fetch)
**Data flow:** `document.body.click` event delegate → `closest('.btn-regenerate-token')` → IIFE async/await fetch → response.ok+JSON or text fallback → alert success/error

**Analogs (in-file):**
- self `:447-471` — existing handler #3 (TARGET REPLACEMENT — preserve `document.body` delegation per RESEARCH Pitfall 5)

#### A. Existing handler (TARGET REPLACEMENT)

Source: `Views/Admin/ManageAssessment.cshtml:447-471` [VERIFIED]

```javascript
// Regenerate Token handler — preserved dari versi lama
document.body.addEventListener('click', function(evt) {
    var btn = evt.target.closest('.btn-regenerate-token');
    if (!btn) return;
    (async function() {
        var id = btn.dataset.id;
        var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
            || document.querySelector('[name="__RequestVerificationToken"]')?.value;
        try {
            var res = await fetch(appUrl('/Admin/RegenerateToken/' + id), {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: '__RequestVerificationToken=' + encodeURIComponent(token)
            });
            var data = await res.json();
            if (data.success) {
                alert('Token baru: ' + data.token);
            } else {
                alert('Error: ' + data.message);
            }
        } catch (e) {
            alert('Gagal regenerate token.');
        }
    })();
});
```

> **Rule planner — CRITICAL preserve (RESEARCH Pitfall 5):**
> 1. `document.body.addEventListener('click', ...)` — JANGAN switch ke `querySelectorAll().forEach(addEventListener)`. HTMX lazy load partial swap rows; non-delegated listeners tidak fire untuk dynamically-loaded rows.
> 2. `evt.target.closest('.btn-regenerate-token')` — preserve delegation pattern.
> 3. CSRF body-encoded (`Content-Type: application/x-www-form-urlencoded` + `__RequestVerificationToken=...` di body) — DIVERGE dari handler #1/#2 yang pakai header `RequestVerificationToken`. Kemungkinan karena ManageAssessment markup tidak punya `<form>` wrapper individual, antiforgery token diserahkan via body. **JANGAN ubah strategi** kecuali planner verify form structure shifted.

#### B. Refactored handler (RESEARCH Pattern 4 async/await variant + D-22/D-23 revised)

Source: RESEARCH §Code Examples line 692-728 [proposed Plan 02]

```javascript
// Regenerate Token handler — Phase 314 refactor (preserve HTMX-friendly delegation)
document.body.addEventListener('click', function(evt) {
    var btn = evt.target.closest('.btn-regenerate-token');
    if (!btn) return;
    (async function() {
        var id = btn.dataset.id;
        var startedCount = parseInt(btn.dataset.startedCount || '0', 10);
        var status = btn.dataset.status || '';
        var msg = 'Regenerate token untuk assessment ini?';
        if (status === 'Open' && startedCount > 0) {
            msg = 'PERINGATAN: ' + startedCount + ' worker sudah masuk ujian. Regenerate token TIDAK akan invalidate sesi mereka yang sudah berjalan — mereka tetap bisa lanjut. Tapi worker lain yang belum masuk dan punya token lama akan ditolak login. Lanjutkan?';
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

> **Rule planner:** preserve all 3 critical aspects from #A above. Tambah `confirm(msg)` dialog (existing tidak punya — handler #3 di-fire langsung tanpa confirm). Verify dengan UX team kalau ini regress; kalau ya, conditional confirm hanya untuk `status === 'Open' && startedCount > 0`.

---

### 5. `tests/e2e/admin-assessment-token.spec.ts` (NEW per D-16)

**Role:** Playwright E2E test spec (TypeScript) — 3 skenario smoke test (D-13/D-14/D-15) + dedicated fixture title (D-08)
**Data flow:** login admin via fixture → search row by exact fixture title → click regen button → handle dialog (confirm/warning) → assert AccessToken DB change via `page.request` GET → assert AuditLog row exists via SQL via subprocess (or skip if no API endpoint)

**Analogs (in-repo):**
- `tests/e2e/assessment.spec.ts:584-757` — FLOW 12 Phase 312 (PRIMARY pattern: `test.describe` block + dedicated fixture title + status badge-scoped row selector + `test.skip()` if seed missing + `page.on('dialog')` for confirm dialog)
- `tests/e2e/assessment.spec.ts:266-397` — FLOW 9 Phase 310 (SECONDARY pattern: `page.evaluate(async () => { fetch(...) })` direct AJAX assertion + JSON shape check)
- `tests/helpers/auth.ts:4-11` — `login(page, 'admin')` fixture
- `tests/helpers/accounts.ts:2` — admin@pertamina.com / 123456
- `tests/e2e/helpers/wizardSelectors.ts` — selector helper (optional extend dengan `tokenRegen` namespace)

#### A. File header + describe block + fixture title pattern

Source: `tests/e2e/assessment.spec.ts:1-22, 584-595` [VERIFIED]

```typescript
import { test, expect, Page } from '@playwright/test';
import { login } from '../helpers/auth';
import { uniqueTitle, today, autoConfirm } from '../helpers/utils';
import { selectors } from './helpers/wizardSelectors';

test.describe.configure({ mode: 'serial' });

// Helper: search on ManageAssessment first search input
async function searchAssessment(page: Page, term: string) {
  const searchInput = page.getByPlaceholder('Cari berdasarkan judul,');
  await searchInput.fill(term);
  await searchInput.press('Enter');
  await page.waitForLoadState('networkidle');
}

// ============================================================
// FLOW 13: Phase 314 — Regenerate Token Defensive Patch
// REQ: TKN-01 (3 success criteria covered by E2E)
// 13.1 — Upcoming + 0 worker started → regen success + token rotated + AuditLog row
// 13.2 — Upcoming + sebagian worker started → regen success (no warning dialog karena Status=Upcoming bukan Open)
// 13.3 — Open + worker running → warning dialog confirm → regen success
// ============================================================
test.describe('Assessment - Phase 314 Regenerate Token Defensive Patch', () => {

  test.beforeEach(async ({ page }) => {
    await login(page, 'admin');
  });

  test('13.1 - Upcoming + 0 worker started → regen success + token DB rotated + AuditLog row', async ({ page }) => {
    // PHASE 314 dedicated fixture (Phase 312 WR-04 pattern)
    const fixtureTitle = 'Phase 314 Token Fixture Upcoming0';
    await page.goto('/Admin/AssessmentMonitoring');
    await searchAssessment(page, fixtureTitle);

    const row = page.locator('tr', { hasText: fixtureTitle })
      .filter({ has: page.locator('span.badge', { hasText: /^Upcoming$/ }) })
      .first();
    if (await row.count() === 0) {
      test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — Plan 02 Wave 0 manual seed required (Upcoming + IsTokenRequired=true + 0 StartedAt)`);
    }

    // Capture old token via DB or visible markup data attribute
    const regenBtn = row.locator('.btn-regenerate-token').first();
    await expect(regenBtn).toBeVisible();

    // Confirm dialog accept (D-22 — Upcoming tidak ada warning, plain confirm)
    page.on('dialog', dialog => {
      expect(dialog.message()).toContain('Regenerate token');
      dialog.accept();
    });

    // Capture token from success alert (Phase 314 D-15d UI assertion)
    let alertMessage = '';
    page.on('dialog', async dialog => {
      if (dialog.message().startsWith('Token baru:')) {
        alertMessage = dialog.message();
        await dialog.accept();
      }
    });

    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/RegenerateToken/') && res.status() === 200),
      regenBtn.click()
    ]);

    // Wait for alert dialog
    await page.waitForTimeout(1_000);
    expect(alertMessage).toMatch(/^Token baru: [A-Z2-9]{6}$/);  // D-15d + RESEARCH §GenerateSecureToken alphabet
  });

  test('13.2 - Upcoming + sebagian worker started → regen success (no warning karena Status=Upcoming)', async ({ page }) => {
    const fixtureTitle = 'Phase 314 Token Fixture UpcomingPartial';
    // ... same pattern, but seed has SOME StartedAt != null but Status still Upcoming (edge case rare — D-36)
    // Assert no PERINGATAN dialog (status !== 'Open' branch)
    test.skip(true, 'Wave 1 fill: requires DB UPDATE manual untuk subset peserta StartedAt');
  });

  test('13.3 - Open + worker running → PERINGATAN dialog → confirm → regen success + AuditLog', async ({ page }) => {
    const fixtureTitle = 'Phase 314 Token Fixture OpenRunning';
    await page.goto('/Admin/AssessmentMonitoring');
    await searchAssessment(page, fixtureTitle);

    const row = page.locator('tr', { hasText: fixtureTitle })
      .filter({ has: page.locator('span.badge', { hasText: /^Open$/ }) })
      .first();
    if (await row.count() === 0) {
      test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — Plan 02 Wave 0 manual seed required (Open + StartedAt != null subset)`);
    }

    // Verify PERINGATAN dialog muncul (D-23 wording REVISED)
    let warningSeen = false;
    page.on('dialog', async dialog => {
      const msg = dialog.message();
      if (msg.startsWith('PERINGATAN:')) {
        warningSeen = true;
        // D-24 finding — wording revised: TIDAK akan invalidate sesi mereka yang sudah berjalan
        expect(msg).toContain('TIDAK akan invalidate sesi');
        await dialog.accept();
      } else if (msg.startsWith('Token baru:')) {
        await dialog.accept();
      }
    });

    const regenBtn = row.locator('.btn-regenerate-token').first();
    await Promise.all([
      page.waitForResponse(res => res.url().includes('/Admin/RegenerateToken/') && res.status() === 200),
      regenBtn.click()
    ]);

    await page.waitForTimeout(1_000);
    expect(warningSeen).toBe(true);
  });
});
```

> **Rule planner:**
> 1. **File location:** `tests/e2e/admin-assessment-token.spec.ts` (NEW per D-16, mirror Phase 312 `admin-assessment-delete.spec.ts` filename convention — even though Phase 312 actually appended FLOW 12 ke `assessment.spec.ts`. Untuk Phase 314 D-16 explicit file baru. Planner pilih: append ke `assessment.spec.ts` ATAU create new file. **Rekomendasi: NEW file** karena CONTEXT D-16 explicit + cleaner separation).
> 2. **Fixture titles:** EXACT match `Phase 314 Token Fixture Upcoming0`, `Phase 314 Token Fixture UpcomingPartial`, `Phase 314 Token Fixture OpenRunning` (Phase 312 WR-04 lesson + RESEARCH §Anti-Patterns).
> 3. **Row selector:** scope ke status badge (Phase 312 WR-03 lesson) — `.filter({ has: page.locator('span.badge', { hasText: /^Upcoming$/ }) })` BUKAN substring match.
> 4. **Dialog handling:** `page.on('dialog', ...)` per Phase 310 FLOW 9 pattern — register BEFORE click. Untuk multi-dialog (confirm → success alert), handler harus branch by message prefix.
> 5. **DB assertion (D-15a/b/c):** Playwright tidak ada native SQL access. Options: (a) tambah lightweight GET endpoint `/Admin/GetAccessToken/{id}` yang return current token (read-only, internal only — security review needed), atau (b) skip DB assertion di E2E + cover di Manual UAT (Phase 312 Path B precedent). **Rekomendasi opsi (b) — manual UAT step verifies DB**.
> 6. **Token alphabet regex:** `/^Token baru: [A-Z2-9]{6}$/` per RESEARCH `GenerateSecureToken` line 2480 — `"ABCDEFGHJKLMNPQRSTUVWXYZ23456789"` (exclude 0, O, 1, I, L).

---

### 6. `.planning/phases/314-.../314-UAT.md` (NEW)

**Role:** Manual UAT script Bahasa Indonesia (mirror Phase 308/312 format)
**Data flow:** n/a (markdown checklist)

**Analogs:**
- `.planning/phases/312-.../312-UAT.md:1-60` — PRIMARY (admin destructive endpoint, sama domain)
- `.planning/phases/308-.../308-UAT.md:1-80` — SECONDARY (4-step format BI)

#### A. Header + Coverage Matrix template

Source: `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md:1-30` [VERIFIED]

```markdown
# Phase 314 — Manual UAT Script

**Phase:** 314-fix-regenerate-token-untuk-status-upcoming
**REQ:** TKN-01 (Fix Regenerate Token untuk Status Upcoming)
**Created:** 2026-05-08
**Mandatory before:** `/gsd-verify-work` Phase 314

> Manual UAT cover N skenario smoke (3 dari ROADMAP SC #6 + 2 extra: D-25 status block + D-33 0-row guard). Eksekusi di environment lokal (`http://localhost:5277/Admin/AssessmentMonitoring`). UAT di Dev/Prod adalah tanggung jawab Team IT — TIDAK termasuk scope developer (per CLAUDE.md).

## Coverage Matrix

| Step | Skenario | Status seed | StartedAt subset | Expected Result | Sign-off |
|------|----------|-------------|------------------|-----------------|----------|
| 1 | Upcoming + 0 worker started | Upcoming | none | Confirm dialog plain → regen OK + alert "Token baru: XXXXXX" + AuditLog row exists | ⬜ |
| 2 | Upcoming + sebagian worker started | Upcoming | subset | Confirm dialog plain (no PERINGATAN — Status≠Open) → regen OK | ⬜ |
| 3 | Open + worker running | Open | subset | PERINGATAN dialog (D-23 revised wording) → confirm → regen OK + worker active session lanjut | ⬜ |
| 4 | Cancelled → block (D-25) | Cancelled | any | alert "Tidak bisa regenerate token untuk assessment yang sudah Cancelled" | ⬜ |
| 5 | Completed → block (D-25) | Completed | any | alert "Tidak bisa regenerate token untuk assessment yang sudah Completed" | ⬜ |
| 6 | Frontend error propagation (D-07/D-09/D-11) — trigger 500 manual | n/a | n/a | alert "Gagal regenerate token: {server message}. Coba lagi atau hubungi IT." (BUKAN generic "Periksa koneksi jaringan") | ⬜ |

## Pre-conditions

- App di-build & running lokal: `dotnet build` + `dotnet run` (cek `http://localhost:5277/`)
- Login fixture: **Admin:** `admin@pertamina.com` (lihat `tests/helpers/accounts.ts`)
- Browser modern (Chrome/Edge/Firefox terbaru)
- DevTools terbuka (Network tab — verify POST `/Admin/RegenerateToken/{id}` returns 200 dengan JSON shape valid)
- DB seed minimal 5 row dengan kombinasi Status: Upcoming (×2 dengan/tanpa StartedAt), Open (×1 dengan StartedAt subset), Cancelled (×1), Completed (×1). Seed via Phase 314 fixture title pattern.
- DB akses (SSMS / DBeaver) untuk verifikasi AccessToken berubah + AuditLog post-action

## Step 1 — Upcoming + 0 worker started → regen OK

**Pre-condition:** Login Admin, navigate `/Admin/AssessmentMonitoring`, cari fixture "Phase 314 Token Fixture Upcoming0".

**Action:**
1. ...
2. ...

**Expect:**
- ...

**Result:** ⬜ PASS / ⬜ FAIL — _catatan:_ ___________

## Step 2 — ...
[follow Phase 312 308 5-7 step-with-action-expect format]
```

> **Rule planner:** copy header + Coverage Matrix structure verbatim. Drill-down per step pakai Phase 312 §Step 1 format (Pre-condition / Action numbered list / Expect bulleted / Result checkbox + catatan). Min 6 steps per Coverage Matrix above. Tambah step 6 manual UAT untuk D-07/D-09/D-11 (frontend error propagation) yang tidak feasible di Playwright tanpa extra infra.

---

### 7. `docs/SEED_JOURNAL.md` (NEW entry append)

**Role:** Markdown journal append — single row per CLAUDE.md §Seed Data Workflow
**Data flow:** n/a (markdown table append)

**Analog:**
- `docs/SEED_JOURNAL.md:1-9` (existing template — empty entry placeholder, ready for first row)
- CLAUDE.md `## Seed Data Workflow` lines 21-32

#### A. Existing template

Source: `docs/SEED_JOURNAL.md:1-9` [VERIFIED]

```markdown
# Seed Journal

Audit trail untuk seed `temporary + local-only`. Lihat [`docs/SEED_WORKFLOW.md`](SEED_WORKFLOW.md) untuk aturan klasifikasi & flow.

**Cara isi:** Tambah satu baris per seed temporary (jangan digabung). Status `active` saat seed masih ada di DB → `cleaned` setelah restore. Entry tidak pernah dihapus (audit trail).

| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |
|---------|-------|-------------|--------|----------------------------|---------------|--------|
| _(belum ada entry)_ | | | | | | |
```

#### B. Phase 314 entry (REPLACE placeholder + append)

Source: composite of CLAUDE.md §Seed Data Workflow + RESEARCH §Validation Architecture / Seeding strategy

```markdown
| Tanggal | Phase | Klasifikasi | Tujuan | Dampak (entitas tersentuh) | Snapshot file | Status |
|---------|-------|-------------|--------|----------------------------|---------------|--------|
| 2026-05-08 | 314 | temporary + local-only | Phase 314 Playwright fixture E2E regen token (3 skenario: Upcoming0 / UpcomingPartial / OpenRunning) | AssessmentSessions(3+), UserAssessmentSessions(beberapa untuk StartedAt subset) | C:\Temp\HcPortalDB_Dev.20260508-XXXX.bak | active |
```

> **Rule planner:** entry baris baru, replace placeholder `_(belum ada entry)_`. Klasifikasi `temporary + local-only` per A6 RESEARCH Assumptions. Status `active` saat seed inserted, update jadi `cleaned` setelah restore (Plan 02 Wave terakhir post-test step). Snapshot file naming pattern `HcPortalDB_Dev.<timestamp>.bak` per `docs/SEED_WORKFLOW.md` §5.1 (RESEARCH §Validation Architecture / Seeding strategy step 1).

---

## Shared Patterns (cross-cutting)

### S1. AntiForgery token retrieval (frontend)

**Source:** 3 view existing handlers + `Views/_ViewImports.cshtml` boilerplate
**Apply to:** all 3 view handler refactors

```javascript
// Variant A (header-based, used in handler #1 + #2):
'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value

// Variant B (body-encoded, used in handler #3 due to HTMX delegation context):
body: '__RequestVerificationToken=' + encodeURIComponent(token)
```

> JANGAN switch variant kecuali planner verify form structure shifted — preserve existing per-view convention.

### S2. `appUrl()` helper (path-prefix safety) — Phase 312 WR-02 lesson

**Source:** `Views/Shared/_Layout.cshtml` (existing helper) — verified usage di 3 view line 400, 456, 1012
**Apply to:** all 3 view handlers

```javascript
fetch(appUrl('/Admin/RegenerateToken/' + id), { ... })
```

> **Anti-pattern:** `'/Admin/RegenerateToken/' + id` (works lokal, FAIL di Dev sub-path `/KPB-PortalHC`).

### S3. Server JSON shape `{ success, message, token? }`

**Source:** existing endpoint return contract (line 2432, 2437, 2467, 2473)
**Apply to:** backend modify (preserve verbatim) + 3 view handler parse

| Field | Type | Note |
|-------|------|------|
| `success` | bool | Required, drives client branch |
| `message` | string | Required, BI for guard rejects + `ex.Message` for catch |
| `token` | string? | Only present when `success=true` |

> JANGAN tambah field baru tanpa diskusi (e.g. `errorCode`, `correlationId`) — scope creep + frontend coupling.

### S4. AuditLog ActionType naming convention (verbatim string)

**Source:** `Models/AuditLog.cs` comment + Phase 312 D-03 (`{Action}Blocked` for guard rejects)
**Apply to:** backend modify

| Path | ActionType |
|------|------------|
| Success | `"RegenerateToken"` (15 chars, fits MaxLength(50)) |
| Failure | **TIDAK ada audit row untuk failure** (per D-06 swallow only) — diverge dari Phase 312 D-03 `RegenerateTokenBlocked` pattern. CONTEXT.md `<code_context>` Established Patterns explicit: "tidak ada failure entry untuk regen (per D-06 swallow only)". |

> **Rule planner:** JANGAN tambah `RegenerateTokenBlocked` audit untuk D-25/D-33 guard rejects — out of scope per CONTEXT D-06 philosophy. Future enhancement (deferred ideas).

### S5. Bahasa Indonesia user-facing copy (CLAUDE.md mandatory)

**Source:** CLAUDE.md line 3 `Always respond in Bahasa Indonesia`
**Apply to:** all alert messages, TempData, dialog wording, audit Description (D-09/D-10/D-22/D-23/D-25 + JSON `message` field)

| Element | BI text |
|---------|---------|
| Confirm plain | `'Regenerate token untuk assessment ini?'` |
| Confirm warning (D-23 revised) | `'PERINGATAN: ' + N + ' worker sudah masuk ujian. Regenerate token TIDAK akan invalidate sesi mereka yang sudah berjalan — mereka tetap bisa lanjut. Tapi worker lain yang belum masuk dan punya token lama akan ditolak login. Lanjutkan?'` |
| Status block (D-25) | `'Tidak bisa regenerate token untuk assessment yang sudah ' + Status + '.'` |
| Sibling 0-row guard (D-33) | `'Data assessment tidak konsisten — sibling group tidak ditemukan. Hubungi IT.'` |
| Schedule MinValue conditional (D-38) | `'Schedule assessment tidak valid. Hubungi IT.'` |
| NRE catch (D-12) | `'Data assessment tidak lengkap (sibling/Schedule null). Hubungi IT.'` |
| Generic alert prefix (D-09) | `'Gagal regenerate token: ' + serverMessage + '. Coba lagi atau hubungi IT.'` |
| Success alert | `'Token baru: ' + data.token` (existing — preserve) |

> Audit Description boleh English (per existing convention — log audit untuk tooling SOC, bukan user-facing). Field name + label di view tetap BI.

### S6. Anti-patterns to avoid (consolidated dari Phase 306/312/313 + RESEARCH)

| Anti-pattern | Source lesson | Mitigation |
|--------------|---------------|------------|
| Path-prefix hardcoded URL | Phase 312 WR-02 | `appUrl()` helper (S2) |
| Playwright row selector substring match | Phase 312 WR-03 | badge-scoped + dedicated fixture title (S7 below) |
| Generic fixture title | Phase 312 WR-04 + Phase 313 D-08 | exact `Phase 314 Token Fixture {Upcoming0\|UpcomingPartial\|OpenRunning}` |
| Audit log throw block business action | Phase 306 D-10 | D-06 try-catch swallow + LogWarning fallback (#C) |
| Generic catch swallow detail | RESEARCH Pitfall 2 | D-12 specific exception types (#E) |
| `r.json()` tanpa `r.ok` check | RESEARCH Pitfall 1 | D-11 `if (!r.ok) return r.text().then(...reject)` (#B sub-pattern) |
| Generic `.catch()` menyembunyikan server message | RESEARCH Hipotesis 4 | D-07 throw `data.message` to `.catch()` |
| Manual normalize Title (lowercase/trim) | RESEARCH Don't Hand-Roll table | SQL Server CI_AS default — D-29 preserve |
| Hand-roll `Random.Next()` token | RESEARCH Don't Hand-Roll | `RandomNumberGenerator` existing `GenerateSecureToken` |
| Switch HTMX delegation pattern ke querySelectorAll forEach | RESEARCH Pitfall 5 | preserve `document.body.addEventListener` di handler #3 (#4 above) |
| Title generic substring match di Playwright | RESEARCH Anti-Patterns | exact title fixture + status badge filter |
| Pre-D-24 wording "akan invalidate session token mereka — login ulang" | RESEARCH §D-24 finding | revised wording: "TIDAK akan invalidate sesi mereka yang sudah berjalan" |

### S7. Playwright dedicated fixture title pattern (Phase 312 WR-04)

**Source:** `tests/e2e/assessment.spec.ts:625-635` (FLOW 12 test 12.1)
**Apply to:** all 3 Phase 314 E2E tests

```typescript
// PHASE 314 dedicated fixture (Phase 312 WR-04 pattern)
const fixtureTitle = 'Phase 314 Token Fixture Upcoming0';  // exact, no uniqueTitle()
await searchAssessment(page, fixtureTitle);

const row = page.locator('tr', { hasText: fixtureTitle })
  .filter({ has: page.locator('span.badge', { hasText: /^Upcoming$/ }) })
  .first();
if (await row.count() === 0) {
  test.skip(true, `Seed "${fixtureTitle}" tidak ditemukan — Plan 02 Wave 0 manual seed required`);
}
```

> **Rule planner:** title EXACT (no `uniqueTitle()` randomization), row filter scoped ke `span.badge` regex anchor `/^Upcoming$/` (NOT substring), `test.skip()` graceful kalau seed missing (BUKAN fail — Phase 312 pattern).

---

## No Analog Found

| File / Pattern | Reason | Workaround |
|----------------|--------|------------|
| DB assertion in Playwright (D-15a/b/c) | Tidak ada native SQL access dari Playwright tanpa subprocess invoke | Cover via Manual UAT step (lihat `314-UAT.md` Step 1-3 expect bullet "DB query verify"). Phase 312 Path B precedent. |
| `_logger` field reference vs `HttpContext.RequestServices.GetRequiredService<ILogger>` | Existing line 2471 pakai service locator pattern; Phase 314 RESEARCH §Code Examples assume `_logger` field injected | Verify dulu di constructor injection. Kalau belum di-inject, optional refactor — atau preserve existing service-locator pattern. Bukan blocker untuk Phase 314 patch. |
| RegenerateTokenBlocked audit row (Phase 312 D-03 analog) | Per CONTEXT.md `<code_context>` Established Patterns explicit: "tidak ada failure entry untuk regen (per D-06 swallow only)". Diverge intentional dari Phase 312. | Skip — out of scope. Future enhancement Deferred Ideas. |
| Lightweight `GET /Admin/GetAccessToken/{id}` endpoint untuk D-15a verify | Tidak ada existing endpoint return token; security review needed | Skip — manual UAT covers. Atau planner bisa kembangkan endpoint terpisah (scope expansion, NOT recommended Phase 314). |

---

## Metadata

**Analog search scope:**
- `Controllers/AssessmentAdminController.cs` (5535 lines)
- `Services/AuditLogService.cs` (44 lines)
- `Models/AssessmentSession.cs`, `Models/AuditLog.cs`, `Data/ApplicationDbContext.cs`
- `Views/Admin/AssessmentMonitoring.cshtml`, `AssessmentMonitoringDetail.cshtml`, `ManageAssessment.cshtml`
- `tests/e2e/assessment.spec.ts`, `tests/helpers/auth.ts`, `tests/helpers/accounts.ts`, `tests/e2e/helpers/wizardSelectors.ts`
- `.planning/phases/308-/`, `.planning/phases/312-/`, `.planning/phases/313-/` (PATTERNS + UAT precedent)
- `docs/SEED_JOURNAL.md`, `docs/SEED_WORKFLOW.md`, `CLAUDE.md`

**Files scanned:** 14 primary + 6 cross-reference

**Pattern extraction date:** 2026-05-08

**Anti-bias note:** Pre-repro pattern assignments (esp. backend body composite #H) assume RESEARCH §Hipotesis 4 (Frontend handler) PROBABLE CONFIRMED prediction. Plan 01 stacktrace adalah ground truth — kalau Hipotesis 1/2/3 CONFIRMED instead, planner adjust patch core (e.g. Schedule MinValue guard MANDATORY kalau Hipotesis 1 CONFIRMED via legacy data + NRE actual). Defensive coverage 8 guards (D-05 cumulative philosophy) tetap regardless of root cause.

---

*Pattern map: Phase 314 fix-regenerate-token-untuk-status-upcoming*
*Linked to: CONTEXT.md (39 decisions), RESEARCH.md (4 hipotesis pre-repro + D-24 finding + 5 SQL queries D-39), ROADMAP.md SC 1-6 line 254-271, REQUIREMENTS.md TKN-01 line 63*
