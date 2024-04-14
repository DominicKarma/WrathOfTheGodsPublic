using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static NoxusBoss.Core.Graphics.InfiniteStairways.NamelessDeityInfiniteStairwayManager;

namespace NoxusBoss.Core.Graphics.InfiniteStairways
{
    public class TileDisabler : ModSystem
    {
        public override void PostUpdatePlayers()
        {
            // Go through tiles.
            if (TilesAreUninteractable && (Collision.SolidCollision(Main.LocalPlayer.TopLeft, Main.LocalPlayer.width, Main.LocalPlayer.height) || Collision.WetCollision(Main.LocalPlayer.TopLeft, Main.LocalPlayer.width, Main.LocalPlayer.height)) && Main.LocalPlayer.velocity.Y > 0f)
                Main.LocalPlayer.position.Y += 4f;
        }

        public override void OnModLoad()
        {
            On_Main.DoDraw_Tiles_Solid += MakeTilesInvisible_SolidLayer;
            On_Main.DoDraw_Tiles_NonSolid += MakeTilesInvisible_NonSolidLayer;
            On_Main.DoDraw_WallsAndBlacks += MakeWallsInvisible;
            On_Main.DrawLiquid += MakeLiquidsInvisible;
            On_WaterfallManager.Draw += MakeWaterfallsInvisible;
            On_Main.DrawBackgroundBlackFill += TemporarilyDisableBlackDrawing;
            On_Collision.TileCollision += DisableTileCollision;
            On_Collision.SlopeCollision += DisableSlopeCollision;
            On_Main.DrawWires += DisableWireDrawing;
        }

        private void MakeTilesInvisible_SolidLayer(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
        {
            if (!TilesAreUninteractable)
                orig(self);
        }

        private void MakeTilesInvisible_NonSolidLayer(On_Main.orig_DoDraw_Tiles_NonSolid orig, Main self)
        {
            if (!TilesAreUninteractable)
                orig(self);
            else
                Draw();
        }

        private void MakeWallsInvisible(On_Main.orig_DoDraw_WallsAndBlacks orig, Main self)
        {
            if (!TilesAreUninteractable)
                orig(self);
        }

        private void MakeLiquidsInvisible(On_Main.orig_DrawLiquid orig, Main self, bool bg, int waterStyle, float alpha, bool drawSinglePassLiquids)
        {
            if (!TilesAreUninteractable)
                orig(self, bg, waterStyle, alpha, drawSinglePassLiquids);
        }

        private void MakeWaterfallsInvisible(On_WaterfallManager.orig_Draw orig, WaterfallManager self, SpriteBatch spriteBatch)
        {
            if (!TilesAreUninteractable)
                orig(self, spriteBatch);
        }

        private Vector2 DisableTileCollision(On_Collision.orig_TileCollision orig, Vector2 Position, Vector2 Velocity, int Width, int Height, bool fallThrough, bool fall2, int gravDir)
        {
            if (!TilesAreUninteractable)
                return orig(Position, Velocity, Width, Height, fallThrough, fall2, gravDir);

            return Velocity;
        }


        private Vector4 DisableSlopeCollision(On_Collision.orig_SlopeCollision orig, Vector2 Position, Vector2 Velocity, int Width, int Height, float gravity, bool fall)
        {
            if (!TilesAreUninteractable)
                return orig(Position, Velocity, Width, Height, gravity, fall);

            return new(Position.X, Position.Y, Velocity.X, Velocity.Y);
        }

        private void DisableWireDrawing(On_Main.orig_DrawWires orig, Main self)
        {
            if (!TilesAreUninteractable)
                orig(self);
        }

        private void TemporarilyDisableBlackDrawing(On_Main.orig_DrawBackgroundBlackFill orig, Main self)
        {
            if (DisableBlackCountdown > 0)
                DisableBlackCountdown--;
            else
                orig(self);
        }
    }
}
