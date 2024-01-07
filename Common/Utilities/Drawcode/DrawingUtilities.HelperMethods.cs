using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Displays arbitrary text in the game chat with a desired color. This method expects to be called server-side in multiplayer, with the message display packet being sent to all clients from there.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="color">The color of the text.</param>
        public static void BroadcastText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(text, color);
            else if (Main.netMode == NetmodeID.Server)
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
        }

        /// <summary>
        /// Returns a color interpolation similar to <see cref="Color.Lerp(Color, Color, float)"/> that supports multiple colors.
        /// </summary>
        /// <param name="interpolant">The 0-1 incremental value used when interpolating.</param>
        /// <param name="colors">The various colors to interpolate across.</param>
        public static Color MulticolorLerp(float interpolant, params Color[] colors)
        {
            // Ensure that the interpolant is within the valid 0-1 range.
            interpolant %= 0.999f;

            // Determine which two colors should be interpolated between based on which "slice" the interpolant falls between.
            int currentColorIndex = (int)(interpolant * colors.Length);
            Color currentColor = colors[currentColorIndex];
            Color nextColor = colors[(currentColorIndex + 1) % colors.Length];

            // Interpolate between the two colors. The interpolant is scaled such that it's within the 0-1 range relative to the slice.
            return Color.Lerp(currentColor, nextColor, interpolant * colors.Length % 1f);
        }

        /// <summary>
        /// Hue shifts a given color by a desired amount. The hue spectrum is within a 0-1 range.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <param name="hueOffset">The amount to offset the hue by.</param>
        public static Color HueShift(this Color color, float hueOffset)
        {
            // Calculate the Hue, Saturation, and Luminosity values of the color, encoded in a Vector3 of 0-1 numbers.
            Vector3 hsl = Main.rgbToHsl(color);

            // Apply the hue offset.
            hsl.X = (hsl.X + hueOffset).Modulo(1f);

            // Convert the new HSL value back to RGB, maintaining the original alpha value, and return it.
            Color rgb = Main.hslToRgb(hsl) with
            {
                A = color.A
            };
            return rgb;
        }

        /// <summary>
        /// Generates an arbitrary quantity of laser point positions for a projectile. Commonly used when calculating points for primitive-based laserbeams.
        /// </summary>
        /// <param name="projectile">The projectile to calculate positions from.</param>
        /// <param name="samplesCount">The amount of subdivions that should be performed. Larger values are more precise, but also more computationally expensive to use.</param>
        /// <param name="laserLength">The length of the laser. Used for determining the end point of the laser.</param>
        /// <param name="laserDirection">The direction of the laser. By default uses the unit direction of the projectile's velocity.</param>
        public static List<Vector2> GetLaserControlPoints(this Projectile projectile, int samplesCount, float laserLength, Vector2? laserDirection = null)
        {
            // Calculate the start and end of the laser.
            // The resulting list will interpolate between these two values.
            Vector2 start = projectile.Center;
            Vector2 end = start + (laserDirection ?? projectile.velocity.SafeNormalize(Vector2.Zero)) * laserLength;

            // Generate 'samplesCount' evenly spaced control points.
            List<Vector2> controlPoints = new();
            for (int i = 0; i < samplesCount; i++)
                controlPoints.Add(Vector2.Lerp(start, end, i / (float)(samplesCount - 1f)));

            return controlPoints;
        }
    }
}
