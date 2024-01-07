using Microsoft.Xna.Framework;
using NoxusBoss.Common.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Biomes
{
    public class EternalGardenBiome : ModBiome
    {
        public const string SkyKey = "NoxusBoss:EternalGarden";

        public override ModWaterStyle WaterStyle => ModContent.Find<ModWaterStyle>("NoxusBoss/EternalGardenWater");

        public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.Find<ModSurfaceBackgroundStyle>("NoxusBoss/EternalGardenSurfaceBGStyle");

        public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.Find<ModUndergroundBackgroundStyle>("NoxusBoss/EternalGardenBGStyle");

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override Color? BackgroundColor => Color.White;

        public override string BestiaryIcon => "NoxusBoss/Common/Biomes/EternalGardenBiome_Icon";

        public override string BackgroundPath => "NoxusBoss/Content/Backgrounds/EternalGardenBG";

        public override string MapBackground => "NoxusBoss/Content/Backgrounds/EternalGardenBG";

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/EternalGarden");

        public override bool IsBiomeActive(Player player) => SubworldSystem.IsActive<EternalGarden>();

        public override float GetWeight(Player player) => 0.96f;

        public override void Load()
        {
            SkyManager.Instance[SkyKey] = new EternalGardenSky();
        }

        public override void SetStaticDefaults()
        {
            EternalGardenSky.LoadTextures();
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            if (SkyManager.Instance[SkyKey] is not null && isActive != SkyManager.Instance[SkyKey].IsActive())
            {
                if (isActive)
                    SkyManager.Instance.Activate(SkyKey);
                else
                    SkyManager.Instance.Deactivate(SkyKey);
            }
        }
    }
}
