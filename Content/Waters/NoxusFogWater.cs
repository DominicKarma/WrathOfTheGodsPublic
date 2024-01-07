using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Waters
{
    public class NoxusFogWater : ModWaterStyle
    {
        public override int ChooseWaterfallStyle()
        {
            return ModContent.Find<ModWaterfallStyle>($"{Mod.Name}/NoxusFogWaterflow").Slot;
        }

        public override int GetSplashDust()
        {
            return 33;
        }

        public override int GetDropletGore()
        {
            return 713;
        }

        public override Color BiomeHairColor()
        {
            return Color.Lerp(Color.MediumPurple, Color.Black, 0.75f);
        }

        public override Asset<Texture2D> GetRainTexture()
        {
            return ModContent.Request<Texture2D>("NoxusBoss/Content/Waters/NoxusFogRain");
        }
    }
}
