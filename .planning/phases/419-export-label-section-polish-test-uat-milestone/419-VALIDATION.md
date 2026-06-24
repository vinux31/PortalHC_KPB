---
phase: 419
slug: export-label-section-polish-test-uat-milestone
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 419 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detailed mapping in 419-RESEARCH.md §"Validation Architecture". Planner fills the Per-Task map.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e, @playwright/test) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test --filter "ExportLabel\|SectionEtWarning\|LinkPrePostSectionGuard"` |
| **Full suite command** | `dotnet test` (xUnit) + `npx playwright test --workers=1` (e2e, app live @5277) |
| **Estimated runtime** | xUnit ~60–90s · Playwright per-spec ~30–90s (serial, DB backup/restore) |

---

## Sampling Rate

- **After every task commit:** Run quick filtered `dotnet test`
- **After every plan wave:** Run full `dotnet test`
- **Before `/gsd-verify-work`:** Full xUnit green + targeted Playwright specs green
- **Max feedback latency:** ~90 seconds (xUnit); Playwright manual gate (live @5277)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (planner fills) | — | — | PAG-04 / D-02 / D-03 | — | — | unit/e2e | — | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] xUnit test stubs: export label Section (Excel band-header + PDF heading + backward-compat no-Section), ET-warning positive (cross-sibling pool fire), LinkPrePost section-guard (block-on-mismatch + skip-on-all-Lainnya)
- [ ] Reuse `SectionFixture` + `AddPackageWithSectionsAsync` (real-SQLEXPRESS disposable) — already exists (Phase 415/416)
- [ ] Playwright spec stubs for 4 D-04 UAT scenarios (reuse scoped-shuffle/inject-397/flexible-participant patterns)

*Most infrastructure exists (415–418). Wave 0 adds new test files only.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| 4 cross-milestone UAT live @5277 (Lifecycle Section / Inject×Section / LinkPrePost×Section / Add-Remove×Section) | PAG-04 + D-04 | Lesson 354 — Razor/JS/SignalR WAJIB real-browser; checkpoint blocking orchestrator | Login admin@pertamina.com, snapshot DB, jalankan 4 skenario, RESTORE DB (SEED_WORKFLOW) |
| Visual band-header Excel + heading PDF | PAG-04 | Rendering dokumen (ClosedXML/QuestPDF) — verifikasi mata pada file hasil | Buka .xlsx + .pdf hasil export, cek "Section {n}: {Nama}" + huruf A–F |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
