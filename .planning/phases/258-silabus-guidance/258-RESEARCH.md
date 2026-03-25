# Phase 258: Silabus & Guidance - Research

**Researched:** 2026-03-25
**Domain:** UAT — Silabus management & Guidance file management (ASP.NET Core MVC)
**Confidence:** HIGH

## Summary

Phase 258 adalah UAT (User Acceptance Testing) untuk fitur Silabus dan Guidance yang sudah terimplementasi lengkap di codebase. Semua controller action, view, dan model sudah ada. Tugas phase ini: analisa code untuk potensi bug, test setiap flow via browser, dan fix bug yang ditemukan.

Code sudah mature — SilabusSave (inline bulk upsert), ImportSilabus (2-pass Excel validation), SilabusDelete (cascade dengan transaction), Deactivate/Reactivate (IsActive flag), ExportSilabus, GuidanceUpload/Replace/Delete/Download semua sudah ada. CDPController.GuidanceDownload menyediakan akses untuk Coach/Coachee dengan path traversal protection.

**Primary recommendation:** Susun test scenario per requirement (SIL-01..SIL-06 + D-09..D-11), Claude analisa code untuk bug, user verifikasi di browser, fix in-place.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Hard delete otomatis saat import — deliverable yang tidak ada di payload Excel baru langsung dihapus dari DB
- D-02: Block hard delete jika ada active DeliverableProgress (gunakan SilabusDeletePreview yang sudah ada)
- D-03: Tidak perlu preview/konfirmasi UI tambahan — cleanup otomatis di dalam import flow
- D-04: Test keduanya — ImportSilabus (Excel upload) DAN SilabusSave (inline edit)
- D-05: Error scenarios: header mismatch, duplicate detection, required field kosong, file format salah
- D-06: Test kedua endpoint download: ProtonDataController.GuidanceDownload (Admin/HC) DAN CDPController.GuidanceDownload (Coach/Coachee)
- D-07: Replace/delete test: happy path + file type validation. Max 10MB sudah di-enforce, tidak perlu test file besar
- D-08: Test scope: UI hide/show + basic cascade. Tidak test impact ke DeliverableProgress (Phase 259+)
- D-09: ExportSilabus masuk scope
- D-10: DownloadSilabusTemplate masuk scope
- D-11: Status Tab di ProtonData/Index.cshtml masuk scope

### Claude's Discretion
- Urutan test scenario per requirement
- Detail teknis orphan cleanup implementation
- Prioritas bug fix jika ditemukan multiple issues

### Deferred Ideas (OUT OF SCOPE)
- None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| SIL-01 | Upload/upsert silabus via Excel | ImportSilabus (line 827) + SilabusSave (line 226) sudah implementasi lengkap |
| SIL-02 | Orphan cleanup — deliverable dihapus dari payload baru ikut terhapus | Orphan cleanup logic ada di SilabusSave (line ~460-530) |
| SIL-03 | Deactivate/reactivate kompetensi | SilabusDeactivate (line 677) + SilabusReactivate (line 699) |
| SIL-04 | Upload guidance file per Bagian+Unit+Track | GuidanceUpload (line 1085) |
| SIL-05 | Coach/Coachee bisa download guidance file | CDPController.GuidanceDownload (line 238) — [Authorize] any authenticated |
| SIL-06 | Replace dan delete guidance file | GuidanceReplace (line 1153) + GuidanceDelete (line 1215) |
</phase_requirements>

## Architecture Patterns

### Existing Code Structure (Fully Implemented)

```
Controllers/
├── ProtonDataController.cs   # [Authorize(Roles="Admin,HC")] — all silabus + guidance CRUD
├── CDPController.cs           # [Authorize] — GuidanceDownload for Coach/Coachee
Views/
├── ProtonData/
│   ├── Index.cshtml           # 3 tabs: Status, Silabus, Guidance
│   └── ImportSilabus.cshtml   # Excel upload flow
Models/
├── ProtonModels.cs            # ProtonKompetensi, ProtonSubKompetensi, ProtonDeliverable, CoachingGuidanceFile
wwwroot/uploads/guidance/      # Physical file storage
```

### Key Implementation Patterns

1. **Hierarki 3-level**: Kompetensi → SubKompetensi → Deliverable, scoped by Bagian+Unit+TrackId
2. **Inline edit (SilabusSave)**: JSON POST dengan `List<SilabusRowDto>`, bulk upsert + orphan cleanup dalam satu operasi
3. **Import Excel (ImportSilabus)**: 2-pass (validate all → transaction insert), header validation, duplicate skip
4. **Soft delete**: IsActive flag pada ProtonKompetensi level saja
5. **Hard delete (SilabusDelete)**: Cascade dalam transaction — progress → sessions → deliverable → orphan sub/komp
6. **Guidance files**: timestamp+GUID naming, max 10MB, allowed extensions (.pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx)

