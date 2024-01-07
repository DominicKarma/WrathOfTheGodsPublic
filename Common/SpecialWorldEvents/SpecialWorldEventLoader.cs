using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace NoxusBoss.Common.SpecialWorldEvents
{
    public class SpecialWorldEventLoader : ModSystem
    {
        private static Dictionary<Type, SpecialWorldEventHandler> customEvents;

        public static bool AnyEventOngoing => customEvents.Any(e => e.Value.EventOngoing);

        public static bool SpecificEventOngoing<T>() where T : SpecialWorldEventHandler
        {
            return customEvents[typeof(T)].EventOngoing;
        }

        public override void OnModLoad()
        {
            customEvents = new();
            foreach (var content in Mod.GetContent().Where(c => c is SpecialWorldEventHandler))
                customEvents[content.GetType()] = (SpecialWorldEventHandler)content;
        }
    }
}
