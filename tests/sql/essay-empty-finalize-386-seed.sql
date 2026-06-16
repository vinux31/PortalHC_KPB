-- ============================================================
-- Phase 386 PXF-04 (F-04) — Essay Empty Finalize e2e Seed
-- REQ: PXF-04 (essay-empty-finalize-386.spec.ts)
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession Status='Menunggu Penilaian' (PendingGrading) +
--   HasManualGrading=1, dengan rantai package: 1 AssessmentPackage + 2 PackageQuestion
--   (QuestionType='Essay') + 1 UserPackageAssignment (ShuffledQuestionIds JSON berisi
--   kedua essay question id) + 2 PackageUserResponse:
--     - Essay #1 (TERISI, EssayScore=NULL → pending sah, bisa dinilai)
--     - Essay #2 (DIKOSONGKAN: TextAnswer=NULL, EssayScore=NULL → SESUDAH fix PXF-04
--       BUKAN pending → "Selesaikan Penilaian" tetap muncul, finalize jalan, essay
--       kosong kontribusi 0; bukan error "Jawaban tidak ditemukan").
--
--   Fixture ini menggerakkan PXF-04 e2e round-trip:
--     page per-worker /Admin/EssayGrading → "Simpan Skor" essay terisi (AJAX)
--     → "Selesaikan Penilaian" → sukses (success:true), essay kosong auto-0.
--   GenerateCertificate=0 (hindari ketergantungan PassPercentage/cert-gen pada e2e
--   finalize — fokus pada parity pending-count + upsert + status-guard PXF-04).
--
-- Klasifikasi: temporary + local-only. Prefix Title '[ESSAYEMPTY386]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\essay-empty-finalize-386-seed.sql
--   (spec Playwright menjalankan ini via db.execScript di beforeAll, dengan db.backup
--    snapshot sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [ESSAYEMPTY386] (FK-safe
--   child→parent) dulu, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]ESSAYEMPTY386%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]ESSAYEMPTY386%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51387, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- ---------- 1) AssessmentSession (PendingGrading, essay-pending) ----------
DECLARE @sessions TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @sessions
VALUES
    (@uid, '[ESSAYEMPTY386] Essay Kosong Finalize PXF-04', 'OJT', GETDATE(), 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 0, NULL, GETDATE(), NULL,
     0, '', GETDATE(), 0, 0, 1, 0);
DECLARE @sid INT = (SELECT TOP 1 Id FROM @sessions);

-- ---------- 2) AssessmentPackage ----------
DECLARE @packages TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @packages
VALUES (@sid, 'Paket A', 1, GETDATE());
DECLARE @pkgid INT = (SELECT TOP 1 Id FROM @packages);

-- ---------- 3) PackageQuestion #1 (Essay terisi) ----------
DECLARE @q1 TABLE (Id INT);
INSERT INTO PackageQuestions
    (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, Rubrik, MaxCharacters)
OUTPUT inserted.Id INTO @q1
VALUES
    (@pkgid, '[ESSAYEMPTY386] Jelaskan fungsi unit CDU pada kilang.', 1, 10, 'Essay',
     '[ESSAYEMPTY386] Rubrik: distilasi atmosferik memisahkan fraksi crude berdasarkan titik didih.', 2000);
DECLARE @qid1 INT = (SELECT TOP 1 Id FROM @q1);

-- ---------- 4) PackageQuestion #2 (Essay DIKOSONGKAN peserta) ----------
DECLARE @q2 TABLE (Id INT);
INSERT INTO PackageQuestions
    (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, Rubrik, MaxCharacters)
OUTPUT inserted.Id INTO @q2
VALUES
    (@pkgid, '[ESSAYEMPTY386] Sebutkan produk utama unit FCC.', 2, 10, 'Essay',
     '[ESSAYEMPTY386] Rubrik: gasoline beroktan tinggi + LPG + LCO.', 2000);
DECLARE @qid2 INT = (SELECT TOP 1 Id FROM @q2);

-- ---------- 5) UserPackageAssignment ----------
-- ShuffledQuestionIds = JSON array berisi kedua essay question id (di-parse GetShuffledQuestionIds()).
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@sid, @pkgid, @uid,
     '[' + CAST(@qid1 AS NVARCHAR(20)) + ',' + CAST(@qid2 AS NVARCHAR(20)) + ']',
     '{}', GETDATE(), 1, 2);

-- ---------- 6) PackageUserResponse #1 (essay terisi, ungraded) ----------
INSERT INTO PackageUserResponses
    (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES
    (@sid, @qid1, NULL, GETDATE(),
     '[ESSAYEMPTY386] CDU memisahkan crude oil menjadi fraksi naphtha, kerosene, diesel, dan residu.',
     NULL);

-- ---------- 7) PackageUserResponse #2 (essay DIKOSONGKAN: TextAnswer=NULL, EssayScore=NULL) ----------
-- SESUDAH fix PXF-04: baris kosong (TextAnswer NULL/whitespace) BUKAN pending → finalize jalan.
INSERT INTO PackageUserResponses
    (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES
    (@sid, @qid2, NULL, GETDATE(), NULL, NULL);

-- ---------- Layer 1 echo (informational) ----------
SELECT COUNT(*) AS EssayEmpty386Seeded FROM AssessmentSessions WHERE Title LIKE '[[]ESSAYEMPTY386]%';
