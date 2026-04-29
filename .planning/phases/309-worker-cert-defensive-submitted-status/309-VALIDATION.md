---
phase: 309
slug: worker-cert-defensive-submitted-status
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-04-29
---

# Phase 309 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (E2E TS) + xUnit (unit C#, optional) |
| **Config file** | `tests/playwright.config.ts` (verify pre-Wave 0 oleh planner) |
| **Quick run command** | `cd tests && npx playwright test e2e/assessment.spec.ts --grep "Phase 309" --reporter=list` |
| **Full suite command** | `cd tests && npx playwright test --reporter=list` |
| **Estimated runtime** | ~30 detik (quick) / ~5 menit (full) |

---

## Sampling Rate

- **After every task commit:** Run quick command (filter "Phase 309")
- **After every plan wave:** Run full suite
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 detik

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 309-01-XX | 01 | 1 | WCRT-01 | T-309-01 / — | Try-catch wrap Certificate action; fallback signatory tidak leak stack trace | E2E | `npx playwright test --grep "Phase 309 WCRT"` | ❌ W0 | ⬜ pending |
| 309-02-XX | 02 | 2 | SUB-01 | — | Status "Menunggu Penilaian" tidak munculkan popup error merah | E2E | `npx playwright test --grep "Phase 309 SUB"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/assessment-309.spec.ts` — stubs untuk WCRT-01 + SUB-01 dengan describe block "Phase 309"
- [ ] Reuse `tests/e2e/helpers/wizardSelectors.ts` (existing) jika ada selector yang relevant
- [ ] Tambahkan helper E2E untuk seed assessment dengan status "Menunggu Penilaian" (sub-route admin atau direct DB seed)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Worker dengan exotic Category null/empty fallback "HC Manager" | WCRT-01 SC#6 | Sulit re-create kondisi data exotic via E2E (data dependent) | Buat user test dengan Category=NULL via SQL seed → akses Certificate page → assert nama signatory = "HC Manager" |
| Post-deploy LogError monitoring | WCRT-01 SC#7 | Production observability, bukan test code | Setelah deploy ke prod, monitor `_logger.LogError` di Application Insights / file log untuk root cause aktual dari Certificate failure |
| Visual styling TempData["Info"] berbeda dari Error | SUB-01 D-09 | Visual / a11y verification | Trigger pending state → buka Certificate → assert alert biru/info (bukan merah/error) |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
