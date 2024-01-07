using System;
using System.Collections.Generic;
using System.Linq;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    public class GetBossDefeatedModCall : ModCallProvider<bool>
    {
        internal static string[] GodlessSpawnNames = new string[]
        {
            "godlessspawn",
            "godless spawn",
            "noxusegg",
            "noxus egg",
        };

        internal static string[] EntropicGodNames = new string[]
        {
            "entropicgod",
            "entropic god",
            "noxus",
        };

        internal static string[] NamelessDeityNames = new string[]
        {
            "namelessdeity",
            "nameless deity",
        };

        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "GetBossDefeated";
            }
        }

        public override string Name => "GetBossDefeated";

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(string);
            }
        }

        protected override bool ProcessGeneric(params object[] args)
        {
            string caseInvariantInput = ((string)args[0]).ToLower();

            if (GodlessSpawnNames.Contains(caseInvariantInput))
                return WorldSaveSystem.HasDefeatedNoxusEgg;

            if (EntropicGodNames.Contains(caseInvariantInput))
                return WorldSaveSystem.HasDefeatedNoxus;

            if (NamelessDeityNames.Contains(caseInvariantInput))
                return WorldSaveSystem.HasDefeatedNamelessDeity;

            return false;
        }
    }
}
