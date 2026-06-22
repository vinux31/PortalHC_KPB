# Phase 406: Admin Config UI + Riwayat HC - Research

**Researched:** 2026-06-21
**Domain:** ASP.NET Core MVC Razor views + Bootstrap 5.3 (config card + riwayat modal) — UI-only phase, 0 migration. Backend (405) DONE.
**Confidence:** HIGH

## Summary

Phase 406 menambah DUA surface UI di atas backend ujian-ulang yang sudah jadi di Phase 405: (1) **card "Ujian Ulang"** di `ManagePackages.cshtml` (mirror card shuffle) + binding form di `CreateAssessment`/`EditAssessment`, dan (2) **modal Riwayat Percobaan HC** di `AssessmentMonitoringDetail.cshtml` (accordion per-attempt + tabel per-soal). Semua data backend SUDAH siap: ViewBag retake di `ManagePackages` action (`AssessmentAdminController.cs:5702-5711`), endpoint POST `UpdateRetakeSettings:5564`, helper pure `RetakeArchiveBuilder.Build` (Helpers/RetakeArchiveBuilder.cs), dan tabel arsip `AssessmentAttemptHistory`/`AssessmentAttemptResponseArchive`. [VERIFIED: codebase grep]

**Keputusan kunci yang ter-resolve oleh riset ini:**
- **Riwayat data-query:** rekomendasi **AJAX endpoint baru** (GET `RiwayatPercobaan(sessionId)` return `PartialView`), BUKAN pre-render ViewBag-all. Alasan: pre-render-all akan menjalankan N query per-pekerja saat page load (mahal di grup besar 30-100 peserta), padahal HC hanya buka 1-2 riwayat. Pola identik dengan `EditHistoryPartial` (`:3252`) yang sudah lazy-load via `fetch().then(text)→innerHTML` di view (`:986-1001`). [VERIFIED: codebase]
- **Current-attempt per-soal:** attempt SAAT INI belum di-arsip (archive hanya saat retake/reset). Sumber = **live `PackageQuestion`+`PackageUserResponse` di-pipe ke `RetakeArchiveBuilder.Build(0, questions, responses)`** → menghasilkan `List<AssessmentAttemptResponseArchive>` ber-shape IDENTIK dengan arsip → satu render-path untuk current+archived. Ini reuse builder 405 (kill-drift, verdict via `AssessmentScoreAggregator.IsQuestionCorrect`). [VERIFIED: Helpers/RetakeArchiveBuilder.cs:23, Services/RetakeService.cs:128-168]
- **Binding Create/Edit:** kedua view `@model HcPortal.Models.AssessmentSession` LANGSUNG (bukan VM) — `asp-for="AllowRetake/MaxAttempts/RetakeCooldownHours"` native, field + `[Range]` sudah ada di model. [VERIFIED: CreateAssessment.cshtml:1, EditAssessment.cshtml:1, Models/AssessmentSession.cs:44-54]

**Primary recommendation:** Implement card via mirror shuffle markup (UI-SPEC contract verbatim); implement riwayat via new lazy-AJAX `PartialView` endpoint that builds a unified DTO list (current via `RetakeArchiveBuilder.Build`, archived via query `AssessmentAttemptResponseArchives` grouped by `AttemptHistoryId`). Verify ALL Razor at runtime via Playwright @5270 (Lesson Phase 354). Extract a pure DTO unifier helper for xUnit coverage.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Retake config card render | Frontend Server (Razor) | — | ViewBag pre-populated server-side (`ManagePackages` action); pure server-rendered conditional markup |
| Retake config save | API/Backend (`UpdateRetakeSettings`) | — | Existing POST endpoint, RBAC+AntiForgery+clamp+propagation — DONE in 405 |
| Create/Edit binding | Frontend Server (Razor `asp-for`) | API (model-bind on POST) | Native MVC model binding to `AssessmentSession`; EF-default covers create path (RTK-01) |
| Progressive disclosure (show/hide fields) | Browser (vanilla JS) | — | Pure client UX; inputs always in DOM, server clamps regardless |
| Riwayat data fetch | API/Backend (new GET partial) | Database (archive + live responses) | Lazy per-worker query; avoids loading all riwayat on monitoring page load |
| Riwayat per-soal verdict | API/Backend (`RetakeArchiveBuilder`+`AssessmentScoreAggregator`) | — | Verdict computed server-side (frozen for archive, live for current); never client |
| Riwayat modal render | Browser (Bootstrap modal/accordion) + Frontend Server (PartialView HTML) | — | Bootstrap manages modal/accordion behavior; PartialView is `@`-encoded server HTML (XSS-safe) |

## Standard Stack

This is a brownfield phase — NO new dependencies. Everything used is already loaded.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Bootstrap | 5.3.0 | card, modal, accordion, form-switch, alert, badge, table | `_Layout.cshtml:38,246` — app-wide [VERIFIED] |
| Bootstrap Icons | 1.10.0 | `bi bi-*` (arrow-repeat, save, clock-history, check/x-circle-fill) | `_Layout.cshtml:39` [VERIFIED] |
| Razor | .NET 8 | `.cshtml` views, `@`-default HTML encoding | all Admin views [VERIFIED] |
| EF Core | 8.0.0 | query archive + live responses (controller side) | existing data access [VERIFIED] |

