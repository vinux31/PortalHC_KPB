---
phase: 384
slug: monitoring-essay-grading-ui-refactor-fase-2
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-15
---

# Phase 384 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> UI refactor (Razor + Bootstrap). Primary acceptance gate = Playwright e2e (UIG-04) + `dotnet build`. Backend unchanged, 0 migration → no new DB-write tests.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.x (`HcPortal.Tests/`) for any C# unit; **Playwright (TypeScript, `tests/e2e/`)** for UI e2e (primary for this phase) |
| **Config file** | `tests/playwright.config.ts` (baseURL `http://localhost:5277`, `fullyParallel:false`, NO `webServer` → `dotnet run` manual wajib) · `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet build` (0 error gate) |
| **Full suite command** | `dotnet test` (xUnit) + `cd tests && npx playwright test --workers=1` (e2e) |
| **Estimated runtime** | build ~60s · e2e flow ~2-3 min |

**Local run prerequisites (CLAUDE.md Develop Workflow + memory):**
- `dotnet run` with `Authentication__UseActiveDirectory=false` (local admin login — Phase 355 lesson)
- e2e: start SQLBrowser + shared-memory conn override (NTLM loopback fail); Playwright combined run WAJIB `--workers=1`
- Razor dynamic → Playwright runtime mandatory; grep+build insufficient (Phase 354 lesson)

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` (must be 0 error)
- **After every plan wave:** Run `dotnet test` + relevant Playwright spec
- **Before `/gsd-verify-work`:** Full suite green (build + xUnit + FLOW 384 e2e)
- **Max feedback latency:** ~180 seconds

---

## Per-Task Verification Map

> Planner fills concrete task IDs into this map (and `<acceptance_criteria>` into each PLAN task). UIG-04 Playwright e2e is the binding acceptance gate for the whole phase.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 384-0?-0? | 0? | 0 | UIG-04 | — | e2e harness/fixture green | e2e (Wave 0) | `npx playwright test flow-384 --workers=1` | ❌ W0 | ⬜ pending |
| 384-0?-0? | 0? | 1 | UIG-01 | — | tabel worker-list render (hanya HasManualGrading, urut NIP), badge 3-state | e2e | `npx playwright test flow-384 --workers=1` | ❌ W0 | ⬜ pending |
| 384-0?-0? | 0? | 1 | UIG-02 | T-384-IDOR | "Tinjau Essay" → GET `/Admin/EssayGrading?sessionId=` (Admin/HC authz) navigates | e2e | `npx playwright test flow-384 --workers=1` | ❌ W0 | ⬜ pending |
| 384-0?-0? | 0? | 1 | UIG-03 | — | Simpan Skor persist (AJAX) + Selesaikan Penilaian round-trip on new page | e2e | `npx playwright test flow-384 --workers=1` | ❌ W0 | ⬜ pending |
| 384-0?-0? | 0? | 1 | UIG-04 | — | full flow: list → Tinjau → skor → Selesaikan → in-place "Selesai" | e2e | `npx playwright test flow-384 --workers=1` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `tests/e2e/flow-384-essay-grading-refactor.spec.ts` (or extend `assessment-pending-grade.spec.ts`) — FLOW 384 e2e RED stubs for UIG-01..04
- [ ] e2e fixture/seed: an AssessmentSession with `HasManualGrading == true` + ≥1 ungraded essay (`EssayScore == null`) reachable from an Admin/HC monitoring detail view — reuse snapshot/restore pattern from `assessment-pending-grade.spec.ts`
- [ ] (optional) xUnit test for new GET `EssayGrading` action: returns view with correct single-session essay items + 403/redirect for non-Admin/HC (IDOR/authz)

*Existing infrastructure (Playwright config, accounts.ts admin/hc helpers) covers harness — only the FLOW 384 spec + fixture are new.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Badge warna 3-state + kontras (bg-warning text-dark) visually correct | UIG-01 | Pixel/contrast judgment beyond DOM assertion | localhost:5277 → Monitoring detail with mixed worker states → eyeball 🟡/🔵/🟢 badges |
| Tabel worker-list responsif di viewport sempit (`table-responsive`) | UIG-01 | Visual layout judgment | Resize browser / DevTools narrow → table scrolls, no overflow break |

*All functional behaviors (render, navigate, persist, finalize) have automated Playwright verification; only visual/contrast/responsive polish is manual.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (FLOW 384 spec + fixture)
- [ ] No watch-mode flags (Playwright `--workers=1`, no `--watch`)
- [ ] Feedback latency < 180s
- [ ] `nyquist_compliant: true` set in frontmatter (planner/auditor sets after map filled)

**Approval:** pending
