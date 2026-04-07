# Roadmap: Portal HC KPB

## Milestones

- ✅ **v1.0–v5.0** - Phases 1-172 (shipped)
- ✅ **v7.1–v7.12** - Phases 176-222 (shipped)
- ✅ **v8.0–v8.7** - Phases 223-253 (shipped)
- ⏸️ **v9.0 Pre-deployment Audit & Finalization** - Phases 254-256 (deferred)
- ✅ **v9.1 UAT Coaching Proton End-to-End** - Phases 257-261 (shipped 2026-03-25, partial)
- ✅ **Phases 262-263** - Sub-path deployment fixes (shipped 2026-03-27)
- ✅ **v10.0 UAT Assessment OJT di Server Development** - Phases 264-280 (shipped)
- ⏸️ **v11.2 Admin Platform Enhancement** - Phases 281-285 (paused — closed early)
- ✅ **v12.0 Controller Refactoring** - Phases 286-291 (shipped 2026-04-02)
- ✅ **v13.0 Redesign Struktur Organisasi** - Phases 292-295 (shipped 2026-04-06)
- 🚧 **v14.0 Assessment Enhancement** - Phases 296-302 (in progress)

## Phases

<details>
<summary>✅ Previous milestones (v1.0–v12.0, Phases 1-291) — SHIPPED</summary>

See .planning/MILESTONES.md for full history.

</details>

<details>
<summary>⏸️ v11.2 Admin Platform Enhancement (Phases 281-285) — PAUSED</summary>

- [ ] **Phase 281: System Settings** - Admin dapat mengelola konfigurasi aplikasi dari UI
- [x] **Phase 282: Maintenance Mode** - Admin dapat mengaktifkan mode pemeliharaan
- [x] **Phase 283: User Impersonation** - Admin dapat melihat aplikasi dari perspektif role/user lain
- [ ] **Phase 285: Dedicated Impersonation Page** - Halaman admin tersendiri untuk impersonation

</details>

<details>
<summary>✅ v13.0 Redesign Struktur Organisasi (Phases 292-295) — SHIPPED</summary>

- [x] **Phase 292: Backend AJAX Endpoints** - GetOrganizationTree JSON + dual-response pada CRUD actions + CSRF utility (completed 2026-04-02)
- [x] **Phase 293: View Shell & Tree Rendering** - Ganti 520-baris view dengan ~130-baris shell + recursive tree dari JSON (completed 2026-04-02)
- [x] **Phase 294: AJAX CRUD Lengkap** - Modal add/edit, toggle, delete, action dropdown via orgTree.js tanpa page reload (completed 2026-04-03)
- [x] **Phase 295: Drag-drop Reorder** - SortableJS reorder sibling-only, cross-parent diblokir (completed 2026-04-03)

</details>

### 🚧 v14.0 Assessment Enhancement (Phases 296-302)

- [x] **Phase 296: Data Foundation + GradingService Extraction** - Migrasi DB backward-compatible dan ekstraksi GradingService sebagai fondasi semua fase berikutnya (completed 2026-04-06)
- [x] **Phase 297: Admin Pre-Post Test** - HC dapat membuat, mengelola, dan memonitor assessment tipe Pre-Post Test (completed 2026-04-07)
- [x] **Phase 298: Question Types** - HC dapat membuat 4 tipe soal baru; sistem auto/manual grading per tipe berfungsi (completed 2026-04-07)
- [ ] **Phase 299: Worker Pre-Post Test + Comparison** - Pekerja dapat mengerjakan Pre-Post Test dan melihat perbandingan gain score
- [ ] **Phase 300: Mobile Optimization** - Exam UI optimal di perangkat mobile untuk pekerja lapangan
- [ ] **Phase 301: Advanced Reporting** - HC dapat melihat item analysis, gain score report, dan export ke Excel
- [ ] **Phase 302: Accessibility WCAG Quick Wins** - Fitur aksesibilitas dasar diterapkan pada halaman ujian

## Phase Details

