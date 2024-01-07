using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using Terraria;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class NamelessDeityScreenShaderData : ScreenShaderData
    {
        public NamelessDeityScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            Main.instance.GraphicsDevice.Textures[1] = DivineLightTexture;
            Shader.Parameters["seamAngle"].SetValue(NamelessDeitySky.SeamAngle);
            Shader.Parameters["seamSlope"].SetValue(NamelessDeitySky.SeamSlope);
            Shader.Parameters["seamBrightness"].SetValue(0.029f);
            Shader.Parameters["warpIntensity"].SetValue(0.016f);
            Shader.Parameters["offsetsAreAllowed"].SetValue(NamelessDeitySky.HeavenlyBackgroundIntensity <= 0.01f);
            UseOpacity(Clamp(1f - NamelessDeitySky.HeavenlyBackgroundIntensity + 0.001f, 0.001f, 1f));
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(Color.Transparent);
            UseIntensity(NamelessDeitySky.SeamScale * InverseLerp(0.5f, 0.1f, NamelessDeitySky.HeavenlyBackgroundIntensity));
            base.Apply();
        }
    }
}
