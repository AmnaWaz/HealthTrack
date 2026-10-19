/* =====================================================================
   HealthTrack - 02_onboarding_update.sql
   Owner: Person A (Aamina)

   PURPOSE  Adds the tables for multi-select fitness goals, trainer
            specialisations and onboarding drafts, seeds the predefined
            options and converts existing free-text Goal / Specialisation
            values into the new tables.

   SAFE / NON-DESTRUCTIVE
     * Only CREATE TABLE / CREATE INDEX / ADD CONSTRAINT / INSERT / one UPDATE
       that marks seed rows as predefined.
     * No DROP, no DELETE, no ALTER of existing columns.
     * Users, passwords, roles links and existing profile data are not changed.
       (The old Goal / Specialisation columns are left exactly as they are.)
     * Idempotent: every step checks first, so running it twice is harmless.
     * One transaction: if anything fails, everything is rolled back.

   BEFORE RUNNING
     1. Run 01_verify_schema.sql and fix anything marked *** ... ***.
     2. Back up the database (adjust the folder; SQL Server's service account
        must be able to write to it - the default Backup folder always works):
          BACKUP DATABASE [HealthTrack]
            TO DISK = N'C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\Backup\HealthTrack_pre_onboarding.bak'
            WITH INIT, NAME = N'HealthTrack before onboarding update';
   AFTER RUNNING
     3. Run 01_verify_schema.sql again: grids [4] and [13] should be all OK.
   ===================================================================== */
USE [HealthTrack];
SET NOCOUNT ON;
SET XACT_ABORT ON;   -- any error rolls back the whole transaction

