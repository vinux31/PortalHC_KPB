# Phase 396: Import Excel + retire BulkBackfill — Research

**Researched:** 2026-06-18
**Domain:** ASP.NET Core MVC (.NET) + EF Core + ClosedXML 0.105.0 — Excel template generation + matrix parsing, reuse `InjectBatchAsync`/`PreviewInjectScore`, hard-remove legacy `BulkBackfill`.
**Confidence:** HIGH (semua klaim teknis terverifikasi file:line di working tree; tidak ada library eksternal baru).

---

<user_constraints>
## User Constraints (from 396-CONTEXT.md)

### Locked Decisions (D-01..D-12 — JANGAN re-litigate; research HOW bukan WHETHER)
- **D-01:** Excel = toggle radio "Isi via Form" / "Import Excel" **di dalam Step-5** wizard. Reuse seam `#step5Placeholder`/`#step5Root` (`InjectAssessment.cshtml:404-406`, verified). JANGAN refactor pills/nav (`goToStep` :619, btnPrev5/Next5 :524/527). Bukan langkah/tab terpisah.
- **D-02:** NIP di Excel **WAJIB subset** pekerja terpilih worker picker Step-2 (picker = satu sumber audience). NIP di baris Excel yang tidak ada di picker → **ditolak** (baris invalid → rollback). NIP valid di `AspNetUsers` saja tidak cukup — harus juga terpilih di picker.
- **D-03:** **Mutually exclusive** — 1 room = 1 metode (semua via form ATAU semua via Excel), tak campur per-pekerja. Toggle D-01 menentukan metode untuk seluruh room.
- **D-04:** Template **MULTI-SHEET** — Sheet-1 = matrix isian (kolom NIP + Nama + Soal 1..N urut authored), Sheet-2 = legend (soal→teks+tipe+ScoreValue+huruf opsi→teks). Sel MC/MA = huruf opsi (`A` untuk MC, `A,C` untuk MA). Urutan soal/opsi **wajib stabil** antara generate-template & parse-upload.
- **D-05:** Essay = 2 kolom per soal (`Skor` 0..ScoreValue + `Teks jawaban` opsional). Teks TIDAK wajib di jalur Excel (rule teks-wajib 395 D-04 di-scope ke mode FORM saja).
- **D-06:** Sel kosong = skip → **OMIT** `InjectAnswerSpec` (BUKAN kirim spec kosong) → grade 0. Konsisten warn-but-allow. Kirim MC/MA kosong = reject-all (HINDARI).
- **D-07:** Excel = jawaban **eksplisit SAJA** (TIDAK ada kolom skor-target; auto-gen tetap jalur form 395). `BuildAutoGenAnswers` tetap reusable internal tapi **tidak di-expose** ke Excel v1.
- **D-08:** Preview dry-run **WAJIB** pasca-upload sebelum commit (tabel NIP+Nama+skor final+lulus+jumlah terjawab) via reuse `PreviewInjectScore` + `AssessmentScoreAggregator.Compute` → **preview == commit**. JANGAN preview nomor cert.
- **D-09:** Error report = daftar **LENGKAP** per-baris/sel (kumpul semua masalah), **atomic** (tak commit bila ≥1 error).
- **D-10:** **HARD-REMOVE** BulkBackfill: hapus GET `BulkBackfill` + POST `BulkBackfillAssessment` di `TrainingAdminController.cs` + DELETE `Views/Admin/BulkBackfill.cshtml`. Route → 404 (bukan redirect 302).
- **D-11:** Hapus **DUA** entry-point UI: kartu Section D (`Views/Admin/Index.cshtml`) + dropdown-item (`Views/Admin/Shared/_AssessmentGroupsTab.cshtml`). JANGAN hapus `ManualDuplicatePredicate` (`AdminBaseController:263`, dipakai AddManual/Import).
- **D-12:** Kasus skor-saja ditutup auto-generate inject (395) — retire aman, nol fungsionalitas hilang.

### Claude's Discretion (teknis — researcher/planner tetapkan)
- Lokasi generator template + parser: rekomendasi helper Excel terpisah (mis. `InjectExcelHelper`) agar controller tipis; reuse pola `XLWorkbook` gen (`TrainingAdminController.cs:1159/:1211`) + parse (`:862/:1298`).
- Endpoint: GET download-template + POST upload(+preview) di `InjectAssessmentController.cs`. Preview = endpoint terpisah ATAU 2-fase 1-form = discretion.
- Bentuk header kolom ("Soal 1", dst), styling legend, format Nama-informational = discretion.
- Debounce/limit ukuran upload (`RequestFormLimits 10MB` pola BulkBackfill), copy notice, ikon.
- Nasib komentar-only refs BulkBackfill (`AssessmentAdminController.cs:4105`, `AdminBaseController.cs:263`) — boleh dibiarkan/light-touch.

### Deferred Ideas (OUT OF SCOPE)
- Auto-generate via Excel (kolom skor-target per-NIP) — ditolak v1 (D-07).
- Redirect 302 BulkBackfill (vs hard-remove) — ditolak (D-10).
- Pertahankan jalur skor-saja tanpa soal — ditolak (D-12 ditutup auto-generate).
- Import gambar soal via Excel — out-of-scope spec §12.
- Campur form + Excel dalam 1 room — ditolak (D-03).
- Essay teks wajib di Excel — ditolak (D-05).
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **INJ-10** | Import jawaban/skor batch via Excel: template ter-generate dari paket soal authored + format matrix (baris=NIP, kolom=soal) + validasi atomic (NIP valid, opsi valid, rollback bila error). | §3 Template generation (ClosedXML multi-sheet, `ExcelExportHelper.ToFileResult` verified), §4 Matrix parser (huruf↔opsi by stable order), §5 Reuse `InjectBatchAsync` (validasi MC==1/MA≥1/essay 0..ScoreValue sudah ada `:382-408`) + `PreviewInjectScore` (D-08, `MapToInMemory`+`Compute` verified `:106-156`), §6 Atomic error-collection (`PreflightValidateAsync` reject-all + tx). |
| **INJ-11** | Retire BulkBackfill — hard-remove, tidak ada dua tool duplikat. | §7 Hard-remove (line ranges exact `TrainingAdminController.cs:776-790` + `:831-985`, 2 UI links, view file, DuplicateGuardTests TIDAK panggil action → tak break compile). |
</phase_requirements>

---

## Summary

Phase 396 menambah **jalur input kedua** (Excel batch) ke wizard inject `/Admin/InjectAssessment` Step-5, dan **menghapus** tool legacy `BulkBackfill`. Tidak ada library baru, tidak ada migration, tidak ada logic grading baru — semua mesin (parse Excel ClosedXML, grade via `InjectBatchAsync`, preview via `AssessmentScoreAggregator.Compute`) sudah ada dan terverifikasi di working tree.

