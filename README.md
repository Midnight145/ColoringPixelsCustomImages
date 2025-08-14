# Coloring Pixels Image Loader

## What it is

This is a BepInEx plugin for ColoringPixels that allows you to load custom images into the game.

## What it isn't

This mod will not unlock access to any DLC that you haven't purchased, nor will it grant you any additional achievements for completing any sideloaded images.

## How to use it

1. Install BepInEx
2. Download the latest release of this plugin from the [releases page](https://github.com/Midnight145/ColoringPixelsCustomImages/releases)
3. Install the plugin by placing the downloaded `.dll` file into the `BepInEx/plugins` directory of your ColoringPixels installation.

## Loading Images
To create a custom book, create a folder called `CustomBooks` in the ColoringPixels save directory. 
1. Windows: `C:\Users\<YourUsername>\AppData\LocalLow\ToastieLabs\ColoringPixels`.
2. Linux: `~/.config/unity3d/ToastieLabs/ColoringPixels`.
3. Mac: `~/Library/Application Support/ToastieLabs/ColoringPixels`. (needs verified)

Make a new folder inside of the CustomBooks folder with whatever you want your book to be named.

Example:
- `C:\Users\<YourUsername>\AppData\LocalLow\ToastieLabs\ColoringPixels\CustomBooks\Pokemon`.

Drop your images into this folder, and use the following naming convention (optional):
`imagename_maxres_colorcount.png`
For example, `girl_200_8.png` will be an image called girl with a maximum side length of 200 pixels and 8 colors.

The plugin expands the color count to up to 999 colors, so you can use any number of colors up to that limit, unlike the default 99.

If you set the maxres and colorcount to something greater than what the corresponding image has, the game will lower the values as necessary.

For example, if I have a 100x50 image with 4 colors, and I name the file `image_200_8.png`, the game will automatically adjust the maxres to 100 and the colorcount to 4.

If you don't use the naming convention, the defaults will be a max side length of 500 and a color count of 99.
