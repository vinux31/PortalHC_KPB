---
phase: 164
slug: hc-to-worker-push-events
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-13
---

# Phase 164 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet build + manual browser UAT |
| **Config file** | HcPortal.csproj |
| **Quick run command** | `dotnet build` |
| **Full suite command** | Manual: 2-browser test (HC + worker) for each push scenario |
| **Estimated runtime** | ~10 seconds (build), ~5 min (manual UAT) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Manual two-browser test of each push scenario
- **Before `/gsd:verify-work`:** All 3 PUSH requirements verified manually
- **Max feedback latency:** 10 seconds (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 164-01-01 | 01 | 1 | PUSH-01, PUSH-02, PUSH-03 | build | `dotnet build` | ✅ | ⬜ pending |
| 164-01-02 | 01 | 1 | PUSH-01 | manual | Reset → worker modal | N/A | ⬜ pending |
| 164-01-03 | 01 | 1 | PUSH-02 | manual | AkhiriUjian → worker modal | N/A | ⬜ pending |
| 164-01-04 | 01 | 1 | PUSH-03 | manual | AkhiriSemua → all workers modal | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. No new test framework needed — SignalR push events require live browser sessions for meaningful verification.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| HC Reset → worker modal within 1s | PUSH-01 | Requires two simultaneous browser sessions | 1. Open HC monitoring + worker exam in 2 tabs. 2. HC clicks Reset. 3. Verify worker sees "Sesi direset" modal < 1s. 4. Verify timer stops, form disabled. |
| HC Akhiri Ujian → worker redirect | PUSH-02 | Requires two simultaneous browser sessions | 1. Open HC monitoring + worker exam. 2. HC clicks Akhiri Ujian. 3. Verify worker sees "Diakhiri oleh pengawas" modal < 1s. 4. Click Lihat Hasil → Results page. |
| HC Akhiri Semua → all workers | PUSH-03 | Requires multiple simultaneous sessions | 1. Open HC monitoring + 2 worker exams. 2. HC clicks Akhiri Semua. 3. Both workers see modal < 1s. |
| Connection badge states | SC-4 | Requires network manipulation | 1. Verify badge shows "Live" when connected. 2. Disconnect network → "Reconnecting". 3. Wait for retry exhaustion → "Disconnected". |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
