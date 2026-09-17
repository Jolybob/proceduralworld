using System.Collections.Generic;

namespace Jolybob.ProceduralWorld.Tilemap
{
    public sealed class WorldCellVisualStateStore : IWorldCellVisualStateProvider
    {
        private readonly Dictionary<WorldPosition, WorldCellVisualState> states =
            new Dictionary<WorldPosition, WorldCellVisualState>();

        public WorldCellVisualState GetState(WorldPosition position, GeneratedCell cell)
        {
            WorldCellVisualState state;
            return states.TryGetValue(position, out state) ? state : WorldCellVisualState.None;
        }

        public void SetState(WorldPosition position, WorldCellVisualState state)
        {
            if (state == WorldCellVisualState.None)
                states.Remove(position);
            else
                states[position] = state;
        }

        public bool ClearState(WorldPosition position)
        {
            return states.Remove(position);
        }

        public void Clear()
        {
            states.Clear();
        }
    }
}