### Supporting (already present)
| Asset | Location | Purpose |
|-------|----------|---------|
| `RetakeArchiveBuilder.Build` | `Helpers/RetakeArchiveBuilder.cs:23` | pure DTO builder current-attempt per-soal (reuse for current attempt) |
| `AssessmentScoreAggregator.IsQuestionCorrect` | `Helpers/AssessmentScoreAggregator.cs:73` | verdict bool? (true/false/null=pending) |
| `AssessmentScoreAggregator.BuildAnswerCell` | `Helpers/AssessmentScoreAggregator.cs:110` | MC/MA answer display string |
| `RetakeRules.ShouldHideRetakeToggle` | `Helpers/RetakeRules.cs:59` | hide card for PreTest/Manual (already wired to `ViewBag.HideRetakeToggle:5706`) |
| `appUrl(path)` JS helper | `_Layout.cshtml:55` | PathBase-aware URL (`/KPB-PortalHC` sub-path on Dev) — MANDATORY for any fetch |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| AJAX PartialView for riwayat | Pre-render ViewBag dict (like EssayGradingMap) | Pre-render runs N×(query archive + query live) per worker at page load → heavy for 30-100-person groups; HC opens 1-2 riwayat only. AJAX = lazy, mirrors `EditHistoryPartial`. **Choose AJAX.** |
| AJAX returning PartialView HTML | AJAX returning JSON + client DOM build | JSON requires client-side `.textContent` assembly of accordion+table (verbose, XSS-risk if `innerHTML` misused). PartialView = server `@`-encoded HTML, drop into `innerHTML` safely. **Choose PartialView.** |
| `asp-for` direct on `AssessmentSession` | New ViewModel with 3 fields | Both Create/Edit ALREADY bind `AssessmentSession`; fields + `[Range]` exist. VM = needless mapping. **Choose asp-for direct.** |

**Installation:** None — zero new packages.

## Architecture Patterns

### System Architecture Diagram

```
SURFACE 1 — Retake Config Card
══════════════════════════════
[HC opens /Admin/ManagePackages?assessmentId=N]
        │
        ▼
ManagePackages GET action (:5624) ──sets──▶ ViewBag.{AllowRetake, MaxAttempts,
        │                                     RetakeCooldownHours, HideRetakeToggle,
        │                                     RetakeMaxAttemptsUsedInGroup, AssessmentId}  (:5702-5711)
        ▼
ManagePackages.cshtml renders:
  @if (ViewBag.HideRetakeToggle != true)  ──▶ card AFTER shuffle card (~:132)
     ├─ toggle #allowRetake (form-switch)
     ├─ #retakeFields (d-none when off) ──▶ JS toggles on change
     │    ├─ #maxAttempts (number 1-5)
     │    │    └─ @if (MaxAttempts < RetakeMaxAttemptsUsedInGroup) → alert-warning (non-blocking)
     │    └─ #retakeCooldownHours (number 0-168)
     └─ <button>Simpan Pengaturan</button>
        │ POST (AntiForgery)
        ▼
UpdateRetakeSettings (:5564) ──▶ clamp + sibling-propagate + audit + PRG ──▶ redirect ManagePackages

SURFACE 1b — Create/Edit binding
════════════════════════════════
CreateAssessment.cshtml (Step3, after shuffle col :551) / EditAssessment.cshtml (near :391)
  @model AssessmentSession ──▶ asp-for="AllowRetake/MaxAttempts/RetakeCooldownHours"
        │ native model-bind on wizard/edit POST ──▶ EF default covers create (RTK-01)

SURFACE 2 — Riwayat HC Modal
════════════════════════════
[HC on /Admin/AssessmentMonitoringDetail] clicks dropdown "Riwayat Percobaan" (data-session-id)
        │ JS: set modal title, fetch(appUrl('/Admin/RiwayatPercobaan?sessionId='+id))
        ▼
RiwayatPercobaan GET (NEW) ──┬─ ARCHIVED: query AssessmentAttemptResponseArchives
        │ [Authorize Admin,HC]│            join AssessmentAttemptHistory (UserId,Title,Category)
        │                     │            group by AttemptHistoryId
        │                     └─ CURRENT: load live PackageQuestions+PackageUserResponses
        │                                 → RetakeArchiveBuilder.Build(0, q, r)  (same DTO shape)
        ▼
returns PartialView("_RiwayatPercobaan", List<RiwayatAttemptVM>)  ── @-encoded ──▶ innerHTML
        ▼
Bootstrap accordion (one item/attempt, newest first, mark current) + per-soal table
```

### Recommended Project Structure (new/changed files)
```
Controllers/AssessmentAdminController.cs   # +RiwayatPercobaan GET action (near :3475, after AssessmentMonitoringDetail)
Helpers/RiwayatUnifier.cs                  # NEW pure helper: unify current(live)+archived → ordered List<RiwayatAttemptVM>
Models/RiwayatAttemptViewModel.cs          # NEW VM: AttemptNumber, ScorePercent, IsPassed, CompletedAt, IsCurrent, Rows
Views/Admin/_RiwayatPercobaan.cshtml       # NEW PartialView: accordion + per-soal table (@-encoded)
Views/Admin/AssessmentMonitoringDetail.cshtml  # +modal shell + dropdown trigger + fetch JS
Views/Admin/ManagePackages.cshtml          # +retake card after shuffle card (~:132) + disclosure JS
Views/Admin/CreateAssessment.cshtml        # +retake column in Step3 (after :551)
Views/Admin/EditAssessment.cshtml          # +retake fields near :391
HcPortal.Tests/RiwayatUnifierTests.cs      # NEW xUnit for the pure unifier
tests/e2e/retake-config-406.spec.ts        # NEW Playwright (mirror shuffle.spec.ts)
tests/e2e/riwayat-hc-406.spec.ts           # NEW Playwright (mirror essay-grading-384.spec.ts)
```

