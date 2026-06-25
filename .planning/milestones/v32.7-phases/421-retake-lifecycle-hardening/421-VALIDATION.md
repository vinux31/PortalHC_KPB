---
phase: 421
slug: retake-lifecycle-hardening
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-23
validated: 2026-06-23
---

# Phase 421 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `421-RESEARCH.md` §Validation Architecture. Per-task map finalized at plan/validate-phase.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (net8.0) + Playwright (e2e, opsional UI confirm) |
| **Config file** | `tests/*.csproj` (existing test project) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Retake"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~60-120 detik (unit); e2e terpisah |

---

## Sampling Rate

- **After every task commit:** `dotnet test --filter "FullyQualifiedName~Retake"` (+ `~ResetGuard` bila tersentuh)
- **After every plan wave:** `dotnet test` (full suite)
- **Before `/gsd-verify-work`:** Full suite green + build 0-err
- **Max feedback latency:** ~120 detik

---

## Per-Task Verification Map

> Finalized `/gsd-validate-phase` 2026-06-23. Pemetaan REQ→test live (re-run hijau). Status: semua COVERED otomatis; UI confirm/modal/round-trip = Manual-Only (Playwright UAT 4/4, lihat 421-03-SUMMARY).

| Requirement | Secure Behavior | Test Type | Target Test (method) | Status |
|-------------|-----------------|-----------|----------------------|--------|
| RTH-01 | `CanRetake` return false saat `UtcNow.AddHours(7) > ExamWindowCloseDate`; `ExecuteAsync` abort SEBELUM RemoveRange (sesi live utuh) | unit (boundary +7h) + integration (abort-before-destroy) | `RetakeRulesTests` (boundary +7h, window open/closed/null) · `RetakeServiceTests.Execute_WindowClosed_AbortsBeforeDestroy` / `Execute_WindowOpen_ProceedsNormally` | ✅ COVERED |
| RTH-02 | Reset HC → `NomorSertifikat == null` pasca `ExecuteAsync` | integration | `RetakeServiceTests.Execute_ResetPassedSession_NullsNomorSertifikat` | ✅ COVERED |
| RTH-03 | Counting cap == warning (helper `RetakeCountingRules` snapshot-presence, paritas 4 situs) | unit/integration (paritas) | `RetakeCountingRulesTests.Parity_CapAndWarning_ShareSnapshotFilter` / `CountForUser_ExcludesLegacyArchives` / `MaxInGroup_ReturnsMaxAcrossUsers` / `MaxInGroup_EmptyGroup_ReturnsZero` / `CountForUser_CountsSnapshotArchives` | ✅ COVERED |
| RTH-04 | Hapus peserta Abandoned/ber-riwayat → ditolak tanpa flag; dgn flag → hapus + 0 orphan `AssessmentAttemptResponseArchive` | integration (guard + cascade) | `ParticipantRemoveGuardTests` ×5 (`AbandonedSession`/`StartedSession`/`AttemptHistorySession`_WithoutFlag_NeedsConfirm · `WithFlag_DeletesSessionAndArchives_NoOrphan` · `NoHistorySession_WithoutFlag_DeletesDirectly`) | ✅ COVERED |
| RTH-05 | Turunkan MaxAttempts < terpakai → warning non-blocking (simpan tetap berhasil); used-count via helper snapshot | integration | `RetakeSettingsEndpointTests.UpdateRetakeSettings_MaxBelowUsed_StillSaves_NonBlocking` / `UsedCount_ViaHelper_ExcludesLegacy` | ✅ COVERED |

---

## Wave 0 Requirements

- [x] Stub/extend test fixtures: `RetakeRules` (window-gate boundary), RetakeService (abort-before-destroy + cert-null + counting parity), EditAssessment participant-remove guard (`ParticipantRemoveGuardTests`), UpdateRetakeSettings warning (`RetakeSettingsEndpointTests`), counting helper (`RetakeCountingRulesTests`).
- [x] Reuse existing seed/fixtures dari v32.4 (AttemptHistory + Archive) via `RetakeServiceFixture`. Tidak ada framework baru.

*Existing xUnit infrastructure covers all phase requirements (extend, jangan bikin baru).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Modal konfirmasi cabut cert (D-04) + modal pra-simpan MaxAttempts (D-07) + soft-confirm round-trip hapus peserta (D-06) | RTH-02/04/05 | UI confirm/toast Razor — verifikasi live | Playwright e2e @5270 atau UAT browser: picu tiap konfirmasi, pastikan non-blocking + server-authoritative |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (0 MISSING)
- [x] No watch-mode flags
- [x] Feedback latency < 120s (slice 76 test / 21s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** validated 2026-06-23 — semua 5 REQ COVERED otomatis, 0 gap.

---

## Validation Audit 2026-06-23

State A audit (VALIDATION.md skeleton sudah ada → finalisasi). Re-run live test slice (`~Retake|~ParticipantRemove`): **Passed 76 / Failed 0 / Skipped 0 (21s)**. Full suite 649/0/2 (421-03-SUMMARY). UI confirm/modal/round-trip diverifikasi Playwright UAT 4/4 (Manual-Only by design — render-class).

| Metric | Count |
|--------|-------|
| Requirements | 5 (RTH-01..05) |
| COVERED (automated) | 5 |
| PARTIAL | 0 |
| MISSING | 0 |
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

Auditor spawn dilewati (0 gap). Test baru dilewati (semua REQ sudah ada test hijau dari eksekusi 421-01/02/03).
