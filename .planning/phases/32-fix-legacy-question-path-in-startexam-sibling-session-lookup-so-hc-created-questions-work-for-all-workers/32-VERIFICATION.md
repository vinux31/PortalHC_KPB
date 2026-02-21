---
phase: 32-fix-legacy-question-path
verified: 2026-02-21T08:10:00Z
status: human_needed
score: 4/4 must-haves verified
human_verification:
  - test: Worker starts legacy (no-package) exam as non-representative and sees questions displayed
    expected: Exam questions appear; sourced from representative sibling session, not blank list
    why_human: Requires real assessment batch with HC-created questions on representative session
  - test: Non-representative worker visits ExamSummary and sees answered questions listed
    expected: Review table is populated with correct questions, not empty
    why_human: Requires end-to-end exam flow with real sibling batch data
  - test: Non-representative worker submits legacy exam and receives non-zero correct score
    expected: Score calculated against representative session questions, not zero (old broken behaviour)
    why_human: Grading loop against questionsForGrading only observable at runtime with real data
  - test: Package-path exam for worker in a batch with packages is completely unaffected
    expected: Exam loads and grades identically to before Phase 32
    why_human: Regression requires a real package-configured assessment
---

# Phase 32: Fix Legacy Question Path Verification Report

**Phase Goal:** Fix the legacy Question path in StartExam -- sibling session lookup so HC-created questions work for all workers in the assessment batch
**Verified:** 2026-02-21T08:10:00Z
**Status:** human_needed (all automated checks passed; runtime behaviour requires human confirmation)
**Re-verification:** No -- initial verification

## Goal Achievement

### Observable Truths

| #  | Truth | Status | Evidence |
|----|-------|--------|----------|
| 1  | Worker starts legacy exam (no packages) and sees HC-created questions | ? UNCERTAIN | Code correct: siblingSessionIds.Contains(a.Id) && a.Questions.Any() at line 1978. Runtime needs human. |
| 2  | Worker reviews ExamSummary and sees answered questions (not empty table) | ? UNCERTAIN | Code correct: siblingSessionIds.Contains(q.AssessmentSessionId) at line 2174. Runtime needs human. |
| 3  | Worker submits legacy exam and receives correct score from sibling session | ? UNCERTAIN | Code correct: foreach (var question in questionsForGrading) at line 2437. Runtime needs human. |
| 4  | Package path exams completely unaffected | VERIFIED | Package if-block lines 1880-1970 untouched in commits 48d5d96 and a8ebc42. |

**Score:** 4/4 truths have correct supporting code. Runtime confirmation on truths 1-3 requires human.

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| Controllers/CMPController.cs | Fixed legacy question path using sibling session lookup in all three actions | VERIFIED | File exists, substantive (3000+ lines), all three legacy else-blocks contain siblingSessionIds.Contains |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| CMPController.cs StartExam legacy path | siblingSessionIds variable | .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any()) | WIRED | Line 1978; outer-scope siblingSessionIds from line 1864 |
| CMPController.cs ExamSummary legacy path | siblingSessionIds variable | .Where(q => siblingSessionIds.Contains(q.AssessmentSessionId)) | WIRED | Lines 2165-2174; inline lookup + Contains query |
| CMPController.cs SubmitExam legacy path | siblingSessionIds variable | .Where(a => siblingSessionIds.Contains(a.Id) && a.Questions.Any()) | WIRED | Lines 2417-2427; inline lookup + sibling session load + grading loop |

### Requirements Coverage

No REQUIREMENTS.md rows are mapped to Phase 32. This phase is a targeted bug fix.

### Anti-Patterns Found

None detected. No TODO/FIXME/PLACEHOLDER comments in CMPController.cs. No stub return patterns. No .Include(a => a.Schedule) added anywhere.

### Human Verification Required

#### 1. Worker with non-representative session starts a legacy exam and sees questions

**Test:** In a batch where HC used the Question button on the representative card, log in as a non-representative worker and click Start Exam.
**Expected:** Exam renders with the correct questions from the representative session -- not a blank question list.
**Why human:** Requires a live database with a multi-worker batch where one session has questions and others have zero.

#### 2. ExamSummary shows answered questions for non-representative worker

**Test:** After answering questions in the exam above, navigate to the ExamSummary review page.
**Expected:** The review table lists the questions and the worker selected answers -- not an empty table.
**Why human:** Requires completing a full exam flow with real data.

#### 3. SubmitExam produces a correct non-zero score

**Test:** Submit the exam with at least one correct answer selected.
**Expected:** The results page shows a non-zero score matching the number of correct answers, not zero (old broken behaviour).
**Why human:** Score calculation against questionsForGrading is only observable at runtime with real data.

#### 4. Package-path exam regression check

**Test:** In a batch configured with packages, start and submit a package-path exam as any worker.
**Expected:** Exam flow and score identical to pre-Phase-32 behaviour.
**Why human:** Regression requires a real package-configured batch to confirm the package if-block was untouched at runtime.

### Gaps Summary

No gaps. All automated checks passed:
- Both commits (48d5d96, a8ebc42) exist and are valid.
- siblingSessionIds.Contains appears at 4 locations: line 1874 (original package path, unchanged), line 1978 (StartExam -- new), line 2174 (ExamSummary -- new), line 2427 (SubmitExam -- new).
- HasPackages = false preserved in StartExam legacy ViewModel (line 2000).
- foreach (var question in questionsForGrading) at line 2437 -- grading loop uses sibling source.
- AssessmentSessionId = id preserved in UserResponse.Add at line 2457 (responses per-worker own session).
- FindAsync(id) in ExamSummary assessment load at line 2102 -- unchanged.
- FirstOrDefaultAsync(a => a.Id == id) in SubmitExam assessment load at line 2285 -- unchanged.
- No .Include(a => a.Schedule) anywhere in the file.
- Zero C# compiler errors (MSB file-lock warnings are unrelated; caused by running app process).
- No TODO/FIXME/PLACEHOLDER anti-patterns in the file.

Runtime behaviour requires human confirmation in a real assessment session.

---

_Verified: 2026-02-21T08:10:00Z_
_Verifier: Claude (gsd-verifier)_