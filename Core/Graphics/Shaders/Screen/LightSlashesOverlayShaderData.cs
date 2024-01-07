using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class LightSlashesOverlayShaderData : ScreenShaderData
    {
        public const string ShaderKey = "NoxusBoss:LightSlashesOverlay";

        public static void ToggleActivityIfNecessary()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            bool shouldBeActive = AnyProjectiles(ModContent.ProjectileType<LightSlash>()) || NoxusDeathCutsceneSystem.SlashTimer >= 1;
            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.DarknessWithLightSlashes)
                shouldBeActive = true;

            if (shouldBeActive && !Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Activate(ShaderKey);
            if (!shouldBeActive && Filters.Scene[ShaderKey].IsActive())
                Filters.Scene.Deactivate(ShaderKey);
        }

        public LightSlashesOverlayShaderData(string passName)
            : base(passName)
        {
        }

        public LightSlashesOverlayShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            float vignetteInterpolant = 0f;
            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.DarknessWithLightSlashes)
                vignetteInterpolant = NamelessDeityBoss.Myself.ai[2];

            Main.instance.GraphicsDevice.Textures[1] = LightSlashDrawer.SlashTarget;
            Main.instance.GraphicsDevice.Textures[2] = NoxusDeathCutsceneSystem.AnimationTimer >= 1 ? WhitePixel : DivineLightTexture;
            Main.instance.GraphicsDevice.Textures[3] = CrackedNoise;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.AnisotropicClamp;
            Main.instance.GraphicsDevice.SamplerStates[2] = SamplerState.AnisotropicWrap;
            Main.instance.GraphicsDevice.SamplerStates[3] = SamplerState.AnisotropicWrap;

            Shader.Parameters["splitBrightnessFactor"].SetValue(3.2f);
            Shader.Parameters["splitTextureZoomFactor"].SetValue(0.75f);
            Shader.Parameters["backgroundOffset"].SetValue((Main.screenPosition - Main.screenLastPosition) / Main.ScreenSize.ToVector2());
            Shader.Parameters["vignetteInterpolant"].SetValue(vignetteInterpolant);
            base.Apply();
        }
    }
}
