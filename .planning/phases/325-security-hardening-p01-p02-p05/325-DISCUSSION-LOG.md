# Phase 325: Security Hardening (P01 + P02 + P05) - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves alternatives considered.

**Date:** 2026-05-27
**Phase:** 325-security-hardening-p01-p02-p05
**Mode:** auto (user typed `auto` in gray area multi-select — Claude applied recommended defaults to all 4 areas)
**Areas discussed:** Test Infrastructure, Magic Byte Constants Placement, P01 Logging Behavior, P05 Scope di AssessmentAdminController:2136

---

## Test Infrastructure

| Option | Description | Selected |
|--------|-------------|----------|
| A | Tambah project xUnit baru `HcPortal.Tests/` — foundation reusable v20.0+ (recommended) | ✓ |
| B | Skip unit test, rely on manual repro + Playwright E2E | |
| C | Inline test via temporary console verify (throwaway) | |

**User's choice:** A (auto — recommended default)
**Rationale:** Spec §5.4 minta 6 unit test `ValidateCertificateFile`. Existing `tests/` cuma Playwright (E2E TS). xUnit project = foundation reusable untuk v20.0+ phases yang akan butuh unit test (P03 cycle validator, Phase 327 `DeriveCertificateStatus`).

**Scope guard:** xUnit setup minimum viable. TIDAK CI integration, TIDAK migrate Playwright, TIDAK refactor controller untuk testability.

---

## Magic Byte Constants Placement

| Option | Description | Selected |
|--------|-------------|----------|
| A | Inline `switch` di `ValidateCertificateFile` (verbatim spec) | |
| B | Extract ke `AssessmentConstants.FileValidation.MagicBytes` dict + helper method (recommended) | ✓ |

**User's choice:** B (auto — recommended default)
**Rationale:** Konsisten dengan pattern existing (`AllowedCertificateExtensions` + `MaxCertificateFileSizeBytes` sudah di `AssessmentConstants.FileValidation`). Testable terpisah via helper `MatchesMagicByte(string ext, byte[] header)`. Easier extend kalau ada format tambahan future.

---

## P01 Logging Behavior

| Option | Description | Selected |
|--------|-------------|----------|
| A | Silent strip via `Path.GetFileName()` (verbatim spec) | |
| B | Log warning kalau filename mengandung path separator atau `..` (recommended) | ✓ |

**User's choice:** B (auto — recommended default)
**Rationale:** Audit trail untuk forensik security incident. Zero perf cost (1 string compare `originalName != file.FileName`). Spec bilang "silent strip" tapi visibility lebih bagus untuk attack pattern detection.

**Format:** `_logger.LogWarning("Path traversal attempt: filename={Original} stripped to {Safe}", file.FileName, safeFileName);` — level Warning (NOT Error karena defensive strip works, attack gagal).

---

## P05 Scope di AssessmentAdminController:2136

| Option | Description | Selected |
|--------|-------------|----------|
| A | Pre-check referencing di **awal endpoint** sebelum buka tx (recommended) | ✓ |
| B | Pre-check inside existing tx (sebelum cascade Phase 323) | |

**User's choice:** A (auto — recommended default)
**Rationale:** Gagal cepat — kalau referenced, return error tanpa buka tx scope. Phase 323 sudah punya tx scope existing untuk cascade `AssessmentEditLogs` + `PackageUserResponses` + `AttemptHistory` + `AssessmentPackages` (line 2040+). Pre-check di luar tx menjaga separation of concern. Tidak konflik dengan Phase 323 work — beda direction (Phase 323 cascade CHILD rows; Phase 325 cek PARENT referencing).

---

## Claude's Discretion (locked tanpa diskusi)

- xUnit version pinning — match SDK terbaru stable untuk net8.0
- Test naming convention — `MethodName_Scenario_ExpectedResult` (industry standard xUnit pattern)
- Magic byte `.jpeg` alias — handle sama dengan `.jpg` (lookup dictionary value sharing)
- LogWarning structured logging — parameterized format (`{Original}`, `{Safe}`), bukan string concat

## Deferred Ideas (dari diskusi/scope guard)

- Soft delete proper (`IsDeleted` column) → v20.0 backlog
- MimeDetective NuGet library → fallback opt-in kalau hardcoded miss
- DB CHECK constraint mutual exclusion (P09) → v20.0
- RBAC integration test (P12) → v20.0
- `DateTime.Now` standardize non-ValidUntil → v20.0 polish
- CI integration xUnit project → milestone DevOps terpisah
- Migrate Playwright tests/ → xUnit (TIDAK migrate, beda role)
