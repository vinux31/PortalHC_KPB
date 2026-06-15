---
phase: 382-grading-lifecycle-cert
verified: 2026-06-15T04:30:00Z
status: human_needed
score: 11/11
overrides_applied: 0
human_verification:
  - test: "Dashboard CMP/CDP/Renewal — tampilkan cert worker lulus ValidUntil=null sebagai Aktif/Permanen"
    expected: "Baris cert worker tsb menampilkan status 'Aktif' atau 'Permanen', BUKAN 'Expired'. Cert null tidak masuk worklist renewal. Badge/notif Home tidak undercount/kontradiktif."
    why_human: "Rendering visual lintas 3 dashboard (CMP/CDP/Renewal + badge Home). DB-level dan predicate-mirror sudah ter-otomasi (e2e #12 DB-assert + CertAlertConsistencyTests). Yang tersisa adalah pixel-level rendering di browser yang tidak bisa di-assert programatik."
---

# Phase 382: Grading / Lifecycle / Cert — Verification Report

**Phase Goal:** Nilai, kelulusan, dan sertifikat worker single-answer benar & tahan-race — grading baca jawaban final tanpa baris duplikat, sesi Abandoned/Cancelled tak bisa di-resurrect, hasil graded tak ketimpa abandon telat, timer Normal ditegakkan, gate token tak bisa di-bypass via save/submit, dan cert ValidUntil=null tampil konsisten "aktif".
**Verified:** 2026-06-15T04:30:00Z
**Status:** human_needed
**Re-verification:** Tidak — verifikasi initial.

## Goal Achievement

### Observable Truths

| #  | Truth                                                                                                                    | Status     | Bukti                                                                                                                                                                |
|----|--------------------------------------------------------------------------------------------------------------------------|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1  | GradingService menilai MC dari jawaban FINAL (SubmittedAt terbaru) — duplikat/basi tidak memengaruhi Score              | VERIFIED   | `finalByQuestion` + `OrderByDescending(r => r.SubmittedAt).First()` di GradingService.cs L87-90; `TryGetValue` menggantikan `FirstOrDefault` di L107 + L162        |
| 2  | Branch MultipleAnswer TIDAK ter-dedupe (multi-row tetap dibaca penuh)                                                   | VERIFIED   | `finalByQuestion` hanya apply `Where(r => r.PackageOptionId.HasValue)` — MA tidak punya PackageOptionId tunggal; GradingDedupeTests.Dedupe_MultipleAnswer_NotDeduped hijau |
| 3  | GradeAndCompleteAsync non-essay menolak commit Completed-lulus saat sesi Abandoned/Cancelled/PendingGrading             | VERIFIED   | GradingService.cs L254-257: `Status != S.Abandoned && Status != S.Cancelled && Status != S.PendingGrading`; SubmitResurrectionTests Test C/D hijau                  |
| 4  | Branch essay menolak commit PendingGrading saat sesi Abandoned/Cancelled                                                | VERIFIED   | GradingService.cs L215-217: `Status != S.Abandoned && Status != S.Cancelled`; covered oleh SubmitResurrectionTests + guard test                                     |
| 5  | AssessmentConstants.AssessmentStatus.Abandoned = single-source const                                                    | VERIFIED   | `Models/AssessmentConstants.cs` L21: `public const string Abandoned = "Abandoned";`                                                                                 |
| 6  | SubmitExam SAVE-01: GroupBy final answer pakai OrderByDescending(SubmittedAt) — push==stored Score                      | VERIFIED   | CMPController.cs L1691-1693: `GroupBy(...).ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First())`                                          |
| 7  | SubmitExam STAT-01: POST ke sesi terminal ditolak + audit SubmitExamBlocked                                             | VERIFIED   | CMPController.cs L1605-1610: guard terminal set `{Completed,Abandoned,Cancelled,PendingGrading}` + `WriteSubmitBlockedAuditAsync`; e2e #8 green                     |
| 8  | SubmitExam + SaveAnswer TOK-02: IsTokenRequired && StartedAt==null → reject                                             | VERIFIED   | CMPController.cs L373-374 (SaveAnswer Json reject) + L1596-1599 (SubmitExam redirect); `ShouldGateMissingStart` pure helper; TokenGateTests 4/4                    |
| 9  | AbandonExam STAT-02: ExecuteUpdate atomic WHERE(Id && UserId && (InProgress\|\|Open)); rowsAffected==0 → reject        | VERIFIED   | CMPController.cs L1283-1294: `ExecuteUpdateAsync` WHERE `UserId==user.Id && (InProgress\|\|Open)` + rowsAffected==0 guard; AbandonGuardTests 3/3 real-SQL          |
| 10 | EnsureCanSubmitExamAsync TMR-01: Standard kini di-enforce (skip hanya Manual/null); TMR-03 token consume on success    | VERIFIED   | `ShouldEnforceSubmitTimer` blocklist CMPController.cs L4468-4473; `TempData.Keep` L4546 + `TempData.Remove` HANYA di success path L1764; EnsureCanSubmitStandardTests 5/5 |
| 11 | DeriveCertificateStatus(null, null) → Aktif (BUKAN Expired) — single-source CERT-01                                    | VERIFIED   | `Models/CertificationManagementViewModel.cs` L59-60: `return CertificateStatus.Aktif`; CertificateStatusTests _ReturnsAktif; CertAlertConsistencyTests 4/4; e2e #12 DB-assert |
| 12 | Tidak ada migration — dotnet ef migrations list tidak bertambah                                                          | VERIFIED   | `git diff 049c21bf^..HEAD -- Migrations/` kosong (exit 0, no output); `dotnet ef migrations add _verify_382` → empty Up/Down di Summary 03; migration=false holds  |

