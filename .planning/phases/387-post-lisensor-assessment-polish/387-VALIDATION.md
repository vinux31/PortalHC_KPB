---
phase: 387
slug: post-lisensor-assessment-polish
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-15
---

# Phase 387 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test` + `cd tests; npx playwright test --workers=1` |
| **Estimated runtime** | ~60s unit · ~120s e2e |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + relevant unit filter
- **After every plan wave:** Run `dotnet test --filter "Category!=Integration"`
- **Before `/gsd-verify-work`:** Full suite + `dotnet run` localhost:5277 manual check
- **Max feedback latency:** ~60 seconds

---

## Per-Task Verification Map

*Filled by planner / nyquist-auditor. Each REQ maps to its verify type per CONTEXT D-09:*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | — | — | PXF-06 | — | Block SubmitEssayScore when Status==Completed; allow during PendingGrading | unit | `dotnet test --filter SubmitEssayScore` | ❌ W0 | ⬜ pending |
| TBD | — | — | PXF-08 | — | Cert number retry 3x + log + surface error to HC | unit/manual | TBD | ❌ W0 | ⬜ pending |
| TBD | — | — | PXF-09 | — | BulkExport "Detail Jawaban" shows essay score/text | unit | TBD | ❌ W0 | ⬜ pending |
| TBD | — | — | PXF-10 | — | FinalizeEssayGrading broadcasts to monitor group | manual | — | — | ⬜ pending |
| TBD | — | — | PXF-11 | — | Results + ExamSummary option image aria has A/B/C/D letter | e2e (Playwright) | `npx playwright test` | ❌ W0 | ⬜ pending |
| TBD | — | — | PXF-12 | — | SubmitExam MC no null-overwrite when question absent from answers | unit | TBD | ❌ W0 | ⬜ pending |
| TBD | — | — | PXF-13 | — | SaveTextAnswer rejects write after timer expired (mirror SaveMultipleAnswer) | unit/manual | TBD | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Unit test stubs for PXF-06 (status guard), PXF-09 (essay cell), PXF-12 (MC null-overwrite)
- [ ] Playwright stub for PXF-11 (aria letter — 2 surfaces: Results + ExamSummary)

*PXF-08/10/13 may be manual+build per CONTEXT D-09 if controller/hub harness is unavailable.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Monitor tab live-updates on finalize | PXF-10 | SignalR multi-tab real-time | Open monitor in 2 tabs; finalize essay grading; confirm other tab updates without refresh |
| Cert-number collision error surfaced | PXF-08 | DbUpdateException collision is hard to force in unit | Manual/log inspection; confirm HC sees error message on persistent failure |

*If none: "All phase behaviors have automated verification."*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
