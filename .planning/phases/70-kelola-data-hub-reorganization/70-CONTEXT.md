# Phase 70: Kelola Data Hub Reorganization - Context

**Gathered:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Restructure Admin/Index.cshtml hub page — activate implemented features, remove stale placeholders, reorganize sections. Navigation and access only — no new features.

</domain>

<decisions>
## Implementation Decisions

### Stale Cards Cleanup
- Deliverable Progress Override (Phase 52) — **aktifkan card**: hapus opacity-75, hapus badge "Segera", tambah href ke `/ProtonData/OverrideList`
- Final Assessment Manager — tetap sebagai card "Segera" (Phase 53 belum dibangun)
- Coaching Session Override — tetap sebagai card "Segera" (Phase 54 belum dibangun)

### Section C Removal
- **Hapus seluruh Section C** (Kelengkapan CRUD) — semua 4 card redundan:
  - Question Bank Edit → sudah ada di Manage Assessment page
  - Package Question Edit/Delete → sudah ada di Manage Assessment page
  - ProtonTrack Edit/Delete → identik dengan Silabus & Coaching Guidance page (SilabusSave/SilabusDelete sudah ada)
  - Password Reset → sudah ada di ManageWorkers EditWorker
- Section C header + 4 card dihapus total

### Section Structure
- Section A (Master Data) — tetap seperti sekarang (5 card aktif)
- Section B (Operasional) — update: Deliverable Progress Override diaktifkan, sisanya tetap
- Section C — dihapus
- Hub jadi 2 section saja: A + B

### HC Visibility
- HC sudah bisa akses hub (/Admin/Index) setelah Phase 69 auth fix
- Semua card di hub visible untuk Admin dan HC (sama persis, tidak perlu filter per role)
- Menu "Kelola Data" di navbar **perlu ditampilkan untuk HC** (saat ini hanya Admin) — cek _Layout.cshtml condition

### Card Ordering
- Section A: Manajemen Pekerja (pertama, sudah dari Phase 69), KKJ Matrix, KKJ-IDP Mapping, Manage Assessments, Silabus & Coaching Guidance
- Section B: Coach-Coachee Mapping, Deliverable Progress Override (aktifkan), Final Assessment Manager (Segera), Coaching Session Override (Segera)

</decisions>

<specifics>
## Specific Ideas

- User ingin di masa depan: section baru khusus "Assessment & Training" yang menggabungkan Manage Assessment, Question Bank, Package Question — tapi ini scope Phase 53/60/61, bukan Phase 70
- User ingin pisahkan monitoring assessment vs management assessment — juga scope fase selanjutnya
- CMP/Records page (Capability Building Records) juga perlu migrasi ke hub — noted untuk fase selanjutnya

</specifics>

<deferred>
## Deferred Ideas

- **Section "Assessment & Training"** — gabungkan assessment + question bank + package di section baru. Scope: Phase 53/61
- **Pisahkan monitoring vs management assessment** — monitoring assessment dan history/list assessment & training berbeda page. Scope: perlu discuss lebih lanjut
- **CMP/Records migrasi ke hub** — halaman Records perlu masuk Kelola Data. Scope: fase baru
- **Import Excel pattern reuse** — user suka pattern import ManageWorkers, ingin terapkan ke hal lain

</deferred>

---

*Phase: 70-kelola-data-hub-reorganization*
*Context gathered: 2026-02-28*
