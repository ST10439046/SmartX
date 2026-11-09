namespace SmartIotApi.Services.Mesh;

public sealed record MeshEdge(string From, string To, double SignalStrength)
{
    public double RoutingCost => 101d - SignalStrength;
}

public sealed class MeshGraph
{
    private readonly Dictionary<string, List<MeshEdge>> _adjacency = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Vertices => _adjacency.Keys;

    public void AddVertex(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
            throw new ArgumentException("A node identifier is required.", nameof(mac));
        _adjacency.TryAdd(mac.Trim(), new List<MeshEdge>());
    }

    public void AddEdge(string from, string to, double signalStrength)
    {
        if (!double.IsFinite(signalStrength) || signalStrength is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(signalStrength), "Signal quality must be between 0 and 100.");
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A node cannot link to itself.");
        AddVertex(from);
        AddVertex(to);
        var a = from.Trim();
        var b = to.Trim();
        if (_adjacency[a].Any(edge => string.Equals(edge.To, b, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A link already exists between {a} and {b}.");
        _adjacency[a].Add(new MeshEdge(a, b, signalStrength));
        _adjacency[b].Add(new MeshEdge(b, a, signalStrength));
        _adjacency[a].Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.To, y.To));
        _adjacency[b].Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.To, y.To));
    }

    public IReadOnlyList<MeshEdge> Neighbors(string mac) =>
        _adjacency.TryGetValue(mac, out var edges) ? edges : Array.Empty<MeshEdge>();
}

public sealed record MeshPath(IReadOnlyList<string> Path, double TotalCost);
public sealed record MeshSpanningForest(IReadOnlyList<MeshEdge> Edges, IReadOnlyList<IReadOnlyList<string>> Components, double TotalCost);

public static class MeshAlgorithms
{
    public static IReadOnlyList<string> BreadthFirst(MeshGraph graph, string start)
    {
        EnsureStart(graph, start);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { start };
        var queue = new Queue<string>();
        var result = new List<string>();
        queue.Enqueue(start);
        while (queue.TryDequeue(out var current))
        {
            result.Add(current);
            foreach (var edge in graph.Neighbors(current))
                if (visited.Add(edge.To)) queue.Enqueue(edge.To);
        }
        return result;
    }

    public static IReadOnlyList<string> DepthFirst(MeshGraph graph, string start)
    {
        EnsureStart(graph, start);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        var stack = new Stack<string>();
        stack.Push(start);
        while (stack.TryPop(out var current))
        {
            if (!visited.Add(current)) continue;
            result.Add(current);
            foreach (var edge in graph.Neighbors(current).Reverse())
                if (!visited.Contains(edge.To)) stack.Push(edge.To);
        }
        return result;
    }

    public static MeshPath? ShortestPath(MeshGraph graph, string start, string destination)
    {
        EnsureStart(graph, start);
        if (!graph.Vertices.Contains(destination, StringComparer.OrdinalIgnoreCase)) return null;
        var distances = graph.Vertices.ToDictionary(x => x, _ => double.PositiveInfinity, StringComparer.OrdinalIgnoreCase);
        var previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var queue = new PriorityQueue<string, double>();
        distances[start] = 0;
        queue.Enqueue(start, 0);
        while (queue.TryDequeue(out var current, out var queuedCost))
        {
            if (queuedCost > distances[current]) continue;
            if (string.Equals(current, destination, StringComparison.OrdinalIgnoreCase)) break;
            foreach (var edge in graph.Neighbors(current))
            {
                var candidate = distances[current] + edge.RoutingCost;
                if (candidate >= distances[edge.To]) continue;
                distances[edge.To] = candidate;
                previous[edge.To] = current;
                queue.Enqueue(edge.To, candidate);
            }
        }
        if (double.IsPositiveInfinity(distances[destination])) return null;
        var path = new List<string> { destination };
        while (!string.Equals(path[^1], start, StringComparison.OrdinalIgnoreCase))
            path.Add(previous[path[^1]]);
        path.Reverse();
        return new MeshPath(path, distances[destination]);
    }

