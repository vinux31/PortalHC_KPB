---
phase: 367
slug: delete-records-cascade-overhaul-hapus-100-sampai-akar-cascad
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-12
updated: 2026-06-13
---

# Phase 367 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`HcPortal.Tests/`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (no separate runsettings) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (SQL-less, < 30s) |
| **Full suite command** | `dotnet test HcPortal.Tests` (termasuk integration real-SQL @localhost\SQLEXPRESS) |
| **Baseline saat ini** | **post-367: quick 209/209 passed** (`Category!=Integration`, re-verified 2026-06-13); full suite **290/290** (209 quick + 81 integration real-SQL per SUMMARY 02/05/06/07). +61 [Fact]/[Theory] baru lintas 8 plan. |
| **Estimated runtime** | quick ~1s; real-SQL integration tambah disposable-DB setup per fixture |

---

## Sampling Rate

- **After every task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` (< 30s) untuk area tersentuh
- **After every plan wave:** `dotnet test HcPortal.Tests` (full suite, real-SQL)
- **Before `/gsd-verify-work`:** Full suite green + Playwright UAT 2-arah (Plan 08) + Seed Workflow (snapshot→seed→restore+journal)
- **Max feedback latency:** filtered runs di bawah ~60s

---

## Per-Task Verification Map

> Sumber: RESEARCH §"Validation Architecture" + field `<automated>` tiap task di 8 PLAN. Setiap task auto memetakan ke automated `dotnet test` / `dotnet build`; checkpoint UAT (Plan 08 Task 2) = Manual-Only (lihat section terpisah). Wave 0 = fixture real-SQL disposable + seed renewal-chain dibangun di Plan 01/02 (task pertama yang butuh).

| Task ID | Plan | Wave | Requirement (#temuan) | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|------------------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-T1 | 01 | 1 | spec-3.1-traversal (BFS lintas tabel, cycle guard) | T-367-01, T-367-02 | rootType whitelist {session,training}; HashSet visited cegah infinite loop | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RecordCascadeServiceTests"` | ✅ `RecordCascadeServiceTests.cs` | ✅ green (5 [Fact] traversal, SUMMARY 01) |
| 01-T2 | 01 | 1 | #15 (mirror ±1 hari), spec-3.1-preview | T-367-03, T-367-04 | BuildPreviewAsync read-only (zero mutasi); preview admin-only | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~MirrorHeuristicTests"` (+ `~RecordCascadeServiceTests`) | ✅ `MirrorHeuristicTests.cs` | ✅ green (6 [Fact] mirror/preview, SUMMARY 01) |
| 02-T1 | 02 | 2 | #5,#6,#8,#9,#10,#11,#19,L-04,L-08,spec-3.1-execute | T-367-05, T-367-06, T-367-07, T-367-08, T-367-09 | 1-tx cascade; mirror-ID IDOR validasi milik-user; pesan generik (no ex.Message); File.Delete confined webroot; soft-cancel + audit jejak; notif OQ-1 konservatif | unit (RED) → integration (02-T2) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RecordCascadeServiceTests"` | ✅ `RecordCascadeServiceTests.cs` (extend) | ✅ green (ExecuteAsync, SUMMARY 02) |
| 02-T2 | 02 | 2 | #5,#6,#8,#9,#10,#11,#19,L-04,L-08 (assert per-tabel) | T-367-05..09 (verifikasi) | Real-SQL per-tabel: PendingBypass `Dibatalkan` bertahan; LinkedSessionId null; Origin Interview/Bypass kebal; rollback utuh; preview==execute | integration real-SQL + [Fact] file | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RecordCascadeIntegrationTests|FullyQualifiedName~RecordCascadeFileTests"` | ✅ `RecordCascadeIntegrationTests.cs`, `RecordCascadeFileTests.cs` | ✅ green (11 [Fact] real-SQL: 8 integration + 3 file, SUMMARY 02) |
| 03-T1 | 03 | 1 | #16,#17,D-01 (badge recompute = baris tampil) | T-367-10, T-367-11 | Read-only formula; badge per-worker dalam scope authz admin; no schema change | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~BadgeRecomputeTests"` | ✅ `BadgeRecomputeTests.cs` | ✅ green (4 [Fact], SUMMARY 03) |
| 04-T1 | 04 | 1 | #18 (sibling no over-match) | T-367-12 | Filter LinkedGroupId==null && bukan Pre/Post && !IsManualEntry; image 366 preserved | unit (predikat) / integration kecil | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~SiblingFilterTests"` | ✅ `SiblingFilterTests.cs` | ✅ green (3 [Fact] predikat Compile(), SUMMARY 04) |
| 04-T2 | 04 | 1 | #20 (ResetAssessment guard IsManualEntry) | T-367-13, T-367-14, T-367-15 | Guard tolak manual; pesan ramah (no leak V7); authz/antiforgery preserved | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ResetGuardTests"` | ✅ `ResetGuardTests.cs` | ✅ green (2 [Fact], SUMMARY 04) |
| 05-T1 | 05 | 3 | #19 (file sertifikat manual post-commit), L-08 | T-367-17, T-367-19 | File.Delete confined webroot post-commit warn-only; image 366 preserved (Opsi B separasi) | [Fact] file-on-disk / integration | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CertFileTab1Tests"` | ✅ `CertFileTab1Tests.cs` | ✅ green (4 [Fact] baru, SUMMARY 05) |
| 05-T2 | 05 | 3 | L-03 (pre-check BLOKIR tab 1 → cascade) | T-367-16, T-367-18 | No-blocker cascade via engine (preview==execute); 1-tx rollback; image 366 sekali (no dobel); audit | integration + full regresi | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CertFileTab1Tests"; dotnet test HcPortal.Tests` | ✅ `CertFileTab1Tests.cs` (extend) | ✅ green (271/271 full + 2× adversarial verify clean, SUMMARY 05) |
| 06-T1 | 06 | 3 | #1,L-06 (DeleteTabResult honesty split) | T-367-25 | Gagal → recordDeleteFailed + pesan generik (no ex.Message); sukses ≠ gagal | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RecordCascadeUiTests"` | ✅ `RecordCascadeUiTests.cs` | ✅ green (4 honesty [Fact], SUMMARY 06) |
| 06-T2 | 06 | 3 | L-03 preview (GET DeletePreview + partial _CascadePreviewModal) | T-367-22 | type whitelist {training,session}→BadRequest; partial anon-shape render runtime (Pitfall 6) | integration HTTP render + full regresi | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RecordCascadeUiTests"; dotnet test HcPortal.Tests` | ✅ `RecordCascadeUiTests.cs` (extend) | ✅ green (whitelist/preview unit; render partial = Playwright 08-T2 SC2 PASS, no HTTP infra Pitfall 6) |
| 06-T3 | 06 | 3 | #2,#3,#4,L-03,L-07 (generik + cascade via engine) | T-367-21, T-367-23, T-367-24 | Gate IsManualEntry dihapus tapi antiforgery/authz preserved; mirror IDOR diteruskan engine; no-blocker; pesan generik | integration + full regresi | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~RecordCascadeUiTests"; dotnet test HcPortal.Tests` | ✅ `RecordCascadeUiTests.cs` (extend) | ✅ green (281/281 full + D-19 6 [Theory] + adversarial verify, SUMMARY 06) |
| 07-T1 | 07 | 4 | #12,D-02 (AddManualAssessment guard EXACT reject) + ImportTrainingResult Skip | T-367-26, T-367-27 | EXACT user+judul+tanggal (no ±1 hari false-positive); reject single | unit | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~DuplicateGuardTests"` | ✅ `DuplicateGuardTests.cs` | ✅ green (5 predikat real-SQL, SUMMARY 07) |
| 07-T2 | 07 | 4 | #12,#14,D-02 (ImportTraining + BulkBackfill skip-with-report) | T-367-26, T-367-27, T-367-28 | EXACT match skip; intra-batch dedup; success counter tak inflate; existing antiforgery preserved | unit/integration + full regresi | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~DuplicateGuardTests"; dotnet test HcPortal.Tests` | ✅ `DuplicateGuardTests.cs` (extend) | ✅ green (4 dedup logic [Fact], 290/290 full, SUMMARY 07) |
| 08-T1 | 08 | 5 | #3,L-06,L-03 (UI: tombol online + rewire 3 → modal + flash S3) | T-367-29, T-367-30, T-367-31 | Anon 10-property terjaga (Pitfall 6); antiforgery di form Hapus Semua; flash gagal merah (no false-success) | build + full regresi (runtime → 08-T2) | `dotnet build; dotnet test HcPortal.Tests` | ✅ view + suite | ✅ green (build 0 err + 209 quick, SUMMARY 08) |
| 08-T2 | 08 | 5 | SC1,SC2,SC3,SC4,SC5 (UAT dual-path) | T-367-32 | Preview eksplisit (D-03) + audit + snapshot DB; honesty browser-verified; runtime render (Pitfall 6) | **Manual-Only (checkpoint)** | `npx playwright test delete-records-cascade --workers=1` (lihat Manual-Only) | ✅ `tests/e2e/delete-records-cascade.spec.ts` | ✅ green (Playwright 3/3 PASS @5277: SC1 DB-verified + SC2 preview + SC4 Rino; SC3 listener+unit, SC5 badge live+unit; SUMMARY 08) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Catatan threat coverage:** T-367-01..32 (32 threat lintas 8 plan) seluruhnya ter-map ke task di atas. Setiap threat punya disposition (mitigate/accept) + mitigation di `<threat_model>` plan masing-masing. Threat accept (T-367-03 preview info-disclosure admin-only, T-367-10 badge count, T-367-28 CSRF existing, T-367-32 hapus-tanpa-konfirmasi) terdokumentasi rasional di plan.

