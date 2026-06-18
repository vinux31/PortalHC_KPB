---
phase: 394
slug: page-setup-room-authoring-soal
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-17
validated: 2026-06-18
---

# Phase 394 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (Razor + JS runtime → mandatory per Phase 354 lesson) |
| **Config file** | existing test project (KPB.Tests) + tests/e2e Playwright config |
| **Quick run command** | `dotnet build` (0 error gate) |
| **Full suite command** | `dotnet test` (xUnit) + Playwright e2e (`--workers=1`) |
| **Estimated runtime** | ~60–120s (build+unit); Playwright ~adds per-spec |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite + Playwright green; `dotnet run` (localhost:5277) manual UAT
- **Max feedback latency:** ~120 seconds (build+unit)

---

## Per-Task Verification Map

> Populated by gsd-planner from the plan tasks. Razor markup/JS contracts (wizard nav, picker binding, cert radio toggle, authoring capture, Cek-title) are runtime-driven → verify via Playwright, NOT grep/build alone.

| Task ID | Plan | Requirement | Threat Ref | Secure Behavior | Test Type | Test File | Automated Command | Status |
|---------|------|-------------|------------|-----------------|-----------|-----------|-------------------|--------|
| 394-01 | 01/04 | INJ-03 | T-394-RBAC | RBAC Admin,HC server-side; Coachee denied (wizard NOT render); 6-pill nav fwd/back | playwright | `inject-assessment-394.spec.ts` describe `INJ-03 RBAC + page` (4 tests) | Playwright `--workers=1` | ✅ green |
| 394-02a | 02 | INJ-04 | T-394-02-future-date | Setup room + Cek Judul (reuse CheckTitleAvailability) + backdate `max=today` guard (future → is-invalid, advance blocked) | playwright | `...spec.ts` describe `INJ-04 setup + cek judul` (2 tests) | Playwright `--workers=1` | ✅ green |
| 394-02b | 02 | INJ-06 | T-394-02-IDOR | Worker picker search/filter/select-all; ≥1 required; live count panel; only active users | playwright | `...spec.ts` describe `INJ-06 worker picker` (1 test) | Playwright `--workers=1` | ✅ green |
| 394-03a | 03 | INJ-05 | T-394-03-XSS-question / nodbwrite | Authoring type-toggle MC/MA/Essay; add soal → injQuestions[] row; form clears; **no reload/no POST**; ≥1 required | playwright | `...spec.ts` describe `INJ-05 authoring` (1 test) | Playwright `--workers=1` | ✅ green |
| 394-03b | 03 | INJ-07 | T-394-03-certdup | Cert radio 3-mode (None/Auto/Manual) conditional blocks + Permanent disables ValidUntil | playwright | `...spec.ts` describe `INJ-07 cert radio` (1 test) | Playwright `--workers=1` | ✅ green |
| 394-04a | 04 | INJ-03..07 (D-07) | T-394-03-nodbwrite | Step-5 placeholder navigable; step-6 confirm summary (textContent); edit-from-confirm; **no DB write GET+POST** (AssessmentSessions count unchanged) | playwright | `...spec.ts` describe `D-07 step5 placeholder + confirm` (3 tests) | Playwright `--workers=1` | ✅ green |
| 394-04b | 04 | POST mapping | T-394-04-NIPforge / future-backdate | `MapToRequest(vm,nip)`: scalars (never "Manual" type), questions (MC opts + Essay rubrik + Order), cert (Manual/Auto/None + Permanent→null), UserId→NIP (null-NIP skipped, Answers empty) | xunit | `InjectViewModelMapTests.cs` (4 facts) | `dotnet test --filter Category!=Integration` | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

**Suite:** xUnit 351/351 (incl. 4 mapping facts) · Playwright inject spec 13/13 PASS, 0 skipped (AD-off, main tree, `--workers=1`). All requirements automated.

---

## Wave 0 Requirements

- [x] Playwright spec scaffold for inject wizard (RBAC + setup + authoring + picker + cert toggle) — INJ-03..07 → `inject-assessment-394.spec.ts` (13 tests, all un-skipped)
- [x] xUnit ViewModel→InjectRequest field map (scalars/questions/cert/UserId→NIP sans Answers) → `InjectViewModelMapTests.cs` (4 facts)
- [x] AD-off env for local Playwright (`Authentication__UseActiveDirectory=false`) — lesson Phase 355 (applied)

*Wave 0 complete — inject-specific specs delivered + green.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Visual layout fidelity vs CreateAssessment (mirror) | INJ-04 | Subjective visual parity | `dotnet run` → /Admin/InjectAssessment → compare wizard look to /Admin/CreateAssessment |

*Most behaviors automatable via Playwright; planner to maximize automated coverage.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (0 MISSING)
- [x] No watch-mode flags (Playwright `--workers=1`)
- [x] Feedback latency < 120s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-18 — all 5 requirements (INJ-03..07) + POST mapping automated; 0 gaps.

---

## Validation Audit 2026-06-18

| Metric | Count |
|--------|-------|
| Requirements | 5 (INJ-03..07) + POST mapping + D-07 no-DB-write |
| COVERED | 7/7 (Playwright 13/13 + xUnit 4 facts) |
| PARTIAL | 0 |
| MISSING | 0 |
| Gaps found | 0 |
| Resolved | 0 (none needed) |
| Escalated | 0 |

**Verdict: NYQUIST-COMPLIANT.** No gaps — auditor spawn skipped. Coverage verified by reading test files against requirement behaviors; green status from Plan-04 SUMMARY (351/351 + 13/13) + gsd-verifier PASSED 5/5. Manual-only "visual layout parity" (INJ-04) is legitimately subjective, not a coverage gap.
