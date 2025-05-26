using System.Drawing;
using SkiaSharp;

namespace ObjToNbt
{
    public class ColorPicker
    {
        Dictionary<string, string> colorCodes = new Dictionary<string, string>();

        private const string defaultTerracotta =
            "minecraft:white_terracotta, #d2b1a1\n" +
            "minecraft:orange_terracotta, #9d5123\n" +
            "minecraft:magenta_terracotta, #94566c\n" +
            "minecraft:light_blue_terracotta, #766f8c\n" +
            "minecraft:yellow_terracotta, #b88221\n" +
            "minecraft:lime_terracotta, #677535\n" +
            "minecraft:pink_terracotta, #a65151\n" +
            "minecraft:gray_terracotta, #3a2b24\n" +
            "minecraft:light_gray_terracotta, #876b62\n" +
            "minecraft:cyan_terracotta, #565c5c\n" +
            "minecraft:purple_terracotta, #7b4a59\n" +
            "minecraft:blue_terracotta, #4a3b5b\n" +
            "minecraft:brown_terracotta, #4f3524\n" +
            "minecraft:green_terracotta, #4e552c\n" +
            "minecraft:red_terracotta, #8d3a2d\n" +
            "minecraft:black_terracotta, #251710\n" +
            "minecraft:terracotta, #e2725b";

        private List<(string, string)> defaultColors = new List<(string, string)>()
        {
            ("white", "#f9ffff"),
            ("light_gray", "#9c9d97"),
            ("gray", "#474f52"),
            ("black", "#1d1c21"),
            ("yellow", "#ffd83d"),
            ("orange", "#f9801d"),
            ("red", "#b02e26"),
            ("brown", "#825432"),
            ("lime", "#80c71f"),
            ("green", "#5d7c15"),
            ("light_blue", "#3ab3da"),
            ("cyan", "#169c9d"),
            ("blue", "#3c44a9"),
            ("pink", "#f38caa"),
            ("magenta", "#c64fbd"),
            ("purple", "#8932b7"),
        };

        public bool ReadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return false;
            }
            if (!filePath.EndsWith(".csv"))
            {
                Console.WriteLine($"File is not a .csv file: {filePath}");
                return false;
            }
            Console.WriteLine($"Reading color data from {filePath}");
            //csv with data: block, color code
            //parse the file
            string data = File.ReadAllText(filePath);
            if (data.Length == 0)
            {
                Console.WriteLine($"File is empty: {filePath}");
                return false;
            }
            ReadFromString(data);
            return true;
        }

        public string DefaultBlockNameWool => "wool";
        public string DefaultBlockNameGlas => "glas";
        public string DefaultBlockNameConcrete => "concrete";
        public string DefaultBlockNameTerracotta => "terracotta";

        public void SetDefault(string blockName)
        {
            colorCodes.Clear();
            switch (blockName)
            {
                case "terracotta":
                    ReadFromString(defaultTerracotta);
                    break;
                case "wool":
                    string defaultWool = string.Join("\n", defaultColors.Select(x => $"minecraft:{x.Item1}_wool, {x.Item2}"));
                    ReadFromString(defaultWool);
                    break;
                case "glas":
                    string defaultGlas = string.Join("\n", defaultColors.Select(x => $"minecraft:{x.Item1}_stained_glass, {x.Item2}"));
                    ReadFromString(defaultGlas);
                    break;
                case "concrete":
                    string defaultConcrete = string.Join("\n", defaultColors.Select(x => $"minecraft:{x.Item1}_concrete, {x.Item2}"));
                    ReadFromString(defaultConcrete);
                    break;
                default:
                    Console.WriteLine($"Unknown block name: {blockName}");
                    break;
            }
        }

        public void ReadFromString(string data)
        {
            colorCodes.Clear();
            //csv with data: block, color code
            //parse the string
            string[] lines = data.Split('\n');
            if (lines.Length == 0)
            {
                Console.WriteLine($"String is empty");
                return;
            }
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                if (parts.Length != 2)
                {
                    Console.WriteLine($"Invalid line: {line}");
                    continue;
                }
                string block = parts[0].Trim();
                string colorCode = parts[1].Trim();
                if (colorCodes.ContainsKey(block))
                {
                    Console.WriteLine($"Duplicate block: {block}");
                    continue;
                }
                colorCodes.Add(block, colorCode);
            }
            Console.WriteLine($"Read {colorCodes.Count} color codes from string");
        }

        public SKColor ToColor(string colorCode)
        {
            //parse the hex color code
            if (colorCode.StartsWith("#"))
            {
                colorCode = colorCode.Substring(1);
            }
            if (colorCode.Length != 6)
            {
                Console.WriteLine($"Invalid color code: {colorCode}");
                return SKColor.Empty;
            }
            int r = int.Parse(colorCode.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(colorCode.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(colorCode.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return new SKColor((byte)r, (byte)g, (byte)b);
        }

        public string GetBestMatch(SKColor color)
        {
            string bestMatch = "";
            double bestDistance = double.MaxValue;
            foreach (var kvp in colorCodes)
            {
                string block = kvp.Key;
                string colorCode = kvp.Value;
                SKColor blockColor = ToColor(colorCode);
                double distance = Math.Sqrt(Math.Pow((int)(color.Red) - blockColor.Red, 2) + Math.Pow((int)(color.Green) - blockColor.Green, 2) + Math.Pow((int)(color.Blue) - blockColor.Blue, 2));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = block;
                }
            }
            return bestMatch;
        }        
    }
}
