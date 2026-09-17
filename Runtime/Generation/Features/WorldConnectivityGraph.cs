using System;
using System.Collections.Generic;

namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Immutable node in a world-space connectivity graph.
    /// </summary>
    public readonly struct WorldConnectivityNode : IEquatable<WorldConnectivityNode>
    {
        public int Id { get; }
        public WorldFeaturePlacement Placement { get; }
        public WorldPosition Position => Placement.Anchor;

        public WorldConnectivityNode(int id, WorldFeaturePlacement placement)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            Id = id;
            Placement = placement;
        }

        public bool Equals(WorldConnectivityNode other)
        {
            return Id == other.Id && Placement == other.Placement;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldConnectivityNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Placement);
        }

        public static bool operator ==(WorldConnectivityNode left, WorldConnectivityNode right) => left.Equals(right);
        public static bool operator !=(WorldConnectivityNode left, WorldConnectivityNode right) => !left.Equals(right);
    }

    /// <summary>
    /// Deterministic undirected graph edge. Endpoint order is canonicalized by node ID.
    /// </summary>
    public readonly struct WorldConnectivityEdge : IEquatable<WorldConnectivityEdge>
    {
        public int A { get; }
        public int B { get; }
        public long SquaredDistance { get; }

        public WorldConnectivityEdge(int a, int b, long squaredDistance)
        {
            if (a < 0)
                throw new ArgumentOutOfRangeException(nameof(a));
            if (b < 0)
                throw new ArgumentOutOfRangeException(nameof(b));
            if (a == b)
                throw new ArgumentException("A connectivity edge cannot connect a node to itself.");
            if (squaredDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(squaredDistance));

            if (a < b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }

            SquaredDistance = squaredDistance;
        }

        public bool Equals(WorldConnectivityEdge other)
        {
            return A == other.A && B == other.B && SquaredDistance == other.SquaredDistance;
        }

        public override bool Equals(object obj)
        {
            return obj is WorldConnectivityEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(A, B, SquaredDistance);
        }

        public static bool operator ==(WorldConnectivityEdge left, WorldConnectivityEdge right) => left.Equals(right);
        public static bool operator !=(WorldConnectivityEdge left, WorldConnectivityEdge right) => !left.Equals(right);
    }

    /// <summary>
    /// Mutable-in-session world connectivity graph. Nodes reference world-space feature placements;
    /// graph data has no dependency on chunk residency or rendering.
    /// </summary>
    public sealed class WorldConnectivityGraph
    {
        private readonly List<WorldConnectivityNode> nodes = new List<WorldConnectivityNode>();
        private readonly List<WorldConnectivityEdge> edges = new List<WorldConnectivityEdge>();
        private readonly HashSet<WorldConnectivityEdge> edgeSet = new HashSet<WorldConnectivityEdge>();

        public IReadOnlyList<WorldConnectivityNode> Nodes => nodes;
        public IReadOnlyList<WorldConnectivityEdge> Edges => edges;

        public void Clear()
        {
            nodes.Clear();
            edges.Clear();
            edgeSet.Clear();
        }

        public int AddNode(WorldFeaturePlacement placement)
        {
            int id = nodes.Count;
            nodes.Add(new WorldConnectivityNode(id, placement));
            return id;
        }

        public bool AddEdge(int a, int b, long squaredDistance)
        {
            if ((uint)a >= nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(a));
            if ((uint)b >= nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(b));

            var edge = new WorldConnectivityEdge(a, b, squaredDistance);
            if (!edgeSet.Add(edge))
                return false;

            edges.Add(edge);
            return true;
        }

        public IReadOnlyList<int> GetNeighbors(int nodeId)
        {
            if ((uint)nodeId >= nodes.Count)
                throw new ArgumentOutOfRangeException(nameof(nodeId));

            var result = new List<int>();
            for (int i = 0; i < edges.Count; i++)
            {
                WorldConnectivityEdge edge = edges[i];
                if (edge.A == nodeId)
                    result.Add(edge.B);
                else if (edge.B == nodeId)
                    result.Add(edge.A);
            }

            result.Sort();
            return result;
        }

        public int GetConnectedComponentCount()
        {
            if (nodes.Count == 0)
                return 0;

            var visited = new bool[nodes.Count];
            int components = 0;
            var queue = new Queue<int>();

            for (int start = 0; start < nodes.Count; start++)
            {
                if (visited[start])
                    continue;

                components++;
                visited[start] = true;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    int node = queue.Dequeue();
                    IReadOnlyList<int> neighbors = GetNeighbors(node);
                    for (int i = 0; i < neighbors.Count; i++)
                    {
                        int neighbor = neighbors[i];
                        if (visited[neighbor])
                            continue;

                        visited[neighbor] = true;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return components;
        }
    }

    /// <summary>
    /// Deterministic configuration for world feature connectivity.
    /// </summary>
    public readonly struct WorldConnectivitySettings
    {
        public int MaximumConnectionDistance { get; }
        public int MaximumConnectionsPerNode { get; }

        public WorldConnectivitySettings(int maximumConnectionDistance, int maximumConnectionsPerNode)
        {
            if (maximumConnectionDistance <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumConnectionDistance));
            if (maximumConnectionsPerNode <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumConnectionsPerNode));

            MaximumConnectionDistance = maximumConnectionDistance;
            MaximumConnectionsPerNode = maximumConnectionsPerNode;
        }
    }

    /// <summary>
    /// Builds a deterministic sparse graph from world-space feature placements.
    /// Candidate discovery uses a uniform world-space grid to avoid scanning every pair
    /// when placements are geographically distributed.
    /// </summary>
    public sealed class WorldConnectivityGraphBuilder
    {
        private readonly struct SpatialBucket : IEquatable<SpatialBucket>
        {
            public readonly int X;
            public readonly int Y;

            public SpatialBucket(int x, int y)
            {
                X = x;
                Y = y;
            }

            public bool Equals(SpatialBucket other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is SpatialBucket other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Y);
        }

        private readonly struct CandidateEdge
        {
            public readonly int A;
            public readonly int B;
            public readonly long Distance;

            public CandidateEdge(int a, int b, long distance)
            {
                A = a;
                B = b;
                Distance = distance;
            }
        }

        private sealed class DisjointSet
        {
            private readonly int[] parents;
            private readonly byte[] ranks;

            public DisjointSet(int count)
            {
                parents = new int[count];
                ranks = new byte[count];
                for (int i = 0; i < count; i++)
                    parents[i] = i;
            }

            public bool Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB)
                    return false;

                if (ranks[rootA] < ranks[rootB])
                {
                    parents[rootA] = rootB;
                }
                else if (ranks[rootA] > ranks[rootB])
                {
                    parents[rootB] = rootA;
                }
                else
                {
                    parents[rootB] = rootA;
                    ranks[rootA]++;
                }

                return true;
            }

            private int Find(int value)
            {
                int root = value;
                while (parents[root] != root)
                    root = parents[root];

                while (parents[value] != value)
                {
                    int parent = parents[value];
                    parents[value] = root;
                    value = parent;
                }

                return root;
            }
        }

        public WorldConnectivityGraph Build(
            IReadOnlyList<WorldFeaturePlacement> placements,
            WorldConnectivitySettings settings)
        {
            if (placements == null)
                throw new ArgumentNullException(nameof(placements));

            var ordered = new List<WorldFeaturePlacement>(placements);
            ordered.Sort(ComparePlacements);

            var graph = new WorldConnectivityGraph();
            for (int i = 0; i < ordered.Count; i++)
                graph.AddNode(ordered[i]);

            if (ordered.Count < 2)
                return graph;

            List<CandidateEdge> candidates = CollectCandidates(ordered, settings.MaximumConnectionDistance);
            candidates.Sort(CompareCandidates);

            int[] degree = new int[ordered.Count];
            var disjointSet = new DisjointSet(ordered.Count);

            // First pass: build a deterministic minimum spanning forest where the degree cap allows it.
            // This gives downstream systems a useful connected backbone without introducing chunk-local rules.
            for (int i = 0; i < candidates.Count; i++)
            {
                CandidateEdge candidate = candidates[i];
                if (degree[candidate.A] >= settings.MaximumConnectionsPerNode
                    || degree[candidate.B] >= settings.MaximumConnectionsPerNode)
                    continue;
                if (!disjointSet.Union(candidate.A, candidate.B))
                    continue;

                graph.AddEdge(candidate.A, candidate.B, candidate.Distance);
                degree[candidate.A]++;
                degree[candidate.B]++;
            }

            // Second pass: add short redundant links while the per-node degree budget remains.
            for (int i = 0; i < candidates.Count; i++)
            {
                CandidateEdge candidate = candidates[i];
                if (degree[candidate.A] >= settings.MaximumConnectionsPerNode
                    || degree[candidate.B] >= settings.MaximumConnectionsPerNode)
                    continue;

                if (graph.AddEdge(candidate.A, candidate.B, candidate.Distance))
                {
                    degree[candidate.A]++;
                    degree[candidate.B]++;
                }
            }

            return graph;
        }

        /// <summary>
        /// Builds a graph from every feature placement intersecting an inclusive world rectangle.
        /// </summary>
        public WorldConnectivityGraph Build(
            WorldFeaturePlacementIndex index,
            WorldPosition minInclusive,
            WorldPosition maxInclusive,
            WorldConnectivitySettings settings)
        {
            if (index == null)
                throw new ArgumentNullException(nameof(index));

            var placements = new WorldFeaturePlacementSet();
            index.CollectIntersecting(minInclusive, maxInclusive, placements);
            return Build(placements.Placements, settings);
        }

        private static List<CandidateEdge> CollectCandidates(
            IReadOnlyList<WorldFeaturePlacement> placements,
            int maximumDistance)
        {
            var buckets = new Dictionary<SpatialBucket, List<int>>();
            var candidates = new List<CandidateEdge>();
            long maxDistanceSquared = (long)maximumDistance * maximumDistance;

            for (int i = 0; i < placements.Count; i++)
            {
                SpatialBucket bucket = ToBucket(placements[i].Anchor, maximumDistance);
                if (!buckets.TryGetValue(bucket, out List<int> bucketMembers))
                {
                    bucketMembers = new List<int>();
                    buckets.Add(bucket, bucketMembers);
                }

                bucketMembers.Add(i);

                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        var neighborBucket = new SpatialBucket(bucket.X + offsetX, bucket.Y + offsetY);
                        if (!buckets.TryGetValue(neighborBucket, out List<int> neighborMembers))
                            continue;

                        for (int m = 0; m < neighborMembers.Count; m++)
                        {
                            int other = neighborMembers[m];
                            if (other >= i)
                                continue;

                            long distance = SquaredDistance(
                                placements[i].Anchor,
                                placements[other].Anchor);
                            if (distance <= maxDistanceSquared)
                                candidates.Add(new CandidateEdge(other, i, distance));
                        }
                    }
                }
            }

            return candidates;
        }

        private static SpatialBucket ToBucket(WorldPosition position, int bucketSize)
        {
            return new SpatialBucket(
                FloorDiv(position.X, bucketSize),
                FloorDiv(position.Y, bucketSize));
        }

        private static int FloorDiv(int value, int divisor)
        {
            long quotient = value / divisor;
            long remainder = value % divisor;
            if (remainder != 0L && value < 0)
                quotient--;

            if (quotient < int.MinValue || quotient > int.MaxValue)
                throw new OverflowException("World-to-connectivity bucket conversion exceeds the supported range.");

            return (int)quotient;
        }

        private static long SquaredDistance(WorldPosition left, WorldPosition right)
        {
            long dx = (long)left.X - right.X;
            long dy = (long)left.Y - right.Y;
            return checked(dx * dx + dy * dy);
        }

        private static int ComparePlacements(WorldFeaturePlacement left, WorldFeaturePlacement right)
        {
            int compare = left.Anchor.X.CompareTo(right.Anchor.X);
            if (compare != 0)
                return compare;
            compare = left.Anchor.Y.CompareTo(right.Anchor.Y);
            if (compare != 0)
                return compare;
            compare = left.FeatureId.CompareTo(right.FeatureId);
            if (compare != 0)
                return compare;
            compare = left.Width.CompareTo(right.Width);
            if (compare != 0)
                return compare;
            compare = left.Height.CompareTo(right.Height);
            if (compare != 0)
                return compare;
            compare = left.OwnerChunk.X.CompareTo(right.OwnerChunk.X);
            if (compare != 0)
                return compare;
            return left.OwnerChunk.Y.CompareTo(right.OwnerChunk.Y);
        }

        private static int CompareCandidates(CandidateEdge left, CandidateEdge right)
        {
            int compare = left.Distance.CompareTo(right.Distance);
            if (compare != 0)
                return compare;
            compare = left.A.CompareTo(right.A);
            if (compare != 0)
                return compare;
            return left.B.CompareTo(right.B);
        }
    }
}
