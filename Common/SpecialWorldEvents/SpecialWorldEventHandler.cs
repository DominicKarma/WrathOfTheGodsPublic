using NoxusBoss.Common.CustomWorldSeeds;
using Terraria.ModLoader;

namespace NoxusBoss.Common.SpecialWorldEvents
{
    public abstract class SpecialWorldEventHandler : ModSystem
    {
        public abstract string WorldSeed
        {
            get;
        }

        public abstract bool EventOngoing
        {
            get;
        }

        public bool EventEnabled => CustomWorldSeedManager.IsSeedActive(WorldSeed);

        public override void PostUpdateNPCs()
        {
            if (EventEnabled)
                HandleEvent();
        }

        public override void OnWorldLoad() => ResetThings();

        public override void OnWorldUnload() => ResetThings();

        public abstract void HandleEvent();

        public abstract void ResetThings();
    }
}
