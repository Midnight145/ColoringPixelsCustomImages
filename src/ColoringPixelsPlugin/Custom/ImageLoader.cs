using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;
using ColoringPixelsMod;
using UnityEngine;

namespace ColoringPixelsPlugin.Custom {

    public static class ImageLoader {
        public static Dictionary<string, LevelData[]> books = new Dictionary<string, LevelData[]>();

        private static readonly ManualLogSource Logger = CustomImagesPlugin.Log;
        private static bool initialized;

        public static void Initialize() {
            if (ImageLoader.initialized) {
                return;
            }

            ImageLoader.Logger.LogInfo("Loading Custom Levels");
            string path = Path.Combine(Application.persistentDataPath, "CustomBooks");
            string[] subfolders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToArray();
            ;
            foreach (string subfolder in subfolders) {
                InitBook(path, subfolder);
                CustomBook _ = new CustomBook(path, subfolder);
            }
        }


        private static void InitBook(string bookPath, string bookName) {
            ImageLoader.Logger.LogInfo("Loading Book: " + bookName);
            string path = Path.Combine(bookPath, bookName);
            var levelList = new ConcurrentBag<LevelData>();

            int index = 0;
            string[] pngFiles = Directory.GetFiles(path, "*.png");

            // pngfile : binfile mapping
            var cachedFiles = new Dictionary<string, string>();
            foreach (string pngFile in pngFiles) {
                string baseName = Path.GetFileNameWithoutExtension(pngFile);
                string binFile = Path.Combine(path, baseName + ".bin");
                if (File.Exists(binFile)) {
                    cachedFiles.Add(pngFile, binFile);
                }
            }

//          Name, Colors, Width, Height, Sprite, Texture
            var data = new ConcurrentDictionary<string, Tuple<string, int, int, int, Sprite, Texture2D>>();
            const string pattern = @"^(?<name>.+?)_(?<dimension>\d+?)_(?<colors>\d+)$";
            Regex regex = new Regex(pattern);

            foreach (string file in pngFiles) {
                byte[] fileData = File.ReadAllBytes(file);
                if (fileData.Length == 0) {
                    Logger.LogError($"Failed to read file data for {file}");
                    continue;
                }

                Texture2D texture = new Texture2D(2, 2); // temp size
                bool isLoaded = texture.LoadImage(fileData); // resized automatically

                if (!isLoaded) {
                    Logger.LogError($"Failed to load image data into texture for {file}");
                    continue;
                }

                Logger.LogDebug("Processing file " + file);

                string filename = Path.GetFileNameWithoutExtension(file);
                Match match = regex.Match(filename);
                string name;
                int dimension, colors;
                if (match.Success) {
                    name = match.Groups["name"].Value;
                    dimension = int.Parse(match.Groups["dimension"].Value);
                    colors = int.Parse(match.Groups["colors"].Value);
                }
                else {
                    name = filename;
                    dimension = Math.Min(500, Math.Max(texture.width, texture.height));
                    colors = 99;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                int width = texture.width;
                int height = texture.height;
                Texture2D qtexture = null;
                if (!cachedFiles.ContainsKey(file)) {
                    // Resize the image if necessary
                    // Used to later make the level data, but that's not needed if loading from cache.
                    // This also has to be done on the main thread, so we can't parallelize this.
                    Texture2D resizedImage = ImageProcessor.ResizeImage(texture, dimension);
                    Logger.LogDebug("Resized image to " + resizedImage.width + "x" + resizedImage.height);
                    width = resizedImage.width;
                    height = resizedImage.height;
                    qtexture = resizedImage;
                }

                data[file] = Tuple.Create(name, colors, width, height, sprite, qtexture);

            }

            // The rest of this can be parallelized, which is good because it takes a while for the quantization of large images.
            Parallel.ForEach(pngFiles, file => {
                string name = data[file].Item1;
                int colors = data[file].Item2, width = data[file].Item3, height = data[file].Item4;
                Sprite sprite = data[file].Item5;
                Texture2D qtexture = data[file].Item6;
                Logger.LogInfo($"Loading {name} ({width}x{height}) with {colors} colors");


                if (!cachedFiles.ContainsKey(file)) {
                    Logger.LogDebug("Starting quantization for " + name);
                    Color32[] quantizedPixels = ImageProcessor.QuantizeImage(qtexture.GetPixels32(), colors);
                    qtexture.SetPixels32(quantizedPixels);
                    Logger.LogDebug("Quantized image for " + name);
                }

                short[] levelData;

                if (!cachedFiles.TryGetValue(file, out string value)) {
                    levelData = CreateLevelData(qtexture);
                    CreateCache(path, file, levelData);
                }
                else {
                    byte[] buffer = File.ReadAllBytes(value);
                    levelData = new short[buffer.Length / 2];
                    Buffer.BlockCopy(buffer, 0, levelData, 0, buffer.Length);
                    // CreateImageSprites(levelData);
                }

                int currentIndex = Interlocked.Decrement(ref index);
                LevelData level = new LevelData(new[] { name, "Custom", "" }, currentIndex, null) {
                    fullLevelData = levelData.ToArray(),
                    saveFile = name,
                    levelSprite = sprite
                };
                levelList.Add(level);
            });
            int maxColorCount = data.Values.Max(t => t.Item2);
            CreateImageSprites(maxColorCount);
            books[bookName] = levelList.ToArray();

            initialized = true;

            data.Clear(); // free up memory
        }

        private static short[] CreateLevelData(Texture2D quantizedImage) {
            List<(short, short, short)> colors = LevelDataCreator.ExtractColors(quantizedImage);
            Logger.LogDebug("Extracted " + colors.Count + " colors");
            LevelDataCreator creator = new LevelDataCreator(quantizedImage, colors);

            return creator.CreateLevelData().ToArray();
        }

        private static void CreateCache(string path, string file, short[] levelData) {
            string binFile = Path.Combine(path, Path.GetFileNameWithoutExtension(file) + ".bin");
            using (FileStream fs = new FileStream(binFile, FileMode.Create, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fs)) {
                    foreach (short item in levelData) {
                        writer.Write(item);
                    }
                }
            }
        }

