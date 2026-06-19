---
gsd_state_version: 1.0
milestone: v22.0
milestone_name: CMP-06 Residual Fix + CMP/Records + ManageAssessment/Monitoring Audit
status: executing
stopped_at: Phase 403 UI-SPEC approved
last_updated: "2026-06-19T01:09:38.882Z"
last_activity: 2026-06-19
progress:
  total_phases: 30
  completed_phases: 3
  total_plans: 13
  completed_plans: 11
  percent: 85
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 401 — proton-unit-resolution-hardening

## Current Position

Phase: 403
Plan: Not started
Status: Executing Phase 401
Last activity: 2026-06-19

> ⏸ RESUME: see `.planning/phases/401-proton-unit-resolution-hardening/.continue-here.md`
> Next = satisfy BLOCKING human-verify D-01 render checkpoint (Razor; Playwright or user "approved"), then Plan 03 Task 4 (turn-green tests) → 401-03-SUMMARY → verify phase. migration=FALSE. Run gsd-tools from ITHandoff toplevel ONLY.

## Next Action

**`/gsd-verify-work 400`** (Phase 400 = 1/1 plan complete, ready_for_verification). Lalu lanjut Wave-1 paralel {401, 403} (depends 399 done; cluster file disjoint, git worktree). Wave 2 = 402 (setelah 401), Wave 3 = 404. `/clear` dulu (fresh context).

**migration=TRUE notify IT** saat milestone push (`AddUserUnitsTable` `fc015f4d`, Plan 01 — SATU-SATUNYA migration v32.3). Plan 02/03/04 = 0 migration.

**Critical path:** `399 → 401 → 402 → 404`.

**Critical path:** `399 → 401 → 402 → 404`.

Urutan + paralelisme eksekusi v32.3 (spec §6):

- **Wave 0 — Phase 399 (solo, migration=TRUE):** Model `UserUnit` + migration `AddUserUnitsTable` (filtered-unique index primary) + backfill 1 primary-row/pekerja + kontrak write-through primary-mirror (Worker Create/Edit/Import) + UI Bagian-single/Unit-multi-select + display semua unit (Profil/WorkerDetail/Settings/ManageWorkers/Excel/Home/`_PSign`) + Import multi-unit + validasi `Unit ∈ unit-Bagian` + audit set-diff + guard hapus-unit. REQ MU-01/02/03/04/05/07.
- **Wave 1 — Phase 400 + 401 + 403 PARALEL** (cluster file disjoint, depends 399; eksekusi via git worktree terpisah, merge tiap selesai):
  - **400** (MU-06) — listing set-aware `WorkerDataService`/`WorkerController`/CMP-view + rollup dedup. 0 migration.
  - **401** (PSU-01/02/03/04/05/07) — resolusi PROTON `AssignmentUnit` eksplisit (drop fallback `User.Unit`) + filter axis + validasi ∈UserUnits + no-clobber + skip+audit-warn + reactivation guard. File `CoachMapping`/`CDP`/`ProtonData`/`Bypass`/`AssessmentAdmin`. 0 migration.
  - **403** (ORG-01/02) — `OrganizationController` cascade/guard UserUnits-aware + reparent cross-Bagian hard-block + PreviewEditCascade. Terisolasi. 0 migration.
- **Wave 2 — Phase 402 SERIAL setelah 401** (CXU-01..05) — coaching cross-unit: eligible set-aware + server guard ⊆Bagian + AssignmentUnit per-coachee + relax JS lock + self-scope multi-unit coach. Berat di `CoachMapping`+`CDP` (shared dgn 401) + butuh aturan AssignmentUnit dari 401. 0 migration.
- **Wave 3 — Phase 404 setelah semua** (QA-01..04) — test SQL riil (SQLEXPRESS, fixture {X,Y} + coach cross-unit + PROTON T1@X→T2@Y) + invariant single-active + invariant `AssignmentUnit ∈ UserUnits` + B-06 anti-dobel + UAT + docs D1=b. 0 migration.

