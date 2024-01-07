using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.States;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Common.CustomWorldSeeds
{
    public class CustomWorldSeedManager : ModSystem
    {
        private static readonly Dictionary<string, WorldSeedState> worldSeeds = new();

        public override void OnModLoad()
        {
            On_UIWorldCreation.ProcessSpecialWorldSeeds += HandleSpecialSeeds;
        }

        private void HandleSpecialSeeds(On_UIWorldCreation.orig_ProcessSpecialWorldSeeds orig, string processedSeed)
        {
            // Check through all seeds to see if there's a match.
            foreach (var seedState in worldSeeds.Values)
            {
                // If any of the valid seed configurations match the inputted seed, then it is marked as enabled. This process does not care about letter case, but it does care about spacing.
                if (seedState.ValidSeeds.Any(s => string.Equals(processedSeed, s, StringComparison.OrdinalIgnoreCase)))
                {
                    seedState.Enabled = true;
                    seedState.WasJustEnabledByWorldgen = true;
                }
            }
        }

        public static void RegisterSeed(string seedName, params string[] validSeeds) => worldSeeds[seedName] = new(seedName, validSeeds);

        public static bool IsSeedActive(string seedName) => worldSeeds.TryGetValue(seedName, out WorldSeedState state) && state.Enabled;

        public override void OnWorldLoad()
        {
            // Disable all seeds that were not just generated.
            foreach (var seedState in worldSeeds.Values)
                seedState.Enabled = seedState.WasJustEnabledByWorldgen;
        }

        public override void OnWorldUnload()
        {
            // Disable all seeds.
            foreach (var seedState in worldSeeds.Values)
            {
                seedState.WasJustEnabledByWorldgen = false;
                seedState.Enabled = false;
            }
        }

        // Save and load seed data to the world.
        public override void SaveWorldData(TagCompound tag)
        {
            foreach (var seedState in worldSeeds.Values)
                seedState.Save(tag);
        }

        public override void LoadWorldData(TagCompound tag)
        {
            foreach (var seedState in worldSeeds.Values)
                seedState.Load(tag);
        }

        // Send and receive seed data across the network.
        public override void NetSend(BinaryWriter writer)
        {
            // Create the binary data holders.
            int currentBitIndex = 0;
            List<BitsByte> storage = new()
            {
                0
            };

            // Fill the bits with seed data.
            foreach (var seedState in worldSeeds.Values)
            {
                seedState.Send(storage[^1], currentBitIndex);
                currentBitIndex++;

                // Create a new byte if the current one's capacity has been exceeded.
                if (currentBitIndex >= 8)
                {
                    storage.Add(0);
                    currentBitIndex = 0;
                }
            }

            // Send the binary data.
            writer.Write(storage.Count);
            foreach (var s in storage)
                writer.Write(s);
        }

        public override void NetReceive(BinaryReader reader)
        {
            // Check how many bytes need to be read.
            int byteCount = reader.ReadInt32();

            // Read all bytes.
            List<BitsByte> storage = new();
            for (int i = 0; i < byteCount; i++)
                storage.Add(reader.ReadByte());

            // Take the contents of the bytes and store them in their respective boolean flags.
            int currentBitIndex = 0;
            foreach (var seedState in worldSeeds.Values)
                seedState.Receive(storage[currentBitIndex / 8], currentBitIndex % 8);
        }
    }
}
