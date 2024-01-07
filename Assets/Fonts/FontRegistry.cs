using System;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Assets.Fonts
{
    public class FontRegistry : ModSystem
    {
        // Historically Calamity received errors when attempting to load fonts on Linux systems for their MGRR boss HP bar.
        // Out of an abundance of caution, this mod implements the same solution as them and only uses the font on windows operating systems.
        public static bool CanLoadFonts => Environment.OSVersion.Platform == PlatformID.Win32NT;

        public static FontRegistry Instance => ModContent.GetInstance<FontRegistry>();

        public static readonly GameCulture ChineseGameCulture = GameCulture.FromCultureName(GameCulture.CultureName.Chinese);

        // This font deliberately makes no sense, and does not correspond to a real world language.
        public DynamicSpriteFont DivineLanguageTextText
        {
            get
            {
                if (Main.netMode == NetmodeID.Server)
                    return null;

                if (CanLoadFonts)
                    return Mod.Assets.Request<DynamicSpriteFont>("Assets/Fonts/DivineLanguageText", AssetRequestMode.ImmediateLoad).Value;

                return FontAssets.MouseText.Value;
            }
        }

        public DynamicSpriteFont NamelessDeityText
        {
            get
            {
                if (Main.netMode == NetmodeID.Server)
                    return null;

                // Chinese characters are not present for this font.
                if (ChineseGameCulture.IsActive)
                    return FontAssets.DeathText.Value;

                if (CanLoadFonts)
                    return Mod.Assets.Request<DynamicSpriteFont>("Assets/Fonts/NamelessDeityText", AssetRequestMode.ImmediateLoad).Value;

                return FontAssets.MouseText.Value;
            }
        }
    }
}
