using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;

namespace NoxusBoss.Core.Graphics.Primitives
{
    public class PrimitiveTrail
    {
        public readonly struct VertexPosition2DColor : IVertexType
        {
            public readonly Vector2 Position;

            public readonly Color Color;

            public readonly Vector2 TextureCoordinates;

            public VertexDeclaration VertexDeclaration => StaticVertexDeclaration;

            public static readonly VertexDeclaration StaticVertexDeclaration = new(new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            });

            public VertexPosition2DColor(Vector2 position, Color color, Vector2 textureCoordinates)
            {
                Position = position;
                Color = color;
                TextureCoordinates = textureCoordinates;
            }
        }

        public delegate float VertexWidthFunction(float completionRatio);

        public delegate Vector2 VertexOffsetFunction(float completionRatio);

        public delegate Color VertexColorFunction(float completionRatio);

        public VertexWidthFunction WidthFunction;

        public VertexColorFunction ColorFunction;

        public VertexOffsetFunction OffsetFunction;

        public bool UsesSmoothening;

        public ManagedShader SpecialShader;

        public int? ProjectionAreaWidth;

        public int? ProjectionAreaHeight;

        public Vector2? ProjectionZoom;

        public static BasicEffect BaseEffect
        {
            get;
            private set;
        }

        public PrimitiveTrail(VertexWidthFunction widthFunction, VertexColorFunction colorFunction, VertexOffsetFunction offsetFunction = null, bool useSmoothening = true, ManagedShader specialShader = null)
        {
            if (widthFunction is null || colorFunction is null)
                throw new NullReferenceException($"In order to create a primitive trail, a non-null {(widthFunction is null ? "width" : "color")} function must be specified.");
            WidthFunction = widthFunction;
            ColorFunction = colorFunction;
            OffsetFunction = offsetFunction;

            UsesSmoothening = useSmoothening;

            if (specialShader != null)
                SpecialShader = specialShader;

            BaseEffect ??= new BasicEffect(Main.instance.GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = false
            };
            UpdateBaseEffect(out _, out _);
        }

        public void UpdateBaseEffect(out Matrix effectProjection, out Matrix effectView)
        {
            // Screen bounds.
            int height = ProjectionAreaHeight ?? Main.instance.GraphicsDevice.Viewport.Height;

            Vector2 zoom = ProjectionZoom ?? Main.GameViewMatrix.Zoom;
            Matrix zoomScaleMatrix = Matrix.CreateScale(zoom.X, zoom.Y, 1f);

            // Get a matrix that aims towards the Z axis (these calculations are relative to a 2D world).
            effectView = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up);

            // Offset the matrix to the appropriate position.
            effectView *= Matrix.CreateTranslation(0f, -height, 0f);

            // Flip the matrix around 180 degrees.
            effectView *= Matrix.CreateRotationZ(Pi);

            // Account for the inverted gravity effect.
            if (Main.LocalPlayer.gravDir == -1f)
                effectView *= Matrix.CreateScale(1f, -1f, 1f) * Matrix.CreateTranslation(0f, height, 0f);

            // And account for the current zoom.
            effectView *= zoomScaleMatrix;

