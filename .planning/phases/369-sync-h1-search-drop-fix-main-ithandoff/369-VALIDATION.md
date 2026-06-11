---
phase: 369
slug: sync-h1-search-drop-fix-main-ithandoff
status: verified
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-11
---

# Phase 369 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + EFCore.InMemory 8.0.0 (dotnet 8.0.418) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | quick ~10s · full ~90s |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"`
- **After every plan wave:** Run `dotnet test` (full)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 369-01-01 | 01 | 1 | URG-01 | T-369-02 | search tidak silently ignored (no data over-exposure beyond authorized roles) | unit (dibawa cherry-pick) | `dotnet test --filter "FullyQualifiedName~Scope_Null_WithSearch_FiltersByName_H1"` | ✅ (mendarat via cherry-pick, line 98) | ✅ green |
| 369-01-02 | 01 | 1 | URG-01 | — | regresi nol pada scope-gating REC-06 | full suite | `dotnet test` | ✅ | ✅ green |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

Existing infrastructure covers all phase requirements — test H1 dibawa langsung oleh cherry-pick `14e7adc5` (tidak ada stub baru).

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Tab Input Records search nama memfilter list live | URG-01 (SC3) | Perilaku runtime HTMX + caller `AssessmentAdminController.cs:280` tanpa searchScope — butuh browser | `Authentication__UseActiveDirectory=false dotnet run` → login admin@5277 → `/Admin/ManageAssessment` Tab Input Records → isi filter (section bila perlu) → search nama → list TERFILTER, bukan full roster. **DONE 2026-06-11**: UAT live Playwright @5277, Bagian GAST baseline 7 row → search "Rino" 1 row (lihat 369-01-SUMMARY SC#3) |

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency < 90s (quick ~2s · full ~42s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** verified 2026-06-11

---

## Validation Audit 2026-06-11

| Metric | Count |
|--------|-------|
| Gaps found | 0 |
| Resolved | 0 |
| Escalated | 0 |

Audit re-run live: `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` → 11/11 Passed (incl. `Scope_Null_WithSearch_FiltersByName_H1` line 98); full suite `dotnet test` → 229/229 Failed: 0. Manual-only UAT sudah dieksekusi saat fase (Playwright 7→1 row, 369-01-SUMMARY SC#3). URG-01 = COVERED. Zero gap — tidak perlu spawn gsd-nyquist-auditor.
