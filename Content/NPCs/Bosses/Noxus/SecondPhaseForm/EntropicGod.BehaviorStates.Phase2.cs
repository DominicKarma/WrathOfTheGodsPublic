using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
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
        public void DoBehavior_GeometricSpikesTeleportAndFireballs()
        {
            int hoverTime = 45;
            int handRaiseTime = 30;
            int fireballShootRate = 10;
            int spikeShootCount = 17;
            int attackTransitionDelay = 105;
            float fireballShootSpeed = 17f;
            float maxHandRaiseOffset = 332f;
            float handSpeedFactor = 4f;

            if (Main.expertMode)
            {
                hoverTime -= 8;
                fireballShootSpeed += 3f;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
                fireballShootSpeed += 3.5f;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                fireballShootRate = 4;
                spikeShootCount = 25;
                attackTransitionDelay = 40;
            }

            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;
            Vector2 flyHoverOffset = new(Sin(TwoPi * AttackTimer / 90f) * 300f, Sin(TwoPi * AttackTimer / 70f + PiOver4) * 50f);

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Simply hover above the target at first.
            if (AttackTimer <= hoverTime)
                BasicFlyMovement(Target.Center - Vector2.UnitY * 420f + flyHoverOffset);

            // Slow down and raise hands. As the hands are raised they shoot fireballs at the target.
            else if (AttackTimer <= hoverTime + handRaiseTime)
            {
                // Rapidly decelerate.
                NPC.velocity *= 0.93f;

                float handRaiseInterpolant = InverseLerp(hoverTime, hoverTime + handRaiseTime, AttackTimer);
                float handRaiseVerticalOffset = Pow(handRaiseInterpolant, 2.3f) * maxHandRaiseOffset;
                leftHandDestination.X = Lerp(leftHandDestination.X, NPC.Center.X, handRaiseInterpolant * 0.7f);
                rightHandDestination.X = Lerp(rightHandDestination.X, NPC.Center.X, handRaiseInterpolant * 0.7f);
                leftHandDestination.Y -= handRaiseVerticalOffset;
                rightHandDestination.Y -= handRaiseVerticalOffset;

                // Open hands.
                MakeHandsOpen();

                // Shoot fireballs at the target.
                if (AttackTimer % fireballShootRate == fireballShootRate - 1f)
                {
                    SoundEngine.PlaySound(FireballShootSound, Hands[0].Center);
                    SoundEngine.PlaySound(FireballShootSound, Hands[1].Center);

                    // Make the fireballs slower at first.
                    fireballShootSpeed *= Utils.Remap(AttackTimer - hoverTime, 0f, 24f, 0.4f, 1f);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 fireballShootVelocity = (Target.Center - Hands[0].Center).SafeNormalize(Vector2.UnitY) * fireballShootSpeed;
                        NewProjectileBetter(NPC.GetSource_FromAI(), Hands[0].Center, fireballShootVelocity, ModContent.ProjectileType<DarkFireball>(), FireballDamage, 0f);

                        fireballShootVelocity = (Target.Center - Hands[1].Center).SafeNormalize(Vector2.UnitY) * fireballShootSpeed;
                        NewProjectileBetter(NPC.GetSource_FromAI(), Hands[1].Center, fireballShootVelocity, ModContent.ProjectileType<DarkFireball>(), FireballDamage, 0f);
                    }
                }
            }

            // Teleport and leave behind a trail of spikes.
            if (AttackTimer >= hoverTime + handRaiseTime && AttackTimer <= hoverTime + handRaiseTime + DefaultTeleportDelay)
            {
                leftHandDestination.Y -= maxHandRaiseOffset;
                rightHandDestination.Y -= maxHandRaiseOffset;
                leftHandDestination.X = Lerp(leftHandDestination.X, NPC.Center.X, 0.7f);
                rightHandDestination.X = Lerp(rightHandDestination.X, NPC.Center.X, 0.7f);

                // Teleport far away from the target and release the spikes when ready.
                if (AttackTimer == hoverTime + handRaiseTime + DefaultTeleportDelay)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < spikeShootCount; i++)
                        {
                            float horizonalSpikeOffsetDirection = Lerp(-1f, 1f, i / (float)(spikeShootCount - 1f));
                            Vector2 spikeSpawnPosition = Target.Center + new Vector2(horizonalSpikeOffsetDirection * 1500f, -900f);
                            spikeSpawnPosition.Y = NPC.Center.Y;

                            Vector2 spikeShootDirection = Vector2.Lerp(Vector2.UnitY, (Target.Center - spikeSpawnPosition).SafeNormalize(Vector2.UnitY), 0.2f).SafeNormalize(Vector2.UnitY);
                            NewProjectileBetter(NPC.GetSource_FromAI(), spikeSpawnPosition, spikeShootDirection * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                            NewProjectileBetter(NPC.GetSource_FromAI(), spikeSpawnPosition, spikeShootDirection * new Vector2(1f, -1f) * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                        }
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NoxusExplosion>(), ExplosionDamage, 0f);
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                    }

                    TeleportTo(Target.Center + Vector2.UnitY * 2500f);
                    SoundEngine.PlaySound(ExplosionTeleportSound with
                    {
                        Volume = 0.7f
                    }, Target.Center);
                }
            }

            if (AttackTimer >= hoverTime + handRaiseTime + DefaultTeleportDelay + attackTransitionDelay)
                SelectNextAttack();

            // Handle teleport visual effects.
            TeleportVisualsInterpolant = InverseLerp(hoverTime + handRaiseTime, hoverTime + handRaiseTime + DefaultTeleportDelay * 2f, AttackTimer);
            NPC.Opacity = InverseLerp(0.4f, 0.1f, TeleportVisualsInterpolant, true) + InverseLerp(0.6f, 0.85f, TeleportVisualsInterpolant);

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_PortalChainCharges()
        {
            int portalExistTime = 76;
            int teleportDelay = 44;
            int fireballCount = 4;
            int chargeCount = 3;
            float portalScale = 1f;
            float startingChargeSpeed = 11f;
            float chargeAcceleration = 1.12f;
            float maxFireballShootAngle = ToRadians(75f);
            float fireballShootSpeed = 12f;

            if (Main.expertMode)
            {
                portalExistTime -= 5;
                fireballShootSpeed += 3f;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
            {
                chargeCount++;
                fireballShootSpeed += 3.5f;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                teleportDelay = 20;
                chargeCount = 9;
                fireballShootSpeed = 23.5f;
            }

            if (CurrentPhase >= 2)
            {
                portalExistTime -= 3;
                fireballCount += 2;
                chargeCount++;
                maxFireballShootAngle *= 1.28f;
                fireballShootSpeed += 4f;
                portalScale += 0.1f;
            }
            float wrappedAttackTimer = AttackTimer % (portalExistTime + 6f);

            // Create portals.
            if (wrappedAttackTimer == 1f)
            {
                SoundEngine.PlaySound(FireballShootSound with { Volume = 2f }, Target.Center);

                // Teleport away from the player at first if this is the first time Noxus is charging, to ensure he doesn't weirdly disappear for the portal teleport.
                if (AttackTimer <= 5f)
                    TeleportToWithDecal(Target.Center + Vector2.UnitY * 2300f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 portalSpawnPosition = Target.Center + Target.Velocity.SafeNormalize(Main.rand.NextVector2Unit()) * 480f;
                    Vector2 portalDirection = (Target.Center - portalSpawnPosition).SafeNormalize(Vector2.UnitY);
                    NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition + Target.Velocity * 30f, portalDirection, ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalExistTime);

                    TeleportPosition = portalSpawnPosition + Target.Velocity * 30f;
                    TeleportDirection = portalDirection;

                    portalSpawnPosition += portalDirection * Target.Center.Distance(portalSpawnPosition) * 2f;
                    portalDirection = (Target.Center - portalSpawnPosition).SafeNormalize(Vector2.UnitY);
                    NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition + Target.Velocity * 30f, portalDirection, ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalExistTime);

                    NPC.netUpdate = true;
                }
            }

            // Do the teleport and release fireballs.
            if (wrappedAttackTimer == teleportDelay)
            {
                NPC.Center = TeleportPosition;
                NPC.velocity = TeleportDirection * startingChargeSpeed;
                NPC.netUpdate = true;

                for (int i = 0; i < NPC.oldPos.Length; i++)
                    NPC.oldPos[i] = Vector2.Zero;

                SoundEngine.PlaySound(JumpscareSound with { Volume = 0.5f }, NPC.Center);
                KeyboardBrightnessIntensity += 0.67f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                    for (int i = 0; i < fireballCount; i++)
                    {
                        float fireballShootAngle = Lerp(-maxFireballShootAngle, maxFireballShootAngle, i / (float)(fireballCount - 1f));
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, NPC.SafeDirectionTo(Target.Center).RotatedBy(fireballShootAngle) * fireballShootSpeed, ModContent.ProjectileType<DarkFireball>(), CometDamage, 0f);
                    }
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, NPC.SafeDirectionTo(Target.Center) * fireballShootSpeed * 0.45f, ModContent.ProjectileType<DarkFireball>(), CometDamage, 0f);
                }
            }

            // Do post-teleport behaviors.
            if (wrappedAttackTimer >= teleportDelay)
            {
                NPC.Opacity = InverseLerpBump(teleportDelay - 3f, teleportDelay + 6f, teleportDelay + 15f, teleportDelay + 19f, wrappedAttackTimer);
                NPC.velocity *= chargeAcceleration;
                ChargeAfterimageInterpolant = 1f;

                // Stop moving if very far away from the target. This is done to prevent the music from getting screwed up.
                if (!NPC.WithinRange(Target.Center, 1400f))
                    NPC.velocity = Vector2.Zero;
            }

            if (AttackTimer >= (portalExistTime + 6f) * chargeCount)
            {
                ClearAllProjectiles();
                SelectNextAttack();
            }
        }

        public void DoBehavior_ThreeDimensionalNightmareDeathRay()
        {
            int moveIntoBackgroundTime = 90;
            int spinTime = 169;
            int aimUpwardsTime = 60;
            int aimDownwardsTime = 11;
            int laserShootCount = 1;
            int secondarySlamTime = 0;
            float handSpeedFactor = 2f;
            float spikeSpacing = 118f;

            if (CurrentPhase >= 2)
            {
                moveIntoBackgroundTime -= 20;
                secondarySlamTime = 109;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                moveIntoBackgroundTime = 32;
                spikeSpacing = 78f;
            }

            int deathrayShootTime = spinTime + aimUpwardsTime + aimDownwardsTime + secondarySlamTime + 24;
            float wrappedAttackTimer = AttackTimer % (moveIntoBackgroundTime + deathrayShootTime);
            Vector2 headCenter = NPC.Center + HeadOffset;
            Vector2 headTangentDirection = (NPC.rotation + HeadRotation).ToRotationVector2();
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Teleport above the target and decide the laser spin direction on the first frame.
            if (wrappedAttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitY * 300f);
                LaserSpinDirection = Main.rand.NextFromList(-1f, 1f);
            }

            if (AttackTimer >= (moveIntoBackgroundTime + deathrayShootTime) * laserShootCount)
            {
                if (AttackTimer >= (moveIntoBackgroundTime + deathrayShootTime) * laserShootCount + 3f)
                    SelectNextAttack();
                return;
            }

            // Move into the background and hover above the target.
            if (wrappedAttackTimer <= moveIntoBackgroundTime)
            {
                ZPosition = Pow(wrappedAttackTimer / moveIntoBackgroundTime, 0.45f) * 1.83f;
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center + new Vector2(Target.Velocity.X * 12f, ZPosition * -200f), 0.14f);
                NPC.velocity.X *= 0.9f;

                // Have Noxus spread his hands outward in a T-pose right before the lasers fire.
                float tPoseInterpolant = InverseLerp(moveIntoBackgroundTime - 30f, moveIntoBackgroundTime - 12f, wrappedAttackTimer);
                leftHandDestination = Vector2.Lerp(leftHandDestination, NPC.Center - Vector2.UnitX * NPC.scale * 400f, tPoseInterpolant);
                rightHandDestination = Vector2.Lerp(rightHandDestination, NPC.Center + Vector2.UnitX * NPC.scale * 400f, tPoseInterpolant);
            }

            // Have Noxus hold his hands up to his head during the spin attack, as though they're casting energy into the orb.
            // This has a small amount of jitter for detail.
            else
            {
                leftHandDestination = headCenter + headTangentDirection * NPC.scale * -80f + Main.rand.NextVector2Circular(7f, 7f);
                rightHandDestination = headCenter + headTangentDirection * NPC.scale * 80f + Main.rand.NextVector2Circular(7f, 7f);
                MakeHandsOpen();
            }

            // Make the laser telegraph opacity go up before the deathray fires.
            LaserTelegraphOpacity = 0f;
            if (wrappedAttackTimer >= moveIntoBackgroundTime - 54f && wrappedAttackTimer < moveIntoBackgroundTime)
                LaserTelegraphOpacity = InverseLerpBump(moveIntoBackgroundTime - 54f, moveIntoBackgroundTime - 12f, moveIntoBackgroundTime - 6f, moveIntoBackgroundTime - 1f, wrappedAttackTimer);

            // Fire the deathray and create some chromatic aberration effects.
            if (wrappedAttackTimer == moveIntoBackgroundTime)
            {
                SoundEngine.PlaySound(ClapSound);
                SoundEngine.PlaySound(ExplosionSound);
                SoundEngine.PlaySound(NightmareDeathrayShootSound);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.UnitX, ModContent.ProjectileType<NightmareDeathRay>(), NightmareDeathrayDamage, 0f, -1, 0f, deathrayShootTime);
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }

                ScreenEffectSystem.SetFlashEffect(NPC.Center, 4f, 45);
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 3f, 45);
            }

            // Try to stay near the target on the Y axis when the laser is spinning.
            float verticalFollowPlayerInterpolant = InverseLerpBump(moveIntoBackgroundTime, moveIntoBackgroundTime + 30f, moveIntoBackgroundTime + spinTime - 30f, moveIntoBackgroundTime + spinTime, wrappedAttackTimer);
            NPC.Center = Vector2.Lerp(NPC.Center, new Vector2(NPC.Center.X, Target.Center.Y), verticalFollowPlayerInterpolant * 0.02f);

            // Orient the laser direction in 3D space.
            // It begins by spinning around, before orienting itself upward and slicing downward, releasing countless spikes.
            float spinCompletion = InverseLerp(moveIntoBackgroundTime, moveIntoBackgroundTime + 30f, wrappedAttackTimer);
            float spinInterpolant = EasingCurves.Cubic.Evaluate(EasingType.In, spinCompletion);
            float generalSpin = TwoPi * (wrappedAttackTimer - moveIntoBackgroundTime) * spinInterpolant / 90f;
            if (LaserSpinDirection == -1f)
                generalSpin = -generalSpin - Pi;

            float aimUpwardsInterpolant = Pow(InverseLerp(spinTime, spinTime + aimUpwardsTime - 20f, wrappedAttackTimer), 0.589f);
            float aimDownwardsInterpolant = Pow(InverseLerp(spinTime + aimUpwardsTime, spinTime + aimUpwardsTime + aimDownwardsTime, wrappedAttackTimer), 0.71f);

            LaserRotation = new(0f, 0f, WrapAngle(generalSpin));
            LaserRotation = new(0f, 0f, LaserRotation.Z.AngleLerp(PiOver2, aimUpwardsInterpolant));

            // Make the laser shift based on the upwards/downwards interpolants.
            LaserSquishFactor = Lerp(1f, 0.4f, aimUpwardsInterpolant) + aimDownwardsInterpolant * 0.5f;
            LaserLengthFactor = Lerp(1f, -1f, aimDownwardsInterpolant);

            // Make the laser slam down again if necessary.
            if (secondarySlamTime > 0)
            {
                float secondarySlamInterpolant = InverseLerp(0f, secondarySlamTime, wrappedAttackTimer - spinTime - aimUpwardsTime - aimDownwardsTime);
                if (secondarySlamInterpolant > 0f)
                {
                    LaserLengthFactor = Lerp(-1f, 1f, InverseLerpBump(0.33f, 0.5f, 0.78f, 0.9f, secondarySlamInterpolant));
                    LaserSquishFactor = Lerp(LaserSquishFactor, 0.1f, InverseLerpBump(-1f, -0.6f, 0.6f, 1f, LaserSquishFactor));
                }

                // Make the screen shatter and release a bunch of perpendicular spikes when the laser is done slamming.
                if ((int)(secondarySlamInterpolant * secondarySlamTime) == (int)(secondarySlamTime * 0.9f))
                {
                    // Make the screen shatter.
                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    ScreenShatterSystem.CreateShatterEffect(new Vector2(NPC.Center.X - Main.screenPosition.X, Main.screenHeight));
                    KeyboardBrightnessIntensity = 1f;

                    // Release the spikes.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float spikeVerticalOffset = Main.rand.NextFloat(32f, 84f);
                        for (float dy = spikeVerticalOffset; dy < 2000f; dy += spikeSpacing)
                        {
                            NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * dy, Vector2.UnitX * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                            NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * dy, -Vector2.UnitX * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                        }
                    }
                }
            }

            // Make Noxus raise his right hand when the laser is aiming upward and vice versa.
            rightHandDestination.Y -= aimUpwardsInterpolant * NPC.scale * LaserLengthFactor * 280f;

            // Make the right hand open.
            if (aimUpwardsInterpolant > 0f)
                Hands[1].ShouldOpen = true;

            // Rise upward as the laser is moved upward.
            if (aimUpwardsInterpolant > 0f && LaserLengthFactor != -1f && NPC.HasPlayerTarget)
                NPC.Center = Vector2.Lerp(NPC.Center, new Vector2(NPC.Center.X, Target.Center.Y - aimUpwardsInterpolant * 800f), aimUpwardsInterpolant * 0.2f);

            // Make the screen shatter.
            if (wrappedAttackTimer == spinTime + aimUpwardsTime + 10f)
            {
                // Release perpendicular spikes.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float spikeVerticalOffset = Main.rand.NextFloat(32f, 84f);
                    for (float dy = spikeVerticalOffset; dy < 2000f; dy += 118f)
                    {
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * dy, Vector2.UnitX * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + Vector2.UnitY * dy, -Vector2.UnitX * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                    }
                }

                SoundEngine.PlaySound(ExplosionTeleportSound);
                ScreenShatterSystem.CreateShatterEffect(new Vector2(NPC.Center.X - Main.screenPosition.X, Main.screenHeight));
                KeyboardBrightnessIntensity = 1f;
            }

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_OrganizedPortalCometBursts()
        {
            int handRaiseTime = 26;
            int portalCastTime = 26;
            int portalSummonRate = 3;
            int portalLingerTime = 70;
            int attackTransitionDelay = 150;
            float handSpeedFactor = 1.96f;
            float portalScale = 0.8f;
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                portalLingerTime = 40;
                attackTransitionDelay = 90;
            }

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

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

                float verticalOffset = Pow(teleportWaitInterpolant, 1.74f) * 330f;
                leftHandDestination.Y -= verticalOffset;
                rightHandDestination.Y -= verticalOffset;

                // Teleport near the target when ready.
                if (AttackTimer == DefaultTeleportDelay)
                {
                    Vector2 hoverDestination = Target.Center - Vector2.UnitY * 350f;
                    TeleportTo(hoverDestination);
                }
            }

            // Raise hands before summoning the portals.
            else if (AttackTimer <= DefaultTeleportDelay * 2f + handRaiseTime)
            {
                float handRaiseInterpolant = InverseLerp(DefaultTeleportDelay * 2f, DefaultTeleportDelay * 2f + handRaiseTime - 6f, AttackTimer);
                leftHandDestination = Vector2.Lerp(leftHandDestination, NPC.Center + new Vector2(-80f, -200f) * NPC.scale, handRaiseInterpolant);
                rightHandDestination = Vector2.Lerp(rightHandDestination, NPC.Center + new Vector2(80f, -200f) * NPC.scale, handRaiseInterpolant);

                PortalArcSpawnCenter = Target.Center;
            }

            // Move hands in an arc and summon portals to the sides of the target.
            else if (AttackTimer < DefaultTeleportDelay * 2f + handRaiseTime + portalCastTime)
            {
                float arcInterpolant = InverseLerp(DefaultTeleportDelay * 2f + handRaiseTime, DefaultTeleportDelay * 2f + handRaiseTime + portalCastTime - 15f, AttackTimer);
                float horizontalArcOffset = Convert01To010(arcInterpolant);
                leftHandDestination = NPC.Center + new Vector2(-horizontalArcOffset * 110f - 80f, arcInterpolant * 400f - 200f) * NPC.scale;
                rightHandDestination = NPC.Center + new Vector2(horizontalArcOffset * 110f + 80f, arcInterpolant * 400f - 200f) * NPC.scale;

                // Summon two portals above and below the target.
                if (AttackTimer == DefaultTeleportDelay * 2f + handRaiseTime + 4f)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 portalSpawnPosition = PortalArcSpawnCenter - Vector2.UnitY * 400f;
                        NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition, (PortalArcSpawnCenter - portalSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalLingerTime);

                        portalSpawnPosition = PortalArcSpawnCenter + Vector2.UnitY * 400f;
                        NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition, (PortalArcSpawnCenter - portalSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalLingerTime);
                    }
                }

                // Summon the portals.
                if (AttackTimer % portalSummonRate == 0f)
                {
                    SoundEngine.PlaySound(FireballShootSound with { MaxInstances = 10 }, Target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 portalSpawnPosition = PortalArcSpawnCenter + new Vector2(-720f - horizontalArcOffset * 240f, -560f + arcInterpolant * 1120f);
                        NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition, (PortalArcSpawnCenter - portalSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalLingerTime);

                        portalSpawnPosition = PortalArcSpawnCenter + new Vector2(720f + horizontalArcOffset * 240f, -560f + arcInterpolant * 1120f);
                        NewProjectileBetter(NPC.GetSource_FromAI(), portalSpawnPosition, (PortalArcSpawnCenter - portalSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalLingerTime);
                    }
                }

                // Keep all portals in stasis so that they fire at the same time.
                if (AttackTimer < DefaultTeleportDelay * 2f + handRaiseTime + portalCastTime - 10f)
                {
                    foreach (Projectile portal in AllProjectilesByID(ModContent.ProjectileType<DarkPortal>()))
                    {
                        if (portal.As<DarkPortal>().Time >= portalLingerTime * 0.5f - 9f)
                            portal.As<DarkPortal>().Time = (int)(portalLingerTime * 0.5f - 9f);
                    }
                }
            }

            // Teleport away and let all portals naturally fire.
            if (AttackTimer == DefaultTeleportDelay * 2f + handRaiseTime + portalCastTime)
            {
                SoundEngine.PlaySound(ExplosionTeleportSound);
                TeleportTo(Target.Center - Vector2.UnitX * TargetDirection * 300f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
            }

            if (AttackTimer >= DefaultTeleportDelay * 2f + handRaiseTime + portalCastTime + attackTransitionDelay)
                SelectNextAttack();

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_RealityWarpSpinCharge()
        {
            int teleportDelay = 15;
            int spinDelay = 16;
            int spinTime = 76;
            int chargeTime = 48;
            int slowDarkCometCount = 5;
            int fastDarkCometCount = 7;
            float darkCometSpread = ToRadians(72f);
            float fastDarkCometShootSpeed = 5f;
            float spinRadius = 600f;
            float maxSpinSpeed = ToRadians(10f);
            float handSpeedFactor = 4f;
            float startingChargeSpeed = 3f;

            if (Main.expertMode)
            {
                spinTime -= 10;
                maxSpinSpeed *= 1.333f;
            }

            if (CommonCalamityVariables.RevengeanceModeActive)
            {
                spinTime -= 5;
                startingChargeSpeed += 0.95f;
                fastDarkCometShootSpeed += 2.2f;
            }

            if (CurrentPhase >= 2)
            {
                spinTime -= 22;
                maxSpinSpeed *= 1.5f;
                startingChargeSpeed += 1.2f;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                spinTime = 38;
                maxSpinSpeed = ToRadians(22f);
                startingChargeSpeed = 6.7f;
            }

            float slowDarkCometShootSpeed = fastDarkCometShootSpeed * 0.56f;
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Go to the next attack state once all charges have been performed.
            if (AttackTimer >= (teleportDelay + spinDelay + spinTime + chargeTime))
            {
                NPC.Opacity = 1f;
                SelectNextAttack();
                return;
            }

            // Slow down in anticipation of the teleport. Teleport visual effects are executed later on.
            // Also delete leftover projectiles, since they're not gonna be easy to see after the screen shatter.
            if (AttackTimer <= teleportDelay)
            {
                NPC.velocity *= 0.8f;
                ClearAllProjectiles();
            }

            // Teleport to the side of the player and wait before charging.
            else if (AttackTimer <= teleportDelay + spinDelay)
            {
                if (AttackTimer == teleportDelay + 1f)
                {
                    SpinAngularOffset = Main.rand.NextFloat(TwoPi);
                    TeleportTo(Target.Center + SpinAngularOffset.ToRotationVector2() * spinRadius);
                    ScreenShatterSystem.CreateShatterEffect(NPC.Center - Main.screenPosition);
                    KeyboardBrightnessIntensity = 1f;

                    SoundEngine.PlaySound(ExplosionTeleportSound with
                    {
                        Volume = 0.7f
                    }, Target.Center);
                }

                NPC.velocity = Vector2.Zero;
            }

            // Spin around the player.
            if (AttackTimer >= teleportDelay && AttackTimer <= teleportDelay + spinDelay + spinTime)
            {
                float spinInterpolant = InverseLerp(teleportDelay + spinDelay, teleportDelay + spinDelay + spinTime, AttackTimer);
                float spinSpeed = SmoothStep(0f, maxSpinSpeed, InverseLerpBump(0f, 0.35f, 0.6f, 0.98f, spinInterpolant));
                SpinAngularOffset += spinSpeed;
                NPC.Center = Target.Center + SpinAngularOffset.ToRotationVector2() * spinRadius;

                // Make the afterimages appear.
                ChargeAfterimageInterpolant = InverseLerp(0f, 0.1f, spinInterpolant);
            }

            // Charge at the player and release bursts of dark comets.
            if (AttackTimer >= teleportDelay + spinDelay + spinTime)
            {
                ChargeAfterimageInterpolant = 1f;

                // Curve towards the player before accelerating.
                Vector2 idealDirection = NPC.SafeDirectionTo(Target.Center);
                if (AttackTimer >= teleportDelay + spinDelay + spinTime + 7f)
                    idealDirection = NPC.velocity.SafeNormalize(Vector2.UnitY);
                if (AttackTimer == teleportDelay + spinDelay + spinTime + 1f)
                    NPC.velocity = idealDirection * startingChargeSpeed;
                NPC.velocity *= 1.1f;

                // Perform clap effects and release dark comets.
                if (AttackTimer == teleportDelay + spinDelay + spinTime + 3f)
                {
                    SoundEngine.PlaySound(ClapSound, Target.Center);
                    ScreenEffectSystem.SetBlurEffect((Hands[0].Center + Hands[1].Center) * 0.5f, 0.7f, 27);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), (Hands[0].Center + Hands[1].Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);

                    // Shoot the comets.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 cometSpawnPosition = (Hands[0].Center + Hands[1].Center) * 0.5f;
                        for (int i = 0; i < slowDarkCometCount; i++)
                        {
                            float localDarkCometSpread = Lerp(-darkCometSpread, darkCometSpread, i / (float)(slowDarkCometCount - 1f));
                            Vector2 darkCometShootVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(localDarkCometSpread) * slowDarkCometShootSpeed;
                            NewProjectileBetter(NPC.GetSource_FromAI(), cometSpawnPosition, darkCometShootVelocity, ModContent.ProjectileType<DarkComet>(), NPC.damage, 0f);
                        }
                        for (int i = 0; i < fastDarkCometCount; i++)
                        {
                            float localDarkCometSpread = Lerp(-darkCometSpread, darkCometSpread, i / (float)(fastDarkCometCount - 1f));
                            Vector2 darkCometShootVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(localDarkCometSpread) * fastDarkCometShootSpeed;
                            NewProjectileBetter(NPC.GetSource_FromAI(), cometSpawnPosition, darkCometShootVelocity, ModContent.ProjectileType<DarkComet>(), NPC.damage, 0f);
                        }
                    }
                }

                handSpeedFactor *= 2f;
                leftHandDestination = NPC.Center + new Vector2(8f, 150f);
                rightHandDestination = NPC.Center + new Vector2(-8f, 150f);

                // Fade away once going fast enough.
                if (NPC.velocity.Length() >= 12f)
                    NPC.Opacity = Clamp(NPC.Opacity - 0.03f, 0f, 1f);
            }

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }
    }
}
