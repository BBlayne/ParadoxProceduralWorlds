using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMesh = TriangleNet.Mesh;
using TriangleNet.Voronoi;
using TMPro;

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

    public Material WireframeMaterial = null;

    private RenderTexture _heightMap = null;
    private RenderTexture _silhouetteMap = null;
    private RenderTexture PolyMapRT = null;
    private RenderTexture ContinentMapRT = null;

    private UnityEngine.Mesh TestMesh = null;

    struct Edge
    {
        public Vector2Int StartPoint;
        public Vector2Int EndPoint;
    }

    private List<Edge> TestEdges = new List<Edge>();

    private bool bDisplayPossionPoints = true;
    private List<Vector3> PoissonPoints = new List<Vector3>();

    public Material UnlitMaterial;
    public Material UnlitTextureMaterial;

    NoiseGenerator noiseGen = null;
    ProceduralWorlds.ShapeGenerator shapeGen = null;
    ProceduralWorlds.PolygonalMapGenerator PolyMapGen = null;

    public Texture2D VoronoiTexture = null;

    public int TargetNumberOfCells = 1000;
    public int MapPadding = 25;
    public int NumberOfTectonicPlates = 30;
    public int NumberOfContinents = 3;
    public float WorldLandmassPercentage = 0.25f;
    public bool bIsRandomFloodFill = false;
    public bool bOutputDebugMaps = true;

    public TMP_Text TestTMPText = null;

    private string AppPath = "";
    private string ImageExportPath = "../ExportedImages";

    public delegate void OnRegionsGeneratedDelegate(RegionGenerator.RegionDebugInfo regionDebugInfo);
    public static OnRegionsGeneratedDelegate regionsGeneratedDelegate;

    void OnDrawGizmos()
    {
        //Gizmos.color = Color.white;
        //Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
        //Gizmos.DrawCube(pos, Vector3.one);
        /*
        if (TestMesh != null)
        {
            Gizmos.color = Color.white;
            foreach (Edge mEdge in TestEdges)
            {
                Gizmos.DrawLine(
                    new Vector3(mEdge.StartPoint.x, mEdge.StartPoint.y, 0), 
                    new Vector3(mEdge.EndPoint.x, mEdge.EndPoint.y, 0)
                );
            }
        }

        if (bDisplayPossionPoints)
        {
            foreach (Vector2 PoissonPoint in PoissonPoints)
            {
                Gizmos.color = Color.green;
                Vector3 pos = new Vector3(PoissonPoint.x, PoissonPoint.y, 0);
                Gizmos.DrawCube(pos, 5*Vector3.one);
            }
        }
        */
    }

    // Start is called before the first frame update
    void Start()
    {
        //var timer = new System.Diagnostics.Stopwatch();
        //timer.Start();

        AppPath = Application.dataPath + "/../ExportedImages/";



        _oldWorldSettings = worldSettings;

        GenerateWorld();
    }

    public void GenerateWorld()
    {
        ResetRtx(PolyMapRT);

        PolyMapGen = new ProceduralWorlds.PolygonalMapGenerator("Blayne", TargetNumberOfCells);
        PolyMapGen.SetDistributionMode(ProceduralWorlds.ESiteDistribution.RANDOM_MIRRORED);
        PolyMapGen.SetPadding(MapPadding);
        Vector2Int WorldSizes = new Vector2Int(worldSettings._worldWidth, worldSettings._worldHeight);
        PolyMapGen.MapDimensions = WorldSizes;
        int NumTectonicPlates = NumberOfTectonicPlates;

        TestTMPText.gameObject.SetActive(true);
        TestTMPText.SetText("");
        TestTMPText.ForceMeshUpdate();

        // World Map Generation
        Mesh WorldMapMesh = PolyMapGen.GeneratePolygonalMapMesh();
        int TotalNumberVorCells = PolyMapGen.BoundedVor.Faces.Count;

        BoundedVoronoi TheBoundedVoronoiGraph = PolyMapGen.BoundedVor;
        Mesh VoronoiUnityMesh = PolyMapGen.GenerateVoronoiUnityMesh(TheBoundedVoronoiGraph);
        RenderTexture VoronoiGraphRTex = PolyMapGen.RenderPolygonalWireframeMap(VoronoiUnityMesh, TextureGenerator.GetUnlitMaterial(), Color.white);
        SaveMapAsPNG("VoronoiGraphRTex", VoronoiGraphRTex);

        RenderTexture TriangRTex = PolyMapGen.RenderPolygonalWireframeMap(WorldMapMesh, null, TextureGenerator.GetUnlitMaterial(), Color.white);
        SaveMapAsPNG("TriangRTex", TriangRTex);

        RenderTexture CellIDRTex = PolyMapGen.GenerateVorCellIdTex(TheBoundedVoronoiGraph, WorldSizes, TestTMPText, 0.005f);
        SaveMapAsPNG("CellIDRTex", CellIDRTex);
        TestTMPText.gameObject.SetActive(false);

        //int PoissonPlatesRadius = 256; // relative to pixel dimensions of our target texture for rendering
        int[] VoronoiTectonicCells = GenerateTectonicPlates
        (
            PolyMapGen.MapDimensions, 
            NumTectonicPlates, 
            MapPadding,
            TheBoundedVoronoiGraph, 
            bIsRandomFloodFill
        );

        EPlateDirections[] PlateDirections = new EPlateDirections[NumTectonicPlates];
        int NumDirections = (int)EPlateDirections.NUM_DIRECTIONS;
        for (int i = 0; i < NumTectonicPlates; i++)
        {
            int Direction = Random.Range(0, NumDirections);
            EPlateDirections PlateDirection = (EPlateDirections)Direction;
            PlateDirections[i] = PlateDirection;
        }

        Vector2Int Hues = new Vector2Int(30, 330);
        Vector2Int Saturation = new Vector2Int(99, 100);
        Vector2Int Brightness = new Vector2Int(99, 100);
        List<Color> PlateColours = TextureGenerator.GenerateHSVColours(NumTectonicPlates + 1, Hues, Saturation, Brightness);
        PlateColours.Shuffle();

        PolyMapRT = PolyMapGen.MainRTex;

        ResetRtx(PolyMapRT);

        Texture2D PlateTexMap = TextureGenerator.GenerateTectonicPlateTextureMap(TotalNumberVorCells, VoronoiTectonicCells, PlateColours);
        PolyMapRT = PolyMapGen.RenderPolygonalMap(WorldMapMesh, PlateTexMap, TextureGenerator.GetUnlitTextureMaterial());
        RenderTexture OutlineTest = TextureGenerator.DrawTextureOutline(PolyMapRT);
        RenderTexture ThickenedOutlineTest = TextureGenerator.DrawTextureOutline(OutlineTest); // ARGH!!

        SaveMapAsPNG("PlateOutlineTexMap", OutlineTest);
        SaveMapAsPNG("PlateThickenedOutlineTexMap", ThickenedOutlineTest);
        SaveMapAsPNG("PlateTexMap", PolyMapRT);

        Mesh ArrowMesh = PolyMapGen.GenerateTectonicPlateCellArrows(TheBoundedVoronoiGraph, VoronoiTectonicCells, PlateDirections);
        RenderTexture ArrowRT = PolyMapGen.RenderArrows(ArrowMesh, Color.red);

        SaveMapAsPNG("ArrowTexMap", ArrowRT);

        //UpdateMapDisplay(PolyMapRT, WorldSizes);

        int[] VoronoiContinentCells = GenerateContinents
        (
            TheBoundedVoronoiGraph,
            WorldSizes,
            3,
            MapPadding,
            0.3f
        );

        List<Color> ContinentColours = TextureGenerator.GenerateHSVColours(3 + 1, Hues, Saturation, Brightness);
        ContinentColours[0] = Color.black;

        Texture2D ContinentTexMap = TextureGenerator.GenerateTectonicPlateTextureMap(TotalNumberVorCells, VoronoiContinentCells, ContinentColours);
        ContinentMapRT = PolyMapGen.RenderPolygonalMap(WorldMapMesh, ContinentTexMap, TextureGenerator.GetUnlitTextureMaterial());

        SaveMapAsPNG("ContinentTexMap", ContinentMapRT);
        UpdateMapDisplay(ContinentMapRT, WorldSizes);

        List<Texture> TexturesToMerge = new List<Texture>();
        TexturesToMerge.Add(ContinentMapRT);
        TexturesToMerge.Add(VoronoiGraphRTex);
        TexturesToMerge.Add(ArrowRT);
        TexturesToMerge.Add(ThickenedOutlineTest);
        //Texture2D OverlayedMapTex = TextureGenerator.MergeTextures(TexturesToMerge.ToArray());
        RenderTexture OverlayedMapTex = TextureGenerator.MergeTexturesToRenderTexture(TexturesToMerge.ToArray());
        SaveMapAsPNG("OverlayedMapTex", OverlayedMapTex);

        List<Texture> DebugTexturesToMerge = new List<Texture>();
        DebugTexturesToMerge.Add(ContinentMapRT);
        DebugTexturesToMerge.Add(VoronoiGraphRTex);
        DebugTexturesToMerge.Add(CellIDRTex);
        RenderTexture DebugOverlayedMapRTex = TextureGenerator.MergeTexturesToRenderTexture(DebugTexturesToMerge.ToArray());
        SaveMapAsPNG("DebugOverlayedMapRTex", DebugOverlayedMapRTex);

        UpdateMapDisplay(OverlayedMapTex, WorldSizes);
    }

    private List<Vector3> PickPoissonRandomPoints(Vector2Int InWorldSizes, int InNumPoints, int InPadding, int InRadius)
    {
        List<Vector3>  OutPoints = MapUtils.GetPoissonDistributedPoints2D
        (
            InWorldSizes, InRadius, 30, InPadding, new List<Vector3>(), InNumPoints
        );

        return OutPoints;
    }

    private List<Vector3> GenerateTectonicPlateSeedPoints(Vector2Int InWorldSizes, int InPadding, int InNumPlates)
    {
        // If I can't end up with a different implementation for continents might need to rename this to something
        // More generic, like "GenerateWorldFeaturePoints(...)" for landmasses, plates, etc.

        List<Vector3> OutPlateSites = new List<Vector3>();

        // Get Initial Sample Sites either Randomly or via Poisson Disc Sampling
        int PoissonRadius = MapUtils.DetermineRadiusForPoissonDisc(InWorldSizes, InNumPlates);
        ProceduralWorlds.ESiteDistribution PlateSiteDistribution = ProceduralWorlds.ESiteDistribution.RANDOM;
        List<Vector3> InitialSamplePoints = MapUtils.GenerateSiteDistribution(
            PlateSiteDistribution,
            InNumPlates,
            InWorldSizes,
            InPadding,
            PoissonRadius,
            null
        );

        // Get the barycentric dual mesh, we'll extract the tectonic plate seed points from the voronoi diagram
        TMesh PlateTriang = PolyMapGen.GenerateDelaunayTriangulation(InitialSamplePoints.ToArray(), 5);
        BoundedVoronoi PlateVor = PolyMapGen.GetBoundedVoronoiTesselationFromTriangulation(PlateTriang);
        if (bOutputDebugMaps)
        {
            Mesh PlateTriangMesh = PolyMapGen.GenerateUnityMeshFromTriangleNetMesh(PlateTriang);
            RenderTexture PlateTriangMeshRT = TextureGenerator.BlitMeshToRT(PlateTriangMesh, InWorldSizes, TextureGenerator.GetUnlitMaterial());
            TextureGenerator.SaveMapAsPNG("PlateTriangRT", PlateTriangMeshRT);
            ResetRtx(PlateTriangMeshRT);
            Mesh PlateSiteMesh = PolyMapGen.GenerateVoronoiUnityMesh(PlateVor);
            RenderTexture PlateRT = TextureGenerator.BlitMeshToRT(PlateSiteMesh, InWorldSizes, TextureGenerator.GetUnlitMaterial());
            TextureGenerator.SaveMapAsPNG("PlateVoronoiRT", PlateRT);
            ResetRtx(PlateRT);
        }

        foreach (var VorFace in PlateVor.Faces)
        {
            if (VorFace.ID != -1 && VorFace.bounded)
            {
                float Jitter = Random.Range(-PoissonRadius / 2, PoissonRadius / 2);
                float NuEcks = (float)VorFace.generator.X + Jitter;
                float NuWhy = (float)VorFace.generator.Y + Jitter;
                OutPlateSites.Add(new Vector3(NuEcks, NuWhy));
            }
        }

        return OutPlateSites;
    }

    private int[] GenerateTectonicPlates(Vector2Int InWorldSizes, int InNumPlates, int InPadding, BoundedVoronoi InMapCells, bool bIsRandom)
    {
        List<Vector3> PlatePointCenters = GenerateTectonicPlateSeedPoints(InWorldSizes, InPadding, InNumPlates);

        List<int> PlateCentersFaceIndices = PolyMapGen.GetCellIDsFromCoordinates(InMapCells, PlatePointCenters);

        /* Random Flood Fill Algorithm */
        return PolyMapGen.TectonicPlatesFloodFill2D(InMapCells, PlateCentersFaceIndices, bIsRandom);
    }



    private int[] GenerateContinents(BoundedVoronoi InVor, Vector2Int InWorldSizes, int InNumContinents, int InPadding, float LandWaterRatio)
    {
        // Initial Continents are generated semi-randomly similar to plates, but instead of random flood-filling
        // until the entire map is filled, we halt for each continent once its reached a specified number of cells.
        // As such, given a total number of cells, we derive the amount of land cells from the ratio. i.e 30% Land vs 70% Water.
        int TotalLandCellCount = Mathf.RoundToInt(LandWaterRatio * InVor.Faces.Count);
        int TotalWaterCellCount = InVor.Faces.Count - TotalLandCellCount;

        List<Vector3> ContinentPointCenters = GenerateTectonicPlateSeedPoints(InWorldSizes, InPadding, InNumContinents);

        float SplitFactor = 0.6f;
        // N Continents with X - % of the Land Area
        // Two Continents: 60% to 40%
        // Three Continents 60 25% 16% 
        List<ContinentInfo> ContinentInfos = new List<ContinentInfo>();
        int RemainingCells = TotalLandCellCount;
        for (int i = 0; i < InNumContinents; i++)
        {
            ContinentInfo CurrentContintentInfo = new ContinentInfo();
            CurrentContintentInfo.NumCells = 1;
            RemainingCells--;
            ContinentInfos.Add(CurrentContintentInfo);
        }

        for (int i = 0; i < InNumContinents; i++)
        {
            if (RemainingCells > 0)
            {
                ContinentInfo CurrentContinentInfo = ContinentInfos[i];
                CurrentContinentInfo.NumCells = Mathf.RoundToInt(RemainingCells * SplitFactor) + ContinentInfos[i].NumCells;                
                ContinentInfos[i] = CurrentContinentInfo;
                RemainingCells = RemainingCells - CurrentContinentInfo.NumCells;
            }
        }

        ContinentInfo FirstContinentInfo = ContinentInfos[0];
        FirstContinentInfo.NumCells += RemainingCells; // assign remaining cells to largest continent
        ContinentInfos[0] = FirstContinentInfo;

        List<int> ContinentCentersFaceIndices = PolyMapGen.GetCellIDsFromCoordinates(InVor, ContinentPointCenters);

        return PolyMapGen.ContinentsFloodFill2D(InVor, ContinentCentersFaceIndices, ContinentInfos);
    }

    private void UpdateMapDisplay(RenderTexture InMapRtx, Vector2Int InWorldSizes)
    {
        if (mapDisplay != null && InMapRtx != null && mapDisplay.MapDisplayImgTarget != null)
        {
            InMapRtx.filterMode = FilterMode.Point;

            mapDisplay.MapDisplayImgTarget.texture = InMapRtx;
            mapDisplay.UpdateMapDisplayRatio(InWorldSizes.x, InWorldSizes.y);
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
            //GenerateWorld();
            if (_silhouetteMap != null)
            {
                UpdateMapDisplay(_silhouetteMap, new Vector2Int(worldSettings._worldWidth, worldSettings._worldHeight));
            }
        }
    }
}
