using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics
{
    public class PrimitiveTrailGroup
    {
        private readonly List<PrimitiveTrailInstance> primitives = new();

        public bool DrawManually => DrawContext.HasFlag(PrimitiveGroupDrawContext.Manual);

        public bool DrawPixelated => DrawContext.HasFlag(PrimitiveGroupDrawContext.Pixelated) && !DrawManually;

        public PrimitiveGroupDrawContext DrawContext
        {
            get;
            private set;
        }

        public DynamicIndexBuffer Indices
        {
            get;
            private set;
        }

        public DynamicVertexBuffer Vertices
        {
            get;
            private set;
        }

        public ManagedShader Shader
        {
            get;
            private set;
        }

        public Action ShaderPreparations
        {
            get;
            protected set;
        }

        public PrimitiveTrailGroup(PrimitiveGroupDrawContext drawContext, ManagedShader shader, int maxIndices, int maxVertices, Action shaderPreparations = null)
        {
            // Store the draw context and draw data.
            DrawContext = drawContext;
            Shader = shader;
            ShaderPreparations = shaderPreparations;

            // Create buffers if not in a server.
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() =>
            {
                var gd = Main.instance.GraphicsDevice;
                Indices = new(gd, IndexElementSize.SixteenBits, maxIndices, BufferUsage.WriteOnly);
                Vertices = new(gd, PrimitiveTrail.VertexPosition2DColor.StaticVertexDeclaration, maxVertices, BufferUsage.WriteOnly);
            });
        }

        public void Add(PrimitiveTrailInstance primitiveDrawer)
        {
            primitiveDrawer.GroupThisBelongsTo = this;
            primitives.Add(primitiveDrawer);
        }

        public void Remove(PrimitiveTrailInstance primitiveDrawer)
        {
            primitives.Remove(primitiveDrawer);
        }

        public void Draw()
        {
            // Don't bother if there are no primitives.
            if (!primitives.Any() || primitives[0].PrimitiveDrawer is null)
                return;

            // Fill index and vertex buffers.
            int indexOffset = 0;
            int vertexOffset = 0;
            int maxIndex = 0;
            short[] indices = new short[primitives.Sum(p => p.Indices is null || p.CompletelyOffscreen ? 0 : p.Indices.Length)];
            PrimitiveTrail.VertexPosition2DColor[] vertices = new PrimitiveTrail.VertexPosition2DColor[primitives.Sum(p => p.Vertices is null || p.CompletelyOffscreen ? 0 : p.Vertices.Length)];
            for (int i = 0; i < primitives.Count; i++)
            {
                // Ignore primitives with unfilled index/vertex buffers.
                PrimitiveTrailInstance p = primitives[i];
                if (p.Indices is null || p.Vertices is null || !p.Indices.Any() || !p.Vertices.Any())
                    continue;

                // Ignore primitives that are completely offscreen.
                if (p.CompletelyOffscreen)
                    continue;

                // The + maxIndex calculation is done to ensure that all indices are isolated, and thus shapes don't interconnect in unintended ways.
                Vector2 offset = primitives[i].Owner.position - primitives[i].Vertices[0].Position + primitives[i].PreviousGeneralOffset;
                for (int j = 0; j < p.Indices.Length; j++)
                    indices[indexOffset + j] = (short)(p.Indices[j] + maxIndex);
                for (int j = 0; j < p.Vertices.Length; j++)
                {
                    var oldVertex = p.Vertices[j];
                    vertices[vertexOffset + j] = new(oldVertex.Position + offset, oldVertex.Color, oldVertex.TextureCoordinates);
                }

                maxIndex = indices.Max() + 1;
                indexOffset += p.Indices.Length;
                vertexOffset += p.Vertices.Length;
                p.SuccessfullyUpdatedBufferThisFrame = false;
            }
            Indices.SetData(indices, 0, indices.Length, SetDataOptions.Discard);
            Vertices.SetData(vertices, 0, vertices.Length, SetDataOptions.Discard);

            // Don't bother if no valid primitive data was found.
            if (indexOffset <= 0 || vertexOffset <= 0)
                return;

            // Calculate projection matrix data.
            primitives[0].PrimitiveDrawer.UpdateBaseEffect(out Matrix projection, out Matrix view);

            // Apply screen scissor effects, for efficiency reasons.
            var gd = Main.instance.GraphicsDevice;
            gd.RasterizerState = CullOnlyScreen;
            gd.ScissorRectangle = new(0, 0, Main.screenWidth, Main.screenHeight);

            // Apply the shader in anticipation of drawing.
            Shader.TrySetParameter("uWorldViewProjection", view * projection);
            ShaderPreparations?.Invoke();
            Shader.Apply();

            // Draw the primitives.
            gd.SetVertexBuffer(Vertices);
            gd.Indices = Indices;
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexOffset, 0, indexOffset / 3);

            // Apply vanilla's regular, non-affecting shader afterwards, to allow returning to regular drawing without having to restart the sprite batch.
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        public void Dispose()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.QueueMainThreadAction(() =>
            {
                Indices?.Dispose();
                Vertices?.Dispose();
            });
        }
    }
}
