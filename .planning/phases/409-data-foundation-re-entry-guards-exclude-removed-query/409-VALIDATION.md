---
phase: 409
slug: data-foundation-re-entry-guards-exclude-removed-query
status: validated
nyquist_compliant: true
wave_0_complete: true
created: 2026-06-21
validated: 2026-06-21
---

# Phase 409 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET 8) |
| **Config file** | `HcPortal.Tests/HcPortal.Tests.csproj` |
| **Quick run command** | `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ParticipantRemoval"` |
| **Full suite command** | `dotnet test HcPortal.Tests` |
| **Estimated runtime** | quick ~17s / full ~2m45s |
| **Suite status (post-validate)** | **571/571 GREEN, 0 failed, 0 skipped** (was 569; +2 fact gap-fill) |

---

## Sampling Rate

- **After every task commit:** Run quick run command (removal guard + exclude tests)
- **After every plan wave:** Run `dotnet build` + full suite
- **Before `/gsd-verify-work`:** `dotnet build` 0 error + full suite green + migration applied DB lokal (sqlcmd verify)
- **Max feedback latency:** ~17 seconds (quick)

---

## Per-Task Verification Map

> Filled by planner per-task dan oleh `/gsd-validate-phase` (Nyquist). Behaviors to validate (Validation Architecture, RESEARCH §):
> - Guard re-entry: `StartExam` blocks `RemovedAt != null` (redirect + message, session NOT marked InProgress)
> - Guard re-entry: `SubmitExam` blocks `RemovedAt != null` before grading (answers discarded)
> - Guard re-entry: `AssessmentHub.JoinBatch` silent-skips `RemovedAt != null`
> - Guard re-entry: `AssessmentHub.SaveTextAnswer`/`SaveMultipleAnswer` (A1) tak memuat sesi `RemovedAt != null` → jawaban tak tersimpan
> - Exclude-removed: admin monitoring/grouping/detail count queries omit `RemovedAt != null`; per-worker `UserAssessmentHistory` UNCHANGED (boundary)
> - Exclude-removed (WR-02): daftar ujian aktif pekerja (`CMPController.Assessment`) omit `RemovedAt != null`; `completedHistory` UNCHANGED (boundary)

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 02-T1 | 02 | 2 | PRMV-03 | T-409-01..09 | test scaffold de-tautologis (guard+exclude+boundary) | integration | `dotnet test --filter ~ParticipantRemoval` | ✅ `ParticipantRemovalGuardTests.cs` | ✅ green |
| 02-T2 | 02 | 2 | PRMV-03 | T-409-01/02/03/08 | removed session cannot Start/Submit/Join; Save* discard | integration | `dotnet test --filter ~ParticipantRemoval` | ✅ | ✅ green |
| 02-T3 | 02 | 2 | PRMV-03 (PLIV-01 foundation) | T-409-09 | exclude removed from 3 monitoring queries; UserAssessmentHistory boundary intact | integration | `dotnet test --filter ~ParticipantRemoval` | ✅ | ✅ green |
| 01-T2 | 01 | 1 | PRMV-03 (foundation) | T-409-04/05/06/07 | migration AddParticipantRemovalColumns applies clean (3 cols nullable, chain intact) | integration (MigrateAsync) + manual sqlcmd | `dotnet test --filter "Category=Integration"` | ✅ fixture `MigrateAsync` | ✅ green (chain validated via fixture) |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Behavior Coverage Matrix (post-validate)

