# Phase 384: Monitoring Essay Grading UI Refactor (Fase 2) - Pattern Map

**Mapped:** 2026-06-15
**Files analyzed:** 6 (1 MODIFY, 4 CREATE, 1 maybe-CREATE) + 1 e2e
**Analogs found:** 6 / 6 (all in-repo, exact or role-match)

> **Sifat phase:** STRUCTURAL REFACTOR. ~80% kerja = clone/pindah markup + logic yang sudah terbukti benar. Backend (controller POST + DB) TIDAK diubah, 0 migration. Tech stack = ASP.NET Core MVC + Razor (.cshtml), Bootstrap 5 + Bootstrap Icons, EF Core. **BUKAN React/Tailwind.**
>
> ⚠ **Catatan untuk planner:** `Controllers/AssessmentAdminController.cs` mungkin diedit session paralel (Fase 1 / Phase 383). Line number absolut bisa bergeser. **Cari analog by method name** (`AssessmentMonitoringDetail`, `SubmitEssayScore`, `FinalizeEssayGrading`), bukan line number. Snapshot line di dokumen ini benar per 2026-06-15.

---

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `Views/Admin/AssessmentMonitoringDetail.cshtml` (MODIFY `:381-481` + extract `:1472-1558`) | view (Razor) | request-response (SSR render) | tabel sesi existing SAME-FILE `:218-231` | exact (same file) |
| `Controllers/AssessmentAdminController.cs` → new `EssayGrading(...)` GET | controller | request-response (read-only, EF query) | `AssessmentMonitoringDetail` GET `@3273` + `EssayGradingMap` builder `@3413-3448` | exact (clone same file) |
| `Views/Admin/EssayGrading.cshtml` (NEW) | view (Razor) | request-response (SSR render) | essay CARD markup `AssessmentMonitoringDetail.cshtml:407-476` + breadcrumb/back `:55-67` + antiforgery `:484` | exact (clone) |
| `wwwroot/js/essay-grading.js` (NEW) | utility (client JS) | event-driven (AJAX fetch + DOM) | inline handlers `:1472-1558` + extracted js `edit-peserta-answers.js` | exact (extract) |
| `Models/AssessmentMonitoringViewModel.cs` → maybe `EssayGradingPageViewModel` (NEW class) | model (ViewModel) | transform (data container) | `MonitoringSessionViewModel` `:48-68` + `EssayGradingItemViewModel` `:73-86` | role-match (same file) |
| `tests/e2e/essay-grading-384.spec.ts` (NEW) + `tests/sql/essay-grading-384-seed.sql` | test (Playwright e2e) | event-driven (browser runtime) | `assessment-pending-grade.spec.ts` + `assessment.spec.ts` FLOW 9 `:265-394` | exact (snapshot/restore pattern) |

---

## Pattern Assignments

### 1. `Views/Admin/AssessmentMonitoringDetail.cshtml` — MODIFY (view, request-response)

**Analog:** SAME FILE — tabel sesi existing (`:218-231`) untuk struktur tabel; blok essay (`:381-481`) yang DIGANTI; handler inline (`:1472-1558`) yang DI-EXTRACT.

