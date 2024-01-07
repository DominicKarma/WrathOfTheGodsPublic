using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Metaballs;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Enemies.NoxusWorld.DismalSeekers
{
    public class DismalSeeker : ModNPC
    {
        public enum DismalSeekerAttackType
        {
            WalkForward,
            BecomeAngry,
            RedirectingLanternFire,
            ThrowLantern
        }

        #region Fields and Properties

        private LoopedSoundInstance ambienceLoopSound;

        public Player Target => Main.player[NPC.target];

        public float LanternRotation
        {
            get;
            set;
        }

        public float LanternAngularVelocity
        {
            get;
            set;
        }

        public bool LanternIsInUse
        {
            get;
            set;
        } = true;

        public DismalSeekerAttackType CurrentState
        {
            get => (DismalSeekerAttackType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public Vector2 OpacityAdjustedScale => new Vector2(Lerp(1f, 3f, Pow(1f - NPC.Opacity, 2f)), Lerp(1f, 0.04f, Sqrt(1f - NPC.Opacity))) * NPC.scale;

        public Vector2 HandOffset
        {
            get
            {
                float horizontalOffset = 12f;
                if (FrameY == 4f)
                    horizontalOffset += 2f;
                if (FrameY == 5f)
                    horizontalOffset += 4f;
                if (FrameY == 6f)
                    horizontalOffset += 6f;

                return new Vector2(NPC.spriteDirection * horizontalOffset, -2f).RotatedBy(NPC.rotation) * OpacityAdjustedScale;
            }
        }

        public ref float AttackTimer => ref NPC.ai[1];

        public ref float FrameY => ref NPC.localAI[0];

        public ref float EyeOpacity => ref NPC.localAI[1];

        public ref float LanternShineInterpolant => ref NPC.localAI[2];

        public ref float LanternBackglowFadeout => ref NPC.localAI[3];

        public static Asset<Texture2D> MyTexture
        {
            get;
            private set;
        }

        public static Asset<Texture2D> EyeTexture
        {
            get;
            private set;
        }

        public static readonly SoundStyle AmbienceSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/DismalSeekerAmbience") with { Volume = 0.16f };

        public static readonly SoundStyle AngerSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/DismalSeekerAnger") with { Volume = 0.57f };

        public static readonly SoundStyle EmberExtinguishSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/EmberExtinguish") with { Volume = 0.61f };

        public static readonly SoundStyle LanternExplodeSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/DismalLanternExplode") with { Volume = 0.9f, PitchVariance = 0.07f };

        public static readonly SoundStyle LanternFireShootSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/DismalLanternFireEmit") with { Volume = 0.7f };

        public static readonly SoundStyle LanternSwaySound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/DismalLanternSway", 2) with { Volume = 0.6f };

        public static readonly SoundStyle LanternThrowSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Enemies/DismalLanternThrow") with { Volume = 0.5f, PitchVariance = 0.04f };

        #endregion Fields and Properties

        #region Initialization

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 7;
            this.ExcludeFromBestiary();

            if (Main.netMode != NetmodeID.Server)
            {
                MyTexture = ModContent.Request<Texture2D>(Texture);
                EyeTexture = ModContent.Request<Texture2D>($"{Texture}Eye");
            }
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 15f;
            NPC.damage = 0;
            NPC.width = 26;
            NPC.height = 48;
            NPC.defense = 8;
            NPC.lifeMax = 600;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.Opacity = 0f;
            NPC.HitSound = EntropicGod.HitSound;
            NPC.DeathSound = SoundID.NPCDeath55;
        }

        #endregion Initialization

        #region Syncing

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)LanternIsInUse.ToInt());
            writer.Write(NPC.Opacity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            LanternIsInUse = reader.ReadByte() != 0;
            NPC.Opacity = reader.ReadSingle();
        }

        #endregion Syncing

        #region AI
        public override void AI()
        {
            // Reset things every frame.
            NPC.damage = NPC.defDamage;
            NPC.dontTakeDamage = false;
            NPC.immortal = false;

            switch (CurrentState)
            {
                case DismalSeekerAttackType.WalkForward:
                    DoBehavior_WalkForward();
                    break;
                case DismalSeekerAttackType.BecomeAngry:
                    DoBehavior_BecomeAngry();
                    break;
                case DismalSeekerAttackType.RedirectingLanternFire:
                    DoBehavior_RedirectingLanternFire();
                    break;
                case DismalSeekerAttackType.ThrowLantern:
                    DoBehavior_ThrowLantern();
                    break;
            }

            // Update the lantern rotation.
            if (LanternIsInUse)
            {
                LanternRotation = LanternRotation.AngleTowards(0f, 0.1f);
                LanternRotation += LanternAngularVelocity;
                LanternAngularVelocity *= 0.95f;
            }
            else
            {
                LanternRotation = 0f;
                LanternAngularVelocity = 0f;
            }

            // Create a periodic aberration effect.
            if (AttackTimer % 60f == 1f)
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 1.4f, 30);

            // Update the ambience sound.
            ambienceLoopSound ??= LoopedSoundManager.CreateNew(AmbienceSound, () => !NPC.active);
            ambienceLoopSound?.Update(NPC.Center);

            // Emit lantern smoke and fire.
            if (LanternIsInUse)
                DismalSeekerLantern.EmitIdleParticles(NPC.Center + HandOffset, Vector2.UnitX * NPC.velocity, NPC.rotation, NPC.Opacity);

            // Search for targets.
            NPC.TargetClosest();

            // Emit light.
            Lighting.AddLight(NPC.Center, Vector3.One * NPC.Opacity * 0.7f);

            AttackTimer++;
        }

        public void DoBehavior_WalkForward()
        {
            float minWalkSpeed = 0.4f;
            float maxWalkSpeed = 3.6f;
            ref float walkSpeed = ref NPC.ai[2];

            // Fade in.
            NPC.Opacity = Clamp(NPC.Opacity + 0.1f, 0f, 1f);

            // Calculate tile values.
            int xCenterTileCoords = (int)(NPC.spriteDirection == -1 ? NPC.Left.X : NPC.Right.X) / 16;
            int yBottomTileCoords = (int)(NPC.Bottom.Y - 15) / 16;
            Tile tileAhead = Framing.GetTileSafely(new Point(xCenterTileCoords + NPC.spriteDirection * 2, yBottomTileCoords));
            Tile tileAheadAboveTarget = Framing.GetTileSafely(new Point(xCenterTileCoords + NPC.spriteDirection * 2, yBottomTileCoords - 1));
            Tile tileAheadBelowTarget = Framing.GetTileSafely(new Point(xCenterTileCoords + NPC.spriteDirection * 2, yBottomTileCoords + 1));
            bool onSolidGround = false;
            for (int dx = -2; dx <= 2; dx++)
            {
                Tile tileBelowTarget = Framing.GetTileSafely(new Point(xCenterTileCoords + dx, yBottomTileCoords + 1));
                if (tileBelowTarget.HasTile && (Main.tileSolid[tileBelowTarget.TileType] || Main.tileSolidTop[tileBelowTarget.TileType]))
                {
                    onSolidGround = true;
                    break;
                }
            }

            // Be immortal while walking, to prevent one-shot hits.
            NPC.immortal = true;

            // Become angry if hit.
            if (NPC.justHit)
            {
                SoundEngine.PlaySound(GlitchSound);
                ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 2f, 90);

                walkSpeed = 0f;
                CurrentState = DismalSeekerAttackType.BecomeAngry;
                AttackTimer = 0f;
                NPC.netUpdate = true;
                return;
            }

            // Disappear if far from the target.
            if (!NPC.WithinRange(Target.Center, 3500f) && AttackTimer >= 300f)
                NPC.active = false;

            // Decide the desired walk speed.
            if (AttackTimer <= 1f || Main.rand.NextBool(180))
            {
                float oldWalkSpeed = walkSpeed;
                walkSpeed = Main.rand.NextFloat(minWalkSpeed, maxWalkSpeed);
                float walkSpeedChange = Distance(oldWalkSpeed, walkSpeed);

                if (walkSpeedChange >= 1.45f)
                {
                    if (NPC.soundDelay <= 0)
                    {
                        SoundEngine.PlaySound(LanternSwaySound, NPC.Center);
                        NPC.soundDelay = 300;
                    }
                    for (int i = 0; i < 8; i++)
                    {
                        Dust fire = Dust.NewDustPerfect(NPC.Center + HandOffset + Vector2.UnitY.RotatedBy(NPC.rotation) * 8f, 264);
                        fire.velocity = -Vector2.UnitY.RotatedByRandom(0.9f) * Main.rand.NextFloat(5f) + Vector2.UnitX * NPC.velocity * 1.5f;
                        fire.color = Color.Lerp(Color.DarkBlue, Color.Fuchsia, Main.rand.NextFloat(0.8f));
                        fire.fadeIn = 1.05f;
                        fire.scale = Main.rand.NextFloat(0.9f, 1.5f);
                        fire.noLight = true;
                        fire.noGravity = true;
                    }
                }
                LanternAngularVelocity += NPC.spriteDirection * walkSpeedChange * 0.1f;
                NPC.netUpdate = true;
            }

            // Update the lantern.
            LanternAngularVelocity = Lerp(LanternAngularVelocity, NPC.velocity.X * 0.02f + Main.windSpeedCurrent * 0.07f, 0.032f);

            // Walk forward.
            float walkDirection = Abs(NPC.velocity.X) >= 0.12f || !onSolidGround ? Sign(NPC.velocity.X) : Sign(Target.Center.X - NPC.Center.X);
            if (AttackTimer <= 5f)
                walkDirection = Sign(Target.Center.X - NPC.Center.X);

            NPC.velocity.X = Lerp(NPC.velocity.X, walkDirection * walkSpeed, 0.06f);
            NPC.spriteDirection = Sign(NPC.velocity.X);

            // The next tile below the seeker's feet is inactive or actuated, jump.
            if (onSolidGround && !tileAheadBelowTarget.HasTile && Main.tileSolid[tileAheadBelowTarget.TileType])
            {
                NPC.velocity.Y = -6f;
                NPC.netUpdate = true;
            }

            // Jump if the seeker is stuck in some what on the X axis, and project dust to
            // make it look like it's using magic as a boost.
            if (onSolidGround && NPC.position.X == NPC.oldPosition.X)
            {
                NPC.position.X += NPC.spriteDirection * 4f;
                NPC.velocity.Y = -6f;
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Dust magicDust = Dust.NewDustPerfect(NPC.Bottom, 112);
                        magicDust.velocity = Vector2.UnitX * Lerp(-4f, 4f, i / 9f);
                        magicDust.noGravity = true;
                    }
                }
                NPC.netSpam = 0;
                NPC.netUpdate = true;
            }

            // Jump if there's an impending obstacle.
            bool aheadObstacle = (tileAheadAboveTarget.HasTile && Main.tileSolid[tileAheadAboveTarget.TileType]) || (tileAhead.HasTile && Main.tileSolid[tileAhead.TileType]);
            if (onSolidGround && aheadObstacle)
            {
                NPC.velocity.Y = -7.2f;
                NPC.netUpdate = true;
            }

            // Handle frames.
            if (onSolidGround)
                NPC.frameCounter += Abs(NPC.velocity.X) / walkSpeed;
            if (NPC.frameCounter >= 11f)
            {
                FrameY = (FrameY + 1f) % 4f;
                NPC.frameCounter = 0f;
            }

            // Apply extra gravity.
            NPC.velocity.Y += 0.075f;
        }

        public void DoBehavior_BecomeAngry()
        {
            int eyeFadeInTime = 150;
            int riseSlowdownTime = 60;
            int attackTransitionDelay = 20;

            // Use max HP and become temporarily immortal.
            NPC.life = NPC.lifeMax;
            NPC.immortal = true;

            // Turn off gravity and tile collision.
            NPC.noGravity = true;
            NPC.noTileCollide = true;

            // Rise upward.
            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = InverseLerp(eyeFadeInTime + riseSlowdownTime, eyeFadeInTime, AttackTimer) * -0.5f;

            // Hold hands out.
            NPC.frameCounter++;
            if (NPC.frameCounter >= 5f)
            {
                NPC.frameCounter = 0f;
                FrameY++;
            }
            FrameY = Clamp(FrameY, 4f, 6f);

            // Look at the target.
            NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();

            // Make the eye appear. Once it's close to fully faded in, the lantern should shine for a moment.
            float eyeAnimationInterpolant = InverseLerp(0f, eyeFadeInTime, AttackTimer);
            EyeOpacity = Pow(eyeAnimationInterpolant, 3f);
            LanternShineInterpolant = InverseLerp(0.65f, 0.95f, eyeAnimationInterpolant);
            LanternBackglowFadeout = InverseLerp(0.3f, 0.8f, eyeAnimationInterpolant) * 0.75f;

            // Play the angy sound on the first frame.
            if (AttackTimer == 1f)
                SoundEngine.PlaySound(AngerSound, NPC.Center);
            if (AttackTimer == eyeFadeInTime - 45f)
                SoundEngine.PlaySound(TwinkleSound with { Pitch = -0.4f }, NPC.Center);

            // Fade out in anticipation of a teleport.
            if (AttackTimer >= eyeFadeInTime + riseSlowdownTime + attackTransitionDelay - 10f)
            {
                if (AttackTimer == eyeFadeInTime + riseSlowdownTime + attackTransitionDelay - 10f)
                    SoundEngine.PlaySound(TeleportInSound, NPC.Center);

                NPC.Opacity = Clamp(NPC.Opacity - 0.11f, 0f, 1f);
            }

            // Shake the screen and begin attacking when ready.
            if (AttackTimer >= eyeFadeInTime + riseSlowdownTime + attackTransitionDelay)
            {
                StartShakeAtPoint(NPC.Center, 10f, shakeStrengthDissipationIncrement: 0.36f);
                NPC.Center = Target.Center + new Vector2(Main.rand.NextFloat(675f, 900f) * Main.rand.NextFromList(-1f, 1f), -400f);
                AttackTimer = 0f;
                CurrentState = DismalSeekerAttackType.RedirectingLanternFire;
                NPC.netUpdate = true;
            }
        }

        public void DoBehavior_RedirectingLanternFire()
        {
            int fastRedirectTime = 24;
            int hoverFireShootTime = 72;
            int flameReleaseRate = 5;
            int attackTransitionDelay = 120;
            ref float initialHorizontalOffset = ref NPC.ai[2];

            NPC.frameCounter++;
            FrameY = (int)(NPC.frameCounter / 5f) % 4;

            // Disable natural despawning.
            NPC.timeLeft = 3600;

            // Reverse the lantern backglow fadeout permanently.
            float idealFadeout = Lerp(1.15f, -0.9f, NPC.Opacity);
            LanternBackglowFadeout = Lerp(LanternBackglowFadeout, idealFadeout, 0.2f);

            if (!NPC.WithinRange(Target.Center, 1500f))
                NPC.Center = Target.Center + new Vector2(-500f, -850f);

            // Fade in from a teleport if necessary.
            if (AttackTimer <= 10f)
            {
                NPC.Opacity = MathF.Max(NPC.Opacity, AttackTimer / 10f);
                if (NPC.Opacity <= 0.2f && AttackTimer == 1f)
                    SoundEngine.PlaySound(TeleportOutSound, NPC.Center);
            }

            // Play the fire shoot sound at first.
            if (AttackTimer == 1f)
                SoundEngine.PlaySound(LanternSwaySound, NPC.Center);
            if (AttackTimer == 10f)
                SoundEngine.PlaySound(LanternFireShootSound, NPC.Center);

            // Look forward.
            NPC.spriteDirection = Sign(NPC.velocity.X);

            // Move quickly towards the target at first.
            if (AttackTimer <= fastRedirectTime)
            {
                initialHorizontalOffset = (Target.Center.X < NPC.Center.X).ToDirectionInt();

                Vector2 hoverDestination = Target.Center + new Vector2(initialHorizontalOffset * 200f, -120f);
                float redirectInterpolant = AttackTimer / fastRedirectTime;
                NPC.Center = Vector2.Lerp(NPC.Center, hoverDestination, Pow(1f - redirectInterpolant, 3f) * 0.12f);
                NPC.SmoothFlyNear(hoverDestination, redirectInterpolant * 0.15f, 0.85f);
                return;
            }

            // Attempt to slowly fly above the player while releasing flames.
            if (AttackTimer <= fastRedirectTime + hoverFireShootTime && NPC.Opacity >= 0.95f)
            {
                Vector2 force = NPC.DirectionToSafe(Target.Center - Vector2.UnitY * 300f) * 0.07f;
                force.X = Abs(force.X) * initialHorizontalOffset;
                NPC.velocity = (NPC.velocity + force).ClampLength(0f, 10f);

                // Release the flames.
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % flameReleaseRate == flameReleaseRate - 1f && !NPC.WithinRange(Target.Center, 130f))
                {
                    Vector2 flameSpawnPosition = NPC.Center + HandOffset + Vector2.UnitY.RotatedBy(NPC.rotation) * OpacityAdjustedScale * 14f;
                    Vector2 flameVelocity = Main.rand.NextVector2CircularEdge(0.9f, 0.9f) + (Target.Center - flameSpawnPosition).SafeNormalize(Vector2.Zero) * 0.4f;
                    NewProjectileBetter(flameSpawnPosition, flameVelocity, ModContent.ProjectileType<RedirectingDarkFlame>(), 40, 0f);
                }
            }

            // Disappear after enough time has passed.
            if (AttackTimer >= fastRedirectTime)
            {
                // Play a teleport sound.
                if (AttackTimer == fastRedirectTime + hoverFireShootTime - 10f)
                    SoundEngine.PlaySound(TeleportInSound, NPC.Center);

                NPC.Opacity = InverseLerp(0f, -10f, AttackTimer - fastRedirectTime - hoverFireShootTime);

                // Wait a bit, and then transition to the next attack.
                if (NPC.Opacity <= 0f && AttackTimer >= fastRedirectTime + hoverFireShootTime + attackTransitionDelay)
                {
                    initialHorizontalOffset = 0f;
                    AttackTimer = 0f;
                    NPC.Opacity = 0f;
                    CurrentState = DismalSeekerAttackType.ThrowLantern;
                    NPC.netUpdate = true;
                }
            }

            // Be invincible while close to invisible.
            if (NPC.Opacity <= 0.5f)
                NPC.dontTakeDamage = true;
        }

        public void DoBehavior_ThrowLantern()
        {
            int slowdownTime = 75;
            int throwTime = 10;
            int attackTransitionDelay = 210;

            // Slow down to a halt and fall to the ground.
            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = Lerp(NPC.velocity.Y, 10f, 0.017f);

            // Look at the target.
            NPC.spriteDirection = (Target.Center.X > NPC.Center.X).ToDirectionInt();

            // Allow tile collision again.
            NPC.noTileCollide = false;

            // Teleport near the player on the first frame.
            if (AttackTimer == 1f)
            {
                SoundEngine.PlaySound(TeleportOutSound, NPC.Center);
                NPC.Center = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 400f, -200f);
                NPC.velocity *= 0.4f;
                NPC.Opacity = 0f;
                NPC.netUpdate = true;
            }
            if (AttackTimer <= 24f)
                NPC.Opacity = InverseLerp(0f, 12f, AttackTimer);

            // Hold arms out.
            if (AttackTimer <= slowdownTime)
                FrameY = 5f;

            // Use throwing frames.
            float throwInterpolant = InverseLerp(0f, throwTime, AttackTimer - slowdownTime);
            if (throwInterpolant > 0f)
                FrameY = (int)Lerp(5f, 3f, throwInterpolant);

            // Throw the lantern when ready.
            if (AttackTimer == slowdownTime + throwTime)
            {
                SoundEngine.PlaySound(LanternThrowSound, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    LanternIsInUse = false;
                    NPC.netUpdate = true;

                    Vector2 lanternVelocity = NPC.DirectionToSafe(Target.Center) * 11f - Vector2.UnitY * 9f;
                    NewProjectileBetter(NPC.Center + HandOffset, lanternVelocity, ModContent.ProjectileType<DismalSeekerLantern>(), 40, 0f, -1, LanternRotation);
                }
            }

            // Disappear.
            if (AttackTimer >= slowdownTime + throwTime + 60f)
            {
                if (AttackTimer == slowdownTime + throwTime + 61f)
                    SoundEngine.PlaySound(TeleportInSound, NPC.Center);

                NPC.Opacity = Clamp(NPC.Opacity - 0.1f, 0f, 1f);
                NPC.dontTakeDamage = true;
            }

            // Update the lantern backglow fadeout.
            LanternBackglowFadeout = 1f - NPC.Opacity;

            // Once enough time has passed, transition to the next attack, with the lantern coming back.
            if (AttackTimer >= slowdownTime + throwTime + attackTransitionDelay)
            {
                AttackTimer = 0f;
                NPC.Center = Target.Center + new Vector2(Main.rand.NextFloat(750f, 1050f) * Main.rand.NextFromList(-1f, 1f), -400f);
                CurrentState = DismalSeekerAttackType.RedirectingLanternFire;
                LanternIsInUse = true;
                NPC.netUpdate = true;
            }
        }

        #endregion AI

        #region Hit Effects

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            int particleCount = NPC.life <= 0 ? 30 : 5;

            // Create particles when hit.
            for (int i = 0; i < particleCount; i++)
            {
                float particleSize = Main.rand.NextFloat(14f, 27f);
                Vector2 particleVelocity = new Vector2(hit.HitDirection, 0f).RotatedByRandom(0.8f) * Main.rand.NextFloat(1f, 5f);
                if (hit.Crit)
                {
                    particleVelocity *= 1.5f;
                    particleVelocity += Main.rand.NextVector2Circular(3f, 3f);
                    particleSize *= 1.6f;
                }
                if (NPC.life <= 0)
                {
                    particleVelocity += Main.rand.NextVector2Circular(5f, 5f);
                    particleSize *= Main.rand.NextFloat(1f, 1.8f);
                }

                PitchBlackMetaball.CreateParticle(NPC.Center + Main.rand.NextVector2Circular(8f, 20f).RotatedBy(NPC.rotation) * OpacityAdjustedScale, particleVelocity, particleSize);
            }

            // Create tatters when killed.
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 tatterSpawnPosition = NPC.TopLeft + Main.rand.NextVector2Square(NPC.width, NPC.height).RotatedBy(NPC.rotation);
                    Gore.NewGore(NPC.GetSource_Death(), tatterSpawnPosition, Main.rand.NextVector2CircularEdge(10f, 1f) - Vector2.UnitY * 2f, ModContent.Find<ModGore>(Mod.Name, "DismalSeekerTatter").Type, NPC.scale);
                }
            }
        }

        #endregion Hit Effects

        #region Loot

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Silk, 1, 3, 7));
            npcLoot.Add(ItemDropRule.Common(ItemID.ObsidianLantern));
        }

        #endregion Loot

        #region Drawing

        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = (int)FrameY * frameHeight;
        }

        public override Color? GetAlpha(Color drawColor) => Color.Lerp(drawColor, Color.White, 0.9f) * NPC.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Collect textures.
            Texture2D texture = MyTexture.Value;
            Texture2D eyeTexture = EyeTexture.Value;

            // Calculate simple draw variables.
            Vector2 drawPosition = NPC.Center - screenPos;
            Color color = NPC.GetAlpha(drawColor);
            SpriteEffects direction = NPC.spriteDirection.ToSpriteDirection();

            // Draw the seeker.
            Main.spriteBatch.Draw(texture, drawPosition, NPC.frame, color, NPC.rotation, NPC.frame.Size() * 0.5f, OpacityAdjustedScale, direction, 0f);

            // Draw the lantern if it's in use.
            if (LanternIsInUse)
            {
                Vector2 lanternDrawPosition = drawPosition + HandOffset;
                float backglowOpacity = Clamp(1f - LanternBackglowFadeout, 0f, 1f) * Sqrt(NPC.Opacity) * 0.8f;
                if (NPC.IsABestiaryIconDummy)
                    backglowOpacity = 0f;
                DismalSeekerLantern.DrawLantern(lanternDrawPosition, NPC.rotation + LanternRotation, NPC.Opacity, backglowOpacity, NPC.scale, OpacityAdjustedScale, direction, LanternShineInterpolant);
            }

            // Draw the eye over everything, so that it's as visible as possible.
            Color eyeColor = (Color.LightCoral with { A = 0 }) * EyeOpacity * NPC.Opacity;
            Main.spriteBatch.Draw(eyeTexture, drawPosition, NPC.frame, eyeColor, NPC.rotation, NPC.frame.Size() * 0.5f, OpacityAdjustedScale, direction, 0f);

            return false;
        }
        #endregion Drawing
    }
}
