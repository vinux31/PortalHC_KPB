---
phase: 311
plan: 04
slug: manageassessment-htmx-gap-closure
tags: [perf, htmx, gap-closure, frontend, bugfix]
requires:
  - phase: 311
    plan: 02
    artifacts:
      - "HTMX shell view + 3 partial endpoints (Plan 02 commits 03abcc92, c2b5a910, 0f3e4690)"
provides:
  - "BUG-1 fix: CSS hide legacy filter rows di 3 partial views (D-10 preserved)"
  - "BUG-2A fix: cross-tab invalidation scoped ke filter-form provenance check"
  - "BUG-2B fix: drop `once` modifier dari restored hx-trigger (no stuck skeleton)"
  - "BUG-5A fix: retry handler pakai htmx.ajax direct call (bypass trigger dispatcher)"
affects:
  - "Views/Admin/ManageAssessment.cshtml (htmx:afterSwap listener + retry click handler)"
  - "wwwroot/css/site.css (Phase 311.1 gap closure CSS block — append only)"
tech-stack:
  added: []
  patterns:
    - "Provenance check via evt.detail.requestConfig.elt.closest('#filter-form') untuk distinguish filter-driven vs tab-activation swap"
    - "htmx.ajax('GET', url, {target, swap, source}) direct call bypass trigger dispatcher untuk retry"
    - "CSS direct-child selector .htmx-tab-wrapper > {legacy-filter} untuk hide partial view filter rows post-swap (D-10 backward compat)"
key-files:
  created: []
  modified:
    - path: "Views/Admin/ManageAssessment.cshtml"
      purpose: "Replace htmx:afterSwap listener + retry click handler dengan filter-form-scoped invalidation + htmx.ajax direct retry (close BUG-2A/2B/5A)"
    - path: "wwwroot/css/site.css"
      purpose: "Append Phase 311.1 gap closure CSS block: hide partial view legacy filter rows via direct-child selector (close BUG-1)"
decisions:
  - "BUG-1 strategy: CSS-only hide approach (Option 1 dari UAT.md fix_options) dipilih untuk preserve D-10 backward compat — partial view files TIDAK dimodifikasi"
  - "BUG-2A strategy: provenance check via requestConfig.elt.closest('#filter-form') — fire HANYA saat trigger element berada dalam filter form; tab activation swap (hx-trigger=load atau shown.bs.tab) early-return"
  - "BUG-2B strategy: drop `once` modifier dari restored hx-trigger (Option 2 dari UAT.md). Wrapper akan re-fire setiap shown.bs.tab event sampai content swap — idempotent post-invalidation, no htmx.cleanup needed"
  - "BUG-5A strategy: htmx.ajax direct call (Option 1 dari UAT.md) — bypass trigger dispatcher. Skeleton ditampilkan dulu sebelum ajax untuk UX feedback"
metrics:
  tasks_completed: 5
  tasks_total: 5
  duration_minutes: ~30
  files_modified: 2
  files_created: 0
  build_errors: 0
  build_warnings: 92
  warnings_baseline: 92
status: complete
uat_local_status: passed
uat_local_date: "2026-05-07"
uat_local_method: "Playwright MCP automated browser flow"
---

# Phase 311 Plan 04: HTMX Gap Closure Summary

**Status:** COMPLETE — 5/5 tasks done, UAT lokal Playwright PASS (Test 1/2/3/4/5).

## One-liner

Tutup 4 sub-bug dari UAT Plan 02 Wave 1 (BUG-1 filter duplikasi, BUG-2A invalidation agresif, BUG-2B once sticky, BUG-5A retry no-op) di `Views/Admin/ManageAssessment.cshtml` + `wwwroot/css/site.css` — scope concentrated 2 file dengan D-10 partial view preservation.

## Tasks Status

