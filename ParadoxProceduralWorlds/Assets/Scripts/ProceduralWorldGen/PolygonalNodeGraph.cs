

using System.Collections.Generic;
using UnityEngine;

public abstract class NodeBase
{
	public int ID { get; set; }
}

/*
 * VCell: Voronoi Cell.
 * 
 */
public class VCell : NodeBase, INode
{
	public INodeData Data { get; set; }

	public DVertex Centroid {  get; set; }

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
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

	public int ID { get; set; }

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
	}
}

/*
 * DVertex: One of the vertices surrounding a delaunay triangle.
 * Also represents the centroid of Voronoi Cell.
 */
public class DVertex : VertexBase, INode
{
	public INodeData Data { get; set; }

	public int ID { get; set; }

	public List<INode> Neighbours { get; set; }

	public List<INode> GetNeighbours()
	{
		return Neighbours;
	}
}

// test comment
public class VHalfEdge : INodeEdge
{
	public int ID { get; set; }

	public INode Start { get; set; }
	public INode End  { get; set; }
}

public class DEdge : INodeEdge
{
	public int ID { get; set; }

	public INode Start { get; set; }
	public INode End  { get; set; }
}

public class PolygonalNodeGraph : INodeGraph
{
	public DVertex[] DVertices;
	public VVertex[] VVertices;

	public VCell[] Cells;
	public DFace[] Faces;

	public VHalfEdge[] HalfEdges;
	public DEdge[] Edges;

	public PolygonalNodeGraph()
	{

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
}