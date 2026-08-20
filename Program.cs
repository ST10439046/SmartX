
using Microsoft.EntityFrameworkCore;
using SmartIotApi.Models;

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
                            "http://localhost:59759")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SmartIotContext>();
                dbContext.Database.EnsureCreated();
                SeedSampleData(dbContext);
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
            var sampleSensors = new[]
            {
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:5E",
                    Room = "Greenhouse",
                    Zone = "North Plot",
                    NodeId = "ENV-01",
                    Category = "Environmental",
                    RegisteredAt = DateTime.UtcNow.AddDays(-21)
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:5F",
                    Room = "Generator Room",
                    Zone = "Utility Wing",
                    NodeId = "PWR-02",
                    Category = "Power",
                    RegisteredAt = DateTime.UtcNow.AddDays(-18)
                },
                new Sensor
                {
                    Macaddress = "00:1A:2B:3C:4D:60",
                    Room = "Irrigation Pump",
                    Zone = "South Plot",
                    NodeId = "ACT-03",
                    Category = "Actuator",
                    RegisteredAt = DateTime.UtcNow.AddDays(-14)
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
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "temperature", DataValue = 26.1, Timestamp = now.AddMinutes(-3) }
                    });
                    break;

                case "power":
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 312, Timestamp = now.AddMinutes(-42) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 338, Timestamp = now.AddMinutes(-24) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 352, Timestamp = now.AddMinutes(-10) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "powerWattage", DataValue = 367, Timestamp = now.AddMinutes(-2) }
                    });
                    break;

                case "actuator":
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 1, Timestamp = now.AddMinutes(-36) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 0, Timestamp = now.AddMinutes(-22) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 1, Timestamp = now.AddMinutes(-8) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "valveState", DataValue = 1, Timestamp = now.AddMinutes(-1) }
                    });
                    break;

                default:
                    results.AddRange(new[]
                    {
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 42.5, Timestamp = now.AddMinutes(-30) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 43.2, Timestamp = now.AddMinutes(-18) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 44.7, Timestamp = now.AddMinutes(-7) },
                        new TelemetryDatum { SensorMac = sensorMac, DataType = "reading", DataValue = 45.0, Timestamp = now.AddMinutes(-1) }
                    });
                    break;
            }

            return results;
        }
    }
}

