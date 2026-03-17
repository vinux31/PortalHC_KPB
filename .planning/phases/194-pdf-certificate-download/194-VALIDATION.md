---
phase: 194
slug: pdf-certificate-download
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-17
---

# Phase 194 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser testing + dotnet build verification |
| **Config file** | none — no automated test framework in project |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build`
- **Before `/gsd:verify-work`:** Build must succeed
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 194-01-01 | 01 | 1 | CERT-03 | build + manual | `dotnet build` | N/A | ⬜ pending |
| 194-01-02 | 01 | 1 | CERT-03 | build + manual | `dotnet build` | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PDF downloads with correct layout | CERT-03 | Visual PDF rendering requires human inspection | Navigate to Certificate page for a passed assessment, click Download PDF, verify A4 landscape layout with all fields |
| Auth guard prevents unauthorized access | CERT-03 | Requires browser session manipulation | Log in as different user, try accessing CMP/CertificatePdf/{id} for another user's certificate, verify Forbid response |
| Filename format correct | CERT-03 | Browser download filename inspection | Download PDF, verify filename matches Sertifikat_{NIP}_{Title}_{Year}.pdf pattern |

*All phase behaviors require manual verification due to visual/PDF nature.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