### Pattern 1: Config card mirror (shuffle card)
**What:** Copy shuffle card structure (`ManagePackages.cshtml:89-131`), drop lock-logic, add 2 number inputs + non-blocking warning.
**When to use:** Surface 1 card.
**Example (verbatim from UI-SPEC §Component Contract 1 — already approved):**
```cshtml
@* Source: 406-UI-SPEC.md §1, mirrors ManagePackages.cshtml:89-131 *@
@if (ViewBag.HideRetakeToggle != true)
{
    bool arChecked = ViewBag.AllowRetake == true;
    <div class="card mb-4">
        <div class="card-header bg-light">
            <h5 class="mb-0"><i class="bi bi-arrow-repeat text-primary me-1"></i>Ujian Ulang</h5>
        </div>
        <div class="card-body">
            <form method="post" asp-action="UpdateRetakeSettings" asp-controller="AssessmentAdmin">
                @Html.AntiForgeryToken()
                <input type="hidden" name="assessmentId" value="@ViewBag.AssessmentId" />
                <div class="form-check form-switch mb-2">
                    <input class="form-check-input" type="checkbox" name="allowRetake" id="allowRetake"
                           value="true" @(arChecked ? "checked" : "") />
                    <label class="form-check-label" for="allowRetake">Izinkan Ujian Ulang</label>
                </div>
                <div class="form-text text-muted mb-3">Saat aktif, peserta yang gagal boleh mengulang ujian ini secara mandiri (di bawah batas percobaan dan setelah masa jeda).</div>
                <div id="retakeFields" class="@(arChecked ? "" : "d-none")">
                    <div class="mb-3" style="max-width: 320px;">
                        <label for="maxAttempts" class="form-label fw-bold">Maksimal Percobaan</label>
                        <input type="number" class="form-control" name="maxAttempts" id="maxAttempts"
                               min="1" max="5" value="@(ViewBag.MaxAttempts ?? 2)" />
                        <div class="form-text text-muted">Berapa kali peserta boleh mencoba (termasuk percobaan pertama). Rentang 1–5.</div>
                        @if ((int)(ViewBag.MaxAttempts ?? 2) < (int)(ViewBag.RetakeMaxAttemptsUsedInGroup ?? 0))
                        {
                            <div class="alert alert-warning d-flex align-items-start mt-2 mb-0" role="alert">
                                <i class="bi bi-exclamation-triangle me-2 mt-1"></i>
                                <div>Maksimal percobaan yang Anda set lebih kecil dari jumlah percobaan yang sudah dipakai sebagian peserta. ... Pengaturan tetap bisa disimpan.</div>
                            </div>
                        }
                    </div>
                    <div class="mb-3" style="max-width: 320px;">
                        <label for="retakeCooldownHours" class="form-label fw-bold">Jeda Ujian Ulang (jam)</label>
                        <input type="number" class="form-control" name="retakeCooldownHours" id="retakeCooldownHours"
                               min="0" max="168" value="@(ViewBag.RetakeCooldownHours ?? 24)" />
                        <div class="form-text text-muted">Jeda minimal sebelum boleh mencoba lagi. 0 = tanpa jeda. Satuan: jam (maksimal 168 = 7 hari).</div>
                    </div>
                </div>
                <button type="submit" class="btn btn-primary"><i class="bi bi-save me-1"></i>Simpan Pengaturan</button>
            </form>
        </div>
    </div>
}
```
> NOTE the `?? 2` / `?? 0` null-coalesce on the warning cast — see Pitfall 1.

### Pattern 2: Progressive disclosure JS (mirror toggle pattern)
**What:** vanilla JS show/hide `#retakeFields` on `#allowRetake` change. Inputs stay in DOM (always submitted; server clamps).
**Example:**
```javascript
// Source: pattern mirrors existing toggle JS in views; inline <script> at end of view
document.getElementById('allowRetake')?.addEventListener('change', function () {
    document.getElementById('retakeFields')?.classList.toggle('d-none', !this.checked);
});
```
For Create/Edit, key the same disclosure on `asp-for="AllowRetake"` (rendered `id="AllowRetake"`).

### Pattern 3: Lazy-AJAX modal body (mirror EditHistoryPartial)
**What:** trigger sets title + fetches a PartialView, drops HTML into modal body.
**Example (mirror `AssessmentMonitoringDetail.cshtml:986-1001` + `:1144`):**
```javascript
// Source: mirrors EditHistoryPartial lazy-load :986-1001 + appUrl :55
document.addEventListener('click', function (e) {
    var btn = e.target.closest('.btn-riwayat-percobaan');
    if (!btn) return;
    var sid = btn.getAttribute('data-session-id');
    var wname = btn.getAttribute('data-worker-name') || '';
    document.getElementById('riwayatModalLabel').textContent = 'Riwayat Percobaan — ' + wname; // .textContent = XSS-safe
    var body = document.getElementById('riwayatBody');
    body.innerHTML = '<div class="text-center py-3"><span class="spinner-border spinner-border-sm me-2"></span>Memuat riwayat...</div>';
    new bootstrap.Modal(document.getElementById('riwayatPercobaanModal')).show();
    fetch(appUrl('/Admin/RiwayatPercobaan?sessionId=' + encodeURIComponent(sid)))
        .then(function (r) { if (!r.ok) throw new Error('HTTP ' + r.status); return r.text(); })
        .then(function (html) { body.innerHTML = html; })   // server-rendered @-encoded HTML
        .catch(function () { body.innerHTML = '<div class="alert alert-warning mb-0">Gagal memuat riwayat. Coba lagi.</div>'; });
});
```

