---
gsd_state_version: 1.0
milestone: v32.5
milestone_name: Flexible Add/Remove Participant
status: executing
stopped_at: Phase 410 context gathered
last_updated: "2026-06-21T03:28:59.069Z"
last_activity: 2026-06-21 -- Phase 410 planning complete
progress:
  total_phases: 16
  completed_phases: 1
  total_plans: 4
  completed_plans: 2
  percent: 50
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 409 — data-foundation-re-entry-guards-exclude-removed-query

## Current Position

Phase: 410
Plan: Not started
Status: Ready to execute
Last activity: 2026-06-21 -- Phase 410 planning complete

Milestone **v32.5 Flexible Add/Remove Participant** — add & remove peserta assessment live (Monitoring Detail, AJAX+SignalR), kapan saja (batch belum-progres maupun InProgress). Hapus **hybrid by-state** (belum-mulai→hard-delete; ada-data→soft-remove+arsip). Soft-remove via 3 kolom `RemovedAt/RemovedBy/RemovalReason`, **migration=TRUE** (Phase 409 `AddParticipantRemovalColumns`; 410-413 = migration=FALSE). RBAC Admin+HC penuh. Branch main. Design spec `docs/superpowers/specs/2026-06-19-flexible-add-remove-participant-design.md`.

**Roadmap (5 fase, depends chain 409 → (410 ∥ 411 sequential file-overlap) → 412 → 413):**

- **409** Data Foundation + Re-entry Guards + Exclude-Removed Query (PRMV-03) — **migration=TRUE** `AddParticipantRemovalColumns` 3 kolom nullable + guard `StartExam`/`SubmitExam`/`Hub.JoinBatch` + exclude `RemovedAt!=null` di semua list/count.
- **410** Add-Participant Backend Live (PART-06, PART-07) — `AddParticipantsLive` (window/idempotent/auto UPA/ready-status/Pre-Post pair/Proton reject) + `GetEligibleParticipantsToAdd`. migration=FALSE.
- **411** Remove + Restore Backend Live (PRMV-01, PRMV-04, PRMV-05, PLIV-03) — `RemoveParticipantLive` hybrid + Pre/Post pair-as-unit + `RestoreParticipantLive` + fix stub `DeleteAssessmentPeserta` + audit/RBAC. migration=FALSE.
- **412** Live Monitoring UI + SignalR (PART-05, PRMV-02, PLIV-01, PLIV-02) — kontrol Tambah/Hapus + modal keras + `participantAdded`/`participantRemoved`/`examRemoved` + panel "Peserta Dikeluarkan". migration=FALSE.
- **413** Test + UAT — xUnit integration (`FlexibleParticipant*Tests`) + Playwright e2e live + regression. migration=FALSE.

## Next Action

**Phase 409 SELESAI (2/2 plan).** NEXT: **`/gsd-verify-work 409`** (verify gate: build 0 error + suite 569/569 + 6 test ParticipantRemoval de-tautologis + run @5277 — semua sudah PASS lokal) → lalu `/gsd-plan-phase 410` ∥ `/gsd-plan-phase 411` (koordinasi file-overlap `AssessmentAdminController.cs` → sequential) → 412 (UI+SignalR, depends 410+411) → 413 (test+UAT, depends semua). Verifikasi lokal tiap fase (`dotnet build` + `dotnet test` + `dotnet run` @5277 + Playwright bila UI/SignalR) → branch main → notify IT (Phase 409 = migration=TRUE hash `01cd7dd0` flag `AddParticipantRemovalColumns`; Plan 02 + 410-413 = migration=FALSE).

**Carry Phase 412/413:** (a) escape/encode `RemovalReason` saat render panel "Peserta Dikeluarkan" (T-409-10 XSS-at-render, di-defer dari 409). (b) A2 export/impact (`ExportAssessmentResults`/`BulkExportPdf`/`GetDeleteImpact`) belum di-exclude removed (defer — revisit bila perlu). (c) reuse `CMPController.IsParticipantRemoved` seam + invarian `RemovedAt==null` di 410/411. (d) jangan regresi 6 test ParticipantRemoval.

## Accumulated Context (carry)

