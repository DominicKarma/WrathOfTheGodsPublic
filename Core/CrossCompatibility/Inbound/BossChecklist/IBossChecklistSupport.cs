using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace NoxusBoss.Core.CrossCompatibility.Inbound
{
    public interface IBossChecklistSupport
    {
        public bool IsMiniboss
        {
            get;
        }

        public string ChecklistEntryName
        {
            get;
        }

        // Here are the boss values from vanilla and Calamity, to give a bit of context:
        // King Slime = 1
        // Desert Scourge = 1.6
        // Eye of Cthulhu = 2
        // Crabulon = 2.7
        // Eater of Worlds and Brain of Cthulhu = 3
        // Hive Mind = 3.98
        // Perforators = 3.99
        // Queen Bee = 4
        // Skeletron = 5
        // Deerclops = 6
        // Slime God = 6.5
        // Wall of Flesh = 7
        // Queen Slime = 8
        // Cryogen = 8.5
        // The Twins = 9
        // Aquatic Scourge = 9.5
        // The Destroyer = 10
        // Brimstone Elemental = 10.5
        // Skeletron Prime = 11
        // Calamitas' Clone = 11.7
        // Plantera = 12
        // Leviathan = 12.8
        // Astrum Aureus = 12.81
        // Golem = 13
        // Duke Fishron = 14
        // The Plaguebringer Goliath = 14.5
        // Empress of Light = 15
        // Betsy = 16 (I don't know why this is counted by Boss Checklist as a proper boss with its own tier stepup but it is)
        // Ravager = 16.5
        // Lunatic Cultist = 17
        // Astrum Deus = 17.5
        // Moon Lord = 18
        // Profaned Guardians = 18.5
        // Dragonfolly = 18.6
        // Providence = 19.01 (Slightly above 19 because Thorium seemingly placed Ragnarock as 19)
        // Ceaseless Void = 19.5
        // Storm Weaver = 19.51
        // Signus = 19.52
        // Polterghast = 20
        // Old Duke = 20.5
        // The Devourer of Gods = 21
        // Yharon = 22
        // Exo Mechs = 22.5
        // Calamitas = 23
        // Eidolon Wyrm = 23.5
        // Boss Rush = 23.75 (Yes this technically isn't real in this mod but altering existing Boss Checklist entries is nightmarish)
        public float ProgressionValue
        {
            get;
        }

        public bool IsDefeated
        {
            get;
        }

        public List<int> Collectibles
        {
            get;
        }

        public int? SpawnItem => null;

        public bool UsesCustomPortraitDrawing => false;

        public void DrawCustomPortrait(SpriteBatch spriteBatch, Rectangle area, Color color) { }
    }
}
