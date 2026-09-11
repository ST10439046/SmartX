using System;

namespace SmartIotApi.Models;

/// <summary>
/// Represents a single sensor reading with value and metadata.
/// Implements operator overloading to enable direct aggregation and delta calculations.
/// 
/// This class allows natural mathematical operations on sensor readings:
/// - Addition: aggregate loads (Meter3 = Meter1 + Meter2)
/// - Subtraction: delta comparisons (Delta = Current - Previous)
/// - Division: averaging/normalization
/// - Multiplication: scaling/calibration
/// - Comparison: threshold detection (<, >, ==, !=)
/// 
/// Usage example:
///   var meter1 = new SensorReading("00:1A:2B:3C:4D:5E", "Power", 100.5, "W");
///   var meter2 = new SensorReading("00:1A:2B:3C:4D:5F", "Power", 50.3, "W");
///   var totalPower = meter1 + meter2;  // Creates new SensorReading with value 150.8
///   
///   var threshold = new SensorReading("ref", "Temp", 25.0, "°C");
///   if (sensorReading > threshold) { /* too hot */ }
/// </summary>
public class SensorReading : IComparable<SensorReading>, IEquatable<SensorReading>
{
    /// <summary>
    /// The MAC address of the sensor generating this reading.
    /// </summary>
    public string SensorMac { get; set; }

    /// <summary>
    /// A descriptive label for this reading (e.g., "temperature", "powerWattage", "humidity").
    /// </summary>
    public string ReadingType { get; set; }

    /// <summary>
    /// The numeric value of this reading.
    /// Stored as double to accommodate a wide range of sensor types.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The unit of measurement (e.g., "°C", "W", "%", "V").
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// The timestamp when this reading was recorded (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Constructor for creating a SensorReading with all fields.
    /// </summary>
    /// <param name="sensorMac">MAC address of the sensor</param>
    /// <param name="readingType">Type/category of reading</param>
    /// <param name="value">The numeric reading value</param>
    /// <param name="unit">Unit of measurement</param>
    /// <param name="timestamp">When the reading was taken (defaults to now)</param>
    public SensorReading(string sensorMac, string readingType, double value, string unit, DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(sensorMac))
            throw new ArgumentException("Sensor MAC cannot be null or empty.", nameof(sensorMac));

        if (string.IsNullOrWhiteSpace(readingType))
            throw new ArgumentException("Reading type cannot be null or empty.", nameof(readingType));

