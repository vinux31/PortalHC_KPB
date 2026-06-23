-- ============================================================
-- Phase 415.1 BUGFIX — Essay Grading Ownership (cross-package) e2e Seed
-- BUGFIX-415.1 (guard WR-02 SubmitEssayScore assignment-based)
-- ============================================================
--
-- Tujuan:
--   Repro live bug "Soal bukan milik sesi ini." pada penilaian essay untuk
--   peserta yang dapat soal dari paket milik sesi-sibling LAIN (paket di-pool
--   lintas sesi by design). Membuktikan fix Plan 01 (predikat assignment-based)
--   di controller ASLI lewat real-browser, BUKAN replica xUnit.
--
--   Struktur (RESEARCH §5 + §6):
--   GRUP A [ESSAYOWN415] Cross Package — 2 sesi sibling:
--     S1 (induk) MEMILIKI AssessmentPackage P + 1 PackageQuestion Essay (essayQ).
--     S2 (worker) PendingGrading; UPA-nya ShuffledQuestionIds MEMUAT essayQ
--        (pooled lintas-sibling) + PackageUserResponse essay pending (EssayScore NULL).
--     → /Admin/EssayGrading?sessionId={S2}: paket essayQ dimiliki S1, BUKAN S2.
--        Pra-fix = alert "Soal bukan milik sesi ini." Pasca-fix = badge "Sudah Dinilai".
--   GRUP B [ESSAYOWN415-PP] Pre Post Same (SamePackage=1) — paritas Pre/Post:
--     S3 (induk-Post) memiliki paket CLONE Pcl + essayQ_post; S4 (worker-Post)
--     PendingGrading; UPA S4 memuat essayQ_post (clone milik induk-Post) + response pending.
--
-- Klasifikasi: temporary + local-only. Prefix Title '[ESSAYOWN415]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\essay-grading-ownership-415.1-seed.sql
--   (spec Playwright menjalankan ini via db.execScript di beforeAll, dengan
--    db.backup snapshot sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [ESSAYOWN415 (cover
--   '[ESSAYOWN415]' DAN '[ESSAYOWN415-PP]') dulu, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
-- LIKE '[[]ESSAYOWN415%' = literal prefix '[ESSAYOWN415' (cover ']' + '-PP]').
-- FK-safe order: child → parent.
DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]ESSAYOWN415%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]ESSAYOWN415%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51415, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

DECLARE @t TABLE (Id INT);

-- =====================================================================
-- GRUP A — [ESSAYOWN415] Cross Package (2 sesi sibling)
-- =====================================================================
DECLARE @schedA DATETIME = GETDATE();

-- S1 induk (MEMILIKI paket)
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @t
VALUES
    (@uid, '[ESSAYOWN415] Cross Package', 'OJT', @schedA, 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL, 0, '', GETDATE(), 0, 0, 1, 0);
DECLARE @s1 INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- S2 worker (POOLED paket dari S1 — paket BUKAN miliknya)
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @t
VALUES
    (@uid, '[ESSAYOWN415] Cross Package', 'OJT', @schedA, 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL, 0, '', GETDATE(), 0, 0, 1, 0);
DECLARE @s2 INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- Paket P dimiliki S1 (induk)
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @t
VALUES (@s1, 'Paket A', 1, GETDATE());
DECLARE @pkgA INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- essayQ milik paket P (induk S1)
INSERT INTO PackageQuestions
    (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, Rubrik, MaxCharacters)
OUTPUT inserted.Id INTO @t
VALUES
    (@pkgA, '[ESSAYOWN415] Jelaskan prosedur start-up pompa sentrifugal.', 1, 10, 'Essay',
     '[ESSAYOWN415] Rubrik: priming + cek suction + buka discharge bertahap.', 2000);
DECLARE @eqA INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- UPA untuk S1 DAN S2 — keduanya ShuffledQuestionIds MEMUAT essayQ (pooled lintas-sibling)
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@s1, @pkgA, @uid, '[' + CAST(@eqA AS NVARCHAR(20)) + ']', '{}', GETDATE(), 1, 1),
    (@s2, @pkgA, @uid, '[' + CAST(@eqA AS NVARCHAR(20)) + ']', '{}', GETDATE(), 1, 1);

-- Response essay pending untuk S2 (worker) → muncul di /Admin/EssayGrading?sessionId={S2}
INSERT INTO PackageUserResponses
    (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES
    (@s2, @eqA, NULL, GETDATE(),
     '[ESSAYOWN415] Priming dulu, cek tekanan suction, lalu buka discharge bertahap.', NULL);

-- =====================================================================
-- GRUP B — [ESSAYOWN415-PP] Pre Post Same (paritas Pre/Post SamePackage)
-- =====================================================================
DECLARE @schedB DATETIME = GETDATE();

-- S3 induk-Post (MEMILIKI paket clone)
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @t
VALUES
    (@uid, '[ESSAYOWN415-PP] Pre Post Same', 'OJT', @schedB, 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL, 0, '', GETDATE(), 0, 0, 1, 1);
DECLARE @s3 INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- S4 worker-Post (POOLED paket clone dari induk-Post)
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @t
VALUES
    (@uid, '[ESSAYOWN415-PP] Pre Post Same', 'OJT', @schedB, 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL, 0, '', GETDATE(), 0, 0, 1, 1);
DECLARE @s4 INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- Paket clone dimiliki S3 (induk-Post)
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @t
VALUES (@s3, 'Paket A (Clone Post)', 1, GETDATE());
DECLARE @pkgB INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- essayQ_post milik paket clone (induk-Post S3)
INSERT INTO PackageQuestions
    (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, Rubrik, MaxCharacters)
OUTPUT inserted.Id INTO @t
VALUES
    (@pkgB, '[ESSAYOWN415-PP] Jelaskan beda Pre-Test dan Post-Test untuk renewal.', 1, 10, 'Essay',
     '[ESSAYOWN415-PP] Rubrik: ukur baseline vs hasil pelatihan.', 2000);
DECLARE @eqB INT = (SELECT TOP 1 Id FROM @t); DELETE FROM @t;

-- UPA untuk S3 DAN S4 — keduanya memuat essayQ_post (paket clone pooled)
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@s3, @pkgB, @uid, '[' + CAST(@eqB AS NVARCHAR(20)) + ']', '{}', GETDATE(), 1, 1),
    (@s4, @pkgB, @uid, '[' + CAST(@eqB AS NVARCHAR(20)) + ']', '{}', GETDATE(), 1, 1);

-- Response essay pending untuk S4 (worker-Post)
INSERT INTO PackageUserResponses
    (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES
    (@s4, @eqB, NULL, GETDATE(),
     '[ESSAYOWN415-PP] Pre-Test ukur baseline sebelum pelatihan, Post-Test ukur hasil sesudah.', NULL);

-- ---------- Layer 1 echo (informational) ----------
SELECT COUNT(*) AS EssayOwn415Seeded FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYOWN415%';
