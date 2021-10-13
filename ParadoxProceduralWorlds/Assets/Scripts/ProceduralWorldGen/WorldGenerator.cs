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

    NoiseGenerator noiseGen = null;

    // Start is called before the first frame update
    void Start()
    {
        noiseGen = new NoiseGenerator(Settings._worldNoiseSettings);
        // get height map
        GenerateWorld();
        UpdateMapDisplay();

        if (_heightMap != null)
        {
            TextureGenerator.SaveTextureAsPNG(
                TextureGenerator.CreateTexture2D(_heightMap, worldSettings._worldWidth, worldSettings._worldHeight),
                "TestHeightMap"
            );
        }
    }

    public void GenerateWorld()
    {
        GetHeightMap();
    }

    private void UpdateMapDisplay()
    {
        if (mapDisplay != null && _heightMap != null && mapDisplay.MapDisplayImgTarget != null)
        {
            _heightMap.filterMode = FilterMode.Point;

            mapDisplay.MapDisplayImgTarget.texture = _heightMap;
        }
    }

    private void GetHeightMap()
    {
        noiseGen.Settings = worldSettings._worldNoiseSettings;

        if (_heightMap != null)
        {
            _heightMap.Release();
            // ???
            if (_heightMap != null)
            {
                Destroy(_heightMap);
            }            
        }

        _heightMap = noiseGen.GenerateHeightMapRenderTexture(
            worldSettings._worldNoiseSettings,
            worldSettings._worldWidth,
            worldSettings._worldHeight
        );       
    }

    // Update is called once per frame
    void Update()
    {        
        GenerateWorld();
        UpdateMapDisplay();
    }
}
