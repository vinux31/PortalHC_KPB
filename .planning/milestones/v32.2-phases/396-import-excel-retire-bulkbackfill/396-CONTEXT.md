# Phase 396: Import Excel + retire BulkBackfill - Context

**Gathered:** 2026-06-18 (discuss interaktif, advisor-off)
**Status:** Ready for planning — semua keputusan produk LOCKED (4 area × ~3 Q = 12 keputusan)

<domain>
## Phase Boundary

Jalur **kedua** input jawaban untuk wizard inject `/Admin/InjectAssessment`: **template Excel ter-generate dari soal authored** (Step 1-3) + **parser matrix** (baris=NIP, kolom=soal) → masuk **`InjectAssessmentService.InjectBatchAsync` yang SAMA** dengan jalur form (nol duplikasi grading) → hasil byte-identik online. Plus **retire (hard-remove)** tool lama `BulkBackfill` (`TrainingAdminController.cs:787/836`, Admin-only, skor-saja) agar tak ada dua entry-point duplikat. Cakupan REQ **INJ-10** (import Excel + validasi atomic) + **INJ-11** (pensiun BulkBackfill).

**Scope-lock:** Sequential setelah 394 (file-overlap `InjectAssessmentController.cs` + `InjectAssessment.cshtml` + `InjectAssessmentService.cs` dengan 395/397 — bukan paralel; 395 lebih dulu). **0 migration** (semua tabel ada; Excel = lapisan parse→DTO `InjectAnswerSpec` yang service sudah konsumsi). Lib Excel = **ClosedXML 0.105.0** (sama BulkBackfill, `XLWorkbook`). RBAC `Admin,HC`. Atomic per-batch (rollback semua bila ≥1 error). Auto-generate = jalur form (395), BUKAN Excel.

</domain>

<decisions>
## Implementation Decisions

> Format: **D-0x** = keputusan user terkunci. "→ Catatan" = turunan teknis (Claude-resolved; planner/researcher boleh detailkan, jangan ubah keputusan).

### Penempatan & audience (Area 1)
- **D-01:** Excel = **toggle di dalam Step-5 'Jawaban'** — radio `Isi via Form` / `Import Excel`. Reuse seam `#step5Placeholder` yang sama dengan 395 (`InjectAssessment.cshtml:404`), **tak refactor pills/nav** (`goToStep` :503-549 tetap utuh). Bukan langkah/tab terpisah.
- **D-02:** **NIP di Excel WAJIB subset pekerja terpilih di worker picker Step-2.** Picker = **satu sumber audience** (siapa yang di-inject); Excel hanya mengisi jawaban mereka. NIP di baris Excel yang **tidak ada di picker → ditolak** (baris invalid → rollback, lihat D-09). NIP valid di `AspNetUsers` saja tidak cukup — harus juga terpilih di picker. (Catatan: validasi NIP D-05/394 picker-by-construction tetap berlaku.)
- **D-03:** **Mutually exclusive** — 1 room inject = **1 metode jawaban** (semua via form ATAU semua via Excel), tak boleh campur per-pekerja. Hindari merge state form vs file + aturan siapa-menang. → Toggle D-01 menentukan metode untuk seluruh room.

### Format & sel template (Area 2)
- **D-04:** Template = **multi-sheet**. **Sheet-1 = matrix isian** (baris=NIP, kolom = `Soal 1..N` urut soal authored; kolom kiri NIP + Nama informational). **Sheet-2 = legend**: tiap soal → teks soal + tipe (MC/MA/Essay) + `ScoreValue` + daftar **huruf opsi → teks opsi**. Sel MC/MA diisi **huruf opsi** (`A` untuk MC, `A,C` untuk MA — pola spec §7.1). → Template di-generate **dari soal authored saat itu** (in-flow, ber-TempId), kolom map balik by **urutan soal**; parser map huruf→opsi by **urutan opsi authored**. Urutan soal/opsi **wajib stabil** antara generate-template dan parse-upload.
- **D-05:** Essay = **2 kolom per soal**: `Skor` (0..ScoreValue) + `Teks jawaban (opsional)`. Teks **tidak wajib** di jalur Excel — rule D-04/395 ("teks essay wajib bila diisi") **di-scope ke mode FORM saja**; jalur Excel **exempt**. Teks tampil di `/CMP/Results` bila diisi; kosong = essay graded murni by `EssayScore` (tetap byte-identik). → Saat parse, isi `InjectAnswerSpec.EssayScore` (+`TextAnswer` bila ada).
- **D-06:** **Sel kosong = skip → grade 0.** Mekanis (KRITIS, sama 395 D-05): "skip" = **OMIT `InjectAnswerSpec`** soal itu, **BUKAN** kirim spec kosong (MC/MA kosong → reject-all D-03/393 :398-400). Service grade baris-hilang sebagai 0 via `AssessmentScoreAggregator`. **Konsisten warn-but-allow** — tak diblok; dampak skor terlihat di preview (D-08), HC konfirmasi.