- **v32.2 CLOSED (NOT PUSHED):** `git push origin main` (~207 commit ahead) + `git push origin v32.2` (tag) saat koordinasi deploy IT. v32.2 migration=FALSE — TAPI v32.5 tambah **migration=TRUE** (3 kolom AssessmentSession, Phase 409) → notify IT saat bundle deploy.
- **Carry-migration IT lama** pending notify: 360 PendingProtonBypass + 372 ShuffleToggles.
- **v32.0 (391+392)** close manual opsional non-blocking — sudah archived + tag lokal `423a2e76`. **Guard Phase 391 (`DeriveReadyStatus`) + tech-debt 398.1 sudah ada di main** → fondasi v32.5 (Phase 410 reuse `DeriveReadyStatus`; Phase 413 jangan regresi guard 391/398.1).
- **v32.3/v32.4** ada di branch ITHandoff (terpisah dari main). v32.5 mulai fase **409** untuk hindari tabrak (v32.3=399-404, v32.4=405-408). v32.4 retake juga sentuh `AssessmentSession` (kolom beda) → koordinasi migration saat bundle deploy.
- **JANGAN tarik ITHandoff→main tanpa cherry-pick guard** (ITHandoff kehilangan guard Phase 391/398.1 — spec §Branch & Deploy).
- ❌ Tidak ada edit kode/DB Dev/Prod (promosi = IT).

---

_(Histori Plan 02 — Wave 1 GREEN, arsip)_

**Phase 397 Plan 02 SELESAI (Wave 1 GREEN — implementasi service INJ-12)** — 3 commits: `af28e9db` feat (Task 1 per-worker bidirectional + Kasus A/B + write-to-online) + `a5c3b050` feat (Task 2 anti-double preflight D-08 + `PreviewPairingAsync` D-07 dry-run) + `e474dda5` feat (Task 3 `UnlinkInjectGroupAsync` D-12 atomic revert). SUMMARY @ `.planning/phases/397-link-pre-post-ke-room-existing/397-02-SUMMARY.md`. **Hanya `Services/InjectAssessmentService.cs` (+343/-2); 0 migration** (no `Migrations/`/`Data/` diff). `dotnet build HcPortal.csproj` **0 error**; 5 Wave-0 397 suite **GREEN 15/15** (`InjectLink` 4 + `AntiDoubleLink` 1 + `PreviewPairing` 4 + `CrossGrouping` 3 KRITIS §13 + `UnlinkInject` 3; real SQLEXPRESS, `HcPortalDB_Dev` untouched) + fast suite **389/389 GREEN** (no regression 395/396). **Yang dibangun:** (1) ganti broadcast `:120` → resolusi sibling **by-UserId** per-pekerja SETELAH SaveChanges (D-02) + write-back bidirectional online. (2) `ResolveLinkContextAsync` (privat, **server re-resolve dari `req.LinkTargetRepId`**, T-397-06; sumber tunggal Kasus A/B → preview==commit): Kasus A adopt `rep.LinkedGroupId` tak-sentuh-online / Kasus B `resolvedGroupId=rep.Id`(RepresentativeId) tulis stiker `LinkedGroupId` ke **SEMUA** sesi room target (Pitfall 2; key **Title+Category+Schedule.Date** — LOCKED, WAJIB cocok picker Plan 03). (3) audit `"LinkPrePost"` per sesi online dimutasi (D-09) gated `!IsManualEntry` ⇒ inject↔inject 0 audit (D-10); `mutatedOnlineSessionIds` HashSet dedup paired-sticker. (4) anti-double D-08 di `PreflightValidateAsync` (daftar lengkap, masuk reject-all path, pesan BI memuat NIP). (5) `PreviewPairingAsync(int?,string,IReadOnlyList<string>,DateTime)→InjectPairingPreview` dry-run **NO write** (Paired/Unpaired/WillTouchOnline/DateWarn skip-when-null Open Q 2/DoubleLinkErrors). (6) `UnlinkInjectGroupAsync(int,string,string)→InjectResult` atomic revert bidirectional + Kasus B stiker via heuristik single-type (Open Q 1 opt-b) + audit `"LinkPrePostUndo"`; IDOR guard load `IsManualEntry`; bogus group → `Success=false` state utuh. **Skor/status/jawaban online TAK disentuh** (T-397-04). **1 deviasi Rule-3** (non-scope): helper `ResolveLinkContextAsync` di-add di Task 1 (bukan Task 2) demi compile-order — Task 1 inline sudah memanggilnya, jadi "refactor to call helper" Task 2 = no-op. Audit ActionType: `LinkPrePost`(11)/`LinkPrePostUndo`(15) ≤ MaxLength(50). Branch main; notify IT migration=FALSE.

