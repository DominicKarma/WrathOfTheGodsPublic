using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class MainMenuScreenShakeShaderData : ScreenShaderData
    {
        public const string ShaderKey = "NoxusBoss:MainMenuShake";

        public static float ScreenShakeIntensity
        {
            get;
            set;
        }

        public MainMenuScreenShakeShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public static void ToggleActivityIfNecessary()
        {
            bool shouldBeActive = ScreenShakeIntensity >= 0.01f;
            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Activate(ShaderKey);
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Deactivate(ShaderKey);
        }

        public override void Apply()
        {
            Vector2 shakeDirecion = (Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.4f).ToRotationVector2();
            Shader.Parameters["shakeOffset"].SetValue(shakeDirecion * Sin(Main.GlobalTimeWrappedHourly * 50f) * ScreenShakeIntensity);
            Shader.Parameters["uScreenResolution"].SetValue(Main.ScreenSize.ToVector2());
            ScreenShakeIntensity = Clamp(ScreenShakeIntensity * 0.95f - 0.044f, 0f, 50f);

            base.Apply();
        }
    }
}
