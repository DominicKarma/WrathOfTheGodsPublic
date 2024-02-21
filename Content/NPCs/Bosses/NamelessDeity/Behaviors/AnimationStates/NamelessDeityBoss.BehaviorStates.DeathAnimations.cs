using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NoxusBoss.Common.MainMenuThemes;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using NoxusBoss.Core.Music;
using SubworldLibrary;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers.NamelessDeitySky;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public int TimeSinceScreenSmash
        {
            get;
            set;
        }

        public bool WaitingForPhase2Transition
        {
            get;
            set;
        }

        public bool WaitingForDeathAnimation
        {
            get;
            set;
        }

        public static int DeathAnimationBlackDelay => 210;

        public void LoadStateTransitions_DeathAnimations()
        {
            // Ensure that phase transitions get priority if they're being waited on and the attack has concluded.
            // An exception to this is if Nameless is quickly killed in the final phase, and the player doesn't see the Moment of Creation attack.
            // If this happens, the Moment of Creation attack happens before Nameless does his defeat animation.
            StateMachine.AddTransitionStateHijack(originalState =>
            {
                if (HasExperiencedFinalAttack && WaitingForDeathAnimation)
                    return Main.zenithWorld ? NamelessAIType.DeathAnimation_GFB : NamelessAIType.DeathAnimation;
                if (!HasExperiencedFinalAttack && WaitingForDeathAnimation)
                    return NamelessAIType.MomentOfCreation;

                return originalState;
            }, finalState =>
            {
                // Disable the phase 2 transition wait if Nameless is dying.
                WaitingForPhase2Transition = false;
            });
        }

        public void DoBehavior_DeathAnimation()
        {
            int handCount = 222;
            int handReleaseRate = 1;
            int blackDelay = DeathAnimationBlackDelay;
            int riseDelay = 60;
            int riseTime = 210;
            int chargeLineUpTime = 45;
            int screenShatterDelay = 14;
            int crashDelay = 269;
            ref float screenShattered = ref NPC.ai[2];
            ref float congratulatoryTextDrawnBefore = ref NPC.ai[3];

            // Make the different stars interpolant dissipate, in case Nameless was defeated when it was changed.
            DifferentStarsInterpolant = Clamp(DifferentStarsInterpolant - 0.03f, 0f, 1f);

            // Stay at 1 HP.
            NPC.life = 1;
            NPC.dontTakeDamage = true;

            // Disable sound muffling.
            SoundMufflingSystem.MuffleFactor = 1f;
            MusicVolumeManipulationSystem.MuffleFactor = 1f;

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Get rid of Nameless' ingame name.
            NPC.GivenName = string.Empty;

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            Vector2 hoverDestinationForHand(int handIndex)
            {
                float goldenRatio = 1.618033f;
                Vector2 center = NPC.Center;
                return center + (TwoPi * goldenRatio * handIndex).ToRotationVector2() * (handIndex * NPC.scale * 12f + 40f) * Pow(ZPosition + 1f, 0.2f);
            }

            // Enter the background and hover behind the player.
            if (UniversalBlackOverlayInterpolant <= 0.9f)
            {
                ZPosition = Clamp(ZPosition + 0.1f, 0f, 5f);
                NPC.SmoothFlyNear(Target.Center - Vector2.UnitY * ZPosition * 50f, 0.25f, 0.7f);
            }
            else if (AttackTimer < blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime)
            {
                NPC.velocity *= 0.9f;

                if (AttackTimer > blackDelay + riseDelay + riseTime && Hands.Count >= handCount - 1)
                    ZPosition = Clamp(ZPosition + 0.3f, 0f, 12f);
                else
                    ZPosition = 0f;
            }

            // Update hands.
            if (AttackTimer <= blackDelay + 10f)
                DefaultUniversalHandMotion(1500f);

            if (AttackTimer <= blackDelay + riseDelay + 60f)
            {
                foreach (var hand in Hands)
                {
                    hand.Opacity = 0f;
                    hand.UsePalmForm = true;
                    if (AttackTimer <= blackDelay + 10f)
                    {
                        hand.Opacity = 1f;
                        hand.UsePalmForm = false;
                    }
                }
                NPC.netUpdate = true;
            }

            // Slow down and make the background go pitch black again while screaming at first.
            if (AttackTimer <= blackDelay)
            {
                // Teleport in front of the target on the first frame.
                if (AttackTimer == 1f)
                {
                    ImmediateTeleportTo(Target.Center + Vector2.UnitX * TargetDirection * 500f, false);

                    // Ensure that Nameless doesn't teleport into the ground.
                    while (Collision.SolidCollision(NPC.Center - new Vector2(350f, 300f), 700, 600) && NPC.position.Y >= 1200f)
                        NPC.position.Y -= 16f;
                }

                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity - 0.02f, 0f, 1f);
                SeamScale = 0f;
                NamelessDeityKeyboardShader.BrightnessIntensity = 1f;
            }
            else
            {
                UniversalBlackOverlayInterpolant = Clamp(UniversalBlackOverlayInterpolant + 0.06f, 0f, 1f);
                NamelessDeityKeyboardShader.DarknessIntensity = UniversalBlackOverlayInterpolant;
            }

            // Scream at first.
            if (AttackTimer == 1f)
                SoundEngine.PlaySound(ScreamSoundLong with { Pitch = -0.075f });
            if (AttackTimer % 8f == 0f && AttackTimer <= blackDelay + riseDelay - 75f)
            {
                Color burstColor = Main.rand.NextBool() ? Color.LightGoldenrodYellow : Color.Lerp(Color.White, Color.IndianRed, 0.7f);

                // Create blur and burst particle effects.
                ExpandingChromaticBurstParticle burst = new(EyePosition, Vector2.Zero, burstColor, 16, 0.1f);
                burst.Spawn();
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);

                if (OverallShakeIntensity <= 11f)
                    StartShakeAtPoint(NPC.Center, 5f);
            }

            // Block the UI and inputs until they return to the title screen.
            if (AttackTimer >= blackDelay + riseDelay - 75f)
                InputAndUIBlockerSystem.Start(true, true, () => NPC.active);

            // Display congratulatory text.
            if (AttackTimer == blackDelay + riseDelay + 60f)
            {
                DrawCongratulatoryText = true;
                congratulatoryTextDrawnBefore = 1f;
                SoundEngine.PlaySound(PulseSound);
            }
            if (AttackTimer == blackDelay + riseDelay + riseTime - 1f)
            {
                DrawCongratulatoryText = false;
                SoundEngine.PlaySound(SoundID.MenuClose);
            }

            // Prevent the player from being at the edge of the world and messing up the text drawing.
            if (congratulatoryTextDrawnBefore == 1f)
            {
                Main.LocalPlayer.position.X = Main.maxTilesX * 8f - 750f;
                NPC.Center = Main.LocalPlayer.Center;
            }

            // Let the player press escape to turn the effect off if they've already defeated Nameless.
            if (Main.netMode == NetmodeID.SinglePlayer && Main.keyState.IsKeyDown(Keys.Escape) && congratulatoryTextDrawnBefore == 1f && WorldSaveSystem.HasDefeatedNamelessDeity)
            {
                NPC.active = false;
                NPC.NPCLoot();
                NPC.Center = Target.Center;
                Main.BestiaryTracker.Kills.RegisterKill(NPC);
                SoundEngine.PlaySound(ScreamSoundDistant);

                TotalScreenOverlaySystem.OverlayInterpolant = 1f;
                TotalScreenOverlaySystem.OverlayColor = Color.Black;

                return;
            }

            // Calculate the background hover position.
            Vector2 hoverDestination = Target.Center + Vector2.UnitY * (ZPosition * -30f - 200f) + Vector2.UnitX * 125f;

            // Stay above the target while in the background.
            if (AttackTimer >= blackDelay + riseDelay)
            {
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.1f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, (hoverDestination - NPC.Center) * 0.07f, 0.06f);
            }

            // Cast a ridiculous quantity of arms outward.
            if (AttackTimer >= blackDelay + riseDelay + riseTime && AttackTimer % handReleaseRate == 0f && Hands.Count < handCount)
            {
                foreach (var hand in Hands)
                    hand.Opacity = 1f;
                ConjureHandsAtPosition(hoverDestinationForHand(Hands.Count), Vector2.Zero);
            }

            // Move further into the background.
            if (AttackTimer >= blackDelay + riseDelay + riseTime + handCount * handReleaseRate && AttackTimer < blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime)
            {
                float hoverSpeedInterpolant = Remap(ZPosition, 3f, 7f, 0.03f, 0.8f);
                NPC.velocity *= 0.8f;
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center - Vector2.UnitY * 100f, hoverSpeedInterpolant);
            }

            // Charge forward into the screen.
            if (AttackTimer >= blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime)
            {
                ZPosition = Clamp(ZPosition - 0.8f, -0.94f, 18f);
                NPC.Center = Vector2.Lerp(NPC.Center, Target.Center, 0.08f);
                if (screenShattered == 0f && ZPosition <= -0.94f)
                {
                    SoundEngine.PlaySound(SupernovaSound with { Volume = 3.3f });
                    SoundEngine.PlaySound(GenericBurstSound with { Volume = 4f });
                    StartShake(25f);
                    DestroyAllHands();
                    ScreenShatterSystem.CreateShatterEffect(NPC.Center - Main.screenPosition, true);
                    ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 3f, 90);

                    screenShattered = 1f;
                    NPC.netUpdate = true;
                }

                // Disable music and make the keyboard shader dance chaotically after the screen has been shattered.
                if (screenShattered == 1f)
                {
                    Music = 0;
                    NamelessDeityKeyboardShader.RandomColorsTimer = 5;
                    TimeSinceScreenSmash++;
                }

                if (AttackTimer >= blackDelay + riseDelay + riseTime + handCount * handReleaseRate + chargeLineUpTime + screenShatterDelay + crashDelay)
                {
                    // Completely disappear in multiplayer. This will happen server-side, while all clients involved in the fight are kicked out of the server.
                    NPC.active = false;

                    if (Main.netMode != NetmodeID.Server)
                    {
                        // Get out of the subworld.
                        typeof(SubworldSystem).GetField("current", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
                        typeof(SubworldSystem).GetField("cache", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

                        // Bring the player to the main menu, and enable the marker that ensures they will receive loot upon re-entry.
                        Main.menuMode = 0;
                        Main.gameMenu = true;
                        Main.LocalPlayer.GetValueRef<bool>(PlayerGiveLootFieldName).Value = true;

                        // Mark Nameless as defeated.
                        WorldSaveSystem.HasDefeatedNamelessDeity = true;

                        // Ensure that the player sees the "You have passed the test." dialog upon being sent to the main menu.
                        NamelessDeityTipsOverrideSystem.UseDeathAnimationText = true;

                        // Save the player's file data, to ensure that the loot re-entry is registered.
                        Player.SavePlayer(Main.ActivePlayerFileData);

                        // Kick clients out of the server.
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            Netplay.Disconnect = true;
                            Main.netMode = NetmodeID.SinglePlayer;
                        }

                        // Forcefully change the menu theme to the Nameless Deity one if he was defeated for the first time.
                        // This is done half as an indicator that the "I got kicked out of my world!" behavior isn't a bug and half as a reveal that the option is unlocked.
                        if (!WorldSaveSystem.HasDefeatedNamelessDeityInAnyWorld)
                        {
                            WorldSaveSystem.HasDefeatedNamelessDeityInAnyWorld = true;

                            do
                                typeof(MenuLoader).GetMethod("OffsetModMenu", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { Main.rand.Next(-2, 3) });
                            while (((ModMenu)typeof(MenuLoader).GetField("switchToMenu", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).FullName != NamelessDeityDimensionMainMenu.Instance.FullName);
                        }
                    }
                }
            }

            // Keep hands moving.
            for (int i = 0; i < Hands.Count; i++)
            {
                DefaultHandDrift(Hands[i], hoverDestinationForHand(i), 5.6f);
                if (AttackTimer > blackDelay + 10f)
                    Hands[i].UsePalmForm = true;
            }
        }

        public void DoBehavior_DeathAnimation_GFB()
        {
            // Stay at 1 HP.
            NPC.life = 1;
            NPC.dontTakeDamage = true;

            // Flap wings.
            UpdateWings(AttackTimer / 72f % 1f);

            // Get rid of Nameless' ingame name.
            NPC.GivenName = string.Empty;

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            // Make hands invisible.
            foreach (var hand in Hands)
                hand.Opacity = 0f;

            // Teleport in front of the target on the first frame and create a funny death sound.
            if (AttackTimer == 1f)
            {
                ImmediateTeleportTo(Target.Center + Vector2.UnitX * TargetDirection * 500f, false);
                SoundEngine.PlaySound(GFBDeathSound);
            }

            // Turn off the music.
            Music = 0;

            // Die and create a Deltarune explosion.
            if (AttackTimer >= 274f)
            {
                SoundEngine.PlaySound(ComicalExplosionDeathSound);
                DeltaruneExplosionParticle explosion = new(NPC.Center, Vector2.Zero, Color.White, 60, 1.9f);
                explosion.Spawn();

                Myself = null;
                NPC.life = 0;
                NPC.HitEffect();
                NPC.NPCLoot();
                Main.BestiaryTracker.Kills.RegisterKill(NPC);
                NPC.active = false;
            }
        }
    }
}
