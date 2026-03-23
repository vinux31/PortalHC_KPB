---
phase: 236
slug: audit-completion
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-23
---

# Phase 236 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | dotnet test (xUnit/MSTest — if configured) |
| **Config file** | none — ASP.NET MVC project, validation via browser + controller inspection |
| **Quick run command** | `grep -based acceptance criteria checks` |
| **Full suite command** | `dotnet build && manual browser UAT` |
| **Estimated runtime** | ~30 seconds (build) + manual |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` to verify compilation
- **After every plan wave:** Build + grep acceptance criteria
- **Before `/gsd:verify-work`:** Full build + browser UAT
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 236-01-01 | 01 | 1 | COMP-01 | grep + build | `grep -n "IsUnique" Data/ApplicationDbContext.cs` | ❌ W0 | ⬜ pending |
| 236-01-02 | 01 | 1 | COMP-01 | grep | `grep -n "ProtonTrackAssignmentId" CDPController` | ✅ | ⬜ pending |
| 236-02-01 | 02 | 1 | COMP-02 | grep | `grep -n "EditCoachingSession\|DeleteCoachingSession" Controllers/CDPController.cs` | ❌ W0 | ⬜ pending |
| 236-03-01 | 03 | 2 | COMP-03 | grep | `grep -n "CoachingLog\|HistoriProton" Views/` | ✅ | ⬜ pending |
| 236-04-01 | 04 | 2 | COMP-04 | grep + build | `grep -n "IsCompleted\|CompletedAt" Models/CoachCoacheeMapping.cs` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] DB migration for unique constraint on ProtonFinalAssessment.ProtonTrackAssignmentId
- [ ] DB migration for IsCompleted/CompletedAt on CoachCoacheeMapping
- [ ] `dotnet build` must pass after each task

*Existing infrastructure covers compilation checks; manual browser UAT for UI behaviors.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Duplikat final assessment ditolak UI | COMP-01 | Browser interaction | Coba create 2x final assessment untuk assignment yang sama — harus ditolak |
| Coaching session link ke deliverable | COMP-02 | UI navigation | Buka coaching session → verifikasi deliverable progress terkait benar |
| HistoriProton timeline lengkap | COMP-03 | Visual inspection | Buka histori coachee → verifikasi timeline tanpa gap/duplikasi |
| Lifecycle tahun 1→2→3 end-to-end | COMP-04 | Multi-step flow | Jalankan full lifecycle dari assignment → completion → cek competency level |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
