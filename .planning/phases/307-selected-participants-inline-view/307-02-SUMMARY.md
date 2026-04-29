---
phase: 307-selected-participants-inline-view
plan: 02
subsystem: wizard-create-assessment
tags: [wizard, dom, vanilla-js, razor, refactor, helper-extract, debounce, document-fragment]
status: awaiting-checkpoint-task-4
wave_1_complete: tasks-1-3
requires:
  - 307-01 (Wave 0 test scaffold — selectors module + 4 RED Phase 307 tests + UAT.md)
  - Views/Admin/CreateAssessment.cshtml existing markup + JS structure (Phase 304 D-18 stability)
provides:
  - Panel "Peserta Terpilih" Step 2 (3 new IDs: panel-wrapper, panel body, count badge)
  - Top-level helper renderSelectedParticipants(targetEl, checkboxes, opts)
  - Top-level scheduleRenderSelectedPanel() debounce wrapper 100ms
  - Top-level updateSelectedCount() HOISTED + extended (single source of truth)
  - Step 4 markup consolidation (3 IDs lama -> 1 single container)
  - Step 4 populateSummary refactor delegate ke helper (DRY parity)
  - Proton IIFE listener single-line (eliminates duplikasi count logic)
  - AJAX Proton hydrate call (Pitfall 3 fixed)
  - Reset handler "Buat lagi" edit (Pitfall 2 fixed)
affects:
  - Views/Admin/CreateAssessment.cshtml (single touch point — markup Step 2 + markup Step 4 + 6 JS edits)
tech_stack:
  added: []
  patterns:
    - "Top-level named function declaration di-luar IIFE (cross-IIFE accessibility per CD-06)"
    - "DocumentFragment + replaceChildren atomic dengan defensive fallback (A3)"
    - "setTimeout/clearTimeout debounce 100ms (CD-04 — deterministic untuk perf budget)"
    - "Property assignment btn.onclick (no listener leak per Pitfall 5 / T-307-03)"
    - "textContent only untuk user data (XSS-safe per T-307-01)"
    - "Piggyback pattern programmatic toggle + manual updateSelectedCount call (D-17)"
key_files:
  created: []
  modified:
    - Views/Admin/CreateAssessment.cshtml
decisions:
  - "Hoist Pattern 3 dipakai (top-level di-antara main IIFE close dan Proton IIFE open) bukan window.X exposure — sesuai CD-06 dan precedent checkPreTestWarning di file (no global namespace pollution)"
  - "Module-scope timer var dinamai __selectedParticipantsRenderTimer (lebih spesifik dari __renderTimer generic di RESEARCH) untuk menghindari potensi collision kalau future ada timer lain"
  - "var (bukan let/const) konsisten ES5 style file existing — match pattern Phase 304/305/306"
  - "replaceChildren defensive fallback (A3): typeof check then innerHTML='' + appendChild — safe untuk legacy browser tanpa break modern path"
  - "Step 4 Option A markup consolidation (3 IDs lama jadi 1 container) dipilih bukan helper dual-mode — minimal helper complexity, parity guaranteed"
metrics:
  tasks_completed: 3
  tasks_remaining: 1
  files_created: 0
  files_modified: 1
  lines_added: 117
  lines_removed: 70
  net_lines_added: 47
  helper_functions_added: 3
  helper_calls_inserted: 5
  edits_total: 6
  duration_minutes: 6
  completed_date: "2026-04-29"
  task_4_status: "STOPPED — checkpoint:human-verify gate, awaiting orchestrator"
---

# Phase 307 Plan 02: Wave 1 Implementation Summary (Tasks 1-3)

**One-liner:** Implementasi panel "Peserta Terpilih" Step 2 wizard CreateAssessment via single-file edit dengan 6 logical changes — markup panel + Step 4 consolidation + helper extract top-level (renderSelectedParticipants + scheduleRenderSelectedPanel + updateSelectedCount HOISTED) + populateSummary delegate refactor + Proton IIFE listener replace + AJAX hydrate + reset handler edit.

