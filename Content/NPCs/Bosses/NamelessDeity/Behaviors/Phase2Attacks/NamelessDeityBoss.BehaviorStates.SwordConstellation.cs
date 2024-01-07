using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Common.Easings;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Particles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public int SwordSlashCounter
        {
            get;
            set;
        }

        public int SwordSlashDirection
        {
            get;
            set;
        }

        public int SwordAnimationTimer
        {
            get;
            set;
        }

        // Used by the sword slash attack. Dictates how the sword's angle is manipulated to achieve the swing.
        public static readonly PiecewiseCurve SwordSlashAngularMotion = new PiecewiseCurve().
            Add(PolynomialEasing.Quadratic, EasingType.Out, 0f, 0.5f, Pi). // Slow start/anticipation.
            Add(PolynomialEasing.Quintic, EasingType.Out, Pi + 0.54f, 0.8f). // Fast swing.
            Add(PolynomialEasing.Cubic, EasingType.In, 0f, 1f); // End swing.

        public static int SwordConstellation_SlashCount
        {
            get
            {
                if (Main.zenithWorld)
                    return 5;

                return 4;
            }
        }

        public void LoadStateTransitions_SwordConstellation()
        {
            // Load the transition from SwordConstellation to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.SwordConstellation, null, false, () =>
            {
                return SwordSlashCounter >= SwordConstellation_SlashCount && SwordAnimationTimer >= 2;
            }, () => SwordSlashCounter = 0);
        }

        public void DoBehavior_SwordConstellation()
        {
            int constellationConvergeTime = SwordConstellation.ConvergeTimeConst;
            int slashAnimationTime = 104 - SwordSlashCounter * 8;
            int slashDelay = 10;
            int slashCount = SwordConstellation_SlashCount;
            int teleportVisualsTime = 14;
            var swords = AllProjectilesByID(ModContent.ProjectileType<SwordConstellation>());
            float maxHandOffset = 300f;
            float verticalAimOffset = 140f;
            ref float swordDoesDamage = ref NPC.ai[2];

            // Enforce a lower bound on the slash animation time, to prevent unfairness.
            if (slashAnimationTime < 70)
                slashAnimationTime = 70;

            // Make the attack faster in successive phases.
            if (CurrentPhase >= 2)
                slashAnimationTime -= 11;

            // Kill the player in GFB.
            if (Main.zenithWorld)
                slashAnimationTime = 33;

            // Use a small Z position.
            ZPosition = 0.12f;

            // Disallow arm variant 4 for this attack because the angles look weird.
            if ((HandTexture?.TextureVariant ?? 0) == 3)
                HandTexture?.Swap();

            // Flap wings.
            UpdateWings(AttackTimer / 35f % 1f);

            // Summon the sword constellation, along with a single hand to wield it on the first frame.
            // Also teleport above the target.
            if (AttackTimer == 1)
            {
                // Delete leftover projectiles on the first frame.
                ClearAllProjectiles();

                SwordAnimationTimer = 0;
                NamelessDeityKeyboardShader.BrightnessIntensity = 1f;
                StartTeleportAnimation(() =>
                {
                    Vector2 teleportOffset = Vector2.UnitX.RotatedByRandom(0.86f) * Main.rand.NextFromList(-1f, 1f) * 350f;
                    Vector2 teleportPosition = Target.Center + teleportOffset;
                    return teleportPosition;
                }, teleportVisualsTime, teleportVisualsTime + 4);

                // Apply visual and sound effects.
                StartShakeAtPoint(NPC.Center, 9.6f);
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 12);
                RadialScreenShoveSystem.Start(EyePosition, 45);

                // Create the sword.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Target.Center, Vector2.Zero, ModContent.ProjectileType<SwordConstellation>(), SwordConstellationDamage, 0f, -1, 1f);

                // Play a sound to accompany the converging stars.
                SoundEngine.PlaySound(StarConvergenceFastSound);
            }

            // Play mumble sounds.
            if (AttackTimer == constellationConvergeTime - 32f)
                PerformMumble();

            // Decide the slash direction.
            if (Distance(NPC.Center.X, Target.Center.X) >= 270f || SwordSlashDirection == 0f)
                SwordSlashDirection = NPC.velocity.X.NonZeroSign();

            if (SwordSlashCounter >= slashCount)
            {
                // Destroy the swords.
                foreach (Projectile sword in swords)
                {
                    for (int i = 0; i < 19; i++)
                    {
                        int gasLifetime = Main.rand.Next(20, 24);
                        float scale = 2.3f;
                        Vector2 gasSpawnPosition = sword.Center + Main.rand.NextVector2Circular(150f, 150f) * NPC.scale;
                        Vector2 gasVelocity = Main.rand.NextVector2Circular(9f, 9f) - Vector2.UnitY * 7.25f;
                        Color gasColor = Color.Lerp(Color.IndianRed, Color.Coral, Main.rand.NextFloat(0.6f));
                        Particle gas = new HeavySmokeParticle(gasSpawnPosition, gasVelocity, gasColor, gasLifetime, scale, 1f, 0f, true);
                        gas.Spawn();
                    }
                    sword.Kill();
                }

                SwordAnimationTimer++;
                DestroyAllHands();
                return;
            }

            // Increment the slash counter.
            if (AttackTimer >= constellationConvergeTime)
            {
                SwordAnimationTimer++;

                // Calculate the teleport visuals interpolant.
                TeleportVisualsInterpolant = InverseLerp(-15f, -1f, SwordAnimationTimer - slashDelay - slashAnimationTime) * 0.5f;
                if (SwordSlashCounter >= 1)
                    TeleportVisualsInterpolant += InverseLerp(11f, 0f, SwordAnimationTimer) * 0.5f;
                else
                    TeleportVisualsInterpolant = 1f;
            }

            if (SwordAnimationTimer >= slashAnimationTime)
            {
                SwordSlashCounter++;
                SwordAnimationTimer = 0;
                NPC.netUpdate = true;

                Vector2 teleportOffsetDirection = Target.Velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.93f);
                if (SwordSlashCounter % 3 == 0)
                    teleportOffsetDirection = teleportOffsetDirection.RotatedBy(PiOver2);

                // Vertically offset teleports have been shown in testing to be overly difficult to manage. As such, they are negated if they appear.
                while (Abs(Vector2.Dot(teleportOffsetDirection, Vector2.UnitY)) >= 0.82f)
                    teleportOffsetDirection = Main.rand.NextVector2Unit();

                // You don't want to know how much pain was put into this line of code.
                teleportOffsetDirection.Y *= 0.5f;

                Vector2 teleportOffset = teleportOffsetDirection * -850f;
                ImmediateTeleportTo(Target.Center + teleportOffset);
                RerollAllSwappableTextures();
            }

            // Calculate the anticipation interpolants.
            float anticipationAnimationRatio = 0.56f;
            float playerDriftAnimationRatio = anticipationAnimationRatio * (NPC.WithinRange(Target.Center, 200f) ? 0.9f : 0.7f);
            float swordAnimationCompletion = InverseLerp(0f, slashAnimationTime - 1f, SwordAnimationTimer - slashDelay);
            float anticipationCompletion = InverseLerp(0f, anticipationAnimationRatio, swordAnimationCompletion);
            float swordScale = InverseLerpBump(-0.1f, 0.07f, 0.5f, 0.8f, swordAnimationCompletion);

            float downwardDirectionToPlayerDot = Abs(Vector2.Dot(NPC.DirectionToSafe(Target.Center), Vector2.UnitY));
            float downwardDirectionLeniancy = InverseLerp(0.54f, 0.8f, downwardDirectionToPlayerDot);
            float handOffsetAngle = SwordSlashDirection == -1 ? -0.49f : -PiOver4;
            if (SwordSlashDirection == -1)
                handOffsetAngle = -handOffsetAngle + Pi;

            // Calculate sword direction values.
            Vector2 handOffsetSquishFactor = new(SwordSlashDirection, 0.5f);
            Vector2 handOffset = handOffsetSquishFactor.SafeNormalize(Vector2.UnitY).RotatedBy(NPC.velocity.ToRotation() + handOffsetAngle) * maxHandOffset;
            Vector2 swordDirection = handOffset.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2);

            // Drift towards the target at first.
            if (swordAnimationCompletion <= playerDriftAnimationRatio)
                NPC.velocity = NPC.DirectionToSafe(Target.Center + Vector2.UnitY * 125f) * 3f;

            // Slash at the target when ready.
            float slashAnticipationCompletion = InverseLerp(0f, slashDelay + (int)((slashAnimationTime - 1f) * anticipationAnimationRatio), SwordAnimationTimer);
            bool slashAboutToHappen = SwordAnimationTimer >= slashDelay + (int)((slashAnimationTime - 1f) * anticipationAnimationRatio * 0.84f);
            bool slashJustHappened = SwordAnimationTimer == slashDelay + (int)((slashAnimationTime - 1f) * anticipationAnimationRatio);
            bool slashHasHappened = SwordAnimationTimer > slashDelay + (int)((slashAnimationTime - 1f) * anticipationAnimationRatio);
            if (slashJustHappened)
            {
                float slashSpeed = NPC.Distance(Target.Center) * 0.22f;
                if (slashSpeed < 140f)
                    slashSpeed = 140f;

                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * slashSpeed;
                NPC.netUpdate = true;

                // Create sounds and visuals.
                StartShakeAtPoint(NPC.Center, 10f);
                NamelessDeityKeyboardShader.BrightnessIntensity = 0.9f;
                RadialScreenShoveSystem.Start(NPC.Center, 45);
                SoundEngine.PlaySound(SwordSlashSound);
                if (!NoxusBossConfig.Instance.PhotosensitivityMode)
                    HighContrastScreenShakeShaderData.ContrastIntensity = SwordSlashCounter * 0.2f + 1.15f;
            }

            // Slow down after the slash.
            if (SwordAnimationTimer > slashDelay + (int)((slashAnimationTime - 1f) * 0.55f) && !NPC.WithinRange(Target.Center, 230f))
                NPC.velocity *= 0.9f;

            // Reset contact damage to 0 if not attacking.
            else
                NPC.damage = 0;

            // Make the sword do damage when the slash is close to happening.
            swordDoesDamage = slashAboutToHappen.ToInt();

            // Move the hands, keeping the sword attached to the active hand.
            if (Hands.Count >= 2)
            {
                // Store the active and idle hands in temporary variables for ease of access.
                int activeHandIndex = SwordSlashDirection == 0 ? 1 : 0;
                int idleHandIndex = 1 - activeHandIndex;
                NamelessDeityHand activeHand = Hands[activeHandIndex];
                NamelessDeityHand idleHand = Hands[idleHandIndex];

                float activeHandHorizontalOffset = Lerp(500f, 420f, downwardDirectionLeniancy) * Lerp(1f, 0.2f, InverseLerpBump(0.3f, 0.76f, 0.95f, 1f, slashAnticipationCompletion));
                if (slashHasHappened)
                    activeHandHorizontalOffset = 1000f;

                activeHand.RotationOffset = 0f;
                DefaultHandDrift(activeHand, NPC.Center + handOffset + Vector2.UnitX * SwordSlashDirection * activeHandHorizontalOffset - Vector2.UnitY * verticalAimOffset, 300f);

                idleHand.RotationOffset = 0f;
                idleHand.DirectionOverride = SwordSlashDirection;
                DefaultHandDrift(idleHand, NPC.Center + new Vector2(SwordSlashDirection * -1000f, 140f) * TeleportVisualsAdjustedScale, 4f);

                // Update the sword.
                if (swords.Any())
                {
                    Projectile sword = swords.First();
                    sword.As<SwordConstellation>().SlashCompletion = anticipationCompletion;
                    sword.rotation = swordDirection.ToRotation();
                    if (SwordSlashDirection == 1)
                        sword.rotation += 0.3f;

                    Vector2 armDirection = (activeHand.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
                    sword.scale = swordScale;
                    sword.Center = activeHand.ActualCenter + handOffset.SafeNormalize(Vector2.UnitY) * 100f;
                    if (slashHasHappened)
                        sword.Center += new Vector2(SwordSlashDirection * 180f, 92f).RotatedBy(NPC.rotation);

                    // Make the sword emit stardust as it's being fired.
                    float stardustSpawnRate = InverseLerp(24f, 60f, NPC.velocity.Length());
                    for (int i = 0; i < 7; i++)
                    {
                        if (Main.rand.NextFloat() >= stardustSpawnRate)
                            continue;

                        int starPoints = Main.rand.Next(3, 9);
                        float starScaleInterpolant = Main.rand.NextFloat();
                        int starLifetime = (int)Lerp(36f, 90f, starScaleInterpolant);
                        float starScale = Lerp(0.7f, 1.9f, starScaleInterpolant);
                        Color starColor = MulticolorLerp(starScaleInterpolant * 0.9f, Color.SkyBlue, Color.Yellow, Color.Orange, Color.Red);
                        starColor = Color.Lerp(starColor, Color.Wheat, 0.4f);

                        Vector2 starVelocity = NPC.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * 30f;
                        TwinkleParticle star = new(sword.Center + Main.rand.NextVector2Circular(60f, 150f).RotatedBy(sword.rotation), starVelocity, starColor, starLifetime, starPoints, new Vector2(Main.rand.NextFloat(0.4f, 1.6f), 1f) * starScale, starColor * 0.5f);
                        star.Spawn();
                    }

                    // Create a screen slice when the slash happens.
                    if (Main.netMode != NetmodeID.MultiplayerClient && slashJustHappened)
                    {
                        Vector2 sliceDirection = NPC.velocity.SafeNormalize(Vector2.UnitY);
                        Vector2 sliceSpawnPosition = activeHand.Center + (sword.rotation - PiOver2).ToRotationVector2() * TeleportVisualsAdjustedScale * -388f;
                        NewProjectileBetter(sliceSpawnPosition - sliceDirection * 2000f, sliceDirection, ModContent.ProjectileType<TelegraphedScreenSlice>(), ScreenSliceDamage, 0f, -1, 3f, 4000f);
                    }
                }
            }
        }
    }
}
