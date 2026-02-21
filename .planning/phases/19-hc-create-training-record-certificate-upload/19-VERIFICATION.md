---
phase: 19-hc-create-training-record-certificate-upload
verified: 2026-02-20T00:00:00Z
status: human_needed
score: 6/6 must-haves verified
re_verification: false
human_verification:
  - test: Open /CMP/Records as HC, click Create Training Offline, submit with PDF, view worker detail
    expected: New training row visible in worker detail with a download button in Sertifikat column
    why_human: File-write and DB persistence require a live HTTP request cycle
  - test: Submit Create Training Offline form with empty required fields
    expected: Form re-renders with inline validation errors; no record created
    why_human: ModelState validation and asp-validation-for rendering must be confirmed visually
  - test: Navigate to /CMP/CreateTrainingRecord as Coachee-role user
    expected: HTTP 403 Forbidden
    why_human: return Forbid() is a runtime HTTP response
  - test: Submit Create Training Offline form without attaching a file
    expected: Record saves normally; Sertifikat column shows em dash on worker detail page
    why_human: Null file-path code path requires live DB write to confirm
---

# Phase 19 Verification Report

**Goal:** HC or Admin opens a Create Training Offline form from the Training Records worker list, fills required and optional fields for any worker system-wide, optionally attaches a certificate file, and saves - producing a visible record in that worker training history.

**Verified:** 2026-02-20  **Status:** human_needed  **Re-verification:** No - initial

---

## Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | HC/Admin sees Create Training Offline button on RecordsWorkerList | VERIFIED | RecordsWorkerList.cshtml line 29: btn-primary with Url.Action CreateTrainingRecord/CMP inside header d-flex block |
| 2 | Button opens form with system-wide worker dropdown, no section filter | VERIFIED | GET CMPController.cs lines 1068-1072: queries _context.Users with no filter, OrderBy FullName; cshtml binds asp-items ViewBag.Workers |
| 3 | Submitting required fields saves record and redirects to worker list | VERIFIED | POST CMPController.cs lines 1141-1161: TrainingRecords.Add + SaveChangesAsync + RedirectToAction Records isFiltered=true |
| 4 | Submitting without required fields shows validation error, does not save | VERIFIED | ViewModel has [Required] on UserId/Judul/Penyelenggara/Kategori/Tanggal/Status (lines 8-39); POST returns View(model) on !ModelState.IsValid without saving; form has asp-validation-for spans |
| 5 | HC can attach PDF/image; file is downloadable from WorkerDetail | VERIFIED (automated) | POST validates extension+size; saves to wwwroot/uploads/certificates/ with timestamp prefix; SertifikatUrl in DB; GetUnifiedRecords line 1211 maps SertifikatUrl; WorkerDetail lines 174-178 render conditional download link. Downloadability requires human test. |
| 6 | Submitting without file saves record normally with no attachment | VERIFIED (automated) | POST guards file block with null/length check; sertifikatUrl=null; WorkerDetail shows em dash via IsNullOrEmpty branch. Null path requires human confirmation. |

**Score:** 6/6 truths verified. 4 runtime behaviors require human confirmation.

---

## Required Artifacts

| Artifact | Status | Details |
|----------|--------|---------|
| Models/CreateTrainingRecordViewModel.cs | VERIFIED | 54 lines; [Required] on 6 fields; IFormFile? CertificateFile present; no stubs |
| Controllers/CMPController.cs | VERIFIED | GET line 1059, POST line 1086; both gated to HC/Admin; POST has [ValidateAntiForgeryToken] |
| Views/CMP/CreateTrainingRecord.cshtml | VERIFIED | 163 lines; 3 card sections; enctype=multipart/form-data; asp-validation-for on required fields |
| Views/CMP/RecordsWorkerList.cshtml | VERIFIED | Button line 29; TempData Success/Error alerts lines 39-52 |
| Views/CMP/WorkerDetail.cshtml | VERIFIED | Sertifikat th line 121 (6% width); conditional download button lines 174-180; CSV export updated lines 247/263 |

