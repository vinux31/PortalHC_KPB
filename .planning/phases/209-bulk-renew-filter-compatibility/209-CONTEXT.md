# Phase 209: Bulk Renew & Filter Compatibility - Context

**Gathered:** 2026-03-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Admin dapat melakukan bulk renew per group sertifikat, dan semua filter existing tetap berfungsi pada tampilan grouped. Tidak ada perubahan DB — murni frontend/JS wiring + server-side filter logic.

</domain>

<decisions>
## Implementation Decisions

### Tombol Renew per Group
- Tombol "Renew N Pekerja" muncul **di dalam header accordion** masing-masing group, bukan global
- Tombol global "Renew Selected" yang ada di atas filter bar **dihapus**
- Tombol hanya muncul saat ada checkbox tercentang di group tersebut, hilang saat tidak ada

### Select-All & Checkbox Behavior
- Checkbox di-**lock per group**: centang di group A → checkbox di group B-Z disabled
- Hapus logic lock per kategori (data-kategori) — diganti lock per group (data-group-key)
- `cb-group-select-all` checkbox di header group mencentang/uncentang semua checkbox di group-nya
- Saat navigasi pagination per group, checkbox di-**reset** (tidak persist lintas page)

### Konfirmasi Bulk Renew
- Sebelum redirect ke CreateAssessment, tampilkan **modal konfirmasi**: "Anda akan me-renew N pekerja untuk sertifikat X. Lanjutkan?"
- Jika user klik "Lanjutkan" → redirect ke CreateAssessment dengan parameter
- Jika user klik "Batal" → modal tertutup, tidak ada aksi

### Filter + Grouped View
- Group yang **semua anggotanya terfilter keluar** → group disembunyikan (tidak muncul sama sekali)
- Badge count di group header (N expired, N akan expired) **update sesuai filter aktif**
- Jika **semua group tersembunyi** karena filter → tampilkan pesan "Tidak ada sertifikat yang sesuai filter" dengan tombol "Reset Filter"

### Summary Cards
- Summary cards (Expired count, Akan Expired count) menampilkan angka dari **data terfilter** (bukan total keseluruhan)
- Mekanisme `updateSummaryCards()` dari Phase 208 sudah tersedia

### Claude's Discretion
- Implementasi detail modal konfirmasi (reuse Bootstrap modal atau inline)
- Urutan disabled/enabled saat unlock group
- Animasi hide/show group saat filter berubah

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Grouped View (Phase 208)
- `Views/Admin/RenewalCertificate.cshtml` — Main page, JS logic untuk filter, checkbox, pagination, summary cards
- `Views/Admin/Shared/_RenewalGroupTablePartial.cshtml` — Per-group table partial, sudah ada `cb-group-select-all` (belum di-wire)
- `Views/Admin/Shared/_RenewalCertificateTablePartial.cshtml` — Flat table partial (legacy, mungkin tidak dipakai di grouped view)

### Controller
- `Controllers/AdminController.cs` — `FilterRenewalCertificate`, `FilterRenewalCertificateGroup`, `RenewalCertificate` actions

### Requirements
- `.planning/REQUIREMENTS.md` — BULK-01, BULK-02, FILT-01, FILT-02

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `cb-group-select-all` checkbox sudah ada di `_RenewalGroupTablePartial.cshtml` (line 9-10) — tinggal wire JS
- `wireCheckboxes()` function sudah ada — perlu dimodifikasi dari lock-per-kategori ke lock-per-group
- `updateRenewSelectedButton()` — perlu diganti jadi per-group button update
- `renewSelected()` — perlu dimodifikasi untuk scope per-group
- `updateSummaryCards()` — sudah berfungsi, partial menyediakan hidden span dengan count
- `refreshGroupTable()` — sudah handle per-group pagination via AJAX

### Established Patterns
- AJAX partial rendering: `FilterRenewalCertificate` returns full grouped HTML, `FilterRenewalCertificateGroup` returns single group table
- Bootstrap collapse untuk accordion groups
- `data-group-key` attribute sudah ada di checkbox per row (`_RenewalGroupTablePartial.cshtml` line 29)

### Integration Points
- Tombol renew per group perlu ditambahkan di grouped view HTML (server-side partial atau JS-injected)
- Modal konfirmasi bisa reuse pattern Bootstrap modal yang sudah ada (certificateHistoryModal)
- Filter logic di server (`FilterRenewalCertificate`) sudah handle filter params — perlu pastikan group yang kosong tidak di-return

</code_context>

<specifics>
## Specific Ideas

- User menegaskan: pagination per group, jadi reset checkbox saat ganti page per group adalah natural
- Modal konfirmasi sebelum bulk renew — user ingin ada langkah konfirmasi, tidak langsung redirect

</specifics>

<deferred>
## Deferred Ideas

None — diskusi tetap dalam scope phase.

</deferred>

---

*Phase: 209-bulk-renew-filter-compatibility*
*Context gathered: 2026-03-20*
