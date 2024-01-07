using System;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Music;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public bool TargetIsUsingRodOfHarmony
        {
            get;
            set;
        }

        public static int EnterPhase2_BackgroundEnterTime => 75;

        public static int EnterPhase2_AttackCooldownTime
        {
            get
            {
                // Make the attack delays a bit shorter outside of Rev+, since otherwise there's an awkwardly wait at the point where the rippers would otherwise be destroyed.
                if (!CommonCalamityVariables.RevengeanceModeActive)
                    return 150;

                return 240;
            }
        }

        public static int EnterPhase2_RodOfHarmonyRantStartDelay => 160;

        public static int EnterPhase2_AttackTransitionDelay => 60;

        public static int EnterPhase3_AttackTransitionDelay => 71;

        public void LoadStateTransitions_Phase2TransitionStart()
        {
            // Prepare to enter phase 2 if ready. This will ensure that once the attack has finished Nameless will enter the second phase.
            StateMachine.AddTransitionStateHijack(originalState =>
            {
                if (CurrentPhase == 0 && WaitingForPhase2Transition && originalState != NamelessAIType.DeathAnimation && originalState != NamelessAIType.DeathAnimation_GFB)
                    return NamelessAIType.EnterPhase2;

                return originalState;
            });

            // As an addendum to the above, this effect happens immediately during the star blender attack instead of waiting due to pacing concerns.
            StateMachine.RegisterTransition(NamelessAIType.SunBlenderBeams, NamelessAIType.EnterPhase2, false, () =>
            {
                return LifeRatio <= Phase2LifeRatio && CurrentPhase == 0;
            });
        }

        public void LoadStateTransitions_Phase2TransitionEnd()
        {
            // Load the transition from EnterPhase2 to the rod of harmony rant.
            StateMachine.RegisterTransition(NamelessAIType.EnterPhase2, NamelessAIType.RodOfHarmonyRant, false, () =>
            {
                return TargetIsUsingRodOfHarmony && AttackTimer >= EnterPhase2_AttackCooldownTime + EnterPhase2_RodOfHarmonyRantStartDelay;
            });

            // Load the transition from EnterPhase2 to the regular cycle.
            StateMachine.RegisterTransition(NamelessAIType.EnterPhase2, NamelessAIType.ResetCycle, false, () =>
            {
                return AttackTimer >= EnterPhase2_BackgroundEnterTime + EnterPhase2_AttackCooldownTime + EnterPhase2_AttackTransitionDelay && KaleidoscopeInterpolant <= 0.23f;
            }, () => NPC.Opacity = 1f);
        }

        public void LoadStateTransitions_Phase3TransitionStart()
        {
            // Enter phase 3 if ready. Unlike phase 2, this happens immediately since the transition effect is intentionally sudden and jarring, like a "psychic damage" attack.
            ApplyToAllStatesExcept(previousState =>
            {
                StateMachine.RegisterTransition(previousState, NamelessAIType.EnterPhase3, false, () =>
                {
                    return LifeRatio <= Phase3LifeRatio && CurrentPhase == 1;
                }, () =>
                {
                    SoundEngine.StopTrackedSounds();
                    ClearAllProjectiles();
                    CurrentPhase = 2;
                    NPC.netUpdate = true;
                });
            }, NamelessAIType.DeathAnimation, NamelessAIType.DeathAnimation_GFB);
        }

        public void LoadStateTransitions_Phase3TransitionEnd()
        {
            // Load the transition from EnterPhase3 to the regular cycle.
            StateMachine.RegisterTransition(NamelessAIType.EnterPhase3, NamelessAIType.ResetCycle, false, () =>
            {
                return AttackTimer >= EnterPhase3_AttackTransitionDelay;
            }, () =>
            {
                // Play the glitch sound.
                SoundEngine.PlaySound(GlitchSound);

                // Teleport behind the target.
                ImmediateTeleportTo(Target.Center - Vector2.UnitX * TargetDirection * 400f, false);

                // Return sounds back to normal.
                SoundMufflingSystem.MuffleFactor = 1f;
                MusicVolumeManipulationSystem.MuffleFactor = 1f;

                // Reset the sword slash variables.
                // The reason this is necessary is because sometimes the sword can be terminated suddenly by this attack, without getting a chance to reset
                // the counter. This can make it so that the first sword slash attack in the third phase can have incorrect data.
                SwordSlashDirection = 0;
                SwordSlashCounter = 0;

                // Disable the light effect.
                OriginalLightGlitchOverlaySystem.OverlayInterpolant = 0f;
                OriginalLightGlitchOverlaySystem.GlitchIntensity = 0f;
                OriginalLightGlitchOverlaySystem.EyeOverlayOpacity = 0f;
                OriginalLightGlitchOverlaySystem.WhiteOverlayInterpolant = 0f;

                // Create disorienting visual effects.
                TotalScreenOverlaySystem.OverlayInterpolant = 1f;
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 2.2f, 150);
                StartShakeAtPoint(NPC.Center, 15f);
                SoundEngine.PlaySound(EarRingingSound with { Volume = 0.12f });
            });
        }

        public void DoBehavior_EnterPhase2()
        {
            int backgroundEnterTime = EnterPhase2_BackgroundEnterTime;
            int cooldownTime = EnterPhase2_AttackCooldownTime;
            int sliceTelegraphTime = 41;
            int daggerShootCount = 14;
            int ripperDestructionAnimationTime = 80;

            ref float daggerShootTimer = ref NPC.ai[2];
            ref float daggerShootCounter = ref NPC.ai[3];

            if (!CommonCalamityVariables.RevengeanceModeActive)
                ripperDestructionAnimationTime = 1;

            int daggerShootRate = (int)(60f - daggerShootCounter * 4.6f);
            float daggerSpacing = Remap(daggerShootTimer, 0f, 7f, 216f, 141f);
            if (daggerShootRate < 42)
                daggerShootRate = 42;
            if (CommonCalamityVariables.DeathModeActive)
                daggerShootRate -= 4;

            // Undo relative darkness effects.
            if (RelativeDarkening > 0f)
            {
                RelativeDarkening = Clamp(RelativeDarkening - 0.075f, 0f, 1f);
                HeavenlyBackgroundIntensity = 1f - RelativeDarkening;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                sliceTelegraphTime = 25;
                daggerShootRate -= 10;
            }

            // Destroy the ripper UI.
            float ripperDestructionAnimationCompletion = InverseLerp(0f, ripperDestructionAnimationTime, AttackTimer - cooldownTime + ripperDestructionAnimationTime);
            if (!RipperUIDestructionSystem.IsUIDestroyed)
                RipperUIDestructionSystem.FistOpacity = InverseLerp(0.04f, 0.74f, ripperDestructionAnimationCompletion);
            else
                RipperUIDestructionSystem.FistOpacity = Clamp(RipperUIDestructionSystem.FistOpacity - 0.04f, 0f, 1f);

            // Censor items and obliterate rods of harmony in GFB.
            if (Main.zenithWorld)
            {
                HotbarUICensorSystem.CensorOpacity = InverseLerp(0.09f, 0.81f, ripperDestructionAnimationCompletion);

                if (AttackTimer == cooldownTime + 1f)
                {
                    // Check if the rod of harmony is is the target's inventory and they have no legitimate cheat permission slip. If it is, do a special rant.
                    if (NPC.HasPlayerTarget && RoHDestructionSystem.PerformRodOfHarmonyCheck(Main.player[NPC.target]))
                    {
                        TargetIsUsingRodOfHarmony = true;
                        NPC.netUpdate = true;
                    }
                }
            }

            if (!RipperUIDestructionSystem.IsUIDestroyed && ripperDestructionAnimationCompletion >= 1f)
            {
                RipperUIDestructionSystem.CreateBarDestructionEffects();
                RipperUIDestructionSystem.IsUIDestroyed = true;
                NamelessDeityKeyboardShader.BrightnessIntensity = 1f;
            }

            // Play mumble sounds.
            if (AttackTimer == 1f)
                PerformMumble();

            // Update wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Enter the background and dissapear.
            if (AttackTimer < backgroundEnterTime + cooldownTime)
            {
                ZPosition = MathF.Max(ZPosition, Remap(AttackTimer, 0f, backgroundEnterTime, 0f, 11f));
                NPC.Opacity = InverseLerp(backgroundEnterTime - 1f, backgroundEnterTime * 0.56f, AttackTimer);
                KaleidoscopeInterpolant = 1f - NPC.Opacity;
                NPC.dontTakeDamage = true;
            }
            else if (!TargetIsUsingRodOfHarmony)
                daggerShootTimer++;

            // Release daggers.
            if (daggerShootTimer >= daggerShootRate && daggerShootCounter < daggerShootCount)
            {
                SoundEngine.PlaySound(PortalCastSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int daggerIndex = 0;
                    Vector2 sliceDirection = Vector2.UnitY.RotatedBy(TwoPi * Main.rand.Next(6) / 6f);
                    Vector2 perpendicularDirection = sliceDirection.RotatedBy(PiOver2);
                    Vector2 daggerSpawnPosition = Target.Center - sliceDirection * 600f;
                    for (float d = 0f; d < 1300f; d += daggerSpacing * (d < 84f ? 0.18f : 1f))
                    {
                        float hueInterpolant = d / 1800f;
                        Vector2 daggerStartingVelocity = sliceDirection * 16f;
                        Vector2 left = daggerSpawnPosition - perpendicularDirection * d;
                        Vector2 right = daggerSpawnPosition + perpendicularDirection * d;

                        NewProjectileBetter(left, daggerStartingVelocity, ModContent.ProjectileType<LightDagger>(), DaggerDamage, 0f, -1, sliceTelegraphTime, hueInterpolant, daggerIndex);
                        NewProjectileBetter(right, daggerStartingVelocity, ModContent.ProjectileType<LightDagger>(), DaggerDamage, 0f, -1, sliceTelegraphTime, hueInterpolant, daggerIndex + 1f);
                        daggerIndex += 2;
                    }
                }

                daggerShootTimer = 0f;
                daggerShootCounter++;
                NPC.netUpdate = true;
            }

            // Keep the attack timer locked while the daggers are in the process of being fired.
            if (daggerShootCounter < daggerShootCount && AttackTimer >= backgroundEnterTime + cooldownTime && !TargetIsUsingRodOfHarmony)
                AttackTimer = backgroundEnterTime + cooldownTime;

            // Make the kaleidoscopic background fade out for the most part before the attack concludes.
            if (AttackTimer >= backgroundEnterTime + cooldownTime + EnterPhase2_AttackTransitionDelay)
                KaleidoscopeInterpolant = Clamp(KaleidoscopeInterpolant - 0.006f, 0.23f, 1f);

            // Update universal hands.
            DefaultUniversalHandMotion();

            // Calculate the background hover position.
            float hoverHorizontalWaveSine = Sin(TwoPi * AttackTimer / 106f);
            Vector2 hoverDestination = Target.Center + new Vector2(Target.Velocity.X * 3f, ZPosition * -27f - 180f);
            hoverDestination.X += hoverHorizontalWaveSine * ZPosition * 36f;

            // Stay above the target while in the background.
            NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.084f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, (hoverDestination - NPC.Center) * 0.07f, 0.095f);
        }

        public void DoBehavior_EnterPhase3()
        {
            int glitchDelay = 32;

            // Disable sounds and music temporarily.
            SoundMufflingSystem.MuffleFactor = 0.004f;
            MusicVolumeManipulationSystem.MuffleFactor = 0f;

            // Handle first-frame initializations.
            if (AttackTimer == 1f)
            {
                // Start the phase transition sound.
                SoundEngine.PlaySound(Phase3TransitionSound);

                // Disable the UI and inputs for the duration of the attack.
                InputAndUIBlockerSystem.Start(true, true, () => CurrentState == NamelessAIType.EnterPhase3);

                // Create a white overlay effect.
                OriginalLightGlitchOverlaySystem.WhiteOverlayInterpolant = 1f;
            }

            // Show the player the Original Light.
            if (AttackTimer <= 10f)
                OriginalLightGlitchOverlaySystem.OverlayInterpolant = 1f;

            // Create glitch effects.
            if (AttackTimer == glitchDelay)
            {
                OriginalLightGlitchOverlaySystem.GlitchIntensity = 1f;
                OriginalLightGlitchOverlaySystem.EyeOverlayOpacity = 1f;
                OriginalLightGlitchOverlaySystem.WhiteOverlayInterpolant = 1f;

                // Play the glitch sound.
                SoundEngine.PlaySound(GlitchSound);
            }
        }
    }
}
