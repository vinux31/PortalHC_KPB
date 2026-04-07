---
phase: 300
slug: mobile-optimization
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-07
---

# Phase 300 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Browser-based manual + responsive viewport testing |
| **Config file** | none — CSS/HTML changes verified via browser DevTools |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 300-01-01 | 01 | 1 | MOB-01 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 300-01-02 | 01 | 1 | MOB-02 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 300-01-03 | 01 | 1 | MOB-03 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 300-01-04 | 01 | 1 | MOB-04 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 300-01-05 | 01 | 1 | MOB-05 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |
| 300-01-06 | 01 | 1 | MOB-06 | — | N/A | build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements — no new test framework needed.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Touch target 48x48dp minimum | MOB-01 | Visual/touch interaction | Open DevTools mobile emulator, verify button dimensions ≥ 48x48 |
| Sticky footer always visible | MOB-03 | Visual layout | Scroll through exam on mobile viewport, confirm footer stays fixed |
| Offcanvas drawer navigation | MOB-02 | Touch interaction | Tap ≡ button, verify drawer opens with question grid |
| Timer sticky in header | MOB-04 | Visual layout | Scroll down on mobile, confirm timer remains visible |
| Anti-copy + touch no conflict | MOB-05 | Event interaction | Verify copy blocked while touch navigation works |
| Landscape sidebar restore | MOB-06 | Orientation change | Rotate device/emulator to landscape, verify sidebar appears |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