**Temuan kritikal yang mengubah asumsi tugas:** **Phase 395 SUDAH ter-commit** (HEAD `929a6c2e feat(395-03)`, commits `561944f7`..`929a6c2e`). Seam yang dijanjikan 395 — `PreviewInjectScore` endpoint (`InjectAssessmentController.cs:106`), `BuildAutoGenAnswers`/`ComputeAutoGenSeed` (`InjectAssessmentService.cs:510/540`), `#AnswersJson`/`ParseAnswerVms` (`view:314`/`controller:327`), `MapToRequest` mengisi `Answers` (`:208`), toggle Step-5 (`view:401-533`) — **SEMUA ada di tree saat ini**. Ketidakpastian "395 mungkin belum dieksekusi" di prompt **TERSELESAIKAN: 395 ada**. Planner BOLEH bergantung pada `PreviewInjectScore` & `MapToInMemory` apa adanya; 396 tidak perlu mendefinisikan endpoint preview sendiri.

Excel parser hanya **menerjemahkan sel → `InjectAnswerSpec`** (huruf opsi → `SelectedOptionTempIds`, skor essay → `EssayScore`), lalu memasukkannya ke jalur yang **identik** dengan form. Cara terbersih: parser server-side menghasilkan `List<InjectWorkerAnswersVM>` yang **sama bentuknya** dengan payload `#AnswersJson` form, sehingga preview (`PreviewInjectScore`) dan commit (`MapToRequest`→`InjectBatchAsync`) bekerja tanpa perubahan. Mesin grade/validasi tidak disentuh.

**Primary recommendation:** Buat `Helpers/InjectExcelHelper.cs` (static, EF-free) dengan `GenerateTemplate(questions, workerNipNames) → XLWorkbook` (2 sheet) dan `ParseMatrix(stream, questions, allowedNips) → (List<InjectWorkerAnswersVM> workers, List<InjectRowError> errors)`. Tambah 2 endpoint di `InjectAssessmentController` (GET `DownloadInjectTemplate`, POST `UploadInjectExcel` → preview JSON). Reuse `ExcelExportHelper.ToFileResult` untuk download, `PreviewInjectScore` untuk preview, `MapToRequest`→`InjectBatchAsync` untuk commit. Hard-remove BulkBackfill di 2 blok line non-kontigu (`776-790` + `831-985`) + 2 UI link + view file. Kunci pemetaan kolom↔soal: **urutan soal authored stabil** (`OrderBy(Order).ThenBy(TempId)`, identik dengan persist `InjectAssessmentService.cs:146` & auto-gen `:550`); huruf opsi↔TempId by **urutan opsi authored** (huruf A=opsi pertama, dst).

---

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Template generation (XLWorkbook 2-sheet) | API / Backend (`Helpers/InjectExcelHelper` + controller GET) | — | Server tahu urutan soal/opsi authored stabil; klien hanya pegang `injQuestions[]` (rapuh untuk file binary). |
| Matrix parsing (huruf↔opsi, blank=skip) | API / Backend (`InjectExcelHelper.ParseMatrix`) | — | Parsing + validasi D-02/D-06/D-09 = server-authoritative; jangan parse di JS. |
| Preview skor (D-08) | API / Backend (`PreviewInjectScore` existing) | Browser (render tabel preview) | Engine `AssessmentScoreAggregator.Compute` EF-free = preview==commit; klien hanya menampilkan. |
| Commit grade/cert/audit | API / Backend (`InjectBatchAsync` existing) | — | Byte-identik online; nol duplikasi (393). |
| Toggle Form/Excel + upload UI + tabel preview | Browser / Client (Razor + JS di Step-5) | Frontend Server (Razor render) | D-01 toggle radio + `<input type=file>` + render tabel; runtime → Playwright wajib (lesson 354/392). |
| Audience selection (siapa di-inject) | Browser (worker picker Step-2) | API (resolve UserId→NIP) | D-02 picker = satu sumber; Excel hanya mengisi jawaban subset. |
| Hard-remove BulkBackfill | API / Backend (`TrainingAdminController`) + Frontend Server (2 view link + view file) | — | Route+UI+view dihapus bersamaan agar tak ada link mati (D-10/D-11). |

---

## Standard Stack

### Core (semua SUDAH ada — nol install)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | **0.105.0** | Generate/parse `.xlsx` (`XLWorkbook`) | `[VERIFIED: HcPortal.csproj]` Sama lib BulkBackfill + ImportTraining + semua export Excel existing. |
| EF Core | (project default) | Persist via `InjectBatchAsync` | `[VERIFIED]` Tidak ada query/tabel baru. |
| ASP.NET Core MVC | (project default) | Controller endpoints + `IFormFile` upload | `[VERIFIED]` Pola `IFormFile? excel` + `[RequestFormLimits]` ada di BulkBackfill `:835`. |

### Supporting helpers (reuse)
| Helper | File:line | Purpose | When to Use |
|--------|-----------|---------|-------------|
| `ExcelExportHelper.ToFileResult(workbook, fileName, controller)` | `Helpers/ExcelExportHelper.cs:28` `[VERIFIED]` | Adjust columns + SaveAs MemoryStream + return `FileContentResult` (mime `...spreadsheetml.sheet`) | Download template (GET). |
| `ExcelExportHelper.CreateSheet(wb, name, headers)` | `:14` `[VERIFIED]` | Tambah worksheet + bold header row 1 | Opsional untuk sheet matrix/legend. |
| `AssessmentScoreAggregator.Compute(questions, responses, passPct)` | `Helpers/AssessmentScoreAggregator.cs:26` `[VERIFIED]` returns `ScoreAggregateResult(TotalScore, MaxScore, Percentage, IsPassed)` | Engine preview (D-08) — sudah dipanggil oleh `PreviewInjectScore`. | Tidak dipanggil langsung; lewat endpoint preview. |
| `InjectAssessmentController.MapToInMemory(...)` | `:283` (private) `[VERIFIED]` | Map pola → in-memory POCO untuk `Compute` (TempId=Id sintetis) | Sudah dipakai `PreviewInjectScore`; Excel reuse endpoint, bukan helper ini langsung. |
| `AdminBaseController.NormalizeTitleForDup` | dipakai `InjectAssessmentService.cs:452,465` `[VERIFIED]` | Normalisasi judul dedup | Otomatis via `InjectBatchAsync` (D-01/D-02). |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Parser server-side menghasilkan `List<InjectWorkerAnswersVM>` (sama bentuk `#AnswersJson`) | Parser menghasilkan `InjectRequest.Workers` langsung | VM-route lebih bersih: preview (`PreviewInjectScore`) + commit (`MapToRequest`) memakai jalur yang SUDAH ada tanpa cabang baru. Direct-route menduplikasi resolve cert/CertValidUntil. **Rekomendasi: VM-route.** |
| Helper terpisah `InjectExcelHelper` | Method di `InjectAssessmentService` | Helper EF-free static = unit-testable murni (round-trip generate→parse tanpa DB), controller tipis. **Rekomendasi: helper terpisah.** |
| Preview reuse `PreviewInjectScore` per-worker (sudah ada) | Endpoint preview-batch baru | Per-worker endpoint sudah teruji (395). Untuk tabel preview multi-NIP, controller upload bisa loop panggil engine yang sama. **Rekomendasi: reuse engine, batch loop di endpoint upload.** |