### Orphan Cleanup di SilabusSave (SIL-02 Critical Path)

SilabusSave sudah punya orphan cleanup: setelah upsert selesai, deliverable yang ada di DB tapi tidak ada di payload baru dihapus. Block delete jika ada active DeliverableProgress. Ini perlu di-verifikasi behavior-nya saat UAT.

## Common Pitfalls

### Pitfall 1: Orphan Cleanup Tidak Berjalan
**What goes wrong:** Deliverable lama tetap ada setelah import ulang tanpa deliverable tersebut
**Why it happens:** Logic orphan cleanup mungkin tidak ter-trigger jika filtering Bagian/Unit/Track tidak match
**How to avoid:** Test dengan skenario: upload silabus 5 deliverable, lalu upload ulang dengan 3 deliverable — pastikan 2 sisanya terhapus
**Warning signs:** Count deliverable di DB sebelum dan sesudah import

### Pitfall 2: Path Traversal pada Guidance Download
**What goes wrong:** User bisa download file di luar wwwroot
**Why it happens:** FilePath di DB di-manipulasi
**How to avoid:** CDPController sudah punya fullPath.StartsWith check. ProtonDataController TIDAK punya check ini — potensial bug
**Warning signs:** ProtonDataController.GuidanceDownload (line 1138) langsung serve tanpa path validation

### Pitfall 3: Block Delete Tidak Konsisten
**What goes wrong:** Hard delete berhasil padahal ada active progress
**Why it happens:** SilabusDelete cek hasActiveProgress, tapi SilabusSave orphan cleanup mungkin tidak cek
**How to avoid:** Verifikasi orphan cleanup di SilabusSave juga cek DeliverableProgress sebelum delete

### Pitfall 4: Deactivate Scope Terlalu Sempit
**What goes wrong:** Kompetensi di-deactivate tapi SubKompetensi/Deliverable masih muncul
**Why it happens:** IsActive hanya di level Kompetensi, view harus filter manual
**How to avoid:** Verifikasi UI memfilter berdasarkan parent IsActive

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel read/write | Custom CSV parser | ClosedXML (sudah dipakai) | Header validation, cell types |
| File type detection | Extension check only | Extension allowlist (sudah ada) | Sudah cukup untuk scope ini |

## Code Examples

### Test Scenario Template (untuk UAT)

```
Scenario: [SIL-XX] [Nama]
Precondition: [data yang harus ada]
Steps:
  1. [langkah]
  2. [langkah]
Expected: [hasil yang benar]
Actual: [diisi saat test]
Status: PASS / FAIL / BUG
```

### Potensi Bug yang Perlu Dicek

1. **ProtonDataController.GuidanceDownload** (line 1138): Tidak ada path traversal protection, berbeda dengan CDPController yang punya `fullPath.StartsWith` check
2. **SilabusSave orphan cleanup**: Perlu verifikasi apakah cek DeliverableProgress sebelum hard delete orphan (D-02 requirement)
3. **ImportSilabus**: Verifikasi behavior saat header tidak match — apakah redirect dengan error message yang jelas

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (browser-based) |
| Config file | N/A — manual testing |
| Quick run command | N/A |
| Full suite command | N/A |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| SIL-01 | Upload/upsert silabus via Excel + inline edit | manual | Claude code analysis + browser verify | N/A |
| SIL-02 | Orphan cleanup saat import ulang | manual | Claude code analysis + browser verify | N/A |
| SIL-03 | Deactivate/reactivate kompetensi | manual | Claude code analysis + browser verify | N/A |
| SIL-04 | Upload guidance file | manual | Claude code analysis + browser verify | N/A |
| SIL-05 | Coach/Coachee download guidance | manual | Claude code analysis + browser verify | N/A |
| SIL-06 | Replace dan delete guidance file | manual | Claude code analysis + browser verify | N/A |

### Sampling Rate
- **Per task:** Claude analisa code, user verifikasi di browser
- **Phase gate:** Semua SIL-01..SIL-06 + D-09..D-11 PASS

### Wave 0 Gaps
None — ini UAT phase, tidak ada test infrastructure yang perlu dibuat.

## Sources

### Primary (HIGH confidence)
- Direct code reading: ProtonDataController.cs (SilabusSave, ImportSilabus, SilabusDelete, Deactivate/Reactivate, Export, Guidance CRUD)
- Direct code reading: CDPController.cs (GuidanceDownload)
- 258-CONTEXT.md — locked decisions dari user

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - code sudah ada, cukup baca langsung
- Architecture: HIGH - pattern established, tidak ada yang baru
- Pitfalls: HIGH - identified dari code review langsung

**Research date:** 2026-03-25
**Valid until:** 2026-04-25 (stable — existing code UAT)