**Score: 11/11 truths verified** (versi kode; 1 item memerlukan konfirmasi visual human — lihat Human Verification)

### Required Artifacts

| Artifact                                          | Disediakan oleh | Status      | Detail                                                                                  |
|---------------------------------------------------|-----------------|-------------|-----------------------------------------------------------------------------------------|
| `Models/AssessmentConstants.cs`                   | Plan 01         | VERIFIED    | `Abandoned` const L21 ada; single-source label                                         |
| `Services/GradingService.cs`                      | Plan 01         | VERIFIED    | `finalByQuestion` + `OrderByDescending` L87-90; guard expand kedua branch L215-217/254-257 |
| `HcPortal.Tests/GradingDedupeTests.cs`            | Plan 01         | VERIFIED    | Exists; `GradingDedupeFixture` real-SQL disposable; `Dedupe_PicksLatestSubmittedAt` + `Dedupe_MultipleAnswer_NotDeduped` |
| `HcPortal.Tests/SubmitResurrectionTests.cs`       | Plan 01         | VERIFIED    | Exists; `SubmitResurrectionFixture` real-SQL; `Grade_OnAbandonedSession_Rejected` + `Grade_OnCancelledSession_Rejected` |
| `Controllers/CMPController.cs`                    | Plan 02         | VERIFIED    | `ExecuteUpdateAsync` AbandonExam; SAVE-01 OrderBy; STAT-01 guard+audit; TOK-02 gate; TMR-01 blocklist; TMR-03 Keep/consume-on-success; 4 pure helper |
| `HcPortal.Tests/AbandonGuardTests.cs`             | Plan 02         | VERIFIED    | Exists; `[Trait("Category","Integration")]`; GUID DbName; 3 test (Completed→0, non-owner→0, InProgress→1 Abandoned) |
| `HcPortal.Tests/EnsureCanSubmitStandardTests.cs`  | Plan 02         | VERIFIED    | Exists; `ShouldEnforceSubmitTimer` Standard-late-reject + Standard-on-time-pass + Manual-late-skip |
| `HcPortal.Tests/AutoSubmitTokenRetryTests.cs`     | Plan 02         | VERIFIED    | Exists; `ShouldConsumeAutoSubmitToken` fail→not-consumed, success→consumed             |
| `HcPortal.Tests/TokenGateTests.cs`                | Plan 02         | VERIFIED    | Exists; `ShouldGateMissingStart` token-required+not-started→gate; non-token→pass       |
| `Models/CertificationManagementViewModel.cs`      | Plan 03         | VERIFIED    | `return CertificateStatus.Aktif` pada branch `validUntil == null` L59-60              |
| `HcPortal.Tests/CertificateStatusTests.cs`        | Plan 03         | VERIFIED    | `_NullValidUntil_NonPermanent_ReturnsAktif` (rewrite dari ReturnsExpired)              |
| `HcPortal.Tests/CertAlertConsistencyTests.cs`     | Plan 03         | VERIFIED    | Exists; 4 fact predicate-mirror: null-cert tak dihitung Expired/AkanExpired di tally+worklist |
| `tests/e2e/exam-taking.spec.ts`                   | Plan 03         | VERIFIED    | #8 anti-resurrection, #9 abandon-vs-graded, #11 timer Standard, #12 cert visibility (18/18 per Summary); #10 didelegasikan ke xUnit |

