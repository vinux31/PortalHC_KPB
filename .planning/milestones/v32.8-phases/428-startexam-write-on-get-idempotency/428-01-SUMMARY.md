---
phase: 428-startexam-write-on-get-idempotency
plan: 01
subsystem: api
tags: [csharp, aspnet-mvc, ef-core, exam, idempotency, impersonation, xunit, real-sql]

# Dependency graph
requires:
  - phase: 427-exam-token-gate-server-authoritative
    provides: "token-gate EXSEC-01 (TokenVerifiedAt) di StartExam:964-972 — WAJIB tetap utuh pasca-refactor"
  - phase: 424 (v32.7 GRDF)
    provides: "GRDF-01 gating Pre->Post via PrePostPairing.FindPairedPreAsync di StartExam"
provides:
  - "GET CMP/StartExam(id) idempoten untuk transisi status Upcoming->Open (tidak ada write-on-GET)"
  - "Effective-status by-schedule in-memory (time-gate: Status==\"Upcoming\" && Schedule > nowWib)"
  - "StartExamIdempotencyTests.cs — 6 test integrasi real-SQL (idempotensi + regresi seluruh gate)"
affects: [StartExam, exam-taking, impersonation-monitoring, merge-reconciliation-vs-main]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Effective-status in-memory (read-only by-schedule) menggantikan persist-then-read — mirror lobby Assessment:245-251"
    - "Impersonation path sebagai satu-satunya GET non-starting untuk membuktikan no-write-on-GET (justStarted di-guard !IsImpersonating)"

key-files:
  created:
    - "HcPortal.Tests/StartExamIdempotencyTests.cs"
  modified:
    - "Controllers/CMPController.cs (StartExam GET :922-939 -> effective-status in-memory)"

key-decisions:
  - "D-02 INLINE: ubah kondisi time-gate jadi Status==\"Upcoming\" && Schedule > nowWib; tidak ekstrak helper (1 konsumen, R-1 minimal)"
  - "D-01 SCOPE: justStarted InProgress write (:1021) + assignment-create (:1106) TETAP di GET (out of scope)"
  - "Fixture: AccessToken selalu non-null (kolom NOT NULL di schema) — irelevan saat IsTokenRequired=false"

patterns-established:
  - "GET idempoten: transisi status pasif (Upcoming->Open) dihitung in-memory, bukan dipersist; transisi aktual hanya saat aksi start"
  - "Test no-write-on-GET via impersonation path (read-only) + reload DB assert Status tak berubah"

requirements-completed: [EXSEC-02]

# Metrics
duration: 8min
completed: 2026-06-25
---

# Phase 428 Plan 01: StartExam Write-on-GET Idempotency Summary

**GET CMP/StartExam(id) jadi idempoten — blok persist `Upcoming->Open` (Status="Open"+SaveChangesAsync) dihapus, diganti effective-status by-schedule in-memory (`Status=="Upcoming" && Schedule > nowWib`); 6 test real-SQL membuktikan idempotensi + regresi seluruh gate (time-gate/GRDF-01/token-gate 427) tetap memblok.**

## Performance

- **Duration:** 8m 12s
- **Started:** 2026-06-25T01:11:23Z
- **Completed:** 2026-06-25T01:19:35Z
- **Tasks:** 2
- **Files modified:** 2 (1 modified, 1 created)

## Accomplishments
- StartExam GET tidak lagi menulis DB untuk transisi status `Upcoming->Open` (idempotensi GET dipulihkan, EXSEC-02 / backlog 999.14 / FLOW-10 ditutup).
- Time-gate diganti ke effective-status by-schedule in-memory: hanya memblok bila benar-benar belum waktunya; Upcoming + waktu-tiba diperlakukan openable tanpa write.
- Gate ordering R-1 utuh (time-gate -> Completed -> GRDF-01 -> token-gate EXSEC-01); justStarted InProgress write + assignment-create tak tersentuh (D-01).
- 6 test integrasi real-SQL baru (`StartExamIdempotencyTests.cs`) — membuktikan SC#1-#4 + regresi token-gate 427; double-GET impersonate Status tetap Upcoming.

## Task Commits

Each task was committed atomically:

1. **Task 1: Refactor StartExam GET ke effective-status in-memory** - `a3d133fc` (refactor)
2. **Task 2: Tulis StartExamIdempotencyTests.cs (6 test real-SQL)** - `b7ca8caf` (test)

_Catatan: meskipun task ber-`tdd="true"`, sifat plan = refactor read-only-pemulihan-idempotensi pada blok existing; test ditulis sebagai pembuktian-pasca-refactor (idempotensi tak bisa diuji RED-dulu tanpa membongkar produk). Lihat TDD Gate Compliance di bawah._

## Files Created/Modified
- `Controllers/CMPController.cs` - StartExam GET: hapus blok auto-transition persist (Status="Open" + UpdatedAt + SaveChangesAsync, dan guard impersonate-nya); ganti time-gate ke `var nowWib = DateTime.UtcNow.AddHours(7); if (Status=="Upcoming" && Schedule > nowWib) ...` (net -6 baris).
- `HcPortal.Tests/StartExamIdempotencyTests.cs` - 6 [Fact] real-SQL (`IClassFixture<RetakeServiceFixture>`, `[Trait("Category","Integration")]`); reuse FakeUserStore/StubSession/MakeUserManager/MakeCmp/StubUrlHelper/SeedUser/SeedPackage dari TokenVerifiedAtTests + factory baru `MakeCmpImpersonating` + seeder fleksibel `SeedSessionAsync`.

