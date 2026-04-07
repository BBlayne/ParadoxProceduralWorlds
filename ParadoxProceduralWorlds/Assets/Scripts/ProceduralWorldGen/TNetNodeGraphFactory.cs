
using DataStructures.ViliWonka.KDTree;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Geometry;
using TriangleNet.Voronoi;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;
using TEdge = TriangleNet.Geometry.Edge;
using TFace = TriangleNet.Topology.DCEL.Face;
using THalfEdge = TriangleNet.Topology.DCEL.HalfEdge;
using TMesh = TriangleNet.Mesh;
using TOSub = TriangleNet.Topology.Osub;
using TSubSegment = TriangleNet.Topology.SubSegment;
using TTriangle = TriangleNet.Topology.Triangle;
using TVertex = TriangleNet.Geometry.Vertex;
using TVVertex = TriangleNet.Topology.DCEL.Vertex;
using VQuery = DataStructures.ViliWonka.KDTree.KDQuery;
using UMath = System.Math;

public class TNetNodeGraphFactory : INodeGraphFactory<TriangleNetTriangulator>
{
	public TriangleNetTriangulator Triangulator { get; set; }
	public TriangulationConfig Configuration { get; set; }

	public SiteGeneratorConfig GeneratorConfig { get; set; }

	public SiteData GeneratedSites { get; set; }

	public KDTree VSiteKDTree = null;
	public KDTree TriangleKDTree = null;

	public TNetNodeGraphFactory()
	{
		Triangulator = new TriangleNetTriangulator();
	}

	public void Init()
	{
		Triangulator.Configuration = Configuration;
	}

