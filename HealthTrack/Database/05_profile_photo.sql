IF COL_LENGTH('dbo.ApplicationUsers', 'ProfilePhotoPath') IS NULL
BEGIN
    ALTER TABLE dbo.ApplicationUsers ADD ProfilePhotoPath NVARCHAR(255) NULL;
END;
GO
