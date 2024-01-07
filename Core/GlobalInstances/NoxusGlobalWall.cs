using System;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalWall : GlobalWall
    {
        public delegate bool WallConditionDelegate(int x, int y, int type);

        public static event WallConditionDelegate IsWallUnbreakableEvent;

        public override void Unload()
        {
            // Reset all events on mod unload.
            IsWallUnbreakableEvent = null;
        }

        public static bool IsWallUnbreakable(int x, int y)
        {
            // Use default behavior if the event has no subscribers.
            if (IsWallUnbreakableEvent is null)
                return false;

            int wallID = ParanoidTileRetrieval(x, y).WallType;
            bool result = false;
            foreach (Delegate d in IsWallUnbreakableEvent.GetInvocationList())
                result |= ((WallConditionDelegate)d).Invoke(x, y, wallID);

            return result;
        }

        public override bool CanExplode(int i, int j, int type)
        {
            if (IsWallUnbreakable(i, j))
                return false;

            return true;
        }
    }
}
