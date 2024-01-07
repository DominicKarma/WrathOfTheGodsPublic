using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class Starbearer : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;

            // Prepare necessary setups to ensure that this tile is treated like grass.
            TileID.Sets.ReplaceTileBreakUp[Type] = true;
            TileID.Sets.SwaysInWindBasic[Type] = true;
            TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);

            // All of the special plants in Nameless' garden glow slightly.
            Main.tileLighted[Type] = true;

            // Use plant destruction visuals and sounds.
            HitSound = SoundID.Grass;
            DustType = DustID.PinkFairy;

            AddMapEntry(new Color(156, 156, 156));
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX;
            int frameY = t.TileFrameY;

            // Draw the main tile texture.
            Texture2D mainTexture = TextureAssets.Tile[Type].Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y + 2f) + drawOffset;
            Color lightColor = Lighting.GetColor(i, j);
            SpriteEffects direction = (i * 3 + j * 717) % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(mainTexture, drawPosition, new Rectangle(frameX, frameY, 16, 16), lightColor, 0f, Vector2.Zero, 1f, direction, 0f);

            // Draw lights over the flower.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 0.4f + (i * 0.9444f + j * 0.3768f) % TwoPi;
            Color glowColor = Color.Lerp(Color.BlueViolet, Color.IndianRed, (i * 0.1455f + j * 0.7484f) % 0.75f) with { A = 0 };
            Vector2 glowDrawPosition = drawPosition + new Vector2(7f, 8f);

            spriteBatch.Draw(BloomFlare, glowDrawPosition, null, Color.Gold with { A = 0 } * 0.3f, bloomFlareRotation, BloomFlare.Size() * 0.5f, 0.05f, 0, 0f);
            spriteBatch.Draw(BloomCircleSmall, glowDrawPosition, null, glowColor * 0.67f, 0f, BloomCircleSmall.Size() * 0.5f, 0.2f, 0, 0f);

            return false;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.8f;
            g = 0.2f;
            b = 0.8f;
        }
    }
}
