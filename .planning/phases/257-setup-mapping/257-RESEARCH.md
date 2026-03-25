# Phase 257: Setup & Mapping - Research

**Researched:** 2026-03-25
**Domain:** UAT Coach-Coachee Mapping (ASP.NET Core MVC + EF Core + ClosedXML)
**Confidence:** HIGH

## Summary

Phase 257 adalah UAT (User Acceptance Testing) untuk fitur coach-coachee mapping yang sudah fully implemented. Semua 13 controller actions sudah ada di `AdminController.cs`, model sudah lengkap, dan view sudah tersedia. Fokus phase ini adalah: (1) verifikasi code logic via analisa, (2) user testing di browser, (3) fix bug yang ditemukan.

Karena ini bukan greenfield development, research ini fokus pada pemahaman code existing, edge cases yang perlu di-test, dan potential bugs berdasarkan code review.

**Primary recommendation:** Susun test checklist per requirement MAP-01..08, Claude analisa code untuk setiap flow, user verifikasi di browser, fix bug in-place.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Reuse data existing di DB — cek apakah cukup coach/coachee/track, tambah manual kalau kurang. Tidak perlu fresh seed script.
- **D-02:** Fix bug langsung in-place saat ditemukan (Claude analisa -> fix -> commit -> user verifikasi). Pending UAT Phase 235/247 yang relevan dengan mapping ikut di-test ulang.
- **D-03:** Claude analisa code untuk pastikan logic benar, lalu user verifikasi di browser untuk flow kritis. Checklist per requirement MAP-01..08.
- **D-04:** Happy path + key edge cases per requirement (duplikat import, assign tanpa track, deactivate mapping dengan session aktif). Tidak perlu exhaustive semua kombinasi.
- **D-05:** Behavior warning only sudah benar — warning muncul, user confirm, lalu proceed. Bukan hard block.
- **D-06:** Partial commit — row valid tetap di-commit, row error ditampilkan di summary. Bukan all-or-nothing.
- **D-07:** Test cascade hanya sampai ProtonTrackAssignment level. DeliverableProgress cascade di-test di Phase 259.

### Claude's Discretion
- Urutan test scenario per requirement
- Detail edge cases mana yang paling kritis untuk di-test

### Deferred Ideas (OUT OF SCOPE)
None
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MAP-01 | Admin/HC bisa melihat daftar coach-coachee mapping (paginated, searchable) | Action `CoachCoacheeMapping` line 3632 — pagination via PaginationHelper, search by name/NIP, section filter |
| MAP-02 | Admin/HC bisa assign coach ke multiple coachee via UI modal | Action `CoachCoacheeMappingAssign` line 3984 — JSON POST, duplicate check, AssignmentSection/Unit required |
| MAP-03 | Admin/HC bisa import coach-coachee mapping via Excel | Action `ImportCoachCoacheeMapping` line 3789 — per-row processing dengan 4 status (Success/Error/Skip/Reactivated) |
| MAP-04 | Admin/HC bisa download template Excel untuk import mapping | Action `DownloadMappingImportTemplate` line 3754 — 2 kolom (NIP Coach, NIP Coachee) |
| MAP-05 | Assign mapping dengan ProtonTrackId otomatis membuat ProtonTrackAssignment + ProtonDeliverableProgress | Line 4083-4135 — reuse inactive assignment atau create new + AutoCreateProgressForAssignment |
| MAP-06 | Deactivate mapping -> cascade deactivate ProtonTrackAssignment | Action `CoachCoacheeMappingDeactivate` line 4320 — transaction + cascade + DeactivatedAt timestamp |
| MAP-07 | Reactivate mapping -> reuse existing ProtonTrackAssignment | Action `CoachCoacheeMappingReactivate` line 4390 — correlate by DeactivatedAt within 5 seconds |
| MAP-08 | Progression warning D-09 muncul saat assign Tahun 2+ jika Tahun sebelumnya belum selesai | Line 4019-4063 — check prevTrack by TrackType+Urutan, return warning JSON if not confirmed |
</phase_requirements>

