using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct NoiseSettings
{
    int Height;
    int Width;
    int Seed;
    int Octaves;
    float Frequency;
    float Persistence;
    float Lacunarity;
    Vector2 Offsets;
}

public class NoiseGenerator
{
    private NoiseSettings _noiseSettings;
    public NoiseSettings Settings {
        get {
            return _noiseSettings;
        }

        set {
            _noiseSettings = value;
        }
    }

    public NoiseGenerator()
    {

    }

    public NoiseGenerator(NoiseSettings InNoiseSettings)
    {
        Settings = InNoiseSettings;
    }
}
