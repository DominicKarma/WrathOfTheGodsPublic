using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public float PunchOffsetAngle
        {
            get;
            set;
        }

        public Vector2 PunchDestination
        {
            get;
            set;
        }

        public static int RealityTearPunches_RedirectTime
        {
            get
            {
                if (Main.zenithWorld)
                    return 6;

                return 11;
            }
        }

        public static int RealityTearPunches_TeleportVisualsTime
        {
            get
            {
                if (Main.zenithWorld)
                    return 18;

                return 21;
            }
        }

        public static int RealityTearPunches_HandCount => 10;

        public static int RealityTearPunches_HandSummonRate => 3;

        public static int RealityTearPunches_HandSummonTime => RealityTearPunches_HandCount * RealityTearPunches_HandSummonRate;

        public static int RealityTearPunches_HandEnergyChargeUpTime
        {
            get
            {
                return 42;
            }
        }

        public static int RealityTearPunches_HandRepositionTime
        {
            get
            {
                return 10;
            }
        }

        public static int RealityTearPunches_HandCollideTime
        {
            get
            {
                return 14;
            }
        }

        public static int RealityTearPunches_HandStunTime
        {
            get
            {
                return 80;
            }
        }

        public static int RealityTearPunches_AttackDuration
        {
            get
            {
                int handAppearTime = RealityTearPunches_RedirectTime + RealityTearPunches_HandSummonTime;
                int handAttackTime = RealityTearPunches_HandEnergyChargeUpTime + RealityTearPunches_HandRepositionTime + RealityTearPunches_HandCollideTime + RealityTearPunches_HandStunTime;
                return handAppearTime + handAttackTime;
            }
        }

        public void LoadStateTransitions_RealityTearPunches()
        {
            // Load the transition from RealityTearPunches to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.RealityTearPunches, null, false, () =>
            {
                return AttackTimer >= RealityTearPunches_AttackDuration;
            });
        }

        public void DoBehavior_RealityTearPunches()
        {
            int redirectTime = RealityTearPunches_RedirectTime;
            int handSummonTime = RealityTearPunches_HandSummonTime;
            int teleportVisualsTime = RealityTearPunches_TeleportVisualsTime;
            int handEnergyChargeUpTime = RealityTearPunches_HandEnergyChargeUpTime;
            int handRepositionTime = RealityTearPunches_HandRepositionTime;
            int handCollideTime = RealityTearPunches_HandCollideTime;
            int handCollideStunTime = RealityTearPunches_HandStunTime;
            int handAttackTimer = (AttackTimer - redirectTime - handSummonTime) % (handEnergyChargeUpTime + handRepositionTime + handCollideTime + handCollideStunTime);
            int screenSliceCount = 6;
            int sliceTelegraphDelay = 41;
            float maxIdleHandSpinSpeed = ToRadians(0.14f);
            float handMoveSpeedFactor = 2.8f;
            Vector2 handOffsetRadius = new(732f, 396f);
            Vector2 idealHandOffsetFromPlayer1 = Vector2.Zero;
            Vector2 idealHandOffsetFromPlayer2 = Vector2.Zero;
            ref float idleHandSpinAngularOffset = ref NPC.ai[2];
            ref float usedHandIndex = ref NPC.ai[3];

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Teleport and hover to the top left/right of the target at first.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 450f, -360f);
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.27f);
                NPC.velocity *= 0.75f;
                usedHandIndex = -1f;

                // Initialize the punch offset angle and teleport to the target.
                if (AttackTimer == 1f)
                {
                    StartTeleportAnimation(() => Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 450f, -360f), 11, 11);
                    PunchOffsetAngle = Target.Velocity.SafeNormalize(Main.rand.NextVector2Unit()).ToRotation() + Main.rand.NextFloatDirection() * 0.9f + PiOver4;
                    NPC.netUpdate = true;
                }

                // Perform teleport visuals.
                TeleportVisualsInterpolant = Lerp(0.5f, 1f, InverseLerp(1f, teleportVisualsTime + 1f, AttackTimer));
                if (TeleportVisualsInterpolant >= 1f)
                    TeleportVisualsInterpolant = 0f;
            }

            // Hover near the target and conjure hands.
            else if (AttackTimer <= redirectTime + handSummonTime)
            {
                // Summon hands.
                if (AttackTimer % RealityTearPunches_HandSummonRate == 0f)
                {
                    Vector2 handOffset = (TwoPi * usedHandIndex / RealityTearPunches_HandCount + idleHandSpinAngularOffset).ToRotationVector2() * handOffsetRadius;
                    ConjureHandsAtPosition(NPC.Center + handOffset, Vector2.Zero);
                }

                // Hover near the target.
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 524f, -382f);
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 23f, 0.6f);

                // Prepare the hands for attacking.
                if (AttackTimer == redirectTime + handSummonTime)
                {
                    usedHandIndex = 2f;
                    NPC.netUpdate = true;
                }
            }

            // Hover above the player while the hands attack.
            else
            {
                NamelessDeityHand leftHand = Hands[(int)usedHandIndex];
                NamelessDeityHand rightHand = Hands[(int)((usedHandIndex + Hands.Count / 2) % Hands.Count)];

                leftHand.PositionalDirectionOverride = 1;
                rightHand.PositionalDirectionOverride = 1;

                // Hover above the target.
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 560f, -380f);
                NPC.SmoothFlyNear(hoverDestination, 0.06f, 0.94f);

                // Make the first hand wait and reel back in anticipation of the arc punch, while the other one silently waits to the side.
                if (handAttackTimer <= handEnergyChargeUpTime)
                {
                    float anticipationReelBackDistance = Pow(InverseLerp(-15f, -3f, handAttackTimer - handEnergyChargeUpTime), 1.7f) * 200f;
                    float handHoverOffsetAngle = Convert01To010(handAttackTimer / handEnergyChargeUpTime) * 0.51f;
                    Vector2 generalHandOffset = Vector2.UnitX.RotatedBy(handHoverOffsetAngle + PunchOffsetAngle) * 320f;
                    Vector2 punchingHandOffset = generalHandOffset + generalHandOffset.SafeNormalize(Vector2.UnitY) * anticipationReelBackDistance - Vector2.UnitY * anticipationReelBackDistance * 0.5f;
                    idealHandOffsetFromPlayer1 = punchingHandOffset;
                    idealHandOffsetFromPlayer2 = -generalHandOffset;

                    // Create charge animation particles from the punching hand.
                    if (handAttackTimer % 10f == 0f)
                    {
                        SoundEngine.PlaySound(SunFireballShootSound, Target.Center);
                        Color energyColor = Color.Lerp(Color.Coral, DialogColorRegistry.NamelessDeityTextColor, Main.rand.NextFloat(0.25f, 0.825f)) * 0.8f;
                        PulseRing ring = new(leftHand.Center, Vector2.Zero, energyColor, 3.2f, 0f, 30);
                        ring.Spawn();
                        StrongBloom bloom = new(leftHand.Center, Vector2.Zero, energyColor, 1.6f, 25);
                        bloom.Spawn();
                        ring = new(rightHand.Center, Vector2.Zero, energyColor, 3.2f, 0f, 30);
                        ring.Spawn();
                        bloom = new(rightHand.Center, Vector2.Zero, energyColor, 1.6f, 25);
                        bloom.Spawn();
                    }

                    // Decide the punch destination right before it happens.
                    if (handAttackTimer == handEnergyChargeUpTime - 1f)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with
                        {
                            Volume = 8f,
                            MaxInstances = 10,
                            Pitch = -0.5f
                        });
                        Vector2 handCenterOfMass = (leftHand.Center + rightHand.Center) * 0.5f;
                        ScreenEffectSystem.SetBlurEffect(handCenterOfMass, 0.67f, 10);
                        RadialScreenShoveSystem.Start(handCenterOfMass, 20);
                        PunchDestination = Vector2.Lerp(leftHand.Center, Target.Center, 0.89f);
                        leftHand.ClearPositionCache();
                        leftHand.TrailOpacity = 1f;
                        NPC.netUpdate = true;
                    }
                }

                // Reposition in preparation of the collision punch.
                else if (handAttackTimer <= handEnergyChargeUpTime + handRepositionTime)
                {
                    // Calculate hand offset information.
                    ulong offsetAngleSeed = (ulong)(usedHandIndex + 74f);
                    float handHoverOffsetAngle = Lerp(-0.1f, 0.1f, Utils.RandomFloat(ref offsetAngleSeed)) + PunchOffsetAngle;
                    float handHoverOffsetDistance = Lerp(330f, 1900f, Pow(InverseLerp(0f, handRepositionTime, handAttackTimer - handEnergyChargeUpTime), 2.3f));
                    Vector2 handOffset = Vector2.UnitX.RotatedBy(handHoverOffsetAngle) * handHoverOffsetDistance;
                    idealHandOffsetFromPlayer1 = handOffset;
                    idealHandOffsetFromPlayer2 = -handOffset;

                    // Completely disable the trail opacity and damage from the previous state.
                    leftHand.TrailOpacity = 0f;

                    handMoveSpeedFactor = 4f;
                    PunchDestination = Target.Center;
                }

                // Make the hands punch each other and create slices.
                else if (handAttackTimer <= handEnergyChargeUpTime + handRepositionTime + handCollideTime)
                {
                    Vector2 handCenter = (leftHand.Center + rightHand.Center) * 0.5f;
                    idealHandOffsetFromPlayer1 = -Target.Center + PunchDestination + handCenter - leftHand.Center;
                    idealHandOffsetFromPlayer2 = -Target.Center + PunchDestination + handCenter - rightHand.Center;

                    // Reset the trail caches on the first frame.
                    if (handAttackTimer == handEnergyChargeUpTime + handRepositionTime + 1f)
                    {
                        leftHand.ClearPositionCache();
                        rightHand.ClearPositionCache();
                    }

                    // Create slices.
                    if (handAttackTimer == handEnergyChargeUpTime + handRepositionTime + 11f)
                    {
                        SoundEngine.PlaySound(SliceSound);
                        SoundEngine.PlaySound(GenericBurstSound);
                        SoundEngine.PlaySound(RealityTearSound with { Volume = 1.1f });
                        leftHand.Velocity *= -0.14f;
                        rightHand.Velocity *= -0.14f;
                        StartShake(11f);

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 impactPoint = (leftHand.Center + rightHand.Center) * 0.5f;
                            NewProjectileBetter(NPC.GetSource_FromAI(), impactPoint, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);

                            float angleToTarget = Target.Center.AngleFrom(impactPoint);
                            for (int i = 0; i < screenSliceCount; i++)
                            {
                                Vector2 screenSliceDirection = (TwoPi * i / screenSliceCount + angleToTarget).ToRotationVector2();
                                NewProjectileBetter(NPC.GetSource_FromAI(), impactPoint - screenSliceDirection * 2000f, screenSliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), ScreenSliceDamage, 0f, -1, sliceTelegraphDelay + (int)(i / 2f) * 3, 4000f);
                            }
                            leftHand.TrailOpacity = 0f;
                            rightHand.TrailOpacity = 0f;
                        }
                    }

                    leftHand.CanDoDamage = true;
                    rightHand.CanDoDamage = true;

                    // Make both hands look into their general direction and use the trail while moving.
                    if (handAttackTimer < handEnergyChargeUpTime + handRepositionTime + 7f)
                    {
                        // Make the trails draw.
                        leftHand.TrailOpacity = 1f;
                        rightHand.TrailOpacity = 1f;

                        handMoveSpeedFactor = 6f;
                    }
                }
                else
                {
                    leftHand.Velocity = leftHand.Velocity.ClampLength(0f, 3f) * 0.8f;
                    rightHand.Velocity = rightHand.Velocity.ClampLength(0f, 3f) * 0.8f;
                    handMoveSpeedFactor = 0.03f;
                }

                if (handAttackTimer >= RealityTearPunches_AttackDuration - 1)
                {
                    leftHand.CanDoDamage = false;
                    rightHand.CanDoDamage = false;
                    DestroyAllHands();
                    NPC.netUpdate = true;
                }
            }

            // Make hands idly move.
            float handSpinSpeed = maxIdleHandSpinSpeed * InverseLerp(0f, 30f, AttackTimer - redirectTime);
            idleHandSpinAngularOffset += handSpinSpeed;

            // Update hands.
            for (int i = 0; i < Hands.Count; i++)
            {
                NamelessDeityHand hand = Hands[i];
                Vector2 handOffset = (TwoPi * i / Hands.Count + idleHandSpinAngularOffset).ToRotationVector2() * new Vector2(1.55f, 1.9f) * handOffsetRadius;
                if (Abs(handOffset.X) < 600f)
                    handOffset.X = Sign(handOffset.X) * 600f;
                handOffset.Y -= 600f;

                Vector2 handDestination = NPC.Center + handOffset;

                hand.HasArms = true;

                // Instruct the hands to move towards a preset offset from the target if this hand is the one being used.
                float localHandMoveSpeedFactor = 5f;
                if (i == usedHandIndex)
                {
                    if (idealHandOffsetFromPlayer1 != Vector2.Zero)
                        handDestination = Target.Center + idealHandOffsetFromPlayer1;
                    localHandMoveSpeedFactor = handMoveSpeedFactor;
                    hand.HasArms = false;

                    if (handAttackTimer <= handEnergyChargeUpTime + handRepositionTime + handCollideTime - 8f)
                        hand.RotationOffset = hand.Center.AngleTo(Target.Center) + Pi;
                    hand.DirectionOverride = 1;
                    hand.PositionalDirectionOverride = 1;
                }
                if ((i + Hands.Count / 2) % Hands.Count == usedHandIndex)
                {
                    if (idealHandOffsetFromPlayer2 != Vector2.Zero)
                        handDestination = Target.Center + idealHandOffsetFromPlayer2;
                    localHandMoveSpeedFactor = handMoveSpeedFactor;
                    hand.HasArms = false;

                    if (handAttackTimer <= handEnergyChargeUpTime + handRepositionTime + handCollideTime - 8f)
                        hand.RotationOffset = hand.Center.AngleTo(Target.Center) + Pi;
                    hand.DirectionOverride = 1;
                    hand.PositionalDirectionOverride = 1;
                }

                DefaultHandDrift(hand, handDestination, localHandMoveSpeedFactor);
                hand.ScaleFactor = 1f;
            }
        }
    }
}
