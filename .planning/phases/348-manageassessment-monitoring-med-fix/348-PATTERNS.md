# Phase 348: manageassessment-monitoring-med-fix - Pattern Map

**Mapped:** 2026-06-04
**Files analyzed:** 8 file MODIFIED (0 new, 0 migration, 0 schema)
**Analogs found:** 13 / 13 (semua MAM punya analog in-codebase — ini audit-driven bug-fix, REUSE pola, bukan invent)

> **Catatan pemakaian (untuk planner):** Phase ini MODIFIES file existing. Semua excerpt di bawah sudah di-verify anchor line-nya 2026-06-04 (line bisa bergeser saat plan dijalankan — re-Grep bila perlu). Bahasa prosa Indonesia; kode/path/identifier English. Konstanta WAJIB: `AssessmentConstants.AssessmentStatus.PendingGrading` (= `"Menunggu Penilaian"`, verified `Models/AssessmentConstants.cs:18`) — JANGAN literal string (D-C).

---

## File Classification

| File MODIFIED | Role | Data Flow | MAM | Closest Analog | Match |
|---------------|------|-----------|-----|----------------|-------|
| `Controllers/AssessmentAdminController.cs` | controller | request-response / CRUD | 01,03,04,06,07,10,11,12 | self (sibling methods) | exact |
| `Controllers/CMPController.cs` | controller | event-driven (SignalR push) | 05 | self L1841-1846 + `WorkerDataService.cs:52-57` | exact |
| `Services/WorkerDataService.cs` | service | CRUD / transform | 07,09 | `CMPController.cs:770-787` | exact |
| `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` | view (partial) | request-response | 02,07,10 | `AssessmentMonitoring.cshtml:337-388` + self L348-427 | exact |
| `Views/Admin/Shared/_TrainingRecordsTab.cshtml` | view (partial) | request-response (HTMX) | 06,08,09 | self filter-form `#filterFormTraining` + pagination HTMX | exact |
| `Views/Admin/AssessmentMonitoring.cshtml` | view | request-response | 11,12 | `AssessmentAdminController.cs:312-313` (categories query) | exact |
| `Views/Admin/AssessmentMonitoringDetail.cshtml` | view | event-driven (SignalR handler) | 05,13 | self L1336-1370 + L739-743 | exact |

> Endpoint MAM-02 hilir (`ExportAssessmentResults` L4120-an, `BulkExportPdf` L4503-an) = controller, CRUD/file-I/O — disentuh untuk "both-half export". `AssessmentMonitoringDetail` (L3163) endpoint **TIDAK perlu diubah** (sudah terima `assessmentType`, lihat MAM-02).

---

## Pattern Assignments — 5 PRIORITAS (REUSE pola, extract verbatim)

### MAM-02 — Link Pre/Post per-half di Tab1 (controller, view)

**Target:** `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:261-285` (dropdown Aksi row Pre-Post — link Monitoring L261-263, Export Excel L278-280, Bulk PDF L283-285 semua kirim `scheduleDate = group.Schedule.Date` = PreTest rep → silently miss PostTest).

**Analog PROVEN:** `Views/Admin/AssessmentMonitoring.cshtml:337-388` (preDetailUrl/postDetailUrl split — sudah jalan di Monitoring list).

**Code excerpt analog** (`AssessmentMonitoring.cshtml:337-388`):
```csharp
@if (group.PreSubRow != null)
{
    var preDetailUrl = Url.Action("AssessmentMonitoringDetail", "AssessmentAdmin", new {
        title = group.Title,
        category = group.Category,
        scheduleDate = group.PreSubRow.Schedule.Date.ToString("yyyy-MM-dd"),
        assessmentType = "PreTest"
    }) ?? "#";
    <tr class="bg-light">
        <td><span class="badge bg-info">Pre-Test</span> @group.PreSubRow.Schedule.ToString("dd MMM yyyy HH:mm")</td>
        ...
        <td><a href="@preDetailUrl" class="btn btn-sm btn-outline-info"><i class="bi bi-eye me-1"></i>Detail Pre</a></td>
    </tr>
}
@if (group.PostSubRow != null)
{
    var postDetailUrl = Url.Action("AssessmentMonitoringDetail", "AssessmentAdmin", new {
        title = group.Title,
        category = group.Category,
        scheduleDate = group.PostSubRow.Schedule.Date.ToString("yyyy-MM-dd"),
        assessmentType = "PostTest"
    }) ?? "#";
    <tr class="bg-light">
        <td><span class="badge bg-secondary">Post-Test</span> ...</td>
        <td><a href="@postDetailUrl" class="btn btn-sm btn-outline-secondary"><i class="bi bi-eye me-1"></i>Detail Post</a></td>
    </tr>
}
```

