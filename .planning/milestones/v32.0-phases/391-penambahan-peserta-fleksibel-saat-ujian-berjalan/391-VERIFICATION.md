---
phase: 391-penambahan-peserta-fleksibel-saat-ujian-berjalan
verified: 2026-06-17T00:00:00+07:00
status: human_needed
score: 8/8
overrides_applied: 0
---

# Phase 391: Penambahan Peserta Fleksibel saat Ujian Berjalan — Verification Report

**Phase Goal:** HC tetap dapat menambah peserta baru ke assessment yang sedang berjalan (ada peserta InProgress) tanpa friksi; sesi peserta baru ber-status SIAP-MULAI (Open jika jadwal tiba, Upcoming jika belum) — BUKAN mewarisi status induk; guard Completed tak salah-blokir penambahan selama window terbuka; warning kosmetik diganti notice informatif (TempData Info); seluruh perilaku dikunci automated regression test. Migration=FALSE.
**Verified:** 2026-06-17
**Status:** human_needed
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| #  | Truth                                                                                                                                         | Status     | Evidence                                                                                                                              |
|----|-----------------------------------------------------------------------------------------------------------------------------------------------|------------|---------------------------------------------------------------------------------------------------------------------------------------|
| 1  | HC dapat menambah peserta baru ke assessment yang sedang berjalan tanpa diblokir guard Completed selama window terbuka                        | VERIFIED   | Guard (L1997-2002): `bool hasAddition = NewUserIds != null && NewUserIds.Count > 0;` + `if (assessment.Status == AssessmentConstants.AssessmentStatus.Completed && !hasAddition)` — jalur penambahan lolos, EDIT murni tetap ditolak. |
| 2  | Sesi peserta baru lahir ber-status siap-mulai (Open/Upcoming) — BUKAN mewarisi status induk                                                   | VERIFIED   | `Status = DeriveReadyStatus(savedAssessment.Schedule, savedAssessment.ExamWindowCloseDate)` (L2171). `Status = savedAssessment.Status` = 0 match (hilang). Helper (L2243-2250) pakai `DateTime.UtcNow.AddHours(7)` (WIB) + konstanta `AssessmentConstants.AssessmentStatus.*`. |
| 3  | Sesi peserta yang sedang berjalan (StartedAt!=null && CompletedAt==null) tidak ter-overwrite Status/Schedule/DurationMinutes oleh edit-loop    | VERIFIED   | Edit-loop standar (L2067): `if (sibling.StartedAt != null && sibling.CompletedAt == null) continue;`. Pre-Post preGroup (L1852) dan postGroup (L1863): filter identik. Fact (c) mengunci selektivitas ini — sesi belum-mulai JUSTRU berubah. |
| 4  | Saat ada peserta InProgress, HC melihat notice Info (alert biru) bukan Warning (alert kuning)                                                 | VERIFIED   | `TempData["Info"] = "Ada peserta yang sedang mengerjakan ujian. ..."` (L2092-2094). `_Layout.cshtml` L210-218: render sebagai `alert alert-info` dengan ikon `bi-info-circle-fill`. TempData["Warning"] pada jalur EditAssessment InProgress sudah hilang. |
| 5  | Automated regression test mengunci: penambahan saat ada InProgress berhasil membuat sesi baru                                                  | VERIFIED   | Fact (a) `AddParticipant_WithInProgressSibling_CreatesNewSession` — seed InProgress user1, tambah user2, Assert.Equal(2, siblings.Count) + Assert.Contains user2. |
| 6  | Test mengunci: sesi peserta baru ber-status siap-mulai (Open/Upcoming) BUKAN InProgress                                                        | VERIFIED   | Fact (b) `AddParticipant_NewSession_HasReadyStatus_NotInProgress` — past→Open/!=InProgress, future→Upcoming/!=InProgress. |
| 7  | Test mengunci: sesi InProgress existing Status/Schedule/DurationMinutes UNCHANGED setelah edit+tambah                                         | VERIFIED   | Fact (c) `AddParticipant_InProgressSibling_StatusScheduleDurationUnchanged` — Assert InProgress: Status==InProgress, Schedule==schedOrig, DurationMinutes==60. Kontrol-positif: sesi belum-mulai DurationMinutes berubah ke 120. |
| 8  | Test mengunci: penambahan TIDAK terblokir saat sebagian sesi Completed selama window terbuka                                                   | VERIFIED   | Fact (d) `AddParticipant_SomeCompleted_NotBlocked_WhileWindowOpen` — WindowAllowsAddition(null)==true, sesi baru tercipta (Assert.Equal(2), Assert.Contains uNew). |

