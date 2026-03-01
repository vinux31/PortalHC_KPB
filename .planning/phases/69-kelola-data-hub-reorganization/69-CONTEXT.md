# Phase 70: Kelola Data Hub Reorganization - Context

**Gathered:** 2026-02-28
**Updated:** 2026-02-28
**Status:** Ready for planning

<domain>
## Phase Boundary

Restructure Admin/Index.cshtml hub page — reorganize into 3 domain-focused sections, activate implemented features, remove stale placeholders, update navbar visibility for HC. Navigation and access only — no new features.

</domain>

<decisions>
## Implementation Decisions

### Section Reorganization (3 sections)
- **Section A: Data Management** (badge biru/primary) — 4 card:
  1. Manajemen Pekerja (aktif, href ManageWorkers)
  2. KKJ Matrix (aktif, href KkjMatrix)
  3. KKJ-IDP Mapping (aktif, href CpdpItems)
  4. Silabus & Coaching Guidance (aktif, href ProtonData/Index)
- **Section B: Proton** (badge kuning/warning) — 3 card:
  1. Coach-Coachee Mapping (aktif, href CoachCoacheeMapping)
  2. Deliverable Progress Override (**aktifkan**: hapus opacity-75, hapus badge "Segera", href `/ProtonData/OverrideList`)
  3. Coaching Session Override (tetap placeholder "Segera", Phase 54 belum dibangun)
- **Section C: Assessment & Training** (badge hijau/success) — 2 card:
  1. Manage Assessments (aktif, href ManageAssessment — pindah dari Section A lama)
  2. Final Assessment Manager (tetap placeholder "Segera", Phase 53 belum dibangun)

### Old Section C Removal
- **Hapus seluruh Section C lama** (Kelengkapan CRUD) — semua 4 card redundan:
  - Question Bank Edit → sudah ada di Manage Assessment page
  - Package Question Edit/Delete → sudah ada di Manage Assessment page
  - ProtonTrack Edit/Delete → identik dengan Silabus & Coaching Guidance page
  - Password Reset → sudah ada di ManageWorkers EditWorker
- Semua card punya href="#" (placeholder), tidak pernah aktif — tidak perlu redirect
- Section C header lama + 4 card dihapus total

### HC Navbar Visibility
- Menu "Kelola Data" di navbar **ditampilkan untuk HC** (ubah condition di _Layout.cshtml dari `userRole == "Admin"` ke `userRole == "Admin" || userRole == "HC"`)
- HC dan Admin lihat hub identik — semua card, semua section, tanpa filter per role
- Label dan icon menu tetap sama: "Kelola Data" dengan bi-gear-fill

### Hub Page Header
- Judul tetap: "Kelola Data"
- Subtitle update: "Panel untuk mengelola data master, proton, dan assessment & training."

### Deliverable Progress Override Activation
- Hapus opacity-75 (card tidak abu-abu lagi)
- Hapus badge "Segera"
- Ganti href="#" ke href="/ProtonData/OverrideList" (route sudah aktif dari Phase 52)
- Deskripsi card tetap: "Override status progress deliverable"
- Icon tetap: bi-pencil-square

### Claude's Discretion
- Exact card description wording adjustments (minor copy)
- HR separator styling between sections
- Any minor spacing/layout adjustments

</decisions>

<specifics>
## Specific Ideas

- Section names chosen to reflect domain grouping: Data Management (master data umum), Proton (coaching & deliverable operations), Assessment & Training (exam management)
- Silabus & Coaching Guidance stays in Data Management (Section A) because it's master data, not operational
- Badge warna mengikuti existing convention: A=primary, B=warning, C=success

</specifics>

<deferred>
## Deferred Ideas

- **Pisahkan monitoring vs management assessment** — monitoring assessment dan history/list assessment & training berbeda page. Scope: perlu discuss lebih lanjut
- **CMP/Records migrasi ke hub** — halaman Records perlu masuk Kelola Data. Scope: fase baru
- **Import Excel pattern reuse** — user suka pattern import ManageWorkers, ingin terapkan ke hal lain
- **Assessment & Training section expansion** — saat ini hanya 2 card, nanti bisa bertambah setelah Phase 53/61 selesai (Question Bank, Package Question management cards)

</deferred>

---

*Phase: 70-kelola-data-hub-reorganization*
*Context gathered: 2026-02-28*
