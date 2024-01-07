using System;

namespace NoxusBoss.Common.Utilities
{
    public static partial class Utilities
    {
        /// <summary>
        /// Gives the <b>real</b> modulo of a divided by a divisor.
        /// This method is necessary because the % operator in C# keeps the sign of the dividend.
        /// </summary>
        public static float Modulo(this float dividend, float divisor)
        {
            return dividend - (float)Math.Floor(dividend / divisor) * divisor;
        }
    }
}
