---
phase: 338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute
type: research
created: 2026-05-30
confidence: HIGH
language: Bahasa Indonesia
researcher_session: claude-opus-4-7
consumed_by: gsd-planner (5 plan output)
---

# Phase 338 — RESEARCH.md

> **Catatan untuk planner:** Penelitian ini sudah codebase-verified (semua line number dibaca langsung). Banyak asumsi awal di CONTEXT.md ternyata sudah ter-implementasi parsial di Phase 320 — sehingga scope CIL-05 berubah dari "tambah 2 sheet baru" jadi "tambah 2 aggregate sheet di atas per-peserta sheets yang sudah ada". OQ-338-1 juga sudah TERSELESAIKAN di codebase (SkiaSharp `SpiderChartRenderer.cs` sudah ada).

---

## User Constraints (from CONTEXT.md)

### Locked Decisions

**REST-04** Strategy A — Re-import via Excel backup `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx` (13 peserta, score total). Audit tag `[BACKFILL]`. CompletedAt manual `2026-03-30`. LinkedSessionId/LinkedGroupId paired ke PostTest sessionId 9-21 (verify saat execute — sessionId Dev DB).

**REST-06** Naming convention strict `{Stage} Test {Track} {Lokasi}`. Track Master: OJT GAST, OJT Pekerja GAST, CMP, CDP, BP, KKJ. LinkedGroupId default manual assign (Edge 3 Pilihan A). Backward audit tool DEFER OQ-336-4.

**D-01 CIL-01** Badge counter Closed (preserve default "Open").
**D-02 CIL-03** Row clickable + kolom Actions (reuse Plan 337-02 CMP-19 pattern).
**D-03 CIL-05 Detail** Grid per-peserta-per-soal (`No | Nama | NIP | Soal 1 Jawaban | Soal 1 Benar? | ... | Score Total`).
**D-04 CIL-05 Elemen** Matrix peserta x elemen (`Nama | NIP | Elemen 1 Score | ... | Avg`).
**D-05 CIL-06** Multi-page PDF per peserta (cover + spider Page 1, jawaban Page 2+).
**D-06 REST-05** Enhance existing DB_HANDOFF_IT pattern (template Markdown + backup script PowerShell + DEV_WORKFLOW SOP).
**D-07 Plan split** 5 plan per wave.

### Claude's Discretion
- OQ-338-1 spider chart rendering — **RESOLVED di research ini**, lihat OQ Resolution
- OQ-338-2 REST-04 bulk insert mechanism — **RESOLVED**, lihat OQ Resolution
- OQ-338-3 CIL-05 query optimization — **RESOLVED**, lihat OQ Resolution

### Deferred Ideas (OUT OF SCOPE)
- Per-user dismissal cookie CIL-04
- UserPreferences table per-user default filter override CIL-01
- GitHub Actions auto-backup REST-05 Option B
- Backward audit-rename tool REST-06 (OQ-336-4 defer)
- Spider chart Playwright headless rendering (sudah resolved via existing helper)

---

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| CIL-01 | Filter default Manage Assessment + Monitoring + badge counter Closed | `_AssessmentGroupsTab.cshtml` status filter dropdown; ManageAssessmentTab_Assessment L198-201 `GroupStatus` aggregation |
| CIL-02 | Search "Cilacap" Semua Status return parent group | **Root cause confirmed L198-201** `if (string.IsNullOrEmpty(statusFilter)) grouped = grouped.Where(g => g.GroupStatus != "Closed")` — Closed groups di-exclude saat tidak ada explicit filter; fix: tampilkan saat search term present |
| CIL-03 | History tab row clickable + Actions kolom | `_HistoryTab.cshtml` L73-100; `AllWorkersHistoryRow` BUTUH field SessionId baru (currently absent); `GetAllWorkersHistory` di `WorkerDataService` butuh projection update |
| CIL-04 | Banner alert di `/CMP/Assessment` admin/HC role | `Views/CMP/Assessment.cshtml` (CMPController.Assessment L195); banner di-render top of page kalau `User.IsInRole("Admin") || User.IsInRole("HC")` |
| CIL-05 | Excel +2 sheet aggregate (Detail Per Soal grid + Elemen Teknis matrix) | `AssessmentAdminController.ExportAssessmentResults` L4077; per-peserta sheets sudah exist sejak Phase 320 — sheet baru ADDITIVE (aggregate cross-peserta view) |
| CIL-06 | `/Admin/BulkExportPdf` ZIP via QuestPDF | QuestPDF 2026.2.2 + SkiaSharp `SpiderChartRenderer` sudah tersedia; pattern existing di `ExportCategoriesPdf` L565-613 (`Document.Create` + `GeneratePdf(stream)`); ZIP via `System.IO.Compression.ZipArchive` built-in .NET 8 |
| REST-04 | Restore 13 PreTest via direct EF insert (bukan UI loop) | `AddManualAssessment` di `TrainingAdminController.cs:638` — viewmodel TIDAK punya AssessmentType/LinkedGroupId fields; **rekomendasi: endpoint admin baru `BulkBackfillAssessment` direct EF insert dalam transaction**, bukan loop call UI |
| REST-05 | Template + script backup pre-deploy | `docs/DB_HANDOFF_IT_2026-05-26.html` L502-625 sudah ada BACKUP + RESTORE pattern; `scripts/` empty (folder exist tapi kosong); SEED_WORKFLOW.md (CLAUDE.md mention) sudah pakai `sqlcmd BACKUP DATABASE` precedent |
| REST-06 | LinkedGroupId auto-pair admin create form | `Controllers/AssessmentAdminController.cs:815` CreateAssessment POST — research belum verifikasi line exact tapi pola dropdown ada di other create forms (cross-link future) |
| REST-07 | DEV_WORKFLOW.md update Section baru | `docs/DEV_WORKFLOW.md` L106-118 Pre-Commit Checklist section — extend dengan Pre-Deploy Backup SOP linking template + script |

---

## Executive Summary

**Big surprise:** `ExportAssessmentResults` di Phase 320 **sudah implement per-peserta sheets** dengan SkiaSharp spider PNG + table Elemen Teknis + table Detail Jawaban (`AssessmentAdminController.cs:4234-4438`). CIL-05 oleh karena itu BUKAN green-field "tambah 2 sheet" — melainkan **tambah 2 sheet AGGREGATE** (cross-peserta grid/matrix) di atas existing per-peserta sheets. Scope tetap valid, value tetap, tapi planner harus akui infrastruktur eksisting (jangan re-build).

**Per REQ insight ringkas:**

- **CIL-01** Badge counter Closed. Source counter: same `grouped` collection L194-196, tambah `Count(g => g.GroupStatus == "Closed")` SEBELUM filter L198-201. Render di view filter dropdown.
- **CIL-02** **Root cause CONFIRMED L198-201**: kalau `statusFilter` null, `grouped = grouped.Where(g => g.GroupStatus != "Closed")` — Closed di-strip silently. Search term tidak override behavior ini. Fix sederhana: kalau search non-empty, jangan strip Closed (atau biarkan strip tapi tambah hint "X grup Closed cocok — klik filter Closed untuk lihat").
- **CIL-03** Row clickable pattern siap reuse dari `Records.cshtml` (Plan 337-02 CMP-19) — tapi `AllWorkersHistoryRow` model BUTUH field `SessionId` baru. `WorkerDataService.GetAllWorkersHistory()` butuh projection update untuk include SessionId.
- **CIL-04** Banner trivial — admin/HC check + `<div class="alert alert-info">` di Assessment.cshtml top.
- **CIL-05** 2 aggregate sheet baru. Detail Per Soal = grid dinamis kolom (No|Nama|NIP|Soal 1 Jawaban|Soal 1 Benar?|...|Score). Elemen Teknis = matrix (Nama|NIP|Elemen 1%|...|Avg). **Query reuse `allResponses` + `allEtScores` + `allQuestions` yang SUDAH di-load di L4236-4252**. Zero additional DB query.
- **CIL-06** QuestPDF Multi-page per peserta + ZIP stream. Reuse `SpiderChartRenderer.RenderRadarPng` byte[] untuk embed image di QuestPDF (`Image(byte[])`). ZIP via `ZipArchive` write-only mode untuk memory efficiency.
- **REST-04** Direct EF insert dalam transaction lebih bersih dari UI loop. `AddManualAssessment` ViewModel kurang field (no AssessmentType/LinkedGroupId/LinkedSessionId).
- **REST-05** Markdown template + PowerShell `BACKUP DATABASE` script. Sudah ada precedent T-SQL inline di DB_HANDOFF_IT_2026-05-26.html L504-506.
- **REST-06** LinkedGroupId manual assign (Edge 3 Pilihan A) — admin pilih dari dropdown counterpart Pre/Post.
- **REST-07** DEV_WORKFLOW.md tambah section "Pre-Deploy Backup SOP".

**Primary recommendation:** Plan 03 (CIL-05) bisa **<200 LOC** karena infrastruktur Phase 320 sudah lengkap. Plan 04 (REST-04 + CIL-06) lebih besar karena 2 endpoint baru. Plan 05 paling kompleks (template tooling + PowerShell script).

---

## DB Schema Findings