## Decisions Made
- **INLINE (D-02):** Tidak ekstrak helper `IsEffectivelyOpen`. Satu-satunya konsumen status pra-start adalah time-gate; cukup ubah kondisinya. Minimalkan baris demi merge-safety R-1 (StartExam = zona konflik PASTI vs main).
- **Impersonation = jalur uji no-write-on-GET:** GET owner non-impersonate pada Upcoming-waktu-tiba men-trigger justStarted InProgress write (worker mulai aktual), jadi tak bisa langsung mengamati "tak ada persist Upcoming->Open". Jalur impersonate (justStarted di-guard `!IsImpersonating()`) = GET 100% read-only -> reload DB Status tetap Upcoming membuktikan idempotensi (T1/T2).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixture seeder menulis NULL ke kolom AccessToken (NOT NULL)**
- **Found during:** Task 2 (StartExamIdempotencyTests — run pertama)
- **Issue:** Seeder fleksibel awalnya `AccessToken = isTokenRequired ? "ABC23X" : null`. Kolom `AssessmentSession.AccessToken` NOT NULL di schema -> `SqlException: Cannot insert the value NULL into column 'AccessToken'` saat `IsTokenRequired=false` (5 dari 6 test gagal di seed). Bug di test-fixture, BUKAN produk (TokenVerifiedAtTests yang terbukti selalu pakai non-null "ABC23X").
- **Fix:** `AccessToken = "ABC23X"` selalu (nilai irelevan saat token off; gate token tetap tak aktif karena `IsTokenRequired=false`).
- **Files modified:** HcPortal.Tests/StartExamIdempotencyTests.cs (seeder; tidak ada perubahan produk).
- **Verification:** re-run -> 6/6 pass.
- **Committed in:** `b7ca8caf` (bagian dari commit Task 2 — bug ditemukan & ditutup sebelum file pertama kali di-commit).

---

**Total deviations:** 1 auto-fixed (1 bug fixture).
**Impact on plan:** Perbaikan murni di test-fixture (skema-awareness), tanpa menyentuh produk maupun melemahkan assertion. Tidak ada scope creep. Tidak ada bug produk ditemukan — refactor T1 lolos seluruh test apa adanya.

## TDD Gate Compliance

Plan task ditandai `tdd="true"`, namun karakter pekerjaan = refactor pemulihan-idempotensi pada blok yang sudah ada (menghapus side-effect, bukan menambah behavior baru). Test idempotensi tak dapat ditulis sebagai RED-gate murni (perilaku "tidak menulis DB" hanya bisa diobservasi setelah persist dihapus). Urutan eksekusi: T1 refactor produk (`refactor` commit `a3d133fc`) -> T2 test pembuktian (`test` commit `b7ca8caf`, 6/6 GREEN sejak fixture-fix). Tidak ada `feat` commit (tidak ada fitur baru — net pengurangan side-effect). Verifikasi end-state: full suite 784/0/2 (no regression), filter `StartExamIdempotencyTests` 6/6 pass real-SQL. Catatan ini didokumentasikan agar gate-sequence transparan untuk verifier.

## Issues Encountered
- Seeder fixture NULL AccessToken (lihat Deviations) — resolved dalam 1 iterasi.

## Threat Surface Scan
Tidak ada surface baru. Perubahan REDUCING side-effect (menghapus 1 write-on-GET yang tak-terguard untuk transisi status). Tidak ada route/endpoint/schema/network baru (migration=FALSE). Impersonate StartExam menjadi 100% read-only (persist-Open satu-satunya write tak-terguard di blok lama — kini dihapus; InProgress write + assignment-create tetap di-guard `!IsImpersonating()`). Tidak ada threat flag.

## User Setup Required
None - tidak ada konfigurasi service eksternal. migration=FALSE (notify IT: tidak ada migration baru di fase ini).

## Next Phase Readiness
- EXSEC-02 selesai. v32.8 in-scope: 426 (✅) + 427 (✅) + 428 (✅ plan 01, fase tunggal-plan).
- Merge-reconciliation note (R-1): saat merge ITHandoff<->main, pertahankan KEDUA lapisan di StartExam — GRDF-01 setelah cek-Completed sebelum token-gate; effective-status menggantikan blok persist+time-gate lama (jangan re-introduce `Status="Open"` persist).
- Deferred (catatan): idempotensi GET PENUH (pindah InProgress/StartedAt + assignment-create ke jalur POST eksplisit) = refactor besar exam-taking, di luar EXSEC-02.

## Self-Check: PASSED

- FOUND: `HcPortal.Tests/StartExamIdempotencyTests.cs`
- FOUND: `.planning/phases/428-startexam-write-on-get-idempotency/428-01-SUMMARY.md`
- FOUND commit: `a3d133fc` (Task 1 refactor)
- FOUND commit: `b7ca8caf` (Task 2 test)
- STATIC: `grep -c 'assessment.Status = "Open"' Controllers/CMPController.cs` = 0 (no persist-Open di StartExam)
- TESTS: `StartExamIdempotencyTests` 6/6 pass (real-SQL); full suite 784/0/2 (no regression).

---
*Phase: 428-startexam-write-on-get-idempotency*
*Completed: 2026-06-25*
