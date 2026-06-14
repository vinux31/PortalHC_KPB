---
phase: 377
slug: impersonation-identity-across-surfaces
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-14
---

# Phase 377 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail dimensi diturunkan dari `377-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET) + Playwright (e2e) |
| **Config file** | `HcPortal.Tests/` (xUnit) · `tests/e2e/` (Playwright) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Impersonation"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30-60 detik (xUnit unit); e2e lebih lama (`--workers=1`) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~Impersonation"`
- **After every plan wave:** Run `dotnet test` (full suite — no regression SC4)
- **Before `/gsd-verify-work`:** Full suite must be green + e2e impersonate→Records hijau
- **Max feedback latency:** 60 detik (xUnit)

---

## Per-Task Verification Map

> Wave numbers: 4 waves total — RED(W1) → GREEN-foundation(W2) → surfaces(W3) → integration(W4).

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|
| 377-01-01 | 01 | 1 | IMP-02 | T-377-01, T-377-02 | Audit enumerasi SEMUA call-site identity-resolution (exhaustive); borderline diklasifikasi eksplisit (tak silently drop) | doc/grep parity | `powershell -Command "Test-Path '.planning/phases/377-impersonation-identity-across-surfaces/377-AUDIT.md'"` | ✅ 377-AUDIT.md |
| 377-01-02 | 01 | 1 | IMP-02 | T-377-03 | Kontrak resolver fail-closed dikunci RED: `(true,false,"user",null) → RoleModeEmpty` (bukan admin) | unit (pure-logic, RED) | `powershell -Command "Test-Path 'HcPortal.Tests/ImpersonationIdentityTests.cs'"` | ✅ ImpersonationIdentityTests.cs (RED) |
| 377-02-01 | 02 | 2 | IMP-01, IMP-02 | T-377-04, T-377-05, T-377-07 | Effective-user resolver single-source: mode-role/target-null → user=null (fail-closed); non-impersonate/expired → real (SC4) | unit (GREEN) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ImpersonationIdentity" --nologo` | ✅ Services/ImpersonationService.cs |
| 377-02-02 | 02 | 2 | IMP-01 | T-377-06 | Target null/terhapus → middleware Stop()+redirect /Admin/Index SEBELUM controller (D-04 fail-closed) | integration (suite) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` | ✅ Middleware/ImpersonationMiddleware.cs |
| 377-03-01 | 03 | 3 | IMP-01, IMP-02 | T-377-08, T-377-10, T-377-11, T-377-12 | CMP resolver impersonation-aware (user.Id=X); mode-role → Records kosong+hint, User=null (TIDAK tampilkan identitas admin, Option A locked) | unit + integration | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ResultsAuthorization|FullyQualifiedName~Impersonation" --nologo` | ✅ Controllers/CMPController.cs, Views/CMP/Records.cshtml |
| 377-03-02a | 03 | 3 | IMP-01 | T-377-08 | Assessment + ExportRecords bypass route ke effective user (mode-role → kosong, bukan admin) | integration (suite) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` | ✅ Controllers/CMPController.cs |
| 377-03-02b | 03 | 3 | IMP-01 | T-377-09 | StartExam write-on-GET di-guard `if(!IsImpersonating())` → tak menulis DB saat impersonasi (read-only); Records hint render | integration (suite) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` | ✅ Controllers/CMPController.cs, Views/CMP/Records.cshtml |
| 377-04-01 | 04 | 3 | IMP-01 | T-377-14 | CDP inject ImpersonationService (Pitfall 2) — build tak break sebelum resolver dipakai | build (compile) | `dotnet build HcPortal.csproj -clp:ErrorsOnly` | ✅ Controllers/CDPController.cs |
| 377-04-02 | 04 | 3 | IMP-01, IMP-02 | T-377-13, T-377-15, T-377-16, T-377-17 | CDP resolver nullable + impersonation-aware (seragam CMP); BuildSertifikatRows self-data = X; caller null-safe (no NRE), ViewBag.UserBagian tak bocor section admin | integration (suite) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` | ✅ Controllers/CDPController.cs |
| 377-05-01 | 05 | 3 | IMP-01, IMP-02 | T-377-18, T-377-19, T-377-20, T-377-21 | Home/Index GetProgress+GetUpcomingEvents pakai effective userId=X; split-brain L38/L53 folded; mode-role → kosong; Guide OUT | integration (suite) | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` | ✅ Controllers/HomeController.cs |
| 377-06-01 | 06 | 4 | IMP-01, IMP-02 | T-377-23, T-377-25 | Fidelity matrix D-01 (impersonate X: data-X OK, data-Y non-owner Forbid); seed temporary local-only (snapshot+restore) | unit + seed | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --filter "FullyQualifiedName~ResultsAuthorization" --nologo` | ✅ ResultsAuthorizationTests.cs, .planning/seeds/377-impersonation-fixtures.sql |
| 377-06-02 | 06 | 4 | IMP-01, IMP-02 | T-377-22 | e2e SC2/SC3 live: impersonate X → /CMP/Records, /Home, /CMP/Assessment = data X (bukan admin); full suite hijau (SC4) | e2e + full suite | `dotnet test HcPortal.Tests/HcPortal.Tests.csproj --nologo` (+ `npx playwright test tests/e2e/impersonation.spec.ts --workers=1`) | ✅ tests/e2e/impersonation.spec.ts |
| 377-06-03 | 06 | 4 | IMP-01 | T-377-22, T-377-24 | UAT browser: banner jujur lintas surface + D-03 kosong+hint + StartExam read-only + SC4 normal | manual (checkpoint blocking) | MANUAL — UAT @localhost:5277 (lihat Manual-Only Verifications) | n/a (checkpoint) |

*Status (diisi saat eksekusi): ⬜ pending · ✅ green · ❌ red · ⚠️ flaky. Saat plan-time semua ⬜ pending.*

---

## Wave 0 Requirements

- [ ] Test stub untuk resolver effective-user (IMP-01/02) — pure-logic seam (no Moq, pola proyek) → `377-01-02` (`ImpersonationIdentityTests.cs`, RED)
- [ ] e2e: extend `tests/e2e/impersonation.spec.ts` (IMP-02 flow) → assert `/CMP/Records` tampil data X → `377-06-02`
- [ ] Fixture: skenario impersonate mode=user (X valid), mode=role (no user), target-null (D-04) → matriks `377-01-02` (pure-logic) + seed `377-06-01`

*Wave 0 RED test = `377-01-02`; menjadi GREEN setelah `377-02-01` (Plan 02) merged. `wave_0_complete` jadi true hanya setelah eksekusi meng-GREEN-kan RED test ini.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Banner "Anda melihat sebagai X" jujur + data X di browser | IMP-01 | UAT visual lintas surface | Impersonate user X @localhost:5277 → buka /CMP/Records, /Home, Assessment → konfirmasi data = X |
| Mode-role → Records kosong + hint, TIDAK tampil identitas admin (D-03 Option A) | IMP-01 | Visual leak check (header tak boleh tampil nama admin) | Impersonate ROLE HC → /CMP/Records → konfirmasi kosong + "Pilih user spesifik", header tak tampil nama admin |
| StartExam read-only saat impersonasi (Pitfall 3) | IMP-01 | Cek efek samping DB (write-on-GET) | Impersonate X → StartExam assessment Upcoming → cek `SELECT Status FROM AssessmentSessions WHERE Id=<id>` tetap Upcoming |

*Local e2e SQL gotcha (STATE.md): start SQLBrowser + `lpc:` shared-memory conn override + `--workers=1`. AD lokal: `Authentication__UseActiveDirectory=false dotnet run`.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies (377-06-03 = checkpoint blocking by design)
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (377-01-02 RED → GREEN di 377-02-01)
- [x] No watch-mode flags
- [x] Feedback latency < 60s (xUnit quick filter)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** plans nyquist-compliant at plan level (setiap task punya `<automated>` atau Wave-0 dep). `wave_0_complete` tetap false sampai eksekusi meng-GREEN-kan RED test 377-01-02.
