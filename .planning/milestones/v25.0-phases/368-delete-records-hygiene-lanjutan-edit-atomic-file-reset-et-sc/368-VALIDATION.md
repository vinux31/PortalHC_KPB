---
phase: 368
slug: delete-records-hygiene-lanjutan-edit-atomic-file-reset-et-sc
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-13
updated: 2026-06-13
---

# Phase 368 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Sumber: 368-RESEARCH.md §"Validation Architecture". Per-task map difinalkan oleh planner saat PLAN.md dibuat (2026-06-13).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`HcPortal.Tests/`) — sudah ada, no install |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (SQL-less, ~1s) |
| **Full suite command** | `dotnet test HcPortal.Tests` (termasuk integration real-SQL @localhost\SQLEXPRESS) |
| **Baseline saat ini** | **post-368: quick 215/215, full 306/306** (re-verified eksekusi 2026-06-13; +18 [Fact]/[Theory] lintas 4 plan) |
| **Estimated runtime** | quick ~1-2s; real-SQL integration tambah disposable-DB per fixture |

---

## Sampling Rate

- **After every task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` untuk area tersentuh
- **After every plan wave:** `dotnet test HcPortal.Tests` (full suite, real-SQL)
- **Before `/gsd-verify-work`:** Full suite green + Seed Workflow (snapshot→seed→restore+journal) untuk test #23 cleanup
- **Max feedback latency:** filtered runs < ~60s

---

## Per-Task Verification Map

> Diisi planner 2026-06-13. Minimal spec §3.4: [Fact] replace-file atomic (#21), retake pasca-reset → ET baru (#22), import ter-audit-log (#24) — SEMUA ter-cover. #23/#25/#26 juga ber-test (fixture sudah ada). #27 = grep/build (kosmetik).

| Task ID | Plan | Wave | Requirement (#temuan) | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|------------------------|------------|-----------------|-----------|-------------------|-------------|--------|
| P01-T1 | 01 | 1 | #21 | T-368-02/03 | atomic replace: new-wins, metadata-only keep, upload-fail keep (V12 confined webroot) | [Fact] file-on-disk (temp-dir, pola 355) | `dotnet test HcPortal.Tests --filter "EditAtomicFile"` | ✅ `EditAtomicFileTests.cs` | ✅ green (3 [Fact], SUMMARY 01) |
| P01-T2 | 01 | 1 | #21, #26 | T-368-01/02/03/04 | EditTraining+EditManualAssessment atomic + same-user renewal check (IDOR), pesan generik | build + quick suite + grep | `dotnet build; dotnet test HcPortal.Tests --filter "Category!=Integration"` | ✅ (controller) | ✅ green (212/212 quick, SUMMARY 01) |
| P01-T3 | 01 | 1 | #26 | T-368-01 | cross-user invalid / non-existent invalid / same-user valid | integration real-SQL | `dotnet test HcPortal.Tests --filter "RenewalValidation"` | ✅ `RenewalValidationTests.cs` | ✅ green (3 [Fact] real-SQL, SUMMARY 01) |
| P02-T1 | 02 | 2 | #24, #27 | T-368-08 | Manual konstanta + GenerateCertificate=isPassed + audit ImportTraining; label swap | build + quick suite + grep | `dotnet build; dotnet test HcPortal.Tests --filter "Category!=Integration"` | ✅ (controller+views) | ✅ green (greps + 212 quick, SUMMARY 02) |
| P02-T2 | 02 | 2 | #23 | T-368-05/06/07/08/09 | GET preview-count + POST [Authorize Admin]+[CSRF] + orphan SEMPIT + audit + idempotent | build + quick suite + grep | `dotnet build; dotnet test HcPortal.Tests --filter "Category!=Integration"` | ✅ (controller+view) | ✅ green (greps + build, SUMMARY 02; UAT P04-T3) |
| P02-T3 | 02 | 2 | #23, #24 | T-368-07/08 | orphan preview=2 → execute → re-run=0 idempotent; #24 field+audit persist | integration real-SQL | `dotnet test HcPortal.Tests --filter "OrphanCleanup\|ImportTrainingAudit"` | ✅ `OrphanCleanupTests.cs`, `ImportTrainingAuditTests.cs` | ✅ green (4 [Fact]/[Theory] real-SQL, SUMMARY 02) |
| P03-T1 | 03 | 1 | #22 | T-368-10 | cleanup ET → retake re-insert ElemenTeknis sama tanpa unique-violation → fresh | integration real-SQL | `dotnet test HcPortal.Tests --filter "ResetEtCleanup"` | ✅ `ResetEtScoreTests.cs` | ✅ green (3 [Fact] real-SQL incl unique-index bukti, SUMMARY 03) |
| P03-T2 | 03 | 1 | #22 | T-368-10/11 | RemoveRange ET sebelum SaveChanges; no new transaction; filter AssessmentSessionId==id | build + filter + grep | `dotnet build; dotnet test HcPortal.Tests --filter "ResetEtCleanup"` | ✅ (controller) | ✅ green (greps + 212 quick, SUMMARY 03) |
| P04-T1 | 04 | 2 | #25 | T-368-12 | helper GroupBy-dedup; duplicate child Name tidak throw; lookup benar | unit [Fact] (no DB) | `dotnet build; dotnet test HcPortal.Tests --filter "ParentNameLookup"` | ✅ `CertDedupTests.cs` | ✅ green (3 [Fact] unit, SUMMARY 04) |
| P04-T2 | 04 | 2 | #25 | T-368-12/13 | CMP+CDP konsumsi helper shared; 0 ToDictionary(c=>c.Name); 0 inline GroupBy | build + quick suite + grep | `dotnet build; dotnet test HcPortal.Tests --filter "ParentNameLookup"` | ✅ (controllers) | ✅ green (greps + 215/215 quick, SUMMARY 04) |
| P04-T3 | 04 | 2 | #23, #27, #25 | T-368-05..12 | UAT browser: #23 preview→execute→idempotent (Seed Workflow), #27 label, #25 no-500 | MANUAL (checkpoint) + full suite | `dotnet test HcPortal.Tests` (otomatis pendukung) | ✅ UAT browser @5277 | ✅ green (UAT 3/3 PASS: #23 DB-verified idempotent + #27 label + #25 no-500; full suite 306/306; Seed Workflow journal cleaned, SUMMARY 04) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Sampling continuity check:** Tidak ada 3 task berturut tanpa automated verify — setiap task punya `<automated>` (build/filter/test) atau Wave-0 test dependency. Checkpoint P04-T3 didukung full-suite otomatis + manual UAT.

---

## Wave 0 Requirements

> Reuse fixture Phase 367 (terbukti). Koreksi RESEARCH: `SeedRenewalChainAsync` sebagai metode bernama TIDAK ADA — seeding inline via `NewSession(renewsSession:...)`. Helper fixture 367 (`SeedUserAsync`/`NewSession`/`NewTraining`/`FakeWebHostEnvironment`) = `private static` di `RecordCascadeIntegrationTests` → test baru salin/adaptasi pola; class `RecordCascadeFixture` public → reuse via `IClassFixture`.

- [ ] Reuse `RecordCascadeFixture` (public) untuk integration test #22/#23/#26 — disposable real-SQL @localhost\SQLEXPRESS, `[Trait("Category","Integration")]`.
- [ ] Helper temp-dir (pola `PackageImageDeleteTests.cs:209-238` Phase 355) untuk [Fact] atomic file #21.
- [ ] Insert orphan AttemptHistory di test mudah — `AssessmentAttemptHistory.SessionId` plain int TANPA FK ke session, seed bebas (User valid via FK User) (#23).
- [ ] 5 test file baru (semua git add eksplisit — untracked lesson): `EditAtomicFileTests.cs` (P01-T1), `RenewalValidationTests.cs` (P01-T3), `OrphanCleanupTests.cs`+`ImportTrainingAuditTests.cs` (P02-T3), `ResetEtScoreTests.cs` (P03-T1), `CertDedupTests.cs` (P04-T1).

> Framework xUnit sudah ada — no install. `wave_0_complete: true` di-set saat 5 test file dibuat + hijau di eksekusi.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions | Plan |
|----------|-------------|------------|-------------------|------|
| #23 endpoint cleanup orphan: preview-count tampil → execute → DB bersih + idempotent (re-run=0) | #23 | End-to-end + DB cross-check butuh browser + sqlcmd; Seed Workflow wajib | UAT @5277 (`Authentication__UseActiveDirectory=false dotnet run`): snapshot→seed orphan AttemptHistory (SessionId dangling) → `GET /Admin/CleanupAttemptHistory` preview hitung → execute → `SELECT` bersih → re-run preview=0 → restore + journal `cleaned` | 04 Task 3 |
| #27 rename label "Bulk Import Nilai (Excel)" tampil di UI | #27 | Render label = verifikasi visual | UAT: buka `/Admin/BulkBackfill` + Admin Index + tab Assessment Groups → label match "Bulk Import Nilai (Excel)" | 04 Task 3 |
| #25 CertificationManagement CMP+CDP render OK (tidak 500) | #25 | Render halaman = verifikasi visual; 500 hanya tampak runtime | UAT: buka CMP + CDP CertificationManagement → render OK (tidak ArgumentException 500) | 04 Task 3 |
| #21 edit metadata-only → file sertifikat lama utuh (opsional) | #21 | File-on-disk + browser edit | UAT: edit training record ubah nama TANPA upload → file lama masih ada | 04 Task 3 |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (Per-Task Map terisi)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (5 test file)
- [x] No watch-mode flags
- [x] Feedback latency < 60s (filtered runs)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** ✅ VALIDATED (post-execution audit 2026-06-13) — 11 task COVERED + green; `wave_0_complete: true` (6 test file dibuat + hijau); `nyquist_compliant: true`.

---

## Validation Audit 2026-06-13

State A audit — rekonsiliasi VALIDATION.md (planner-filled, status ⬜/❌-create) terhadap artefak eksekusi (4 SUMMARY) + verifikasi disk + re-run suite + UAT browser.

| Metric | Count |
|--------|-------|
| Tasks di Per-Task Map | 11 (10 auto + 1 checkpoint UAT) |
| Gaps found (MISSING) | 0 |
| COVERED (green) | 11 |
| Resolved (auditor-filled) | 0 (semua test dibuat saat eksekusi TDD) |
| Escalated to Manual-Only | 0 (UAT P04-T3 sudah PASS browser) |

**Bukti:**
- 6/6 file test ADA di disk: `EditAtomicFileTests.cs`, `RenewalValidationTests.cs`, `OrphanCleanupTests.cs`, `ImportTrainingAuditTests.cs`, `ResetEtScoreTests.cs`, `CertDedupTests.cs`.
- Full suite eksekusi: **306/306** (215 quick + 91 integration real-SQL); +18 [Fact]/[Theory] baru.
- UAT P04-T3 (Manual-Only checkpoint) PASS browser @5277: #23 preview=3→execute→idempotent DB-verified (orphan=0, non-orphan 5 utuh, audit logged) + #27 label + #25 CDP no-500. Seed Workflow journal `cleaned`.
- Spec §3.4 minimal ter-cover: [Fact] replace-file atomic (#21) ✅, retake-after-reset ET fresh (#22) ✅, import audit-logged (#24) ✅.
- Migration=FALSE seluruh phase; 0 regresi.

**Hasil:** Phase 368 NYQUIST-COMPLIANT. Zero gap — auditor (gsd-nyquist-auditor) tidak di-spawn.
