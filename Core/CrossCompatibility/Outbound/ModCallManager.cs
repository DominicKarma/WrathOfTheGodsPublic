using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    public class ModCallManager : ModSystem
    {
        private static readonly List<ModCallProvider> modCalls = new();

        public override void OnModLoad()
        {
            // Load all mod calls.
            foreach (Type t in AssemblyManager.GetLoadableTypes(Mod.Code))
            {
                if (!t.IsSubclassOf(typeof(ModCallProvider)) || t.IsAbstract)
                    continue;

                ModCallProvider call = (ModCallProvider)Activator.CreateInstance(t);
                call.Load();
                modCalls.Add(call);
            }
        }

        internal static object Call(params object[] args)
        {
            if (!args.Any())
                throw new ArgumentException("There must be at least one argument in order to use mod calls!");
            if (args[0] is not string command)
                throw new ArgumentException("The first argument must supply a string that specifies the mod call's type!");

            foreach (ModCallProvider call in modCalls)
            {
                bool callMatch = call.CallCommands.Any(c => c.Equals(command, StringComparison.OrdinalIgnoreCase));

                // Process the mod call if a match was found. The first argument is ommitted since that's simply the command, which was already used.
                // Error handling regarding inputs is performed via the ProcessInternal method.
                if (callMatch)
                    return call.ProcessInternal(args.Skip(1).ToArray());
            }

            return null;
        }
    }
}
