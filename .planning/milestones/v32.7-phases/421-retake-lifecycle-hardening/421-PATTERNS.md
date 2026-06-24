# Phase 421: Retake Lifecycle Hardening - Pattern Map

**Mapped:** 2026-06-23
**Files analyzed:** 11 (8 modify, 1 new helper, 2 new/extend tests) + 5 test surfaces
**Analogs found:** 11 / 11 (all in-repo; this is a hardening phase — every surface has a sibling pattern already in the codebase)

> ASP.NET Core 8 MVC + EF Core 8 + Razor/Bootstrap 5. migration=FALSE. Branch ITHandoff, run @ `http://localhost:5270`. ALL user-facing text Bahasa Indonesia.
> Almost every change is "close one leaking site of an already-centralized rule." The closest analog is usually the SAME FILE one site over, or the v32.4 sibling (`ShuffleToggleRules`/`ShuffleUpdateEndpointTests`).

## File Classification

| File (modify/new) | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Helpers/RetakeRules.cs` (MODIFY) | utility (pure rules) | transform (decision) | self + `Helpers/ShuffleToggleRules.cs` | exact (same file) |
| `Services/RetakeService.cs` (MODIFY) | service | CRUD / event-driven (destructive TX) | self (`ExecuteAsync`/`CanRetakeAsync` already present) | exact (same file) |
| `CountEraRetakeArchives` helper (NEW) | utility (DB-aware count) | request-response (query) | `Helpers/ShuffleToggleRules.cs` + existing inline predicate (3 sites) | role-match |
| `Controllers/AssessmentAdminController.cs` — `UpdateRetakeSettings` ~:5654 (MODIFY) | controller | request-response (POST/PRG) | self (same method) + `UpdateShuffleSettings` | exact (same method) |
| `Controllers/AssessmentAdminController.cs` — warning count ~:5795 (MODIFY) | controller | request-response (GET ViewBag) | the 3 identical snapshot-presence sites | role-match (DIVERGENT site) |
| `Controllers/AssessmentAdminController.cs` — `ResetAssessment` ~:4286 (MODIFY-light) | controller | request-response (POST → service) | self (delegates to service; D-03 lands in service) | exact |
| `Controllers/AssessmentAdminController.cs` — `EditAssessment` POST `removedUserIds` ~:1924-1971 (MODIFY) | controller | CRUD (cascade delete) | self (loop) + existing TempData guard at :1930 | exact (same loop) |
| `Views/Admin/ManagePackages.cshtml` ~:140-173 (MODIFY) | view | request-response (form+alert) | self (existing inline alert :157-163) | exact |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` ~:334 (MODIFY) | view | request-response (confirm form) | self (existing `onsubmit="return confirm(...)"` :335) | exact |
| `Views/Admin/EditAssessment.cshtml` ~:697-713 (MODIFY) | view | request-response (round-trip hidden flag) | self (form + `@Html.AntiForgeryToken()`) | role-match |
| `HcPortal.Tests/RetakeRulesTests.cs` (EXTEND) | test (pure unit) | transform | self (`Can(...)` helper) | exact |
| `HcPortal.Tests/RetakeServiceTests.cs` (EXTEND) | test (integration real-SQL) | CRUD | self (`SeedSessionAsync`, fixture) | exact |
| `HcPortal.Tests/RetakeSettingsEndpointTests.cs` (EXTEND) | test (integration real-SQL) | request-response | self ("replicate endpoint body" pattern) | exact |
| New `CountEraRetakeArchives` test + participant-remove guard test (NEW) | test (unit + integration) | transform / CRUD | `ShuffleToggleRulesTests.cs` + `RetakeSettingsEndpointTests.cs` | role-match |

---

## Pattern Assignments

### `Helpers/RetakeRules.cs` — D-01 eligibility (utility, transform)

**Analog:** self (`CanRetake`, lines 29-52) + sibling kill-drift pattern `Helpers/ShuffleToggleRules.cs`.

**Read first:** `Helpers/RetakeRules.cs` (full, 85 lines), `Helpers/ShuffleToggleRules.cs` (full, 22 lines), `Controllers/CMPController.cs:955-960` (the +7h WIB expression to mirror byte-identically).

