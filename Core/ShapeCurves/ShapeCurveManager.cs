using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace NoxusBoss.Core.ShapeCurves
{
    public class ShapeCurveManager : ModSystem
    {
        private static readonly Dictionary<string, ShapeCurve> shapes = new();

        public override void OnModLoad()
        {
            // Load all shape points.
            // In binary they are simply stored as a list of paired, unordered X/Y floats. They are normalized such that their values never exceed a 0 to 1 range, and can thusly
            // be scaled up easily via the inbuilt ShapeCurve methods.
            foreach (var path in Mod.GetFileNames().Where(f => f.Contains("Core/ShapeCurves/") && Path.GetExtension(f) == ".vec"))
            {
                byte[] curveBytes = Mod.GetFileBytes(path);
                if (curveBytes.Length <= 0)
                    return;

                using MemoryStream byteStream = new(curveBytes);
                using BinaryReader reader = new(byteStream);

                // Determine the name of the shape based on its file name.
                string shapeName = Path.GetFileNameWithoutExtension(path);

                // Read points from the file's binary data and store them in the universal registry.
                int pointCount = reader.ReadInt32();
                List<Vector2> shapePoints = new();
                for (int i = 0; i < pointCount; i++)
                {
                    float x = reader.ReadSingle();
                    float y = reader.ReadSingle();
                    shapePoints.Add(new(x, y));
                }
                shapes[shapeName] = new(shapePoints);
            }
        }

        public static bool TryFind(string name, out ShapeCurve curve)
        {
            curve = default;
            if (shapes.TryGetValue(name, out ShapeCurve s))
            {
                curve = s;
                return true;
            }
            return false;
        }
    }
}
