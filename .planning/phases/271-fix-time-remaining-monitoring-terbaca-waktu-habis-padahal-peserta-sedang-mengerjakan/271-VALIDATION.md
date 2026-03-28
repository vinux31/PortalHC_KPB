---
phase: 271
slug: fix-time-remaining-monitoring-terbaca-waktu-habis-padahal-peserta-sedang-mengerjakan
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-28
---

# Phase 271 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT (ASP.NET MVC — no automated test suite) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` + manual browser test |
| **Estimated runtime** | ~30 seconds (build) + manual verification |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` to verify compilation
- **After every plan wave:** Manual browser test on dev server
- **Before `/gsd:verify-work`:** Full manual UAT must pass
- **Max feedback latency:** 30 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 271-01-01 | 01 | 1 | Timer resume | build | `dotnet build` | ✅ | ⬜ pending |
| 271-01-02 | 01 | 1 | Timer clamp | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Resume timer shows correct remaining time | Timer resume | Requires real browser session with navigate away/return | 1. Start exam 2. Wait 1 min 3. Navigate to home 4. Return to exam 5. Verify timer shows ~correct remaining |
| Timer never increases after resume | Timer no-increase | Requires real browser session with refresh | 1. Note timer value 2. Refresh browser 3. Verify timer <= previous value |
| Timer expires at correct time | Timer expiry | Requires real-time wait | 1. Start exam with short duration 2. Wait until expiry 3. Verify modal appears at correct time |

*All verifications require manual browser testing — no automated test framework in project.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