**Installation:** Tidak ada. (Verifikasi: `grep -i closedxml HcPortal.csproj` → `Version="0.105.0"`.)

---

## Architecture Patterns

### System Architecture Diagram

```
                          ┌─────────────────────────────────────────────┐
   HC (browser)           │   Step-5 wizard /Admin/InjectAssessment      │
        │                 │   [Toggle radio: "Isi via Form" | "Import    │
        │                 │    Excel"]  (D-01 di #step5Root, room-level)  │
        ▼                 └─────────────────────────────────────────────┘
  ┌──────────────┐   form path (395, existing)        excel path (396, NEW)
  │ Step-2 picker│        │                                   │
  │ UserIds[]    │        ▼                                   ▼
  │(value=Id)    │  per-worker form  ┌──────────────┐  ┌──────────────────┐
  └──────┬───────┘  → #AnswersJson   │ GET Download │  │ POST Upload      │
         │                           │ InjectTemplate│  │ InjectExcel      │
         │ (selected UserIds → NIP)  └──────┬───────┘  │ (IFormFile)      │
         ▼                                  │          └────────┬─────────┘
  ┌────────────────────────┐               ▼                   │
  │ Controller resolves     │   InjectExcelHelper.Generate     ▼
  │ userIdToNip (existing   │   Template(questions,        InjectExcelHelper.ParseMatrix
  │ :57-59)                 │   workerNipNames)            (stream, questions, allowedNips)
  └──────────┬─────────────┘   → XLWorkbook 2-sheet         │  validate D-02/D-06/D-09
             │                  → ExcelExportHelper.ToFile   │  → (workers VM, errors[])
             │                  Result (.xlsx download)      │
             │                                               ▼
             │                                        ┌───────────────────┐
             │                            errors≥1 ───│ reject-all,       │
             │                                        │ return error list │ (D-09 atomic)
             │                                        │ NO write          │
             │                                        └─────────┬─────────┘
             │                              0 errors            │
             │                                                  ▼
             │                                        preview each NIP via
             │                                        AssessmentScoreAggregator.Compute
             │                                        (engine = PreviewInjectScore, D-08)
             │                                                  │
             ▼                                                  ▼
  ┌──────────────────────────────────────────────────────────────────────┐
  │ #AnswersJson (serialized List<InjectWorkerAnswersVM>) — SAME field     │
  │ for both paths → submit → MapToRequest (:163) → InjectRequest          │
  └────────────────────────────────┬─────────────────────────────────────┘
                                    ▼
                   InjectAssessmentService.InjectBatchAsync (393, existing)
                   PreflightValidate (reject-all D-03/D-09) → tx atomic →
                   GradeAndCompleteAsync → cert backdate → AuditLog "ManualInject"
                                    ▼
                   /CMP/Records "Assessment Online" + /CMP/Results per-soal (gratis)
```

### Recommended Project Structure (file touch map)
```
Helpers/
└── InjectExcelHelper.cs        # NEW — GenerateTemplate(...) + ParseMatrix(...) static, EF-free
Controllers/
├── InjectAssessmentController.cs   # EXTEND — +GET DownloadInjectTemplate +POST UploadInjectExcel
└── TrainingAdminController.cs       # REMOVE — BulkBackfill GET+POST (lines 776-790 + 831-985)
ViewModels/
└── InjectAssessmentViewModel.cs     # (opsional) +InjectExcelMode flag / preview rows holder
Views/Admin/
├── InjectAssessment.cshtml          # EXTEND Step-5 — toggle Form/Excel + upload + preview tabel
├── BulkBackfill.cshtml              # DELETE (D-10)
├── Index.cshtml                     # REMOVE card (D-11)
└── Shared/_AssessmentGroupsTab.cshtml # REMOVE dropdown-item + divider (D-11)
HcPortal.Tests/
└── InjectExcelHelperTests.cs        # NEW — round-trip + cell-mapping + validation unit tests
tests/e2e/
└── inject-assessment-396.spec.ts    # NEW — download→fill→upload→preview→commit + route 404
```

### Pattern 1: ClosedXML multi-sheet template generation
**What:** Buat `XLWorkbook` dengan 2 worksheet (matrix + legend), tulis header + baris, kembalikan via `ExcelExportHelper.ToFileResult`.
**When to use:** GET `DownloadInjectTemplate`.
**Example (pola VERIFIED dari TrainingAdminController.cs:1159-1255):**
```csharp
// Source: [VERIFIED: Controllers/TrainingAdminController.cs:1159-1203, ExcelExportHelper.cs:28]
using var wb = new XLWorkbook();
var matrix = wb.Worksheets.Add("Jawaban");          // Sheet-1
// header: NIP | Nama | Soal 1 | Soal 2 | ... (essay → "Soal k Skor" + "Soal k Teks")
matrix.Cell(1, 1).Value = "NIP"; matrix.Cell(1, 2).Value = "Nama";
int col = 3;
foreach (var q in questions.OrderBy(q => q.Order).ThenBy(q => q.TempId)) {
    if (q.QuestionType == "Essay") {
        matrix.Cell(1, col++).Value = $"Soal {idx} Skor (0..{q.ScoreValue})";
        matrix.Cell(1, col++).Value = $"Soal {idx} Teks (opsional)";
    } else {
        matrix.Cell(1, col++).Value = $"Soal {idx} ({(q.QuestionType=="MultipleAnswer" ? "MA huruf, pisah koma" : "MC 1 huruf")})";
    }
}
matrix.Range(1, 1, 1, col - 1).Style.Font.Bold = true;
// data rows: 1 baris per pekerja terpilih (NIP + Nama pre-isi, sel jawaban kosong)
int r = 2;
foreach (var (nip, name) in workerNipNames) { matrix.Cell(r,1).Value = nip; matrix.Cell(r,2).Value = name; r++; }

var legend = wb.Worksheets.Add("Legenda");           // Sheet-2
// per soal: nomor | teks | tipe | ScoreValue | A=teksA, B=teksB, ...
return ExcelExportHelper.ToFileResult(wb, "inject_template.xlsx", this);
```
**Catatan:** Pre-isi baris NIP+Nama dari pekerja terpilih (Step-2) → HC tinggal mengisi sel jawaban; ini juga menegakkan D-02 by-construction (template hanya berisi NIP yang valid di picker). Controller resolve NIP+Nama dari `vm.UserIds` (pola `userIdToNip` `:57-59`, tambah FullName ke projection). `ApplicationUser.NIP` = `string?` nullable `[VERIFIED: Models/ApplicationUser.cs:18]` → skip user tanpa NIP.

