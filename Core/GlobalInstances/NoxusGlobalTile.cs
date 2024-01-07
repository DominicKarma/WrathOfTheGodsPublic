using System;
using Terraria.ModLoader;

namespace NoxusBoss.Core.GlobalItems
{
    public class NoxusGlobalTile : GlobalTile
    {
        public delegate void NearbyEffectsDelegate(int x, int y, int type, bool closer);

        public static event NearbyEffectsDelegate NearbyEffectsEvent;

        public delegate bool TileConditionDelegate(int x, int y, int type);

        public static event TileConditionDelegate IsTileUnbreakableEvent;

        public override void Unload()
        {
            // Reset all events on mod unload.
            NearbyEffectsEvent = null;
            IsTileUnbreakableEvent = null;
        }

        public override void NearbyEffects(int i, int j, int type, bool closer)
        {
            NearbyEffectsEvent?.Invoke(i, j, type, closer);
        }

        public static bool IsTileUnbreakable(int x, int y)
        {
            // Use default behavior if the event has no subscribers.
            if (IsTileUnbreakableEvent is null)
                return false;

            int tileID = ParanoidTileRetrieval(x, y).TileType;
            bool result = false;
            foreach (Delegate d in IsTileUnbreakableEvent.GetInvocationList())
                result |= ((TileConditionDelegate)d).Invoke(x, y, tileID);

            return result;
        }

        public override bool CanExplode(int i, int j, int type)
        {
            if (IsTileUnbreakable(i, j))
                return false;

            return true;
        }

        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (IsTileUnbreakable(i, j))
                return false;

            return true;
        }
    }
}
