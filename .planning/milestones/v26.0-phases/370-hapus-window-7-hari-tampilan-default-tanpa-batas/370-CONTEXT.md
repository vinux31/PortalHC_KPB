# Phase 370: Hapus Window 7-Hari (Tampilan Default Tanpa Batas) - Context

**Gathered:** 2026-06-11
**Status:** Ready for planning

<domain>
## Phase Boundary

Tampilan default Tab Assessment (`ManageAssessmentTab_Assessment`) + `AssessmentMonitoring` menampilkan SEMUA sesi tanpa batas umur — filter `sevenDaysAgo` dihapus sepenuhnya. Helper `ApplySevenDayWindow` (quick task 260611-m9r) di-retire + test disesuaikan. Filter status default "Aktif (Open/Upcoming)" + hide-Closed CIL-02 TETAP. Search behavior 260611-m9r tidak regresi. REQ: URG-02. Migration=false. Query-layer only — view TIDAK berubah.

</domain>

<decisions>
## Implementation Decisions

### Nasib helper + test
- **D-01:** Helper `ApplySevenDayWindow` **hapus total** — definisi (`AssessmentAdminController.cs:2820-2826`) + 2 call site (`:123`, `:2873`) + var `sevenDaysAgo` (`:116`, `:2870`). Komentar 260611-m9r di kedua method dibersihkan/diperbarui (termasuk komentar 90-review lama "7-day window is intentional" di AssessmentMonitoring — sudah tidak berlaku).
- **D-02:** File `HcPortal.Tests/AssessmentSearchWindowTests.cs` (3 [Fact] uji helper) **dihapus utuh** — helper hilang, test tak bisa compile. Tidak di-repurpose.

### Badge counter status (CIL-01)
- **D-03:** Badge counter (`ViewBag.OpenCount/UpcomingCount/ClosedCount`) **biarkan all-time** — tanpa window, ClosedCount mencakup semua sesi historis (angka besar = jujur, konsisten dengan list yang tampil saat filter Closed dipilih; badge = row count). Zero kode ekstra.

### Guard regresi
- **D-04:** Pengganti 3 test lama = **grep-guard + UAT** — verifikasi plan: grep zero sisa `sevenDaysAgo`/`ApplySevenDayWindow` di `ManageAssessmentTab_Assessment` + `AssessmentMonitoring` (dan codebase) + full suite hijau + UAT @5277 (sesi >7 hari tampil di default view, search tetap jalan, pagination Tab Assessment jalan). TIDAK ada test unit/integration baru — perilaku "tidak ada filter" tidak bermakna diuji unit (controller butuh DbContext).

### Scope tambahan (opportunistic)
- **D-05:** `AssessmentMonitoring` query ditambah `.AsNoTracking()` (`:2872`) — 1 baris, read-only method (tidak ada SaveChanges), selaras pola Phase 311 di Tab Assessment. Makin relevan karena row bertambah tanpa window.

### UAT
- **D-06:** UAT pakai **data legacy existing** DB lokal (12 InProgress + 9 Open legacy + Post Test OJT >7 hari) — zero seed, zero snapshot/restore (read-only verification).

### Claude's Discretion
- Wording komentar pengganti di 2 method (jejak keputusan Phase 370 boleh 1 baris singkat atau tanpa komentar — yang penting komentar stale 260611-m9r/90-review hilang).
- Urutan langkah edit vs hapus test (kompilasi tetap hijau di tiap commit).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Quick task asal helper (perilaku yang di-retire)
- `.planning/quick/260611-m9r-fix-search-blind-spot-7-day-window-searc/260611-m9r-SUMMARY.md` — helper `ApplySevenDayWindow` + 2 call site + 3 test; perilaku search-tembus-window yang TIDAK boleh regresi
- `.planning/quick/260611-m9r-fix-search-blind-spot-7-day-window-searc/260611-m9r-PLAN.md` — lokasi line persis call site + pola wiring

### Preseden filter status (TETAP berlaku)
- `.planning/ROADMAP.md` §Phase 370 — SC lengkap + trade-off yang sudah diterima user
- Phase 338 CIL-01/CIL-02 (badge counter + hide-Closed default hanya saat status & search kosong) — implementasi live di `Controllers/AssessmentAdminController.cs:205-215` (Tab Assessment) + `:3030-3052` (Monitoring, termasuk MAP-15 status="All" saat search)

</canonical_refs>

<code_context>
## Existing Code Insights

### Target edit
- `Controllers/AssessmentAdminController.cs:116,123` — `ManageAssessmentTab_Assessment`: var `sevenDaysAgo` + call `ApplySevenDayWindow` (hapus keduanya)
- `Controllers/AssessmentAdminController.cs:2870,2873` — `AssessmentMonitoring`: idem + tambah `.AsNoTracking()` di `:2872`
- `Controllers/AssessmentAdminController.cs:2816-2826` — definisi helper (hapus blok)
- `HcPortal.Tests/AssessmentSearchWindowTests.cs` — hapus file

### Established Patterns
- CIL-02: hide-Closed default HANYA saat statusFilter & search dua-duanya kosong — TIDAK disentuh fase ini
- CIL-01: badge counter dihitung SEBELUM filter status apply — tetap, jadi all-time (D-03)
- MAP-15: search non-empty → dropdown status "All" di Monitoring — tetap
- Pagination Tab Assessment via `PaginationHelper.Calculate` in-memory post-grouping — tetap jalan, hanya datasetnya membesar

### Integration Points
- Kedua method `[Authorize(Roles = "Admin, HC")]`; Tab Assessment = HTMX partial (Phase 311), Monitoring = full view
- Grouping in-memory (ToListAsync lalu GroupBy) — tanpa window seluruh tabel AssessmentSessions di-load; diterima di skala saat ini (SC3, 58 row lokal)

### Koordinasi sesi paralel
- Dependency 363 SUDAH terpenuhi (363 completed 2026-06-11) — `AssessmentAdminController.cs` aman diedit
- Phase 366/367 (belum jalan) juga sentuh file ini — 370 selesaikan dulu commit-nya supaya line stability terjaga

</code_context>

<specifics>
## Specific Ideas

- Keputusan user 2026-06-11 verbatim: "7 hari jadi tanpa batas" — window dihapus PENUH, bukan diperpanjang/configurable.
- Trade-off DITERIMA user: sesi Open/InProgress terbengkalai lama ikut tampil di default view (lokal: 12 InProgress + 9 Open legacy).

</specifics>

<deferred>
## Deferred Ideas

- **Pagination AssessmentMonitoring** — Monitoring render semua group tanpa paging; tanpa window halaman memanjang seiring waktu. Tambah paging = kapabilitas baru + ubah view (roadmap 370: view tak berubah). Kandidat fase perf/UX nanti kalau row membengkak. Mitigasi sementara: default filter Aktif menyembunyikan Closed.

</deferred>

---

*Phase: 370-hapus-window-7-hari-tampilan-default-tanpa-batas*
*Context gathered: 2026-06-11*
