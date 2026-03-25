# Phase 258: Silabus & Guidance - Context

**Gathered:** 2026-03-25
**Status:** Ready for planning

<domain>
## Phase Boundary

UAT silabus management (upload/edit/delete hierarki Kompetensi → SubKompetensi → Deliverable) dan guidance file management (upload/replace/delete/download). Scope = SIL-01..SIL-06. Fix bug yang ditemukan saat testing.

</domain>

<decisions>
## Implementation Decisions

### Orphan Cleanup (SIL-02)
- **D-01:** Hard delete otomatis saat import — deliverable yang tidak ada di payload Excel baru langsung dihapus dari DB
- **D-02:** Block hard delete jika ada active DeliverableProgress (gunakan SilabusDeletePreview yang sudah ada)
- **D-03:** Tidak perlu preview/konfirmasi UI tambahan — cleanup otomatis di dalam import flow

### Import vs Inline Edit (SIL-01)
- **D-04:** Test keduanya — ImportSilabus (Excel upload) DAN SilabusSave (inline edit)
- **D-05:** Error scenarios yang harus di-test: header mismatch, duplicate detection, required field kosong, file format salah (non-Excel)

### Guidance Access (SIL-04/05/06)
- **D-06:** Test kedua endpoint download: ProtonDataController.GuidanceDownload (Admin/HC) DAN CDPController.GuidanceDownload (Coach/Coachee)
- **D-07:** Replace/delete test: happy path + file type validation (upload file type yang tidak diizinkan). Max 10MB sudah di-enforce di code, tidak perlu test file besar.

### Deactivate/Reactivate (SIL-03)
- **D-08:** Test scope: UI hide/show + basic cascade (SubKompetensi/Deliverable ikut tersembunyi). Tidak test impact ke DeliverableProgress (itu Phase 259+).

### Export & Template (tambahan scope)
- **D-09:** ExportSilabus masuk scope Phase 258 — test export menghasilkan file valid dengan data benar
- **D-10:** DownloadSilabusTemplate — test download menghasilkan template dengan kolom benar
- **D-11:** Status Tab di ProtonData/Index.cshtml — test load menampilkan status Silabus/Guidance per Bagian+Unit+Track

### Carry Forward dari Phase 257
- Bug fix langsung in-place (D-02 Phase 257)
- Claude analisa code → user verifikasi di browser (D-03 Phase 257)
- Happy path + key edge cases, bukan exhaustive (D-04 Phase 257)

### Claude's Discretion
- Urutan test scenario per requirement
- Detail teknis orphan cleanup implementation
- Prioritas bug fix jika ditemukan multiple issues

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Silabus Management
- `Controllers/ProtonDataController.cs` §SilabusSave (line ~226) — Inline edit bulk upsert
- `Controllers/ProtonDataController.cs` §ImportSilabus (line ~827) — Excel import 2-pass validation
- `Controllers/ProtonDataController.cs` §SilabusDelete (line ~582) — Hard delete with cascade
- `Controllers/ProtonDataController.cs` §SilabusDeletePreview (line ~536) — Preview impact before delete
- `Controllers/ProtonDataController.cs` §SilabusDeactivate (line ~677) — Soft delete
- `Controllers/ProtonDataController.cs` §SilabusReactivate (line ~699) — Restore deactivated
- `Controllers/ProtonDataController.cs` §ExportSilabus (line ~720) — Export to .xlsx
- `Controllers/ProtonDataController.cs` §DownloadSilabusTemplate (line ~764) — Template download

### Guidance Management
- `Controllers/ProtonDataController.cs` §GuidanceUpload (line ~1085) — Upload guidance file
- `Controllers/ProtonDataController.cs` §GuidanceDownload (line ~1138) — Admin/HC download
- `Controllers/ProtonDataController.cs` §GuidanceReplace (line ~1153) — Replace file
- `Controllers/ProtonDataController.cs` §GuidanceDelete (line ~1215) — Delete file
- `Controllers/CDPController.cs` §GuidanceDownload (line ~238) — Coach/Coachee download

### Views
- `Views/ProtonData/Index.cshtml` — Main UI (Status tab, Silabus tab, Guidance tab)
- `Views/ProtonData/ImportSilabus.cshtml` — Import flow UI

### Models
- `Models/ProtonModels.cs` — ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, CoachingGuidanceFile

### Requirements
- `.planning/REQUIREMENTS.md` §Silabus & Guidance — SIL-01..SIL-06

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- SilabusSave: bulk upsert via List<SilabusRowDto> — inline editing sudah lengkap
- ImportSilabus: 2-pass validation (validate all → transaction insert) — sama pattern dengan ImportWorkers
- SilabusDeletePreview/SubKompetensiDeletePreview/KompetensiDeletePreview: preview impact sebelum delete
- GuidanceUpload: file type validation (.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx), max 10MB
- GuidanceReplace: safe replacement (upload → DB update → delete old)

### Established Patterns
- Import Excel: download template → upload file → 2-pass process → redirect with summary (TempData)
- Deactivate/reactivate: IsActive flag pada ProtonKompetensi
- File storage: `/uploads/guidance/` directory, timestamp+GUID naming
- ProtonDataController: class-level `[Authorize(Roles = "Admin,HC")]`

### Integration Points
- ProtonData/Index.cshtml: 3 tabs (Status, Silabus, Guidance) dengan cascading filter Bagian → Unit → Track
- CDPController.GuidanceDownload: broader access (`[Authorize]` — any authenticated user)
- Path traversal protection sudah ada di CDPController (fullPath.StartsWith check)

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 258-silabus-guidance*
*Context gathered: 2026-03-25*
