using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NoxusBoss.Core.Netcode.Packets;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NoxusBoss.Core.Netcode
{
    // NOTE -- This system or more or less equivalent to the one I wrote for Infernum a while ago.
    public class PacketManager : ModSystem
    {
        internal static Dictionary<string, BaseCustomPacket> RegisteredPackets = new();

        public override void OnModLoad()
        {
            RegisteredPackets = new();
            foreach (Type t in AssemblyManager.GetLoadableTypes(Mod.Code))
            {
                if (!t.IsSubclassOf(typeof(BaseCustomPacket)) || t.IsAbstract)
                    continue;

                BaseCustomPacket packet = Activator.CreateInstance(t) as BaseCustomPacket;
                RegisteredPackets[t.FullName] = packet;
            }
        }

        internal static void PreparePacket(BaseCustomPacket packet, object[] context, short? sender = null)
        {
            // Don't try to send packets in singleplayer.
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            // Assume the sender is the current client if nothing else is supplied.
            sender ??= (short)Main.myPlayer;

            ModPacket wrapperPacket = NoxusBoss.Instance.GetPacket();

            // Write the identification header. This is necessary to ensure that on the receiving end the reader know how to interpret the packet.
            wrapperPacket.Write(packet.GetType().FullName);

            // Write the sender and original context if the packet needs to be re-sent from the server.
            if (packet.ResendFromServer)
            {
                wrapperPacket.Write(sender.Value);

                // Send the context data.
                BinaryFormatter contextFormatter = new();
                using MemoryStream stream = new();

                byte[] contextBytes = stream.ToArray();
                contextFormatter.Serialize(stream, context);
                wrapperPacket.Write(contextBytes.Length);
                wrapperPacket.Write(contextBytes);
            }

            // Write the requested packet data.
            packet.Write(wrapperPacket, context);

            // Send the packet.
            wrapperPacket.Send(-1, sender.Value);
        }

        public static void SendPacket<T>(params object[] context) where T : BaseCustomPacket
        {
            // Verify that the packet is registered before trying to send it.
            string packetName = typeof(T).FullName;
            if (Main.netMode == NetmodeID.SinglePlayer || !RegisteredPackets.TryGetValue(packetName, out BaseCustomPacket packet))
                return;

            PreparePacket(packet, context);
        }

        public static void ReceivePacket(BinaryReader reader)
        {
            // Read the identification header to determine how the packet should be processed.
            string packetName = reader.ReadString();

            // If no valid packet could be found, get out of here.
            // There will inevitably be a reader underflow error caused by TML's packet policing, but there aren't any clear-cut solutions that
            // I know of that adequately addresses that problem, and from what I can tell it's never catastrophic when it happens.
            if (!RegisteredPackets.TryGetValue(packetName, out BaseCustomPacket packet))
                return;

            // Determine who sent this packet if it needs to resend.
            short sender = -1;
            object[] context = null;
            if (packet.ResendFromServer)
            {
                sender = reader.ReadInt16();
                int contextByteCount = reader.ReadInt32();
                byte[] contextBytes = reader.ReadBytes(contextByteCount);

                BinaryFormatter contextFormatter = new();
                using MemoryStream stream = new(contextBytes);
                context = (object[])contextFormatter.Deserialize(stream);
            }

            // Read off requested packet data.
            packet.Read(reader);

            // If this packet was received server-side and the packet needs to be re-sent, send it back to all the clients, with the
            // exception of the one that originally brought this packet to the server.
            if (Main.netMode == NetmodeID.Server && packet.ResendFromServer)
                PreparePacket(packet, context, sender);
        }
    }
}