### Anti-Patterns to Avoid
- **`@Html.Raw(QuestionText/AnswerText)`** — these are user-content; use `@` default-encode (Razor) or `.textContent` (JS). NEVER `Html.Raw`/`innerHTML`+raw-string. (D-02)
- **Pre-render ViewBag of ALL workers' riwayat** in `AssessmentMonitoringDetail` action — runs N queries at page load; use lazy AJAX.
- **Re-grading inline** in the riwayat view — verdict must come from `AssessmentScoreAggregator.IsQuestionCorrect` (via `RetakeArchiveBuilder` for current, frozen `IsCorrect` for archived). Inline re-grade diverges from Results/PDF/Excel.
- **Adding `disabled`/lock to the retake card** — D-03 = no-lock (unlike shuffle). Card editable anytime.
- **Raw `Url.Action`/string URL in fetch** without `appUrl()` — breaks on Dev `/KPB-PortalHC` sub-path (Lesson Phase 385 PXF-01).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Current-attempt per-soal rows | New inline grading loop | `RetakeArchiveBuilder.Build(0, questions, responses)` | Identical DTO shape to archive; verdict via aggregator (kill-drift) — `Helpers/RetakeArchiveBuilder.cs:23` |
| Correct/wrong verdict | `opt.IsCorrect` inline check | `AssessmentScoreAggregator.IsQuestionCorrect` | Handles MC/MA/Essay + pending-null; single source of truth — `:73` |
| MC/MA answer display string | Manual option join | `AssessmentScoreAggregator.BuildAnswerCell` | Already used by archive builder, PDF, Excel — `:110` |
| Hide-for-PreTest/Manual | New conditional | `RetakeRules.ShouldHideRetakeToggle` (already → `ViewBag.HideRetakeToggle:5706`) | Pure helper, unit-tested — `Helpers/RetakeRules.cs:59` |
| Config save (clamp/propagate/audit/PRG) | New endpoint | `UpdateRetakeSettings:5564` | Done in 405 — card just POSTs to it |
| PathBase-aware fetch URL | Hardcoded `/Admin/...` | `appUrl('/Admin/...')` | `_Layout.cshtml:55` — Dev sub-path |
| Accordion/modal/form-switch behavior | Custom JS | Bootstrap 5.3 `data-bs-*` attributes | Focus trap, Esc, keyboard toggle handled |

**Key insight:** The 405 backend already extracted the two pure helpers (`RetakeArchiveBuilder`, `RetakeRules`) and the aggregator. Phase 406 is almost entirely VIEW + ONE controller data-method that REUSES these. The only new pure logic is unifying current+archived attempts into an ordered list — extract that into `RiwayatUnifier` for xUnit coverage.

## Runtime State Inventory

> Not a rename/refactor/migration phase. Section omitted (greenfield UI + read-only data-query; 0 migration confirmed by phase scope + RTK-05/08 = fase 406, migration was 405-only per REQUIREMENTS.md:10,18).

## Common Pitfalls

### Pitfall 1: ViewBag warning-cast NRE
**What goes wrong:** `(int)ViewBag.MaxAttempts < (int)ViewBag.RetakeMaxAttemptsUsedInGroup` throws `RuntimeBinderException`/NRE if either ViewBag is null (dynamic cast of null to value-type int).
**Why it happens:** UI-checker flagged this. Both ARE set non-null in `ManagePackages` action: `ViewBag.MaxAttempts = assessment.MaxAttempts` (`:5704`, non-null int default 2) and `ViewBag.RetakeMaxAttemptsUsedInGroup = retakeMaxArchivedForGroup + 1` (`:5711`, `FirstOrDefaultAsync` on `int` returns 0 → always ≥1, non-null). [VERIFIED: AssessmentAdminController.cs:5704,5708-5711]
**How to avoid:** Even though current code guarantees non-null, defensive-cast with `?? `: `(int)(ViewBag.MaxAttempts ?? 2) < (int)(ViewBag.RetakeMaxAttemptsUsedInGroup ?? 0)`. Costs nothing, immunizes against future ViewBag refactors. (Used in Pattern 1 example above.)
**Warning signs:** YSOD on ManagePackages render when retake card visible.

