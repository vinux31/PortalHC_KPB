---
phase: 283
slug: user-impersonation
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-04-01
---

# Phase 283 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (TypeScript) |
| **Config file** | `Tests/playwright.config.ts` |
| **Quick run command** | `cd Tests && npx playwright test --grep "impersonation" --headed` |
| **Full suite command** | `cd Tests && npx playwright test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `cd Tests && npx playwright test --grep "impersonation"`
- **After every plan wave:** Run `cd Tests && npx playwright test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 283-01-01 | 01 | 1 | IMP-01 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "view as"` | ✅ | ✅ green |
| 283-01-02 | 01 | 1 | IMP-02 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "autocomplete"` | ✅ | ✅ green |
| 283-01-03 | 01 | 1 | IMP-03 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "banner"` | ✅ | ✅ green |
| 283-01-04 | 01 | 1 | IMP-04 | manual | N/A — perlu manipulasi waktu | N/A | ⬜ pending |
| 283-01-05 | 01 | 1 | IMP-05 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "read-only"` | ✅ | ✅ green |
| 283-01-06 | 01 | 1 | IMP-06 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "audit"` | ✅ | ✅ green |
| 283-01-07 | 01 | 1 | IMP-07 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "SearchUsersApi"` | ✅ | ✅ green |
| 283-01-08 | 01 | 1 | IMP-08 | e2e | `cd tests && npx playwright test e2e/impersonation.spec.ts --grep "stop"` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `Tests/e2e/impersonation.spec.ts` — stubs for IMP-01 to IMP-08
- [ ] Test helpers: login as Admin utility

*Existing infrastructure covers test framework setup.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Auto-expire 30 menit | IMP-04 | Perlu manipulasi waktu / menunggu 30 menit | 1. Start impersonation 2. Tunggu 30 menit atau ubah session timestamp 3. Verify banner hilang dan session kembali ke admin |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** green — 9 automated tests pass (IMP-01 to IMP-08 except IMP-04 manual)

---

## Validation Audit 2026-04-01

| Metric | Count |
|--------|-------|
| Gaps found | 7 |
| Resolved | 7 |
| Escalated | 0 |
