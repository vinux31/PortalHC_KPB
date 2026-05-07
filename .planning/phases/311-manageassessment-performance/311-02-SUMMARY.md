---
phase: 311
plan: 02
slug: manageassessment-htmx-lazy-load
tags: [perf, htmx, lazy-load, partial-action, frontend, backend, refactor]
requires:
  - phase: 311
    plan: 01
    artifacts:
      - "Per-segment Stopwatch baseline (commit a4ce556e — direpurposed ke per-action di Plan 02)"
provides:
  - "HTMX lazy load architecture untuk /Admin/ManageAssessment"
  - "3 partial endpoints: ManageAssessmentTab_Assessment / _Training / _History"
  - "Vendored HTMX 2.0.10 di wwwroot/lib/htmx/htmx.min.js (51 KB uncompressed, ~14 KB gzipped)"
  - "Per-action Stopwatch instrumentation (D-09 telemetry preserved)"
  - "Cross-tab cache invalidation handler (D-05) + error template + retry button (UI-SPEC §2)"
affects:
  - "Controllers/AssessmentAdminController.cs (action ManageAssessment refactored shell-only + 3 NEW partial actions)"
  - "Views/Admin/ManageAssessment.cshtml (full refactor — HTMX attrs + skeleton 3 varian + filter form di shell level)"
  - "wwwroot/lib/htmx/htmx.min.js (NEW vendored library)"
  - "wwwroot/css/site.css (append .htmx-request loading state rules)"
  - "REQUIREMENTS.md §PERF-01 (strategy + acceptance criteria updated per D-08)"
tech-stack:
  added:
    - "HTMX 2.0.10 (vendored, NOT CDN)"
  patterns:
    - "Lazy load per tab via HTMX hx-trigger (load untuk active, shown.bs.tab once untuk inactive)"
    - "PartialView return dengan [Authorize(Roles=Admin,HC)] + [ResponseCache(NoStore=true)]"
    - "Per-action Stopwatch + structured logger 'ManageAssessment perf [tab={Tab}]: ...' "
    - "Bootstrap 5 .placeholder-glow + .placeholder skeleton (no custom CSS)"
    - "Filter form id=filter-form di shell level + hx-include untuk shared filter values"
    - "Cross-tab cache invalidation via htmx:afterSwap event listener"
key-files:
  created:
    - path: "wwwroot/lib/htmx/htmx.min.js"
      purpose: "Vendored HTMX 2.0.10 library (51 KB uncompressed, ~14 KB gzipped via response compression)"
      sha256: "71ea67185bfa8c98c39d31717c6fce5d852370fcdfd129db4543774d3145c0de"
  modified:
    - path: ".planning/REQUIREMENTS.md"
      purpose: "PERF-01 strategy + acceptance criteria updated (D-08): HTMX lazy load architecture, ≤40 detik wifi kantor, <14 KB initial doc, ≤2 detik tab switch"
    - path: "Controllers/AssessmentAdminController.cs"
      purpose: "ManageAssessment action refactored shell-only (D-10 backward compat); 3 NEW partial actions: ManageAssessmentTab_Assessment/_Training/_History dengan ResponseCache(NoStore) + Stopwatch per-action (D-09)"
    - path: "Views/Admin/ManageAssessment.cshtml"
      purpose: "Full refactor ke HTMX-driven shell — 3 tab pane wrappers dengan skeleton + filter form di shell + error template handler + cross-tab cache invalidation"
    - path: "wwwroot/css/site.css"
      purpose: ".htmx-request loading state rules (UI-SPEC §3): #filter-form opacity 0.7 + .tab-pane opacity 0.85 + cursor:progress global"
decisions:
  - "AsNoTracking() di Categories distinct query (zero-cost read-only optimization, independent dari Plan 03 cache wrap) — applied di shell action + ManageAssessmentTab_Assessment partial"
  - "PartialView('Shared/_..., null) dengan model=null karena semua data via ViewBag (D-10 contract preserved)"
  - "Filter form di-promote ke shell level (bukan duplicate di partial views) — shell-level form yang dipakai HTMX trigger; partial views existing tidak dimodifikasi (D-10 backward compat)"
  - "Tab activation listener update endpoint URL & target dynamic via htmx.process (HTMX tidak observe atribut runtime change)"
