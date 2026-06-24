# Phase 415: Section Foundation + Import Excel Diperluas - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 415 (KEYSTONE, migration=TRUE) menghadirkan **fondasi Section**:
- Data model `AssessmentPackageSection` per-paket + kolom `PackageQuestion.SectionId` (nullable).
- UI admin: kelola (buat/edit/hapus/urut) Section + toggle `StartNewPage`+`ShuffleEnabled` per-Section + assign soal→Section.
- Import Excel diperluas: kolom No.Section/Nama Section + Opsi A–F, dual-format kompatibel-mundur, fingerprint dedup +Section+opsi.
- Validasi struktur Section antar-paket (D-13 tolak-keras).
- Sync Pre→Post ikut salin struktur Section (SEC-06).

Maps REQ: **SEC-01..06, IMP-01..03** (9 REQ).

OUT (fase lain): acak per-section (416), pagination (417), render/grading/authoring-form opsi dinamis A–F (418 — 415 HANYA siapkan kolom import Opsi A–F + data model dukung opsi >4), export label Section (419).
</domain>

<decisions>
## Implementation Decisions

### UI Kelola Section
- **D-415-01:** UI kelola Section = **panel inline di `Views/Admin/ManagePackageQuestions.cshtml`** (BUKAN halaman khusus / modal). HC lihat Section + soal-nya sekaligus, assign langsung. Sejalan alur kelola soal existing.

### Urut + Assign Soal→Section
- **D-415-02:** Urutan Section ditentukan **No.Section angka** (HC ketik 1,2,3 — D-04). Assign soal→Section = **dropdown pilih Section di form buat/edit soal**. JS minim (BUKAN drag-drop). Bulk-assign = OUT (defer; tak diminta).

### Template Excel + Dual-Format
- **D-415-03:** **SATU template universal diperluas** (tambah kolom No.Section + Nama Section + Opsi A–F). **Deteksi format otomatis by jumlah kolom**: ≤9 kolom = file lama (tanpa Section, opsi A–D — tetap di-import, IMP-02) ; >9 = format baru. HC TIDAK pilih template manual. Catatan: kolom Opsi E/F di template **disiapkan di 415** (import menerima + data model simpan); authoring-form + render + grading huruf A–F = **Phase 418**.

### Validasi Mismatch Struktur Section (D-13)
- **D-415-04:** Error 'struktur Section antar-paket tidak sama' muncul di **DUA titik**: (1) **saat upload import** — tolak + tampilkan **DAFTAR ketidakcocokan LENGKAP** (sebut SectionNumber + jumlah soal diharapkan vs aktual per paket) ; (2) **guard ulang saat mulai ujian** (cek drift edit manual pasca-import). Pesan jelas Bahasa Indonesia.

### Carried forward (locked di spec — TIDAK dibahas ulang)
- Section = entity baru per-paket, terpisah dari ElemenTeknis; 1 Section ⊃ banyak ET (D-02/D-03).
- `AssessmentPackageSection`(Id, AssessmentPackageId FK, SectionNumber int, Name nvarchar null, StartNewPage bit default 0, ShuffleEnabled bit default 1) + index unik `(AssessmentPackageId, SectionNumber)`. `PackageQuestion.SectionId` int? nullable (spec §5.1/5.2).
- Section opsional → kosong = perilaku global lama; soal tanpa Section = grup "Lainnya" di urutan akhir (D-05/D-15).
- migration=TRUE `AddAssessmentPackageSection` (tabel + kolom), non-breaking (SectionId=null backfill), rollback drop (spec §5.4/§11).
- Fingerprint dedup = hash(Q, OptA..F, SectionNumber) (spec §15.C).
- Sync Pre→Post (`SyncPackagesToPost`/`CopyPackagesFromPre`) salin record Section + SectionId + opsi 5–6 (SEC-06, spec §15.E).
- Nama Section = opsional (boleh kosong, label tampilan).

