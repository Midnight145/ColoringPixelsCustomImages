using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using BepInEx.Logging;
using ColoringPixelsMod;
using UnityEngine;

public static class ImageLoader {
    public static LevelData[] levels;

    private static ManualLogSource Logger = CustomImagesPlugin.Log;

    public static void Init() {
        Logger.LogInfo("Loading Custom Levels");
        string path = Path.Combine(Application.persistentDataPath, "Custom");
        List<LevelData> levelList = new List<LevelData>();
        int index = 0;
        string[] pngFiles = Directory.GetFiles(path, "*.png");
        string[] binFiles = Directory.GetFiles(path, "*.bin");

        foreach (var file in pngFiles) {
            ImageLoader.Logger.LogInfo(file);
        }
        
        var binNames = new HashSet<string>(binFiles.Select(f => Path.GetFileNameWithoutExtension(f)));
        var pngMap = pngFiles
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => f);
        var binToPng = new Dictionary<string, string>();
        foreach (var binFile in binFiles) {
            string baseName = Path.GetFileNameWithoutExtension(binFile);
            if (pngMap.TryGetValue(baseName, out var pngFile)) {
                binToPng[binFile] = pngFile;
            }
        }
        
        var uncached = pngFiles
            .Where(png => !binNames.Contains(Path.GetFileNameWithoutExtension(png)))
            .ToList();
        
        // print dicts
        
        Logger.LogInfo("PNG to BIN mapping:");
        foreach (var kvp in binToPng) {
            Logger.LogInfo($"BIN: {kvp.Key}, PNG: {kvp.Value}");
        }
        Logger.LogInfo("Uncached PNG files:");
        foreach (var file in uncached) {
            Logger.LogInfo(file);
        }

        var data = new ConcurrentDictionary<string, Tuple<string, int, int, int, int>>();
        var pixels = new ConcurrentDictionary<string, Color32[]>();
        var quantizedPixels = new ConcurrentDictionary<string, Color32[]>();
        foreach (var file in uncached) {
            byte[] fileData = File.ReadAllBytes(file);
            if (fileData.Length == 0) {
                Logger.LogError($"Failed to read file data for {file}");
                continue;
            }
            Texture2D texture = new Texture2D(2, 2); // Temporary size, will adjust to the image size
            bool isLoaded = texture.LoadImage(fileData); // Automatically resizes the texture
            
            
            if (!isLoaded) {
                Logger.LogError($"Failed to load image data into texture for {file}");
                continue;
            }
            
            Logger.LogDebug("Processing file " + file);
            string pattern = @"^.+/(?<name>.+?)_(?<dimension>\d+?)_(?<colors>\d+)\.png$";
            Regex regex = new Regex(pattern);

            Match match = regex.Match(file);
            string name;
            int dimension, colors;
            if (match.Success) {
                name = match.Groups["name"].Value;
                dimension = int.Parse(match.Groups["dimension"].Value);
                colors = int.Parse(match.Groups["colors"].Value);
            } else {
                name = file;
                dimension = 100;
                colors = 25;
            }

            Logger.LogDebug("Matched " + name + " " + dimension + " " + colors);
            Texture2D resizedImage = ImageProcessor.ResizeImage(texture, dimension);
            pixels[file] = resizedImage.GetPixels32();
            Logger.LogDebug("Resized image to " + resizedImage.width + "x" + resizedImage.height);
            data[file] = Tuple.Create(name, dimension, colors, resizedImage.width, resizedImage.height);
        }

        // Process files for resizing on the main thread
        Parallel.ForEach(uncached, file => {
            Logger.LogDebug("Starting quantization for " + data[file].Item1);
            quantizedPixels[file] = ImageProcessor.QuantizeImage(pixels[file], data[file].Item3);
            Logger.LogDebug("Quantized image for " + data[file].Item1);
        });
        