**Code excerpt target (BUG)** (`_AssessmentGroupsTab.cshtml:261-285`):
```csharp
<li>
    <a class="dropdown-item" href="@Url.Action("AssessmentMonitoringDetail", "AssessmentAdmin", new { title = (string)group.Title, category = (string)group.Category, scheduleDate = ((DateTime)group.Schedule).Date.ToString("yyyy-MM-dd") })">
        <i class="bi bi-binoculars me-2"></i>Monitoring
    </a>
</li>
...
<a class="dropdown-item" href="@Url.Action("ExportAssessmentResults", ... new { ..., scheduleDate = ((DateTime)group.Schedule).Date.ToString("yyyy-MM-dd") })">Export Excel</a>
...
<a class="dropdown-item" href="@Url.Action("BulkExportPdf", ... new { ..., scheduleDate = ((DateTime)group.Schedule).Date.ToString("yyyy-MM-dd") })">Bulk Export PDF (ZIP)</a>
```

**Endpoint sudah siap (NO CHANGE):** `AssessmentAdminController.cs:3163-3172` — `AssessmentMonitoringDetail(string title, string category, DateTime scheduleDate, string? assessmentType = null)` SUDAH terima + filter `assessmentType` (`if (!string.IsNullOrEmpty(assessmentType)) query = query.Where(a => a.AssessmentType == assessmentType);`). Jadi Monitoring-link MAM-02 = murni view-side (tambah link per-half untuk `group.IsPrePostGroup`).

**Catatan adaptasi:**
- `_AssessmentGroupsTab.cshtml` pakai `dynamic` (`(string)group.Title`, `(DateTime)group.Schedule`) — beda dari `AssessmentMonitoring.cshtml` yang strongly-typed `MonitoringGroupViewModel`. Tapi `_AssessmentGroupsTab` dynamic row TIDAK punya `PreSubRow/PostSubRow` (cek shape di `AssessmentAdminController.cs:158-170` — hanya `Schedule`, `LinkedGroupId`, `AllIds`). **Planner putuskan:** untuk Monitoring-link minimal = kirim 2 link (PreTest pakai `group.Schedule`, PostTest perlu schedule PostTest — yang TIDAK ada di dynamic shape Tab1). **Opsi robust:** route-by-`LinkedGroupId` (param baru), atau expose Post schedule ke dynamic shape. Prinsip inti D-01: **JANGAN single-date filter**.
- Export Excel + Bulk PDF: D-01 mandat ekspor **KEDUA half** (LinkedGroupId-aware). Endpoint `ExportAssessmentResults`/`BulkExportPdf` saat ini filter `Schedule.Date` → harus diubah jadi: bila row Pre-Post, query by `LinkedGroupId` (include Pre + Post), bukan single date. Cek anchor endpoint L4120-an / L4503-an saat plan.
- MAM-02 exact button layout (hindari clutter 6 tombol) = Claude's discretion planner (CONTEXT D-01 note). Koord MAP-17/349.

---

### MAM-07 — Pagination Tab2 ASLI (service + controller + view)

**Target:**
- `Services/WorkerDataService.cs:242` — `GetWorkersInSection(...)` signature TANPA `page`/`pageSize`; build `workerList` lalu apply filter (category L373, subCategory L384, statusFilter L393, search L402) → **return full list TANPA Skip/Take**.
- `Controllers/AssessmentAdminController.cs:245-263,275` — `ManageAssessmentTab_Training` terima `page`/`pageSize` tapi cuma di-LOG (`page={Page}` L275), TIDAK dipakai untuk paginate.
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` — TIDAK ada markup pagination.

**Analog GOLD #1** (`CMPController.cs:770-787` — paginate `workerList` dari `GetWorkersInSection`, EXACT data type WorkerTrainingStatus):
```csharp
var workerList = await _workerDataService.GetWorkersInSection(
    sectionFilter, unit, category, search, statusFilter, from, to, subCategory, searchScope);

// Phase 337 CMP-26 (D-02): pageSize whitelist (20/50/100), default 20 untuk invalid
var pageSizeValidated = (pageSize == 20 || pageSize == 50 || pageSize == 100) ? pageSize : 20;
var paging = HcPortal.Helpers.PaginationHelper.Calculate(workerList.Count, page, pageSizeValidated);
var pagedWorkerList = workerList.Skip(paging.Skip).Take(paging.Take).ToList();

Response.Headers.Append("X-Pagination", System.Text.Json.JsonSerializer.Serialize(new {
    paging.CurrentPage, paging.TotalPages, paging.TotalCount, PageSize = paging.Take
}));

return PartialView("_RecordsTeamBody", pagedWorkerList);
```

**Analog GOLD #2** (`AssessmentAdminController.cs:210-218` — Tab1 sibling, set ViewBag persis yang dibaca markup pagination Tab1):
```csharp
var paging = PaginationHelper.Calculate(grouped.Count, page, pageSize);

