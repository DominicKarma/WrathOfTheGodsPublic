using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Tiles
{
    public class ElysianRose : ModTile
    {
        public const int Width = 2;

        public const int Height = 3;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;

            // Prepare necessary setups to ensure that this tile is treated like grass.
            Main.tileCut[Type] = true;
            TileID.Sets.ReplaceTileBreakUp[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            // All of the special plants in Nameless' garden glow slightly.
            Main.tileLighted[Type] = true;

            // Use plant destruction visuals and sounds.
            HitSound = SoundID.Grass;
            DustType = 244;

            AddMapEntry(new Color(242, 160, 174));
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.56f;
            g = 0.41f;
            b = 0.356f;
        }
    }
}
