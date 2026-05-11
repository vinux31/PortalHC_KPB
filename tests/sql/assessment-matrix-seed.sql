-- ============================================================
-- Phase 315 Assessment Matrix Test Seed
-- REQ: QA-01
-- ============================================================
--
-- Tujuan:
--   Seed 18 AssessmentSession (9 scenario × 2 sibling peserta) + 18 packages +
--   54 questions (3 per package: MC + MA + Essay mixed atau single-type per scenario)
--   + 144 PackageOptions (4 per MC/MA question; Essay tidak punya options).
--   Untuk Phase 315 matrix discovery test (7 skenario + 3 sentinel meta-validation).
--   2 peserta (coachee = rino.prasetyo, coachee2 = iwan3) -> 2 sibling sessions per scenario.
--
-- Cara run (DB lokal saja - JANGAN jalankan di Dev/Prod per CLAUDE.md Develop Workflow):
--   sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i tests\sql\assessment-matrix-seed.sql
--
-- Idempotent: WIPE-AND-INSERT -- re-run aman, akan delete fixture lama dengan Title prefix
--   [MATRIX_TEST_2026_05_11] (6-step FK-respecting cleanup chain).
--
-- Pre-condition:
--   1. User dengan Email = 'rino.prasetyo@pertamina.com' ada di Users (coachee fixture).
--   2. User dengan Email = 'iwan3@pertamina.com' ada di Users (coachee2 fixture).
--   3. ID range 9001-9018 (AssessmentSessions + AssessmentPackages) belum dipakai data lain
--      (collision guard akan throw kalau range conflict).
--
-- Marker strategy: Title prefix [MATRIX_TEST_2026_05_11] (D-05 fallback -- Notes field
--   tidak ada di AssessmentSession per Wave 0 Q3, confirmed via grep di Plan 01).
--
-- Anti-pattern Phase 309 (UserId=NULL FK violation):
--   UserId DIWAJIBKAN valid - subquery + THROW guard kalau user tidak ada.
--
-- IDENTITY_INSERT pattern: deterministic PK 9001+ supaya .matrix-state.json config match DB
--   (Pitfall 4 mitigation; per-table block SET IDENTITY_INSERT ON/OFF).
--
-- ============================================================
-- DETERMINISTIC OPTION ID FORMULA (single source of truth):
--   optId = 80001 + (qId - 50001) * 4 + optIndex
--   where qId in [50001..50054], optIndex in [0..3]
--
-- Examples:
--   qId=50001 (MC)    -> opts [80001, 80002, 80003, 80004]; correct = 80001 (optIndex=0)
--   qId=50002 (MA)    -> opts [80005, 80006, 80007, 80008]; correct = 80005+80006 (optIndex=0,1)
--   qId=50003 (Essay) -> NO options (Essay tidak butuh PackageOptions). Slot 80009-80012 reserved
--                        (gap by design -- formula tetap deterministic).
--   qId=50054 (last)  -> opts [80213, 80214, 80215, 80216]
--
-- Formula ini WAJIB di-replikasi di tests/e2e/global.setup.ts buildScenarios() supaya
-- correctOptionIds[] derivation deterministic, NOT hand-typed. Single source of truth ->
-- seed SQL dan helper consume formula yang sama, mismatch jadi mustahil.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
-- Rule 1 auto-fix Plan 04: QUOTED_IDENTIFIER + ANSI_NULLS WAJIB ON untuk DELETE pada
-- table yang punya filtered indexes / indexed views (error 1934 di smoke run Plan 04
-- saat DELETE di section 2 cleanup). sqlcmd default OFF — set explicit di seed header.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ============================================================
-- 1. Resolve referensi user + waktu acuan (2 users -- coachee + coachee2)
-- ============================================================
DECLARE @CoacheeId NVARCHAR(450) = (
    SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com'
);
DECLARE @Coachee2Id NVARCHAR(450) = (
    SELECT TOP 1 Id FROM Users WHERE Email = 'iwan3@pertamina.com'
);

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @ScheduleDate DATETIME2 = '2026-05-11T00:00:00';

