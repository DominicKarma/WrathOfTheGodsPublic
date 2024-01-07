using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeityMusicScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)10;

        public override int Music => NamelessDeityBoss.Myself?.ModNPC.Music ?? 0;

        public override bool IsSceneEffectActive(Player player) => NamelessDeityBoss.Myself is not null;
    }
}
