using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Core.ShapeCurves
{
    // This code isn't actually used anywhere ingame, it's just here for the purpose of clarity in terms of how the shape files are made.
    // If you wish to use it you'll want to copypaste this code and tweak it for use in a separate C# project.
    public class ShapeCurveGenerator
    {
#pragma warning disable CA1416 // Validate platform compatibility
        internal static void GenerateShapeFile(string imagePath)
        {
            string shapeFileName = $"{Path.GetFileNameWithoutExtension(imagePath)}.vec";
            string shapeFilePath = Environment.CurrentDirectory + "\\" + shapeFileName;
            using var shapeFile = File.Create(shapeFilePath);
            using BinaryWriter shapeFileWriter = new(shapeFile);

            // Read all pure-red pixels from the image as places to generate points.
            // The points are stored in this program's memory as pixel-space values, but are normalized when they're stored in the file.
            Bitmap inputImage = (Bitmap)Image.FromFile(imagePath);
            List<Vector2> points = new();
            for (int i = 0; i < inputImage.Width; i++)
            {
                for (int j = 0; j < inputImage.Height; j++)
                {
                    var c = inputImage.GetPixel(i, j);
                    if (c.R >= 255 && c.G <= 0 && c.B <= 0)
                        points.Add(new Vector2(i, j));
                }
            }

            // Find the boundaries of the points.
            Vector2 min = Vector2.One * 999999f;
            Vector2 max = Vector2.One * -999999f;
            foreach (Vector2 p in points)
            {
                if (p.X < min.X)
                    min.X = p.X;
                if (p.X > max.X)
                    max.X = p.X;

                if (p.Y < min.Y)
                    min.Y = p.Y;
                if (p.Y > max.Y)
                    max.Y = p.Y;
            }

            // Write normalized points to the resulting shape file.
            Vector2 correction = new(inputImage.Width, inputImage.Height);
            shapeFileWriter.Write(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = (points[i] - min) / correction;
                shapeFileWriter.Write(points[i].X);
                shapeFileWriter.Write(points[i].Y);
            }
        }
#pragma warning restore CA1416 // Validate platform compatibility
    }
}
