using Luminance.Assets;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class ArcingStarburst : ModProjectile, IDrawAdditive, IPixelatedPrimitiveRenderer, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public static LazyAsset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float MaxSpeedFactor => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 13;

            if (Main.netMode != NetmodeID.Server)
                MyTexture = LazyAsset<Texture2D>.Request(Texture);
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * 120;
        }

        public override void AI()
        {
            // Die if Nameless is not present.
            if (NamelessDeityBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            if (MaxSpeedFactor <= 0f)
                MaxSpeedFactor = 1f;

            // Release short-lived orange-red sparks.
            if (Main.rand.NextBool(15))
            {
                Color sparkColor = Color.Lerp(Color.Yellow, Color.Cyan, Main.rand.NextFloat(0.4f, 0.85f));
                sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                spark.noLight = true;
                spark.color = sparkColor;
                spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                spark.noGravity = spark.velocity.Length() >= 3.5f;
                spark.scale = spark.velocity.Length() * 0.1f + 0.64f;
            }

            // Animate frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            // Handle arcing behaviors.
            int slowdownTime = 53;
            int redirectTime = 27;
            int fastHomeTime = 86;
            NPCAimedTarget target = NamelessDeityBoss.Myself.GetTargetData();
            Vector2 directionToTarget = Projectile.SafeDirectionTo(target.Center);
            if (Time <= slowdownTime)
                Projectile.velocity *= 0.84f;
            else if (Time <= slowdownTime + redirectTime)
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * 10f, 0.035f);
            else if (Time <= slowdownTime + redirectTime + fastHomeTime)
            {
                float maxBaseSpeed = Lerp(23.75f, 32f, Projectile.identity / 8f % 1f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, directionToTarget * MaxSpeedFactor * maxBaseSpeed, 0.017f);

                // Die if the target has been touched, to prevent unfair telefrags.
                if (Projectile.WithinRange(target.Center, 28f))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        Color sparkColor = Color.Lerp(Color.Yellow, Color.Cyan, Main.rand.NextFloat(0.4f, 0.98f));
                        sparkColor = Color.Lerp(sparkColor, Color.Wheat, 0.4f);

                        Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), 264);
                        spark.noLight = true;
                        spark.color = sparkColor;
                        spark.velocity = Main.rand.NextVector2Circular(10f, 10f);
                        spark.noGravity = true;
                        spark.scale = spark.velocity.Length() * 0.1f + 0.94f;
                    }

                    Projectile.Kill();
                }
            }
            else
                Projectile.velocity *= 1.019f;

            // Fade in and out based on how long the starburst has existed.
            Projectile.Opacity = InverseLerp(0f, 24f, Projectile.timeLeft) * InverseLerp(0f, 20f, Time);
            Projectile.rotation = Projectile.velocity.ToRotation() - PiOver2;

            Time++;
        }

        public float FlameTrailWidthFunction(float completionRatio)
        {
            return SmoothStep(18f, 5f, completionRatio) * Projectile.Opacity;
        }

        public Color FlameTrailColorFunction(float completionRatio)
        {
            // Make the trail fade out at the end and fade in sharply at the start, to prevent the trail having a definitive, flat "start".
            float trailOpacity = InverseLerpBump(0f, 0.067f, 0.27f, 0.75f, completionRatio) * 0.9f;

            // Interpolate between a bunch of colors based on the completion ratio.
            Color startingColor = Color.Lerp(Color.White, Color.IndianRed, 0.25f);
            Color middleColor = Color.Lerp(Color.Yellow, Color.Cyan, Lerp(0.35f, 0.95f, Projectile.identity / 14f % 1f));
            Color endColor = Color.Lerp(Color.DeepSkyBlue, Color.Black, 0.35f);
            Color color = MulticolorLerp(completionRatio, startingColor, middleColor, endColor) * trailOpacity;

            color.A = (byte)(trailOpacity * 255);
            return color * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Draw the bloom flare.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;

            Color baseColor1 = Color.Turquoise;
            Color baseColor2 = Color.DeepSkyBlue;
            Color bloomFlareColor1 = baseColor1 with { A = 0 } * Projectile.Opacity * 0.54f;
            Color bloomFlareColor2 = baseColor2 with { A = 0 } * Projectile.Opacity * 0.54f;

            Vector2 bloomFlareDrawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(BloomFlare, bloomFlareDrawPosition, null, bloomFlareColor1, bloomFlareRotation, BloomFlare.Size() * 0.5f, Projectile.scale * 0.08f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, bloomFlareDrawPosition, null, bloomFlareColor2, -bloomFlareRotation, BloomFlare.Size() * 0.5f, Projectile.scale * 0.096f, 0, 0f);

            // Draw the star.
            Texture2D texture = MyTexture.Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(Color.White);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0f);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            ManagedShader shader = ShaderManager.GetShader("NoxusBoss.GenericFlameTrail");
            shader.SetTexture(StreakMagma, 1, SamplerState.LinearWrap);

            PrimitiveSettings settings = new(FlameTrailWidthFunction, FlameTrailColorFunction, _ => Projectile.Size * 0.5f, Pixelate: true, Shader: shader);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos, settings, 7);
        }
    }
}
