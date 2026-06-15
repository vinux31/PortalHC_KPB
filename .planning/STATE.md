---
gsd_state_version: 1.0
milestone: v31.0
milestone_name: Hotfix Pra-Ujian Lisensor
status: Executing Phase 386
stopped_at: Completed 386-05-PLAN.md
last_updated: "2026-06-15T15:25:26.195Z"
last_activity: 2026-06-15
progress:
  total_phases: 24
  completed_phases: 1
  total_plans: 12
  completed_plans: 7
  percent: 58
---

# Project State: Portal HC KPB

## Project Reference

See: .planning/PROJECT.md

**Core value:** Evidence-based competency tracking with automated assessment-to-CPDP integration
**Current focus:** Phase 386 — assessmentadmincontroller-hardening

## Current Position

Phase: 386 (assessmentadmincontroller-hardening) — EXECUTING
Plan: 6 of 6 (386-05 Wave-4 PXF-05 DONE — 2 surface bukti-resmi export di-route ke helper display bersama. **PDF** `GeneratePerPesertaPdf` (Controllers/AssessmentAdminController.cs, loop "Detail Jawaban per Soal"): blok single-row `FirstOrDefault` MA-mislabel diganti `responsesForQ = Where(PackageQuestionId==q.Id).ToList()` → `bool? correct = IsQuestionCorrect(q, responsesForQ)` (SEMUA tipe, MA all-or-nothing SetEquals) + `string jawaban = BuildAnswerCell(q, responsesForQ)` (MA join ", " SEMUA opsi terpilih); statusColor/statusText (✓ Benar/✗ Salah/— Pending) + render QuestPDF UTUH byte-identik; MC byte-identik; Essay sudah pakai helper. **Excel** `ExcelExportHelper.AddDetailPerSoalSheet` (F-DEV-02 D-13 folded): blok `var response = FirstOrDefault(...)` + `if (response==null){...}else{...}` diganti `responsesForQ = Where(SessionId && q.Id).ToList()` → BuildAnswerCell + IsQuestionCorrect; cell 2-kolom (Jawaban + ✓/✗/— warna Green/Red) UTUH. **Unifikasi label essay Excel (intentional D-13):** `EssayScore >= ScoreValue/2` lama → `> 0` (v30.0 canonical) = sama dgn PDF + web Results. **Compute (scoring engine) 0 diff** di AssessmentScoreAggregator.cs (git-verified, D-11 — display-path saja). Tak perlu `using` baru (ExcelExportHelper sudah namespace HcPortal.Helpers). Verif: build 0 error; grep single-row mislabel 0× di kedua file; pure suite 347/347 GREEN (incl PdfAnswerCellTests + IsQuestionCorrect regression); 0 migration. Commits 85861b69 (PDF) + bb058f1b (Excel). **PXF-05 CLOSED.** e2e tetap `test.fixme` (un-skip Plan 06). Sisa: 386-06 (verify/e2e + UAT browser PDF/Excel). Predecessor: 386-04 PXF-04 + 386-03 PXF-02 + 386-02 helper + 386-01 RED DONE.

Plan-LAMA: 5 of 6 (386-04 Wave-3 PXF-04 DONE — predikat pending essay TUNGGAL `!IsNullOrWhiteSpace(TextAnswer) && EssayScore==null` di 4 surface byte-identik `Controllers/AssessmentAdminController.cs` (SITE1 page L3506, SITE2 finalize-gate L3650, SITE3 SubmitEssayScore L3580, SITE4 Monitoring L3308-3321) → tutup F-04 essay-empty dead-end. `SubmitEssayScore` jadi defensive UPSERT (buat row bila absen, TextAnswer=null) ganti dead-end "Jawaban tidak ditemukan" + status-guard WAJIB tolak `Status != PendingGrading` (D-08, T-386-AUTHZ HIGH, tutup F-03 widening); attributes `[HttpPost]/[Authorize Admin,HC]/[ValidateAntiForgeryToken]` UTUH. **Rule-1 fix saat Task 3:** RESEARCH L60 keliru asumsi `IsNullOrWhiteSpace` translate ke `=N''` — probe SQL Server EF Core 8 buktikan LTRIM/RTRIM/TRIM TAK trim tab/newline → server-side IsNullOrWhiteSpace divergen .NET utk TextAnswer=`\t\n`. Solusi: 2 EF site (SITE3+SITE4) filter `EssayScore==null`+Join Essay server-side, materialize TextAnswer, lalu `IsNullOrWhiteSpace` IN-MEMORY (predikat logis tetap byte-identik 4 surface; hanya titik-eval whitespace geser server→memori). 2 EF mirror di `EssayEmptyPendingParityTests` diselaraskan. Parity 6/6 GREEN (incl varian `\t\n`) + authz 2/2 + suite 474/474; 0 migration. Commits 6efd0294/79132809/866917b6. **PXF-04 CLOSED.** Predecessor: 386-03 PXF-02 + 386-02 helper + 386-01 RED DONE. Sisa: 386-05 (PXF-05 PDF MA SetEquals) + 386-06 (verify/e2e))

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

Stopped at: Completed 386-05-PLAN.md (Wave-4 PXF-05 CLOSED — PDF GeneratePerPesertaPdf + Excel AddDetailPerSoalSheet di-route ke BuildAnswerCell + IsQuestionCorrect; MA all-or-nothing SetEquals di KEDUA surface resmi; Compute 0 diff; pure suite 347/347; 0 migration; commits 85861b69+bb058f1b)

Next action: **`/gsd-execute-phase 386`** lanjut ke **386-06-PLAN.md** (Wave-5 verify/e2e): un-skip e2e `option-validation-386.spec.ts` + `essay-empty-finalize-386.spec.ts` (`test.fixme`→`test`), + UAT browser PDF per-peserta & Excel "Detail Per Soal" konfirmasi MA di-label Benar HANYA bila set benar-persis terpilih + Jawaban list semua opsi. Setelah 386-06 → Phase 386 SELESAI; gabung dengan Phase 385 (sudah complete) jadi 1 bundle → 1 push `origin/ITHandoff` → notify IT re-deploy Dev sebelum hari-H (~2026-06-17). v31.0 = **0 migration baru**. JANGAN edit DB/kode Dev/Prod (CLAUDE.md Develop Workflow).
