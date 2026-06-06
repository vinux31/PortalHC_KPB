# Phase 343 — Audit Deliverable (SC1 + ORG-INTEG-02)

**Tanggal:** 2026-06-03
**Tujuan:** Pemenuhan **SC1** (audit grep `"Bagian"|"Unit"|"Sub-unit"` per file + keputusan eksplisit ganti/skip per occurrence) DAN **ORG-INTEG-02** (verdict controller display-string).
**Sifat:** Dokumen audit/dokumentasi murni — BUKAN kode. Swap aktual dikerjakan Plan 02/03/04. Tabel REPLACE penuh (file:line | snippet | tier | reason) ada otoritatif di `343-RESEARCH.md` L118-209; dokumen ini meringkas + menambah traceability "Diterapkan di Plan" + verdict final per AMBIGUOUS/controller.

Konvensi tier: **0 = Bagian**, **1 = Unit**, **2 = Sub-unit** (seed `Data/SeedData.cs` L121-123; fallback `"Level {N}"` bila tabel kosong).

---

## §1 Folder-Structure Finding (validasi D-03)

4 dari 7 "area" yang disebut ROADMAP TIDAK punya Views folder sendiri — form-nya hidup di `Views/Admin/`:

| Views folder | Status | Form sebenarnya di |
|--------------|--------|--------------------|
| `Views/Worker/` | KOSONG (0 file) | `Views/Admin/CreateWorker.cshtml`, `EditWorker.cshtml`, `WorkerDetail.cshtml`, `ManageWorkers.cshtml`, `ImportWorkers.cshtml` |
| `Views/CoachMapping/` | KOSONG (0 file) | `Views/Admin/CoachCoacheeMapping.cshtml` |
| `Views/Renewal/` | KOSONG (0 file) | `Views/Admin/RenewalCertificate.cshtml` |
| `Views/DocumentAdmin/` | KOSONG (0 file) | `Views/Admin/Kkj*.cshtml`, `Cpdp*.cshtml`, `Views/CMP/DokumenKkj.cshtml` |

**Kesimpulan D-03:** Scope swap aktual = folder yang BENAR ADA file: `Views/CMP/`, `Views/CDP/`, `Views/ProtonData/`, `Views/Admin/`, plus `Views/Account/` (Profile/Settings — temuan tambahan, tidak ada di spec 7-area). Worker/CoachMapping/Renewal/DocumentAdmin = **audit-only** (form tercakup via `Views/Admin/`). Tidak ada perubahan paksa untuk folder kosong.

---

## §2 REPLACE Targets (display tier-label → swap) + Traceability per Plan

~95 occurrence di ~25 file. Tabel lengkap per-baris: **`343-RESEARCH.md` L118-209** (otoritatif). Distribusi + traceability:

| File | Occurrence (line) | Tier | Diterapkan di Plan |
|------|-------------------|------|--------------------|
| Views/CMP/AnalyticsDashboard.cshtml | 84,86,94,96,543,548 | 0/1 | **Plan 02** |
| Views/CMP/RecordsTeam.cshtml | 20,24,41,50,52,135,136 | 0/1 | **Plan 02** |
| Views/CDP/CertificationManagement.cshtml | 89,97,105,110,112 | 0/1 | **Plan 02** |
| Views/CDP/CoachingProton.cshtml | 100,120 | 0/1 | **Plan 02** |
| Views/CDP/HistoriProton.cshtml | 30,34,51,60,62,109 | 0/1 | **Plan 02** |
| Views/CDP/HistoriProtonDetail.cshtml | 36,84 | 1 | **Plan 02** |
| Views/CDP/PlanIdp.cshtml | 72,86,91,94 | 0/1 | **Plan 02** |
| Views/CDP/Shared/_CoachingProtonPartial.cshtml | 29,35 | 1 | **Plan 02** |
| Views/CDP/Shared/_CertificationManagementTablePartial.cshtml | 19,20 | 0/1 | **Plan 02** |
| Views/ProtonData/Index.cshtml | 79,81,85,87,230,232,236,238 | 0/1 | **Plan 03** |
| Views/ProtonData/Override.cshtml | 29,31,35,37 | 0/1 | **Plan 03** |
| Views/Admin/CoachCoacheeMapping.cshtml | 235,236,435,437,445,503,505,513 | 0/1 | **Plan 03** |
| Views/Admin/CreateWorker.cshtml | 116,122 | 0/1 | **Plan 03** |
| Views/Admin/EditWorker.cshtml | 121,127 | 0/1 | **Plan 03** |
| Views/Admin/ManageWorkers.cshtml | 132,134,152,154,226,227 | 0/1 | **Plan 03** |
| Views/Admin/WorkerDetail.cshtml | 89,102 | 0/1 | **Plan 03** |
| Views/Admin/RenewalCertificate.cshtml | 55,57,65,67 | 0/1 | **Plan 03** |
| Views/Admin/Shared/_TrainingRecordsTab.cshtml | 37,45,87,94,194 | 0/1 | **Plan 03** |
| Views/Admin/EditAssessment.cshtml | 522,598 | 0 | **Plan 04** |
| Views/Admin/CreateAssessment.cshtml | 264 | 0 | **Plan 04** |
| Views/Admin/CpdpUpload.cshtml | 39,42 | 0 | **Plan 04** |
| Views/Admin/KkjUpload.cshtml | 39,42 | 0 | **Plan 04** |
| Views/Account/Profile.cshtml | 78,84 | 0/1 | **Plan 04** |
| Views/Account/Settings.cshtml | 96,114 | 0/1 | **Plan 04** |

