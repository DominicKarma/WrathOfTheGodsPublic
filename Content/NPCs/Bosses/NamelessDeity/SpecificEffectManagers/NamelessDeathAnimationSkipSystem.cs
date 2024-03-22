using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers
{
    public class NamelessDeathAnimationSkipSystem : ModSystem
    {
        /// <summary>
        /// Whether Nameless should skip his death animation and immediately provide loot upon being defeated, rather than sending the player to the title screen.
        /// </summary>
        /// 
        /// <remarks>
        /// This exists for use by the RemoveNamelessDeathAnimationCall mod call.
        /// </remarks>
        public static bool SkipNextDeathAnimation
        {
            get;
            set;
        }

        public override void OnWorldLoad() => SkipNextDeathAnimation = false;

        public override void OnWorldUnload() => SkipNextDeathAnimation = false;

        public override void NetSend(BinaryWriter writer) => writer.Write((byte)SkipNextDeathAnimation.ToInt());

        public override void NetReceive(BinaryReader reader) => SkipNextDeathAnimation = reader.ReadByte() != 0;
    }
}
