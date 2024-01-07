using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public partial class NamelessDeityBoss : ModNPC
    {
        public static int Glock_ShootTime => 420;

        public void LoadStateTransitions_Glock()
        {
            // Load the transition from SwordConstellation to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.Glock, null, false, () =>
            {
                return AttackTimer >= Glock_ShootTime;
            }, () =>
            {
                for (int i = 0; i < Hands.Count; i++)
                    Hands[i].HasGlock = false;
            });
        }

        public void DoBehavior_Glock()
        {
            int shootRate = 20;
            int shootTime = Glock_ShootTime;
            ref float verticalRecoilOffset = ref NPC.ai[2];

            // Update wings.
            UpdateWings(AttackTimer / 50f % 1f);

            // Teleport to the right of the player on the first frame.
            if (AttackTimer == 1)
                StartTeleportAnimation(() => Target.Center - Vector2.UnitX * 900f, 12, 12);

            // Hover near the player.
            NPC.SmoothFlyNearWithSlowdownRadius(Target.Center - Vector2.UnitX * 1108f, 0.15f, 0.85f, 55f);

            // Shoot stars from the glock.
            if (AttackTimer % shootRate == shootRate - 1)
            {
                SoundEngine.PlaySound(LowQualityGunShootSound);
                StartShake(15f, shakeStrengthDissipationIncrement: 0.8f);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 glockEnd = Hands[0].Center;

                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(star =>
                    {
                        star.As<BackgroundStar>().ApproachingScreen = true;
                    });
                    NewProjectileBetter(glockEnd, Vector2.Zero, ModContent.ProjectileType<BackgroundStar>(), 0, 0f, -1, 0.3f);

                    // Apply a bit of recoil to Nameless.
                    Vector2 pushBackForce = (Target.Center - glockEnd).SafeNormalize(Vector2.UnitY) * 20f;
                    NPC.velocity -= pushBackForce;
                    NPC.netUpdate = true;
                }

                verticalRecoilOffset = -450f;
            }

            // Use the United States flag after a round has been fired.
            if (AttackTimer >= shootRate)
                NamelessDeitySky.UnitedStatesFlagOpacity = 0.3f;

            // Play an eagle sound the first time the gun is shot.
            if (AttackTimer == shootRate)
                SoundEngine.PlaySound(NotActuallyAnEagleSound);

            // Ensure the background sky stays normal.
            NamelessDeitySky.HeavenlyBackgroundIntensity = 1f;

            // Make the recoil go away over time.
            verticalRecoilOffset *= 0.87f;

            // Update hands.
            if (Hands.Count >= 2)
            {
                DefaultHandDrift(Hands[0], NPC.Center + NPC.DirectionToSafe(Target.Center + Vector2.UnitY * (verticalRecoilOffset + 150f)) * 990f + Vector2.UnitX * verticalRecoilOffset * 0.56f, 3.1f);
                Hands[0].HasGlock = AttackTimer >= 2;

                DefaultHandDrift(Hands[1], NPC.Center + new Vector2(-900f, -120f) * TeleportVisualsAdjustedScale, 2.5f);
            }
        }
    }
}
