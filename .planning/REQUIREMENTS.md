# Requirements: Portal HC KPB — v32.9 EditQuestion Option-Edit Data Integrity (Identity-Based)

**Defined:** 2026-06-24
**Core Value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Source:** Backlog 999.15 (review Phase 418 WR-01), diverifikasi reproduces on main 2026-06-24 via 7-agen verify workflow. Bukan design spec baru — fix data-integrity terverifikasi.
**Migration:** FALSE — pure logic + view change (no schema). FK `PackageUserResponse → PackageOption` Restrict (`ApplicationDbContext.cs:561-564`) tak diubah.

## Konteks Bug (ringkas)

Loop upsert `EditQuestion` (`AssessmentAdminController.cs:8121-8160`) map baris input ke `PackageOption` existing **by POSISI** (`existing[i]`, OrderBy Id) → pertahankan `Id` per-posisi. Form authoring (`ManagePackageQuestions.cshtml`) meng-compact daftar saat hapus opsi tengah (A,B,C,D → hapus B → kirim A,C,D). Guard edit-shrink D-418-02 hanya tangkap slot EKOR → `Id` opsi-B selamat, guard tak menyala walau B sudah dijawab. Akibat: record B di-UPDATE jadi teks "C"; `PackageUserResponse` simpan `PackageOptionId` saja (no text snapshot) → jawaban peserta yang dulu "B" kini menunjuk teks "C" → makna berubah senyap di review/grading/PDF.

⚠️ In-code note `AAC:8027-8035`: upsert posisional di-LOCK spec D-418-02. Fix ubah **MEKANISME** (identity-based: hidden `OptionId` per baris + match-by-Id), BUKAN sekadar perketat threshold guard.

## v1 Requirements

Requirements milestone v32.9. Tiap REQ map ke satu fase roadmap (fase mulai 420).

### Option-Edit Integrity (OPTEDIT)

- [ ] **OPTEDIT-01**: HC/Admin dapat menghapus opsi jawaban di posisi MANAPUN (termasuk tengah) pada soal yang BELUM dijawab peserta, dan record `PackageOption` yang terhapus adalah opsi yang benar-benar dipilih HC — opsi tersisa tidak ter-relabel/tergeser maknanya.
- [ ] **OPTEDIT-02**: Saat HC/Admin menghapus opsi yang SUDAH dijawab peserta (posisi manapun, bukan hanya ekor), sistem memblokir dengan pesan ramah (guard answered-option menyala) alih-alih me-relabel jawaban peserta secara senyap.
- [ ] **OPTEDIT-03**: Mengedit teks/kebenaran opsi yang sudah dijawab tetap meng-update record `PackageOption` yang tepat (match by stable `Id`, bukan posisi), sehingga jawaban peserta tetap merujuk opsi yang sama secara semantik di seluruh surface review/grading/PDF.
- [ ] **OPTEDIT-04**: Konversi soal MC/MA→Essay dan penyusutan opsi pada soal terjawab tetap diblokir tanpa error 500 (regression-lock 999.14 — guard D-418-02 tetap berlaku, FK-Restrict tak ter-trip).
- [ ] **OPTEDIT-05**: Alur existing tidak regresi — `CreateQuestion` (soal baru), edit soal yang belum pernah dijawab, dan import Excel soal tetap berfungsi normal dengan form authoring identity-based.

### Verification (VRF)

- [ ] **VRF-01**: Integration test controller-level mereproduksi skenario relabel-senyap (hapus opsi tengah pada soal terjawab) dan membuktikan kini diblokir, plus regression-lock 999.14; ditambah Playwright UAT real-browser pada form authoring (lesson 354).

## Future Requirements (deferred)

- **Data-repair response historis** — pass perbaikan/audit `PackageUserResponse` yang sudah terlanjur ter-relabel oleh bug ini sebelum fix. Defer s/d ada bukti korupsi nyata di data Dev/Prod (belum dikonfirmasi). Bila ada, milestone terpisah (kemungkinan butuh tooling audit + migration data).

## Out of Scope

- **Perubahan skema / migration** — fix murni logic + view. `PackageUserResponse` tetap simpan `PackageOptionId` saja (tak menambah text snapshot).
- **Mengubah keputusan spec D-418-02 di luar mekanisme upsert** — guard answered-option + validator min-2/max-6 + kontrak `List<OptionInput>`/`correctIndex` (418) tetap; hanya cara match input→existing yang berubah (posisi → identity).
- **Backlog yang di-drop/defer** — 999.14 (closed 418, jadi regression-lock saja), 999.9 (no-op, view BulkBackfill dihapus 396), 999.5 (stale, 365 covered), 999.13 + 999.16 (defensif benign/no-op, permanent-defer).
- **Inject form opsi** — form Inject (`InjectAssessment`) client-side membuat soal baru, tak melalui upsert EditQuestion; di luar jalur bug ini.

## Traceability

| REQ | Phase | Status |
|-----|-------|--------|
| OPTEDIT-01 | Phase 420 | pending |
| OPTEDIT-02 | Phase 420 | pending |
| OPTEDIT-03 | Phase 420 | pending |
| OPTEDIT-04 | Phase 420 | pending |
| OPTEDIT-05 | Phase 420 | pending |
| VRF-01 | Phase 420 | pending |

**Coverage:** 6/6 REQ mapped ke Phase 420, 0 orphan, 0 duplikat.
