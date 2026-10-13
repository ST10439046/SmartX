// Telemetry batch processor: groups and processes telemetry records so batches can be handled consistently.
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartIotApi.Models;

/// <summary>
/// TelemetryBatchProcessor handles efficient storage and processing of raw telemetry data
/// using a multi-dimensional/jagged array structure, then provides optimized algorithms
/// to migrate this raw array data into dynamic List{T} collections for ingestion.
/// 
/// This class demonstrates advanced array usage and demonstrates memory-efficient
/// batch processing patterns suitable for high-volume telemetry ingestion scenarios.
/// 
/// Architecture:
/// - Raw data is stored in jagged arrays (double[][]) for compact memory layout
/// - Each sub-array represents a batch of readings for a specific sensor
/// - Optimized shift/migration algorithm moves data from arrays to List{T} for processing
/// - Supports multiple data types through generic wrappers
/// 
/// Usage example:
///   var processor = new TelemetryBatchProcessor(10);  // 10-item batches
///   processor.AddRawBatch("00:1A:2B:3C:4D:5E", new[] { 23.5, 24.1, 25.0, 26.2 });
///   processor.AddRawBatch("00:1A:2B:3C:4D:5F", new[] { 100.5, 105.3, 110.0 });
///   
///   var telemetryList = processor.MigrateBatchesToList();  // Shift to List{TelemetryPacket{double}}
/// </summary>
public class TelemetryBatchProcessor
{
    /// <summary>
    /// The target batch size. Batches larger than this trigger migration to List{T}.
    /// </summary>
    private readonly int _batchSizeThreshold;

    /// <summary>
    /// Jagged array storing raw telemetry data, organized by sensor MAC.
    /// First dimension: sensor index
    /// Second dimension: raw telemetry values for that sensor
    /// </summary>
    private Dictionary<string, double[][]> _sensorRawBatches = new();

    /// <summary>
    /// Jagged array for storing sensor metadata (MAC addresses, types).
    /// Corresponds to _sensorRawBatches organization.
    /// </summary>
    private Dictionary<string, (string DataType, string Unit)[]> _sensorMetadata = new();

    /// <summary>
    /// Jagged array for storing timestamps associated with raw data.
    /// </summary>
    private Dictionary<string, DateTime[][]> _sensorTimestamps = new();

    /// <summary>
    /// Constructor for TelemetryBatchProcessor.
    /// </summary>
    /// <param name="batchSizeThreshold">
    /// Number of readings at which a batch is considered full and should be migrated.
    /// Default is 100 readings per batch.
    /// </param>
    public TelemetryBatchProcessor(int batchSizeThreshold = 100)
    {
        if (batchSizeThreshold <= 0)
            throw new ArgumentException("Batch size threshold must be greater than 0.", nameof(batchSizeThreshold));

        _batchSizeThreshold = batchSizeThreshold;
    }

    /// <summary>
    /// Adds a raw batch of telemetry values for a specific sensor.
    /// Data is stored in a jagged array structure for memory efficiency.
    /// </summary>
    /// <param name="sensorMac">MAC address of the sensor</param>
    /// <param name="values">Array of raw telemetry values (e.g., temperatures, power readings)</param>
    /// <param name="dataType">Type of data (e.g., "temperature", "powerWattage")</param>
    /// <param name="unit">Unit of measurement (e.g., "°C", "W")</param>
    public void AddRawBatch(string sensorMac, double[] values, string dataType = "reading", string unit = "")
    {
        if (string.IsNullOrWhiteSpace(sensorMac))
            throw new ArgumentException("Sensor MAC cannot be null or empty.", nameof(sensorMac));

        if (values == null || values.Length == 0)
            throw new ArgumentException("Values array cannot be null or empty.", nameof(values));

        // Initialize jagged array for this sensor if not present
        if (!_sensorRawBatches.ContainsKey(sensorMac))
        {
            _sensorRawBatches[sensorMac] = new double[0][];
            _sensorMetadata[sensorMac] = new (string, string)[0];
            _sensorTimestamps[sensorMac] = new DateTime[0][];
        }

        // Expand jagged array to accommodate new batch
        var currentBatches = _sensorRawBatches[sensorMac];
        Array.Resize(ref currentBatches, currentBatches.Length + 1);
        currentBatches[currentBatches.Length - 1] = values;
        _sensorRawBatches[sensorMac] = currentBatches;

        // Expand metadata array
        var currentMetadata = _sensorMetadata[sensorMac];
        Array.Resize(ref currentMetadata, currentMetadata.Length + 1);
        currentMetadata[currentMetadata.Length - 1] = (dataType, unit);
        _sensorMetadata[sensorMac] = currentMetadata;

        // Expand timestamp array with current time for all values
        var currentTimestamps = _sensorTimestamps[sensorMac];
        Array.Resize(ref currentTimestamps, currentTimestamps.Length + 1);
        currentTimestamps[currentTimestamps.Length - 1] = values.Select(_ => DateTime.UtcNow).ToArray();
        _sensorTimestamps[sensorMac] = currentTimestamps;
    }

