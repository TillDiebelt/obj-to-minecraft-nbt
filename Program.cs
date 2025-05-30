using System.Runtime.CompilerServices;
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

            [Option('t', "texture", Required = false, Default = false, HelpText = "Texturize the object. Further information will be asked. Can load .mtl and texture file if they are in the same folder with the .obj")]
            public bool Texturize { get; set; }

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

            bool colorize = false;
            Colorizer colorizer = new Colorizer();
            bool randomize = false;
            if (options.Value.Texturize)
            {
                var mtlPath = options.Value.InputPath.Replace(".obj", ".mtl");
                if (File.Exists(mtlPath))
                {
                    if (colorizer.LoadMtl(mtlPath))
                    {
                        Console.WriteLine("Found valid mtl file and texture, want to load it? (Y/N)");
                        var key = Console.ReadKey();
                        Console.WriteLine();
                        if (key.Key == ConsoleKey.Y)
                        {
                            Console.WriteLine("Enter material csv path (empty for default):");
                            var csvPath = Console.ReadLine();
                            if (!string.IsNullOrEmpty(csvPath))
                            {
                                if (colorizer.LoadCsv(csvPath))
                                {
                                    Console.WriteLine("Loaded material csv file.");
                                    colorize = true;
                                }
                                else
                                {
                                    Console.WriteLine("Failed to load material csv file.");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Using default material csv file.");
                                colorize = true;
                            }
                        }
                    }
                }

                bool select = false;
                if (!colorize)
                {
                    Console.WriteLine("Want to load Texture Image or Randomize with Csv?");
                    Console.WriteLine("1. Load Texture Image");
                    Console.WriteLine("2. Randomize with Csv");
                    var choice = Console.ReadKey();
                    Console.WriteLine();
                    if (choice.Key == ConsoleKey.D1 || choice.Key == ConsoleKey.NumPad1)
                    {
                        select = true;
                    }
                    else if (choice.Key == ConsoleKey.D2 || choice.Key == ConsoleKey.NumPad2)
                    {
                        randomize = true;
                    }
                    else
                    {
                        Console.WriteLine("Invalid choice. Exiting.");
                        return;
                    }
                }
                if (select)
                {

                    Console.WriteLine("Please enter texture to load. (Make sure it is the same used of UV mapping)");
                    var texturePath = Console.ReadLine();
                    if (string.IsNullOrEmpty(texturePath))
                    {
                        Console.WriteLine("No texture path provided.");
                        return;
                    }
                    if (colorizer.LoadImage(texturePath))
                    {
                        Console.WriteLine($"Loaded texture: {texturePath}");
                        colorize = true;
                    }
                    else
                    {
                        Console.WriteLine($"Texture file not found: {texturePath}");
                        return;
                    }

                    Console.WriteLine("Enter material csv path (empty for default):");
                    var csvPath = Console.ReadLine();
                    if (!string.IsNullOrEmpty(csvPath))
                    {
                        if (colorizer.LoadCsv(csvPath))
                        {
                            Console.WriteLine("Loaded material csv file.");
                            colorize = true;
                        }
                        else
                        {
                            Console.WriteLine("Failed to load material csv file.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Using default material csv file.");
                        colorize = true;
                    }
                }
            }

            HashSet<((int x, int y, int z), string blockId)>? structure = null;

            if (colorize)
            {
                structure = Voxelizer.VoxelizeColorized(options.Value.InputPath, ref colorizer);
                if (structure == null)
                {
                    Console.WriteLine("No blocks to export.");
                    return;
                }
                if (options.Value.FloodFill)
                {
                    Console.WriteLine("Flood fill is not supported with colorized voxelization.");
                }
            }
            else
            {

                HashSet<(int x, int y, int z)>? blocks = Voxelizer.Voxelize(options.Value.InputPath);
                if (blocks == null)
                {
                    Console.WriteLine("No blocks to export.");
                    return;
                }



                Console.WriteLine($"Total Blocks: {blocks.Count}");

                if(randomize)
                {
                    Console.WriteLine("Randomizing blocks with CSV file.");
                    Console.WriteLine("Enter CSV path for randomizer blocks (empty for default):");
                    var csvPath = Console.ReadLine();
                    if (string.IsNullOrEmpty(csvPath))
                    {
                        csvPath = "random.csv"; // Default path
                    }
                    var randomizerBlocks = ReadCsvRandomizerBlocks(csvPath);
                    if (randomizerBlocks.Count == 0)
                    {
                        Console.WriteLine("No randomizer blocks found in CSV.");
                        return;
                    }
                    Console.WriteLine($"Randomizer blocks found: {randomizerBlocks.Count}");
                    structure = Voxelizer.FloodColorizeRandom(blocks, randomizerBlocks);
                    if (options.Value.FloodFill)
                    {
                        blocks = Voxelizer.FloodFill(blocks);
                    }
                }
                else
                {
                    if (options.Value.FloodFill)
                    {
                        blocks = Voxelizer.FloodFill(blocks);
                    }
                    structure = blocks
                        .Select(b => (b, options.Value.BlockName))
                        .ToHashSet();
                }
            }

            List<HashSet<((int x, int y, int z) coords, string blockId)>> chunks = new List<HashSet<((int x, int y, int z) coords, string blockId)>>();
            if (options.Value.ChunkSize > 0)
            {
                chunks = Chunkify(structure, options.Value.ChunkSize);
                Console.WriteLine($"Total Chunks: {chunks.Count}");
            }
            else
            {
                chunks.Add(structure);
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
                NbtExporter.ExportToStructureNbt(outputPath, structure);
            }
            else
            {
                Console.WriteLine($"Exporting {chunks.Count} chunks to {outputPath}");
                foreach (var c in chunks)
                {
                    var chunkPath = outputPath.Replace(".nbt", $"_{c.First().coords.x}_{c.First().coords.y}_{c.First().coords.z}.nbt");
                    NbtExporter.ExportToStructureNbt(chunkPath, c);
                }
            }
        }

        static List<HashSet<((int x, int y, int z) coords, string blockId)>> Chunkify(HashSet<((int x, int y, int z) coords, string blockId)> blocks, int chunkSize)
        {
            var chunks = new List<HashSet<((int x, int y, int z) coords, string blockId)>>();
            var currentChunk = new HashSet<((int x, int y, int z) coords, string blockId)>();
            blocks = blocks.OrderBy(b => b.coords.z).ToHashSet();
            int currentZ = blocks.First().coords.z;
            foreach (var block in blocks)
            {
                if (currentChunk.Count >= chunkSize && block.coords.z > currentZ)
                {
                    chunks.Add(currentChunk);
                    currentChunk = new HashSet<((int x, int y, int z) coords, string blockId)>();
                }
                currentChunk.Add(block);
                currentZ = block.coords.z;
            }
            if (currentChunk.Count > 0)
            {
                chunks.Add(currentChunk);
            }
            return chunks;
        }

        static List<(string blockId, double chance)> ReadCsvRandomizerBlocks(string csvPath)
        {
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"CSV file not found: {csvPath}");
                return new List<(string blockId, double chance)>();
            }
            var lines = File.ReadAllLines(csvPath);
            var blocks = new List<(string blockId, double chance)>();
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length != 2 || !double.TryParse(parts[1], out double chance))
                {
                    Console.WriteLine($"Invalid line in CSV: {line}");
                    continue;
                }
                blocks.Add((parts[0].Trim(), chance));
            }
            return blocks;
        }
    }
}

