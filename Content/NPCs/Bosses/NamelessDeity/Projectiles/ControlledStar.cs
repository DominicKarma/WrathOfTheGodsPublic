using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class ControlledStar : ModProjectile, IDrawsWithShader, IDrawAdditive, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public float LayeringPriority => 0.7f;

        public ref float Time => ref Projectile.ai[0];

        public ref float UnstableOverlayInterpolant => ref Projectile.ai[1];

        public static int GrowToFullSizeTime => 60;

        public static float MaxScale => 4f;

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // No Nameless Deity? Die.
            if (NamelessDeityBoss.Myself is null)
                Projectile.Kill();

            Time++;

            if (UnstableOverlayInterpolant <= 0.01f)
                Projectile.scale = Pow(InverseLerp(1f, GrowToFullSizeTime, Time), 4.1f) * MaxScale;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            Color bloomFlareColor1 = Color.LightGoldenrodYellow;
            Color bloomFlareColor2 = Color.OrangeRed;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float scale = Projectile.scale * 0.5f;
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor1 * Projectile.Opacity * 0.75f, bloomFlareRotation, BloomFlare.Size() * 0.5f, scale, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor2 * Projectile.Opacity * 0.45f, -bloomFlareRotation, BloomFlare.Size() * 0.5f, scale * 1.2f, 0, 0f);
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.Draw(BloomCircleSmall, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 }, Projectile.rotation, BloomCircleSmall.Size() * 0.5f, Projectile.scale * 1.2f, 0, 0f);

            var fireballShader = ShaderManager.GetShader("NoxusBoss.SunShader");
            fireballShader.TrySetParameter("coronaIntensityFactor", UnstableOverlayInterpolant * 0.67f + 0.044f);
            fireballShader.TrySetParameter("mainColor", new Color(204, 163, 79));
            fireballShader.TrySetParameter("darkerColor", new Color(204, 92, 25));
            fireballShader.TrySetParameter("subtractiveAccentFactor", new Color(181, 0, 0));
            fireballShader.TrySetParameter("sphereSpinTime", Main.GlobalTimeWrappedHourly * 0.9f);
            fireballShader.SetTexture(WavyBlotchNoise, 1);
            fireballShader.SetTexture(PsychedelicWingTextureOffsetMap, 2);
            fireballShader.Apply();

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Vector2.One * Projectile.width * Projectile.scale * 1.5f / DendriticNoiseZoomedOut.Size();
            Main.spriteBatch.Draw(DendriticNoiseZoomedOut, drawPosition, null, Color.White with { A = 200 }, Projectile.rotation, DendriticNoiseZoomedOut.Size() * 0.5f, scale, 0, 0f);

            // Draw a pure white overlay over the fireball if instructed.
            if (UnstableOverlayInterpolant >= 0.2f)
            {
                Main.spriteBatch.PrepareForShaders(BlendState.Additive);

                float glowPulse = Sin(Main.GlobalTimeWrappedHourly * UnstableOverlayInterpolant * 55f) * UnstableOverlayInterpolant * 0.42f;
                Main.spriteBatch.Draw(BloomCircle, Projectile.Center - Main.screenPosition, null, Color.White * UnstableOverlayInterpolant, Projectile.rotation, BloomCircle.Size() * 0.5f, Projectile.scale * 0.9f + glowPulse, 0, 0f);
            }
        }
    }
}
