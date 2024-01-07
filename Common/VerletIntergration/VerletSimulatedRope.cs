using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;

namespace NoxusBoss.Common.VerletIntergration
{
    /// <summary>
    /// Represents a collection of verlet points as a rope.
    /// </summary>
    public class VerletSimulatedRope
    {
        /// <summary>
        /// The ideal overall length of the rope in pixels.
        /// </summary>
        public float IdealRopeLength;

        /// <summary>
        /// The true length of the rope in pixels.
        /// </summary>
        public float RopeLength
        {
            get
            {
                float length = 0f;

                for (int i = 1; i < Rope.Count; i++)
                    length += Rope[i].Position.Distance(Rope[i - 1].Position);

                return length;
            }
        }

        /// <summary>
        /// The primitive drawer responsible for rendering the rope.
        /// </summary>
        public PrimitiveTrail RopeDrawer;

        /// <summary>
        /// The list of rope segments.
        /// </summary>
        public List<VerletSimulatedSegment> Rope;

        public VerletSimulatedRope(Vector2 position, Vector2 velocity, int totalPoints, float length)
        {
            Rope ??= new();

            IdealRopeLength = length;
            for (int i = 0; i < totalPoints; i++)
                Rope?.Add(new(position + Vector2.UnitY * i, velocity, i == 0));
        }

        /// <summary>
        /// Updates the vine, locking its first segment in place at a desired position to prevent it from falling forever.
        /// </summary>
        /// <param name="topPosition">Where the first rope segment should be locked.</param>
        public void Update(Vector2 topPosition, float gravity)
        {
            Rope[0].Position = topPosition;
            Rope[0].Locked = true;
            VerletSimulations.TileCollisionVerletSimulation(Rope, IdealRopeLength / Rope.Count, 10, gravity);
        }

        /// <summary>
        /// Draws the rope with a texture projected on top of it.
        /// </summary>
        /// <param name="projection">The texture the project onto the rope.</param>
        /// <param name="drawOffset">The offset for drawn rope points. This typically should be a world -> screen space offset.</param>
        /// <param name="flipHorizontally">Whether the texture should be flipped on the projection.</param>
        /// <param name="colorFunction">The color factor function for the rope strip.</param>
        /// <param name="projectionWidth">The area width upon which primitive screen space -> UV space projections should occur.</param>
        /// <param name="projectionHeight">The area height upon which primitive screen space -> UV space projections should occur.</param>
        public void DrawProjection(Texture2D projection, Vector2 drawOffset, bool flipHorizontally, Func<float, Color> colorFunction, int? projectionWidth = null, int? projectionHeight = null)
        {
            // Initialize the rope drawer primitive.
            var projectionShader = ShaderManager.GetShader("PrimitiveProjection");
            RopeDrawer = new(_ => projection.Width, new(colorFunction), null, true, projectionShader)
            {
                ProjectionAreaWidth = projectionWidth,
                ProjectionAreaHeight = projectionHeight
            };

            Main.instance.GraphicsDevice.Textures[1] = projection;
            Main.instance.GraphicsDevice.SamplerStates[1] = SamplerState.AnisotropicClamp;
            Main.instance.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

            projectionShader.TrySetParameter("horizontalFlip", flipHorizontally);
            projectionShader.TrySetParameter("heightRatio", projection.Height / (float)projection.Width);
            projectionShader.TrySetParameter("lengthRatio", RopeLength / IdealRopeLength * 1.3f);

            RopeDrawer.Draw(Rope.Select(r => r.Position), drawOffset, 24);
        }
    }
}