### Pattern 2: ClosedXML matrix parsing (huruf→opsi by stable order)
**What:** Baca worksheet matrix, untuk tiap baris NIP map tiap kolom soal → `InjectAnswerSpec`. Huruf opsi → `TempId` opsi via urutan opsi authored (A=opsi[0], B=opsi[1], ...).
**When to use:** POST `UploadInjectExcel`.
**Example (pola VERIFIED dari TrainingAdminController.cs:862-873 + :1307-1323):**
```csharp
// Source: [VERIFIED: Controllers/TrainingAdminController.cs:862-873]
using var stream = excel.OpenReadStream();
using var wb = new XLWorkbook(stream);
var ws = wb.Worksheet("Jawaban");                    // atau Worksheets.First()
int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
var orderedQ = questions.OrderBy(q => q.Order).ThenBy(q => q.TempId).ToList();   // STABLE — sama generate
for (int row = 2; row <= lastRow; row++) {
    var nip = ws.Cell(row, 1).GetString().Trim();
    if (string.IsNullOrEmpty(nip)) continue;          // baris kosong total → skip
    if (!allowedNips.Contains(nip)) { errors.Add(new InjectRowError{ Nip=nip, Message=$"Baris {row}: NIP {nip} tidak ada di pekerja terpilih." }); continue; }   // D-02
    int col = 3;
    var answers = new List<InjectAnswerVM>();
    foreach (var q in orderedQ) {
        if (q.QuestionType == "Essay") {
            var scoreStr = ws.Cell(row, col++).GetString().Trim();
            var text     = ws.Cell(row, col++).GetString().Trim();
            if (string.IsNullOrEmpty(scoreStr)) continue;   // D-06 sel kosong = OMIT
            if (!int.TryParse(scoreStr, out var sc)) { errors.Add(...); continue; }
            // range 0..ScoreValue divalidasi ulang oleh PreflightValidate; bisa juga di sini utk error per-sel
            answers.Add(new InjectAnswerVM{ QuestionTempId=q.TempId, EssayScore=sc, TextAnswer=string.IsNullOrEmpty(text)?null:text });
        } else {
            var cell = ws.Cell(row, col++).GetString().Trim();
            if (string.IsNullOrEmpty(cell)) continue;        // D-06 sel kosong = OMIT (BUKAN spec kosong)
            var letters = cell.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var selected = new List<int>();
            var orderedOpts = q.Options;                      // urutan authored = urutan huruf (A,B,C,..)
            foreach (var L in letters) {
                int oi = LetterToIndex(L);                    // "A"→0 ... "Z"→25
                if (oi < 0 || oi >= orderedOpts.Count) { errors.Add(new InjectRowError{ Nip=nip, Message=$"Baris {row} Soal {idx}: opsi '{L}' tidak valid." }); continue; }
                selected.Add(orderedOpts[oi].TempId);
            }
            answers.Add(new InjectAnswerVM{ QuestionTempId=q.TempId, SelectedOptionTempIds=selected });
        }
    }
    workers.Add(new InjectWorkerAnswersVM{ UserId=nipToUserId[nip], Mode="manual", Answers=answers });
}
```
**KRITIS — stable ordering:** Urutan kolom soal di-generate `OrderBy(Order).ThenBy(TempId)` dan di-parse dengan urutan **identik**. `InjectQuestionSpec.Options` di-serialize/diserialize dalam urutan authored (huruf A=`Options[0]`). Parser **TIDAK** boleh `OrderBy(TempId)` opsi (auto-gen `MakeAnswer` melakukannya untuk pemilihan deterministik, tapi pemetaan huruf↔opsi harus pakai **urutan authored apa adanya** agar A=opsi pertama yang HC tulis). Verifikasi: template-gen dan parser memakai **SATU sumber `questions`** (dari `#QuestionsJson` yang sama) → urutan konsisten dalam satu room. `[ASSUMED]` huruf-ke-opsi pakai posisi authored (lihat Assumptions A1).

### Pattern 3: Excel path memetakan ke jalur form yang sama (preview==commit)
**What:** Parser menghasilkan `List<InjectWorkerAnswersVM>` (Mode="manual" selalu — D-07), serialize ke `#AnswersJson`, lalu commit lewat `MapToRequest`→`InjectBatchAsync` yang sama. Preview pakai engine `AssessmentScoreAggregator.Compute` yang sama.
**When to use:** Seluruh flow Excel.
**Why:** Nol cabang baru di service/grading. `MapToRequest` (`:163`) sudah mengonsumsi `workerAnswers` (manual = copy spec apa adanya, `ResolveWorkerAnswers:247`). `InjectBatchAsync` `PreflightValidateAsync:382-408` sudah memvalidasi MC==1 / MA≥1 / essay 0..ScoreValue / opsi valid → Excel mewarisi gratis. `[VERIFIED]`

### Anti-Patterns to Avoid
- **Hitung skor di parser/JS:** JANGAN. Excel hanya translasi sel→spec; `InjectBatchAsync` + `Compute` grade. (Carry 393/395 byte-identik.)
- **Kirim MC/MA kosong saat sel kosong:** JANGAN — D-06 OMIT spec. Kirim `SelectedOptionTempIds=[]` untuk MC → `PreflightValidate:405` "wajib tepat 1 jawaban" → reject-all SELURUH batch (jebakan sama 395 D-05). Sel kosong = `continue`, tidak push answer.
- **Auto-add NIP yang tak di picker:** JANGAN — D-02 tolak ke daftar error.
- **OrderBy(TempId) untuk pemetaan huruf↔opsi:** JANGAN — pakai urutan authored (A=opsi pertama). TempId order hanya untuk pemilihan deterministik internal auto-gen.
- **Hapus `using ClosedXML.Excel;` di TrainingAdminController:** JANGAN — masih dipakai `:1159/:1211/:1298` (template+import training). Hanya `:862` (BulkBackfill parse) yang hilang.
- **Hapus `ManualDuplicatePredicate`:** JANGAN (D-11) — dipakai AddManual/ImportTraining `:1342`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Grade Excel answers | Custom scoring di parser | `InjectBatchAsync` (393) | Byte-identik online; cert/ET/audit gratis. |
| Preview skor pra-commit | Hitung % di JS/parser | `PreviewInjectScore` + `AssessmentScoreAggregator.Compute` (395) | preview==commit (D-08); EF-free, sudah teruji. |
| Validasi opsi/MC/MA/essay-range | Validasi baru di parser | `PreflightValidateAsync:382-408` (393) | Sudah validasi MC==1/MA≥1/essay 0..ScoreValue/opsi valid + reject-all (D-09). |
| Dedup/anti-double-cert | Cek baru | `FindDuplicateNipsAsync` (393) | D-01/D-02 cert-aware, otomatis via `InjectBatchAsync`. |
| Atomic tx rollback | Tx baru | `InjectBatchAsync` `BeginTransactionAsync`/`RollbackAsync:88/330` | Sudah atomic per-batch. |
| `.xlsx` download FileResult | `new MemoryStream` + `File(...)` inline | `ExcelExportHelper.ToFileResult:28` | Adjust columns + mime + filename terstandar. |
| UserId→NIP map | Query baru | `userIdToNip` pola `InjectAssessmentController.cs:57-59` | Sudah ada di POST commit. |

