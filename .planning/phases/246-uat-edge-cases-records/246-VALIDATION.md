---
phase: 246
slug: uat-edge-cases-records
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-24
---

# Phase 246 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Browser UAT (manual) + dotnet build (compilation check) |
| **Config file** | none — this is a UAT phase |
| **Quick run command** | `dotnet build --no-restore` |
| **Full suite command** | `dotnet build --no-restore` + browser UAT checklist |
| **Estimated runtime** | ~10 seconds (build) + manual UAT |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --no-restore`
- **After every plan wave:** Run full suite + browser UAT checklist
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds (build only)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 246-01-01 | 01 | 1 | EDGE-01, EDGE-03 | compilation | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 246-01-02 | 01 | 1 | EDGE-04 | compilation | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 246-02-01 | 02 | 2 | EDGE-01 | manual | Browser UAT | N/A | ⬜ pending |
| 246-02-02 | 02 | 2 | EDGE-02 | manual | Browser UAT | N/A | ⬜ pending |
| 246-02-03 | 02 | 2 | EDGE-03 | manual | Browser UAT | N/A | ⬜ pending |
| 246-02-04 | 02 | 2 | EDGE-04 | manual | Browser UAT | N/A | ⬜ pending |
| 246-02-05 | 02 | 2 | REC-01 | manual | Browser UAT | N/A | ⬜ pending |
| 246-02-06 | 02 | 2 | REC-02 | manual | Browser UAT | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Plan 01 seeds data; Plan 02 is browser UAT.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Token salah ditolak | EDGE-01 | Requires browser interaction with exam UI | Enter wrong token on StartExam page |
| Force close & reset | EDGE-02 | Requires multi-tab HC+Worker interaction | HC force closes while worker is in exam |
| Regenerate token | EDGE-03 | Requires token rotation + re-attempt | HC regenerates, worker tries old token |
| Renewal expired E2E | EDGE-04 | Requires full UI flow alarm→renewal | Check Home banner, click, complete renewal |
| My Records view+export | REC-01 | Requires visual verification + file download | Open My Records, check columns, export |
| Team View records+export | REC-02 | Requires visual verification + file download | Open Team View, filter, export |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
