-- ============================================================
-- Phase 386 PXF-02 (F-DEV-01) — Option Validation e2e Seed
-- REQ: PXF-02 (option-validation-386.spec.ts)
-- ============================================================
--
-- Tujuan:
--   Seed 1 AssessmentSession (Upcoming/Draft) + 1 AssessmentPackage KOSONG (tanpa
--   PackageQuestion). Spec membuka /Admin/ManagePackageQuestions?packageId=<seeded>
--   lalu mencoba submit soal MultipleChoice yang malformed (correct flag tapi SEMUA
--   opsi kosong) → CreateQuestion harus REJECT lewat QuestionOptionValidator →
--   TempData["Error"] di-render sebagai .alert-danger; soal malformed TIDAK tersimpan
--   (daftar soal tetap 0).
--
--   Spec resolve packageId dari DB (hindari hardcode) lewat prefix Title '[OPTVAL386]'.
--
-- Klasifikasi: temporary + local-only. Prefix Title '[OPTVAL386]' untuk Layer 1
--   seeded-check + Layer 4 cleanup verify. JANGAN promosikan ke Data/SeedData.cs.
--
-- Cara run (DB lokal saja — JANGAN di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\option-validation-386-seed.sql
--   (spec Playwright menjalankan ini via db.execScript di beforeAll, dengan db.backup
--    snapshot sebelum + db.restore sesudah — SEED_WORKFLOW.)
--
-- Idempotent: WIPE-AND-INSERT — hapus fixture lama prefix [OPTVAL386] (FK-safe
--   child→parent) dulu, lalu INSERT.
--
-- Pre-condition: user Email='rino.prasetyo@pertamina.com' ada di Users (owner fixture).
-- ============================================================

SET NOCOUNT ON;

-- ---------- Cleanup fixture lama (idempotent re-run) ----------
DELETE FROM PackageUserResponses
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386%');

DELETE FROM UserPackageAssignments
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386%');

DELETE FROM PackageOptions
 WHERE PackageQuestionId IN (
        SELECT q.Id FROM PackageQuestions q
          JOIN AssessmentPackages p ON q.AssessmentPackageId = p.Id
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]OPTVAL386%');

DELETE FROM PackageQuestions
 WHERE AssessmentPackageId IN (
        SELECT p.Id FROM AssessmentPackages p
          JOIN AssessmentSessions s ON p.AssessmentSessionId = s.Id
         WHERE s.Title LIKE '[[]OPTVAL386%');

DELETE FROM AssessmentPackages
 WHERE AssessmentSessionId IN (SELECT Id FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386%');

DELETE FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386%';

-- ---------- Pre-condition ----------
DECLARE @uid NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @uid IS NULL
    THROW 51386, 'Seed pre-condition gagal: user rino.prasetyo@pertamina.com tidak ditemukan di Users.', 1;

-- ---------- 1) AssessmentSession (Upcoming, draft konfigurasi) ----------
DECLARE @sessions TABLE (Id INT);
INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, Score,
     IsTokenRequired, AccessToken, CreatedAt, ElapsedSeconds, IsManualEntry, HasManualGrading, SamePackage)
OUTPUT inserted.Id INTO @sessions
VALUES
    (@uid, '[OPTVAL386] Validasi Opsi PXF-02', 'OJT', GETDATE(), 60, 'Upcoming', 0, 'bg-primary',
     70, 1, 0, NULL, NULL, NULL,
     0, '', GETDATE(), 0, 0, 0, 0);
DECLARE @sid INT = (SELECT TOP 1 Id FROM @sessions);

-- ---------- 2) AssessmentPackage (KOSONG — tanpa PackageQuestion) ----------
DECLARE @packages TABLE (Id INT);
INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT inserted.Id INTO @packages
VALUES (@sid, 'Paket Validasi', 1, GETDATE());

-- ---------- Layer 1 echo (informational) ----------
SELECT COUNT(*) AS OptVal386Seeded FROM AssessmentSessions WHERE Title LIKE '[[]OPTVAL386]%';
