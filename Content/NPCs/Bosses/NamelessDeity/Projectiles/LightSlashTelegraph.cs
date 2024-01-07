using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class LightSlashTelegraph : ModProjectile, IDrawAdditive, IDrawPixelated, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public static PrimitiveTrail GodRayDrawer
        {
            get;
            private set;
        }

        public static Asset<Texture2D>[] CrackTextureAssets
        {
            get;
            private set;
        }

        public static Asset<Texture2D>[] CrackGlowTextureAssets
        {
            get;
            private set;
        }

        public const int TotalCrackTextures = 2;

        public ref float Frame => ref Projectile.localAI[0];

        public ref float GlowOpacity => ref Projectile.localAI[1];

        public ref float GlowScale => ref Projectile.localAI[2];

        public ref float Lifetime => ref Projectile.ai[0];

        public ref float Time => ref Projectile.ai[1];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 13;

            // Load textures.
            if (Main.netMode != NetmodeID.Server)
            {
                CrackTextureAssets = new Asset<Texture2D>[TotalCrackTextures];
                CrackGlowTextureAssets = new Asset<Texture2D>[TotalCrackTextures];
                for (int i = 0; i < TotalCrackTextures; i++)
                {
                    CrackTextureAssets[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Projectiles/LightSlashTelegraph{i + 1}");
                    CrackGlowTextureAssets[i] = ModContent.Request<Texture2D>($"NoxusBoss/Content/NPCs/Bosses/NamelessDeity/Projectiles/LightSlashTelegraphGlow{i + 1}");
                }
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 272;
            Projectile.height = 272;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 60000;
            Projectile.scale = 0.0001f;
            GlowScale = 1f;
        }

        public override void AI()
        {
            // Initialize things on the first frame.
            if (Time == 0f)
            {
                Projectile.rotation = Main.rand.NextFloatDirection() * PiOver4;
                RadialScreenShoveSystem.Start(Projectile.Center, 15);
            }

            // Make the crack emerge.
            float lifetimeCompletion = Time / Lifetime;
            Projectile.scale = lifetimeCompletion >= 0.16f ? 1f : 0f;

            // Play a reality crack sound as the crack appears.
            // Also create some outward expanding shard particles.
            if (Time == (int)(Lifetime * 0.16f))
            {
                SoundEngine.PlaySound(NamelessDeityBoss.RealityCrackSound with { Pitch = 0.25f });
                ScreenEffectSystem.SetBlurEffect(Projectile.Center, 1.5f, 24);

                // Create shards. Ones that fly out faster also disappear faster.
                for (int i = 0; i < 24; i++)
                {
                    Vector2 shardVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 30f);
                    int shardLifetime = (int)Remap(shardVelocity.Length(), 30f, 11f, 4f, 52f) + Main.rand.Next(10);
                    Color shardColor = Color.Lerp(Color.LightCoral, Color.Wheat, Main.rand.NextFloat(0.7f));
                    Color backglowColor = Color.Lerp(Color.Coral, Color.Red, Main.rand.NextFloat(0.6f)) * 0.8f;

                    GlowyShardParticle shard = new(Projectile.Center, shardVelocity, shardColor, backglowColor, 1f, 0.3f, shardLifetime);
                    shard.Spawn();
                }

                // Create a strong bloom behind everything.
                StrongBloom bloom = new(Projectile.Center, Vector2.Zero, DialogColorRegistry.NamelessDeityTextColor, 6f, (int)(Lifetime - Time));
                bloom.Spawn();
            }

            if (Time == (int)(Lifetime * 0.56f) + 2)
            {
                SoundEngine.PlaySound(PulseSound with { Pitch = 0.4f });
                Frame = 1;
            }

            // Make the crack glow after enough time has passed.
            float glowInterpolant = InverseLerp(0.48f, 0.91f, lifetimeCompletion);
            GlowOpacity = InverseLerp(0f, 0.3f, glowInterpolant);
            GlowScale = Sin(Pow(glowInterpolant, 1.3f) * PiOver2) * GlowOpacity * 1.32f;

            // Increment the time. Once it exceeds the lifetime of the projectile, die.
            Time++;
            if (Time >= Lifetime)
                Projectile.Kill();
        }

        public float GodRayWidth(float completionRatio)
        {
            return Lerp(3f, 16f, completionRatio) * GlowScale * Projectile.scale;
        }

        public Color GodRayColor(float completionRatio)
        {
            float godRayOpacity = InverseLerp(0.82f, 0.36f, completionRatio) * Projectile.Opacity;
            return Color.Lerp(Color.White, Color.Coral, completionRatio) * godRayOpacity;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Collect draw information.
            Color color = Color.White * Projectile.Opacity;
            Color colorBack = Color.DarkBlue * Projectile.Opacity;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 scale = Vector2.One * Projectile.scale * 1.7f;
            Texture2D crackTexture = CrackTextureAssets[(int)Frame].Value;
            Texture2D crackGlowTexture = CrackGlowTextureAssets[(int)Frame].Value;

            // Draw the textures. The crack itself disappears at the end of this projectile's lifetime, but everything else lingers.
            float crackOpacity = InverseLerp(0.9f, 0.71f, Time / Lifetime);
            spriteBatch.Draw(crackTexture, drawPosition, null, color * crackOpacity, Projectile.rotation, crackTexture.Size() * 0.5f, scale * (1f + (1f - crackOpacity) * 0.25f), 0, 0f);
            spriteBatch.Draw(crackGlowTexture, drawPosition, null, colorBack * GlowOpacity * 0.52f, Projectile.rotation, crackGlowTexture.Size() * 0.5f, scale * GlowScale * (1.25f + (1f - crackOpacity) * 0.36f), 0, 0f);
            spriteBatch.Draw(crackGlowTexture, drawPosition, null, color * GlowOpacity, Projectile.rotation, crackGlowTexture.Size() * 0.5f, scale * GlowScale * (1f + (1f - crackOpacity) * 0.36f), 0, 0f);

            // Draw a manual glow over the crack right as it appears.
            float refinedGlowIntensity = InverseLerpBump(0.16f, 0.18f, 0.27f, 0.33f, Time / Lifetime);
            for (int i = 0; i < 2; i++)
                spriteBatch.Draw(crackGlowTexture, drawPosition, null, color * refinedGlowIntensity * 0.7f, Projectile.rotation, crackGlowTexture.Size() * 0.5f, scale * 0.6f, 0, 0f);

            // Draw bright spires over everything.
            float spireScale = Projectile.scale * Sin(Time / Lifetime * 2.9f) * 0.6f;
            float spireRotation = Projectile.identity * 0.909f;
            if (Time >= Lifetime * 0.45f)
            {
                if (Time % 20 >= 10)
                    spireRotation += 1.03f;
                spireScale *= 1f - InverseLerpBump(6f, 10f, 11f, 14f, Time % 20f);
            }
            spriteBatch.Draw(BrightSpiresTexture, drawPosition, null, color * Sqrt(GlowOpacity), spireRotation, BrightSpiresTexture.Size() * 0.5f, scale * spireScale, 0, 0f);
        }

        public void DrawWithPixelation()
        {
            // Initialize the god ray drawer.
            GodRayDrawer ??= new PrimitiveTrail(GodRayWidth, GodRayColor, null, false);

            // Draw god rays.
            ulong rngSeed = (ulong)(Projectile.identity * 17175 + 19);
            for (int i = 0; i < 6; i++)
            {
                float godRayRotation = TwoPi * i / 6f + Projectile.rotation + Time * (i % 2f - 1f).NonZeroSign() * 0.0184f;
                godRayRotation += Utils.RandomFloat(ref rngSeed) * 0.3f;
                float godRayLength = Lerp(300f, 508f, Utils.RandomFloat(ref rngSeed)) * Projectile.scale;

                List<Vector2> godRayPositions = Projectile.GetLaserControlPoints(24, godRayLength, godRayRotation.ToRotationVector2());
                GodRayDrawer.Draw(godRayPositions, -Main.screenPosition, 32);
            }
        }
    }
}
