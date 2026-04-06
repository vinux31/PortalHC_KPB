# Requirements: Portal HC KPB v14.0

**Defined:** 2026-04-06
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

## v14.0 Requirements

Requirements for Assessment Enhancement milestone. Each maps to roadmap phases.

### Data Foundation

- [ ] **FOUND-01**: GradingService extracted from AssessmentAdminController — grading logic callable from SubmitExam, AkhiriUjian, AkhiriSemuaUjian, GradeFromSavedAnswers
- [ ] **FOUND-02**: QuestionType enum added to PackageQuestion (MultipleChoice, TrueFalse, MultipleAnswer, Essay, FillBlank) with default MultipleChoice for backward compatibility
- [ ] **FOUND-03**: TextAnswer nullable string field added to PackageUserResponse for Essay and FillBlank responses
- [ ] **FOUND-04**: AssessmentType field added to AssessmentSession (Standard, PrePostTest, Interview) with default Standard
- [ ] **FOUND-05**: AssessmentPhase nullable field added to AssessmentSession (null for Standard, "Pre" or "Post" for PrePostTest)
- [ ] **FOUND-06**: LinkedGroupId nullable GUID field added to AssessmentSession — shared between Pre and Post sessions of same assessment
- [ ] **FOUND-07**: LinkedSessionId nullable FK added to AssessmentSession — Pre points to Post, Post points to Pre
- [ ] **FOUND-08**: HasManualGrading bool field added to AssessmentSession — true when assessment contains Essay or FillBlank questions
- [ ] **FOUND-09**: DB migration applies cleanly with zero breaking changes to existing data

### Assessment Type & Pre-Post Test (Admin)

- [ ] **PPT-01**: HC can select assessment type (Standard / Pre-Post Test) when creating a new assessment
- [ ] **PPT-02**: When Pre-Post Test selected, HC can configure separate schedules for Pre-Test and Post-Test
- [ ] **PPT-03**: When Pre-Post Test selected, HC can configure separate duration for Pre-Test and Post-Test
- [ ] **PPT-04**: When Pre-Post Test selected, HC can assign different question packages to Pre and Post, or use checkbox "Gunakan paket soal yang sama" to copy all Pre packages to Post
- [ ] **PPT-05**: System creates 2 linked AssessmentSession records per user (Pre + Post) with shared LinkedGroupId
- [ ] **PPT-06**: Pre-Post assessment group appears as 1 entry in AssessmentMonitoring with expandable Pre/Post detail
- [ ] **PPT-07**: HC can reset Pre-Test and Post-Test automatically cascades to reset as well
- [ ] **PPT-08**: HC can delete Pre-Post assessment group and both Pre and Post sessions are deleted
- [ ] **PPT-09**: Certificate is only generated from Post-Test result (if GenerateCertificate enabled and IsPassed)
- [ ] **PPT-10**: Training Record is created only from Post-Test completion
- [ ] **PPT-11**: Renewal of Pre-Post Test certificate can be either Standard or PrePostTest type (HC choice)

### Assessment Type & Pre-Post Test (Worker)

- [ ] **WKPPT-01**: Worker sees Pre-Test and Post-Test as 2 separate cards in assessment list, visually linked
- [ ] **WKPPT-02**: Worker can start Pre-Test when status is Open and schedule has arrived
- [ ] **WKPPT-03**: Worker cannot start Post-Test until Pre-Test is Completed
- [ ] **WKPPT-04**: Worker can start Post-Test when Pre-Test is Completed and Post-Test schedule has arrived
- [ ] **WKPPT-05**: Worker can view side-by-side comparison of Pre vs Post results after Post-Test completion
- [ ] **WKPPT-06**: Comparison view shows per-element score breakdown (Pre vs Post) with gain score calculation
- [ ] **WKPPT-07**: Gain score calculated as (PostScore - PreScore) / (100 - PreScore) x 100; if PreScore = 100, Gain = 100

### Question Types

- [ ] **QTYPE-01**: HC can create True/False questions (2 options: Benar/Salah) in question package
- [ ] **QTYPE-02**: HC can create Multiple Answer questions (multiple correct options, checkboxes) in question package
- [ ] **QTYPE-03**: HC can create Essay questions (free text answer, no auto-grade) in question package
- [ ] **QTYPE-04**: HC can create Fill in the Blank questions (text input, case-insensitive match) in question package
- [ ] **QTYPE-05**: Excel import template supports QuestionType column for bulk question upload
- [ ] **QTYPE-06**: StartExam renders correct UI per question type (radio for MC/TF, checkbox for MA, textarea for Essay, text input for FillBlank)
- [ ] **QTYPE-07**: True/False and Multiple Choice auto-graded on SubmitExam (existing logic)
- [ ] **QTYPE-08**: Multiple Answer graded as all-or-nothing on SubmitExam — all correct options must be selected, no partial credit
- [ ] **QTYPE-09**: Essay questions marked as "Menunggu Penilaian" after worker submit — HC grades manually from monitoring detail
- [ ] **QTYPE-10**: Fill in the Blank auto-graded with case-insensitive exact match against correct answer text
- [ ] **QTYPE-11**: Assessment with Essay questions shows IsPassed = null until HC completes manual grading
- [ ] **QTYPE-12**: HC can grade Essay questions from AssessmentMonitoringDetail with score input per question
- [ ] **QTYPE-13**: After HC grades all Essay questions, system recalculates total score and determines IsPassed

### Mobile Optimization

