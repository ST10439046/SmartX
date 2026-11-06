// Application entry point: registers services, configures database access and middleware, maps API routes, and starts the ASP.NET Core host.

using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;
using SmartIotApi.Services;
using SmartIotApi.Services.Mesh;

namespace SmartIotApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<SmartIotContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.WriteIndented = true;
                });
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(
                            "http://localhost:5173",
                            "http://localhost:3000",
                            "http://localhost:59759",
                            "http://localhost:59760",
                            "http://localhost:5174")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Register the TelemetrySimulatorService for background telemetry generation
            // This service runs only in Development environment and sends random sensor data every 10 seconds
            builder.Services.AddSingleton<RealtimeCommandService>();
            builder.Services.AddSingleton<TelemetryAvlTreeService>();
            builder.Services.AddSingleton<TelemetrySimulatorService>();
            builder.Services.AddHostedService(provider => provider.GetRequiredService<TelemetrySimulatorService>());

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SmartIotContext>();
                dbContext.Database.EnsureCreated();
                dbContext.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('dbo.ActiveAlerts', 'AcknowledgedAt') IS NULL
    ALTER TABLE dbo.ActiveAlerts ADD AcknowledgedAt datetime2 NULL;
IF COL_LENGTH('dbo.ActiveAlerts', 'AcknowledgedBy') IS NULL
    ALTER TABLE dbo.ActiveAlerts ADD AcknowledgedBy nvarchar(120) NULL;
IF COL_LENGTH('dbo.Sensors', 'IsGateway') IS NULL
    ALTER TABLE dbo.Sensors ADD IsGateway bit NOT NULL CONSTRAINT DF_Sensors_IsGateway DEFAULT (0);
IF COL_LENGTH('dbo.NetworkTopology', 'SignalStrength') IS NULL
    ALTER TABLE dbo.NetworkTopology ADD SignalStrength float NULL;
UPDATE dbo.NetworkTopology SET SignalStrength = 50 WHERE SignalStrength IS NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.NetworkTopology') AND name = 'SignalStrength' AND is_nullable = 1)
    ALTER TABLE dbo.NetworkTopology ALTER COLUMN SignalStrength float NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_NetworkTopology_SignalStrength')
    ALTER TABLE dbo.NetworkTopology ADD CONSTRAINT CK_NetworkTopology_SignalStrength CHECK (SignalStrength >= 0 AND SignalStrength <= 100);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Sensors_SingleGateway' AND object_id = OBJECT_ID('dbo.Sensors'))
    CREATE UNIQUE INDEX UX_Sensors_SingleGateway ON dbo.Sensors(IsGateway) WHERE IsGateway = 1;
