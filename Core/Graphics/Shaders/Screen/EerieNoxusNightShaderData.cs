using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.CustomWorldSeeds;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class EerieNoxusNightShaderData : ScreenShaderData
    {
        public const string ShaderKey = "NoxusBoss:NoxusWorldNight";

        public EerieNoxusNightShaderData(string passName)
            : base(passName)
        {
        }

        public EerieNoxusNightShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            bool shouldBeActive = NoxusWorldManager.Enabled && !Main.dayTime && Main.LocalPlayer.Center.Y < Main.worldSurface * 16f && !Main.LocalPlayer.ZoneSkyHeight;
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Activate(ShaderKey);
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Deactivate(ShaderKey);
        }

        public override void Apply()
        {
            float playerVerticalPosition = Main.LocalPlayer.Center.Y / 16f;
            float verticalPositionInterpolant = InverseLerpBump(1f, 0.9f, 0.42f, 0.35f, playerVerticalPosition / (float)Main.worldSurface);
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(Color.Transparent);
            UseIntensity(InverseLerpBump(0f, 1200f, (float)Main.nightLength - 1200f, (float)Main.nightLength, (float)Main.time) * verticalPositionInterpolant);

            Main.instance.GraphicsDevice.Textures[1] = SharpNoise;
            Main.instance.GraphicsDevice.Textures[2] = DendriticNoiseZoomedOut;
            Main.instance.GraphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
            Shader.Parameters["maxShineBrightnessFactor"].SetValue(3f);
            Shader.Parameters["zoom"].SetValue(Main.ForcedMinimumZoom / Main.GameViewMatrix.Zoom.X);
            Shader.Parameters["unscaledScreenArea"].SetValue(Main.ScreenSize.ToVector2() / Main.ForcedMinimumZoom);
            Shader.Parameters["sparkleColor1"].SetValue(Color.SkyBlue.ToVector3());
            Shader.Parameters["sparkleColor2"].SetValue(Color.Wheat.ToVector3());
            Shader.Parameters["glowColor"].SetValue(Color.MediumPurple.ToVector3() * 0.7f);

            base.Apply();
        }
    }
}
