

using DataStructures.ViliWonka.KDTree;
using System.Collections.Generic;
using System.Linq;
using TriangleNet.Geometry;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using TMesh = TriangleNet.Mesh;
using TVertex = TriangleNet.Geometry.Vertex;
using VQuery = DataStructures.ViliWonka.KDTree.KDQuery;
using TQualityOptions = TriangleNet.Meshing.QualityOptions;

public enum ENodeType
{
	None,
	BOUNDARY,
	INTERIOR
}

public enum EUnityMeshMode
{
	VORONOI,
	DELAUNAY
}

public abstract class NodeBase
{
	public int ID { get; set; } = -1;
}

/*
 * VCell: Voronoi Cell.
 * 
 */
public class VCell : NodeBase, INode
{
	public INodeData Data { get; set; }

	public DVertex Centroid { get; set; }

	public VHalfEdge HalfEdge { get; set; }

	public VHalfEdge[] HalfEdges { get; set; }

	public VVertex[] Vertices { get; set; }

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
	}

	public ENodeType NodeType { get; set; } = ENodeType.None;

	public VCell()
	{
		Neighbours = new List<INode>();
	}
}

/*
 * DFace: Delaunay Triangle "Face".
 * 
 */
public class DFace : NodeBase, INode
{
	public INodeData Data { get; set; }

	// The voronoi vertex associated with this triangle, at most 1
	public VVertex Centroid {  get; set; }

	// the actual midpoint of this triangle as the centroid might get adjusts for aesthetics
	public Vector3 Origin { get; set; }

	public DVertex[] Vertices { get; set; }
	public DEdge[] Edges { get; set; }

	public DFace()
	{
		Vertices = new DVertex[3];
		Neighbours = new List<INode>();
		Edges = new DEdge[3];
		Origin = new Vector3();
	}

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
	}
}

public abstract class VertexBase
{
	public Vector3 Coords { get; set; }
}

/*
 * VVertex: One of the vertices surrounding a voronoi cell.
 * Also represents the centoid of a delaunay face.
 */
public class VVertex : VertexBase, INode
{
	public INodeData Data { get; set; }

	public int ID { get; set; } = -1;

	public DFace Triangle { get; set; }

	public VHalfEdge Leaving {  get; set; }

	public ENodeType NodeType { get; set; } = ENodeType.None;

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
	}

	public VVertex()
	{
		Neighbours = new List<INode>();
	}
}

/*
 * DVertex: One of the vertices surrounding a delaunay triangle.
 * Also represents the centroid of Voronoi Cell.
 */
public class DVertex : VertexBase, INode
{
	public INodeData Data { get; set; }

	public int ID { get; set; } = -1;

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
	}

	public DVertex()
	{
		Neighbours = new List<INode>();
	}
}

// test comment
public class VHalfEdge : INodeEdge<VVertex>
{
	public int ID { get; set; } = -1;

	public VVertex Start { get; set; }
	public VVertex End  { get; set; }

	public VHalfEdge Next { get; set; }

	public VHalfEdge Twin {  get; set; }
	public VCell Cell { get; set; }
	public INodeData Data { get; set; }
	public List<INode> Neighbours { get; set; }

	public VHalfEdge()
	{
		Start = new VVertex();
		End = new VVertex();
	}

	public List<INode> GetNeighbours()
	{
		throw new System.NotImplementedException();
	}
}

public class DEdge : INodeEdge<DVertex>
{
	public int ID { get; set; } = -1;

