---
phase: 409-data-foundation-re-entry-guards-exclude-removed-query
plan: 01
subsystem: database
tags: [ef-core, migration, soft-delete, assessment-session, sql-server, nullable-additive]

# Dependency graph
requires:
  - phase: (none)
    provides: AssessmentSession entity + ApplicationDbContext Fluent block + EF migration chain existing
provides:
  - "3 kolom soft-remove di AssessmentSession: RemovedAt (DateTime?), RemovedBy (string?), RemovalReason (string?)"
  - "Fluent HasMaxLength(500) untuk RemovalReason (D-03)"
  - "Migration AddParticipantRemovalColumns (additif nullable) ter-apply di HcPortalDB_Dev lokal"
  - "Invarian tunggal: soft-removed <=> RemovedAt != null; aktif <=> RemovedAt == null"
  - "Local tool manifest .config/dotnet-tools.json pin dotnet-ef 8.0.0 (mitigasi Pitfall 5)"
affects: [409-02 (guard re-entry + exclude-removed query), 410 (idempotency cek sesi aktif RemovedAt==null), 411 (tulis/clear kolom removal), 412 (panel Peserta Dikeluarkan baca removed)]

# Tech tracking
tech-stack:
  added: ["local tool manifest dotnet-ef 8.0.0 (.config/dotnet-tools.json)"]
  patterns: ["Additive nullable migration tanpa defaultValue destruktif (cermin Phase 372 ShuffleToggles, tapi nullable:true)", "Fluent length-config di Entity<AssessmentSession> block (cermin TahunKe HasMaxLength)", "Soft-remove invariant = RemovedAt != null (BUKAN via Status)"]

key-files:
  created:
    - "Migrations/20260621011101_AddParticipantRemovalColumns.cs"
    - "Migrations/20260621011101_AddParticipantRemovalColumns.Designer.cs"
    - ".config/dotnet-tools.json"
  modified:
    - "Models/AssessmentSession.cs"
    - "Data/ApplicationDbContext.cs"
    - "Migrations/ApplicationDbContextModelSnapshot.cs"

key-decisions:
  - "Fluent API (bukan Data Annotation) untuk HasMaxLength(500) RemovalReason — konvensi dominan block Entity<AssessmentSession>"
  - "RemovedBy plain string? (cermin CreatedBy) tanpa Fluent → nvarchar(max), acceptable D-03"
  - "Pin dotnet-ef 8.0.0 via LOCAL tool manifest (bukan downgrade global) — global 10.0.3 menolak downgrade; manifest self-contained + deterministik untuk migrasi future repo"

patterns-established:
  - "Additive nullable migration: 3 kolom nullable:true tanpa defaultValue → baris existing otomatis NULL (non-destruktif)"
  - "EF tool pinning per-repo via .config/dotnet-tools.json (rollForward:false) — cegah snapshot ter-stamp ProductVersion 10.x"

requirements-completed: [PRMV-03]

# Metrics
duration: 6min
completed: 2026-06-21
---

# Phase 409 Plan 01: Data Foundation (Soft-Remove Schema) Summary

**3 kolom soft-remove (RemovedAt/RemovedBy/RemovalReason) additif nullable di AssessmentSession via migration AddParticipantRemovalColumns (EF 8.0.0), ter-apply ke HcPortalDB_Dev lokal — menetapkan invarian tunggal soft-removed ⇔ RemovedAt != null.**

## Performance

- **Duration:** ~6 min
- **Started:** 2026-06-21T01:07:45Z
- **Completed:** 2026-06-21T01:13:11Z
- **Tasks:** 2 (Task 1 model+Fluent, Task 2 [BLOCKING] migration scaffold+apply+verify)
- **Files modified:** 6 (2 modified kode, 3 created migration, 1 created manifest)

