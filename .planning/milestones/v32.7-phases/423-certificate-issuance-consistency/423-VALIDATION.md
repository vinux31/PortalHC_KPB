---
phase: 423
slug: certificate-issuance-consistency
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-24
validated: 2026-06-24
---

# Phase 423 — Validation Strategy

> Per-phase validation contract untuk fase Certificate Issuance Consistency.
> Diisi oleh planner (Per-Task Verification Map) + gsd-validate-phase.
> Strategi arsitektur: lihat `423-RESEARCH.md` §"Validation Architecture".

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test` (Integration butuh SQLEXPRESS live) |
| **Estimated runtime** | ~60-120 detik (unit), lebih lama dgn integration |

---

## Sampling Rate

- **After every task commit:** Run quick (non-integration) suite
- **After every plan wave:** Run full suite (incl real-SQL integration @SQLEXPRESS)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~120 detik

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Secure Behavior | Test Type | Automated Command | File | Status |
|---------|------|------|-------------|-----------------|-----------|-------------------|------|--------|
| 423-01-T1 | 01 | 1 | CERT-01/02/04/06/07 | ShouldIssueCertificate tolak PreTest; DeriveValidUntil Permanent/Annual/3-Year; ResemblesAutoCertFormat regex; PendingAgeBadgeClass thresholds | unit (pure) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CertIssuanceRulesTests"` | `HcPortal.Tests/CertIssuanceRulesTests.cs` | ✅ green (20/20) |
| 423-02-T3 | 02 | 2 | CERT-01/02/03/05 | PreTest no-cert via GradeAndCompleteAsync; exactly-1-cert kanonik; ValidUntil Annual+1y; anti-dup block+renewal-exempt; seq-fail signal queryable | integration (real-SQL) | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CertIssuanceIntegrationTests"` | `HcPortal.Tests/CertIssuanceIntegrationTests.cs` | ✅ green (5/5) |
| 423-03-T3 | 03 | 3 | CERT-07 | Badge umur PendingGrading render di EssayGrading + AssessmentMonitoringDetail, warna >3 kuning/>7 merah, tanpa auto-finalize | UAT manual | `dotnet build` (Razor compile) + UAT @5270 | `Views/Admin/EssayGrading.cshtml`, `Views/Admin/AssessmentMonitoringDetail.cshtml` | ✅ green (UAT approved) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 — Status COMPLETE

| Check | Status | Keterangan |
|-------|--------|-----------|
| `HcPortal.Tests/CertIssuanceRulesTests.cs` — pure truth-table CERT-01/02/04/06/07 | ✅ 20/20 PASS | ShouldIssueCertificate 8 case + IsPassed-null Fact; DeriveValidUntil 4 case + null Fact; ResemblesAutoCertFormat 7 InlineData + null/empty/whitespace Fact; PendingAgeBadgeClass thresholds + boundary exact |
| `HcPortal.Tests/CertIssuanceIntegrationTests.cs` — integration real-SQL CERT-01/02/03/05 | ✅ 5/5 PASS | CERT01_PreTest_GradesButNeverIssuesCertificate; CERT03_PostTestPassing_IssuesExactlyOneCertificate_CanonicalFormat; CERT02_PostTestAnnual_DerivesValidUntilPlusOneYear; CERT05_AntiDup_ActiveBlocks_ExpiredPasses_RenewalExempt; CERT03_SeqFailSignal_QueryablePredicate_FindsStuckSession |

**Diverifikasi:** `dotnet test HcPortal.Tests --filter "FullyQualifiedName~CertIssuance"` → **25/25 PASS**

---

## Coverage Map CERT-01..07