ViewBag.ManagementData = grouped.Skip(paging.Skip).Take(paging.Take).ToList();
ViewBag.CurrentPage = paging.CurrentPage;
ViewBag.TotalPages = paging.TotalPages;
ViewBag.TotalCount = paging.TotalCount;
```

**Helper** (`Helpers/PaginationHelper.cs:3,7-12`):
```csharp
public record PaginationResult(int CurrentPage, int TotalPages, int TotalCount, int Skip, int Take);
public static PaginationResult Calculate(int totalCount, int page, int pageSize) {
    var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    var currentPage = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
    var skip = (currentPage - 1) * pageSize;
    return new PaginationResult(currentPage, totalPages, totalCount, skip, pageSize);
}
```

**Analog markup pagination HTMX** (`_AssessmentGroupsTab.cshtml:348-427` — tiru untuk Tab2, ganti `#filterFormAssessment`→`#filterFormTraining`, action→`ManageAssessmentTab_Training`):
```html
@if (totalPages > 1)
{
    <nav aria-label="Assessment pagination" class="mt-4">
        <div class="d-flex justify-content-between align-items-center">
            <div class="text-muted small">
                Menampilkan @((currentPage - 1) * 20 + 1) - @(Math.Min(currentPage * 20, totalCount)) dari @totalCount grup
            </div>
            <ul class="pagination pagination-sm mb-0">
                @{ var paginateAction = Url.Action("ManageAssessmentTab_Assessment", "AssessmentAdmin"); }
                <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                    <button class="page-link" type="button"
                            hx-get="@paginateAction"
                            hx-vals='{"page":"1"}'
                            hx-include="#filterFormAssessment"
                            hx-target="closest .htmx-tab-wrapper"
                            hx-swap="innerHTML"
                            @(currentPage == 1 ? "disabled" : "")>
                        <i class="bi bi-chevron-double-left"></i>
                    </button>
                </li>
                @* ... Previous / Page Numbers (startPage..endPage) / Next / Last sama pola ... *@
            </ul>
        </div>
    </nav>
}
```

**ViewBag header analog Tab1** (`_AssessmentGroupsTab.cshtml:1-9`):
```csharp
var currentPage = (int)(ViewBag.CurrentPage ?? 1);
var totalPages = (int)(ViewBag.TotalPages ?? 1);
var totalCount = (int)(ViewBag.TotalCount ?? 0);
```

**Catatan adaptasi (kritis):**
- **Tempat Skip/Take:** D-02 spec bilang "tambah Skip/Take di `GetWorkersInSection`". Tapi analog `CMPController.cs:776` membuktikan **paginate di CALLER (controller action) lebih aman** — `GetWorkersInSection` tetap return full list (dipakai banyak caller: CMP, history, export). **Rekomendasi planner:** paginate di `ManageAssessmentTab_Training` (ala CMP L776), JANGAN ubah signature `GetWorkersInSection` (hindari ripple ke caller lain). Ini honor "pagination ASLI" tanpa breaking.
- Tab2 wrapper target = `closest .htmx-tab-wrapper` (sama Tab1). Filter form Tab2 = `#filterFormTraining` (verified `_TrainingRecordsTab.cshtml:30`).
- **Koord MAM-06:** pagination markup hanya render bila `!isInitialState && workers.Count > 0`. Pastikan pagination TIDAK muncul di empty-state.
- Magic number `20` di markup Tab1 = LOW MAP-21/349 (jangan fold ke 348; copy as-is dulu).

---

### MAM-08 — Delete Training/ManualAsm re-swap HTMX (view + handler)

**Target:** `Views/Admin/Shared/_TrainingRecordsTab.cshtml:327-334` (DeleteTraining) + `:342-349` (DeleteManualAssessment) — keduanya `<form method="post" action="...">` full-page POST.

**Code excerpt target (BUG)** (`_TrainingRecordsTab.cshtml:327-334`):
```html
<form method="post" action="@Url.Action("DeleteTraining", "TrainingAdmin")" class="d-inline">
    @Html.AntiForgeryToken()
    <input type="hidden" name="id" value="@row.Id" />
    <button type="submit" class="btn btn-sm btn-outline-danger" title="Hapus"
            onclick="return confirm('Hapus training record ini?')">
        <i class="bi bi-trash"></i>
    </button>
</form>
```

**Analog HTMX re-swap + hx-include** (pola filter/pagination existing di tab yang sama, `_TrainingRecordsTab.cshtml:147-153` Reset Filter button + `_AssessmentGroupsTab.cshtml:362-368` pagination button):
```html
<!-- Pola hx-get re-swap + hx-include filter form (dari Reset Filter L147-153) -->
<button type="button" class="btn btn-secondary" title="Reset Filter"
        hx-get="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
        hx-target="closest .htmx-tab-wrapper"
        hx-swap="innerHTML"
        onclick="...">
    <i class="bi bi-arrow-counterclockwise"></i>
</button>
```
```html
<!-- Pola hx-get + hx-include="#filterFormAssessment" (dari pagination Tab1 L362-368) -->
<button class="page-link" type="button"
        hx-get="@paginateAction"
        hx-vals='{"page":"1"}'
        hx-include="#filterFormAssessment"
        hx-target="closest .htmx-tab-wrapper"
        hx-swap="innerHTML">...</button>
```

