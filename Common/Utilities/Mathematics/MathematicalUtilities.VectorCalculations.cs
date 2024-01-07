using Microsoft.Xna.Framework;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Calculates the midpoint limb position of a two-limbed IK system via trigonometry.
        /// </summary>
        /// <param name="start">The start of the IK system.</param>
        /// <param name="end">The end effector position of the IK system</param>
        /// <param name="a">The length of the first limb.</param>
        /// <param name="b">The length of the second limb.</param>
        /// <param name="flip">Wheter the angles need to be flipped.</param>
        public static Vector2 IKSolve2(Vector2 start, Vector2 end, float a, float b, bool flip)
        {
            float c = Vector2.Distance(start, end);
            float angle = Acos(Clamp((c * c + a * a - b * b) / (c * a * 2f), -1f, 1f)) * flip.ToDirectionInt();
            return start + (angle + start.AngleTo(end)).ToRotationVector2() * a;
        }

        /// <summary>
        /// Clamps the length of a vector.
        /// </summary>
        /// <param name="v">The vector to clamp the length of.</param>
        /// <param name="min">The minimum vector length.</param>
        /// <param name="max">The maximum vector length.</param>
        public static Vector2 ClampLength(this Vector2 v, float min, float max)
        {
            return v.SafeNormalize(Vector2.UnitY) * Clamp(v.Length(), min, max);
        }

        /// <summary>
        /// A polar equation for a star petal with a given amount of points.
        /// </summary>
        /// <param name="pointCount">The amount of points the star should have.</param>
        /// <param name="angle">The input angle for the polar equation.</param>
        public static Vector2 StarPolarEquation(int pointCount, float angle)
        {
            float spacedAngle = angle;

            // There should be a star point that looks directly upward. However, that isn't the case for odd star counts with the equation below.
            // To address this, a -90 degree rotation is performed.
            if (pointCount % 2 != 0)
                spacedAngle -= PiOver2;

            // Refer to desmos to view the resulting shape this creates. It's basically a black box of trig otherwise.
            float sqrt3 = 1.732051f;
            float numerator = Cos(Pi * (pointCount + 1f) / pointCount);
            float starAdjustedAngle = Asin(Cos(pointCount * spacedAngle)) * 2f;
            float denominator = Cos((starAdjustedAngle + PiOver2 * pointCount) / (pointCount * 2f));
            Vector2 result = angle.ToRotationVector2() * numerator / denominator / sqrt3;
            return result;
        }

        /// <summary>
        /// Interpolates between three <see cref="Vector2"/>-based points via a quadratic Bezier spline.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <param name="c">The third point.</param>
        /// <param name="interpolant">The interpolant to sample points by.</param>
        public static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float interpolant)
        {
            Vector2 firstTerm = Pow(1f - interpolant, 2f) * a;
            Vector2 secondTerm = (2f - interpolant * 2f) * interpolant * b;
            Vector2 thirdTerm = Pow(interpolant, 2f) * c;
            return firstTerm + secondTerm + thirdTerm;
        }

        /// <summary>
        /// Calculates the signed distance of a point from a given line. This is relative to how far it is perpendicular to said line.
        /// </summary>
        /// <param name="p">The point to check.</param>
        /// <param name="linePoint">The pivot point of the line upon which it rotates.</param>
        /// <param name="lineDirection">The direction of the line.</param>
        public static float SignedDistanceToLine(Vector2 p, Vector2 linePoint, Vector2 lineDirection)
        {
            return Vector2.Dot(lineDirection, p - linePoint);
        }
    }
}
