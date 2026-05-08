-- ============================================================
-- Phase 313 Timer Fixtures - Block Manual Submit Saat Waktu Habis
-- REQ: TMR-01
-- ============================================================
--
-- Tujuan:
--   Seed 7 fixture AssessmentSessions dengan StartedAt back-dated (D-07)
--   + dedicated title pattern (D-08) untuk Wave 0 Playwright FLOW 313
--   + Manual UAT 313-UAT.md.
--
-- Cara run (DB lokal saja - JANGAN jalankan di Dev/Prod per CLAUDE.md DEV_WORKFLOW):
--   - SSMS: open file, F5 (execute) terhadap koneksi DB lokal HcPortalDB_Dev
--   - sqlcmd: sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -i .planning\seeds\313-timer-fixtures.sql
--
-- Idempotent:
--   Script ini WIPE-AND-INSERT - re-run aman, akan delete fixture lama (title prefix
--   'Phase 313 Timer Fixture') sebelum INSERT 7 row baru. Tidak akan menyentuh data lain.
--
-- Pre-condition:
--   1. User dengan Email = 'rino.prasetyo@pertamina.com' ada di Users (coachee fixture).
--      Verify: SELECT Email FROM Users WHERE Email='rino.prasetyo@pertamina.com';
--   2. Skema AssessmentSessions sesuai snapshot 2026-05-08
--      (verified Migrations/ApplicationDbContextModelSnapshot.cs:286-459)
--
-- Anti-pattern Phase 309 (UserId=NULL FK violation):
--   UserId DIWAJIBKAN valid - script pakai subquery + THROW guard kalau user tidak ada.
--
-- ------------------------------------------------------------
-- Phase 313.1 update (2026-05-08, gap closure F-313-UAT-01):
--   Seed sekarang hierarchical: 7 Sessions -> 7 AssessmentPackages -> 21 PackageQuestions
--   (3 per package, MultipleChoice synthetic) -> 84 PackageOptions (4 per question,
--   IsCorrect=true index 0). Cleanup chain 6-step FK-respecting di-PREPEND.
--   INSERT chain di-wrap dalam BEGIN TRAN ... COMMIT explicit (D-07) sebagai defense
--   layer kedua di atas SET XACT_ABORT ON existing -- keduanya stack: XACT_ABORT
--   auto-rollback on error, BEGIN TRAN provides explicit transaction boundary.
--
-- Skema verified Migrations/ApplicationDbContextModelSnapshot.cs:258-284 (Package),
-- 1198-1262 (Option/Question), 286-459 (Session). FK topology:
--   AssessmentSession -> AssessmentPackage (Cascade) -> PackageQuestion (Cascade) -> PackageOption (Cascade)
--   AssessmentSession <- PackageUserResponse (Restrict)
--   AssessmentSession <- UserPackageAssignment (Cascade Session, Restrict Package)
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ============================================================
-- 1. Resolve referensi user + waktu acuan
-- ============================================================
DECLARE @UserId NVARCHAR(450) = (
    SELECT TOP 1 Id
    FROM Users
    WHERE Email = 'rino.prasetyo@pertamina.com'
);

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Validation guards (anti-pattern Phase 309 mitigation)
IF @UserId IS NULL
BEGIN
    THROW 50001,
        'User rino.prasetyo@pertamina.com tidak ditemukan di Users - abort. Jalankan seed user coachee terlebih dahulu.',
        1;
END;

PRINT N'[Phase 313 seed] UserId resolved: ' + @UserId;
PRINT N'[Phase 313 seed] @Now = ' + CONVERT(NVARCHAR(30), @Now, 126);

-- ============================================================
-- 2. Idempotent cleanup chain 6-step (FK-respecting; D-08 + D-09)
--    Cleanup jalan SEBELUM BEGIN TRAN supaya idempotent re-run tetap aman
--    walaupun INSERT chain berikutnya gagal.
-- ============================================================

