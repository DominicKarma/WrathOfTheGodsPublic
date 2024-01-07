using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.BaseEntities;
using NoxusBoss.Common.DataStructures;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.GlobalItems;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.Projectiles
{
    public class SuperCosmicBeam : BasePrimitiveLaserbeam, IDrawPixelated, IProjOwnedByBoss<NamelessDeityBoss>
    {
        public const string PlayerImmuneTimeOverrideName = "ImmuneTimeOverride";

        public const string DisintegrationTimeName = "DisintegrationTime";

        public const string BeingDisintegratedName = "BeingDisintegrated";

        public static readonly int DefaultLifetime = SecondsToFrames(9f);

        // This laser should be drawn with pixelation, and as such should not be drawn manually via the base projectile.
        public override bool UseStandardDrawing => true;

        public override int LaserPointCount => 45;

        public override float LaserExtendSpeedInterpolant => 0.15f;

        public override float MaxLaserLength => 9400f;

        public override ManagedShader LaserShader => ShaderManager.GetShader("NamelessDeityCosmicLaserShader");

        public override void SetStaticDefaults()
        {
            // Since this laserbeam is super big, ensure that it doesn't get pruned based on distance from the camera.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 20000;

            // Register an effect for the player that handles the decreased i-frames, similar to Sans' fight.
            NoxusPlayer.PostUpdateEvent += HandleIFrameOverride;

            // Register an effect for the player that keeps track of how long they've been disintegrated.
            NoxusPlayer.PostUpdateEvent += HandleDisintegrationTimer;
        }

        private static void HandleIFrameOverride(NoxusPlayer p)
        {
            Referenced<int> immuneTimeOverride = p.GetValueRef<int>(PlayerImmuneTimeOverrideName);
            if (immuneTimeOverride != 0)
            {
                p.Player.immuneTime = immuneTimeOverride;
                immuneTimeOverride.Value = 0;
            }
        }

        private static void HandleDisintegrationTimer(NoxusPlayer p)
        {
            Referenced<int> disintegrationTime = p.GetValueRef<int>(DisintegrationTimeName);
            Referenced<bool> beingDisintegrated = p.GetValueRef<bool>(BeingDisintegratedName);
            disintegrationTime.Value = Utils.Clamp(disintegrationTime + (beingDisintegrated ? 1 : -90), 0, 90);
            beingDisintegrated.Value = false;

            if (Main.myPlayer == p.Player.whoAmI && disintegrationTime == 1)
                SoundEngine.PlaySound(NamelessDeityBoss.CosmicLaserObliterationSound with { MaxInstances = 1, SoundLimitBehavior = SoundLimitBehavior.IgnoreNew });
        }

        public override void SetDefaults()
        {
            Projectile.width = 540;
            Projectile.height = 540;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = DefaultLifetime;
            Projectile.Opacity = 0f;
        }

        // This uses PreAI instead of PostAI to ensure that the AI hook uses the correct, updated velocity when deciding Projectile.rotation.
        public override bool PreAI()
        {
            // Check if Nameless is present. If he isn't, disappear immediately.
            if (NamelessDeityBoss.Myself is null)
            {
                Projectile.Kill();
                return false;
            }

            // Stick to Nameless.
            Projectile.Center = NamelessDeityBoss.Myself.Center + Projectile.velocity * 320f;

            // Inherit the direction of the laser from Nameless' direction angle AI value.
            Projectile.velocity = NamelessDeityBoss.Myself.ai[2].ToRotationVector2();

            // Grow at the start of the laser's lifetime and shrink again at the end of it.
            Projectile.scale = InverseLerp(0f, 12f, Time) * InverseLerp(20f, 45f, Projectile.timeLeft);

            // Rapidly fade in.
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.1f, 0f, 1f);

            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Give a short time window before the laser starts doing damage, to prevent cheap hits.
            if (Time <= 5f)
                return false;

            return base.Colliding(projHitbox, targetHitbox);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Allow the target to be hit multiple times in rapid succession.
            target.GetValueRef<int>(PlayerImmuneTimeOverrideName).Value = 6;

            // Release on-hit particles.
            float particleIntensity = 1f - target.statLife / (float)target.statLifeMax2;
            float particleAppearInterpolant = InverseLerp(0.02f, 0.1f, particleIntensity);
            float deathFadeOut = InverseLerp(0.49f, 0.95f, particleIntensity);
            for (int i = 0; i < particleAppearInterpolant * (1f - deathFadeOut) * 11f + 4f; i++)
            {
                if (Main.rand.NextFloat() < deathFadeOut - 0.3f)
                    continue;

                Dust light = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Square(-25f, 25f), 264);
                light.velocity = Projectile.DirectionToSafe(target.Center).RotatedByRandom(0.72f) * Main.rand.NextFloat(1.2f, 8f);
                light.color = Color.Lerp(Color.Cyan, Color.Fuchsia, Sin01(Main.GlobalTimeWrappedHourly * 10f + i * 0.2f)) * particleAppearInterpolant * (1f - deathFadeOut);
                light.scale = Main.rand.NextFloat(0.5f, 1.8f) * Lerp(1f, 0.1f, particleIntensity);
                light.fadeIn = 0.7f;
                light.noGravity = true;
            }

            target.immuneAlpha = (int)(particleIntensity * 255);

            ExpandingGreyscaleCircleParticle circle = new(target.Center, Vector2.Zero, new Color(219, 194, 229) * 0.3f, 8, 0.04f);
            circle.Spawn();
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            // Disable the default hit sound since the player will be hit multiple times in rapid succession.
            modifiers.DisableSound();
        }

        public override float LaserWidthFunction(float completionRatio) => Projectile.scale * Projectile.width;

        public override Color LaserColorFunction(float completionRatio) => Color.White * Pow(Projectile.scale, 1.8f) * Projectile.Opacity;

        public override void PrepareLaserShader(ManagedShader laserShader)
        {
            float laserLength = LaserLengthFactor * MaxLaserLength;
            Vector2 playerPosition = Main.LocalPlayer.Center;
            Vector2 laserCenter = Projectile.Center + Projectile.velocity * laserLength * 0.5f;
            float playerU = SignedDistanceToLine(playerPosition, laserCenter, Projectile.velocity) / laserLength;
            float playerV = SignedDistanceToLine(playerPosition, laserCenter, Projectile.velocity.RotatedBy(PiOver2)) / LaserWidthFunction(0.5f);
            Vector2 playerUV = new(playerU * 0.96f, playerV * 0.5f);

            // Mark the player as being disintegrated if their UVs are within that of the laser.
            float disintegrationInterpolant = Clamp(Main.LocalPlayer.GetValueRef<int>(DisintegrationTimeName) / 40f, 0f, 1f);
            if (playerUV.Between(new Vector2(-0.467f, -0.5f), Vector2.One * 0.5f) && !Main.LocalPlayer.dead && !Main.LocalPlayer.ghost)
            {
                Main.LocalPlayer.GetValueRef<bool>(BeingDisintegratedName).Value = true;
                LocalPlayerDrawManager.StopCondition = () => !Main.LocalPlayer.GetValueRef<bool>(BeingDisintegratedName);
                LocalPlayerDrawManager.ShaderDrawAction = () =>
                {
                    float k = InverseLerp(0f, 0.5f, disintegrationInterpolant);

                    ManagedShader animeShader = ShaderManager.GetShader("AnimeObliterationShader");
                    animeShader.TrySetParameter("scatterDirectionBias", Projectile.velocity * Pow(k, 2f) * -20f);
                    animeShader.TrySetParameter("pixelationFactor", Vector2.One * 1.5f / LocalPlayerDrawManager.PlayerTarget.Size());
                    animeShader.TrySetParameter("disintegrationFactor", Pow(k, 1.6f) * 4f);
                    animeShader.TrySetParameter("opacity", InverseLerp(0.75f, 0.5f, disintegrationInterpolant));
                    animeShader.SetTexture(DendriticNoiseZoomedOut, 1);
                    animeShader.Apply();
                };
            }

            // Draw the laser.
            float lengthInterpolant = InverseLerp(0f, 0.2f, disintegrationInterpolant) * 0.9f;
            float heightV = Main.LocalPlayer.height / LaserWidthFunction(0.5f) * 10f;
            float width = InverseLerp(0.066f, 0.27f, disintegrationInterpolant) * InverseLerp(1f, 0.333f, disintegrationInterpolant) * heightV + 0.002f;
            laserShader.TrySetParameter("uStretchReverseFactor", 0.15f);
            laserShader.TrySetParameter("scrollSpeedFactor", NoxusBossConfig.Instance.PhotosensitivityMode ? 0.6f : 1.3f);
            laserShader.TrySetParameter("lightSmashWidthFactor", width);
            laserShader.TrySetParameter("lightSmashLengthFactor", lengthInterpolant);
            laserShader.TrySetParameter("lightSmashLengthOffset", -0.005f);
            laserShader.TrySetParameter("startingLightBrightness", 1f);
            laserShader.TrySetParameter("maxLightTexturingDarkness", 0.6f);
            laserShader.TrySetParameter("lightSmashEdgeNoisePower", 0.4f + width + disintegrationInterpolant * 0.2f);
            laserShader.TrySetParameter("lightSmashOpacity", InverseLerp(0.01f, 0.11f, disintegrationInterpolant) * InverseLerp(1f, 0.9f, disintegrationInterpolant));
            laserShader.TrySetParameter("playerCoords", playerUV);
            laserShader.TrySetParameter("laserDirection", Projectile.velocity);
            laserShader.SetTexture(CosmosTexture, 1, SamplerState.AnisotropicWrap);
            laserShader.SetTexture(FireNoise, 2, SamplerState.AnisotropicWrap);
            laserShader.SetTexture(CrackedNoise, 3, SamplerState.AnisotropicWrap);
            laserShader.SetTexture(VoidTexture, 4, SamplerState.AnisotropicWrap);
            laserShader.SetTexture(SharpNoise, 5, SamplerState.AnisotropicWrap);
            laserShader.SetTexture(FireNoise, 6, SamplerState.AnisotropicWrap);
        }

        public void DrawWithPixelation() { }
    }
}
