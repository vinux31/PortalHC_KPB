# Phase 419: Export Label Section + Polish + Test/UAT Milestone - Context

**Gathered:** 2026-06-24
**Status:** Ready for planning

<domain>
## Phase Boundary

Fase **TERAKHIR** milestone v32.6. Tiga tujuan:
1. **PAG-04** — bukti hasil per-soal (Excel "Detail Per Soal" + PDF per-peserta) menampilkan label/header Section ("Section {n}: {Nama}"), dengan huruf opsi A–F dinamis.
2. **Polish carry-over** — fix DEF-416-01 (predikat ET-coverage warning yang dead-code) + IN-01 (grouping `SectionId`→`SectionNumber`) + guard integritas LinkPrePost × Section.
3. **Test/UAT/Audit milestone** — suite test baru hijau + test lama hijau (kompatibel-mundur) + Playwright real-browser UAT keempat interaksi lintas-milestone + audit milestone PASSED 20/20 REQ sebelum ship.

**Bukan scope (carry-forward terkunci):**
- D-12: TANPA breakdown skor per-Section di hasil/sertifikat — Section di export = label organisasi, BUKAN skor. (→ SREP-01 deferred)
- D-11: kontrol halaman di tingkat Section, bukan per-soal.
- Tipe soal baru, page-number per-soal, Excel zero-config — semua Out of Scope v32.6.

</domain>

<decisions>
## Implementation Decisions

