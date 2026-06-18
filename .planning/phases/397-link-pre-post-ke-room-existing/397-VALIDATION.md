---
phase: 397
slug: link-pre-post-ke-room-existing
status: draft
nyquist_compliant: false
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
| 397-01-01 | 01 | 0 | INJ-12 | — | N/A | unit | `dotnet test --filter Category!=Integration` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Planner: isi map penuh dari 397-RESEARCH.md §Validation Architecture (cross inject↔online grouping intact, per-pekerja bidirectional link, Kasus B online-write atomic+rollback, anti-double-link reject, preview==commit pairing, unlink reversal).*

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
