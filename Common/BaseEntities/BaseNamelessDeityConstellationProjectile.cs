using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.ShapeCurves;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.BaseEntities
{
    public abstract class BaseNamelessDeityConstellationProjectile : ModProjectile
    {
        /// <summary>
        /// A cached value of <see cref="constellationShape"/>.
        /// </summary>
        /// <remarks>
        /// This stores the constellationShape property in a field for performance reasons every frame, since the underlying getter method used there can be straining when done
        /// many times per frame, due to looping.
        /// </remarks>
        public ShapeCurve ConstellationShape;

        /// <summary>
        /// A static cache for the bloom flare texture that's stored for performance reasons.
        /// </summary>
        protected static Texture2D bloomFlare
        {
            get;
            private set;
        }

        /// <summary>
        /// A static cache for the bloom circle texture that's stored for performance reasons.
        /// </summary>
        protected static Texture2D bloomCircle
        {
            get;
            private set;
        }

        /// <summary>
        /// A static cache for the star texture that's stored for performance reasons.
        /// </summary>
        protected static Texture2D starTexture
        {
            get;
            private set;
        }

        /// <summary>
        /// The shape curve that composes the constellation's shape.
        /// </summary>
        protected abstract ShapeCurve constellationShape
        {
            get;
        }

        /// <summary>
        /// How long it takes for the constellation to completely converge.
        /// </summary>
        public abstract int ConvergeTime
        {
            get;
        }

        /// <summary>
        /// The factor by which stars are randomly offset from the positions of the points in the <see cref="constellationShape"/>.
        /// </summary>
        public abstract float StarRandomOffsetFactor
        {
            get;
        }

        /// <summary>
        /// This determines how much the star draw loop is incremented. Higher values result in better efficiency but less detail.
        /// </summary>
        /// <remarks>
        /// By default his can typically be set to 1 without issue.
        /// </remarks>
        public abstract int StarDrawIncrement
        {
            get;
        }

        /// <summary>
        /// The animation time factor for star convergence.
        /// </summary>
        public abstract float StarConvergenceSpeed
        {
            get;
        }

        /// <summary>
        /// Decides the primary bloom flare color.
        /// </summary>
        /// <param name="colorVariantInterpolant">The color variant interpolant.</param>
        public abstract Color DecidePrimaryBloomFlareColor(float colorVariantInterpolant);

        /// <summary>
        /// Decides the secondary bloom flare color.
        /// </summary>
        /// <param name="colorVariantInterpolant">The color variant interpolant.</param>
        public abstract Color DecideSecondaryBloomFlareColor(float colorVariantInterpolant);

        /// <summary>
        /// The scale factor of the stars that compose the shape.
        /// </summary>
        public virtual float StarScaleFactor => Utils.Remap(Time, ConvergeTime * 0.5f, ConvergeTime, 1f, 2.6f);

        /// <summary>
        /// The amount of time, in frames, it has been since the constellation was created.
        /// </summary>
        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => SparkTexturePath;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void AI()
        {
            // Die if Nameless is not present.
            if (NamelessDeityBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Store the constellation shape.
            ConstellationShape = constellationShape;

            // Increment the time.
            if (Projectile.IsFinalExtraUpdate())
                Time++;
        }

        /// <summary>
        /// Gets the movement interpolant from scattered to focused for a given star.
        /// </summary>
        /// <param name="index">The star's index.</param>
        public float GetStarMovementInterpolant(int index)
        {
            int starPrepareStartTime = (int)(index * ConvergeTime * StarConvergenceSpeed) + 10;
            return Pow(InverseLerp(starPrepareStartTime, starPrepareStartTime + 56f, Time), 0.68f);
        }

        /// <summary>
        /// Calculates a given star's draw position.
        /// </summary>
        /// <param name="index">The star's index.</param>
        public Vector2 GetStarPosition(int index)
        {
            // Calculate the seed for the starting spots of the stars. This is randomized based on both projectile index and star index, so it should be
            // pretty unique across the fight.
            ulong starSeed = (ulong)Projectile.identity * 113uL + (ulong)index * 602uL + 54uL;

            // Orient the stars in such a way that they come from the background in random spots.
            Vector2 starDirectionFromCenter = (ConstellationShape.ShapePoints[index] - ConstellationShape.Center).SafeNormalize(Vector2.UnitY);
            Vector2 randomOffset = new(Lerp(-1350f, 1350f, Utils.RandomFloat(ref starSeed)), Lerp(-920f, 920f, Utils.RandomFloat(ref starSeed)));
            Vector2 startingSpot = Main.ScreenSize.ToVector2() * 0.5f + starDirectionFromCenter * 500f + randomOffset;
            Vector2 starPosition = ConstellationShape.ShapePoints[index] + Projectile.Center - Main.screenPosition;

            // Apply a tiny, random offset to the star position.
            starPosition += Lerp(-TwoPi, TwoPi, Utils.RandomFloat(ref starSeed)).ToRotationVector2() * Lerp(1.5f, 5.3f, Utils.RandomFloat(ref starSeed)) * StarRandomOffsetFactor;

            return Vector2.Lerp(startingSpot, starPosition, GetStarMovementInterpolant(index));
        }

        /// <summary>
        /// Draws bloom flare for a given star.
        /// </summary>
        /// <param name="drawPosition">The star's draw position.</param>
        /// <param name="colorVariantInterpolant">An interpolant that exists to create variance between star colors.</param>
        /// <param name="scale">The star's scale.</param>
        /// <param name="index">The star's index.</param>
        public void DrawBloomFlare(Vector2 drawPosition, float colorVariantInterpolant, float scale, int index)
        {
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            Color bloomFlareColor1 = DecidePrimaryBloomFlareColor(colorVariantInterpolant);
            Color bloomFlareColor2 = DecideSecondaryBloomFlareColor(colorVariantInterpolant);

            bloomFlareColor1 *= Utils.Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);
            bloomFlareColor2 *= Utils.Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);

            // Make the stars individually twinkle.
            float scaleFactorPhaseShift = index * 5.853567f * (index % 2 == 0).ToDirectionInt();
            float scaleFactor = Lerp(0.75f, 1.25f, Cos01(Main.GlobalTimeWrappedHourly * 6.4f + scaleFactorPhaseShift));
            scale *= scaleFactor;

            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor1 with { A = 0 } * Projectile.Opacity, bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.11f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor2 with { A = 0 } * Projectile.Opacity, -bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.08f, 0, 0f);
        }

        /// <summary>
        /// Draws a given star.
        /// </summary>
        /// <param name="drawPosition">The star's draw position.</param>
        /// <param name="colorVariantInterpolant">An interpolant that exists to create variance between star colors.</param>
        /// <param name="scale">The star's scale.</param>
        /// <param name="index">The star's index.</param>
        public void DrawStar(Vector2 drawPosition, float colorVariantInterpolant, float scale, int index)
        {
            // Draw a bloom flare behind the star.
            DrawBloomFlare(drawPosition, colorVariantInterpolant, scale * (NamelessDeityBoss.Myself?.scale ?? 0f), index);

            // Draw the star.
            Rectangle frame = starTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = Projectile.GetAlpha(Color.Wheat) with { A = 0 } * Utils.Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.3f, 1f);

            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, scale * 0.5f, 0, 0f);
            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation - Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation + Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Store textures for efficiency.
            bloomCircle ??= BloomCircle;
            bloomFlare ??= BloomFlare;
            starTexture ??= ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

            ulong starSeed = (ulong)Projectile.identity * 674uL + 25uL;

            // Draw the stars that compose the constellation's outline.
            for (int i = 0; i < ConstellationShape.ShapePoints.Count; i += StarDrawIncrement)
            {
                float colorVariantInterpolant = Sqrt(Utils.RandomFloat(ref starSeed));
                float scale = StarScaleFactor * Lerp(0.15f, 0.95f, Utils.RandomFloat(ref starSeed)) * Projectile.scale;

                // Make the scale more uniform as the star scale factor gets larger.
                scale = Utils.Remap(StarScaleFactor * 0.75f, scale, StarScaleFactor, 1f, 2.5f) * StarScaleFactor / 2.6f;

                Vector2 shapeDrawPosition = GetStarPosition(i);
                DrawStar(shapeDrawPosition, colorVariantInterpolant, scale * 0.4f, i);
            }

            return false;
        }
    }
}
