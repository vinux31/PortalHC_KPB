---
gsd_state_version: 1.0
milestone: v32.0
milestone_name: Manajemen Peserta
status: verifying
stopped_at: Completed 392-02-PLAN.md
last_updated: "2026-06-17T07:29:24.281Z"
last_activity: 2026-06-17
progress:
  total_phases: 23
  completed_phases: 2
  total_plans: 4
  completed_plans: 4
  percent: 100
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 392 — Perbaikan CreateWorker + Audit Field

## Current Position

Phase: 999.7
Plan: Not started
Status: Phase 392 complete — WRKR-01/02/03 closed; ready for `/gsd-verify-work` + notify IT (migration=FALSE)
Last activity: 2026-06-17

**Milestone v32.0 Manajemen Peserta** — Phases 391-392 (LANJUT dari v31.0 phase terakhir 387; tidak reset ke 1). 0 migration. Branch main. 7/7 REQ mapped (0 orphan, 0 duplikat).

- **Phase 391 — Penambahan Peserta Fleksibel saat Ujian Berjalan** (PART-01..04): tambah peserta saat ada `InProgress` tetap jalan + tutup edge guard `Completed` sesi representatif (tak salah-blokir selama window terbuka) + ganti warning kosmetik jadi notice informatif + regression test (warisi status induk + tak overwrite sesi existing). File: `Controllers/AssessmentAdminController.cs` (EditAssessment BULK ASSIGN ~L2114-2226, guard `Completed` ~L1992, notice TempData ~L2077-2085) + view/monitoring + xUnit. **UI hint: yes.**
- **Phase 392 — Perbaikan CreateWorker + Audit Field** (WRKR-01..03): buka kunci Nama/Email (`readonly` krn AD mode) + `type="email"` + `<span asp-validation-for>` inline (Position/Directorate/Section/Unit); audit + Playwright-verify SEMUA field berfungsi + create submission sukses. File: `Views/Admin/CreateWorker.cshtml` (VIEW-ONLY; controller `WorkerController.CreateWorker` + model `ManageUserViewModel` tak diubah). **UI hint: yes.**

Dua fase file-disjoint & independen → boleh dikerjakan paralel.

## Next Action

Phase 392 SELESAI (WRKR-01/02/03 closed; Plan 01 `0d788e8a` view fix + Plan 02 `840fab21` e2e verify). **Next: `/gsd-verify-work` Phase 392** lalu notify IT (commit hash + flag migration=FALSE). Phase 391 (Penambahan Peserta Fleksibel) masih bisa di-plan/execute paralel (file-disjoint). Tiap fase: `dotnet build` + `dotnet run` (localhost:5277) + Playwright lokal sebelum commit → branch main → notify IT. ❌ tidak ada edit di Dev/Prod (CLAUDE.md Develop Workflow).

⚠️ **Catatan env e2e (Plan 02):** app TIDAK pakai runtime Razor compilation (`AddControllersWithViews` tanpa `AddRazorRuntimeCompilation`) → view embedded saat build. Untuk verifikasi e2e perubahan view, WAJIB jalankan build/app dari **main working tree** (bukan worktree sibling `PortalHC_KPB-ITHandoff` yang pre-Plan-01). Carry DEF-392-01 (shared `initFormLoading` disable tombol pada submit-divalidasi-gagal — infra bersama, future phase).

## Tag Git

