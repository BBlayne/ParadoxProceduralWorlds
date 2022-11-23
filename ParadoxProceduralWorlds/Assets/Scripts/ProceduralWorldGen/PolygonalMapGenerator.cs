using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TVertex = TriangleNet.Geometry.Vertex;
using System.Linq;
using DataStructures.ViliWonka.KDTree;
using VQuery = DataStructures.ViliWonka.KDTree.KDQuery;
using TriangleNet.Voronoi;
using TMesh = TriangleNet.Mesh;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TFace = TriangleNet.Topology.DCEL.Face;
using TQualityOptions = TriangleNet.Meshing.QualityOptions;

// Design Notes
/*
 * Following the process outlined in Redbloggames:
 * http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/
 * 
 * (1) Need Voronoi/Delaney Triangulation
 * 
 */
namespace ProceduralWorlds
{
    public enum ESiteDistribution
    {
        RANDOM
    }

    public enum ECellColour
    {
        RANDOM,
        NGRADIANT, // Per N-Types, different colour gradiant
        BLACK_AND_WHITE
    }

    public struct MapCell
    {
        public int CellID;
        public ERegionType RegionType;
        public float Elevation;
    }

    public class PolygonalMapGenerator
    {
        public MapCell[] MapCells;

        ESiteDistribution SiteDistributionMode = ESiteDistribution.RANDOM;
        ECellColour CellColour = ECellColour.RANDOM;

        public Vector3[] InitialSites = null;

        public Vector2Int MapDimensions = new Vector2Int();

        public int NumberOfTargetCells = 0;

        public bool bSaveDebugMaps = true;

        public RenderTexture MainRTex = null;

        public BoundedVoronoi Vor = null;
        public TMesh Triang = null;

        private Mesh CombinedVoronoiMesh = null;

        public PolygonalMapGenerator(int InNumCells)
        {
            Random.InitState(Time.time.ToString().GetHashCode());
            NumberOfTargetCells = InNumCells;
            SiteDistributionMode = ESiteDistribution.RANDOM;
            CellColour = ECellColour.RANDOM;
        }

        public PolygonalMapGenerator(int InSeed, int InNumCells)
        {
            Random.InitState(InSeed);
            NumberOfTargetCells = InNumCells;
            SiteDistributionMode = ESiteDistribution.RANDOM;
            CellColour = ECellColour.RANDOM;
        }

        public PolygonalMapGenerator(string InSeedString, int InNumCells)
        {
            Random.InitState(InSeedString.GetHashCode());
            NumberOfTargetCells = InNumCells;
            SiteDistributionMode = ESiteDistribution.RANDOM;
            CellColour = ECellColour.RANDOM;
        }

        public void SetDistributionMode(ESiteDistribution InDistribution)
        {
            SiteDistributionMode = InDistribution;
        }

        public void SetColourMode(ECellColour InColourMode)
        {
            CellColour = InColourMode;
        }

        public void SetSites(Vector3[] InSites)
        {
            InitialSites = InSites;
        }

        public void GenerateSiteDistribution(int InPadding = 25)
        {
            switch (SiteDistributionMode)
            {
            case ESiteDistribution.RANDOM:
            default:
                InitialSites = MapUtils.GenerateRandomPoints2D(NumberOfTargetCells, MapDimensions, InPadding).ToArray();
                break;
            }
        }

        public Mesh GeneratePolygonalMapMesh()
        {
            GenerateSiteDistribution(25);
            TMesh Triangulation = GenerateDelaunayTriangulation(InitialSites, 2);
            Triang = Triangulation;

            BoundedVoronoi mBoundedVoronoi = GetVoronoiTesselationFromTriangulation(Triang);
            MoveVoronoiVerticesToDelauneyCentroids(mBoundedVoronoi, 2.5f);
            Vor = mBoundedVoronoi;

            List<Mesh> VorCellMeshes = TriangulateVoronoiCells(Vor);            
            RandomLandDistribution(Vor.Faces.Count, 0.25f);
            Texture2D VoronoiTexture = TextureGenerator.GenerateBiColourElevationTextureMap(VorCellMeshes.Count, MapCells);
            if (bSaveDebugMaps)
            {
                TextureGenerator.SaveTextureAsPNG(VoronoiTexture, "VoronoiTexture");
            }
            GenerateUVsForVoronoiUnityMesh(VorCellMeshes);
            CombineInstance[] CombineInstances = new CombineInstance[VorCellMeshes.Count];
            for (int i = 0; i < VorCellMeshes.Count; i++)
            {
                CombineInstances[i].mesh = VorCellMeshes[i];
                CombineInstances[i].transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            }

            CombinedVoronoiMesh = new Mesh();
            CombinedVoronoiMesh.CombineMeshes(CombineInstances);

            MainRTex = RenderPolygonalMap(CombinedVoronoiMesh, VoronoiTexture, TextureGenerator.GetUnlitTextureMaterial());

            return CombinedVoronoiMesh;
        }

