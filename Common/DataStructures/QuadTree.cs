using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace NoxusBoss.Common
{
    public class QuadTree<T>
    {
        public class BoundedObject
        {
            public T Value;

            public Rectangle Area;
        }

        private readonly List<BoundedObject> objects;

        private readonly QuadTree<T>[] neighbors = new QuadTree<T>[4];

        public int Level
        {
            get;
            protected set;
        }

        public Rectangle OverallArea
        {
            get;
            protected set;
        }

        public const int MaxObjectsPerNode = 5;

        public const int MaxLevels = 32;

        public QuadTree(int level, Rectangle bounds)
        {
            Level = level;
            objects = new();
            OverallArea = bounds;
        }

        public void Insert(T obj, Rectangle objectArea)
        {
            // Recursively walk down the tree if occupied.
            if (neighbors[0] is not null)
            {
                int areaIndex = GetIndex(objectArea);
                if (areaIndex != -1)
                {
                    neighbors[areaIndex].Insert(obj, objectArea);
                    return;
                }
            }

            // Insert the object.
            objects.Add(new()
            {
                Value = obj,
                Area = objectArea
            });

            // Check if the new object has increased the size of the tree beyond its limit.
            // If it has, split it.
            if (objects.Count > MaxObjectsPerNode && Level < MaxLevels)
            {
                if (neighbors[0] == null)
                    Split();

                int i = 0;
                while (i < objects.Count)
                {
                    int index = GetIndex(objects[i].Area);
                    if (index != -1)
                    {
                        neighbors[index].Insert(objects[i].Value, objects[i].Area);
                        objects.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public List<T> GetAllInRange(Rectangle queryArea)
        {
            List<T> result = new();

            int index = GetIndex(queryArea);
            if (index != -1 && neighbors[0] != null)
                result.AddRange(neighbors[index].GetAllInRange(queryArea));
            else
                result.AddRange(objects.Select(o => o.Value));

            return result;
        }

        private int GetIndex(Rectangle area)
        {
            // Index formatting is as follows:
            /* 1   0
             *   x
             * 2   3
             */

            int index = -1;
            int verticalMidpoint = OverallArea.Center.X;
            int horizontalMidpoint = OverallArea.Center.Y;

            // Check whether the object can completely fit within the top quadrants.
            bool topQuadrant = area.Y < horizontalMidpoint && area.Y + area.Height < horizontalMidpoint;

            // Check whether the object can completely fit within the bottom quadrants.
            bool bottomQuadrant = area.Y > horizontalMidpoint;

            // Check whether the object can completely fit within the left quadrants.
            if (area.X < verticalMidpoint && area.X + area.Width < verticalMidpoint)
            {
                if (topQuadrant)
                    index = 1;
                else if (bottomQuadrant)
                    index = 2;
            }

            // Check whether the object can completely fit within the right quadrants.
            else if (area.X > verticalMidpoint)
            {
                if (topQuadrant)
                    index = 0;
                else if (bottomQuadrant)
                    index = 3;
            }

            return index;
        }

        private void Split()
        {
            int x = OverallArea.X;
            int y = OverallArea.Y;
            int subWidth = OverallArea.Width / 2;
            int subHeight = OverallArea.Height / 2;

            neighbors[0] = new QuadTree<T>(Level + 1, new(x + subWidth, y, subWidth, subHeight));
            neighbors[1] = new QuadTree<T>(Level + 1, new(x, y, subWidth, subHeight));
            neighbors[2] = new QuadTree<T>(Level + 1, new(x, y + subHeight, subWidth, subHeight));
            neighbors[3] = new QuadTree<T>(Level + 1, new(x + subWidth, y + subHeight, subWidth, subHeight));
        }
    }
}
