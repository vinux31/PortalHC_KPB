# Phase 198: CRUD Consolidation - Research

**Researched:** 2026-03-18
**Domain:** ASP.NET Core MVC controller refactoring (route consolidation, action relocation)
**Confidence:** HIGH

## Summary

Phase ini murni refactoring: menghapus 2 action orphan dari CMPController (EditTrainingRecord, DeleteTrainingRecord), memindahkan 2 action import (ImportTraining, DownloadImportTrainingTemplate) dari CMP ke Admin, dan membersihkan referensi terkait di views. Tidak ada fitur baru, tidak ada perubahan database.

Investigasi kode menunjukkan: (1) EditTrainingRecordViewModel dipakai oleh AdminController dan Admin/EditTraining.cshtml -- JANGAN dihapus, (2) import logic di CMP sudah mature (~100 baris POST handler), (3) ManageAssessment tab training sudah punya tombol "Tambah Training" di line 362 -- import button ditambahkan di sebelahnya, (4) RecordsTeam.cshtml lines 115-127 berisi import buttons yang harus dihapus.

**Primary recommendation:** Kerjakan dalam 1 plan dengan 3 task berurutan: hapus CMP edit/delete, pindahkan import ke Admin, bersihkan view references.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Hapus langsung EditTrainingRecord dan DeleteTrainingRecord dari CMPController -- endpoints sudah orphaned, tidak perlu redirect
- Pindahkan ImportTraining dan DownloadImportTrainingTemplate dari CMPController ke AdminController
- View ImportTraining ditulis ulang mengikuti pattern ImportWorkers.cshtml (bukan copy dari CMP)
- Tombol Import ditempatkan di ManageAssessment?tab=training (di samping tombol Add Training)
- Hapus tombol import dari CMP/RecordsTeam
- Hapus action dari CMP tanpa redirect
- Authorization tetap [Authorize(Roles = "Admin, HC")]
- Worker Detail views tidak perlu perubahan -- sudah distinct

### Claude's Discretion
- Detail implementasi view ImportTraining baru (selama mengikuti pattern ImportWorkers.cshtml)
- Cleanup ViewModel/model yang jadi orphan setelah penghapusan

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CRUD-01 | Training Record edit/hapus di CMPController dihapus -- Admin jadi satu-satunya entry point | CMPController lines 845, 930 berisi EditTrainingRecord dan DeleteTrainingRecord POST actions. Tidak ada view/link yang mengarah ke sini -- safe to delete. |
| CRUD-02 | Training Import dipindahkan ke Admin | CMPController lines 673-789 berisi DownloadImportTrainingTemplate dan ImportTraining (GET+POST). Logic sudah mature. ImportWorkers.cshtml tersedia sebagai pattern. ManageAssessment line 362 adalah lokasi penempatan button. |
| CRUD-03 | Worker Detail di Admin vs CMP dibedakan tujuannya | Sudah distinct -- tidak perlu perubahan. Admin/WorkerDetail = profil, CMP/RecordsWorkerDetail = training & assessment history. |
</phase_requirements>

## Architecture Patterns

### Pattern: Action Relocation (CMP to Admin)

Import actions dipindahkan dari CMPController ke AdminController. Logic bisnis (Excel parsing, validation, record creation) di-copy karena tightly coupled ke controller context (ViewBag, TempData, redirect targets).

**Key differences CMP vs Admin context:**
- Breadcrumb: Kelola Data > Manajemen Assessment > Import Training (bukan CMP breadcrumb)
- Redirect setelah import: ManageAssessment?tab=training (bukan RecordsTeam)
- View location: Views/Admin/ImportTraining.cshtml (bukan Views/CMP/)

### Pattern: ImportWorkers.cshtml (target pattern)

Structure yang harus diikuti oleh ImportTraining view baru:
1. Breadcrumb navigation (Kelola Data > Manajemen Assessment > Import Training)
2. Header dengan tombol Kembali
3. Alert untuk TempData["Error"]
4. Results summary cards (Success/Skip/Error counts) -- sesuaikan fields untuk training
5. Results table dengan status badges
6. Upload card: Step 1 Download Template + Step 2 Upload
7. Format Notes table (kolom template)
8. Drag & drop JS + file validation

### Anti-Patterns to Avoid
- **Jangan copy CMP view as-is:** CONTEXT.md explicitly says rewrite mengikuti ImportWorkers pattern
- **Jangan hapus EditTrainingRecordViewModel:** Masih dipakai AdminController (lines 5327, 5352) dan Views/Admin/EditTraining.cshtml
- **Jangan redirect CMP routes:** CONTEXT.md says no redirect needed karena endpoints sudah orphaned

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Excel template generation | Custom workbook setup | ExcelExportHelper (Phase 197) jika applicable, atau copy existing template logic dari CMP | Sudah ada pattern |
| Import result model | New model class | ImportTrainingResult yang sudah ada di Models/ | Model sudah mature |

