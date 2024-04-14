using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Items.Placeable.Monoliths;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class DivineMonolithTile : ModTile
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
            AddMapEntry(new Color(124, 160, 140));
        }

        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (Main.tile[i, j].TileFrameX == 36 && Main.tile[i, j].TileFrameY == 0)
            {
                NamelessDeityDimensionSkyGenerator.InProximityOfDivineMonolith = true;
                NamelessDeityDimensionSkyGenerator.TimeSinceCloseToDivineMonolith = 0;

                Vector2 startingPoint = new Point(i, j).ToWorldCoordinates() + new Vector2(17f, 6f);
                Player nearestPlayer = Main.player[Player.FindClosest(startingPoint, 0, 0)];
                if (Main.rand.NextBool(27) && !Main.gamePaused && !nearestPlayer.WithinRange(startingPoint, 100f) && NamelessDeitySky.SkyIntensityOverride >= 0.99f)
                {
                    Vector2 directionToPlayer = (nearestPlayer.Center - startingPoint).SafeNormalize(-Vector2.UnitY);
                    Vector2 galaxyVelocity = directionToPlayer.RotatedByRandom(1.87f) * Main.rand.NextFloat(9.5f, 28f) + Main.rand.NextVector2Circular(9f, 9f);
                    Color galaxyColor = MulticolorLerp(Pow(Main.rand.NextFloat(), 2f) * 0.92f, Color.OrangeRed, Color.Coral, Color.HotPink, Color.Magenta, Color.DarkViolet, Color.Cyan) * 1.9f;
                    galaxyColor = Color.Lerp(galaxyColor, Color.Wheat, 0.55f);

                    int galaxyLifetime = (int)Utils.Remap(galaxyVelocity.Length(), 10f, 19.2f, 90f, 150f) + Main.rand.Next(-30, 45);
                    float galaxyScale = Utils.Remap(galaxyVelocity.Length(), 9.5f, 17.4f, 0.12f, 0.4f) + Pow(Main.rand.NextFloat(), 3f) * 0.8f;

                    DivineMonolithGalaxySystem.Galaxy.CreateNew(startingPoint, galaxyVelocity, galaxyColor, galaxyLifetime, Main.rand.NextFloat(TwoPi), galaxyScale);

                    // Create particles.
                    StrongBloom bloom = new(startingPoint, Vector2.Zero, galaxyColor * 0.7f, 3f, 32);
                    MagicBurstParticle burst = new(startingPoint, Vector2.Zero, galaxyColor, 14, 0.3f, 0.084f);
                    bloom.Spawn();
                    burst.Spawn();
                }
            }
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            if (frameX >= 36)
                Item.NewItem(new EntitySource_TileBreak(i, j), new Point(i, j).ToWorldCoordinates(), ModContent.ItemType<DivineMonolith>());
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override bool CreateDust(int i, int j, ref int type)
        {
            for (int k = 0; k < 2; k++)
            {
                Dust fire = Dust.NewDustPerfect(new Vector2(i, j).ToWorldCoordinates() + Main.rand.NextVector2Circular(8f, 8f), 173);
                fire.scale = 1.3f;
                fire.velocity = Main.rand.NextVector2Circular(3f, 3f);
                fire.noGravity = true;
            }
            return false;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX;
            int frameY = t.TileFrameY;

            // Draw the main tile texture.
            Texture2D mainTexture = TextureAssets.Tile[Type].Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset;
            Color lightColor = Lighting.GetColor(i, j);
            if (frameY <= 16)
                lightColor = Color.White;

            spriteBatch.Draw(mainTexture, drawPosition, new Rectangle(frameX, frameY, 16, 16), lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

            // Draw lights over the flower.
            if (frameX == 36 && frameY == 0)
            {
                float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 0.4f + (i * 0.9444f + j * 0.3768f) % TwoPi;
                Vector2 glowDrawPosition = drawPosition + new Vector2(17f, 6f);
                spriteBatch.Draw(BloomFlare, glowDrawPosition, null, Color.Cyan with { A = 0 } * 0.4f, bloomFlareRotation, BloomFlare.Size() * 0.5f, 0.05f, 0, 0f);
                spriteBatch.Draw(BloomCircleSmall, glowDrawPosition, null, Color.Purple with { A = 0 } * 0.8f, 0f, BloomCircleSmall.Size() * 0.5f, 0.4f, 0, 0f);
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
            player.cursorItemIconID = ModContent.ItemType<DivineMonolith>();
        }
    }
}
