-- ============================================================
-- Phase 361 Bypass Fixtures - Worker Multi-State untuk e2e UAT Bypass Tahun
-- REQ: PBYP-10
-- ============================================================
--
-- Tujuan:
--   Seed 4 worker multi-state yang menutup 4 closure mode + skenario pending (D-23):
--     Worker A (choirul.anam)    : komplit  -> semua progress Approved + final ada  -> eligible CL-A
--     Worker B (moch.widyadhana) : partial  -> 1 Approved + 1 Pending, TANPA final  -> eligible CL-B(a)/(b)
--     Worker C (mohammad.arsyad) : punya final (progress campur)                    -> CL-B DITOLAK (D-D)
--     Worker D (iwan3)           : pending CL-B(b) Status='Menunggu' + exam belum lulus (E5)
--
-- Cara run (DB lokal SAJA - JANGAN jalankan di Dev/Prod per CLAUDE.md DEV_WORKFLOW):
--   - SSMS: open file, F5 (execute) terhadap koneksi DB lokal HcPortalDB_Dev
--   - sqlcmd: sqlcmd -S "localhost\SQLEXPRESS" -d HcPortalDB_Dev -E -C -I -b -i .planning\seeds\361-bypass-fixtures.sql
--
-- Idempotent:
--   Script ini WIPE-AND-INSERT - re-run aman. Cleanup pakai marker:
--     - ProtonTrackAssignments.AssignedById = 'PHASE361-FIXTURE' (sentinel, kolom no-FK)
--     - PendingProtonBypasses.Reason LIKE 'Phase 361%'
--     - AssessmentSessions.Title LIKE 'Phase 361 Bypass Fixture%'
--   Tidak menyentuh data lain (assignment inactive existing iwan3 di track 4/5 TIDAK disentuh).
--
-- Pre-condition:
--   1. 4 user fixture ada di Users (THROW guard di bawah):
--      choirul.anam@pertamina.com, moch.widyadhana@pertamina.com,
--      mohammad.arsyad@pertamina.com, iwan3@pertamina.com, admin@pertamina.com (inisiator)
--   2. ProtonTracks Id=1 (Panelman Tahun 1, Urutan=1) dan Id=2 (Panelman Tahun 2, Urutan=2) ada.
--   3. ProtonDeliverableList Id=4 dan Id=5 (deliverable track 1) ada.
--
-- Anti-pattern Phase 309 (FK violation / referensi NULL):
--   Semua referensi user/track/deliverable di-resolve via subquery + THROW guard.
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- ============================================================
-- 1. Resolve referensi + THROW guard (anti Phase 309)
-- ============================================================
DECLARE @WorkerA NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'choirul.anam@pertamina.com');
IF @WorkerA IS NULL THROW 50001, 'User choirul.anam@pertamina.com tidak ditemukan - abort. Seed user dulu.', 1;

DECLARE @WorkerB NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'moch.widyadhana@pertamina.com');
IF @WorkerB IS NULL THROW 50002, 'User moch.widyadhana@pertamina.com tidak ditemukan - abort. Seed user dulu.', 1;

DECLARE @WorkerC NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'mohammad.arsyad@pertamina.com');
IF @WorkerC IS NULL THROW 50003, 'User mohammad.arsyad@pertamina.com tidak ditemukan - abort. Seed user dulu.', 1;

DECLARE @WorkerD NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'iwan3@pertamina.com');
IF @WorkerD IS NULL THROW 50004, 'User iwan3@pertamina.com tidak ditemukan - abort. Seed user dulu.', 1;

DECLARE @AdminId NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'admin@pertamina.com');
IF @AdminId IS NULL THROW 50005, 'User admin@pertamina.com tidak ditemukan - abort.', 1;

DECLARE @SourceTrackId INT = (SELECT TOP 1 Id FROM ProtonTracks WHERE Id = 1 AND Urutan = 1);
IF @SourceTrackId IS NULL THROW 50006, 'ProtonTracks Id=1 (Urutan=1) tidak ditemukan - abort.', 1;