**Pure-rules convention** (`CanRetake` signature + fail-fast guard order, lines 29-52):
```csharp
public static bool CanRetake(
    bool allowRetake, string? assessmentType, bool isManualEntry,
    string status, bool? isPassed, int attemptsUsed, int maxAttempts,
    int retakeCooldownHours, DateTime? completedAt, DateTime nowUtc)
{
    if (!allowRetake) return false;
    if (assessmentType == "PreTest") return false;
    if (isManualEntry) return false;
    if (status != "Completed") return false;
    if (isPassed != false) return false;
    if (attemptsUsed >= maxAttempts) return false;
    // ★ D-01: add window gate HERE (research recommends BEFORE cooldown — window = hard-close, fundamental) ★
    if (retakeCooldownHours <= 0) return true;
    if (completedAt == null) return false;
    return nowUtc >= completedAt.Value.AddHours(retakeCooldownHours);
}
```

**+7h WIB convention to replicate (MUST be byte-identical to StartExam)** — `Controllers/CMPController.cs:956`:
```csharp
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
// In RetakeRules (pure, nowUtc injected): add param `DateTime? examWindowCloseDate`, then:
//   if (examWindowCloseDate.HasValue && nowUtc.AddHours(7) > examWindowCloseDate.Value) return false;
```

**Integration-point WARNING:** `CanRetake` signature change ripples to its ONE production caller — `RetakeService.CanRetakeAsync:244`. Pass `s.ExamWindowCloseDate`. Tier-feedback (`ResolveReviewMode` lines 71-77) and `ShouldHideRetakeToggle` (lines 59-60) stay in this file — Open Question whether `attemptsRemaining` (CMPController:2480) and `IsInCooldown` (CMPController:2493) must also become window-aware (research Pitfalls 1 & 2 — planner must decide; recommend gating both).

---

### `Services/RetakeService.cs` — D-01 execution abort + D-03 cert-null (service, destructive TX)

**Analog:** self. `ExecuteAsync` (lines 69-223), `CanRetakeAsync` (lines 232-248).

**Read first:** `Services/RetakeService.cs` (full, 250 lines).

**D-01 execution gate** — place abort BEFORE the claim `ExecuteUpdateAsync` (currently lines 95-112). The session is loaded at line 72; add window check right after the `Cancelled`/`Open` guards (lines 78-90), before `BeginTransactionAsync` (line 95):
```csharp
// ★ D-01 defense-in-depth: abort BEFORE RemoveRange/ExecuteUpdateAsync if window closed.
//   Mirror +7h WIB verbatim. Return RetakeResult(false, "...") — live session stays intact (not a shell).
if (assessment.ExamWindowCloseDate.HasValue && DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value)
    return new RetakeResult(false, "Masa ujian sudah ditutup — ujian ulang tidak bisa dijalankan.");
```
> NOTE the existing abort idiom returns `new RetakeResult(false, "...")` (e.g. lines 75, 79). Reuse exactly. The `RetakeResult` record is at line 17.