    /// <summary>
    /// Adds a raw batch with explicit timestamps for each value.
    /// </summary>
    /// <param name="sensorMac">MAC address of the sensor</param>
    /// <param name="values">Array of raw telemetry values</param>
    /// <param name="timestamps">Array of timestamps (must match values.Length)</param>
    /// <param name="dataType">Type of data</param>
    /// <param name="unit">Unit of measurement</param>
    public void AddRawBatchWithTimestamps(string sensorMac, double[] values, DateTime[] timestamps, 
        string dataType = "reading", string unit = "")
    {
        if (values == null || values.Length != timestamps?.Length)
            throw new ArgumentException("Values and timestamps arrays must have the same length.", nameof(timestamps));

        // Initialize jagged array for this sensor if not present
        if (!_sensorRawBatches.ContainsKey(sensorMac))
        {
            _sensorRawBatches[sensorMac] = new double[0][];
            _sensorMetadata[sensorMac] = new (string, string)[0];
            _sensorTimestamps[sensorMac] = new DateTime[0][];
        }

        // Expand jagged arrays
        var currentBatches = _sensorRawBatches[sensorMac];
        Array.Resize(ref currentBatches, currentBatches.Length + 1);
        currentBatches[currentBatches.Length - 1] = values;
        _sensorRawBatches[sensorMac] = currentBatches;

        var currentMetadata = _sensorMetadata[sensorMac];
        Array.Resize(ref currentMetadata, currentMetadata.Length + 1);
        currentMetadata[currentMetadata.Length - 1] = (dataType, unit);
        _sensorMetadata[sensorMac] = currentMetadata;

        var currentTimestamps = _sensorTimestamps[sensorMac];
        Array.Resize(ref currentTimestamps, currentTimestamps.Length + 1);
        currentTimestamps[currentTimestamps.Length - 1] = timestamps;
        _sensorTimestamps[sensorMac] = currentTimestamps;
    }

    /// <summary>
    /// Migrates raw telemetry data from jagged arrays to a List{TelemetryPacket{double}} collection.
    /// This demonstrates the optimized "shift" algorithm mentioned in requirements.
    /// 
    /// Algorithm:
    /// 1. Iterate through each sensor's jagged array batches
    /// 2. Flatten each batch from multi-dimensional to single dimension
    /// 3. Create TelemetryPacket{double} wrappers for each value
    /// 4. Shift data into target List{T} collection
    /// 5. Clear source arrays to free memory
    /// </summary>
    /// <returns>List of telemetry packets ready for ingestion</returns>
    public List<TelemetryPacket<double>> MigrateBatchesToList()
    {
        var result = new List<TelemetryPacket<double>>();

        // Iterate through each sensor's batches
        foreach (var sensorMac in _sensorRawBatches.Keys)
        {
            var batches = _sensorRawBatches[sensorMac];
            var metadata = _sensorMetadata[sensorMac];
            var timestamps = _sensorTimestamps[sensorMac];

            // Flatten and migrate each batch from array to List
            for (int batchIndex = 0; batchIndex < batches.Length; batchIndex++)
            {
                var batchValues = batches[batchIndex];
                var (dataType, unit) = metadata[batchIndex];
                var batchTimestamps = timestamps[batchIndex];

                // Shift: Convert array elements to TelemetryPacket and add to list
                for (int valueIndex = 0; valueIndex < batchValues.Length; valueIndex++)
                {
                    var packet = new TelemetryPacket<double>(
                        sensorMac,
                        batchValues[valueIndex],
                        dataType,
                        batchTimestamps[valueIndex],
                        unit
                    );

                    result.Add(packet);
                }
            }
        }

        // Clear source arrays to free memory
        Clear();

        return result;
    }

