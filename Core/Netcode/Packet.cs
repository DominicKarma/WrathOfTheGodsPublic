using System.IO;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Netcode
{
    public abstract class Packet
    {
        // This determines whether a packet should be sent back to clients once on the server. This applies in cases where a client
        // needs to inform the server of a change, and the packet can't be sent from the server itself (such as if a player makes a left click).
        // This is important because it isn't enough to just send a packet and be done with it, as TML has hidden rules with its packet structure:
        // 1. Packets sent from clients go to the server.
        // 2. Packets sent from the server go to the clients (with the optional exception of one client if you supply a client that should not recieve the packet).

        // As a rule of thumb, leave this as true if you're doing anything client-specific related, such as taking in player inputs.
        // If it's for something server-specific (such as world operations) this doesn't matter much.
        public virtual bool ResendFromServer => true;

        // RULES FOR SETTING UP PACKETS
        // 1: Make sure to read things in the order they were written in, like a conveyer belt. The computer is not a magic device that can infer usage context, it simply
        // receives a stream of bytes. As such, it's your job to ensure that the data is interpreted correctly.

        // 1.1: Be careful when reading data in conditionals, as they might not be triggered, resulting in violations of rule 1.

        // 1.2: Never use reader.ReadInt32() or similar things inside of a loop iterator directly. If you need to keep a counter for a loop, store it as a separate local variable.
        // Using the Read methods inside of a loop check will involve going through 4 bytes *for every loop iteration*, which is pretty much never
        // the behavior you want (and if it is, you'd best leave a comment to ensure that readers know why).

        // 2: Do your best to ensure that ALL data is read, even in failure cases. If you can, try to collect all BinaryReader information in local variables at the top
        // of the Read hook, and then once that's complete perform any necessary early returns/failure cases if said data is garbage.
        public abstract void Write(ModPacket packet, params object[] context);

        public abstract void Read(BinaryReader reader);
    }
}
