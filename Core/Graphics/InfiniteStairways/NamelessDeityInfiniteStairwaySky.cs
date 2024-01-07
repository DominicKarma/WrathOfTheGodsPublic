using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public class NamelessDeityInfiniteStairwayScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player) => NamelessDeityInfiniteStairwayManager.StairwayIsVisible;

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override int Music => NamelessDeityInfiniteStairwayTopAnimationManager.AnimationActive ? MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/NamelessDeityStairTop") : 0;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            string skyKey = "NoxusBoss:InfiniteStairway";
            if (SkyManager.Instance[skyKey] is not null)
            {
                if (isActive)
                    SkyManager.Instance.Activate(skyKey);
                else
                    SkyManager.Instance.Deactivate(skyKey);
            }
        }
    }

    public class NamelessDeityInfiniteStairwaySky : CustomSky
    {
        public class BackgroundSmoke
        {
            public int Time;

            public int Lifetime;

            public float Rotation;

            public Vector2 DrawPosition;

            public Vector2 Velocity;

            public Color SmokeColor;

            public void Update()
            {
                Time++;
                DrawPosition += Velocity;
                Velocity *= 0.983f;
                SmokeColor *= 0.997f;
                Rotation += Velocity.X * 0.01f;
            }
        }

        public static bool IsEffectActive => NamelessDeityInfiniteStairwayManager.StairwayIsVisible;

        public static float Intensity
        {
            get;
            private set;
        }

        public static List<BackgroundSmoke> SmokeParticles
        {
            get;
            private set;
        } = new();

        public static bool AlreadyDrewThisFrame
        {
            get;
            set;
        }

        public override void Update(GameTime gameTime)
        {
            AlreadyDrewThisFrame = false;

            // Make the intensity go up or down based on whether the sky is in use.
            Intensity = Clamp(Intensity + IsEffectActive.ToDirectionInt() * 0.01f, 0f, 1f);

            // Disable ambient sky objects like wyverns and eyes appearing in front of the background.
            if (IsEffectActive)
                SkyManager.Instance["Ambience"].Deactivate();

            UpdateSmokeParticles();
        }

        public static void UpdateSmokeParticles()
        {
            // Randomly emit smoke.
            int smokeReleaseChance = 2;
            if (Main.rand.NextBool(smokeReleaseChance))
            {
                for (int i = 0; i < 8; i++)
                {
                    SmokeParticles.Add(new()
                    {
                        DrawPosition = new Vector2(Main.rand.NextFloat(-400f, Main.screenWidth + 400f), Main.screenHeight + 372f),
                        Velocity = -Vector2.UnitY * Main.rand.NextFloat(5f, 23f) + Main.rand.NextVector2Circular(3f, 3f),
                        SmokeColor = Color.Lerp(Color.Yellow, Color.Wheat, Main.rand.NextFloat(0.5f, 0.85f)) * 0.9f,
                        Rotation = Main.rand.NextFloat(TwoPi),
                        Lifetime = Main.rand.Next(120, 480)
                    });
                }
            }

            // Update smoke particles.
            SmokeParticles.RemoveAll(s => s.Time >= s.Lifetime);
            foreach (BackgroundSmoke smoke in SmokeParticles)
                smoke.Update();
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, Color.White, Intensity * 0.85f);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) { }

        public static void DrawBackground(Matrix matrix)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);

            // Draw the sky overlay.
            int screenWidth = Main.instance.GraphicsDevice.Viewport.Width;
            int screenHeight = Main.instance.GraphicsDevice.Viewport.Height;
            Rectangle screenArea = new(0, -500, screenWidth, screenHeight + 800);
            Texture2D skyTexture = NamelessDeityDimensionSkyGenerator.SkyTexture.Value;

            // Calculate draw colors.
            float nearSurfaceInterpolant = InverseLerp((float)Main.worldSurface * 16f - 4200f, (float)Main.worldSurface * 16f - 3500f, Main.LocalPlayer.Center.Y);
            float localIntensity = Sin01(Main.GlobalTimeWrappedHourly * Intensity * 12f) * Intensity * 0.1f + Intensity;
            Color backgroundColor = Color.Lerp(Color.Black, Color.Wheat, NamelessDeityInfiniteStairwayManager.RunCompletion) * localIntensity;
            backgroundColor = Color.Lerp(backgroundColor, Color.Black, nearSurfaceInterpolant);

            // Draw the base background.
            for (int i = 0; i < 2; i++)
                Main.spriteBatch.Draw(skyTexture, screenArea, backgroundColor * NamelessDeityInfiniteStairwayManager.Opacity);

            // Prepare additive blending for smoke particles.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);

            // Draw all active smoke particles in the background.
            foreach (BackgroundSmoke smoke in SmokeParticles)
                Main.spriteBatch.Draw(FogTexture, smoke.DrawPosition, null, smoke.SmokeColor * Intensity * NamelessDeityInfiniteStairwayManager.RunCompletion * (1f - nearSurfaceInterpolant) * 0.6f, smoke.Rotation, FogTexture.Size() * 0.5f, 2f, 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        }

        public override float GetCloudAlpha() => 1f - Pow(Intensity, 3f);

        public override void Activate(Vector2 position, params object[] args) { }

        public override void Deactivate(params object[] args) { }

        public override void Reset() { }

        public override bool IsActive()
        {
            return IsEffectActive || Intensity > 0f;
        }
    }
}
