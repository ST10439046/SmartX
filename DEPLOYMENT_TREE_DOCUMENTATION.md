# IoT Nested Deployment Tree with Recursive Validation

## Overview

This implementation provides a hierarchical location/zone structure for your IoT network with recursive validation to ensure pathway integrity, operational safety, and the prevention of circular dependencies. All sensors are **active by default**.

## Architecture

### Hierarchical Structure
```
Facility A (Root - IsActive=true)
├── Zone 1 (Parent - IsActive=true)
│   ├── Sub-Zone B (Child - IsActive=true)
│   │   └── ESP32-Sensor Location (Leaf - IsActive=true)
│   │       └── Sensor: ENV-01 (IsActive=true)
│   └── Sensor: ACT-03 (IsActive=true)
└── Sensor: PWR-02 (IsActive=true)
```

## Key Components

### 1. **DeploymentNode Model** (`Models/DeploymentNode.cs`)
- Represents a hierarchical tier in the IoT network
- Examples: Facility, Zone, Sub-Zone, Location
- Properties:
  - `NodeId`: Unique identifier
  - `Name`: Tier name (e.g., "Facility A")
  - `IsActive`: Operational status (default: true)
  - `ParentNodeId`: Self-referential foreign key for hierarchy
  - `Parent`: Navigation to parent node
  - `Children`: Collection of child nodes
  - `Sensors`: Collection of sensors at this location

### 2. **DeploymentValidator Class** (`Models/DeploymentValidator.cs`)

#### Method: `IsDeploymentPathSafe(DeploymentNode? node, HashSet<string>? visitedNodes = null)`

This is the **core recursive validation method** with the following logic:

**BASE CASES:**
1. **`node == null`**: Reached the root → Path is valid, return `true`
2. **Circular Dependency**: Node already visited → Return `false`
3. **Inactive Node**: Any inactive tier makes path unsafe → Return `false`

**RECURSIVE STEP:** Recursively validate parent node

```csharp
public bool IsDeploymentPathSafe(DeploymentNode? node, HashSet<string>? visitedNodes = null)
{
	// Initialize cycle detector on first call
	visitedNodes ??= new HashSet<string>();

	// BASE CASE 1: Reached root
	if (node == null) return true;

	// BASE CASE 2: Circular reference detected
	if (visitedNodes.Contains(node.Name)) return false;

	visitedNodes.Add(node.Name);

	// BASE CASE 3: Operational safety
	if (!node.IsActive) return false;

	// RECURSIVE STEP: Check parent
	return IsDeploymentPathSafe(node.Parent, visitedNodes);
}
```

#### Additional Methods:
- **`ValidateDeploymentTreeSafety()`**: Validates entire subtree recursively
- **`GetDeploymentPath()`**: Returns deployment path from node to root (diagnostic utility)

### 3. **Sensor Model Updates** (`Models/Sensor.cs`)
- Added `IsActive` property (default: `true`)
- Added `DeploymentNodeId` foreign key
- Added `DeploymentNode` navigation property

All sensors are **active by default** and can be deployed within the hierarchy.

### 4. **Database Configuration** (`Models/SmartIotContext.cs`)
- Added `DeploymentNodes` DbSet
- Configured self-referential relationship with `Restrict` delete behavior (prevents accidental deletions)
- Configured Sensor-DeploymentNode relationship with `SetNull` delete behavior

## API Endpoints

### Deployment Node Management (`/api/deploymentnode`)

#### GET /api/deploymentnode
Returns all deployment nodes with hierarchy info
```json
[
  {
	"nodeId": 1,
	"name": "Facility A (Root)",
	"isActive": true,
	"parentNodeId": null,
	"childrenCount": 1,
	"sensorsCount": 1,
	"pathSafe": true
  }
]
```

#### GET /api/deploymentnode/{id}
Returns specific node with full hierarchy details
```json
{
  "nodeId": 1,
  "name": "Facility A",
  "isActive": true,
  "deploymentPath": ["Facility A"],
  "pathSafe": true,
  "children": [
	{ "nodeId": 2, "name": "Zone 1", "isActive": true }
  ],
  "associatedSensors": [...]
}
```