DECLARE @TargetTrackId INT = (SELECT TOP 1 Id FROM ProtonTracks WHERE Id = 2 AND Urutan = 2);
IF @TargetTrackId IS NULL THROW 50007, 'ProtonTracks Id=2 (Urutan=2) tidak ditemukan - abort.', 1;

DECLARE @Deliv1 INT = (SELECT TOP 1 Id FROM ProtonDeliverableList WHERE Id = 4);
IF @Deliv1 IS NULL THROW 50008, 'ProtonDeliverableList Id=4 tidak ditemukan - abort.', 1;

DECLARE @Deliv2 INT = (SELECT TOP 1 Id FROM ProtonDeliverableList WHERE Id = 5);
IF @Deliv2 IS NULL THROW 50009, 'ProtonDeliverableList Id=5 tidak ditemukan - abort.', 1;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @Marker NVARCHAR(50) = N'PHASE361-FIXTURE';

PRINT N'[Phase 361 seed] Referensi resolved. WorkerA=' + @WorkerA + N' WorkerD=' + @WorkerD;

-- ============================================================
-- 2. Idempotent cleanup chain (FK-respecting; SEBELUM BEGIN TRAN)
--    Scope KETAT: hanya baris marker fixture ini.
-- ============================================================

-- 2.1 PendingProtonBypasses (by Reason marker)
DELETE FROM PendingProtonBypasses WHERE Reason LIKE N'Phase 361%';
PRINT N'[Phase 361 seed] PendingProtonBypasses cleaned.';

-- 2.2 AssessmentSessions fixture (by Title marker)
DELETE FROM AssessmentSessions WHERE Title LIKE N'Phase 361 Bypass Fixture%';
PRINT N'[Phase 361 seed] AssessmentSessions cleaned.';

-- 2.3 ProtonFinalAssessments (by assignment marker)
DELETE fa FROM ProtonFinalAssessments fa
INNER JOIN ProtonTrackAssignments a ON fa.ProtonTrackAssignmentId = a.Id
WHERE a.AssignedById = @Marker;
PRINT N'[Phase 361 seed] ProtonFinalAssessments cleaned.';

-- 2.4 ProtonDeliverableProgresses (by assignment marker)
DELETE p FROM ProtonDeliverableProgresses p
INNER JOIN ProtonTrackAssignments a ON p.ProtonTrackAssignmentId = a.Id
WHERE a.AssignedById = @Marker;
PRINT N'[Phase 361 seed] ProtonDeliverableProgresses cleaned.';

-- 2.5 ProtonTrackAssignments fixture (by AssignedById marker)
DELETE FROM ProtonTrackAssignments WHERE AssignedById = @Marker;
PRINT N'[Phase 361 seed] ProtonTrackAssignments cleaned.';

-- ============================================================
-- 3. INSERT 4 worker state (BEGIN TRAN ... COMMIT)
-- ============================================================
BEGIN TRAN;

-- ---------- Worker A: komplit -> CL-A ----------
INSERT INTO ProtonTrackAssignments (CoacheeId, AssignedById, ProtonTrackId, IsActive, AssignedAt)
VALUES (@WorkerA, @Marker, @SourceTrackId, 1, @Now);
DECLARE @AsgA INT = SCOPE_IDENTITY();

INSERT INTO ProtonDeliverableProgresses
    (CoacheeId, ProtonDeliverableId, ProtonTrackAssignmentId, Status, CreatedAt, ApprovedAt, HCApprovalStatus, SrSpvApprovalStatus, ShApprovalStatus)
VALUES
    (@WorkerA, @Deliv1, @AsgA, N'Approved', @Now, @Now, N'Reviewed', N'Approved', N'Approved'),
    (@WorkerA, @Deliv2, @AsgA, N'Approved', @Now, @Now, N'Reviewed', N'Approved', N'Approved');

