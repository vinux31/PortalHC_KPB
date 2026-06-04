# Design Spec — v22.0 ManageAssessment + Assessment Monitoring Audit Fix (Phase 348 + 349)

**Milestone:** v22.0
**Date:** 2026-06-04
**Author:** Rino (brainstorm w/ Claude)
**Status:** APPROVED — ready for writing-plans

## Sumber

Audit ekshaustif (6 review-agent × 5 lensa: function/content/logic/filter/UI) + adversarial verify
halaman **ManageAssessment** (tab Assessment Groups / Input Records / History) dan **Assessment Monitoring**
(list + detail) pada 2026-06-04 via Workflow `audit-manageassessment-monitoring` (59 agent).
Hasil: **53 finding diangkat → 44 confirmed (0 HIGH / 15 MED / 29 LOW), 9 false-positive ditolak**.
Verifikasi adversarial menurunkan banyak severity (HIGH→MED, MED→LOW) dan buang 9 FP
(typo "Licencor" = canonical, status "Permanent" unreachable, sort Title-then-Date by design, dll).

Halaman tersentuh:
- `Views/Admin/ManageAssessment.cshtml` (host 3 tab HTMX + JS client-filter History)
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml` (Tab 1: Assessment Groups)
- `Views/Admin/Shared/_TrainingRecordsTab.cshtml` (Tab 2: Input Records)
- `Views/Admin/Shared/_HistoryTab.cshtml` (Tab 3: History / Riwayat Assessment)
- `Views/Admin/AssessmentMonitoring.cshtml` (Monitoring list)
- `Views/Admin/AssessmentMonitoringDetail.cshtml` (Monitoring detail per-peserta)
- `Controllers/AssessmentAdminController.cs` (tab actions L106/245/284; AssessmentMonitoring L2670; Detail L3163; RegenerateToken L2616; AkhiriSemuaUjian L3914)
- `Controllers/CMPController.cs` (SignalR `workerSubmitted` push L1767)
- `Services/WorkerDataService.cs` (GetAllWorkersHistory L92/134; GetWorkersInSection L242)

## Goal

1. **Correctness** — tutup bug logic/filter/fungsi MED (Pre-Post handling, essay PendingGrading status, Tab2 struktural, status-badge divergence, Monitoring filter).
2. **Konsistensi Pre-Post** — token regen, monitoring/export link, badge pending-grading konsisten antara PreTest dan PostTest.
3. **Polish** — tutup 29 LOW (i18n Monitoring, a11y, empty-state, code-hygiene).

## Keputusan terkunci (user 2026-06-04)

- **D-01 Struktur (rekomendasi Claude, di-ACK user):** Pecah 2 phase — **348** (14 MED correctness) & **349** (29 LOW polish). Bug dulu, polish nyusul.
- **D-02 Scope LOW (user):** **SEMUA 29 LOW masuk** (termasuk code-hygiene nits: dead param, magic number, dead var).
- **D-03 Dedup M4:** Tab3 History PendingGrading (M4, `WorkerDataService.cs:134`) **SUDAH dicakup REC-07 Phase 346** (REC-07 include PendingGrading di `GetAllWorkersHistory:136` — fungsi yang sama feed History tab). **TIDAK diduplikat di 348.** Aksi: tambah surface History tab ke UAT Phase 346; optional badge "Menunggu Penilaian" di cell Pass/Fail → MAP polish 349.
- **D-04 Sequential:** v22.0 strict sequential 345→346→347→**348→349**. Phase 348 setelah 347 (M5/M6 pakai label/konstanta "Menunggu Penilaian" yang sudah mapan dari 345).
- **D-05 No migration, no schema change** untuk kedua phase.

---

## Phase 348 — ManageAssessment + Monitoring MED Correctness Fix

**Goal:** Pre-Post group konsisten (token/export/badge), essay pending tak salah-label "Completed", Tab2 empty-state + pagination + filter benar, status-badge match filter, Monitoring filter data-driven & jujur.
**Risk:** Medium (M1/M5/M6 sentuh logic shared grading + token; M7 ubah initial-load behavior). **Effort:** L. **No migration.**
**Depends:** Phase 347 (sequential v22.0; M5/M6/badge pakai label "Menunggu Penilaian" + konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` dari 345).