### `PackageUserResponses` table (model `Models/PackageUserResponse.cs`)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | |
| AssessmentSessionId | int FK → AssessmentSession | |
| PackageQuestionId | int FK → PackageQuestion | |
| PackageOptionId | int? FK → PackageOption | NULL untuk soal Essay (TextAnswer dipakai) |
| SubmittedAt | DateTime | DateTime.UtcNow default |
| TextAnswer | string? | Essay jawaban (null untuk MC/MA) |
| EssayScore | int? | Manual HC grading (null = belum dinilai) |

**Verified [VERIFIED: Models/PackageUserResponse.cs full read].** Schema mendukung CIL-05 grid format — query JOIN PackageOption untuk option text + IsCorrect, JOIN PackageQuestion untuk question text + QuestionType.

### `SessionElemenTeknisScores` table (model `Models/SessionElemenTeknisScore.cs`)

| Field | Type | Notes |
|-------|------|-------|
| Id | int PK | |
| AssessmentSessionId | int FK | |
| ElemenTeknis | string | Nama elemen ("Lainnya" untuk untagged) |
| CorrectCount | int | Jumlah jawaban benar untuk elemen |
| QuestionCount | int | Total soal untuk elemen |

**Verified [VERIFIED: Models/SessionElemenTeknisScore.cs full read].** Percentage = `CorrectCount / QuestionCount * 100`. Matrix sheet CIL-05 D-04 bisa langsung pivot.

### `AssessmentSessions.IsManualEntry` flag + AuditLog mechanism

`IsManualEntry: bool` field exists (`Models/AssessmentSession.cs:131`). Used di `AddManualAssessment` (TrainingAdminController:694) untuk distinguish online vs HC-input.

**AuditLog tag mechanism [VERIFIED: AddManualAssessment L703-706]:**
```csharp
await _auditLog.LogAsync(actor.Id, actor.FullName, "Create",
    $"Assessment manual ditambahkan: {model.Title} untuk {model.WorkerCerts!.Count} pekerja",
    0, "AssessmentSession");
```
- `actionType` parameter: "Create" (string, no enum)
- `description`: free-text — REST-04 pakai prefix `[BACKFILL]` literal di description (no DB schema change needed)
- `targetType`: "AssessmentSession" (string)

**REST-04 audit pattern:** Loop 13x setelah insert, log per-row dengan description `[BACKFILL] Re-import PreTest OJT GAST Cilacap dari Excel backup 30 Mar 2026 — root cause: Phase 336 ROOT_CAUSE.md (IT redeploy tanpa backup)` + targetId = sessionId baru.

### `AssessmentSessions` fields critical untuk REST-04

| Field | Required REST-04 | Source |
|-------|-----------------|--------|
| UserId | yes | Excel NIP → User lookup |
| Title | yes | Rename ke `Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap` |
| Category | yes | Default `"Assessment Proton"` atau equivalent — verify saat execute |
| Schedule | yes | 2026-03-30 |
| Score | yes | Excel kolom Score |
| IsPassed | yes | Excel kolom Pass/Fail |
| CompletedAt | yes | 2026-03-30 (manual) |
| Status | yes | `"Completed"` |
| IsManualEntry | yes | `true` |
| AssessmentType | yes | `"PreTest"` |
| LinkedGroupId | yes | FK ke PostTest counterpart group (lookup via 13 NIP cross-match PostTest Cilacap 19 May) |
| LinkedSessionId | optional | Per-row pair ke PostTest sessionId 9-21 |
| Penyelenggara | yes | `"Tim HC Pertamina KPB"` |
| Kota | yes | `"Cilacap"` |
| SubKategori | yes | `"OJT Pekerja GAST"` |
| CertificateType | optional | `"Annual"` atau confirm user |
| CreatedAt | yes | DateTime.UtcNow |
| CreatedBy | yes | HC admin user.Id yang execute |

---

## OQ Resolution

### OQ-338-1: CIL-06 Spider chart rendering — Playwright vs JS capture?

**RESOLVED: NEITHER. Reuse existing `Helpers/SpiderChartRenderer.cs` (SkiaSharp PNG renderer).**

**Evidence [VERIFIED: Helpers/SpiderChartRenderer.cs full read + AssessmentAdminController.cs:4277 usage]:**
- File `Helpers/SpiderChartRenderer.cs` sudah exist sejak Phase 320 (commit di v17.0)
- API: `public static byte[] RenderRadarPng(IList<(string label, double percentage)> data, int size = 500)`
- Sudah dipakai di `ExportAssessmentResults` L4277 dengan `Parallel.ForEachAsync` MaxDegreeOfParallelism = ProcessorCount untuk batch
- Output: PNG byte[], embed-ready di QuestPDF via `Image(byte[])` API atau ClosedXML `ws.AddPicture(ms, name)`

**Rationale:**
- Playwright headless: butuh install Chromium + browser launch ~500ms per session = 50 PDF * 500ms = 25 detik overhead untuk launch saja. PLUS dependency runtime (~150MB)
- JS client-capture: workflow break (sync PDF generation butuh sync data)
- QuestPDF native chart: tidak ada built-in chart component (2026.2.2 confirmed)
- **SkiaSharp PNG**: deterministic, no async, no external dep, sudah proven di Phase 320 untuk 50 peserta <30s

**Implementation hint Plan 04:**
```csharp
// Per peserta loop di BulkExportPdf
var etData = etScores
    .Where(et => et.AssessmentSessionId == session.Id)
    .OrderBy(e => e.ElemenTeknis)
    .Select(e => (e.ElemenTeknis, e.QuestionCount > 0 ? (double)e.CorrectCount / e.QuestionCount * 100 : 0d))
    .ToList();

byte[] spiderPng = HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(etData, 400);
// Embed di QuestPDF page:
// page.Content().Image(spiderPng); -- atau FitArea/FitWidth
```

### OQ-338-2: REST-04 Bulk insert — SQL script vs UI loop vs hybrid endpoint?

**RESOLVED: Hybrid endpoint A3 (new admin-only `BulkBackfillAssessment` direct EF insert dalam transaction).**

**Rationale:**

| Option | Pros | Cons |
|--------|------|------|
| A1: Loop UI `AddManualAssessment` 13x | Audit log natural | ViewModel `CreateManualAssessmentViewModel` TIDAK punya AssessmentType/LinkedGroupId/LinkedSessionId field — manual hack workaround needed. UI overhead 13 HTTP request |
| A2: Bulk SQL script | Fastest | Bypass EF validation, bypass AuditLog hook, no .NET-side type safety |
| A3: New `BulkBackfillAssessment` admin endpoint | Transactional, audit log per row, reusable future, full ViewModel control | New endpoint (~100 LOC) |

**Pattern (recommended):**

```csharp
// Controllers/AssessmentAdminController.cs — new endpoint
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> BulkBackfillAssessment(BulkBackfillViewModel model)
{
    // Pre-check duplicate (T-338-RISK)
    var existing = await _context.AssessmentSessions
        .Where(a => a.Title == model.Title && a.CompletedAt == model.CompletedAt)
        .CountAsync();
    if (existing > 0)
    {
        TempData["Error"] = $"Duplicate detected: {existing} row sudah ada untuk title+date ini. Abort.";
        return RedirectToAction("ManageAssessment");
    }

    using var tx = await _context.Database.BeginTransactionAsync();
    var actor = await _userManager.GetUserAsync(User);
    var createdSessionIds = new List<int>();

    try
    {
        foreach (var entry in model.Entries)
        {
            var session = new AssessmentSession
            {
                UserId = entry.UserId,
                Title = model.Title,
                Category = model.Category,
                Schedule = model.CompletedAt,
                CompletedAt = model.CompletedAt,
                Status = "Completed",
                IsManualEntry = true,
                AssessmentType = "PreTest",
                LinkedGroupId = model.LinkedGroupId,
                Score = entry.Score,
                IsPassed = entry.IsPassed,
                PassPercentage = 70,
                Penyelenggara = model.Penyelenggara,
                Kota = model.Kota,
                SubKategori = model.SubKategori,
                CertificateType = model.CertificateType,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actor!.Id
            };
            _context.AssessmentSessions.Add(session);
        }
        await _context.SaveChangesAsync();

        // Audit log per row
        foreach (var session in /* sessions just added */)
        {
            await _auditLog.LogAsync(actor!.Id, actor.FullName, "Create",
                $"[BACKFILL] Re-import PreTest dari Excel backup — root cause Phase 336",
                session.Id, "AssessmentSession");
        }

        await tx.CommitAsync();
        TempData["Success"] = $"Berhasil backfill {model.Entries.Count} PreTest entries.";
    }
    catch
    {
        // disposal auto-rollback
        throw;
    }
    return RedirectToAction("ManageAssessment");
}
```

**ViewModel baru (~30 LOC):**
```csharp
public class BulkBackfillViewModel {
    public string Title { get; set; }
    public string Category { get; set; }
    public DateTime CompletedAt { get; set; }
    public int? LinkedGroupId { get; set; }
    public string? Penyelenggara { get; set; }
    public string? Kota { get; set; }
    public string? SubKategori { get; set; }
    public string? CertificateType { get; set; }
    public List<BulkBackfillEntry> Entries { get; set; } = new();
}

public class BulkBackfillEntry {
    public string UserId { get; set; }
    public int Score { get; set; }
    public bool IsPassed { get; set; }
}
```

**Trigger mechanism:** Bisa dua pilihan:
1. **One-shot script** (rekomendasi REST-04): pre-construct model dari Excel (Plan 04 task 1), POST sekali via Playwright atau curl
2. **Permanent admin page** `/Admin/BulkBackfillAssessment` dengan file upload Excel: future reusable utility

