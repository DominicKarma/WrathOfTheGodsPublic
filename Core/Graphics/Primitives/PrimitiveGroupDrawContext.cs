using System;

namespace NoxusBoss.Core.Graphics
{
    [Flags]
    public enum PrimitiveGroupDrawContext
    {
        Pixelated = 0b0000001,
        BeforeProjectiles = 0b0000010,
        AfterProjectiles = 0b0000100,
        AfterNPCs = 0b0001000,
        Manual = 0b0010000
    }
}
