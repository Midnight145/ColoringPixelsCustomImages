using System;
using System.Collections.Generic;
using System.IO;
using ColoringPixelsMod;
using UnityEngine;

public static class CustomSprites {
    public static readonly List<Sprite> sprites = new List<Sprite>();
    private static string filename = "spritesheet.png";
    private static byte[] fileData = File.ReadAllBytes(filename);

    private static readonly Texture2D spritesheet;
    
    static CustomSprites() {
        spritesheet = new Texture2D(1, 1);
        bool isLoaded = spritesheet.LoadImage(fileData); // Automatically resizes the texture
        if (!isLoaded) {
            CustomImagesPlugin.Log.LogError($"Failed to load image data into texture for {filename}");
        }
    }

    public static Sprite CreateSprite(int num) {
        int width = 100, height = 100, col = 30, row = 30;
        Sprite sprite = ExtractSprite(CustomSprites.spritesheet,width, height, col, row, num);
        CustomSprites.sprites.Add(sprite);
        return sprite;
    }
    
    static Sprite ExtractSprite(Texture2D spritesheet, int spriteWidth, int spriteHeight, int columns, int rows, int index)
    {
        int column = index % columns;
        int row = index / rows;
        int x = column * spriteWidth;
        int y = spritesheet.height - ((row + 1) * spriteHeight);
        
        Rect rect = new Rect(x, y, spriteWidth, spriteHeight);

        Sprite sprite = Sprite.Create(spritesheet, rect, new Vector2(0.5f, 0.5f), 100); // Pixels Per Unit (PPU) set to 100
        return sprite;
    }
}
