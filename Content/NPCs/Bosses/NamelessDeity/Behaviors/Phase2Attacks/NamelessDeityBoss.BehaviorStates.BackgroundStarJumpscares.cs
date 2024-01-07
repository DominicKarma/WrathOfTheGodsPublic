using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public int BackgroundStarJumpscares_StarShoveDelay
        {
            get
            {
                int starShoveDelay = DefaultTwinkleLifetime + 10;

                if (BackgroundStarJumpscares_BarrageCounter >= 1f)
                    starShoveDelay -= 4;

                if (Main.zenithWorld)
                    starShoveDelay = 6;

                return starShoveDelay;
            }
        }

        public int BackgroundStarJumpscares_AttackTransitionDelay
        {
            get
            {
                int attackTransitionDelay = 80;

                if (BackgroundStarJumpscares_BarrageCounter >= 1f)
                    attackTransitionDelay += 40;

                if (Main.zenithWorld)
                    attackTransitionDelay = 67;

                return attackTransitionDelay;
            }
        }

        public ref float BackgroundStarJumpscares_BarrageCounter => ref NPC.ai[2];

        public void LoadStateTransitions_BackgroundStarJumpscares()
        {
            // Load the transition from BackgroundStarJumpscares to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.BackgroundStarJumpscares, null, false, () =>
            {
                return AttackTimer >= BackgroundStarJumpscares_StarShoveDelay + BackgroundStarJumpscares_AttackTransitionDelay && HeavenlyBackgroundIntensity == 1f;
            });
        }

        public void DoBehavior_BackgroundStarJumpscares()
        {
            int barrageCount = 2;
            int backgroundDimTime = 32;
            int starCreationRate = 5;
            int starCreationCountPerSide = 4;
            int starFireDelay = 29;
            int starShoveDelay = BackgroundStarJumpscares_StarShoveDelay;
            int attackTransitionDelay = BackgroundStarJumpscares_AttackTransitionDelay;
            float defaultStarZPosition = 5.9f;
            float defaultHandHoverOffset = 870f;
            float handHoverOffset = defaultHandHoverOffset;
            Vector2 leftHandHoverPosition = NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * defaultHandHoverOffset;
            Vector2 rightHandHoverPosition = NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * defaultHandHoverOffset;
            Vector2 destinationOffsetArea = new(792f, 450f);
            ref float barrageCounter = ref BackgroundStarJumpscares_BarrageCounter;
            ref float timeSinceStarsWereShoved = ref NPC.ai[3];

            float handUpwardExtendInterpolant = InverseLerp(4f, 10f, timeSinceStarsWereShoved);
            float handOutwardExtendInterpolant = InverseLerpBump(starShoveDelay - 9f, starShoveDelay - 2f, starShoveDelay + 14f, starShoveDelay + 20f, timeSinceStarsWereShoved) * (1f - handUpwardExtendInterpolant);
            leftHandHoverPosition += new Vector2(-240f, -120f) * handOutwardExtendInterpolant;
            leftHandHoverPosition += new Vector2(750f, -1000f) * handUpwardExtendInterpolant;
            rightHandHoverPosition += new Vector2(240f, -120f) * handOutwardExtendInterpolant;
            rightHandHoverPosition += new Vector2(-750f, -1000f) * handUpwardExtendInterpolant;

            if (barrageCounter >= 1f)
            {
                starCreationRate--;
                starCreationCountPerSide++;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                starCreationRate = 2;
                starFireDelay = 10;
                destinationOffsetArea *= 1.1f;
            }

            int starCreationTime = starCreationRate * starCreationCountPerSide + starFireDelay;

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Get rid of the seam if it's still there but hidden.
            SeamScale = 0f;

            // Make the background dim and have Nameless go into the background at first.
            if (AttackTimer <= backgroundDimTime)
            {
                // Start a teleport
                if (AttackTimer == 1)
                    StartTeleportAnimation(() => Target.Center - Vector2.UnitY * 300f, 11, 11);

                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 0.5f, 0.09f);
                ZPosition = Pow(AttackTimer / (float)backgroundDimTime, 1.74f) * 4.5f;

                if (CurrentPhase >= 1)
                    KaleidoscopeInterpolant *= 0.8f;

                // Create hands.
                if (AttackTimer == backgroundDimTime - 1f)
                {
                    while (Hands.Count < starCreationCountPerSide * 2)
                        ConjureHandsAtPosition(NPC.Center, Vector2.Zero);
                }
            }

            // Move behind the player and cast stars.
            else if (AttackTimer <= backgroundDimTime + starCreationTime)
            {
                // Make the hands suddenly move outward. They return to Nameless shortly before the stars being being shoved.
                handHoverOffset = Remap(AttackTimer - backgroundDimTime, starCreationTime - 12f, starCreationTime - 4f, 1220f, defaultHandHoverOffset);
                if (AttackTimer == backgroundDimTime + 1f)
                {
                    SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with
                    {
                        Volume = 8f,
                        MaxInstances = 10,
                        Pitch = -0.5f
                    });
                }

                // Create stars.
                if (AttackTimer % starCreationRate == 1f && AttackTimer < backgroundDimTime + starCreationRate * starCreationCountPerSide)
                {
                    SoundEngine.PlaySound(SoundID.Item100 with
                    {
                        Pitch = 0.2f,
                        Volume = 0.6f,
                        MaxInstances = 8
                    });
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int starCreationCounter = CountProjectiles(ModContent.ProjectileType<BackgroundStar>()) / 2;

                        // Create the star on the left.
                        float destinationOffsetAngle = Pi * starCreationCounter / starCreationCountPerSide - PiOver4;
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(star =>
                        {
                            star.As<BackgroundStar>().ScreenDestinationOffset = destinationOffsetAngle.ToRotationVector2() * destinationOffsetArea + Main.rand.NextVector2Circular(150f, 55f);
                        });
                        NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<BackgroundStar>(), 0, 0f, -1, defaultStarZPosition + Main.rand.NextFloat(3.7f), -starCreationCounter);

                        // Create the star on the right.
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(star =>
                        {
                            star.As<BackgroundStar>().ScreenDestinationOffset = destinationOffsetAngle.ToRotationVector2() * -destinationOffsetArea + Main.rand.NextVector2Circular(150f, 55f);
                        });
                        NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<BackgroundStar>(), 0, 0f, -1, defaultStarZPosition + Main.rand.NextFloat(3.7f), starCreationCounter);

                        // Fire an NPC state sync.
                        NPC.netSpam = 0;
                        NPC.netUpdate = true;
                    }
                }

                // Hold all stars in place near the target.
                var stars = AllProjectilesByID(ModContent.ProjectileType<BackgroundStar>());
                foreach (Projectile star in stars)
                {
                    if (!star.As<BackgroundStar>().ApproachingScreen)
                    {
                        float starIndex = star.As<BackgroundStar>().Index;
                        float parallaxSpeed = Lerp(0.4f, 0.9f, star.identity * 13.584f % 1f);
                        Vector2 starHoverOffset = (star.identity * 98.157f).ToRotationVector2() * Abs(starIndex) * 12f;
                        Vector2 parallaxOffset = Vector2.One * (AttackTimer - backgroundDimTime) * -parallaxSpeed;
                        star.Center = Target.Center + new Vector2(starIndex * 154f, Abs(starIndex) * -20f) + starHoverOffset + parallaxOffset;
                        star.velocity = -Vector2.One * parallaxSpeed;
                    }
                }

                // Make the background dim even more for a bit of extra suspense before the stars are fired.
                if (AttackTimer >= backgroundDimTime + starCreationRate * starCreationCountPerSide)
                    HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 0.35f, 0.11f);

                // Play mumble sounds.
                if (AttackTimer == backgroundDimTime + starCreationTime - 30f)
                    PerformMumble();
            }

            // Shove hands towards the screen.
            else if (timeSinceStarsWereShoved <= starShoveDelay + attackTransitionDelay)
            {
                // Cast twinkle telegraphs before the shove occurs.
                if (timeSinceStarsWereShoved == 1f)
                {
                    var stars = AllProjectilesByID(ModContent.ProjectileType<BackgroundStar>());
                    foreach (Projectile star in stars)
                    {
                        CreateTwinkle(star.Center, Vector2.One * 1.55f, Color.Transparent, new(Vector2.Zero, () => star.As<BackgroundStar>()?.WorldDestination ?? Vector2.Zero));
                    }
                }

                // Prepare visuals in anticipation of the star shove.
                if (timeSinceStarsWereShoved == starShoveDelay)
                {
                    // Play sounds and create visuals.
                    SoundEngine.PlaySound(FastHandMovementSound);
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.6f, 12);

                    StartShakeAtPoint(NPC.Center, 5.5f);
                }

                // Shove stars. This isn't done all at once for more impact.
                if (timeSinceStarsWereShoved >= starShoveDelay && AttackTimer % 2f == 1f)
                {
                    var stars = AllProjectilesByID(ModContent.ProjectileType<BackgroundStar>()).OrderBy(p => Main.rand.NextFloat());
                    foreach (Projectile star in stars)
                    {
                        if (star.As<BackgroundStar>().ApproachingScreen)
                            continue;

                        int handIndex = (int)star.ai[1] + starCreationCountPerSide;
                        if (handIndex < Hands.Count)
                            Hands[handIndex].ScaleFactor = 1.5f;

                        star.As<BackgroundStar>().ApproachingScreen = true;
                        star.velocity = Vector2.Zero;
                        star.netUpdate = true;
                        break;
                    }
                }

                // Begin the next barrage if ready.
                timeSinceStarsWereShoved++;
                if (timeSinceStarsWereShoved >= starShoveDelay + attackTransitionDelay && barrageCounter < barrageCount - 1f)
                {
                    AttackTimer = backgroundDimTime;
                    timeSinceStarsWereShoved = 0f;
                    barrageCounter++;
                    NPC.netUpdate = true;
                }

                // The stars will increase the background brightness when they explode, ensure that when this happens it returns to its natural levels shortly afterwards.
                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 0.35f, 0.13f);
            }

            else
            {
                ZPosition *= 0.84f;
                HeavenlyBackgroundIntensity = Lerp(HeavenlyBackgroundIntensity, 1f, Main.zenithWorld ? 0.32f : 0.16f);

                if (CurrentPhase >= 1)
                    KaleidoscopeInterpolant = Lerp(KaleidoscopeInterpolant, 0.23f, 0.15f);

                if (HeavenlyBackgroundIntensity >= 0.999f)
                {
                    ZPosition = 0f;
                    HeavenlyBackgroundIntensity = 1f;
                    DestroyAllHands();
                    ClearAllProjectiles();
                }
            }

            // Calculate the background hover position.
            float hoverHorizontalWaveSine = Sin(TwoPi * AttackTimer / 102f);
            float hoverVerticalWaveSine = Sin(TwoPi * AttackTimer / 132f);
            Vector2 hoverDestination = Target.Center + new Vector2(Target.Velocity.X * 14.5f, ZPosition * -40f - 180f);
            hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 36f;
            hoverDestination.Y -= hoverVerticalWaveSine * ZPosition * 8f;

            // Stay above the target while in the background.
            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.03f);
            NPC.SmoothFlyNear(hoverDestination, 0.07f, 0.94f);

            // Move hands.
            if (Hands.Count >= 2)
            {
                int handIndex = 0;
                foreach (var hand in Hands)
                {
                    hand.ScaleFactor = Lerp(hand.ScaleFactor, 1f, 0.09f);
                    hand.RotationOffset = 0f;
                    hand.DirectionOverride = 0;
                    hand.HasArms = true;

                    Vector2 handHoverOffsetVector = (TwoPi * handIndex / Hands.Count + PiOver2 - 0.35f).ToRotationVector2() * new Vector2(2f, 0.9f) * handHoverOffset;
                    handHoverOffsetVector.Y += 350f;
                    DefaultHandDrift(hand, NPC.Center + handHoverOffsetVector, 0.3f);
                    handIndex++;
                }

                Hands[0].ScaleFactor = Lerp(Hands[0].ScaleFactor, 1f, 0.09f);
                Hands[1].ScaleFactor = Lerp(Hands[1].ScaleFactor, 1f, 0.09f);
                Hands[0].RotationOffset = 0f;
                Hands[1].RotationOffset = 0f;
                Hands[0].DirectionOverride = 0;
                Hands[1].DirectionOverride = 0;
                Hands[0].HasArms = true;
                Hands[1].HasArms = true;

                DefaultHandDrift(Hands[0], rightHandHoverPosition, 4f);
                DefaultHandDrift(Hands[1], leftHandHoverPosition, 4f);
            }
        }
    }
}
