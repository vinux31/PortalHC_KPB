---
phase: 391
slug: penambahan-peserta-fleksibel-saat-ujian-berjalan
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-17
---

# Phase 391 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Source: `391-RESEARCH.md` § Validation Architecture.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 (`HcPortal.Tests`) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` (net8.0) |
| **Quick run command** | `dotnet test --filter "Category!=Integration"` (fast suite — Integration excluded) |
| **Full suite command** | `dotnet test` (includes Integration real-SQL; needs SQLEXPRESS up) |
| **Integration-only** | `dotnet test --filter "Category=Integration"` |
| **Estimated runtime** | fast suite ~30-60s · full (with Integration) ~2-3 min |

---

## Sampling Rate

- **After every task commit:** `dotnet build` 0 error + `dotnet test --filter "Category!=Integration"` (fast suite green).
- **After every plan wave:** `dotnet test` full (incl. Integration real-SQL) — SQLEXPRESS must be up; disposable `HcPortalDB_Test_{guid}` (never touches `HcPortalDB_Dev`).
- **Before `/gsd-verify-work`:** Full suite green + (optional) browser verify notice Info di ManageAssessment.
- **Max feedback latency:** ~60s (fast suite).

---

## Per-Task Verification Map

> Task IDs filled during planning/execution. Mapping by requirement (from RESEARCH § Phase Requirements → Test Map):

| Requirement | Behavior | Test Type | Automated Command | File Exists | Status |
|-------------|----------|-----------|-------------------|-------------|--------|
| PART-01 | Tambah saat ada `InProgress` → sesi baru tercipta | integration | `dotnet test --filter "Category=Integration"` | ❌ Wave 0 (file baru) | ⬜ pending |
| PART-01 | Sesi baru ber-status siap-mulai (Open/Upcoming, BUKAN InProgress) | integration | idem | ❌ Wave 0 | ⬜ pending |
| PART-02 | Penambahan tak terblokir saat sebagian `Completed` + window terbuka | integration | idem | ❌ Wave 0 | ⬜ pending |
| PART-03 | Notice = `Info` (bukan `Warning`) — informatif | manual / opsional unit-assert TempData | browser localhost:5277 ATAU assert TempData key | ❌ Wave 0 (opsional) | ⬜ pending |
| PART-04(c) | Sesi `InProgress` existing Status/Schedule/Duration UNCHANGED | integration | `dotnet test --filter "Category=Integration"` | ❌ Wave 0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `HcPortal.Tests/FlexibleParticipantAddTests.cs` — disposable fixture + ≥4 facts (a/b/c/d) mengunci PART-01/02/04. Pola: copy `PostLisensorPolishFixture` (`IAsyncLifetime` MigrateAsync→EnsureDeletedAsync, `[Trait("Category","Integration")]`, `HcPortalDB_Test_{guid}` @ `localhost\SQLEXPRESS`).
- [ ] Helper `DeriveReadyStatus(schedule, window)` (D-01) dapat diuji — pola project = **replikasi byte-identik di test** (bukan expose via InternalsVisibleTo), atau panggil lewat skenario integration end-to-end EditAssessment.
- [ ] (Opsional) `tests/e2e/flexible-add-notice-391.spec.ts` — assert alert Info muncul di ManageAssessment setelah tambah peserta saat ada `InProgress`. Default: manual browser cukup (notice = TempData statis via `_Layout`, bukan render dinamis kompleks — beda dari kasus aria Phase 354).

*Tidak ada framework install — xUnit + SqlServer provider sudah ada di csproj.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Notice `Info` tampil informatif di ManageAssessment | PART-03 | Render TempData via `_Layout` (alert biru) — visual; opsional di-cover unit-assert TempData key | localhost:5277 → tambah peserta ke assessment dgn ≥1 peserta InProgress → cek alert biru "Info" (bukan kuning "Warning") + teks informatif |

---

## Validation Sign-Off

- [ ] All tasks have automated verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references (FlexibleParticipantAddTests.cs)
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s (fast suite)
- [ ] `nyquist_compliant: true` set in frontmatter (after Wave 0)

**Approval:** pending
