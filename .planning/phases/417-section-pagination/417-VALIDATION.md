---
phase: 417
slug: section-pagination
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-23
---

# Phase 417 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `playwright.config.ts` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~SectionPaginator"` |
| **Full suite command** | `dotnet test HcPortal.Tests` (+ Playwright combined `--workers=1`) |
| **Estimated runtime** | ~30–90 detik (unit); e2e terpisah |

---

## Sampling Rate

- **After every task commit:** Run quick command (filter `SectionPaginator`)
- **After every plan wave:** Run full xUnit suite
- **Before `/gsd-verify-work`:** Full suite green + Playwright `section-pagination.spec.ts` green
- **Max feedback latency:** ~90 detik (unit)

---

## Per-Task Verification Map

> Planner mengisi baris detail per-task saat plan dibuat. Kerangka requirement → test type di bawah.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 417-W0 | — | 0 | PAG-01/02/03 | — | N/A (render-only, no new authz) | unit+e2e (stubs) | `dotnet test --filter SectionPaginator` | ❌ W0 | ⬜ pending |
| PAG-01 | TBD | 1 | PAG-01 | — | N/A | unit (ComputeSectionPages) + e2e render header | TBD | ❌ W0 | ⬜ pending |
| PAG-02 | TBD | 1 | PAG-02 | — | N/A | unit (StartNewPage break + auto-split per-10) | TBD | ❌ W0 | ⬜ pending |
| PAG-03 | TBD | 1 | PAG-03 | — | N/A (resume = own session only; guard existing) | unit (clamp/fallback) + e2e resume toast | TBD | ❌ W0 | ⬜ pending |
| BC-golden | TBD | 1 | PAG-01..03 | — | N/A | unit golden (no-Section flat == baseline) | TBD | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/SectionPaginatorTests.cs` — stubs untuk `ComputeSectionPages` (PAG-01/02/03): header at section boundary, StartNewPage break, auto-split per-10, "Lainnya" last, no-Section flat golden, resume clamp/fallback page 0.
- [ ] `tests/e2e/section-pagination.spec.ts` — stubs: render header (nama saja) + "(lanjutan)", navigator per-Section, indikator "Section — Halaman n/total", resume toast "Lanjut dari soal no. X", mobile 5/halaman, no-Section backward-compat.

*Infrastruktur xUnit + Playwright sudah ada (HcPortal.Tests + tests/e2e) — Wave 0 hanya menambah file test baru, bukan install framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual styling header/navigator/toast di real browser | PAG-01/02/03 | Estetika & layout Bootstrap tak terukur unit | Playwright UAT @localhost:5277 (autopilot §5) — assert DOM live + screenshot |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
