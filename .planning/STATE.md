---
gsd_state_version: 1.0
milestone: v32.0
milestone_name: Manajemen Peserta
status: Defining requirements
stopped_at: "v32.0 Manajemen Peserta started 2026-06-17 — defining requirements + roadmap (phases 388-389: CreateWorker field fix + audit, penambahan peserta fleksibel saat ujian berjalan)"
last_updated: "2026-06-17T00:00:00.000Z"
last_activity: 2026-06-17
progress:
  total_phases: 0
  completed_phases: 0
  total_plans: 0
  completed_plans: 0
  percent: 0
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** v32.0 Manajemen Peserta — defining requirements + roadmap (phases 388-389: CreateWorker field fix + audit, penambahan peserta fleksibel saat ujian berjalan)

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: 2026-06-17 — Milestone v32.0 Manajemen Peserta started

**Milestone v32.0 Manajemen Peserta** (lanjut dari v31.0 phase terakhir 387; expected phases 388-389). 0 migration. Branch ITHandoff.
- **1.1 Penambahan peserta fleksibel saat ujian berjalan** — pastikan tambah peserta saat ada `InProgress` tetap jalan + tutup edge guard `Completed` sesi representatif + perjelas notice UX + regression test. File: `AssessmentAdminController.cs` (EditAssessment BULK ASSIGN L2114-2226, guard L1992, warning L2077-2085) + view/monitoring.
- **1.2 Perbaiki /Admin/CreateWorker + audit field** — buka kunci Nama/Email (readonly krn AD mode) + type=email + validation span; verifikasi runtime SEMUA field berfungsi + create submission sukses. File: `Views/Admin/CreateWorker.cshtml` (view-only; controller/model tak diubah).

(Penomoran phase final + success criteria ditentukan roadmapper.)

## Next Action

Defining requirements → roadmap (in progress via `/gsd-new-milestone`). Setelah roadmap approved: `/gsd-plan-phase <N>` (atau `/gsd-discuss-phase <N>`). Tiap fase: `dotnet build` + Playwright lokal sebelum commit → push `origin/ITHandoff` → notify IT (migration=FALSE). ❌ tidak ada edit di Dev/Prod (CLAUDE.md Develop Workflow).

## Tag Git

- `v24.0`, `v25.0`, `v26.0`, `v27.0`, `v28.0` — ✅ PUSHED ke `origin/ITHandoff` 2026-06-14.
- `v29.0` — ✅ PUSHED `origin/ITHandoff` 2026-06-15.
- `v30.0` — ✅ PUSHED `origin/ITHandoff` 2026-06-15 (HEAD `fe8c5ffe`).
- `v31.0` — belum dibuat (milestone aktif, roadmap baru).

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = **phase lama, dianggap OK / non-blocking** (kode sudah ship + jalan; tak ada bug report di milestone v16-v30). Bukan pekerjaan tertunda aktif. Tetap dicatat sebagai histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

### v31.0 Future (deferred pasca-acara) — dari readiness audit

| Temuan | Sev | Catatan | Status |
|--------|-----|---------|--------|
| F-02 | MED | Excel matrix label essay drift (`≥SV/2` vs `>0`) | Future (pasca-acara) |
| F-03 | MED | Edit essay pasca-finalize desync Score | Future (pasca-acara) |
| F-01 | LOW | UI MA tanpa warn "sebagian=0" | Future (mitigasi: briefing peserta) |
| F-06 | LOW | Cert nomor no-retry (essay finalize) | Future (pasca-acara) |
| F-11 | LOW | a11y aria opsi A/B/C/D | Future (pasca-acara) |
| F-13 | LOW | Finalize tak broadcast monitor | Future (1-operator ≈ nihil) |
| F-19 | LOW | Excel BulkExport essay selalu "—" | Future (pasca-acara) |
| F-20 | LOW | SubmitExam MC null-overwrite laten | Future (happy-path aman) |
| F-22 | LOW | SaveTextAnswer tanpa guard timer | Future (pasca-acara) |
| F-18 | MED | Export soal by-paket bukan ShuffledQuestionIds (≥2 paket) | OUT (kondisional; mitigasi: pakai 1 paket → skip) |

### v15.0 Deferred (carry-over) — ACCEPTED OK

| REQ | Item | Status | Due |
|-----|------|--------|-----|
| EPRV-01 | Preview Essay rubrik/jawaban — Jalur A (label) vs Jalur B (field baru) | accepted-OK (user 2026-06-14; buka bila perlu field baru) | 2026-05-12 |

