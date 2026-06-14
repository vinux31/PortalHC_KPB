# Requirements: v25.0 Proton Kelulusan & Bypass

**Milestone goal:** Logic kelulusan Proton konsisten (exam Tahun 1/2 terbit penanda + gate berurutan dipaksa), lalu fitur Bypass Tahun.

**Specs:** `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` (A) + `docs/superpowers/specs/2026-06-09-proton-bypass-tahun-design.md` (B). B depends A.

---

## v25.0 Requirements

### PCOMP — Proton Completion Logic (fase 358-359)

- [x] **PCOMP-01**: Exam Proton Tahun 1/2 yang lulus otomatis menerbitkan penanda `ProtonFinalAssessment` (dashboard menandai "Lulus").
- [x] **PCOMP-02**: Re-grade exam Proton dari Pass→Fail menghapus penanda yang ber-`Origin="Exam"` (penanda Bypass/Interview kebal).
- [x] **PCOMP-03**: Pembuatan/penghapusan penanda lewat satu helper bersama `ProtonCompletionService.EnsureProtonFinalAssessment` (dipakai jalur exam, interview, bypass).
- [x] **PCOMP-04**: Kolom `Origin` (Exam/Interview/Bypass) ditambah di `ProtonFinalAssessment` (migration #1; baris lama → "Interview").
- [x] **PCOMP-05**: Backfill 1x — penanda terbit untuk exam Tahun 1/2 lama yang sudah lulus + deliverable 100% (idempotent).
- [x] **PCOMP-06**: Gate eligibility divalidasi **server-side** di POST CreateAssessment (deliverable 100% + Tahun N-1 lulus), bukan cuma filter JS.
- [x] **PCOMP-07**: Gate antar-tahun keras — assign/eligible Tahun N diblok kalau Tahun N-1 (TrackType sama) belum lulus (bypass exempt).
- [x] **PCOMP-08**: Tahun 3 deliverable data-driven — kalau silabus Tahun 3 ada deliverable, gate 100% berlaku (Tahun 3 final tetap interview).
- [x] **PCOMP-09**: Tombol "Mark graduated" diblok kalau Tahun 3 belum lulus.
- [x] **PCOMP-10**: Tampilan `CompetencyLevelGranted` + grafik tren dimatikan (kolom DB dibiarkan dormant, tidak di-drop).

### PBYP — Proton Bypass Tahun (fase 360-361)

- [x] **PBYP-01**: Tabel `PendingProtonBypass` (migration #2) menyimpan rencana bypass tertunda (lifecycle Menunggu→Siap→Selesai/Dibatalkan).
- [x] **PBYP-02**: Bypass mendukung 4 closure mode — CL-A (lulus instan), CL-B(a) (input manual instan), CL-B(b) (buat assessment, tunggu lulus), CL-C (tinggalkan); validasi |Δtahun|≤1 + 1-assignment-aktif.
- [x] **PBYP-03**: Exam CL-B(b) yang lulus memicu pending→"Siap" + notif `PROTON_BYPASS_READY` ke HC inisiator (GradingService flip flag, BUKAN auto-pindah).
- [x] **PBYP-04**: Bypass menangani coach — deactivate mapping aktif lama lalu create baru (constraint filtered-unique E15); HC bisa ganti coach via dropdown.
- [x] **PBYP-05**: Bootstrap deliverable target pakai Unit dari form bypass (bukan dari mapping).
- [x] **PBYP-06**: HC bisa batal pending sebelum pindah (auto-cancel exam: belum-dikerjakan→hapus, sudah-lulus→pertahankan hasil).
- [x] **PBYP-07**: 6 endpoint bypass (`BypassList`, `BypassPendingList`, `BypassDetail`, `BypassSave`, `BypassConfirm`, `BypassCancelPending`) `[Authorize(Admin,HC)]` + AntiForgery + audit.
- [x] **PBYP-08**: Page Override jadi 2 tab — Tab1 existing (tak diubah) + Tab2 "Bypass Tahun" dengan wizard 3-langkah (Tujuan → Closure mode → Detail+alasan).
- [x] **PBYP-09**: Panel "Menunggu Konfirmasi" di Tab2 + notif deep-link `/ProtonData/Override?tab=bypass&pending={id}` + 1-klik konfirmasi pindah.
- [x] **PBYP-10**: UAT end-to-end bypass (4 closure mode + pending konfirmasi + batal + re-grade fail).

---

## v26.0 Requirements (URGENT — added 2026-06-11, interleave dengan sisa v25.0)

### URG — Search & Records Visibility (fase 369-371)

- [x] **URG-01**: Fix H1 search-drop (`14e7adc5` main) tersinkron ke ITHandoff — `GetWorkersInSection` searchScope null/kosong di-treat "Nama" (search tidak diabaikan diam-diam) + test regresi hijau. (Phase 369 SHIPPED LOCAL 2026-06-11, cherry-pick `5210e4d4`)
- [x] **URG-02**: Window 7-hari dihapus dari tampilan default `ManageAssessmentTab_Assessment` + `AssessmentMonitoring` — semua sesi tampil tanpa batas umur (filter status default "Aktif" + hide-Closed CIL-02 tetap; search behavior quick 260611-m9r tidak regresi). (Phase 370 SHIPPED LOCAL 2026-06-11, completed `a24600b1`, kode `2f686e71`)
- [x] **URG-03**: Sesi assessment online (IsManualEntry=false) tampil di tab Input Records per worker dengan badge pembeda "Assessment Online" — visibility-only, aksi hapus tetap scope Phase 367. (Phase 371 SHIPPED LOCAL 2026-06-12, commit `d1d03e13`)

---

## v27.0 Requirements (added 2026-06-13 APPEND-ONLY — STATE.md tetap v25.0)

**Milestone goal:** HC bisa ON/OFF dua sistem pengacakan independen (Acak Soal + Acak Pilihan) per-assessment lewat halaman ManagePackages. Default ON dua-duanya (perilaku existing tak berubah).

**Spec:** `docs/superpowers/specs/2026-06-13-shuffle-toggle-design.md`

> ⚠️ File-overlap dengan v25.0 aktif (`AssessmentAdminController.cs`, `CMPController.cs`). Koordinasi sebelum `/gsd-plan-phase 372`.

### SHUF — Shuffle Toggle (fase 372-375)

- [ ] **SHUF-01**: Kolom `ShuffleQuestions` + `ShuffleOptions` (bool) di `AssessmentSession`; migration `defaultValue:true` → baris lama ON dua-duanya. [372]
- [ ] **SHUF-02**: Form CreateAssessment set kedua flag eksplisit (default checked) di SEMUA loop create (standard/Pre/Post) — hindari EF bool-false trap pada assessment baru. [372]
- [ ] **SHUF-03**: Ubah toggle propagate ke semua sibling grup (pola `foreach` EditAssessment POST). [372]
- [ ] **SHUF-04**: Acak Soal ON = perilaku existing (1 paket: acak urutan; ≥2 paket: sampling K lintas paket + ET-balanced + acak). [373]
- [ ] **SHUF-05**: Acak Soal OFF + 1 paket = semua soal, urutan `q.Order` (semua peserta identik). [373]
- [ ] **SHUF-06**: Acak Soal OFF + ≥2 paket = distribusi 1 paket utuh/worker, round-robin **index-session-stabil** (`paket[posisi % jumlahPaketBerSoal]`), urutan `q.Order`, guard paket kosong. [373]
- [ ] **SHUF-07**: Acak Pilihan independen dari Acak Soal — ON bangun `optionShuffleDict`, OFF simpan `"{}"` (view fallback DB order). [373]
- [ ] **SHUF-08**: Resume stale-count guard deterministik untuk mode OFF (rekomputasi konsisten via index-stabil). [373]
- [ ] **SHUF-09**: `ReshufflePackage`/`ReshuffleAll` hormati `ShuffleQuestions` DAN `ShuffleOptions` (fix bug existing hard-code opsi `"{}"`). [373]
- [x] **SHUF-10**: Toggle UI di header ManagePackages, aktif walau `SamePackage` lock isi paket; endpoint POST `UpdateShuffleSettings` (`[Authorize(Admin,HC)]`+AntiForgery+audit+propagate). [374]
- [x] **SHUF-11**: Lock toggle (read-only + guard server-side) saat ada peserta mulai (`StartedAt!=null` ATAU ada `UserPackageAssignment` grup). [374]
- [x] **SHUF-12**: Warning non-blocking saat multi-paket + Acak Soal OFF + jumlah soal antar paket berbeda (nilai/pass% tak setara). [374]
- [x] **SHUF-13**: Reminder visual di Post bila Pre OFF tapi Post ON (cek via `LinkedSessionId`); no auto-cascade, no hidden state (opsi Z). [374]
- [x] **SHUF-14**: Sembunyikan toggle untuk Proton Tahun 3 / Manual entry (bukan ujian online berbasis paket). [374]
- [ ] **SHUF-15**: Cleanup komentar stale `CMPController.cs:1054` ("option shuffle removed" — sekarang digerbang `ShuffleOptions`). [373]
- [ ] **SHUF-16**: Test — xUnit core semua mode + migration default + propagasi + lock + reshuffle flag; Playwright UAT toggle/lock/reminder/warning. [375]

---

## Out of Scope (v25.0)

- **Audit/improve Tab1 Override Deliverable** — ditunda (Tab1 belum tulis `DeliverableStatusHistory`, belum warning un-approve penanda-Lulus, belum `RejectedById`). → backlog.
- **Undo bypass executed** (tombol undo) — koreksi via bypass lagi (spec B §8.2 Opsi C). Butuh kolom `PreviousStatus` kalau dibangun nanti.
- **Menghidupkan level kompetensi** (dibuang, A-3).
- **Drop kolom `CompetencyLevelGranted`** (dibiarkan dormant).
- **Konfigurasi gate via UI** (gate = aturan tetap kode).

## Traceability

| REQ | Phase |
|-----|-------|
| PCOMP-01,02,03,04,05 | 358 |
| PCOMP-06,07,08,09,10 | 359 |
| PBYP-01,02,03,04,05,06,07 | 360 |
| PBYP-08,09,10 | 361 |
| URG-01 | 369 (v26.0) |
| URG-02 | 370 (v26.0) |
| URG-03 | 371 (v26.0) |

v25.0: 20 REQ → 4 phase, 100% mapped. v26.0: 3 REQ → 3 phase, 100% mapped.
