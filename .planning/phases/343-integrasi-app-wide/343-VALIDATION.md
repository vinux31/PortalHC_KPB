---
phase: 343
slug: integrasi-app-wide
status: approved
nyquist_compliant: true
wave_0_complete: false
created: 2026-06-03
---

# Phase 343 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Phase 343 = refactor display string (literal "Bagian"/"Unit"/"Sub-unit" → `@OrgLabels.GetLabel(N)`). Formal xUnit + Playwright E2E = Phase 344 (TEST-01..06). Validasi 343 = sampling cheap: `dotnet build` + grep residual + spot-render.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (existing `*.Tests`) — full test = Phase 344. Phase 343 pakai compile + grep + spot-render |
| **Config file** | existing test project (`*.Tests/*.csproj`) |
| **Quick run command** | `dotnet build` (compile-time verify `@OrgLabels` + `@inject` resolve di semua view/partial) |
| **Full suite command** | `dotnet test` (regression existing; Phase 344 tambah TEST-01..06) |
| **Estimated runtime** | build ~30-60s; test ~existing |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (harus 0 error — bukti tiap `@OrgLabels.GetLabel(N)` resolve, termasuk di partial)
- **After every plan wave:** grep residual display literal hilang di file REPLACE + `dotnet build`
- **Before `/gsd-verify-work` (Phase 344):** `dotnet test` green + spot-render 1 page per area
- **Max feedback latency:** ~60 detik (build)

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 343-01-xx | 01 | 1 | ORG-INTEG-01 | — | @inject di `_ViewImports.cshtml` resolve; default label render Indonesian | build + grep | `dotnet build` ; `grep "@inject HcPortal.Services.IOrgLabelService" Views/_ViewImports.cshtml` | ✅ | ⬜ pending |
| 343-02-xx | 02 | 2 | ORG-INTEG-01 | — | View REPLACE targets pakai `@OrgLabels.GetLabel(N)`, no hardcode display tersisa | build + grep residual | `dotnet build` ; grep `>Bagian<`/`<th>Bagian</th>`/`-- Pilih Bagian --` = 0 di file REPLACE | ✅ | ⬜ pending |
| 343-0x-xx | 0x | x | ORG-INTEG-02 | — | Controller display string audited; no actionable (atau minimal inject DocumentAdmin) | manual audit + build | `dotnet build` | ✅ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

*Existing infrastructure covers all phase requirements.*

- No new test file untuk Phase 343 — formal xUnit/Playwright = Phase 344 deliverable (CONTEXT deferred + TEST-01..06).
- Catatan: Phase 344 akan butuh test `OrgLabelService.GetLabel` happy+fallback (TEST-01) + Playwright label baru kelihatan 2+ page (TEST-06). BUKAN scope 343.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Rename label "Bagian"→"Direktorat" muncul di ≥3 page integrasi (SC2) | ORG-INTEG-01 | Butuh mutasi label via UI + visual render cross-page; formal Playwright = Phase 344 | Rename via `/Admin/ManageOrgLevelLabels` → reload CMP filter (AnalyticsDashboard/RecordsTeam) + Worker form (Views/Admin) + CDP assignment → label baru tampil; restore "Bagian" setelah demo |
| Fallback `"Level N"` TIDAK muncul (seed ada) | ORG-INTEG-01 | Konfirmasi seed Phase 340 ter-load di DB lokal | Render page tanpa rename → label tampil "Bagian"/"Unit"/"Sub-unit" (bukan "Level 0/1/2") |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify (`dotnet build` / grep) or manual-only with instructions
- [x] Sampling continuity: no 3 consecutive tasks without automated verify (build per task)
- [x] Wave 0 covers all MISSING references (N/A — none)
- [x] No watch-mode flags
- [x] Feedback latency < 60s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** approved 2026-06-03
