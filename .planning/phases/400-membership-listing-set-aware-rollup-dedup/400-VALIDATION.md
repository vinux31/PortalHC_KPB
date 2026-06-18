---
phase: 400
slug: membership-listing-set-aware-rollup-dedup
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-18
---

# Phase 400 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: 400-RESEARCH.md §Validation Architecture (HIGH confidence).

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + EF Core InMemory 8.0.0 (+ SqlServer 8.0.0 untuk integration/Phase 404) |
| **Config file** | none — auto-discovery xUnit (`HcPortal.Tests.csproj`) |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~WorkerDataServiceSearchTests"` |
| **Full suite command** | `dotnet test HcPortal.Tests` (baseline 366/366 hijau per MEMORY 399) |
| **Estimated runtime** | quick ~10s · full ~60–90s |

---

## Sampling Rate

- **After every task commit:** `dotnet build` (0 error — tangkap Pitfall #1 nav-prop) + `dotnet test --filter "FullyQualifiedName~WorkerDataServiceSearchTests"`
- **After every plan wave:** `dotnet test HcPortal.Tests` (full suite, ≥366 + test baru hijau)
- **Before `/gsd-verify-work`:** Full suite hijau + `dotnet run` (localhost:5277) cek fixture {X,Y} (filter X & Y muncul, rollup dedup) + cek DB lokal
- **Max feedback latency:** ~10s (quick)

> SQL-real (EXISTS translation + pagination count di SQL Server) + UAT browser = **DEFER Phase 404** (extend `UserUnitsBackfillFixture`/SQLEXPRESS).

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 400-W0 | 01 | 0 | MU-06 | — | N/A | unit (InMemory) | `dotnet test --filter "Name~MultiUnitWorker_AppearsInBothUnitFilters"` | ❌ W0 | ⬜ pending |
| 400-W0 | 01 | 0 | MU-06 | — | N/A | unit (InMemory) | `dotnet test --filter "Name~MultiUnitWorker_SingleRow_NoFilter"` (dedup by-construction) | ❌ W0 | ⬜ pending |
| 400-W0 | 01 | 0 | MU-06 | — | D-03 active-only | unit (InMemory) | `dotnet test --filter "Name~InactiveUnit_ExcludedFromFilter"` | ❌ W0 | ⬜ pending |
| 400-W0 | 01 | 0 | MU-06 | — | D-02 contextual | unit (InMemory) | `dotnet test --filter "Name~UnfilteredColumn_AllActiveUnits_PrimaryFirst"` | ❌ W0 | ⬜ pending |
| 400-W0 | 01 | 0 | MU-06 | — | D-02 contextual | unit (InMemory) | `dotnet test --filter "Name~FilteredColumn_ShowsUnitFilter"` | ❌ W0 | ⬜ pending |
| 400-W0 | 01 | 0 | MU-06 | — | D-05 fallback | unit (InMemory) | `dotnet test --filter "Name~ZeroUnit_Fallback"` | ❌ W0 | ⬜ pending |
| 400-W0 | 01 | 0 | MU-06 (no-drift D1=b) | V4 access ctrl (pasif) | scope unchanged | unit regresi (existing) | `dotnet test --filter "Name~Scope_Null_NoFilter_BackwardCompat"` | ✅ (WorkerDataServiceSearchTests.cs:86) | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/WorkerDataServiceSearchTests.cs` — tambah ~7 test MU-06 (reuse `MakeService`/`User` helper existing; seed `ctx.UserUnits.AddRange(...)`): set-aware both-units, dedup single-row, IsActive D-03, contextual filtered/unfiltered D-02, fallback D-05, no-drift regresi.
- [ ] `HcPortal.Tests/FakeWorkerDataService.cs` — TIDAK perlu diubah (fake return empty, dipakai GradingService test saja). [VERIFIED:24]
- [ ] Framework install — TIDAK perlu (InMemory + xUnit sudah ada).

*Predikat ditulis sebagai correlated subquery `_context.UserUnits.Any(uu => uu.UserId == u.Id && uu.Unit == unitFilter && uu.IsActive)` — nav prop `ApplicationUser.UserUnits` TIDAK ADA (`.WithMany()` no-arg). Pitfall #1.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Kolom Unit tampil "X" saat filter unit-X, "X, Y" tanpa filter | MU-06 (D-02) | Razor render runtime — grep+build tak cukup (lesson Phase 354) | `dotnet run` → /CMP/Records Team View → filter unit-X (kolom "X"), reset (kolom "X, Y") |
| SQL-real: EXISTS translate + pagination count akurat di SQL Server | MU-06 (SC#4) | EF-InMemory ≠ SqlServer semantics (paritas) | **DEFER Phase 404** (SQLEXPRESS fixture {X,Y}) |
| Fixture {X,Y} muncul di filter X & Y + rollup Bagian dedup | MU-06 (SC#1/2) | Cek DB lokal + browser | `dotnet run` localhost:5277 + cek DB lokal (Seed Workflow: snapshot→seed→restore, `docs/SEED_JOURNAL.md`) |

---

## Observable Facts per Success Criterion (Nyquist)

- **SC#1 (set-aware):** `GetWorkersInSection("A", unitFilter:"X").Single()` && `...unitFilter:"Y").Single()` untuk pekerja {X,Y} → kedua hijau = listing set-aware terbukti.
- **SC#2 (dedup):** `GetWorkersInSection("A").Count` untuk pekerja {X,Y} == 1 (bukan 2) → denominator/completion% tak ganda. **JANGAN tambah `.Distinct()`** (anti-pattern menyamarkan fan-out — `.Any()` subquery = no JOIN).
- **SC#3 (no-drift D1=b):** existing `Scope_Null_NoFilter_BackwardCompat` tetap hijau + diff menunjukkan path analytics (`CMPController:2581/:2589`) & Team View call (`:543`, no unitFilter) tak berubah → 0 drift by-construction.
- **SC#4 (build/run/DB):** `dotnet build` 0 error + fixture {X,Y} tampil di filter X & Y + rollup Bagian dedup terverifikasi DB lokal.

> **Scope note (RESEARCH temuan #2):** Set-aware menyentuh **4 consumer** `GetWorkersInSection` (CONTEXT menyebut 3): consumer ke-4 = `AssessmentAdminController.cs:278` (ManageAssessmentTab / Kelola Data Section C) yang juga meneruskan `unit` — benign + otomatis set-aware (tak butuh kode tambahan) tapi WAJIB masuk verification scope.

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (~7 test ke WorkerDataServiceSearchTests.cs)
- [ ] No watch-mode flags
- [ ] Feedback latency < 90s (full) / 10s (quick)
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
