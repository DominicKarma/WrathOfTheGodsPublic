using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class NamelessDeityClockDeathZoneScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => AnyProjectiles(ModContent.ProjectileType<ClockConstellation>());

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:NamelessDeityClockDeathZoneSky", isActive);
        }
    }

    public class NamelessDeityClockDeathZoneScreenShaderData : ScreenShaderData
    {
        public static float OutlineIntensity
        {
            get;
            set;
        }

        public static Vector2 ClockCenter
        {
            get;
            set;
        }

        public NamelessDeityClockDeathZoneScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Update(GameTime gameTime)
        {
            var clocks = AllProjectilesByID(ModContent.ProjectileType<ClockConstellation>());
            if (clocks.Any())
            {
                ClockCenter = clocks.First().Center;
                OutlineIntensity = clocks.First().Opacity;
            }
            else
                OutlineIntensity = Clamp(OutlineIntensity - 0.08f, 0f, 1f);
        }

        public override void Apply()
        {
            Main.instance.GraphicsDevice.Textures[1] = FireNoise;
            Main.instance.GraphicsDevice.Textures[2] = WavyBlotchNoise;
            Shader.Parameters["clockCenter"].SetValue(WorldSpaceToScreenUV(ClockCenter));
            UseColor(DialogColorRegistry.NamelessDeityTextColor);
            UseSecondaryColor(new Color(19, 43, 159) * 0.67f);
            UseIntensity(OutlineIntensity * 0.92f);
            base.Apply();
        }
    }
}
