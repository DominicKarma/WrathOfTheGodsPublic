using System;
using System.Collections.Generic;
using System.Linq;
using Luminance.Core.ModCalls;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    public class GetBossDefeatedModCall : ModCall
    {
        internal static string[] GodlessSpawnNames =
        [
            "godlessspawn",
            "godless spawn",
            "noxusegg",
            "noxus egg",
        ];

        internal static string[] EntropicGodNames =
        [
            "entropicgod",
            "entropic god",
            "noxus",
        ];

        internal static string[] NamelessDeityNames =
        [
            "namelessdeity",
            "nameless deity",
        ];

        public override IEnumerable<string> GetCallCommands()
        {
            yield return "GetBossDefeated";
        }

        public override IEnumerable<Type> GetInputTypes()
        {
            yield return typeof(string);
        }

        protected override object SafeProcess(params object[] argsWithoutCommand)
        {
            string caseInvariantInput = ((string)argsWithoutCommand[0]).ToLower();

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
