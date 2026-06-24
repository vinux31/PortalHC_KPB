# Phase 419: Export Label Section + Polish + Test/UAT Milestone - Research

**Researched:** 2026-06-24
**Domain:** ASP.NET Core (C#) export rendering (ClosedXML/QuestPDF) + cross-feature integration guard + xUnit/Playwright QA + milestone audit
**Confidence:** HIGH (semua temuan diverifikasi langsung di codebase; tidak ada ketergantungan library eksternal baru)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01 Export label Section:**
  - **Excel `AddDetailPerSoalSheet`** (`Helpers/ExcelExportHelper.cs:50`, matrix 1-baris/peserta, kolom per-soal): urutkan kolom soal per **(SectionNumber, Order)** (saat ini `OrderBy(q.Order)` saja) + **baris header merged** di atas grup kolom tiap Section `"Section {n}: {Nama}"`. Soal tanpa Section → grup **"Lainnya"** terakhir (SEC-03).
  - **PDF `GeneratePerPesertaPdf`** (`AssessmentAdminController.cs:5703`, list soal vertikal/peserta): sisip **heading `"Section {n}: {Nama}"`** sebelum blok soal tiap Section; "Lainnya" terakhir.
  - **Kompatibel-mundur:** assessment tanpa Section → semua soal grup "Lainnya" tunggal mempertahankan `q.Order` → tampilan praktis identik sekarang.
  - **Huruf opsi A–F dinamis** (Phase 418) WAJIB konsisten di export — reuse `AssessmentScoreAggregator.BuildAnswerCell`/`IsQuestionCorrect` (kill-drift).
- **D-02 Guard LinkPrePost × Section:** HARD-BLOCK link Pre↔Post (Phase 397 inject link) bila struktur Section tidak identik (jumlah Section + jumlah soal per-SectionNumber per paket berbeda), pesan jelas (sebut SectionNumber + jumlah diharapkan vs aktual). REUSE fingerprint identitas per-Section dari **SEC-04** (`SectionStructureComparer`) — jangan duplikasi predikat.
- **D-03 ET-warning re-spec (DEF-416-01 + IN-01):** lokasi `AssessmentAdminController.cs:7673-7680`. Re-spec: `DistinctEt` = distinct ET pool soal Section LINTAS paket-saudara (sibling) dalam 1 assessment; `K = min(count soal Section antar paket-saudara)`. Matching Section lintas-sibling pakai `SectionNumber` (bukan SectionId). Tetap NON-BLOCKING. Tambah test positif.
- **D-04 UAT:** keempat interaksi lintas-milestone real-browser @5277 — (1) Lifecycle Section inti, (2) Inject v32.2 × Section, (3) LinkPrePost 397 × Section, (4) Add/Remove v32.5 × Section.
- **D-05 test suite + audit milestone PASSED 20/20 REQ.** Test lama WAJIB tetap hijau (kompatibel-mundur = Section kosong).
- **D-06 folded todo:** cleanup data test lokal pasca-UAT (SEED_WORKFLOW snapshot→restore), tandai journal `cleaned`.
- **Carry-forward:** D-12 NO per-Section score breakdown di export (label organisasi saja). **migration=FALSE.**

### Claude's Discretion
- Penyembunyian header/heading "Lainnya" saat assessment tanpa Section (1 grup tunggal) — pilih paling backward-compatible secara visual.
- Detail styling Excel (merge range, warna band-header) & layout heading PDF (font/spacing).
- Urutan/struktur file test baru & nama spec Playwright.

### Deferred Ideas (OUT OF SCOPE)
- **SREP-01** breakdown skor per-Section di hasil/sertifikat (terkunci D-12).
- **SAMP-01** sampling "ambil N dari M" per-Section (v2).
- **Excel zero-config** (dropdown Data Validation + import skor per-soal) — milestone quick-win terpisah.
- Tipe soal baru, page-number per-soal — Out of Scope v32.6.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PAG-04 | Export per-soal (Excel/PDF) menampilkan label/header Section | `AddDetailPerSoalSheet` (Excel band-header) + `GeneratePerPesertaPdf` (PDF heading) reorder by (SectionNumber, Order); model `PackageQuestion.Section` nav menyediakan SectionNumber+Name; reuse `BuildAnswerCell`/`IsQuestionCorrect`. Lihat §Architecture Patterns + §Code Examples. |

Polish carry-over (di-fold ke 419, bukan REQ formal tapi locked decision):
| Item | Description | Research Support |
|------|-------------|------------------|
| D-02 | Guard LinkPrePost × Section hard-block | `SectionStructureComparer.MismatchedSections` (SEC-04) + insertion point di `InjectAssessmentService` preflight/`ResolveLinkContextAsync`. **⚠ Open Q-1:** inject package = all-Lainnya (tak ber-Section) — lihat §Open Questions. |
| D-03 | Re-spec predikat ET-warning lintas-sibling | `ManagePackageQuestions:7673-7680`; butuh muat paket-saudara (saat ini hanya muat 1 paket). |
| D-06 | Cleanup data test lokal | SEED_WORKFLOW snapshot→restore (e2e global.setup/teardown sudah punya pola). |
</phase_requirements>

## Summary

Phase 419 adalah fase **TERAKHIR & ship** milestone v32.6. Tiga pekerjaan: (1) **PAG-04** — label/header Section di dua jalur export (Excel "Detail Per Soal" + PDF per-peserta); (2) **polish carry-over** — guard integritas LinkPrePost × Section (D-02), re-spec predikat ET-warning yang dead-code (D-03/IN-01); (3) **QA milestone** — suite test baru + Playwright UAT 4 interaksi lintas-milestone + audit 20/20 REQ. **migration=FALSE** (tak ada perubahan skema — semua data Section sudah ada dari Phase 415).

Kabar baik dari investigasi codebase: hampir semua infrastruktur sudah ada. Model `PackageQuestion.Section` (nav ke `AssessmentPackageSection` dengan `SectionNumber`+`Name`) sudah hidup sejak 415. Helper `SectionStructureComparer` (SEC-04, `Helpers/SectionStructureComparer.cs`) menyediakan komparasi struktur per-SectionNumber reusable yang TEPAT untuk D-02 — pola pemanggilan persis ada di `CMPController.StartExam:1098-1119`. SEC-06 sync (`SyncPackagesToPost`) **sudah lengkap** meng-clone Section+opsi E/F di semua mutation site → kerja 419 untuk SEC-06 = **audit-only, bukan kode baru**. Export helper reuse `BuildAnswerCell`/`IsQuestionCorrect` yang mengembalikan **OptionText** (bukan huruf) → "huruf A–F dinamis konsisten" otomatis terpenuhi (export tampilkan teks jawaban, letter-agnostik).

Dua landmine yang HARUS diperhatikan planner: **(1)** `AddDetailPerSoalSheet` & `GeneratePerPesertaPdf` me-load `PackageQuestions` dengan `.Include(q => q.Options)` TAPI **TANPA `.Include(q => q.Section)`** (controller :5425-5428 & :5673-5676) — tanpa eager-load Section, `q.Section?.SectionNumber` SELALU null → semua jatuh ke "Lainnya" (silent bug, mirror Pitfall 416). **(2)** Inject flow (Phase 397) membuat package dari `InjectQuestionSpec` yang **TIDAK punya field SectionId/SectionNumber** → package inject SELALU all-Lainnya. D-02 hard-block "struktur tidak identik" akan memblok SEMUA link inject-Pre↔online-room-ber-Section. Ini perlu keputusan desain (lihat §Open Questions Q-1).

**Primary recommendation:** PAG-04 via grouping `OrderBy(SectionNumber).ThenBy(Order)` + band-header (Excel) / heading (PDF), WAJIB tambah `.Include(q => q.Section)` di kedua call-site load. D-02 reuse `SectionStructureComparer` di `InjectAssessmentService` preflight — TAPI angkat Open Q-1 ke discuss/planner sebelum implement (inject = all-Lainnya). D-03 muat paket-saudara di `ManagePackageQuestions` GET lalu hitung pool ET lintas-sibling group-by SectionNumber. Semua test reuse `SectionFixture` (real SQLEXPRESS disposable DB); UAT reuse pola `scoped-shuffle.spec.ts` (serial, backup/restore, createAssessmentViaWizard, SQL UPDATE SectionId).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Export label Section (Excel band-header) | Helper (`ExcelExportHelper`) | Controller (load+Include) | Render murni; controller siapkan data ter-Include |
| Export label Section (PDF heading) | Controller (`GeneratePerPesertaPdf`) | — | QuestPDF doc-builder inline di controller (existing) |
| Guard LinkPrePost × Section | Service (`InjectAssessmentService`) | Helper (`SectionStructureComparer`) | Link-resolution & preflight server-authoritative di service; komparasi via helper SEC-04 |
| ET-warning re-spec | Controller GET (`ManagePackageQuestions`) | View (render non-blocking) | ViewBag computed di GET; render di Razor |
| SEC-06 sync audit | Controller (`SyncPackagesToPost`) | — | **Sudah lengkap** — 419 audit-only |
| Test (data-layer) | xUnit + `SectionFixture` | real SQLEXPRESS | FK/unique-index butuh DB nyata (bukan InMemory) |
| UAT (Razor/JS/SignalR) | Playwright @5277 | DB backup/restore | Lesson 354 — unit tak nangkap render/wiring |
| Cleanup pasca-UAT | SEED_WORKFLOW (sqlcmd BACKUP/RESTORE) | SEED_JOURNAL | Snapshot→restore per CLAUDE.md |

## Standard Stack

Tidak ada library baru. Semua sudah terpasang & dipakai di milestone ini.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ClosedXML | (existing) | Excel `.xlsx` generate (`AddDetailPerSoalSheet`) | Sudah dipakai semua export Excel; mendukung `Range().Merge()` untuk band-header |
| QuestPDF | 2026.2.2 (per komentar :5636) | PDF per-peserta (`GeneratePerPesertaPdf`) | Sudah dipakai bulk PDF; fluent `Column().Item().Text()` untuk heading |
| xUnit | (existing) | Unit + integration test | Standar proyek; `SectionFixture` real-SQLEXPRESS pattern |
| Playwright | (existing `tests/`) | Real-browser UAT @5277 | Lesson 354 — wajib untuk Razor/JS/SignalR |

### Supporting (existing helpers — REUSE, jangan tulis ulang)
| Helper | Location | Purpose | When to Use |
|--------|----------|---------|-------------|
| `SectionStructureComparer` | `Helpers/SectionStructureComparer.cs` | Komparasi count per-SectionNumber (`KeyOf`, `MismatchedSections`, `SectionLabel`, `LainnyaKey`) | D-02 guard (SEC-04 fingerprint) |
| `AssessmentScoreAggregator.BuildAnswerCell` / `IsQuestionCorrect` | `Helpers/AssessmentScoreAggregator.cs` | Jawaban + Benar? (sumber tunggal export+web) | PAG-04 — JANGAN ubah (kill-drift) |
| `SectionFixture` | `HcPortal.Tests/SectionFixture.cs` | Disposable SQLEXPRESS DB + MigrateAsync (415) | Semua test data-layer baru 419 |
| `dbSnapshot` (`db.backup`/`db.execScript`) | `tests/e2e/helpers/dbSnapshot.ts` | BACKUP/RESTORE DB lokal | UAT e2e + cleanup D-06 |
| `createAssessmentViaWizard`/`addQuestionViaForm` | `tests/e2e/helpers/examTypes.ts` | Buat assessment + soal via wizard UI | UAT scenario build |

**Installation:** Tidak ada — `npm install`/`dotnet add package` TIDAK diperlukan (migration=FALSE, zero new deps).

## Architecture Patterns

### System Architecture Diagram

```
PAG-04 EXPORT (read-only, no DB write)
─────────────────────────────────────
Admin/HC klik Export Excel ─→ ExportAssessmentResults (GET, AssessmentAdminController:~5300)
   │                                │
   │  load sessions+responses+ET    ▼
   │  load PackageQuestions  ──→ [⚠ ADD .Include(q=>q.Section)] (saat ini hanya .Include(Options))
   │                                │
   └─→ ExcelExportHelper.AddDetailPerSoalSheet(workbook, sessions, responses, questions)
            │
            ▼  reorder: OrderBy(q.Section?.SectionNumber ?? +∞).ThenBy(q.Order)
            │  group by SectionNumber → band-header row "Section {n}: {Nama}" merged
            │  per soal: BuildAnswerCell + IsQuestionCorrect (REUSE — kill-drift)
            ▼
       .xlsx download

Admin/HC klik Export PDF ─→ BulkExportPdf (GET, :5641)
   │  load PackageQuestions ──→ [⚠ ADD .Include(q=>q.Section)]
   └─→ GeneratePerPesertaPdf(session, ..., questions, ...)  (:5703, per peserta)
            │  Page 2+: group sessionQuestions by SectionNumber
            ▼  emit heading "Section {n}: {Nama}" sebelum tiap blok soal
       .pdf (zip bundle)

D-02 GUARD (write path — inject link)
─────────────────────────────────────
Admin/HC submit inject Pre/Post + pilih target room ─→ InjectAssessmentController.Inject (POST)
   └─→ InjectAssessmentService.InjectBatchAsync
          │  PreflightValidateAsync (errors collected, reject-all path)
          │  ResolveLinkContextAsync(LinkTargetRepId) → groupId + target room
          ▼  [NEW D-02] bila link aktif: bandingkan struktur Section
          │     inject package counts  vs  target room package counts
          │     via SectionStructureComparer.MismatchedSections
          │     mismatch → InjectRowError "Section {n}: diharapkan X, aktual Y" → reject
          ▼
       commit / reject

D-03 ET-WARNING (read path — GET, non-blocking signal)
──────────────────────────────────────────────────────
ManagePackageQuestions(packageId) GET (:7644)
   │  [NEW] muat paket-saudara (sibling) di assessment yang sama
   │  pool ET = distinct ET soal SectionNumber=N LINTAS semua paket-saudara
   │  K = min(count soal SectionNumber=N antar paket-saudara)
   ▼  ViewBag.SectionEtWarnings = where DistinctEt > K  (group by SectionNumber)
   View render .alert-warning (NON-BLOCKING)
```

### Recommended File Touch Map (no new files for production code)
```
Helpers/ExcelExportHelper.cs        # AddDetailPerSoalSheet — reorder + band-header (PAG-04 Excel)
Controllers/AssessmentAdminController.cs
   :5425-5428                       # ADD .Include(q=>q.Section) — Excel export load
   :5673-5676                       # ADD .Include(q=>q.Section) — PDF export load
   :5703 GeneratePerPesertaPdf      # group + heading (PAG-04 PDF)
   :7644 ManagePackageQuestions     # muat sibling + re-spec ET-warning (D-03/IN-01)
   :7686 SectionEtWarning record    # pertahankan shape (atau extend bila perlu)
Services/InjectAssessmentService.cs # D-02 guard di preflight/InjectBatchAsync (⚠ Open Q-1)
HcPortal.Tests/                     # file test BARU (export label, ET-warning positif, LinkPrePost guard)
tests/e2e/                          # spec UAT BARU (4 interaksi D-04)
```

### Pattern 1: Section-aware ordering (kanonik milestone)
**What:** Urutan soal canonical = `OrderBy(SectionNumber)` lalu `OrderBy(Order)` dalam Section; "Lainnya" (SectionId null) terakhir.
**When to use:** Reorder kolom Excel + blok PDF.
**Example (mirror `ShuffleEngine`/`SectionPaginator` Phase 416/417):**
```csharp
// Source: pola partisi ShuffleEngine.cs (Phase 416) + SectionPaginatorTests
// "Lainnya" terakhir: null SectionNumber → int.MaxValue sebagai sort key.
var ordered = questions
    .OrderBy(q => q.Section?.SectionNumber ?? int.MaxValue)
    .ThenBy(q => q.Order)
    .ThenBy(q => q.Id)
    .ToList();
// group untuk band-header / heading:
var groups = ordered
    .GroupBy(q => q.Section?.SectionNumber)   // null = grup "Lainnya"
    .OrderBy(g => g.Key ?? int.MaxValue);
```

### Pattern 2: SEC-04 struktur-Section comparison (REUSE untuk D-02)
**What:** Bandingkan dua peta count-per-SectionNumber; `MismatchedSections` kembalikan daftar key beda.
**When to use:** D-02 LinkPrePost guard.
**Example (verbatim pola dari `CMPController.StartExam:1098-1119`):**
```csharp
// Source: Controllers/CMPController.cs:1098-1119 (StartExam re-guard SEC-04)
var injectCounts = injectQuestions
    .GroupBy(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber))   // null→LainnyaKey
    .ToDictionary(g => g.Key, g => g.Count());
var targetCounts = targetRoomQuestions
    .GroupBy(q => SectionStructureComparer.KeyOf(q.Section?.SectionNumber))
    .ToDictionary(g => g.Key, g => g.Count());
var mismatched = SectionStructureComparer.MismatchedSections(injectCounts, targetCounts);
if (mismatched.Any()) {
    foreach (var sn in mismatched) {
        int x = injectCounts.GetValueOrDefault(sn);
        int y = targetCounts.GetValueOrDefault(sn);
        errors.Add(new InjectRowError {
            Message = $"Section {SectionStructureComparer.SectionLabel(sn)}: room target punya {y} soal, batch inject punya {x} soal (struktur harus sama untuk ditautkan)."
        });
    }
}
```

### Anti-Patterns to Avoid
- **Lupa `.Include(q => q.Section)`:** export load saat ini TIDAK eager-load Section → `q.Section` null senyap → semua soal jatuh ke "Lainnya" (PAG-04 gagal tanpa error). MIRROR Pitfall 416 (`ThenInclude(q.Section)` lupa = partisi senyap "Lainnya").
- **Menulis predikat struktur baru untuk D-02:** dilarang CONTEXT — reuse `SectionStructureComparer` (kill-drift dengan import/StartExam guard).
- **Mengubah `BuildAnswerCell`/`IsQuestionCorrect` untuk export:** dilarang (kill-drift v30.0/386 — export+web+PDF share helper ini).
- **Per-Section score breakdown di export:** D-12 melarang — Section = label organisasi SAJA, bukan skor.
- **Mmatching Section lintas-sibling pakai SectionId:** SALAH (IN-01) — sibling beda Id, sama nomor → WAJIB `SectionNumber`.
- **Band-header "Lainnya" saat 0 Section:** assessment legacy (semua null) sebaiknya TANPA band-header (backward-compat visual, Claude's Discretion D-01).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Komparasi struktur Section antar-paket | Predikat GroupBy+count baru | `SectionStructureComparer.MismatchedSections` | SEC-04 single-source; null-safe `KeyOf` (Lainnya = sentinel); sudah dipakai import+StartExam |
| Jawaban + Benar? di export | Logika MC/MA/Essay inline | `BuildAnswerCell` + `IsQuestionCorrect` | Kill-drift export↔web↔PDF (D-13/v30.0); MA all-or-nothing SetEquals + Essay >0 |
| Label huruf opsi A–F | String concat huruf manual | (tak perlu) `BuildAnswerCell` kembalikan OptionText | Export tampilkan TEKS jawaban, letter-agnostik → A–F otomatis konsisten |
| Test DB FK/unique-index Section | InMemory provider | `SectionFixture` (real SQLEXPRESS) | InMemory tak tegakkan FK SetNull + unique index (PackageId, SectionNumber) |
| DB snapshot/restore UAT | sqlcmd manual ad-hoc | `tests/e2e/helpers/dbSnapshot.ts` (`backup`/`execScript`) | Pola teruji 315/416; resolve default backup dir; SEED_JOURNAL flow |
| Sync Section Pre→Post | Audit + tulis ulang clone | (sudah ada) `SyncPackagesToPost:6576` | SEC-06 lengkap — clone Section+opsi E/F via nav remap (Pitfall 8 aware) |

**Key insight:** 419 adalah fase **integrasi + polish**, bukan greenfield. Mayoritas "kerja" = wire helper existing ke 2 surface export + reuse comparator existing untuk 1 guard + re-spec 1 predikat + tulis test. Penambahan kode produksi minimal; risiko terbesar = lupa `.Include` (silent) dan salah-tafsir scope D-02 (inject = all-Lainnya).

## Runtime State Inventory

> Fase ini bukan rename/refactor murni, TAPI menyentuh data live (UAT akan menulis DB). Inventory difokuskan ke state yang tersentuh test/UAT.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | UAT D-04 menulis AssessmentSessions/Packages/Questions/Sections/Responses/Certificates lokal (HcPortalDB_Dev). Inject UAT menulis sesi IsManualEntry + cert#. LinkPrePost UAT menulis LinkedGroupId. | **D-06 cleanup**: BACKUP sebelum UAT, RESTORE sesudah (snapshot→restore), tandai SEED_JOURNAL `cleaned`. |
| Live service config | None — tidak ada n8n/Datadog/external service tersentuh. Verified: fase code+test+export only. | None |
| OS-registered state | None — tidak ada Task Scheduler/pm2/systemd. Verified. | None |
| Secrets/env vars | None baru. Test pakai `Authentication__UseActiveDirectory=false` (env existing untuk dotnet run @5277). | None |
| Build artifacts | None — migration=FALSE, no new package → tidak ada egg-info/binary baru. `HcPortal.Tests.dll` rebuild normal saat `dotnet test`. | None (normal build) |

**Catatan SEED_WORKFLOW (CLAUDE.md):** semua seed UAT = `temporary + local-only`. Snapshot DB lokal via `sqlcmd ... BACKUP DATABASE` SEBELUM insert, RESTORE SETELAH test (sukses ATAU gagal). Pending todo `2026-06-11-one-time-cleanup-data-test-lokal-setelah-367-ship.md` di-fold ke D-06 sebagai langkah pra-ship.

## Common Pitfalls

### Pitfall 1: Export load tanpa `.Include(q => q.Section)` → semua "Lainnya" (silent)
**What goes wrong:** Band-header/heading Section tak pernah muncul; semua soal masuk grup "Lainnya".
**Why it happens:** `AddDetailPerSoalSheet` + `GeneratePerPesertaPdf` load `PackageQuestions` dengan `.Include(q => q.Options)` saja (controller :5425-5428 & :5673-5676). `q.Section` lazy-nav = null tanpa eager-load (no lazy-loading proxy aktif).
**How to avoid:** Tambah `.Include(q => q.Section)` di KEDUA call-site load sebelum panggil helper/PDF.
**Warning signs:** Test export label hijau di in-memory builder (Section di-set langsung) tapi UAT real-browser tampil semua "Lainnya" → indikasi Include lupa. MIRROR persis Pitfall 3 Phase 416 (`.ThenInclude(q.Section)` lupa di 3 call-site shuffle).

### Pitfall 2: D-02 inject package selalu all-Lainnya → guard memblok semua link ber-Section
**What goes wrong:** Inject-Pre (package all-Lainnya) di-link ke online room ber-Section → `MismatchedSections` selalu non-empty → SEMUA link inject ke room ber-Section tertolak.
**Why it happens:** `InjectQuestionSpec` (`Models/InjectAssessmentDtos.cs:15-25`) TIDAK punya field SectionId/SectionNumber. `InjectBatchAsync` membuat `PackageQuestion` tanpa SectionId (:207-217) → package inject struktur = `{Lainnya: N}`.
**How to avoid:** **Angkat ke discuss/planner (Open Q-1)** sebelum implement. Opsi: (a) guard hanya fire bila KEDUA sisi punya Section (skip bila salah satu all-Lainnya — selaras semantik re-guard StartExam yang skip all-null legacy); (b) guard bandingkan hanya bila target ber-Section DAN inject ber-Section (saat ini mustahil → guard efektif no-op untuk inject sampai inject dukung Section). Rekomendasi peneliti: opsi (a) — konsisten dengan SEC-04 yang skip legacy all-null (`guardAnySections` di StartExam:1095).
**Warning signs:** Test guard yang seed inject all-Lainnya vs target ber-Section "berhasil blok" — tapi UAT scenario 3 "sukses link struktur sama" gagal karena inject TAK BISA punya struktur sama.

### Pitfall 3: D-03 ET-warning butuh sibling, GET hanya muat 1 paket
**What goes wrong:** Re-spec predikat butuh pool ET lintas paket-saudara, tapi `ManagePackageQuestions:7646-7649` hanya `FirstOrDefaultAsync(p.Id == packageId)` (1 paket).
**Why it happens:** Predikat lama hanya butuh 1 paket (per-Section dalam paket) — makanya dead-code. Re-spec butuh data sibling yang belum di-load.
**How to avoid:** Di GET, setelah resolve `pkg.AssessmentSessionId`, muat semua paket di assessment yang sama (`Where(p => p.AssessmentSessionId == pkg.AssessmentSessionId)` + `.Include(p => p.Questions).ThenInclude(q => q.Section)`), lalu hitung per SectionNumber: pool ET = distinct ET semua soal SectionNumber=N lintas paket; K = min(count soal SectionNumber=N per paket).
**Warning signs:** Predikat tetap "tak pernah fire" setelah re-spec → indikasi masih bandingkan dalam 1 paket (lupa muat sibling).

### Pitfall 4: Excel band-header menggeser baris data → off-by-one
**What goes wrong:** `AddDetailPerSoalSheet` saat ini header di row 1, data mulai row 2 (`rowIdx = 2`). Menambah band-header di ATAS kolom-header menggeser semua: band row 1, kolom-header row 2, data row 3+.
**Why it happens:** Band-header adalah baris baru di atas grup kolom Section. Freeze rows + merge range + indeks data harus disesuaikan.
**How to avoid:** Hitung ulang `rowIdx` awal (3 bila ada band, atau 2 backward-compat tanpa band); `FreezeRows(2)` bila band hadir; merge band hanya menutup rentang kolom milik Section itu (Jawaban+Benar? per soal = 2 kolom/soal). Pertimbangkan kolom No/Nama/NIP (1-3) tetap tanpa band.
**Warning signs:** Skor Total cell salah posisi; tools eksternal yang baca by header-row break (T-338-01 mitigation existing).

### Pitfall 5: UAT yang tak real-browser meleset wiring (lesson 354)
**What goes wrong:** Unit test hijau tapi Razor/JS export button atau Section render rusak di browser.
**Why it happens:** Lesson 354 (di-re-confirm 413 `monFlashRow`): build+grep+unit tak nangkap ReferenceError JS / render Razor / SignalR.
**How to avoid:** D-04 WAJIB Playwright @5277 real-browser untuk 4 interaksi. Export = download event assert + (opsional) parse .xlsx/.pdf isi.
**Warning signs:** Export label "lulus" hanya di unit helper test (in-memory) → wajib UAT konfirmasi end-to-end.

## Code Examples

### Excel band-header merged (ClosedXML — verified pola existing)
```csharp
// Source: ExcelExportHelper.cs pola existing (Range().Merge() dipakai di per-peserta sheet :5475)
// Band-header di atas grup kolom Section. col range = kolom Jawaban..Benar? milik Section itu.
var band = ws.Range(bandRow, startCol, bandRow, endCol);
band.Merge();
band.Value = sectionNumber.HasValue ? $"Section {sectionNumber}: {sectionName}" : "Lainnya";
band.Style.Font.Bold = true;
band.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
band.Style.Fill.BackgroundColor = XLColor.LightBlue;   // Claude's Discretion styling
```

### PDF Section heading (QuestPDF — verified pola existing)
```csharp
// Source: GeneratePerPesertaPdf:5812-5843 (Column.Item().Text() loop existing)
// Group sessionQuestions by SectionNumber, emit heading sebelum tiap blok.
foreach (var grp in ordered.GroupBy(q => q.Section?.SectionNumber).OrderBy(g => g.Key ?? int.MaxValue))
{
    var label = grp.Key.HasValue ? $"Section {grp.Key}: {grp.First().Section?.Name}" : "Lainnya";
    col.Item().PaddingTop(6).Text(label).Bold().FontSize(12)
       .FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);   // Claude's Discretion layout
    foreach (var q in grp.OrderBy(q => q.Order).ThenBy(q => q.Id))
    {
        // ... blok soal existing (BuildAnswerCell + IsQuestionCorrect) — TAK diubah
    }
}
```

### Test data-layer pattern (reuse SectionFixture helper)
```csharp
// Source: HcPortal.Tests/SectionMismatchGuardTests.cs:172-201 (AddPackageWithSectionsAsync)
// REUSE helper ini untuk seed paket ber-Section di test export/guard/ET-warning baru.
[Trait("Category", "Integration")]
public class ExportSectionLabelTests : IClassFixture<SectionFixture> { /* ... */ }
// dist: dict (SectionNumber?, count) → seed Section row + soal ber-SectionId.
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Export `OrderBy(q.Order)` flat | `OrderBy(SectionNumber).ThenBy(Order)` + band/heading | Phase 419 (PAG-04) | Kolom/blok dikelompokkan per Section |
| ET-warning `DistinctEt > K` dalam 1 paket | `DistinctEt` pool lintas-sibling, `K=min` antar-sibling | Phase 419 (D-03) | Predikat jadi reachable (sebelumnya dead-code) |
| ET-warning group by `SectionId` | group by `SectionNumber` (IN-01) | Phase 419 | Match lintas-sibling (beda Id, sama nomor) |
| LinkPrePost tanpa cek struktur Section | Hard-block bila struktur Section beda (D-02) | Phase 419 | Cegah link Pre↔Post divergen (perilaku BARU) |

**Deprecated/outdated:**
- Predikat ET-warning lama (`AssessmentAdminController.cs:7680` `.Where(w => w.DistinctEt > w.K)`) — dead-code, di-replace D-03. (Dokumentasi penuh di `416-deferred-items.md` DEF-416-01.)

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Tidak ada lazy-loading proxy EF aktif → `q.Section` null tanpa `.Include` | Pitfall 1 | Bila proxy aktif, Include tak wajib (tapi Include tetap aman/eksplisit). LOW risk — codebase pakai eksplisit Include di mana-mana. |
| A2 | D-02 sebaiknya skip guard bila salah satu sisi all-Lainnya (opsi a) | Pitfall 2 / Open Q-1 | Bila user mau hard-block total (inject ber-Section mustahil → blok semua), maka inject ke room ber-Section selalu gagal. Perlu konfirmasi user. |
| A3 | `BuildAnswerCell` kembalikan OptionText (letter-agnostik) → A–F dinamis otomatis konsisten di export | Don't Hand-Roll | Bila ada surface export yang render huruf eksplisit (belum ditemukan), perlu sinkronisasi terpisah. Verified: export pakai BuildAnswerCell, bukan huruf. |
| A4 | QuestPDF version 2026.2.2 (dari komentar kode :5636) masih terpasang | Standard Stack | Bila beda versi, fluent API bisa beda — tapi pola `Column().Item().Text()` stabil lintas versi. |

## Open Questions

1. **D-02 scope: inject package selalu all-Lainnya — apa perilaku guard yang benar?** (CRITICAL — angkat ke planner/discuss)
   - What we know: `InjectQuestionSpec` tak punya SectionId; inject package selalu `{Lainnya: N}`. Target room (online) bisa ber-Section. `MismatchedSections` akan selalu fire untuk inject-Pre↔room-ber-Section.
   - What's unclear: Apakah D-02 dimaksudkan untuk JUGA memblok link inject↔room-ber-Section (yang berarti link semacam itu selalu gagal), ATAU hanya untuk room↔room online yang sama-sama bisa ber-Section?
   - Recommendation: Opsi (a) — guard skip bila salah satu sisi all-Lainnya (konsisten dengan SEC-04 StartExam `guardAnySections` yang skip legacy all-null). Inject (all-Lainnya) → tak ter-block; link sah. Guard efektif aktif HANYA saat KEDUA sisi ber-Section. Planner sebaiknya konfirmasi ke user di discuss/plan.

2. **Apakah ada surface non-inject "LinkPrePost" selain inject flow?**
   - What we know: D-02/CONTEXT menyebut "Phase 397". Phase 397 = INJ-12 = inject-based link (`InjectAssessmentService`). Tidak ditemukan endpoint LinkPrePost non-inject terpisah; Pre/Post pairing biasa di-set saat CreateAssessment (`LinkedGroupId` auto-pair :874-884) atau via `SyncPackagesToPost` (SamePackage). Linking room **existing** = jalur inject (Phase 397).
   - What's unclear: Apakah user juga ingin guard di jalur CreateAssessment auto-pair (SamePackage sudah jamin identik via SEC-06 deep-clone → tak perlu)?
   - Recommendation: Fokus D-02 di jalur inject Phase 397 (`InjectAssessmentService`). SamePackage path sudah dijamin SEC-06 (deep-clone identik) — tak butuh guard. Konfirmasi cukup di planner.

3. **Format band-header Excel saat assessment 0 Section (legacy)?**
   - What we know: Claude's Discretion (D-01) — boleh sembunyikan band "Lainnya" tunggal.
   - Recommendation: Bila SEMUA soal null-Section → JANGAN render band-header (output byte-mirip sekarang = backward-compat aman). Bila ada ≥1 Section → render band untuk semua grup termasuk "Lainnya".

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (`dotnet build`/`test`/`run`) | Build + unit + UAT host | ✓ (proyek aktif) | — | — |
| SQL Server LOCAL (`localhost\SQLEXPRESS`) | `SectionFixture` real-DB test + UAT DB | ✓ (dipakai 415-418) | — | — |
| App @ `http://localhost:5277` | Playwright UAT (D-04) | ✓ (main worktree port) | — | — |
| Playwright + chromium (`tests/`) | UAT real-browser | ✓ (specs existing) | — | bila browser absent: `cd tests; npx playwright install chromium` |
| `Authentication__UseActiveDirectory=false` env | dotnet run untuk login lokal | ✓ (pola existing UAT) | — | — |
| sqlcmd (BACKUP/RESTORE) | D-06 cleanup + UAT snapshot | ✓ (`dbSnapshot.ts` pakai) | — | — |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** Playwright chromium (install bila absent).

> Catatan port: app run lokal port **5277** (main branch worktree — sesuai CLAUDE.md Develop Workflow). Cabang ITHandoff pakai 5270; tapi v32.6 di **main** → 5277. NTLM loopback → Playwright WAJIB `--workers=1` (`playwright.config fullyParallel:false`).

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit (`HcPortal.Tests`) + Playwright (`tests/e2e`) |
| Config file | `HcPortal.Tests/HcPortal.Tests.csproj`; `tests/playwright.config.ts` |
| Quick run command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~Section\|FullyQualifiedName~Export"` |
| Full suite command | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (full, real-SQLEXPRESS integration termasuk) |
| e2e run | `cd tests && npx playwright test e2e/<spec> --workers=1` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| PAG-04 | Excel band-header per Section (urut SectionNumber, Order; Lainnya terakhir) | unit | `dotnet test --filter ExportSectionLabelTests` | ❌ Wave 0 (file baru) |
| PAG-04 | PDF heading per Section antar-blok | unit | `dotnet test --filter ExportSectionLabelTests` | ❌ Wave 0 |
| PAG-04 | Backward-compat: 0 Section → output identik (1 grup "Lainnya", urut Order) | unit | `dotnet test --filter ExportSectionLabelTests.NoSection_BackwardCompat` | ❌ Wave 0 |
| PAG-04 | Reuse BuildAnswerCell/IsQuestionCorrect (A–F konsisten) | unit (regresi) | `dotnet test --filter PdfAnswerCellTests` (existing) | ✅ existing |
| D-02 | LinkPrePost hard-block bila struktur Section beda | integration | `dotnet test --filter LinkPrePostSectionGuardTests` | ❌ Wave 0 (real-DB, SectionFixture) |
| D-02 | LinkPrePost sukses bila struktur identik (atau salah satu all-Lainnya, per Open Q-1) | integration | `dotnet test --filter LinkPrePostSectionGuardTests.Match_Passes` | ❌ Wave 0 |
| D-03 | ET-warning FIRE bila DistinctEt(pool sibling) > K(min) — **test positif** | integration | `dotnet test --filter SectionEtWarningTests.CrossSiblingPool_Fires` | ❌ Wave 0 (NB: 416 hanya buktikan non-blocking S3/S3b) |
| D-03 | ET-warning group by SectionNumber (IN-01) lintas-sibling | integration | `dotnet test --filter SectionEtWarningTests.GroupBySectionNumber` | ❌ Wave 0 |
| D-03 | ET-warning tetap NON-BLOCKING (form/aksi aktif) | integration/e2e | `scoped-shuffle.spec.ts` S3 (existing) + assert positif baru | ✅ partial (S3 existing) |
| D-04.1 | UAT Lifecycle Section inti (create→assign→ujian→A–F→resume→export label) | e2e | `cd tests && npx playwright test e2e/section-lifecycle-419 --workers=1` | ❌ Wave 0 (spec baru) |
| D-04.2 | UAT Inject v32.2 × Section + opsi 5–6 | e2e | `npx playwright test e2e/inject-section-419 --workers=1` | ❌ Wave 0 |
| D-04.3 | UAT LinkPrePost 397 × Section (sama=sukses, beda=tertolak) | e2e | `npx playwright test e2e/linkprepost-section-419 --workers=1` | ❌ Wave 0 |
| D-04.4 | UAT Add/Remove v32.5 × Section + pagination (eager-assign konsisten) | e2e | `npx playwright test e2e/addremove-section-419 --workers=1` | ❌ Wave 0 |
| D-05 | Test lama tetap hijau (kompatibel-mundur) | full suite | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` | ✅ existing (415-418 suites) |

### Already-Built (415-418 — JANGAN duplikasi, audit ulang saja)
| Suite | Covers |
|-------|--------|
| `SectionCrudTests`, `SectionImportTests`, `SectionSyncPrePostTests`, `SectionMismatchGuardTests` (SEC-04 real-DB), `SectionFixRegressionTests` | Phase 415 SEC/IMP |
| `SectionScopedShuffleTests`, `ShuffleEngineTests` (golden-order) | Phase 416 SHF |
| `SectionPaginatorTests` | Phase 417 PAG-01/02/03 |
| `OptionValidationTests`, `EditShrinkGuardLogicTests`, `EditShrinkGuardIntegrationTests` | Phase 418 OPT |
| `PdfAnswerCellTests`, `AssessmentScoreAggregatorTests`, `IsQuestionCorrectTests` | export helper kill-drift (reuse di 419) |
| e2e: `scoped-shuffle.spec.ts`, `section-pagination.spec.ts`, `option-dynamic-418.spec.ts`, `inject-*.spec.ts`, `flexible-participant-412.spec.ts` | runtime 416/417/418 + v32.2/v32.5 (pola UAT 419) |

### Sampling Rate
- **Per task commit:** `dotnet test --filter "FullyQualifiedName~Section\|FullyQualifiedName~Export"` (quick, < 30s subset)
- **Per wave merge:** `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (full unit+integration green)
- **Phase gate:** Full suite green + 4 e2e UAT pass + audit milestone 20/20 sebelum `/gsd-verify-work` & ship.

### Wave 0 Gaps
- [ ] `HcPortal.Tests/ExportSectionLabelTests.cs` — PAG-04 (Excel band-header + PDF heading + backward-compat). Pertimbangkan helper-level test (panggil `AddDetailPerSoalSheet` + assert workbook cell/merge) — atau jika perlu real-DB, pakai `SectionFixture`.
- [ ] `HcPortal.Tests/LinkPrePostSectionGuardTests.cs` — D-02 (real-DB, `SectionFixture`; seed inject batch + target room; assert reject/pass). Resolve Open Q-1 dulu.
- [ ] `HcPortal.Tests/SectionEtWarningTests.cs` — D-03 **test positif** (predikat fire bermakna) + IN-01 group-by-SectionNumber lintas-sibling. (Gap eksplisit dari DEF-416-01: 416 tak punya test positif.)
- [ ] `tests/e2e/section-lifecycle-419.spec.ts` — D-04.1 (reuse pola `scoped-shuffle.spec.ts`).
- [ ] `tests/e2e/inject-section-419.spec.ts` — D-04.2 (reuse `inject-*.spec.ts` pola).
- [ ] `tests/e2e/linkprepost-section-419.spec.ts` — D-04.3 (reuse `inject-assessment-397.spec.ts` pola).
- [ ] `tests/e2e/addremove-section-419.spec.ts` — D-04.4 (reuse `flexible-participant-412.spec.ts` pola).
- [ ] Framework install: tidak perlu (xUnit + Playwright sudah ada). Bila chromium absent: `cd tests; npx playwright install chromium`.

## Security Domain

> `security_enforcement` tidak di-set di config.json → enabled (default). Fase ini read-heavy (export) + 1 write-guard (D-02) + GET (ET-warning).

### Applicable ASVS Categories
| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V1 Architecture | yes | Reuse helper existing (no new attack surface); export read-only |
| V2 Authentication | no (existing) | Export endpoints sudah `[Authorize(Roles = "Admin, HC")]` (verified :5640, :7643) — JANGAN longgarkan |
| V3 Session Management | no | Tidak menyentuh session/auth |
| V4 Access Control | yes | Export + ManagePackageQuestions + Inject = Admin/HC only (existing). D-02 guard server-authoritative di service (BUKAN client) |
| V5 Input Validation | yes | D-02 reject-all path (daftar error LENGKAP, no stop-at-first — pola existing 396/397); Section label di export = teks dari DB (escape via ClosedXML cell value / QuestPDF .Text() — bukan HTML) |
| V6 Cryptography | no | Tidak ada crypto baru |
| V12 Files/Resources | yes | Export file download (.xlsx/.zip) — DoS guard max 50 peserta sudah ada (:5660). Pertahankan |

### Known Threat Patterns for {ASP.NET Core export + inject guard}
| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Tampering LinkTargetRepId (client kirim group palsu) | Tampering | Server RE-RESOLVE via `ResolveLinkContextAsync` (T-397-06 existing) — D-02 guard pakai data server-resolved, BUKAN client |
| Injection via Section Name di export cell | Injection/XSS | ClosedXML cell `.Value` (bukan formula) + QuestPDF `.Text()` (bukan HTML) → tak eval. Nama Section dari DB (sudah ter-validasi 415) |
| IDOR export sesi orang lain | Elevation | Endpoint `[Authorize(Roles=Admin,HC)]` — admin/HC by design lihat semua (existing, tak diubah) |
| DoS export besar | DoS | Max 50 peserta/batch PDF (:5660 existing) — pertahankan saat tambah grouping |
| Section label di .xlsx formula-injection (`=`, `+`, `@` prefix) | Injection | LOW — Section Name editorial HC; ClosedXML tulis sebagai string value, bukan formula. Akui-by-design (mirror existing cell writes) |

## Sources

### Primary (HIGH confidence — codebase verified langsung)
- `Helpers/ExcelExportHelper.cs:50-112` (`AddDetailPerSoalSheet`) — struktur header row 1, data row 2+, OrderBy(q.Order)
- `Controllers/AssessmentAdminController.cs:5425-5428` & `:5673-5676` — export load TANPA `.Include(q=>q.Section)`
- `Controllers/AssessmentAdminController.cs:5703-5850` (`GeneratePerPesertaPdf`) — per-question loop :5816, OrderBy(q.Order) :5734
- `Controllers/AssessmentAdminController.cs:7644-7686` (`ManagePackageQuestions` + `SectionEtWarning` record) — predikat dead-code :7680, muat 1 paket saja
- `Helpers/SectionStructureComparer.cs` (full) — `KeyOf`/`MismatchedSections`/`SectionLabel`/`LainnyaKey` (SEC-04)
- `Controllers/CMPController.cs:1088-1124` — pola pemanggilan `SectionStructureComparer` (re-guard StartExam) + `guardAnySections` skip-legacy
- `Services/InjectAssessmentService.cs:42-652` — `InjectBatchAsync`, `PreflightValidateAsync`, `ResolveLinkContextAsync`, `PreviewPairingAsync` (Phase 397 link)
- `Models/InjectAssessmentDtos.cs:15-25` — `InjectQuestionSpec` TANPA SectionId (Open Q-1 root)
- `Models/AssessmentPackage.cs:34-105` — `AssessmentPackageSection` (SectionNumber+Name) + `PackageQuestion.Section` nav
- `Controllers/AssessmentAdminController.cs:6576-6695` (`SyncPackagesToPost`) — SEC-06 deep-clone Section+opsi E/F (sudah lengkap)
- `HcPortal.Tests/SectionFixture.cs` + `SectionMismatchGuardTests.cs` — pola test real-SQLEXPRESS + `AddPackageWithSectionsAsync`
- `tests/e2e/scoped-shuffle.spec.ts`, `inject-assessment-397.spec.ts`, `flexible-participant-412.spec.ts`, `global.setup.ts`, `helpers/wizardSelectors.ts` — pola UAT (serial, backup/restore, wizard, port 5277)
- `.planning/phases/416-scoped-shuffle-acak-per-section/deferred-items.md` — DEF-416-01 (D-03 root + saran re-spec)
- `.planning/config.json` — nyquist_validation:true, security default-enabled
- `CLAUDE.md` + Develop/Seed Workflow — port 5277, snapshot→restore

### Secondary (MEDIUM)
- `.planning/REQUIREMENTS.md` — PAG-04 traceability 20/20
- `docs/superpowers/specs/2026-06-22-...design.md:56-57,149` — D-11 (kontrol halaman per-Section) + D-12 (NO per-Section score breakdown)

### Tertiary (LOW)
- (none — semua klaim diverifikasi di codebase atau dokumen lock)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero new deps; semua helper/library sudah dipakai 415-418, diverifikasi di file.
- Architecture (PAG-04 export): HIGH — call-site, model nav, helper signature, dan pola ordering semua dibaca langsung.
- D-02 guard: HIGH untuk mekanik (SectionStructureComparer reuse), MEDIUM untuk scope (Open Q-1 inject all-Lainnya butuh keputusan user).
- D-03 ET-warning: HIGH — lokasi, predikat dead-code, dan data-gap (muat sibling) dikonfirmasi.
- Pitfalls: HIGH — Pitfall 1 (Include) & Pitfall 2 (inject all-Lainnya) keduanya diverifikasi dari kode.
- Test/UAT: HIGH — fixture + e2e pola existing dibaca; gaps eksplisit.

**Research date:** 2026-06-24
**Valid until:** ~2026-07-24 (kode stabil; tidak ada fast-moving external dep — valid sampai milestone berikut menyentuh AssessmentAdminController/InjectAssessmentService)