## Accomplishments
- 3 properti soft-remove ditambah ke `AssessmentSession` (nullable, NO `[MaxLength]` annotation; XML doc mendeklarasikan invarian RemovedAt UTC null=aktif/non-null=removed).
- Fluent `HasMaxLength(500).IsRequired(false)` untuk `RemovalReason` di block `Entity<AssessmentSession>` (D-03), tanpa menyentuh filtered unique index `IX_AssessmentSessions_NomorSertifikat_Unique`.
- Migration `AddParticipantRemovalColumns` di-scaffold via `dotnet ef` (BUKAN hand-roll) — 3 `AddColumn` nullable:true tanpa defaultValue, `Down()` simetris 3 `DropColumn`, snapshot ProductVersion 8.0.0.
- Migration ter-apply ke `HcPortalDB_Dev` lokal; sqlcmd confirm 3 kolom hadir + semua nullable + RemovalReason len=500 + 60 baris existing `RemovedAt` NULL (additif non-destruktif).
- Tool manifest lokal `.config/dotnet-tools.json` pin `dotnet-ef` 8.0.0 (mitigasi permanen Pitfall 5 / T-409-05 untuk migrasi future repo).

## Task Commits

Each task was committed atomically:

1. **Task 1: Tambah 3 properti soft-remove ke model + Fluent config** - `3806e7b9` (feat)
2. **Task 2 [BLOCKING]: Scaffold migration AddParticipantRemovalColumns + apply DB lokal + verify sqlcmd** - `01cd7dd0` (feat)

**Plan metadata:** (final docs commit — STATE.md + ROADMAP.md + SUMMARY.md)

## Files Created/Modified
- `Models/AssessmentSession.cs` - +3 properti soft-remove (RemovedAt/RemovedBy/RemovalReason) di region audit-field dekat CreatedBy
- `Data/ApplicationDbContext.cs` - +1 baris Fluent `RemovalReason.HasMaxLength(500).IsRequired(false)` di block Entity<AssessmentSession>
- `Migrations/20260621011101_AddParticipantRemovalColumns.cs` - NEW; 3 AddColumn nullable + 3 DropColumn simetris
- `Migrations/20260621011101_AddParticipantRemovalColumns.Designer.cs` - NEW; designer (ProductVersion 8.0.0)
- `Migrations/ApplicationDbContextModelSnapshot.cs` - +10 baris (3 properti baru, ProductVersion 8.0.0)
- `.config/dotnet-tools.json` - NEW; local tool manifest pin dotnet-ef 8.0.0 (rollForward:false)

## Decisions Made
- **Fluent vs Data Annotation:** Fluent `HasMaxLength(500)` dipilih untuk `RemovalReason` (konvensi dominan block `Entity<AssessmentSession>`, cermin `TahunKe`). `RemovedBy` dibiarkan plain `string?` → `nvarchar(max)`, identik `CreatedBy` (acceptable D-03).
- **EF tool pinning:** Global `dotnet ef` = 10.0.3 (vs project EF 8.0.0). `dotnet tool update --version 8.0.0 --global` MENOLAK downgrade ("8.0.0 lower than existing 10.0.3"). Solusi: local tool manifest `.config/dotnet-tools.json` (install dotnet-ef 8.0.0) — local tool diutamakan atas global saat invoke dari direktori repo. Self-contained, deterministik, dan mencegah snapshot ter-stamp 10.x untuk SEMUA migrasi future repo (Phase 411 dst.). Verified: `dotnet ef --version` = 8.0.0 dari direktori repo; snapshot + Designer ber-ProductVersion 8.0.0.

## Deviations from Plan

[Rule 3 - Auto-fix blocking issue] **Tambah local tool manifest `.config/dotnet-tools.json`**
- **Found during:** Task 2 (scaffold migration — BLOCKING gate)
- **Issue:** Global `dotnet ef` = 10.0.3 (mismatch vs project EF 8.0.0). Plan/CRITICAL_ENVIRONMENT_NOTE preferred-order opsi 1 (`dotnet tool update --version 8.0.0 --global`) GAGAL — tool update menolak downgrade dari 10.0.3 ke 8.0.0. Tanpa pin 8.0.0, scaffolder akan stamp snapshot ProductVersion 10.x → break runtime EF8 + gagal acceptance criteria.
- **Fix:** Opsi 2 (fallback yang sudah disebut plan/CRITICAL_ENVIRONMENT_NOTE): `dotnet new tool-manifest` + `dotnet tool install dotnet-ef --version 8.0.0`. Local manifest diutamakan atas global. Hasil: `dotnet ef --version` = 8.0.0; snapshot + Designer ber-ProductVersion 8.0.0 (verified grep `ProductVersion", "8.0.0`).
- **Files modified:** `.config/dotnet-tools.json` (NEW, committed bersama migration di `01cd7dd0`)
- **Verification:** `dotnet ef --version` = 8.0.0 dari direktori repo; ProductVersion 8.0.0 di snapshot + Designer; migration apply sukses; build 0 error.