    public static MeshSpanningForest MinimumSpanningForest(MeshGraph graph)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var forest = new List<MeshEdge>();
        var components = new List<IReadOnlyList<string>>();
        foreach (var root in graph.Vertices.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (visited.Contains(root)) continue;
            var component = new List<string>();
            var queue = new PriorityQueue<MeshEdge, double>();
            visited.Add(root);
            component.Add(root);
            EnqueueFrontier(root);
            while (queue.TryDequeue(out var edge, out _))
            {
                if (!visited.Add(edge.To)) continue;
                forest.Add(new MeshEdge(edge.From, edge.To, edge.SignalStrength));
                component.Add(edge.To);
                EnqueueFrontier(edge.To);
            }
            components.Add(component);
            void EnqueueFrontier(string node)
            {
                foreach (var candidate in graph.Neighbors(node))
                    if (!visited.Contains(candidate.To))
                        queue.Enqueue(candidate, candidate.RoutingCost);
            }
        }
        return new MeshSpanningForest(forest, components, forest.Sum(edge => edge.RoutingCost));
    }

    private static void EnsureStart(MeshGraph graph, string start)
    {
        ArgumentNullException.ThrowIfNull(graph);
        if (!graph.Vertices.Contains(start, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Node '{start}' does not exist in the graph.", nameof(start));
    }
}

public sealed class TelemetryAvlTree<T>
{
    private sealed class Node(DateTime key, T value)
    {
        public DateTime Key = key;
        public List<T> Values = [value];
        public Node? Left;
        public Node? Right;
        public int Height = 1;
    }

    private Node? _root;
    public int Count { get; private set; }

    public void Add(DateTime timestamp, T value)
    {
        _root = Insert(_root, timestamp, value);
        Count++;
    }

    public IReadOnlyList<T> Range(DateTime fromInclusive, DateTime toInclusive)
    {
        if (fromInclusive > toInclusive) throw new ArgumentException("Range start must not be after range end.");
        var result = new List<T>();
        Collect(_root, fromInclusive, toInclusive, result);
        return result;
    }

    private static Node Insert(Node? node, DateTime key, T value)
    {
        if (node is null) return new Node(key, value);
        if (key < node.Key) node.Left = Insert(node.Left, key, value);
        else if (key > node.Key) node.Right = Insert(node.Right, key, value);
        else { node.Values.Add(value); return node; }
        Update(node);
        var balance = Height(node.Left) - Height(node.Right);
        if (balance > 1 && key < node.Left!.Key) return RotateRight(node);
        if (balance < -1 && key > node.Right!.Key) return RotateLeft(node);
        if (balance > 1 && key > node.Left!.Key) { node.Left = RotateLeft(node.Left); return RotateRight(node); }
        if (balance < -1 && key < node.Right!.Key) { node.Right = RotateRight(node.Right); return RotateLeft(node); }
        return node;
    }

    private static void Collect(Node? node, DateTime from, DateTime to, List<T> result)
    {
        if (node is null) return;
        if (from < node.Key) Collect(node.Left, from, to, result);
        if (node.Key >= from && node.Key <= to) result.AddRange(node.Values);
        if (to > node.Key) Collect(node.Right, from, to, result);
    }

    private static int Height(Node? node) => node?.Height ?? 0;
    private static void Update(Node node) => node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    private static Node RotateRight(Node root)
    {
        var pivot = root.Left!;
        root.Left = pivot.Right;
        pivot.Right = root;
        Update(root); Update(pivot);
        return pivot;
    }
    private static Node RotateLeft(Node root)
    {
        var pivot = root.Right!;
        root.Right = pivot.Left;
        pivot.Left = root;
        Update(root); Update(pivot);
        return pivot;
    }
}