### Phase 292: Backend AJAX Endpoints
**Goal**: OrganizationController siap melayani AJAX — endpoint GetOrganizationTree baru tersedia dan semua CRUD action sudah dual-response (JSON jika AJAX, redirect jika form POST)
**Depends on**: Nothing (first phase v13.0)
**Requirements**: TREE-01, TREE-04
**Success Criteria** (what must be TRUE):
  1. GET `/Organization/GetOrganizationTree` mengembalikan flat JSON array semua OrganizationUnit dengan field Id, Name, ParentId, Level, DisplayOrder, IsActive
  2. POST actions (Create, Edit, Toggle, Delete, Reorder) mengembalikan `{success, message}` JSON jika header `X-Requested-With: XMLHttpRequest` ada, tetap redirect jika bukan AJAX
  3. Semua AJAX POST sudah melewati CSRF dengan utility function terpusat `ajaxPost(url, data)` di orgTree.js
  4. Tidak ada regression pada alur PRG yang sudah ada — halaman tetap berfungsi normal jika JS dimatikan
**Plans**: 1 plan
Plans:
- [x] 292-01-PLAN.md — IsAjaxRequest helper + GetOrganizationTree + dual-response + orgTree.js utility
**UI hint**: yes

### Phase 293: View Shell & Tree Rendering
**Goal**: Halaman ManageOrganization ter-render sebagai tree view interaktif dari JSON — user dapat melihat hierarki dengan indentasi, expand/collapse per node dan semua sekaligus, serta badge status
**Depends on**: Phase 292
**Requirements**: TREE-01, TREE-02, TREE-03, TREE-04
**Success Criteria** (what must be TRUE):
  1. Halaman ManageOrganization menampilkan tree view dengan indentasi visual per level (Bagian → Unit → Sub-unit)
  2. User dapat expand/collapse node individual dengan klik panah, dan ada tombol Expand All / Collapse All
  3. Setiap node menampilkan badge Aktif (hijau) atau Nonaktif (merah/abu) yang sesuai dengan status database
  4. Tree mendukung kedalaman unlimited — rendering rekursif berjalan benar untuk node Level 0, 1, 2, dan seterusnya
  5. ManageOrganization.cshtml dikurangi dari ~520 baris menjadi ~130 baris dengan 3 loop Razor dihapus
**Plans**: 1 plan
Plans:
- [x] 293-01-PLAN.md — View shell + orgTree.js tree rendering + expand/collapse
**UI hint**: yes

### Phase 294: AJAX CRUD Lengkap
**Goal**: Admin/HC dapat melakukan seluruh operasi CRUD pada struktur organisasi via modal tanpa page reload — Add, Edit, Toggle, Delete semuanya AJAX dengan feedback toast
**Depends on**: Phase 293
**Requirements**: CRUD-01, CRUD-02, CRUD-03, CRUD-04, CRUD-05
**Success Criteria** (what must be TRUE):
  1. Admin/HC dapat menambah unit baru via modal — form terisi, submit, tree refresh tanpa reload halaman
  2. Admin/HC dapat mengedit nama dan parent unit via modal — perubahan tersimpan dan tree diperbarui tanpa reload
  3. Admin/HC dapat toggle aktif/nonaktif unit — status badge berubah instan tanpa reload
  4. Admin/HC dapat menghapus unit via modal konfirmasi — node hilang dari tree tanpa reload
  5. Setiap node memiliki action dropdown (Edit, Toggle, Hapus) menggantikan tombol inline; setiap operasi menampilkan toast notifikasi sukses/gagal
**Plans**: 1 plan
Plans:
- [x] 294-01-PLAN.md — AJAX CRUD modal + action dropdown + tree refresh
**UI hint**: yes

### Phase 295: Drag-drop Reorder
**Goal**: Admin/HC dapat mengubah urutan unit dalam sibling yang sama dengan drag-and-drop — cross-parent drag diblokir sepenuhnya
**Depends on**: Phase 294
**Requirements**: REORD-01, REORD-02
**Success Criteria** (what must be TRUE):
  1. Admin/HC dapat drag node ke posisi atas/bawah dalam sibling yang sama — urutan tersimpan ke database via `ReorderOrganizationUnit`
  2. Drag handle visual muncul pada hover node sehingga user tahu bahwa node bisa di-drag
  3. Drag lintas parent (reparent via drag) diblokir secara teknis — SortableJS dikonfigurasi `group: false` sehingga node tidak bisa pindah ke parent lain
