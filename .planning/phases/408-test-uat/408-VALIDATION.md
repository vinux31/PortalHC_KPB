---
phase: 408
slug: test-uat
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-22
validated: 2026-06-22
---

# Phase 408 — Validation Strategy

> Capstone test phase. Sebagian besar coverage SUDAH hijau (regresi); 408 isi 3 gap (retake→1cert integration, lifecycle e2e, secure gate).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (unit+integration real-SQL @SQLEXPRESS) + @playwright/test (e2e, baseURL `E2E_BASE_URL=http://localhost:5270`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` ; `tests/playwright.config.ts` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (unit, SQL-less) |
| **Full suite command** | `dotnet test HcPortal.Tests` (incl Integration) + `cd tests && E2E_BASE_URL=http://localhost:5270 npx playwright test --workers=1` (app @5270) |
| **Estimated runtime** | unit <30s; integration per-run; e2e per-run |

---

## Sampling Rate

- **Per task commit:** `dotnet build` 0 error + `dotnet test --filter "Category!=Integration"` (unit quick).
- **Per wave:** full `dotnet test` (incl Integration @SQLEXPRESS) + spec e2e baru @5270.
- **Phase gate:** seluruh suite hijau (existing tak regresi + GAP-1) + lifecycle e2e hijau + secure 408 `threats_open:0` sebelum `/gsd-verify-work`.
- **Max latency:** unit <30s.

---

## Per-Task Verification Map

| Requirement | Behavior | Test Type | Automated Command | File Exists | Status |
|-------------|----------|-----------|-------------------|-------------|--------|
| RTK-14 unit RetakeRules | CanRetake + ShouldHide + ResolveReviewMode semua cabang | unit | `dotnet test --filter ~RetakeRulesTests` | ✅ 22 Fact | ⬜ regresi |
| RTK-14 unit ArchiveBuilder | snapshot verdict + essay full-text | unit | `dotnet test --filter ~RetakeArchiveBuilderTests` | ✅ 4 Fact | ⬜ regresi |
| RTK-14 unit Riwayat | unify DESC + grouping strict | unit | `dotnet test --filter ~RiwayatUnifierTests` | ✅ 6 Fact | ⬜ regresi |
| RTK-14 integ RetakeService | claim-atomik, snapshot-before-delete, counting no-conflate | integration | `dotnet test --filter ~RetakeServiceTests` | ✅ 8 Fact (incl Counting_PrePostSameTitle_NoConflate) | ⬜ regresi |
| RTK-14 endpoint worker | IDOR Forbid / not-eligible redirect / success token-clear | integration | `dotnet test --filter ~RetakeExamEndpointTests` | ✅ 3 Fact | ⬜ regresi |
| **RTK-14 integ retake→pass→1 cert** | retake → grade lulus → tepat 1 NomorSertifikat (anti-double-cert) | integration | `dotnet test --filter ~RetakeThenPassCert` | ❌ **Wave 0 GAP-1** | ⬜ pending |
| **RTK-14 e2e lifecycle penuh** | gagal→skor+✓/✗ no-key→Ujian Ulang→modal→StartExam→jawab benar→lulus→cert# | e2e | `npx playwright test retake-lifecycle-408.spec.ts --workers=1` | ❌ **Wave 0 GAP-3** | ⬜ pending |
| RTK-14 smoke per-surface | config/riwayat-HC 406 + worker 407 | e2e | `npx playwright test retake-config-406 riwayat-hc-406 retake-worker-407 --workers=1` | ✅ 6+5+6 | ⬜ regresi |
| RTK-14 security gate | RBAC/CSRF/cooldown-cap/no-leak konsolidasi | secure-phase | `gsd-secure-phase 408` | ❌ **Wave 0 GAP-secure** | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/RetakeThenPassCertTests.cs` — retake→pass→1 cert (reuse RetakeServiceFixture real-SQL; OQ-1: baca ctor GradingService, fallback Opsi-B controller-endpoint bila deps berat). Covers RTK-14 inti.
- [ ] `tests/e2e/retake-lifecycle-408.spec.ts` + `tests/sql/retake-lifecycle-408-seed.sql` — lifecycle penuh (gagal cooldown=0 → Ujian Ulang → ambil ulang lulus → cert#); reuse submitExamTwoStep + label-by-text + login + dbSnapshot.
- [ ] Plan secure-phase 408 dengan `<threat_model>` konsolidasi (407 register + invariant cert-uniqueness).
- [ ] Framework install: NONE (toolchain lengkap).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Cooldown countdown ticking visual | RTK-14/RTK-10 | timing JS visual | sudah dicover smoke 407 sc-6 (live @5270) — regresi run |

*Sisanya otomatis (unit + integration + e2e + secure gate).*

---

## Validation Sign-Off

- [ ] All tasks automated verify or Wave 0 deps
- [ ] Wave 0 covers GAP-1 (retake→cert) + GAP-3 (lifecycle e2e) + secure gate
- [ ] Lifecycle e2e verified live @5270 (real-browser, lesson 354/413)
- [ ] No watch-mode flags
- [x] `nyquist_compliant: true` set after execution

**Approval:** APPROVED 2026-06-22

---

## Validation Audit 2026-06-22

| Metric | Count |
|--------|-------|
| Gaps found | 1 (e2e lifecycle fixture-bug) |
| Resolved | 1 |
| Escalated | 0 |

Wave-0 gaps semua tertutup + hijau:
- **GAP-1** `RetakeThenPassCertTests` (retake→1 cert) — green (Integration real-SQL).
- **GAP-3** `retake-lifecycle-408.spec.ts` — green @5270 (LULUS 100% + cert). Awalnya RED → menyingkap fixture-bug seed (`QuestionType='SingleAnswer'` invalid → grade skip→0%); fixed → `MultipleChoice`. Produk terbukti benar.
- **GAP-secure** — `gsd-secure-phase 408` SECURED 13/13 threats_open:0.
- Regresi: full xUnit **614/0/2** + e2e **19/19** (lifecycle + 407×6 + 406×11). NYQUIST-COMPLIANT.
