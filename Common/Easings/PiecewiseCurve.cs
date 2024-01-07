using System;
using System.Collections.Generic;
using System.Linq;

namespace NoxusBoss.Common.Easings
{
    public class PiecewiseCurve
    {
        /// <summary>
        /// A piecewise curve that takes up a part of the domain of a <see cref="PiecewiseCurve"/>, specifying the equivalent range and curvature in said domain.
        /// </summary>
        protected readonly struct CurveSegment
        {
            /// <summary>
            /// The starting output height value. This is what is outputted when the <see cref="PiecewiseCurve"/> is evaluated at <see cref="AnimationStart"/>.
            /// </summary>
            internal readonly float StartingHeight;

            /// <summary>
            /// The ending output height value. This is what is outputted when the <see cref="PiecewiseCurve"/> is evaluated at <see cref="AnimationEnd"/>.
            /// </summary>
            internal readonly float EndingHeight;

            /// <summary>
            /// The start of this curve segment's domain relative to the <see cref="PiecewiseCurve"/>.
            /// </summary>
            internal readonly float AnimationStart;

            /// <summary>
            /// The ending of this curve segment's domain relative to the <see cref="PiecewiseCurve"/>.
            /// </summary>
            internal readonly float AnimationEnd;

            /// <summary>
            /// The easing curve that dictates the how the outputs vary between <see cref="StartingHeight"/> and <see cref="EndingHeight"/>.
            /// </summary>
            internal readonly EasingCurve Curve;

            /// <summary>
            /// The easing curve type from In, Out, and InOut that specifies how the <see cref="Curve"/> is sampled.
            /// </summary>
            internal readonly EasingType CurveType;

            public CurveSegment(float startingHeight, float endingHeight, float animationStart, float animationEnd, EasingCurve curve, EasingType curveType)
            {
                StartingHeight = startingHeight;
                EndingHeight = endingHeight;
                AnimationStart = animationStart;
                AnimationEnd = animationEnd;
                Curve = curve;
                CurveType = curveType;
            }
        }

        /// <summary>
        /// The list of <see cref="CurveSegment"/> that encompass the entire 0-1 domain of this function.
        /// </summary>
        protected List<CurveSegment> segments = new();

        public PiecewiseCurve Add(EasingCurve curve, EasingType curveType, float endingHeight, float animationEnd, float? startingHeight = null)
        {
            float animationStart = segments.Any() ? segments.Last().AnimationEnd : 0f;
            startingHeight ??= segments.Any() ? segments.Last().EndingHeight : 0f;
            if (animationEnd <= 0f || animationEnd > 1f)
                throw new InvalidOperationException("A piecewise animation curve segment cannot have a domain outside of 0-1.");

            // Add the new segment.
            segments.Add(new(startingHeight.Value, endingHeight, animationStart, animationEnd, curve, curveType));

            // Return the piecewise curve that called this method to allow method chaining.
            return this;
        }

        public float Evaluate(float interpolant)
        {
            // Clamp the interpolant into the valid range.
            interpolant = Clamp(interpolant, 0f, 1f);

            // Calculate the local interpolant relative to the segment that the base interpolant fits into.
            CurveSegment segmentToUse = segments.Find(s => interpolant >= s.AnimationStart && interpolant <= s.AnimationEnd);
            float curveLocalInterpolant = InverseLerp(segmentToUse.AnimationStart, segmentToUse.AnimationEnd, interpolant);

            // Calculate the segment value based on the local interpolant.
            return segmentToUse.Curve.Evaluate(segmentToUse.CurveType, segmentToUse.StartingHeight, segmentToUse.EndingHeight, curveLocalInterpolant);
        }
    }
}
