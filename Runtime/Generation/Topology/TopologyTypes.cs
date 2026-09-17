namespace Jolybob.ProceduralWorld
{
    public enum CellTopology : byte
    {
        Solid = 0,
        Empty = 1,
        Water = 2,
        Lava = 3,
        Chasm = 4
    }

    /// <summary>
    /// Deterministic scalar describing distance to the nearest Voronoi cell boundary.
    /// A value near zero is close to a boundary; larger values are deeper inside a cell.
    /// </summary>
    public interface IVoronoiEdgeField
    {
        float SampleEdgeDistance(int worldX, int worldY);
    }

    public interface ITopologyModifier
    {
        int Order { get; }
        void Execute(WorldGenerationContext context);
    }
}
