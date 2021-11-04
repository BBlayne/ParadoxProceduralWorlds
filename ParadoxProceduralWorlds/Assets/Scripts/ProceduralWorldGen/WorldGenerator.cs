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

    private WorldSettings _oldWorldSettings;

    public MapDisplay mapDisplay = null;

    private RenderTexture _heightMap = null;
    private RenderTexture _silhouetteMap = null;

    NoiseGenerator noiseGen = null;
    ShapeGenerator shapeGen = null;

    private string AppPath = "";

    // Start is called before the first frame update
    void Start()
    {
        AppPath = Application.dataPath + "/Temp/";

        //var timer = new System.Diagnostics.Stopwatch();
        //timer.Start();

        noiseGen = new NoiseGenerator(Settings._worldNoiseSettings);

        _oldWorldSettings = worldSettings;
        // get height map
        noiseGen = new NoiseGenerator(Settings._worldNoiseSettings);
        GenerateWorld();
        UpdateMapDisplay(_heightMap);


        SaveMapAsPNG("HeightMap", _heightMap);
        SaveMapAsPNG("SilhouetteFalloffMap", _silhouetteMap);


        UpdateMapDisplay(_silhouetteMap);

        shapeGen = new ShapeGenerator();

        if (_silhouetteMap == null) return;

        List<Region> regions = shapeGen.GetRegions(
            TextureGenerator.CreateTexture2D(_silhouetteMap, worldSettings._worldWidth, worldSettings._worldHeight)
        );

        Debug.Log("Num of regions that have coords: " + regions.Count);

        //timer.Stop();
        //System.TimeSpan duration = timer.Elapsed;
        //string timeElapsedMsg = "Time taken to execute start: " + duration.ToString(@"m\:ss\.fff");
        //Debug.Log(timeElapsedMsg);

        List<Vector4> colours = new List<Vector4>();

        Random.InitState((int)System.DateTime.Now.Ticks);

        for (int i = 0; i <= shapeGen.currentLabel; ++i)
        {
            colours.Add(Color.green);
        }

        for (int i = 0; i < regions.Count; ++i)
        {
            if (regions[i].RegionType == ERegionType.Land)
            {
                colours[regions[i].Label] = Color.white;
            }
            else
            {
                colours[regions[i].Label] = Color.black;
            }
            //Color randomColour = Random.ColorHSV(0, 1, 0, 1, 0.25f, 1, 1, 1);
            //colours.Add(new Vector4(randomColour.r, randomColour.g, randomColour.b, 1));
        }

        //Texture2D SilhouetteMap =
        //    TextureGenerator.CreateTexture2D(_silhouetteMap, worldSettings._worldWidth, worldSettings._worldHeight);

        //RenderTexture landColouredTexture = TextureGenerator.GetRandomColourLandRegionsTexture
        //(
        //    worldSettings._worldWidth,
        //    worldSettings._worldHeight,
        //    colours.ToArray(),
        //    SilhouetteMap.GetRawTextureData(),
        //    shapeGen.LabelledWorldGrid
        //);

        //SaveMapAsPNG("colouredLandRegions", landColouredTexture);

        //ResetRtx(landColouredTexture);

        RenderTexture colouredTexture = TextureGenerator.GetRandomColourRegionsTexture
        (
            worldSettings._worldWidth,
            worldSettings._worldHeight,
            colours.ToArray(),
            shapeGen.LabelledWorldGrid
        );

        SaveMapAsPNG("testSilhouetteRegions", colouredTexture);

        ResetRtx(colouredTexture);
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

    async void SaveMapAsPNG(string InFileName, RenderTexture InTex)
    {
        if (InTex != null)
        {
            //StartCoroutine(TextureGenerator.AsyncSaveTextureAsPNG(
            //    TextureGenerator.CreateTexture2D(InTex, worldSettings._worldWidth, worldSettings._worldHeight),
            //    InFileName
            //));

            //TextureGenerator.ThreadedSaveTextureAsPNG(
            //    TextureGenerator.CreateTexture2D(InTex, worldSettings._worldWidth, worldSettings._worldHeight),
            //    InFileName
            //);

            // attempting C# aync functionality
            await TextureGenerator.SaveTextureAsPng(
                TextureGenerator.CreateTexture2D(InTex, worldSettings._worldWidth, worldSettings._worldHeight),
                AppPath,
                InFileName + ".png"
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        // the rendering seems to be killing my frames
        // so probably these functions should only be called
        // if/when there's a change in the settings.
        if (!_oldWorldSettings.Equals(worldSettings))
        {
            _oldWorldSettings = worldSettings;
            GenerateWorld();
            if (_silhouetteMap != null)
            {
                UpdateMapDisplay(_silhouetteMap);
            }
        }
    }
}