**Score: 8/8 truths verified**

---

### Required Artifacts

| Artifact                                              | Expected                                                                 | Status     | Details                                                                                             |
|-------------------------------------------------------|--------------------------------------------------------------------------|------------|-----------------------------------------------------------------------------------------------------|
| `Controllers/AssessmentAdminController.cs`            | Helper DeriveReadyStatus + 5 titik bedah D-01..D-05                     | VERIFIED   | Helper ada L2243-2250 (WIB benar, konstanta). D-01: Status via DeriveReadyStatus L2171. D-02: guard+hasAddition L1997-2002. D-03: skip standar L2067. D-04: TempData["Info"] L2092. D-05: skip Pre-Post L1852+L1863. |
| `HcPortal.Tests/FlexibleParticipantAddTests.cs`       | Fixture disposable + >=4 facts (a/b/c/d) PART-04, min 120 baris         | VERIFIED   | File ada, 232 baris. FlexibleParticipantAddFixture:IAsyncLifetime. 4 [Fact] (a/b/c/d). WIB DateTime.UtcNow.AddHours(7) 6x. EnsureDeletedAsync 2x. [Trait("Category","Integration")]. |

---

### Key Link Verification

| From                                           | To                                               | Via                                                                  | Status   | Details                                                                                         |
|------------------------------------------------|--------------------------------------------------|----------------------------------------------------------------------|----------|-------------------------------------------------------------------------------------------------|
| EditAssessment POST BULK ASSIGN newSessions    | DeriveReadyStatus(schedule, examWindowCloseDate) | `Status = DeriveReadyStatus(savedAssessment.Schedule, ...)` (L2171) | WIRED    | Tepat 1 match. Baris lama `Status = savedAssessment.Status` = 0 match.                         |
| EditAssessment POST edit-loop siblings         | skip sesi berjalan                               | `if (sibling.StartedAt != null && sibling.CompletedAt == null) continue;` (L2067) | WIRED | Tepat 1 match di dalam edit-loop standar.                                              |
| EditAssessment POST guard Completed            | izinkan penambahan saat window terbuka           | `&& !hasAddition` (L1998)                                            | WIRED    | Guard hanya blokir saat `!hasAddition`. Jalur penambahan lolos sepenuhnya.                     |
| EditAssessment POST hasInProgress notice       | _Layout Info alert                               | `TempData["Info"]` (L2092)                                           | WIRED    | _Layout.cshtml L210-218 render `alert alert-info`. Tidak perlu perubahan view.                 |
| FlexibleParticipantAddFixture                  | HcPortalDB_Test_{guid} @ localhost\SQLEXPRESS    | IAsyncLifetime MigrateAsync → EnsureDeletedAsync                     | WIRED    | Connection string hardcode `HcPortalDB_Test_{Guid.NewGuid():N}`. DisposeAsync EnsureDeletedAsync. |
| FlexibleParticipantAddTests                    | Category=Integration filter                      | `[Trait("Category", "Integration")]`                                 | WIRED    | Kelas test L55: `[Trait("Category", "Integration")]`.                                          |
| Test DeriveReadyStatus replica                 | Controller DeriveReadyStatus (Plan 01)           | Mirror byte-identik WIB DateTime.UtcNow.AddHours(7)                 | WIRED    | Test L89-93 identik dengan controller L2245-2249: `nowWib = DateTime.UtcNow.AddHours(7)`, `schedule <= nowWib ? S.Open : S.Upcoming`. |

---

### Data-Flow Trace (Level 4)

