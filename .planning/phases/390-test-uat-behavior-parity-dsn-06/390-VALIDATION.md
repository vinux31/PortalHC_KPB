---
phase: 390
slug: test-uat-behavior-parity-dsn-06
status: planned
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-17
---

# Phase 390 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright @playwright/test (e2e) + xUnit (dotnet test) |
| **Config file** | `tests/playwright.config.ts` (workers=1) + `*.Tests.csproj` |
| **Quick run command** | `cd tests && npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` |
| **Full suite command** | `dotnet test` + `cd tests && npx playwright test --workers=1` |
| **Estimated runtime** | ~120 seconds |

---

## Sampling Rate

- **After every task commit:** Run quick parity specs (the 2 escalated spec files)
- **After every plan wave:** Run full suite (dotnet test + playwright)
- **Before `/gsd-verify-work`:** Full suite must be green + live UAT mutations PASS (snapshot/restore)
- **Max feedback latency:** 120 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 390-01-01 | 01 | 1 | DSN-06 | T-390-01 | N/A (build gate + fixture) | build | `dotnet build` | ✅ | ⬜ pending |
| 390-01-02 | 01 | 1 | DSN-06 | T-390-02 | preserved CSRF/appUrl control re-asserted | e2e | `npx playwright test coachcoacheemapping-389 --list` | ✅ | ⬜ pending |
| 390-01-03 | 01 | 1 | DSN-06 | T-390-02 | preserved controls re-asserted | e2e | `npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` | ✅ | ⬜ pending |
| 390-02-01 | 02 | 2 | DSN-06 | T-390-03/04 | snapshot/restore + role-gate | manual-UAT (Playwright MCP) | (live MCP, snapshot→restore) | ✅ | ⬜ pending |
| 390-02-02 | 02 | 2 | DSN-06 | T-390-03 | snapshot/restore | manual-UAT (import) | (fixture upload + restore) | ✅ | ⬜ pending |
| 390-02-03 | 02 | 2 | DSN-06 | T-390-05 | preserved controls confirmed | regression | `dotnet test` + `npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky · Filled during planning.*

---

## Wave 0 Requirements

- [x] Existing infrastructure covers all phase requirements (extend `coachcoacheemapping-389.spec.ts` + `coachworkload-388.spec.ts`; no new framework).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Import Excel mapping | DSN-06 | File-upload otomatis flaky + butuh fixture+restore (CONTEXT D-05) | UAT manual: upload fixture `.xlsx` (kolom `NIP Coach`/`NIP Coachee`) → assert import-results card + baris muncul → RESTORE |
| Live mutation roundtrip (add/edit/deactivate/graduated/delete/reactivate; set-threshold/approve/skip) | DSN-06 | Mutasi DB; dijalankan main agent via Playwright MCP dgn snapshot→restore (CONTEXT D-03/D-04) | Snapshot DB → jalankan tiap aksi live localhost:5277 → assert UI/DB → RESTORE → catat SEED_JOURNAL cleaned |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
