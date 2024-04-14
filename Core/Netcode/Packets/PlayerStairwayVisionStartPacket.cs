using System.IO;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.GlobalItems;
using Terraria;
using Terraria.ModLoader;
using static NoxusBoss.Core.FixUnconsciousPlayersBeingVisibleInMP;

namespace NoxusBoss.Core.Netcode.Packets
{
    public class PlayerStairwayVisionStartPacket : Packet
    {
        public override void Write(ModPacket packet, params object[] context)
        {
            Vector2 unconsciousPosition = (Vector2)context[1];
            packet.Write((int)context[0]);
            packet.WriteVector2(unconsciousPosition);
        }

        public override void Read(BinaryReader reader)
        {
            Player p = Main.player[reader.ReadInt32()];
            NoxusPlayer modPlayer = p.GetModPlayer<NoxusPlayer>();

            modPlayer.GetValueRef<bool>(IsExperiencingStairwayVisionField).Value = true;
            modPlayer.GetValueRef<Vector2>(PositionBeforeVisionField).Value = reader.ReadVector2();
        }
    }
}
