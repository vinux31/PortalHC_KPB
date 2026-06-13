---
phase: 372
slug: data-foundation-propagasi-toggle
status: complete
nyquist_compliant: true
wave_0_complete: true
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
| 372-01-T3 | 01 | 1 | SHUF-01 | — | migration backfill: baris omit kolom shuffle → DB DEFAULT 1 (mekanisme backfill baris lama); + round-trip false/true | integration (real-SQL) `ShuffleMigrationTests` | `dotnet test --filter ~ShuffleMigration` | ✅ | ✅ green (2/2) |
| 372-02-T2 | 02 | 2 | SHUF-02 | T-372-04/05 | assessment baru via form → flag persist sesuai centang (ON default / OFF eksplisit / independen), tak kena EF bool-false trap; 8 write-site controller di-cover anchored-insertion + grep≥8 + build | integration (real-SQL) `ShuffleCreatePersistenceTests` (3/3) | `dotnet test --filter ~ShuffleCreate` | ✅ | ✅ green (3/3) |
| 372-02-T2 | 02 | 2 | SHUF-03 | T-372-06 | ubah toggle EditAssessment → semua sibling grup ikut (standard 3-sibling + Pre-Post), persist ke setiap baris | integration (real-SQL) `ShufflePropagationTests` (2/2) | `dotnet test --filter ~ShufflePropagation` | ✅ | ✅ green (2/2) |

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