Catatan: ini bukan scope creep — manifest adalah tooling yang MEMPRODUKSI artifact migration (di-commit bersama agar chain konsisten) dan secara permanen menutup mitigasi T-409-05 (snapshot version mismatch) untuk repo. Plan/CRITICAL_ENVIRONMENT_NOTE secara eksplisit mengizinkan jalur local-manifest ini.

---

**Total deviations:** 1 auto-fixed (1 blocking).
**Impact on plan:** Deviation diperlukan untuk lolos BLOCKING gate (ProductVersion 8.0.x). Tidak ada scope creep — jalur fallback sudah diantisipasi plan.

## Issues Encountered
- **Global EF tool tak bisa di-downgrade:** `dotnet tool update dotnet-ef --version 8.0.0 --global` menolak (versi lebih rendah dari existing 10.0.3). Diselesaikan via local tool manifest (lihat Deviations Rule 3). Global tool 10.0.3 dibiarkan utuh (tak diutak-atik environment user).

## Migration Notes (migration=TRUE)

**migration=TRUE** → saat plan/phase ini ter-merge ke main untuk deploy, **notify IT** dengan:
- **Commit hash migration:** `01cd7dd0` (file `Migrations/20260621011101_AddParticipantRemovalColumns.cs` + Designer + snapshot)
- **Flag migration:** `AddParticipantRemovalColumns` (3 kolom nullable additif di tabel `AssessmentSessions`: `RemovedAt` datetime2, `RemovedBy` nvarchar(max), `RemovalReason` nvarchar(500) — semua NULL untuk baris existing, non-destruktif, NOL backfill).
- IT meng-apply ke DB Dev/Prod (developer TIDAK edit DB Dev/Prod — CLAUDE.md Develop Workflow).
- Carry-migration lama IT yang masih pending notify (catatan STATE): 360 PendingProtonBypass + 372 ShuffleToggles.

## Known Stubs
None — fondasi skema murni; tidak ada UI/data-source stub. 3 kolom nullable additif by-design (penulisan nilai = Phase 411; pembacaan guard = Plan 02). Tidak ada placeholder/hardcoded-empty yang mengalir ke UI.

## User Setup Required
None - no external service configuration required. (Migration apply ke DB Dev/Prod = tanggung jawab IT saat deploy.)

## Next Phase Readiness
- **Plan 02 (guard re-entry + exclude-removed query) SIAP** — kolom `RemovedAt` sudah landing di `HcPortalDB_Dev`, jadi integration test Plan 02 tak akan false-positive. BLOCKING gate Plan 01 TUNTAS.
- **Phase 410** (idempotency cek sesi aktif `RemovedAt==null`), **411** (tulis/clear kolom), **412** (panel "Peserta Dikeluarkan" baca removed) dapat mengkonsumsi skema ini.
- Migration chain utuh (snapshot konsisten, ProductVersion 8.0.0) — migrasi future (411 dst.) akan terus pakai EF 8.0.0 via local manifest.
- Tidak ada blocker.

## Self-Check: PASSED

- Files created/modified verified present: Models/AssessmentSession.cs, Data/ApplicationDbContext.cs, Migrations/20260621011101_AddParticipantRemovalColumns.cs (+Designer), Migrations/ApplicationDbContextModelSnapshot.cs, .config/dotnet-tools.json, 409-01-SUMMARY.md — all FOUND.
- Commits verified in git log: 3806e7b9 (Task 1), 01cd7dd0 (Task 2) — all FOUND.
- DB applied verified via sqlcmd: 3 kolom nullable hadir di AssessmentSessions, RemovalReason len=500, 60 baris existing RemovedAt NULL.
- Snapshot ProductVersion = 8.0.0 (NOT 10.x). Build 0 error.

---
*Phase: 409-data-foundation-re-entry-guards-exclude-removed-query*
*Completed: 2026-06-21*
