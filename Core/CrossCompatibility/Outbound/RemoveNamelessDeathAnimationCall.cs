using System;
using System.Collections.Generic;
using NoxusBoss.Content.NPCs.Bosses.NamelessDeity.SpecificEffectManagers;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    // This is super niche but Toasty asked for it for an addon mod of his.
    public class RemoveNamelessDeathAnimationCall : ModCallProvider
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "MakeNextNamelessDeathAnimationNotHappen";
            }
        }

        public override string Name => "MakeNextNamelessDeathAnimationNotHappen";

        public override IEnumerable<Type> InputTypes => Array.Empty<Type>();

        protected override object Process(params object[] args)
        {
            NamelessDeathAnimationSkipSystem.SkipNextDeathAnimation = true;
            return new();
        }
    }
}
