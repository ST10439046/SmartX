// Deployment validator: checks deployment data against the application's rules before it is used.
using System;
using System.Collections.Generic;

namespace SmartIotApi.Models;

/// <summary>
/// Validates the hierarchical deployment node structure to ensure pathway integrity,
/// operational safety, and the absence of circular dependencies.
/// </summary>
public class DeploymentValidator
{
    /// <summary>
    /// Recursively validates that a deployment node and all its parent tiers are active and correctly linked.
    /// This method climbs the hierarchy from a leaf node (e.g., a specific sensor location) all the way up to 
    /// the root facility to ensure the entire path is valid and operational.
    /// </summary>
    /// <param name="node">The current node to validate (typically starting from a sensor's deployment location).</param>
    /// <param name="visitedNodes">A HashSet to track visited nodes and prevent circular dependencies. 
    /// Automatically initialized on the first call.</param>
    /// <returns>
    /// True if the deployment path is completely safe (all parents exist, are active, and no circular references exist).
    /// False if any safety check fails.
    /// </returns>
    /// <remarks>
    /// BASE CASES:
    /// 1. node == null: Reached the root (parent is null) → Path is valid, return true.
    /// 2. Circular dependency detected: Node was already visited in this path → Return false.
    /// 3. Node is inactive: Any inactive tier makes the path unsafe → Return false.
    /// 
    /// RECURSIVE STEP: Validate current node's parent.
    /// </remarks>
    public bool IsDeploymentPathSafe(DeploymentNode? node, HashSet<string>? visitedNodes = null)
    {
        // Initialize cycle detector on the first call
        visitedNodes ??= new HashSet<string>();

        // --- BASE CASE 1: Reached the absolute root (parent is null) ---
        // If we climbed all the way to the root without failing, the path is safe!
        if (node == null)
        {
            return true;
        }

        // --- BASE CASE 2: Circular Dependency / Loop Detection ---
        // If we have already visited this node name in this climb, we have an infinite loop!
        if (visitedNodes.Contains(node.Name))
        {
            // Unsafe: Circular reference detected (e.g., A -> B -> C -> A)
            return false;
        }

        // Mark this node as visited for this path validation
        visitedNodes.Add(node.Name);

        // --- BASE CASE 3: Operational Safety Check ---
        // If any tier along the path is marked as inactive, the sensor path is unsafe!
        if (!node.IsActive)
        {
            return false;
        }

        // --- RECURSIVE STEP ---
        // If the current node is valid, recursively check its Parent!
        return IsDeploymentPathSafe(node.Parent, visitedNodes);
    }

    /// <summary>
    /// Validates all sensors under a deployment node to ensure they have safe deployment paths.
    /// This method traverses the hierarchy and checks each sensor's location path.
    /// </summary>
    /// <param name="node">The root deployment node to validate.</param>
    /// <returns>True if all sensors under this node have safe deployment paths; otherwise, false.</returns>
    public bool ValidateDeploymentTreeSafety(DeploymentNode? node)
    {
        if (node == null)
        {
            return true;
        }

        // Check the current node's safety
        if (!IsDeploymentPathSafe(node))
        {
            return false;
        }

        // Recursively validate all children
        foreach (var child in node.Children)
        {
            if (!ValidateDeploymentTreeSafety(child))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the complete path from a node to the root, useful for diagnostics and logging.
    /// </summary>
    /// <param name="node">The node to trace upward.</param>
    /// <returns>A list of node names from the given node to the root.</returns>
    public List<string> GetDeploymentPath(DeploymentNode? node)
    {
        var path = new List<string>();
        var visited = new HashSet<string>();

        while (node != null)
        {
            // Prevent infinite loops in diagnostic output
            if (visited.Contains(node.Name))
            {
                path.Add($"{node.Name} [CIRCULAR REFERENCE DETECTED]");
                break;
            }

            visited.Add(node.Name);
            path.Add(node.Name);
            node = node.Parent;
        }

        path.Reverse(); // Show path from root to leaf
        return path;
    }
}