/* ---------- 0. Pre-flight: the existing tables/columns this script relies on ---------- */
IF OBJECT_ID(N'dbo.ApplicationUsers', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Roles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ClientProfiles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TrainerProfiles', N'U') IS NULL
    THROW 50001, 'Pre-flight failed: ApplicationUsers, Roles, ClientProfiles or TrainerProfiles is missing. Run 01_verify_schema.sql.', 1;

IF COL_LENGTH(N'dbo.ApplicationUsers', N'UserId') IS NULL
   OR COL_LENGTH(N'dbo.Roles', N'RoleName') IS NULL
   OR COL_LENGTH(N'dbo.ClientProfiles', N'ClientId') IS NULL
   OR COL_LENGTH(N'dbo.ClientProfiles', N'UserId') IS NULL
   OR COL_LENGTH(N'dbo.ClientProfiles', N'Goal') IS NULL
   OR COL_LENGTH(N'dbo.TrainerProfiles', N'TrainerId') IS NULL
   OR COL_LENGTH(N'dbo.TrainerProfiles', N'UserId') IS NULL
   OR COL_LENGTH(N'dbo.TrainerProfiles', N'Specialisation') IS NULL
    THROW 50002, 'Pre-flight failed: an expected column is missing. Run 01_verify_schema.sql (grid [4]).', 1;

-- If a new table already exists from an earlier attempt, it must have the expected shape.
IF OBJECT_ID(N'dbo.FitnessGoals', N'U') IS NOT NULL
   AND (COL_LENGTH(N'dbo.FitnessGoals', N'Name') IS NULL OR COL_LENGTH(N'dbo.FitnessGoals', N'IsPredefined') IS NULL
        OR COL_LENGTH(N'dbo.FitnessGoals', N'CreatedByUserId') IS NULL)
    THROW 50003, 'dbo.FitnessGoals exists but does not have the expected columns. Stop and compare with this script.', 1;
IF OBJECT_ID(N'dbo.Specialisations', N'U') IS NOT NULL
   AND (COL_LENGTH(N'dbo.Specialisations', N'Name') IS NULL OR COL_LENGTH(N'dbo.Specialisations', N'IsPredefined') IS NULL
        OR COL_LENGTH(N'dbo.Specialisations', N'CreatedByUserId') IS NULL)
    THROW 50004, 'dbo.Specialisations exists but does not have the expected columns. Stop and compare with this script.', 1;

BEGIN TRANSACTION;

/* ---------- 1. Lookup tables ---------- */
IF OBJECT_ID(N'dbo.FitnessGoals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FitnessGoals (
        FitnessGoalId   int IDENTITY(1,1) NOT NULL,
        Name            nvarchar(50)      NOT NULL,
        IsPredefined    bit               NOT NULL,
        SortOrder       int               NOT NULL,
        CreatedByUserId int               NULL,
        CreatedAt       datetime          NOT NULL,
        CONSTRAINT [PK_dbo.FitnessGoals] PRIMARY KEY CLUSTERED (FitnessGoalId)
    );
    PRINT 'Created dbo.FitnessGoals';
END;

IF OBJECT_ID(N'dbo.Specialisations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Specialisations (
        SpecialisationId int IDENTITY(1,1) NOT NULL,
        Name             nvarchar(50)      NOT NULL,
        IsPredefined     bit               NOT NULL,
        SortOrder        int               NOT NULL,
        CreatedByUserId  int               NULL,
        CreatedAt        datetime          NOT NULL,
        CONSTRAINT [PK_dbo.Specialisations] PRIMARY KEY CLUSTERED (SpecialisationId)
    );
    PRINT 'Created dbo.Specialisations';
END;

/* ---------- 2. Junction tables (composite keys block duplicate selections) ---------- */
IF OBJECT_ID(N'dbo.ClientGoals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientGoals (
        ClientId      int NOT NULL,
        FitnessGoalId int NOT NULL,
        Position      int NOT NULL,
        CONSTRAINT [PK_dbo.ClientGoals] PRIMARY KEY CLUSTERED (ClientId, FitnessGoalId)
    );
    PRINT 'Created dbo.ClientGoals';
END;

IF OBJECT_ID(N'dbo.TrainerSpecialisations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TrainerSpecialisations (
        TrainerId        int NOT NULL,
        SpecialisationId int NOT NULL,
        Position         int NOT NULL,
        CONSTRAINT [PK_dbo.TrainerSpecialisations] PRIMARY KEY CLUSTERED (TrainerId, SpecialisationId)
    );
    PRINT 'Created dbo.TrainerSpecialisations';
END;

/* ---------- 3. Onboarding drafts (one row per unfinished account) ---------- */
IF OBJECT_ID(N'dbo.OnboardingDrafts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OnboardingDrafts (
        UserId      int           NOT NULL,
        CurrentStep tinyint       NOT NULL,
        DataJson    nvarchar(max) NOT NULL,
        UpdatedAt   datetime      NOT NULL,
        CONSTRAINT [PK_dbo.OnboardingDrafts] PRIMARY KEY CLUSTERED (UserId)
    );
    PRINT 'Created dbo.OnboardingDrafts';
END;

/* ---------- 4. Indexes (names match what Entity Framework would generate) ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FitnessGoals') AND name = N'UX_FitnessGoals_Name')
    CREATE UNIQUE NONCLUSTERED INDEX UX_FitnessGoals_Name ON dbo.FitnessGoals (Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.FitnessGoals') AND name = N'IX_CreatedByUserId')
    CREATE NONCLUSTERED INDEX IX_CreatedByUserId ON dbo.FitnessGoals (CreatedByUserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Specialisations') AND name = N'UX_Specialisations_Name')
    CREATE UNIQUE NONCLUSTERED INDEX UX_Specialisations_Name ON dbo.Specialisations (Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Specialisations') AND name = N'IX_CreatedByUserId')
    CREATE NONCLUSTERED INDEX IX_CreatedByUserId ON dbo.Specialisations (CreatedByUserId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ClientGoals') AND name = N'IX_FitnessGoalId')
    CREATE NONCLUSTERED INDEX IX_FitnessGoalId ON dbo.ClientGoals (FitnessGoalId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TrainerSpecialisations') AND name = N'IX_SpecialisationId')
    CREATE NONCLUSTERED INDEX IX_SpecialisationId ON dbo.TrainerSpecialisations (SpecialisationId);

/* ---------- 5. Foreign keys (NO ACTION on delete, like the rest of the schema) ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.FitnessGoals_dbo.ApplicationUsers_CreatedByUserId')
    ALTER TABLE dbo.FitnessGoals ADD CONSTRAINT [FK_dbo.FitnessGoals_dbo.ApplicationUsers_CreatedByUserId]
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.ApplicationUsers (UserId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.Specialisations_dbo.ApplicationUsers_CreatedByUserId')
    ALTER TABLE dbo.Specialisations ADD CONSTRAINT [FK_dbo.Specialisations_dbo.ApplicationUsers_CreatedByUserId]
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.ApplicationUsers (UserId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.ClientGoals_dbo.ClientProfiles_ClientId')
    ALTER TABLE dbo.ClientGoals ADD CONSTRAINT [FK_dbo.ClientGoals_dbo.ClientProfiles_ClientId]
        FOREIGN KEY (ClientId) REFERENCES dbo.ClientProfiles (ClientId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.ClientGoals_dbo.FitnessGoals_FitnessGoalId')
    ALTER TABLE dbo.ClientGoals ADD CONSTRAINT [FK_dbo.ClientGoals_dbo.FitnessGoals_FitnessGoalId]
        FOREIGN KEY (FitnessGoalId) REFERENCES dbo.FitnessGoals (FitnessGoalId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.TrainerSpecialisations_dbo.TrainerProfiles_TrainerId')
    ALTER TABLE dbo.TrainerSpecialisations ADD CONSTRAINT [FK_dbo.TrainerSpecialisations_dbo.TrainerProfiles_TrainerId]
        FOREIGN KEY (TrainerId) REFERENCES dbo.TrainerProfiles (TrainerId);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.TrainerSpecialisations_dbo.Specialisations_SpecialisationId')
    ALTER TABLE dbo.TrainerSpecialisations ADD CONSTRAINT [FK_dbo.TrainerSpecialisations_dbo.Specialisations_SpecialisationId]
        FOREIGN KEY (SpecialisationId) REFERENCES dbo.Specialisations (SpecialisationId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_dbo.OnboardingDrafts_dbo.ApplicationUsers_UserId')
    ALTER TABLE dbo.OnboardingDrafts ADD CONSTRAINT [FK_dbo.OnboardingDrafts_dbo.ApplicationUsers_UserId]
        FOREIGN KEY (UserId) REFERENCES dbo.ApplicationUsers (UserId);

/* ---------- 6. Roles (fixes databases created without the seed rows) ---------- */
INSERT dbo.Roles (RoleName)
SELECT v.RoleName FROM (VALUES (N'Client'), (N'Trainer'), (N'Admin')) v(RoleName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.RoleName = v.RoleName);
PRINT CONCAT('Roles inserted: ', @@ROWCOUNT);

/* ---------- 7. Predefined options (same lists as HealthTrackInitializer / ProfileOptions.cs) ---------- */
DECLARE @Goals TABLE (Name nvarchar(50), SortOrder int);
INSERT @Goals VALUES
 (N'Build muscle',1),(N'Lose weight',2),(N'Gain weight',3),(N'Increase strength',4),(N'Improve endurance',5),
 (N'Improve flexibility',6),(N'General fitness',7),(N'Sports performance',8),(N'Boxing conditioning',9);

DECLARE @Specs TABLE (Name nvarchar(50), SortOrder int);
INSERT @Specs VALUES
 (N'Strength training',1),(N'Bodybuilding',2),(N'Weight management',3),(N'Cardio and endurance',4),(N'Functional training',5),
 (N'Mobility and flexibility',6),(N'General fitness',7),(N'Sports conditioning',8),(N'Boxing and combat fitness',9),(N'Nutrition coaching',10);

-- A user may already have typed one of these as a custom option: promote it instead of duplicating.
UPDATE g SET IsPredefined = 1, SortOrder = s.SortOrder
FROM dbo.FitnessGoals g JOIN @Goals s ON s.Name = g.Name
WHERE g.IsPredefined = 0 OR g.SortOrder <> s.SortOrder;
INSERT dbo.FitnessGoals (Name, IsPredefined, SortOrder, CreatedByUserId, CreatedAt)
SELECT s.Name, 1, s.SortOrder, NULL, GETDATE() FROM @Goals s
WHERE NOT EXISTS (SELECT 1 FROM dbo.FitnessGoals g WHERE g.Name = s.Name);
PRINT CONCAT('Predefined goals inserted: ', @@ROWCOUNT);

UPDATE p SET IsPredefined = 1, SortOrder = s.SortOrder
FROM dbo.Specialisations p JOIN @Specs s ON s.Name = p.Name
WHERE p.IsPredefined = 0 OR p.SortOrder <> s.SortOrder;
INSERT dbo.Specialisations (Name, IsPredefined, SortOrder, CreatedByUserId, CreatedAt)
SELECT s.Name, 1, s.SortOrder, NULL, GETDATE() FROM @Specs s
WHERE NOT EXISTS (SELECT 1 FROM dbo.Specialisations p WHERE p.Name = s.Name);
PRINT CONCAT('Predefined specialisations inserted: ', @@ROWCOUNT);

/* ---------- 8. Legacy free text -> new tables ----------
   Rule (documented in the report): existing profiles keep their old text column untouched.
   Each comma-separated part (2-50 characters, max 15 per person) is linked to a predefined
   option when the name matches (case-insensitive), otherwise stored as a user-defined option
   owned by the first user who had it. Only profiles with NO links yet are processed, so a
   second run does nothing. */
CREATE TABLE #LegacyGoal (ClientId int, UserId int, Item nvarchar(50) COLLATE DATABASE_DEFAULT, Pos int);
WITH src AS (
    SELECT cp.ClientId, cp.UserId, CAST(cp.Goal + N',' AS nvarchar(max)) AS Rest
    FROM dbo.ClientProfiles cp
    WHERE NULLIF(LTRIM(RTRIM(cp.Goal)), N'') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.ClientGoals x WHERE x.ClientId = cp.ClientId)
), parts AS (
    SELECT ClientId, UserId,
           CAST(LTRIM(RTRIM(LEFT(Rest, CHARINDEX(N',', Rest) - 1))) AS nvarchar(max)) AS Item,
           CAST(STUFF(Rest, 1, CHARINDEX(N',', Rest), N'') AS nvarchar(max)) AS Rest, 1 AS Pos
    FROM src
    UNION ALL
    SELECT ClientId, UserId,
           CAST(LTRIM(RTRIM(LEFT(Rest, CHARINDEX(N',', Rest) - 1))) AS nvarchar(max)),
           CAST(STUFF(Rest, 1, CHARINDEX(N',', Rest), N'') AS nvarchar(max)), Pos + 1
    FROM parts WHERE Rest <> N''
)
INSERT #LegacyGoal (ClientId, UserId, Item, Pos)
SELECT ClientId, UserId, LEFT(Item, 50), MIN(Pos)
FROM parts WHERE LEN(Item) >= 2
GROUP BY ClientId, UserId, LEFT(Item, 50)
OPTION (MAXRECURSION 500);   -- one level per comma; the column is at most 200 characters

