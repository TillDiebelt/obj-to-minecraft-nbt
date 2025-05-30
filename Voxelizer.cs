using System.Globalization;
using System.Numerics;

namespace ObjToNbt
{
    struct Vector3D
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3D(float x, float y, float z) { this.X = x; this.Y = y; this.Z = z; }
        public override string ToString() => $"({X},{Y},{Z})";
    }

    struct Vector2D
    {
        public float X;
        public float Y;

        public Vector2D(float x, float y) { this.X = x; this.Y = y; }
        public override string ToString() => $"({X},{Y})";
    }

    public class Voxelizer
    {

        public static HashSet<(int x, int y, int z)>? Voxelize(string path)
        {
            var vertices = new List<Vector3D>();
            var faces = new List<int[]>();

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("v "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    vertices.Add(new Vector3D(x, y, z));
                }
                else if (line.StartsWith("f "))
                {
                    var indices = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Skip(1)
                                      .Select(p => int.Parse(p.Split('/')[0]) - 1)
                                      .ToArray();
                    if (indices.Length == 4)
                        faces.Add(indices);
                }
            }

            if (faces.Count == 0)
            {
                Console.WriteLine("No square faces found. Make sure your model is in the right format!");
                return null;
            }

            var firstFace = faces[0];
            var v0f = vertices[firstFace[0]];
            var v1f = vertices[firstFace[1]];
            float dx = v1f.X - v0f.X;
            float dy = v1f.Y - v0f.Y;
            float dz = v1f.Z - v0f.Z;
            float edgeLength = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            float scale = 1.0f / edgeLength;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            foreach (var v in vertices)
            {
                if (v.X < minX)
                {
                    minX = v.X;
                }
                if (v.Y < minY)
                {
                    minY = v.Y;
                }
                if (v.Y < minZ)
                {
                    minZ = v.Z;
                }
            }

            var intVertices = vertices
            .Select(v => (
                x: v.X - minX,
                y: v.Y - minY,
                z: v.Z - minZ
            ))
            .Select(v => (
                x: (v.x * scale),
                y: (v.y * scale),
                z: (v.z * scale)
            ))
            .Select(v => (
                x: Math.Round(v.x),
                y: Math.Round(v.y),
                z: Math.Round(v.z)
            ))
            .Select(v => (
                x: (int)v.x,
                y: (int)v.y,
                z: (int)v.z
            )).ToList();

            var blocks = new HashSet<(int x, int y, int z)>();

            foreach (var face in faces)
            {
                var v0 = new Vector3(intVertices[face[0]].x, intVertices[face[0]].y, intVertices[face[0]].z);
                var v1 = new Vector3(intVertices[face[1]].x, intVertices[face[1]].y, intVertices[face[1]].z);
                var v2 = new Vector3(intVertices[face[2]].x, intVertices[face[2]].y, intVertices[face[2]].z);
                var v3 = new Vector3(intVertices[face[3]].x, intVertices[face[3]].y, intVertices[face[3]].z);

                var center = new Vector3(
                    (v0.X + v1.X + v2.X + v3.X) / 4f,
                    (v0.Y + v1.Y + v2.Y + v3.Y) / 4f,
                    (v0.Z + v1.Z + v2.Z + v3.Z) / 4f
                );

                var normal = GetNormal(v0, v1, v2, v3);

                if (Math.Abs(normal.X) + Math.Abs(normal.Y) + Math.Abs(normal.Z) != 1)
                {
                    Console.WriteLine("Wrong normal detected, skipping face. Make sure your model is in the right format!");
                    continue;
                }

                center.X += -normal.X * 0.5f;
                center.Y += -normal.Y * 0.5f;
                center.Z += -normal.Z * 0.5f;

                blocks.Add(((int)center.X, (int)center.Y, (int)center.Z));
            }

            return blocks;
        }

        public static HashSet<((int x, int y, int z), string blockId)>? VoxelizeColorized(string path, ref Colorizer colorizer)
        {
            var vertices = new List<Vector3D>();
            var colors = new List<Vector2D>();
            var faces = new List<int[]>();
            var facesColor = new List<int[]>();

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith("v "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    vertices.Add(new Vector3D(x, y, z));
                }
                else if (line.StartsWith("f "))
                {
                    var indices = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Skip(1)
                                      .Select(p => int.Parse(p.Split('/')[0]) - 1)
                                      .ToArray();
                    if (indices.Length == 4)
                        faces.Add(indices);
                    var colorIndices = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                      .Skip(1)
                                      .Select(p => int.Parse(p.Split('/')[1]) - 1)
                                      .ToArray();
                    if (colorIndices.Length == 4)
                        facesColor.Add(colorIndices);
                }
                else if (line.StartsWith("vt "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    colors.Add(new Vector2D(u, v));
                }
            }

            if (faces.Count == 0)
            {
                Console.WriteLine("No square faces found. Make sure your model is in the right format!");
                return null;
            }

            var firstFace = faces[0];
            var v0f = vertices[firstFace[0]];
            var v1f = vertices[firstFace[1]];
            float dx = v1f.X - v0f.X;
            float dy = v1f.Y - v0f.Y;
            float dz = v1f.Z - v0f.Z;
            float edgeLength = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            float scale = 1.0f / edgeLength;

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            foreach (var v in vertices)
            {
                if (v.X < minX)
                {
                    minX = v.X;
                }
                if (v.Y < minY)
                {
                    minY = v.Y;
                }
                if (v.Y < minZ)
                {
                    minZ = v.Z;
                }
            }

            var intVertices = vertices
            .Select(v => (
                x: v.X - minX,
                y: v.Y - minY,
                z: v.Z - minZ
            ))
            .Select(v => (
                x: (v.x * scale),
                y: (v.y * scale),
                z: (v.z * scale)
            ))
            .Select(v => (
                x: Math.Round(v.x),
                y: Math.Round(v.y),
                z: Math.Round(v.z)
            ))
            .Select(v => (
                x: (int)v.x,
                y: (int)v.y,
                z: (int)v.z
            )).ToList();

            var blocks = new Dictionary<(int x, int y, int z), List<int>>();

            foreach (var face in faces)
            {
                var v0 = new Vector3(intVertices[face[0]].x, intVertices[face[0]].y, intVertices[face[0]].z);
                var v1 = new Vector3(intVertices[face[1]].x, intVertices[face[1]].y, intVertices[face[1]].z);
                var v2 = new Vector3(intVertices[face[2]].x, intVertices[face[2]].y, intVertices[face[2]].z);
                var v3 = new Vector3(intVertices[face[3]].x, intVertices[face[3]].y, intVertices[face[3]].z);

                var center = new Vector3(
                    (v0.X + v1.X + v2.X + v3.X) / 4f,
                    (v0.Y + v1.Y + v2.Y + v3.Y) / 4f,
                    (v0.Z + v1.Z + v2.Z + v3.Z) / 4f
                );

                var normal = GetNormal(v0, v1, v2, v3);

                if (Math.Abs(normal.X) + Math.Abs(normal.Y) + Math.Abs(normal.Z) != 1)
                {
                    Console.WriteLine("Wrong normal detected, skipping face. Make sure your model is in the right format!");
                    continue;
                }

                center.X += -normal.X * 0.5f;
                center.Y += -normal.Y * 0.5f;
                center.Z += -normal.Z * 0.5f;

                var block = ((int)center.X, (int)center.Y, (int)center.Z);
                if (!blocks.ContainsKey(block))
                {
                    blocks[block] = new List<int>();
                }
                blocks[block].Add(faces.IndexOf(face));
            }

            var colorizedBlocks = new HashSet<((int x, int y, int z), string blockId)>();
            foreach (var block in blocks)
            {
                var vts = new List<List<(float x, float y)>>();
                foreach (var faceIndex in block.Value)
                {
                    var face = faces[faceIndex];
                    var colorIndex = facesColor[faceIndex];
                    var vt0 = colors[colorIndex[0]];
                    var vt1 = colors[colorIndex[1]];
                    var vt2 = colors[colorIndex[2]];
                    var vt3 = colors[colorIndex[3]];
                    vts.Add(new List<(float x, float y)> { (vt0.X, vt0.Y), (vt1.X, vt1.Y), (vt2.X, vt2.Y), (vt3.X, vt3.Y) });
                }
                var blockId = colorizer.GetBlockId(vts);
                colorizedBlocks.Add((block.Key, blockId));
            }

            return colorizedBlocks;
        }

        public static HashSet<(int x, int y, int z)> FloodFill(HashSet<(int x, int y, int z)> blocks)
        {
            var inside = FindInside(blocks);
            var filled = FloodFill(inside, blocks);
            filled = blocks.Union(filled).ToHashSet();
            return filled;
        }

        static Vector3 GetNormal(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var n1 = Cross(Normalize(Subtract(v1, v0)), Normalize(Subtract(v2, v0)));
            var n2 = Cross(Normalize(Subtract(v2, v0)), Normalize(Subtract(v3, v0)));

            var nx = (n1.X + n2.X) / 2;
            var ny = (n1.Y + n2.Y) / 2;
            var nz = (n1.Z + n2.Z) / 2;

            return Normalize(new Vector3(nx, ny, nz));
        }

        static Vector3 Subtract(Vector3 a, Vector3 b) =>
            new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        static Vector3 Cross(Vector3 a, Vector3 b) =>
            new(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X
            );

        static Vector3 Normalize(Vector3 v)
        {
            float length = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return new Vector3(v.X / length, v.Y / length, v.Z / length);
        }

        static HashSet<(int x, int y, int z)> FloodFill(Vector3 start, HashSet<(int x, int y, int z)> blocks, int max = 10_000_000)
        {
            var filled = new HashSet<(int x, int y, int z)>();
            var queue = new Queue<Vector3>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                if (filled.Count > max)
                {
                    Console.WriteLine("Flood fill limit reached.");
                    break;
                }
                var current = queue.Dequeue();
                var pos = ((int)current.X, (int)current.Y, (int)current.Z);
                if (filled.Contains(pos) || blocks.Contains(pos)) continue;
                filled.Add(pos);
                foreach (var offset in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ })
                {
                    queue.Enqueue(current + offset);
                }
            }
            return filled;
        }

        public static HashSet<((int x, int y, int z) coord, string blockId)> FloodColorizeRandom(HashSet<(int x, int y, int z)> blocks, List<(string id, double chance)> blockIds)
        {
            var filled = new HashSet<((int x, int y, int z) coord, string blockId)>();
            var seen = new HashSet<(int x, int y, int z)>();
            while(filled.Count < blocks.Count)
            {
                var first = blocks.Except(seen).FirstOrDefault();
                Queue<(int x, int y, int z)> todo = new Queue<(int x, int y, int z)>();
                todo.Enqueue(first);
                var random = new Random();
                while (todo.Count > 0)
                {
                    var current = todo.Dequeue();
                    if (seen.Contains(current)) continue;
                    List<(int x, int y, int z)> neighbors = new List<(int x, int y, int z)>();
                    foreach (var offset in new[] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
                    {
                        if (!blocks.Contains((current.x + offset.Item1, current.y + offset.Item2, current.z + offset.Item3))) continue;
                        todo.Enqueue((current.x + offset.Item1, current.y + offset.Item2, current.z + offset.Item3));
                        neighbors.Add((current.x + offset.Item1, current.y + offset.Item2, current.z + offset.Item3));
                    }

                    // Randomly select a block ID based on the provided chances and neighbors
                    string selectedBlockId = "";
                    if (neighbors.Count > 0)
                    {
                        var neighborBlockIds = neighbors.Select(n => filled.FirstOrDefault(f => f.coord == n).blockId).Where(id => !string.IsNullOrEmpty(id)).ToList();
                        if (neighborBlockIds.Count > 0)
                        {
                            selectedBlockId = neighborBlockIds[random.Next(neighborBlockIds.Count)];
                        }
                        else
                        {
                            var totalChance = blockIds.Sum(b => b.chance);
                            var randomValue = random.NextDouble() * totalChance;
                            double cumulativeChance = 0.0;
                            foreach (var blockId in blockIds)
                            {
                                cumulativeChance += blockId.chance;
                                if (randomValue < cumulativeChance)
                                {
                                    selectedBlockId = blockId.id;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var totalChance = blockIds.Sum(b => b.chance);
                        var randomValue = random.NextDouble() * totalChance;
                        double cumulativeChance = 0.0;
                        foreach (var blockId in blockIds)
                        {
                            cumulativeChance += blockId.chance;
                            if (randomValue < cumulativeChance)
                            {
                                selectedBlockId = blockId.id;
                                break;
                            }
                        }
                    }
                    filled.Add((current, selectedBlockId));
                    seen.Add(current);
                    // Enqueue neighboring blocks
                }
            }
            return filled;
        }

        static Vector3 FindInside(HashSet<(int x, int y, int z)> blocks)
        {
            var minX = blocks.Min(b => b.x);
            var minY = blocks.Min(b => b.y);
            var minZ = blocks.Min(b => b.z);
            var maxX = blocks.Max(b => b.x);
            var maxY = blocks.Max(b => b.y);
            var maxZ = blocks.Max(b => b.z);
            return new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
        }
    }
}
