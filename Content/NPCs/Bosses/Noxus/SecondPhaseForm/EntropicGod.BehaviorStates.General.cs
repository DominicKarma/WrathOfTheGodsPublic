using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers.NoxusSky;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        public void DoBehavior_Phase2Transition()
        {
            int anticipationSoundTime = 289;
            int screamTime = 180;
            float handSpeedFactor = 1.3f;
            Vector2 headCenter = NPC.Center + HeadOffset;
            Vector2 headTangentDirection = (NPC.rotation + HeadRotation).ToRotationVector2();
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Disable damage. It is not relevant for this behavior.
            NPC.damage = 0;
            NPC.dontTakeDamage = true;

            // Teleport above the player on the first frame.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitY * 350f);
                SoundEngine.PlaySound(BrainRotSound);
            }

            // Have the head rotate to the side.
            HeadRotation = Pi / 13f;

            // Periodically create chromatic aberration effects in accordance with the heartbeat of the sound.
            if (AttackTimer < anticipationSoundTime && AttackTimer % 30f == 29f)
            {
                float attackCompletion = AttackTimer / anticipationSoundTime;
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, attackCompletion * 2f + 0.3f, 12);
                StartShakeAtPoint(NPC.Center, Lerp(3f, 9f, attackCompletion), TwoPi, null, 0.2f, 3200f, 2400f);
            }

            // Make the eye gleam before teleporting.
            EyeGleamInterpolant = InverseLerp(anticipationSoundTime - 90f, anticipationSoundTime - 5f, AttackTimer);
            if (AttackTimer >= anticipationSoundTime)
                EyeGleamInterpolant = 0f;

            // Make the wind slow down at first but get fast after the anticipation.
            float idealWindSpeed = AttackTimer >= anticipationSoundTime ? 2.5f : 0.3f;
            WindSpeedFactor = Lerp(WindSpeedFactor, idealWindSpeed, 0.35f);

            // Move up and down and jitter a bit.
            float jitterIntensity = InverseLerp(0f, anticipationSoundTime, AttackTimer);
            NPC.velocity = Vector2.UnitY * Sin(TwoPi * AttackTimer / 56f) * 2f;
            if (jitterIntensity < 1f)
                NPC.Center += Main.rand.NextVector2Circular(15f, 15f) * Pow(jitterIntensity, 1.56f);

            // Scream after the anticipation is over.
            if (AttackTimer >= anticipationSoundTime)
            {
                if (AttackTimer == anticipationSoundTime)
                {
                    TeleportTo(Target.Center - Vector2.UnitY * 350f);

                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + HeadOffset, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }

                HeadRotation = 0f;
                NPC.velocity = Vector2.Zero;

                if (AttackTimer % 11f == 1f && AttackTimer <= anticipationSoundTime + screamTime - 45f)
                {
                    SoundEngine.PlaySound(ScreamSound with { Volume = 1.4f });
                    Color burstColor = Main.rand.NextBool() ? Color.SlateBlue : Color.Lerp(Color.White, Color.MediumPurple, 0.7f);

                    // Create blur and burst particle effects.
                    ExpandingChromaticBurstParticle burst = new(NPC.Center + HeadOffset, Vector2.Zero, burstColor, 16, 0.1f);
                    burst.Spawn();
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);
                    if (OverallShakeIntensity <= 9f)
                        StartShakeAtPoint(NPC.Center, 4f);
                }

                NPC.Center += Main.rand.NextVector2Circular(12.5f, 12.5f);

                leftHandDestination.Y += TeleportVisualsAdjustedScale.Y * 75f;
                rightHandDestination.Y += TeleportVisualsAdjustedScale.Y * 75f;
            }
            else
            {
                leftHandDestination = headCenter + headTangentDirection * NPC.scale * -80f + Main.rand.NextVector2Circular(7f, 7f);
                rightHandDestination = headCenter + headTangentDirection * NPC.scale * 80f + Main.rand.NextVector2Circular(7f, 7f);
            }

            if (AttackTimer >= anticipationSoundTime + screamTime)
                SelectNextAttack();

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_Phase3Transition()
        {
            int stunTime = 240;
            int screamTime = 210;
            float handSpeedFactor = 2f;
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Disable damage. It is not relevant for this behavior.
            NPC.damage = 0;
            NPC.dontTakeDamage = true;

            // Make the wind slow down at first but get fast after the stun.
            float idealWindSpeed = AttackTimer >= stunTime ? 4f : 0.15f;
            WindSpeedFactor = Lerp(WindSpeedFactor, idealWindSpeed, 0.39f);

            // Do stunned effects and make the music go away.
            if (AttackTimer <= stunTime)
            {
                // Do a loud explosion effect on the first frame to mask the fact that the music is gone. Having it just abrupt go away without a seam would be weird.
                if (AttackTimer == 1f)
                {
                    TeleportTo(Target.Center - Vector2.UnitY * 205f);

                    SoundEngine.PlaySound(ExplosionTeleportSound with { Volume = 1.5f });
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }

                // Temporarily disable the music.
                Music = 0;

                // Fade away over time.
                NPC.Opacity = InverseLerp(stunTime - 6f, stunTime - 105f, AttackTimer);

                // Have the head rotate to the side.
                HeadRotation = Pi / 13f;

                // Make the camera zoom in on Noxus.
                float cameraPanInterpolant = InverseLerp(5f, 11f, AttackTimer);
                float cameraZoom = InverseLerp(11f, 60f, AttackTimer) * 0.2f;
                CameraPanSystem.PanTowards(NPC.Center, cameraPanInterpolant);
                CameraPanSystem.Zoom = cameraZoom;

                // Make the boss bar close.
                NPC.MakeCalamityBossBarClose();
                NPC.dontTakeDamage = true;
            }

            else
            {
                // Bring the music back.
                if (Music == 0)
                    Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/EntropicGod");

                // Teleport above the player again.
                if (AttackTimer == stunTime + 1f)
                {
                    NPC.Opacity = 1f;
                    TeleportTo(Target.Center - Vector2.UnitY * 350f);

                    SoundEngine.PlaySound(ExplosionTeleportSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + HeadOffset, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }

                // Reset the head being rotated.
                HeadRotation = 0f;
                NPC.velocity = Vector2.Zero;

                // Make the big eye appear.
                BigEyeOpacity = Clamp(BigEyeOpacity + 0.075f, 0f, 1f);

                // Create scream shockwaves.
                if (AttackTimer % 9f == 1f && AttackTimer <= stunTime + screamTime - 45f)
                {
                    SoundEngine.PlaySound(ScreamSound with { Volume = 1.3f });
                    Color burstColor = Main.rand.NextBool() ? Color.SlateBlue : Color.Lerp(Color.White, Color.MediumPurple, 0.7f);

                    // Create blur and burst particle effects.
                    ExpandingChromaticBurstParticle burst = new(NPC.Center + HeadOffset, Vector2.Zero, burstColor, 16, 0.1f);
                    burst.Spawn();
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 30);

                    if (OverallShakeIntensity <= 13f)
                        StartShakeAtPoint(NPC.Center, 4f);
                }

                // Jitter in place violently.
                NPC.Center += Main.rand.NextVector2Circular(12.5f, 12.5f);

                // Create powerful, lingering screen shake effects.
                if (OverallShakeIntensity <= 9f)
                    StartShakeAtPoint(NPC.Center, 4f);

                // Create explosions everywhere near the player.
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % 5f == 4f)
                {
                    Vector2 explosionSpawnPosition = Target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(250f, 720f);
                    NewProjectileBetter(NPC.GetSource_FromAI(), explosionSpawnPosition, Vector2.Zero, ModContent.ProjectileType<NoxusExplosion>(), 0, 0f);
                }

                leftHandDestination.Y += TeleportVisualsAdjustedScale.Y * 75f;
                rightHandDestination.Y += TeleportVisualsAdjustedScale.Y * 75f;
            }

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);

            if (AttackTimer >= stunTime + screamTime)
                SelectNextAttack();
        }

        public void DoBehavior_MigraineAttack()
        {
            // In sync with the sound.
            int attackTransitionDelay = 289;

            // Handle teleport visual effects.
            float fadeInInterpolant = InverseLerp(0f, DefaultTeleportDelay, AttackTimer);
            TeleportVisualsInterpolant = fadeInInterpolant * 0.5f + 0.5f;
            NPC.Opacity = Pow(fadeInInterpolant, 1.34f);

            // Teleport above the player at first.
            if (AttackTimer == 1f)
            {
                TeleportTo(Target.Center - Vector2.UnitY * 200f);
                NPC.velocity = NPC.SafeDirectionTo(Target.Center).RotatedByRandom(0.66f) * -8f;
                SoundEngine.PlaySound(BrainRotSound with { Volume = 0.55f });
            }

            // Disable contact damage and remove defensive things.
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.SetDR(0f);

            // Slow down.
            NPC.velocity *= 0.98f;

            // Make the hand whirl around.
            HeadRotation = Sin(TwoPi * AttackTimer / 31f) * 0.25f;
            HeadSquishiness = HeadRotation * 0.3f;

            // Have hands grip the edges of the head, as though Noxus is having a serious headache.
            Vector2 headCenter = NPC.Center + HeadOffset;
            Vector2 headTangentDirection = (NPC.rotation + HeadRotation).ToRotationVector2();
            Vector2 leftHandDestination = headCenter + headTangentDirection * NPC.scale * -80f;
            Vector2 rightHandDestination = headCenter + headTangentDirection * NPC.scale * 80f;

            // Periodically create chromatic aberration effects in accordance with the heartbeat of the sound.
            if (AttackTimer % 30f == 29f)
            {
                float attackCompletion = AttackTimer / attackTransitionDelay;
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, attackCompletion * 2f + 0.3f, 12);
                StartShakeAtPoint(NPC.Center, Lerp(3f, 9f, attackCompletion));
            }

            // Jitter in place.
            NPC.Center += Main.rand.NextVector2CircularEdge(1f, 1f) * Lerp(1.5f, 8f, Pow(AttackTimer / attackTransitionDelay, 1.9f));

            // Fade away right before the attack ends. Also have the hands move away from the head.
            float attackEndInterpolant = InverseLerp(attackTransitionDelay - 96f, attackTransitionDelay - 30f, AttackTimer);
            if (attackEndInterpolant > 0f)
            {
                NPC.Opacity *= InverseLerp(attackTransitionDelay, attackTransitionDelay - 11f, AttackTimer);
                leftHandDestination -= (headCenter - leftHandDestination).SafeNormalize(Vector2.UnitY) * new Vector2(90f, -135f) * attackEndInterpolant;
                rightHandDestination -= (headCenter - rightHandDestination).SafeNormalize(Vector2.UnitY) * new Vector2(90f, -135f) * attackEndInterpolant;

                // Make the head stop spinning.
                HeadRotation *= 1f - attackEndInterpolant;
            }

            if (AttackTimer >= attackTransitionDelay)
            {
                SoundEngine.PlaySound(ExplosionTeleportSound, Target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.GetSource_FromAI(), (Hands[0].Center + Hands[1].Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 2.8f, 60);
                SelectNextAttack();
            }

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, 1.1f);
            DefaultHandDrift(Hands[1], rightHandDestination, 1.1f);
        }
    }
}
