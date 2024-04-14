using System;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.Graphics.Shaders.Keyboard.NoxusKeyboardShader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        public void DoBehavior_PortalChainCharges2()
        {
            int initialPortalExistTime = 132;
            int aimedPortalExistTime = 75;
            int teleportDelay = 44;
            int cometCount = 7;
            int aimedDashTime = 33;
            int aimedDashCount = 3;
            float portalScale = 1.5f;
            float horizontalPortalOffset = 600f;
            float startingChargeSpeed = 10f;
            float maxChargeSpeed = 111.63f;
            float chargeAcceleration = 1.09f;

            if (PortalChainDashCounter >= 1)
            {
                teleportDelay -= 4;
                aimedPortalExistTime -= 4;
            }

            if (Main.expertMode)
            {
                cometCount += 2;
                initialPortalExistTime -= 10;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
            {
                teleportDelay -= 5;
                aimedDashCount += 2;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
                aimedPortalExistTime = 45;

            // Create two portals above (Or below if they're flying upward) the target on the first frame.
            if (AttackTimer <= 1f)
            {
                SoundEngine.PlaySound(FireballShootSound with { Volume = 2f }, Target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 portalSpawnPosition = Target.Center - Vector2.UnitX * Target.Velocity.X.NonZeroSign() * 350f;
                    NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition - Vector2.UnitY * horizontalPortalOffset, Vector2.UnitY, ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, initialPortalExistTime);
                    NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition + Vector2.UnitY * horizontalPortalOffset, -Vector2.UnitY, ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, initialPortalExistTime);

                    TeleportDirection = Vector2.UnitY;
                    TeleportPosition = portalSpawnPosition - Vector2.UnitY * TeleportDirection * horizontalPortalOffset;
                    NPC.netUpdate = true;
                }

                NPC.Opacity = 0f;
                NPC.netUpdate = true;
            }

            // Perform accelerating teleports.
            if (AttackTimer == teleportDelay || (AttackTimer >= teleportDelay + 1f && NPC.Opacity <= 0f && AttackTimer < initialPortalExistTime - 25f))
            {
                NPC.Opacity = 1f;
                NPC.Center = TeleportPosition;
                NPC.velocity = TeleportDirection * MathF.Max(startingChargeSpeed, NPC.velocity.Length());
                NPC.netSpam = 0;
                NPC.netUpdate = true;

                for (int i = 0; i < NPC.oldPos.Length; i++)
                    NPC.oldPos[i] = Vector2.Zero;

                SoundEngine.PlaySound(JumpscareSound with { Volume = 0.45f, MaxInstances = 9 }, NPC.Center);
                KeyboardBrightnessIntensity += 0.67f;
            }

            // Accelerate.
            if (AttackTimer >= teleportDelay + 1f && AttackTimer < initialPortalExistTime)
            {
                NPC.velocity = (NPC.velocity * chargeAcceleration).ClampLength(startingChargeSpeed, maxChargeSpeed);

                // Fade away based on how close Noxus is to the next portal.
                NPC.Opacity = InverseLerp(NPC.velocity.Length() + 4f, NPC.velocity.Length() * 4f, NPC.Distance(TeleportPosition + TeleportDirection * horizontalPortalOffset * 2f));
                NPC.Opacity *= InverseLerp(initialPortalExistTime - 20f, initialPortalExistTime - 23f, AttackTimer);
            }

            // Secretly stay below the target as the aimed-ahead portal appears and when it goes away.
            if ((AttackTimer >= initialPortalExistTime && AttackTimer < initialPortalExistTime + teleportDelay) || AttackTimer >= initialPortalExistTime + teleportDelay + 42f)
            {
                NPC.Center = Target.Center + Vector2.UnitY * 1600f;
                NPC.Opacity = 0f;
            }

            // Be invisible right before the initial portal disappears.
            if (AttackTimer > initialPortalExistTime - 15f && AttackTimer < initialPortalExistTime)
                NPC.Opacity = 0f;

            // Create the aimed-ahead portal.
            if (AttackTimer == initialPortalExistTime)
            {
                SoundEngine.PlaySound(FireballShootSound with { Volume = 2f }, Target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    TeleportPosition = Target.Center + Target.Velocity.SafeNormalize(Vector2.UnitX * TargetDirection) * new Vector2(800f, 500f);
                    if (Target.Velocity.Length() <= 3f)
                        TeleportPosition = Target.Center + Main.rand.NextVector2CircularEdge(450f, 450f);

                    TeleportDirection = (Target.Center - TeleportPosition).SafeNormalize(Vector2.UnitY);

                    NewProjectileBetter(NPC.GetSource_FromAI(), TeleportPosition, TeleportDirection, ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale + 0.3f, aimedPortalExistTime);
                    NPC.netUpdate = true;
                }
            }

            // Do the hilarious super charge.
            if (AttackTimer == initialPortalExistTime + teleportDelay)
            {
                NPC.Center = TeleportPosition;
                SoundEngine.PlaySound(JumpscareSound with { Volume = 0.6f }, NPC.Center);
                KeyboardBrightnessIntensity += 0.67f;

                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 1.9f, 20);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                    for (int i = 0; i < cometCount; i++)
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, (TwoPi * i / cometCount).ToRotationVector2() * 4.4f, ModContent.ProjectileType<DarkComet>(), CometDamage, 0f);
                }

                // Reset afterimages.
                for (int i = 0; i < NPC.oldPos.Length; i++)
                    NPC.oldPos[i] = Vector2.Zero;

                // Shake the screen.
                StartShakeAtPoint(NPC.Center, 15f);

                // Perform the charge movement.
                NPC.velocity = TeleportDirection * maxChargeSpeed * 0.75f;

                // Charge in the opposite direction if the player has gone behind the portal.
                if (Vector2.Dot(Target.Center - TeleportPosition, TeleportDirection) < 0f)
                    NPC.velocity *= -1f;

                NPC.Opacity = 1f;
                NPC.netUpdate = true;
            }
            if (AttackTimer >= initialPortalExistTime + teleportDelay)
                NPC.Opacity = 1f;

            // Prepare the next dash.
            if (AttackTimer >= initialPortalExistTime + teleportDelay + aimedDashTime && PortalChainDashCounter < aimedDashCount - 1f)
            {
                AttackTimer = initialPortalExistTime - 1f;
                PortalChainDashCounter++;
                NPC.netUpdate = true;
            }

            if (AttackTimer >= initialPortalExistTime + aimedPortalExistTime)
                SelectNextAttack();
        }

        public void DoBehavior_BrainFogAndThreeDimensionalCharges()
        {
            int fogCoverTime = 145;
            int handPressTime = 36;
            int disappearIntoFogTime = 70;
            int chargeCount = 1;

            if (BrainFogChargeCounter >= 1)
            {
                fogCoverTime -= 55;
                disappearIntoFogTime -= 25;
            }

            int delayBeforeInvisible = DefaultTeleportDelay * 2 + fogCoverTime + disappearIntoFogTime;
            int delayBeforeTwinklesAppear = 60;
            int twinkleCount = 5;
            int delayPerTwinkle = 16;
            int chargeDelay = 48;
            int chargeTime = 20;

            if (Main.expertMode)
            {
                twinkleCount--;
                delayPerTwinkle -= 2;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
            {
                twinkleCount--;
                delayPerTwinkle--;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                twinkleCount = 3;
                delayPerTwinkle = 9;
                chargeDelay = 40;
            }

            int twinkleHoverTime = delayBeforeTwinklesAppear + delayPerTwinkle * twinkleCount + chargeDelay;
            float handSpeedFactor = 1.8f;
            float fogSpreadInterpolant = InverseLerp(DefaultTeleportDelay * 2f, DefaultTeleportDelay * 2f + fogCoverTime, AttackTimer);
            float handPressInterpolant = InverseLerp(DefaultTeleportDelay * 2f, DefaultTeleportDelay * 2f + handPressTime, AttackTimer);
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Disable contact damage by default.
            // This will get enabled again later in this method, during the charges.
            NPC.damage = 0;

            // Create some fog effects on the head as the fog gets stronger.
            if (fogSpreadInterpolant > 0f && ZPosition == 0f)
            {
                float fogOpacity = (fogSpreadInterpolant * 0.3f + 0.2f) * InverseLerp(0.9f, 0.67f, fogSpreadInterpolant);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 fogVelocity = Main.rand.NextVector2Circular(36f, 36f) * fogSpreadInterpolant;
                    HeavySmokeParticle fog = new(NPC.Center + HeadOffset, fogVelocity, NoxusSky.FogColor, 50, 3f, fogOpacity, 0f, true);
                    fog.Spawn();
                }
            }

            // Slow down and begin to teleport above the player. This also involves waiting after the teleport before attacking.
            if (AttackTimer <= DefaultTeleportDelay * 2f)
            {
                NPC.velocity *= 0.8f;

                // Raise hands as the teleport happens.
                float teleportWaitInterpolant = InverseLerp(0f, DefaultTeleportDelay - 9f, AttackTimer);
                if (AttackTimer >= DefaultTeleportDelay + 1f)
                    teleportWaitInterpolant = 0f;
                else
                    handSpeedFactor += 0.75f;

                float verticalOffset = Pow(teleportWaitInterpolant, 1.7f) * 400f;
                leftHandDestination.Y -= verticalOffset;
                rightHandDestination.Y -= verticalOffset;

                // Teleport near the target when ready.
                if (AttackTimer == DefaultTeleportDelay)
                {
                    Vector2 hoverDestination = Target.Center - Vector2.UnitY * 400f;
                    TeleportTo(hoverDestination);
                }
            }

            // The fog is coming the fog is coming the fog is coming the fog is coming the fog is coming the fog is coming the fog is coming
            else if (AttackTimer <= DefaultTeleportDelay * 2f + fogCoverTime)
            {
                FogSpreadDistance = Pow(fogSpreadInterpolant, 3.8f) * 2f;
                FogIntensity = Pow(fogSpreadInterpolant, 0.85f);
            }

            // Fly away into the fog.
            else if (AttackTimer <= delayBeforeInvisible)
            {
                float fadeIntoFogInterpolant = InverseLerp(DefaultTeleportDelay * 2f + fogCoverTime, delayBeforeInvisible, AttackTimer);
                ZPosition = fadeIntoFogInterpolant * 2.2f;
                NPC.Opacity = InverseLerp(1f, 0.5f, fadeIntoFogInterpolant);

                // Move above the target.
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * fadeIntoFogInterpolant * 360f, fadeIntoFogInterpolant * 0.08f);

                if (fadeIntoFogInterpolant >= 1f)
                    CreateTwinkle(NPC.Center, Vector2.One * 2f);
            }

            // Silently hover near the player while invisible. Twinkles are periodically released as indicates when this happens.
            else if (AttackTimer <= delayBeforeInvisible + twinkleHoverTime)
            {
                float predictivenessFadeOffInterpolant = InverseLerp(delayBeforeInvisible + twinkleHoverTime - 6f, delayBeforeInvisible + twinkleHoverTime - 20f, AttackTimer);
                Vector2 hoverDestination = Target.Center + Target.Velocity * InverseLerp(Target.Velocity.Length(), 5f, 10f) * predictivenessFadeOffInterpolant * 50f;
                if (NPC.WithinRange(hoverDestination, 150f))
                {
                    NPC.velocity *= 0.89f;
                    NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.11f);
                }
                else
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(Target.Center) * 18f, 0.11f);

                if (AttackTimer >= delayBeforeInvisible + delayBeforeTwinklesAppear && AttackTimer % delayPerTwinkle == delayPerTwinkle - 1f)
                    CreateTwinkle(NPC.Center + Main.rand.NextVector2Circular(30f, 30f), Vector2.One * 1.35f);

                // Make the screen shake as the wait continues.
                float maxScreenShake = InverseLerp(delayBeforeInvisible, delayBeforeInvisible + twinkleHoverTime, AttackTimer) * 8f + 1f;
                if (OverallShakeIntensity < maxScreenShake - 2f)
                    StartShakeAtPoint(NPC.Center, 2f);
            }

            // Charge very, very, VERY quickly at the target.
            else if (AttackTimer <= delayBeforeInvisible + twinkleHoverTime + chargeTime)
            {
                if (AttackTimer == delayBeforeInvisible + twinkleHoverTime + 1f)
                {
                    if (NPC.WithinRange(Target.Center, 105f))
                        NPC.Center = Target.Center + Main.rand.NextVector2CircularEdge(0.04f, 0.04f);

                    NPC.netUpdate = true;

                    SoundEngine.PlaySound(JumpscareSound);
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 20);
                    ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 1.3f, 60);
                    StartShakeAtPoint(NPC.Center, 16f);
                }

                // Get close and make the fog dissipate.
                FogIntensity = InverseLerp(delayBeforeInvisible + twinkleHoverTime + chargeTime - 4f, delayBeforeInvisible + twinkleHoverTime, AttackTimer);
                ZPosition = Clamp(ZPosition - 0.17f, -0.98f, 4f);
                NPC.Opacity = ZPosition <= -0.98f ? 0f : 1f;

                // Drift horizontally towards the target if they're sufficiently far from the twinkle, to make it so that they player can't just constantly run in one direction to avoid the charge.
                if (Distance(NPC.Center.X, Target.Center.X) >= 100f)
                    NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitX * NPC.SafeDirectionTo(Target.Center) * 16f, 0.067f);

                // Do damage again if zoomed in enough.
                if (ZPosition <= 0.3f && ZPosition >= -0.8f)
                    NPC.damage = NPC.defDamage;

                handPressInterpolant = 0f;
                handSpeedFactor += 6f;
            }

            // Shatter the screen after the charge has completed.
            // Also release comets at the player if they're relatively far away.
            if (AttackTimer == delayBeforeInvisible + twinkleHoverTime + chargeTime)
            {
                ScreenShatterSystem.CreateShatterEffect(NPC.Center - Main.screenPosition);
                KeyboardBrightnessIntensity = 1f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);

                    if (!NPC.WithinRange(Target.Center, 350f))
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 cometVelocity = (Target.Center - NPC.Center) * 0.0098f + Main.rand.NextVector2Circular(4f, 4f);
                            NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, cometVelocity, ModContent.ProjectileType<DarkComet>(), CometDamage, 0f);
                        }
                    }
                }
            }

            if (AttackTimer >= delayBeforeInvisible + twinkleHoverTime + chargeTime + 20f)
            {
                NPC.Opacity = 1f;
                ZPosition = 0f;
                FogSpreadDistance = 0f;
                FogIntensity = 0f;
                AttackTimer = 0f;
                BrainFogChargeCounter++;
                TeleportTo(Target.Center - Vector2.UnitY * 1200f);
                NPC.netUpdate = true;

                if (BrainFogChargeCounter >= chargeCount)
                    SelectNextAttack();
            }

            // Press hands together after the teleport.
            leftHandDestination = Vector2.Lerp(leftHandDestination, NPC.Center + TeleportVisualsAdjustedScale * new Vector2(-30f, 160f - Sqrt(fogSpreadInterpolant) * 66f), handPressInterpolant);
            rightHandDestination = Vector2.Lerp(rightHandDestination, NPC.Center + TeleportVisualsAdjustedScale * new Vector2(30f, 160f - Sqrt(fogSpreadInterpolant) * 66f), handPressInterpolant);

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }
    }
}