        SensorMac = sensorMac;
        ReadingType = readingType;
        Value = value;
        Unit = unit;
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Addition operator: Combines two sensor readings by summing their values.
    /// Useful for aggregating sensor data (e.g., total power consumption).
    /// 
    /// The result uses the first reading's metadata and current timestamp.
    /// </summary>
    /// <param name="left">First sensor reading</param>
    /// <param name="right">Second sensor reading to add</param>
    /// <returns>A new SensorReading with the sum of both values</returns>
    public static SensorReading operator +(SensorReading left, SensorReading right)
    {
        if (left == null || right == null)
            throw new ArgumentNullException(left == null ? nameof(left) : nameof(right));

        // Ensure readings are of compatible types
        if (!left.ReadingType.Equals(right.ReadingType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot add readings of different types: {left.ReadingType} and {right.ReadingType}");

        return new SensorReading(
            $"{left.SensorMac}+{right.SensorMac}",
            left.ReadingType,
            left.Value + right.Value,
            left.Unit,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Subtraction operator: Calculates the delta between two sensor readings.
    /// Useful for detecting changes, deltas, and threshold violations.
    /// 
    /// Example: Delta = CurrentReading - PreviousReading
    /// </summary>
    /// <param name="left">First sensor reading (minuend)</param>
    /// <param name="right">Second sensor reading (subtrahend)</param>
    /// <returns>A new SensorReading representing the difference</returns>
    public static SensorReading operator -(SensorReading left, SensorReading right)
    {
        if (left == null || right == null)
            throw new ArgumentNullException(left == null ? nameof(left) : nameof(right));

        if (!left.ReadingType.Equals(right.ReadingType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Cannot subtract readings of different types: {left.ReadingType} and {right.ReadingType}");

        return new SensorReading(
            $"{left.SensorMac}-delta",
            $"{left.ReadingType}_delta",
            left.Value - right.Value,
            left.Unit,
            DateTime.UtcNow
        );
    }

    /// <summary>
    /// Multiplication operator: Scales a sensor reading by a factor.
    /// Useful for unit conversions (e.g., kW to W) or calibration adjustments.
    /// 
    /// Example: ScaledPower = OriginalPower * 1000 (convert kW to W)
    /// </summary>
    /// <param name="reading">The sensor reading to scale</param>
    /// <param name="factor">The scaling factor</param>
    /// <returns>A new SensorReading with scaled value</returns>
    public static SensorReading operator *(SensorReading reading, double factor)
    {
        if (reading == null)
            throw new ArgumentNullException(nameof(reading));

        return new SensorReading(
            reading.SensorMac,
            reading.ReadingType,
            reading.Value * factor,
            reading.Unit,
            reading.Timestamp
        );
    }

    /// <summary>
    /// Multiplication operator: Scales a sensor reading by a factor (reverse operand order).
    /// </summary>
    /// <param name="factor">The scaling factor</param>
    /// <param name="reading">The sensor reading to scale</param>
    /// <returns>A new SensorReading with scaled value</returns>
    public static SensorReading operator *(double factor, SensorReading reading)
    {
        return reading * factor;
    }

    /// <summary>
    /// Division operator: Divides a sensor reading by a factor.
    /// Useful for averaging or unit conversions.
    /// 
    /// Example: AveragePower = TotalPower / ReadingCount
    /// </summary>
    /// <param name="reading">The sensor reading to divide</param>
    /// <param name="divisor">The divisor</param>
    /// <returns>A new SensorReading with divided value</returns>
    /// <exception cref="ArgumentException">Thrown if divisor is zero</exception>
    public static SensorReading operator /(SensorReading reading, double divisor)
    {
        if (reading == null)
            throw new ArgumentNullException(nameof(reading));

        if (divisor == 0)
            throw new ArgumentException("Divisor cannot be zero.", nameof(divisor));

        return new SensorReading(
            reading.SensorMac,
            reading.ReadingType,
            reading.Value / divisor,
            reading.Unit,
            reading.Timestamp
        );
    }

    /// <summary>
    /// Greater than operator: Compares two sensor readings by value.
    /// Useful for threshold detection and alarm conditions.
    /// </summary>
    public static bool operator >(SensorReading left, SensorReading right)
    {
        if (left == null || right == null)
            throw new ArgumentNullException(left == null ? nameof(left) : nameof(right));

        return left.Value > right.Value;
    }

    /// <summary>
    /// Less than operator: Compares two sensor readings by value.
    /// </summary>
    public static bool operator <(SensorReading left, SensorReading right)
    {
        if (left == null || right == null)
            throw new ArgumentNullException(left == null ? nameof(left) : nameof(right));

        return left.Value < right.Value;
    }

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    public static bool operator >=(SensorReading left, SensorReading right)
    {
        if (left == null || right == null)
            throw new ArgumentNullException(left == null ? nameof(left) : nameof(right));

        return left.Value >= right.Value;
    }

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    public static bool operator <=(SensorReading left, SensorReading right)
    {
        if (left == null || right == null)
            throw new ArgumentNullException(left == null ? nameof(left) : nameof(right));

        return left.Value <= right.Value;
    }

    /// <summary>
    /// Equality operator: Compares two sensor readings by value.
    /// </summary>
    public static bool operator ==(SensorReading left, SensorReading right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Value == right.Value && left.SensorMac == right.SensorMac;
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(SensorReading left, SensorReading right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Implements IComparable for sorting sensor readings.
    /// Sorts by value in ascending order.
    /// </summary>
    public int CompareTo(SensorReading? other)
    {
        if (other == null)
            return 1;

        return Value.CompareTo(other.Value);
    }

    /// <summary>
    /// Implements IEquatable for collection operations.
    /// </summary>
    public bool Equals(SensorReading? other)
    {
        if (other is null)
            return false;

        return SensorMac == other.SensorMac &&
               ReadingType == other.ReadingType &&
               Value == other.Value &&
               Timestamp == other.Timestamp;
    }

    /// <summary>
    /// Equality override.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as SensorReading);
    }

    /// <summary>
    /// Hash code override for use in collections.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(SensorMac, ReadingType, Value, Timestamp);
    }

    /// <summary>
    /// Returns a formatted string representation of this sensor reading.
    /// </summary>
    public override string ToString()
    {
        return $"{ReadingType}: {Value:F2}{Unit} @ {Timestamp:O} ({SensorMac})";
    }

    /// <summary>
    /// Returns a detailed string representation with all fields.
    /// </summary>
    public string ToDetailedString()
    {
        return $"SensorReading {{ MAC: {SensorMac}, Type: {ReadingType}, Value: {Value:F4}, Unit: {Unit}, Timestamp: {Timestamp:O} }}";
    }

    /// <summary>
    /// Aggregates multiple sensor readings into a single result.
    /// All readings must be of the same type.
    /// </summary>
    /// <param name="readings">Collection of readings to aggregate</param>
    /// <returns>A new SensorReading with the sum of all values</returns>
    public static SensorReading Aggregate(params SensorReading[] readings)
    {
        if (readings == null || readings.Length == 0)
            throw new ArgumentException("Must provide at least one reading to aggregate.", nameof(readings));

        var first = readings[0];
        var result = new SensorReading(first.SensorMac, first.ReadingType, 0, first.Unit);

        foreach (var reading in readings)
        {
            result = result + reading;
        }

        return result;
    }

    /// <summary>
    /// Calculates the average of multiple sensor readings.
    /// All readings must be of the same type.
    /// </summary>
    /// <param name="readings">Collection of readings to average</param>
    /// <returns>A new SensorReading with the average value</returns>
    public static SensorReading Average(params SensorReading[] readings)
    {
        if (readings == null || readings.Length == 0)
            throw new ArgumentException("Must provide at least one reading to average.", nameof(readings));

        var sum = Aggregate(readings);
        return sum / readings.Length;
    }

    /// <summary>
    /// Finds the maximum reading from a collection.
    /// </summary>
    public static SensorReading Max(params SensorReading[] readings)
    {
        if (readings == null || readings.Length == 0)
            throw new ArgumentException("Must provide at least one reading.", nameof(readings));

        return readings.OrderByDescending(r => r.Value).First();
    }

    /// <summary>
    /// Finds the minimum reading from a collection.
    /// </summary>
    public static SensorReading Min(params SensorReading[] readings)
    {
        if (readings == null || readings.Length == 0)
            throw new ArgumentException("Must provide at least one reading.", nameof(readings));

        return readings.OrderBy(r => r.Value).First();
    }
}