### Requirements — Phase 348 (MED)

> Catatan: M4 (Tab3 History PendingGrading) **bukan REQ 348** — dicakup REC-07 Phase 346 (D-03).

#### Tema A — Konsistensi Pre-Post group

- [ ] **MAM-01** — RegenerateToken match by LinkedGroupId untuk Pre-Post.
  `AssessmentAdminController.cs:2616-2621`: bila `assessment.LinkedGroupId != null`, pilih siblings by `LinkedGroupId`
  (bukan `Title==X && Category==Y && Schedule.Date==Z`), supaya PreTest **dan** PostTest dapat token baru yang sama.
  **Bug:** PostTest beda tanggal → token PostTest tak ikut regenerate → peserta kena "Token tidak valid", PostTest ke-block.
  Pesan audit (`L2636 siblings.Count`) ikut benar. **PITFALL:** validasi cuma enforce `PostSchedule > PreSchedule`, beda tanggal = normal.

- [ ] **MAM-02** — Link Monitoring/Export Pre-Post sadar LinkedGroupId / pecah per-half.
  `_AssessmentGroupsTab.cshtml:261-285`: link Monitoring + Export Excel + Bulk PDF kirim `scheduleDate=group.Schedule.Date` (PreTest rep)
  → endapoint (`AssessmentMonitoringDetail` L3165, `ExportAssessmentResults` L4120, `BulkExportPdf` L4503) filter by date → silently miss PostTest.
  **Fix:** untuk row `IsPrePostGroup`, route by `LinkedGroupId` (tambah param), **atau** pecah jadi link per-half (date + assessmentType)
  meniru pola yang sudah ada di `AssessmentMonitoring.cshtml:337-368` (preDetailUrl/postDetailUrl terpisah).

- [ ] **MAM-03** — `MenungguPenilaianCount` di-set untuk Pre-Post group di AssessmentMonitoring.
  `AssessmentAdminController.cs:2749-2796` (prePostGroups) tak pernah assign `MenungguPenilaianCount` (default 0) → badge "X belum dinilai"
  (`AssessmentMonitoring.cshtml:276`) tak pernah muncul untuk seluruh tipe Pre-Post.
  **Fix:** tambah `MenungguPenilaianCount = postSubs.Count(a => a.IsMenungguPenilaian)` (grading terjadi di Post). Parity dgn standardGroups L2825.
  *(Finding dilaporkan 2× — Monitor list + Cross-cutting — bug sama.)*

#### Tema B — Essay PendingGrading status (Monitoring surface)

- [ ] **MAM-04** — Status derivation Detail cek PendingGrading SEBELUM CompletedAt.
  `AssessmentAdminController.cs:3229-3239`: GradingService set essay `Status="Menunggu Penilaian"` **+ `CompletedAt` terisi + `IsPassed=null`** bersamaan.
  Derivation cek `CompletedAt != null` DULU → essay-pending salah-map "Completed" (badge hijau), Result "—" kontradiksi, `CompletedCount` inflated, passRate deflated, cabang view "Menunggu Penilaian" (cshtml L239/246) jadi dead-code.
  **Fix:** sisipkan cabang pertama `if (a.Status == AssessmentConstants.AssessmentStatus.PendingGrading) userStatus = "Menunggu Penilaian";` sebelum cek CompletedAt. `CompletedCount` exclude ungraded essay.

