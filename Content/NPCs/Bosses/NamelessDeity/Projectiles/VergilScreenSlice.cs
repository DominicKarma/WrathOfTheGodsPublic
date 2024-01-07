using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class VergilScreenSlice : ModProjectile, IDrawAdditive, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public ref float TelegraphTime => ref Projectile.ai[0];

        public ref float LineLength => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public static readonly int SliceTime = SecondsToFrames(0.31667f);

        public int ShotProjectileTelegraphTime => (int)(TelegraphTime * 2f - 14f);

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 80;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
        }

        public override void AI()
        {
            // Decide the rotation of the line.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Define the universal opacity.
            Projectile.Opacity = InverseLerp(TelegraphTime + SliceTime - 1f, TelegraphTime + SliceTime - 12f, Time);

            if (Time >= TelegraphTime + SliceTime)
                Projectile.Kill();

            // Split the screen if the telegraph is over.
            if (Time == TelegraphTime - 1f && NamelessDeityBoss.Myself is not null)
                LocalScreenSplitSystem.Start(Projectile.Center + Projectile.velocity * LineLength * 0.5f, SliceTime * 2 + 3, Projectile.rotation, Projectile.width * 0.15f);

            if (Time >= TelegraphTime + SliceTime)
                Projectile.Kill();

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= TelegraphTime)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LineLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Create a telegraph.
            if (Time <= TelegraphTime)
            {
                float localLineLength = LineLength * InverseLerp(0f, 15f, Time);
                float opacityFactor = InverseLerp(0f, 4f, Time);
                float scaleFactor = InverseLerp(0f, TelegraphTime * 0.5f, Time) * 0.8f;
                spriteBatch.DrawBloomLine(Projectile.Center, Projectile.Center + Projectile.velocity * localLineLength, Color.IndianRed * opacityFactor, Projectile.width * scaleFactor * 3f);
                spriteBatch.DrawBloomLine(Projectile.Center, Projectile.Center + Projectile.velocity * localLineLength, Color.Wheat * opacityFactor, Projectile.width * scaleFactor * 1.6f);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