**Kontrak terkunci (Wave 1 WAJIB sediakan):** (1) `UnlinkInjectGroupAsync(int injectGroupId, string actorUserId, string actorName)→Task<InjectResult>` BARU (D-12; symbol genuinely-missing penggerak RED). (2) `PreviewPairingAsync(int? linkTargetRepId, string injectAssessmentType, IReadOnlyList<string> injectUserIds, DateTime injectCompletedAt)→Task<InjectPairingPreview>` BARU (D-07 seam). (3) Resolusi link per-pekerja di `InjectBatchAsync` digerakkan `req.LinkTargetRepId` (signature TAK berubah): resolve `LinkedSessionId` by-UserId (ganti broadcast `:120`), Kasus A adopt / Kasus B tulis-ke-SEMUA-sesi-target + write-back bidirectional + audit `"LinkPrePost"`; anti-double preflight (D-08 daftar lengkap); rollback total bila error; audit `"LinkPrePostUndo"` saat unlink. **Invarian KRITIS** (spec §13): display pasang by `LinkedGroupId`+`UserId` (CMPController.cs:3417-3433) → `LinkedGroupId` WAJIB benar; test `InjectCrossGroupingTests` membuktikan pasangan silang inject↔online tampil via query GetGainScoreData-equivalent.

**Kunci implementasi (Plan 04):** **N1 toggle** `name="step5Method"` (Isi via Form default / Import Excel) — pilih Excel ⇒ sembunyikan SELURUH `#step5FormPath` (roster form 395) + tampilkan `#step5ExcelPanel` (mutually exclusive D-03); hidden bound `#Step5Method` posting pilihan ke server. **N2** Download Template = **hidden-form POST** (`document.createElement('form')` + `data-download-url=@Url.Action(DownloadInjectTemplate)` bawa `#QuestionsJson`+`UserIds`+antiforgery ⇒ unduhan `.xlsx` NYATA, BUKAN fetch-blob; Playwright assert download event) + file picker `.xlsx/.xls` + Upload & Pratinjau (fetch FormData → `UploadInjectExcel`). **N3** tabel preview (NIP/Nama, Skor Final %, badge Lulus/Tidak Lulus, Soal Terjawab) via `.textContent` (XSS-safe T-396-09), **NO cert# di preview** (gate D-08). **N4** upload invalid render **DAFTAR error LENGKAP** (bukan stop-at-first) `.alert-danger` + sembunyikan preview + reset `injExcelAnswersCache='[]'` (atomic NO-commit D-09); sel kosong = warn-but-allow (D-06). **Gate commit (T-396-10):** submit listener isi `#AnswersJson` dari `window.injExcelAnswersCache` HANYA setelah upload 0-error; invalid ⇒ cache `'[]'` ⇒ commit hasilkan kosong (preview = gate). **D-05:** `req.EssayTextRequired=false` HANYA saat `vm.Step5Method=="excel"` (jalur form tetap default true). Commit jalur SAMA `#btnInject`→`MapToRequest`→`InjectBatchAsync` (byte-identik online), NOL cabang grading baru. **e2e .xlsx authoring:** pakai `exceljs` (devDep) bangun file upload FRESH (exceljs.readFile incompat output ClosedXML); parser baca sheet `"Jawaban"` by-name + posisi-kolom ⇒ file fresh setara diterima.

