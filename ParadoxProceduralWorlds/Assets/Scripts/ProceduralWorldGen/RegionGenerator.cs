using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using Jobberwocky.GeometryAlgorithms.Source.API;
using Jobberwocky.GeometryAlgorithms.Source.Core;
using Jobberwocky.GeometryAlgorithms.Source.Parameters;
using TVertex = TriangleNet.Geometry.Vertex;
using DataStructures.ViliWonka.KDTree;
using RTree;
//using System.Linq;

/* 
    Region Generator Description
    ----------------------------
    Regions were initialized in the ShapeGenerator, but here is where they 
    are further substantiated with the shape information and height map 
    information.
   
 
 */
public class RegionGenerator
{
    public static string AppPath = Application.dataPath + "/Temp/";

    private static string shaderPath = "Shaders/TextureUtilShader";
    private static string noShaderMsg = "Could not find the compute shader. Did you move/rename any of the files?";

    private struct RegionBound
    {
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;
    }

    public class RegionInfo
    {
        public Mesh DelaunayMesh;
        public Mesh BoundaryMesh;
        public Mesh VoronoiMesh;
    }

    public class RegionDebugInfo
    {
        public TriangleNet.Voronoi.StandardVoronoi regionVoronoiGraph = null;
        public TriangleNet.Mesh mesh = null;
        public Mesh UnityMesh = null;
    }

    public RegionDebugInfo debugRegionInfo = null;

    public class RegionPoint : RTree.ISpatialData, System.IComparable<RegionPoint>, System.IEquatable<RegionPoint>
    {
        private Envelope envelope;
        public Envelope Envelope => envelope;

        public RegionPoint(Vector3 inOrigin, Vector3 inExtents)
        {
            envelope = new Envelope(minX: inOrigin.x, maxX: inExtents.x, minY: inOrigin.y, maxY: inExtents.y);
        }

        public RegionPoint(Vector3 inPoint)
        {
            envelope = new Envelope(inPoint.x, inPoint.y);
        }

        public int CompareTo(RegionPoint other)
        {
            if (this.Envelope.MinX != other.Envelope.MinX)
                return this.Envelope.MinX.CompareTo(other.Envelope.MinX);
            if (this.Envelope.MinY != other.Envelope.MinY)
                return this.Envelope.MinY.CompareTo(other.Envelope.MinY);
            if (this.Envelope.MaxX != other.Envelope.MaxX)
                return this.Envelope.MaxX.CompareTo(other.Envelope.MaxX);
            if (this.Envelope.MaxY != other.Envelope.MaxY)
                return this.Envelope.MaxY.CompareTo(other.Envelope.MaxY);
            return 0;
        }

        public bool Equals(RegionPoint other) => this.envelope == other.envelope;

        public Vector3 point;
    }

    public bool ShouldBeInSubRegion(Vector3 inSite, int inX, int inY)
    {
        return false;
    }

    public void GetSubRegionsFromRegion
    (
        Texture inDownscaledRegionMap, 
        Texture inSourceHeightMap,
        Region inRegion, 
        List<Vector3> inSites, 
        int inMapWidth, 
        int inMapHeight
    )
    {
        // Need some "ShouldBeThisColour" method based on euclidian distance or similar.

    }

    public RenderTexture GenerateRegionSilhouetteTexture(Region inRegion, int inMapWidth, int inMapHeight, Color inSilhouetteColour)
    {
        // Generates a masking texture of a region's/landmass's silhouette from the Texture Generator. 
        return TextureGenerator.GetRegionSilhouette(inMapWidth, inMapHeight, inSilhouetteColour, ref inRegion);
    }

    public List<Vector3> GenerateSitesForRegion(Region inRegion, int inTargetSubRegions, Vector2 inScaling)
    {
        List<Coord> CoordsToPick = new List<Coord>(inRegion.Coords);

        List<Vector3> sitesToReturn = new List<Vector3>();

        if (inTargetSubRegions >= CoordsToPick.Count)
        {
            Debug.Log("Problem: There's less available coords to pick than sites.");
        }

        // Poisson Disk Sampling might be better
        for (int i = 0; i < inTargetSubRegions; ++i)
        {
            Coord pickedCoord = CoordsToPick[Random.Range(0, CoordsToPick.Count)];
            sitesToReturn.Add(
                new Vector3(Mathf.RoundToInt(pickedCoord.x / inScaling.x), Mathf.RoundToInt(pickedCoord.y / inScaling.y), 0)
            );

            CoordsToPick.Remove(pickedCoord);
        }

        return sitesToReturn;
    }