#### POST /api/deploymentnode
Create new deployment node
```json
{
  "name": "New Zone",
  "description": "Operational zone",
  "isActive": true,
  "parentNodeId": 1
}
```

#### PUT /api/deploymentnode/{id}
Update deployment node (validates circular references)

#### DELETE /api/deploymentnode/{id}
Delete node (only if no children or sensors)

#### POST /api/deploymentnode/{id}/validate
Validate entire deployment tree
```json
{
  "treeValidationPassed": true,
  "pathToRootValid": true,
  "deploymentPath": ["Facility A", "Zone 1", "Sub-Zone B"],
  "message": "Entire deployment tree is safe..."
}
```

#### GET /api/deploymentnode/{id}/sensors
Get all sensors at a specific deployment node

### Sensor Management (`/api/sensor`)

#### GET /api/sensor
Returns all sensors with deployment validation status
```json
[
  {
	"macaddress": "00:1A:2B:3C:4D:5E",
	"isActive": true,
	"deploymentNodeId": 4,
	"deploymentPathSafe": true
  }
]
```

#### GET /api/sensor/{macaddress}
Returns sensor with deployment path details
```json
{
  "macaddress": "00:1A:2B:3C:4D:5E",
  "isActive": true,
  "deploymentNodeId": 4,
  "deploymentPathSafe": true,
  "deploymentPath": ["Facility A", "Zone 1", "Sub-Zone B", "ESP32-Sensor Location"]
}
```

#### POST /api/sensor
Create new sensor (validates deployment path safety)

#### PUT /api/sensor/{macaddress}
Update sensor (validates deployment path safety)

## Sample Deployment Hierarchy

The seeded data creates:
```
Facility A (NodeId=1, IsActive=true)
├── Zone 1 (NodeId=2, IsActive=true)
│   ├── Sub-Zone B (NodeId=3, IsActive=true)
│   │   └── ESP32-Sensor Location (NodeId=4, IsActive=true)
│   │       └── Sensor 00:1A:2B:3C:4D:5E (ENV-01, IsActive=true)
│   └── Sensor 00:1A:2B:3C:4D:60 (ACT-03, IsActive=true)
└── Sensor 00:1A:2B:3C:4D:5F (PWR-02, IsActive=true)
```

All sensors are **active by default**.

## Safety Features

### 1. **Circular Reference Prevention**
- Uses `HashSet<string>` to track visited nodes during recursion
- Prevents stack overflow exceptions
- Returns `false` if cycle is detected

### 2. **Operational Safety**
- Any inactive parent tier invalidates the entire path
- POST/PUT operations validate deployment path before saving
- Sensors cannot be placed in unsafe deployment paths

### 3. **Data Integrity**
- Self-referential relationships use `Restrict` delete behavior
- Cannot delete nodes with children (promotes safe hierarchy management)
- Cannot delete nodes with associated sensors

### 4. **Recursive Validation Performance**
- Linear time complexity: O(n) where n = depth of hierarchy
- Typical IoT deployments: depth ≤ 5-10 levels → ~microseconds
- HashSet lookups: O(1) average case
- Suitable for real-time API operations

## Examples

### Example 1: Validate Sensor Path
```csharp
var validator = new DeploymentValidator();
var sensor = await context.Sensors
	.Include(s => s.DeploymentNode)
	.FirstAsync(s => s.Macaddress == "00:1A:2B:3C:4D:5E");

bool isSafe = validator.IsDeploymentPathSafe(sensor.DeploymentNode);
// Returns: true (all parents are active)
```

### Example 2: Get Deployment Path
```csharp
var path = validator.GetDeploymentPath(deploymentNode);
// Returns: ["Facility A", "Zone 1", "Sub-Zone B", "ESP32-Sensor Location"]
```

### Example 3: Prevent Circular Reference
```csharp
// Attempt to set Zone 1's parent to Sub-Zone B
zone1.ParentNodeId = 3; // Sub-Zone B's NodeId

// Validation fails when updating:
// PUT /api/deploymentnode/2
// Response: "Setting this parent would create a circular reference."
```