**Catatan adaptasi (kritis):**
- Konversi `<form method=post>` → `<button hx-post="@Url.Action("DeleteTraining","TrainingAdmin")" hx-include="#filterFormTraining" hx-target="closest .htmx-tab-wrapper" hx-swap="innerHTML" hx-confirm="Hapus training record ini?" ...>` + sertakan `id` (via `hx-vals` atau hidden input dalam `hx-include` scope). Antiforgery: HTMX butuh token — cek apakah `hx-headers` / global antiforgery sudah ke-set (lihat `@Html.AntiForgeryToken()` existing + pola `data-session-id` JS fetch di `AssessmentMonitoring.cshtml:161`).
- **Handler `TrainingAdminController.cs` DeleteTraining (~L586/619) + DeleteManualAssessment (~L985/1016):** saat ini `RedirectToAction(new { tab="training" })`. Untuk re-swap, handler harus **return partial** (`ManageAssessmentTab_Training`) ATAU view tetap re-swap dengan memanggil ulang `ManageAssessmentTab_Training` lewat `hx-post` ke action delete yang lalu `return PartialView`. **Planner putuskan** mekanik: (a) delete action di TrainingAdmin return partial Tab2, atau (b) delete via hx-post lalu `hx-trigger`/OOB swap re-fetch Tab2. Verifikasi anchor handler L586/619/985/1016 saat plan (line dari spec, belum di-Read di pass ini).
- **Koord MAM-06 (WAJIB):** pasca-delete re-swap memanggil `ManageAssessmentTab_Training` dengan `#filterFormTraining` yang punya `isFiltered=true` (hidden, `_TrainingRecordsTab.cshtml:32`) + filter values aktif → `isInitialState` HARUS tetap `false` (jangan balik ke empty-state "Pilih filter"). Ini auto-terpenuhi bila MAM-06 derive `isInitialState` dari `isFiltered`/filter presence (lihat MAM-06).
- Fits arsitektur HTMX Phase 311/322 (ManageAssessment = HTMX host 3 tab).

---

### MAM-03 + MAM-11 — Parity assignment + data-driven dropdown (controller + view)

#### MAM-03 (controller)
**Target:** `AssessmentAdminController.cs:2749-2795` (prePostGroups `MonitoringGroupViewModel`) — TIDAK assign `MenungguPenilaianCount` (default 0).

**Analog parity** (`AssessmentAdminController.cs:2825` — standardGroups, sibling di method yang sama):
```csharp
return new MonitoringGroupViewModel
{
    ...
    AccessToken = rep.AccessToken ?? "",
    MenungguPenilaianCount = g.Count(a => a.IsMenungguPenilaian)   // L2825 — INI yang hilang di prePostGroups
};
```

**Catatan adaptasi:** Tambah `MenungguPenilaianCount = postSubs.Count(a => a.IsMenungguPenilaian)` ke object prePostGroups L2749-2795 (grading terjadi di Post — gunakan `postSubs`, bukan `g`). `IsMenungguPenilaian` = computed property AssessmentSession (verified dipakai L2825). Badge konsumen = `AssessmentMonitoring.cshtml:276-280` (`@if (menungguCount > 0)`), yang membaca `group.MenungguPenilaianCount` (L229). Tanpa fix, badge "X belum dinilai" tak pernah muncul untuk SELURUH tipe Pre-Post.

#### MAM-11 (controller + view)
**Target:** `Views/Admin/AssessmentMonitoring.cshtml:125-148` — dropdown Kategori hardcoded array (termasuk "Proton" phantom L134).

**Code excerpt target (BUG)** (`AssessmentMonitoring.cshtml:126-136`):
```csharp
var categories = new[]
{
    ("", "Semua Kategori"),
    ("OJT", "OJT"), ("IHT", "IHT"),
    ("Training Licencor", "Training Licencor"), ("OTS", "OTS"),
    ("Mandatory HSSE Training", "Mandatory HSSE Training"),
    ("Proton", "Proton"),                 // ← phantom: match 0, canonical seed = "Assessment Proton"
    ("Assessment Proton", "Assessment Proton")
};
```

**Analog data-driven query** (`AssessmentAdminController.cs:312-313` — `SetCategoriesViewBag`, pola Include + OrderBy SortOrder):
```csharp
var parentCategories = await _context.AssessmentCategories
    .Include(c => c.Children.OrderBy(ch => ch.SortOrder).ThenBy(ch => ch.Name))
        .ThenInclude(ch => ch.Children.OrderBy(gc => gc.SortOrder).ThenBy(gc => gc.Name))
    ...
```