**UAT live (checkpoint) — 5/5 PASS:** (N1) toggle saling-eksklusif teks BI benar. (N2) download `inject_template.xlsx` 2 sheet (`Jawaban` matrix prefilled per-worker + `Legenda` No|Teks|Tipe|Skor Maks|Opsi). (N3) upload valid → preview Rino 100% Lulus 2/2 NO-cert# → commit → DB sesi #173 Score=100 IsPassed=1 IsManualEntry=1 Completed, 2 responses, AuditLog ManualInject=1, cert KPB/005/VI/2026 auto, `/CMP/Results/173` per-soal benar + `/CMP/Certificate/173` downloadable; **essay skor-tanpa-teks DITERIMA = D-05 confirmed**; anti silent-grade-0 (100 bukan 0). (D-09) upload invalid (MC "E" + essay 99>max + NIP asing) → alert "Perbaiki kesalahan berikut…" 3-item LENGKAP, preview hidden, cache `"[]"`, DB 0 sesi (rollback atomic). (D-06) sel kosong (MC empty + Essay 10) → warn "1 jawaban kosong…dihitung 0" + preview Rino 50% Tidak Lulus 1/2 (warn-but-allow). DB di-RESTORE, SEED_JOURNAL cleaned.

**⚠️ TEMUAN KOSMETIK non-blocking (catat, JANGAN fix di 396):** sheet `Legenda` kolom `Tipe` tampilkan enum internal `"MultipleChoice"` untuk soal ber-label UI `"Single Answer"` (LBL-02). HC lihat `"MultipleChoice"` di legenda, bukan `"Single Answer"`. Teks referensi (legenda, bukan sel data) — kandidat polish 1-baris (map enum→label UI di `InjectExcelHelper.GenerateTemplate` Legenda); bundling ke Phase 398 atau backlog, bukan blocker 396.

## Tag Git

- `v24.0`, `v25.0`, `v26.0`, `v27.0`, `v28.0` — ✅ PUSHED ke `origin/ITHandoff` 2026-06-14.
- `v29.0` — ✅ PUSHED `origin/ITHandoff` 2026-06-15.
- `v30.0` — ✅ PUSHED `origin/ITHandoff` 2026-06-15 (HEAD `fe8c5ffe`).
- `v31.0` — ✅ shipped local + MERGED origin/main 2026-06-16 (merge `7ea6c81e`; branch ITHandoff HEAD `64456bd5`).
- `v32.0` — phases 391+392 COMPLETE local, closed manual (tag lokal `423a2e76`).
- `v32.2` — shipped local + audited PASSED + closed 2026-06-19 (Inject Hasil Assessment Manual); NOT PUSHED (branch main).
- `v32.5` — milestone aktif (Flexible Add/Remove Participant); roadmap created (5 fase 409-413); belum di-plan.

## Deferred Items

> 📌 **Acknowledged @ v32.2 close (2026-06-19):** pre-close audit-open surfaced 60+ open artifact — SEMUA pre-existing / v32.0 / backlog-lama, BUKAN v32.2 (v32.2 sendiri bersih, audit PASSED). Di-acknowledge + deferred:
> - **391/392 UAT + VERIFICATION gaps** (`391-HUMAN-UAT` resolved, `392-HUMAN-UAT` 1-pending, 391/392 VERIFICATION human_needed) = **v32.0** (milestone deferred, di-close manual terpisah — bukan v32.2).
> - **14 debug session** (276-psrt03, kkj-*, monitoring, dll, [diagnosed]/[investigating]) = pre-existing, tak terkait v32.2.
> - **46 quick-task [missing]** = backlog project-wide lama (file artifact hilang) — acknowledged-deferred.
> - 999.x backlog (6/9/10 functional-closed; 11/12/13 RESOLVED oleh 398.1) — dir diarsip ke `milestones/v32.2-phases/`.
> Tidak ada satupun blocker v32.2. Buka lagi hanya bila muncul bug/kebutuhan nyata.

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
| 999.11 WR-01 PendingGrading edit-guard gap EditAssessment | LOW pre-existing (off-theme v32.5; pertimbangkan bundle bila 411 sentuh guard sama) |
| 999.12 regression test 391 → WebApplicationFactory | MED test-infra (off-theme; pertimbangkan bundle ke 413 `FlexibleParticipant*Tests`) |
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |
| 999.6 impersonate identity (dir tersisa) | sudah ditutup fungsional v28.0/377; dir backlog tinggal |
| 999.10 route CMP (dir tersisa) | sudah ditutup v28.0/378; dir backlog tinggal |
| 999.13 e2e essay-submit helper flaky (`fillEssayAnswer`/`submitExamTwoStep` → exam-types FLOW L + exam-taking Flow K "belum dijawab") | pre-existing test-infra, NON-inject (393-397 nol ubah Views/CMP+AssessmentHub.cs) & NON-defect produk (essay-flush-385 3/3 + 557 xUnit PASS); jalur DIRECT `hub.invoke('SaveTextAnswer')` tak konsisten vs jalur produk `flushEssay`. Perbaiki helper (bukan kode produk). Ditemukan Phase 398 D-05 ii |
| 43 quick-task todo (audit-open, semua status `[missing]`) | acknowledged deferred (backlog project-wide lama, todo file ada artifact hilang) |

