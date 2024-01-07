using System;
using Terraria;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Enables toggled tile states on the X frame based on whether a wire signal is flowing through the tile.
        /// <br></br>
        /// This applies for multiframed tiles.
        /// </summary>
        /// <param name="type">The tile's ID.</param>
        /// <param name="x">The X position of the tile.</param>
        /// <param name="y">The Y position of the tile.</param>
        /// <param name="tileWidth">The width of the overall tile.</param>
        /// <param name="tileHeight">The height of overall tile.</param>
        public static void LightHitWire(int type, int x, int y, int tileWidth, int tileHeight)
        {
            // Calculate the top left of the tile by offsetting relative to its frame data.
            int topLeftX = x - Main.tile[x, y].TileFrameX / 18 % tileWidth;
            int topLeftY = y - Main.tile[x, y].TileFrameY / 18 % tileHeight;

            for (int l = topLeftX; l < topLeftX + tileWidth; l++)
            {
                for (int m = topLeftY; m < topLeftY + tileHeight; m++)
                {
                    if (Main.tile[l, m].HasTile && Main.tile[l, m].TileType == type)
                    {
                        if (Main.tile[l, m].TileFrameX < tileWidth * 18)
                            Main.tile[l, m].TileFrameX += (short)(tileWidth * 18);
                        else
                            Main.tile[l, m].TileFrameX -= (short)(tileWidth * 18);
                    }
                }
            }

            // A wire signal was processed. That means it doesn't need to happen again for all the subtiles.
            // Use SkipWire to prevent this.
            if (Wiring.running)
            {
                for (int k = 0; k < tileWidth; k++)
                {
                    for (int l = 0; l < tileHeight; l++)
                        Wiring.SkipWire(topLeftX + k, topLeftY + l);
                }
            }
        }

        /// <summary>
        /// Performs an index-safe tile retrieval. If this mistakenly attempts to access a tile outside of the world, it returns a default, empty tile rather than throwing an <see cref="IndexOutOfRangeException"/>.
        /// </summary>
        /// <param name="x">The X position of the tile.</param>
        /// <param name="y">The Y position of the tile.</param>
        public static Tile ParanoidTileRetrieval(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
                return new();

            return Main.tile[x, y];
        }
    }
}