            effectProjection = Matrix.CreateOrthographicOffCenter(0f, (ProjectionAreaWidth ?? Main.screenWidth) * zoom.X, 0f, (ProjectionAreaHeight ?? Main.screenHeight) * zoom.Y, 0f, 1f) * zoomScaleMatrix;
            BaseEffect.View = effectView;
            BaseEffect.Projection = effectProjection;
        }

        public void UpdatePixelatedBaseEffect(out Matrix effectProjection, out Matrix effectView)
        {
            effectProjection = Matrix.CreateOrthographicOffCenter(0, ProjectionAreaWidth ?? Main.screenWidth, ProjectionAreaHeight ?? Main.screenHeight, 0, -1, 1);
            effectView = Matrix.Identity;
            BaseEffect.Projection = effectProjection;
            BaseEffect.View = effectView;
        }

        public List<Vector2> GetTrailPoints(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints)
        {
            // Don't smoothen the points unless explicitly told do so.
            if (!UsesSmoothening)
            {
                List<Vector2> basePoints = originalPositions.Where(originalPosition => originalPosition != Vector2.Zero).ToList();
                List<Vector2> endPoints = new();

                if (basePoints.Count <= 2)
                    return endPoints;

                // Remap the original positions across a certain length.
                for (int i = 0; i < basePoints.Count; i++)
                {
                    Vector2 offset = generalOffset;
                    if (OffsetFunction != null)
                        offset += OffsetFunction(i / (float)(basePoints.Count - 1f));

                    endPoints.Add(basePoints[i] + offset);
                }
                return endPoints;
            }

            List<Vector2> controlPoints = new();
            for (int i = 0; i < originalPositions.Count(); i++)
            {
                // Don't incorporate points that are zeroed out.
                // They are almost certainly a result of incomplete oldPos arrays.
                if (originalPositions.ElementAt(i) == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)originalPositions.Count();
                Vector2 offset = generalOffset;
                if (OffsetFunction != null)
                    offset += OffsetFunction(completionRatio);
                controlPoints.Add(originalPositions.ElementAt(i) + offset);
            }
            List<Vector2> points = new();

            // Avoid stupid index errors.
            if (controlPoints.Count <= 4)
                return controlPoints;

            for (int j = 0; j < totalTrailPoints; j++)
            {
                float splineInterpolant = j / (float)totalTrailPoints;
                float localSplineInterpolant = splineInterpolant * (controlPoints.Count - 1f) % 1f;
                int localSplineIndex = (int)(splineInterpolant * (controlPoints.Count - 1f));

                Vector2 farLeft;
                Vector2 left = controlPoints[localSplineIndex];
                Vector2 right = controlPoints[localSplineIndex + 1];
                Vector2 farRight;

                // Special case: If the spline attempts to access the previous/next index but the index is already at the very beginning/end, simply
                // cheat a little bit by creating a phantom point that's mirrored from the previous one.
                if (localSplineIndex <= 0)
                {
                    Vector2 mirrored = left * 2f - right;
                    farLeft = mirrored;
                }
                else
                    farLeft = controlPoints[localSplineIndex - 1];

                if (localSplineIndex >= controlPoints.Count - 2)
                {
                    Vector2 mirrored = right * 2f - left;
                    farRight = mirrored;
                }
                else
                    farRight = controlPoints[localSplineIndex + 2];

                points.Add(Vector2.CatmullRom(farLeft, left, right, farRight, localSplineInterpolant));
            }

            // Manually insert the front and end points.
            points.Insert(0, controlPoints.First());
            points.Add(controlPoints.Last());

            return points;
        }

        public VertexPosition2DColor[] GetVerticesFromTrailPoints(List<Vector2> trailPoints, float? directionOverride = null)
        {
            VertexPosition2DColor[] vertices = new VertexPosition2DColor[(trailPoints.Count - 1) * 2];

            for (int i = 0; i < trailPoints.Count - 1; i++)
            {
                float completionRatio = i / (float)trailPoints.Count;
                float widthAtVertex = WidthFunction(completionRatio);
                Color vertexColor = ColorFunction(completionRatio);

                Vector2 currentPosition = trailPoints[i];
                Vector2 positionAhead = trailPoints[i + 1];
                Vector2 directionToAhead = (positionAhead - trailPoints[i]).SafeNormalize(Vector2.Zero);
                if (directionOverride.HasValue)
                    directionToAhead = directionOverride.Value.ToRotationVector2();

                Vector2 leftCurrentTextureCoord = new(completionRatio, 0f);
                Vector2 rightCurrentTextureCoord = new(completionRatio, 1f);

                // Point 90 degrees away from the direction towards the next point, and use it to mark the edges of the rectangle.
                // This doesn't use RotatedBy for the sake of performance (there can potentially be a lot of trail points).
                Vector2 sideDirection = new(-directionToAhead.Y, directionToAhead.X);

                // What this is doing, at its core, is defining a rectangle based on two triangles.
                // These triangles are defined based on the width of the strip at that point.
                // The resulting rectangles combined are what make the trail itself.
                vertices[i * 2] = new(currentPosition - sideDirection * widthAtVertex, vertexColor, leftCurrentTextureCoord);
                vertices[i * 2 + 1] = new(currentPosition + sideDirection * widthAtVertex, vertexColor, rightCurrentTextureCoord);
            }

            return vertices;
        }

        public static short[] GetIndicesFromTrailPoints(int pointCount)
        {
            // What this is doing is basically representing each point on the vertices list as
            // indices. These indices should come together to create a tiny rectangle that acts
            // as a segment on the trail. This is achieved here by splitting the indices (or rather, points)
            // into 2 triangles, which requires 6 points.
            // The logic here basically determines which indices are connected together.
            int totalIndices = (pointCount - 1) * 6;
            short[] indices = new short[totalIndices];

            for (int i = 0; i < pointCount - 2; i++)
            {
                int startingTriangleIndex = i * 6;
                int connectToIndex = i * 2;
                indices[startingTriangleIndex] = (short)connectToIndex;
                indices[startingTriangleIndex + 1] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 2] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 3] = (short)(connectToIndex + 2);
                indices[startingTriangleIndex + 4] = (short)(connectToIndex + 1);
                indices[startingTriangleIndex + 5] = (short)(connectToIndex + 3);
            }

            return indices;
        }

        /// <summary>
        /// Call this to draw primitives to the base RenderTarget.
        /// </summary>
        /// <param name="originalPositions"></param>
        /// <param name="generalOffset"></param>
        /// <param name="totalTrailPoints"></param>
        /// <param name="directionOverride"></param>
        public void Draw(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, float? directionOverride = null) => DrawPrims(originalPositions, generalOffset, totalTrailPoints, false, directionOverride);

        private void DrawPrims(IEnumerable<Vector2> originalPositions, Vector2 generalOffset, int totalTrailPoints, bool pixelated, float? directionOverride = null)
        {
            if (originalPositions.Count() <= 2)
                return;

            originalPositions = originalPositions.Where(p => p != Vector2.Zero);
            List<Vector2> trailPoints = GetTrailPoints(originalPositions, generalOffset, totalTrailPoints);

            // A trail with only one point or less has nothing to connect to, and therefore, can't make a trail.
            if (trailPoints.Count <= 2)
                return;

            // If the trail point has any NaN positions, don't draw anything.
            if (trailPoints.Any(point => point.HasNaNs()))
                return;

            // If the trail points are all equal, don't draw anything.
            if (trailPoints.All(point => point == trailPoints[0]))
                return;

            DrawPrimsFromVertexData(GetVerticesFromTrailPoints(trailPoints, directionOverride), GetIndicesFromTrailPoints(trailPoints.Count), pixelated);
        }

        internal void DrawPrimsFromVertexData(VertexPosition2DColor[] vertices, short[] triangleIndices, bool pixelated)
        {
            if (triangleIndices.Length % 6 != 0 || vertices.Length <= 3)
                return;

            Matrix projection;
            Matrix view;

            if (pixelated)
                UpdatePixelatedBaseEffect(out projection, out view);
            else
                UpdateBaseEffect(out projection, out view);

            Main.instance.GraphicsDevice.RasterizerState = CullOnlyScreen;
            Main.instance.GraphicsDevice.ScissorRectangle = new(0, 0, Main.screenWidth, Main.screenHeight);

            if (SpecialShader != null)
            {
                SpecialShader.TrySetParameter("uWorldViewProjection", view * projection);
                SpecialShader.Apply();
            }
            else
                BaseEffect.CurrentTechnique.Passes[0].Apply();

            Main.instance.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices.ToArray(), 0, vertices.Length, triangleIndices, 0, triangleIndices.Length / 3);
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}
