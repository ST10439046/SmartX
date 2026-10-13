// Entity Framework Core database context: defines the application's SQL Server connection model and entity-to-table mappings.
﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SmartIotApi.Models;

public partial class SmartIotContext : DbContext
{
    public SmartIotContext()
    {
    }

    public SmartIotContext(DbContextOptions<SmartIotContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActiveAlert> ActiveAlerts { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<CommandHistory> CommandHistories { get; set; }

    public virtual DbSet<DeploymentNode> DeploymentNodes { get; set; }

    public virtual DbSet<NetworkTopology> NetworkTopologies { get; set; }

    public virtual DbSet<Sensor> Sensors { get; set; }

    public virtual DbSet<TelemetryDatum> TelemetryData { get; set; }

    // Connection settings are supplied by dependency injection and configuration.
    // Do not embed workstation credentials or override the Docker connection string.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__ActiveAl__EBB16AEDA695AC07");

            entity.Property(e => e.AlertId).HasColumnName("AlertID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.ErrorCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsResolved).HasDefaultValue(false);
            entity.Property(e => e.SensorMac)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("SensorMAC");

            entity.HasOne(d => d.SensorMacNavigation).WithMany(p => p.ActiveAlerts)
                .HasForeignKey(d => d.SensorMac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ActiveAle__Senso__4BAC3F29");
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__Attachme__442C64DE0957B1A5");

            entity.Property(e => e.AttachmentId).HasColumnName("AttachmentID");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FilePath).IsUnicode(false);
            entity.Property(e => e.FileType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SensorMac)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("SensorMAC");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.SensorMacNavigation).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.SensorMac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attachmen__Senso__3F466844");
        });

        modelBuilder.Entity<CommandHistory>(entity =>
        {
            entity.HasKey(e => e.CommandId).HasName("PK__CommandH__6B4108E6AA0E7168");

            entity.ToTable("CommandHistory");

            entity.Property(e => e.CommandId).HasColumnName("CommandID");
            entity.Property(e => e.CommandType)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SensorMac)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("SensorMAC");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.SensorMacNavigation).WithMany(p => p.CommandHistories)
                .HasForeignKey(d => d.SensorMac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommandHi__Senso__4316F928");
        });

        modelBuilder.Entity<DeploymentNode>(entity =>
        {
            entity.HasKey(e => e.NodeId);

            entity.Property(e => e.NodeId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            // Self-referential relationship for parent-child hierarchy
            entity.HasOne(d => d.Parent)
                .WithMany(p => p.Children)
                .HasForeignKey(d => d.ParentNodeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK__DeploymentNode__Parent");
        });

        modelBuilder.Entity<NetworkTopology>(entity =>
        {
            entity.HasKey(e => e.LinkId).HasName("PK__NetworkT__2D1221558729ED3A");

            entity.ToTable("NetworkTopology");

            entity.Property(e => e.LinkId).HasColumnName("LinkID");
            entity.Property(e => e.FromNodeMac)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("FromNodeMAC");
            entity.Property(e => e.ToNodeMac)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("ToNodeMAC");

            entity.HasOne(d => d.FromNodeMacNavigation).WithMany(p => p.NetworkTopologyFromNodeMacNavigations)
                .HasForeignKey(d => d.FromNodeMac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NetworkTo__FromN__45F365D3");

            entity.HasOne(d => d.ToNodeMacNavigation).WithMany(p => p.NetworkTopologyToNodeMacNavigations)
                .HasForeignKey(d => d.ToNodeMac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NetworkTo__ToNod__46E78A0C");
        });

        modelBuilder.Entity<Sensor>(entity =>
        {
            entity.HasKey(e => e.Macaddress).HasName("PK__Sensors__95BFB048C85BC2DF");

            entity.Property(e => e.Macaddress)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("MACAddress");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.NodeId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("NodeID");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Room)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Zone)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            // Foreign key relationship to DeploymentNode
            entity.HasOne(d => d.DeploymentNode)
                .WithMany(p => p.Sensors)
                .HasForeignKey(d => d.DeploymentNodeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Sensor__DeploymentNode");
        });

        modelBuilder.Entity<TelemetryDatum>(entity =>
        {
            entity.HasKey(e => e.TelemetryId).HasName("PK__Telemetr__157CAF176CE91377");

            entity.Property(e => e.TelemetryId).HasColumnName("TelemetryID");
            entity.Property(e => e.DataType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SensorMac)
                .HasMaxLength(17)
                .IsUnicode(false)
                .HasColumnName("SensorMAC");
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.SensorMacNavigation).WithMany(p => p.TelemetryData)
                .HasForeignKey(d => d.SensorMac)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Telemetry__Senso__3B75D760");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