- `v24.0`, `v25.0`, `v26.0`, `v27.0`, `v28.0` — ✅ PUSHED ke `origin/ITHandoff` 2026-06-14.
- `v29.0` — ✅ PUSHED `origin/ITHandoff` 2026-06-15.
- `v30.0` — ✅ PUSHED `origin/ITHandoff` 2026-06-15 (HEAD `fe8c5ffe`).
- `v31.0` — ✅ shipped local + MERGED origin/main 2026-06-16 (merge `7ea6c81e`; branch ITHandoff HEAD `64456bd5`).
- `v32.0` — belum dibuat (milestone aktif, roadmap baru, belum di-plan).

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = **phase lama, dianggap OK / non-blocking** (kode sudah ship + jalan; tak ada bug report di milestone v16-v31). Bukan pekerjaan tertunda aktif. Tetap dicatat sebagai histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

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
| Notify IT — 2 migration carry (`PendingProtonBypass`+index/360, `ShuffleToggles`/372). **v29.0 + v30.0 + v31.0 = 0 migration baru.** | ⏳ PENDING — kasih commit hash + flag ke IT |
| **v31.0** — semua fix **0 migration**; merged origin/main 2026-06-16 (`7ea6c81e`); Dev UAT full lifecycle PASS. Sisa: notify IT (ITHandoff `64456bd5` / main `7ea6c81e`, migration=FALSE) | ⏳ pending notify |
| **v32.0** — kedua fase **0 migration** (view + logic + test); target push `origin/main` per-fase → notify IT re-deploy Dev | ⏳ pending (milestone aktif, belum di-plan) |
| IT apply migration DB Dev + promosi server Dev (10.55.3.3)/Prod | ⏳ tanggung jawab IT (bukan dev) |

## Accumulated Context

### Roadmap Evolution

- **v32.0 roadmap dibuat 2026-06-17** — Phases 391-392, 7 REQ (PART-01..04 + WRKR-01..03). Penomoran LANJUT dari v31.0 phase terakhir (387) → mulai 391 (tidak reset). Phasing by file-overlap (split alami 2 fase, fitur file-disjoint & independen): PART-01..04 (`AssessmentAdminController.cs` + view/monitoring + test) → 391; WRKR-01..03 (`Views/Admin/CreateWorker.cshtml` view-only) → 392. 0 migration (kedua fase). Out of scope: hard-block penambahan saat InProgress, perubahan controller/model CreateWorker, AD-sync, migration.
- **v31.0 roadmap dibuat 2026-06-15** — Phases 385-386, 5 PXF (PXF-01..05). Penomoran LANJUT dari v30.0 (384). Phasing by file-overlap: PXF-01+PXF-03 (file view) → 385; PXF-02+PXF-04+PXF-05 (semua `AssessmentAdminController.cs`) → 386 (gabung hindari konflik write paralel). 0 migration. (Phase 387 ditambah pasca-acara: 7 REQ polish PXF-06..13.)
- Phase 385 sempat DIBATALKAN konteks-lama (2026-06-15): readiness ujian = verifikasi browser/UAT. **Catatan:** angka "385" kini DIPAKAI ULANG sebagai phase v31.0 Exam-Taking & Image Render Hotfix (build kode nyata, bukan verifikasi-only). Scope readiness asli tetap hidup di `.planning/notes/2026-06-15-readiness-ujian-lisensor.md`; 5 must-fix-nya jadi PXF-01..05.

### Decisions (persist across milestones)

