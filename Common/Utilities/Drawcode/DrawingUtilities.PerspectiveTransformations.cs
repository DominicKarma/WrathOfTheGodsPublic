using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Calculates perspective matrices for usage by vertex shaders, notably in the context of primitive meshes.
        /// </summary>
        /// <param name="viewMatrix">The view matrix.</param>
        /// <param name="projectionMatrix">The projection matrix.</param>
        public static void CalculatePrimitivePerspectiveMatricies(out Matrix viewMatrix, out Matrix projectionMatrix, bool ui = false)
        {
            Vector2 zoom = Main.GameViewMatrix.Zoom;
            if (ui)
                zoom = Vector2.One;

            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

            // Calculate screen bounds.
            int width = Main.screenWidth;
            int height = Main.screenHeight;

            // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
            viewMatrix = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            // Offset the matrix to the appropriate position.
            viewMatrix *= Matrix.CreateTranslation(0f, -height, 0f);

            // Flip the matrix around 180 degrees.
            viewMatrix *= Matrix.CreateRotationZ(Pi);

            // Account for the inverted gravity effect.
            if (Main.LocalPlayer.gravDir == -1f && !ui)
                viewMatrix *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

            // And account for the current zoom.
            viewMatrix *= zoomScaleMatrix;

            projectionMatrix = Matrix.CreateOrthographicOffCenter(0f, width * zoom.X, 0f, height * zoom.Y, 0f, 1f) * zoomScaleMatrix;
        }

        /// <summary>
        /// Calculates a <see cref="Matrix"/> for the purpose of <see cref="SpriteBatch"/> resets in the context of background/sky drawing.
        /// </summary>
        public static Matrix GetCustomSkyBackgroundMatrix()
        {
            Matrix transformationMatrix = Main.BackgroundViewMatrix.TransformationMatrix;
            Vector3 translationDirection = new(1f, Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);

            transformationMatrix.Translation -= Main.BackgroundViewMatrix.ZoomMatrix.Translation * translationDirection;
            return transformationMatrix;
        }

        /// <summary>
        /// Converts world positions to 0-1 UV values relative to the screen. This is incredibly useful when supplying position data to screen shaders.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        public static Vector2 WorldSpaceToScreenUV(Vector2 worldPosition)
        {
            // Calculate the coordinates relative to the raw screen size. This does not yet account for things like zoom.
            Vector2 baseUV = (worldPosition - Main.screenPosition) / Main.ScreenSize.ToVector2();

            // Once the above normalized coordinates are calculated, apply the game view matrix to the result to ensure that zoom is incorporated into the result.
            // In order to achieve this it is necessary to firstly anchor the coordinates so that <0, 0> is the origin and not <0.5, 0.5>, and then convert back to
            // the original anchor point after the transformation is complete.
            return Vector2.Transform(baseUV - Vector2.One * 0.5f, Main.GameViewMatrix.TransformationMatrix with { M41 = 0f, M42 = 0f }) + Vector2.One * 0.5f;
        }
    }
}
