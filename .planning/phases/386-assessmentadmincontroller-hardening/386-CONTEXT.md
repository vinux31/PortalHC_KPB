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
- **D-03 (opsi benar wajib ber-isi):** Akar F-DEV-01 = `correctA=true` tapi `optionA` kosong → `correctCount` lolos (`~6441`) tapi opsi tak tersimpan → 0-opsi. Maka WAJIB tambah: **setiap opsi yang dicentang benar harus ber-isi (ber-teks)**. Idealnya "≥1 opsi benar ber-isi" untuk MC, "≥2 opsi benar ber-isi" untuk MA — gabung dengan validasi `correctCount` existing (jangan duplikasi, perketat agar correct-checked tanpa teks ditolak).
- **D-04 (client-side = opsional/Claude discretion):** Server-side adalah sumber kebenaran (WAJIB). Validasi client-side ringan di view `ManagePackageQuestions.cshtml` (disable submit / pesan inline saat opsi/correct kosong) = nice-to-have UX, boleh ditambah planner bila murah, BUKAN keharusan. (`AssessmentAdminController.cs` tetap fase ini; bila view disentuh, catat — view berbeda dari Phase 385.)

### PXF-04 — Essay kosong tidak dead-end finalize (SubmitEssayScore `~3525`, FinalizeEssayGrading `~3566`, page count `~3500`)
- **D-05 (definisi "essay kosong"):** Essay dianggap **kosong = 0 poin otomatis** bila **tidak ada response row** ATAU **`TextAnswer` kosong/whitespace**. Essay kosong **TIDAK dihitung pending**.
- **D-06 (predikat pending tunggal — kill-drift):** Definisikan **satu predikat** "essay pending" = **`TextAnswer` non-empty AND `EssayScore == null`** (peserta menjawab tapi HC belum menilai). Pakai predikat yang SAMA di:
  1. **Page count** `EssayGrading` (`~3500` `essayPendingCount = items.Count(EssayScore==null)`) → ubah agar hanya hitung essay ber-jawaban yang belum dinilai → tombol "Selesaikan Penilaian" muncul saat sisa pending = essay kosong saja.
  2. **FinalizeEssayGrading** essay-gate (`~3620` `essayResponses.Any(r => r.EssayScore == null)`) → jangan blokir karena essay ber-row tapi `TextAnswer` kosong + belum dinilai; treat sebagai 0. (No-row sudah lolos by-nature karena tak masuk `essayResponses`.)
  3. Konsisten dengan **Monitoring** (row-based, =0 "siap") → basis hitung sama.
- **D-07 (auto-0 mekanisme):** Tidak buat row saat GET (hindari side-effect on read). Essay kosong → skor efektif 0 via **`AssessmentScoreAggregator.Compute`** existing (`FinalizeEssayGrading ~3652`; response/EssayScore null = kontribusi 0). Tak perlu materialisasi row.
- **D-08 (SubmitEssayScore defensive upsert):** Hilangkan dead-end `"Jawaban tidak ditemukan"` (`~3530-3531`): bila HC menilai essay yang belum punya row, **create row lalu set EssayScore** (upsert) alih-alih error. Low-risk, menutup success-criterion "bukan error Jawaban tidak ditemukan". (No migration — insert row entitas existing.)

### PXF-05 — PDF per-peserta MA correctness (BulkExportPdf/GeneratePerPesertaPdf `~5070-5092`)
- **D-09 (helper terpusat semua tipe — kill-drift):** Ganti logika single-option (`resp = sessionResponses.FirstOrDefault(...)` → `resp.PackageOptionId` → `opt.IsCorrect`, `~5070-5079`) dengan **`AssessmentScoreAggregator.IsQuestionCorrect(q, sessionResponses.Where(r => r.PackageQuestionId == q.Id))`** untuk **SEMUA tipe** (MC/MA/Essay), bukan hanya essay. Helper sudah dipakai utk essay di `~5085` + sudah handle MA `SetEquals` non-empty guard (v30.0). Preseden: Excel path `~4863` sudah `selectedIds.SetEquals(correctIds)`.
- **D-10 (kolom "Jawaban" tampilkan semua opsi MA):** Untuk MA, kolom "Jawaban" PDF (`~5071 jawaban`, `~5102`) gabung **SEMUA `OptionText` terpilih** (join semua response rows, mis. "Avtur, Solar, Bensin") bukan 1 opsi. MC tetap 1 opsi. Essay tetap `TextAnswer` (existing). Bukti resmi → jawaban lengkap & label benar konsisten.
- **D-11 (jangan sentuh scoring engine):** Hanya display-path PDF yang diperbaiki. Scoring on-submit + `Compute` (yang sudah benar MA `SetEquals`, terverifikasi 14 unit test) TIDAK diubah. PDF hanya menyamakan label display dengan scoring yang sudah benar.