-- Validation guards (anti-pattern Phase 309 mitigation)
IF @CoacheeId IS NULL OR @Coachee2Id IS NULL
BEGIN
    THROW 50001,
        'Fixture user (rino.prasetyo@pertamina.com atau iwan3@pertamina.com) tidak ditemukan di Users - abort. Seed UserManager dulu via dotnet ef seed.',
        1;
END;

PRINT N'[Phase 315 seed] CoacheeId resolved: ' + @CoacheeId;
PRINT N'[Phase 315 seed] Coachee2Id resolved: ' + @Coachee2Id;
PRINT N'[Phase 315 seed] @Now = ' + CONVERT(NVARCHAR(30), @Now, 126);

-- ============================================================
-- 2. Pre-check ID collision (defense -- RESEARCH Q4 mitigation)
--    Cek range 9001-9018 di AssessmentSessions DAN AssessmentPackages (overlap range,
--    tabel berbeda tapi sama-sama dipakai IDENTITY_INSERT 9001-9018).
-- ============================================================
DECLARE @SessionCollisionCount INT = (
    SELECT COUNT(*) FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9018
);
DECLARE @PackageCollisionCount INT = (
    SELECT COUNT(*) FROM AssessmentPackages WHERE Id BETWEEN 9001 AND 9018
);
IF @SessionCollisionCount > 0 OR @PackageCollisionCount > 0
BEGIN
    THROW 50002,
        'ID range 9001-9018 sudah dipakai di AssessmentSessions atau AssessmentPackages - abort. Inspect: SELECT Id, Title FROM AssessmentSessions WHERE Id BETWEEN 9001 AND 9018; SELECT Id FROM AssessmentPackages WHERE Id BETWEEN 9001 AND 9018',
        1;
END;

PRINT N'[Phase 315 seed] Collision check passed: range 9001-9018 free.';

-- ============================================================
-- 3. Idempotent 6-step FK-respecting cleanup chain
--    (Pattern D -- copy dari .planning/seeds/313-timer-fixtures.sql lines 73-110)
--    Filter Title prefix [MATRIX_TEST_2026_05_11] supaya HANYA fixture phase 315 yang ke-clean.
-- ============================================================

-- 3.1 PackageUserResponses (Restrict on Session/Question/Option)
DELETE pur FROM PackageUserResponses pur
INNER JOIN AssessmentSessions s ON pur.AssessmentSessionId = s.Id
WHERE s.Title LIKE '[[]MATRIX_TEST_2026_05_11]%';
PRINT N'[Phase 315 seed] PackageUserResponses cleaned.';

-- 3.2 UserPackageAssignments (Cascade on Session, Restrict on Package)
DELETE upa FROM UserPackageAssignments upa
INNER JOIN AssessmentSessions s ON upa.AssessmentSessionId = s.Id
WHERE s.Title LIKE '[[]MATRIX_TEST_2026_05_11]%';
PRINT N'[Phase 315 seed] UserPackageAssignments cleaned.';

-- 3.3 PackageOptions (JOIN via PackageQuestions -> AssessmentPackages -> AssessmentSessions)
DELETE po FROM PackageOptions po
INNER JOIN PackageQuestions pq ON po.PackageQuestionId = pq.Id
INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
INNER JOIN AssessmentSessions s ON ap.AssessmentSessionId = s.Id
WHERE s.Title LIKE '[[]MATRIX_TEST_2026_05_11]%';
PRINT N'[Phase 315 seed] PackageOptions cleaned.';

-- 3.4 PackageQuestions
DELETE pq FROM PackageQuestions pq
INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
INNER JOIN AssessmentSessions s ON ap.AssessmentSessionId = s.Id
WHERE s.Title LIKE '[[]MATRIX_TEST_2026_05_11]%';
PRINT N'[Phase 315 seed] PackageQuestions cleaned.';

-- 3.5 AssessmentPackages
DELETE ap FROM AssessmentPackages ap
INNER JOIN AssessmentSessions s ON ap.AssessmentSessionId = s.Id
WHERE s.Title LIKE '[[]MATRIX_TEST_2026_05_11]%';
PRINT N'[Phase 315 seed] AssessmentPackages cleaned.';

-- 3.6 AssessmentSessions
DELETE FROM AssessmentSessions WHERE Title LIKE '[[]MATRIX_TEST_2026_05_11]%';
PRINT N'[Phase 315 seed] AssessmentSessions cleaned.';

