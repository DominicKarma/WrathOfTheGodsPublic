using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class DivineRoseSystem : ModSystem
    {
        public class Galaxy
        {
            public int Variant;

            public int Time;

            public int Lifetime;

            public float Eccentricity;

            public float Rotation;

            public float SpinRotation;

            public float GeneralScale;

            public Color GeneralColor;

            public Vector2 Center;

            public Vector2 Velocity;

            public float LifetimeCompletion => Time / (float)Lifetime;

            public float Scale => InverseLerp(0f, 6f, Time) * Lerp(1f, 1.5f, LifetimeCompletion) * GeneralScale;

            public float Opacity => InverseLerp(0f, 9f, Time) * InverseLerp(1f, 0.4f, Pow(LifetimeCompletion, 0.6f));

            public void Update()
            {
                Time++;
                Center += Velocity;
                Velocity = (Velocity * 0.97f).ClampLength(0f, 40f);
                Rotation += Velocity.X * 0.003f;
                SpinRotation += Velocity.X * 0.04f;
            }
        }

        public static Asset<Texture2D> RoseTexture
        {
            get;
            internal set;
        }

        public static int FrameIncrementRate => 8;

        public static int FrameCount => 26;

        public static int CensorStartingFrame => 14;

        public static int BlackOverlayStartTime => FrameIncrementRate * CensorStartingFrame;

        public static int AttackDelay => FrameIncrementRate * FrameCount + 54;

        public static int ExplosionDelay => AttackDelay + 30;

        public static float NamelessDeityZPosition => 7.5f;

        public static Vector2 RoseOffsetFromScreenCenter => -Vector2.UnitY * 210f;

        public static Vector2 BaseCensorOffset => -Vector2.UnitY * 100f;

        public static readonly List<Galaxy> ActiveGalaxies = new();

        public override void OnModLoad()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            RoseTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/SpecificEffectManagers/DivineRose");
        }

        public static List<Vector2> GenerateGodRayPositions(Vector2 roseDrawPosition, Vector2 maxLength)
        {
            List<Vector2> godRayPositions = new();
            for (int i = 0; i < 20; i++)
                godRayPositions.Add(roseDrawPosition + maxLength * i / 19f);

            return godRayPositions;
        }

        public static void Draw(int time)
        {
            // Calculate draw variables.
            float scale = Remap(time, -1f, 75f, 0.01f, 1.6f);

            // Draw the rose.
            float generalOpacity = InverseLerp(1f, NamelessDeityZPosition - 1f, NamelessDeityBoss.Myself.As<NamelessDeityBoss>().ZPosition);
            Vector2 roseDrawPosition = Main.ScreenSize.ToVector2() * 0.5f + RoseOffsetFromScreenCenter;
            DrawRose(time, generalOpacity, scale, roseDrawPosition);

            // Draw the glow effect when ready.
            float glowAnimationCompletion = InverseLerp(BlackOverlayStartTime, AttackDelay - 20f, time);
            if (glowAnimationCompletion > 0f && glowAnimationCompletion < 1f)
            {
                if (time == BlackOverlayStartTime + 1 && !Main.gamePaused)
                    SoundEngine.PlaySound(NamelessDeityBoss.StarConvergenceFastSound);

                // Draw the colored bloom flares.
                float bloomFlareRotation = SmoothStep(0f, Pi, glowAnimationCompletion);
                float bloomFlareOpacity = Convert01To010(glowAnimationCompletion) * generalOpacity;
                for (int i = 0; i < 6; i++)
                {
                    Color bloomFlareColor = Main.hslToRgb((i / 6f + glowAnimationCompletion * 0.84f) % 1f, 0.9f, 0.56f) * bloomFlareOpacity;
                    bloomFlareColor.A = 0;
                    Main.spriteBatch.Draw(BloomFlare, roseDrawPosition + BaseCensorOffset, null, bloomFlareColor, bloomFlareRotation + TwoPi * i / 6f, BloomFlare.Size() * 0.5f, bloomFlareOpacity * 0.64f + i * 0.04f, 0, 0f);
                }

                // Draw the bloom glow.
                Main.spriteBatch.Draw(BloomCircle, roseDrawPosition + BaseCensorOffset, null, (Color.Wheat with { A = 0 }) * bloomFlareOpacity * 0.4f, bloomFlareRotation, BloomCircle.Size() * 0.5f, bloomFlareOpacity * 0.2f, 0, 0f);
                Main.spriteBatch.Draw(BloomCircle, roseDrawPosition + BaseCensorOffset, null, (Color.IndianRed with { A = 0 }) * bloomFlareOpacity * 0.1f, bloomFlareRotation, BloomCircle.Size() * 0.5f, bloomFlareOpacity * 0.5f, 0, 0f);
            }

            // Summon galaxies upward if the explosion has happened.
            if (time >= ExplosionDelay && generalOpacity >= 1f)
            {
                Color galaxyColor = MulticolorLerp(Pow(Main.rand.NextFloat(), 2f) * 0.94f, Color.OrangeRed, Color.Coral, Color.HotPink, Color.Magenta, Color.DarkViolet, Color.Cyan, Color.White) * 1.9f;
                galaxyColor = Color.Lerp(galaxyColor, Color.Wheat, 0.55f);

                Vector2 galaxyVelocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(11f, 25f) + Main.rand.NextVector2Circular(5f, 5f);
                int galaxyLifetime = (int)Remap(galaxyVelocity.Length(), 10f, 19.2f, 40f, 70f) + Main.rand.Next(-12, 45);
                float galaxyScale = Remap(galaxyVelocity.Length(), 9.5f, 17.4f, 0.12f, 0.4f) + Pow(Main.rand.NextFloat(), 3f) * 0.8f;

                ActiveGalaxies.Add(new()
                {
                    Center = roseDrawPosition + BaseCensorOffset,
                    Velocity = galaxyVelocity,
                    GeneralColor = galaxyColor,
                    Lifetime = galaxyLifetime,
                    Rotation = Main.rand.NextFloat(TwoPi),
                    GeneralScale = galaxyScale,
                    Eccentricity = Main.rand.NextFloat(),
                    Variant = Main.rand.Next(9)
                });
            }

            // Draw galaxies, along with a pulsating white behind the censor.
            if (time >= ExplosionDelay && ActiveGalaxies.Any())
            {
                // Draw the white pulse.
                float pulseScale = Lerp(2.2f, 2.6f, Sin01(Main.GlobalTimeWrappedHourly * 55f)) * 0.64f;
                for (int i = 0; i < 4; i++)
                {
                    Main.spriteBatch.Draw(BloomCircleSmall, roseDrawPosition + BaseCensorOffset, null, (Color.Wheat with { A = 0 }) * generalOpacity, 0f, BloomCircleSmall.Size() * 0.5f, pulseScale, 0, 0f);
                    Main.spriteBatch.Draw(BloomCircleSmall, roseDrawPosition + BaseCensorOffset, null, (Color.Wheat with { A = 0 }) * generalOpacity * 0.7f, 0f, BloomCircleSmall.Size() * 0.5f, pulseScale * 1.5f, 0, 0f);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
                DrawGalaxies();
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
            }

            // Draw the censor over the rose when ready.
            if (time >= BlackOverlayStartTime)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());

                // Apply a static shader.
                var censorShader = ShaderManager.GetShader("StaticOverlayShader");
                censorShader.SetTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/noise"), 1, SamplerState.PointWrap);
                censorShader.Apply();

                ulong offsetSeed = (ulong)(time / 4) << 10;
                float offsetAngle = Utils.RandomFloat(ref offsetSeed) * 1000f;
                Vector2 censorOffset = BaseCensorOffset + offsetAngle.ToRotationVector2() * 4f;

                Main.spriteBatch.Draw(WhitePixel, roseDrawPosition + censorOffset, null, Color.Black * generalOpacity, 0f, WhitePixel.Size() * 0.5f, scale * 100f, 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
            }
        }

        public static void DrawRose(int time, float generalOpacity, float scale, Vector2 roseDrawPosition)
        {
            Texture2D rose = RoseTexture.Value;
            int frameY = Utils.Clamp(time / FrameIncrementRate, 0, FrameCount - 1);
            Rectangle frame = rose.Frame(1, FrameCount, 0, frameY);
            Vector2 origin = frame.Size() * 0.5f;
            Color roseColor = Color.Lerp(Color.DarkGray, Color.White, frameY / (float)FrameCount) * generalOpacity;

            // Draw the rose with a cartoon shader.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
            var cartoonShader = ShaderManager.GetShader("CelShader");
            cartoonShader.TrySetParameter("pixelationFactor", Vector2.One * 1.75f / rose.Size());
            cartoonShader.TrySetParameter("textureSize", rose.Size());
            cartoonShader.TrySetParameter("horizontalEdgeKernel", new Matrix()
            {
                M11 = 1f,
                M12 = 0f,
                M13 = -1f,
                M21 = 2f,
                M22 = 0f,
                M23 = -2f,
                M31 = 1f,
                M32 = 0f,
                M33 = -1f
            });
            cartoonShader.TrySetParameter("verticalEdgeKernel", new Matrix()
            {
                M11 = 1f,
                M12 = 2f,
                M13 = 1f,
                M21 = 0f,
                M22 = 0f,
                M23 = 0f,
                M31 = -1f,
                M32 = -2f,
                M33 = -1f
            });
            cartoonShader.Apply();

            Main.spriteBatch.Draw(rose, roseDrawPosition, frame, roseColor, 0.14f, origin, scale, 0, 0f);
            if (time >= ExplosionDelay)
            {
                float rosePulse = Main.GlobalTimeWrappedHourly * 5f % 1f;
                Main.spriteBatch.Draw(rose, roseDrawPosition, frame, roseColor * (1f - rosePulse), 0.14f, origin, scale * (1f + rosePulse * 0.2f), 0, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
        }

        public static void DrawGalaxies()
        {
            var galaxyShader = ShaderManager.GetShader("GalaxyShader");

            ActiveGalaxies.RemoveAll(g => g.Time >= g.Lifetime);
            foreach (Galaxy g in ActiveGalaxies)
            {
                g.Update();

                Matrix flatScale = new()
                {
                    M11 = Lerp(1f, 2.3f, g.Eccentricity),
                    M12 = 0f,
                    M21 = 0f,
                    M22 = 1f
                };
                Matrix spinInPlaceRotation = Matrix.CreateRotationZ(g.SpinRotation);
                Matrix orientationRotation = Matrix.CreateRotationZ(g.Rotation);
                galaxyShader.TrySetParameter("transformation", orientationRotation * flatScale * spinInPlaceRotation);
                galaxyShader.Apply();

                // Draw the main galaxy.
                float galaxyScale = g.Scale * 1.3f + Pow(g.LifetimeCompletion, 1.7f) * 19f;
                Texture2D galaxyTexture = GalaxyTextures[g.Variant];
                Vector2 galaxyDrawPosition = g.Center;
                Main.spriteBatch.Draw(galaxyTexture, galaxyDrawPosition, null, g.GeneralColor * g.Opacity, 0f, galaxyTexture.Size() * 0.5f, galaxyScale, 0, 0f);

                // Draw a secondary galaxy on top with some color contrast.
                Color secondaryGalaxyColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.6f + g.LifetimeCompletion * 0.3f) % 1f, 0.8f, 0.6f) * 0.4f;
                Main.spriteBatch.Draw(galaxyTexture, galaxyDrawPosition, null, secondaryGalaxyColor * g.Opacity * 0.6f, 0f, galaxyTexture.Size() * 0.5f, galaxyScale * 1.1f, 0, 0f);
            }
        }
    }
}
