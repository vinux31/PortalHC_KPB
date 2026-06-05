---
title: CMP/Records Search & Filter Audit (3 surfaces)
date: 2026-06-05
type: audit
status: confirmed-findings
surfaces_audited:
  - "My Records tab (CMP/Records → Views/CMP/Records.cshtml)"
  - "Team View tab (RecordsTeamPartial + Export* → Views/CMP/RecordsTeam.cshtml + _RecordsTeamBody.cshtml, backed by WorkerDataService.GetWorkersInSection)"
  - "Worker Detail (RecordsWorkerDetail → Views/CMP/RecordsWorkerDetail.cshtml, backed by WorkerDataService.GetUnifiedRecords)"
backing_service: Services/WorkerDataService.cs (GetWorkersInSection, GetUnifiedRecords, GetAllWorkersHistory)
prior_art_constraints:
  - "REC-06 D-07 (Phase 346): Team search scoped to Nama/Training deliberately; per-worker badge count must stay utuh (post-load worker filter, never mutate counts)."
  - "v22.0 MAP-23: added Category to Monitoring page search — reference pattern for broadening scope honestly."
summary_counts:
  HIGH: 1
  MED: 4
  LOW: 2
---

# CMP/Records Search & Filter Audit

Audit perilaku **search & filter** di 3 surface CMP/Records untuk menemukan gap di mana user
tidak menemukan data yang seharusnya muncul. Semua temuan di bawah **code-verified** (dibaca dari
source, bukan spekulatif). Provenance pakai `file:line`.

Konvensi label: `VERIFIED` = dibaca langsung dari kode. Tidak ada klaim `ASSUMED` yang dipromosikan
jadi finding.

---

## 1. Surface-by-surface search/filter map

### 1.1 My Records tab — `Views/CMP/Records.cshtml` (client-side JS, data dari `GetUnifiedRecords(user.Id)`)

| Field | Label / name | Scope (apa yang dicocokkan) | Mechanism | file:line | Consistent? |
|---|---|---|---|---|---|
| Cari | `#searchInput`, placeholder "Cari berdasarkan judul..." | `data-title` = `item.Title.ToLower()` — **mencakup judul Assessment DAN Training** (unified table, tiap row punya data-title) | Client JS `filterTable()` `title.includes(searchTerm)` | view `Records.cshtml:59`, `:168`, `:341-344` | OK (cover 2 tipe) |
| Tahun | `#yearFilter` (+ quick-button group 3 tahun + Semua) | `data-year` = `item.Date.Year`, exact `year === yearFilter` | Client JS | `:63-77`, `:168`, `:345` | n/a (unik) |
| Reset | `clearFilters()` | reset search + year, hapus sessionStorage | Client JS | `:82`, `:394-402` | — |

Feedback: counter live di 4 badge (total + assessment + training + total stat) `:355-363`. Empty-state
disuntik JS saat 0-match `:366-379`. **Tidak ada** `aria-live` di badge counter. **Tidak ada** filter
Kategori / SubKategori / Tipe / Status / Tanggal di My Records (hanya Search + Tahun).

### 1.2 Team View tab — `Views/CMP/RecordsTeam.cshtml` + `RecordsTeamPartial` (server-side, `GetWorkersInSection`)

