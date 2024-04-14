using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeityScarySkyManager : ModSystem
    {
        public enum SkyVariant
        {
            FractalEye,
            DistortedTitleScreen,
            CirclingHands,
            JuliaFractal,
            NamelessDeityJumpscare,

            LeakingSourceCodeLmao,
            OgsculeanSojourn
        }

        public static bool IsActive => FlashBrightness > 0f;

        public static float FlashBrightness
        {
            get;
            private set;
        }

        public static ulong Seed
        {
            get;
            private set;
        }

        public static SkyVariant Variant
        {
            get;
            set;
        }

        public static Asset<Texture2D> Omniscience
        {
            get;
            private set;
        }

        public static Asset<Texture2D> TitleScreen
        {
            get;
            private set;
        }

        public static Asset<Texture2D> SourceCode
        {
            get;
            private set;
        }

        public const float MaxBrightness = 1.43f;

        public static readonly SoundStyle FlashSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/NamelessDeity/ScaryFlash", 6) with { MaxInstances = 5 };

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Load and cache textures.
            string directory = "NoxusBoss/Content/NPCs/Bosses/NamelessDeity/ScaryBackgroundElements";
            Omniscience = ModContent.Request<Texture2D>($"{directory}/Omniscience");
            TitleScreen = ModContent.Request<Texture2D>($"{directory}/TitleScreen");
            SourceCode = ModContent.Request<Texture2D>($"{directory}/SourceCode");
        }

        public static void DrawFractalEye(ulong seed, float localBrightness)
        {
            // This explicit integer cast won't throw any errors if the seed is greater than int.MaxValue, it'll just truncate the excess bytes.
            // For most purposes this is unideal due to unexpected behavior, but in this case the exact values of the numbers don't actually matter, just that they're valid for
            // converting to pseudorandom values.
            SpriteEffects direction = ((int)Abs((int)seed % 2)).ToSpriteDirection();
            Vector2 screenCenter = Main.ScreenSize.ToVector2() * 0.5f;
            Vector2 eyeDrawPosition = screenCenter + (Utils.RandomFloat(ref seed) * TwoPi).ToRotationVector2() * Utils.RandomFloat(ref seed) * 120f;

            // Calculate the eye draw color.
            Color eyeColor = Color.White * localBrightness;
            eyeColor.A = 0;

            // Draw the eye.
            Texture2D eyeTexture = Omniscience.Value;
            Main.spriteBatch.Draw(eyeTexture, eyeDrawPosition, null, eyeColor, 0f, eyeTexture.Size() * 0.5f, 2.7f, direction, 0f);
        }

        public static void DrawDistortedTitleScreen(float localBrightness)
        {
            // Apply the glitch shader.
            float lightBrightness = InverseLerp(0.6f, 0.85f, FlashBrightness);
            float glitchIntensity = InverseLerp(0.54f, 0.9f, FlashBrightness);
            var glitchShader = ShaderManager.GetShader("NoxusBoss.GlitchShader");
            glitchShader.SetTexture(SharpNoise, 1, SamplerState.AnisotropicWrap);
            glitchShader.TrySetParameter("coordinateZoomFactor", Vector2.One * 10.5f);
            glitchShader.TrySetParameter("glitchInterpolant", glitchIntensity);
            glitchShader.Apply();

            // Calculate draw information.
            Texture2D titleTexture = TitleScreen.Value;
            Vector2 screenSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);

            Color screenColor = Color.White * localBrightness * (1f - lightBrightness);
            Color lightColor = Color.White * lightBrightness * 0.9f;

            // Draw the title screen.
            Main.spriteBatch.Draw(titleTexture, screenSize * 0.5f, null, screenColor, 0f, titleTexture.Size() * 0.5f, screenSize / titleTexture.Size(), 0, 0f);

            // Draw the original light as the effect appears.
            Texture2D originalLight = NamelessDeityDimensionSkyGenerator.OriginalLightBackgroundTexture.Value;
            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(originalLight, screenSize * 0.5f + Main.rand.NextVector2Circular(4f, 4f), null, lightColor, 0f, originalLight.Size() * 0.5f, screenSize / originalLight.Size() * 1.2f, 0, 0f);
        }

        public static void DrawCirclingHands(ulong seed, float localBrightness)
        {
            // Collect general information first.
            float maxRadius = 1450f;
            float generalScale = 1.45f;
            Texture2D handTexture = NamelessDeityBoss.Myself.As<NamelessDeityBoss>().HandTexture.UsedTexture;
            Rectangle handFrame = handTexture.Frame(1, 3, 0, 0);
            Color handColor = Color.White * localBrightness * 0.8f;
            Vector2 screenCenter = Main.ScreenSize.ToVector2() * 0.5f;

            for (float radius = maxRadius; radius >= 204f; radius -= Lerp(280f, 500f, Utils.RandomFloat(ref seed)))
            {
                // Calculate the scale of the hand for the given ring. Hands closer to the center are smaller, hands further out are bigger.
                float handScale = 0.7f + radius * 0.00105f;

                // Calculate the elliptical factors of the ring.
                Vector2 ellipseFactor = new(1f, Lerp(0.56f, 0.97f, radius / maxRadius));
                float semiMajorAxis = MathF.Max(ellipseFactor.X, ellipseFactor.Y);
                float semiMinorAxis = MathF.Min(ellipseFactor.X, ellipseFactor.Y);

                // Calculate how high a hand extends with the aforementioned scale.
                // This is used for calculating how many hands should be drawn on the ring.
                float handHeight = handScale * handFrame.Height;

                // Calculate the perimeter of the ellipse.
                // Interestingly, getting the exact value for this involves some form of infinite series or elliptic integrals (which are a pain to numerically calculate).
                // For the purposes of my needs, it's fine to just use an extremely good approximation instead. In this case, I'm using one discovered by Ramanujan.
                // Its margin of error should not exceeed ~0.00001, given that the axes won't be extremely varied.
                // The details of such can be found at this video:
                // https://www.youtube.com/watch?v=5nW3nJhBHL0
                float h = Pow(semiMajorAxis - semiMinorAxis, 2f) / Pow(semiMajorAxis + semiMinorAxis, 2f);
                float perimeter = Pi * radius * (semiMajorAxis + semiMinorAxis) * (1f + h * 3f / (Sqrt(4f - h * 3f) + 10f));

                // Use the perimeter and divide it based on how much distance each hand takes up to determine how many hands should be drawn.
                // This is artficially decreased slightly so that there's some space between each hand.
                int handCount = (int)(perimeter / handHeight * 0.8f);

                // Lastly, calculate the angular offset for the entire ring. This determines how fast the hands will spin.
                float ringSpin = Main.GlobalTimeWrappedHourly * Lerp(2.4f, 0.5f, radius / maxRadius);
                for (int i = 0; i < handCount; i++)
                {
                    float handOffsetAngle = TwoPi * i / handCount + ringSpin;
                    Vector2 handOffset = Utils.Vector2FromElipse(handOffsetAngle.ToRotationVector2(), ellipseFactor * generalScale * radius);
                    Main.spriteBatch.Draw(handTexture, screenCenter + handOffset, handFrame, handColor, handOffsetAngle, handFrame.Size() * 0.5f, generalScale * handScale, 0, 0f);
                }
            }

            // Draw the eye behind everything.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());

            for (int i = 0; i < 7; i++)
            {
                Main.spriteBatch.Draw(BloomCircle, screenCenter, null, new Color(1f, 0.82f, 0.687f) * localBrightness * 0.67f, 0f, BloomCircle.Size() * 0.5f, generalScale * 3f, 0, 0f);
                Main.spriteBatch.Draw(BloomFlare, screenCenter, null, new Color(1f, 0.43f, 0.59f) * localBrightness * 0.48f, Main.GlobalTimeWrappedHourly * 0.5f, BloomFlare.Size() * 0.5f, generalScale * 1.75f, 0, 0f);
                Main.spriteBatch.Draw(BloomFlare, screenCenter, null, new Color(0.99f, 0.48f, 0.42f) * localBrightness * 0.4f, Main.GlobalTimeWrappedHourly * -0.61f, BloomFlare.Size() * 0.5f, generalScale * 1.99f, 0, 0f);
                Main.spriteBatch.Draw(NamelessDeityBoss.EyeFullTexture.Value, screenCenter, null, new Color(1f, 1f, 1f) * localBrightness, 0f, NamelessDeityBoss.EyeFullTexture.Size() * 0.5f, generalScale * 0.2889f, 0, 0f);
            }
        }

        public static void DrawJuliaFractal(ulong seed, float localBrightness)
        {
            Vector2 screenSize = Main.ScreenSize.ToVector2();
            Vector2 screenCenter = screenSize * 0.5f;

            // Draw the original light behind everything.
            Texture2D originalLight = NamelessDeityDimensionSkyGenerator.OriginalLightBackgroundTexture.Value;
            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(originalLight, screenCenter + Main.rand.NextVector2Circular(4f, 4f), null, Color.White * localBrightness * 0.6f, 0f, originalLight.Size() * 0.5f, screenSize / originalLight.Size() * 1.1f, 0, 0f);

            // Prepare the shader.
            var shader = ShaderManager.GetShader("NoxusBoss.JuliaSetFractalShader");
            shader.TrySetParameter("animationTime", Clamp(1f - FlashBrightness / MaxBrightness, 0f, 1f) * 2f);
            shader.Apply();

            // Draw the fractal.
            for (int i = 0; i < 2; i++)
            {
                float rotation = Utils.RandomFloat(ref seed) * TwoPi;
                float size = Lerp(1300f, 2100f, Utils.RandomFloat(ref seed));
                Vector2 drawOffset = Vector2.UnitX * Lerp(-205f, 205f, Utils.RandomFloat(ref seed));
                Main.spriteBatch.Draw(WhitePixel, screenCenter + drawOffset, null, Color.White * Cbrt(localBrightness), rotation, WhitePixel.Size() * 0.5f, size, 0, 0f);
            }
        }

        public static void DrawSourceCode(float localBrightness)
        {
            Texture2D code = SourceCode.Value;
            Vector2 codeScale = Vector2.One * MathF.Min(Main.screenWidth, Main.screenHeight) / code.Size() * 1.05f;
            Main.spriteBatch.Draw(code, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White * localBrightness * 0.93f, 0f, code.Size() * 0.5f, codeScale, 0, 0f);
        }

        public static void DrawOgsculeanSojourn(float localBrightness)
        {
            Texture2D ogscule = OgsculeOverlaySystem.OgsculeTexture.Value;
            Vector2 ogsculeScale = Main.ScreenSize.ToVector2() / ogscule.Size();
            Main.spriteBatch.Draw(ogscule, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White * localBrightness * 0.93f, 0f, ogscule.Size() * 0.5f, ogsculeScale, 0, 0f);
        }

        public static void Draw()
        {
            // Do nothing if the effect is not active.
            if (!IsActive)
                return;

            // Make the flash brightness quickly dissipate at first so the initial burst doesn't last long.
            // After this, it takes a bit more time to go away.
            if (!Main.gamePaused)
            {
                if (FlashBrightness > 1f)
                    FlashBrightness = Clamp(FlashBrightness * 0.936f, 1f, 10f);
                else
                    FlashBrightness = Clamp(FlashBrightness - 0.016f, 0f, 1f);
            }

            // Alter the sprite batch if the sky variant needs that.
            bool spriteBatchChanged = AlterSpriteBatchIfNecessary();

            // Draw sky layers. This is done in such a way brightness values above 1 are accounted for via additive blending across the various layers.
            float layeredBrightness = FlashBrightness;
            while (layeredBrightness > 0f)
            {
                DrawLayer(Clamp(layeredBrightness, 0f, 1f));
                layeredBrightness--;
            }

            // Reset the sprite batch if it was changed.
            if (spriteBatchChanged)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
            }
        }

        public static void DrawLayer(float localBrightness)
        {
            // Copy the seed so that RNG methods that use ref arguments don't alter the state of everything else.
            // Technically just passing in Seed itself would work since ulongs are structs, but this helps elucidate the reasoning.
            ulong seedCopy = Seed;

            switch (Variant)
            {
                case SkyVariant.FractalEye:
                    DrawFractalEye(seedCopy, localBrightness);
                    break;
                case SkyVariant.DistortedTitleScreen:
                    DrawDistortedTitleScreen(localBrightness);
                    break;
                case SkyVariant.CirclingHands:
                    DrawCirclingHands(seedCopy, localBrightness);
                    break;
                case SkyVariant.JuliaFractal:
                    DrawJuliaFractal(seedCopy, localBrightness);
                    break;
                case SkyVariant.LeakingSourceCodeLmao:
                    DrawSourceCode(localBrightness);
                    break;
                case SkyVariant.OgsculeanSojourn:
                    DrawOgsculeanSojourn(localBrightness);
                    break;

                // Nothing explicitly happens here. Nameless does all the drawing on his own.
                case SkyVariant.NamelessDeityJumpscare:
                    break;
            }
        }

        public static bool AlterSpriteBatchIfNecessary()
        {
            switch (Variant)
            {
                // Shaders are necessary for the static overlay.
                case SkyVariant.DistortedTitleScreen:
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
                    return true;
                // Shaders are necessary for the fractal shader.
                case SkyVariant.JuliaFractal:
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
                    return true;
                case SkyVariant.CirclingHands:
                    return true;
            }

            return false;
        }

        public static void Start()
        {
            // Play a flash sound.
            SoundEngine.PlaySound(FlashSound);

            // Pick a new seed.
            Seed = (ulong)Main.rand.Next(int.MaxValue);

            // Define the variant RNG.
            WeightedRandom<SkyVariant> variantSelector = new(Main.rand.Next());
            variantSelector.Add(SkyVariant.FractalEye, 1D);
            variantSelector.Add(SkyVariant.DistortedTitleScreen, 1D);
            variantSelector.Add(SkyVariant.CirclingHands, 1D);
            variantSelector.Add(SkyVariant.JuliaFractal, 1D);
            variantSelector.Add(SkyVariant.NamelessDeityJumpscare, 1D);
            variantSelector.Add(SkyVariant.LeakingSourceCodeLmao, 0.01);
            variantSelector.Add(SkyVariant.OgsculeanSojourn, 0.002);

            // Pick a new sky variant. This will always be different than the previous one, to ensure variety.
            SkyVariant oldVariant = Variant;
            do
                Variant = variantSelector.Get();
            while (Variant == oldVariant);

            // Create the flash.
            FlashBrightness = MaxBrightness;
        }
    }
}