**Plans**: 1 plan
Plans:
- [ ] 293-01-PLAN.md — View shell + orgTree.js tree rendering + expand/collapse
**UI hint**: yes

### Phase 296: Data Foundation + GradingService Extraction
**Goal**: Fondasi teknis tersedia — migrasi DB selesai tanpa breaking change dan GradingService terekstrak sebagai komponen terpusat
**Depends on**: Nothing (phase pertama milestone ini)
**Requirements**: FOUND-01, FOUND-02, FOUND-03, FOUND-04, FOUND-05, FOUND-06, FOUND-07, FOUND-08, FOUND-09
**Success Criteria** (what must be TRUE):
  1. Semua tabel yang sudah ada tetap berfungsi normal setelah migrasi — data existing tidak ada yang hilang atau rusak
  2. GradingService dapat dipanggil dari SubmitExam, AkhiriUjian, AkhiriSemuaUjian, dan GradeFromSavedAnswers tanpa mengubah hasil grading assessment existing (Multiple Choice)
  3. Kolom QuestionType pada PackageQuestion default ke "MultipleChoice" — semua soal lama terbaca sebagai MultipleChoice tanpa perlu update data
  4. Kolom AssessmentType, AssessmentPhase, LinkedGroupId, LinkedSessionId, HasManualGrading pada AssessmentSession tersedia dan nullable-safe
  5. DB migration berjalan bersih di environment development tanpa error
Plans:
- [x] 296-01-PLAN.md — Data foundation migration (completed 2026-04-06)
- [x] 296-02-PLAN.md — GradingService extraction (completed 2026-04-06)
- [x] 296-03-PLAN.md — Controller wiring ke GradingService (completed 2026-04-06)

### Phase 297: Admin Pre-Post Test
**Goal**: HC dapat membuat assessment Pre-Post Test, mengatur jadwal dan paket soal terpisah untuk Pre dan Post, serta memonitor keduanya dari satu tampilan
**Depends on**: Phase 296
**Requirements**: PPT-01, PPT-02, PPT-03, PPT-04, PPT-05, PPT-06, PPT-07, PPT-08, PPT-09, PPT-10, PPT-11
**Success Criteria** (what must be TRUE):
  1. HC dapat memilih tipe assessment "Pre-Post Test" saat membuat assessment baru dan mengatur jadwal serta durasi berbeda untuk Pre dan Post
  2. HC dapat mencentang "Gunakan paket soal yang sama" untuk menyalin semua paket Pre ke Post, atau memilih paket soal berbeda untuk Pre dan Post secara independen
  3. Halaman AssessmentMonitoring menampilkan grup Pre-Post Test sebagai satu entri yang dapat di-expand untuk melihat detail Pre dan Post secara terpisah
  4. HC mereset Pre-Test dan reset tersebut otomatis men-cascade ke Post-Test yang terhubung
  5. HC menghapus grup Pre-Post dan kedua sesi (Pre + Post) terhapus bersama tanpa orphan record
  6. Sertifikat dan Training Record hanya digenerate dari hasil Post-Test, bukan Pre-Test
**Plans**: 4 plans
Plans:
- [x] 297-01-PLAN.md — ViewModel extension + CreateAssessment backend + view
- [x] 297-02-PLAN.md — Monitoring grouping + expandable view + ManageAssessment badge
- [x] 297-03-PLAN.md — EditAssessment tab Pre/Post + ManagePackages Copy
- [x] 297-04-PLAN.md — DeletePrePostGroup + ResetAssessment guard + Renewal
**UI hint**: yes

