namespace NoxusBoss.Common.Easings
{
    public class ElasticEasing : EasingCurve
    {
        public static readonly ElasticEasing Default = new();

        public ElasticEasing()
        {
            InCurve = new(interpolant =>
            {
                float sineFactor = TwoPi / 3f;
                float exponentialTerm = -Pow(2f, interpolant * 10f - 10f);
                float sinusoidalTerm = Sin((interpolant * 10f - 10.75f) * sineFactor);
                return exponentialTerm * sinusoidalTerm;
            });
            OutCurve = new(interpolant =>
            {
                float sineFactor = TwoPi / 3f;
                float exponentialTerm = Pow(2f, interpolant * -10f);
                float sinusoidalTerm = Sin((interpolant * 10f - 0.75f) * sineFactor);
                return exponentialTerm * sinusoidalTerm + 1f;
            });
            InOutCurve = new(interpolant =>
            {
                float sineFactor = TwoPi / 4.5f;
                float sinusoidalTerm = Sin((interpolant * 20f - 11.125f) * sineFactor) * 0.5f;
                if (interpolant < 0.5f)
                {
                    float exponentialTerm = -Pow(2f, interpolant * 20f - 10f);
                    return exponentialTerm * sinusoidalTerm;
                }
                else
                {
                    float exponentialTerm = Pow(2f, interpolant * -20f + 10f);
                    return exponentialTerm * sinusoidalTerm + 1f;
                }
            });
        }
    }
}
