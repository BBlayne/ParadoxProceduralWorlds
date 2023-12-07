using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using System;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);        
        texture.Apply();

        //SaveTextureAsPNG(texture, "TestHeightMap");

        return texture;
    }

    public static Texture2D TextureFromColourMap(Color[] colourMap, Vector2 dims)
    {
        Texture2D texture = new Texture2D((int)dims.x, (int)dims.y, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();

        return texture;
    }

    public static void SaveTextureAsPNG(Texture2D texture)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/Map.png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/Map.png");
    }

    public static void SaveTextureAsPNG(Texture2D texture, string name)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Temp/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/Temp/" + name + ".png");
    }

    public static Texture2D TextureFromTiles(int width, int height, MapTile[,] tiles)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                float value = tiles[x, y].HeightValue;

                //Set color range, 0 = black, 1 = white
                pixels[x + y * width] = Color.Lerp(Color.black, Color.white, value);
            }
        }

        texture.SetPixels(pixels);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        return texture;
    }

    public static void SaveTextureAsPNG(Texture2D texture, string name, string path)
    {
        // Encode texture into PNG        
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/" + path + "/" + name + ".png", bytes);
        Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + Application.dataPath + "/" + path +"/" + name + ".png");
    }

    public static Texture2D TextureFromHeightMapThreaded(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];


        int ThreadCount = ThreadManager.Instance.ThreadCount; // should be processor count * 4.

        int ThreadWorkSizeX = width / ThreadCount; // i think 8 x-worker threads?
        int ThreadWorkSizeY = height / ThreadCount; // 4 y-worker threads?

        Thread[] threads = new Thread[ThreadCount];

        for (int tt = 0; tt < ThreadCount; tt++)
        {
            var tti = tt;
            threads[tt] = new Thread(() =>
            {
                int startJ = ThreadWorkSizeY * tti;
                int endJ = (tti * ThreadWorkSizeY) + ThreadWorkSizeY;
                if ((tti + 1) == ThreadCount) { endJ = height; }
                for (int tj = startJ; tj < endJ; tj++)
                {
                    for (int ti = 0; ti < width; ti++)
                    {
                        colourMap[tj * width + ti] = Color.Lerp(Color.black, Color.white, heightMap[ti, tj]);
                        // originalPixels[x + (rows - y -1) * width];
                        // for me: [width - ti - 1
                        //colourMap[tj * width + ti] = Color.Lerp(Color.black, Color.white, heightMap[width - ti - 1, tj]);
                    }
                }
            });

            threads[tt].Start();
        }

        for (int tt = 0; tt < ThreadCount; tt++)
        {
            threads[tt].Join();
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromHeightMap(float[] heightMap1D, int width, int height)
    {
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap1D[y * width + x]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
}