| Field | Label / name | Scope (apa yang dicocokkan) | Mechanism | file:line | Consistent? |
|---|---|---|---|---|---|
| Bagian | `#sectionFilter` | `u.Section == section` | Server EF | view `:19-48`; svc `WorkerDataService.cs:249-250` | n/a |
| Unit | `#unitFilter` | `u.Unit == unitFilter` | Server EF | view `:49-54`; svc `:252-253` | n/a |
| Category | `#categoryFilter` | narrow worker: `TrainingRecords.Kategori` **OR** `AssessmentSessions.Category` (case-insensitive equals) | Server in-memory | view `:55-60`; svc `:373-381` | partial (lihat SF-06) |
| Sub Category | `#subCategoryFilter` (disabled bila parent tak punya children) | **hanya** `TrainingRecords.SubKategori` equals — Assessment **tidak** punya subcat | Server in-memory | view `:61-66`; svc `:384-390` | by-design (assessment no subcat) |
| Status | `#statusFilter` ALL/Sudah/Belum | `CompletionPercentage == 100` (Sudah) / `!= 100` (Belum) — basis **training completion**, bukan assessment | Server in-memory | view `:70-77`; svc `:393-399` | n/a |
| Tanggal Awal/Akhir | `#dateFrom`/`#dateTo` | SQL date filter ke Training (`TanggalMulai ?? Tanggal`) + Assessment (`CompletedAt ?? Schedule`); worker tanpa record di rentang di-skip | Server EF + in-memory | view `:78-85`; svc `:275-290`, `:321-323` | n/a |
| **Cari** | `#teamSearch`, placeholder "Cari nama/NIP atau judul training..." | **Nama**: FullName/NIP. **Training**: `TrainingRecords.Judul`. **Keduanya**: Nama/NIP ∪ `TrainingRecords.Judul`. **Assessment title TIDAK PERNAH dicari di scope manapun.** | Server: "Nama" = SQL pre-narrow `:257-264`; "Training"/"Keduanya" = post-load in-memory `:402-417` | view `:92-97`; svc `:255-264`, `:401-417` | **GAP — SF-01/SF-02** |
| Lingkup | `#searchScope` Nama/Training/Keduanya (default Keduanya) | switch untuk Cari di atas | Server | view `:98-105` | **no "Assessment" option — SF-02** |
| Reset | `resetTeamFilters()` | reset semua, scope balik "Keduanya" | Client JS → re-fetch | view `:86-90`, `:434-454` | — |

Feedback: counter "Showing N workers" `:128-131` + paginationInfo "Page X dari Y (N workers)"
`:252`. Date hint `aria-live="polite"` `:132-134`, inverted-range warning `:348-356`. Empty-state
partial `:3-11`. Loading spinner `:137-141`. SubCategory disabled-state benar `:456-473`.

### 1.3 Worker Detail — `Views/CMP/RecordsWorkerDetail.cshtml` (client-side JS, data dari `GetUnifiedRecords(workerId)`)

| Field | Label / name | Scope | Mechanism | file:line | Consistent? |
|---|---|---|---|---|---|
| Cari | `#searchInput`, placeholder "Cari berdasarkan judul..." | `data-title` = `item.Title.ToLower()` — **cover Assessment + Training** | Client JS `title.includes` | view `:134`, `:214`, `:350` | OK |
| Kategori | `#categoryFilter` (opsi = master AssessmentCategories) | `data-category` = `Kategori?.ToLower()`, **exact equals** `category === categoryFilter` | Client JS | view `:137-149`, `:216`, `:352` | **SF-04** (exact + dead-option) |
| Sub Kategori | `#subCategoryFilter` (disabled bila no children) | `data-subcategory` exact equals | Client JS | view `:150-155`, `:217`, `:353` | n/a |
| Tahun | `#yearFilter` | `data-year` exact | Client JS | view `:156-165`, `:351` | n/a |
| Tipe | `#typeFilter` Assessment/Training | `data-type` = `RecordType.ToLower()` exact | Client JS | view `:166-173`, `:218`, `:354` | n/a (absent di My Records — SF-05) |
| Reset | `clearFilters()` | reset 5 field | Client JS | view `:174-178`, `:380-388` | — |

Feedback: **TIDAK ADA** result counter, **TIDAK ADA** 0-match message setelah JS filter (server-side
empty-state `:200-208` hanya muncul kalau `unifiedRecords` kosong dari awal). `filterTable()` cuma
toggle `display` `:343-357` — **SF-03**.

---

## 2. Confirmed findings