**Plan 04 rekomendasi:** Pilihan (1) untuk REST-04 execute saja. (2) bisa di-defer ke backlog kalau user request reusable tool nanti.

### OQ-338-3: CIL-05 PackageUserResponses query optimization — N+1 risk?

**RESOLVED: NO N+1 — query optimization sudah ter-implementasi di Phase 320 (L4234-4252).**

**Evidence [VERIFIED: AssessmentAdminController.cs:4234-4252]:**

```csharp
// Pre-load all per-session data in single query (avoid N+1 — T-320-02-07).
var eligibleSessionIds = eligibleSessions.Select(s => s.Id).ToList();
var allResponses = await _context.PackageUserResponses
    .Where(r => eligibleSessionIds.Contains(r.AssessmentSessionId))
    .ToListAsync();
var allEtScores = await _context.SessionElemenTeknisScores
    .Where(et => eligibleSessionIds.Contains(et.AssessmentSessionId))
    .ToListAsync();

// Load all questions+options for involved packages.
var sessionPackageMap = await _context.UserPackageAssignments
    .Where(a => eligibleSessionIds.Contains(a.AssessmentSessionId))
    .Select(a => new { a.AssessmentSessionId, a.AssessmentPackageId })
    .ToListAsync();
var packageIds = sessionPackageMap.Select(x => x.AssessmentPackageId).Distinct().ToList();
var allQuestions = await _context.PackageQuestions
    .Include(q => q.Options)
    .Where(q => packageIds.Contains(q.AssessmentPackageId))
    .ToListAsync();
```

**Total queries: 4 (responses + etScores + sessionPackageMap + questions). Constant, not per-session.**

**CIL-05 implementation:** Reuse 4 collections in-memory untuk render aggregate sheets. Zero additional DB roundtrip.

**Memory budget:** 50 peserta x 30 soal = 1500 rows responses + 50 ET breakdown + 30 questions = trivial untuk in-memory aggregation.

---

## Reusable Pattern Inventory

### From Phase 320 (v17.0 Assessment Export)
- **`SpiderChartRenderer.RenderRadarPng`** — SkiaSharp PNG byte[], reuse di CIL-06 BulkExportPdf
- **`SheetNameSanitizer.Sanitize`** — Excel sheet name collision guard, reuse kalau aggregate sheet baru butuh dynamic naming
- **`Parallel.ForEachAsync` PNG pre-compute** — pattern untuk batch generate spider PNG di CIL-06 (kalau 50 peserta)
- **Pre-load 4-query pattern** (L4234-4252) — reuse di CIL-05 aggregate sheets (zero new query)

### From Phase 337-02 (CMP-19 row keyboard nav)
- **`data-href` + delegated click + keydown Enter/Space** — reuse di CIL-03 History tab row clickable
  ```cshtml
  <tr class="row-clickable"
      data-href="/CMP/Results/@row.SessionId"
      tabindex="0" role="link"
      style="cursor:pointer;">
  ```
  ```javascript
  document.querySelectorAll('.row-clickable[data-href]').forEach(row => {
      row.addEventListener('click', () => window.location.href = row.dataset.href);
      row.addEventListener('keydown', (e) => {
          if (e.key === 'Enter' || e.key === ' ') {
              e.preventDefault();
              window.location.href = row.dataset.href;
          }
      });
  });
  ```

### From Phase 337-03 (CMP-24 IQueryable composition)
- **Pre-build query, .Where() chain, single ToListAsync** — pattern relevant untuk CIL-02 search query fix (composition vs imperative)

### From existing QuestPDF usage (`ExportCategoriesPdf` L565-613)
- **`Document.Create(container => container.Page(page => ...))`** — standard pattern
- **`page.Content().Column(col => col.Item().Text(...))`** — composition
- **Embed PNG: `col.Item().Image(byte[] pngBytes)`** — for spider chart
- **`pdf.GeneratePdf(stream)`** then `return File(stream.ToArray(), "application/pdf", fileName)`

### From DB_HANDOFF_IT_2026-05-26.html
- **HTML doc styling** (red/navy palette, callout boxes, `pre.sql` code blocks) — REST-05 template inherit visual identity
- **BACKUP DATABASE T-SQL inline** L504-506:
  ```sql
  BACKUP DATABASE HcPortalDB_Dev
  TO DISK='C:\Backup\HcPortalDB_Dev.<date>-<context>.bak'
  WITH FORMAT, INIT;
  ```
- **RESTORE DATABASE** L615-620 dengan SINGLE_USER + REPLACE + MULTI_USER pattern (REST-05 reference for rollback section)

### From CLAUDE.md / SEED_WORKFLOW.md
- **`sqlcmd BACKUP DATABASE`** already adopted convention — REST-05 script extend ini, pakai PowerShell wrapper untuk parameterized + repeatable

---

## Risk Register Update (refined T-338-01..06)

| ID | Threat | Likelihood | Impact | Refined Mitigation |
|----|--------|------------|--------|---------------------|
| T-338-01 | CIL-05 Excel format breaking change downstream | LOW | MED | Per Phase 320 sudah ADDITIVE precedent. 2 sheet baru DI BELAKANG per-peserta sheets. Tool IT yang index by sheet name "Summary" tetap aman |
| T-338-02 | CIL-06 BulkExportPdf DoS / memory spike | MED | HIGH | (a) Enforce max 50 peserta hard limit di endpoint validation, (b) stream ZIP entry-by-entry (jangan accumulate PDF di memory — write 1 PDF entry, flush, next), (c) `CancellationToken` honor, (d) per-PDF MemoryStream `using` scope (auto-dispose post-write) |
| T-338-03 | REST-04 audit trust — fake row 30 Mar | HIGH | LOW | `[BACKFILL]` prefix mandatory di description + duplicate pre-check query + 1 ad-hoc execution audit log entry oleh actor |
| T-338-04 | REST-05 backup script secret leak via git | LOW | HIGH | Script accept `$env:DB_PASSWORD` / `-SqlPassword` param, NEVER hardcode. `.gitignore *.bak`. Document explicit di script header comment. SQL Server Windows Auth preferred (zero credential) |
| T-338-05 | CIL-01 badge counter user lama tidak notice | MED | LOW | (a) Use `bg-secondary` distinct color, (b) tooltip `title="X assessment dengan status Closed"`, (c) optional `position-absolute top-0 start-100 translate-middle` notification dot, (d) accept some users tidak notice — non-blocking |
| T-338-06 | REST-06 LinkedGroupId auto-pair orphan | LOW | MED | Per CONTEXT D-02 + NAMING-SPEC Edge 3 Pilihan A: DEFAULT manual assign (dropdown), not auto. Edge 3 Pilihan B (regex auto-detect) defer ke backlog |
| **T-338-07 NEW** | REST-04 UserId lookup failure 13 NIP | MED | HIGH | Pre-check di Plan 04 task 1: query `_context.Users.Where(u => excelNips.Contains(u.NIP)).ToListAsync()` — kalau count != 13, list missing NIP + abort sebelum insert |
| **T-338-08 NEW** | CIL-03 GetAllWorkersHistory schema change | LOW | LOW | `AllWorkersHistoryRow` add nullable `SessionId int?` (training rows null). Backward compat: existing view rows tetap render, baru row punya SessionId field tambahan |
| **T-338-09 NEW** | CIL-02 fix regression risk | LOW | MED | Test: search "Cilacap" Semua Status return Closed groups; search "" Semua Status TIDAK regression (default behavior preserve Open+Upcoming only); test with empty filter + non-matching search (return 0 expected) |

---

## Plan-by-Plan Researcher Notes

### Plan 01 — Wave 1 (CIL-01 + CIL-02) — Filter Badge + Search Aggregation Fix

**Scope:** 2 REQ. Estimasi ~80 LOC.

**Files to modify:**
- `Controllers/AssessmentAdminController.cs` L198-201 (ManageAssessmentTab_Assessment) — CIL-02 fix
- `Controllers/AssessmentAdminController.cs` ManageAssessmentTab_Assessment + AssessmentMonitoring action — CIL-01 add `ViewBag.ClosedCount`
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` — render badge counter di dropdown filter Closed option
- `Views/Admin/AssessmentMonitoring.cshtml` (verify path) — badge same

**Implementation approach:**

CIL-02 fix (the simpler bug):
```csharp
// CURRENT L198-201
if (string.IsNullOrEmpty(statusFilter))
    grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
else if (statusFilter == "Open" || statusFilter == "Upcoming" || statusFilter == "Closed")
    grouped = grouped.Where(g => g.GroupStatus == statusFilter).ToList();

// FIX: kalau search term explicit ada, biarkan all status
if (string.IsNullOrEmpty(statusFilter) && string.IsNullOrEmpty(search))
    grouped = grouped.Where(g => g.GroupStatus != "Closed").ToList();
else if (!string.IsNullOrEmpty(statusFilter) && (statusFilter == "Open" || statusFilter == "Upcoming" || statusFilter == "Closed"))
    grouped = grouped.Where(g => g.GroupStatus == statusFilter).ToList();
