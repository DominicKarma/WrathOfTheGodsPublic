using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public static partial class CommonCalamityVariables
    {
        public static bool RevengeanceModeActive
        {
            get => TryGetFromModCall(out bool active, "GetDifficulty", "Revengeance") && active;
            set => SetWorldValue("revenge", value);
        }

        public static bool DeathModeActive
        {
            get => TryGetFromModCall(out bool active, "GetDifficulty", "Death") && active;
            set => SetWorldValue("death", value);
        }

        public static bool AcidRainIsOngoing => TryGetFromModCall(out bool active, "GetAcidRainActive") && active;

        // This represents a shorthand, future-proofed wrapper for accessing Calamity's mod calls where possible.
        // This is done over direct member access for the purpose of minimizing damage in the case of breaking update changes.
        // While the member could be renamed, it's a safe bet that mod calls that access said member will not be a problem.
        public static bool TryGetFromModCall<T>(out T result, params string[] modCallInfo)
        {
            // Use a default value for the output result.
            result = default;

            if (BaseCalamity is not null)
            {
                // Get the result from the mod call. If incorrect mod call information is passed the call will throw an exception.
                // *Technically* implementing some error-handling for that would be the absolute best for future-proofing, but it's possible that would incur considerable
                // performance costs and I don't take Calamity's developers for such fools that they'd change mod calls without some some legacy handling.
                object callResult = BaseCalamity.Call(modCallInfo);

                // If the call result is the desired resulting type, return it.
                if (callResult is not null and T r)
                {
                    result = r;
                    return true;
                }
            }

            // As a failsafe, return false.
            return false;
        }

        // Be careful with numeric types in this! For most programming purposes it's fine to rely on implicit operators for bytes, shorts, ints, etc. to some extent, but when objects are
        // being boxed and unboxed explicitly again you can't rely on that. 
        public static void TrySetFromModCall(object value, params string[] modCallInfo)
        {
            // Don't bother if Calamity is not enabled.
            if (BaseCalamity is null)
                return;

            // It is standard that call information, such as string identifiers, go first while value information goes last.
            BaseCalamity.Call(modCallInfo, value);
        }
    }
}