INSERT dbo.FitnessGoals (Name, IsPredefined, SortOrder, CreatedByUserId, CreatedAt)
SELECT l.Item, 0, 1000, MIN(l.UserId), GETDATE()
FROM #LegacyGoal l
WHERE NOT EXISTS (SELECT 1 FROM dbo.FitnessGoals g WHERE g.Name = l.Item)
GROUP BY l.Item;
PRINT CONCAT('User-defined goals created from legacy text: ', @@ROWCOUNT);

INSERT dbo.ClientGoals (ClientId, FitnessGoalId, Position)
SELECT r.ClientId, r.FitnessGoalId, r.NewPos
FROM (
    SELECT l.ClientId, g.FitnessGoalId,
           ROW_NUMBER() OVER (PARTITION BY l.ClientId ORDER BY MIN(l.Pos)) - 1 AS NewPos
    FROM #LegacyGoal l JOIN dbo.FitnessGoals g ON g.Name = l.Item
    GROUP BY l.ClientId, g.FitnessGoalId
) r
WHERE r.NewPos < 15;
PRINT CONCAT('Client goal links created from legacy text: ', @@ROWCOUNT);
DROP TABLE #LegacyGoal;

CREATE TABLE #LegacySpec (TrainerId int, UserId int, Item nvarchar(50) COLLATE DATABASE_DEFAULT, Pos int);
WITH src AS (
    SELECT tp.TrainerId, tp.UserId, CAST(tp.Specialisation + N',' AS nvarchar(max)) AS Rest
    FROM dbo.TrainerProfiles tp
    WHERE NULLIF(LTRIM(RTRIM(tp.Specialisation)), N'') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.TrainerSpecialisations x WHERE x.TrainerId = tp.TrainerId)
), parts AS (
    SELECT TrainerId, UserId,
           CAST(LTRIM(RTRIM(LEFT(Rest, CHARINDEX(N',', Rest) - 1))) AS nvarchar(max)) AS Item,
           CAST(STUFF(Rest, 1, CHARINDEX(N',', Rest), N'') AS nvarchar(max)) AS Rest, 1 AS Pos
    FROM src
    UNION ALL
    SELECT TrainerId, UserId,
           CAST(LTRIM(RTRIM(LEFT(Rest, CHARINDEX(N',', Rest) - 1))) AS nvarchar(max)),
           CAST(STUFF(Rest, 1, CHARINDEX(N',', Rest), N'') AS nvarchar(max)), Pos + 1
    FROM parts WHERE Rest <> N''
)
INSERT #LegacySpec (TrainerId, UserId, Item, Pos)
SELECT TrainerId, UserId, LEFT(Item, 50), MIN(Pos)
FROM parts WHERE LEN(Item) >= 2
GROUP BY TrainerId, UserId, LEFT(Item, 50)
OPTION (MAXRECURSION 500);