// else (search non-empty, statusFilter empty): pass-through all status (CIL-02 fix)
```

CIL-01 badge:
```csharp
// COMPUTE BEFORE FILTER (capture pre-filter count)
var allGroupedSnapshot = grouped.ToList(); // duplicate untuk count
ViewBag.ClosedCount = allGroupedSnapshot.Count(g => g.GroupStatus == "Closed");
ViewBag.OpenCount = allGroupedSnapshot.Count(g => g.GroupStatus == "Open" || g.GroupStatus == "InProgress");
ViewBag.UpcomingCount = allGroupedSnapshot.Count(g => g.GroupStatus == "Upcoming");
// THEN apply filter
```

```cshtml
<!-- _AssessmentGroupsTab.cshtml status dropdown -->
<option value="Closed">Closed @if((int)(ViewBag.ClosedCount ?? 0) > 0) { <span class="badge bg-secondary">@ViewBag.ClosedCount</span> }</option>
```

**Threats:** T-338-05 (badge not noticed) + T-338-09 (regression risk).

**UAT criteria:**
- AC1: Open default filter masih tampil hanya Open+Upcoming groups (no Closed bleed)
- AC2: Search "Cilacap" tanpa status filter tampilkan Closed groups Cilacap (PostTest 19 May → sudah Closed bila pass date)
- AC3: Badge counter Closed match `_context.AssessmentSessions` raw Closed count per kategori grup

---

### Plan 02 — Wave 2 (CIL-03 + CIL-04) — History Drill-down + Banner Alert

**Scope:** 2 REQ. Estimasi ~120 LOC.

**Files to modify:**
- `Models/AllWorkersHistoryRow.cs` — add `public int? SessionId { get; set; }` (nullable; training rows = null)
- `Services/WorkerDataService.cs` (find) — `GetAllWorkersHistory` projection update untuk include `SessionId = a.Id` di assessment branch
- `Views/Admin/Shared/_HistoryTab.cshtml` L73-100 — `<tr>` jadi clickable (data-href + tabindex), tambah `<th>Aksi</th>` + `<td><a href="/CMP/Results/{sessionId}"><i class="bi bi-eye"></i> Lihat</a></td>` di kolom akhir
- `Views/CMP/Assessment.cshtml` — banner alert (CIL-04) top of page

**CIL-03 implementation:**

ViewBag pendukung untuk training rows (yang null SessionId): row TIDAK clickable, action kolom kosong.

```cshtml
<!-- _HistoryTab.cshtml table header tambah kolom -->
<th class="p-3 text-end">Aksi</th>

<!-- Row -->
<tr class="assessment-history-row @(row.SessionId.HasValue ? "row-clickable" : "")"
    @(row.SessionId.HasValue ? Html.Raw($"data-href=\"/CMP/Results/{row.SessionId}\" tabindex=\"0\" role=\"link\"") : Html.Raw(""))
    style="@(row.SessionId.HasValue ? "cursor:pointer;" : "")"
    data-worker="..." data-title="...">
    ...
    <td class="p-3 text-end">
        @if (row.SessionId.HasValue)
        {
            <a class="btn btn-sm btn-outline-primary"
               href="/CMP/Results/@row.SessionId"
               onclick="event.stopPropagation();"
               aria-label="Lihat detail assessment">
                <i class="bi bi-eye"></i> Lihat
            </a>
        }
    </td>
</tr>

