# Phase 312: Admin Full-Delete Assessment Room — Pattern Map

**Mapped:** 2026-05-07
**Files analyzed:** 4 (3 modified + 1 created)
**Analogs found:** 4 / 4 (semua exact / role-match in-repo)
**Stack target:** ASP.NET Core MVC 8 (Razor + Bootstrap 5 + bootstrap-icons + HTMX 2.0.x)
**Bahasa:** Bahasa Indonesia (per `CLAUDE.md` — semua user-facing copy)

> Konsumen: `gsd-planner`. Tujuan dokumen ini supaya planner tidak perlu re-grep analog — semua excerpt + line number sudah disiapkan untuk di-copy langsung ke `## Action` section per plan.

---

## File Classification

| File | Type | Role | Data Flow | Closest Analog | Match Quality |
|------|------|------|-----------|----------------|---------------|
| `Controllers/AssessmentAdminController.cs` (modified) | C# controller actions | controller (HttpPost cascade-delete + HttpGet AJAX) | request-response (form POST → cascade DB → AuditLog → redirect; HttpGet → JSON) | self (line 2018-2317 untuk delete, line 3419-3435 untuk JSON endpoint) | **exact** (in-file existing patterns) |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (modified) | Razor partial view | view component (dropdown actions + modal + script block) | client-side modal orchestration + form POST submit | `Views/Admin/AssessmentMonitoringDetail.cshtml:500-537, 937-962` (`akhiriSemuaModal`) + `Views/Admin/CoachWorkload.cshtml:37-275` (Razor role guard) | **exact** (modal markup + AJAX handler precedent) |
| `tests/e2e/assessment.spec.ts` (modified) | Playwright E2E spec | test (multi-role flow + DOM assertion + AJAX response) | request-response | `tests/e2e/assessment.spec.ts:266-401` (FLOW 9 Phase 310 describe block) + `:444-480` (FLOW 3 Edit & Delete) | **exact** (in-file FLOW pattern) |
| `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md` (created) | Markdown UAT checklist | doc (manual UAT BI step-by-step) | n/a | `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` + `.planning/phases/310-essay-finalize-idempotency/310-UAT.md` | **role-match** (template precedent) |

---

## Pattern Assignments

### 1. `Controllers/AssessmentAdminController.cs` (modified)

**Role:** controller actions
**Data flow:** form POST → guard → cascade EF Core → AuditLog → TempData + RedirectToAction (existing pattern preserved)

**Analogs (in-file, no need to read other files):**
- `:2018-2121` — `DeleteAssessment(int id)` (single delete cascade pattern, AuditLog convention)
- `:2123-2226` — `DeleteAssessmentGroup(int id)` (group cascade, sibling load via Title+Category+Schedule.Date)
- `:2228-2317` — `DeletePrePostGroup(int linkedGroupId)` (Pre-Post pair cascade via `LinkedGroupId`)
- `:3419-3435` — `GetAkhiriSemuaCounts` (HttpGet JSON endpoint blueprint untuk `GetDeleteImpact`)
- `Services/AuditLogService.cs:21-42` — `LogAsync` signature stable

#### A. Action attribute pattern (preserved verbatim, semua 3 method)

Source: `Controllers/AssessmentAdminController.cs:2019-2022, 2124-2127, 2229-2232`

```csharp
[HttpPost]
[Authorize(Roles = "Admin, HC")]   // ← TIDAK DIUBAH (per SC #2 + boundary CONTEXT.md line 10)
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteAssessment(int id)
```

> **Rule untuk planner:** attribute block jangan di-touch. Guard lives di body method, bukan attribute.

#### B. Try/catch + logger + TempData pattern (existing, preserved)

Source: `Controllers/AssessmentAdminController.cs:2024-2046, 2114-2120`

```csharp
var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AssessmentAdminController>>();

try
{
    var assessment = await _context.AssessmentSessions
        .FirstOrDefaultAsync(a => a.Id == id);

    if (assessment == null)
    {
        logger.LogWarning($"Delete attempt failed: Assessment {id} not found");
        TempData["Error"] = "Assessment not found.";
        return RedirectToAction("ManageAssessment");
    }

    // ... existing PrePost block (line 2042-2046) UNCHANGED — masih perlu untuk D-19 ...

    // ⬇ PHASE 312 INSERT POINT: guard call setelah PrePost block, sebelum cascade
    // var blockResult = await EnsureCanDeleteAsync(...);
    // if (blockResult != null) return blockResult;

    // ... cascade existing UNCHANGED ...

    await _context.SaveChangesAsync();
    // audit log UNCHANGED structure (B + extend description) ...
    return RedirectToAction("ManageAssessment");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error deleting assessment {Id}", id);
    TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
    return RedirectToAction("ManageAssessment");
}
```

