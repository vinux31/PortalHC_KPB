---
phase: 410-add-participant-backend-live
verified: 2026-06-21T07:00:00Z
status: passed
score: 10/10
overrides_applied: 0
re_verification: null
gaps: []
deferred: []
---

# Phase 410: Add-Participant Backend Live — Laporan Verifikasi

**Phase Goal:** Backend penambahan peserta live tersedia sebagai endpoint AJAX: Admin/HC dapat menambah satu/lebih peserta ke batch (belum-progres maupun ada InProgress) yang membuat `AssessmentSession` + `UserPackageAssignment` otomatis ber-status siap-mulai (`DeriveReadyStatus` → Open/Upcoming, BUKAN InProgress), ditolak bila window ujian (`ExamWindowCloseDate`) sudah lewat (400 + pesan), idempoten (user yang sudah punya sesi aktif `RemovedAt==null` di batch di-skip diam + report count), dan untuk assessment Pre/Post membuat pasangan sesi Pre+Post.
**Verified:** 2026-06-21T07:00:00Z
**Status:** PASSED
**Re-verification:** Tidak — verifikasi awal.

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | `AddParticipantsLive` membuat `AssessmentSession` ber-status siap-mulai (`DeriveReadyStatus` → Open/Upcoming, NEVER InProgress; `StartedAt`/`CompletedAt`/`RemovedAt` = null) per user baru (PART-06) | VERIFIED | Controller:2322 `BuildReadyParticipantSession` menggunakan `DeriveReadyStatus(rep.Schedule, rep.ExamWindowCloseDate)`; `Status` TIDAK di-set hardcoded; T5 integration membuktikan `Status=="Open"` + `RemovedAt==null` dari DB nyata |
| 2 | Penambahan ditolak 400 + pesan verbatim "Window ujian sudah tutup, tidak bisa tambah peserta." bila `ExamWindowCloseDate` sudah lewat; user yang sudah punya sesi APAPUN di batch (aktif maupun removed) di-skip (idempoten) (PART-06) | VERIFIED | Controller:2372-2373 guard window WIB (`DateTime.UtcNow.AddHours(7)`) + pesan locked verbatim; Controller:2378-2384 idempotency tanpa filter `RemovedAt`; T7 (window 400 + 0-write) + T10 (idempotent skipped[]) hijau dari DB nyata |
| 3 | Penambahan ke assessment Pre/Post membuat pasangan sesi PreTest+PostTest dengan `LinkedSessionId` cross-set; sesi Proton ditolak dengan pesan jelas (PART-07) | VERIFIED | Controller:2411-2423 cabang `isPrePost` buat `newPre` + `newPost`, SaveChanges → cross-link `LinkedSessionId`; Controller:2368-2369 Proton reject dini; T9 (Pre/Post pair + LinkedSessionId) + T8 (Proton 400) hijau dari DB nyata |
| 4 | Pembuatan bersifat atomic per request (gagal → rollback); endpoint mengembalikan JSON `added[]/skipped[]` + counts; `GetEligibleParticipantsToAdd` mengembalikan user yang belum punya sesi APAPUN di batch (D-01/D-02) (PART-06) | VERIFIED | Controller:2405 `BeginTransactionAsync` + 2445 `RollbackAsync` dalam catch; Controller:2476-2482 JSON shape `{added, skipped, addedCount, skippedCount}`; Controller:2302-2306 `alreadyInBatch` tanpa filter `RemovedAt` (D-01); Controller:2309-2313 semua `IsActive` tanpa unit/section filter (D-02); T1/T2/T4 eligible + T10 JSON shape hijau |
| 5 | `dotnet build` 0 error + `dotnet test` hijau (10/10 test baru + 581/581 full suite); migration=FALSE (tidak ada `Migrations/` diff baru dari Phase 410) | VERIFIED | SUMMARY-01: build succeeded 0 error, fast-suite 394/394; SUMMARY-02: 10/10 `FlexibleParticipantAddLive` + full suite 581/581; Migrations/ hanya berisi `AddParticipantRemovalColumns` (Phase 409) — tidak ada migration baru |

**Score:** 5/5 success criteria ROADMAP terpenuhi, 10/10 must-haves PLAN terpenuhi

---

### Deferred Items

