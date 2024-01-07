using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Pets
{
    public class BlackHolePet : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 82;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            CheckActive();

            // Hover near the owner.
            Vector2 hoverDestination = Owner.Center + new Vector2(Owner.direction * 50f, -36f);
            Projectile.velocity = (Projectile.velocity + Projectile.DirectionToSafe(hoverDestination) * 0.3f).ClampLength(0f, 25f);
            if (Vector2.Dot(Projectile.velocity, Projectile.DirectionToSafe(hoverDestination)) < 0f)
                Projectile.velocity *= 0.96f;

            // Fly away from the owner if too close, so that it doesn't obscure them with the distortion effects.
            Projectile.velocity -= Projectile.DirectionToSafe(Owner.Center) * InverseLerp(145f, 70f, Projectile.Distance(Owner.Center)) * 2f;

            // Teleport near the player if they're very far away.
            if (!Projectile.WithinRange(Owner.Center, 2000f))
            {
                Projectile.Center = Owner.Center - Vector2.UnitY * 150f;
                Projectile.velocity *= 0.15f;
                Projectile.scale = 0f;
                Projectile.netUpdate = true;
            }

            // Grow over time.
            Projectile.scale = Clamp(Projectile.scale + 0.01f, 0f, 1f);

            // Store this pet as the quasar pet.
            if (Main.myPlayer == Projectile.owner)
                GravitationalLensingShaderData.QuasarPet = Projectile;
        }

        public void CheckActive()
        {
            // Keep the projectile from disappearing as long as the player isn't dead and has the pet buff.
            if (!Owner.dead && Owner.HasBuff(ModContent.BuffType<BlackHolePetbuff>()))
                Projectile.timeLeft = 2;
        }
    }
}