Tidak berlaku untuk phase ini. Perubahan adalah logika controller (status derivation) + test — bukan komponen yang render data dari state/store dinamis. Flow data: BULK ASSIGN di controller menetapkan `Status = DeriveReadyStatus(...)` langsung saat INSERT ke DB — ini adalah source of truth, bukan intermediate state.

---

### Behavioral Spot-Checks

| Behavior                                                              | Method                                                             | Result                                                         | Status   |
|-----------------------------------------------------------------------|--------------------------------------------------------------------|----------------------------------------------------------------|----------|
| DeriveReadyStatus menggunakan WIB (bukan DateTime.Now/DateTime.UtcNow) | Grep `DateTime.UtcNow.AddHours(7)` di controller                 | Match di L2245 di dalam DeriveReadyStatus                      | PASS     |
| `Status = savedAssessment.Status` sudah hilang                        | Grep `Status = savedAssessment\.Status`                           | 0 match — baris lama tergantikan sepenuhnya                    | PASS     |
| Pre-Post per-phase loops punya filter sesi berjalan (D-05)            | Read L1849-1870                                                    | Skip ada di L1852 (preGroup) dan L1863 (postGroup)             | PASS     |
| Test file punya 4 [Fact] dengan re-query via NewCtx() terpisah        | Grep `\[Fact\]` dan `await using var verify = NewCtx()`          | 4 [Fact], 4 `await using var verify = NewCtx()`                | PASS     |
| Commits 4 ada di git log                                              | git log (d87381a1, 7273f37e, 746fed94, 31e71a3e)                  | Semua 4 commit ada, pesan sesuai D-01/D-02/D-03/D-04/D-05     | PASS     |
| 0 migration                                                           | git diff --name-only d87381a1~1 31e71a3e (grep migration)         | 0 file Migrations/ tersentuh                                   | PASS     |

---

### Requirements Coverage

| Requirement | Source Plan  | Description                                                                                                                           | Status     | Evidence                                                                                          |
|-------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------|------------|---------------------------------------------------------------------------------------------------|
| PART-01     | Plan 01      | HC dapat menambah peserta baru ke assessment sedang berjalan tanpa diblokir; peserta baru mewarisi status sehingga bisa langsung mengerjakan | VERIFIED   | CONTEXT D-01 supersede REQUIREMENTS.md wording "mewarisi status induk" — sesi baru lahir Open/Upcoming (siap-mulai) bukan inherit InProgress. Guard tidak blokir jalur penambahan. Peserta baru langsung bisa StartExam (status Open/Upcoming adalah valid entry di CMPController.StartExam). |
| PART-02     | Plan 01      | Guard Completed tidak salah-memblokir penambahan; HC tetap dapat menambah peserta selama window ujian belum lewat                     | VERIFIED   | D-02: `bool hasAddition` + `&& !hasAddition` di guard. Guard lolos saat ada penambahan. Fallback ExamWindowCloseDate==null = boleh tambah (longgar). |
| PART-03     | Plan 01      | Notice informatif (nuansa informasi, bukan warning kesan-error) saat ada peserta InProgress                                           | VERIFIED   | TempData["Info"] dengan wording menenangkan. _Layout render `alert alert-info` (biru). TempData["Warning"] jalur InProgress = 0 match. |
| PART-04     | Plan 02      | Perilaku dikunci automated regression test: (a) add berhasil, (b) peserta baru siap-mulai, (c) existing tidak ter-overwrite, (d) tidak terblokir saat Completed+window | VERIFIED   | 4 facts (a/b/c/d) di FlexibleParticipantAddTests.cs. Full suite 486/486 green (per SUMMARY-02). |

**Catatan PART-01 vs REQUIREMENTS.md wording:** REQUIREMENTS.md L15 menyebut "peserta baru mewarisi status induk sehingga bisa langsung mengerjakan". CONTEXT.md D-01 (sebagai keputusan implementasi yang lebih baru) menetapkan sesi baru BUKAN inherit status induk, melainkan mendapat status siap-mulai (Open/Upcoming). Ini adalah SUPERSEDE yang disengaja — BUKAN kegagalan. Kedua tujuan akhir sama: peserta baru dapat langsung mengerjakan (StartExam valid untuk status Open). PART-01 dianggap VERIFIED karena tujuan bisnis terpenuhi dan keputusan design ini terdokumentasi di CONTEXT.md.