**Apa yang dikerjakan:**
1. GANTI blok essay `:381-481` (stacked cards per-worker) dengan tabel worker-list ringkas.
2. EXTRACT handler inline `:1472-1558` (`.btn-save-essay-score` + `.btn-finalize-grading`) ke `wwwroot/js/essay-grading.js` (lihat file #4). Setelah refactor, blok markup essay-grading hilang dari view ini → handler ini menjadi **dead** di sini.

**Guard yang DIPERTAHANKAN byte-for-byte (D-01/D-05)** — `:382-385`:
```cshtml
@{
    var essayGradingMap = ViewBag.EssayGradingMap as Dictionary<int, List<HcPortal.Models.EssayGradingItemViewModel>>;
}
@if (essayGradingMap != null && essayGradingMap.Any())
```

**Struktur tabel — MIRROR pola tabel sesi existing** (`:218-231`):
```cshtml
<div class="card-body p-0">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
                <tr>
                    <th>Nama</th><th>Progres</th><th>Status</th> ... <th>Aksi</th>
                </tr>
            </thead>
            <tbody>
```

**Badge 3-state derivation (D-04)** — REUSE finalize gate Phase 310 (existing `:451-453`, JANGAN ubah kriteria):
```cshtml
var isFinalized = session.Status == AssessmentConstants.AssessmentStatus.Completed
                  && !string.IsNullOrEmpty(session.NomorSertifikat);
```
- 🟡 `EssayPendingCount > 0` → `<span class="badge bg-warning text-dark">@s.EssayPendingCount belum dinilai</span>`
- 🔵 `EssayPendingCount == 0 && !isFinalized` → `<span class="badge bg-info">Siap difinalisasi</span>`
- 🟢 `isFinalized` → `<span class="badge bg-success">Selesai</span>`

> ⚠ **Kontras a11y (existing pola `:11`, `:246`, `:397`):** `bg-warning` WAJIB dipasangkan `text-dark`.

**Data source + urut (D-02/D-03)** — REUSE `Model.Sessions` (existing iterate `:389`):
```cshtml
@foreach (var s in Model.Sessions.Where(x => x.HasManualGrading).OrderBy(x => x.UserNIP))
```

**Tombol "Tinjau Essay" (D-06)** — navigasi GET (BUKAN AJAX), carry 4 nav param (mitigasi Pitfall 3/4). `ViewBag.AssessmentType` di-set action `:3285`; date format ikuti hidden field existing `:489 hScheduleDate`:
```cshtml
<a class="btn btn-primary btn-sm" href="@Url.Action("EssayGrading", new {
        sessionId = s.Id,
        title = Model.Title,
        category = Model.Category,
        scheduleDate = Model.Schedule.ToString("yyyy-MM-dd"),
        assessmentType = ViewBag.AssessmentType })">
    <i class="bi bi-pencil-square me-1"></i>Tinjau Essay
</a>
```

**Antiforgery form yang TETAP ADA (`:484`)** — tetap dibutuhkan surface lain di view ini:
```cshtml
<form id="antiforgeryForm" style="display:none">@Html.AntiForgeryToken()</form>
```

**⚠ Open Question #1 RESOLVED:** `grep` selesai. `.btn-finalize-grading` / `.btn-save-essay-score` / `.essay-grading-card` HANYA muncul di blok yang diganti (`:407-476`) dan handler (`:1472-1558`). **Setelah refactor TIDAK ada lagi di view ini** → handler finalize pindah TOTAL ke `essay-grading.js` + page baru dengan behavior in-place (D-09), tak perlu param dua-mode reload/inplace. Planner re-verify grep pasca-refactor (sampling per RESEARCH Pitfall 2).

---

### 2. `Controllers/AssessmentAdminController.cs` — new `EssayGrading(...)` GET action (controller, request-response)

**Analog:** SAME FILE — `AssessmentMonitoringDetail` GET (`@3271-3273` attributes + signature) + `EssayGradingMap` builder (`@3413-3448`) di-clone untuk SINGLE session.

**Routing context:** `[Route("Admin/[action]")]` (`:19`) + class `[Authorize]` di `AdminBaseController.cs:12` → action `EssayGrading` otomatis route ke `/Admin/EssayGrading`. Query string `?sessionId=...&title=...` konsisten convention.

**Attribute pattern (clone `:3271-3273`)** — authz SAMAKAN `AssessmentMonitoringDetail`/`SubmitEssayScore`/`FinalizeEssayGrading` (mitigasi IDOR — V4 Access Control):
```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> EssayGrading(
    int sessionId, string title, string category, DateTime scheduleDate, string? assessmentType = null)
```

**Guard + redirect aman (pola `:3287-3291`)** — V5 input validation:
```csharp
var session = await _context.AssessmentSessions
    .Include(a => a.User)
    .FirstOrDefaultAsync(a => a.Id == sessionId);
if (session == null || !session.HasManualGrading)
{
    TempData["Error"] = "Sesi penilaian essay tidak ditemukan.";
    return RedirectToAction("AssessmentMonitoringDetail",
        new { title, category, scheduleDate, assessmentType });
}
```

**Core pattern — CLONE single-session essay loader (`:3419-3446`):**
```csharp
var assignment = await _context.UserPackageAssignments
    .FirstOrDefaultAsync(a => a.AssessmentSessionId == session.Id);
// guard assignment == null
var shuffled = assignment.GetShuffledQuestionIds();
var essayQs = await _context.PackageQuestions
    .Where(q => shuffled.Contains(q.Id) && q.QuestionType == "Essay")
    .ToListAsync();
var essayRespMap = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == session.Id &&
           essayQs.Select(q => q.Id).Contains(r.PackageQuestionId))
    .ToDictionaryAsync(r => r.PackageQuestionId);
var items = essayQs.Select((q, idx) => new EssayGradingItemViewModel {
    QuestionId = q.Id, DisplayNumber = idx + 1, QuestionText = q.QuestionText ?? "",
    Rubrik = q.Rubrik,
    TextAnswer = essayRespMap.TryGetValue(q.Id, out var resp) ? resp.TextAnswer : null,
    EssayScore = essayRespMap.TryGetValue(q.Id, out var resp2) ? resp2.EssayScore : null,
    ScoreValue = q.ScoreValue, ImagePath = q.ImagePath, ImageAlt = q.ImageAlt
}).ToList();
```

**EssayPendingCount untuk header/badge (pola `:3308-3316`, single session):**
```csharp
var essayPendingCount = await _context.PackageUserResponses
    .Where(r => r.AssessmentSessionId == session.Id && r.EssayScore == null)
    .Join(_context.PackageQuestions.Where(q => q.QuestionType == "Essay"),
        r => r.PackageQuestionId, q => q.Id, (r, q) => r)
    .CountAsync();
```

**Compute isFinalized + pass ViewModel (D-10)** — gate identik `:451-453`:
```csharp
var isFinalized = session.Status == AssessmentConstants.AssessmentStatus.Completed
                  && !string.IsNullOrEmpty(session.NomorSertifikat);
// pass EssayGradingPageViewModel (lihat file #5) atau MonitoringSessionViewModel + ViewBag
return View(model);
```

> **Anti-pattern (RESEARCH):** JANGAN tulis query ad-hoc baru untuk load essay items — clone builder existing agar tetap shuffle-aware (`GetShuffledQuestionIds()`) + response-join benar.

---

### 3. `Views/Admin/EssayGrading.cshtml` — NEW (view, request-response)

**Analog:** essay CARD markup `AssessmentMonitoringDetail.cshtml:407-476` (clone byte-for-byte struktur) + breadcrumb/back `:55-67` + antiforgery `:484` + `_QuestionImage` partial.

**Header + back-link (POLA back-link `:64-67` + breadcrumb `:55-61`)** — carry 4 nav param (Pitfall 3):
```cshtml
<nav aria-label="breadcrumb" class="mb-3">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a href="@Url.Action("Index", "Admin")">Kelola Data</a></li>
        <li class="breadcrumb-item"><a href="@Url.Action("AssessmentMonitoring", "AssessmentAdmin")">Assessment Monitoring</a></li>
        <li class="breadcrumb-item active">@Model.UserFullName</li>
    </ol>
</nav>
<div class="mb-4">
    <a href="@Url.Action("AssessmentMonitoringDetail", new { title = Model.Title, category = Model.Category, scheduleDate = Model.ScheduleDate, assessmentType = Model.AssessmentType })"
       class="btn btn-outline-secondary btn-sm">
        <i class="bi bi-arrow-left me-1"></i>Kembali ke Monitoring
    </a>
</div>
<h2 class="fw-bold mb-1">@Model.UserFullName</h2>
<p class="text-muted mb-0"><i class="bi bi-person-badge me-1"></i>NIP: @Model.UserNIP</p>
```

**Kartu essay — CLONE `:407-446` MEMPERTAHANKAN selector PERSIS (Pitfall 1 — kritis):**
```cshtml
@foreach (var essayItem in Model.EssayItems)
{
    <div class="card shadow-sm mb-3 essay-grading-card" id="essay_@essayItem.QuestionId">
        <div class="card-body">
            <div class="d-flex justify-content-between align-items-start mb-2">
                <h6 class="fw-semibold">Soal @essayItem.DisplayNumber: @essayItem.QuestionText</h6>
                <span class="badge @(essayItem.EssayScore.HasValue ? "bg-success" : "bg-secondary") essay-status-badge"
                      id="badge_@(Model.SessionId)_@essayItem.QuestionId">
                    @(essayItem.EssayScore.HasValue ? "Sudah Dinilai" : "Belum Dinilai")
                </span>
            </div>
            @await Html.PartialAsync("_QuestionImage", new { ImagePath = essayItem.ImagePath, ImageAlt = essayItem.ImageAlt, Cap = 240 })
            <div class="border rounded p-3 bg-light mb-2">
                <small class="text-muted d-block mb-1">Jawaban Pekerja:</small>
                <p class="mb-0">@(essayItem.TextAnswer ?? "(tidak ada jawaban)")</p>
            </div>
            @* rubrik collapse :421-430 *@
            <div class="d-flex align-items-center gap-2">
                <label class="form-label mb-0 small">Skor:</label>
                <input type="number" class="form-control essay-score-input"
                       min="0" max="@essayItem.ScoreValue" style="max-width:80px"
                       value="@essayItem.EssayScore"
                       data-question-id="@essayItem.QuestionId"
                       data-session-id="@Model.SessionId"
                       @(Model.IsFinalized ? "disabled" : "") />
                <span class="text-muted small">/ @essayItem.ScoreValue</span>
                @if (!Model.IsFinalized)
                {
                    <button class="btn btn-primary btn-sm btn-save-essay-score"
                            data-question-id="@essayItem.QuestionId"
                            data-session-id="@Model.SessionId">Simpan Skor</button>
                }
            </div>
        </div>
    </div>
}
```

> **MANDATORY selectors (Pitfall 1 — handler hard-bind):** `.essay-grading-card`, `.essay-score-input`, `id="badge_{sessionId}_{questionId}"`, `data-question-id`, `data-session-id`, `.btn-save-essay-score`, `id="finalizeSection_{sessionId}"`, `.btn-finalize-grading`. Mengubah salah satunya = handler diam-diam gagal (POST 200 tapi badge tak update).

**Section finalize — CLONE `:448-476` + read-only gate D-10 (Pitfall 6 tooltip wrapper `:460-468`):**
```cshtml
<div id="finalizeSection_@Model.SessionId"
     style="display: @(Model.EssayPendingCount == 0 ? "block" : "none")">
    @if (Model.IsFinalized)
    {
        <span data-bs-toggle="tooltip" title="@tooltipText" style="display:inline-block;">
            <button class="btn btn-success btn-finalize-grading" data-session-id="@Model.SessionId"
                    disabled style="pointer-events: none;">
                <i class="bi bi-check-circle me-1"></i>Selesaikan Penilaian
            </button>
        </span>
    }
    else
    {
        <button class="btn btn-success btn-finalize-grading" data-session-id="@Model.SessionId">
            <i class="bi bi-check-circle me-1"></i>Selesaikan Penilaian
        </button>
    }
</div>
```

**Antiforgery form (REUSE `:484` verbatim) — WAJIB untuk handler AJAX:**
```cshtml
<form id="antiforgeryForm" style="display:none">@Html.AntiForgeryToken()</form>
```

**Script include — POLA `@section Scripts` `:1232-1236`** (referensikan js baru + lightbox partial untuk `_QuestionImage`):
```cshtml
@await Html.PartialAsync("_ImageLightboxModal")
@section Scripts {
    <script src="~/js/essay-grading.js"></script>
}
```

> **Anti-pattern (RESEARCH security V4.XSS):** JANGAN `@Html.Raw` pada `TextAnswer`/`QuestionText`/`Rubrik` — Razor auto-encode (pola existing `:419` aman).

---

### 4. `wwwroot/js/essay-grading.js` — NEW (utility, event-driven)

**Analog:** inline handlers `AssessmentMonitoringDetail.cshtml:1472-1558` (logic) + extracted file `wwwroot/js/edit-peserta-answers.js` (struktur file: `DOMContentLoaded` guard + early-return). 8 file `wwwroot/js/*.js` existing = precedent kuat extract.

**Pola file extract (dari `edit-peserta-answers.js:1-3`):**
```javascript
document.addEventListener("DOMContentLoaded", () => {
  // guard: only run on essay grading page
  if (!document.querySelector('.essay-grading-card')) return;
  // ... handlers
});
```

**Simpan Skor handler — REUSE `:1472-1517` apa adanya** (helper global `appUrl` dari `_Layout.cshtml:55`, token dari `#antiforgeryForm`):
```javascript
document.querySelectorAll('.btn-save-essay-score').forEach(function(btn) {
  btn.addEventListener('click', async function() {
    const sessionId = this.dataset.sessionId, questionId = this.dataset.questionId;
    const card = this.closest('.essay-grading-card');
    const score = parseInt(card.querySelector('.essay-score-input').value);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    if (isNaN(score)) { alert('Masukkan nilai skor yang valid.'); return; }
    this.disabled = true;
    try {
      const res = await fetch(appUrl('/Admin/SubmitEssayScore'), {
        method: 'POST',
        headers: {'Content-Type':'application/x-www-form-urlencoded','X-Requested-With':'XMLHttpRequest'},
        body: 'sessionId='+sessionId+'&questionId='+questionId+'&score='+score
              +'&__RequestVerificationToken='+encodeURIComponent(token)
      });
      const data = await res.json();
      if (data.success) {
        const badge = document.getElementById('badge_'+sessionId+'_'+questionId);
        if (badge) { badge.className = 'badge bg-success essay-status-badge'; badge.textContent = 'Sudah Dinilai'; }
        if (data.allGraded) {
          const fs = document.getElementById('finalizeSection_'+sessionId);
          if (fs) fs.style.display = 'block';
        }
      } else { alert(data.message); }
    } catch (err) { alert('Gagal menyimpan skor. Silakan coba lagi.'); }
    finally { this.disabled = false; }
  });
});
```

**Finalize handler — D-09 TITIK PERUBAHAN (vs existing `:1547`):**
- Existing monitoring (`:1545-1547`): on success pertama → `location.reload()`.
- **Page baru (D-09): GANTI `location.reload()` jadi update IN-PLACE** (TANPA redirect):
  - badge soal sudah `bg-success` "Sudah Dinilai" (dari Simpan Skor)
  - tombol finalize → `disabled` + state "Selesai" (hijau)
  - semua `.essay-score-input` → `disabled`
  - tombol `.btn-save-essay-score` → hide/disable
  - cabang `alreadyFinalized` (`:1537-1544`, `showAlert('info', ...)`) DIPERTAHANKAN
- `showAlert` helper (`:1452-1455`) saat ini inline di view → tentukan: bawa ke js baru atau definisikan ulang minimal. (Planner pilih — Discretion D-08.)

> **Don't hand-roll (RESEARCH):** URL prefix WAJIB lewat `appUrl('/Admin/...')` (app deploy under sub-path `/KPB-PortalHC`). Token plumbing REUSE `querySelector('input[name="__RequestVerificationToken"]')`.

---

### 5. `Models/AssessmentMonitoringViewModel.cs` — maybe `EssayGradingPageViewModel` (model, transform)

**Analog:** SAME FILE — `MonitoringSessionViewModel` (`:48-68`) + `EssayGradingItemViewModel` (`:73-86`).

**Keputusan (RESEARCH Open Question #2 — pembungkus DIREKOMENDASIKAN):**
- **Tabel worker-list TIDAK perlu ViewModel baru** — `MonitoringSessionViewModel` (`:48-68`) sudah punya `UserFullName`/`UserNIP`/`HasManualGrading`/`EssayPendingCount`/`Status`/`NomorSertifikat`. Reuse `Model.Sessions.Where(...)`.
- **Page per-worker:** buat `EssayGradingPageViewModel` pembungkus (type-safe, lebih bersih dari ViewBag untuk single-session page). Field minimum:

```csharp
public class EssayGradingPageViewModel
{
    public int SessionId { get; set; }
    public string UserFullName { get; set; } = "";
    public string UserNIP { get; set; } = "";
    public int EssayPendingCount { get; set; }
    public bool IsFinalized { get; set; }
    public DateTime? CompletedAt { get; set; }           // untuk tooltip read-only (pola :454-456)
    public List<EssayGradingItemViewModel> EssayItems { get; set; } = new();
    // 4 nav param untuk back-link (Pitfall 3)
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string ScheduleDate { get; set; } = "";       // yyyy-MM-dd string (Pitfall 4 tz-safe)
    public string? AssessmentType { get; set; }
}
```

> Convention class existing: PascalCase props, default-init string `= ""` dan `List<> = new()`, XML doc comment `<summary>` (pola `:70-72`).

---

### 6. `tests/e2e/essay-grading-384.spec.ts` + `tests/sql/essay-grading-384-seed.sql` — NEW (test, event-driven)

**Analog:** `tests/e2e/assessment-pending-grade.spec.ts` (snapshot/seed/restore harness, FULL) + `tests/e2e/assessment.spec.ts` FLOW 9 (`:265-394`, selector essay finalize) + `tests/sql/pending345-seed.sql` (seed shape) + `tests/helpers/accounts.ts` (admin creds) + `tests/helpers/dbSnapshot.ts`.

**Snapshot/seed/restore harness — CLONE `assessment-pending-grade.spec.ts:43-83`:**
```typescript
test.describe.configure({ mode: 'serial' });
// beforeAll: resolve InstanceDefaultBackupPath → db.backup(snapshotPath)
//            → db.execScript('../sql/essay-grading-384-seed.sql')
//            → Layer 1 assert seeded (COUNT > 0)
//            → resolve scheduleDate via CONVERT(varchar(10), Schedule, 23) (tz-safe)
// afterAll:  db.restore(snapshotPath) dalam try; Layer 4 assert DB bersih (COUNT == 0)
```

**Login — inline `loginAny` pola `:25-34`** (accept any redirect away dari `/Account/Login`; pakai `accounts.admin`):
```typescript
async function loginAny(page, accountKey) {
  const { email, password } = accounts[accountKey];
  await page.goto('/Account/Login');
  await page.fill('input[name="email"]', email);
  await page.fill('input[name="password"]', password);
  await Promise.all([
    page.waitForURL(url => !url.toString().includes('/Account/Login'), { timeout: 15_000 }),
    page.click('button[type="submit"]'),
  ]);
}
```

**Selector assertions — REUSE pola FLOW 9 (`assessment.spec.ts:298-336`):**
```typescript
const finalizeBtn = page.locator('.btn-finalize-grading').first();
await Promise.all([
  page.waitForResponse(res => res.url().includes('/Admin/FinalizeEssayGrading') && res.status() === 200),
  finalizeBtn.click()
]);
page.on('dialog', dialog => dialog.accept()); // confirm() auto-accept
```

**Flow e2e UIG-04 (D-11):**
1. `loginAny(page, 'admin')`
2. goto `/Admin/AssessmentMonitoringDetail?title&category&scheduleDate` → assert tabel worker-list render + baris worker target + badge 🟡 "{N} belum dinilai"
3. klik `a:has-text("Tinjau Essay")` → `waitForURL(/EssayGrading/)` → assert `<h2>` nama worker + `.essay-grading-card`
4. fill `.essay-score-input` → klik `.btn-save-essay-score` → `waitForResponse(/SubmitEssayScore/ 200)` → assert `#badge_{sid}_{qid}` → "Sudah Dinilai" (`bg-success`)
5. klik `.btn-finalize-grading` → `waitForResponse(/FinalizeEssayGrading/ 200)` → assert **IN-PLACE** "Selesai" (badge hijau, input disabled), **URL TETAP `/EssayGrading`** (BUKAN reload) — kunci D-09
6. (opsional) klik "Kembali ke Monitoring" → assert balik benar; badge worker → 🟢 "Selesai"

**Seed fixture (`essay-grading-384-seed.sql`) — pola `pending345-seed.sql`, butuh 1 AssessmentSession dengan:**
- `HasManualGrading = 1`, `Status = AssessmentConstants.AssessmentStatus.PendingGrading` (agar finalize lolos gate `:3524`)
- ≥1 `PackageQuestion` `QuestionType='Essay'` + `UserPackageAssignment.ShuffledQuestionIds` berisi essay question id
- `PackageUserResponse` (TextAnswer terisi, `EssayScore = NULL` = ungraded)
- (opsional fixture kedua read-only D-10): `Status='Completed'` + `NomorSertifikat` terisi
- Klasifikasi seed `temporary + local-only` (CLAUDE.md Seed Workflow). Title prefix `[ESSAY384]` untuk cleanup query.

> **Run lokal (Pitfall 5 + memory Phase 355):** `playwright.config.ts` TANPA `webServer` → app TIDAK auto-start. Jalankan `dotnet run` manual dengan env `Authentication__UseActiveDirectory=false` SEBELUM e2e. Combined run WAJIB `--workers=1` (NTLM loopback + shared-memory). `fullyParallel:false` sudah di config.
> **Razor dynamic → runtime wajib (Phase 354):** grep+build TIDAK cukup membuktikan selector/handler match. e2e WAJIB.

---

## Shared Patterns

### Authorization (V2/V4 — IDOR mitigation)
**Source:** `Controllers/AssessmentAdminController.cs:3456`, `:3497` + class `[Authorize]` `AdminBaseController.cs:12`
**Apply to:** new GET `EssayGrading` action
```csharp
[Authorize(Roles = "Admin, HC")]
```
Admin/HC = full-access tier (komentar in-repo `AdminBaseController:47` "no role scoping — Admin/HC full access"). Tidak ada user-level ownership check (HC menilai semua worker). GET read-only, no DB write baru, no antiforgery di GET.

### Antiforgery token plumbing (V4.2 CSRF)
**Source:** `AssessmentMonitoringDetail.cshtml:484` (form) + `:1479`/`:1524` (read) — pola identik 4× di view existing
**Apply to:** `EssayGrading.cshtml` (form) + `essay-grading.js` (read)
```cshtml
<form id="antiforgeryForm" style="display:none">@Html.AntiForgeryToken()</form>
```
```javascript
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
// body: '...&__RequestVerificationToken=' + encodeURIComponent(token)
```
POST endpoint existing sudah `[ValidateAntiForgeryToken]` (`:3457`, `:3498`) — TAK diubah.

### URL prefix (basePath-aware) — Don't hand-roll
**Source:** `Views/Shared/_Layout.cshtml:54-55` global helper
**Apply to:** semua fetch di `essay-grading.js`
```javascript
function appUrl(path) { return basePath + (path.startsWith('/') ? path : '/' + path); }
// usage: fetch(appUrl('/Admin/SubmitEssayScore'), ...)
```
App deploy under sub-path `/KPB-PortalHC` — hardcode `/Admin/...` akan 404.

### Image render (anti-drift) — Don't hand-roll
**Source:** `Views/Shared/_QuestionImage.cshtml` (reflection-based, null-skip, lightbox+lazy+a11y)
**Apply to:** kartu essay di `EssayGrading.cshtml`
```cshtml
@await Html.PartialAsync("_QuestionImage", new { ImagePath = essayItem.ImagePath, ImageAlt = essayItem.ImageAlt, Cap = 240 })
```
Render NOTHING bila ImagePath null/whitespace. Butuh `_ImageLightboxModal` partial juga (existing `:1230`).

### Endpoint contract (UNCHANGED — reuse apa adanya)
**Source:** `AssessmentAdminController.cs:3458-3487` (SubmitEssayScore) + `:3499-3534+` (FinalizeEssayGrading)
**Apply to:** `essay-grading.js` AJAX
```csharp
// POST /Admin/SubmitEssayScore (sessionId, questionId, score)
//   → { success:true, pendingCount:int, allGraded:bool }  | { success:false, message }
// POST /Admin/FinalizeEssayGrading (sessionId)
//   → first finalize: { success:true }
//   → already Completed: { success:true, alreadyFinalized:true, message, score, isPassed, nomorSertifikat }
//   → non-PendingGrading: { success:false, message (BI literal per status) }
```
Skor divalidasi backend (`:3472` range 0..ScoreValue) — read-only client (D-10) = UX guard, defense-in-depth backend sudah ada.

---

## No Analog Found

Tidak ada. Semua 6 file punya analog in-repo (exact atau role-match). Phase ini refactor murni — tidak ada role/data-flow baru di codebase. RESEARCH §State of the Art mengonfirmasi: tak ada library/komponen/migration baru.

---

## Metadata

**Analog search scope:**
- `Views/Admin/AssessmentMonitoringDetail.cshtml` (view markup + inline handlers)
- `Controllers/AssessmentAdminController.cs` + `Controllers/AdminBaseController.cs` (action + routing + authz)
- `Models/AssessmentMonitoringViewModel.cs` (ViewModels)
- `Views/Shared/_QuestionImage.cshtml`, `_Layout.cshtml` (partials + global helpers)
- `wwwroot/js/*.js` (8 extracted files — precedent)
- `tests/e2e/` (23 specs), `tests/helpers/` (accounts/dbSnapshot), `tests/sql/` (5 seed fixtures)

**Files scanned:** ~14 source + test files (read), grep across controller + view + specs

**Key patterns identified:**
1. **Tabel Bootstrap** = `<div class="table-responsive"><table class="table table-hover align-middle mb-0"><thead class="table-light">` — pola app-wide, mirror untuk worker-list.
2. **Essay grading handler** = hard-bind selectors (`.essay-grading-card`/`.essay-score-input`/`#badge_{sid}_{qid}`/`#finalizeSection_{sid}`) — clone byte-for-byte WAJIB.
3. **Controller** = `[Route("Admin/[action]")]` + `[Authorize(Roles="Admin, HC")]` + EF `_context` query + `EssayGradingMap` builder (shuffle-aware).
4. **JS extract** = `wwwroot/js/*.js` precedent (8 file) + `appUrl()` global + antiforgery via `#antiforgeryForm`.
5. **e2e** = snapshot→seed→UAT→restore (SEED_WORKFLOW), `--workers=1`, `Authentication__UseActiveDirectory=false`, Razor dynamic → Playwright runtime wajib.

**Pattern extraction date:** 2026-06-15