### Carry-over dari v14.0 close (2026-04-24) — ACCEPTED OK

| Category | Item | Status | Source |
|----------|------|--------|--------|
| UAT | Phase 303 Plan 02 Task 3 — Coach Workload 12-langkah human verification | accepted-OK (kode ship+jalan; approval formal di-waive) | STATE.md (prior) |
| UAT | Phase 235 — 5 items butuh human verification via browser | accepted-OK | STATE.md (prior) |
| UAT | Phase 247 approval chain — 2 TODO (HC review + resubmit notification) | accepted-OK | STATE.md (prior) |
| Research gap | Phase 297 Pre-Post Renewal behavior — keputusan 2 sesi baru otomatis | accepted-OK (undecided, non-blocking) | v14.0 planning |
| Research gap | Phase 298 essay max character limit — nvarchar(max) vs nvarchar(2000) | accepted-OK (undecided, non-blocking) | v14.0 planning |
| Blocker | Phase 293 `GetSectionUnitsDictAsync` Level 2+ support | accepted-OK (org 2-level cukup; buka bila butuh >2 level) | v13.0 carry-over |
| v11.2 paused | Phase 281 (System Settings) + Phase 285 (Dedicated Impersonation Page) | accepted-OK (closed-early, non-blocking) | MILESTONES.md v11.2 |

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |
| 999.6 impersonate identity (dir tersisa) | sudah ditutup fungsional v28.0/377; dir backlog tinggal |
| 999.10 route CMP (dir tersisa) | sudah ditutup v28.0/378; dir backlog tinggal |
| 43 quick-task todo (audit-open, semua status `[missing]`) | acknowledged deferred (backlog project-wide lama, todo file ada artifact hilang) |

### Push IT

| Item | Status |
|------|--------|
| Push v29.0 + v30.0 ke `origin/ITHandoff` (branch + tag) | ✅ PUSHED 2026-06-15, HEAD `fe8c5ffe` |
| Notify IT — 2 migration carry (`PendingProtonBypass`+index/360, `ShuffleToggles`/372). **v29.0 + v30.0 = 0 migration baru.** | ⏳ PENDING — kasih commit hash + flag ke IT |
| **v31.0** — semua 5 fix **0 migration**; target 1 push → IT re-deploy Dev sebelum hari-H | ⏳ pending (milestone aktif, belum di-plan) |
| IT apply migration DB Dev + promosi server Dev (10.55.3.3)/Prod | ⏳ tanggung jawab IT (bukan dev) |

## Accumulated Context

### Roadmap Evolution

- **v31.0 roadmap dibuat 2026-06-15** — Phases 385-386, 5 PXF (PXF-01..05). Penomoran LANJUT dari v30.0 (384). Phasing by file-overlap: PXF-01+PXF-03 (file view) → 385; PXF-02+PXF-04+PXF-05 (semua `AssessmentAdminController.cs`) → 386 (gabung hindari konflik write paralel). 0 migration.
- Phase 385 sempat DIBATALKAN konteks-lama (2026-06-15): readiness ujian = verifikasi browser/UAT. **Catatan:** angka "385" kini DIPAKAI ULANG sebagai phase v31.0 Exam-Taking & Image Render Hotfix (build kode nyata, bukan verifikasi-only). Scope readiness asli tetap hidup di `.planning/notes/2026-06-15-readiness-ujian-lisensor.md`; 5 must-fix-nya jadi PXF-01..05.

### Decisions (persist across milestones)