### Export Label Section (PAG-04) — REQ inti
- **D-01:** Format export = **Excel band-header + PDF heading**.
  - **Excel `AddDetailPerSoalSheet`** (`Helpers/ExcelExportHelper.cs:50`, matrix lebar 1-baris/peserta, kolom per-soal): urutkan kolom soal per **(SectionNumber, Order)** (saat ini `OrderBy(q.Order)` saja), lalu tambah **baris header merged** di atas grup kolom tiap Section bertuliskan `"Section {n}: {Nama}"`. Soal tanpa Section masuk grup **"Lainnya"** di urutan terakhir (SEC-03).
  - **PDF `GeneratePerPesertaPdf`** (`AssessmentAdminController.cs:5688`, list soal vertikal/peserta): sisipkan **heading `"Section {n}: {Nama}"`** sebelum blok soal tiap Section; "Lainnya" terakhir.
  - **Kompatibel-mundur:** assessment tanpa Section → semua soal di grup "Lainnya" tunggal yang mempertahankan urutan `q.Order` → tampilan praktis identik dengan sekarang (header band/heading "Lainnya" boleh disembunyikan bila hanya 1 grup tanpa Section — keputusan kecil → Claude's Discretion).
  - **Huruf opsi A–F dinamis** (Phase 418) WAJIB konsisten di export (reuse `AssessmentScoreAggregator.BuildAnswerCell`/`IsQuestionCorrect` — kill-drift dengan web Results).

### Guard LinkPrePost × Section
> **⛔ D-02 DI-DROP ke backlog 999.16 (keputusan user 2026-06-24, saat eksekusi 419).** Temuan: `InjectQuestionSpec` tak punya `SectionId` → paket inject SELALU all-Lainnya → satu-satunya surface LinkPrePost (jalur inject Phase 397) selalu kena skip-on-all-Lainnya → guard **no-op/tak teramati**. Logika banding sudah ada & teruji (`SectionStructureComparer` + `SectionMismatchGuardTests`). Plan 03 DIHAPUS. SEC-06 sync audit TETAP (Plan 05). Promosikan 999.16 bila ada surface LinkPrePost non-inject. Teks asli di bawah disimpan untuk konteks.

- **D-02 (DROPPED):** **Hard-block bila struktur Section beda — TAPI skip bila ada sisi all-Lainnya.** LinkPrePost (Phase 397) **menolak** menaut Pre↔Post bila **KEDUA** sisi punya Section nyata dan strukturnya tidak identik (jumlah Section + jumlah soal per-SectionNumber per paket berbeda), dengan pesan jelas (sebut SectionNumber + jumlah diharapkan vs aktual). Reuse helper **`SectionStructureComparer.MismatchedSections`** (SEC-04, Phase 415) + pola pemanggilan `CMPController.StartExam:1098-1119` (`guardAnySections`) — jangan tulis predikat baru.
  - **Resolusi Open-Q research (user 2026-06-24):** bila **salah satu** sisi **tanpa Section sama sekali** (all-"Lainnya" — mis. paket buatan **Inject** yang `InjectQuestionSpec` tak punya field Section) → **LEWATI guard** (boleh link). Konsisten dengan SEC-04 StartExam `guardAnySections` (skip bila tak ada Section di salah satu sisi). Mencegah memblok semua link inject-Pre ↔ room ber-Section.
  - Rasional: SEC-06 sync deep-clone menjamin struktur identik HANYA untuk jalur sync SamePackage; LinkPrePost menaut room **existing** yang bisa di-author independen → struktur bisa divergen tanpa guard. Ini perilaku BARU (bukan audit-only). Research: surface LinkPrePost = **inject-based** (Phase 397); tak ada surface non-inject lain.

### Fix ET-coverage Warning (DEF-416-01 + IN-01)
- **D-03:** **Full re-spec predikat.** Lokasi `AssessmentAdminController.cs:7673-7680` (`ViewBag.SectionEtWarnings`).
  - Predikat lama `DistinctEt > K` dengan `K = COUNT(soal SectionId=s.Id)` dan `DistinctEt = distinct ET dalam 1 paket` **tak pernah** true (tiap PackageQuestion punya 1 ET → distinct ET ≤ jumlah soal). Dead-code.
  - **Re-spec:** `DistinctEt` = distinct ElemenTeknis dari **pool soal Section yang sama LINTAS paket-saudara (sibling)** dalam satu assessment; `K = min(count soal Section antar paket-saudara)` (kuota yang benar-benar dipresentasikan ke peserta). Warning fire bila `DistinctEt > K` (sebagian ET tak terjamin muncul).
  - **IN-01:** matching Section lintas-sibling pakai **`SectionNumber`** (bukan `SectionId` — sibling beda Id, sama nomor); selaraskan grouping warning ke `SectionNumber`.
  - Sertakan **test positif** yang membuat predikat fire bermakna (sebelumnya hanya S3/S3b yang buktikan non-blocking).
  - Tetap **NON-BLOCKING** (sinyal, bukan error) — D-416-03 load-bearing tak berubah.

### Test / UAT / Audit Milestone
- **D-04:** UAT real-browser live @5277 mencakup **keempat** interaksi (semua dipilih):
  1. **Lifecycle Section inti** — Section + scoped-shuffle + pagination + opsi 2–6 end-to-end (create→assign section→ujian→render A–F→resume→export label).
  2. **Inject v32.2 × Section** — inject hasil manual saat paket ber-Section + opsi 5–6 (handle section/huruf dinamis benar).
  3. **LinkPrePost 397 × Section** — link Pre↔Post struktur Section sama (sukses) vs beda (tertolak — uji guard D-02).
  4. **Add/Remove v32.5 × Section** — tambah/hapus peserta live saat ujian ber-Section + pagination aktif (eager-assign per-section konsisten, mirror Phase 416 interaksi `AddParticipantsLive`).
- **D-05:** Suite test baru = test export label Section (Excel band-header + PDF heading + backward-compat no-Section) + test positif ET-warning (D-03) + test guard LinkPrePost (D-02). Test lama WAJIB tetap hijau (kompatibel-mundur = Section kosong). Cakupan unit `BuildSectionQuestionAssignment` isolasi-section + mismatch/dual-format/fingerprint + Playwright pagination/opsi-5–6/resume sebagian besar SUDAH ada di Phase 415–418 → 419 menambah yang belum + audit ulang lintas-fitur.
- **Audit milestone**: PASSED 20/20 REQ ter-cover + integrasi koheren sebelum ship. migration=FALSE.

### Folded Todos
- **D-06 (folded):** `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` ("One-time cleanup data test/audit lokal setelah Phase 367 ship", area: database) → di-fold ke 419 sebagai langkah **pasca-UAT/pra-ship**: bersihkan seed/data test lokal hasil UAT sesuai SEED_WORKFLOW (snapshot→restore), tandai journal `cleaned`. 419 = fase ship terakhir → tempat tepat.

### Claude's Discretion
- Penyembunyian header/heading "Lainnya" saat assessment tanpa Section (1 grup tunggal) — pilih yang paling backward-compatible secara visual.
- Detail styling Excel (merge range, warna band-header) & layout heading PDF (font/spacing).
- Urutan/struktur file test baru & nama spec Playwright.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & keputusan milestone
- `docs/superpowers/specs/2026-06-22-section-scoped-shuffle-pagination-dynamic-options-design.md` — 15 keputusan D-01..D-15 + §15 addendum re-check; khususnya **D-11** (kontrol halaman per-Section), **D-12** (TANPA breakdown skor per-Section di hasil/sertifikat).
- `.planning/REQUIREMENTS.md` — **PAG-04** (satu-satunya REQ 419) + traceability 20/20.
- `.planning/ROADMAP.md` §"Phase 419" — Goal, Success Criteria, File-overlap final, **Carry-over Phase 416 (DEF-416-01/WR-01)**.

### Carry-over & deferred
- `.planning/phases/416-scoped-shuffle-acak-per-section/deferred-items.md` — **DEF-416-01** detail penuh (predikat ET-warning dead-code + saran re-spec + DISPOSISI fix-di-419).

### Kode yang disentuh (integration points)
- `Helpers/ExcelExportHelper.cs:50` (`AddDetailPerSoalSheet`) + `:119` (`AddElemenTeknisSheet` referensi pola).
- `Controllers/AssessmentAdminController.cs:5688` (`GeneratePerPesertaPdf`), `:5434` (call `AddDetailPerSoalSheet`), `:7673-7680` (predikat ET-warning yang di-re-spec).
- `Helpers/AssessmentScoreAggregator.cs` (`BuildAnswerCell`/`IsQuestionCorrect` — kill-drift export↔web).
- LinkPrePost (Phase 397) endpoint — **researcher: lokasikan** action link Pre↔Post + titik validasi untuk sisip guard D-02.
- `SyncPackagesToPost`/`CopyPackagesFromPre` (SEC-06) — audit ulang SEMUA pemicu sync salin Section + opsi (file-overlap roadmap).

[Catatan: `Section` di banyak titik controller = org-unit pekerja (`u.Section`), BUKAN `AssessmentPackageSection`. Section grup-soal diakses via `PackageQuestion.SectionId`/`q.Section` + tabel `AssessmentPackageSection`.]

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentScoreAggregator.BuildAnswerCell` / `IsQuestionCorrect` — sumber tunggal Jawaban + Benar? (sudah dipakai Excel + PDF + web Results). Export label Section TIDAK boleh mengubah ini (kill-drift v30.0/386).
- Fingerprint identitas per-Section (SEC-04, Phase 415) — reuse untuk guard LinkPrePost (D-02), jangan tulis predikat baru.
- `SectionEtWarning` record (`AssessmentAdminController.cs:7686`) — pertahankan, ganti sumber data `DistinctEt`/`K` (D-03).

### Established Patterns
- Export = ClosedXML (`XLWorkbook`) per-sheet; header bold + freeze rows; `Columns().AdjustToContents()`.
- PDF = QuestPDF (`GeneratePerPesertaPdf`).
- Section ordering canonical = `OrderBy(SectionNumber)` lalu `OrderBy(q.Order)` dalam Section (mirror `ShuffleEngine`/`SectionPaginator` Phase 416/417).

### Integration Points
- Export Excel: `AddDetailPerSoalSheet` (reorder + band-header).
- Export PDF: `GeneratePerPesertaPdf` (heading antar-blok).
- Guard: LinkPrePost action (Phase 397) — sisip blok struktur-Section-mismatch.
- Warning: `AssessmentAdminController.cs:7673-7680` (re-spec predikat lintas-sibling).
- Sync audit: `SyncPackagesToPost`/`CopyPackagesFromPre`.

</code_context>

<specifics>
## Specific Ideas

- Label persis: `"Section {n}: {Nama}"` (n = SectionNumber). Grup tanpa Section = `"Lainnya"`, selalu terakhir.
- Guard D-02 mismatch message: sebut SectionNumber + jumlah soal diharapkan (Pre) vs aktual (Post) — selaras gaya pesan SEC-04.
- UAT WAJIB real-browser (lesson 354 — Razor/JS/SignalR tak bisa diuji unit); DB snapshot→restore tiap skenario (SEED_WORKFLOW).

</specifics>

<deferred>
## Deferred Ideas

- **SREP-01** — breakdown skor per-Section di hasil/sertifikat. Terkunci D-12 = tidak untuk v32.6 (export hanya label organisasi). Buka bila diminta.
- **SAMP-01** — sampling "ambil N dari M" per-Section. Ditolak brainstorm Q4; v2.
- **Excel zero-config** (dropdown Data Validation + import skor per-soal) — milestone quick-win terpisah ([[project_excel_zeroconfig_dropdown_deferred]]).

### Reviewed Todos (not folded)
- (tidak ada — satu-satunya todo cocok di-fold ke scope D-06)

</deferred>

---

*Phase: 419-export-label-section-polish-test-uat-milestone*
*Context gathered: 2026-06-24*
