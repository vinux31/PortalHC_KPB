# Design Spec — v22.0 CMP/Records Enhancement (Phase 346 + 347)

**Milestone:** v22.0
**Date:** 2026-06-04
**Author:** Rino (brainstorm w/ Claude)
**Status:** APPROVED — ready for writing-plans

## Sumber

Audit ekshaustif 7-lens + adversarial verify halaman **CMP/Records** (tab My Records + Team View + drill-down Worker Detail) pada 2026-06-04: **37 confirmed finding, 14 rejected**. Permintaan user: tambah fasilitas detail/hasil + search Team View, plus tutup bug & polish yang ketemu.

Halaman tersentuh:
- `Views/CMP/Records.cshtml` (host: tab My Records + tab Team View)
- `Views/CMP/RecordsWorkerDetail.cshtml` (drill-down Team View → 1 pekerja)
- `Views/CMP/RecordsTeam.cshtml` (partial Team View: filter + export + pagination)
- `Views/CMP/_RecordsTeamBody.cshtml` (baris worker)
- `Controllers/CMPController.cs` (Records L479, RecordsWorkerDetail L538, Export* L652/L704, RecordsTeamPartial L753, Results L2169, Certificate L1815, CertificatePdf L1926)
- `Services/WorkerDataService.cs` (GetUnifiedRecords L28, GetAllWorkersHistory L92, GetWorkersInSection L242)

## Goal

1. **My Records** — pekerja bisa lihat detail assessment (hasil/skor) **dan** training (penyelenggara/kota/tanggal/sertifikat), dengan affordance jelas (tombol Aksi).
2. **Worker Detail (Team View)** — atasan bisa buka **hasil assessment** anggota tim (bukan cuma sertifikat) + detail training lengkap.
3. **Team View** — search adaptif: cari nama orang / nama training / keduanya.
4. Tutup bug logic (PendingGrading hilang, date-range, count) + polish i18n/a11y.

## Keputusan terkunci (user 2026-06-04)

- **D-01 Privasi (Hal 1 = Pilihan 1):** Atasan L1–L4 boleh lihat hasil assessment **penuh** (skor + review jawaban per soal) anggota tim. L4 dibatasi section sendiri. Coach/Coachee tak terpengaruh (tak punya Team View).
- **D-02 Struktur (Hal 2 = Cara A):** Pecah 2 phase — **346** (fitur + logic) & **347** (polish i18n/a11y).
- **D-03 My Records discoverability (Cara C):** Tambah kolom "Aksi" dengan tombol eksplisit **DAN** row tetap clickable.
- **D-04 Detail training (Q3):** Modal, **bukan** page baru. Data = ~13 field flat + 1 PDF (`SertifikatUrl`); tak ada konten bertingkat → modal cukup. (Beda dari assessment yang butuh page `Results` karena ada review per-soal.)
- **D-05 Team search (Q2):** 1 kotak input + selektor scope (**Nama / Training / Keduanya**), server-side (jalan lintas pagination).
- **D-06 REC-04 authz:** mirror aturan `RecordsWorkerDetail` (L1–3 full, L4 section-scoped), owner/Admin/HC dipertahankan.

---

## Phase 346 — CMP/Records Detail, Search & Logic Fix

**Goal:** Pekerja & atasan bisa lihat detail assessment/training jelas + Team View bisa di-search + assessment pending tak hilang.
**Risk:** Medium (REC-04 authz security-sensitive; REC-06 service query). **Effort:** M–L. **No migration, no schema change.**
**Depends:** Phase 345 (REC-07 butuh label "Menunggu Penilaian"). Eksekusi **setelah** 345.

### Requirements

#### REC-01 — My Records: kolom "Aksi" + row clickable (D-03)
- Tambah kolom header **"Aksi"** di tabel My Records (`Records.cshtml` thead L150-157 → 7 kolom).
- Per row:
  - Assessment Online → tombol `Lihat Hasil` (`btn btn-sm btn-outline-primary`, icon `bi-bar-chart-line`) → `Url.Action("Results","CMP", new { id = item.AssessmentSessionId })`.
  - Training Manual → tombol `Detail` (`btn btn-sm btn-outline-info`, icon `bi-info-circle`) → buka modal REC-02.
