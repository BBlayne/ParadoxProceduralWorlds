using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    /* 
     * World & Noise Settings 
     * **********************
     * [todo] move to or link to a UI controller
     * 
     * World setting is to specify broader settings
     * like the size of the game world and number of 
     * continents. Noise Settings is for how the shapes
     * of the landmasses look like.
     */
    [System.Serializable]
    public struct WorldSettings
    {
        [Range(256, 8192)]
        public int _worldHeight;
        [Range(256, 8192)]
        public int _worldWidth;

        public NoiseSettings _worldNoiseSettings;
    }
    
    public WorldSettings worldSettings;
    public WorldSettings Settings {
        get {
            return worldSettings;
        }

        set {
            worldSettings = value;
        }
    }

    public MapDisplay mapDisplay = null;

    private RenderTexture _heightMap = null;
    private RenderTexture _silhouetteMap = null;

    NoiseGenerator noiseGen = null;

    // Start is called before the first frame update
    void Start()
    {
        noiseGen = new NoiseGenerator(Settings._worldNoiseSettings);
        // get height map
        GenerateWorld();
        UpdateMapDisplay(_heightMap);
        SaveMapAsPNG("HeightMap", _heightMap);
        SaveMapAsPNG("SilhouetteFalloffMap", _silhouetteMap);
    }

    public void GenerateWorld()
    {
        ResetRtx(_heightMap);
        _heightMap = GetHeightMap();
        ResetRtx(_silhouetteMap);
        _silhouetteMap = GetHeightMapSilhouette();
    }

    private void UpdateMapDisplay(RenderTexture InMapRtx)
    {
        if (mapDisplay != null && InMapRtx != null && mapDisplay.MapDisplayImgTarget != null)
        {
            InMapRtx.filterMode = FilterMode.Point;

            mapDisplay.MapDisplayImgTarget.texture = InMapRtx;
        }
    }

    private void ResetRtx(RenderTexture InRtx)
    {
        if (InRtx != null)
        {
            InRtx.Release();
            // ???
            if (InRtx != null)
            {
                Destroy(InRtx);
            }            
        }
    }

    private RenderTexture GetHeightMap()
    {
        noiseGen.Settings = worldSettings._worldNoiseSettings;

        RenderTexture heightMap = noiseGen.GenerateHeightMapRenderTexture
        (
            worldSettings._worldNoiseSettings,
            worldSettings._worldWidth,
            worldSettings._worldHeight
        );

        return heightMap;
    }

    private RenderTexture GetHeightMapSilhouette()
    {
        noiseGen.Settings = worldSettings._worldNoiseSettings;

        RenderTexture silhouette = noiseGen.GenerateHeightMapSilhouetteRTx
        (
            worldSettings._worldNoiseSettings,
            worldSettings._worldWidth,
            worldSettings._worldHeight
        );

        return silhouette;
    }

    void SaveMapAsPNG(string InFileName, RenderTexture InTex)
    {
        if (InTex != null)
        {
            TextureGenerator.SaveTextureAsPNG(
                TextureGenerator.CreateTexture2D(InTex, worldSettings._worldWidth, worldSettings._worldHeight),
                InFileName
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        // the rendering seems to be killing my frames
        // so probably these functions should only be called
        // if/when there's a change in the settings.
        GenerateWorld();
        UpdateMapDisplay(_silhouetteMap);
    }
}
