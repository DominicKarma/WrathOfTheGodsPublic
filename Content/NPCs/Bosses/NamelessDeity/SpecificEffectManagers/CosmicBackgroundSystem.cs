using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class CosmicBackgroundSystem : ModSystem
    {
        public static float StarZoomIncrement
        {
            get;
            set;
        }

        public static bool HasLoaded
        {
            get;
            private set;
        }

        public static ManagedRenderTarget KalisetFractal
        {
            get;
            internal set;
        }

        // Load is used instead of OnModLoad to ensure that the texture generation happens as soon as possible.
        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(PrepareTarget);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        internal static void PrepareTarget()
        {
            int width = 3072;
            int height = 3072;
            int iterations = 14;

            // This is stored as a render target and not a PNG in the mod's source because the fractal needs to contain information that exceeds the traditional range of 0-1 color values.
            // It could theoretically be loaded into a binary file in some way but at that point you're going to need to translate it into some GPU-friendly object, like a render target.
            // It's easiest to just create it dynamically here.
            // There are a LOT of calculations needed to generate the entire texture though, hence the usage of background threads.
            KalisetFractal = new(false, (_, _2) =>
            {
                return new(Main.instance.GraphicsDevice, width, height, true, SurfaceFormat.Single, DepthFormat.Depth16, 2, RenderTargetUsage.PreserveContents);
            });

            // This number is highly important to the resulting structure of the fractal, and is very sensitive (as is typically the case with objects from Chaos Theory).
            // Many numbers will give no fractal at all, pure white, or pure black. But tweaking it to be just right gives some amazing patterns.
            // Feel free to tweak this if you want to see what it does to the texture.
            float julia = 0.584f;

            new Thread(_ =>
            {
                float[] kalisetData = new float[width * height];

                // Evolve the system based on the Kaliset.
                // Over time it will achieve very, very chaotic behavior similar to a fractal and as such is incredibly reliable for
                // getting pseudo-random change over time.
                for (int i = 0; i < width * height; i++)
                {
                    int x = i % width;
                    int y = i / width;
                    float previousDistance = 0f;
                    float totalChange = 0f;
                    Vector2 p = new(x / (float)width - 0.5f, y / (float)height - 0.5f);

                    // Repeat the iterative function of 'abs(z) / dot(z) - c' multiple times to generate the fractal patterns.
                    // The higher the amount of iterations, the greater amount of detail. Do note that too much detail can lead to grainy artifacts
                    // due to individual pixels being unusually bright next to their neighbors, as the fractal inevitably becomes so detailed that the
                    // texture cannot encompass all of its features.
                    for (int j = 0; j < iterations; j++)
                    {
                        p = new Vector2(Math.Abs(p.X), Math.Abs(p.Y)) / Vector2.Dot(p, p);
                        p.X -= julia;
                        p.Y -= julia;

                        float distance = p.Length();
                        totalChange += Math.Abs(distance - previousDistance);
                        previousDistance = distance;
                    }

                    // Sometimes the results of the above iterative process will send the distance so far off that the numbers explode into the NaN or Infinity range.
                    // The GPU won't know what to do with this and will just act like it's a black pixel, which we don't want.
                    // As such, this check exists to put a hard limit on the values sent into the fractal texture. Something beyond 1000 shouldn't be making a difference anyway.
                    // At that point the pixel it spits out from the shader should be a pure white.
                    if (float.IsNaN(totalChange) || float.IsInfinity(totalChange) || totalChange >= 1000f)
                        totalChange = 1000f;

                    kalisetData[i] = totalChange;
                }

                Main.QueueMainThreadAction(() => KalisetFractal.Target.SetData(kalisetData));
                HasLoaded = true;
            }).Start();
        }

        public static void Draw(float intensity)
        {
            if (intensity <= 0f)
                return;

            float areaComponent = MathF.Max(Main.instance.GraphicsDevice.DisplayMode.Width, Main.instance.GraphicsDevice.DisplayMode.Height);
            Vector2 screenArea = Vector2.One * areaComponent;
            Vector2 scale = screenArea / TextureAssets.MagicPixel.Value.Size();

            Color frontStarColor = Color.Lerp(Color.Coral, Color.White, DifferentStarsInterpolant);
            Color backStarColor = Color.Lerp(Color.Yellow, Color.Pink, DifferentStarsInterpolant);
            if (Main.LocalPlayer.name.Equals("Moonburn", StringComparison.OrdinalIgnoreCase))
            {
                frontStarColor = Color.Lerp(new(109, 244, 234), Color.Coral, DifferentStarsInterpolant);
                backStarColor = Color.Lerp(Color.Magenta.HueShift(Sin01(Main.GlobalTimeWrappedHourly * 5f) * 0.07f) * 0.8f, Color.Yellow, DifferentStarsInterpolant);
            }

            var cosmicShader = ShaderManager.GetShader("NoxusBoss.CosmicBackgroundShader");
            cosmicShader.TrySetParameter("zoom", StarZoomIncrement + 0.12f);
            cosmicShader.TrySetParameter("brightness", intensity);
            cosmicShader.TrySetParameter("scrollSpeedFactor", 0.0015f);
            cosmicShader.TrySetParameter("frontStarColor", frontStarColor.ToVector3() * 0.5f);
            cosmicShader.TrySetParameter("backStarColor", backStarColor.ToVector3() * 0.4f);
            cosmicShader.TrySetParameter("colorChangeInfluence1", new Vector3(-2.1f, 0.5f, 2.15f)); // Adds cyan.
            cosmicShader.TrySetParameter("colorChangeInfluence2", new Vector3(2.4f, -0.9f, 5.8f)); // Adds violets.
            cosmicShader.TrySetParameter("colorChangeStrength1", 0.6f);
            cosmicShader.TrySetParameter("colorChangeStrength2", 0.81f);
            cosmicShader.TrySetParameter("detailIterations", (int)Lerp(10f, 19f, Pow(NoxusBossConfig.Instance.VisualOverlayIntensity, 3f)));
            cosmicShader.SetTexture(KalisetFractal, 1, SamplerState.AnisotropicWrap);
            cosmicShader.SetTexture(TurbulentNoise, 2, SamplerState.AnisotropicWrap);
            cosmicShader.Apply();

            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, screenArea * 0.5f, null, Color.White * 0.35f, 0f, TextureAssets.MagicPixel.Value.Size() * 0.5f, scale, 0, 0f);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}
