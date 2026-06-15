# Phase 386: AssessmentAdminController Hardening - Context

**Gathered:** 2026-06-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Tiga fix correctness/lifecycle di **`Controllers/AssessmentAdminController.cs`** (satu file = satu fase = nol konflik write paralel) untuk ujian lisensor real (~2026-06-17):

- **PXF-02 (F-DEV-01, HIGH):** admin tak bisa menyimpan soal Single/Multiple Answer tanpa opsi jawaban — `CreateQuestion`/`EditQuestion` menolak (validasi jumlah opsi ber-isi) dengan pesan jelas, sehingga tak ada soal cacat yang membekukan submit ujian.
- **PXF-04 (F-04, MED):** HC bisa menyelesaikan penilaian walau peserta mengosongkan essay — hitungan pending konsisten antar surface, tombol "Selesaikan Penilaian" muncul, tidak dead-end, essay kosong = 0 poin otomatis.
- **PXF-05 (F-17, MED):** PDF bukti per-peserta menandai soal Multiple Answer Benar/Salah akurat (all-or-nothing `SetEquals`, baca SEMUA opsi terpilih) — konsisten dengan web/Excel karena PDF = bukti/arsip resmi lisensor.

**File-disjoint dari Phase 385** (`_QuestionImage.cshtml` + `StartExam.cshtml`) → boleh paralel. **0 migration** (murni validasi controller + hitung pending + scoring PDF display; tak ada schema/write DB). OUT: semua temuan Future (F-02/03/01/06/11/13/19/20/22) + F-18 (kondisional ≥2 paket).

</domain>

<decisions>
## Implementation Decisions

Mode diskusi: user pilih **"pakai semua default, skip diskusi"** (hotfix urgent, scope locked roadmap) — ketiga area di-lock ke rekomendasi di bawah. Sumber kebenaran tetap server-side; semua fix verifikasi lokal (`dotnet build` + `dotnet test` + Playwright) sebelum commit.

### PXF-02 — Validasi opsi soal (CreateQuestion `~6440`, EditQuestion `~6647`)
- **D-01 (min opsi):** Untuk tipe `MultipleChoice` & `MultipleAnswer`, WAJIB **≥2 opsi ber-isi** (standar pilihan ganda). Validasi server-side, tolak dengan pesan jelas + `RedirectToAction("ManagePackageQuestions")` (pola validasi existing `~6442-6456`). Essay tak terkena (tetap validasi rubrik existing).
- **D-02 ("ber-isi" = ber-teks):** Definisi "opsi ber-isi" = **`!string.IsNullOrWhiteSpace(optionX)`** (ber-teks), **selaras dengan loop persist existing** (`CreateQuestion ~6486-6500` & `EditQuestion ~6708-6747` yang hanya menyimpan opsi bila text non-empty). Opsi gambar-saja (teks kosong) saat ini **sudah di-drop oleh loop persist** → bukan regresi; dukungan opsi gambar-tanpa-teks = **deferred** (lihat Deferred). Validasi text-based menutup F-DEV-01 (semua A/B/C/D kosong) tanpa memperluas scope ke save-loop.
- **D-03 (opsi benar wajib ber-isi):** Akar F-DEV-01 = `correctA=true` tapi `optionA` kosong → `correctCount` lolos (`~6441`) tapi opsi tak tersimpan → 0-opsi. **Terverifikasi TRUE untuk MC DAN MA** (loop persist text-gated, tak inspeksi correctness — MA `correctCount≥2` pun bisa 0-opsi bila opsi-benar kosong). Maka WAJIB tambah: **setiap opsi yang dicentang benar harus ber-isi (ber-teks)**. **JANGAN ubah `correctCount` existing** — MC tetap `==1`, MA tetap `≥2` (jangan longgarkan jadi `≥1`). Tambahan murni: (a) opsi-benar yang dicentang harus ber-teks, (b) total opsi ber-teks `≥2`. Gabung dengan gate existing (perketat, jangan duplikasi/longgarkan).
- **D-04 (client-side = opsional/Claude discretion):** Server-side adalah sumber kebenaran (WAJIB). Validasi client-side ringan di view `ManagePackageQuestions.cshtml` (disable submit / pesan inline saat opsi/correct kosong) = nice-to-have UX, boleh ditambah planner bila murah, BUKAN keharusan. (`AssessmentAdminController.cs` tetap fase ini; bila view disentuh, catat — view berbeda dari Phase 385.)

