# Phase 253: AddTraining multi-select pekerja dan perbaikan form - Context

**Gathered:** 2026-03-25
**Status:** Ready for planning

<domain>
## Phase Boundary

Mengubah halaman AddTraining agar mendukung pemilihan multiple pekerja sekaligus (multi-select) dengan dynamic rows per pekerja untuk file sertifikat. Fix duplikat kategori di dropdown. Tidak menambah field baru atau capability baru di luar form AddTraining.

</domain>

<decisions>
## Implementation Decisions

### Multi-select Pekerja
- **D-01:** Gunakan searchable multi-select dropdown (Tom Select atau Select2 — Claude's discretion berdasarkan stack). Project sudah punya jQuery via _Layout.cshtml.
- **D-02:** Dua mode: renewal mode tetap single-select (backward compatible), akses langsung mendukung multi-select.
- **D-03:** Maximum 20 pekerja per submission. Minimum 1 (wajib).

### Dynamic Rows per Pekerja
- **D-04:** Saat pekerja dipilih di multi-select, tampilkan dynamic rows/cards per pekerja di section "Data Sertifikat". Setiap pekerja punya field sendiri: File Sertifikat + Nomor Sertifikat.
- **D-05:** Data training (Judul, Penyelenggara, Kota, Kategori, SubKategori, Tanggal, TanggalMulai, TanggalSelesai, Status) tetap shared/sama untuk semua pekerja.

### Perilaku Simpan
- **D-06:** 1 TrainingRecord dibuat per pekerja yang dipilih. Reuse logic bulk yang sudah ada di POST handler.
- **D-07:** Validasi semua file sebelum simpan. Jika ada file yang gagal validasi (>10MB, format salah), block semua — tidak ada record yang disimpan.

### Fix Kategori Duplikat
- **D-08:** Filter duplikat "Assessment Proton" di query (SetTrainingCategoryViewBag). Pakai GroupBy/Distinct agar nama kategori unik. Tidak ubah data DB.

### Claude's Discretion
- Library JS: Claude pilih antara Tom Select atau Select2 berdasarkan kompatibilitas dengan jQuery yang sudah ada di project
- Detail styling dynamic rows (card vs table row vs list item)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Form & Controller
- `Views/Admin/AddTraining.cshtml` — Current form template (single select, renewal scripts)
- `Controllers/AdminController.cs` lines 5687-5963 — GET + POST AddTraining handlers
- `Models/CreateTrainingRecordViewModel.cs` — Current ViewModel (single UserId)

### Existing Bulk Pattern
- `Controllers/AdminController.cs` lines 5896-5930 — Existing bulk renewal logic (bulkUserIds + fkMap) — reusable pattern

### Category
- `Controllers/AdminController.cs` SetTrainingCategoryViewBag method — Source of kategori dropdown data

### Layout
- `Views/Shared/_Layout.cshtml` line 170-171 — jQuery 3.7.1 already loaded

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **Bulk save logic** (AdminController lines 5896-5930): Loop bulkUserIds, create TrainingRecord per user. Can be adapted for multi-select non-renewal mode.
- **jQuery 3.7.1**: Already loaded globally via _Layout.cshtml — Select2 would work without new dependency.
- **FileUploadHelper.SaveFileAsync**: Existing helper for file uploads — need to call per pekerja.

### Established Patterns
- **ViewBag.Workers**: SelectListItem list for dropdown, already populated in GET handler.
- **Hidden field approach**: Renewal mode uses hidden `UserIds` JSON field for bulk submission.

### Integration Points
- **ViewModel**: `CreateTrainingRecordViewModel.UserId` is single string — POST handler needs to accept List<string> or JSON array for multi-select.
- **Form enctype**: Already `multipart/form-data` — supports multiple file uploads.
- **POST validation**: ModelState validation on UserId needs to be conditional for multi-select.

</code_context>

<specifics>
## Specific Ideas

- Setiap pekerja harus punya file sertifikat sendiri (bukan shared)
- Dynamic rows muncul setelah pekerja dipilih di multi-select dropdown
- Per pekerja row: Nama pekerja + File upload + Nomor Sertifikat

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 253-addtraining-multi-select-pekerja-dan-perbaikan-form*
*Context gathered: 2026-03-25*
