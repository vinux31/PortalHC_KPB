-- ============================================================
-- Phase 384 UIG-04 — Monitoring Essay Grading UI Refactor e2e Seed
-- REQ: UIG-04 (FLOW 384)
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession Status='Menunggu Penilaian' (PendingGrading) +
--   HasManualGrading=1, dengan rantai package lengkap: 1 AssessmentPackage +
--   1 PackageQuestion (QuestionType='Essay') + 1 UserPackageAssignment
--   (ShuffledQuestionIds JSON berisi essay question id) + 1 PackageUserResponse
--   (TextAnswer terisi, EssayScore=NULL → muncul "belum dinilai").
--
--   Fixture ini menggerakkan FLOW 384 e2e UIG-04 round-trip:
--     tabel worker-list (badge 🟡 "{N} belum dinilai") → "Tinjau Essay"
--     → page per-worker /Admin/EssayGrading → "Simpan Skor" (AJAX)
--     → "Selesaikan Penilaian" → state "Selesai" in-place (D-09).
--   GenerateCertificate=1 + worker mengisi skor penuh (>= PassPercentage) →
--   FinalizeEssayGrading menerbitkan NomorSertifikat → reopening page = READ-ONLY
--   (D-10) + badge monitoring 🟢 "Selesai". Cert-gen di FinalizeEssayGrading
--   ter-try/catch (AssessmentAdminController:3631-3644) → tak bikin finalize 500.
--
-- Klasifikasi: temporary + local-only. Prefix Title '[ESSAY384]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\essay-grading-384-seed.sql
--   (spec Playwright FLOW 384 menjalankan ini via db.execScript di beforeAll,
--    dengan db.backup snapshot sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [ESSAY384] (FK-safe
--   child→parent) dulu, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (worker fixture).
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
-- LIKE '[[]ESSAY384%' = literal prefix '[ESSAY384' (cover '[ESSAY384] ...').
-- FK-safe order: child → parent (PackageUserResponses → UserPackageAssignments →
-- PackageOptions → PackageQuestions → AssessmentPackages → AssessmentSessions).
DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]ESSAY384%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]ESSAY384%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51384, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- ---------- 1) AssessmentSession (PendingGrading, essay-pending) ----------
DECLARE @sessions TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @sessions
VALUES
    (@uid, '[ESSAY384] Penilaian Essay UIG-04', 'OJT', GETDATE(), 60, 'Menunggu Penilaian', 100, 'bg-primary',
     70, 1, 1, NULL, GETDATE(), NULL,
     0, '', GETDATE(), 0, 0, 1, 0);
DECLARE @sid INT = (SELECT TOP 1 Id FROM @sessions);

-- ---------- 2) AssessmentPackage ----------
DECLARE @packages TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @packages
VALUES (@sid, 'Paket A', 1, GETDATE());
DECLARE @pkgid INT = (SELECT TOP 1 Id FROM @packages);

-- ---------- 3) PackageQuestion (Essay) ----------
-- [Order] = reserved word → bracketed. ScoreValue=10 (MaxScore=10 → e2e isi 10 = 100% lulus → cert).
DECLARE @questions TABLE (Id INT);
INSERT INTO PackageQuestions
    (AssessmentPackageId, QuestionText, [Order], ScoreValue, QuestionType, Rubrik, MaxCharacters)
OUTPUT inserted.Id INTO @questions
VALUES
    (@pkgid, '[ESSAY384] Jelaskan proses alkylation pada unit kilang.', 1, 10, 'Essay',
     '[ESSAY384] Rubrik: sebut isobutane + olefin + katalis asam (HF/H2SO4) + produk alkylate oktan tinggi.', 2000);
DECLARE @qid INT = (SELECT TOP 1 Id FROM @questions);

-- ---------- 4) UserPackageAssignment ----------
-- ShuffledQuestionIds = JSON array berisi essay question id (mis. '[123]') — di-parse GetShuffledQuestionIds().
INSERT INTO UserPackageAssignments
    (AssessmentSessionId, AssessmentPackageId, UserId, ShuffledQuestionIds,
     ShuffledOptionIdsPerQuestion, AssignedAt, IsCompleted, SavedQuestionCount)
VALUES
    (@sid, @pkgid, @uid, '[' + CAST(@qid AS NVARCHAR(20)) + ']', '{}', GETDATE(), 1, 1);

-- ---------- 5) PackageUserResponse (essay submitted, ungraded) ----------
INSERT INTO PackageUserResponses
    (AssessmentSessionId, PackageQuestionId, PackageOptionId, SubmittedAt, TextAnswer, EssayScore)
VALUES
    (@sid, @qid, NULL, GETDATE(),
     '[ESSAY384] Alkylation menggabungkan isobutane dengan olefin memakai katalis asam menghasilkan alkylate beroktan tinggi.',
     NULL);

-- ---------- Layer 1 echo (informational) ----------
SELECT COUNT(*) AS Essay384Seeded FROM AssessmentSessions WHERE Title LIKE '[[]ESSAY384]%';