### Lingkup & preview (Area 3)
- **D-07:** Excel = **jawaban eksplisit SAJA** (huruf opsi / skor essay). **TIDAK ada kolom skor-target** di Excel — auto-generate tetap **jalur form** (395). Excel & auto-gen = dua jalur berbeda, nol ambiguitas sel. → `BuildAutoGenAnswers` (395, server-side) **tetap reusable internal** bila kelak dibutuhkan, tapi **tidak di-expose** ke Excel di v1.
- **D-08:** **Preview dry-run WAJIB** pasca-upload, **sebelum commit**. Tampilkan tabel: NIP + Nama + **skor final aktual** + Lulus/Tidak + jumlah soal terjawab. Reuse **`PreviewInjectScore`** dry-run + engine **`AssessmentScoreAggregator.Compute`** (395 D-09 — pure/EF-free → **preview == commit**). HC verifikasi → klik commit. **JANGAN** preview nomor sertifikat (395 D-09: nomor tak ter-reserve pra-commit). Bukan commit-langsung pola BulkBackfill lama.
- **D-09:** Error report = **daftar LENGKAP per-baris/sel** — kumpulkan **semua** masalah sekaligus (mis. "baris 3 NIP 123 tak ada di picker", "baris 5 kol Soal-2 opsi E tidak valid", "baris 7 essay skor 15 > maks 10") agar HC perbaiki file **sekali jalan**. **Atomic**: tak ada yang ter-commit bila ada **≥1 error** (rollback total, pola BulkBackfill `transaction`). Bukan stop-di-error-pertama.

### Retire BulkBackfill (Area 4 — INJ-11)
- **D-10:** **HARD-REMOVE total** — hapus action **GET `BulkBackfill` (`:787`)** + **POST `BulkBackfillAssessment` (`:836-985`)** di `TrainingAdminController.cs` + hapus view **`Views/Admin/BulkBackfill.cshtml`**. Route lama → 404. (Bukan redirect 302; user pilih bersih total dari dead code.)
- **D-11:** **Hapus DUA entry-point UI** (bukan satu): (1) kartu Section D `Views/Admin/Index.cshtml:309`; **(2) dropdown-item `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:319`** — entry-point KEDUA yang ditemukan saat scout, **wajib ikut hapus** atau hard-remove route bikin link mati. Satu pintu masuk = kartu Inject Assessment baru di Section C (394).
- **D-12:** Kasus **"hanya punya skor akhir tanpa soal"** (yang dulu dilayani BulkBackfill skor-saja) **ditutup mode AUTO-GENERATE inject** (395): HC beri skor target → sistem sintesis jawaban + rincian per-soal lengkap. Retire **aman, nol fungsionalitas hilang** — inject baru justru lebih kaya (rincian per-soal + elemen teknis + cert). → Justifikasi INJ-11.

### Claude's Discretion (teknis — researcher/planner tetapkan)
- **Lokasi generator template + parser:** rekomendasi di `InjectAssessmentService.cs` / helper Excel terpisah (mis. `InjectExcelHelper`) agar controller tipis; reuse pola `XLWorkbook` gen (`TrainingAdminController.cs:1159/:1211`) + parse (`:862/:1298`).
- **Endpoint:** GET download-template + POST upload(+preview) di `InjectAssessmentController.cs`. Apakah preview (D-08) endpoint terpisah atau 2-fase 1-form (upload→preview→confirm) = discretion; ikut pola preview 395 (`PreviewInjectScore`).
- **Pemetaan kolom↔soal & huruf↔opsi:** by urutan stabil (D-04). Bentuk header kolom ("Soal 1", "S1 (MC)", dst), styling legend, format Nama-informational = discretion.
- **Nasib komentar-only refs** BulkBackfill (`InjectAssessmentService.cs:103`, `AssessmentAdminController.cs:4105`, `AdminBaseController.cs:263`) — historis; boleh dibiarkan atau light-touch update. **JANGAN** hapus `ManualDuplicatePredicate` (AdminBaseController) — masih dipakai AddManual/Import.
- Debounce/limit ukuran upload (pola `RequestFormLimits 10MB` BulkBackfill), copy notice, ikon.

### Folded Todos
[Tak ada todo yang di-fold. Lihat Reviewed Todos di bawah.]

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents WAJIB baca sebelum plan/implement.**

