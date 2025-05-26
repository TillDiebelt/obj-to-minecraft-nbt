using fNbt;

namespace ObjToNbt
{
    public static class NbtExporter
    {
        public static void ExportToStructureNbt(string outputPath, HashSet<((int x, int y, int z), string blockId)> voxels)
        {
            if (voxels.Count == 0) return;

            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            foreach (var ((x, y, z), s) in voxels)
            {
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (z < minZ) minZ = z;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                if (z > maxZ) maxZ = z;
            }

            int sizeX = maxX - minX + 1;
            int sizeY = maxY - minY + 1;
            int sizeZ = maxZ - minZ + 1;

            var blocks = new NbtList("blocks", NbtTagType.Compound);
            Dictionary<string, int> palette = new Dictionary<string, int>();
            var nbtpalette = new NbtList("palette", NbtTagType.Compound);
            foreach (var ((x, y, z), s) in voxels)
            {
                int state = 0;
                if (palette.ContainsKey(s))
                {
                    state = palette[s];
                }
                else
                {
                    state = palette.Count;
                    nbtpalette.Add(new NbtCompound { new NbtString("Name", s) });
                    palette[s] = state;
                }
                var block = new NbtCompound
                {
                    new NbtInt("state", state),
                    new NbtList("pos", new[] {
                        new NbtInt(x - minX),
                        new NbtInt(y - minY),
                        new NbtInt(z - minZ)
                    })
                };
                blocks.Add(block);
            }

            var root = new NbtCompound("")
                    {
                    new NbtList("size", new[] {
                        new NbtInt(sizeX),
                        new NbtInt(sizeY),
                        new NbtInt(sizeZ)
                    }),
                blocks,
                nbtpalette,
                new NbtList("entities", NbtTagType.Compound)
            };

            //Console.WriteLine(root.ToString("  "));

            var file = new NbtFile(root);
            file.SaveToFile(outputPath, NbtCompression.GZip);

            Console.WriteLine($"NBT structure saved to {outputPath} with {voxels.Count} blocks.");
        }

        public static void ExportToStructureNbt(string outputPath, HashSet<(int x, int y, int z)> voxels, string blockName = "minecraft:stone")
        {
            if (voxels.Count == 0) return;

            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            foreach (var (x, y, z) in voxels)
            {
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (z < minZ) minZ = z;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                if (z > maxZ) maxZ = z;
            }

            int sizeX = maxX - minX + 1;
            int sizeY = maxY - minY + 1;
            int sizeZ = maxZ - minZ + 1;

            var blocks = new NbtList("blocks", NbtTagType.Compound);
            foreach (var (x, y, z) in voxels)
            {
                var block = new NbtCompound
                {
                    new NbtInt("state", 0),
                    new NbtList("pos", new[] {
                        new NbtInt(x - minX),
                        new NbtInt(y - minY),
                        new NbtInt(z - minZ)
                    })
                };
                blocks.Add(block);
            }

            var palette = new NbtList("palette", NbtTagType.Compound)
            {
                new NbtCompound { new NbtString("Name", blockName) }
                };

            var root = new NbtCompound("")
                    {
                    new NbtList("size", new[] {
                        new NbtInt(sizeX),
                        new NbtInt(sizeY),
                        new NbtInt(sizeZ)
                    }),
                blocks,
                palette,
                new NbtList("entities", NbtTagType.Compound)
            };

            var file = new NbtFile(root);
            file.SaveToFile(outputPath, NbtCompression.GZip);

            Console.WriteLine($"NBT structure saved to {outputPath} with {voxels.Count} blocks.");
        }
    }
}
