---
phase: 286
slug: assessmenttype-pre-post-linking
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-02
---

# Phase 286 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual browser verification |
| **Config file** | PortalHC_KPB.csproj |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 286-01-01 | 01 | 1 | BASE-01 | build | `dotnet build` | ✅ | ✅ green |
| 286-01-02 | 01 | 1 | BASE-01 | grep | `grep "class AdminBaseController" Controllers/AdminBaseController.cs` | ✅ | ✅ green |
| 286-01-03 | 01 | 1 | BASE-02 | grep | `grep "class AdminController.*AdminBaseController" Controllers/AdminController.cs` | ✅ | ✅ green |
| 286-01-04 | 01 | 1 | BASE-01 | build | `dotnet build` (zero errors) | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Controllers/AdminBaseController.cs` — new file, base controller class
- Existing infrastructure covers remaining requirements (build verification).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| All Admin/* routes still work | BASE-02 | Routing requires browser | Navigate to /Admin/Index, /Admin/ManageWorkers, verify pages load |
| Authorization still enforced | BASE-02 | Auth requires browser session | Access /Admin/* as unauthenticated user, verify redirect to login |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated (2026-04-02)

---

## Validation Audit 2026-04-02
| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |
| Manual-only | 2 |