## Architecture Patterns

### Existing Code Structure
```
Controllers/
  AdminController.cs          # 13 mapping actions (line ~3632-4470)
Models/
  CoachCoacheeMapping.cs      # Entity: CoachId, CoacheeId, IsActive, AssignmentSection/Unit, IsCompleted
  ImportMappingResult.cs       # Per-row import result (RowNum, Status, Message)
  ProtonModels.cs              # ProtonTrackAssignment, ProtonDeliverableProgress
Views/Admin/
  CoachCoacheeMapping.cshtml   # List + modals (assign, edit, import)
```

### Key Patterns in Code
1. **Grouped pagination**: Mappings grouped by Coach, paginated over coach groups (bukan individual rows)
2. **JSON API for mutations**: Assign/Edit/Deactivate/Reactivate return `Json(new { success, message })`
3. **Transaction wrapping**: Deactivate dan Reactivate menggunakan `BeginTransactionAsync` + commit/rollback
4. **Import = partial commit**: Semua valid rows di-commit dalam satu transaction, error rows hanya di-report
5. **Cascade via DeactivatedAt timestamp**: Reactivate correlate assignments by timestamp within 5-second window

### Anti-Patterns to Watch
- **N+1 in assign loop**: `CoachCoacheeMappingAssign` melakukan per-coachee DB queries di dalam foreach loop (line 4097-4132). Acceptable untuk small batches tapi bisa jadi slow untuk bulk.
- **Import reactivate tanpa track assignment reactivation**: Import flow hanya reactivate mapping (`IsActive = true`) tapi TIDAK reactivate ProtonTrackAssignment. Ini mungkin intentional (import hanya mapping level) tapi perlu di-verify.

## Common Pitfalls

### Pitfall 1: Import Reactivate vs Assign Reactivate Inconsistency
**What goes wrong:** Import Excel reactivate mapping tapi tidak reactivate ProtonTrackAssignment, sedangkan UI reactivate (via button) melakukan cascade reactivation.
**Why it happens:** Import flow intentionally simpler — hanya mapping level, tanpa track assignment logic.
**How to avoid:** Verify bahwa ini expected behavior per D-06/D-07. Jika user expect import juga reactivate track, ini bug.
**Warning signs:** Coachee yang di-reactivate via import tidak punya active track assignment.

### Pitfall 2: Reactivate Timestamp Correlation Window
**What goes wrong:** Reactivate menggunakan 5-second window untuk correlate DeactivatedAt. Jika deactivate dan manual assignment deactivation terjadi dalam 5 detik, wrong assignments bisa ter-reactivate.
**Why it happens:** Timestamp-based correlation bukan exact match.
**How to avoid:** Edge case yang unlikely di production, tapi perlu awareness.

### Pitfall 3: EligibleCoachees Filter
**What goes wrong:** Modal assign hanya menampilkan coachees yang belum punya active mapping (`!activeCoacheeIds.Contains(u.Id)`). Jika data test limited, modal bisa kosong.
**How to avoid:** Pastikan ada user dengan role Coachee yang belum ter-assign sebelum test MAP-02.

### Pitfall 4: Progression Warning Skip untuk Existing Assignment
**What goes wrong:** D-09 warning di-skip jika coachee already has existing assignment untuk track yang sama (line 4035-4038). Ini benar untuk reactivation scenario tapi bisa confusing saat test.
**How to avoid:** Test dengan coachee baru (tanpa existing assignment) untuk trigger warning.

## Test Strategy

### Recommended Test Order (Claude's Discretion)
1. **MAP-01** (List) — baseline, pastikan halaman load tanpa error
2. **MAP-04** (Download template) — quick check, no side effects
3. **MAP-02** (Assign via modal) — core flow, buat test data untuk subsequent tests
4. **MAP-05** (Track assignment creation) — verify side effect dari MAP-02
5. **MAP-08** (Progression warning) — requires Tahun 2+ track, test setelah MAP-02 berhasil
6. **MAP-03** (Import Excel) — test create, skip duplicate, reactivate
7. **MAP-06** (Deactivate) — test cascade ke ProtonTrackAssignment
8. **MAP-07** (Reactivate) — test reuse assignment, harus setelah MAP-06