**Pola swap (3 kanonik, detail di `343-PATTERNS.md`):** (1) visible-text-only — `<label>`/`<th>` text diganti, `id`/`for` tetap literal; (2) combined-text — `Semua X`, `-- Pilih X --`, `X Penugasan`; (3) detail-field — label kiri diganti, sel nilai data JANGAN. Partial views (`_CoachingProtonPartial`, `_CertificationManagementTablePartial`, `_TrainingRecordsTab`) **mewarisi @inject** dari `_ViewImports.cshtml` (Plan 01, D-01) — tidak perlu @inject ulang.

---

## §3 AMBIGUOUS — Keputusan Final (eksplisit + alasan)

| file:line | snippet | verdict FINAL | alasan |
|-----------|---------|---------------|--------|
| Views/Admin/CpdpFiles.cshtml:64,87 | `Tambah Bagian` / `Hapus Bagian` (button) | **REPLACE button visible text, SKIP JS func/toast** (Plan 04) | Button referensi tier root, user-facing → ikut rename. JS func `addBagian()`/`deleteBagian()` + toast `'Bagian berhasil dihapus'` (L172/223/241) = SKIP (identifier + literal). |
| Views/Admin/KkjMatrix.cshtml:64,87 | `Tambah Bagian` / `Hapus Bagian` (button) | **REPLACE button visible text, SKIP JS func/toast** (Plan 04) | File kembar CpdpFiles — keputusan sama. |
| Views/Admin/EditAssessment.cshtml:606 | `Lainnya (Tanpa Bagian)` | **REPLACE** (Plan 04) | Konsistensi tier-0 label; ganti hanya kata "Bagian" jadi GetLabel(0), frasa "Lainnya (Tanpa ...)" dipertahankan. |
| Views/Admin/CreateAssessment.cshtml:272 | `Lainnya (Tanpa Bagian)` | **REPLACE** (Plan 04) | Sama EditAssessment:606. |
| Views/Admin/Index.cshtml:45 | `Kelola hierarki Bagian dan Unit kerja ...` | **SKIP** | Prose deskripsi card admin; combined "Bagian dan Unit" awkward di-split (D-04). |
| Views/Admin/Index.cshtml:61 | `Kelola nama tier organisasi (Bagian/Unit/Sub-unit) ...` | **SKIP** | Deskripsi card ManageOrgLevelLabels — harus literal supaya self-documenting (kalau dinamis jadi tautologis). |
| Views/Admin/ManageOrgLevelLabels.cshtml:17 | `Ubah nama tampilan tier (Bagian/Unit/Sub-unit/...)` | **SKIP** | Subtitle page CRUD label itu sendiri → circular bila pakai GetLabel. |
| Views/Admin/ManageOrganization.cshtml:115,159 | subtitle prose + modal title `Tambah Unit` | **SKIP** | Phase 342 page (out of scope; ORG-TREE-09 sudah handle modal title via JS — jangan double-handle). |
| Views/CDP/CoachingProton.cshtml:95 | `@* Bagian dropdown ... *@` | **SKIP** | Razor comment, tidak dirender. |

---

## §4 SKIP (noise — JANGAN ganti) — Whitelist grep-residual reviewer

Semua match selain REPLACE/AMBIGUOUS = SKIP. Kategori (detail `343-RESEARCH.md` L228-242):