- Row tetap clickable untuk assessment (data-href existing L162-169 dipertahankan); training row dapat handler buka modal.
- **PITFALL (wajib):** colspan empty-state `<td colspan="6">` (`Records.cshtml:227`) **dan** JS inject empty-state (`L381`) ubah 6 → **7**. Kalau tidak, baris "Data belum ada" rusak layout.
- Kolom "Sertifikat" existing dipertahankan apa adanya.

#### REC-02 — My Records: modal detail training (FEAT-1A, D-04)
- Port `trainingDetailModal` dari `RecordsWorkerDetail.cshtml` (L288-307) ke `Records.cshtml`.
- Field modal (`<dl>`): Nama Kegiatan, Penyelenggara, Kota, Tanggal Mulai, Tanggal Selesai, Nomor Sertifikat, **+ Kategori, + SubKategori, + Status, + Valid Until / Certificate Type**.
- Tombol PDF di dalam modal kalau `SertifikatUrl` ada (`target="_blank" rel="noopener"`).
- Data via `data-*` attribute pada tombol Detail (pola RecordsWorkerDetail L255-260) + JS handler `show.bs.modal` (pola L438-447).
- Semua field sudah tersedia di `UnifiedTrainingRecord` (Penyelenggara/Kota/TanggalMulai/TanggalSelesai/NomorSertifikat/Kategori/SubKategori/Status/ValidUntil/CertificateType) — no controller change.

#### REC-03 — Worker Detail: tombol "Lihat Hasil" → Results (FEAT-1B)
- Di action column `RecordsWorkerDetail.cshtml` (L248-277), row Assessment Online tambah tombol `Lihat Hasil` → `Url.Action("Results","CMP", new { id = item.AssessmentSessionId })` (alongside tombol Sertifikat existing).
- `returnUrl` ke halaman worker detail (preserve back nav).

#### REC-04 — Extend authz Results + Certificate + CertificatePdf (D-01, D-06) 🔐
- Ubah authz 3 action di `CMPController.cs` dari `owner || Admin || HC` menjadi:
  ```
  isAuthorized =
        assessment.UserId == user.Id           // owner (coach/coachee self — tetap)
     || roleLevel <= 3                          // Admin(1)/HC(2)/Direktur-VP-Manager(3): full
     || (roleLevel == 4
         && !string.IsNullOrEmpty(user.Section)
         && assessment.User?.Section == user.Section)   // SectionHead/SrSupervisor: section-scoped
  ```
  (Admin & HC sudah ter-cover oleh `roleLevel <= 3`; cek role string lama bisa dihapus atau dipertahankan sebagai defense-in-depth.)
- **Action:** `Results` (L2169), `Certificate` (L1815), `CertificatePdf` (L1926).
- **PITFALL (wajib):**
  - `Results` sudah `.Include(a => a.User)` (L2172) → `assessment.User.Section` tersedia.
  - `Certificate` (L1815) + `CertificatePdf` (L1926) **wajib tambah `.Include(a => a.User)`** sebelum cek, kalau tidak `assessment.User` null → L4 selalu Forbid.
  - Ketiga action perlu panggil `GetCurrentUserRoleLevelAsync` (atau resolve roleLevel via `UserRoles.GetRoleLevel`) — saat ini cuma pakai `GetRolesAsync`.
- **Efek samping diinginkan:** sekalian fix AUTHZ-01 (tombol Sertifikat di Worker Detail yang dead untuk L3/L4).
- **Catatan keamanan:** setelah perubahan, L4 user dengan `Section` null **tidak** boleh lolos lihat user lain ber-Section-null → guard `!string.IsNullOrEmpty(user.Section)` wajib (sesuai pola RecordsWorkerDetail L549-553). L5/L6 (Coach/Coachee) tetap hanya owner.

#### REC-05 — Worker Detail: modal training + Kategori/SubKategori (FEAT-1B training info)
- Extend `trainingDetailModal` di `RecordsWorkerDetail.cshtml` (`<dl>` L296-303) tambah row **Kategori** + **SubKategori** (data sudah ada di kolom tabel L223-224, fold ke modal untuk kelengkapan).
- Wire via `data-kategori`/`data-subcategory` pada tombol Detail (L255-260) + JS handler (L438-447).