**Key insight:** 396 adalah **lapisan translasi tipis** (sel Excel → `InjectAnswerSpec`) + **penghapusan**. Semua "kerja berat" (grade, preview, validasi, dedup, cert, audit, atomic) sudah dibangun & teruji di 393/395. Risiko utama bukan logic baru melainkan **pemetaan kolom/huruf yang konsisten** dan **hard-remove yang bersih tanpa link mati / break compile**.

---

## Runtime State Inventory (rename/refactor/retire phase)

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| **Stored data** | Sesi historis hasil BulkBackfill (`AssessmentSession` dgn `AccessToken="BACKFILL"`, `AssessmentType="Manual"`) **tetap ada di DB** — hard-remove hanya menghapus *tool*, bukan data yang sudah ter-insert. | **None** — D-10 retire tool, JANGAN hapus data historis. Sesi lama tetap tampil di /CMP/Records (gratis via `GetUnifiedRecords` tak-filter). Verified by: spec §2.1/§2.2. |
| **Live service config** | None (tidak ada n8n/Datadog/external config merujuk route `BulkBackfill`). | **None — verified by** grep `BulkBackfill` → hanya file repo (kode/view/planning), tak ada config eksternal. |
| **OS-registered state** | None. | **None — verified by** tidak ada scheduled task / cron memanggil endpoint. |
| **Secrets/env vars** | None. | **None.** |
| **Build artifacts** | View `BulkBackfill.cshtml` di-embed saat build (Razor compile-time, `AddControllersWithViews` tanpa RuntimeCompilation — lesson 392). Setelah DELETE file + rebuild, binary tak lagi punya view. | **Reinstall/rebuild:** `dotnet build` setelah hapus view; verifikasi e2e dari **main working tree** (binary stale/sibling diam-diam membatalkan verifikasi runtime — lesson STATE.md `392-02`). |

**Referensi BulkBackfill yang HARUS dibersihkan (non-planning, non-test):**
- `Controllers/TrainingAdminController.cs` — comment block `776-782` + GET `784-790` + POST comment+method `831-985` `[VERIFIED]`. **(⚠ NON-KONTIGU: `792-829` = `CleanupAttemptHistory` GET/POST yang HARUS TETAP — di antara GET & POST BulkBackfill.)**
- `Views/Admin/BulkBackfill.cshtml` — DELETE (119 baris) `[VERIFIED]`.
- `Views/Admin/Index.cshtml:306-321` — `@if (User.IsInRole("Admin")) { <div col-md-4>...BulkBackfill...</div> }`. Hapus seluruh blok `@if` (306-321) — card adalah satu-satunya isinya `[VERIFIED]`.
- `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:317-322` — `<li><hr dropdown-divider></li>` (317) + `<li><a ...BulkBackfill...></li>` (318-322). Hapus divider + li (317-322) agar tak ada divider yatim `[VERIFIED]`.

**Comment-only refs (light-touch/biarkan — D-11 discretion):** `AssessmentAdminController.cs:4105` ("BulkBackfill precedent"), `AdminBaseController.cs:263` ("Import/BulkBackfill skip"). `[VERIFIED]` — komentar dokumentasi, tak memengaruhi compile/route.

---

## Common Pitfalls

### Pitfall 1: Pemetaan kolom↔soal / huruf↔opsi tidak stabil (HIGHEST RISK)
**What goes wrong:** Template di-generate dengan urutan soal X, parser membaca dengan urutan Y → jawaban masuk ke soal salah → skor salah, tapi TIDAK error (silent corruption).
**Why it happens:** Soal authored ber-`TempId` (client-state, bukan DB Id). `injQuestions[]` order di klien vs `OrderBy(Order).ThenBy(TempId)` di server bisa beda bila tidak disengaja konsisten.
**How to avoid:** Generate DAN parse memakai **satu** `List<InjectQuestionSpec>` (dari `#QuestionsJson` yang sama) + **satu** comparator `OrderBy(q=>q.Order).ThenBy(q=>q.TempId)` (identik dgn persist `InjectAssessmentService.cs:146` & auto-gen `:550`). Huruf opsi = posisi **authored** (`q.Options[0]`=A). Kunci dengan unit test round-trip: generate→parse→assert TempId mapping identik.
**Warning signs:** Preview skor != ekspektasi manual; e2e "isi A benar → skor 0".

### Pitfall 2: Sel kosong dikirim sebagai spec kosong → reject-all batch
**What goes wrong:** MC kosong → push `InjectAnswerSpec{ SelectedOptionTempIds=[] }` → `PreflightValidate:405` "MC wajib tepat 1" → SELURUH batch ditolak.
**Why it happens:** Loop parser push answer tanpa guard sel kosong.
**How to avoid:** Sel kosong = `continue` (OMIT, D-06), JANGAN push. Identik dengan JS form `buildWorkerAnswers:1352` (`if (!auto && ans.skipped) return`).
**Warning signs:** Upload file dengan 1 sel kosong → "batch ditolak" alih-alih "skor 0 untuk soal itu".

### Pitfall 3: NIP valid di AspNetUsers tapi tak di picker → lolos (langgar D-02)
**What goes wrong:** Parser hanya cek NIP exists di DB (pola BulkBackfill lama `:894`) → NIP di luar picker ter-inject.
**Why it happens:** Salin pola BulkBackfill yang `allowedNips` = semua user.
**How to avoid:** `allowedNips` = NIP hasil resolve dari `vm.UserIds` (picker Step-2) SAJA. NIP di Excel di luar set → error per-baris (D-02/D-09). `PreflightValidate` cek "exists di AspNetUsers"; cek "in picker" = tanggung jawab **parser/controller 396** (service tidak tahu picker).
**Warning signs:** e2e: Excel berisi NIP pekerja tak-terpilih → harus masuk daftar error, bukan ter-commit.

### Pitfall 4: Verifikasi view runtime pada app tanpa Razor RuntimeCompilation
**What goes wrong:** Edit `.cshtml` (toggle/upload/preview) tidak tercermin di app yang berjalan → e2e hijau/merah palsu.
**Why it happens:** `AddControllersWithViews()` tanpa `AddRazorRuntimeCompilation` → view embedded saat build; binary stale/sibling worktree (`PortalHC_KPB-ITHandoff`) diam-diam membatalkan verifikasi. `[VERIFIED: STATE.md 392-02 lesson]`
**How to avoid:** `dotnet build HcPortal.csproj` di **main tree** → stop app lain → run `HcPortal.exe`/`dotnet run` main-tree :5277 AD-off SEBELUM Playwright.
**Warning signs:** Toggle Form/Excel tak muncul walau kode ada.

