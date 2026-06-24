---
phase: 424
slug: grading-de-dup-flow-linking-gating-pre-post
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-24
---

# Phase 424 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `424-RESEARCH.md` §Validation Architecture. migration=FALSE.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) |
| **Config file** | existing test project (`*.Tests`) — no new framework |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Grading\|FullyQualifiedName~Sibling\|FullyQualifiedName~Essay\|FullyQualifiedName~StartExam"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~ pure-unit seconds; real-SQL (SQLEXPRESS) minutes |

> Real-SQL integration uses disposable SQLEXPRESS fixture recipe from 422/423 (`IClassFixture`, `NoOpHubContext`, GradingService ctor). Existing extend-targets per RESEARCH: `GradingDedupeFixture`/`GradingDedupeTests`, `AssessmentScoreAggregatorTests`, `EnsureCanSubmitStandardTests`, `SiblingPrePostFilterTests`, `EssayEmptyPendingParityTests`.

---

## Sampling Rate

- **After every task commit:** Run quick run command
- **After every plan wave:** Run full suite command
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** pure-unit < 30s

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 424-01-xx | 01 | 1 | GRDF-02 | — | Server-authoritative unified scorer; identical numeric result (parity) | parity-characterization (pure + real-SQL) | `dotnet test --filter FullyQualifiedName~Grading` | ⬜ TBD planner | ⬜ pending |
| 424-0x-xx | 0x | x | GRDF-01 | — | Post StartExam blocked when paired Pre ≠ Completed; orphan passes | real-SQL integration | `dotnet test --filter FullyQualifiedName~StartExam` | ⬜ TBD planner | ⬜ pending |
| 424-0x-xx | 0x | x | GRDF-03/04 | — | Pairing filtered per UserId; Standard no title-pseudo-link | pure-unit + real-SQL | `dotnet test --filter FullyQualifiedName~Sibling` | ⬜ TBD planner | ⬜ pending |
| 424-0x-xx | 0x | x | GRDF-05 | — | ElapsedSeconds includes ExtraTimeMinutes uniformly | pure-unit | `dotnet test --filter FullyQualifiedName~Elapsed` | ⬜ TBD planner | ⬜ pending |
| 424-0x-xx | 0x | x | GRDF-07 | — | On-time empty essay rejects whole submit; timeout finalizes | real-SQL integration | `dotnet test --filter FullyQualifiedName~Essay` | ⬜ TBD planner | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky. Planner/nyquist-auditor finalizes task IDs + Wave 0 stubs.*

---

## Wave 0 Requirements

- [ ] Extend existing test files (above) with RED stubs for GRDF-01/02/03/04/05/07 before implementation.
- [ ] Parity-characterization tests for unified scorer (esp. MC >1-response last-write-wins, MA set-equality, Essay EssayScore>0/null=pending) MUST pass against current behavior BEFORE refactor (lock parity), then stay green after convergence.

*Existing infrastructure (xUnit + SQLEXPRESS fixture) covers all phase requirements — no new framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Post terblok redirect + pesan ramah di browser | GRDF-01 | UX redirect/TempData visible only in live browser | Playwright/manual @5270: peserta dgn Pre belum Completed → buka Post → expect redirect ke Assessment + pesan "Selesaikan Pre-Test dulu" |
| Submit on-time essay kosong ditolak utuh di browser | GRDF-07 | Server re-render + message UX | Manual @5270: isi sebagian essay kosong, submit sebelum timeout → expect blocked + pesan; timeout path → finalize |

*Automated tests cover the logic; the two rows above are UAT confirmations.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references + parity locks
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s (pure-unit)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