| ID | Sev | Surface | Gap class | Description | file:line | Suggested fix direction | Preserves D-07? |
|---|---|---|---|---|---|---|---|
| **SF-01** | **HIGH** | Team View | Search scope gap (999.2) | Cari dengan Lingkup "Training" / "Keduanya" hanya match `TrainingRecords.Judul` + Nama/NIP. **Judul Assessment tidak pernah dicocokkan.** User cari judul assessment ("ojt v14.2") → 0 worker meski worker punya assessment itu. Root cause: blok post-load `:402-417` hanya cek `t.Judul`, dan SQL pre-narrow `:257-264` hanya FullName/NIP. Test "Keduanya" (`WorkerDataServiceSearchTests.cs:72-83`) cuma validasi Nama∪Training — buktikan assessment-title memang tak ter-cover. | `WorkerDataService.cs:402-417` (+`:257-264`) | Tambah `w.AssessmentSessions.Any(a => a.Title contains search)` ke predikat "Training"/"Keduanya" (pola sama spt `:378-380` yg sudah union Assessment.Category untuk Category filter). **Wajib**: tetap post-load worker-level filter (jangan sentuh badge count) → preserves D-07. Pertimbangkan rename scope "Training"→"Judul" + tambah opsi/perilaku assessment. | YES (filter worker, count utuh) |
| **SF-02** | MED | Team View | Feedback/UX + scope gap | Dropdown Lingkup hanya Nama/Training/Keduanya — **tidak ada opsi "Assessment"**, dan "Keduanya" berimplikasi "kedua jenis record" padahal artinya cuma Nama∪Training. Placeholder "...atau judul training" mengonfirmasi assessment memang tak dicari, jadi user tak punya jalan apa pun untuk cari judul assessment di Team View. Sibling SF-01 (UI side). | `RecordsTeam.cshtml:92-105` (placeholder `:96`, options `:100-104`) | Setelah SF-01 diperbaiki: relabel scope (mis. Nama / Judul Kegiatan / Keduanya) + update placeholder supaya jujur mencakup assessment. Referensi pola MAP-23 (broaden scope + label jujur). | YES |
| **SF-03** | MED | Worker Detail | Feedback/UX edge | `filterTable()` menyembunyikan baris tanpa **counter** dan tanpa **pesan 0-match**. Saat semua baris ter-filter habis, tabel tampak kosong tanpa keterangan (empty-state `:200-208` hanya server-side, hanya jika data awal kosong). Beda dengan My Records yg suntik empty-state `Records.cshtml:366-379`, dan Team View yg punya "Showing N workers". | `RecordsWorkerDetail.cshtml:336-358` (tidak ada counter/empty inject) | Tambahkan visible counter (mis. "Menampilkan N dari M") + inject baris "Tidak ada data sesuai filter" saat visibleCount==0, mirror pola My Records. Tambah `aria-live` untuk SR. | YES (view-only) |
| **SF-04** | MED | Worker Detail | Filter correctness | Filter Kategori pakai **exact-equality** `category === categoryFilter` (`:352`), bukan contains. Opsi dropdown diambil dari master `AssessmentCategories`, tapi `data-category` berasal dari `TrainingRecords.Kategori` / `AssessmentSessions.Category` (bisa free-text/legacy). Jika nilai Kategori record tidak sama persis dengan nama master → record itu **tak bisa difilter** dan/atau ada **opsi mati** (master yg tak punya record). SubKategori menanggung risiko sama. Team View juga exact-equals (`:373-381`) tapi di sana opsi & data berasal lebih konsisten — risiko utama di Worker Detail karena master vs free-text. | `RecordsWorkerDetail.cshtml:352-353`, opsi `:140-148` | Bangun opsi Kategori dari nilai yang benar-benar ada di `unifiedRecords` (distinct), atau samakan nilai saat seed/simpan. Konfirmasi apakah Kategori record dijamin = master name (kalau ya, severity turun ke LOW). | YES |
| **SF-05** | LOW | My Records vs Worker Detail | Cross-surface inconsistency | My Records (`GetUnifiedRecords(user.Id)`) hanya punya Search + Tahun. Worker Detail (`GetUnifiedRecords(workerId)`, data shape identik) punya Search + Kategori + SubKategori + Tahun + Tipe. User melihat data diri sendiri justru lebih sedikit alat filter dibanding saat melihat record orang lain. | `Records.cshtml:54-93` vs `RecordsWorkerDetail.cshtml:128-181` | Pertimbangkan menambah Kategori/Tipe ke My Records agar paritas (data sudah tersedia di `UnifiedTrainingRecord`). Low karena bukan kehilangan data, hanya keterbatasan alat. | YES |
| **SF-06** | MED | Team View Export | Export parity | `ExportRecordsTeamAssessment` mem-pre-filter worker-list via `GetWorkersInSection(...searchScope)` (`:670`) lalu `GetAllWorkersHistory(... category: null)` (`:677`) — sehingga (a) gara-gara SF-01, search judul-assessment → 0 worker → **export assessment kosong** walau on-screen badge worker menampilkan assessment; (b) baris assessment di export **tidak** di-narrow per-`category` (hanya worker yg di-narrow), sedangkan `ExportRecordsTeamTraining` mem-filter baris training by category (`:729-730`). Akibatnya scope baris kedua sheet tidak simetris terhadap filter Category. | `CMPController.cs:669-680` (assessment), `:721-732` (training) | Setelah SF-01: search assessment-title akan ikut mengembalikan worker yg benar. Untuk simetri Category, dokumentasikan/keputusan: assessment sengaja tak punya kolom Category row-level (lihat by-design #B) — bila tidak, terapkan filter Category konsisten. | YES |
| **SF-07** | LOW | Worker Detail → Team View nav | Feedback/UX (state loss) | Tombol "Back to Team View" + breadcrumb hanya round-trip `section/unit/category/statusFilter/search`. **subCategory, dateFrom, dateTo, searchScope hilang** saat kembali — user harus set ulang rentang tanggal & scope. Bukan gap "tak menemukan data", tapi konteks filter hilang. | `RecordsWorkerDetail.cshtml:27-47` | Tambahkan asp-route untuk subCategory/dateFrom/dateTo/searchScope, atau andalkan sessionStorage Team filter (`cmp-records-team-filter`) yang sudah restore otomatis `RecordsTeam.cshtml:305-327` — verifikasi restore menang atas query-string. | YES |

**Counts: 1 HIGH / 4 MED / 2 LOW.**

Effort sizing: **S (quick)** — SF-01, SF-02, SF-03, SF-05, SF-07. **M (lebih besar / butuh keputusan data)** — SF-04 (audit nilai Kategori real vs master), SF-06 (keputusan simetri export + tergantung SF-01).

---

## 3. Out of scope / by-design (jangan di-refix)

- **A. Team View = agregat per-worker, bukan per-record.** Tabel Team menampilkan badge count (Assessment Lulus / Training) per worker; search "menyaring worker yang muncul, jumlah badge per worker tetap utuh" — ini **REC-06 D-07 (Phase 346)** yang disengaja. Setiap perbaikan SF-01 **wajib** tetap filter di level worker (post-load), tidak boleh mengubah angka badge. (`RecordsTeam.cshtml:107`, svc `:401`).
- **B. SubKategori di Team/Worker Detail hanya menyentuh Training.** `AssessmentSessions` tidak punya kolom SubKategori; filter subcat sengaja `TrainingRecords.SubKategori` saja (`WorkerDataService.cs:384-390`). Bukan bug.
- **C. Status filter Team (Sudah/Belum) berbasis training completion %.** Bukan status assessment. Ini definisi `CompletionPercentage` yang ada (`:353-367`, `:393-399`) — settled, bukan gap search.
- **D. "Nama" scope pakai SQL pre-narrow, "Training"/"Keduanya" pakai post-load.** Asimetri mekanisme ini disengaja (D-07) agar training-only match tidak terbuang oleh pre-narrow nama (`:255-264` vs `:401-417`). Jangan "menyeragamkan" jadi semua-SQL tanpa mempertahankan invariant.
- **E. My Records tanpa filter Kategori/Status itu pilihan desain awal** (bukan regresi). Hanya dicatat sebagai inconsistency LOW (SF-05), bukan defect.
- **F. Export memakai snapshot filter saat klik** (links di-update di `filterTeamTable`/`updateExportLinks` `RecordsTeam.cshtml:329-346`), jadi export = WYSIWYG terhadap filter aktif — itu memang tujuannya; satu-satunya cacatnya adalah propagasi SF-01 (sudah dicatat di SF-06).

---

## 4. Catatan verifikasi

- SF-01 dibuktikan ganda: (1) kode `WorkerDataService.cs:402-417` tidak menyebut `AssessmentSessions`/`a.Title`;
  bandingkan dengan `:373-381` (Category filter) yang **memang** sudah union `AssessmentSessions.Any(a => a.Category ...)` — pola yang sama belum diterapkan ke search. (2) Test `WorkerDataServiceSearchTests.cs:72-83` "Keduanya" hanya assert union Nama∪Training; tidak ada test assessment-title sama sekali.
- My Records & Worker Detail search **tidak** mengidap 999.2: keduanya memfilter `data-title` pada *unified* table yang barisnya mencakup assessment maupun training (`Records.cshtml:168` & `:341`; `RecordsWorkerDetail.cshtml:214` & `:350`). Jadi gap omit-assessment khusus Team View server-side.
