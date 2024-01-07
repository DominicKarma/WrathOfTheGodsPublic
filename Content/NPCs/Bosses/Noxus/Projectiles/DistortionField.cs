using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class DistortionField : ModProjectile, IProjOwnedByBoss<EntropicGod>, IDrawAdditive
    {
        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 150;

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 360;
            Projectile.height = 360;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Handle fade effects.
            Projectile.Opacity = InverseLerpBump(0f, 10f, Lifetime * 0.5f, Lifetime, Time);

            // Emit a bunch of gas.
            float fogOpacity = Projectile.Opacity * 0.6f;
            for (int i = 0; i < 3; i++)
            {
                Vector2 fogVelocity = Main.rand.NextVector2Circular(36f, 36f) * Projectile.Opacity;
                HeavySmokeParticle fog = new(Projectile.Center, fogVelocity, NoxusSky.FogColor, 50, 3f, fogOpacity, 0f, true);
                fog.Spawn();
            }

            Time++;

            if (Projectile.timeLeft <= 60)
                Projectile.damage = 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_RegularAttack);
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Projectile.Size / HollowCircleSoftEdge.Size() * 1.4f;
            spriteBatch.Draw(HollowCircleSoftEdge, drawPosition, null, Projectile.GetAlpha(Color.MediumPurple), Projectile.rotation, HollowCircleSoftEdge.Size() * 0.5f, scale, 0, 0f);
        }
    }
}
