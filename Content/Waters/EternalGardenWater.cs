using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.MiscSceneManagers;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Waters
{
    public class EternalGardenWater : ModWaterStyle
    {
        // Copy of LiquidRenderer.LiquidDrawCache, since that's private. Thankfully, MonoMod is capable of interpreting the private struct as this in parameter definitions.
        public struct LiquidDrawCache
        {
            public Rectangle SourceRectangle;

            public Vector2 LiquidOffset;

            public bool IsVisible;

            public float Opacity;

            public byte Type;

            public bool IsSurfaceLiquid;

            public bool HasWall;
        }

        private static int specialLiquidID;

        public static bool FancyWaterEnabled => NoxusBossConfig.Instance.VisualOverlayIntensity >= 0.4f;

        public static bool DrewWaterThisFrame
        {
            get;
            set;
        }

        public static Asset<Texture2D> CosmicTexture
        {
            get;
            private set;
        }

        public override int ChooseWaterfallStyle() => ModContent.Find<ModWaterfallStyle>("NoxusBoss/EternalGardenWaterflow").Slot;

        public override int GetSplashDust() => 33;

        public override int GetDropletGore() => 713;

        public override Color BiomeHairColor() => Color.ForestGreen;

        public override void LightColorMultiplier(ref float r, ref float g, ref float b)
        {
            r = 1.06f;
            g = 1.071f;
            b = 1.075f;
        }

        public override void SetStaticDefaults()
        {
            specialLiquidID = Slot;

            // Load detours and IL edits pertaining to water drawing.
            Main.QueueMainThreadAction(() =>
            {
                On_LiquidRenderer.DrawNormalLiquids += DisableNaturalWaterDrawing;
                On_TileDrawing.DrawPartialLiquid += SlopesMyBeloathed;

                new ManagedILEdit("Draw Eternal Garden Water Details", edit =>
                {
                    IL_Main.DrawLiquid += edit.SubscriptionWrapper;
                }, DrawSpecialWater).Apply();

                new ManagedILEdit("Draw Eternal Garden Water Shader", edit =>
                {
                    IL_Main.DoDraw += edit.SubscriptionWrapper;
                }, DrawCosmicEffectOverWaterWrapper).Apply();
            });

            if (Main.netMode != NetmodeID.Server)
                CosmicTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Waters/EternalGardenWaterCosmos");
        }

        private void DisableNaturalWaterDrawing(On_LiquidRenderer.orig_DrawNormalLiquids orig, LiquidRenderer self, SpriteBatch spriteBatch, Vector2 drawOffset, int waterStyle, float globalAlpha, bool isBackgroundDraw)
        {
            if (waterStyle != Slot || !FancyWaterEnabled)
                orig(self, spriteBatch, drawOffset, waterStyle, globalAlpha, isBackgroundDraw);
        }

        private void SlopesMyBeloathed(On_TileDrawing.orig_DrawPartialLiquid orig, TileDrawing self, bool behindBlocks, Tile tileCache, ref Vector2 position, ref Rectangle liquidSize, int liquidType, ref VertexColors colors)
        {
            if (liquidType == specialLiquidID && DrewWaterThisFrame && FancyWaterEnabled)
            {
                // Calculate position information.
                Vector2 offScreenRange = new(Main.offScreenRange, Main.offScreenRange);
                if (Main.drawToScreen)
                    offScreenRange = Vector2.Zero;
                Vector2 originalPosition = position;
                position += Main.screenPosition - offScreenRange;
                Vector2 drawOffset = offScreenRange - Main.screenPosition;

                // Store useful tile properties as locals.
                int i = (int)(position.X / 16f);
                int j = (int)(position.Y / 16f);
                int slope = (int)tileCache.Slope;

                // Calculate and apply opacity and the vertex colors.
                float opacity = 0.72f;
                if (slope == 0)
                    opacity *= 0.85f;

                Lighting.GetCornerColors(i, j, out VertexColors vertices);
                LiquidRenderer.SetShimmerVertexColors(ref vertices, opacity * 0.65f, i, j);

                vertices.BottomLeftColor *= opacity;
                vertices.BottomRightColor *= opacity;
                vertices.TopLeftColor *= opacity;
                vertices.TopRightColor *= opacity;

                if (tileCache.Slope != SlopeType.Solid)
                {
                    Rectangle offsetSize = liquidSize;
                    offsetSize.X += 18 * (slope - 1);
                    Main.spriteBatch.Draw(TextureAssets.LiquidSlope[specialLiquidID].Value, originalPosition, offsetSize, vertices.TopLeftColor * opacity, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                }
                else if (!TileID.Sets.BlocksWaterDrawingBehindSelf[(int)tileCache.BlockType] || slope == 0)
                {
                    Main.DrawTileInWater(drawOffset, i, j);

                    if (Main.tile[i, j].IsHalfBlock)
                    {
                        liquidSize.Height -= 8;
                        Main.tileBatch.Draw(TextureAssets.Liquid[specialLiquidID].Value, originalPosition + Vector2.UnitY * 8f, liquidSize, vertices, default, 1f, SpriteEffects.None);
                    }
                    else if (Main.tile[i, j].IsHalfBlock)
                        Main.tileBatch.Draw(TextureAssets.Liquid[specialLiquidID].Value, originalPosition, liquidSize, vertices, default, 1f, SpriteEffects.None);
                    else
                        Main.tileBatch.Draw(TextureAssets.Liquid[specialLiquidID].Value, originalPosition, liquidSize, vertices, default, 1f, SpriteEffects.None);
                }
                return;
            }
            orig(self, behindBlocks, tileCache, ref position, ref liquidSize, liquidType, ref colors);
        }

        private void DrawSpecialWater(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            // Collect member information via reflection for the searching process.
            FieldInfo drawCacheField = typeof(LiquidRenderer).GetField("_drawCache", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo animationFrameField = typeof(LiquidRenderer).GetField("_animationFrame", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo parameterlessTileBatchBegin = typeof(TileBatch).GetMethod("Begin", Array.Empty<Type>());

            // Go after the stopwatch start call.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Stopwatch>("Start")))
            {
                edit.LogFailure("The Stopwatch.Start call could not be found.");
                return;
            }

            // Go after the drawOffset initialization.
            int drawOffsetIndex = 0;
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchStloc(out drawOffsetIndex)))
            {
                edit.LogFailure("The drawOffset local variable storage could not be found.");
                return;
            }

            // Provide the following parameters:
            // 1. The draw cache.
            // 2. The instance.
            // 3. The draw offset.
            // 4. The animation frame.
            // 5. Whether the draw is happening in the background.
            // 6. The water style being drawn.
            cursor.EmitDelegate(() => LiquidRenderer.Instance);
            cursor.Emit(OpCodes.Ldfld, drawCacheField);

            cursor.EmitDelegate(() => LiquidRenderer.Instance);

            cursor.Emit(OpCodes.Ldloc, drawOffsetIndex);

            cursor.EmitDelegate(() => LiquidRenderer.Instance);
            cursor.Emit(OpCodes.Ldfld, animationFrameField);

            cursor.Emit(OpCodes.Ldarg_1);

            cursor.Emit(OpCodes.Ldarg_2);

            // Call the draw method.
            cursor.EmitDelegate(DrawSelf);
        }

        private static void DrawCosmicEffectOverWaterWrapper(ILContext context, ManagedILEdit edit)
        {
            ILCursor cursor = new(context);

            // Collect member information via reflection for the searching process.
            MethodInfo whiteGetter = typeof(Color).GetProperty("White").GetMethod;
            MethodInfo basicSpritebatchDraw = typeof(SpriteBatch).GetMethod("Draw", new Type[] { typeof(Texture2D), typeof(Vector2), typeof(Color) });

            // Go after the target field load.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdsfld<Main>("waterTarget")))
            {
                edit.LogFailure("The Main.waterTarget field load could not be found.");
                return;
            }

            // Move after Color.White, with the intent of replacing it with complete transparency if the water type is special.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall(whiteGetter)))
            {
                edit.LogFailure("The Color.White property load could not be found.");
                return;
            }

            cursor.EmitDelegate<Func<Color, Color>>(originalColor =>
            {
                if (Main.waterStyle != specialLiquidID)
                    return originalColor;

                return Color.Transparent;
            });

            // Move after the draw call.
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt(basicSpritebatchDraw)))
            {
                edit.LogFailure("The Main.spriteBatch.Draw(Texture2D, Vector2, Color) call load could not be found.");
                return;
            }

            // Perform the special draw if necessary.
            cursor.EmitDelegate(DrawCosmicEffectOverWater);
        }

        private static void DrawCosmicEffectOverWater()
        {
            // Reset the water render target if it's disposed for any reason. This can happen most notably if the game's screen recently got resized.
            if (Main.waterTarget.IsDisposed)
            {
                int width = Main.instance.GraphicsDevice.PresentationParameters.BackBufferWidth;
                int height = Main.instance.GraphicsDevice.PresentationParameters.BackBufferHeight;
                width += Main.offScreenRange * 2;
                height += Main.offScreenRange * 2;
                Main.waterTarget = new RenderTarget2D(Main.instance.GraphicsDevice, width, height, false, Main.instance.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);
                return;
            }

            Vector2 scenePosition = Main.sceneWaterPos;
            if (Main.waterStyle != specialLiquidID || !DrewWaterThisFrame || !FancyWaterEnabled)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(Main.waterTarget, scenePosition - Main.screenPosition, Color.White);
                Main.spriteBatch.ResetToDefault();

                return;
            }

            // Prepare for shader drawing.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Get and prepare the shader.
            var cosmicShader = ShaderManager.GetShader("CosmicWaterShader");
            cosmicShader.TrySetParameter("screenPosition", -scenePosition);
            cosmicShader.TrySetParameter("targetSize", Main.waterTarget.Size());
            cosmicShader.SetTexture(CosmicTexture.Value, 1);
            cosmicShader.SetTexture(SmudgeNoise, 2);
            cosmicShader.Apply();

            // Draw the water target.
            Main.spriteBatch.Draw(Main.waterTarget, scenePosition - Main.screenPosition, Color.White);

            Main.spriteBatch.ResetToDefault();
        }

        private static void DrawTileInstance(int i, int j, float opacity, Rectangle frame, Rectangle sparkleFrame, Vector2 drawOffset, Vector2 liquidOffset, bool slope)
        {
            // Calculate tile colors.
            Lighting.GetCornerColors(i, j, out VertexColors colors, 1f);
            LiquidRenderer.SetShimmerVertexColors(ref colors, opacity * 0.65f, i, j);

            colors.BottomLeftColor *= opacity;
            colors.BottomRightColor *= opacity;
            colors.TopLeftColor *= opacity;
            colors.TopRightColor *= opacity;

            // Draw the liquid.
            Texture2D texture = slope ? TextureAssets.LiquidSlope[specialLiquidID].Value : LiquidRenderer.Instance._liquidTextures[specialLiquidID].Value;
            Main.tileBatch.Draw(texture, new Vector2(i << 4, j << 4) + drawOffset + liquidOffset, frame, colors, Vector2.Zero, 1f, 0);

            // Draw sparkles.
            bool top = sparkleFrame.X != 16 || sparkleFrame.Y % 80 != 48;
            if ((top || (i + j) % 3 == 0) && !slope)
            {
                sparkleFrame.X += 48;
                sparkleFrame.Y += LiquidRenderer.Instance.GetShimmerFrame(top, i, j) * 80;
                SetSparkleColors(ref colors, opacity * 2f, i, j, top);
                Main.tileBatch.Draw(LiquidRenderer.Instance._liquidTextures[14].Value, new Vector2(i << 4, j << 4) + drawOffset + liquidOffset, sparkleFrame, colors, Vector2.Zero, 1f, 0);
            }
        }

        private static VertexColors SetSparkleColors(ref VertexColors colors, float opacity, int x, int y, bool top)
        {
            colors.BottomLeftColor = GetSparkleColor(top, x, y + 1);
            colors.BottomRightColor = GetSparkleColor(top, x + 1, y + 1);
            colors.TopLeftColor = GetSparkleColor(top, x, y);
            colors.TopRightColor = GetSparkleColor(top, x + 1, y);
            colors.BottomLeftColor *= opacity;
            colors.BottomRightColor *= opacity;
            colors.TopLeftColor *= opacity;
            colors.TopRightColor *= opacity;
            return colors;
        }

        private static Color GetSparkleColor(bool top, float worldPositionX, float worldPositionY)
        {
            // Based partially on the shimmer draw behaviors.
            float hueInterpolant = Sin01(worldPositionX + worldPositionY / 12f + (float)Main.timeForVisualEffects * 0.01f);
            Color color = Color.Lerp(Color.Cyan, Color.Purple, hueInterpolant);
            color.A = 0;
            return new Color(color.ToVector4() * LiquidRenderer.GetShimmerGlitterOpacity(top, worldPositionX, worldPositionY)) * 1.25f;
        }

        private static bool HasNeighborWithLiquid(int x, int y)
        {
            Tile left = ParanoidTileRetrieval(x - 1, y);
            Tile right = ParanoidTileRetrieval(x + 1, y);
            Tile top = ParanoidTileRetrieval(x, y - 1);
            Tile bottom = ParanoidTileRetrieval(x, y + 1);
            return left.LiquidAmount >= 184 || right.LiquidAmount >= 184 || top.LiquidAmount >= 184 || bottom.LiquidAmount >= 184;
        }

        private static unsafe void DrawSelf(LiquidDrawCache[] drawCache, LiquidRenderer self, Vector2 drawOffset, int animationFrame, bool backgroundDraw, int waterStyle)
        {
            float baseOpacity = Main.liquidAlpha[waterStyle];
            Rectangle drawArea = self.GetCachedDrawArea();

            // Do nothing if not drawing the special liquid type.
            if (waterStyle != specialLiquidID || baseOpacity <= 0.001f || backgroundDraw || !FancyWaterEnabled)
                return;

            // Reset the DrewWaterThisFrame value.
            DrewWaterThisFrame = false;

            Main.tileBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.Default, RasterizerState.CullNone);

            fixed (LiquidDrawCache* ptr = &drawCache[0])
            {
                LiquidDrawCache* current = ptr;

                // Go through the entire draw area and check for liquids with the current type ID.
                for (int i = drawArea.X; i < drawArea.X + drawArea.Width; i++)
                {
                    for (int j = drawArea.Y; j < drawArea.Y + drawArea.Height; j++)
                    {
                        Tile t = Framing.GetTileSafely(i, j);
                        bool notSolid = t.Slope != SlopeType.Solid && HasNeighborWithLiquid(i, j);
                        if (current->IsVisible || notSolid)
                        {
                            // Calculate the opacity.
                            float opacity = current->Opacity * baseOpacity * 0.64f;
                            opacity = Math.Min(1f, opacity);

                            // Calculate frame information.
                            Rectangle frame = current->SourceRectangle;
                            if (current->IsSurfaceLiquid && notSolid)
                                frame.Y = 1280;
                            else
                                frame.Y += animationFrame * 80;

                            if (WorldGen.SolidTile(i + 1, j))
                                frame.Width += 2;
                            if (WorldGen.SolidTile(i, j + 1))
                                frame.Height += 4;

                            if (notSolid)
                            {
                                bool left = t.Slope is SlopeType.SlopeUpLeft or SlopeType.SlopeDownLeft;
                                frame = new((int)t.Slope * 16 - (left ? 16 : 14), 0, 16, 16);
                            }

                            Vector2 liquidOffset = current->LiquidOffset;

                            // Draw the tile.
                            DrawTileInstance(i, j, opacity, frame, current->SourceRectangle, drawOffset, liquidOffset, notSolid);
                            DrewWaterThisFrame = true;
                        }
                        current++;
                    }
                }
            }

            Main.tileBatch.End();
        }
    }
}
