---
phase: 20-edit-delete-recordsworkerlist-wiring
verified: 2026-02-20T11:39:38Z
status: human_needed
score: 5/5 must-haves verified
human_verification:
  - test: Edit modal opens pre-populated on Training Manual row
    expected: Clicking pencil icon opens Bootstrap modal with all fields pre-filled; cert download link visible if cert exists; no page navigation
    why_human: Bootstrap modal open and Razor value binding require browser DOM
  - test: Edit save persists changes with success alert
    expected: Modal form POSTs to /CMP/EditTrainingRecord; changes in table row after redirect; green TempData success alert shown
    why_human: Requires live HTTP POST, database roundtrip, rendered HTML inspection
  - test: Certificate file replacement on edit
    expected: Old file gone from disk; new file saved with timestamp; download link updated
    why_human: Physical file system changes require runtime verification
  - test: Delete with confirm removes row and cert file
    expected: confirm() dialog appears; on confirm row absent and cert file deleted from disk; TempData success alert shown
    why_human: Browser confirm cannot be triggered programmatically; file deletion requires runtime check
  - test: Assessment Online rows show no Edit/Delete buttons
    expected: Assessment Online rows show em-dash in Aksi column; no buttons
    why_human: Requires visual inspection of rendered HTML with mixed row types
---

# Phase 20: Edit, Delete, and RecordsWorkerList Wiring - Verification Report

**Phase Goal:** HC or Admin can edit any existing manual training record (including replacing a certificate file) and can delete any manual record, with both actions accessible from WorkerDetail via Bootstrap modal (in-page edit) and browser confirm() delete.
**Verified:** 2026-02-20T11:39:38Z
**Status:** human_needed
**Re-verification:** No -- initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | HC sees Edit and Delete buttons only on Training Manual rows; Assessment Online rows have no such buttons | VERIFIED | WorkerDetail.cshtml:201 -- guard on item.RecordType == Training Manual and item.TrainingRecordId.HasValue wraps both buttons; else branch renders em-dash span |
| 2 | Clicking Edit opens a pre-populated Bootstrap modal on WorkerDetail -- no page navigation occurs | VERIFIED | WorkerDetail.cshtml:204-205 -- pencil button uses data-bs-toggle/data-bs-target pointing to per-row modal ID; modals rendered inline at lines 232-369 with all fields bound from item.* Razor values |
| 3 | Saving the edit modal POSTs to the server, updates the record, and redirects back to WorkerDetail with a success alert | VERIFIED | Modal form at WorkerDetail.cshtml:244-245 has action=EditTrainingRecord enctype=multipart/form-data; CMPController.cs:1278-1281 calls SaveChangesAsync, sets TempData[Success], redirects to WorkerDetail; TempData alerts at WorkerDetail.cshtml:11-24 |
| 4 | Uploading a new file in the edit modal replaces the old file on disk and the new file is downloadable | VERIFIED | CMPController.cs:1243-1262 -- deletes old file via File.Delete(oldPath), saves new file with timestamp prefix via CopyToAsync, updates record.SertifikatUrl |
| 5 | Clicking Delete with confirm() removes the record and its certificate file from disk; the row is gone on next load | VERIFIED | WorkerDetail.cshtml:208-217 -- inline POST form to DeleteTrainingRecord with confirm guard and antiforgery token; CMPController.cs:1296-1310 -- File.Delete on cert, Remove + SaveChangesAsync, TempData success, redirect |