<script>
document.querySelectorAll('.row-clickable[data-href]').forEach(row => {
    row.addEventListener('click', (e) => {
        if (e.target.closest('a, button')) return; // prevent double-trigger
        window.location.href = row.dataset.href;
    });
    row.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            window.location.href = row.dataset.href;
        }
    });
});
</script>
```

**CIL-04 implementation:**
```cshtml
<!-- Views/CMP/Assessment.cshtml top -->
@if (User.IsInRole("Admin") || User.IsInRole("HC"))
{
    <div class="alert alert-info alert-dismissible fade show d-flex align-items-center" role="alert">
        <i class="bi bi-info-circle me-2"></i>
        <div class="flex-grow-1">
            <strong>Lihat assessment yang sudah selesai?</strong>
            <a href="/Admin/ManageAssessment?tab=history" class="alert-link">Buka Riwayat Assessment di Admin Panel</a>
            untuk drill-down per peserta.
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Tutup"></button>
    </div>
}
```

(Banner non-persistent — auto-dismiss tidak persist antar refresh per CONTEXT deferred — full feature OK.)

**Threats:** T-338-08 (schema add — backward compat verify).

**UAT criteria:**
- AC1: Row assessment clickable + keyboard Enter trigger redirect ke /CMP/Results/{id}
- AC2: Row training (SessionId null) TIDAK clickable, action kolom kosong (no visible button)
- AC3: Admin/HC user lihat banner di /CMP/Assessment, klik link redirect ke /Admin/ManageAssessment?tab=history
- AC4: Worker biasa (Coachee role only) TIDAK lihat banner
- AC5: Banner dismiss button works (session-only OK, no cookie persist per CONTEXT defer)

---

### Plan 03 — Wave 3 (CIL-05) HIGH PRIORITY — Excel +2 Aggregate Sheets

**Scope:** 1 REQ. Estimasi ~150 LOC.

**Files to modify:**
- `Controllers/AssessmentAdminController.cs` `ExportAssessmentResults` L4438 (AFTER existing per-peserta loop, BEFORE `return ExcelExportHelper.ToFileResult`) — insert 2 new sheets

**Critical context for planner:** Per-peserta sheets sudah ada (L4282-4438). 2 sheet baru = AGGREGATE view yang COMPLEMENT, bukan duplicate. Naming proposal:
- Sheet 2 (after Summary, before per-peserta): `"Detail Per Soal"` (grid)
- Sheet 3: `"Elemen Teknis"` (matrix)

**Sheet 2 "Detail Per Soal" implementation (D-03 grid):**

```csharp
// Insert SETELAH L4438 sebelum return
{
    var soalSheet = workbook.Worksheets.Add("Detail Per Soal");
    soalSheet.Cell(1, 1).Value = "Detail Per Soal — Grid Peserta x Soal";
    soalSheet.Cell(1, 1).Style.Font.Bold = true;
    soalSheet.Cell(1, 1).Style.Font.FontSize = 13;

    // Determine canonical question order (per first package — assumption: all peserta dapat same package)
    // EDGE CASE: kalau peserta beda package, fallback "[unknown]" untuk soal yang tidak applicable
    var allEligibleQuestionIds = sessionPackageMap
        .SelectMany(sp => allQuestions
            .Where(q => q.AssessmentPackageId == sp.AssessmentPackageId)
            .OrderBy(q => q.Id)
            .Select(q => q.Id))
        .Distinct()
        .ToList();

    var canonicalQuestions = allQuestions
        .Where(q => allEligibleQuestionIds.Contains(q.Id))
        .OrderBy(q => q.Id)
        .ToList();

    // Header row 3: No | Nama | NIP | Soal 1 Jawaban | Soal 1 Benar? | ... | Score Total
    int headerRow = 3;
    int col = 1;
    soalSheet.Cell(headerRow, col++).Value = "No";
    soalSheet.Cell(headerRow, col++).Value = "Nama";
    soalSheet.Cell(headerRow, col++).Value = "NIP";
    for (int qi = 0; qi < canonicalQuestions.Count; qi++)
    {
        soalSheet.Cell(headerRow, col++).Value = $"Soal {qi + 1} Jawaban";
        soalSheet.Cell(headerRow, col++).Value = $"Soal {qi + 1} Benar?";
    }
    soalSheet.Cell(headerRow, col).Value = "Score Total";

    soalSheet.Range(headerRow, 1, headerRow, col).Style.Font.Bold = true;
    soalSheet.Range(headerRow, 1, headerRow, col).Style.Fill.BackgroundColor = XLColor.LightBlue;

    // Data rows
    int dataRow = headerRow + 1;
    int no = 1;
    foreach (var session in eligibleSessions.Where(s => !s.IsManualEntry))
    {
        int c = 1;
        soalSheet.Cell(dataRow, c++).Value = no++;
        soalSheet.Cell(dataRow, c++).Value = session.User?.FullName ?? "Unknown";
        soalSheet.Cell(dataRow, c++).Value = session.User?.NIP ?? "—";

        var sessionResp = allResponses.Where(r => r.AssessmentSessionId == session.Id).ToList();

        foreach (var q in canonicalQuestions)
        {
            var response = sessionResp.FirstOrDefault(r => r.PackageQuestionId == q.Id);
            string jawaban; bool? benar = null;

            if (response == null) { jawaban = "Tidak dijawab"; benar = false; }
            else if (q.QuestionType == "Essay")
            {
                jawaban = "[Essay]";
                benar = response.EssayScore.HasValue
                    ? response.EssayScore.Value >= (q.ScoreValue / 2)  // verify rubric threshold
                    : (bool?)null;
            }
            else
            {
                // MC or MA
                var opt = q.Options.FirstOrDefault(o => o.Id == response.PackageOptionId);
                jawaban = opt?.OptionText ?? "—";
                benar = opt?.IsCorrect ?? false;
            }

            soalSheet.Cell(dataRow, c++).Value = jawaban;
            soalSheet.Cell(dataRow, c++).Value = benar.HasValue ? (benar.Value ? "✓" : "✗") : "—";
        }

        soalSheet.Cell(dataRow, c).Value = session.Score ?? 0;
        dataRow++;
    }
}
```

**Sheet 3 "Elemen Teknis" implementation (D-04 matrix):**

```csharp
{
    var etSheet = workbook.Worksheets.Add("Elemen Teknis");
    etSheet.Cell(1, 1).Value = "Elemen Teknis — Matrix Peserta x Elemen";
    etSheet.Cell(1, 1).Style.Font.Bold = true;
    etSheet.Cell(1, 1).Style.Font.FontSize = 13;

    // Distinct elemen names across all eligible sessions
    var distinctElemens = allEtScores
        .Select(et => et.ElemenTeknis)
        .Distinct()
        .OrderBy(e => e)
        .ToList();

    int headerRow = 3;
    int col = 1;
    etSheet.Cell(headerRow, col++).Value = "No";
    etSheet.Cell(headerRow, col++).Value = "Nama";
    etSheet.Cell(headerRow, col++).Value = "NIP";
    foreach (var elemen in distinctElemens)
        etSheet.Cell(headerRow, col++).Value = elemen;
    etSheet.Cell(headerRow, col).Value = "Rata-rata";

    etSheet.Range(headerRow, 1, headerRow, col).Style.Font.Bold = true;
    etSheet.Range(headerRow, 1, headerRow, col).Style.Fill.BackgroundColor = XLColor.LightBlue;

    int dataRow = headerRow + 1;
    int no = 1;
    foreach (var session in eligibleSessions.Where(s => !s.IsManualEntry))
    {
        int c = 1;
        etSheet.Cell(dataRow, c++).Value = no++;
        etSheet.Cell(dataRow, c++).Value = session.User?.FullName ?? "Unknown";
        etSheet.Cell(dataRow, c++).Value = session.User?.NIP ?? "—";

        var sessionEt = allEtScores.Where(et => et.AssessmentSessionId == session.Id).ToList();
        double sumPct = 0; int countPct = 0;

        foreach (var elemen in distinctElemens)
        {
            var entry = sessionEt.FirstOrDefault(et => et.ElemenTeknis == elemen);
            if (entry != null && entry.QuestionCount > 0)
            {
                double pct = (double)entry.CorrectCount / entry.QuestionCount * 100;
                etSheet.Cell(dataRow, c++).Value = $"{pct:F1}%";
                sumPct += pct; countPct++;
            }
            else
            {
                etSheet.Cell(dataRow, c++).Value = "—";
            }
        }

        etSheet.Cell(dataRow, c).Value = countPct > 0 ? $"{sumPct / countPct:F1}%" : "—";
        dataRow++;
    }
}
```

**Sheet order recommendation (planner decide):** Summary → Detail Per Soal → Elemen Teknis → per-peserta sheet 1..N. Aggregate sheets di awal supaya analyst Excel discover dulu sebelum drill-down per-peserta.

**Threats:** T-338-01 (format breaking — mitigation: ADDITIVE only, no rename "Summary" sheet).

**UAT criteria:**
- AC1: Excel buka di MS Excel + LibreOffice, 2 sheet baru visible
- AC2: Detail Per Soal grid kolom dinamis sesuai jumlah soal package (e.g., 30 soal → 60 kolom jawaban+benar)
- AC3: Elemen Teknis matrix kolom = distinct elemen names, baris = peserta, sel = percentage
- AC4: Sheet existing (Summary + per-peserta) TIDAK regression (sama persis pre-change)
- AC5: Manual entry sessions (IsManualEntry=true) di-skip dari aggregate sheets (sama dengan existing per-peserta logic)
- AC6: 50 peserta x 30 soal performance < 30s (same SLA Phase 320 EXP-08)

---

### Plan 04 — Wave 4 (REST-04 + CIL-06) — Restore Execute + BulkExportPdf ZIP

**Scope:** 2 REQ. Estimasi ~400 LOC (paling besar di phase ini).

**Files to modify/create:**
- `Models/BulkBackfillViewModel.cs` (new) — REST-04 ViewModel
- `Controllers/AssessmentAdminController.cs` — REST-04 endpoint `BulkBackfillAssessment` + CIL-06 endpoint `BulkExportPdf`
- `scripts/restore-pretest-cilacap-2026-03-30.ps1` or `.cs` (new) — REST-04 one-shot trigger script (optional, atau dijadikan Plan 04 task 1 dengan curl/Playwright)
- `tests/e2e/bulk-export-pdf.spec.ts` (optional) — CIL-06 Playwright smoke

**REST-04 implementation:** Lihat OQ-338-2 resolution (full code sample di atas).

**REST-04 trigger sequence:**
1. **Task 1 (pre-flight):** Parse Excel `downloads/PreTest-OJT_GAST__GTO__SRU_RU_IV__20260330_Results.xlsx` → 13 BulkBackfillEntry (UserId via NIP lookup, Score, IsPassed). Validate 13/13 NIP exist di DB (T-338-07).
2. **Task 2 (LinkedGroupId discovery):** Query PostTest counterpart group — `SELECT TOP 1 LinkedGroupId FROM AssessmentSessions WHERE AssessmentType='PostTest' AND Title LIKE '%OJT Pekerja GAST%Cilacap%' AND CompletedAt > '2026-05-19'`. Capture untuk Pre pair.
3. **Task 3 (execute):** POST to `BulkBackfillAssessment` endpoint dengan model lengkap. Verify response success.
4. **Task 4 (verify):** Query `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE 'Pre Test OJT Pekerja GAST%Cilacap' AND CompletedAt='2026-03-30'` = 13. Query AuditLog 13 entries `[BACKFILL]`. Cross-match dengan CSV `04-Pre-vs-Post-Comparison.csv`.

**CIL-06 implementation pattern:**

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
public async Task<IActionResult> BulkExportPdf(string title, string category, DateTime scheduleDate, CancellationToken ct)
{
    // Reuse Phase 320 pattern — load eligible sessions + related data
    var sessions = await _context.AssessmentSessions
        .Include(a => a.User)
        .Where(a => a.Title == title && a.Category == category && a.Schedule.Date == scheduleDate.Date)
        .ToListAsync(ct);

    if (sessions.Count == 0) { TempData["Error"] = "No sessions."; return RedirectToAction("ManageAssessment"); }
    if (sessions.Count > 50)
    {
        TempData["Error"] = "Batch limit 50 peserta. Filter lebih sempit.";
        return RedirectToAction("ManageAssessment");
    }

    var eligibleSessions = sessions
        .Where(s => s.Status != "Cancelled" && ((s.CompletedAt != null || s.Score != null) || s.Status == "Abandoned"))
        .OrderBy(s => s.User?.FullName ?? "")
        .ToList();

    var eligibleSessionIds = eligibleSessions.Select(s => s.Id).ToList();
    var allResponses = await _context.PackageUserResponses
        .Where(r => eligibleSessionIds.Contains(r.AssessmentSessionId))
        .ToListAsync(ct);
    var allEtScores = await _context.SessionElemenTeknisScores
        .Where(et => eligibleSessionIds.Contains(et.AssessmentSessionId))
        .ToListAsync(ct);
    var sessionPackageMap = await _context.UserPackageAssignments
        .Where(a => eligibleSessionIds.Contains(a.AssessmentSessionId))
        .Select(a => new { a.AssessmentSessionId, a.AssessmentPackageId })
        .ToListAsync(ct);
    var packageIds = sessionPackageMap.Select(x => x.AssessmentPackageId).Distinct().ToList();
    var allQuestions = await _context.PackageQuestions
        .Include(q => q.Options)
        .Where(q => packageIds.Contains(q.AssessmentPackageId))
        .ToListAsync(ct);

    // Stream ZIP — write entry-by-entry untuk memory efficiency
    var zipStream = new MemoryStream();
    using (var zip = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var session in eligibleSessions)
        {
            ct.ThrowIfCancellationRequested();

            // Pre-compute spider PNG
            var etData = allEtScores
                .Where(et => et.AssessmentSessionId == session.Id)
                .OrderBy(e => e.ElemenTeknis)
                .Select(e => (e.ElemenTeknis, e.QuestionCount > 0 ? (double)e.CorrectCount / e.QuestionCount * 100 : 0d))
                .ToList();

            byte[] spiderPng = etData.Count >= 3
                ? HcPortal.Helpers.SpiderChartRenderer.RenderRadarPng(etData, 400)
                : Array.Empty<byte>();

            // Build per-peserta PDF
            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                // Page 1: Cover + Spider
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(40);
                    page.Header().Text($"{session.User?.FullName ?? "Unknown"} (NIP {session.User?.NIP ?? "—"})").FontSize(14).Bold();
                    page.Content().Column(col =>
                    {
                        col.Item().Text(title).FontSize(12).Bold();
                        col.Item().Text($"Tanggal: {session.CompletedAt:dd MMM yyyy HH:mm}");
                        col.Item().Text($"Score: {session.Score ?? 0} / 100");
                        col.Item().Text($"Status: {(session.IsPassed == true ? "Pass" : session.IsPassed == false ? "Fail" : "—")}")
                                  .FontColor(session.IsPassed == true ? QuestPDF.Helpers.Colors.Green.Medium : QuestPDF.Helpers.Colors.Red.Medium);
                        if (spiderPng.Length > 0)
                            col.Item().PaddingTop(20).Image(spiderPng).FitArea();
                    });
                    page.Footer().AlignCenter().Text(t => { t.Span("Page "); t.CurrentPageNumber(); t.Span(" of "); t.TotalPages(); });
                });

                // Page 2+: Detail Jawaban per soal
                var sessionPackage = sessionPackageMap.FirstOrDefault(x => x.AssessmentSessionId == session.Id);
                if (sessionPackage != null)
                {
                    var sessionQuestions = allQuestions.Where(q => q.AssessmentPackageId == sessionPackage.AssessmentPackageId).OrderBy(q => q.Id).ToList();
                    var sessionResp = allResponses.Where(r => r.AssessmentSessionId == session.Id).ToList();

                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(40);
                        page.Header().Text("Detail Jawaban").Bold().FontSize(12);
                        page.Content().Column(col =>
                        {
                            int no = 1;
                            foreach (var q in sessionQuestions)
                            {
                                var response = sessionResp.FirstOrDefault(r => r.PackageQuestionId == q.Id);
                                string jawaban; bool correct;
                                if (response == null) { jawaban = "Tidak dijawab"; correct = false; }
                                else if (q.QuestionType == "Essay") { jawaban = response.TextAnswer ?? "—"; correct = (response.EssayScore ?? 0) > 0; }
                                else
                                {
                                    var opt = q.Options.FirstOrDefault(o => o.Id == response.PackageOptionId);
                                    jawaban = opt?.OptionText ?? "—";
                                    correct = opt?.IsCorrect == true;
                                }
                                string correctText = string.Join(", ", q.Options.Where(o => o.IsCorrect).Select(o => o.OptionText));

                                col.Item().PaddingVertical(8).Border(0.5f).Padding(10).Column(qcol =>
                                {
                                    qcol.Item().Text($"Soal {no++}: {q.QuestionText}").Bold().FontSize(10);
                                    qcol.Item().PaddingTop(4).Text(t => { t.Span("Jawaban: ").Bold(); t.Span(jawaban); }).FontSize(9);
                                    qcol.Item().Text(t => { t.Span("Benar: ").Bold(); t.Span(correctText); }).FontSize(9);
                                    qcol.Item().Text($"Status: {(correct ? "Benar" : "Salah")}")
                                        .FontColor(correct ? QuestPDF.Helpers.Colors.Green.Medium : QuestPDF.Helpers.Colors.Red.Medium)
                                        .FontSize(9);
                                });
                            }
                        });
                    });
                }
            });

            // Write entry to ZIP — stream, no memory accumulation
            string safeName = System.Text.RegularExpressions.Regex.Replace(session.User?.NIP ?? "NA", @"[^\w]", "_");
            string safeFullName = System.Text.RegularExpressions.Regex.Replace(session.User?.FullName ?? "Unknown", @"[^\w]", "_");
            var entry = zip.CreateEntry($"{safeName}_{safeFullName}.pdf", System.IO.Compression.CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            using var pdfMs = new MemoryStream();
            pdf.GeneratePdf(pdfMs);
            pdfMs.Position = 0;
            await pdfMs.CopyToAsync(entryStream, ct);
        }
    }

    zipStream.Position = 0;
    var safeTitle = System.Text.RegularExpressions.Regex.Replace(title, @"[^\w]", "_");
    var fileName = $"{safeTitle}_{scheduleDate:yyyyMMdd}_Bundle.zip";
    return File(zipStream.ToArray(), "application/zip", fileName);
}
```

