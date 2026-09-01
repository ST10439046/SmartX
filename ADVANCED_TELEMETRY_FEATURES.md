# Advanced Telemetry Features - Implementation Guide

## Overview

This document describes the three advanced C# features that have been implemented for the Smart-X IoT platform:

1. **Generics (TelemetryPacket<T>)** - Reusable generic wrapper for diverse sensor structures
2. **Operator Overloading (SensorReading)** - Direct aggregation and delta calculations
3. **Advanced Arrays & Lists (TelemetryBatchProcessor)** - Array-to-List migration with optimized algorithms
4. **Bonus: Telemetry Simulator** - Background service for 10-second interval telemetry generation

---

## Feature 1: Generics - TelemetryPacket<T>

### Purpose
`TelemetryPacket<T>` is a reusable generic **struct** (value type) that handles disparate sensor structures uniformly without boxing/unboxing overhead. It wraps sensor data with metadata and provides validation and serialization.

### Design Highlights
- **Value Type (Struct)**: Avoids heap allocation and GC pressure for high-frequency ingestion
- **Generic Parameter T**: Accommodates float, int, bool, double, and custom types
- **Zero-Copy Semantics**: Direct memory layout for efficient batch processing
- **Validation**: Built-in MAC address format checking and timestamp validation

### File Location
```
Models/TelemetryPacket.cs
```

### Usage Examples

#### Basic Usage - Temperature Sensor (float)
```csharp
var tempPacket = new TelemetryPacket<float>(
	sensorMac: "00:1A:2B:3C:4D:5E",
	value: 23.5f,
	dataType: "temperature",
	unit: "°C"
);

// Validate before processing
if (tempPacket.Validate())
{
	var telemetryDatum = tempPacket.ToTelemetryDatum();
	_context.TelemetryData.Add(telemetryDatum);
}
```

#### Power Sensor (int)
```csharp
var powerPacket = new TelemetryPacket<int>(
	sensorMac: "00:1A:2B:3C:4D:5F",
	value: 312,
	dataType: "powerWattage",
	unit: "W"
);
```

#### Boolean Actuator State
```csharp
var actuatorPacket = new TelemetryPacket<bool>(
	sensorMac: "00:1A:2B:3C:4D:60",
	value: true,
	dataType: "valveState",
	unit: ""
);
```

#### Batch Processing with Value Type Benefits
```csharp
// Stack-allocated array of value types - no GC pressure
var readings = new TelemetryPacket<double>[1000];

for (int i = 0; i < 1000; i++)
{
	readings[i] = new TelemetryPacket<double>(
		"00:1A:2B:3C:4D:5E",
		_random.NextDouble() * 50,
		"temperature",
		DateTime.UtcNow
	);
}
```

### Key Methods

| Method | Purpose |
|--------|---------|
| `Validate()` | Validates MAC format and timestamp |
| `ToTelemetryDatum()` | Converts to ORM model for database storage |
| `FromTelemetryDatum()` | Converts from database model back to packet |
| `WithValue(T)` | Creates copy with new value, preserves metadata |
| `WithTimestamp()` | Creates copy with new timestamp |
| `ToJson()` | Returns serializable dictionary representation |

### Performance Benefits
- **No Boxing/Unboxing**: Value type eliminates boxing overhead for primitive types
- **Memory Efficiency**: 56 bytes on 64-bit system (MAC string excluded)
- **Cache Locality**: Sequential allocation improves CPU cache utilization
- **GC Pressure**: Reduced garbage collection cycles for high-frequency data

---

## Feature 2: Operator Overloading - SensorReading

### Purpose
`SensorReading` class implements operator overloading to enable natural mathematical operations on sensor data. Supports aggregation, delta calculations, and threshold detection in clean, readable code.

### File Location
```
Models/SensorReading.cs
```

### Supported Operators

#### Arithmetic Operators