---

### Anti-Patterns Found

| File                                    | Line  | Pattern                          | Severity | Impact                                                      |
|-----------------------------------------|-------|----------------------------------|----------|-------------------------------------------------------------|
| Tidak ada anti-pattern ditemukan        | —     | —                                | —        | Implementasi bersih; tidak ada stub/placeholder/TODO di area yang dimodifikasi. |

Pemeriksaan dilakukan pada area yang dimodifikasi (L1820-2095, L2241-2250, FlexibleParticipantAddTests.cs): tidak ada `TODO`, `FIXME`, `return null`, `return {}`, `return []`, handler kosong, atau hardcoded empty data yang relevan.

---

### Human Verification Required

Automated checks semua pass. Satu area perlu verifikasi manusia karena melibatkan runtime browser dan alur penuh UAT:

#### 1. Alur Penambahan Peserta ke Assessment Berjalan (End-to-End)

**Test:** Login sebagai HC di Dev (`http://10.55.3.3/KPB-PortalHC`). Buka assessment yang sudah ada peserta dengan status InProgress (atau simulasikan: buat assessment, assign 1 peserta, peserta mulai ujian → status berubah InProgress). Lalu buka EditAssessment untuk assessment tersebut dan tambah peserta baru.

**Expected:**
- Penambahan berhasil (tidak muncul error, redirect ke ManageAssessment)
- Muncul alert biru (Info) berisi teks menenangkan tentang peserta sedang mengerjakan, bukan alert kuning (Warning)
- Peserta baru muncul di daftar dengan status Open atau Upcoming (bukan InProgress)
- Peserta yang sedang ujian Status/Schedule/DurationMinutes tidak berubah

**Why human:** Memerlukan environment Dev yang sudah di-deploy IT, sesi InProgress nyata (timing-sensitive), dan verifikasi visual alert warna + wording di browser.

#### 2. Guard Completed — EDIT Murni Tetap Ditolak

**Test:** Di Dev, buka assessment yang semua pesertanya sudah Completed. Lakukan EDIT (ubah Title atau field lain) TANPA menambah peserta baru.

**Expected:** Muncul error "Cannot edit completed assessments." dan redirect kembali ke ManageAssessment. Tidak ada perubahan tersimpan.

**Why human:** Memerlukan assessment dengan status Completed yang tersedia di Dev, dan verifikasi perilaku redirect + pesan error di browser.

---

### Gaps Summary

Tidak ada gap yang ditemukan. Semua 8 must-have truths VERIFIED di codebase. Implementation correctness dikonfirmasi:

- `DeriveReadyStatus` menggunakan WIB yang benar (`DateTime.UtcNow.AddHours(7)`) — bukan `DateTime.Now` atau `DateTime.UtcNow` polos (regresi d844c552 tidak terjadi ulang)
- `Status = savedAssessment.Status` sudah tidak ada (0 match) — digantikan sepenuhnya
- Guard `!hasAddition` ada dan lokasinya tepat (setelah Pre-Post branch return, sebelum BULK ASSIGN)
- Skip sesi berjalan ada di 3 titik: edit-loop standar (L2067) + preGroup Pre-Post (L1852) + postGroup Pre-Post (L1863)
- `TempData["Info"]` menggantikan `TempData["Warning"]` pada jalur InProgress
- `_Layout.cshtml` render Info sebagai `alert alert-info` — 0 perubahan view diperlukan
- 4 regression facts (a/b/c/d) real-SQL integration di FlexibleParticipantAddTests.cs dengan re-query via NewCtx() terpisah
- 0 migration — 0 file Migrations/ tersentuh
- Commits 4 (d87381a1, 7273f37e, 746fed94, 31e71a3e) semua ada di git log main

Status `human_needed` karena 2 item UAT browser diperlukan untuk konfirmasi penuh alur runtime di Dev environment.

---

_Verified: 2026-06-17_
_Verifier: Claude (gsd-verifier)_
