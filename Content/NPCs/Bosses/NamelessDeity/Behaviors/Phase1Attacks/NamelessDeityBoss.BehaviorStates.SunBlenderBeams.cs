using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public int SunBlenderBeams_AttackDelay
        {
            get
            {
                int starGrowTime = ControlledStar.GrowToFullSizeTime;
                int attackDelay = starGrowTime + 40;
                if (CurrentPhase >= 1)
                    attackDelay -= 10;

                return attackDelay;
            }
        }

        public int SunBlenderBeams_AttackDuration
        {
            get
            {
                int duration = 450;
                if (Main.zenithWorld)
                    duration -= 90;
                if (CurrentPhase >= 1)
                    duration -= 45;

                return duration;
            }
        }

        public float RelativeDarkening
        {
            get;
            set;
        }

        public void LoadStateTransitions_SunBlenderBeams()
        {
            // Load the transition from SunBlenderBeams to the next in the cycle.
            // These happens if either there's no star projectiles or the attack timer has reached its limit.
            StateMachine.RegisterTransition(NamelessAIType.SunBlenderBeams, null, false, () =>
            {
                return AttackTimer >= SunBlenderBeams_AttackDelay + SunBlenderBeams_AttackDuration || (!AnyProjectiles(ModContent.ProjectileType<ControlledStar>()) && AttackTimer >= 5);
            }, () => StarShouldBeHeldByLeftHand = false);
        }

        public void DoBehavior_SunBlenderBeams()
        {
            int starGrowTime = ControlledStar.GrowToFullSizeTime;
            int attackDelay = SunBlenderBeams_AttackDelay;
            int flareShootCount = 4;
            int laserShootTime = 32;
            int attackDuration = SunBlenderBeams_AttackDuration;
            int starburstReleaseRate = 64;
            int starburstCount = 16;
            int teleportVisualsTime = 20;
            int baseFlareCount = 2;
            int minTelegraphTime = 23;
            int maxTelegraphTime = 60;
            float starburstStartingSpeed = 0.6f;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                flareShootCount += 3;
                laserShootTime = 16;
                starburstReleaseRate = 16;
                minTelegraphTime = 11;
                maxTelegraphTime = 34;
                baseFlareCount = 9;
            }

            ref float flareShootCounter = ref NPC.ai[2];

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
            {
                baseFlareCount += 2;
                minTelegraphTime -= 4;
                maxTelegraphTime -= 13;
            }

            // Use the robed arm variant.
            HandTexture?.ForceToVariant(0);
            ArmTexture?.ForceToVariant(0);
            ForearmTexture?.ForceToVariant(0);

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Create a suitable star and two hands on the first frame.
            // The star will give the appearance of actually coming from the background.
            if (AttackTimer == 1)
            {
                // Teleport behind the target.
                StartTeleportAnimation(() => Target.Center - Vector2.UnitX * TargetDirection * 400f, teleportVisualsTime / 2, teleportVisualsTime / 2);

                // Create the star.
                Vector2 starSpawnPosition = NPC.Center + new Vector2(300f, -350f) * TeleportVisualsAdjustedScale;
                CreateTwinkle(starSpawnPosition, Vector2.One * 1.3f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(starSpawnPosition, Vector2.Zero, ModContent.ProjectileType<ControlledStar>(), 0, 0f);

                // Play mumble sounds.
                PerformMumble();
                return;
            }

            // Decide which hand should hold the start.
            if (AttackTimer <= 10)
                StarShouldBeHeldByLeftHand = NPC.Center.X > Target.Center.X;

            if (AttackTimer == 52f)
            {
                SoundEngine.PlaySound(FingerSnapSound with
                {
                    Volume = 4f
                });
                NamelessDeityKeyboardShader.BrightnessIntensity += 0.45f;
            }

            // Verify that a star actually exists. If not, terminate this attack immediately.
            List<Projectile> stars = AllProjectilesByID(ModContent.ProjectileType<ControlledStar>()).ToList();
            if (!stars.Any())
            {
                DestroyAllHands();
                flareShootCounter = 0f;
                AttackTimer = 0;
                return;
            }

            // Become darker.
            RelativeDarkening = Clamp(RelativeDarkening + 0.06f, 0f, 0.5f);
            NamelessDeitySky.HeavenlyBackgroundIntensity = 1f - RelativeDarkening;
            NPC.Opacity = 1f;

            // Fly to the side of the target before the attack begins.
            if (AttackTimer < attackDelay)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 792f, -250f);
                NPC.SmoothFlyNear(hoverDestination, 0.12f, 0.87f);
            }

            // Drift towards the target once the attack has begun.
            else
            {
                // Rapidly slow down if there's any remnant speed from prior movement.
                if (NPC.velocity.Length() > 4f)
                    NPC.velocity *= 0.8f;

                float hoverSpeedInterpolant = Remap(NPC.Distance(Target.Center), 980f, 1900f, 0.0018f, 0.04f);
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center, hoverSpeedInterpolant);
                NPC.SimpleFlyMovement(NPC.DirectionToSafe(Target.Center) * 4f, 0.1f);
            }

            // Find the star to control.
            Projectile star = stars.First();

            // Crate a light wave if the star released lasers.
            if (NPC.ai[3] == 1f)
            {
                RadialScreenShoveSystem.Start(EyePosition, 35);

                // Brighten the screen for a short moment.
                ScreenEffectSystem.SetFlashEffect(star.Center, 2f, 24);

                NamelessDeityKeyboardShader.BrightnessIntensity += 0.72f;
                NPC.ai[3] = 0f;
                NPC.netUpdate = true;
            }

            // Update hand positions.
            int handHoldingStarIndex = StarShouldBeHeldByLeftHand ? 0 : 1;
            Vector2 leftHandOffset = Vector2.Zero;
            Vector2 rightHandOffset = Vector2.Zero;
            Vector2 hoverVerticalOffset = Vector2.UnitY * Cos01(AttackTimer / 13f) * 50f;
            if (StarShouldBeHeldByLeftHand)
                leftHandOffset += new Vector2(-150f, -160f) + hoverVerticalOffset;
            else
                rightHandOffset += new Vector2(150f, -160f) + hoverVerticalOffset;

            DefaultHandDrift(Hands[0], NPC.Center - Vector2.UnitX * TeleportVisualsAdjustedScale * 720f + leftHandOffset, 1.8f);
            DefaultHandDrift(Hands[1], NPC.Center + Vector2.UnitX * TeleportVisualsAdjustedScale * 720f + rightHandOffset, 1.8f);
            Hands[handHoldingStarIndex].RotationOffset = PiOver4 - 0.2f;
            Hands[1 - handHoldingStarIndex].RotationOffset = PiOver4 - 0.32f;
            if (StarShouldBeHeldByLeftHand)
            {
                Hands[handHoldingStarIndex].RotationOffset *= -1f;
                Hands[1 - handHoldingStarIndex].RotationOffset *= -1f;
            }

            // Hold the star in Nameless' right hand.
            float verticalOffset = Convert01To010(InverseLerp(0f, starGrowTime, AttackTimer)) * 175f;
            Vector2 starPosition = Hands[handHoldingStarIndex].Center + (new Vector2(StarShouldBeHeldByLeftHand.ToDirectionInt() * 250f, verticalOffset - 450f) - hoverVerticalOffset) * TeleportVisualsAdjustedScale;
            star.Center = starPosition;

            // Release accelerating bursts of starbursts over time.
            if (AttackTimer >= attackDelay && AttackTimer % starburstReleaseRate == 0f)
            {
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot with
                {
                    Pitch = -0.4f,
                    MaxInstances = 5
                }, star.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int starburstID = ModContent.ProjectileType<Starburst>();
                    int starburstCounter = (int)Round(AttackTimer / starburstReleaseRate);
                    float shootOffsetAngle = starburstCounter % 2 == 0 ? Pi / starburstCount : 0f;
                    if (!NPC.WithinRange(Target.Center, 1450f))
                    {
                        if (Main.rand.NextBool())
                            starburstID = ModContent.ProjectileType<ArcingStarburst>();
                        starburstStartingSpeed *= 5.6f;
                    }

                    for (int i = 0; i < starburstCount; i++)
                    {
                        Vector2 starburstVelocity = star.DirectionToSafe(Target.Center).RotatedBy(TwoPi * i / starburstCount + shootOffsetAngle) * starburstStartingSpeed;
                        NewProjectileBetter(star.Center + starburstVelocity * 3f, starburstVelocity, starburstID, StarburstDamage, 0f);
                    }
                    for (int i = 0; i < starburstCount / 2; i++)
                    {
                        Vector2 starburstVelocity = star.DirectionToSafe(Target.Center).RotatedBy(TwoPi * i / starburstCount / 2f + shootOffsetAngle) * starburstStartingSpeed * 0.6f;
                        NewProjectileBetter(star.Center + starburstVelocity * 3f, starburstVelocity, starburstID, StarburstDamage, 0f);
                    }
                }
            }

            // Release telegraphed solar flare lasers over time. The quantity of lasers increases as time goes on.
            if (AttackTimer >= attackDelay && flareShootCounter < flareShootCount && !AllProjectilesByID(ModContent.ProjectileType<TelegraphedStarLaserbeam>()).Any())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Create the flares.
                    int flareCount = (int)(flareShootCounter * 2f) + baseFlareCount;
                    int flareTelegraphTime = (int)Remap(AttackTimer - attackDelay, 0f, 300f, minTelegraphTime, maxTelegraphTime) + laserShootTime;
                    float flareSpinDirection = (flareShootCounter % 2f == 0f).ToDirectionInt();
                    float flareSpinCoverage = PiOver2 * flareSpinDirection;
                    Vector2 directionToTarget = star.DirectionToSafe(Target.Center);
                    for (int i = 0; i < flareCount; i++)
                    {
                        Vector2 flareDirection = directionToTarget.RotatedBy(TwoPi * i / flareCount);
                        NewProjectileBetter(star.Center, flareDirection, ModContent.ProjectileType<TelegraphedStarLaserbeam>(), SunBeamDamage, 0f, -1, flareTelegraphTime, laserShootTime, flareSpinCoverage / flareTelegraphTime);
                    }

                    flareShootCounter++;
                    NPC.netUpdate = true;
                }
            }
        }
    }
}
