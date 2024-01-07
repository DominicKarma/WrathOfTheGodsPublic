using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class DivineMonolithGalaxySystem : ModSystem
    {
        public class Galaxy
        {
            public int Variant;

            public int Time;

            public int Lifetime;

            public bool Accelerates;

            public float Eccentricity;

            public float Rotation;

            public float SpinRotation;

            public float GeneralScale;

            public Color GeneralColor;

            public Vector2 Center;

            public Vector2 Velocity;

            public float LifetimeCompletion => Time / (float)Lifetime;

            public float Scale => InverseLerp(0f, 6f, Time) * Lerp(1f, 1.5f, LifetimeCompletion) * GeneralScale;

            public float Opacity => InverseLerp(0f, 9f, Time) * InverseLerp(1f, 0.4f, Pow(LifetimeCompletion, 0.6f));

            public void Update()
            {
                Time++;
                Center += Velocity;
                Velocity *= Accelerates ? 1.04f : 0.91f;
                Rotation += Velocity.X * 0.003f;
                SpinRotation += Velocity.X * 0.04f;

                int dustCount = (int)Remap(Velocity.Length(), 4f, 0.1f, 1f, 5f) + (int)Lerp(1f, 19f, Pow(LifetimeCompletion, 3f));

                for (int i = 0; i < dustCount; i++)
                {
                    if (Main.rand.NextBool())
                        continue;

                    Color dustColor = Main.hslToRgb(Main.rand.NextFloat(0.94f, 1.25f) % 1f, 1f, 0.8f) * Opacity;
                    dustColor.A /= 8;

                    Vector2 offsetScale = new Vector2(1.2f, 1.2f - Eccentricity) * (Scale + Pow(LifetimeCompletion, 2f) * 6f);
                    Dust light = Dust.NewDustPerfect(Center + Main.rand.NextVector2Circular(27f, 27f) * offsetScale + Velocity, 267, Velocity.ClampLength(0f, 2f) * -2.6f, 0, dustColor);
                    light.scale = Opacity * 0.2f;
                    light.fadeIn = Main.rand.NextFloat(1.3f);
                    light.noGravity = true;
                    light.noLight = true;
                }
            }

            public static void CreateNew(Vector2 center, Vector2 velocity, Color color, int lifetime, float rotation, float scale)
            {
                ActiveGalaxies.Add(new()
                {
                    Center = center,
                    Velocity = velocity,
                    GeneralColor = color,
                    Lifetime = lifetime,
                    Rotation = rotation,
                    GeneralScale = scale,
                    Eccentricity = Main.rand.NextFloat(),
                    Variant = Main.rand.Next(9)
                });
            }
        }

        public static readonly List<Galaxy> ActiveGalaxies = new();

        public override void OnModLoad()
        {
            On_Main.DrawProjectiles += DrawGalaxies;
        }

        public override void OnWorldLoad() => ActiveGalaxies.Clear();

        public override void OnWorldUnload() => ActiveGalaxies.Clear();

        private void DrawGalaxies(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            // Draw and update all galaxies.
            if (ActiveGalaxies.Any())
            {
                UpdateGalaxies();
                ActiveGalaxies.RemoveAll(g => g.Time >= g.Lifetime);

                // Draw galaxies.
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                var galaxyShader = ShaderManager.GetShader("GalaxyShader");
                foreach (Galaxy g in ActiveGalaxies)
                {
                    Matrix flatScale = new()
                    {
                        M11 = Lerp(1f, 2.3f, g.Eccentricity),
                        M12 = 0f,
                        M21 = 0f,
                        M22 = 1f
                    };
                    Matrix spinInPlaceRotation = Matrix.CreateRotationZ(g.SpinRotation);
                    Matrix orientationRotation = Matrix.CreateRotationZ(g.Rotation);
                    galaxyShader.TrySetParameter("transformation", orientationRotation * flatScale * spinInPlaceRotation);
                    galaxyShader.Apply();

                    // Draw the main galaxy.
                    float galaxyScale = g.Scale * 1.56f + Pow(g.LifetimeCompletion, 1.7f) * 19f;
                    Texture2D galaxyTexture = GalaxyTextures[g.Variant];
                    Vector2 galaxyDrawPosition = g.Center - Main.screenPosition;
                    Main.spriteBatch.Draw(galaxyTexture, galaxyDrawPosition, null, g.GeneralColor * g.Opacity, 0f, galaxyTexture.Size() * 0.5f, galaxyScale, 0, 0f);

                    // Draw a secondary galaxy on top with some color contrast.
                    Color secondaryGalaxyColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.6f + g.LifetimeCompletion * 0.3f) % 1f, 0.8f, 0.6f) * 0.4f;
                    Main.spriteBatch.Draw(galaxyTexture, galaxyDrawPosition, null, secondaryGalaxyColor * g.Opacity * 0.6f, 0f, galaxyTexture.Size() * 0.5f, galaxyScale * 1.1f, 0, 0f);
                }

                Main.spriteBatch.End();
            }
        }

        public static void UpdateGalaxies()
        {
            if (Main.gamePaused)
                return;

            foreach (Galaxy g in ActiveGalaxies)
                g.Update();
        }
    }
}
