# Phase 277: Delete Peserta Assessment di EditAssessment - Context

**Gathered:** 2026-04-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Menambahkan fitur hapus peserta assessment (satu AssessmentSession) langsung dari halaman EditAssessment. Termasuk kolom status assessment di tabel peserta dan guard agar hanya peserta yang belum mulai ujian yang bisa dihapus.

</domain>

<decisions>
## Implementation Decisions

### Delete guard logic
- **D-01:** CanDelete = false jika `StartedAt != null` ATAU `CompletedAt != null` ATAU `Status == "Completed"`. Tidak ada status "InProgress"/"Abandoned"/"Cancelled" di codebase — guard cukup berbasis field datetime + status "Completed".
- **D-02:** Hard delete satu AssessmentSession beserta semua data turunan (PackageUserResponses, AssessmentAttemptHistory, AssessmentPackages + nested, UserPackageAssignments via cascade). Bukan soft delete.

### Confirmation UX
- **D-03:** Pakai browser `confirm()` sederhana, konsisten dengan DeleteAssessment dan DeleteAssessmentGroup yang sudah ada.

### Delete scope
- **D-04:** Delete satu-per-satu saja (per row). Tidak ada bulk delete / checkbox select. Bulk sudah ter-cover oleh DeleteAssessmentGroup existing.

### Redirect setelah delete
- **D-05:** Jika session yang dihapus bukan session yang sedang dibuka → redirect ke EditAssessment session yang sedang dibuka.
- **D-06:** Jika session yang dihapus adalah session yang sedang dibuka → cari sibling lain, redirect ke sibling tersebut.
- **D-07:** Jika peserta terakhir di grup dihapus → redirect ke ManageAssessment dengan success message.

### Tabel peserta
- **D-08:** Tambah kolom "Status Assessment" dan kolom "Actions" (tombol delete) pada tabel Currently Assigned Users.
- **D-09:** Badge status: Open (hijau), Upcoming (biru), Completed (abu). Fallback badge netral untuk status lain.
- **D-10:** Row yang tidak eligible delete → tombol disabled.

### Claude's Discretion
- Warna badge exact dan styling tombol delete
- Pesan error exact saat delete diblok
- Audit log format detail

</decisions>

<specifics>
## Specific Ideas

- Pola delete data turunan ikuti pattern DeleteAssessment existing (lines 2067-2159 di AdminController.cs) — urutan hapus sudah proven safe.
- Tabel peserta existing sudah ada di EditAssessment.cshtml lines 326-370, tinggal diperluas.

</specifics>

<canonical_refs>
## Canonical References

No external specs — requirements are fully captured in decisions above.

### Existing code references
- `Controllers/AdminController.cs` lines 1741-1812 — GET EditAssessment, cara load assigned users + siblings
- `Controllers/AdminController.cs` lines 2067-2159 — DeleteAssessment, pola delete data turunan yang harus diikuti
- `Views/Admin/EditAssessment.cshtml` lines 326-370 — Tabel assigned users existing yang akan diperluas

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **DeleteAssessment action**: Pola delete cascade (PackageUserResponses → AttemptHistory → Packages+Questions+Options → Session) sudah ada dan bisa di-reuse/extract
- **Sibling query**: Pattern `same Title + Category + Schedule.Date` sudah ada di GET EditAssessment

### Established Patterns
- Delete actions pakai `[HttpPost]` + `[ValidateAntiForgeryToken]` + `[Authorize(Roles = "Admin, HC")]`
- Audit log via pattern existing di DeleteAssessment
- TempData["Success"] / TempData["Error"] untuk flash messages

### Integration Points
- ViewBag.AssignedUsers perlu diperluas dengan SessionId, Status, CanDelete
- Form POST delete kecil per-row di tabel peserta

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 277-coba-check-dan-tambahkan-fitur-delete-peserta-assessment-pada-page-editassessment*
*Context gathered: 2026-04-01*
