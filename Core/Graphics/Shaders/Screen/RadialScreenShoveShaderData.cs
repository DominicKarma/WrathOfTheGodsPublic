using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class RadialScreenShoveShaderData : ScreenShaderData
    {
        public RadialScreenShoveShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            float distortionPower = RadialScreenShoveSystem.DistortionPower * NoxusBossConfig.Instance.VisualOverlayIntensity * 0.11f;
            Shader.Parameters["blurPower"].SetValue(NoxusBossConfig.Instance.VisualOverlayIntensity * 0.5f);
            Shader.Parameters["pulseTimer"].SetValue(Main.GlobalTimeWrappedHourly * 16f);
            Shader.Parameters["distortionPower"].SetValue(Main.gamePaused ? 0f : distortionPower);
            Shader.Parameters["distortionCenter"].SetValue(WorldSpaceToScreenUV(RadialScreenShoveSystem.DistortionCenter));
            base.Apply();
        }
    }
}