-- ============================================================
-- 4. BEGIN TRAN -- explicit transaction boundary (Pattern E)
--    SET XACT_ABORT ON sudah aktif (Section 1) -> kalau error runtime,
--    transaction auto-rollback. BEGIN TRAN memberikan defense-in-depth
--    + explicit boundary observable via @@TRANCOUNT.
-- ============================================================
BEGIN TRAN;
PRINT N'[Phase 315 seed] BEGIN TRAN -- explicit transaction opened.';

-- ============================================================
-- 5.1 AssessmentSessions -- 18 sibling sessions (9 scenarios x 2 peserta)
--     ID range: 9001-9018. Peserta1 (coachee) di odd Id, Peserta2 (coachee2) di even Id.
--     Pengecualian: S9 [META-AllWrong] dan S10 [META-CollectorCheck] hanya 1 session
--                   (single-peserta sentinel), pakai coachee saja (per plan buildScenarios:
--                   sessionIdPeserta1 = sessionIdPeserta2 untuk sentinel ini).
-- ============================================================
SET IDENTITY_INSERT AssessmentSessions ON;

INSERT INTO AssessmentSessions
  (Id, Title, Category, UserId, AccessToken, BannerColor, Status, Schedule, DurationMinutes,
   ElapsedSeconds, GenerateCertificate, HasManualGrading, IsManualEntry, IsTokenRequired,
   PassPercentage, Progress, SamePackage, AllowAnswerReview, CreatedAt, AssessmentType)
