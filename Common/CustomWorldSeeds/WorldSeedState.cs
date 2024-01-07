using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;

namespace NoxusBoss.Common.CustomWorldSeeds
{
    public class WorldSeedState
    {
        public bool WasJustEnabledByWorldgen;

        public bool Enabled;

        public string SeedName;

        public List<string> ValidSeeds;

        public WorldSeedState(string seedName, params string[] validSeeds)
        {
            SeedName = seedName;
            ValidSeeds = validSeeds.ToList();
        }

        public void Save(TagCompound tag)
        {
            if (Enabled)
                tag[SeedName] = true;
        }

        public void Load(TagCompound tag) => Enabled = tag.ContainsKey(SeedName);

        public void Send(BitsByte storage, int currentBitIndex) => storage[currentBitIndex] = Enabled;

        public void Receive(BitsByte storage, int currentBitIndex) => Enabled = storage[currentBitIndex];
    }
}