- [ ] **MOB-01**: Exam UI is touch-friendly with minimum 48x48dp tap targets for all interactive elements
- [ ] **MOB-02**: Worker can swipe left/right to navigate between question pages on mobile
- [ ] **MOB-03**: Fixed bottom navigation bar on mobile with Previous/Next/Submit buttons
- [ ] **MOB-04**: Question navigation panel becomes offcanvas drawer on mobile (instead of sidebar)
- [ ] **MOB-05**: Timer display is compact and always visible on mobile header
- [ ] **MOB-06**: Anti-copy protection (Phase 280) works correctly alongside touch/swipe events

### Advanced Reporting

- [ ] **RPT-01**: HC can view Item Analysis per question: difficulty index (p-value = % correct)
- [ ] **RPT-02**: HC can view Item Analysis per question: discrimination index (Kelley upper/lower 27%) with warning when n < 30
- [ ] **RPT-03**: HC can view distractor analysis: percentage of respondents per option per question
- [ ] **RPT-04**: HC can view Pre-Post Gain Score report for PrePostTest assessments — per user, per element
- [ ] **RPT-05**: HC can view comparative report: average scores across bagian/unit, kategori, periode
- [ ] **RPT-06**: Item Analysis and Gain Score reports exportable to Excel
- [ ] **RPT-07**: Analytics Dashboard includes new panel for Gain Score trend (PrePostTest assessments)

### Accessibility

- [ ] **A11Y-01**: Skip-to-content link at top of page, visible on focus
- [ ] **A11Y-02**: All exam questions and options navigable via keyboard (arrow keys for options, Tab for navigation)
- [ ] **A11Y-03**: Timer has aria-live="assertive" announcement when < 5 minutes remaining
- [ ] **A11Y-04**: Font size control (A+/A-) on exam page, persisted via localStorage
- [ ] **A11Y-05**: HC can set ExtraTimeMinutes per session for workers with special needs
- [ ] **A11Y-06**: Focus automatically moves to first question when navigating to new page

## Future Requirements

### Deferred Features

- **QTYPE-ADV-01**: Matching question type (drag-drop pairs) — high complexity, defer to v15
- **QTYPE-ADV-02**: Question bank with tag-based random selection — separate feature, defer
- **MOB-ADV-01**: PWA / offline exam support — requires service worker architecture
- **RPT-ADV-01**: PDF report generation with charts (QuestPDF + chart rendering) — complexity
- **A11Y-ADV-01**: High contrast mode toggle — needs design system review
- **A11Y-ADV-02**: Screen reader optimized results page — defer after base accessibility

## Out of Scope

| Feature | Reason |
|---------|--------|
| Anti-cheating additions (tab detection, fullscreen, webcam) | User decided current copy-paste block is sufficient |
| Adaptive testing (CAT) | Too complex for this milestone, different assessment paradigm |
| Multi-language support | Single-language (Indonesian) is sufficient for KPB |
| Video/audio in questions | Storage/bandwidth constraints, not needed for current assessment types |
| Partial credit for Multiple Answer | Decision: All-or-Nothing for K3 compliance context |
| GradingService as separate microservice | Overkill — extract to class within same project is sufficient |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| FOUND-01 | TBD | Pending |
| FOUND-02 | TBD | Pending |
| FOUND-03 | TBD | Pending |
| FOUND-04 | TBD | Pending |
| FOUND-05 | TBD | Pending |
| FOUND-06 | TBD | Pending |
| FOUND-07 | TBD | Pending |
| FOUND-08 | TBD | Pending |
| FOUND-09 | TBD | Pending |
| PPT-01 | TBD | Pending |
| PPT-02 | TBD | Pending |
| PPT-03 | TBD | Pending |
| PPT-04 | TBD | Pending |
| PPT-05 | TBD | Pending |
| PPT-06 | TBD | Pending |
| PPT-07 | TBD | Pending |
| PPT-08 | TBD | Pending |
| PPT-09 | TBD | Pending |
| PPT-10 | TBD | Pending |
| PPT-11 | TBD | Pending |
| WKPPT-01 | TBD | Pending |
| WKPPT-02 | TBD | Pending |
| WKPPT-03 | TBD | Pending |
| WKPPT-04 | TBD | Pending |
| WKPPT-05 | TBD | Pending |
| WKPPT-06 | TBD | Pending |
| WKPPT-07 | TBD | Pending |
| QTYPE-01 | TBD | Pending |
| QTYPE-02 | TBD | Pending |
| QTYPE-03 | TBD | Pending |
| QTYPE-04 | TBD | Pending |
| QTYPE-05 | TBD | Pending |
| QTYPE-06 | TBD | Pending |
| QTYPE-07 | TBD | Pending |
| QTYPE-08 | TBD | Pending |
| QTYPE-09 | TBD | Pending |
| QTYPE-10 | TBD | Pending |
| QTYPE-11 | TBD | Pending |
| QTYPE-12 | TBD | Pending |
| QTYPE-13 | TBD | Pending |
| MOB-01 | TBD | Pending |
| MOB-02 | TBD | Pending |
| MOB-03 | TBD | Pending |
| MOB-04 | TBD | Pending |
| MOB-05 | TBD | Pending |
| MOB-06 | TBD | Pending |
| RPT-01 | TBD | Pending |
| RPT-02 | TBD | Pending |
| RPT-03 | TBD | Pending |
| RPT-04 | TBD | Pending |
| RPT-05 | TBD | Pending |
| RPT-06 | TBD | Pending |
| RPT-07 | TBD | Pending |
| A11Y-01 | TBD | Pending |
| A11Y-02 | TBD | Pending |
| A11Y-03 | TBD | Pending |
| A11Y-04 | TBD | Pending |
| A11Y-05 | TBD | Pending |
| A11Y-06 | TBD | Pending |

**Coverage:**
- v14.0 requirements: 52 total
- Mapped to phases: 0
- Unmapped: 52

---
*Requirements defined: 2026-04-06*
*Last updated: 2026-04-06 after initial definition*
