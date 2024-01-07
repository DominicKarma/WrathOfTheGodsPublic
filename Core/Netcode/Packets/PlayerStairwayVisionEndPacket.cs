using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Netcode.Packets
{
    public class PlayerStairwayVisionEndPacket : BaseCustomPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            packet.Write((int)context[0]);
        }

        public override void Read(BinaryReader reader)
        {
            Player p = Main.player[reader.ReadInt32()];
            p.GetValueRef<bool>(FixUnconsciousPlayersBeingVisibleInMP.IsExperiencingStairwayVisionField).Value = false;
        }
    }
}