### Claude's Discretion
- Ekstraksi abstraksi urutan-soal `SectionAwareQuestionProvider`/`IQuestionSequence` di awal 415 (spec §13 — pangkas penyebaran ~23 titik di fase 416/417/419). Keputusan teknis planner.
- Mekanik migration EF, skema index, impl fingerprint, struktur view partial inline, lokasi seam validasi.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Design spec (PRIMARY — wajib baca)
- `docs/superpowers/specs/2026-06-22-section-scoped-shuffle-pagination-dynamic-options-design.md` — desain milestone v32.6; 15 keputusan D-01..D-15 + §15 addendum. Untuk 415: **§3** (decision log), **§5** (model data: AssessmentPackageSection + SectionId), **§9** (import Excel format/dual-format/validasi/fingerprint), **§11** (migrasi/backward-compat), **§13** (rencana fase + saran abstraksi), **§15.B** (timing validasi D-13), **§15.C** (fingerprint), **§15.D** (UI admin Section), **§15.E** (sync Pre→Post), **§15.G** (koreksi faktual: `QuestionOptionValidator` signature dll).

### Template referensi
- `docs/KPB - Licensor Training - SRU - Pre Test batch 1.xlsx` — template gaya ClassMarker (acuan kolom: Question Type, Question Categories=Section, Option 1..10, Page Number).
- `docs/Tipe A.xlsx` — format import existing 9-kolom (Pertanyaan, Opsi A–D, Jawaban Benar, Elemen Teknis, QuestionType, Rubrik).

### Project
- `.planning/REQUIREMENTS.md` — REQ SEC-01..06 + IMP-01..03.
- `docs/SEED_WORKFLOW.md` + `docs/DEV_WORKFLOW.md` — SOP seed UAT + develop.
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Models/AssessmentPackage.cs` — `PackageQuestion` (Order, ElemenTeknis, ScoreValue) + `PackageOption` (ICollection, tanpa batas). Tambah `SectionId` di `PackageQuestion`; entity baru `AssessmentPackageSection`.
- `Data/ApplicationDbContext.cs` — registrasi entity + index; tambah DbSet `AssessmentPackageSection` + index unik.
- `Controllers/AssessmentAdminController.cs` — `ImportPackageQuestions` (parser 9-kolom ~L6683), `DownloadQuestionTemplate` (~L6589), `CreateQuestion`/`EditQuestion` (~L7109), `MakePackageFingerprint` (~L7074), `ExtractPackageCorrectLetter` (~L7057, `'ABCD'`), `SyncPackagesToPost`/`CopyPackagesFromPre` (~L6359).
- `Views/Admin/ManagePackageQuestions.cshtml` — layar kelola soal → lokasi panel Section inline (D-415-01) + dropdown assign (D-415-02).
- `Views/Admin/ManagePackages.cshtml` — toggle shuffle existing (precedent toggle UI per-section nanti).
- `Helpers/ExcelExportHelper.cs` + ClosedXML 0.105 — template/parse Excel.
- `Helpers/QuestionOptionValidator.cs` — `ValidateQuestionOptions(string type, string?[] texts, bool[] corrects)` (signature §15.G).
- v13.0 org-tree (SortableJS) — precedent drag-drop (TIDAK dipakai 415 per D-415-02; catat masa depan).

### Established Patterns
- Import: parser-by-position + fingerprint dedup + cross-package count validation → jadikan **per-Section** (D-13).
- Migration additif nullable (pola Phase 352 image cols, Phase 409 removal cols) — non-breaking.

### Integration Points
- `PackageQuestion.SectionId` FK → `AssessmentPackageSection`.
- Import → auto-buat record Section dari No.Section/Nama saat commit.
- 415 menyiapkan data + import; dikonsumsi 416 (shuffle), 417 (pagination), 418 (opsi dinamis render), 419 (export).
</code_context>

<specifics>
## Specific Ideas
- Acuan kolom dari template ClassMarker referensi (Question Categories → Section; Option 1..N → Opsi A–F, max 6 per D-06).
- Dual-format: deteksi by jumlah kolom (≤9 lama / >9 baru) — HC tak pilih manual.
</specifics>

<deferred>
## Deferred Ideas
- Bulk-assign banyak soal ke Section sekaligus — future bila perlu (415 pakai per-soal dropdown).
- Drag-drop reorder Section (SortableJS) — future; 415 pakai No.Section angka.

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" (score 0.6) — tak relevan scope Section; tetap di backlog.
</deferred>

---

*Phase: 415-section-foundation-import-excel-diperluas*
*Context gathered: 2026-06-22*
