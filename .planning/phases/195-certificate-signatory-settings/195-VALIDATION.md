---
phase: 195
slug: certificate-signatory-settings
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-18
---

# Phase 195 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET Core MVC — no automated test suite) |
| **Config file** | none |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~15 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run` + manual verification
- **Before `/gsd:verify-work`:** Full build must succeed, manual UAT pass
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 195-01-xx | 01 | 1 | R195-1 | build | `dotnet build` | ✅ | ⬜ pending |
| 195-02-xx | 02 | 1 | R195-2 | manual | browser: Admin/ManageCategories | n/a | ⬜ pending |
| 195-03-xx | 03 | 2 | R195-3 | manual | browser: CreateAssessment + EditAssessment | n/a | ⬜ pending |
| 195-04-xx | 04 | 2 | R195-4 | manual | browser: CMP/Certificate + CMP/CertificatePdf | n/a | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No test framework installation needed — project uses manual browser UAT.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Sub-category hierarchy in ManageCategories | R195-2 | UI rendering, indented tree layout | Create parent, add child, verify indent + expand |
| Optgroup dropdown in CreateAssessment | R195-3 | HTML optgroup rendering | Open wizard step 1, verify grouped dropdown |
| Signatory on certificate | R195-4 | Visual rendering of P-Sign + inheritance | Set signatory on category, view certificate, verify P-Sign shows |
| Delete blocked on parent with children | R195-2 | UI interaction + DB constraint | Try delete parent with children, verify blocked message |
| Signatory inheritance fallback | R195-4 | Multi-level logic | Category with no signatory → parent signatory → static fallback |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
