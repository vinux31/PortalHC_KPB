---
phase: 247
slug: bug-fix-pasca-uat
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-24
---

# Phase 247 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + browser manual testing |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build --no-restore` |
| **Full suite command** | `dotnet build --no-restore && echo "BUILD OK"` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --no-restore`
- **After every plan wave:** Run `dotnet build --no-restore && echo "BUILD OK"`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 247-01-01 | 01 | 1 | FIX-01 | build + code review | `dotnet build --no-restore` | ✅ | ⬜ pending |
| 247-02-01 | 02 | 2 | FIX-01 | manual browser | N/A (manual) | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. Bug fix phase uses build verification + manual browser testing.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ET distribution balanced | FIX-01 | Requires exam creation with multiple ETs to verify distribution | Create assessment with 4+ ET, verify soal count per ET is balanced |
| Phase 235 approval chain | FIX-01 | Multi-role browser testing | Login as SrSpv → approve → login as SH → approve → verify chain |
| Phase 244 monitoring | FIX-01 | SignalR real-time, browser-only | Open monitoring page, verify real-time updates |
| Phase 246 edge cases | FIX-01 | Token, force close, alarm, records | Test each scenario in browser |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
