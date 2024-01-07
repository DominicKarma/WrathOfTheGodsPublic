using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;

namespace NoxusBoss.Core.Graphics.Shaders.Screen
{
    public class GenericScreenShaderData : ScreenShaderData
    {
        public GenericScreenShaderData(string passName)
            : base(passName)
        {
        }

        public GenericScreenShaderData(Ref<Effect> shader, string passName)
            : base(shader, passName)
        {
        }

        public override void Apply()
        {
            UseTargetPosition(Main.LocalPlayer.Center);
            UseColor(Color.Transparent);
            base.Apply();
        }
    }
}
