using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.FormPresets;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Music;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public LoopedSoundInstance ChantSound;

        public LoopedSoundInstance CosmicLaserSound;

        public static int SuperCosmicLaserbeam_TeleportDelay => 26;

        public static int SuperCosmicLaserbeam_AttackDelay => SuperCosmicLaserbeam_TeleportDelay + BookConstellation.ConvergeTimeConst + 150;

        public static int SuperCosmicLaserbeam_LaserShootTime => SuperCosmicBeam.DefaultLifetime;

        public static int SuperCosmicLaserbeam_BackgroundEffectFadeoutTime => 45;

        public static int SuperCosmicLaserbeam_AttackDuration => SuperCosmicLaserbeam_AttackDelay + SuperCosmicLaserbeam_LaserShootTime + SuperCosmicLaserbeam_BackgroundEffectFadeoutTime;

        public void LoadStateTransitions_SuperCosmicLaserbeam()
        {
            // Load the transition from SuperCosmicLaserbeam to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.SuperCosmicLaserbeam, null, false, () =>
            {
                // Go to the next attack state immediately if the laser is missing.
                if (AttackTimer >= SuperCosmicLaserbeam_AttackDelay && AttackTimer <= SuperCosmicLaserbeam_AttackDelay + SuperCosmicLaserbeam_LaserShootTime - 30f && !AnyProjectiles(ModContent.ProjectileType<SuperCosmicBeam>()))
                    return true;

                return AttackTimer >= SuperCosmicLaserbeam_AttackDuration;
            }, () =>
            {
                SoundMufflingSystem.MuffleFactor = 1f;
                MusicVolumeManipulationSystem.MuffleFactor = 1f;
            });
        }

        public void DoBehavior_SuperCosmicLaserbeam()
        {
            int attackDelay = SuperCosmicLaserbeam_AttackDelay;
            int laserShootTime = SuperCosmicLaserbeam_LaserShootTime;
            int realityTearReleaseRate = 75;
            float laserAngularVelocity = Utils.Remap(NPC.Distance(Target.Center), 1150f, 1775f, 0.0161f, 0.074f);

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                realityTearReleaseRate = 45;
                laserAngularVelocity *= 2.2f;
            }

            ref float laserDirection = ref NPC.ai[2];
            ref float windIntensity = ref NPC.ai[3];

            Vector2 laserStart = NPC.Center + laserDirection.ToRotationVector2() * 100f;

            // Flap wings.
            UpdateWings(AttackTimer / 50f % 1f);

            // Teleport near the target.
            TeleportVisualsInterpolant = 0f;
            if (AttackTimer == 1)
            {
                StartTeleportAnimation(() =>
                {
                    Vector2 teleportPosition = Target.Center + new Vector2(TargetDirection * -400f, -240f);
                    if (teleportPosition.Y < 800f)
                        teleportPosition.Y = 800f;
                    while (Collision.SolidCollision(teleportPosition, NPC.width, NPC.height + 250))
                        teleportPosition.Y -= 16f;

                    return teleportPosition;
                }, 13, 13);
            }

            // Cast the book after the teleport.
            if (Main.netMode != NetmodeID.MultiplayerClient && !AnyProjectiles(ModContent.ProjectileType<BookConstellation>()) && AttackTimer <= attackDelay)
                NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.UnitX, ModContent.ProjectileType<BookConstellation>(), 0, 0f);

            // Make the sky more pale.
            if (AttackTimer <= 75f)
                DifferentStarsInterpolant = InverseLerp(0f, 60f, AttackTimer);
            HeavenlyBackgroundIntensity = Lerp(1f, 0.5f, DifferentStarsInterpolant);

            // Periodically fire reality tears at the starting point of the laser.
            if (AttackTimer >= attackDelay && AttackTimer <= attackDelay + laserShootTime - 60f && AttackTimer % realityTearReleaseRate == 0f)
            {
                SoundEngine.PlaySound(SliceSound, EyePosition);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float sliceAngle = Pi * i / 3f + laserDirection + PiOver2;
                        Vector2 sliceDirection = sliceAngle.ToRotationVector2();
                        NewProjectileBetter(NPC.GetSource_FromAI(), laserStart - sliceDirection * 2000f, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), 0, 0f, -1, 30f, 4000f);
                    }

                    if (Target.Center.WithinRange(NPC.Center, 335f))
                    {
                        for (int i = 0; i < 3; i++)
                            NewProjectileBetter(NPC.GetSource_FromAI(), EyePosition, (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY) * 5.6f + Main.rand.NextVector2Circular(0.9f, 0.9f), ModContent.ProjectileType<Starburst>(), StarburstDamage, 0f);
                    }
                }
            }

            // Make the wind intensify as necessary.
            bool windExists = AttackTimer >= attackDelay && AttackTimer <= attackDelay + laserShootTime - 20f;
            windIntensity = Clamp(windIntensity + windExists.ToDirectionInt() * 0.075f, 0f, 1f);

            // Periodically create screen pulse effects.
            if (AttackTimer >= attackDelay && AttackTimer % 30f == 0f)
            {
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 0.2f, 15);
                RadialScreenShoveSystem.Start(Vector2.Lerp(laserStart, Target.Center, 0.9f), 20);
            }

            // Play mumble sounds.
            if (AttackTimer == attackDelay - 40f && !NamelessDeityFormPresetRegistry.UsingLynelPreset)
                PerformMumble();

            if (NamelessDeityFormPresetRegistry.UsingLynelPreset && AttackTimer == attackDelay - 105f)
                CosmicLaserSound = LoopedSoundManager.CreateNew(JermaImKillingYouSound, () => !NPC.active || CurrentState != NamelessAIType.SuperCosmicLaserbeam);

            // Create the super laser.
            if (AttackTimer == attackDelay)
            {
                // Start the cosmic laser sound.
                if (!NamelessDeityFormPresetRegistry.UsingLynelPreset)
                {
                    CosmicLaserSound?.Stop();
                    CosmicLaserSound = LoopedSoundManager.CreateNew(CosmicLaserStartSound, CosmicLaserLoopSound, () => !NPC.active || CurrentState != NamelessAIType.SuperCosmicLaserbeam);
                }

                // Make Nameless chant.
                ChantSound?.Stop();
                if (!NamelessDeityFormPresetRegistry.UsingLynelPreset)
                    ChantSound = LoopedSoundManager.CreateNew(ChantSoundLooped, () => !NPC.active || CurrentState != NamelessAIType.SuperCosmicLaserbeam);

                // Shake the screen.
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, NPC.SafeDirectionTo(Target.Center), 42f, 2.75f, 112));
                HighContrastScreenShakeShaderData.ContrastIntensity = 14.5f;

                ScreenEffectSystem.SetFlashEffect(NPC.Center, 2f, 60);
                RadialScreenShoveSystem.Start(NPC.Center, 54);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, laserDirection.ToRotationVector2(), ModContent.ProjectileType<SuperCosmicBeam>(), SuperLaserbeamDamage, 0f, -1, 0f, SuperCosmicBeam.DefaultLifetime);
                }
            }

            float muffleInterpolant = InverseLerpBump(attackDelay, attackDelay + 17f, attackDelay + laserShootTime - 40f, attackDelay + laserShootTime + 32f, AttackTimer);
            float attackCompletion = InverseLerp(0f, laserShootTime - 30f, AttackTimer - attackDelay);
            if (AttackTimer % 5f == 0f && (AttackTimer >= attackDelay || NamelessDeityFormPresetRegistry.UsingLynelPreset))
            {
                // For some reason the audio engine has a stroke near the end of the attack and makes this do a stutter-like glitch.
                // This isn't anything I explicitly programmed and is effectively unintended behavior, but it fits Nameless so I'll leave it alone for now.
                CosmicLaserSound?.Update(Main.LocalPlayer.Center, sound =>
                {
                    float fadeOut = InverseLerp(0.98f, 0.93f, attackCompletion);
                    float ringingInterpolant = InverseLerpBump(0.98f, 0.93f, 0.4f, 0.12f, SoundMufflingSystem.EarRingingIntensity);
                    sound.Sound.Volume = Main.soundVolume * (Lerp(0.05f, 1.5f, muffleInterpolant) * Lerp(1f, 0.04f, ringingInterpolant) + InverseLerp(0.15f, 0.05f, attackCompletion) * 0.4f);
                    sound.Sound.Pitch = NamelessDeityFormPresetRegistry.UsingLynelPreset ? 0f : Lerp(0.01f, 0.6f, Pow(attackCompletion, 1.36f));
                });
                ChantSound?.Update(Main.LocalPlayer.Center, sound =>
                {
                    float fadeOut = InverseLerp(0.98f, 0.9f, attackCompletion);
                    sound.Sound.Volume = Clamp(Main.soundVolume * fadeOut * ChantSoundLooped.Volume, 0f, 1f);
                });
            }

            // Update the laser sound.
            if (AttackTimer >= attackDelay)
            {
                // Make the color contrast dissipate after the initial explosion.
                HighContrastScreenShakeShaderData.ContrastIntensity = Clamp(HighContrastScreenShakeShaderData.ContrastIntensity - 0.24f, 0f, 20f);

                // Make all other sounds rapidly fade out.
                SoundMufflingSystem.MuffleFactor = Lerp(1f, 0.009f, muffleInterpolant);
                MusicVolumeManipulationSystem.MuffleFactor = 1f - muffleInterpolant;

                SoundMufflingSystem.EarRingingIntensity *= 0.995f;

                // Make all sounds cease.
                if (AttackTimer >= attackDelay + laserShootTime + 60f)
                {
                    CosmicLaserSound?.Stop();
                    ChantSound?.Stop();
                }
            }

            // Keep the keyboard shader brightness at its maximum.
            if (AttackTimer >= attackDelay && AttackTimer < attackDelay + laserShootTime)
                NamelessDeityKeyboardShader.BrightnessIntensity = 1f;

            // Very slowly fly towards the target.
            if (NPC.WithinRange(Target.Center, 40f))
                NPC.velocity *= 0.92f;
            else
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center) * 2f, 0.15f);

            // Zoom towards the as the attack ends.
            if (AttackTimer >= attackDelay + laserShootTime - 5f)
            {
                float slowdownRadius = Utils.Remap(AttackTimer - attackDelay - laserShootTime, 5f, 50f, 270f, 600f);
                NPC.SmoothFlyNearWithSlowdownRadius(Target.Center, 0.07f, 0.09f, slowdownRadius);
            }

            // Spin the laser towards the target. If the player runs away it locks onto them.
            float idealLaserDirection = NPC.AngleTo(Target.Center);
            laserDirection = laserDirection.AngleLerp(idealLaserDirection, laserAngularVelocity);
            float idealHandAimDirection = InverseLerpBump(attackDelay - 90f, attackDelay - 36f, SuperCosmicLaserbeam_AttackDuration - 90f, SuperCosmicLaserbeam_AttackDuration - 32f, AttackTimer);

            // Update universal hands.
            float verticalOffset = Sin(AttackTimer / 20f) * 90f;
            Vector2 leftHoverOffset = new Vector2(-1100f, verticalOffset + 160f) * TeleportVisualsAdjustedScale;
            Vector2 rightHoverOffset = new Vector2(1100f, verticalOffset + 160f) * TeleportVisualsAdjustedScale;
            leftHoverOffset = Vector2.Lerp(leftHoverOffset, new(Sin(AttackTimer / 6f) * 40f - 540f, verticalOffset - 440f), windIntensity);
            rightHoverOffset = Vector2.Lerp(rightHoverOffset, new(Sin(AttackTimer / 5f) * 40f + 540f, -verticalOffset - 440f), windIntensity);

            Hands[0].Center = NPC.Center + leftHoverOffset.RotatedBy(0f.AngleLerp(laserDirection + PiOver2, idealHandAimDirection));
            Hands[1].Center = NPC.Center + rightHoverOffset.RotatedBy(0f.AngleLerp(laserDirection + PiOver2, idealHandAimDirection));
            Hands[0].DirectionOverride = 1;
            Hands[1].DirectionOverride = -1;
            Hands[0].PositionalDirectionOverride = -1;
            Hands[1].PositionalDirectionOverride = 1;
            Hands[0].RotationOffset = -PiOver4;
            Hands[1].RotationOffset = PiOver4;

            // Make the stars return to normal shortly before transitioning to the next attack.
            if (AttackTimer >= attackDelay + laserShootTime)
                DifferentStarsInterpolant = Clamp(DifferentStarsInterpolant - 0.06f, 0f, 1f);
        }
    }
}