### Pitfall 5: Hard-remove memutus compile / membuat link mati
**What goes wrong:** (a) Hapus method tapi sisa `nameof(BulkBackfill)` di kode lain → compile error. (b) Hapus route tapi sisa `Url.Action("BulkBackfill")` di view → link 404 mati. (c) Hapus blok kontigu termasuk `CleanupAttemptHistory` (`792-829`).
**How to avoid:** Hapus 2 blok NON-KONTIGU `776-790` + `831-985` (sisakan `792-829`). Hapus 2 UI link + view. `nameof(BulkBackfill)` hanya dipakai DI DALAM method yang dihapus (`:849-983`) → hilang bersamaan. `DuplicateGuardTests` TIDAK panggil action (pakai HashSet replica + `ManualDuplicatePredicate`) → tak break. `[VERIFIED: DuplicateGuardTests.cs:118-163]`
**Warning signs:** `dotnet build` error CS0103 / `Url.Action` resolve null.

### Pitfall 6: Tipe sel ScoreValue/score di-parse salah (numerik vs string)
**What goes wrong:** Excel cell numerik → `GetString()` bisa "85.0" / kosong tergantung format.
**How to avoid:** Pola BulkBackfill `:870-872`: coba `cell.TryGetValue<double>(out var d)` lalu fallback `int.TryParse(cell.GetString().Trim())`. Untuk huruf opsi pakai `GetString().Trim()`.

---

## Code Examples

### Round-trip mapping helper (huruf↔index)
```csharp
// Source: [pattern — letter to zero-based index]
private static int LetterToIndex(string letter) {
    var s = letter.Trim().ToUpperInvariant();
    if (s.Length != 1 || s[0] < 'A' || s[0] > 'Z') return -1;
    return s[0] - 'A';   // A→0, B→1, ...
}
private static string IndexToLetter(int i) => ((char)('A' + i)).ToString();   // 0→A (untuk legend Sheet-2)
```

### Endpoint upload (preview) — bentuk yang direkomendasikan
```csharp
// Source: [pattern reuse — InjectAssessmentController existing endpoints :103-156]
[HttpPost]
[Authorize(Roles = "Admin, HC")]
[ValidateAntiForgeryToken]
[RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]   // pola BulkBackfill :835
public async Task<IActionResult> UploadInjectExcel(IFormFile? excel, string questionsJson, string userIdsCsv) {
    // 1. parse questions (ParseQuestionVms pola) + resolve UserIds → {nip: {userId, name}}
    // 2. InjectExcelHelper.ParseMatrix(stream, questions, allowedNips) → (workers, errors)
    // 3. errors≥1 → return Json(new { ok=false, errors })   (D-09 atomic, NO write)
    // 4. 0 errors → loop workers: AssessmentScoreAggregator.Compute (engine PreviewInjectScore) → preview rows
    //    return Json(new { ok=true, answersJson = JsonSerialize(workers), previews = [...] })   (D-08, NO cert#)
}
```
Klien menaruh `answersJson` ke `#AnswersJson` lalu submit form yang sama → `MapToRequest`→`InjectBatchAsync` (commit, byte-identik). Toggle D-01: saat mode Excel, klien sembunyikan per-worker form, tampilkan upload + tabel preview; `#QuestionsJson` tetap di-serialize seperti biasa.

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| BulkBackfill: skor-agregat saja (no soal/jawaban/cert), Admin-only, commit-langsung | Inject: full-fidelity (per-soal+ET+cert), Admin+HC, preview-before-commit | Phase 393-396 (v32.2) | Excel inject jauh lebih kaya; BulkBackfill retire aman (D-12). |
| `string.GetHashCode()` untuk seed | `SHA256` (`ComputeAutoGenSeed:510`) | Phase 395 | Tak relevan 396 (Excel = manual, no seed). |

**Deprecated/outdated setelah 396:**
- Route `/Admin/BulkBackfill` + `/Admin/BulkBackfillAssessment` → 404 (D-10).
- View `BulkBackfill.cshtml` → dihapus.
- 2 UI link (Index card + dropdown-item) → dihapus.

---

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Pemetaan huruf↔opsi memakai **urutan opsi authored apa adanya** (A=`Options[0]`, B=`Options[1]`), BUKAN `OrderBy(TempId)`. Legend Sheet-2 melabeli huruf dengan urutan yang sama. | Pattern 2/3 | Bila salah-order, jawaban masuk opsi salah → skor salah silent. **Mitigasi:** unit test round-trip generate→parse + e2e "A benar→skor penuh". Planner kunci comparator opsi eksplisit. |
| A2 | Sheet matrix dinamai (mis. "Jawaban") dan parser membaca by nama; bila HC rename sheet, fallback `Worksheets.First()`. | Pattern 1/2 | HC rename sheet → parse gagal. **Mitigasi:** parser fallback `.First()` + validasi header row. |
| A3 | Header kolom/format Nama-informational = Claude's discretion (CONTEXT). Bentuk header "Soal {n} (MC ...)" bebas selama parser-gen konsisten. | Pattern 1 | Rendah — internal-konsisten. |
| A4 | Limit upload 10MB (pola BulkBackfill `:835`) memadai untuk matrix (puluhan NIP × puluhan soal). | Pattern Endpoint | Rendah. |

---

## Open Questions

1. **Preview tabel: per-NIP loop vs endpoint batch baru?**
   - What we know: `PreviewInjectScore` (existing) menerima 1 worker. `AssessmentScoreAggregator.Compute` EF-free, bisa di-loop server-side di endpoint upload.
   - What's unclear: Apakah tampilkan preview lewat 1 panggilan upload (Json `previews[]`) atau N panggilan `PreviewInjectScore`.
   - Recommendation: 1 panggilan upload yang me-loop `Compute` internal (1 round-trip, tabel D-08 sekaligus). Tetap engine sama → preview==commit.

2. **Apakah serialize Excel-parsed answers via `#AnswersJson` (klien) atau commit langsung dari hasil upload (server-cache)?**
   - What we know: `#AnswersJson` + `MapToRequest` jalur form sudah teruji.
   - What's unclear: Excel parse server-side; menaruh kembali ke `#AnswersJson` klien lalu submit ulang = round-trip ganda tapi reuse penuh.
   - Recommendation: kembalikan `answersJson` dari upload → klien isi `#AnswersJson` → submit (nol cabang commit baru). Alternatif TempData/session = state server (hindari).

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| ClosedXML | Template gen + parse | ✓ | 0.105.0 `[VERIFIED: HcPortal.csproj]` | — |
| .NET SDK / `dotnet build`+`run` | Verifikasi lokal (CLAUDE.md) | ✓ (project aktif) | — | — |
| SQL Server (SQLEXPRESS lokal) | Integration tests (preview==commit fixture) + e2e commit | ✓ (dipakai 393/395 tests) | — | Unit tests (round-trip parser) jalan tanpa DB. |
| Playwright (`tests/`) | e2e toggle/upload/preview/commit + route 404 | ✓ (specs 394/395 ada) | — | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None blocking.