---

## Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| RecordsWorkerList.cshtml | CMPController.CreateTrainingRecord | Url.Action CreateTrainingRecord/CMP | WIRED | Line 29 - exact pattern confirmed |
| CreateTrainingRecord.cshtml | CMPController.CreateTrainingRecord | form asp-action=CreateTrainingRecord asp-controller=CMP method=post | WIRED | Line 23 - form tag correct; enctype=multipart/form-data present |
| CMPController.cs | CreateTrainingRecordViewModel.cs | model binding POST, ViewBag.Workers GET | WIRED | GET returns View(new CreateTrainingRecordViewModel()) line 1080; POST parameter line 1086; using HcPortal.Models line 4 |
| CMPController.cs | wwwroot/uploads/certificates/ | _env.WebRootPath + uploads/certificates | WIRED | Path.Combine line 1129; sertifikatUrl line 1137; SertifikatUrl on record line 1154 |
| CMPController.GetUnifiedRecords | UnifiedTrainingRecord.SertifikatUrl | SertifikatUrl = t.SertifikatUrl | WIRED | Line 1211 - training record projection; UnifiedTrainingRecord.cs property at line 34 |

---

## Requirements Coverage

| Requirement | Status | Notes |
|-------------|--------|-------|
| TRN-01: HC/Admin creates training records for any worker system-wide | SATISFIED | System-wide dropdown; HC/Admin gate on GET and POST |
| TRN-04: Certificate file upload PDF/JPG/PNG with downloadable link | SATISFIED (automated) | File validation, disk write, SertifikatUrl stored, download link rendered; runtime confirmation needed |

---

## Anti-Patterns

None. No TODO/FIXME/placeholder comments, no stub return values, no empty handlers in any phase 19 file.

---

## Build

dotnet build: 0 errors, 34 warnings. All warnings are pre-existing CS8602 nullable warnings in CDPController.cs - none in phase 19 files. Commits db343d1 and 7bb1ccc confirmed in git log.

---

## Human Verification Required

### 1. Full Create + Certificate Download flow

**Test:** Log in as HC or Admin. Navigate to /CMP/Records. Click Create Training Offline. Fill all required fields and attach a small PDF. Click Simpan.

**Expected:** Redirect to Records with green success alert. Navigate to the worker detail page. The new training row has a green download button in the Sertifikat column. Clicking opens the PDF in a new tab.

**Why human:** File-write-to-disk and DB persistence of SertifikatUrl require a live HTTP request cycle.

### 2. Required field validation

**Test:** Open Create Training Offline form. Click Simpan without filling any field.

**Expected:** Form re-renders with inline validation errors under Pekerja, Nama Pelatihan, Penyelenggara, Kategori, Tanggal, Status. No record created.

**Why human:** ModelState validation with asp-validation-for rendering must be confirmed visually; jQuery Validate client-side behaviour also needs confirmation.

### 3. Role gate (non-HC blocked)

**Test:** Log in as Coachee-role user. Navigate directly to /CMP/CreateTrainingRecord.

**Expected:** HTTP 403 Forbidden (not a redirect, not a rendered form).

**Why human:** return Forbid() produces a runtime HTTP response; cannot be confirmed statically.

### 4. Save without file attachment

**Test:** Fill all required fields on the Create Training Offline form but do NOT attach a file. Click Simpan.

**Expected:** Redirect to Records with success message. Worker detail shows em dash in Sertifikat column for the new row.

**Why human:** Null certificate code path requires live DB write and rendered view to confirm.

---

## Gaps Summary

No gaps found. All 6 observable truths have complete automated evidence. All 5 artifacts exist, are substantive, and are wired. All 5 key links are confirmed connected. The 4 human verification items are runtime confirmation requirements, not implementation deficiencies.

---
_Verified: 2026-02-20 | Verifier: Claude (gsd-verifier)_
