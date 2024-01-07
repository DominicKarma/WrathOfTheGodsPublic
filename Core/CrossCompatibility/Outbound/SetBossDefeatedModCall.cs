using System;
using System.Collections.Generic;
using System.Linq;
using static NoxusBoss.Core.CrossCompatibility.Outbound.GetBossDefeatedModCall;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    public class SetBossDefeatedModCall : ModCallProvider
    {
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
                yield return typeof(bool);
            }
        }

        protected override object Process(params object[] args)
        {
            string caseInvariantInput = ((string)args[0]).ToLower();
            bool setValue = (bool)args[1];

            if (GodlessSpawnNames.Contains(caseInvariantInput))
                WorldSaveSystem.HasDefeatedNoxusEgg = setValue;

            if (EntropicGodNames.Contains(caseInvariantInput))
                WorldSaveSystem.HasDefeatedNoxus = setValue;

            if (NamelessDeityNames.Contains(caseInvariantInput))
                WorldSaveSystem.HasDefeatedNamelessDeity = setValue;

            return null;
        }
    }
}
