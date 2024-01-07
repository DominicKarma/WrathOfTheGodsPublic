using Microsoft.Xna.Framework;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public LoopedSoundInstance IntroDroneLoopSound;

        public static int Awaken_StarRecedeDelay => 30;

        public static int Awaken_StarRecedeTime => 45;

        public static int Awaken_EyeAppearTime => 30;

        public static int Awaken_EyeObserveTime => 96;

        public static int Awaken_PupilContractDelay => 12;

        public static int Awaken_SeamAppearTime => 5;

        public static int Awaken_SeamGrowTime => 30;

        public static int Awaken_OverallDuration
        {
            get
            {
                return Awaken_StarRecedeDelay + Awaken_StarRecedeTime + Awaken_EyeAppearTime + Awaken_EyeObserveTime;
            }
        }

        public void LoadStateTransitions_Awaken()
        {
            // Load the transition from Awaken to OpenScreenTear (or RoarAnimation, if Nameless has already been defeated).
            StateMachine.RegisterTransition(NamelessAIType.Awaken, NamelessAIType.OpenScreenTear, false, () =>
            {
                return AttackTimer >= Awaken_OverallDuration;
            }, () =>
            {
                SkyEyeOpacity = 0f;
            });
        }

        public void DoBehavior_Awaken()
        {
            int starRecedeDelay = Awaken_StarRecedeDelay;
            int starRecedeTime = Awaken_StarRecedeTime;
            int eyeAppearTime = Awaken_EyeAppearTime;
            int eyeObserveTime = Awaken_EyeObserveTime;
            int pupilContractDelay = Awaken_PupilContractDelay;
            int seamAppearTime = Awaken_SeamAppearTime;
            int seamGrowTime = Awaken_SeamGrowTime;

            // NO. You do NOT get adrenaline for sitting around and doing nothing.
            if (NPC.HasPlayerTarget)
                Main.player[NPC.target].ResetRippers();

            // Create suspense shortly before the animation concludes.
            if (AttackTimer == Awaken_OverallDuration - 150f)
                SoundEngine.PlaySound(IntroSuspenseSound);

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            // Disable music.
            Music = 0;

            // Make some screen shake effects happen.
            if (AttackTimer < starRecedeDelay)
            {
                float screenShakeIntensityInterpolant = Pow(AttackTimer / starRecedeDelay, 1.84f);
                SetUniversalRumble(Lerp(2f, 11.5f, screenShakeIntensityInterpolant), TwoPi / 9f, Vector2.UnitY);
                return;
            }

            // Make the stars recede away in fear.
            StarRecedeInterpolant = InverseLerp(starRecedeDelay, starRecedeDelay + starRecedeTime, AttackTimer);
            if (AttackTimer == starRecedeDelay + 10f)
            {
                // Play the star recede sound.
                SoundEngine.PlaySound(StarRecedeSound);

                // Start the intro drone sound.
                IntroDroneLoopSound?.Stop();
                IntroDroneLoopSound = LoopedSoundManager.CreateNew(IntroDroneSound, () =>
                {
                    return !NPC.active || IsAttackState(CurrentState) || HeavenlyBackgroundIntensity >= 0.15f;
                });
            }

            // Update the drone loop sound.
            IntroDroneLoopSound?.Update(Main.LocalPlayer.Center);

            // Perform some camera effects.
            float zoomOutInterpolant = InverseLerp(starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 17f, starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 4f, AttackTimer);
            CameraPanSystem.CameraFocusPoint = new Vector2(Main.LocalPlayer.Center.X, 3000f);
            CameraPanSystem.CameraPanInterpolant = Pow(StarRecedeInterpolant * (1f - zoomOutInterpolant), 0.17f);
            CameraPanSystem.Zoom = Pow(StarRecedeInterpolant * (1f - zoomOutInterpolant), 0.4f) * 0.6f;

            // Inputs are disabled and UIs are hidden during the camera effects. This is safely undone once Nameless begins screaming, or if he goes away for some reason.
            if (AttackTimer == starRecedeDelay + 1f)
            {
                InputAndUIBlockerSystem.Start(true, true, () =>
                {
                    // The block should immediately terminate if Nameless leaves for some reason.
                    if (!NPC.active)
                        return false;

                    // The block naturally terminates once Nameless isn't doing any introductory attack scenes.
                    if (CurrentState is not NamelessAIType.Awaken and not NamelessAIType.OpenScreenTear)
                        return false;

                    return true;
                });
            }

            // All code beyond this point only executes once all the stars have left.
            if (StarRecedeInterpolant < 1f)
                return;

            // Make the eye appear.
            SkyEyeOpacity = InverseLerp(starRecedeDelay + starRecedeTime, starRecedeDelay + starRecedeTime + eyeAppearTime, AttackTimer);
            if (AttackTimer == starRecedeDelay + starRecedeTime + 1f)
                SoundEngine.PlaySound(AppearSound);

            float pupilRollInterpolant = Pow(InverseLerp(1f, eyeAppearTime + 8f, AttackTimer - starRecedeDelay - starRecedeTime - 15f), 0.34f);
            float pupilScaleInterpolant = InverseLerp(0f, 10f, AttackTimer - starRecedeDelay - starRecedeTime - eyeAppearTime + 20f);
            float pupilContractInterpolant = Pow(InverseLerp(18f, pupilContractDelay, AttackTimer - starRecedeDelay - starRecedeTime - eyeAppearTime - eyeObserveTime), 0.25f);

            // Play a disgusting eye roll sound shortly after appearing.
            if (AttackTimer == starRecedeDelay + starRecedeTime + 20f)
                SoundEngine.PlaySound(EyeRollSound);

            // Make the eye look at the player after rolling eyes.
            Vector2 eyeRollDirection = Vector2.UnitY.RotatedBy(TwoPi * pupilRollInterpolant);
            SkyPupilOffset = Vector2.Lerp(SkyPupilOffset, Vector2.UnitY * (pupilContractInterpolant * 36f + 8f) + eyeRollDirection * 20f, 0.3f);
            SkyPupilScale = Pow(pupilScaleInterpolant, 1.7f) - pupilContractInterpolant * 0.5f;

            // Make the eye disappear before the seam appears.
            if (AttackTimer >= starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 48f)
            {
                if (AttackTimer == starRecedeDelay + starRecedeTime + eyeAppearTime + eyeObserveTime + pupilContractDelay - 35f)
                {
                    Color twinkleColor = Color.Lerp(Color.HotPink, Color.Cyan, Main.rand.NextFloat(0.36f, 0.64f));
                    TwinkleParticle twinkle = new(Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, 470f), Vector2.Zero, twinkleColor, 30, 6, Vector2.One * 2f);
                    twinkle.Spawn();
                }

                SkyEyeScale *= 0.7f;
                if (SkyEyeScale <= 0.15f)
                    SkyEyeScale = 0f;
            }
        }
    }
}