- [ ] **MAM-05** — Live SignalR `workerSubmitted` jangan push "Completed"+Pass/Fail prematur untuk essay.
  `CMPController.cs:1767-1770`: push hardcode `status="Completed"` + `result = finalPercentage >= PassPercentage ? "Pass":"Fail"` dari skor MC/MA saja.
  Untuk sesi essay (GradingService balik state PendingGrading), hasil masih pending → monitor cat hijau "Completed" + verdict yang bisa flip.
  **Fix:** branch bila graded state = PendingGrading → push `status="Menunggu Penilaian"`, `result="—"` (atau event `workerPendingGrading`). Update handler `AssessmentMonitoringDetail.cshtml:1336-1362` render badge pending. **PITFALL:** `hasEssay` lokal di GradingService → service harus surface state pending ke caller.

#### Tema C — Tab2 Input Records struktural

- [ ] **MAM-06** — `isInitialState` diturunkan dari absennya filter (bukan hardcode false).
  `AssessmentAdminController.cs:251`: `bool isInitialState = false;` hardcode → empty-state "Pilih filter" (`_TrainingRecordsTab.cshtml:163-171`) jadi dead code; SETIAP first paint load SELURUH roster aktif (regresi dari refactor Phase 311, commit c2b5a910).
  **Fix:** `isInitialState = string.IsNullOrEmpty(isFiltered) && IsNullOrEmpty(section/unit/category/statusFilter/search)` (param `isFiltered` sudah di-post hidden field L32). Skip full-roster query sampai admin benar-benar filter. **Verifikasi parity sama Phase 287 commit fc161a18.**

- [ ] **MAM-07** — Tab2 pagination atau drop param mati.
  `AssessmentAdminController.cs:245-263`: `page/pageSize` diterima + dilog tapi tak pernah dipakai; `GetWorkersInSection` tak ada Skip/Take; view tak ada markup pagination → seluruh roster 1 halaman.
  **Fix (pilih):** (a) tambah Skip/Take di `GetWorkersInSection` + render kontrol pagination meniru Tab1 (`_AssessmentGroupsTab.cshtml:348-427`), **atau** (b) drop param `page/pageSize` + token log `page=` yang menyesatkan. **Rekomendasi (a)** bila roster section bisa ratusan.

- [ ] **MAM-08** — Delete Training/ManualAsm preserve filter context.
  `_TrainingRecordsTab.cshtml:327-349`: delete pakai full-page `<form method=post>`; handler (`TrainingAdminController.cs` DeleteTraining L586/619, DeleteManualAssessment L985/1016) redirect `new { tab="training" }` tanpa filter → admin terlempar keluar view ter-filter + reload full roster tiap hapus 1 baris.
  **Fix (rekomendasi):** konversi delete jadi `hx-post` yang re-swap wrapper tab (preserve filter form via `hx-include`). *(Alternatif RedirectToAction route-values cuma partial — Phase 322 cuma restore section+unit on full load.)*

- [ ] **MAM-09** — Filter Status Tab2 ("Sudah/Belum") jujur untuk pekerja assessment-only.
  `_TrainingRecordsTab.cshtml:107-125` + `WorkerDataService.cs:360-365,390-397`: `statusFilter` dipetakan ke `CompletionPercentage` (training-only). Tanpa kategori, pekerja yang cuma punya manual assessment (0 training) selalu `CompletionPercentage==0` → "Belum", walau assessment-nya lulus. Kontradiksi kolom display baris yang sama (`CompletionDisplayText` yang DO hitung assessment).
  **Fix:** relabel kontrol → **"Status Training"** dan/atau lipat passed manual-assessment ke penentuan Sudah/Belum saat no-category.

#### Tema D — Status display divergence

- [ ] **MAM-10** — Badge status baris Tab1 pakai `GroupStatus` (bukan rep `Status`).
  `_AssessmentGroupsTab.cshtml:195-221`: badge render `@group.Status` (rep single-session), tapi filter/stats/hide-Closed pakai `GroupStatus` turunan (`AssessmentAdminController.cs:177-180`). Baris di-filter "Open" bisa berlabel "Completed/Abandoned".
  **Fix:** bind `@group.GroupStatus` di switch + teks badge (sudah exposed L167/188). Drop arm `Completed/InProgress/Abandoned` (bukan nilai GroupStatus); pertahankan Open/Upcoming/Closed; **tambah case "Closed"** (saat ini fallthrough bg-secondary = sama dgn Completed). *(Menyatukan finding MED cross-cut + LOW twin L1.)*

