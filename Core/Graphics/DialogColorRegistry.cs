using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class DialogColorRegistry : ModSystem
    {
        public static Color CattailAnimationTextColor
        {
            get;
            private set;
        }

        public static Color NoxusTextColor
        {
            get;
            private set;
        }

        public static Color PurifierWarningTextColor
        {
            get;
            private set;
        }

        public static Color VanillaEventTextColor
        {
            get;
            private set;
        }

        public static Color NamelessDeityTextColor
        {
            get;
            private set;
        }

        public const string ColorLocalizationPrefix = "Mods.NoxusBoss.Colors.";

        public static Color LoadFromLocalization(string key)
        {
            string hexString = Language.GetTextValue($"{ColorLocalizationPrefix}{key}");

            // Ensure that the hex string is a valid number.
            if (!int.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int hex))
                throw new InvalidCastException($"A hex string of {hexString} was supplied for the '{key}' localization entry. However, this string could not be convered to a number!");

            return ColorFromHex(hex);
        }

        public static Color ColorFromHex(int hex)
        {
            // Unpack the hex value as RGB components.
            // Bits 1-8 are blue.
            // Bits 9-16 are green.
            // Bits 17-24 are red.
            int blue = (byte)hex;
            int green = (byte)(hex >> 8);
            int red = (byte)(hex >> 16);
            return new(red, green, blue);
        }

        public override void PostSetupContent()
        {
            // Load text colors from the localization file.
            CattailAnimationTextColor = LoadFromLocalization("CattailAnimationTextHex");
            NoxusTextColor = LoadFromLocalization("NoxusTextHex");
            PurifierWarningTextColor = LoadFromLocalization("PurifierWarningTextHex");
            VanillaEventTextColor = LoadFromLocalization("VanillaEventTextHex");
            NamelessDeityTextColor = LoadFromLocalization("NamelessDeityTextHex");
        }
    }
}
