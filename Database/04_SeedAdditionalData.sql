-- =============================================
-- Phase C: Seed Additional Data
-- Tables: AssessmentSessions, CoachingLogs, IdpItems
-- =============================================

USE HcPortalDb_Dev;

-- =============================================
-- 1. SEED ASSESSMENT SESSIONS (~12 sessions)
-- =============================================

-- Get user IDs for reference
DECLARE @RustamId NVARCHAR(450);
DECLARE @IwanId NVARCHAR(450);
DECLARE @TaufikId NVARCHAR(450);
DECLARE @ChoirulId NVARCHAR(450);

SELECT @RustamId = Id FROM dbo.AspNetUsers WHERE Email = 'rustam.nugroho@pertamina.com';
SELECT @IwanId = Id FROM dbo.AspNetUsers WHERE Email = 'iwan3@pertamina.com';
SELECT @TaufikId = Id FROM dbo.AspNetUsers WHERE Email = 'taufik.hartopo@pertamina.com';
SELECT @ChoirulId = Id FROM dbo.AspNetUsers WHERE Email = 'choirul.anam@pertamina.com';

-- Clear existing data (optional - for clean slate)
DELETE FROM AssessmentSessions;
DBCC CHECKIDENT ('AssessmentSessions', RESEED, 0);

-- Insert Assessment Sessions
INSERT INTO AssessmentSessions (UserId, Title, Category, Type, Schedule, DurationMinutes, Status, Progress, BannerColor, Score, IsTokenRequired)
VALUES
-- Rustam's Assessments (Coach)
(@RustamId, 'PROTON Assessment: Distillation Operations', 'Assessment OJ', 'Assessment OJ', '2024-11-15 09:00:00', 180, 'Completed', 100, 'bg-success', 85, 1),
(@RustamId, 'OTS Simulation: Emergency Shutdown', 'OTS', 'OTS', '2024-10-05 14:00:00', 120, 'Completed', 100, 'bg-success', 90, 0),
(@RustamId, 'IHT: Pump Maintenance Training', 'IHT', 'IHT', '2024-07-18 10:00:00', 240, 'Completed', 100, 'bg-success', 88, 0),
(@RustamId, 'Mandatory HSSE: Fire Fighting', 'Mandatory HSSE Training', 'Mandatory HSSE Training', '2025-02-20 08:00:00', 480, 'Upcoming', 0, 'bg-warning', NULL, 1),

-- Iwan's Assessments (Operator/Coachee)
(@IwanId, 'PROTON Assessment: Heat Exchanger Systems', 'Assessment OJ', 'Assessment OJ', '2025-01-10 09:00:00', 180, 'Completed', 100, 'bg-success', 78, 1),
(@IwanId, 'OTS Simulation: Blackout Recovery', 'OTS', 'OTS', '2024-12-20 14:00:00', 120, 'Completed', 100, 'bg-success', 82, 0),
(@IwanId, 'On Job Training: Panel Operator', 'Assessment OJ', 'Assessment OJ', '2024-09-12 10:00:00', 300, 'Completed', 100, 'bg-success', 85, 0),
(@IwanId, 'Mandatory HSSE: Working at Height', 'Mandatory HSSE Training', 'Mandatory HSSE Training', '2025-03-15 08:00:00', 480, 'Open', 0, 'bg-primary', NULL, 1),

-- Taufik's Assessments (Section Head)
(@TaufikId, 'Licencor Training: Boiler Class 1', 'Licencor', 'Licencor', '2023-08-15 09:00:00', 600, 'Completed', 100, 'bg-success', 92, 1),
(@TaufikId, 'IHT: Process Control Systems', 'IHT', 'IHT', '2024-06-30 10:00:00', 240, 'Completed', 100, 'bg-success', 90, 0),

-- Choirul's Assessments (Sr Supervisor)
(@ChoirulId, 'PROTON Assessment: Reactor Operations', 'Assessment OJ', 'Assessment OJ', '2024-08-20 09:00:00', 180, 'Completed', 100, 'bg-success', 87, 1),
(@ChoirulId, 'Mandatory HSSE: Gas Tester Certification', 'Mandatory HSSE Training', 'Mandatory HSSE Training', '2024-01-20 08:00:00', 480, 'Completed', 100, 'bg-success', 95, 1);

PRINT '✅ Seeded ' + CAST(@@ROWCOUNT AS VARCHAR) + ' Assessment Sessions';

-- =============================================
-- 2. SEED COACHING LOGS (~18 logs)
-- =============================================

-- Clear existing data (optional)
DELETE FROM CoachingLogs;
DBCC CHECKIDENT ('CoachingLogs', RESEED, 0);

