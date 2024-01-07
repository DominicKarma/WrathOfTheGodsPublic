using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core.Graphics.Particles;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class NoxusDecalParticle : Particle
    {
        public override string TexturePath => InvisiblePixelPath;

        public NoxusDecalParticle(Vector2 position, float rotation, Color color, int lifetime, float scale)
        {
            Position = position;
            Rotation = rotation;
            Color = color;
            Scale = scale;
            Lifetime = lifetime;
        }

        public override void Update()
        {
            Opacity = 1f - LifetimeRatio;
        }

        public override void Draw()
        {
            if (EntropicGod.Myself is null)
                return;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Color c = Color * Opacity;
            float contrastInterpolant = InverseLerp(0.6f, 0.8f, Opacity);

            var backgroundShader = ShaderManager.GetShader("MonochromeShader");
            backgroundShader.TrySetParameter("contrastInterpolant", contrastInterpolant);
            backgroundShader.Apply();

            EntropicGod.Myself.As<EntropicGod>().DrawDecal(Position - Main.screenPosition, c, Rotation);
            Main.spriteBatch.ResetToDefault();
        }
    }
}
