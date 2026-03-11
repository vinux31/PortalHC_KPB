---
phase: 150-certificate-toggle-implementation
verified: 2026-03-11T23:00:00Z
status: passed
score: 7/7 must-haves verified
---

# Phase 150: Certificate Toggle Implementation Verification Report

**Phase Goal:** HC can control whether an assessment generates certificates via a toggle; Results and Certificate pages respect this flag

**Verified:** 2026-03-11T23:00:00Z
**Status:** PASSED
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
| --- | ------- | ---------- | -------------- |
| 1 | AssessmentSession has GenerateCertificate bool field defaulting to false for new assessments | ✓ VERIFIED | Models/AssessmentSession.cs line 35-36: `public bool GenerateCertificate { get; set; } = false;` |
| 2 | Existing assessments via migration have GenerateCertificate = true | ✓ VERIFIED | Migration 20260311012214_AddGenerateCertificateToAssessmentSession.cs lines 13-18: `defaultValue: true` for backward compatibility |
| 3 | CreateAssessment form shows 'Terbitkan Sertifikat' toggle (default OFF) | ✓ VERIFIED | Views/Admin/CreateAssessment.cshtml lines 310-315: Toggle with label "Terbitkan Sertifikat" and id "GenerateCertificate"; binding via `asp-for="GenerateCertificate"` |
| 4 | EditAssessment form shows 'Terbitkan Sertifikat' toggle pre-filled with current value | ✓ VERIFIED | Views/Admin/EditAssessment.cshtml lines 209-214: Toggle with pre-fill via `asp-for="GenerateCertificate"` (id "GenerateCertificateToggle") |
| 5 | Results page 'View Certificate' button only shows when GenerateCertificate AND IsPassed are both true | ✓ VERIFIED | Views/CMP/Results.cshtml line 324: `@if (Model.IsPassed && Model.GenerateCertificate)` guard before certificate button render |
| 6 | Certificate action returns NotFound when GenerateCertificate is false | ✓ VERIFIED | Controllers/CMPController.cs lines 1782-1784: `if (!assessment.GenerateCertificate) return NotFound();` after Completed check |
| 7 | Records.cshtml and RecordsWorkerDetail.cshtml show dash in Sertifikat column when GenerateCertificate is false for assessment rows | ✓ VERIFIED | Views/CMP/Records.cshtml lines 187-194: Conditional link when `GenerateCertificate && AssessmentSessionId.HasValue`, else dash; Views/CMP/RecordsWorkerDetail.cshtml lines 245-252: Identical pattern |

**Score:** 7/7 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
| -------- | ----------- | ------ | ------- |
| Models/AssessmentSession.cs | GenerateCertificate property with default false | ✓ VERIFIED | Line 35-36: Property exists with correct default and Display attribute |
| Models/AssessmentResultsViewModel.cs | GenerateCertificate field for view binding | ✓ VERIFIED | Line 13: Field exists and matches property name |
| Models/UnifiedTrainingRecord.cs | GenerateCertificate field for Records views | ✓ VERIFIED | Line 49: Field with default false, comment documenting use |
| Migration file | AddGenerateCertificateToAssessmentSession with defaultValue true | ✓ VERIFIED | 20260311012214_AddGenerateCertificateToAssessmentSession.cs exists with correct Up() method |
| Controllers/AdminController.cs | GenerateCertificate binding in Create/Edit | ✓ VERIFIED | 3 bindings: CreateAssessment POST (916), EditAssessment sibling copy (1172), ViewModel mapping (1257) |
| Controllers/CMPController.cs | Certificate guard + Results ViewModel + GetUnifiedRecords | ✓ VERIFIED | 4 locations: Certificate guard (1783), Results new path (1960), Results legacy path (2025), GetUnifiedRecords (698) |
| Views/Admin/CreateAssessment.cshtml | Toggle UI with Terbitkan Sertifikat label | ✓ VERIFIED | Lines 309-316: Complete toggle with label, help text, and asp-for binding |
| Views/Admin/EditAssessment.cshtml | Toggle UI pre-filled | ✓ VERIFIED | Lines 208-215: Complete toggle with pre-fill via asp-for binding |
| Views/CMP/Results.cshtml | Conditional View Certificate button | ✓ VERIFIED | Lines 324-329: Button rendered only when IsPassed && GenerateCertificate |
| Views/CMP/Records.cshtml | Conditional Sertifikat column for assessment rows | ✓ VERIFIED | Lines 187-194: Three-branch condition (Training Manual with expiry / Assessment with cert / Assessment without cert or no ID) |
| Views/CMP/RecordsWorkerDetail.cshtml | Conditional Sertifikat column for assessment rows | ✓ VERIFIED | Lines 245-252: Identical three-branch condition |

### Key Link Verification

