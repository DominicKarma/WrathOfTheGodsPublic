using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public static int InwardStarPattenedExplosions_StarCreationDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 7;

                return 21;
            }
        }

        public static int InwardStarPattenedExplosions_StarCreationTime
        {
            get
            {
                if (Main.zenithWorld)
                    return 22;

                return 68;
            }
        }

        public static int InwardStarPattenedExplosions_AttackTransitionDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 120;

                return 210;
            }
        }

        public static float InwardStarPattenedExplosions_SpinRadius
        {
            get
            {
                if (Main.zenithWorld)
                    return 185f;

                return 220f;
            }
        }

        public static int InwardStarPattenedExplosions_AttackDuration => InwardStarPattenedExplosions_StarCreationDelay + InwardStarPattenedExplosions_StarCreationTime + InwardStarPattenedExplosions_AttackTransitionDelay;

        public void LoadStateTransitions_InwardStarPattenedExplosions()
        {
            // Load the transition from InwardStarPattenedExplosions to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.InwardStarPattenedExplosions, null, false, () =>
            {
                return AttackTimer >= InwardStarPattenedExplosions_AttackDuration;
            });
        }

        public void DoBehavior_InwardStarPattenedExplosions()
        {
            int starCreationDelay = InwardStarPattenedExplosions_StarCreationDelay;
            int starCreationTime = InwardStarPattenedExplosions_StarCreationTime;
            int attackTransitionDelay = InwardStarPattenedExplosions_AttackTransitionDelay;
            float spinRadius = InwardStarPattenedExplosions_SpinRadius;
            float handMoveSpeedFactor = 3.7f;
            ref float spinDirection = ref NPC.ai[2];
            ref float starOffset = ref NPC.ai[3];
            Vector2 leftHandHoverDestination = NPC.Center + new Vector2(-660f - ZPosition * 40f, 118f);
            Vector2 rightHandHoverDestination = NPC.Center + new Vector2(660f + ZPosition * 40f, 118f);

            // Flap wings.
            UpdateWings(AttackTimer / 48f % 1f);

            // Hover above the player at first.
            if (AttackTimer <= starCreationDelay)
            {
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * spinRadius;
                NPC.SmoothFlyNear(hoverDestination, 0.16f, 0.88f);

                // Begin a teleport.
                if (AttackTimer == 1)
                    StartTeleportAnimation(() => Target.Center - Vector2.UnitY * spinRadius, 11, 11);
            }

            // Spin around the player and conjure the star.
            else if (AttackTimer <= starCreationDelay + starCreationTime)
            {
                // Decide the spin direction if that hasn't happened yet, based on which side of the player Nameless is.
                // This is done so that the spin continues moving in the direction the hover made Nameless move.
                if (spinDirection == 0f)
                {
                    spinDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                    NPC.netUpdate = true;
                }

                int movementDelay = starCreationDelay + starCreationTime - (int)AttackTimer + 17;
                float spinCompletionRatio = InverseLerp(0f, starCreationTime, AttackTimer - starCreationDelay);
                float spinOffsetAngle = SmoothStep(0f, TwoPi, InverseLerp(0f, starCreationTime, AttackTimer - starCreationDelay)) * spinDirection;
                float hoverSnapInterpolant = InverseLerp(0f, 5f, AttackTimer - starCreationDelay) * 0.48f;
                Vector2 spinOffset = -Vector2.UnitY.RotatedBy(spinOffsetAngle) * spinRadius;
                Vector2 spinDestination = Target.Center + spinOffset;

                // Spin around the target.
                NPC.Center = Vector2.Lerp(NPC.Center, spinDestination, hoverSnapInterpolant);
                NPC.velocity = spinOffset.SafeNormalize(Vector2.UnitY).RotatedBy(PiOver2 * spinDirection) * 25f;

                // Enter the background.
                ZPosition = Pow(spinCompletionRatio, 2f) * 10f;

                if (AttackTimer % 2f == 0f)
                {
                    if (AttackTimer % 4f == 0f)
                        SoundEngine.PlaySound(SunFireballShootSound);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NewProjectileBetter(CensorPosition, (TwoPi * spinCompletionRatio).ToRotationVector2() * 8f, ModContent.ProjectileType<StarPatterenedStarburst>(), StarburstDamage, 0f, -1, 0f, movementDelay + 5);

                        int star = NewProjectileBetter(CensorPosition, (TwoPi * spinCompletionRatio + Pi / 5f).ToRotationVector2() * 8f, ModContent.ProjectileType<StarPatterenedStarburst>(), StarburstDamage, 0f, -1, 0f, movementDelay + 9);
                        if (Main.projectile.IndexInRange(star))
                        {
                            Main.projectile[star].As<StarPatterenedStarburst>().RadiusOffset = 400f;
                            Main.projectile[star].As<StarPatterenedStarburst>().ConvergenceAngleOffset = Pi / 5f;
                        }

                        star = NewProjectileBetter(CensorPosition, (TwoPi * spinCompletionRatio + TwoPi / 5f).ToRotationVector2() * 8f, ModContent.ProjectileType<StarPatterenedStarburst>(), StarburstDamage, 0f, -1, 0f, movementDelay + 16);
                        if (Main.projectile.IndexInRange(star))
                        {
                            Main.projectile[star].As<StarPatterenedStarburst>().RadiusOffset = 900f;
                            Main.projectile[star].As<StarPatterenedStarburst>().ConvergenceAngleOffset = TwoPi / 5f;
                        }
                    }
                }
            }

            // Continue arcing as the stars do their thing.
            else
            {
                if (AttackTimer == starCreationDelay + starCreationTime + 16f)
                {
                    SoundEngine.PlaySound(SupernovaSound);
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 1f, 13);
                }

                // Accelerate and continue arcing until very, very fast.
                if (NPC.velocity.Length() <= 105f)
                    NPC.velocity = NPC.velocity.RotatedBy(TwoPi * spinDirection / 210f) * 1.08f;

                // Keep entering the background.
                ZPosition *= 1.02f;

                // Fade out while accelerating.
                NPC.Opacity = Clamp(NPC.Opacity - 0.02f, 0f, 1f);

                // Silently hover above the player when completely invisible.
                // This has no effect on the aesthetics and the player will not notice this, but it helps significantly in ensuring that Nameless isn't very far from the player when the next attack begins.
                if (NPC.Opacity <= 0f)
                {
                    NPC.velocity = Vector2.Zero;
                    NPC.Center = Target.Center - Vector2.UnitY * 560f;
                }

                if (AttackTimer == starCreationDelay + starCreationTime + attackTransitionDelay - 1)
                    DestroyAllHands();
            }

            if (Hands.Count >= 2)
            {
                Hands[0].RotationOffset = 0f;
                Hands[1].RotationOffset = 0f;
                Hands[0].DirectionOverride = 0;
                Hands[1].DirectionOverride = 0;
                DefaultHandDrift(Hands[0], rightHandHoverDestination, handMoveSpeedFactor);
                DefaultHandDrift(Hands[1], leftHandHoverDestination, handMoveSpeedFactor);
            }
        }
    }
}