#### REC-06 — Team View: search adaptif Nama/Training/Keduanya (FEAT-2, D-05)
- **UI** (`RecordsTeam.cshtml`): tambah baris filter — 1 `<input type="text" id="teamSearch">` + 1 `<select id="searchScope">` dengan opsi `Nama` / `Training` / `Keduanya` (default Keduanya). Debounce sama pola existing (L359-369).
- **JS:** `getFilterState()` (L292-302) tambah `search` + `searchScope`; `doFetch()` (L378-389) kirim 2 param; `updateExportLinks()` (L330-345) ikut set param; `saveFilterState`/`restoreFilterState` persist; `resetTeamFilters` clear.
- **Controller** (`RecordsTeamPartial` L753): tambah param `string? search, string? searchScope`; teruskan ke service. Endpoint export `ExportRecordsTeamAssessment`/`ExportRecordsTeamTraining` (L652/L704) — `search` sudah ada, **tambah `searchScope`** + teruskan.
- **Service** (`GetWorkersInSection` L242): tambah param `string? searchScope = null`. Logika:
  - `searchScope == "Nama"` → existing FullName/NIP filter (L255-262).
  - `searchScope == "Training"` → filter worker yang punya `TrainingRecords.Any(t => t.Judul.ToLower().Contains(search))`. Karena training di-load per-user (trainingsByUser L277-279), filter worker-list **setelah** load (pola category narrow L370-378). Untuk efisiensi opsional: pre-filter user-ids via subquery training.
  - `searchScope == "Keduanya"` (default) → worker match Nama/NIP **ATAU** punya training Judul cocok (union).
- **Semantik:** search **menyaring worker mana yang muncul**, badge count assessment/training per worker tetap utuh (bukan per-training row). Dokumentasikan di tooltip/hint.
- Backward-compat: caller lain `GetWorkersInSection` tanpa `searchScope` tetap jalan (param optional default null = perilaku lama).

#### REC-07 — Include PendingGrading di My Records + export (CMP-LOGIC-02 + CMP-FILTER-02)
- `GetUnifiedRecords` (L31-34): WHERE jadi `a.UserId == userId && (a.Status == "Completed" || a.Status == AssessmentConstants.AssessmentStatus.PendingGrading)`.
- `GetAllWorkersHistory` (L134-136): currentQuery WHERE jadi `a.Status == "Completed" || a.Status == AssessmentConstants.AssessmentStatus.PendingGrading`.
- **PITFALL (wajib):** pakai `AssessmentConstants.AssessmentStatus.PendingGrading` (nilai tersimpan = `"Menunggu Penilaian"`), **BUKAN** literal `"PendingGrading"` (match 0 row).
- **Depends Phase 345:** setelah 345, switch label di `GetUnifiedRecords:51` map `IsPassed null → "Menunggu Penilaian"`. PendingGrading session punya `IsPassed == null` → label benar otomatis. Verifikasi label tampil "Menunggu Penilaian" (bukan "Completed"/"Failed") setelah include.
- Excel `ExportRecords` (CMPController L694) sudah tolerate null IsPassed → blank/Menunggu (no extra map needed; verifikasi sejalan Phase 345 minor fold-in).

#### REC-08 — Team View: validasi date range (FILTER-006)
- `RecordsTeam.cshtml` `doFetch()`/`filterTeamTable()`: kalau `dateFrom > dateTo` → tampilkan warning (extend `updateDateHint` L347-357) atau auto-swap. Pilih **warning** (lebih jelas, tak ubah input user diam-diam): hint "Tanggal Awal lebih besar dari Tanggal Akhir — perbaiki rentang."