### Spec & requirements (sumber kebenaran)
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` — **§7.1** (Excel matrix: baris=NIP, kolom=soal, sel=huruf opsi `A`/`A,C`, essay skor/skor+teks, template dari paket authored, validasi NIP+opsi+atomic), **§11 F4** (deliverable: template generator + matrix parser + validasi atomic + pensiun BulkBackfill), **§2.1** (BulkBackfill existing: `:787` GET / `:836` POST, view, Section D, **Admin-only**), **§10** (atomic per-batch, audit, anti-double-cert), **§12** (out-of-scope: import gambar via Excel = TIDAK).
- `.planning/REQUIREMENTS.md` — **INJ-10** (import Excel batch, template dari soal, matrix, validasi atomic rollback), **INJ-11** (retire/redirect BulkBackfill, tak ada dua tool duplikat).
- `.planning/ROADMAP.md` — Phase 396 details + 5 Success Criteria + UI hint:yes; dependency sequential-setelah-394.

### CONTEXT carry-forward (keputusan terkunci)
- `.planning/phases/393-backend-core-inject/393-CONTEXT.md` — kontrak `InjectAssessmentService.InjectBatchAsync` + D-03 reject-all (MC==1/MA≥1), D-05 essay `EssayScore` 0..ScoreValue, atomic per-batch, `IsManualEntry=true` + AuditLog `"ManualInject"`, cert toggle.
- `.planning/phases/394-page-setup-room-authoring-soal/394-CONTEXT.md` — D-01 wizard nav-pills, D-02 6-langkah (Excel = isi Step-5), D-05 worker picker (audience), D-07 0-DB-write-sampai-commit.
- `.planning/phases/395-mode-jawaban-input-asli-auto-generate/395-CONTEXT.md` — **D-05 skip=omit→0 (basis D-06 396)**, **D-09 preview via Aggregator (basis D-08 396)**, **`BuildAutoGenAnswers` server-side (reusable, D-07)**, seam Step-5 `#step5Placeholder`, `#AnswersJson`/`ParseAnswerVms`/`MapToRequest:116`, `PreviewInjectScore`.

### Kode di-reuse / di-extend (verifikasi line saat plan)
- `Controllers/TrainingAdminController.cs` — **`:784-985` BulkBackfill GET+POST (HARD-REMOVE, D-10)** + **pola Excel parse `:862` (`XLWorkbook(stream)`, `LastRowUsed`, `Cell(r,c).GetString/TryGetValue`)** + **pola Excel gen `:1159/:1211` (`new XLWorkbook()`)** + parse `:1298` (reuse pola atomic `BeginTransactionAsync`/`RollbackAsync` :905/:980).
- `Services/InjectAssessmentService.cs:189-218` (write answers / konsumsi `InjectAnswerSpec`) + `:382-401` (validasi MC==1/MA≥1/essay) — Excel parser emit DTO yang sama; **nol perubahan backend grading** (happy-path).
- `Controllers/InjectAssessmentController.cs` — `MapToRequest` (`:116` isi `Answers`), `ParseQuestionVms` (`:126-139`), pola POST. Tambah endpoint template/upload/preview di sini.
- `Views/Admin/InjectAssessment.cshtml` — Step-5 seam `#step5Placeholder` (`:404`), `#btnInject` (`:479`), serialisasi `#QuestionsJson` (`:868-875`). Tambah toggle Form/Excel + UI upload + tabel preview di Step-5.
- `Helpers/AssessmentScoreAggregator.cs:26-60` — engine preview (D-08), `Compute` pure EF-free, identik commit.

### Hapus / bersihkan (D-10/D-11)
- `Views/Admin/BulkBackfill.cshtml` — **DELETE** (D-10).
- `Views/Admin/Index.cshtml:309` — hapus kartu Section D link `BulkBackfill` (D-11).
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:319` — **hapus dropdown-item link `BulkBackfill` (entry-point KEDUA, D-11)**.

### Test impact (verifikasi tak break)
- `HcPortal.Tests/DuplicateGuardTests.cs` — `#14` (`BulkBackfill_ExistingUserId_Skipped_NoSuccessIncrement` dll) **menguji `AdminBaseController.ManualDuplicatePredicate` bersama** (zero-drift), BUKAN memanggil action `BulkBackfillAssessment`. Hard-remove action **seharusnya tak break compile** test; predikat **TETAP** (dipakai AddManual/Import). Planner: konfirmasi tak ada compile-dependency + update nama/komentar test bila perlu (kosmetik).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **`InjectAssessmentService.InjectBatchAsync` + `InjectAnswerSpec`** (393) — Excel parser cukup emit `List<InjectWorkerSpec{Answers}>` yang sama dgn form; service grade & atomic sudah ada/teruji.
- **`AssessmentScoreAggregator.Compute` + `PreviewInjectScore`** (395) — preview dry-run Excel (D-08), preview==commit.
- **Pola ClosedXML** di `TrainingAdminController` — gen (`:1159/:1211`) + parse (`:862/:1298`) + atomic tx (`:905`) → tiru untuk template+matrix.
- **Seam Step-5 394/395** — `#step5Placeholder` + toggle metode; Excel = cabang radio kedua.