### PXF-04 — Essay kosong tidak dead-end finalize (SubmitEssayScore `~3525`, FinalizeEssayGrading `~3566`, page count `~3500`)
- **D-05 (definisi "essay kosong"):** Essay dianggap **kosong = 0 poin otomatis** bila **tidak ada response row** ATAU **`TextAnswer` kosong/whitespace**. Essay kosong **TIDAK dihitung pending**.
- **D-06 (predikat pending tunggal — kill-drift; ⚠️ KOREKSI verifier — 4 titik, bukan 3):** Definisikan **satu predikat byte-identik** "essay pending" = **`!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null`** (peserta benar-benar menjawab tapi HC belum menilai; whitespace-only = kosong, BUKAN pending). Pakai predikat yang SAMA di **KEEMPAT titik** (verifier konfirmasi `EssayScore==null` muncul di 4 surface live; hanya menyentuh 2-3 → drift BARU arah berlawanan):
  1. **Page count** `EssayGrading` (`~3500` `essayPendingCount = items.Count(EssayScore==null)`) → hanya hitung essay ber-jawaban belum dinilai → tombol "Selesaikan" muncul saat sisa pending = essay kosong saja. (Tombol di view `EssayGrading.cshtml:98` data-driven dari `Model.EssayPendingCount` controller — verifier konfirmasi TAK perlu edit view utk visibility.)
  2. **FinalizeEssayGrading** essay-gate (`~3620` `essayResponses.Any(r => r.EssayScore == null)`) → jangan blokir krn essay ber-row TextAnswer kosong + belum dinilai; treat 0. (No-row lolos by-nature, tak masuk `essayResponses`.)
  3. **SubmitEssayScore pendingCount** (`~3547-3551`, basis `allGraded = pendingCount==0`) → drive JS reveal tombol (`essay-grading.js:63`). **WAJIB pakai predikat sama** — kalau tidak, sesudah HC nilai essay terakhir sementara ada row kosong, `pendingCount>0` → tombol tak pernah muncul (dead-end jalur dinamis pasca-save, walau page-load benar). **(miss verifier PXF04-F2)**
  4. **Monitoring** count `AssessmentMonitoringDetail` (`~3308-3314`, row-based `EssayScore==null` TANPA cek TextAnswer) → **WAJIB tambah filter `!IsNullOrWhiteSpace(TextAnswer)` juga**. Klaim lama "Monitoring =0 siap, basis sama" hanya benar utk kasus NO-ROW; utk kasus row-ada-TextAnswer-kosong (worker ketik lalu hapus → autosave bikin row kosong) Monitoring hitung pending tapi page/finalize tidak → **drift baru**. **(miss verifier PXF04-F1 / C-01)**
- **D-06a (caveat domain-iterasi):** Page meng-enumerate **questions** (semua essayQs), Monitoring meng-enumerate **response rows**. Predikat sama belum tentu COUNT sama bila domain iterasi beda. Test D-12 WAJIB assert count identik 4 fixture (lihat D-12).
- **D-07 (auto-0 mekanisme):** Tidak buat row saat GET (hindari side-effect on read). Essay kosong → skor efektif 0 via **`AssessmentScoreAggregator.Compute`** existing (`FinalizeEssayGrading ~3652`; response/EssayScore null = kontribusi 0). Tak perlu materialisasi row.
- **D-08 (SubmitEssayScore defensive upsert + ⚠️ status-guard WAJIB):** Hilangkan dead-end `"Jawaban tidak ditemukan"` (`~3530-3531`): bila HC menilai essay yang belum punya row, **create row lalu set EssayScore** (upsert) alih-alih error. (No migration — insert row entitas existing; `PackageOptionId`/`TextAnswer` nullable, `SubmittedAt` default → upsert aman, verifier konfirmasi.) **⚠️ WAJIB sekaligus tambah status-guard:** `SubmitEssayScore` (`~3525`) saat ini **tanpa cek status apa pun** → utk sesi `Completed`-tapi-gagal (`NomorSertifikat==null` → `IsFinalized==false` → view biarkan Simpan Skor aktif), D-08 upsert JUSTRU **memperlebar lubang F-03** (HC bikin+score row pasca-Completed → `Score`/`IsPassed` desync, cacat PDF resmi). Tambah guard: **tolak bila `Status != PendingGrading`** (atau minimal `Status==Completed/Cancelled`). Ini "trivial saat menyentuh SubmitEssayScore" yang sudah di-restui Deferred-note → bukan scope-creep; tanpa ini D-08 = regresi data-safety. **(miss verifier C-02)**

