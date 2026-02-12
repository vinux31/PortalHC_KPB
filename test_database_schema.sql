-- Test 1: Check indexes exist
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc AS IndexType,
    is_unique AS IsUnique
FROM sys.indexes 
WHERE object_id = OBJECT_ID('AssessmentSessions')
ORDER BY name;

-- Test 2: Check audit fields exist
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AssessmentSessions' 
AND COLUMN_NAME IN ('CreatedAt', 'UpdatedAt', 'CreatedBy', 'Type')
ORDER BY COLUMN_NAME;

-- Test 3: Check check constraints exist
SELECT name AS ConstraintName, definition
FROM sys.check_constraints 
WHERE parent_object_id = OBJECT_ID('AssessmentSessions');
