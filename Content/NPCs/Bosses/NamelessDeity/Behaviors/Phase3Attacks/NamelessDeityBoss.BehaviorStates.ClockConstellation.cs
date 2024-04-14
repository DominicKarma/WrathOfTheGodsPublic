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
        public bool ClockConstellation_AttackHasConcluded
        {
            get => NPC.ai[2] == 1f;
            set => NPC.ai[2] = value.ToInt();
        }

        public void LoadStateTransitions_ClockConstellation()
        {
            // Load the transition from ClockConstellation to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.ClockConstellation, null, false, () =>
            {
                return HeavenlyBackgroundIntensity >= 0.9999f && ClockConstellation_AttackHasConcluded;
            });
        }

        public void DoBehavior_ClockConstellation()
        {
            int redirectTime = 35;
            int spinDuration = ClockConstellation.RegularSpinDuration + ClockConstellation.ReversedTimeSpinDuration;
            int waitDuration = (int)(1f / ClockConstellation.FadeOutIncrement) + ClockConstellation.TollWaitDuration * ClockConstellation.MaxTolls;
            int attackDuration = redirectTime + ClockConstellation.ConvergenceDuration + spinDuration + waitDuration + 10;
            var clocks = AllProjectilesByID(ModContent.ProjectileType<ClockConstellation>());
            bool clockExists = clocks.Any();

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Hover near the target at first.
            if (AttackTimer <= redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 400f, -250f);
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.29f);
                NPC.Opacity = 1f;

                // Make the background dark.
                ClockConstellation_AttackHasConcluded = false;
                HeavenlyBackgroundIntensity = Lerp(1f, 0.18f, AttackTimer / redirectTime);
                SeamScale = 0f;
            }

            // Teleport away after redirecting and create a clock constellation on top of the target.
            if (AttackTimer == redirectTime)
            {
                StartShake(12f);
                SoundEngine.PlaySound(SupernovaSound);
                SoundEngine.PlaySound(ScreamSoundShort);
                SoundEngine.PlaySound(GenericBurstSound);
                NamelessDeityKeyboardShader.BrightnessIntensity = 0.6f;

                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ClockConstellation>(), 0, 0f);
                }

                ImmediateTeleportTo(Target.Center + Vector2.UnitY * 2000f);

                // Play a sound to accompany the converging stars.
                SoundEngine.PlaySound(StarConvergenceSound);
            }

            // Stay at the top of the clock after redirecting.
            if (AttackTimer >= redirectTime && AttackTimer <= attackDuration - 90f && !ClockConstellation_AttackHasConcluded)
            {
                NPC.Opacity = 1f;

                if (clockExists)
                    NPC.Center = clocks.First().Center + new Vector2(-720f, Cos(AttackTimer / 34.5f) * 50f - 1020f);

                // Burn the target if they try to leave the clock.
                if (clockExists && !Target.Center.WithinRange(clocks.First().Center, 932f) && AttackTimer >= redirectTime + 60f)
                {
                    if (NPC.HasPlayerTarget)
                        Main.player[NPC.target].Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), Main.rand.Next(900, 950), 0);
                    else if (NPC.HasNPCTarget)
                        Main.npc[NPC.TranslatedTargetIndex].SimpleStrikeNPC(1500, 0);
                }
            }

            // Go to the next attack immediately if the clock is missing when it should be present.
            if (AttackTimer >= redirectTime + 1 && !clockExists)
                ClockConstellation_AttackHasConcluded = true;

            if (AttackTimer >= attackDuration && !ClockConstellation_AttackHasConcluded)
            {
                foreach (var clock in clocks)
                {
                    clock.Kill();
                    ImmediateTeleportTo(clock.Center);
                }

                SoundEngine.PlaySound(SupernovaSound);
                SoundEngine.PlaySound(GenericBurstSound);
                StartShake(12f);

                ScreenEffectSystem.SetFlashEffect(NPC.Center, 5f, 90);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ClockConstellation_AttackHasConcluded = true;
                    NPC.Opacity = 1f;
                    NPC.netUpdate = true;

                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }
            }

            // Update universal hands.
            float handHoverOffset = Sin(AttackTimer / 6f) * (1f - ClockConstellation.TimeIsStopped.ToInt()) * 120f + 850f;
            DefaultUniversalHandMotion(handHoverOffset);

            // Make the background return.
            if (ClockConstellation_AttackHasConcluded)
                HeavenlyBackgroundIntensity = Clamp(HeavenlyBackgroundIntensity + 0.05f, 0f, 1f);
        }
    }
}