	public INodeGraph GenerateNodeGraph()
	{
		if (Triangulator == null)
			return null;

		Triangulator.Sites = GeneratedSites.GeneratedSites;
		Triangulator.Configuration = Configuration;

		Triangulator.Triangulate();

		if (Triangulator.TNetVoronoiTesselation == null || Triangulator.Triangulation == null)
			return null;

		PolygonalNodeGraph nodeGraph = new PolygonalNodeGraph();

		// grab our underlying data as easy to use arrays
		TVertex[] TVertices = Triangulator.Triangulation.Vertices.ToArray();
		TTriangle[] TTriangles = Triangulator.Triangulation.Triangles.ToArray();
		TEdge[] TEdges = Triangulator.Triangulation.Edges.ToArray();

		TFace[] VFaces = Triangulator.TNetVoronoiTesselation.Faces.ToArray();
		THalfEdge[] THalfEdges = Triangulator.TNetVoronoiTesselation.HalfEdges.ToArray();
		TVVertex[] VVertices = Triangulator.TNetVoronoiTesselation.Vertices.ToArray();

		TSubSegment[] TSubSegments = new TSubSegment[Triangulator.Triangulation.subsegs.Count];
		Triangulator.Triangulation.subsegs.Values.CopyTo(TSubSegments, 0);

		Dictionary<int, VCell> BoundaryCells = new Dictionary<int, VCell>();
		Dictionary<int, THalfEdge> BoundaryEdges = new Dictionary<int, THalfEdge>();

		VQuery SiteQuery = new VQuery();
		Dictionary<int, int> FaceEdges = new Dictionary<int, int>();

		// List of our coordinates corresponding to our Voronoi Cells/Delaunay nodes
		// preserving the ID's for ordering purposes.
		Dictionary<int, Vector3> NodeCoords = new Dictionary<int, Vector3>();

		Dictionary<int, Vector3> TriangleCenters = new Dictionary<int, Vector3>();

		/*
		 * Generate Delaunay Vertices (DVertex)
		 *	- ID
		 *	- Coordinates (2D)
		 *	
		 *	To Backtrack:
		 *	- A Delaunay vertex is neighboured by edges.
		 *
		 */
		int NumTVertices = TVertices.Length;
		nodeGraph.DVertices = new DVertex[NumTVertices];
		nodeGraph.CellCoordinates = new Vector3[NumTVertices];
		for (int i = 0; i < NumTVertices; i++)
		{
			TVertex TVert = TVertices[i];
			nodeGraph.DVertices[i] = new DVertex();
			nodeGraph.DVertices[i].ID = TVert.ID;
			nodeGraph.DVertices[i].Coords = new Vector3((float)TVert.X, (float)TVert.Y, 0.0f);
			NodeCoords.Add(nodeGraph.DVertices[i].ID, nodeGraph.DVertices[i].Coords);
			nodeGraph.CellCoordinates[i] = nodeGraph.DVertices[i].Coords;
		}

		nodeGraph.InitCellCoordinateSearchTree();

		VSiteKDTree = new KDTree(NodeCoords.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray(), 32);

		/*
		 * Generate Delaunay Triangles from TNet - Triangles (DFace)
		 * Called 'Face' for our Barycentric Dual Mesh.
		 * - ID
		 * - Composite vertices.
		 * - Backtrack: Neighbouring triangles. [x]
		 * - Backtrack: Composite edges. [x]
		 * - Backtrack: Centroid (the Voronoi Vertex) [x?]
		 */
		int NumTriangles = TTriangles.Length;
		nodeGraph.Faces = new DFace[NumTriangles];
		for (int i = 0; i < NumTriangles; i++)
		{
			TTriangle Triangle = TTriangles[i];
			nodeGraph.Faces[i] = new DFace();
			nodeGraph.Faces[i].ID = Triangle.ID;
			nodeGraph.Faces[i].Vertices[0] = nodeGraph.DVertices[Triangle.GetVertexID(0)];
			nodeGraph.Faces[i].Vertices[1] = nodeGraph.DVertices[Triangle.GetVertexID(1)];
			nodeGraph.Faces[i].Vertices[2] = nodeGraph.DVertices[Triangle.GetVertexID(2)];

			Vector3 MidPoint =  nodeGraph.Faces[i].Vertices[0].Coords +
								nodeGraph.Faces[i].Vertices[1].Coords +
								nodeGraph.Faces[i].Vertices[2].Coords;
			MidPoint /= 3;
			TriangleCenters.Add(i, MidPoint);
			nodeGraph.Faces[i].Origin = MidPoint;
		}

		TriangleKDTree = new KDTree(TriangleCenters.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray(), 32);

		/*
		 * Generate Delaunay Edges (DEdge)
		 * - ID (The hashcode?)
		 * - P0 (Start Point)
		 * - P1 (End Point)
		 */
		int NumDEdges = TSubSegments.Length;
		nodeGraph.Edges = new DEdge[NumDEdges];
		for (int i = 0; i < NumDEdges; i++)
		{
			TSubSegment TempEdge = TSubSegments[i];
			nodeGraph.Edges[i] = new DEdge();
			nodeGraph.Edges[i].ID = TempEdge.GetHashCode();
			nodeGraph.Edges[i].Start = nodeGraph.DVertices[TempEdge.P0];
			nodeGraph.Edges[i].End = nodeGraph.DVertices[TempEdge.P1];
		}

		/*
		 * Generate Voronoi Vertices (VVertex)
		 * - ID 
		 * - Coords
		 * 
		 * To backtrack/Todo/Test?:
		 * - Associated Triangle (done, but needs testing)
		 * - Triangle Face Centroid (done but needs testing)
		 */
		int NumVVertices = VVertices.Length;
		nodeGraph.VVertices = new VVertex[NumVVertices];
		for (int i = 0; i < NumVVertices; i++)
		{
			TVVertex VorVert = VVertices[i];
			nodeGraph.VVertices[i] = new VVertex();
			nodeGraph.VVertices[i].ID = VorVert.ID;
			nodeGraph.VVertices[i].Coords = new Vector3((float)VorVert.X, (float)VorVert.Y, 0.0f);
		}

		/*
		 * Generate Voronoi Half-Edges
		 * - ID
		 * - Start Voronoi Vertex
		 * - End Voronoi Vertex
		 * 
		 * To Backtrack:
		 * - Twin half edge [x]
		 * - Voronoi Cell face [x]
		 * - Next Half Edge for the current face [x]
		 */
		int NumVHalfEdges = THalfEdges.Length;
		nodeGraph.HalfEdges = new VHalfEdge[NumVHalfEdges];
		for (int i = 0; i < NumVHalfEdges; i++)
		{
			THalfEdge TempHalfEdge = THalfEdges[i];
			nodeGraph.HalfEdges[i] = new VHalfEdge();
			nodeGraph.HalfEdges[i].ID = TempHalfEdge.ID;
			nodeGraph.HalfEdges[i].Start = nodeGraph.VVertices[TempHalfEdge.Origin.ID];
			nodeGraph.HalfEdges[i].End = nodeGraph.VVertices[TempHalfEdge.Next.Origin.ID];
		}

		/*
		 * Generate Voronoi Cells
		 * - ID
		 * - Centroid (corresponds to a Delaunay Vertex)
		 * - HalfEdge: Initial/starting half edge that surrounds the cell
		 * - Half Edges: The enumerated list of half edges.
		 * 
		 * To Backtrack:
		 * - List of neighbouring Voronoi cells. [x]
		 */
		int NumCells = VFaces.Length;
		nodeGraph.Cells = new VCell[NumCells];
		for (int i = 0; i < NumCells; i++)
		{
			TFace Cell = VFaces[i];
			nodeGraph.Cells[i] = new VCell();
			nodeGraph.Cells[i].ID = Cell.ID;
			nodeGraph.Cells[i].Centroid = nodeGraph.DVertices[Cell.generator.ID];
			nodeGraph.Cells[i].HalfEdge = nodeGraph.HalfEdges[Cell.Edge.ID];

			List<THalfEdge> CellHalfEdges = Cell.EnumerateEdges().ToList();
			nodeGraph.Cells[i].HalfEdges = new VHalfEdge[CellHalfEdges.Count];
			for (int j = 0; j < CellHalfEdges.Count;j++)
			{
				nodeGraph.Cells[i].HalfEdges[j] = nodeGraph.HalfEdges[CellHalfEdges[j].ID];
				nodeGraph.Cells[i].HalfEdges[j].Cell = nodeGraph.Cells[i];
			}
		}

		/* ***** Backtracking ***** */

		/*
		 * For each Delaunay triangle (DFace) backfill its three neighbouring faces
		 * and surrounding edges (test if hash code works)
		 */
		for (int i = 0; i < NumTriangles; i++)
		{
			TTriangle Triangle = TTriangles[i];
			for (int j = 0; j < 3; j++)
			{
				int TriangleID = Triangle.GetNeighborID(j);
				if (TriangleID >= 0)
				{
					nodeGraph.Faces[i].Neighbours.Add(nodeGraph.Faces[TriangleID]);
				}

				try
				{
					TSubSegment SubSegment = Triangle.GetSegment(j) as TSubSegment;
					if (SubSegment != null)
					{
						int SegmentID = SubSegment.GetHashCode();
						if (SegmentID >= 0)
						{
							nodeGraph.Faces[i].Edges[j] = nodeGraph.Edges[SegmentID];
						}
					}
				}
				catch (System.Exception e)
				{
					Debug.LogError("Error: " + e.Message);
				}
			}
		}

		/*
		 * For each Half-Edge setup its twin and its Next, and corresponding "Face"/Cell.
		 */
		for (int i = 0; i < NumVHalfEdges; i++)
		{
			THalfEdge TempHalfEdge = THalfEdges[i];

			if (TempHalfEdge.Twin != null && TempHalfEdge.Twin.ID >= 0)
			{
				nodeGraph.HalfEdges[i].Twin = nodeGraph.HalfEdges[TempHalfEdge.Twin.ID];
			}

			if (TempHalfEdge.Twin == null || (TempHalfEdge.Twin != null && (TempHalfEdge.Twin.face.ID == -1 || TempHalfEdge.face.ID == -1)))
			{
				nodeGraph.HalfEdges[i].Start.NodeType = ENodeType.BOUNDARY;
				nodeGraph.HalfEdges[i].End.NodeType = ENodeType.BOUNDARY;
			}
			
			if (TempHalfEdge.Next != null && TempHalfEdge.Next.ID >= 0)
			{
				nodeGraph.HalfEdges[i].Next = nodeGraph.HalfEdges[TempHalfEdge.Next.ID];
			}
			
			if (TempHalfEdge.Face != null && TempHalfEdge.Face.ID >= 0)
			{
				nodeGraph.HalfEdges[i].Cell = nodeGraph.Cells[TempHalfEdge.Face.ID];
			}			
		}

		/*
		 * For each Voronoi Cell enumerate its neighbouring cells
		 * 
		 * Todo: Need to also add cells which are at the otherside of the map
		 */
		for (int i = 0; i < NumCells; i++)
		{
			TFace Cell = VFaces[i];
			THalfEdge[] CellEdges = Cell.EnumerateEdges().ToArray();
			bool IsBoundary = false;
			for (int j = 0; j < CellEdges.Length; j++)
			{
				if (CellEdges[j].Twin != null && CellEdges[j].Twin.Face != null && CellEdges[j].Twin.Face.ID >= 0)
				{
					try
					{
						nodeGraph.Cells[i].Neighbours.Add(nodeGraph.Cells[CellEdges[j].Twin.Face.ID]);
					}
					catch (System.Exception e)
					{
						Debug.LogError("Exception at i=" + i + ", and j=" + j);
					}					
				}
				else
				{
					if (CellEdges[j].Twin == null)
					{
						Vector3 p0 = new Vector3((float)CellEdges[j].Origin.X, (float)CellEdges[j].Origin.Y, 0.0f);
						Vector3 p1 = new Vector3((float)CellEdges[j].Next.Origin.X, (float)CellEdges[j].Next.Origin.Y, 0.0f);
						Vector3 Midpoint = (p0 + p1) / 2;
						Vector3 Dir = Vector3.Normalize(Midpoint - nodeGraph.Cells[i].Centroid.Coords);
						if (UMath.Abs(Vector3.Dot(Vector3.right, Dir)) > 0.5f)
						{
							// only really care about the horizontal axis edges of the map
							IsBoundary = true;						
						}
						BoundaryEdges[j] = CellEdges[j].Twin;
					}
				}				
			}			

			// If the current Cell is on the outer edge find its
			// horizontal flip Cell update the list of neighbours
			if (IsBoundary)
			{
				nodeGraph.Cells[i].NodeType = ENodeType.BOUNDARY;
				BoundaryCells[i] = nodeGraph.Cells[i];
				Vector3 CurrentCellCoord = nodeGraph.Cells[i].Centroid.Coords;
				Vector3 OppositeCellCoord = new Vector3(GeneratorConfig.MapDimensions.x - CurrentCellCoord.x, CurrentCellCoord.y);
				List<int> Results = new List<int>();
				if (MapUtils.GetNodeIDFromCoordinate(OppositeCellCoord, VSiteKDTree, SiteQuery, ref Results))
				{
					nodeGraph.Cells[i].Neighbours.Add(nodeGraph.Cells[Results[0]]);
				}
				else
				{
					Debug.LogWarning("Node ID (Flipped Boundary) not found: " + i);
				}
			}
		}

		/*
		 * For each Voronoi Vertex assign the "leaving" vertex
		 */
		for (int i = 0; i < NumVVertices; i++)
		{
			TVVertex VorVert = VVertices[i];
			nodeGraph.VVertices[i].Leaving = nodeGraph.HalfEdges[VorVert.Leaving.ID];

			List<THalfEdge> LeavingEdges = VorVert.EnumerateEdges().ToList();
			foreach (var elem in LeavingEdges)
			{
				nodeGraph.VVertices[i].Neighbours.Add(nodeGraph.HalfEdges[elem.ID]);
			}
		}

		// properly initializing the centroids of all triangles and which triangle corresponds
		// to which voronoi vertex, as ideally every voronoi vertex should be in the middle of 
		// its corresponding delaunay triangle
		VVertex[] VerticesToSmooth = nodeGraph.VVertices;
		foreach (var vert in VerticesToSmooth)
		{
			if (vert.NodeType == ENodeType.BOUNDARY)
			{
				Debug.Log("vertex id: " + vert.ID + " is a boundary vertex skipping...");
				continue;
			}				

			Vector3 TriangleCentroid = new Vector3();		
			Debug.Log("vertex id: " + vert.ID + " number of neighbours: " + vert.Neighbours.Count);
			foreach (VHalfEdge vhalfedge in vert.Neighbours)
			{
				if (vhalfedge.Twin != null && vhalfedge.Cell != null && vhalfedge.Cell.Centroid != null)
				{
					TriangleCentroid += vhalfedge.Cell.Centroid.Coords;

					if (vhalfedge.Twin.Cell != null && vhalfedge.Cell.Centroid != null)
					{
						//TriangleCentroid += vhalfedge.Twin.Cell.Centroid.Coords;
					}	
				}
			}
			TriangleCentroid /= vert.Neighbours.Count;

			List<int> Results = new List<int>();
			if (MapUtils.GetNodeIDFromCoordinate(TriangleCentroid, TriangleKDTree, SiteQuery, ref Results))
			{
				Debug.Log("Assigning voronoi cell vertex id: " + vert.ID + " to delaunay centroid: " + Results[0]);
				nodeGraph.Faces[Results[0]].Centroid = vert;
				vert.Triangle = nodeGraph.Faces[Results[0]];
				if (Configuration.VoronoiRelaxationEnabled)
				{
					// if relaxation is enabled, update the position of this vertex to match
					// it's triangles centroid
					vert.Coords = TriangleCentroid;
				}
			}
			else
			{
				Debug.LogWarning("Node ID (Triangle Centroid Voronoi Vertex) not found...");
			}					
		}

		return nodeGraph;
	}
}