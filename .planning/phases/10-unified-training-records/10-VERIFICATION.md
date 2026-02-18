---
phase: 10-unified-training-records
verified: 2026-02-18T00:00:00Z
status: passed
score: 18/18 must-haves verified
re_verification: false
gaps: []
human_verification:
  - test: Visit /CMP/Records as a Coachee and confirm merged table renders both assessment and training rows
    expected: One table with blue Assessment Online badges and green Training Manual badges, sorted most-recent-first
    why_human: Cannot execute the running application to test controller routing and DB-backed rendering
  - test: Visit /CMP/Records as an HC user and confirm worker list shows completion text format
    expected: Worker list table with Completion column showing CompletionDisplayText text
    why_human: Cannot test actual DB-queried data; relies on live user session and data
  - test: Visit /CMP/Records as an Admin and confirm worker list is always shown
    expected: Worker list view (RecordsWorkerList), not personal Records view
    why_human: Role-branch logic verified in code but Admin SelectedView simulation requires live session
  - test: In WorkerDetail view a worker with a Training Manual record with ValidUntil date in the past
    expected: Expired danger badge appears next to the date in the Berlaku Sampai column
    why_human: IsExpired computed property verified in code; badge rendering requires live data with past date
---

# Phase 10: Unified Training Records Verification Report

**Phase Goal:** Users can view their complete development history in a single merged table with type-differentiated columns, and HC can see a worker list with completion rates drawn from both sources.
**Verified:** 2026-02-18
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths — Plan 01 (Data Layer)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | GetUnifiedRecords() returns a combined date-sorted list containing both completed AssessmentSessions and TrainingRecords | VERIFIED | CMPController.cs lines 703-744: two EF Core queries, in-memory merge, OrderByDescending + ThenBy(SortPriority) |
| 2 | Assessment rows have RecordType=Assessment Online, Score, IsPassed, Status derived from IsPassed | VERIFIED | CMPController.cs lines 717-726: Status = a.IsPassed == true ? Passed : Failed |
| 3 | Training Manual rows have RecordType=Training Manual, Penyelenggara, CertificateType, ValidUntil, Status as-is | VERIFIED | CMPController.cs lines 728-738: all training-specific fields mapped, Status = t.Status |
| 4 | SortPriority=0 for assessments =1 for trainings; tie-break ordering works correctly | VERIFIED | Line 725 (SortPriority=0), line 737 (SortPriority=1), line 742 (.ThenBy(r => r.SortPriority)) |
| 5 | IsExpired computed property returns true only when ValidUntil is past DateTime.Now (no lookahead) | VERIFIED | UnifiedTrainingRecord.cs line 37: public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.Now |
| 6 | WorkerTrainingStatus has CompletedAssessments and CompletedTrainings integer fields and CompletionDisplayText computed property | VERIFIED | WorkerTrainingStatus.cs lines 48-54: CompletedAssessments (new), CompletedTrainings (pre-existing line 18), CompletionDisplayText computed string |
| 7 | GetWorkersInSection() batch-queries passed assessments per user using a single GroupBy query | VERIFIED | CMPController.cs lines 773-780: .Where(IsPassed == true).GroupBy(a => a.UserId) before foreach loop |
| 8 | Admin in all SelectedView states routes to RecordsWorkerList; only Coach/Coachee routes to personal Records view | VERIFIED | CMPController.cs lines 643-667: isCoacheeView = userRole == Coach or Coachee — all other roles fall through to RecordsWorkerList |
| 9 | Completion count for assessments uses IsPassed==true only; for trainings uses Passed or Valid only (not Permanent) | VERIFIED | CMPController.cs lines 774-775 (IsPassed == true), lines 796-798 (Passed or Valid, Permanent excluded with comment) |