    private Vector2 GetRandomPointInCircle(Vector3 Center, float Radius)
    {
        // need k-d tree for this

        float r = Radius * Mathf.Sqrt(Random.value);
        float theta = Random.value * 2 * Mathf.PI;

        return new Vector2(Center.x + (r * Mathf.Cos(theta)), Center.y + (r * Mathf.Sin(theta)));
    }

    public List<Vector3> GetScaledRegionEdges(Region inRegion, Vector2 inScaling, Texture2D inScaledRegion)
    {
        Unity.Collections.NativeArray<Color32> pixels = inScaledRegion.GetRawTextureData<Color32>();

        Vector2 startPoint = new Vector2(0, inScaledRegion.height / 2);

        // iterate from middle left to middle right until we find our "island"
        bool bStartPointFound = false;

        for (int j = inScaledRegion.height / 2; j < inScaledRegion.height; ++j)
        {
            for (int i = 0; i < inScaledRegion.width; ++i)
            {
                if (pixels[j * inScaledRegion.width + i].a != 0)
                {
                    startPoint.x = i;
                    startPoint.y = j;
                    bStartPointFound = true;
                    break;
                }
            }

            if (bStartPointFound) break;
        }

        if (!bStartPointFound)
        {
            for (int j = (inScaledRegion.height / 2) - 1; j >= 0; --j)
            {
                for (int i = 0; i < inScaledRegion.width; ++i)
                {
                    if (pixels[j * inScaledRegion.width + i].a != 0)
                    {
                        startPoint.x = i;
                        startPoint.y = j;
                        bStartPointFound = true;
                    }
                }

                if (bStartPointFound) break;
            }
        }        

        // make sure we found a point
        if (pixels[(int)startPoint.y * inScaledRegion.width + (int)startPoint.x].a == 0)
        {
            Debug.Log("Start Point not found");
            return new List<Vector3>();
        }

        Queue<Vector3> pointQ = new Queue<Vector3>();
        HashSet<Vector3> visitedPoints = new HashSet<Vector3>();
        HashSet<Vector3> edgePoints = new HashSet<Vector3>();
        pointQ.Enqueue(startPoint);

        // infinite loop here somewhere
        while (pointQ.Count > 0)
        {
            Vector3 current = pointQ.Dequeue();
            visitedPoints.Add(current);

            if (pixels[(int)(current.y - 1) * inScaledRegion.width + (int)current.x].a == 0 ||
                pixels[(int)(current.y + 1) * inScaledRegion.width + (int)current.x].a == 0 ||
                pixels[(int)current.y * inScaledRegion.width + (int)current.x + 1].a == 0 ||
                pixels[(int)current.y * inScaledRegion.width + (int)current.x - 1].a == 0)
            {
                if (!edgePoints.Contains(current))
                {
                    edgePoints.Add(current);
                }
            }
            
            if (pixels[(int)(current.y - 1) * inScaledRegion.width + (int)current.x].a != 0 &&
                !visitedPoints.Contains(new Vector3(current.x, current.y - 1)))
            {
                pointQ.Enqueue(new Vector3(current.x, current.y - 1));
                visitedPoints.Add(new Vector3(current.x, current.y - 1));
            }

            if (pixels[(int)(current.y + 1) * inScaledRegion.width + (int)current.x].a != 0 &&
                !visitedPoints.Contains(new Vector3(current.x, current.y + 1)))
            {
                pointQ.Enqueue(new Vector3(current.x, current.y + 1));
                visitedPoints.Add(new Vector3(current.x, current.y + 1));
            }

            if (pixels[(int)current.y * inScaledRegion.width + (int)current.x + 1].a != 0 &&
                !visitedPoints.Contains(new Vector3(current.x + 1, current.y)))
            {
                pointQ.Enqueue(new Vector3(current.x + 1, current.y));
                visitedPoints.Add(new Vector3(current.x + 1, current.y));
            }

            if (pixels[(int)current.y * inScaledRegion.width + (int)current.x - 1].a != 0 &&
                !visitedPoints.Contains(new Vector3(current.x - 1, current.y)))
            {
                pointQ.Enqueue(new Vector3(current.x - 1, current.y));
                visitedPoints.Add(new Vector3(current.x - 1, current.y));
            }            
        }

        return new List<Vector3>(edgePoints);
    }

