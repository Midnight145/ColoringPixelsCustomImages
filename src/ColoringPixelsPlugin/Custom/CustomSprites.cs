using System.Collections.Generic;
using System.IO;
using ColoringPixelsMod;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class CustomSprites {
    public static readonly List<Sprite> sprites = new List<Sprite>();
    private const string filename = "spritesheet.png";
    private static readonly byte[] fileData = File.ReadAllBytes(CustomSprites.filename);

    private static readonly Texture2D spritesheet;
    
    static CustomSprites() {
        spritesheet = new Texture2D(1, 1);
        bool isLoaded = spritesheet.LoadImage(fileData); // Automatically resizes the texture
        if (!isLoaded) {
            CustomImagesPlugin.Log.LogError($"Failed to load image data into texture for {CustomSprites.filename}");
        }
    }

    public static void CreateSprite(int num) {
        int width = 100, height = 100, col = 30, row = 30;
        Sprite sprite = ExtractSprite(width, height, col, row, num);
        CustomSprites.sprites.Add(sprite);
    }

    private static Sprite ExtractSprite(int spriteWidth, int spriteHeight, int columns, int rows, int index)
    {
        int column = index % columns;
        int row = index / rows;
        int x = column * spriteWidth;
        int y = spritesheet.height - ((row + 1) * spriteHeight);
        
        Rect rect = new Rect(x, y, spriteWidth, spriteHeight);

        Sprite sprite = Sprite.Create(spritesheet, rect, new Vector2(0.5f, 0.5f), 100);
        return sprite;
    }
    
    // called from ClickTest via injected method in TransformClickTest_Setup
    public static void ExpandTileset() {
        ClickTest clickTest = Object.FindObjectOfType<ClickTest>();
        if (clickTest.tileNorm.Length < clickTest.colourPalette.Length) {
            Tile[] buffer = clickTest.tileNorm;
            clickTest.tileNorm = new Tile[clickTest.colourPalette.Length];

            for (int i = 0; i < buffer.Length; i++) {
                clickTest.tileNorm[i] = buffer[i];
            }
            for (int i = buffer.Length; i < clickTest.colourPalette.Length; i++) {
                clickTest.tileNorm[i] = ScriptableObject.CreateInstance<Tile>();
                clickTest.tileNorm[i].sprite = CustomSprites.sprites[i - buffer.Length];
            }
        }
    }
}
