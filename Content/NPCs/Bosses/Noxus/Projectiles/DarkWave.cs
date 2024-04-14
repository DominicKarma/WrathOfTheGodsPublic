using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class DarkWave : ModProjectile, IProjOwnedByBoss<EntropicGod>, IDrawsWithShader
    {
        public int Lifetime = 60;

        public float Opacity = 1f;

        public float MinScale = 1.2f;

        public float MaxScale = 5f;

        public float MaxRadius = 2000f;

        public float RadiusExpandRateInterpolant = 0.08f;

        public ref float Radius => ref Projectile.ai[0];

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            // Do screen shake effects.
            if (Projectile.localAI[0] == 0f)
            {
                StartShakeAtPoint(Projectile.Center, 7f);
                Projectile.localAI[0] = 1f;
            }

            // Cause the wave to expand outward, along with its hitbox.
            Radius = Lerp(Radius, MaxRadius, RadiusExpandRateInterpolant);
            Projectile.scale = Lerp(MinScale, MaxScale, InverseLerp(Lifetime, 0f, Projectile.timeLeft));

            if (Projectile.ai[1] != 0f)
                Projectile.Opacity = Projectile.ai[1];
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CircularHitboxCollision(Projectile.Center, Radius * 0.4f, targetHitbox);
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            DrawData explosionDrawData = new(DendriticNoise, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

            var shockwaveShader = ShaderManager.GetShader("NoxusBoss.ShockwaveShader");
            shockwaveShader.TrySetParameter("shockwaveColor", Color.Lerp(Color.MediumSlateBlue, Color.Black, 0.1f).ToVector3());
            shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
            shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
            shockwaveShader.TrySetParameter("projectilePosition", Projectile.Center - Main.screenPosition);
            shockwaveShader.TrySetParameter("shockwaveOpacityFactor", Projectile.Opacity);
            shockwaveShader.Apply();
            explosionDrawData.Draw(Main.spriteBatch);
        }
    }
}