**Score: 5/5 truths verified**

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| Models/EditTrainingRecordViewModel.cs | Edit form binding model -- all fields except UserId; includes WorkerId and WorkerName for redirect | VERIFIED | 63 lines; Id, WorkerId, WorkerName, Judul, Penyelenggara, Kota, Kategori, Tanggal, TanggalMulai, TanggalSelesai, Status, NomorSertifikat, ValidUntil, CertificateType, CertificateFile, ExistingSertifikatUrl; no UserId |
| Models/UnifiedTrainingRecord.cs | TrainingRecordId property for Edit/Delete action generation | VERIFIED | TrainingRecordId at line 43 (int?); also Kategori, Kota, NomorSertifikat, TanggalMulai, TanggalSelesai for modal pre-population |
| Controllers/CMPController.cs | EditTrainingRecord POST and DeleteTrainingRecord POST actions | VERIFIED | EditTrainingRecord at line 1203 ([HttpPost] only, no GET); DeleteTrainingRecord at line 1287 ([HttpPost]); both have HC/Admin gate, file handling, SaveChangesAsync, TempData, redirect |
| Views/CMP/WorkerDetail.cshtml | Edit/Delete action cells on Training Manual rows; per-row Bootstrap modals; TempData alerts | VERIFIED | Aksi column header at line 136; action cell at lines 200-224; modals loop at lines 232-369; TempData alerts at lines 11-24 |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| WorkerDetail.cshtml pencil button | Bootstrap modal #editModal-@item.TrainingRecordId | data-bs-toggle and data-bs-target on button | WIRED | Lines 204-205; modal IDs match at line 234 |
| WorkerDetail.cshtml modal form | /CMP/EditTrainingRecord | form POST enctype=multipart/form-data | WIRED | Lines 244-245; action and enctype both confirmed |
| WorkerDetail.cshtml delete form | /CMP/DeleteTrainingRecord | form POST with confirm() onclick guard and hidden id=TrainingRecordId | WIRED | Lines 208-217; action, confirm(), and id hidden input all present |
| CMPController.cs EditTrainingRecord POST | wwwroot/uploads/certificates/ | File replace: delete old at WebRootPath+SertifikatUrl, save new with timestamp | WIRED | CMPController.cs:1246-1261; File.Delete + CopyToAsync + record.SertifikatUrl update confirmed |
| CMPController.cs DeleteTrainingRecord POST | wwwroot/uploads/certificates/ | File.Delete on record.SertifikatUrl path before DB Remove | WIRED | CMPController.cs:1300-1303; File.Delete on SertifikatUrl path confirmed |

Note on key_link pattern File.Delete.*SertifikatUrl: the plan specified a single-line grep pattern that does not match because the implementation uses adjacent lines. Manual inspection confirmed both operations are present and correctly wired in both controller actions.

---

### Requirements Coverage

| Requirement | Status | Notes |
|-------------|--------|-------|
| TRN-02 (Edit manual training record) | SATISFIED | EditTrainingRecord POST fully implemented; Bootstrap modal in WorkerDetail; all fields editable except UserId/Pekerja |
| TRN-03 (Delete manual training record) | SATISFIED | DeleteTrainingRecord POST fully implemented; confirm() guard; certificate file physical deletion before DB row removal |

---

### Anti-Patterns Found

None. No TODO/FIXME/PLACEHOLDER/stub patterns detected in any modified file. No empty implementations or console.log-only handlers. Build: 0 errors, 35 pre-existing warnings (none new to this phase). Both commits (6e0a5bb, 7f756a3) confirmed in git history.

---

### Human Verification Required

All supporting code is substantively implemented and wired. The following require a running browser to confirm runtime behaviour.

#### 1. Edit Modal Opens Pre-Populated

**Test:** Navigate to WorkerDetail for a worker with at least one manual training record (HC or Admin session). Click the pencil icon on a Training Manual row.
**Expected:** Bootstrap modal opens in-page without redirect; all form fields are pre-filled with that record current values; if a certificate exists, a Download Sertifikat Saat Ini button is shown.
**Why human:** Bootstrap modal open/close and Razor value binding into form inputs require a browser DOM to verify.

#### 2. Edit Save -- Changes Persist with Success Alert

**Test:** Change one field (e.g., Penyelenggara) in the edit modal and click Simpan Perubahan.
**Expected:** Page redirects back to WorkerDetail; green dismissible success alert shown at top; table row reflects updated value.
**Why human:** Requires live HTTP POST, database roundtrip, and rendered page inspection.

#### 3. Certificate File Replacement

**Test:** Open edit modal for a record with an existing certificate. Upload a new file and save.
**Expected:** Old file gone from wwwroot/uploads/certificates/; new file present with timestamp prefix; download link now points to new file.
**Why human:** Physical file system state and URL correctness require runtime inspection.

#### 4. Delete -- Row and File Removed

**Test:** Click the trash icon on a Training Manual row.
**Expected:** Browser native confirm() dialog appears. On confirm: page redirects; that row absent from WorkerDetail; green TempData success alert shown; cert file gone from disk.
**Why human:** Browser confirm() cannot be programmatically triggered; file deletion requires runtime check.

#### 5. Assessment Online Rows -- No Action Buttons

**Test:** View WorkerDetail for a worker who has both Assessment Online and Training Manual records.
**Expected:** Training Manual rows have pencil and trash buttons. Assessment Online rows show only em-dash; no buttons.
**Why human:** Requires visual inspection of rendered HTML with mixed row types present.

---

## Gaps Summary

No gaps. All 5 observable truths are verified by substantive, wired code. Both git commits (6e0a5bb, 7f756a3) confirmed in git history. Build passes 0 errors. Human verification items are runtime behaviour checks only -- code implementation is complete and correctly wired.

---

_Verified: 2026-02-20T11:39:38Z_
_Verifier: Claude (gsd-verifier)_
