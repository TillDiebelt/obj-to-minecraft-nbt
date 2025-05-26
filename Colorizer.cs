using SkiaSharp;

namespace ObjToNbt
{
    public class Colorizer
    {
        private SKBitmap? Bitmap;
        ColorPicker colorPicker = new ColorPicker();

        public Colorizer()
        {
            colorPicker.SetDefault("terracotta");
        }

        public bool LoadMtl(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                return false;
            }
            var text = File.ReadAllText(path);
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("map_Kd "))
                {
                    var parts = line.Split(' ');
                    if (parts.Length > 1)
                    {
                        var imagePath = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar) + 1) + line.Substring(7).Trim();
                        if (File.Exists(imagePath))
                        {
                            using (var stream = File.OpenRead(imagePath))
                            {
                                Bitmap = SKBitmap.Decode(stream);
                            }
                            Console.WriteLine($"Loaded texture: {imagePath}");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"Texture file not found: {imagePath}");
                            return false;
                        }
                    }
                }
            }
            Console.WriteLine("No texture found in MTL file.");
            return false;
        }

        public bool LoadImage(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                return false;
            }
            using (var stream = File.OpenRead(path))
            {
                Bitmap = SKBitmap.Decode(stream);
            }
            return true;
        }

        public bool LoadCsv(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                return false;
            }
            return colorPicker.ReadFromFile(path);
        }

        public string GetBlockId(List<List<(float x, float y)>> vts)
        {
            return getClosestMatch(getAverageColor(vts));
        }

        private string getClosestMatch(SKColor color)
        {
            return colorPicker.GetBestMatch(color);
        }

        private SKColor getAverageColor(List<List<(float x, float y)>> vts)
        {
            if (Bitmap == null)
            {
                Console.WriteLine("No bitmap loaded.");
                return SKColors.Transparent;
            }
            List<int> Rs = new List<int>();
            List<int> Gs = new List<int>();
            List<int> Bs = new List<int>();
            foreach (var square in vts)
            {
                if (square.Count != 4)
                {
                    Console.WriteLine("Invalid square: " + string.Join(", ", square));
                    continue;
                }
                var p1 = square[0];
                var p2 = square[1];
                var p3 = square[2];
                var p4 = square[3];
                (int X, int Y) center = (
                    ((int)(((p1.x + p2.x + p3.x + p4.x) / 4.0) * this.Bitmap.Width)  % this.Bitmap.Width  + this.Bitmap.Width)  % this.Bitmap.Width, 
                    ((int)(((p1.y + p2.y + p3.y + p4.y) / 4.0) * this.Bitmap.Height) % this.Bitmap.Height + this.Bitmap.Height) % this.Bitmap.Height
                );
                var color = Bitmap.GetPixel(center.X, this.Bitmap.Height - center.Y - 1);
                Rs.Add(color.Red);
                Gs.Add(color.Green);
                Bs.Add(color.Blue);
            }
            int r = (int)Rs.Average();
            int g = (int)Gs.Average();
            int b = (int)Bs.Average();
            var sk = new SKColor((byte)r, (byte)g, (byte)b);
            return sk;
        }

    }

}