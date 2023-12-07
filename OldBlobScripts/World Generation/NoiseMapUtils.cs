using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class NoiseMapUtils
{
    public enum BLEND_MODES
    {
        NORMAL,
        MULTIPLY,
        DIVIDE,
        ADDITIVE, // also called linear dodge
        SUBTRACT,
        DIFFERENCE,
        OVERLAY,
        SCREEN
    }

    static float[,] Blend_Normal(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float v = mapB[i, j];
                blendMinMax.AddValue(v);
            }
        }
        return mapB;
    }

    static float[,] Blend_Multiply(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        float[,] blendedMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float v = mapA[i, j] * mapB[i, j];
                blendedMap[i, j] = v;
                blendMinMax.AddValue(v);
            }
        }

        return blendedMap;
    }

    static float[,] Blend_Divide(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        float[,] blendedMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float b = mapB[i, j];
                float a = mapA[i, j];
                float v = 0;

                /*
                Color colourA = Color.Lerp(Color.black, Color.white, a);
                Color colourB = Color.Lerp(Color.black, Color.white, b);

                Vector3 colVecA = new Vector3(colourA.r, colourA.g, colourA.b);
                Vector3 colVecB = new Vector3(colourB.r, colourB.g, colourB.b);

                float br = Mathf.Max(0.1f, colourB.r);
                float bg = Mathf.Max(0.1f, colourB.g);
                float bb = Mathf.Max(0.1f, colourB.b);

                Vector3 colVecV = new Vector3(Mathf.Clamp01(colourA.r / br),
                                              Mathf.Clamp01(colourA.g / bg),
                                              Mathf.Clamp01(colourA.b / bb));

                */
                //v
                // set to be between -1 and 1
                //v = v * 2 - 1;

                //v = colVecV.x;
                b = Mathf.Max(0.1f, b);
                a = Mathf.Max(0.1f, a);
                v = a / b;
                v = Mathf.Clamp01(v);
                //v = Mathf.Log10(v);
                blendedMap[i, j] = v;
                blendMinMax.AddValue(v);
                //Debug.Log(v);
            }
        }

        return blendedMap;
    }

    static float[,] Blend_Add(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        float[,] blendedMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float v = mapA[i, j] + mapB[i, j];
                blendedMap[i, j] = v;
                blendMinMax.AddValue(v);
            }
        }

        return blendedMap;
    }

    static float[,] Blend_Diff(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        float[,] blendedMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float v = mapA[i, j] - mapB[i, j];
                blendedMap[i, j] = v;
                blendMinMax.AddValue(v);
            }
        }

        return blendedMap;
    }

    static float[,] Blend_Screen(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        float[,] blendedMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float v = (1 - mapA[i, j]) * (1 - mapB[i, j]);
                blendedMap[i, j] = v;
                blendMinMax.AddValue(v);
            }
        }

        return blendedMap;
    }

    static float[,] Blend_Overlay(float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        float[,] blendedMap = new float[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float v_a = mapA[i, j];
                float v_b = mapB[i, j];
                float v = 0;
                if (v_a < 0.5f)
                {
                    v = 2 * v_a * v_b;
                }
                else
                {
                    v = 1 - (2 * (1 - v_a) * (1 - v_b));
                }
                blendedMap[i, j] = v;
                blendMinMax.AddValue(v);
            }
        }

        return blendedMap;
    }

    // Given layer a and a layer b where b is the top layer and a is the underlying layer.
    public static float[,] Blend(BLEND_MODES blend_mode, float[,] mapA, float[,] mapB, int width, int height, WorldMinMax blendMinMax)
    {
        switch (blend_mode)
        {
            case BLEND_MODES.NORMAL:
                return Blend_Normal(mapA, mapB, width, height, blendMinMax);
            case BLEND_MODES.MULTIPLY:
                return Blend_Multiply(mapA, mapB, width, height, blendMinMax);
            //case BLEND_MODES.DIVIDE:
            //    return Blend_Divide(mapA, mapB, width, height, blendMinMax);
            case BLEND_MODES.ADDITIVE:
                return Blend_Add(mapA, mapB, width, height, blendMinMax);
            case BLEND_MODES.DIFFERENCE:
                return Blend_Diff(mapA, mapB, width, height, blendMinMax);
            case BLEND_MODES.OVERLAY:
                return Blend_Overlay(mapA, mapB, width, height, blendMinMax);
            case BLEND_MODES.SCREEN:
                return Blend_Screen(mapA, mapB, width, height, blendMinMax);
        }

        return new float[width, height];
    }

    public static float[,] ApplyFalloffMap(int Width, int Height, float[,] map, float[,] falloffMap, float cutoff)
    {
        float[,] finalMap = new float[Width, Height];
        for (int i = 0; i < Width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                float v = map[i, j] - falloffMap[i, j];
                //finalMap[i, j] = Mathf.Max(0, v);
                if (v < cutoff)
                {
                    v = 0;
                }

                finalMap[i, j] = v;
            }
        }

        return finalMap;
    }

    public static Vector2[] GenerateOctaveLayerOffsets(System.Random prng, int numOctaves, Vector2 offset)
    {
        Vector2[] octaveOffsets = new Vector2[numOctaves];
        for (int i = 0; i < numOctaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        return octaveOffsets;
    }

    public static float[,] GenerateSimplexNoiseMap2D(int mapWidth,
                                                        int mapHeight,
                                                        int seed,
                                                        float scale,
                                                        int octaves,
                                                        float persistance,
                                                        float lacunarity,
                                                        Vector3 offset,
                                                        float minHeight,
                                                        float perturbation,
                                                        WorldMinMax worldMinMax)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        FastNoise fastNoise = new FastNoise(seed);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float dx = x;
                float dy = y;

                /*
                float base_v = GetBaseNoise(fastNoise,
                    x, y, mapWidth, mapHeight,
                    octaves, octaveOffsets,
                    scale, lacunarity, persistance, worldMinMax);

                
                float ridged_v =
                    GetRidgedNoise(fastNoise, x, y, mapWidth, mapHeight,
                    octaves, octaveOffsets,
                    scale, lacunarity, persistance, worldMinMax);
                    

                float billowed_v =
                    GetBillowedNoise(fastNoise, x, y, mapWidth, mapHeight,
                    octaves, octaveOffsets,
                    scale, lacunarity, persistance, worldMinMax);

                float v1 = (billowed_v * 2) - 1;
                v1 *= perturbation;

                float xin = dx + v1;
                float yin = dy + v1;

                float perturbed_v = GetRidgedNoise(fastNoise, xin, yin, mapWidth, mapHeight,
                    octaves, octaveOffsets,
                    scale, lacunarity, persistance, worldMinMax);

                float v2 = (perturbed_v * base_v) - minHeight;

                worldMinMax.AddValue(v2);

                noiseMap[x, y] = v2;
                */
                noiseMap[x, y] = 0;
            }
        }

        return noiseMap;
    }

    public static float Normalize01(float val, float min, float max)
    {
        float divisor = max - min;
        if (divisor <= 0)
            return 1f;
        return (val - min) / divisor;
    }
}
