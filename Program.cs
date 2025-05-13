using CommandLine;

namespace ObjToNbt
{
    public static class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Value(0, MetaName = "input", Required = true, HelpText = "Input OBJ file path.")]
            public string InputPath { get; set; }

            [Option('o', "output", Required = false, Default = "", HelpText = "Output NBT file path.")]
            public string OutputPath { get; set; }

            [Option('c', "chunk-size", Required = false, Default = -1, HelpText = "Chunk size for voxelization.")]
            public int ChunkSize { get; set; }

            [Option('b', "block-name", Required = false, Default = "minecraft:stone", HelpText = "Block name in NBT.")]
            public string BlockName { get; set; }

            [Option('f', "floodfill", Required = false, Default = false, HelpText = "Fill the object with blocks. WARNING: center must be part of the object!")]
            public bool FloodFill { get; set; }

        }

        public static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args);

            if (options == null || options.Errors.Count() > 0)
            {
                Console.WriteLine("Failed to parse arguments.");
                return;
            }

            if(!File.Exists(options.Value.InputPath))
            {
                Console.WriteLine($"File not found: {options.Value.InputPath}");
                return;
            }
            if(!options.Value.InputPath.EndsWith(".obj"))
            {
                Console.WriteLine($"File is not a .obj file: {options.Value.InputPath}");
                return;
            }

            HashSet<(int x, int y, int z)>? blocks = Voxelizer.Voxelize(options.Value.InputPath);
            if (blocks == null)
            {
                Console.WriteLine("No blocks to export.");
                return;
            }

            if(options.Value.FloodFill)
            {
                blocks = Voxelizer.FloodFill(blocks);
            }

            Console.WriteLine($"Total Blocks: {blocks.Count}");

            List<HashSet<(int x, int y, int z)>> chunks = new List<HashSet<(int x, int y, int z)>>();
            if (options.Value.ChunkSize > 0)
            {
                chunks = Chunkify(blocks, options.Value.ChunkSize);
                Console.WriteLine($"Total Chunks: {chunks.Count}");
            }
            else
            {
                chunks.Add(blocks);
            }

            string outputPath = options.Value.InputPath.Replace(".obj", ".nbt");
            if (!string.IsNullOrEmpty(options.Value.OutputPath))
            {
                outputPath = options.Value.OutputPath;
            }
            if (!outputPath.EndsWith(".nbt"))
            {
                outputPath += ".nbt";
            }

            if (chunks.Count == 1)
            {
                NbtExporter.ExportToStructureNbt(outputPath, blocks, options.Value.BlockName);
            }
            else
            {
                Console.WriteLine($"Exporting {chunks.Count} chunks to {outputPath}");
                foreach (var c in chunks)
                {
                    var chunkPath = outputPath.Replace(".nbt", $"_{c.First().x}_{c.First().y}_{c.First().z}.nbt");
                    NbtExporter.ExportToStructureNbt(chunkPath, c, options.Value.BlockName);
                }
            }
        }

        static List<HashSet<(int x, int y, int z)>> Chunkify(HashSet<(int x, int y, int z)> blocks, int chunkSize)
        {
            var chunks = new List<HashSet<(int x, int y, int z)>>();
            var currentChunk = new HashSet<(int x, int y, int z)>();
            blocks = blocks.OrderBy(b => b.z).ToHashSet();
            int currentZ = blocks.First().z;
            foreach (var block in blocks)
            {
                if (currentChunk.Count >= chunkSize && block.z > currentZ)
                {
                    chunks.Add(currentChunk);
                    currentChunk = new HashSet<(int x, int y, int z)>();
                }
                currentChunk.Add(block);
                currentZ = block.z;
            }
            if (currentChunk.Count > 0)
            {
                chunks.Add(currentChunk);
            }
            return chunks;
        }
    }
}

