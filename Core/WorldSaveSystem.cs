using System.IO;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using SubworldLibrary;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static NoxusBoss.Content.NPCs.Bosses.Noxus.SpecificEffectManagers.NoxusEggCutsceneSystem;

namespace NoxusBoss.Core
{
    public class WorldSaveSystem : ModSystem
    {
        // This field is used for performance reasons, since it could be a bit unideal to be doing file existence checks many times every frame.
        private static bool? hasDefeatedNamelessDeityInAnyWorldField;

        // Toasty's QoL mod needs fields to access things. TECHNICALLY it's possible to attempt to get the compiler-generated backer field with hardcoded
        // strings and wacky reflection, but that's kind of unpleasant and something I'd rather not do.
        private static bool hasDefeatedNamelessDeity;

        private static bool hasDefeatedNoxus;

        public static int NamelessDeityDeathCount
        {
            get;
            set;
        }

        public static bool HasDefeatedNoxusEgg
        {
            get;
            set;
        }

        public static bool HasDefeatedNoxus
        {
            get => hasDefeatedNoxus;
            set
            {
                if (hasDefeatedNoxus != value)
                {
                    if (value && !Main.gameMenu && !Main.zenithWorld)
                        NoxusDefeatAnimationSystem.Start();

                    hasDefeatedNoxus = value;
                }
            }
        }

        public static bool HasDefeatedNamelessDeity
        {
            get => hasDefeatedNamelessDeity;
            set => hasDefeatedNamelessDeity = value;
        }

        public static bool HasMetNamelessDeity
        {
            get;
            set;
        }

        public static bool HasPlacedCattail
        {
            get;
            set;
        }

        public static bool HasDefeatedNamelessDeityInAnyWorld
        {
            get
            {
                hasDefeatedNamelessDeityInAnyWorldField ??= hasDefeatedNamelessDeityInAnyWorldField = File.Exists(NamelessDeityDefeatConfirmationFilePath);
                return hasDefeatedNamelessDeityInAnyWorldField.Value;
            }
            set
            {
                hasDefeatedNamelessDeityInAnyWorldField = value;
                if (!value)
                    File.Delete(NamelessDeityDefeatConfirmationFilePath);
                else
                {
                    var pathWriter = File.CreateText(NamelessDeityDefeatConfirmationFilePath);
                    pathWriter.WriteLine("The contents of this file don't matter, just that the file exists. Delete it if you want the Nameless Deity to not be marked as defeated.");
                    pathWriter.Close();
                }
            }
        }

        public static bool OgsculeRulesOverTheUniverse
        {
            get;
            set;
        }

        public static string NamelessDeityDefeatConfirmationFilePath => Main.SavePath + "\\NamelessDeityDefeatConfirmation.txt";

        public override void OnWorldLoad()
        {
            NamelessDeityBoss.Myself = null;
            if (SubworldSystem.AnyActive())
                return;

            NamelessDeityDeathCount = 0;
            HasDefeatedNoxusEgg = false;
            hasDefeatedNoxus = false;
            HasDefeatedNamelessDeity = false;
            HasMetNamelessDeity = false;
            OgsculeRulesOverTheUniverse = false;
            HasPlacedCattail = false;
            NoxusHasFallenFromSky = false;
        }

        public override void OnWorldUnload()
        {
            if (SubworldSystem.AnyActive())
                return;

            NamelessDeityDeathCount = 0;
            HasDefeatedNoxusEgg = false;
            hasDefeatedNoxus = false;
            HasDefeatedNamelessDeity = false;
            HasMetNamelessDeity = false;
            OgsculeRulesOverTheUniverse = false;
            HasPlacedCattail = false;
            NoxusHasFallenFromSky = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (HasDefeatedNoxusEgg)
                tag["HasDefeatedEgg"] = true;
            if (hasDefeatedNoxus)
                tag["HasDefeatedNoxus"] = true;
            if (HasDefeatedNamelessDeity)
                tag["HasDefeatedNamelessDeity"] = true;
            if (NoxusHasFallenFromSky)
                tag["NoxusHasFallenFromSky"] = true;
            if (HasMetNamelessDeity)
                tag["HasMetNamelessDeity"] = true;
            if (OgsculeRulesOverTheUniverse)
                tag["OgsculeRulesOverTheUniverse"] = true;
            if (HasPlacedCattail)
                tag["HasPlacedCattail"] = true;

            tag["NamelessDeityDeathCount"] = NamelessDeityDeathCount;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            HasDefeatedNoxusEgg = tag.ContainsKey("HasDefeatedEgg");
            hasDefeatedNoxus = tag.ContainsKey("HasDefeatedNoxus");
            HasDefeatedNamelessDeity = tag.ContainsKey("HasDefeatedNamelessDeity");
            NoxusHasFallenFromSky = tag.ContainsKey("NoxusHasFallenFromSky");
            HasMetNamelessDeity = tag.ContainsKey("HasMetNamelessDeity");
            OgsculeRulesOverTheUniverse = tag.ContainsKey("OgsculeRulesOverTheUniverse");
            HasPlacedCattail = tag.ContainsKey("HasPlacedCattail");

            NamelessDeityDeathCount = tag.GetInt("NamelessDeityDeathCount");
        }

        public override void NetSend(BinaryWriter writer)
        {
            BitsByte b1 = new();
            b1[0] = HasDefeatedNoxusEgg;
            b1[1] = hasDefeatedNoxus;
            b1[2] = HasDefeatedNamelessDeity;
            b1[3] = HasMetNamelessDeity;
            b1[4] = OgsculeRulesOverTheUniverse;
            b1[5] = HasPlacedCattail;
            b1[6] = NoxusHasFallenFromSky;

            writer.Write(b1);
            writer.Write(NamelessDeityDeathCount);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte b1 = reader.ReadByte();
            HasDefeatedNoxusEgg = b1[0];
            hasDefeatedNoxus = b1[1];
            HasDefeatedNamelessDeity = b1[2];
            HasMetNamelessDeity = b1[3];
            OgsculeRulesOverTheUniverse = b1[4];
            HasPlacedCattail = b1[5];
            NoxusHasFallenFromSky = b1[6];

            NamelessDeityDeathCount = reader.ReadInt32();
        }
    }
}