INSERT INTO ProtonFinalAssessments
    (CoacheeId, CreatedById, ProtonTrackAssignmentId, Status, CompetencyLevelGranted, Notes, Origin, CreatedAt, CompletedAt)
VALUES
    (@WorkerA, @AdminId, @AsgA, N'Completed', 0, N'Phase 361 Bypass Fixture - final Worker A', N'Exam', @Now, @Now);

PRINT N'[Phase 361 seed] Worker A (komplit, CL-A) inserted. AsgId=' + CAST(@AsgA AS NVARCHAR(10));

-- ---------- Worker B: partial -> CL-B(a)/(b) ----------
INSERT INTO ProtonTrackAssignments (CoacheeId, AssignedById, ProtonTrackId, IsActive, AssignedAt)
VALUES (@WorkerB, @Marker, @SourceTrackId, 1, @Now);
DECLARE @AsgB INT = SCOPE_IDENTITY();

INSERT INTO ProtonDeliverableProgresses
    (CoacheeId, ProtonDeliverableId, ProtonTrackAssignmentId, Status, CreatedAt, ApprovedAt, HCApprovalStatus, SrSpvApprovalStatus, ShApprovalStatus)
VALUES
    (@WorkerB, @Deliv1, @AsgB, N'Approved', @Now, @Now, N'Reviewed', N'Approved', N'Approved'),
    (@WorkerB, @Deliv2, @AsgB, N'Pending', @Now, NULL, N'Pending', N'Pending', N'Pending');

PRINT N'[Phase 361 seed] Worker B (partial, CL-B) inserted. AsgId=' + CAST(@AsgB AS NVARCHAR(10));

-- ---------- Worker C: punya final -> CL-B ditolak (D-D) ----------
INSERT INTO ProtonTrackAssignments (CoacheeId, AssignedById, ProtonTrackId, IsActive, AssignedAt)
VALUES (@WorkerC, @Marker, @SourceTrackId, 1, @Now);
DECLARE @AsgC INT = SCOPE_IDENTITY();

INSERT INTO ProtonDeliverableProgresses
    (CoacheeId, ProtonDeliverableId, ProtonTrackAssignmentId, Status, CreatedAt, ApprovedAt, HCApprovalStatus, SrSpvApprovalStatus, ShApprovalStatus)
VALUES
    (@WorkerC, @Deliv1, @AsgC, N'Approved', @Now, @Now, N'Reviewed', N'Approved', N'Approved'),
    (@WorkerC, @Deliv2, @AsgC, N'Submitted', @Now, NULL, N'Pending', N'Pending', N'Pending');

INSERT INTO ProtonFinalAssessments
    (CoacheeId, CreatedById, ProtonTrackAssignmentId, Status, CompetencyLevelGranted, Notes, Origin, CreatedAt, CompletedAt)
VALUES
    (@WorkerC, @AdminId, @AsgC, N'Completed', 0, N'Phase 361 Bypass Fixture - final Worker C', N'Interview', @Now, @Now);

PRINT N'[Phase 361 seed] Worker C (punya final, CL-B ditolak) inserted. AsgId=' + CAST(@AsgC AS NVARCHAR(10));

-- ---------- Worker D: pending CL-B(b) Menunggu + exam belum lulus (E5) ----------
INSERT INTO ProtonTrackAssignments (CoacheeId, AssignedById, ProtonTrackId, IsActive, AssignedAt)
VALUES (@WorkerD, @Marker, @SourceTrackId, 1, @Now);
DECLARE @AsgD INT = SCOPE_IDENTITY();

INSERT INTO AssessmentSessions
    (UserId, Title, Category, Schedule, DurationMinutes, Status, Progress, BannerColor,
     PassPercentage, AllowAnswerReview, GenerateCertificate, IsPassed, CompletedAt, StartedAt,
     ElapsedSeconds, IsTokenRequired, AccessToken, CreatedAt, IsManualEntry, ProtonTrackId, TahunKe)
