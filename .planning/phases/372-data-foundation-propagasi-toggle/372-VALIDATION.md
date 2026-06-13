---
phase: 372
slug: data-foundation-propagasi-toggle
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-13
---

# Phase 372 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8, HcPortal.Tests) |
| **Config file** | none — project-based (`HcPortal.Tests.csproj`) |
| **Quick run command** | `dotnet test --filter "FullyQualifiedName~Shuffle"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~30–90 seconds (real-SQL migration tests slower) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "FullyQualifiedName~Shuffle"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** ~90 seconds

---

## Per-Task Verification Map

*Filled by planner — each task that adds testable behavior maps to a real-SQL or unit test. Validation Architecture source: `372-RESEARCH.md`.*

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| {planner fills} | | | SHUF-01 | — | migration backfill: baris lama kedua flag `true` | integration (real-SQL) | `dotnet test --filter ~Shuffle` | ❌ W0 | ⬜ pending |
| {planner fills} | | | SHUF-02 | — | assessment baru via form → flag tersimpan sesuai centang (default ON), tak kena EF bool-false trap; semua write-site `new AssessmentSession` set eksplisit | integration (real-SQL) | `dotnet test --filter ~Shuffle` | ❌ W0 | ⬜ pending |
| {planner fills} | | | SHUF-03 | — | ubah toggle EditAssessment → semua sibling grup ikut (foreach propagate) | integration (real-SQL) | `dotnet test --filter ~Shuffle` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] Test file(s) untuk shuffle data-foundation — pola gold-standard real-SQL fixture (`ProtonCompletionFixture` / `OrgLabelMigrationIntegrationTests`), disposable `HcPortalDB_Test_<guid>` + `MigrateAsync` (InMemory bypass migration DDL — TIDAK valid untuk SHUF-01 default backfill).
- [ ] Stubs untuk SHUF-01 (migration default), SHUF-02 (form persist + EF bool trap semua write-site), SHUF-03 (sibling propagation).

*Planner finalizes exact file paths from RESEARCH.md Validation Architecture (3 file test Wave 0 teridentifikasi).*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Toggle wizard Langkah 3 + summary Langkah 4 tampil benar (checkbox default checked, status ON/OFF di konfirmasi) | SHUF-02 | Razor UI runtime — grep/build tak cukup (lesson Phase 354) | UAT @5277: buat assessment, cek 2 toggle ON default di Langkah 3, status muncul di Langkah 4. Detail UAT penuh di Phase 375 (Playwright). |

*Catatan: UAT Playwright lengkap = scope Phase 375. Phase 372 manual smoke saja.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
