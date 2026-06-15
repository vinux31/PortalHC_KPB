---
phase: 387
slug: post-lisensor-assessment-polish
status: audited
nyquist_compliant: false
nyquist_outcome: partial
wave_0_complete: true
created: 2026-06-15
audited: 2026-06-16
---

# Phase 387 — Validation (Audited)

> Post-execution Nyquist audit. 4 REQ automated + 3 REQ manual-only (justified). Phase shipped + verified (verifier 7/7).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e) |
| **Config file** | HcPortal.Tests/HcPortal.Tests.csproj · tests/playwright.config.ts |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (347/347 GREEN) |
| **Integration command** | `dotnet test --filter "Category=Integration"` (PostLisensorPolishTests 8/8 — needs local SQL up) |
| **E2E command** | `cd tests; npx playwright test aria-opsi-387 --workers=1` (3/3 PASS) |
| **Estimated runtime** | ~60s unit · ~120s e2e |

---

## Per-Task Verification Map

| Requirement | Plan | Wave | Secure Behavior | Test Type | Test File / Facts | Status |
|-------------|------|------|-----------------|-----------|-------------------|--------|
| PXF-06 | 387-01 | W1 | SubmitEssayScore WR-01 type-guard + WR-02 ownership-guard + status-guard (reject Completed, allow PendingGrading) | unit (Integration) | `PostLisensorPolishTests.cs`: `SubmitEssayScore_NonEssayQuestion_Rejected…` (WR-01), `…CrossSessionQuestion_Rejected…` (WR-02), `…CompletedSession_RejectedByStatusGuard`, `…PendingGradingValidEssay_SavesScore` (4 facts) | ✅ COVERED |
| PXF-08 | 387-01 | W1 | Cert number retry 3x on collision + LogError + surface certError to HC | manual | DbUpdateException collision impractical to force in unit; verified manual (finalize sesi 169 → NomorSertifikat assigned, no certError on success) — APPROVED 387-04 | 🟡 MANUAL-ONLY |
| PXF-09 | 387-01 | W1 | Excel BulkExport "Detail Jawaban" essay cell → TextAnswer + "Skor: x/y" / "Belum dinilai" / "Tidak dijawab" | unit (Integration) | `PostLisensorPolishTests.cs`: `EssayCell_GradedAnswer_ShowsTextAndScore`, `EssayCell_BlankAnswer_ShowsTidakDijawab` (2 facts) | ✅ COVERED |
| PXF-10 | 387-01 | W1 | FinalizeEssayGrading broadcasts `workerSubmitted` to monitor-{batchKey} group (real worker name) | manual | SignalR real-time multi-tab; verified manual (JoinMonitor → received live `{sessionId:169,score:100,result:Pass}`) — APPROVED 387-04. (Worker-name fix 61b4e4ef eager-loads User) | 🟡 MANUAL-ONLY |
| PXF-11 | 387-03 | W1 | Results + ExamSummary option-image aria-label contains letter "opsi A/B/C/D" | e2e (Playwright) | `tests/e2e/aria-opsi-387.spec.ts` — runtime aria-label assert on BOTH surfaces (3 tests, D-09 mandatory per lesson Phase 354) | ✅ COVERED |
| PXF-12 | 387-02 | W1 | SubmitExam MC upsert guarded by `answers.ContainsKey(q.Id)` — absent question NOT null-overwritten | unit (Integration) | `PostLisensorPolishTests.cs`: `McUpsert_AbsentQuestion_PreservesSavedAnswer`, `McUpsert_PresentQuestion_UpdatesAnswer` (2 facts) | ✅ COVERED |
| PXF-13 | 387-02 | W1 | SaveTextAnswer rejects essay write after timer expired (mirror SaveMultipleAnswer, accounts ExtraTimeMinutes) | manual | Timer-state manipulation; verified manual A/B (StartedAt=2020+1min EXPIRED→rejected unchanged; StartedAt=now+60min→success) — APPROVED 387-04. Logic is verbatim mirror of SaveMultipleAnswer guard | 🟡 MANUAL-ONLY |

*Status: ✅ COVERED (automated, green) · 🟡 MANUAL-ONLY (justified + human-verified) · ❌ MISSING*

**Coverage:** 4/7 REQ automated (PXF-06/09/11/12) · 3/7 REQ manual-only (PXF-08/10/13) · 0 MISSING.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Verified |
|----------|-------------|------------|----------|
| Cert-number collision error surfaced | PXF-08 | DbUpdateException unique-collision impractical to force deterministically in unit; retry-loop persistence is DB-integration-bound | ✅ APPROVED 387-04 (finalize sesi 169, cert assigned, no error on success) |
| Monitor tab live-updates on finalize | PXF-10 | SignalR multi-tab real-time broadcast; no in-process harness | ✅ APPROVED 387-04 (live `workerSubmitted` received without refresh) |
| Essay write rejected after timer expiry | PXF-13 | Timer-state (StartedAt/Duration) manipulation across SignalR session; guard is verbatim mirror of unit-adjacent SaveMultipleAnswer | ✅ APPROVED 387-04 (A=expired→rejected, B=valid→success) |

---

## Validation Sign-Off

- [x] Every REQ has verification (automated OR justified manual-only + human-verified)
- [x] No 3 consecutive REQ without any verification
- [x] Automated tests green (347/347 fast + 8/8 Integration + 3/3 Playwright per 387-VERIFICATION)
- [x] No watch-mode flags
- [x] Manual-only items documented with justification + APPROVED checkpoint
- [ ] `nyquist_compliant: true` — NOT set; outcome = PARTIAL (3 REQ manual-only by design, not automatable cheaply)

**Approval:** audited — PARTIAL (acceptable; phase shipped + verifier 7/7)

---

## Validation Audit 2026-06-16
| Metric | Count |
|--------|-------|
| Requirements | 7 |
| Automated (COVERED) | 4 (PXF-06/09/11/12) |
| Manual-only (justified) | 3 (PXF-08/10/13) |
| Missing | 0 |

State A audit (draft template → reflect post-execution reality). No new test code generated: the 3 gaps are legitimately manual-only (SignalR real-time, DbUpdateException collision, SignalR timer-state) and already human-verified APPROVED in 387-04. v31.0 auto-close chain.
