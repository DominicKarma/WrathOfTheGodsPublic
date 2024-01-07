using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Projectiles;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class TerminusShaderScene : ModSceneEffect
    {
        public static Projectile Terminus
        {
            get;
            set;
        }

        public override bool IsSceneEffectActive(Player player) => Terminus is not null;

        public override int Music => 0;

        public override SceneEffectPriority Priority => (SceneEffectPriority)8;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:Terminus", isActive);
            Terminus = null;
        }
    }

    public class TerminusScreenShaderData : ScreenShaderData
    {
        public static float DarknessIntensity
        {
            get;
            set;
        }

        public static Vector2 TerminusPosition
        {
            get;
            set;
        }

        public TerminusScreenShaderData(string passName)
            : base(passName)
        {
        }

        public TerminusScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Update(GameTime gameTime)
        {
            if (TerminusShaderScene.Terminus is not null && TerminusShaderScene.Terminus.type != ModContent.ProjectileType<TerminusProj>())
                TerminusShaderScene.Terminus = null;

            if (TerminusShaderScene.Terminus is not null)
            {
                DarknessIntensity = TerminusShaderScene.Terminus.As<TerminusProj>().DarknessIntensity;
                TerminusPosition = TerminusShaderScene.Terminus.Center;
            }
        }

        public override void Apply()
        {
            UseTargetPosition(WorldSpaceToScreenUV(TerminusPosition));
            UseIntensity(DarknessIntensity * 0.3f);
            UseColor(Color.Transparent);
            Main.instance.GraphicsDevice.Textures[1] = DendriticNoise;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
            Shader.Parameters["darknessInterpolantStart"]?.SetValue(0.2f);
            Shader.Parameters["darknessInterpolantEnd"]?.SetValue(0.23f);
            Shader.Parameters["darknessInterpolantNoiseFactor"]?.SetValue(0.0331f);
            Shader.Parameters["noiseZoom"]?.SetValue(Vector2.One * 3f);
            Shader.Parameters["noiseScrollOffset"]?.SetValue(Vector2.UnitY * Main.GlobalTimeWrappedHourly * 0.3f);

            base.Apply();
        }
    }
}
