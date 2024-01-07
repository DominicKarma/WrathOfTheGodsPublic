using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.ShapeCurves;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class ClockConstellation : BaseNamelessDeityConstellationProjectile, IProjOwnedByBoss<NamelessDeityBoss>
    {
        private static bool timeIsStopped;

        private static Asset<Texture2D> hourHandAsset;

        private static Asset<Texture2D> minuteHandAsset;

        public override int ConvergeTime => ConvergenceDuration;

        public override int StarDrawIncrement => 4;

        public override float StarConvergenceSpeed => 0.00036f;

        public override float StarRandomOffsetFactor => 1f;

        protected override ShapeCurve constellationShape
        {
            get
            {
                ShapeCurveManager.TryFind("Clock", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 1.414f);
            }
        }

        public override Color DecidePrimaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Red, Color.Yellow, Pow(colorVariantInterpolant, 2f)) * 0.33f;
        }

        public override Color DecideSecondaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Orange, Color.White, colorVariantInterpolant) * 0.4f;
        }

        public SlotId TickSound;

        public int StartingHour;

        public int TimeRestartDelay;

        public int TollCounter;

        public float PreviousHourRotation = -10f;

        // Every second toll reverses time.
        public bool TimeIsReversed => TollCounter % 2 == 1;

        public int TollSpinDuration
        {
            get
            {
                // The first toll involves convergence behaviors for the stars. As such, the convergence time must be added for it specifically.
                if (TollCounter == 0)
                    return ConvergeTime + RegularSpinDuration;

                // Otherwise, use the corresponding spin duration based on whether time is being reversed.
                return TimeIsReversed ? ReversedTimeSpinDuration : RegularSpinDuration;
            }
        }

        public static int MaxTolls => 2;

        public static float HourArc => Pi / 6f;

        // These angular velocity constants manipulate the hour hand, hence their slow pacing.
        public static float DefaultAngularVelocity => HourArc / RegularSpinDuration;

        // Static for access by Nameless when calculating attack durations.
        public static int ConvergenceDuration => SecondsToFrames(5f);

        public static int RegularSpinDuration => SecondsToFrames(5.5f);

        public static int ReversedTimeSpinDuration => SecondsToFrames(5f);

        public static int TollWaitDuration => SecondsToFrames(2f);

        public static float FadeOutIncrement => 0.034f;

        public static float ReversedTimeMinSpeedFactor => 0.36f;

        public static float ReversedTimeMaxSpeedFactor => 5.1f;

        // This may seem a bit opaque, but the math behind how this is arrived at should elucidate things.
        // Basically, the equation for the reversed time arc movement factor is as follows:

        // ArcWindUpInterpolant = InverseLerp(0f, ReversedTimeSpinDuration, TimeSinceLastToll)
        // ArcMovementFactor = Lerp(ReversedTimeMinSpeedFactor, ReversedTimeMaxSpeedFactor, Pow(ArcWindUpInterpolant, ReversedTimeArcInterpolantPower))

        // Things like wind-up time and speed factors are constants above, since unlike the ReversedTimeArcInterpolantPower variable those are intuitive and easy to control.

        // So how do we solve for the exponent?
        // Well, it's necessary to look at the problem in terms of discrete steps, since this process will happen in discrete steps every frame.
        // Each frame, we'll increment the angle by DefaultAngularVelocity * ArcMovementFactor. This journey should encompass 360 / 12 (or 30) degrees (aka going from one hour to another).
        // So basically, we want the sum of all of these increments to equal pi/6, and in such a way that ReversedTimeArcInterpolantPower is a calculated variable.
        // Fortunately, there's a perfect tool for this task: a definite integral.
        // While it will be slightly off, since integrals work on infinitesimal sum increments rather than discrete frame rates, it should be close enough to be sufficient.
        // This integral will allow for the calculation of things *based on their incremental behavior*. Let's rewrite the equation in terms of it:

        // For reference, ArcWindUpInterpolant is the variable of integration as t, ReversedTimeMinSpeedFactor is a, ReversedTimeMaxSpeedFactor is b, DefaultAngularVelocity is c, ReversedTimeSpinDuration is f, and ReversedTimeArcInterpolantPower is x.
        // c * f * ∫(0, 1) (a + (b - a) * t^x) * dt = pi / 6
        // ∫(0, 1) (a + (b - a) * t^x) * dt = pi / (c * f * 6)                                              (Get constants on right side)
        // ∫(0, 1) a * dt + ∫(0, 1) (b - a) * t^x * dt = pi / (c * f * 6)                                   (Split constant term into second integral)
        // ∫(0, 1) (b - a) * t^x * dt = pi / (c * f * 6) - a                                                (Separate constant and move to the right side)
        // (1^(x + 1) * (b - a)) / (x + 1) - (0^(x + 1) * (b - a)) / (x + 1) = pi / (c * f * 6) - a         (Evaluate integral at upper and lower bounds and subtract the two)
        // (b - a) / (x + 1) = pi / (c * f * 6) - a                                                         (Simplify results)
        // (x + 1) / (b - a) = 1 / (pi / (c * f * 6) - a)                                                   (Invert so that x + 1 is on in the left hand numerator)
        // x + 1 = (b - a) / (pi / (c * f * 6) - a)                                                         (Remove denominator from left side)
        // x = (b - a) / (pi / (c * f * 6) - a) - 1                                                         (Remove the subtraction by 1 to get the value of x)
        public static float ReversedTimeArcInterpolantPower => (ReversedTimeMaxSpeedFactor - ReversedTimeMinSpeedFactor) / (HourArc / (DefaultAngularVelocity * ReversedTimeSpinDuration) - ReversedTimeMinSpeedFactor) - 1f;

        public static bool TimeIsStopped
        {
            get
            {
                // Turn off the time stop effect if Nameless isn't actually present.
                if (NamelessDeityBoss.Myself is null)
                    timeIsStopped = false;

                return timeIsStopped;
            }
            set => timeIsStopped = value;
        }

        public ref float HourHandRotation => ref Projectile.ai[0];

        public ref float MinuteHandRotation => ref Projectile.ai[1];

        public ref float TimeSinceLastToll => ref Projectile.ai[2];

        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            hourHandAsset = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Projectiles/ClockHourHand");
            minuteHandAsset = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Projectiles/ClockMinuteHand");
        }

        public override void SetDefaults()
        {
            Projectile.width = 840;
            Projectile.height = 840;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 60000;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(TimeRestartDelay);
            writer.Write(TollCounter);
            writer.Write(PreviousHourRotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            TimeRestartDelay = reader.ReadInt32();
            TollCounter = reader.ReadInt32();
            PreviousHourRotation = reader.ReadSingle();
        }

        public override void PostAI()
        {
            // Fade in at first. If the final toll has happened, fade out.
            if (TollCounter >= MaxTolls)
                Projectile.Opacity = Clamp(Projectile.Opacity - FadeOutIncrement, 0f, 1f);
            else
                Projectile.Opacity = InverseLerp(0f, 45f, Time);

            // Make the time restart delay go down.
            TimeIsStopped = false;
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            int starburstID = ModContent.ProjectileType<Starburst>();
            if (TimeRestartDelay >= 1)
            {
                TimeIsStopped = true;
                TimeRestartDelay--;

                // Make all starbursts go back.
                if (TimeRestartDelay <= 0)
                {
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        Projectile p = Main.projectile[i];
                        if (p.type == starburstID && p.active)
                        {
                            p.ai[2] = 1f;
                            p.velocity = p.DirectionToSafe(Projectile.Center) * p.velocity.Length() * 0.31f;
                            if (!p.WithinRange(Projectile.Center, ReversedTimeSpinDuration * 16f))
                                p.Kill();

                            p.netUpdate = true;
                        }
                    }
                    HourHandRotation -= 0.004f;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                TimeSinceLastToll++;

                // Create periodically collapsing chromatic bursts. This doesn't happen on low graphics settings.
                bool lowGraphicsSettings = NoxusBossConfig.Instance.PhotosensitivityMode || Main.gfxQuality <= 0.4f;
                if (TimeIsReversed && TimeSinceLastToll % 30 == 1 && !lowGraphicsSettings)
                {
                    ExpandingChromaticBurstParticle burst = new(Projectile.Center, Vector2.Zero, Color.Coral * 0.54f, 14, 18.3f, -1.3f);
                    burst.Spawn();
                }
            }

            // Kill all starbursts that are going back in time and are close to the c lock.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.type == starburstID && p.active && p.ai[2] == 1f && p.WithinRange(Projectile.Center, 30f))
                    p.Kill();
            }

            // Approach the nearest player.
            if (Projectile.WithinRange(target.Center, 100f) || TimeIsStopped || TimeIsReversed || TollCounter >= MaxTolls)
                Projectile.velocity *= 0.82f;
            else
            {
                float approachSpeed = Pow(InverseLerp(ConvergeTime, 0f, Time), 2f) * 19f + 3f;
                Projectile.velocity = Projectile.DirectionToSafe(target.Center) * approachSpeed;
            }

            // Make the hands move quickly as they fade in before moving more gradually.
            // This cause a time stop if the hour hand reaches a new hour and the clock has completely faded in.
            float handAppearInterpolant = InverseLerp(0f, ConvergeTime, Time);
            float baseAngularVelocity = DefaultAngularVelocity;
            float handMovementSpeed = 1f;

            // Make the hands accelerate as time goes backwards.
            if (TimeIsReversed)
            {
                float arcWindUpInterpolant = InverseLerp(0f, ReversedTimeSpinDuration, TimeSinceLastToll);
                handMovementSpeed = Lerp(ReversedTimeMinSpeedFactor, ReversedTimeMaxSpeedFactor, Pow(arcWindUpInterpolant, ReversedTimeArcInterpolantPower));
            }
            if (TollCounter >= MaxTolls)
                handMovementSpeed = 0f;

            // Make the time go on.
            float hourHandOffset = baseAngularVelocity * handMovementSpeed * (1f - TimeIsStopped.ToInt()) * TimeIsReversed.ToDirectionInt();

            // Make the hour move manually at the start.
            if (handAppearInterpolant < 1f)
            {
                PreviousHourRotation = StartingHour * HourArc;
                HourHandRotation = Pow(handAppearInterpolant, 0.15f) * HourArc * 12f + HourArc * StartingHour;
            }
            else
                HourHandRotation -= hourHandOffset;

            // Ensure that hand rotations stay within a 0-2pi range.
            PreviousHourRotation = WrapAngle360(PreviousHourRotation);
            HourHandRotation = WrapAngle360(HourHandRotation);

            // Make the minute hand rotate in accordance with the hour hand.
            MinuteHandRotation = WrapAngle360(HourHandRotation * 12f - PiOver2);

            // Make the clock strike if it reaches a new hour.
            if (TimeSinceLastToll >= TollSpinDuration)
            {
                // Create a clock strike sound and other visuals.
                StartShakeAtPoint(Projectile.Center, 11f);
                ScreenEffectSystem.SetFlashEffect(Projectile.Center, 1f, 30);
                SoundEngine.PlaySound(NamelessDeityBoss.ClockStrikeSound);

                float closestHourRotation = Round(WrapAngle360(HourHandRotation) / HourArc) * HourArc;
                MinuteHandRotation = HourHandRotation * 12f - PiOver2;
                HourHandRotation = closestHourRotation;
                TimeSinceLastToll = 0f;
                TimeRestartDelay = TollWaitDuration;
                Projectile.netUpdate = true;
                TollCounter++;
                NamelessDeityKeyboardShader.BrightnessIntensity += 0.8f;

                // Make the clock hands split the screen instead on the final toll.
                if (TollCounter >= MaxTolls)
                {
                    ScreenEffectSystem.SetFlashEffect(Projectile.Center, 3f, 60);
                    StartShakeAtPoint(Projectile.Center, 16f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int telegraphTime = 41;
                        float telegraphLineLength = 4500f;
                        Vector2 hourHandDirection = HourHandRotation.ToRotationVector2();
                        Vector2 minuteHandDirection = MinuteHandRotation.ToRotationVector2();

                        foreach (var starburst in AllProjectilesByID(starburstID))
                            starburst.Kill();

                        NewProjectileBetter(Projectile.Center - minuteHandDirection * telegraphLineLength * 0.5f, minuteHandDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, telegraphTime, telegraphLineLength);
                        NewProjectileBetter(Projectile.Center - hourHandDirection * telegraphLineLength * 0.5f, hourHandDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, telegraphTime, telegraphLineLength);
                    }
                }
            }

            // Start the loop sound and initialize the starting hour configuration on the first frame.
            if (Projectile.localAI[0] == 0f || ((!SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s2) || !s2.IsPlaying) && !TimeIsStopped))
            {
                if (Projectile.localAI[0] == 0f)
                {
                    StartingHour = Main.rand.Next(12);
                    HourHandRotation = StartingHour * HourArc;
                    Projectile.netUpdate = true;
                }

                TickSound = SoundEngine.PlaySound(TimeIsReversed ? NamelessDeityBoss.ClockTickSoundReversed : NamelessDeityBoss.ClockTickSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            // Update the ticking loop sound.
            if (SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s))
            {
                s.Position = Projectile.Center;
                s.Volume = Projectile.Opacity * handAppearInterpolant * 1.9f;

                // Make the sound temporarily stop if time is stopped.
                if (TimeIsStopped || Time <= ConvergeTime * 0.65f || TollCounter >= MaxTolls)
                    s.Stop();
            }

            // Release starbursts in an even spread. This is made to roughly sync up with the clock ticks.
            int starburstReleaseRate = 17;
            int starburstCount = 9;
            float starburstShootSpeed = 2.05f;

            // GFB? Uh oh!
            if (Main.zenithWorld)
            {
                starburstReleaseRate = 9;
                starburstCount = 11;
                starburstShootSpeed = 5f;
            }

            if (handAppearInterpolant >= 0.75f && Time % starburstReleaseRate == 0f && !TimeIsStopped && TollCounter < MaxTolls)
            {
                StartShakeAtPoint(Projectile.Center, 3.6f);

                bool canPlaySound = !TimeIsReversed || CountProjectiles(starburstID) >= 30;
                if (canPlaySound)
                    SoundEngine.PlaySound((TimeIsReversed ? NamelessDeityBoss.SunFireballShootSoundReversed : NamelessDeityBoss.SunFireballShootSound) with { Volume = 0.76f, MaxInstances = 20 }, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient && !TimeIsReversed)
                {
                    float shootOffsetAngle = (Time % (starburstReleaseRate * 2f) == 0f) ? Pi / starburstCount : 0f;
                    shootOffsetAngle += Main.rand.NextFloatDirection() * 0.0294f;

                    for (int i = 0; i < starburstCount; i++)
                    {
                        Vector2 starburstVelocity = (TwoPi * i / starburstCount + shootOffsetAngle + Projectile.AngleTo(target.Center)).ToRotationVector2() * starburstShootSpeed;
                        NewProjectileBetter(Projectile.Center, starburstVelocity, starburstID, NamelessDeityBoss.StarburstDamage, 0f, -1, 0f, 2f);
                    }
                }
            }

            // Adjust the time. This process can technically make Main.time negative but that doesn't seem to cause any significant problems, and works fine with the watch UI.
            int hour = (int)((HourHandRotation + PiOver2 + 0.002f).Modulo(TwoPi) / TwoPi * 12f);
            int minute = (int)((MinuteHandRotation + PiOver2 + 0.002f).Modulo(TwoPi) / TwoPi * 60f);
            int totalMinutes = hour * 60 + minute;
            Main.dayTime = true;
            Main.time = totalMinutes * 60 - 16200f;
        }

        public void DrawBloom()
        {
            if (bloomCircle is null)
                return;

            // Calculate colors.
            Color bloomCircleColor = Projectile.GetAlpha(Color.Orange) * 0.3f;
            Color bloomFlareColor = Projectile.GetAlpha(Color.LightCoral) * 0.64f;

            // Draw the bloom backglow.
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.Draw(bloomCircle, drawPosition, null, bloomCircleColor, 0f, bloomCircle.Size() * 0.5f, 5f, 0, 0f);

            // Draw bloom flares that go in opposite rotations.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * -0.4f;
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor, bloomFlareRotation * -0.7f, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
        }

        public void DrawClockHands()
        {
            // Calculate clock hand colors.
            float handOpacity = InverseLerp(ConvergeTime - 40f, ConvergeTime + 96f, Time);
            Color generalHandColor = Color.Lerp(Color.OrangeRed, Color.Coral, 0.24f) with { A = 20 };
            Color minuteHandColor = Projectile.GetAlpha(generalHandColor) * handOpacity;
            Color hourHandColor = Projectile.GetAlpha(generalHandColor) * handOpacity;

            // Collect textures.
            Texture2D minuteHandTexture = minuteHandAsset.Value;
            Texture2D hourHandTexture = hourHandAsset.Value;

            // Calculate the clock hand scale and draw positions. The scale is relative to the hitbox of the projectile so that the clock can be arbitrarily sized without issue.
            float handScale = Projectile.width / (float)hourHandTexture.Width * 0.52f;
            Vector2 handBaseDrawPosition = Projectile.Center - Main.screenPosition;
            Vector2 minuteHandDrawPosition = handBaseDrawPosition - MinuteHandRotation.ToRotationVector2() * handScale * 26f;
            Vector2 hourHandDrawPosition = handBaseDrawPosition - HourHandRotation.ToRotationVector2() * handScale * 26f;

            // Draw the hands with afterimages.
            for (int i = 0; i < 24; i++)
            {
                float afterimageOpacity = 1f - i / 24f;
                float minuteHandAfterimageRotation = MinuteHandRotation + i * (TimeIsStopped ? 0.002f : 0.008f) * TimeIsReversed.ToDirectionInt();
                float hourHandAfterimageRotation = HourHandRotation + i * (TimeIsStopped ? 0.0013f : 0.005f) * TimeIsReversed.ToDirectionInt();
                Main.spriteBatch.Draw(minuteHandTexture, minuteHandDrawPosition, null, minuteHandColor * afterimageOpacity, minuteHandAfterimageRotation, Vector2.UnitY * minuteHandTexture.Size() * 0.5f, handScale, 0, 0f);
                Main.spriteBatch.Draw(hourHandTexture, hourHandDrawPosition, null, hourHandColor * afterimageOpacity, hourHandAfterimageRotation, Vector2.UnitY * hourHandTexture.Size() * 0.5f, handScale, 0, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacityFactor = Lerp(1f, 0.6f, InverseLerpBump(30f, 60f, 90f, 120f, TimeRestartDelay));
            Projectile.Opacity *= opacityFactor;

            // Draw bloom behind the clock to give a nice ambient glow.
            Main.spriteBatch.UseBlendState(BlendState.Additive);
            DrawBloom();
            Main.spriteBatch.ResetToDefault();

            // Draw the clock.
            Projectile.scale *= 1.7f;
            base.PreDraw(ref lightColor);
            Projectile.scale /= 1.7f;

            // Draw clock hands.
            DrawClockHands();
            Projectile.Opacity /= opacityFactor;

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(TickSound, out ActiveSound s))
                s.Stop();
            TimeIsStopped = false;
        }
    }
}
