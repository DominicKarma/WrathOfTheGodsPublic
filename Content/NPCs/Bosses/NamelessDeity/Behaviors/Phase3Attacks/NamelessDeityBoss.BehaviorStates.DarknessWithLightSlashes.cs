using System;
using Luminance.Common.Easings;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
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
        public LoopedSoundInstance HeartbeatLoopSound;

        public Vector2 LightSlashPosition
        {
            get;
            set;
        }

        public static int DarknessWithLightSlashes_SlashCount
        {
            get
            {
                if (Main.zenithWorld)
                    return 3;

                return 4;
            }
        }

        public ref float DarknessWithLightSlashes_SlashCounter => ref NPC.ai[3];

        public void LoadStateTransitions_DarknessWithLightSlashes()
        {
            // Load the transition from DarknessWithLightSlashes to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.DarknessWithLightSlashes, null, false, () =>
            {
                return DarknessWithLightSlashes_SlashCounter >= DarknessWithLightSlashes_SlashCount;
            }, () =>
            {
                ZPosition = 0f;
                NPC.Center = Target.Center - Vector2.UnitY * 700f;
                NPC.velocity = Vector2.UnitY * 9f;
                NPC.netUpdate = true;
            });
        }

        public void DoBehavior_DarknessWithLightSlashes()
        {
            int backgroundEnterTime = 45;
            int flashFadeInTime = 30;
            int flashLingerTime = 30;
            int flashFadeOutTime = 45;
            int slashTelegraphTime = 36;
            int slashTime = 87;
            int slashCount = DarknessWithLightSlashes_SlashCount;
            float slashStartingOffsetVariance = 85f;
            float maxGeneralDarkness = 0.089f;
            float slashSpeed = 15.5f;
            float slashInterpolationSpeed = 0.0151f;
            Vector2 slashStartingOffset = new(470f, 200f);
            ref float vignetteInterpolant = ref NPC.ai[2];
            ref float slashCounter = ref DarknessWithLightSlashes_SlashCounter;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                slashTime = 61;
                slashSpeed = 39f;
            }

            // Flap wings.
            UpdateWings(AttackTimer / 50f % 1f);

            // Enter the background at first.
            if (AttackTimer <= backgroundEnterTime)
            {
                // Move into the background.
                float fadeIntoBackgroundInterpolant = AttackTimer / backgroundEnterTime;
                ZPosition = EasingCurves.Quadratic.Evaluate(EasingType.In, 0f, 7f, fadeIntoBackgroundInterpolant);

                // Move up higher and higher above the target based on how far Nameless is in the background.
                float verticalOffset = ZPosition * 25f + 300f;
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * verticalOffset;
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.03f);
                NPC.SmoothFlyNearWithSlowdownRadius(hoverDestination, 0.09f, 0.92f, 60f);
            }
            else
            {
                NPC.SmoothFlyNearWithSlowdownRadius(Target.Center - Vector2.UnitY * 550f, 0.15f, 0.85f, 80f);
                NPC.Opacity = 0f;
            }

            // Create a black flash over the screen.
            float flashInterpolant = InverseLerpBump(0f, flashFadeInTime, flashFadeInTime + flashLingerTime, flashFadeInTime + flashLingerTime + flashFadeOutTime, AttackTimer - backgroundEnterTime);
            if (flashInterpolant > 0f)
            {
                TotalScreenOverlaySystem.OverlayColor = Color.Black;
                TotalScreenOverlaySystem.OverlayInterpolant = flashInterpolant;
                if (AttackTimer >= backgroundEnterTime + flashFadeInTime + flashLingerTime && TotalScreenOverlaySystem.OverlayInterpolant < maxGeneralDarkness)
                    TotalScreenOverlaySystem.OverlayInterpolant = maxGeneralDarkness;
            }

            // Make the background go dark during the flash.
            // This effect will naturally dissipate when Nameless does another attack.
            BlackOverlayInterpolant = MathF.Max(BlackOverlayInterpolant, flashInterpolant * 0.99f);

            // Play a glitch sound as the flash starts.
            if (AttackTimer == backgroundEnterTime + 1f)
                SoundEngine.PlaySound(GlitchSound);

            // Make the heartbeat sound go on when slashes are not present.
            if (AttackTimer == 1f || (HeartbeatLoopSound.HasBeenStopped && !AnyProjectiles(ModContent.ProjectileType<LightSlash>())))
            {
                HeartbeatLoopSound?.Stop();
                HeartbeatLoopSound = LoopedSoundManager.CreateNew(HeartbeatSound, () =>
                {
                    return !NPC.active || CurrentState != NamelessAIType.DarknessWithLightSlashes || AnyProjectiles(ModContent.ProjectileType<LightSlash>());
                });
            }

            // Update the heart beat loop sound.
            HeartbeatLoopSound?.Update(Main.LocalPlayer.Center);

            // Update universal hands.
            DefaultUniversalHandMotion();

            // Calculate the attack timer without the cinematics prior to the battle.
            // If it is not ready yet, wait until it is before proceeding into the rest of this method.
            int adjustedAttackTimer = (int)AttackTimer - backgroundEnterTime - flashFadeInTime - flashLingerTime - flashFadeOutTime;
            if (adjustedAttackTimer < 0)
                return;

            // Handle visuals during the jumpscare background.
            if (NamelessDeityScarySkyManager.IsActive && NamelessDeityScarySkyManager.Variant == NamelessDeityScarySkyManager.SkyVariant.NamelessDeityJumpscare)
            {
                ZPosition = -0.68f;
                NPC.Opacity = InverseLerp(0.04f, 0.2f, NamelessDeityScarySkyManager.FlashBrightness);
                NPC.Center = Target.Center + Vector2.UnitY * 170f;
                NPC.dontTakeDamage = true;
            }
            else
            {
                NPC.Opacity = 0f;
                ZPosition = 7f;
            }

            // Keep a certain amount of darkness in place.
            bool attackIsAboutToEnd = slashCounter >= slashCount - 1f && adjustedAttackTimer >= slashTelegraphTime + slashTime;
            TotalScreenOverlaySystem.OverlayColor = Color.Black;
            TotalScreenOverlaySystem.OverlayInterpolant = attackIsAboutToEnd ? 0f : maxGeneralDarkness;

            if (AnyProjectiles(ModContent.ProjectileType<LightSlash>()))
            {
                float minInterpolant = attackIsAboutToEnd ? 0f : 0.6f;
                vignetteInterpolant = Clamp(vignetteInterpolant - 0.07f, minInterpolant, 1f);
            }
            else
                vignetteInterpolant = Clamp(vignetteInterpolant + 0.134f, 0f, 1f);

            // Determine the light slash position and create a telegraph at its position.
            if (Main.netMode != NetmodeID.MultiplayerClient && adjustedAttackTimer == 1)
            {
                Vector2 slashSpawnOffset = Target.Velocity.SafeNormalize(Main.rand.NextVector2Unit()).RotatedByRandom(0.6f);
                LightSlashPosition = Target.Center + slashSpawnOffset * slashStartingOffset + Main.rand.NextVector2Circular(slashStartingOffsetVariance, slashStartingOffsetVariance) + Target.Velocity * 22f;
                NPC.netUpdate = true;
                NewProjectileBetter(NPC.GetSource_FromAI(), LightSlashPosition, Vector2.Zero, ModContent.ProjectileType<LightSlashTelegraph>(), 0, 0f, -1, slashTelegraphTime);
            }

            // Prepare the scream sound and create sudden visuals at the slash telegraph.
            if (adjustedAttackTimer == slashTelegraphTime)
            {
                // Play the scream sound.
                SoundEngine.PlaySound(ScreamSoundDistorted with { MaxInstances = 10 });

                // Create screen effects.
                ScreenEffectSystem.SetBlurEffect(LightSlashPosition, 1f, 16);
                ScreenEffectSystem.SetChromaticAberrationEffect(LightSlashPosition, 1f, 45);

                // Make the background spooky.
                NamelessDeityScarySkyManager.Start();

                // Create a light wave.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.GetSource_FromAI(), LightSlashPosition, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
            }

            // Create slashes.
            if (adjustedAttackTimer >= slashTelegraphTime && adjustedAttackTimer < slashTelegraphTime + slashTime && adjustedAttackTimer % 6f == 1f)
            {
                // Keep the keyboard shader bright.
                NamelessDeityKeyboardShader.BrightnessIntensity += 0.3f;

                // Play a slash sound.
                SoundEngine.PlaySound(SliceSound with { Volume = 0.4f, MaxInstances = 400 });

                // Shake the screen.
                StartShakeAtPoint(LightSlashPosition, 5f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float offset = 0f; offset < 42f; offset += Main.rand.NextFloat(28f, 40f))
                    {
                        // Calculate the slash's direction. This is randomized a bit but generally favors appearing to be facing towards the player, as through Nameless is
                        // slicing his hands back and forth while flying towards the player.
                        // If close to the player, however, this is not necessary and the direction is completely random.
                        float slashDirection = LightSlashPosition.AngleTo(Target.Center) + Main.rand.NextFloatDirection() * 1.05f - PiOver2;
                        if (LightSlashPosition.WithinRange(Target.Center, 160f))
                            slashDirection = Main.rand.NextFloat(TwoPi);

                        // Calculate the slash draw position. This is randomized a bit to create variety.
                        float randomOffset = Utils.Remap(LightSlashPosition.Distance(Target.Center), 100f, 250f, 10f, 90f);
                        Vector2 slashSpawnPosition = LightSlashPosition + (Target.Center - LightSlashPosition).SafeNormalize(Main.rand.NextVector2Unit()) * offset;
                        if (LightSlashPosition.WithinRange(Target.Center, 80f))
                        {
                            slashSpawnPosition = Target.Center;
                            randomOffset = 100f;
                        }

                        // Create the slash.
                        NewProjectileBetter(NPC.GetSource_FromAI(), slashSpawnPosition + Main.rand.NextVector2Unit() * randomOffset, Vector2.Zero, ModContent.ProjectileType<LightSlash>(), LightSlashDamage, 0f, -1, slashDirection);
                    }
                }
            }

            // Make the slashes move towards the target.
            if (adjustedAttackTimer >= slashTelegraphTime)
                LightSlashPosition = Vector2.Lerp(LightSlashPosition, Target.Center, slashInterpolationSpeed).MoveTowards(Target.Center, slashSpeed);

            // Handle transitions.
            if (adjustedAttackTimer >= slashTelegraphTime + slashTime + 25)
            {
                AttackTimer -= slashTelegraphTime + slashTime + 28;
                slashCounter++;
                NPC.netUpdate = true;
            }
        }
    }
}
