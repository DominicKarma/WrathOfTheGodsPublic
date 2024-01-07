using NoxusBoss.Common.Easings;
using Terraria;
using static NoxusBoss.Content.NPCs.Bosses.NamelessDeity.NamelessDeityBoss;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity
{
    public class NamelessDeityWingSet
    {
        /// <summary>
        /// The current rotation of the wings.
        /// </summary>
        public float Rotation
        {
            get;
            set;
        }

        /// <summary>
        /// The previous rotation of the wings.
        /// </summary>
        public float PreviousRotation
        {
            get;
            set;
        }

        /// <summary>
        /// The moving average of the rotational difference. This value interpolates rapidly towards the value of <b>Rotation - PreviousRotation</b>.
        /// </summary>
        public float RotationDifferenceMovingAverage
        {
            get;
            set;
        }

        /// <summary>
        /// An easing curve for determining the angular offset of wings when flapping.<br></br>
        /// Positive rotations correspond to upward flaps.<br></br>
        /// Negative rotations correspond to downward flaps.
        /// </summary>
        public static readonly PiecewiseCurve WingFlapAngularMotion = new PiecewiseCurve().
            Add(PolynomialEasing.Cubic, EasingType.Out, 0.25f, 0.36f, -0.4f). // Anticipation.
            Add(PolynomialEasing.Quartic, EasingType.In, -1.82f, 0.51f). // Flap. Descends 1.87 radians (Approximately 107 degrees) in a short period of time.
            Add(PolynomialEasing.Quadratic, EasingType.In, -0.4f, 1f); // Recovery. By the end of this frame the wings have returned to their starting value of -0.4 radians and are ready for anticipation again.

        /// <summary>
        /// Updates the wings.
        /// </summary>
        /// <param name="motionState">The motion that should be used when updating.</param>
        /// <param name="animationCompletion">The 0-1 interpolant for the animation completion.</param>
        public void Update(WingMotionState motionState, float animationCompletion)
        {
            // Cache the current wing rotation as the previous one.
            PreviousRotation = Rotation;

            // It's easing curve time!
            switch (motionState)
            {
                case WingMotionState.RiseUpward:
                    Rotation = (-0.6f).AngleLerp(0.36f, animationCompletion);
                    break;
                case WingMotionState.Flap:
                    Rotation = WingFlapAngularMotion.Evaluate(animationCompletion % 1f);
                    break;
            }

            // Update the rotational moving average.
            RotationDifferenceMovingAverage = Lerp(RotationDifferenceMovingAverage, Rotation - PreviousRotation, 0.15f);
        }
    }
}