**Threats:** T-338-02 (memory DoS — mitigation di code: 50 limit + stream + per-PDF dispose). T-338-07 (REST-04 NIP lookup — pre-check).

**UAT criteria REST-04:**
- AC1: 13 row baru di AssessmentSessions Title="Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap" AssessmentType="PreTest"
- AC2: 13 AuditLog entries description prefix `[BACKFILL]`, actorUserId = HC admin executor
- AC3: LinkedGroupId match PostTest counterpart Group
- AC4: Pre vs Post comparison CSV `04-Pre-vs-Post-Comparison.csv` matches DB query post-import
- AC5: Re-run BulkBackfillAssessment dengan model sama → return error "duplicate detected" (T-338-03)

**UAT criteria CIL-06:**
- AC1: `/Admin/BulkExportPdf?title=...&category=...&scheduleDate=...` return application/zip Content-Type
- AC2: ZIP berisi N file PDF (1 per peserta eligible) with name `{NIP}_{Nama}.pdf`
- AC3: Each PDF berisi cover page (nama+nip+score+pass status+spider chart) + detail jawaban pages
- AC4: 50 peserta batch < 60 detik (relaxed SLA dari EXP-08 30s karena PDF lebih kompleks dari Excel)
- AC5: 51 peserta batch return TempData Error "Batch limit 50"
- AC6: Spider chart embed visible di PDF (PNG render verify)
- AC7: Manual entry sessions (IsManualEntry=true) di-skip atau punya special handling (planner decide)

---

### Plan 05 — Wave 5 (REST-05 + REST-06 + REST-07) — Backup Hook + LinkedGroupId Enforce + DEV_WORKFLOW

**Scope:** 3 REQ. Estimasi ~250 LOC + dokumentasi.

**Files to create/modify:**
- `docs/templates/DB_HANDOFF_IT.template.md` (new) — markdown template dengan placeholders
- `scripts/backup-dev-pre-migration.ps1` (new) — PowerShell BACKUP DATABASE wrapper
- `scripts/render-handoff.ps1` or `scripts/render-handoff.cs` (new, optional) — placeholder substitution renderer (markdown → HTML)
- `docs/DEV_WORKFLOW.md` L106-118 — add "Section 7: Pre-Deploy Backup SOP"
- `Controllers/AssessmentAdminController.cs` `CreateAssessment` POST handler — LinkedGroupId dropdown integration (find line — Grep suggested L815 in CONTEXT, verify saat execute)
- `Views/AssessmentAdmin/CreateAssessment.cshtml` (or equivalent) — dropdown "Pair with existing assessment" UI

**REST-05 template structure (`DB_HANDOFF_IT.template.md`):**

```markdown
# Handoff IT — {{title}} ({{date}})

**Date:** {{date}}
**Commit Hash:** {{commit_hash}}
**Migration Flag:** {{has_migration}} ({{migration_list}})
**Affected Tables:** {{affected_tables}}

## Step 1 — Pre-Pull DB Snapshot
{{#has_migration}}
Wajib backup sebelum apply migration:

```powershell
.\scripts\backup-dev-pre-migration.ps1 `
  -Server "10.55.3.3" `
  -Database "HcPortalDB_Dev" `
  -OutputPath "C:\Backup\HcPortalDB_Dev.{{date}}-pre-deploy.bak"
```
{{/has_migration}}

## Step 2 — Git Pull
```bash
cd C:\inetpub\wwwroot\KPB-PortalHC
git pull origin main
```

## Step 3 — Apply Migration
{{#has_migration}}
```bash
dotnet ef database update --context ApplicationDbContext --no-build
```
{{/has_migration}}

## Step 4 — Verify
- Open http://10.55.3.3/KPB-PortalHC
- Smoke test login + golden path

## Rollback (kalau gagal)
```sql
ALTER DATABASE HcPortalDB_Dev SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE HcPortalDB_Dev
FROM DISK='C:\Backup\HcPortalDB_Dev.{{date}}-pre-deploy.bak'
WITH REPLACE;
ALTER DATABASE HcPortalDB_Dev SET MULTI_USER;
```
```

**REST-05 backup script (`scripts/backup-dev-pre-migration.ps1`):**

```powershell
<#
.SYNOPSIS
    Pre-deploy backup wrapper untuk SQL Server (Windows Auth default).
.EXAMPLE
    .\backup-dev-pre-migration.ps1 -Server "10.55.3.3" -Database "HcPortalDB_Dev"
.NOTES
    Default OutputPath: C:\Backup\{Database}.{yyyyMMdd_HHmmss}-pre-deploy.bak
    Uses Windows Auth (Integrated Security) — TIDAK accept password param (security policy).
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Server,
    [Parameter(Mandatory=$true)][string]$Database,
    [Parameter(Mandatory=$false)][string]$OutputPath
)

if (-not $OutputPath) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $OutputPath = "C:\Backup\$Database.$timestamp-pre-deploy.bak"
}

# Ensure backup dir exists
$backupDir = Split-Path $OutputPath -Parent
if (-not (Test-Path $backupDir)) {
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Write-Host "[INFO] Created backup directory: $backupDir"
}

Write-Host "[INFO] Starting BACKUP DATABASE [$Database] TO DISK='$OutputPath'..."

$sql = @"
BACKUP DATABASE [$Database]
TO DISK = N'$OutputPath'
WITH FORMAT, INIT, NAME = '$Database-Pre-Deploy', SKIP, NOREWIND, NOUNLOAD, STATS = 10;
"@

try {
    Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $sql -QueryTimeout 600 -ErrorAction Stop
    $size = (Get-Item $OutputPath).Length / 1MB
    Write-Host "[OK] Backup complete: $OutputPath ($([Math]::Round($size, 2)) MB)" -ForegroundColor Green
    exit 0
} catch {
    Write-Error "[FAIL] Backup failed: $_"
    exit 1
}
```

**Sqlcmd dependency note:** Script assume `Invoke-Sqlcmd` SqlServer PowerShell module installed di IT machine. Fallback: pakai `sqlcmd.exe` CLI (always available di SQL Server install). Planner decide which to use OR support both via runtime check.

**REST-06 LinkedGroupId dropdown:**

```csharp
// AssessmentAdminController.CreateAssessment GET — populate ViewBag dropdown
ViewBag.PairCandidates = await _context.AssessmentSessions
    .Where(a => a.AssessmentType == "PreTest" || a.AssessmentType == "PostTest")
    .Where(a => a.LinkedGroupId != null)
    .Where(a => a.CompletedAt >= DateTime.UtcNow.AddMonths(-6))
    .OrderByDescending(a => a.CompletedAt)
    .Select(a => new { a.LinkedGroupId, Display = $"{a.AssessmentType} — {a.Title} — {a.CompletedAt:dd MMM yyyy}" })
    .Distinct()
    .ToListAsync();
