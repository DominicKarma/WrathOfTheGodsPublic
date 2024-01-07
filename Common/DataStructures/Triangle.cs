using Microsoft.Xna.Framework;

namespace NoxusBoss.Common.DataStructures
{
    public readonly struct Triangle
    {
        public readonly Vector2 Vertex1;

        public readonly Vector2 Vertex2;

        public readonly Vector2 Vertex3;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            Vertex1 = a;
            Vertex2 = b;
            Vertex3 = c;
        }
    }
}
