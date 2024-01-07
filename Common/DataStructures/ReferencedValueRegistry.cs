using System.Collections.Generic;
using System.Diagnostics;

namespace NoxusBoss.Common.DataStructures
{
    [DebuggerDisplay("{localValues}")]
    public class ReferencedValueRegistry
    {
        // A generalized collection of player-specific data. This acts as a way of handling such things without creating 300 different, unrelated fields that clutter up this file.
        // Use GetValueRef to access their contents. It is wise to use constants when referencing keys in either method rather than string literals due to DRY principles.
        private readonly Dictionary<string, Referenced<object>> localValues = new();

        private void VerifyValueExists<T>(string key) where T : struct
        {
            if (!localValues.TryGetValue(key, out Referenced<object> value) || value.Value is not T)
                localValues[key] = new(default(T));
        }

        public Referenced<T> GetValueRef<T>(string key) where T : struct
        {
            VerifyValueExists<T>(key);
            return localValues[key];
        }
    }
}
