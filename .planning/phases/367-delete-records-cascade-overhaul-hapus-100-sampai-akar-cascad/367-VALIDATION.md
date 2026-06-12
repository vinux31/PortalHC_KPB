---
phase: 367
slug: delete-records-cascade-overhaul-hapus-100-sampai-akar-cascad
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-12
---

# Phase 367 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | none — existing test project (`*.Tests`) covers infra |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~RecordCascadeDelete"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~unit fast; real-SQL integration adds disposable-DB setup |

---

## Sampling Rate

- **After every task commit:** Run quick filtered `dotnet test` for the touched area
- **After every plan wave:** Run `dotnet test` (full suite)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** keep filtered runs under ~60s

---

## Per-Task Verification Map

> Filled by gsd-planner from RESEARCH.md "## Validation Architecture". Each task maps to an automated `dotnet test` filter, real-SQL integration assertion (per-table, Phase 360 pattern), file-cert `[Fact]` post-commit (Phase 355 pattern), or Playwright dual-path (success/fail). UI-honesty + browser paths land in Manual-Only where automation is impractical.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | — | — | Temuan #1-12,#14-20 | — | — | unit/integration | `dotnet test` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Real-SQL integration fixture for cascade assertions (reuse disposable-DB pattern, Phase 360)
- [ ] Seed helper: renewal-chain (TrainingRecords induk + anak `Renews*Id`, cross-table) for cascade + Playwright repro

*Filled during planning; if existing infra suffices, mark "Existing infrastructure covers all phase requirements."*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| UI HTMX jujur (gagal → merah di partial, sukses → sinyal) | SC#3 | HTMX partial render best verified in-browser | Playwright dual-path seed renewal-chain @5277 |

*Refined during planning.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
