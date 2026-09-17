using System;

namespace Jolybob.ProceduralWorld.Tilemap
{
    [Flags]
    public enum WorldCellVisualState : byte
    {
        None = 0,
        Selected = 1 << 0,
        Hovered = 1 << 1,
        Damaged = 1 << 2,
        Interactable = 1 << 3
    }
}