    /// <summary>
    /// Alternative migration method that returns List{TelemetryDatum} directly for database storage.
    /// Useful when database insertion is the final destination.
    /// </summary>
    public List<TelemetryDatum> MigrateBatchesToTelemetryData()
    {
        var packets = MigrateBatchesToList();
        return packets.Select(p => p.ToTelemetryDatum()).ToList();
    }

    /// <summary>
    /// Gets the total number of values across all batches and sensors.
    /// </summary>
    public int GetTotalValueCount()
    {
        return _sensorRawBatches.Sum(kvp => kvp.Value.Sum(batch => batch.Length));
    }

    /// <summary>
    /// Gets the total number of batches across all sensors.
    /// </summary>
    public int GetTotalBatchCount()
    {
        return _sensorRawBatches.Sum(kvp => kvp.Value.Length);
    }

    /// <summary>
    /// Gets the number of sensors currently being tracked.
    /// </summary>
    public int GetSensorCount()
    {
        return _sensorRawBatches.Count;
    }

    /// <summary>
    /// Gets the batch information for a specific sensor.
    /// </summary>
    public (int BatchCount, int TotalValues, string DataType, string Unit)? GetSensorBatchInfo(string sensorMac)
    {
        if (!_sensorRawBatches.ContainsKey(sensorMac))
            return null;

        var batches = _sensorRawBatches[sensorMac];
        var metadata = _sensorMetadata[sensorMac];

        return (
            BatchCount: batches.Length,
            TotalValues: batches.Sum(b => b.Length),
            DataType: batches.Length > 0 ? metadata[0].DataType : "unknown",
            Unit: batches.Length > 0 ? metadata[0].Unit : ""
        );
    }

    /// <summary>
    /// Gets all sensor MACs currently being tracked.
    /// </summary>
    public IEnumerable<string> GetTrackedSensors()
    {
        return _sensorRawBatches.Keys;
    }

    /// <summary>
    /// Determines if any sensor has reached or exceeded the batch size threshold.
    /// </summary>
    public bool HasBatchesReadyForMigration()
    {
        return _sensorRawBatches.Any(kvp => kvp.Value.Sum(b => b.Length) >= _batchSizeThreshold);
    }

    /// <summary>
    /// Gets sensors that have reached or exceeded the batch size threshold.
    /// </summary>
    public IEnumerable<string> GetSensorsReadyForMigration()
    {
        return _sensorRawBatches
            .Where(kvp => kvp.Value.Sum(b => b.Length) >= _batchSizeThreshold)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Clears all stored batches and metadata, freeing memory.
    /// </summary>
    public void Clear()
    {
        _sensorRawBatches.Clear();
        _sensorMetadata.Clear();
        _sensorTimestamps.Clear();
    }

    /// <summary>
    /// Returns a summary string of current batch statistics.
    /// </summary>
    public override string ToString()
    {
        var sensorCount = GetSensorCount();
        var totalBatches = GetTotalBatchCount();
        var totalValues = GetTotalValueCount();

        return $"TelemetryBatchProcessor: {sensorCount} sensors, {totalBatches} batches, {totalValues} values (threshold: {_batchSizeThreshold})";
    }

    /// <summary>
    /// Generates a detailed report of all stored batches.
    /// </summary>
    public string GetDetailedReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine($"=== Telemetry Batch Processor Report ===");
        report.AppendLine($"Total Sensors: {GetSensorCount()}");
        report.AppendLine($"Total Batches: {GetTotalBatchCount()}");
        report.AppendLine($"Total Values: {GetTotalValueCount()}");
        report.AppendLine($"Batch Size Threshold: {_batchSizeThreshold}");
        report.AppendLine();

        foreach (var sensorMac in GetTrackedSensors())
        {
            var info = GetSensorBatchInfo(sensorMac);
            if (info.HasValue)
            {
                report.AppendLine($"Sensor: {sensorMac}");
                report.AppendLine($"  Batches: {info.Value.BatchCount}");
                report.AppendLine($"  Total Values: {info.Value.TotalValues}");
                report.AppendLine($"  Data Type: {info.Value.DataType}");
                report.AppendLine($"  Unit: {info.Value.Unit}");
            }
        }

        report.AppendLine();
        report.AppendLine($"Ready for Migration: {string.Join(", ", GetSensorsReadyForMigration())}");

        return report.ToString();
    }
}
