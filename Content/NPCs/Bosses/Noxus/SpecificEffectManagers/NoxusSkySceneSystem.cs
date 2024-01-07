using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Content.NPCs.Bosses.Noxus.FirstPhaseForm;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers
{
    public class NoxusSkySceneSystem : ModSystem
    {
        internal static Main.SceneArea PreviousSceneDetails
        {
            get;
            private set;
        }

        public static float EclipseDarknessInterpolant
        {
            get;
            set;
        }

        public static Vector2 GetSunPosition(Main.SceneArea sceneArea, float dayCompletion)
        {
            float verticalOffsetInterpolant;
            if (dayCompletion < 0.5f)
                verticalOffsetInterpolant = Pow(1f - dayCompletion * 2f, 2f);
            else
                verticalOffsetInterpolant = Pow(dayCompletion - 0.5f, 2f) * 4f;
            Texture2D sunTexture = TextureAssets.Sun.Value;
            int x = (int)(dayCompletion * sceneArea.totalWidth + sunTexture.Width * 2f + dayCompletion * 210f) - 325;
            int y = (int)(sceneArea.bgTopY + verticalOffsetInterpolant * 250f + Main.sunModY + 180f);
            return new(x, y);
        }

        public override void OnModLoad()
        {
            On_Main.DrawSunAndMoon += DrawNoxusInBackgroundHook;
        }

        private void DrawNoxusInBackgroundHook(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
        {
            orig(self, sceneArea, moonColor, sunColor, tempMushroomInfluence);
            PreviousSceneDetails = sceneArea;
            DrawNoxusInBackground(sceneArea);
        }

        public static void DrawNoxusInBackground(Main.SceneArea sceneArea)
        {
            // Make the eclipse darkness effect naturally dissipate to ensure that it goes away even if the checks below are failed.
            EclipseDarknessInterpolant = Clamp(EclipseDarknessInterpolant - 0.04f, 0f, 1f);

            // The eliminative checks below don't apply if in the Noxus World.
            bool inGameMenuNotGeneratingWorld = Main.gameMenu && !WorldGen.generatingWorld;
            if (inGameMenuNotGeneratingWorld || !NoxusWorldManager.Enabled || !Main.dayTime)
            {
                // Don't draw Noxus if he's fucking dead, has fallen from space already, or hasn't started orbiting the planet yet.
                if (WorldSaveSystem.HasDefeatedNoxusEgg || NoxusEggCutsceneSystem.NoxusHasFallenFromSky || !NoxusEggCutsceneSystem.NoxusBeganOrbitingPlanet)
                    return;

                // Don't draw Noxus if he's behind the view position, if on the title screen, or it's night.
                if (CelestialOrbitDetails.NoxusOrbitOffset.Z < 0f || Main.gameMenu || !Main.dayTime)
                    return;
            }

            // Calculate draw values for Noxus.
            Texture2D noxusEggTexture = NoxusEgg.MyTexture.Value;
            Color noxusDrawColor = Color.Lerp(Color.Black * 0.035f, new(64, 64, 64), Pow(EclipseDarknessInterpolant, 0.54f));

            Vector2 sunPosition = GetSunPosition(sceneArea, (float)(Main.time / Main.dayLength));
            Vector2 noxusDrawPosition = new Vector2(CelestialOrbitDetails.NoxusHorizontalOffset, CelestialOrbitDetails.NoxusVerticalOffset) + sceneArea.SceneLocalScreenPositionOffset;
            noxusDrawPosition.Y += sceneArea.bgTopY;

            // In the Noxus World, Noxus is always on top of the sun.
            if (NoxusWorldManager.Enabled)
                noxusDrawPosition = sunPosition - Vector2.UnitY * 0.5f;

            // Make Noxus darker as an indication that he's becoming a silhouette if close to the sun.
            float distanceFromSun = sunPosition.Distance(noxusDrawPosition);
            float silhouetteInterpolant = InverseLerp(85f, 21f, distanceFromSun);
            noxusDrawColor = Color.Lerp(noxusDrawColor, Color.Black, Pow(silhouetteInterpolant, 0.6f) * 0.85f);
            if (silhouetteInterpolant > 0f)
                noxusDrawColor.A = (byte)Lerp(noxusDrawColor.A, 255f, Pow(silhouetteInterpolant, 1.3f));

            // Calculate the eclipse darkness intensity.
            EclipseDarknessInterpolant = InverseLerp(58f, 21f, distanceFromSun);

            // Draw a bloom flare over the sun if an eclipse is happening.
            if (EclipseDarknessInterpolant > 0f)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

                Main.spriteBatch.Draw(CoronaTexture, sunPosition, null, Color.Wheat * EclipseDarknessInterpolant * 0.58f, Main.GlobalTimeWrappedHourly * 0.03f, CoronaTexture.Size() * 0.5f, 0.26f, 0, 0f);
                Main.spriteBatch.Draw(BloomFlare, sunPosition, null, Color.LightGoldenrodYellow * EclipseDarknessInterpolant * 0.5f, Main.GlobalTimeWrappedHourly * 1.2f, BloomFlare.Size() * 0.5f, 0.3f, 0, 0f);
                Main.spriteBatch.Draw(BloomFlare, sunPosition, null, Color.LightGoldenrodYellow * EclipseDarknessInterpolant * 0.4f, Main.GlobalTimeWrappedHourly * -0.92f, BloomFlare.Size() * 0.5f, 0.25f, 0, 0f);
            }

            // Draw the egg with a moderate amount of blur.
            float eggScale = Lerp(0.41f, 0.42f, Pow(EclipseDarknessInterpolant, 0.4f));
            float maxBlurOffset = Lerp(0.4f, 0.6f, EclipseDarknessInterpolant) / noxusEggTexture.Width * 6f;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            // Apply the blur shader.
            var blurShader = ShaderManager.GetShader("HorizontalBlurShader");
            blurShader.TrySetParameter("maxBlurOffset", maxBlurOffset);
            blurShader.Apply();

            Main.spriteBatch.Draw(noxusEggTexture, noxusDrawPosition, null, noxusDrawColor, 0f, noxusEggTexture.Size() * 0.5f, eggScale, 0, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);
        }

        public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
        {
            backgroundColor = Color.Lerp(backgroundColor, new(8, 8, 11), EclipseDarknessInterpolant * 0.96f);
            tileColor = Color.Lerp(tileColor, Color.Black, EclipseDarknessInterpolant * 0.85f);
        }
    }
}
