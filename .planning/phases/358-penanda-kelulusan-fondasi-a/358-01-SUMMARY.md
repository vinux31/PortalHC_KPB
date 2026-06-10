---
phase: 358-penanda-kelulusan-fondasi-a
plan: 01
status: complete
requirements: [PCOMP-04]
commits: [34ac03e0, c942d0da]
migration: true
---

# Plan 358-01 SUMMARY — Kolom Origin + Migration + Fixture

## Apa yang dibangun
- **Field `Origin`** (`[MaxLength(20)] public string? Origin`) di `ProtonFinalAssessment` (`Models/ProtonModels.cs`). Pembeda 3 jalur penanda: `"Exam"` | `"Interview"` | `"Bypass"`.
- **Migration `20260610014907_AddOriginToProtonFinalAssessment`** (+Designer +ModelSnapshot): `AddColumn` Origin nvarchar(20) null + data-seed `UPDATE ProtonFinalAssessments SET Origin='Interview' WHERE Origin IS NULL`. Applied lokal HcPortalDB_Dev.
- **Fixture test real-SQL** `HcPortal.Tests/ProtonCompletionServiceTests.cs`: `ProtonCompletionFixture` (TEST-05 disposable `HcPortalDB_Test_<guid>`, `MigrateAsync` penuh, drop sukses+gagal) + smoke `[Fact] Migration_AddsOriginColumn` (kolom Origin ter-map + queryable). Siap diisi [Fact] service Plan 02.

## Verifikasi
- `dotnet build` 0 error.
- `dotnet test --filter ProtonCompletionServiceTests` → 1/1 pass.
- `COL_LENGTH('ProtonFinalAssessments','Origin')` = 40 (nvarchar(20)).
- DB lokal: 0 baris penanda (kosong) → data-seed no-op lokal, terbukti tereksekusi di log `ef database update`. Akan men-`'Interview'`-kan baris lama saat IT promosi Dev/Prod.
- Snapshot pre-migration `C:\Temp\HcPortalDB_Dev_pre358origin.bak` (16 MB) + entry `docs/SEED_JOURNAL.md`.

## Checkpoint human-verify
APPROVED (user verifikasi: DB 0 baris OK, snapshot ada, commit OK).

## ⚠️ IT-FLAG
**migration=TRUE** — `20260610014907_AddOriginToProtonFinalAssessment`. Promosi Dev/Prod = Team IT. Commit: `34ac03e0`. Data-seed akan set baris penanda lama → `'Interview'` di Dev/Prod.

## Catatan untuk plan berikutnya
- Fixture `ProtonCompletionFixture` + `ProtonCompletionServiceTests` siap pakai (Plan 02 tambah [Fact] Ensure/Remove/GetPassedYears).
- ProtonTracks di-seed migration (HasData, UNIQUE TrackType+TahunKe) — test reuse existing track, JANGAN insert.
