-- Phase 413-02 e2e seed (temporary + local-only) — flip 1 sesi (yang PUNYA paket soal,
-- bukan Pre/Post pair, dimiliki account test 'rino.prasetyo') ke InProgress agar worker
-- bisa Resume /CMP/StartExam/{id} di Playwright multi-context (force-kick signal).
--
-- Pitfall 1 (412-03): HANYA pilih sesi yang punya AssessmentPackages — sesi tanpa paket
-- render "tidak memiliki paket soal" → StartExam redirect.
-- Pitfall 4: sesi harus dimiliki account login worker (rino) supaya StartExam authorize+Resume.
--
-- Strategi: resolve TOP 1 sesi non-Proton, RemovedAt NULL, BUKAN bagian Pre/Post pair
-- (LinkedGroupId NULL → simple single-session soft-remove, hindari pair-as-unit), PUNYA paket,
-- dimiliki rino.prasetyo@pertamina.com. Lalu flip ke InProgress + window +1 hari.
--
-- Output (resolved via queryScalar/queryString di spec, BUKAN dari script ini):
--   inProgressSessionId, batchTitle, batchCategory, batchScheduleDate.
--
-- Semua mutasi in-place → revert via RESTORE snapshot (afterAll). Tidak ada INSERT.
-- Data/SeedData.cs tak tersentuh.

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @rinoId NVARCHAR(450) = (SELECT TOP 1 Id FROM Users WHERE Email = 'rino.prasetyo@pertamina.com');
IF @rinoId IS NULL
    THROW 51413, 'Seed 413: user rino.prasetyo@pertamina.com tidak ditemukan.', 1;

DECLARE @sid INT = (
    SELECT TOP 1 s.Id
    FROM AssessmentSessions s
    WHERE EXISTS (SELECT 1 FROM AssessmentPackages p WHERE p.AssessmentSessionId = s.Id)
      AND s.RemovedAt IS NULL
      AND s.Category <> 'Assessment Proton'
      AND s.LinkedGroupId IS NULL            -- BUKAN Pre/Post pair (hindari pair-as-unit removal)
      AND s.UserId = @rinoId                 -- dimiliki account login worker (Resume authorize)
    ORDER BY s.Id DESC
);
IF @sid IS NULL
    THROW 51413, 'Seed 413: tidak ada sesi (punya paket, non-Proton, non-pair, milik rino) untuk di-flip InProgress.', 1;

-- Flip ke InProgress + buka jendela ujian +1 hari (412-03 pola sukses).
UPDATE AssessmentSessions
SET Status = 'InProgress',
    StartedAt = GETDATE(),
    CompletedAt = NULL,
    ExamWindowCloseDate = DATEADD(day, 1, GETDATE())
WHERE Id = @sid;

-- UPA belum tentu ada (sesi Open bisa belum punya UPA) — update bila ada (lazy-create saat StartExam).
UPDATE UserPackageAssignments SET IsCompleted = 0 WHERE AssessmentSessionId = @sid;

-- Echo sesi terpilih untuk audit log run (informational).
SELECT s.Id            AS InProgressSessionId,
       s.Title         AS BatchTitle,
       s.Category      AS BatchCategory,
       CONVERT(varchar(10), s.Schedule, 23) AS BatchScheduleDate
FROM AssessmentSessions s WHERE s.Id = @sid;
