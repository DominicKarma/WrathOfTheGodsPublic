using Microsoft.Xna.Framework;

namespace NoxusBoss.Common.Easings
{
    // Refer to https://easings.net/ for most of the equations in the derived classes.
    public abstract class EasingCurve
    {
        /// <summary>
        /// 'In' curves start out slowly but gradually reach their end value.
        /// <br></br>
        /// A good analogous example of such is x^2 on values of 0-1.
        /// </summary>
        public InterpolationFunction InCurve
        {
            get;
            protected set;
        }

        /// <summary>
        /// 'Out' curves start out quickly but gradually slow down to reach their end value.
        /// <br></br>
        /// A good analogous example of such is x^0.5 on values of 0-1.
        /// </summary>
        public InterpolationFunction OutCurve
        {
            get;
            protected set;
        }

        /// <summary>
        /// In-out curves start and end gradually, but gain a faster pace to reach their end value.
        /// <br></br>
        /// A good analogous example of such is 3x^2 - 2x^3, better known as the MathHelper.SmoothStep function.
        /// </summary>
        public InterpolationFunction InOutCurve
        {
            get;
            protected set;
        }

        public delegate float InterpolationFunction(float interpolant);

        public float Evaluate(EasingType easingType, float interpolant) => Evaluate(easingType, 0f, 1f, interpolant);

        public float Evaluate(EasingType easingType, float start, float end, float interpolant)
        {
            // Clamp the interpolant into the valid range.
            interpolant = Clamp(interpolant, 0f, 1f);

            float easedInterpolant = easingType switch
            {
                EasingType.In => InCurve(interpolant),
                EasingType.Out => OutCurve(interpolant),
                EasingType.InOut => InOutCurve(interpolant),
                _ => start,
            };

            return Lerp(start, end, easedInterpolant);
        }

        // This isn't used for PiecewiseCurve instances anywhere, but it's a nice utility for more dynamic vector interpolations.
        public Vector2 Evaluate(EasingType easingType, Vector2 start, Vector2 end, float interpolant)
        {
            float x = Evaluate(easingType, start.X, end.X, interpolant);
            float y = Evaluate(easingType, start.Y, end.Y, interpolant);
            return new(x, y);
        }
    }
}