### Phase 298: Question Types
**Goal**: HC dapat membuat soal True/False, Multiple Answer, Essay, dan Fill in the Blank; pekerja dapat menjawab dengan UI yang sesuai; dan sistem melakukan grading otomatis atau manual dengan benar per tipe soal
**Depends on**: Phase 296
**Requirements**: QTYPE-01, QTYPE-02, QTYPE-03, QTYPE-04, QTYPE-05, QTYPE-06, QTYPE-07, QTYPE-08, QTYPE-09, QTYPE-10, QTYPE-11, QTYPE-12, QTYPE-13
**Success Criteria** (what must be TRUE):
  1. HC dapat membuat soal True/False (2 opsi radio), Multiple Answer (checkbox multi-pilih), Essay (rich text editor), dan Fill in the Blank (text input) dari halaman manajemen soal
  2. Template Excel impor soal memiliki kolom QuestionType — upload bulk berhasil dengan tipe soal beragam dalam satu file
  3. Halaman StartExam menampilkan radio button untuk MC/TF, checkbox untuk MA, textarea untuk Essay, dan text input untuk FillBlank sesuai tipe soal masing-masing
  4. Skor Multiple Answer dihitung all-or-nothing — pekerja harus memilih semua opsi benar tanpa salah pilih untuk mendapat nilai penuh
  5. Soal Essay tidak ter-grading otomatis — status assessment menjadi "Menunggu Penilaian" dan IsPassed tetap null sampai HC menilai semua soal Essay
  6. HC dapat menginput skor per soal Essay dari halaman AssessmentMonitoringDetail, dan sistem menghitung ulang skor total serta menentukan IsPassed setelah semua Essay dinilai
  7. Fill in the Blank di-grade otomatis dengan exact match case-insensitive terhadap jawaban benar yang telah didefinisikan HC
**Plans**: 5 plans
Plans:
- [x] 298-01-PLAN.md — True/False question type admin + exam UI + grading
- [x] 298-02-PLAN.md — Multiple Answer question type admin + exam UI + grading
- [x] 298-03-PLAN.md — Essay question type admin + exam UI + manual grading
- [x] 298-04-PLAN.md — Fill in the Blank question type admin + exam UI + grading
- [x] 298-05-PLAN.md — Excel import support for new question types
**UI hint**: yes

### Phase 299: Worker Pre-Post Test + Comparison
**Goal**: Pekerja dapat mengerjakan Pre-Test dan Post-Test secara berurutan, dan dapat melihat perbandingan skor beserta gain score setelah Post-Test selesai
**Depends on**: Phase 297
**Requirements**: WKPPT-01, WKPPT-02, WKPPT-03, WKPPT-04, WKPPT-05, WKPPT-06, WKPPT-07
**Success Criteria** (what must be TRUE):
  1. Halaman daftar assessment pekerja menampilkan Pre-Test dan Post-Test sebagai 2 card terpisah yang secara visual terlihat terhubung (badge atau label yang jelas)
  2. Pekerja tidak dapat memulai Post-Test sebelum Pre-Test berstatus Completed — tombol Post-Test tidak aktif atau menampilkan pesan informatif
  3. Setelah Pre-Test Completed dan jadwal Post-Test tiba, pekerja dapat memulai Post-Test secara normal
  4. Setelah Post-Test selesai, pekerja dapat mengakses halaman perbandingan Pre vs Post yang menampilkan skor per elemen secara side-by-side
  5. Gain score ditampilkan dengan formula (PostScore - PreScore) / (100 - PreScore) x 100; kasus PreScore = 100 menampilkan Gain = 100
**Plans**: TBD
**UI hint**: yes

### Phase 300: Mobile Optimization
**Goal**: Pekerja di lapangan dapat mengerjakan ujian dari perangkat mobile dengan nyaman — navigasi sentuh berfungsi, antarmuka tidak terpotong, dan tombol mudah ditekan
**Depends on**: Phase 298
**Requirements**: MOB-01, MOB-02, MOB-03, MOB-04, MOB-05, MOB-06
**Success Criteria** (what must be TRUE):
  1. Semua tombol dan opsi jawaban di halaman ujian mobile memiliki area sentuh minimal 48x48dp — tidak ada elemen yang sulit ditekan di layar kecil
  2. Pekerja dapat menggeser layar ke kiri/kanan (swipe) untuk berpindah antar halaman soal di perangkat mobile
  3. Tombol Previous, Next, dan Submit selalu terlihat di bagian bawah layar saat scroll, dan panel navigasi soal muncul sebagai offcanvas drawer di mobile (bukan sidebar tetap)
  4. Timer ujian tetap terlihat di header mobile meskipun pengguna scroll ke bawah
  5. Fitur anti-copy yang sudah ada (Phase 280) tetap berfungsi benar bersama touch/swipe events tanpa saling konflik
