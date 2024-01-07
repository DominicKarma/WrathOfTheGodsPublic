using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Determines if a typical hitbox rectangle is intersecting a circular hitbox.
        /// </summary>
        /// <param name="centerCheckPosition">The center of the circular hitbox.</param>
        /// <param name="radius">The radius of the circular hitbox.</param>
        /// <param name="targetHitbox">The hitbox of the target to check.</param>
        public static bool CircularHitboxCollision(Vector2 centerCheckPosition, float radius, Rectangle targetHitbox)
        {
            float topLeftDistance = Vector2.Distance(centerCheckPosition, targetHitbox.TopLeft());
            float topRightDistance = Vector2.Distance(centerCheckPosition, targetHitbox.TopRight());
            float bottomLeftDistance = Vector2.Distance(centerCheckPosition, targetHitbox.BottomLeft());
            float bottomRightDistance = Vector2.Distance(centerCheckPosition, targetHitbox.BottomRight());

            float distanceToClosestPoint = topLeftDistance;
            if (topRightDistance < distanceToClosestPoint)
                distanceToClosestPoint = topRightDistance;
            if (bottomLeftDistance < distanceToClosestPoint)
                distanceToClosestPoint = bottomLeftDistance;
            if (bottomRightDistance < distanceToClosestPoint)
                distanceToClosestPoint = bottomRightDistance;

            return distanceToClosestPoint <= radius;
        }

        /// <summary>
        /// <see cref="Collision.TileCollision(Vector2, Vector2, int, int, bool, bool, int)"/> but it doesn't fail for non-wooden platforms.
        /// </summary>
        /// <param name="topLeft">The top left of the area to check.</param>
        /// <param name="width">The width of the area the check.</param>
        /// <param name="height">The height of the area the check.</param>
        /// <param name="onlyTopSurfaces">Whether only top surface collisions were found.</param>
        public static bool TileCollision(Vector2 topLeft, float width, float height, out bool onlyTopSurfaces)
        {
            onlyTopSurfaces = false;

            int leftX = (int)(topLeft.X / 16f) - 1;
            int rightX = (int)((topLeft.X + (float)width) / 16f) + 2;
            int topY = (int)(topLeft.Y / 16f) - 1;
            int bottomY = (int)((topLeft.Y + (float)height) / 16f) + 2;

            leftX = Utils.Clamp(leftX, 0, Main.maxTilesX - 1);
            rightX = Utils.Clamp(rightX, 0, Main.maxTilesX - 1);
            topY = Utils.Clamp(topY, 0, Main.maxTilesY - 1);
            bottomY = Utils.Clamp(bottomY, 0, Main.maxTilesY - 1);

            Vector2 tileWorldPosition = default;
            for (int i = leftX; i < rightX; i++)
            {
                for (int j = topY; j < bottomY; j++)
                {
                    Tile tile = Main.tile[i, j];
                    if (!tile.HasUnactuatedTile)
                        continue;

                    bool topSurfaceCollision = (Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0) || TileID.Sets.Platforms[tile.TileType];
                    bool solidCollision = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType] && !TileID.Sets.Platforms[tile.TileType];

                    if (solidCollision || topSurfaceCollision)
                    {
                        tileWorldPosition.X = i * 16;
                        tileWorldPosition.Y = j * 16;
                        int tileHeight = 16;
                        if (tile.IsHalfBlock)
                        {
                            tileWorldPosition.Y += 8f;
                            tileHeight -= 8;
                        }

                        if (topLeft.X + (float)width > tileWorldPosition.X &&
                            topLeft.X < tileWorldPosition.X + 16f &&
                            topLeft.Y + (float)height > tileWorldPosition.Y &&
                            topLeft.Y < tileWorldPosition.Y + (float)tileHeight)
                        {
                            if (solidCollision)
                            {
                                onlyTopSurfaces = false;
                                return true;
                            }

                            // Don't return if a top surface was found. Continue checking for non top surface tiles.
                            else
                                onlyTopSurfaces = true;
                        }
                    }
                }
            }

            if (onlyTopSurfaces)
                return true;

            return false;
        }

        /// <summary>
        /// Performs collision response across a specific axis relative to the AABB of the base hitbox and of all surrounding hitboxes. This response involves velocity manipulations (such as not falling when there's ground below).
        /// </summary>
        /// <param name="vertical">Whether to apply responses on the Y axis. If true, the Y axis is checked. If false, the X axis is checked.</param>
        /// <param name="checkBoxes">All interactable hitboxes to potentially respond to.</param>
        /// <param name="hitbox">The base hitbox.</param>
        /// <param name="velocity">The velocity of the hibox.</param>
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
