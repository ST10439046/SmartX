// Deployment node entity: represents a location or logical node where sensors can be deployed.
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SmartIotApi.Models;

/// <summary>
/// Represents a hierarchical tier in the IoT network deployment structure.
/// Examples: Facility (root), Zone, Sub-Zone, Location.
/// Forms a tree structure where each node can have a parent reference.
/// </summary>
public partial class DeploymentNode
{
    /// <summary>
    /// Unique identifier for the deployment node.
    /// </summary>
    public int NodeId { get; set; }

    /// <summary>
    /// Name of the deployment tier (e.g., "Facility A", "Zone 1", "Sub-Zone B").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of this deployment tier.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this tier is currently active/operational.
    /// If false, any sensors under this tier are considered unsafe to operate.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Foreign key to the parent node (for self-referential hierarchy).
    /// Null if this is a root node (e.g., Facility A).
    /// </summary>
    public int? ParentNodeId { get; set; }

    /// <summary>
    /// Timestamp when this deployment node was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the parent node in the hierarchy.
    /// </summary>
    [JsonIgnore]
    public virtual DeploymentNode? Parent { get; set; }

    /// <summary>
    /// Navigation property to child nodes in the hierarchy.
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<DeploymentNode> Children { get; set; } = new List<DeploymentNode>();

    /// <summary>
    /// Navigation property to sensors deployed at this location tier.
    /// </summary>
    [JsonIgnore]
    public virtual ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
}