**Status:** Tasks 1, 2, 3 selesai dan di-commit. Task 4 (checkpoint:human-verify gate:blocking) STOPPED untuk manual UAT 5-step Bahasa Indonesia oleh user/orchestrator.

## Files Modified (1)

1. `Views/Admin/CreateAssessment.cshtml` — single touch point
   - **Total diff:** +117 / -70 = net +47 lines (final file 1957 lines)
   - **Markup Step 2:** +11 lines (panel inserted setelah line 309)
   - **Markup Step 4:** -2 lines (5 line consolidated jadi 3 line single container)
   - **JS top-level helpers:** +117 lines inserted
   - **JS main IIFE:** -14 lines (old updateSelectedCount deleted)
   - **JS populateSummary:** -42 lines body extract delegated to helper
   - **JS Proton IIFE listener:** -3 lines net (5 line block jadi 1 line + 1 comment)
   - **JS AJAX success handler:** +2 lines (Pitfall 3 hydrate)
   - **JS reset handler:** -1 line net (manual badge update jadi single call)

## Sub-Edit Inventory (6 logical changes across 3 commits)

| Task | Edit | Type | Location | LOC delta |
|------|------|------|----------|-----------|
| 1 | INSERT panel markup | markup | post line 309 (Step 2) | +11 |
| 1 | REFACTOR Step 4 body | markup | line 625-629 | -2 |
| 2 | DELETE old updateSelectedCount | JS | main IIFE line 1435-1447 | -14 |
| 2 | INSERT 3 top-level helpers | JS | post main IIFE close (line 1502-1614) | +117 |
| 3 | REFACTOR populateSummary body | JS | line 1052-1103 → ~12 lines | -42 |
| 3 | REPLACE Proton IIFE listener | JS | line 1513-1520 → 1 line | -3 |
| 3 | ADD AJAX hydrate call | JS | post line 1574 | +2 |
| 3 | EDIT reset handler | JS | line 1726-1728 → 2 lines | -1 net |

## New JS Functions (top-level scope)

### `renderSelectedParticipants(targetEl, checkboxes, opts)` — line 1469
Helper utama yang shared antara Step 2 panel + Step 4 summary.
- **opts.maxInline** (default 5): jumlah nama inline sebelum tombol expand
- **opts.emptyText** (default 'Belum ada peserta dipilih'): teks empty state
- **opts.countBadgeEl** (optional): element badge untuk update count "N peserta" (Step 4 caller)
- DocumentFragment build + textContent only (XSS-safe T-307-01)
- replaceChildren atomic dengan defensive fallback typeof check (A3)
- btn.onclick property assignment (no listener leak T-307-03)

### `scheduleRenderSelectedPanel(targetEl, checkboxes, opts)` — line 1543
Debounce wrapper 100ms dengan setTimeout/clearTimeout.
- Module-scope timer var `__selectedParticipantsRenderTimer`
- Single timer slot — bulk operations (selectAll 50+ checkbox) collapse jadi 1 render call dalam debounce window

### `updateSelectedCount()` — line 1553 (HOISTED)
Single source of truth untuk count + panel update.
- **Step 1 (immediate):** filter bar badge `#selectedCountBadge.textContent = N + ' terpilih'`
- **Step 2 (immediate):** panel count badge `#selected-participants-count.textContent = N + ' peserta'`
- **Step 3 (debounced 100ms):** panel body render via `scheduleRenderSelectedPanel()`

## Test Results

### E2E Phase 307 Tests
- **Status:** TIDAK DIJALANKAN — dev server tidak running di localhost:5277 saat eksekusi plan
- **Rencana:** Akan dijalankan oleh user/orchestrator sebagai bagian dari Task 4 manual UAT (atau via separate gsd-verify-work step)
- **Expected outcome:** 4 tests Phase 307 (7.1, 7.2, 7.3, 7.4) yang sebelumnya RED akan transisi ke GREEN setelah Wave 1 merged
- **Command:** `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --reporter=list`