-- Insert Coaching Logs
-- Note: TrackingItemId is set to 1 for now (you may need to adjust based on your TrackingItems table)
INSERT INTO CoachingLogs (TrackingItemId, CoachId, CoachName, CoachPosition, CoacheeId, CoacheeName, SubKompetensi, Deliverables, Tanggal, CoacheeCompetencies, CatatanCoach, Kesimpulan, Result, Status, CreatedAt, UpdatedAt)
VALUES
-- Rustam coaching Iwan (6 sessions)
(1, @RustamId, 'Rustam Santiko', 'Coach', @IwanId, 'Iwan', 
 'Operasi Distilasi', 'Mampu mengoperasikan kolom distilasi secara mandiri', 
 '2024-11-01', 'Sudah memahami prinsip dasar distilasi, mampu membaca parameter dengan baik', 
 'Coachee menunjukkan pemahaman yang baik. Perlu latihan lebih untuk situasi emergency.', 
 'PerluDikembangkan', 'Good', 'Submitted', '2024-11-01', '2024-11-01'),

(1, @RustamId, 'Rustam Santiko', 'Coach', @IwanId, 'Iwan', 
 'Troubleshooting Pompa', 'Mampu mengidentifikasi masalah pompa dan melakukan troubleshooting dasar', 
 '2024-11-08', 'Dapat mengidentifikasi masalah umum pada pompa, memahami prosedur troubleshooting', 
 'Coachee sudah cukup mandiri dalam troubleshooting pompa sentrifugal.', 
 'Mandiri', 'Suitable', 'Submitted', '2024-11-08', '2024-11-08'),

(1, @RustamId, 'Rustam Santiko', 'Coach', @IwanId, 'Iwan', 
 'Safety Procedures', 'Memahami dan menerapkan prosedur keselamatan kerja', 
 '2024-11-15', 'Sangat memahami prosedur safety, selalu menggunakan APD dengan benar', 
 'Coachee menunjukkan komitmen tinggi terhadap safety. Excellent performance.', 
 'Mandiri', 'Excellence', 'Submitted', '2024-11-15', '2024-11-15'),

(1, @RustamId, 'Rustam Santiko', 'Coach', @IwanId, 'Iwan', 
 'Heat Exchanger Operations', 'Mampu mengoperasikan dan memonitor heat exchanger', 
 '2024-11-22', 'Memahami prinsip perpindahan panas, dapat membaca temperature profile', 
 'Coachee perlu lebih banyak praktek untuk cleaning procedures.', 
 'PerluDikembangkan', 'NeedImprovement', 'Submitted', '2024-11-22', '2024-11-22'),

(1, @RustamId, 'Rustam Santiko', 'Coach', @IwanId, 'Iwan', 
 'Emergency Response', 'Mampu merespon situasi emergency dengan tepat', 
 '2024-12-01', 'Sudah memahami prosedur emergency shutdown, dapat mengambil keputusan dengan cepat', 
 'Coachee menunjukkan kemampuan yang baik dalam simulasi emergency.', 
 'Mandiri', 'Good', 'Submitted', '2024-12-01', '2024-12-01'),

(1, @RustamId, 'Rustam Santiko', 'Coach', @IwanId, 'Iwan', 
 'Process Control', 'Memahami dan mengoperasikan sistem kontrol proses', 
 '2024-12-10', 'Dapat membaca DCS dengan baik, memahami control loop', 
 'Coachee sudah mandiri dalam operasi normal. Perlu latihan untuk tuning controller.', 
 'PerluDikembangkan', 'Suitable', 'Submitted', '2024-12-10', '2024-12-10'),

-- Taufik coaching Choirul (6 sessions)
(1, @TaufikId, 'Taufik Basuki', 'Section Head', @ChoirulId, 'Choirul Anam', 
 'Leadership Skills', 'Mengembangkan kemampuan kepemimpinan tim', 
 '2024-09-15', 'Mampu memimpin tim dengan baik, komunikasi efektif', 
 'Menunjukkan potensi kepemimpinan yang baik. Perlu lebih tegas dalam decision making.', 
 'PerluDikembangkan', 'Good', 'Submitted', '2024-09-15', '2024-09-15'),

(1, @TaufikId, 'Taufik Basuki', 'Section Head', @ChoirulId, 'Choirul Anam', 
 'Budget Management', 'Memahami pengelolaan budget operasional', 
 '2024-09-22', 'Sudah memahami prinsip budget management, dapat membuat forecast', 
 'Coachee menunjukkan pemahaman yang baik tentang cost control.', 
 'Mandiri', 'Suitable', 'Submitted', '2024-09-22', '2024-09-22'),

(1, @TaufikId, 'Taufik Basuki', 'Section Head', @ChoirulId, 'Choirul Anam', 
 'Performance Management', 'Mampu mengelola performance tim', 
 '2024-10-01', 'Dapat melakukan performance review dengan objektif', 
 'Excellent dalam memberikan feedback kepada tim.', 
 'Mandiri', 'Excellence', 'Submitted', '2024-10-01', '2024-10-01'),