| Behavior | Threat | Test (fact) | Drives | De-tautology |
|----------|--------|-------------|--------|--------------|
| StartExam blocks removed (no mark-InProgress) | T-409-01 | `StartExam_Blocks_RemovedSession` (4) | REAL seam `CMPController.IsParticipantRemoved` atas entitas dari SQL nyata | helper produksi ASLI |
| SubmitExam blocks removed (grading skip, Score unchanged) | T-409-02 | `SubmitExam_Blocks_RemovedSession` (5) | REAL seam + observasi Score DB tak berubah | helper produksi ASLI |
| JoinBatch silent-skips removed | T-409-03 | `JoinBatch_Predicate_Rejects_RemovedSession` (6) | predikat `AnyAsync` EKSAK vs SQL nyata | predicate-replay (infra limit) |
| **Hub Save* tak load removed (jawaban tak tersimpan)** | **T-409-08** | **`SaveAnswer_Hub_DoesNotLoad_RemovedSession` (7) — GAP-1 FILLED** | predikat `FirstOrDefaultAsync` EKSAK (by `s.Id`) vs SQL nyata; removed→null, aktif→non-null | predicate-replay (infra limit) |
| Tab Assessment grouping excludes removed | T-409-09 | `ManageAssessmentTab_Excludes_RemovedSession` (1) | REAL action `AssessmentAdminController.ManageAssessmentTab_Assessment` | action produksi ASLI |
| MonitoringDetail counts exclude removed | T-409-09 | `MonitoringDetail_Counts_ExcludeRemoved` (2) | REAL action `AssessmentMonitoringDetail` | action produksi ASLI |
| UserAssessmentHistory boundary (removed tetap tampil) | T-409-09 | `UserAssessmentHistory_StillShows_RemovedSession` (3) | REAL action `UserAssessmentHistory` | action produksi ASLI |
| **Daftar ujian aktif pekerja excludes removed + completedHistory boundary** | **WR-02 / PLIV-01** | **`AssessmentActiveList_ExcludesRemoved_HistoryStillShows` (8) — GAP-2 FILLED** | dua bentuk query EKSAK (`Assessment` :208-218 active + :328 history) vs SQL nyata | predicate-replay (infra limit) |
| SaveAnswer (MC controller) guard | WR-01 | (lihat Manual-Only) — COVERED at helper `IsParticipantRemoved` (facts 4/5); wiring → manual/413 | seam shared, wiring tak ter-otomasi | helper produksi ASLI |

---

## Wave 0 Requirements

- [x] Test file(s) for PRMV-03 guard block + exclude-removed query (extend existing real-controller/SQLEXPRESS pattern, NON-tautological — backlog 999.12)
- [x] Shared fixture: removed-session seed (`RemovedAt` set) + active-session baseline (`ParticipantRemovalGuardFixture` + `SeedUserAsync`/`SeedSessionAsync`)
- [x] GAP-1 (T-409-08 Hub Save* load-guard) covered — fact (7)
- [x] GAP-2 (WR-02 active-list exclude + history boundary) covered — fact (8)

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Migration applies to local DB; 3 columns present, existing rows NULL | PRMV-03 (foundation) | DDL apply + sqlcmd inspection | `dotnet ef database update` (ASPNETCORE_ENVIRONMENT=Development) + `sqlcmd -C -I` verify columns nullable, all NULL (DONE Plan 01: 60 rows, RemovedAt NULL) |
| **SaveAnswer (MC controller) WIRING** — action `CMPController.SaveAnswer` memanggil `IsParticipantRemoved` lalu menolak | WR-01 / PRMV-03 (A1) | **HARD INFRA LIMIT**: `SaveAnswer` deref `_userManager`/impersonation/DI scope; proyek tak punya WebApplicationFactory. **Seam `IsParticipantRemoved` SUDAH ter-otomasi** (facts 4/5 — logika deteksi sama); hanya WIRING (action→seam) yang tak ter-otomasi. Identik dengan accepted gap StartExam/SubmitExam wiring (review IN-02). | E2E live Phase 413: soft-remove peserta mid-exam (set `RemovedAt` via 411) → ubah jawaban MC → assert response `{ success:false }` + tak ada `progressUpdate` broadcast. ATAU UAT browser. |
| StartExam/SubmitExam guard WIRING (action→seam→redirect+TempData) | PRMV-03 | review IN-02: action deref dependency berat; helper `IsParticipantRemoved` ter-otomasi (facts 4/5), blok `if (IsParticipantRemoved(...)) redirect` = wiring | E2E Phase 413 / UAT: sesi removed → StartExam redirect "Anda telah dikeluarkan dari ujian ini." |

*Full e2e (Playwright live Monitoring + force-kick) deferred to Phase 413.*

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify atau Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references (GAP-1 + GAP-2 filled)
- [x] No watch-mode flags
- [x] Feedback latency < 30s (quick ~17s)
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** APPROVED (Nyquist) 2026-06-21

---

## Validation Audit

### 2026-06-21 — /gsd-validate-phase (Nyquist auditor, State A gap-fill)

**Verdict:** GAPS FILLED — nyquist_compliant: true, wave_0_complete: true.

