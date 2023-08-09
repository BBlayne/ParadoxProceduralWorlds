using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseSettings
{
    public string SeedString;
    public int Seed;
    [Range(0, 9)]
    public int Octaves;
    [Range(10, 1250)]
    public float Frequency;
    [Range(0.005f, 1.0f)]
    public float Persistence;
    [Range(0.10f, 3.5f)]
    public float Lacunarity;
    public Vector2 Offsets;
    [Range(0.0f, 0.6f)]
    public float WaterLevel;
    [Range(1.0f, 4.0f)]
    public float AFalloffFactor;
    [Range(1.0f, 4.0f)]
    public float BFalloffFactor;
}

public class NoiseGenerator
{
    private int _seed = 0;

    private NoiseSettings _settings;

    public NoiseSettings Settings 
    {
        get {
            return _settings;
        }

        set {    
            if (_settings.Seed != value.Seed)
            {
                WorldNoise3D.seed = value.Seed;
            }

            _settings = value;
        }
    }

    public NoiseGenerator()
    {
        _settings.Seed = 0;
        _settings.AFalloffFactor = 3.0f;
        _settings.BFalloffFactor = 2.2f;
        WorldNoise3D.seed = 0;
    }

    public NoiseGenerator(int InSeed = 0)
    {
        _settings.Seed = InSeed;
        _settings.AFalloffFactor = 3.0f;
        _settings.BFalloffFactor = 2.2f;
        WorldNoise3D.seed = InSeed;
    }

    public NoiseGenerator(NoiseSettings InNoiseSettings)
    {
        _seed = InNoiseSettings.Seed;
        WorldNoise3D.seed = _seed;
        Settings = InNoiseSettings;
    }

    public RenderTexture GenerateHeightMapRenderTexture(int InWidth, int InHeight)
    {
        return WorldNoise3D.GetNoiseRenderTexture(InWidth, InHeight, Settings);
    }

    public RenderTexture GenerateHeightMapRenderTexture(NoiseSettings InNoiseSettings, int InWidth, int InHeight)
    {
        Settings = InNoiseSettings;

        return WorldNoise3D.GetNoiseRenderTexture(InWidth, InHeight, InNoiseSettings);
    }

    public Texture2D GenerateHeightMapTexture(int InWidth, int InHeight)
    {
        return TextureGenerator.CreateTexture2D(
            GenerateHeightMapRenderTexture(InWidth, InHeight),
            InWidth,
            InHeight
        );
    }

    public Texture2D GenerateHeightMapTexture(NoiseSettings InNoiseSettings, int InWidth, int InHeight)
    {
        return TextureGenerator.CreateTexture2D(
            GenerateHeightMapRenderTexture(InNoiseSettings, InWidth, InHeight), 
            InWidth, 
            InHeight
        );
    }

    // get noise Silhouette
    public RenderTexture GenerateHeightMapSilhouetteRTx(NoiseSettings InNoiseSettings, int InWidth, int InHeight)
    {
        Settings = InNoiseSettings;

        return WorldNoise3D.GetNoiseSilhouetteRtx(InWidth, InHeight, InNoiseSettings);
    }
}
