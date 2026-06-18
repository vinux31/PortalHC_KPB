---
phase: 390
slug: test-uat-behavior-parity-dsn-06
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-17
validated: 2026-06-17
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
| 390-01-01 | 01 | 1 | DSN-06 | T-390-01 | N/A (build gate + fixture) | build | `dotnet build` | ✅ | ✅ green |
| 390-01-02 | 01 | 1 | DSN-06 | T-390-02 | preserved CSRF/appUrl control re-asserted | e2e | `npx playwright test coachcoacheemapping-389 --list` | ✅ | ✅ green |
| 390-01-03 | 01 | 1 | DSN-06 | T-390-02 | preserved controls re-asserted | e2e | `npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` | ✅ | ✅ green |
| 390-02-01 | 02 | 2 | DSN-06 | T-390-03/04 | snapshot/restore + role-gate | manual-UAT (Playwright MCP) | (live MCP, snapshot→restore) | ✅ | ✅ green |
| 390-02-02 | 02 | 2 | DSN-06 | T-390-03 | snapshot/restore | manual-UAT (import) | (fixture upload + restore) | ✅ | ✅ green |
| 390-02-03 | 02 | 2 | DSN-06 | T-390-05 | preserved controls confirmed | regression | `dotnet test` + `npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` | ✅ | ✅ green |

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

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (none — existing infra extended)
- [x] No watch-mode flags
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-17

---

## Validation Audit 2026-06-17

State A finalization. VALIDATION.md was created at planning (status: planned, all rows ⬜) and never reconciled after execution. Reconciled against 390-01/390-02-SUMMARY + 390-VERIFICATION (passed 11/11) run evidence.

Run evidence (all 6 tasks green):
- `dotnet build` → 0 error (25 pre-existing warnings).
- `npx playwright test coachcoacheemapping-389 coachworkload-388 --workers=1` → 21 passed / 5 skipped / 0 FAILED (skips = data-guard V-05/11/12/13 + 388 approve/skip, no overload data).
- `dotnet test` → 482 passed / 0 failed.
- Live Playwright MCP roundtrip (snapshot→restore): C1-C6 + W1/W3 PASS; C7 import rollback verified; SEED_JOURNAL Status=cleaned, COUNT=1==baseline.

| Metric | Count |
|--------|-------|
| Requirements (DSN-06) | 1 |
| COVERED | 1 |
| PARTIAL | 0 |
| MISSING | 0 |
| Gaps found | 0 |
| Resolved | 0 (no auditor spawn needed) |
| Escalated | 0 |

Manual-only items (Import Excel + live mutation roundtrip) executed + documented in 390-02-SUMMARY (flaky-to-automate by design per CONTEXT D-03/D-05; verified via Playwright MCP snapshot/restore). Phase NYQUIST-COMPLIANT.