#### REC-09 — Perjelas makna badge "Assessment" (CMP-FILTER-01, dilunakkan)
- **JANGAN rename** field `CompletedAssessments` (nyebar 3 file, value LOW). Sebagai gantinya:
  - Header kolom Team View `RecordsTeam.cshtml:137` "Assessment" → tetap, tambah `title`/tooltip "Jumlah assessment lulus" ATAU ubah label jadi "Assessment Lulus".
  - Opsional: tambah komentar kode di `WorkerTrainingStatus.cs:48` yang sudah ada (`// IsPassed == true count`) — sudah benar.

#### REC-10 — Worker Detail: category filter server-side (FILTER-005) — OPSIONAL
- *Boleh di-drop bila mau ringkas.* Saat ini filter category/subcategory `RecordsWorkerDetail.cshtml` client-side (L384-406) pada semua record yang sudah ter-render. Untuk 1 pekerja jumlah record realistis kecil → impact LOW. Bila dikerjakan: tambah param ke action `RecordsWorkerDetail` (L538) + filter di query. **Rekomendasi: DROP** (over-engineering untuk data 1 pekerja yang tak paginated).

### Coverage Phase 346

| REQ | Finding source | Severity |
|-----|----------------|----------|
| REC-01 | FUG-001, FEAT-1A (discoverability) | MED |
| REC-02 | FUG-001, FEAT-1A | MED |
| REC-03 | FUG-002, FEAT-1B | MED |
| REC-04 | FEAT-1B blocker, AUTHZ-01 | 🔐 security |
| REC-05 | FEAT-1B (training info) | LOW |
| REC-06 | FUG-003, FILTER-001, FILTER-002, FEAT-2 | MED |
| REC-07 | CMP-LOGIC-02, CMP-FILTER-02 | MED |
| REC-08 | FILTER-006 | LOW |
| REC-09 | CMP-FILTER-01 | LOW |
| REC-10 | FILTER-005 (opsional/drop) | LOW |

---

## Phase 347 — CMP/Records i18n + a11y Polish

**Goal:** Konsistensi bahasa Indonesia + aksesibilitas + DRY pada halaman Records.
**Risk:** Low. **Effort:** S–M. **No migration, no schema change.**
**Depends:** Phase 346 (sequential — hindari konflik `Records.cshtml`/`RecordsWorkerDetail.cshtml`). Koordinasi POL-01 dengan Phase 345.

### Requirements

| REQ | Isi | File |
|-----|-----|------|
| **POL-01** | Badge assessment `Passed`→`Lulus`, `Failed`→`Tidak Lulus` (case IsPassed true/false). **Koordinasi Phase 345** (jangan timpa case null="Menunggu Penilaian"). | `Records.cshtml:184,186`, `RecordsWorkerDetail.cshtml:228,230` |
| **POL-02** | Header `Score` → `Nilai` | `Records.cshtml:154` |
| **POL-03** | `Position`→`Jabatan`; `Section`→`@OrgLabels.GetLabel(0)`; header Team `Position`→`Jabatan` (Phase 343 kelewat 2 view ini) | `RecordsWorkerDetail.cshtml:66,70`, `RecordsTeam.cshtml:134` |
| **POL-04** | `All Categories`/`All Sub Categories`/`All Types`→`Semua Kategori`/`Semua Sub Kategori`/`Semua Tipe` (+ I18N-008: label opsi tipe konsisten) | `RecordsWorkerDetail.cshtml:137,150,164,165,166,412`, `RecordsTeam.cshtml:58,64,450` |
| **POL-05** | Subtitle Inggris → Indonesia ("Lihat detail rekam jejak penilaian dan pelatihan anggota tim.") | `RecordsWorkerDetail.cshtml:38` |
| **POL-06** | Modal `aria-labelledby="modalTrainingTitle"` + `role="dialog"`; btn-close `aria-label="Tutup"` | `RecordsWorkerDetail.cshtml:288,293` (+ modal baru REC-02) |
| **POL-07** | Label `for=` association semua filter; My Records search visible label (UI-005/UI-014) | `RecordsTeam.cshtml:20,50,56,62,71,79,83`, `RecordsWorkerDetail.cshtml:132-168`, `Records.cshtml:58` |
| **POL-08** | DRY: ekstrak `<style>` duplikat (.stat-card/.sticky-header/@keyframes fadeIn) dari 3 view → 1 file CSS (mis. `wwwroot/css/records.css`) | 3 view + `_Layout`/section |
| **POL-09** | Mobile: grid filter responsif `col-12 col-sm-6 col-md-...` (hindari 6 row numpuk) | `RecordsWorkerDetail.cshtml:132-174` |
| **POL-10** | `type="button"` di reset (Records:81, RecordsWorkerDetail:170); label tombol konsisten (`Lihat`↔`Sertifikat` I18N-009); pagination `aria-current="page"` (UI-011) | 3 view |

