using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class ExplodingStar : ModProjectile, IDrawsWithShader, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public bool ShaderShouldDrawAdditively => true;

        public bool SetActiveFalseInsteadOfKill => true;

        public static bool FromStarConvergenceAttack => NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.InwardStarPattenedExplosions;

        public ref float Temperature => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public ref float ScaleGrowBase => ref Projectile.ai[0];

        public ref float StarburstShootSpeedFactor => ref Projectile.ai[1];

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 40;

            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.ConjureExplodingStars)
                Projectile.timeLeft = 30;
            if (FromStarConvergenceAttack)
                Projectile.timeLeft = 15;
        }

        public override void AI()
        {
            // Initialize the star temperature. This is used for determining colors.
            if (Temperature <= 0f)
                Temperature = Main.rand.NextFloat(3000f, 32000f);

            Time++;

            // Perform scale effects to do the explosion.
            if (Time <= 20f && !FromStarConvergenceAttack)
                Projectile.scale = Pow(InverseLerp(0f, 20f, Time), 2.7f);
            else
            {
                if (ScaleGrowBase < 1f)
                    ScaleGrowBase = 1.066f;

                Projectile.scale *= ScaleGrowBase;
            }

            float fadeIn = InverseLerp(0.05f, 0.2f, Projectile.scale);
            float fadeOut = InverseLerp(40f, 31f, Time);
            Projectile.Opacity = fadeIn * fadeOut;

            // Create screenshake and play explosion sounds when ready.
            if (Time == 11f)
            {
                StartShakeAtPoint(Projectile.Center, 5f, TwoPi, Vector2.UnitX, 0.1f);

                SoundStyle explosionSound = FromStarConvergenceAttack ? NamelessDeityBoss.SupernovaSound : NamelessDeityBoss.GenericBurstSound with { Pitch = 0.5f };
                SoundEngine.PlaySound(explosionSound with { MaxInstances = 3 });
                ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 30);
                NamelessDeityKeyboardShader.BrightnessIntensity += 0.23f;
            }
            if (fadeOut <= 0.8f)
                Projectile.damage = 0;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.19f, targetHitbox);
        }

        public override void OnKill(int timeLeft)
        {
            // Release and even spread of starbursts.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int starburstCount = 5;
                int starburstID = ModContent.ProjectileType<ArcingStarburst>();
                float starburstSpread = TwoPi;
                float starburstSpeed = 22f;
                if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.InwardStarPattenedExplosions)
                    starburstCount = 7;

                if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.BackgroundStarJumpscares)
                    return;

                Vector2 directionToTarget = Projectile.DirectionToSafe(Main.player[Player.FindClosest(Projectile.Center, 1, 1)].Center);
                for (int i = 0; i < starburstCount; i++)
                {
                    Vector2 starburstVelocity = directionToTarget.RotatedBy(Lerp(-starburstSpread, starburstSpread, i / (float)(starburstCount - 1f)) * 0.5f) * starburstSpeed + Main.rand.NextVector2Circular(starburstSpeed, starburstSpeed) / 11f;
                    NewProjectileBetter(Projectile.Center, starburstVelocity, starburstID, NamelessDeityBoss.StarburstDamage, 0f, -1);
                }
            }
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            float colorInterpolant = InverseLerp(3000f, 32000f, Temperature);
            Color starColor = MulticolorLerp(colorInterpolant, Color.Red, Color.Orange, Color.Yellow);
            starColor = Color.Lerp(starColor, Color.IndianRed, 0.32f);

            var fireballShader = ShaderManager.GetShader("FireballShader");
            fireballShader.TrySetParameter("mainColor", starColor.ToVector3() * Projectile.Opacity);
            fireballShader.TrySetParameter("resolution", new Vector2(100f, 100f));
            fireballShader.TrySetParameter("speed", 0.76f);
            fireballShader.TrySetParameter("zoom", 0.0004f);
            fireballShader.TrySetParameter("dist", 60f);
            fireballShader.TrySetParameter("opacity", Projectile.Opacity);
            fireballShader.SetTexture(FireNoise, 1);
            fireballShader.SetTexture(TurbulentNoise, 2);
            fireballShader.Apply();

            spriteBatch.Draw(InvisiblePixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, InvisiblePixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.3f, SpriteEffects.None, 0f);

            fireballShader.TrySetParameter("mainColor", Color.Wheat.ToVector3() * Projectile.Opacity * 0.6f);
            fireballShader.Apply();
            spriteBatch.Draw(InvisiblePixel, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, InvisiblePixel.Size() * 0.5f, Projectile.width * Projectile.scale * 1.08f, SpriteEffects.None, 0f);
        }
    }
}
