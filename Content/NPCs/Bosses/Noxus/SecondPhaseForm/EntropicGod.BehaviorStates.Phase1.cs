using Microsoft.Xna.Framework;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.Particles.Metaballs;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm
{
    public partial class EntropicGod : ModNPC
    {
        public void DoBehavior_DarkExplosionCharges()
        {
            int chargeDelay = 40;
            int chargeTime = 41;
            int explosionCreationRate = 10;
            int chargeTeleportCount = 3;

            float initialChargeSpeed = 6f;
            float chargeAcceleration = 1.1f;
            float maxChargeSpeed = 62f;
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;
            ref Vector2 closestHandToTargetDestination = ref leftHandDestination;
            if (Target.Center.X >= NPC.Center.X)
                closestHandToTargetDestination = ref rightHandDestination;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                chargeDelay = 27;
                chargeTime = 26;
                explosionCreationRate = 7;
                maxChargeSpeed = 81f;
            }

            int wrappedAttackTimer = (int)AttackTimer % (DefaultTeleportDelay + chargeDelay + chargeTime);

            // Go to the next attack state once all charges have been performed.
            if (AttackTimer >= (DefaultTeleportDelay + chargeDelay + chargeTime) * chargeTeleportCount)
            {
                SelectNextAttack();
                return;
            }

            // Slow down in anticipation of the teleport. Teleport visual effects are executed later on.
            if (wrappedAttackTimer <= DefaultTeleportDelay)
            {
                NPC.velocity *= 0.8f;

                // Teleport near the target when ready.
                if (wrappedAttackTimer == DefaultTeleportDelay)
                {
                    Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 480f, -420f);
                    TeleportTo(hoverDestination);
                }
            }

            // Raise the hand closest to the player up in anticipation of the charge.
            else if (wrappedAttackTimer <= DefaultTeleportDelay + chargeDelay)
            {
                float anticipationInterpolant = InverseLerp(0f, chargeDelay, wrappedAttackTimer - DefaultTeleportDelay);

                closestHandToTargetDestination.X += Sign(NPC.Center.X - closestHandToTargetDestination.X) * Pow(anticipationInterpolant, 3.5f) * 180f;
                closestHandToTargetDestination.Y -= Pow(anticipationInterpolant, 2.6f) * 360f;

                // Cease all movement.
                NPC.velocity = Vector2.Zero;
            }

            // Do charge effects.
            else if (wrappedAttackTimer <= DefaultTeleportDelay + chargeDelay + chargeTime)
            {
                // Perform the charge.
                if (wrappedAttackTimer == DefaultTeleportDelay + chargeDelay + 1f)
                {
                    NPC.velocity = NPC.SafeDirectionTo(Target.Center) * initialChargeSpeed;
                    NPC.netUpdate = true;
                }

                // Accelerate.
                if (NPC.velocity.Length() < maxChargeSpeed)
                    NPC.velocity *= chargeAcceleration;

                // Arc a little bit at first.
                if (wrappedAttackTimer <= DefaultTeleportDelay + chargeDelay + 16f)
                {
                    float idealDirection = NPC.AngleTo(Target.Center);
                    float currentDirection = NPC.velocity.ToRotation();
                    NPC.velocity = NPC.velocity.RotatedBy(WrapAngle(idealDirection - currentDirection) * 0.12f);
                }

                // Make hands move towards the target, as if they're attempting to grab them.
                leftHandDestination = NPC.Center + (Target.Center - Hands[0].Center).SafeNormalize(Vector2.UnitY) * 300f;
                rightHandDestination = NPC.Center + (Target.Center - Hands[1].Center).SafeNormalize(Vector2.UnitY) * 300f;
                MakeHandsOpen();

                // Periodically create explosions.
                if (wrappedAttackTimer % explosionCreationRate == explosionCreationRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.Item72, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NoxusExplosion>(), ExplosionDamage, 0f);
                }

                ChargeAfterimageInterpolant = InverseLerp(0f, 8f, AttackTimer - DefaultTeleportDelay - chargeDelay);
            }

            // Prepare teleport scaling visual effects.
            TeleportVisualsInterpolant = InverseLerp(0f, DefaultTeleportDelay * 2f, wrappedAttackTimer);

            // Become invisible in accordance with how far along the teleport visuals are.
            NPC.Opacity = InverseLerp(0.34f, 0.03f, TeleportVisualsInterpolant, true) + InverseLerp(0.56f, 0.84f, TeleportVisualsInterpolant);

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination);
            DefaultHandDrift(Hands[1], rightHandDestination);
        }

        public void DoBehavior_DarkEnergyBoltHandWave()
        {
            int handArcTime = 50;
            int spikeReleaseRate = 2;
            int telegraphTime = NoxSpike.TelegraphTime;
            int attackTransitionDelay = 90;
            float handSpeedFactor = 1.8f;
            float maxArcAngle = ToRadians(160f);

            if (Main.expertMode)
            {
                spikeReleaseRate--;
                handArcTime -= 10;
                handSpeedFactor += 1.1f;
            }

            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Slow down and begin to teleport above the player. This also involves waiting after the teleport before attacking.
            if (AttackTimer <= DefaultTeleportDelay * 2f)
            {
                NPC.velocity *= 0.8f;

                // Raise hands as the teleport happens.
                float teleportWaitInterpolant = InverseLerp(0f, DefaultTeleportDelay - 9f, AttackTimer);
                if (AttackTimer >= DefaultTeleportDelay + 1f)
                    teleportWaitInterpolant = 0f;
                else
                    handSpeedFactor += 0.75f;

                float verticalOffset = Pow(teleportWaitInterpolant, 1.7f) * 400f;
                leftHandDestination.Y -= verticalOffset;
                rightHandDestination.Y -= verticalOffset;

                // Teleport near the target when ready.
                if (AttackTimer == DefaultTeleportDelay)
                {
                    Vector2 hoverDestination = Target.Center - Vector2.UnitY * 540f;
                    TeleportTo(hoverDestination);
                }
            }

            // Have the hand stay below Noxus before moving outward to create a slightly greater than 90 degree arc in both directions.
            // Spikes are created when this happens, but they don't initially move.
            else if (AttackTimer <= DefaultTeleportDelay * 2f + handArcTime)
            {
                float arcInterpolant = InverseLerp(11f, handArcTime - 8f, AttackTimer - DefaultTeleportDelay * 2f);
                leftHandDestination = CalculateHandOffsetForClapAnticipation(arcInterpolant, maxArcAngle, true);
                rightHandDestination = CalculateHandOffsetForClapAnticipation(arcInterpolant, maxArcAngle, false);
                MakeHandsOpen();

                // Release the spikes.
                // (((((OK yeah technically they move but it's only for rotation and they're so slow that it's negligible)))))
                if (AttackTimer % spikeReleaseRate == spikeReleaseRate - 1f && AttackTimer >= DefaultTeleportDelay * 2f + 16f)
                {
                    int telegraphDelay = (int)AttackTimer - (DefaultTeleportDelay * 2 + handArcTime);
                    NewProjectileBetter(NPC.GetSource_FromAI(), Hands[0].Center, (Hands[0].Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f, -1, 0f, telegraphDelay);
                    NewProjectileBetter(NPC.GetSource_FromAI(), Hands[1].Center, (Hands[1].Center - NPC.Center).SafeNormalize(Vector2.UnitY) * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f, -1, 0f, telegraphDelay);
                }
            }

            // Hold the hands in place after the spikes have been cast.
            else if (AttackTimer <= DefaultTeleportDelay * 2f + handArcTime + telegraphTime)
            {
                leftHandDestination = NPC.Center + Vector2.UnitY.RotatedBy(maxArcAngle) * 250f + Vector2.UnitX * -8f;
                rightHandDestination = NPC.Center + Vector2.UnitY.RotatedBy(-maxArcAngle) * 250f + Vector2.UnitX * 8f;
            }

            // Prepare teleport scaling visual effects.
            TeleportVisualsInterpolant = InverseLerp(0f, DefaultTeleportDelay * 2f, AttackTimer);

            // Clap hands and make all spikes fire in their projected paths.
            if (AttackTimer >= DefaultTeleportDelay * 2f + handArcTime + telegraphTime - 5f)
            {
                if (AttackTimer == DefaultTeleportDelay * 2f + handArcTime + telegraphTime)
                {
                    SoundEngine.PlaySound(ClapSound, Target.Center);
                    ScreenEffectSystem.SetBlurEffect((Hands[0].Center + Hands[1].Center) * 0.5f, 0.7f, 27);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.GetSource_FromAI(), (Hands[0].Center + Hands[1].Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }

                // Fly around above the player after the spikes have been fired.
                if (AttackTimer >= DefaultTeleportDelay * 2f + handArcTime + telegraphTime + 30f)
                    BasicFlyMovement(Target.Center - Vector2.UnitY * 333f);
                else
                {
                    handSpeedFactor *= 3f;
                    leftHandDestination = NPC.Center + new Vector2(8f, 150f);
                    rightHandDestination = NPC.Center + new Vector2(-8f, 150f);
                }
            }

            if (AttackTimer >= DefaultTeleportDelay * 2f + handArcTime + telegraphTime + attackTransitionDelay)
                SelectNextAttack();

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_FireballBarrage()
        {
            int teleportDelay = 15;
            int chargeDelay = 27;
            int chargeTime = 54;
            int chargeTeleportCount = 2;
            int wrappedAttackTimer = (int)AttackTimer % (teleportDelay + chargeDelay + chargeTime);
            int chargeCounter = (int)AttackTimer / (teleportDelay + chargeDelay + chargeTime);
            int fireballShootRate = 7;
            float initialChargeSpeed = 6.7f;
            float chargeAcceleration = 1.08f;
            float maxChargeSpeed = 54f;
            float handSpeedFactor = 1f;
            float fireballShootSpeed = 9f;

            if (Main.expertMode)
            {
                initialChargeSpeed += 1.2f;
                fireballShootSpeed += 2f;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
            {
                fireballShootRate--;
                chargeAcceleration += 0.032f;
            }

            bool teleportBelowTarget = chargeCounter % 2f == 0f;
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Go to the next attack state once all charges have been performed.
            if (AttackTimer >= (teleportDelay + chargeDelay + chargeTime) * chargeTeleportCount)
            {
                NPC.velocity *= 0.92f;
                if (AttackTimer >= (teleportDelay + chargeDelay + chargeTime) * chargeTeleportCount + 32f)
                    SelectNextAttack();
                return;
            }

            // Slow down in anticipation of the teleport. Teleport visual effects are executed later on.
            if (wrappedAttackTimer <= teleportDelay)
                NPC.velocity *= 0.8f;

            // Teleport to the side of the player and wait before charging.
            else if (wrappedAttackTimer <= teleportDelay + chargeDelay)
            {
                if (wrappedAttackTimer == teleportDelay + 1f)
                {
                    Vector2 teleportPosition = Target.Center + new Vector2(TargetDirection * -840f, teleportBelowTarget.ToDirectionInt() * 424f);
                    TeleportTo(teleportPosition);

                    SoundEngine.PlaySound(ExplosionTeleportSound with
                    {
                        Volume = 0.7f
                    }, Target.Center);
                }

                NPC.velocity = Vector2.Zero;
            }

            // Charge and release fireballs.
            else if (wrappedAttackTimer <= teleportDelay + chargeDelay + chargeTime)
            {
                // Charge horizontally.
                if (wrappedAttackTimer == teleportDelay + chargeDelay + 1f)
                {
                    NPC.velocity = Vector2.UnitX * Sign(NPC.SafeDirectionTo(Target.Center).X) * initialChargeSpeed;
                    NPC.netUpdate = true;
                }

                // Use afterimages.
                ChargeAfterimageInterpolant = InverseLerp(teleportDelay + chargeDelay, teleportDelay + chargeDelay + 7f, wrappedAttackTimer);

                // Accelerate.
                if (NPC.velocity.Length() < maxChargeSpeed)
                    NPC.velocity *= chargeAcceleration;

                // Release fireballs.
                if ((wrappedAttackTimer % fireballShootRate == 0f || Distance(NPC.Center.X, Target.Center.X) <= 120f))
                {
                    SoundEngine.PlaySound(SoundID.DD2_EtherianPortalSpawnEnemy with
                    {
                        Pitch = -Main.rand.NextFloat(0.2f, 0.5f),
                        Volume = 0.8f,
                        MaxInstances = 50
                    }, Target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 fireballShootVelocity = -Vector2.UnitY.RotatedByRandom(0.2f) * teleportBelowTarget.ToDirectionInt() * fireballShootSpeed;
                        NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center + fireballShootVelocity * 10f, fireballShootVelocity, ModContent.ProjectileType<DarkFireball>(), FireballDamage, 0f, -1, 0f, 60f);
                    }
                }
            }

            // Prepare teleport scaling visual effects.
            TeleportVisualsInterpolant = InverseLerp(0f, teleportDelay * 2f, wrappedAttackTimer);

            // Become invisible in accordance with how far along the teleport visuals are.
            NPC.Opacity = InverseLerp(0.34f, 0.03f, TeleportVisualsInterpolant, true) + InverseLerp(0.56f, 0.84f, TeleportVisualsInterpolant);

            // Make hands raise in the direction of the player as time goes on.
            float handRaiseInterpolant = InverseLerp(teleportDelay + 4f, teleportDelay + chargeDelay + 9f, wrappedAttackTimer);
            float verticalHandOffset = handRaiseInterpolant * teleportBelowTarget.ToDirectionInt() * 100f;
            leftHandDestination.Y -= verticalHandOffset;
            rightHandDestination.Y -= verticalHandOffset;
            handSpeedFactor += handRaiseInterpolant * 1.6f;

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_HoveringHandGasBursts()
        {
            int shootDelay = 60;
            int projectileShootRate = 28;
            int shootTime = 240;
            float handSpeedFactor = 1.33f;
            float hoverAcceleration = 0.75f;
            float hoverFlySpeed = 24f;
            Vector2 hoverOffset = new(-90f, -350f);
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                shootDelay = 40;
                projectileShootRate = 10;
                shootTime = 210;
            }

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            NPC.Center = Vector2.Lerp(NPC.Center, Target.Center + hoverOffset, 0.027f);
            if (!NPC.WithinRange(Target.Center + hoverOffset, 100f))
                NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center + hoverOffset) * hoverFlySpeed, hoverAcceleration);
            else
                NPC.velocity = (NPC.velocity * 1.04f).SafeNormalize(Vector2.UnitY) * Clamp(NPC.velocity.Length(), 7f, hoverFlySpeed);

            // Bring hands to the sides of the target.
            float handVerticalOffset = Sin(TwoPi * AttackTimer / 180f) * 400f + Target.Velocity.Y * 44f;
            float handHoverInterpolant = InverseLerp(0f, shootDelay, AttackTimer);
            leftHandDestination = Vector2.Lerp(leftHandDestination, Target.Center - new Vector2(780f, handVerticalOffset), handHoverInterpolant);
            rightHandDestination = Vector2.Lerp(rightHandDestination, Target.Center + new Vector2(780f, handVerticalOffset), handHoverInterpolant);
            MakeHandsOpen();

            // Make hands shoot gas bursts and comets.
            if (handHoverInterpolant >= 1f && AttackTimer % projectileShootRate == projectileShootRate - 1f && AttackTimer <= shootDelay + shootTime)
            {
                Vector2 leftCometShootVelocity = (Target.Center - Hands[0].Center).SafeNormalize(Vector2.UnitY) * 4f;
                Vector2 rightCometShootVelocity = (Target.Center - Hands[1].Center).SafeNormalize(Vector2.UnitY) * 4f;

                // Create gas particles.
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                for (int i = 0; i < 40; i++)
                    ModContent.GetInstance<NoxusGasMetaball>().CreateParticle(Hands[0].Center + leftCometShootVelocity.RotatedByRandom(0.98f) * Main.rand.NextFloat(4f), leftCometShootVelocity.RotatedByRandom(0.68f) * Main.rand.NextFloat(3f), Main.rand.NextFloat(13f, 56f));

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!Target.Center.WithinRange(Hands[0].Center, 330f))
                        NewProjectileBetter(NPC.GetSource_FromAI(), Hands[0].Center, leftCometShootVelocity, ModContent.ProjectileType<DarkComet>(), CometDamage, 0f);
                    if (!Target.Center.WithinRange(Hands[1].Center, 330f))
                        NewProjectileBetter(NPC.GetSource_FromAI(), Hands[1].Center, rightCometShootVelocity, ModContent.ProjectileType<DarkComet>(), CometDamage, 0f);
                }
            }

            if (AttackTimer >= shootDelay + shootTime + 60)
                SelectNextAttack();

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_RapidExplosiveTeleports()
        {
            int teleportCount = 7;
            int teleportRate = 23;
            int teleportCounter = (int)AttackTimer / teleportRate;
            int handRaiseTime = 67;
            int attackTransitionDelay = 180; // This is intentionally a bit long to give the player an opportunity to do melee hits.
            int fireballCount = 17;
            float handSpeedFactor = 2.1f;
            float maxArcAngle = ToRadians(165f);
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                teleportCount = 14;
                teleportRate = 5;
            }

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Fade in quickly after teleports.
            if (teleportCounter < teleportCount)
                NPC.Opacity = InverseLerp(0f, 5f, AttackTimer % teleportRate);
            else
                NPC.Opacity = Clamp(NPC.Opacity + 0.1f, 0f, 1f);

            // Teleport around.
            if (AttackTimer % teleportRate == teleportRate - 1f && teleportCounter < teleportCount)
            {
                Vector2 teleportDestination;
                do
                    teleportDestination = Target.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(356f, 400f);
                while (NPC.WithinRange(teleportDestination, 200f));

                // Teleport in front of the target if this is the final teleport.
                if (teleportCounter == teleportCount - 1)
                    teleportDestination = Target.Center + Target.Velocity * new Vector2(40f, 30f) + Main.rand.NextVector2CircularEdge(200f, 200f);

                // Release an explosion at the old position prior to the teleport.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<NoxusExplosion>(), ExplosionDamage, 0f);

                // Make things blurrier for a short time.
                if (NPC.WithinRange(Target.Center, 400f))
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.28f, 12);

                TeleportToWithDecal(teleportDestination);
            }

            // Raise hands after the final teleport.
            float handRaiseInterpolant = InverseLerp(teleportCount * teleportRate, teleportCount * teleportRate + handRaiseTime, AttackTimer);

            // Once the hands are done raising, make them clap.
            if (handRaiseInterpolant >= 1f)
            {
                maxArcAngle = ToRadians(3f);
                handSpeedFactor += 3f;
            }

            if (handRaiseInterpolant > 0f)
            {
                leftHandDestination = CalculateHandOffsetForClapAnticipation(handRaiseInterpolant, maxArcAngle, true);
                rightHandDestination = CalculateHandOffsetForClapAnticipation(handRaiseInterpolant, maxArcAngle, false);
            }

            // Release the barrage of explosive fireballs once the clap is done.
            if (AttackTimer == teleportCount * teleportRate + handRaiseTime + 6f)
            {
                SoundEngine.PlaySound(ClapSound, Target.Center);
                ScreenEffectSystem.SetBlurEffect((Hands[0].Center + Hands[1].Center) * 0.5f, 0.7f, 27);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.GetSource_FromAI(), (Hands[0].Center + Hands[1].Center) * 0.5f, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);

                    Vector2 fireballSpawnPosition = (Hands[0].Center + Hands[1].Center) * 0.5f;
                    for (int i = 0; i < fireballCount; i++)
                    {
                        Vector2 fireballVelocity = (Target.Center - fireballSpawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(1.34f) * Main.rand.NextFloat(9.5f, 24f);
                        NewProjectileBetter(NPC.GetSource_FromAI(), fireballSpawnPosition, fireballVelocity, ModContent.ProjectileType<DarkFireball>(), FireballDamage, 0f, -1, 0f, Main.rand.NextFloat(18f, 36f));
                    }
                }
            }

            if (AttackTimer >= teleportCount * teleportRate + handRaiseTime + attackTransitionDelay)
                SelectNextAttack();

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }

        public void DoBehavior_TeleportAndShootNoxusGas()
        {
            int wrappedAttackTimer = (int)AttackTimer % (DefaultTeleportDelay * 2);
            int teleportCounter = (int)AttackTimer / (DefaultTeleportDelay * 2);
            int gasShootCount = 3;
            float handSpeedFactor = 2.52f;
            float gasShootMaxAngle = ToRadians(37f);
            float gasShootSpeed = 10.4f;
            Vector2 leftHandDestination = NPC.Center + Hands[0].DefaultOffset * NPC.scale;
            Vector2 rightHandDestination = NPC.Center + Hands[1].DefaultOffset * NPC.scale;
            Vector2 closestHandPosition = Hands[0].Center;
            ref Vector2 closestHandToTargetDestination = ref leftHandDestination;
            if (Target.Center.X >= NPC.Center.X)
            {
                closestHandToTargetDestination = ref rightHandDestination;
                closestHandPosition = Hands[1].Center;
            }

            // Kill the player in GFB.
            if (Main.zenithWorld)
            {
                gasShootCount = 7;
                gasShootSpeed = 19f;
            }

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Handle teleport visual effects.
            TeleportVisualsInterpolant = InverseLerp(0f, DefaultTeleportDelay * 2f, wrappedAttackTimer);
            NPC.Opacity = InverseLerp(0.4f, 0.1f, TeleportVisualsInterpolant, true) + InverseLerp(0.6f, 0.85f, TeleportVisualsInterpolant);

            // Perform the teleport.
            if (wrappedAttackTimer == DefaultTeleportDelay)
            {
                float teleportOffsetAngle = TwoPi * (teleportCounter % 4f) / 4f + PiOver4;
                Vector2 teleportOffset = teleportOffsetAngle.ToRotationVector2() * new Vector2(1050f, 360f);
                Vector2 teleportDestination = Target.Center + teleportOffset;

                // Move a bit further away if the target is moving in the direction of the teleport offset.
                if (Vector2.Dot(Target.Velocity, teleportOffset) > 0f)
                    teleportDestination += Target.Velocity * 28f;

                if (teleportCounter >= 4)
                {
                    teleportDestination = Target.Center - Vector2.UnitY * 340f;
                    SelectNextAttack();
                }

                TeleportTo(teleportDestination);
            }

            // Make the hand that's closest to the target face towards them before firing the gas.
            float aimHandAtTargetInterpolant = InverseLerp(DefaultTeleportDelay, DefaultTeleportDelay + 10f, AttackTimer);
            closestHandToTargetDestination = Vector2.Lerp(closestHandToTargetDestination, NPC.Center + NPC.SafeDirectionTo(Target.Center) * 157f, aimHandAtTargetInterpolant);

            // Release Noxus gas in a spread.
            if (wrappedAttackTimer == DefaultTeleportDelay * 2f - 12f)
            {
                SoundEngine.PlaySound(FireballShootSound, Target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < gasShootCount; i++)
                    {
                        float gasShootArc = Lerp(-gasShootMaxAngle, gasShootMaxAngle, i / (float)(gasShootCount - 1f)) + Main.rand.NextFloatDirection() * 0.048f;
                        Vector2 gasShootVelocity = (Target.Center - closestHandPosition).SafeNormalize(Vector2.UnitY).RotatedBy(gasShootArc) * gasShootSpeed;
                        NewProjectileBetter(NPC.GetSource_FromAI(), closestHandPosition, gasShootVelocity, ModContent.ProjectileType<NoxusGas>(), NoxusGasDamage, 0f);
                    }
                }
            }

            // Move hands.
            DefaultHandDrift(Hands[0], leftHandDestination, handSpeedFactor);
            DefaultHandDrift(Hands[1], rightHandDestination, handSpeedFactor);
        }
    }
}
