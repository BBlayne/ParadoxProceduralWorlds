using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TVertex = TriangleNet.Geometry.Vertex;
using System.Linq;
using DataStructures.ViliWonka.KDTree;
using TMPro;
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
        RANDOM,
        POISSON,
        RANDOM_MIRRORED
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

        public int MapPadding = 25;

        public bool bSaveDebugMaps = true;
        public bool bSaveDebugIncrementalFloodFillMaps = false;

        public RenderTexture MainRTex = null;

        public BoundedVoronoi BoundedVor = null;
        public VoronoiBase BaseVor = null;
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

        public void SetPadding(int InPadding)
        {
            MapPadding = InPadding;
        }

        public List<Vector3> GenerateSiteDistribution
        (
            ESiteDistribution InSiteDistribution, 
            int InTargetNumberSites, 
            Vector2Int InMapDims, 
            int InPadding,
            List<Vector3> InSupplementSites
        )
        {
            switch (InSiteDistribution)
            {
            case ESiteDistribution.POISSON:
                return MapUtils.GetPoissonDistributedPoints2D(
                    InMapDims,
                    MapUtils.DetermineRadiusForPoissonDisc(InMapDims, InTargetNumberSites), 
                    30, 
                    InPadding,
                    InSupplementSites,
                    InTargetNumberSites
                );
            case ESiteDistribution.RANDOM:
            default:
                return MapUtils.GenerateRandomPoints2D(InTargetNumberSites, InMapDims, InPadding);
            }
        }

        public void GenerateSiteDistribution()
        {
            switch (SiteDistributionMode)
            {
            case ESiteDistribution.RANDOM_MIRRORED:
                InitialSites = MapUtils.GenerateRandomPointsMirrored2D(NumberOfTargetCells, MapDimensions, MapPadding).ToArray();
                break;
            case ESiteDistribution.RANDOM:
            default:
                InitialSites = MapUtils.GenerateRandomPoints2D(NumberOfTargetCells, MapDimensions, MapPadding).ToArray();
                break;
            }
        }

        public Mesh GetArrowMesh(float InStemWidth, float InTipWidth, float InTipLength, float InStemLength)
        {
            // Generates an Arrow that's centered near the approximate center of gravity, 
            // Instead of being centered about the origin of the stem.

            // setup
            List<Vector3> VerticeList = new List<Vector3>();
            List<int> TriangleList = new List<int>();

            Mesh Arrow = new Mesh();

            // Stem Setup
            Vector3 StemOrigin = Vector3.zero;
            float StemHalfWidth = InStemWidth / 2;
            float StemHalfLength = InStemLength / 2;

            // Stem Points
            VerticeList.Add(StemOrigin + (StemHalfWidth * Vector3.down) - (StemHalfLength * Vector3.right));
            VerticeList.Add(StemOrigin + (StemHalfWidth * Vector3.up) - (StemHalfLength * Vector3.right));
            VerticeList.Add(VerticeList[0] + (InStemLength * Vector3.right));
            VerticeList.Add(VerticeList[1] + (InStemLength * Vector3.right));

            // Stem Triangles
            TriangleList.Add(0);
            TriangleList.Add(1);
            TriangleList.Add(3);

            TriangleList.Add(0);
            TriangleList.Add(3);
            TriangleList.Add(2);

            // Tip setup
            Vector3 TipOrigin = StemHalfLength * Vector3.right;
            float TipHalfWidth = InTipWidth / 2;

            // Tip Points
            VerticeList.Add(TipOrigin + (TipHalfWidth * Vector3.up));
            VerticeList.Add(TipOrigin + (TipHalfWidth * Vector3.down));
            VerticeList.Add(TipOrigin + (InTipLength * Vector3.right));

            // tip triangle
            TriangleList.Add(4);
            TriangleList.Add(6);
            TriangleList.Add(5);

            Arrow.vertices = VerticeList.ToArray();
            Arrow.triangles = TriangleList.ToArray();

            return Arrow;
        }

        public Mesh GenerateTectonicPlateCellArrows(BoundedVoronoi InVor, int[] InTectonicPlateCellIndices, EPlateDirections[] InDirections)
        {
            Mesh CombinedArrowMesh = new Mesh();

            CombineInstance[] CombineInstances = new CombineInstance[InVor.Faces.Count];
            for (int i = 0; i < InVor.Faces.Count; i++)
            {
                CombineInstances[i].mesh = GetArrowMesh(2, 6, 3, 4);

                EPlateDirections CellDirection = InDirections[InTectonicPlateCellIndices[i] - 1];
                Quaternion ArrowRotation = Quaternion.identity;
                Vector3 CellPosition = TriangleNetUtility.PointToVector3(InVor.Faces[i].generator);
                switch (CellDirection)
                {
                case EPlateDirections.NORTH:
                    ArrowRotation = Quaternion.AngleAxis(90, Vector3.forward);
                    break;
                case EPlateDirections.NORTHEAST:
                    ArrowRotation = Quaternion.AngleAxis(45, Vector3.forward);
                    break;
                case EPlateDirections.EAST:
                    ArrowRotation = Quaternion.AngleAxis(0, Vector3.forward);
                    break;
                case EPlateDirections.SOUTHEAST:
                    ArrowRotation = Quaternion.AngleAxis(315, Vector3.forward);                    
                    break;
                case EPlateDirections.SOUTH:
                    ArrowRotation = Quaternion.AngleAxis(270, Vector3.forward);
                    break;
                case EPlateDirections.SOUTHWEST:
                    ArrowRotation = Quaternion.AngleAxis(225, Vector3.forward);
                    break;
                case EPlateDirections.WEST:
                    ArrowRotation = Quaternion.AngleAxis(180, Vector3.forward);                    
                    break;
                case EPlateDirections.NORTHWEST:
                    ArrowRotation = Quaternion.AngleAxis(135, Vector3.forward);
                    break;
                }
                CombineInstances[i].transform = Matrix4x4.TRS(CellPosition, ArrowRotation, Vector3.one);
            }
            CombinedArrowMesh.CombineMeshes(CombineInstances);

            return CombinedArrowMesh;
        }

        public Mesh GeneratePolygonalMapMesh() 
        {
            GenerateSiteDistribution();
            TMesh Triangulation = GenerateDelaunayTriangulation(InitialSites, 2);
            Triang = Triangulation;

            Mesh TriangUnityMesh = GenerateUnityMesh(Triangulation);
            RenderTexture TriangGraphRTex = RenderPolygonalWireframeMap(TriangUnityMesh, null, TextureGenerator.GetUnlitTextureMaterial(), Color.white);

            if (bSaveDebugMaps)
            {
                TextureGenerator.SaveTextureAsPNG(TextureGenerator.CreateTexture2D(TriangGraphRTex), "TriangUnityMesh");
            }

            BoundedVoronoi mBoundedVoronoi = GetBoundedVoronoiTesselationFromTriangulation(Triang);
            StandardVoronoi mStdVoronoi = GetStandardVoronoiTesselationFromTriangulation(Triang);
            BoundedVor = mBoundedVoronoi;
            BaseVor = mStdVoronoi;

            MoveVoronoiVerticesToDelauneyCentroids(mBoundedVoronoi, 2.5f);
            BoundedVor = mBoundedVoronoi;

            Mesh VoronoiUnityMesh = GenerateVoronoiUnityMesh(mBoundedVoronoi);
            RenderTexture VoronoiGraphRTex = RenderPolygonalWireframeMap(VoronoiUnityMesh, null, TextureGenerator.GetUnlitTextureMaterial(), Color.white);
            
            if (bSaveDebugMaps)
            {
                TextureGenerator.SaveTextureAsPNG(TextureGenerator.CreateTexture2D(VoronoiGraphRTex), "VoronoiUnityMesh");
            }

            List<Mesh> VorCellMeshes = TriangulateVoronoiCells(mBoundedVoronoi, false);
            Debug.Log("");
            RandomLandDistribution(mBoundedVoronoi.Faces.Count, 0.25f);
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

            // Fucks up somewhere because out of bounds...?
            delaneyShape.Add(new TVertex(0, 0));
            delaneyShape.Add(new TVertex(0, MapDimensions.y + 1));
            delaneyShape.Add(new TVertex(MapDimensions.x + 1, MapDimensions.y + 1));
            delaneyShape.Add(new TVertex(MapDimensions.x + 1, 0));

            //delaneyShape.Add(new TVertex(0, 0));
            //delaneyShape.Add(new TVertex(0, MapDimensions.y)); 
            //delaneyShape.Add(new TVertex(MapDimensions.x, MapDimensions.y));
            //delaneyShape.Add(new TVertex(MapDimensions.x, 0)); 

            //delaneyShape.Add(new TVertex(0, 0));
            //delaneyShape.Add(new TVertex(1, MapDimensions.y)); 
            //delaneyShape.Add(new TVertex(MapDimensions.x, MapDimensions.y)); 
            //delaneyShape.Add(new TVertex(MapDimensions.x, 1)); 

            delaneyShape.Bounds();

            TMesh TriangleMesh = (TriangleNet.Mesh)delaneyShape.Triangulate(options);

            TriangleNet.Smoothing.SimpleSmoother simpleSmoother = new TriangleNet.Smoothing.SimpleSmoother();
            if (TriangleMesh != null)
            {
                simpleSmoother.Smooth(TriangleMesh, InSmoothing);
            }

            return TriangleMesh;
        }

        public StandardVoronoi GetStandardVoronoiTesselationFromTriangulation(TMesh InTriangleMesh)
        {
            return new StandardVoronoi(InTriangleMesh);
        }

        public BoundedVoronoi GetBoundedVoronoiTesselationFromTriangulation(TMesh InTriangleMesh)
        {
            return new BoundedVoronoi(InTriangleMesh);
        }

        public Mesh GenerateVoronoiUnityMesh(VoronoiBase InVor)
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

        public List<Mesh> TriangulateVoronoiCells(VoronoiBase InVorGraph, bool bCheckBounds)
        {
            TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions()
            {
                ConformingDelaunay = true
            };

            List<Mesh> VoronoiTriangulations = new List<Mesh>();

            for (int VorIndex = 0; VorIndex < InVorGraph.Faces.Count; VorIndex++)
            {
                TFace CurrentFace = InVorGraph.Faces[VorIndex];
                if (CurrentFace == null || (bCheckBounds && !CurrentFace.bounded))
                {
                    continue;
                }

                List<THalfEdge> CurrentFaceEdges = CurrentFace.EnumerateEdges().ToList();
                Polygon DelaneyShape = new Polygon();

                // Better method
                foreach (THalfEdge CurrentFaceEdge in CurrentFaceEdges)
                {
                    // Rounding in attempt to fix an issue with errant pixels
                    double Ecks = Mathf.Round((float)CurrentFaceEdge.Origin.X);
                    double Why = Mathf.Round((float)CurrentFaceEdge.Origin.Y);

                    TVertex VertexToAdd = new TVertex(Ecks, Why);
                    DelaneyShape.Add(VertexToAdd);
                }
                DelaneyShape.Bounds();

                VoronoiTriangulations.Add(GenerateUnityMeshFromTriangleNetMesh((TMesh)DelaneyShape.Triangulate(options)));
            }

            return VoronoiTriangulations;
        }        

        public void GenerateUVsForVoronoiUnityMesh(List<Mesh> InVoronoiTriangulations)
        {
            float Offset = 0.0625f / InVoronoiTriangulations.Count;
            Debug.Log("Generating UVs for " + InVoronoiTriangulations.Count + " polygonal cells.");
            for (int i = 0; i < InVoronoiTriangulations.Count; i++)
            {
                List<Vector2> MeshUVs = new List<Vector2>();                
                for (int j = 0; j < InVoronoiTriangulations[i].vertexCount; j++)
                {
                    float NuU = ((float)i / InVoronoiTriangulations.Count) + Offset;
                    MeshUVs.Add(new Vector2(NuU, 0.0f));
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

        // Adjusting Voronoi Vertices into Centroids of the Delaunay Triangles
        public void MoveVoronoiVerticesToDelauneyCentroids(BoundedVoronoi InVor, float InJitterRange)
        {
            // Iterate through and relax the vertices of all voronoi faces except for those along the map edges
            foreach (var mFace in InVor.Faces)
            {
                if (mFace.bounded)
                {
                    List<THalfEdge> FaceEdges = mFace.EnumerateEdges().ToList();
                    foreach (var mFaceEdge in FaceEdges)
                    {
                        // get all edges surrounding this vertex
                        List<THalfEdge> VertexEdges = mFaceEdge.Origin.EnumerateEdges().ToList();
                        var CurrentVertex = InVor.Vertices[mFaceEdge.Origin.ID];
                        // compute the centroid by getting the location of the site of each face
                        Vector2 DelaunayTriangleCentroid = new Vector2(0, 0);
                        foreach (var VertexEdge in VertexEdges)
                        {
                            DelaunayTriangleCentroid += new Vector2(
                                (float)VertexEdge.Face.generator.X, 
                                (float)VertexEdge.Face.generator.Y
                            );
                        }

                        DelaunayTriangleCentroid /= VertexEdges.Count;

                        CurrentVertex.X = Mathf.Round(DelaunayTriangleCentroid.x + Random.Range(-InJitterRange, InJitterRange));
                        CurrentVertex.Y = Mathf.Round(DelaunayTriangleCentroid.y + Random.Range(-InJitterRange, InJitterRange));
                    }
                }
            }

            /*
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

                CurrentVertex.X = Mathf.Round(DelaunayTriangleCentroid.x + Random.Range(-InRange, InRange));
                CurrentVertex.Y = Mathf.Round(DelaunayTriangleCentroid.y + Random.Range(-InRange, InRange));
            }
            */
        }

        // Might have this have a mode passed in for Solid vs Wireframe...
        public RenderTexture RenderArrows(Mesh InMapMesh, Color InArrowColour)
        {
            RenderTexture ArrowMapRT = null;
            Material MeshMaterial = TextureGenerator.GetUnlitMaterial();
            if (MeshMaterial != null)
            {
                MeshMaterial.SetColor("_Color", InArrowColour);
                ArrowMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, MeshMaterial, false, true);
                if (bSaveDebugMaps)
                {
                    TextureGenerator.SaveMapAsPNG("RenderArrowMapTest", ArrowMapRT);
                }
            }

            return ArrowMapRT;
        }

        public RenderTexture RenderPolygonalWireframeMap(Mesh InMapMesh, Material InMeshMaterial, Color InColour, bool isBGTransparent = true)
        {
            RenderTexture PolyMapRT = null;
            if (InMeshMaterial != null)
            {
                InMeshMaterial.SetColor("_Color", InColour);
                PolyMapRT = new RenderTexture(MapDimensions.x, MapDimensions.y, 0);
                PolyMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, InMeshMaterial, true, isBGTransparent);
                if (bSaveDebugMaps)
                {
                    //TextureGenerator.SaveMapAsPNG("RenderPolygonalMapTest", PolyMapRT);
                }
            }

            return PolyMapRT;
        }

        public RenderTexture RenderPolygonalWireframeMap(Mesh InMapMesh, Texture2D InTexture, Material InMeshMaterial, Color InColour, bool isBGTransparent = true)
        {
            RenderTexture PolyMapRT = null;
            if (InMeshMaterial != null)
            {
                InMeshMaterial.SetColor("_Color", InColour);
                PolyMapRT = new RenderTexture(MapDimensions.x, MapDimensions.y, 0);
                if (InTexture != null)
                {
                    InMeshMaterial.mainTexture = InTexture;
                }

                PolyMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, InMeshMaterial, true, isBGTransparent);
                if (bSaveDebugMaps)
                {
                    //TextureGenerator.SaveMapAsPNG("RenderPolygonalMapTest", PolyMapRT);
                }
            }

            return PolyMapRT;
        }

        public RenderTexture RenderPolygonalMap(Mesh InMapMesh, Texture2D InTexture, Material InMeshMaterial)
        {
            RenderTexture PolyMapRT = null;
            if (InMeshMaterial != null)
            {
                PolyMapRT = new RenderTexture(MapDimensions.x, MapDimensions.y, 0);
                if (InTexture != null)
                {
                    InMeshMaterial.mainTexture = InTexture;
                }
                
                PolyMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, InMeshMaterial, false, true);
                if (bSaveDebugMaps)
                {
                    TextureGenerator.SaveMapAsPNG("RenderPolygonalMapDebug", PolyMapRT);
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
                PolyMapRT = TextureGenerator.BlitMeshToRT(InMapMesh, MapDimensions, InMeshMaterial, false, false);
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
            Vector2Int Hues = new Vector2Int(30, 330);
            Vector2Int Saturation = new Vector2Int(99, 100);
            Vector2Int Brightness = new Vector2Int(99, 100);
            List<Color> DebugColours = TextureGenerator.GenerateHSVColours(InStartCells.Count + 1, Hues, Saturation, Brightness);
            DebugColours[0] = Color.black;

            int MaxCells = VorMap.Faces.Count;
            int NumPlates = InStartCells.Count;
            HashSet<int> ClosedList = new HashSet<int>();
            Heap<Priority>[] Frontiers = new Heap<Priority>[NumPlates];
            int[] CellsToBeFilled = new int[MaxCells];

            for (int i = 0; i < NumPlates; i++)
            {
                Frontiers[i] = new Heap<Priority>(MaxCells);
                Frontiers[i].Add(new Priority(InStartCells[i], 0));
                ClosedList.Add(InStartCells[i]);
            }

            int CurrentPlateIndex = 0;
            int MinimumBlobSize = 4;
            int MaxIterations = Mathf.RoundToInt(Mathf.Min(MaxCells, MinimumBlobSize * NumPlates));
            int Iteration = 0;
            // As we might be doing random flood fill, we'll iterate over the initial points
            // at least once. Potentially N by M times so each Blob has a minimum of M cells.
            while (Iteration < MaxIterations)
            {
                for (int i = 0; i < NumPlates; i++)
                {
                    Heap<Priority> CurrentQueue = Frontiers[i];
                    if (CurrentQueue.Count <= 0)
                    {
                        continue;
                    }

                    int CurrentFaceIndex = CurrentQueue.RemoveFirst().Value;
                    // Annoyingly some faces have a invalid negative index
                    // for esoteric voronoi computational reasons...
                    while (CurrentFaceIndex < 0 && CurrentQueue.Count > 0)
                    {
                        CurrentFaceIndex = CurrentQueue.RemoveFirst().Value;
                    }

                    if (CurrentFaceIndex < 0)
                    {
                        continue;
                    }

                    CellsToBeFilled[CurrentFaceIndex] = i + 1; // Offset so all cells default are black

                    TFace Face = VorMap.Faces[CurrentFaceIndex];
                    List<THalfEdge> Edges = Face.EnumerateEdges().ToList();
                    foreach (var Edge in Edges)
                    {
                        if (Edge.Twin != null && Edge.Twin.Face != null && Edge.Twin.Face != Face)
                        {
                            if (!ClosedList.Contains(Edge.Twin.Face.ID))
                            {
                                int Rank = bIsRandomSearch ? Random.Range(0, MaxCells * 100) : ClosedList.Count;
                                Frontiers[i].Add(new Priority(Edge.Twin.Face.ID, Rank));
                                ClosedList.Add(Edge.Twin.Face.ID);
                            }
                        }
                    }
                    Iteration++;
                }                
            }

            int sum = Frontiers.Sum(collection => collection.Count);
            while (Frontiers.Sum(collection => collection.Count) > 0)
            {
                Heap<Priority> CurrentQueue = Frontiers[CurrentPlateIndex];
                if (CurrentQueue.Count <= 0)
                {
                    CurrentPlateIndex = (CurrentPlateIndex + 1) % NumPlates;
                    continue;
                }

                int CurrentFaceIndex = CurrentQueue.RemoveFirst().Value;
                while (CurrentFaceIndex < 0 && CurrentQueue.Count > 0)
                {
                    CurrentFaceIndex = CurrentQueue.RemoveFirst().Value;
                }

                if (CurrentFaceIndex < 0)
                {
                    CurrentPlateIndex = (CurrentPlateIndex + 1) % NumPlates;
                    continue;
                }

                CellsToBeFilled[CurrentFaceIndex] = CurrentPlateIndex + 1;

                TFace Face = VorMap.Faces[CurrentFaceIndex];
                List<THalfEdge> Edges = Face.EnumerateEdges().ToList();
                foreach (var Edge in Edges)
                {
                    if (Edge.Twin != null && Edge.Twin.Face != null && Edge.Twin.Face != Face)
                    {
                        if (!ClosedList.Contains(Edge.Twin.Face.ID))
                        {
                            if (Edge.Twin.Face.ID > -1)
                            {
                                int Rank = bIsRandomSearch ? Random.Range(0, MaxCells * 100) : ClosedList.Count;
                                Frontiers[CurrentPlateIndex].Add(new Priority(Edge.Twin.Face.ID, Rank));
                                ClosedList.Add(Edge.Twin.Face.ID);
                            }
                        }
                    }
                }

                if (bSaveDebugIncrementalFloodFillMaps)
                {
                    if (CombinedVoronoiMesh != null)
                    {
                        RenderPolygonalMap("DebugPlates" + Iteration, CombinedVoronoiMesh, 
                            TextureGenerator.GenerateTectonicPlateTextureMap(MaxCells, CellsToBeFilled, DebugColours),
                            TextureGenerator.GetUnlitTextureMaterial()
                        );
                    }
                }

                if (bIsRandomSearch)
                {
                    CurrentPlateIndex = Random.Range(0, NumPlates);
                }
                else
                {
                    CurrentPlateIndex++;
                    CurrentPlateIndex = CurrentPlateIndex % NumPlates;
                }

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

        public int[] ContinentsFloodFill2D(BoundedVoronoi VorMap, List<int> InStartCells, List<ContinentInfo> InContinentInfo)
        {
            int DebugIterationStep = 2;
            // What do we want:
            // Array of indices 0...N per tile mapping to one of N continents.
            // What we need:
            // Array whose Length is the # of landmasses, and it's elements are the # of cells.
            // Our Voronoi map.
            Vector2Int Hues = new Vector2Int(30, 330);
            Vector2Int Saturation = new Vector2Int(99, 100);
            Vector2Int Brightness = new Vector2Int(99, 100);
            List<Color> DebugColours = TextureGenerator.GenerateHSVColours(InStartCells.Count + 1, Hues, Saturation, Brightness);
            DebugColours[0] = Color.black;

            int MaxCells = VorMap.Faces.Count;
            int NumContinents = InStartCells.Count;
            HashSet<int> ClosedList = new HashSet<int>();
            Heap<PriorityVCell> Frontier = new Heap<PriorityVCell>(MaxCells);
            int[] CellsToBeFilled = new int[MaxCells];
            int[] ContinentCellCounter = new int[NumContinents];

            for (int i = 0; i < NumContinents; i++)
            {
                Frontier.Add(new PriorityVCell(InStartCells[i], InStartCells[i], i));                
                CellsToBeFilled[InStartCells[i]] = i + 1;
                ClosedList.Add(InStartCells[i]);
                ContinentCellCounter[i] = 1;
            }

            //int MinimumBlobSize = 4;
            //int MaxIterations = Mathf.RoundToInt(Mathf.Min(MaxCells, MinimumBlobSize * NumContinents));
            int Iteration = 0;

            while (Frontier.Count > 0)
            {
                PriorityVCell CurrentCell = Frontier.RemoveFirst();
                if (CurrentCell == null)
                {
                    continue;
                }

                int CurrentFaceIndex = CurrentCell.CellIndex;
                int CurrentContinentID = CellsToBeFilled[CurrentCell.CellParentIndex];
                // In case we have fake faces inserted by our voronoi library
                // We'll keep popping until we have another valid one.
                while (CurrentFaceIndex < 0)
                {
                    PriorityVCell PoppedCell = Frontier.RemoveFirst();
                    if (PoppedCell != null)
                    {
                        CurrentFaceIndex = PoppedCell.CellIndex;
                    }                    
                }

                CellsToBeFilled[CurrentFaceIndex] = CurrentContinentID;                

                // Iterate through all of the adjacent faces to the current voronoi cell face
                TFace Face = VorMap.Faces[CurrentFaceIndex];
                List<THalfEdge> Edges = Face.EnumerateEdges().ToList();
                foreach (var Edge in Edges)
                {
                    // If the current continent has reached its target number of cells
                    ContinentInfo CurrentContinentInfo = InContinentInfo[CurrentContinentID - 1];
                    if (ContinentCellCounter[CurrentContinentID - 1] >= CurrentContinentInfo.NumCells)
                    {
                        break;
                    }

                    if (Edge.Twin != null && Edge.Twin.Face != null && Edge.Twin.Face != Face)
                    {
                        if (!ClosedList.Contains(Edge.Twin.Face.ID))
                        {
                            if (Edge.Twin.Face.ID > -1)
                            {
                                int Rank = Random.Range(0, int.MaxValue);
                                Frontier.Add(new PriorityVCell(Edge.Twin.Face.ID, CurrentFaceIndex, Rank));
                                ClosedList.Add(Edge.Twin.Face.ID);
                                ContinentCellCounter[CurrentContinentID - 1]++;
                            }
                        }
                    }
                }

                // Debug Images
                if (bSaveDebugIncrementalFloodFillMaps && (Iteration % DebugIterationStep == 0))
                {
                    if (CombinedVoronoiMesh != null)
                    {
                        RenderPolygonalMap("DebugContinents" + Iteration, CombinedVoronoiMesh,
                            TextureGenerator.GenerateContinentalTextureMap(MaxCells, CellsToBeFilled, DebugColours),
                            TextureGenerator.GetUnlitTextureMaterial()
                        );
                    }
                }

                Iteration++;
            }

            return CellsToBeFilled;
        }

        public RenderTexture GenerateVorCellIdTex(BoundedVoronoi InVorGraph, Vector2Int InWorldSizes, TMP_Text InTextObj, float InTxtSize)
        {
            RenderTexture OutRTex = new RenderTexture(InWorldSizes.x, InWorldSizes.y, 0);
            RenderTexture TempRTex = RenderTexture.GetTemporary(
                InWorldSizes.x,
                InWorldSizes.y,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );

            int oldQuality = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(5);

            RenderTexture Previous = RenderTexture.active;
            RenderTexture.active = TempRTex;
            RenderTexture TestTextRTex = null;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, InWorldSizes.x, InWorldSizes.y, 0);
            foreach (var CellFace in InVorGraph.Faces)
            {
                if (TestTextRTex != null)
                {
                    TestTextRTex.Release();
                    UnityEngine.Object.Destroy(TestTextRTex);
                }
                InTextObj.SetText(CellFace.ID.ToString());
                InTextObj.ForceMeshUpdate();
                Vector3 Position = new Vector3(
                    (float)CellFace.generator.X / InWorldSizes.x, 
                    (float)CellFace.generator.Y / InWorldSizes.y, 
                    0);

                TestTextRTex = TextureGenerator.BlitTextToTexture(InTextObj, Position, InWorldSizes, InTxtSize);
                Graphics.DrawTexture(new Rect(0, 0, InWorldSizes.x, InWorldSizes.y), TestTextRTex);
            }
            GL.PopMatrix();

            Graphics.Blit(TempRTex, OutRTex);
            RenderTexture.active = Previous;
            RenderTexture.ReleaseTemporary(TempRTex);

            QualitySettings.SetQualityLevel(oldQuality);

            return OutRTex;
        }
    }
}