---

## Validation Architecture

> Nyquist enabled (config tidak set `nyquist_validation=false`).

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests`) + Playwright (`tests/e2e`, TypeScript) `[VERIFIED]` |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/playwright.config.ts` |
| Quick run command | `dotnet test --filter Category!=Integration` (fast suite — saat ini 381/381 baseline pre-396) |
| Full suite command | `dotnet test` (termasuk Integration real-SQL) + `cd tests && npx playwright test e2e/inject-assessment-396.spec.ts --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| INJ-10 | Parser map huruf→opsi benar (A=opsi[0]) | unit | `dotnet test --filter "FullyQualifiedName~InjectExcelHelperTests" -x` | ❌ Wave 0 |
| INJ-10 | Round-trip: generate template → parse → struktur/TempId identik | unit | idem | ❌ Wave 0 |
| INJ-10 | Sel kosong = skip → OMIT spec (bukan reject) | unit | idem | ❌ Wave 0 |
| INJ-10 | Sel MA `A,C` → 2 TempId; sel MC 1 huruf | unit | idem | ❌ Wave 0 |
| INJ-10 | Essay: skor parse + teks opsional (kosong→null) | unit | idem | ❌ Wave 0 |
| INJ-10 | NIP tak di picker → error per-baris (D-02) | unit | idem | ❌ Wave 0 |
| INJ-10 | Huruf opsi invalid (`E` di soal 4-opsi) → error per-sel (D-09) | unit | idem | ❌ Wave 0 |
| INJ-10 | Essay skor > ScoreValue → reject (delegasi `PreflightValidate`) | integration | `dotnet test --filter "Category=Integration&FullyQualifiedName~InjectExcel"` | ❌ Wave 0 (atau unit jika parser pre-cek) |
| INJ-10 | Excel path → InjectWorkerSpec IDENTIK form path → skor identik (preview==commit) | integration | idem | ❌ Wave 0 (extend `InjectPreviewEqualsCommitTests` pattern) |
| INJ-10 | Atomic: ≥1 error → 0 sesi ter-commit | integration | idem | ❌ Wave 0 |
| INJ-10 | e2e: download template → fill → upload → preview tabel → commit → /CMP/Results skor benar | e2e | `npx playwright test e2e/inject-assessment-396.spec.ts --workers=1` | ❌ Wave 0 |
| INJ-10 | e2e: baris invalid → daftar error LENGKAP + rollback (0 write) | e2e | idem | ❌ Wave 0 |
| INJ-11 | Route `/Admin/BulkBackfill` → 404 (post-remove) | e2e | idem | ❌ Wave 0 |
| INJ-11 | Kartu Index + dropdown-item hilang | e2e | idem | ❌ Wave 0 |
| INJ-11 | `dotnet build` 0 error pasca hard-remove (DuplicateGuardTests compile) | smoke | `dotnet build HcPortal.csproj` | ✓ existing |

### Sampling Rate
- **Per task commit:** `dotnet test --filter Category!=Integration` (fast unit; parser round-trip + mapping).
- **Per wave merge:** `dotnet build` + fast suite + Integration inject (`Category=Integration`) bila DB tersedia.
- **Phase gate:** Full xUnit + Playwright `inject-assessment-396.spec.ts` green dari **main tree** (Pitfall 4) sebelum `/gsd-verify-work`. CLAUDE.md Seed Workflow: snapshot DB lokal (e2e menulis commit+cert+audit) → RESTORE di afterAll.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/InjectExcelHelperTests.cs` — covers INJ-10 (round-trip, huruf↔opsi, blank=skip, MA multi, essay, NIP-not-in-picker, invalid-letter). **Pure unit, no DB** → fast suite.
- [ ] Extend Integration: tambah fact ke pola `InjectPreviewEqualsCommitTests.cs` (Excel-parsed workers → score == form workers) ATAU file baru `InjectExcelCommitTests.cs` `[Trait Category=Integration]` pakai `InjectAssessmentFixture` disposable.
- [ ] `tests/e2e/inject-assessment-396.spec.ts` — download→fill→upload→preview→commit + invalid→error-list→rollback + BulkBackfill 404 + cards gone. (Buat `.xlsx` fixture in-test via library Node atau pre-bake; alternatif: drive template yang di-generate app lalu mutate cell via SheetJS.)
- [ ] Framework: tidak ada install (xUnit + Playwright sudah ada).

---

## Security Domain

> `security_enforcement` enabled (default).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Server-authoritative: parse+validasi+grade di backend; klien hanya UI. |
| V2 Authentication | no | RBAC via `[Authorize]` (di bawah). |
| V4 Access Control | **yes** | `[Authorize(Roles="Admin, HC")]` pada endpoint baru (mirror existing `InjectAssessment` `:41`). BulkBackfill Admin-only → diganti Admin+HC (spec D-9). |
| V5 Input Validation | **yes** | File: validasi ekstensi `.xlsx/.xls` + `[RequestFormLimits 10MB]` (pola `:835`); parse `try/catch` → pesan, bukan 500 (pola `:876`). Sel: NIP-in-picker (D-02), huruf valid, range. Malformed → daftar error (D-09), JANGAN throw. |
| V6 Cryptography | no | — (seed 395 non-secret; Excel tak pakai). |
| V12 File Upload | **yes** | `IFormFile` ekstensi whitelist + size limit + parse dalam `try/catch`; tidak menyimpan file ke disk (parse stream langsung, pola `:861`). |

### Known Threat Patterns
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| NIP di Excel di luar picker (privilege via data) | Tampering / Elevation | D-02 reject — `allowedNips` = picker set saja `[VERIFIED gap: parser 396 enforce]`. |
| Malformed/huge `.xlsx` (DoS) | DoS | Ekstensi whitelist + 10MB limit + try/catch parse (pola BulkBackfill). |
| Antiforgery bypass pada upload/preview | Tampering | `[ValidateAntiForgeryToken]` (pola `PreviewInjectScore:105`). |
| Double-cert via Excel | Tampering | Warisi `FindDuplicateNipsAsync` dedup cert-aware (393) via `InjectBatchAsync` — gratis. |
| Audit gap | Repudiation | `InjectBatchAsync` audit `ManualInject` per sesi (393) — gratis; tak butuh audit Excel terpisah. |

---

## Sources

