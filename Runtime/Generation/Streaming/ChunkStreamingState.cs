namespace Jolybob.ProceduralWorld
{
    /// <summary>
    /// Runtime lifecycle state for a scheduled chunk coordinate.
    /// </summary>
    public enum ChunkStreamingState
    {
        Inactive = 0,
        Pending = 1,
        Loaded = 2
    }
}
