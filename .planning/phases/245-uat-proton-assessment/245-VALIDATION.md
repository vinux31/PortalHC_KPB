---
phase: 245
slug: uat-proton-assessment
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-24
---

# Phase 245 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Manual UAT (browser-based verification) |
| **Config file** | none — UAT phase, no automated test framework |
| **Quick run command** | `dotnet build` |
| **Full suite command** | `dotnet build && dotnet run` |
| **Estimated runtime** | ~30 seconds (build) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build`
- **After every plan wave:** Run `dotnet build && dotnet run`
- **Before `/gsd:verify-work`:** Full build must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 245-01-01 | 01 | 1 | PROT-01 | code-review | `grep -n "TahunKe" Controllers/CMPController.cs` | ✅ | ⬜ pending |
| 245-01-02 | 01 | 1 | PROT-02 | code-review | `grep -n "DurationMinutes" Data/SeedData.cs` | ✅ | ⬜ pending |
| 245-01-03 | 01 | 1 | PROT-03 | code-review | `grep -n "SubmitInterviewResults" Controllers/CMPController.cs` | ✅ | ⬜ pending |
| 245-01-04 | 01 | 1 | PROT-04 | code-review | `grep -n "ProtonFinalAssessment" Controllers/CMPController.cs` | ✅ | ⬜ pending |
| 245-02-01 | 02 | 2 | PROT-01 | manual-uat | Browser: Admin creates Proton Tahun 1 assessment | N/A | ⬜ pending |
| 245-02-02 | 02 | 2 | PROT-02 | manual-uat | Browser: Admin creates Proton Tahun 3 interview | N/A | ⬜ pending |
| 245-02-03 | 02 | 2 | PROT-03 | manual-uat | Browser: HC inputs interview results | N/A | ⬜ pending |
| 245-02-04 | 02 | 2 | PROT-04 | manual-uat | Browser: Verify ProtonFinalAssessment + certificate | N/A | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements. This is a UAT phase — no new test framework needed.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Admin creates Proton Tahun 1 assessment with track selection | PROT-01 | UI interaction + data seeding | Create assessment, assign peserta, verify worker can start exam |
| Admin creates Proton Tahun 3 interview (duration=0, no soal) | PROT-02 | UI interaction + validation rules | Create assessment with tipe interview, verify no paket soal required |
| HC inputs interview results (5 aspek, juri, catatan, IsPassed) | PROT-03 | Complex form interaction | Open monitoring detail, fill interview form, submit, verify saved |
| ProtonFinalAssessment auto-created + certificate accessible | PROT-04 | End-to-end flow across roles | Mark Tahun 3 passed, verify ProtonFinalAssessment created, check worker access |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
