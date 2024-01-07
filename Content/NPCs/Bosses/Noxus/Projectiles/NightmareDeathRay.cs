using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles
{
    public class NightmareDeathRay : ModProjectile, IProjOwnedByBoss<EntropicGod>
    {
        public PrimitiveTrail3D BeamDrawer
        {
            get;
            set;
        }

        public float LaserLength
        {
            get;
            set;
        }

        public Vector3 Rotation;

        public Matrix RotationMatrix;

        public float DarknessInterpolant
        {
            get
            {
                if (EntropicGod.Myself is null)
                    return 0f;

                Vector3 direction3D = Vector3.Transform(Vector3.UnitX, RotationMatrix);
                Vector2 direction = new(direction3D.X, -direction3D.Y * EntropicGod.Myself.As<EntropicGod>().LaserLengthFactor);
                return InverseLerp(PiOver4, 0f, direction.AngleBetween(Vector2.UnitY));
            }
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public static float SquishFactor => 1.18f;

        public const float MaxLaserLength = 4600f;

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 225;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 9000;
            Projectile.alpha = 255;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(LaserLength);
            writer.Write(Rotation.X);
            writer.Write(Rotation.Y);
            writer.Write(Rotation.Z);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LaserLength = reader.ReadSingle();
            Rotation = new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public override void AI()
        {
            // Immediately die if Noxus is not present.
            if (EntropicGod.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Stick above Noxus.
            Projectile.Center = EntropicGod.Myself.Center - Vector2.UnitY.RotatedBy(EntropicGod.Myself.rotation) * EntropicGod.Myself.scale * 200f;

            // Fade in.
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 25, 0, 255);

            // Determine the scale of the laser.
            CalculateScale();

            // The laser technically always faces to the right for Terraria's 2D system.
            // In 3D, however, the laser orientation is controlled by custom rotation values that are inherited from Noxus.
            // The matrix is stored as a variable instead of a property since having it used 200 times every frame for NPC collision loops would not be ideal.
            Projectile.velocity = Vector2.UnitX;
            Rotation = EntropicGod.Myself.As<EntropicGod>().LaserRotation;
            RotationMatrix = CreateRotationMatrix(Rotation) * Matrix.CreateScale(1f, 1f / SquishFactor, 1f);

            if (Time >= Lifetime)
                Projectile.Kill();

            // Make the laser quickly move outward.
            LaserLength = Pow(InverseLerp(4f, 30f, Time), 2.4f) * MaxLaserLength * SquishFactor * EntropicGod.Myself.As<EntropicGod>().LaserLengthFactor;

            // And create bright light.
            Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1.4f);

            Time++;
        }

        public static Matrix CreateRotationMatrix(Vector3 rotations)
        {
            return Matrix.CreateRotationZ(rotations.Z) * Matrix.CreateRotationY(rotations.Y) * Matrix.CreateRotationX(rotations.X);
        }

        public void CalculateScale()
        {
            Projectile.scale = Convert01To010(Time / Lifetime) * 1.45f;
            if (Projectile.scale > 1f)
                Projectile.scale = 1f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= 30f || Projectile.timeLeft <= 10)
                return false;

            float _ = 0f;
            float width = Projectile.width * 0.4f;

            Vector3 direction3D = Vector3.Transform(Vector3.UnitX, RotationMatrix);
            Vector2 direction = new(direction3D.X, -direction3D.Y);

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + direction * LaserLength, width, ref _);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), EntropicGod.DebuffDuration_PowerfulAttack);
        }

        public float WidthFunction(float completionRatio)
        {
            if (EntropicGod.Myself is null)
                return 0f;

            return Projectile.width * EntropicGod.Myself.As<EntropicGod>().LaserSquishFactor * 1.36f + completionRatio * 170f;
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.DarkSlateBlue, Color.HotPink, 0.3f);
            color = Color.Lerp(color, Color.DeepSkyBlue, 0.12f);

            // Make the color a bit darker when pointing downward.
            color = Color.Lerp(color, Color.Black, DarknessInterpolant * 0.2f + 0.04f);

            float opacity = Projectile.Opacity * InverseLerp(1f, 0.91f, completionRatio) * Projectile.scale * 1.15f;
            return color * opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawLaser();

            Main.spriteBatch.UseBlendState(BlendState.Additive);
            DrawFrontGlow();
            DrawBloomFlare();

            Main.spriteBatch.ResetToDefault();

            return false;
        }

        public void DrawFrontGlow()
        {
            float pulse = Cos(Main.GlobalTimeWrappedHourly * 36f);
            Vector2 origin = BloomCircle.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.scale * 20f;
            Vector2 baseScale = new Vector2(1f + pulse * 0.05f, 1f) * Projectile.scale * 0.7f;
            Main.spriteBatch.Draw(BloomCircle, drawPosition, null, Color.White, 0f, origin, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircle, drawPosition, null, Color.Violet * 0.4f, 0f, origin, baseScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircle, drawPosition, null, Color.Blue * 0.3f, 0f, origin, baseScale * 1.7f, 0, 0f);
        }

        public void DrawBloomFlare()
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.scale * 20f;
            Color bloomFlareColor = Color.Lerp(Color.Wheat, Color.Blue, 0.7f);
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.76f;
            float bloomFlareScale = Projectile.scale * 0.4f;
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor, -bloomFlareRotation, BloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);

            bloomFlareColor = Color.Lerp(Color.Wheat, Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + 0.5f) % 1f, 1f, 0.55f), 0.7f);
            bloomFlareColor = Color.Lerp(bloomFlareColor, Color.Magenta, 0.63f);
            Main.spriteBatch.Draw(BloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, BloomFlare.Size() * 0.5f, bloomFlareScale, 0, 0f);
        }

        public void DrawLaser()
        {
            var laserShader = ShaderManager.GetShader("NoxusLaserShader");
            BeamDrawer ??= new PrimitiveTrail3D(WidthFunction, ColorFunction, null, true, laserShader);

            laserShader.SetTexture(StreakNightmareDeathray, 1);
            laserShader.SetTexture(StreakLightning, 2, SamplerState.LinearWrap);
            laserShader.SetTexture(StreakNightmareDeathrayOverlay, 3, SamplerState.LinearWrap);
            laserShader.SetTexture(DendriticNoise, 4);
            laserShader.SetTexture(ViscousNoise, 5);

            if (Time >= 2f)
            {
                List<Vector2> laserControlPoints = Projectile.GetLaserControlPoints(33, LaserLength, Vector2.UnitX);
                BeamDrawer.SpecifyPerspectiveMatrixMultiplier(CreateRotationMatrix(Rotation) * Matrix.CreateScale(1f, 1f / SquishFactor, 1f));
                BeamDrawer.Draw(laserControlPoints, Projectile.Center, 40);
            }
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