> **Rule planner:** guard call sisip di **awal try block** setelah `assessment == null` check + setelah existing PrePost early-return (untuk `DeleteAssessment` saja). Untuk `DeleteAssessmentGroup` sisip setelah `siblings = await _context.AssessmentSessions.Where(...).ToListAsync()` line 2152. Untuk `DeletePrePostGroup` sisip setelah `groupSessions = await _context.AssessmentSessions.Where(a => a.LinkedGroupId == linkedGroupId).ToListAsync()` line 2241. **Pre-cascade**, supaya kalau guard reject tidak ada DB write yang sudah terjadi.

#### C. AuditLog success call signature (existing, extend description)

Source: `Controllers/AssessmentAdminController.cs:2094-2109` (DeleteAssessment), `:2199-2214` (DeleteAssessmentGroup), `:2291-2306` (DeletePrePostGroup)

```csharp
// Audit log
try
{
    var deleteUser = await _userManager.GetUserAsync(User);
    var deleteActorName = string.IsNullOrWhiteSpace(deleteUser?.NIP)
        ? (deleteUser?.FullName ?? "Unknown")
        : $"{deleteUser.NIP} - {deleteUser.FullName}";
    await _auditLog.LogAsync(
        deleteUser?.Id ?? "",
        deleteActorName,
        "DeleteAssessment",                                                       // ← actionType
        $"Deleted assessment '{assessmentTitle}' [ID={id}]",                       // ← description (PHASE 312: extend dengan Status + ResponseCount)
        id,
        "AssessmentSession");
}
catch (Exception auditEx)
{
    logger.LogWarning(auditEx, "Audit log write failed for DeleteAssessment {Id}", id);
}
```

> **Rule planner (D-03 success):** description string di-extend mengikuti UI-SPEC line 167-169:
> - Single: `$"Deleted assessment '{assessmentTitle}' [ID={id}] Status={preDeleteStatus} ResponseCount={preDeleteResponseCount}"`
> - Group: `$"Deleted assessment group '{rep.Title}' [RepId={id}] SessionCount={siblings.Count} Status={statusSummary} ResponseCount={preDeleteResponseCount}"`
> - PrePost: `$"Deleted Pre-Post group '{groupTitle}' [LinkedGroupId={linkedGroupId}] Status=PreTest:{x},PostTest:{y} ResponseCount={total}"`
>
> **Snapshot WAJIB sebelum cascade** (line 2089 SaveChangesAsync — setelah ini assessment.Status sudah hilang). Capture `preDeleteStatus`, `preDeleteResponseCount` ke local var di **awal try block** (setelah guard pass). Reference `_auditLog.LogAsync` signature di `Services/AuditLogService.cs:21-27`.

#### D. AuditLog blocked entry pattern (NEW — D-03; convention baru)

Pattern derived dari B + C, tapi action type suffix `"Blocked"`. Helper `EnsureCanDeleteAsync` yang akan tulis entry. Skeleton:

```csharp
// Insert as private helper di akhir class (sebelum closing brace) — atau dekat existing helpers
private async Task<IActionResult?> EnsureCanDeleteAsync(
    string actionPrefix,                       // "DeleteAssessment" | "DeleteAssessmentGroup" | "DeletePrePostGroup"
    int targetId,
    string entityType,                         // "AssessmentSession"
    IList<AssessmentSession> sessions)
{
    // Admin override: lewati semua cek
    if (User.IsInRole("Admin")) return null;

    // HC tier: cek Status==Completed atau ada response peserta
    var sessionIds = sessions.Select(s => s.Id).ToList();
    int responseCount = await _context.PackageUserResponses
        .CountAsync(r => sessionIds.Contains(r.AssessmentSessionId));
    bool anyCompleted = sessions.Any(s => s.Status == "Completed");

    if (anyCompleted || responseCount > 0)
    {
        // AuditLog blocked entry
        try
        {
            var blockUser = await _userManager.GetUserAsync(User);
            var blockActor = string.IsNullOrWhiteSpace(blockUser?.NIP)
                ? (blockUser?.FullName ?? "Unknown")
                : $"{blockUser.NIP} - {blockUser.FullName}";
            string statusSummary = anyCompleted
                ? string.Join(",", sessions.Select(s => s.Status))
                : sessions.First().Status;
            await _auditLog.LogAsync(
                blockUser?.Id ?? "",
                blockActor,
                $"{actionPrefix}Blocked",
                $"HC role blocked from {actionPrefix} [TargetId={targetId}]: Status={statusSummary}, ResponseCount={responseCount}",
                targetId,
                entityType);
        }
        catch (Exception ex)
        {
            // logger swallow — pattern dari existing AuditLog catch (line 2106-2109)
        }

        TempData["Error"] = "Anda tidak memiliki izin untuk menghapus assessment yang sudah Completed atau memiliki jawaban peserta.";
        return RedirectToAction("ManageAssessment");
    }

    return null;  // pass — caller lanjut cascade
}
```

> **Convention naming `Blocked`:** TIDAK ada precedent existing di repo (RESEARCH.md line 1073 confirmed via grep miss). Konvensi baru reasonable + parallel ke action prefix. Planner finalisasi.