### Primary (HIGH confidence — verified file:line, working tree HEAD 929a6c2e)
- `Controllers/TrainingAdminController.cs:776-985` — BulkBackfill GET/POST + parse pattern `:862-873` + tx `:905/980`; ClosedXML gen `:1159/:1211`, parse `:1298`, `using :8`.
- `Helpers/ExcelExportHelper.cs:14,28` — `CreateSheet`, `ToFileResult`, `AddDetailPerSoalSheet` (multi-sheet pattern).
- `Controllers/InjectAssessmentController.cs:57-59,106-156,163-220,283-307,327-340` — userIdToNip, `PreviewInjectScore`, `MapToRequest`, `MapToInMemory`, `ParseAnswerVms`.
- `Services/InjectAssessmentService.cs:42-334,382-408,510,540` — `InjectBatchAsync`, `PreflightValidateAsync`, `ComputeAutoGenSeed`, `BuildAutoGenAnswers`.
- `Models/InjectAssessmentDtos.cs` — `InjectAnswerSpec`/`InjectWorkerSpec`/`InjectRequest`/`InjectRowError`/`InjectPreviewRequest/Result`.
- `Helpers/AssessmentScoreAggregator.cs:26-60` — `Compute` signature + formula.
- `ViewModels/InjectAssessmentViewModel.cs` — `InjectWorkerAnswersVM`/`InjectAnswerVM`/`AnswersJson`.
- `Views/Admin/InjectAssessment.cshtml:208-286,312-314,401-533,985-996,1346-1485` — picker, hidden JSON, Step-5 seam, serialize, preview.
- `Views/Admin/BulkBackfill.cshtml` (119 lines), `Views/Admin/Index.cshtml:306-321`, `Views/Admin/Shared/_AssessmentGroupsTab.cshtml:317-322` — removal targets.
- `HcPortal.Tests/DuplicateGuardTests.cs:118-163` — `#14` tests = HashSet replica + predicate (no action call).
- `HcPortal.Tests/InjectPreviewEqualsCommitTests.cs`, `InjectViewModelMapTests.cs`, `tests/e2e/inject-assessment-395.spec.ts` — test harness patterns.
- `Models/ApplicationUser.cs:18` — `NIP string?`. `HcPortal.csproj` — ClosedXML 0.105.0. `Program.cs:57` — DI `InjectAssessmentService`.
- `git log` — Phase 395 committed (`561944f7`..`929a6c2e`).

### Secondary (MEDIUM)
- `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md` §7.1, §11 F4, §2.1, §10, §12.

### Tertiary (LOW)
- None — semua klaim verified di working tree.

---

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** — ClosedXML 0.105.0 verified; semua helper/endpoint/DTO ada & terverifikasi file:line; nol library baru.
- Architecture: **HIGH** — Phase 395 ter-commit di tree → seam (preview/answers/toggle) konkret, bukan hipotetis.
- Pitfalls: **HIGH** — derived dari pola kode verified + lesson STATE.md (392 view-runtime, 354 Playwright, 395 skip=omit reject-all).
- Hard-remove: **HIGH** — line ranges + non-kontiguitas (CleanupAttemptHistory) + test-compile safety verified.

**Open dependency note (RESOLVED):** Prompt mengkhawatirkan "395 mungkin belum dieksekusi". **Terselesaikan:** HEAD `929a6c2e` = 395-03 ter-commit. Planner BOLEH bergantung pada `PreviewInjectScore`, `MapToRequest` Answers, `#AnswersJson`, toggle Step-5. File-overlap dengan 397 (belum dieksekusi) tetap berlaku → 396 sequential sebelum 397.

**Research date:** 2026-06-18
**Valid until:** 2026-07-18 (stabil — repo internal, nol lib eksternal volatile). Re-verify line numbers bila 395/396 di-rebase.

---

## RESEARCH COMPLETE

**Phase:** 396 — Import Excel + retire BulkBackfill
**Confidence:** HIGH

### Key Findings
- **Phase 395 SUDAH ter-commit** (HEAD `929a6c2e`) → seam preview/answers/toggle Step-5 KONKRET; 396 reuse `PreviewInjectScore`+`MapToRequest`+`#AnswersJson` apa adanya (tidak perlu endpoint preview baru). Ketidakpastian dependency di prompt TERSELESAIKAN.
- Excel = lapisan translasi tipis: parser server-side hasilkan `List<InjectWorkerAnswersVM>` (Mode="manual", D-07) → preview (`AssessmentScoreAggregator.Compute`, preview==commit) → commit (`InjectBatchAsync`, byte-identik). Nol logic grading/validasi baru — `PreflightValidate:382-408` sudah validasi MC==1/MA≥1/essay 0..ScoreValue.
- **Risiko #1 = pemetaan stabil** kolom↔soal (`OrderBy(Order).ThenBy(TempId)`) & huruf↔opsi (urutan authored, A=Options[0]) — kunci dengan unit test round-trip generate→parse.
- **D-02 (NIP-in-picker) = tanggung jawab parser 396**, BUKAN service (service hanya cek exists di AspNetUsers); `allowedNips` = NIP dari `vm.UserIds`. D-06 sel kosong = OMIT spec (kirim kosong → reject-all batch).
- **Hard-remove = 2 blok NON-KONTIGU** di `TrainingAdminController.cs` (`776-790` + `831-985`; sisakan `792-829` CleanupAttemptHistory) + view file + 2 UI link (Index `306-321`, dropdown `317-322`). `using ClosedXML` & `ManualDuplicatePredicate` TETAP. DuplicateGuardTests TIDAK panggil action → tak break compile.

### File Created
`.planning/phases/396-import-excel-retire-bulkbackfill/396-RESEARCH.md`

### Confidence Assessment
| Area | Level | Reason |
|------|-------|--------|
| Standard Stack | HIGH | ClosedXML 0.105.0 + semua helper/DTO/endpoint verified file:line; nol install. |
| Architecture | HIGH | 395 ter-commit → seam konkret; jalur reuse jelas. |
| Pitfalls | HIGH | Pola verified + lesson STATE.md (392/354/395). |
| Hard-remove | HIGH | Line ranges + non-kontiguitas + test-safety verified. |

### Open Questions (non-blocking)
- Preview: 1 panggilan upload yang loop `Compute` (rekomendasi) vs N `PreviewInjectScore`.
- Commit: kembalikan `answersJson` dari upload → `#AnswersJson` klien → submit (rekomendasi, nol cabang commit baru).

### Ready for Planning
Research complete. Planner dapat membuat PLAN.md (rekomendasi: Wave 1 `InjectExcelHelper` + unit test → Wave 2 controller endpoints + integration → Wave 3 view toggle/upload/preview + e2e → Wave 4 hard-remove BulkBackfill + route-404 e2e). 0 migration. Verifikasi lokal `dotnet build`+`dotnet run` localhost:5277 + Playwright dari main tree (CLAUDE.md) sebelum commit; notify IT migration=FALSE.
