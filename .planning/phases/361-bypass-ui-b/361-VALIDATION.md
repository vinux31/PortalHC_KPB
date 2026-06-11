---
phase: 361
slug: bypass-ui-b
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-11
---

# Phase 361 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright `@playwright/test` (e2e) + xUnit `HcPortal.Tests` (unit — sudah ter-cover 360, TIDAK ditambah) |
| **Config file** | `tests/playwright.config.ts` (testDir `./e2e`, baseURL `http://localhost:5277`, globalTeardown RESTORE) |
| **Quick run command** | `dotnet build` (compile gate) |
| **Full suite command** | `cd tests && npx playwright test proton-bypass.spec.ts` (setelah `Authentication__UseActiveDirectory=false dotnet run` @5277) |
| **Estimated runtime** | build ~30s; spec e2e ~2-4 menit |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (0 error)
- **After every plan wave:** Run `dotnet build` + Playwright spec relevan @5277 (AD=false)
- **Before `/gsd-verify-work`:** Full spec hijau + UAT live MCP (4 closure mode + pending konfirmasi + batal + re-grade fail)
- **Max feedback latency:** ~240 seconds (e2e spec)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (diisi planner) | — | — | PBYP-08 | XSS innerHTML | render via `escHtml()` | e2e | `npx playwright test proton-bypass.spec.ts` | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | PBYP-08 | CSRF POST | `RequestVerificationToken` header | e2e | per-mode BypassSave → toast sukses; CL-B(b) → alert kuning | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | PBYP-09 | stale-state confirm | toast pesan backend + auto-refresh | e2e | goto deep-link → modal auto-open; confirm/cancel flow | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | PBYP-10 | double-submit | disable + spinner in-flight | e2e + live MCP | full spec + UAT MCP @5277 | ❌ W0 | ⬜ pending |
| (diisi planner) | — | — | PBYP-10 | — | re-grade Pass→Fail → badge "Menunggu Exam" | e2e UI ringan | trigger `/AssessmentAdmin/EditPesertaAnswers` → assert badge | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/proton-bypass.spec.ts` — covers PBYP-08/09/10 (pembagian file = discretion D-22)
- [ ] SQL fixture worker multi-state (komplit CL-A, partial CL-B, punya final→tolak, exam in-progress E5) per D-23 — pola `.planning/seeds/313-timer-fixtures.sql` (WIPE-AND-INSERT idempotent + THROW guard + BEGIN TRAN)
- [ ] Reuse `tests/helpers/dbSnapshot.ts` + `tests/helpers/auth.ts` — TIDAK perlu helper baru
- [ ] `docs/SEED_JOURNAL.md` entry (active→cleaned)
- [ ] Framework install: TIDAK perlu — `tests/node_modules` sudah ada

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| UAT live 4 closure mode + konfirmasi + batal + re-grade fail | PBYP-10 | D-22 dua lapis: live MCP = bukti UAT, spec = regresi | Playwright MCP @5277 login admin, jalankan skenario per mode, screenshot |
| Visual: Tab1 existing tak berubah perilaku | PBYP-08 | regresi visual halus | buka Tab1, jalankan filter + override existing, bandingkan perilaku |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 240s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
