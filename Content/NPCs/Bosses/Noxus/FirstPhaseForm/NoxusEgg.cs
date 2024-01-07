using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.Metaballs;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm.EntropicGod;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm
{
    [AutoloadBossHead]
    public class NoxusEgg : ModNPC
    {
        #region Custom Types and Enumerations
        public enum NoxusEggAttackType
        {
            Awaken,
            ChargeWithComets,
            DistortingTeleports,
            PortalGasBursts,
            DownwardTeleportingSlams,
            DeathAnimation
        }

        #endregion Custom Types and Enumerations

        #region Fields and Properties

        private static Asset<Texture2D> backTexture;

        private static Asset<Texture2D> shellPiece1Texture;

        private static Asset<Texture2D> shellPiece2Texture;

        private static Asset<Texture2D> shellPiece3Texture;

        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public float LifeRatio => NPC.life / (float)NPC.lifeMax;

        public Player Target => Main.player[NPC.target];

        public NoxusEggAttackType CurrentAttack
        {
            get => (NoxusEggAttackType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public static int DistortionFieldDamage => Main.expertMode ? 400 : 250;

        public static readonly SoundStyle HitSound = new SoundStyle("NoxusBoss/Assets/Sounds/NPCHit/NoxusEggHurt") with { PitchVariance = 0.4f, Volume = 0.5f };

        public static readonly SoundStyle OminousSound = new("NoxusBoss/Assets/Sounds/Custom/Noxus/OminousIntroduction");

        public const float DefaultDR = 0.7f;

        public const float TerminationLifeRatio = 0.8274f;
        #endregion Fields and Properties

        #region Initialization
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailingMode[NPC.type] = 3;
            NPCID.Sets.TrailCacheLength[NPC.type] = 90;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Scale = 0.3f,
                PortraitScale = 0.5f
            };
            NPCID.Sets.NPCBestiaryDrawOffset[Type] = value;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.MPAllowedEnemies[Type] = true;

            // Apply miracleblight immunities.
            NPC.MakeImmuneToMiracleblight();

            On_NPC.DoDeathEvents_DropBossPotionsAndHearts += DisableNoxusEggBossDeathEffects;

            // Load textures if not serverside.
            if (Main.netMode != NetmodeID.Server)
            {
                MyTexture = ModContent.Request<Texture2D>(Texture);
                backTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/FirstPhaseForm/NoxusBack");
                shellPiece1Texture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/FirstPhaseForm/NoxusEggShell1");
                shellPiece2Texture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/FirstPhaseForm/NoxusEggShell2");
                shellPiece3Texture = ModContent.Request<Texture2D>("NoxusBoss/Content/NPCs/Bosses/Noxus/FirstPhaseForm/NoxusEggShell3");
            }
        }

        private void DisableNoxusEggBossDeathEffects(On_NPC.orig_DoDeathEvents_DropBossPotionsAndHearts orig, NPC self, ref string typeName)
        {
            if (self.type != Type)
                orig(self, ref typeName);
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 50f;
            NPC.damage = 376;
            NPC.width = 224;
            NPC.height = 224;
            NPC.defense = 130;
            NPC.SetLifeMaxByMode(2641950, 3244495, 4635000);

            if (Main.expertMode)
            {
                NPC.damage = 575;

                // Fuck arbitrary Expert boosts.
                NPC.lifeMax /= 2;
                NPC.damage /= 2;
            }

            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.value = 0;
            NPC.netAlways = true;
            NPC.hide = true;
            NPC.MakeCalamityBossBarClose();

            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/GodIsOnHisKnees");
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                new FlavorTextBestiaryInfoElement($"Mods.{Mod.Name}.Bestiary.{Name}"),
                new MoonLordPortraitBackgroundProviderBestiaryInfoElement()
            });
        }
        #endregion Initialization

        #region AI
        public override void AI()
        {
            // Pick a target if the current one is invalid.
            bool invalidTargetIndex = NPC.target is < 0 or >= 255;
            if (invalidTargetIndex)
                NPC.TargetClosest();

            bool invalidTarget = Target.dead || !Target.active;
            if (invalidTarget)
                NPC.TargetClosest();

            if (!NPC.WithinRange(Target.Center, 4600f - Target.aggro))
                NPC.TargetClosest();

            // Hey bozo the player's gone. Leave.
            if (Target.dead || !Target.active)
                NPC.active = false;

            // Grant the target infinite flight.
            Target.wingTime = Target.wingTimeMax;
            Target.GrantInfiniteFlight();

            // Disable rain.
            Main.StopRain();

            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.defense = NPC.defDefense;
            NPC.dontTakeDamage = false;
            NPC.ShowNameOnHover = true;
            NPC.SetDR(DefaultDR);

            // Ensure that the player receives the boss effects buff.
            Target.GrantBossEffectsBuff();

            // Do not despawn.
            NPC.timeLeft = 7200;

            // Begin the death animation if ready.
            if (LifeRatio < TerminationLifeRatio && NPC.ai[2] == 0f)
                TriggerDeathAnimation();

            Music = MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/GodIsOnHisKnees");
            switch (CurrentAttack)
            {
                case NoxusEggAttackType.Awaken:
                    DoBehavior_Awaken();
                    break;
                case NoxusEggAttackType.ChargeWithComets:
                    DoBehavior_ChargeWithComets();
                    break;
                case NoxusEggAttackType.DistortingTeleports:
                    DoBehavior_DistortingTeleports();
                    break;
                case NoxusEggAttackType.PortalGasBursts:
                    DoBehavior_PortalGasBursts();
                    break;
                case NoxusEggAttackType.DownwardTeleportingSlams:
                    DoBehavior_DownwardTeleportingSlams();
                    break;
                case NoxusEggAttackType.DeathAnimation:
                    DoBehavior_DeathAnimation();
                    break;
            }

            // Disable damage when invisible.
            if (NPC.Opacity <= 0.35f)
            {
                NPC.ShowNameOnHover = false;
                NPC.dontTakeDamage = true;
                NPC.damage = 0;
            }

            AttackTimer++;

            // Rotate based on horizontal speed.
            NPC.rotation = NPC.velocity.X * 0.004f;
        }

        public void DoBehavior_Awaken()
        {
            int screenRumbleTime = 320;
            int roarTime = 90;

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            // Play an ominous sound at first.
            if (AttackTimer == 1f)
                SoundEngine.PlaySound(OminousSound);

            // Start out invisible.
            if (AttackTimer < screenRumbleTime)
            {
                NPC.Opacity = 0f;
                NPC.Center = Target.Center - Vector2.UnitY * 350f;
            }

            // Create an explosion sound and appear.
            if (AttackTimer == screenRumbleTime)
            {
                SoundEngine.PlaySound(ExplosionTeleportSound);
                TeleportTo(NPC.Center + Vector2.UnitY * 50f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
            }

            // Roar after appearing.
            if (AttackTimer >= screenRumbleTime)
            {
                NPC.Opacity = 1f;

                if (AttackTimer % 15f == 1f && AttackTimer <= screenRumbleTime + roarTime - 45f)
                {
                    SoundEngine.PlaySound(ScreamSound with { Volume = 1.1f, Pitch = -0.25f });
                    Color burstColor = Main.rand.NextBool() ? Color.SlateBlue : Color.Lerp(Color.White, Color.MediumPurple, 0.7f);

                    // Create blur and burst particle effects.
                    ExpandingChromaticBurstParticle burst = new(NPC.Center, Vector2.Zero, burstColor, 16, 0.1f);
                    burst.Spawn();
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.7f, 24);

                    if (OverallShakeIntensity <= 1f)
                        StartShake(10f);
                }
            }
            else
                Music = 0;

            // Accelerate upward right before teleporting away.
            if (AttackTimer >= screenRumbleTime + roarTime - 24f)
                NPC.velocity = Vector2.Lerp(NPC.velocity, -Vector2.UnitY * 20f, 0.1f);

            if (AttackTimer >= screenRumbleTime + roarTime)
                SelectNextAttack();
        }

        public void DoBehavior_ChargeWithComets()
        {
            int chargeDelay = 42;
            int chargeTime = 48;
            int chargeCount = 2;
            int cometReleaseRate = 11;
            float initialChargeSpeed = 6f;
            float maxChargeSpeed = 66f;
            float cometShootSpeed = 9f;
            float chargeAcceleration = 1.09f;

            if (Main.expertMode)
            {
                chargeDelay -= 9;
                maxChargeSpeed += 8f;
            }

            float wrappedAttackTimer = AttackTimer % (chargeDelay + chargeTime);

            // Teleport to the opposite side of the player on the first frame.
            if (wrappedAttackTimer == 1f)
                TeleportTo(Target.Center + Vector2.UnitX * Target.direction * -670f);

            // Perform chargeup effects before charging.
            if (wrappedAttackTimer < chargeDelay - 16f && wrappedAttackTimer % 10f == 5f)
            {
                Color energyColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
                PulseRing ring = new(NPC.Center, Vector2.Zero, energyColor, 3.6f, 0f, 30);
                ring.Spawn();

                StrongBloom bloom = new(NPC.Center, Vector2.Zero, energyColor, 1f, 12);
                bloom.Spawn();
            }

            // Charge at the player.
            if (wrappedAttackTimer == chargeDelay)
            {
                SoundEngine.PlaySound(ExplosionTeleportSound, NPC.Center);
                NPC.velocity = NPC.DirectionToSafe(Target.Center) * initialChargeSpeed;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
            }

            // Accelerate.
            if (NPC.velocity != Vector2.Zero)
                NPC.velocity = (NPC.velocity * chargeAcceleration).ClampLength(0f, maxChargeSpeed);

            // Release comets while charging.
            if (wrappedAttackTimer >= chargeDelay)
            {
                if (wrappedAttackTimer % cometReleaseRate == cometReleaseRate - 1f && !NPC.WithinRange(Target.Center, 250f))
                {
                    SoundEngine.PlaySound(FireballShootSound, NPC.Center);

                    // Create gas particles.
                    Vector2 cometShootVelocity = NPC.DirectionToSafe(Target.Center) * cometShootSpeed;
                    SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                    for (int i = 0; i < 40; i++)
                        NoxusGasMetaball.CreateParticle(NPC.Center + cometShootVelocity.RotatedByRandom(0.98f) * Main.rand.NextFloat(1.3f), cometShootVelocity.RotatedByRandom(0.68f) * Main.rand.NextFloat(1.1f), Main.rand.NextFloat(13f, 56f));

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NewProjectileBetter(NPC.Center, cometShootVelocity, ModContent.ProjectileType<DarkComet>(), CometDamage, 0f);
                }
            }

            if (AttackTimer >= (chargeDelay + chargeTime) * chargeCount)
            {
                ClearAllProjectiles();
                SelectNextAttack();
            }
        }

        public void DoBehavior_DistortingTeleports()
        {
            int teleportCount = 8;
            int teleportRate = 16;
            int cometCount = 8;
            float cometShootSpeed = 6f;

            if (Main.expertMode)
                teleportRate -= 3;
            if (CommonCalamityVariables.RevengeanceModeActive)
                cometShootSpeed += 0.4f;

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Perform teleports.
            if (AttackTimer % teleportRate == 1f && AttackTimer < teleportRate * teleportCount)
            {
                SoundEngine.PlaySound(FireballShootSound, NPC.Center);

                Vector2 teleportOffsetDirection = Target.velocity.SafeNormalize(Main.rand.NextVector2Unit());
                if (Target.velocity.Length() < 4f)
                    teleportOffsetDirection *= 0.64f;
                if (Target.velocity.Length() >= 15f)
                    teleportOffsetDirection *= 1.5f;

                TeleportTo(Target.Center + teleportOffsetDirection * 770f);

                Color energyColor = Color.Lerp(Color.MediumPurple, Color.DarkBlue, Main.rand.NextFloat(0.5f));
                PulseRing ring = new(NPC.Center, Vector2.Zero, energyColor, 0f, 4f, 30);
                ring.Spawn();

                StrongBloom bloom = new(NPC.Center, Vector2.Zero, energyColor, 1f, 12);
                bloom.Spawn();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DistortionField>(), DistortionFieldDamage, 0f);
            }

            // Teleport behind the player and release comets at the end.
            if (AttackTimer == teleportRate * (teleportCount + 1f))
            {
                Vector2 teleportOffsetDirection = Target.velocity.SafeNormalize(Main.rand.NextVector2Unit());

                // Make the offset taper off if the target is moving slowly.
                teleportOffsetDirection *= Remap(NPC.Distance(Target.Center), 3f, 6f, 0.4f, 1f);

                TeleportTo(Target.Center - teleportOffsetDirection * 600f);

                SoundEngine.PlaySound(FireballShootSound, NPC.Center);
                SoundEngine.PlaySound(ExplosionTeleportSound, NPC.Center);

                // Create gas particles.
                Vector2 cometShootVelocity = NPC.DirectionToSafe(Target.Center) * cometShootSpeed;
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                for (int i = 0; i < 40; i++)
                    NoxusGasMetaball.CreateParticle(NPC.Center + cometShootVelocity.RotatedByRandom(0.98f) * Main.rand.NextFloat(1.3f), cometShootVelocity.RotatedByRandom(0.68f) * Main.rand.NextFloat(1.1f), Main.rand.NextFloat(13f, 56f));

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                    for (int i = 0; i < cometCount; i++)
                        NewProjectileBetter(NPC.Center, cometShootVelocity.RotatedByRandom(1.22f) + Main.rand.NextVector2Circular(3.2f, 3.2f), ModContent.ProjectileType<DarkComet>(), CometDamage, 0f);
                }
            }

            if (AttackTimer >= teleportRate * (teleportCount + 1f) + 60f)
                SelectNextAttack();
        }

        public void DoBehavior_PortalGasBursts()
        {
            int portalSummonRate = 15;
            int portalSummonTime = 210;
            int portalLingerTime = 76;
            float portalScale = 0.6f;

            if (Main.expertMode)
            {
                portalSummonRate -= 2;
                portalSummonTime += 30;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
                portalSummonTime += 30;

            // Disable contact damage. It is not relevant for this attack.
            NPC.damage = 0;

            // Fly up and down.
            NPC.velocity = Vector2.UnitY * Sin(TwoPi * AttackTimer / 80f) * 3.6f;

            // Teleport behind the player on the first frame.
            if (AttackTimer == 1f)
                TeleportTo(Target.Center - Vector2.UnitX * Target.direction * 275f);

            // Summon portals around the target.
            if (AttackTimer % portalSummonRate == portalSummonRate - 1f && AttackTimer < portalSummonTime)
            {
                SoundEngine.PlaySound(FireballShootSound, Target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angularOffset = 0f;
                    if (Main.rand.NextBool(3, 10))
                        angularOffset = Main.rand.NextFloatDirection() * 0.55f;

                    Vector2 portalSpawnPosition = Target.Center + Target.velocity.SafeNormalize(Main.rand.NextVector2Unit()).RotatedBy(angularOffset) * Main.rand.NextFloat(720f, 750f);
                    NewProjectileBetter(portalSpawnPosition, (Target.Center - portalSpawnPosition).SafeNormalize(Vector2.UnitY), ModContent.ProjectileType<DarkPortal>(), 0, 0f, -1, portalScale, portalLingerTime);
                }
            }

            if (AttackTimer >= portalSummonTime + 90f)
                SelectNextAttack();
        }

        public void DoBehavior_DownwardTeleportingSlams()
        {
            int slamCount = 2;
            int hoverTime = 33;
            int slamTime = 90;
            int sitInPlaceTime = 18;
            float minSpikeGap = 95f;
            ref float slamCounter = ref NPC.ai[3];

            if (Main.expertMode)
            {
                if (slamCounter >= 1f)
                    hoverTime -= 3;
                sitInPlaceTime -= 3;
            }
            if (CommonCalamityVariables.RevengeanceModeActive)
            {
                slamCount++;

                if (slamCounter >= 1f)
                    hoverTime -= 3;
                sitInPlaceTime -= 4;
                minSpikeGap -= 12f;
            }

            int slamDelay = hoverTime + sitInPlaceTime;
            float wrappedAttackTimer = AttackTimer % (slamDelay + slamTime);
            float startingSlamSpeed = 20f;
            float maxSlamSpeed = 100f;
            float slamAcceleration = 1.14f;

            // Teleport above the player on the first frame.
            if (wrappedAttackTimer == 1f)
                TeleportTo(Target.Center - Vector2.UnitY * 195f);

            // Stay above the player before slamming down.
            if (wrappedAttackTimer <= hoverTime && wrappedAttackTimer >= 2f)
            {
                float hoverInterpolant = Pow(wrappedAttackTimer / hoverTime, 0.74f);
                Vector2 start = Target.Center - Vector2.UnitY * 195f;
                Vector2 end = Target.Center - Vector2.UnitY * 360f;
                NPC.Center = Vector2.Lerp(start, end, hoverInterpolant);
                NPC.velocity = Vector2.Zero;
            }

            // Slam downward and release telegraphed spikes from the sides.
            if (wrappedAttackTimer == slamDelay)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float dx = 200f; dx < 1700f; dx += minSpikeGap * Main.rand.NextFloat(1f, 1.263f))
                    {
                        Vector2 spikeSpawnOffset = new(dx, -700f);
                        NewProjectileBetter(NPC.Center + spikeSpawnOffset, Vector2.UnitY.RotatedByRandom(0.018f) * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);

                        spikeSpawnOffset = new(-dx, -700f);
                        NewProjectileBetter(NPC.Center + spikeSpawnOffset, Vector2.UnitY.RotatedByRandom(0.018f) * 0.0001f, ModContent.ProjectileType<NoxSpike>(), SpikeDamage, 0f);
                    }
                }

                SoundEngine.PlaySound(ExplosionTeleportSound, NPC.Center);
                NPC.velocity = Vector2.UnitY * startingSlamSpeed;
                NPC.netUpdate = true;
            }

            // Collide with the ground.
            if (wrappedAttackTimer >= slamDelay && NPC.Bottom.Y >= Target.Bottom.Y + 8f && TileCollision(NPC.BottomLeft - Vector2.UnitY * 108f, NPC.width, 108f, out _) && NPC.velocity.Y != 0f)
            {
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.5f, 10);
                StartShakeAtPoint(NPC.Center, 15f, TwoPi / 9f, Vector2.UnitY);

                NPC.velocity = Vector2.Zero;
                AttackTimer += slamDelay + slamTime - wrappedAttackTimer - 32f;
                NPC.netUpdate = true;

                // Create ground collision effects.
                for (int i = 0; i < NPC.width; i += Main.rand.Next(2, 6))
                {
                    Point p = new((int)(NPC.BottomLeft.X + i) / 16, (int)(NPC.BottomLeft.Y / 16f) - 1);
                    Tile t = ParanoidTileRetrieval(p.X, p.Y);

                    // Create tile impact dust particles.
                    if (t.HasUnactuatedTile)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            Dust d = Main.dust[WorldGen.KillTile_MakeTileDust(p.X, p.Y, t)];
                            d.scale = Main.rand.NextFloat(1f, 1.6f);
                            d.velocity = -Vector2.UnitY.RotatedByRandom(0.7f) * d.scale * Main.rand.NextFloat(1f, 10f);
                            d.noGravity = d.velocity.Length() >= 9f;
                            d.active = true;
                        }
                    }
                }

                // Create a shock effect over tiles.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Bottom - Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<GroundStompShock>(), 0, 0f);
            }

            // Accelerate after slamming.
            if (wrappedAttackTimer >= slamDelay && NPC.velocity.Length() < maxSlamSpeed)
                NPC.velocity *= slamAcceleration;

            if (wrappedAttackTimer == slamDelay + slamTime - 1f)
                slamCounter++;

            if (AttackTimer >= (slamDelay + slamTime) * slamCount)
            {
                slamCounter = 0f;
                SelectNextAttack();
            }
        }

        public void DoBehavior_DeathAnimation()
        {
            int twinkleDelay = 160;
            int torrentDelay = twinkleDelay + 36;
            int animationTime = 450;

            // Disable damage. It is not relevant for this behavior.
            NPC.damage = 0;
            NPC.dontTakeDamage = true;

            // Close the HP bar.
            NPC.MakeCalamityBossBarClose();

            // Teleport above the player and clear projectiles on the first frame.
            if (AttackTimer == 1f)
            {
                ClearAllProjectiles();
                TeleportTo(Target.Center - Vector2.UnitY * 360f);
                SoundEngine.PlaySound(GlitchSound);
                NoxusSky.SkyIntensityOverride = 1f;
            }

            // Turn off the music.
            Music = 0;

            // Randomly make the sky glitch.
            int glitchChance = (int)Lerp(66f, 36f, Pow(AttackTimer / animationTime, 1.5f));
            if (AttackTimer >= 90f && Main.rand.NextBool(glitchChance) && NoxusSky.SkyIntensityOverride <= 0f && AttackTimer < torrentDelay - 30f)
            {
                SoundEngine.PlaySound(GlitchSound);
                NoxusSky.SkyIntensityOverride = 1f;
            }

            // Create a twinkle on top of the center.
            if (AttackTimer == twinkleDelay)
            {
                SoundEngine.PlaySound(TwinkleSound, NPC.Center);
                TwinkleParticle twinkle = new(NPC.Center, Vector2.Zero, Color.Pink, 35, 6, new Vector2(1.3f, 2f));
                twinkle.Spawn();
            }

            // Release torrents of dark energy and have the background fully transition to blank.
            if (AttackTimer >= torrentDelay)
            {
                NoxusSky.SkyIntensityOverride = InverseLerp(torrentDelay - 1f, animationTime - 20f, AttackTimer);
                NPC.Opacity = InverseLerp(animationTime + 160f, torrentDelay - 1f, AttackTimer);

                if (AttackTimer % 10f == 1f && NPC.Opacity >= 0.6f)
                {
                    SoundEngine.PlaySound(ScreamSound with { Volume = 1.1f, Pitch = -0.25f });
                    Color burstColor = Main.rand.NextBool() ? Color.SlateBlue : Color.Lerp(Color.White, Color.MediumPurple, 0.7f);

                    // Create blur and burst particle effects.
                    ExpandingChromaticBurstParticle burst = new(NPC.Center, Vector2.Zero, burstColor, 16, 0.1f);
                    burst.Spawn();
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.3f, 24);
                    ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 1f, 10);

                    if (OverallShakeIntensity <= 1f)
                        StartShakeAtPoint(NPC.Center, 10f);
                }

                // Release the dark energy comets.
                for (int i = 0; i < 3; i++)
                    NoxusGasMetaball.CreateParticle(NPC.Center, Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(13f, 56f) + NoxusSky.SkyIntensityOverride * 50f);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(NPC.Center, Main.rand.NextVector2Circular(40f, 40f), ModContent.ProjectileType<DarkComet>(), 0, 0f);
            }

            // Enter the second phase by transforming into the Entropic God.
            if (AttackTimer >= animationTime)
            {
                // Create an explosion sound.
                SoundEngine.PlaySound(ExplosionTeleportSound with { Volume = 3f });

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Register the first form as defeated in the bestiary.
                    Main.BestiaryTracker.Kills.RegisterKill(NPC);

                    // Spawn the Entropic God.
                    NPC.NewNPC(NPC.GetSource_Death(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<EntropicGod>(), 1);

                    // Create a wave effect.
                    NewProjectileBetter(NPC.Center, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }

                // Shake the screen.
                StartShakeAtPoint(NPC.Center, 16f);

                NPC.life = 0;
                NPC.checkDead();

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    OnKill();
                NPC.active = false;
            }
        }

        public void SelectNextAttack()
        {
            switch (CurrentAttack)
            {
                case NoxusEggAttackType.Awaken:
                    CurrentAttack = NoxusEggAttackType.ChargeWithComets;
                    break;
                case NoxusEggAttackType.ChargeWithComets:
                    CurrentAttack = NoxusEggAttackType.DistortingTeleports;
                    break;
                case NoxusEggAttackType.DistortingTeleports:
                    CurrentAttack = NoxusEggAttackType.PortalGasBursts;
                    break;
                case NoxusEggAttackType.PortalGasBursts:
                    CurrentAttack = NoxusEggAttackType.DownwardTeleportingSlams;
                    break;
                case NoxusEggAttackType.DownwardTeleportingSlams:
                    CurrentAttack = NoxusEggAttackType.ChargeWithComets;
                    break;
            }

            AttackTimer = 0f;
            NPC.netUpdate = true;
        }

        public void TriggerDeathAnimation()
        {
            SelectNextAttack();
            ClearAllProjectiles();
            NPC.ai[2] = 1f;
            NPC.dontTakeDamage = true;
            CurrentAttack = NoxusEggAttackType.DeathAnimation;
            NPC.netUpdate = true;
        }

        public void TeleportTo(Vector2 teleportPosition)
        {
            // Leave behind a decal afterimage at the old position.
            NoxusEggDecalParticle decal = new(NPC.Center, NPC.rotation, Color.MediumPurple * 0.5f, 24, NPC.scale);
            decal.Spawn();

            NPC.Center = teleportPosition;
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;

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

        #endregion AI

        #region Drawing

        public override void DrawBehind(int index)
        {
            if (NPC.hide && NPC.Opacity >= 0.02f)
                Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void BossHeadSlot(ref int index)
        {
            // Make the head icon disappear if Noxus is invisible.
            if (NPC.Opacity <= 0.45f)
                index = -1;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw the Noxus egg manually during the death animation.
            // This is done so that he can be split into individual pieces that split as the animation goes on.
            if (CurrentAttack == NoxusEggAttackType.DeathAnimation)
            {
                DrawSplitEggShell(screenPos);
                return false;
            }

            // Get textures from asset wrappers.
            Texture2D texture = MyTexture.Value;

            // Draw afterimages.
            float universalOpacity = 0.5f;
            float minClosenessInterpolant = 0.4f;
            for (int i = 20; i >= 0; i--)
            {
                float afterimageOpacity = Pow(1f - i / 21f, 5.9f) * universalOpacity;
                Vector2 afterimageDrawPosition = Vector2.Lerp(NPC.oldPos[i] + NPC.Size * 0.5f, NPC.Center, minClosenessInterpolant) - screenPos;
                Color afterimageColor = new Color(209, 155, 218, 0) * afterimageOpacity;
                Main.spriteBatch.Draw(texture, afterimageDrawPosition, NPC.frame, NPC.GetAlpha(afterimageColor), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, 0, 0f);
            }

            // Draw the Noxus egg.
            Main.spriteBatch.Draw(texture, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, 0, 0f);
            return false;
        }

        public void DrawSplitEggShell(Vector2 screenPos)
        {
            // Get textures from asset wrappers.
            Texture2D back = backTexture.Value;
            Texture2D shellPiece1 = shellPiece1Texture.Value;
            Texture2D shellPiece2 = shellPiece2Texture.Value;
            Texture2D shellPiece3 = shellPiece3Texture.Value;

            Vector2 shell1DrawPosition = NPC.Center - screenPos + new Vector2(-84f, 0f).RotatedBy(NPC.rotation) * NPC.scale;
            Vector2 shell2DrawPosition = NPC.Center - screenPos + new Vector2(40f, 66f).RotatedBy(NPC.rotation) * NPC.scale;
            Vector2 shell3DrawPosition = NPC.Center - screenPos + new Vector2(40f, -50f).RotatedBy(NPC.rotation) * NPC.scale;

            float shellBlastOffInterpolant = InverseLerp(444f, 450f, AttackTimer) * 25f;
            float shell1OpenInterpolant = InverseLerp(60f, 80f, AttackTimer) + shellBlastOffInterpolant;
            float shell2OpenInterpolant = InverseLerp(100f, 120f, AttackTimer) + shellBlastOffInterpolant;
            float shell3OpenInterpolant = InverseLerp(140f, 160f, AttackTimer) + shellBlastOffInterpolant;
            shell1DrawPosition += new Vector2(2f, -8f).RotatedBy(NPC.rotation) * shell1OpenInterpolant * NPC.scale;
            shell2DrawPosition += new Vector2(4f, 8f).RotatedBy(NPC.rotation) * shell2OpenInterpolant * NPC.scale;
            shell3DrawPosition += new Vector2(3f, -19f).RotatedBy(NPC.rotation) * shell3OpenInterpolant * NPC.scale;

            Main.spriteBatch.Draw(back, NPC.Center - screenPos, null, NPC.GetAlpha(Color.White), NPC.rotation, backTexture.Size() * 0.5f, NPC.scale * new Vector2(1.5f, 1.7f), 0, 0f);
            Main.spriteBatch.Draw(shellPiece1, shell1DrawPosition, null, NPC.GetAlpha(Color.White), NPC.rotation, shellPiece1.Size() * 0.5f, NPC.scale, 0, 0f);
            Main.spriteBatch.Draw(shellPiece2, shell2DrawPosition, null, NPC.GetAlpha(Color.White), NPC.rotation, shellPiece2.Size() * 0.5f, NPC.scale, 0, 0f);
            Main.spriteBatch.Draw(shellPiece3, shell3DrawPosition, null, NPC.GetAlpha(Color.White), NPC.rotation, shellPiece3.Size() * 0.5f, NPC.scale, 0, 0f);
        }
        #endregion Drawing

        #region Hit Effects and Loot

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.soundDelay >= 1)
                return;

            NPC.soundDelay = 9;
            SoundEngine.PlaySound(HitSound, NPC.Center);
        }

        public override bool CheckDead()
        {
            AttackTimer = 0f;

            // Disallow natural death. The time check here is as a way of catching cases where multiple hits happen on the same frame and trigger a death.
            // If it just checked the attack state, then hit one would trigger the state change, set the HP to one, and the second hit would then deplete the
            // single HP and prematurely kill Noxus.
            if (CurrentAttack == NoxusEggAttackType.DeathAnimation && AttackTimer >= 10f)
                return true;

            TriggerDeathAnimation();
            return false;
        }

        public override void OnKill()
        {
            WorldSaveSystem.HasDefeatedNoxusEgg = true;
            NetMessage.SendData(MessageID.WorldData);
        }

        public override void BossLoot(ref string name, ref int potionType) => SetOmegaPotionLoot(ref potionType);

        // Ensure that Noxus' contact damage adhere to the special boss-specific cooldown slot, to prevent things like lava cheese.
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            cooldownSlot = ImmunityCooldownID.Bosses;
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<NoxusFumes>(), DebuffDuration_RegularAttack);
        }
        #endregion Hit Effects and Loot

        #region Gotta Manually Disable Despawning Lmao

        // Disable natural despawning for Noxus.
        public override bool CheckActive() => false;

        #endregion Gotta Manually Disable Despawning Lmao
    }
}