| Kategori noise | Contoh | Reason |
|----------------|--------|--------|
| Import/doc field-description table | `ProtonData/ImportSilabus.cshtml:205-206`, `Admin/ImportWorkers.cshtml:205-206` (`<td>Bagian</td>` deskripsi kolom Excel) | Header file import = literal (D-04 explicit SKIP) |
| Element id / `for=` attr | `id="filterBagian"`, `for="filterBagian"`, `id="silabusBagian"`, `id="overrideBagian"` | JS bergantung id literal — hanya visible text berubah (Pitfall 1) |
| JS variable / function name | `selectedBagian`, `unitsByBagian`, `onBagianChange()`, `addBagian()`, `bagianSelects[]` | Kode JS, ganti = break |
| **JS string literal (alert/toast)** | **`Views/Admin/CoachCoacheeMapping.cshtml:723,724,805,806` → `alert('Pilih Bagian Penugasan.')`** | **JS literal di `<script>`, BUKAN display Razor → SKIP** (temuan baru phase ini) |
| ViewBag/property access | `ViewBag.Bagian`, `@row.Bagian`, `@w.Unit`, `@Model.Unit`, `bagian.Name` | C# property/data, schema unchanged (D-02) |
| Unit NAME (data) | `@bagian.Name` render, contoh "RFCC LPG Treating..." | Data, bukan tier label |
| Razor comment / render-nilai | `@* ... *@`, `_PSign.cshtml:42 <div>@Model.Unit</div>` | Comment tak dirender / render NILAI user (data) |

---

## §5 Controller Audit (ORG-INTEG-02)

> Grep `"...Bagian..."`/`"...Sub-unit..."` di `Controllers/`. Hasil: **near-zero actionable**.

| Controller:line | snippet | verdict | reason |
|-----------------|---------|---------|--------|
| CDPController.cs:3681,4015,4128,4205 | `"No","Nama","Bagian","Unit",...` | SKIP | Excel/CSV export header (schema file deterministik, bukan UI display) |
| CMPController.cs:3112,3607,3880 | `new[]{"Bagian","Kategori",...}` | SKIP | Excel export header |
| CoachMappingController.cs:1232 | `"Bagian Penugasan","Unit Penugasan"` | SKIP | Excel export header |
| ProtonDataController.cs:126,759,805,891 | interp key + header arrays | SKIP | string key (126) + Excel header |
| WorkerController.cs:174,850,878 | export/template header + cell instruction | SKIP | Excel export/template literal |
| WorkerController.cs:227,363 | `$"Bagian '{model.Section}' tidak ditemukan..."` | SKIP | ModelState validation error — deterministik, low value, D-02 minimize risk |
| DocumentAdminController.cs:106,287,299,315,380 | `TempData/Json message "Bagian tidak ditemukan."` | **SKIP (stretch dicatat)** | Kandidat REPLACE paling dekat ORG-INTEG-02, TAPI D-02 "minimize over-replace" → **DEFAULT SKIP**. Stretch opsional: inject `_orgLabels` ke DocumentAdminController + ganti message ke GetLabel(0). NOT dikerjakan default. |
| DocumentAdminController.cs:151,165,353,357,364,498,512,570 | audit log / `_logger` body | SKIP | Audit log + ILogger body (D-02 explicit SKIP — stabil untuk debug) |
| DocumentAdminController.cs:263 | `Name="Bagian Baru"` | SKIP | Default NAMA OrganizationUnit baru (data value) |
| OrgLabelController.cs:41 | `// Response example: {"0":"Bagian",...}` | SKIP | Code comment |

**Verdict FINAL ORG-INTEG-02:** Dipenuhi via **"audited, near-zero actionable"** — SEMUA controller display-bearing string = Excel/CSV export header / audit-log body / ModelState validation / property interpolation = **legit SKIP** per spec §4.8. Audit ini ADALAH coverage REQ. Satu-satunya kandidat (DocumentAdmin TempData/Json) = DEFAULT SKIP per D-02; opsi inject dicatat sebagai stretch, bukan default.

---

## §6 Success Criteria Mapping

| SC | Kriteria | Dipenuhi di |
|----|----------|-------------|
| **SC1** | Audit grep per file + keputusan eksplisit per occurrence (ganti vs skip) | **Dokumen ini** (§1-§5) + `343-RESEARCH.md` L118-262 |
| **SC2** | Rename label muncul di ≥3 page integrasi | Plan 02/03/04 swaps + manual spot-render (VALIDATION.md Manual-Only; formal E2E = Phase 344) |
| **SC3** | View Razor pakai @inject consistent, no hardcode display di area target | Plan 01 @inject global + Plan 02/03/04 swaps + grep residual per wave |
| **SC4** | Audit log body + literal xUnit TETAP statis "Bagian"/"Unit" | SKIP guards §4 + §5 controller (audit body SKIP) + setiap swap task Plan 02/03/04 |
| **SC5** | Controller string display dynamic; audit/test literal statis (ORG-INTEG-02) | §5 verdict (display-bearing = legit SKIP); test literal N/A Phase 343 (tests = Phase 344 TEST-01..06) → deferred-noted |

---

*Phase 343 — audit deliverable. Plan 01 Task 2. Sumber otoritatif: `343-RESEARCH.md`, `343-PATTERNS.md`, `343-CONTEXT.md` (D-01..D-04), `v21.0-ROADMAP.md` Phase 343.*
