# Requirements: v25.0 Proton Kelulusan & Bypass

**Milestone goal:** Logic kelulusan Proton konsisten (exam Tahun 1/2 terbit penanda + gate berurutan dipaksa), lalu fitur Bypass Tahun.

**Specs:** `docs/superpowers/specs/2026-06-09-proton-completion-logic-design.md` (A) + `docs/superpowers/specs/2026-06-09-proton-bypass-tahun-design.md` (B). B depends A.

---

## v25.0 Requirements

### PCOMP — Proton Completion Logic (fase 358-359)

- [ ] **PCOMP-01**: Exam Proton Tahun 1/2 yang lulus otomatis menerbitkan penanda `ProtonFinalAssessment` (dashboard menandai "Lulus").
- [ ] **PCOMP-02**: Re-grade exam Proton dari Pass→Fail menghapus penanda yang ber-`Origin="Exam"` (penanda Bypass/Interview kebal).
- [ ] **PCOMP-03**: Pembuatan/penghapusan penanda lewat satu helper bersama `ProtonCompletionService.EnsureProtonFinalAssessment` (dipakai jalur exam, interview, bypass).
- [ ] **PCOMP-04**: Kolom `Origin` (Exam/Interview/Bypass) ditambah di `ProtonFinalAssessment` (migration #1; baris lama → "Interview").
- [ ] **PCOMP-05**: Backfill 1x — penanda terbit untuk exam Tahun 1/2 lama yang sudah lulus + deliverable 100% (idempotent).
- [x] **PCOMP-06**: Gate eligibility divalidasi **server-side** di POST CreateAssessment (deliverable 100% + Tahun N-1 lulus), bukan cuma filter JS.
- [x] **PCOMP-07**: Gate antar-tahun keras — assign/eligible Tahun N diblok kalau Tahun N-1 (TrackType sama) belum lulus (bypass exempt).
- [x] **PCOMP-08**: Tahun 3 deliverable data-driven — kalau silabus Tahun 3 ada deliverable, gate 100% berlaku (Tahun 3 final tetap interview).
- [x] **PCOMP-09**: Tombol "Mark graduated" diblok kalau Tahun 3 belum lulus.
- [x] **PCOMP-10**: Tampilan `CompetencyLevelGranted` + grafik tren dimatikan (kolom DB dibiarkan dormant, tidak di-drop).

### PBYP — Proton Bypass Tahun (fase 360-361)

- [ ] **PBYP-01**: Tabel `PendingProtonBypass` (migration #2) menyimpan rencana bypass tertunda (lifecycle Menunggu→Siap→Selesai/Dibatalkan).
- [ ] **PBYP-02**: Bypass mendukung 4 closure mode — CL-A (lulus instan), CL-B(a) (input manual instan), CL-B(b) (buat assessment, tunggu lulus), CL-C (tinggalkan); validasi |Δtahun|≤1 + 1-assignment-aktif.
- [ ] **PBYP-03**: Exam CL-B(b) yang lulus memicu pending→"Siap" + notif `PROTON_BYPASS_READY` ke HC inisiator (GradingService flip flag, BUKAN auto-pindah).
- [ ] **PBYP-04**: Bypass menangani coach — deactivate mapping aktif lama lalu create baru (constraint filtered-unique E15); HC bisa ganti coach via dropdown.
- [ ] **PBYP-05**: Bootstrap deliverable target pakai Unit dari form bypass (bukan dari mapping).
- [ ] **PBYP-06**: HC bisa batal pending sebelum pindah (auto-cancel exam: belum-dikerjakan→hapus, sudah-lulus→pertahankan hasil).
- [ ] **PBYP-07**: 6 endpoint bypass (`BypassList`, `BypassPendingList`, `BypassDetail`, `BypassSave`, `BypassConfirm`, `BypassCancelPending`) `[Authorize(Admin,HC)]` + AntiForgery + audit.
- [ ] **PBYP-08**: Page Override jadi 2 tab — Tab1 existing (tak diubah) + Tab2 "Bypass Tahun" dengan wizard 3-langkah (Tujuan → Closure mode → Detail+alasan).
- [ ] **PBYP-09**: Panel "Menunggu Konfirmasi" di Tab2 + notif deep-link `/ProtonData/Override?tab=bypass&pending={id}` + 1-klik konfirmasi pindah.
- [ ] **PBYP-10**: UAT end-to-end bypass (4 closure mode + pending konfirmasi + batal + re-grade fail).

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

20 REQ → 4 phase, 100% mapped.