- [v31.0 / 387-04 verifikasi proporsional D-09 — PHASE 387 CLOSE]: 3 jalur verifikasi sesuai kedalaman tiap fix (unit untuk logic, Playwright untuk a11y render runtime, manual untuk SignalR/cert/timer LOW tanpa harness). **Task 1** `HcPortal.Tests/PostLisensorPolishTests.cs` (baru) — fixture disposable `HcPortalDB_Test_{guid}` (`IAsyncLifetime` MigrateAsync→EnsureDeletedAsync, `[Trait Category=Integration]` di-exclude fast suite, `HcPortalDB_Dev` TAK disentuh — T-387-09 mitigasi), logika guard/cell direplikasi data-level PERSIS seperti controller (pola SubmitResurrectionTests): PXF-06 guard status (`S.Completed` reject / `S.PendingGrading` allow EssayScore), PXF-09 essay cell (`"Skor: X/Y"` / `"Tidak dijawab"` / `"Belum dinilai"`), PXF-12 `answers.ContainsKey` absent→PackageOptionId unchanged. 8 facts positive+negative → **8/8 PASS**. **Task 2** `tests/e2e/aria-opsi-387.spec.ts` (baru) — assert `aria-label` berisi "opsi A" RUNTIME di Results + ExamSummary (D-09 MANDATORY: a11y Razor dinamis, grep+build INSUFFICIENT — lesson Phase 354) → **3/3 PASS** `--workers=1`. **Task 3 checkpoint human-verify — APPROVED** (T-387-10 mitigasi: 3 LOW tanpa harness verified manual): browser+SignalR+DB localhost:5277 AD-off shared-memory SQL; snapshot→mutate→RESTORE `C:\Temp .bak` per CLAUDE.md Seed Workflow, `docs/SEED_JOURNAL.md` CLEANED 0 residue. **PXF-08** finalize sesi 169 "TEST E2E Campur 2026-06-15" (essay 10/10, GenerateCertificate=1, IsPassed) → `NomorSertifikat`="KPB/005/VI/2026" ter-assign (retry-loop generate+persist), session→Completed, no certError saat sukses (dikonfirmasi DB 2× finalize). **PXF-10** klien SignalR `/hubs/assessment` JoinMonitor batchKey tepat "TEST E2E Campur 2026-06-15|OJT|2026-06-15" → grup monitor terima `workerSubmitted` live `{sessionId:169, workerName:"Admin KPB", score:100, result:"Pass", status:"Completed"}` tanpa refresh (percobaan-1 meleset hanya karena Title ber-suffix tanggal → re-join key tepat tertangkap bersih). **PXF-13** A/B `SaveTextAnswer` sesi admin-owned: (A) StartedAt=2020+Dur=1min EXPIRED→tulis DITOLAK TextAnswer UNCHANGED; (B) StartedAt=now+Dur=60min NOT expired→tulis SUKSES — guard mirror verbatim `SaveMultipleAnswer`. Semua mutasi (sesi 169 + responses) di-RESTORE; SEED_JOURNAL CLEANED. **PXF-06 fact=guard status** menyelaraskan redirect 387-01 (WR-01/WR-02 sudah cover build+grep di 387-01). Verif fase: fast suite **347/347 GREEN** + build 0 error + 0 migration; `HcPortalDB_Dev` untouched. Commits `46bd422d` (Task1 unit) + `3b4db3a2` (Task2 Playwright); Task 3 verify-only. **No deviation.** **PHASE 387 SELESAI** (4/4 plans; 7 REQ PXF-06/08/09/10/11/12/13 closed; 0 migration). **Handoff:** deploy IT KEDUA pasca-acara terpisah dari bundle 385+386 — gabung → push `origin/ITHandoff` (BUKAN sekarang, keputusan developer) → notify IT flag migration=FALSE. ❌ JANGAN edit Dev/Prod.
- [v31.0 / 387-03 PXF-11 aria opsi huruf A/B/C/D]: a11y polish 2 surface review (file-disjoint dari Plan 01/02, wave 1, no overlap `AssessmentAdminController.cs`/`CMPController.cs`/`AssessmentHub.cs`). Kedua loop opsi yang sebelumnya `@foreach` tanpa index var diubah ke indexed `@for` agar bisa derive huruf, lalu satu-satunya perubahan markup lain = string `AriaContext` partial `_QuestionImage`. **Results.cshtml** (Options List ~L356-391): `@foreach (var option in question.Options)` → `@{ string[] letters = { "A","B","C","D" }; } @for (int oi = 0; oi < question.Options.Count; oi++) { var option = question.Options[oi]; var letter = oi < letters.Length ? letters[oi] : (oi + 1).ToString(); ... AriaContext = "opsi " + letter }`. Markup per-opsi (itemClass success/danger, icon check/x, OptionText span, `(Jawaban Anda)`/`(Jawaban Benar)` label) + blok essay-fallback `!question.Options.Any() && !IsNullOrEmpty(UserAnswer)` UTUH. **ExamSummary.cshtml** (~L57-63): `@foreach (var optImg in item.OptionImages)` → indexed `@for` + `var optImg = item.OptionImages[oi]` + letter; `AriaContext = "opsi " + letter`. Partial gambar-SOAL `Cap = 240` (L56, no AriaContext) TIDAK disentuh (git-diff verified — hanya context line). **Pilihan `for` vs `Select((o,i))`:** kedua koleksi adalah `List<T>` (`question.Options`=`List<OptionReviewItem>`, `item.OptionImages`=`List<ExamSummaryOptionItem>`) → indexable `[oi]`, jadi pakai indexed-`for` (TIRU TEPAT `StartExam.cshtml:125/134/148`, diff lebih kecil dari projection). **`letters` scope:** grep konfirmasi tak ada deklarasi `string[] letters` sebelumnya di kedua view → deklarasi 1× per loop aman (no Razor duplicate-var error). `_QuestionImage` baca `AriaContext` via reflection + render NOTHING saat `ImagePath` null → huruf hanya muncul pada opsi yang punya gambar. Verif: `dotnet build HcPortal.csproj` 0 error per task + grep acceptance PASS + `git diff` ExamSummary konfirmasi Cap=240 partial unchanged; 0 migration; no deletion. Commits `77f0f57f` (Task1 Results) + `5cef4e81` (Task2 ExamSummary). **D-09 MANDATORY (defer Plan 04):** PXF-11 butuh Playwright runtime assert `aria-label` berisi huruf di KEDUA surface (a11y render = Razor dinamis, grep+build INSUFFICIENT — lesson Phase 354). **PXF-11 code-complete; 0 migration.** Next: 387-04 (test/e2e PXF-11 + PXF-12).
- [v31.0 / 387-02 PXF-12 + PXF-13 participant write guards]: 2 server-authoritative guard di file disjoint dari Plan 01/03 (CMPController.cs + AssessmentHub.cs; tak ada overlap dgn AssessmentAdminController.cs Plan 01). **PXF-12** (`Controllers/CMPController.cs` `SubmitExam`, cabang MC upsert ~L1712): bungkus `existingResponse.PackageOptionId = selectedOptId; existingResponse.SubmittedAt = DateTime.UtcNow;` dengan guard `if (answers.ContainsKey(q.Id))` — reuse self-analog L1703 (di sana bentuk ternary `answers.ContainsKey(q.Id) ? ... : null`, jadi pattern `if (answers.ContainsKey(q.Id))` match TEPAT 1× = guard baru). Efek: soal MC absent dari form submit (mis. form parsial / JS gagal) TIDAK lagi null-overwrite jawaban yang sudah ter-autosave via SignalR (T-387-05 Tampering/DoS-by-dataloss). Cabang MA (`MultipleAnswer` L1728) + `else if (selectedOptId.HasValue)` add-block + Essay manual = untouch. **PXF-13** (`Hubs/AssessmentHub.cs` `SaveTextAnswer` ~L151): sisip guard timer-expiry VERBATIM dari sibling `SaveMultipleAnswer:205-215` SETELAH blok `session == null` SEBELUM truncate/upsert — `if (session.StartedAt.HasValue && session.DurationMinutes > 0) { elapsed = (UtcNow - StartedAt).TotalSeconds; allowed = (DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60; if (elapsed > allowed) { LogWarning("SaveTextAnswer: timer expired for session {SessionId}", sessionId); return; } }`. Satu-satunya beda vs analog = log string `SaveMultipleAnswer`→`SaveTextAnswer` (param nama `sessionId` sama di kedua method → port tanpa rename). Efek: tulis essay pasca-timer ditolak + dicatat (T-387-06 Tampering / T-387-07 Repudiation), memperhitungkan ExtraTimeMinutes konsisten dgn MA. Literal `"InProgress"` query L144 TAK diubah (out of scope, konsisten SaveMultipleAnswer:198). `_logger`/`StartedAt`/`DurationMinutes`/`ExtraTimeMinutes` sudah in-scope (session full-entity via FirstOrDefaultAsync L143). Verif: `dotnet build` 0 error + fast suite `dotnet test --filter Category!=Integration` **347/347 GREEN** (no regression) + grep acceptance (`if (answers.ContainsKey(q.Id))` L1714 di dalam blok `existingResponses.TryGetValue`; `SaveTextAnswer: timer expired for session` L158; `elapsed > allowed` 2× = L156 SaveText + L222 SaveMultiple) + `git diff` konfirmasi MA branch unchanged & `"InProgress"` unchanged; 0 migration; no file deletion. Commits `b457f57c` (PXF-12) + `0cd566ae` (PXF-13). **TDD gate:** kedua task `tdd="true"` tapi plan `<verification>`/`<done>` eksplisit serahkan unit test PXF-12 (Test 2 absent-MC-no-nullify, disposable real-SQL fixture) ke Plan 04 — tak ada `test(...)` RED commit di plan ini by design; verifikasi via build+grep+regression. **PXF-12 + PXF-13 closed (code-level); 0 migration.** Next: 387-03 (view aria PXF-11) + 387-04 (test PXF-12).
- [v31.0 / 386-06 Wave-5 verify/e2e + UAT browser — PHASE 386 CLOSE]: 2 e2e spec Wave-0 di-un-gate (`test.fixme` dilepas, 0 sisa) + di-reconcile ke perilaku ter-wire. `option-validation-386.spec.ts`: submit Single Answer all-opsi-kosong (satu di-check correct) → assert pesan LOCKED `/minimal 2 opsi.*berisi teks/i` di `.alert-danger` + soal malformed TIDAK ter-persist. `essay-empty-finalize-386.spec.ts`: drive sesi PendingGrading + ≥1 essay kosong → buka EssayGrading per-worker → "Selesaikan Penilaian" visible → grade essay terjawab → Selesaikan → sukses tanpa "Jawaban tidak ditemukan". **Self-contained seed** (D-baru): tiap spec bawa seed SQL idempotent sendiri (`tests/sql/option-validation-386-seed.sql` + `tests/sql/essay-empty-finalize-386-seed.sql`) supaya deterministik tanpa andalkan data ambient; dicatat `docs/SEED_JOURNAL.md` (temporary + local-only, sesuai CLAUDE.md Seed Data Workflow). Gate fase (CLAUDE.md Develop Workflow, verifikasi lokal SEBELUM commit): `dotnet build` 0 error + `dotnet test` **474/474 GREEN** (incl EssayEmptyPendingParity 6/6 varian `"  "`/`"\t\n"`, OptionValidation, PdfAnswerCell, IsQuestionCorrect regression, Authz) + e2e `option-validation-386`+`essay-empty-finalize-386` `--workers=1` **3/3 PASS**. **Browser UAT 4/4 APPROVED** (localhost:5277, AD-off, shared-memory SQL — QuestPDF/ClosedXML byte output TIDAK unit-assertable → human sign-off = bukti kanonik PXF-05 SC#3 + F-DEV-02): (1) **PDF** per-peserta sesi 118 "UAT v14 Standard" (Iwan, NIP 123456) via `pdftotext` — MA all-or-nothing via shared `IsQuestionCorrect`+`BuildAnswerCell`: Soal 2 "CO2, CH4"→Benar, Soal 7 "Propana, Butana"→Benar, Soal 8 "Helm, Safety shoes, Kacamata safety"→Benar, **Soal 9 partial {Impeller} dari {Impeller,Volute,Shaft}→SALAH = F-17 fix proof**; (2) **Excel** "Detail Jawaban" sheet (xlsx sharedStrings) byte-identik label dgn PDF, Soal 9 Peserta "Impeller" vs Benar "Impeller, Volute, Shaft" → Status ✗ = **F-DEV-02 proof** (kedua surface resmi route ke 1 helper bersama); essay render "Essay – manual grading"/"—"; (3) **PXF-02** ManagePackageQuestions pkg50 "Paket A — ojt v1.10" submit SA all-opsi-kosong → reject banner "Single Answer membutuhkan minimal 2 opsi jawaban yang berisi teks.", Daftar Soal tetap 20 (DB count 20→20, no 'PXF-02 UAT%' row) = **F-DEV-01 exam-freeze closed**; (4) **PXF-04** EssayGrading sesi 118 render benar — 4 essay + Skor + "Simpan Skor" per-item + tombol "Selesaikan Penilaian"; behavior essay-emptied→finalizable covered automated e2e 3/3 + parity unit 6/6 (no DB mutation, data sesi-118 utuh). DB lokal bersih, no seed temporary tersisa. Commit Task 1 `87112ad4` (test); Task 2 (suite gate) + Task 3 (UAT) verify-only no-commit. **PXF-02 + PXF-04 + PXF-05 CLOSED end-to-end (unit+e2e+manual); 0 migration. PHASE 386 SELESAI** (6/6 plans). Sisa: gabung Phase 385 (complete) → 1 push `origin/ITHandoff` → notify IT re-deploy Dev (flag migration=FALSE) sebelum hari-H ~2026-06-17. ❌ JANGAN edit kode/DB Dev/Prod.
- [v31.0 / 386-05 Wave-4 PXF-05 PDF + Excel MA-label wiring]: 2 surface bukti-resmi export di-route ke helper display bersama `AssessmentScoreAggregator.IsQuestionCorrect` (label) + `BuildAnswerCell` (jawaban) → MA di-label all-or-nothing (SetEquals) + list SEMUA opsi terpilih di KEDUA surface identik; **kill-drift**. (1) **PDF** `GeneratePerPesertaPdf` (`Controllers/AssessmentAdminController.cs`, loop "Detail Jawaban per Soal"): blok single-row `var resp = sessionResponses.FirstOrDefault(...)` + `if (resp != null){... opt.IsCorrect ...}` (MA mislabel — baca 1 row) DIGANTI `var responsesForQ = sessionResponses.Where(r => r.PackageQuestionId == q.Id).ToList(); bool? correct = IsQuestionCorrect(q, responsesForQ); string jawaban = BuildAnswerCell(q, responsesForQ);` — `statusColor`/`statusText` ternary (✓ Benar/✗ Salah/— Pending) + render QuestPDF UTUH byte-identik; MC byte-identik; Essay sudah pakai helper. (2) **Excel** `ExcelExportHelper.AddDetailPerSoalSheet` (F-DEV-02 D-13 folded — surface mislabel KEDUA): blok `var response = responses.FirstOrDefault(...)` + `if (response==null){...}else{...selectedOption.IsCorrect / EssayScore >= ScoreValue/2...}` DIGANTI `var responsesForQ = responses.Where(r => r.AssessmentSessionId == session.Id && r.PackageQuestionId == q.Id).ToList();` → BuildAnswerCell + IsQuestionCorrect; cell 2-kolom (Jawaban + ✓/✗/— warna Green/Red) UTUH. **Unifikasi label essay Excel (INTENTIONAL D-13):** lama `EssayScore >= ScoreValue/2` → `> 0` (v30.0 canonical) = SAMA dgn PDF + web Results (1 aturan correctness essay lintas semua surface resmi). Tak perlu `using` baru (ExcelExportHelper sudah namespace `HcPortal.Helpers`). **Compute (scoring engine) TAK DISENTUH** — `Helpers/AssessmentScoreAggregator.cs` 0 diff lintas 2 commit (git-verified, D-11 display-path saja). Nuansa no-response Excel: dulu both cell "—"; kini BuildAnswerCell→"—" (unchanged) + IsQuestionCorrect→`false` utk MC/MA tak-jawab (✗) / `null` Essay pending (—) — sama kontrak PDF (Task 1) + web Results. Verif: build 0 error; grep single-row mislabel 0× di kedua file; pure suite 347/347 GREEN (incl PdfAnswerCellTests + IsQuestionCorrect regression); 0 migration. Commits 85861b69 (PDF) + bb058f1b (Excel). **PXF-05 CLOSED** (helper Wave-1 386-02 + wiring ini). e2e tetap `test.fixme` (un-skip Plan 06 + UAT browser PDF/Excel).
- [v31.0 / 386-04 Wave-3 PXF-04 essay-empty finalize parity + upsert]: predikat pending essay TUNGGAL `!string.IsNullOrWhiteSpace(TextAnswer) && EssayScore == null` di **4 surface byte-identik** (`Controllers/AssessmentAdminController.cs` SITE1 page L3506, SITE2 finalize-gate L3650, SITE3 SubmitEssayScore L3580, SITE4 Monitoring L3308-3321) → tutup F-04 (essay dikosongkan = dead-end finalize, pending-count divergen). `SubmitEssayScore` jadi **defensive upsert** (buat PackageUserResponse bila absen, PackageOptionId=null/TextAnswer=null/EssayScore=score; idiom AssessmentHub.SaveTextAnswer) ganti dead-end "Jawaban tidak ditemukan" + **status-guard WAJIB** tolak `Status != PendingGrading` ("Penilaian hanya bisa dilakukan saat status Menunggu Penilaian.", cermin FinalizeEssayGrading:3591) — D-08, T-386-AUTHZ HIGH (tutup F-03 widening). Urutan WAJIB status-guard→load question→range-guard→upsert (skor invalid tak pernah bikin row). Attributes `[HttpPost]/[Authorize Admin,HC]/[ValidateAntiForgeryToken]` UTUH (reflection lock GREEN). **Rule-1 koreksi (Task 3):** RESEARCH L60/L301 keliru — `IsNullOrWhiteSpace` di EF Core 8 SQL Server TIDAK translate ke `=N''` murni; probe langsung buktikan SQL Server LTRIM/RTRIM/TRIM hanya trim spasi ASCII (BUKAN tab CHAR9/newline CHAR10) → server-side eval divergen dari .NET utk TextAnswer=`\t\n` (re-introduce divergensi count T-386-04-COUNT = bug F-04 itu sendiri). Solusi: 2 EF site (SITE3+SITE4) filter `EssayScore==null` + Join Essay **server-side**, materialize TextAnswer (set kecil ≤30 peserta), lalu `!IsNullOrWhiteSpace` **IN-MEMORY** (semantik .NET penuh). Predikat LOGIS tetap byte-identik 4 surface; hanya titik-eval whitespace geser server→memori (TIDAK ubah teks predikat — sesuai larangan plan). 2 EF mirror builder di `EssayEmptyPendingParityTests` diselaraskan ke shape produksi baru (drift-guard tetap akurat). Verif: parity 6/6 GREEN (incl varian `"  "` + `"\t\n"`) + authz 2/2 + full suite 474/474; build 0 error; 0 migration (INSERT entity existing, field nullable). Commits 6efd0294 (4-site predikat) + 79132809 (upsert+guard) + 866917b6 (whitespace in-memory + mirror). **PXF-04 CLOSED.** e2e `essay-empty-finalize-386.spec.ts` tetap `test.fixme` (un-skip Plan 06).
- [v31.0 / 386-03 Wave-2 PXF-02 wiring]: `QuestionOptionValidator.ValidateQuestionOptions` di-wire ke BOTH `CreateQuestion` + `EditQuestion(POST)` di `Controllers/AssessmentAdminController.cs` — blok byte-identik (12 baris) disisipkan SETELAH gate Essay-rubrik, SEBELUM persist (Create L6458-6469 sebelum `int nextOrder`; Edit L6665-6676 sebelum `var oldScore`). Reuse helper + LOCKED error strings Wave-1 lewat helper (controller TIDAK re-author wording → tak bisa drift Create vs Edit). **correctCount gate UTUH** (MC `!= 1`, MA `< 2`) — orthogonal (kuantitas-benar vs ada-teks), keduanya jalan sebelum persist. **Client-side D-04 (ManagePackageQuestions.cshtml) SENGAJA di-skip** — server-side = must-fix F-DEV-01; warning browser = layer UX opsional deferred. **SyncPackagesToPost (def L5645, 6 call) + ImportPackageQuestions (L5952/5969) LOCKED-OUT** — tak ada validasi disisipkan di dekatnya; copy-path/importer/persist loop tak tersentuh. Verif: `ValidateQuestionOptions` tepat 2× (L6460+L6679); full build 0 error/0 warning; `dotnet test --filter Category!=Integration` 347/347 GREEN (incl OptionValidationTests). **PXF-02 CLOSED** (helper Wave-1 + wiring ini). Commit cc8b610b; 0 migration. e2e `option-validation-386.spec.ts` tetap `test.fixme` (un-skip Plan 06).
- [v31.0 / 386-02 Wave-1 GREEN]: 2 helper pure EF-free dibuat → 24/24 test GREEN (OptionValidation 7 + PdfAnswerCell 6 + IsQuestionCorrect regression). (1) `Helpers/QuestionOptionValidator.cs` namespace `HcPortal.Helpers` — `ValidateQuestionOptions(type, texts, corrects)`: Essay/non-opsi bypass; MC/MA wajib ≥2 opsi ber-teks (D-01) + tiap opsi correct wajib ber-teks (D-03); ber-teks=!IsNullOrWhiteSpace (D-02). TIDAK menyalin correctCount gate (tetap di controller L6440-6456). **Pesan error LOCKED untuk Wave 2:** `"{Short} membutuhkan minimal 2 opsi jawaban yang berisi teks."` + `"Opsi yang ditandai sebagai jawaban benar harus berisi teks ({Short})."`. (2) `AssessmentScoreAggregator.BuildAnswerCell` (di samping IsQuestionCorrect, **Compute+IsQuestionCorrect body tak berubah, D-11**): MA join SEMUA OptionText terpilih urut Id `.OrderBy(o=>o.Id)` separator `", "` (D-10 preseden Excel L4860); MC OptionText tunggal; Essay truncate 300+`"..."` (L5083); kosong=`"—"`. Wave 2 wire ke CreateQuestion+EditQuestion; Wave 4 wire ke PDF/Excel. Commits d7a49dc3 + 85ce39e1; 0 migration; 0 controller/view touched.
- [v31.0 / 386-01 Wave-0 RED]: TDD-RED scaffold 6 test files dulu (PXF-02/04/05) sebelum kode produksi. MA answer-cell join **LOCKED = ", " (comma-space, D-10 preseden Excel)** — Wave 1 `BuildAnswerCell` WAJIB match. 4 mirror count-builder encode predikat BARU `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` (Wave 3 menyamakan 4 production site ke mirror, drift-guard cite L3308/L3500/L3547/L3620). Build RED HANYA pada Wave-1 helper `QuestionOptionValidator.ValidateQuestionOptions` (file baru) + `AssessmentScoreAggregator.BuildAnswerCell` (method baru). 0 production code. e2e gated `test.fixme`.
- [v31.0 / phasing]: 3 REQ yang menyentuh `Controllers/AssessmentAdminController.cs` (PXF-02 CreateQuestion/EditQuestion, PXF-04 EssayGrading pending-count, PXF-05 BulkExportPdf/GeneratePerPesertaPdf) **digabung satu fase (386)** untuk menjamin nol konflik write paralel. PXF-01 (`_QuestionImage.cshtml`) + PXF-03 (`StartExam.cshtml`) file-disjoint → Phase 385.
- [v30.0 / ECG-06 (383-04)]: Regression lock poin 2 (Simpan/Selesaikan essay) tanpa ubah kode produksi (D-05); 5 test mirror-data-level di `EssayFinalizeRecomputeTests.cs`; full suite 440/440. Migration guard `dotnet ef add _verify_383` = 0 model diff.
- [v30.0 / ECG-01..05]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar, `=0`=Salah, `null`=pending) dipakai di `CMPController.Results` 4 site + PDF export `GeneratePerPesertaPdf` (kill-drift). MA non-empty guard `selected.Count>0 && SetEquals` (display-path, beda dari scoring `Compute`).
- [v29.0 / 382 / D-01-IMPACT]: SAVE-01 dedupe last-write-wins in-memory, **NO migration**. v29.0 + v30.0 = 0 migration baru.
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen default ON; engine pure `Helpers/ShuffleEngine.cs`.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route].

