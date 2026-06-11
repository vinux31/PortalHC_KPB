---
phase: 364
slug: restore-baseline-regresi-e2e-exam
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-11
---

# Phase 364 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (e2e, tests/) + xUnit (HcPortal.Tests) |
| **Config file** | tests/playwright.config.ts |
| **Quick run command** | `npx playwright test exam-taking.spec.ts exam-types.spec.ts` (dari tests/, app live @5277) |
| **Full suite command** | `dotnet test` + 2 spec target full run @localhost:5277 |
| **Estimated runtime** | e2e 2 spec ~10-20 menit; dotnet test ~detik |

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error (e2e run hanya pada gate task — mahal)
- **After every plan wave:** run spec yang disentuh wave tsb @5277
- **Before `/gsd-verify-work`:** kedua spec PASS 1x full run + `dotnet test` hijau (D-08/D-15)
- **Max feedback latency:** ~20 menit (full e2e 2 spec)

---

## Per-Task Verification Map

> Diisi planner saat PLAN.md dibuat. Acuan: 364-RESEARCH.md §Validation Architecture.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | — | — | SC#1-4 (test-only) | — | N/A | e2e | `npx playwright test exam-taking.spec.ts exam-types.spec.ts` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — Playwright config + global setup/teardown (BACKUP/RESTORE DB otomatis) + dbSnapshot helper sudah ada. Tidak ada framework baru.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Baseline diagnosa pre-edit (D-10) | SC#2 | Run e2e as-is untuk klasifikasi failure judul vs non-judul — hasil dicatat, bukan asersi | Run kedua spec as-is @5277, catat failure per-flow di SUMMARY |