-- 2.1 PackageUserResponses pertama (Restrict on Session/Question/Option)
DELETE pur FROM PackageUserResponses pur
INNER JOIN AssessmentSessions s ON pur.AssessmentSessionId = s.Id
WHERE s.Title LIKE 'Phase 313 Timer Fixture%';
PRINT N'[Phase 313.1 seed] PackageUserResponses cleaned.';

-- 2.2 UserPackageAssignments (Cascade on Session, but Restrict on Package)
DELETE upa FROM UserPackageAssignments upa
INNER JOIN AssessmentSessions s ON upa.AssessmentSessionId = s.Id
WHERE s.Title LIKE 'Phase 313 Timer Fixture%';
PRINT N'[Phase 313.1 seed] UserPackageAssignments cleaned.';

-- 2.3 PackageOptions
DELETE po FROM PackageOptions po
INNER JOIN PackageQuestions pq ON po.PackageQuestionId = pq.Id
INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
INNER JOIN AssessmentSessions s ON ap.AssessmentSessionId = s.Id
WHERE s.Title LIKE 'Phase 313 Timer Fixture%';
PRINT N'[Phase 313.1 seed] PackageOptions cleaned.';

-- 2.4 PackageQuestions
DELETE pq FROM PackageQuestions pq
INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
INNER JOIN AssessmentSessions s ON ap.AssessmentSessionId = s.Id
WHERE s.Title LIKE 'Phase 313 Timer Fixture%';
PRINT N'[Phase 313.1 seed] PackageQuestions cleaned.';

-- 2.5 AssessmentPackages
DELETE ap FROM AssessmentPackages ap
INNER JOIN AssessmentSessions s ON ap.AssessmentSessionId = s.Id
WHERE s.Title LIKE 'Phase 313 Timer Fixture%';
PRINT N'[Phase 313.1 seed] AssessmentPackages cleaned.';

-- 2.6 AssessmentSessions
DELETE FROM AssessmentSessions WHERE Title LIKE 'Phase 313 Timer Fixture%';
PRINT N'[Phase 313.1 seed] AssessmentSessions cleaned.';

-- ============================================================
-- 2.7 BEGIN TRAN -- explicit transaction boundary (D-07)
--     SET XACT_ABORT ON sudah aktif (Section 1) -> kalau error runtime,
--     transaction auto-rollback. BEGIN TRAN memberikan defense-in-depth +
--     explicit boundary observable via @@TRANCOUNT.
-- ============================================================
BEGIN TRAN;
PRINT N'[Phase 313.1 seed] BEGIN TRAN -- explicit transaction opened.';

-- ============================================================
-- 3. Insert 7 fixture (D-07 back-dated + D-08 title pattern) -- capture identity
-- ============================================================
-- Matrix (Duration=60 min untuk semua, ExtraTime=NULL):
--   # | Title (Type, Scenario)                          | AssessmentType | StartedAt offset | Expected
--   1 | Phase 313 Timer Fixture Online ManualBeforeTime | Online         | NOW - 5 min      | Submit OK
--   2 | Phase 313 Timer Fixture Online ManualAfterGrace | Online         | NOW - 61 min     | Tier-1 BLOCK
--   3 | Phase 313 Timer Fixture Online AutoInGrace      | Online         | NOW - 61 min     | Submit OK (Tier-2 grace)
--   4 | Phase 313 Timer Fixture Online AutoAfterGrace   | Online         | NOW - 67 min     | Tier-2 BLOCK
--   5 | Phase 313 Timer Fixture PreTest ManualAfterGrace| PreTest        | NOW - 61 min     | Tier-1 BLOCK
--   6 | Phase 313 Timer Fixture PostTest ManualAfterGrace| PostTest      | NOW - 61 min     | Tier-1 BLOCK
--   7 | Phase 313 Timer Fixture Manual ExcludeVerify    | Manual         | NOW - 161 min    | Submit OK (D-15 exclude)

