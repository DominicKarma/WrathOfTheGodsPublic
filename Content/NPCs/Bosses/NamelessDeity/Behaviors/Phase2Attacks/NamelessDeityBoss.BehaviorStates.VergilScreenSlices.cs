using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public static int VergilScreenSlices_SliceShootDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 40;

                return 32;
            }
        }

        public static int VergilScreenSlices_SliceReleaseRate
        {
            get
            {
                if (Main.zenithWorld)
                    return 3;

                return 5;
            }
        }

        public static int VergilScreenSlices_SliceReleaseCount
        {
            get
            {
                if (Main.zenithWorld)
                    return 11;

                return 9;
            }
        }

        public static int VergilScreenSlices_SliceReleaseTime => VergilScreenSlices_SliceReleaseRate * VergilScreenSlices_SliceReleaseCount + 25;

        public static int VergilScreenSlices_AttackDelay
        {
            get
            {
                return 10;
            }
        }

        public static int VergilScreenSlices_AttackTransitionDelay
        {
            get
            {
                return 32;
            }
        }

        public static int VegilScreenSlices_AttackDuration => VergilScreenSlices_SliceShootDelay + VergilScreenSlices_SliceReleaseTime + VergilScreenSlices_AttackDelay + VergilScreenSlices_AttackTransitionDelay;

        public void LoadStateTransitions_VergilScreenSlices()
        {
            // Load the transition from VergilScreenSlices to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.VergilScreenSlices, null, false, () =>
            {
                return AttackTimer >= VegilScreenSlices_AttackDuration;
            });
        }

        public void DoBehavior_VergilScreenSlices()
        {
            int sliceShootDelay = VergilScreenSlices_SliceShootDelay;
            int sliceReleaseRate = VergilScreenSlices_SliceReleaseRate;
            int sliceReleaseCount = VergilScreenSlices_SliceReleaseCount;
            int sliceReleaseTime = VergilScreenSlices_SliceReleaseTime;
            int fireDelay = VergilScreenSlices_AttackDelay;
            int teleportDelay = 15;
            float sliceLength = 3200f;
            ref float sliceCounter = ref NPC.ai[2];

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Update universal hands.
            DefaultUniversalHandMotion();

            if (AttackTimer <= sliceShootDelay)
            {
                // Play mumble sounds.
                if (AttackTimer == 1f)
                    PerformMumble();

                // Hover above the target at first.
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 442f;
                NPC.velocity = (hoverDestination - NPC.Center) * 0.1f;

                // Use teleport visuals.
                if (AttackTimer >= sliceShootDelay - teleportDelay)
                {
                    if (AttackTimer == sliceShootDelay - teleportDelay + 1f)
                        SoundEngine.PlaySound(TeleportInSound, NPC.Center);

                    TeleportVisualsInterpolant = InverseLerp(sliceShootDelay - teleportDelay, sliceShootDelay - 1f, AttackTimer) * 0.5f;
                }

                // Teleport away after hovering.
                if (AttackTimer == sliceShootDelay - 1f)
                {
                    RadialScreenShoveSystem.Start(EyePosition, 45);
                    ImmediateTeleportTo(Target.Center + Vector2.UnitY * 2000f);
                }

                // Dim the background for suspense.
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.032f, 0.5f, 1f);

                return;
            }

            // Reset the teleport visuals interpolant after the teleport has concluded.
            TeleportVisualsInterpolant = 0f;

            // Stay invisible.
            NPC.Opacity = 0f;

            // Release slice telegraphs around the player.
            if (AttackTimer % sliceReleaseRate == 0f && sliceCounter < sliceReleaseCount)
            {
                SoundEngine.PlaySound(SliceSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int telegraphTime = sliceReleaseTime - (int)(AttackTimer - sliceShootDelay) + (int)sliceCounter * 2 + fireDelay;
                    Vector2 sliceSpawnCenter = Target.Center + Main.rand.NextVector2Unit() * (sliceCounter + 35f + Main.rand.NextFloat(600f)) + Target.Velocity * 8f;
                    if (sliceCounter == 0f)
                        sliceSpawnCenter = Target.Center + Main.rand.NextVector2Circular(10f, 10f);

                    Vector2 sliceDirection = new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-6f, 6f)).SafeNormalize(Vector2.UnitX);
                    NewProjectileBetter(sliceSpawnCenter - sliceDirection * sliceLength * 0.5f, sliceDirection, ModContent.ProjectileType<VergilScreenSlice>(), ScreenSliceDamage, 0f, -1, telegraphTime, sliceLength);
                }

                sliceCounter++;
            }

            // Slice the screen.
            if (AttackTimer == sliceShootDelay + sliceReleaseTime + fireDelay)
            {
                // Calculate the center of the slices.
                List<LineSegment> lineSegments = new();
                List<Projectile> slices = AllProjectilesByID(ModContent.ProjectileType<VergilScreenSlice>()).ToList();
                for (int i = 0; i < slices.Count; i++)
                {
                    Vector2 start = slices[i].Center;
                    Vector2 end = start + slices[i].velocity * slices[i].As<VergilScreenSlice>().LineLength;
                    lineSegments.Add(new(start, end));
                }

                ScreenShatterSystem.CreateShatterEffect(lineSegments.ToArray(), 2);
                SoundEngine.PlaySound(GenericBurstSound);
                NamelessDeityKeyboardShader.BrightnessIntensity = 1f;
                StartShake(15f);
            }

            // Make the background come back.
            if (AttackTimer >= sliceShootDelay + sliceReleaseTime + fireDelay)
            {
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity + 0.08f, 0f, 1f);
                RadialScreenShoveSystem.Start(Target.Center - Vector2.UnitY * 400f, 20);
                NPC.Opacity = 1f;
            }

            // Create some screen burn marks.
            if (AttackTimer == sliceShootDelay + sliceReleaseTime + fireDelay + VergilScreenSlices_AttackTransitionDelay / 2)
                LocalScreenSplitBurnAfterimageSystem.TakeSnapshot(180);

            // Stay invisible.
            NPC.Center = Target.Center + Vector2.UnitY * 2000f;
        }
    }
}