### Observable Truths — Plan 02 (View Layer)

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 10 | A worker visiting /CMP/Records sees one table with both completed assessments and manual training records | VERIFIED | Records.cshtml: @model List<UnifiedTrainingRecord>, single #unifiedTable, no tab navigation present |
| 11 | Each row has a colored type badge — blue pill (Assessment Online) or green pill (Training Manual) | VERIFIED | Records.cshtml lines 126-133: badge rounded-pill bg-primary for Assessment Online, bg-success for Training Manual |
| 12 | All 9 columns are always present; non-applicable cells display an em dash not a blank | VERIFIED | Records.cshtml lines 103-112: 9 th columns; null cells use ?? em-dash or span.text-muted em-dash |
| 13 | Assessment Online rows show Score and Pass/Fail; Training Manual rows show Penyelenggara Tipe Sertifikat Berlaku Sampai | VERIFIED | Records.cshtml lines 136-148 (Score/IsPassed), lines 149-161 (Penyelenggara, CertificateType, ValidUntil) |
| 14 | Training Manual rows with ValidUntil in the past show an Expired danger badge | VERIFIED | Records.cshtml lines 151-161: @if (item.IsExpired) renders span.badge.bg-danger — same pattern in WorkerDetail.cshtml lines 154-155 |
| 15 | Worker with no records sees Belum ada riwayat pelatihan (no call to action) | VERIFIED | Records.cshtml lines 115-120: @if (Model.Count == 0) renders plain td text only — no icon no CTA |
| 16 | HC visiting /CMP/Records sees worker list with a completion count column | VERIFIED | RecordsWorkerList.cshtml line 151 (th Completion header), line 178 (@worker.CompletionDisplayText) |
| 17 | WorkerDetail stat cards show Total Records Completed Pending (3 cards no Expiring Soon) | VERIFIED | WorkerDetail.cshtml lines 40-85: exactly 3 stat cards — Expiring Soon card absent |
| 18 | Records.cshtml keeps search-by-title and year filters; tab nav is removed | VERIFIED | Records.cshtml lines 76-93: searchInput + yearFilter only; grep for nav-pills and tab-pane returned 0 matches |

**Score: 18/18 truths verified**

---

## Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|------|
| Models/UnifiedTrainingRecord.cs | Flat ViewModel bridging AssessmentSession and TrainingRecord | VERIFIED | Exists 38 lines all 10 fields + IsExpired computed property namespace HcPortal.Models |
| Models/WorkerTrainingStatus.cs | Extended with CompletedAssessments CompletedTrainings CompletionDisplayText | VERIFIED | Exists 56 lines CompletedAssessments at line 48 CompletionDisplayText at lines 52-54 |
| Controllers/CMPController.cs | GetUnifiedRecords() updated Records() role branch updated GetWorkersInSection() | VERIFIED | GetUnifiedRecords() at line 703 isCoacheeView branch at line 645 batch lookup at line 774 |
| Views/CMP/Records.cshtml | Unified single-table view @model List<UnifiedTrainingRecord> | VERIFIED | Line 1 is @model List<HcPortal.Models.UnifiedTrainingRecord> 295 lines |
| Views/CMP/RecordsWorkerList.cshtml | HC worker list with CompletionDisplayText replacing SUDAH/BELUM | VERIFIED | CompletionDisplayText at line 178 column header Completion at line 151 |
| Views/CMP/WorkerDetail.cshtml | Worker detail using @model List<UnifiedTrainingRecord> 10-column table | VERIFIED | Line 1 is @model List<HcPortal.Models.UnifiedTrainingRecord> 10-column table at lines 110-121 |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|------|
| CMPController.cs Records() | Models/UnifiedTrainingRecord.cs | GetUnifiedRecords() return type | WIRED | Line 648: var unified = await GetUnifiedRecords(user.Id); return View(Records, unified) |
| CMPController.cs GetWorkersInSection() | _context.AssessmentSessions | batch GroupBy query for passed assessments | WIRED | Lines 774-776: .Where(IsPassed == true).GroupBy(a => a.UserId) |
| CMPController.cs | Records and WorkerDetail views | return View with unified list | WIRED | Line 649: View(Records, unified); Line 687: View(WorkerDetail, unified) |
| Views/CMP/Records.cshtml | Models/UnifiedTrainingRecord.cs | @model directive at top of file | WIRED | Line 1: @model List<HcPortal.Models.UnifiedTrainingRecord> |
| Views/CMP/WorkerDetail.cshtml | Models/UnifiedTrainingRecord.cs | @model directive at top of file | WIRED | Line 1: @model List<HcPortal.Models.UnifiedTrainingRecord> |
| Views/CMP/RecordsWorkerList.cshtml | Models/WorkerTrainingStatus.cs | worker.CompletionDisplayText in table cell | WIRED | Line 178: @worker.CompletionDisplayText inside small.text-muted tag |