**Catatan adaptasi:** Spec CONTEXT MAM-11 minta `_context.AssessmentCategories.Where(c => c.IsActive).OrderBy(c => c.SortOrder)` via ViewBag, dipopulasi di action `AssessmentMonitoring` (`AssessmentAdminController.cs:2670-2673`). View loop `ViewBag.MonitoringCategories` (flat list cukup — Monitoring tak perlu hierarki seperti CreateAssessment). Buang "Proton" otomatis (data-driven = hanya kategori real). Pola `IsActive` + `SortOrder` standard di codebase (verified pattern `Include...OrderBy(SortOrder)` L312-313).

---

## Pattern Assignments — MAM-04 + MAM-05 (status derivation PendingGrading, isolasi shared)

> M1/M5 sentuh shared grading + SignalR → CONTEXT decisions §Claude's Discretion sarankan isolasi plan. Konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` WAJIB.

### MAM-04 — Detail status cek PendingGrading SEBELUM CompletedAt (controller)

**Target:** `AssessmentAdminController.cs:3229-3239` (status derivation `AssessmentMonitoringDetail`) — cek `a.CompletedAt != null` DULU (L3230) → essay-pending salah-map "Completed".

**Code excerpt target (BUG)** (`AssessmentAdminController.cs:3229-3239`):
```csharp
string userStatus;
if (a.CompletedAt != null)            // ← essay-pending JUGA punya CompletedAt terisi (GradingService L203) → salah "Completed"
    userStatus = "Completed";
else if (a.Status == "Cancelled")
    userStatus = "Dibatalkan";
else if (a.Status == "Abandoned")
    userStatus = "Abandoned";
else if (a.StartedAt != null)
    userStatus = "InProgress";
else
    userStatus = "Not started";
```

**Analog ordering (PendingGrading-first)** (`Services/WorkerDataService.cs:52-57` — switch derivation, null/pending diutamakan):
```csharp
Status = a.IsPassed switch
{
    true => "Passed",
    false => "Failed",
    null => AssessmentConstants.AssessmentStatus.PendingGrading   // null/pending = case eksplisit
},
```

**Analog konstanta-guard** (`AssessmentAdminController.cs:3418` + `WorkerDataService.cs:33,136`):
```csharp
if (session.Status != AssessmentConstants.AssessmentStatus.PendingGrading)   // L3418
// dan pola: (a.Status == "Completed" || a.Status == AssessmentConstants.AssessmentStatus.PendingGrading)  // L4734 / WDS L33,136
```

**Catatan adaptasi:**
- Sisipkan cabang **PERTAMA**: `if (a.Status == AssessmentConstants.AssessmentStatus.PendingGrading) userStatus = "Menunggu Penilaian"; else if (a.CompletedAt != null) ...`. Konstanta, BUKAN literal.
- `CompletedCount` (`AssessmentAdminController.cs:3273` `sessionViewModels.Count(s => s.UserStatus == "Completed")`) auto-exclude ungraded essay setelah fix (karena userStatus "Menunggu Penilaian" ≠ "Completed"). Verifikasi `PassedCount`/`PendingCount`/`InProgressCount` (L3274-3280) tetap konsisten.
- View `AssessmentMonitoringDetail.cshtml` punya cabang "Menunggu Penilaian" (CONTEXT sebut L239/246) yang saat ini dead-code → fix ini meng-aktif-kan. Cek `statusLabel`/`statusClass` di view (sekitar L260-271) konsumsi `UserStatus`.

### MAM-05 — SignalR workerSubmitted jangan push "Completed" prematur (controller + view)

**Target controller:** `Controllers/CMPController.cs:1766-1772` (push `workerSubmitted` setelah `GradeAndCompleteAsync`).

**Code excerpt target (BUG)** (`CMPController.cs:1756-1772`):
```csharp
bool graded = await _gradingService.GradeAndCompleteAsync(assessment);
if (!graded) { /* race */ return RedirectToAction("Results", new { id }); }

// SignalR push: notify HC monitor group that worker submitted (package path)
{
    var submitBatchKey = $"{assessment.Title}|{assessment.Category}|{assessment.Schedule.Date:yyyy-MM-dd}";
    var result = finalPercentage >= assessment.PassPercentage ? "Pass" : "Fail";   // ← MC/MA-only, essay belum dinilai
    int totalQuestionsSubmit = shuffledIds.Count;
    await _hubContext.Clients.Group($"monitor-{submitBatchKey}").SendAsync("workerSubmitted",
        new { sessionId = id, workerName = user.FullName, score = finalPercentage, result, status = "Completed", totalQuestions = totalQuestionsSubmit });   // ← hardcode "Completed"
}
```