Tidak ada item yang di-defer.

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Controllers/AssessmentAdminController.cs` | `AddParticipantsLive` (HttpPost) + `GetEligibleParticipantsToAdd` (HttpGet) + `BuildReadyParticipantSession` helper + `CreateEagerAssignmentsAsync` helper | VERIFIED | Keempat signature ditemukan pada baris :2294, :2322, :2356, :2488 — total +241 baris, 0 baris dihapus; file SUBSTANTIF (logika 9-langkah penuh, bukan stub) |
| `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` | Kelas `FlexibleParticipantAddLiveEligibleTests` (InMemory) + `FlexibleParticipantAddLiveWriteTests` (SQLEXPRESS Integration) — 10 test | VERIFIED | File 584 baris, 2 kelas, 10 `[Fact]`; kelas read-path dan write-path keduanya present; semua 10 test hijau |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| `AddParticipantsLive` | `DeriveReadyStatus` | status derivation per sesi baru | VERIFIED | `:2331` `Status = DeriveReadyStatus(rep.Schedule, rep.ExamWindowCloseDate)` di dalam `BuildReadyParticipantSession` |
| `AddParticipantsLive` | `ShuffleEngine.BuildQuestionAssignment` | eager UPA (A1) via `CreateEagerAssignmentsAsync` | VERIFIED | `:2508` `ShuffleEngine.BuildQuestionAssignment(packages, s.ShuffleQuestions, workerIndex, rng)` dalam `CreateEagerAssignmentsAsync` — dipanggil DALAM transaksi |
| `AddParticipantsLive` | `_auditLog.LogAsync` | audit `AddParticipantLive` | VERIFIED | `:2438-2439` `await _auditLog.LogAsync(actorId, actorName, "AddParticipantLive", ...)` DALAM transaksi sebelum commit |
| `GetEligibleParticipantsToAdd` | `AssessmentSessions` (alreadyInBatch query) | D-01 exclude sesi APAPUN | VERIFIED | `:2302-2306` query `Where(a => a.Title == rep.Title && a.Category == rep.Category && a.Schedule.Date == rep.Schedule.Date)` TANPA filter `RemovedAt` — sesuai D-01 |
| `FlexibleParticipantAddLiveTests` | `AssessmentAdminController.GetEligibleParticipantsToAdd` | InMemory real-controller invocation | VERIFIED | Baris 109 file test: `var result = await ctrl.GetEligibleParticipantsToAdd(sessionId)` — action ASLI di-invoke |
| `FlexibleParticipantAddLiveTests` | `AssessmentAdminController.AddParticipantsLive` | SQLEXPRESS write-path invocation (Opsi 2a) | VERIFIED | Baris 344/402/446/484/523/571 file test: `await ctrl.AddParticipantsLive(repId, ...)` — action ASLI dipanggil atas DB nyata |

---

### Data-Flow Trace (Level 4)

| Artifact | Data Variable | Source | Produces Real Data | Status |
|----------|---------------|--------|--------------------|--------|
| `AddParticipantsLive` | `createdSessions` (list sesi baru) | `_context.AssessmentSessions.Add(session)` + `SaveChangesAsync()` di dalam transaksi DB nyata | Ya — SK nyata ke SQL Server via EF Core; T5/T6/T9/T10 membuktikan row muncul di DB disposable | FLOWING |
| `CreateEagerAssignmentsAsync` | `UserPackageAssignment` row | `_context.UserPackageAssignments.Add(...)` + `SaveChangesAsync()`; data soal dari `AssessmentPackages.Include(p => p.Questions)` DB nyata | Ya — T6 membuktikan `UserPackageAssignment` tercipta EAGER dengan `ShuffledQuestionIds != "[]"` | FLOWING |
| `GetEligibleParticipantsToAdd` | `eligible` list | `_context.Users.Where(u => u.IsActive && !alreadyInBatch.Contains(u.Id))` dari `ApplicationDbContext.Users` | Ya — T1/T2/T4 membuktikan LINQ produksi dikeksekusi atas data InMemory nyata | FLOWING |

---

### Behavioral Spot-Checks

| Behavior | Evidence | Status |
|----------|----------|--------|
| `AddParticipantsLive` reject window tutup → 400 + pesan verbatim | T7: `Assert.Contains("Window ujian sudah tutup, tidak bisa tambah peserta.", JsonSerializer.Serialize(bad.Value))` — hijau atas SQLEXPRESS | PASS |
| `AddParticipantsLive` reject Proton → 400 + 0 write | T8: `Assert.Contains("Assessment Proton", ...)` + count batch tidak bertambah — hijau atas SQLEXPRESS | PASS |
| `AddParticipantsLive` Pre/Post pair → 2 sesi + `LinkedSessionId` cross-set | T9: `Assert.Equal(newPost.Id, newPre.LinkedSessionId)` + `Assert.Equal(newPre.Id, newPost.LinkedSessionId)` — hijau | PASS |
| `GetEligibleParticipantsToAdd` exclude user removed (D-01) | T1: `Assert.DoesNotContain("u-B", eligible)` (sesi RemovedAt!=null) — hijau InMemory | PASS |
| `GetEligibleParticipantsToAdd` tidak filter unit/section (D-02) | T2: `Assert.Contains("u-other-unit", eligible)` (Section/Unit beda) — hijau InMemory | PASS |
| `AddParticipantsLive` idempotent — user existing masuk skipped[] | T10: `addedCount==1` + `skippedCount==1` + existing TIDAK dobel-create — hijau | PASS |
| Tidak ada `_hubContext` dipanggil di dalam `AddParticipantsLive` (D-04) | Controller:2463 hanya komentar "JANGAN sentuh _hubContext"; grep konfirmasi tidak ada `_hubContext.` di dalam method body `AddParticipantsLive` | PASS |
| migration=FALSE — tidak ada Migrations/ baru dari Phase 410 | Migrations/ hanya berisi `AddParticipantRemovalColumns` (Phase 409, timestamp `20260621011101`); Phase 410 commits `01e6251f`/`422b4359`/`2ff434c5` tidak menyertakan file migration | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| PART-06 | 410-01 + 410-02 | Penambahan peserta buat AssessmentSession + UPA ready-status, tolak window lewat, idempoten | SATISFIED | Controller `AddParticipantsLive` :2356 + `BuildReadyParticipantSession` :2322 + `CreateEagerAssignmentsAsync` :2488; T5/T6/T7/T10 hijau |
| PART-07 | 410-01 + 410-02 | Penambahan ke Pre/Post membuat pasangan Pre+Post; Proton ditolak | SATISFIED | Controller :2410-2423 Pre/Post branch + :2368-2369 Proton reject; T8/T9 hijau |

---

### Anti-Patterns Found

| File | Pattern | Severity | Impact |
|------|---------|----------|--------|
| `HcPortal.Tests/FlexibleParticipantAddLiveTests.cs` | NO replica predikat (no `WindowAllowsAddition`/`DeriveReadyStatus` tiruan) — hanya muncul di komentar baris 5 | - | Tidak ada — BUKAN anti-pattern; ini konfirmasi positif bahwa lesson 999.12 dipatuhi |

Tidak ada anti-pattern blocker atau warning ditemukan.

---

### Human Verification Required

Tidak ada item yang memerlukan verifikasi manusia. Semua klaim inti telah diverifikasi secara programatik via:
- Pembacaan langsung kode sumber (method body, atribut, komentar desain)
- File tes de-tautologis yang menjalankan action controller ASLI atas DB nyata (bukan replica)
- Grep untuk pesan locked verbatim, atribut RBAC/antiforgery, dan absensi `_hubContext` di body endpoint

---

### Gaps Summary

Tidak ada gap. Seluruh 5 success criteria ROADMAP dan 10 must-haves PLAN terpenuhi sepenuhnya.

---

## Ringkasan Pencapaian Goal

Phase 410 mencapai goalnya secara penuh:

1. **Endpoint `AddParticipantsLive`** (HttpPost, Admin+HC, antiforgery) hadir di Controller:2356 dengan alur 9-langkah lengkap — validasi input, Proton reject, window guard WIB, idempotency D-01 (sesi APAPUN tanpa filter `RemovedAt`), validate user, resolve actor, transaksi atomic (sesi + eager UPA + audit), notif post-commit, JSON response.

2. **Endpoint `GetEligibleParticipantsToAdd`** (HttpGet, Admin+HC) hadir di Controller:2294 dengan D-01 (exclude sesi APAPUN) + D-02 (semua `IsActive` tanpa filter unit/section).

3. **Helper `BuildReadyParticipantSession`** (Controller:2322) menggunakan `DeriveReadyStatus` — status NEVER InProgress, kolom removal null.

4. **Helper `CreateEagerAssignmentsAsync`** (Controller:2488) cermin `CMPController.StartExam:1038-1117` untuk eager UPA dalam transaksi atomic (A1 LOCKED).

5. **10 test de-tautologis** (`FlexibleParticipantAddLiveTests.cs`) menjalankan action ASLI atas InMemory (read-path) dan SQLEXPRESS nyata (write-path); tidak ada replica predikat; 10/10 hijau; full suite 581/581 tidak ada regresi.

6. **migration=FALSE** terverifikasi — tidak ada file Migrations baru dari Phase 410.

7. **D-04** terpenuhi — tidak ada `_hubContext` dipanggil di dalam `AddParticipantsLive` (SignalR di-defer ke Phase 412).

---

_Verified: 2026-06-21T07:00:00Z_
_Verifier: Claude (gsd-verifier)_
