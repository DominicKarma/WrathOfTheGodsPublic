using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.ID;

namespace NoxusBoss.Core.Graphics.Automators
{
    public interface IDrawGroupedPrimitives
    {
        public int MaxVertices
        {
            get;
        }

        public int MaxIndices
        {
            get;
        }

        public PrimitiveGroupDrawContext DrawContext
        {
            get;
        }

        public ManagedShader Shader
        {
            get;
        }

        public PrimitiveTrailGroup Group => PrimitiveTrailGroupingSystem.GetGroup(GetType());

        public void PrepareShaderForPrimitives()
        {

        }

        public PrimitiveTrailInstance GenerateInstance(Entity owner)
        {
            // Don't bother trying to create an instance when the mod is being loaded.
            if (Main.gameMenu)
                return null;

            // Don't attempt to create an instance serverside.
            if (Main.netMode == NetmodeID.Server)
                return null;

            PrimitiveTrailInstance instance = new(owner);
            Group?.Add(instance);

            return instance;
        }
    }
}
