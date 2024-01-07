using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Items.SummonItems;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.GlobalItems;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    [Autoload(Side = ModSide.Client)]
    public class OgsculeOverlaySystem : ModSystem
    {
        public static Asset<Texture2D> OgsculeTexture
        {
            get;
            private set;
        }

        public static Vector2 DisclaimerPosition
        {
            get;
            set;
        }

        public static Vector2 DisclaimerVelocity
        {
            get;
            set;
        }

        public static bool OgsculeRulesOverTheUniverse => Main.zenithWorld && WorldSaveSystem.OgsculeRulesOverTheUniverse && NamelessDeityBoss.Myself is null;

        public override void OnModLoad()
        {
            if (Main.netMode != NetmodeID.Server)
                OgsculeTexture = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Ogscule");

            Main.OnPostDraw += DrawOgscule;
            NoxusGlobalItem.UseItemEvent += MakeGFBTerminusSummonOgscule;
        }

        public override void OnModUnload() => Main.OnPostDraw -= DrawOgscule;

        private static void MakeGFBTerminusSummonOgscule(Item item, Player player)
        {
            // Be afraid.
            if (item.type == FakeTerminus.TerminusID && Main.zenithWorld)
                WorldSaveSystem.OgsculeRulesOverTheUniverse = true;
        }

        private void DrawOgscule(GameTime obj)
        {
            // Don't draw anything on the game menu.
            if (Main.gameMenu)
                return;

            if (!OgsculeRulesOverTheUniverse)
            {
                DisclaimerPosition = Main.ScreenSize.ToVector2() * 0.5f - Vector2.UnitY * 200f;
                DisclaimerVelocity = new Vector2(4f, 4f);
                return;
            }

            // Draw the ogscule overlay.
            Main.spriteBatch.Begin();

            // Load the 'don't annoy the Cal devs about this' text from localization.
            string text = Language.GetTextValue("Mods.NoxusBoss.Dialog.OgsculeDontAnnoyCalDevsText");

            float textScale = Sin(Main.GlobalTimeWrappedHourly * 3.5f) * 0.03f + 0.55f;
            Color gay = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 4f % 1f, 0.5f, 0.45f);
            Vector2 ogsculeScale = Main.ScreenSize.ToVector2() / OgsculeTexture.Size();
            Main.spriteBatch.Draw(OgsculeTexture.Value, Main.ScreenSize.ToVector2() * 0.5f, null, Color.White * 0.35f, 0f, OgsculeTexture.Size() * 0.5f, ogsculeScale, 0, 0f);
            Main.spriteBatch.DrawString(FontAssets.DeathText.Value, text, DisclaimerPosition, gay, 0f, Vector2.Zero, textScale, 0, 0f);

            // Update the ogscule position.
            DisclaimerPosition += DisclaimerVelocity;
            if (DisclaimerPosition.X <= 0f || DisclaimerPosition.X >= Main.screenWidth - 940f)
                DisclaimerVelocity = Vector2.Reflect(DisclaimerVelocity, Vector2.UnitX);
            if (DisclaimerPosition.Y <= 0f || DisclaimerPosition.Y >= Main.screenHeight - 32f)
                DisclaimerVelocity = Vector2.Reflect(DisclaimerVelocity, Vector2.UnitY);
            DisclaimerPosition = Vector2.Clamp(DisclaimerPosition, new Vector2(1f, 1f), new Vector2(Main.screenWidth - 941f, Main.screenHeight - 33f));

            Main.spriteBatch.End();
        }
    }

    public class OgsculeOverlayScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)100;

        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Sounds/Music/Ogscule");

        public override bool IsSceneEffectActive(Player player) => OgsculeOverlaySystem.OgsculeRulesOverTheUniverse;
    }
}
