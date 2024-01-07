using Microsoft.Xna.Framework;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class NamelessDeityAfterimage : ModProjectile, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public ref float BaseScale => ref Projectile.ai[0];

        public ref float Rotation => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public static int GrowToFullSizeTime => 60;

        public static float MaxScale => 4f;

        public static int Lifetime => SecondsToFrames(0.32f);

        public override string Texture => InvisiblePixelPath;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            // No Nameless Deity? Die.
            if (NamelessDeityBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Fade out.
            Projectile.Opacity = Pow(1f - Time / Lifetime, 0.6f) * NamelessDeityBoss.Myself.As<NamelessDeityBoss>().AfterimageOpacityFactor;

            // Expand/collapse.
            if (BaseScale > NamelessDeityBoss.DefaultScaleFactor * 0.99f)
                Projectile.scale *= 0.96f;
            else
            {
                Projectile.scale *= 1.018f;
                Projectile.Opacity *= 0.95f;
            }

            // Slow down.
            Projectile.velocity *= 0.976f;

            // Increment the timer.
            Time++;
        }

        public void DrawSelf()
        {
            // Prepare the afterimage psychedelic shader.
            var target = NamelessDeityTargetManager.NamelessDeityTarget;
            var afterimageShader = ShaderManager.GetShader("NamelessDeityPsychedelicAfterimageShader");
            afterimageShader.TrySetParameter("uScreenResolution", Main.ScreenSize.ToVector2());
            afterimageShader.TrySetParameter("warpSpeed", Time * 0.00011f);
            afterimageShader.SetTexture(TurbulentNoise, 1);
            afterimageShader.Apply();

            // Draw the target.
            float scale = Projectile.scale * BaseScale;
            float colorInterpolant = (Projectile.identity / 19f + Main.GlobalTimeWrappedHourly * 0.75f + Projectile.Center.X / 150f) % 1f;
            Color afterimageColor = MulticolorLerp(colorInterpolant, new(0, 255, 255), Color.Green, new(197, 255, 251));
            Main.spriteBatch.Draw(target, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(afterimageColor), Rotation, target.Size() * 0.5f, scale, 0, 0f);
        }
    }
}