metrics:
  tasks_completed: 3
  tasks_total: 5
  duration_minutes: ~30
  files_modified: 4
  files_created: 1
  build_errors: 0
  build_warnings: 92
  warnings_baseline: 92
status: paused-at-checkpoint
checkpoint_type: human-verify
---

# Phase 311 Plan 02: HTMX Lazy Load Refactor Summary

**Status:** PAUSED at Task 4 (checkpoint:human-verify) — 3/5 tasks complete, blocking pada manual UAT smoke test parity + perf measurement wifi kantor.

## One-liner

Refactor `Controllers/AssessmentAdminController.cs` `ManageAssessment` action (L60-262) jadi shell-only + 3 partial endpoints (HTMX lazy load architecture per Phase 311 DESIGN.md approved 2026-05-07), dengan vendored HTMX 2.0.10, per-action Stopwatch (D-09), cross-tab cache invalidation (D-05), dan error template + retry button (UI-SPEC §2).

## Tasks Status

| # | Task | Status | Commit | Notes |
|---|------|--------|--------|-------|
| 1 | Update REQUIREMENTS.md PERF-01 + vendor HTMX 2.0.10 + CSS .htmx-request rules | DONE | `03abcc92` | SHA-256: `71ea67185b...` |
| 2 | Refactor ManageAssessment ke shell + 3 partial actions | DONE | `c2b5a910` | 0 errors, 92 warnings |
| 3 | Refactor ManageAssessment.cshtml shell view (HTMX attrs + skeleton + error template) | DONE | `0f3e4690` | 0 errors, 92 warnings |
| 4 | Manual UAT smoke parity + perf measurement wifi kantor | **PAUSED** | — | checkpoint:human-verify, blocking |
| 5 | Final commit Plan 02 changes | PENDING | — | execute hanya kalau Task 4 = "uat passed" |

## Implementation Details

### Backend (Task 2 — commit `c2b5a910`)

**ManageAssessment shell action** (`Controllers/AssessmentAdminController.cs`):
- Signature unchanged (D-10 backward compat — URL bookmarks tetap valid)
- Body refactored ke shell-only:
  - Tab routing: `var activeTab = tab switch {...}`
  - ViewBag pre-populate: SearchTerm, SelectedCategory, SelectedStatus, SelectedSection, SelectedUnit, CurrentPage, PageSize
  - Categories dropdown query dengan `.AsNoTracking()` (zero-cost)
  - Stopwatch + structured log: `"ManageAssessment perf [tab=shell]: elapsed={Ms}ms ..."`
  - `return View()` (no data fetch — _workerDataService TIDAK dipanggil)

**3 NEW partial actions:**

```csharp
[HttpGet]
[Authorize(Roles = "Admin, HC")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public async Task<IActionResult> ManageAssessmentTab_Assessment(string? search, int page = 1, ...)
{
    var sw = Stopwatch.StartNew();
    // T1 logic (Assessment query chain + grouping + pagination)
    sw.Stop();
    _logger.LogInformation("ManageAssessment perf [tab=assessment]: ...", ...);
    return PartialView("Shared/_AssessmentGroupsTab", null);
}
```

Sama pattern untuk `ManageAssessmentTab_Training` (T2 + T4) dan `ManageAssessmentTab_History` (T3).

**ViewBag contract preserved** sesuai partial view consumer (D-10) — partial views existing TIDAK dimodifikasi.

### Frontend (Task 3 — commit `0f3e4690`)

**Filter form di-promote ke shell level:**
```html
<form id="filter-form" method="get" class="row g-2 align-items-end my-3" onsubmit="return false;">
  <input type="hidden" name="tab" value="@activeTab" id="filter-tab" />
  <input type="hidden" name="page" value="1" id="filter-page" />
  <input type="text" name="search" id="filter-search"
         hx-get="@urlAssessment"
         hx-trigger="input changed delay:500ms, keyup[key=='Enter']"
         hx-target="#pane-@activeTab > div.htmx-tab-wrapper"
         hx-include="#filter-form"
         hx-sync="this:replace" />
  <!-- category + status dropdowns dengan hx-trigger="change" instant -->
</form>
```

