using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Draws a projectile as a series of afterimages. The first of these afterimages is centered on the center of the projectile's hitbox.<br />
        /// This function is guaranteed to draw the projectile itself, even if it has no afterimages and/or the Afterimages config option is turned off.
        /// </summary>
        /// <param name="proj">The projectile to be drawn.</param>
        /// <param name="mode">The type of afterimage drawing code to use. Vanilla Terraria has three options: 0, 1, and 2.</param>
        /// <param name="lightColor">The light color to use for the afterimages.</param>
        /// <param name="typeOneIncrement">If mode 1 is used, this controls the loop increment. Set it to more than 1 to skip afterimages.</param>
        /// <param name="minScale">The minimum scaling factor across the afterimages. Defaults to 1, but values below that allow for afterimages to "shrink" as they get smaller.</param>
        /// <param name="positionClumpInterpolant">An interpolant that determines how clumped afterimages are to the original position via Vector2.Lerp(afterimagePosition, Projectile.Center, positionClumpInterpolant).</param>
        /// <param name="texture">The texture to draw. Set to <b>null</b> to draw the projectile's own loaded texture.</param>
        /// <param name="drawCentered">If <b>false</b>, the afterimages will be centered on the projectile's position instead of its own center.</param>
        public static void DrawAfterimagesCentered(Projectile proj, int mode, Color lightColor, int typeOneIncrement = 1, int? afterimageCountOverride = null, float minScale = 1f, float positionClumpInterpolant = 0f, Texture2D texture = null, bool drawCentered = true)
        {
            // Use the projectile's default texture if nothing is explicitly supplied.
            texture ??= TextureAssets.Projectile[proj.type].Value;

            // Calculate frame information for the projectile.
            int frameHeight = texture.Height / Main.projFrames[proj.type];
            int frameY = frameHeight * proj.frame;
            Rectangle rectangle = new(0, frameY, texture.Width, frameHeight);

            // Calculate the projectile's origin, rotation, and scale.
            Vector2 origin = rectangle.Size() * 0.5f;
            float rotation = proj.rotation;

            // Calculate the direction of the projectile as a SpriteEffects instance.
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (proj.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            // If no afterimages are drawn due to an invalid mode being specified, ensure the projectile itself is drawn anyway at the end of this method.
            bool failedToDrawAfterimages = false;

            // Determine whether afterimages should be drawn at all.
            bool afterimagesArePermitted = GetFromCalamityConfig("Afterimages", true);
            if (afterimagesArePermitted)
            {
                Vector2 centerOffset = drawCentered ? proj.Size * 0.5f : Vector2.Zero;
                switch (mode)
                {
                    // Standard afterimages. No customizable features other than total afterimage count.
                    // Type 0 afterimages linearly scale down from 100% to 0% opacity. Their color and lighting is equal to the main projectile's.
                    case 0:
                        int afterimageCount = afterimageCountOverride ?? proj.oldPos.Length;
                        for (int i = afterimageCount - 1; i >= 0; i--)
                        {
                            float scale = proj.scale * Lerp(1f, minScale, 1f - (afterimageCount - i) / (float)afterimageCount);
                            Vector2 drawPos = Vector2.Lerp(proj.oldPos[i] + centerOffset, proj.Center, positionClumpInterpolant) - Main.screenPosition + Vector2.UnitY * proj.gfxOffY;
                            Color color = proj.GetAlpha(lightColor) * ((proj.oldPos.Length - i) / (float)proj.oldPos.Length);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, rotation, origin, scale, spriteEffects, 0f);
                        }
                        break;

                    // Paladin's Hammer style afterimages. Can be optionally spaced out further by using the typeOneDistanceMultiplier variable.
                    // Type 1 afterimages linearly scale down from 66% to 0% opacity. They otherwise do not differ from type 0.
                    case 1:
                        // Safety check: the loop must increment
                        int increment = Math.Max(1, typeOneIncrement);
                        Color drawColor = proj.GetAlpha(lightColor);
                        afterimageCount = afterimageCountOverride ?? ProjectileID.Sets.TrailCacheLength[proj.type];
                        int i2 = afterimageCount - 1;
                        while (i2 >= 0)
                        {
                            float scale = proj.scale * Lerp(1f, minScale, 1f - (afterimageCount - i2) / (float)afterimageCount);
                            Vector2 drawPos = Vector2.Lerp(proj.oldPos[i2] + centerOffset, proj.Center, positionClumpInterpolant) - Main.screenPosition + Vector2.UnitY * proj.gfxOffY;
                            if (i2 > 0)
                            {
                                float colorMult = afterimageCount - i2;
                                drawColor *= colorMult / (afterimageCount * 1.5f);
                            }
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), drawColor, rotation, origin, scale, spriteEffects, 0f);
                            i2 -= increment;
                        }
                        break;

                    // Standard afterimages with rotation. No customizable features other than total afterimage count.
                    // Type 2 afterimages linearly scale down from 100% to 0% opacity. Their color and lighting is equal to the main projectile's.
                    case 2:
                        afterimageCount = afterimageCountOverride ?? proj.oldPos.Length;
                        for (int i = afterimageCount - 1; i >= 0; i--)
                        {
                            float afterimageRot = proj.oldRot[i];
                            float scale = proj.scale * Lerp(1f, minScale, 1f - (afterimageCount - i) / (float)afterimageCount);
                            SpriteEffects sfxForThisAfterimage = proj.oldSpriteDirection[i] == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                            Vector2 drawPos = Vector2.Lerp(proj.oldPos[i] + centerOffset, proj.Center, positionClumpInterpolant) - Main.screenPosition + Vector2.UnitY * proj.gfxOffY;
                            Color color = proj.GetAlpha(lightColor) * ((afterimageCount - i) / (float)afterimageCount);
                            Main.spriteBatch.Draw(texture, drawPos, new Rectangle?(rectangle), color, afterimageRot, origin, scale, sfxForThisAfterimage, 0f);
                        }
                        break;

                    default:
                        failedToDrawAfterimages = true;
                        break;
                }
            }

            // Draw the projectile itself. Only do this if no afterimages are drawn because afterimage 0 is the projectile itself.
            if (!afterimagesArePermitted || ProjectileID.Sets.TrailCacheLength[proj.type] <= 0 || failedToDrawAfterimages)
            {
                Vector2 startPos = drawCentered ? proj.Center : proj.position;
                Main.spriteBatch.Draw(texture, startPos - Main.screenPosition + new Vector2(0f, proj.gfxOffY), rectangle, proj.GetAlpha(lightColor), rotation, origin, proj.scale, spriteEffects, 0f);
            }
        }

        /// <summary>
        /// Generates special tooltip text for an item when they're holding the <see cref="Keys.LeftShift"/> button. Notably used for lore items.
        /// </summary>
        /// <param name="tooltips">The original tooltips.</param>
        /// <param name="holdShiftTooltips">The tooltips to display when holding shift.</param>
        /// <param name="hideNormalTooltip">Whether the original tooltips should be hidden when holding shift. Defaults to false.</param>
        public static void DrawHeldShiftTooltip(List<TooltipLine> tooltips, TooltipLine[] holdShiftTooltips, bool hideNormalTooltip = false)
        {
            // Do not override anything if the Left Shift key is not being held.
            if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                return;

            // Acquire base tooltip data.
            int firstTooltipIndex = -1;
            int lastTooltipIndex = -1;
            int standardTooltipCount = 0;
            for (int i = 0; i < tooltips.Count; i++)
            {
                if (tooltips[i].Name.StartsWith("Tooltip"))
                {
                    if (firstTooltipIndex == -1)
                    {
                        firstTooltipIndex = i;
                    }
                    lastTooltipIndex = i;
                    standardTooltipCount++;
                }
            }

            // Replace tooltips.
            if (firstTooltipIndex != -1)
            {
                if (hideNormalTooltip)
                {
                    tooltips.RemoveRange(firstTooltipIndex, standardTooltipCount);
                    lastTooltipIndex -= standardTooltipCount;
                }
                tooltips.InsertRange(lastTooltipIndex + 1, holdShiftTooltips);
            }
        }

        /// <summary>
        /// Draws a simple bloom line from a starting point to an ending point. Positional parameters are expected to be in world position.
        /// <br></br>
        /// This method expects to be drawn with <see cref="BlendState.Additive"/>.
        /// </summary>
        /// <param name="spriteBatch">The sprite batch to draw the line with.</param>
        /// <param name="start">The world position of the start of the line.</param>
        /// <param name="end">The world position of the end of the line.</param>
        /// <param name="color">The color of the line.</param>
        /// <param name="width">How wide the line should be.</param>
        public static void DrawBloomLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            // Draw nothing if the start and end are equal, to prevent division by 0 problems.
            if (start == end)
                return;

            start -= Main.screenPosition;
            end -= Main.screenPosition;

            float rotation = (end - start).ToRotation() + PiOver2;
            Vector2 scale = new Vector2(width, Vector2.Distance(start, end)) / BloomLineTexture.Size();
            Vector2 origin = new(BloomLineTexture.Width * 0.5f, BloomLineTexture.Height);

            spriteBatch.Draw(BloomLineTexture, start, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
