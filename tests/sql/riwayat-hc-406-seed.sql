-- ============================================================
-- Phase 406 RTK-08 — Riwayat Percobaan HC modal e2e Seed
-- REQ: RTK-08 (FLOW 406 riwayat-hc)
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession Status='Completed' (current attempt LIVE renderable) +
--   rantai package minimal (1 AssessmentPackage + 2 PackageQuestion SA + 1 UserPackageAssignment
--   + 2 PackageUserResponse) sehingga RetakeArchiveBuilder.Build(0,...) menghasilkan baris
--   per-soal untuk "Percobaan saat ini", PLUS 2 attempt TER-ARSIP (AssessmentAttemptHistory)
--   dengan AssessmentAttemptResponseArchive yang menutup semua case render modal riwayat:
--     - baris BENAR (IsCorrect=1) + baris SALAH (IsCorrect=0)
--     - baris ESSAY-PENDING (IsCorrect=NULL → status muted "—"/Menunggu, BUKAN ✗)
--     - baris dengan AnswerText payload XSS '<script>...' → harus ter-encode (inert)
--
--   Fixture ini menggerakkan FLOW 406 e2e riwayat-hc-406:
--     dropdown "Riwayat Percobaan" → #riwayatPercobaanModal lazy-fetch _RiwayatPercobaan
--     → accordion per-attempt (terbaru dulu, current di-badge "Percobaan saat ini")
--     → tabel per-soal (No/Soal/Jawaban/Status/Skor) tri-state ✓/✗/—.
--
-- Klasifikasi: temporary + local-only. Prefix Title '[RIWAYAT406]' untuk Layer 1 seeded-check
--   + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\riwayat-hc-406-seed.sql
--   (spec Playwright FLOW 406 menjalankan ini via db.execScript di beforeAll, dengan db.backup
--    snapshot sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [RIWAYAT406] (FK-safe child→parent)
--   termasuk arsip-nya, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
-- FK-safe order: arsip → history → response → assignment → option → question → package → session.
DELETE FROM AssessmentAttemptResponseArchives
 WHERE AttemptHistoryId IN (
        SELECT Id FROM AssessmentAttemptHistory WHERE Title LIKE '[[]RIWAYAT406%');

DELETE FROM AssessmentAttemptHistory WHERE Title LIKE '[[]RIWAYAT406%';

DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]RIWAYAT406%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]RIWAYAT406%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51406, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

DECLARE @title    NVARCHAR(200) = '[RIWAYAT406] Riwayat Percobaan RTK-08';
DECLARE @category NVARCHAR(100) = 'OJT';
DECLARE @sched    DATETIME2     = CAST(GETDATE() AS DATE);   -- tanggal stabil (hindari tz drift)

-- ---------- 1) AssessmentSession (Completed → trigger render + current attempt live) ----------
DECLARE @sessions TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage,
     AssessmentType, ShuffleOptions, ShuffleQuestions, AllowRetake, MaxAttempts, RetakeCooldownHours)
OUTPUT inserted.Id INTO @sessions
VALUES
    (@uid, @title, @category, @sched, 60, 'Completed', 100, 'bg-primary',
     70, 1, 0, 1, GETDATE(), 100,
     0, '', GETDATE(), 120, 0, 0, 0,
     'PostTest', 0, 0, 1, 3, 24);
DECLARE @sid INT = (SELECT TOP 1 Id FROM @sessions);

-- ---------- 2) AssessmentPackage ----------
DECLARE @packages TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @packages
VALUES (@sid, 'Paket A', 1, GETDATE());
DECLARE @pkgid INT = (SELECT TOP 1 Id FROM @packages);

-- ---------- 3) PackageQuestion (current attempt — 2 SA) ----------
-- MaxCharacters NOT NULL (default 0 untuk non-essay).
DECLARE @q1 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @q1
VALUES (@pkgid, '[RIWAYAT406] Soal current 1: katalis alkylation?', 1, 50, 'SingleAnswer', 0);
DECLARE @qid1 INT = (SELECT TOP 1 Id FROM @q1);

DECLARE @q2 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @q2
VALUES (@pkgid, '[RIWAYAT406] Soal current 2: produk utama?', 2, 50, 'SingleAnswer', 0);
DECLARE @qid2 INT = (SELECT TOP 1 Id FROM @q2);

