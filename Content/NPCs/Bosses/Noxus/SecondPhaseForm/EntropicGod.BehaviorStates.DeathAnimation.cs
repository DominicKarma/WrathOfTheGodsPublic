using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Metaballs;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        public bool BeingYankedOutOfPortal
        {
            get;
            set;
        }

        public void DoBehavior_DeathAnimation()
        {
            int portalSummonDelay = 90;
            int portalEnterDelay = 30;
            int portalExistTime = 156;
            float portalVerticalOffset = 400f;
            float portalScale = 1.8f;
            Vector2 headCenter = NPC.Center + HeadOffset;
            Vector2 headTangentDirection = (NPC.rotation + HeadRotation).ToRotationVector2();
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Make fog disappear.
            FogIntensity = Clamp(FogIntensity - 0.06f, 0f, 1f);
            FogSpreadDistance = Clamp(FogSpreadDistance - 0.02f, 0f, 1f);

            // Teleport above the player on the first frame.
            if (AttackTimer == 1f)
            {
                Vector2 teleportDestination = Target.Center - Vector2.UnitY * 300f;
                if (teleportDestination.Y < 960f)
                    teleportDestination.Y = 960f;

                TeleportToWithDecal(teleportDestination);
            }

            // Disable damage. It is not relevant for this behavior.
            NPC.damage = 0;
            NPC.dontTakeDamage = true;

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            // Make Noxus move his hands up in a surrendering position once Nameless arrives.
            float surrenderMotionInterpolant = Pow(InverseLerp(0.7f, 0.95f, NoxusDeathCutsceneSystem.EyeAppearInterpolant), 0.8f);
            float fearInterpolant = InverseLerp(0.7f, 1f, NoxusDeathCutsceneSystem.EyeAppearInterpolant);
            leftHandDestination = Vector2.Lerp(leftHandDestination, NPC.Center + new Vector2(-100f, -240f) * NPC.scale, surrenderMotionInterpolant);
            rightHandDestination = Vector2.Lerp(rightHandDestination, NPC.Center + new Vector2(100f, -240f) * NPC.scale, surrenderMotionInterpolant);

            // Make Noxus move his hands up to his head after being violently slashed at by Nameless.
            float deathStretchInterpolant = Pow(InverseLerp(10f, 40f, NoxusDeathCutsceneSystem.SlashTimer), 0.8f);
            HeadRotation = deathStretchInterpolant * Pi / 9f;

            leftHandDestination = Vector2.Lerp(leftHandDestination, headCenter + headTangentDirection * NPC.scale * -40f + Main.rand.NextVector2Circular(7f, 7f) + Vector2.UnitY * 20f, deathStretchInterpolant);
            rightHandDestination = Vector2.Lerp(rightHandDestination, headCenter + headTangentDirection * NPC.scale * 40f + Main.rand.NextVector2Circular(7f, 7f) + Vector2.UnitY * 20f, deathStretchInterpolant);

            // Turn off the music.
            Music = 0;

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, 2f);
            DefaultHandDrift(Hands[1], rightHandDestination, 2f);
            Hands[0].Center += Main.rand.NextVector2Unit() * fearInterpolant * (1f - deathStretchInterpolant) * 3f;
            Hands[1].Center += Main.rand.NextVector2Unit() * fearInterpolant * (1f - deathStretchInterpolant) * 3f;

            if (fearInterpolant > 0f)
                NPC.Center += Main.rand.NextVector2Unit() * (1.4f - NPC.Opacity) * 16f;

            // Create the portal.
            if (AttackTimer == portalSummonDelay)
            {
                SoundEngine.PlaySound(FireballShootSound);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center + Vector2.UnitY * portalVerticalOffset, -Vector2.UnitY, ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalExistTime);
            }

            // Make the camera pan on Noxus.
            float cameraInterpolant = Pow(InverseLerp(1f, 40f, AttackTimer - portalSummonDelay - portalEnterDelay + 96f), 4f);
            CameraPanSystem.Zoom = cameraInterpolant * -0.2f + NoxusDeathCutsceneSystem.EyeAppearInterpolant * 0.425f;
            CameraPanSystem.CameraFocusPoint = Vector2.Lerp(CameraPanSystem.CameraFocusPoint, NPC.Center, 0.6f);
            CameraPanSystem.CameraPanInterpolant = cameraInterpolant;

            // Move into the portal and attempt to leave.
            if (AttackTimer >= portalSummonDelay + portalEnterDelay)
            {
                if (AttackTimer >= portalSummonDelay + portalEnterDelay + 26f)
                {
                    float yankSpeed = InverseLerp(27f, 6f, AttackTimer - portalSummonDelay - portalEnterDelay - 26f) * 70f;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, -Vector2.UnitY * yankSpeed, 0.12f);
                    NPC.rotation += Main.rand.NextFloatDirection() * yankSpeed * 0.2f;

                    BeingYankedOutOfPortal = true;
                }
                else
                    NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.UnitY * portalVerticalOffset / 25f, 0.096f);

                if (AttackTimer == portalSummonDelay + portalEnterDelay + 26f)
                {
                    SoundEngine.PlaySound(GlitchSound);
                    NoxusDeathCutsceneSystem.Start();
                }
            }

            // Die.
            if ((AttackTimer >= portalSummonDelay + portalExistTime && NoxusDeathCutsceneSystem.AnimationTimer <= 0) || NamelessDeityBoss.Myself is not null)
            {
                if (NamelessDeityBoss.Myself is not null)
                    Explode();

                NPC.life = 0;
                NPC.HitEffect(0, 9999);
                NPC.NPCLoot();
                NPC.checkDead();
                NPC.active = false;
            }
        }

        public void Explode()
        {
            SoundEngine.PlaySound(JumpscareSound with { Volume = 0.8f });
            SoundEngine.PlaySound(NamelessDeityBoss.MomentOfCreationSound with { Volume = 2f });

            for (int i = 0; i < 240; i++)
            {
                Vector2 gasSpawnPosition = NPC.Center + Main.rand.NextVector2Circular(82f, 82f);
                float gasSize = NPC.width * Main.rand.NextFloat(0.9f, 4f);
                PitchBlackMetaball.CreateParticle(gasSpawnPosition, Main.rand.NextVector2Circular(27f, 27f), gasSize);
            }
        }
    }
}