### Open Blockers/Concerns

- **F-09 (PXF-01) belum dikonfirmasi browser oleh fixer** — verifier read-only confirmed HARD di Dev (404, prefix drop) 2026-06-15. **WAJIB UAT browser 1× di `http://10.55.3.3/KPB-PortalHC` layar StartExam bergambar sesudah fix + re-deploy** sebelum ujian. Lokal no-repro (no PathBase) → andalkan Playwright + URL prefix.
- **F-DEV-01 (PXF-02) — 1 soal salah-konfig membekukan submit awal untuk SEMUA peserta** (timer-expiry auto-submit tetap fire, soal 0-opsi auto-0). Mitigasi operasional tetap: cek tiap soal punya opsi saat setup.
- [push] Carry migration (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag. v28/v29/v30/**v31 = 0 migration baru.**

## Session Continuity

Last activity: 2026-06-15

Stopped at: Completed 387-04-PLAN.md (**Phase 387 COMPLETE** — verifikasi proporsional D-09: 8 xUnit Integration facts PXF-06/09/12 disposable real-SQL + Playwright a11y PXF-11 2-surface 3/3 + browser/SignalR/DB manual PXF-08/10/13 APPROVED [cert KPB/005/VI/2026 + workerSubmitted live + timer A/B]; fast suite 347/347 GREEN + build 0 error + 0 migration; commits `46bd422d` test / `3b4db3a2` test; SEED_JOURNAL CLEANED 0 residue)

Next action: **v31.0 SELESAI lokal — Phase 385 + 386 + 387 ketiganya COMPLETE + verified** (385+386 = must-fix pra-ujian; 387 = Post-Lisensor Polish 7 REQ pasca-acara). Sisa handoff (CLAUDE.md Develop Workflow):

1. **Bundle URGENT (pra-ujian):** push 385+386 ke `origin/ITHandoff` + notify IT (commit hash HEAD + flag migration=FALSE) → IT re-deploy Dev (10.55.3.3) + **UAT browser PXF-01** (gambar sub-path `/KPB-PortalHC`, open blocker — lokal no-repro) sebelum hari-H ~2026-06-17.
2. **Bundle 387 (PASCA-acara, deploy IT KEDUA terpisah):** Phase 387 (PXF-06/08/09/10/11/12/13) gabung → push `origin/ITHandoff` (BUKAN sekarang — keputusan developer) → notify IT flag migration=FALSE.

❌ JANGAN edit DB/kode Dev/Prod (Develop Workflow). Carry lama migration (PendingProtonBypass+index/360, ShuffleToggles/372) tetap pending — v31.0 = 0 migration baru. Pasca-handoff: `/gsd-verify-phase 387` atau `/gsd-new-milestone`.
