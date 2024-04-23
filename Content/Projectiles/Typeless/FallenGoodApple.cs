using Microsoft.Xna.Framework;
using NoxusBoss.Content.Items;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Typeless
{
    public class FallenGoodApple : ModProjectile
    {
        /// <summary>
        /// How long this apple has existed for, in frames.
        /// </summary>
        public ref float Time => ref Projectile.ai[0];

        /// <summary>
        /// The fall-over direction of this apple.
        /// </summary>
        public ref float FallOverDirection => ref Projectile.ai[1];

        /// <summary>
        /// How long this apple should last for, in frames.
        /// </summary>
        public static int Lifetime => SecondsToFrames(7.5f);

        public override string Texture => "NoxusBoss/Content/Items/GoodApple";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 450;
            FallOverDirection = Main.rand?.NextFromList(-1f, 1f) ?? 1f;
        }

        public override void AI()
        {
            // Fade out when close to death.
            Projectile.Opacity = InverseLerp(Lifetime, Lifetime - 120f, Time);

            // Move.
            Projectile.velocity.Y += 0.3f;

            // Decide Projectile.rotation.
            if (Abs(Projectile.velocity.Y) >= 0.0001f)
                Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;
            else
            {
                Projectile.velocity.X = 0f;

                float fallOverSpeed = Utils.Remap(Abs(Projectile.rotation), 0.1f, 0.6f, 0.04f, 0.12f);
                Projectile.rotation = Clamp(Projectile.rotation + FallOverDirection * fallOverSpeed, -PiOver2, PiOver2);
            }

            // Give the player an apple if interacted with.
            if (Main.LocalPlayer.Hitbox.Intersects(Projectile.Hitbox) && Abs(Projectile.velocity.Y - 0.3f) <= 0.0001f)
            {
                Projectile.Kill();
                Main.LocalPlayer.QuickSpawnItem(new EntitySource_WorldEvent(), ModContent.ItemType<GoodApple>());
            }

            Time++;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Abs(oldVelocity.Y - 0.3f) >= 0.001f)
                SoundEngine.PlaySound(SoundID.Item48 with { MaxInstances = 15, Volume = 0.75f, PitchVariance = 0.2f }, Projectile.Center);
            Projectile.velocity.X = 0f;
            return false;
        }

        public override bool PreDraw(ref Color lightColor) => true;
    }
}