### Example 4: Deactivate Zone (Safety Check)
```csharp
// Deactivate Zone 1
zone1.IsActive = false;

// Now any sensor in Sub-Zone B under Zone 1 is unsafe:
validator.IsDeploymentPathSafe(subZoneB); // Returns: false
```

## Database Schema

### DeploymentNode Table
```sql
CREATE TABLE DeploymentNodes (
	NodeId INT PRIMARY KEY IDENTITY(1,1),
	Name NVARCHAR(100) NOT NULL,
	Description NVARCHAR(500),
	IsActive BIT DEFAULT 1,
	ParentNodeId INT,
	CreatedAt DATETIME DEFAULT GETDATE(),
	FOREIGN KEY (ParentNodeId) REFERENCES DeploymentNodes(NodeId) ON DELETE RESTRICT
);
```

### Sensor Table (Updated)
```sql
ALTER TABLE Sensors ADD 
	IsActive BIT DEFAULT 1,
	DeploymentNodeId INT,
	FOREIGN KEY (DeploymentNodeId) REFERENCES DeploymentNodes(NodeId) ON DELETE SET NULL;
```

## Performance Considerations

- **Recursion Depth**: Typically limited by physical hierarchy (5-20 levels max)
- **Call Stack**: Safe stack usage (~1KB per recursive call, systems have ~1MB+ stack)
- **Memory**: HashSet uses O(n) memory where n = path length (minimal)
- **Time**: O(depth) per validation, fast enough for API operations

## Migration from Existing Schema

If you already have sensors without deployment nodes:

1. Create default DeploymentNodes for existing zones
2. Backfill `DeploymentNodeId` in existing Sensors
3. All existing sensors remain active by default

```csharp
// Example migration
var legacyZones = await context.Sensors
	.Select(s => s.Zone)
	.Distinct()
	.ToListAsync();

foreach (var zone in legacyZones)
{
	var node = new DeploymentNode { Name = zone, IsActive = true };
	context.DeploymentNodes.Add(node);
	await context.SaveChangesAsync();

	var sensors = context.Sensors.Where(s => s.Zone == zone);
	foreach (var sensor in sensors)
	{
		sensor.DeploymentNodeId = node.NodeId;
		sensor.IsActive = true;
	}
	await context.SaveChangesAsync();
}
```

## Testing Recursive Validation

```csharp
[Test]
public void TestCircularReferenceDetection()
{
	var validator = new DeploymentValidator();
	var nodeA = new DeploymentNode { Name = "A" };
	var nodeB = new DeploymentNode { Name = "B", Parent = nodeA };
	nodeA.Parent = nodeB; // Creates cycle: A -> B -> A

	// Should return false due to circular dependency
	Assert.IsFalse(validator.IsDeploymentPathSafe(nodeA));
}

[Test]
public void TestInactiveNodeDetection()
{
	var validator = new DeploymentValidator();
	var facility = new DeploymentNode { Name = "Facility", IsActive = true };
	var zone = new DeploymentNode { Name = "Zone", Parent = facility, IsActive = false };

	// Should return false because zone is inactive
	Assert.IsFalse(validator.IsDeploymentPathSafe(zone));
}

[Test]
public void TestValidPathToRoot()
{
	var validator = new DeploymentValidator();
	var facility = new DeploymentNode { Name = "Facility", IsActive = true };
	var zone = new DeploymentNode { Name = "Zone", Parent = facility, IsActive = true };
	var subzone = new DeploymentNode { Name = "SubZone", Parent = zone, IsActive = true };

	// Should return true - valid path all active
	Assert.IsTrue(validator.IsDeploymentPathSafe(subzone));
}
```

## Security Considerations

1. **Input Validation**: All API endpoints validate parent node existence
2. **Circular Reference Prevention**: Recursive validation prevents infinite loops
3. **Operational Safety**: Inactive nodes automatically invalidate entire subtrees
4. **Data Consistency**: Foreign key constraints ensure referential integrity
5. **Authorization**: Consider adding role-based access control for deployment management

---

**Status**: ✅ All sensors are active by default. Recursive validation ensures hierarchical integrity.
