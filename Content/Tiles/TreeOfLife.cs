using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Subworlds;
using NoxusBoss.Content.Items;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Content.Projectiles.Typeless;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class TreeOfLife : ModTile
    {
        public static Asset<Texture2D> TreeTexture
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(60, 81, 60));

            if (Main.netMode != NetmodeID.Server)
                TreeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/TreeOfLifeButReal");
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            // Draw the main tile texture.
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y + 20f) + drawOffset;
            Color lightColor = Lighting.GetColor(i, j);
            float treeRotation = Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.01f + 0.03f;
            float scale = 1f;
            spriteBatch.Draw(TreeTexture.Value, drawPosition, null, lightColor, treeRotation, TreeTexture.Size() * new Vector2(0.5f, 1f), scale, 0, 0f);

            // Stop here if there are no fruits to draw.
            if (EternalGardenUpdateSystem.LifeFruitDroppedFromTree)
                return false;

            // Calculate fruit draw offsets.
            ulong fruitSeed = (ulong)(i * 3 + j * 7);
            Vector2[] fruitOffsets = new Vector2[]
            {
                new(-104f, -120f),
                new(-71f, -177f),
                new(-51f, -204f),
                new(-35f, -172f),
                new(12f, -148f),
                new(30f, -175f),
                new(67f, -139f)
            };

            // Draw fruits on the leaves.
            bool namelessIsPresent = NamelessDeityBoss.Myself is not null && NamelessDeityBoss.Myself.Opacity >= 1f;
            foreach (Vector2 fruitOffset in fruitOffsets)
            {
                Color fruitColor = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.2f + fruitOffset.X * 0.07f) % 1f, 0.92f, 0.92f);
                float fruitScale = Lerp(0.55f, 1.02f, Utils.RandomFloat(ref fruitSeed)) * scale;
                float fruitRotation = Sin(fruitOffset.X * 0.6f + Main.GlobalTimeWrappedHourly * 5.6f) * 0.15f - 0.25f;
                Vector2 properFruitOffset = fruitOffset.RotatedBy(treeRotation) * scale;
                spriteBatch.Draw(GoodApple.MyTexture.Value, drawPosition + properFruitOffset, null, fruitColor, fruitRotation, GoodApple.MyTexture.Size() * new Vector2(0.5f, 0f), fruitScale, 0, 0f);

                // Drop the fruit if Nameless has arrived.
                if (namelessIsPresent)
                {
                    Vector2 fruitWorldPosition = new Point(i, j).ToWorldCoordinates() + Vector2.UnitY * 20f + properFruitOffset;
                    NewProjectileBetter(new EntitySource_WorldEvent(), fruitWorldPosition, Vector2.UnitY.RotatedBy(fruitRotation) * Main.rand.NextFloat(2.5f, 3.3f), ModContent.ProjectileType<FallenGoodApple>(), 0, 0f);
                    EternalGardenUpdateSystem.LifeFruitDroppedFromTree = true;
                }
            }
            return false;
        }
    }
}