---

## Requirements Coverage

| Requirement | Description | Status | Notes |
|-------------|-------------|--------|------|
| TREC-01 | User can see complete development history in one merged table sorted by date | SATISFIED | Records.cshtml single unified table; GetUnifiedRecords() merges both sources |
| TREC-02 | Unified table differentiates row types — Assessment Online shows Score/Pass-Fail; Training Manual shows Penyelenggara/Tipe Sertifikat/Berlaku Sampai | SATISFIED | 9-column table with type badges; em-dash for non-applicable cells |
| TREC-03 | HC and Admin see worker list with completion rate from both sources | SATISFIED | RecordsWorkerList uses CompletionDisplayText; batch GroupBy query supplies assessment counts |

---

## Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| Controllers/CMPController.cs | 827 | Permanent used in category-filter CompletionPercentage branch | Info | Not a blocker — feeds only CompletionPercentage (legacy SUDAH/BELUM filter), not CompletedTrainings. Lines 796-798 correctly exclude Permanent. |
| Controllers/CMPController.cs | 691-700 | GetPersonalTrainingRecords() retained as dead code | Info | Documented as intentional in 10-01-SUMMARY.md — not called from Records() or WorkerDetail(). |

No stub implementations found. No TODO/FIXME/placeholder comments in any phase 10 files.

---

## Human Verification Required

### 1. Merged table rendering with live data

**Test:** Sign in as a Coachee (or Coach) and navigate to /CMP/Records
**Expected:** One table displaying both completed assessment sessions and manual training records, with blue pills for Assessment Online rows and green pills for Training Manual rows, sorted most-recent-first. Score/Pass-Fail populated for assessments; Penyelenggara/Berlaku Sampai populated for training rows; non-applicable cells show em dash.
**Why human:** Controller routing, DB query results, and Razor rendering cannot be tested without a running application and live data.

### 2. HC completion count display

**Test:** Sign in as an HC user and navigate to /CMP/Records, apply any section filter
**Expected:** Worker list with a Completion column showing text like 5 completed (3 assessments + 2 trainings) for each worker
**Why human:** CompletionDisplayText value depends on live DB data from both AssessmentSessions and TrainingRecords tables.

### 3. Admin always sees worker list

**Test:** Sign in as an Admin user and navigate to /CMP/Records, cycling through all SelectedView values via the role switcher
**Expected:** Always shows RecordsWorkerList — never the personal unified records table
**Why human:** SelectedView state is session-based and requires live role-switcher interaction.

### 4. Expired certificate badge

**Test:** View the WorkerDetail page for a worker who has a Training Manual record with a ValidUntil date before today
**Expected:** An Expired danger badge appears below the date in the Berlaku Sampai cell
**Why human:** IsExpired logic verified in code; requires live data with a past ValidUntil date to confirm rendering.

---

## Commit Verification

All 4 task commits documented in SUMMARYs confirmed present in git log:

- 0c42d2f — feat(10-01): create UnifiedTrainingRecord ViewModel
- 3a9b584 — feat(10-01): extend WorkerTrainingStatus and rewrite CMPController data layer
- 932deb8 — feat(10-02): rewrite Records.cshtml as unified single table
- 2954853 — feat(10-02): update RecordsWorkerList and WorkerDetail for unified model

---

## Summary

Phase 10 goal is fully achieved. All 18 observable truths verified against actual code — not SUMMARY claims. All 6 artifacts exist and are substantive. All 6 key links are wired. No blocker anti-patterns.

The two info-level findings are intentional and non-blocking:
1. Permanent at CMPController line 827 feeds only CompletionPercentage (a legacy percentage field used for the category-filter SUDAH/BELUM UI), not the CompletedTrainings count that feeds CompletionDisplayText.
2. GetPersonalTrainingRecords() is dead code retained intentionally as documented in 10-01-SUMMARY.md.

TREC-01, TREC-02, and TREC-03 are satisfied. Note: ROADMAP.md progress table shows Phase 10 as Not started / 0/2 — this is a stale documentation artifact only; the actual code and commits are complete.

---

_Verified: 2026-02-18_
_Verifier: Claude (gsd-verifier)_
