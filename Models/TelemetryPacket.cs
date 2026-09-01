using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

/// <summary>
/// A reusable generic wrapper struct for handling disparate sensor structures uniformly
/// without boxing/unboxing overhead. This value type stores sensor data with metadata
/// and provides built-in validation and serialization support.
/// 
/// Usage examples:
/// - TelemetryPacket{float} for temperature sensors
/// - TelemetryPacket{int} for counter/power sensors
/// - TelemetryPacket{bool} for state sensors (on/off, open/closed)
/// 
/// By using a struct instead of a class, we avoid heap allocation and garbage collection
/// pressure, making it ideal for high-frequency telemetry ingestion at scale.
/// </summary>
/// <typeparam name="T">The type of the sensor value (float, int, bool, double, etc.)</typeparam>
public struct TelemetryPacket<T> : IEquatable<TelemetryPacket<T>>
    where T : struct, IComparable<T>, IFormattable
{
    /// <summary>
    /// The MAC address of the sensor generating this telemetry packet.
    /// </summary>
    public string SensorMac { get; set; }

    /// <summary>
    /// The raw sensor value of the specified type (T).
    /// For example: 23.5 (temperature), 312 (wattage), 1 (valve state).
    /// </summary>
    public T Value { get; set; }

    /// <summary>
    /// The timestamp when this telemetry was recorded (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// A descriptive label for the data type being measured.
    /// Examples: "temperature", "powerWattage", "valveState", "humidity".
    /// </summary>
    public string DataType { get; set; }

    /// <summary>
    /// Optional unit of measurement (e.g., "°C", "W", "V", "%").
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Optional metadata or notes about this telemetry packet.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Indicates whether this packet represents valid, usable data.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Constructor for creating a TelemetryPacket with all required fields.
    /// </summary>
    /// <param name="sensorMac">MAC address of the sensor</param>
    /// <param name="value">The sensor reading value</param>
    /// <param name="dataType">Description of the data type</param>
    /// <param name="unit">Optional unit of measurement</param>
    /// <param name="metadata">Optional additional metadata</param>
    public TelemetryPacket(string sensorMac, T value, string dataType, string? unit = null, string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(sensorMac))
            throw new ArgumentException("Sensor MAC address cannot be null or empty.", nameof(sensorMac));

        if (string.IsNullOrWhiteSpace(dataType))
            throw new ArgumentException("Data type cannot be null or empty.", nameof(dataType));

        SensorMac = sensorMac;
        Value = value;
        Timestamp = DateTime.UtcNow;
        DataType = dataType;
        Unit = unit;
        Metadata = metadata;
        IsValid = true;
    }

    /// <summary>
    /// Constructor for creating a TelemetryPacket with a custom timestamp.
    /// </summary>
    public TelemetryPacket(string sensorMac, T value, string dataType, DateTime timestamp, string? unit = null, string? metadata = null)
        : this(sensorMac, value, dataType, unit, metadata)
    {
        Timestamp = timestamp;
    }

    /// <summary>
    /// Validates that this packet's data satisfies optional constraints.
    /// Override or extend this method for specific validation logic per sensor type.
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool Validate()
    {
        // Basic validation: ensure MAC is in valid format (XX:XX:XX:XX:XX:XX)
        if (!IsValidMacAddress(SensorMac))
        {
            IsValid = false;
            return false;
        }

        // Ensure timestamp is not in the future (allow 1 minute clock skew tolerance)
        if (Timestamp > DateTime.UtcNow.AddMinutes(1))
        {
            IsValid = false;
            return false;
        }

        IsValid = true;
        return true;
    }

    /// <summary>
    /// Converts this TelemetryPacket to a TelemetryDatum for database storage.
    /// This bridges the gap between the generic packet structure and the ORM model.
    /// </summary>
    public TelemetryDatum ToTelemetryDatum()
    {
        return new TelemetryDatum
        {
            SensorMac = SensorMac,
            DataValue = Convert.ToDouble(Value),
            DataType = DataType,
            Timestamp = Timestamp
        };
    }

    /// <summary>
    /// Creates a TelemetryPacket from an existing TelemetryDatum.
    /// Performs necessary type conversion from double to T.
    /// </summary>
    public static TelemetryPacket<T> FromTelemetryDatum(TelemetryDatum datum)
    {
        if (datum == null)
            throw new ArgumentNullException(nameof(datum));

        var dataValue = datum.DataValue.HasValue ? datum.DataValue.Value : Convert.ToDouble(default(T));
        T value = (T)Convert.ChangeType(dataValue, typeof(T));

        return new TelemetryPacket<T>(
            datum.SensorMac,
            value,
            datum.DataType ?? "unknown",
            datum.Timestamp ?? DateTime.UtcNow
        );
    }

    /// <summary>
    /// Creates a copy of this packet with a new value while preserving metadata.
    /// Useful for transformations and aggregations.
    /// </summary>
    public TelemetryPacket<T> WithValue(T newValue)
    {
        return new TelemetryPacket<T>(SensorMac, newValue, DataType, Timestamp, Unit, Metadata);
    }

    /// <summary>
    /// Creates a copy of this packet with a new timestamp.
    /// </summary>
    public TelemetryPacket<T> WithTimestamp(DateTime timestamp)
    {
        return new TelemetryPacket<T>(SensorMac, Value, DataType, timestamp, Unit, Metadata);
    }

    /// <summary>
    /// Returns a formatted string representation of this telemetry packet.
    /// </summary>
    public override string ToString()
    {
        var unitStr = string.IsNullOrEmpty(Unit) ? "" : $" {Unit}";
        var metadataStr = string.IsNullOrEmpty(Metadata) ? "" : $" [{Metadata}]";
        return $"{DataType}: {Value}{unitStr} @ {Timestamp:O} ({SensorMac}){metadataStr}";
    }

    /// <summary>
    /// Returns a JSON-serializable representation of this packet.
    /// </summary>
    public Dictionary<string, object> ToJson()
    {
        return new Dictionary<string, object>
        {
            { "sensorMac", SensorMac },
            { "value", Value },
            { "dataType", DataType },
            { "timestamp", Timestamp },
            { "unit", Unit ?? "" },
            { "metadata", Metadata ?? "" },
            { "isValid", IsValid }
        };
    }

    /// <summary>
    /// Equality comparison for TelemetryPacket structs.
    /// </summary>
    public bool Equals(TelemetryPacket<T> other)
    {
        return SensorMac == other.SensorMac &&
               Value.Equals(other.Value) &&
               Timestamp == other.Timestamp &&
               DataType == other.DataType;
    }

    /// <summary>
    /// Equality comparison override.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is TelemetryPacket<T> other && Equals(other);
    }

    /// <summary>
    /// Hash code override for use in collections.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(SensorMac, Value, Timestamp, DataType);
    }

    /// <summary>
    /// Helper method to validate MAC address format.
    /// Expected format: XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX
    /// </summary>
    private static bool IsValidMacAddress(string mac)
    {
        if (string.IsNullOrEmpty(mac))
            return false;

        // Allow both colon and hyphen separators
        var parts = mac.Split(new[] { ':', '-' }, StringSplitOptions.None);

        if (parts.Length != 6)
            return false;

        // Each part should be exactly 2 hex characters
        foreach (var part in parts)
        {
            if (part.Length != 2 || !part.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Operator overload: Equality comparison.
    /// </summary>
    public static bool operator ==(TelemetryPacket<T> left, TelemetryPacket<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Operator overload: Inequality comparison.
    /// </summary>
    public static bool operator !=(TelemetryPacket<T> left, TelemetryPacket<T> right)
    {
        return !left.Equals(right);
    }
}