### Push IT

| Item | Status |
|------|--------|
| Push v29.0 + v30.0 ke `origin/ITHandoff` (branch + tag) | ✅ PUSHED 2026-06-15, HEAD `fe8c5ffe` |
| Notify IT — 2 migration carry (`PendingProtonBypass`+index/360, `ShuffleToggles`/372). **v29.0 + v30.0 + v31.0 + v32.0 + v32.2 = 0 migration baru; v32.5 Phase 409 = migration=TRUE (`AddParticipantRemovalColumns`).** | ⏳ PENDING — kasih commit hash + flag ke IT |
| Push v32.2 + v32.5 ke `origin/main` (bundle deploy) | ⏳ pending koordinasi IT (v32.5 Phase 409 = migration=TRUE → flag IT) |

## Accumulated Context

### Roadmap Evolution

- **v32.5 roadmap dibuat 2026-06-19** — Phases 409-413, 11 REQ (PART-05/06/07 + PRMV-01..05 + PLIV-01/02/03). Penomoran mulai **409** (BUKAN lanjut 398 main) — pilihan eksplisit user agar tak tabrak v32.3 (399-404) + v32.4 (405-408) di branch ITHandoff saat merge antar-branch. 5 fase by arsitektur spec (A-H): **409** fondasi data+guard+exclude-query (migration=TRUE `AddParticipantRemovalColumns`, PRMV-03) → **410** add backend (PART-06/07) ∥ **411** remove/restore backend (PRMV-01/04/05 + PLIV-03) [file-overlap `AssessmentAdminController.cs` → sequential] → **412** UI+SignalR live (PART-05 + PRMV-02 + PLIV-01/02) → **413** test+UAT. Spec-driven (skip domain-research; audit kode 4-agen sudah peta file:line). Branch main. Scope OUT: Proton add/remove, self-service, bulk-import-live, force-disconnect, notif peserta, migration selain 3 kolom removal. migration=TRUE hanya 409; 410-413 = FALSE.
- **Phase 398.1 disisipkan 2026-06-19** setelah Phase 398 (desimal, INSERTED) — Tech-debt cleanup INJ (v32.2): tutup 9 temuan code-review + carry (396 WR-01/02/03, 397 WR-01/02/03, 999.11/12/13) + cosmetic Legenda LBL-02, SEBELUM `/gsd-complete-milestone v32.2`. Desimal 398.1 dipilih (BUKAN 399 — 399-404 dipakai v32.3 di branch ITHandoff, hindari konflik merge). Branch main, migration=false. ⚠️ verifikasi tiap warning real vs false-positive dulu (receiving-code-review). v32.2 tetap 13/13 REQ (tak nambah REQ INJ).
- **v32.2 milestone dimulai 2026-06-17** — Inject Hasil Assessment Manual ("Seakan Online"), 6 fase (393-398, LANJUT dari 392; tidak reset). Sumber: brainstorm + design spec `docs/superpowers/specs/2026-06-17-inject-assessment-manual-design.md`. Skip research (sudah research codebase saat brainstorm). Keputusan kunci: reuse mesin existing (authoring + GradingService + CertNumberHelper, nol duplikasi); standalone-alur tapi reuse-kode; sertifikat toggle per-room (auto/manual/tanpa); link Pre/Post silang inject↔online; retire/absorb BulkBackfill; 0 migration. v32.0 (391/392) TIDAK dihapus dir-nya (lanjut tanpa `phases clear`, atas keputusan user). Requirements + roadmap menyusul.
- **v32.0 roadmap dibuat 2026-06-17** — Phases 391-392, 7 REQ (PART-01..04 + WRKR-01..03). Penomoran LANJUT dari v31.0 phase terakhir (387) → mulai 391 (tidak reset). Phasing by file-overlap (split alami 2 fase, fitur file-disjoint & independen): PART-01..04 (`AssessmentAdminController.cs` + view/monitoring + test) → 391; WRKR-01..03 (`Views/Admin/CreateWorker.cshtml` view-only) → 392. 0 migration (kedua fase). Out of scope: hard-block penambahan saat InProgress, perubahan controller/model CreateWorker, AD-sync, migration.
- **v31.0 roadmap dibuat 2026-06-15** — Phases 385-386, 5 PXF (PXF-01..05). Penomoran LANJUT dari v30.0 (384). Phasing by file-overlap: PXF-01+PXF-03 (file view) → 385; PXF-02+PXF-04+PXF-05 (semua `AssessmentAdminController.cs`) → 386 (gabung hindari konflik write paralel). 0 migration. (Phase 387 ditambah pasca-acara: 7 REQ polish PXF-06..13.)
- Phase 385 sempat DIBATALKAN konteks-lama (2026-06-15): readiness ujian = verifikasi browser/UAT. **Catatan:** angka "385" kini DIPAKAI ULANG sebagai phase v31.0 Exam-Taking & Image Render Hotfix (build kode nyata, bukan verifikasi-only). Scope readiness asli tetap hidup di `.planning/notes/2026-06-15-readiness-ujian-lisensor.md`; 5 must-fix-nya jadi PXF-01..05.

