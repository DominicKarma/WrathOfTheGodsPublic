using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace NoxusBoss.Content.Tiles
{
    public class NamelessDeityTrophyTile : ModTile
    {
        private static Asset<Texture2D> tileTexture;

        private static Asset<Texture2D> eyelidTexture;

        private static Asset<Texture2D> scleraTexture;

        private static Asset<Texture2D> pupilTexture;

        public static int DelaySinceLastBlinkSound
        {
            get;
            set;
        }

        public static int DelayUntilWiredEyesGoDormant
        {
            get;
            set;
        }

        public static int AwakenTimer
        {
            get;
            set;
        }

        public static readonly SoundStyle BlinkSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Environment/NamelessDeityTrophyBlink") with { PitchVariance = 0.1f };

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.FramesOnKillWall[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.addTile(Type);

            AddMapEntry(new(120, 85, 60), Language.GetText("MapObject.Trophy"));
            DustType = 7;

            // Load texture assets. This way, tiles don't attempt to load them from the central registry via Request every time.
            if (Main.netMode != NetmodeID.Server)
            {
                tileTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/NamelessDeityTrophyTileFull");
                eyelidTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/NamelessDeityTrophyEyelid");
                scleraTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/NamelessDeityTrophySclera");
                pupilTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Tiles/NamelessDeityTrophyPupil");
            }
        }

        public override void HitWire(int i, int j)
        {
            Tile t = ParanoidTileRetrieval(i, j);
            int left = i - t.TileFrameX / 18;
            int top = j - t.TileFrameY / 18;

            // If the tile is in the second variant (aka frame 54 at the top left), shift it back to zero.
            // Otherwise, shift it to 54. This acts as a "toggle" for the effect.
            short tileFrameOffset = (short)(t.TileFrameX >= 54 ? 0 : 54);

            // Ensure that wire signals aren't fired multiple times from the trophy's subtiles.
            for (int dx = 0; dx < 3; dx++)
            {
                for (int dy = 0; dy < 3; dy++)
                {
                    if (dx != 0 || dy != 0)
                        Wiring.SkipWire(left + dx, top + dy);
                }
            }

            for (int dx = 0; dx < 3; dx++)
            {
                for (int dy = 0; dy < 3; dy++)
                    Main.tile[left + dx, top + dy].TileFrameX = (short)(dx * 18 + tileFrameOffset);
            }

            // Send a packet informing everyone of the tile state change.
            if (Main.netMode != NetmodeID.SinglePlayer)
                NetMessage.SendTileSquare(-1, left + 1, top + 1, 4, TileChangeType.None);
            DelayUntilWiredEyesGoDormant = 0;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile t = Main.tile[i, j];
            int frameX = t.TileFrameX % 54;
            int frameY = t.TileFrameY;

            // Everything is drawn by the top-left frame at once.
            if (frameX != 0 || frameY != 0)
                return false;

            // Draw the main tile texture.
            Texture2D mainTexture = tileTexture.Value;
            Vector2 drawOffset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 drawPosition = new Vector2(i * 16 - Main.screenPosition.X, j * 16 - Main.screenPosition.Y) + drawOffset;
            Color lightColor = Lighting.GetColor(i + 1, j + 1);
            if (!Main.tile[i, j].IsTileInvisible)
                spriteBatch.Draw(mainTexture, drawPosition, null, lightColor, 0f, Vector2.Zero, 1f, 0, 0f);

            // Calculate the direction to the player, to determine how the pupil should be oriented.
            // This does not look at the nearest player, it explicitly looks at the current client at all times.
            // Under typical circumstances this would be a bit weird, and somewhat illogical, but since this is a NamelessDeity item I think it makes for a pretty cool, albeit
            // subtle detail. Makes it as though Nameless is looking across multiple different realities at once or something.
            Vector2 worldPosition = new Point(i + 1, j + 1).ToWorldCoordinates();
            Vector2 offsetFromPlayer = Main.LocalPlayer.Center - worldPosition;

            // Calculate the pupil frame.
            int pupilFrame = 0;
            float pupilFrameTime = (Main.GlobalTimeWrappedHourly * 1.9f + i * 0.44f + j * 0.13f) % 15f;
            if (pupilFrameTime >= 14f)
                pupilFrame = (int)Remap(pupilFrameTime, 14f, 14.8f, 0f, 8f);

            // Check if the tile is activated by wiring. This is represented under the hood as whether FrameX is 54 or greater.
            // This is a LITTLE bit weird but it makes for the easiest packet syncing, since frame information is already a part of tile data syncing packets.
            bool activatedByWire = t.TileFrameX >= 54;
            bool drawEye = true;

            // If the tile is activated by wire, then the blinking animation is changed a bit.
            // Instead of idly blinking and looking at the player, the eye will wait until a player gets really close to one of them.
            // Once this happens, all wired eyes will activate at once and look at the player intently, until they go away.
            if (activatedByWire)
            {
                // Use dormant frames if inactive.
                DelayUntilWiredEyesGoDormant = Utils.Clamp(DelayUntilWiredEyesGoDormant - 1, 0, 300);
                if (DelayUntilWiredEyesGoDormant <= 0)
                {
                    pupilFrame = 6;
                    drawEye = false;
                    AwakenTimer = 0;
                }
                else
                {
                    pupilFrame = (int)Remap(AwakenTimer, 0f, 24f, 6f, 8f);
                    AwakenTimer++;
                }

                float triggerRange = DelayUntilWiredEyesGoDormant <= 0 ? 60f : 700f;
                if (offsetFromPlayer.Length() <= triggerRange)
                    DelayUntilWiredEyesGoDormant = 300;
            }

            // Play blinking sounds.
            DelaySinceLastBlinkSound = Utils.Clamp(DelaySinceLastBlinkSound - 1, 0, 30);
            if (DelaySinceLastBlinkSound <= 0 && (pupilFrame == 4 || pupilFrame == 5) && !activatedByWire && Main.instance.IsActive && offsetFromPlayer.Length() <= 900f)
            {
                DelaySinceLastBlinkSound = 30;
                SoundEngine.PlaySound(BlinkSound, worldPosition);
            }

            // Draw the eye over the tile.
            if (drawEye)
            {
                float eyeScale = 0.37f;
                float pupilScaleFactor = Remap(offsetFromPlayer.Length(), 30f, 142f, Sin(Main.GlobalTimeWrappedHourly * 50f + i + j * 13f) * 0.012f + 0.6f, 0.9f);

                if (activatedByWire)
                    pupilScaleFactor *= Remap(AwakenTimer, 0f, 30f, 0.3f, 1f);

                Texture2D sclera = scleraTexture.Value;
                Texture2D pupil = pupilTexture.Value;
                Texture2D eyelid = eyelidTexture.Value;
                Vector2 pupilOffset = (offsetFromPlayer * 0.1f).ClampLength(0f, 24f) * new Vector2(1f, 0.4f) * eyeScale;
                Rectangle eyelidFrameRectangle = eyelid.Frame(1, 9, 0, pupilFrame);
                drawPosition += Vector2.One * 24f;
                Main.spriteBatch.Draw(sclera, drawPosition, null, Color.White, 0f, sclera.Size() * 0.5f, eyeScale, 0, 0f);
                Main.spriteBatch.Draw(pupil, drawPosition + pupilOffset, null, Color.White, 0f, pupil.Size() * 0.5f, eyeScale * pupilScaleFactor, 0, 0f);
                Main.spriteBatch.Draw(eyelid, drawPosition, eyelidFrameRectangle, Color.White, 0f, eyelidFrameRectangle.Size() * 0.5f, eyeScale, 0, 0f);
            }

            return false;
        }
    }
}