#### E. New HttpGet `GetDeleteImpact` (analog `GetAkhiriSemuaCounts`)

Source analog: `Controllers/AssessmentAdminController.cs:3419-3435`

```csharp
// EXISTING (analog skeleton):
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> GetAkhiriSemuaCounts(string title, string category, DateTime scheduleDate)
{
    var sessions = await _context.AssessmentSessions
        .Where(a => a.Title == title
                 && a.Category == category
                 && a.Schedule.Date == scheduleDate.Date
                 && (a.Status == "Open" || a.Status == "InProgress"))
        .ToListAsync();

    int inProgressCount = sessions.Count(s => s.StartedAt != null && s.CompletedAt == null && s.Score == null);
    int notStartedCount = sessions.Count(s => s.StartedAt == null);

    return Json(new { inProgressCount, notStartedCount });
}
```

**Apply for `GetDeleteImpact(string type, int id)`:** signature serupa, branch by `type ∈ {"single","group","prepost"}`, kembalikan JSON `{ status, responseCount, certCount, packageCount, attemptCount, prePostBreakdown?, sessionCount }`. Detail lengkap di RESEARCH.md Example 2 (line 615-693). Tempatkan **setelah** `GetAkhiriSemuaCounts` (~line 3435) untuk grouping locality dengan AJAX endpoint lain.

> **Rule planner:** semua query pakai `.AsNoTracking()` (read-only — saving cycles). Validasi `type` dengan early `BadRequest` untuk safety. Pertahankan `[Authorize(Roles = "Admin, HC")]` (HC boleh lihat impact preview meski tidak boleh delete — Assumption A5).

---

### 2. `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (modified)

**Role:** Razor partial view (refactor dropdown actions + append modal markup + append script block)
**Data flow:** Bootstrap 5 modal `show.bs.modal` → `fetch()` AJAX `/Admin/GetDeleteImpact` → DOM populate → user click Lanjutkan → swap step → form POST submit (browser navigation)

**Analogs:**
- **Modal markup precedent:** `Views/Admin/AssessmentMonitoringDetail.cshtml:500-537` (`akhiriSemuaModal` — sama-sama destructive, sama-sama load JSON impact)
- **Modal AJAX handler precedent:** `Views/Admin/AssessmentMonitoringDetail.cshtml:937-962` (`show.bs.modal` event + fetch + spinner swap)
- **Razor role guard:** `Views/Admin/CoachWorkload.cshtml:37-42, 63-68`
- **Existing dropdown markup yang akan di-refactor:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:248-274`

#### A. Existing dropdown form-with-confirm pattern (REPLACE)

Source: `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:248-274` (current state — yang akan di-refactor)

```razor
<li><hr class="dropdown-divider"></li>
@if ((bool)group.IsPrePostGroup)
{
    <!-- D-19: delete hanya per-grup untuk Pre-Post -->
    <li>
        <form method="post" asp-controller="AssessmentAdmin" asp-action="DeletePrePostGroup">
            @Html.AntiForgeryToken()
            <input type="hidden" name="linkedGroupId" value="@group.LinkedGroupId" />
            <button type="submit" class="dropdown-item text-danger"
                    onclick="return confirm('Tindakan ini akan menghapus kedua sesi (Pre dan Post), semua paket soal, jawaban peserta, dan assignment secara permanen. Lanjutkan?')">
                <i class="bi bi-trash3 me-2"></i>Hapus Grup Pre-Post
            </button>
        </form>
    </li>
}
else
{
    <li>
        <form method="post" asp-controller="AssessmentAdmin" asp-action="DeleteAssessmentGroup" asp-route-id="@group.RepresentativeId">
            @Html.AntiForgeryToken()
            <button type="submit" class="dropdown-item text-danger"
                    onclick="return confirm('Hapus semua assessment dalam grup ini?')">
                <i class="bi bi-trash me-2"></i>Hapus Grup
            </button>
        </form>
    </li>
}
```

> **Rule planner:** ganti per-row `<form>...</form>` dengan single `<button data-bs-toggle="modal" data-bs-target="#deleteAssessmentModal" data-delete-type="..." data-delete-id="..." data-delete-title="...">`. Form tunggal pindah ke bottom of partial sebagai shared modal submit. Lihat UI-SPEC line 191-209 untuk markup template.

#### B. Razor role guard pattern (apply at button render)

Source: `Views/Admin/CoachWorkload.cshtml:37-42`

```razor
@if (User.IsInRole("Admin"))
{
    <button type="button" class="btn btn-primary" data-bs-toggle="modal" data-bs-target="#thresholdModal">
        <i class="bi bi-gear me-1"></i>Set Threshold
    </button>
}
```

**Apply for D-01 (UI hide HC saat Completed):** wrap dropdown `<li>` dengan `@if (isAdmin || canHcDelete)` per UI-SPEC line 191-209.

```razor
@{
    bool isAdmin = User.IsInRole("Admin");
    string groupStatus = (string)group.Status;
    bool canHcDelete = groupStatus != "Completed";   // simplified — responseCount cek di server (D-01 + Q1 opsi B)
}
@if (isAdmin || canHcDelete)
{
    <li>
        <button type="button" class="dropdown-item text-danger"
                data-bs-toggle="modal" data-bs-target="#deleteAssessmentModal"
                data-delete-type="@(group.IsPrePostGroup ? "prepost" : "group")"
                data-delete-id="@(group.IsPrePostGroup ? group.LinkedGroupId : group.RepresentativeId)"
                data-delete-title="@group.Title">
            <i class="bi bi-trash3 me-2"></i>@(group.IsPrePostGroup ? "Hapus Grup Pre-Post" : "Hapus Grup")
        </button>
    </li>
}
```

#### C. Modal markup template (analog `akhiriSemuaModal`)

Source: `Views/Admin/AssessmentMonitoringDetail.cshtml:500-537`

```razor
<div class="modal fade" id="akhiriSemuaModal" tabindex="-1" aria-labelledby="akhiriSemuaModalLabel" aria-hidden="true">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header bg-danger text-white">
                <h5 class="modal-title fw-semibold" id="akhiriSemuaModalLabel">
                    <i class="bi bi-x-circle me-2"></i>Akhiri Semua Ujian
                </h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body">
                <div id="akhiriSemuaLoading" class="text-center py-3">
                    <span class="spinner-border spinner-border-sm me-2"></span>Memuat data...
                </div>
                <div id="akhiriSemuaContent" style="display:none;">
                    <p class="mb-2 fw-semibold">Akhiri semua ujian?</p>
                    <ul class="mb-2">
                        <li><strong id="akhiriInProgressCount">0</strong> peserta InProgress akan dinilai dari jawaban tersimpan.</li>
                        <li><strong id="akhiriNotStartedCount">0</strong> peserta belum mulai akan dibatalkan.</li>
                    </ul>
                    <p class="text-danger mb-0"><i class="bi bi-exclamation-triangle-fill me-1"></i>Tindakan ini tidak dapat dibatalkan.</p>
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Batal</button>
                <form asp-action="AkhiriSemuaUjian" asp-controller="AssessmentAdmin" method="post" class="d-inline">
                    @Html.AntiForgeryToken()
                    <input type="hidden" name="title" value="@Model.Title" />
                    <input type="hidden" name="category" value="@Model.Category" />
                    <input type="hidden" name="scheduleDate" value="@Model.Schedule.ToString("yyyy-MM-dd")" />
                    <button type="submit" class="btn btn-danger" id="akhiriSemuaSubmitBtn">
                        <i class="bi bi-x-circle me-1"></i>Ya, Akhiri Semua
                    </button>
                </form>
            </div>
        </div>
    </div>