### Decisions (persist across milestones)

- [v32.5 / 409-02 guard re-entry + exclude-removed]: **PRMV-03 delivered + PLIV-01 foundation, migration=FALSE.** 3 commits — `cf7838b5` (test scaffold de-tautologis Wave-0) + `a0afd785` (guard) + `2baf7402` (exclude). **Seam tunggal** `CMPController.IsParticipantRemoved(session) => session.RemovedAt != null` (cermin `IsResultsAuthorized`; deteksi removed via `RemovedAt`, BUKAN Status — akar D-04). **Guard:** `StartExam` (SEBELUM auto-transition Upcoming→Open & mark-InProgress) + `SubmitExam` (SEBELUM `ShouldGateMissingStart`/grading) → redirect "Assessment" + `TempData["Error"]="Anda telah dikeluarkan dari ujian ini."` (locked verbatim, tepat 2x). **Hub (silent-skip dipertahankan):** `JoinBatch` AnyAsync + `SaveTextAnswer`/`SaveMultipleAnswer` FirstOrDefaultAsync += `&& s.RemovedAt == null` (A1 IN scope = defense-in-depth; 3 occurrence). **Exclude (PLIV-01):** per-surface `.Where(a => a.RemovedAt == null)` di `managementQuery`+`AssessmentMonitoring.query`+`AssessmentMonitoringDetail.query` (3x; count/grouping inherit). **NO EF global `HasQueryFilter`** (FORBIDDEN). **Boundary (D-01a):** `UserAssessmentHistory`+export/impact+`WorkerDataService` UNTOUCHED — riwayat pekerja TETAP tampil removed (sertifikat utuh & reversibel). **6 test de-tautologis** (999.12): exclude/boundary via InMemory real-`AssessmentAdminController`; guard via SQLEXPRESS disposable (helper produksi `IsParticipantRemoved` atas entitas SQL nyata) + EF `AnyAsync` nyata (JoinBatch). Full suite **569/569 GREEN**, build 0 error, run @5277 OK. **Deviasi:** seam `IsParticipantRemoved` (Rule 2 — agar guard testable de-tautologis; proyek tak punya WebApplicationFactory; behavior+pesan locked identik) + test-infra ActionDescriptor/StubUrlHelper/seed-order (Rule 3). **Defer 412:** T-409-10 (XSS-at-render RemovalReason) + A2 (export/impact exclude). SUMMARY `.planning/phases/409-data-foundation-re-entry-guards-exclude-removed-query/409-02-SUMMARY.md`. **Plan 02 migration=FALSE** (no Migrations/Data diff).
- [v32.5 / 409-01 fondasi skema soft-remove]: **migration=TRUE applied lokal.** 2 commits — `3806e7b9` (model 3 props `RemovedAt DateTime?`/`RemovedBy string?`/`RemovalReason string?` nullable, NO `[MaxLength]` annotation, cermin `CreatedBy`/`CompletedAt`; Fluent `RemovalReason.HasMaxLength(500).IsRequired(false)` di block `Entity<AssessmentSession>` D-03; `RemovedBy` plain `string?`→nvarchar(max)) + `01cd7dd0` (migration `AddParticipantRemovalColumns` scaffold via **dotnet-ef 8.0.0** + apply `HcPortalDB_Dev`). **Invarian tunggal:** soft-removed ⇔ `RemovedAt != null`; aktif ⇔ `RemovedAt == null` (BUKAN via Status — soft-remove tak mutasi Status). Migration: 3 `AddColumn` nullable:true tanpa defaultValue (additif non-destruktif), `Down()` simetris 3 `DropColumn`, snapshot ProductVersion 8.0.0. sqlcmd confirm: RemovalReason nvarchar(500) YES, RemovedAt datetime2 YES, RemovedBy nvarchar(max) YES; 60 baris existing `RemovedAt` NULL (NOL backfill). **Tool pinning:** global `dotnet ef` 10.0.3 menolak downgrade ke 8.0.0 → solusi local tool manifest `.config/dotnet-tools.json` (install dotnet-ef 8.0.0, rollForward:false) — mitigasi permanen Pitfall 5/T-409-05 (snapshot stamp 10.x) untuk migrasi future repo (Plan deviation Rule 3, committed bersama migration). Build 0 error. SUMMARY `.planning/phases/409-data-foundation-re-entry-guards-exclude-removed-query/409-01-SUMMARY.md`. **notify IT: hash `01cd7dd0` flag `AddParticipantRemovalColumns`.**
- [v32.5 / phasing + locked]: 6 keputusan spec LOCKED (1 surface live Monitoring Detail; 2 hybrid by-state hapus; 3 konfirmasi-keras+force-kick; 4 add longgar+guard wajar; 5 RBAC Admin+HC penuh + longgarkan `EnsureCanDeleteAsync` HC dgn mitigasi soft-remove+audit+modal; 6 model 3 kolom removal migration=TRUE). Sumber-kebenaran soft-removed = `RemovedAt != null`. `AssessmentSession` per-peserta; batch = `Title+Category+Schedule.Date`; InProgress turunan. DELETE 1-peserta belum ada backend (stub mati `DeleteAssessmentPeserta` `EditAssessment.cshtml:666`). Pre/Post diperlakukan pasangan-sebagai-satu-unit (add buat pair, remove keduanya konsisten hard/soft). SignalR broadcast HANYA setelah `CommitAsync` sukses. Proton (`Category=="Assessment Proton"`) ditolak semua endpoint. Reuse: `DeriveReadyStatus` (391), `RecordCascadeDeleteService` (hard-delete single-root), pola atomic `InjectAssessmentService`, partner-handling `DeletePrePostGroup`, pola `examClosed`/`AkhiriUjian` (force-kick).
- [v32.2 / 397-03 INJ-12 Wave 2 controller/preview wiring]: `Controllers/InjectAssessmentController.cs` (+147) + `HcPortal.Tests/InjectViewModelMapTests.cs` (+1 unit) — expose seam service Wave 1 ke HTTP surface, scope-lock controller-only. `SearchLinkTargets` GET picker JSON room tipe-LAWAN (TIDAK filter IsManualEntry D-10); `MapToRequest` set HANYA `req.LinkTargetRepId`; `PreviewPairing` POST companion; `UnlinkInjectGroup` POST. Kasus B key picker standalone WAJIB == `ResolveLinkContextAsync` write-to-all key (Title+Category+Schedule.Date). 0 migration.
- [v32.0 / phasing]: 2 fitur file-disjoint & independen (1.1 `AssessmentAdminController.cs` BULK ASSIGN; 1.2 `Views/Admin/CreateWorker.cshtml` view-only) → split alami 2 fase (391 + 392). `AssessmentSession` = per-peserta (tambah peserta = INSERT sesi baru, BUKAN tabel join). PART-02 fix = jangan biarkan guard `Completed` (L1992) salah-blokir penambahan saat grup masih aktif/window terbuka. PART-03 = notice informatif ganti `TempData["Warning"]` kosmetik. 0 migration.
- [v31.0 / 386 ringkas]: PXF-02 helper `QuestionOptionValidator.ValidateQuestionOptions` di-wire ke CreateQuestion+EditQuestion + PXF-04 predikat pending essay TUNGGAL `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` di 4 surface + SubmitEssayScore defensive upsert + status-guard PendingGrading + PXF-05 PDF/Excel MA all-or-nothing via shared `IsQuestionCorrect`+`BuildAnswerCell`. 0 migration.
- [v30.0 / ECG-01..06]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar, `=0`=Salah, `null`=pending) dipakai di `CMPController.Results` 4 site + PDF export (kill-drift).
- [v29.0 / 382 / D-01-IMPACT]: SAVE-01 dedupe last-write-wins in-memory, **NO migration**. v29.0 + v30.0 + v31.0 + v32.0 + v32.2 = 0 migration baru; v32.5 Phase 409 = migration=TRUE.
- [v27.0 / SHUF]: Shuffle Toggle 2 sistem independen default ON; engine pure `Helpers/ShuffleEngine.cs`.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` = single source of truth label lintas 11+ surface.
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route].

### Open Blockers/Concerns

- **F-09 (PXF-01) — verifier confirmed HARD di Dev (404, prefix drop) 2026-06-15; UAT browser Dev full lifecycle PASS 2026-06-16** (gambar sub-path `/KPB-PortalHC` 8 img 200, prefix ok — UAT gambar sub-path CLOSED). Carry historis (resolved).
- [push] Carry migration lama (Origin, PendingProtonBypass+index/360, ShuffleToggles/372) — notify IT flag. v28/v29/v30/v31/v32.0/v32.2 = 0 migration baru. **v32.5 Phase 409 = migration=TRUE (`AddParticipantRemovalColumns` 3 kolom nullable additif).**
- **v32.5** — tidak ada open blocker baru; root cause & file ter-peta dari audit kode 4-agen (spec code-verified file:line). Risiko utama: (a) file-overlap Phase 410↔411 di `AssessmentAdminController.cs` → sequential/koordinasi merge; (b) Phase 412 SignalR+force-kick = runtime Razor/JS → Playwright wajib (lesson Phase 354); (c) Phase 409 jangan over-exclude (semua list/count aktif harus exclude `RemovedAt!=null`, tapi panel "Peserta Dikeluarkan" justru tampilkan yang removed); (d) Phase 413 jangan regresi guard 391/398.1; (e) jangan tarik ITHandoff→main tanpa cherry-pick guard 391/398.1.

## Session Continuity

Last activity: 2026-06-21

Stopped at: Phase 410 context gathered

Next action: **`/gsd-verify-work 409`** (Phase 409 SELESAI — build 0 error + suite 569/569 GREEN + 6 test ParticipantRemoval de-tautologis + run @5277, semua sudah PASS lokal). Setelah verify: `/gsd-plan-phase 410` ∥ `/gsd-plan-phase 411` (file-overlap `AssessmentAdminController.cs` → sequential) → 412 (UI+SignalR, depends 410+411) → 413 (test+UAT, depends semua). Verifikasi lokal tiap fase (`dotnet build` + `dotnet test` + `dotnet run` @5277 + Playwright bila UI/SignalR) → branch main → notify IT (Phase 409 migration=TRUE hash `01cd7dd0` flag `AddParticipantRemovalColumns`; Plan 02 + 410-413 = FALSE). ❌ JANGAN edit DB/kode Dev/Prod (CLAUDE.md). ⚠️ JANGAN tarik ITHandoff→main tanpa cherry-pick guard 391/398.1.
