-- ============================================================
-- Phase 408 RTK-14 — Lifecycle e2e Seed (gagal → Ujian Ulang → lulus → 1 cert)
-- REQ: RTK-14 (capstone lifecycle: bukti visual akhir fitur ujian ulang berfungsi
--      dari KEGAGALAN sampai TERBITNYA SERTIFIKAT, di real-browser).
-- ============================================================
--
-- Tujuan: seed 1 AssessmentSession GAGAL milik worker fixture (rino.prasetyo@pertamina.com)
--   yang menggerakkan LIFECYCLE penuh di /CMP/Results/{id} → StartExam → grade → cert:
--
--   [RETAKE408] Lifecycle Fail-to-Pass — Status=Completed, IsPassed=0 (MULAI GAGAL, Score rendah),
--          AllowAnswerReview=1, AllowRetake=1, MaxAttempts=3, RetakeCooldownHours=0 (no cooldown →
--          tombol "Ujian Ulang" aktif langsung — Pitfall 5), GenerateCertificate=1 (cert WAJIB
--          enabled — Pitfall 2; berbeda dari 407 yang pakai 0), PassPercentage rendah (50) supaya
--          jawab semua-benar → Score 100 ≥ 50 → LULUS. 0 arsip era-retake (currentAttempt=1) +
--          MaxAttempts=3 → eligible. CompletedAt 2 hari lalu (gate cooldown tak relevan).
--          Server resolve RetakeMode = ShowWrongFlagsOnly (sesi gagal + sisa-percobaan +
--          AllowAnswerReview) → Results pra-retake render verdict ✓/✗ TANPA kunci jawaban.
--
--   Paket soal MC dengan JAWABAN-BENAR DETERMINISTIK: tiap soal punya satu PackageOption
--   IsCorrect=1 dengan OptionText UNIK ber-prefix 'BENAR408_Qn_*' → spec e2e pilih by-TEXT
--   (shuffle-safe, Shuffle opsi default ON v27.0). Plus current responses (prior failed attempt)
--   memilih opsi SALAH → Results pra-retake menampilkan cross (✗) verdict.
--
--   ALUR DIUJI: Results(gagal) → klik #btnRetake → modal → POST RetakeExam (ExecuteAsync reset:
--     hapus responses+assignment + arsip snapshot, Status→Open, clear token) → StartExam (fresh,
--     worker generate ulang assignment) → jawab BENAR semua → submit (GradeAndCompleteAsync grade
--     dari DB → Score 100 ≥ 50 → IsPassed=1 → GenerateCertificate → terbit NomorSertifikat
--     `KPB/{seq:D3}/{RomanMonth}/{year}`) → Results LULUS + nomor sertifikat.
--
-- Klasifikasi: temporary + local-only. Prefix Title '[RETAKE408]' untuk Layer 1 seeded-check
--   + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\retake-lifecycle-408-seed.sql
--   (spec Playwright menjalankan ini via db.execScript di beforeAll, dengan db.backup snapshot
--    sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [RETAKE408] (FK-safe child→parent)
--   termasuk arsip-nya, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- Output 1 baris: SELECT @sidLife (id sesi) untuk run manual / referensi.
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
-- FK-safe order: arsip → history → response → assignment → option → question → package → session.
DELETE FROM AssessmentAttemptResponseArchives
 WHERE AttemptHistoryId IN (
        SELECT Id FROM AssessmentAttemptHistory WHERE Title LIKE '[[]RETAKE408%');

DELETE FROM AssessmentAttemptHistory WHERE Title LIKE '[[]RETAKE408%';

DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE408%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE408%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]RETAKE408%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]RETAKE408%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE408%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]RETAKE408%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51408, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

DECLARE @category NVARCHAR(100) = 'OJT';
DECLARE @sched    DATETIME2     = CAST(GETDATE() AS DATE);   -- tanggal stabil (hindari tz drift)

-- ============================================================
-- [RETAKE408] Lifecycle Fail-to-Pass (gagal → eligible retake → lulus → cert)
-- ============================================================
DECLARE @titleLife NVARCHAR(200) = '[RETAKE408] Lifecycle Fail-to-Pass';
DECLARE @sessLife TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage,
     AssessmentType, ShuffleOptions, ShuffleQuestions, AllowRetake, MaxAttempts, RetakeCooldownHours)
OUTPUT inserted.Id INTO @sessLife
VALUES
    (@uid, @titleLife, @category, @sched, 60, 'Completed', 100, 'bg-danger',
     50, 1, 1, 0, DATEADD(DAY,-2,GETUTCDATE()), 40,             -- PassPercentage=50, GenerateCertificate=1, IsPassed=0 (GAGAL), CompletedAt 2 hari lalu
     0, '', GETUTCDATE(), 120, 0, 0, 0,                          -- AccessToken='' + IsTokenRequired=0 (hindari token gate StartExam re-entry)
     'PostTest', 1, 0, 1, 3, 0);                                 -- PostTest, ShuffleOptions=1, AllowRetake=1, MaxAttempts=3, cooldown=0 → tombol aktif + cert path
