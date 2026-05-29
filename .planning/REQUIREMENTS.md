# Requirements — v20.0 (TBD)

**Milestone:** v20.0
**Started:** TBD
**Status:** Planning

> Fresh slate post v18.0 + v19.0 close (2026-05-29). Previous requirements archived:
> - `milestones/v18.0-REQUIREMENTS.md` (CASCADE-01 + DUPL-01..05)
> - `milestones/v19.0-REQUIREMENTS.md` (SEC-01..03 + FOUNDATION + CLOSURE + VAL-01..02 + TZ-01 + CSCD-AUDIT + CSCD-01..07)

## Goal

TBD — define via `/gsd-new-milestone v20.0`.

## v20.0 Candidate Requirements (Backlog)

Carry-over deferred (8 item dari v13-v15 + v15.0 EPRV-01):

- [ ] **EPRV-01** (v15.0 deferred): Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru). Menunggu user verifikasi save/load Rubrik.
- [ ] **Phase 303 Plan 02 Task 3** (v14.0 UAT carry-over): Coach Workload 12-langkah human verification. Paused-at-checkpoint.
- [ ] **Phase 235** (v8.0 UAT carry-over): 5 items butuh human verification via browser.
- [ ] **Phase 247** (approval chain): 2 TODO (HC review + resubmit notification). Overlap risk dengan Phase 310 T9 NotifyIfGroupCompleted.
- [ ] **Phase 297 Pre-Post Renewal**: keputusan 2 sesi baru otomatis. Undecided.
- [ ] **Phase 298 essay max character limit**: nvarchar(max) vs nvarchar(2000). Undecided.
- [ ] **Phase 293 `GetSectionUnitsDictAsync`** Level 2+ support. Undecided.
- [ ] **v11.2 paused** Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page).

Promoted todo baru 2026-05-29 (kandidat formal REQ v20.0):

- [ ] **TODO-001-gap-ux-assessment-monitoring** — 6 fix UX assessment monitoring (filter default + Group search bug + History row clickable + banner alert routing + Excel breakdown Elemen Teknis + bulk PDF export). Source: `.planning/todos/pending/001-gap-ux-assessment-monitoring.md`. Cross-link incident `2026-05-29-pretest-ojt-gast-cilacap-lost.md`.
- [ ] **TODO-002-restore-pretest-ojt-gast-cilacap** — Investigate migration loss (git log AssessmentSession schema 30 Mar–19 May) + decide restore strategy (A/B/C) + guardrail backup SOP pre-deploy. Source: `.planning/todos/pending/002-restore-pretest-ojt-gast-cilacap.md`.

## Out of Scope (initial)

TBD — define dengan v20.0 spec.

---

*Created: 2026-05-29 post v18.0 + v19.0 milestone close. Use `/gsd-new-milestone v20.0` untuk formal requirements definition.*
