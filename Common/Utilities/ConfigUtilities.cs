using System;
using System.Reflection;
using NoxusBoss.Core.CrossCompatibility.Inbound;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        private static FieldInfo calConfigInstanceField;

        /// <summary>
        /// Useful way of acquiring information from Calamity's config without a strong reference.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyName">The name of the config value. Should correspond to the property name in Calamity's source code. If it doesn't exist for some reason the default value will be used.</param>
        /// <param name="defaultValue">The default value to use if the config data could not be accessed. Normally is the default for whatever data type is requested (such as zero for integers), but can be manually specified.</param>
        public static T GetFromCalamityConfig<T>(string propertyName, T defaultValue = default)
        {
            // Immediately return the default value if Calamity is not enabled.
            if (ModReferences.BaseCalamity is null)
                return defaultValue;

            // Immediately return the default value if Calamity's config doesn't exist for some reason.
            Type calConfigType = ModReferences.BaseCalamity.Code.GetType("CalamityMod.CalamityConfig");
            if (calConfigType is null)
                return defaultValue;

            // Use reflection to access the property's data. If this fails, return the default value.
            calConfigInstanceField ??= calConfigType.GetField("Instance");
            object calConfig = calConfigInstanceField.GetValue(null);
            return (T)calConfig?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(calConfig) ?? defaultValue;
        }
    }
}
