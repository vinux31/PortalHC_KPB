---
phase: 317-fix-surf-316-a-ma-essay-mixed-e2e-via-ui
plan: 02
subsystem: testing
tags: [e2e, playwright, exam-types, mixed, allow-answer-review, extra-time, signalr, regression]

requires:
  - plan: 01
    provides: examTypes.ts (7 exports), wizardSelectors.ts (questionFormSelectors), exam-types.spec.ts (smoke + FLOW K + FLOW L), DOM-text matching pivot, SURF-317-A workaround pattern
provides:
  - tests/e2e/exam-types.spec.ts extended with FLOW M (5 sub-tests) + FLOW N (4 sub-tests) + FLOW O (5 sub-tests)
  - tests/e2e/helpers/examTypes.ts extended with addExtraTimeViaModal (multi-context SignalR helper)
  - tests/e2e/helpers/wizardSelectors.ts extended with extraTimeSelectors const
  - docs/test-reports/2026-05-11-flow-a-j-regression.md baseline report dengan SURF-317-A1 anchor
  - Multi-context HC + Worker SignalR broadcast verification pattern (FLOW O)
  - AllowAnswerReview=false negative assertion pattern (FLOW N)
affects: [Phase 318+ FLOW A-J fix scope, Phase 320 (proposed) FLOW A-J refresh]

tech-stack:
  added: []
  patterns:
    - "Multi-context cookie isolation — browser.newContext() x 2 + try/finally defensive close (HC + Worker SignalR cross-context broadcast)"
    - "SignalR composite group key — batch-{Title}|{Category}|{Date:yyyy-MM-dd} via URLSearchParams navigation"
    - "AllowAnswerReview negative assertion — .alert-info visible + .card 'Tinjauan Jawaban' toHaveCount(0)"
    - "Mixed question type cycle — DOM-text marker matching untuk MC + MA + Essay dalam single assessment"
    - "Resume flow modal handling — wait #examLoadingOverlay hidden → dismiss #resumeConfirmModal kalau visible"
    - "Stale page.once('dialog') cleanup — Resume link tidak fire confirm(), upfront handler collides dengan submitExamTwoStep handler"

key-files:
  created:
    - docs/test-reports/2026-05-11-flow-a-j-regression.md
  modified:
    - tests/e2e/exam-types.spec.ts (append FLOW M/N/O describes)
    - tests/e2e/helpers/examTypes.ts (add addExtraTimeViaModal)
    - tests/e2e/helpers/wizardSelectors.ts (append extraTimeSelectors)

key-decisions:
  - "FLOW N split N1-N4 (vs plan N1-N3) — atomic step separation per FLOW M pattern (M1 wizard, M2 package, M3 questions, M4 worker, M5 grade-verify). Behaviorally equivalent."
  - "FLOW M Results verify pakai DB-based queryScalar (SURF-317-A workaround) — Mixed includes MA → Results page 500"
  - "FLOW N Results verify pakai UI .alert-info assertion — MC-only tidak trigger SURF-317-A"
  - "FLOW O Results verify pakai UI .badge text-bg-success — MC-only tidak trigger SURF-317-A"
  - "FLOW O Resume scenario — remove upfront page.once('dialog') (collides dengan submitExamTwoStep handler)"
  - "Regression baseline diagnose-only — exam-taking.spec.ts NOT modified, anchor SURF-317-A1 → Phase 318+ fix"

patterns-established:
  - "Multi-context test isolation pattern — 2 separate browser contexts dengan defensive close di finally"
  - "SignalR broadcast verification via worker waitForFunction — assert window.timerStartRemaining > initial + delta"
  - "Negative UI assertion pair — POSITIVE (.alert-info visible) + NEGATIVE (.card toHaveCount(0)) untuk feature-disabled branch"
  - "Resume flow handler — examLoadingOverlay wait → resumeConfirmModal dismiss kalau visible (StartExam.cshtml:1141-1152)"

requirements-completed: [QA-02]

duration: ~1.5 jam (3 tasks dengan 1 iteration fix — FLOW O O4 resume modal + stale dialog handler)

deviations:
  - "FLOW N N1-N4 split (vs plan N1-N3) → 4 sub-tests bukan 3"
  - "Total Phase 317 suite = 27 sub-tests (vs plan target 26) — akibat FLOW N split"
  - "FLOW O O4 dialog handler removed (vs plan template) — Resume link direct nav, stale handler collide dengan submitExamTwoStep"

verification:
  command: npx playwright test exam-types.spec.ts --reporter=list
  result: 28/28 passed (1 setup + 27 sub-tests) in 1.9m
  per-flow:
    smoke-wave-0: 2/2 (W0.1, W0.2)
    flow-K: 5/5 (K1-K5)
    flow-L: 6/6 (L1-L6)
    flow-M: 5/5 (M1-M5)
    flow-N: 4/4 (N1-N4)
    flow-O: 5/5 (O1-O5)
  tsc: exit 0
  regression-diff: tests/e2e/exam-taking.spec.ts unmodified (git diff empty)

surf-anchors:
  - SURF-317-A: MA Results page 500 — workaround via DB-based score verify (Plan 01 discovery, Plan 02 inherited untuk FLOW M)
  - SURF-317-A1: legacy .user-check-item input selector tidak match Phase 304+ form-check Bootstrap markup — file-level serial mode bikin cascade abort 74 tests. Target fix Phase 318+ atau dedicated Phase 320 FLOW A-J refresh.

next:
  - Phase 317 dapat di-close. QA-02 5/5 coverage hijau.
  - Recommendation Phase 318+ opportunistic fix SURF-317-A1.
  - Recommendation Phase 320 (proposed) dedicated FLOW A-J wholesale refresh kalau pass rate post-fix tetap rendah.

manual-uat:
  - HC manual fire AddExtraTime di local UI → confirm modal UX natural (Indonesian copy "berhasil ditambahkan")
  - Worker mid-exam in-progress → verify timer visual increment within 2s post-AddExtraTime (matches automated assertion)
  - Cleanup dev DB setelah test selesai: hapus rows prefix [317-M], [317-N], [317-O] per CLAUDE.md SEED_WORKFLOW

commits:
  - cc37d594 feat(317-02): task 1 — FLOW M Mixed + FLOW N AllowAnswerReview=false hijau (9/9)
  - 4bd108fa feat(317-02): task 2 — FLOW O AddExtraTime SignalR multi-context hijau (5/5)
  - f23e77e6 feat(317-02): task 3 — regression baseline FLOW A-J + Phase 317 finalize