**State A:** 6 fact pre-existing GREEN. 2 gap MISSING + 1 gap COVERED-at-helper diidentifikasi review (WR-01/WR-02) + threat T-409-08.

**Gap closure:**

- **GAP-1 (T-409-08, MISSING → FILLED):** Hub `SaveTextAnswer`/`SaveMultipleAnswer` session-load guard. Fact baru `SaveAnswer_Hub_DoesNotLoad_RemovedSession` (fact 7) mereplay predikat produksi EKSAK `FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.Status == "InProgress" && s.RemovedAt == null)` terhadap SQL nyata (SQLEXPRESS disposable). Seed: sesi InProgress removed + sesi InProgress aktif (user sama). Assert: removed→null (tak ter-load → Save* `if (session==null) return` → jawaban TAK tersimpan), aktif→non-null. Distinct dari JoinBatch (by `s.Id`, FirstOrDefaultAsync vs AnyAsync). Pola predicate-replay = pola diterima proyek (facts 3/6, infra limit Hub butuh Context/scopeFactory).

- **GAP-2 (WR-02, MISSING → FILLED):** Daftar ujian aktif pekerja exclude removed + boundary completedHistory. Fact baru `AssessmentActiveList_ExcludesRemoved_HistoryStillShows` (fact 8) mereplay DUA bentuk query produksi EKSAK (`CMPController.Assessment` :208-218 active-list dgn `.Where(a => a.RemovedAt == null)` + :328 completedHistory TANPA filter RemovedAt) terhadap SQL nyata. Seed: 1 Open aktif + 1 Open removed + 1 Completed removed. Assert: active-list → hanya 1 (removed lenyap); history → 1 Completed removed TETAP tampil (boundary, anti over-exclude, sertifikat utuh).

- **GAP-3 (WR-01, COVERED-at-helper):** `CMPController.SaveAnswer` (MC) guard memakai seam `IsParticipantRemoved` yang SAMA — sudah ter-otomasi facts 4/5. Wiring action→seam tak ter-otomasi (HARD INFRA LIMIT: deref `_userManager`/DI, no WebApplicationFactory), identik accepted gap StartExam/SubmitExam (review IN-02). TIDAK ditambah test helper redundan. Didokumentasikan COVERED (helper) + wiring → Manual-Only/Phase 413. Tak buat test tautologis.

**De-tautology (lesson 999.12):** Gap fill mengutamakan drive seam/action produksi ASLI; di mana infra memaksa (Hub/Assessment action deref `_userManager`/DI scope, proyek tak punya WebApplicationFactory), dipakai predicate-replay EKSAK vs SQL nyata — pola accepted proyek (facts 3/6). TIDAK ada test yang menulis ulang logika tiruan lalu meng-assert dirinya sendiri; predikat yang di-replay = string predikat produksi verbatim, dijalankan terhadap schema riil.

**Files modified:** `HcPortal.Tests/ParticipantRemovalGuardTests.cs` (+2 fact, facts 7 & 8 di kelas `ParticipantRemovalGuardTests` [Trait Category=Integration]).

**Verification:**
- `dotnet test HcPortal.Tests --filter "FullyQualifiedName~ParticipantRemoval"` → **8/8 GREEN** (6 existing + 2 new).
- `dotnet test HcPortal.Tests` (full) → **571/571 GREEN, 0 failed, 0 skipped** (was 569, +2). NO regression.

**Residual (justified manual-only):** SaveAnswer/StartExam/SubmitExam guard WIRING (action→seam→redirect/reject) — seam logika ter-otomasi, wiring deferred ke E2E Phase 413 / UAT (HARD INFRA LIMIT, accepted pattern). IN-03 (warning "ada peserta sedang mengerjakan" count removed) = non-correctness nudge, deferred Phase 411 per review. IN-01 (JoinBatch predicate-copy) = tech-debt terdokumentasi (seam refactor optional). A2 export/impact exclude = OUT scope 409 (defer 412/413).

**Verdict basis nyquist_compliant=true:** Semua behavior PRMV-03/PLIV-01 read-path tercakup — automated (8 fact: guard StartExam/SubmitExam/JoinBatch/Save*, exclude Tab/Detail/active-list, boundary history) atau justified manual-only (wiring → 413, blocked by HARD INFRA LIMIT yang sama untuk seluruh CMP/Hub action di fixture ini). Tak ada gap correctness/security yang tak tercakup.