| From | To | Via | Status | Details |
| ---- | --- | --- | ------ | ------- |
| Views/Admin/CreateAssessment.cshtml | Controllers/AdminController.cs (CreateAssessment POST) | asp-for="GenerateCertificate" binding | ✓ WIRED | Form model binding automatically maps checkbox to GenerateCertificate property; AdminController line 916 assigns to AssessmentSession |
| Views/Admin/EditAssessment.cshtml | Controllers/AdminController.cs (EditAssessment POST) | asp-for="GenerateCertificate" pre-fill and binding | ✓ WIRED | Form binding receives current value and submits changes; line 1172 updates sibling sessions |
| Controllers/CMPController.cs (Results) | Models/AssessmentResultsViewModel.cs | GenerateCertificate assignment | ✓ WIRED | Lines 1960 and 2025 both map assessment.GenerateCertificate to ViewModel field |
| Views/CMP/Results.cshtml | Models/AssessmentResultsViewModel.cs | Model.GenerateCertificate reference | ✓ WIRED | Line 324 reads Model.GenerateCertificate in conditional guard |
| Controllers/CMPController.cs (Certificate) | AssessmentSession.GenerateCertificate | NotFound guard | ✓ WIRED | Line 1783 reads assessment.GenerateCertificate and returns NotFound if false |
| Controllers/CMPController.cs (GetUnifiedRecords) | Models/UnifiedTrainingRecord.cs | GenerateCertificate projection | ✓ WIRED | Line 698 maps assessment.GenerateCertificate to UnifiedTrainingRecord.GenerateCertificate |
| Views/CMP/Records.cshtml | Models/UnifiedTrainingRecord.cs | item.GenerateCertificate reference | ✓ WIRED | Line 187 reads item.GenerateCertificate in conditional logic |
| Views/CMP/RecordsWorkerDetail.cshtml | Models/UnifiedTrainingRecord.cs | item.GenerateCertificate reference | ✓ WIRED | Line 245 reads item.GenerateCertificate in conditional logic |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
| ----------- | ---------- | ----------- | ------ | -------- |
| CERT-01 | 150-01-PLAN.md | HC can enable/disable certificate generation when creating an assessment (toggle "Terbitkan Sertifikat", default OFF) | ✓ SATISFIED | CreateAssessment.cshtml toggle (lines 310-315) + AdminController binding (line 916) + Model default false (AssessmentSession.cs line 36) |
| CERT-02 | 150-01-PLAN.md | HC can edit the certificate toggle on existing assessments via EditAssessment | ✓ SATISFIED | EditAssessment.cshtml toggle (lines 208-214) + AdminController sibling update (line 1172) |
| CERT-03 | 150-01-PLAN.md | Results page hides "View Certificate" button when GenerateCertificate is false, even if worker passed | ✓ SATISFIED | Results.cshtml conditional (line 324: `Model.IsPassed && Model.GenerateCertificate`) |
| CERT-04 | 150-01-PLAN.md | Certificate action returns 404 when GenerateCertificate is false (server-side guard) | ✓ SATISFIED | CMPController.Certificate guard (lines 1782-1784: `if (!assessment.GenerateCertificate) return NotFound();`) |
| CERT-05 | 150-01-PLAN.md | All existing assessments retain certificate access (migration default = true) | ✓ SATISFIED | Migration defaultValue: true (20260311012214_AddGenerateCertificateToAssessmentSession.cs line 18) |
| CERT-06 | 150-01-PLAN.md | Training Records views hide certificate link/column (show dash) when GenerateCertificate is false for assessment rows | ✓ SATISFIED | Records.cshtml (lines 187-194) and RecordsWorkerDetail.cshtml (lines 245-252) both implement three-branch conditional that shows link only when GenerateCertificate=true && AssessmentSessionId.HasValue, else dash |

**All 6 requirements satisfied — 6/6 coverage**

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
| ---- | ---- | ------- | -------- | ------ |
| None | — | — | — | No blocking anti-patterns detected |

### Human Verification Required

None — all functionality can be verified programmatically. Field bindings, conditions, and migrations are explicit in code.

*Optional manual testing flows:*
- Create assessment with toggle OFF → verify no Certificate button in Results even if passed
- Create assessment with toggle ON → verify Certificate button appears when passed
- Edit assessment to toggle OFF → verify Certificate button disappears and URL returns 404
- Check Records view for both certificate and non-certificate assessments → verify conditional column rendering

### Gaps Summary

No gaps found. All must-haves verified at three levels (exists, substantive, wired). Phase goal achieved:

- GenerateCertificate boolean toggle added to AssessmentSession with new-assessment default OFF, existing-assessment migration default TRUE
- Toggle UI implemented in both Create and Edit forms with proper label "Terbitkan Sertifikat"
- Results page conditionally renders certificate button only when both IsPassed AND GenerateCertificate are true
- Certificate action guards against viewing certificate when GenerateCertificate is false (returns NotFound)
- Training Records views conditionally show certificate link or dash based on GenerateCertificate flag
- All controller actions properly bind, pass, and respect the flag
- Build succeeds with 0 errors
- All 6 requirements (CERT-01 through CERT-06) satisfied

---

*Verified: 2026-03-11T23:00:00Z*
*Verifier: Claude (gsd-verifier)*
