using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;

namespace NoxusBoss.Common.BaseEntities
{
    public abstract class BaseTelegraphedPrimitiveLaserbeam : BasePrimitiveLaserbeam
    {
        public PrimitiveTrail TelegraphDrawer
        {
            get;
            protected set;
        }

        public abstract int TelegraphPointCount
        {
            get;
        }

        public abstract ManagedShader TelegraphShader
        {
            get;
        }

        public ref float TelegraphTime => ref Projectile.ai[0];

        public virtual List<Vector2> GenerateTelegraphControlPoints()
        {
            // Calculate telegraph control points. The key difference between this and the laser is that the telegraph always reaches out by the laser's maximum distance, while the laser bursts out a bit initially.
            // This can be overridden with something else if desired, however.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            return Projectile.GetLaserControlPoints(6, MaxLaserLength, laserDirection);
        }

        public abstract void OnLaserFire();

        public abstract void PrepareTelegraphShader(ManagedShader telegraphShader);

        public abstract float TelegraphWidthFunction(float completionRatio);

        public abstract Color TelegraphColorFunction(float completionRatio);

        // This overrides the behavior of BasePrimitiveLaserbeam completely.
        public override void AI()
        {
            // Make the laser extend after the telegraph has vanished.
            if (Time >= TelegraphTime)
                LaserLengthFactor = Lerp(LaserLengthFactor, 1f, LaserExtendSpeedInterpolant);

            // Decide the rotation of the laser.
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Die once the laser is done existing.
            if (Time >= TelegraphTime + LaserShootTime)
                Projectile.Kill();

            // Apply special effects when the laser fires.
            if (Time == TelegraphTime - 1f)
                OnLaserFire();

            Time++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= TelegraphTime || Time >= TelegraphTime + LaserShootTime - 7f)
                return false;

            float _ = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * LaserLengthFactor * MaxLaserLength;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.scale * Projectile.width * 0.9f, ref _);
        }

        public void DrawTelegraphOrLaser()
        {
            // Initialize primitive drawers.
            TelegraphDrawer ??= new(TelegraphWidthFunction, TelegraphColorFunction, null, true, TelegraphShader);
            LaserDrawer ??= new(LaserWidthFunction, LaserColorFunction, null, true, LaserShader);

            // Draw the telegraph at first.
            Vector2 laserDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            if (Time <= TelegraphTime)
            {
                PrepareTelegraphShader(TelegraphShader);
                TelegraphDrawer.Draw(GenerateTelegraphControlPoints(), -Main.screenPosition, TelegraphPointCount);
                return;
            }

            // Draw the laser after the telegraph has ceased.
            DrawLaser();
        }

        // This overrides the behavior of BasePrimitiveLaserbeam completely.
        public override bool PreDraw(ref Color lightColor)
        {
            // Do nothing if standard drawing is disabled.
            if (!UseStandardDrawing)
                return false;

            // Draw the laser manually if standard drawing is enabled.
            DrawTelegraphOrLaser();
            return false;
        }
    }
}
