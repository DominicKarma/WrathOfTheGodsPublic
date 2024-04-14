using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Core.Graphics.Shaders.Keyboard;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public static int ArcingEyeStarbursts_SquintTime
        {
            get
            {
                if (Main.zenithWorld)
                    return 4;

                return 32;
            }
        }

        public static int ArcingEyeStarbursts_ShootDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 10;

                return 70;
            }
        }

        public static int ArcingEyeStarbursts_StarburstCount
        {
            get
            {
                if (Main.zenithWorld)
                    return 36;

                return 27;
            }
        }

        public static int ArcingEyeStarbursts_AttackTransitionDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 12;

                return 78;
            }
        }

        public static float ArcingEyeStarbursts_StarburstArc
        {
            get
            {
                return ToRadians(396f);
            }
        }

        public static float ArcingEyeStarbursts_StarburstShootSpeed
        {
            get
            {
                return 24f;
            }
        }

        public static int ArcingEyeStarbursts_TotalDuration => ArcingEyeStarbursts_ShootDelay + ArcingEyeStarbursts_StarburstCount + ArcingEyeStarbursts_AttackTransitionDelay;

        public void LoadStateTransitions_ArcingEyeStarbursts()
        {
            // Load the transition from ArcingEyeStarbursts to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.ArcingEyeStarbursts, null, false, () =>
            {
                return AttackTimer >= ArcingEyeStarbursts_TotalDuration;
            });
        }

        public void DoBehavior_ArcingEyeStarbursts()
        {
            int squintTime = ArcingEyeStarbursts_SquintTime;
            int shootDelay = ArcingEyeStarbursts_ShootDelay;
            int starburstCount = ArcingEyeStarbursts_StarburstCount;
            int attackTransitionDelay = ArcingEyeStarbursts_AttackTransitionDelay;

            // Flap wings.
            UpdateWings(AttackTimer / 45f % 1f);

            // Apparently needed after black hole attack??? The fuck????????
            DestroyAllHands();

            // Teleport above the target at first.
            if (AttackTimer == 1)
                StartTeleportAnimation(() => Target.Center - Vector2.UnitY * 372f, 12, 12);

            // Quickly hover above the player, zipping back and forth, before firing.
            float lookAtTargetInterpolant = InverseLerp(shootDelay * 0.5f, shootDelay - 6f, AttackTimer);
            Vector2 sideOfPlayer = Target.Center + (Target.Center.X < NPC.Center.X).ToDirectionInt() * Vector2.UnitX * 800f;
            Vector2 hoverDestination = Vector2.Lerp(Target.Center + GeneralHoverOffset, sideOfPlayer, Pow(lookAtTargetInterpolant, 0.5f));
            Vector2 idealVelocity = (hoverDestination - NPC.Center) * Sqrt(1f - lookAtTargetInterpolant) * 0.14f;
            NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.17f);

            // Decide a hover offset if it's unitialized or has been reached.
            if (GeneralHoverOffset == Vector2.Zero || (NPC.WithinRange(Target.Center + GeneralHoverOffset, Target.Velocity.Length() * 2f + 90f) && AttackTimer % 20f == 0f))
            {
                // Make the screen rumble a little bit.
                if (OverallShakeIntensity <= 1f)
                {
                    StartShakeAtPoint(NPC.Center, 6.5f);
                    SoundEngine.PlaySound(SuddenMovementSound);
                }

                float horizontalOffsetSign = GeneralHoverOffset.X == 0f ? Main.rand.NextFromList(-1f, 1f) : -Sign(GeneralHoverOffset.X);
                GeneralHoverOffset = new Vector2(horizontalOffsetSign * Main.rand.NextFloat(500f, 700f), Main.rand.NextFloat(-550f, -340f));
                NPC.netUpdate = true;
            }

            // Shoot the redirecting starbursts.
            if (AttackTimer >= shootDelay && AttackTimer <= shootDelay + starburstCount)
            {
                // Create a light explosion and initial spread of starbursts on the first frame.
                if (AttackTimer == shootDelay)
                {
                    RadialScreenShoveSystem.Start(EyePosition, 30);

                    StartShakeAtPoint(NPC.Center, 10f);
                    SoundEngine.PlaySound(GenericBurstSound);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 21; i++)
                        {
                            Vector2 starburstVelocity = (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY).RotatedBy(TwoPi * i / 21f) * ArcingEyeStarbursts_StarburstShootSpeed * 0.08f;
                            NewProjectileBetter(NPC.GetSource_FromAI(), EyePosition, starburstVelocity, ModContent.ProjectileType<Starburst>(), StarburstDamage, 0f);
                        }
                    }

                    NamelessDeityKeyboardShader.BrightnessIntensity += 0.67f;
                    ScreenEffectSystem.SetBlurEffect(EyePosition, 0.6f, 24);
                }

                // Release the projectiles.
                float starburstInterpolant = InverseLerp(0f, starburstCount, AttackTimer - shootDelay);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float starburstShootOffsetAngle = Lerp(-ArcingEyeStarbursts_StarburstArc, ArcingEyeStarbursts_StarburstArc, starburstInterpolant);
                    Vector2 starburstVelocity = (Target.Center - EyePosition).SafeNormalize(Vector2.UnitY).RotatedBy(starburstShootOffsetAngle) * ArcingEyeStarbursts_StarburstShootSpeed;
                    NewProjectileBetter(NPC.GetSource_FromAI(), EyePosition, starburstVelocity, ModContent.ProjectileType<ArcingStarburst>(), StarburstDamage, 0f);
                }

                // Create sound and screen effects.
                ScreenEffectSystem.SetChromaticAberrationEffect(EyePosition, starburstInterpolant * 3f, 10);

                // Play fireball sounds.
                if (Main.rand.NextBool(3))
                {
                    SoundEngine.PlaySound(SunFireballShootSound with
                    {
                        MaxInstances = 100,
                        Volume = 0.5f,
                        Pitch = -0.2f
                    });
                }
            }

            // Update universal hands.
            float handHoverOffset = Utils.Remap(AttackTimer - shootDelay - starburstCount, 0f, starburstCount + 12f, 900f, 1200f);
            DefaultUniversalHandMotion(handHoverOffset);
        }
    }
}
