---
phase: 250
slug: security-performance
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-24
---

# Phase 250 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC — no unit test framework configured) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 250-01-01 | 01 | 1 | SEC-01 | grep | `grep -c "console.log" Views/CMP/Assessment.cshtml` returns 0 | ✅ | ⬜ pending |
| 250-01-02 | 01 | 1 | SEC-02 | grep | `grep "HtmlEncode\|WebUtility.HtmlEncode" Views/CDP/CoachingProton.cshtml` returns matches | ✅ | ⬜ pending |
| 250-01-03 | 01 | 1 | PERF-01 | grep | `grep "IMemoryCache" Controllers/HomeController.cs` returns matches | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework setup needed — verification is via grep and build success.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| No token/response in DevTools console | SEC-01 | Browser DevTools required | Open Assessment page, F12, check Console tab — no token or payload logged |
| XSS in tooltip renders as text | SEC-02 | Browser rendering required | Create approver with name `<script>alert(1)</script>`, view CoachingProton tooltip — must show as literal text |
| Notification throttled to 1x/hour | PERF-01 | Multi-request timing required | Refresh dashboard 3x in 1 minute, check DB/logs — notification query runs only once |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
