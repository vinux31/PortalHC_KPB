# Requirements: Portal HC KPB — v32.3 Akun Multi-Unit (dalam 1 Bagian) + Coaching Cross-Unit + PROTON Sekuensial

**Defined:** 2026-06-18
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration

> **Milestone scope:** Pekerja boleh jadi anggota **>1 Unit**, tapi **SELALU dalam 1 Bagian** (Section tetap scalar; pindah Bagian = mutasi, out-of-scope). PROTON **SEKUENtial** — tepat 1 `ProtonTrackAssignment` aktif (Tahun1@unitX selesai → Tahun2@unitY), invariant single-active dipertahankan. Cert/analytics atribusi = **primary unit** (keputusan D1=b, terdokumentasi, **tanpa** kolom unit-at-issue). **migration=TRUE** (1 tabel junction `UserUnits`). Design ref: `docs/superpowers/specs/2026-06-18-akun-multi-unit-within-bagian-design.md`. Branch ITHandoff; deploy ditunda (close bareng).

## v1 Requirements

Requirements untuk milestone v32.3. Tiap REQ map ke satu phase.

### Akun Multi-Unit (MU)

- [ ] **MU-01**: Admin/HC dapat menetapkan **lebih dari satu Unit** (semua dalam 1 Bagian) pada akun pekerja lewat **multi-select Unit** di Create/Edit Worker; picker Bagian tetap single (cascade Bagian→daftar unit Bagian itu).
- [ ] **MU-02**: Tepat satu Unit ditandai **PRIMARY**; `ApplicationUser.Unit` selalu mencerminkan unit primary (write-through di Create/Edit/Import). Recompute primary deterministik: menghapus unit primary mem-promote unit lain (atau blok bila tak ada sisa); mengosongkan semua unit set `ApplicationUser.Unit = null` & 0 baris `IsPrimary`; setiap write menjaga tepat 1 `IsPrimary` per user. Audit-log mencatat **set-diff** (unit ditambah / dihapus / primary berubah).
- [ ] **MU-03**: Pengguna melihat **seluruh unit** pekerja (primary ditandai) di Profil, WorkerDetail, Settings, tabel ManageWorkers, export Excel worker, **Home dashboard**, dan **kartu tanda tangan `_PSign`** (cek reuse cert/print tetap layak).
- [ ] **MU-04**: Bulk Import worker mendukung **multi-unit** (kolom/format unit ganda per baris), tervalidasi tiap unit anak Bagian baris itu.
- [x] **MU-05**: Migrasi backfill — setiap pekerja existing mendapat 1 baris `UserUnits` primary (= `Unit` lama). Setiap **junction-write** (bukan hanya backfill) memvalidasi `Unit ∈ unit-Bagian pekerja` (Name tidak global-unique → wajib dipasangkan dengan Bagian).
- [ ] **MU-06**: Listing keanggotaan (roster tim/section, `GetWorkersInSection`, role-filter, tabel CMP records) **set-aware** — pekerja multi-unit muncul di tiap unit-nya; rollup tingkat Bagian **dedup** (completion%/denominator tidak menghitung pekerja ganda).
- [ ] **MU-07**: Saat Edit/Import **menghapus** sebuah Unit dari `UserUnits` pekerja (atau memindah primary), sistem **memblok** (atau wajib deactivate dulu) bila Unit itu masih dirujuk `CoachCoacheeMapping.AssignmentUnit` aktif atau `ProtonTrackAssignment` aktif pekerja — mencegah orphan `AssignmentUnit ∉ UserUnits` (jaga Invariant #4).

### PROTON Sekuensial Multi-Unit (PSU)

- [ ] **PSU-01**: Unit PROTON di-resolve dari **`AssignmentUnit` eksplisit** (mapping aktif), fallback `User.Unit` dibuang (ambigu saat multi). Pekerja dapat menjalani Tahun 1 @ unit X lalu (setelah selesai) Tahun 2 @ unit Y secara **sekuensial**; sertifikat tiap unit tersimpan utuh sebagai histori.
- [ ] **PSU-02**: Semua filter coachee/tim/bypass pada surface PROTON memakai **`AssignmentUnit`** (bukan `User.Unit`), sehingga coachee yang di-PROTON-kan di unit non-primary tetap tampil di unit yang benar (BypassList, coachee-scope CDP).
- [ ] **PSU-03**: `AssignmentUnit` divalidasi **∈ `coachee.UserUnits`** di jalur Assign/Edit/Import; bypass `TargetUnit` divalidasi **∈ `worker.UserUnits`** + valid di org-tree (saat ini hanya cek non-empty).
- [ ] **PSU-04**: `CleanupCoachCoacheeMappingOrg` dan Import **tidak** menimpa (`clobber`) `AssignmentUnit` ke unit primary — pertahankan unit yang sah (UserUnits-aware).
- [ ] **PSU-05**: Setelah fallback dibuang, **6 read-path** resolusi unit PROTON (`CoachMappingController` GetEligibleCoachees + AutoCreateProgress, `AssessmentAdminController` gate eligibility exam, `CDPController` defensive-filter ×2, `ProtonDataController`) **wajib skip coachee + audit-warn** bila `AssignmentUnit` kosong — **dilarang** diam-diam memakai `User.Unit` primary. Khusus gate eligibility (penentu penerbitan session/cert) tak boleh menerbitkan dengan unit ter-resolve dari primary.
- [ ] **PSU-07**: Jalur **reactivation** (`CoachCoacheeMappingReactivate` + Import-reactivate): (a) jangan timpa `AssignmentUnit` ke primary — pertahankan unit asli mapping; (b) validasi `AssignmentUnit ∈ coachee.UserUnits` aktif sebelum reaktivasi (tolak bila unit sudah dilepas); (c) reaktivasi `ProtonTrackAssignment` harus cocok unit dengan mapping (bukan sekadar "assignment terakhir").

### Coaching Cross-Unit (CXU)

- [ ] **CXU-01**: HC memilih sebuah Bagian → memilih coach → daftar coachee eligible = **semua coachee di Bagian itu** (lintas unit), bukan hanya unit coach (set-aware: `coachee.Section == coach.Section`).
- [ ] **CXU-02**: Server **mem-enforce** coachee yang di-assign harus **⊆ Bagian coach** (tolak cross-Bagian) — guard baru di endpoint assign (saat ini tidak ada perbandingan coach.Section vs coachee.Section).
- [ ] **CXU-03**: `AssignmentUnit` di-set **per-coachee** dari unit coachee yang dipilih (bukan satu nilai batch untuk semua) — reshape payload menjadi map `coacheeId→unit` + dropdown unit per-baris bersumber dari `coachee.UserUnits`; tiap unit divalidasi per PSU-03.
- [ ] **CXU-04**: Lock JS "satu batch = satu unit" di-relax ke **level Bagian** (boleh multi-unit dalam 1 Bagian dalam satu batch).
- [ ] **CXU-05**: Self-scope coaching-role **set-aware** — untuk Coach/Supervisor, `unit = user.Unit` paksa di `CDPController` 305/326/636 (dan post-filter coachee 490-491) diganti `IN(coach.UserUnits)` sehingga coach yang akunnya multi-unit melihat & meng-export **semua** coachee yang dimapping di seluruh unit-nya dalam Bagian.

### Org Integrity (ORG)

- [ ] **ORG-01**: Rename/reparent unit di `OrganizationController` meng-cascade ke `UserUnits.Unit` (+ recompute primary-mirror); delete-guard meng-scan `UserUnits` (termasuk membership **sekunder**, bukan hanya scalar `Users.Unit`) agar unit yang masih dipakai tak terhapus.
- [ ] **ORG-02**: Reparent unit lintas-Bagian **hard-BLOCK** bila ada pekerja yang `UserUnits`-nya akan terpecah ke >1 Bagian (jaga Invariant #1 "1 Bagian/akun"); `PreviewEditCascade` menghitung baris `UserUnits` terdampak agar preview == actual.

### Test & UAT (QA)

- [ ] **QA-01**: Test multi-unit dijalankan di **SQL riil** (EF-InMemory tidak meng-enforce filtered-unique index) — fixture pekerja {X,Y} dalam 1 Bagian + coach cross-unit + PROTON Tahun1@X → Tahun2@Y.
- [ ] **QA-02**: UAT lokal (build + run localhost:5277 + DB lokal, Playwright bila ada) + docs mencatat batasan **D1=b** (cert/analytics atribusi primary unit).
- [ ] **QA-03**: Test invariant **single-active** di SQL riil — coachee multi-unit T1@X → bypass/reassign T2@Y meng-assert tepat 1 `ProtonTrackAssignment` aktif + 1 `CoachCoacheeMapping` aktif (filtered-unique terjaga), termasuk jalur Reactivate + Import-reactivate.
- [ ] **QA-04**: Test invariant **`AssignmentUnit ∈ coachee.UserUnits`** di setiap junction-write (Assign/Edit/Import/bypass TargetUnit/reactivate) + test B-06 anti-dobel `ProtonDeliverableBootstrap` lintas-unit (CoacheeId sama, deliverable unit X vs Y tidak saling skip) + `ProtonKompetensi.Unit` 1:1 per deliverable.

## v2 Requirements (Future / deferred)

- Cert/analytics atribusi per-unit akurat (kolom unit-at-issue di `AssessmentSession`/`TrainingRecord` + backfill) — bila kebutuhan compliance per-unit muncul.
- PROTON paralel (2 track aktif konkuren) — bila bisnis butuh; perlu relax unique index + kolom Unit di `ProtonTrackAssignment` + re-key ~21 site + routing cert/penanda per-assignment.

## Out of Scope

| Feature | Reason |
|---------|--------|
| PROTON paralel (2 assignment aktif barengan) | Sekuensial dikonfirmasi user; invariant single-active dipertahankan |
| Cert/analytics atribusi per-unit (unit-at-issue) | D1=b — atribusi primary, terdokumentasi; hindari migration ke-2 |
| Akun multi-**Bagian** | Selalu 1 Bagian/akun; pindah Bagian = mutasi (proses terpisah) |
| Multi-Role (1 pekerja >1 role) | Riset terpisah; bukan milestone ini |
| Year-gate per-unit | Tetap per-TrackType (D2) — cocok skenario Tahun1@X → Tahun2@Y, no change |

## Traceability

Diisi saat pembuatan roadmap.

| Requirement | Phase | Status |
|-------------|-------|--------|
| MU-01 | Phase 399 | Pending |
| MU-02 | Phase 399 | Pending |
| MU-03 | Phase 399 | Pending |
| MU-04 | Phase 399 | Pending |
| MU-05 | Phase 399 | Complete |
| MU-07 | Phase 399 | Pending |
| MU-06 | Phase 400 | Pending |
| PSU-01 | Phase 401 | Pending |
| PSU-02 | Phase 401 | Pending |
| PSU-03 | Phase 401 | Pending |
| PSU-04 | Phase 401 | Pending |
| PSU-05 | Phase 401 | Pending |
| PSU-07 | Phase 401 | Pending |
| CXU-01 | Phase 402 | Pending |
| CXU-02 | Phase 402 | Pending |
| CXU-03 | Phase 402 | Pending |
| CXU-04 | Phase 402 | Pending |
| CXU-05 | Phase 402 | Pending |
| ORG-01 | Phase 403 | Pending |
| ORG-02 | Phase 403 | Pending |
| QA-01 | Phase 404 | Pending |
| QA-02 | Phase 404 | Pending |
| QA-03 | Phase 404 | Pending |
| QA-04 | Phase 404 | Pending |

**Coverage:**
- v1 requirements: 24 total (MU 7, PSU 6, CXU 5, ORG 2, QA 4)
- Mapped to phases: 24 (399: MU-01/02/03/04/05/07 · 400: MU-06 · 401: PSU-01/02/03/04/05/07 · 402: CXU-01..05 · 403: ORG-01/02 · 404: QA-01..04)
- Migration: TRUE (1 — `UserUnits` junction; backfill 1 primary row/pekerja)
- Dependency: 400/401/403 → 399; 402 → 401; 404 → semua

---
*Requirements defined: 2026-06-18 — derived from committed spec + adversarial coverage audit (12 gaps closed: CXU-05, MU-07, PSU-05, PSU-07, QA-03/04 + tightened MU-02/03/05, CXU-03, ORG-01/02).*
