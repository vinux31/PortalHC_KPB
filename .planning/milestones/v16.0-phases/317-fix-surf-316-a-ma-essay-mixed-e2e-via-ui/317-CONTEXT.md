# Phase 317 CONTEXT — Fix SURF-316-A + MA/Essay/Mixed E2E via UI

**Milestone:** v16.0 QA Test Coverage
**Status:** Executing (Task 1+2 done outside planner; Task 3-8 pending)
**Depends on:** Phase 316 (matrix test infra)
**Source plan:** `~/.claude/plans/phase-315-dan-316-streamed-llama.md` (8 tasks)

---

## Goal

1. Tutup SURF-316-A (broad submit selector + 2-step submit flow missing) — DONE
2. Buat `tests/e2e/exam-types.spec.ts` 5 FLOW baru via HC UI creation (FLOW K MA, FLOW L Essay+HC grading, FLOW M Mixed, FLOW N AllowAnswerReview=false, FLOW O AddExtraTime)
3. Regression smoke FLOW A-J di `exam-taking.spec.ts` — catat baseline pass rate

## Tasks (per source plan)

- [x] **Task 1** — Fix SURF-316-A: `examMatrix.ts:181-191` submit selector + ExamSummary→Results step 2 (commit pending)
- [x] **Task 2** — Matrix test full run validation (11/11 passed; root cause sibling-pool resolved via seed SQL peserta2 packages removed)
- [ ] **Task 3** — FLOW K: MA full cycle via HC wizard creation
- [ ] **Task 4** — FLOW L: Essay full cycle (HC create → worker answer → HC grade → Results)
- [ ] **Task 5** — FLOW M: Mixed (MC+MA+Essay) full cycle
- [ ] **Task 6** — FLOW N: AllowAnswerReview=false (Results page hides answer review)
- [ ] **Task 7** — FLOW O: AddExtraTime (HC adds time mid-exam, worker timer updates)
- [ ] **Task 8** — Regression smoke FLOW A-J — verify exam-taking.spec.ts pass rate ≥ baseline

## Bonus (done outside scope)

- examMatrix.ts helper hardening:
  - Essay saveIndicator → text check (not visibility) avoid stale fade race
  - gradeEssaysAsHc → `[data-session-id]` targeting (not nth(sessionIndex)) — fix wrong-session save bug
  - finalize click → dialog handler (confirm() accepted)
  - verifyResultPage → `.badge.text-bg-{secondary|success|danger}` (actual Results.cshtml class names)

## Resolved Questions

1. Submit flow = 2-step: `#reviewSubmitBtn` → ExamSummary → "Kumpulkan Ujian" → Results
2. AllowAnswerReview=false = same submit flow, only Results hides per-question review
3. MA checkbox = `input.exam-checkbox[value]` + `data-question-id` (`.nth(N)` for unknown IDs)
4. Essay textarea = `textarea.exam-essay` + `id="essay_{qId}"` + `data-question-id`
5. MC radio = `input.exam-radio` + `name="radio_{qId}"` + `data-question-id`
6. Essay score input = `input.essay-score-input[data-question-id][data-session-id][max]`
7. Finalize = `button.btn-finalize-grading[data-session-id]` text "Selesaikan Penilaian" + confirm() dialog

## Open Questions

1. AddExtraTime SignalR — apakah timer worker update real-time atau hanya server-side?
2. ExamSummary back-to-exam — apakah ada link "Kembali ke Ujian" yang bisa accidentally trigger? (Confirmed YES per screenshot — perlu guard di FLOW N/O)
3. Matrix `gradeEssaysAsHc` post-submit — verify HC grading masih bisa setelah Phase 316 fix

## Next Action

`/gsd-plan-phase 317` untuk formalize task split jadi PLANs, atau lanjut execute manual buat `tests/e2e/exam-types.spec.ts` (FLOW K first wave).
