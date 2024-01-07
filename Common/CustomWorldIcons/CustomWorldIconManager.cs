using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Core;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Common.CustomWorldIcons
{
    public class CustomWorldIconManager : ModSystem
    {
        private static Asset<Texture2D> noxusWorldIcon;

        private static Asset<Texture2D> postNamelessDeityIcon;

        public override void OnModLoad()
        {
            Main.QueueMainThreadAction(() =>
            {
                On_AWorldListItem.GetIcon += UseCustomIcons;
            });

            LoadIconTextures();
        }

        private static void LoadIconTextures()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            noxusWorldIcon = ModContent.Request<Texture2D>("NoxusBoss/Common/CustomWorldIcons/IconNoxusWorld", AssetRequestMode.ImmediateLoad);
            postNamelessDeityIcon = ModContent.Request<Texture2D>("NoxusBoss/Common/CustomWorldIcons/IconPostNamelessDeity", AssetRequestMode.ImmediateLoad);
        }

        private Asset<Texture2D> UseCustomIcons(On_AWorldListItem.orig_GetIcon orig, AWorldListItem self)
        {
            // Check for tag data.
            if (self.Data.TryGetHeaderData<CustomWorldIconManager>(out TagCompound tag))
            {
                if (tag.ContainsKey("NoxusWorld"))
                    return noxusWorldIcon;
                if (tag.ContainsKey("UnlockedNamelessDeityWorldIcon"))
                    return postNamelessDeityIcon;
            }

            return orig(self);
        }

        public override void SaveWorldHeader(TagCompound tag)
        {
            if (WorldSaveSystem.HasDefeatedNamelessDeity)
                tag["UnlockedNamelessDeityWorldIcon"] = true;
            if (NoxusWorldManager.Enabled)
                tag["NoxusWorld"] = true;
        }
    }
}
