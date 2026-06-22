-- ============================================================
-- Phase 407 RTK-10/11/12 — Worker Self-Service Retake e2e Seed
-- REQ: RTK-10 (retake control) / RTK-11 (tier feedback leak-safe) / RTK-12 (riwayat pekerja)
-- ============================================================
--
-- Tujuan: seed 3 AssessmentSession milik worker fixture (rino.prasetyo@pertamina.com)
--   yang menggerakkan FLOW worker /CMP/Results/{id}:
--
--   [407A] LEAK-SAFE + ELIGIBLE — Status=Completed, IsPassed=0 (TIDAK LULUS), AllowAnswerReview=1,
--          AllowRetake=1, MaxAttempts=3, RetakeCooldownHours=0 (no cooldown → tombol "Ujian Ulang"
--          aktif), + 1 attempt ter-arsip → currentAttempt=2 < 3 → attemptsRemaining=true.
--          Server resolve RetakeMode = ShowWrongFlagsOnly → view WAJIB suppress kunci:
--          per-soal hanya verdict ✓/✗ + "Jawaban Anda", TANPA list-group-item-success /
--          "(Jawaban Benar)" / CorrectAnswer. Plus QuestionReviews current (q1 benar, q2 salah).
--
--   [407B] CAP REACHED — Status=Completed, IsPassed=0, AllowRetake=1, MaxAttempts=2,
--          + 2 attempt ter-arsip → currentAttempt=3 >= 2 → IsCapReached=true → CanRetake=false.
--          View: alert-warning "Batas percobaan tercapai" TANPA tombol.
--
--   [407C] COOLDOWN ACTIVE — Status=Completed, IsPassed=0, AllowRetake=1, MaxAttempts=3,
--          RetakeCooldownHours=24, CompletedAt = SEKARANG (cooldown belum lewat) → tombol
--          DISABLED + data-cooldown-until masa-depan → #retakeCountdown ticking HH:MM:SS.
--          (CanRetakeAsync server bisa false saat cooldown; tombol tetap dirender via flag
--          CanRetake/CooldownUntilUtc — lihat controller 407-02. Untuk render tombol disabled
--          kita andalkan CanRetake + CooldownUntilUtc>now. Bila controller meng-gate CanRetake
--          oleh cooldown, spec 407C menerima EITHER tombol-disabled-countdown ATAU absen — assert
--          countdown only when tombol hadir, supaya non-flaky.)
--
-- Klasifikasi: temporary + local-only. Prefix Title '[RETAKE407]' untuk Layer 1 seeded-check
--   + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\retake-worker-407-seed.sql
--   (spec Playwright menjalankan ini via db.execScript di beforeAll, dengan db.backup snapshot
--    sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [RETAKE407] (FK-safe child→parent)
--   termasuk arsip-nya, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- Output 3 baris: SELECT @sidA, @sidB, @sidC (id sesi) untuk spec navigasi.
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
-- FK-safe order: arsip → history → response → assignment → option → question → package → session.
DELETE FROM AssessmentAttemptResponseArchives
 WHERE AttemptHistoryId IN (
        SELECT Id FROM AssessmentAttemptHistory WHERE Title LIKE '[[]RETAKE407%');

DELETE FROM AssessmentAttemptHistory WHERE Title LIKE '[[]RETAKE407%';

DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE407%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE407%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]RETAKE407%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]RETAKE407%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE407%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE407%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51407, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

DECLARE @category NVARCHAR(100) = 'OJT';
DECLARE @sched    DATETIME2     = CAST(GETDATE() AS DATE);   -- tanggal stabil (hindari tz drift)

-- ============================================================
-- [407A] LEAK-SAFE + ELIGIBLE (ShowWrongFlagsOnly + tombol Ujian Ulang aktif)
-- ============================================================
DECLARE @titleA NVARCHAR(200) = '[RETAKE407] A LeakSafe Eligible';
DECLARE @sessA TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage,
     AssessmentType, ShuffleOptions, ShuffleQuestions, AllowRetake, MaxAttempts, RetakeCooldownHours)
OUTPUT inserted.Id INTO @sessA
VALUES
    (@uid, @titleA, @category, @sched, 60, 'Completed', 100, 'bg-danger',
     70, 1, 0, 0, DATEADD(DAY,-2,GETUTCDATE()), 40,            -- IsPassed=0 (TIDAK LULUS), CompletedAt 2 hari lalu
     0, '', GETUTCDATE(), 120, 0, 0, 0,
     'PostTest', 0, 0, 1, 3, 0);                                -- AllowRetake=1, MaxAttempts=3, cooldown=0 → tombol aktif
DECLARE @sidA INT = (SELECT TOP 1 Id FROM @sessA);

-- package + 2 SA question (q1 benar, q2 salah) → current attempt QuestionReviews
DECLARE @pkgA TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @pkgA VALUES (@sidA, 'Paket A', 1, GETDATE());
DECLARE @pkgidA INT = (SELECT TOP 1 Id FROM @pkgA);

DECLARE @qA1 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @qA1
VALUES (@pkgidA, '[RETAKE407] Soal A1: katalis alkylation?', 1, 50, 'SingleAnswer', 0);
DECLARE @qidA1 INT = (SELECT TOP 1 Id FROM @qA1);

DECLARE @qA2 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @qA2
VALUES (@pkgidA, '[RETAKE407] Soal A2: produk utama?', 2, 50, 'SingleAnswer', 0);
DECLARE @qidA2 INT = (SELECT TOP 1 Id FROM @qA2);