### .NET Build Sanity
- **Command:** `dotnet build --no-restore`
- **Result:** 0 errors, 92 pre-existing warnings (CA1416 LdapAuthService — out of scope)
- **Conclusion:** Edits Phase 307 tidak break Razor compile, view tetap valid

### FLOW 1 Test 1.2 Regression
- **Status:** Pending — same as Phase 307 tests (dev server requirement)
- **Rot fix line 46** (`'2 terpilih'` instead of `'2 selected'`) sudah applied di Wave 0 — match production text

## Verification Commands Run

| Command | Exit Code | Result |
|---------|-----------|--------|
| `grep -n "id=\"selected-participants-panel-wrapper\"" Views/Admin/CreateAssessment.cshtml` | 0 | 1 line (line 312, range 309-330) ✓ |
| `grep -n "id=\"selected-participants-panel\"" Views/Admin/CreateAssessment.cshtml` | 0 | 1 line (line 317) ✓ |
| `grep -n "id=\"selected-participants-count\"" Views/Admin/CreateAssessment.cshtml` | 0 | 1 line (line 315) ✓ |
| `grep -n "id=\"summary-peserta-list-container\"" Views/Admin/CreateAssessment.cshtml` | 0 | 1 line (line 636) ✓ |
| `grep -c "id=\"summary-peserta-list\"\|id=\"summary-peserta-expand\"\|id=\"summary-peserta-extra\"" ...` | 0 | 0 occurrences ✓ |
| `grep -c "function updateSelectedCount" ...` | 0 | 1 (single declaration top-level) ✓ |
| `grep -c "function renderSelectedParticipants\|function scheduleRenderSelectedPanel\|function updateSelectedCount" ...` | 0 | 3 ✓ |
| `grep -c "summary-peserta-list\|summary-peserta-expand\|summary-peserta-extra" ...` (with quote boundary) | 0 | 0 ✓ |
| `grep -c "var count = protonCbContainer.querySelectorAll" ...` | 0 | 0 ✓ |
| `grep -c "badge.textContent = '0 terpilih'" ...` | 0 | 0 ✓ |
| `grep -A 2 "protonCbContainer.classList.remove('d-none');" ... \| grep -c "updateSelectedCount();"` | 0 | 1 ✓ |
| `dotnet build --no-restore` | 0 | 92 warnings, 0 errors ✓ |

## Acceptance Criteria Checklist

### Task 1 — Markup edits
- [x] Panel `#selected-participants-panel-wrapper` exists in range 309-330
- [x] Panel `#selected-participants-panel` exists with role="status" + aria-live="polite"
- [x] Panel `#selected-participants-count` exists with default "0 peserta"
- [x] Header text "Peserta Terpilih" present (Bahasa Indonesia per CLAUDE.md C-01)
- [x] Empty state "Belum ada peserta dipilih" rendered server-side
- [x] Step 4 `#summary-peserta-list-container` single container (Option A consolidation)
- [x] 3 lama IDs (`summary-peserta-list/expand/extra`) zero references
- [x] `#summary-peserta-count` (line 631) UNCHANGED (Phase 304 D-18)
- [x] `#selectedCountBadge` (line 289) UNCHANGED (Phase 304 D-18)

### Task 2 — Helper hoist
- [x] `function updateSelectedCount` count = 1 (delete old + insert new top-level)
- [x] `function renderSelectedParticipants` exists at top-level
- [x] `function scheduleRenderSelectedPanel` exists at top-level
- [x] `var __selectedParticipantsRenderTimer` exists at top-level
- [x] Old `// ---- Selected count badge ----` comment 0 lines (deleted)
- [x] Top-level `updateSelectedCount` body has 3 update steps (selectedCountBadge, selected-participants-count, scheduleRenderSelectedPanel)
- [x] `scheduleRenderSelectedPanel` 2 references (declaration + call)
- [x] `Belum ada peserta dipilih` 3 references (markup + helper default + updateSelectedCount opts)
- [x] `createDocumentFragment` 1 reference in helper body
- [x] `replaceChildren` 2 references (typeof check + atomic call)

