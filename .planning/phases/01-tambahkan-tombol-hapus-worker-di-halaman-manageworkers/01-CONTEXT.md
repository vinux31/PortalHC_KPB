# Phase 1: Tambahkan tombol hapus worker di halaman ManageWorkers - Context

**Gathered:** 2026-03-26
**Status:** Ready for planning

<domain>
## Phase Boundary

Menambahkan tombol hapus (delete) di tabel ManageWorkers yang memanggil endpoint `DeleteWorker` yang sudah ada di AdminController. Tidak ada perubahan backend — hanya UI.

</domain>

<decisions>
## Implementation Decisions

### Visibilitas Tombol
- **D-01:** Tombol hapus hanya muncul untuk user yang sudah di-deactivate (IsActive == false). Flow: deactivate dulu → baru bisa hapus.
- **D-02:** Tombol hapus menggantikan posisi tombol "Aktifkan Kembali" tidak — tombol hapus ditambahkan di samping tombol Reactivate dalam btn-group yang sudah ada.

### Konfirmasi
- **D-03:** Gunakan `confirm()` JS native (konsisten dengan pattern `confirmDeactivate` yang sudah ada). Pesan harus jelas bahwa ini hapus permanen beserta semua data terkait.

### Akses Role
- **D-04:** Kedua role Admin dan HC bisa melihat dan menggunakan tombol hapus (konsisten dengan endpoint `[Authorize(Roles = "Admin, HC")]`).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Backend Endpoint
- `Controllers/AdminController.cs` line 5059-5178 — `DeleteWorker` action (POST, cascade delete semua data terkait, audit log)

### View Template
- `Views/Admin/ManageWorkers.cshtml` line 266-292 — btn-group pattern (Edit + Deactivate/Reactivate), confirmDeactivate JS function

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `confirmDeactivate()` JS function di ManageWorkers.cshtml — pattern konfirmasi yang bisa diikuti untuk confirmDelete
- btn-group btn-group-sm pattern — tombol hapus ditambahkan dalam group yang sama

### Established Patterns
- Form POST dengan AntiForgeryToken + hidden input id (lihat DeactivateWorker form)
- `confirm()` native untuk konfirmasi aksi destruktif
- TempData["Error"] / TempData["Success"] untuk feedback setelah redirect

### Integration Points
- Tombol hapus ditambahkan di dalam block `else` (user non-aktif) pada baris 283-291, di samping form ReactivateWorker

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

*Phase: 01-tambahkan-tombol-hapus-worker-di-halaman-manageworkers*
*Context gathered: 2026-03-26*