	public DVertex Start { get; set; }
	public DVertex End  { get; set; }
	public INodeData Data { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
	public List<INode> Neighbours { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

	public List<INode> GetNeighbours()
	{
		throw new System.NotImplementedException();
	}
}

public class PolygonalNodeGraph : INodeGraph
{
	public DVertex[] DVertices;
	public VVertex[] VVertices;

	public VCell[] Cells;
	public DFace[] Faces;

	public VHalfEdge[] HalfEdges;
	public DEdge[] Edges;

	public KDTree VSiteKDTree = null;

	public Vector3[] CellCoordinates;

	VQuery SiteQuery = new VQuery();

	List<int> CellSearchResults = new List<int>();

	public PolygonalNodeGraph()
	{

	}

	public void InitCellCoordinateSearchTree()
	{
		if (CellCoordinates == null)
		{
			Debug.LogError("CellCoordinates Array Not Initialized or Valid");
			return;
		}
		VSiteKDTree = new KDTree(CellCoordinates, 32);
	}

	public int GetCellIDFromCoordinate(Vector3 InCoordinate)
	{
		if (VSiteKDTree == null)
		{
			return -1;
		}

		CellSearchResults.Clear();
		if (MapUtils.GetNodeIDFromCoordinate(InCoordinate, VSiteKDTree, SiteQuery, ref CellSearchResults))
		{
			return CellSearchResults[0];
		}

		Debug.LogWarning("Node ID (Triangle Centroid Voronoi Vertex) not found...");
		return -1;
	}

	public DFace GetFace(int InFaceIndex)
	{
		if (Faces != null && InFaceIndex >= 0 && InFaceIndex < Faces.Length)
		{
			return Faces[InFaceIndex];
		}

		return null;
	}

	public VCell GetCell(int InCellIndex)
	{
		if (Cells != null && InCellIndex >= 0 && InCellIndex < Cells.Length)
		{
			return Cells[InCellIndex];
		}

		return null;
	}

	public VVertex GetVoronoiVertex(int InVVertexIndex)
	{
		if (VVertices != null && InVVertexIndex >= 0 && InVVertexIndex < VVertices.Length)
		{
			return VVertices[InVVertexIndex];
		}

		return null;
	}

	public DVertex GetDelaunayVertex(int InDVertexIndex)
	{
		if (DVertices != null && InDVertexIndex >= 0 && InDVertexIndex < DVertices.Length)
		{
			return DVertices[InDVertexIndex];
		}

		return null;
	}

	public int GetNumFaces()
	{
		if (Faces != null)
			return Faces.Length;

		return -1;
	}

	public int GetNumCells()
	{
		if (Cells != null)
			return Cells.Length;

		return -1;
	}

	public int GetNumVoronoiVertices()
	{
		if (VVertices != null)
			return VVertices.Length;

		return -1;
	}

	public int GetNumDelaunayVertices()
	{
		if (DVertices != null)
			return DVertices.Length;

		return -1;
	}

	public int GetNumHalfEdges()
	{
		if (HalfEdges != null)
			return HalfEdges.Length;

		return -1;
	}

	public int GetNumDelaunayEdges()
	{
		if (Edges != null)
			return Edges.Length;

		return -1;
	}

	public Mesh GenerateUnityMeshFromGraph(EUnityMeshMode InMeshMode)
	{
		switch (InMeshMode) 
		{
			case EUnityMeshMode.VORONOI:
				return GenerateSimpleUnityMeshFromVoronoi();
			case EUnityMeshMode.DELAUNAY:
				return GenerateUnityMeshFromDelaunay();
			default:
			return null;
		}
	}

	private Mesh GenerateSimpleUnityMeshFromVoronoi()
	{
		Mesh OutMesh = new Mesh();

		List<VHalfEdge> Edges = new List<VHalfEdge>(HalfEdges);
		List<Vector3> MeshVertices = new List<Vector3>();
		List<int> MeshIndices = new List<int>();
		int IndexCounter = 0;
		for (int i = 0; i < Edges.Count; i++)
		{
			MeshVertices.Add(Edges[i].Start.Coords);
			MeshVertices.Add(Edges[i].End.Coords);
			MeshIndices.Add(IndexCounter++);
			MeshIndices.Add(IndexCounter++);
		}
		OutMesh.vertices = MeshVertices.ToArray();
		OutMesh.SetIndices(MeshIndices, MeshTopology.Lines, 0);

		return OutMesh;
	}

	private Mesh GenerateUnityCellMeshFromVoronoi()
	{
		Mesh OutMesh = new Mesh();

		TriangleNet.Meshing.ConstraintOptions options = new TriangleNet.Meshing.ConstraintOptions()
		{
			ConformingDelaunay = true
		};

		Vector2 TextureSizes = new Vector2Int(600, 600);

		List<VCell> cells = new List<VCell>(Cells);
		int numCells = cells.Count;
		float Offset = 0.0625f / cells.Count;

		List<VHalfEdge> Edges = new List<VHalfEdge>(HalfEdges);
		List<Vector3> MeshVertices = new List<Vector3>();
		List<int> MeshIndices = new List<int>();
		List<Vector2> UVs = new List<Vector2>();
		int IndexCounter = 0;
		for (int i = 0; i < cells.Count; i++)
		{
			VCell cell = cells[i];
			Polygon DelaneyShape = new Polygon();
			for (int j = 0; j < cell.Vertices.Length; j++)
			{
				VVertex vVertex = cell.Vertices[j];
				//MeshVertices.Add(Edges[i].Start.Coords);
				//MeshVertices.Add(Edges[i].End.Coords);
				//MeshIndices.Add(IndexCounter++);
				//MeshIndices.Add(IndexCounter++);
				
				UVs.Add(new Vector2(cell.ID / (float)numCells + Offset, 0.0f));

				TVertex VertexToAdd = new TVertex(vVertex.Coords.x, vVertex.Coords.y);
				DelaneyShape.Add(VertexToAdd);
			}
			DelaneyShape.Bounds();

			Mesh cellMesh = GenerateUnityMeshFromTriangleNetMesh((TMesh)DelaneyShape.Triangulate(options));
			// add to our mesh
		}

		OutMesh.vertices = MeshVertices.ToArray();
		OutMesh.SetIndices(MeshIndices, MeshTopology.Triangles, 0);
		OutMesh.SetUVs(0, UVs.ToArray());

		return OutMesh;
	}

	public Mesh GenerateUnityMeshFromTriangleNetMesh(TMesh InMesh)
	{
		return GenerateUnityMeshFromTriangulation(InMesh);
	}

	public Mesh GenerateUnityMeshFromTriangulation(TMesh triangleNetMesh, TQualityOptions options = null)
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

	private Mesh GenerateUnityMeshFromDelaunay()
	{
		Mesh OutMesh = new Mesh();

		int[] trisIndex = new int[Faces.Length * 3];

		int k = 0;

		foreach (var triangle in Faces)
		{
			for (int i = 2; i >= 0; i--)
			{
				trisIndex[k] = triangle.Vertices[i].ID;
				k++;
			}
		}

		OutMesh.vertices = CellCoordinates;
		OutMesh.triangles = trisIndex;

		OutMesh.RecalculateBounds();
		OutMesh.RecalculateNormals();
		return OutMesh;
	}
}