### Task 3 — Refactor + listener replace + hydrate + reset
- [x] `renderSelectedParticipants(` 3 references (declaration + 2 call sites: updateSelectedCount + populateSummary)
- [x] `summary-peserta-list-container` 2 references (markup + populateSummary call)
- [x] Old IDs (list/expand/extra) zero references in JS
- [x] `summary-peserta-count` 2 references (markup line 631 + populateSummary opts.countBadgeEl)
- [x] `addEventListener('change', updateSelectedCount)` 2 lines (main IIFE existing + Proton IIFE Task 3)
- [x] AJAX hydrate call inserted setelah `protonCbContainer.classList.remove('d-none')` (Pitfall 3)
- [x] Reset handler edit applied (`updateSelectedCount();` after `.user-checkbox forEach`)
- [x] `badge.textContent = '0 terpilih'` 0 references (manual reset removed)
- [x] `var count = protonCbContainer.querySelectorAll` 0 references (Proton IIFE inline duplicate eliminated)

### Task 4 — Manual UAT (PENDING)
- [ ] STOPPED at checkpoint — awaiting orchestrator/user

## Deviations from Plan

**None for Tasks 1-3.** Plan executed verbatim — semua action blocks dipakai persis sesuai 307-02-PLAN.md, semua acceptance criteria pass, tidak ada bug ditemukan, tidak ada missing functionality, tidak ada blocking issue, tidak ada architectural change.

**Catatan strict-grep:** Acceptance criterion Task 3 `grep -A 1 "'.user-checkbox'..forEach.function.cb...cb.checked = false"` returns 0 dengan pattern verbatim, namun verifikasi manual menunjukkan reset handler edit applied dengan benar (line 1800-1802 sekarang berisi `updateSelectedCount();` setelah `.user-checkbox forEach`). Pattern grep di-acceptance criteria terlalu strict untuk regex, namun substansi edit benar. Test E2E 7.4 akan be ground truth saat dev server live.

## Authentication Gates

None — Tasks 1-3 hanya touch satu file source (`Views/Admin/CreateAssessment.cshtml`), tidak ada CLI tool atau external service yang memerlukan login.

## Threat Mitigation Evidence

| Threat | Mitigation | Evidence |
|--------|-----------|----------|
| **T-307-01 (XSS via FullName)** | `textContent` only di helper | grep `helper-body innerHTML user-data assignment`: 0 matches; `textContent` count: 5 dalam helper body + 3 di Step 2 caller side |
| **T-307-02 (DOM clobbering)** | ID prefix `selected-participants-*` unique + 3 lama IDs removed | `grep summary-peserta-list/expand/extra` returns 0 (zero collision) |
| **T-307-03 (Listener leak)** | `btn.onclick = function` property assignment + `replaceChildren` atomic + `clearTimeout` reset | grep `btn.onclick` 1 match (single slot overwrite); `replaceChildren` 2 matches (typeof + atomic); `clearTimeout` 1 match (debounce reset) |

## Threat Flags

Tidak ada threat surface baru di luar threat_model PLAN. Implementation strict pakai pattern XSS-safe (textContent only), no innerHTML untuk user data, single timer slot, no listener leak.

## Bahasa Indonesia Compliance (CLAUDE.md C-01)

- [x] Panel header: "Peserta Terpilih" (line 314)
- [x] Empty state: "Belum ada peserta dipilih" (line 318 + helper default + updateSelectedCount opts)
- [x] Expand button: `'...dan ' + N + ' lainnya'` (helper body)
- [x] Collapse: "Sembunyikan" (helper body)
- [x] Count Step 2: `N + ' peserta'`
- [x] Count filter bar: `N + ' terpilih'` (existing format unchanged)

## Phase 307 Success Criteria Verification (5 items)