</div>
```

**Differences for `#deleteAssessmentModal`:**
- ID: `deleteAssessmentModal` + label `deleteAssessmentModalLabel`
- Title dinamis (set dari JS pakai `data-delete-title`): `Hapus Assessment: ...` / `Hapus Grup Assessment: ...` / `Hapus Grup Pre-Post Test: ...`
- Body **2 panels** (bukan 1): `#dam-step-1` (impact preview list 5 baris + optional `#dam-prepost-breakdown`) + `#dam-step-2` (alert-danger warning + cascade enumeration list + microcopy "Lanjutkan?"). Default `#dam-step-1` visible, `#dam-step-2` `display:none`.
- Footer **2 versions**: `#dam-footer-1` (Batal + Lanjutkan→) + `#dam-footer-2` (←Kembali + Hapus Permanen submit). Swap parallel dengan body panel.
- Form: dynamic `action` attribute set by JS — single `<form id="dam-submit-form">` dengan 2 hidden field (`#dam-form-id` name=`id`, `#dam-form-linkedid` name=`linkedGroupId`) yang JS toggle `disabled` per type. `@Html.AntiForgeryToken()` WAJIB.
- Modal-dialog default `modal-md`; JS switch ke `modal-lg` saat `data-delete-type === 'prepost'` (per UI-SPEC line 47-48).

> **Copy strings literal:** semua label/heading/microcopy ambil dari UI-SPEC line 100-174. Jangan invent ulang.

#### D. Modal AJAX handler pattern (analog `akhiriSemuaModal` JS)

Source: `Views/Admin/AssessmentMonitoringDetail.cshtml:937-962`