        public Mesh GenerateUnityMesh(TMesh triangleNetMesh, TQualityOptions options = null)
        {
            if (options != null)
            {
                triangleNetMesh.Refine(options);
            }

            Mesh mesh = new Mesh();
            var triangleNetVerts = triangleNetMesh.Vertices.ToList();

            var triangles = triangleNetMesh.Triangles;

            Vector3[] verts = new Vector3[triangleNetVerts.Count];
            int[] trisIndex = new int[triangles.Count * 3];

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = new Vector3((float)triangleNetVerts[i].x, (float)triangleNetVerts[i].y, 0);
            }

            int k = 0;

            foreach (var triangle in triangles)
            {
                for (int i = 2; i >= 0; i--)
                {
                    trisIndex[k] = triangleNetVerts.IndexOf(triangle.GetVertex(i));
                    k++;
                }
            }

            mesh.vertices = verts;
            mesh.triangles = trisIndex;

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private List<TVertex> ToTriangleNetVertices(List<Vector2> points)
        {
            List<TVertex> vertices = new List<TVertex>();
            foreach (var vec in points)
            {
                vertices.Add(new TVertex(vec.x, vec.y));
            }

            return vertices;
        }

        // Using Triangle.Net
        public Mesh GenerateDelaunayMesh(List<Vector3> InPoints, int InSmoothing = 0)
        {
            TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions() 
            { 
                ConformingDelaunay = true ,
                SegmentSplitting = 1
            };

            List<TVertex> sites = new List<TVertex>();
            Polygon delaneyShape = new Polygon();          

            foreach (Vector3 PointV3 in InPoints)
            {
                TVertex Vertex = new TVertex(PointV3.x, PointV3.y);
                sites.Add(Vertex);
                delaneyShape.Add(Vertex);
            }

            delaneyShape.Bounds();

            TMesh TriangleMesh = (TMesh)delaneyShape.Triangulate(options);

            TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
            if (TriangleMesh != null)
            {
                simpleSmoother.Smooth(TriangleMesh, InSmoothing);
            }            

            return GenerateUnityMesh(TriangleMesh);
        }

        public Mesh GenerateUnityMeshFromTriangleNetMesh(TMesh InMesh)
        {
            return GenerateUnityMesh(InMesh);
        }

        public TMesh GenerateDelaunayTriangulation(Vector3[] InPoints, int InSmoothing = 0)
        {
            TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions()
            {
                ConformingDelaunay = true
            };

            List<TVertex> sites = new List<TVertex>();
            Polygon delaneyShape = new Polygon();

            foreach (Vector3 PointV3 in InPoints)
            {
                TVertex Vertex = new TVertex(PointV3.x, PointV3.y);
                sites.Add(Vertex);
                delaneyShape.Add(Vertex);
            }

            delaneyShape.Add(new TVertex(0, 0));
            delaneyShape.Add(new TVertex(1, MapDimensions.y)); 
            delaneyShape.Add(new TVertex(MapDimensions.x, MapDimensions.y)); 
            delaneyShape.Add(new TVertex(MapDimensions.x, 1)); 

            delaneyShape.Bounds();

            TMesh TriangleMesh = (TriangleNet.Mesh)delaneyShape.Triangulate(options);

            TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
            if (TriangleMesh != null)
            {
                simpleSmoother.Smooth(TriangleMesh, InSmoothing);
            }

            return TriangleMesh;
        }

        public BoundedVoronoi GetVoronoiTesselationFromTriangulation(TMesh InTriangleMesh)
        {
            return new BoundedVoronoi(InTriangleMesh);
        }

