using System;
using System.Linq;
using Luminance.Common.DataStructures;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public int MumbleTimer
        {
            get;
            set;
        }

        public static int TotalUniversalHands => 2;

        public static int DefaultTwinkleLifetime => 30;

        public void PerformZPositionEffects()
        {
            // Give the illusion of being in 3D space by shrinking. This is also followed by darkening effects in the draw code, to make it look like he's fading into the dark clouds.
            // The DrawBehind section of code causes Nameless to layer behind things like trees to better sell the illusion.
            NPC.scale = DefaultScaleFactor / (ZPosition + 1f);
            if (Math.Abs(ZPosition) >= 2.03f)
                NPC.ShowNameOnHover = false;

            if (ZPosition <= -0.96f)
                NPC.scale = 0f;

            // Resize the hitbox based on scale.
            int oldWidth = NPC.width;
            int idealWidth = (int)(NPC.scale * 310f);
            int idealHeight = (int)(NPC.scale * 400f);
            if (idealWidth != oldWidth)
            {
                NPC.position.X += NPC.width / 2;
                NPC.position.Y += NPC.height / 2;
                NPC.width = idealWidth;
                NPC.height = idealHeight;
                NPC.position.X -= NPC.width / 2;
                NPC.position.Y -= NPC.height / 2;
            }
        }

        public void PerformMumble()
        {
            if (MumbleTimer <= 0)
                MumbleTimer = 1;
        }

        public void UpdateIdleSound()
        {
            // Start the loop sound on the first frame.
            bool canPlaySound = NPC.active && CurrentState != NamelessAIType.Awaken && CurrentState != NamelessAIType.OpenScreenTear && CurrentState != NamelessAIType.DeathAnimation && CurrentState != NamelessAIType.DeathAnimation_GFB;
            if (canPlaySound && (!SoundEngine.TryGetActiveSound(IdleSoundSlot, out ActiveSound s) || !s.IsPlaying))
            {
                s?.Stop();
                IdleSoundSlot = SoundEngine.PlaySound(HummSound with { PlayOnlyIfFocused = true }, NPC.Center);
            }

            if (!SoundEngine.TryGetActiveSound(IdleSoundSlot, out ActiveSound idleSound))
                return;

            // Stop the sound if it can't be currently played.
            if (!canPlaySound)
            {
                idleSound.Volume *= 0.9f;
                if (idleSound.Volume <= 0.001f || !NPC.active)
                    idleSound.Stop();
                return;
            }

            // Make the idle sound's pitch go up the faster Nameless is moving.
            float movementSpeed = NPC.position.Distance(NPC.oldPosition);
            bool probablyTeleported = movementSpeed >= 160f;
            if (!probablyTeleported)
                idleSound.Sound.Pitch = Utils.Remap(movementSpeed, 5f, 35f, 0f, 0.025f);

            // Make the idle sound's volume depend on how opaque and close to the foreground Nameless is.
            float backgroundSoundFade = 1f / (ZPosition + 1f);
            if (ZPosition < 0f)
                backgroundSoundFade = 1f;

            idleSound.Volume = NPC.Opacity * backgroundSoundFade * SoundMufflingSystem.MuffleFactor * HummSound.Volume;
            idleSound.Position = NPC.Center;
        }

        public void ConjureHandsAtPosition(Vector2 position, Vector2 velocity, bool useRobe = true)
        {
            SoundEngine.PlaySound(SoundID.Item100 with { MaxInstances = 100, Volume = 0.4f }, position);

            Hands.Add(new(position, useRobe)
            {
                Velocity = velocity
            });
            NPC.netSpam = 0;
            NPC.netUpdate = true;

            // Create particles.
            for (int i = 0; i < 12; i++)
            {
                int gasLifetime = Main.rand.Next(20, 24);
                float scale = NPC.scale * 2.3f;
                Vector2 gasSpawnPosition = position + Main.rand.NextVector2Circular(75f, 75f) * NPC.scale;
                Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f + velocity;
                Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));

                HeavySmokeParticle gas = new(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                gas.Spawn();
            }
        }

        public void CreateHandVanishVisuals(NamelessDeityHand hand)
        {
            // Create particles.
            for (int i = 0; i < 10; i++)
            {
                int gasLifetime = Main.rand.Next(20, 24);
                float scale = 2.3f;
                Vector2 gasSpawnPosition = hand.Center + Main.rand.NextVector2Circular(75f, 75f) * NPC.scale;
                Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f;
                Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));

                HeavySmokeParticle gas = new(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                gas.Spawn();
            }
        }

        public void DestroyAllHands(bool includeUniversalHands = false)
        {
            for (int i = includeUniversalHands ? 0 : TotalUniversalHands; i < Hands.Count; i++)
                CreateHandVanishVisuals(Hands[i]);

            while (Hands.Count > (includeUniversalHands ? 0 : TotalUniversalHands))
                Hands.Remove(Hands.Last());

            NPC.netUpdate = true;
        }

        public void DefaultHandDrift(NamelessDeityHand hand, Vector2 hoverDestination, float speedFactor = 1f)
        {
            float maxFlySpeed = NPC.velocity.Length() + 33f;
            Vector2 idealVelocity = (hoverDestination - hand.Center) * 0.2f;

            if (idealVelocity.Length() >= maxFlySpeed)
                idealVelocity = idealVelocity.SafeNormalize(Vector2.UnitY) * maxFlySpeed;
            if (hand.Velocity.Length() <= maxFlySpeed * 0.7f)
                hand.Velocity *= 1.056f;

            hand.Velocity = Vector2.Lerp(hand.Velocity, idealVelocity * speedFactor, 0.27f);

            // If the speed factor is high enough just stick to the ideal position.
            if (speedFactor >= 20f)
            {
                hand.Velocity = Vector2.Zero;
                hand.Center = hoverDestination;
            }

            // Reset the direction override.
            hand.PositionalDirectionOverride = 0;
        }

        public void DefaultUniversalHandMotion(float hoverOffset = 950f)
        {
            if (Hands.Count(h => h.HasArms) < TotalUniversalHands)
            {
                Hands.Insert(0, new(NPC.Center, true));
                NPC.netUpdate = true;
            }
            if (Hands.Count < 2)
                return;

            float verticalOffset = Sin(TwoPi * FightLength / 120f) * 75f;
            DefaultHandDrift(Hands[0], NPC.Center + new Vector2(-hoverOffset, verticalOffset + 160f) * TeleportVisualsAdjustedScale, 300f);
            DefaultHandDrift(Hands[1], NPC.Center + new Vector2(hoverOffset, verticalOffset + 160f) * TeleportVisualsAdjustedScale, 300f);

            Hands[0].DirectionOverride = 0;
            Hands[1].DirectionOverride = 0;
            Hands[0].RotationOffset = 0f;
            Hands[1].RotationOffset = 0f;
            Hands[0].HasArms = true;
            Hands[1].HasArms = true;
        }

        public void UpdateWings(float animationCompletion)
        {
            // Make the wings flap comically fast in GFB.
            if (Main.zenithWorld)
                animationCompletion = animationCompletion * 3f % 1f;

            // Update wings.
            Wings.Update(WingsMotionState, animationCompletion);

            // Play wing flap sounds.
            bool invalidAttackState = CurrentState is NamelessAIType.DeathAnimation or NamelessAIType.DeathAnimation_GFB or NamelessAIType.CrushStarIntoQuasar;
            if (WingsMotionState == WingMotionState.Flap && Distance(animationCompletion, 0.5f) <= 0.03f && NPC.soundDelay <= 0 && !invalidAttackState && NPC.Opacity >= 0.2f)
            {
                float volume = NPC.Opacity * WingFlapSound.Volume;
                if (ZPosition >= 0.01f)
                    volume /= ZPosition * 3f + 1f;

                SoundEngine.PlaySound(WingFlapSound with { Volume = volume }, NPC.Center);
                NPC.soundDelay = 10;
            }
        }

        public void StartTeleportAnimation(Func<Vector2> teleportDestination, int teleportInTime, int teleportOutTime)
        {
            ShouldStartTeleportAnimation = true;
            TeleportDestination = teleportDestination;
            TeleportInTime = teleportInTime;
            TeleportOutTime = teleportOutTime;
            AttackTimer++;
        }

        public void ImmediateTeleportTo(Vector2 teleportPosition, bool playSound = true)
        {
            Vector2 originalCenter = NPC.Center;
            NPC.Center = teleportPosition;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

            // Reorient hands to account for the sudden change in position.
            foreach (NamelessDeityHand hand in Hands)
                hand.Center = NPC.Center + (hand.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 100f;

            // Reset the oldPos array, so that afterimages don't suddenly "jump" due to the positional change.
            for (int i = 0; i < NPC.oldPos.Length; i++)
                NPC.oldPos[i] = NPC.position;

            // Play a teleport sound if the teleport parameters permits.
            if (playSound)
                SoundEngine.PlaySound(TeleportOutSound, NPC.Center);

            // Create teleport particle effects.
            ExpandingGreyscaleCircleParticle circle = new(NPC.Center, Vector2.Zero, new(219, 194, 229), 19, 0.28f);
            VerticalLightStreakParticle bigLightStreak = new(NPC.Center, Vector2.Zero, new(228, 215, 239), 18, new(2.7f, 3.45f));
            MagicBurstParticle magicBurst = new(NPC.Center, Vector2.Zero, new(150, 109, 219), 12, 0.1f);
            for (int i = 0; i < 30; i++)
            {
                Vector2 smallLightStreakSpawnPosition = NPC.Center + Main.rand.NextVector2Square(-NPC.width, NPC.width) * new Vector2(0.4f, 0.2f);
                Vector2 smallLightStreakVelocity = Vector2.UnitY * Main.rand.NextFloat(-3f, 3f);
                VerticalLightStreakParticle smallLightStreak = new(smallLightStreakSpawnPosition, smallLightStreakVelocity, Color.White, 10, new(0.1f, 0.3f));
                smallLightStreak.Spawn();
            }

            circle.Spawn();
            bigLightStreak.Spawn();
            magicBurst.Spawn();

            if (Main.netMode != NetmodeID.Server)
            {
                // Bring the vines along with too.
                for (int i = 0; i < (LeftVine?.Rope?.Count ?? 0); i++)
                {
                    LeftVine.Rope[i].Velocity = Vector2.UnitY * 5f;
                    LeftVine.Rope[i].Position = NPC.Center + Vector2.UnitY * i;
                    LeftVine.Rope[i].OldPosition = LeftVine.Rope[i].Position;
                }
                for (int i = 0; i < (RightVine?.Rope?.Count ?? 0); i++)
                {
                    RightVine.Rope[i].Velocity = Vector2.UnitY * 5f;
                    RightVine.Rope[i].Position = NPC.Center + Vector2.UnitY * i;
                    RightVine.Rope[i].OldPosition = LeftVine.Rope[i].Position;
                }

                for (int i = 0; i < 25; i++)
                    HandleDanglingPlantRotation();
            }

            // Change form if doing so wouldn't be noticed.
            if (TeleportVisualsAdjustedScale.Length() <= 0.01f || !NPC.WithinRange(Target.Center, 1200f) || NPC.Opacity <= 0.1f)
                RerollAllSwappableTextures();
        }

        public void ClearAllProjectiles()
        {
            IProjOwnedByBoss<NamelessDeityBoss>.KillAll();
            DestroyAllHands();
        }

        public static TwinkleParticle CreateTwinkle(Vector2 spawnPosition, Vector2 scaleFactor, Color backglowBloomColor = default, TwinkleParticle.LockOnDetails lockOnDetails = default)
        {
            Color twinkleColor = Color.Lerp(Color.Goldenrod, Color.IndianRed, Main.rand.NextFloat(0.15f, 0.67f));
            TwinkleParticle twinkle = new(spawnPosition, Vector2.Zero, twinkleColor, DefaultTwinkleLifetime, 8, scaleFactor, backglowBloomColor, lockOnDetails);
            twinkle.Spawn();

            SoundEngine.PlaySound(TwinkleSound with { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest });
            return twinkle;
        }
    }
}