#### Tema E — Monitoring list filter/content

- [ ] **MAM-11** — Dropdown Kategori Monitoring data-driven.
  `AssessmentMonitoring.cshtml:125-148`: list kategori hardcoded, out-of-sync `AssessmentCategories`. "Proton" phantom (match 0; canonical seed = "Assessment Proton"); kategori baru admin tak bisa difilter.
  **Fix:** populate dari `_context.AssessmentCategories.Where(c=>c.IsActive).OrderBy(c=>c.SortOrder)` via ViewBag (pola CreateAssessment L312/345/355) + buang "Proton".

- [ ] **MAM-12** — Tooltip badge Closed jujur (search by judul saja, bukan "lokasi").
  `AssessmentMonitoring.cshtml:169`: tooltip janji "search judul/**lokasi** spesifik", tapi controller search cuma `Title.Contains` (`AssessmentAdminController.cs:2685-2688`); `Kota` tak di-search/display. Placeholder L75 "Cari nama assessment..." (title-only) malah kontradiksi tooltip.
  **Fix (minimal jujur):** buang "lokasi" dari tooltip. *(Opsi besar: extend search ke Kota — lihat MAP-? L27 cross-cut search-scope; out of scope MED.)*

#### Tema F — Monitoring detail function

- [ ] **MAM-13** — Selector tombol Reshuffle (single worker) scoped, jangan bentrok `<tr>`.
  `AssessmentMonitoringDetail.cshtml:739-743`: `reshuffleWorker()` resolve btn via `document.querySelector('[data-session-id="'+sessionId+'"]')` — tapi `<tr data-session-id>` (L268) punya attribute sama & lebih dulu di DOM → querySelector balik `<tr>`, spinner ganti SELURUH baris, restore HTML salah. *(Reshuffle server tetap jalan — sessionId dari arg, bukan btn; cuma glitch visual self-healing.)*
  **Fix:** scope selector ke tombol (class distinct / `[data-reshuffle-session-id]`), atau ubah onclick `reshuffleWorker(this)` pakai elemen yang dilempar.

**Coverage Phase 348: 13 REQ (MAM-01..13) — semua 15 MED minus 2 dedup (MenungguPenilaian 2×→MAM-03; status-badge MED+LOW→MAM-10) minus M4 (→REC-07/346). No migration.**

---

## Phase 349 — ManageAssessment + Monitoring LOW Polish

**Goal:** Tutup 29 LOW: i18n Monitoring, a11y (aria-label/chevron), empty-state/feedback copy, dan code-hygiene.
**Risk:** Low (polish, no logic-bearing). **Effort:** M. **No migration.**
**Depends:** Phase 348 (sequential — sentuh file yang sama: `_AssessmentGroupsTab.cshtml`, `AssessmentMonitoring*.cshtml`, `ManageAssessment.cshtml`).

### Requirements — Phase 349 (LOW, grouped)

#### Group i18n
- [ ] **MAP-01** — Monitoring Detail terjemah chrome Inggris → Indonesia: header tabel `Name/Progress/Status/Score/Result/Completed At/Actions` → `Nama/Progres/Status/Nilai/Hasil/Selesai Pada/Aksi`; kartu `Total Assigned/Completed/In Progress/Not Started` → `Total Ditugaskan/Selesai/Sedang Mengerjakan/Belum Mulai`; `Back to Monitoring`→`Kembali ke Monitoring`; `Per-User Status`/`Export Results`/`View Results`. (`AssessmentMonitoringDetail.cshtml:72,150-168,183,192,214-220,293`)
- [ ] **MAP-02** — Konsistenkan label identitas: History sub-tab Assessment "NIP" vs Training "Nopeg" → standar **"NIP"** (header + placeholder). (`_HistoryTab.cshtml:62,155,175,33`)