        public Mesh GenerateVoronoiUnityMesh(BoundedVoronoi InVor)
        {
            Mesh OutVorMesh = new Mesh();

            List<IEdge> Edges = InVor.Edges.ToList();
            List<Vector3> MeshVertices = new List<Vector3>();
            List<int> MeshIndices = new List<int>();
            int IndexCounter = 0;
            for (int i = 0; i < Edges.Count; i++)
            {
                Vector3 PointP = new Vector3
                (
                    (float)InVor.Vertices[Edges[i].P0].x,
                    (float)InVor.Vertices[Edges[i].P0].y,
                    0
                );

                Vector3 PointQ = new Vector3
                (
                    (float)InVor.Vertices[Edges[i].P1].x,
                    (float)InVor.Vertices[Edges[i].P1].y,
                    0
                );

                MeshVertices.Add(PointP);
                MeshVertices.Add(PointQ);
                MeshIndices.Add(IndexCounter++);
                MeshIndices.Add(IndexCounter++);
            }
            OutVorMesh.vertices = MeshVertices.ToArray();
            OutVorMesh.SetIndices(MeshIndices, MeshTopology.Lines, 0);

            return OutVorMesh;
        }

        public Mesh GenerateVoronoiUnityMesh(TMesh InTriangleMesh)
        {
            BoundedVoronoi OutVorGraph = new BoundedVoronoi(InTriangleMesh);
            Mesh OutVorMesh = new Mesh();

            List<IEdge> Edges = OutVorGraph.Edges.ToList();
            List<Vector3> MeshVertices = new List<Vector3>();
            List<int> MeshIndices = new List<int>();
            int IndexCounter = 0;
            for (int i = 0; i < Edges.Count; i++)
            {
                Vector3 PointP = new Vector3
                (
                    (float)OutVorGraph.Vertices[Edges[i].P0].x,
                    (float)OutVorGraph.Vertices[Edges[i].P0].y, 
                    0
                );

                Vector3 PointQ = new Vector3
                (
                    (float)OutVorGraph.Vertices[Edges[i].P1].x,
                    (float)OutVorGraph.Vertices[Edges[i].P1].y,
                    0
                );

                MeshVertices.Add(PointP);
                MeshVertices.Add(PointQ);
                MeshIndices.Add(IndexCounter++);
                MeshIndices.Add(IndexCounter++);
            }
            OutVorMesh.vertices = MeshVertices.ToArray();
            OutVorMesh.SetIndices(MeshIndices, MeshTopology.Lines, 0);

            return OutVorMesh;
        }

        public void RandomLandDistribution(int InNumCells, float LandThreshold = 0.40f)
        {
            MapCells = new MapCell[InNumCells];
            for (int i = 0; i < InNumCells; i++)
            {
                float Elevation = (float)Random.Range(0, 100) / 100;
                if (MapCells != null && MapCells.Length > 0 && i < MapCells.Length)
                {
                    MapCells[i].CellID = i;
                    if (Elevation >= LandThreshold)
                    {
                        MapCells[i].RegionType = ERegionType.Water;
                    }
                    else
                    {
                        MapCells[i].RegionType = ERegionType.Land;
                    }
                    MapCells[i].Elevation = Elevation;
                } 
            }
        }

        public List<Mesh> TriangulateVoronoiCells(BoundedVoronoi InVorGraph)
        {
            TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions()
            {
                ConformingDelaunay = true
            };

            List<Mesh> VoronoiTriangulations = new List<Mesh>();

            for (int VorIndex = 0; VorIndex < InVorGraph.Faces.Count; VorIndex++)
            {
                Polygon DelaneyShape = new Polygon();
                var CurrentEdge = InVorGraph.Faces[VorIndex].Edge;
                if (CurrentEdge != null)
                {
                    var FirstEdge = CurrentEdge;
                    while (CurrentEdge.Next != null && CurrentEdge.Next != FirstEdge)
                    {
                        TVertex VertexToAdd = new TVertex(CurrentEdge.Origin.x, CurrentEdge.Origin.y);
                        DelaneyShape.Add(VertexToAdd);

                        CurrentEdge = CurrentEdge.Next;                       
                    }

                    if (CurrentEdge.Next != null)
                    {
                        TVertex VertexToAdd = new TVertex(CurrentEdge.Origin.x, CurrentEdge.Origin.y);
                        DelaneyShape.Add(VertexToAdd);
                    }
                }
                DelaneyShape.Bounds();

                VoronoiTriangulations.Add(GenerateUnityMeshFromTriangleNetMesh((TMesh)DelaneyShape.Triangulate(options)));
            }

            return VoronoiTriangulations;
        }        

