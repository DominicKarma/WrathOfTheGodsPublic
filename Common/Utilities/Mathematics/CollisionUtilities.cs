using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Performs collision response across a specific axis relative to the AABB of the base hitbox and of all surrounding hitboxes. This response involves velocity manipulations (such as not falling when there's ground below).
        /// </summary>
        /// <param name="vertical">Whether to apply responses on the Y axis. If true, the Y axis is checked. If false, the X axis is checked.</param>
        /// <param name="checkBoxes">All interactable hitboxes to potentially respond to.</param>
        /// <param name="hitbox">The base hitbox.</param>
        /// <param name="velocity">The velocity of the hitbox.</param>
        /// <param name="depth">How much the velocity would have gone into a hitbox without the collision response. Useful for ascertaining collision context, such as if it was from above or below when a Y collision is registered.</param>
        public static void PerformAABBCollisionResponse(bool vertical, List<Rectangle> checkBoxes, Rectangle hitbox, ref Vector2 velocity, out Vector2 depth)
        {
            // Initialize the depth as zero.
            depth = Vector2.Zero;

            // Do nothing if there are no hitboxes.
            if (!checkBoxes.Any())
                return;

            // Check and respond to all nearby hitboxes.
            foreach (var checkBox in checkBoxes)
            {
                if (!hitbox.Intersects(checkBox))
                    continue;

                // Calculate intersection depth.
                if (Intersects(hitbox, checkBox, vertical, out depth))
                {
                    // Adjust velocity based on intersection directions.
                    if (vertical)
                    {
                        if (depth.Y < 0f)
                            velocity.Y = 0f;
                        else if (velocity.Y < 0.02f)
                            velocity.Y = 0.02f;
                    }
                    else
                    {
                        if (Sign(depth.X) != Sign(velocity.X))
                            velocity.X = 0f;
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Checks whether two rectangles intersect across a specific axis, and if so calculates by how much they're intersecting.
        /// </summary>
        /// <param name="hitbox">The first hitbox.</param>
        /// <param name="collideable">The second hitbox.</param>
        /// <param name="vertical">Whether to apply responses on the Y axis. If true, the Y axis is checked. If false, the X axis is checked.</param>
        /// <param name="depth">How much the hitboxes are intersecting on both axes.</param>
        public static bool Intersects(Rectangle hitbox, Rectangle collideable, bool vertical, out Vector2 depth)
        {
            if (vertical)
                depth = new Vector2(0, GetVerticalIntersectionDepth(hitbox, collideable));
            else
                depth = new Vector2(GetHorizontalIntersectionDepth(hitbox, collideable), 0f);

            return depth.Y != 0f || depth.X != 0f;
        }

        /// <summary>
        /// Calculates rectangle intersection quantities relative to the X axis.
        /// </summary>
        /// <param name="rectA">The first hitbox.</param>
        /// <param name="rectB">The second hitbox.</param>
        public static float GetHorizontalIntersectionDepth(Rectangle rectA, Rectangle rectB)
        {
            // Calculate half sizes.
            var halfWidthA = rectA.Width * 0.5f;
            var halfWidthB = rectB.Width * 0.5f;

            // Calculate centers.
            var centerA = rectA.Left + halfWidthA;
            var centerB = rectB.Left + halfWidthB;

            // Calculate current and minimum-non-intersecting distances between centers.
            var distanceX = centerA - centerB;
            var minDistanceX = halfWidthA + halfWidthB;

            // If we are not intersecting at all, return zero.
            if (Abs(distanceX) >= minDistanceX)
                return 0f;

            // Calculate and return intersection depths.
            return distanceX > 0f ? minDistanceX - distanceX : -minDistanceX - distanceX;
        }

        /// <summary>
        /// Calculates rectangle intersection quantities relative to the Y axis.
        /// </summary>
        /// <param name="rectA">The first hitbox.</param>
        /// <param name="rectB">The second hitbox.</param>
        public static float GetVerticalIntersectionDepth(Rectangle rectA, Rectangle rectB)
        {
            // Calculate half sizes.
            var halfHeightA = rectA.Height * 0.5f;
            var halfHeightB = rectB.Height * 0.5f;

            // Calculate centers.
            var centerA = rectA.Top + halfHeightA;
            var centerB = rectB.Top + halfHeightB;

            // Calculate current and minimum-non-intersecting distances between centers.
            var distanceY = centerA - centerB;
            var minDistanceY = halfHeightA + halfHeightB;

            // If we are not intersecting at all, return (0, 0).
            if (Abs(distanceY) >= minDistanceY)
                return 0f;

            // Calculate and return intersection depths.
            return distanceY > 0f ? minDistanceY - distanceY : -minDistanceY - distanceY;
        }
    }
}