| # | Task                                                                  | Status | Commit     | Notes                                  |
| - | --------------------------------------------------------------------- | ------ | ---------- | -------------------------------------- |
| 1 | Hide legacy partial filter rows via CSS (BUG-1)                       | DONE   | `b17292f7` | 31 baris CSS appended di site.css      |
| 2 | Scope cross-tab invalidation ke filter-form (BUG-2A) + drop once (2B) | DONE   | `bbf88fa8` | Provenance check + dropped once suffix |
| 3 | Replace retry handler dengan htmx.ajax direct (BUG-5A)                | DONE   | `b5fb6354` | Skeleton + bypass trigger dispatcher   |
| 4 | Build verification + smoke load gate                                  | DONE   | (no edit)  | Build 0 errors, 92 warnings, CSS balanced |
| 5 | Re-UAT manual lokal — Test 1, Test 2, Test 5 retry                    | DONE   | —          | UAT Playwright lokal PASS (lihat section UAT Result) |

## UAT Result (2026-05-07, Playwright MCP)

UAT lokal dilakukan automated via Playwright MCP terhadap dev server worktree (port 5300, `dotnet run` dari `.claude/worktrees/agent-a4c68c3dce84abdae`). Login `admin@pertamina.com`. Hasil 5 step:

| Step | Verifikasi | Hasil | Bukti |
| ---- | ---------- | ----- | ----- |
| 1 | BUG-1 filter dup hidden 3 tab | PASS | DOM eval: `visibleFormsCount=1` (`#filter-form` shell), partial filter rows 0/2 visible Assessment, 0/2 visible Training (incl `.card.bg-light` 0/1), 0/3 visible History |
| 2 | BUG-2A invalidation + BUG-2B once | PASS | Network log: 1 req per first-click tab, 2nd click Input Records → 0 req baru (cached). Filter `test` → debounce 1.5s → req `Tab_Training?search=test`. Klik Assessment → `Tab_Assessment?search=test`, klik History → `Tab_History?search=test` (cross-tab invalidation). Tidak ada skeleton stuck |
| 3 | BUG-5A retry button | PASS | XHR monkey-patched force-fail → klik Input Records → error template "Gagal memuat data" + tombol "Coba Lagi" muncul (htmx:sendError fired). Unpatch + klik Coba Lagi → fresh request 200 OK → tabel real swap |
| 4 | Race condition smoke | PASS | Rapid 4× tab clicks dalam ~250ms (training/history/assessment/training) → settled di pane-training, 0 visible skeleton, network log dedup hanya 3 req unik |
| 5 | Server log D-09 instrumentation | PASS | Server console: `ManageAssessment perf [tab=shell\|assessment\|training\|history]: elapsed=22-129ms search_present=True/False page=1` per partial endpoint |

**Catatan non-blocking:**
- Tab History "Riwayat Assessment" subtab nampak menampilkan full 33 rows tanpa filter `search=test` — kemungkinan controller `_History` partial tidak apply search ke subtab Riwayat. Di luar scope Plan 04 (UI gap closure); flag untuk follow-up backend (Plan 03 atau backlog) bila perlu filter behavior konsisten.
- 2 console errors expected (`htmx:afterRequest` + `htmx:sendError`) saat Step 3 force-fail — ini dari htmx fire error event normal, bukan bug.

**Build gate:** Worktree dev server start berhasil dari port 5300 (port 5277 main occupied), 0 errors, 92 warnings (Phase 309 baseline preserved).

## Implementation Details

### Task 1 — CSS hide legacy partial filter rows (commit `b17292f7`)

Append block CSS di akhir `wwwroot/css/site.css` (31 baris, append only — existing Plan 02 `.htmx-request` rules preserved):

```css
/* Phase 311.1 gap closure (2026-05-07): hide partial view filter rows. */

/* _AssessmentGroupsTab.cshtml — search form (row.mb-4) + filter row (row.mb-3) */
.htmx-tab-wrapper > .row.mb-4,
.htmx-tab-wrapper > .row.mb-3 { display: none !important; }

/* _TrainingRecordsTab.cshtml — filter card (card.bg-light) */
.htmx-tab-wrapper > .card.bg-light { display: none !important; }

/* _HistoryTab.cshtml — nested filter row inside sub-tab pane */
.htmx-tab-wrapper .row.g-2.mb-3.pt-3 { display: none !important; }
```

