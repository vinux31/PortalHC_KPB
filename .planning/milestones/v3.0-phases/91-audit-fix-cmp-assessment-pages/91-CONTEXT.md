# Phase 91: Audit & fix CMP Assessment pages (Assessment + Records) - Context

**Gathered:** 2026-03-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Worker-facing CMP Assessment flow audit: hub page, token entry, exam (start/resume/submit), results, certificate, and records. Fix navigation bugs, security gaps, exam flow edge cases, and improve Records page layout. Does NOT touch Admin assessment management (Phase 90 completed that).

</domain>

<decisions>
## Implementation Decisions

### Navigation & Breadcrumb
- Results.cshtml and Certificate.cshtml: Back button/breadcrumb always goes to CMP/Assessment — misdirects Admin/HC coming from Admin pages. Fix with Claude's discretion on approach (referer-based, returnUrl param, or other)
- Certificate.cshtml: same approach as Results
- Records.cshtml: add breadcrumb (CMP > Assessment > Records)
- Assessment hub does NOT need breadcrumb (top-level page)

### Security & Authorization
- VerifyToken CSRF: add `[ValidateAntiForgeryToken]` + send CSRF token in AJAX modal JS
- SubmitExam HC access: Bug — add HC role (currently allows Admin but not HC, inconsistent)
- Full CSRF audit: review ALL POST actions (SaveAnswer, SaveLegacyAnswer, UpdateSessionProgress, AbandonExam, ExamSummary POST, SubmitExam, VerifyToken) — ensure all have antiforgery + JS sends token
- Full authorization audit: review all 13 assessment actions for consistency. Claude's discretion on per-action rules.
- Completed session access: Claude's discretion (redirect vs 403)
- Answer review exposure: no change needed — Admin controls AllowAnswerReview per assessment

### Exam Flow Edge Cases
- Resume modal: verify existing resume modal works correctly (IsResume, LastActivePage, ElapsedSeconds). Fix if broken.
- Timer enforcement: Claude's discretion — review and fix gaps
- ExamSummary back-to-exam: verify "Back to Exam" restores saved answers and page position
- Auto-save retry: add retry logic (2-3x exponential backoff) to SaveAnswer/SaveLegacyAnswer JS. Show warning if fails.
- Force-close notification: show modal "Ujian ditutup oleh HC" before redirect when polling detects force-close (not silent redirect)
- Abandoned handling: leave as-is. HC resets from Admin if retry needed.
- Token modal: audit JS modal, fix bugs if found
- Package vs Legacy path: audit both for consistency in scoring, resume, save, summary

### Question Shuffling Improvements
- Fix #1 — Single-package shuffle: when only 1 package, shuffle question order (currently identical for all workers)
- Fix #2 — Option shuffle: randomize A/B/C/D per worker per question. Store in ShuffledOptionIdsPerQuestion (field exists, currently empty). Update view rendering + keep scoring ID-based.
- Keep horizontal interleaving as-is (per-package internal order stays sequential)

### Records Page Improvements
- Stat cards: change from Total/Selesai/Pending to Assessment / Training / Total
- Table layout: split into 2 tabs — Assessment Online tab and Training Manual tab
- Assessment row links: click row navigates to CMP/Results
- Breadcrumb: CMP > Assessment > Records

### Claude's Discretion
- Navigation approach for Results/Certificate back buttons
- Per-action authorization consistency rules
- StartExam completed-session handling
- Timer enforcement details
- Records page UX details beyond specified

</decisions>

<specifics>
## Specific Ideas

- Phase 90 already fixed: IsActive filters on Admin actions, RegenerateToken sibling sync, Delete cascades. CMP actions are clean — no IsActive issues.
- Fisher-Yates shuffle implementation correct, RNG fine (.NET 6+). Only scope of shuffling needs expanding.
- ShuffledOptionIdsPerQuestion field already exists on UserPackageAssignment (stores "{}"). Reuse for option shuffle.
- Cross-reference: Admin/UserAssessmentHistory links CMP/Results in same tab — Back button misdirects.

</specifics>

<code_context>
## Existing Code Insights

### Key Files
- Controllers/CMPController.cs: 13 assessment actions (Assessment, SaveAnswer, SaveLegacyAnswer, CheckExamStatus, UpdateSessionProgress, Records, VerifyToken, StartExam, AbandonExam, ExamSummary GET/POST, SubmitExam, Certificate, Results)
- Views/CMP/Assessment.cshtml (658 lines): Worker hub with cards + token modal + riwayat
- Views/CMP/StartExam.cshtml (728 lines): Live exam with timer, pagination, auto-save, polling
- Views/CMP/ExamSummary.cshtml (100 lines): Pre-submit review
- Views/CMP/Results.cshtml (224 lines): Score/pass + answer review + certificate link
- Views/CMP/Records.cshtml (295 lines): Unified records table with stats
- Views/CMP/Certificate.cshtml (275 lines): Print-ready A4 certificate

### Established Patterns
- Fisher-Yates shuffle helper at CMPController line 1113
- BuildCrossPackageAssignment at line 1130 — slot-list cross-package interleaving
- SaveAnswer upsert: ExecuteUpdateAsync then fallback Add
- CheckExamStatus polling with IMemoryCache 5s TTL
- Ownership check: session.UserId != user.Id && !IsInRole("Admin") && !IsInRole("HC")

### Integration Points
- Admin/AssessmentMonitoringDetail "View Results" opens CMP/Results (new tab)
- Admin/UserAssessmentHistory links CMP/Results (same tab — back button issue)
- Flow: CMP/Assessment → StartExam → ExamSummary → SubmitExam → Results → Certificate

</code_context>

<deferred>
## Deferred Ideas

- Token brute-force rate limiting — future security hardening phase
- Scheduled/automated assessment close — not in scope

</deferred>

---

*Phase: 91-audit-fix-cmp-assessment-pages*
*Context gathered: 2026-03-04*