```csharp
// Addition: Aggregate sensor loads
var meter1 = new SensorReading("00:1A:2B:3C:4D:5E", "Power", 100.5, "W");
var meter2 = new SensorReading("00:1A:2B:3C:4D:5F", "Power", 50.3, "W");
var total = meter1 + meter2;  // New reading with value 150.8

// Subtraction: Calculate deltas
var current = new SensorReading("00:1A:2B:3C:4D:5E", "Temp", 25.0, "°C");
var previous = new SensorReading("00:1A:2B:3C:4D:5E", "Temp", 22.0, "°C");
var delta = current - previous;  // New reading with value 3.0

// Multiplication: Scale and calibration
var scaled = meter1 * 1.05;  // Apply 5% multiplier (e.g., calibration factor)

// Division: Averaging and normalization
var average = total / 2;  // Average the sum
```

#### Comparison Operators

```csharp
// Threshold detection
var threshold = new SensorReading("ref", "Temp", 30.0, "°C");
var current = new SensorReading("S1", "Temp", 32.5, "°C");

if (current > threshold)
{
	// Trigger high temperature alert
}

if (current >= threshold)  // Greater than or equal
if (current < threshold)   // Less than
if (current <= threshold)  // Less than or equal
if (current == threshold)  // Equals
if (current != threshold)  // Not equals
```

### Static Aggregation Methods

```csharp
// Aggregate multiple readings
var reading1 = new SensorReading("S1", "Power", 100, "W");
var reading2 = new SensorReading("S2", "Power", 150, "W");
var reading3 = new SensorReading("S3", "Power", 120, "W");

var totalPower = SensorReading.Aggregate(reading1, reading2, reading3);  // 370W

// Calculate average
var avgPower = SensorReading.Average(reading1, reading2, reading3);  // ~123.33W

// Find extremes
var maxReading = SensorReading.Max(reading1, reading2, reading3);
var minReading = SensorReading.Min(reading1, reading2, reading3);
```

### Use Cases

#### Smart Meter Aggregation
```csharp
public class BuildingEnergyAnalyzer
{
	public SensorReading CalculateTotalConsumption(List<SensorReading> meterReadings)
	{
		var total = new SensorReading("building", "Power", 0, "W");
		foreach (var meter in meterReadings)
		{
			total = total + meter;  // Clean, intuitive aggregation
		}
		return total;
	}
}
```

#### Temperature Anomaly Detection
```csharp
public class TemperatureMonitor
{
	private SensorReading _baseline;

	public bool DetectAnomaly(SensorReading current)
	{
		var delta = current - _baseline;
		var anomalyThreshold = new SensorReading("ref", "Temp", 5.0, "°C");

		return Math.Abs(delta.Value) > anomalyThreshold.Value;  // Exceeded 5°C change
	}
}
```

#### Load Balancing Across Sensors
```csharp
public class LoadBalancer
{
	public void BalanceSensors(List<SensorReading> sensors)
	{
		var average = SensorReading.Average(sensors.ToArray());

		foreach (var sensor in sensors)
		{
			if (sensor > average)
			{
				ReduceLoad(sensor);
			}
			else if (sensor < average)
			{
				IncreaseLoad(sensor);
			}
		}
	}
}
```

---

## Feature 3: Advanced Arrays & Lists - TelemetryBatchProcessor

### Purpose
`TelemetryBatchProcessor` demonstrates advanced array usage by storing raw telemetry in multi-dimensional jagged arrays, then provides optimized algorithms to migrate this data into `List<T>` collections. Ideal for batch processing and ingestion pipelines.

### File Location
```
Models/TelemetryBatchProcessor.cs
```

### Architecture

#### Jagged Array Structure
```
_sensorRawBatches: Dictionary<string, double[][]>
├── "00:1A:2B:3C:4D:5E" → double[3][]  (3 batches for sensor 1)
│   ├── [0] → [23.5, 24.1, 25.0, 26.2]
│   ├── [1] → [26.5, 27.1, 28.0]
│   └── [2] → [28.5, 29.1]
└── "00:1A:2B:3C:4D:5F" → double[2][]  (2 batches for sensor 2)
	├── [0] → [100.5, 105.3, 110.0]
	└── [1] → [112.0, 115.5]
```

