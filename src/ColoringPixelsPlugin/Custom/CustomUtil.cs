using UnityEngine;
using UnityEngine.Tilemaps;

namespace ColoringPixelsMod.Custom {
    public class CustomUtil {
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
}