### PXF-05 — PDF per-peserta MA correctness (private `GeneratePerPesertaPdf` `~4955-5112`; loop per-soal `~5067-5105`)
- **D-09 (helper terpusat semua tipe — kill-drift):** Lokasi tepat = method **`private byte[] GeneratePerPesertaPdf` (`~4955-5112`)**, dipanggil dari `BulkExportPdf` (`~4940`). Ganti logika single-option (`resp = sessionResponses.FirstOrDefault(...)` `~5070` → `resp.PackageOptionId` → `opt.IsCorrect`, **blok mislabel `~5074-5080`**) dengan **`AssessmentScoreAggregator.IsQuestionCorrect(q, sessionResponses.Where(r => r.PackageQuestionId == q.Id))`** untuk **SEMUA tipe** (MC/MA/Essay), bukan hanya essay. Helper sudah dipakai utk essay di `~5085` (jangan double) + handle MA `SetEquals` non-empty guard (verifier konfirmasi MC default-branch byte-identik dgn PDF MC existing → no regresi). Preseden: Excel per-session path `~4857-4863` sudah `selectedIds.SetEquals(correctIds)`.
- **D-10 (kolom "Jawaban" tampilkan semua opsi MA):** Untuk MA, kolom "Jawaban" PDF (`~5071 jawaban`, `~5102`) gabung **SEMUA `OptionText` terpilih** (join semua response rows, mis. "Avtur, Solar, Bensin") bukan 1 opsi. MC tetap 1 opsi. Essay tetap `TextAnswer` (existing). Bukti resmi → jawaban lengkap & label benar konsisten.
- **D-11 (jangan sentuh scoring engine):** Hanya display-path PDF yang diperbaiki. Scoring on-submit + `Compute` (yang sudah benar MA `SetEquals`, terverifikasi 14 unit test) TIDAK diubah. PDF hanya menyamakan label display dengan scoring yang sudah benar.
- **D-12b (helper display baru `BuildAnswerCell` — testability C-03):** Extract helper murni di `Helpers/AssessmentScoreAggregator.cs` (sebelah `IsQuestionCorrect`): `BuildAnswerCell(q, responses)` → string jawaban (MA = join semua `OptionText` terpilih, MC = 1 opsi, Essay = `TextAnswer` truncate 300). Helper pure unit-testable (pola `IsQuestionCorrectTests.cs`) → membuktikan PXF-05 tanpa menembus `private byte[] GeneratePerPesertaPdf` (QuestPDF). `GeneratePerPesertaPdf` memanggil `IsQuestionCorrect` + `BuildAnswerCell`.
- **D-13 (F-DEV-02 DI-FOLD ke PXF-05 — keputusan owner 2026-06-15):** Surface mislabel MA KEDUA **`Helpers/ExcelExportHelper.cs:83` `AddDetailPerSoalSheet`** (sheet "Detail Per Soal", dipanggil controller `~4690`) **DIPERBAIKI BARENG** pakai helper sama (`IsQuestionCorrect` + `BuildAnswerCell`) — ganti `FirstOrDefault` 1-row. Scope phase kini = controller + `AssessmentScoreAggregator.cs` (helper baru) + `ExcelExportHelper.cs` (1 call-site). Tetap 0 konflik paralel (385 disjoint). Closes F-DEV-02.