(1, @TaufikId, 'Taufik Basuki', 'Section Head', @ChoirulId, 'Choirul Anam', 
 'Strategic Planning', 'Mengembangkan kemampuan perencanaan strategis', 
 '2024-10-15', 'Memahami konsep strategic planning, dapat membuat action plan', 
 'Perlu lebih banyak exposure ke level management untuk pengembangan lebih lanjut.', 
 'PerluDikembangkan', 'Good', 'Submitted', '2024-10-15', '2024-10-15'),

(1, @TaufikId, 'Taufik Basuki', 'Section Head', @ChoirulId, 'Choirul Anam', 
 'Conflict Resolution', 'Mampu menyelesaikan konflik dalam tim', 
 '2024-10-22', 'Dapat mengidentifikasi sumber konflik dan mencari solusi win-win', 
 'Coachee menunjukkan kemampuan mediasi yang baik.', 
 'Mandiri', 'Suitable', 'Submitted', '2024-10-22', '2024-10-22'),

(1, @TaufikId, 'Taufik Basuki', 'Section Head', @ChoirulId, 'Choirul Anam', 
 'Change Management', 'Memahami dan mengelola perubahan organisasi', 
 '2024-11-05', 'Sudah memahami prinsip change management, dapat mengkomunikasikan perubahan dengan baik', 
 'Excellent dalam mengelola resistensi terhadap perubahan.', 
 'Mandiri', 'Excellence', 'Submitted', '2024-11-05', '2024-11-05'),

-- Choirul coaching other operators (6 sessions)
(1, @ChoirulId, 'Choirul Anam', 'Sr Supervisor', @IwanId, 'Iwan', 
 'Reactor Operations', 'Memahami operasi reaktor katalitik', 
 '2024-08-10', 'Sudah memahami prinsip dasar reaksi katalitik', 
 'Coachee perlu lebih banyak praktek untuk startup dan shutdown procedures.', 
 'PerluDikembangkan', 'NeedImprovement', 'Submitted', '2024-08-10', '2024-08-10'),

(1, @ChoirulId, 'Choirul Anam', 'Sr Supervisor', @IwanId, 'Iwan', 
 'Catalyst Handling', 'Mampu melakukan handling katalis dengan aman', 
 '2024-08-20', 'Memahami prosedur safety untuk handling katalis', 
 'Coachee menunjukkan awareness yang baik terhadap safety procedures.', 
 'Mandiri', 'Suitable', 'Submitted', '2024-08-20', '2024-08-20'),

(1, @ChoirulId, 'Choirul Anam', 'Sr Supervisor', @IwanId, 'Iwan', 
 'Process Optimization', 'Memahami optimasi parameter proses', 
 '2024-09-01', 'Dapat mengidentifikasi parameter kunci untuk optimasi', 
 'Coachee menunjukkan analytical thinking yang baik.', 
 'Mandiri', 'Good', 'Submitted', '2024-09-01', '2024-09-01'),

(1, @ChoirulId, 'Choirul Anam', 'Sr Supervisor', @IwanId, 'Iwan', 
 'Quality Control', 'Memahami prosedur quality control produk', 
 '2024-09-15', 'Dapat melakukan sampling dan basic testing', 
 'Coachee sudah mandiri dalam quality control rutin.', 
 'Mandiri', 'Suitable', 'Submitted', '2024-09-15', '2024-09-15'),

(1, @ChoirulId, 'Choirul Anam', 'Sr Supervisor', @IwanId, 'Iwan', 
 'Equipment Maintenance', 'Memahami preventive maintenance equipment', 
 '2024-09-25', 'Sudah memahami maintenance schedule dan dapat melakukan inspection', 
 'Excellent dalam dokumentasi maintenance activities.', 
 'Mandiri', 'Excellence', 'Submitted', '2024-09-25', '2024-09-25'),

(1, @ChoirulId, 'Choirul Anam', 'Sr Supervisor', @IwanId, 'Iwan', 
 'Environmental Compliance', 'Memahami environmental regulations dan compliance', 
 '2024-10-05', 'Memahami environmental standards, dapat melakukan monitoring emisi', 
 'Coachee menunjukkan komitmen terhadap environmental protection.', 
 'Mandiri', 'Good', 'Submitted', '2024-10-05', '2024-10-05');

PRINT '✅ Seeded ' + CAST(@@ROWCOUNT AS VARCHAR) + ' Coaching Logs';

-- =============================================
-- 3. SEED IDP ITEMS (~12 items)
-- =============================================

-- Clear existing data (optional)
DELETE FROM IdpItems;
DBCC CHECKIDENT ('IdpItems', RESEED, 0);

