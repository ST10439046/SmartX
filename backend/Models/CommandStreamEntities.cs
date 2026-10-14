// Command-stream models: define the request and response data structures used by command-stream operations.
using Microsoft.EntityFrameworkCore;

namespace SmartIotApi.Models;

/// <summary>Configurable telemetry threshold that can create and resolve an ActiveAlert.</summary>
public sealed class AlertRule
{
    public int AlertRuleId { get; set; }
    public string Name { get; set; } = null!;
    public string? SensorMac { get; set; }
    public string DataType { get; set; } = null!;
    public string Comparison { get; set; } = ">=";
    public double Threshold { get; set; }
    public string Severity { get; set; } = "warning";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

/// <summary>Last requested output state for a simulated actuator/device.</summary>
public sealed class CommandDeviceState
{
    public string SensorMac { get; set; } = null!;
    public string CurrentState { get; set; } = "UNKNOWN";
    public DateTime UpdatedAt { get; set; }
}

public partial class SmartIotContext
{
    public virtual DbSet<AlertRule> AlertRules { get; set; } = null!;
    public virtual DbSet<CommandDeviceState> CommandDeviceStates { get; set; } = null!;
    public virtual DbSet<OperatorActivity> OperatorActivities { get; set; } = null!;

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertRule>(entity =>
        {
            entity.ToTable("AlertRules");
            entity.HasKey(e => e.AlertRuleId);
            entity.Property(e => e.AlertRuleId).HasColumnName("AlertRuleID");
            entity.Property(e => e.Name).HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.SensorMac).HasMaxLength(17).IsUnicode(false).HasColumnName("SensorMAC");
            entity.Property(e => e.DataType).HasMaxLength(50).IsUnicode(false).IsRequired();
            entity.Property(e => e.Comparison).HasMaxLength(2).IsUnicode(false).IsRequired();
            entity.Property(e => e.Threshold).HasColumnType("float");
            entity.Property(e => e.Severity).HasMaxLength(20).IsUnicode(false).HasDefaultValue("warning");
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.HasOne<Sensor>().WithMany().HasForeignKey(e => e.SensorMac)
                .OnDelete(DeleteBehavior.SetNull).HasConstraintName("FK_AlertRules_Sensors");
        });

        modelBuilder.Entity<OperatorActivity>(entity =>
        {
            entity.ToTable("OperatorActivities");
            entity.HasKey(e => e.OperatorActivityId);
            entity.Property(e => e.ActivityType).HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.SensorMac).HasMaxLength(17).IsUnicode(false).HasColumnName("SensorMAC");
            entity.Property(e => e.Detail).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NormalizedDetail).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.SensorMac, e.ActivityType, e.CreatedAt });
        });

        modelBuilder.Entity<CommandDeviceState>(entity =>
        {
            entity.ToTable("CommandDeviceStates");
            entity.HasKey(e => e.SensorMac);
            entity.Property(e => e.SensorMac).HasMaxLength(17).IsUnicode(false).HasColumnName("SensorMAC");
            entity.Property(e => e.CurrentState).HasMaxLength(20).IsUnicode(false).HasDefaultValue("UNKNOWN");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasDefaultValueSql("(getdate())");
            entity.HasOne<Sensor>().WithMany().HasForeignKey(e => e.SensorMac)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("FK_CommandDeviceStates_Sensors");
        });
    }
}