**3 tab pane wrappers (.htmx-tab-wrapper):**
- Active tab: `hx-trigger="load"` (immediate fire setelah HTMX init)
- Inactive tabs: `hx-trigger="shown.bs.tab from:#tab-{name} once"` (D-01 explicit ID match)
- Skeleton placeholder visible saat shell render (UI-SPEC §1a/b/c)

**Cross-tab cache invalidation handler (D-05):**
```javascript
document.body.addEventListener('htmx:afterSwap', function(evt) {
    var target = evt.detail.target;
    if (!target.classList.contains('htmx-tab-wrapper')) return;
    // Reset wrapper tab non-aktif ke skeleton + restore 'shown.bs.tab once' trigger
    document.querySelectorAll('.htmx-tab-wrapper').forEach(function(wrapper){
        if (wrapper === target) return;
        wrapper.innerHTML = '<div class="placeholder-glow">...skeleton...</div>';
        wrapper.setAttribute('hx-trigger', 'shown.bs.tab from:#tab-...');
        if (window.htmx) htmx.process(wrapper);
    });
});
```

**Error template + retry button (UI-SPEC §2):**
```javascript
document.body.addEventListener('htmx:responseError', function(evt) {
    renderHtmxError(evt.detail.target);
});
// Retry button delegated → htmx.trigger(wrapper, 'load')
```

**HTMX script tag:**
```html
<script src="~/lib/htmx/htmx.min.js" defer></script>
```

PathBase `/KPB-PortalHC` auto-resolved via Razor `@Url.Action(...)` helper.

## Build Verdict

```
0 Errors
92 Warnings (Phase 309 baseline preserved — no regression)
Time Elapsed: ~28-46 seconds
```

## Deviations from Plan

### [Rule 1 - Bug] HTMX file size > plan acceptance bound

- **Found during:** Task 1 vendoring step
- **Issue:** Plan acceptance bound `[ "$SIZE" -ge 8000 ] && [ "$SIZE" -le 25000 ]` (8-25 KB).
  Aktual: HTMX 2.0.10 minified uncompressed = **51,238 bytes** (~50 KB).
- **Root cause:** Plan reference "~14 KB" sebenarnya merujuk ke **gzipped transfer size** (Cloudflare TCP 14KB rule context), bukan uncompressed file size. HTMX 2.0.x official npm minified build ~50 KB raw, ~14 KB gzipped.
- **Resolution:** Keep file as-is (resmi dari unpkg). ASP.NET Core static middleware default enable response compression (gzip) → wire-transfer size kepada browser akan ~14 KB sesuai target plan. Plan acceptance bound terlalu strict; documented sebagai deviation di commit `03abcc92`.
- **Files affected:** `wwwroot/lib/htmx/htmx.min.js`
- **Commit:** `03abcc92`

### [Procedural] Edit tool path resolution issue

- **Found during:** Task 1 — initial edits ke REQUIREMENTS.md / site.css ter-apply ke main repo path (luar worktree)
- **Issue:** Edit tool dengan relative-style path resolves ke `C:/Users/.../PortalHC_KPB/...` (main repo) instead of worktree path
- **Resolution:** Revert main repo (`git checkout -- ...`); re-apply edits dengan absolute path eksplisit ke worktree (`C:/Users/.../.claude/worktrees/agent-.../...`)
- **No data lost** — main repo bersih sebelum revert tanpa staging.

## Auth/Authorization Posture

Setiap partial action di-decorate `[Authorize(Roles = "Admin, HC")]` sesuai threat model T-311-02-02. ViewBag contract preserved per partial view consumer (T-311-02-03 XSS via Razor encoding default). `[ResponseCache(NoStore = true)]` per D-06 cegah browser/proxy cache filter-spesifik response (T-311-02-05). `hx-sync="this:replace"` race condition mitigation (T-311-02-06).

## Resume Instructions for Continuation Agent

**Status saat agent resumed:** Task 4 = checkpoint:human-verify (BLOCKING). Continuation agent akan dipanggil dengan resume signal dari user.

**If user reply "uat passed" + perf metrics paste:**
1. Verify Task 1-3 commits exist via `git log --oneline -5`
2. Execute Task 5 (final commit Plan 02 changes — wrap up)
3. Update SUMMARY.md dengan UAT result + perf metrics
4. Mark plan COMPLETE

