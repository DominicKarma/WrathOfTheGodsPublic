using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core.CrossCompatibility.Inbound;
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
        public void LoadStateTransitions_CrushStarIntoQuasar()
        {
            // Load the transition from CrushStarIntoQuasar to the next in the cycle.
            // These happens if either there's no star or quasar projectile. Since the quasar eventually disappears, this process catches both edge-cases and natural attack cycle progression.
            StateMachine.RegisterTransition(NamelessAIType.CrushStarIntoQuasar, null, false, () =>
            {
                bool noStarsOrQuasars = !AnyProjectiles(ModContent.ProjectileType<ControlledStar>()) && !AnyProjectiles(ModContent.ProjectileType<Quasar>());
                return noStarsOrQuasars;
            }, () => NPC.Opacity = 1f);
        }

        public void DoBehavior_CrushStarIntoQuasar()
        {
            int redirectTime = 45;
            int starPressureTime = 111;
            int supernovaDelay = 30;
            int pressureArmsCount = 7;
            int plasmaShootDelay = 60;
            int plasmaShootRate = 5;
            int plasmaSkipChance = 0;
            int plasmaShootTime = Supernova.Lifetime - 90;
            bool centerSparksAroundQuasar = false;
            float plasmaShootSpeed = 9f;
            float handOrbitOffset = 100f;
            float pressureInterpolant = InverseLerp(redirectTime, redirectTime + starPressureTime, AttackTimer);

            // Flap wings.
            UpdateWings(AttackTimer / 40f % 1f);

            // Apply difficulty-specific balancing.
            if (CommonCalamityVariables.RevengeanceModeActive)
                plasmaShootRate--;
            if (CommonCalamityVariables.DeathModeActive)
            {
                // The balancing reasons behind this are a bit complicated.
                // Fundamentally, I don't believe the sparks being centered around the quasar is good for introductory behaviors for the boss.
                // When it's relative to the quasar, it sets an incredibly high bar of mechanical expectations, because at that point in order to
                // learn the attack you have to not just how to move the player, but secretly also how to move the quasar, which is far less easy.
                // Testers have had mixed reactions on it, and I don't want to necessarily deny those who can handle and enjoy that skill bar, hence this being in death mode.
                // However, for everyone else I think it's more consistent with the rest of Nameless' difficulty to not have this be the case.
                centerSparksAroundQuasar = true;
                plasmaShootRate -= 2;
            }

            // Make things faster in successive phases.
            if (CurrentPhase >= 1)
            {
                plasmaShootRate--;
                plasmaSkipChance = 3;
            }

            Projectile star = null;
            Projectile quasar = null;
            List<Projectile> stars = AllProjectilesByID(ModContent.ProjectileType<ControlledStar>()).ToList();
            List<Projectile> quasars = AllProjectilesByID(ModContent.ProjectileType<Quasar>()).ToList();
            if (stars.Any())
                star = stars.First();
            if (quasars.Any())
                quasar = quasars.First();

            // Make relative darkening effects from the sun attack dissipate.
            RelativeDarkening = Clamp(RelativeDarkening - 0.045f, 0f, 1f);
            NamelessDeitySky.HeavenlyBackgroundIntensity = 1f - RelativeDarkening;

            // Conjure hands and destroy leftover starbursts on the first frame.
            if (AttackTimer == 1f)
            {
                int arcingStarburstID = ModContent.ProjectileType<ArcingStarburst>();
                int starburstID = ModContent.ProjectileType<Starburst>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (!p.active || (p.type != arcingStarburstID && p.type != starburstID))
                        continue;

                    p.Kill();
                }

                for (int i = 0; i < pressureArmsCount; i++)
                    ConjureHandsAtPosition(star.Center, Vector2.Zero);
                return;
            }

            // Make the star slowly attempt to drift below the player.
            if (star is not null)
            {
                Vector2 starHoverDestination = Target.Center + new Vector2((Target.Center.X < star.Center.X).ToDirectionInt() * 450f, -20f);
                star.velocity = Vector2.Lerp(star.velocity, star.DirectionToSafe(starHoverDestination) * 8f, 0.04f);
            }

            // Have Nameless rapidly attempt to hover above the player at first, with a bit of a horizontal offset.
            if (AttackTimer < redirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 300f, -250f);
                NPC.SmoothFlyNear(hoverDestination, 0.12f, 0.85f);

                // Keep the store below Xeoc while hovering.
                if (star is not null)
                    star.Center = Vector2.Lerp(star.Center, NPC.Center + Vector2.UnitY * TeleportVisualsAdjustedScale * 350f, 0.15f);

                // Perform teleport effects.
                TeleportVisualsInterpolant = InverseLerp(-12f, 0f, AttackTimer - redirectTime) * 0.5f;
            }
            else
            {
                // Teleport away from the player on the first frame.
                if (AttackTimer == redirectTime)
                {
                    RadialScreenShoveSystem.Start(EyePosition, 45);
                    NamelessDeityKeyboardShader.BrightnessIntensity += 0.6f;
                }

                // Hover in place silently below the player.
                TeleportVisualsInterpolant = 0f;
                NPC.Center = Target.Center + Vector2.UnitY * 1900f;
                NPC.dontTakeDamage = true;
                if (star is null)
                    NPC.Opacity = 0f;

                // Make hands close in on the star, as though collapsing it.
                // The hands jitter a bit during this, as a way of indicating that they're fighting slightly to collapse the star.
                if (star is not null)
                {
                    float starScale = Remap(pressureInterpolant, 0.1f, 0.8f, ControlledStar.MaxScale, 0.8f);
                    star.scale = starScale;
                    star.ai[1] = pressureInterpolant;
                    handOrbitOffset += Sin(AttackTimer / 4f) * pressureInterpolant * 8f;
                }

                // Make remaining universal hands invisible.
                if (AttackTimer >= redirectTime + starPressureTime + supernovaDelay)
                {
                    for (int i = 0; i < Hands.Count; i++)
                        Hands[i].Opacity = 0f;
                }
            }

            // Make hands circle the star.
            for (int i = 0; i < Hands.Count; i++)
            {
                if (star is not null)
                {
                    DefaultHandDrift(Hands[i], star.Center + (TwoPi * i / Hands.Count + AttackTimer / 18f).ToRotationVector2() * (star.scale * handOrbitOffset + 100f) + Vector2.One * 20f, 1.4f);
                    Hands[i].RotationOffset = Hands[i].Center.AngleTo(star.Center) + PiOver2;
                }
                Hands[i].Center += Main.rand.NextVector2Circular(17f, 17f) * Pow(pressureInterpolant, 2f);
                Hands[i].HasArms = false;
            }

            // Play the star crush sound.
            if (AttackTimer == redirectTime + starPressureTime + supernovaDelay - 183)
                SoundEngine.PlaySound(StarCrushSound);

            // Destroy the star and create a supernova and quasar.
            if (AttackTimer == redirectTime + starPressureTime + supernovaDelay - 1)
            {
                // Apply sound and visual effects.
                SoundEngine.PlaySound(Supernova2Sound);
                if (star is not null && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<Supernova>(), 0, 0f);
                    NewProjectileBetter(star.Center, Vector2.Zero, ModContent.ProjectileType<Quasar>(), QuasarDamage, 0f, -1, 0f, 0f, Supernova.Lifetime);
                }

                if (star is not null)
                {
                    StartShakeAtPoint(star.Center, 25f);
                    ScreenEffectSystem.SetChromaticAberrationEffect(star.Center, 1.5f, 54);
                    NamelessDeityKeyboardShader.BrightnessIntensity = 1f;

                    star.Kill();
                }

                // Delete the hands.
                DestroyAllHands();

                NPC.netUpdate = true;
            }

            // Create plasma around the player that converges into the black quasar.
            if (quasar is not null && AttackTimer >= redirectTime + starPressureTime + supernovaDelay + plasmaShootDelay && AttackTimer <= redirectTime + starPressureTime + supernovaDelay + plasmaShootDelay + plasmaShootTime && AttackTimer % plasmaShootRate == 0f)
            {
                if (plasmaSkipChance >= 1 && Main.rand.NextBool(plasmaSkipChance))
                    return;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float spawnOffsetBoost = centerSparksAroundQuasar ? quasar.Distance(Target.Center) : 150f;
                    Vector2 plasmaSpawnCenter = centerSparksAroundQuasar ? quasar.Center : Target.Center;
                    Vector2 plasmaSpawnPosition = plasmaSpawnCenter + (TwoPi * AttackTimer / 30f).ToRotationVector2() * (Main.rand.NextFloat(600f, 700f) + spawnOffsetBoost);
                    Vector2 plasmaVelocity = (plasmaSpawnCenter - plasmaSpawnPosition).SafeNormalize(Vector2.UnitY) * plasmaShootSpeed;
                    while (Target.Center.WithinRange(plasmaSpawnPosition, 1040f))
                        plasmaSpawnPosition -= plasmaVelocity;

                    NewProjectileBetter(plasmaSpawnPosition, plasmaVelocity, ModContent.ProjectileType<ConvergingSupernovaEnergy>(), SupernovaEnergyDamage, 0f);
                }
            }
        }
    }
}
