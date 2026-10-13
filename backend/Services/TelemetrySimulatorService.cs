// Telemetry simulator service: creates sample sensor readings for development scenarios when the environment permits it.
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartIotApi.Models;

namespace SmartIotApi.Services;

/// <summary>
/// TelemetrySimulatorService is an IHostedService that runs in the background and simulates
/// telemetry data ingestion from IoT sensors at regular 10-second intervals.
/// 
/// This service:
/// - Uses a timer-based execution model for efficient background processing
/// - Generates random telemetry data for each sensor in the database
/// - Respects sensor type/category to generate realistic data (environmental, power, actuator)
/// - Sends data to the TelemetryDataController via HTTP POST requests
/// - Operates only in Development environment (as per configuration)
/// - Logs all activities and errors
/// 
/// Typical flow:
/// 1. Service starts (StartAsync called)
/// 2. Timer begins 10-second intervals
/// 3. Each interval: fetch all active sensors, generate random data, POST to API
/// 4. Continue until application shutdown
/// 
/// Usage in Program.cs:
///   builder.Services.AddSingleton<TelemetrySimulatorService>();
///   app.Services.GetRequiredService{TelemetrySimulatorService}().StartAsync(cancellationToken);
/// </summary>
public class TelemetrySimulatorService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelemetrySimulatorService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _baseApiUrl;
    private Timer? _simulationTimer;
    private HttpClient? _httpClient;
    private readonly Random _random = new();
    private bool _isRunning = false;
    private readonly Stopwatch _sessionStopwatch = new();

    /// <summary>
    /// Constructor for TelemetrySimulatorService.
    /// </summary>
    /// <param name="serviceProvider">Service provider for database access</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="environment">Environment information</param>
    /// <param name="baseApiUrl">Base URL for API calls (e.g., http://localhost:5231)</param>
    public TelemetrySimulatorService(
        IServiceProvider serviceProvider,
        ILogger<TelemetrySimulatorService> logger,
        IWebHostEnvironment environment,
        string baseApiUrl = "http://localhost:5231")
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _baseApiUrl = baseApiUrl;

        _logger.LogInformation("TelemetrySimulatorService initialized with base URL: {BaseUrl}", _baseApiUrl);
    }

    /// <summary>
    /// Called when the application is starting up.
    /// Initializes the HTTP client and starts the simulation timer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completed task</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Only run in Development environment
        if (!_environment.IsDevelopment())
        {
            _logger.LogWarning("Telemetry simulator is configured to run in Development environment only. Current environment: {Environment}", _environment.EnvironmentName);
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting TelemetrySimulatorService...");

        // Initialize HTTP client for API calls
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseApiUrl)
        };

        // Start the 10-second timer
        _simulationTimer = new Timer(
            callback: async _ => await GenerateTelemetryAsync(),
            state: null,
            dueTime: TimeSpan.FromSeconds(2),   // Start after 2 seconds to allow DB initialization
            period: TimeSpan.FromSeconds(10)    // Repeat every 10 seconds
        );

        _isRunning = true;
        _sessionStopwatch.Start();
        _logger.LogInformation("TelemetrySimulatorService started successfully. Telemetry will be generated every 10 seconds.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the application is shutting down.
    /// Stops the simulation timer and cleans up resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Completed task</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _isRunning = false;
        _sessionStopwatch.Stop();

        if (_simulationTimer != null)
        {
            _simulationTimer.Dispose();
            _logger.LogInformation("TelemetrySimulatorService timer disposed.");
        }

        if (_httpClient != null)
        {
            _httpClient.Dispose();
            _logger.LogInformation("TelemetrySimulatorService HTTP client disposed.");
        }

        _logger.LogInformation("TelemetrySimulatorService stopped after {ElapsedSeconds} seconds.", 
            _sessionStopwatch.Elapsed.TotalSeconds);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Core simulation method. Called on every 10-second interval.
    /// Fetches active sensors, generates random telemetry data, and POSTs to the API.
    /// </summary>
    private async Task GenerateTelemetryAsync()
    {
        try
        {
            if (_httpClient == null || !_isRunning)
                return;

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<SmartIotContext>();

                // Fetch all active sensors
                var activeSensors = dbContext.Sensors
                    .Where(s => s.IsActive)
                    .ToList();

                if (!activeSensors.Any())
                {
                    _logger.LogDebug("No active sensors found for telemetry generation.");
                    return;
                }

                _logger.LogDebug("Generating telemetry for {SensorCount} active sensors.", activeSensors.Count);

                // Generate and send telemetry for each sensor
                foreach (var sensor in activeSensors)
                {
                    try
                    {
                        var telemetry = GenerateTelemetryForSensor(sensor);
                        await SendTelemetryAsync(telemetry);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error generating or sending telemetry for sensor {SensorMac}.", sensor.Macaddress);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in telemetry generation cycle.");
        }
    }

    /// <summary>
    /// Generates random telemetry data for a specific sensor based on its category.
    /// Different sensor categories generate realistic data:
    /// - Environmental: Temperature (20-30°C), Humidity (30-80%), Pressure (950-1050 hPa)
    /// - Power: Wattage (200-500W), Current (5-15A), Voltage (220-240V)
    /// - Actuator: State (0 or 1), Angle (0-360°)
    /// - Other: Generic numeric reading
    /// </summary>
    private TelemetryDatum GenerateTelemetryForSensor(Sensor sensor)
    {
        var now = DateTime.UtcNow;
        var category = sensor.Category?.ToLowerInvariant() ?? "unknown";

        return category switch
        {
            "environmental" => GenerateEnvironmentalData(sensor.Macaddress, now),
            "power" => GeneratePowerData(sensor.Macaddress, now),
            "actuator" => GenerateActuatorData(sensor.Macaddress, now),
            "temperature" => new TelemetryDatum
            {
                SensorMac = sensor.Macaddress,
                DataType = "temperature",
                DataValue = 15 + _random.NextDouble() * 25,  // 15-40°C
                Timestamp = now
            },
            "humidity" => new TelemetryDatum
            {
                SensorMac = sensor.Macaddress,
                DataType = "humidity",
                DataValue = 20 + _random.NextDouble() * 70,  // 20-90%
                Timestamp = now
            },
            _ => new TelemetryDatum
            {
                SensorMac = sensor.Macaddress,
                DataType = "genericReading",
                DataValue = _random.NextDouble() * 100,  // 0-100
                Timestamp = now
            }
        };
    }

    /// <summary>
    /// Generates realistic environmental sensor data (temperature).
    /// Includes some temporal variation to simulate real-world patterns.
    /// </summary>
    private TelemetryDatum GenerateEnvironmentalData(string sensorMac, DateTime timestamp)
    {
        // Base temperature around 22°C with some variation
        var baseTemp = 22.0;
        var variation = (_random.NextDouble() - 0.5) * 8;  // ±4°C variation
        var sinusoidalVariation = Math.Sin(DateTime.UtcNow.Ticks / 300_000_000.0) * 3;  // Slow drift ±3°C
        var value = baseTemp + variation + sinusoidalVariation;

        return new TelemetryDatum
        {
            SensorMac = sensorMac,
            DataType = "temperature",
            DataValue = Math.Round(value, 1),
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Generates realistic power sensor data (wattage).
    /// Simulates typical power consumption patterns.
    /// </summary>
    private TelemetryDatum GeneratePowerData(string sensorMac, DateTime timestamp)
    {
        // Base power load around 300W with some random variation
        var basePower = 300.0;
        var randomVariation = (_random.NextDouble() - 0.5) * 150;  // ±75W variation
        var spikeProbability = _random.NextDouble();
        var spike = spikeProbability < 0.1 ? _random.NextDouble() * 200 : 0;  // 10% chance of spike up to 200W
        var value = basePower + randomVariation + spike;

        return new TelemetryDatum
        {
            SensorMac = sensorMac,
            DataType = "powerWattage",
            DataValue = Math.Max(0, Math.Round(value, 0)),  // Ensure non-negative
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Generates realistic actuator sensor data (state: 0 or 1).
    /// Simulates on/off states with occasional transitions.
    /// </summary>
    private TelemetryDatum GenerateActuatorData(string sensorMac, DateTime timestamp)
    {
        // 70% chance of being on, 30% chance of being off
        var state = _random.NextDouble() < 0.7 ? 1 : 0;

        return new TelemetryDatum
        {
            SensorMac = sensorMac,
            DataType = "valveState",
            DataValue = state,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// Sends a telemetry datum to the API via HTTP POST request.
    /// Targets the TelemetryDataController POST endpoint.
    /// </summary>
    private async Task SendTelemetryAsync(TelemetryDatum telemetry)
    {
        if (_httpClient == null)
            return;

        try
        {
            var json = JsonSerializer.Serialize(telemetry);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/telemetrydata", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Telemetry sent successfully for sensor {SensorMac}: {DataType}={DataValue}", 
                    telemetry.SensorMac, telemetry.DataType, telemetry.DataValue);
            }
            else
            {
                _logger.LogWarning("Failed to send telemetry for sensor {SensorMac}. Status: {StatusCode}", 
                    telemetry.SensorMac, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending telemetry for sensor {SensorMac}.", telemetry.SensorMac);
        }
    }

    /// <summary>
    /// Gets the current session duration since the simulator started.
    /// </summary>
    public TimeSpan GetSessionDuration()
    {
        return _sessionStopwatch.Elapsed;
    }

    /// <summary>
    /// Returns true if the simulator is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;
}
