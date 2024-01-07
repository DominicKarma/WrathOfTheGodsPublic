using Microsoft.Xna.Framework;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class MidnightMonolithTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileSpelunker[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 16 };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(164, 124, 255));
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.tile[i, j].TileFrameX == 36 && Main.tile[i, j].TileFrameY == 0)
            {
                NoxusSky.InProximityOfMidnightMonolith = true;
                NoxusSky.TimeSinceCloseToMidnightMonolith = 0;
            }
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            if (frameX >= 36)
                Item.NewItem(new EntitySource_TileBreak(i, j), new Point(i, j).ToWorldCoordinates(), ModContent.ItemType<MidnightMonolith>());
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool CreateDust(int i, int j, ref int type)
        {
            for (int k = 0; k < 2; k++)
            {
                Dust fire = Dust.NewDustPerfect(new Vector2(i, j).ToWorldCoordinates() + Main.rand.NextVector2Circular(8f, 8f), 180);
                fire.scale = 1.3f;
                fire.velocity = Main.rand.NextVector2Circular(3f, 3f);
                fire.noGravity = true;
            }
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num)
        {
            num = fail ? 1 : 3;
        }

        public override void HitWire(int i, int j)
        {
            LightHitWire(Type, i, j, 2, 4);
        }

        public override bool RightClick(int i, int j)
        {
            LightHitWire(Type, i, j, 2, 4);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<MidnightMonolith>();
        }
    }
}
