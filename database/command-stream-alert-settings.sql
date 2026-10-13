USE [SmartIot];
GO

IF OBJECT_ID(N'dbo.AlertRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AlertRules
    (
        AlertRuleID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AlertRules PRIMARY KEY,
        Name VARCHAR(100) NOT NULL,
        SensorMAC VARCHAR(17) NULL,
        DataType VARCHAR(50) NOT NULL,
        Comparison VARCHAR(2) NOT NULL,
        Threshold FLOAT NOT NULL,
        Severity VARCHAR(20) NOT NULL CONSTRAINT DF_AlertRules_Severity DEFAULT ('warning'),
        IsEnabled BIT NOT NULL CONSTRAINT DF_AlertRules_IsEnabled DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_AlertRules_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT CK_AlertRules_Comparison CHECK (Comparison IN ('>', '>=', '<', '<=', '=')),
        CONSTRAINT CK_AlertRules_Severity CHECK (Severity IN ('info', 'warning', 'critical')),
        CONSTRAINT FK_AlertRules_Sensors FOREIGN KEY (SensorMAC)
            REFERENCES dbo.Sensors(MACAddress) ON DELETE SET NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.CommandDeviceStates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommandDeviceStates
    (
        SensorMAC VARCHAR(17) NOT NULL CONSTRAINT PK_CommandDeviceStates PRIMARY KEY,
        CurrentState VARCHAR(20) NOT NULL CONSTRAINT DF_CommandDeviceStates_CurrentState DEFAULT ('UNKNOWN'),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_CommandDeviceStates_UpdatedAt DEFAULT (GETDATE()),
        CONSTRAINT FK_CommandDeviceStates_Sensors FOREIGN KEY (SensorMAC)
            REFERENCES dbo.Sensors(MACAddress) ON DELETE CASCADE
    );
END;
GO

-- Optional starter rules. Run only if these demo rules do not already exist.
IF NOT EXISTS (SELECT 1 FROM dbo.AlertRules WHERE Name = 'High temperature demo')
    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    VALUES ('High temperature demo', NULL, 'temperature', '>=', 40, 'critical', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.AlertRules WHERE Name = 'Low moisture demo')
    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    VALUES ('Low moisture demo', NULL, 'moisture', '<', 20, 'warning', 1);
GO
