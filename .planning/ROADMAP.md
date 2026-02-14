# Roadmap: Portal HC KPB - CMP Assessment Completion

## Overview

Complete the CMP assessment workflow by adding results display, pass/fail logic, and answer review capabilities. This enables users to see their performance immediately after completing assessments, and provides HC staff with the configuration controls needed to tailor assessment experiences. Future phases extend this with analytics dashboards and competency tracking integration.

## Phases

- [ ] **Phase 1: Assessment Results & Configuration** - Complete assessment workflow with results page, pass/fail logic, and HC configuration controls
- [ ] **Phase 2: HC Reports Dashboard** - Analytics and reporting tools for HC staff to view all assessment results and export data
- [ ] **Phase 3: KKJ/CPDP Integration** - Connect assessment results to competency tracking and development planning

## Phase Details

### Phase 1: Assessment Results & Configuration
**Goal**: Users can see their assessment results with pass/fail status and review answers, HC can configure pass thresholds and answer review visibility per assessment

**Depends on**: Nothing (first phase, builds on existing assessment system)

**Requirements**: FR1, FR2, FR3, FR4, FR5

**Success Criteria** (what must be TRUE):
  1. User completes an assessment and is immediately redirected to a results page showing score, pass/fail status, and passing threshold
  2. User can review which questions they answered correctly or incorrectly (if HC enabled answer review for that assessment)
  3. User can access past assessment results from the assessment lobby by clicking "View Results" on completed assessments
  4. HC can set pass percentage (0-100) when creating or editing assessments, with category-based defaults
  5. HC can toggle "Allow Answer Review" checkbox when creating or editing assessments to control whether users see correct answers
  6. Pass/fail status is calculated automatically on exam submission and stored in the database

**Plans:** 3 plans

Plans:
- [ ] 01-01-PLAN.md -- Database schema changes (PassPercentage, AllowAnswerReview, IsPassed, CompletedAt)
- [ ] 01-02-PLAN.md -- Assessment configuration UI (Create/Edit form enhancements)
- [ ] 01-03-PLAN.md -- Results page, SubmitExam redirect, and lobby links

---

### Phase 2: HC Reports Dashboard
**Goal**: HC staff can view, analyze, and export assessment results across all users with filtering and performance analytics

**Depends on**: Phase 1 (requires assessment results data structure)

**Requirements**: FR6

**Success Criteria** (what must be TRUE):
  1. HC can view a dashboard listing all assessments with summary statistics (total assigned, completed, pass rate)
  2. HC can filter assessment results by category, date range, section, or specific user
  3. HC can export assessment results to Excel format for external analysis
  4. HC can view individual user's complete assessment history showing all past results
  5. HC can see performance analytics including charts for pass rate trends, score distributions, and category comparisons

**Plans**: TBD

Plans:
- [ ] TBD (to be planned)

---

### Phase 3: KKJ/CPDP Integration
**Goal**: Assessment results automatically inform competency tracking and generate personalized development recommendations

**Depends on**: Phase 2 (requires assessment results and analytics foundation)

**Requirements**: None defined yet (future scope)

**Success Criteria** (what must be TRUE):
  1. User can view their current competency level vs target level for each KKJ skill, with levels updated based on assessment results
  2. System displays gap analysis visualization showing which competencies need development based on assessment performance
  3. System generates automatic IDP suggestions when competency gaps are detected from assessment results
  4. CPDP progress tracking reflects assessment completions and scores as evidence of competency development
  5. Assessment results are linked to specific CPDP competencies so HC can track which assessments validate which skills

**Plans**: TBD

Plans:
- [ ] TBD (to be planned)

---

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Assessment Results & Configuration | 0/3 | Planning complete | - |
| 2. HC Reports Dashboard | 0/TBD | Not started | - |
| 3. KKJ/CPDP Integration | 0/TBD | Not started | - |