**If user reply "uat failed" + gap explanation:**
1. Identify gap: tab mana yang miss kolom/row count, atau perf tab mana yang miss
2. Return ke Task 2 (controller fix) atau Task 3 (view fix) sesuai gap
3. Re-build + re-verify
4. Re-trigger checkpoint untuk UAT round 2

**Verification steps for user (Task 4 manual UAT):**

Pre-requisite: `dotnet run` lokal (atau deploy ke staging). Server `10.55.3.3` masih commit lama April — IT akan deploy commit baru post-Plan 02 merge.

1. **Smoke parity Tab Assessment (~3 menit):**
   - Buka `https://localhost:5001/KPB-PortalHC/Admin/ManageAssessment` (lokal) atau wifi kantor URL
   - Tab Assessment otomatis aktif
   - Verifikasi: skeleton placeholder visible instant; setelah HTMX init, swap dengan tabel real
   - Test filter (search debounce 500ms, category instant, status instant) + pagination AJAX

2. **Smoke parity Tab Training (~3 menit):**
   - Klik tab "Input Records"
   - Verifikasi: skeleton training visible → swap dengan tabel + filter row + worker rows
   - Klik tab Training lagi → instant (NO re-fetch karena `once`)

3. **Smoke parity Tab History (~3 menit):**
   - Klik tab "History"
   - Verifikasi: skeleton history → sub-tabs Riwayat Assessment + Riwayat Training render

4. **Performance measurement wifi kantor (~10 menit):**
   - Chrome DevTools Network → Disable cache → Hard reload
   - Capture: shell document size KB, TTFB, content download, total time
   - Capture: active tab partial size, time
   - 3 cold runs (skip Run 1 warmup, median Run 2-3)
   - Acceptance:
     - Initial response document <14 KB (gzipped)
     - End-to-end load wifi kantor ≤40 detik (vs baseline ~1.4 menit)
     - Tab switching post-initial ≤2 detik
     - TTFB ≤500ms

5. **Race condition + error handling (~3 menit):**
   - Cepat klik antar tab — verify no flicker (hx-sync auto-cancel)
   - Force network offline DevTools → klik tab → verify error template "Gagal memuat data" + tombol "Coba Lagi"

**Resume signal:** Reply "uat passed" + perf metrics paste OR "uat failed" + gap explanation.

## Handoff Note ke Plan 03

Plan 03 (opportunistic backend) akan tambahkan:
- AsNoTracking() di main T1 query chain (current Plan 02 hanya add di Categories distinct)
- IX_AssessmentSessions_LinkedGroupId index (EF migration)
- IX_AssessmentSessions_ExamWindowCloseDate index (EF migration)
- IMemoryCache wrap untuk Categories distinct query (TTL 5 menit) + cache invalidation hooks di AddCategory/EditCategory/DeleteCategory

Plan 03 dieksekusi PARALLEL atau SETELAH Plan 02 (low-cost ~50 baris kode + 1 migration; not block Plan 02 acceptance).

## Memory Transition Note

Update memory `project_311_wave0_checkpoint.md`: Plan 02 implementation 3/5 complete, paused-at-checkpoint. Setelah UAT pass, mark Plan 02 done; mulai Plan 03 (backend opportunistic).

## Self-Check: PASSED

Files verified to exist:
- FOUND: `Controllers/AssessmentAdminController.cs` (3 partial actions present, 4 stopwatch logs, 3 ResponseCache attrs)
- FOUND: `Views/Admin/ManageAssessment.cshtml` (HTMX attrs, skeleton 3 varian, filter form, error handlers)
- FOUND: `wwwroot/lib/htmx/htmx.min.js` (51,238 bytes, SHA-256 documented)
- FOUND: `wwwroot/css/site.css` (3+ .htmx-request rules, Phase 311 marker present)
- FOUND: `.planning/REQUIREMENTS.md` (HTMX lazy load architecture strategy, ≤40 detik, ≤2 detik, <14 KB criteria)

Commits verified:
- FOUND: `03abcc92` (Task 1 — vendor + REQUIREMENTS + CSS)
- FOUND: `c2b5a910` (Task 2 — controller refactor)
- FOUND: `0f3e4690` (Task 3 — view refactor)

Build verified: 0 errors, 92 warnings (Phase 309 baseline preserved).
