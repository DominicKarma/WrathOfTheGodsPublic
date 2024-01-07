using System.Collections.Generic;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public interface IInfernumBossBarSupport
    {
        public IEnumerable<float> PhaseThresholdLifeRatios
        {
            get;
        }
    }
}
