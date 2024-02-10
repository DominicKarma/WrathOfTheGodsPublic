using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.Inbound.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public class WikithisCompatibilitySystem : ModSystem
    {
        public static bool CompatibilityEnabled => true;

        public const string WikiURL = "https://terrariamods.wiki.gg/wiki/{}";

        public override void PostSetupContent()
        {
            // Wikithis is client-side, and should not be accessed on servers.
            if (Main.netMode == NetmodeID.Server)
                return;

            // Don't load anything if Wikithis is not enabled.
            if (Wikithis is null)
                return;

            // Don't load if compatibility is disabled.
            // Note: After discussing it, this will be disabled at mod release time, with a patch turning this on once ready.
            if (!CompatibilityEnabled)
                return;

            // Register the wiki URL.
            Wikithis.Call("AddModURL", Mod, WikiURL);

            // Register the wiki texture.
            Wikithis.Call("AddWikiTexture", Mod, ModContent.Request<Texture2D>("NoxusBoss/Core/CrossCompatibility/Inbound/WikiThis/WikiThisIcon"));

            // Clear up name conflicts.
            static void EnemyRedirect(int npcID, string pageName)
            {
                Wikithis.Call("NPCIDReplacement", npcID, pageName);
            }
            EnemyRedirect(ModContent.NPCType<NamelessDeityBoss>(), "Nameless Deity of Light");
        }
    }
}
