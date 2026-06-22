---
phase: 420
slug: form-create-edit-persistensi-field-ux-pre-post
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-22
---

# Phase 420 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail per-task map diisi/diverifikasi oleh `/gsd-validate-phase 420` (nyquist-auditor) setelah eksekusi.
> Basis: `420-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (HcPortal.Tests) + Playwright (tests/e2e, TS) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj`, `playwright.config.ts` |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` |
| **Full suite command** | `dotnet test` + `npx playwright test --workers=1` |
| **Estimated runtime** | xUnit ~30-60s; e2e per-spec ~30-90s |

---

## Sampling Rate

- **After every task commit:** `dotnet build` + `dotnet test --filter "Category!=Integration"`
- **After every plan wave:** full xUnit suite
- **Before `/gsd-verify-work`:** full xUnit green + Playwright e2e form-420 green (@5270)
- **Max feedback latency:** ~60 detik (unit)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| (TBD plan) | — | — | FORM-01 | — | shuffle tersimpan setelah Edit (tak reset OFF) | unit+e2e | `dotnet test --filter ShuffleEditPersistence` | ❌ W0 | ⬜ pending |
| (TBD plan) | — | — | FORM-02/03 | — | retake config tersimpan Create+Edit | unit | `dotnet test --filter Retake.*Persist` | ❌ W0 | ⬜ pending |
| (TBD plan) | — | — | FORM-04 | T-420-lock | sesi Completed tolak edit metadata (group-aware) | unit | `dotnet test --filter EditLockCompleted` | ❌ W0 | ⬜ pending |
| (TBD plan) | — | — | FORM-05/06 | T-420-manual | Edit GET IsManualEntry → redirect EditManualAssessment | unit | `dotnet test --filter EditManualRedirect` | ❌ W0 | ⬜ pending |
| (TBD plan) | — | — | FORM-07..11 | — | render per-mode (sub-kartu/SamePackage header/retake hidden Pre-Post) + Standard tak berubah | e2e | `npx playwright test form-420 --workers=1` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky. Map final diisi gsd-validate-phase.*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/Form420PersistenceTests.cs` — stubs FORM-01..06 (shuffle/retake/validuntil/lock/redirect persistence)
- [ ] `tests/e2e/form-420.spec.ts` — stubs FORM-07..11 (UX render per-mode + backward-compat Standard)

---

## Backward-Compat Guard (regresi)

Mode **Standard** WAJIB tak berubah perilaku DOM+payload. Lindungi via test existing yang menyentuh CreateAssessment/EditAssessment standard path + assert Standard render identik (tak ada sub-kartu, retake tampil, input standard ter-POST).