### Established Patterns
- Atomic batch: `BeginTransactionAsync` → SaveChanges → `CommitAsync`/`RollbackAsync` (BulkBackfill + InjectBatchAsync).
- Excel parse: header row 1 skip, data row 2+, `Cell(r,c).GetString().Trim()` + `TryGetValue<double>`.
- Validasi atomic all-or-nothing: kumpul error → reject sebelum write (D-09 perluas jadi daftar lengkap).
- Razor + JS toggle + file upload runtime → **Playwright wajib** (pelajaran 354): grep+build tak cukup untuk toggle/upload/preview.

### Integration Points
- `InjectAssessment.cshtml` Step-5 → toggle Form/Excel → (Excel) download-template + upload → preview → `#btnInject` → controller → `InjectBatchAsync`.
- **File-overlap SEQUENTIAL:** 396 extend controller/view/service yang sama dgn 395 (lebih dulu) & 397. Plan 396 setelah 395 ter-commit.
- Retire: route + 2 view-link + view-file dihapus; `ManualDuplicatePredicate` shared TIDAK disentuh.

### Risiko teknis utama (wajib ditangani plan)
- **Entry-point kedua** (`_AssessmentGroupsTab.cshtml:319`) — lupa hapus = link mati pasca hard-remove (INJ-11 tak tuntas).
- **Urutan soal/opsi stabil** (D-04) — template-gen vs parse harus map kolom↔soal & huruf↔opsi by urutan yang sama; soal authored ber-TempId, bukan ID DB.
- **Sel kosong = omit, bukan empty** (D-06) — kirim MC/MA kosong → reject-all batch (sama jebakan 395).
- **NIP di Excel tapi tak di picker** (D-02) — wajib ditolak (bukan auto-add), masuk daftar error (D-09).
- **DuplicateGuardTests compile** — pastikan tak panggil action yang dihapus.
- **Anti-double-cert** (spec §10) — Excel lewat `InjectBatchAsync` warisi guard dedup 393.

</code_context>

<specifics>
## Specific Ideas

- User (klarifikasi 394): *"saya ingin ada dua fasilitas import excel atau tulis manual ... untuk excel import di phase 396 ya"* → 396 = jalur Excel batch, **mutually exclusive** dgn form per-room (D-03).
- Benang merah pilihan user 396: **bersih & satu-pintu** (hard-remove total D-10, hapus 2 link D-11) + **aman untuk HC** (preview wajib D-08, error lengkap D-09, blank=skip bukan blok D-06) + **nol duplikasi mesin** (Excel → `InjectBatchAsync` yang sama).
- Prinsip menyeluruh (carry 393/395): hasil inject byte-identik online — Excel parser cuma translasi sel→`InjectAnswerSpec`, **jangan hitung skor sendiri**; service grade, Aggregator preview.

</specifics>

<deferred>
## Deferred Ideas

- **Auto-generate via Excel** (kolom skor-target per-NIP) — ditolak v1 (D-07); Excel eksplisit-saja, auto-gen di form 395. Bila kelak butuh → `BuildAutoGenAnswers` sudah reusable.
- **Redirect 302 BulkBackfill** (vs hard-remove) — ditolak (D-10 bersih total).
- **Pertahankan jalur skor-saja tanpa soal** — ditolak (D-12 ditutup auto-generate).
- **Import gambar soal via Excel** — out-of-scope spec §12 (gambar via UI authoring saja).
- **Campur form + Excel dalam 1 room** — ditolak (D-03 mutually exclusive).
- **Essay teks wajib di Excel** — ditolak (D-05 teks opsional; rule wajib hanya mode form).

### Reviewed Todos (not folded)
- `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` ("One-time cleanup data test/audit lokal setelah Phase 367") — **false-positive** keyword match (test/controllers); tugas cleanup DB lokal pasca-367, bukan scope import Excel 396. Tidak di-fold (sama keputusan 394).

</deferred>

---

*Phase: 396-import-excel-retire-bulkbackfill*
*Context gathered: 2026-06-18 (discuss interaktif, advisor-off; 4 area × 3 Q = 12 keputusan locked)*