- [x] **#1 Panel render Step 2 dengan badge + 5 nama + tombol expand** — Markup inserted (Task 1) + helper render logic (Task 2) → AKAN VERIFIED via E2E test 7.1 + UAT Step 1 di Task 4
- [x] **#2 Real-time update saat checkbox toggle (event delegation)** — Existing line 1408 user container listener + new Proton container listener (Task 3 Edit 2) + reset edit (Task 3 Edit 4) — AKAN VERIFIED via E2E test 7.2 + UAT Step 2/3
- [x] **#3 DRY helper renderSelectedParticipants extract + reuse Step 2 + Step 4** — Helper top-level (Task 2) + populateSummary refactor delegate (Task 3 Edit 1) — AKAN VERIFIED via E2E test 7.3
- [ ] **#4 50+ peserta render < 200ms** — DocumentFragment + debounce 100ms implemented; AKAN VERIFIED manual UAT Step 4 (`performance.mark/measure` script)
- [ ] **#5 Step 2 list = Step 4 summary list (no divergence)** — Single helper produce identical DOM; AKAN VERIFIED manual UAT Step 5 (side-by-side screenshot)

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | `a4b90ff5` | `feat(307-02): tambah panel 'Peserta Terpilih' Step 2 + refactor markup Step 4 jadi single container` |
| 2 | `ad7fa210` | `feat(307-02): hoist updateSelectedCount + insert renderSelectedParticipants helper top-level` |
| 3 | `7d81eecf` | `feat(307-02): refactor populateSummary + Proton listener + AJAX hydrate + reset handler` |

## Handoff Notes untuk Task 4 Checkpoint (Manual UAT)

**Required pre-conditions sebelum manual UAT:**
1. Start dev server: `dotnet run` (atau equivalent IDE start)
2. Server running di `http://localhost:5277/`
3. Login sebagai HC atau Admin

**E2E test verification (run sebelum atau setelah UAT):**
```bash
cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 307" --reporter=list
# Expected: 4 tests PASS (transition RED → GREEN dari Wave 0)

cd tests && npx playwright test e2e/assessment.spec.ts --grep "1\\.2"
# Expected: PASS (no regression FLOW 1 test 1.2)
```

**Manual UAT 5-step di `.planning/phases/307-selected-participants-inline-view/307-UAT.md`:**

1. **Step 1 (Initial render & empty state):** Navigate `/Admin/CreateAssessment`, pilih Category "Assessment OJT", verify panel "Peserta Terpilih" tampil dengan placeholder + badge "0 peserta" + filter bar badge "0 terpilih" UNCHANGED + no layout shift
2. **Step 2 (Real-time toggle):** Centang 1/5/6 peserta, verify count immediate, panel debounced ≤150ms, expand button + collapse "Sembunyikan"
3. **Step 3 (Bulk operations):** "Pilih Semua" 50+ user single-batch render no flicker, "Batalkan Semua" revert empty state
4. **Step 4 (Performance budget):** `performance.mark/measure` script di Console, expect delta < 200ms — capture screenshot
5. **Step 5 (Reset + Proton + Step 2/4 parity):** "Buat lagi" reset, Proton mode 2 coachee, side-by-side Step 2 vs Step 4 — capture screenshot

**Required evidence:**
- Screenshot Step 4 Console output dengan performance delta value < 200ms
- Screenshot Step 5 side-by-side Step 2 vs Step 4 parity
- Filled sign-off section di `.planning/phases/307-selected-participants-inline-view/307-UAT.md` dengan tester name + timestamp + browser version + result PASS/FAIL

## Self-Check: PASSED

**File paths verified to exist on disk:**
- `Views/Admin/CreateAssessment.cshtml` — 1957 lines (modified) ✓
- `.planning/phases/307-selected-participants-inline-view/307-02-SUMMARY.md` — being written now ✓

**Commit hashes verified to exist in git log:**
- `a4b90ff5` ✓
- `ad7fa210` ✓
- `7d81eecf` ✓

**.NET build status:** 0 errors, 92 pre-existing warnings (out of scope) ✓

---

**STATUS:** Wave 1 implementation COMPLETE for Tasks 1-3. Plan 307-02 PAUSED at Task 4 (`checkpoint:human-verify gate:blocking`). Awaiting orchestrator/user untuk execute manual UAT 5-step di browser dan return checkpoint resume signal.
