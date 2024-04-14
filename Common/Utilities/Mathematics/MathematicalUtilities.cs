using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Approximates the derivative of a function at a given point based on a 
        /// </summary>
        /// <param name="fx">The function to take the derivative of.</param>
        /// <param name="x">The value to evaluate the derivative at.</param>
        public static double ApproximateDerivative(this Func<double, double> fx, double x)
        {
            double left = fx(x + 1e-7);
            double right = fx(x - 1e-7);
            return (left + right) * 5e6;
        }

        /// <summary>
        /// Searches for an approximate for a root of a given function.
        /// </summary>
        /// <param name="fx">The function to find the root for.</param>
        /// <param name="initialGuess">The initial guess for what the root could be.</param>
        /// <param name="iterations">The amount of iterations to perform. The higher this is, the more generally accurate the result will be.</param>
        public static double IterativelySearchForRoot(this Func<double, double> fx, double initialGuess, int iterations)
        {
            // This uses the Newton-Raphson method to iteratively get closer and closer to roots of a given function.
            // The exactly formula is as follows:
            // x = x - f(x) / f'(x)
            // In most circumstances repeating the above equation will result in closer and closer approximations to a root.
            // The exact reason as to why this intuitively works can be found at the following video:
            // https://www.youtube.com/watch?v=-RdOwhmqP5s
            double result = initialGuess;
            for (int i = 0; i < iterations; i++)
            {
                double derivative = fx.ApproximateDerivative(result);
                result -= fx(result) / derivative;
            }

            return result;
        }

        /// <summary>
        /// Subdivides a rectangle into frames.
        /// </summary>
        /// <param name="rectangle">The base rectangle.</param>
        /// <param name="horizontalFrames">The amount of horizontal frames to subdivide into.</param>
        /// <param name="verticalFrames">The amount of vertical frames to subdivide into.</param>
        /// <param name="frameX">The index of the X frame.</param>
        /// <param name="frameY">The index of the Y frame.</param>
        public static Rectangle Subdivide(this Rectangle rectangle, int horizontalFrames, int verticalFrames, int frameX, int frameY)
        {
            int width = rectangle.Width / horizontalFrames;
            int height = rectangle.Height / verticalFrames;
            return new Rectangle(rectangle.Left + width * frameX, rectangle.Top + height * frameY, width, height);
        }

        /// <summary>
        /// Calculates perspective matrices for usage by vertex shaders, notably in the context of primitive meshes.
        /// </summary>
        /// <param name="viewMatrix">The view matrix.</param>
        /// <param name="projectionMatrix">The projection matrix.</param>
        public static void CalculatePrimitivePerspectiveMatrices(out Matrix viewMatrix, out Matrix projectionMatrix, bool ui = false)
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
    }
}
