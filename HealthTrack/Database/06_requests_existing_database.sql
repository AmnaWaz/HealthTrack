/* HealthTrack existing-database upgrade. Run after a verified backup.
   Execute against the database configured in Web.config.
   Creates only missing request tables and indexes. Does not delete data.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.ClientProfiles', N'U') IS NULL OR OBJECT_ID(N'dbo.TrainerProfiles', N'U') IS NULL
    THROW 50110, 'ClientProfiles / TrainerProfiles missing. Check the selected database.', 1;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.TrainerRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrainerRequests (
        TrainerRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClientId INT NOT NULL, TrainerId INT NOT NULL,
        Category NVARCHAR(30) NOT NULL, Priority NVARCHAR(10) NOT NULL,
        Subject NVARCHAR(100) NOT NULL, Description NVARCHAR(1000) NOT NULL,
        Status NVARCHAR(20) NOT NULL, TrainerResponse NVARCHAR(1000) NULL,
        CreatedAt DATETIME NOT NULL, UpdatedAt DATETIME NOT NULL,
        RespondedAt DATETIME NULL, ClosedAt DATETIME NULL,
        ClosedByRole NVARCHAR(10) NULL, AiSuggestedPriority NVARCHAR(10) NULL,
        AiReason NVARCHAR(500) NULL, RequiresUrgentReview BIT NOT NULL DEFAULT(0),
        AiAnalysisSource NVARCHAR(20) NULL,
        CONSTRAINT FK_TrainerRequests_Client FOREIGN KEY(ClientId) REFERENCES dbo.ClientProfiles(ClientId),
        CONSTRAINT FK_TrainerRequests_Trainer FOREIGN KEY(TrainerId) REFERENCES dbo.TrainerProfiles(TrainerId)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TrainerRequest_Client' AND object_id=OBJECT_ID('dbo.TrainerRequests'))
    CREATE INDEX IX_TrainerRequest_Client ON dbo.TrainerRequests(ClientId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_TrainerRequest_TrainerStatus' AND object_id=OBJECT_ID('dbo.TrainerRequests'))
    CREATE INDEX IX_TrainerRequest_TrainerStatus ON dbo.TrainerRequests(TrainerId,Status);
IF OBJECT_ID(N'dbo.TrainerSelectionRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrainerSelectionRequests (
        TrainerSelectionRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ClientId INT NOT NULL, TrainerId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL, CreatedAt DATETIME NOT NULL,
        RespondedAt DATETIME NULL, TrainerNote NVARCHAR(500) NULL,
        CONSTRAINT FK_TrainerSelections_Client FOREIGN KEY(ClientId) REFERENCES dbo.ClientProfiles(ClientId),
        CONSTRAINT FK_TrainerSelections_Trainer FOREIGN KEY(TrainerId) REFERENCES dbo.TrainerProfiles(TrainerId)
    );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SelectionRequest_Client' AND object_id=OBJECT_ID('dbo.TrainerSelectionRequests'))
    CREATE INDEX IX_SelectionRequest_Client ON dbo.TrainerSelectionRequests(ClientId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SelectionRequest_TrainerStatus' AND object_id=OBJECT_ID('dbo.TrainerSelectionRequests'))
    CREATE INDEX IX_SelectionRequest_TrainerStatus ON dbo.TrainerSelectionRequests(TrainerId,Status);
COMMIT TRANSACTION;
SELECT name AS TableName FROM sys.tables WHERE name IN ('TrainerRequests','TrainerSelectionRequests');