**PITFALL KRITIS (verified `GradingService.cs:189-204`):** flow essay pakai **`ExecuteUpdateAsync`** (bulk SQL) untuk set `Status = PendingGrading`:
```csharp
if (hasEssay)
{
    var essayRowsAffected = await _context.AssessmentSessions
        .Where(s => s.Id == session.Id && s.Status != Completed && s.Status != PendingGrading)
        .ExecuteUpdateAsync(s => s
            .SetProperty(r => r.Status, AssessmentConstants.AssessmentStatus.PendingGrading)
            .SetProperty(r => r.IsPassed, (bool?)null)
            .SetProperty(r => r.CompletedAt, DateTime.UtcNow)  ...);
    return true;   // ← return bool TRUE untuk graded DAN pending (caller tak bisa bedakan!)
}
```
→ `ExecuteUpdateAsync` **bypass change-tracker**, jadi entity `assessment` di CMPController **TIDAK ter-update** — `assessment.Status` tetap nilai lama. `GradeAndCompleteAsync` return `bool graded` yang `true` untuk KEDUA flow (essay-pending + completed). **Caller TIDAK bisa cek `assessment.Status` langsung untuk tahu apakah pending.**

**Solusi surface (planner putuskan):**
1. **Reload status** sesudah grading: `var freshStatus = await _context.AssessmentSessions.Where(s => s.Id == id).Select(s => s.Status).FirstAsync();` lalu branch `if (freshStatus == AssessmentConstants.AssessmentStatus.PendingGrading)`. (Pola reload mirip `CMPController.cs:1842` yang cek `assessment.Status == PendingGrading` SETELAH operasi.) — minimal-risk, no signature change.
2. **Ubah signature `GradeAndCompleteAsync`** return enum/tuple (`graded`, `isPending`) — lebih bersih tapi ripple ke caller lain (cek pemakai lain `GradeAndCompleteAsync`).
**Default CONTEXT = reuse `workerSubmitted` dgn status override** (D-05 Claude's discretion): bila pending → push `status = AssessmentConstants.AssessmentStatus.PendingGrading`, `result = "—"`.

**Target view handler:** `Views/Admin/AssessmentMonitoringDetail.cshtml:1336-1370`.

**Code excerpt target (BUG)** (`AssessmentMonitoringDetail.cshtml:1336-1362`):
```javascript
window.assessmentHub.on('workerSubmitted', function(data) {
    var tr = document.querySelector('tr[data-session-id="' + data.sessionId + '"]');
    ...
    if (tr) {
        var tds = tr.querySelectorAll('td');
        if (tds[2]) tds[2].innerHTML = '<span class="badge bg-success">Completed</span>';   // ← hardcode Completed/hijau
        if (tds[3]) tds[3].textContent = data.score !== null ? data.score + '%' : '—';
        if (tds[4]) {
            tds[4].textContent = data.result || '—';
            tds[4].className = 'result-cell ' + (data.result === 'Pass' ? 'text-success fw-semibold' : (data.result === 'Fail' ? 'text-danger fw-semibold' : 'text-muted'));
        }
    }
});
```

**Catatan adaptasi:** Handler harus baca `data.status` (bukan hardcode). Bila `data.status === 'Menunggu Penilaian'` → badge `bg-warning text-dark">Menunggu Penilaian`, result cell `—` muted. Analog badge pending = `AssessmentMonitoring.cshtml:278` (`<span class="badge bg-warning text-dark">Menunggu Penilaian</span>`). `result === 'Pass'/'Fail'` ternary L1361 sudah handle muted fallback untuk `—`. Pastikan `updateSummaryFromDOM()` (L1374) hitung pending tidak sebagai Completed (koord MAP-10/11/349 untuk summary card, tapi jangan over-reach di 348).

---

## Pattern Assignments — 8 MAM IN-PLACE EDIT (target + konteks, tanpa reuse pola besar)

### MAM-01 — RegenerateToken match by LinkedGroupId (controller)
**Target:** `AssessmentAdminController.cs:2616-2621`.
**Konteks existing (BUG):**
```csharp
var siblings = await _context.AssessmentSessions
    .Where(a => a.Title == assessment.Title
             && a.Category == assessment.Category
             && a.Schedule.Date == assessment.Schedule.Date)   // ← PostTest beda tanggal tak ikut
    .ToListAsync();
```
**Adaptasi:** Bila `assessment.LinkedGroupId != null` → `.Where(a => a.LinkedGroupId == assessment.LinkedGroupId)`; else fallback ke match Title+Category+Schedule.Date (standar). Audit msg L2636 (`{siblings.Count} sibling(s)`) ikut benar otomatis. PITFALL: validasi cuma enforce `PostSchedule > PreSchedule` — beda tanggal = normal.

### MAM-06 — isInitialState derive dari absennya filter (controller)
**Target:** `AssessmentAdminController.cs:251`.
**Konteks existing (BUG):** `bool isInitialState = false;` (hardcode → empty-state `_TrainingRecordsTab.cshtml:163-171` dead, full-roster load tiap first paint).
**Adaptasi:** `bool isInitialState = string.IsNullOrEmpty(isFiltered) && string.IsNullOrEmpty(section) && string.IsNullOrEmpty(unit) && string.IsNullOrEmpty(category) && string.IsNullOrEmpty(statusFilter) && string.IsNullOrEmpty(search);` — param `isFiltered` sudah di-post (hidden field `_TrainingRecordsTab.cshtml:32` `value="true"`). Skip query `GetWorkersInSection` bila initial (L260-263 sudah ada guard `if (isInitialState) workers = new List<>()`). **PITFALL (CONTEXT specifics):** cek `.planning/phases/*322*/322-UAT.md` — UAT lama PASS dgn full-roster-on-load (FP-rejected "Reset full-roster by design"). Verifikasi parity Phase 287 `fc161a18`. Koord MAM-08 (post-delete re-swap jaga `isInitialState=false`).

### MAM-09 — Relabel "Status Training" saja (view)
**Target:** `_TrainingRecordsTab.cshtml:108` (label `<label ...>Status</label>`).
**Konteks existing:** dropdown statusFilter "Sudah"/"Belum" (L115-124) dipetakan ke `CompletionPercentage` (training-only) di `WorkerDataService.cs:393-398`.
**Adaptasi (D-04 = opsi A relabel ONLY):** ubah label L108 `Status` → `Status Training`. **JANGAN** ubah `WorkerDataService.cs:360-398` logic (fold passed manual-assessment = deferred, CONTEXT deferred). Koord MAP-19/349 (badge "Status Training").

### MAM-10 — Badge Tab1 bind GroupStatus + tambah case Closed (view)
**Target:** `_AssessmentGroupsTab.cshtml:195-203` (switch) + `:221` (render `@group.Status`).
**Konteks existing (BUG):**
```csharp
var statusBadge = (string)group.Status switch   // ← rep single-session
{
    "Open" => "bg-success", "Completed" => "bg-secondary", "Upcoming" => "bg-info",
    "InProgress" => "bg-warning text-dark", "Abandoned" => "bg-dark", _ => "bg-secondary"
};
...
<td><span class="badge @statusBadge">@group.Status</span></td>   // L221
```
**Adaptasi:** bind `(string)group.GroupStatus` (sudah exposed di dynamic shape — verified `AssessmentAdminController.cs:167` prePost + `:188` standard). Drop arm `Completed/InProgress/Abandoned` (BUKAN nilai GroupStatus — GroupStatus hanya Open/Upcoming/Closed, lihat derivasi L177-180). **Tambah case `"Closed" => "bg-secondary"`**. Render `@group.GroupStatus` di L221. Analog badge GroupStatus yang BENAR sudah jalan di `AssessmentMonitoring.cshtml:220-226` (`group.GroupStatus switch { "Open"=>"bg-success", "Upcoming"=>"bg-info text-dark", "Closed"=>"bg-secondary", _=>"bg-secondary" }`).

### MAM-11 — (lihat MAM-03+11 section di atas, view+controller)

### MAM-12 — Tooltip Closed jujur, buang "lokasi" (view)
**Target:** `AssessmentMonitoring.cshtml:169` (tooltip badge Closed) + cross-ref `_AssessmentGroupsTab.cshtml:121` (tooltip kembar).
**Konteks existing (BUG):** `title="Closed group assessment (hidden default — pilih filter 'Closed' atau 'Semua Status' untuk lihat, atau search judul/lokasi spesifik)"` — tapi controller search cuma `Title.Contains` (verified `AssessmentAdminController.cs:2685-2688` `query.Where(a => a.Title.ToLower().Contains(lower))`, `Kota` tak di-search).
**Adaptasi (minimal jujur):** buang kata "lokasi" dari tooltip L169 (jadi "search judul spesifik"). Extend search ke Kota = out-of-scope MED (CONTEXT deferred → MAP-23/349). Catatan: tooltip kembar `_AssessmentGroupsTab.cshtml:121` juga punya "judul/lokasi" — planner putuskan apakah ikut diperbaiki (konsistensi) atau scope ke Monitoring saja.

### MAM-13 — Reshuffle selector scoped (view)
**Target:** `AssessmentMonitoringDetail.cshtml:739-743` (`reshuffleWorker` selector) + call-site `:354` (server) + `:868` (JS-render).
**Konteks existing (BUG):**
```javascript
function reshuffleWorker(sessionId) {
    var btn = document.querySelector('[data-session-id="' + sessionId + '"]');   // ← ambil <tr> L268, bukan <button> L353
    var originalHtml = btn.innerHTML;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';   // spinner ganti SELURUH row
```
`<tr data-session-id="@session.Id">` (L268) match duluan (DOM-first) > `<button data-session-id ...>` (L353).
**Adaptasi (CONTEXT MAM-13):** ubah signature `reshuffleWorker(this)` (lempar elemen button) ATAU pakai attribute distinct `[data-reshuffle-session-id]` di button. **Update 2 call-site:** server-render `onclick="reshuffleWorker(@session.Id)"` (L354) + JS-render string `onclick="reshuffleWorker(' + session.sessionId + ')"` (L868). Server reshuffle tetap jalan (sessionId dari arg) — ini glitch visual saja. Bila pakai `this`: `function reshuffleWorker(btn) { var sessionId = btn.dataset.sessionId; ... }` — hindari `document.querySelector` ambigu sepenuhnya.

---

## Shared Patterns (cross-cutting, apply ke beberapa MAM)

### Konstanta PendingGrading (apply: MAM-04, MAM-05)
**Source:** `Models/AssessmentConstants.cs:18`
```csharp
public const string PendingGrading = "Menunggu Penilaian"; // Phase 309 D-04
```
**Apply:** SEMUA derivation/branch status pending pakai `AssessmentConstants.AssessmentStatus.PendingGrading`, BUKAN literal `"Menunggu Penilaian"`. Helper terkait: `AssessmentConstants.IsAssessmentSubmitted(status)` (L78-79, = Completed OR PendingGrading) bila perlu cek "submitted". Verified usage existing: `AssessmentAdminController.cs:3418,3494,4623,4734`; `CMPController.cs:1842,1947,2279,2369,2393`; `WorkerDataService.cs:33,56,136`.

### HTMX re-swap + hx-include filter form (apply: MAM-07, MAM-08)
**Source:** `_AssessmentGroupsTab.cshtml:362-368` (pagination) + `_TrainingRecordsTab.cshtml:147-153` (Reset Filter)
```html
hx-get|hx-post="@Url.Action("ManageAssessmentTab_Training", "AssessmentAdmin")"
hx-include="#filterFormTraining"   <!-- preserve filter state -->
hx-target="closest .htmx-tab-wrapper"
hx-swap="innerHTML"
```
**Apply:** Tab2 = `#filterFormTraining` (verified L30) + action `ManageAssessmentTab_Training`. Tab1 = `#filterFormAssessment` (verified L13) + `ManageAssessmentTab_Assessment`. Wrapper selector universal = `closest .htmx-tab-wrapper`.

### PaginationHelper + Skip/Take (apply: MAM-07)
**Source:** `Helpers/PaginationHelper.cs:7` → `PaginationResult(CurrentPage, TotalPages, TotalCount, Skip, Take)`
**Apply:** Paginate di CALLER (controller action), bukan di service (lihat MAM-07 catatan — `CMPController.cs:776` precedent). Set `ViewBag.CurrentPage/TotalPages/TotalCount` (markup pagination baca ini, lihat `_AssessmentGroupsTab.cshtml:3-5`).

### dynamic vs strongly-typed view-model (apply: MAM-02, MAM-10)
`_AssessmentGroupsTab.cshtml` (Tab1) pakai `dynamic` (`ViewBag.ManagementData as IEnumerable<dynamic>`, cast `(string)group.X`). `AssessmentMonitoring*.cshtml` pakai `MonitoringGroupViewModel` strongly-typed. **Saat copy analog dari Monitoring ke Tab1, sesuaikan ke dynamic cast** + cek property tersedia di dynamic shape (`AssessmentAdminController.cs:158-192`: Tab1 dynamic punya `Title/Category/Schedule/Status/GroupStatus/IsPrePostGroup/LinkedGroupId/RepresentativeId/AllIds/IsTokenRequired` — TIDAK punya `PreSubRow/PostSubRow`).

---

## No Analog Found

Tidak ada. Semua 13 MAM punya analog in-codebase (audit-driven REUSE, bukan greenfield). Yang perlu "invent" minimal:
- MAM-02 "both-half export" endpoint logic (`ExportAssessmentResults`/`BulkExportPdf` LinkedGroupId query) — endpoint belum di-Read pass ini; planner verify anchor L4120-an/L4503-an + adaptasi query dari single-date ke LinkedGroupId-OR-date. Pola Pre-Post grouping ada di `AssessmentAdminController.cs:158-171` (GroupBy LinkedGroupId) sebagai referensi.

---

## Metadata

**Analog search scope:** `Controllers/` (AssessmentAdminController, CMPController), `Services/` (WorkerDataService, GradingService), `Views/Admin/` + `Views/Admin/Shared/`, `Models/AssessmentConstants.cs`, `Helpers/PaginationHelper.cs`.
**Files scanned (Read):** 9 (AssessmentAdminController, CMPController, WorkerDataService, GradingService, AssessmentConstants, PaginationHelper, _AssessmentGroupsTab, _TrainingRecordsTab, AssessmentMonitoring + AssessmentMonitoringDetail).
**Anchor verification:** semua anchor line dari CONTEXT/spec di-verify via Read/Grep 2026-06-04. Anchor handler delete (`TrainingAdminController.cs:586/619/985/1016`) + endpoint export (`L4120/L4503`) BELUM di-Read pass ini — planner WAJIB verify saat plan (line dari spec, bisa bergeser).
**Pattern extraction date:** 2026-06-04
