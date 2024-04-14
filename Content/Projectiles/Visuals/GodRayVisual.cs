using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Dusts;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.Graphics.Automators;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Common.Subworlds.EternalGardenUpdateSystem;

namespace NoxusBoss.Content.Projectiles.Visuals
{
    public class GodRayVisual : ModProjectile, IDrawsWithShader
    {
        public const int Width = 450;

        public const int Height = 11000;

        public static Color MainColor => Color.Lerp(Color.Wheat, Color.White, Lerp(0.4f, 0.76f, Cos01(Main.GlobalTimeWrappedHourly * 0.4f)));

        public static Color ColorAccent => new(255, 63, 93);

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 7400;

        public override void SetDefaults()
        {
            // Using the Height constant here has a habit of causing vanilla's out-of-world projectile deletion to kill this, due to how large it is.
            Projectile.width = Width;
            if (WasInSubworldLastUpdateFrame)
                Projectile.width = (int)(Projectile.width * 3.2f);

            Projectile.height = 1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900000;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Fade in or out depending on if Nameless is present.
            bool fadeIn = NamelessDeityBoss.Myself is null;
            Projectile.Opacity = Clamp(Projectile.Opacity + fadeIn.ToDirectionInt() * 0.0379f, 0f, 1f);

            if (Projectile.Opacity <= 0f && !fadeIn)
                Projectile.Kill();

            // Emit light.
            Vector2 rayDirection = Vector2.UnitY.RotatedBy(Projectile.rotation);
            DelegateMethods.v3_1 = Color.LightCoral.ToVector3() * 1.4f;
            Utils.PlotTileLine(Projectile.Bottom - rayDirection * 2500f, Projectile.Bottom + rayDirection, Projectile.width, DelegateMethods.CastLightOpen_StopForSolids);

            // Decide rotation.
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation();

            // Emit small light dust in the ray.
            Vector2 lightSpawnPosition = Vector2.Zero;

            // Try to keep the light outside of tiles.
            for (int i = 0; i < 15; i++)
            {
                lightSpawnPosition = Projectile.Bottom - rayDirection * Main.rand.NextFloat(160f, 1200f);
                lightSpawnPosition += rayDirection.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.width * 0.3f;
                if (!WorldGen.SolidTile(lightSpawnPosition.ToTileCoordinates()))
                    break;
            }

            Dust light = Dust.NewDustPerfect(lightSpawnPosition, ModContent.DustType<FlowerPieceDust>());
            light.color = Color.Lerp(MainColor, ColorAccent, Main.rand.NextFloat(0.5f));
            light.color = Color.Lerp(light.color, Color.Wheat, 0.6f);
            light.velocity = Main.rand.NextVector2Circular(3f, 3f);
            light.scale = 0.75f;
        }

        public void DrawWithShader(SpriteBatch spriteBatch)
        {
            // Collect the shader and draw data for later.
            var godRayShader = ShaderManager.GetShader("NoxusBoss.GodRayShader");
            Vector2 textureArea = new Vector2(Projectile.width, Height) / WhitePixel.Size();

            // Apply the god ray shader.
            godRayShader.TrySetParameter("noise1Zoom", 0.5f);
            godRayShader.TrySetParameter("noise2Zoom", 0.24f);
            godRayShader.TrySetParameter("edgeFadePower", 4f);
            godRayShader.TrySetParameter("edgeTaperDistance", 0.15f);
            godRayShader.TrySetParameter("animationSpeed", 0.2f);
            godRayShader.TrySetParameter("noiseOpacityPower", 2f);
            godRayShader.TrySetParameter("bottomBrightnessIntensity", 0.2f);
            godRayShader.TrySetParameter("colorAccent", ColorAccent.ToVector4() * Projectile.Opacity * 0.14f);
            godRayShader.SetTexture(TurbulentNoise, 1, SamplerState.LinearWrap);
            godRayShader.Apply();

            // Draw a large white rectangle based on the hitbox of the ray.
            // The shader will transform the rectangle into the ray.
            float brightnessFadeIn = InverseLerp(15f, NamelessDeitySummonDelayInCenter * 0.67f, TimeSpentInCenter);
            float brightnessFadeOut = InverseLerp(NamelessDeitySummonDelayInCenter - 4f, NamelessDeitySummonDelayInCenter - 16f, TimeSpentInCenter);
            float brightnessInterpolant = brightnessFadeIn * brightnessFadeOut;
            float brightness = Lerp(0.14f, 0.59f, InverseLerp(0f, 0.7f, brightnessInterpolant));
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(WhitePixel, drawPosition, null, Projectile.GetAlpha(MainColor) * brightness, Projectile.rotation, WhitePixel.Size() * new Vector2(0.5f, 1f), textureArea, 0, 0f);

            // Draw the vignette over the player's screen.
            if (brightnessInterpolant > 0.44f)
                DrawVignette(InverseLerp(0.44f, 1f, brightnessInterpolant));
        }

        public void DrawVignette(float brightnessInterpolant)
        {
            // Draw a pixel over the player's screen and then draw the vignette over it.
            var vignetteShader = ShaderManager.GetShader("NoxusBoss.FleshyVignetteShader");
            vignetteShader.TrySetParameter("animationSpeed", 0.05f);
            vignetteShader.TrySetParameter("vignettePower", Lerp(6f, 3.97f, brightnessInterpolant));
            vignetteShader.TrySetParameter("vignetteBrightness", Lerp(3f, 20f, brightnessInterpolant));
            vignetteShader.TrySetParameter("crackBrightness", Sqrt(brightnessInterpolant) * 0.95f);
            vignetteShader.TrySetParameter("aspectRatioCorrectionFactor", new Vector2(Main.screenWidth / (float)Main.screenHeight, 1f));
            vignetteShader.TrySetParameter("primaryColor", Color.White);
            vignetteShader.TrySetParameter("secondaryColor", Color.White);
            vignetteShader.TrySetParameter("radialOffsetTime", InverseLerp(30f, NamelessDeitySummonDelayInCenter, TimeSpentInCenter) * 1.2f);
            vignetteShader.SetTexture(CrackedNoise, 1);
            vignetteShader.Apply();

            Color vignetteColor = Projectile.GetAlpha(Color.Gray) * brightnessInterpolant * InverseLerp(800f, 308f, Distance(Projectile.Center.X, Main.LocalPlayer.Center.X)) * 0.67f;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 textureArea = screenArea / WhitePixel.Size();
            Main.spriteBatch.Draw(WhitePixel, screenArea * 0.5f, null, vignetteColor, 0f, WhitePixel.Size() * 0.5f, textureArea, 0, 0f);
        }

        // Manual drawing is not necessary.
        public override bool PreDraw(ref Color lightColor) => false;

        // This visual should not move.
        public override bool ShouldUpdatePosition() => false;
    }
}