### Verifikasi (D-12)
- **PXF-02:** unit test (soal MC/MA 0-opsi ditolak, opsi-benar-tanpa-teks ditolak, ≥2 opsi ber-isi diterima) + Playwright (admin coba simpan soal Single 0-opsi → ditolak pesan jelas, soal tak tersimpan).
- **PXF-04:** unit test **assert COUNT identik antara Monitoring-builder, page-builder, SubmitEssayScore, finalize-gate** pada **4 fixture WAJIB** (verifier C-04): (a) essay no-row, (b) row TextAnswer kosong/whitespace, (c) row TextAnswer terisi + ungraded, (d) graded. Jangan cuma test "no row". + Playwright (HC finalize sesi dengan ≥1 essay kosong → tombol Selesaikan muncul → round-trip sukses, essay kosong = 0).
- **PXF-05:** ⚠️ verifier C-03 — **"unit test `IsQuestionCorrect` MA" TIDAK membuktikan PXF-05** (helper sudah 14 test hijau; risiko nyata = mis-wiring ke PDF + join D-10). `GeneratePerPesertaPdf` = `private byte[]` QuestPDF → tak tertembus unit test. WAJIB salah satu: **(1) extract logika baris-PDF jadi helper pure testable** (mis. `ResolveQuestionCorrectness(q,responses)` + `BuildAnswerCell(q,responses)` → unit-test label MA + gabungan opsi), ATAU **(2) test integrasi** panggil `GeneratePerPesertaPdf` + parse output. Test harus assert: MA benar={A,C,D} ⇒ "Benar" + kolom Jawaban = SEMUA opsi terpilih; partial/superset ⇒ "Salah".
- Semua: `dotnet build` 0 error + `dotnet test` hijau + `dotnet run` (localhost:5277) sebelum commit (CLAUDE.md Develop Workflow). AD lokal `Authentication__UseActiveDirectory=false`; admin login `admin@pertamina.com`; Playwright `--workers=1`. UAT final di Dev diserahkan ke user/IT pasca re-deploy (❌ tak ada edit langsung Dev/Prod).

### Claude's Discretion
- Struktur predikat "essay pending" (extension method / helper inline / static) — selama dipakai byte-identik di **4 titik** (D-06: 3308 Monitoring, 3500 page, 3547 SubmitEssayScore, 3620 finalize).
- Bentuk pesan validasi PXF-02 (selama jelas + Bahasa Indonesia + pola `QuestionTypeLabels.Short`).
- Apakah tambah validasi client-side PXF-02 (D-04, opsional).
- Format gabung opsi terpilih MA di PDF (koma vs newline) (D-10).
- Struktur unit/Playwright test.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Register temuan + root cause (WAJIB baca)
- `.planning/notes/2026-06-15-readiness-ujian-lisensor.md` — register final adversarial-verified; detail F-DEV-01 (validasi opsi, root cause `CreateQuestion/EditQuestion` validasi `correctCount` tapi tak validasi jumlah opsi ber-teks), F-04 (essay kosong dead-end, pending-count divergen monitoring vs page), F-17 (PDF MA `FirstOrDefault` bukan `SetEquals`), + fakta scoring terverifikasi (MA all-or-nothing `SetEquals`, default pass 70%).

### Requirement
- `.planning/REQUIREMENTS.md` — PXF-02, PXF-04, PXF-05 (acceptance) + Future/Out scope.

### File yang disentuh fase ini (controller inti + 4 titik predikat)
> ⚠️ "Single file" valid utk **paralelisasi vs Phase 385** (385 = `_QuestionImage.cshtml` + `StartExam.cshtml`, disjoint). Tapi PXF-04 menyentuh **4 titik dalam** `AssessmentAdminController.cs` (bukan 2-3), + view opsional D-04. Bukan harfiah "satu method".
- `Controllers/AssessmentAdminController.cs`:
  - `CreateQuestion ~6395-6547` (validasi `~6440-6456` `correctCount`; loop opsi persist `~6486-6500` text-gated) — PXF-02.
  - `EditQuestion(POST) ~6600-6747` (validasi `~6647-6663`; update-in-place opsi `~6708-6747` text-gated) — PXF-02.
  - **Monitoring count `AssessmentMonitoringDetail` `~3308-3314`** (row-based `EssayScore==null`, tambah filter TextAnswer) — PXF-04 titik-4 **(awalnya terlewat)**.
  - page action `EssayGrading` count `~3500` (`essayPendingCount`) + `EssayGradingPageViewModel` build (`IsFinalized` set `~3501`, `EssayPendingCount` `~3509`) — PXF-04 titik-1.
  - `SubmitEssayScore ~3525-3554` (dead-end `~3530`, **pendingCount `~3547-3551` = titik-3 predikat**, + status-guard D-08) — PXF-04.
  - `FinalizeEssayGrading ~3566-3760` (essay-gate `~3620` = titik-2, `Compute ~3652`) — PXF-04.
  - **`GeneratePerPesertaPdf ~4955-5112`** (loop per-soal `~5067-5105`, `FirstOrDefault ~5070`, blok mislabel MA/MC `~5074-5080`, essay helper sudah-benar `~5085`, kolom Jawaban `~5102`) — PXF-05.
  - preseden Excel per-session MA `SetEquals` `~4857-4863` (referensi, jangan diubah).