**D-03 cert-null** — add ONE `.SetProperty` line to the existing 9-property claim block (lines 103-112):
```csharp
.ExecuteUpdateAsync(s => s
    .SetProperty(r => r.Status, "Open")
    .SetProperty(r => r.Score, (int?)null)
    .SetProperty(r => r.IsPassed, (bool?)null)
    .SetProperty(r => r.Progress, 0)
    .SetProperty(r => r.StartedAt, (DateTime?)null)
    .SetProperty(r => r.CompletedAt, (DateTime?)null)
    .SetProperty(r => r.ElapsedSeconds, (int)0)
    .SetProperty(r => r.LastActivePage, (int?)null)
    // ★ D-03: + .SetProperty(r => r.NomorSertifikat, (string?)null) ★
    .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
```
> `ResetAssessment` (controller :4338) delegates fully to `ExecuteAsync` — this one line satisfies BOTH HC-reset and worker-retake. No separate cert-null in the controller (Don't Hand-Roll).

**D-05 wire (cap site)** — `CanRetakeAsync` (lines 237-242) is one of the 3 identical snapshot-presence sites; replace inline predicate with the shared `CountEraRetakeArchives` helper. Also re-wire the duplicate inline count inside `ExecuteAsync` (lines 145-150). Both currently read:
```csharp
int eraRetakeArchives = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == s.UserId && h.Title == s.Title && h.Category == s.Category
             && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
    .CountAsync();
```

---

### `CountEraRetakeArchives` helper (NEW utility — DB-aware count, D-05 kill-drift)

**Analog:** `Helpers/ShuffleToggleRules.cs` (static pure helper class pattern) + the 3 identical inline predicates (`RetakeService.cs:145-150`, `RetakeService.cs:237-242`, `CMPController.cs:2472-2475`).

**Read first:** `Helpers/ShuffleToggleRules.cs`, all 3 predicate sites above, the DIVERGENT site `AssessmentAdminController.cs:5795-5798`.

**Discretion (CONTEXT D-05):** name & position are Claude's call, AS LONG AS all 4 sites use the identical snapshot-presence predicate. Recommended: a `Helpers/` static taking `IQueryable<AssessmentAttemptHistory>` + `(title, category)` and exposing the snapshot-presence base query, so callers choose `.Where(UserId==x).CountAsync()` (cap, per-user) vs `.GroupBy(UserId).Select(g=>g.Count()).Max()` (warning, max-in-group).

**Semantic split (research Pitfall 3 — CRITICAL):** Do NOT collapse all 4 to the same shape.
- Sites a/b/c (cap) = per-user count for `(UserId, Title, Category)` — verified `RetakeService.cs:237-242`:
```csharp
.Where(h => h.UserId == s.UserId && h.Title == s.Title && h.Category == s.Category
         && _context.AssessmentAttemptResponseArchives.Any(a => a.AttemptHistoryId == h.Id))
.CountAsync();   // attemptsUsed = result + 1
```
- Site d (warning, ManagePackages) = MAX across all users in group `(Title, Category)`. The ONLY thing missing is the snapshot-presence filter — the `GroupBy(UserId)` is correct (find the worker with the most attempts).
- **Keep `+1`** (current attempt = archives + 1) at ALL four sites.

**Note:** this is DB-aware (touches `_context`), unlike pure `RetakeRules`/`ShuffleToggleRules`. Either pass `DbSet`/`IQueryable` in (preferred — keeps it testable via real-SQL) or make it an extension on `ApplicationDbContext`. Mirror the existing inline predicate's `.Any(a => a.AttemptHistoryId == h.Id)` exactly.

---

### `AssessmentAdminController.cs` — `UpdateRetakeSettings` ~:5654 (controller, POST/PRG) — D-02 + D-07

**Analog:** self (same method, lines 5654-5698) + `UpdateShuffleSettings` (~:5644, sibling PRG pattern).

**Read first:** `AssessmentAdminController.cs:5648-5698` (the method), `:5789-5798` (warning count + helper wire site).

**Existing PRG + TempData + audit idiom** (lines 5660-5697):
```csharp
if (HcPortal.Helpers.RetakeRules.ShouldHideRetakeToggle(assessment.AssessmentType, assessment.IsManualEntry))
{
    TempData["Error"] = "Ujian ulang tidak berlaku untuk Pre-Test atau assessment manual.";
    return RedirectToAction("ManagePackages", new { assessmentId });
}
maxAttempts = Math.Clamp(maxAttempts, 1, 5);
retakeCooldownHours = Math.Clamp(retakeCooldownHours, 0, 168);
// ... sibling propagation foreach ...
await _context.SaveChangesAsync();
// ... audit try/catch warn-only ...
TempData["Success"] = "Pengaturan ujian ulang berhasil disimpan.";
return RedirectToAction("ManagePackages", new { assessmentId });
```

- **D-02 (cooldown > window warning, non-blocking):** after clamp, before/after save, compute whether `RetakeCooldownHours` can push eligibility past the remaining window to `ExamWindowCloseDate`; if so set `TempData["Warning"] = "..."` (toast). **Setting still saves** — non-blocking. Use `TempData["Warning"]` (mirror existing `TempData["Error"]`/`TempData["Success"]` idiom in this method).
- **D-07 (MaxAttempts < used → pre-save confirm modal, non-blocking):** the modal lives in the view (`ManagePackages.cshtml`); server stays non-blocking. Compute "used" via the `CountEraRetakeArchives` helper (D-05, max-in-group form — same value as `ViewBag.RetakeMaxAttemptsUsedInGroup`). If a hidden confirm flag is required by the chosen UX, add a bool param to the signature (mirror D-06's `confirmRemoveWithHistory` approach) — but research Pitfall states D-07 stays JS-confirm/modal pre-action (non-destructive, retroactive behavior preserved at `RetakeRules.cs:46`).

**D-05 at warning count site (`:5795-5798`, the DIVERGENT site)** — current code (NO snapshot filter):
```csharp
int retakeMaxArchivedForGroup = await _context.AssessmentAttemptHistory
    .Where(h => h.Title == assessment.Title && h.Category == assessment.Category)   // ❌ no snapshot-presence
    .GroupBy(h => h.UserId).Select(g => g.Count()).OrderByDescending(c => c).FirstOrDefaultAsync();
ViewBag.RetakeMaxAttemptsUsedInGroup = retakeMaxArchivedForGroup + 1;
```
Re-wire through `CountEraRetakeArchives` (max-in-group form). Keep `+1`. Keep the `GroupBy(UserId)` semantics — only ADD the `.Any(a => a.AttemptHistoryId == h.Id)` snapshot filter.

**Security note:** `[Authorize(Roles = "Admin, HC")]` + `[ValidateAntiForgeryToken]` (lines 5652-5653) MUST be preserved when adding params/flags. Clamp (5667-5668) is server-side defense — keep.

---

### `AssessmentAdminController.cs` — `ResetAssessment` ~:4286 (controller) — D-03 (no logic change) / D-04 trigger

**Analog:** self (lines 4286-4365). Delegates fully to `_retakeService.ExecuteAsync` (line 4338) → D-03 cert-null lands in the service, NOT here. The guard chain (`IsResettable` :4280, Pre-Post block :4304-4319, status whitelist :4322) stays. D-04 is a VIEW-layer confirm (see `AssessmentMonitoringDetail.cshtml` below) — controller is server-authoritative cert-null already (via service).

**Read first:** `AssessmentAdminController.cs:4278-4365`.

**Existing TempData success/error PRG idiom (lines 4347-4364)** — reuse verbatim for any added messaging.

---

### `AssessmentAdminController.cs` — `EditAssessment` POST `removedUserIds` loop ~:1924-1971 (controller, cascade delete) — D-06

**Analog:** self (the loop). Existing guard at `:1930-1939` (currently only InProgress/Completed via TempData+continue).

**Read first:** `AssessmentAdminController.cs:1820-1971` (method signature + Pre-Post branch + removedUserIds loop).

**Method signature to extend** (lines 1823-1826) — add hidden confirm flag (D-06 server round-trip):
```csharp
public async Task<IActionResult> EditAssessment(int id, AssessmentSession model, List<string> NewUserIds,
    DateTime? PreSchedule, int? PreDurationMinutes, DateTime? PreExamWindowCloseDate,
    DateTime? PostSchedule, int? PostDurationMinutes, DateTime? PostExamWindowCloseDate,
    List<string>? UserIds)
    // ★ D-06: add `bool confirmRemoveWithHistory = false` ★
```

**Existing guard to EXTEND** (lines 1930-1939):
```csharp
if (userPreSession != null && (userPreSession.Status == "InProgress" || userPreSession.Status == "Completed")) {
    TempData["Error"] = $"Tidak dapat menghapus peserta — sesi Pre-Test sudah {userPreSession.Status}.";
    continue;
}
// (same for Post, lines 1935-1939)
```
**D-06 expansion:** detect `Status == "Abandoned"` OR `StartedAt != null` OR existing `AttemptHistory` by SessionId. When detected AND `!confirmRemoveWithHistory` → `TempData["Warning"]` + `continue` (cancel that delete). When `confirmRemoveWithHistory == true` → proceed with delete + cleanup.

**Existing cascade-delete idiom in the SAME loop (lines 1947-1970)** — the loop ALREADY removes `AttemptHistory` by SessionId (lines 1953-1956), and DB cascade (`OnDelete(Cascade)`, `ApplicationDbContext.cs:591-594`) removes the archives automatically:
```csharp
var attempts = await _context.AssessmentAttemptHistory
    .Where(h => sessionIdsToRemove.Contains(h.SessionId))
    .ToListAsync();
if (attempts.Any()) _context.AssessmentAttemptHistory.RemoveRange(attempts);
```
> **D-06 cleanup = rely on DB cascade** (Don't Hand-Roll; research Pitfall 4). Removing `AttemptHistory` triggers cascade-delete of `AssessmentAttemptResponseArchives` by `AttemptHistoryId`. Do NOT double-delete archives manually (would error). Add a test asserting 0 orphan archives after delete.

**CRITICAL — dead form clarification:** `DeleteAssessmentPeserta` (referenced by `EditAssessment.cshtml:709` `deletePesertaForm`) does NOT exist in any controller on ITHandoff (grep confirmed: only in the view, the research doc, and the audit doc — zero `.cs` matches). The v32.5 FlexibleParticipantRemove endpoint is on `main`, NOT here. D-06 lives ONLY in the `EditAssessment` POST `removedUserIds` loop. Do NOT pull v32.5 from `main`.

---

### `Views/Admin/ManagePackages.cshtml` ~:140-173 (view) — D-07 modal + D-02 warning copy

**Analog:** self. Existing post-save inline warning alert (lines 157-163) + retake form (lines 143-173).

**Read first:** `Views/Admin/ManagePackages.cshtml:140-176`.

**Existing non-blocking warning alert idiom (lines 157-163)** — this is the Bootstrap `alert alert-warning` pattern to replicate for D-07 / D-02 copy:
```razor
@if ((int)(ViewBag.MaxAttempts ?? 2) < (int)(ViewBag.RetakeMaxAttemptsUsedInGroup ?? 0))
{
    <div class="alert alert-warning d-flex align-items-start mt-2 mb-0" role="alert">
        <i class="bi bi-exclamation-triangle me-2 mt-1"></i>
        <div>Maksimal percobaan yang Anda set lebih kecil dari jumlah percobaan ...</div>
    </div>
}
```

**Existing form + antiforgery idiom (lines 143-145)**:
```razor
<form method="post" asp-action="UpdateRetakeSettings" asp-controller="AssessmentAdmin">
    @Html.AntiForgeryToken()
    <input type="hidden" name="assessmentId" value="@ViewBag.AssessmentId" />
```
**D-07 pre-save confirm:** add `onsubmit="return confirm('...')"` (mirror `AssessmentMonitoringDetail.cshtml:335` idiom below) OR a Bootstrap modal triggered only when `ViewBag.RetakeMaxAttemptsUsedInGroup > maxAttempts`. Discretion (CONTEXT). Keep non-blocking — confirm just gates the POST; server saves regardless.

---

### `Views/Admin/AssessmentMonitoringDetail.cshtml` ~:334 (view) — D-04 confirm cabut-cert

**Analog:** self. Existing reset confirm form (lines 333-342).

**Read first:** `Views/Admin/AssessmentMonitoringDetail.cshtml:320-355`, `Models/AssessmentMonitoringViewModel.cs:48-68`.

**Existing confirm-before-destructive idiom (lines 334-341)**:
```razor
<form asp-action="ResetAssessment" asp-controller="AssessmentAdmin" method="post" class="m-0"
      onsubmit="return confirm('Reset sesi ini? Semua jawaban akan dihapus dan peserta dapat mengulang ujian.')">
    @Html.AntiForgeryToken()
    <input type="hidden" name="id" value="@session.Id" />
    <button type="submit" class="dropdown-item">
        <i class="bi bi-arrow-counterclockwise me-1"></i>Reset
    </button>
</form>
```

**D-04 conditional confirm copy (view-only — VM already exposes the fields):** `MonitoringSessionViewModel` exposes `IsPassed` (bool?, line 55) and `NomorSertifikat` (string?, line 67). NO VM/controller change needed. Build a conditional message:
```razor
@{
    bool hasCert = session.IsPassed == true || session.NomorSertifikat != null;
    string resetConfirm = hasCert
        ? "Reset sesi ini? Sesi ini SUDAH LULUS dan memiliki nomor sertifikat. Mereset akan MENCABUT sertifikat (nomor dihapus) dan semua jawaban dihapus. Lanjutkan?"
        : "Reset sesi ini? Semua jawaban akan dihapus dan peserta dapat mengulang ujian.";
}
<form asp-action="ResetAssessment" ... onsubmit="return confirm('@Html.Raw(Json.Serialize(resetConfirm))')">
```
> RESEARCH note used `session.IsPassed`/`session.NomorSertifikat` and field name `MonitoringSessionRow` — actual VM class is `MonitoringSessionViewModel` (lines 48-68); the iterated variable in the view is `session`. Verify the loop variable name when editing.

---

### `Views/Admin/EditAssessment.cshtml` ~:697-713 (view) — D-06 soft-confirm round-trip flag

**Analog:** self (main edit form + `@Html.AntiForgeryToken()`). The `deletePesertaForm` (lines 708-713) is DEAD (points at non-existent endpoint) — D-06 round-trip rides the MAIN edit form, not this dead form.

**Read first:** `Views/Admin/EditAssessment.cshtml:697-754`.

**D-06 hidden flag:** when the server round-trip returns a warning (peserta ber-riwayat), re-render with a hidden `confirmRemoveWithHistory=true` input inside the main edit form (`editAssessmentForm`), so the next submit carries the flag. Keep `@Html.AntiForgeryToken()` on the re-submit (V13 ASVS). The native `confirm()` (lines 745-754) MAY stay as first-line UX but is NOT the gate — server is authoritative (research Anti-Pattern: JS-only confirm forbidden for D-06).

---

## Shared Patterns

### Pure-rules kill-drift (decision centralization)
**Source:** `Helpers/RetakeRules.cs`, `Helpers/ShuffleToggleRules.cs`
**Apply to:** `RetakeRules.CanRetake` (D-01 param) + new `CountEraRetakeArchives` (D-05)
**Idiom:** static class, caller supplies all facts as params, `nowUtc` injected (never `DateTime.Now` internal). Used in ≥2 sites to prevent divergence.
```csharp
public static class ShuffleToggleRules
{
    public static bool IsShuffleLocked(bool anyStarted, bool anyAssignment) => anyStarted || anyAssignment;
}
```

### +7h WIB window convention (NO drift)
**Source:** `Controllers/CMPController.cs:956`
**Apply to:** D-01 eligibility (`RetakeRules`) + D-01 execution (`RetakeService.ExecuteAsync`) + D-02 warning compute
```csharp
DateTime.UtcNow.AddHours(7) > assessment.ExamWindowCloseDate.Value   // verbatim. Never +8h, never TimeZoneInfo, never DateTime.Now.
```

### Non-blocking toast (TempData)
**Source:** `AssessmentAdminController.cs:4347/4359/5662/5696` (Error/Success), guard `:1932`
**Apply to:** D-02 (`TempData["Warning"]`), D-06 warning (`TempData["Warning"]`)
```csharp
TempData["Error"] = "...";        // existing
TempData["Success"] = "...";      // existing
TempData["Warning"] = "...";      // D-02/D-06 non-blocking warnings (consume in _Layout / view)
```

### Confirm-before destructive (JS confirm / Bootstrap modal)
**Source:** `Views/Admin/AssessmentMonitoringDetail.cshtml:335` (and AkhiriUjian :348)
**Apply to:** D-04 (cabut cert), D-07 (MaxAttempts down). NOT D-06 (server round-trip required).
```razor
onsubmit="return confirm('...Lanjutkan?')"
```

### Server round-trip + hidden confirm flag (server-authoritative)
**Source:** NEW pattern this phase (no prior in ITHandoff); param-default idiom mirrors existing optional params in `EditAssessment` signature (`List<string>? UserIds`).
**Apply to:** D-06 only. Add `bool confirmRemoveWithHistory = false` param; evaluate on SERVER each POST.

### Cascade cleanup via DB FK (no manual RemoveRange of archives)
**Source:** `Data/ApplicationDbContext.cs:588-595` (`OnDelete(DeleteBehavior.Cascade)` on `AssessmentAttemptResponseArchive → AttemptHistory`)
**Apply to:** D-06 — removing `AttemptHistory` (existing loop :1953-1956) auto-removes archives. Do NOT double-delete.

### ExecuteUpdateAsync set-based reset (add 1 SetProperty)
**Source:** `Services/RetakeService.cs:101-112`
**Apply to:** D-03 — add `.SetProperty(r => r.NomorSertifikat, (string?)null)` to the 9-property block.

---

## Test Patterns

### Pure unit (no DB) — extend `RetakeRulesTests.cs`
**Analog:** self. `Can(...)` helper (lines 21-35) with all-eligible defaults + per-branch override; `[Fact]`/`[Theory]` + `[InlineData]`.
**For D-01:** add `DateTime? examWindowCloseDate = null` param to `Can(...)`, then cases: window-open eligible, window-closed (`now+7h > EWCD`) blocked, EWCD null = no gate (backward-compat). Fixed clock `Now = 2026-06-19 12:00 UTC` (line 17).
```csharp
private static bool Can(..., DateTime? examWindowCloseDate = null)
    => RetakeRules.CanRetake(..., examWindowCloseDate, completedAt ?? EligibleCompletedAt, nowUtc ?? Now);
[Fact] public void Blocked_WhenWindowClosed() => Assert.False(Can(examWindowCloseDate: Now));  // now+7h > now
```

### Integration real-SQL — extend `RetakeServiceTests.cs`
**Analog:** self. `RetakeServiceFixture` (lines 34-67, disposable `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`, `MigrateAsync` full chain, `EnsureDeletedAsync` on dispose), `NoOpHubContext` (lines 70-100, hand-stub, NO Moq), `NullLogger`, `[Trait("Category","Integration")]` (line 102). Seed helpers: `SeedSessionAsync` (lines 122-140), `SeedPackageWithResponsesAsync` (143-174), `SeedEraRetakeArchiveAsync` (177-187), `SeedLegacyArchiveAsync` (190-194).
**For D-01/D-03:** add `examWindowCloseDate` + `nomorSertifikat` params to `SeedSessionAsync`. Test: seed EWCD past → `ExecuteAsync` aborts → assert responses/assignment STILL exist + Status unchanged (RTH-01). Seed passed session + cert → `ExecuteAsync` → assert `NomorSertifikat == null` (RTH-02).
```csharp
private static RetakeService NewService(ApplicationDbContext ctx) =>
    new RetakeService(ctx, new AuditLogService(ctx), new NoOpHubContext(), NullLogger<RetakeService>.Instance);
```

### Integration real-SQL "replicate endpoint body" — extend `RetakeSettingsEndpointTests.cs` + NEW participant-remove test
**Analog:** `RetakeSettingsEndpointTests.cs` (shares `RetakeServiceFixture` via `IClassFixture`; replicates endpoint body over real SQL since there is NO WebApplicationFactory — parked 999.12). Pattern: query siblings by key, foreach mutate, SaveChanges, assert. RBAC/AntiForgery/PRG verified via code-grep, NOT HTTP test.
**For RTH-04 (NEW file):** replicate the `removedUserIds` loop body over real SQL — assert Abandoned/with-history rejected without flag (TempData/continue), deleted + 0 orphan archives with flag. Mirror seed/assert style.
**For RTH-03 (NEW file):** `CountEraRetakeArchives` parity test — assert cap (per-user) == warning predicate (snapshot-presence), legacy archive (no child) NOT counted. Unit-or-integration.
**For RTH-05:** extend `RetakeSettingsEndpointTests.cs` warning-count parity if helper D-05 changes ViewBag count.
**Optional (research Wave 0):** extract delete-guard predicate to pure static (e.g. `HasRetakeHistory(...)`) like `IsResettable` → test via `ResetGuardTests` pure pattern (no DB).

### Pure guard predicate test — `ResetGuardTests.cs` style
**Analog:** `ResetGuardTests.cs` (lines 9-20) — `Assert.True/False(AssessmentAdminController.IsResettable(new AssessmentSession {...}))`. Use if D-06 detection is extracted to a static predicate.

**Run commands:** `dotnet test --filter "Category!=Integration"` (per-task, SQL-less, sub-second) · `dotnet test` (per-wave, needs SQLEXPRESS) · full suite + Playwright UAT @5270 (phase gate).

---

## No Analog Found

None. Every surface in this phase has an in-repo analog (this is hardening of an existing v32.4 engine). The single genuinely-new mechanism — **server round-trip + hidden confirm flag (D-06)** — has no prior in ITHandoff but reuses the existing optional-param + TempData + cascade-delete idioms already present in the same `EditAssessment` method. The planner should treat it as composition of existing patterns, not greenfield.

---

## Metadata

**Analog search scope:** `Helpers/`, `Services/`, `Controllers/`, `Views/Admin/`, `Views/CMP/`, `Models/`, `Data/`, `HcPortal.Tests/`
**Files scanned (read line-by-line):** RetakeRules.cs, ShuffleToggleRules.cs, RetakeService.cs, AssessmentAdminController.cs (4 regions), CMPController.cs (2 regions), ManagePackages.cshtml, AssessmentMonitoringDetail.cshtml, EditAssessment.cshtml, AssessmentMonitoringViewModel.cs, ApplicationDbContext.cs (cascade), AssessmentSession.cs (fields), RetakeRulesTests.cs, RetakeServiceTests.cs, RetakeSettingsEndpointTests.cs, ResetGuardTests.cs
**Key verifications:** `DeleteAssessmentPeserta` = 0 controller matches (dead form, view-only); `ExamWindowCloseDate` (AssessmentSession:77) + `NomorSertifikat` (AssessmentSession:91) exist → migration=FALSE confirmed; cascade FK config present (DbContext:588-595).
**Pattern extraction date:** 2026-06-23