```js
// ---- Akhiri Semua Modal: fetch counts on open ----
var akhiriModal = document.getElementById('akhiriSemuaModal');
if (akhiriModal) {
    akhiriModal.addEventListener('show.bs.modal', function () {
        var loading = document.getElementById('akhiriSemuaLoading');
        var content = document.getElementById('akhiriSemuaContent');
        loading.style.display = '';
        content.style.display = 'none';

        var url = appUrl('/Admin/GetAkhiriSemuaCounts')
            + '?title=' + encodeURIComponent(hTitle)
            + '&category=' + encodeURIComponent(hCategory)
            + '&scheduleDate=' + encodeURIComponent(hScheduleDate);

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                document.getElementById('akhiriInProgressCount').textContent = data.inProgressCount || 0;
                document.getElementById('akhiriNotStartedCount').textContent = data.notStartedCount || 0;
                loading.style.display = 'none';
                content.style.display = '';
            })
            .catch(function () {
                loading.innerHTML = '<span class="text-danger">Gagal memuat data. Tutup dan coba lagi.</span>';
            });
    });
}
```

**Apply for `deleteAssessmentModal`:**
1. **`show.bs.modal` event:** ambil `event.relatedTarget.dataset` — set form action + hidden field name + title + state reset (step 1 visible, step 2 hidden, spinner visible, content hidden, Lanjutkan disabled).
2. **fetch `/Admin/GetDeleteImpact?type=...&id=...`** — populate `#dam-status`, `#dam-response-count`, `#dam-cert-count`, `#dam-package-count`, `#dam-attempt-count`. Untuk type=prepost, render `#dam-prepost-breakdown` list + tambah class `modal-lg` di `.modal-dialog`.
3. **Step navigation buttons** `#dam-next-btn` + `#dam-back-btn` — swap `style.display` antara step 1 ↔ step 2 + footer 1 ↔ footer 2.
4. **Error path:** `.catch(...)` ganti `#dam-loading` innerHTML dengan `<span class="text-danger">Gagal memuat data dampak. Tutup dan coba lagi.</span>` (per UI-SPEC line 126).

Skeleton lengkap di RESEARCH.md Example 4 (line 748-832).

> **HTMX consideration (Pitfall A3, RESEARCH.md):** Phase 311 sudah introduce HTMX partial swap. Modal handler attached lewat IIFE di partial bisa **lost** setelah filter/pagination swap. Mitigation: pasang event listener di `document.body` dengan `addEventListener('htmx:afterSwap', ...)` re-init flag, atau pakai event delegation langsung `document.body.addEventListener('show.bs.modal', ...)` yang naturally bubble. Planner pilih yang terbaik untuk maintainability.

#### E. AntiForgeryToken pattern (preserved)

Source: `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:254, 267` + `Views/Admin/AssessmentMonitoringDetail.cshtml:526`

```razor
@Html.AntiForgeryToken()
```

> **Rule planner:** modal form WAJIB carry `@Html.AntiForgeryToken()` karena 3 controller action target pakai `[ValidateAntiForgeryToken]`. Tempatkan inside `<form id="dam-submit-form">`.

#### F. TempData flash banner (existing, preserved at parent layout)

Pattern source: `Views/Admin/CoachWorkload.cshtml:46-60`