    public List<Coord> GeneratePoissonSitesForRegion
    (
        Region inRegion, 
        int inTargetSubRegions,         
        Vector2 inScaling,
        Texture2D inScaledRegion,
        int radius, 
        int numSamplesBeforeRejection = 30
    )
    {
        /* setup data stuff we need */
        // init our prng
        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        // the pixels belonging to our continent, unscaled
        List<Coord> coordsToPick = new List<Coord>(inRegion.Coords);

        // our pixels array
        Unity.Collections.NativeArray<Color32> pixels = inScaledRegion.GetRawTextureData<Color32>();



        int scaledMapWidth = inScaledRegion.width;
        int scaledMapHeight = inScaledRegion.height;

        // our KD tree doesn't like being modified
        int[] prunedCoords = new int[scaledMapWidth * scaledMapHeight];

        // sites to return at the end
        List<Coord> sites = new List<Coord>();

        // our initial spawned sites, duplicatesas a result of downscaling should
        // avoid being added.
        HashSet<Coord> spawnedSites = new HashSet<Coord>();

        // we're going to spawn double the number of requested sites and then
        // filter our way to the requested number; if we end up with more sites
        // than requested we'll delete sites at random until we've reached it.
        int sitesToSpawn = Mathf.Min(coordsToPick.Count, inTargetSubRegions * 2);
        // We're going to select a number of points at random and then prune them
        int pointsRemaining = inTargetSubRegions;
        for (int i = 0; i < sitesToSpawn; ++i)
        {
            int coordSpawnIndex = Random.Range(0, coordsToPick.Count);
            Coord coordToSpawn = coordsToPick[coordSpawnIndex];
            spawnedSites.Add(coordToSpawn);
            coordsToPick.RemoveAt(coordSpawnIndex);
        }

        List<Vector3> vecSpawnPoints = new List<Vector3>();
        foreach (var coordPoint in spawnedSites)
        {
            vecSpawnPoints.Add(new Vector3(coordPoint.x, coordPoint.y));
        }

        KDTree kdTree = new KDTree(vecSpawnPoints.ToArray(), 32);
        DataStructures.ViliWonka.KDTree.KDQuery query = new DataStructures.ViliWonka.KDTree.KDQuery();

        List<int> results = new List<int>();
        for (int i = 0; i < vecSpawnPoints.Count; ++i)
        {            
            Vector3 currentPoint = vecSpawnPoints[i];
            // check if this is a pruned point, if so we skip
            if (prunedCoords[(int)currentPoint.y * scaledMapWidth + (int)currentPoint.x] != 1)
            {
                // otherwise, remove all points within radius distance away
                query.Radius(kdTree, currentPoint, radius, results);
                for (int j = 0; j < results.Count; ++j)
                {
                    Vector3 coordToPrune = vecSpawnPoints[results[j]];
                    // mark the pixel as pruned
                    prunedCoords[(int)coordToPrune.y * scaledMapWidth + (int)coordToPrune.x] = 1;
                }
            }

        }


        return sites;
    }

