using Luminance.Core.Hooking;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class HeavySmokeParticleRenderer : ManualParticleRenderer<HeavySmokeParticle>, IExistingDetourProvider
    {
        public static bool DrawnManuallyByNamelessDeity => NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().DrawingSmokeManually;

        public override void RenderParticles()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var particle in Particles)
                particle.Draw(Main.spriteBatch);

            Main.spriteBatch.End();
        }

        public void Subscribe() => On_Main.DrawDust += DrawSelfByDefault;

        public void Unsubscribe() => On_Main.DrawDust -= DrawSelfByDefault;

        private static void DrawSelfByDefault(On_Main.orig_DrawDust orig, Main self)
        {
            orig(self);

            if (!DrawnManuallyByNamelessDeity)
                ModContent.GetInstance<HeavySmokeParticleRenderer>().RenderParticles();
        }
    }
}