        foreach (var file in uncached) {
            Logger.LogInfo(file);
            (int width, int height) = (data[file].Item4, data[file].Item5);
            string name = data[file].Item1;
            Texture2D quantizedImage = new Texture2D(width, height);
            Color32[] newPixels = quantizedPixels[file];
            quantizedImage.SetPixels32(newPixels);
            quantizedImage.Apply();
            Sprite sprite = Sprite.Create(quantizedImage, new Rect(0, 0, quantizedImage.width, quantizedImage.height), new Vector2(0.5f, 0.5f));
            Logger.LogDebug("Successfully created sprite for " + name);
            
            Logger.LogDebug("Extracting colors...");
            List<(int, int, int)> colors_ = LevelDataCreator.ExtractColors(quantizedImage);
            LevelDataCreator creator = new LevelDataCreator(quantizedImage, colors_);
            List<short> fullLevelData = new List<short>();
            foreach (var item in creator.CreateLevelData()) {
                fullLevelData.Add((short)item);
            }
            int currentIndex = Interlocked.Increment(ref index) - 1;
            LevelData level = new LevelData(new[] { name, "Custom", "" }, currentIndex, null) {
                fullLevelData = fullLevelData.ToArray(),
                saveFile = name,
                levelSprite = sprite
            };
            levelList.Add(level);
            Logger.LogInfo("Loaded " + name);
            // save level data to bin
            string binFile = Path.Combine(path, Path.GetFileNameWithoutExtension(file) + ".bin");
            using (FileStream fs = new FileStream(binFile, FileMode.Create, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fs)) {
                    foreach (var item in level.fullLevelData) {
                        writer.Write(item);
                    }
                }
            }
        }
        
        foreach (var file in binFiles) {
            string basename = Path.GetFileNameWithoutExtension(file);
            byte[] fileData = File.ReadAllBytes(file);
            byte[] image = File.ReadAllBytes(binToPng[file]);
            Texture2D texture = new Texture2D(2, 2); // Temporary size, will adjust to the image size
            bool isLoaded = texture.LoadImage(image); // Automatically resizes the texture
            if (!isLoaded) {
                Logger.LogError($"Failed to load image data into texture for {file}");
                continue;
            }
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            
            if (fileData.Length == 0) {
                Logger.LogError($"Failed to read file data for {file}");
                continue;
            }
            
            index = Interlocked.Increment(ref index) - 1;
            string pattern = @"^.+/(?<name>.+?)_(?<dimension>\d+?)_(?<colors>\d+)\.bin";
            Regex regex = new Regex(pattern);

            Match match = regex.Match(file);
            string name = match.Success ? match.Groups["name"].Value : file;
            LevelData level = new LevelData(new[] { name, "Custom", "" }, index, null) {
                fullLevelData = new short[fileData.Length / 2],
                saveFile = name,
                levelSprite = sprite
            };
            Buffer.BlockCopy(fileData, 0, level.fullLevelData, 0, fileData.Length);
            levelList.Add(level);
            Logger.LogInfo("Loaded " + name);
        }

        levels = levelList.ToArray();
    }
}

public class LevelDataCreator {
    private readonly Texture2D image;
    private readonly List<int> fullLevelData;
    private readonly List<(int, int, int)> colors;

    public LevelDataCreator(Texture2D image, List<(int, int, int)> colors) {
        this.image = image;
        this.colors = colors;
        this.CreateNewSprites();
        this.fullLevelData = new List<int>();
    }

    private void CreateNewSprites() {
        if (this.colors.Count >= 99) {
            if (CustomSprites.sprites.Count < this.colors.Count - 99) {
                for (int i = 0; i < this.colors.Count - 99; i++) {
                    CustomSprites.CreateSprite(i);
                }
            }
        }
    }

    public List<int> CreateLevelData() {
        // Add width, height, and initial padding
        fullLevelData.Add(image.width);
        fullLevelData.Add(image.height);

        // Loop through each pixel in the image
        for (int x = 0; x < image.width; x++) {
            for (int y = 0; y < image.height; y++) {
                int flippedY = image.height - 1 - y;
                // Get the pixel color at the given position
                Color32 pixel = image.GetPixel(x, flippedY);
                int r = pixel.r;
                int g = pixel.g;
                int b = pixel.b;
                int a = pixel.a;

                // Append color index or 0 for transparency
                fullLevelData.Add(a == 0 ? 0 : colors.IndexOf((r, g, b)) + 1);
            }
        }

        // Add the color palette length and padding
        fullLevelData.Add(colors.Count);

        // Append each color component with padding
        foreach (var color in colors) {
            fullLevelData.Add(color.Item1);
            fullLevelData.Add(color.Item2);
            fullLevelData.Add(color.Item3);
        }

        return fullLevelData;
    }
    
    public static List<(int, int, int)> ExtractColors(Texture2D texture)
    {
        var uniqueColors = new HashSet<(int, int, int)>();
        Color32[] pixels = texture.GetPixels32();

        foreach (Color32 pixel in pixels)
        {
            // Ignore fully transparent pixels
            if (pixel.a == 0)
                continue;

            // Add unique (R, G, B) color
            uniqueColors.Add((pixel.r, pixel.g, pixel.b));
        }

        return new List<(int, int, int)>(uniqueColors);
    }
}