### Verifikasi (D-12)
- **PXF-02:** unit test (soal MC/MA 0-opsi ditolak, opsi-benar-tanpa-teks ditolak, ≥2 opsi ber-isi diterima) + Playwright (admin coba simpan soal Single 0-opsi → ditolak pesan jelas, soal tak tersimpan).
- **PXF-04:** unit test (pending-count basis Monitoring == page EssayGrading; essay kosong tak pending; finalize lolos dengan essay kosong) + Playwright (HC finalize sesi dengan ≥1 essay kosong → tombol Selesaikan muncul → round-trip sukses, essay kosong = 0).
- **PXF-05:** unit test PDF MA `IsQuestionCorrect` (benar={A,C,D} ⇒ Benar; partial/superset ⇒ Salah) + assert kolom Jawaban berisi semua opsi terpilih.
- Semua: `dotnet build` 0 error + `dotnet test` hijau + `dotnet run` (localhost:5277) sebelum commit (CLAUDE.md Develop Workflow). AD lokal `Authentication__UseActiveDirectory=false`; admin login `admin@pertamina.com`; Playwright `--workers=1`. UAT final di Dev diserahkan ke user/IT pasca re-deploy (❌ tak ada edit langsung Dev/Prod).

### Claude's Discretion
- Struktur predikat "essay pending" (extension method / helper inline / static) — selama dipakai konsisten di 3 titik (D-06).
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

### File yang disentuh fase ini (single file)
- `Controllers/AssessmentAdminController.cs`:
  - `CreateQuestion ~6395-6547` (validasi `~6440-6456` `correctCount`; loop opsi persist `~6486-6500` text-gated) — PXF-02.
  - `EditQuestion(POST) ~6600-6747` (validasi `~6647-6663`; update-in-place opsi `~6708-6747` text-gated) — PXF-02.
  - page action `EssayGrading` count `~3500` (`essayPendingCount`) + `EssayGradingPageViewModel` build — PXF-04.
  - `SubmitEssayScore ~3525-3554` (dead-end `~3530`, pendingCount `~3547`) — PXF-04.
  - `FinalizeEssayGrading ~3566-3760` (essay-gate `~3620`, `Compute ~3652`) — PXF-04.
  - per-peserta PDF `~5048-5111` (`FirstOrDefault ~5070`, single-option correctness `~5076-5086`, essay helper `~5085`) — PXF-05.
  - preseden Excel MA `SetEquals` `~4863` (referensi, jangan diubah).

### Helper & preseden (reuse, jangan ubah logika)
- `Helpers/AssessmentScoreAggregator.cs` — `IsQuestionCorrect(q, responses)` (MC/MA/Essay, bool?; MA non-empty guard `SetEquals`) + `Compute(questions, responses, passPct)`. Kill-drift single source (v30.0 ECG-01).
- `Helpers/QuestionTypeLabels.cs` — `Short(...)` label tipe utk pesan validasi (dipakai `~6444-6449`).

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
- **F-02/F-03/F-01/F-06/F-11/F-13/F-19/F-20/F-22** — Future pasca-acara (lihat `.planning/REQUIREMENTS.md` + STATE.md Deferred). F-03 (`SubmitEssayScore` status-guard pasca-finalize) bersinggungan dengan file ini tapi OUT milestone ini — JANGAN gabung kecuali trivial saat menyentuh `SubmitEssayScore`.
- **F-18** (export by-paket bukan `ShuffledQuestionIds`, ≥2 paket) — OUT/kondisional; mitigasi pakai 1 paket → skip.

### Reviewed Todos (not folded)
- "One-time cleanup data test/audit lokal setelah Phase 367 ship" (area: database, score 0.6) — match keyword false-positive (`controllers/assessmentadmincontroller`); isi sebenarnya = cleanup DB test lokal, BUKAN controller hardening. **Tidak di-fold** (out of scope hotfix). Tetap di backlog todo.

</deferred>

---

*Phase: 386-assessmentadmincontroller-hardening*
*Context gathered: 2026-06-15*
