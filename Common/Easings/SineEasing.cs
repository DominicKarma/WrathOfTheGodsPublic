namespace NoxusBoss.Common.Easings
{
    public class SineEasing : EasingCurve
    {
        public static readonly SineEasing Default = new();

        public SineEasing()
        {
            InCurve = new(interpolant =>
            {
                return 1f - Cos(interpolant * PiOver2);
            });
            OutCurve = new(interpolant =>
            {
                return Sin(interpolant * PiOver2);
            });
            InOutCurve = new(interpolant =>
            {
                return (Cos(Pi * interpolant) - 1f) * -0.5f;
            });
        }
    }
}
