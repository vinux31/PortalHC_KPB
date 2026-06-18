---
phase: 398
slug: test-uat-seakan-online
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 398 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail kontrak validasi (testable contracts) ada di `398-RESEARCH.md` §Validation Architecture — planner derive Per-Task Map dari sana.
> CATATAN: Phase 398 ADALAH fase test/verifikasi — "test" di sini = deliverable utama (spec e2e + rerun regresi + audit milestone), bukan test untuk kode baru.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (`tests/e2e`) + xUnit (`HcPortal.Tests`) |
| **Config file** | `tests/playwright.config.ts` + `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `cd tests && npx playwright test e2e/inject-seakan-online-398.spec.ts --workers=1` |
| **Full suite command** | `dotnet test` + `cd tests && npx playwright test --workers=1` (incl. online-path regresi) |
| **Estimated runtime** | e2e 398 beberapa menit (real-SQL + browser); `dotnet test` ~1-2 menit |

---

## Sampling Rate

- **After every task commit:** Run skenario e2e yang sedang dikerjakan (`--workers=1`)
- **After every plan wave:** Run spec 398 penuh + `dotnet test`
- **Before milestone audit:** Full suite hijau + spec 398 hijau + online-path regresi hijau
- **Max feedback latency:** per-skenario ~1-2 menit

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| _planner isi dari RESEARCH §Validation Architecture_ | | | INJ-13 | | | e2e/integration | | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/inject-seakan-online-398.spec.ts` — spec konsolidasi downstream parity (4-5 skenario: Form/Auto-gen/Excel + essay + Pre/Post + side-by-side vs online asli) — _planner finalize_
- [ ] Reuse online-path regresi specs (`exam-types.spec.ts` FLOW K/L/M, `exam-taking.spec.ts` Flow A) — rerun, jangan tulis ulang

*Planner finalize daftar Wave 0 dari RESEARCH §Validation Architecture.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| (tak ada baru) | INJ-13 | D-01: human UAT 398 di-skip — bukti mata-manusia sudah dari per-phase UAT 394-397 | n/a |

*Sebagian besar perilaku punya automated verify; human UAT 398 sengaja di-skip (D-01).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency reasonable (per-skenario)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