-- Insert IDP Items
INSERT INTO IdpItems (UserId, Kompetensi, SubKompetensi, Deliverable, Aktivitas, Metode, DueDate, Status, Evidence, ApproveSrSpv, ApproveSectionHead, ApproveHC)
VALUES
-- Iwan's IDP (4 items)
(@IwanId, 'Operasi Unit Proses', 'Distilasi', 'Mampu mengoperasikan kolom distilasi secara mandiri', 
 'On Job Training dengan Coach', 'Coaching & Mentoring', '2025-03-31', 'In Progress', 
 NULL, 'Approved', 'Approved', 'Pending'),

(@IwanId, 'Troubleshooting', 'Pompa & Kompresor', 'Mampu melakukan troubleshooting pompa dan kompresor', 
 'IHT Pump Maintenance', 'Internal Training', '2025-04-30', 'Not Started', 
 NULL, 'Approved', 'Pending', 'Not Started'),

(@IwanId, 'Safety & Environment', 'Emergency Response', 'Mampu merespon emergency dengan tepat', 
 'Emergency Response Drill', 'Simulation & Drill', '2025-05-31', 'Not Started', 
 NULL, 'Pending', 'Not Started', 'Not Started'),

(@IwanId, 'Process Control', 'DCS Operation', 'Mampu mengoperasikan DCS dengan mahir', 
 'OTS Simulation Training', 'OTS Training', '2025-06-30', 'Not Started', 
 NULL, 'Pending', 'Not Started', 'Not Started'),

-- Rustam's IDP (4 items)
(@RustamId, 'Leadership', 'Team Management', 'Mengembangkan kemampuan memimpin tim', 
 'Leadership Training Program', 'External Training', '2025-04-30', 'In Progress', 
 NULL, 'Approved', 'Approved', 'Approved'),

(@RustamId, 'Technical Expertise', 'Advanced Process Control', 'Menguasai advanced process control techniques', 
 'APC Training & Implementation', 'External Training + Project', '2025-07-31', 'Not Started', 
 NULL, 'Approved', 'Approved', 'Pending'),

(@RustamId, 'Coaching Skills', 'Effective Coaching', 'Meningkatkan kemampuan coaching', 
 'Coach the Coach Program', 'Internal Training', '2025-05-31', 'In Progress', 
 '/evidence/coach-training-cert.pdf', 'Approved', 'Approved', 'Approved'),

(@RustamId, 'Innovation', 'Process Improvement', 'Mengimplementasikan improvement project', 
 'Kaizen Project Implementation', 'Project Based', '2025-09-30', 'Not Started', 
 NULL, 'Pending', 'Not Started', 'Not Started'),

-- Choirul's IDP (4 items)
(@ChoirulId, 'Managerial Skills', 'Budget Management', 'Menguasai budget planning dan control', 
 'Budget Management Training', 'External Training', '2025-03-31', 'In Progress', 
 NULL, 'Approved', 'Approved', 'Approved'),

(@ChoirulId, 'Strategic Thinking', 'Strategic Planning', 'Mampu membuat strategic plan unit', 
 'Strategic Planning Workshop', 'Workshop', '2025-06-30', 'Not Started', 
 NULL, 'Approved', 'Pending', 'Not Started'),

(@ChoirulId, 'People Development', 'Performance Management', 'Menguasai performance management system', 
 'Performance Management Training', 'Internal Training', '2025-04-30', 'In Progress', 
 '/evidence/pm-training-cert.pdf', 'Approved', 'Approved', 'Approved'),

(@ChoirulId, 'Change Management', 'Leading Change', 'Mampu memimpin perubahan organisasi', 
 'Change Management Program', 'External Training', '2025-08-31', 'Not Started', 
 NULL, 'Pending', 'Not Started', 'Not Started');

PRINT '✅ Seeded ' + CAST(@@ROWCOUNT AS VARCHAR) + ' IDP Items';

-- =============================================
-- VERIFICATION QUERIES
-- =============================================

DECLARE @CountAssessment INT;
DECLARE @CountCoaching INT;
DECLARE @CountIdp INT;

SELECT @CountAssessment = COUNT(*) FROM AssessmentSessions;
SELECT @CountCoaching = COUNT(*) FROM CoachingLogs;
SELECT @CountIdp = COUNT(*) FROM IdpItems;

PRINT '';
PRINT '=== VERIFICATION ===';
PRINT 'AssessmentSessions: ' + CAST(@CountAssessment AS VARCHAR);
PRINT 'CoachingLogs: ' + CAST(@CountCoaching AS VARCHAR);
PRINT 'IdpItems: ' + CAST(@CountIdp AS VARCHAR);
PRINT '';
PRINT 'Phase C seed data completed successfully!';

GO