#### Group a11y
- [ ] **MAP-03** — Chevron/aria affordance toggle collapse: Tab1 "N peserta" (`_AssessmentGroupsTab.cshtml:233-237`) + Tab2 expand-records (`_TrainingRecordsTab.cshtml:232-237`) — tambah `aria-label` deskriptif + chevron rotate via CSS `[aria-expanded="true"]`.
- [ ] **MAP-04** — Tab3 drill-down: hilangkan ARIA nested-interactive (`<a>` di dalam `<tr role="link">`). Pilih 1 affordance primer (rekomendasi: simpan tombol "Lihat", drop `role/tabindex` pada `<tr>`). (`_HistoryTab.cshtml:78-118`)

#### Group empty-state & feedback
- [ ] **MAP-05** — Tab1 empty-state filter-aware: bila search kosong tapi kategori/status aktif → "Tidak ada assessment untuk filter ini" + Reset (bukan "Buat assessment pertama"). (`_AssessmentGroupsTab.cshtml:133-156`)
- [ ] **MAP-06** — Tab1 "Hapus Pencarian" → reuse HTMX clear-search-only (preserve kategori/status) **atau** relabel "Reset Semua Filter". (`_AssessmentGroupsTab.cshtml:143-146`)
- [ ] **MAP-07** — Tab3 client-filter: bila 0 baris match → inject baris "Tidak ada hasil untuk filter ini." (+`aria-live="polite"`). Berlaku assessment & training. (`ManageAssessment.cshtml:316-337`)
- [ ] **MAP-08** — Tab3 badge count sub-tab ikut filter aktif (update visible-row count) **atau** tambah baris "Menampilkan X dari Y". (`_HistoryTab.cshtml:13,20`)
- [ ] **MAP-09** — Skeleton loader match konten asli: Tab2 (4→5 filter, kolom→7) + History (kolom→8/5). (`ManageAssessment.cshtml:146-165,180-199`)

#### Group display nits
- [ ] **MAP-10** — Monitor Detail kartu summary tambah "Abandoned" (atau lipat ke bucket) supaya jumlah = Total; sync JS `updateSummaryFromDOM` (L1280-1298). (`AssessmentMonitoringDetail.cshtml:146-177`)
- [ ] **MAP-11** — Monitor Detail: "In Progress" card pakai `@Model.InProgressCount` (bukan inline LINQ); buang dead var `completedPct`/`passRatePct` **atau** surface sebagai progress/pass-rate (pakai passRate exclude-pending ala 345). (`AssessmentMonitoringDetail.cshtml:33-39,161`)
- [ ] **MAP-12** — Monitor Detail: tombol "Akhiri Semua Ujian" hanya render bila ada yang bisa diakhiri (`@if (Model.InProgressCount > 0 || Model.GroupStatus == "Open")`); modal wording "X belum mulai" pakai predikat identik dgn aksi cancel. (`AssessmentMonitoringDetail.cshtml:196-199,542`)
- [ ] **MAP-13** — Monitor list: `TotalCount` exclude Cancelled (`g.Count(a => a.Status != "Cancelled")`) supaya progress bar bisa 100% + `CancelledCount` parity. (`AssessmentAdminController.cs:2819-2822` + Pre-Post path L2757)
- [ ] **MAP-14** — Monitor list: subtitle buang "real-time" (list tak ada SignalR — reserve untuk Detail). (`AssessmentMonitoring.cshtml:27`)
- [ ] **MAP-15** — Monitor list: dropdown Status saat search jangan misrepresent (set "Semua Status" bila search broaden scope / controller set `ViewBag.SelectedStatus="All"`). (`AssessmentMonitoring.cshtml:12,80-87`)
- [ ] **MAP-16** — Monitor list: buang kategori dobel (muted subtitle di kolom Assessment vs badge kolom Kategori). (`AssessmentMonitoring.cshtml:257-260,271-273`)
- [ ] **MAP-17** — Monitor list: token-required Pre-Post group → minimal render "View Detail"; idealnya Regenerate Token (target LinkedGroupId, koord MAM-01). (`AssessmentMonitoring.cshtml:298-327`)
- [ ] **MAP-18** — Tab2 Detail manual-assessment tri-state: `IsPassed==null → "Menunggu Penilaian"` (samakan Status ternary). Defensive. (`_TrainingRecordsTab.cshtml:249`)
- [ ] **MAP-19** — Tab2 "Status Training" badge "Belum ada" gated combined count (training + manual assessment), atau selalu render `CompletionDisplayText`. (`_TrainingRecordsTab.cshtml:222-229`)
- [ ] **MAP-20** — (opsional/koord MAM-04) Tab3 History cell Pass/Fail tambah badge "Menunggu Penilaian" saat `IsPassed==null && Status==PendingGrading` (rows muncul dari REC-07/346). (`_HistoryTab.cshtml:102-103`)

