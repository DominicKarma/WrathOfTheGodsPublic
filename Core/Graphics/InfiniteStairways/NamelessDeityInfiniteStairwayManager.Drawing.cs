using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Common.Subworlds;
using NoxusBoss.Core.MiscSceneManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public partial class NamelessDeityInfiniteStairwayManager : ModSystem
    {
        private static Asset<Texture2D>[] plantTextures;

        // These members are INTENTIONALLY unsynced. The effect is supposed to be exclusive to individual players.
        public static float Opacity
        {
            get;
            set;
        }

        public static int StairsDirection => -1;

        public static bool StairwayIsVisible => Opacity > 0f;

        public static bool TilesAreUninteractable => Opacity >= 1f;

        private static void LoadPlantTextures()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            plantTextures = new Asset<Texture2D>[]
            {
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/BrimstoneRose"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/FirstFlower1"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/FirstFlower2"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/FirstFlower3"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower1"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower2"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower3"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower4"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower5"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower6"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower7"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower8"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower9"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Flower10"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass1"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass2"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass3"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass4"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass5"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass6"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass7"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Grass8"),
                ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GardenVisionPlants/Starbearer"),
            };
        }

        public override void OnWorldUnload() => Opacity = 0f;

        private int DisableIdleParticlesFromTiles(On_Gore.orig_NewGore_IEntitySource_Vector2_Vector2_int_float orig, IEntitySource source, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            if (StairwayIsVisible)
                return Main.maxDust;

            return orig(source, Position, Velocity, Type, Scale);
        }

        private Vector3 TemporarilyDisableBlackDrawingForLight(On_LightingEngine.orig_GetColor orig, LightingEngine self, int x, int y)
        {
            if (DisableBlackCountdown > 0 || StairwayIsVisible)
                return Vector3.One;

            return orig(self, x, y);
        }

        private void ObfuscatePositionInfo(ILContext context, ManagedILEdit edit)
        {
            int displayTextIndex = 0;
            ILCursor cursor = new(context);

            void replaceText(string searchName)
            {
                if (!cursor.TryGotoNext(MoveType.After, c => c.MatchStloc(displayTextIndex)))
                {
                    edit.LogFailure($"The {searchName} search for the displayText local variable could not be found.");
                    return;
                }

                cursor.Emit(OpCodes.Ldloc, displayTextIndex);
                cursor.EmitDelegate<Func<string, string>>(displayText =>
                {
                    if (StairwayIsVisible || EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                        return "???";

                    return displayText;
                });
                cursor.Emit(OpCodes.Stloc, displayTextIndex);
            }

            // Find the text display string.
            for (int i = 0; i < 2; i++)
            {
                if (!cursor.TryGotoNext(MoveType.Before, c => c.MatchLdstr(out _)))
                {
                    edit.LogFailure($"The {(i == 0 ? "first" : "second")} ldstr opcode could not be found!");
                    return;
                }
            }

            // Store the display string's local index.
            if (!cursor.TryGotoNext(c => c.MatchStloc(out displayTextIndex)))
            {
                edit.LogFailure($"The displayText local index storage could not be found!");
                return;
            }

            // Change text for the watch.
            if (!cursor.TryGotoNext(MoveType.After, c => c.MatchLdsfld<Main>("time")))
            {
                edit.LogFailure($"The Main.time load could not be found!");
                return;
            }

            cursor.Emit(OpCodes.Pop);
            cursor.EmitDelegate(() =>
            {
                if (!StairwayIsVisible || EternalGardenUpdateSystem.WasInSubworldLastUpdateFrame)
                    return Main.time;

                return (double)Main.rand.NextFloat(86400f);
            });

            // Change text for the compass.
            if (!cursor.TryGotoNext(c => c.MatchLdstr("GameUI.CompassEast")))
            {
                edit.LogFailure($"The 'GameUI.CompassEast' string load could not be found!");
                return;
            }

            replaceText("CompassEast");

            // Change text for the depth meter.
            if (!cursor.TryGotoNext(c => c.MatchLdstr("GameUI.LayerUnderground")))
            {
                edit.LogFailure($"The 'GameUI.LayerUnderground' string load could not be found!");
                return;
            }

            replaceText("LayerUnderground");
        }

        // This code is copypasted from an Infernum IL edit I made a while ago. It will work with Infernum's edit, as it only applies an opacity multiplication to colors.
        internal void ObfuscateMap(ILContext il)
        {
            ILCursor cursor = new(il);
            MethodInfo colorFloatMultiply = typeof(Color).GetMethod("op_Multiply", new Type[] { typeof(Color), typeof(float) });
            ConstructorInfo colorConstructor = typeof(Color).GetConstructor(new Type[] { typeof(int), typeof(int), typeof(int), typeof(int) });

            // ==== APPLY EFFECT TO FULLSCREEN MAP =====

            // Find the map background draw method and use it as a hooking reference.
            if (!cursor.TryGotoNext(i => i.MatchCall<Main>("DrawMapFullscreenBackground")))
                return;

            // Go to the next 3 instances of Color.White being loaded and multiply them by the opacity factor.
            for (int i = 0; i < 3; i++)
            {
                if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall<Color>("get_White")))
                    continue;

                cursor.EmitDelegate(() => 1f - Opacity);
                cursor.Emit(OpCodes.Call, colorFloatMultiply);
            }

            // ==== APPLY EFFECT TO MAP RENDER TARGETS =====

            // Move after the map target color is decided, and multiply the result by the opacity factor/add blackness to it.
            if (!cursor.TryGotoNext(i => i.MatchLdfld<Main>("mapTarget")))
                return;
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchNewobj(colorConstructor)))
                return;

            cursor.EmitDelegate((Color c) =>
            {
                if (Main.mapFullscreen)
                    return c * (1f - Opacity);

                return Color.Lerp(c, Color.Black, Opacity);
            });
        }

        public static void ApplyWalkInteractions(Player player)
        {
            int dustCount = 1;
            float dustLingerance = 1.1f;

            // Play step sounds as the player moves on the stairs. They are higher pitched the faster the player is moving.
            // Furthermore, it takes less time for the next potential sound to occur the faster the player is moving.
            float horizontalSpeed = Abs(player.velocity.X);
            if (StepSoundCountdown <= 0)
            {
                float stepSoundPitch = Utils.Remap(horizontalSpeed, 4f, 11f, 0f, 0.55f);
                SoundEngine.PlaySound(StairStepSound with { Pitch = stepSoundPitch });

                // Define the delay for the next step sound.
                StepSoundCountdown = (int)Utils.Remap(horizontalSpeed, 4f, 11f, 30f, 2f);
                dustCount += 5;
                dustLingerance += 0.9f;

                // Add a very tiny screen shake.
                StartShakeAtPoint(player.Bottom, 1f);
            }

            // Create a little bit of fancy dust at the player's feet.
            for (int i = 0; i < dustCount; i++)
            {
                Color dustColor = Main.hslToRgb(Main.rand.NextFloat(0.94f, 1.25f) % 1f, 1f, 0.84f) * Opacity;
                dustColor.A /= 8;

                Vector2 footSpawnPosition = Vector2.Lerp(player.BottomLeft, player.BottomRight, Main.rand.NextFloat());
                Dust light = Dust.NewDustPerfect(footSpawnPosition + Main.rand.NextVector2Circular(3f, 3f), 267, -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f, 4f) * dustLingerance, 0, dustColor);
                light.scale = Opacity * 0.1f;
                light.fadeIn = Main.rand.NextFloat(dustLingerance);
                light.noGravity = true;
                light.noLight = true;
            }
        }

        public static void DrawGardenFlowers(Vector2 gardenEdge)
        {
            UnifiedRandom rng = new(774);

            // Draw a bunch of flowers.
            for (float dx = 100f; dx < 2000f; dx += rng.NextFloat(30f, 60f))
            {
                Texture2D flowerTexture = rng.Next(plantTextures).Value;
                Vector2 flowerDrawPosition = gardenEdge + new Vector2(StairsDirection * dx, -5f);
                if (dx >= 300f && dx <= 500f)
                    flowerDrawPosition.Y -= 4f;
                if (dx >= 650f && dx <= 760f)
                    flowerDrawPosition.Y -= 10f;

                Vector2 flowerOrigin = flowerTexture.Size() * new Vector2(0.5f, 1f);

                float flowerRotation = AperiodicSin(Main.GlobalTimeWrappedHourly * 3f + dx) * 0.14f + 0.11f;
                SpriteEffects flowerDirection = rng.NextBool() ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Main.spriteBatch.Draw(flowerTexture, flowerDrawPosition, null, Color.White * Opacity, flowerRotation, flowerOrigin, 1f, flowerDirection, 0);
            }
        }

        public static void DrawGarden(Vector2 drawOffset, SpriteEffects direction)
        {
            if (GardenStartX == 0f)
                return;

            // Draw flowers behind the garden.
            Vector2 gardenDrawPosition = StairwayPositionFromXPosition(GardenStartX) + drawOffset - Vector2.UnitX * StairsDirection * 24f;
            DrawGardenFlowers(gardenDrawPosition);

            // Draw the garden.
            Texture2D garden = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/EternalGarden").Value;
            Main.spriteBatch.Draw(garden, gardenDrawPosition, null, Color.White * Opacity, 0f, garden.Size() * new Vector2(1f, 0.38f), 0.75f, direction ^ SpriteEffects.FlipHorizontally, 0f);
        }

        public static void DrawStairs(Vector2 drawOffset, SpriteEffects direction)
        {
            // Use additive blending the draw the backglow.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);

            // Acquire textures.
            Texture2D stairTexture = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/InfiniteStairways/DivineStairway").Value;
            Texture2D backglow = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/InfiniteStairways/DivineStairwayBackglowBlur").Value;

            // Draw the stairs' backglow.
            float horizontalDrawStep = WidthPerStep * StepsPerSprite - 1f;
            float opacity = Opacity * InverseLerp(150f, 30f, NamelessDeityInfiniteStairwayTopAnimationManager.AnimationTimer);
            for (float x = -30f; x < Main.screenWidth + 30f; x += horizontalDrawStep)
            {
                Vector2 snappedScreenPosition = (Main.screenPosition / new Vector2(horizontalDrawStep, HeightPerStep)).Floor() * new Vector2(horizontalDrawStep, HeightPerStep);
                Vector2 stairwayDrawPosition = StairwayPositionFromXPosition(snappedScreenPosition.X + x).Floor() + drawOffset;
                if (GardenStartX != 0f && stairwayDrawPosition.X - drawOffset.X < GardenStartX)
                    continue;

                Main.spriteBatch.Draw(backglow, stairwayDrawPosition, null, Color.White * opacity, 0f, backglow.Size() * 0.5f, new Vector2(1.2f, 1.05f), direction, 0f);
            }

            // Return to default blending to draw the stairs above the backglow.
            Main.spriteBatch.ResetToDefault();

            // Draw the stairs.
            for (float x = -30f; x < Main.screenWidth + 30f; x += horizontalDrawStep)
            {
                Vector2 snappedScreenPosition = (Main.screenPosition / new Vector2(horizontalDrawStep, HeightPerStep)).Floor() * new Vector2(horizontalDrawStep, HeightPerStep);
                Vector2 stairwayDrawPosition = StairwayPositionFromXPosition(snappedScreenPosition.X + x).Floor() + drawOffset;
                if (GardenStartX != 0f && stairwayDrawPosition.X - drawOffset.X < GardenStartX)
                    continue;

                Main.spriteBatch.Draw(stairTexture, stairwayDrawPosition, null, (Color.White with { A = 96 }) * opacity, 0f, stairTexture.Size() * 0.5f, 1f, direction, 0f);
            }
        }

        public static void Draw()
        {
            // Prepare the spritebatch for drawing.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin();

            // Draw the sky behind everything if necessary. This invalidates the pesky black overlay effect.
            NamelessDeityInfiniteStairwaySky.DrawBackground(Main.Transform);

            // Determine the direction of the stairs.
            SpriteEffects direction = StairsDirection == 1f ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Calculate positioning variables.
            Vector2 drawOffset = Vector2.UnitY * -12f - Main.screenPosition;

            // Draw the garden.
            DrawGarden(drawOffset, direction);

            // Draw the stairs.
            DrawStairs(drawOffset, direction);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin();
        }
    }
}
