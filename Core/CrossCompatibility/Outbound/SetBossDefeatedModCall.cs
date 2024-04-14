using System;
using System.Collections.Generic;
using System.Linq;
using Luminance.Core.ModCalls;
using static NoxusBoss.Core.CrossCompatibility.Outbound.GetBossDefeatedModCall;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    public class SetBossDefeatedModCall : ModCall
    {
        public override IEnumerable<string> GetCallCommands()
        {
            yield return "GetBossDefeated";
        }

        public override IEnumerable<Type> GetInputTypes()
        {
            yield return typeof(string);
            yield return typeof(bool);
        }

        protected override object SafeProcess(params object[] argsWithoutCommand)
        {
            string caseInvariantInput = ((string)argsWithoutCommand[0]).ToLower();
            bool setValue = (bool)argsWithoutCommand[1];

            if (GodlessSpawnNames.Contains(caseInvariantInput))
                WorldSaveSystem.HasDefeatedNoxusEgg = setValue;

            if (EntropicGodNames.Contains(caseInvariantInput))
                WorldSaveSystem.HasDefeatedNoxus = setValue;

            if (NamelessDeityNames.Contains(caseInvariantInput))
                WorldSaveSystem.HasDefeatedNamelessDeity = setValue;

            return new();
        }
    }
}
