using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Core.Graphics.Particles;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;

namespace NoxusBoss.Content.Particles
{
    public class NoxusEggDecalParticle : Particle
    {
        public override string TexturePath => InvisiblePixelPath;

        public NoxusEggDecalParticle(Vector2 position, float rotation, Color color, int lifetime, float scale)
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Color c = Color * Opacity;
            float contrastInterpolant = InverseLerp(0.6f, 0.8f, Opacity);

            var backgroundShader = ShaderManager.GetShader("MonochromeShader");
            backgroundShader.TrySetParameter("contrastInterpolant", contrastInterpolant);
            backgroundShader.Apply();

            Main.spriteBatch.Draw(NoxusEgg.MyTexture.Value, Position - Main.screenPosition, null, c, Rotation, NoxusEgg.MyTexture.Size() * 0.5f, Scale, 0, 0f);
            Main.spriteBatch.ResetToDefault();
        }
    }
}