### Coverage Phase 347 — 15 finding LOW
UI-001/002/003/004/005/006/007/009/010/011/012/014 + I18N-001/002/003/004/005/006/008/009 + AUTHZ-01 (sudah di REC-04).

---

## Out of Scope

- **Phase 345 items** (CMP06R-01..05): RecordsWorkerDetail null→"Menunggu Penilaian" 3-way, UserAssessmentHistory, BulkExportPdf. Sudah ditangani phase aktif — **jangan duplikat**.
- `AssessmentMonitoringDetail.cshtml:1409` JS SignalR result-cell (di luar halaman Records).
- REC-10 (category filter server-side Worker Detail) — rekomendasi DROP.
- Rename field `CompletedAssessments` cross-file (REC-09 dilunakkan jadi label/tooltip).
- Migration / schema change — **tidak ada di kedua phase**.

## Rejected findings (14) — tidak dikerjakan
CMP-SEMANTIC-01 (naming opini), FILTER-004 (salah atribusi method), FILTER-007 (skenario tak reproducible), FILTER-008 (sudah di-handle), UI-008 (beda audiens by-design), UI-013 (Bootstrap z-index sudah benar), UI-015 (XSS hipotetis, sink numerik), I18N-007 ("Detail" valid ID), AUTHZ-02..07 (authz sudah benar — REC-04 sengaja melonggarkan Results/Certificate per D-01, bukan karena bug).

## Sequencing & Dependency

```
Phase 345 (label "Menunggu Penilaian", 3 surface)
   │
   ├──→ Phase 346 (REC-07 butuh label; REC-01/02/03/05 sentuh Records + WorkerDetail)
   │        │
   │        └──→ Phase 347 (POL-01 sentuh baris badge sama; polish)
```
**WAJIB sequential 345 → 346 → 347.** Ketiganya edit `Records.cshtml` + `RecordsWorkerDetail.cshtml` (baris berdekatan) → eksekusi paralel = konflik.

## Testing Strategy

- **xUnit** (`HcPortal.Tests`):
  - REC-04: authz matrix — owner/Admin/HC/L3/L4-same-section/L4-other-section/L5/L6 terhadap Results+Certificate (Forbid vs OK).
  - REC-06: `GetWorkersInSection` dengan searchScope Nama/Training/Keduanya → worker-set benar.
  - REC-07: `GetUnifiedRecords` + `GetAllWorkersHistory` include session PendingGrading; label "Menunggu Penilaian".
- **Playwright UAT** (Dev admin/coach pwd lokal, DB `HcPortalDB_Dev`):
  - My Records: kolom Aksi muncul, tombol Lihat Hasil → Results, tombol Detail → modal (semua field).
  - Worker Detail: tombol Lihat Hasil → Results (login sebagai L3 & L4-section); L4 beda section Forbid.
  - Team View: search Nama, search Training, Keduanya; export ikut ke-filter; date range invalid → warning.
  - REC-07: assessment esai pending tampil "Menunggu Penilaian" di My Records.
- **Build gate:** `dotnet build` 0 error + `dotnet test` hijau sebelum commit (per CLAUDE.md DEV_WORKFLOW).

## Notes Implementasi

- Dev creds lokal: admin + coach pwd `123456`, DB `HcPortalDB_Dev` (SQLEXPRESS).
- Semua perubahan **lokal dulu** → verify build+test+Playwright → commit+push → notify IT (commit hash + flag migration = NO). Jangan edit Dev/Prod langsung (CLAUDE.md).
- Bundle push v22.0 menyatu dengan carry-over v19/v20/v21 yang masih pending push origin/main + IT promo Dev.