---

## Wave 0 Requirements

Infrastruktur test yang harus dibangun di task pertama yang membutuhkannya (bukan plan terpisah — di-bake ke Plan 01/02):

- [x] **Real-SQL integration fixture** untuk cascade assertions — disposable `HcPortalDB_Test_{Guid}` @localhost\SQLEXPRESS, `IAsyncLifetime` (`InitializeAsync` → `Database.MigrateAsync()`, `DisposeAsync` → `EnsureDeletedAsync()`), `[Trait("Category","Integration")]`. **Reuse pola `ImageCleanupIntegrationTests.cs:31-95` / `ProtonCompletionFixture` (Phase 360/366)** — framework xUnit sudah ada, no install. **DIBANGUN Plan 02 Task 2** (`RecordCascadeIntegrationTests.cs`) — 8 [Fact] real-SQL hijau (SUMMARY 02).
- [x] **Seed helper renewal-chain** — `SeedRenewalChainAsync(ctx, ...)`: `ApplicationUser` minimal DULU (FK `FK_AssessmentSessions_Users_UserId`, lesson 366 deviation), lalu chain: session induk → TrainingRecord anak (`RenewsSessionId=induk.Id`) → AssessmentSession cucu (`RenewsTrainingId=anak.Id`) + artefak per-tabel (EditLog/Response/AttemptHistory/UPA/Package+Q+O/PendingBypass(Status!="Dibatalkan")/UserNotification(ActionUrl="/CMP/StartExam/{id}")/ProtonFinalAssessment(Origin="Exam")). **DIBANGUN Plan 02 Task 2**, di-reuse Plan 05/06/07 + repro Playwright Plan 08. Deviasi seed: ProtonFinalAssessment Exam+Interview butuh track/assignment beda (unique-index) — reuse 2 track seeded (SUMMARY 02).
- [x] **Helper file temp webroot** — `MakeTempWebRoot` + `WriteFakeImage` (pola `ImageCleanupIntegrationTests:83-95`) untuk assert File.Delete post-commit (#19). **DIBANGUN Plan 02 Task 2** (`RecordCascadeFileTests.cs`, 3 [Fact]) + dipakai Plan 05 (`CertFileTab1Tests.cs`).
- [x] **Test scaffold files** (dibuat oleh task masing-masing): `RecordCascadeServiceTests.cs`, `MirrorHeuristicTests.cs`, `RecordCascadeIntegrationTests.cs`, `RecordCascadeFileTests.cs`, `BadgeRecomputeTests.cs`, `SiblingFilterTests.cs`, `ResetGuardTests.cs`, `CertFileTab1Tests.cs`, `RecordCascadeUiTests.cs`, `DuplicateGuardTests.cs`, `tests/e2e/delete-records-cascade.spec.ts` — **11/11 ADA di disk** (verified glob 2026-06-13).

> Framework xUnit sudah ada — **no install needed.** Fixture/seed pattern di-reuse dari Phase 360/366 (terbukti). `wave_0_complete: true` — fixture + seed helper + temp-webroot benar-benar dibangun & hijau saat eksekusi Plan 02 (11 [Fact] real-SQL pass).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| UI HTMX jujur — gagal → flash MERAH di partial, sukses → flash hijau + re-fetch (no false-success) | #1, L-06, SC1/SC3 | HTMX partial render + HX-Trigger event flow paling andal diverifikasi in-browser (build/test tak tangkap render runtime) | Playwright dual-path @5277, seed renewal-chain (Plan 08 Task 2 how-to-verify) |
| Render modal preview cascade (`_CascadePreviewModal` anon-shape) tanpa RuntimeBinderException/500 | L-03 preview, SC2, Pitfall 6 | Razor partial anon-shape LOLOS build tapi bisa error runtime (lesson Phase 354/371); browser = verifikasi definitif | Plan 08 Task 2: expand worker → klik hapus → modal muncul + daftar korban persis + mirror checkbox |
| Online >7 hari (kasus Rino) tampil + terhapus tuntas; DB 100% bersih per tabel | #3, #4, SC4 | End-to-end reproduksi + DB cross-check SQL per tabel butuh browser + sqlcmd | Plan 08 Task 2: seed online >7hari → hapus → `SELECT` per tabel bersih |
| Guard dup AddManualAssessment ditolak + badge↔list konsisten | #12, D-02, D-01, SC5 (deliverable Plan 07 + Plan 03) | Verifikasi UX form reject + konsistensi badge visual butuh browser | Plan 08 Task 2: AddManual dup exact → ditolak; expand worker → badge cocok baris |

**Automated companion:** `npx playwright test delete-records-cascade --workers=1` (DB isolation, lesson local-e2e SQL env). Spec `tests/e2e/delete-records-cascade.spec.ts` di-commit di Plan 08 Task 2. Seed Workflow WAJIB: snapshot DB → seed → restore + journal `cleaned` (docs/SEED_WORKFLOW.md). AD lokal: `Authentication__UseActiveDirectory=false dotnet run`.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (14 auto-tasks mapped; 1 checkpoint = Manual-Only dengan Playwright companion)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (setiap task auto punya `dotnet test`/`dotnet build`; checkpoint Plan 08 Task 2 didahului 08-T1 build+suite)
- [x] Wave 0 covers all MISSING references (fixture real-SQL + seed renewal-chain + temp webroot helper dibangun Plan 02 Task 2, di-reuse downstream)
- [x] No watch-mode flags (semua `dotnet test`/`dotnet build` one-shot; Playwright `--workers=1` one-shot)
- [x] Feedback latency < 60s (quick filter `Category!=Integration` < 30s; full suite per wave)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** ✅ VALIDATED (post-execution audit 2026-06-13) — 16 task seluruhnya COVERED + green; `wave_0_complete: true` (fixture+seed+temp-webroot hijau di Plan 02); `nyquist_compliant: true`.

---

## Validation Audit 2026-06-13

State A audit — rekonsiliasi VALIDATION.md draft (pre-eksekusi, semua ⬜/❌ W0) terhadap artefak eksekusi aktual (8 SUMMARY) + verifikasi disk + re-run quick suite.

| Metric | Count |
|--------|-------|
| Tasks di Per-Task Map | 16 (15 auto + 1 checkpoint) |
| Gaps found (MISSING) | 0 |
| COVERED (green) | 16 |
| Resolved (auditor-filled) | 0 (tak perlu — semua test sudah dibuat saat eksekusi TDD) |
| Escalated to Manual-Only | 0 (Manual-Only section pre-existing tetap; companion Playwright sudah PASS) |

**Bukti:**
- 11/11 file test ADA di disk (10 xUnit + 1 e2e), verified glob.
- Quick suite re-run 2026-06-13: **209/209 passed** (`Category!=Integration`, ~1s).
- Full suite per SUMMARY: 290/290 (209 quick + 81 integration real-SQL), terakhir di Plan 07.
- e2e `delete-records-cascade.spec.ts` 3/3 PASS @5277 (SC1 DB-verified, SC2 preview, SC4 Rino) — Plan 08.
- +61 [Fact]/[Theory] baru lintas 8 plan; 0 regresi; Migration=FALSE seluruh phase.
- Catatan: 06-T2 render partial `_CascadePreviewModal` tak di-unit-test (no WebApplicationFactory/TestServer, Pitfall 6) → ter-cover Playwright 08-T2 SC2 (Manual-Only companion, sudah hijau). Bukan gap.

**Hasil:** Phase 367 NYQUIST-COMPLIANT. Tak ada test perlu di-generate. Auditor (gsd-nyquist-auditor) tidak di-spawn — zero gap.
