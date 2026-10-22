-- Run against your existing HealthTrack database. Read-only checks.
SELECT DB_NAME() AS SelectedDatabase;
SELECT t.name AS TableName
FROM sys.tables AS t
WHERE t.name IN ('Users','ApplicationUsers','TrainerProfiles','ClientProfiles','Appointments',
                 'Notifications','TrainerRequests','TrainerSelectionRequests')
ORDER BY t.name;

SELECT t.name AS TableName, c.name AS ColumnName, ty.name AS DataType, c.max_length AS MaxBytes
FROM sys.tables AS t
JOIN sys.columns AS c ON c.object_id = t.object_id
JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE t.name IN ('Users','ApplicationUsers')
  AND c.name IN ('IsActive','ProfilePhotoPath','RoleId','Email')
ORDER BY t.name, c.name;

SELECT t.name AS TableName, c.name AS ColumnName, c.max_length AS MaxBytes
FROM sys.tables AS t
JOIN sys.columns AS c ON c.object_id = t.object_id
WHERE t.name = 'Notifications' AND c.name IN ('Message','Type');
