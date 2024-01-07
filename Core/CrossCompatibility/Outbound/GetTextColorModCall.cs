using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using NoxusBoss.Core.Graphics;

namespace NoxusBoss.Core.CrossCompatibility.Outbound
{
    // This mainly exists for the person maintaining the Noxus voice acted dialog addon, so that they can standardize their dialog strings with the rest of the mod without copypasting.
    // Anyone else is free to make use of it if they want though.
    public class GetTextColorModCall : ModCallProvider<Color>
    {
        public override IEnumerable<string> CallCommands
        {
            get
            {
                yield return "GetTextColor";
            }
        }

        public override string Name => "GetTextColor";

        public override IEnumerable<Type> InputTypes
        {
            get
            {
                yield return typeof(string);
            }
        }

        protected override Color ProcessGeneric(params object[] args)
        {
            string colorName = (string)args[0];
            return (Color)(typeof(DialogColorRegistry).GetProperty(colorName)?.GetValue(null) ?? Color.Transparent);
        }
    }
}
