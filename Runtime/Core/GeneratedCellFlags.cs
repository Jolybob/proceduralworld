using System;

namespace Jolybob.ProceduralWorld
{
    [Flags]
    public enum GeneratedCellFlags : byte
    {
        None = 0,
        Carved = 1 << 0,
        Reserved = 1 << 1
    }
}
