using SmartIotApi.Services.Mesh;

namespace SmartIotApi.Tests.Mesh;

public sealed class MeshAlgorithmsTests
{
    private static MeshGraph CreateGraph()
    {
        var graph = new MeshGraph();
        graph.AddEdge("A", "B", 90);
        graph.AddEdge("A", "C", 70);
        graph.AddEdge("B", "C", 80);
        graph.AddEdge("B", "D", 60);
        graph.AddEdge("C", "D", 95);
        graph.AddEdge("D", "E", 50);
        graph.AddVertex("ISOLATED");
        return graph;
    }

    [Fact]
    public void BreadthFirstTraversal_VisitsReachableNodesByLayers()
    {
        var result = MeshAlgorithms.BreadthFirst(CreateGraph(), "A");

        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, result);
        Assert.DoesNotContain("ISOLATED", result);
    }

    [Fact]
    public void DepthFirstTraversal_VisitsEveryReachableNodeOnce()
    {
        var result = MeshAlgorithms.DepthFirst(CreateGraph(), "A");

        Assert.Equal(5, result.Count);
        Assert.Equal(5, result.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("A", result[0]);
    }

    [Fact]
    public void Dijkstra_ChoosesLowestDerivedRoutingCost()
    {
        var result = MeshAlgorithms.ShortestPath(CreateGraph(), "A", "E");

        Assert.NotNull(result);
        Assert.Equal(new[] { "A", "C", "D", "E" }, result!.Path);
        Assert.Equal(41, result.TotalCost);
    }

    [Fact]
    public void Prim_ReturnsMinimumSpanningForestIncludingDisconnectedVertex()
    {
        var result = MeshAlgorithms.MinimumSpanningForest(CreateGraph());

        Assert.Equal(4, result.Edges.Count);
        Assert.Equal(5, result.Components.Count);
        Assert.Contains(result.Components, component => component.Contains("ISOLATED"));
    }

    [Fact]
    public void AddEdge_RejectsSignalQualityOutsideZeroToOneHundred()
    {
        var graph = new MeshGraph();

        Assert.Throws<ArgumentOutOfRangeException>(() => graph.AddEdge("A", "B", 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => graph.AddEdge("A", "B", -1));
    }

    [Fact]
    public void AvlTree_ReturnsRangeInTimestampOrderAndKeepsDuplicateTimestamps()
    {
        var tree = new TelemetryAvlTree<string>();
        var timestamp = DateTime.UtcNow;

        tree.Add(timestamp.AddMinutes(2), "later");
        tree.Add(timestamp, "first");
        tree.Add(timestamp, "same-time");
        tree.Add(timestamp.AddMinutes(1), "middle");

        var result = tree.Range(timestamp, timestamp.AddMinutes(2));

        Assert.Equal(new[] { "first", "same-time", "middle", "later" }, result);
        Assert.Equal(4, tree.Count);
    }
}
