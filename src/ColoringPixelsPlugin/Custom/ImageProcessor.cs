using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class ImageProcessor {
    public static Texture2D ResizeImage(Texture2D image, int maxSize = 256) {
        int width = image.width;
        int height = image.height;

        if (width <= 0 || height <= 0) {
            throw new ArgumentException("Image dimensions must be greater than zero.");
        }

        if (width > maxSize || height > maxSize) {
            if (width > height) {
                height = (int)(height * (maxSize / (float)width));
                width = maxSize;
            } else {
                width = (int)(width * (maxSize / (float)height));
                height = maxSize;
            }
        }

        if (width <= 0 || height <= 0) {
            throw new ArgumentException("Calculated dimensions must be greater than zero.");
        }

        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = rt;
        Graphics.Blit(image, rt);

        Texture2D resizedImage = new Texture2D(width, height);
        resizedImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resizedImage.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return resizedImage;
    }

    public static Color32[] QuantizeImage(Color32[] pixels, int numColors = 16) {
        List<Color32> uniqueColors = pixels.Distinct().ToList();

        if (uniqueColors.Count <= numColors) {
            return pixels;
        }

        float[][] pixelData = pixels.Select(p => new float[] { p.r, p.g, p.b }).ToArray();
        byte[] alphaValues = pixels.Select(p => p.a).ToArray();

        KMeans kmeans = new KMeans(numColors);
        int[] clusters = kmeans.Fit(pixelData, maxIterations: 25);

        for (int i = 0; i < pixelData.Length; i++) {
            int clusterIndex = clusters[i];
            if (clusterIndex >= 0 && clusterIndex < kmeans.Centroids.Length) {
                float[] cluster = kmeans.Centroids[clusterIndex];
                Color32 quantizedColor = new Color32((byte)cluster[0], (byte)cluster[1], (byte)cluster[2], alphaValues[i]);
                pixels[i] = quantizedColor;
            }
        }


        return pixels;
    }
}

internal class KMeans {
    private readonly int numClusters;
    private readonly Random rnd = new Random(0);

    public KMeans(int numClusters) => this.numClusters = numClusters;

    public float[][] Centroids { get; private set; }

    public int[] Fit(float[][] data, int maxIterations = 100, float tolerance = 1e-4f) {
        int[] labels = new int[data.Length];
        this.Centroids = InitializeCentroids(data);

        for (int iter = 0; iter < maxIterations; iter++) {
            bool centroidsChanged = false;

            for (int i = 0; i < data.Length; i++) {
                float minDistance = float.MaxValue;
                for (int j = 0; j < this.Centroids.Length; j++) {
                    float distance = EuclideanDistance(data[i], this.Centroids[j]);
                    if (distance < minDistance) {
                        minDistance = distance;
                        labels[i] = j;
                    }
                }
            }

            float[][] newCentroids = new float[this.numClusters][];
            int[] clusterSizes = new int[this.numClusters];
            for (int i = 0; i < data.Length; i++) {
                int cluster = labels[i];
                if (newCentroids[cluster] == null) {
                    newCentroids[cluster] = new float[data[i].Length];
                }

                for (int j = 0; j < data[i].Length; j++) {
                    newCentroids[cluster][j] += data[i][j];
                }

                clusterSizes[cluster]++;
            }

            for (int j = 0; j < this.numClusters; j++) {
                if (clusterSizes[j] == 0) {
                    newCentroids[j] = data[rnd.Next(data.Length)];
                } else {
                    for (int k = 0; k < newCentroids[j].Length; k++) {
                        newCentroids[j][k] /= clusterSizes[j];
                    }
                }

                if (EuclideanDistance(this.Centroids[j], newCentroids[j]) > tolerance) {
                    centroidsChanged = true;
                }
            }

            this.Centroids = newCentroids;

            if (!centroidsChanged) {
                break;
            }
        }

        return labels;
    }

    private float[][] InitializeCentroids(float[][] data) {
        return data.OrderBy(_ => rnd.Next()).Take(this.numClusters).ToArray();
    }

    private static float EuclideanDistance(float[] point1, float[] point2) {
        float sum = 0;
        for (int i = 0; i < point1.Length; i++) {
            sum += (point1[i] - point2[i]) * (point1[i] - point2[i]);
        }

        return (float)Math.Sqrt(sum);
    }
}