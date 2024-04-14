using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class NoxSpike : ModProjectile, IDrawAdditive, IProjOwnedByBoss<EntropicGod>
    {
        public ref float Index => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public static int TelegraphTime
        {
            get
            {
                float telegraphSeconds = 0.6f;
                if (Main.expertMode)
                    telegraphSeconds = 0.55f;
                if (CommonCalamityVariables.RevengeanceModeActive)
                    telegraphSeconds = 0.5f;
                if (Main.zenithWorld)
                    telegraphSeconds = 0.16f;

                return SecondsToFrames(telegraphSeconds);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4350;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.MaxUpdates = 5;
            Projectile.timeLeft = Projectile.MaxUpdates * 300;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Accelerate over time.
            Projectile.velocity *= 1.012f;

            // Decide the current rotation.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            if (Projectile.IsFinalExtraUpdate())
            {
                Time++;

                // Fire once the telegraphing is done.
                if (Time == TelegraphTime)
                {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 4f;
                    Projectile.netUpdate = true;
                }
            }
        }

        public override bool? CanDamage() => Projectile.velocity.Length() >= 1f;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_RegularAttack);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.White;
            drawColor.A = (byte)Utils.Remap(Projectile.velocity.Length(), 3f, 13f, 255f, 0f);
            DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], drawColor);

            // Draw a pulsating overlay if moving very slowly.
            float overlayOpacity = InverseLerp(2.4f, 0.01f, Projectile.velocity.Length()) * 0.3f;
            float pulsationInterpolant = Cos01(TwoPi * Index / 12f + Main.GlobalTimeWrappedHourly * 15f);
            float scalePulsation = Lerp(1f, 1.3f, pulsationInterpolant);
            if (overlayOpacity > 0f)
            {
                Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(drawColor) * overlayOpacity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * scalePulsation, 0, 0);
            }

            return false;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Draw a telegraph line.
            float bloomLineIntensity = Pow(Convert01To010(InverseLerp(0f, TelegraphTime, Time)), 0.4f);
            spriteBatch.DrawBloomLine(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 4000f, Color.HotPink * bloomLineIntensity, 18f);
        }
    }
}
