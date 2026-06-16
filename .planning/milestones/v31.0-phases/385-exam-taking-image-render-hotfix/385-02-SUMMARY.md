---
phase: 385-exam-taking-image-render-hotfix
plan: 02
subsystem: ui
tags: [signalr, essay, exam, javascript, playwright, autosave]

requires:
  - phase: 298/313 (exam autosave + timer)
    provides: "SaveTextAnswer hub, pendingSaves/inFlightSaves guard, reviewSubmitBtn/changePage flow"
provides:
  - "StartExam.cshtml: flushEssay() + essayInFlight + hasPendingSaves() guard MC+essay"
  - "Save-on-blur essay + timeout best-effort flush (non-blocking)"
  - "e2e essay-flush-385: ketik→submit langsung→TextAnswer utuh di DB + no reject palsu"
affects: [exam-taking, StartExam, essay, submit-gate]

tech-stack:
  added: []
  patterns:
    - "Pre-submit/changePage guard tunggu MC (pendingSaves/inFlightSaves) DAN essay (essayInFlight) via helper hasPendingSaves() hoisted"
    - "flushEssay() return Promise.all — di-await sebelum submit; timeout-path fire-and-forget (jangan tunda deadline)"

key-files:
  created:
    - tests/e2e/essay-flush-385.spec.ts
  modified:
    - Views/CMP/StartExam.cshtml

key-decisions:
  - "Flush JS-side cukup (D-03) — gate CMPController UNION form+DB sudah benar, JANGAN ubah Hub/Controller"
  - "Timeout auto-submit = flushEssay() TANPA await (best-effort, jangan tunda deadline 0-detik)"
  - "Debounce 2s autosave normal DIPERTAHANKAN"

patterns-established:
  - "essayInFlight Set di-track persis seperti inFlightSaves MC, masuk satu predikat hasPendingSaves()"

requirements-completed: [PXF-03]

duration: ~50min
completed: 2026-06-15
---

# Phase 385 Plan 02: Flush Essay Sebelum Submit (F-21/PXF-03) Summary

**`StartExam.cshtml` flush jawaban essay (invoke SaveTextAnswer + await) sebelum submit/changePage + save-on-blur + timeout best-effort → keystroke ~2 detik terakhir tidak hilang dan peserta sudah-ketik tidak ditolak "belum dijawab"; e2e terbukti TextAnswer tersimpan utuh (97/97 char) di DB sebelum gate.**

## Performance
- **Duration:** ~50 min (termasuk diagnostik e2e test-data)
- **Completed:** 2026-06-15
- **Tasks:** 2
- **Files modified:** 1 (+1 created)

## Accomplishments
- `StartExam.cshtml`: `essayInFlight` Set + `flushEssay()` (clear debounce, invoke segera, Promise.all) + `hasPendingSaves()` (MC+essay) hoisted.
- reviewSubmitBtn & changePage: panggil flushEssay() lalu tunggu `hasPendingSaves()` clear sebelum submit/navigasi (rTimeout/navTimeout 5s fallback dipertahankan).
- Save-on-blur essay (D-02). Timeout auto-submit flushEssay() best-effort tanpa await (D-03). Debounce 2s dipertahankan.
- e2e `essay-flush-385.spec.ts`: ketik essay → submit langsung (tanpa debounce) → DB `PackageUserResponses.TextAnswer` utuh (exact-match COUNT) + no banner "belum dijawab" + landing Results. PASS 2/2.

## Task Commits
1. **Task 1: StartExam flush essay** - `242e8d2e` (fix)
2. **Task 2: e2e essay flush + no reject** - `b39497bf` (test)

## Files Created/Modified
- `Views/CMP/StartExam.cshtml` - flushEssay/essayInFlight/hasPendingSaves + blur + timeout flush
- `tests/e2e/essay-flush-385.spec.ts` - e2e flush utuh + gate no-reject

## Decisions Made
- Flush JS-side saja; gate server (CMPController:1627-1653 UNION form+DB) sudah benar — tak disentuh.
- e2e set value via `evaluate` (bukan `page.fill`) — robust terhadap atribut `maxlength` textarea.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Test construction] e2e fill tak mengisi textarea (maxlength)**
- **Found during:** Task 2 (diagnostik berulang)
- **Issue:** `page.fill(ESSAY_TEXT)` hasil value kosong (textarea `maxlength="@q.MaxCharacters"`; `page.fill` menghormati maxlength). SaveTextAnswer ter-invoke valLen=0 → row essay kosong → gate benar tolak. Bukan bug fix produksi.
- **Fix:** Set value via `textarea.evaluate(el=>{el.value=v; dispatch input})` (assignment programatik abaikan maxlength) + tunggu hub `state==='Connected'` sebelum interaksi.
- **Verification:** DIAG run — valAfterFill=97, INVOKE valLen=97, DB lenText=97, savedCount=1. Final run 5/5 PASS.
- **Committed in:** `b39497bf`

---
**Total deviations:** 1 (test-construction). **Impact:** Fix produksi `StartExam.cshtml` terbukti benar (flush persist 97/97 char sebelum submit); penyesuaian hanya konstruksi e2e. No scope creep.

## Issues Encountered
- Diagnostik panjang membuktikan root cause failure awal = test-data (`MaxCharacters` → `maxlength` halangi `page.fill`), BUKAN bug flush. Setelah value di-set via evaluate, flush persist utuh → gate lolos → Results.

## User Setup Required
None.

## Next Phase Readiness
- PXF-03 selesai + e2e PASS lokal. UAT final di Dev diserahkan IT setelah re-deploy.
- Migration: FALSE (JS-side only, tak ubah Hub/Controller/DB).

---
*Phase: 385-exam-taking-image-render-hotfix*
*Completed: 2026-06-15*
