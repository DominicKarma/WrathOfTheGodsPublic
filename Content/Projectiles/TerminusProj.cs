using System.Collections.Generic;
using Luminance.Assets;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Subworlds;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.Particles;
using NoxusBoss.Content.Projectiles.Visuals;
using NoxusBoss.Core.Fixes;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Projectiles
{
    public class TerminusProj : ModProjectile, IPixelatedPrimitiveRenderer, IDrawAdditive
    {
        public class ChargingEnergyStreak
        {
            public float Opacity = 1f;

            public float BaseWidth;

            public float SpeedInterpolant;

            public Color GeneralColor;

            public Vector2 CurrentOffset;

            public Vector2 StartingOffset;

            public ChargingEnergyStreak(float speedInterpolant, float baseWidth, Color generalColor, Vector2 startingOffset)
            {
                // Initialize things.
                SpeedInterpolant = speedInterpolant;
                BaseWidth = baseWidth;
                GeneralColor = generalColor;
                CurrentOffset = startingOffset;
                StartingOffset = startingOffset;

                // Don't attempt to load shaders serverside.
                if (Main.netMode == NetmodeID.Server)
                    return;
            }

            public void Update()
            {
                CurrentOffset = Vector2.Lerp(CurrentOffset, Vector2.Zero, SpeedInterpolant);
                CurrentOffset = Utils.MoveTowards(CurrentOffset, Vector2.Zero, SpeedInterpolant * 19f);

                if (CurrentOffset.Length() <= 8f)
                {
                    StartingOffset = Vector2.Lerp(StartingOffset, Vector2.Zero, SpeedInterpolant * 1.4f);
                    Opacity = Saturate(Opacity * 0.94f - 0.09f);
                }
            }

            public float EnergyWidthFunction(float completionRatio) => BaseWidth - (1f - completionRatio) * 3f;

            public Color EnergyColorFunction(float _) => GeneralColor with { A = 0 } * Opacity;
        }

        public enum TerminusAIState
        {
            RiseUpward,
            PerformDimnessEffect,
            ChargeEnergy,
            OpenEye
        }

        public bool HasInitialized
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value.ToInt();
        }

        public TerminusAIState CurrentState
        {
            get => (TerminusAIState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ShakeInfo Rumble;

        public Vector2 EyePupilOffset;

        public List<ChargingEnergyStreak> EnergyStreaks = [];

        public static LazyAsset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public static readonly SoundStyle ActivateSound = new("NoxusBoss/Assets/Sounds/Item/TerminusActivate");

        public static readonly SoundStyle RoarSound = new("NoxusBoss/Assets/Sounds/Item/TerminusRoar");

        public static readonly PiecewiseCurve RiseMotionCurve = new PiecewiseCurve().
            Add(EasingCurves.Quadratic, EasingType.In, -4f, 0.36f). // Ascend motion.
            Add(EasingCurves.Linear, EasingType.In, -4f, 0.72f). // Rise without change.
            Add(EasingCurves.MakePoly(1.5f), EasingType.Out, 0f, 1f); // Slowdown.

        // These first three times are roughly synced to the duration of the Terminus chargeup sound, which is around 5.813 seconds (348 frames).
        public static int RiseUpwardTime => 92;

        public static int DimnessAppearTime => 96;

        public static int EnergyChargeTime => 120;

        public static int EyeAppearTime => 45;

        public static int EyeOpenAnimationTime => 300;

        public ref float Time => ref Projectile.ai[1];

        public ref float DarknessIntensity => ref Projectile.ai[2];

        public ref float EyeOpacity => ref Projectile.localAI[1];

        public ref float VortexIntensity => ref Projectile.localAI[2];

        public Player Owner => Main.player[Projectile.owner];

        public override string Texture => "NoxusBoss/Content/Items/SummonItems/Terminus";

        public override void SetStaticDefaults()
        {
            if (Main.netMode != NetmodeID.Server)
                MyTexture = LazyAsset<Texture2D>.Request(Texture);
        }

        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 70;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // The Terminus refuses to exist if Nameless is around.
            if (NamelessDeityBoss.Myself is not null)
            {
                VortexIntensity = 0f;
                Projectile.active = false;
            }

            // Disallow entering the subworld if the world is saved on the cloud because apparently that's a common reason of why people's games are freezing.
            if (Main.ActiveWorldFileData.IsCloudSave)
            {
                BroadcastText(Language.GetTextValue("Mods.NoxusBoss.Dialog.CloudSaveMessage"), Color.Red);
                Projectile.active = false;
                return;
            }

            // Perform initialization effects on the first frame.
            if (!HasInitialized)
            {
                Projectile.position.X += Owner.direction * 38f;

                // Create the particle effects.
                ExpandingGreyscaleCircleParticle circle = new(Projectile.Center, Vector2.Zero, new(219, 194, 229), 10, 0.28f);
                VerticalLightStreakParticle bigLightStreak = new(Projectile.Center, Vector2.Zero, new(228, 215, 239), 10, new(2.4f, 3f));
                MagicBurstParticle magicBurst = new(Projectile.Center, Vector2.Zero, new(150, 109, 219), 12, 0.1f);
                for (int i = 0; i < 30; i++)
                {
                    Vector2 smallLightStreakSpawnPosition = Projectile.Center + Main.rand.NextVector2Square(-Projectile.width, Projectile.width) * new Vector2(0.4f, 0.2f);
                    Vector2 smallLightStreakVelocity = Vector2.UnitY * Main.rand.NextFloat(-4f, 4f);
                    VerticalLightStreakParticle smallLightStreak = new(smallLightStreakSpawnPosition, smallLightStreakVelocity, Color.White, 10, new(0.1f, 0.3f));
                    smallLightStreak.Spawn();
                }

                circle.Spawn();
                bigLightStreak.Spawn();
                magicBurst.Spawn();

                // Shake the screen a little bit.
                SetUniversalRumble(5.6f);

                // Start playing the Terminus chargeup sound.
                SoundEngine.PlaySound(ActivateSound);

                HasInitialized = true;
            }

            // Quickly fade in.
            Projectile.Opacity = Saturate(Projectile.Opacity + 0.1667f);

            // Inform the screen shader that the Terminus is present.
            TerminusShaderScene.Terminus = Projectile;

            // Perform AI states.
            switch (CurrentState)
            {
                case TerminusAIState.RiseUpward:
                    DoBehavior_RiseUpward();
                    break;
                case TerminusAIState.PerformDimnessEffect:
                    DoBehavior_PerformDimnessEffect();
                    break;
                case TerminusAIState.ChargeEnergy:
                    DoBehavior_ChargeEnergy();
                    break;
                case TerminusAIState.OpenEye:
                    DoBehavior_OpenEye();
                    break;
            }

            // Update all streaks.
            EnergyStreaks.RemoveAll(s => s.Opacity <= 0.003f);
            for (int i = 0; i < EnergyStreaks.Count; i++)
                EnergyStreaks[i].Update();

            // Update the crystal vortex.
            if (!Main.dedServ)
            {
                if (!Filters.Scene["NoxusBoss:TerminusVortex"].IsActive())
                    Filters.Scene.Activate("NoxusBoss:TerminusVortex");

                Filters.Scene["NoxusBoss:TerminusVortex"].GetShader().UseIntensity(VortexIntensity * 3f).UseProgress(0f).UseTargetPosition(Projectile.Center);
            }

            // Increment the general timer.
            Time++;
        }

        public void DoBehavior_RiseUpward()
        {
            // Rise upward. At the end of the animation the upward motion ceases.
            float animationCompletion = InverseLerp(0f, RiseUpwardTime, Time);
            Projectile.velocity = Vector2.UnitY * RiseMotionCurve.Evaluate(animationCompletion);

            // Periodically create chromatic aberration effects.
            if (Time % 30f == 29f)
            {
                float aberrationIntensity = Lerp(0.6f, 1.1f, animationCompletion);
                ScreenEffectSystem.SetChromaticAberrationEffect(Projectile.Center, aberrationIntensity, 24);
            }

            // Go to the next AI state once the rise animation is completed.
            if (animationCompletion >= 1f)
            {
                Time = 0f;
                CurrentState = TerminusAIState.PerformDimnessEffect;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_PerformDimnessEffect()
        {
            // Create a glitch sound on the first frame.
            if (Time == 1f)
            {
                SoundEngine.PlaySound(GlitchSound);
                StartShakeAtPoint(Projectile.Center, 10.5f);
            }

            // Make everything go dim.
            DarknessIntensity = InverseLerp(0f, DimnessAppearTime, Time);

            // Make the vortex appear.
            VortexIntensity = Saturate(VortexIntensity + 0.05f);

            // Go to the next AI state once the dimness animation is completed.
            if (DarknessIntensity >= 1f)
            {
                Time = 0f;
                CurrentState = TerminusAIState.ChargeEnergy;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_ChargeEnergy()
        {
            // Shake in place.
            Projectile.Center += Main.rand.NextVector2Circular(1f, 1f);

            // Create randomly spaced energy streaks around the Terminus. This stops before the animation does, to allow the streaks the settle.
            if (Time < EnergyChargeTime - 30f)
            {
                float streakSpeedInterpolant = Main.rand.NextFloat(0.15f, 0.187f);
                float streakWidth = Main.rand.NextFloat(4f, 5.6f);
                Vector2 streakOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(375f, 550f);
                Color streakColor = Color.Lerp(Color.Wheat, Color.IndianRed, Main.rand.NextFloat(0.6f));
                EnergyStreaks.Add(new(streakSpeedInterpolant, streakWidth, streakColor, streakOffset));
            }

            // Create pulse rings and bloom periodically.
            if (Time % 15f == 0f)
            {
                // Play pulse sounds.
                SoundEngine.PlaySound(PulseSound with { PitchVariance = 0.3f, MaxInstances = 10 });

                Color energyColor = Color.Lerp(Color.Wheat, Color.Red, Main.rand.NextFloat(0.5f));
                PulseRing ring = new(Projectile.Center, Vector2.Zero, energyColor, 4.2f, 0f, 38);
                ring.Spawn();

                StrongBloom bloom = new(Projectile.Center, Vector2.Zero, energyColor, 1f, 15);
                bloom.Spawn();
            }

            // Make the screen rumble.
            float animationCompletion = InverseLerp(0f, EnergyChargeTime, Time);
            float screenRumblePower = Lerp(4.5f, 8f, animationCompletion);
            SetUniversalRumble(screenRumblePower);

            // Create light sparkles everywhere.
            for (int i = 0; i < 2; i++)
            {
                Color sparkleColor = Color.Lerp(Color.Wheat, Color.IndianRed, Main.rand.NextFloat(0.36f));
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PortalBoltTrail, 0f, 0f, 0, sparkleColor);
                Main.dust[dustIndex].position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 10f, Projectile.height * 8f) - Vector2.UnitY * 100f;
                Main.dust[dustIndex].velocity *= Main.rand.NextFloat() * 0.8f;
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].noLight = true;
                Main.dust[dustIndex].fadeIn = 0.6f + Main.rand.NextFloat() * 0.63f;
                Main.dust[dustIndex].velocity += Vector2.UnitY * 3f;
                Main.dust[dustIndex].scale = 0.35f;

                if (dustIndex != Main.maxDust)
                {
                    Dust sparkleCopy = Dust.CloneDust(dustIndex);
                    sparkleCopy.scale /= 2f;
                    sparkleCopy.fadeIn *= 0.85f;
                    sparkleCopy.color = new Color(255, 255, 255, 255);
                }
            }

            // Go to the next AI state once the energy charge animation is completed.
            if (animationCompletion >= 1f)
            {
                Time = 0f;
                CurrentState = TerminusAIState.OpenEye;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_OpenEye()
        {
            // Make the darkness dissipate.
            DarknessIntensity *= 0.96f;

            float animationCompletion = InverseLerp(0f, EyeOpenAnimationTime, Time);

            // Play the roar on the first frame.
            if (Time == 1f)
                SoundEngine.PlaySound(RoarSound);

            // Create a god ray from above on the first frame if entering the Garden.
            if (Main.myPlayer == Projectile.owner && Time == 1f && !EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
            {
                Vector2 raySpawnPosition = Projectile.Bottom + Vector2.UnitY * 32f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), raySpawnPosition, Vector2.Zero, ModContent.ProjectileType<GodRayVisual>(), 0, 0f, Projectile.owner);
            }

            // Make the screen rumble.
            SetUniversalRumble(10f);

            // Make the eye fade in.
            EyeOpacity = InverseLerp(0f, EyeAppearTime, Time);

            // Periodically create chromatic aberration effects.
            if (Time % 30f == 29f)
            {
                float aberrationIntensity = Lerp(1.08f, 1.42f, animationCompletion);
                ScreenEffectSystem.SetChromaticAberrationEffect(Projectile.Center, aberrationIntensity, 24);
            }

            // Make the eye look up.
            if (animationCompletion >= 0.56f)
            {
                EyePupilOffset = Vector2.Lerp(EyePupilOffset, -Vector2.UnitY * 22f, 0.09f);
                VortexIntensity = Saturate(VortexIntensity - 0.065f);
            }

            // Make the screen go white. This draws over UI elements so that they don't suddenly and weirdly go away when the subworld is entered.
            if (animationCompletion >= 0.6f && Main.myPlayer == Projectile.owner)
                TotalScreenOverlaySystem.OverlayInterpolant = InverseLerp(0.6f, 0.96f, animationCompletion);

            // Teleport the user to the garden.
            if (animationCompletion >= 1f)
            {
                if (!Main.dedServ && Filters.Scene["NoxusBoss:TerminusVortex"].IsActive())
                    Filters.Scene.Deactivate("NoxusBoss:TerminusVortex");

                // Send the user to the eternal garden.
                EternalGarden.ClientWorldDataTag = EternalGarden.SafeWorldDataToTag("Client", false);
                if (Main.myPlayer == Projectile.owner)
                {
                    EternalGardenIntroBackgroundFix.ShouldDrawWhite = true;
                    if (SubworldSystem.IsActive<EternalGarden>())
                        SubworldSystem.Exit();
                    else
                        SubworldSystem.Enter<EternalGarden>();
                }

                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Utils.Remap(EyeOpacity, 0f, 0.6f, 1f, 0.2f);
            Main.EntitySpriteDraw(MyTexture.Value, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(Color.White) * opacity, Projectile.rotation, MyTexture.Value.Size() * 0.5f, Projectile.scale, 0, 0);
            return false;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            Vector2 eyePosition = Projectile.Center - Vector2.UnitY.RotatedBy(Projectile.rotation) * Projectile.scale * 6f - Main.screenPosition;

            // Draw a glowing orb over the moon.
            float glowDissipateFactor = Utils.Remap(EyeOpacity, 0.2f, 1f, 1f, 0.74f) * 0.67f;
            if (!Main.dayTime)
                glowDissipateFactor *= 1.5f;

            Vector2 backglowOrigin = BloomCircle.Size() * 0.5f;
            Vector2 baseScale = Vector2.One * EyeOpacity * Lerp(1.9f, 2f, Cos01(Main.GlobalTimeWrappedHourly * 4f)) * Projectile.scale;

            // Make everything "blink" at first.
            baseScale.Y *= 1f - Convert01To010(InverseLerp(0.25f, 0.75f, EyeOpacity));

            Main.spriteBatch.Draw(BloomCircle, eyePosition, null, Color.White * glowDissipateFactor, 0f, backglowOrigin, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircle, eyePosition, null, Color.IndianRed * glowDissipateFactor * 0.4f, 0f, backglowOrigin, baseScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(BloomCircle, eyePosition, null, Color.Coral * glowDissipateFactor * 0.3f, 0f, backglowOrigin, baseScale * 1.7f, 0, 0f);

            // Draw a bloom flare over the orb.
            Main.spriteBatch.Draw(BloomFlare, eyePosition, null, Color.LightCoral * glowDissipateFactor * 0.6f, Main.GlobalTimeWrappedHourly * 0.4f, BloomFlare.Size() * 0.5f, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(BloomFlare, eyePosition, null, Color.Coral * glowDissipateFactor * 0.6f, Main.GlobalTimeWrappedHourly * -0.26f, BloomFlare.Size() * 0.5f, baseScale * 0.7f, 0, 0f);

            // Draw the spires over the bloom flare.
            Main.spriteBatch.Draw(ChromaticSpires, eyePosition, null, Color.White * glowDissipateFactor, 0f, ChromaticSpires.Size() * 0.5f, baseScale * 0.8f, 0, 0f);

            // Draw the eye.
            Texture2D eyeTexture = NamelessDeityBoss.EyeTexture.Value;
            Texture2D pupilTexture = NamelessDeityBoss.PupilTexture.Value;
            Vector2 eyeScale = baseScale * 0.4f;
            Main.spriteBatch.Draw(eyeTexture, eyePosition, null, Color.White * EyeOpacity, 0f, eyeTexture.Size() * 0.5f, eyeScale, 0, 0f);
            Main.spriteBatch.Draw(pupilTexture, eyePosition + (Vector2.UnitX * 6f + EyePupilOffset) * eyeScale, null, Color.White * EyeOpacity, 0f, pupilTexture.Size() * 0.5f, eyeScale, 0, 0f);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            // Configure the streak shader's texture.
            var streakShader = ShaderManager.GetShader("NoxusBoss.GenericTrailStreak");
            streakShader.SetTexture(StreakBloomLine, 1);

            // Draw energy streaks as primitives.
            Vector2 drawCenter = Projectile.Center - Vector2.UnitY.RotatedBy(Projectile.rotation) * Projectile.scale * 6f;
            for (int i = 0; i < EnergyStreaks.Count; i++)
            {
                ChargingEnergyStreak streak = EnergyStreaks[i];
                Vector2 start = streak.StartingOffset;
                Vector2 end = streak.CurrentOffset;
                Vector2 midpoint1 = Vector2.Lerp(start, end, 0.2f);
                Vector2 midpoint2 = Vector2.Lerp(start, end, 0.4f);
                Vector2 midpoint3 = Vector2.Lerp(start, end, 0.6f);
                Vector2 midpoint4 = Vector2.Lerp(start, end, 0.8f);

                PrimitiveSettings settings = new(streak.EnergyWidthFunction, streak.EnergyColorFunction, _ => drawCenter, Pixelate: true, Shader: ShaderManager.GetShader("NoxusBoss.GenericTrailStreak"));
                PrimitiveRenderer.RenderTrail(new Vector2[]
                {
                    end, midpoint4, midpoint3, midpoint2, midpoint1, start
                }, settings, 27);
            }
        }
    }
}
