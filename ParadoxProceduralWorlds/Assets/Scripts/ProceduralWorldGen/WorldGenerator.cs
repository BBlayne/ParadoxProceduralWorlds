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
    ProceduralWorlds.ShapeGenerator shapeGen = null;

    private string AppPath = "";

    public delegate void OnRegionsGeneratedDelegate(RegionGenerator.RegionDebugInfo regionDebugInfo);
    public static OnRegionsGeneratedDelegate regionsGeneratedDelegate;

    // Start is called before the first frame update
    void Start()
    {
        AppPath = Application.dataPath + "/Temp/";

        var timer = new System.Diagnostics.Stopwatch();
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

        shapeGen = new ProceduralWorlds.ShapeGenerator();

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

        int largestRegionIndex = 0;
        int largestRegionSize = int.MinValue;
        for (int i = 0; i < regions.Count; ++i)
        {
            if (regions[i].RegionType == ERegionType.Land)
            {
                colours[regions[i].Label] = Color.white;
                if (regions[i].Coords.Count > largestRegionSize)
                {
                    largestRegionIndex = i;
                    largestRegionSize = regions[i].Coords.Count;
                }
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

        //RenderTexture colouredTexture = TextureGenerator.GetRandomColourRegionsTexture
        //(
        //    worldSettings._worldWidth,
        //    worldSettings._worldHeight,
        //    colours.ToArray(),
        //    shapeGen.LabelledWorldGrid
        //);

        //SaveMapAsPNG("testSilhouetteRegions", colouredTexture);
        //ResetRtx(colouredTexture);

        /*
         * Region Generation, focusing on the silhouette of the largest region
         */

        RegionGenerator regionGen = new RegionGenerator();

        timer.Restart();
        timer.Start();
        RenderTexture largestRegionSilhouette = regionGen.GenerateRegionSilhouetteTexture
        (
            regions[largestRegionIndex],
            worldSettings._worldWidth,
            worldSettings._worldHeight,
            Color.black
        );
        timer.Stop();
        System.TimeSpan duration = timer.Elapsed;
        string timeElapsedMsg = "Time taken to execute GenerateRegionSilhouetteTexture: " + duration.ToString(@"m\:ss\.fff");
        Debug.Log(timeElapsedMsg);

        SaveMapAsPNG("largestRegionSilhouette", largestRegionSilhouette);

        /*
         * Scaling the result for more manageable sampling/examination
         */
        
        Vector2 scaling = new Vector2(16, 16);

        Texture2D scaledSilhouetteRegion = TextureGenerator.CreateTexture2D(largestRegionSilhouette);
        TextureGenerator.GetScaledRenderTextureGL
        (
            scaledSilhouetteRegion,
            (int)(largestRegionSilhouette.width / scaling.x),
            (int)(largestRegionSilhouette.height / scaling.y),
            FilterMode.Point
        );

        SaveMapAsPNG("scaledSilhouetteRegion", scaledSilhouetteRegion);

        Texture2D silhouetteRegionTexToScale = TextureGenerator.CreateTexture2D(largestRegionSilhouette);

        // scaling the largest region silhouette (using a shader)
        RenderTexture scaledSilhouetteRegionNN = TextureGenerator.GetScaledRenderTextureGPU(
                silhouetteRegionTexToScale,
                (int)(largestRegionSilhouette.width / scaling.x),
                (int)(largestRegionSilhouette.height / scaling.y)
        );

        Texture2D scaledSilhouetteRegionNNTex = TextureGenerator.CreateTexture2D(scaledSilhouetteRegionNN);

        SaveMapAsPNG("scaledSilhouetteRegionNN", scaledSilhouetteRegionNN);

        // scaling the height map for sampling
        RenderTexture scaledHeightMapNNRT = TextureGenerator.GetScaledRenderTextureGPU(
                _heightMap,
                (int)(_heightMap.width / scaling.x),
                (int)(_heightMap.height / scaling.y)
        );
        SaveMapAsPNG("scaledHeightMapNNRT", scaledHeightMapNNRT);

        Texture2D scaledHeightMapNNTex = TextureGenerator.CreateTexture2D(scaledHeightMapNNRT);

        //timer.Restart();
        //timer.Start();
        //RegionGenerator.RegionInfo largestRegionInfo = regionGen.GenerateConstrainedDelaunayMeshForRegion
        //    (
        //        regions[largestRegionIndex],
        //        1000
        //    );

        //List<Vector3> largestRegionMeshPixels = RegionDebug.DebugConstrainedDelaunayMeshToPixels
        //    (
        //        largestRegionInfo.DelaunayMesh, 
        //        largestRegionInfo.BoundaryMesh
        //    );

        //RenderTexture ConstrainedDelaunayRegionTex = RegionDebug.DebugDrawConstainedDelaunayRegion
        //(
        //    largestRegionMeshPixels,
        //    largestRegionSilhouette,
        //    Color.red
        //) as RenderTexture;

        //timer.Stop();
        //System.TimeSpan drawDelaunayDuration = timer.Elapsed;
        //string drawDelaunayTimeElapsedMsg = "Time to create delaunay region texture: " + drawDelaunayDuration.ToString(@"m\:ss\.fff");
        //Debug.Log(drawDelaunayTimeElapsedMsg);

        //SaveMapAsPNG("ConstrainedDelaunayRegionTex", ConstrainedDelaunayRegionTex);

        //ResetRtx(ConstrainedDelaunayRegionTex);

        /*****************************
         * Region Edge Detection
         *****************************/
        timer.Restart();
        timer.Start();
        List<Vector3> edgePixels = regionGen.GetScaledRegionEdges(regions[largestRegionIndex], scaling, scaledSilhouetteRegionNNTex);
        Texture2D scaledRegionEdgesTex = RegionDebug.DisplayRegionEdgePixels(edgePixels, scaledSilhouetteRegionNNTex, Color.red);
        SaveMapAsPNG("scaledRegionEdgesTex", scaledRegionEdgesTex);
        timer.Stop();
        System.TimeSpan regionEdgesDuration = timer.Elapsed;
        string regionEdgesDurationMsg = "Time to regionEdgesDuration: " + regionEdgesDuration.ToString(@"m\:ss\.fff");
        Debug.Log(regionEdgesDurationMsg);

        /*****************************
         * Scaled SubRegion Detection
         *****************************/

        //timer.Restart();
        //timer.Start();
        //SubregionGenerator subregionGen = new SubregionGenerator();
        //Texture2D scaledSubRegionsOfLargestRegion = subregionGen.GetSubRegionsFromRegion(
        //    scaledSilhouetteRegionNNTex, 
        //    scaledHeightMapNNTex, 
        //    regionGen.GenerateSitesForRegion(regions[largestRegionIndex], 200, scaling)
        //);

        //timer.Stop();
        //System.TimeSpan scaledSubRegionsDuration = timer.Elapsed;
        //string scaledSubRegionsDurationMsg = "Time to GetSubRegionsFromRegion: " + scaledSubRegionsDuration.ToString(@"m\:ss\.fff");
        //Debug.Log(scaledSubRegionsDurationMsg);

        //SaveMapAsPNG("scaledSubRegionsOfLargestRegion", scaledSubRegionsOfLargestRegion);

        ResetRtx(scaledHeightMapNNRT);
        ResetRtx(scaledSilhouetteRegionNN);
        ResetRtx(largestRegionSilhouette);

        //timer.Restart();
        //timer.Start();
        //RenderTexture InitedMaskTexture = regionGen.GenerateTestRegion
        //(
        //    regions[largestRegionIndex], 
        //    250,
        //    worldSettings._worldWidth,
        //    worldSettings._worldHeight
        //);
        //timer.Stop();
        //System.TimeSpan duration = timer.Elapsed;
        //string timeElapsedMsg = "Time taken to execute GenerateTestRegion: " + duration.ToString(@"m\:ss\.fff");
        //Debug.Log(timeElapsedMsg);

        //RegionDebug.DebugRegionSites
        //(
        //    regionGen.debugRegionInfo,
        //    TextureGenerator.CreateTexture2D(InitedMaskTexture)
        //);

        //SaveMapAsPNG("InitMaskRegion_Largest", InitedMaskTexture);

        //ResetRtx(InitedMaskTexture);
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

    async void SaveMapAsPNG(string InFileName, Texture2D InTex)
    {
        if (InTex != null)
        {
            // attempting C# aync functionality
            await TextureGenerator.SaveTextureAsPng(
                InTex,
                AppPath,
                InFileName + ".png"
            );
        }
    }

    async void SaveMapAsPNG(string InFileName, RenderTexture InTex)
    {
        if (InTex != null)
        {
            // attempting C# aync functionality
            await TextureGenerator.SaveTextureAsPng(
                TextureGenerator.CreateTexture2D(InTex),
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