### Key Link Verification

| From                                    | To                                            | Via                                          | Status   | Detail                                                                                              |
|-----------------------------------------|-----------------------------------------------|----------------------------------------------|----------|-----------------------------------------------------------------------------------------------------|
| GradingService MC scoring loop          | finalByQuestion lookup (dedupe-read)          | `TryGetValue` menggantikan `FirstOrDefault`  | WIRED    | L107: `finalByQuestion.TryGetValue(q.Id, out var fr)` + L162 ET branch idem                       |
| GradingService non-essay ExecuteUpdate  | WHERE NOT IN (terminal set)                   | `S.Abandoned/Cancelled/PendingGrading`       | WIRED    | L254-257: 4 kondisi != termasuk Abandoned, Cancelled, PendingGrading                               |
| CMPController SubmitExam GroupBy        | `OrderByDescending(SubmittedAt).First()`      | final-write-wins                             | WIRED    | L1693: `g.OrderByDescending(r => r.SubmittedAt).First()`                                          |
| CMPController AbandonExam               | `ExecuteUpdateAsync` WHERE UserId && status   | atomic guarded transition (was TOCTOU)       | WIRED    | L1283-1288: WHERE `a.UserId == user.Id && (a.Status == S.InProgress \|\| S.Open)`                 |
| EnsureCanSubmitExamAsync                | `ShouldEnforceSubmitTimer` blocklist          | skip hanya Manual/null                       | WIRED    | L4514: `if (!ShouldEnforceSubmitTimer(assessment.AssessmentType)) return null;`                    |
| EnsureCanSubmitExamAsync token          | `TempData.Keep` + consume on success path     | TMR-03 peek semantics                        | WIRED    | L4546 Keep; L1762-1764 Remove hanya setelah `ShouldConsumeAutoSubmitToken(graded)`                |
| DeriveCertificateStatus validUntil==null | CertificateStatus.Aktif                      | single-source flip                           | WIRED    | L59-60: `if (validUntil == null) return CertificateStatus.Aktif;`                                 |
| AdminBase/Renewal/CDP tally consumer    | Status enum (auto-coherent via single-source) | tidak diedit — konsumsi lewat Status enum    | WIRED    | AdminBaseController L200 filter `Status==Expired\|\|AkanExpired` → null-cert auto-drop after fix  |

### Data-Flow Trace (Level 4)

| Artifact                               | Data Variable         | Source                                  | Produces Real Data | Status    |
|----------------------------------------|-----------------------|-----------------------------------------|--------------------|-----------|
| GradingService.GradeAndCompleteAsync   | `allResponses` (list) | EF ToListAsync dari DB                  | Ya (real DB query) | FLOWING   |
| GradingService `finalByQuestion`       | dedupe map            | in-memory LINQ dari `allResponses` list | Ya (dari real data)| FLOWING   |
| CMPController.SubmitExam `existingResponses` | dedupe map     | EF ToListAsync + GroupBy OrderByDesc    | Ya (real DB query) | FLOWING   |
| DeriveCertificateStatus                | `validUntil` param    | passed dari caller (DB row field)       | Ya (dari DB row)   | FLOWING   |