**Plans**: TBD
**UI hint**: yes

### Phase 301: Advanced Reporting
**Goal**: HC dapat melihat kualitas soal secara statistik (item analysis), tren gain score Pre-Post, dan perbandingan antar kelompok — semua dapat diekspor ke Excel
**Depends on**: Phase 297, Phase 298
**Requirements**: RPT-01, RPT-02, RPT-03, RPT-04, RPT-05, RPT-06, RPT-07
**Success Criteria** (what must be TRUE):
  1. HC dapat melihat Item Analysis per soal yang menampilkan difficulty index (p-value = % responden menjawab benar) untuk setiap soal dalam sebuah assessment
  2. HC dapat melihat discrimination index (Kelley upper/lower 27%) dengan warning eksplisit "Data belum cukup" ketika jumlah responden kurang dari 30
  3. HC dapat melihat distractor analysis — persentase responden yang memilih tiap opsi per soal
  4. HC dapat melihat Pre-Post Gain Score Report untuk assessment PrePostTest per pekerja dan per elemen kompetensi dengan gain score konsisten
  5. HC dapat mengekspor Item Analysis dan Gain Score Report ke file Excel yang terstruktur
  6. Analytics Dashboard menampilkan panel baru untuk tren gain score dari assessment PrePostTest
**Plans**: TBD
**UI hint**: yes

### Phase 302: Accessibility WCAG Quick Wins
**Goal**: Halaman ujian dapat digunakan dengan keyboard dan screen reader, dan peserta dengan kebutuhan khusus mendapat akomodasi waktu tambahan
**Depends on**: Phase 298, Phase 300
**Requirements**: A11Y-01, A11Y-02, A11Y-03, A11Y-04, A11Y-05, A11Y-06
**Success Criteria** (what must be TRUE):
  1. Link "Lewati ke konten utama" muncul dan dapat difokus via Tab di bagian atas setiap halaman — klik langsung memindahkan fokus ke area konten utama
  2. Semua soal dan opsi jawaban di halaman ujian dapat dinavigasi menggunakan keyboard saja (arrow keys untuk opsi, Tab untuk berpindah soal/navigasi)
  3. Timer ujian mengumumkan melalui screen reader (aria-live) ketika sisa waktu kurang dari 5 menit — tidak menginterupsi pembacaan setiap detik
  4. Kontrol ukuran font (A+/A-) tersedia di halaman ujian dan preferensi disimpan via localStorage sehingga persisten antar sesi
  5. HC dapat menetapkan waktu tambahan (ExtraTimeMinutes) per sesi untuk peserta dengan kebutuhan khusus, dan sistem menambahkannya ke durasi ujian pekerja tersebut
  6. Fokus keyboard otomatis berpindah ke soal pertama saat pekerja berpindah ke halaman soal baru
**Plans**: TBD
**UI hint**: yes

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 292. Backend AJAX Endpoints | 1/1 | Complete | 2026-04-02 |
| 293. View Shell & Tree Rendering | 1/1 | Complete | 2026-04-02 |
| 294. AJAX CRUD Lengkap | 1/1 | Complete | 2026-04-03 |
| 295. Drag-drop Reorder | 1/1 | Complete | 2026-04-03 |
| 296. Data Foundation + GradingService Extraction | 3/3 | Complete | 2026-04-06 |
| 297. Admin Pre-Post Test | 4/4 | Complete   | 2026-04-07 |
| 298. Question Types | 5/5 | Complete   | 2026-04-07 |
| 299. Worker Pre-Post Test + Comparison | 0/? | Not started | - |
| 300. Mobile Optimization | 0/? | Not started | - |
| 301. Advanced Reporting | 0/? | Not started | - |
| 302. Accessibility WCAG Quick Wins | 0/? | Not started | - |

---

*Roadmap updated: 2026-04-07*
*v14.0 phases: 296-302 | Coverage: 52/52 requirements*