```

```cshtml
<select asp-for="LinkedGroupId" class="form-select">
    <option value="">— Tidak terhubung —</option>
    @foreach (var c in ViewBag.PairCandidates as IEnumerable<dynamic> ?? Enumerable.Empty<dynamic>())
    {
        <option value="@c.LinkedGroupId">@c.Display</option>
    }
</select>
<small class="text-muted">Pair dengan PreTest/PostTest counterpart untuk reporting Pre-vs-Post.</small>
```

**REST-07 DEV_WORKFLOW.md tambahan:**

```markdown
## 7. Pre-Deploy Backup SOP (REST-05/06/07)

Setiap deploy ke Dev/Prod yang ADA MIGRATION:

1. Generate handoff doc dari template:
   ```bash
   # (Manual: edit template, replace placeholders {{date}}, {{commit_hash}}, etc.)
   cp docs/templates/DB_HANDOFF_IT.template.md docs/DB_HANDOFF_IT_$(date +%Y-%m-%d).md
   # Render HTML pakai pandoc atau markdown-to-html tool pilihan
   ```
2. Attach handoff doc ke email + WhatsApp ke Team IT
3. Team IT WAJIB jalankan **SEBELUM** apply migration:
   ```powershell
   .\scripts\backup-dev-pre-migration.ps1 -Server "10.55.3.3" -Database "HcPortalDB_Dev"
   ```
4. Verify backup file exist di `C:\Backup\` dan size > 0
5. Baru jalankan `dotnet ef database update`

**Rationale:** Phase 336 root cause = IT redeploy tanpa backup → PreTest Cilacap data loss. Backup SOP ini eliminate recurrence dengan force backup step explicit setiap deploy.

**Reference:** `docs/DB_HANDOFF_IT_2026-05-13.html` + `docs/DB_HANDOFF_IT_2026-05-26.html` sebagai precedent existing.
```

**Naming convention enforcement (REST-06 input validation):**

Per CONTEXT NAMING-SPEC, `[RegularExpression(...)]` attribute di `CreateAssessmentViewModel.Title`:
```csharp
[RegularExpression(
    @"^(Pre|Post) Test .+ (di Unit .+ RU [IVX]+ \w+|\w+)$",
    ErrorMessage = "Format judul harus: '{Pre|Post} Test {Track} {Lokasi}' (contoh: 'Pre Test OJT Pekerja GAST di Unit SRU dan GTO RU IV Cilacap')")]
public string Title { get; set; }
```

**Threats:** T-338-04 (script secret — mitigation Windows Auth no password param), T-338-06 (auto-pair orphan — manual assign default).

**UAT criteria:**
- AC1: Template render produces handoff doc dengan placeholders substituted
- AC2: Backup script jalankan di lokal dev machine dengan SQL Server Express → `.bak` file created
- AC3: Create assessment dropdown "Pair with..." show candidates dari last 6 month
- AC4: Title violate regex → ModelState invalid, error message user-friendly
- AC5: Title comply regex → save success
- AC6: DEV_WORKFLOW.md Section 7 hyperlink to template + script verify

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK | All | ✓ assumed (existing project) | net8.0 | — |
| ClosedXML | CIL-05 | ✓ | 0.105.0 | — |
| QuestPDF | CIL-06 | ✓ | 2026.2.2 | — |
| SkiaSharp | CIL-06 spider PNG | ✓ | 3.116.1 | — |
| SkiaSharp.NativeAssets.Win32 | CIL-06 runtime | ✓ | 3.116.1 | — |
| `System.IO.Compression` | CIL-06 ZIP | ✓ built-in net8.0 | — | — |
| SQL Server (Dev) | REST-05 backup | ✓ assumed (HcPortalDB_Dev exists) | 2019+ assumed | — |
| `Invoke-Sqlcmd` PowerShell | REST-05 script | ✗ uncertain (SqlServer PS module optional) | — | `sqlcmd.exe` CLI always shipped with SQL Server install |
| Markdown renderer (pandoc) | REST-05 template HTML output | ✗ uncertain | — | Plain Markdown enough untuk MVP; HTML render manual atau skip |

