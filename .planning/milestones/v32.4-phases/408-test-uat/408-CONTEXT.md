# Phase 408: Test & UAT - Context

**Gathered:** 2026-06-22
**Status:** Ready for planning

<domain>
## Phase Boundary

Capstone Test & UAT milestone v32.4 Ujian Ulang (RTK-14). Membuktikan seluruh kemampuan ujian ulang benar end-to-end, MENGISI GAP test yang belum ada (bukan menulis ulang yang sudah hijau). 0 migration.

**Sudah ADA + hijau (jangan tulis ulang, cukup konfirmasi tak regresi):**
- xUnit: `RetakeRulesTests` (22/22 incl 6 ResolveReviewMode), `RetakeArchiveBuilderTests`, `RetakeServiceTests`, `RetakeSettingsEndpointTests`, `RetakeExamEndpointTests` (3/3), `RiwayatUnifierTests`.
- Playwright: `retake-config-406.spec.ts` (6), `riwayat-hc-406.spec.ts` (5), `retake-worker-407.spec.ts` (6) — semua hijau @5270.
- Secure: 406 SECURED 13/13, 407 SECURED 9/9.

**GAP yang DIBANGUN di 408:**
1. **Playwright lifecycle penuh (1 happy-path)** — satu alur end-to-end: gagal → skor + tanda-salah (kunci hidden) → klik "Ujian Ulang" → (cooldown=0) ambil ulang → lulus → **1 sertifikat terbit**. Ini yang belum ada (smoke 406/407 menguji per-surface, bukan alur retake yang BENAR-BENAR mengeksekusi ExecuteAsync → StartExam → lulus → cert).
2. **xUnit integration (real-SQL)** — retake-then-pass → **tepat 1 cert** (guard anti-double-cert existing) + counting `(UserId, Title, Category)` **tidak konflasi** Pre/Post ber-Title sama. (Konfirmasi/lengkapi bila belum eksplisit di RetakeServiceTests.)
3. **Secure-phase 408** — gerbang security milestone terkonsolidasi (RBAC worker-own-only, antiforgery, server-side cooldown/cap revalidation, no answer-key leak saat retake-eligible).

Out of scope: fitur/UI baru, migration, perubahan engine grading.
</domain>

<decisions>
## Implementation Decisions

### Gray areas di-discuss (2026-06-22)

