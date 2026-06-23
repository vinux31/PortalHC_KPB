---
phase: 421
slug: retake-lifecycle-hardening
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-23
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

> Skeleton — diisi lengkap oleh planner (per-task) + `/gsd-validate-phase`. Pemetaan REQ→test di bawah dari RESEARCH §Validation Architecture.

| Requirement | Secure Behavior | Test Type | Target (extend) |
|-------------|-----------------|-----------|-----------------|
| RTH-01 | `CanRetake` return false saat `UtcNow.AddHours(7) > ExamWindowCloseDate`; `ExecuteAsync` abort SEBELUM RemoveRange (sesi live utuh) | unit (boundary +7h) + integration (abort-before-destroy) | `RetakeRules` tests + RetakeService tests |
| RTH-02 | Reset HC → `NomorSertifikat == null` pasca `ExecuteAsync` | integration | RetakeService tests + `ResetGuardTests` |
| RTH-03 | Counting cap == warning (helper `CountEraRetakeArchives` paritas 4 situs, snapshot-presence) | unit (paritas) | RetakeService/counting tests |
| RTH-04 | Hapus peserta Abandoned/ber-riwayat → ditolak tanpa flag; dgn flag → hapus + 0 orphan `AssessmentAttemptResponseArchive` | integration (guard + cascade) | EditAssessment participant tests |
| RTH-05 | Turunkan MaxAttempts < terpakai → warning non-blocking (simpan tetap berhasil) | integration/unit | UpdateRetakeSettings tests |

---

## Wave 0 Requirements

- [ ] Stub/extend test fixtures: `RetakeRules` (window-gate boundary), RetakeService (abort-before-destroy + cert-null + counting parity), EditAssessment participant-remove guard, UpdateRetakeSettings warning.
- [ ] Reuse existing seed/fixtures dari v32.4 (AttemptHistory + Archive). Tidak ada framework baru.

*Existing xUnit infrastructure covers all phase requirements (extend, jangan bikin baru).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Modal konfirmasi cabut cert (D-04) + modal pra-simpan MaxAttempts (D-07) + soft-confirm round-trip hapus peserta (D-06) | RTH-02/04/05 | UI confirm/toast Razor — verifikasi live | Playwright e2e @5270 atau UAT browser: picu tiap konfirmasi, pastikan non-blocking + server-authoritative |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 120s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