#### Group code-hygiene (D-02 semua masuk)
- [ ] **MAP-21** — Pagination Tab1: drop magic number `20` → expose `paging.Take` via ViewBag, pakai di rowNum + "Menampilkan X-Y" + hidden input. (`_AssessmentGroupsTab.cshtml:16,180,354`)
- [ ] **MAP-22** — History/Training action: drop param mati `pageSize/statusFilter` (History) + `pageSize` (Training) + no-op `page` wiring (`ManageAssessment.cshtml:19`), atau dokumentasikan intent Phase 322. (`AssessmentAdminController.cs:245-305`)
- [ ] **MAP-23** — (opsional) Cross-cut: extend search Monitoring ke Category untuk parity dgn tab (Nama/NIP **tidak** — list Monitoring aggregate, tak render per-user). Update placeholder. (`AssessmentAdminController.cs:2685-2689`)

**Coverage Phase 349: 23 REQ bucket (MAP-01..23) mencakup 29 LOW (beberapa LOW digabung: pagination-20 L4+L28→MAP-21; dead-param L10+L29→MAP-22; status-badge LOW L1→MAM-10/348). D-02 semua 29 ditangani. No migration.**

---

## Dependensi & urutan

```
345 → 346 → 347 → 348 → 349   (v22.0 strict sequential)
              │      │
              │      └─ 348 pakai konstanta/label "Menunggu Penilaian" dari 345
              └─ REC-07 (346) sudah fix Tab3 History PendingGrading (M4) — 348 TIDAK duplikat
```

## Pitfalls (wajib saat plan/execute)

1. **MAM-01/MAM-02/MAM-17:** Pre-Post = `LinkedGroupId`, PostTest bisa beda tanggal. Jangan asumsi same-date.
2. **MAM-04/MAM-05/MAP-20:** pakai konstanta `AssessmentConstants.AssessmentStatus.PendingGrading` — JANGAN literal string. Koordinasi dgn label 345.
3. **MAM-06:** verifikasi behavior parity Phase 287 (fc161a18); ada UAT Phase 322 yang PASS dengan full-roster-on-load — pastikan fix tak break UAT lama (cek 322-UAT.md). *(Catatan: FP-rejected "Reset full-roster by design" — fix MAM-06 mengembalikan empty-state, koord dgn 322 expectation.)*
4. **MAM-10:** drop arm switch non-GroupStatus, tambah case "Closed".
5. **Sequential strict:** 348 & 349 sentuh file yang sama → jangan paralel; 349 setelah 348 selesai.
6. **No migration / no schema** kedua phase.

## Catatan dedup vs v22.0 existing

- **M4 (Tab3 History PendingGrading)** → REC-07 Phase 346. Tindak lanjut: tambah Tab3 History ke UAT 346.
- **MAP-20** (badge History pending) depends rows muncul dari REC-07/346.
- LOW i18n (MAP-01) BEDA surface dari POL-* Phase 347 (347 scoped CMP/Records; ini Monitoring) — net-new, tak overlap.