```razor
@if (TempData["Success"] != null)
{
    <div class="alert alert-success alert-dismissible fade show">
        <i class="bi bi-check-circle me-2"></i>@TempData["Success"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
@if (TempData["Error"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show">
        <i class="bi bi-exclamation-triangle me-2"></i>@TempData["Error"]
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

> **Rule planner:** TIDAK perlu tambah di partial. `ManageAssessment.cshtml` shell sudah render TempData flash. Phase 312 hanya men-set `TempData["Success"]` / `TempData["Error"]` di controller (existing convention). Strings copy dari UI-SPEC line 153-161.

---

### 3. `tests/e2e/assessment.spec.ts` (modified)

**Role:** Playwright E2E spec (append FLOW 12 describe block)
**Data flow:** browser → login (admin/hc) → navigate → modal open → AJAX response → assert DOM/JSON → submit → assert RedirectToAction outcome

**Analogs (in-file):**
- `tests/e2e/assessment.spec.ts:266-401` (FLOW 9 Phase 310 — pattern paling recent untuk multi-test describe block + scaffold-with-skip + login per test)
- `tests/e2e/assessment.spec.ts:444-480` (FLOW 3 — existing delete dropdown test, partial parity)
- `tests/helpers/auth.ts:4-11` — `login(page, 'admin' | 'hc')` helper
- `tests/helpers/accounts.ts:1-12` — `admin` + `hc` fixtures

#### A. Describe block boilerplate

Source: `tests/e2e/assessment.spec.ts:266-275`

```ts
// FLOW 9: Phase 310 — Essay Finalize Idempotency
// REQ: ESCG-01 (5 success criteria)
// SC #1 alreadyFinalized branch + SC #2 disabled state — auto E2E
// SC #3 (notif dedup) + SC #4 (audit dedup) + SC #5 (parallel finalize) — manual UAT
// ============================================================
test.describe('Assessment - Phase 310 Essay Finalize Idempotency', () => {

  test.beforeEach(async ({ page }) => {
    await login(page, 'hc');
  });
```

**Apply for FLOW 12:**

```ts
// ============================================================
// FLOW 12: Phase 312 — Admin Full-Delete Role Guard
// REQ: DEL-01 (6 success criteria)
// 12.0 — GetDeleteImpact JSON helper test
// 12.1-12.5 — 5 SC matrix smoke (Admin+Open, Admin+Completed, HC+Open noresp, HC+Completed BLOCK, HC+Open hasresp BLOCK)
// 12.6 — extra HC+PrePost+Completed BLOCK (D-04 scope expansion)
// ============================================================
test.describe('Assessment - Phase 312 Admin Full-Delete', () => {
  // tidak pakai beforeEach login — tiap test login berbeda role (admin vs hc)
});
```

> **Rule planner:** TIDAK pakai `beforeEach` karena tiap test pilih role berbeda. Login per-test (existing pattern `tests/e2e/assessment.spec.ts:24, 33`).

#### B. Login per-test pattern

Source: `tests/e2e/assessment.spec.ts:24-26, 33-36, 446-449`

```ts
test('1.1 - HC can navigate to Create Assessment page', async ({ page }) => {
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    // ...
});

test('3.1 - HC can open Edit page for assessment', async ({ page }) => {
    test.skip(!assessmentTitle, 'Assessment not created yet');
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    // ...
});
```

**Apply per test 12.x:** call `await login(page, 'admin')` atau `await login(page, 'hc')` di awal sesuai matrix.

#### C. Scaffold-with-skip pattern (untuk seed dependency)

Source: `tests/e2e/assessment.spec.ts:282-292, 311-321`

```ts
test('9.1 - SC #2: Tombol Selesaikan Penilaian disabled + tooltip wrapper saat session Status=Completed', async ({ page }) => {
    // PLACEHOLDER target session ID — Wave 1 fill dengan actual seeded ID
    const completedSessionTitle = 'Phase 310 Completed Fixture';

    await page.goto('/Admin/ManageAssessment');
    const groupRow = page.locator('tr', { hasText: completedSessionTitle }).first();

    // Test akan SKIP otomatis kalau seed belum ada (RED state pre-fixture)
    if (await groupRow.count() === 0) {
      test.skip(true, 'Seed session "Phase 310 Completed Fixture" not found — Wave 1 manual seed required');
    }

    // ... assertion ...
});
```

> **Rule planner:** test 12.x butuh seed assessment dengan Status spesifik (Open/Completed) + responseCount 0/>0. Pakai `test.skip(...)` pattern untuk graceful pre-seed degradation. Wave 0 scaffold + Wave 1 fill sesudah seed siap.

#### D. AJAX JSON endpoint test pattern (untuk test 12.0 `GetDeleteImpact`)

Source: tidak ada in-file analog langsung. Pakai `page.request.get(...)` Playwright API atau `page.evaluate(() => fetch(...))`. Excerpt minimal:

```ts
test('12.0 - GetDeleteImpact returns expected JSON shape for type=single', async ({ page }) => {
  await login(page, 'admin');
  // contoh: assume seeded session id 100
  const response = await page.request.get('/Admin/GetDeleteImpact?type=single&id=100');
  expect(response.status()).toBe(200);
  const data = await response.json();
  expect(data).toHaveProperty('status');
  expect(data).toHaveProperty('responseCount');
  expect(data).toHaveProperty('certCount');
  expect(data).toHaveProperty('packageCount');
  expect(data).toHaveProperty('attemptCount');
  expect(data).toHaveProperty('sessionCount');
});
```

#### E. Dropdown delete-flow assertion pattern

Source: `tests/e2e/assessment.spec.ts:461-479`

```ts
test('3.2 - HC can delete assessment group', async ({ page }) => {
    test.skip(!assessmentTitle, 'Assessment not created yet');
    await login(page, 'hc');
    await page.goto('/Admin/ManageAssessment');
    await searchAssessment(page, assessmentTitle);

    // Find delete button in dropdown
    const dropdown = page.locator('.dropdown-toggle').first();
    if (await dropdown.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await dropdown.click();
      autoConfirm(page);
      const deleteBtn = page.locator('button[formaction*="DeleteAssessmentGroup"], a[href*="DeleteAssessment"]').first();
      if (await deleteBtn.isVisible({ timeout: 2_000 }).catch(() => false)) {
        await deleteBtn.click();
        await page.waitForURL('**/ManageAssessment**', { timeout: 10_000 });
      }
    }
});
```

> **Rule planner:** Phase 312 ganti pakai modal-flow:
> 1. Click dropdown trigger → click `.dropdown-item.text-danger`
> 2. Wait for `#deleteAssessmentModal.show` → wait for `#dam-content` visible (post-AJAX)
> 3. Click `#dam-next-btn` → wait `#dam-step-2` visible
> 4. Click submit "Hapus Permanen" → wait `**/ManageAssessment**` redirect
> 5. Assert `.alert-success` (sukses) atau `.alert-danger` (HC blocked)
> Untuk skenario HC HIDE: assert `.dropdown-item.text-danger` count == 0 (button tidak di-render).

---

### 4. `.planning/phases/312-admin-full-delete-assessment-room/312-UAT.md` (created)

**Role:** Manual UAT checklist Bahasa Indonesia (6 skenario per CONTEXT D-04)
**Template precedent:** `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md` (paling clean step-by-step BI) + `.planning/phases/310-essay-finalize-idempotency/310-UAT.md` (path A coverage matrix style)

#### A. Frontmatter + header (analog 308)

Source: `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md:1-10`

```markdown
# Phase 308 — Manual UAT Script

**Phase:** 308-prepost-wizard-validation-fix
**REQ:** WIZ-04 (PrePost Wizard Validation Fix — Status field tidak reset wizard ke Step 1)
**Created:** 2026-04-29
**Mandatory before:** `/gsd-verify-work` Phase 308

> Manual UAT cover 5 success criteria Phase 308 dengan test matrix 4 kombinasi (D-10): Standard saja, Switch S→PP→S, PP saja, Switch PP→S→PP. ...
```

**Apply for 312:**

```markdown
# Phase 312 — Manual UAT Script

**Phase:** 312-admin-full-delete-assessment-room
**REQ:** DEL-01 (Admin Full-Delete Assessment Room — role tier guard di body method)
**Created:** 2026-05-07
**Mandatory before:** `/gsd-verify-work` Phase 312

> Manual UAT cover 6 skenario smoke (5 dari SC #6 + 1 extra D-04 untuk Pre-Post path) ...
```

#### B. Pre-conditions block (analog 308)

Source: `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md:12-19`

```markdown
## Pre-conditions

- App di-build & running (development atau staging)
- Login sebagai HC atau Admin (kredensial lihat `tests/helpers/accounts.ts`)
- Browser modern: Chrome / Edge / Firefox versi terbaru
- DevTools terbuka (Console tab — capture JS errors selama testing)
- Database seed minimal 5+ user untuk pilih peserta + 1+ Package soal Standard + 1+ Package soal PrePost (Pre-Test + Post-Test)
```

**Apply for 312:** sesuaikan seed requirement — butuh session dengan kombinasi Status (Open/Completed) × responseCount (0/>0) × type (Standard/PrePost). Min: 4 session/grup matrix.

#### C. Step format (analog 308 step structure)

Source: `.planning/phases/308-prepost-wizard-validation-fix/308-UAT.md:22-46`

```markdown
## Step 1 — Standard saja: pilih tipe Standard, fill semua field termasuk Status, submit, verify success (D-10 #1, success criteria #5 regression guard)

**Action:**
1. Navigasi ke `/Admin/CreateAssessment` (Create mode, fresh form)
2. Inspeksi Step 1: type selector ...
3. ...

**Expect:**
- Status field di Step 1 visible saat Standard mode (default) — wrapper TIDAK d-none
- Submit sukses → redirect ke `/Admin/ManageAssessment` ...
- **TIDAK** ada error ...
```

**Apply for 312:** 6 step matrix:
1. Step 1 — Admin + Open + 0 response → DELETE OK
2. Step 2 — Admin + Completed + ada response → DELETE OK (Admin override)
3. Step 3 — HC + Open + 0 response → DELETE OK
4. Step 4 — HC + Completed → button HIDE (UI assertion D-01)
5. Step 5 — HC + Open + ada response → button TAMPIL (UI imperfect Q1 opsi B), modal terbuka, submit reject + AuditLog blocked + flash error
6. Step 6 (extra D-04) — HC + PrePost + Completed → button HIDE atau backend reject (verify both UI + AuditLog)

Tiap step format: **Pre-condition** seed yang perlu + **Action** klik flow + **Expect** assertion DOM + DB AuditLog row + flash banner copy match UI-SPEC line 153-161.

#### D. Path A coverage matrix style (alternative — analog 310)

Source: `.planning/phases/310-essay-finalize-idempotency/310-UAT.md:10-25`

```markdown
| Step | SC | Method | Result |
|------|----|--------|--------|
| 1 | SC #1 alreadyFinalized (D-03) | Klik tombol enabled session 118 → alert-info biru | **PASS** |
| 2 | SC #2 visual disabled + tooltip (D-02) | SQL temp inject `NomorSertifikat='TEST-CERT-310-UAT'` Id=118 → reload → snapshot → revert | **PASS** |
| ...
```

> **Rule planner:** boleh kombinasi 308-style step description + 310-style coverage matrix di header. Pilih sesuai phase complexity. Phase 312 cocok hybrid: matrix di header (6 row, status tracker) + 6 step detail di bawah.

---

## Shared Patterns

### Authentication / Role Guard

**Source ASP.NET:** `[Authorize(Roles = "Admin, HC")]` attribute — preserved verbatim
**Source Razor:** `@if (User.IsInRole("Admin")) { ... }` — `Views/Admin/CoachWorkload.cshtml:37, 63, 251, 275`
**Source Controller body:** `if (User.IsInRole("Admin")) return null;` — pattern baru di `EnsureCanDeleteAsync`

**Apply to:** semua 3 method delete + view conditional render + helper method

### Error Handling

**Source:** `Controllers/AssessmentAdminController.cs:2114-2120` (catch-all + logger.LogError + TempData["Error"] + RedirectToAction)
**Apply to:** semua 3 method delete + new GetDeleteImpact (untuk JSON return BadRequest/NotFound)

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Error deleting assessment {Id}", id);
    TempData["Error"] = "Gagal menghapus assessment. Silakan coba lagi.";
    return RedirectToAction("ManageAssessment");
}
```

### AuditLog Service Call

**Source:** `Services/AuditLogService.cs:21-42` + `Controllers/AssessmentAdminController.cs:2094-2109`
**Signature:** `LogAsync(actorUserId, actorName, actionType, description, targetId, targetType)`
**Apply to:** sukses path 3 method (description extend Status+ResponseCount) + helper `EnsureCanDeleteAsync` blocked path (action suffix `Blocked`)

> **Convention:** wrap `_auditLog.LogAsync(...)` dalam try/catch + `logger.LogWarning` swallow — audit failure jangan break user flow.

### Cascade Delete Pattern (PRESERVED — DON'T TOUCH)

**Source:** `Controllers/AssessmentAdminController.cs:2048-2089` (DeleteAssessment), `:2158-2196` (DeleteAssessmentGroup), `:2254-2287` (DeletePrePostGroup)
**Order:** PackageUserResponses → AssessmentAttemptHistory → AssessmentPackages (with Questions+Options) → AssessmentSessions

> **Rule planner:** SC #5 mensyaratkan cascade utuh. Guard sisip **sebelum** cascade block, jangan ubah cascade logic itu sendiri.

### TempData Flash Banner

**Source:** `Views/Admin/CoachWorkload.cshtml:46-60` (rendered di parent layout / ManageAssessment shell)
**Apply to:** controller set `TempData["Success"]` / `TempData["Error"]` per UI-SPEC line 153-161 — view layer SUDAH render banner di shell.

### AntiForgeryToken

**Source:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:254, 267`
**Apply to:** modal `<form id="dam-submit-form">` di view modification — WAJIB karena 3 controller method pakai `[ValidateAntiForgeryToken]`.

### Bootstrap 5 Modal AJAX Pattern

**Source:** `Views/Admin/AssessmentMonitoringDetail.cshtml:500-537, 937-962`
**Apply to:** new `#deleteAssessmentModal` di `_AssessmentGroupsTab.cshtml` (markup + JS handler)

---

## No Analog Found

| File | Role | Reason |
|------|------|--------|
| (none) | — | Semua 4 file punya analog langsung in-repo. Phase 312 = pure pattern composition, zero greenfield. |

**Catatan minor (no precedent):**
- `{Action}Blocked` action type naming convention: TIDAK ada precedent existing (RESEARCH §Sources Secondary). Konvensi baru reasonable — planner finalisasi. Tidak block development.
- 2-step modal (panel swap step 1 → step 2): analog `akhiriSemuaModal` adalah single-step. Phase 312 extend dengan panel swap — pattern incremental, JS skeleton di RESEARCH.md Example 4 sudah lengkap.

---

## Metadata

**Analog search scope:**
- `Controllers/` (controller actions: cascade, AJAX endpoint, AuditLog usage)
- `Views/Admin/` (Razor partials, modal markup, role conditional render)
- `Services/AuditLogService.cs` (signature confirmation)
- `tests/e2e/assessment.spec.ts` + `tests/helpers/` (Playwright FLOW pattern, login helper, accounts fixture)
- `.planning/phases/30*-UAT.md` + `31*-UAT.md` (UAT template precedent — 6 file scanned, picked 308 + 310)

**Files scanned:** 12 (Controllers/AssessmentAdminController.cs, Services/AuditLogService.cs, Views/Admin/Shared/_AssessmentGroupsTab.cshtml, Views/Admin/AssessmentMonitoringDetail.cshtml, Views/Admin/CoachWorkload.cshtml, tests/e2e/assessment.spec.ts, tests/helpers/auth.ts, tests/helpers/accounts.ts, .planning/phases/308-prepost-wizard-validation-fix/308-UAT.md, .planning/phases/310-essay-finalize-idempotency/310-UAT.md, .planning/phases/311-manageassessment-performance/311-UAT.md, plus context CONTEXT/RESEARCH/UI-SPEC)

**Pattern extraction date:** 2026-05-07

---

*Phase: 312-admin-full-delete-assessment-room*
*PATTERNS drafted: 2026-05-07*
*Stack: ASP.NET Core MVC 8 + Razor + Bootstrap 5 + bootstrap-icons + HTMX 2.0.x*
*Bahasa: Bahasa Indonesia (per CLAUDE.md)*
