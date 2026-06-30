-- ============================================================
-- Phase 321 EDIT — Self-seed sesi Completed PASS untuk e2e Edit Jawaban Peserta
-- REQ: EDIT-01/03/06/07/10 (spec edit-peserta-answers.spec.ts).
-- ============================================================
--
-- Tujuan: seed 1 AssessmentSession yang LOLOS gate AssessmentEditEligibility.IsEditableAsync:
--   Status='Completed', IsManualEntry=0, BUKAN Proton Tahun 3, punya UserPackageAssignment,
--   plus >=1 soal QuestionType='MultipleChoice' yang id-nya masuk JSON ShuffledQuestionIds
--   (GET EditPesertaAnswers hanya render soal yang ada di ShuffledQuestionIds).
--
-- Klasifikasi: temporary + local-only. Prefix Title '[EDIT321]' untuk seeded-check + cleanup.
--   JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\edit-peserta-321-seed.sql
--   (spec Playwright menjalankan ini via db.execScript di beforeAll, dengan db.backup snapshot
--    sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [EDIT321] (FK-safe child->parent).
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- Output 1 baris: SELECT @sid (id sesi) untuk run manual / referensi.
-- WAJIB QuestionType='MultipleChoice' (bukan 'SingleAnswer') agar render & grade benar.
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run; FK-safe child->parent) ----------
DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]EDIT321%');
DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]EDIT321%');
DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]EDIT321%');
DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]EDIT321%');
DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]EDIT321%');
DELETE FROM AssessmentSessions WHERE Title LIKE '[[]EDIT321%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51321, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

DECLARE @title    NVARCHAR(200) = '[EDIT321] Edit Jawaban Peserta';
DECLARE @category NVARCHAR(100) = 'OJT';
DECLARE @sched    DATETIME2     = CAST(GETDATE() AS DATE);

-- ---------- Sesi Completed PASS (eligible edit) ----------
DECLARE @sess TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage,
     AssessmentType, ShuffleOptions, ShuffleQuestions, AllowRetake, MaxAttempts, RetakeCooldownHours)
OUTPUT inserted.Id INTO @sess
VALUES
    (@uid, @title, @category, @sched, 60, 'Completed', 100, 'bg-success',
     50, 1, 1, 1, GETUTCDATE(), 100,
     0, '', GETUTCDATE(), 120, 0, 0, 0,
     'PostTest', 0, 0, 0, 1, 0);
DECLARE @sid INT = (SELECT TOP 1 Id FROM @sess);

-- ---------- Paket + 2 soal MultipleChoice ----------
DECLARE @pkg TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @pkg VALUES (@sid, 'Paket Edit', 1, GETDATE());
DECLARE @pkgid INT = (SELECT TOP 1 Id FROM @pkg);

-- Soal Q1
DECLARE @q1 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @q1
VALUES (@pkgid, '[EDIT321] Soal Q1 (MC)', 1, 50, 'MultipleChoice', 0);
DECLARE @qid1 INT = (SELECT TOP 1 Id FROM @q1);
DECLARE @o1c TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @o1c VALUES (@qid1, 'EDIT321_Q1_Benar', 1);
DECLARE @oid1c INT = (SELECT TOP 1 Id FROM @o1c);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qid1, 'EDIT321_Q1_Salah', 0);

-- Soal Q2
DECLARE @q2 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @q2
VALUES (@pkgid, '[EDIT321] Soal Q2 (MC)', 2, 50, 'MultipleChoice', 0);
DECLARE @qid2 INT = (SELECT TOP 1 Id FROM @q2);
DECLARE @o2c TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @o2c VALUES (@qid2, 'EDIT321_Q2_Benar', 1);
DECLARE @oid2c INT = (SELECT TOP 1 Id FROM @o2c);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qid2, 'EDIT321_Q2_Salah', 0);

-- ---------- UserPackageAssignment (ShuffledQuestionIds berisi kedua id soal) ----------
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@sid, @pkgid, @uid,
     '[' + CAST(@qid1 AS NVARCHAR(20)) + ',' + CAST(@qid2 AS NVARCHAR(20)) + ']',
     '{}', GETDATE(), 1, 2);

-- ---------- Responses (jawab opsi benar → sesi PASS konsisten) ----------
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sid, @qid1, @oid1c, GETDATE(), NULL, NULL);
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sid, @qid2, @oid2c, GETDATE(), NULL, NULL);

-- ---------- Output: session id untuk spec / run manual ----------
SELECT @sid AS Edit321SessionId;