| Requirement | Coverage | Test | Verdict |
|-------------|----------|------|---------|
| CERT-01 PreTest reject (helper) | CertIssuanceRulesTests ShouldIssueCertificate_TruthTable (8 case + IsPassed-null) | unit | ✅ |
| CERT-01 SITE 1 GradeAndCompleteAsync (live) | CertIssuanceIntegrationTests CERT01_PreTest… | integration | ✅ |
| CERT-01 SITE 2 RegradeAfterEditAsync | Covered by helper unit + SITE 1 integration (logika gate identik via helper; SITE 2 sudah punya cek PreTest sebelum 423, seragamkan ke helper) | unit (helper) | ✅ accepted |
| CERT-01 SITE 3 FinalizeEssayGrading | **Accepted manual-only** (lihat §Gap Assessment SITE 3) | UAT 423-03-T3 | ✅ accepted |
| CERT-01 SITE 4 AddManualAssessment | Accepted: manual tidak melewati ShouldIssueCertificate auto-gate (IsManualEntry, nomor diisi langsung); ResemblesAutoCertFormat mencegah namespace-clash (CERT-04 | UAT 423-03-T3 | ✅ accepted |
| CERT-02 ValidUntil Annual derive | CertIssuanceRulesTests DeriveValidUntil_FromCompletedAt; CertIssuanceIntegrationTests CERT02_… | unit + integration | ✅ |
| CERT-02 ValidUntil Permanent/3-Year | CertIssuanceRulesTests DeriveValidUntil_FromCompletedAt | unit | ✅ |
| CERT-03 seq atomik exactly-1-cert | CertIssuanceIntegrationTests CERT03_PostTestPassing… | integration | ✅ |
| CERT-03 seq-fail signal queryable | CertIssuanceIntegrationTests CERT03_SeqFailSignal… | integration | ✅ |
| CERT-04 namespace reject regex | CertIssuanceRulesTests ResemblesAutoCertFormat_Regex (7 case) + ResemblesAutoCertFormat_NullEmptyWhitespace | unit | ✅ |
| CERT-05 anti-dup block/expired/renewal | CertIssuanceIntegrationTests CERT05_AntiDup… | integration | ✅ |
| CERT-06 Permanent → ValidUntil null | CertIssuanceRulesTests DeriveValidUntil_FromCompletedAt (Permanent→null) | unit | ✅ |
| CERT-07 badge umur PendingGrading (>3 kuning/>7 merah) | CertIssuanceRulesTests PendingAgeBadgeClass_Thresholds + PendingAgeBadgeClass_BoundaryExact; UAT 423-03-T3 | unit + UAT | ✅ |

---

## Gap Assessment — SITE 3 FinalizeEssayGrading (CR-01)

**Gap:** Fix CR-01 (sinkron in-memory `session.IsPassed/Score/Status/CompletedAt` sebelum gate cert di `AssessmentAdminController.FinalizeEssayGrading`) tidak ter-cover oleh automated integration test yang memanggil controller HTTP secara langsung.

**Keputusan: ACCEPTED (manual-only)**

Justifikasi:
1. **Test langsung tidak praktis** — `FinalizeEssayGrading` adalah HTTP POST controller yang membutuhkan `WebApplicationFactory` + middleware stack (RBAC, antiforgery, SignalR hub) yang tidak tersedia di fixture `RetakeServiceFixture` yang dipakai proyek. Menambahkan `WebApplicationFactory` = scope creep besar, bukan behavioral test minimal.
2. **Mirror dari SITE 1/2 yang sudah ter-test** — logika sinkron in-memory adalah **pola yang persis sama** seperti yang sudah diimplementasi dan diuji di `GradingService` SITE 1 (`GradeAndCompleteAsync`) dan SITE 2 (`RegradeAfterEditAsync`). `CertIssuanceIntegrationTests.CERT01_PreTest_GradesButNeverIssuesCertificate` membuktikan bahwa gate `ShouldIssueCertificate` bekerja benar saat in-memory state akurat.
3. **Unit test gate pure sudah hijau** — `CertIssuanceRulesTests.ShouldIssueCertificate_TruthTable` membuktikan gate itu sendiri correct. Bug CR-01 bukan pada gate (yang sudah ter-unit-test), tetapi pada sinkron state sebelum gate — yang adalah pola EF boilerplate standar.
4. **Verification pasca-fix sudah dilakukan** — `dotnet test --filter Cert|RetakeThenPassCert|EssayFinalize` → **64/64 PASS** (dari 423-REVIEW-FIX.md) + UAT manual Task 3 Plan 03 yang meng-verify behavior end-to-end dari halaman EssayGrading.
5. **Risiko residual rendah** — `completedAtSync` di-set dari query DB aktual (bukan parameter request), sinkron 4 field string/bool/DateTime adalah idiomatic EF after-ExecuteUpdateAsync, dan PRE-cert gate adalah pure function yang bisa diprediksi.

**Mitigation:** UAT 423-03-T3 §6 (opsional sanity Wave 2): finalize satu sesi essay lulus PostTest → cert terbit 1× format kanonik; finalize sesi essay PreTest → TIDAK ada cert.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Badge umur PendingGrading (warna >3hr/>7hr) di EssayGrading + ManageAssessment | CERT-07 | Render Razor + warna ambang | UAT @5270 (423-03-T3): 3 sesi seed (2/5/8 hari) → badge abu/kuning/merah; no auto-finalize |
| SITE 3 FinalizeEssayGrading cert path (CR-01 fix) | CERT-01 | Controller HTTP path; WebApplicationFactory tidak ada di suite | UAT 423-03-T3 §6: finalize essay PostTest → cert KPB/NNN/ROMAN/TAHUN; essay PreTest → null |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify atau accepted-manual dengan justifikasi
- [x] Sampling continuity: tidak ada 3 task berturut-turut tanpa automated verify
- [x] Wave 0 covers semua MISSING references (unit + integration tests ada)
- [x] No watch-mode flags
- [x] Feedback latency < 120s (unit ~112ms, integration ~2-13s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** NYQUIST-COMPLIANT — 25/25 automated tests PASS; 0 gaps unaccounted; SITE 3 gap accepted dengan justifikasi + UAT coverage.