VALUES
  -- Scenario 1: Manual Mixed (MC + MA + Essay)  -- HasManualGrading=1 karena ada Essay
  (9001, N'[MATRIX_TEST_2026_05_11] S1 Manual Mixed', N'Matrix Test Category', @CoacheeId,
   N'MTX-S1-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 0, 1, 1, 0, 75, 0, 1, 1, @Now, N'Manual'),
  (9002, N'[MATRIX_TEST_2026_05_11] S1 Manual Mixed', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S1-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 0, 1, 1, 0, 75, 0, 1, 1, @Now, N'Manual'),
  -- Scenario 2: Online Mixed  -- HasManualGrading=1 karena ada Essay
  (9003, N'[MATRIX_TEST_2026_05_11] S2 Online Mixed', N'Matrix Test Category', @CoacheeId,
   N'MTX-S2-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  (9004, N'[MATRIX_TEST_2026_05_11] S2 Online Mixed', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S2-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  -- Scenario 3: PreTest Mixed  -- HasManualGrading=1
  (9005, N'[MATRIX_TEST_2026_05_11] S3 PreTest Mixed', N'Matrix Test Category', @CoacheeId,
   N'MTX-S3-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'PreTest'),
  (9006, N'[MATRIX_TEST_2026_05_11] S3 PreTest Mixed', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S3-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'PreTest'),
  -- Scenario 4: PostTest Mixed  -- HasManualGrading=1
  (9007, N'[MATRIX_TEST_2026_05_11] S4 PostTest Mixed', N'Matrix Test Category', @CoacheeId,
   N'MTX-S4-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'PostTest'),
  (9008, N'[MATRIX_TEST_2026_05_11] S4 PostTest Mixed', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S4-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'PostTest'),
  -- Scenario 5: Online MC only  -- HasManualGrading=0
  (9009, N'[MATRIX_TEST_2026_05_11] S5 Online MC only', N'Matrix Test Category', @CoacheeId,
   N'MTX-S5-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 0, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  (9010, N'[MATRIX_TEST_2026_05_11] S5 Online MC only', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S5-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 0, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  -- Scenario 6: Online MA only
  (9011, N'[MATRIX_TEST_2026_05_11] S6 Online MA only', N'Matrix Test Category', @CoacheeId,
   N'MTX-S6-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 0, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  (9012, N'[MATRIX_TEST_2026_05_11] S6 Online MA only', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S6-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 0, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  -- Scenario 7: Online Essay only -- HasManualGrading=1
  (9013, N'[MATRIX_TEST_2026_05_11] S7 Online Essay only', N'Matrix Test Category', @CoacheeId,
   N'MTX-S7-P1', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  (9014, N'[MATRIX_TEST_2026_05_11] S7 Online Essay only', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S7-P2', N'bg-primary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  -- Sentinel S8: [META-AllCorrect] (2 peserta -- mixed) -- HasManualGrading=1
  (9015, N'[MATRIX_TEST_2026_05_11] [META-AllCorrect] Sentinel', N'Matrix Test Category', @CoacheeId,
   N'MTX-S8-P1', N'bg-secondary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  (9016, N'[MATRIX_TEST_2026_05_11] [META-AllCorrect] Sentinel', N'Matrix Test Category', @Coachee2Id,
   N'MTX-S8-P2', N'bg-secondary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  -- Sentinel S9: [META-AllWrong] (1 peserta -- coachee saja, sentinel single)
  (9017, N'[MATRIX_TEST_2026_05_11] [META-AllWrong] Sentinel', N'Matrix Test Category', @CoacheeId,
   N'MTX-S9-P1', N'bg-secondary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online'),
  -- Sentinel S10: [META-CollectorCheck] (1 peserta -- coachee, test.fail() block)
  (9018, N'[MATRIX_TEST_2026_05_11] [META-CollectorCheck] Sentinel', N'Matrix Test Category', @CoacheeId,
   N'MTX-S10-P1', N'bg-secondary', N'NotStarted', @ScheduleDate, 60, 0, 1, 0, 0, 0, 75, 0, 1, 1, @Now, N'Online');

SET IDENTITY_INSERT AssessmentSessions OFF;
PRINT N'[Phase 315 seed] 18 AssessmentSessions di-INSERT (Id 9001-9018).';

-- ============================================================
-- 5.2 AssessmentPackages -- 1 package per session (A1 verdict: 1-PER-SESSION)
--     ID range: 9001-9018 (1:1 mapping ke session Id; AssessmentPackages punya tabel sendiri,
--     range Id boleh overlap dengan AssessmentSessions karena tabel berbeda).
-- ============================================================
SET IDENTITY_INSERT AssessmentPackages ON;

INSERT INTO AssessmentPackages (Id, AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
VALUES
  (9001, 9001, N'Phase 315 Matrix Package', 1, @Now),
  (9003, 9003, N'Phase 315 Matrix Package', 1, @Now),
  (9005, 9005, N'Phase 315 Matrix Package', 1, @Now),
  (9007, 9007, N'Phase 315 Matrix Package', 1, @Now),
  (9009, 9009, N'Phase 315 Matrix Package', 1, @Now),
  (9011, 9011, N'Phase 315 Matrix Package', 1, @Now),
  (9013, 9013, N'Phase 315 Matrix Package', 1, @Now),
  (9015, 9015, N'Phase 315 Matrix Package', 1, @Now),
  (9017, 9017, N'Phase 315 Matrix Package', 1, @Now),
  (9018, 9018, N'Phase 315 Matrix Package', 1, @Now);

SET IDENTITY_INSERT AssessmentPackages OFF;
-- 10 packages (peserta1 only per scenario; peserta2 shares via sibling-session cross-package pool).
PRINT N'[Phase 315 seed] 10 AssessmentPackages di-INSERT (peserta1 sessions only).';

-- ============================================================
-- 5.3 PackageQuestions -- 54 questions (3 per package, mixed type per scenario)
--     ID range: 50001-50054.
--     Schema (per Migrations/ApplicationDbContextModelSnapshot.cs:1223-1263):
--       Id, AssessmentPackageId, [Order] (reserved keyword, bracket required), QuestionText,
--       QuestionType, ScoreValue, MaxCharacters, Rubrik (nullable), ElemenTeknis (nullable).
--     CATATAN: Tidak ada kolom CreatedAt di PackageQuestions (berbeda dengan AssessmentPackages).
-- ============================================================
SET IDENTITY_INSERT PackageQuestions ON;

INSERT INTO PackageQuestions
  (Id, AssessmentPackageId, [Order], QuestionText, QuestionType, ScoreValue, MaxCharacters, Rubrik, ElemenTeknis)
VALUES
  -- S1 Manual Mixed peserta1 (package 9001): MC + MA + Essay
  -- peserta2 (session 9002) shares via cross-package pool — no separate package needed.
  (50001, 9001, 1, N'S1 MC: Pilih jawaban benar', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50002, 9001, 2, N'S1 MA: Pilih dua jawaban benar', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50003, 9001, 3, N'S1 Essay: Jelaskan konsep singkat', N'Essay', 35, 2000, N'Kunci: konsep matrix test discovery', NULL),
  -- S2 Online Mixed peserta1 (9003)
  (50007, 9003, 1, N'S2 MC', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50008, 9003, 2, N'S2 MA', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50009, 9003, 3, N'S2 Essay', N'Essay', 35, 2000, N'Kunci S2', NULL),
  -- S3 PreTest Mixed peserta1 (9005)
  (50013, 9005, 1, N'S3 MC', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50014, 9005, 2, N'S3 MA', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50015, 9005, 3, N'S3 Essay', N'Essay', 35, 2000, N'Kunci S3', NULL),
  -- S4 PostTest Mixed peserta1 (9007)
  (50019, 9007, 1, N'S4 MC', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50020, 9007, 2, N'S4 MA', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50021, 9007, 3, N'S4 Essay', N'Essay', 35, 2000, N'Kunci S4', NULL),
  -- S5 Online MC only peserta1 (9009): 3 MC questions
  (50025, 9009, 1, N'S5 MC #1', N'MultipleChoice', 34, 2000, NULL, NULL),
  (50026, 9009, 2, N'S5 MC #2', N'MultipleChoice', 33, 2000, NULL, NULL),
  (50027, 9009, 3, N'S5 MC #3', N'MultipleChoice', 33, 2000, NULL, NULL),
  -- S6 Online MA only peserta1 (9011)
  (50031, 9011, 1, N'S6 MA #1', N'MultipleAnswer', 34, 2000, NULL, NULL),
  (50032, 9011, 2, N'S6 MA #2', N'MultipleAnswer', 33, 2000, NULL, NULL),
  (50033, 9011, 3, N'S6 MA #3', N'MultipleAnswer', 33, 2000, NULL, NULL),
  -- S7 Online Essay only peserta1 (9013)
  (50037, 9013, 1, N'S7 Essay #1', N'Essay', 34, 2000, N'Kunci S7 #1', NULL),
  (50038, 9013, 2, N'S7 Essay #2', N'Essay', 33, 2000, N'Kunci S7 #2', NULL),
  (50039, 9013, 3, N'S7 Essay #3', N'Essay', 33, 2000, N'Kunci S7 #3', NULL),
  -- Sentinel S8 META-AllCorrect peserta1 (9015)
  (50043, 9015, 1, N'S8 MC sentinel', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50044, 9015, 2, N'S8 MA sentinel', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50045, 9015, 3, N'S8 Essay sentinel', N'Essay', 35, 2000, N'Kunci sentinel', NULL),
  -- Sentinel S9 META-AllWrong (9017) -- 1 peserta saja
  (50049, 9017, 1, N'S9 MC sentinel', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50050, 9017, 2, N'S9 MA sentinel', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50051, 9017, 3, N'S9 Essay sentinel', N'Essay', 35, 2000, N'Kunci sentinel S9', NULL),
  -- Sentinel S10 META-CollectorCheck (9018) -- 1 peserta saja
  (50052, 9018, 1, N'S10 MC sentinel', N'MultipleChoice', 30, 2000, NULL, NULL),
  (50053, 9018, 2, N'S10 MA sentinel', N'MultipleAnswer', 35, 2000, NULL, NULL),
  (50054, 9018, 3, N'S10 Essay sentinel', N'Essay', 35, 2000, N'Kunci sentinel S10', NULL);

SET IDENTITY_INSERT PackageQuestions OFF;
PRINT N'[Phase 315 seed] 30 PackageQuestions di-INSERT (peserta1 only — peserta2 shares via sibling pool).';

-- ============================================================
-- 5.4 PackageOptions -- 80 options (4 per MC/MA question; Essay skipped; peserta2 removed — sibling pool)
--     Generated mekanis dengan formula optId = 80001 + (qId - 50001) * 4 + optIndex.
--     Schema (per Migrations/ApplicationDbContextModelSnapshot.cs:1198-1221):
--       Id, PackageQuestionId, OptionText, IsCorrect (4 kolom -- TIDAK ada CreatedAt/OrderNumber).
--     MC pattern: optIndex 0 = correct (1 correct + 3 wrong).
--     MA pattern: optIndex 0+1 = correct (2 correct + 2 wrong).
--     Essay pattern: skip (Essay tidak butuh options; slot 80009-80012, 80021-80024, dst reserved/unused).
-- ============================================================
SET IDENTITY_INSERT PackageOptions ON;

INSERT INTO PackageOptions (Id, PackageQuestionId, OptionText, IsCorrect)
VALUES
  (80001, 50001, N'Jawaban A (benar)', 1),
  (80002, 50001, N'Jawaban B', 0),
  (80003, 50001, N'Jawaban C', 0),
  (80004, 50001, N'Jawaban D', 0),
  (80005, 50002, N'Jawaban A (benar)', 1),
  (80006, 50002, N'Jawaban B (benar)', 1),
  (80007, 50002, N'Jawaban C', 0),
  (80008, 50002, N'Jawaban D', 0),
  -- S1 P2 options (80013-80020 for q50004/50005) removed — peserta2 shares P1 via sibling pool
  (80025, 50007, N'Jawaban A (benar)', 1),
  (80026, 50007, N'Jawaban B', 0),
  (80027, 50007, N'Jawaban C', 0),
  (80028, 50007, N'Jawaban D', 0),
  (80029, 50008, N'Jawaban A (benar)', 1),
  (80030, 50008, N'Jawaban B (benar)', 1),
  (80031, 50008, N'Jawaban C', 0),
  (80032, 50008, N'Jawaban D', 0),
  -- S2 P2 options (80037-80044 for q50010/50011) removed — peserta2 shares P1 via sibling pool
  (80049, 50013, N'Jawaban A (benar)', 1),
  (80050, 50013, N'Jawaban B', 0),
  (80051, 50013, N'Jawaban C', 0),
  (80052, 50013, N'Jawaban D', 0),
  (80053, 50014, N'Jawaban A (benar)', 1),
  (80054, 50014, N'Jawaban B (benar)', 1),
  (80055, 50014, N'Jawaban C', 0),
  (80056, 50014, N'Jawaban D', 0),
  -- S3 P2 options (80061-80068 for q50016/50017) removed — peserta2 shares P1 via sibling pool
  (80073, 50019, N'Jawaban A (benar)', 1),
  (80074, 50019, N'Jawaban B', 0),
  (80075, 50019, N'Jawaban C', 0),
  (80076, 50019, N'Jawaban D', 0),
  (80077, 50020, N'Jawaban A (benar)', 1),
  (80078, 50020, N'Jawaban B (benar)', 1),
  (80079, 50020, N'Jawaban C', 0),
  (80080, 50020, N'Jawaban D', 0),
  -- S4 P2 options (80085-80092 for q50022/50023) removed — peserta2 shares P1 via sibling pool
  (80097, 50025, N'Jawaban A (benar)', 1),
  (80098, 50025, N'Jawaban B', 0),
  (80099, 50025, N'Jawaban C', 0),
  (80100, 50025, N'Jawaban D', 0),
  (80101, 50026, N'Jawaban A (benar)', 1),
  (80102, 50026, N'Jawaban B', 0),
  (80103, 50026, N'Jawaban C', 0),
  (80104, 50026, N'Jawaban D', 0),
  (80105, 50027, N'Jawaban A (benar)', 1),
  (80106, 50027, N'Jawaban B', 0),
  (80107, 50027, N'Jawaban C', 0),
  (80108, 50027, N'Jawaban D', 0),
  -- S5 P2 options (80109-80120 for q50028/50029/50030) removed — peserta2 shares P1 via sibling pool
  (80121, 50031, N'Jawaban A (benar)', 1),
  (80122, 50031, N'Jawaban B (benar)', 1),
  (80123, 50031, N'Jawaban C', 0),
  (80124, 50031, N'Jawaban D', 0),
  (80125, 50032, N'Jawaban A (benar)', 1),
  (80126, 50032, N'Jawaban B (benar)', 1),
  (80127, 50032, N'Jawaban C', 0),
  (80128, 50032, N'Jawaban D', 0),
  (80129, 50033, N'Jawaban A (benar)', 1),
  (80130, 50033, N'Jawaban B (benar)', 1),
  (80131, 50033, N'Jawaban C', 0),
  (80132, 50033, N'Jawaban D', 0),
  -- S6 P2 options (80133-80144 for q50034/50035/50036) removed — peserta2 shares P1 via sibling pool
  (80169, 50043, N'Jawaban A (benar)', 1),
  (80170, 50043, N'Jawaban B', 0),
  (80171, 50043, N'Jawaban C', 0),
  (80172, 50043, N'Jawaban D', 0),
  (80173, 50044, N'Jawaban A (benar)', 1),
  (80174, 50044, N'Jawaban B (benar)', 1),
  (80175, 50044, N'Jawaban C', 0),
  (80176, 50044, N'Jawaban D', 0),
  -- S8 P2 options (80181-80188 for q50046/50047) removed — peserta2 shares P1 via sibling pool
  (80193, 50049, N'Jawaban A (benar)', 1),
  (80194, 50049, N'Jawaban B', 0),
  (80195, 50049, N'Jawaban C', 0),
  (80196, 50049, N'Jawaban D', 0),
  (80197, 50050, N'Jawaban A (benar)', 1),
  (80198, 50050, N'Jawaban B (benar)', 1),
  (80199, 50050, N'Jawaban C', 0),
  (80200, 50050, N'Jawaban D', 0),
  (80205, 50052, N'Jawaban A (benar)', 1),
  (80206, 50052, N'Jawaban B', 0),
  (80207, 50052, N'Jawaban C', 0),
  (80208, 50052, N'Jawaban D', 0),
  (80209, 50053, N'Jawaban A (benar)', 1),
  (80210, 50053, N'Jawaban B (benar)', 1),
  (80211, 50053, N'Jawaban C', 0),
  (80212, 50053, N'Jawaban D', 0);

SET IDENTITY_INSERT PackageOptions OFF;
PRINT N'[Phase 315 seed] 80 PackageOptions di-INSERT (range 80001-80212, gap untuk Essay dan peserta2 — sibling pool).';

-- ============================================================
-- 6. COMMIT -- close explicit transaction (Pattern E)
--    Kalau sampai sini tanpa error, semua INSERT (Sessions/Packages/Questions/Options)
--    persist atomik. Kalau ada error sebelum COMMIT, XACT_ABORT auto-rollback semua.
-- ============================================================
COMMIT;
PRINT N'[Phase 315 seed] COMMIT -- transaction closed, INSERT chain persisted.';

-- ============================================================
-- 7. Final verification SELECT (Layer 1 source -- dipakai global.setup.ts queryScalar)
--    Expected: 18 rows; peserta1 PkgCount=1 QCount=3; peserta2 PkgCount=0 QCount=0 (sibling pool).
-- ============================================================
SELECT s.Id, s.Title, s.UserId, s.AssessmentType,
    (SELECT COUNT(*) FROM AssessmentPackages WHERE AssessmentSessionId = s.Id) AS PkgCount,
    (SELECT COUNT(*) FROM PackageQuestions pq INNER JOIN AssessmentPackages ap
        ON pq.AssessmentPackageId = ap.Id WHERE ap.AssessmentSessionId = s.Id) AS QCount,
    (SELECT COUNT(*) FROM PackageOptions po INNER JOIN PackageQuestions pq
        ON po.PackageQuestionId = pq.Id INNER JOIN AssessmentPackages ap
        ON pq.AssessmentPackageId = ap.Id WHERE ap.AssessmentSessionId = s.Id) AS OptCount
FROM AssessmentSessions s
WHERE s.Title LIKE '[[]MATRIX_TEST_2026_05_11]%'
ORDER BY s.Id;
-- Expected layout: 18 rows; peserta1 sessions PkgCount=1, QCount=3;
-- peserta2 sessions PkgCount=0, QCount=0 (get sibling package from peserta1 at StartExam).
-- peserta1 OptCount: S1-S4 mixed=8; S5 MC-only=12; S6 MA-only=12; S7 Essay-only=0;
--                   S8 sentinel mixed=8; S9,S10 sentinel mixed=8 each.
