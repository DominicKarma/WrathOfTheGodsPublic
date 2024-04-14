using System.Collections.Generic;
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
        public List<Vector2> StarSpawnOffsets = new();

        public bool StarShouldBeHeldByLeftHand
        {
            get;
            set;
        }

        public static int ConjureExplodingStars_RedirectTime
        {
            get
            {
                if (Main.zenithWorld)
                    return 9;

                return 15;
            }
        }

        public static int ConjureExplodingStars_HoverTime
        {
            get
            {
                if (Main.zenithWorld)
                    return 5;

                return 22;
            }
        }

        public static int ConjureExplodingStars_StarShootCount
        {
            get
            {
                if (Main.zenithWorld)
                    return 8;

                return 6;
            }
        }

        public static int ConjureExplodingStars_StarCreateRate
        {
            get
            {
                if (Main.zenithWorld)
                    return 2;

                return 4;
            }
        }

        public static int ConjureExplodingStars_StarBlastDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 4;

                return 5;
            }
        }

        public static int ConjureExplodingStars_AttackTransitionDelay
        {
            get
            {
                if (Main.zenithWorld)
                    return 10;

                return 72;
            }
        }

        public static int ConjureExplodingStars_ExplosionCount
        {
            get
            {
                if (Main.zenithWorld)
                    return 3;

                return 1;
            }
        }

        public static float ConjureExplodingStars_StarOffsetRadius
        {
            get
            {
                if (Main.zenithWorld)
                    return 450f;

                return 480f;
            }
        }

        public ref float ConjureExplodingStars_ExplosionCounter => ref NPC.ai[2];

        public void LoadStateTransitions_ConjureExplodingStars()
        {
            // Load the transition from ConjureExplodingStars to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.ConjureExplodingStars, null, false, () =>
            {
                return ConjureExplodingStars_ExplosionCounter >= ConjureExplodingStars_ExplosionCount;
            });
        }

        public void DoBehavior_ConjureExplodingStars()
        {
            int redirectTime = ConjureExplodingStars_RedirectTime;
            int hoverTime = ConjureExplodingStars_HoverTime;
            int starShootCount = ConjureExplodingStars_StarShootCount;
            int starCreateRate = ConjureExplodingStars_StarCreateRate;
            int starBlastDelay = ConjureExplodingStars_StarBlastDelay;
            int attackTransitionDelay = ConjureExplodingStars_AttackTransitionDelay;
            int explosionCount = ConjureExplodingStars_ExplosionCount;
            int starTelegraphTime = starShootCount * starCreateRate;
            float starOffsetRadius = ConjureExplodingStars_StarOffsetRadius;
            ref float explosionAngularOffset = ref NPC.ai[3];

            // Initialize the explosion's angular offset on the first frame.
            if (AttackTimer == 1)
            {
                explosionAngularOffset = TwoPi * Main.rand.NextFloat() / ConjureExplodingStars_StarShootCount * 0.37f;
                NPC.netUpdate = true;
            }

            // Fly towards above the target at first. After the redirect and hover time has concluded, however, Nameless becomes comfortable with his current position and slows down rapidly.
            float slowdownRadius = 230f;
            Vector2 hoverDestination = Target.Center - Vector2.UnitY * 380f;
            if (AttackTimer >= redirectTime + hoverTime)
                hoverDestination = NPC.Center;
            NPC.SmoothFlyNearWithSlowdownRadius(hoverDestination, 0.17f, 0.89f, slowdownRadius);

            // Slow down rapidly if flying past the hover destination. If this happens when Nameless is moving really, really fast a sonic boom of sorts is created.
            if (Vector2.Dot(NPC.velocity, NPC.SafeDirectionTo(hoverDestination)) < 0f)
            {
                // Create the sonic boom if necessary.
                if (NPC.velocity.Length() >= 75f)
                {
                    NPC.velocity = NPC.velocity.ClampLength(0f, 74f);
                    SoundEngine.PlaySound(SunFireballShootSound with
                    {
                        Pitch = 0.6f
                    }, Target.Center);
                    ScreenEffectSystem.SetFlashEffect(NPC.Center, 4f, 54);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                }

                NPC.velocity *= 0.67f;
            }

            // Flap wings.
            UpdateWings(AttackTimer / 54f % 1f);

            // Update hands.
            if (Hands.Count >= 2)
            {
                float comeTogetherInterpolant = InverseLerp(0f, 10f, AttackTimer - redirectTime - hoverTime - starTelegraphTime - starBlastDelay);
                float handHoverOffset = Lerp(950f, 660f, comeTogetherInterpolant);
                DefaultHandDrift(Hands[0], NPC.Center + new Vector2(-handHoverOffset, 100f + comeTogetherInterpolant * 120f) * TeleportVisualsAdjustedScale, 2.5f);
                DefaultHandDrift(Hands[1], NPC.Center + new Vector2(handHoverOffset, 100f - comeTogetherInterpolant * 900f) * TeleportVisualsAdjustedScale, 2.5f);

                // Snap fingers and make the screen shake.
                if (AttackTimer == redirectTime + hoverTime - 10f)
                {
                    SoundEngine.PlaySound(FingerSnapSound with
                    {
                        Volume = 4f
                    });
                    StartShakeAtPoint(NPC.Center, 6f);
                }
            }

            // Create star telegraphs.
            if (AttackTimer >= redirectTime + hoverTime && AttackTimer <= redirectTime + hoverTime + starTelegraphTime && AttackTimer % starCreateRate == 1f)
            {
                float starSpawnOffsetAngle = TwoPi * (AttackTimer - redirectTime - hoverTime) / starTelegraphTime - PiOver2 + explosionAngularOffset;
                Vector2 starSpawnOffset = starSpawnOffsetAngle.ToRotationVector2() * starOffsetRadius;
                StarSpawnOffsets.Add(starSpawnOffset);

                Color bloomColor = Color.Lerp(Color.Orange, Color.DarkRed, Main.rand.NextFloat(0.25f, 0.9f));
                CreateTwinkle(Target.Center + starSpawnOffset, Vector2.One * 3f, bloomColor, new(starSpawnOffset, () => Target.Center));

                NPC.netSpam = 0;
                NPC.netUpdate = true;
            }

            // Release stars.
            if (AttackTimer == redirectTime + hoverTime + starTelegraphTime + starBlastDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (Vector2 starSpawnOffset in StarSpawnOffsets)
                        NewProjectileBetter(NPC.GetSource_FromAI(), Target.Center + starSpawnOffset, Vector2.Zero, ModContent.ProjectileType<ExplodingStar>(), ExplodingStarDamage, 0f, -1, 0f, 0.6f);

                    StarSpawnOffsets.Clear();
                    NPC.netSpam = 0;
                    NPC.netUpdate = true;
                }
            }

            if (AttackTimer >= redirectTime + hoverTime + starTelegraphTime * 2f + starBlastDelay + attackTransitionDelay)
            {
                ConjureExplodingStars_ExplosionCounter++;
                if (ConjureExplodingStars_ExplosionCounter < ConjureExplodingStars_ExplosionCount)
                {
                    AttackTimer = 0;
                    NPC.netUpdate = true;
                }
            }
        }
    }
}
