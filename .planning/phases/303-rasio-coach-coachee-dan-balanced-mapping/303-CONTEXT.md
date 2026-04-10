# Phase 303: Rasio Coach-Coachee dan Balanced Mapping - Context

**Gathered:** 2026-04-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Menambahkan halaman analitik baru "Coach Workload" yang menampilkan rasio beban coach-coachee, visualisasi distribusi, saran otomatis reassign, dan tools untuk membuat mapping lebih balanced. Fitur ini membantu HC dan Admin mengetahui coach mana yang bebannya banyak dan mengambil tindakan penyeimbangan.

</domain>

<decisions>
## Implementation Decisions

### Halaman & Navigasi
- **D-01:** Halaman baru terpisah dari halaman CoachCoacheeMapping yang sudah ada — bukan embedded di halaman mapping existing
- **D-02:** Nama menu: "Coach Workload"
- **D-03:** Penempatan menu: sebelum "Deliverable Progress Override" di sidebar CMP

### Visualisasi
- **D-04:** Bar chart horizontal sebagai visualisasi utama — setiap coach = 1 bar, panjang bar = jumlah coachee, warna berdasarkan threshold (hijau/kuning/merah)
- **D-05:** Summary cards di atas: Total Coach Aktif, Total Coachee Aktif, Rata-rata Rasio, Coach Overloaded
- **D-06:** Tabel detail per coach di bawah chart: Nama Coach, Section, Jumlah Coachee, Status
- **D-07:** Filter by section (dropdown)
- **D-08:** Export Excel — tombol download data rasio ke Excel

### Definisi Beban
- **D-09:** Beban = jumlah coachee aktif (IsActive=true DAN IsCompleted=false)
- **D-10:** Coachee graduated (IsCompleted=true) tidak dihitung dalam beban

### Saran Reassign
- **D-11:** Section terpisah "Saran Penyeimbangan" di bawah chart dan tabel — list saran dengan tombol Approve/Skip per saran
- **D-12:** Saran reassign prioritaskan coach tujuan di section yang sama dengan coachee
- **D-13:** Saran butuh approval Admin — tidak otomatis dijalankan

### Auto-Suggest saat Assign Baru
- **D-14:** Saat assign coachee baru (di halaman mapping), suggest coach dengan beban terendah di section yang sama

### Threshold
- **D-15:** Threshold configurable oleh Admin — tombol button yang membuka modal popup untuk set batas maks coachee per coach
- **D-16:** Threshold tersimpan di database (bukan hardcoded)

### Akses Role
- **D-17:** Admin — full access (lihat data, set threshold, approve saran reassign)
- **D-18:** HC — read-only (lihat data rasio dan saran, tidak bisa approve atau ubah threshold)

### Notifikasi
- **D-19:** Tidak perlu notifikasi otomatis — Admin/HC cukup lihat dari halaman analitik

### Claude's Discretion
- Default value threshold awal
- Algoritma prioritas saran reassign (coachee Tahun 1 lebih prioritas dipindah, dll)
- Exact chart library/implementasi (Chart.js atau native)
- Styling dan spacing detail
- Empty state saat tidak ada data

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Existing Coach-Coachee System
- `Models/CoachCoacheeMapping.cs` — Entity model: Id, CoachId, CoacheeId, IsActive, StartDate, EndDate, AssignmentSection, AssignmentUnit, IsCompleted, CompletedAt
- `Controllers/CoachMappingController.cs` — Existing CRUD, import, assign, edit, deactivate actions
- `Views/Admin/CoachCoacheeMapping.cshtml` — Current mapping view: grouped by coach, pagination, search, section filter

### Patterns & Architecture
- `.planning/codebase/STRUCTURE.md` — Project structure, where to add new code
- `.planning/codebase/CONVENTIONS.md` — Naming conventions, patterns

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CoachMappingController.cs` — Already has section/unit queries, coach-coachee grouping logic, Excel export (ClosedXML pattern)
- `Helpers/ExcelExportHelper.cs` — ClosedXML-based Excel export helper
- `Helpers/PaginationHelper.cs` — Pagination calculation
- `AdminBaseController.cs` — Base controller for admin features (DI, routing, auth)
- Existing section/unit dropdown pattern in CoachCoacheeMapping view

### Established Patterns
- Admin controllers inherit `AdminBaseController`, views in `Views/Admin/`
- Route pattern: `[Route("Admin/[action]")]`
- ViewBag for passing data to views
- ClosedXML for Excel export
- jQuery + Bootstrap for UI interactions

### Integration Points
- `CoachCoacheeMappingAssign` action — tempat integrasi auto-suggest coach beban terendah
- CMP sidebar menu — tambah menu "Coach Workload" sebelum "Deliverable Progress Override"
- `ApplicationDbContext` — perlu DbSet baru untuk threshold config (atau tabel AppSettings)

</code_context>

<specifics>
## Specific Ideas

- Threshold setting via tombol + modal popup (bukan inline input)
- Bar chart dengan warna hijau/kuning/merah berdasarkan threshold
- Saran reassign dalam section terpisah dengan Approve/Skip per saran

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 303-rasio-coach-coachee-dan-balanced-mapping*
*Context gathered: 2026-04-10*
