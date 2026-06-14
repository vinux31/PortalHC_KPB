---
phase: 377
slug: impersonation-identity-across-surfaces
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-14
---

# Phase 377 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail dimensi diturunkan dari `377-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (e2e) |
| **Config file** | `HcPortal.Tests/` (xUnit) · `tests/e2e/` (Playwright) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Impersonation"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30-60 detik (xUnit unit); e2e lebih lama (`--workers=1`) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~Impersonation"`
- **After every plan wave:** Run `dotnet test` (full suite — no regression SC4)
- **Before `/gsd-verify-work`:** Full suite must be green + e2e impersonate→Records hijau
- **Max feedback latency:** 60 detik (xUnit)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| _diisi planner_ | — | — | IMP-01/02 | T-377-* | effective user = X (impersonate); = admin (normal) | unit | `dotnet test --filter ...` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Test stub untuk resolver effective-user (IMP-01/02) — pure-logic seam (no Moq, pola proyek)
- [ ] e2e: extend `tests/e2e/impersonation.spec.ts` (IMP-02 flow) → assert `/CMP/Records` tampil data X
- [ ] Fixture: skenario impersonate mode=user (X valid), mode=role (no user), target-null (D-04)

*Detail final diisi planner berdasarkan RESEARCH §Validation Architecture.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Banner "Anda melihat sebagai X" jujur + data X di browser | IMP-01 | UAT visual lintas surface | Impersonate user X @localhost:5277 → buka /CMP/Records, /Home, Assessment → konfirmasi data = X |

*Local e2e SQL gotcha (STATE.md): start SQLBrowser + `lpc:` shared-memory conn override + `--workers=1`. AD lokal: `Authentication__UseActiveDirectory=false dotnet run`.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