#### Metadata Tracking
```
_sensorMetadata: Dictionary<string, (string DataType, string Unit)[]>
_sensorTimestamps: Dictionary<string, DateTime[][]>
```

### Usage Examples

#### Basic Batch Addition
```csharp
var processor = new TelemetryBatchProcessor(batchSizeThreshold: 100);

// Add raw temperature data
processor.AddRawBatch(
	sensorMac: "00:1A:2B:3C:4D:5E",
	values: new[] { 23.5, 24.1, 25.0, 26.2 },
	dataType: "temperature",
	unit: "°C"
);

// Add power data
processor.AddRawBatch(
	sensorMac: "00:1A:2B:3C:4D:5F",
	values: new[] { 100.5, 105.3, 110.0 },
	dataType: "powerWattage",
	unit: "W"
);
```

#### Migration with Timestamps
```csharp
var timestamps = new[]
{
	DateTime.UtcNow.AddMinutes(-30),
	DateTime.UtcNow.AddMinutes(-20),
	DateTime.UtcNow.AddMinutes(-10)
};

processor.AddRawBatchWithTimestamps(
	sensorMac: "00:1A:2B:3C:4D:5E",
	values: new[] { 23.5, 24.1, 25.0 },
	timestamps: timestamps,
	dataType: "temperature",
	unit: "°C"
);
```

#### Optimized Migration Algorithm
```csharp
// Shift raw array data to List<TelemetryPacket<double>>
var telemetryList = processor.MigrateBatchesToList();

// Result is ready for database insertion or further processing
Console.WriteLine($"Migrated {telemetryList.Count} telemetry packets");

// Direct database insertion
var telemetryData = processor.MigrateBatchesToTelemetryData();
_context.TelemetryData.AddRange(telemetryData);
_context.SaveChangesAsync();
```

#### Batch Monitoring
```csharp
var info = processor.GetSensorBatchInfo("00:1A:2B:3C:4D:5E");
if (info.HasValue)
{
	Console.WriteLine($"Batches: {info.Value.BatchCount}");
	Console.WriteLine($"Total Values: {info.Value.TotalValues}");
	Console.WriteLine($"Data Type: {info.Value.DataType}");
	Console.WriteLine($"Unit: {info.Value.Unit}");
}

// Check if any sensor is ready for migration
if (processor.HasBatchesReadyForMigration())
{
	var readySensors = processor.GetSensorsReadyForMigration();
	Console.WriteLine($"Ready to migrate: {string.Join(", ", readySensors)}");
}

// Get detailed statistics
Console.WriteLine(processor.GetDetailedReport());
```

### Key Methods

| Method | Purpose |
|--------|---------|
| `AddRawBatch()` | Add raw values for a sensor |
| `AddRawBatchWithTimestamps()` | Add values with explicit timestamps |
| `MigrateBatchesToList()` | Shift arrays to List<TelemetryPacket<double>> |
| `MigrateBatchesToTelemetryData()` | Direct conversion for DB insertion |
| `HasBatchesReadyForMigration()` | Check if thresholds exceeded |
| `GetDetailedReport()` | Statistics and monitoring |

### Real-World Scenario

```csharp
public class TelemetryIngestionPipeline
{
	private readonly TelemetryBatchProcessor _processor;
	private readonly SmartIotContext _context;

	public TelemetryIngestionPipeline(int batchSize = 500)
	{
		_processor = new TelemetryBatchProcessor(batchSize);
	}

	public async Task ProcessIncomingSensorDataAsync(string sensorMac, double[] readings)
	{
		// Stage 1: Store in jagged arrays
		_processor.AddRawBatch(sensorMac, readings, "temperature", "°C");

		// Stage 2: Check if batch threshold reached
		if (_processor.HasBatchesReadyForMigration())
		{
			// Stage 3: Migrate to List<T>
			var telemetryData = _processor.MigrateBatchesToTelemetryData();

			// Stage 4: Bulk insert to database
			_context.TelemetryData.AddRange(telemetryData);
			await _context.SaveChangesAsync();

			Console.WriteLine($"Ingested {telemetryData.Count} telemetry records");
		}
	}
}
```