DECLARE @sidLife INT = (SELECT TOP 1 Id FROM @sessLife);

-- ---------- Paket + 3 soal MC jawaban-benar deterministik ----------
DECLARE @pkgLife TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @pkgLife VALUES (@sidLife, 'Paket Lifecycle', 1, GETDATE());
DECLARE @pkgidLife INT = (SELECT TOP 1 Id FROM @pkgLife);

-- Soal Q1 — marker 'Q1' di QuestionText; opsi benar 'BENAR408_Q1_AsamHF'
DECLARE @qL1 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @qL1
VALUES (@pkgidLife, '[RETAKE408] Soal Q1: katalis alkylation yang benar?', 1, 34, 'MultipleChoice', 0);  -- FIX: produk pakai "MultipleChoice" (label UI "Single Answer"); "SingleAnswer" bukan QuestionType valid → grade switch skip → 0%
DECLARE @qidL1 INT = (SELECT TOP 1 Id FROM @qL1);

DECLARE @optL1 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optL1 VALUES (@qidL1, 'BENAR408_Q1_AsamHF', 1);   -- jawaban BENAR Q1 (spec pilih by-text)
DECLARE @optidL1 INT = (SELECT TOP 1 Id FROM @optL1);
DECLARE @optL1WrongTbl TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optL1WrongTbl VALUES (@qidL1, 'SALAH408_Q1_Air', 0);
DECLARE @optL1Wrong INT = (SELECT TOP 1 Id FROM @optL1WrongTbl);

-- Soal Q2 — marker 'Q2'; opsi benar 'BENAR408_Q2_Alkylate'
DECLARE @qL2 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @qL2
VALUES (@pkgidLife, '[RETAKE408] Soal Q2: produk utama proses ini?', 2, 33, 'MultipleChoice', 0);  -- FIX: produk pakai "MultipleChoice" (label UI "Single Answer"); "SingleAnswer" bukan QuestionType valid → grade switch skip → 0%
DECLARE @qidL2 INT = (SELECT TOP 1 Id FROM @qL2);

DECLARE @optL2 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optL2 VALUES (@qidL2, 'BENAR408_Q2_Alkylate', 1);  -- jawaban BENAR Q2
DECLARE @optidL2 INT = (SELECT TOP 1 Id FROM @optL2);
DECLARE @optL2WrongTbl TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optL2WrongTbl VALUES (@qidL2, 'SALAH408_Q2_Kokas', 0);
DECLARE @optL2Wrong INT = (SELECT TOP 1 Id FROM @optL2WrongTbl);

-- Soal Q3 — marker 'Q3'; opsi benar 'BENAR408_Q3_Isobutana'
DECLARE @qL3 TABLE (Id INT);
INSERT INTO PackageQuestions (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, MaxCharacters)
OUTPUT inserted.Id INTO @qL3
VALUES (@pkgidLife, '[RETAKE408] Soal Q3: umpan reaksi yang tepat?', 3, 33, 'MultipleChoice', 0);  -- FIX: produk pakai "MultipleChoice" (label UI "Single Answer"); "SingleAnswer" bukan QuestionType valid → grade switch skip → 0%
DECLARE @qidL3 INT = (SELECT TOP 1 Id FROM @qL3);

DECLARE @optL3 TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optL3 VALUES (@qidL3, 'BENAR408_Q3_Isobutana', 1); -- jawaban BENAR Q3
DECLARE @optidL3 INT = (SELECT TOP 1 Id FROM @optL3);
DECLARE @optL3WrongTbl TABLE (Id INT);
INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
OUTPUT inserted.Id INTO @optL3WrongTbl VALUES (@qidL3, 'SALAH408_Q3_Nitrogen', 0);
DECLARE @optL3Wrong INT = (SELECT TOP 1 Id FROM @optL3WrongTbl);

-- ---------- UserPackageAssignment (kondisi awal Results konsisten) ----------
-- (RetakeExam→StartExam akan hapus assignment & worker generate ulang saat StartExam;
--  cukup pastikan kondisi awal Results konsisten + soal current ada.)
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@sidLife, @pkgidLife, @uid,
     '[' + CAST(@qidL1 AS NVARCHAR(20)) + ',' + CAST(@qidL2 AS NVARCHAR(20)) + ',' + CAST(@qidL3 AS NVARCHAR(20)) + ']',
     '{}', GETDATE(), 1, 3);

-- ---------- Current responses (prior failed attempt — semua SALAH → ✗ verdict pra-retake) ----------
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sidLife, @qidL1, @optL1Wrong, GETDATE(), NULL, NULL);
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sidLife, @qidL2, @optL2Wrong, GETDATE(), NULL, NULL);
INSERT INTO PackageUserResponses (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES (@sidLife, @qidL3, @optL3Wrong, GETDATE(), NULL, NULL);

-- 0 arsip era-retake → currentAttempt=1 < MaxAttempts(3) → eligible retake (lifecycle bersih single-pass).

-- ---------- Output: session id untuk spec / run manual ----------
SELECT @sidLife AS SidFailed;
