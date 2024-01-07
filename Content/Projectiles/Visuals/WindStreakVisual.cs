using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles.Visuals
{
    public class WindStreakVisual : ModProjectile, IDrawGroupedPrimitives
    {
        public int MaxVertices => 3072; // 1024 + 2048

        public int MaxIndices => 8192;

        public PrimitiveTrailInstance PrimitiveInstance
        {
            get;
            private set;
        }

        public PrimitiveGroupDrawContext DrawContext => PrimitiveGroupDrawContext.Pixelated;

        public ManagedShader Shader => ShaderManager.GetShader("GenericTrailStreak");

        public ref float Time => ref Projectile.ai[0];

        public ref float LoopInterpolant => ref Projectile.ai[1];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;

            // This is done to ensure that the streak doesn't immediately disappear once the tip alone has left the screen.
            // The default is 480.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 640;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            PrimitiveInstance = (this as IDrawGroupedPrimitives).GenerateInstance(Projectile);
        }

        public override void AI()
        {
            // Fade in and out based on time.
            Projectile.Opacity = InverseLerp(0f, 32f, Time) * 0.09f;

            // Ensure that the wind quickly fades out if Nameless is present or if the wind is inside of tiles.
            bool insideTile = Collision.SolidCollision(Projectile.Center, 1, 1);
            if (NamelessDeityBoss.Myself is not null || insideTile)
            {
                if (Projectile.timeLeft > 60)
                    Projectile.timeLeft = 60;
            }

            if (insideTile)
                Projectile.Opacity *= 0.5f;

            // Move in a mostly flat horizontal streak, occasionally doing loops.
            float loopSpeed = Lerp(4.4f, 8.5f, Projectile.identity / 16f % 1f);
            float loopAnimationUpdateRate = Lerp(0.016f, 0.031f, Projectile.identity / 11f % 1f);
            float movementSpeed = Projectile.velocity.Length();
            float movementDirection = Sign(Projectile.velocity.X);
            Vector2 horizontalVelocity = Vector2.UnitX * movementSpeed * movementDirection;
            Vector2 baseVelocity = horizontalVelocity + Vector2.UnitY * (Sin(Projectile.Center.X / 132f + Projectile.identity) * 2f - 1.2f);
            Vector2 loopVelocity = (LoopInterpolant * TwoPi).ToRotationVector2() * new Vector2(1f, 0.6f) * loopSpeed;

            if (LoopInterpolant <= 0f)
            {
                Projectile.velocity = baseVelocity;

                // Randomly loop around.
                if (Time >= 32f && Main.rand.NextBool(240))
                {
                    LoopInterpolant = 0.001f;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, loopVelocity, 0.15f);
                LoopInterpolant += loopAnimationUpdateRate;
                if (LoopInterpolant >= 1f)
                {
                    LoopInterpolant = 0f;
                    Projectile.netUpdate = true;
                }
            }

            Time++;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.Lerp(Color.Wheat, Color.IndianRed, Projectile.identity / 12f % 1f * 0.56f);
        }

        public static float WindWidthFunction(float completionRatio) => 7f - (1f - completionRatio) * 3f;

        public Color WindColorFunction(float completionRatio) => Projectile.GetAlpha(Color.White) with { A = 0 } * (1f - completionRatio) * Projectile.Opacity;

        public void PrepareShaderForPrimitives()
        {
            Shader.SetTexture(StreakBloomLine, 1);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Queue index and vertex data to the group.
            PrimitiveInstance.PrimitiveDrawer ??= new(WindWidthFunction, WindColorFunction, null, true, Shader);
            PrimitiveInstance.UpdateBuffers(Projectile.oldPos, Projectile.Size * 0.5f - Main.screenPosition, 7);
            return false;
        }

        public override void OnKill(int timeLeft) => PrimitiveInstance?.Destroy();
    }
}
