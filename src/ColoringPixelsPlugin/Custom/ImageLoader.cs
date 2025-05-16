using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx.Logging;
using ColoringPixelsMod;
using UnityEngine;

public static class ImageLoader {
    public static LevelData[] levels;

    private static readonly ManualLogSource Logger = CustomImagesPlugin.Log;

    public static void Init() {
        ImageLoader.Logger.LogInfo("Loading Custom Levels");
        string path = Path.Combine(Application.persistentDataPath, "Custom");
        var levelList = new List<LevelData>();
        int index = 0;
        string[] pngFiles = Directory.GetFiles(path, "*.png");

        var fileMap = new Dictionary<string, string>();
        foreach (string pngFile in pngFiles) {
            string baseName = Path.GetFileNameWithoutExtension(pngFile);
            string binFile = Path.Combine(path, baseName + ".bin");
            if (File.Exists(binFile)) {
                fileMap.Add(pngFile, binFile);
            }
        }
        
        List<string> uncached = pngFiles
            .Where(png => !fileMap.ContainsKey(png))
            .ToList();
        
//          Name, Colors, Width, Height, Sprite
        var data = new ConcurrentDictionary<string, Tuple<string, int, int, int, Sprite>>();
        var pixels = new ConcurrentDictionary<string, Color32[]>();
        var quantizedPixels = new ConcurrentDictionary<string, Color32[]>();
        const string pattern = @"^.+/(?<name>.+?)_(?<dimension>\d+?)_(?<colors>\d+)\.png$";
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
            
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            int width = texture.width;
            int height = texture.height;
            
            if (!fileMap.ContainsKey(file)) {
                // Resize the image if necessary
                // Used to later make the level data, but that's not needed if loading from cache.
                Texture2D resizedImage = ImageProcessor.ResizeImage(texture, dimension);
                pixels[file] = resizedImage.GetPixels32();
                Logger.LogDebug("Resized image to " + resizedImage.width + "x" + resizedImage.height);
                width = resizedImage.width;
                height = resizedImage.height;
            }
            
            data[file] = Tuple.Create(name, colors, width, height, sprite);
        }

        // we can only do so much in parallel due to thread safety checks in Unity
        // unfortunately, this means that we have to do this in two passes and iterate over pngFiles twice
        
        // parallelizing this is worth the performance hit though, especially when you have several large images
        Parallel.ForEach(uncached, file => {
            Logger.LogDebug("Starting quantization for " + data[file].Item1);
            quantizedPixels[file] = ImageProcessor.QuantizeImage(pixels[file], data[file].Item2);
            Logger.LogDebug("Quantized image for " + data[file].Item1);
        });
        
        foreach (string file in pngFiles) {
            short[] levelData;
            string name = data[file].Item1;
            Sprite sprite = data[file].Item5;
            if (!fileMap.TryGetValue(file, out string value)) {
                levelData = CreateLevelData(data[file].Item3, data[file].Item4, quantizedPixels[file]);
                CreateCache(path, file, levelData);
            }
            else {
                byte[] buffer = File.ReadAllBytes(value);
                levelData = new short[buffer.Length / 2];
                Buffer.BlockCopy(buffer, 0, levelData, 0, buffer.Length);
                CreateImageSprites(levelData);
            }
            
            index -= 1;
            LevelData level = new LevelData(new[] { name, "Custom", "" }, index, null) {
                fullLevelData = levelData.ToArray(),
                saveFile = name,
                levelSprite = sprite
            };
            levelList.Add(level);
        }
        
        levels = levelList.ToArray();
    }

    private static short[] CreateLevelData(int width, int height, Color32[] pixels) {
        Texture2D quantizedImage = new Texture2D(width, height);
        quantizedImage.SetPixels32(pixels);
        quantizedImage.Apply();
        Logger.LogDebug("Extracting colors...");
        List<(int, int, int)> colors_ = LevelDataCreator.ExtractColors(quantizedImage);
        LevelDataCreator creator = new LevelDataCreator(quantizedImage, colors_);

        return creator.CreateLevelData().Select(item => (short)item).ToArray();
    }
    
    private static void CreateCache(string path, string file, short[] levelData) {
        string binFile = Path.Combine(path, Path.GetFileNameWithoutExtension(file) + ".bin");
        using (FileStream fs = new FileStream(binFile, FileMode.Create, FileAccess.Write)) {
            using (BinaryWriter writer = new BinaryWriter(fs)) {
                foreach (var item in levelData) {
                    writer.Write(item);
                }
            }
        }
    }

    private static void CreateImageSprites(short[] levelData) {
        int colorCount = levelData[2 + levelData[0] * levelData[1]];
        if (colorCount >= 99) {
            if (CustomSprites.sprites.Count < colorCount - 99) {
                for (int i = 0; i < colorCount - 99; i++) {
                    CustomSprites.CreateSprite(i);
                }
            }
        }
    }
}

internal class LevelDataCreator {
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
        foreach ((int, int, int) color in colors) {
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
