using System.Collections.Generic;
using System.IO;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.BaseEntities;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.ShapeCurves;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class SwordConstellation : BaseNamelessDeityConstellationProjectile, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public float ZPosition;

        public Matrix SquishTransformation
        {
            get
            {
                float squishFactorX = Projectile.scale * 0.387f;
                float squishFactorY = Lerp(2.56f, 1.67f, Projectile.scale);
                return Matrix.CreateScale(squishFactorX * GeneralSquishInterpolant, squishFactorY, 1f);
            }
        }

        public float GeneralSquishInterpolant => InverseLerp(0f, 0.45f, Projectile.scale);

        public ref float SlashCompletion => ref Projectile.ai[0];

        public override int ConvergeTime => ConvergeTimeConst;

        public override int StarDrawIncrement => 1;

        public override float StarConvergenceSpeed => 0.00058f;

        public override float StarRandomOffsetFactor => 0f;

        public override float StarScaleFactor => base.StarScaleFactor * 0.44f;

        protected override ShapeCurve constellationShape
        {
            get
            {
                ShapeCurveManager.TryFind("Sword", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * 0.62f).LinearlyTransform(SquishTransformation).Rotate(Projectile.rotation);
            }
        }

        public override Color DecidePrimaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.SkyBlue, Color.Orange, colorVariantInterpolant) * 0.33f;
        }

        public override Color DecideSecondaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Cyan, Color.White, colorVariantInterpolant) * 0.42f;
        }

        public static readonly int ConvergeTimeConst = SecondsToFrames(2f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = 850;
            Projectile.height = 850;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.timeLeft = 60000;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ZPosition);

        public override void ReceiveExtraAI(BinaryReader reader) => ZPosition = reader.ReadSingle();

        // This is done via PreAI instead of PostAI to ensure that the AI method, which defines the constellation shape, has all the correct information, rather than having a one-frame discrepancy.
        public override bool PreAI()
        {
            // Die if Nameless is not present for some reason.
            if (NamelessDeityBoss.Myself is null)
            {
                Projectile.Kill();
                return false;
            }

            // Appear from the background at first.
            if (Time <= ConvergeTime)
            {
                float zPositionInterpolant = Pow(InverseLerp(15f, ConvergeTime, Time), 0.8f);
                float zPositionVariance = Projectile.identity * 18557.34173f % 12f;
                ZPosition = Lerp(zPositionVariance + 7f, 1.3f, zPositionInterpolant);
            }

            // Fade in based on how long the sword has existed.
            // Also fade out based on how close the stars are to the background.
            Projectile.Opacity = InverseLerp(0f, 30f, Time) * Utils.Remap(ZPosition, 0.2f, 9f, 3.3f, 0.45f) * GeneralSquishInterpolant;

            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= ConvergeTime || NamelessDeityBoss.Myself is null || NamelessDeityBoss.Myself.ai[2] == 0f)
                return false;

            for (int i = 0; i < 30; i++)
            {
                int checkIndex = Utils.Clamp(i / 4 + 1, 1, 15);
                float _ = 0f;
                float angle1 = Projectile.oldRot[checkIndex];
                float angle2 = Projectile.oldRot[checkIndex - 1];
                float angle = angle2.AngleLerp(angle1, i / 4f % 1f);

                Vector2 direction = (angle - PiOver2).ToRotationVector2() * GeneralSquishInterpolant;
                Vector2 start = Projectile.oldPos[checkIndex] + Projectile.Size * 0.5f;
                Vector2 end = start + direction * 240f;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, GeneralSquishInterpolant * 120f, ref _))
                    return true;
            }

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Play a blood sound.
            SoundEngine.PlaySound(LargeBloodSpillSound with { Volume = 3f }, target.Center);

            Vector2 direction = (Projectile.rotation - PiOver2).ToRotationVector2() * GeneralSquishInterpolant;

            // Create a bunch of on-hit blood particles.
            for (int i = 0; i < 15; i++)
            {
                int bloodLifetime = Main.rand.Next(28, 46);
                float bloodScale = Main.rand.NextFloat(0.8f, 1.1f);
                Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                if (Main.rand.NextBool(20))
                    bloodScale *= 2f;

                Vector2 bloodVelocity = direction.RotatedByRandom(0.81f) * Main.rand.NextFloat(11f, 30f);
                bloodVelocity.Y -= 12f;
                BloodParticle blood = new(target.Center, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                blood.Spawn();
            }
            for (int i = 0; i < 25; i++)
            {
                float bloodScale = Main.rand.NextFloat(0.28f, 0.4f);
                Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                Vector2 bloodVelocity = direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(9f, 20.5f);
                BloodParticle2 blood = new(target.Center, bloodVelocity, 30, bloodScale, bloodColor);
                blood.Spawn();
            }
        }

        // Ensure that the blade draws behind Nameless' hand. Wouldn't want the handle awkwardly protruding on top of the robe cloth.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            SpecialLayeringSystem.DrawCacheBeforeBlack_Proj.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the slash.
            Main.spriteBatch.PrepareForShaders();

            // Draw the stars that compose the blade.
            base.PreDraw(ref lightColor);

            return false;
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