### Behavioral Spot-Checks

| Behavior                                         | Metode verifikasi          | Hasil                                          | Status  |
|--------------------------------------------------|----------------------------|------------------------------------------------|---------|
| `Abandoned` const = string "Abandoned"           | Grep AssessmentConstants.cs | `public const string Abandoned = "Abandoned";` | PASS    |
| MC scoring reads FINAL answer (dedupe)           | Grep GradingService.cs      | `OrderByDescending(r => r.SubmittedAt).First()` ada L90 | PASS |
| GradingService rejects Abandoned/Cancelled/PendingGrading | Grep GradingService.cs | `S.Abandoned`, `S.Cancelled`, `S.PendingGrading` di kedua branch | PASS |
| AbandonExam atomic + ownership in WHERE          | Read CMPController.cs L1283 | `ExecuteUpdateAsync` WHERE `UserId==user.Id && (InProgress\|\|Open)` | PASS |
| EnsureCanSubmit skip only Manual/null            | Grep CMPController.cs       | `ShouldEnforceSubmitTimer` blocklist L4468-4473 | PASS |
| TMR-03 token consumed only on success            | Grep CMPController.cs       | `TempData.Keep` L4546; `TempData.Remove` hanya di L1764 setelah grading sukses | PASS |
| DeriveCertificateStatus null→Aktif               | Grep CertificationManagementViewModel.cs | `return CertificateStatus.Aktif` L60 | PASS |
| Migration=false                                  | git diff Migrations/        | Output kosong (no migration files changed)     | PASS    |
| E2E #8-12 scenarios exist in spec                | Grep exam-taking.spec.ts    | "anti-resurrection", "abandon", "timer Standard", "cert visibility" ditemukan | PASS |

### Requirements Coverage

| REQ-ID | Source Plan   | Deskripsi                                                  | Status      | Bukti                                                                                     |
|--------|---------------|------------------------------------------------------------|-------------|-------------------------------------------------------------------------------------------|
| WSE-06 | 382-01, 382-02 | Nilai MC dari jawaban FINAL (dedupe last-write-wins)      | SATISFIED   | GradingService `finalByQuestion` + CMPController GroupBy OrderByDesc; GradingDedupeTests |
| WSE-07 | 382-01, 382-02 | Sesi Abandoned/Cancelled tak bisa di-resurrect jadi lulus | SATISFIED   | Guard kedua branch GradingService + SubmitExam early guard terminal set; SubmitResurrectionTests + e2e #8 |
| WSE-08 | 382-02        | Graded verdict tak ketimpa AbandonExam telat               | SATISFIED   | AbandonExam `ExecuteUpdateAsync` WHERE (InProgress\|\|Open) + rowsAffected==0; AbandonGuardTests; e2e #9 |
| WSE-09 | 382-02        | Timer Standard ditegakkan (TMR-01/02/03)                  | SATISFIED   | `ShouldEnforceSubmitTimer` blocklist (TMR-01); serverTimerExpired authoritative (TMR-02); `TempData.Keep` + consume-on-success (TMR-03); EnsureCanSubmitStandardTests; e2e #11 |
| WSE-10 | 382-02        | Token gate SaveAnswer/SubmitExam (StartedAt==null reject)  | SATISFIED   | `ShouldGateMissingStart` di kedua handler; TokenGateTests                                 |
| WSE-11 | 382-03        | Cert ValidUntil=null tampil "aktif" semua surface          | SATISFIED (kode) | `DeriveCertificateStatus` null→Aktif; CertAlertConsistencyTests; e2e #12 DB-assert. Visual rendering = human verify item |

**Catatan:** `REQUIREMENTS.md` Traceability table baris WSE-11 masih "pending" (belum di-update), sementara ROADMAP.md dan kode sudah selesai. Ini adalah inkonsistensi dokumentasi minor — bukan gap kode.

### Anti-Patterns Found

