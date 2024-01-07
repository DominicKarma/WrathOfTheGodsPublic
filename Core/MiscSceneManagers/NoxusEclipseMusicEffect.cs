using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.MiscSceneManagers
{
    public class NoxusEclipseMusicEffect : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => (NoxusWorldManager.Enabled || NoxusSkySceneSystem.EclipseDarknessInterpolant >= 0.01f) && player.ZoneOverworldHeight && !Main.eclipse;

        public override SceneEffectPriority Priority => SceneEffectPriority.Environment;

        public override float GetWeight(Player player) => 0.67f;

        public override int Music
        {
            get
            {
                if (!Main.dayTime && NoxusWorldManager.Enabled)
                    return MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/NoxusWorldNight");

                return MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/NoxusEclipse");
            }
        }
    }
}
