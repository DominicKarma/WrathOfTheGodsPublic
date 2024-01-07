using System.Reflection;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public interface IToastyQoLChecklistBossSupport
    {
        // Adheres to the same weights as Boss Checklist. Refer to IBossChecklistSupport for a comprehensive list of existing values.
        public float ProgressionValue
        {
            get;
        }

        // Toasty's QoL mod uses a FieldInfo instead of a delegate.
        // This is because somewhere it needs to set its value, and not just retrieve it.
        public FieldInfo IsDefeatedField
        {
            get;
        }
    }
}
