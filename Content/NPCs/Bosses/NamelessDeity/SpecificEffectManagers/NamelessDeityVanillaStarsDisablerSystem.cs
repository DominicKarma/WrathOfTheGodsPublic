using NoxusBoss.Common.Subworlds;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeityVanillaStarsDisablerSystem : ModSystem
    {
        public override void OnModLoad()
        {
            On_Main.DrawStarsInBackground += MakeStarsGoAway;
        }

        private void MakeStarsGoAway(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
        {
            if (Main.gameMenu || Main.dayTime || artificial || !EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                orig(self, sceneArea, artificial);
        }
    }
}
