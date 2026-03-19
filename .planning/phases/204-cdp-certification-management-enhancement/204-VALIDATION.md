---
phase: 204
slug: cdp-certification-management-enhancement
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-19
---

# Phase 204 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build (compile check) + manual browser verification |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build --no-restore -q` |
| **Full suite command** | `dotnet build --no-restore` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build --no-restore -q`
- **After every plan wave:** Run `dotnet build --no-restore`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 204-01-01 | 01 | 1 | CDP-01, CDP-03 | build + grep | `dotnet build --no-restore -q` | ✅ | ⬜ pending |
| 204-01-02 | 01 | 1 | CDP-01, CDP-02 | build + grep | `dotnet build --no-restore -q` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — this is a Razor/C# project using dotnet build for compilation checks and browser-based verification.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Renewed rows hidden by default in CDP table | CDP-01 | Visual DOM behavior requires browser | Navigate to CDP > Certification Management, verify renewed certs not shown |
| Toggle shows renewed rows with opacity 50% | CDP-02 | Visual styling check | Click "Tampilkan Riwayat Renewal" toggle, verify dimmed rows appear |
| Expired card count excludes renewed | CDP-03 | Requires data with known renewed certs | Compare Expired card count before/after having renewed certs in DB |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
