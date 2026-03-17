---
phase: 192
slug: validuntil-nomorsertifikat
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-17
---

# Phase 192 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual browser verification (ASP.NET MVC — no automated test suite) |
| **Config file** | none |
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
| 192-01-01 | 01 | 1 | CERT-01, CERT-02 | build + manual | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| ValidUntil stored correctly per session | CERT-01 | DB state verification | Create assessment with ValidUntil, check AssessmentSessions table |
| NomorSertifikat unique per session | CERT-02 | DB state + format verification | Create assessment, verify KPB/{SEQ}/{ROMAN}/{YEAR} format in DB |
| Null ValidUntil accepted | CERT-01 | Optional field behavior | Create assessment without ValidUntil, verify null stored |
| UNIQUE constraint prevents duplicates | CERT-02 | Concurrency behavior | Verify DB constraint exists via migration |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
