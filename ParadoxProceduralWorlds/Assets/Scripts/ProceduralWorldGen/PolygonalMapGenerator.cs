using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TVertex = TriangleNet.Geometry.Vertex;
using System.Linq;
using DataStructures.ViliWonka.KDTree;
using RTree;
using TriangleNet.Voronoi;
using TMesh = TriangleNet.Mesh;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
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

        public Vector3[] InitialSites = null;

        public Vector2Int MapDimensions = new Vector2Int();

        public int NumberOfTargetCells = 0;

        public bool bSaveDebugMaps = true;

        public RenderTexture MainRTex = null;

        public PolygonalMapGenerator(int InNumCells)
        {
            Random.InitState(Time.time.ToString().GetHashCode());
            NumberOfTargetCells = InNumCells;
            SiteDistributionMode = ESiteDistribution.RANDOM;
        }

        public PolygonalMapGenerator(int InSeed, int InNumCells)
        {
            Random.InitState(InSeed);
            NumberOfTargetCells = InNumCells;
            SiteDistributionMode = ESiteDistribution.RANDOM;
        }

        public PolygonalMapGenerator(string InSeedString, int InNumCells)
        {
            Random.InitState(InSeedString.GetHashCode());
            NumberOfTargetCells = InNumCells;
            SiteDistributionMode = ESiteDistribution.RANDOM;
        }

        public void SetDistributionMode(ESiteDistribution InDistribution)
        {
            SiteDistributionMode = InDistribution;
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
                InitialSites = GenerateRandomPoints(NumberOfTargetCells, MapDimensions, InPadding).ToArray();
                break;
            }
        }

        public Mesh GeneratePolygonalMapMesh()
        {
            GenerateSiteDistribution(25);
            TMesh Triangulation = GenerateDelaunayTriangulation(InitialSites, 2);
            BoundedVoronoi mBoundedVoronoi = GetVoronoiTesselationFromTriangulation(Triangulation);
            MoveVoronoiVerticesToDelauneyCentroids(mBoundedVoronoi, 2.5f);
            List<Mesh> VorCellMeshes = TriangulateVoronoiCells(mBoundedVoronoi);            
            RandomLandDistribution(mBoundedVoronoi.Faces.Count);
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

            Mesh CombinedVoronoiMesh = new Mesh();
            CombinedVoronoiMesh.CombineMeshes(CombineInstances);

            MainRTex = RenderPolygonalMap(CombinedVoronoiMesh, VoronoiTexture, TextureGenerator.GetUnlitTextureMaterial());

            return CombinedVoronoiMesh;
        }

        private static bool IsValid
        (
            Vector3 InCandidate, Vector2 InMapSizes, float InCelLSize, float InRadius, List<Vector3> InPoints, int[,] InGrid, int InPadding
        )
        {
            if (InCandidate.x >= InPadding && InCandidate.x < (InMapSizes.x  - InPadding) && InCandidate.y >= InPadding && InCandidate.y < (InMapSizes.y - InPadding))
            {
                int CellX = (int)(InCandidate.x / InCelLSize);
                int CellY = (int)(InCandidate.y / InCelLSize);
                int SearchStartX = Mathf.Max(0, CellX - 2);
                int SearchEndX = Mathf.Min(CellX + 2, InGrid.GetLength(0) - 1);
                int SearchStartY = Mathf.Max(0, CellY - 2);
                int SearchEndY = Mathf.Min(CellY + 2, InGrid.GetLength(1) - 1);

                for (int x = SearchStartX; x <= SearchEndX; x++)
                {
                    for (int y = SearchStartY; y <= SearchEndY; y++)
                    {
                        int PointIndex = InGrid[x, y] - 1;
                        if (PointIndex != -1)
                        {
                            float SqrDist = (InCandidate - InPoints[PointIndex]).sqrMagnitude;
                            if (SqrDist < InRadius * InRadius)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public List<Vector3> GenerateRandomPoints(int InNumPoints, Vector2Int InMapSizes, int InPadding)
        {
            List<Vector3> Points = new List<Vector3>();

            for (int i = 0; i < InNumPoints; i++)
            {
                Points.Add
                (
                    new Vector3
                    (
                        Random.Range(InPadding, InMapSizes.x - InPadding), 
                        Random.Range(InPadding, InMapSizes.y - InPadding), 
                        0
                    )
                );
            }

            return Points;
        }

        public List<Vector3> GeneratePoissonDistribution
        (
            int InTargetTiles, Vector2Int InMapSizes, float InRadius, int numMaxSamples, int InPadding
        )
        {
            float CellSize = InRadius / Mathf.Sqrt(2);

            int[,] Grid = new int[Mathf.CeilToInt(InMapSizes.x / CellSize), Mathf.CeilToInt(InMapSizes.y / CellSize)];
            List<Vector3> Points = new List<Vector3>();
            List<Vector3> SpawnPoints = new List<Vector3>();

            SpawnPoints.Add(
                new Vector3
                (
                    Mathf.CeilToInt(Random.Range(InPadding, InMapSizes.x - InPadding)), 
                    Mathf.CeilToInt(Random.Range(InPadding, InMapSizes.y - InPadding)),
                    0
                )
            );

            //Points.Add(new Vector3(InPadding, InPadding, 0));
            //Points.Add(new Vector3(InMapSizes.x - InPadding + 1, InPadding, 0));
            //Points.Add(new Vector3(InPadding, InMapSizes.y - InPadding + 1, 0));
            //Points.Add(new Vector3(InMapSizes.x - InPadding + 1, InMapSizes.y - InPadding + 1, 0));

            Debug.Log("First point randomly chosen at " + SpawnPoints[0]);

            while (SpawnPoints.Count > 0 && Points.Count < InTargetTiles)
            {
                int SpawnIndex = Random.Range(0, SpawnPoints.Count);
                Vector3 SpawnCenter = SpawnPoints[SpawnIndex];
                bool bCandidateAccepted = false;

                for (int i = 0; i < numMaxSamples; i++)
                {
                    float Angle = Random.value * Mathf.PI * 2;
                    Vector3 Direction = new Vector3(Mathf.Sin(Angle), Mathf.Cos(Angle), 0);
                    Vector3 Candidate = SpawnCenter + Direction * Random.Range(InRadius, 2 * InRadius);
                    if (IsValid(Candidate, InMapSizes, CellSize, InRadius, Points, Grid, InPadding))
                    {
                        Points.Add(Candidate);
                        SpawnPoints.Add(Candidate);
                        Grid[(int)(Candidate.x / CellSize), (int)(Candidate.y / CellSize)] = Points.Count;
                        bCandidateAccepted = true;
                        break;
                    } 
                }

                if (!bCandidateAccepted)
                {
                    SpawnPoints.RemoveAt(SpawnIndex);
                }
            }

            return Points;
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
    }
}
