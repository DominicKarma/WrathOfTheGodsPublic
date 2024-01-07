using System.IO;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.CrossCompatibility.Inbound;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Core.Autoloaders
{
    public static class MusicBoxAutoloader
    {
        [Autoload(false)]
        public class AutoloadableMusicBoxItem : ModItem, IToastyQoLChecklistItemSupport
        {
            // Using MusicLoader.GetMusicSlot at Load time doesn't work and returns a value of 0. As such, it's necessary to store the path of the music so that the slot ID
            // can be retrieved at a later time in the loading process.
            private readonly string musicPath;

            private readonly string texturePath;

            private readonly string name;

            private readonly ToastyQoLRequirement obtainmentRequirement;

            internal int tileID;

            public ToastyQoLRequirement Requirement => obtainmentRequirement;

            public override string Name => name;

            public override string Texture => texturePath;

            // Necessary for autoloaded types since the constructor is important in determining the behavior of the given instance, making it impossible to rely on an a parameterless one for
            // managing said instances.
            protected override bool CloneNewInstances => true;

            public AutoloadableMusicBoxItem(string texturePath, string musicPath, ToastyQoLRequirement requirement)
            {
                string name = Path.GetFileName(texturePath).Replace("_Item", string.Empty);
                obtainmentRequirement = requirement;
                this.musicPath = musicPath;
                this.texturePath = texturePath;
                this.name = name;
            }

            public override void SetStaticDefaults()
            {
                Item.ResearchUnlockCount = 1;

                // Music boxes can't get prefixes in vanilla.
                ItemID.Sets.CanGetPrefixes[Type] = false;

                // Recorded music boxes transform into the basic form in shimmer.
                ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.MusicBox;

                // Register the music box with the desired music.
                int musicSlotID = MusicLoader.GetMusicSlot(Mod, musicPath);
                MusicLoader.AddMusicBox(Mod, musicSlotID, Type, tileID);
            }

            public override void SetDefaults()
            {
                Item.DefaultToMusicBox(tileID);
            }
        }

        [Autoload(false)]
        public class AutoloadableMusicBoxTile : ModTile
        {
            internal int itemID;

            private readonly string texturePath;

            private readonly string name;

            public override string Name => name;

            public override string Texture => texturePath;

            public AutoloadableMusicBoxTile(string texturePath)
            {
                name = Path.GetFileName(texturePath).Replace("_Tile", "Tile");
                this.texturePath = texturePath;
            }

            public override void SetStaticDefaults()
            {
                Main.tileFrameImportant[Type] = true;
                Main.tileObsidianKill[Type] = true;
                TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
                TileObjectData.newTile.Origin = new Point16(0, 1);
                TileObjectData.newTile.LavaDeath = false;
                TileObjectData.newTile.DrawYOffset = 2;
                TileObjectData.addTile(Type);

                TileID.Sets.DisableSmartCursor[Type] = true;

                AddMapEntry(new Color(150, 137, 142));
            }

            public override void MouseOver(int i, int j)
            {
                Player player = Main.LocalPlayer;
                player.noThrow = 2;
                player.cursorItemIconEnabled = true;
                player.cursorItemIconID = itemID;
            }

            public override bool CreateDust(int i, int j, ref int type) => false;

            public override void KillMultiTile(int i, int j, int frameX, int frameY)
            {
                if (frameX >= 36)
                    Item.NewItem(new EntitySource_TileBreak(i, j), new Point(i, j).ToWorldCoordinates(), itemID);
            }
        }

        public static void Create(Mod mod, string texturePathBase, string musicPath, ToastyQoLRequirement requirement, out int itemID, out int tileID)
        {
            // Autoload the item.
            AutoloadableMusicBoxItem boxItem = new($"{texturePathBase}_Item", musicPath, requirement);
            mod.AddContent(boxItem);
            itemID = boxItem.Type;

            // Autoload the tile.
            AutoloadableMusicBoxTile boxTile = new($"{texturePathBase}_Tile");
            mod.AddContent(boxTile);
            tileID = boxTile.Type;

            // Link the loaded types together by informing each other of their respective IDs.
            boxItem.tileID = tileID;
            boxTile.itemID = itemID;
        }
    }
}
