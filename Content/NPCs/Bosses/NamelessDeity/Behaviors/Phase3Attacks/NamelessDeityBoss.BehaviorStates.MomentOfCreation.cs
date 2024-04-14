using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.CosmicBackgroundSystem;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public bool HasExperiencedFinalAttack
        {
            get;
            set;
        }

        public static int MomentOfCreation_GalaxySpawnDelay => 32;

        public static int MomentOfCreation_GalaxyShootTime => SecondsToFrames(Main.zenithWorld ? 90f : 15f);

        public static int MomentOfCreation_FlowerDisappearDelay => 75;

        public static int MomentOfCreation_AttackDuration => DivineRoseSystem.ExplosionDelay + MomentOfCreation_GalaxySpawnDelay + MomentOfCreation_GalaxyShootTime + MomentOfCreation_FlowerDisappearDelay + 75;

        public void LoadStateTransitions_MomentOfCreation()
        {
            // Load the transition from MomentOfCreation to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.MomentOfCreation, null, false, () =>
            {
                return AttackTimer >= MomentOfCreation_AttackDuration;
            });
        }

        public void DoBehavior_MomentOfCreation()
        {
            int backgroundEnterTime = 45;
            int starRecedeTime = DivineRoseSystem.BlackOverlayStartTime - 10;
            int fingerSnapDelay = DivineRoseSystem.AttackDelay;
            int explosionDelay = DivineRoseSystem.ExplosionDelay;
            int galaxySpawnDelay = MomentOfCreation_GalaxySpawnDelay;
            int galaxyShootTime = MomentOfCreation_GalaxyShootTime;
            float idealZPosition = DivineRoseSystem.NamelessDeityZPosition;
            float maxStarZoomout = 0.5f;

            ref float galaxyReleaseTimer = ref NPC.ai[2];
            ref float galaxyCounter = ref NPC.ai[3];
            int galaxyReleaseRate = (int)Clamp(42f - galaxyCounter * 3f, 16f, 120f);

            // Kill the player in GFB.
            if (Main.zenithWorld)
                galaxyReleaseRate -= 12;

            if (Hands.Count < 4)
                ConjureHandsAtPosition(NPC.Center, Vector2.Zero);

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Make the stars return.
            KaleidoscopeInterpolant = 1f - InverseLerpBump(0f, 20f, explosionDelay + galaxySpawnDelay + galaxyShootTime + MomentOfCreation_FlowerDisappearDelay, explosionDelay + galaxySpawnDelay + galaxyShootTime + MomentOfCreation_FlowerDisappearDelay + 25f, AttackTimer);

            // Make the background stars recede.
            float starRecedeInterpolant = InverseLerp(0f, starRecedeTime, AttackTimer);
            if (starRecedeInterpolant < 1f)
            {
                StarZoomIncrement = Pow(starRecedeInterpolant, 7f) * maxStarZoomout;
                HeavenlyBackgroundIntensity = Lerp(1f, 0.04f, Pow(starRecedeInterpolant, 2f));
            }
            else
            {
                StarZoomIncrement = maxStarZoomout;

                if (TotalScreenOverlaySystem.OverlayInterpolant <= 0f)
                    HeavenlyBackgroundIntensity *= 0.9f;
                if (HeavenlyBackgroundIntensity < 0.004f)
                {
                    StarZoomIncrement = 0f;
                    HeavenlyBackgroundIntensity = 0.00001f;
                }
            }

            // Enter the background and fly around the rose.
            float movementSharpness = 0.1f;
            float movementSmoothness = 0.82f;
            Vector2 spinOffset = Vector2.UnitY.RotatedBy(TwoPi * ZPosition * AttackTimer / 800f) * new Vector2(400f, 300f) * (0.6f + ZPosition * 0.06f);
            Vector2 hoverDestination = Target.Center + DivineRoseSystem.RoseOffsetFromScreenCenter + spinOffset;
            if (AttackTimer >= fingerSnapDelay - 30f)
            {
                hoverDestination = Target.Center + DivineRoseSystem.RoseOffsetFromScreenCenter + Vector2.UnitY * 120f;
                movementSharpness = 0.3f;
                movementSmoothness = 0.5f;
            }

            // Perform Z position and hover movement.
            if (AttackTimer < explosionDelay + galaxySpawnDelay + galaxyShootTime + MomentOfCreation_FlowerDisappearDelay)
            {
                ZPosition = Pow(Clamp(AttackTimer / (float)backgroundEnterTime, 0f, 1f), 1.6f) * idealZPosition;
                NPC.SmoothFlyNear(hoverDestination, movementSharpness, movementSmoothness);
            }

            // Darken the screen when ready.
            if (AttackTimer == DivineRoseSystem.BlackOverlayStartTime - 4)
                SoundEngine.PlaySound(GlitchSound);
            if (AttackTimer >= DivineRoseSystem.BlackOverlayStartTime - 4 && AttackTimer <= DivineRoseSystem.BlackOverlayStartTime + 6)
            {
                TotalScreenOverlaySystem.OverlayInterpolant = 1.6f;
                TotalScreenOverlaySystem.OverlayColor = Color.Black;
            }

            // Make the rose explode into a bunch of galaxies.
            if (AttackTimer == explosionDelay)
            {
                Vector2 censorPosition = Target.Center + DivineRoseSystem.RoseOffsetFromScreenCenter + DivineRoseSystem.BaseCensorOffset;

                StartShake(20f);
                SoundEngine.PlaySound(MomentOfCreationSound);
                ScreenEffectSystem.SetChromaticAberrationEffect(censorPosition, 1f, 120);
                NamelessDeityKeyboardShader.BrightnessIntensity = 1f;

                if (Main.netMode == NetmodeID.SinglePlayer)
                    NewProjectileBetter(NPC.GetSource_FromAI(), censorPosition, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
            }

            // Perform galaxy spawning and timing behaviors after the explosion has happened.
            if (AttackTimer >= explosionDelay + galaxySpawnDelay && AttackTimer < explosionDelay + galaxySpawnDelay + galaxyShootTime)
            {
                galaxyReleaseTimer++;

                // Create galaxies that fall from above.
                if (galaxyReleaseTimer >= galaxyReleaseRate)
                {
                    SoundEngine.PlaySound(GalaxyTelegraphSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 telegraphSpawnPosition = Target.Center + Vector2.UnitX * (Main.rand.NextFloatDirection() * 900f + Target.Velocity.X * Main.rand.NextFloat(30f, 45f));
                        NewProjectileBetter(NPC.GetSource_FromAI(), telegraphSpawnPosition, Vector2.UnitY, ModContent.ProjectileType<FallingGalaxy>(), GalaxyDamage, 0f);
                    }

                    galaxyReleaseTimer = 0f;
                    galaxyCounter++;
                    NPC.netUpdate = true;
                }
            }

            // Make the flower disappear when done shooting.
            if (AttackTimer == explosionDelay + galaxySpawnDelay + galaxyShootTime + 75f)
            {
                SoundEngine.PlaySound(GlitchSound);
                TotalScreenOverlaySystem.OverlayInterpolant = 1.7f;
                TotalScreenOverlaySystem.OverlayColor = Color.Black;
                ZPosition = 0f;
                NPC.Center = Target.Center - Vector2.UnitY * 350f;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
            }

            if (AttackTimer >= explosionDelay + galaxySpawnDelay + galaxyShootTime + MomentOfCreation_FlowerDisappearDelay)
            {
                HeavenlyBackgroundIntensity = InverseLerp(75f, 145f, AttackTimer - explosionDelay - galaxySpawnDelay - galaxyShootTime);
                StarZoomIncrement = 0f;
                KaleidoscopeInterpolant = Clamp(KaleidoscopeInterpolant + 0.05f, 0f, 1f);
            }

            // Mumble before snapping fingers.
            if (AttackTimer == fingerSnapDelay - 50f)
                PerformMumble();

            // Update hands.
            if (Hands.Count >= 4)
            {
                Vector2[] handOffsets = new Vector2[4]
                {
                    new(-1080f, -500f),
                    new(1080f, -500f),
                    new(900f, 120f),
                    new(-900f, 120f)
                };

                for (int i = 0; i < handOffsets.Length; i++)
                {
                    // Apply randomness to the hand offsets.
                    ulong seed = (ulong)(i + NPC.whoAmI * 717);
                    float randomOffset = Lerp(30f, 108f, Utils.RandomFloat(ref seed));
                    Vector2 randomDirection = (Utils.RandomFloat(ref seed) * TwoPi * 3f + FightLength * (i - 3.4f) / 7f).ToRotationVector2() * new Vector2(1.61f, 1.1f);
                    handOffsets[i] += randomDirection * randomOffset;

                    // Move hands.
                    DefaultHandDrift(Hands[i], NPC.Center + handOffsets[i], 6f);
                }

                Hands[0].RotationOffset = 0.2f;
                Hands[1].RotationOffset = -0.2f;

                // Snap fingers and make the screen shake.
                if (AttackTimer == fingerSnapDelay)
                {
                    SoundEngine.PlaySound(FingerSnapSound with
                    {
                        Volume = 4f
                    });
                    StartShakeAtPoint(NPC.Center, 9f);
                }
            }
        }
    }
}