| File                     | Item                                           | Severity  | Impact                                                                                         |
|--------------------------|------------------------------------------------|-----------|-----------------------------------------------------------------------------------------------|
| `Services/GradingService.cs` L475 | `RegradeAfterEditAsync` masih pakai literal `s.Status == "Completed"` (IN-01 dari Review) | Info | Konsistensi minor; nilai string identik dengan `S.Completed`; bukan bug |
| `Controllers/CMPController.cs` | `elapsed >= allowed` dihitung ulang di 2 site (IN-02 dari Review) | Info | Risiko drift jika satu site berubah tanpa yang lain; bukan bug saat ini |
| `Services/GradingService.cs` L229-231 | Log essay-branch "sudah Completed/Menunggu Penilaian" tidak menyebut Abandoned/Cancelled (IN-03 dari Review) | Info | Operator bisa salah diagnosa log; tidak memengaruhi kebenaran kode |
| `Controllers/CMPController.cs` L1605-1747 | Answer rows tetap dipersist meski sesi concurrent menjadi terminal pasca pre-check (WR-01 dari Review) | Warning | Data tidiness — baris inert pada sesi terminal, tapi atomic grading guard di GradingService tetap blok resurrection; tidak ada correctness/security defect |

Tidak ada anti-pattern **Blocker** yang mencegah tujuan phase.

### Human Verification Required

#### 1. Dashboard Visual — Cert ValidUntil=null Konsisten di CMP/CDP/Renewal + Badge Home

**Test:** Jalankan lokal dengan `Authentication__UseActiveDirectory=false dotnet run` (http://localhost:5277, SQLBrowser aktif + `lpc:` conn bila perlu). Pastikan ada worker LULUS single-answer (Normal/PostTest) dengan sertifikat ValidUntil=null (gunakan e2e #12 atau seed; snapshot+restore DB per Seed Workflow). Kemudian:
1. Buka dashboard cert **CMP** → assert baris worker tsb tampil **"Aktif" atau "Permanen"**, BUKAN "Expired".
2. Buka **CDP** cert tally + **Renewal worklist** → assert cert null TIDAK muncul sebagai item renewal / tidak menambah ExpiredCount/AkanExpiredCount.
3. Buka **Home** (login worker tsb) → assert badge cert & notifikasi tidak menandai cert null sebagai expired (tidak undercount/kontradiktif).
4. Buka **Results** worker → assert LULUS + NomorSertifikat + PDF dapat diunduh.

**Expected:** Semua surface menampilkan "Aktif/Permanen" untuk cert tanpa expiry date. Worklist renewal tidak mengandung cert null. Badge/notif Home konsisten.

**Why human:** Rendering visual lintas 3 dashboard tidak bisa di-assert programatik secara penuh. DB-level sudah ter-otomasi: `CertAlertConsistencyTests` membuktikan predicate-mirror consumer tidak menghitung null-cert sebagai Expired/AkanExpired; e2e #12 DB-assert membuktikan `IsPassed=1 + ValidUntil IS NULL` dan CMP dashboard tidak menampilkan string "Expired" untuk cert tersebut. Yang tersisa adalah konfirmasi visual badge warna, label teks rendering, dan navigasi lintas surface yang bersifat pixel-level.

**Resume signal:** Ketik "approved" bila semua surface konsisten, atau jelaskan surface mana yang masih menampilkan "Expired"/undercount (gap → buat plan gap-closure).

---

## Gaps Summary

Tidak ada gaps yang menghalangi pencapaian tujuan phase secara kode. Satu-satunya item terbuka adalah konfirmasi visual human untuk rendering dashboard cert (WSE-11 visual layer) — yang sudah dijamin oleh DB-level test tetapi belum dispot-check secara visual di browser.

**Inkonsistensi dokumentasi minor** (tidak menghalangi):
- `REQUIREMENTS.md` Traceability baris WSE-11 masih "pending" — perlu di-flip ke "Complete" untuk sinkroni dengan ROADMAP.md yang sudah benar.

---

_Verified: 2026-06-15T04:30:00Z_
_Verifier: Claude (gsd-verifier)_
