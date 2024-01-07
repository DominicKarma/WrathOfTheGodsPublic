using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Common.MainMenuThemes
{
    public class NamelessDeityDimensionMainMenu : ModMenu
    {
        public static ModMenu Instance
        {
            get;
            private set;
        }

        public override void Load()
        {
            Instance = this;
        }

        public override string DisplayName => Language.GetTextValue($"Mods.{Mod.Name}.NamelessDeityDimensionMainMenu.DisplayName");

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/EternalGarden");

        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("NoxusBoss/Common/MainMenuThemes/NamelessDeityLogo");

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            Main.spriteBatch.Draw(NamelessDeityDimensionSkyGenerator.NamelessDeityDimensionTarget, Vector2.Zero, Color.White);
            return true;
        }

        public override bool IsAvailable => WorldSaveSystem.HasDefeatedNamelessDeityInAnyWorld;
    }
}
