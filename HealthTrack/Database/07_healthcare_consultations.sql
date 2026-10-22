-- Run on the HealthTrack database after taking a backup.
IF OBJECT_ID(N'dbo.HealthcareAppointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HealthcareAppointments (
      HealthcareAppointmentId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_HealthcareAppointments PRIMARY KEY,
      ClientId int NOT NULL,
      ProfessionalName nvarchar(120) NOT NULL,
      Clinic nvarchar(120) NULL,
      AppointmentDateTime datetime NOT NULL,
      Status nvarchar(20) NOT NULL,
      Notes nvarchar(500) NULL,
      CreatedAt datetime NOT NULL,
      CONSTRAINT FK_HealthcareAppointments_Client FOREIGN KEY (ClientId) REFERENCES dbo.ClientProfiles(ClientId)
    );
END;