**Selector strategy:** Direct-child selector untuk Assessment + Training (filter row langsung jadi child wrapper post-swap). Descendant selector `.row.g-2.mb-3.pt-3` untuk History (filter row nested 2 level dalam sub-tab pane). 4-class match unique ke filter row history saja (bukan content row generic).

**D-10 backward compat:** 3 file partial view (`_AssessmentGroupsTab.cshtml`, `_TrainingRecordsTab.cshtml`, `_HistoryTab.cshtml`) TIDAK dimodifikasi — verifiable: `git diff --name-only HEAD~3 HEAD` → hanya 2 file (cshtml shell + site.css).

### Task 2 — Scope cross-tab invalidation + drop once (commit `bbf88fa8`)

Replace `htmx:afterSwap` listener di `Views/Admin/ManageAssessment.cshtml`:

**BUG-2A fix — provenance check:**
```javascript
var triggerElt = evt.detail.requestConfig && evt.detail.requestConfig.elt;
if (!triggerElt || !triggerElt.closest || !triggerElt.closest('#filter-form')) {
    return; // Skip: bukan filter-driven swap (normal tab activation atau retry)
}
```

Listener fire HANYA saat swap berasal dari element di dalam `#filter-form` (search input, category select, status select). Tab activation swap (`hx-trigger="load"` atau `"shown.bs.tab"`) early-return karena `requestConfig.elt` = wrapper itu sendiri, bukan element filter form.

**BUG-2B fix — drop once modifier:**
```javascript
wrapper.setAttribute('hx-trigger', 'shown.bs.tab from:#tab-' + tabName);
// ↑ SEBELUMNYA: '... + tabName + ' once'  (sticky bug)
```

Tanpa suffix `once`, wrapper akan re-fire setiap `shown.bs.tab` event sampai content swap — idempotent karena post-invalidation skeleton ke-render dulu, lalu fetch ulang dengan filter terbaru.

### Task 3 — Retry handler htmx.ajax direct (commit `b5fb6354`)

Replace retry click handler:

```javascript
// SEBELUMNYA: htmx.trigger(wrapper, 'load')  (no-op karena wrapper hx-trigger=shown.bs.tab)
// SEKARANG:
var url = wrapper.getAttribute('hx-get');
if (!url) return;
wrapper.innerHTML = '<div aria-busy="true" ...skeleton...></div>';
htmx.ajax('GET', url, { target: wrapper, swap: 'innerHTML', source: wrapper });
```

`htmx.ajax` bypass trigger dispatcher sepenuhnya — request langsung fire dengan target wrapper + innerHTML swap. Source argument = wrapper supaya `hx-include="#filter-form"` declarative attribute di-honor (filter values terpreserve).

### Task 4 — Build verification

```
Time Elapsed 00:00:39.23
0 Error(s)
92 Warning(s)
```

**Phase 309 baseline preserved (≤92 warnings).** CSS brace balance check: 16 opening = 16 closing. JS handler count check: 5 `document.body.addEventListener` calls (afterSwap + responseError + sendError + retry click + regenerate token click) — sesuai expected.

Smoke `curl` skip karena worktree environment tidak setup auth/database — Razor view parse correctness implicit verified via `dotnet build` (Razor compile-on-build aktif).

## Build Verdict

```
0 Errors
92 Warnings (Phase 309 baseline preserved — no regression)
Time Elapsed: ~39 seconds (Release config)
```

## Deviations from Plan

### [Procedural] Edit tool path resolution issue (recurring dari Plan 02)

