using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public ref float RealityTearDaggers_AttackTransitionTimer => ref NPC.ai[3];

        public void LoadStateTransitions_RealityTearDaggers()
        {
            // Load the transition from RealityTearDaggers to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.RealityTearDaggers, null, false, () =>
            {
                return RealityTearDaggers_AttackTransitionTimer >= 84f;
            });
        }

        public void DoBehavior_RealityTearDaggers()
        {
            int riseTime = 14;
            int sliceTelegraphTime = 31;
            int screenSliceRate = sliceTelegraphTime + 11;
            int totalHorizontalSlices = 5;
            int totalRadialSlices = 5;
            int handWaveTime = 27;
            int totalHands = 6;
            float sliceTelegraphLength = 2800f;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                riseTime = 7;
                sliceTelegraphTime = 18;
                screenSliceRate = sliceTelegraphTime + 11;
                totalHorizontalSlices = 4;
                totalRadialSlices = 4;
            }

            int totalSlices = totalHorizontalSlices + totalRadialSlices;
            float wrappedAttackTimer = AttackTimer % screenSliceRate;
            ref float sliceCounter = ref NPC.ai[2];

            // Calculate slice information.
            Vector2 sliceDirection = Vector2.UnitX;
            Vector2 sliceSpawnOffset = Vector2.Zero;
            if (sliceCounter % totalSlices >= totalHorizontalSlices)
            {
                sliceDirection = sliceDirection.RotatedBy(TwoPi * (sliceCounter - totalHorizontalSlices) / totalRadialSlices);
                sliceSpawnOffset += sliceDirection.RotatedBy(PiOver2) * 400f;
            }

            // Disallow arm variant 4 for this attack because the angles look weird.
            if ((HandTexture?.TextureVariant ?? 0) == 3)
                HandTexture?.Swap();

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Move into the background.
            if (AttackTimer <= riseTime)
                ZPosition = Pow(AttackTimer / (float)riseTime, 1.6f) * 2.4f;

            // Calculate the background hover position.
            float hoverHorizontalWaveSine = Sin(TwoPi * AttackTimer / 96f);
            float hoverVerticalWaveSine = Sin(TwoPi * AttackTimer / 120f);
            Vector2 hoverDestination = Target.Center + new Vector2(Target.Velocity.X * 14.5f, ZPosition * -40f - 200f);
            hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 40f;
            hoverDestination.Y -= hoverVerticalWaveSine * ZPosition * 8f;
            if (Main.zenithWorld)
                hoverDestination += Main.rand.NextVector2Circular(1200f, 500f);

            // Stay above the target while in the background.
            NPC.SmoothFlyNear(hoverDestination, 0.07f, 0.94f);

            // Create hands.
            if (AttackTimer == 1f)
            {
                for (int i = TotalUniversalHands; i < totalHands; i++)
                {
                    Vector2 handOffset = (TwoPi * i / totalHands).ToRotationVector2() * NPC.scale * 400f;
                    if (Abs(handOffset.X) <= 0.001f)
                        handOffset.X = 0f;

                    ConjureHandsAtPosition(NPC.Center + handOffset, sliceDirection * 3f);
                }

                // Play mumble sounds.
                PerformMumble();
            }

            // Operate hands that move in the direction of the slice.
            if (Hands.Any())
            {
                // Update the hands.
                for (int i = 0; i < Hands.Count; i++)
                {
                    // Hands extend outwards shortly before a barrage of daggers is fired.
                    float handExtendInterpolant = InverseLerp(14f, 24f, wrappedAttackTimer - sliceTelegraphTime);
                    if (wrappedAttackTimer - sliceTelegraphTime <= -7f && sliceCounter >= 1f)
                        handExtendInterpolant = 1f;

                    float handExtendFactor = Lerp(2.6f, 1.46f, handExtendInterpolant);
                    float handDriftSpeed = Remap(ZPosition, 1.1f, 0.36f, 1.9f, 0.8f);

                    NamelessDeityHand hand = Hands[i];

                    // Group hands such that they prefer being to the sides before moving upward in a commanding pose when daggers are about to fire.
                    Vector2 hoverOffset = new Vector2((i % 2 == 0).ToDirectionInt() * 500f, 30f) * handExtendFactor;
                    hoverOffset.X = Lerp(hoverOffset.X, hoverOffset.X.NonZeroSign() * 150f, handExtendInterpolant * 0.48f);
                    hoverOffset.Y -= handExtendInterpolant * 750f - 60f;

                    int sideIndependentIndex = i / 2;
                    if (sideIndependentIndex != (int)(sliceCounter % (totalHands / 2)))
                    {
                        float verticalOffset = Cos01(AttackTimer / 12f) * (sideIndependentIndex * 80f + 150f);
                        hoverOffset = new Vector2((i % 2 == 0).ToDirectionInt() * (900f - sideIndependentIndex * 90f), 50f - sideIndependentIndex * 150f - verticalOffset);
                        hoverOffset = Vector2.Lerp(hoverOffset, -Vector2.UnitY * 600f, sideIndependentIndex * 0.1f);
                    }

                    // Moves the hands to their end position.
                    Vector2 handDestination = NPC.Center + hoverOffset;
                    hand.Center = Vector2.Lerp(hand.Center, handDestination, wrappedAttackTimer / handWaveTime * Sqrt(handExtendInterpolant) * 0.6f + 0.02f);

                    // Perform update code.
                    DefaultHandDrift(hand, handDestination, handDriftSpeed);
                }
            }

            // Create slices.
            if (wrappedAttackTimer == handWaveTime && AttackTimer >= riseTime + 1f && sliceCounter < totalSlices)
            {
                // Create a reality tear.
                float sliceOffset = 28f;
                if (sliceCounter >= totalHorizontalSlices)
                {
                    sliceDirection = sliceDirection.RotatedBy(Pi / totalRadialSlices);
                    sliceOffset = 0f;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Target.Center - sliceDirection * sliceTelegraphLength * 0.5f + sliceSpawnOffset, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), ScreenSliceDamage, 0f, -1, sliceTelegraphTime, sliceTelegraphLength, sliceOffset);

                SoundEngine.PlaySound(RealityTearSound with { Volume = 0.56f });
                NamelessDeityKeyboardShader.BrightnessIntensity += 0.6f;

                sliceCounter++;
                NPC.netUpdate = true;
            }

            // Return to the foreground and destroy hands after doing the slices.
            if (sliceCounter >= totalSlices)
            {
                ZPosition = Lerp(ZPosition, 0f, 0.06f);

                // Destroy the hands after enough time has passed.
                if (RealityTearDaggers_AttackTransitionTimer == 25f)
                    DestroyAllHands();

                RealityTearDaggers_AttackTransitionTimer++;
            }
        }
    }
}