-- NB: Field NOT NULL yang harus diisi (per ApplicationDbContextModelSnapshot.cs):
--   Title, Category, UserId, AccessToken, BannerColor, Status, Schedule, DurationMinutes,
--   ElapsedSeconds (default 0), GenerateCertificate, HasManualGrading, IsManualEntry,
--   IsTokenRequired, PassPercentage, Progress, SamePackage, AllowAnswerReview, CreatedAt.
-- AssessmentSession.Schedule adalah DateTime (bukan FK Schedules) - pakai @Now.

DECLARE @SessionIds TABLE (Id INT, Title NVARCHAR(450));

INSERT INTO AssessmentSessions (
    Title, Category, UserId, AssessmentType,
    DurationMinutes, ExtraTimeMinutes, StartedAt, Status,
    Schedule, AccessToken, BannerColor,
    AllowAnswerReview, GenerateCertificate, HasManualGrading,
    IsManualEntry, IsTokenRequired, SamePackage,
    PassPercentage, Progress, ElapsedSeconds,
    CreatedAt
)
OUTPUT INSERTED.Id, INSERTED.Title INTO @SessionIds(Id, Title)
VALUES
    -- 1: Online + Manual + before-time -> submit OK (regression)
    ( 'Phase 313 Timer Fixture Online ManualBeforeTime', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -5, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 300,
      @Now ),

    -- 2: Online + Manual + after-time (in grace) -> Tier-1 BLOCK
    ( 'Phase 313 Timer Fixture Online ManualAfterGrace', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 3: Online + Auto + after-time (in grace) -> submit OK (Tier-2 grace covers)
    ( 'Phase 313 Timer Fixture Online AutoInGrace', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 4: Online + Auto + after-grace -> Tier-2 BLOCK (existing preserved)
    ( 'Phase 313 Timer Fixture Online AutoAfterGrace', 'Test Phase 313', @UserId, 'Online',
      60, NULL, DATEADD(MINUTE, -67, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 4020,
      @Now ),

    -- 5: PreTest + Manual + after-time (in grace) -> Tier-1 BLOCK
    ( 'Phase 313 Timer Fixture PreTest ManualAfterGrace', 'Test Phase 313', @UserId, 'PreTest',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 6: PostTest + Manual + after-time (in grace) -> Tier-1 BLOCK
    ( 'Phase 313 Timer Fixture PostTest ManualAfterGrace', 'Test Phase 313', @UserId, 'PostTest',
      60, NULL, DATEADD(MINUTE, -61, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 3660,
      @Now ),

    -- 7: Manual + Manual + after-time -> submit OK (D-15 exclude verify)
    ( 'Phase 313 Timer Fixture Manual ExcludeVerify', 'Test Phase 313', @UserId, 'Manual',
      60, NULL, DATEADD(MINUTE, -161, @Now), 'InProgress',
      @Now, '', 'bg-primary',
      1, 0, 0,
      0, 0, 0,
      70, 0, 9660,
      @Now );

PRINT N'[Phase 313.1 seed] 7 AssessmentSessions di-INSERT (identity captured).';

-- ============================================================
-- 4. Insert AssessmentPackages (1 per session) -- D-01
-- ============================================================
DECLARE @PackageIds TABLE (Id INT, AssessmentSessionId INT);

INSERT INTO AssessmentPackages (AssessmentSessionId, PackageName, PackageNumber, CreatedAt)
OUTPUT INSERTED.Id, INSERTED.AssessmentSessionId INTO @PackageIds(Id, AssessmentSessionId)
SELECT
    s.Id,
    N'Phase 313 Test Package',  -- D-01
    1,                           -- PackageNumber
    @Now
FROM @SessionIds s;

PRINT N'[Phase 313.1 seed] 7 AssessmentPackages di-INSERT.';

-- ============================================================
-- 5. Insert PackageQuestions (3 per package) -- D-02 + D-04 (synthetic) + D-05 (identik 7 fixture)
-- ============================================================
DECLARE @QuestionTemplate TABLE ([Order] INT, QuestionText NVARCHAR(MAX), ScoreValue INT);
INSERT INTO @QuestionTemplate VALUES
    (1, N'Phase 313 Test Q1: Pilih jawaban yang benar.', 10),
    (2, N'Phase 313 Test Q2: Pilih jawaban yang benar.', 10),
    (3, N'Phase 313 Test Q3: Pilih jawaban yang benar.', 10);

DECLARE @QuestionIds TABLE (Id INT, AssessmentPackageId INT, [Order] INT);

INSERT INTO PackageQuestions (
    AssessmentPackageId, [Order], QuestionText, ScoreValue,
    QuestionType, MaxCharacters, Rubrik, ElemenTeknis
)
OUTPUT INSERTED.Id, INSERTED.AssessmentPackageId, INSERTED.[Order]
    INTO @QuestionIds(Id, AssessmentPackageId, [Order])
SELECT
    p.Id,
    t.[Order],
    t.QuestionText,
    t.ScoreValue,
    N'MultipleChoice',  -- D-02
    0,                  -- MaxCharacters (MC ignore)
    NULL,               -- Rubrik
    NULL                -- ElemenTeknis
FROM @PackageIds p
CROSS JOIN @QuestionTemplate t;

PRINT N'[Phase 313.1 seed] 21 PackageQuestions di-INSERT.';

-- ============================================================
-- 6. Insert PackageOptions (4 per question) -- D-03 (IsCorrect=1 HANYA index 0)
-- ============================================================
DECLARE @OptionTemplate TABLE ([Index] INT, OptionText NVARCHAR(MAX), IsCorrect BIT);
INSERT INTO @OptionTemplate VALUES
    (0, N'Pilihan A', 1),
    (1, N'Pilihan B', 0),
    (2, N'Pilihan C', 0),
    (3, N'Pilihan D', 0);

INSERT INTO PackageOptions (PackageQuestionId, OptionText, IsCorrect)
SELECT
    q.Id,
    o.OptionText,
    o.IsCorrect
FROM @QuestionIds q
CROSS JOIN @OptionTemplate o;

PRINT N'[Phase 313.1 seed] 84 PackageOptions di-INSERT.';

-- ============================================================
-- 6.1 COMMIT -- close explicit transaction (D-07)
--     Kalau sampai sini tanpa error, semua INSERT (Sessions/Packages/Questions/Options)
--     persist atomik. Kalau ada error sebelum COMMIT, XACT_ABORT auto-rollback semua.
-- ============================================================
COMMIT;
PRINT N'[Phase 313.1 seed] COMMIT -- transaction closed, INSERT chain persisted.';

-- ============================================================
-- 7. Final verification SELECT (post-COMMIT, read-only)
-- ============================================================
SELECT
    s.Id, s.Title, s.AssessmentType, s.StartedAt, s.DurationMinutes,
    ISNULL(s.ExtraTimeMinutes, 0) AS ExtraTimeMinutes,
    DATEDIFF(MINUTE, s.StartedAt, SYSUTCDATETIME()) AS ElapsedMinutes,
    s.Status, s.UserId,
    (SELECT COUNT(*) FROM AssessmentPackages WHERE AssessmentSessionId = s.Id) AS PkgCount,
    (SELECT COUNT(*) FROM PackageQuestions pq INNER JOIN AssessmentPackages ap
         ON pq.AssessmentPackageId = ap.Id WHERE ap.AssessmentSessionId = s.Id) AS QCount,
    (SELECT COUNT(*) FROM PackageOptions po
         INNER JOIN PackageQuestions pq ON po.PackageQuestionId = pq.Id
         INNER JOIN AssessmentPackages ap ON pq.AssessmentPackageId = ap.Id
         WHERE ap.AssessmentSessionId = s.Id) AS OptCount
FROM AssessmentSessions s
WHERE s.Title LIKE 'Phase 313 Timer Fixture%'
ORDER BY s.Title;
-- Expected: 7 row, semua dengan PkgCount=1, QCount=3, OptCount=12