INSERT dbo.Specialisations (Name, IsPredefined, SortOrder, CreatedByUserId, CreatedAt)
SELECT l.Item, 0, 1000, MIN(l.UserId), GETDATE()
FROM #LegacySpec l
WHERE NOT EXISTS (SELECT 1 FROM dbo.Specialisations p WHERE p.Name = l.Item)
GROUP BY l.Item;
PRINT CONCAT('User-defined specialisations created from legacy text: ', @@ROWCOUNT);

INSERT dbo.TrainerSpecialisations (TrainerId, SpecialisationId, Position)
SELECT r.TrainerId, r.SpecialisationId, r.NewPos
FROM (
    SELECT l.TrainerId, p.SpecialisationId,
           ROW_NUMBER() OVER (PARTITION BY l.TrainerId ORDER BY MIN(l.Pos)) - 1 AS NewPos
    FROM #LegacySpec l JOIN dbo.Specialisations p ON p.Name = l.Item
    GROUP BY l.TrainerId, p.SpecialisationId
) r
WHERE r.NewPos < 15;
PRINT CONCAT('Trainer specialisation links created from legacy text: ', @@ROWCOUNT);
DROP TABLE #LegacySpec;

COMMIT TRANSACTION;
PRINT 'HealthTrack onboarding update completed successfully. Now run 01_verify_schema.sql again.';
