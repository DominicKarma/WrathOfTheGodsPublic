using System.IO;
using NoxusBoss.Common.SpecialWorldEvents;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Netcode.Packets
{
    public class NoxusFogTimePacket : BaseCustomPacket
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            packet.Write(NoxusFogEventManager.FogRestartCooldown);
            packet.Write(NoxusFogEventManager.FogTime);
            packet.Write(NoxusFogEventManager.FogDuration);
        }

        public override void Read(BinaryReader reader)
        {
            NoxusFogEventManager.FogRestartCooldown = reader.ReadInt32();
            NoxusFogEventManager.FogTime = reader.ReadInt32();
            NoxusFogEventManager.FogDuration = reader.ReadInt32();
        }
    }
}