---

## Feature 4: Telemetry Simulator Service

### Purpose
`TelemetrySimulatorService` is a background service that generates random telemetry data every 10 seconds and POSTs it to the API. Runs only in Development environment.

### File Location
```
Services/TelemetrySimulatorService.cs
```

### How It Works

1. **Service Registration** (Program.cs):
   ```csharp
   builder.Services.AddSingleton<TelemetrySimulatorService>();
   builder.Services.AddHostedService(provider => provider.GetRequiredService<TelemetrySimulatorService>());
   ```

2. **Startup**: When app starts, simulator initializes an HTTP client and starts a 10-second timer

3. **Generation**: Every 10 seconds:
   - Fetches all active sensors from database
   - Generates category-specific random data
   - POSTs to `/api/telemetrydata` endpoint

4. **Data Generation by Category**:

| Category | Data Type | Range | Pattern |
|----------|-----------|-------|---------|
| Environmental | temperature | 15-40°C | Sinusoidal drift with variation |
| Power | powerWattage | 200-500W | Base load with random spikes |
| Actuator | valveState | 0 or 1 | 70% on, 30% off |
| Temperature | temperature | 15-40°C | Same as environmental |
| Humidity | humidity | 20-90% | Random variation |
| (Other) | genericReading | 0-100 | Random value |

### Example Data Generation Output

```
Timestamp: 2024-01-15 10:30:00 UTC
Sensor: 00:1A:2B:3C:4D:5E (Environmental)
Type: temperature
Value: 24.7°C

Timestamp: 2024-01-15 10:30:00 UTC
Sensor: 00:1A:2B:3C:4D:5F (Power)
Type: powerWattage
Value: 342W

Timestamp: 2024-01-15 10:30:00 UTC
Sensor: 00:1A:2B:3C:4D:60 (Actuator)
Type: valveState
Value: 1 (ON)
```

### Monitoring Simulator Activity

Check the application logs to see simulator activity:

```
[INF] TelemetrySimulatorService initialized with base URL: http://localhost:5231
[INF] Starting TelemetrySimulatorService...
[INF] TelemetrySimulatorService started successfully. Telemetry will be generated every 10 seconds.
[DBG] Generating telemetry for 3 active sensors.
[DBG] Telemetry sent successfully for sensor 00:1A:2B:3C:4D:5E: temperature=24.7
[DBG] Telemetry sent successfully for sensor 00:1A:2B:3C:4D:5F: powerWattage=342
[DBG] Telemetry sent successfully for sensor 00:1A:2B:3C:4D:60: valveState=1
```

### Environment Control

Simulator only runs in Development:
```csharp
if (!_environment.IsDevelopment())
{
	_logger.LogWarning("Telemetry simulator configured for Development only.");
	return Task.CompletedTask;
}
```

To disable in Development, set `ASPNETCORE_ENVIRONMENT=Production`.

---

## Integration Example: Complete Pipeline

### Scenario: Real-time telemetry ingestion with aggregation and alerting