        private static void CreateImageSprites(int count) {
            if (count >= 99) {
                if (CustomSprites.sprites.Count < count - 99) {
                    for (int i = 0; i < count - 99; i++) {
                        CustomSprites.CreateSprite(i);
                    }
                }
            }
        }
    }

    internal class LevelDataCreator {
        private readonly Texture2D image;
        private readonly List<short> fullLevelData;
        private readonly List<(short, short, short)> colors;

        public LevelDataCreator(Texture2D image, List<(short, short, short)> colors) {
            this.image = image;
            this.colors = colors;
            // this.CreateNewSprites();
            this.fullLevelData = new List<short>();
        }

        public List<short> CreateLevelData() {
            // Add width, height, and initial padding
            fullLevelData.Add((short)image.width);
            fullLevelData.Add((short)image.height);

            // Loop through each pixel in the image
            for (int x = 0; x < image.width; x++) {
                for (int y = 0; y < image.height; y++) {
                    int flippedY = image.height - 1 - y;
                    // Get the pixel color at the given position
                    Color32 pixel = image.GetPixel(x, flippedY);
                    short r = pixel.r;
                    short g = pixel.g;
                    short b = pixel.b;
                    short a = pixel.a;

                    // Append color index or 0 for transparency
                    fullLevelData.Add((short)(a == 0 ? 0 : this.colors.IndexOf((r, g, b)) + 1));
                }
            }

            // Add the color palette length and padding
            fullLevelData.Add((short)colors.Count);

            // Append each color component with padding
            foreach ((short, short, short) color in colors) {
                fullLevelData.Add(color.Item1);
                fullLevelData.Add(color.Item2);
                fullLevelData.Add(color.Item3);
            }

            return fullLevelData;
        }

        public static List<(short, short, short)> ExtractColors(Texture2D texture) {
            return texture.GetPixels32()
                .Where(p => p.a != 0)
                .Select(p => ((short)p.r, (short)p.g, (short)p.b)).Distinct()
                .ToList();
        }
    }
}