## Common Pitfalls

### Pitfall 1: Lupa update ImportTrainingResult namespace/reference
**What goes wrong:** Setelah pindah action ke AdminController, view baru harus reference model yang benar
**How to avoid:** ImportTrainingResult sudah di Models/ namespace -- tidak perlu pindah, cukup reference dari Admin view

### Pitfall 2: ManageAssessment button placement breaks layout
**What goes wrong:** Menambah button Import di sebelah "Tambah Training" bisa break alignment
**How to avoid:** Gunakan button group atau d-flex gap pattern yang konsisten dengan existing UI. Line 360-364 saat ini pakai d-flex justify-content-between.

### Pitfall 3: Lupa hapus semua CMP references di views
**What goes wrong:** Ada link/button di view CMP yang masih mengarah ke CMP/ImportTraining yang sudah dihapus
**How to avoid:** Cek RecordsTeam.cshtml lines 115-127 (import buttons) -- hapus block ini. Grep seluruh codebase untuk "ImportTraining.*CMP" dan "DownloadImportTrainingTemplate.*CMP".

## Code Examples

### Lokasi tombol Import di ManageAssessment (line 360-364 saat ini)
```html
<!-- Current -->
<div class="d-flex justify-content-between align-items-center mb-4">
    <h5 class="fw-bold mb-0">Training Records</h5>
    <a href="@Url.Action("AddTraining", "Admin")" class="btn btn-primary">
        <i class="bi bi-plus-lg me-2"></i>Tambah Training
    </a>
</div>

<!-- Target: tambah Import button -->
<div class="d-flex justify-content-between align-items-center mb-4">
    <h5 class="fw-bold mb-0">Training Records</h5>
    <div class="d-flex gap-2">
        <a href="@Url.Action("ImportTraining", "Admin")" class="btn btn-outline-primary">
            <i class="bi bi-file-earmark-arrow-up me-2"></i>Import Excel
        </a>
        <a href="@Url.Action("AddTraining", "Admin")" class="btn btn-primary">
            <i class="bi bi-plus-lg me-2"></i>Tambah Training
        </a>
    </div>
</div>
```

### RecordsTeam.cshtml block to remove (lines 115-127)
```html
<!-- REMOVE THIS ENTIRE BLOCK -->
@if (userRole == "Admin" || userRole == "HC")
{
    <div class="col-12 col-md-6 col-lg-3">
        <a href="@Url.Action("ImportTraining", "CMP")" class="btn btn-primary w-100">
            <i class="bi bi-file-earmark-arrow-up me-1"></i>Import Excel
        </a>
    </div>
    <div class="col-12 col-md-6 col-lg-3">
        <a href="@Url.Action("DownloadImportTrainingTemplate", "CMP")" class="btn btn-outline-primary w-100">
            <i class="bi bi-file-earmark-excel me-1"></i>Download Template
        </a>
    </div>
}
```

### ImportTraining result model fields (from ImportTrainingResult.cs)
```csharp
// Fields: NIP, Judul, Status, Message
// Status values: "Success", "Error", "Skip"
```

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual browser testing (no automated test framework) |
| Quick run command | `dotnet build` |
| Full suite command | `dotnet build` (compilation = primary gate) |

### Phase Requirements to Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| CRUD-01 | CMP edit/delete actions removed, Admin still works | manual | `dotnet build` (compile check) | N/A |
| CRUD-02 | Import accessible from Admin, not from CMP | manual | `dotnet build` (compile check) | N/A |
| CRUD-03 | Worker Detail views distinct | manual (visual) | N/A | N/A |

### Sampling Rate
- **Per task commit:** `dotnet build`
- **Per wave merge:** `dotnet build` + manual browser check
- **Phase gate:** Compile clean + manual verification of all 3 requirements

### Wave 0 Gaps
None -- no automated test infrastructure in this project; compilation is the gate.

## Sources

### Primary (HIGH confidence)
- Direct code inspection: CMPController.cs (lines 670-970), AdminController.cs (lines 5327-5352)
- Direct code inspection: ManageAssessment.cshtml (lines 356-365), RecordsTeam.cshtml (lines 110-134)
- Direct code inspection: ImportWorkers.cshtml (full file -- target pattern)
- Grep results: EditTrainingRecordViewModel usage across codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - pure refactoring of existing code, no new libraries
- Architecture: HIGH - all patterns already established in codebase
- Pitfalls: HIGH - identified from direct code inspection

**Research date:** 2026-03-18
**Valid until:** 2026-04-18 (stable -- internal refactoring)
