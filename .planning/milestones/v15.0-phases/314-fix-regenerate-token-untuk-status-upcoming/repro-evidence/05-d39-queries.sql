-- ============================================================================
-- Phase 314 Plan 01 Task 3 — D-39 Data Shape Baseline Queries
-- Eksekusi di DB Dev via SSMS/DBeaver dengan READ-ONLY credential.
-- JANGAN UPDATE/DELETE/INSERT. Output tabular masuk ke 06-d39-results.txt.
-- ============================================================================

-- (a) Schedule MinValue check (validate D-37/D-38 conditional guard)
-- Decision rule:
--   N = 0 → SKIP D-38 guard di Plan 02 (CreateAssessment datepicker prevent)
--   N > 0 → APPLY D-38 guard + suggest data cleanup script ke Team IT
SELECT COUNT(*) AS MinValueCount
FROM AssessmentSessions
WHERE Schedule = '0001-01-01';

-- Optional drilldown jika count > 0:
-- SELECT TOP 10 Id, Title, Category, Schedule, Status, IsTokenRequired, CreatedAt
-- FROM AssessmentSessions
-- WHERE Schedule = '0001-01-01'
-- ORDER BY CreatedAt DESC;


-- (b) Title duplicate sample (case/whitespace test — validate D-29 SQL Server CI_AS sufficient)
-- Decision rule:
--   Duplicates dengan whitespace/case variation → audit-only (D-32), TIDAK ubah matching
--   Zero duplicates → D-29 risk NONE
SELECT TOP 10 Title, Category, COUNT(*) AS Cnt
FROM AssessmentSessions
GROUP BY Title, Category
HAVING COUNT(*) > 1
ORDER BY Cnt DESC;


-- (c) Category NULL/empty check (validate D-31 [Required] non-null assumption)
-- Decision rule:
--   N = 0 → D-31 [Required] enforced; existing matching `a.Category == assessment.Category` safe
--   N > 0 → audit-only di RESEARCH (D-32). Defer Category null-coalesce ke Deferred Ideas
SELECT
    SUM(CASE WHEN Category IS NULL THEN 1 ELSE 0 END) AS CategoryNullCount,
    SUM(CASE WHEN Category = '' THEN 1 ELSE 0 END) AS CategoryEmptyCount,
    SUM(CASE WHEN Category IS NOT NULL AND Category <> '' THEN 1 ELSE 0 END) AS CategoryValidCount
FROM AssessmentSessions;


-- (d) Trigger condition distribution (Status='Upcoming' + IsTokenRequired=1 + 0 worker started)
-- NOTE: AssessmentSessions.StartedAt adalah field di entity AssessmentSession (per Models/AssessmentSession.cs:40),
-- bukan di tabel UserAssessmentSessions. Query disesuaikan ke schema actual.
-- Decision rule:
--   TriggerConditionRows > 0 → fixture seed feasible di Dev (existing rows match trigger)
--   TriggerConditionRows = 0 → repro Task 1 harus create fixture sendiri via UI
SELECT
    Status,
    COUNT(*) AS TotalRows,
    SUM(CASE WHEN IsTokenRequired = 1 THEN 1 ELSE 0 END) AS TokenRequiredRows,
    SUM(CASE WHEN IsTokenRequired = 1 AND StartedAt IS NULL THEN 1 ELSE 0 END) AS TriggerConditionRows
FROM AssessmentSessions
GROUP BY Status
ORDER BY TotalRows DESC;

-- Sample 10 row yang persis matching trigger condition (untuk repro candidate selection):
SELECT TOP 10 Id, Title, Category, Schedule, Status, IsTokenRequired, StartedAt, CreatedAt
FROM AssessmentSessions
WHERE Status = 'Upcoming'
  AND IsTokenRequired = 1
  AND StartedAt IS NULL
ORDER BY CreatedAt DESC;


-- (e) Sibling group size statistics (Title + Category + Schedule.Date)
-- Decision rule:
--   Min = 0 → invariant violation (D-33 0-row guard MANDATORY)
--   Min = 1 → single-sibling normal (Online type)
--   Max > 5 → multi-sibling exists (PrePost groups), test harus cover > 1 sibling
SELECT
    AVG(CAST(GroupSize AS FLOAT)) AS AvgGroupSize,
    MIN(GroupSize) AS MinGroupSize,
    MAX(GroupSize) AS MaxGroupSize,
    COUNT(*) AS TotalGroups
FROM (
    SELECT Title, Category, CAST(Schedule AS DATE) AS ScheduleDate, COUNT(*) AS GroupSize
    FROM AssessmentSessions
    GROUP BY Title, Category, CAST(Schedule AS DATE)
) AS GroupStats;