**Invariant global WAJIB dijaga (spec §7):** (1) Section scalar 1 Bagian/akun (semua `UserUnits.Unit` anak Bagian); (2) PROTON single-active (1 `ProtonTrackAssignment` aktif, index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` dipertahankan); (3) primary mirror `ApplicationUser.Unit`=baris `IsPrimary` (write-through); (4) `AssignmentUnit ∈ coachee.UserUnits` (pasca-401); (5) `ProtonKompetensi.Unit` 1:1 per deliverable. **D1=b** = cert/analytics atribusi primary (no kolom unit-at-issue). **De-risk:** authz Section (`IsResultsAuthorized`+SectionHead L4) 100% scalar → 0 perubahan.

**Verifikasi tiap fase (CLAUDE.md Develop Workflow):** `dotnet build` + `dotnet run` (localhost:5277) + cek DB lokal + Playwright bila ada UI. ❌ tidak ada edit di Dev/Prod. Semua → 1 push → notify IT re-deploy Dev (**migration=TRUE** Phase 399, commit hash).

## Tag Git

- `v24.0`..`v31.0` — ✅ tag dibuat. v29/v30 PUSHED `origin/ITHandoff`; v31.0 MERGED→main + PUSHED `origin/main` 2026-06-16 (merge `7ea6c81e`). v32.1 CLOSED (archive-only, tag lokal, NOT pushed — deploy bareng v32.3).

## Deferred Items

> ✅ **ACCEPTED OK 2026-06-14** (keputusan user): semua carry-over v11.2/v13/v14/v15 di bawah = phase lama, dianggap OK / non-blocking (kode ship + jalan; tak ada bug report v16-v32). Histori, bukan TODO. Buka lagi hanya bila muncul bug/kebutuhan nyata.

### Carry-over ACCEPTED OK (ringkas)

| Item | Status |
|------|--------|
| EPRV-01 Preview Essay rubrik (v15.0) | accepted-OK |
| Phase 303 Coach Workload 12-langkah UAT | accepted-OK (kode ship+jalan) |
| Phase 235 (5 items) + Phase 247 (2 TODO) UAT | accepted-OK |
| Phase 297 Pre-Post Renewal behavior + Phase 298 essay char limit | accepted-OK (undecided, non-blocking) |
| Phase 293 org Level 2+ support | accepted-OK (org 2-level cukup) |
| v11.2 Phase 281 (System Settings) + 285 (Impersonation Page) | accepted-OK (closed-early) |

### Backlog aktif (belum dipromote)

| Item | Reason |
|------|--------|
| 999.9 label residu "Backfill/Restore" di UI BulkBackfill | kosmetik (LOW) |
| 999.11 WR-01 PendingGrading guard (parked dari v32.0) | tech-debt parked |
| 999.12 391 test WebApplicationFactory (parked dari v32.0) | tech-debt parked |
| 43 quick-task todo (audit-open, status `[missing]`) | acknowledged deferred (artifact lama hilang) |

### v2 / future (dari REQUIREMENTS v32.3)

| Item | Reason |
|------|--------|
| Cert/analytics atribusi per-unit akurat (kolom unit-at-issue + backfill) | deferred — D1=b primary; buka bila compliance per-unit butuh (migration ke-2) |
| PROTON paralel (2 track aktif konkuren) | deferred — sekuensial dikonfirmasi; perlu relax unique index + kolom Unit `ProtonTrackAssignment` + re-key ~21 site |

### Push IT

| Item | Status |
|------|--------|
| Notify IT — 2 migration carry lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) | ⏳ PENDING (carry lama) |
| **v32.1** — 0 migration, 0 backend; deploy ditunda (close bareng v32.3) | ⏳ pending (bundle dgn v32.3) |
| **v32.3** — **migration=TRUE** (`AddUserUnitsTable` Phase 399 + backfill); 1 push → notify IT migration=TRUE (commit hash) | ⏳ pending (roadmap dibuat, belum di-plan/execute) |

## Accumulated Context

### Decisions (persist across milestones)

- [400-01 / MU-06 set-aware listing SHIPPED — Phase 400 COMPLETE]: filter unit listing pekerja diubah dari scalar `u.Unit==unitFilter` (primary-only) → **set-aware correlated EXISTS** `_context.UserUnits.Any(uu=>uu.UserId==u.Id && uu.Unit==unitFilter && uu.IsActive)` di **3 lokasi** (`WorkerDataService.GetWorkersInSection` + `WorkerController.ManageWorkers` + `ExportWorkers`). Pekerja {X,Y} 1-Bagian muncul saat difilter **tiap** unit (termasuk non-primary); **dedup by-construction** (`.Any()` boolean subquery = 1 row/user, no fan-out — **TANPA `.Distinct()`**). Kolom **Unit kontekstual D-02** (filtered→matched unit / unfiltered→all-active primary-first comma-join `string.Join(", ", uList)` / 0-unit→fallback `user.Unit` D-05); **batch-load dict `unitsByUser` D-04** (1 query active-only primary-first, no N+1). **PITFALL #1 dihindari**: `_context.UserUnits` correlated, BUKAN nav-prop `u.UserUnits` (CS1061). **Anomali-backfill check `HcPortalDB_Dev`=0 → `.Any()` MURNI final** (NO OR-scalar-fallback; backfill 399 lengkap, RESEARCH Open Q2 resolved). **Consumer #4 `AssessmentAdminController:278`** (ManageAssessmentTab) mewarisi set-aware OTOMATIS (no code change). **No-drift D1=b (SC#3)**: analytics `CMPController:2581/:2589` scalar mirror + Team View `:543` no-filter call **UTUH** (diff bersih). Markup `_RecordsTeamBody.cshtml` byte-stable (value-driven). suite **507/0/3** (3 skip = `UserUnitsBackfillIntegrationTests` SQLEXPRESS-gated milik Phase 404; baseline ≥366 terlampaui, 0 regresi); filter `~WorkerDataServiceSearchTests` 17/17. **UAT lokal APPROVED** (Playwright+SQL snapshot→seed→RESTORE, journal cleaned `e203c9ad`: user Iwan GAST primary "Alkylation Unit (065)" + sekunder aktif "RFCC NHT (053)" → no-filter 1-baris dedup + kolom "Alkylation Unit (065), RFCC NHT (053)" / filter primary→match kolom "Alkylation Unit (065)" / **filter NON-primary RFCC→TETAP match** (set-aware ✓) kolom "RFCC NHT (053)" / EXISTS translation benar di SQL Server riil). 6 test MU-06 (set-aware both-units, dedup `Count==1`, IsActive D-03, kontekstual unfiltered/filtered D-02, fallback D-05) + regresi `Scope_Null_NoFilter_BackwardCompat` (SC#3). Commits RED `24a71b7f` + GREEN `520058b8`. **Plan 400-01 = 0 migration.** Files: `Services/WorkerDataService.cs`, `Controllers/WorkerController.cs`, `HcPortal.Tests/WorkerDataServiceSearchTests.cs`. **Phase 400 = 1/1 plan COMPLETE → ready `/gsd-verify-work`.**
- [399-04 / display semua unit SHIPPED — Phase 399 LENGKAP]: 7 surface tampil SEMUA unit pekerja primary ditandai (**MU-03 done**). 5 HTML (Profile/Settings/WorkerDetail/ManageWorkers/Home hero) badge primary-first via `Units.OrderByDescending(x=>x==PrimaryUnit).ThenBy(x=>x)`: primary=`bg-success bg-opacity-10 text-success`+`bi-star-fill`+"Utama"(+visually-hidden), sekunder=`bg-secondary bg-opacity-25 text-dark` (UI-SPEC §B PERSIS). **`_PSign` cetak all-units primary-first comma-join (D-07 LOCKED, BUKAN primary-only, teks polos no-badge)**; Excel kolom 7 primary-first (399-02). VM **Profile/Settings/PSign/DashboardHome** diperluas `List<string> Units`+`string? PrimaryUnit` (Section TETAP scalar; PSign scalar `Unit` fallback dipertahankan). **AccountController inject `ApplicationDbContext` BARU** (sebelumnya TIDAK ada — VERIFIED) + Profile/Settings GET populate UserUnits primary-first → VM + nested PSign; **WorkerDetail GET `ViewBag.WorkerUnits`/`PrimaryUnit`** (read-only view-binding, @model=ApplicationUser bukan VM — Rule 3, TIDAK sentuh write-logic 399-02); **HomeController** populate `CurrentUserUnits`/`PrimaryUnit` (mode-role null-safe). Fallback D-09 ("Belum diisi" panel / "-" cell / no-row print). Mirror `user.Unit` dipertahankan (pembaca belum-migrasi). Spec `tests/e2e/multiunit-display-399.spec.ts` **8/8 hijau** headless `--workers=1` (D-01..D-08: WorkerDetail 2 badge+bintang+Utama, ordering primary-first, ManageWorkers cell, 0-unit fallback, Profile smoke, **_PSign D-07 login-as-pekerja-2-unit baca `.psign-label`**, **Excel D-08 JSZip `xl/sharedStrings.xml` primary-first comma**). **build 0 error**; **suite 366/366** (0 skip, no regresi); app boot localhost:5277 HTTP 200; DB snapshot→RESTORE baseline `UserUnits`=6 (SEED_JOURNAL cleaned). 3 deviasi auto-fix (Rule 3 WorkerDetail ViewBag view-binding; Rule 1 ×2 spec: createWorker re-select Bagian + Excel assert via JSZip ganti exceljs fragile). Commits `24e0f6f2`/`781c2bf2`/`87e3ad7d`/`79dadd33`. **Plan 04 = 0 migration.** **Phase 399 Foundation LENGKAP → ready `/gsd-verify-work`.**
- [399-03 / multi-select widget SHIPPED]: `initSectionUnitMultiCascade(opts)` ditambah di `wwwroot/js/shared-cascade.js` (EXTEND — `initSectionUnitCascade`+`togglePassword` utuh) — render checkbox-list Unit `name="Units"` + radio `name="PrimaryUnit"` per baris client-side dari `ViewBag.SectionUnitsJson` (no AJAX, D-01). State machine **UI-SPEC §A 8-state WIRED**: Bagian kosong→placeholder; pilih Bagian→checkbox-list; centang→radio enabled; **default primary=first checked (D-02)**; uncheck primary→promote ke checked berikutnya; uncheck→radio disabled+clear; ganti Bagian→reset (invariant #1); HTML-escape nama unit (**T-399-03-04 mitigate**, bukan accept). Widget di **CreateWorker+EditWorker** (`#unitMultiContainer` role=group+aria-label; Bagian TETAP single `<select id=sectionSelect>`; idiom form + "Simpan" tak berubah, no inline font magic DSN-05). **MU-07 modal EditWorker** (`ViewBag.NeedConfirm`→modal "Konfirmasi Penghapusan {Unit}" + tombol "Ya, Hapus & Nonaktifkan" submit `form=editWorkerForm name=ConfirmedDeactivate value=true`; PROTON hard-block D-11 via validation-summary merah). **EditWorker GET pre-fill** `Model.Units`/`Model.PrimaryUnit` dari junction (**Rule 3 blocking round-trip**, view-binding necessity — no logic baru, tak sentuh write-through/guard 399-02). Spec `tests/e2e/multiunit-widget-399.spec.ts` 9 widget test test-first (data-driven `pickSectionWithUnits`, no hardcode Bagian) → **Playwright RUNTIME 9/9 hijau** (W-06 round-trip 2-unit Create→Edit + W-04 default-primary + W-05 promote + W-08 a11y; W-09 MU-07 skip fixture — logika server GREEN unit test 399-02). **build 0 error/0 warning**; app boot localhost:5277 HTTP 200; DB snapshot(setup)→RESTORE(teardown) baseline `UserUnits`=6 (worker round-trip temporary bersih, SEED_JOURNAL cleaned); suite 366/366. Scope display surfaces/`_PSign`/AccountController UTUH (= Plan 04). Commits `2a2767aa`/`b3756903`/`60aad1ab`/`03a775c6`. **Plan 03 = 0 migration.** **REQ MU-01/MU-02 done.**
- [399-02 / write-through SyncUserUnitsAsync SHIPPED]: kontrak write-through primary-mirror ter-implement di `WorkerController` sebagai PUBLIC STATIC helper testable (`SyncUserUnitsAsync(ctx,user,units,primary)` + `ParseUnitCell` + `ValidateUnitsInSection` + `EvaluateRemoveUnitGuardAsync` + `WorkerUnitsView` record) — pola `AdminBaseController.FindTitleDuplicatesAsync` (testable seam, no InternalsVisibleTo). Single-source junction `UserUnits` + mirror `ApplicationUser.Unit` (replace-set DELETE-before-INSERT, no window 2-primary) + audit set-diff (D-12, hapus anti-pattern `if user.Unit != model.Unit`). Wire Create/Edit/Import; **EditWorker 1-tx atomic** (`BeginTransactionAsync`: UpdateAsync + UserUnits + deactivate, **Open Q3**). **MU-07 asimetris**: PTA aktif → HARD-BLOCK (`protonUnit = AssignmentUnit ?? oldPrimary`, **Open Q1** kedua cabang + **Open Q2** kosong-semua-unit ter-cover) (D-11); coach-mapping aktif tanpa PTA → confirm (NeedConfirm re-prompt) → auto-deactivate IsActive/EndDate 1 tx (D-10). Validasi `Unit∈Bagian` + `primary∈set` tiap junction-write (MU-05, mass-assignment guard). Import **pipe** "UnitA|UnitB" first=primary dedup backward-compat (MU-04) + per-unit validasi; Export kolom 7 primary-first comma-join (D-08); template help-text + contoh pipe (D-06); ManageWorkers `ViewBag.UserUnitsDict` (untuk display Plan 04, unitFilter TETAP scalar = Phase 400 MU-06). **6 test logic Wave 0 GREEN (19 fakta)**; suite **366/366** (347+19, 0 skip, no regresi); build 0 error; app boot localhost:5277 HTTP 200; **round-trip 2-unit SQL lokal MATCH** (2 baris/1 IsPrimary/mirror==primary-row) + filtered-unique tolak 2nd primary + RESTORE baseline (SEED_JOURNAL cleaned). authz `[Authorize(Admin,HC)]` 12 + `[ValidateAntiForgeryToken]` 6 utuh; bind hanya Units/PrimaryUnit. Commits 862003b7/facc0df6/23fb5033/dadca0cc. **Plan 02 = 0 migration.** Interface siap Plan 03 (widget konsumsi Units/PrimaryUnit/SectionUnitsJson) + Plan 04 (display konsumsi UserUnitsDict/VM/PSign).
- [399-01 / junction UserUnits APPLIED]: tabel `UserUnits` ada di DB lokal (`HcPortalDB_Dev`) — filtered-unique `IX_UserUnits_UserId_PrimaryUnique` WHERE [IsPrimary]=1 (enforce invariant #3) + unique `IX_UserUnits_UserId_Unit_Unique` + backfill idempotent (6 baris IsPrimary=1 == 6 Users Unit-non-null; Unit-null=0 baris; re-run backfill=0 dobel). Migration scaffold via **global `dotnet ef` CLI v10.0.3** (backward-compat OK dengan project EF 8.0.0; snapshot tetap ProductVersion 8.0.0) — fallback pin-tool/hand-author tak perlu. Index UserId polos dibuang (EF dedup ke filtered-unique; has-pending-model-changes=none). `Users.Unit` scalar DIPERTAHANKAN (mirror). **migration=TRUE commit `fc015f4d`** (`AddUserUnitsTable`) — SATU-SATUNYA migration milestone v32.3, notify IT saat push. Interface siap untuk plan 02 (SyncUserUnitsAsync write-through) + plan 03/04 (UI/display). 7 test scaffold Wave 0 RED skip-with-reason (suite 347 pass/0 fail/16 skip).
- [v32.3 roadmap / phases 399-404]: 6 fase derived dari spec §5/§6 (mapping fase→REQ + dependency TERKUNCI, bukan re-derive) — **399** Foundation junction `UserUnits`+mirror+multi-select UI (MU-01/02/03/04/05/07, **migration=TRUE**), **400** listing set-aware+dedup (MU-06), **401** PROTON unit-resolution hardening (PSU-01/02/03/04/05/07), **402** coaching cross-unit (CXU-01..05), **403** Org cascade/guard UserUnits-aware (ORG-01/02), **404** test SQL riil+UAT+docs (QA-01..04). 24/24 REQ mapped, 0 orphan/duplicate. Dependency: 400/401/403→399; 402→401; 404→semua. Wave 1 {400,401,403} PARALEL (cluster file disjoint), 402 serial setelah 401. Critical path 399→401→402→404. Phase numbering mulai 399 (391-398 reserved di branch main: v32.0=391-392, v32.2=393-398).
- [v32.3 invariant (spec §7)]: (1) Section scalar 1 Bagian/akun; (2) PROTON single-active (index `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` + E8 dipertahankan); (3) primary mirror `ApplicationUser.Unit`=baris `UserUnits.IsPrimary` write-through; (4) `AssignmentUnit ∈ coachee.UserUnits` (pasca-401); (5) `ProtonKompetensi.Unit` 1:1 per deliverable. Junction `UserUnits.Unit` = NAME-string (konsisten `AssignmentUnit`/`ProtonKompetensi.Unit`), validasi via `GetUnitsForSectionAsync(user.Section)`. D1=b cert/analytics atribusi primary (no kolom unit-at-issue, no migration ke-2). De-risk: authz Section 100% scalar → 0 perubahan.
- [v32.1 / 389-01 spec parity]: `tests/e2e/coachcoacheemapping-389.spec.ts` 14-test (V-01..V-14) test-first (Nyquist safeguard, Phase 354 lesson). Closed PASSED 7/7.
- [v31.0 Hotfix Pra-Ujian Lisensor / phases 385-387]: 14/14 PXF closed, 0 migration. Pattern: shared display helper `AssessmentScoreAggregator.IsQuestionCorrect`+`BuildAnswerCell` (kill-drift); essay PathBase-aware sub-path. MERGED→main 7ea6c81e, UAT Dev full-lifecycle PASS.
- [v30.0 / ECG]: helper terpusat `AssessmentScoreAggregator.IsQuestionCorrect` (essay `>0`=Benar) `CMPController.Results` 4 site + PDF (kill-drift).
- [v29.0 / 382]: SAVE-01 dedupe last-write-wins in-memory, NO migration.
- [v25.0 / A-4]: Penanda kelulusan Proton lewat 1 helper bersama `ProtonCompletionService` (Origin).
- [v24.0 / spec §9]: Hapus file gambar pola Phase 333/335 — kumpul path SEBELUM tx, File.Delete SETELAH commit, warn-only.
- [v24.0 / AF-1..7]: eligibility coachee per-unit (CoacheeEligibilityCalculator) — **relevan v32.3** (multi-unit memperluas eligibility ke lintas-unit dalam Bagian, fase 402).
- [v22.0 cross-milestone]: `AssessmentConstants.AssessmentStatus.PendingGrading` single source of truth label.
- [v21.0 / org labels]: tier label org (Bagian/Unit/Sub-unit) configurable via `IOrgLabelService` + global `@inject OrgLabels` (110 calls / 26 views) — **relevan v32.3** (display unit + multi-select Bagian/Unit pakai `@OrgLabels.GetLabel(0/1)`).
- [v13.0]: org tree `OrganizationUnit` self-FK `ParentId` (Level0=Bagian, Level1=Unit), user nyambung via Name-string bukan Id — **fondasi v32.3** (`UserUnits.Unit` NAME-string anak Bagian; `GetSectionUnitsDictAsync`/`GetUnitsForSectionAsync` primitif siap dipakai multi-select + validasi).
- [v12.0]: AdminController dipecah jadi 8 controller per domain; URL tetap via [Route]. (Worker/CoachMapping/CDP/Organization/AssessmentAdmin = controller terpisah → fondasi cluster file disjoint paralelisme Wave 1 v32.3.)

### Open Blockers/Concerns

- [push] Carry migration lama (`PendingProtonBypass`+index/360, `ShuffleToggles`/372) — notify IT flag. **v32.3 = migration BARU TRUE** (`AddUserUnitsTable` Phase 399 + backfill).
- [v32.3 risiko utama (spec §10)]: (a) `CleanupCoachCoacheeMappingOrg` reset `AssignmentUnit`→primary = data-loss multi-unit → Fase 401 jadikan UserUnits-aware/gated SEBELUM data multi-unit produksi; (b) reparent unit lintas-Bagian = split-brain Section → Fase 403 hard-block; (c) primary-mirror desync → kontrak write-through terpusat + test; (d) EF-InMemory tak enforce filtered-unique-index → test palsu hijau → Fase 404 WAJIB SQL riil SQLEXPRESS; (e) atribusi cert primary bikin cert unit-Y muncul di laporan unit-X → diterima (D1=b), didokumentasikan.
- [v32.3 / v32.0 close DEFERRED]: ⚠️ JANGAN `/gsd-complete-milestone v32.0` standar (REQUIREMENTS/STATE/PROJECT kini live v32.3 → step5 destruktif). Safe close v32.0 NANTI manual (post-v32.3). Lihat MEMORY `project_v32_0_close_deferred`.

## Session Continuity

Last activity: 2026-06-18

Stopped at: Phase 403 UI-SPEC approved

Next action: **`/gsd-verify-work 400`** (Phase 400 = 1/1 plan complete). Lalu lanjut Wave-1 paralel {401 (sudah PLANNED, 6 plan), 403 (TBD)} (depends 399 done; cluster file disjoint, git worktree). Wave 2 = 402 (setelah 401), Wave 3 = 404. `/clear` dulu (fresh context). **migration=FALSE Phase 400** — notify IT saat milestone push (satu-satunya migration v32.3 = 399 `AddUserUnitsTable` `fc015f4d`).