- **D-01 (Cakupan lifecycle e2e):** **1 alur happy-path penuh.** Satu Playwright test end-to-end: sesi gagal (failed + attempt-sisa, AllowAnswerReview=true, cooldown=0) → halaman Hasil tampil skor + ✓/✗ tanpa kunci (ShowWrongFlagsOnly) → klik "Ujian Ulang" → modal konfirmasi → POST RetakeExam → redirect StartExam (token re-required) → ambil ulang dengan jawaban benar → lulus → **sertifikat terbit (cert# muncul)**. Cabang lock (cap habis) & cooldown-aktif sudah dibuktikan smoke 407 (skenario 5 & 6) — TIDAK diulang di sini. @5270 + seed/restore (SEED_WORKFLOW).

- **D-02 (retake-then-pass → 1 cert + counting):** **xUnit integration real-SQL (@SQLEXPRESS), deterministik.** Test: setup sesi gagal → `RetakeService.ExecuteAsync` → simulasi ambil-ulang lulus (grade) → assert **tepat 1 sertifikat** (guard anti-double-cert existing tidak terbit dobel) + counting `(UserId, Title, Category)` tak campur Pre/Post ber-Title sama (Pre & Post Title sama → attempt terhitung terpisah per Category/type, tak konflasi). Bukan Playwright (hindari flake; cert# sudah dicek visual di smoke 406). Mirror fixture `RetakeServiceTests` (NoOpHubContext + NullLogger + real-SQL).

- **D-03 (Security milestone):** **Secure-phase 408 baru (gerbang formal).** Jalankan `gsd-secure-phase 408` dengan threat model konsolidasi milestone — meski 406 (13/13) + 407 (9/9) sudah cover, 408 menjadi gerbang penutup yang menegaskan seluruh permukaan retake aman tanpa regresi: RBAC (worker hanya sesi sendiri — IDOR), antiforgery, server-side cooldown/cap revalidation, no answer-key leak saat retake-eligible. Plan 408 WAJIB sertakan `<threat_model>` block.

### Claude's Discretion
- Struktur file test integration (folder/namespace, mirror RetakeServiceTests) — planner pilih.
- Apakah counting no-conflate sudah ter-cover di RetakeServiceTests existing → kalau ya, cukup tambah assert eksplisit; kalau tidak, test baru.
- Detail seed lifecycle e2e (reuse pola retake-worker-407 seed + tambah jalur lulus pasca-retake).
- Apakah perlu test regresi eksplisit guard 391/398.1 dll (andalkan full-suite run).
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Spec & milestone
- `docs/superpowers/specs/2026-06-19-attempt-retake-assessment-design.md` — AUTHORITATIVE (RTK-14 test scope).
- `.planning/phases/405-backend-core-data-retakerules-retakeservice-refactor-reset-config-endpoint/405-CONTEXT.md` — signature service/rules + must-fix.
- `.planning/phases/407-worker-self-service-gating-tier-feedback-riwayat-pekerja/407-UAT.md` + `407-SECURITY.md` — pola UAT + threat model worker.
- `.planning/phases/406-admin-config-ui-riwayat-hc/406-SECURITY.md` — threat model admin/HC.

### Kode + test existing (read sebelum implement)
- `Services/RetakeService.cs` — `ExecuteAsync` (claim-atomik, cert via GradingService/CertNumberHelper), `CanRetakeAsync`.
- `Helpers/RetakeRules.cs`, `Helpers/RetakeArchiveBuilder.cs`, `Helpers/RiwayatUnifier.cs`, `Helpers/AssessmentScoreAggregator.cs`, `Helpers/CertNumberHelper.cs`.
- `HcPortal.Tests/RetakeServiceTests.cs` (fixture real-SQL: NoOpHubContext + NullLogger), `RetakeRulesTests.cs`, `RetakeExamEndpointTests.cs`.
- `tests/e2e/retake-worker-407.spec.ts` + `tests/sql/retake-worker-407-seed.sql` (pola lifecycle seed), `tests/e2e/helpers/` (login, dbSnapshot, examTypes wizard).
- `Controllers/CMPController.cs` (RetakeExam, StartExam, Results), `Controllers/AssessmentAdminController.cs` (ResetAssessment, grade flow).
</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RetakeServiceTests` fixture (real-SQL @SQLEXPRESS, NoOpHubContext, NullLogger) — reuse untuk integration retake-then-pass.
- `retake-worker-407.spec.ts` + seed — pola lifecycle e2e (login coachee, gotoResults, dbSnapshot BACKUP/RESTORE). Lifecycle test extend ini: tambah jalur klik-Ujian-Ulang → StartExam → submit lulus → cert.
- Grading + CertNumberHelper (cert# format KPB/xxx/VI/2026) — assert 1 cert.

### Established Patterns
- Integration test = `[Trait("Category","Integration")]` real-SQL; unit = InMemory. Quick filter `Category!=Integration`.
- e2e combined run WAJIB `--workers=1`; SEED_WORKFLOW snapshot→seed→restore via global.setup/dbSnapshot.

### Integration Points
- Lifecycle e2e: Results → RetakeExam (POST) → StartExam → exam-taking flow → submit → grade → Results (lulus) → cert. Menyentuh exam-taking flow (yang 406/407 hindari) — INILAH cakupan 408.
</code_context>

<specifics>
## Specific Ideas
- Lifecycle e2e cooldown=0 supaya tombol langsung aktif (tak perlu tunggu jeda) — fokus alur retake→lulus→cert.
- Integration assert: `Certificates`/cert# count == 1 pasca retake-then-pass; AssessmentAttemptHistory + Archive konsisten.
</specifics>

<deferred>
## Deferred Ideas
None — milestone capstone; semua dalam scope RTK-14.
</deferred>

---

*Phase: 408-test-uat*
*Context gathered: 2026-06-22*
