using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using System.Linq;
using TriangleNet.Topology;

public class WorldGenerator : MonoBehaviour
{
    // singleton
    private static WorldGenerator _instance;

    public static WorldGenerator Instance {
        get {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<WorldGenerator>();
            }

            return _instance;
        }
    }

    private ShapeSettings worldShapeSettings = null;
    private ShapeGenerator worldShapeGenerator = null;

    private HeightMapGenerator heightMapGen = null;
    //public VoronoiTest voronoiManager = null;

    public MeshRenderer mapRenderer = null;

    public VoronoiSettings voronoiSettings = null;

    float[,] heightMap;

    List<Vertex> waterSites = new List<Vertex>();
    List<Vertex> landSites = new List<Vertex>();

    // debugging
    List<VoronoiCell> nodes = new List<VoronoiCell>();
    HashSet<TileBlob> blobs = new HashSet<TileBlob>();

    TriangleNet.Mesh mesh;

    public bool useAdjustedVoronoi = true;
    public bool drawCubes = true;
    public bool drawEvenEdges = true;

    TriangleNet.Voronoi.StandardVoronoi voronoi = null;
    TriangleNet.Voronoi.StandardVoronoi voronoi2 = null;

    BarycentricGraphGenerator baryGen = null;

    private void Start()
    {

    }

    public void Init(ShapeGenerator _shapeGen, ShapeSettings _shapeSettings)
    {
        worldShapeGenerator = _shapeGen;
        heightMapGen = new HeightMapGenerator(_shapeGen);
        worldShapeSettings = _shapeSettings;
    }

    // when adding new news layers to the UI, be sure to add a new layer to
    // the existing noise layers and update the resulting texture(s).
    /*
    public void RegisterNewNoiseLayer(NoiseSettings _nuNoiseSettings)
    {
        worldShapeSettings.AddNoiseLayer(_nuNoiseSettings);
        worldShapeGenerator.UpdateSettings(worldShapeSettings);
    }
    */

    /*
    public void UpdateNoiseLayerSettings(int _layerID, NoiseSettings _noiseSettings)
    {
        worldShapeGenerator.UpdateSettings(_layerID, _noiseSettings);
    }
    */

    /*
    public void UpdateWorldNoiseLayerBlendMode(int _layerID, int _blendMode)
    {
        worldShapeGenerator.UpdateBlendMode(_layerID, _blendMode);
    }
    */

    /*
    public void ToggleNoiseLayer(int _layerID, bool _toggle)
    {
        if (worldShapeSettings != null)
        {
            if (_layerID < worldShapeSettings.noiseLayers.Length)
            {
                worldShapeSettings.noiseLayers[_layerID].enabled = _toggle;
            }                
        }
    }
    */

    public Texture2D GetHeightMap()
    {
        return heightMapGen.GenerateHeightMap();
    }

    // deprecating in favour of WorldManager
    /*
    public void GenerateCombinedWorldMap()
    {
        if (mapGen == null) return;

        mapGen.Initialize();
        mapGen.Generate();
        heightMap = mapGen.finalHeightMap;

        Texture2D heightMapTex =
            TextureGenerator.TextureFromHeightMap(heightMap);
        TextureGenerator.SaveTextureAsPNG(heightMapTex, "heightMapTex");

        if (heightMap == null) return;

        width = mapGen.Width;
        height = mapGen.Height;

        VoronoiMapGenerator landMapGen = new VoronoiMapGenerator(width, height);
        landMapGen.iterations = 5; //voronoiManager.iterations;
        landMapGen.relax = true; //voronoiManager.relax;
        landMapGen.seed = 0; // voronoiManager.seed;
        landMapGen.useRandomSeed = false; // voronoiManager.useRandomSeed;
        landMapGen.fillPercent = 40;
        landMapGen.Init_Triangle();
        landMapGen.Init();
        landMapGen.minHeight = 0;
        landMapGen.minHue = 0;
        landMapGen.maxHue = 150;
        landMapGen.maxSat = 100;
        landMapGen.minSat = 60;
        landMapGen.maxBright = 100;
        landMapGen.minBright = 60;

        VoronoiMapGenerator seaMapGen = new VoronoiMapGenerator(width, height);
        seaMapGen.iterations = 5;
        seaMapGen.relax = true; // voronoiManager.relax;
        seaMapGen.seed = 0; // voronoiManager.seed;
        seaMapGen.useRandomSeed = false; // voronoiManager.useRandomSeed;
        seaMapGen.fillPercent = 100;
        seaMapGen.Init_Triangle();
        seaMapGen.Init();
        seaMapGen.minHeight = 0.25f;
        seaMapGen.minHue = 190;
        seaMapGen.maxHue = 240;
        seaMapGen.maxSat = 100;
        seaMapGen.minSat = 60;
        seaMapGen.maxBright = 100;
        seaMapGen.minBright = 60;

        // generate the sites
        landSites = landMapGen.GenerateOffsetSites(1000, 0);
        waterSites = seaMapGen.GenerateOffsetSites(100, 0);

        // generate the delaney triangulation meshes & voronoi maps
        landMapGen.GenerateDelaneyMesh(landSites);
        landMapGen.GenerateVoronoiDiagram();
        voronoi = landMapGen.voronoi;
        mesh = landMapGen.mesh;
        landMapGen.Sites = landSites;
        seaMapGen.GenerateDelaneyMesh(waterSites);
        seaMapGen.GenerateVoronoiDiagram();
        seaMapGen.Sites = waterSites;

        baryGen = new BarycentricGraphGenerator(mesh);
        baryGen.GenerateBarycentricDualMesh(width, height);
        voronoi2 = baryGen.voronoi;
        landMapGen.voronoi = voronoi2;
        voronoi = voronoi2;

        // Colouring the resulting voronoi
        VoronoiColourGenerator vMapColourGen = new VoronoiColourGenerator(width, height);
        // set the used height map
        vMapColourGen.heightMap = heightMap;

        vMapColourGen.SetVoronoiMapGenerators(landMapGen);
        vMapColourGen.SetVoronoiMapGenerators(seaMapGen);

        vMapColourGen.InitializeVoronoiCombinedCellColours();

        vMapColourGen.GenerateCellColours();

        VoronoiBlobGenerator vBlobGraphGen = 
            new VoronoiBlobGenerator(landMapGen.mesh, vMapColourGen);
        nodes = vBlobGraphGen.voronoiGraphGenie.nodes;
        blobs = vBlobGraphGen.Blobs;

        vMapColourGen.GenerateCellColours(blobs);
        vMapColourGen.GenerateCombinedLayeredMap();

        //UpdateMapDisplay(vMapColourGen.SaveTexture());
    }
    */

    /*
    // moving to WorldManager
    public void UpdateMapDisplay(Texture2D tex)
    {
        if (mapRenderer == null) return; 

        mapRenderer.sharedMaterial.mainTexture = tex;
        mapRenderer.sharedMaterial.SetTexture("_BaseMap", tex);
        //mapRenderer.sharedMaterial.SetTexture("_MainTex", tex);
        mapRenderer.transform.localScale = new Vector3((float)width / 10, 1, (float)height / 10);
    }
    */

}
