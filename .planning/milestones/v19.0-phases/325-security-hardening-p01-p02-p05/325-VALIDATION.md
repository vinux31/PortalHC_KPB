---
phase: 325
slug: security-hardening-p01-p02-p05
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-05-27
---

# Phase 325 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Architecture detail di `325-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (NEW project `HcPortal.Tests/` — Wave 0 bootstrap per D-08) + Playwright TypeScript (existing `tests/` E2E manual UAT) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (NEW, Wave 0) + `tests/playwright.config.ts` (existing) |
| **Quick run command** | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` |
| **Full suite command** | `dotnet test` (sln-wide) |
| **Estimated runtime** | ~5-10 detik xUnit unit, ~30-60 detik Playwright kalau ada scenario relevant |

---

## Sampling Rate

- **After every task commit:** Run `dotnet build` + `dotnet test HcPortal.Tests/HcPortal.Tests.csproj` (kalau test project sudah ada)
- **After every plan wave:** Run `dotnet test` full suite
- **Before `/gsd-verify-work`:** Full suite + manual Postman path traversal + manual rename `.exe→.pdf` + manual FK delete scenario
- **Max feedback latency:** ~10 detik (xUnit) + ~5 menit (manual UAT batch)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Coverage | Test Type | Automated Command | Status |
|---------|------|------|----------|-----------|-------------------|--------|
| TBD-01 | 01 Bootstrap xUnit | 0 | D-08 test infra | infrastructure | `dotnet build HcPortal.Tests/` | ⬜ pending |
| TBD-02 | 02 Helper P01+P02 | 1 | SC1+SC2+SC3+SC6 | unit | `dotnet test --filter "FullyQualifiedName~FileUploadHelperTests"` | ⬜ pending |
| TBD-03 | 03 Refactor inline | 1 | P02 bypass close | manual + grep | `grep -n "allowedExtensions.Contains" Controllers/TrainingAdminController.cs` (should return 0) | ⬜ pending |
| TBD-04 | 04 P05 3 endpoint | 2 | SC4+SC5 | manual | `dotnet build` + manual delete UI scenario | ⬜ pending |
| TBD-05 | 05 UAT batch | 3 | All SC | manual | Postman + browser + SEED_WORKFLOW snapshot/restore | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Final task IDs di-resolve setelah `/gsd-execute-phase` start (depend on planner output).*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/HcPortal.Tests.csproj` — xUnit project bootstrap (target `net8.0`)
- [ ] `HcPortal.Tests/FileUploadHelperTests.cs` — 6 test case stub (3 valid magic byte + 3 invalid)
- [ ] `HcPortal.Tests/TestData/sample.pdf` + `sample.jpg` + `sample.png` + `fake-magic.pdf` (exe rename) + `empty.pdf` — test fixtures
- [ ] `HcPortal.sln` update — add `HcPortal.Tests` project entry
- [ ] NuGet packages: `Microsoft.NET.Test.Sdk` + `xunit` + `xunit.runner.visualstudio` (versions stable for net8.0 per RESEARCH.md)

---

## Manual-Only Verifications

| Behavior | Coverage | Why Manual | Test Instructions |
|----------|----------|------------|-------------------|
| Path traversal upload via Postman | SC1 | Multipart request manipulation tidak fit unit test | Postman: POST `/Admin/AddTraining` multipart filename=`../../test.pdf` → verify file landed di `uploads/certificates/` BUKAN parent folder |
| `.exe` rename `.pdf` reject | SC2 | File system / multipart fixture | Browser form upload `notepad.exe` di-rename `.pdf` → verify form error display "magic byte mismatch" |
| Normal PDF/JPG/PNG upload | SC3 | E2E regression smoke | Browser upload 3 real cert sample → verify save sukses + visible di list |
| FK delete with referencing | SC4 | DB state setup + UI check | SEED: TR A + TR B (RenewsTrainingId=A.Id) → UI delete A → verify TempData error + A masih ada |
| FK delete clean (no referencing) | SC5 | Regression no false-positive | SEED: TR C standalone → UI delete C → verify TempData success + C terhapus |

*Snapshot DB lokal sebelum SC4/SC5 per CLAUDE.md SEED_WORKFLOW; restore setelah verify selesai.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: tidak ada 3 consecutive tasks tanpa automated verify
- [ ] Wave 0 covers all MISSING references (xUnit infra bootstrap)
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s xUnit, manual UAT batched
- [ ] `nyquist_compliant: true` set di frontmatter setelah planner final + checker pass

**Approval:** pending
