using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Common.Easings;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders.Screen;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class Quasar : ModProjectile, IDrawAdditive, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public class EnergySuckParticle
        {
            public int Time;

            public int Lifetime;

            public float Opacity;

            public Color DrawColor;

            public Vector2 Center;

            public Vector2 Velocity;

            public void Update(Vector2 destination)
            {
                Center += Velocity;
                Velocity = Vector2.Lerp(Velocity, (destination - Center) * 0.1f, 0.04f);
                Time++;
                Opacity = InverseLerp(0f, 205f, Center.Distance(destination)) * 0.56f;
            }
        }

        public LoopedSoundInstance BrrrrrSound;

        public List<EnergySuckParticle> Particles = new();

        public ref float Time => ref Projectile.ai[0];

        public ref float ReboundCountdown => ref Projectile.ai[1];

        public ref float Lifetime => ref Projectile.ai[2];

        public override string Texture => InvisiblePixelPath;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 750;
        }

        public override void SetDefaults()
        {
            Projectile.width = 500;
            Projectile.height = 500;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = 999999;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // No Nameless Deity? Die.
            if (NamelessDeityBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Grow over time.
            Projectile.scale = ExponentialEasing.Default.Evaluate(EasingType.In, InverseLerp(0f, 35f, Time)) * 10f;

            // Accelerate towards the target.
            NPCAimedTarget target = NamelessDeityBoss.Myself.GetTargetData();

            if (ReboundCountdown > 0f)
            {
                Projectile.velocity *= 0.99f;
                ReboundCountdown--;
            }
            else if (!Projectile.WithinRange(target.Center, 380f) && Time >= 56f)
            {
                Vector2 force = Projectile.DirectionToSafe(target.Center) * Projectile.scale * 0.0765f;
                if (AllProjectilesByID(Projectile.type).Count() >= 2)
                    force *= 0.56f;

                // Apply difficulty-specific balancing.
                if (CommonCalamityVariables.RevengeanceModeActive)
                    force *= 1.1765f;

                // GFB? Die.
                if (Main.zenithWorld)
                    force *= 3.3f;

                Projectile.velocity += force;

                // Make the black hole go faster if it's moving away from the target.
                if (Vector2.Dot(Projectile.DirectionToSafe(target.Center), Projectile.velocity) < 0f)
                    Projectile.velocity += force * 1.3f;

                // Zip towards the target if they're not moving much.
                if (target.Velocity.Length() <= 4f)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionToSafe(target.Center) * 22f, 0.08f);
            }

            // Enforce a hard limit on the velocity.
            Projectile.velocity = Projectile.velocity.ClampLength(0f, 30f);

            // Create suck energy particles.
            Vector2 energySpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Projectile.width * Main.rand.NextFloat(0.97f, 2.1f);
            Vector2 energyVelocity = (Projectile.Center - energySpawnPosition).RotatedBy(PiOver2) * 0.037f;
            Particles.Add(new()
            {
                Center = energySpawnPosition,
                Velocity = energyVelocity,
                Opacity = 1f,
                DrawColor = Color.Lerp(Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat()), Color.White, NamelessDeitySky.KaleidoscopeInterpolant * 0.9f),
                Lifetime = 30
            });

            // Update all particles.
            Particles.RemoveAll(p => p.Time >= p.Lifetime);
            for (int i = 0; i < Particles.Count; i++)
            {
                var p = Particles[i];
                p.Update(Projectile.Center);
            }

            // Dissipate at the end.
            Projectile.Opacity = InverseLerp(8f, 60f, Lifetime - Time);
            if (Time >= Lifetime)
                Projectile.Kill();

            // Start the loop sound on the first frame.
            BrrrrrSound ??= LoopedSoundManager.CreateNew(NamelessDeityBoss.QuasarSoundLooped_Start, NamelessDeityBoss.QuasarSoundLooped, () =>
            {
                return !Projectile.active;
            });

            // Update the loop sound.
            BrrrrrSound.Update(Projectile.Center);

            // Avoid other quasars.
            float pushForce = 2f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile otherProj = Main.projectile[i];

                // Short circuits to make the loop as fast as possible
                if (!otherProj.active || i == Projectile.whoAmI)
                    continue;

                // If the other projectile is indeed the same owned by the same player and they're too close, nudge them away.
                bool sameProjType = otherProj.type == Projectile.type;
                float taxicabDist = Math.Abs(Projectile.position.X - otherProj.position.X) + Math.Abs(Projectile.position.Y - otherProj.position.Y);
                if (sameProjType && taxicabDist < Projectile.width * 1.458f)
                {
                    if (Projectile.position.X < otherProj.position.X)
                        Projectile.velocity.X -= pushForce;
                    else
                        Projectile.velocity.X += pushForce;

                    // Prevent the momentum gained from the pushed quasars serving as a basis for it to slam so quickly into the player that it basically telefrags them.
                    ReboundCountdown = 27f;
                    otherProj.As<Quasar>().ReboundCountdown = 27f;

                    if (RadialScreenShoveSystem.DistortionTimer <= 0)
                        RadialScreenShoveSystem.Start((Projectile.Center + otherProj.Center) * 0.5f, 30);
                }
            }

            // Register the gravitational lensing quasar as this projectile.
            GravitationalLensingShaderData.Quasar = Projectile;

            Time++;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Create a sucking effect over the black hole.
            float suckPulse = 1f - Main.GlobalTimeWrappedHourly * 4f % 1f;
            float suckRotation = Main.GlobalTimeWrappedHourly * -3f;
            Color suckColor = Color.Wheat * InverseLerpBump(0.05f, 0.25f, 0.67f, 1f, suckPulse) * Projectile.Opacity * 1.4f;
            Main.spriteBatch.Draw(ChromaticBurst, Projectile.Center - Main.screenPosition, null, suckColor, suckRotation, ChromaticBurst.Size() * 0.5f, Vector2.One * suckPulse * 2.6f, 0, 0f);

            // Draw particles.
            DrawParticles();
        }

        public void DrawParticles()
        {
            float energyBaseScale = 1f;

            // Draw energy particles that get sucked into the black hole.
            foreach (EnergySuckParticle particle in Particles)
            {
                float squish = 0.21f;
                float rotation = particle.Velocity.ToRotation();
                Vector2 origin = BloomCircleSmall.Size() * 0.5f;
                Vector2 scale = new(energyBaseScale - energyBaseScale * squish * 0.3f, energyBaseScale * squish);
                Vector2 drawPosition = particle.Center - Main.screenPosition;

                Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, particle.DrawColor * particle.Opacity * Projectile.Opacity * 0.8f, rotation, origin, scale * 0.32f, 0, 0f);
                Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, particle.DrawColor * particle.Opacity * Projectile.Opacity, rotation, origin, scale * 0.27f, 0, 0f);
                Main.spriteBatch.Draw(BloomCircleSmall, drawPosition, null, Color.White * particle.Opacity * Projectile.Opacity * 0.9f, rotation, origin, scale * 0.24f, 0, 0f);
            }
        }

        // Prevent cheap hits if the quasar happens to spawn near a player at first.
        public override bool? CanDamage() => Time >= 48f && Projectile.Opacity >= 0.8f;

        public override void OnKill(int timeLeft)
        {
            NamelessDeityBoss.CreateTwinkle(Projectile.Center, Vector2.One * 2f);
            RadialScreenShoveSystem.Start(Projectile.Center, 72);
        }
    }
}