- **Found during:** Task 1 — Edit tool ke `wwwroot/css/site.css` dengan main-repo style absolute path resolved ke `C:/.../PortalHC_KPB/wwwroot/css/site.css` (main repo) instead of worktree path `C:/.../.claude/worktrees/agent-.../wwwroot/css/site.css`
- **Resolution:** Revert main repo (`cd main_repo && git checkout -- wwwroot/css/site.css`); re-apply edit dengan absolute path eksplisit ke worktree path (`C:/.../.claude/worktrees/agent-a4c68c3dce84abdae/wwwroot/css/site.css`)
- **No data lost** — main repo bersih sebelum revert tanpa staging.
- **Note:** Same procedural issue documented di Plan 02 SUMMARY. Selanjutnya Task 2 dan Task 3 langsung pakai absolute worktree path → sukses pertama kali.

## D-10 Backward Compatibility Verification

`git diff --name-only HEAD~3 HEAD` output:
```
Views/Admin/ManageAssessment.cshtml
wwwroot/css/site.css
```

3 partial view files (`_AssessmentGroupsTab.cshtml`, `_TrainingRecordsTab.cshtml`, `_HistoryTab.cshtml`) **TIDAK** ter-modify. D-10 contract preserved.

## Auth/Authorization Posture

Tidak ada perubahan controller atau auth surface. `[Authorize(Roles="Admin,HC")]` di partial endpoints (Plan 02) preserved. CSS dan inline JS tidak introduce trust boundary baru. Threat model T-311.04-01 sampai T-311.04-04 (lihat PLAN.md): semua disposition `accept` dengan ASVS L1 acceptable justification — tidak ada `mitigate`-required disposition yang perlu di-implement runtime.

## Resume Instructions for Continuation Agent

**Status saat agent resumed:** Task 5 = checkpoint:human-verify (BLOCKING). Continuation agent dipanggil dengan resume signal dari user.

**If user reply "uat passed local":**
1. Verify Task 1-3 commits exist via `git log --oneline -5` (b17292f7, bbf88fa8, b5fb6354)
2. Update SUMMARY.md status field `paused-at-checkpoint` → `complete`, ubah Task 5 status `PAUSED` → `DONE`
3. Tambah section "UAT Result" dengan timestamp + observasi user
4. Mark Plan 04 COMPLETE → handoff ke webdev deploy untuk Test 4 (perf wifi kantor)
5. Plan 02 paused-at-checkpoint dapat dilanjutkan setelah Test 4 wifi kantor PASS

**If user reply "uat failed [step] [observation]":**
1. Identify step + bug ID terkait (Step 1 → BUG-1, Step 2 → BUG-2A/2B, Step 3 → BUG-5A)
2. Re-iterate task terkait (Task 1 untuk BUG-1, Task 2 untuk BUG-2A/2B, Task 3 untuk BUG-5A) sesuai gap
3. Re-build + re-verify
4. Re-trigger checkpoint untuk UAT round 2

**Verification steps for user (Task 5 manual UAT):**

Pre-requisite: `dotnet run` lokal (port 5001 https). Login admin@pertamina.com (per memory `reference_dev_credentials`).

1. **Step 1 — Test 1 BUG-1 verification (~2 menit):**
   - Buka https://localhost:5001/KPB-PortalHC/Admin/ManageAssessment
   - Tab Assessment auto-aktif → tunggu skeleton swap ke tabel real
   - DevTools Elements: HANYA 1 row filter visible (shell `#filter-form`). Filter row legacy (search + category/status dropdowns) HARUS `display: none`.
   - Klik tab "Input Records" → filter card `bg-light` partial Training TIDAK visible.
   - Klik tab "History" → filter row sub-tab Riwayat Assessment TIDAK visible.
   - **Expected:** TIDAK ADA filter form duplikat di 3 tab.

