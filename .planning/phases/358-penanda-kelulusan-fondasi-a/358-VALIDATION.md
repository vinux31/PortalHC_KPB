---
phase: 358
slug: penanda-kelulusan-fondasi-a
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-10
updated: 2026-06-14
hygiene_note: "Frontmatter flipped 2026-06-14 (post-exec) — kerja faktanya hijau: integration 5/5 real-SQL + suite 148/148, VERIFICATION 5/5 must-haves. Frontmatter ini template pre-exec yg lupa di-flip. Per-task map body biarkan apa adanya (snapshot rencana)."
---

# Phase 358 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution. Detail teknis: lihat `358-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter ProtonCompletionServiceTests` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30–90 detik (integration real-SQL lebih lambat dari unit) |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error) + `dotnet test --filter <relevant>`
- **After every plan wave:** `dotnet test` (full suite hijau)
- **Before `/gsd-verify-work`:** full suite hijau + UAT lokal:5277 (`Authentication__UseActiveDirectory=false dotnet run`)
- **Max feedback latency:** ~90 detik

---

## Per-Task Verification Map

> Diisi/diperhalus oleh planner saat PLAN.md dibuat. Baseline dari RESEARCH §Validation Architecture.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 358-01-01 | 01 | 0/1 | PCOMP-04 | — | migration `Origin` apply + baris lama="Interview" | integration (real-SQL) | `dotnet test --filter OriginMigration` | ❌ W0 | ⬜ pending |
| 358-02-01 | 02 | 1 | PCOMP-03 | — | `EnsureAsync` idempotent (call ke-2 return false) | integration (real-SQL) | `dotnet test --filter ProtonCompletionServiceTests` | ❌ W0 | ⬜ pending |
| 358-02-02 | 02 | 1 | PCOMP-02 | — | `RemoveExamOriginAsync` hapus Origin="Exam" saja; Bypass/Interview kebal | integration (real-SQL) | `dotnet test --filter ProtonCompletionServiceTests` | ❌ W0 | ⬜ pending |
| 358-03-01 | 03 | 2 | PCOMP-01 | — | exam Proton lulus → penanda Origin="Exam" terbit (GradeAndCompleteAsync + FinalizeEssayGrading defensive D-05a) | integration / UAT | `dotnet test` + UAT @5277 | ❌ W0 | ⬜ pending |
| 358-03-02 | 03 | 2 | PCOMP-02 | — | re-grade Fail→Pass terbit, Pass→Fail hapus Origin=Exam | integration / UAT | `dotnet test` + UAT @5277 | ❌ W0 | ⬜ pending |
| 358-04-01 | 04 | 2 | PCOMP-05 | — | backfill idempotent: exam Tahun1/2 lulus + deliverable 100% → penanda; re-run tak duplikat | integration / manual @5277 | `dotnet test` + manual backfill | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Task IDs tentatif — planner finalize sesuai pemecahan PLAN.*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/ProtonCompletionServiceTests.cs` — fixture integration real-SQL (pola TEST-05 / `OrgLabelMigrationIntegrationTests.cs:24-66`) untuk EnsureAsync/RemoveExamOrigin/GetPassedYears.
- [ ] Reuse fixture disposable `ApplicationDbContext` existing — TIDAK perlu install framework (xUnit sudah ada).

*Unit murni tidak relevan di 358 — service butuh DbContext. `ProtonYearGate` unit test = Phase 359 (D-02, out of scope).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Dashboard CDP/HistoriProton tampil "Lulus" untuk exam Tahun 1/2 | PCOMP-01 | Render UI + integrasi dashboard read-path (`CDPController.cs:377`) | UAT @5277: grade Proton Tahun 1 lulus → buka dashboard coachee → cek status "Lulus/Completed" |
| Backfill 1x via endpoint admin di data nyata | PCOMP-05 | Operasi 1x idempotent atas data existing; butuh snapshot DB (SEED_WORKFLOW) | Snapshot DB → POST endpoint backfill (admin) → cek jumlah penanda + dashboard worker lama → restore bila perlu |
| Interview Tahun 3 tetap terbit Origin="Interview" | PCOMP-03 | Alur HC input interview (offline) | UAT @5277: SubmitInterviewResults Tahun 3 lulus → cek penanda Origin="Interview" |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (fixture ProtonCompletionServiceTests)
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
