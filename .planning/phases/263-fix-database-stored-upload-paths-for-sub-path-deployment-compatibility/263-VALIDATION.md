---
phase: 263
slug: fix-database-stored-upload-paths-for-sub-path-deployment-compatibility
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-27
---

# Phase 263 — Validation Strategy

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
- **Before `/gsd:verify-work`:** Build must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 263-01-01 | 01 | 1 | D-05 | build + grep | `dotnet build && grep -n "Url.Content" Views/Admin/AssessmentMonitoringDetail.cshtml` | ✅ | ⬜ pending |
| 263-01-02 | 01 | 1 | D-06 | build + grep | `dotnet build && grep -n "basePath" Views/ProtonData/Override.cshtml` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Upload file link resolves with /KPB-PortalHC/ prefix | D-05, D-06 | Requires running app with PathBase configured | 1. Deploy with PathBase /KPB-PortalHC/ 2. Navigate to AssessmentMonitoringDetail with existing upload 3. Click file link — should resolve correctly 4. Navigate to Override page with evidence — link should work |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
