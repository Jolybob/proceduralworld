namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Describes the deterministic placement rules shared by world-space features.
    /// Feature content remains owned by the feature-specific catalog.
    /// </summary>
    public interface IWorldFeaturePlacementDefinition
    {
        int FeatureId { get; }
        int Width { get; }
        int Height { get; }
        float SpawnChance { get; }
        int MaxPerChunk { get; }
        int MinimumDistanceFromOrigin { get; }
    }
}