- [v32.0 / 392-02 WRKR-03 — PHASE 392 CLOSE]: Playwright e2e (`tests/e2e/createworker-392.spec.ts`, AD-off, `--workers=1`) **GREEN 3/3** (setup + TEST A static guard + TEST B runtime) buktikan `/Admin/CreateWorker` usable runtime: field `#FullName`/`#Email` bisa diketik (tidak readonly), `#Email` `type="email"`, `window.jQuery.validator` ter-load (D-05 client-side aktif via `_ValidationScriptsPartial` di `@section Scripts`), `.field-validation-error` span muncul saat submit invalid (tetap di CreateWorker), cascade Bagian→Unit bangun opsi Unit runtime, create sukses (redirect ManageWorkers + Success flash "berhasil" + baris DB). **TEST A** static source-grep guard: CreateWorker.cshtml TAK punya `readonly=`/`bg-light` (editable di AD mode BY CONSTRUCTION — tutup bug readonly-AD yang run AD-off tak bisa exercise, Pitfall F-NEW-04) + `type="email"` + `_ValidationScriptsPartial` ada. **Teardown** self-cleaning `afterAll` (jalan walau gagal) via `DeleteWorker` POST (anti-forgery token + Identity cascade roles) → verifikasi `SELECT COUNT(*) FROM Users WHERE Email LIKE 'e2e-cw-%@local.test'` = **0** (0 residu, 0 orphan role); SEED_JOURNAL Phase 392 → CLEANED. **DEVIASI Rule 3 (blocking env-correction):** app awal di :5277 ternyata dari **worktree SIBLING `PortalHC_KPB-ITHandoff`** (branch ITHandoff HEAD `f648cc00`, BUKAN ancestor Plan-01 `0d788e8a`) → CreateWorker.cshtml-nya masih `readonly="@(isAdMode?...)"` + tanpa validation scripts → `jQuery.validator` undefined → TEST B gagal. Root cause: app pakai `AddControllersWithViews()` **tanpa** `AddRazorRuntimeCompilation` (no `RuntimeCompilation` package) → **view embedded saat build**, edit/commit `.cshtml` tak pengaruhi binary stale/sibling. Resolusi: `dotnet build HcPortal.csproj` (0 error) main-tree → stop app ITHandoff → run `HcPortal.exe` main-tree :5277 AD-off (DB SQLEXPRESS sama) → `_ValidationScriptsPartial` ter-serve SETELAH jQuery + `validator===true` → spec hijau. **DEF-392-01 (deferred, deferred-items.md):** `wwwroot/js/shared-loading.js` `initFormLoading` men-disable tombol submit pada submit yang DIBATALKAN validasi (preventDefault tak hentikan listener native lain) → tombol nyangkut disabled; infra bersama pra-existing (`8c504bc3`), OUT OF SCOPE view-only 392 → spec reload halaman antara fase validasi & create (bukan masking). **Scope-lock D-08:** `git diff --quiet -- Controllers/WorkerController.cs Models/ManageUserViewModel.cs Views/Admin/EditWorker.cshtml` = `ZERO_DIFF_OK`; `dotnet build` 0 error; `dotnet test --filter Category!=Integration` **347/347 GREEN** (no regression, baseline sama Phase 387); **0 migration**. Commit `840fab21` (test). **PHASE 392 SELESAI** (2/2 plans; WRKR-01/02/03 closed: Plan 01 `0d788e8a` view + Plan 02 `840fab21` e2e). **Handoff:** push `main` + notify IT flag migration=FALSE. ❌ JANGAN edit Dev/Prod. **Lesson:** verifikasi e2e perubahan VIEW pada app tanpa runtime-compilation WAJIB build+run dari working tree yang benar (binary stale/sibling diam-diam membatalkan verifikasi runtime).
- [v32.0 / phasing]: 2 fitur file-disjoint & independen (1.1 `AssessmentAdminController.cs` BULK ASSIGN; 1.2 `Views/Admin/CreateWorker.cshtml` view-only) → split alami 2 fase (391 + 392), boleh paralel. Konteks: `AssessmentSession` = per-peserta (tambah peserta = INSERT sesi baru, BUKAN tabel join). `/Admin/CreateWorker` = buat akun pegawai (`ApplicationUser`), BUKAN peserta assessment. PART-02 fix = jangan biarkan guard `Completed` (L1992) salah-blokir penambahan saat grup masih aktif/window terbuka (BUKAN hapus guard total — hard-block penambahan saat InProgress = OUT, keputusan user fleksibel). PART-03 = notice informatif ganti `TempData["Warning"]` kosmetik. 0 migration.
- [v31.0 / 387-04 verifikasi proporsional D-09 — PHASE 387 CLOSE]: 3 jalur verifikasi sesuai kedalaman tiap fix (unit untuk logic, Playwright untuk a11y render runtime, manual untuk SignalR/cert/timer LOW tanpa harness). **Task 1** `HcPortal.Tests/PostLisensorPolishTests.cs` (baru) — fixture disposable `HcPortalDB_Test_{guid}` (`IAsyncLifetime` MigrateAsync→EnsureDeletedAsync, `[Trait Category=Integration]` di-exclude fast suite, `HcPortalDB_Dev` TAK disentuh — T-387-09 mitigasi), logika guard/cell direplikasi data-level PERSIS seperti controller (pola SubmitResurrectionTests): PXF-06 guard status (`S.Completed` reject / `S.PendingGrading` allow EssayScore), PXF-09 essay cell (`"Skor: X/Y"` / `"Tidak dijawab"` / `"Belum dinilai"`), PXF-12 `answers.ContainsKey` absent→PackageOptionId unchanged. 8 facts positive+negative → **8/8 PASS**. **Task 2** `tests/e2e/aria-opsi-387.spec.ts` (baru) — assert `aria-label` berisi "opsi A" RUNTIME di Results + ExamSummary (D-09 MANDATORY: a11y Razor dinamis, grep+build INSUFFICIENT — lesson Phase 354) → **3/3 PASS** `--workers=1`. **Task 3 checkpoint human-verify — APPROVED** (T-387-10 mitigasi: 3 LOW tanpa harness verified manual): browser+SignalR+DB localhost:5277 AD-off shared-memory SQL; snapshot→mutate→RESTORE `C:\Temp .bak` per CLAUDE.md Seed Workflow, `docs/SEED_JOURNAL.md` CLEANED 0 residue. **PXF-08** finalize sesi 169 "TEST E2E Campur 2026-06-15" (essay 10/10, GenerateCertificate=1, IsPassed) → `NomorSertifikat`="KPB/005/VI/2026" ter-assign (retry-loop generate+persist), session→Completed, no certError saat sukses (dikonfirmasi DB 2× finalize). **PXF-10** klien SignalR `/hubs/assessment` JoinMonitor batchKey tepat "TEST E2E Campur 2026-06-15|OJT|2026-06-15" → grup monitor terima `workerSubmitted` live `{sessionId:169, workerName:"Admin KPB", score:100, result:"Pass", status:"Completed"}` tanpa refresh (percobaan-1 meleset hanya karena Title ber-suffix tanggal → re-join key tepat tertangkap bersih). **PXF-13** A/B `SaveTextAnswer` sesi admin-owned: (A) StartedAt=2020+Dur=1min EXPIRED→tulis DITOLAK TextAnswer UNCHANGED; (B) StartedAt=now+Dur=60min NOT expired→tulis SUKSES — guard mirror verbatim `SaveMultipleAnswer`. Semua mutasi (sesi 169 + responses) di-RESTORE; SEED_JOURNAL CLEANED. **PXF-06 fact=guard status** menyelaraskan redirect 387-01 (WR-01/WR-02 sudah cover build+grep di 387-01). Verif fase: fast suite **347/347 GREEN** + build 0 error + 0 migration; `HcPortalDB_Dev` untouched. Commits `46bd422d` (Task1 unit) + `3b4db3a2` (Task2 Playwright); Task 3 verify-only. **No deviation.** **PHASE 387 SELESAI** (4/4 plans; 7 REQ PXF-06/08/09/10/11/12/13 closed; 0 migration). **Handoff:** deploy IT KEDUA pasca-acara terpisah dari bundle 385+386 — gabung → push `origin/ITHandoff` (BUKAN sekarang, keputusan developer) → notify IT flag migration=FALSE. ❌ JANGAN edit Dev/Prod.
- [v31.0 / 387-03 PXF-11 aria opsi huruf A/B/C/D]: a11y polish 2 surface review (file-disjoint dari Plan 01/02, wave 1, no overlap `AssessmentAdminController.cs`/`CMPController.cs`/`AssessmentHub.cs`). Kedua loop opsi yang sebelumnya `@foreach` tanpa index var diubah ke indexed `@for` agar bisa derive huruf, lalu satu-satunya perubahan markup lain = string `AriaContext` partial `_QuestionImage`. **Results.cshtml** (Options List ~L356-391): `@foreach (var option in question.Options)` → `@{ string[] letters = { "A","B","C","D" }; } @for (int oi = 0; oi < question.Options.Count; oi++) { var option = question.Options[oi]; var letter = oi < letters.Length ? letters[oi] : (oi + 1).ToString(); ... AriaContext = "opsi " + letter }`. Markup per-opsi (itemClass success/danger, icon check/x, OptionText span, `(Jawaban Anda)`/`(Jawaban Benar)` label) + blok essay-fallback `!question.Options.Any() && !IsNullOrEmpty(UserAnswer)` UTUH. **ExamSummary.cshtml** (~L57-63): `@foreach (var optImg in item.OptionImages)` → indexed `@for` + `var optImg = item.OptionImages[oi]` + letter; `AriaContext = "opsi " + letter`. Partial gambar-SOAL `Cap = 240` (L56, no AriaContext) TIDAK disentuh (git-diff verified — hanya context line). **Pilihan `for` vs `Select((o,i))`:** kedua koleksi adalah `List<T>` (`question.Options`=`List<OptionReviewItem>`, `item.OptionImages`=`List<ExamSummaryOptionItem>`) → indexable `[oi]`, jadi pakai indexed-`for` (TIRU TEPAT `StartExam.cshtml:125/134/148`, diff lebih kecil dari projection). **`letters` scope:** grep konfirmasi tak ada deklarasi `string[] letters` sebelumnya di kedua view → deklarasi 1× per loop aman (no Razor duplicate-var error). `_QuestionImage` baca `AriaContext` via reflection + render NOTHING saat `ImagePath` null → huruf hanya muncul pada opsi yang punya gambar. Verif: `dotnet build HcPortal.csproj` 0 error per task + grep acceptance PASS + `git diff` ExamSummary konfirmasi Cap=240 partial unchanged; 0 migration; no deletion. Commits `77f0f57f` (Task1 Results) + `5cef4e81` (Task2 ExamSummary). **D-09 MANDATORY (defer Plan 04):** PXF-11 butuh Playwright runtime assert `aria-label` berisi huruf di KEDUA surface (a11y render = Razor dinamis, grep+build INSUFFICIENT — lesson Phase 354). **PXF-11 code-complete; 0 migration.** Next: 387-04 (test/e2e PXF-11 + PXF-12).
- [v31.0 / 387-02 PXF-12 + PXF-13 participant write guards]: 2 server-authoritative guard di file disjoint dari Plan 01/03 (CMPController.cs + AssessmentHub.cs; tak ada overlap dgn AssessmentAdminController.cs Plan 01). **PXF-12** (`Controllers/CMPController.cs` `SubmitExam`, cabang MC upsert ~L1712): bungkus `existingResponse.PackageOptionId = selectedOptId; existingResponse.SubmittedAt = DateTime.UtcNow;` dengan guard `if (answers.ContainsKey(q.Id))` — reuse self-analog L1703 (di sana bentuk ternary `answers.ContainsKey(q.Id) ? ... : null`, jadi pattern `if (answers.ContainsKey(q.Id))` match TEPAT 1× = guard baru). Efek: soal MC absent dari form submit (mis. form parsial / JS gagal) TIDAK lagi null-overwrite jawaban yang sudah ter-autosave via SignalR (T-387-05 Tampering/DoS-by-dataloss). Cabang MA (`MultipleAnswer` L1728) + `else if (selectedOptId.HasValue)` add-block + Essay manual = untouch. **PXF-13** (`Hubs/AssessmentHub.cs` `SaveTextAnswer` ~L151): sisip guard timer-expiry VERBATIM dari sibling `SaveMultipleAnswer:205-215` SETELAH blok `session == null` SEBELUM truncate/upsert — `if (session.StartedAt.HasValue && session.DurationMinutes > 0) { elapsed = (UtcNow - StartedAt).TotalSeconds; allowed = (DurationMinutes + (ExtraTimeMinutes ?? 0)) * 60; if (elapsed > allowed) { LogWarning("SaveTextAnswer: timer expired for session {SessionId}", sessionId); return; } }`. Satu-satunya beda vs analog = log string `SaveMultipleAnswer`→`SaveTextAnswer` (param nama `sessionId` sama di kedua method → port tanpa rename). Efek: tulis essay pasca-timer ditolak + dicatat (T-387-06 Tampering / T-387-07 Repudiation), memperhitungkan ExtraTimeMinutes konsisten dgn MA. Literal `"InProgress"` query L144 TAK diubah (out of scope, konsisten SaveMultipleAnswer:198). `_logger`/`StartedAt`/`DurationMinutes`/`ExtraTimeMinutes` sudah in-scope (session full-entity via FirstOrDefaultAsync L143). Verif: `dotnet build` 0 error + fast suite `dotnet test --filter Category!=Integration` **347/347 GREEN** (no regression) + grep acceptance + `git diff` konfirmasi MA branch unchanged & `"InProgress"` unchanged; 0 migration; no file deletion. Commits `b457f57c` (PXF-12) + `0cd566ae` (PXF-13). **PXF-12 + PXF-13 closed (code-level); 0 migration.** Next: 387-03 (view aria PXF-11) + 387-04 (test PXF-12).
- [v31.0 / 386 ringkas]: PXF-02 helper `QuestionOptionValidator.ValidateQuestionOptions` di-wire ke CreateQuestion+EditQuestion (≥2 opsi ber-teks, ≥1 benar ber-teks; pesan LOCKED) + PXF-04 predikat pending essay TUNGGAL `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` di 4 surface byte-identik + SubmitEssayScore defensive upsert + status-guard PendingGrading (whitespace eval IN-MEMORY krn SQL Server TRIM tak trim tab/newline) + PXF-05 PDF/Excel MA all-or-nothing via shared `IsQuestionCorrect`+`BuildAnswerCell` (kill-drift; Excel essay label unify ke `>0`). Suite 474/474 + e2e 3/3 + UAT 4/4. 0 migration.
- [v30.0 / ECG-01..06]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar, `=0`=Salah, `null`=pending) dipakai di `CMPController.Results` 4 site + PDF export (kill-drift). MA non-empty guard `selected.Count>0 && SetEquals` (display-path, beda dari scoring `Compute`). Regression lock Simpan/Selesaikan essay tanpa ubah kode produksi.
- [v29.0 / 382 / D-01-IMPACT]: SAVE-01 dedupe last-write-wins in-memory, **NO migration**. v29.0 + v30.0 + v31.0 = 0 migration baru.
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen default ON; engine pure `Helpers/ShuffleEngine.cs`.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route].

