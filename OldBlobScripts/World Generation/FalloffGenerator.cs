using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public enum FalloffModes
    {        
        HORIZONTAL,
        VERTICAL,
        SQUARE, // horizontal and vertical
        CIRCULAR
    }

    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }

    static float Evaluate(float value, float _a, float _b)
    {
        float a = _a;
        float b = _b;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }

    public static float[,] GenerateFalloffMap(int width, int height, Vector3 factors, FalloffModes mode)
    {
        float[,] map = new float[width, height];
        int centerX = width / 2;
        int centerY = height / 2;
        float max_dst = Mathf.Sqrt(Mathf.Pow(centerX - 0, 2) + Mathf.Pow(centerY - 0, 2));

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                // casting x and y to ranges of -1 to 1
                float x = (j / (float)width) * 2 - 1;
                float y = (i / (float)height) * 2 - 1;
                float value = 0;
                switch (mode)
                {
                    case FalloffModes.SQUARE:
                        value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                        map[j, i] = Evaluate(value, factors.x, factors.y);
                        break;
                    case FalloffModes.HORIZONTAL:
                        value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(0));
                        map[j, i] = Evaluate(value, factors.x, factors.y);
                        break;
                    case FalloffModes.VERTICAL:
                        value = Mathf.Max(Mathf.Abs(0), Mathf.Abs(y));
                        map[j, i] = Evaluate(value, factors.x, factors.y);
                        break;
                    case FalloffModes.CIRCULAR:
                        float dst = Mathf.Sqrt(Mathf.Pow(centerX - j, 2) + Mathf.Pow(centerY - i, 2));                        
                        value = dst / max_dst;
                        value = Mathf.Pow(value, factors.z);
                        map[j, i] = value;
                        break;
                    default:
                        value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                        map[j, i] = Evaluate(value, factors.x, factors.y);
                        break;
                        /*
                    case FalloffModes.INVERTED_VERTICAL:
                        value = Mathf.Max(Mathf.Abs(0), Mathf.Abs(y));
                        map[j, i] = Mathf.Clamp01(1 - Evaluate(value, factors.x, factors.y));
                        break;
                        */
                }

            }
        }

        return map;
    }
}