    public List<Vector3> GenerateSemiPoissonSitesForRegion
    (
        Region inRegion, 
        int inTargetSubRegions, 
        Vector2 inScaling,
        Texture2D inScaledRegion
    )
    {
        int radius = 5;

        List<Coord> coordsToPick = new List<Coord>(inRegion.Coords);

        //List<Vector3> sitesToReturn = new List<Vector3>();

        if (inTargetSubRegions >= coordsToPick.Count)
        {
            Debug.Log("Problem: There's less available coords to pick than sites.");
        }        

        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        Unity.Collections.NativeArray<Color32> pixels = inScaledRegion.GetRawTextureData<Color32>();

        int pointsRemaining = inTargetSubRegions;

        var tree = new RTree.RTree<RegionPoint>();

        while (pointsRemaining > 0 && coordsToPick.Count > 0)
        {
            Coord sampleCoord = coordsToPick[Random.Range(0, coordsToPick.Count)];
            Vector3 samplePoint = new Vector3(sampleCoord.x / inScaling.x, sampleCoord.y / inScaling.x);

            // check if point is near other points
            IEnumerable<RegionPoint> result = tree.Search(new Envelope
            (
                minX: samplePoint.x - radius,
                minY: samplePoint.y - radius,
                maxX: samplePoint.x + radius,
                maxY: samplePoint.y + radius
            ));

            // if not, generate 4 points randomly in circle around it & enqeue
            int resCount = ((IList<RegionPoint>)result).Count;
            if (resCount <= 0)
            {
                // insert the current point, and decrement
                tree.Insert(new RegionPoint(samplePoint));
                inTargetSubRegions--;
            }

            coordsToPick.Remove(sampleCoord);
        }

        List<Vector3> sitesToReturn = new List<Vector3>();
        foreach (var regionPoint in tree.Search())
        {
            sitesToReturn.Add(new Vector3((float)regionPoint.Envelope.MinX, (float)regionPoint.Envelope.MinY));
        }
             
        return sitesToReturn;
    }

    public RegionInfo GenerateConstrainedDelaunayMeshForRegion
    (
        Region inRegion, 
        int inTargetSubRegions
    )
    {
        /*
         * Attempt to generate a non-convex delaunay triangulation; supposedly for the Jabberwock library
         * a concave delaunay mesh isn't transferable to making a voronoi mesh.
         * 
         * So this attempt is to see how the constrained delaunay triangulation looks.
         */
        List<Vector3> vecSites = new List<Vector3>();

        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        List<Coord> CoordsToPick = new List<Coord>(inRegion.Coords);

        if (inTargetSubRegions >= CoordsToPick.Count)
        {
            Debug.Log("Problem: There's less available coords to pick than sites.");
        }

        // Poisson Disk Sampling might be better
        for (int i = 0; i < inTargetSubRegions; ++i)
        {
            Coord pickedCoord = CoordsToPick[Random.Range(0, CoordsToPick.Count)];
            vecSites.Add(new Vector3(pickedCoord.x, pickedCoord.y, 0));

            CoordsToPick.Remove(pickedCoord);
        }

        var hullAPI = new HullAPI();
        var hull = hullAPI.Hull2D(new Hull2DParameters() 
        { 
            Points = vecSites.ToArray()
            //,Concavity = 1
        });

        var parameters = new Triangulation2DParameters();
        parameters.Points = vecSites.ToArray();
        parameters.Boundary = hull.vertices;
        parameters.Delaunay = true;

        var triangulationAPI = new TriangulationAPI();
        var mesh = triangulationAPI.Triangulate2D(parameters);

        //debugRegionInfo = new RegionDebugInfo();
        //debugRegionInfo.UnityMesh = mesh;

        Debug.Log("Picked " + vecSites.Count + " points for sites!");

        RegionInfo outRegionInfo = new RegionInfo();

        outRegionInfo.DelaunayMesh = mesh;
        outRegionInfo.BoundaryMesh = hull;

        Debug.Log("Delaunay Mesh has " + mesh.vertices.Length + " vertices!");
        Debug.Log("Hull has " + hull.vertices.Length + " vertices!");

        return outRegionInfo;
    }

    

    public RenderTexture GenerateTestRegion(Region inRegion, int inTargetSubRegions, int inMapWidth, int inMapHeight)
    {
        TriangleNet.Meshing.ConstraintOptions options =
            new TriangleNet.Meshing.ConstraintOptions() { ConformingDelaunay = true };

        List<TVertex> sites = new List<TVertex>();
        Polygon delaneyShape = new Polygon();

        UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);

