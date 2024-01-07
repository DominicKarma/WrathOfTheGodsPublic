using System;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        public void SelectNextAttack()
        {
            AttackTimer = 0f;
            NPC.Opacity = 1f;
            TeleportVisualsInterpolant = 0f;
            ZPosition = 0f;
            BrainFogChargeCounter = 0;
            PortalChainDashCounter = 0;
            PhaseCycleIndex++;

            // Cycle through attacks based on phase.
            if (CurrentPhase == 2)
                CurrentAttack = Phase3AttackCycle[PhaseCycleIndex % Phase3AttackCycle.Length];
            else if (CurrentPhase == 1)
                CurrentAttack = Phase2AttackCycle[PhaseCycleIndex % Phase2AttackCycle.Length];
            else
                CurrentAttack = Phase1AttackCycle[PhaseCycleIndex % Phase1AttackCycle.Length];

            TargetClosest();

            NPC.netUpdate = true;
        }

        public void BasicFlyMovement(Vector2 hoverDestination)
        {
            if (Distance(hoverDestination.X, NPC.Center.X) >= 100f || Distance(hoverDestination.Y, NPC.Center.Y) >= 180f)
            {
                Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.075f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.11f);
            }
        }

        public void DefaultHandDrift(EntropicGodHand hand, Vector2 hoverDestination, float speedFactor = 1f)
        {
            float maxFlySpeed = NPC.velocity.Length() + 13f;
            Vector2 idealVelocity = (hoverDestination - hand.Center) * 0.2f;

            // Propel the hands away from the center of Noxus, to prevent them from being unseeable.
            if (CurrentAttack is not EntropicGodAttackType.MigraineAttack and not EntropicGodAttackType.RapidExplosiveTeleports and not EntropicGodAttackType.BrainFogAndThreeDimensionalCharges and not EntropicGodAttackType.Phase2Transition)
                idealVelocity += NPC.DirectionToSafe(hand.Center) * Remap(NPC.Distance(hand.Center) / NPC.scale, 200f, 50f, 0f, 27f);

            if (idealVelocity.Length() >= maxFlySpeed)
                idealVelocity = idealVelocity.SafeNormalize(Vector2.UnitY) * maxFlySpeed;
            if (hand.Velocity.Length() <= maxFlySpeed * 0.7f)
                hand.Velocity *= 1.056f;

            hand.Velocity = Vector2.Lerp(hand.Velocity, idealVelocity * speedFactor, 0.39f);
        }

        public Vector2 CalculateHandOffsetForClapAnticipation(float arcInterpolant, float maxArcAngle, bool left)
        {
            // This blends between a sharp x^3 polynomial and the more gradual smoothstep function, to take advantage of both the very sudden increases
            // with exponents greater than one and the calm, non-linear slowdown right before reaching one that smoothsteps are notorious for.
            // The connective interpolant of x^2 exists to give more weight to the sharp polynomial at first.
            float smoothenedInterpolant = Lerp(Pow(arcInterpolant, 3f), SmoothStep(0f, 1f, arcInterpolant), Pow(arcInterpolant, 2f));
            float arcAngle = maxArcAngle * smoothenedInterpolant;
            return NPC.Center + Vector2.UnitY.RotatedBy(arcAngle * left.ToDirectionInt()) * 250f + Vector2.UnitX * left.ToDirectionInt() * -4f;
        }

        public void PerformZPositionEffects()
        {
            // Give the illusion of being in 3D space by shrinking. This is also followed by darkening effects in the draw code, to make it look like he's fading into the dark clouds.
            // The DrawBehind section of code causes Noxus to layer being things like trees to better sell the illusion.
            NPC.scale = 1f / (ZPosition + 1f);
            if (Math.Abs(ZPosition) >= 2.03f)
            {
                NPC.dontTakeDamage = true;
                NPC.ShowNameOnHover = false;
            }

            if (ZPosition <= -0.96f)
                NPC.scale = 0f;

            // Resize the hitbox based on scale.
            int oldWidth = NPC.width;
            int idealWidth = (int)(NPC.scale * 122f);
            int idealHeight = (int)(NPC.scale * 290f);
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

        public void MakeHandsOpen()
        {
            for (int i = 0; i < Hands.Length; i++)
                Hands[i].ShouldOpen = true;
        }

        public void PreparePhaseTransitionsIfNecessary()
        {
            if (CurrentPhase == 0 && LifeRatio < Phase2LifeRatio)
            {
                SelectNextAttack();
                ClearAllProjectiles();
                CurrentAttack = EntropicGodAttackType.Phase2Transition;
                PhaseCycleIndex = -1;
                CurrentPhase++;
                NPC.netUpdate = true;
            }

            if (CurrentPhase == 1 && LifeRatio < Phase3LifeRatio)
            {
                SelectNextAttack();
                ClearAllProjectiles();
                CurrentAttack = EntropicGodAttackType.Phase3Transition;
                PhaseCycleIndex = -1;
                CurrentPhase++;
                NPC.netUpdate = true;
            }
        }

        public void UpdateHands()
        {
            foreach (EntropicGodHand hand in Hands)
            {
                hand.FrameTimer++;
                if (hand.FrameTimer % 5 == 4)
                    hand.Frame = Utils.Clamp(hand.Frame - hand.ShouldOpen.ToDirectionInt(), 0, 2);

                hand.Center += hand.Velocity;
                hand.Velocity *= 0.99f;
            }
        }

        public void TeleportTo(Vector2 teleportPosition)
        {
            NPC.Center = teleportPosition;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

            // Reorient hands to account for the sudden change in position.
            foreach (EntropicGodHand hand in Hands)
                hand.Center = NPC.Center + (hand.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 100f;

            // Reset the oldPos array, so that afterimages don't suddenly "jump" due to the positional change.
            for (int i = 0; i < NPC.oldPos.Length; i++)
                NPC.oldPos[i] = NPC.position;

            SoundEngine.PlaySound(TeleportOutSound, NPC.Center);

            // Create teleport particle effects.
            ExpandingGreyscaleCircleParticle circle = new(NPC.Center, Vector2.Zero, new(219, 194, 229), 10, 0.28f);
            VerticalLightStreakParticle bigLightStreak = new(NPC.Center, Vector2.Zero, new(228, 215, 239), 10, new(2.4f, 3f));
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
        }

        public void TeleportToWithDecal(Vector2 teleportPosition)
        {
            // Create the decal particle at the old position before teleporting.
            NoxusDecalParticle decal = new(NPC.Center, NPC.rotation, Color.Lerp(Color.Cyan, Color.HotPink, 0.7f), 27, NPC.scale);
            decal.Spawn();

            NPC.Center = teleportPosition;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

            // Reorient hands to account for the sudden change in position.
            foreach (EntropicGodHand hand in Hands)
                hand.Center = NPC.Center + (hand.Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 100f;

            // Reset the oldPos array, so that afterimages don't suddenly "jump" due to the positional change.
            for (int i = 0; i < NPC.oldPos.Length; i++)
                NPC.oldPos[i] = NPC.position;

            SoundEngine.PlaySound(TeleportOutSound, NPC.Center);

            ExpandingGreyscaleCircleParticle circle = new(NPC.Center, Vector2.Zero, new Color(219, 194, 229) * 0.5f, 10, 0.28f);
            for (int i = 0; i < 30; i++)
            {
                Vector2 smallLightStreakSpawnPosition = NPC.Center + Main.rand.NextVector2Square(-NPC.width, NPC.width) * new Vector2(0.4f, 0.2f);
                Vector2 smallLightStreakVelocity = Vector2.UnitY * Main.rand.NextFloat(-3f, 3f);
                VerticalLightStreakParticle smallLightStreak = new(smallLightStreakSpawnPosition, smallLightStreakVelocity, Color.White, 10, new(0.1f, 0.3f));
                smallLightStreak.Spawn();
            }

            circle.Spawn();
        }

        public static void ClearAllProjectiles() => IProjOwnedByBoss<EntropicGod>.KillAll();

        public static TwinkleParticle CreateTwinkle(Vector2 spawnPosition, Vector2 scaleFactor)
        {
            Color twinkleColor = Color.Lerp(Color.HotPink, Color.Cyan, Main.rand.NextFloat(0.36f, 0.64f));
            TwinkleParticle twinkle = new(spawnPosition, Vector2.Zero, twinkleColor, 30, 6, scaleFactor);
            twinkle.Spawn();

            SoundEngine.PlaySound(TwinkleSound);
            return twinkle;
        }
    }
}
