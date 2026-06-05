# Requirements — v23.0 CMP/Records Search & Filter Consistency Audit

**Milestone:** v23.0
**Started:** 2026-06-05
**Status:** 🚀 ACTIVE
**Source:** Codebase audit `docs/superpowers/specs/2026-06-05-cmp-records-search-filter-audit.md` (7 confirmed: 1 HIGH / 4 MED / 2 LOW)
**Surfaces:** CMP/Records — My Records + Team View + Worker Detail
**Origin:** Backlog Phase 999.2 (bug UAT 2026-06-05: search "ojt v14.2" → 0 worker di Team View)

---

## v23.0 Requirements

### Search Scope — Team View (SF-01, SF-02)

- [ ] **SF-01**: Saat HC/admin search di Team View CMP/Records dengan Lingkup "Keduanya" (atau "Training"-extended), worker yang punya **assessment** ber-judul cocok ikut muncul — bukan hanya cocok Nama/NIP + judul Training. (Cari "ojt v14.2" → worker pemilik assessment itu tampil.) Predikat tambah `AssessmentSessions.Any(a => a.Title contains search)` di level worker (post-load) — **preserve REC-06 D-07** (badge count per-worker tetap utuh). `WorkerDataService.GetWorkersInSection:402-417`.
- [ ] **SF-02**: Dropdown "Lingkup" Team View punya opsi eksplisit yang mencakup pencarian Assessment (mis. "Assessment" atau relabel "Keduanya" = Nama/NIP + Training + Assessment), dan placeholder/label search jujur mencerminkan field yang benar-benar dicari (tidak menyesatkan). `RecordsTeam.cshtml` (Lingkup select + search placeholder).

### Search/Filter Feedback & Cross-Surface Consistency (SF-03, SF-05, SF-07)

- [ ] **SF-03**: Saat client-filter Worker Detail menyembunyikan semua baris (0 match), tampil pesan "Tidak ada hasil untuk filter ini." (`aria-live="polite"`) + counter "Menampilkan X dari Y" yang ikut filter aktif — bukan tabel kosong tanpa keterangan. (Reuse pola v22 MAP-07/08.) `RecordsWorkerDetail.cshtml` + filter JS.
- [ ] **SF-05**: Field search/filter di My Records dan Worker Detail konsisten — scope search selaras + set filter sebanding antar surface data-sendiri vs data-pekerja (tidak ada gap "satu surface bisa filter X, satunya tidak" tanpa alasan). `Records.cshtml` (My Records) vs `RecordsWorkerDetail.cshtml`.
- [ ] **SF-07**: Tombol "Back to Team View" di Worker Detail kembali ke state Team View yang sama — preserve param filter (`subCategory`, `dateFrom`, `dateTo`, `searchScope`), tidak hanya sebagian. `RecordsWorkerDetail.cshtml` back-link.

### Filter Correctness & Export Parity (SF-04, SF-06)

- [ ] **SF-04**: Filter Kategori di Worker Detail mencocokkan kategori **record aktual** (assessment + training rows), bukan hanya exact-equals terhadap master `AssessmentCategories` — sehingga record free-text/legacy tetap terfilter benar, dan opsi dropdown tidak menyertakan kategori mati (yang tak punya record). `RecordsWorkerDetail.cshtml` filter Kategori + `GetUnifiedRecords`.
- [ ] **SF-06**: Export Team View (Assessment + Training) menghasilkan data identik dengan yang tampil di tabel on-screen (WYSIWYG) — terapkan search/filter/scope yang sama; Export Assessment tidak kosong saat user search judul assessment (konsekuensi SF-01), dan baris assessment di-narrow per-Category setara baris training (sheet tidak asimetris). `CMPController.ExportRecordsTeamAssessment/Training` + `GetWorkersInSection`.

---

## Future Requirements (deferred)

Tidak ada — seluruh 7 temuan audit masuk scope v23.0.

## Out of Scope (by-design — JANGAN re-fix)

- **Team View = agregat per-worker** (REC-06 D-07 Phase 346) — search memfilter *worker mana yang muncul*, badge/count per-worker tetap utuh. SF-01 wajib mempertahankan invariant ini (filter di level worker, bukan per-record).
- **SubKategori filter = Training-only** — Assessment tak punya sub-kategori; bukan gap.
- **Mekanisme asimetris** Nama-via-SQL-pre-narrow vs Training/Assessment-via-post-load-filter — disengaja (performa + badge-count integrity), bukan bug.
- **999.1 Realtime Assessment SignalR** — di-drop user 2026-06-05.
- **Search Monitoring list by Category** — sudah ditangani v22.0 MAP-23 (page berbeda, bukan CMP/Records).

## Traceability

| REQ-ID | Sev | Phase | Surface | Status |
|--------|-----|-------|---------|--------|
| SF-01 | HIGH | Phase 350 | Team View (search predicate) | Pending |
| SF-02 | MED | Phase 350 | Team View (Lingkup dropdown + placeholder) | Pending |
| SF-06 | MED | Phase 350 | Team View Export (parity) | Pending |
| SF-03 | MED | Phase 351 | Worker Detail (0-match + counter) | Pending |
| SF-04 | MED | Phase 351 | Worker Detail (Kategori actual-match) | Pending |
| SF-05 | LOW | Phase 351 | My Records ↔ Worker Detail (parity) | Pending |
| SF-07 | LOW | Phase 351 | Worker Detail → Team View (back-nav state) | Pending |

**Mapped: 7/7 ✓ — Orphans: 0 — Duplicates: 0 — No migration — Preserves REC-06 D-07**