        public void GenerateUVsForVoronoiUnityMesh(List<Mesh> InVoronoiTriangulations)
        {
            for (int i = 0; i < InVoronoiTriangulations.Count; i++)
            {
                List<Vector2> MeshUVs = new List<Vector2>();                
                for (int j = 0; j < InVoronoiTriangulations[i].vertexCount; j++)
                {
                    MeshUVs.Add(new Vector2((float)i / InVoronoiTriangulations.Count, 0.0f));
                }
                InVoronoiTriangulations[i].uv = MeshUVs.ToArray();
            }            
        }

        public void GenerateUVsForVoronoiUnityMesh(List<Mesh> InVoronoiTriangulations, Texture2D InTex)
        {
            if (InTex == null || InTex.width < 1 || InTex.height < 1) 
            { 
                return; 
            }

            int InTexWidth = InTex.width;
            int InTexHeight = InTex.height;
            for (int i = 0; i < InVoronoiTriangulations.Count; i++)
            {
                List<Vector2> MeshUVs = new List<Vector2>();
                int ColourPick = Random.Range(0, 4); // 0 to (and including) 3
                ColourPick *= (InTexWidth / 2);

                float why = ((Mathf.Floor((float)ColourPick / InTexWidth) * (InTexWidth / 2)) + (InTexWidth / 4)) / InTexWidth;
                float ecks = (((float)ColourPick % InTexWidth) + (InTexHeight / 4)) / InTexHeight;
                for (int j = 0; j < InVoronoiTriangulations[i].vertexCount; j++)
                {
                    MeshUVs.Add(new Vector2(ecks, why));
                }
                InVoronoiTriangulations[i].uv = MeshUVs.ToArray();
            }
        }

        // TODO
        // To triangulate a voronoi mesh such that its polygons are drawable and texturable;
        // I need to loop through each voronoi cell:
        // Triangulate the cell, one at a time; adding the resulting triangulation as a shape to a List
        // where the index of trianglenet mesh corresponds to the voronoi cell.
        // Then I can assign a single colour to these vertices via UVs.
        // Either procedurally generated 2D texture with a bunch of colours
        // or a test texture with just a few colours to test. i.e white and black.
        // And then blit the non-wireframe version.

        // Adjusting Voronoi Vertices into Centroids of the Delaunay Triangles
        public void MoveVoronoiVerticesToDelauneyCentroids(BoundedVoronoi InVor, float InRange)
        {
            for (int VVertexIndex = 0; VVertexIndex < InVor.Vertices.Count; VVertexIndex++)
            {
                var CurrentVertex = InVor.Vertices[VVertexIndex];

                if (CurrentVertex == null) continue; // lmao

                if (Mathf.RoundToInt((float)CurrentVertex.X) == 0 ||
                    Mathf.RoundToInt((float)CurrentVertex.X) == 1 ||
                    Mathf.RoundToInt((float)CurrentVertex.Y) == 0 ||
                    Mathf.RoundToInt((float)CurrentVertex.Y) == 1 ||
                     Mathf.RoundToInt((float)CurrentVertex.X) == Mathf.RoundToInt(MapDimensions.x) ||
                     Mathf.RoundToInt((float)CurrentVertex.X) == Mathf.RoundToInt(MapDimensions.x) - 1 ||
                     Mathf.RoundToInt((float)CurrentVertex.Y) == Mathf.RoundToInt(MapDimensions.y) ||
                     Mathf.RoundToInt((float)CurrentVertex.Y) == Mathf.RoundToInt(MapDimensions.y) - 1)
                {
                    // is an edge on the edge, skip
                    continue;
                }

                // get all edges surrounding this vertex
                List<THalfEdge> Edges = CurrentVertex.EnumerateEdges().ToList();

                // compute the centroid by getting the location of the
                // site of each face
                Vector2 DelaunayTriangleCentroid = new Vector2(0, 0);
                foreach (var CurrentEdge in Edges)
                {
                    if (CurrentEdge == null || CurrentEdge.Face == null || CurrentEdge.Face.generator == null) 
                    {
                        Debug.Log("Here wtf?");
                        continue; 
                    }

                    try
                    {
                        DelaunayTriangleCentroid += new Vector2
                        (
                            (float)CurrentEdge.Face.generator.X,
                            (float)CurrentEdge.Face.generator.Y
                        );
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log("Exception!?: " + e.Message);
                        continue;
                    }
                }

                DelaunayTriangleCentroid /= Edges.Count;

                CurrentVertex.X = DelaunayTriangleCentroid.x + Random.Range(-InRange, InRange);
                CurrentVertex.Y = DelaunayTriangleCentroid.y + Random.Range(-InRange, InRange);
            }
        }

