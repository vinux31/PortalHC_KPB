---
phase: 401-proton-unit-resolution-hardening
plan: 04
subsystem: api
tags: [proton, assessment, unit-resolution, multi-unit, cert-gate, audit]

requires:
  - phase: 401-01
    provides: "D-03 channel pattern + AuditLogService primitive"
provides:
  - "AssessmentAdminController cert-issuing eligibility gate resolves AssignmentUnit-only; empty -> BLOCK + persisted AuditLog 'ProtonUnitUnresolved' + LogWarning"
affects: [404]

tech-stack:
  added: []
  patterns:
    - "Most security-sensitive resolver (session/cert issuance) uses GATE channel (persisted AuditLog), mirroring 401-02 GetEligibleCoachees"

key-files:
  created:
    - HcPortal.Tests/CertGateAuditTests.cs
  modified:
    - Controllers/AssessmentAdminController.cs

key-decisions:
  - "Actor resolved once before the gate foreach loop, null-safe ('system' fallback)"
  - "CertGateAuditTests in its OWN file (not UnitUnresolvedAuditTests) to keep 401-04 file-disjoint from 401-02 — both Wave-1 plans never touch the same test file"

patterns-established:
  - "Cert-gate is the highest-stakes application of GATE-channel: empty AssignmentUnit must never issue a session/cert against a primary-resolved unit"

requirements-completed: [PSU-01, PSU-05]

duration: ~9min
completed: 2026-06-18
---

# Phase 401 Plan 04: Cert-Gate AssignmentUnit-only Summary

**Hardened the AssessmentAdminController cert-issuing eligibility gate — empty AssignmentUnit now BLOCKs session/cert issuance (no primary-resolved unit) and persists a ProtonUnitUnresolved AuditLog.**

## Performance

- **Duration:** ~9 min
- **Tasks:** 2/2 (TDD)
- **Files:** 1 controller modified, 1 test created

## Accomplishments

- **Cert-gate (PSU-01/05)** — `AssessmentAdminController` gate (`:1411`, the path issuing `AssessmentSession` + `NomorSertifikat`) resolves `AssignmentUnit` only. Empty → coachee BLOCKED (`gateSkippedNotHundred++`, no cert against primary, D-02) + persisted `AuditLog` `ProtonUnitUnresolved` + `LogWarning` (D-03 gate channel). Actor resolved null-safe before loop.
- **Untouched:** year-gate (`gateSkippedPrevYear`), `trackHasDeliverables` fallback, per-unit deliverable scoping, `IsEligiblePerUnit`.
- **CertGateAuditTests** in its own file — proves persisted-channel + action label + cert context.

## Task Commits

1. **Task 1: Drop fallback + gate-block AuditLog at cert-gate** — `31492171` (feat)
2. **Task 2: CertGateAuditTests persisted-audit contract** — `b19c6016` (test)

## Files Created/Modified

- `Controllers/AssessmentAdminController.cs` — gate resolver (drop `Select(u => u.Unit)`) + actor null-safe + gate-block audit/warning
- `HcPortal.Tests/CertGateAuditTests.cs` — GREEN persisted-channel contract (own file)

## Verification

- `dotnet build` → **Build succeeded**
- `grep "Select(u => u.Unit)" AssessmentAdminController.cs` → **0** (fallback removed)
- `grep "ProtonUnitUnresolved"` → **1**; `grep "gateSkippedNotHundred++"` → **2** (baseline unchanged — BLOCK accounting intact)
- Filter `~CertGateAudit` → **1 passed**
- Full suite (`Category!=Integration`) → **386 passed / 0 failed / 4 skipped** (+1 vs Plan 02, 0 regression)
- Live `dotnet run` boot smoke → batched to phase-end (per 401-02 deviation)

## Deviations from Plan

**[Process - batched verification] Live boot smoke deferred to phase-end** — same rationale as 401-02 (pure backend gate change, no route/UI; batched once after last autonomous plan). **Impact:** none on correctness.

**Total deviations:** 1 (process, non-functional).

## Self-Check: PASSED

- key-files exist: ✓
- `git log --grep="401-04"` returns 2 commits: ✓ (31492171, b19c6016)
- Acceptance re-run: ✓ (0 Select(u=>u.Unit), ProtonUnitUnresolved≥1, gate _auditLog+LogWarning present, gateSkippedNotHundred++ unchanged at 2, cert-gate test passes, build 0 error)
- No `## Self-Check: FAILED` marker.
