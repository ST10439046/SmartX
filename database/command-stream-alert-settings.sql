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

-- Provision sensor-specific defaults according to each sensor's category.
-- The API also provisions these rules for newly registered sensors.
-- Defaults are intentionally scoped to SensorMAC, not global rules.
IF OBJECT_ID(N'dbo.Sensors', N'U') IS NOT NULL
BEGIN
    -- Environmental devices: temperature and common humidity/moisture metrics.
    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' temperature high'), s.MACAddress, 'temperature', '>=', 40, 'critical', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%environment%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = 'temperature' AND r.Comparison = '>=' AND r.Threshold = 40);

    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' temperature low'), s.MACAddress, 'temperature', '<=', 0, 'warning', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%environment%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = 'temperature' AND r.Comparison = '<=' AND r.Threshold = 0);

    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' humidity high'), s.MACAddress, 'humidity', '>=', 85, 'warning', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%environment%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = 'humidity' AND r.Comparison = '>=' AND r.Threshold = 85);

    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' moisture low'), s.MACAddress, 'moisture', '<', 20, 'warning', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%environment%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = 'moisture' AND r.Comparison = '<' AND r.Threshold = 20);

    -- Power devices: overload and non-positive readings.
    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' power overload'), s.MACAddress, 'powerWattage', '>=', 1000, 'critical', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%power%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = LOWER('powerWattage') AND r.Comparison = '>=' AND r.Threshold = 1000);

    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' power invalid'), s.MACAddress, 'powerWattage', '<=', 0, 'warning', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%power%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = LOWER('powerWattage') AND r.Comparison = '<=' AND r.Threshold = 0);

    -- Actuator state values are normally 0 or 1; values outside that range are suspicious.
    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' actuator state high'), s.MACAddress, 'valveState', '>=', 2, 'critical', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%actuator%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = 'valvestate' AND r.Comparison = '>=' AND r.Threshold = 2);

    INSERT dbo.AlertRules (Name, SensorMAC, DataType, Comparison, Threshold, Severity, IsEnabled)
    SELECT CONCAT(COALESCE(NULLIF(s.NodeID, ''), s.MACAddress), ' actuator state low'), s.MACAddress, 'valveState', '<', 0, 'critical', 1
    FROM dbo.Sensors s
    WHERE LOWER(ISNULL(s.Category, '')) LIKE '%actuator%'
      AND NOT EXISTS (SELECT 1 FROM dbo.AlertRules r WHERE r.SensorMAC = s.MACAddress AND LOWER(r.DataType) = 'valvestate' AND r.Comparison = '<' AND r.Threshold = 0);
END;
GO