2. **Step 2 — Test 2 BUG-2A + BUG-2B verification (~3 menit):**
   - Refresh halaman, DevTools Network recording on.
   - Klik tab "Input Records" pertama kali → 1 request fired ke `/ManageAssessmentTab_Training`.
   - Klik tab "History" pertama kali → 1 request fired ke `/ManageAssessmentTab_History`.
   - Klik balik tab "Input Records" → **EXPECTED: TIDAK ada request baru** (cached, content instant).
   - Ubah search box di shell filter (ketik "test"). Tunggu 500ms debounce → request fired ke endpoint tab aktif.
   - Klik tab "Assessment" → **EXPECTED: request fired ke `/ManageAssessmentTab_Assessment` dengan param search=test**.
   - Klik tab "History" → **EXPECTED: request fired ke `/ManageAssessmentTab_History` dengan param search=test**.
   - **Expected:** TIDAK ada tab stuck di skeleton "Memuat data".

3. **Step 3 — Test 5 BUG-5A verification (~2 menit):**
   - Refresh halaman. DevTools Network → Throttling "Offline".
   - Klik tab "Input Records" (yang belum di-fetch) → **EXPECTED: error template muncul** dengan heading "Gagal memuat data" + tombol "Coba Lagi".
   - Throttling kembali "No throttling".
   - Klik "Coba Lagi" → **EXPECTED: skeleton muncul → 1 request baru fired → swap dengan tabel real**.

4. **Step 4 — Race condition smoke (~1 menit):**
   - Refresh halaman. Cepat klik antar tab (Assessment → Training → History → Assessment dalam <2 detik).
   - DevTools Network: request previous status "cancelled" (hx-sync auto-cancel).
   - **Expected:** Tidak ada flicker / duplicate render.

5. **Step 5 — Server log sanity (~30 detik):**
   - Cek server console: `ManageAssessment perf [tab=...]: elapsed=...ms` (D-09 Stopwatch instrumentation preserved).

**Reply format:**
- "uat passed local" → Plan 04 close, hand off ke webdev deploy untuk Test 4 (perf wifi kantor)
- "uat failed [step] [observation]" → revisit task terkait + iterate

## Handoff Note ke Webdev

Setelah UAT lokal PASS, deploy commit ke server `10.55.3.3` untuk Test 4:
- Initial response document <14 KB (gzipped)
- End-to-end load wifi kantor ≤40 detik (vs baseline ~1.4 menit)
- Tab switching post-initial ≤2 detik
- TTFB ≤500ms

Setelah Test 4 PASS, Plan 02 paused-at-checkpoint dapat di-close (mark complete) → milestone v15.0 lanjut ke phase berikutnya.

## Memory Transition Note

Update memory `project_311_wave0_checkpoint.md`: Plan 04 implementation 4/5 complete (paused-at-checkpoint Task 5). Setelah UAT lokal pass + webdev Test 4 pass, mark Plan 04 done dan close Plan 02 sekaligus.

## Self-Check: PASSED

Files verified:
- FOUND: `Views/Admin/ManageAssessment.cshtml` (markers: "Phase 311 Plan 04 gap closure (BUG-2A)", "Phase 311 Plan 04 gap closure (BUG-5A)", "triggerElt.closest('#filter-form')", "htmx.ajax('GET'", absent: "htmx.trigger(wrapper, 'load')", absent: "from:#tab-' + tabName + ' once'")
- FOUND: `wwwroot/css/site.css` (markers: "Phase 311.1 gap closure", `.htmx-tab-wrapper > .row.mb-4`, `.htmx-tab-wrapper > .card.bg-light`, `.htmx-tab-wrapper .row.g-2.mb-3.pt-3`)

Commits verified:
- FOUND: `b17292f7` (Task 1 — CSS hide legacy filter rows)
- FOUND: `bbf88fa8` (Task 2 — provenance check + drop once)
- FOUND: `b5fb6354` (Task 3 — retry htmx.ajax direct)

Build verified: 0 errors, 92 warnings (Phase 309 baseline preserved).

D-10 verified: `git diff --name-only HEAD~3 HEAD` output 2 files only (Views/Admin/ManageAssessment.cshtml + wwwroot/css/site.css). 3 partial view files unchanged.
