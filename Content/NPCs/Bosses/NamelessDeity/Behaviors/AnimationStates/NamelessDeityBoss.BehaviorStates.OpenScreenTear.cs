using Microsoft.Xna.Framework;
using NoxusBoss.Core;
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
        public static int OpenScreenTear_SeamGripTime => 90;

        public static int OpenScreenTear_SeamRipOpenTime => 36;

        public static int OpenScreenTear_BackgroundAppearDelay => 120;

        public static int OpenScreenTear_BackgroundAppearTime => 30;

        public static int OpenScreenTear_NamelessDeityAppearDelay => 105;

        public static int OpenScreenTear_OverallDuration => OpenScreenTear_SeamGripTime + OpenScreenTear_SeamRipOpenTime + OpenScreenTear_BackgroundAppearDelay +
            OpenScreenTear_BackgroundAppearTime + OpenScreenTear_NamelessDeityAppearDelay;

        public void LoadStateTransitions_OpenScreenTear()
        {
            // Load the transition from OpenScreenTear to RoarAnimation.
            StateMachine.RegisterTransition(NamelessAIType.OpenScreenTear, NamelessAIType.RoarAnimation, false, () =>
            {
                return AttackTimer >= OpenScreenTear_OverallDuration;
            });
        }

        public void DoBehavior_OpenScreenTear()
        {
            int gripTime = OpenScreenTear_SeamGripTime;
            int ripOpenTime = OpenScreenTear_SeamRipOpenTime;
            int backgroundAppearDelay = OpenScreenTear_BackgroundAppearDelay;
            int backgroundAppearTime = OpenScreenTear_BackgroundAppearTime;
            int myselfAppearDelay = OpenScreenTear_NamelessDeityAppearDelay;

            // NO. You do NOT get adrenaline for sitting around and doing nothing.
            if (NPC.HasPlayerTarget)
                Main.player[NPC.target].ResetRippers();

            // Use a specific hand texture.
            if (HandTexture is not null)
                HandTexture.TextureVariant = 4;

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            // Disable music.
            Music = 0;

            // Keep the seam scale at its minimum at first.
            SeamScale = Pow(InverseLerp(0f, 20f, AttackTimer), 2f) * 2.3f;

            // Play a screen slice sound on the first frame.
            if (AttackTimer == 1f)
                SoundEngine.PlaySound(IntroScreenSliceSound);

            // Update the drone loop sound from the Awaken state. It'll naturally terminate once Nameless starts attacking.
            IntroDroneLoopSound?.Update(Main.LocalPlayer.Center);

            // Stay above the target.
            NPC.Center = Target.Center - Vector2.UnitY * 4000f;

            // Stay invisible.
            NPC.Opacity = 0f;

            // Create many hands that will tear apart the screen on the first few frames.
            if (AttackTimer <= 16f && AttackTimer % 2f == 0f)
            {
                int handIndex = AttackTimer / 2;
                float verticalOffset = handIndex * 40f + 250f;
                if (handIndex % 2 == 0)
                    verticalOffset *= -1f;

                Hands.Add(new(Target.Center - Vector2.UnitX.RotatedBy(-SeamAngle) * verticalOffset, false)
                {
                    Velocity = Main.rand.NextVector2CircularEdge(17f, 17f),
                    Opacity = 0f
                });
                return;
            }

            // Make chromatic aberration effects happen periodically.
            if (AttackTimer % 20f == 19f && HeavenlyBackgroundIntensity <= 0.1f)
            {
                float aberrationIntensity = Remap(AttackTimer, 0f, 120f, 0.4f, 1.6f);
                ScreenEffectSystem.SetChromaticAberrationEffect(Target.Center, aberrationIntensity, 10);
            }

            // Have the hands move above and below the player, on the seam.
            float handMoveInterpolant = Pow(InverseLerp(0f, gripTime, AttackTimer), 3.2f) * 0.5f;

            Vector2 verticalOffsetDirection = Vector2.UnitX.RotatedBy(-SeamAngle - 0.015f);
            for (int i = 0; i < Hands.Count; i++)
            {
                bool left = i % 2 == 0;
                Vector2 handDestination = Target.Center + verticalOffsetDirection * -left.ToDirectionInt() * (i * 75f + 150f);
                handDestination += verticalOffsetDirection.RotatedBy(PiOver2 * -left.ToDirectionInt()) * 40f;
                if (handDestination.Y <= Target.Center.Y)
                    handDestination.X -= 100f;

                handDestination.X -= left.ToDirectionInt() * 52f;

                Hands[i].ScaleFactor = 1.54f;
                Hands[i].Opacity = InverseLerp(0f, 12f, AttackTimer);
                Hands[i].Center = Vector2.Lerp(Hands[i].Center, handDestination, handMoveInterpolant) + Main.rand.NextVector2Circular(4f, 4f);
                if (Hands[i].Center.WithinRange(handDestination, 60f))
                    Hands[i].RotationOffset = Hands[i].RotationOffset.AngleLerp(PiOver2 * left.ToDirectionInt(), 0.3f);
                else
                    Hands[i].RotationOffset = (handDestination - Hands[i].Center).ToRotation();
            }

            // Rip open the seam.
            SeamScale += Pow(InverseLerp(0f, ripOpenTime, AttackTimer - gripTime - 30f), 1.5f) * 250f;
            if (SeamScale >= 2f && HeavenlyBackgroundIntensity <= 0.3f)
            {
                if (OverallShakeIntensity <= 11f)
                    StartShake(8f);
            }

            if (AttackTimer == gripTime + 30f)
                SoundEngine.PlaySound(ScreenTearSound);

            // Delete the hands once the seam is fully opened.
            if (AttackTimer == gripTime + ripOpenTime + 60f)
            {
                DestroyAllHands(true);
                NPC.netUpdate = true;
            }

            // Make the natural background appear.
            HeavenlyBackgroundIntensity = InverseLerp(0f, backgroundAppearTime, AttackTimer - gripTime - ripOpenTime - backgroundAppearDelay);

            if (AttackTimer >= gripTime + ripOpenTime + backgroundAppearDelay + backgroundAppearTime)
            {
                SkyEyeScale *= 0.7f;
                if (SkyEyeScale <= 0.15f)
                    SkyEyeScale = 0f;

                if (AttackTimer == OpenScreenTear_OverallDuration - 1)
                {
                    // Mark Nameless as having been met for next time, so that the player doesn't have to wait as long.
                    if (!WorldSaveSystem.HasMetNamelessDeity && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        WorldSaveSystem.HasMetNamelessDeity = true;
                        NetMessage.SendData(MessageID.WorldData);
                    }
                }
            }
        }
    }
}
