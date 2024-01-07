using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Wraps an angle similar to <see cref="MathHelper.WrapAngle(float)"/>, except with a range of 0 to 2pi instead of -pi to pi.
        /// </summary>
        /// <param name="theta">The angle to wrap.</param>
        public static float WrapAngle360(float theta)
        {
            theta = WrapAngle(theta);
            if (theta < 0f)
                theta += TwoPi;

            return theta;
        }

        /// <summary>
        /// Determines the angular distance between two vectors based on dot product comparisons. This method ensures underlying normalization is performed safely.
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        public static float AngleBetween(this Vector2 v1, Vector2 v2) => Acos(Vector2.Dot(v1.SafeNormalize(Vector2.Zero), v2.SafeNormalize(Vector2.Zero)));
    }
}
