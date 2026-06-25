---
phase: 420
slug: form-create-edit-persistensi-field-ux-pre-post
status: complete
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-22
validated: 2026-06-23
---

# Phase 420 — Validation Strategy

> Per-phase validation contract. Per-task map diverifikasi `/gsd-validate-phase 420` 2026-06-23.
> Hasil: **NYQUIST-COMPLIANT** — 11/11 requirement (FORM-01..11) punya test otomatis hijau, 0 gap.
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

| Plan | Requirement | Threat Ref | Secure Behavior | Test Type | Test File / Method | Automated Command | Status |
|------|-------------|------------|-----------------|-----------|--------------------|-------------------|--------|
| 420-01 | FORM-01 | T-420-04 | shuffle tersimpan setelah Edit (tak reset OFF, akar E-01) | unit + e2e | `FormPersistence420Tests.EditStdLoop_ShuffleWrite_PersistsTrueWhenModelTrue` + `tests/e2e/form-persistence-420.spec.ts` (FORM-01 lifecycle) | `dotnet test --filter FormPersistence420Tests` · `npx playwright test form-persistence-420 --workers=1` | ✅ green |
| 420-01 | FORM-02 | T-420-01 | retake config disalin 3 jalur build Create + clamp | unit | `FormPersistence420Tests.Create_{Standard,Pre,Post}Build_*` (3) | `dotnet test --filter FormPersistence420Tests` | ✅ green |
| 420-01 | FORM-03 | T-420-01 | retake config disalin ke SEMUA sibling Edit std loop + clamp | unit | `FormPersistence420Tests.EditStdLoop_PersistsValidUntilAndRetakeToAllSiblings` | `dotnet test --filter FormPersistence420Tests` | ✅ green |
| 420-01 | FORM-04 | — | ValidUntil tersimpan ke SEMUA sibling Edit std loop | unit | `FormPersistence420Tests.EditStdLoop_PersistsValidUntilAndRetakeToAllSiblings` | `dotnet test --filter FormPersistence420Tests` | ✅ green |
| 420-02 | FORM-05 | T-420-lock | sesi/grup Completed tolak edit metadata (group-aware, server-side) | unit | `EditGuardRedirect420Tests.EditCompletedLockGuard_*` (3: group-aware/standard/all-open negatif) | `dotnet test --filter EditGuardRedirect420Tests` | ✅ green |
| 420-02 | FORM-06 | T-420-manual | GET Edit IsManualEntry → redirect EditManualAssessment | unit | `EditGuardRedirect420Tests.EditManualRedirect_*` (2: redirect/online negatif) | `dotnet test --filter EditGuardRedirect420Tests` | ✅ green |
| 420-03 | FORM-07 | — | SamePackage di header section Pre-Post (bukan kartu Post) | e2e | `tests/e2e/form-prepost-ux-420.spec.ts` (FORM-07) | `npx playwright test form-prepost-ux-420 --workers=1` | ✅ green |
| 420-03 | FORM-08 | — | dua sub-kartu Pre-Post muncul / hilang Standard | e2e | `form-prepost-ux-420.spec.ts` (FORM-08/10) | `npx playwright test form-prepost-ux-420 --workers=1` | ✅ green |
| 420-03 | FORM-09 | T-420-stale | std jadwal + schedHidden/ewcdHidden disabled saat Pre-Post (anti-POST) | e2e | `form-prepost-ux-420.spec.ts` (FORM-09) | `npx playwright test form-prepost-ux-420 --workers=1` | ✅ green |
| 420-03 | FORM-10 | T-420-massassign / binding-break | rename CreationMode binding utuh + regresi mode-switch | e2e | `form-prepost-ux-420.spec.ts` (FORM-08/10) + `assessment.spec.ts` 8.3/8.4 | `npx playwright test form-prepost-ux-420 --workers=1` · `assessment.spec.ts -g "8\.[34]"` | ✅ green |
| 420-03 | FORM-11 | — | blok Ujian Ulang hidden + input retake disabled saat Pre-Post | e2e | `form-prepost-ux-420.spec.ts` (FORM-11) | `npx playwright test form-prepost-ux-420 --workers=1` | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky.*

---

## Wave 0 Requirements

- [x] `HcPortal.Tests/FormPersistence420Tests.cs` — FORM-01/02/03/04 (shuffle/retake/validuntil persistence, 5 Fact). **10/10 green** (gabung EditGuard).
- [x] `HcPortal.Tests/EditGuardRedirect420Tests.cs` — FORM-05/06 (Completed lock group-aware + manual redirect, 5 Fact).
- [x] `tests/e2e/form-persistence-420.spec.ts` — FORM-01 lifecycle (create shuffle ON → Edit → submit → reopen MASIH checked).
- [x] `tests/e2e/form-prepost-ux-420.spec.ts` — FORM-07..11 (UX render per-mode + regresi Standard, 5 scenario).

> CATATAN: nama file final beda dari draft (`Form420PersistenceTests.cs`→`FormPersistence420Tests.cs`+`EditGuardRedirect420Tests.cs`; `form-420.spec.ts`→`form-persistence-420.spec.ts`+`form-prepost-ux-420.spec.ts`). Cakupan REQ identik/lebih lengkap.

---

## Backward-Compat Guard (regresi)

Mode **Standard** WAJIB tak berubah perilaku DOM+payload. Dilindungi:
- e2e `form-prepost-ux-420.spec.ts` skenario "Regresi Standard" (round-trip Pre-Post→Standard restore DOM tunggal) — ✅ green.
- e2e `assessment.spec.ts` 8.3/8.4 (mode-switch Status, rename-critical) — ✅ green.
- unit `EditCompletedLockGuard_StandardCompleted_*` (regresi guard standard) — ✅ green.
- `assessment.spec.ts` 8.1/8.2 — **FIXED 2026-06-23** (debt scaffold Phase 308, BUKAN regresi 420): diubah ke kontrak observable value+class spt 8.3/8.4; tak lagi `toBeVisible`/`selectOption` atas `#Status` (step-3) dari step-1. **FLOW 8 (8.1-8.4) hijau penuh.**

---

## Validation Audit 2026-06-23

| Metric | Count |
|--------|-------|
| Requirements (FORM-01..11) | 11 |
| COVERED (green) | 11 |
| PARTIAL | 0 |
| MISSING | 0 |
| Gaps found | 0 |

**Verdict: NYQUIST-COMPLIANT.** State A audit — semua REQ sudah punya test otomatis hijau (unit 10/10 + e2e 7/7), 0 gap → tidak perlu generate test (nyquist-auditor di-skip per Step 3). Run live 2026-06-23 @5270 mengkonfirmasi produk.