**Missing dependencies with no blocker:** None — semua hal critical sudah available.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (HcPortal.Tests sibling project — bootstrap Phase 325-01) + Playwright (tests/e2e) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/playwright.config.ts` |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo --verbosity quiet` |
| Full suite command | Quick + `cd tests && npx playwright test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CIL-01 | Badge counter Closed render | manual (Playwright optional) | open browser → manage assessment → verify badge | ❌ Wave 0 (manual) |
| CIL-02 | Search "Cilacap" return Closed groups | manual + Playwright | search input + assert row count > 0 | ❌ Wave 0 (Playwright optional) |
| CIL-03 | Row clickable + Actions kolom | manual + Playwright | click row → URL /CMP/Results/{id} | ❌ Wave 0 |
| CIL-04 | Banner visible admin/HC | manual | login as admin, navigate /CMP/Assessment | ❌ manual only |
| CIL-05 | 2 sheet aggregate exist + content match | manual | download Excel, open MS Excel, verify | ❌ manual only |
| CIL-06 | ZIP returned + N PDF entries | manual + curl | curl endpoint, unzip, ls | ❌ Wave 0 (Playwright optional) |
| REST-04 | 13 row inserted | dotnet test (integration if seeded) OR manual SQL query | `SELECT COUNT(*) FROM AssessmentSessions WHERE Title LIKE 'Pre Test...'` | ❌ manual SQL |
| REST-05 | Script runs, .bak created | manual (need SQL Server lokal) | run script, verify file | ❌ manual only |
| REST-06 | Title validation works | unit test (xUnit) OR manual | `dotnet test` regex validator | ❌ Wave 0 (unit optional) |
| REST-07 | DEV_WORKFLOW.md Section 7 exists | manual review | git diff verify | — |

### Sampling Rate
- **Per task commit:** `dotnet build` (warning-free) + `dotnet run` smoke
- **Per wave merge:** Per CLAUDE.md Step 4 verify lokal (Playwright optional)
- **Phase gate:** All 5 plan SC PASS browser-verified (proven Phase 327 pattern)

### Wave 0 Gaps
- Playwright spec optional: `tests/e2e/cilacap-bundle.spec.ts` covering CIL-01..04 (low priority, manual sufficient)
- Backup script smoke test: jalankan di lokal dev with SQLEXPRESS sebelum mark REST-05 SC PASS

---

## Project Constraints (from CLAUDE.md)

1. **Always respond in Bahasa Indonesia** — All plan + commit msg + UI string in Bahasa Indonesia
2. **Develop Workflow strict:** Lokal → Dev → Prod, never edit directly Dev/Prod
3. **Migration WAJIB EF Core** — No manual ALTER. REST-06 input validation tidak butuh migration (regex di ViewModel). CIL-03 `AllWorkersHistoryRow.SessionId` add field = no migration (projection-only model)
4. **Pre-commit checklist** per DEV_WORKFLOW.md §5: `dotnet build` warning-free + `dotnet run` manual verify localhost:5277 + golden path + edge case + migration commit kalau ada
5. **IT notify** dengan commit hash + flag migration — REST-04 execute LOKAL only first (NOT push to Dev until verified + IT acknowledged)
6. **Seed Workflow** untuk testing: klasifikasi + snapshot DB + journal entry + restore — relevant kalau Plan 04 task 1 butuh seed lokal untuk test REST-04 pre-flight (Excel parse + NIP lookup)

---

## State of the Art

| Old Approach | Current Approach | Source |
|--------------|------------------|--------|
| Per-peserta sheet built from scratch per request | Pre-loaded 4-query pattern + per-peserta loop | Phase 320 ExportAssessmentResults (v17.0) |
| Inline `onclick="window.location.href=..."` row | data-href + delegated click + keydown a11y | Phase 337-02 CMP-19 (v20.0) |
| Spider chart via Chart.js client only | SkiaSharp server-side PNG byte[] | Phase 320 SpiderChartRenderer (v17.0) |
| Status filter hardcoded exclude Closed | Status filter respect search context | Phase 338 CIL-02 fix (this phase) |
| Pre/Post pair manual via free-text Title parse | Explicit LinkedGroupId FK + admin UI dropdown | Phase 200 LinkedGroupId field + Phase 338 REST-06 (this phase) |

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Phase 320 ExportAssessmentResults eligible-session logic = same logic CIL-05 wants | DB Schema + Plan 03 | LOW — pattern proven, same eligibility = consistent |
| A2 | All 13 PreTest peserta NIP exist as User di DB | REST-04 trigger | MED — pre-check task 1 mitigates; kalau missing → block insert |
| A3 | LinkedGroupId PostTest counterpart sudah exist (sessionId 9-21 Dev) | REST-04 LinkedGroupId discovery | MED — query Task 2 confirm; kalau tidak ada, REST-04 insert PreTest standalone (LinkedGroupId null) acceptable |
| A4 | QuestPDF 2026.2.2 `.Image(byte[])` API exists | CIL-06 spider embed | LOW — `Image(byte[])` is standard QuestPDF.Fluent API, ada di docs |
| A5 | `Invoke-Sqlcmd` available di IT Windows machine | REST-05 script | MED — fallback `sqlcmd.exe` CLI shipped dengan SQL Server install |
| A6 | DEV_WORKFLOW.md Section 7 fit existing structure | REST-07 | LOW — file pendek, additive section OK |
| A7 | `CreateAssessment` POST handler line ~815 di AssessmentAdminController (per CONTEXT) | REST-06 LinkedGroupId UI | LOW — verify line saat execute Plan 05 |
| A8 | `AllWorkersHistoryRow` schema change non-breaking | CIL-03 | LOW — nullable add, EF projection-only model |
| A9 | Existing per-peserta sheet sudah memenuhi REQ EXP-03/04 user expectation, CIL-05 D-03/D-04 = aggregate komplemen | CIL-05 scope | MED — user discus ulang kalau ada interpretasi berbeda (lebih baik defer to Plan 03 task review) |
| A10 | 50 peserta hard limit untuk BulkExportPdf acceptable | CIL-06 T-338-02 | LOW — incident scope max 13 peserta, 50 = ~3x headroom |

---

## Open Questions

1. **CIL-05 sheet ordering** — Aggregate sheets sebelum per-peserta sheets ATAU sesudah?
   - What we know: Phase 320 spec sheet order = Summary, then per-peserta
   - What's unclear: User preference reading flow
   - Recommendation: Aggregate FIRST (Summary → Detail Per Soal → Elemen Teknis → per-peserta) — analyst discover aggregate first, drill-down second

2. **REST-04 trigger mechanism — script vs admin page?**
   - What we know: One-shot script enough untuk Cilacap restore
   - What's unclear: User want reusable tool untuk future loss?
   - Recommendation: Script only (REST-04 scope). Admin page = backlog kalau ada incident lain

3. **REST-05 template rendering — pandoc vs plain MD?**
   - What we know: 2 existing handoff doc adalah HTML hand-written
   - What's unclear: IT consume HTML or MD natively?
   - Recommendation: Plain Markdown template enough (IT email client render OK). HTML render optional bonus

4. **CIL-04 banner persistence — session vs cookie?**
   - CONTEXT defer cookie remember dismissal
   - Recommendation: Bootstrap default `data-bs-dismiss="alert"` session-only OK

5. **REST-06 backward audit tool — implement Plan 05 ATAU defer ke v21.0?**
   - CONTEXT mention `/Admin/NamingConventionAudit` page tapi NAMING-SPEC defer OQ-336-4
   - Recommendation: DEFER, Plan 05 cuma new-only enforce (regex validator), backward audit kalau user request lanjutan

---

## Next Steps untuk Planner

1. **Read this RESEARCH.md fully** + CONTEXT.md + 336-RESTORE-DECISION.md + 336-NAMING-CONVENTION-SPEC.md before generating 5 plans
2. **Generate 5 PLAN files** sesuai split D-07:
   - `338-01-PLAN.md` (CIL-01 + CIL-02) — ~80 LOC, 2 task + UAT
   - `338-02-PLAN.md` (CIL-03 + CIL-04) — ~120 LOC, 3 task (model + service + view + banner)
   - `338-03-PLAN.md` (CIL-05) — ~150 LOC, 2-3 task (2 sheet implementations)
   - `338-04-PLAN.md` (REST-04 + CIL-06) — ~400 LOC, 6 task (BulkBackfill endpoint + ViewModel + restore script + BulkExportPdf endpoint + ZIP + UAT)
   - `338-05-PLAN.md` (REST-05 + REST-06 + REST-07) — ~250 LOC + dokumentasi, 5 task (template + script + DEV_WORKFLOW edit + LinkedGroupId UI + regex validator)
3. **Confirm Plan 03 sheet ordering** dengan user (OQ-1 above) — kalau aggregate first preference confirmed, lock di plan
4. **Confirm REST-04 trigger as script-only** (OQ-2) — kalau yes, admin page defer
5. **Optional discus phase tambahan** kalau user mau verify A2 (13 NIP mapping) atau A3 (LinkedGroupId PostTest) sebelum lock REST-04 implementation
6. **Plan 05 SOP integration with IT** — coordinate dengan user kalau ada specific IT process requirement (email format, channel notification) sebelum lock REST-05 template content

**Critical reminders for planner:**
- All commit messages + UI strings + comments **Bahasa Indonesia** per CLAUDE.md
- DEV_WORKFLOW.md per-task checklist apply (build + run + Playwright optional + commit + IT notify if migration)
- Phase 338 = bundle 10 REQ — keep wave boundary clean, plan-by-plan UAT-able (per Phase 327 proven pattern)
- Verify code via `dotnet build` warning-free + `dotnet run` localhost:5277 manual smoke SETIAP task selesai

---

## Sources

### Primary (HIGH confidence — direct codebase read)
- `Helpers/SpiderChartRenderer.cs` — full file read (verifikasi RenderRadarPng API)
- `Helpers/ExcelExportHelper.cs` — full file (CreateSheet + ToFileResult helpers)
- `Models/PackageUserResponse.cs` — full file (schema)
- `Models/SessionElemenTeknisScore.cs` — full file (schema)
- `Models/AssessmentSession.cs` — full file (fields available REST-04)
- `Models/AssessmentResultsViewModel.cs` — full file (ViewModel ref)
- `Models/AllWorkersHistoryRow.cs` — full file (CIL-03 schema add needed)
- `Models/CreateManualAssessmentViewModel.cs` — full file (gap: no AssessmentType/LinkedGroupId)
- `Controllers/AssessmentAdminController.cs` — L59-298 (ManageAssessment+Tab actions), L4077-4444 (ExportAssessmentResults), L555-613 (QuestPDF pattern)
- `Controllers/TrainingAdminController.cs` — L620-710 (AddManualAssessment GET+POST), L711-802 (Edit), L803-840 (Delete partial)
- `Controllers/CMPController.cs` — L180-299 (Assessment action — CIL-04 banner target)
- `Views/Admin/Shared/_HistoryTab.cshtml` — L1-140 (CIL-03 modification target)
- `HcPortal.csproj` — L1-32 (package versions: ClosedXML 0.105.0, QuestPDF 2026.2.2, SkiaSharp 3.116.1)
- `Program.cs` — L6-8 (QuestPDF Community license init)
- `docs/DB_HANDOFF_IT_2026-05-26.html` — L502-625 (BACKUP+RESTORE SQL precedent)
- `docs/DEV_WORKFLOW.md` — L1-143 (full SOP)
- `CLAUDE.md` — full (Bahasa Indonesia + Develop Workflow + Seed Workflow)
- `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-RESTORE-DECISION.md` — full
- `.planning/phases/336-investigate-pretest-loss-cilacap-restore-strategy/336-NAMING-CONVENTION-SPEC.md` — full
- `.planning/phases/337-cmp-records-full-overhaul-filter-data-arch-a11y/337-02-PLAN.md` — CMP-19 pattern grep
- `.planning/phases/338-cilacap-ux-gap-bundle-excel-bulkpdf-restore-execute/338-CONTEXT.md` — full
- `.planning/REQUIREMENTS.md` — full
- `.planning/todos/pending/001-gap-ux-assessment-monitoring.md` — full
- `.planning/todos/pending/002-restore-pretest-ojt-gast-cilacap.md` — full
- `.planning/phases/320-assessment-export-per-peserta-excel/320-RESEARCH.md` — partial (Task 1-2 SkiaSharp setup)

### Secondary (MEDIUM confidence — cross-reference)
- Existing SkiaSharp 3.116.1 PNG render proven di Phase 320 production
- QuestPDF 2026.2.2 `Document.Create + page.Content + GeneratePdf(stream)` pattern proven L555-613 ExportCategoriesPdf + 2 CDPController endpoints
- ClosedXML 0.105.0 `XLWorkbook + AddPicture + SaveAs(stream)` pattern proven L4152-4443

### Tertiary (LOW confidence — assumptions needing verify at execute time)
- `Invoke-Sqlcmd` PowerShell module availability di IT machine (assumption — fallback sqlcmd.exe)
- `CreateAssessment` POST handler exact line ~815 (CONTEXT mention, not verified in research)
- 13 PreTest Cilacap NIP all map to existing User records (Task 1 pre-check Plan 04)
- LinkedGroupId PostTest Cilacap counterpart Group exists with sessionId 9-21 (Task 2 query Plan 04)

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages verified via HcPortal.csproj + Program.cs
- Architecture patterns: HIGH — proven existing patterns Phase 320 + 337 cited line numbers
- DB schema: HIGH — model files read end-to-end
- CIL-02 root cause: HIGH — code line 198-201 explicit identified
- CIL-05 scope reinterpretation: MEDIUM — assumes "+2 sheet aggregate" not "+2 sheet replace existing" (Assumption A9)
- REST-04 mechanism: MEDIUM — ViewModel gap identified but new endpoint approach pending user buy-in
- REST-05 implementation: MEDIUM — depends on IT environment (Invoke-Sqlcmd availability)
- Spider rendering OQ-1: HIGH — codebase artifact exists, no decision needed
- N+1 OQ-3: HIGH — proven optimization pattern Phase 320

**Research date:** 2026-05-30
**Valid until:** 2026-06-29 (30 days for stable stack; revisit if QuestPDF/SkiaSharp/ClosedXML major version bumps)