        List<Coord> CoordsToPick = new List<Coord>(inRegion.Coords);

        if (inTargetSubRegions >= CoordsToPick.Count)
        {
            Debug.Log("Problem: There's less available coords to pick than sites.");
        }

        // Poisson Disk Sampling might be better
        for (int i = 0; i < inTargetSubRegions; ++i)
        {
            Coord pickedCoord = CoordsToPick[Random.Range(0, CoordsToPick.Count)];
            TVertex vert = new TVertex(pickedCoord.x, pickedCoord.y);
            sites.Add(vert);
            delaneyShape.Add(vert);

            CoordsToPick.Remove(pickedCoord);
        }

        //delaneyShape.Add(new Vertex(0, 0));
        //delaneyShape.Add(new Vertex(inMapWidth - 1, 0));
        //delaneyShape.Add(new Vertex(0, inMapHeight - 1));
        //delaneyShape.Add(new Vertex(inMapWidth - 1, inMapHeight - 1));

        delaneyShape.Add(new TVertex(3770 - 1, 883 - 1)); // bottom left
        delaneyShape.Add(new TVertex(6517 + 1, 883 - 1)); // bottom right
        delaneyShape.Add(new TVertex(3770 - 1, 3224 + 1)); // top left
        delaneyShape.Add(new TVertex(6517 + 1, 3224 + 1)); // top right
        delaneyShape.Bounds();

        TriangleNet.Mesh mesh = (TriangleNet.Mesh)delaneyShape.Triangulate(options);

        TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
        simpleSmoother.Smooth(mesh, 8);

        TriangleNet.Voronoi.StandardVoronoi voronoiGraph = new TriangleNet.Voronoi.StandardVoronoi(mesh);

        UnityEngine.Debug.Log("Voronoi Face Count: " + voronoiGraph.Faces.Count);
        UnityEngine.Debug.Log("(Mesh) Vertice Count: " + mesh.Vertices.Count);

        debugRegionInfo = new RegionDebugInfo();
        debugRegionInfo.regionVoronoiGraph = voronoiGraph;
        debugRegionInfo.mesh = mesh;

        return TextureGenerator.FillVoronoiRegion(inMapWidth, inMapHeight, ref inRegion, voronoiGraph.Faces.ToArray());
    }

    private void GetRegionBounds(ref Region inRegion, int inMapWidth, int inMapHeight)
    {
        ComputeShader shader = Resources.Load(shaderPath) as ComputeShader;
        if (shader == null)
        {
            Debug.LogError(noShaderMsg);
            return;
        }

        int kernel = shader.FindKernel("FindRegionBoundaries");

        shader.SetInt("coordsSize", inRegion.Coords.Count);

        // Passing in our array of coordinates comprising our region, a Coord is 2 ints (4 bytes each)
        ComputeBuffer coordBuffer = new ComputeBuffer(inRegion.Coords.Count, 8);
        coordBuffer.SetData(inRegion.Coords);
        shader.SetBuffer(kernel, "coords", coordBuffer);

        RegionBound[] bounds = new RegionBound[1];
        bounds[0].minX = int.MaxValue;
        bounds[0].minY = int.MaxValue;

        // Each RegionBound struct is 4 ints and 4 bytes each, there is 1 RegionBound in array
        ComputeBuffer regionBoundsBuffer = new ComputeBuffer(bounds.Length, 4 * 4);
        regionBoundsBuffer.SetData(bounds);
        shader.SetBuffer(kernel, "regionbounds", regionBoundsBuffer);

        shader.Dispatch(kernel, Mathf.CeilToInt(inMapWidth / 16f), Mathf.CeilToInt(inMapHeight / 16f), 1);

        regionBoundsBuffer.GetData(bounds);

        inRegion.MinX = bounds[0].minX;
        inRegion.MaxX = bounds[0].maxX;
        inRegion.MinY = bounds[0].minY;
        inRegion.MaxY = bounds[0].maxY;

        coordBuffer.Release();
        regionBoundsBuffer.Release();
    }
}