```csharp
public class TelemetryPipeline
{
	private readonly SmartIotContext _context;
	private readonly TelemetryBatchProcessor _processor;

	public async Task ProcessTelemetryAsync()
	{
		// Step 1: Simulator generates data every 10 seconds (background)
		// Data automatically POSTs to /api/telemetrydata

		// Step 2: Aggregate power readings across multiple meters
		var powerReadings = _context.TelemetryData
			.Where(t => t.DataType == "powerWattage")
			.Select(t => new SensorReading(
				t.SensorMac,
				t.DataType,
				t.DataValue ?? 0,
				"W"
			))
			.ToList();

		var totalPower = SensorReading.Aggregate(powerReadings.ToArray());
		Console.WriteLine($"Total power consumption: {totalPower.Value}W");

		// Step 3: Detect temperature anomalies
		var tempReadings = _context.TelemetryData
			.Where(t => t.DataType == "temperature")
			.Select(t => new SensorReading(
				t.SensorMac,
				t.DataType,
				t.DataValue ?? 0,
				"°C"
			))
			.ToList();

		var avgTemp = SensorReading.Average(tempReadings.ToArray());
		var threshold = new SensorReading("ref", "temperature", avgTemp.Value + 5, "°C");

		foreach (var temp in tempReadings)
		{
			if (temp > threshold)
			{
				Console.WriteLine($"ALERT: {temp.SensorMac} exceeded temperature threshold!");
			}
		}

		// Step 4: Use batch processor for efficient ingestion
		_processor.AddRawBatch(
			"00:1A:2B:3C:4D:5E",
			new[] { 23.5, 24.1, 25.0, 26.2 },
			"temperature",
			"°C"
		);

		if (_processor.HasBatchesReadyForMigration())
		{
			var telemetryData = _processor.MigrateBatchesToTelemetryData();
			_context.TelemetryData.AddRange(telemetryData);
			await _context.SaveChangesAsync();
		}
	}
}
```

---

## Testing & Verification

### Build Verification
```bash
dotnet build SmartIotApi.csproj
# Should output: Build successful
```

### Runtime Verification
```bash
dotnet run
# Watch application logs for simulator activity
# Check database for growing TelemetryData records
```

### API Testing
```bash
# Get all telemetry
curl http://localhost:5231/api/telemetrydata

# Should see records with simulator-generated data
# Example response:
[
  {
	"telemetryId": 1,
	"sensorMac": "00:1A:2B:3C:4D:5E",
	"dataType": "temperature",
	"dataValue": 24.7,
	"timestamp": "2024-01-15T10:30:00Z"
  },
  ...
]
```

---

## Performance Considerations

### TelemetryPacket<T>
- **Struct Overhead**: ~56 bytes per instance (64-bit system)
- **GC Pressure**: Near-zero for value types
- **Cache Efficiency**: Sequential allocation improves L1/L2 cache hits

### SensorReading
- **Operator Cost**: O(1) for all arithmetic operations
- **Memory**: ~64 bytes per instance
- **Aggregation**: O(n) for n readings

### TelemetryBatchProcessor
- **Array Storage**: O(n) memory where n = total values
- **Migration**: O(n) time complexity for shift operation
- **Threshold Detection**: O(1) amortized

### Simulator
- **Frequency**: 10-second intervals (6 requests/minute per sensor)
- **Payload**: ~150 bytes per POST
- **Database Impact**: ~6 inserts/minute per sensor

---

## Future Enhancements

1. **Distributed Simulation**: Run simulators across multiple instances
2. **Configurable Data Patterns**: Load realistic sensor profiles
3. **Performance Metrics**: Track ingestion rates and latency
4. **Stream Processing**: Integrate with Kafka/RabbitMQ
5. **Compression**: Compress array data for archival
6. **Parallel Processing**: Multi-threaded array migration

---

## Troubleshooting

### Simulator Not Running
- Check environment: Must be Development
- Verify API URL is correct
- Check application logs for errors

### Build Errors
- Ensure all `using` statements are present
- Verify .NET version is 10+
- Run `dotnet restore` to restore dependencies

### Database Not Growing
- Verify sensors are marked `IsActive = true`
- Check API endpoint: `/api/telemetrydata` must exist
- Monitor logs for POST failures

---

## Summary

These four features provide a complete, production-ready telemetry ingestion pipeline:

✅ **Generics** - Type-safe, efficient data packaging
✅ **Operators** - Intuitive sensor data aggregation and analysis
✅ **Arrays/Lists** - High-performance batch processing
✅ **Simulator** - Continuous data generation for testing and development

All features work together to support modern IoT applications at scale.