VALUES
    (@WorkerD, N'Phase 361 Bypass Fixture - Exam CL-B(b) Worker D', N'Assessment Proton', @Now, 60, N'Open', 0, N'bg-primary',
     70, 1, 0, NULL, NULL, NULL,
     0, 0, N'', @Now, 0, @SourceTrackId, N'Tahun 1');
DECLARE @SessD INT = SCOPE_IDENTITY();

INSERT INTO PendingProtonBypasses
    (CoacheeId, SourceProtonTrackId, TargetProtonTrackId, TargetUnit, TargetCoachId,
     Reason, LinkedAssessmentSessionId, Status, InitiatedById, CreatedAt, ResolvedAt)
VALUES
    (@WorkerD, @SourceTrackId, @TargetTrackId, N'Alkylation Unit (065)', NULL,
     N'Phase 361 Bypass Fixture - pending CL-B(b)', @SessD, N'Menunggu', @AdminId, @Now, NULL);

PRINT N'[Phase 361 seed] Worker D (pending Menunggu, E5) inserted. AsgId=' + CAST(@AsgD AS NVARCHAR(10)) + N' SessId=' + CAST(@SessD AS NVARCHAR(10));

COMMIT;
PRINT N'[Phase 361 seed] COMMIT sukses.';

-- ============================================================
-- 4. Verification SELECT (post-COMMIT, read-only)
-- ============================================================
DECLARE @cA INT = (SELECT COUNT(*) FROM ProtonFinalAssessments fa INNER JOIN ProtonTrackAssignments a ON fa.ProtonTrackAssignmentId=a.Id WHERE a.AssignedById=@Marker AND a.CoacheeId=@WorkerA);
DECLARE @cB INT = (SELECT COUNT(*) FROM ProtonFinalAssessments fa INNER JOIN ProtonTrackAssignments a ON fa.ProtonTrackAssignmentId=a.Id WHERE a.AssignedById=@Marker AND a.CoacheeId=@WorkerB);
DECLARE @cC INT = (SELECT COUNT(*) FROM ProtonFinalAssessments fa INNER JOIN ProtonTrackAssignments a ON fa.ProtonTrackAssignmentId=a.Id WHERE a.AssignedById=@Marker AND a.CoacheeId=@WorkerC);
DECLARE @cD INT = (SELECT COUNT(*) FROM PendingProtonBypasses WHERE Reason LIKE N'Phase 361%' AND Status=N'Menunggu' AND CoacheeId=@WorkerD);
DECLARE @cAsg INT = (SELECT COUNT(*) FROM ProtonTrackAssignments WHERE AssignedById=@Marker AND IsActive=1);

PRINT N'[Phase 361 verify] Assignment aktif fixture = ' + CAST(@cAsg AS NVARCHAR(10)) + N' (expect 4)';
PRINT N'[Phase 361 verify] Worker A final = ' + CAST(@cA AS NVARCHAR(10)) + N' (expect 1 -> CL-A eligible)';
PRINT N'[Phase 361 verify] Worker B final = ' + CAST(@cB AS NVARCHAR(10)) + N' (expect 0 -> CL-B eligible)';
PRINT N'[Phase 361 verify] Worker C final = ' + CAST(@cC AS NVARCHAR(10)) + N' (expect 1 -> CL-B ditolak D-D)';
PRINT N'[Phase 361 verify] Worker D pending Menunggu = ' + CAST(@cD AS NVARCHAR(10)) + N' (expect 1 -> badge Menunggu Exam)';

IF @cAsg <> 4 OR @cA <> 1 OR @cB <> 0 OR @cC <> 1 OR @cD <> 1
    THROW 50010, 'Phase 361 verify GAGAL - state fixture tidak sesuai ekspektasi.', 1;

PRINT N'[Phase 361 seed] SELESAI - 4 worker multi-state siap untuk e2e bypass.';