- View (opsional/e2e, disjoint dari 385): `Views/Admin/ManagePackageQuestions.cshtml` (D-04 client-side opsional), `Views/Admin/EssayGrading.cshtml` (button data-driven `:98` — TAK perlu edit utk visibility), `Views/Admin/AssessmentMonitoringDetail.cshtml` (referensi count).

### Helper & preseden (reuse, jangan ubah logika)
- `Helpers/AssessmentScoreAggregator.cs` — `IsQuestionCorrect(q, responses)` (MC/MA/Essay, bool?; MA non-empty guard `SetEquals` `~78-82`; essay `>0` `~84-89`; MC default `~90-96`) + `Compute(questions, responses, passPct)` (`maxScore += q.ScoreValue` per soal → essay kosong = 0, % tak inflasi). Kill-drift single source (v30.0 ECG-01).
- `Models/QuestionTypeLabels.cs` *(KOREKSI path — bukan `Helpers/`)* — `Short(...)` label tipe utk pesan validasi (dipakai `~6444-6449`).
- `Models/PackageUserResponse.cs` — entity upsert D-08 (`PackageOptionId int?`, `TextAnswer string?`, `EssayScore int?`, `SubmittedAt` default) → INSERT tanpa migration.
- `Hubs/AssessmentHub.cs` — `SaveTextAnswer ~134-181` (upsert row walau text kosong → sumber kasus "row ada TextAnswer kosong"), `SaveMultipleAnswer ~234-249` (MA multi-row persistence → dasar D-09/D-10).

### Sibling phase (konsistensi, file-disjoint)
- `.planning/phases/385-exam-taking-image-render-hotfix/385-CONTEXT.md` — PXF-01/PXF-03, file view berbeda → paralel-aman.

