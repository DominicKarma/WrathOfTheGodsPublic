using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Common.BaseEntities
{
    public interface IBoid
    {
        public int GroupID
        {
            get;
        }

        public float FlockmateDetectionRange
        {
            get;
        }

        public Vector2 BoidCenter
        {
            get;
        }

        public Rectangle BoidArea
        {
            get;
        }

        public List<BoidsManager.BoidForceApplicationRule> SimulationRules
        {
            get;
        }

        public ref Vector2 BoidVelocity
        {
            get;
        }

        public bool CurrentlyUsingBoidBehavior
        {
            get;
            set;
        }
    }
}
