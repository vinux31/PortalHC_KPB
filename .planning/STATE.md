---
gsd_state_version: 1.0
milestone: v31.0
milestone_name: Hotfix Pra-Ujian Lisensor
status: Executing Phase 386
stopped_at: Completed 386-01-PLAN.md
last_updated: "2026-06-15T14:40:25.836Z"
last_activity: 2026-06-15
progress:
  total_phases: 24
  completed_phases: 1
  total_plans: 12
  completed_plans: 3
  percent: 25
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 386 — assessmentadmincontroller-hardening

## Current Position

Phase: 386 (assessmentadmincontroller-hardening) — EXECUTING
Plan: 2 of 6 (386-01 Wave-0 RED scaffolds DONE — 5 test files + 1 authz extend, 0 production code, build RED only on Wave-1 helpers ValidateQuestionOptions/BuildAnswerCell)

**MILESTONE v31.0 STARTED — Hotfix Pra-Ujian Lisensor (urgent, acara ~2026-06-17).** 5 temuan must-fix dari readiness audit gladi-bersih E2E 2026-06-15 (register final adversarial-verified: `.planning/notes/2026-06-15-readiness-ujian-lisensor.md` — 3 HIGH · 5 MED · 7 LOW; 5 dipromote ke PXF-01..05). Ujian lisensor: SA+MA+Essay+soal bergambar, ≤30 peserta, PDF per-peserta = bukti resmi. Target: 1 bundle → 1 deploy IT sebelum hari-H. **0 migration** (semua fix view/controller/validasi). Pendekatan: hotfix langsung (skip domain-research).

**Roadmap v31.0 (3 fase, penomoran LANJUT dari v30.0 phase terakhir 384):**

| Phase | Goal (ringkas) | REQ | File |
|-------|----------------|-----|------|
| **385 Exam-Taking & Image Render Hotfix** | Gambar soal/opsi tampil di sub-path `/KPB-PortalHC` (PathBase-aware) + essay flush saat submit/blur/timeout | PXF-01, PXF-03 | `Views/Shared/_QuestionImage.cshtml`, `Views/CMP/StartExam.cshtml` (+ mungkin `CMPController.cs`) |
| **386 AssessmentAdminController Hardening** | Validasi soal ≥1 opsi + essay kosong tak dead-end finalize + PDF MA SetEquals akurat | PXF-02, PXF-04, PXF-05 | `Controllers/AssessmentAdminController.cs` |
| **387 Post-Lisensor Assessment Polish** (PASCA-acara, depends 386) | 9 temuan polish: guard SubmitEssayScore status, Excel essay label + MA SetEquals, cert nomor retry, BulkExport essay skor/teks, broadcast monitor, aria opsi huruf, SubmitExam no null-overwrite, SaveTextAnswer guard timer | PXF-06..14 | `AssessmentAdminController.cs`, `ExcelExportHelper.cs`, `Results.cshtml`, `CMPController.cs`, `AssessmentHub.cs` |

**File-overlap (kunci phasing):** PXF-02 + PXF-04 + PXF-05 semua di `AssessmentAdminController.cs` → **digabung Phase 386**. PXF-01 + PXF-03 file view berbeda → Phase 385. Phase 387 (PXF-06..14) = polish pasca-acara, **depends 386** (PXF-06/08/09/10 juga di `AssessmentAdminController.cs`); deploy IT KEDUA terpisah dari bundle urgent. Semua 0 migration.

**Coverage:** 14/14 PXF ter-map ✓ — Orphans: 0 — Duplicates: 0. (385-386 = 5 must-fix pra-acara; 387 = 9 polish pasca-acara, ditambah 2026-06-15 dari FUTURE + F-DEV-02.)

**Plan:** Not started

**Next:** `/gsd-plan-phase 385` (lalu `/gsd-plan-phase 386`). Tiap fase: `dotnet build` + `dotnet run` (localhost:5277) + verifikasi (PXF-01 via URL prefix `/KPB-PortalHC` lokal + Playwright; PXF-02/03/04 unit test + Playwright; PXF-05 unit test) sebelum commit → 1 push → notify IT re-deploy. ❌ tidak ada edit di Dev/Prod (CLAUDE.md Develop Workflow). Mitigasi operasional saat ujian (walau sudah fix): 1 paket soal, cek tiap soal punya opsi, briefing peserta.

Predecessor: v25.0 + v26.0 + v27.0 + v28.0 + v29.0 + v30.0 SHIPPED LOCAL + audited PASSED + closed. v29/v30 PUSHED `origin/ITHandoff` + tag (`v29.0`/`v30.0`).

| Milestone | Phases | REQ | Audit | Archive |
|-----------|--------|-----|-------|---------|
| v25.0 Proton Kelulusan & Bypass | 358-368 | 20/20 PCOMP/PBYP | PASSED | milestones/v25.0-ROADMAP.md |
| v26.0 Urgent Search & Records Visibility | 369-371 | 3/3 URG | PASSED | milestones/v26.0-ROADMAP.md |
| v27.0 Shuffle Toggle | 372-375 | 16/16 SHUF | PASSED | milestones/v27.0-ROADMAP.md |
| v28.0 Assessment & Records Bug Fixes | 376-379 | 6/6 GRADE/IMP/CMPRT/E2E | PASSED | milestones/v28.0-ROADMAP.md |
| v29.0 Assessment E2E Worker-Success Fix | 380-382 | 11/11 WSE | PASSED | milestones/v29.0-ROADMAP.md |
| v30.0 Essay Grading Correctness + Monitoring UI Refactor | 383-384 | 10/10 ECG/UIG | PASSED | milestones/v30.0-ROADMAP.md |

## Next Action

1. **`/gsd-plan-phase 385`** — rencanakan Phase 385 (PXF-01 gambar PathBase + PXF-03 flush essay). File view, paralel-aman.
2. **`/gsd-plan-phase 386`** — rencanakan Phase 386 (PXF-02 validasi opsi + PXF-04 essay kosong finalize + PXF-05 PDF MA SetEquals). Satu file `AssessmentAdminController.cs`.
3. Setelah kedua fase shipped + verified lokal → 1 push → notify IT re-deploy Dev sebelum hari-H (~2026-06-17).

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

Stopped at: Completed 386-01-PLAN.md

Next action: **`/gsd-plan-phase 385`** (PXF-01 gambar PathBase + PXF-03 flush essay; file view, paralel-aman) lalu **`/gsd-plan-phase 386`** (PXF-02/04/05; satu file `AssessmentAdminController.cs`). Urgent (acara ~2026-06-17): target 1 bundle → 1 push → notify IT re-deploy. Tiap fase verify lokal (`dotnet build`+`dotnet run` localhost:5277 + Playwright/unit per REQ) sebelum commit. JANGAN edit DB/kode Dev/Prod (CLAUDE.md).
