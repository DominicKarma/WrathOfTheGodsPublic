using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics.Primitives;
using Terraria;

namespace NoxusBoss.Core.Graphics
{
    public class PrimitiveTrailInstance
    {
        // If buffers aren't updated, that means that their old contents can persist on the screen long after the object that contains the buffers has left.
        // This can result in the effects lingering on the screen like a thumbtack until eventually this instance is naturally removed.
        // A good example of when this could happen is with the projectile PreDraw hook. Offscreen projectiles are naturally culled off if their position is offscreen.
        // This is a perfectly fine optimization, but if buffers are updated in PreDraw and ONLY the projectile's position (and not its trail) is accounted for, the above
        // issue can (and has in testing, with the WindStreakVisual) happen.
        public bool SuccessfullyUpdatedBufferThisFrame
        {
            get;
            set;
        }

        public Entity Owner
        {
            get;
            private set;
        }

        public Vector2 PreviousGeneralOffset
        {
            get;
            private set;
        }

        public bool CompletelyOffscreen
        {
            get
            {
                // Count this instance as offscreen if there are no vertices to check or if the buffer has not been updated.
                if (Vertices is null || !SuccessfullyUpdatedBufferThisFrame)
                    return true;

                // Look through all vertices. If any of them is inside of the screen bounds, then it isn't completely offscreen.
                Vector2 topLeft = Vector2.One * -25f;
                Vector2 bottomRight = new(Main.screenWidth + 25f, Main.screenHeight + 25f);
                for (int i = 0; i < Vertices.Length; i++)
                {
                    if (Vertices[i].Position.Between(topLeft, bottomRight))
                        return false;
                }

                return true;
            }
        }

        public bool HasExpired
        {
            get;
            set;
        }

        public PrimitiveTrail.VertexPosition2DColor[] Vertices
        {
            get;
            private set;
        }

        public short[] Indices
        {
            get;
            private set;
        }

        // This needs to be in the trail instance and not the group itself if you want local data to be included in the trail's drawing.
        // For example, if you want a trail that fades out based on an individual projectile's Opacity value then you can't have
        // a single trail that manages all projectiles because some projectiles may have different opacity values.
        public PrimitiveTrail PrimitiveDrawer
        {
            get;
            set;
        }

        public PrimitiveTrailGroup GroupThisBelongsTo
        {
            get;
            internal set;
        }

        internal PrimitiveTrailInstance(Entity owner) => Owner = owner;

        // This should be called in something like PreDraw, before the group itself is expected to draw.
        public void UpdateBuffers(Vector2[] positions, Vector2 generalOffset, int totalTrailPoints)
        {
            PreviousGeneralOffset = generalOffset;
            List<Vector2> smoothTrailPositions = PrimitiveDrawer.GetTrailPoints(positions, generalOffset, totalTrailPoints);
            Vertices = PrimitiveDrawer.GetVerticesFromTrailPoints(smoothTrailPositions);
            Indices = PrimitiveTrail.GetIndicesFromTrailPoints(smoothTrailPositions.Count);
            SuccessfullyUpdatedBufferThisFrame = true;
        }

        public void Destroy() => GroupThisBelongsTo.Remove(this);
    }
}