### Pitfall 2: Current attempt has NO archive rows
**What goes wrong:** Querying only `AssessmentAttemptResponseArchives` shows ZERO per-soal for the worker's CURRENT (latest) attempt — because archiving only happens at retake/reset, so the live in-progress/completed attempt isn't yet in the archive.
**Why it happens:** Archive is a snapshot frozen *before* `PackageUserResponse` delete (`RetakeArchiveBuilder` doc, `RetakeService.cs:125-171`). The current session's responses still live in `PackageUserResponse`/`UserPackageAssignment`.
**How to avoid:** Source current attempt from LIVE data exactly as RetakeService does (`:128-139`): load `UserPackageAssignment.GetShuffledQuestionIds()` → `PackageQuestions.Include(Options)` → `PackageUserResponses` for the session → `RetakeArchiveBuilder.Build(0, questions, responses)` (sentinel `attemptHistoryId=0`). This yields the same `List<AssessmentAttemptResponseArchive>` shape; mark it `IsCurrent=true`. Only build current if session is Completed/Failed (skip InProgress/NotStarted — no meaningful per-soal yet, or show empty-state).
**Warning signs:** "Percobaan saat ini" accordion shows "Tidak ada rincian jawaban".

### Pitfall 3: Joining archive to history — use AttemptHistoryId, not (UserId,Title)
**What goes wrong:** Per-soal rows attach to the wrong attempt if grouped by user/title instead of the FK.
**Why it happens:** `AssessmentAttemptResponseArchive.AttemptHistoryId` is the FK (`Models/AssessmentAttemptResponseArchive.cs:17`, cascade). `AssessmentAttemptHistory` carries `(UserId, Title, Category, AttemptNumber, Score, IsPassed, CompletedAt)`.
**How to avoid:** Query history rows for the worker by `(UserId, Title, Category)` (anti-conflate Pre/Post — same key as RetakeService `:146-148`), then load archive rows `WHERE AttemptHistoryId IN (...)` and group by `AttemptHistoryId`. Order attempts by `AttemptNumber DESC` (newest first per UI-SPEC). [VERIFIED: Services/RetakeService.cs:145-150]
```csharp
// per-worker query shape (controller)
var session = await _context.AssessmentSessions.Include(s => s.User)
    .FirstOrDefaultAsync(s => s.Id == sessionId);                       // current session
var histories = await _context.AssessmentAttemptHistory
    .Where(h => h.UserId == session.UserId
             && h.Title == session.Title && h.Category == session.Category)
    .OrderByDescending(h => h.AttemptNumber)
    .ToListAsync();
var histIds = histories.Select(h => h.Id).ToList();
var archiveRows = await _context.AssessmentAttemptResponseArchives
    .Where(a => histIds.Contains(a.AttemptHistoryId))
    .ToListAsync();                                                     // group in-memory by AttemptHistoryId
// current attempt rows (live) — only if completed:
List<AssessmentAttemptResponseArchive> currentRows = new();
if (session.Status == "Completed") {
    var assign = await _context.UserPackageAssignments.FirstOrDefaultAsync(a => a.AssessmentSessionId == sessionId);
    var qids = assign?.GetShuffledQuestionIds() ?? new List<int>();
    var qs   = await _context.PackageQuestions.Include(q => q.Options).Where(q => qids.Contains(q.Id)).ToListAsync();
    var resp = await _context.PackageUserResponses.Where(r => r.AssessmentSessionId == sessionId).ToListAsync();
    if (qs.Count > 0) currentRows = RetakeArchiveBuilder.Build(0, qs, resp);  // attemptHistoryId=0 sentinel
}
```

### Pitfall 4: Essay-pending IsCorrect == null display
**What goes wrong:** `bool? IsCorrect` is tri-state; treating null as false shows ✗ for an ungraded essay.
**Why it happens:** `IsQuestionCorrect` returns null for ungraded essay (`AssessmentScoreAggregator.cs:87`). Archive preserves null (`AssessmentAttemptResponseArchive.IsCorrect` is `bool?`).
**How to avoid:** Three-branch render (UI-SPEC §3): `== true` → check-circle; `== false` → x-circle; `else` (null) → muted `—` + `title="Menunggu penilaian"`. Never collapse null→false.
**Warning signs:** Ungraded essays show as wrong.

### Pitfall 5: XSS via Html.Raw on user content
**What goes wrong:** `QuestionText`/`AnswerText`/worker-name contain user input; `Html.Raw` or `innerHTML` with raw string = stored XSS.
**Why it happens:** D-02 explicitly forbids; QuestionText/AnswerText are author/worker-supplied.
**How to avoid:** PartialView path → Razor `@r.QuestionText` (auto HTML-encode). JS-set title → `.textContent` (not `.innerHTML`). The fetched PartialView HTML into `innerHTML` is SAFE because the server already `@`-encoded each field. NEVER `Html.Raw` on these. [CITED: 406-CONTEXT.md D-02]
**Warning signs:** A `<script>`-containing question/answer executes in the modal.

### Pitfall 6: Razor dynamic UI not verified at runtime
**What goes wrong:** `dotnet build` + grep pass but the card/modal silently fails to render (ViewBag-cast errors, missing element id, accordion not toggling) only at runtime.
**Why it happens:** Razor `@`-conditionals + dynamic ViewBag compile fine but throw/misbehave at request time. Lesson Phase 354: grep+build insufficient.
**How to avoid:** Mandatory Playwright runtime verify @5270 for BOTH surfaces (see Validation Architecture). [CITED: 406-CONTEXT.md code_context, MEMORY Phase 354]
**Warning signs:** Build green but page 500s or card absent in browser.

### Pitfall 7: appUrl missing on fetch → 404 on Dev
**What goes wrong:** `fetch('/Admin/RiwayatPercobaan...')` 404s on Dev because app is hosted under `/KPB-PortalHC`.
**Why it happens:** Dev uses PathBase sub-path (Lesson Phase 385 PXF-01).
**How to avoid:** Always `fetch(appUrl('/Admin/RiwayatPercobaan?...'))`. [VERIFIED: _Layout.cshtml:55, existing fetches :1144,:1013]

