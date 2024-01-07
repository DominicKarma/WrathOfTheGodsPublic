using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        // This is a modular tooltip editor which loops over all tooltip lines of an item,
        // selects all those which match an arbitrary function you provide, and then edits them using another arbitrary function you provide.
        public static void ApplyTooltipEdits(IList<TooltipLine> lines, Item item, Func<Item, TooltipLine, bool> replacementCondition, Action<TooltipLine> replacementAction)
        {
            foreach (TooltipLine line in lines)
            {
                if (replacementCondition(item, line))
                    replacementAction(line);
            }
        }

        // This function produces simple predicates to match a specific line of a tooltip, by number/index.
        public static Func<Item, TooltipLine, bool> LineNum(int n) => (Item i, TooltipLine l) => l.Mod == "Terraria" && l.Name == $"Tooltip{n}";

        // This function is shorthand to invoke ApplyTooltipEdits using the above methods.
        public static void EditTooltipByNum(int lineNum, Item item, IList<TooltipLine> lines, Action<TooltipLine> action) => ApplyTooltipEdits(lines, item, LineNum(lineNum), action);
    }
}
