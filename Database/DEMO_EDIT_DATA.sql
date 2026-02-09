-- ============================================================
-- DEMO: Cara Edit Data di SQL Server Management Studio (SSMS)
-- Database: HcPortalDB_Dev
-- ============================================================

-- ============================================================
-- BAGIAN 1: MELIHAT DATA (SELECT)
-- ============================================================

-- 1.1. Lihat semua data di tabel KkjMatrices
SELECT * FROM KkjMatrices
-- Hasilnya: 17 rows (data KKJ Matrix yang sudah di-seed)

-- 1.2. Lihat data dengan kolom tertentu saja
SELECT Id, No, Kompetensi, SkillGroup 
FROM KkjMatrices

-- 1.3. Lihat data dengan filter
SELECT * FROM KkjMatrices 
WHERE SkillGroup = 'Engineering'

-- 1.4. Lihat data dengan urutan
SELECT * FROM KkjMatrices 
ORDER BY No ASC

-- 1.5. Lihat jumlah data
SELECT COUNT(*) AS TotalKKJ FROM KkjMatrices

-- 1.6. Lihat 5 data pertama
SELECT TOP 5 * FROM KkjMatrices


-- ============================================================
-- BAGIAN 2: TAMBAH DATA (INSERT)
-- ============================================================

-- 2.1. Tambah 1 data baru ke KkjMatrices
INSERT INTO KkjMatrices (
    No, SkillGroup, SubSkillGroup, Indeks, Kompetensi,
    Target_SectionHead, Target_SrSpv_GSH, Target_ShiftSpv_GSH,
    Target_Panelman_GSH_12_13, Target_Panelman_GSH_14,
    Target_Operator_GSH_8_11, Target_Operator_GSH_12_13,
    Target_ShiftSpv_ARU, Target_Panelman_ARU_12_13,
    Target_Panelman_ARU_14, Target_Operator_ARU_8_11,
    Target_Operator_ARU_12_13, Target_SrSpv_Facility,
    Target_JrAnalyst, Target_HSE
)
VALUES (
    18, 'Engineering', 'Safety Engineering', '12.3.1', 'Emergency Response Planning',
    '3', '2', '2', '-', '-', '-', '-', '2', '-', '-', '-', '-', '2', '-', '3'
)

-- Cek apakah data berhasil ditambahkan
SELECT * FROM KkjMatrices WHERE No = 18


-- 2.2. Tambah data ke CpdpItems
INSERT INTO CpdpItems (
    No, NamaKompetensi, IndikatorPerilaku, DetailIndikator,
    Silabus, TargetDeliverable, Status
)
VALUES (
    '6', 'Demo Competency', '1 (Basic)', 'Memahami konsep dasar demo',
    '6.1. Introduction to Demo', 'Target: Mampu memahami demo', ''
)

-- Cek hasil
SELECT * FROM CpdpItems WHERE No = '6'


-- ============================================================
-- BAGIAN 3: EDIT DATA (UPDATE)
-- ============================================================

-- 3.1. Update 1 data berdasarkan ID
UPDATE KkjMatrices 
SET Kompetensi = 'Gas Processing Operations - UPDATED'
WHERE Id = 1

-- Cek hasil update
SELECT * FROM KkjMatrices WHERE Id = 1


-- 3.2. Update multiple kolom sekaligus
UPDATE KkjMatrices
SET Kompetensi = 'Emergency Response Planning - REVISED',
    Target_HSE = '3',
    Target_SectionHead = '3'
WHERE No = 18

-- Cek hasil
SELECT * FROM KkjMatrices WHERE No = 18


-- 3.3. Update berdasarkan kondisi (multiple rows)
UPDATE CpdpItems
SET Status = 'Active'
WHERE IndikatorPerilaku = '1 (Basic)'

-- Cek berapa row yang ter-update
SELECT * FROM CpdpItems WHERE Status = 'Active'


-- ============================================================
-- BAGIAN 4: HAPUS DATA (DELETE)
-- ============================================================

-- 4.1. Hapus 1 data berdasarkan ID
-- HATI-HATI: Pastikan WHERE clause benar!
DELETE FROM KkjMatrices WHERE No = 18

-- Cek apakah sudah terhapus
SELECT * FROM KkjMatrices WHERE No = 18
-- Hasilnya: 0 rows (sudah terhapus)


-- 4.2. Hapus data dengan kondisi
DELETE FROM CpdpItems WHERE No = '6'

-- Cek hasil
SELECT * FROM CpdpItems WHERE No = '6'


-- ⚠️ PERINGATAN: Jangan jalankan query ini kecuali Anda yakin!
-- DELETE FROM KkjMatrices  -- Ini akan hapus SEMUA data!


-- ============================================================
-- BAGIAN 5: QUERY GABUNGAN (JOIN)
-- ============================================================

-- 5.1. Lihat Training Records dengan nama User
SELECT 
    tr.Id,
    tr.Judul,
    tr.Kategori,
    tr.Status,
    tr.Tanggal,
    u.FullName AS NamaUser,
    u.Email,
    u.Position
FROM TrainingRecords tr
INNER JOIN Users u ON tr.UserId = u.Id
ORDER BY tr.Tanggal DESC


-- 5.2. Lihat berapa training per user
SELECT 
    u.FullName,
    u.Email,
    COUNT(tr.Id) AS JumlahTraining
FROM Users u
LEFT JOIN TrainingRecords tr ON u.Id = tr.UserId
GROUP BY u.FullName, u.Email
ORDER BY JumlahTraining DESC


-- ============================================================
-- BAGIAN 6: QUERY BERGUNA LAINNYA
-- ============================================================

-- 6.1. Lihat struktur tabel
EXEC sp_help 'KkjMatrices'

-- 6.2. Lihat semua tabel di database
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME

-- 6.3. Lihat jumlah data di setiap tabel
SELECT 
    t.name AS TableName,
    SUM(p.rows) AS RowCount
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0,1)
GROUP BY t.name
ORDER BY RowCount DESC


-- ============================================================
-- BAGIAN 7: BACKUP & RESTORE
-- ============================================================

-- 7.1. Backup data sebelum edit besar
SELECT * INTO KkjMatrices_Backup FROM KkjMatrices

-- 7.2. Restore dari backup
-- DELETE FROM KkjMatrices
-- INSERT INTO KkjMatrices SELECT * FROM KkjMatrices_Backup

-- 7.3. Hapus tabel backup
-- DROP TABLE KkjMatrices_Backup


-- ============================================================
-- TIPS PENTING
-- ============================================================

/*
1. SELALU gunakan WHERE clause saat UPDATE/DELETE
   ❌ DELETE FROM KkjMatrices
   ✅ DELETE FROM KkjMatrices WHERE Id = 18

2. Test query SELECT dulu sebelum UPDATE/DELETE
   SELECT * FROM KkjMatrices WHERE Id = 18  -- Cek dulu
   DELETE FROM KkjMatrices WHERE Id = 18    -- Baru hapus

3. Gunakan TRANSACTION untuk perubahan besar
   BEGIN TRANSACTION
   UPDATE KkjMatrices SET ...
   -- Cek hasilnya
   SELECT * FROM KkjMatrices
   -- Jika OK:
   COMMIT
   -- Jika salah:
   ROLLBACK

4. Backup dulu sebelum perubahan besar
   SELECT * INTO TableName_Backup FROM TableName
*/
