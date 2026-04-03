

using DataStructures.ViliWonka.KDTree;
using System.Collections.Generic;
using UnityEditor.Timeline.Actions;
using UnityEngine;

using VQuery = DataStructures.ViliWonka.KDTree.KDQuery;

public enum ENodeType
{
	None,
	BOUNDARY
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

	public VVertex Centroid {  get; set; }

	public DVertex[] Vertices { get; set; }
	public DEdge[] Edges { get; set; }

	public DFace()
	{
		Vertices = new DVertex[3];
		Neighbours = new List<INode>();
		Edges = new DEdge[3];
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
				return GenerateUnityMeshFromVoronoi();
			case EUnityMeshMode.DELAUNAY:
				return GenerateUnityMeshFromDelaunay();
			default:
			return null;
		}
	}

	private Mesh GenerateUnityMeshFromVoronoi()
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