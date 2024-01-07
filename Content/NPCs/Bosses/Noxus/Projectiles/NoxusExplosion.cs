using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class NoxusExplosion : ModProjectile, IDrawAdditive, IProjOwnedByBoss<EntropicGod>
    {
        public const int Lifetime = 54;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 720;
            Projectile.hostile = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.35f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.5f);
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 3 == 2)
                Projectile.frame++;

            if (Projectile.frame >= 18)
                Projectile.Kill();
            Projectile.scale *= 1.0115f;
            Projectile.Opacity = InverseLerpBump(2f, 12f, Lifetime - 1f, Lifetime, Projectile.timeLeft);

            if (Projectile.timeLeft == 40)
            {
                SoundEngine.PlaySound(EntropicGod.ExplosionSound, Projectile.Center);

                // Release a little bit of bright dust.
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 dustVelocity = Main.rand.NextVector2Circular(20f, 20f);
                    Dust dust = Dust.NewDustPerfect(dustSpawnPosition, 264, dustVelocity);
                    dust.color = Color.Lerp(Color.SkyBlue, Color.Fuchsia, Main.rand.NextFloat(0.35f, 0.65f));
                    dust.scale = 2.2f;
                    dust.noLight = true;
                    dust.noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_RegularAttack);
        }

        public void DrawSelf(float scale, Color color)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(3, 6, Projectile.frame / 6, Projectile.frame % 6);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Main.EntitySpriteDraw(texture, drawPosition, frame, color, 0f, origin, scale, 0, 0);
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Draw an additive bloom circle behind the explosion.
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color lightBurstColor = Color.Lerp(Color.Lerp(Color.Blue, Color.BlueViolet, Projectile.timeLeft / 303f), Color.Purple, 0.64f) * 0.9f;
            lightBurstColor = Color.Lerp(lightBurstColor, Color.White, -0.1f) * Projectile.Opacity;
            Main.EntitySpriteDraw(BloomCircle, drawPosition, null, lightBurstColor, 0f, BloomCircle.Size() * 0.5f, Projectile.scale * 1.27f, SpriteEffects.None, 0);

            DrawSelf(1.88f, Color.White);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawSelf(1.5f, Color.Black);
            return false;
        }

        public override bool? CanDamage() => Projectile.timeLeft < 84;
    }
}