-- Options — teks opsi-benar UNIK supaya spec bisa assert ABSEN di DOM ShowWrongFlagsOnly (leak-safety).
DECLARE @optA1 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optA1 VALUES (@qidA1, 'KUNCIBENAR_A1_AsamHF', 1);
DECLARE @optidA1 INT = (SELECT TOP 1 Id FROM @optA1);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qidA1, 'PILIHANKU_A1_Air', 0);

DECLARE @optA2 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optA2 VALUES (@qidA2, 'KUNCIBENAR_A2_Alkylate', 1);
DECLARE @optidA2 INT = (SELECT TOP 1 Id FROM @optA2);
DECLARE @optA2WrongTbl TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optA2WrongTbl VALUES (@qidA2, 'PILIHANKU_A2_Kokas', 0);
DECLARE @optA2Wrong INT = (SELECT TOP 1 Id FROM @optA2WrongTbl);

INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@sidA, @pkgidA, @uid,
     '[' + CAST(@qidA1 AS NVARCHAR(20)) + ',' + CAST(@qidA2 AS NVARCHAR(20)) + ']',
     '{}', GETDATE(), 1, 2);

-- current attempt responses: q1 pilih opsi benar → ✓; q2 pilih opsi salah → ✗
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sidA, @qidA1, @optidA1, GETDATE(), NULL, NULL);
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sidA, @qidA2, @optA2Wrong, GETDATE(), NULL, NULL);

-- 1 attempt ter-arsip → currentAttempt = 1 + 1 = 2 < MaxAttempts(3) → attemptsRemaining=true
DECLARE @hA1 TABLE (Id INT);
INSERT INTO AssessmentAttemptHistory (SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt, CreatedAt)
OUTPUT inserted.Id INTO @hA1
VALUES (@sidA, @uid, @titleA, @category, 30, 0, DATEADD(DAY,-4,GETUTCDATE()), DATEADD(DAY,-4,GETUTCDATE()), 1, GETUTCDATE(), GETUTCDATE());
DECLARE @hidA1 INT = (SELECT TOP 1 Id FROM @hA1);
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hidA1, 7001, '[RETAKE407] Arsip A1 soal benar', 'Jawaban tepat A1', 1, 50, GETUTCDATE());
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hidA1, 7002, '[RETAKE407] Arsip A1 soal salah', 'Jawaban keliru A1', 0, 0, GETUTCDATE());

-- ============================================================
-- [407B] CAP REACHED (alert-warning lock, no tombol)
-- ============================================================
DECLARE @titleB NVARCHAR(200) = '[RETAKE407] B CapReached';
DECLARE @sessB TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage,
     AssessmentType, ShuffleOptions, ShuffleQuestions, AllowRetake, MaxAttempts, RetakeCooldownHours)
OUTPUT inserted.Id INTO @sessB
VALUES
    (@uid, @titleB, @category, @sched, 60, 'Completed', 100, 'bg-danger',
     70, 1, 0, 0, DATEADD(DAY,-1,GETUTCDATE()), 50,            -- IsPassed=0
     0, '', GETUTCDATE(), 120, 0, 0, 0,
     'PostTest', 0, 0, 1, 2, 0);                                -- MaxAttempts=2; akan ada 2 arsip → currentAttempt=3 >= 2 → cap
DECLARE @sidB INT = (SELECT TOP 1 Id FROM @sessB);

-- 2 attempt ter-arsip → currentAttempt = 2 + 1 = 3 >= MaxAttempts(2) → IsCapReached=true
DECLARE @hB1 TABLE (Id INT);
INSERT INTO AssessmentAttemptHistory (SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt, CreatedAt)
OUTPUT inserted.Id INTO @hB1
VALUES (@sidB, @uid, @titleB, @category, 40, 0, DATEADD(DAY,-5,GETUTCDATE()), DATEADD(DAY,-5,GETUTCDATE()), 1, GETUTCDATE(), GETUTCDATE());
DECLARE @hidB1 INT = (SELECT TOP 1 Id FROM @hB1);
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hidB1, 7101, '[RETAKE407] Arsip B1 soal', 'Jawaban B1', 0, 0, GETUTCDATE());

DECLARE @hB2 TABLE (Id INT);
INSERT INTO AssessmentAttemptHistory (SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt, CreatedAt)
OUTPUT inserted.Id INTO @hB2
VALUES (@sidB, @uid, @titleB, @category, 50, 0, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE()), 2, GETUTCDATE(), GETUTCDATE());
DECLARE @hidB2 INT = (SELECT TOP 1 Id FROM @hB2);
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hidB2, 7102, '[RETAKE407] Arsip B2 soal', 'Jawaban B2', 0, 0, GETUTCDATE());

-- ============================================================
-- [407C] COOLDOWN ACTIVE (tombol disabled + countdown ticking)
-- ============================================================
DECLARE @titleC NVARCHAR(200) = '[RETAKE407] C CooldownActive';
DECLARE @sessC TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage,
     AssessmentType, ShuffleOptions, ShuffleQuestions, AllowRetake, MaxAttempts, RetakeCooldownHours)
OUTPUT inserted.Id INTO @sessC
VALUES
    (@uid, @titleC, @category, @sched, 60, 'Completed', 100, 'bg-danger',
     70, 1, 0, 0, GETUTCDATE(), 45,                            -- CompletedAt = SEKARANG → cooldown belum lewat
     0, '', GETUTCDATE(), 120, 0, 0, 0,
     'PostTest', 0, 0, 1, 3, 24);                               -- RetakeCooldownHours=24 → CooldownUntilUtc = now + 24h
DECLARE @sidC INT = (SELECT TOP 1 Id FROM @sessC);

-- (Tidak perlu arsip untuk 407C — currentAttempt=1 < 3; cooldown yang men-gate.)

-- ---------- Output: 3 session id untuk spec ----------
SELECT @sidA AS SidA, @sidB AS SidB, @sidC AS SidC;