## Code Examples

### New controller action (riwayat data) — place after AssessmentMonitoringDetail (~:3475)
```csharp
// Source: mirrors EssayGrading single-session loader :3482 + EditHistoryPartial :3252 + RetakeService :128-168
[HttpGet]
[Authorize(Roles = "Admin, HC")]   // SAME RBAC as AssessmentMonitoringDetail :3290 [VERIFIED]
public async Task<IActionResult> RiwayatPercobaan(int sessionId)
{
    var session = await _context.AssessmentSessions.Include(s => s.User)
        .FirstOrDefaultAsync(s => s.Id == sessionId);
    if (session == null) return NotFound();
    // ... histories + archiveRows + currentRows (see Pitfall 3 query shape) ...
    var vm = RiwayatUnifier.Build(session, histories, archiveRows, currentRows);  // pure, xUnit-tested
    return PartialView("_RiwayatPercobaan", vm);   // @-encoded HTML body
}
```

### Pure unifier helper (extract for xUnit) — `Helpers/RiwayatUnifier.cs`
```csharp
// Pure (EF-free): merge current(live, marked IsCurrent) + archived(grouped by AttemptHistoryId),
// order by AttemptNumber DESC. ScorePercent from history.Score / current session.Score.
// No DB — caller supplies facts (mirror RetakeRules/RetakeArchiveBuilder purity pattern).
public static List<RiwayatAttemptViewModel> Build(
    AssessmentSession current,
    IEnumerable<AssessmentAttemptHistory> histories,
    IEnumerable<AssessmentAttemptResponseArchive> archiveRows,
    IEnumerable<AssessmentAttemptResponseArchive> currentRows) { /* ... */ }
```

### Riwayat PartialView per-soal cell (XSS-safe tri-state) — `_RiwayatPercobaan.cshtml`
```cshtml
@* Source: 406-UI-SPEC.md §3 — @-encoded, tri-state IsCorrect *@
<td>@r.QuestionText</td>
<td>@(string.IsNullOrEmpty(r.AnswerText) ? "—" : r.AnswerText)</td>
<td class="text-center">
    @if (r.IsCorrect == true)       { <i class="bi bi-check-circle-fill text-success" title="Benar"></i> }
    else if (r.IsCorrect == false)  { <i class="bi bi-x-circle-fill text-danger" title="Salah"></i> }
    else                            { <span class="text-muted" title="Menunggu penilaian">—</span> }
</td>
<td class="text-end">@r.AwardedScore</td>
```

