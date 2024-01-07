using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Subworlds;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeitySkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NamelessDeitySky.SkyIntensityOverride > 0f || NPC.AnyNPCs(ModContent.NPCType<NamelessDeityBoss>());

        public override void Load()
        {
            On_Main.DrawSunAndMoon += NoMoonInGarden;
            On_Main.DrawBackground += NoBackgroundDuringNamelessDeityFight;
            On_Main.DrawSurfaceBG += NoBackgroundDuringNamelessDeityFight2;
        }

        private void NoMoonInGarden(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            // The moon does not exist in the garden subworld, because it is not the base Terraria world.
            if (!EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                orig(self, sceneArea, moonColor * Pow(1f - NamelessDeitySky.SkyEyeOpacity, 2f), sunColor, tempMushroomInfluence);
        }

        private void NoBackgroundDuringNamelessDeityFight(On_Main.orig_DrawBackground orig, Main self)
        {
            if (NamelessDeitySky.HeavenlyBackgroundIntensity < 0.3f)
                orig(self);
        }

        private void NoBackgroundDuringNamelessDeityFight2(On_Main.orig_DrawSurfaceBG orig, Main self)
        {
            if (NamelessDeitySky.HeavenlyBackgroundIntensity < 0.3f)
                orig(self);
            else
            {
                SkyManager.Instance.ResetDepthTracker();
                SkyManager.Instance.DrawToDepth(Main.spriteBatch, 1f / 0.12f);
                if (!Main.mapFullscreen)
                    SkyManager.Instance.DrawRemainingDepth(Main.spriteBatch);
            }
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:NamelessDeitySky", isActive);

            if (!isActive)
                NamelessDeitySky.GraduallyResetEverything();
        }
    }

    public class NamelessDeitySky : CustomSky
    {
        public class BackgroundSmoke
        {
            public int Time;

            public int Lifetime;

            public float Rotation;

            public Vector2 DrawPosition;

            public Vector2 Velocity;

            public Color SmokeColor;

            public void Update()
            {
                Time++;
                DrawPosition += Velocity;
                Velocity *= 0.983f;
                SmokeColor *= 0.997f;
                Rotation += Velocity.X * 0.01f;
            }
        }

        public static bool IsEffectActive
        {
            get;
            private set;
        }

        public static float Intensity
        {
            get;
            private set;
        }

        public static float StarRecedeInterpolant
        {
            get;
            set;
        }

        public static float SkyIntensityOverride
        {
            get;
            set;
        }

        public static float SkyEyeOpacity
        {
            get;
            set;
        }

        public static float SkyEyeScale
        {
            get;
            set;
        } = 1f;

        public static float SkyPupilScale
        {
            get;
            set;
        } = 1f;

        public static float KaleidoscopeInterpolant
        {
            get;
            set;
        }

        public static float BlackOverlayInterpolant
        {
            get;
            set;
        }

        public static float SkyDistortionBrightnessIntensity
        {
            get;
            set;
        }

        public static Vector2 SkyPupilOffset
        {
            get;
            set;
        }

        public static float SeamScale
        {
            get;
            set;
        }

        public static float HeavenlyBackgroundIntensity
        {
            get;
            set;
        }

        public static float ManualSunScale
        {
            get;
            set;
        } = 1f;

        public static float DifferentStarsInterpolant
        {
            get;
            set;
        }

        // Used during the GFB glock attack. It's awesome.
        public static float UnitedStatesFlagOpacity
        {
            get;
            set;
        }

        public static List<BackgroundSmoke> SmokeParticles
        {
            get;
            private set;
        } = new();

        public static TimeSpan DrawCooldown
        {
            get;
            set;
        }

        public static TimeSpan LastFrameElapsedGameTime
        {
            get;
            set;
        }

        public static float SeamAngle => 1.67f;

        public static float SeamSlope => Tan(-SeamAngle);

        public static Asset<Texture2D> UnitedStatesFlagTexture
        {
            get;
            private set;
        }

        public override void Update(GameTime gameTime)
        {
            LastFrameElapsedGameTime = gameTime.ElapsedGameTime;

            // Make the intensity go up or down based on whether the sky is in use.
            Intensity = Clamp(Intensity + IsEffectActive.ToDirectionInt() * 0.01f, 0f, 1f);

            // Make the star recede interpolant go up or down based on how strong the intensity is. If the intensity is at its maximum the effect is uninterrupted.
            StarRecedeInterpolant = Clamp(StarRecedeInterpolant - (1f - Intensity) * 0.11f, 0f, 1f);

            // Disable ambient sky objects like wyverns and eyes appearing in front of the background.
            if (IsEffectActive)
                SkyManager.Instance["Ambience"].Deactivate();

            if (!NamelessDeityDimensionSkyGenerator.InProximityOfDivineMonolith)
                SkyIntensityOverride = Clamp(SkyIntensityOverride - 0.07f, 0f, 1f);
            if (Intensity < 1f)
                SkyEyeOpacity = Clamp(SkyEyeOpacity - 0.02f, 0f, Intensity + 0.001f);

            float minKaleidoscopeInterpolant = 0f;
            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentPhase >= 2)
            {
                var namelessAIState = NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState;
                if (namelessAIState is NamelessDeityBoss.NamelessAIType.SuperCosmicLaserbeam or NamelessDeityBoss.NamelessAIType.MomentOfCreation)
                    minKaleidoscopeInterpolant = 0f;
                else if (namelessAIState == NamelessDeityBoss.NamelessAIType.ClockConstellation)
                    minKaleidoscopeInterpolant = 0.7f;
                else
                    minKaleidoscopeInterpolant = 0.9f;
            }

            // Make a bunch of things return to their base values.
            UnitedStatesFlagOpacity = Clamp(UnitedStatesFlagOpacity - 0.01f, 0f, 1f);
            if (!Main.gamePaused && !IsEffectActive)
                GraduallyResetEverything(minKaleidoscopeInterpolant);
            else if (KaleidoscopeInterpolant < minKaleidoscopeInterpolant || (HeavenlyBackgroundIntensity < 1f && minKaleidoscopeInterpolant >= 0.01f))
            {
                KaleidoscopeInterpolant = minKaleidoscopeInterpolant;
                HeavenlyBackgroundIntensity = 1f;
            }

            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState != NamelessDeityBoss.NamelessAIType.DarknessWithLightSlashes)
                BlackOverlayInterpolant = Clamp(BlackOverlayInterpolant - 0.09f, 0f, 1f);

            // Make the eye disappear from the background if Nameless is already visible in the foreground.
            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.Opacity >= 0.3f)
                SkyEyeScale = 0f;

            if (!IsEffectActive)
                SeamScale = 0f;
        }

        public static void GraduallyResetEverything(float minKaleidoscopeInterpolant = 0f)
        {
            SkyPupilOffset = Utils.MoveTowards(Vector2.Lerp(SkyPupilOffset, Vector2.Zero, 0.03f), Vector2.Zero, 4f);
            SkyPupilScale = Lerp(SkyPupilScale, 1f, 0.05f);
            SkyEyeScale = Lerp(SkyEyeScale, 1f, 0.05f);
            SeamScale = Clamp(SeamScale * 0.87f - 0.023f, 0f, 300f);
            HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.02f, 0f, 2.5f);
            ManualSunScale = Clamp(ManualSunScale * 0.92f - 0.3f, 0f, 50f);
            DifferentStarsInterpolant = Clamp(DifferentStarsInterpolant - 0.1f, 0f, 1f);
            KaleidoscopeInterpolant = Clamp(KaleidoscopeInterpolant * 0.95f - 0.15f, minKaleidoscopeInterpolant, 1f);
        }

        public static void UpdateSmokeParticles()
        {
            // Randomly emit smoke.
            int smokeReleaseChance = 6;
            if (Main.rand.NextBool(smokeReleaseChance))
            {
                for (int i = 0; i < 4; i++)
                {
                    SmokeParticles.Add(new()
                    {
                        DrawPosition = new Vector2(Main.rand.NextFloat(-400f, Main.screenWidth + 400f), Main.screenHeight + 372f),
                        Velocity = -Vector2.UnitY * Main.rand.NextFloat(5f, 23f) + Main.rand.NextVector2Circular(3f, 3f),
                        SmokeColor = Color.Lerp(Color.Coral, Color.Wheat, Main.rand.NextFloat(0.5f, 0.85f)) * 0.9f,
                        Rotation = Main.rand.NextFloat(TwoPi),
                        Lifetime = Main.rand.Next(120, 480)
                    });
                }
            }

            // Update smoke particles.
            SmokeParticles.RemoveAll(s => s.Time >= s.Lifetime);
            foreach (BackgroundSmoke smoke in SmokeParticles)
                smoke.Update();
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, Color.White, Intensity * Lerp(0.4f, 1f, HeavenlyBackgroundIntensity) * 0.9f);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            // Ensure that the background only draws once per frame for efficiency.
            DrawCooldown -= LastFrameElapsedGameTime;
            if (minDepth >= -1000000f || (DrawCooldown.TotalMilliseconds >= 0.01 && Main.instance.IsActive))
                return;

            // Draw the sky background overlay, sun, and smoke.
            DrawCooldown = TimeSpan.FromSeconds(1D / 60D);
            if (maxDepth >= 0f && minDepth < 0f)
            {
                DrawStarDimension();

                // Draw the US flag if it's in use.
                Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
                if (UnitedStatesFlagOpacity > 0f)
                    DrawUnitedStatesFlag(screenArea);

                // Draw a black overlay if necessary.
                Main.spriteBatch.Draw(WhitePixel, screenArea * 0.5f, null, Color.Black * BlackOverlayInterpolant, 0f, WhitePixel.Size() * 0.5f, screenArea / WhitePixel.Size(), 0, 0f);

                // Draw the scary sky.
                NamelessDeityScarySkyManager.Draw();

                // Draw the divine rose.
                if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.MomentOfCreation)
                    DivineRoseSystem.Draw(NamelessDeityBoss.Myself.As<NamelessDeityBoss>().AttackTimer);
            }
        }

        public static void DrawUnitedStatesFlag(Vector2 screenArea)
        {
            UnitedStatesFlagTexture ??= ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/SpecificEffectManagers/UnitedStatesFlag");
            Texture2D flag = UnitedStatesFlagTexture.Value;
            Main.spriteBatch.Draw(flag, screenArea * 0.5f, null, Color.White * UnitedStatesFlagOpacity, 0f, flag.Size() * 0.5f, screenArea / flag.Size(), 0, 0f);
        }

        public static void DrawStarDimension()
        {
            var starTexture = NamelessDeityDimensionSkyGenerator.NamelessDeityDimensionTarget;

            // Prepare for shader drawing.
            bool useBehindStarsShader = NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.2f && !NoxusBossConfig.Instance.PhotosensitivityMode && NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.Opacity > 0f;
            if (NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState == NamelessDeityBoss.NamelessAIType.OpenScreenTear)
                useBehindStarsShader = false;

            if (useBehindStarsShader)
            {
                float idealBrightnessIntensity = 2.05f;
                var namelessAIState = NamelessDeityBoss.Myself.As<NamelessDeityBoss>().CurrentState;
                if (namelessAIState is NamelessDeityBoss.NamelessAIType.SunBlenderBeams or NamelessDeityBoss.NamelessAIType.RealityTearPunches or NamelessDeityBoss.NamelessAIType.ArcingEyeStarbursts)
                    idealBrightnessIntensity = 1.19f;

                SkyDistortionBrightnessIntensity = Lerp(SkyDistortionBrightnessIntensity, idealBrightnessIntensity, 0.15f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());

                // Draw the stars, with things revealed behind Nameless Deity.
                float scale = NamelessDeityBoss.Myself.As<NamelessDeityBoss>().TeleportVisualsAdjustedScale.Length() * 0.707f;
                float distortionRadius = NamelessDeityBoss.Myself.width * NamelessDeityBoss.Myself.Opacity * Pow(scale, 2f) * 2.09f;
                Vector2 distortionCenter = (NamelessDeityBoss.Myself.Center - Main.screenPosition) / starTexture.Size();

                var behindStarsShader = ShaderManager.GetShader("BehindStarsShader");
                behindStarsShader.TrySetParameter("aspectRatio", starTexture.Height / (float)starTexture.Width);
                behindStarsShader.TrySetParameter("distortionRadius", distortionRadius / starTexture.Width);
                behindStarsShader.TrySetParameter("distortionCenter", distortionCenter);
                behindStarsShader.TrySetParameter("brightnessIntensity", scale * SkyDistortionBrightnessIntensity);
                behindStarsShader.SetTexture(DendriticNoise, 1);
                behindStarsShader.SetTexture(FireNoise, 2);
                behindStarsShader.Apply();
            }

            Main.spriteBatch.Draw(starTexture, Vector2.Zero, Color.White * Pow(Intensity, 2f));

            if (useBehindStarsShader)
            {
                // Return to normal.
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone, null, GetCustomSkyBackgroundMatrix());
            }
        }

        public static void ReplaceMoonWithNamelessDeityEye(Vector2 eyePosition, Matrix perspectiveMatrix)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, perspectiveMatrix);

            // Draw a glowing orb over the moon.
            float glowDissipateFactor = Remap(SkyEyeOpacity, 0.2f, 1f, 1f, 0.74f);
            Vector2 backglowOrigin = BloomCircleSmall.Size() * 0.5f;
            Vector2 baseScale = Vector2.One * SkyEyeOpacity * Lerp(1.9f, 2f, Cos01(Main.GlobalTimeWrappedHourly * 4f)) * SkyEyeScale * 1.4f;

            // Make everything "blink" at first.
            baseScale.Y *= 1f - Convert01To010(InverseLerp(0.25f, 0.75f, SkyEyeOpacity));

            Color additiveWhite = Color.White with { A = 0 };
            Main.spriteBatch.Draw(BloomCircleSmall, eyePosition, null, additiveWhite * glowDissipateFactor * 0.42f, 0f, backglowOrigin, baseScale * 1.4f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, eyePosition, null, (Color.IndianRed with { A = 0 }) * glowDissipateFactor * 0.22f, 0f, backglowOrigin, baseScale * 2.4f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircleSmall, eyePosition, null, (Color.Coral with { A = 0 }) * glowDissipateFactor * 0.13f, 0f, backglowOrigin, baseScale * 3.3f, 0, 0f);

            // Draw a bloom flare over the orb.
            Main.spriteBatch.Draw(BloomFlare, eyePosition, null, (Color.LightCoral with { A = 0 }) * glowDissipateFactor * 0.6f, Main.GlobalTimeWrappedHourly * 0.4f, BloomFlare.Size() * 0.5f, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, eyePosition, null, (Color.Coral with { A = 0 }) * glowDissipateFactor * 0.6f, Main.GlobalTimeWrappedHourly * -0.26f, BloomFlare.Size() * 0.5f, baseScale * 0.7f, 0, 0f);

            // Draw the spires over the bloom flare.
            Main.spriteBatch.Draw(ChromaticSpires, eyePosition, null, additiveWhite * glowDissipateFactor, 0f, ChromaticSpires.Size() * 0.5f, baseScale * 0.8f, 0, 0f);

            // Draw the eye.
            Texture2D eyeTexture = NamelessDeityBoss.EyeTexture.Value;
            Texture2D pupilTexture = NamelessDeityBoss.PupilTexture.Value;
            Vector2 eyeScale = baseScale * 0.4f;
            Main.spriteBatch.Draw(eyeTexture, eyePosition, null, additiveWhite * SkyEyeOpacity, 0f, eyeTexture.Size() * 0.5f, eyeScale, 0, 0f);
            Main.spriteBatch.Draw(pupilTexture, eyePosition + (new Vector2(6f, 0f) + SkyPupilOffset) * eyeScale, null, additiveWhite * SkyEyeOpacity, 0f, pupilTexture.Size() * 0.5f, eyeScale * SkyPupilScale, 0, 0f);
        }

        public override float GetCloudAlpha() => 1f - Clamp(Intensity, SkyIntensityOverride, 1f);

        public override void Activate(Vector2 position, params object[] args)
        {
            IsEffectActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            IsEffectActive = false;
        }

        public override void Reset()
        {
            IsEffectActive = false;
        }

        public override bool IsActive()
        {
            return IsEffectActive || Intensity > 0f;
        }
    }
}
