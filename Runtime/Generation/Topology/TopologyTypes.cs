namespace Jolybob.ProceduralWorld
{
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