### Open Blockers/Concerns

- **F-09 (PXF-01) — verifier confirmed HARD di Dev (404, prefix drop) 2026-06-15; UAT browser Dev full lifecycle PASS 2026-06-16** (gambar sub-path `/KPB-PortalHC` 8 img 200, prefix ok — UAT gambar sub-path CLOSED). Carry historis (resolved).
- [push] Carry migration lama (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag. v28/v29/v30/v31/**v32 = 0 migration baru.**
- **v32.0** — tidak ada open blocker baru; root cause & file ter-peta dari investigasi 2026-06-17 (adversarial-verified). Risiko utama: PART-02 (jangan over-relax guard `Completed` sampai memecah logika edit-shared-field), WRKR-03 (cascade Bagian→Unit JS + create submission wajib Playwright runtime — grep+build tak cukup, lesson Phase 354).

## Session Continuity

Last activity: 2026-06-17

Stopped at: Completed 392-02-PLAN.md

Next action: **`/gsd-plan-phase 391`** (Penambahan Peserta Fleksibel saat Ujian Berjalan) — atau `/gsd-discuss-phase 391`. Phase 392 (Perbaikan CreateWorker) bisa di-plan paralel (file-disjoint). Tiap fase: verifikasi lokal (`dotnet build` + `dotnet run` localhost:5277 + Playwright) SEBELUM commit → branch main → notify IT (commit hash + flag migration=FALSE). ❌ JANGAN edit DB/kode Dev/Prod (CLAUDE.md Develop Workflow).
