---
phase: 397
slug: link-pre-post-ke-room-existing
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-18
---

# Phase 397 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail kontrak validasi (testable contracts) ada di `397-RESEARCH.md` §Validation Architecture — planner derive Dimensi-8 dari sana.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`HcPortal.Tests`) + Playwright (`tests/e2e`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` + `playwright.config.ts` |
| **Quick run command** | `dotnet test --filter Category!=Integration` (fast suite) |
| **Full suite command** | `dotnet test` (+ `npx playwright test --workers=1` untuk e2e) |
| **Estimated runtime** | fast ~30s; integration real-SQL + e2e lebih lama |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter Category!=Integration`
- **After every plan wave:** Run `dotnet test` (full, termasuk Integration real-SQL)
- **Before `/gsd-verify-work`:** Full suite + Playwright e2e must be green
- **Max feedback latency:** ~30s (fast suite)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 397-01-01 | 01 | 1 | INJ-12 | T-397-01 | DTO/VM carry link contract (server re-resolves) | build | `dotnet build HcPortal.csproj` | ❌ W0 | ⬜ pending |
| 397-01-02 | 01 | 1 | INJ-12 | T-397-03 | RED: per-worker link, Kasus A/B, atomic rollback, anti-double, unlink revert | integration (RED) | `dotnet build HcPortal.Tests/HcPortal.Tests.csproj` (clean RED — missing symbol only) | ❌ W0 | ⬜ pending |
| 397-01-03 | 01 | 1 | INJ-12 | T-397-02 | RED: preview==commit pairing + cross inject↔online grouping intact (§13 KRITIS) | integration (RED) | `dotnet build HcPortal.Tests/HcPortal.Tests.csproj` (clean RED — missing symbol only) | ❌ W0 | ⬜ pending |
| 397-02-01 | 02 | 2 | INJ-12 | T-397-04 | Per-worker bidirectional link + Kasus A/B write-to-online (online Score/Status untouched), atomic | integration | `dotnet test --filter "FullyQualifiedName~InjectLink"` | ❌ W0 | ⬜ pending |
| 397-02-02 | 02 | 2 | INJ-12 | T-397-06 | Anti-double reject full list (D-08) + PreviewPairingAsync dry-run no-write (D-07) | integration | `dotnet test --filter "FullyQualifiedName~AntiDoubleLink|FullyQualifiedName~PreviewPairing"` | ❌ W0 | ⬜ pending |
| 397-02-03 | 02 | 2 | INJ-12 | T-397-05, T-397-07 | UnlinkInjectGroupAsync atomic revert + audit "LinkPrePostUndo" (D-12), IDOR guard | integration | `dotnet test --filter "FullyQualifiedName~UnlinkInject|FullyQualifiedName~CrossGrouping"` | ❌ W0 | ⬜ pending |
| 397-03-01 | 03 | 3 | INJ-12 | T-397-09, T-397-11, T-397-13 | SearchLinkTargets JSON (RBAC, opposite-type whitelist, parameterized) + MapToRequest link wiring | build/integration | `dotnet build HcPortal.csproj` + `dotnet test --filter Category!=Integration` | ❌ W0 | ⬜ pending |
| 397-03-02 | 03 | 3 | INJ-12 | T-397-10, T-397-12 | PreviewPairing POST dry-run (CSRF) + UnlinkInjectGroup POST (RBAC+CSRF, Json shape) | build/integration | `dotnet build HcPortal.csproj` + `dotnet test --filter Category!=Integration` | ❌ W0 | ⬜ pending |
| 397-04-01 | 04 | 4 | INJ-12 | T-397-14 | Step-1 trigger + chip + room-picker modal (XSS-safe .textContent) | build + e2e | `dotnet build HcPortal.csproj` (runtime in 397-04-03) | ❌ W0 | ⬜ pending |
| 397-04-02 | 04 | 4 | INJ-12 | T-397-14, T-397-15, T-397-16 | Pairing summary + anti-double entries + unlink confirm modal (Bootstrap, not native) | build + e2e | `dotnet build HcPortal.csproj` (runtime in 397-04-03) | ❌ W0 | ⬜ pending |
| 397-04-03 | 04 | 4 | INJ-12 | T-397-17 | Playwright runtime (modal/picker/chip/preview/unlink) + cross-grouping intact + 0-migration gate | e2e (Playwright) | `cd tests && npx playwright test e2e/inject-assessment-397.spec.ts --workers=1` | ❌ W0 | ⬜ pending |
| 397-04-04 | 04 | 4 | INJ-12 | T-397-17 | Human-verify link Pre/Post end-to-end (checkpoint, exempt from automated verify) | manual | manual (checkpoint:human-verify — see Manual-Only Verifications) | n/a | ⬜ pending (manual) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Note: every `auto` task has an `<automated>` verify (build / `dotnet test` / Playwright). The single checkpoint task (397-04-04, `checkpoint:human-verify`) is exempt from the Nyquist automated-verify requirement → `nyquist_compliant: true`.*

---

## Wave 0 Requirements

- [ ] xUnit suite untuk wiring link (LinkedGroupId adopt/Kasus-B-write, per-pekerja LinkedSessionId bidirectional, atomic rollback) — `HcPortal.Tests/`
- [ ] Playwright e2e (modal picker + chip + ringkasan pairing + unlink confirm — runtime Razor/JS, lesson Phase 354) — `tests/e2e/`

*Planner finalize daftar Wave 0 dari RESEARCH §Validation Architecture.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Pasangan silang inject↔online tampil utuh di /CMP/Records + Monitoring | INJ-12 | UAT browser live (data online disentuh — Kasus B) | localhost:5277 AD-off: inject Pre → link ke Post online standalone → cek Records pasangan + gain-score; snapshot+restore DB (CLAUDE.md Seed) |

*Sebagian besar perilaku punya automated verify; UAT browser tetap utk konfirmasi "seakan online".*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
