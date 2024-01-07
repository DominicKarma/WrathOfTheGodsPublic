using NoxusBoss.Content.Projectiles.Typeless;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.MiscSceneManagers
{
    public class PurifierExplosionScene : ModSceneEffect
    {
        public override int Music
        {
            get
            {
                if (PurifierMonologueDrawer.TimeSinceMonologueBegan >= 210)
                    return MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/PurifierElevatorMusic");

                return 0;
            }
        }

        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<ThePurifierProj>()] >= 1)
                return true;
            if (!Main.gameMenu && WorldGen.generatingWorld)
                return true;

            return false;
        }
    }
}
