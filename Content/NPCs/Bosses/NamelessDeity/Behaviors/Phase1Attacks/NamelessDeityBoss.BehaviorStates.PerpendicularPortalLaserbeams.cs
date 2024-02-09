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
        public int PerpendicularPortalLaserbeams_FanVariant
        {
            get;
            set;
        }

        public static int PerpendicularPortalLaserbeams_ChargeCount
        {
            get
            {
                return 3;
            }
        }

        public ref float PerpendicularPortalLaserbeams_ChargeCounter => ref NPC.ai[3];

        public void LoadStateTransitions_PerpendicularPortalLaserbeams()
        {
            // Load the transition from PerpendicularPortalLaserbeams to the next in the cycle.
            StateMachine.RegisterTransition(NamelessAIType.PerpendicularPortalLaserbeams, null, false, () =>
            {
                return PerpendicularPortalLaserbeams_ChargeCounter >= PerpendicularPortalLaserbeams_ChargeCount;
            });
        }

        public void DoBehavior_PerpendicularPortalLaserbeams()
        {
            int closeRedirectTime = 25;
            int farRedirectTime = 16;
            int horizontalChargeTime = 50;
            int portalExistTime = 40;
            int chargeCount = PerpendicularPortalLaserbeams_ChargeCount;
            int laserShootTime = 32;
            float chargeSpeedFactor = 1f;
            ref float chargeDirectionSign = ref NPC.ai[2];

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                closeRedirectTime = 11;
                horizontalChargeTime = 25;
                portalExistTime = 30;
                laserShootTime = 24;
            }

            bool verticalCharges = PerpendicularPortalLaserbeams_ChargeCounter % 2f == 1f;
            float laserAngularVariance = verticalCharges ? 0.02f : 0.05f;
            float fastChargeSpeedInterpolant = verticalCharges ? 0.184f : 0.13f;
            int portalReleaseRate = verticalCharges ? 3 : 4;

            // Flap wings.
            UpdateWings(AttackTimer / 42f % 1f);

            // Update universal hands.
            DefaultUniversalHandMotion();

            // Choose a fan to use on the first frame, and lock it henceforth.
            if (AttackTimer == 1)
                PerpendicularPortalLaserbeams_FanVariant = Utils.SelectRandom(Main.rand, 1, 3, 4);
            FinsTexture?.ForceToVariant(PerpendicularPortalLaserbeams_FanVariant);

            // Make the fan animation speed increase.
            FanAnimationSpeed = Lerp(FanAnimationSpeed, 7f, 0.166f);

            // Move to the side of the player.
            if (AttackTimer <= closeRedirectTime)
            {
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 350f, 300f);

                // Teleport to the hover destination on the first frame.
                if (AttackTimer == 1f)
                {
                    ImmediateTeleportTo(hoverDestination);
                    RerollAllSwappableTextures();
                    StartShakeAtPoint(NPC.Center, 8f);
                }

                // Fade in.
                NPC.Opacity = InverseLerp(3f, 10f, AttackTimer);

                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.39f);
                NPC.velocity *= 0.85f;
                return;
            }

            // Move back a bit from the player.
            if (AttackTimer <= closeRedirectTime + farRedirectTime)
            {
                float flySpeed = Remap(AttackTimer - closeRedirectTime, 0f, farRedirectTime - 4f, 45f, 80f) * chargeSpeedFactor;
                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 1450f, -Target.Velocity.Y * 12f - 542f);
                if (verticalCharges)
                    hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 960f - Target.Velocity.X * 12f, 1075f);

                // Handle movement.
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, 0.026f);
                NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.DirectionToSafe(hoverDestination) * flySpeed, 0.15f);

                if (AttackTimer == closeRedirectTime + 1)
                {
                    chargeDirectionSign = (Target.Center.X > NPC.Center.X).ToDirectionInt();
                    if (verticalCharges)
                        chargeDirectionSign = -1f;

                    NPC.velocity.Y *= 0.6f;
                    NPC.netUpdate = true;
                }

                return;
            }

            // Perform the charge.
            if (AttackTimer <= closeRedirectTime + farRedirectTime + horizontalChargeTime)
            {
                // Release primordial stardust from the fans.
                Vector2 leftFanPosition = NPC.Center + new Vector2(-280f, -54f).RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale;
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % 4f == 1f)
                {
                    Vector2 stardustVelocity = -Vector2.UnitY.RotatedByRandom(0.58f) * Main.rand.NextFloat(3f, 40f);
                    if (verticalCharges)
                    {
                        bool stardustGoesRight = NPC.Center.X >= Target.Center.X;
                        stardustVelocity = stardustVelocity.RotatedBy(stardustGoesRight.ToDirectionInt() * PiOver2);
                    }

                    NewProjectileBetter(leftFanPosition - Vector2.UnitX * 150f, stardustVelocity, ModContent.ProjectileType<PrimordialStardust>(), PrimordialStardustDamage, 0f);
                }

                // Release portals. If the charge is really close to the target they appear regardless of the timer, to ensure that they can't just stand still.
                bool forcefullySpawnPortal = (Distance(NPC.Center.X, Target.Center.X) <= 90f && !verticalCharges) || (Distance(NPC.Center.Y, Target.Center.Y) <= 55f && verticalCharges);
                if (Main.netMode != NetmodeID.MultiplayerClient && (AttackTimer % portalReleaseRate == 1f || forcefullySpawnPortal) && AttackTimer >= closeRedirectTime + farRedirectTime + 5f)
                {
                    int remainingChargeTime = horizontalChargeTime - (int)(AttackTimer - closeRedirectTime - farRedirectTime);
                    int fireDelay = remainingChargeTime + 14;
                    float portalScale = Main.rand.NextFloat(0.54f, 0.67f);

                    Vector2 portalDirection = ((verticalCharges ? Vector2.UnitX : Vector2.UnitY) * NPC.DirectionToSafe(Target.Center)).SafeNormalize(Vector2.UnitY).RotatedByRandom(laserAngularVariance);

                    // Summon the portal and shoot the telegraph for the laser.
                    NewProjectileBetter(NPC.Center + portalDirection * Main.rand.NextFloatDirection() * 20f, portalDirection, ModContent.ProjectileType<LightPortal>(), 0, 0f, -1, portalScale, portalExistTime + remainingChargeTime + 15, fireDelay);
                    NewProjectileBetter(NPC.Center, portalDirection, ModContent.ProjectileType<TelegraphedPortalLaserbeam>(), PortalLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);

                    // Spawn a second telegraph laser in the opposite direction if a portal was summoned due to being close to the target.
                    // This is done to prevent just flying up/forward to negate the attack.
                    if (forcefullySpawnPortal)
                    {
                        portalDirection *= -1f;
                        NewProjectileBetter(NPC.Center, portalDirection, ModContent.ProjectileType<TelegraphedPortalLaserbeam>(), PortalLaserbeamDamage, 0f, -1, fireDelay, laserShootTime);
                    }
                }

                // Go FAST.
                float oldSpeed = NPC.velocity.Length();
                Vector2 chargeDirectionVector = verticalCharges ? Vector2.UnitY * chargeDirectionSign : Vector2.UnitX * chargeDirectionSign;
                NPC.velocity = Vector2.Lerp(NPC.velocity, chargeDirectionVector * chargeSpeedFactor * 150f, fastChargeSpeedInterpolant);
                if (NPC.velocity.Length() >= chargeSpeedFactor * 92f && oldSpeed <= chargeSpeedFactor * 91f)
                {
                    SoundEngine.PlaySound(SuddenMovementSound);
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.5f, 10);
                    StartShakeAtPoint(NPC.Center, 5f);
                }

                return;
            }

            PerpendicularPortalLaserbeams_ChargeCounter++;
            if (PerpendicularPortalLaserbeams_ChargeCounter < chargeCount)
            {
                AttackTimer = 0;
                NPC.netAlways = true;
            }
        }
    }
}