### Key Edge Cases per Requirement
| Req | Happy Path | Edge Case |
|-----|-----------|-----------|
| MAP-01 | List tampil dengan data | Search NIP, filter section, toggle showAll |
| MAP-02 | Assign 1 coach ke 2 coachees | Assign ke coachee yang sudah punya coach aktif |
| MAP-03 | Import 3 rows valid | Row dengan NIP tidak ada, duplicate active, self-assign |
| MAP-04 | Download file .xlsx | - |
| MAP-05 | Assign dengan TrackId -> ProtonTrackAssignment terbuat | Assign tanpa TrackId (no track side-effect) |
| MAP-06 | Deactivate -> cascade | Deactivate mapping tanpa active track assignments |
| MAP-07 | Reactivate -> reuse | Reactivate setelah long time (DeactivatedAt correlation) |
| MAP-08 | Warning muncul untuk Tahun 2 | Confirm warning -> proceed, coachee dengan completed Tahun 1 (no warning) |

## Data Prerequisites

Per D-01, reuse existing data. Perlu dicek:
1. Minimal 2 user dengan role Coach (active)
2. Minimal 4 user dengan role Coachee (active, belum ter-assign)
3. Minimal 2 ProtonTrack records (Tahun 1 dan Tahun 2 dari TrackType yang sama)
4. Minimal 1 existing active mapping (untuk test search/deactivate)

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Manual UAT (browser-based) |
| Config file | N/A — UAT phase |
| Quick run command | Claude code review + user browser verification |
| Full suite command | Checklist MAP-01..08 all pass |

### Phase Requirements -> Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| MAP-01 | List page loads with data, pagination, search | manual | Browse /Admin/CoachCoacheeMapping | N/A |
| MAP-02 | Assign modal creates mapping | manual | POST /Admin/CoachCoacheeMappingAssign | N/A |
| MAP-03 | Import Excel processes rows correctly | manual | POST /Admin/ImportCoachCoacheeMapping | N/A |
| MAP-04 | Template download works | manual | GET /Admin/DownloadMappingImportTemplate | N/A |
| MAP-05 | Track assignment auto-created | manual | Verify DB after MAP-02 with TrackId | N/A |
| MAP-06 | Deactivate cascades to track assignment | manual | POST /Admin/CoachCoacheeMappingDeactivate | N/A |
| MAP-07 | Reactivate reuses track assignment | manual | POST /Admin/CoachCoacheeMappingReactivate | N/A |
| MAP-08 | Progression warning appears | manual | Assign Tahun 2+ track, observe warning | N/A |

### Sampling Rate
- **Per task:** Claude code review sebelum user test
- **Per wave:** User verifikasi semua 8 requirements di browser
- **Phase gate:** All MAP-01..08 pass

### Wave 0 Gaps
None — ini UAT phase, bukan automated test phase. Semua verifikasi manual via browser.

## Environment Availability

Step 2.6: SKIPPED (no external dependencies — semua code sudah implemented, testing via browser pada running application).

## Sources

### Primary (HIGH confidence)
- `Controllers/AdminController.cs` line 3632-4470 — direct code review of all 13 mapping actions
- `Models/CoachCoacheeMapping.cs` — entity model
- `Models/ImportMappingResult.cs` — import result model

## Metadata

**Confidence breakdown:**
- Code understanding: HIGH - direct source code review
- Test strategy: HIGH - based on actual code paths
- Edge cases: HIGH - identified from code logic
- Data prerequisites: MEDIUM - needs runtime verification

**Research date:** 2026-03-25
**Valid until:** 2026-04-25 (stable — code already implemented)
