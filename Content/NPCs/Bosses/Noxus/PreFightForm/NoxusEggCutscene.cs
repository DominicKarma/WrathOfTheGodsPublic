using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.Projectiles;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Metaballs;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.PreFightForm
{
    public class NoxusEggCutscene : ModNPC
    {
        #region Custom Types and Enumerations
        public enum NoxusEggAIType
        {
            ComeOutOfSky,
            GetUp,
            DoGlitchEffectsAndDisappear
        }

        #endregion Custom Types and Enumerations

        #region Fields and Properties

        public TwinkleParticle Twinkle
        {
            get;
            set;
        }

        public Player PlayerToFollow => Main.player[NPC.target];

        public NoxusEggAIType CurrentState
        {
            get => (NoxusEggAIType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AITimer => ref NPC.ai[1];

        public ref float AngularVelocity => ref NPC.ai[2];

        public ref float BurnIntensity => ref NPC.localAI[0];

        public override string Texture => "NoxusBoss/Content/NPCs/Bosses/Noxus/FirstPhaseForm/NoxusEgg";

        #endregion Fields and Properties

        #region Initialization
        public override void SetStaticDefaults()
        {
            this.ExcludeFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 50f;
            NPC.damage = 0;
            NPC.width = 224;
            NPC.height = 224;
            NPC.defense = 0;
            NPC.lifeMax = 1000000;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.canGhostHeal = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.value = 0;
            NPC.netAlways = true;
            NPC.Opacity = 0f;
        }

        #endregion Initialization

        #region AI
        public override void AI()
        {
            switch (CurrentState)
            {
                case NoxusEggAIType.ComeOutOfSky:
                    DoBehavior_ComeOutOfSky();
                    break;
                case NoxusEggAIType.GetUp:
                    DoBehavior_GetUp();
                    break;
                case NoxusEggAIType.DoGlitchEffectsAndDisappear:
                    DoBehavior_DoGlitchEffectsAndDisappear();
                    break;
            }

            // Emit heat particles while on fire.
            if (BurnIntensity > 0f)
            {
                for (int i = 0; i < BurnIntensity * 15f; i++)
                {
                    Color fireMistColor = Color.Lerp(Color.Red, Color.Gray, Main.rand.NextFloat(0.5f, 0.85f));
                    var smoke = new MediumMistParticle(NPC.Center + Main.rand.NextVector2Circular(124f, 124f) * NPC.scale, Main.rand.NextVector2Circular(4.5f, 4.5f) - Vector2.UnitY * 3f, fireMistColor, Color.Gray, Main.rand.NextFloat(0.5f, 0.8f), 198 - Main.rand.Next(50), 0.02f);
                    smoke.Spawn();
                }
            }

            NPC.ShowNameOnHover = NPC.scale >= 0.7f;
            AITimer++;
        }

        public void DoBehavior_ComeOutOfSky()
        {
            int crashDelay = 90;
            float startingCrashSpeed = 29f;
            float endingCrashSpeed = 96f;
            float crashAcceleration = 1.1f;
            Vector2 startingPosition = PlayerToFollow.Center + new Vector2(NPC.ai[3] * -500f, -420f);

            // Choose a player to follow on the first frame.
            if (AITimer <= 1f)
            {
                NPC.TargetClosest();

                // Verify that the player is in an open surface area and not in space.
                // If they aren't, despawn as though nothing happened.
                bool openAir = PlayerToFollow.ZoneForest && !PlayerToFollow.ZoneSkyHeight;
                for (int dy = 4; dy < 36; dy++)
                {
                    Tile t = ParanoidTileRetrieval((int)(PlayerToFollow.Center.X / 16f), (int)(PlayerToFollow.Center.Y / 16f) - dy);
                    if (t.HasUnactuatedTile && WorldGen.SolidTile(t))
                    {
                        openAir = false;
                        break;
                    }

                    t = ParanoidTileRetrieval((int)(startingPosition.X / 16f), (int)(startingPosition.Y / 16f) - dy);
                    if (t.HasUnactuatedTile && WorldGen.SolidTile(t))
                    {
                        openAir = false;
                        break;
                    }
                }

                if (!openAir)
                    NPC.active = false;
                NPC.ai[3] = PlayerToFollow.direction;

                return;
            }

            // Stick near the player as the twinkle does its animation.
            if (AITimer <= crashDelay)
            {
                if (AITimer == 2f)
                    Twinkle = EntropicGod.CreateTwinkle(NPC.Center, Vector2.One * 1.5f);

                NPC.Center = startingPosition;
                NPC.Opacity = 1f;
                NPC.scale = 0.001f;
            }

            // Create a twinkle on the first frame and hold it at Noxus' position.
            if (Twinkle is not null)
            {
                Twinkle.Position = NPC.Center + Vector2.UnitY * 50f;
                Twinkle.Time = (int)AITimer;
                if (Twinkle.Time >= 16)
                {
                    Twinkle.Time = 15;
                    Twinkle.ScaleFactor *= Remap(AITimer, Twinkle.Time, crashDelay - 10f, 1f, 1.05f);
                }
            }

            // Crash in front of the player after the twinkle is gone.
            if (AITimer == crashDelay)
            {
                NPC.velocity = NPC.DirectionToSafe(PlayerToFollow.Center - Vector2.UnitX * NPC.ai[3] * 200f) * startingCrashSpeed;
                NPC.netUpdate = true;
            }

            // Accelerate and fade in.
            if (NPC.velocity.Length() < endingCrashSpeed)
                NPC.velocity *= crashAcceleration;
            if (AITimer >= crashDelay)
            {
                NPC.scale = Clamp(NPC.scale + 0.084f, 0f, 1f);

                if (NPC.velocity != Vector2.Zero)
                    NPC.rotation = NPC.velocity.ToRotation() - PiOver2;
                BurnIntensity = 1f;
            }

            // Collide with the ground.
            Rectangle actualHitbox = Utils.CenteredRectangle(NPC.Center, NPC.Size * NPC.scale);
            if (AITimer >= crashDelay && actualHitbox.Bottom >= PlayerToFollow.Bottom.Y + 8f && TileCollision(actualHitbox.BottomLeft() - Vector2.UnitY * NPC.scale * 108f, NPC.width, NPC.scale * 108f, out _) && NPC.velocity.Y != 0f)
            {
                SoundEngine.PlaySound(EntropicGod.ExplosionTeleportSound);
                ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.5f, 10);
                StartShakeAtPoint(NPC.Center, 16f);

                NPC.velocity = Vector2.Zero;
                CurrentState = NoxusEggAIType.GetUp;
                AITimer = 0f;
                NPC.netUpdate = true;

                // Create ground collision effects.
                for (int i = 0; i < NPC.width; i += Main.rand.Next(2, 6))
                {
                    Point p = new((int)(NPC.BottomLeft.X + i) / 16, (int)(NPC.BottomLeft.Y / 16f) - 1);
                    Tile t = ParanoidTileRetrieval(p.X, p.Y);
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
                {
                    NewProjectileBetter(NPC.Bottom - Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<GroundStompShock>(), 0, 0f);
                    NewProjectileBetter(NPC.Bottom - Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<DarkWave>(), 0, 0f);
                }
            }
        }

        public void DoBehavior_GetUp()
        {
            int moveDelay = 120;
            int riseTime = moveDelay + 60;

            // Make the burn effect dissipate as Noxus cools off.
            BurnIntensity = Clamp(BurnIntensity - 0.01f, 0f, 1f);

            AngularVelocity *= 0.89f;
            if (Main.rand.NextBool(60) && AITimer >= moveDelay)
                AngularVelocity = WrapAngle(NPC.rotation) * -Main.rand.NextFloat(0.11f, 0.16f);
            NPC.rotation += AngularVelocity;

            // Wait until sufficiently straightened out.
            if (Abs(NPC.rotation) > 0.1f && AITimer >= moveDelay)
                AITimer = moveDelay;

            // Rise upwards before hoving in place.
            float riseInterpolant = InverseLerp(moveDelay + 1f, riseTime, AITimer);
            float hoverInterpolant = InverseLerp(riseTime + 10f, riseTime + 36f, AITimer);
            NPC.velocity = Vector2.Lerp(NPC.velocity, -Vector2.UnitY * riseInterpolant * 10.5f, 0.06f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, -Vector2.UnitY * Sin(AITimer / 30f) * 1.2f, hoverInterpolant);

            if (AITimer >= riseTime + 90f)
            {
                AITimer = 0f;
                CurrentState = NoxusEggAIType.DoGlitchEffectsAndDisappear;
                NPC.netUpdate = true;
            }
        }

        public void DoBehavior_DoGlitchEffectsAndDisappear()
        {
            int glitchBuildupTime = 240;
            int glitchMaximizationDelay = 300;

            // Slow down.
            NPC.velocity *= 0.97f;

            // Randomly make the sky glitch.
            int glitchChance = (int)Remap(AITimer, 60f, glitchBuildupTime, 44f, 18f);
            if (AITimer >= glitchMaximizationDelay)
                glitchChance = 1;

            if (Main.rand.NextBool(glitchChance) && (NoxusSky.SkyIntensityOverride <= 0.4f || glitchChance <= 1))
            {
                if (glitchChance >= 2)
                    SoundEngine.PlaySound(GlitchSound);
                NoxusSky.SkyIntensityOverride = 1f;

                // Create gas particles.
                SoundEngine.PlaySound(SoundID.Item104, NPC.Center);
                for (int i = 0; i < 40; i++)
                {
                    Vector2 cometShootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 20f);
                    NoxusGasMetaball.CreateParticle(NPC.Center + cometShootVelocity.RotatedByRandom(0.98f) * Main.rand.NextFloat(1.3f), cometShootVelocity.RotatedByRandom(0.68f) * Main.rand.NextFloat(1.1f), Main.rand.NextFloat(13f, 56f));
                }

                if (AITimer < glitchMaximizationDelay || AITimer % 12f == 11f)
                {
                    // Scream.
                    SoundEngine.PlaySound(EntropicGod.ScreamSound with { Volume = 1.1f, Pitch = -0.25f });
                    Color burstColor = Main.rand.NextBool() ? Color.SlateBlue : Color.Lerp(Color.White, Color.MediumPurple, 0.7f);

                    // Create blur and burst particle effects.
                    ExpandingChromaticBurstParticle burst = new(NPC.Center, Vector2.Zero, burstColor, 16, 0.1f);
                    burst.Spawn();
                    ScreenEffectSystem.SetBlurEffect(NPC.Center, 0.3f, 24);
                    ScreenEffectSystem.SetChromaticAberrationEffect(NPC.Center, 1f, 10);
                }

                // Do some screen shake.
                if (OverallShakeIntensity <= 9f)
                    StartShakeAtPoint(NPC.Center, 6f);

                if (Main.netMode != NetmodeID.MultiplayerClient && AITimer < glitchMaximizationDelay)
                {
                    NPC.velocity = Main.rand.NextVector2Circular(6f, 6f);
                    NPC.netUpdate = true;

                    for (int i = 0; i < 26; i++)
                        NewProjectileBetter(NPC.Center, Main.rand.NextVector2Circular(60f, 60f), ModContent.ProjectileType<DarkComet>(), 0, 0f);
                }

                // Teleport away in a flash after enough time has passed.
                if (AITimer >= glitchMaximizationDelay + 120f)
                {
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
                    NPC.active = false;
                }
            }

            NPC.rotation = NPC.velocity.X * 0.013f;
        }

        #endregion AI

        #region Drawing

        public override Color? GetAlpha(Color drawColor)
        {
            Color baseColor = Color.Lerp(Color.DarkGray, Color.White, InverseLerp(0.45f, 0.875f, NPC.scale));
            baseColor = Color.Lerp(baseColor, Color.OrangeRed, BurnIntensity);
            return Color.Lerp(baseColor, baseColor.MultiplyRGBA(drawColor), 0.4f) * NPC.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = NoxusEgg.MyTexture.Value;
            Vector2 drawPosition = NPC.Center - screenPos;
            Main.spriteBatch.Draw(texture, drawPosition, null, NPC.GetAlpha(drawColor), NPC.rotation, texture.Size() * 0.5f, NPC.scale, 0, 0f);
            return false;
        }
        #endregion Drawing
    }
}
