---
phase: 345
slug: assessment-pending-grade-display-fix
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-04
---

# Phase 345 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Detail Validation Architecture: lihat `345-RESEARCH.md` §Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`HcPortal.Tests`) + Playwright (`tests/e2e`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` · `playwright.config.ts` |
| **Quick run command** | `dotnet test HcPortal.Tests` |
| **Full suite command** | `dotnet test HcPortal.Tests` + `npx playwright test` (e2e UAT) |
| **Estimated runtime** | ~2–5 detik (xUnit) · ~30–60 detik (Playwright 3 surface) |

---

## Sampling Rate

- **After every task commit:** `dotnet test HcPortal.Tests` (xUnit cepat)
- **After every plan wave:** full xUnit; Playwright UAT setelah Wave akhir (345-04)
- **Before `/gsd-verify-work`:** xUnit hijau + Playwright 3 surface PASS
- **Max feedback latency:** ~5 detik (xUnit)

---

## Per-Task Verification Map

> Diisi gsd-planner saat generate plan (mapping task→test). Wajib: tiap REQ punya automated verify atau Wave 0 dependency.

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| _planner-fill_ | 01-03 | 1 | CMP06R-01..04 | — | label "Menunggu Penilaian" untuk IsPassed==null | unit/e2e | `dotnet test HcPortal.Tests` | ❌ W0 | ⬜ pending |
| _planner-fill_ | 04 | 2 | CMP06R-05 | — | passRate exclude pending + 3 surface UAT | unit/e2e | `dotnet test` + `npx playwright test` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/` — test class baru (VM nullable mapping + `ComputeHistoryStats`/passRate exclude-pending math). Infra xUnit sudah ada (InMemory pattern).
- [ ] `tests/e2e/` — Playwright spec baru 3 surface; helper login admin/hc + `dbSnapshot.ts` snapshot/restore sudah ada (SEED_WORKFLOW).

*Infra xUnit + Playwright sudah ada — Wave 0 = tambah test file, bukan install framework.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Warna badge amber visual (web + PDF) | CMP06R-01/03 | Render visual/warna PDF sulit assert otomatis penuh | Playwright screenshot 3 surface + buka PDF BulkExportPdf, cek "Menunggu Penilaian" + warna netral |

*Sisanya automated (xUnit math + Playwright DOM text assertion).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 5s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
