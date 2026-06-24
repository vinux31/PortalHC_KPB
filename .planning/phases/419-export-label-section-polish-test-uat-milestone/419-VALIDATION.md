---
phase: 419
slug: export-label-section-polish-test-uat-milestone
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-24
validated: 2026-06-24
---

# Phase 419 — Validation Strategy

> Per-phase validation contract. Finalized 2026-06-24 after Wave-0 tests + cross-milestone e2e all GREEN.
> Detailed mapping in 419-RESEARCH.md §"Validation Architecture".

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e, @playwright/test) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~ExportSectionLabelTests\|FullyQualifiedName~SectionEtWarningTests"` |
| **Full suite command** | `dotnet test` (xUnit, 695/0/0) + `npx playwright test section-lifecycle-419 inject-section-419 linkprepost-section-419 addremove-section-419 --workers=1` (app live @5277) |
| **Estimated runtime** | xUnit ~90–165s · Playwright per-spec ~17–40s (serial, DB backup/restore) |

---

## Sampling Rate

- **After every task commit:** quick filtered `dotnet test`
- **After every plan wave:** full `dotnet test`
- **Before ship:** full xUnit green + 4 cross-milestone Playwright specs green live @5277

---

## Per-Task Verification Map

| Requirement / Decision | Test Type | Test File | Automated Command | Status |
|------------------------|-----------|-----------|-------------------|--------|
| PAG-04 — Excel band-header "Section {n}: {Nama}" (aggregate sheet) | unit (real-SQL) | HcPortal.Tests/ExportSectionLabelTests.cs (BandHeader_RendersSectionLabelRow, BandHeader_OrdersBySectionNumberThenOrder) | `dotnet test --filter ExportSectionLabelTests` | ✅ green |
| PAG-04 — Excel backward-compat (no Section = legacy, no band) | unit | ExportSectionLabelTests.NoSection_BackwardCompat | same | ✅ green |
| PAG-04 — per-peserta Excel "Detail Jawaban" Section heading (review fix #1) | unit | ExportSectionLabelTests.PerPesertaDetail_RendersSectionHeadingsInOrder + PerPesertaDetail_NoSection_BackwardCompat | same | ✅ green |
| PAG-04 — Excel band live (real file) + PDF heading | e2e | tests/e2e/section-lifecycle-419.spec.ts (JSZip sharedStrings "Section 1/2: {Nama}") + curl-UAT extract_text PDF | `npx playwright test section-lifecycle-419 --workers=1` | ✅ green live @5277 |
| D-03 / DEF-416-01 — ET-warning cross-sibling pool fires (DistinctEt > K) | unit (real-SQL) | HcPortal.Tests/SectionEtWarningTests.cs (CrossSiblingPool_Fires, GroupBySectionNumber_NotSectionId, FullCoverage_NoWarning_NonBlocking) | `dotnet test --filter SectionEtWarningTests` | ✅ green |
| D-03 — K = min DISTINCT-ET per package (repeated-ET sibling false-negative regression) | unit | SectionEtWarningTests.RepeatedEtInSibling_Fires (de-tautology: fails under old raw-count K) | same | ✅ green |
| D-04.1 — lifecycle: Section + A–F + pagination + resume + export | e2e | tests/e2e/section-lifecycle-419.spec.ts | `npx playwright test section-lifecycle-419 --workers=1` | ✅ green live @5277 |
| D-04.2 — Inject × Section + opsi 5–6 (preview==commit, all-Lainnya, cert/per-soal) | e2e | tests/e2e/inject-section-419.spec.ts | `npx playwright test inject-section-419 --workers=1` | ✅ green live @5277 |
| D-04.3 — LinkPrePost × Section koherensi (online untouched, Section utuh) | e2e | tests/e2e/linkprepost-section-419.spec.ts | `npx playwright test linkprepost-section-419 --workers=1` | ✅ green live @5277 |
| D-04.4 — Add/Remove × Section + pagination (eager per-section, rino unaffected) | e2e | tests/e2e/addremove-section-419.spec.ts | `npx playwright test addremove-section-419 --workers=1` | ✅ green live @5277 |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [x] xUnit: export label Section (Excel band-header + per-peserta heading + backward-compat no-Section), ET-warning cross-sibling fire — ExportSectionLabelTests (5) + SectionEtWarningTests (4) GREEN.
- [x] Reuse `SectionFixture` (real-SQLEXPRESS disposable) — Phase 415/416 infra reused.
- [x] Playwright specs for 4 D-04 UAT scenarios — section-lifecycle / inject-section / linkprepost-section / addremove-section -419, all un-fixme'd + GREEN live.

*LinkPrePost section-guard test (originally planned Wave-0) DROPPED with D-02 → backlog 999.16: InjectQuestionSpec has no SectionId → inject packages always all-Lainnya → guard would be no-op. Replaced by D-04.3 coherence e2e (link succeeds, online untouched, Section intact).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Status |
|----------|-------------|------------|--------|
| (none outstanding) | — | The 4 cross-milestone scenarios that were manual-only are now AUTOMATED Playwright e2e (GREEN live @5277). Visual band-header/PDF heading covered by JSZip sharedStrings assert (e2e) + extract_text (curl UAT). | ✅ automated |

---

## Validation Sign-Off

- [x] All requirements have automated verify (xUnit + Playwright)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all references (LinkPrePost-guard intentionally dropped → 999.16)
- [x] No watch-mode flags
- [x] Feedback latency < ~165s (xUnit) + per-spec e2e
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-24 — PAG-04 + D-03 + D-04 all automated-green (xUnit 695/0/0 incl. 9 phase-419 tests + 4 cross-milestone e2e GREEN live @5277).

## Validation Audit 2026-06-24
| Metric | Count |
|--------|-------|
| Gaps found | 0 (all requirements COVERED by existing green tests) |
| Resolved | 0 (no test-gen needed) |
| Escalated | 0 |