### Milestone archive (konteks helper)
- `.planning/milestones/v30.0-ROADMAP.md` — asal `IsQuestionCorrect` (essay `>0`, MA `SetEquals` display-path).

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AssessmentScoreAggregator.IsQuestionCorrect` — sudah handle MC/MA/Essay; tinggal di-extend pemakaiannya ke MC/MA di PDF per-peserta (PXF-05 D-09). Tak perlu logika baru.
- `AssessmentScoreAggregator.Compute` — sudah skor essay kosong = 0 (kontribusi null=0); finalize tinggal lolos-kan gate (PXF-04 D-07).
- Pola validasi `CreateQuestion`/`EditQuestion` (`TempData["Error"]` + `RedirectToAction("ManagePackageQuestions")`) — tambah validasi opsi mengikuti pola yang sama (PXF-02).
- `QuestionTypeLabels.Short` — untuk pesan validasi konsisten.

### Established Patterns
- Loop persist opsi **text-gated** (`if(!IsNullOrWhiteSpace(text))`) di Create (`~6488`) & Edit (`~6721/6731/6738`) → validasi PXF-02 harus pakai basis "ber-teks" yang sama agar konsisten (D-02).
- Pending essay basis: Monitoring row-based vs page `EssayScore==null` → divergen (F-04). Fix = predikat tunggal (D-06).
- PDF per-peserta MA pakai `FirstOrDefault` 1-row → MA mislabel (F-17). MA tersimpan sebagai BANYAK response rows per soal → wajib agregasi semua row (D-09/D-10).

### Integration Points
- MA response = multi-row per `PackageQuestionId` (`PackageOptionId` per opsi terpilih) → kunci utk D-09 (SetEquals) & D-10 (display semua).
- `FinalizeEssayGrading` essay-gate `~3620` + page count `~3500` + Monitoring count = 3 titik yang harus pakai predikat pending yang sama (D-06).

</code_context>

<specifics>
## Specific Ideas

- F-DEV-01 CONFIRMED Dev 2026-06-15: soal Single Answer tersimpan 0-opsi → "Kumpulkan Ujian" DISABLED permanen utk SEMUA peserta (timer-expiry auto-submit tetap fire, soal 0-opsi auto-0). Mitigasi operasional tetap: cek tiap soal punya opsi saat setup.
- PDF per-peserta = **bukti/arsip resmi lisensor** → label MA salah = cacat dokumen resmi. Prioritas akurasi.
- Essay kosong realistis (worker skip essay) → jangan biarkan HC dead-end finalize di hari-H.

</specifics>

<deferred>
## Deferred Ideas

- **Dukungan opsi gambar-tanpa-teks** — loop persist existing sudah drop opsi tanpa teks; validasi PXF-02 ini berbasis teks (D-02). Bila ujian butuh opsi gambar-saja, itu fix terpisah (sentuh save-loop Create+Edit) → **Future**, bukan fase hotfix ini. (Risiko rendah: ujian lisensor confirmed pakai opsi ber-teks Avtur/Bensin/Solar.)
- **F-02/F-01/F-06/F-11/F-13/F-19/F-20/F-22** — Future pasca-acara (lihat `.planning/REQUIREMENTS.md` + STATE.md Deferred).
- **F-03 (status-guard `SubmitEssayScore`) — sebagian DI-FOLD ke D-08.** Verifier C-02: D-08 upsert TANPA guard = perlebar lubang F-03 → guard minimal (`tolak bila Status != PendingGrading`) sekarang **WAJIB bagian D-08** (trivial, dilakukan saat menyentuh method). Sisa F-03 (recompute/desync Score pasca-finalize penuh, `~3525` cabang lain) tetap Future.
- **✅ F-DEV-02 (`Helpers/ExcelExportHelper.cs:83` `AddDetailPerSoalSheet`) — DI-FOLD ke PXF-05** (keputusan owner 2026-06-15). Lihat **D-13**. Bukan lagi deferred.
- **F-18** (export by-paket bukan `ShuffledQuestionIds`, ≥2 paket) — OUT/kondisional; `GeneratePerPesertaPdf` resolve soal by `AssessmentPackageId` (`~4984`) = persis F-18. PXF-05 **mengasumsikan 1 paket**; bila multi-paket aktif, PXF-05 sendiri tak cukup. Mitigasi runbook hari-H: pakai 1 paket → skip.

### Catatan untuk planner (jangan keliru — verifier)
- **`SyncPackagesToPost` (`~5689`)** = copy-path verbatim Pre→Post, dipanggil ujung `CreateQuestion`/`EditQuestion`. Terlindungi transitif oleh validasi upstream D-01/D-03 → **JANGAN tambah validasi opsi di sini** (akan tolak clone yang sah).
- **`ImportPackageQuestions` (`~6162-6230`) — LOCKED OUT** (keputusan 2026-06-15, research-recommended). Wajibkan ke-4 opsi terisi → tak bisa hasilkan 0-opsi (aman dari F-DEV-01, tak blokir ujian). Rule MA-benar import (`~6179` `≥1`) BEDA dari form (`≥2`) = drift diketahui tapi **TIDAK disentuh fase ini** (samakan = Future). JANGAN ubah importer.
- **PXF-02 validasi — LOCKED extract helper** (keputusan 2026-06-15): ekstrak `ValidateQuestionOptions(type, texts, corrects)` murni (mis. di `Helpers/`) dipakai `CreateQuestion` + `EditQuestion` (sekali test, kill-drift), bukan inline duplikat. Pure unit-testable (pola `IsQuestionCorrectTests.cs`).
- **Urutan opsi MA di kolom Jawaban PDF** = urutan Id (`PackageOption` tak punya field Order), bukan huruf A/B/C/D tampilan — kosmetik, konsisten Excel `~4860`/Results. Label tetap benar (SetEquals order-independent).

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" (area: database, score 0.6) — match keyword false-positive (`controllers/assessmentadmincontroller`); isi sebenarnya = cleanup DB test lokal, BUKAN controller hardening. **Tidak di-fold** (out of scope hotfix). Tetap di backlog todo.

</deferred>

---

*Phase: 386-assessmentadmincontroller-hardening*
*Context gathered: 2026-06-15*