        public RenderTexture RenderPolygonalMap(Mesh InMapMesh, Texture2D InTexture, Material InMeshMaterial)
        {
            RenderTexture PolyMapRT = null;
            if (InTexture != null && InMeshMaterial != null)
            {
                PolyMapRT = new RenderTexture(MapDimensions.x, MapDimensions.y, 0);
                InMeshMaterial.mainTexture = InTexture;
                PolyMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, InMeshMaterial, false);
                if (bSaveDebugMaps)
                {
                    TextureGenerator.SaveMapAsPNG("RenderPolygonalMapTest", PolyMapRT);
                }
            }

            return PolyMapRT;
        }

        public RenderTexture RenderPolygonalMap(string InName, Mesh InMapMesh, Texture2D InTexture, Material InMeshMaterial)
        {
            RenderTexture PolyMapRT = null;
            if (InTexture != null && InMeshMaterial != null)
            {
                PolyMapRT = new RenderTexture(MapDimensions.x, MapDimensions.y, 0);
                InMeshMaterial.mainTexture = InTexture;
                PolyMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, InMeshMaterial, false);
                if (bSaveDebugMaps)
                {
                    TextureGenerator.SaveMapAsPNG(InName, PolyMapRT);
                }
            }

            return PolyMapRT;
        }


        public int[] TectonicPlatesFloodFill2D(BoundedVoronoi VorMap, List<int> InStartCells, bool bIsRandomSearch = false)
        {
            /* 
                Idea is rather than starting from a specific point and visiting its neighbours according to a heuristic,
                we'll instead swap between a set of starting points, which take turns visiting their neighbours which 
                should result in a evenly divided map between the N input starting points.

                Things we need:
                A means of swapping between Face vertices & their corresponding Delaunay Triangulation Vertices for 
                finding adjacent cells.

                One closed list shared by all entities, as once a cell is assigned, it isn't being re-assigned!

                But the Frontier/Open List, each entity needs their own?
             */


            /*
             * Current problem is the flood fill isn't really "flood" filling, it seems to be skipping
             * past some cells resulting in disjoint patches of coloured regions.
             */
            Vector2Int Hues = new Vector2Int(30, 330);
            Vector2Int Saturation = new Vector2Int(99, 100);
            Vector2Int Brightness = new Vector2Int(99, 100);
            List<Color> DebugColours = TextureGenerator.GenerateHSVColours(InStartCells.Count + 1, Hues, Saturation, Brightness);
            DebugColours[0] = Color.black;

            int MaxCells = VorMap.Faces.Count;
            int NumPlates = InStartCells.Count;
            HashSet<int> ClosedList = new HashSet<int>();
            HashSet<int> OpenedList = new HashSet<int>();
            Queue<int>[] Frontiers = new Queue<int>[NumPlates];
            int[] CellsToBeFilled = new int[MaxCells];

            for (int i = 0; i < NumPlates; i++)
            {
                Frontiers[i] = new Queue<int>();
                Frontiers[i].Enqueue(InStartCells[i]);
                ClosedList.Add(InStartCells[i]);
            }

            int CurrentPlateIndex = 0;

            int sum = Frontiers.Sum(collection => collection.Count);

            int Iteration = 0;

            while (Frontiers.Sum(collection => collection.Count) > 0)
            {
                Queue<int> CurrentQueue = Frontiers[CurrentPlateIndex];
                if (CurrentQueue.Count <= 0)
                {
                    CurrentPlateIndex++;
                    CurrentPlateIndex = CurrentPlateIndex % NumPlates;
                    continue;
                }

                int CurrentFaceIndex = CurrentQueue.Dequeue();
                while (CurrentFaceIndex < 0)
                {
                    CurrentFaceIndex = CurrentQueue.Dequeue();
                }
                if (CurrentFaceIndex < 0 || CurrentFaceIndex >= VorMap.Faces.Count)
                {
                    CurrentPlateIndex++;
                    CurrentPlateIndex = CurrentPlateIndex % NumPlates;
                    continue;
                }

                CellsToBeFilled[CurrentFaceIndex] = CurrentPlateIndex + 1;

                TFace Face = VorMap.Faces[CurrentFaceIndex];
                List<int> Neighbours = new List<int>();
                List<THalfEdge> Edges = Face.EnumerateEdges().ToList();
                foreach (var Edge in Edges)
                {
                    if (Edge.Twin != null && Edge.Twin.Face != null && Edge.Twin.Face != Face)
                    {
                        if (bIsRandomSearch)
                        {
                            Neighbours.Add(Edge.Twin.Face.ID);
                        }
                        else
                        {
                            if (!ClosedList.Contains(Edge.Twin.Face.ID))
                            {
                                if (Edge.Twin.Face.ID > -1)
                                {
                                    Frontiers[CurrentPlateIndex].Enqueue(Edge.Twin.Face.ID);
                                    ClosedList.Add(Edge.Twin.Face.ID);
                                }
                            }                            
                        }                        
                    }
                }

                if (bIsRandomSearch)
                {
                    Neighbours.Shuffle();
                    for (int j = 0; j < Neighbours.Count; j++)
                    {
                        if (!ClosedList.Contains(Neighbours[j]))
                        {
                            if (Neighbours[j] > -1)
                            {
                                Frontiers[CurrentPlateIndex].Enqueue(Neighbours[j]);
                                ClosedList.Add(Neighbours[j]);
                            }
                        }
                    }
                }

                if (CombinedVoronoiMesh != null)
                {
                    if (Iteration == 0)
                    {
                        //CellsToBeFilled[7] = 1;
                        //CellsToBeFilled[8] = 0;
                    }
                    RenderPolygonalMap("DebugPlates" + Iteration, CombinedVoronoiMesh, 
                        TextureGenerator.GenerateTectonicPlateTextureMap(MaxCells, CellsToBeFilled, DebugColours),
                        TextureGenerator.GetUnlitTextureMaterial()
                    );
                }

                Debug.Log("Iteration: " + Iteration + ", CurrentPlate: " + CurrentPlateIndex);
                // take turns between entities
                CurrentPlateIndex++;
                CurrentPlateIndex = (CurrentPlateIndex % NumPlates);
                Iteration++;
            }

            return CellsToBeFilled;
        }

        private int GetCellIDFromCoordinate(Vector3 InPoint, KDTree InKDTree, VQuery InQuery, ref List<int> InOutResults)
        {
            InOutResults.Clear();

            InQuery.ClosestPoint(InKDTree, InPoint, InOutResults);

            if (InOutResults.Count > 0)
            {
                return InOutResults[0];
            }

            return -1; // Not Found???
        }

        public List<int> GetCellIDsFromCoordinates(BoundedVoronoi InVoronoi, List<Vector3> InListPoints)
        {
            // Get a list of Vector3 coordinates (corresponding to locations picked via poisson disc distribution)
            // And return a list of indices to their corresponding voronoi faces
            List<int> Results = new List<int>();
            List<int> TempResults = new List<int>();
            // Convert a list of 2D coordinates into a list of corresponding 

            KDTree SiteKDTree = GetVoronoiSiteKDTree(ConvertVoronoiCellsToVec3(InVoronoi));
            VQuery SiteQuery = new VQuery();

            foreach (var Coordinate in InListPoints)
            {
                Results.Add(GetCellIDFromCoordinate(Coordinate, SiteKDTree, SiteQuery, ref TempResults));
            }

            return Results;
        }

        private List<Vector3> ConvertVoronoiCellsToVec3(BoundedVoronoi InVoronoi)
        {
            List<Vector3> OutCoords = new List<Vector3>();
            foreach (var Face in InVoronoi.Faces)
            {
                OutCoords.Add(new Vector3((float)Face.generator.X, (float)Face.generator.Y, 0));
            }

            return OutCoords;
        }

        private KDTree GetVoronoiSiteKDTree(List<Vector3> InVoronoiSiteCoords)
        {
            return new KDTree(InVoronoiSiteCoords.ToArray(), 32);
        }
    }
}
