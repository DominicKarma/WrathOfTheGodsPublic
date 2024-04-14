using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Tools.Utilities.Mathematics;
using NoxusBoss.Common.Utilities;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.Configuration;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class ScreenShatterSystem : ModSystem
    {
        public class ScreenTriangleShard
        {
            public float RotationX;

            public float RotationY;

            public float RotationZ;

            public Vector3 RotationalAxis;

            public Vector2 ScreenCoord1;

            public Vector2 ScreenCoord2;

            public Vector2 ScreenCoord3;

            public Vector2 DrawPosition;

            public Vector2 Velocity;

            public void Update()
            {
                float angularSlowdownInterpolant = InverseLerp(0.112f, 1f, Velocity.Length());
                RotationX += angularSlowdownInterpolant * RotationalAxis.X;
                RotationY += angularSlowdownInterpolant * RotationalAxis.Y;
                RotationZ += angularSlowdownInterpolant * RotationalAxis.Z;
                Velocity *= 0.91f;
                DrawPosition += InverseLerp(0.97f, 0.55f, ShardOpacity) * Velocity;
            }

            public ScreenTriangleShard(Vector2 a, Vector2 b, Vector2 c, Vector2 drawPosition)
            {
                ScreenCoord1 = a;
                ScreenCoord2 = b;
                ScreenCoord3 = c;
                DrawPosition = drawPosition;
                Velocity = (DrawPosition - ShatterFocalPoint).RotatedByRandom(0.23f) * Main.rand.NextFloat(0.04f, 0.06f) + Main.rand.NextVector2CircularEdge(1.2f, 1.2f);
                RotationalAxis = new(Main.rand.NextFloatDirection() * 0.06f, Main.rand.NextFloatDirection() * 0.06f, Main.rand.NextFloatDirection() * 0.03f);
            }
        }

        private static LineSegment[] manualSliceLines;

        private static bool useNamelessDeityGameBreakSound;

        public static int SliceUpdatesPerFrame
        {
            get;
            set;
        }

        public static bool ShouldCreateSnapshot
        {
            get;
            private set;
        }

        public static float ShardOpacity
        {
            get;
            private set;
        }

        public static Vector2 ShatterFocalPoint
        {
            get;
            private set;
        }

        public static BasicEffect DrawShader
        {
            get;
            private set;
        }

        public static ManagedRenderTarget ContentsBeforeShattering
        {
            get;
            private set;
        }

        public static readonly List<ScreenTriangleShard> screenTriangles = new();

        public override void Load()
        {
            Main.OnPostDraw += DrawShatterEffect;
            Main.QueueMainThreadAction(() =>
            {
                if (Main.netMode == NetmodeID.Server)
                    return;

                ContentsBeforeShattering = new(false, ManagedRenderTarget.CreateScreenSizedTarget);
                DrawShader = new(Main.instance.GraphicsDevice)
                {
                    TextureEnabled = true,
                    VertexColorEnabled = true
                };
            });
        }

        public override void Unload()
        {
            Main.OnPostDraw -= DrawShatterEffect;
            Main.QueueMainThreadAction(() =>
            {
                if (Main.netMode == NetmodeID.Server)
                    return;

                DrawShader?.Dispose();
            });
        }

        private void DrawShatterEffect(GameTime obj)
        {
            if (ShardOpacity <= 0.0001f)
                return;

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw and update all shards.
            Vector3 screenArea = new(Main.screenWidth, Main.screenHeight, 1f);
            List<VertexPositionColorTexture> shardVertices = new();
            Color shardColor = Color.White * Pow(ShardOpacity, 0.56f);
            foreach (ScreenTriangleShard shard in screenTriangles)
            {
                Vector3 a = new(shard.ScreenCoord1, 0f);
                Vector3 b = new(shard.ScreenCoord2, 0f);
                Vector3 c = new(shard.ScreenCoord3, 0f);

                // Calculate the rotation matrix for the shard.
                Matrix shardTransformation = Matrix.CreateRotationX(shard.RotationX) * Matrix.CreateRotationY(shard.RotationY) * Matrix.CreateRotationZ(shard.RotationZ);

                // Rotate shards in accordance with the rotation matrix.
                a = Vector3.Transform(a, shardTransformation);
                b = Vector3.Transform(b, shardTransformation);
                c = Vector3.Transform(c, shardTransformation);
                a.Z = 0f;
                b.Z = 0f;
                c.Z = 0f;

                Vector3 center = (a + b + c) / 3f;
                Vector3 drawPositionA = (a - center) * screenArea + new Vector3(shard.DrawPosition, 0f);
                Vector3 drawPositionB = (b - center) * screenArea + new Vector3(shard.DrawPosition, 0f);
                Vector3 drawPositionC = (c - center) * screenArea + new Vector3(shard.DrawPosition, 0f);
                shardVertices.Add(new(drawPositionA, shardColor, shard.ScreenCoord1));
                shardVertices.Add(new(drawPositionB, shardColor, shard.ScreenCoord2));
                shardVertices.Add(new(drawPositionC, shardColor, shard.ScreenCoord3));

                for (int i = 0; i < SliceUpdatesPerFrame; i++)
                    shard.Update();
            }

            if (shardVertices.Any())
            {
                CalculatePrimitivePerspectiveMatrices(out Matrix effectView, out Matrix effectProjection);

                // Calculate a universal scale factor for all shards. This is done to make them appear to be "approaching" the camera as time goes on.
                Vector2 scaleFactor = (SmoothStep(0f, 1f, 1f - ShardOpacity) * 3f + 1f) * Vector2.One / Main.GameViewMatrix.Zoom;
                Matrix scale = Matrix.CreateScale(scaleFactor.X, scaleFactor.Y, 1f);

                // Set shader data.
                DrawShader.View = effectView;
                DrawShader.Projection = effectProjection * scale;
                DrawShader.Texture = ContentsBeforeShattering;
                DrawShader.CurrentTechnique.Passes[0].Apply();

                // Draw shard vertices.
                Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, shardVertices.ToArray(), 0, screenTriangles.Count);
            }

            // Make the shards become more transparent as time goes on, until eventually they disappear.
            for (int i = 0; i < SliceUpdatesPerFrame; i++)
            {
                ShardOpacity *= 0.95f;
                if (ShardOpacity <= 0.0045f)
                    screenTriangles.Clear();
            }

            Main.spriteBatch.End();
        }

        public static void CreateShatterEffect(Vector2 shatterScreenPosition, bool gameBreakVariant = false, int sliceUpdatesPerFrame = 1)
        {
            if (!NoxusBossConfig.Instance.ScreenShatterEffects)
            {
                StartShake(11f);
                return;
            }

            useNamelessDeityGameBreakSound = gameBreakVariant;
            ShatterFocalPoint = shatterScreenPosition;
            ShouldCreateSnapshot = true;
            ShardOpacity = 1f;
            SliceUpdatesPerFrame = sliceUpdatesPerFrame;
            manualSliceLines = Array.Empty<LineSegment>();
        }

        public static void CreateShatterEffect(LineSegment[] sliceLines, int sliceUpdatesPerFrame = 1)
        {
            if (!NoxusBossConfig.Instance.ScreenShatterEffects)
            {
                StartShake(11f);
                return;
            }

            ShouldCreateSnapshot = true;
            ShardOpacity = 1f;
            SliceUpdatesPerFrame = sliceUpdatesPerFrame;
            manualSliceLines = sliceLines;
        }

        public static void CreateSnapshotIfNecessary(RenderTarget2D screenContents)
        {
            if (!ShouldCreateSnapshot)
                return;

            ShouldCreateSnapshot = false;

            // Reset the render target if it is invalid or of the incorrect size.
            if (ContentsBeforeShattering is null || ContentsBeforeShattering.IsDisposed || ContentsBeforeShattering.Width != screenContents.Width || ContentsBeforeShattering.Height != screenContents.Height)
                ContentsBeforeShattering.Recreate(screenContents.Width, screenContents.Height);

            // Take the contents of the screen for usage by the shatter pieces.
            ContentsBeforeShattering.CopyContentsFrom(screenContents);

            // Reset old shards.
            screenTriangles.Clear();

            // Generate radial slice angles. These are randomly offset slightly, and form the basis of the shatter effect.
            // The slice is relative to the focal point.
            List<float> radialSliceAngles = new();
            Rectangle screenRectangle = new(0, 0, ContentsBeforeShattering.Width, ContentsBeforeShattering.Height);
            if (manualSliceLines is null || manualSliceLines.Length <= 2)
            {
                int radialSliceCount = Main.rand.Next(8, 12);
                for (int i = 0; i < radialSliceCount; i++)
                {
                    float sliceAngle = TwoPi * i / radialSliceCount + Main.rand.NextFloatDirection() * 0.00146f;
                    radialSliceAngles.Add(sliceAngle);
                }

                for (int i = 0; i < radialSliceAngles.Count; i++)
                {
                    Vector2 a = ShatterFocalPoint / screenRectangle.Size();
                    Vector2 b = a + radialSliceAngles[i].ToRotationVector2() * 0.7f;
                    Vector2 c = a + radialSliceAngles[(i + 1) % radialSliceAngles.Count].ToRotationVector2() * 0.7f;

                    screenTriangles.Add(new(a, b, c, (a + b + c) / 3f * screenRectangle.Size()));
                }

                // Subdivide the triangles until a lot of triangles exist.
                while (screenTriangles.Count <= 350)
                    SubdivideRadialTriangle(Main.rand.Next(screenTriangles), Main.rand.NextFloat(0.15f, 0.44f), Main.rand.NextFloat(0.15f, 0.44f), screenTriangles);
            }
            else
            {
                List<Vector2> intersectionPoints = new();

                // Calculate the averge center of all lines.
                ShatterFocalPoint = Vector2.Zero;
                for (int i = 0; i < manualSliceLines.Length; i++)
                    ShatterFocalPoint += (manualSliceLines[i].Start + manualSliceLines[i].End) / manualSliceLines.Length * 0.5f;
                ShatterFocalPoint -= Main.screenPosition;

                Rectangle lineArea = Utils.CenteredRectangle(ShatterFocalPoint, screenRectangle.Size());

                // Calculate line intersection points relative to the rectangle.
                for (int i = 0; i < manualSliceLines.Length; i++)
                {
                    var line = manualSliceLines[i];
                    var lineInverted = new LineSegment(line.End, line.Start);

                    // Calculate collision points for the line.
                    if (IntersectsLine(line, lineArea, out float x, out float y))
                        intersectionPoints.Add(new(x, y));
                    intersectionPoints.Add((line.End + line.Start) * 0.5f - Main.screenPosition);
                    if (IntersectsLine(lineInverted, lineArea, out float x2, out float y2))
                        intersectionPoints.Add(new(x2, y2));
                }

                // Manually add corners to the intersection points.
                intersectionPoints.Add(lineArea.TopLeft());
                intersectionPoints.Add(lineArea.TopRight());
                intersectionPoints.Add(lineArea.BottomRight());
                intersectionPoints.Add(lineArea.BottomLeft());

                // This is probably borked but ICBA.
                var triangles = EarClippingTriangulation.GenerateMesh(intersectionPoints);
                foreach (var triangle in triangles)
                {
                    Vector2 a = triangle.Vertex1 / lineArea.Size();
                    Vector2 b = triangle.Vertex2 / lineArea.Size();
                    Vector2 c = triangle.Vertex3 / lineArea.Size();
                    screenTriangles.Add(new(a, b, c, (a + b + c) / 3f * screenRectangle.Size()));
                }
            }

            ScreenEffectSystem.SetFlashEffect(ShatterFocalPoint + Main.screenPosition, 2f, 32);
            SoundEngine.PlaySound(useNamelessDeityGameBreakSound ? NamelessDeityBoss.GameBreakSound : ShatterSound);
            useNamelessDeityGameBreakSound = false;
        }

        public static bool IntersectsLine(LineSegment line, Rectangle rect, out float intersectionX, out float intersectionY)
        {
            float x1 = line.Start.X - Main.screenPosition.X;
            float y1 = line.Start.Y - Main.screenPosition.Y;
            float x2 = line.End.X - Main.screenPosition.X;
            float y2 = line.End.Y - Main.screenPosition.Y;
            float xMin = Math.Min(x1, x2);
            float xMax = Math.Max(x1, x2);
            float yMin = Math.Min(y1, y2);
            float yMax = Math.Max(y1, y2);

            // Check if the line is completely outside the AABB.
            if (xMax < rect.Left || xMin > rect.Right || yMax < rect.Top || yMin > rect.Bottom)
            {
                intersectionX = 0;
                intersectionY = 0;
                return false;
            }

            // Check if the line is vertical, to avoid division by zero.
            if (x1 == x2)
            {
                intersectionX = x1;
                intersectionY = Math.Clamp(y1, rect.Top, rect.Bottom);
                return true;
            }

            // Calculate the slope of the line.
            float slope = (y2 - y1) / (x2 - x1);

            // Calculate the y-coordinate at the left/right boundaries of the AABB.
            float yLeft = slope * (rect.Left - x1) + y1;
            float yRight = slope * (rect.Right - x1) + y1;

            // Calculate the x-coordinate at the top/bottom boundaries of the AABB.
            float xTop = (rect.Top - y1) / slope + x1;
            float xBottom = (rect.Bottom - y1) / slope + x1;

            // Check if the line intersects any of the four AABB boundaries.
            if ((y1 <= yLeft && yLeft <= y2 || y2 <= yLeft && yLeft <= y1) && rect.Top <= yLeft && yLeft <= rect.Bottom)
            {
                intersectionX = rect.Left;
                intersectionY = yLeft;
                return true;
            }
            else if ((y1 <= yRight && yRight <= y2 || y2 <= yRight && yRight <= y1) && rect.Top <= yRight && yRight <= rect.Bottom)
            {
                intersectionX = rect.Right;
                intersectionY = yRight;
                return true;
            }
            else if ((x1 <= xTop && xTop <= x2 || x2 <= xTop && xTop <= x1) && rect.Left <= xTop && xTop <= rect.Right)
            {
                intersectionX = xTop;
                intersectionY = rect.Top;
                return true;
            }
            else if ((x1 <= xBottom && xBottom <= x2 || x2 <= xBottom && xBottom <= x1) && rect.Left <= xBottom && xBottom <= rect.Right)
            {
                intersectionX = xBottom;
                intersectionY = rect.Bottom;
                return true;
            }

            intersectionX = 0f;
            intersectionY = 0f;
            return false;
        }

        public static void SubdivideRadialTriangle(ScreenTriangleShard shard, float line1BreakInterpolant, float line2BreakInterpolant, List<ScreenTriangleShard> shards)
        {
            // Remove the original shard from the list, since its subdivisions will be placed instead.
            shards.Remove(shard);

            Vector2 center = shard.ScreenCoord1;
            Vector2 left = shard.ScreenCoord2;
            Vector2 right = shard.ScreenCoord3;

            // Calculate the shatter point relative to the lines of the triangle.
            Vector2 lineBreakLeft = Vector2.Lerp(center, left, line1BreakInterpolant);
            Vector2 lineBreakRight = Vector2.Lerp(center, right, line2BreakInterpolant);

            // Subdivision one - Inner triangle.
            shards.Add(new(center, lineBreakLeft, lineBreakRight, Main.ScreenSize.ToVector2() * (center + lineBreakLeft + lineBreakRight) / 3f));

            // Subdivision two - First part of outer triangle.
            shards.Add(new(right, left, lineBreakLeft, Main.ScreenSize.ToVector2() * (left + right + lineBreakLeft) / 3f));

            // Subdivision three - Second part of outer triangle.
            shards.Add(new(right, lineBreakLeft, lineBreakRight, Main.ScreenSize.ToVector2() * (right + lineBreakLeft + lineBreakRight) / 3f));
        }
    }
}
