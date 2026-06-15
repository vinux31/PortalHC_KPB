# Phase 365: Test-hardening Coach×Coachee — AF-3 xUnit - Discussion Log

> **Audit trail only.** Jangan dipakai sebagai input planning/research/execution.
> Keputusan ada di CONTEXT.md — log ini menyimpan alternatif yang dipertimbangkan.

**Date:** 2026-06-12
**Phase:** 365-test-hardening-coach-coachee-af-3-xunit
**Areas discussed:** Infra & testability, Cakupan skenario, Kedalaman asersi

---

## Area selection

| Option | Description | Selected |
|--------|-------------|----------|
| Infra & testability | Konflik nyata dgn SC roadmap (UserManager deref + transaksi) | ✓ |
| Cakupan skenario | Perilaku mana yang dikunci | ✓ |
| Kedalaman asersi | Seberapa dalam assert end-state | ✓ |

**User's choice:** ketiganya.

---

## Infra & testability

| Option | Description | Selected |
|--------|-------------|----------|
| Extract static core (pola 363) | Core static + wrapper tipis; test core tanpa UserManager | ✓ |
| Zero-change + UserManager-double + real-SQL | Pertahankan endpoint, bangun UserManager double (nol preseden) | |
| InMemory + suppress + fake UM | Paling ringan; unique-index tak enforce | |

**User's choice:** Extract static core (pola 363).
**Notes:** Endpoint deref `_userManager.GetUserAsync` di baris pertama (`:1118`) → null-substitute gagal; repo nol preseden UserManager-double; pola 363 (`ApproveDeliverableCoreAsync`) sudah mapan. Konsekuensi: SC roadmap "zero produksi berubah" dilonggarkan → behavior-preserving + parity-locked.

### Sub-putusan: bentuk core + backend test

| Option | Description | Selected |
|--------|-------------|----------|
| Core murni + real-SQL fixture | Transaksi+audit di wrapper; test via ProtonCompletionFixture; unique-index nyata | ✓ |
| Core murni + InMemory | Cepat, tapi unique-index tak enforce → re-assignability tak teruji nyata | |
| Transaksi di dalam core | Core self-contained; atomicity ikut teruji; kurang murni | |

**User's choice:** Core murni + real-SQL fixture.
**Notes:** Hanya real SQL enforce `IX_CoachCoacheeMappings_CoacheeId_ActiveUnique` → bukti D-03 (index bebas → re-assignable) butuh real SQL. Transaksi + audit log tetap di wrapper.

---

## Cakupan skenario

| Option | Description | Selected |
|--------|-------------|----------|
| Standard (5-6 fact) | Happy + re-assignability + 2 guard + mapping-null + histori-utuh | ✓ |
| Minimal (2-3 fact) | Hanya happy + re-assignability; guard tak terkunci | |
| Max (standard + rollback) | + rollback level-controller (butuh UserManager-double); race tetap backlog | |

**User's choice:** Standard (5-6 fact).
**Notes:** Race AF-6 + e2e re-assign = backlog (exclude). Re-assignability di level index = inti bukti D-03, murah dgn real SQL → in-scope.

---

## Kedalaman asersi

| Option | Description | Selected |
|--------|-------------|----------|
| Full end-state core | Flag + timestamp + cascade + cascadeCount + histori utuh; guard no-mutasi; audit tak di-assert | ✓ |
| Flag-only (lean) | Cuma IsCompleted/IsActive + ok/error | |
| Full + assert audit row | + baris AuditLogs (butuh UserManager-double / pindah audit ke core) | |

**User's choice:** Full end-state core.
**Notes:** Guard pakai cek token kunci error (bukan verbatim → hindari brittle microcopy). Audit log di wrapper → tak di-assert di xUnit.

## Claude's Discretion
- Bentuk signature core final (mapping-null di core vs wrapper).
- Strategi seed fixture (reuse pola `SeedProgressChainAsync` + Tahun-3 chain + ProtonFinalAssessment + CoachCoacheeMapping).
- Smoke-test wrapper opsional.

## Deferred Ideas
- AF-6 race harness, e2e Playwright re-assign, rollback-on-failure level-controller, assert baris audit log.