### BS 5.3 accordion conventions (confirmed in-app at PlanIdp.cshtml:297-336)
```cshtml
@* data-bs-toggle="collapse" + data-bs-target="#id" + accordion-collapse collapse + data-bs-parent *@
<div class="accordion-item">
  <h2 class="accordion-header">
    <button class="accordion-button @(isFirst ? "" : "collapsed")" type="button"
            data-bs-toggle="collapse" data-bs-target="#att-@n" aria-expanded="@(isFirst)" aria-controls="att-@n">...</button>
  </h2>
  <div id="att-@n" class="accordion-collapse collapse @(isFirst ? "show" : "")" data-bs-parent="#riwayatAccordion">
    <div class="accordion-body p-0">...table...</div>
  </div>
</div>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `.planning/codebase/TESTING.md` says "No unit test framework / no xUnit" | `HcPortal.Tests/` xUnit suite (571/571) EXISTS and is the standard for pure helpers | grew since 2026-04-02 doc | TESTING.md is STALE on this; pure-helper coverage IS expected (RetakeArchiveBuilderTests, RetakeServiceTests present) [VERIFIED: HcPortal.Tests/*.cs] |

**Deprecated/outdated:**
- TESTING.md "Base URL: http://localhost:5277" — on branch ITHandoff the app + Playwright run @5270 (CLAUDE.md + MEMORY reference). Use 5270 for this phase's e2e.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Typical group size 30-100 peserta → pre-render-all riwayat is heavy → AJAX preferred | Riwayat data-query | LOW — if groups are tiny (<10), pre-render also fine; AJAX still correct/safe, just slightly more code |
| A2 | Current attempt per-soal only meaningful when `session.Status=="Completed"` | Pitfall 2/3 | LOW — InProgress attempts have partial responses; showing them is a design choice (UI-SPEC implies completed/archived focus). Confirm with discuss if HC wants live in-progress drill. |

**If this table is empty:** N/A — two low-risk assumptions logged.

## Open Questions

1. **Should the current (live) attempt appear in the riwayat modal at all, or only archived attempts?**
   - What we know: UI-SPEC §3 says "current = live `AssessmentSession`; archived = `AssessmentAttemptHistory`", implying current IS shown with "Percobaan saat ini" badge.
   - What's unclear: whether to build current per-soal for a Completed-but-not-yet-retaken session (yes, recommended) vs only archived.
   - Recommendation: SHOW current (Completed only) via `RetakeArchiveBuilder.Build(0,...)`, badge "Percobaan saat ini". Empty-state if not Completed.

2. **Trigger placement: dropdown item vs inline icon button?**
   - What we know: UI-SPEC §2 offers both; dropdown `<li>` before `:368` preferred, inline icon next to Activity Log (`:305`) acceptable.
   - Recommendation: dropdown `<li>` (consistent with Edit/Reset/Akhiri). Render for Completed OR has-archived (planner adds a small ViewBag flag or always-render + empty-state).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 8 SDK / `dotnet` | build/run | ✓ (project active) | 8.x | — |
| SQL Server (localhost\SQLEXPRESS, HcPortalDB_Dev) | live render + e2e | ✓ (per CLAUDE.md/MEMORY) | — | — |
| Playwright (`tests/`) | runtime UI verify @5270 | ✓ | v1.58.2 | — |
| xUnit (`HcPortal.Tests/`) | unifier unit test | ✓ | present | — |
| Bootstrap 5.3.0 / Icons 1.10.0 (CDN) | all markup | ✓ | 5.3.0 / 1.10.0 | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — all in place.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests/`) for pure helpers + Playwright v1.58.2 (`tests/e2e/`) for UI |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/playwright.config.ts` |
| Quick run command | `dotnet test HcPortal.Tests --filter FullyQualifiedName~RiwayatUnifier` (unit) |
| Full suite command | `dotnet test HcPortal.Tests` + `cd tests && npx playwright test retake-config-406 riwayat-hc-406` |

> NOTE: app + Playwright run @ **http://localhost:5270** on branch ITHandoff (override `dotnet run --urls`; Playwright baseURL). `Authentication__UseActiveDirectory=false` for local run. Auth: admin@pertamina.com / 123456. e2e use `--workers=1` (DB isolation) + DB snapshot/restore (CLAUDE.md SEED_WORKFLOW).

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| RTK-05 | Retake card renders when not hidden; toggle reveals number inputs | e2e (Playwright) | `npx playwright test retake-config-406 -g "card render"` | ❌ Wave 0 |
| RTK-05 | Card HIDDEN for Pre-Test / Manual entry | e2e | `npx playwright test retake-config-406 -g "hide"` | ❌ Wave 0 |
| RTK-05 | Save POST → PRG success + persisted values reflected on reload | e2e | `npx playwright test retake-config-406 -g "save"` | ❌ Wave 0 |
| RTK-05 | Non-blocking warning shows when MaxAttempts < used; Save still works | e2e | `npx playwright test retake-config-406 -g "warning"` | ❌ Wave 0 |
| RTK-05 | Create/Edit bind AllowRetake/MaxAttempts/RetakeCooldownHours | e2e | `npx playwright test retake-config-406 -g "binding"` | ❌ Wave 0 |
| RTK-08 | Dropdown "Riwayat Percobaan" opens modal; AJAX loads body | e2e | `npx playwright test riwayat-hc-406 -g "open"` | ❌ Wave 0 |
| RTK-08 | Accordion per-attempt + per-soal table renders (Q/answer/status/score) | e2e | `npx playwright test riwayat-hc-406 -g "per-soal"` | ❌ Wave 0 |
| RTK-08 | Current attempt marked "Percobaan saat ini"; archived listed newest-first | e2e | `npx playwright test riwayat-hc-406 -g "current"` | ❌ Wave 0 |
| RTK-08 | Essay-pending shows `—`/Menunggu (not ✗); XSS-encoded text | e2e | `npx playwright test riwayat-hc-406 -g "pending"` | ❌ Wave 0 |
| RTK-08 | `RiwayatUnifier.Build` orders DESC, marks IsCurrent, unifies current+archived | unit (xUnit) | `dotnet test --filter ~RiwayatUnifier` | ❌ Wave 0 |
| RTK-08 | `RiwayatPercobaan` action RBAC Admin/HC (manual-confirm via attribute) | static review | N/A (attribute audit) | manual |

### Sampling Rate
- **Per task commit:** `dotnet build` + `dotnet test --filter ~RiwayatUnifier` (fast, <30s for the pure helper).
- **Per wave merge:** full `dotnet test HcPortal.Tests` (regression on RetakeArchiveBuilder/RetakeService/etc.) + targeted `npx playwright test retake-config-406 riwayat-hc-406 --workers=1`.
- **Phase gate:** full xUnit green + both new Playwright specs green @5270 before `/gsd-verify-work`. Razor MUST be runtime-verified (Pitfall 6) — build+grep insufficient.

### Wave 0 Gaps
- [ ] `Helpers/RiwayatUnifier.cs` + `HcPortal.Tests/RiwayatUnifierTests.cs` — covers RTK-08 unification (current+archived ordering, IsCurrent, score%). Mirror `RetakeArchiveBuilderTests.cs` (no-DB pure pattern).
- [ ] `Models/RiwayatAttemptViewModel.cs` — DTO for the partial.
- [ ] `Views/Admin/_RiwayatPercobaan.cshtml` — accordion+table partial (@-encoded).
- [ ] `tests/e2e/retake-config-406.spec.ts` — mirror `shuffle.spec.ts` (DB snapshot/restore + wizard + render/save/hide/warning).
- [ ] `tests/e2e/riwayat-hc-406.spec.ts` — mirror `essay-grading-384.spec.ts` (monitoring-detail context, modal open, per-soal render). Will need seeded archived attempts (temp seed per CLAUDE.md SEED_WORKFLOW; restore after).
- [ ] (Framework already installed — no install step.)

*Manual-only items:* RBAC attribute audit on `RiwayatPercobaan` (static); full lifecycle retake browser UAT is Phase 408 (RTK-14) — 406 e2e scope = card+modal render/interaction, not the end-to-end retake flow.

## Security Domain

> `security_enforcement` config not located in this session; security gate is a dedicated downstream phase (RTK-14 / Phase 408 + `/gsd-secure-phase`). Documenting applicable controls for this UI phase.

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | Existing Identity; actions `[Authorize(Roles="Admin, HC")]` (mirror `:3290`, `:5565`) |
| V3 Session Management | no (no new session state) | — |
| V4 Access Control | yes | New `RiwayatPercobaan` GET MUST carry `[Authorize(Roles="Admin, HC")]` — same as `AssessmentMonitoringDetail` |
| V5 Input Validation | yes | `sessionId` int model-bind; server `Math.Clamp` (already in `UpdateRetakeSettings`); `min/max` on inputs are UX-only |
| V6 Cryptography | no | — |
| CSRF | yes | Card form `@Html.AntiForgeryToken()` + endpoint `[ValidateAntiForgeryToken]` (already on `UpdateRetakeSettings:5566`). GET riwayat = read-only, no AntiForgery needed |
| Output Encoding (XSS) | yes | `@`-encode QuestionText/AnswerText/worker-name; `.textContent` for JS title; NO `Html.Raw`/`innerHTML`+raw (D-02) |

### Known Threat Patterns for ASP.NET Core MVC + Razor
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Stored XSS via QuestionText/AnswerText in modal | Tampering/Info-disclosure | Razor `@` auto-encode in PartialView; `.textContent` for title (D-02) |
| Missing authz on new riwayat GET → answer-key/PII leak | Info disclosure | `[Authorize(Roles="Admin, HC")]` on `RiwayatPercobaan` |
| IDOR via `sessionId` param | Elevation | Admin/HC role already gates monitoring; (Phase 407 worker endpoint adds ownership check — out of scope here) |
| CSRF on config save | Tampering | AntiForgery already on `UpdateRetakeSettings` form+endpoint |
| Client bypass of min/max → out-of-range MaxAttempts | Tampering | Server `Math.Clamp(1,5)/(0,168)` in `UpdateRetakeSettings:5580-5581` (already) |

## Sources

### Primary (HIGH confidence)
- `Controllers/AssessmentAdminController.cs` — `:3290-3475` AssessmentMonitoringDetail (RBAC + EssayGradingMap build), `:5564-5611` UpdateRetakeSettings, `:5624-5738` ManagePackages (ViewBag retake `:5702-5711`), `:3252` EditHistoryPartial
- `Views/Admin/ManagePackages.cshtml:83-132` — shuffle card mirror
- `Views/Admin/AssessmentMonitoringDetail.cshtml` — per-peserta table `:241-372`, dropdown `:322-368`, modals `:481/:500/:539`, lazy-AJAX JS `:986-1001/:1144`, EssayGradingMap consume `:382-383`
- `Views/Admin/CreateAssessment.cshtml:1,536-551` + `EditAssessment.cshtml:1,384-413` — `@model AssessmentSession` + insertion points
- `Models/AssessmentSession.cs:44-54` — AllowRetake/MaxAttempts/RetakeCooldownHours + `[Range]`
- `Models/AssessmentAttemptHistory.cs` + `Models/AssessmentAttemptResponseArchive.cs` — riwayat data model
- `Helpers/RetakeArchiveBuilder.cs:23` + `Helpers/AssessmentScoreAggregator.cs:73,110` + `Helpers/RetakeRules.cs:59`
- `Services/RetakeService.cs:125-171` — live questions+responses load pattern (current-attempt source)
- `Data/ApplicationDbContext.cs:68,71` — DbSet names `AssessmentAttemptHistory` / `AssessmentAttemptResponseArchives`
- `Views/Shared/_Layout.cshtml:38,39,55,246` — Bootstrap 5.3.0/Icons 1.10.0, `appUrl` helper
- `Views/CDP/PlanIdp.cshtml:297-336` — in-app BS 5.3 accordion conventions
- `HcPortal.Tests/RetakeArchiveBuilderTests.cs` — pure-helper unit-test pattern to mirror
- `tests/e2e/shuffle.spec.ts` + `tests/e2e/essay-grading-384.spec.ts` — e2e mirror templates
- `.planning/phases/406-admin-config-ui-riwayat-hc/406-UI-SPEC.md` (APPROVED) + `406-CONTEXT.md`
- `.planning/REQUIREMENTS.md` (RTK-05, RTK-08)

### Secondary (MEDIUM confidence)
- `.planning/codebase/CONVENTIONS.md` (2026-04-02) — controller/view conventions (current)
- `.planning/codebase/TESTING.md` (2026-04-02) — STALE re: "no xUnit"; corrected by file-system evidence

### Tertiary (LOW confidence)
- None — all claims verified against this repo.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all assets verified by grep/read in this repo; zero new deps.
- Architecture (card + modal + data-query): HIGH — mirror lines + reuse helpers verified; AJAX-vs-prerender is a reasoned design choice (LOW-risk assumption A1).
- Pitfalls: HIGH — each traced to a verified file:line (ViewBag nullability, current-vs-archive split, tri-state IsCorrect, XSS rule, appUrl).

**Research date:** 2026-06-21
**Valid until:** 2026-07-21 (stable brownfield; only risk = unrelated refactor of ManagePackages ViewBag or RetakeArchiveBuilder signature)