IF OBJECT_ID('dbo.OperatorActivities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperatorActivities (
        OperatorActivityId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_OperatorActivities PRIMARY KEY,
        ActivityType varchar(20) NOT NULL,
        SensorMAC varchar(17) NULL,
        Detail nvarchar(500) NOT NULL,
        NormalizedDetail nvarchar(500) NOT NULL,
        CreatedAt datetime2 NOT NULL CONSTRAINT DF_OperatorActivities_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_OperatorActivities_SensorMAC_ActivityType_CreatedAt
        ON dbo.OperatorActivities (SensorMAC, ActivityType, CreatedAt DESC);
END");
                SeedSampleData(dbContext);
                var telemetryIndex = scope.ServiceProvider.GetRequiredService<TelemetryAvlTreeService>();
                telemetryIndex.Rebuild(dbContext.TelemetryData.AsNoTracking().ToList());
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseCors("AllowFrontend");

            app.MapControllers();

            app.Run();
        }

        private static void SeedSampleData(SmartIotContext context)
        {
            // First, seed deployment hierarchy (Facility -> Zone -> Sub-Zone)
            var facilityA = new DeploymentNode
            {
                Name = "Facility A (Root)",
                Description = "Main IoT facility - Root deployment node",
                IsActive = true,
                ParentNodeId = null
            };

            var zone1 = new DeploymentNode
            {
                Name = "Zone 1 (Parent)",
                Description = "Primary operational zone",
                IsActive = true,
                ParentNodeId = null // Will be set after facilityA is created
            };

            var subZoneB = new DeploymentNode
            {
                Name = "Sub-Zone B (Child)",
                Description = "Secondary operational sub-zone",
                IsActive = true,
                ParentNodeId = null // Will be set after zone1 is created
            };

            var sensorLocation = new DeploymentNode
            {
                Name = "ESP32-Sensor Location (Leaf)",
                Description = "Specific location for ESP32 sensor node",
                IsActive = true,
                ParentNodeId = null // Will be set after subZoneB is created
            };

            // Add deployment nodes if they don't exist
            if (!context.DeploymentNodes.Any(d => d.Name == facilityA.Name))
            {
                context.DeploymentNodes.Add(facilityA);
                context.SaveChanges();
            }
            else
            {
                facilityA = context.DeploymentNodes.First(d => d.Name == facilityA.Name);
            }

            if (!context.DeploymentNodes.Any(d => d.Name == zone1.Name))
            {
                zone1.ParentNodeId = facilityA.NodeId;
                context.DeploymentNodes.Add(zone1);
                context.SaveChanges();
            }
            else
            {
                zone1 = context.DeploymentNodes.First(d => d.Name == zone1.Name);
            }

            if (!context.DeploymentNodes.Any(d => d.Name == subZoneB.Name))
            {
                subZoneB.ParentNodeId = zone1.NodeId;
                context.DeploymentNodes.Add(subZoneB);
                context.SaveChanges();
            }
            else
            {
                subZoneB = context.DeploymentNodes.First(d => d.Name == subZoneB.Name);
            }

            if (!context.DeploymentNodes.Any(d => d.Name == sensorLocation.Name))
            {
                sensorLocation.ParentNodeId = subZoneB.NodeId;
                context.DeploymentNodes.Add(sensorLocation);
                context.SaveChanges();
            }
            else
            {
                sensorLocation = context.DeploymentNodes.First(d => d.Name == sensorLocation.Name);
            }

            // Create sensors with deployment hierarchy and active by default
            var sampleSensors = new[]
            {
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:5E",
                    Room = "Greenhouse",
                    Zone = "North Plot",
                    NodeId = "ENV-01",
                    Category = "Environmental",
                    RegisteredAt = DateTime.UtcNow.AddDays(-21),
                    IsActive = true,
                    DeploymentNodeId = sensorLocation.NodeId
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:5F",
                    Room = "Generator Room",
                    Zone = "Utility Wing",
                    NodeId = "PWR-02",
                    Category = "Power",
                    RegisteredAt = DateTime.UtcNow.AddDays(-18),
                    IsActive = true,
                    DeploymentNodeId = subZoneB.NodeId
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:60",
                    Room = "Irrigation Pump",
                    Zone = "South Plot",
                    NodeId = "ACT-03",
                    Category = "Actuator",
                    RegisteredAt = DateTime.UtcNow.AddDays(-14),
                    IsActive = true,
                    DeploymentNodeId = zone1.NodeId
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:61",
                    Room = "Water Storage",
                    Zone = "North Plot",
                    NodeId = "ENV-04",
                    Category = "Environmental",
                    RegisteredAt = DateTime.UtcNow.AddDays(-12),
                    IsActive = true,
                    DeploymentNodeId = sensorLocation.NodeId
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:62",
                    Room = "Control Room",
                    Zone = "Utility Wing",
                    NodeId = "PWR-05",
                    Category = "Power",
                    RegisteredAt = DateTime.UtcNow.AddDays(-9),
                    IsActive = true,
                    DeploymentNodeId = subZoneB.NodeId
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:63",
                    Room = "Remote Weather Station",
                    Zone = "East Plot",
                    NodeId = "ENV-06",
                    Category = "Environmental",
                    RegisteredAt = DateTime.UtcNow.AddDays(-6),
                    IsActive = true,
                    DeploymentNodeId = zone1.NodeId
                }
            };

            foreach (var sensor in sampleSensors)
            {
                if (!context.Sensors.Any(s => s.Macaddress == sensor.Macaddress))
                {
                    context.Sensors.Add(sensor);
                }
            }

            context.SaveChanges();

            // The demo starts with one operator-selected gateway and a deterministic five-node mesh.
            var gateway = context.Sensors.First(s => s.Macaddress == "00:1A:2B:3C:4D:5E");
            if (!context.Sensors.Any(s => s.IsGateway))
                gateway.IsGateway = true;

            var sampleLinks = new (string From, string To, double Quality)[]
            {
                ("00:1A:2B:3C:4D:5E", "00:1A:2B:3C:4D:5F", 90),
                ("00:1A:2B:3C:4D:5E", "00:1A:2B:3C:4D:60", 72),
                ("00:1A:2B:3C:4D:5F", "00:1A:2B:3C:4D:61", 84),
                ("00:1A:2B:3C:4D:60", "00:1A:2B:3C:4D:61", 63),
                ("00:1A:2B:3C:4D:60", "00:1A:2B:3C:4D:62", 95),
                ("00:1A:2B:3C:4D:61", "00:1A:2B:3C:4D:62", 78)
            };

            foreach (var link in sampleLinks)
            {
                var exists = context.NetworkTopologies.Any(item =>
                    (item.FromNodeMac == link.From && item.ToNodeMac == link.To) ||
                    (item.FromNodeMac == link.To && item.ToNodeMac == link.From));
                if (!exists)
                    context.NetworkTopologies.Add(new NetworkTopology
                    {
                        FromNodeMac = link.From,
                        ToNodeMac = link.To,
                        SignalStrength = link.Quality
                    });
            }

            context.SaveChanges();

            foreach (var sensor in sampleSensors)
            {
                if (context.TelemetryData.Any(t => t.SensorMac == sensor.Macaddress))
                {
                    continue;
                }

                context.TelemetryData.AddRange(BuildTelemetry(sensor.Macaddress, sensor.Category ?? "Unknown"));
            }

            context.SaveChanges();
        }

        private static List<TelemetryDatum> BuildTelemetry(string sensorMac, string category)
        {
            var now = DateTime.UtcNow;
            var results = new List<TelemetryDatum>();

            switch (category.ToLowerInvariant())
            {
                case "environmental":
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "temperature", DataValue = 23.1, Timestamp = now.AddMinutes(-35) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "temperature", DataValue = 24.7, Timestamp = now.AddMinutes(-20) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "temperature", DataValue = 25.4, Timestamp = now.AddMinutes(-9) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "temperature", DataValue = 26.1, Timestamp = now.AddSeconds(-5) }
                    });
                    break;

                case "power":
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 312, Timestamp = now.AddMinutes(-42) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 338, Timestamp = now.AddMinutes(-24) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 352, Timestamp = now.AddMinutes(-10) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 367, Timestamp = now.AddSeconds(-5) }
                    });
                    break;

                case "actuator":
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 1, Timestamp = now.AddMinutes(-36) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 0, Timestamp = now.AddMinutes(-22) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 1, Timestamp = now.AddMinutes(-8) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 1, Timestamp = now.AddSeconds(-5) }
                    });
                    break;

                default:
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 42.5, Timestamp = now.AddMinutes(-30) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 43.2, Timestamp = now.AddMinutes(-18) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 44.7, Timestamp = now.AddMinutes(-7) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 45.0, Timestamp = now.AddSeconds(-5) }
                    });
                    break;
            }

            return results;
        }
    }
}