-- Options (1 benar per soal) — diperlukan RetakeArchiveBuilder untuk verdict current.
DECLARE @opt1 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @opt1
VALUES (@qid1, 'Asam HF', 1);
DECLARE @optid1 INT = (SELECT TOP 1 Id FROM @opt1);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qid1, 'Air', 0);

DECLARE @opt2 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @opt2
VALUES (@qid2, 'Alkylate', 1);
DECLARE @optid2 INT = (SELECT TOP 1 Id FROM @opt2);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect) VALUES (@qid2, 'Kokas', 0);

-- ---------- 4) UserPackageAssignment (ShuffledQuestionIds JSON) ----------
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@sid, @pkgid, @uid,
     '[' + CAST(@qid1 AS NVARCHAR(20)) + ',' + CAST(@qid2 AS NVARCHAR(20)) + ']',
     '{}', GETDATE(), 1, 2);

-- ---------- 5) PackageUserResponse (current attempt — q1 benar, q2 salah) ----------
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sid, @qid1, @optid1, GETDATE(), NULL, NULL);   -- pilih opsi benar → ✓
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sid, @qid2, (SELECT TOP 1 Id FROM PackageOptions WHERE PackageQuestionId = @qid2 AND IsCorrect = 0), GETDATE(), NULL, NULL); -- opsi salah → ✗

-- ---------- 6) Arsip: 2 attempt ter-arsip (AttemptNumber 1 & 2) ----------
-- history keyed by (UserId, Title, Category) IDENTIK session (anti-konflasi Pitfall 3).
DECLARE @h1 TABLE (Id INT);
INSERT INTO AssessmentAttemptHistory (SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt, CreatedAt)
OUTPUT inserted.Id INTO @h1
VALUES (@sid, @uid, @title, @category, 40, 0, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE()), 1, GETUTCDATE(), GETUTCDATE());
DECLARE @hid1 INT = (SELECT TOP 1 Id FROM @h1);

DECLARE @h2 TABLE (Id INT);
INSERT INTO AssessmentAttemptHistory (SessionId, UserId, Title, Category, Score, IsPassed, StartedAt, CompletedAt, AttemptNumber, ArchivedAt, CreatedAt)
OUTPUT inserted.Id INTO @h2
VALUES (@sid, @uid, @title, @category, 60, 0, DATEADD(DAY,-2,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE()), 2, GETUTCDATE(), GETUTCDATE());
DECLARE @hid2 INT = (SELECT TOP 1 Id FROM @h2);

-- Attempt 1 arsip: 1 benar + 1 salah + 1 XSS-payload (encoded-proof).
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hid1, 9001, '[RIWAYAT406] Arsip A1 soal benar', 'Jawaban tepat A', 1, 50, GETUTCDATE());
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hid1, 9002, '[RIWAYAT406] Arsip A1 soal salah', 'Jawaban keliru A', 0, 0, GETUTCDATE());
-- XSS payload di AnswerText — partial harus @-encode jadi teks literal (script TIDAK eksekusi).
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hid1, 9003, '[RIWAYAT406] Arsip A1 soal XSS', '<script>window.__riwayatXss406=1</script>', 0, 0, GETUTCDATE());

-- Attempt 2 arsip: 1 benar + 1 ESSAY-PENDING (IsCorrect NULL → "—"/Menunggu, bukan ✗).
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hid2, 9004, '[RIWAYAT406] Arsip A2 soal benar', 'Jawaban tepat B', 1, 50, GETUTCDATE());
INSERT INTO AssessmentAttemptResponseArchives (AttemptHistoryId, PackageQuestionId, QuestionText, AnswerText, IsCorrect, AwardedScore, ArchivedAt)
VALUES (@hid2, 9005, '[RIWAYAT406] Arsip A2 soal essay', 'Uraian essay menunggu penilaian.', NULL, 0, GETUTCDATE());

-- ---------- Layer 1 echo (informational) ----------
SELECT COUNT(*) AS Riwayat406Seeded FROM AssessmentSessions WHERE Title LIKE '[[]RIWAYAT406]%';
