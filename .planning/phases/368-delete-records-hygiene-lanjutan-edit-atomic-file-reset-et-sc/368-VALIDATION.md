---
phase: 368
slug: delete-records-hygiene-lanjutan-edit-atomic-file-reset-et-sc
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-06-13
---

# Phase 368 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.
> Sumber: 368-RESEARCH.md §"Validation Architecture". Per-task map difinalkan oleh planner/executor saat PLAN.md dibuat.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (`HcPortal.Tests/`) — sudah ada, no install |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "Category!=Integration"` (SQL-less, ~1s) |
| **Full suite command** | `dotnet test HcPortal.Tests` (termasuk integration real-SQL @localhost\SQLEXPRESS) |
| **Baseline saat ini** | post-367: quick 209/209, full 290/290 |
| **Estimated runtime** | quick ~1-2s; real-SQL integration tambah disposable-DB per fixture |

---

## Sampling Rate

- **After every task commit:** `dotnet test HcPortal.Tests --filter "Category!=Integration"` untuk area tersentuh
- **After every plan wave:** `dotnet test HcPortal.Tests` (full suite, real-SQL)
- **Before `/gsd-verify-work`:** Full suite green + Seed Workflow (snapshot→seed→restore+journal) untuk test #23 cleanup
- **Max feedback latency:** filtered runs < ~60s

---

## Per-Task Verification Map

> Diisi oleh planner/executor. Acuan: 368-RESEARCH.md §"Validation Architecture" memetakan #21-27 ke test type. Minimal spec §3.4: [Fact] replace-file atomic (#21, pola Phase 355 `Replace_NewFileWins`), retake pasca-reset hasilkan ET scores baru (#22), import ter-audit-log (#24).

| Task ID | Plan | Wave | Requirement (#temuan) | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|------------------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | — | — | #21-27 (diisi planner) | — | — | unit / integration real-SQL / [Fact] file / build | `dotnet test HcPortal.Tests` | ⬜ | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

> Reuse fixture Phase 367 (terbukti). Koreksi RESEARCH: `SeedRenewalChainAsync` sebagai metode bernama TIDAK ADA — seeding inline via `NewSession(renewsSession:...)` di `RecordCascadeFixture`.

- [ ] Reuse `RecordCascadeFixture` + helper (367 Plan 02) untuk integration test #21/#22/#23/#26 — disposable real-SQL @localhost\SQLEXPRESS, `[Trait("Category","Integration")]`.
- [ ] Helper temp-webroot (`MakeTempWebRoot`/`WriteFakeImage`, 367) untuk [Fact] atomic file #21 (pola Phase 355 `Replace_NewFileWins_DeletesOldFileOnDisk`).
- [ ] Insert orphan AttemptHistory di test mudah — `AssessmentAttemptHistory.SessionId` plain int TANPA FK ke session (no constraint), seed bebas (#23).

> Framework xUnit sudah ada — no install. `wave_0_complete: true` di-set saat fixture re-use + test hijau di eksekusi.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| #23 endpoint cleanup orphan: preview-count tampil → execute → DB bersih + idempotent (re-run=0) | #23 | End-to-end + DB cross-check butuh browser + sqlcmd; Seed Workflow wajib | UAT @5277: seed orphan AttemptHistory → buka endpoint → preview hitung → execute → `SELECT` bersih → re-run preview=0 |
| #27 rename label "Bulk Import Nilai (Excel)" tampil di UI | #27 | Render label = verifikasi visual | UAT: buka BulkBackfill view → label match |

*Detail Manual-Only difinalkan planner (Plan UI/UAT).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 60s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending (draft — per-task map diisi saat planning/eksekusi; `nyquist_compliant` & `wave_0_complete` di-set true saat test hijau)
